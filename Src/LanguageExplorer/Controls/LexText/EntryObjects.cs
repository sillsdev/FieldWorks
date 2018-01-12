// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// A class that allows for parameters to be passed to the Go dialog form the client.
	/// Currently, this only works for XCore messages, not the IText entry point.
	/// </summary>
	public class WindowParams
	{
		#region Data members

		/// <summary>
		/// Window title.
		/// </summary>
		public string m_title;
		/// <summary>
		/// Text in label to the left of the form edit box.
		/// </summary>
		public string m_label;
		/// <summary>
		/// Text on OK button.
		/// </summary>
		public string m_btnText;

		#endregion Data members
	}
}
