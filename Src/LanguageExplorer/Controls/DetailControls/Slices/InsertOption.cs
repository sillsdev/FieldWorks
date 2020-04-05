// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	internal sealed class InsertOption
	{
		internal InsertOption(RuleInsertType type)
		{
			Type = type;
		}

		internal RuleInsertType Type { get; }

		public override string ToString()
		{
			return GetOptionString(Type);
		}

		private static string GetOptionString(RuleInsertType type)
		{
			switch (type)
			{
				case RuleInsertType.MorphemeBoundary:
					return LanguageExplorerControls.ksRuleMorphBdryOpt;
				case RuleInsertType.NaturalClass:
					return LanguageExplorerControls.ksRuleNCOpt;
				case RuleInsertType.Phoneme:
					return LanguageExplorerControls.ksRulePhonemeOpt;
				case RuleInsertType.WordBoundary:
					return LanguageExplorerControls.ksRuleWordBdryOpt;
				case RuleInsertType.Features:
					return LanguageExplorerControls.ksRuleFeaturesOpt;
				case RuleInsertType.Variable:
					return LanguageExplorerControls.ksRuleVarOpt;
				case RuleInsertType.Index:
					return LanguageExplorerControls.ksRuleIndexOpt;
				case RuleInsertType.Column:
					return LanguageExplorerControls.ksRuleColOpt;
			}
			return null;
		}
	}
}