// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Abstract base class for LEnty and LSense,
	/// which are 'cheap' versions of the corresponding LCM classes.
	/// </summary>
	internal abstract class LObject
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		internal LObject(int hvo)
		{
			HVO = hvo;
		}

		public int HVO { get; }
	}
}