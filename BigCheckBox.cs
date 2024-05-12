using System.Drawing;
using System.Windows.Forms;

// This is a custom checkbox control that is larger than the standard checkbox control.
// Taken from https://stackoverflow.com/a/59192202, slightly modified
namespace F_Key_Sender
{
    public class BigCheckBox : CheckBox
    {
        public BigCheckBox()
        {
            this.Text = "Ctrl";
            this.TextAlign = ContentAlignment.MiddleRight;
            this.Height = 100;  // Control height
            this.Width = 200;   // Control width
        }

        public override bool AutoSize
        {
            set { base.AutoSize = false; }
            get { return base.AutoSize; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Adjust the size of the checkbox square
            int squareSide = 40;  // Smaller size for the checkbox
            int verticalPosition = (this.Height - squareSide) / 2;  // Center vertically

            Rectangle rect = new Rectangle(new Point(0, verticalPosition), new Size(squareSide, squareSide));

            // Draw the checkbox
            ControlPaint.DrawCheckBox(e.Graphics, rect, this.Checked ? ButtonState.Checked : ButtonState.Normal);
        }
    }
}
