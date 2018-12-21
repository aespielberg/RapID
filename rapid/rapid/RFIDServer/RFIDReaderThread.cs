using System;
using System.Threading;
using Impinj.OctaneSdk;
using MathNet.Numerics.Distributions;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using rapid;

namespace RFID
{
    public class RFIDReaderThread
    {
        TimeSeriesChart chart = null;
        bool shouldGraph;
        public RFIDReaderThread() {

        }

        // Initializes an RFIDReader and passes the delegate functions to it.
        // Also sets up a thread to run a realtime chart, if desired.
        public RFIDReaderThread(int numTags, Action<ConcurrentDictionary<string, Bernoulli>> onTouchDistributionChanged,
                                Action<ConcurrentDictionary<string, Normal>> onVelocityDistributionChanged, bool shouldGraph) {
            this.shouldGraph = shouldGraph;
            Console.WriteLine("reader thread " + shouldGraph);
            if (shouldGraph)
            {
                this.chart = new TimeSeriesChart();
            }
            RFIDReader.initializeSettings(numTags, chart);
            if (onTouchDistributionChanged != null)
            {
                RFIDReader.setDelegates(onTouchDistributionChanged, onVelocityDistributionChanged);

            }
        }

        // Starts up a new thread that runs the RFIDReader.
        public void run()
        {
            if (this.shouldGraph)
            {
                Thread chartThread = new Thread(() => Application.Run(this.chart));
                chartThread.Start();
                // Wait for form to load up.
                if (this.chart != null)
                {
                    while (!this.chart.isReady()) ;
                }
            }

            Thread readerThread = new Thread(new ThreadStart(RFIDReader.run));
            readerThread.Start();

            // Spin for a while waiting for the started thread to become
            // alive:
            while (!readerThread.IsAlive) ;
        }
    }
}