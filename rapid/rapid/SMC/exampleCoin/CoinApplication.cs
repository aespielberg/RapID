using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using System.Collections.Specialized;

namespace smc {
    public class CoinApplication<TState, TInteraction> : RFIDApplication<TState, TInteraction>
                                                        where TInteraction : CoinInteraction<TState>, new()
                                                        where TState : CoinInteractionState {

        // Which state we desire to be in.
        CoinInteractionState desiredState;
        CoinInteractionState mostLikelyState;
        // Constructor, calls base constructor then initializes the TagInputDistribution queue.
        public CoinApplication() : base() {

        }

        /*****************
        Note that we don't need to implement initialize since we'll just use
        the base class' implementation.
         *****************/
        
        private void setDesiredStateToTouched()
        {
            // Randomly select which tag we want to be touched next.
            // In this simple application, there is only one tag.
            this.desiredState = new CoinInteractionState();
            this.desiredState.setHeads(0.0);
            Console.WriteLine("Simon says: Touch the tag!");
        }

        private void setDesiredStateToUntouched()
        {
            this.desiredState = new CoinInteractionState();
            this.desiredState.setHeads(1.0);
            Console.WriteLine("Simon says: Don't touch the tag!");
        }
        
        public override void afterInitialize()
        {
            Console.WriteLine("afterInitialize");
            base.afterInitialize();
            this.setDesiredStateToTouched();
        }

        public override void afterUpdate()
        {
            // Report the status of the tag (covered/uncovered) if we are more than 95% sure.
            this.mostLikelyState = (CoinInteractionState) this.interaction.getMostLikelyStateIfProbable(.95);
            if (this.mostLikelyState == null)
            {
                return;
            }
            if (this.mostLikelyState.Equals(this.desiredState))
            {
                Console.WriteLine("Good job!");
                if (this.desiredState.getHeads() == 1.0)
                {
                    this.setDesiredStateToTouched();
                }
                else
                {
                    this.setDesiredStateToUntouched();
                }
            }
            this.interaction.displayHistogram();
        }

        // Helper to print the state the application thinks is the most likely.
        void printState()
        {
            if (object.ReferenceEquals(null, this.mostLikelyState))
            {
                Console.WriteLine("inconclusive");
            }
            else if (this.mostLikelyState.getHeads() == 1.0)
            {
                Console.WriteLine("visible");
            }
            else
            {
                Console.WriteLine("covered");
            }
        }

    }

}
