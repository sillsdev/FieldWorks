// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.Text;
using SIL.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SIL.AlloGenModel
{
	public class Matcher
	{
		public MatcherType Type { get; set; } = MatcherType.Anywhere;
		public string Pattern { get; set; } = "";
		public bool MatchCase { get; set; } = false;
		public bool MatchDiacritics { get; set; } = false;

		public Matcher() { }

		public Matcher(MatcherType type)
		{
			Type = type;
		}

		public Matcher Duplicate()
		{
			Matcher newMatcher = new Matcher();
			newMatcher.Type = Type;
			newMatcher.Pattern = Pattern;
			newMatcher.MatchCase = MatchCase;
			newMatcher.MatchDiacritics = MatchDiacritics;
			return newMatcher;
		}

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
			{
				return false;
			}
			else
			{
				Matcher matcher = (Matcher)obj;
				return (Type == matcher.Type)
					&& (Pattern == matcher.Pattern)
					&& (MatchCase == matcher.MatchCase)
					&& (MatchDiacritics == matcher.MatchDiacritics);
			}
		}

		public override int GetHashCode()
		{
			return Tuple.Create(Type, Pattern, MatchCase, MatchDiacritics).GetHashCode();
		}

		string GetMatcherTypeName()
		{
			StringBuilder sb = new StringBuilder();
			switch (Type)
			{
				case MatcherType.Begin:
					sb.Append("Begin");
					break;
				case MatcherType.End:
					sb.Append("End");
					break;
				case MatcherType.Exact:
					sb.Append("Exact");
					break;
				case MatcherType.RegularExpression:
					sb.Append("RegExp");
					break;
				default:
					sb.Append("Anywhere");
					break;
			}
			sb.Append("Matcher");
			return sb.ToString();
		}

		public IMatcher GetFwMatcher(int ws, out string errorMessage)
		{
			errorMessage = "";
			IVwPattern fwPattern = CreateFwPattern(ws);
			IMatcher fwMatcher = null;
			if (Type == MatcherType.Begin)
				fwMatcher = new BeginMatcher(fwPattern);
			else if (Type == MatcherType.End)
				fwMatcher = new EndMatcher(fwPattern);
			else if (Type == MatcherType.Exact)
				fwMatcher = new ExactMatcher(fwPattern);
			else if (Type == MatcherType.RegularExpression)
			{
				fwMatcher = new RegExpMatcher(fwPattern);
				if (!fwMatcher.IsValid())
				{
					errorMessage = fwMatcher.ErrorMessage();
					fwMatcher = null;
				}
			}
			else
				fwMatcher = new AnywhereMatcher(fwPattern);
			return fwMatcher;
		}

		IVwPattern CreateFwPattern(int ws)
		{
			IVwPattern fwPattern = VwPatternClass.Create();
			fwPattern.MatchCase = MatchCase;
			fwPattern.MatchDiacritics = MatchDiacritics;
			fwPattern.MatchOldWritingSystem = false;
			fwPattern.Pattern = TsStringUtils.MakeString(Pattern, ws);
			switch (Type)
			{
				case MatcherType.Anywhere:
					fwPattern.MatchWholeWord = false;
					fwPattern.UseRegularExpressions = false;
					break;
				case MatcherType.Begin:
					fwPattern.MatchWholeWord = false;
					fwPattern.UseRegularExpressions = false;
					break;
				case MatcherType.End:
					fwPattern.MatchWholeWord = false;
					fwPattern.UseRegularExpressions = false;
					break;
				case MatcherType.Exact:
					fwPattern.MatchWholeWord = true;
					fwPattern.UseRegularExpressions = false;
					break;
				case MatcherType.RegularExpression:
					fwPattern.MatchWholeWord = false;
					fwPattern.UseRegularExpressions = true;
					break;
			}
			return fwPattern;
		}
	}

	public enum MatcherType
	{
		Anywhere,
		Begin,
		End,
		Exact,
		RegularExpression,
	}
}
