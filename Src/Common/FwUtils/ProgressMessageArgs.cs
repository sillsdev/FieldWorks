// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressMessageArgs.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Utility class for passing progress dialog information back to someone who knows about
	/// the progress dialog via an event handler.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProgressMessageArgs : EventArgs
		{
		/// <summary>
		/// The resource id of the progress message to display.
		/// </summary>
		public string MessageId { get; set; }

		///<summary>
		/// The maximum value for the progress bar.
		///</summary>
		public int Max { get; set; }
		}
}
