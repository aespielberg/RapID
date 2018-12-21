using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

namespace smc {
    public class SimonForm : Form
    {
        // Map tag IDs to names and map tag IDs to rectangle objects.
        private Dictionary<string, string> tagLabels = new Dictionary<string, string>();
        private Dictionary<string, Rectangle> rectangles = new Dictionary<string, Rectangle> ();

        private Graphics graphics;
        private Pen pen = new Pen(Color.AliceBlue);
        private Brush touchBrush, desiredBrush, untouchBrush, mistakeBrush, tagLabelBrush;
        private Font tagLabelFont;
        private StringFormat centerStringFormat = new StringFormat();

        private int rectangleWidth = 100;

        // Takes in a dictionary mapping a tag epc to the display name of that tag.
        public SimonForm(Dictionary<string, string> tagIdsToLabel)
        {
            this.initializeDrawingProperties();
            this.initializeTagRectangles(tagIdsToLabel);
            
        }

        private void initializeDrawingProperties()
        {
            this.BackColor = Color.Black;

            // Set desired colors for touched and untouched states, and for when we want the player to touch the tag.
            this.touchBrush = new SolidBrush(Color.LawnGreen);
            this.desiredBrush = new SolidBrush(Color.ForestGreen);
            this.untouchBrush = new SolidBrush(Color.DeepSkyBlue);
            this.mistakeBrush = new SolidBrush(Color.Red);
            this.tagLabelBrush = new SolidBrush(Color.White);

            this.tagLabelFont = new Font("Arial", 16);

            this.centerStringFormat.Alignment = StringAlignment.Center;
            this.centerStringFormat.LineAlignment = StringAlignment.Center;

            this.graphics = this.CreateGraphics();
        }

        private void initializeTagRectangles(Dictionary<string, string> tagIdsToLabel)
        {
            // Initialize graphics, one rectangle per tag.
            // TODO: intelligently lay out the rectangles in the window.
            // For now, put them in a line.
            int count = 0;
            int spacing = 50;
            foreach (var tag in tagIdsToLabel)
            {
                rectangles.Add(tag.Key, new Rectangle(50 + count * (this.rectangleWidth + spacing), (this.Height - this.rectangleWidth) / 2, this.rectangleWidth, this.rectangleWidth));
                this.markTagUntouched(tag.Key);
                this.graphics.DrawString(tag.Value, this.tagLabelFont, this.tagLabelBrush, this.rectangles[tag.Key], this.centerStringFormat);
                count++;
            }
        }

        // Mark a tag so that the player knows it should be untouched.
        public void markTagUntouched(string tagId)
        {
            this.graphics.FillRectangle(this.untouchBrush, this.rectangles[tagId]);
        }

        // Mark a tag so that the player knows it should be touched.
        public void markTagDesired(string tagId)
        {
            this.graphics.FillRectangle(this.desiredBrush, this.rectangles[tagId]);
        }

        // Mark a tag so that the player knows they correctly touched a tag.
        public void markTagCorrect(string tagId)
        {
            this.graphics.FillRectangle(this.touchBrush, this.rectangles[tagId]);
        }

        // Mark a tag so that the player knows they incorrectly touched a tag.
        public void markTagMistake(string tagId)
        {
            this.graphics.FillRectangle(this.mistakeBrush, this.rectangles[tagId]);
        }

        // Mark all tags untouched.
        public void markAllUntouched()
        {
            foreach(var tag in this.rectangles)
            {
                this.markTagUntouched(tag.Key);
            }
        }
    }
}
