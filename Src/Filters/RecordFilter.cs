// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
//	At the moment (July 2004), the only instances of this class do filtering in memory.
//	This does not imply that all filtering will always be done in memory, only that we haven't
//	yet designed or implemented a way to do the filtering while querying.
//		when we do, we will probably split RecordFilter into two subclasses, one for in memory
//	filters which can use LCM properties, and one which will somehow contribute to the Where
//	clause which populates the RecordList. In that case, the RecordList will be aware of the filter
//	and will be responsible for somehow incorporating its filtering desires as it does the query.

// (JohnT, May 2004). Added additional classes and interfaces designed to support a filter bar
// in a Browse view. (Should these be spread across more files?)

// The filter bar looks like a set of combos, one per column, which the user can fill in to limit
// the items shown in the view to ones that satisfy some condition.

// The idea here is that, given the XML that defines a cell view, if it is one of the types we
// recognize, we instantiate a corresponding implementation of the StringFinder interface, which
// allows us to obtain the strings shown in the cell given the base object.

// Then, given the full object list, we populate a combo box menu with 'blanks', 'non-blanks',
// and a list of the unique values that occur (found by applying the string finder to the objects;
// if more than about 30 are found, give up). Menu could also contain 'use patterns' (then the user
// may type a regexp); a submenu of pattern functions; 'Any of...' (brings up a dialog with the
// complete list of values that occur, and check boxes); 'Any except...' (similar); and perhaps
// eventually something like what the 'Custom...' item brings up in Excel autofilters. The user can
// also just type a string; without pattern matching, only initial and final # (start and end of string)
// have special meaning.
//
// When the user selects a filter option for a cell, we instantiate the appropriate kind of Matcher,
// and combine this with the string finder to make a FilterBarCellFilter. If more than one cell
// is set, or there was an original filter defining the list, we further combine all the filters
// using an AndFilter.
// </remarks>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.WritingSystems;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Event arguments for FilterChangeHandler event.
	/// Arguably, we could have separate events for adding and removing, but that would make it
	/// more difficult to avoid refreshing the list twice when switching from one filter to
	/// another. Arguably, both add and remove could be arrays. But so far there has been no
	/// need for this, and if we do, we can easily keep the current constructor but change
	/// the acessors, which are probably rather less used.
	/// </summary>
	public class FilterChangeEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilterChangeEventArgs"/> class.
		/// </summary>
		public FilterChangeEventArgs(RecordFilter added, RecordFilter removed)
		{
			Added = added;
			Removed = removed;
		}

		/// <summary>
		/// Gets the added RecordFilter.
		/// </summary>
		public RecordFilter Added { get; }

		/// <summary>
		/// Gets the removed RecordFilter.
		/// </summary>
		public RecordFilter Removed { get; }
	}

	/// <summary />
	public delegate void FilterChangeHandler(object sender, FilterChangeEventArgs e);

	/// <summary>
	/// Summary description for RecordFilter.
	/// </summary>
	public abstract class RecordFilter : IPersistAsXml, IStoresLcmCache, IStoresDataAccess
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RecordFilter"/> class.
		/// </summary>
		protected RecordFilter()
		{
			Name = FiltersStrings.ksUnknown;
			id = "Unknown";
		}


		/// <summary>
		/// Set the cache on the specified object if it wants it.
		/// </summary>
		internal void SetCache(object obj, LcmCache cache)
		{
			var sfc = obj as IStoresLcmCache;
			if (sfc != null)
			{
				sfc.Cache = cache;
			}
		}

		internal void SetDataAccess(object obj, ISilDataAccess sda)
		{
			var sfc = obj as IStoresDataAccess;
			if (sfc != null)
			{
				sfc.DataAccess = sda;
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// this is used, for example, and persist in the selected id in the users xml preferences
		/// </summary>
		public string id { get; protected set; }

		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		public virtual string imageName => "SimpleFilter";

		/// <summary>
		/// May be used to preload data for efficient filtering of many instances.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public abstract bool Accept(IManyOnePathSortItem item);

		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		public virtual bool IsUserVisible => false;

		/// <summary>
		/// Tells whether the filter is currently valid.  This is true by default.
		/// </summary>
		public virtual bool IsValid => true;

		/// <summary>
		/// a factory method for filters
		/// </summary>
		public static RecordFilter Create(LcmCache cache, XElement configuration)
		{
			var filter = (RecordFilter)DynamicLoader.CreateObject(configuration);
			filter.Init(cache, configuration);
			return filter;
		}

		/// <summary>
		/// Initialize the filter
		/// </summary>
		public virtual void Init(LcmCache cache, XElement filterNode)
		{
		}

		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		public virtual bool SameFilter(RecordFilter other)
		{
			return other == this;
		}

		/// <summary>
		/// If this or a contained filter is considered equal to the argument, answer true
		/// </summary>
		public bool Contains(RecordFilter other)
		{
			return EqualContainedFilter(other) != null;
		}

		/// <summary>
		/// If this or a contained filter is considered equal to the argument, answer the filter
		/// or subfilter that is consiered equal.
		/// Record Filters that can contain more than one filter (e.g. AndFilter) should override this.
		/// </summary>
		public virtual RecordFilter EqualContainedFilter(RecordFilter other)
		{
			return SameFilter(other) ? this : null;
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "name", Name);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public virtual void InitXml(XElement node)
		{
			Name = XmlUtils.GetMandatoryAttributeValue(node, "name");
		}

		#endregion

		#region IStoresLcmCache

		/// <summary>
		/// Set the cache.
		/// </summary>
		public virtual LcmCache Cache
		{
			set
			{
				//nothing to do
			}
		}
		#endregion

		/// <summary>
		/// Allows setting some data access other than the one derived from the Cache.
		/// To have this effect, it must be called AFTER setting the cache.
		/// </summary>
		public virtual ISilDataAccess DataAccess
		{
			set { }
		}
	}

	/// <summary>
	/// this filter passes CmAnnotations which are pointing at objects of the class listed
	/// in the targetClasses attribute.
	/// </summary>
	public class ProblemAnnotationFilter: RecordFilter
	{
		private LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProblemAnnotationFilter"/> class.
		/// </summary>
		/// <remarks>must have a constructor with no parameters, to use with the dynamic loader
		/// or IPersistAsXml</remarks>
		public ProblemAnnotationFilter()
		{
			ClassIds = new List<int>();
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "classIds", XmlUtils.MakeStringFromList(ClassIds));
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			ClassIds = new List<int>(XmlUtils.GetMandatoryIntegerListAttributeValue(node, "classIds"));
		}

		/// <summary>
		/// Gets the class ids.
		/// </summary>
		public List<int> ClassIds { get; protected set; }

		public override LcmCache Cache
		{
			set
			{
				m_cache = value;
				base.Cache = value;
			}
		}

		/// <summary>
		/// Initialize the filter
		/// </summary>
		public override void Init(LcmCache cache, XElement filterNode)
		{
			base.Init(cache, filterNode);
			m_cache = cache;
			var classList =XmlUtils.GetMandatoryAttributeValue(filterNode, "targetClasses");
			var classes= classList.Split(',');

			//enhance: currently, this will require that we name every subclass as well.
			foreach(var name in classes)
			{
				var cls = cache.DomainDataByFlid.MetaDataCache.GetClassId(name.Trim());
				if (cls <= 0)
				{
					throw new FwConfigurationException("The class name '" + name + "' is not valid");
				}
				ClassIds.Add(cls);
			}
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept (IManyOnePathSortItem item)
		{
			var obj = item.KeyObjectUsing(m_cache);
			if (!(obj is ICmBaseAnnotation))
			{
				return false; // It's not a base annotation
			}

			var annotation = (ICmBaseAnnotation)obj;
			if (annotation.BeginObjectRA == null)
			{
				return false;
			}

			var cls = annotation.BeginObjectRA.ClassID;
			foreach (var i in ClassIds)
			{
				if (i == cls)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Matchers are able to tell whether a string matches a pattern.
	/// </summary>
	public interface IMatcher
	{
		/// <summary />
		bool Accept(ITsString tssKey);
		/// <summary />
		bool Matches(ITsString arg);
		/// <summary />
		bool SameMatcher(IMatcher other);
		/// <summary />
		bool IsValid();
		/// <summary />
		string ErrorMessage();
		/// <summary />
		bool CanMakeValid();
		/// <summary />
		ITsString MakeValid();
		/// <summary />
		ITsString Label { get; set; }
		/// <summary />
		ILgWritingSystemFactory WritingSystemFactory { get; set; }
		/// <summary>
		/// If there is one specific writing system that the matcher looks for, return it;
		/// otherwise return 0.
		/// </summary>
		int WritingSystem { get; }
	}

	/// <summary>
	/// This is a base class for matchers; so far it just implements storing the label.
	/// </summary>
	public abstract class BaseMatcher : IMatcher, IPersistAsXml, IStoresLcmCache
	{
		// Todo: get this initialized somehow.
		// This is used only to save the value restored by InitXml until the Cache is set
		// so that m_tssLabel can be computed.
		private string m_xmlLabel;

		#region IMatcher Members

		/// <summary />
		public abstract bool Matches(ITsString arg);

		/// <summary />
		public abstract bool SameMatcher(IMatcher other);

		/// <summary>
		/// No specific writing system for most of these matchers.
		/// </summary>
		public virtual int WritingSystem => 0;

		/// <summary />
		public ITsString Label { get; set; }

		/// <summary />
		public virtual bool IsValid()
		{
			return true;	// most matchers won't have to override this - regex is one that does though
		}

		/// <summary />
		public virtual string ErrorMessage()
		{
			return string.Format(FiltersStrings.ksErrorMsg, IsValid());
		}

		/// <summary />
		public virtual bool CanMakeValid()
		{
			return false;
		}

		/// <summary />
		public virtual ITsString MakeValid()
		{
			return null;	// should only be called if CanMakeValid it true and that class should implement it.
		}
		#endregion

		/// <summary />
		public ILgWritingSystemFactory WritingSystemFactory { set; get; }

		/// <summary>
		/// This is overridden only by BlankMatcher currently, which matches
		/// on an empty list of strings.
		/// </summary>
		public virtual bool Accept(ITsString tss)
		{
			return Matches(tss);
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement node)
		{
			if (Label != null)
			{
				var contents = TsStringUtils.GetXmlRep(Label, WritingSystemFactory, 0, false);
				XmlUtils.SetAttribute(node, "label", contents);
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public virtual void InitXml(XElement node)
		{
			m_xmlLabel = XmlUtils.GetOptionalAttributeValue(node, "label");
		}

		#endregion

		#region Implementation of IStoresLcmCache

		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		public virtual LcmCache Cache
		{
			set
			{
				WritingSystemFactory = value.WritingSystemFactory;
				if (m_xmlLabel != null)
				{
					Label = TsStringSerializer.DeserializeTsStringFromXml(m_xmlLabel, WritingSystemFactory);
				}
			}
		}

		#endregion
	}

	/// <summary>
	/// Class to keep track of the ichMin/ichLim sets resulting from a FindIn() match.
	/// </summary>
	public struct MatchRangePair
	{
		public MatchRangePair(int ichMin, int ichLim)
		{
			IchMin = ichMin;
			IchLim = ichLim;
		}

		public void Reset()
		{
			IchMin = -1;
			IchLim = -1;
		}

		public int IchMin { get; internal set; }

		public int IchLim { get; internal set; }
	}


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
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "pattern", Pattern.Pattern.Text);
			int var;
			var ws = Pattern.Pattern.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			XmlUtils.SetAttribute(node, "ws", ws.ToString());
			XmlUtils.SetAttribute(node, "matchCase", Pattern.MatchCase.ToString());
			XmlUtils.SetAttribute(node, "matchDiacritics", Pattern.MatchDiacritics.ToString());
			// NOTE!! if any more properties of the matcher become significant, they should be
			// accounted for also in SameMatcher!
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);

			m_persistNode = node;
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

	/// <summary>
	/// Matches if the pattern is exactly the argument.
	/// </summary>
	public class ExactMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		/// <param name="pattern"></param>
		public ExactMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ExactMatcher() {}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin == 0 && match.IchLim == m_tssSource.Length;
		}
	}

	/// <summary>
	/// Matches if the pattern occurs at the start of the argument
	/// </summary>
	public class BeginMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public BeginMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public BeginMatcher() {}

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && arg.Length >= Pattern.Pattern.Length && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin == 0;
		}
	}

	/// <summary>
	/// Matches if the pattern occurs at the end of the argument
	/// </summary>
	public class EndMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public EndMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public EndMatcher() {}

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && arg.Length >= Pattern.Pattern.Length && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchLim == m_tssSource.Length;
		}
	}

	/// <summary>
	/// Matches if the pattern occurs anywhere in the argument
	/// </summary>
	public class AnywhereMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public AnywhereMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public AnywhereMatcher() {}

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin >= 0;
		}
	}

	/// <summary>
	/// Matches if the pattern (interpreted as a regular expression) matches the argument
	/// </summary>
	public class RegExpMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public RegExpMatcher(IVwPattern pattern) : base(pattern)
		{
			Init();
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public RegExpMatcher() {}

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
					finalErrorMessage = String.Format(FiltersStrings.ksUnknownError, errMsg);
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

	/// <summary>
	/// Matches blanks.
	/// </summary>
	public class BlankMatcher : BaseMatcher
	{
		/// <summary>
		/// Matches any empty or null string, or one consisting entirely of white space
		/// characters. I think the .NET definition of white space is good enough; it's unlikely
		/// we'll need new PUA whitespace characters.
		/// </summary>
		public override bool Matches(ITsString arg)
		{
			if (arg == null || arg.Length == 0)
			{
				return true;
			}
			return arg.Text.All(t => char.IsWhiteSpace(t));
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			return other is BlankMatcher;
		}
	}

	/// <summary>
	/// Matches non-blanks.
	/// </summary>
	public class NonBlankMatcher : BaseMatcher
	{
		/// <summary>
		/// The exact opposite of BlankMatcher.
		/// </summary>
		public override bool Matches(ITsString arg)
		{
			if (arg == null || arg.Length == 0)
			{
				return false;
			}
			return arg.Text.Any(t => !char.IsWhiteSpace(t));
		}
		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			return other is NonBlankMatcher;
		}
	}

	/// <summary>
	/// Matches if the embedded matcher fails.
	/// </summary>
	public class InvertMatcher : BaseMatcher, IStoresLcmCache, IStoresDataAccess
	{
		/// <summary>
		/// regular constructor
		/// </summary>
		public InvertMatcher (IMatcher matcher)
		{
			MatcherToInvert = matcher;
		}

		/// <summary>
		/// default for persistence.
		/// </summary>
		public InvertMatcher()
		{
		}

		/// <summary>
		/// Gets the matcher to invert.
		/// </summary>
		public IMatcher MatcherToInvert { get; private set; }

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && !MatcherToInvert.Matches(arg);
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			var other2 = other as InvertMatcher;
			return other2 != null && MatcherToInvert.SameMatcher(other2.MatcherToInvert);
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			DynamicLoader.PersistObject(MatcherToInvert, node, "invertMatcher");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			MatcherToInvert = DynamicLoader.RestoreFromChild(node, "invertMatcher") as IMatcher;
		}

		#region IStoresLcmCache Members

		LcmCache IStoresLcmCache.Cache
		{
			set
			{
				if (MatcherToInvert is IStoresLcmCache)
				{
					(MatcherToInvert as IStoresLcmCache).Cache = value;
				}
			}
		}

		ISilDataAccess IStoresDataAccess.DataAccess
		{
			set
			{
				if (MatcherToInvert is IStoresDataAccess)
				{
					(MatcherToInvert as IStoresDataAccess).DataAccess = value;
				}
			}
		}
		#endregion
	}

	// Enhance JohnT: other ideas:
	//  - ListMatcher: matches any string in a list; useful if we allow user to check multiple values to match.
	//  - OrFilter: matches if any subfilter matches.

	/// <summary>
	/// Implementors of this interface are responsible for finding one or more strings that are displayed
	/// as the value of one column in a browse view. The argument is the Hvo of the object that the browse
	/// row represents.
	///
	/// Optimize JohnT: it would be nice not to generate all the strings if an early one matches.
	/// For this reason, perhaps returning an enumerator would be better. However, it's a bit more
	/// complex, and I expect in most cases most objects will fail, and for those objects we have
	/// to try all the strings anyway.
	/// </summary>
	public interface IStringFinder
	{
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		string[] Strings(int hvo);

		/// <summary>
		/// Strings the specified item.
		/// </summary>
		string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd);
		string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd); // similar key more suitable for sorting.
		ITsString Key(IManyOnePathSortItem item);

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		bool SameFinder(IStringFinder other);

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		void CollectItems(int hvo, ArrayList collector);

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. Also permitted to do nothing.
		/// </summary>
		void Preload(object rootObj);

		/// <summary>
		/// Called if we need to ensure that a particular (typically decorator) DA is used to
		/// interpret properties.
		/// </summary>
		ISilDataAccess DataAccess { set; }
	}

	/// <summary />
	public abstract class StringFinderBase : IStringFinder, IPersistAsXml, IStoresLcmCache
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringFinderBase"/> class.
		/// Default constructor for IPersistAsXml
		/// </summary>
		protected StringFinderBase()
		{
		}

		/// <summary>
		/// Normal constructor for most uses.
		/// </summary>
		protected StringFinderBase(ISilDataAccess sda)
		{
			DataAccess = sda;
		}

		/// <summary>
		/// Default is to return the strings for the key object.
		/// </summary>
		public string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			var result = Strings(item.KeyObject);
			if (sortedFromEnd)
			{
				for (var i = 0; i < result.Length; i++)
				{
					result[i] = TsStringUtils.ReverseString(result[i]);
				}
			}

			return result;
		}

		/// <summary>
		/// For most of these we want to return the same thing.
		/// </summary>
		public string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			return Strings(item, sortedFromEnd);
		}

		public virtual ITsString Key(IManyOnePathSortItem item)
		{
			throw new NotImplementedException("Don't have new Key function implemented on class " + this.GetType());
		}


		public ISilDataAccess DataAccess { get; set; }

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public abstract string[] Strings(int hvo);
		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		public abstract bool SameFinder(IStringFinder other);
		/// <summary>
		/// Add to collector the IManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public virtual void CollectItems(int hvo, ArrayList collector)
		{
			collector.Add(new ManyOnePathSortItem(hvo, null, null));
		}

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. Also permitted to do nothing, as in this default implementation.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement node)
		{
			// nothing to do in base class
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public virtual void InitXml(XElement node)
		{
			// nothing to do in base class
		}

		#endregion

		#region IStoresLcmCache
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		public LcmCache Cache
		{
			set
			{
				DataAccess = value.DomainDataByFlid;
			}
		}
		#endregion
	}

	/// <summary>
	/// This class implements StringFinder by looking up one multilingual property of the object
	/// itself.
	/// </summary>
	public class OwnMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public OwnMlPropFinder(ISilDataAccess sda, int flid, int ws)
			: base(sda)
		{
			Flid = flid;
			Ws = ws;
		}

		/// <summary>
		/// For persistence with IPersistAsXml
		/// </summary>
		public OwnMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flid", Flid.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_MultiStringAlt(hvo, Flid, Ws).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid, ws, and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OwnMlPropFinder;
			return other2 != null && (other2.Flid == Flid && other2.DataAccess == DataAccess && other2.Ws == Ws);
		}

		/// <summary>
		/// Keys the specified item.
		/// </summary>
		public override ITsString Key(IManyOnePathSortItem item)
		{
			return DataAccess.get_MultiStringAlt(item.KeyObject, Flid, Ws);
		}

		#endregion
	}

	/// <summary>
	/// This class implements StringFinder by looking up one monlingual property of the object
	/// itself.
	/// </summary>
	public class OwnMonoPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		public OwnMonoPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			Flid = flid;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		public OwnMonoPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flid", Flid.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_StringProp(hvo, Flid).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OwnMonoPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.Flid == Flid && other2.DataAccess == DataAccess;
		}

		#endregion
	}

	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a sequence
	/// of values from some kind of sequence or collection. We return the values of the
	/// displayed property for each item in the sequence.
	/// </summary>
	public class OneIndirectMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OneIndirectMlPropFinder"/> class.
		/// </summary>
		public OneIndirectMlPropFinder(ISilDataAccess sda, int flidVec, int flidString, int ws)
			: base(sda)
		{
			FlidVec = flidVec;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// Gets the flid vec.
		/// </summary>
		public int FlidVec { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectMlPropFinder()
		{
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flidVec", FlidVec.ToString());
			XmlUtils.SetAttribute(node, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(node, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			FlidVec = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidVec");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			var count = DataAccess.get_VecSize(hvo, FlidVec);
			var result = new string[count];
			for (var i = 0; i < count; ++i)
			{
				result[i] = DataAccess.get_MultiStringAlt(DataAccess.get_VecItem(hvo, FlidVec, i), FlidString, Ws).Text ?? string.Empty;
			}
			return result;
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OneIndirectMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.FlidVec == FlidVec && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}
		#endregion
	}

	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a sequence
	/// of strings derived from a multistring property of objects at the leaves of a tree.
	/// A sequence of flids specifies the (currently sequence) properties to follow to reach the
	/// leaves. Leaf objects are found from the root by taking retrieving property flidVec[0]
	/// of the root object, then flidVec[1] of the resulting object, and so forth.
	/// </summary>
	public class MultiIndirectMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultiIndirectMlPropFinder"/> class.
		/// </summary>
		public MultiIndirectMlPropFinder(ISilDataAccess sda, int[] flidVec, int flidString, int ws)
			: base(sda)
		{
			VecFlids = flidVec;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public MultiIndirectMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the vec flids.
		/// </summary>
		public int[] VecFlids { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flidVec", XmlUtils.MakeIntegerListValue(VecFlids));
			XmlUtils.SetAttribute(node, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(node, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			VecFlids = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "flidVec");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		// Return the number of values in the tree rooted at hvo, where index (into m_flidVec)
		// gives the index of the property which should be followed from that object.
		private int CountItems(int hvo, int index)
		{
			var count = DataAccess.get_VecSize(hvo, VecFlids[index]);
			if (index == VecFlids.Length - 1)
			{
				return count;
			}
			var total = 0;
			for (var i = 0; i < count; ++i)
			{
				total += CountItems(DataAccess.get_VecItem(hvo, VecFlids[index], i), index + 1);
			}
			return total;
		}

		/// <summary>
		/// Insert into results, starting at resIndex, the strings obtained from the
		/// tree rooted at hvo. flidVec[flidIndex] is the property to follow from hvo.
		/// </summary>
		private void GetItems(int hvo, int flidIndex, string[] results, ref int resIndex)
		{
			if (flidIndex == VecFlids.Length)
			{
				// add the string for this leaf object
				results[resIndex] = DataAccess.get_MultiStringAlt(hvo, FlidString, Ws).Text ?? string.Empty;
				resIndex++;
			}
			else
			{
				var count = DataAccess.get_VecSize(hvo, VecFlids[flidIndex]);
				for (var i = 0; i < count; ++i)
				{
					GetItems(DataAccess.get_VecItem(hvo, VecFlids[flidIndex], i), flidIndex + 1, results, ref resIndex);
				}
			}
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			var result = new string[CountItems(hvo, 0)];
			var resIndex = 0;
			GetItems(hvo, 0, result, ref resIndex);
			return result;
		}

		private static bool SameVec(int[] first, int[] second)
		{
			if (first.Length != second.Length)
			{
				return false;
			}

			return !first.Where((t, i) => t != second[i]).Any();
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as MultiIndirectMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return SameVec(other2.VecFlids, VecFlids) && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}
		#endregion
	}

	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a single
	/// ML alternative from an object that is the value of an atomic property. We return the value of the
	/// displayed property for the target object.
	/// </summary>
	public class OneIndirectAtomMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OneIndirectAtomMlPropFinder"/> class.
		/// </summary>
		public OneIndirectAtomMlPropFinder(ISilDataAccess sda, int flidAtom, int flidString, int ws)
			: base(sda)
		{
			FlidAtom = flidAtom;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectAtomMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid atom.
		/// </summary>
		public int FlidAtom { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flidAtom", FlidAtom.ToString());
			XmlUtils.SetAttribute(node, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(node, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			FlidAtom = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidAtom");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_MultiStringAlt(DataAccess.get_ObjectProp(hvo, FlidAtom), FlidString, Ws).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OneIndirectAtomMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.FlidAtom == FlidAtom && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}

		#endregion
	}

	/// <summary>
	/// A FilterBarCellFilter handles one cell of a filter bar. It is made up of two components: a StringFinder
	/// specifies how to find target strings from the hvo of the target object, while a IMatcher
	/// specifies how to determine whether a string matches. An object passes if any of the target strings
	/// produced by the finder matches the pattern.
	///
	/// The design avoids using LCM objects as performance is likely to be critical. For a long list,
	/// it would be well to pre-load the cache with all relevant properties in as few queries as
	/// possible. It is designed to allow easy conversion to passing an HVO to Accept.
	/// </summary>
	public class FilterBarCellFilter : RecordFilter
	{
		/// <summary>
		/// Normal constructor.
		/// </summary>
		public FilterBarCellFilter(IStringFinder finder, IMatcher matcher)
		{
			Finder = finder;
			Matcher = matcher;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public FilterBarCellFilter()
		{
		}

		/// <summary>
		/// Get the finder
		/// </summary>
		public IStringFinder Finder { get; private set; }

		/// <summary>
		/// Get the matcher.
		/// </summary>
		public IMatcher Matcher { get; private set; }

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			return Matcher.Accept(Finder.Key(item));
		}

		/// <summary>
		/// Let your finder preload whatever it wants to.
		/// </summary>
		public override void Preload(object rootObj)
		{
			Finder.Preload(rootObj);
		}

		/// <summary>
		/// Valid if the matcher is (finder doesn't yet have a way to be invalid)
		/// </summary>
		public override bool IsValid => base.IsValid && Matcher.IsValid();

		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		public override bool SameFilter(RecordFilter other)
		{
			var fbcOther = other as FilterBarCellFilter;
			return fbcOther != null && (fbcOther.Finder.SameFinder(Finder) && fbcOther.Matcher.SameMatcher(Matcher));
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			DynamicLoader.PersistObject(Finder, node, "finder");
			DynamicLoader.PersistObject(Matcher, node, "matcher");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Debug.Assert(Finder == null);
			Finder = DynamicLoader.RestoreFromChild(node, "finder") as IStringFinder;
			Matcher = DynamicLoader.RestoreFromChild(node, "matcher") as IMatcher;
		}

		#endregion

		/// <summary>
		/// Set the cache.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				SetCache(Finder, value);
				SetCache(Matcher, value);
			}
		}

		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				SetDataAccess(Finder, value);
				SetDataAccess(Matcher, value);
			}
		}

		/// <summary>
		/// These filters are always created by user action in the filter bar, and thus visible to the user.
		/// </summary>
		public override bool IsUserVisible => true;
	}

	/// <summary>
	/// An AndFilter is initialized with a sequence of other RecordFilters, and accepts anything
	/// they all accept. Typically there is one item for each non-blank cell in the FilterBar,
	/// plus possibly the original filter that helps define the collection we are viewing.
	/// </summary>
	public class AndFilter : RecordFilter
	{
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			foreach (RecordFilter f in Filters)
			{
				if (!f.Accept(item))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the filters.
		/// </summary>
		public ArrayList Filters { get; private set; } = new ArrayList();

		/// <summary>
		/// Adds the specified f.
		/// </summary>
		public void Add(RecordFilter f)
		{
			Debug.Assert(!Contains(f), "This filter (" + f + ") has already been added to the list.");
			Filters.Add(f);
		}

		/// <summary>
		/// Removes the specified f.
		/// </summary>
		public void Remove(RecordFilter f)
		{
			for (var i = 0; i < Filters.Count; ++i)
			{
				if (((RecordFilter)Filters[i]).SameFilter(f))
				{
					Filters.RemoveAt(i);
					break;
				}
			}
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count => Filters.Count;

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			foreach (RecordFilter rf in Filters)
			{
				DynamicLoader.PersistObject(rf, node, "filter");
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			Debug.Assert(Filters != null && Filters.Count == 0);
			Filters = new ArrayList(node.Elements().Count());
			foreach (var child in node.Elements())
			{
				Filters.Add(DynamicLoader.RestoreFromChild(child, "."));
			}
		}

		/// <summary>
		/// Set the cache.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				foreach (var obj in Filters)
				{
					SetCache(obj, value);
				}
			}
		}

		/// <summary>
		/// Pass it on to any subfilters that can use it.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				foreach (var obj in Filters)
				{
					if (obj is IStoresDataAccess)
					{
						((IStoresDataAccess)obj).DataAccess = value;
					}
				}
			}
		}

		/// <summary>
		///  An AndFilter is user-visible if ANY of its compoents is.
		/// </summary>
		public override bool IsUserVisible
		{
			get
			{
				return Filters.Cast<RecordFilter>().Any(f => f.IsUserVisible);
			}
		}

		/// <summary>
		/// Does our AndFilter Contain the other recordfilter or one equal to it? If so answer the equal one.
		/// </summary>
		public override RecordFilter EqualContainedFilter(RecordFilter other)
		{
			for (var i = 0; i < Count; ++i)
			{
				var filter = Filters[i] as RecordFilter;
				var result = filter.EqualContainedFilter(other);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// not certain we will want to continue to have this... it simplifies the task I have
	/// at hand, which is providing a way,from the menu bar to clear all filters
	///
	/// the RecordList will recognize this has been set and will actually clear
	/// its filter... so this will not actually be used and us what actually slow down showing everything.
	/// </summary>
	public class NullFilter : RecordFilter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NullFilter"/> class.
		/// </summary>
		public NullFilter()
		{
			Name = FiltersStrings.ksNoFilter;
			id = "No Filter";
		}

		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		/// <value>The name of the image.</value>
		public override string imageName => "NoFilter";

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			return true;
		}
	}

	/// <summary>
	/// A filter for the View/Filter menu/toolbar to turn off all filters.
	/// </summary>
	public class NoFilters : NullFilter
	{
		public NoFilters() : base()
		{
		}
	}

	/// <summary>
	/// A dummy filter to uncheck any selections in the View/Filters menu
	/// </summary>
	public class UncheckAll : NullFilter
	{
		public UncheckAll()
		{
			Name = FiltersStrings.ksUncheckAll;
		}
	}

	/// <summary>
	/// Interface implemented by RecordSorter if it can send percent done messages.
	/// </summary>
	public interface IReportsSortProgress
	{
		Action<int> SetPercentDone { get; set;}
	}

	/// <summary>
	/// Interface implemented by RecordList, indicating it can be told when comparisons occur.
	/// </summary>
	internal interface INoteComparision
	{
		void ComparisonOccurred();
	}
}
