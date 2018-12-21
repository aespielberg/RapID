using System.Collections.Concurrent;
using System;

namespace smc {

    [Serializable]
    public abstract class InteractionState {

        /*
        Constructor for this class.
        */
        protected InteractionState() {
            // Does nothing.
            // User should define all fields and additional methods.
        }

        /*
        Takes in a deterministic state and updates the
        interaction state based on user-defined behavior.
        */
        public virtual void update(ConcurrentQueue<TagInputValue> inputValues) {
            // User defined behavior.
        }

        // Force derived classes to override the Equals and GetHashCode methods, 
        // so that the interaction states are properly serializable and workable with ConcurrentDictoinary.
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }
}