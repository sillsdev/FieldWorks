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
		private MapOfSourceWordElements _mapOfSourceWordElements;
		private char[] _achTrim;

		internal ModifyTargetWordsForm(string strSourceWord,
			MapOfSourceWordElements mapOfSourceWordElements, string strTargetWordFont,
			char[] achTrim)
		{
			InitializeComponent();
			_mapOfSourceWordElements = mapOfSourceWordElements;
			_achTrim = achTrim;
			targetFormDisplayControl.TargetWordFont = new Font(strTargetWordFont, 12);
			targetFormDisplayControl.CallToSetModified = SetModified;

			SourceWordElement sourceWordElement;
			if (mapOfSourceWordElements.TryGetValue(strSourceWord, out sourceWordElement))
				targetFormDisplayControl.Initialize(sourceWordElement, DeleteSourceWord);
		}

		private void DeleteSourceWord(string strSourceWord)
		{
			_mapOfSourceWordElements.Remove(strSourceWord);
		}

		public void SetModified()
		{
			buttonOK.Enabled = targetFormDisplayControl.AreAllTargetFormsNonEmpty;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Debug.Assert(targetFormDisplayControl.AreAllTargetFormsNonEmpty);
			targetFormDisplayControl.TrimTargetWordForms(_achTrim);
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
