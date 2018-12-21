using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RFID;
using MathNet.Numerics.Distributions;

namespace rapid.Filters {
    public class GaussianFilter : Filter {

        // Constructor just calls the base constructor
        public GaussianFilter(Normal distribution) : base (distribution) {}

        // Update method.
        // The feature is a delta velocity value from the reader.
        public override void updateWithFeature(double feature, double deltaT) {
            // First do the transition update.
            this.transitionUpdate(deltaT);
            this.observationUpdate(feature);

        }

        // Update when we haven't received a new reading from this tag in deltaT time.
        public override void updateWithoutFeature(double deltaT) {
            this.transitionUpdate(deltaT);
        }

        private void transitionUpdate(double deltaT)
        {
            Normal currentDist = (Normal)this.getDistribution();
            // Mean stays the same, but variance increases.
            double newMean = currentDist.Mean;
            double newVariance = currentDist.Variance + SolutionConstants.GaussianVarianceScalingFactor * deltaT;
            // Console.WriteLine("new variance " + newVariance);

            this.distribution = new Normal(newMean, Math.Sqrt(newVariance));
        }

        private void observationUpdate(double feature) {
            Normal currentDist = (Normal)this.distribution;
            double u1 = currentDist.Mean;
            double v1 = currentDist.Variance;

            // Get p(z | x) which is a Gaussian with mean = feature, and variance = .01/pi^2
            double u2 = feature;
            double v2 = .01 / (Math.Pow(Math.PI, 2));

            // The new distribution after the update will be currentDist * p(z | x)
            // We achieve this by calculating the new mean, new variance,
            // and a scaling factor to keep things normalized.
            // From https://www.cs.nyu.edu/~roweis/notes/gaussid.pdf.
            double v = 1.0 / ((1.0 / v1) + (1.0 / v2));
            double u = (u1 / v1 + u2 / v2) * v;

            Normal newDist = new Normal(u, Math.Sqrt(v));
            this.distribution = newDist;
        }

    }
}
