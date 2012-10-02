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
	public partial class TargetFormDisplayControl : UserControl
	{
		public Font TargetWordFont { get; set; }

		public TargetFormDisplayControl()
		{
			InitializeComponent();
		}

		private SourceWordElement SourceWordElement { get; set; }

		public delegate void DeleteSourceWord(string strSourceWord);
		public delegate void SetModified();

		internal void Initialize(SourceWordElement sourceWordElement,
			DeleteSourceWord functionToDeleteSourceWord)
		{
			System.Diagnostics.Debug.Assert((sourceWordElement != null)
											&& (functionToDeleteSourceWord != null));

			SourceWordElement = sourceWordElement;
			CallToDeleteSourceWord = functionToDeleteSourceWord;
			foreach (TargetWordElement strTargetForm in sourceWordElement)
				AddToPanel(strTargetForm);
			buttonAdd.Enabled = true;
		}

		public void Reset()
		{
			buttonAdd.Enabled = false;
			flowLayoutPanelTargetWords.Controls.Clear();
		}

		private Control AddToPanel(TargetWordElement targetWordElement)
		{
			var textBox = new TextBoxWithButtons(this)
			{
				TargetWordElement = targetWordElement,
				Font = TargetWordFont
			};

			flowLayoutPanelTargetWords.Controls.Add(textBox);
			return textBox;
		}

		public SetModified CallToSetModified { get; set; }
		public DeleteSourceWord CallToDeleteSourceWord { get; set; }
		public void DeleteTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			flowLayoutPanelTargetWords.Controls.Remove(textBoxWithButtons);
			SourceWordElement.Remove(textBoxWithButtons.TargetWordElement);

			if (flowLayoutPanelTargetWords.Controls.Count == 0)
			{
				DialogResult res = MessageBox.Show(String.Format(Properties.Resources.IDS_QueryToDeleteSourceWord,
																 SourceWordElement.SourceWord),
												   EncConverters.cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Yes)
					CallToDeleteSourceWord(SourceWordElement.SourceWord);
			}
			CallToSetModified();
		}

		public void MoveUpTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			int nIndex = flowLayoutPanelTargetWords.Controls.IndexOf(textBoxWithButtons);
			nIndex = Math.Max(0, nIndex - 1);
			flowLayoutPanelTargetWords.Controls.SetChildIndex(textBoxWithButtons, nIndex);
			CallToSetModified();
		}

		public void MoveDownTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			int nIndex = flowLayoutPanelTargetWords.Controls.IndexOf(textBoxWithButtons);
			nIndex = Math.Max(flowLayoutPanelTargetWords.Controls.Count - 1, nIndex + 1);
			flowLayoutPanelTargetWords.Controls.SetChildIndex(textBoxWithButtons, nIndex);
			CallToSetModified();
		}

		public bool AreAllTargetFormsNonEmpty
		{
			get
			{
				return flowLayoutPanelTargetWords.Controls.Cast<TextBoxWithButtons>().All(control => !String.IsNullOrEmpty(control.TargetWordElement.TargetWord));
			}
		}

		public void TrimTargetWordForms(char[] achTrim)
		{
			System.Diagnostics.Debug.Assert(AreAllTargetFormsNonEmpty); // should be called first
			foreach (TextBoxWithButtons ctrl in flowLayoutPanelTargetWords.Controls)
				ctrl.TargetWordElement.TargetWord = ctrl.TargetWordElement.TargetWord.Trim(achTrim);
		}

		private void buttonAdd_Click(object sender, EventArgs e)
		{
			Control textBox = AddToPanel(SourceWordElement.GetNewTargetWord);
			textBox.Focus();
			CallToSetModified();
		}
	}
}
