// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CollectorEnv.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Xml.Xsl;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	#region Class CollectorEnv
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CollectorEnv is a base class for objects that implement the IVwEnv interface. The main
	/// implementation (in C++) is normally used to actually produce a Views-based display. This
	/// class and its subclasses are used by the same code, but for purposes like producing an
	/// equivalent string, or testing whether some display will be blank.
	/// Note that collectorEnv does not currently fully support multiple-root-object situations
	/// (you would have to call the outer Display() method for each root box, and it would
	/// not track ihvoRoot...I'm not quite sure where that might be needed.)
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CollectorEnv : IVwEnv
	{
		#region PrevPropCount class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class PrevPropCounter
		{
			/// <summary>Count of occurrences of each property</summary>
			private Dictionary<int, int> m_cpropPrev = new Dictionary<int, int>();

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the count of the previous occurrences of the given property.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public int GetCount(int tag)
			{
				int value;
				return m_cpropPrev.TryGetValue(tag, out value) ? value : -1;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Increments the count of the previous occurrences of the given property.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// --------------------------------------------------------------------------------
			public void Increment(int tag)
			{
				if (m_cpropPrev.ContainsKey(tag))
					m_cpropPrev[tag] += 1;
				else
					m_cpropPrev[tag] = 0;
			}
		}
		#endregion

		#region StackItem class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class StackItem
		{
			/// <summary>Hvo of the next higher item in the view hierarchy (usually the "owner" of
			/// the item)</summary>
			public int m_hvoOuter;
			/// <summary>Hvo of the current item</summary>
			public int m_hvo;
			/// <summary>Tag of the current item</summary>
			public int m_tag;
			/// <summary>Index of the current item</summary>
			public int m_ihvo;
			/// <summary>Handles counting of previous occurrences of properties</summary>
			public PrevPropCounter m_cpropPrev = new PrevPropCounter();

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:StackItem"/> class.
			/// </summary>
			/// <param name="hvoOuter">The hvo of the next higher item in the view hierarchy
			/// (usually the "owner" of the item)</param>
			/// <param name="hvo">The hvo.</param>
			/// <param name="tag">The tag.</param>
			/// <param name="ihvo">The ihvo.</param>
			/// --------------------------------------------------------------------------------
			public StackItem(int hvoOuter, int hvo, int tag, int ihvo)
			{
				m_hvoOuter = hvoOuter;
				m_hvo = hvo;
				m_tag = tag;
				m_ihvo = ihvo;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Creates a sel lev info from this stack item.
			/// </summary>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public SelLevInfo ToSelLevInfo()
			{
				SelLevInfo levInfo = new SelLevInfo();
				levInfo.hvo = m_hvo;
				levInfo.tag = m_tag;
				levInfo.ihvo = m_ihvo;
				//???????//levInfo.cpropPrevious = m_cpropPrev[m_tag];
				return levInfo;
			}
		}
		#endregion // StackItem

		#region Data members
		private IVwEnv m_baseEnv;
		/// <summary></summary>
		protected ISilDataAccess m_sda;
		/// <summary>The object we are currently building a display of.</summary>
		protected int m_hvoCurr;
		/// <summary>Collection of StackItems, keeps track of outer context.</summary>
		protected List<StackItem> m_stack = new List<StackItem>();
		/// <summary>Prop currently being built</summary>
		protected int m_tagCurrent = 0;
		/// <summary>Used for multiple properties at the root level</summary>
		protected PrevPropCounter m_cpropPrev = new PrevPropCounter();
		/// <summary>set by OpenProp and closeObject, cleared by CloseProp and OpenObject;
		/// Keeps track of whether things currently being added are part of some
		/// known property of the current object.</summary>
		protected bool m_fIsPropOpen;
		/// <summary>Set if we add something while m_fIsPropOpen is true;
		/// cleared (and we note an occurrence of ktagNotAnAttr) if it is set
		/// when clearing m_fIsPropOpen.</summary>
		protected bool m_fGotNonPropInfo;
		/// <summary>This is used to detect virtual properties in setting notifiers.  See LT-8245.</summary>
		protected IFwMetaDataCache m_mdc = null;
		/// <summary>This is used to find virtual property handlers in setting notifiers.  See LT-8245</summary>
		protected IVwCacheDa m_cda = null;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create one. If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Data access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// ------------------------------------------------------------------------------------
		public CollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
		{
			m_baseEnv = baseEnv;
			m_sda = sda;
			if (baseEnv == null)
				m_hvoCurr = hvoRoot;
			else
				m_hvoCurr = baseEnv.OpenObject;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the metadata cache.  This is needed to detect virtual properties in setting
		/// notifiers.  See LT-8245.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFwMetaDataCache MetaDataCache
		{
			get { return m_mdc; }
			set { m_mdc = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the cache data access.  This is needed to get virtual properties handlers in
		/// setting notifiers.  See LT-8245.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwCacheDa CacheDa
		{
			get { return m_cda; }
			set { m_cda = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			m_tagCurrent = 0;
			m_stack.Clear();
			m_cpropPrev = new PrevPropCounter();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property currently being built.
		/// </summary>
		/// <value>The current prop tag.</value>
		/// ------------------------------------------------------------------------------------
		protected int CurrentPropTag
		{
			get { return m_tagCurrent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of previous occurrences of the given property at our current stack
		/// level.
		/// </summary>
		/// <param name="tag">The tag/flid to check.</param>
		/// ------------------------------------------------------------------------------------
		protected int CPropPrev(int tag)
		{
			if (m_stack.Count == 0)
				return m_cpropPrev.GetCount(tag);

			StackItem top = m_stack[m_stack.Count - 1];
			return top.m_cpropPrev.GetCount(tag);
		}


		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of the previous occurrences of the given property at the bottom
		/// of the stack.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public int CountOfPrevPropAtRoot
		{
			get
			{
				return (m_stack.Count == 0) ? m_cpropPrev.GetCount(m_tagCurrent) :
					m_cpropPrev.GetCount(m_stack[0].m_tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a TsString into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddTsString(ITsString tss)
		{
			AddResultString(tss.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if we don't need to process any further. Some methods may be able
		/// to truncate operations.
		/// </summary>
		/// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool Finished
		{
			get {return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="s">The s.</param>
		/// ------------------------------------------------------------------------------------
		internal protected virtual void AddResultString(string s)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result, with known writing system.
		/// This base implementation ignores the writing system.
		/// </summary>
		/// <param name="s">The s.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		internal virtual void AddResultString(string s, int ws)
		{
			AddResultString(s);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the object.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="ihvo">The index of this object in the collection being displayed.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OpenTheObject(int hvo, int ihvo)
		{
			m_stack.Add(new StackItem(m_hvoCurr, hvo, m_tagCurrent, ihvo));
			m_hvoCurr = hvo;
			m_fIsPropOpen = false;
		}

		/// <summary>
		/// look at top of context stack
		/// </summary>
		protected StackItem PeekStack
		{
			get { return m_stack != null && m_stack.Count > 0 ? m_stack[m_stack.Count - 1] : null; }
		}

		/// <summary>
		/// context stack
		/// </summary>
		/// <returns></returns>
		protected StackItem PopStack()
		{
			StackItem top = m_stack[m_stack.Count - 1];
			m_stack.RemoveAt(m_stack.Count - 1);
			return top;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CloseTheObject()
		{
			Debug.Assert(!m_fIsPropOpen); // should close any props we open before closing obj.
			// Note any trailing non-prop info at the end of the current object.
			CheckForNonPropInfo();
			StackItem top = m_stack[m_stack.Count - 1];
			m_stack.RemoveAt(m_stack.Count - 1);
			m_hvoCurr = top.m_hvoOuter;
			m_tagCurrent = top.m_tag;
			// objects are always added as part of some property, so if we close one,
			// we must be back inside the property we were in when we added it.
			m_fIsPropOpen = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the prop.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		protected void OpenProp(int tag)
		{
			CheckForNonPropInfo();
			m_fIsPropOpen = true;

			IncrementPropCount(tag);
			m_tagCurrent = tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See whether anything has been added to the view while no property was open,
		/// and if so, bump the gap-in-attrs count.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CheckForNonPropInfo()
		{
			if (Finished)
				return;

			if (m_fGotNonPropInfo)
			{
				IncrementPropCount((int)VwSpecialAttrTags.ktagGapInAttrs);
				m_fGotNonPropInfo = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increments the property count for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		private void IncrementPropCount(int tag)
		{
			if (m_stack.Count == 0)
				m_cpropPrev.Increment(tag);
			else
			{
				StackItem top = m_stack[m_stack.Count - 1];
				top.m_cpropPrev.Increment(tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the prop. Does nothing as yet, but keeps things analogous to real VwEnv.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CloseProp()
		{
			m_fIsPropOpen = false;
		}

		#region IVwEnv Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A rather arbitrary way of representing a bar as a string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void AddSeparatorBar()
		{
			NoteAddingSomething();
			AddResultString("|");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rarely if ever used.
		/// </summary>
		/// <param name="_rghvo">_rghvo</param>
		/// <param name="chvo">chvo</param>
		/// <param name="_vwvc">_vwvc</param>
		/// <param name="frag">frag</param>
		/// ------------------------------------------------------------------------------------
		public void AddLazyItems(int[] _rghvo, int chvo, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException("AddLazyItems is not yet implemented");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add all the alternatives, in a very primitive arrangement. Not used much if at all.
		/// </summary>
		/// <param name="tag">tag</param>
		/// ------------------------------------------------------------------------------------
		public void AddStringAlt(int tag)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotImplementedException("AddStringAlt is not yet implemented in CollectorEnv");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddUnicodeProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			OpenProp(tag);
			AddResultString(m_sda.get_UnicodeProp(m_hvoCurr, tag), ws);
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we're capturing a test on top of a real VwEnv, pass the information on
		/// so things will regenerate properly.
		/// </summary>
		/// <param name="rghvo">_rghvo</param>
		/// <param name="rgtag">_rgtag</param>
		/// <param name="chvo">chvo</param>
		/// ------------------------------------------------------------------------------------
		public virtual void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
		{
			if (m_baseEnv != null)
			{
				if (chvo == 1 && m_cda != null && m_mdc != null && m_mdc.get_IsVirtual((uint)rgtag[0]))
				{
					// See if we can update rghvo, rgtag, and chvo to better handle notifiers for
					// virtual properties.
					IVwVirtualHandler vh = m_cda.GetVirtualHandlerId(rgtag[0]);
					if (vh != null && vh is BaseVirtualHandler)
					{
						(vh as BaseVirtualHandler).UpdateNotifierLists(m_sda, ref rghvo, ref rgtag, ref chvo);
					}
				}
				m_baseEnv.NoteDependency(rghvo, rgtag, chvo);
			}
		}

		/// <summary>
		/// If we're capturing a test on top of a real VwEnv, pass the information on
		/// so things will regenerate properly.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_tssVal"></param>
		public void NoteStringValDependency(int hvo, int tag, int ws, ITsString _tssVal)
		{
			if (m_baseEnv != null)
				m_baseEnv.NoteStringValDependency(hvo, tag, ws, _tssVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set text properties. Nothing to do here. None of the collectors cares about text
		/// properties (yet).
		/// </summary>
		/// <value></value>
		/// <returns>A ITsTextProps</returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsTextProps Props
		{
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenMappedTaggedPara()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No easy way to implement this, it depends on a StrUni format method.
		/// </summary>
		/// <param name="tag">tag</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddGenDateProp(int tag)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotImplementedException("AddGenDateProp not implemented for CollectorEnv");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// An implementation equivalent to the IVwEnv one is fairly trivial, but so unsatisfactory
		/// that I'm sure we're not using it.
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_rgenc">_rgenc</param>
		/// <param name="cws">cws</param>
		/// ------------------------------------------------------------------------------------
		public void AddStringAltSeq(int tag, int[] _rgenc, int cws)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotImplementedException("AddStringAltSeq not implemented for CollectorEnv");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj vec.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			OpenProp(tag);
			vc.DisplayVec(this, m_hvoCurr, tag, frag);
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CloseParagraph()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OpenParagraph()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTaggedPara()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the string.
		/// </summary>
		/// <param name="tss">The TSS.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddString(ITsString tss)
		{
			NoteAddingSomething();
			AddTsString(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTableBody()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// <param name="cOverrideProperties">cOverrideProperties</param>
		/// <param name="_rgOverrideProperties">_rgOverrideProperties</param>
		/// ------------------------------------------------------------------------------------
		public void OpenOverridePara(int cOverrideProperties,
			DispPropOverride[] _rgOverrideProperties)
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member CurrentObject
		/// </summary>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int CurrentObject()
		{
			return m_hvoCurr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here. None of our collectors cares about string properties (yet).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void set_StringProperty(int sp, string bstrValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseDiv()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTableRow()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTable(int cCols, VwLength vlWidth, int mpBorder,
			VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule,
			int mpSpacing, int mpPadding, bool fSelectOneCol)
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CloseInnerPile()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the object we are currently building a display of. The name is unfortunately
		/// fixed by the IVwEnv interface. Compare OpenTheObject(), which handles changing
		/// which one is current.
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int OpenObject
		{
			get { return m_hvoCurr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddTsString(m_sda.get_StringProp(m_hvoCurr, tag));
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here. None of our collectors cares about integer properties (yet).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void set_IntProperty(int tpt, int tpv, int nValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// May need to do this one soon. Needs a new interface to give access to the IVwEnv
		/// internal methods.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="flags"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddTimeProp(int tag, uint flags)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			//throw new NotImplementedException("AddTimeProp not implemented by CollectorEnv");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTableFooter()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here. None of our collectors cares about flow object organization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTableHeaderCell()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CloseTableCell()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddIntProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddIntProp(int tag)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddResultString(m_sda.get_IntProp(m_hvoCurr, tag).ToString());
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OpenTableRow()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddAdjustedPicture
		/// </summary>
		/// <param name="_pict">_pict</param>
		/// <param name="tag">tag</param>
		/// <param name="dxmpWidth">dxmpWidth</param>
		/// <param name="dympHeight">dympHeight</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddPicture(stdole.IPicture _pict, int tag, int dxmpWidth,
			int dympHeight)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddResultString("A picture"); // REVIEW: This seems weird. Is the caption handled correctly?
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member SetParagraphMark
		/// </summary>
		/// <param name="boundaryMark">enumeration value used to represent the paragraph or section
		/// boundary and whether it is highlighted or not: endOfParagraph,	endOfSection,
		/// endOfParagraphHighlighted, or endofSectionHighlighted
		/// </param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetParagraphMark(VwBoundaryMark boundaryMark)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTableBody()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the outer object.
		/// </summary>
		/// <param name="iLevel">Index of the outer level to retrieve.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The ihvo.</param>
		/// ------------------------------------------------------------------------------------
		public void GetOuterObject(int iLevel, out int hvo, out int tag, out int ihvo)
		{
			int clevBase = 0;
			if (m_baseEnv != null)
				clevBase = m_baseEnv.EmbeddingLevel;
			if (iLevel >= clevBase)
			{
				// A row from our own stack
				StackItem item = m_stack[iLevel - clevBase];
				hvo = item.m_hvoOuter;
				tag = item.m_tag;
				ihvo = item.m_ihvo;
			}
			else
				m_baseEnv.GetOuterObject(iLevel, out hvo, out tag, out ihvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddTsString(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws));
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Probably unused, but we'll put something in because a window is probably 'something'
		/// and therefore interesting to the TestCollectorEnv.
		/// </summary>
		/// <param name="_ew"></param>
		/// <param name="dmpAscent"></param>
		/// <param name="fJustifyRight"></param>
		/// <param name="fAutoShow"></param>
		/// ------------------------------------------------------------------------------------
		public void AddWindow(IVwEmbeddedWindow _ew, int dmpAscent, bool fJustifyRight,
			bool fAutoShow)
		{
			AddResultString("A window");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For our purpose, laziness is not relevant. Equivalent to the non-lazy version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddLazyVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			AddObjVecItems(tag, vc, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj prop.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
				return;
			int hvoItem = m_sda.get_ObjectProp(m_hvoCurr, tag);
			if (hvoItem != 0)
			{
				OpenProp(tag);
				OpenTheObject(hvoItem, 0);
				vc.Display(this, hvoItem, frag);
				CloseTheObject();
				CloseProp();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer some arbitrary guess. The string length is not very good, but probably
		/// good enough for collector results. Non-zero (except for empty strings) in case
		/// something divides by it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void get_StringWidth(ITsString tss, ITsTextProps _ttp, out int dmpx, out int dmpy)
		{
			dmpx = tss.Length;
			dmpy = 10;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenSpan()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddIntPropPic
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_vc">The view constructor.</param>
		/// <param name="frag">frag</param>
		/// <param name="nMin">nMin</param>
		/// <param name="nMax">nMax</param>
		/// ------------------------------------------------------------------------------------
		public void AddIntPropPic(int tag, IVwViewConstructor _vc, int frag, int nMin, int nMax)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddResultString("A picture"); // REVIEW: This seems weird.
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddPicture
		/// </summary>
		/// <param name="_pict">_pict</param>
		/// <param name="tag">tag</param>
		/// <param name="_ttpCaption">The _TTP caption.</param>
		/// <param name="hvoCmFile">The hvo cm file.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="dxmpWidth">Width of the DXMP.</param>
		/// <param name="dympHeight">Height of the dymp.</param>
		/// <param name="_vc">The view constructor.</param>
		/// ------------------------------------------------------------------------------------
		public void AddPictureWithCaption(stdole.IPicture _pict, int tag,
			ITsTextProps _ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight,
			IVwViewConstructor _vc)
		{
			if (Finished)
				return;
			AddPicture(_pict, tag, dxmpWidth, dympHeight);
			OpenParagraph();
			AddStringAltMember((int)CmPicture.CmPictureTags.kflidCaption, ws, _vc);
			CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTableFooter()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the prop.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
				return;
			OpenProp(tag);
			AddTsString(vc.DisplayVariant(this, tag, m_sda.get_Prop(m_hvoCurr, tag), frag));
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTableHeader()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenInnerPile()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTableHeader()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OpenTableCell(int nRowSpan, int nColSpan)
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a DataAccess
		/// </summary>
		/// <value></value>
		/// <returns>A ISilDataAccess</returns>
		/// ------------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			get { return m_sda; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseTable()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj.
		/// </summary>
		/// <param name="hvoItem">The hvo item.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			if (Finished)
				return;
			OpenProp((int)VwSpecialAttrTags.ktagNotAnAttr);
			OpenTheObject(hvoItem, 0);
			vc.Display(this, hvoItem, frag);
			CloseTheObject();
			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a EmbeddingLevel
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int EmbeddingLevel
		{
			get
			{
				if (m_baseEnv == null)
					return m_stack.Count;
				else
					return m_baseEnv.EmbeddingLevel + m_stack.Count;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Also not implemented at all by VwEnv.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddDerivedProp(int[] _rgtag, int ctag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException("AddDerivedProp not implemented by CollectorEnv");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseSpan()
		{
			CloseFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here. None of our collectors cares about flow object organization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeColumns(int nColSpan, VwLength vlWidth)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenDiv()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenConcPara(int ichMinItem, int ichLimItem,
			VwConcParaOpts cpoFlags, int dmpAlign)
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenMappedPara()
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing to do here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
		{
			OpenFlowObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddSimpleRect
		/// </summary>
		/// <param name="rgb">rgb</param>
		/// <param name="dmpWidth">dmpWidth</param>
		/// <param name="dmpHeight">dmpHeight</param>
		/// <param name="dmpBaselineOffset">dmpBaselineOffset</param>
		/// ------------------------------------------------------------------------------------
		public void AddSimpleRect(uint rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		{
			NoteAddingSomething();
			AddResultString("A rectangle"); // makes it non-empty, at least.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj vec items.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
				return;
			OpenProp(tag);
			int cobj = m_sda.get_VecSize(m_hvoCurr, tag);

			for (int i = 0; i < cobj; i++)
			{
				int hvoItem = m_sda.get_VecItem(m_hvoCurr, tag, i);
				if (DisplayThisObject(hvoItem, tag))
				{
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				}
				if (Finished)
					break;
			}

			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj vec items in reverse order.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddReversedObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
				return;
			OpenProp(tag);
			int cobj = m_sda.get_VecSize(m_hvoCurr, tag);

			for (int i = cobj - 1; i >= 0; --i)
			{
				int hvoItem = m_sda.get_VecItem(m_hvoCurr, tag, i);
				if (DisplayThisObject(hvoItem, tag))
				{
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				}
				if (Finished)
					break;
			}

			CloseProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sub-classes can override
		/// </summary>
		/// <param name="hvoItem">The hvo item.</param>
		/// <param name="tag">The tag.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool DisplayThisObject(int hvoItem, int tag)
		{
			// no-op: Always assume we want to display an object
			return true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a flow object. Default only cares for the purpose of keeping track of
		/// non-property things added, which affects our count of gaps in properties,
		/// which is important for figuring out which literal a selection is part of.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OpenFlowObject()
		{
			NoteAddingSomething();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Note that something (box, text, picture) is being added to the display.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void NoteAddingSomething()
		{
			if (!m_fIsPropOpen)
				m_fGotNonPropInfo = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes a flow object. Default implementation doesn't care about flow object
		/// organization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CloseFlowObject()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Optionally apply an XSLT to the output file.
		/// </summary>
		/// <param name="sXsltFile">The s XSLT file.</param>
		/// <param name="sOutputFile">The s output file.</param>
		/// <param name="iPass">The i pass.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PostProcess(string sXsltFile, string sOutputFile, int iPass)
		{
			if (sXsltFile == null || sXsltFile.Length == 0)
				return;
			// Apply XSLT to the output file, first renaming it so that the user sees
			// the expected final output file.
			string sIntermediateFile = RenameOutputToPassN(sOutputFile, iPass);

			XslCompiledTransform xsl = new XslCompiledTransform();
			xsl.Load(sXsltFile);
			xsl.Transform(sIntermediateFile, sOutputFile);
			// Deleting them deals with LT-6345,
			// which asked that they be put in the temp folder.
			// Rather than piling them up forever in there,
			// see if simply deleting them is better.
			// Since nobody outside this method knows what sIntermediateFile is,
			// it would be hard to think other code would be sad to not find it.
			// If some client can figure out waht it was, then the temp folder
			// option might have to be used.
			File.Delete(sIntermediateFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Renames the output to pass N.
		/// </summary>
		/// <param name="sOutputFile">The s output file.</param>
		/// <param name="iPass">The i pass.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string RenameOutputToPassN(string sOutputFile, int iPass)
		{
			int idx = sOutputFile.LastIndexOfAny(new char[] { '/', '\\' });
			if (idx < 0)
				idx = 0;
			else
				++idx;
			string sIntermediateFile = sOutputFile.Substring(0, idx) + "Phase" +
				iPass.ToString() +
				"-" + sOutputFile.Substring(idx);
			try
			{
				System.IO.File.Delete(sIntermediateFile);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
			System.IO.File.Move(sOutputFile, sIntermediateFile);
			return sIntermediateFile;
		}
	}
	#endregion // CollectorEnv

	#region Class StringCollectorEnv
	/// <summary>
	/// This subclass is used to accumulate a string equivalent to the result that would be
	/// produced by calling Display().
	/// </summary>
	public class StringCollectorEnv : CollectorEnv
	{
		private StringBuilder m_builder = new StringBuilder();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringCollectorEnv"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// ------------------------------------------------------------------------------------
		public StringCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>The result.</value>
		/// ------------------------------------------------------------------------------------
		public string Result
		{
			get { return m_builder.ToString(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="s">The s.</param>
		/// ------------------------------------------------------------------------------------
		internal protected override void AddResultString(string s)
		{
			base.AddResultString (s);
			if (s == null)
				return;
			m_builder.Append(s);
		}
	}
	#endregion // StringCollectorEnv

	#region StringMeasureEnv
	/// <summary>
	/// This subclass is used to estimate the total width  (and max height)of all text that the display shows.
	/// Currently it only supports a rough approximation by picking one font to use throughout.
	/// Could be enhanced to consider changes in text properties.
	/// Could be much more easily made to do the right thing if we had a valid base VwEnv.
	/// </summary>
	public class StringMeasureEnv : CollectorEnv
	{
		/// <summary> total pixel width (and max height) of all text in the display </summary>
		protected int m_width; // pixels
		/// <summary>Drawing.Graphics for this view</summary>
		System.Drawing.Graphics m_graphics; // passed in initialize for measuring strings.
		/// <summary> </summary>
		protected System.Drawing.Font m_font;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringMeasureEnv"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// <param name="graphics">To use to measure string</param>
		/// <param name="font">To use to measure strings</param>
		/// ------------------------------------------------------------------------------------
		public StringMeasureEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot,
			System.Drawing.Graphics graphics, System.Drawing.Font font)
			: base(baseEnv, sda, hvoRoot)
		{
			m_graphics = graphics;
			m_font = font;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the result width.
		/// </summary>
		/// <value>The result.</value>
		/// ------------------------------------------------------------------------------------
		public int Width
		{
			get { return m_width; }
		}

		/// <summary>
		/// Gets the System.Drawing.Graphics object.
		/// </summary>
		protected Graphics GraphicsObj
		{
			get { return m_graphics; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result, by adding its width
		/// </summary>
		/// <param name="s">The s.</param>
		/// ------------------------------------------------------------------------------------
		internal protected override void AddResultString(string s)
		{
			base.AddResultString (s);
			if (s == null)
				return;
			AddStringWidth(s);
		}

		/// <summary>
		/// update total pixel width for display
		/// </summary>
		/// <param name="s"></param>
		protected virtual void AddStringWidth(string s)
		{
			m_width += Convert.ToInt32(m_graphics.MeasureString(s, m_font).Width);
		}
	}


	/// <summary>
	/// Estimates the longest width (in pixels) of string contents in a specified column in a table.
	/// Currently only calculates for cells spanning one column in a views table.
	/// </summary>
	public class MaxStringWidthForColumnEnv : StringMeasureEnv
	{
		IDictionary<int, Font> m_wsToFont = new Dictionary<int, Font>();
		int m_fontWs = 0;
		IVwStylesheet m_styleSheet = null;
		ILgWritingSystemFactory m_wsf = null;

		/// <summary>Index of the column being examined.</summary>
		protected int m_icolToWatch = -1;
		/// <summary>Index of the current column.</summary>
		protected int m_icolCurr = -1;
		/// <summary>Index of the current row.</summary>
		int m_irowCurr = -1;
		/// <summary>Number of columns spanned by the current cell.</summary>
		protected int m_nColSpanCurr = 0;
		/// <summary>Maximum width so far collected in this column.</summary>
		int m_widthMax = 0;
		/// <summary>Index of the row with the widest width so far.</summary>
		int m_irowOfWidestString = -1;

		/// <summary>
		///
		/// </summary>
		/// <param name="stylesheet"></param>
		/// <param name="sda"></param>
		/// <param name="hvoRoot"></param>
		/// <param name="graphics"></param>
		/// <param name="icolumn"></param>
		public MaxStringWidthForColumnEnv(IVwStylesheet stylesheet, ISilDataAccess sda, int hvoRoot,
			System.Drawing.Graphics graphics, int icolumn)
			: base(null, sda, hvoRoot, graphics, null)
		{
			m_icolToWatch = icolumn;
			m_wsf = sda.WritingSystemFactory;
			m_styleSheet = stylesheet;
		}

		/// <summary>
		///
		/// </summary>
		public override void OpenTableRow()
		{
			m_irowCurr++;
			m_icolCurr = 0;
			m_nColSpanCurr = 0;
			base.OpenTableRow();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="nRowSpan"></param>
		/// <param name="nColSpan"></param>
		public override void OpenTableCell(int nRowSpan, int nColSpan)
		{
			// reset the width
			m_width = 0;
			// add the col span of the previous cell
			m_icolCurr += m_nColSpanCurr;
			// then log the current cell span.
			m_nColSpanCurr = nColSpan;
			base.OpenTableCell(nRowSpan, nColSpan);
		}

		bool m_fInPara = false;
		/// <summary>
		///
		/// </summary>
		public override void OpenParagraph()
		{
			// new paragraph means new line, so reset our width.
			m_fInPara = true;
			m_width = 0;
			base.OpenParagraph();
		}


		/// <summary>
		///
		/// </summary>
		protected override void CloseTheObject()
		{
			base.CloseTheObject();

			if (!m_fInPara)
			{
				// we didn't open the object in the context of a paragraph
				// so assume these are getting added to their
				// own paragraph boxes.
				UpdateMaxStringWidth();
			}
		}

		/// <summary>
		/// update max string width info, if we haven't already done so.
		/// </summary>
		public override void CloseParagraph()
		{
			base.CloseParagraph();
			// update max string width info
			UpdateMaxStringWidth();
			m_fInPara = false;
		}

		/// <summary>
		/// update max string width info, if we haven't already done so.
		/// </summary>
		public override void CloseTableCell()
		{
			base.CloseTableCell();
			UpdateMaxStringWidth();
		}

		/// <summary>
		/// Updates the column width counter for auto-resizing Views columns.
		/// </summary>
		protected virtual void UpdateMaxStringWidth()
		{
			if (m_width > m_widthMax)
			{
				m_widthMax = m_width;
				m_irowOfWidestString = m_irowCurr;
			}
			m_width = 0;
		}

		/// <summary>
		/// return the maximum string pixel width in the display of a given column that
		/// can be used to provide the width of the column.
		/// </summary>
		public int MaxStringWidth
		{
			get { return m_widthMax; }
		}

		/// <summary>
		/// index of row containing string of longest width
		/// </summary>
		public int RowIndexOfMaxStringWidth
		{
			get { return m_irowOfWidestString; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tss"></param>
		public override void AddTsString(ITsString tss)
		{
			// get the first ws from the tss to determine whether we need to use a different font
			// assume if there are multiple embedded wss, they will not throw off the basic width of this text.
			int wsTss = StringUtils.GetWsAtOffset(tss, 0);
			Debug.Assert(wsTss > 0, String.Format("Invalid ws({0}) embedded in run in string '{1}'.", wsTss, tss.Text));
			if (wsTss != m_fontWs && wsTss > 0)
			{
				m_fontWs = wsTss;
				SetFontToCurrentWs();
			}
			base.AddTsString(tss);
		}

		private void SetFontToCurrentWs()
		{
			m_font = GetFontFromWs(m_fontWs);
		}

		private Font GetFontFromWs(int ws)
		{
			Font font;
			if (ws == 0)
			{
				// create a font ex-nihilo
				string fontName = "Arial";
				int fontSize = 14;
				font = new System.Drawing.Font(fontName, fontSize);
				m_wsToFont.Add(0, font);
			}
			else if (!m_wsToFont.TryGetValue(ws, out font))
			{
				// get font from stylesheet.
				font = EditingHelper.GetFontForNormalStyle(ws, m_styleSheet, m_wsf);
				m_wsToFont.Add(ws, font);
			}
			return font;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="s"></param>
		internal protected override void AddResultString(string s)
		{
			// if we haven't already establed a font, do so now.
			if (m_font == null)
				SetFontToCurrentWs();
			base.AddResultString(s);
		}

		internal override void AddResultString(string s, int ws)
		{
			if (m_fontWs != ws && ws > 0)
				m_fontWs = ws;
			base.AddResultString(s, ws);
		}

		/// <summary>
		/// only update string width if we're in the column we're interested in.
		/// </summary>
		/// <param name="s"></param>
		protected override void AddStringWidth(string s)
		{
			if (m_icolCurr != m_icolToWatch || m_nColSpanCurr != 1)
				return;
			base.AddStringWidth(s);
		}
	}

	#endregion // StringMeasureEnv

	#region Class TestCollectorEnv
	/// <summary>
	/// This subclass is used to test whether the call to Display produces anything.
	/// Adding literals doesn't count.
	/// </summary>
	public class TestCollectorEnv : CollectorEnv
	{
		private bool m_fNonEmpty = false;
		private bool m_fNoteEmptyDependencies = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TestCollectorEnv"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// ------------------------------------------------------------------------------------
		public TestCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>The result.</value>
		/// ------------------------------------------------------------------------------------
		public bool Result
		{
			get { return m_fNonEmpty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This collector is done as soon as we know there's something there.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool Finished
		{
			get
			{
				return m_fNonEmpty;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="s">The s.</param>
		/// ------------------------------------------------------------------------------------
		internal protected override void AddResultString(string s)
		{
			base.AddResultString(s);
			if (String.IsNullOrEmpty(s))
				return;
			// Review JohnT: should we test for non-blank? Maybe that could be configurable?
			m_fNonEmpty = true;
		}

		Set<int> m_notedStringPropertyDependencies = new Set<int>();
		/// <summary>
		///
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_vwvc"></param>
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			base.AddStringAltMember(tag, ws, _vwvc);

			// (LT-6224) if we want the display to update for any empty multistring alternatives,
			// we will just note a dependency on the string property, and that will cover all
			// of the writing systems for that string property
			// We only need to do this once.
			if (NoteEmptyDependencies && !m_notedStringPropertyDependencies.Contains(tag))
			{
				NoteEmptyDependency(tag);
				// NOTE: This should get cleared in OpenTheObject().
				m_notedStringPropertyDependencies.Add(tag);
			}
		}

		/// <summary>
		/// We need to clear the list for noting string property dependencies
		/// any time we're in the context of a different object.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ihvo"></param>
		protected override void OpenTheObject(int hvo, int ihvo)
		{
			m_notedStringPropertyDependencies.Clear();
			base.OpenTheObject(hvo, ihvo);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing. We don't want to count literals in this test class.
		/// </summary>
		/// <param name="tss">The TSS.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddString(ITsString tss)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag whether or not to note dependencies on empty properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NoteEmptyDependencies
		{
			get { return m_fNoteEmptyDependencies; }
			set { m_fNoteEmptyDependencies = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When testing whether anything exists, add notifiers for empty properties if the
		/// caller has so requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddObjProp(tag, vc, frag);
			if (m_fNoteEmptyDependencies)
				NoteEmptyObjectDependency(tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When testing whether anything exists, add notifiers for empty vectors if the
		/// caller has so requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddObjVec(tag, vc, frag);
			if (m_fNoteEmptyDependencies)
				NoteEmptyVectorDependency(tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When testing whether anything exists, add notifiers for empty vectors if the
		/// caller has so requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void AddReversedObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			base.AddReversedObjVecItems(tag, vc, frag);
			if (m_fNoteEmptyDependencies)
				NoteEmptyVectorDependency(tag);
		}

		private void NoteEmptyObjectDependency(int tag)
		{
			int hvoObj = m_sda.get_ObjectProp(m_hvoCurr, tag);
			if (hvoObj == 0)
				NoteEmptyDependency(tag);
		}

		private void NoteEmptyVectorDependency(int tag)
		{
			int chvo = m_sda.get_VecSize(m_hvoCurr, tag);
			if (chvo == 0)
				NoteEmptyDependency(tag);
		}

		private void NoteEmptyDependency(int tag)
		{
			NoteDependency(new int[] { m_hvoCurr }, new int[] { tag }, 1);
		}
	}
	#endregion // TestCollectorEnv

	#region Class TsStringCollectorEnv
	/// <summary>
	/// Collect the results as a TsString.
	/// </summary>
	public class TsStringCollectorEnv : CollectorEnv
	{
		private ITsIncStrBldr m_builder;
		private bool m_fNewParagraph = false;
		private int m_cParaOpened = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TsStringCollectorEnv"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// ------------------------------------------------------------------------------------
		public TsStringCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot):
			base(baseEnv, sda, hvoRoot)
		{
			m_builder = TsIncStrBldrClass.Create();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a TsString into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddTsString(ITsString tss)
		{
			AppendSpaceForFirstWordInNewParagraph(tss.Text);
			m_builder.AppendTsString(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		/// <param name="s">The s.</param>
		/// ------------------------------------------------------------------------------------
		internal protected override void AddResultString(string s)
		{
			AppendSpaceForFirstWordInNewParagraph(s);
			m_builder.Append(s);
		}

		/// <summary>
		/// keep track of the opened paragraphs, so we can add spaces before strings in new paragraphs.
		/// </summary>
		public override void OpenParagraph()
		{
			base.OpenParagraph();
			m_cParaOpened++;
			if (m_cParaOpened > 1)
				m_fNewParagraph = true;
		}

		bool m_fRequestAppendSpaceForFirstWordInNewParagraph = true;
		/// <summary>
		/// Indicates whether this collector will append space for first word in new paragraph.
		/// True, by default.
		/// </summary>
		public bool RequestAppendSpaceForFirstWordInNewParagraph
		{
			get { return m_fRequestAppendSpaceForFirstWordInNewParagraph; }
			set { m_fRequestAppendSpaceForFirstWordInNewParagraph = value; }
		}

		/// <summary>
		/// We want to append a space if its the first (non-zero-lengthed) word in new paragraph
		/// (after the first paragraph).
		/// </summary>
		/// <param name="s"></param>
		private void AppendSpaceForFirstWordInNewParagraph(string s)
		{
			if (m_fNewParagraph && !String.IsNullOrEmpty(s))
			{
				if (RequestAppendSpaceForFirstWordInNewParagraph)
					m_builder.Append(" ");
				m_fNewParagraph = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate a string into our result, with known writing system.
		/// This base implementation ignores the writing system.
		/// </summary>
		/// <param name="s">The s.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		internal override void AddResultString(string s, int ws)
		{
			// we want to prepend a space to our string before appending more text.
			AppendSpaceForFirstWordInNewParagraph(s);
			m_builder.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, ws);
			m_builder.Append(s);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>The result.</value>
		/// ------------------------------------------------------------------------------------
		public ITsString Result
		{
			get { return m_builder.GetString(); }
		}
	}
	#endregion // TsStringCollectorEnv
}
