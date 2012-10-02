using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public class MetaRuleFormulaSlice : RuleFormulaSlice
	{
		public MetaRuleFormulaSlice()
		{
		}

		public override void FinishInit()
		{
			CheckDisposed();
			Control = new MetaRuleFormulaControl(m_configurationNode);
		}

	}
}
