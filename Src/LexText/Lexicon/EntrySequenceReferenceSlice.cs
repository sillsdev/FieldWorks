// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EntrySequenceReferenceSlice.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Provides a class that uses the Go dlg for selecting entries for subentries.
	/// </summary>
	public class EntrySequenceReferenceSlice : CustomReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EntrySequenceReferenceSlice"/> class.
		/// </summary>
		public EntrySequenceReferenceSlice()
			: base(new EntrySequenceReferenceLauncher())
		{
		}
	}
}
