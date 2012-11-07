using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal class SemanticDomainReferenceVectorSlice : PossibilityReferenceVectorSlice
	{

		protected SemanticDomainReferenceVectorSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		public SemanticDomainReferenceVectorSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new SemanticDomainReferenceLauncher(), cache, obj, flid)
		{
		}
	}
}
