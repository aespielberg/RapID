using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using MathNet.Numerics.Distributions;
using System;

using RFID;
using rapid.Filters;

namespace smc {

    // Paramaterized by an Interaction, which is in turn parameterized by an InteractionState.
    public abstract class RFIDApplication<TState, TInteraction> where TInteraction : Interaction<TState>, new()
        where TState : InteractionState {

        // An RFID reader thread instance that we will need to start up.
        protected RFIDReaderThread readerThread;

        // Concurrent queue for Interactions.
        protected TInteraction interaction;
        // Array of all tagIds involved in this application.
        protected List<string> tagIds = new List<string>();
        // Concurrent queue of tagFeatureDistributions, that should be updated by the delegate
        // functions onCollectionChanged and onPropertyChanged.
        ConcurrentQueue<TagInputDistribution> tagFeatureDistributions;

        // Number of tags and number of modalities (a modality is touch or velocity or proximity etc.)
        protected int numTags;
        protected int numModalities;


        /*
        Constructor for the class, empty.
        */
        public RFIDApplication() {

        }

        /*
        The initialize method sets up the delegates for the observable concurrent dictionaries
        in the RFIDReader class, passes them into an RFIDReaderThread,
        starts the RFIDReaderThread, and initializes the interactions of this application.
        */
        public virtual void initialize(int numTags, int numModalities, ConcurrentDictionary<TState, double> initialStateDistribution, bool shouldGraph) {
            Console.WriteLine("initialize " + shouldGraph);
            this.numTags = numTags;
            this.numModalities = numModalities;

            // Initialize a thread with the delegates.
            this.readerThread = new RFIDReaderThread(numTags, this.touchDistributionDelegate,this.velocityDistributionDelegate, shouldGraph);

            // Initialize interactions.
            this.interaction = new TInteraction();
            // Remember our tagIds.
            this.tagIds = SolutionConstants.tagIds;
            // Right now, there's only one interaction, so there's just the one set of tags,
            // but we can imagine multiple interactions with multiple sets of tags.
            List<string> interactonTagIds = new List<string>();
            foreach (string tag in SolutionConstants.tagIds)
            {
                interactonTagIds.Add(tag);
            }
            this.interaction.initialize(interactonTagIds, initialStateDistribution);
            this.tagFeatureDistributions = new ConcurrentQueue<TagInputDistribution>();

            this.afterInitialize();

            // Run the thread, which begins running the RFID Reader.
            this.readerThread.run();
        }

        /*
        The run method is a loop that continuously calls the interaction's update method.
        */
        public virtual void run() {
            // We expect to get one update per tag per modality.
            int expectedQueueSize = this.numTags * this.numModalities;
            while (true) {
                // Wait for all of the filters to be queried.
                // The value of shouldReportTagDistributions is false when we have just enqueued data into
                // the tagFeatureDistributions queue, so that is the time to process the information.
                if (!RFIDReader.shouldReportTagDistributions) {
                    if (this.tagFeatureDistributions.Count == expectedQueueSize)
                    {
                        this.interaction.update(this.tagFeatureDistributions);
                        // Allow user to do something here
                        this.afterUpdate();
                        // Reset to an empty array.
                        this.tagFeatureDistributions = new ConcurrentQueue<TagInputDistribution>();

                    }
                    // Now that we've processed the data, tell the reader to send another update.
                    RFIDReader.shouldReportTagDistributions = true;
                }
            }
        }

        // To be overwritten by the user.
        // A hook into the program right after initialize and right before starting the RFIDReaderThread.
        public virtual void afterInitialize()
        {
            // Empty in the base class.
        }

        // To be overwritten by the user.
        // A hook into the program right after the interaction(s) update.
        public virtual void afterUpdate()
        {
            // Empty in the base class.
        }

        /*
        The delegate method for handling collectionChanged events on the filter for detecting touch events.
        Only called when the RFIDReader is initializing the BernoulliFilterDict.
        */
        void touchDistributionDelegate(ConcurrentDictionary<string, Bernoulli> touchDict){
            foreach (KeyValuePair<string, Bernoulli> item in touchDict) {
                TagInputDistribution tagDict = new TagInputDistribution();
                tagDict.tag = item.Key;
                tagDict.modality = Modality.touch;
                tagDict.distribution = item.Value;
                this.tagFeatureDistributions.Enqueue(tagDict);
            }
        }

        /*
        The delegate method for handling collectionChanged events on the filter for detecting velocity events.
        Only called when the RFIDReader is initializing the GaussianFilterDict.
        */
        void velocityDistributionDelegate(ConcurrentDictionary<string, Normal> velocityDict)
        {
            foreach (KeyValuePair<string, Normal> item in velocityDict)
            {
                TagInputDistribution tagDict = new TagInputDistribution();
                tagDict.tag = item.Key;
                tagDict.modality = Modality.velocity;
                tagDict.distribution = item.Value;
                this.tagFeatureDistributions.Enqueue(tagDict);
            }
            // Don't trigger another report of tag updates until the application has finished processing
            // the ones we just enqueued.
            // Only called in the velocity distribution delegate because it is called second.
            RFIDReader.shouldReportTagDistributions = false;
        }

    }

}
