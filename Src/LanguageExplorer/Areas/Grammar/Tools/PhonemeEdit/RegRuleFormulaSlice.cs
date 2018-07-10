// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit
{
	/// <summary>
	/// This class represents a <c>PhRegularRule</c> slice.
	/// </summary>
	internal sealed class RegRuleFormulaSlice : RuleFormulaSlice
	{
		internal RegRuleFormulaSlice(ISharedEventHandlers sharedEventHandlers)
			: base(sharedEventHandlers)
		{
		}

		public override void FinishInit()
		{
			Control = new RegRuleFormulaControl(_sharedEventHandlers, ConfigurationNode);
		}
	}
}