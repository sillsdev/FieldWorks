// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas
{
	internal class InsertOption
	{
		public InsertOption(RuleInsertType type)
		{
			Type = type;
		}

		public RuleInsertType Type { get; }

		public override string ToString()
		{
			return GetOptionString(Type);
		}

		private static string GetOptionString(RuleInsertType type)
		{
			switch (type)
			{
				case RuleInsertType.MorphemeBoundary:
					return AreaResources.ksRuleMorphBdryOpt;
				case RuleInsertType.NaturalClass:
					return AreaResources.ksRuleNCOpt;
				case RuleInsertType.Phoneme:
					return AreaResources.ksRulePhonemeOpt;
				case RuleInsertType.WordBoundary:
					return AreaResources.ksRuleWordBdryOpt;
				case RuleInsertType.Features:
					return AreaResources.ksRuleFeaturesOpt;
				case RuleInsertType.Variable:
					return AreaResources.ksRuleVarOpt;
				case RuleInsertType.Index:
					return AreaResources.ksRuleIndexOpt;
				case RuleInsertType.Column:
					return AreaResources.ksRuleColOpt;
			}
			return null;
		}
	}
}