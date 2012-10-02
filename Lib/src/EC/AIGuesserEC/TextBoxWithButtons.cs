using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SilEncConverters40
{
	public partial class TextBoxWithButtons : UserControl
	{
		private ModifyTargetWordsForm _parent;
		public TextBoxWithButtons(ModifyTargetWordsForm parent)
		{
			_parent = parent;
			InitializeComponent();
		}

		public string TextBoxValue
		{
			get { return textBox.Text; }
			set { textBox.Text = value; }
		}

		public override Font Font
		{
			set
			{
				textBox.Font = value;
			}
		}

		private void buttonDelete_Click(object sender, EventArgs e)
		{
			_parent.DeleteTextWithButtons(this);
		}

		private void buttonMoveUp_Click(object sender, EventArgs e)
		{
			_parent.MoveUpTextWithButtons(this);
		}

		private void buttonMoveDown_Click(object sender, EventArgs e)
		{
			_parent.MoveDownTextWithButtons(this);
		}
	}
}
