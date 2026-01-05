using System;
using System.Windows.Forms;

namespace CustomActions
{
    public partial class ClosePromptForm : Form
    {
        public ClosePromptForm(string text)
        {
            InitializeComponent();
            messageText.Text = text;
        }
    }
}
