// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SelectionEvents.cs
// Responsibility: FW Team

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
