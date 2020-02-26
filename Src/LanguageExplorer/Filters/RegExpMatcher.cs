// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the pattern (interpreted as a regular expression) matches the argument
	/// </summary>
	public class RegExpMatcher : SimpleStringMatcher
	{
		/// <summary />
		public RegExpMatcher(IVwPattern pattern) : base(pattern)
		{
			Init();
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public RegExpMatcher() { }

		/// <summary />
		void Init()
		{
			Pattern.UseRegularExpressions = true;
		}

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && base.Matches(arg);
		}

		/// <summary />
		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin >= 0;
		}

		/// <summary />
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				Init();
			}
		}

		/// <summary />
		public override bool IsValid()
		{
			return Pattern.ErrorMessage == null && Pattern.Pattern.Text != null && base.IsValid();
		}

		/// <summary />
		public override string ErrorMessage()
		{
			var errMsg = Pattern.ErrorMessage;
			if (Pattern.Pattern.Text == null)
			{
				errMsg = "U_REGEX_RULE_SYNTAX";
			}
			// handle the case where the error msg has bubbled up from a base class
			if (errMsg == null)
			{
				if (base.IsValid() == false)
				{
					return base.ErrorMessage();
				}
			}
			switch (errMsg)
			{
				default:
					return string.Format(FiltersStrings.ksUnknownError, errMsg);
				case "U_ZERO_ERROR":
					return FiltersStrings.ksNoError;
				case "U_REGEX_ERROR_START":
					return FiltersStrings.ksRegexErrorStart;
				case "U_REGEX_INTERNAL_ERROR":
					return FiltersStrings.ksRegexInternalError;
				case "U_REGEX_RULE_SYNTAX":
					return FiltersStrings.ksRegexRuleSyntax;
				case "U_REGEX_INVALID_STATE":
					return FiltersStrings.ksRegexInvalidState;
				case "U_REGEX_BAD_ESCAPE_SEQUENCE":
					return FiltersStrings.ksRegexBadEscapeSequence;
				case "U_REGEX_PROPERTY_SYNTAX":
					return FiltersStrings.ksRegexPropertySyntax;
				case "U_REGEX_UNIMPLEMENTED":
					return FiltersStrings.ksRegexUnimplemented;
				case "U_REGEX_MISMATCHED_PAREN":
					return FiltersStrings.ksRegexMismatchedParen;
				case "U_REGEX_NUMBER_TOO_BIG":
					return FiltersStrings.ksRegexNumberTooBig;
				case "U_REGEX_BAD_INTERVAL":
					return FiltersStrings.ksRegexBadInterval;
				case "U_REGEX_MAX_LT_MIN":
					return FiltersStrings.ksRegexMaxLtMin;
				case "U_REGEX_INVALID_BACK_REF":
					return FiltersStrings.ksRegexInvalidBackRef;
				case "U_REGEX_INVALID_FLAG":
					return FiltersStrings.ksRegexInvalidFlag;
				case "U_REGEX_LOOK_BEHIND_LIMIT":
					return FiltersStrings.ksRegexLookBehindLimit;
				case "U_REGEX_SET_CONTAINS_STRING":
					return FiltersStrings.ksRegexSetContainsString;
				case "U_REGEX_ERROR_LIMIT":
					return FiltersStrings.ksRegexErrorLimit;
			}
		}
	}
}