using MathNet.Numerics.Distributions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using RFID;

namespace smc
{
    public class SimonApplication<TState, TInteraction> : RFIDApplication<TState, TInteraction> where TInteraction : SimonInteraction<TState>, new() where TState : SimonInteractionState
    {

        private SimonInteractionState desiredState;
        private SimonInteractionState mostLikelyState;
        private SimonInteractionState lastMostLikelyState;
        private Random random = new Random();
        private double snapThreshold = .95; // How certain we must be in a state in order to consider ourselves in that state.
        private SimonInteractionState lastObservedState; // The most recent state we snapped to.

        // private SimonForm form; // Use form.markTagDesired(tag) and form.markAllUntouched when GUI is finished.

        // Constructor, calls base constructor then initializes the TagInputDistribution queue.
        public SimonApplication() : base()
        {

        }

        private void initialize()
        {
            // Create and run the GUI.
            // Initialize it with a dictionary mapping tag IDs to their display labels.

/*
            Dictionary<string, string> tagIdsToLabels = new Dictionary<string, string>();
            Debug.Assert(this.tagIds.Count == SolutionConstants.tagLabels.Count);
            for (int i = 0; i < this.tagIds.Count; i++)
            {
                tagIdsToLabels.Add(this.tagIds[i], SolutionConstants.tagLabels[i]);
            }
            this.form = new SimonForm(tagIdsToLabels);
            Application.Run(this.form);
*/
        }

        // Given a tag, chooses a plausible action to ask the player to perform.
        // Modifies this.desiredState and returns a string with instructions to display.
        private string requestNewTagAction(string tagId)
        {
            Console.WriteLine("requesting new tag action, current state: " + this.lastObservedState);
            // Tag is untouched.
            if (this.lastObservedState.getTagTouchState(tagId) == Touch.untouched) {
                if (this.lastObservedState.getTagMotionState(tagId) == Motion.still)
                {
                    // If tag is in the up state, ask to move it back down.
                    if (this.lastObservedState.getTagUp(tagId))
                    {
                        this.desiredState.setTagMotionState(tagId, Motion.down);
                        return "Lower tag " + tagId;
                    }
                    // If tag is the down state, either ask for touch or move up, with equal probability.
                    if (random.Next(2) == 0)
                    {
                        this.desiredState.setTagTouchState(tagId, Touch.touched);
                        return "Cover tag " + tagId;
                    }
                    else
                    {
                        this.desiredState.setTagMotionState(tagId, Motion.up);
                        return "Move tag " + tagId + " up";
                    }
                } else
                {
                    // It's already moving, so we can ask for it to stop?
                    this.desiredState.setTagMotionState(tagId, Motion.still);
                    return "Stop moving tag " + tagId;
                }
            } else
            {
                // Tag is touched, so only possible option is to ask for it to be untouched.
                this.desiredState.setTagTouchState(tagId, Touch.untouched);
                return "Uncover tag " + tagId;
            }
        }

        private void requestNewState()
        {
            // Select some number of the tags.
            HashSet<string> desiredTags = new HashSet<string>();
            desiredTags.Add(this.tagIds[random.Next(this.tagIds.Count)]);
            foreach (string tag in this.tagIds)
            {
                if (random.Next(2) == 0)
                {
                    desiredTags.Add(tag);
                }
            }
            // For each selected tag, ask the user to do something to the tag,
            // touch or move, but must be consistent with the current state of the tag.
            // Our new desired state will start as the last observed state and then we will make modifications.
            this.desiredState = (ObjectCopier.Clone<SimonInteractionState>(this.lastObservedState));
            string instructions = "Simon says:\n";
            foreach( string tag in desiredTags)
            {
                instructions += "\t" + this.requestNewTagAction(tag) + "\n";
            }
            Console.WriteLine(instructions + " desired state: " + this.desiredState.ToString());
        }

        public override void afterInitialize()
        {
            base.afterInitialize();
            this.lastMostLikelyState = new SimonInteractionState();
            this.lastMostLikelyState.initialize(this.tagIds);
            this.lastObservedState = new SimonInteractionState();
            this.lastObservedState.initialize(this.tagIds);
            this.requestNewState();
        }

        public override void afterUpdate()
        {
            // Check the most likely state.
            this.mostLikelyState = (SimonInteractionState)this.interaction.getMostLikelyStateIfProbable(this.snapThreshold);
            if (this.mostLikelyState == null)
            {
                return;
            }

            /*
            // Update the state of the GUI to reflect which tags were touched.
            List<string> desiredTouchedTags = this.desiredState.getTouchedTags();
            bool error = false;
            foreach (string touchedTag in this.mostLikelyState.getTouchedTags())
            {
                if (desiredTouchedTags.Contains(touchedTag)) {
                    Console.WriteLine("Yay!");
                    this.form.markTagCorrect(touchedTag);
                }
                else
                {
                    Console.WriteLine("Mistake...");
                    this.form.markTagMistake(touchedTag);
                    error = true;
                }
            }
            if (error)
            {
                // They lose, and it automatically starts again.
                Console.WriteLine("You lose!");
                this.requestTagTouch();
            }
            */

            // Check if we are in a new state, and then check if player followed Simon's instructions correctly.
            // Console.WriteLine("most likely state exists " + this.mostLikelyState);
            // Console.WriteLine(this.mostLikelyState);
            if (!this.mostLikelyState.Equals(this.lastObservedState))
            {
                Console.WriteLine("noticed state change to " + this.mostLikelyState);
                this.lastObservedState = ObjectCopier.Clone<SimonInteractionState>(this.mostLikelyState);
                if (this.mostLikelyState.Equals(this.desiredState))
                {
                    // Player is correct.
                    Console.WriteLine("Well done! Score ");
                }
                else
                {
                    // Player is incorrect.
                    Console.WriteLine("Incorrect. Simon is disappointed.");
                }
                this.requestNewState();           
            }
        }

    }

}
