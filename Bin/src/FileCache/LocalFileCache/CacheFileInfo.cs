// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CacheFileInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	internal class CacheFileInfo
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CacheFileInfo"/> class.
		/// </summary>
		/// <param name="files">The files.</param>
		/// -----------------------------------------------------------------------------------
		internal CacheFileInfo(CachedFile[] files)
		{
			m_Files = files;
			m_lastAccessed = DateTime.Now;
		}

		private CachedFile[] m_Files;
		private DateTime m_lastAccessed;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files.
		/// </summary>
		/// <value>The files.</value>
		/// --------------------------------------------------------------------------------
		public CachedFile[] Files
		{
			get
			{
				m_lastAccessed = DateTime.Now;
				return m_Files;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files for debugging - i.e. doesn't set last accessed time stamp.
		/// </summary>
		/// <value>The debug files.</value>
		/// ------------------------------------------------------------------------------------
		public CachedFile[] DebugFiles
		{
			get { return m_Files; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last accessed date.
		/// </summary>
		/// <value>The last accessed date.</value>
		/// -----------------------------------------------------------------------------------
		public DateTime LastAccessed
		{
			get { return m_lastAccessed; }
		}
	}

}
