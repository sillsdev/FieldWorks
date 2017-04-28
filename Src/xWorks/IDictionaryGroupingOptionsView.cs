// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.XWorks
{
	public interface IDictionaryGroupingOptionsView
	{
		string Description { get; set; }

		bool DisplayInParagraph { get; set; }

		/// <summary>
		/// Fired when the user changes their display in paragraph settings
		/// </summary>
		event EventHandler DisplayInParagraphChanged;

		/// <summary>
		/// Fired when the text of the description has changed
		/// </summary>
		event EventHandler DescriptionChanged;

		/// <summary>
		/// UserControl's Load event will call
		/// </summary>
		event EventHandler Load;
	}
}
