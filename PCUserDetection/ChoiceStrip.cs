using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCUserDetection
{
    /// <summary>
    /// A row of small buttons where one of them is the choice in force, like the
    /// Light / Dark / Auto switch in the rail.
    /// </summary>
    /// <remarks>
    /// This is used instead of a combo box or a check box because both of those
    /// paint parts of themselves in system colours that no property turns off,
    /// which shows up badly against the dark palette. A button obeys the colours
    /// it is given, so the strip themes cleanly.
    /// </remarks>
    internal class ChoiceStrip<T> : FlowLayoutPanel
    {
        private T value;

        public ChoiceStrip()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = false;
            Margin = new Padding(0, 4, 0, 4);
        }

        /// <summary>Raised when a different button in the strip is chosen.</summary>
        public event EventHandler ValueChanged;

        /// <summary>The choice in force. Setting it re-marks the buttons.</summary>
        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                ApplyTheme();
            }
        }

        public ChoiceStrip<T> Add(string text, T choice)
        {
            var button = new Button
            {
                Text = text,
                Tag = choice,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(64, 30),
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0, 0, 6, 0),
                Font = Theme.Small,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.Click += (s, e) => Choose(choice);
            Controls.Add(button);

            // the first button added is the choice until one is set, so the strip
            // is never in a state where nothing is marked
            if (Controls.Count == 1) value = choice;

            return this;
        }

        /// <summary>Re-reads the palette. Called after the theme changes.</summary>
        public void ApplyTheme()
        {
            BackColor = Theme.Background;

            foreach (Control control in Controls)
            {
                var button = (Button)control;
                Theme.StyleChoice(button, Equals(button.Tag, value));
            }
        }

        private void Choose(T choice)
        {
            if (Equals(value, choice)) return;

            Value = choice;

            var handler = ValueChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
