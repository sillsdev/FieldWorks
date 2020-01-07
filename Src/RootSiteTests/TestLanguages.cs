// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>Defines the possible languages</summary>
	[Flags]
	public enum TestLanguages
	{
		/// <summary>No paragraphs</summary>
		None = 0,
		/// <summary>English paragraphs</summary>
		English = 1,
		/// <summary>French paragraphs</summary>
		French = 2,
		/// <summary>UserWs paragraphs</summary>
		UserWs = 4,
		/// <summary>Empty paragraphs</summary>
		Empty = 8,
		/// <summary>Paragraph with 3 writing systems</summary>
		Mixed = 16,
	}
}