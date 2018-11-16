// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>Type of text contained in a token</summary>
	public enum TextType
	{
		/// <summary>Text of verses</summary>
		Verse,
		/// <summary>Text of a footnote or cross reference</summary>
		Note,
		/// <summary>Text within a picture caption</summary>
		PictureCaption,
		/// <summary>v</summary>
		ChapterNumber,
		/// <summary>Digits and modifier of verse number, may include bridges</summary>
		VerseNumber,
		/// <summary>Introductions, titles, section heads, ...</summary>
		Other
	}
}