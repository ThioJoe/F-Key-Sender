using System.Drawing;
using System.Windows.Forms;

namespace F_Key_Sender
{
    public class BigCheckBox : CheckBox
    {
        private int _checkboxSize = 40; // Default size

        public int CheckboxSize
        {
            get { return _checkboxSize; }
            set
            {
                _checkboxSize = value;
                this.Invalidate(); // Redraw the control with the new size
            }
        }

        public BigCheckBox()
        {
            this.Text = "Ctrl";
            this.TextAlign = ContentAlignment.MiddleRight;
            this.Height = 100;  // Control height
            this.Width = 200;   // Control width
            this.AutoSize = false; // Disable auto-size
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Calculate vertical position to center the checkbox square vertically
            int verticalPosition = (this.Height - _checkboxSize) / 2;

            // Define the rectangle for the checkbox
            Rectangle rect = new Rectangle(new Point(0, verticalPosition), new Size(_checkboxSize, _checkboxSize));

            // Draw the checkbox
            ControlPaint.DrawCheckBox(e.Graphics, rect, this.Checked ? ButtonState.Checked : ButtonState.Normal);
        }
    }
}
