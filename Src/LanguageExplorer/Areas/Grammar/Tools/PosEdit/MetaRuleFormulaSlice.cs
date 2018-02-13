// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	internal sealed class MetaRuleFormulaSlice : RuleFormulaSlice
	{
		public MetaRuleFormulaSlice()
		{
		}

		public override void FinishInit()
		{
			Control = new MetaRuleFormulaControl(ConfigurationNode);
		}

	}
}