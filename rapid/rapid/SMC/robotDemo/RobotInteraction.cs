using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace smc
{
  public class RobotInteraction<TState> : Interaction<TState> where TState : RobotInteractionState
  {
    public RobotInteraction() : base()
    {
      this.numSamples = 100;
    }

    public override void initialize(List<string> tagIds, ConcurrentDictionary<TState, double> initialHistogram)
    {
            base.initialize(tagIds, initialHistogram);
      // Initialize the interaction states with the given tagIds.
      foreach (KeyValuePair<TState, double> state in this.histogram)
      {
        state.Key.initialize(tagIds);
      }
    }

    // Can override if we want to...
    public override void displayHistogram()
    {
            base.displayHistogram();
    }
  }
}
