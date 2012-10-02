using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SilEncConverters40
{
	public partial class ModifyTargetWordsForm : Form
	{
		private Font _fontTargetWords;

		public ModifyTargetWordsForm(IEnumerable<string> astrTargetForms, string strTargetWordFont)
		{
			InitializeComponent();
			_fontTargetWords = new Font(strTargetWordFont, 12);
			foreach (string strTargetForm in astrTargetForms)
				AddToPanel(strTargetForm);
		}

		private readonly List<string> m_astrNewTargetForms = new List<string>();
		public List<string> NewTargetForms
		{
			get { return m_astrNewTargetForms; }
		}

		private Control AddToPanel(string strTargetForm)
		{
			var textBox = new TextBoxWithButtons(this)
							  {
								  TextBoxValue = strTargetForm,
								  Font = _fontTargetWords
							  };
			flowLayoutPanelTargetWords.Controls.Add(textBox);
			return textBox;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			foreach (TextBoxWithButtons ctrl in flowLayoutPanelTargetWords.Controls)
				m_astrNewTargetForms.Add(ctrl.TextBoxValue);

			DialogResult = DialogResult.OK;
		}

		public void DeleteTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			flowLayoutPanelTargetWords.Controls.Remove(textBoxWithButtons);
		}

		public void MoveUpTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			int nIndex = flowLayoutPanelTargetWords.Controls.IndexOf(textBoxWithButtons);
			nIndex = Math.Max(0, nIndex - 1);
			flowLayoutPanelTargetWords.Controls.SetChildIndex(textBoxWithButtons, nIndex);
		}

		public void MoveDownTextWithButtons(TextBoxWithButtons textBoxWithButtons)
		{
			int nIndex = flowLayoutPanelTargetWords.Controls.IndexOf(textBoxWithButtons);
			nIndex = Math.Max(flowLayoutPanelTargetWords.Controls.Count - 1, nIndex + 1);
			flowLayoutPanelTargetWords.Controls.SetChildIndex(textBoxWithButtons, nIndex);
		}

		private void buttonAdd_Click(object sender, EventArgs e)
		{
			Control textBox = AddToPanel(null);
			textBox.Focus();
		}
	}
}
