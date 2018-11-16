// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A base class for several kinds of matcher that do various kinds of string equality/inequality testing.
	/// </summary>
	public abstract class SimpleStringMatcher : BaseMatcher
	{
		/// <summary />
		protected IVwTxtSrcInit m_textSourceInit;
		/// <summary />
		protected IVwTextSource m_ts;
		/// <summary />
		protected ITsString m_tssSource;
		/// <summary />
		protected const int m_MaxSearchStringLength = 1000;	// max length of search string
		/// <summary />
		protected XElement m_persistNode;
		/// <summary />
		protected MatchRangePair m_currentMatchRangePair = new MatchRangePair(-1, -1);
		/// <summary>
		/// Cache for the Match set resulting from FindIn() for a string;
		/// </summary>
		protected List<MatchRangePair> m_results = new List<MatchRangePair>();

		/// <summary>
		/// normal constructor
		/// </summary>
		protected SimpleStringMatcher(IVwPattern pattern)
		{
			Pattern = pattern;
			Init();
		}

		/// <summary>
		/// This class explicitly looks for a particular ws.
		/// </summary>
		public override int WritingSystem => !string.IsNullOrEmpty(Pattern?.IcuLocale) && WritingSystemFactory != null
			? WritingSystemFactory.GetWsFromStr(Pattern.IcuLocale)
			: 0;

		/// <summary>
		/// default for persistence
		/// </summary>
		protected SimpleStringMatcher()
		{
			Init();
		}

		private void Init()
		{
			m_textSourceInit = VwStringTextSourceClass.Create();
			m_ts = m_textSourceInit as IVwTextSource;

			if (Pattern == null)
			{
				Pattern = VwPatternClass.Create();
			}
		}

		/// <summary>
		/// Retrieve pattern (for testing)
		/// </summary>
		public IVwPattern Pattern { get; protected set; }

		/// <summary>
		/// Finds the first match satisfying the abstract method CurrentResultDoesMatch().
		/// </summary>
		/// <param name="tssSource"></param>
		/// <returns>Min and Lim of the segment in the string matching the pattern
		/// and CurrentResultDoesMatch or {return}.IchMin = -1 if nothing was found.</returns>
		protected MatchRangePair FindFirstMatch(ITsString tssSource)
		{
			var mrp = new MatchRangePair();
			mrp.Reset();
			var mrpLast = new MatchRangePair();
			mrpLast.Reset();
			m_textSourceInit.SetString(tssSource);
			m_tssSource = tssSource;
			bool found;
			do
			{   // get first/next match and make sure it is not the same segment of the string
				mrp = FindNextPatternMatch(mrpLast);
				if (mrpLast.Equals(mrp))
				{
					break; // it found the same segment again: Prevent cycles, eg, for Reg Exp "$" (LT-7041).
				}
				mrpLast = mrp;
				found = mrp.IchMin >= 0; // see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation
			} while (found && !CurrentResultDoesMatch(mrp)); // must match the overridden condition
			return mrp;
		}

		/// <summary>
		/// Finds the next match satisfying the pattern.
		/// For some odd cases like looking for "$" in a regular expression,
		/// this will return the same range. The calling code must check.
		/// </summary>
		/// <param name="lastMatch">A match that has been reset to start or the last one found.</param>
		/// <returns>Min and Lim of the segment in the string matching the pattern.</returns>
		protected MatchRangePair FindNextPatternMatch(MatchRangePair lastMatch)
		{
			var ichStart = 0;
			// if we already have a current match, then reset the starting position
			// NOTE: there seems to be a bug(?) in FindIn that prevents us from using IchMin + 1 to find overlapping matches.
			if (lastMatch.IchMin >= 0) // see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation
			{
				ichStart = lastMatch.IchLim;
			}
			int ichMin;
			int ichLim;
			Pattern.FindIn(m_ts, ichStart, m_tssSource.Length, true, out ichMin, out ichLim, null);
			return new MatchRangePair(ichMin, ichLim);
		}

		/// <summary>
		/// Override this method to match additional conditions not handled at the pattern level.
		/// </summary>
		/// <param name="match">The pattern-matched string segment limits to check against
		/// additional matching criteria.</param>
		/// <returns>true if the additional checks succeeded.</returns>
		protected abstract bool CurrentResultDoesMatch(MatchRangePair match);

		/// <summary>Gets all segments of the string that match the pattern. The caller must
		/// call Matches() first to check that there is at least one match. It also sets the
		/// first match this one returns.</summary>
		/// <returns>The list of all unique filter matches found.</returns>
		public List<MatchRangePair> GetAllResults()
		{
			m_results.Clear();
			Debug.Assert(m_currentMatchRangePair.IchMin >= 0, "SimpleStringMatcher.Matches() must set the first filter match.");
			m_results.Add(m_currentMatchRangePair);
			var mrpLast = m_currentMatchRangePair; // set via Matches()
			bool found;
			do
			{
				var mrp = FindNextPatternMatch(mrpLast);
				if (mrp.Equals(mrpLast))// presuming the only duplicate would be the last match that might be found ;-)
				{
					break; // Prevent cycles, eg, for Reg Exp "$" (LT-7041).
				}
				mrpLast = mrp;
				found = mrp.IchMin >= 0; // see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation
				if (found && CurrentResultDoesMatch(mrp)) // must match the overridden condition
				{
					m_results.Add(mrp);
				}
			} while (found);
			return m_results;
		}

		/// <summary />
		protected MatchRangePair CurrentResult => m_currentMatchRangePair;

		#region IMatcher Members

		/// <summary>
		/// Answers the question "Are there any matches?" so that if there aren't,
		/// time is not wasted looking for them. To get all the matches, call GetAllResults() next.
		/// </summary>
		public override bool Matches(ITsString arg)
		{
			m_currentMatchRangePair.Reset();
			try
			{
				m_currentMatchRangePair = FindFirstMatch(arg);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}
			// see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation for *.IchMin >= 0
			return m_currentMatchRangePair.IchMin >= 0;
		}

		/// <summary />
		/// <remarks>For most subclasses, it is enough if it is the same class and pattern.</remarks>
		/// ---------------------------------------------------------------------------------------
		public override bool SameMatcher(IMatcher other)
		{
			if (!(other is SimpleStringMatcher))
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (other.GetType() != GetType())
			{
				return false;
			}
			var otherPattern = ((SimpleStringMatcher)other).Pattern;
			if (otherPattern.Pattern == null)
			{
				if (Pattern.Pattern != null)
				{
					return false;
				}
			}
			else if (!otherPattern.Pattern.Equals(Pattern.Pattern))
			{
				return false;
			}
			return otherPattern.MatchCase == Pattern.MatchCase && otherPattern.MatchDiacritics == Pattern.MatchDiacritics;
		}

		/// <summary>
		/// Check to see if the matcher is valid.
		/// </summary>
		public override bool IsValid()
		{
			return !HasError() && base.IsValid();
		}

		/// <summary>
		/// If the error was in this object, then return the error msg for it, otherwise return
		/// the base error msg.
		/// </summary>
		public override string ErrorMessage()
		{
			return HasError() ? string.Format(FiltersStrings.ksMatchStringToLongLength0, m_MaxSearchStringLength ) : base.ErrorMessage();
		}

		/// <summary>
		/// Does this object know how to make the matcher valid, in the case of SimpleStringMatcher
		/// it's just a matter of truncating the search string to be of a valid length.
		/// </summary>
		public override bool CanMakeValid()
		{
			return HasError();
		}

		/// <summary>
		/// Truncate the match pattern to be of a valid length if that was the error.
		/// </summary>
		public override ITsString MakeValid()
		{
			return HasError() ? Pattern.Pattern.GetSubstring(0, m_MaxSearchStringLength - 1) : base.MakeValid();
		}

		/// <summary>
		/// Local method for testing if there is an error: currently it's just the length of the string
		/// </summary>
		private bool HasError()
		{
			return Pattern.Pattern.Length > m_MaxSearchStringLength;
		}

		#endregion

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "pattern", Pattern.Pattern.Text);
			int var;
			var ws = Pattern.Pattern.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			XmlUtils.SetAttribute(element, "ws", ws.ToString());
			XmlUtils.SetAttribute(element, "matchCase", Pattern.MatchCase.ToString());
			XmlUtils.SetAttribute(element, "matchDiacritics", Pattern.MatchDiacritics.ToString());
			// NOTE!! if any more properties of the matcher become significant, they should be
			// accounted for also in SameMatcher!
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);

			m_persistNode = element;
		}

		/// <summary>
		/// The Cache property finishes the initialization that was started with InitXML
		/// We wait until here because the cache is needed to get the writing system
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;

				if (m_persistNode != null && Pattern.Pattern == null)
				{
					var ws = XmlUtils.GetOptionalIntegerValue(m_persistNode, "ws", value.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
					var tss = TsStringUtils.MakeString(XmlUtils.GetMandatoryAttributeValue(m_persistNode, "pattern"), ws);
					Pattern.Pattern = tss;

					Pattern.MatchCase = XmlUtils.GetOptionalBooleanAttributeValue(m_persistNode, "matchCase", false);
					Pattern.MatchDiacritics = XmlUtils.GetOptionalBooleanAttributeValue(m_persistNode, "matchDiacritics", false);

					// These values are currently never set to anything other than false, initialize them that way
					Pattern.MatchOldWritingSystem = false;
					Pattern.MatchWholeWord = false;
					// UseRegularExpressions is always assumed to be false, the RegExpMatcher class sets it to true in the constructor
					Pattern.UseRegularExpressions = false;
					SetupPatternCollating(Pattern, value);
				}
			}
		}

		/// <summary>
		/// After setting the Pattern (TsString) of the VwPattern, once we have a cache, we can figure out the locale
		/// and sort rules to use based on the WS of the pattern string.
		/// </summary>
		public static void SetupPatternCollating(IVwPattern pattern, LcmCache cache)
		{
			pattern.IcuLocale = cache.ServiceLocator.WritingSystemFactory.GetStrFromWs(pattern.Pattern.get_WritingSystem(0));
			var ws = cache.ServiceLocator.WritingSystemManager.Get(pattern.IcuLocale);
			// Enhance JohnT: we would like to be able to make it use the defined collating rules for the
			// other sort types, but don't currently know how.
			var rulesCollation = ws?.DefaultCollation as RulesCollationDefinition;
			if (rulesCollation != null && rulesCollation.IsValid)
			{
				pattern.IcuCollatingRules = rulesCollation.CollationRules;
			}
		}
	}
}