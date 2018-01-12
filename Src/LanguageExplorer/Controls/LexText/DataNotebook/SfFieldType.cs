// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	public enum SfFieldType
	{
		/// <summary>ignored</summary>
		Discard,
		/// <summary>Multi-paragraph text field</summary>
		Text,
		/// <summary>Simple string text field</summary>
		String,
		/// <summary>Date/Time type field</summary>
		DateTime,
		/// <summary>List item reference field</summary>
		ListRef,
		/// <summary>Link field</summary>
		Link,
		/// <summary>Invalid field -- not handled by program!</summary>
		Invalid
	}
}