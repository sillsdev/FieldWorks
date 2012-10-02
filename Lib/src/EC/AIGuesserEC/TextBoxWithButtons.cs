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
		private TargetFormDisplayControl _parent;
		public TextBoxWithButtons(TargetFormDisplayControl parent)
		{
			_parent = parent;
			InitializeComponent();

			// for some reason if I have these in the Designer.cs, it fails to open properly
			//  (probably related to the fact that our namespace is SilEncConverters40,
			//  but that means something else...
			buttonDelete.Image = Properties.Resources.DeleteHS;
			buttonMoveUp.Image = Properties.Resources.BuilderDialog_moveup;
			buttonMoveDown.Image = Properties.Resources.BuilderDialog_movedown;
		}

		private bool _bBlockSetModified = false;
		private TargetWordElement _targetWordElement;
		internal TargetWordElement TargetWordElement
		{
			get { return _targetWordElement; }
			set
			{
				_targetWordElement = value;
				_bBlockSetModified = true;
				TextBoxValue = _targetWordElement.TargetWord;
				_bBlockSetModified = false;
			}
		}

		private string TextBoxValue
		{
			set { textBox.Text = value; }
		}

		public override Font Font
		{
			set { textBox.Font = value; }
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

		private void textBox_TextChanged(object sender, EventArgs e)
		{
			// if the user edits the translation, then we have to update the map of maps
			_targetWordElement.TargetWord = textBox.Text;
			if (!_bBlockSetModified)
				_parent.CallToSetModified();
		}
	}
}
