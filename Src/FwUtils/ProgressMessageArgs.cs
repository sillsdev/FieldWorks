// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProgressMessageArgs.cs
// Responsibility: mcconnel

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
