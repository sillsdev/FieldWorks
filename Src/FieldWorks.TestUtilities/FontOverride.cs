// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FieldWorks.TestUtilities
{
	/// <summary>
	/// Struct which represents an overriden font for a writing system
	/// </summary>
	public struct FontOverride
	{
		/// <summary>Writing system to override font for</summary>
		public int writingSystem;
		/// <summary>Font size in Points</summary>
		public int fontSize;
	}
}