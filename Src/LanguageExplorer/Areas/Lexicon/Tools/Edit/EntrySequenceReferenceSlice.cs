// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Provides a class that uses the Go dlg for selecting entries for subentries.
	/// </summary>
	internal class EntrySequenceReferenceSlice : CustomReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EntrySequenceReferenceSlice"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "EntrySequenceReferenceLauncher gets added to panel's Controls collection and disposed there")]
		public EntrySequenceReferenceSlice()
			: base(new EntrySequenceReferenceLauncher())
		{
		}
	}
}
