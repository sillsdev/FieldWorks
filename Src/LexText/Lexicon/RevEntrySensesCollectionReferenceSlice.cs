// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RevEntrySensesCollectionReferenceSlice.cs
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
	public class RevEntrySensesCollectionReferenceSlice : CustomReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RevEntrySensesCollectionReferenceSlice"/> class.
		/// </summary>
		public RevEntrySensesCollectionReferenceSlice()
			: base(new RevEntrySensesCollectionReferenceLauncher())
		{
		}
	}
}
