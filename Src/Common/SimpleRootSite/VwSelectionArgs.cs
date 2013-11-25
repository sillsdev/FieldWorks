// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: VwSelectionArgs.cs

using System;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;

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
