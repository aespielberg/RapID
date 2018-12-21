using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;

namespace rapid.Filters {
    public abstract class Filter {

        /** Fields **/
        // The "master" version of the distribution that is only updated upon
        // a real observation.
        protected IDistribution distribution;
        // Temporary distribution used for keeping track of estimated updates
        // while we are in between observations.
        protected IDistribution tempDistribution;

        // Flag that indicates if the query function should return the master distribution
        // or the temporary distribution.
        // The temporary distribution is used to keep track of our estimated updates
        // to the distribution between actual observations.
        protected bool useMasterDistribution = true;

        // User should provide an initial distribution.
        public Filter(IDistribution distribution) {
            this.distribution = distribution;
            this.tempDistribution = distribution;
        }

        // Updates the distribution upon observing the given feature.
        // To be implemented by the derived classes.
        public abstract void updateWithFeature(double feature, double deltaT);

        // Updates the distribution between observations, where the updates
        // are estimated based on the amount of time since a reading.
        public abstract void updateWithoutFeature(double deltaT);

        // Query the filter, returns the distribution.
        public IDistribution getDistribution() {
            if (this.useMasterDistribution) {
                return this.distribution;
            } else {
                return this.tempDistribution;
            }
        }
    }
}
