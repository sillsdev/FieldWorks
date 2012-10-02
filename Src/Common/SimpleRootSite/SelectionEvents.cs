// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SelectionEvents.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SelectionEvents
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// EventArgs about a FieldWorks selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class FwSelectionEventArgs : EventArgs
		{
			private IVwSelection m_selection;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="FwSelectionEventArgs"/> class.
			/// </summary>
			/// <param name="selection">The selection.</param>
			/// --------------------------------------------------------------------------------
			public FwSelectionEventArgs(IVwSelection selection)
			{
				m_selection = selection;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the selection.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public IVwSelection Selection
			{
				get { return m_selection; }
			}
		}
	}
}
