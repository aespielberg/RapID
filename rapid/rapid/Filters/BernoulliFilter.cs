using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;

namespace rapid.Filters {
    public class BernoulliFilter : Filter {

        // Magic numbers for P(z | x) and P(x_i | x_{i-1})
        double mu_visible, sigma_visible, mu_covered, sigma_covered;
        // Distributions over deltaT values assuming that the tag is visible or covered.
        Normal visibleDist, coveredDist;

        // Default number for the decay exponent in the transition update.
        double transitionDecayFactor = 1.0;

        // Constructor calls the base constructor,
        // and initializes magic numbers parameterized by the number of tags.
        // Note that the Bernoulli distribution uses 1 = visible, 0 = covered.
        public BernoulliFilter(Bernoulli distribution, int numTags) : base(distribution) {
            // The hardcoded constants are derived from trial and error.
            this.mu_visible = 0.004 * numTags + 0.0299;
            this.sigma_visible = 0.0013 * numTags + 0.0124;
            this.visibleDist = new Normal(mu_visible, sigma_visible);

            this.mu_covered = .1115 * numTags + 0.0300;
            this.sigma_covered = 0.1657 * numTags + 0.2093;
            this.coveredDist = new Normal(mu_covered, sigma_covered);
        }

        // The update method consists of a transition and an observation.
        // The feature is the delta time since the last successful reading.
        // In this case, deltaT is the actual feature, and the deltaT argument is unused. 
        public override void updateWithFeature(double feature, double unused) {
            // First apply the transition.
            // Pass in the feature as deltaT.
            Bernoulli transitionedDist = this.transitionUpdate(feature);

            // Then apply the the observation rule, and update this.distribution.
            this.distribution = this.observationUpdate(feature, transitionedDist);

            // When client queries, return master distribution,
            // because we just updated with an observation.
            this.useMasterDistribution = true;
        }

        // This update method is for when we haven't received data in a while.
        public override void updateWithoutFeature(double _deltaT) {
            double deltaT = _deltaT;
            // Apply the transition rule.
            Bernoulli transitionedDist = this.transitionUpdate(deltaT);

            // Reweight the transitioned distribution.
            // Use the Gaussians visibleDist and coveredDist, and integrate over them.
            // Exact equation provided in the RapID paper page 5.
            double probZGivenVisible = this.probNoTagRead(deltaT, true);
            double probZGivenCovered = this.probNoTagRead(deltaT, false);

            // Update probabilities p(x|z) = p(x_t | x_{t-1}) * p(z|x)
            double newProbVisible = transitionedDist.P * probZGivenVisible;
            double newProbCovered = (1 - transitionedDist.P) * probZGivenCovered;
            // Reweight and create new distribution, and set as tempDistribution,
            // since we are between observations.
            double p = newProbVisible / (newProbVisible + newProbCovered);
            // TODO: examine this NaN thing.
            if (double.IsNaN(p)) {
                this.tempDistribution = new Bernoulli(0);
            }
            else {
                this.tempDistribution = new Bernoulli(p);
            }

            // Set this flag so that when client queries,
            // we return the temporary distribution,
            // because we are estimating updates while between observations.
            this.useMasterDistribution = false;
        }

        /*******************/
        /* PRIVATE METHODS */
        /*******************/

        // Returns an updated Bernoulli distribution after a transition.
        private Bernoulli transitionUpdate(double deltaT) {
            if (deltaT < 0) {
                deltaT = 0.1;
            }
            // Get current visible and covered beliefs.
            double currentVisible = ((Bernoulli) this.distribution).P;
            double currentCovered = 1 - currentVisible;

            // Apply the transition update rule.
            // Details in the RapID paper pages 4-5.
            double updatedProbVisible = .5 + (1.0 / Math.Pow(2, deltaT / this.transitionDecayFactor) * (1 - .5));

            Bernoulli updatedDist = new Bernoulli(updatedProbVisible);
            return updatedDist;
        }

        // Returns an updated Bernoulli distribution after an observation,
        // given a deltaT observation and a prior distribution.
        private Bernoulli observationUpdate(double _deltaT, Bernoulli prior) {
            double deltaT = _deltaT;
            // Renormalize because we only care about the part of the Gaussian that is greater than 0.
            double massLessThanZero = this.visibleDist.CumulativeDistribution(0);
            double probZGivenVisible= this.visibleDist.Density(deltaT) / (1 - massLessThanZero);
            massLessThanZero = this.coveredDist.CumulativeDistribution(0);
            double probZGivenCovered= this.coveredDist.Density(deltaT) / (1 - massLessThanZero);

            // Reweight.
            double updatedVisibleProb = prior.P * probZGivenVisible;
            double updatedCoveredProb = (1 - prior.P) * probZGivenCovered;
            // Normalize and return new distribution
            double p = updatedVisibleProb / (updatedVisibleProb + updatedCoveredProb);
            // HACKHACKHACK
            if (double.IsNaN(p)) {
                Console.WriteLine("hacking in observation update\n");

                return new Bernoulli(0);
            }
            else {
                return new Bernoulli(p);
            }
        }

        // Helper function that calculates the probability of no tag read for a given 
        // amount of time T.
        // Calculates 1 - (integral from 0 to T of gaussian+),
        // where gaussian+ is when we consider only the positive values in the Gaussian
        // distribution and then renormalize.
        private double probNoTagRead(double T, bool visible) {
            Normal dist = this.coveredDist;
            if (visible) {
                dist = this.visibleDist;
            }

            return 1.0 - (dist.CumulativeDistribution(T) - dist.CumulativeDistribution(0)) / (1.0 - dist.CumulativeDistribution(0));
        }
    }
}
