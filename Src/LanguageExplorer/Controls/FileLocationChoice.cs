// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls
{
	/// <summary>Options the user has for files that are not under the LinkedFiles folder</summary>
	public enum FileLocationChoice
	{
		/// <summary>Copy file to LinkedFiles folder</summary>
		Copy,
		/// <summary>Move file to LinkedFiles folder</summary>
		Move,
		/// <summary>Leave file in original folder</summary>
		Leave,
	}
}