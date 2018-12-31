// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// CollectorEnv is a base class for objects that implement the IVwEnv interface. The main
	/// implementation (in C++) is normally used to actually produce a Views-based display. This
	/// class and its subclasses are used by the same code, but for purposes like producing an
	/// equivalent string, or testing whether some display will be blank.
	/// Note that collectorEnv does not currently fully support multiple-root-object situations
	/// (you would have to call the outer Display() method for each root box, and it would
	/// not track ihvoRoot...I'm not quite sure where that might be needed.)
	/// </summary>
	internal class CollectorEnv : IVwEnv
	{
		/// <summary>
		/// Converts the VwEnv Collector Stack to a SelLevInfo array.
		/// </summary>
		/// <param name="locationStack">The vwEnv collector stack.</param>
		/// <param name="cPropPrevRootLevel">The count of previous occurrences of the base item on the stack..</param>
		internal static SelLevInfo[] ConvertVwEnvStackToSelLevInfo(IList<StackItem> locationStack, int cPropPrevRootLevel)
		{
			var location = new SelLevInfo[locationStack.Count];
			for (int i = 0, iSelLevInfo = locationStack.Count - 1; i < locationStack.Count; i++, iSelLevInfo--)
			{
				var item = locationStack[i];
				location[iSelLevInfo] = item.ToSelLevInfo();
				if (i > 0)
				{
					var prevItem = locationStack[i - 1];
					var tagLevel = location[iSelLevInfo].tag;
					// See how many times the next outer level has seen the
					// property for this level.
					location[iSelLevInfo].cpropPrevious = prevItem.m_cpropPrev.GetCount(tagLevel);
				}
				else
				{
					location[iSelLevInfo].cpropPrevious = cPropPrevRootLevel;
				}
			}
			return location;
		}

		#region Data members
		/// <summary />
		protected IVwEnv m_baseEnv;

		/// <summary>Collection of StackItems, keeps track of outer context.</summary>
		protected List<StackItem> m_stack = new List<StackItem>();

		/// <summary />
		protected int m_ws;
		/// <summary>tracks the current vector item being added in AddObj </summary>
		protected IDictionary<int, int> m_vectorItemIndex = new Dictionary<int, int>();
		/// <summary>Used for multiple properties at the root level</summary>
		protected PrevPropCounter m_cpropPrev = new PrevPropCounter();
		/// <summary>set by OpenProp and closeObject, cleared by CloseProp and OpenObject;
		/// Keeps track of whether things currently being added are part of some
		/// known property of the current object.</summary>
		protected bool m_fIsPropOpen;
		/// <summary>
		/// Current flow object is a paragraph (or possibly a span..that is considered an equivalent state).
		/// This is maintained by code in various OpenX and CloseX methods. It should be correct if the
		/// client code is correct: it closes every paragraph it opens, and only nests spans and inner piles
		/// inside paragraphs. Client code that breaks these rules could get different results in this
		/// code and the real VwEnv. To fully match the behavior we would have to implement a stack of
		/// open flow objects.</summary>
		private bool m_fIsParaOpen;
		/// <summary>Set if we add something while m_fIsPropOpen is true;
		/// cleared (and we note an occurrence of ktagNotAnAttr) if it is set
		/// when clearing m_fIsPropOpen.</summary>
		protected bool m_fGotNonPropInfo;
		#endregion

		#region Constructor

		/// <summary>
		/// Create one. If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Data access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		public CollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
		{
			m_baseEnv = baseEnv;
			DataAccess = sda;
			OpenObject = baseEnv?.OpenObject ?? hvoRoot;
		}
		#endregion

		/// <summary>
		/// Get/set the metadata cache.  This is needed to detect virtual properties in setting
		/// notifiers.  See LT-8245.
		/// </summary>
		public IFwMetaDataCache MetaDataCache { get; set; }

		/// <summary>
		/// Get/set the cache data access.  This is needed to get virtual properties handlers in
		/// setting notifiers.  See LT-8245.
		/// </summary>
		public IVwCacheDa CacheDa { get; set; }

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset()
		{
			m_vectorItemIndex.Clear();
			m_ws = 0;
			CurrentPropTag = 0;
			m_stack.Clear();
			m_cpropPrev = new PrevPropCounter();
		}

		/// <summary>
		/// Gets the property currently being built.
		/// </summary>
		/// <value>The current prop tag.</value>
		protected int CurrentPropTag { get; set; }

		/// <summary>
		/// Gets the count of previous occurrences of the given property at our current stack
		/// level.
		/// </summary>
		protected int CPropPrev(int tag)
		{
			if (m_stack.Count == 0)
			{
				return m_cpropPrev.GetCount(tag);
			}
			var top = m_stack[m_stack.Count - 1];
			return top.m_cpropPrev.GetCount(tag);
		}

		/// <summary>
		/// Gets the count of the previous occurrences of the given property at the bottom
		/// of the stack.
		/// </summary>
		public int CountOfPrevPropAtRoot => m_stack.Count == 0 ? m_cpropPrev.GetCount(CurrentPropTag) : m_cpropPrev.GetCount(m_stack[0].m_tag);

		/// <summary>
		/// Accumulate a TsString into our result. The base implementation does nothing.
		/// </summary>
		public virtual void AddTsString(ITsString tss)
		{
			AddResultString(tss.Text);
		}

		/// <summary>
		/// Return true if we don't need to process any further. Some methods may be able
		/// to truncate operations.
		/// </summary>
		protected virtual bool Finished => false;

		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		public virtual void AddResultString(string s)
		{
		}

		/// <summary>
		/// Accumulate a string into our result, with known writing system.
		/// This base implementation ignores the writing system.
		/// </summary>
		public virtual void AddResultString(string s, int ws)
		{
			AddResultString(s);
		}

		/// <summary>
		/// Opens the object.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="ihvo">The index of this object in the collection being displayed.</param>
		protected virtual void OpenTheObject(int hvo, int ihvo)
		{
			m_stack.Add(new StackItem(OpenObject, hvo, CurrentPropTag, ihvo));
			OpenObject = hvo;
			m_fIsPropOpen = false;
		}

		/// <summary>
		/// look at top of context stack
		/// </summary>
		protected StackItem PeekStack => m_stack != null && m_stack.Count > 0 ? m_stack[m_stack.Count - 1] : null;

		/// <summary>
		/// Closes the object.
		/// </summary>
		protected virtual void CloseTheObject()
		{
			// should close any props we open before closing obj.
			Debug.Assert(!m_fIsPropOpen);
			// Note any trailing non-prop info at the end of the current object.
			CheckForNonPropInfo();
			var top = m_stack[m_stack.Count - 1];
			m_stack.RemoveAt(m_stack.Count - 1);
			OpenObject = top.m_hvoOuter;
			CurrentPropTag = top.m_tag;
			m_ws = 0;
			// objects are always added as part of some property, so if we close one,
			// we must be back inside the property we were in when we added it.
			m_fIsPropOpen = true;
		}

		/// <summary>
		/// calls OpenProp and starts tracking the vector property.
		/// </summary>
		protected virtual void OpenVecProp(int tag)
		{
			OpenProp(tag, 0);
			m_vectorItemIndex[tag] = -1;
		}

		/// <summary>
		/// Opens the prop.
		/// </summary>
		protected virtual void OpenProp(int tag)
		{
			OpenProp(tag, 0);
		}

		/// <summary>
		/// Opens the prop.
		/// </summary>
		protected virtual void OpenProp(int tag, int ws)
		{
			CheckForNonPropInfo();
			m_fIsPropOpen = true;
			IncrementPropCount(tag);
			CurrentPropTag = tag;
			m_ws = ws;
		}

		/// <summary>
		/// See whether anything has been added to the view while no property was open,
		/// and if so, bump the gap-in-attrs count.
		/// </summary>
		protected virtual void CheckForNonPropInfo()
		{
			if (Finished)
			{
				return;
			}
			if (m_fGotNonPropInfo)
			{
				IncrementPropCount((int)VwSpecialAttrTags.ktagGapInAttrs);
				m_fGotNonPropInfo = false;
			}
		}

		/// <summary>
		/// Increments the property count for the given tag.
		/// </summary>
		private void IncrementPropCount(int tag)
		{
			if (m_stack.Count == 0)
			{
				m_cpropPrev.Increment(tag);
			}
			else
			{
				var top = m_stack[m_stack.Count - 1];
				top.m_cpropPrev.Increment(tag);
			}
		}

		/// <summary>
		/// Closes the prop. Does nothing as yet, but keeps things analogous to real VwEnv.
		/// </summary>
		protected virtual void CloseProp()
		{
			m_fIsPropOpen = false;
		}

		#region IVwEnv Members

		/// <summary>
		/// A rather arbitrary way of representing a bar as a string.
		/// </summary>
		public virtual void AddSeparatorBar()
		{
			NoteAddingSomething();
			AddResultString("|");
		}

		/// <summary />
		public void AddLazyItems(int[] rghvo, int chvo, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException("AddLazyItems is not supported");
		}

		/// <summary>
		/// Add all the alternatives, in a very primitive arrangement. Not used much if at all.
		/// </summary>
		public void AddStringAlt(int tag)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotSupportedException("AddStringAlt is not yet supported in CollectorEnv");
		}

		/// <summary />
		public virtual void AddUnicodeProp(int tag, int ws, IVwViewConstructor vwvc)
		{
			OpenProp(tag, ws);
			AddResultString(DataAccess.get_UnicodeProp(OpenObject, tag), ws);
			CloseProp();
		}

		/// <summary>
		/// If we're capturing a test on top of a real VwEnv, pass the information on
		/// so things will regenerate properly.
		/// </summary>
		public virtual void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
		{
			m_baseEnv?.NoteDependency(rghvo, rgtag, chvo);
		}

		/// <summary>
		/// If we're capturing a test on top of a real VwEnv, pass the information on
		/// so things will regenerate properly.
		/// </summary>
		public void NoteStringValDependency(int hvo, int tag, int ws, ITsString tssVal)
		{
			m_baseEnv?.NoteStringValDependency(hvo, tag, ws, tssVal);
		}

		/// <summary>
		/// Set text properties. Nothing to do here. None of the collectors cares about text
		/// properties (yet).
		/// </summary>
		public virtual ITsTextProps Props
		{
			set { }
		}

		/// <summary>Call virtual OpenParagraph</summary>
		public void OpenMappedTaggedPara()
		{
			OpenParagraph();
		}

		/// <summary>
		/// No easy way to implement this, it depends on a StrUni format method.
		/// </summary>
		public virtual void AddGenDateProp(int tag)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotSupportedException("AddGenDateProp not supported for CollectorEnv");
		}

		/// <summary>
		/// An implementation equivalent to the IVwEnv one is fairly trivial, but so unsatisfactory
		/// that I'm sure we're not using it.
		/// </summary>
		public void AddStringAltSeq(int tag, int[] rgenc, int cws)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			throw new NotSupportedException("AddStringAltSeq not supported for CollectorEnv");
		}

		/// <summary>
		/// Adds the obj vec.
		/// </summary>
		public virtual void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			OpenVecProp(tag);
			vc.DisplayVec(this, OpenObject, tag, frag);
			CloseProp();
		}

		/// <summary>
		/// Nothing to do here.
		/// </summary>
		public virtual void CloseParagraph()
		{
			CloseFlowObject();
			m_fIsParaOpen = false;
		}

		/// <summary />
		public virtual void OpenParagraph()
		{
			OpenFlowObject();
			m_fIsParaOpen = true;
		}

		/// <summary>Call virtual OpenParagraph</summary>
		public void OpenTaggedPara()
		{
			OpenParagraph();
		}

		/// <summary>
		/// Adds the string.
		/// </summary>
		public virtual void AddString(ITsString tss)
		{
			NoteAddingSomething();
			AddTsString(tss);
		}

		/// <summary />
		public void CloseTableBody()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Call virtual OpenParagraph
		/// </summary>
		public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] _rgOverrideProperties)
		{
			OpenParagraph();
		}

		/// <summary />
		public int CurrentObject()
		{
			return OpenObject;
		}

		/// <summary>
		/// Nothing to do here. None of our collectors cares about string properties (yet).
		/// </summary>
		public virtual void set_StringProperty(int sp, string bstrValue)
		{
		}

		/// <summary />
		public void CloseDiv()
		{
			CloseFlowObject();
		}

		/// <summary />
		public void CloseTableRow()
		{
			CloseFlowObject();
		}

		/// <summary />
		public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
		{
			OpenFlowObject();
		}

		/// <summary>Call virtual CloseParagraph</summary>
		public virtual void CloseInnerPile()
		{
			CloseParagraph();
		}

		/// <summary>
		/// Gets the object we are currently building a display of. The name is unfortunately
		/// fixed by the IVwEnv interface. Compare OpenTheObject(), which handles changing
		/// which one is current.
		/// </summary>
		public int OpenObject { get; protected set; }

		/// <summary>
		/// Member AddStringProp
		/// </summary>
		public virtual void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			AddTsString(DataAccess.get_StringProp(OpenObject, tag));
			CloseProp();
		}

		/// <summary>
		/// Nothing to do here. None of our collectors cares about integer properties (yet).
		/// </summary>
		public virtual void set_IntProperty(int tpt, int tpv, int nValue)
		{
		}

		/// <summary>
		/// May need to do this one soon. Needs a new interface to give access to the IVwEnv
		/// internal methods.
		/// </summary>
		public virtual void AddTimeProp(int tag, uint flags)
		{
			// TODO: If we ever implement this, we need to do the following:
			//IncrementPropCount(tag);
			//throw new NotImplementedException("AddTimeProp not implemented by CollectorEnv");
		}

		/// <summary>
		/// Nothing to do here.
		/// </summary>
		public void OpenTableFooter()
		{
			OpenFlowObject();
		}

		/// <summary>
		/// Nothing to do here. None of our collectors cares about flow object organization.
		/// </summary>
		public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
		{
		}

		/// <summary>
		/// Nothing to do here.
		/// </summary>
		public void CloseTableHeaderCell()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Nothing to do here.
		/// </summary>
		public virtual void CloseTableCell()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Member AddIntProp
		/// </summary>
		public virtual void AddIntProp(int tag)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			AddResultString(DataAccess.get_IntProp(OpenObject, tag).ToString());
			CloseProp();
		}

		/// <summary>
		/// Nothing to do here.
		/// </summary>
		public virtual void OpenTableRow()
		{
			OpenFlowObject();
		}

		/// <summary>
		/// Member AddAdjustedPicture
		/// </summary>
		public virtual void AddPicture(IPicture pict, int tag, int dxmpWidth, int dympHeight)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			AddResultString("A picture"); // REVIEW: This seems weird. Is the caption handled correctly?
			CloseProp();
		}

		/// <summary>
		/// Member SetParagraphMark
		/// </summary>
		/// <param name="boundaryMark">enumeration value used to represent the paragraph or section
		/// boundary and whether it is highlighted or not: endOfParagraph,	endOfSection,
		/// endOfParagraphHighlighted, or endofSectionHighlighted
		/// </param>
		public virtual void SetParagraphMark(VwBoundaryMark boundaryMark)
		{
		}

		/// <summary />
		public virtual void OpenTableBody()
		{
			OpenFlowObject();
		}

		/// <summary>
		/// Gets the outer object.
		/// </summary>
		/// <param name="iLevel">Index of the outer level to retrieve.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvo">The ihvo.</param>
		public void GetOuterObject(int iLevel, out int hvo, out int tag, out int ihvo)
		{
			var clevBase = 0;
			if (m_baseEnv != null)
			{
				clevBase = m_baseEnv.EmbeddingLevel;
			}
			if (iLevel >= clevBase)
			{
				// A row from our own stack
				var item = m_stack[iLevel - clevBase];
				hvo = item.m_hvoOuter;
				tag = item.m_tag;
				ihvo = item.m_ihvo;
			}
			else
			{
				m_baseEnv.GetOuterObject(iLevel, out hvo, out tag, out ihvo);
			}
		}

		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		public virtual void AddStringAltMember(int tag, int ws, IVwViewConstructor vwvc)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag, ws);
			AddTsString(DataAccess.get_MultiStringAlt(OpenObject, tag, ws));
			CloseProp();
		}

		/// <summary>
		/// Probably unused, but we'll put something in because a window is probably 'something'
		/// and therefore interesting to the TestCollectorEnv.
		/// </summary>
		public virtual void AddWindow(IVwEmbeddedWindow ew, int dmpAscent, bool fJustifyRight, bool fAutoShow)
		{
			AddResultString("A window");
		}

		/// <summary>
		/// For our purpose, laziness is not relevant. Equivalent to the non-lazy version.
		/// </summary>
		public void AddLazyVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			AddObjVecItems(tag, vc, frag);
		}

		/// <summary>
		/// Adds the obj prop.
		/// </summary>
		public virtual void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
			{
				return;
			}
			var hvoItem = DataAccess.get_ObjectProp(OpenObject, tag);
			if (hvoItem != 0)
			{
				OpenProp(tag);
				OpenTheObject(hvoItem, 0);
				vc.Display(this, hvoItem, frag);
				CloseTheObject();
				CloseProp();
			}
		}

		/// <summary>
		/// Answer some arbitrary guess. The string length is not very good, but probably
		/// good enough for collector results. Non-zero (except for empty strings) in case
		/// something divides by it.
		/// </summary>
		public void get_StringWidth(ITsString tss, ITsTextProps ttp, out int dmpx, out int dmpy)
		{
			dmpx = tss.Length;
			dmpy = 10;
		}

		/// <summary />
		public void OpenSpan()
		{
			OpenFlowObject();
		}

		/// <summary>
		/// Member AddIntPropPic
		/// </summary>
		public virtual void AddIntPropPic(int tag, IVwViewConstructor vc, int frag, int nMin, int nMax)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			AddResultString("A picture"); // REVIEW: This seems weird.
			CloseProp();
		}

		/// <summary />
		/// <param name="pict">pict</param>
		/// <param name="tag">tag</param>
		/// <param name="ttpCaption">The TTP caption.</param>
		/// <param name="hvoCmFile">The hvo cm file.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="dxmpWidth">Width of the DXMP.</param>
		/// <param name="dympHeight">Height of the dymp.</param>
		/// <param name="vc">The view constructor.</param>
		public void AddPictureWithCaption(IPicture pict, int tag, ITsTextProps ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor vc)
		{
			if (Finished)
			{
				return;
			}
			AddPicture(pict, tag, dxmpWidth, dympHeight);
			OpenParagraph();
			AddStringAltMember(CmPictureTags.kflidCaption, ws, vc);
			CloseParagraph();
		}

		/// <summary />
		public void CloseTableFooter()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Adds the prop.
		/// </summary>
		public virtual void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			AddTsString(vc.DisplayVariant(this, tag, frag));
			CloseProp();
		}

		/// <summary />
		public void OpenTableHeader()
		{
			OpenFlowObject();
		}

		/// <summary>Call virtual OpenParagraph</summary>
		public virtual void OpenInnerPile()
		{
			OpenParagraph();
		}

		/// <summary />
		public void CloseTableHeader()
		{
			CloseFlowObject();
		}

		/// <summary />
		public virtual void OpenTableCell(int nRowSpan, int nColSpan)
		{
			OpenFlowObject();
		}

		/// <summary>
		/// Gets a DataAccess
		/// </summary>
		public ISilDataAccess DataAccess { get; }

		/// <summary />
		public void CloseTable()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Adds the obj.
		/// </summary>
		public virtual void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			if (Finished)
			{
				return;
			}
			var wasPropOpen = m_fIsPropOpen;
			if (!m_fIsPropOpen)
			{
				// AddObj in the middle of an arbitrary object is treated as the phony property NotAnAttr.
				// If a property is already open (AddObjVec), we treat it as one of the objects in
				// that property.
				OpenProp((int)VwSpecialAttrTags.ktagNotAnAttr);
			}
			int ihvo;
			if (m_vectorItemIndex.TryGetValue(CurrentPropTag, out ihvo))
			{
				m_vectorItemIndex[CurrentPropTag] = ++ihvo;
			}
			else
			{
				ihvo = 0; // not a vector item.
			}
			OpenTheObject(hvoItem, ihvo);
			vc.Display(this, hvoItem, frag);
			CloseTheObject();
			if (!wasPropOpen)
			{
				CloseProp();
			}
		}

		/// <summary>
		/// Gets a EmbeddingLevel
		/// </summary>
		public int EmbeddingLevel => m_baseEnv == null ? m_stack.Count : m_baseEnv.EmbeddingLevel + m_stack.Count;

		/// <summary>
		/// Also not implemented at all by VwEnv.
		/// </summary>
		public virtual void AddDerivedProp(int[] rgtag, int ctag, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException("AddDerivedProp not supported by CollectorEnv");
		}

		/// <summary />
		public virtual void CloseSpan()
		{
			CloseFlowObject();
		}

		/// <summary>
		/// Nothing to do here. None of our collectors cares about flow object organization.
		/// </summary>
		public void MakeColumns(int nColSpan, VwLength vlWidth)
		{
		}

		/// <summary />
		public void OpenDiv()
		{
			OpenFlowObject();
		}

		/// <summary>Call virtual OpenParagraph</summary>
		public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign)
		{
			OpenParagraph();
		}

		/// <summary>Call virtual OpenParagraph</summary>
		public void OpenMappedPara()
		{
			OpenParagraph();
		}

		/// <summary />
		public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
		{
			OpenFlowObject();
		}

		/// <summary />
		public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		{
			NoteAddingSomething();
			AddResultString("A rectangle"); // makes it non-empty, at least.
		}

		/// <summary />
		public virtual void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			int[] items = null;
			var managedSda = DataAccess as ISilDataAccessManaged;
			int cobj;
			if (managedSda != null)
			{
				// If the vector should be virtual, we have to compute it only once.
				// Note: we COULD do this with the VecProp method of the regular ISilDataAccess.
				// But in practice the SDA will (almost?) always be a managed one, and using the
				// COM VecProp involves marshalling that is messy and slow on both sides.
				items = managedSda.VecProp(OpenObject, tag);
				cobj = items.Length;
			}
			else
			{
				cobj = DataAccess.get_VecSize(OpenObject, tag);
			}
			for (var i = 0; i < cobj; i++)
			{
				var hvoItem = items?[i] ?? DataAccess.get_VecItem(OpenObject, tag, i);
				if (DisplayThisObject(hvoItem, tag))
				{
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				}
				if (Finished)
				{
					break;
				}
			}

			CloseProp();
		}

		/// <summary>
		/// Adds the obj vec items in reverse order.
		/// </summary>
		public virtual void AddReversedObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			if (Finished)
			{
				return;
			}
			OpenProp(tag);
			var cobj = DataAccess.get_VecSize(OpenObject, tag);
			for (var i = cobj - 1; i >= 0; --i)
			{
				var hvoItem = DataAccess.get_VecItem(OpenObject, tag, i);
				if (DisplayThisObject(hvoItem, tag))
				{
					OpenTheObject(hvoItem, i);
					vc.Display(this, hvoItem, frag);
					CloseTheObject();
				}
				if (Finished)
				{
					break;
				}
			}
			CloseProp();
		}

		/// <summary>
		/// Sub-classes can override
		/// </summary>
		protected virtual bool DisplayThisObject(int hvoItem, int tag)
		{
			// no-op: Always assume we want to display an object
			return true;
		}
		#endregion

		/// <summary>
		/// Opens a flow object. Default only cares for the purpose of keeping track of
		/// non-property things added, which affects our count of gaps in properties,
		/// which is important for figuring out which literal a selection is part of.
		/// </summary>
		protected virtual void OpenFlowObject()
		{
			NoteAddingSomething();
		}

		/// <summary>
		/// Note that something (box, text, picture) is being added to the display.
		/// </summary>
		private void NoteAddingSomething()
		{
			if (!m_fIsPropOpen)
			{
				m_fGotNonPropInfo = true;
			}
		}

		/// <summary>
		/// Closes a flow object. Default implementation doesn't care about flow object
		/// organization.
		/// </summary>
		protected virtual void CloseFlowObject()
		{
		}

		/// <summary>
		/// Optionally apply an XSLT to the output file.
		/// </summary>
		public virtual void PostProcess(string sXsltFile, string sOutputFile, int iPass)
		{
			if (string.IsNullOrEmpty(sXsltFile))
			{
				return;
			}
			ProcessXsltForPass(sXsltFile, sOutputFile, iPass);
		}

		/// <summary>
		/// Apply the XSLT to the output file, first renaming it so that the user sees the
		/// expected final output file.
		/// </summary>
		public static void ProcessXsltForPass(string sXsltFile, string sOutputFile, int iPass)
		{
			var sIntermediateFile = RenameOutputToPassN(sOutputFile, iPass);
			var xsl = new XslCompiledTransform();
			xsl.Load(sXsltFile);
			xsl.Transform(sIntermediateFile, sOutputFile);
			// Deleting them deals with LT-6345,
			// which asked that they be put in the temp folder.
			// But moving them to the temp directory is better for debugging errors.
			FileUtils.MoveFileToTempDirectory(sIntermediateFile, "FieldWorks-Export");
		}

		/// <summary>
		/// Renames the output to pass N.
		/// </summary>
		public static string RenameOutputToPassN(string sOutputFile, int iPass)
		{
			var idx = sOutputFile.LastIndexOfAny(new char[] { '/', '\\' });
			if (idx < 0)
			{
				idx = 0;
			}
			else
			{
				++idx;
			}
			var sIntermediateFile = sOutputFile.Substring(0, idx) + "Phase" + iPass + "-" + sOutputFile.Substring(idx);
			try
			{
				File.Delete(sIntermediateFile);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}
			for (var tries = 0; tries < 5; tries++)
			{
				try
				{
					File.Move(sOutputFile, sIntermediateFile);
					return sIntermediateFile;
				}
				catch (IOException)
				{
					// Sometimes the system seems to think the file we just wrote is still locked.
					// A short pause may save the day.
					Thread.Sleep(200);
				}
			}
			// If five tries didn't do it, try one more time and let it die if we still can't.
			File.Move(sOutputFile, sIntermediateFile);
			return sIntermediateFile;
		}

		#region IVwEnv Members

		/// <summary>
		/// Do nothing.  This doesn't affect the collection of data.
		/// </summary>
		public void EmptyParagraphBehavior(int behavior)
		{
		}

		/// <summary>
		/// Current flow object is a paragraph. (But being in a span it will still be true.)
		/// </summary>
		public bool IsParagraphOpen()
		{
			return m_fIsParaOpen;
		}

		#endregion
	}
}