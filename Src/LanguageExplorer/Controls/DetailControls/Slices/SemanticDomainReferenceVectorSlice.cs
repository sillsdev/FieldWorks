// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	internal sealed class SemanticDomainReferenceVectorSlice : PossibilityReferenceVectorSlice
	{
		internal SemanticDomainReferenceVectorSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new SemanticDomainReferenceLauncher(), cache, obj, flid)
		{
		}
	}
}