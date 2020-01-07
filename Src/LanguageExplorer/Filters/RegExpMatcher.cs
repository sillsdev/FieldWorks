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
			string finalErrorMessage;
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
					finalErrorMessage = string.Format(FiltersStrings.ksUnknownError, errMsg);
					break;
				case "U_ZERO_ERROR":
					finalErrorMessage = FiltersStrings.ksNoError;
					break;
				case "U_REGEX_ERROR_START":
					finalErrorMessage = FiltersStrings.ksRegexErrorStart;
					break;
				case "U_REGEX_INTERNAL_ERROR":
					finalErrorMessage = FiltersStrings.ksRegexInternalError;
					break;
				case "U_REGEX_RULE_SYNTAX":
					finalErrorMessage = FiltersStrings.ksRegexRuleSyntax;
					break;
				case "U_REGEX_INVALID_STATE":
					finalErrorMessage = FiltersStrings.ksRegexInvalidState;
					break;
				case "U_REGEX_BAD_ESCAPE_SEQUENCE":
					finalErrorMessage = FiltersStrings.ksRegexBadEscapeSequence;
					break;
				case "U_REGEX_PROPERTY_SYNTAX":
					finalErrorMessage = FiltersStrings.ksRegexPropertySyntax;
					break;
				case "U_REGEX_UNIMPLEMENTED":
					finalErrorMessage = FiltersStrings.ksRegexUnimplemented;
					break;
				case "U_REGEX_MISMATCHED_PAREN":
					finalErrorMessage = FiltersStrings.ksRegexMismatchedParen;
					break;
				case "U_REGEX_NUMBER_TOO_BIG":
					finalErrorMessage = FiltersStrings.ksRegexNumberTooBig;
					break;
				case "U_REGEX_BAD_INTERVAL":
					finalErrorMessage = FiltersStrings.ksRegexBadInterval;
					break;
				case "U_REGEX_MAX_LT_MIN":
					finalErrorMessage = FiltersStrings.ksRegexMaxLtMin;
					break;
				case "U_REGEX_INVALID_BACK_REF":
					finalErrorMessage = FiltersStrings.ksRegexInvalidBackRef;
					break;
				case "U_REGEX_INVALID_FLAG":
					finalErrorMessage = FiltersStrings.ksRegexInvalidFlag;
					break;
				case "U_REGEX_LOOK_BEHIND_LIMIT":
					finalErrorMessage = FiltersStrings.ksRegexLookBehindLimit;
					break;
				case "U_REGEX_SET_CONTAINS_STRING":
					finalErrorMessage = FiltersStrings.ksRegexSetContainsString;
					break;
				case "U_REGEX_ERROR_LIMIT":
					finalErrorMessage = FiltersStrings.ksRegexErrorLimit;
					break;
			}
			return finalErrorMessage;
		}
	}
}