// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal class SemanticDomainReferenceVectorSlice : PossibilityReferenceVectorSlice
	{

		protected SemanticDomainReferenceVectorSlice(Control control, LcmCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		public SemanticDomainReferenceVectorSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new SemanticDomainReferenceLauncher(), cache, obj, flid)
		{
		}
	}
}
