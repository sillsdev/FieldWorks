// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FindReplaceCollectorEnvBase.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FindReplaceCollectorEnvBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for specialized find and replace collector classes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FindReplaceCollectorEnvBase : CollectorEnv
	{
		#region Data members
		/// <summary></summary>
		protected IVwViewConstructor m_vc;
		/// <summary></summary>
		protected int m_frag;
		/// <summary></summary>
		protected IVwPattern m_Pattern;
		/// <summary></summary>
		protected IVwTxtSrcInit2 m_textSourceInit;
		/// <summary></summary>
		protected IVwSearchKiller m_searchKiller;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FindReplaceCollectorEnvBase"/> class.
		/// </summary>
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		/// ------------------------------------------------------------------------------------
		public FindReplaceCollectorEnvBase(IVwViewConstructor vc, ISilDataAccess sda,
			int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(null, sda, hvoRoot)
		{
			m_vc = vc;
			m_frag = frag;
			m_Pattern = vwPattern;
			m_searchKiller = searchKiller;
			m_textSourceInit = VwMappedTxtSrcClass.Create();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor - Initializes a new instance of the
		/// <see cref="T:FindReplaceCollectorEnvBase"/> class.
		/// </summary>
		/// <param name="fc">The FindReplaceCollectorEnvBase object to clone.</param>
		/// ------------------------------------------------------------------------------------
		public FindReplaceCollectorEnvBase(FindReplaceCollectorEnvBase fc) :
			this(fc.m_vc, fc.m_sda, fc.m_hvoCurr, fc.m_frag, fc.m_Pattern, fc.m_searchKiller)
		{
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if we don't need to process any further. Some methods may be able
		/// to truncate operations.
		/// </summary>
		/// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		protected override bool Finished
		{
			get
			{
				if (m_searchKiller == null)
					return false;

				m_searchKiller.FlushMessages();
				return m_searchKiller.AbortRequest;
			}
		}
		#endregion
	}
	#endregion

	#region ReplaceAllCollectorEnv class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles replacing text
	/// </summary>
	/// <remarks>The current implementation doesn't work for different styles, tags, and WSs
	/// that are applied by the VC.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class ReplaceAllCollectorEnv : FindReplaceCollectorEnvBase
	{
		#region Data Members
		private int m_cReplace;
		private bool m_fEmptySearch;
		private Stack<bool> m_ReadOnlyStack = new Stack<bool>();
		private bool m_fReadOnly = false;
		/// <summary>ORCs to move after the replaced text</summary>
		private ITsString m_ORCsToMove;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FindReplaceCollectorEnvBase"/> class.
		/// </summary>
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		/// ------------------------------------------------------------------------------------
		public ReplaceAllCollectorEnv(IVwViewConstructor vc, ISilDataAccess sda,
			int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(vc, sda, hvoRoot, frag, vwPattern, searchKiller)
		{
			m_ReadOnlyStack.Push(m_fReadOnly);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces all.
		/// </summary>
		/// <returns>Number of replacements made</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int ReplaceAll()
		{
			m_fEmptySearch = (m_Pattern.Pattern.Length == 0);
			m_vc.Display(this, m_hvoCurr, m_frag);
			return m_cReplace;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the string.
		/// </summary>
		/// <param name="tsbBuilder">The string builder for the text to be replaced.</param>
		/// <param name="tssInput">The input string to be replaced.</param>
		/// <param name="ichMinInput">The start in the input string.</param>
		/// <param name="ichLimInput">The lim in the input string.</param>
		/// <param name="tssReplace">The replacement text. This should come from VwPattern.ReplacementText,
		/// NOT VwPattern.ReplaceWith. The former includes any ORCs that need to be saved from the input, as well as
		/// properly handling $1, $2 etc. in regular expressions.</param>
		/// <param name="delta">length difference between tssInput and tsbBuilder from previous
		/// replacements.</param>
		/// <param name="fEmptySearch"><c>true</c> if search text is empty (irun.e. we're searching
		/// for a style or Writing System)</param>
		/// <param name="fUseWs">if set to <c>true</c> use the writing system used in the
		/// replace string of the Find/Replace dialog.</param>
		/// <returns>Change in length of the string.</returns>
		/// ------------------------------------------------------------------------------------
		public static int ReplaceString(ITsStrBldr tsbBuilder, ITsString tssInput,
			int ichMinInput, int ichLimInput, ITsString tssReplace, int delta, bool fEmptySearch,
			bool fUseWs)
		{
			int initialLength = tsbBuilder.Length;
			int replaceRunCount = tssReplace.RunCount;

			// Determine whether to replace the sStyleName. We do this if any of the runs of
			// the replacement string have the sStyleName set (to something other than
			// Default Paragraph Characters).
			bool fUseStyle = false;
			bool fUseTags = false;

			// ENHANCE (EberhardB): If we're not doing a RegEx search we could store these flags
			// since they don't change.
			TsRunInfo runInfo;
			for (int irunReplace = 0; irunReplace < replaceRunCount; irunReplace++)
			{
				ITsTextProps textProps = tssReplace.FetchRunInfo(irunReplace, out runInfo);
				string sStyleName =
					textProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (sStyleName != null && sStyleName.Length > 0)
					fUseStyle = true;

				//string tags = textProps.GetStrPropValue((int)FwTextPropType.ktptTags);
				//if (tags.Length > 0)
				//    fUseTags = true;
			}

			int iRunInput = tssInput.get_RunAt(ichMinInput);
			ITsTextProps selProps = tssInput.get_Properties(iRunInput);
			ITsPropsBldr propsBldr = selProps.GetBldr();

			// Remove all tags that are anywhere in the Find-what string. But also include any
			// other tags that are present in the first run of the found string. So the resulting
			// replacement string will have any tags in the first char of the selection plus
			// any specified replacement tags.
			//			Vector<StrUni> vstuTagsToRemove;
			//			GetTagsToRemove(m_qtssFindWhat, &fUseTags, vstuTagsToRemove);
			//			Vector<StrUni> vstuTagsToInclude;
			//			GetTagsToInclude(qtssSel, vstuTagsToRemove, vstuTagsToInclude);

			// Make a string builder to accumulate the real replacement string.

			// Copy the runs of the replacement string, adjusting the properties.
			// Make a string builder to accumulate the real replacement string.
			ITsStrBldr stringBldr = TsStrBldrClass.Create();

			// Copy the runs of the replacement string, adjusting the properties.
			for (int irun = 0; irun < replaceRunCount; irun++)
			{
				ITsTextProps ttpReplaceRun = tssReplace.FetchRunInfo(irun, out runInfo);
				if (StringUtils.GetGuidFromRun(tssReplace, irun) != Guid.Empty)
				{
					// If the run was a footnote or picture ORC, then just use the run
					// properties as they are.
				}
				else if (fUseWs || fUseStyle || fUseTags)
				{
					// Copy only writing system/old writing system, char sStyleName and/or
					// tag info into the builder.
					if (fUseWs)
					{
						int ttv, ws;
						ws = ttpReplaceRun.GetIntPropValues((int)FwTextPropType.ktptWs, out ttv);
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, ttv, ws);
					}
					if (fUseStyle)
					{
						string sStyleName = ttpReplaceRun.GetStrPropValue(
							(int)FwTextPropType.ktptNamedStyle);

						if (sStyleName == FwStyleSheet.kstrDefaultCharStyle)
							propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
								null);
						else
							propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
								sStyleName);
					}
					//if (fUseTags)
					//{
					//    string sTagsRepl = ttpReplaceRun.GetStrPropValue(ktptTags);
					//    string sTags = AddReplacementTags(vstuTagsToInclude, sTagsRepl);
					//    propsBldr.SetStrPropValue(ktptTags, sTags);
					//}
					ttpReplaceRun = propsBldr.GetTextProps();
				}
				else
				{
					// Its not a footnote so copy all props exactly from (the first run of the) matched text.
					ttpReplaceRun = selProps;
				}

				// Insert modified run into string builder.
				if (fEmptySearch && tssReplace.Length == 0)
				{
					// We are just replacing an ws/ows/sStyleName/tags. The text remains unchanged.
					// ENHANCE (SharonC): Rework this when we get patterns properly implemented.
					string runText = tssInput.get_RunText(iRunInput);
					if (runText.Length > ichLimInput - ichMinInput)
						runText = runText.Substring(0, ichLimInput - ichMinInput);
					stringBldr.Replace(0, 0, runText, ttpReplaceRun);
				}
				else
				{
					stringBldr.Replace(runInfo.ichMin, runInfo.ichMin,
						tssReplace.get_RunText(irun), ttpReplaceRun);
				}
			}

			tsbBuilder.ReplaceTsString(delta + ichMinInput, delta + ichLimInput, stringBldr.GetString());
			int finalLength = tsbBuilder.Length;
			return finalLength - initialLength;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ORC that need to be moved to the end of the text that was replaced.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString ORCsToMove
		{
			get { return m_ORCsToMove; }
			set { m_ORCsToMove = value; }
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			ITsString tss = DoReplace(m_sda.get_StringProp(m_hvoCurr, tag));
			if (tss != null)
			{
				m_sda.SetString(m_hvoCurr, tag, tss);
				// We shouldn't have to do this in the new FDO
				//m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoCurr, tag,
				//    0, tss.Length, tss.Length);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			ITsString tss = DoReplace(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws));
			if (tss != null)
			{
				m_sda.SetMultiStringAlt(m_hvoCurr, tag, ws, tss);
				// We shouldn't have to do this in the new FDO
				//// For multi-string properties, the "ivMin" parameter to PropChanged is
				//// really the writing system HVO (per documentation in idh file).
				//m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoCurr, tag,
				//    ws, tss.Length, tss.Length);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the object.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="ihvo">The ihvo.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OpenTheObject(int hvo, int ihvo)
		{
			base.OpenTheObject(hvo, ihvo);
			m_ReadOnlyStack.Push(m_fReadOnly);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CloseTheObject()
		{
			m_ReadOnlyStack.Pop();
			m_fReadOnly = m_ReadOnlyStack.Peek();
			base.CloseTheObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// <param name="tpt"></param>
		/// <param name="tpv"></param>
		/// <param name="nValue"></param>
		/// ------------------------------------------------------------------------------------
		public override void set_IntProperty(int tpt, int tpv, int nValue)
		{
			if (tpt == (int)FwTextPropType.ktptEditable)
				m_fReadOnly |= (nValue == (int)TptEditable.ktptNotEditable);
			base.set_IntProperty(tpt, tpv, nValue);
		}
		#endregion // Overrides

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the find or replace.
		/// </summary>
		/// <param name="tss">The original string.</param>
		/// <returns>The replaced string.</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString DoReplace(ITsString tss)
		{
			if (tss == null || tss.Length == 0)
				return null;

			m_textSourceInit.SetString(tss, m_vc, m_sda.WritingSystemFactory);

			IVwTextSource textSource = m_textSourceInit as IVwTextSource;
			int ichMinLog, ichLimLog;
			ITsStrBldr tsb = null;
			int cch = tss.Length; // length of old string
			int delta = 0; // length difference between new and old string

			for (int ichStartLog = 0; ichStartLog <= cch; )
			{
				m_Pattern.FindIn(textSource, ichStartLog, cch, true, out ichMinLog, out ichLimLog, null);
				if (ichMinLog < 0)
					break;
				if (tsb == null)
					tsb = tss.GetBldr();

				if (IsEditable(tss, ichMinLog, ichLimLog))
				{
					delta += ReplaceString(tsb, tss, ichMinLog, ichLimLog, m_Pattern.ReplacementText,
						delta, m_fEmptySearch, m_Pattern.MatchOldWritingSystem);
					m_cReplace++;
				}
				ichStartLog = ichLimLog;
			}

			if (tsb == null)
				return null;

			return tsb.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified string is editable.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">The ich lim.</param>
		/// <returns>
		/// 	<c>true</c> if the specified string is editable; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsEditable(ITsString tss, int ichMin, int ichLim)
		{
			if (m_fReadOnly)
				return false;

			int irunLim = tss.get_RunAt(ichLim);
			if (tss.get_RunAt(ichMin) == irunLim)
				irunLim++;
			for (int irun = tss.get_RunAt(ichMin); irun < irunLim; irun++)
			{
				ITsTextProps ttp = tss.get_Properties(irun);
				int nVar;
				if (ttp != null)
				{
					int nVal = ttp.GetIntPropValues((int)FwTextPropType.ktptEditable, out nVar);
					if (nVal == (int)TptEditable.ktptNotEditable ||
						nVal == (int)TptEditable.ktptSemiEditable)
						return false;
				}
			}
			return true;
		}
	}
	#endregion // ReplaceAllCollectorEnv

	#region FindCollectorEnv class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles finding text
	/// </summary>
	/// <remarks>The current implementation doesn't work for different styles, tags, and WSs
	/// that are applied by the VC.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class FindCollectorEnv : FindReplaceCollectorEnvBase
	{
		#region Data members
		/// <summary>Found match location</summary>
		protected LocationInfo m_LocationFound;
		/// <summary>Location where find next should stop because it has wrapped around</summary>
		protected LocationInfo m_LimitLocation;
		/// <summary>Location to start current find next</summary>
		protected LocationInfo m_StartLocation;
		/// <summary>True if we have passed the limit, false otherwise</summary>
		protected bool m_fHitLimit;
		/// <summary>True if the find has already wrapped, false otherwise</summary>
		protected bool m_fHaveWrapped = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FindCollectorEnv"/> class.
		/// </summary>
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		/// ------------------------------------------------------------------------------------
		public FindCollectorEnv(IVwViewConstructor vc, ISilDataAccess sda,
			int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(vc, sda, hvoRoot, frag, vwPattern, searchKiller)
		{
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next occurence.
		/// </summary>
		/// <param name="startLocation">The selection level information, property tag and
		/// character offset that represent the location where the search should start.</param>
		/// <returns>
		/// A LocationInfo thingy if a match was found, otherwise <c>null</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public LocationInfo FindNext(LocationInfo startLocation)
		{
			m_StartLocation = startLocation;
			m_LocationFound = null;
			m_fHitLimit = false;

			Reset(); // Just in case
			// Enhance JohnT: if we need to handle more than one root object, this would
			// be one place to loop over them.
			m_vc.Display(this, m_hvoCurr, m_frag);

			if (m_LocationFound == null && m_fHitLimit)
				m_LimitLocation = null;
			else if (m_LimitLocation == null)
				m_LimitLocation = new LocationInfo(startLocation);
			return m_LocationFound;
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether to search this object based on whether we've reached our start
		/// location yet.
		/// </summary>
		/// <param name="hvoItem">The hvo item.</param>
		/// <param name="tag">The tag.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool DisplayThisObject(int hvoItem, int tag)
		{
			// We want to skip the beginning until we reach our start location.
			if (m_StartLocation == null || Finished)
				return true;

			int cPropPrev = CPropPrev(tag);

			foreach (SelLevInfo lev in m_StartLocation.m_location)
			{
				if (lev.tag == tag && lev.cpropPrevious == cPropPrev)
					return lev.hvo == hvoItem;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to check whether the start location or the limit location is in a literal
		/// string or some other added property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CheckForNonPropInfo()
		{
			if (Finished)
				return;

			if (m_fGotNonPropInfo)
			{
				// This should clear the m_fGotNonPropInfo flag and increment the count of
				// props for the ktagGapInAttrs
				base.CheckForNonPropInfo();

				// If our start location was in the object we just added (which isn't checked
				// by the normal find code), we need to set the start location to null so that
				// the find code will start looking for a match.
				CheckForStartLocationAndLimit((int)VwSpecialAttrTags.ktagGapInAttrs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the string. Overridden to check for strings that were added inside of an
		/// open property.
		/// </summary>
		/// <param name="tss">The TSS.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddString(ITsString tss)
		{
			base.AddString(tss);

			if (!m_fGotNonPropInfo)
			{
				// We actually had a prop open already, but we still need to do the checks for
				// this string. In this case m_tagCurrent should hold the tag that belongs to
				// the open property.
				CheckForStartLocationAndLimit(m_tagCurrent);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (Finished)
				return;

			base.AddStringProp(tag, _vwvc);

			if (m_StartLocation != null && !CurrentLocationIsStartLocation(tag))
				return;

			DoFind(m_sda.get_StringProp(m_hvoCurr, tag), tag);

			// We now processed the start location, so continue normally
			m_StartLocation = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			if (Finished)
				return;

			base.AddStringAltMember(tag, ws, _vwvc);

			if (m_StartLocation != null && !CurrentLocationIsStartLocation(tag))
				return;

			DoFind(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws), tag);

			// We now processed the start location, so continue normally
			m_StartLocation = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if we don't need to process any further. Some methods may be able
		/// to truncate operations.
		/// </summary>
		/// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		protected override bool Finished
		{
			get { return m_LocationFound != null || m_fHitLimit || base.Finished; }
		}
		#endregion // Overrides

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether a match was found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FoundMatch
		{
			get	{ return (m_LocationFound != null); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether find stopped at limit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StoppedAtLimit
		{
			get { return m_fHitLimit; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the find has already wrapped around
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasWrapped
		{
			set { m_fHaveWrapped = value; }
			get { return m_fHaveWrapped; }
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the property at the current location is the starting
		/// location.
		/// NOTE: This method will return false if there is no start location
		/// </summary>
		/// <param name="tag">The tag of the property.</param>
		/// <returns>True if it is the starting location, false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool CurrentLocationIsStartLocation(int tag)
		{
			return (m_StartLocation != null &&
				CurrentStackIsSameAsLocationInfo(m_StartLocation, tag));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the current stack loacation and the specified tag match the location
		/// specified in the given LocationInfo
		/// </summary>
		/// <param name="info">The LocationInfo to check.</param>
		/// <param name="tag">The tag of the current property.</param>
		/// <returns>True if the location is the same, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool CurrentStackIsSameAsLocationInfo(LocationInfo info, int tag)
		{
			if (info.m_location.Length != m_stack.Count)
				return false;

			// If we haven't gotten to the same occurrence of the same object property, we haven't
			// hit the starting point.
			for (int lev = 0; lev < m_stack.Count; lev++)
			{
				SelLevInfo limInfo = info.m_location[lev];
				// NOTE: the information in our m_stack variable and the information stored in
				// the selection levels are in opposite order.
				int iourStackLev = m_stack.Count - lev - 1;
				StackItem stackInfo = m_stack[iourStackLev];
				int cPrevProps = (iourStackLev > 0 ? m_stack[iourStackLev - 1].m_cpropPrev.GetCount(stackInfo.m_tag) :
					m_cpropPrev.GetCount(stackInfo.m_tag));

				if (limInfo.tag != stackInfo.m_tag || limInfo.cpropPrevious != cPrevProps ||
					limInfo.hvo != stackInfo.m_hvo)
				{
					return false; // Can't be at the same location
				}
			}

			// ENHANCE: If we ever need to handle multiple root objects, we'd need to check
			// ihvoRoot here.
			return (info.m_tag == tag && info.m_cpropPrev == CPropPrev(tag));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if the current location is the starting location. It will also check
		/// to see if the current location is the limit (or passed the limit).
		/// NOTE: This method doesn't check any character position as it should only be used for
		/// properties that won't be searched (i.e. that the find will skip over)
		/// </summary>
		/// <param name="tag">The tag of the current object.</param>
		/// ------------------------------------------------------------------------------------
		protected void CheckForStartLocationAndLimit(int tag)
		{
			if (Finished)
				return;

			if (CurrentLocationIsStartLocation(tag))
				m_StartLocation = null;
			else if (m_StartLocation == null && m_LimitLocation != null)
			{
				// Pass in -1 because we don't care about the character position
				if (PassedLimit(tag, -1))
					m_fHitLimit = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the find.
		/// </summary>
		/// <param name="tss">The original string.</param>
		/// <param name="tag">Tag</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DoFind(ITsString tss, int tag)
		{
			m_textSourceInit.SetString(tss, m_vc, m_sda.WritingSystemFactory);

			IVwTextSource textSource = m_textSourceInit as IVwTextSource;
			int ichBegin = 0;
			if (m_StartLocation != null)
			{
				Debug.Assert(m_StartLocation.TopLevelHvo == m_hvoCurr && m_StartLocation.m_tag == tag);
				ichBegin = m_StartLocation.m_ichLim;
			}

			int ichMin, ichLim;
			// When we re-wrote the find stuff to use this FindCollectorEnv, we removed some
			// whacky code from the FwFindReplaceDlg to try to deal with a sporadic failure
			// reported as TE-4085. We're no longer even calling the same method on vwPattern,
			// but if this failure ever recurs, this is probably the place where we'd want to
			// put a try/catch block so we could retry the find.
			m_Pattern.FindIn(textSource, ichBegin, tss.Length, true, out ichMin,
				out ichLim, null);
			if (PassedLimit(tag, ichMin))
			{
				m_fHitLimit = true;
				return;
			}
			if (ichMin >= 0)
				m_LocationFound = new LocationInfo(m_stack, CountOfPrevPropAtRoot, tag,
					ichMin, ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see whether we have passed the limit so we can stop searching and not just
		/// go on and on endlessly in an infinite loop forever and ever until the user gets fed
		/// up and throws the computer out the window.
		/// </summary>
		/// <param name="tag">The tag of the property whose string is being searched</param>
		/// <param name="testIch">The character offset position being tested. May be -1 if
		/// no match was found in this string, in which case we treat it as being beyond the
		/// limit if this string is the string that contains the limit.</param>
		/// <returns><c>true</c> if we passed the limit; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool PassedLimit(int tag, int testIch)
		{
			Debug.Assert(!m_fHitLimit);

			// If we don't have a limit, we're still looking for our start position.
			if (m_LimitLocation == null)
				return false;

			// If our start location is after the limit then we haven't hit the limit
			if (m_StartLocation != null && m_StartLocation.m_ichLim >= m_LimitLocation.m_ichMin &&
				!m_fHaveWrapped)
				return false;

			if (!CurrentStackIsSameAsLocationInfo(m_LimitLocation, tag))
				return false;

			// We are back in the same string. If we have hit or passed the limit offset, then
			// return true
			return (testIch < 0 || testIch >= m_LimitLocation.m_ichMin);
		}
		#endregion
	}
	#endregion // FindCollectorEnv

	#region ReverseFindCollectorEnv
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// <remarks>The current implementation doesn't work for different styles, tags, and WSs
	/// that are applied by the VC.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class ReverseFindCollectorEnv : FindCollectorEnv
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ReverseFindCollectorEnv"/> class.
		/// </summary>
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		/// ------------------------------------------------------------------------------------
		public ReverseFindCollectorEnv(IVwViewConstructor vc, ISilDataAccess sda,
			int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(vc, sda, hvoRoot, frag, vwPattern, searchKiller)
		{
		}

		#region Overriden protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the find.
		/// </summary>
		/// <param name="tss">The original string.</param>
		/// <param name="tag">Tag</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoFind(ITsString tss, int tag)
		{
			m_textSourceInit.SetString(tss, m_vc, m_sda.WritingSystemFactory);

			IVwTextSource textSource = m_textSourceInit as IVwTextSource;
			int ichBegin = textSource.LengthSearch;
			if (m_StartLocation != null)
			{
				Debug.Assert(m_StartLocation.TopLevelHvo == m_hvoCurr && m_StartLocation.m_tag == tag);
				ichBegin = m_StartLocation.m_ichMin;
			}

			int ichMin, ichLim;
			// When we re-wrote the find stuff to use this FindCollectorEnv, we removed some
			// whacky code from the FwFindReplaceDlg to try to deal with a sporadic failure
			// reported as TE-4085. We're no longer even calling the same method on vwPattern,
			// but if this failure ever recurs, this is probably the place where we'd want to
			// put a try/catch block so we could retry the find.
			m_Pattern.FindIn(textSource, ichBegin, 0, false, out ichMin, out ichLim, null);
			if (PassedLimit(tag, ichMin))
			{
				m_fHitLimit = true;
				return;
			}
			if (ichMin >= 0)
				m_LocationFound = new LocationInfo(m_stack, CPropPrev(tag), tag, ichMin, ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see whether we have passed the limit so we can stop searching and not just
		/// go on and on endlessly in an infinite loop forever and ever until the user gets fed
		/// up and throws the computer out the window.
		/// </summary>
		/// <param name="tag">The tag of the property whose string is being searched</param>
		/// <param name="testIch">The character offset position being tested. May be -1 if
		/// no match was found in this string, in which case we treat it as being beyond the
		/// limit if this string is the string that contains the limit.</param>
		/// <returns><c>true</c> if we passed the limit; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool PassedLimit(int tag, int testIch)
		{
			Debug.Assert(!m_fHitLimit);

			// If we don't have a limit, we're still looking for our start position.
			if (m_LimitLocation == null)
				return false;

			// If our start location is after the limit then we haven't hit the limit
			if (m_StartLocation != null && m_StartLocation.m_ichLim <= m_LimitLocation.m_ichMin &&
				!m_fHaveWrapped)
				return false;

			// If we haven't gotten to the same occurrence of the same object property, we haven't
			// hit the limit.
			if (m_LimitLocation.TopLevelHvo != m_hvoCurr || m_LimitLocation.m_tag != tag ||
				m_LimitLocation.m_cpropPrev != CPropPrev(tag))
			{
				return false;
			}

			// We are back in the same string. If we have hit or passed the limit offset, then
			// return true;
			return (testIch < 0 || testIch <= m_LimitLocation.m_ichLim);
		}
		#endregion
	}
	#endregion
}
