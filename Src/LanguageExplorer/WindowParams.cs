// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// A class that allows for parameters to be passed to the Go dialog from the client.
	/// </summary>
	internal sealed class WindowParams
	{
		/// <summary>
		/// Window title.
		/// </summary>
		internal string m_title;
		/// <summary>
		/// Text in label to the left of the form edit box.
		/// </summary>
		internal string m_label;
		/// <summary>
		/// Text on OK button.
		/// </summary>
		internal string m_btnText;
	}
}
