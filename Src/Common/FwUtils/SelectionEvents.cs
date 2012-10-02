// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2004' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SelectionEvents.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.FwUtils
{

	/// <summary>
	///
	/// </summary>
	public delegate void FwSelectionChangedEventHandler (object sender, FwObjectSelectionEventArgs e);

	#region FwObjectSelectionEventArgs
	/// <remarks>
	/// This event argument class could be used for other user interface lists or trees containing FieldWorks objects
	/// </remarks>
	public class FwObjectSelectionEventArgs : EventArgs
	{
		private int m_index = -1;
		private int m_hvo;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hvo"></param>
		public FwObjectSelectionEventArgs(int hvo)
		{
			m_hvo= hvo;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="index"></param>
		public FwObjectSelectionEventArgs(int hvo, int index)
		{
			m_index = index;
			m_hvo = hvo;
		}
		/// <summary>
		/// The id of the selected object.
		/// </summary>
		public int Hvo
		{
			get
			{
				return m_hvo;
			}
		}

		/// <summary>
		/// Index of the selected object in its list.
		/// </summary>
		public int Index
		{
			get
			{
				return m_index;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool HasSelectedItem
		{
			get
			{
				return this.Hvo != 0;
			}
		}
	}
	#endregion
}
