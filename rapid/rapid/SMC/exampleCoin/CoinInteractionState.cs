using System;
using System.Collections.Concurrent;

namespace smc {

    [Serializable]
    public class CoinInteractionState : InteractionState {

        // Keep track of the number of heads seen so far, and the total number of coins seen so far.
        private double heads;

        // Constructor, just initializes number of heads to 0.
        public CoinInteractionState() {
            this.heads = 0.0;
        }

        // Set the number of heads.
        public void setHeads(double heads)
        {
            this.heads = heads;
        }

        // Get the number of heads seen so far.
        public double getHeads() {
            return this.heads;
        }

        public override string ToString()
        {
            if (this.heads == 0)
            {
                return "touched";
            } else
            {
                return "untouched";
            }
                
        }

        // Need to override GetHashCode for identification in collections like ConcurrentDictionary
        public override int GetHashCode() {
            return (int) (this.heads);
        }

        // Need to override Equals method.
        public override bool Equals(Object obj) {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            CoinInteractionState state = (CoinInteractionState)obj;
            return (this.heads == state.heads);
        }

        // Expect that inputValues is an array of length 1,
        // where the single element has a value of
        // either 1 or 0, for the number of heads that appeared.
        public override void update(ConcurrentQueue<TagInputValue> inputValues) {
            // Update the number of heads.
            foreach (TagInputValue sample in inputValues) {
                // Only count samples if their modality is touch.
                // Here, sample.value is either 1 or 0, so we just add that count to heads.
                if (sample.modality == Modality.touch)
                {
                    this.heads = sample.value;
                }
            }

        }

    }

}

