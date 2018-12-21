using System;
using System.Threading;
using Impinj.OctaneSdk;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using MathNet.Numerics.Distributions;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;

using rapid.Filters;
using rapid;
using RFID;

namespace RFID
{
    static class RFIDReader
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
        static ConcurrentDictionary<string, double> VelFeatureDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> DeltaRSSIDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> DeltaTimeDict = new ConcurrentDictionary<string, double>();

        static ConcurrentDictionary<string, double> lastTimeDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> lastRSSIDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> lastPhaseAngleDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> lastDeltaPhiDict = new ConcurrentDictionary<string, double>();
        static ConcurrentDictionary<string, double> channelDict = new ConcurrentDictionary<string, double>();

        static string time_csv = "deltatime.csv";
        static string rssi_csv = "rssi.csv";
        static string vel_csv = "velocity.csv";
        // Stores timestamped data of the channel and the phase.
        static string phase_csv = "phase.csv";
        // Store data to relearn the velocity filters.
        static string vel_feature_csv = "vel_feature.csv";

        // Maps a tag to a probability distribution over whether the tag is covered or uncovered.
        static ConcurrentDictionary<string, BernoulliFilter> TouchFilterDict = new ConcurrentDictionary<string, BernoulliFilter>();
        // Maps a tag to a probability distribution over the tag's velocity.
        static ConcurrentDictionary<string, GaussianFilter> VelocityFilterDict = new ConcurrentDictionary<string, GaussianFilter>();

        // The following two observable dictionaries are used in the SMC code to get discrete values queried from the distributions
        // above, in order to update the histogram over all InteractionStates.

        // Maps a tag to the latest value queried from the associated distribution over tag covered or uncovered.
        static ObservableConcurrentDictionary<Bernoulli> ObservableTouchDistributionDict = new ObservableConcurrentDictionary<Bernoulli>();
        // Maps a tag to the latest value queried from the associated distribution over the tag's velocity.
        static ObservableConcurrentDictionary<Normal> ObservableVelocityDistributionDict = new ObservableConcurrentDictionary<Normal>();

        // Speed of light in cm/second.
        static double c = 2.998 * 1e8;
        // The reader thread sets NUMTAGS by passing it into initializeSettings().
        static int NUMTAGS = 1;
        static int MilliSecPerSeconds = 1000;

        // Time Series Chart
        static bool shouldGraph = true;
        static TimeSeriesChart timeSeriesChart = null;

        // Boolean that controls whether or not we should send more tag distribution data to the application.
        // It's set to true when the application finishes processing the latest update, and
        // it's set to false right after the application reads in the latest batch of distributions.
        public static bool shouldReportTagDistributions = true;

        static string testEPC = "757733B2DDD9014000000001";

        // Method that the reader thread will run.
        public static void run()
        {
            int count = 0;
            while (true)
            {
                Thread.Sleep(7);

                // Update dictionaries no feature.
                double timeNow = (double)DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond * MilliSecPerSeconds);
                updateDictsWithoutFeature(timeNow);
                if (TouchFilterDict.ContainsKey(testEPC))
                {
                    printFilterValues(testEPC);
                }
                count += 1;
                // If we should pass more tag data to the application, then do so.
                if (shouldReportTagDistributions)
                {
                    // Create a dictionary with the latest distributions for touch.
                    ConcurrentDictionary<string, Bernoulli> newTouchDict = new ConcurrentDictionary<string, Bernoulli>();
                    foreach (var touchEntry in TouchFilterDict)
                    {
                        newTouchDict[touchEntry.Key] = (Bernoulli)touchEntry.Value.getDistribution();
                    }
                    // Create a dictionary with the latest distributions for velocity.
                    ConcurrentDictionary<string, Normal> newVelocityDict = new ConcurrentDictionary<string, Normal>();
                    foreach (var velocityEntry in VelocityFilterDict)
                    {
                        newVelocityDict[velocityEntry.Key] = (Normal)velocityEntry.Value.getDistribution();
                    }
                    // Do not change the order in which the dictionaries are updated.
                    // TODO: be less rigid about this requirement.
                    ObservableTouchDistributionDict.replaceCollectionWith(newTouchDict, true);
                    ObservableVelocityDistributionDict.replaceCollectionWith(newVelocityDict, true);
                }

                if (TouchFilterDict.ContainsKey(testEPC))
                {
                    // double probVisible = ((Bernoulli)(TouchFilterDict[testEPC]).getDistribution()).P;
                    // Console.WriteLine(testEPC + " prob visible: " + probVisible.ToString());
                }

                reader.QueryTags();
            }
        }

        // Sets the delegates for handling collection changed events and property changed events
        // for the BernouliFilterDict and the GaussianFilterDict.
        // This function is called by an RFIDReaderThread instance, and the delegates
        // are passed into the RFIDReaderThread from the RFIDApplication.
        public static void setDelegates(Action<ConcurrentDictionary<string, Bernoulli>> onTouchDistributionChanged,
                                        Action<ConcurrentDictionary<string, Normal>> onVelocityDistributionChanged) {
            ObservableTouchDistributionDict.setHandler(onTouchDistributionChanged);
            ObservableVelocityDistributionDict.setHandler(onVelocityDistributionChanged);
        }

        public static void initializeSettings(int numTags, TimeSeriesChart chart)
        {
            // Set the number of tags.
            NUMTAGS = numTags;
            // Set the chart.
            if (chart != null)
            {
                timeSeriesChart = chart;
                shouldGraph = true;
            }

            // Connect to the reader.
            // Change the ReaderHostname constant in SolutionConstants.cs 
            // to the IP address or hostname of your reader.
            reader.Connect(SolutionConstants.ReaderHostname);

            /////////////////////////////////////////
            //    SETTINGS                         //
            /////////////////////////////////////////
            Settings settings = reader.QueryDefaultSettings();
            
            // All data that we want tag to report.
            settings.Report.IncludeAntennaPortNumber = true;
            settings.Report.IncludeFirstSeenTime = true;
            settings.Report.IncludePhaseAngle = true;
            settings.Report.IncludePeakRssi = true;
            settings.Report.IncludeChannel = true;

            // Optimize reader for region with low number of tags, low chance of interference.
            settings.ReaderMode = ReaderMode.MaxMiller;
            settings.SearchMode = SearchMode.DualTarget;

            // Enable antenna #1. Disable all others.
            settings.Antennas.DisableAll();
            settings.Antennas.GetAntenna(1).IsEnabled = true;

            // Use same settings as the MultiReader software.
            settings.Antennas.GetAntenna(1).TxPowerInDbm = 25;
            settings.Antennas.GetAntenna(1).RxSensitivityInDbm = -70;

            // Wait until tag query has ended before sending tag report.
            settings.Report.Mode = ReportMode.WaitForQuery;

            // Apply the newly modified settings.
            reader.ApplySettings(settings);

            // Assign the TagsReported event handler. (Gets all of the tags)
            reader.TagsReported += OnTagsReported;

            // Start reading.
            reader.Start();

            // Initialize CSV Files.
            File.WriteAllText(@"deltatime.csv", string.Empty);
            File.WriteAllText(@"rssi.csv", string.Empty);
            File.WriteAllText(@"velocity.csv", string.Empty);

            Console.WriteLine("Initialized Reader");
        }

        static void disconnectReader()
        {
            // Stop reading.
            reader.Stop();
            // Disconnect from the reader.
            reader.Disconnect();
            Console.WriteLine("Disconnect");
        }

        // Perform an update in the case where there is no feature available feature.
        // The update is based on the amount of time since we have last seen the tags.
        // This is updating the filters only, not the last_seen dictionaries.
        private static void updateDictsWithoutFeature(double timeNow)
        {
            foreach (var tEntry in TouchFilterDict)
            {
                double deltaT = timeNow - lastTimeDict[tEntry.Key];
                tEntry.Value.updateWithoutFeature(deltaT);
            }
            foreach (var vEntry in VelocityFilterDict)
            {
                double deltaT = timeNow - lastTimeDict[vEntry.Key];
                if (deltaT < 0)
                {
                    Console.WriteLine("deltaT < 0 in updateDictsWithoutFeature");
                    continue;
                }
                if (vEntry.Key == testEPC)
                {
                    //addLineToCSV(vel_feature_csv, "deltaT " + deltaT.ToString());
                }
                vEntry.Value.updateWithoutFeature(deltaT);
            }
        }

        // Used to throttle print statement frequency to once every 15 reads.
        static int count = 0;
        const int printFrequency = 15;
        
        // Tags aren't reported until we ask for them.
        // This method is called asynchronously when we get a report.
        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                string epc = tag.Epc.ToString().Replace(" ", String.Empty);

                if (!SolutionConstants.tagIds.Contains(epc))
                {
                    continue;
                }
                DateTime time = DateTime.Now;
                // Get the phase angle to determine velocity.
                double phaseAngle = tag.PhaseAngleInRadians;
                // Mod by PI to overcome reader hardware errors.
                phaseAngle = phaseAngle % Math.PI;

                // Get the RSSI (received signal stength intensity) to determine position.
                double rssi = tag.PeakRssiInDbm;
                double channelInHz = tag.ChannelInMhz * 1e6;
                double timeInTicks = (double)time.Ticks;
                double timeInSec = timeInTicks / (TimeSpan.TicksPerMillisecond * MilliSecPerSeconds);

                // Add phase and channel data to a file.
                // We will do analysis on the phase angles within a channel over time,
                // and for the phases vs. channels.
                // Collect multiple times, once with tag not moving, once with tag moving up, and once with tag moving down.
                if (epc == testEPC)
                {
                    string data = timeInSec.ToString() + " " + channelInHz.ToString() + " " + phaseAngle.ToString();
                    // addLineToCSV(phase_csv, data);
                }

                // If we haven't seen this tag before, initialize dictionaries for it.
                if (!lastTimeDict.Keys.Contains(epc))
                {
                    lastTimeDict.TryAdd(epc, timeInSec);
                    lastPhaseAngleDict.TryAdd(epc, phaseAngle);
                    lastDeltaPhiDict.TryAdd(epc, 0.0);
                    lastRSSIDict.TryAdd(epc, rssi);
                    channelDict.TryAdd(epc, channelInHz);

                    VelocityFilterDict[epc]  = new GaussianFilter(new Normal(0.0, 1e-5));
                    TouchFilterDict[epc] = new BernoulliFilter(new Bernoulli(.5), NUMTAGS);
                    return;
                }

                // Get time between reads.
                double lastTime;
                lastTimeDict.TryGetValue(epc, out lastTime);

                double deltaTimeInSec = timeInSec - lastTime;
               // plot("deltaT", timeInSec, deltaTimeInSec);
                if (deltaTimeInSec <= 0)
                {
                    Console.WriteLine("deltaTimeInSec less than or equal to 0");
                    continue;

                }

                updateTimeBetweenReadsDictionary(epc, timeInSec, deltaTimeInSec);
                updateVelocityDictionary(epc, timeInSec, phaseAngle, channelInHz, deltaTimeInSec);
                updateRSSIDictionary(epc, rssi);
                // Update dictionaries containing previous information.
                updateLastDictionaries(epc, phaseAngle, timeInSec, rssi);

                if (count % printFrequency == 1 && epc == testEPC)
                {
                    //printUpdatedDictionaries(epc);
                }
                count += 1;

            }
        }


        static void updateTimeBetweenReadsDictionary(String epc, double timeInSec, double deltaTimeInSec)
        {
            DeltaTimeDict[epc] = deltaTimeInSec;
            // addtoCSV(time_csv, DeltaTimeDict);

            // Update probability distribution for whether tag is covered or uncovered.
            TouchFilterDict[epc].updateWithFeature(deltaTimeInSec, 0);
            // Plot the probability that the tag is touched.
            // double probVisible = ((Bernoulli)(TouchFilterDict[epc]).getDistribution()).P;
            // Console.WriteLine(epc + " prob visible: " + probVisible.ToString());
            // plot("probVisible " + epc, timeInSec, probVisible);
        }

        static void updateVelocityDictionary(String epc, double timeInSec, double phaseAngle, double channelInHz, double deltaTimeInSec)
        {
            // Only update velocity if channel is correct.
            if (channelDict[epc] == channelInHz)
            { 
                double lastPhaseAngle;
                lastPhaseAngleDict.TryGetValue(epc, out lastPhaseAngle);
                double lastDeltaPhi;
                lastDeltaPhiDict.TryGetValue(epc, out lastDeltaPhi);

                // Multiply by -1 so that moving upward (away from reader) gives a positive deltaPhi.
                double deltaPhi = -1.0 * (phaseAngle - lastPhaseAngle);
                lastDeltaPhiDict[epc] = deltaPhi;
                // Throw out bad boundary values that result from cycling through values from 0 -> 2pi -> 0.
                // TODO: pull this threshold out into a constant?
                if (Math.Abs(lastDeltaPhi - deltaPhi) > Math.PI/2.0)
                {
                    // Console.WriteLine("   ignoring...");
                    return;
                }
                //plot("phase", timeInSec, phaseAngle/channelInHz);
                //plot("deltaPhi", timeInSec, deltaPhi);
                double constant = c / (4 * Math.PI * channelInHz);
                double velFeature = constant * deltaPhi / deltaTimeInSec;
                // Console.WriteLine("velFeature: " + velFeature.ToString());
                // plot("velFeature", timeInSec, velFeature);
                // Collect data to relearn the velocity filters.
                if (epc == testEPC)
                {
                    // addLineToCSV(vel_feature_csv, "deltaPhi " + deltaPhi.ToString() + " feature " + velFeature.ToString());
                }
                if (Math.Abs(velFeature) < Double.MaxValue)
                {
                    // Console.WriteLine("deltaVel " + velFeature);
                    // VelFeatureDict[epc] = velFeature;
                    // addtoCSV(vel_csv, VelFeatureDict);
                    VelocityFilterDict[epc].updateWithFeature(velFeature, deltaTimeInSec);
                    double meanVel = ((Normal)(VelocityFilterDict[epc]).getDistribution()).Mean;
                    double variance = ((Normal)(VelocityFilterDict[epc]).getDistribution()).Variance;
                    // Console.WriteLine(epc + " meanVel: " + meanVel + "    variance: " + variance);
                    // plot("velEstimate", timeInSec, ((Normal)VelocityFilterDict[epc].getDistribution()).Mean);
                }

            }
            else
            {
                channelDict[epc] = channelInHz;
            }
        }

        static void updateRSSIDictionary(String epc, double rssi)
        {
            double lastRSSI;
            lastRSSIDict.TryGetValue(epc, out lastRSSI);

            double deltaRSSI = rssi - lastRSSI; //in db
            DeltaRSSIDict[epc] = deltaRSSI;
            // addtoCSV(rssi_csv, DeltaRSSIDict);
        }

        // Record the latest values for this tag.
        static void updateLastDictionaries(String epc, double phaseAngle, double timeInSec, double rssi)
        {
            lastPhaseAngleDict[epc] = phaseAngle;
            lastTimeDict[epc] = timeInSec;
            lastRSSIDict[epc] = rssi;
        }


   /* Helpers and Debugging Utilities. */

        static void plot(string label, double x, double y)
        {
            if (shouldGraph)
            {
                timeSeriesChart.addPoint(label, x, y);
            }
        }

        static void printLastDictionaries(String epc)
        {
            double lastTime2;
            lastTimeDict.TryGetValue(epc, out lastTime2);
            Console.WriteLine("LastTimeDict " + lastTime2);

            double lastphaseAngle;
            lastPhaseAngleDict.TryGetValue(epc, out lastphaseAngle);
            Console.WriteLine("LastPhaseAngle" + lastphaseAngle);

            double lastRSSI;
            lastRSSIDict.TryGetValue(epc, out lastRSSI);
            Console.WriteLine("LastRSSI" + lastRSSI);
        }

        static void printUpdatedDictionaries(String epc)
        {
            double dTime2;
            DeltaTimeDict.TryGetValue(epc, out dTime2);
            Console.WriteLine("DeltaTimeDict" + dTime2);

            double dRSSI2;
            DeltaRSSIDict.TryGetValue(epc, out dRSSI2);
            Console.WriteLine("DeltaRSSIDict" + dRSSI2);

            double dVel2;
            VelFeatureDict.TryGetValue(epc, out dVel2);
            Console.WriteLine("DeltaVelDict" + Math.Round(dVel2, 6));

        }

        static void printFilterValues(string epc)
        {
            BernoulliFilter bFilter;
            TouchFilterDict.TryGetValue(epc, out bFilter);
            Bernoulli touchDist = (Bernoulli) bFilter.getDistribution();

            GaussianFilter gFilter;
            VelocityFilterDict.TryGetValue(epc, out gFilter);
            Normal velDist = (Normal)gFilter.getDistribution();

            // Console.WriteLine("P(visible) = " + touchDist.P);
            // Console.WriteLine("Velocity = (" + velDist.Mean + ", " + velDist.StdDev + ")");
                
        }

        static void addtoCSV(string csvFile, ConcurrentDictionary<string, double> dict)
        {
            String csv = String.Join(
                ",", dict.Select(d => d.Value)
                ); // add each value to the correct key
            File.AppendAllText(csvFile, csv + ",\n");
        }

        static void addLineToCSV(string csvFile, string line)
        {
            File.AppendAllText(csvFile, line + "\n");
        }
    }
}
