using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace smc
{
    // Keeps track of the immediate state for each tag
    [Serializable]
    public class SimonInteractionState : InteractionState
    {

        // Dictionary mapping a tagId to a touch/untouch state.
        private ConcurrentDictionary<string, Touch> touchDict = new ConcurrentDictionary<string, Touch>();

        // Dictionary mapping a tagId to one of three motion states: up, down, still.
        private ConcurrentDictionary<string, Motion> motionDict = new ConcurrentDictionary<string, Motion>();

        // Keep track of if we were last up or down. Not included in Equals comparison, just for book keeping.
        private ConcurrentDictionary<string, bool> lastUpDict = new ConcurrentDictionary<string, bool>();

        // Constructor, does nothing.
        public SimonInteractionState()
        {
            // Initialization is handled by the initialize method, not here.
        }

        // Initialize dictionaries to default states for the given tagIds.
        // Default state means untouched and with velocity 0.
        public void initialize(List<string> tagIds)
        {
            foreach (string tagId in tagIds)
            {
                this.touchDict[tagId] = Touch.untouched;
                this.motionDict[tagId] = Motion.still;
                this.lastUpDict[tagId] = false;
            }
        }

        // Set the touch state of a tag.
        public void setTagTouchState(string tagId, Touch state)
        {
            this.touchDict[tagId] = state;
        }

        // Get the touch state of a tag.
        public Touch getTagTouchState(string tagId)
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

        // Get list of all touched tags.
        public List<string> getTouchedTags()
        {
            List<string> touchedTags = new List<string>();
            foreach (var tag in this.touchDict)
            {
                if (tag.Value == Touch.touched)
                {
                    touchedTags.Add(tag.Key);
                }
            }
            return touchedTags;
        }

        // Set the motion state of a tag.
        public void setTagMotionState(string tagId, Motion state)
        {
            this.motionDict[tagId] = state;
        }

        // Get the motion state of a tag.
        public Motion getTagMotionState(string tagId)
        {
            if (this.motionDict.ContainsKey(tagId))
            {
                return this.motionDict[tagId];
            }
            else
            {
                throw new Exception("Cannot get state of unknown tag: " + tagId);
            }
        }

        // Get whether or not we believe the tag is elevated.
        public bool getTagUp(string tagId)
        {
            if (this.lastUpDict.ContainsKey(tagId))
            {
                return this.lastUpDict[tagId];
            }
            else
            {
                throw new Exception("Cannot get state of unknown tag: " + tagId);
            }
        }

        // Update our state based on the observed values.
        public override void update(ConcurrentQueue<TagInputValue> inputValues)
        {
            foreach (TagInputValue sample in inputValues)
            {
                // The samples are either for touch or motion features.
                if (sample.modality == Modality.touch)
                {
                    // Console.WriteLine(sample.value);
                    if (!this.touchDict.ContainsKey(sample.tag))
                    {
                        throw new Exception("Unknown tag in SimonInteractionState update: " + sample.tag);
                    }
                    // A value of 1 means untouched (visible), a value of 0 means touched (not visible).
                    if (sample.value == 1)
                    {
                        this.touchDict[sample.tag] = Touch.untouched;
                    } else if (sample.value == 0)
                    {
                        this.touchDict[sample.tag] = Touch.touched;
                    } else
                    {
                        throw new Exception("received impossible value for touch/untouch sample");
                    }
                    
                    // this.touchDict[sample.tag] = sample.value == 0 ? Touch.touched : Touch.untouched;
                } else if (sample.modality == Modality.velocity)
                {
                    // Console.WriteLine(sample.value);
                    if (!this.motionDict.ContainsKey(sample.tag))
                    {
                        throw new Exception("Unknown tag in SimonInteractionState update: " + sample.tag);
                    }
                    // Determine whether or not the tag is moving up, down, or not moving at all.
                    // Consider the magnitude and parity of the value we received.
                    // Console.WriteLine(sample.value);
                    if (Math.Abs(sample.value) < .3)
                    {
                        this.motionDict[sample.tag] = Motion.still;
                        // Console.WriteLine("still");
                    } else if (sample.value > 0)
                    {
                        this.motionDict[sample.tag] = Motion.up;
                        this.lastUpDict[sample.tag] = true;
                        // Console.WriteLine("up");
                    }
                    else
                    {
                        this.motionDict[sample.tag] = Motion.down;
                        this.lastUpDict[sample.tag] = false;
                        // Console.WriteLine("down");
                    }
                }
                // If tag is covered, don't bother with velocity, assume tag is on the table.
                if (this.touchDict[sample.tag] == Touch.touched)
                {
                    // Console.WriteLine("tag covered, ignore velocity " + this.touchDict[sample.tag].ToString());
                    this.motionDict[sample.tag] = Motion.still;
                    this.lastUpDict[sample.tag] = false;
                }
            }
            // Console.WriteLine(this);
        }

        public override string ToString()
        {
            string s = "";
            foreach (string tag in this.touchDict.Keys)
            {
                s += tag + " " + touchDict[tag].ToString() + " " + motionDict[tag].ToString() + "\n";
            }
            return s;
        }

        // Need to override GetHashCode for identification in collections like ConcurrentDictionary.
        public override int GetHashCode()
        {
            double hash = 0;
            int count = 1;
            foreach (string tag in this.touchDict.Keys)
            {
                hash += Math.Pow(count, (double)touchDict[tag]);
                hash += Math.Pow(count, (double)motionDict[tag]);
                count++;
            }
            return (int)hash;
        }

        // Need to override Equals method.
        public override bool Equals(Object obj)
        {
            // Check for null values and compare runtime types.
            if (obj == null || this.GetType() != obj.GetType())
                return false;

            SimonInteractionState state = (SimonInteractionState)obj;

            // Compare hashes.
            if (!(this.GetHashCode() == state.GetHashCode()))
            {
                return false;
            }
            // Compare touchDict.
            if (this.touchDict.Count != state.touchDict.Count)
            {
                return false;
            }
            foreach (var pair in this.touchDict)
            {
                if (!state.touchDict.ContainsKey(pair.Key))
                {
                    return false;
                }
                if (!state.touchDict[pair.Key].Equals(pair.Value))
                {
                    return false;
                }
            }
            foreach (string tag in state.touchDict.Keys)
            {
                if (!this.touchDict.ContainsKey(tag))
                {
                    return false;
                }     
            }

            // Compare motionDict.
            if (this.motionDict.Count != state.motionDict.Count)
            {
                return false;
            }
            foreach (var pair in this.motionDict)
            {
                if (!state.motionDict.ContainsKey(pair.Key))
                {
                    return false;
                }
                if (!state.motionDict[pair.Key].Equals(pair.Value))
                {
                    return false;
                }
            }
            foreach (string tag in state.motionDict.Keys)
            {
                if (!this.motionDict.ContainsKey(tag))
                {
                    return false;
                }
            }
            return true;
        }
    }

}

