using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

namespace smc {
    // Paramaterized by an InteractionState that the user will specify.
    public abstract class Interaction<TState> where TState : InteractionState {

        /************/
        /*  Fields  */
        /************/

        // The number of samples in each update loop.
        protected int numSamples;
        // The tag ids of the system.
        protected List<string> tagIds;
        // Dictionary representing our most updated distribution
        // over all possible interaction states.
        protected ConcurrentDictionary<TState, double> histogram;
        // The current most likely state.
        protected TState mostLikelyState;
        protected double mostLikelyStateProb = 0;

        /*************/
        /*  Methods  */
        /*************/

        // Constructor for this class.
        // Empty because using parameterized types makes it so the constructors must take no argument...? 
        public Interaction() {

        }

        // Caller must specify a the tagIds and the initial distribution of states for this interaction.
        public virtual void initialize(List<string> tagIds, ConcurrentDictionary<TState, double> initialHistogram) {
            this.tagIds = ObjectCopier.Clone<List<string>>(tagIds);
            this.histogram = initialHistogram;

            // Verify that the initial distribution's probabilities sum to 1
            if (!this.verifyHistogram()) {
                throw new Exception("Initial distribution doesn't sum to 1.");
            }
        }

        // Verifies the current histogram is valid (probabilities sum to 1)
        private bool verifyHistogram() {
            double probabilitySum = 0;
            foreach (double p in this.histogram.Values) {
                probabilitySum += p;
            }
            // Tolerate some floating point error.
            double errorThreshold = .0001;
            return Math.Abs(probabilitySum - 1) < errorThreshold;
        }

        // The main update loop.
        // This loop performs two sets of uniform sample, clones and updates
        // InteractionStates, and finally modifies the distributions.
        // The argument, inputDistributions, is our current best guess
        // for what the distribution of events should be.
        public virtual void update(ConcurrentQueue<TagInputDistribution> inputDistributions) {
            // This is an array of length numSamples.
            ConcurrentQueue<TState> ISsamples = Util.lowVarianceSample<TState>(this.histogram, this.numSamples);

            // Dummy variable used to store result of TryDequeue later.
            TState dummyState;

            // Sample from inputDistributions numSamples times.
            for (int i = 0; i < this.numSamples; i++) {
                // Get the array of deterministic values. The array has length 2m, where m is the number of tags.
                ConcurrentQueue<TagInputValue> tagInputValues = new ConcurrentQueue<TagInputValue>();
                foreach (TagInputDistribution tagInputDist in inputDistributions) {
                    tagInputValues.Enqueue(Util.sampleInput(tagInputDist));
                }
                // Update the interaction state.
                if (ISsamples.TryDequeue(out dummyState)) {
                    // Update and enqueue.
                    dummyState.update(tagInputValues);
                    ISsamples.Enqueue(ObjectCopier.Clone<TState>(dummyState));
                }
                
            }

            // Create new histogram to reflect this round of updates.
            // If this sample is already in the histogram, then increase the weight by 1/numSamples
            // Otherwise, create a new entry in the histogram with weight 1/numSamples
            ConcurrentDictionary<TState, double> newHistogram = new ConcurrentDictionary<TState, double>();
            this.mostLikelyStateProb = 0;
            foreach (TState state in ISsamples) {
                if (newHistogram.ContainsKey(state)) {
                    newHistogram[state] += (double) 1.0/this.numSamples;
                } else {
                    newHistogram[state] = (double) 1.0/this.numSamples;
                }
                if (newHistogram[state] > this.mostLikelyStateProb)
                {
                    this.mostLikelyState = state;
                    this.mostLikelyStateProb = newHistogram[state];
                }
            }
            this.histogram = newHistogram;
            if (! verifyHistogram()) {
                // throw new Exception("Updated histogram probabilities don't sum to 1.");
            }
            /*
            // Print out the values of the histogram.
            string s = "";
            foreach (TState state in this.histogram.Keys)
            {
                s += state.ToString() + ": " + this.histogram[state].ToString();
            }
            Console.WriteLine(s);
            */
        }


        // Return a copy of the distribution from the interaction;
        public virtual ConcurrentDictionary<TState, double> getHistogram() {
            return ObjectCopier.Clone<ConcurrentDictionary<TState, double>>(this.histogram);
        }

        // Get the most likely state if it's greater than a certain threshold value, otherwise return null.
        public virtual TState getMostLikelyStateIfProbable(double threshold)
        {
            if (this.mostLikelyStateProb > threshold)
            {
                return ObjectCopier.Clone<TState>(this.mostLikelyState);
            }
            else
            {
                return null;
            }
        }

        // Displays information about the histogram in user-readable format.
        // Should be overriden by derived classes, but not strictly necessary.
        public virtual void displayHistogram() {
           
        }
    }
}
