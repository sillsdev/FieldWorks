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
		protected int m_hvo;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		internal LObject(int hvo)
		{
			m_hvo = hvo;
		}

		public int HVO
		{
			get { return m_hvo; }
		}
	}
}