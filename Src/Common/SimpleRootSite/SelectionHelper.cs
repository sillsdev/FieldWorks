// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SelectionHelper.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrapper for all the information that a text selection contains.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class SelectionHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Types of selection limits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum SelLimitType
		{
			/// <summary>
			/// Use this to retrieve info about the limit of the selection which is closest
			/// (logically) to the top of the view.
			/// </summary>
			Top,
			/// <summary>
			/// Use this to retrieve info about the limit of the selection which is closest
			/// (logically) to the bottom of the view.
			/// </summary>
			Bottom,
			/// <summary>
			/// Use this to retrieve info about the anchor of the selection (i.e., where the
			/// user clicked to start the selection).
			/// </summary>
			Anchor,
			/// <summary>
			/// Use this to retrieve info about the end of the selection (i.e., the location
			/// to which the user shift-clicked, shift-arrowed, or ended their mouse drag to
			/// extend the selection).
			/// </summary>
			End,
		}

		#region SelInfo class
		/// <summary>Contains information about a selection</summary>
		[Serializable]
		public class SelInfo
		{
			/// <summary>Index of the root object</summary>
			public int ihvoRoot;
			/// <summary>Number of previous properties</summary>
			public int cpropPrevious;
			/// <summary>Character index</summary>
			public int ich;
			/// <summary>Writing system</summary>
			public int ws;
			/// <summary>The tag of the text property selected. </summary>
			public int tagTextProp = (int)StTxtPara.StTxtParaTags.kflidContents;
			/// <summary>
			/// Text Props associated with the selection itself. This can be different
			/// from the properties of the text where the selection is. This allows a
			/// selection (most likely an insertion point) to have properties set that
			/// will be applied if the user starts typing.
			/// </summary>
			[NonSerialized]
			public ITsTextProps ttpSelProps;
			/// <summary>IP associated with characters before current position</summary>
			public bool fAssocPrev = true;
			/// <summary>Index of end HVO</summary>
			public int ihvoEnd = -1;
			/// <summary>Level information</summary>
			public SelLevInfo[] rgvsli = new SelLevInfo[0];

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Creates a new object of type <see cref="SelInfo"/>.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public SelInfo()
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Copy constructor
			/// </summary>
			/// <param name="src">The source object</param>
			/// --------------------------------------------------------------------------------
			public SelInfo(SelInfo src)
			{
				if (src == null)
					return;

				tagTextProp =  src.tagTextProp;
				ihvoRoot = src.ihvoRoot;
				cpropPrevious = src.cpropPrevious;
				ich = src.ich;
				ws = src.ws;
				fAssocPrev = src.fAssocPrev;
				ihvoEnd = src.ihvoEnd;
				rgvsli = new SelLevInfo[src.rgvsli.Length];
				Array.Copy(src.rgvsli, rgvsli, src.rgvsli.Length);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares <paramref name="s1"/> with <paramref name="s2"/>
			/// </summary>
			/// <param name="s1"></param>
			/// <param name="s2"></param>
			/// <returns><c>true</c> if s1 is less than s2, otherwise <c>false</c></returns>
			/// <remarks>Both objects must have the same number of levels, otherwise
			/// an <see cref="ArgumentException"/> will be thrown.</remarks>
			/// --------------------------------------------------------------------------------
			public static bool operator < (SelInfo s1, SelInfo s2)
			{
				if (s1.rgvsli.Length != s2.rgvsli.Length)
					throw new ArgumentException("Number of levels differs");

				for (int i = s1.rgvsli.Length - 1; i >= 0; i--)
				{
					if (s1.rgvsli[i].tag != s2.rgvsli[i].tag)
						throw new ArgumentException("Differing tags");
					if (s1.rgvsli[i].ihvo > s2.rgvsli[i].ihvo)
						return false;
					else if (s1.rgvsli[i].ihvo == s2.rgvsli[i].ihvo)
					{
						if (s1.rgvsli[i].cpropPrevious > s2.rgvsli[i].cpropPrevious)
							return false;
						else if (s1.rgvsli[i].cpropPrevious < s2.rgvsli[i].cpropPrevious)
							return true;

					}
					else
						return true;
				}

				if (s1.ihvoRoot > s2.ihvoRoot)
					return false;
				else if (s1.ihvoRoot == s2.ihvoRoot)
				{
					if (s1.cpropPrevious > s2.cpropPrevious)
						return false;
					else if (s1.cpropPrevious == s2.cpropPrevious)
					{
						if (s1.ich >= s2.ich)
							return false;
					}
					else
						return true;
				}

				return true;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares <paramref name="s1"/> with <paramref name="s2"/>
			/// </summary>
			/// <param name="s1"></param>
			/// <param name="s2"></param>
			/// <returns><c>true</c> if s1 is greater than s2, otherwise <c>false</c></returns>
			/// <remarks>Both objects must have the same number of levels, otherwise
			/// <c>false</c> will be returned.</remarks>
			/// --------------------------------------------------------------------------------
			public static bool operator > (SelInfo s1, SelInfo s2)
			{
				return s2 < s1;
			}
		}
		#endregion

		#region Data members
		private SelInfo[] m_selInfo = new SelInfo[2];
		private int m_iTop = -1;
		private int m_ihvoEnd = -1;
		private bool m_fEndSet = false;
		/// <summary>Used for testing: holds SelectionHelper mock</summary>
		public static SelectionHelper s_mockedSelectionHelper = null;

		/// <summary>The distance the IP is from the top of the view</summary>
		[NonSerialized]
		protected int m_dyIPTop = 0;

		[NonSerialized]
		private IVwRootSite m_rootSite;

		[NonSerialized]
		private IVwSelection m_vwSel;

		#endregion

		#region Construction and Initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The default constructor must be followed by a call to SetSelection before it will
		/// really be useful
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SelectionHelper()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a selection helper based on an existing selection
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected SelectionHelper(IVwSelection vwSel, IVwRootSite rootSite)
		{
			m_vwSel = vwSel;
			if (vwSel != null)
				m_fEndSet = vwSel.IsRange;
			RootSite = rootSite;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="src">The source object</param>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper(SelectionHelper src)
		{
			m_selInfo[0] = new SelInfo(src.SelectionInfo[0]);
			m_selInfo[1] = new SelInfo(src.SelectionInfo[1]);
			m_iTop = src.m_iTop;
			m_ihvoEnd = src.m_ihvoEnd;
			RootSite = src.RootSite;
			m_vwSel = src.m_vwSel;
			m_fEndSet = src.m_fEndSet;
			m_dyIPTop = src.m_dyIPTop;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a SelectionHelper with the information about the current selection.
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <returns>A new <see cref="SelectionHelper"/> object</returns>
		/// -----------------------------------------------------------------------------------
		public static SelectionHelper Create(IVwRootSite rootSite)
		{
			return GetSelectionInfo(null, rootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a SelectionHelper with the information about the current selection.
		/// </summary>
		/// <param name="vwSel">The selection to create a SelectionHelper object from or
		/// null to create it from the given RootSite</param>
		/// <param name="rootSite">The root site</param>
		/// <returns>A new <see cref="SelectionHelper"/> object</returns>
		/// ------------------------------------------------------------------------------------
		public static SelectionHelper Create(IVwSelection vwSel, IVwRootSite rootSite)
		{
			return GetSelectionInfo(vwSel, rootSite);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets all information about a selection by calling <c>IVwSelection.AllTextSelInfo</c>.
		/// </summary>
		/// <param name="vwSel">The selection to get info for, or <c>null</c> to get current
		/// selection.</param>
		/// <param name="rootSite">The root site</param>
		/// <returns>A new <see cref="SelectionHelper"/> object</returns>
		/// -----------------------------------------------------------------------------------
		public static SelectionHelper GetSelectionInfo(IVwSelection vwSel, IVwRootSite rootSite)
		{
			if (s_mockedSelectionHelper != null)
				return s_mockedSelectionHelper;

			if (vwSel == null || !vwSel.IsValid)
			{
				if (rootSite == null || rootSite.RootBox == null)
					return null;
				vwSel = rootSite.RootBox.Selection;
				if (vwSel == null || !vwSel.IsValid)
					return null;
			}

			Debug.Assert(vwSel.IsValid);
			SelectionHelper helper = new SelectionHelper(vwSel, rootSite);

			if (!helper.GetSelEndInfo(false))
				return null;
			if (!helper.GetSelEndInfo(true))
				return null;

			return helper;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get information about the selection
		/// </summary>
		/// <param name="fEnd"><c>true</c> to get information about the end of the selection,
		/// otherwise <c>false</c>.</param>
		/// <returns><c>true</c> if information retrieved, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		private bool GetSelEndInfo(bool fEnd)
		{
			int i = fEnd ? 1 : 0;
			int cvsli = m_vwSel.CLevels(fEnd) - 1;
			//Review TE Team (JT-JH): We changed this from if (cvsli <= 0). We're guessing there just isn't any 0 cases in TE?
			if (cvsli < 0)
				cvsli = 0;
				//return false;

			if (m_selInfo[i] == null)
				m_selInfo[i] = new SelInfo();

			using (ArrayPtr prgvsli = MarshalEx.ArrayToNative(cvsli, typeof(SelLevInfo)))
			{
				m_vwSel.AllSelEndInfo(fEnd, out m_selInfo[i].ihvoRoot, cvsli, prgvsli,
					out m_selInfo[i].tagTextProp, out m_selInfo[i].cpropPrevious, out m_selInfo[i].ich,
					out m_selInfo[i].ws, out m_selInfo[i].fAssocPrev, out m_selInfo[i].ttpSelProps);
				m_selInfo[i].rgvsli = (SelLevInfo[]) MarshalEx.NativeToArray(prgvsli, cvsli,
					typeof(SelLevInfo));
			}

			if (fEnd)
				m_fEndSet = true;
			return true;
		}
		#endregion

		#region Methods to get selection properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified flid is located in the level info for the selection
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="limitType">Type of the limit.</param>
		/// <returns>true if the specified flid is found, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsFlidInLevelInfo(int flid, SelLimitType limitType)
		{
			SelLevInfo[] info = GetLevelInfo(limitType);
			for (int i = 0; i < info.Length; i++)
			{
				if (info[i].tag == flid)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the selection level info for the specified tag found in the level info for
		/// this selection.
		/// NOTE: This version only searches the anchor for the specified tag.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <returns>The level info for the specified tag</returns>
		/// <exception cref="Exception">Thrown if the specified tag is not found in the level
		/// info for the Anchor of the selection</exception>
		/// ------------------------------------------------------------------------------------
		public SelLevInfo GetLevelInfoForTag(int tag)
		{
			return GetLevelInfoForTag(tag, SelLimitType.Anchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the selection level info for the specified tag found in the level info for
		/// this selection.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <param name="limitType">The limit of the selection to search for the specified tag
		/// </param>
		/// <returns>The level info for the specified tag</returns>
		/// <exception cref="Exception">Thrown if the specified tag is not found in the level
		/// info of the selection</exception>
		/// ------------------------------------------------------------------------------------
		public SelLevInfo GetLevelInfoForTag(int tag, SelLimitType limitType)
		{
			SelLevInfo selLevInfo;
			if (GetLevelInfoForTag(tag, limitType, out selLevInfo))
				return selLevInfo;
			throw new Exception("No selection level had the requested tag.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the selection level info for the specified tag found in the level info for
		/// this selection.
		/// NOTE: This version only searches the anchor for the specified tag.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <param name="selLevInfo">The level info for the specified tag, if found; undefined
		/// otherwise</param>
		/// <returns><c>true</c>if the specified tag is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetLevelInfoForTag(int tag, out SelLevInfo selLevInfo)
		{
			return GetLevelInfoForTag(tag, SelLimitType.Anchor, out selLevInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the selection level info for the specified tag found in the level info for
		/// this selection.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <param name="limitType">The limit of the selection to search for the specified tag
		/// </param>
		/// <param name="selLevInfo">The level info for the specified tag, if found; undefined
		/// otherwise</param>
		/// <returns><c>true</c>if the specified tag is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetLevelInfoForTag(int tag, SelLimitType limitType,
			out SelLevInfo selLevInfo)
		{
			SelLevInfo[] info = GetLevelInfo(limitType);
			for (int i = 0; i < info.Length; i++)
			{
				if (info[i].tag == tag)
				{
					selLevInfo = info[i];
					return true;
				}
			}
			selLevInfo = new SelLevInfo();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the level number of the specified tag for this selection.
		/// NOTE: This version only searches the anchor for the specified tag.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <returns>The level number of the specified tag for this selection, or -1 if it
		/// could not be found</returns>
		/// ------------------------------------------------------------------------------------
		public int GetLevelForTag(int tag)
		{
			return GetLevelForTag(tag, SelLimitType.Anchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the level number of the specified tag for this selection.
		/// </summary>
		/// <param name="tag">The field tag to search for
		/// (i.e. BaseStText.StTextTags.kflidParagraphs)</param>
		/// <param name="limitType">The limit of the selection to search for the specified tag
		/// </param>
		/// <returns>The level number of the specified tag for this selection, or -1 if it
		/// could not be found</returns>
		/// ------------------------------------------------------------------------------------
		public int GetLevelForTag(int tag, SelLimitType limitType)
		{
			SelLevInfo[] info = GetLevelInfo(limitType);
			for (int i = 0; i < info.Length; i++)
			{
				if (info[i].tag == tag)
					return i;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection props for the current selection
		/// </summary>
		/// <param name="vttp">Returned array of ITsTextProps in the selection</param>
		/// <param name="vvps">Returned array of IVwPropertyStores in the selection</param>
		/// ------------------------------------------------------------------------------------
		public void GetCurrSelectionProps(out ITsTextProps[] vttp, out IVwPropertyStore[] vvps)
		{
			int cttp;
			GetSelectionProps(Selection, out vttp, out vvps, out cttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the location of the selection
		/// TODO: Make this work in PrintLayout view
		/// </summary>
		/// <returns>point on the screen of the anchor of the selection</returns>
		/// ------------------------------------------------------------------------------------
		public Point GetLocation()
		{
			// TODO: what if selection anchor is off the screen?

			IVwGraphics viewGraphics;
			Rect rectangleSource, rectangleDestination;

			RootSite.GetGraphics(m_rootSite.RootBox, out viewGraphics,
				out rectangleSource, out rectangleDestination);
			Rect rectanglePrimary, rectangleSecondary;
			try
			{
				bool isSplit;
				bool shouldEndBeforeAnchor;

				m_vwSel.Location(viewGraphics, rectangleSource, rectangleDestination,
					out rectanglePrimary, out rectangleSecondary, out isSplit,
					out shouldEndBeforeAnchor);

			}
			finally
			{
				RootSite.ReleaseGraphics(m_rootSite.RootBox, viewGraphics);
			}
			Point location = new Point(rectanglePrimary.left, rectanglePrimary.top);
			return location;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection props for the specified IVwSelection
		/// </summary>
		/// <param name="vwSel">The view Selection</param>
		/// <param name="vttp">Returned array of ITsTextProps in the selection</param>
		/// <param name="vvps">Returned array of IVwPropertyStores in the selection</param>
		/// <param name="cttp">Returned count of TsTxtProps (this is basically just the number
		/// of runs in the selection)</param>
		/// ------------------------------------------------------------------------------------
		public static void GetSelectionProps(IVwSelection vwSel, out ITsTextProps[] vttp,
			out IVwPropertyStore[] vvps, out int cttp)
		{
			Debug.Assert(vwSel != null);
			vttp = null;
			vvps = null;
			cttp = 0;
			if (!vwSel.IsValid)
				return;

			// The first call to GetSelectionProps gets the count of properties
			vwSel.GetSelectionProps(0, ArrayPtr.Null, ArrayPtr.Null, out cttp);
			if (cttp == 0)
				return;

			using (ArrayPtr pvTtp = MarshalEx.ArrayToNative(cttp, typeof(ITsTextProps)))
			{
				using (ArrayPtr pvVps = MarshalEx.ArrayToNative(cttp, typeof(IVwPropertyStore)))
				{
					vwSel.GetSelectionProps(cttp, pvTtp, pvVps, out cttp);
					vttp = (ITsTextProps[])MarshalEx.NativeToArray(pvTtp, cttp,
						typeof(ITsTextProps));
					vvps = (IVwPropertyStore[])MarshalEx.NativeToArray(pvVps, cttp,
						typeof(IVwPropertyStore));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is like GetSelectionProps, except that the vvpsSoft items include all
		/// formatting MINUS the hard formatting in the text properties (ie, those derived by
		/// the view constructor and styles).
		/// </summary>
		/// <param name="vwSel">The view Selection</param>
		/// <param name="vttp">Returned array of ITsTextProps in the selection</param>
		/// <param name="vvpsSoft">Returned array of IVwPropertyStores in the selection</param>
		/// <param name="cttp">Returned count of properties</param>
		/// ------------------------------------------------------------------------------------
		public static void GetHardAndSoftCharProps(IVwSelection vwSel,
			out ITsTextProps[] vttp, out IVwPropertyStore[] vvpsSoft, out int cttp)
		{
			vttp = null;
			vvpsSoft = null;

			GetSelectionProps(vwSel, out vttp, out vvpsSoft, out cttp);

			if (cttp == 0)
				return;

			using (ArrayPtr pvTtp = MarshalEx.ArrayToNative(cttp, typeof(ITsTextProps)))
			{
				using (ArrayPtr pvVps = MarshalEx.ArrayToNative(cttp, typeof(IVwPropertyStore)))
				{
					vwSel.GetHardAndSoftCharProps(cttp, pvTtp, pvVps, out cttp);
					vttp = (ITsTextProps[])MarshalEx.NativeToArray(pvTtp, cttp,
						typeof(ITsTextProps));
					vvpsSoft = (IVwPropertyStore[])MarshalEx.NativeToArray(pvVps, cttp,
						typeof(IVwPropertyStore));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph properties for the specified IVwSelection
		/// </summary>
		/// <param name="vwSel">The view Selection</param>
		/// <param name="vvps">Returned array of IVwPropertyStores in the selection</param>
		/// <param name="cvps">Returned count of IVwPropertyStores</param>
		/// ------------------------------------------------------------------------------------
		public static void GetParaProps(IVwSelection vwSel, out IVwPropertyStore[] vvps,
			out int cvps)
		{
			vvps = null;
			vwSel.GetParaProps(0, ArrayPtr.Null, out cvps);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(cvps, typeof(IVwPropertyStore)))
			{
				vwSel.GetParaProps(cvps, arrayPtr, out cvps);
				vvps = (IVwPropertyStore[])MarshalEx.NativeToArray(arrayPtr, cvps,
					typeof(IVwPropertyStore));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TsString of the property where the given selection limit is located. This
		/// gets the entire TSS of the paragraph, not just the selected portion.
		/// </summary>
		/// <param name="limit">the part of the selection where tss is to be retrieved (top,
		/// bottom, end, anchor)</param>
		/// <returns>
		/// the TsString containing the given limit of this Selection, or null if the selection
		/// is not in a paragraph.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString GetTss(SelLimitType limit)
		{
			try
			{
				ITsString tss;
				int ich, hvoObj, tag, ws;
				bool fAssocPrev;
				Selection.TextSelInfo(IsEnd(limit), out tss, out ich, out fAssocPrev,
					out hvoObj, out tag, out ws);
				return tss;
			}
			catch
			{
				return null;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the language writing system used by the given selection. If the selection
		/// has no writing system or contains more than one writing system, zero is returned.
		/// </summary>
		/// <param name="vwsel">The selection</param>
		/// <returns>The writing system of the selection, or zero if selection is null or
		/// has no writing system or there are multiple writing systems in the selection.
		/// </returns>
		/// <remarks>ENHANCE JohnT (DaveO): This should this be a COM method for IVwSelection
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public static int GetWsOfEntireSelection(IVwSelection vwsel)
		{
			return GetWsOfSelection(vwsel, false);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the first language writing system used by the given selection.
		/// </summary>
		/// <param name="vwsel">The selection</param>
		/// <returns>The first writing system in the selection, or zero if selection is null or
		/// has no writing system</returns>
		/// <remarks>ENHANCE JohnT: Should this be a COM method for IVwSelection?</remarks>
		/// -----------------------------------------------------------------------------------
		public static int GetFirstWsOfSelection(IVwSelection vwsel)
		{
			return GetWsOfSelection(vwsel, true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the implementation for GetWsOfEntireSelection and GetFirstWsOfSelection. It
		/// retrieves the language writing system used by the given selection.
		/// </summary>
		/// <param name="vwsel">The selection</param>
		/// <param name="fStopAtFirstWs"><c>true</c> if caller only cares about the first ws
		/// found; <c>false</c> if the caller wants a ws that represents the whole selection
		/// (if any)</param>
		/// <returns>The first writing system in the selection</returns>
		/// <remarks>ENHANCE JohnT: Should this be a COM method for IVwSelection?</remarks>
		/// -----------------------------------------------------------------------------------
		private static int GetWsOfSelection(IVwSelection vwsel, bool fStopAtFirstWs)
		{
			if (vwsel == null)
				return 0;

			try
			{
				ITsTextProps[] vttp;
				IVwPropertyStore[] vvps;
				int cttp;

				int wsSaveFirst = -1;

				SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

				if (cttp == 0)
					return 0;

				foreach (ITsTextProps ttp in vttp)
				{
					int var;
					int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs,
						out var);
					if (ws != -1)
					{
						if (fStopAtFirstWs)
							return ws;
						// This will set wsSave the first time we find a ws in a run
						if (wsSaveFirst == -1)
							wsSaveFirst = ws;
						else if (wsSaveFirst != ws)
						{
							// Multiple writing systems selected
							return 0;
						}
					}
				}

				// On the off chance we fund no writing systems at all, just return zero. It
				// should be safe because we can't have an editable selection where we would
				// really use the information.
				return wsSaveFirst != -1 ? wsSaveFirst : 0;
			}
			catch(System.Runtime.InteropServices.ExternalException objException)
			{
				string msg = string.Format(objException.ErrorCode.ToString() + " " +
					objException.GetBaseException().Source.ToString() + "\n\n" +
					objException.Message + "\n" +
					objException.TargetSite.ToString() + "\n\n" +
					objException.StackTrace.ToString(),
					objException.Source.ToString());

				Debug.Assert(false, msg);
				return 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if selection is editable
		/// </summary>
		/// <param name="sel">Selection</param>
		/// <returns><c>true</c> if selection is editable, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsEditable(IVwSelection sel)
		{
			return sel.CanFormatChar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if text that belongs to ttp and vps is editable.
		/// </summary>
		/// <param name="ttp"></param>
		/// <param name="vps"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsEditable(ITsTextProps ttp, IVwPropertyStore vps)
		{
			int nVar;
			int nVal = -1;
			if (ttp != null)
				nVal = ttp.GetIntPropValues((int)FwTextPropType.ktptEditable,
					out nVar);

			if (nVal == -1 && vps != null)
				nVal = vps.get_IntProperty((int)FwTextPropType.ktptEditable);

			if (nVal == (int)TptEditable.ktptNotEditable ||
				nVal == (int)TptEditable.ktptSemiEditable)
				return false;

			return true;
		}
		#endregion

		#region ReduceSelectionToIp methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reduce a range selection to a simple insertion point, specifying which limit of
		/// the range selection to use as the position for the new IP.
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <param name="limit">Specify Top to place the IP at the top-most limit of the
		/// selection. Specify Bottom to place the IP at the bottom-most limit of the selection.
		/// Specify Anchor to place the IP at the point where the user initiated the selection.
		/// Specify End to place the IP at the point where the user completed the selection. Be
		/// aware the user may select text in either direction, thus the end of the selection\
		/// could be visually before the anchor. For a simple insertion point or a selection
		/// entirely within a single StText, this parameter doesn't actually make any
		/// difference.</param>
		/// <param name="fMakeVisible">Indicates whether to scroll the IP into view.</param>
		/// ------------------------------------------------------------------------------------
		public static SelectionHelper ReduceSelectionToIp(IVwRootSite rootSite, SelLimitType limit,
			bool fMakeVisible)
		{
			return ReduceSelectionToIp(rootSite, limit, fMakeVisible, true);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reduce a range selection to a simple insertion point, specifying which limit of
		/// the range selection to use as the position for the new IP.
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <param name="limit">Specify Top to place the IP at the top-most limit of the
		/// selection. Specify Bottom to place the IP at the bottom-most limit of the selection.
		/// Specify Anchor to place the IP at the point where the user initiated the selection.
		/// Specify End to place the IP at the point where the user completed the selection. Be
		/// aware the user may select text in either direction, thus the end of the selection\
		/// could be visually before the anchor. For a simple insertion point or a selection
		/// entirely within a single StText, this parameter doesn't actually make any
		/// difference.</param>
		/// <param name="fMakeVisible">Indicates whether to scroll the IP into view.</param>
		/// <param name="fInstall">True to install the created selection, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		public static SelectionHelper ReduceSelectionToIp(IVwRootSite rootSite, SelLimitType limit,
			bool fMakeVisible, bool fInstall)
		{
			SelectionHelper helper = SelectionHelper.Create(rootSite);
			if (helper == null)
				return null;

			return helper.ReduceSelectionToIp(limit, fMakeVisible, fInstall);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the contrary limit
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private SelLimitType ContraryLimit(SelLimitType limit)
		{
			switch(limit)
			{
				case SelLimitType.Anchor:
					return SelLimitType.End;
				case SelLimitType.End:
					return SelLimitType.Anchor;
				case SelLimitType.Top:
					return SelLimitType.Bottom;
				case SelLimitType.Bottom:
					return SelLimitType.Top;
			}
			return SelLimitType.Anchor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reduces this selection to an insertion point at the specified limit.
		/// Will not install or make visible.
		/// </summary>
		/// <param name="limit">The current selection limit to reduce to</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ReduceToIp(SelLimitType limit)
		{
			ReduceToIp(limit, false, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reduces this selection to an insertion point at the specified limit.
		/// </summary>
		/// <param name="limit">The current selection limit to reduce to</param>
		/// <param name="fMakeVisible">Indicates whether to scroll the IP into view.</param>
		/// <param name="fInstall">True to install the created selection, false otherwise</param>
		/// <returns>The selection that was created</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection ReduceToIp(SelLimitType limit, bool fMakeVisible, bool fInstall)
		{
			SelLimitType newLimit = ContraryLimit(limit);

			// set the information for the IP for the other limit
			SetTextPropId(newLimit, GetTextPropId(limit));
			SetNumberOfPreviousProps(newLimit, GetNumberOfPreviousProps(limit));
			SetIch(newLimit, GetIch(limit));
			SetIhvoRoot(newLimit, GetIhvoRoot(limit));
			SetAssocPrev(newLimit, GetAssocPrev(limit));
			SetWritingSystem(newLimit, GetWritingSystem(limit));
			SetNumberOfLevels(newLimit, GetNumberOfLevels(limit));
			SetLevelInfo(newLimit, GetLevelInfo(limit));

			return SetSelection(m_rootSite, fInstall, fMakeVisible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the workhorse that actually reduces a range selection to a simple insertion
		/// point, given the specified index to indicate the limit where the IP is to be
		/// created.
		/// </summary>
		/// <param name="limit">Specify Top to place the IP at the top-most limit of the
		/// selection. Specify Bottom to place the IP at the bottom-most limit of the selection.
		/// Specify Anchor to place the IP at the point where the user initiated the selection.
		/// Specify End to place the IP at the point where the user completed the selection. Be
		/// aware the user may select text in either direction, thus the end of the selection\
		/// could be visually before the anchor. For a simple insertion point or a selection
		/// entirely within a single StText, this parameter doesn't actually make any
		/// difference.</param>
		/// <param name="fMakeVisible">Indicates whether to scroll the IP into view.</param>
		/// <param name="fInstall">True to install the created selection, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		protected virtual SelectionHelper ReduceSelectionToIp(SelLimitType limit, bool fMakeVisible,
			bool fInstall)
		{
			SelectionHelper textSelHelper = new SelectionHelper(this);
			textSelHelper.ReduceToIp(limit);

			// and make the selection
			if (fInstall)
				textSelHelper.SetSelection(m_rootSite, true, fMakeVisible);
			return textSelHelper;
		}
		#endregion

		#region Methods to create and set selections
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection by calling <c>IVwRootBox.MakeRangeSelection</c>.
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <param name="fInstall">Makes the selection the current selection</param>
		/// <param name="fMakeVisible">Determines whether or not to make the selection visible.
		/// </param>
		/// <returns>The selection</returns>
		/// -----------------------------------------------------------------------------------
		public virtual IVwSelection SetSelection(IVwRootSite rootSite, bool fInstall,
			bool fMakeVisible)
		{
			return SetSelection(rootSite, fInstall, fMakeVisible, VwScrollSelOpts.kssoDefault);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection by calling <c>IVwRootBox.MakeRangeSelection</c>.
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <param name="fInstall">Makes the selection the current selection</param>
		/// <param name="fMakeVisible">Determines whether or not to make the selection visible.
		/// </param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection, null if it could not return a valid one.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual IVwSelection SetSelection(IVwRootSite rootSite, bool fInstall,
			bool fMakeVisible, VwScrollSelOpts scrollOption)
		{
			if (rootSite == null || rootSite.RootBox == null)
				return null;
			try
			{
				IVwSelection sel = MakeRangeSelection(rootSite.RootBox, fInstall);
				if (fInstall && !sel.IsValid)
				{
					// We rarely expect to have an invalid selection after we install a new selection,
					// but it's possible for selection side-effects to have invalidated it
					// (e.g. highlighting a row in a browse view cf. LT-5033.)
					sel = MakeRangeSelection(rootSite, fInstall);
				}
				if (sel.IsValid)
					m_vwSel = sel;
				else
					m_vwSel = null;

				if (fMakeVisible && m_vwSel != null)
					rootSite.ScrollSelectionIntoView(m_vwSel, scrollOption);

				return m_vwSel;
			}
			catch
			{
				return null;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a range selection based upon our saved selection info.
		/// NOTE: Installing the selection may trigger side effects that will invalidate the selection.
		/// Callers should check to make sure the selection is still valid before using it.
		/// </summary>
		/// <param name="rootSite"></param>
		/// <param name="fInstall"></param>
		/// <returns>a range selection (could become invalid as a side-effect of installing.)</returns>
		/// -----------------------------------------------------------------------------------
		private IVwSelection MakeRangeSelection(IVwRootSite rootSite, bool fInstall)
		{
			return MakeRangeSelection(rootSite.RootBox, fInstall);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a range selection based upon our saved selection info.
		/// NOTE: Installing the selection may trigger side effects that will invalidate the selection.
		/// Callers should check to make sure the selection is still valid before using it.
		/// </summary>
		/// <param name="rootBox"></param>
		/// <param name="fInstall"></param>
		/// <returns>a range selection (could become invalid as a side-effect of installing.)</returns>
		/// -----------------------------------------------------------------------------------
		public IVwSelection MakeRangeSelection(IVwRootBox rootBox, bool fInstall)
		{
			int iAnchor = 0;
			int iEnd = 1;
			if (!m_fEndSet)
			{	// No end information set, so use iAnchor as end
				iEnd = iAnchor;
			}
			if (m_selInfo[iEnd] == null)
			{
				m_selInfo[iEnd] = new SelInfo(m_selInfo[iAnchor]);
			}

			// we want to pass fInstall=false to MakeTextSelection so that it doesn't notify
			// the RootSite of the selection change.
			IVwSelection vwSelAnchor = rootBox.MakeTextSelection(
				m_selInfo[iAnchor].ihvoRoot, m_selInfo[iAnchor].rgvsli.Length,
				m_selInfo[iAnchor].rgvsli, m_selInfo[iAnchor].tagTextProp,
				m_selInfo[iAnchor].cpropPrevious, m_selInfo[iAnchor].ich, m_selInfo[iAnchor].ich,
				m_selInfo[iAnchor].ws, m_selInfo[iAnchor].fAssocPrev, m_selInfo[iAnchor].ihvoEnd,
				null, false);
			IVwSelection vwSelEnd = rootBox.MakeTextSelection(
				m_selInfo[iEnd].ihvoRoot, m_selInfo[iEnd].rgvsli.Length,
				m_selInfo[iEnd].rgvsli, m_selInfo[iEnd].tagTextProp,
				m_selInfo[iEnd].cpropPrevious, m_selInfo[iEnd].ich, m_selInfo[iEnd].ich,
				m_selInfo[iEnd].ws, m_selInfo[iEnd].fAssocPrev, m_selInfo[iEnd].ihvoEnd,
				null, false);
			return rootBox.MakeRangeSelection(vwSelAnchor, vwSelEnd, fInstall);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection
		/// </summary>
		/// <param name="rootSite">The root site</param>
		/// <returns>The selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection SetSelection(IVwRootSite rootSite)
		{
			return SetSelection(rootSite, true, true, VwScrollSelOpts.kssoDefault);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to find a complete word that the selection corresponds to.
		/// If it is a range, this is the word at its start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ITsString SelectedWord
		{
			get
			{
				try
				{
					SelectionHelper ip = this;
					if (IsRange)
					{
						ip = ReduceSelectionToIp(SelLimitType.Top, false, false);
						ip.SetSelection(RootSite, false, false);
					}

					IVwSelection selWord = ip.Selection.GrowToWord();
					if (selWord == null || !selWord.IsRange)
						return null;
					SelectionHelper wordHelper = SelectionHelper.Create(selWord, RootSite);
					ITsStrBldr bldr = wordHelper.GetTss(SelLimitType.Anchor).GetBldr();
					int ichMin = Math.Min(wordHelper.IchAnchor, wordHelper.IchEnd);
					int ichLim = Math.Max(wordHelper.IchAnchor, wordHelper.IchEnd);
					if (ichLim < bldr.Length)
						bldr.ReplaceTsString(ichLim, bldr.Length, null);
					if (ichMin > 0)
						bldr.ReplaceTsString(0, ichMin, null);
					return bldr.GetString();
				}
				catch(Exception)
				{
					// If anything goes wrong, perhaps because we have some bizarre sort
					// of selection such as a picture, just give up getting a selected word.
					return null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets and install the selection in the previously supplied rootsite.
		/// </summary>
		/// <param name="fMakeVisible">Indicates whether to scroll the selection into view
		/// </param>
		/// <returns>The selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection SetSelection(bool fMakeVisible)
		{
			return SetSelection(m_rootSite, true, fMakeVisible, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The requested anchor and endpoint may be beyond the end of the string. Try to make
		/// a selection as near the end of the string as possible.
		/// </summary>
		/// <param name="fMakeVisible">Indicates whether to scroll the selection into view
		/// </param>
		/// <returns>The selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection MakeBest(bool fMakeVisible)
		{
			return MakeBest(m_rootSite, fMakeVisible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The requested anchor and endpoint may be beyond the end of the string. Try to make
		/// a selection as near the end of the string as possible.
		/// </summary>
		/// <param name="fMakeVisible">Indicates whether to scroll the selection into view
		/// </param>
		/// <param name="rootsite">The rootsite that will try take the selection</param>
		/// <returns>The selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection MakeBest(IVwRootSite rootsite, bool fMakeVisible)
		{
			SelInfo ichAnchorOrig = m_selInfo[0];
			SelInfo ichEndOrig = m_selInfo[1];

			// Try setting original selection
			IVwSelection vwsel = SetSelection(rootsite, true, fMakeVisible,
				VwScrollSelOpts.kssoDefault);
			if (vwsel != null)
				return vwsel;

			// Otherwise try endpoint = anchor (if the endpoint is set)
			if (m_fEndSet)
			{
				try
				{
					if (m_selInfo[1] == null || m_selInfo[0] < m_selInfo[1])
						m_selInfo[1] = m_selInfo[0];
					else
						m_selInfo[0] = m_selInfo[1];
				}
				catch (ArgumentException)
				{
					// comparison failed due to selection points being at different text levels,
					// e.g., section heading and section content. Assume first selection point
					// is top
					m_selInfo[1] = m_selInfo[0];
				}

				vwsel = SetSelection(rootsite, true, fMakeVisible);
				if (vwsel != null)
					return vwsel;
			}

			// If we can't find a selection try to create a selection at the end of the
			// current paragraph.
			IchAnchor = 0;
			IVwSelection sel = SetSelection(rootsite, false, false);
			if (sel == null)
				return null;
			bool fAssocPrev;
			int hvoObj, tag, ws, ich;
			ITsString tss;
			sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out ws);
			if (tss != null)
				IchAnchor = tss.Length;
			return SetSelection(rootsite, true, fMakeVisible, VwScrollSelOpts.kssoDefault);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection then scrolls the window so the IP is at the same vertical
		/// position it was when this selection helper object was created. This method is
		/// used mainly after reconstructing a view. After reconstruction, the desire is to
		/// not only have the IP back in the data where it was before reconstruction, but to
		/// have the same number of pixels between the IP and the top of the view.
		/// This method does it's best to do this.
		/// </summary>
		/// <returns>True if a selection could be made (regardless of its accuracy.
		/// Otherwise, false.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool RestoreSelectionAndScrollPos()
		{
			if (RootSite == null)
				return false;

			// Try to restore the selection as best as possible.
			if (MakeBest(true) == null)
				return false;

			IRootSite site = RootSite as IRootSite;
			if (site != null)
				site.ScrollSelectionToLocation(Selection, m_dyIPTop);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selection level for the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveLevel(int tag)
		{
			RemoveLevel(tag, SelLimitType.Anchor);
			RemoveLevel(tag, SelLimitType.End);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selection level for the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="type">Anchor or End</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveLevel(int tag, SelLimitType type)
		{
			int iLevel = GetLevelForTag(tag, type);
			RemoveLevelAt(iLevel, type);
		}

		private void RemoveLevelAt(int iLevel, SelLimitType type)
		{
			if (iLevel < 0)
				return;
			SelLevInfo[] levInfo = GetLevelInfo(type);
			List<SelLevInfo> temp = new List<SelLevInfo>(levInfo);
			temp.RemoveAt(iLevel);
			SetLevelInfo(type, temp.ToArray());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove the specified level from the SelLevInfo for both ends.
		/// </summary>
		/// <param name="ilev"></param>
		/// -----------------------------------------------------------------------------------
		public void RemoveLevelAt(int ilev)
		{
			RemoveLevelAt(ilev, SelLimitType.Anchor);
			RemoveLevelAt(ilev, SelLimitType.End);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a selection level for the specified tag.
		/// </summary>
		/// <param name="iLev">Index at which the level is to be inserted.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The index of the object to insert in the appended level</param>
		/// <param name="ws">HVO of the writing system, if the property is a Multitext</param>
		/// ------------------------------------------------------------------------------------
		public void InsertLevel(int iLev, int tag, int ihvo, int ws)
		{
			InsertLevel(iLev, tag, ihvo, ws, SelLimitType.Anchor);
			InsertLevel(iLev, tag, ihvo, ws, SelLimitType.End);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a selection level for the specified tag.
		/// </summary>
		/// <param name="iLev">Index at which the level is to be inserted.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The index of the object to insert in the appended level</param>
		/// <param name="type">Anchor or End</param>
		/// <param name="ws">HVO of the writing system, if the property is a Multitext</param>
		/// ------------------------------------------------------------------------------------
		private void InsertLevel(int iLev, int tag, int ihvo, int ws, SelLimitType type)
		{
			SelLevInfo[] levInfo = GetLevelInfo(type);
			List<SelLevInfo> temp = new List<SelLevInfo>(levInfo);
			SelLevInfo level = new SelLevInfo();
			level.tag = tag;
			level.ihvo = ihvo;
			level.cpropPrevious = 0;
			level.ws = ws;
			temp.Insert(iLev, level);
			SetLevelInfo(type, temp.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a selection level for the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The index of the object to insert in the appended level</param>
		/// <param name="ws">HVO of the writing system, if the property is a Multitext</param>
		/// ------------------------------------------------------------------------------------
		public void AppendLevel(int tag, int ihvo, int ws)
		{
			AppendLevel(tag, ihvo, ws, SelLimitType.Anchor);
			AppendLevel(tag, ihvo, ws, SelLimitType.End);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a selection level for the specified tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The index of the object to insert in the appended level</param>
		/// <param name="type">Anchor or End</param>
		/// <param name="ws">HVO of the writing system, if the property is a Multitext</param>
		/// ------------------------------------------------------------------------------------
		private void AppendLevel(int tag, int ihvo, int ws, SelLimitType type)
		{
			InsertLevel(GetNumberOfLevels(type), tag, ihvo, ws, type);
		}
		#endregion

		#region Private Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The selection helper stores two sets of selection levels: one for the anchor
		/// (index 0) and one for the end (index 1). This property returns either 0 or 1
		/// (corresponding either to the anchor or the end), depending on whether the selection
		/// was made top-down or bottom-up. If it is bottom-up (i.e., if the end of the
		/// selection is higher in the view than the anchor), then the top index is the index of
		/// the end, so this property returns 1; otherwise, it returns 0. If there is no
		/// selection at all, then this arbitrarily returns 0;
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int TopIndex
		{
			get
			{
				if (m_vwSel == null)
					return 0;
				if (m_iTop < 0)
					m_iTop = m_vwSel.EndBeforeAnchor ? 1 : 0;
				return m_iTop;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The selection helper stores two sets of selection levels: one for the anchor
		/// (index 0) and one for the end (index 1). This property returns either 0 or 1
		/// (corresponding either to the anchor or the end), depending on whether the selection
		/// was made top-down or bottom-up. If it is bottom-up (i.e., if the end of the
		/// selection is higher in the view than the anchor), then the bottom index is the index
		/// of the end, so this property returns 0; otherwise, it returns 1. If there is no
		/// selection at all, then this arbitrarily returns 1;
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int BottomIndex
		{
			get
			{
				return TopIndex == 1 ? 0 : 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information about the selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal protected SelInfo[] SelectionInfo
		{
			get { return m_selInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the index used for the appropriate limit type
		/// </summary>
		/// <param name="type">Limit type</param>
		/// <returns>Index</returns>
		/// ------------------------------------------------------------------------------------
		private int GetIndex(SelLimitType type)
		{
			int i;
			switch (type)
			{
				case SelLimitType.Anchor:
					i = 0;
					break;
				case SelLimitType.End:
					i = 1;
					break;
				case SelLimitType.Top:
					i = TopIndex;
					break;
				case SelLimitType.Bottom:
					i = BottomIndex;
					break;
				default:
					throw new ArgumentOutOfRangeException("Got unexpected SelLimitType");
			}
			if (m_selInfo[i] == null)
				m_selInfo[i] = new SelInfo(m_selInfo[i == 0 ? 1 : 0]);
			return i;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified type is the end.
		/// </summary>
		/// <param name="type">Limit type.</param>
		/// <returns>
		/// 	<c>true</c> if the specified type is the end; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsEnd(SelLimitType type)
		{
			return (GetIndex(type) == 1);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets wheter or not the selection is a range selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsRange
		{
			get
			{
				// Its possible that the selection extents were changed and the SelectionHelper
				// wasn't updated to reflect those changes. (Example is to double-click a word
				// in TE. This creates a range selection without updating the selection
				// information in the SelectionHelper)
				if (Selection != null)
					return Selection.IsRange;

				SelLevInfo[] anchorLev = GetLevelInfo(SelLimitType.Anchor);
				SelLevInfo[] endLev = GetLevelInfo(SelLimitType.End);
				return (IchAnchor != IchEnd || anchorLev[0] != endLev[0] ||
					GetNumberOfLevels(SelLimitType.Anchor) != GetNumberOfLevels(SelLimitType.End));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets wheter or not the selection is valid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsValid
		{
			get {return (Selection != null && Selection.IsValid);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this selection is visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is visible; otherwise, <c>false</c>. Also
		/// <c>false</c> if no rootsite is set or root site isn't derived from SimpleRootSite.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsVisible
		{
			get
			{
				if (RootSite == null || !(RootSite is SimpleRootSite))
					return false;
				return ((SimpleRootSite)RootSite).IsSelectionVisible(Selection);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection that this <see cref="SelectionHelper"/> represents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection Selection
		{
			get { return m_vwSel; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the rootsite
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IVwRootSite RootSite
		{
			get { return m_rootSite; }
			set
			{
				if (m_rootSite != null && m_rootSite is Control)
					((Control)m_rootSite).Disposed -= new EventHandler(OnRootSiteDisposed);

				m_rootSite = value;

				if (m_rootSite != null && m_rootSite is Control)
					((Control)m_rootSite).Disposed += new EventHandler(OnRootSiteDisposed);

				try
				{
					if (m_rootSite != null && m_rootSite is SimpleRootSite)
					{
						SimpleRootSite rootSite = (SimpleRootSite)m_rootSite;
						int dyIPTop = rootSite.IPDistanceFromWindowTop(
							m_rootSite.RootBox.Selection);

						if (dyIPTop > 0)
							m_dyIPTop = dyIPTop;
					}
				}
				catch
				{
					// ignore and go on with life...
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the root site gets disposed. We can't use m_rootSite any more.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnRootSiteDisposed(object sender, EventArgs e)
		{
			m_rootSite = null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of levels needed to traverse the view objects to reach the
		/// given limit of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int GetNumberOfLevels(SelLimitType type)
		{
			return GetSelInfo(type).rgvsli.Length;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the number of levels needed to traverse the view objects to reach the
		/// given limit of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void SetNumberOfLevels(SelLimitType type, int value)
		{
			int iType = GetIndex(type);
			m_selInfo[iType].rgvsli = new SelLevInfo[value];
			for (int i = 0; i < value; i++)
				m_selInfo[iType].rgvsli[i] = new SelLevInfo();
			if (value > 0)
				m_selInfo[iType].rgvsli[0].tag =
					(int)StText.StTextTags.kflidParagraphs;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of levels needed to traverse the view objects to reach the
		/// selected object(s).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int NumberOfLevels
		{
			get
			{
				return GetNumberOfLevels(SelLimitType.Anchor);
			}
			set
			{
				SetNumberOfLevels(SelLimitType.Anchor, value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal selection info for the requested end of the selection.
		/// </summary>
		/// <param name="type">Anchor or End</param>
		/// ------------------------------------------------------------------------------------
		private SelInfo GetSelInfo(SelLimitType type)
		{
			return m_selInfo[GetIndex(type)];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the root object for the given limit of the selection. This
		/// is 0 for views that don't display mutliple root objects).
		/// </summary>
		/// <param name="type">Anchor or End</param>
		/// ------------------------------------------------------------------------------------
		public virtual int GetIhvoRoot(SelLimitType type)
		{
			return GetSelInfo(type).ihvoRoot;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the index of the root object for the given limit of the selection. This
		/// is 0 for views that don't display mutliple root objects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void SetIhvoRoot(SelLimitType type, int value)
		{
			GetSelInfo(type).ihvoRoot = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the root object (for views that display mutliple root
		/// objects). Default is 0.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int IhvoRoot
		{
			get { return GetIhvoRoot(SelLimitType.Anchor); }
			set { SetIhvoRoot(SelLimitType.Anchor, value); }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of previous elements
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Obsolete("Use NumberOfPreviousProps instead")]
		public int CpropPrevious
		{
			get { return m_selInfo[0].cpropPrevious; }
			set { m_selInfo[0].cpropPrevious = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of previous elements for the given limit of the selection
		/// </summary>
		/// <param name="type">Anchor or End</param>
		/// -----------------------------------------------------------------------------------
		public virtual int GetNumberOfPreviousProps(SelLimitType type)
		{
			return GetSelInfo(type).cpropPrevious;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text property that occurs at the indicated end of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int GetTextPropId(SelLimitType type)
		{
			return GetSelInfo(type).tagTextProp;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text property that occurs at the indicated end of the selection.
		/// </summary>
		/// <param name="type">Anchor or End</param>
		/// <param name="tagTextProp">Text property</param>
		/// -----------------------------------------------------------------------------------
		public void SetTextPropId(SelLimitType type, int tagTextProp)
		{
			GetSelInfo(type).tagTextProp = tagTextProp;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the number of previous elements for the given limit of the selection
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void SetNumberOfPreviousProps(SelLimitType type, int value)
		{
			GetSelInfo(type).cpropPrevious = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of previous elements for the given limit of the selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int NumberOfPreviousProps
		{
			get { return GetNumberOfPreviousProps(SelLimitType.Anchor); }
			set { SetNumberOfPreviousProps(SelLimitType.Anchor, value); }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the 0-based index of the character for the given limit of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int GetIch(SelLimitType type)
		{
			return GetSelInfo(type).ich;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the 0-based index of the character for the given limit of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void SetIch(SelLimitType type, int value)
		{
			GetSelInfo(type).ich = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the 0-based index of the character at which the selection begins (or
		/// before which the insertion point is to be placed if IchAnchor == IchEnd)
		/// </summary>
		/// <remarks>Note that if IchAnchor==IchEnd, setting IchAnchor will effectively move
		/// the end as well. Set IchEnd (and thereby m_fEndSet) if you intend to make a range
		/// selection!</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual int IchAnchor
		{
			get { return GetIch(SelLimitType.Anchor); }
			set
			{
				SetIch(SelLimitType.Anchor, value);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the character location to end the selection. Should be set equal to
		/// Ichanchor for a simple insertion point.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int IchEnd
		{
			get { return GetIch(SelLimitType.End); }
			set
			{
				m_fEndSet = true;
				SetIch(SelLimitType.End, value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the writing system associated with the insertion point.
		/// <p>Note: If you need the writing system for the selection, you should
		/// use <see cref="GetFirstWsOfSelection"/>.</p>
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetWritingSystem(SelLimitType type)
		{
			return GetSelInfo(type).ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the writing system associated with the insertion point.
		/// <p>Note: If you need the writing system for the selection, you should
		/// use <see cref="GetFirstWsOfSelection"/>.</p>
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <param name="value">Writing system</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetWritingSystem(SelLimitType type, int value)
		{
			GetSelInfo(type).ws = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system associated with the insertion point.
		/// <p>Note: If you need the writing system for the selection, you should
		/// use <see cref="GetFirstWsOfSelection"/>.</p>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int Ws
		{
			get { return GetWritingSystem(SelLimitType.Anchor); }
			set { SetWritingSystem(SelLimitType.Anchor, value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text props associated with the given end of the selection.
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <returns>Text props associated with the given end of the selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsTextProps GetSelProps(SelLimitType type)
		{
			return GetSelInfo(type).ttpSelProps;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the text props associated with the given end of the selection.
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <param name="value">Properties to set for the selection</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetSelProps(SelLimitType type, ITsTextProps value)
		{
			GetSelInfo(type).ttpSelProps = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text properties associated with the insertion point. If selection
		/// is a range, we're not sure you should necessarily even be doing this. This is
		/// probably relevant only for insertion points.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual ITsTextProps SelProps
		{
			get { return GetSelProps(SelLimitType.End); }
			set { SetSelProps(SelLimitType.End, value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether of not the insertion point should be associated with the
		/// characters immediately preceding it in the view (default) or not.
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <returns><c>true</c> to associate IP with preceding characters, otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool GetAssocPrev(SelLimitType type)
		{
			return GetSelInfo(type).fAssocPrev;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether of not the insertion point should be associated with the
		/// characters immediately preceding it in the view (default) or not.
		/// </summary>
		/// <param name="type">Which end of the selection</param>
		/// <param name="value"><c>true</c> to associate IP with preceding characters, otherwise
		/// <c>false</c></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetAssocPrev(SelLimitType type, bool value)
		{
			GetSelInfo(type).fAssocPrev = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether of not the insertion point should be associated with the
		/// characters immediately preceding it in the view (default) or not.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual bool AssocPrev
		{
			get { return GetAssocPrev(SelLimitType.Anchor); }
			set { SetAssocPrev(SelLimitType.Anchor, value); }
		}

		//		/// -----------------------------------------------------------------------------------
		//		/// <summary>
		//		/// Gets or sets the index of the object containing the selection endpoint. This is
		//		/// -1 by default, but should be set to a valid HVO if the selection spans multiple
		//		/// paragraphs.
		//		/// </summary>
		//		/// -----------------------------------------------------------------------------------
		//		public int IhvoEndPara
		//		{
		//			get { return m_ihvoEnd; }
		//			set { m_ihvoEnd = value; }
		//		}
		//		/// -----------------------------------------------------------------------------------
		//		/// <summary>
		//		/// Gets or sets the text props to be used for any text that is entered after the
		//		/// insertion point is set.
		//		/// </summary>
		//		/// -----------------------------------------------------------------------------------
		//		public ITsTextProps Ttp
		//		{
		//			get { return m_ttp; }
		//			set { m_ttp = value; }
		//		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the array of VwSelLevInfo. Array elements should indicate the chain of
		/// objects that needs to be traversed to get from the root object to object where the
		/// selection is to be made. The tag for item n should be the flid in which the
		/// children of the root object are owned. The 0th element of this array must have
		/// its tag value set to BaseStText.StTextTags.kflidParagraphs. This is set
		/// automatically whenever the array is resized using Cvsli.
		/// </summary>
		/// <param name="type">type</param>
		/// <returns>The level info</returns>
		/// -----------------------------------------------------------------------------------
		public virtual SelLevInfo[] GetLevelInfo(SelLimitType type)
		{
			return GetSelInfo(type).rgvsli;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the array of SelLevInfo.
		/// </summary>
		/// <param name="type">type</param>
		/// <param name="value">The level info</param>
		/// -----------------------------------------------------------------------------------
		public virtual void SetLevelInfo(SelLimitType type, SelLevInfo[] value)
		{
			GetSelInfo(type).rgvsli = value;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the array of VwSelLevInfo. Array elements should indicate the chain of
		/// objects that needs to be traversed to get from the root object to object where the
		/// selection is to be made. The tag for item n should be the flid in which the
		/// children of the root object are owned. The 0th element of this array must have
		/// its tag value set to BaseStText.StTextTags.kflidParagraphs. This is set
		/// automatically whenever the array is resized using Cvsli.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual SelLevInfo[] LevelInfo
		{
			get { return GetLevelInfo(SelLimitType.Anchor); }
		}
		#endregion
	}
}
