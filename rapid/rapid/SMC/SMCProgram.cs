using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using RFID;

namespace smc {
    class SMCProgram {
        public static void SMCMain(string[] args) {
            // coinTest();
            simonTest();
            
        }

        static void coinTest() {
            // Create new app
            CoinApplication<CoinInteractionState, CoinInteraction<CoinInteractionState>> app = new CoinApplication<CoinInteractionState, CoinInteraction<CoinInteractionState>> ();

            // Create an initial histogram (distribution).
            ConcurrentDictionary<CoinInteractionState, double> initialHistogram = new ConcurrentDictionary<CoinInteractionState, double>();
            // Create an initial state, with number of heads set to 0.
            CoinInteractionState initialState = new CoinInteractionState();
            // Set the distribution to have only one state, with probability 1.
            initialHistogram.AddOrUpdate(initialState, 1.0, (key, oldValue) => 1.0);

            // Initialize app with the initial histogram.
            // Pass true for shouldGraph so that we get the realtime graphing.
            app.initialize(SolutionConstants.tagIds.Count, SolutionConstants.numModalities, initialHistogram, false);

            // Run the application.
            app.run();

            // Wait endlessly for user to exit, so that console output doesn't disappear.
            while (true) { }
        }

        static void simonTest()
        {
            // Create the app.
            SimonApplication<SimonInteractionState, SimonInteraction<SimonInteractionState>> app = new SimonApplication<SimonInteractionState, SimonInteraction<SimonInteractionState>>();
            // Create the initial histogram.
            ConcurrentDictionary<SimonInteractionState, double> initialHistogram = new ConcurrentDictionary<SimonInteractionState, double>();
            // Create an initial state, with no touches.
            SimonInteractionState initialState = new SimonInteractionState();
            initialHistogram.AddOrUpdate(initialState, 1.0, (key, oldValue) => 1.0);

            // For now, we have just one tag, and we have only touch and velocity.
            int numTags = SolutionConstants.tagIds.Count;
            int numModalities = SolutionConstants.numModalities;
            // Initialize app.
            app.initialize(numTags, numModalities, initialHistogram, false);
            // Run app.
            app.run();
            // Spin.
            while (true) { }
        }

        static void testSimonInteractionStateEquals()
        {
            SimonInteractionState state1 = new SimonInteractionState();
            SimonInteractionState state2 = new SimonInteractionState();
            state1.setTagTouchState("tag1", Touch.touched);
            Console.WriteLine(state1.Equals(state2));
            Console.WriteLine(state2.Equals(state1));

            state2.setTagTouchState("tag1", Touch.touched);
            Console.WriteLine(state1.Equals(state2));
            Console.WriteLine(state2.Equals(state1));

            state2.setTagTouchState("tag1", Touch.untouched);
            Console.WriteLine(state1.Equals(state2));
            Console.WriteLine(state2.Equals(state1));

            state1.setTagTouchState("tag2", Touch.touched);
            state1.setTagTouchState("tag1", Touch.untouched);
            state2.setTagTouchState("tag2", Touch.touched);
            Console.WriteLine(state1.Equals(state2));
            Console.WriteLine(state2.Equals(state1));

            while (true) { }
        }
    }
}
