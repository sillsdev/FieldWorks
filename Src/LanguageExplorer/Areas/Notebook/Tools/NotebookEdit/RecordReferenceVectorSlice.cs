// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary />
	internal sealed class RecordReferenceVectorSlice : CustomReferenceVectorSlice
	{
		/// <summary />
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "RecordReferenceVectorLauncher gets added to panel's Controls collection and disposed there")]
		public RecordReferenceVectorSlice()
			: base(new RecordReferenceVectorLauncher())
		{
		}
	}
}
