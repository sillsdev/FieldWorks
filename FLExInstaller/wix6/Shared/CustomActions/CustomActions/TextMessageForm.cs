using System.Drawing;
using System.Windows.Forms;

namespace CustomActions
{
    public partial class TextMessageForm : Form
    {
        public TextMessageForm(string text)
        {
            InitializeComponent();
            lblText.Text = text;
            lblText.TextAlign = ContentAlignment.MiddleLeft;
        }
    }
}
