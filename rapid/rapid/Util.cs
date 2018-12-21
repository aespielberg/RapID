using MathNet.Numerics.Distributions;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

using smc;

/* Structs and Enums */

// Defines the type of data we are getting from the RFID tag.
public enum Modality {
    touch, velocity, proximity
};

// Binary state for touched or untouched tags.
public enum Touch
{
    touched, untouched
};   

// Ternary state for moving up, moving down, or staying still.
public enum Motion
{
    still, up, down
};

// Represents the distribution of a the probabilistic value of
// a modality of an RFID tag.
public struct TagInputDistribution {
    public string tag;
    public Modality modality;
    public IDistribution distribution;
};


// Represents a deterministic, sampled value of
// a modality of an RFID tag.
public struct TagInputValue {
    public string tag;
    public Modality modality;
    public double value;
};

/* Lock for ensuring the application sends a batch of updated tagFeatureDistributions
 * to the interactions before the RFIDReaderThread triggers more updates to the
 * observable concurrent dictionaries.
*/
public static class TagLock
{
    public static Object tagLock = new object();
}

/* This class contains utility methods. */
public class Util {

    // Return n uniformly sampled samples from the given distribution.
    // InteractionStates are cloned before being returned.
    // Pseudocode taken from Probabilistic Robotics chapter 4 pg. 86
    public static ConcurrentQueue<T> lowVarianceSample<T>(ConcurrentDictionary<T, double> distribution, int n) {
        // Convert the dictionary into an array.
        KeyValuePair<T, double>[] keyvalues = distribution.ToArray();
        // Create the empty ConcurrentQueue to store the samples we will produce.
        ConcurrentQueue<T> samples = new ConcurrentQueue<T>();
        Random random = new Random();
        // Find a random number in the range [0, n-1] inclusive.
        double r = random.NextDouble()*1.0/n;
        // Initialize a counter to 0.
        int i = 0;
        // Grab the associated value at the starting index.
        double c = keyvalues[i].Value;
        // For each of the n samples we want to get:
        for (int m = 0; m < n; m++) {
            double u = r + (m) * (1.0/n);
            while (u > c) {
                i += 1;
                c += keyvalues[i].Value;
            }
            // Add the InteractionState at index i to our samples array.
            samples.Enqueue(ObjectCopier.Clone<T>(keyvalues[i].Key));
        }
        return samples;
    }

    // Samples a TagInputDistribution and returns a TagInputValue with the sampled value.
    public static TagInputValue sampleInput(TagInputDistribution tagInputDist) {
        // Sample the IDistribution.
        // Note that only the IContinuousDistribution and IDiscreteDistribution have a Sample function,
        // and that the IDistribution by itself does not include a Sample function in the interface.
        double sampledValue;
        if (tagInputDist.distribution is IDiscreteDistribution) {
            sampledValue = ((IDiscreteDistribution) tagInputDist.distribution).Sample();
        } else if (tagInputDist.distribution is IContinuousDistribution) {
            sampledValue = ((IContinuousDistribution)tagInputDist.distribution).Sample();
        } else {
            throw new Exception("tag distribution is not IDiscreteDistribution or IContinuousDistribution.");
        }
  
        // Create a new TagInputValue and populate its fields.
        TagInputValue tagInputValue;
        tagInputValue.tag = tagInputDist.tag;
        tagInputValue.modality = tagInputDist.modality;
        tagInputValue.value = sampledValue;
        return tagInputValue;
    }

    // Clone a dictionary mapping strings to Bernoulli distributions.
    public static ConcurrentDictionary<string, Bernoulli> cloneBernoulliDict(ConcurrentDictionary<string, Bernoulli> input)
    {
        ConcurrentDictionary<string, Bernoulli> output = new ConcurrentDictionary<string, Bernoulli> ();
        foreach (var pair in input)
        {
            output.TryAdd(pair.Key, new Bernoulli(pair.Value.P));
        }
        return output;
    }

    // Clone a dictionary mapping strings to Normal distributions.
    public static ConcurrentDictionary<string, Normal> cloneNormalDict(ConcurrentDictionary<string, Normal> input)
    {
        ConcurrentDictionary<string, Normal> output = new ConcurrentDictionary<string, Normal> ();
        foreach (var pair in input)
        {
            output.TryAdd(pair.Key, new Normal(pair.Value.Mean, pair.Value.StdDev));
        }
        return output;
    }

}

