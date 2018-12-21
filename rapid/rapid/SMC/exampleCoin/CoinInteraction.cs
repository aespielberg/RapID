using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MathNet.Numerics.Distributions;

namespace smc {
    public class CoinInteraction<TState> : Interaction<TState> where TState : CoinInteractionState{

        // Constructor, just calls base constructor
        public CoinInteraction() : base() {
            this.numSamples = 100;
        }

        /*****************
        Note that we should be able to use the same update method as in the base Interaction class.
        *****************/

        // Convenient function to print out the average of the histogram. Will be updated to
        // do more sophisticated representation of the histogram.
        public override void displayHistogram() {
            // Print the mean.
            double average = 0.0;
            double standardDeviation = 0.0;
            foreach (KeyValuePair<TState, double> stateToProb in this.histogram) {
                average += stateToProb.Value * stateToProb.Key.getHeads();
            }
            foreach (KeyValuePair<TState, double> stateToProb in this.histogram) {
                standardDeviation += Math.Pow(stateToProb.Key.getHeads() - average, 2);
            }
            standardDeviation = Math.Sqrt(standardDeviation);

            //Console.WriteLine("Current heads (average, std dev) is (" + average + ", " + standardDeviation + ")");
            //Console.WriteLine("average: " + average);
            // TODO: use System.Windows.Forms.DataVisualization.Charting to actually do some visualization
        }

    }

}

