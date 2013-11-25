// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrRefEventArgs.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Custom event args for an event that needs to report a Scripture reference (as a BCV int)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ScrRefEventArgs : EventArgs
	{
		/// <summary>Gets/Sets the Scripture reference as a BCV int (BBCCCVVV).</summary>
		public int RefBCV = 0;

		/// <summary>Gets/Sets the key term  reference.</summary>
		public KeyTermRef KeyTermRef = KeyTermRef.Empty;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the event args
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrRefEventArgs(KeyTermRef keyTermRef)
		{
			if (keyTermRef != null && keyTermRef != KeyTermRef.Empty)
			{
				KeyTermRef = keyTermRef;
				RefBCV = keyTermRef.ChkRef.Ref;
			}
		}
	}
}
