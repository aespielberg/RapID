using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MathNet.Numerics.Distributions;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.Linq;


namespace smc
{
    // Creates the overall histogram based on the changing tag values
    public class SimonInteraction<TState> : Interaction<TState> where TState : SimonInteractionState
    {

        // Constructor, just calls base constructor
        public SimonInteraction() : base()
        {
            this.numSamples = 500; 
        }

        public override void initialize(List<string> tagIds, ConcurrentDictionary<TState, double> initialHistogram)
        {
            base.initialize(tagIds, initialHistogram);
            foreach (KeyValuePair<TState, double> state in this.histogram)
            {
                // to initialize each interaction state's dictionary
                state.Key.initialize(tagIds);
            }

        }

        // Convenient function to print out the average of the histogram. Will be updated to
        // do more sophisticated representation of the histogram.
        // are we supposed to display all possible combos of tags + tag states? (8^4 combinations)
        // currently we're displaying top 10 states
        public override void displayHistogram()
        {
            // For now, do nothing.
            return;
            //initialize chart -- TODO: move this code outside so only happens once
            var chart = new Chart();

            var chartArea = new ChartArea();
            chartArea.Name = "Histogram";
            chart.ChartAreas.Add(chartArea);
            chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

            Series series = new Series
            {
                Name = "series2",
                ChartType = SeriesChartType.Column
            };
            chart.Series.Add(series);


            //List<KeyValuePair<TState, double>> myList = this.histogram.ToList();
            //myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value)); // is this sorted big to small?
            // only display top 10 values
            var histSorted = this.histogram.OrderByDescending(key => key.Value).ToList();
            for(int i = 0; i < 10; i++)
            {
                KeyValuePair<TState, double> stateToProb = histSorted[i];
                double probability = stateToProb.Value;
                series.Points.Add(probability);
                var p1 = series.Points[i];
                p1.AxisLabel = "hi"; //TODO: make it tag and state
                p1.Label = System.Convert.ToString(probability);
            }

            //adds all values to chart
            chart.Invalidate();
        }
    }
}

