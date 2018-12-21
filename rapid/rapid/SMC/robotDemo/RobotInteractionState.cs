using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace smc
{
  [Serializable]
  public class RobotInteractionState : InteractionState
  {

    private ConcurrentDictionary<string, Touch> touchDict;

    public RobotInteractionState()
    {
        // Does nothing.
    }

    public void initialize(List<string> tagIds)
    {
        // Assume all are untouched.
        foreach (string tag in tagIds)
        {
        this.touchDict[tag] = Touch.untouched;
        }
    }

    public Touch getTouch(string tagId)
    {
        if (this.touchDict.ContainsKey(tagId))
        {
        return this.touchDict[tagId];
        }
        else
        {
        throw new Exception("Cannot get state of unknown tag: " + tagId);
        }
    }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }


  }
}
