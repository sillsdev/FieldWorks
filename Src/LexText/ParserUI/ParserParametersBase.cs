// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserParametersBase : Form
	{

		/// <summary>
		/// member strings
		/// </summary>
		private string m_sXmlParameters;

		/// <summary>
		///Get or set the parser parameters XML text
		///</summary>
		public string XmlRep
		{
			get
			{
				CheckDisposed();

				return m_sXmlParameters;
			}
			set
			{
				CheckDisposed();

				m_sXmlParameters = value;
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		protected void ReportChangeOfValue(string item, int value, int newValue, int min, int max)
		{
			string sMessage = String.Format(ParserUIStrings.ksChangedValueReport, item, value, newValue, min, max);
			MessageBox.Show(sMessage, ParserUIStrings.ksChangeValueDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}
}
