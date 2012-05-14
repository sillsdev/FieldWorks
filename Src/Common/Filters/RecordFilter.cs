// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: .cs
// History: John Hatton, created
// Last reviewed:
//
// <remarks>
//	At the moment (July 2004), the only instances of this class do filtering in memory.
//	This does not imply that all filtering will always be done in memory, only that we haven't
//	yet designed or implemented a way to do the filtering while querying.
//		when we do, we will probably split RecordFilter into two subclasses, one for in memory
//	filters which can use FDO properties, and one which will somehow contribute to the Where
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
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Filters
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Event arguments for FilterChangeHandler event.
	/// Arguably, we could have separate events for adding and removing, but that would make it
	/// more difficult to avoid refreshing the list twice when switching from one filter to
	/// another. Arguably, both add and remove could be arrays. But so far there has been no
	/// need for this, and if we do, we can easily keep the current constructor but change
	/// the acessors, which are probably rather less used.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FilterChangeEventArgs
	{
		/// <summary></summary>
		private RecordFilter m_added;
		/// <summary></summary>
		private RecordFilter m_removed;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilterChangeEventArgs"/> class.
		/// </summary>
		/// <param name="added">The added RecordFilter.</param>
		/// <param name="removed">The removed RecordFilter.</param>
		/// ------------------------------------------------------------------------------------
		public FilterChangeEventArgs(RecordFilter added, RecordFilter removed)
		{
			m_added = added;
			m_removed = removed;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the added RecordFilter.
		/// </summary>
		/// <value>The added RecordFilter.</value>
		/// ------------------------------------------------------------------------------------
		public RecordFilter Added
		{
			get { return m_added; }
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the removed RecordFilter.
		/// </summary>
		/// <value>The removed RecordFilter.</value>
		/// ---------------------------------------------------------------------------------------
		public RecordFilter Removed
		{
			get { return m_removed; }
		}
	}

	/// <summary></summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void FilterChangeHandler(object sender, FilterChangeEventArgs e);
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for RecordFilter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class RecordFilter : IPersistAsXml, IStoresFdoCache, IStoresDataAccess,
		IAcceptsStringTable
	{

		/// <summary></summary>
		protected string m_name;
		protected string m_id;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RecordFilter"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RecordFilter()
		{
			m_name = FiltersStrings.ksUnknown;
			m_id = "Unknown";
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache on the specified object if it wants it.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		internal void SetCache(object obj, FdoCache cache)
		{
			var sfc = obj as IStoresFdoCache;
			if (sfc != null)
				sfc.Cache = cache;
		}

		internal void SetDataAccess(object obj, ISilDataAccess sda)
		{
			var sfc = obj as IStoresDataAccess;
			if (sfc != null)
				sfc.DataAccess = sda;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the string table on the specified object if it wants it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SetStringTable(object obj, StringTable table)
		{
			var target = obj as IAcceptsStringTable;
			if (target != null)
				target.StringTable = table;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get
			{
				return m_name;
			}
		}
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// this is used, for example, and persist in the selected id in the users xml preferences
		/// </summary>
		/// <value>The id.</value>
		/// ------------------------------------------------------------------------------------------
		public string id
		{
			get
			{
				return m_id;
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		/// <value>The name of the image.</value>
		/// ------------------------------------------------------------------------------------------
		public virtual string imageName
		{
			get
			{
				return "SimpleFilter";
			}
		}

		/// <summary>
		/// May be used to preload data for efficient filtering of many instances.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Accept(IManyOnePathSortItem item);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is user visible; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsUserVisible
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells whether the filter is currently valid.  This is true by default.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsValid
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// a factory method for filters
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="configuration">The configuration.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public RecordFilter Create (FdoCache cache, XmlNode configuration)
		{
			var filter = (RecordFilter)DynamicLoader.CreateObject(configuration);
			filter.Init(cache, configuration);
			return filter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterNode">The filter node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(FdoCache cache,XmlNode filterNode)
		{
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PersistAsXml(XmlNode node)
		{
			XmlUtils.AppendAttribute(node, "name", m_name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void InitXml(XmlNode node)
		{
			m_name = XmlUtils.GetManditoryAttributeValue(node, "name");
		}

		#endregion

		#region IStoresFdoCache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public virtual FdoCache Cache
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
		#region IAcceptsStringTable Members

		/// <summary>
		/// Subclasses override if they need one.
		/// </summary>
		public virtual StringTable StringTable
		{
			set
			{
				// default does nothing
			}
		}

		#endregion
	}
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// this filter passes CmAnnotations which are pointing at objects of the class listed
	/// in the targetClasses attribute.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProblemAnnotationFilter: RecordFilter
	{
		/// <summary></summary>
		protected List<int> m_classIds;

		private FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProblemAnnotationFilter"/> class.
		/// </summary>
		/// <remarks>must have a constructor with no parameters, to use with the dynamic loader
		/// or IPersistAsXml</remarks>
		/// ------------------------------------------------------------------------------------
		public ProblemAnnotationFilter()
		{
			m_classIds = new List<int>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "classIds", XmlUtils.MakeListValue(m_classIds));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_classIds = new List<int>(XmlUtils.GetMandatoryIntegerListAttributeValue(node, "classIds"));
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the class ids.
		/// </summary>
		/// <value>The class ids.</value>
		/// ------------------------------------------------------------------------------------------
		public List<int> ClassIds
		{
			get { return m_classIds; }
		}

		public override FdoCache Cache
		{
			set
			{
				m_cache = value;
				base.Cache = value;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filterNode"></param>
		/// ------------------------------------------------------------------------------------
		public override void Init(FdoCache cache,XmlNode filterNode)
		{
			base.Init(cache, filterNode);
			m_cache = cache;
			string classList =XmlUtils.GetManditoryAttributeValue(filterNode, "targetClasses");
			string[] classes= classList.Split(',');

			//enhance: currently, this will require that we name every subclass as well.
			foreach(string name in classes)
			{
				int cls = cache.DomainDataByFlid.MetaDataCache.GetClassId(name.Trim());
				if (cls <= 0)
					throw new SIL.Utils.ConfigurationException("The class name '" + name + "' is not valid");
				m_classIds.Add(cls);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item"></param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Accept (IManyOnePathSortItem item)
		{
			var obj = item.KeyObjectUsing(m_cache);
			if (!(obj is ICmBaseAnnotation))
				return false; // It's not a base annotation

			var annotation = (ICmBaseAnnotation)obj;
			if (annotation.BeginObjectRA == null)
				return false;

			int cls = annotation.BeginObjectRA.ClassID;
			foreach(uint i in m_classIds)
			{
				if ( i == cls)
					return true;
			}
			return false;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Matchers are able to tell whether a string matches a pattern.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IMatcher
	{
		/// <summary></summary>
		bool Accept(ITsString tssKey);
		/// <summary></summary>
		bool Matches(ITsString arg);
		/// <summary></summary>
		bool SameMatcher(IMatcher other);
		/// <summary></summary>
		bool IsValid();
		/// <summary></summary>
		string ErrorMessage();
		/// <summary></summary>
		bool CanMakeValid();
		/// <summary></summary>
		ITsString MakeValid();
		/// <summary></summary>
		ITsString Label { get; set; }
		/// <summary></summary>
		ILgWritingSystemFactory WritingSystemFactory { get; set; }
		/// <summary>
		/// If there is one specific writing system that the matcher looks for, return it;
		/// otherwise return 0.
		/// </summary>
		int WritingSystem { get; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a base class for matchers; so far it just implements storing the label.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class BaseMatcher : IMatcher, IPersistAsXml, IStoresFdoCache
	{
		ITsString m_tssLabel;
		// Todo: get this initialized somehow.
		ILgWritingSystemFactory m_wsf; // unfortunately needed for serialization
		// This is used only to save the value restored by InitXml until the Cache is set
		// so that m_tssLabel can be computed.
		string m_xmlLabel;

		#region IMatcher Members

		public abstract bool Matches(ITsString arg);

		public abstract bool SameMatcher(IMatcher other);
		/// <summary>
		/// No specific writing system for most of these matchers.
		/// </summary>
		public virtual int WritingSystem { get { return 0;} }

		public ITsString Label
		{
			get	{ return m_tssLabel; }
			set	{ m_tssLabel = value; }
		}

		public virtual bool IsValid()
		{
			return true;	// most matchers won't have to override this - regex is one that does though
		}

		public virtual string ErrorMessage()
		{
			return String.Format(FiltersStrings.ksErrorMsg, IsValid().ToString());
		}

		public virtual bool CanMakeValid()
		{
			return false;
		}

		public virtual ITsString MakeValid()
		{
			return null;	// should only be called if CanMakeValid it true and that class should implement it.
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set { m_wsf = value; }
			get { return m_wsf; }
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// This is overridden only by BlankMatcher currently, which matches
		/// on an empty list of strings.
		/// </summary>
		/// <param name="strings"></param>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public virtual bool Accept(ITsString tss)
		{
			return Matches(tss);
		}

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PersistAsXml(XmlNode node)
		{
			if (m_tssLabel != null)
			{
				string contents = TsStringUtils.GetXmlRep(m_tssLabel, m_wsf, 0, false);
				XmlUtils.AppendAttribute(node, "label", contents);
			}
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ---------------------------------------------------------------------------------------
		public virtual void InitXml(XmlNode node)
		{
			m_xmlLabel = XmlUtils.GetOptionalAttributeValue(node, "label");
		}

		#endregion

		#region Implementation of IStoresFdoCache

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public virtual FdoCache Cache
		{
			set
			{
				m_wsf = value.WritingSystemFactory;
				if (m_xmlLabel != null)
					m_tssLabel = TsStringSerializer.DeserializeTsStringFromXml(m_xmlLabel, m_wsf);
			}
		}

		#endregion
	}

	/// <summary>
	/// Class to keep track of the ichMin/ichLim sets resulting from a FindIn() match.
	/// </summary>
	public struct MatchRangePair
	{
		internal int m_ichMin;
		internal int m_ichLim;

		public MatchRangePair(int ichMin, int ichLim)
		{
			m_ichMin = ichMin;
			m_ichLim = ichLim;
		}

		public void Reset()
		{
			m_ichMin = -1;
			m_ichLim = -1;
		}

		public int IchMin
		{
			get { return m_ichMin; }
		}
		public int IchLim
		{
			get { return m_ichLim; }
		}
	}


	/// <summary>
	/// A base class for several kinds of matcher that do various kinds of string equality/inequality testing.
	/// </summary>
	public abstract class SimpleStringMatcher : BaseMatcher
	{
		/// <summary></summary>
		protected IVwPattern m_pattern;
		protected IVwTxtSrcInit m_textSourceInit;
		protected IVwTextSource m_ts;
		protected ITsString m_tssSource;
		protected const int m_MaxSearchStringLength = 1000;	// max lenthg of search string

		protected XmlNode m_persistNode;

		protected MatchRangePair m_currentMatchRangePair = new MatchRangePair(-1, -1);
		/// <summary>
		/// Cache for the Match set resulting from FindIn() for a string;
		/// </summary>
		protected List<MatchRangePair> m_results = new List<MatchRangePair>();

		/// <summary>
		/// normal constructor
		/// </summary>
		/// <param name="pattern"></param>
		public SimpleStringMatcher(IVwPattern pattern)
		{
			m_pattern = pattern;
			Init();
		}

		/// <summary>
		/// This class explicitly looks for a particular ws.
		/// </summary>
		public override int WritingSystem
		{
			get
			{
				if(Pattern != null && !String.IsNullOrEmpty(Pattern.IcuLocale) && WritingSystemFactory != null)
					return WritingSystemFactory.GetWsFromStr(Pattern.IcuLocale);
				return 0;
			}
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public SimpleStringMatcher()
		{
			Init();
		}

		private void Init()
		{
			m_textSourceInit = VwStringTextSourceClass.Create();
			m_ts = m_textSourceInit as IVwTextSource;

			if(m_pattern == null)
				m_pattern = VwPatternClass.Create();
		}

		/// <summary>
		/// Retrieve pattern (for testing)
		/// </summary>
		public IVwPattern Pattern
		{
			get { return m_pattern; }
		}

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
			bool found = false;
			do
			{   // get first/next match and make sure it is not the same segment of the string
				mrp = FindNextPatternMatch(mrpLast);
				if (mrpLast.Equals(mrp))
					break; // it found the same segment again: Prevent cycles, eg, for Reg Exp "$" (LT-7041).
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
			int ichStart = 0;
			// if we already have a current match, then reset the starting position
			// NOTE: there seems to be a bug(?) in FindIn that prevents us from using IchMin + 1 to find overlapping matches.
			if (lastMatch.IchMin >= 0) // see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation
				ichStart = lastMatch.IchLim;
			int ichMin;
			int ichLim;
			m_pattern.FindIn(m_ts, ichStart, m_tssSource.Length, true, out ichMin, out ichLim, null);
			return new MatchRangePair(ichMin, ichLim);
		}

		/// <summary>
		/// Override this method to match additional conditions not handled at the pattern level.
		/// </summary>
		/// <param name="match">The pattern-matched string segment limits to check against
		/// additional matching criteria.</param>
		/// <returns>true if the additional checks succeeded.</returns>
		abstract protected bool CurrentResultDoesMatch(MatchRangePair match);

		/// <summary>Gets all segments of the string that match the pattern. The caller must
		/// call Matches() first to check that there is at least one match. It also sets the
		/// first match this one returns.</summary>
		/// <returns>The list of all unique filter matches found.</returns>
		public List<MatchRangePair> GetAllResults()
		{
			m_results.Clear();
			Debug.Assert(m_currentMatchRangePair.IchMin >= 0, "SimpleStringMatcher.Matches() must set the first filter match.");
			m_results.Add(m_currentMatchRangePair);
			MatchRangePair mrpLast = m_currentMatchRangePair; // set via Matches()
			bool found = false;
			do
			{
				MatchRangePair mrp = FindNextPatternMatch(mrpLast);
				if (mrp.Equals(mrpLast))// presuming the only duplicate would be the last match that might be found ;-)
					break; // Prevent cycles, eg, for Reg Exp "$" (LT-7041).
				mrpLast = mrp;
				found = mrp.IchMin >= 0; // see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation
				if (found && CurrentResultDoesMatch(mrp)) // must match the overridden condition
					m_results.Add(mrp);
			} while (found);
			return m_results;
		}

		protected MatchRangePair CurrentResult
		{
			get { return m_currentMatchRangePair; }
		}


		#region IMatcher Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answers the question "Are there any matches?" so that if there aren't,
		/// time is not wasted looking for them. To get all the matches, call GetAllResults() next.
		/// </summary>
		/// <param name="arg">The string to apply the pattern match to.</param>
		/// <returns>true if a match was found</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			m_currentMatchRangePair.Reset();
			try
			{
				m_currentMatchRangePair = FindFirstMatch(arg);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
			// see VwPattern.cpp STDMETHODIMP VwPattern::FindIn documentation for *.IchMin >= 0
			return m_currentMatchRangePair.IchMin >= 0;
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// <remarks>For most subclasses, it is enough if it is the same class and pattern.</remarks>
		/// ---------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override bool SameMatcher(IMatcher other)
		{
			if (!(other is SimpleStringMatcher))
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (other.GetType() != this.GetType())
				return false;
			IVwPattern otherPattern = (other as SimpleStringMatcher).m_pattern;
			if (otherPattern.Pattern == null)
			{
				if (m_pattern.Pattern != null)
					return false;
			}
			else if (!otherPattern.Pattern.Equals(m_pattern.Pattern))
				return false;
			return otherPattern.MatchCase == m_pattern.MatchCase
				&& otherPattern.MatchDiacritics == m_pattern.MatchDiacritics;
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the matcher is valid.
		/// </summary>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public override bool IsValid()
		{
			if (HasError())
				return false;
			return base.IsValid();
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// If the error was in this object, then return the error msg for it, otherwise return
		/// the base error msg.
		/// </summary>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public override string ErrorMessage()
		{
			if (HasError())
			{
				return String.Format(FiltersStrings.ksMatchStringToLongLength0, m_MaxSearchStringLength );
			}
			return base.ErrorMessage();
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Does this object know how to make the matcher valid, in the case of SimpleStringMatcher
		/// it's just a matter of truncating the search string to be of a valid length.
		/// </summary>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public override bool CanMakeValid()
		{
			if (HasError())
				return true;
			return false;
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Truncate the match pattern to be of a valid length if that was the error.
		/// </summary>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public override ITsString MakeValid()
		{
			if (HasError())
			{
				return m_pattern.Pattern.GetSubstring(0, m_MaxSearchStringLength-1);
			}
			return base.MakeValid();
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Local method for testing if there is an error: currently it's just the length of the string
		/// </summary>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		private bool HasError()
		{
			if (m_pattern.Pattern.Length > m_MaxSearchStringLength)
				return true;
			return false;
		}

		#endregion

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ---------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml (node);
			XmlUtils.AppendAttribute(node, "pattern", m_pattern.Pattern.Text);
			int var;
			int ws = m_pattern.Pattern.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			XmlUtils.AppendAttribute(node, "ws", ws.ToString());
			XmlUtils.AppendAttribute(node, "matchCase", m_pattern.MatchCase.ToString());
			XmlUtils.AppendAttribute(node, "matchDiacritics", m_pattern.MatchDiacritics.ToString());
			// NOTE!! if any more properties of the matcher become significant, they should be
			// accounted for also in SameMatcher!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);

			m_persistNode = node;
		}

		// The Cache property finishes the initialization that was started with InitXML
		// We wait until here because the cache is needed to get the writing system
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;

				if(m_persistNode != null && m_pattern.Pattern == null)
				{
					ITsString tss;
					ITsStrFactory tsf = value.TsStrFactory;
					int ws = XmlUtils.GetOptionalIntegerValue(m_persistNode,
						"ws",
						value.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
					tss = tsf.MakeString(XmlUtils.GetManditoryAttributeValue(m_persistNode, "pattern"), ws);
					m_pattern.Pattern = tss;

					m_pattern.MatchCase = XmlUtils.GetOptionalBooleanAttributeValue(m_persistNode, "matchCase", false);
					m_pattern.MatchDiacritics = XmlUtils.GetOptionalBooleanAttributeValue(m_persistNode, "matchDiacritics", false);

					// These values are currently never set to anything other than false, initialize them that way
					m_pattern.MatchOldWritingSystem = false;
					m_pattern.MatchWholeWord = false;
					// UseRegularExpressions is always assumed to be false, the RegExpMatcher class sets it to true in the constructor
					m_pattern.UseRegularExpressions = false;
				}
			}
		}

	}

	/// -------------------------------------------------------------------------------------------
	/// <summary>
	/// Matches if the pattern is exactly the argument.
	/// </summary>
	/// -------------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			return base.Matches(arg);
		}

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
		/// <param name="pattern"></param>
		public BeginMatcher(IVwPattern pattern) : base(pattern) {}
		/// <summary>
		/// default for persistence
		/// </summary>
		public BeginMatcher() {}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ---------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			if(arg == null || arg.Length < m_pattern.Pattern.Length)
				return false;
			return base.Matches(arg);
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
		/// <param name="pattern"></param>
		public EndMatcher(IVwPattern pattern) : base(pattern) {}
		/// <summary>
		/// default for persistence
		/// </summary>
		public EndMatcher() {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			if(arg == null || arg.Length < m_pattern.Pattern.Length)
				return false;

			return base.Matches(arg);
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
		/// <param name="pattern"></param>
		public AnywhereMatcher(IVwPattern pattern) : base(pattern) {}
		/// <summary>
		/// default for persistence
		/// </summary>
		public AnywhereMatcher() {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			if(arg == null)
				return false;

			return base.Matches(arg);
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
		/// <param name="pattern"></param>
		public RegExpMatcher(IVwPattern pattern) : base(pattern)
		{
			Init();
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public RegExpMatcher() {}

		void Init()
		{
			m_pattern.UseRegularExpressions = true;
		}

		public override bool Matches(ITsString arg)
		{
			if(arg == null)
				return false;

			return base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin >= 0;
		}

		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				Init();
			}
		}

		public override bool IsValid()
		{
			string errMsg = m_pattern.ErrorMessage;
			if (errMsg == null && m_pattern.Pattern.Text != null)
				return base.IsValid();	// now see if there is a problem in the base classes
			return false;
		}

		public override string ErrorMessage()
		{
			string finalErrorMessage;
			string errMsg = m_pattern.ErrorMessage;
			if (m_pattern.Pattern.Text == null)
				errMsg = "U_REGEX_RULE_SYNTAX";

			// handle the case where the error msg has bubbled up from a base class
			if (errMsg == null)
			{
				if (base.IsValid() == false)
					return base.ErrorMessage();
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Matches any empty or null string, or one consisting entirely of white space
		/// characters. I think the .NET definition of white space is good enough; it's unlikely
		/// we'll need new PUA whitespace characters.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			if (arg == null || arg.Length == 0)
				return true;
			string text = arg.Text;
			for (int i = 0; i < text.Length; i++)
			{
				if (!System.Char.IsWhiteSpace(text[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The exact opposite of BlankMatcher.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			if (arg == null || arg.Length == 0)
				return false;
			string text = arg.Text;
			for (int i = 0; i < text.Length; i++)
			{
				if (!System.Char.IsWhiteSpace(text[i]))
					return true;
			}
			return false;
		}
		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameMatcher(IMatcher other)
		{
			return other is NonBlankMatcher;
		}
	}

	/// <summary>
	/// Matches if the embedded matcher fails.
	/// </summary>
	public class InvertMatcher : BaseMatcher, IAcceptsStringTable, IStoresFdoCache, IStoresDataAccess
	{
		IMatcher m_matcher;

		/// <summary>
		/// regular constructor
		/// </summary>
		/// <param name="matcher"></param>
		public InvertMatcher (IMatcher matcher)
		{
			m_matcher = matcher;
		}

		/// <summary>
		/// default for persistence.
		/// </summary>
		public InvertMatcher()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the matcher to invert.
		/// </summary>
		/// <value>The matcher to invert.</value>
		/// ------------------------------------------------------------------------------------
		public IMatcher MatcherToInvert
		{
			get { return m_matcher; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString arg)
		{
			return arg != null && !m_matcher.Matches(arg);
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameMatcher(IMatcher other)
		{
			InvertMatcher other2 = other as InvertMatcher;
			if (other2 == null)
				return false;
			return m_matcher.SameMatcher(other2.m_matcher);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml (node);
			DynamicLoader.PersistObject(m_matcher, node, "invertMatcher");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			m_matcher = DynamicLoader.RestoreFromChild(node, "invertMatcher") as IMatcher;
		}

		#region IAcceptsStringTable Members

		public StringTable StringTable
		{
			set
			{
				if (m_matcher is IAcceptsStringTable)
					(m_matcher as IAcceptsStringTable).StringTable = value;
			}
		}

		#endregion

		#region IStoresFdoCache Members

		FdoCache IStoresFdoCache.Cache
		{
			set
			{
				if (m_matcher is IStoresFdoCache)
					(m_matcher as IStoresFdoCache).Cache = value;
			}
		}

		ISilDataAccess IStoresDataAccess.DataAccess
		{
			set
			{
				if (m_matcher is IStoresDataAccess)
					(m_matcher as IStoresDataAccess).DataAccess = value;
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
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		string[] Strings(int hvo);
		/// <summary>
		/// Stringses the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd);
		string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd); // similar key more suitable for sorting.
		ITsString Key(IManyOnePathSortItem item);

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
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

	//------------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	//------------------------------------------------------------------------------------------
	public abstract class StringFinderBase : IStringFinder, IPersistAsXml, IStoresFdoCache
	{
		/// <summary></summary>
		protected ISilDataAccess m_sda;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringFinderBase"/> class.
		/// Default constructor for IPersistAsXml
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringFinderBase()
		{
		}

		/// <summary>
		/// Normal constructor for most uses.
		/// </summary>
		/// <param name="sda"></param>
		public StringFinderBase(ISilDataAccess sda)
		{
			m_sda = sda;
		}

		/// <summary>
		/// Default is to return the strings for the key object.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			string[] result = Strings(item.KeyObject);
			if (sortedFromEnd)
				for(int i = 0; i < result.Length; i++)
					result[i] = TsStringUtils.ReverseString(result[i]);

			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For most of these we want to return the same thing.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			return Strings(item, sortedFromEnd);
		}

		public ITsString Key(IManyOnePathSortItem item)
		{
			throw new NotImplementedException("Don't have new Key function implemented on class " + this.GetType());
		}


		public ISilDataAccess DataAccess
		{
			get { return m_sda; }
			set { m_sda = value; }
		}

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		public abstract string[] Strings(int hvo);
		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PersistAsXml(XmlNode node)
		{
			// nothing to do in base class
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void InitXml(XmlNode node)
		{
			// nothing to do in base class
		}

		#endregion

		#region IStoresFdoCache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			set
			{
				m_sda = value.DomainDataByFlid;
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
		int m_flid;
		int m_ws;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		public OwnMlPropFinder(ISilDataAccess sda, int flid, int ws)
			: base(sda)
		{
			m_flid = flid;
			m_ws = ws;
		}

		/// <summary>
		/// For persistence with IPersistAsXml
		/// </summary>
		public OwnMlPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The flid.</value>
		/// ------------------------------------------------------------------------------------
		public int Flid
		{
			get { return m_flid; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws.
		/// </summary>
		/// <value>The ws.</value>
		/// ------------------------------------------------------------------------------------
		public int Ws
		{
			get { return m_ws; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flid", m_flid.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] Strings(int hvo)
		{
			string val = m_sda.get_MultiStringAlt(hvo, m_flid, m_ws).Text;
			if (val == null)
				val = "";
			return new string[] {val};
		}

		/// <summary>
		/// Same if it is the same type for the same flid, ws, and DA.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			OwnMlPropFinder other2 = other as OwnMlPropFinder;
			if (other2 == null)
				return false;
			return other2.m_flid == this.m_flid && other2.m_sda == this.m_sda && other2.m_ws == this.m_ws;
		}

		#endregion
	}

	/// <summary>
	/// This class implements StringFinder by looking up one monlingual property of the object
	/// itself.
	/// </summary>
	public class OwnMonoPropFinder : StringFinderBase
	{
		int m_flid;

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="flid">The flid.</param>
		/// ------------------------------------------------------------------------------------------
		public OwnMonoPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			m_flid = flid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OwnMonoPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The flid.</value>
		/// ------------------------------------------------------------------------------------------
		public int Flid
		{
			get { return m_flid; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flid", m_flid.ToString());
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ---------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] Strings(int hvo)
		{
			string val = m_sda.get_StringProp(hvo, m_flid).Text;
			if (val == null)
				val = "";
			return new string[] {val};
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			OwnMonoPropFinder other2 = other as OwnMonoPropFinder;
			if (other2 == null)
				return false;
			return other2.m_flid == this.m_flid && other2.m_sda == this.m_sda;
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
		int m_flidVec;
		int m_flidString;
		int m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OneIndirectMlPropFinder"/> class.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="flidVec">The flid vec.</param>
		/// <param name="flidString">The flid string.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public OneIndirectMlPropFinder(ISilDataAccess sda, int flidVec, int flidString, int ws)
			: base(sda)
		{
			m_flidVec = flidVec;
			m_flidString = flidString;
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid vec.
		/// </summary>
		/// <value>The flid vec.</value>
		/// ------------------------------------------------------------------------------------
		public int FlidVec
		{
			get { return m_flidVec; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid string.
		/// </summary>
		/// <value>The flid string.</value>
		/// ------------------------------------------------------------------------------------
		public int FlidString
		{
			get { return m_flidString; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws.
		/// </summary>
		/// <value>The ws.</value>
		/// ------------------------------------------------------------------------------------
		public int Ws
		{
			get { return m_ws; }
		}
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectMlPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flidVec", m_flidVec.ToString());
			XmlUtils.AppendAttribute(node, "flidString", m_flidString.ToString());
			XmlUtils.AppendAttribute(node, "ws", m_ws.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_flidVec = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidVec");
			m_flidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			m_ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] Strings(int hvo)
		{
			int count = m_sda.get_VecSize(hvo, m_flidVec);
			string[] result = new string[count];
			for (int i = 0; i < count; ++i)
			{
				string val = m_sda.get_MultiStringAlt(m_sda.get_VecItem(hvo, m_flidVec, i), m_flidString, m_ws).Text;
				if (val == null)
					val = "";
				result[i] = val;
			}
			return result;
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			OneIndirectMlPropFinder other2 = other as OneIndirectMlPropFinder;
			if (other2 == null)
				return false;
			return other2.m_flidVec == this.m_flidVec && other2.m_sda == this.m_sda
				&& other2.m_flidString == this.m_flidString && other2.m_ws == this.m_ws;
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
		int[] m_flidVec;
		int m_flidString;
		int m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultiIndirectMlPropFinder"/> class.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="flidVec">The flid vec.</param>
		/// <param name="flidString">The flid string.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public MultiIndirectMlPropFinder(ISilDataAccess sda, int[] flidVec, int flidString, int ws)
			: base(sda)
		{
			m_flidVec = flidVec;
			m_flidString = flidString;
			m_ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public MultiIndirectMlPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vec flids.
		/// </summary>
		/// <value>The vec flids.</value>
		/// ------------------------------------------------------------------------------------
		public int[] VecFlids
		{
			get { return m_flidVec; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid string.
		/// </summary>
		/// <value>The flid string.</value>
		/// ------------------------------------------------------------------------------------
		public int FlidString
		{
			get { return m_flidString; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws.
		/// </summary>
		/// <value>The ws.</value>
		/// ------------------------------------------------------------------------------------
		public int Ws
		{
			get { return m_ws; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flidVec", XmlUtils.MakeIntegerListValue(m_flidVec));
			XmlUtils.AppendAttribute(node, "flidString", m_flidString.ToString());
			XmlUtils.AppendAttribute(node, "ws", m_ws.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_flidVec = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "flidVec");
			m_flidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			m_ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		// Return the number of values in the tree rooted at hvo, where index (into m_flidVec)
		// gives the index of the property which should be followed from that object.
		int CountItems(int hvo, int index)
		{
			int count = m_sda.get_VecSize(hvo, m_flidVec[index]);
			if (index == m_flidVec.Length - 1)
				return count;
			int total = 0;
			for (int i = 0; i < count; ++i)
				total += CountItems(m_sda.get_VecItem(hvo, m_flidVec[index], i), index + 1);
			return total;
		}

		/// <summary>
		/// Insert into results, starting at resIndex, the strings obtained from the
		/// tree rooted at hvo. flidVec[flidIndex] is the property to follow from hvo.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flidIndex"></param>
		/// <param name="results"></param>
		/// <param name="resIndex"></param>
		void GetItems(int hvo, int flidIndex, string[] results, ref int resIndex)
		{
			if (flidIndex == m_flidVec.Length)
			{
				// add the string for this leaf object
				string val = m_sda.get_MultiStringAlt(hvo, m_flidString, m_ws).Text;
				if (val == null)
					val = "";
				results[resIndex] = val;
				resIndex++;
			}
			else
			{
				int count = m_sda.get_VecSize(hvo, m_flidVec[flidIndex]);
				for (int i = 0; i < count; ++i)
				{
					GetItems(m_sda.get_VecItem(hvo, m_flidVec[flidIndex], i), flidIndex + 1, results, ref resIndex);
				}
			}
		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		public override string[] Strings(int hvo)
		{
			string[] result = new string[CountItems(hvo, 0)];
			int resIndex = 0;
			GetItems(hvo, 0, result, ref resIndex);
			return result;
		}

		bool SameVec(int[] first, int[] second)
		{
			if (first.Length != second.Length)
				return false;
			for (int i = 0; i < first.Length; ++i)
				if (first[i] != second[i])
					return false;
			return true;
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			MultiIndirectMlPropFinder other2 = other as MultiIndirectMlPropFinder;
			if (other2 == null)
				return false;
			return SameVec(other2.m_flidVec, this.m_flidVec) && other2.m_sda == this.m_sda
				&& other2.m_flidString == this.m_flidString && other2.m_ws == this.m_ws;
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
		int m_flidAtom;
		int m_flidString;
		int m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OneIndirectAtomMlPropFinder"/> class.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="flidAtom">The flid atom.</param>
		/// <param name="flidString">The flid string.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public OneIndirectAtomMlPropFinder(ISilDataAccess sda, int flidAtom, int flidString, int ws)
			: base(sda)
		{
			m_flidAtom = flidAtom;
			m_flidString = flidString;
			m_ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectAtomMlPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid atom.
		/// </summary>
		/// <value>The flid atom.</value>
		/// ------------------------------------------------------------------------------------
		public int FlidAtom
		{
			get { return m_flidAtom; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid string.
		/// </summary>
		/// <value>The flid string.</value>
		/// ------------------------------------------------------------------------------------
		public int FlidString
		{
			get { return m_flidString; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws.
		/// </summary>
		/// <value>The ws.</value>
		/// ------------------------------------------------------------------------------------
		public int Ws
		{
			get { return m_ws; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "flidAtom", m_flidAtom.ToString());
			XmlUtils.AppendAttribute(node, "flidString", m_flidString.ToString());
			XmlUtils.AppendAttribute(node, "ws", m_ws.ToString());
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_flidAtom = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidAtom");
			m_flidString = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flidString");
			m_ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] Strings(int hvo)
		{
			string val = m_sda.get_MultiStringAlt(m_sda.get_ObjectProp(hvo, m_flidAtom), m_flidString, m_ws).Text;
			if (val == null)
				val = "";
			return new string[] {val};
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			OneIndirectAtomMlPropFinder other2 = other as OneIndirectAtomMlPropFinder;
			if (other2 == null)
				return false;
			return other2.m_flidAtom == this.m_flidAtom && other2.m_sda == this.m_sda
				&& other2.m_flidString == this.m_flidString && other2.m_ws == this.m_ws;
		}

		#endregion
	}


	/// <summary>
	/// A FilterBarCellFilter handles one cell of a filter bar. It is made up of two components: a StringFinder
	/// specifies how to find target strings from the hvo of the target object, while a IMatcher
	/// specifies how to determine whether a string matches. An object passes if any of the target strings
	/// produced by the finder matches the pattern.
	///
	/// The design avoids using FDO objects as performance is likely to be critical. For a long list,
	/// it would be well to pre-load the cache with all relevant properties in as few queries as
	/// possible. It is designed to allow easy conversion to passing an HVO to Accept.
	/// </summary>
	public class FilterBarCellFilter : RecordFilter
	{
		private IStringFinder m_finder;
		private IMatcher m_matcher;

		/// <summary>
		/// Normal constructor.
		/// </summary>
		/// <param name="finder">A reference to a finder</param>
		/// <param name="matcher">A reference to a matcher</param>
		public FilterBarCellFilter(IStringFinder finder, IMatcher matcher)
		{
			m_finder = finder;
			m_matcher = matcher;
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
		public IStringFinder Finder
		{
			get { return m_finder; }
		}

		/// <summary>
		/// Get the matcher.
		/// </summary>
		public IMatcher Matcher
		{
			get { return m_matcher; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------------
		public override bool Accept(IManyOnePathSortItem item)
		{
			return Matcher.Accept(m_finder.Key(item));
		}

		/// <summary>
		/// Let your finder preload whatever it wants to.
		/// </summary>
		public override void Preload(object rootObj)
		{
			m_finder.Preload(rootObj);
		}

		/// <summary>
		/// Valid if the matcher is (finder doesn't yet have a way to be invalid)
		/// </summary>
		public override bool IsValid
		{
			get
			{
				return base.IsValid && m_matcher.IsValid();
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		public override bool SameFilter(RecordFilter other)
		{
			FilterBarCellFilter fbcOther = other as FilterBarCellFilter;
			if (fbcOther == null)
				return false;
			return fbcOther.m_finder.SameFinder(m_finder) && fbcOther.m_matcher.SameMatcher(m_matcher);
		}

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			DynamicLoader.PersistObject(m_finder, node, "finder");
			DynamicLoader.PersistObject(m_matcher, node, "matcher");
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			Debug.Assert(m_finder == null);
			m_finder = DynamicLoader.RestoreFromChild(node, "finder") as IStringFinder;
			m_matcher = DynamicLoader.RestoreFromChild(node, "matcher") as IMatcher;
		}

		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				SetCache(m_finder, value);
				SetCache(m_matcher, value);
			}
		}

		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				SetDataAccess(m_finder, value);
				SetDataAccess(m_matcher, value);
			}
		}

		/// <summary>
		/// Pass the string finder to children that may want it.
		/// </summary>
		public override StringTable StringTable
		{
			set
			{
				base.StringTable = value;
				SetStringTable(m_finder, value);
				SetStringTable(m_matcher, value);
			}
		}

		/// <summary>
		/// These filters are always created by user action in the filter bar, and thus visible to the user.
		/// </summary>
		public override bool IsUserVisible
		{
			get
			{
				return true;
			}
		}
	}

	/// <summary>
	/// An AndFilter is initialized with a sequence of other RecordFilters, and accepts anything
	/// they all accept. Typically there is one item for each non-blank cell in the FilterBar,
	/// plus possibly the original filter that helps define the collection we are viewing.
	/// </summary>
	public class AndFilter : RecordFilter
	{
		/// <summary>references to filters</summary>
		private ArrayList m_filters = new ArrayList();

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------------
		public override bool Accept(IManyOnePathSortItem item)
		{
			foreach (RecordFilter f in m_filters)
				if (!f.Accept(item))
					return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filters.
		/// </summary>
		/// <value>The filters.</value>
		/// ------------------------------------------------------------------------------------------
		public ArrayList Filters
		{
			get
			{
				return m_filters;
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified f.
		/// </summary>
		/// <param name="f">The f.</param>
		/// ------------------------------------------------------------------------------------------
		public void Add(RecordFilter f)
		{
			Debug.Assert(!this.Contains(f), "This filter (" + f + ") has already been added to the list.");
			m_filters.Add(f);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified f.
		/// </summary>
		/// <param name="f">The f.</param>
		/// ------------------------------------------------------------------------------------------
		public void Remove(RecordFilter f)
		{
			for (int i = 0; i < m_filters.Count; ++i)
			{
				if (((RecordFilter)m_filters[i]).SameFilter(f))
				{
					var filterToRemove = m_filters[i];
					m_filters.RemoveAt(i);
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count.
		/// </summary>
		/// <value>The count.</value>
		/// ------------------------------------------------------------------------------------------
		public int Count
		{
			get
			{
				return m_filters.Count;
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			foreach(RecordFilter rf in m_filters)
				DynamicLoader.PersistObject(rf, node, "filter");
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			Debug.Assert(m_filters != null && m_filters.Count == 0);
			m_filters = new ArrayList(node.ChildNodes.Count);
			foreach (XmlNode child in node.ChildNodes)
			{
				var filter = DynamicLoader.RestoreFromChild(child, ".");
				m_filters.Add(filter);
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Set the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				foreach(object obj in m_filters)
					SetCache(obj, value);
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
				foreach (object obj in m_filters)
				{
					if (obj is IStoresDataAccess)
						((IStoresDataAccess)obj).DataAccess = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Set the string table.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------------
		public override StringTable  StringTable
		{
			set
			{
				base.StringTable = value;
				foreach (object obj in m_filters)
					SetStringTable(obj, value);
			}
		}

		/// <summary>
		///  An AndFilter is user-visible if ANY of its compoents is.
		/// </summary>
		public override bool IsUserVisible
		{
			get
			{
				return m_filters.Cast<RecordFilter>().Any(f => f.IsUserVisible);
			}
		}

		/// <summary>
		/// Does our AndFilter Contain the other recordfilter or one equal to it? If so answer the equal one.
		/// </summary>
		public override RecordFilter EqualContainedFilter(RecordFilter other)
		{
			for (int i = 0; i < Count; ++i)
			{
				RecordFilter filter = this.Filters[i] as RecordFilter;
				var result = filter.EqualContainedFilter(other);
				if (result != null)
					return result;
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
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NullFilter"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public NullFilter()
		{
			m_name = FiltersStrings.ksNoFilter;
			m_id = "No Filter";
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		/// <value>The name of the image.</value>
		/// ------------------------------------------------------------------------------------------
		public override string imageName
		{
			get
			{
				return "NoFilter";
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------------
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
			m_name = FiltersStrings.ksUncheckAll;
		}
	}

	/// <summary>
	/// Interface implemented by finders which require a string table.
	/// </summary>
	public interface IAcceptsStringTable
	{
		StringTable StringTable { set; }
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
