// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VwSelectionArgs.cs
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Class for passing rootbox and new selection to handler.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class VwSelectionArgs : EventArgs
	{
		private IVwRootBox m_rootb;
		private IVwSelection m_vwsel;
		/// <summary>
		/// The Rootbox whose selection has changed.
		/// </summary>
		public IVwRootBox RootBox
		{
			get { return m_rootb; }
		}
		/// <summary>
		/// The new selection for the rootbox.
		/// </summary>
		public IVwSelection Selection
		{
			get { return m_vwsel; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="rootb">The rootb.</param>
		/// <param name="vwsel">The vwsel.</param>
		/// ------------------------------------------------------------------------------------
		public VwSelectionArgs(IVwRootBox rootb, IVwSelection vwsel)
		{
			m_rootb = rootb;
			m_vwsel = vwsel;
		}
	}
}
