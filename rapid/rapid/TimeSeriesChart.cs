using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;
namespace rapid
{
    public class TimeSeriesChart : Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart;
        // Map string labels to a series of points that will show up on the chart.
        private ConcurrentDictionary<string, System.Windows.Forms.DataVisualization.Charting.Series> labelToSeriesDict;
        private Random r = new Random();
        // Delegates to be invoked for safe cross-thread access of form.
        private delegate void ChartSeriesAddDelegate(Series series);
        private delegate int SeriesPointsAddDelegate(double x, double y);
        // Only set to true after form has loaded.
        // Prevents null reference errors when other threads access the form prematurely.
        private bool ready = false;

        // Constructor just needs to call initialize, which will set up the chart and the form.
        public TimeSeriesChart()
        {
            InitializeComponent();

        }

        // To be called by other threads, they should wait until the form has loaded.
        public bool isReady()
        {
            return this.ready;
        }

        private void LoadForm(object sender, EventArgs e)
        {
            this.labelToSeriesDict = new ConcurrentDictionary<string, System.Windows.Forms.DataVisualization.Charting.Series>();
            chart.Series.Clear();
            this.ready = true;
        }

        // Adds a point to the chart for the given label and (x, y) values.
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void addPoint(string label, double x, double y)
        {
            if (!labelToSeriesDict.ContainsKey(label))
            {
                // Create a new series if we haven't seen this label before.
                this.labelToSeriesDict[label] = new System.Windows.Forms.DataVisualization.Charting.Series
                {
                    Name = label,
                    Color = System.Drawing.Color.FromArgb((int)(r.NextDouble() * 255), (int)(r.NextDouble() * 255), (int)(r.NextDouble() * 255)),
                    IsVisibleInLegend = true,
                    IsXValueIndexed = false,
                    ChartType = SeriesChartType.Line
                };

                // InvokeRequired required compares the thread ID of the calling thread to the thread ID of
                // the creating thread. If these threads are different, it returns true.
                if (this.chart.InvokeRequired)
                {
                    ChartSeriesAddDelegate d = new ChartSeriesAddDelegate(this.chart.Series.Add);
                    this.Invoke(d, new object[] { this.labelToSeriesDict[label] });
                }
                else
                {
                    this.chart.Series.Add(this.labelToSeriesDict[label]);
                }
            }

            // Again, need to check InvokeRequired, because we might need to make a delegate and use Invoke.
            // This is apparently the appropriate way to make cross-thread calls to a form object.
            if (this.chart.InvokeRequired)
            {
                SeriesPointsAddDelegate d = new SeriesPointsAddDelegate(this.labelToSeriesDict[label].Points.AddXY);
                this.Invoke(d, new object[] { x, y });
            }
            else
            {
                this.labelToSeriesDict[label].Points.AddXY(x, y);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).BeginInit();
            this.SuspendLayout();

            // Set up the chart.
            chartArea.Name = "RapidChartArea";
            this.chart.ChartAreas.Add(chartArea);
            this.chart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend.Name = "Legend";
            this.chart.Legends.Add(legend);
            this.chart.Location = new System.Drawing.Point(0, 50);
            this.chart.Name = "chart";
            // this.chart1.Size = new System.Drawing.Size(284, 212);
            this.chart.TabIndex = 0;
            this.chart.Text = "chart";

            // Set up the whole form.
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.chart);
            this.Name = "RapidForm";
            this.Text = "Tag Data Chart";
            this.Load += new System.EventHandler(this.LoadForm);
            ((System.ComponentModel.ISupportInitialize)(this.chart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}

