// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	internal sealed class AffixRuleFormulaSlice : RuleFormulaSlice
	{
		public AffixRuleFormulaSlice(ISharedEventHandlers sharedEventHandlers)
			: base(sharedEventHandlers)
		{
		}

		public override void FinishInit()
		{
			Control = new AffixRuleFormulaControl(_sharedEventHandlers, ConfigurationNode);
		}
	}
}