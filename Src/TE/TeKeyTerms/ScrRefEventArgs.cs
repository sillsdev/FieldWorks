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
// File: ScrRefEventArgs.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
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
