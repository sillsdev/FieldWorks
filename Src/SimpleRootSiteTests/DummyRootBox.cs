// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public class DummyRootBox : IVwRootBox
	{
		internal ISilDataAccess m_dummyDataAccess = new DummyDataAccess();
		internal DummyVwSelection m_dummySelection;
		internal SimpleRootSite m_dummySimpleRootSite;

		// current total text.
		public string Text = string.Empty;

		public DummyRootBox(SimpleRootSite srs)
		{
			m_dummySimpleRootSite = srs;
			m_dummySelection = new DummyVwSelection(this, 0, 0);
		}

		#region IVwRootBox implementation
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			throw new NotSupportedException();
		}

		public void SetSite(IVwRootSite _vrs)
		{
			throw new NotSupportedException();
		}

		public void SetRootObjects(int[] _rghvo, IVwViewConstructor[] _rgpvwvc, int[] _rgfrag, IVwStylesheet _ss, int chvo)
		{
			throw new NotSupportedException();
		}

		public void SetRootObject(int hvo, IVwViewConstructor _vwvc, int frag, IVwStylesheet _ss)
		{
			throw new NotSupportedException();
		}

		public void SetRootVariant(object v, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public void SetRootString(ITsString _tss, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public object GetRootVariant()
		{
			throw new NotSupportedException();
		}

		public void Serialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotSupportedException();
		}

		public void Deserialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotSupportedException();
		}

		public void WriteWpx(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotSupportedException();
		}

		public void DestroySelection()
		{
			throw new NotSupportedException();
		}

		public IVwSelection MakeTextSelection(int ihvoRoot, int cvlsi, SelLevInfo[] _rgvsli, int tagTextProp, int cpropPrevious,
			int ichAnchor, int ichEnd, int ws, bool fAssocPrev, int ihvoEnd, ITsTextProps _ttpIns, bool fInstall)
		{
			return new DummyVwSelection(this, ichAnchor, ichEnd);
		}

		public IVwSelection MakeRangeSelection(IVwSelection _selAnchor, IVwSelection _selEnd, bool fInstall)
		{
			m_dummySelection = new DummyVwSelection(this, (_selAnchor as DummyVwSelection).Anchor, (_selEnd as DummyVwSelection).End);
			return m_dummySelection;
		}

		public IVwSelection MakeSimpleSel(bool fInitial, bool fEdit, bool fRange, bool fInstall)
		{
			throw new NotSupportedException();
		}

		public IVwSelection MakeTextSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int cvsliEnd, SelLevInfo[] _rgvsliEnd,
			bool fInitial, bool fEdit, bool fRange, bool fWholeObj, bool fInstall)
		{
			throw new NotSupportedException();
		}

		public IVwSelection MakeSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int tag,
			bool fInstall)
		{
			throw new NotSupportedException();
		}

		public IVwSelection MakeSelAt(int xd, int yd, Rect rcSrc, Rect rcDst, bool fInstall)
		{
			throw new NotSupportedException();
		}

		public IVwSelection MakeSelInBox(IVwSelection _selInit, bool fEndPoint, int iLevel, int iBox, bool fInitial, bool fRange, bool fInstall)
		{
			throw new NotSupportedException();
		}

		public bool get_IsClickInText(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public bool get_IsClickInObject(int xd, int yd, Rect rcSrc, Rect rcDst, out int _odt)
		{
			throw new NotSupportedException();
		}

		public bool get_IsClickInOverlayTag(int xd, int yd, Rect rcSrc1, Rect rcDst1, out int _iGuid, out string _bstrGuids,
			out Rect _rcTag, out Rect _rcAllTags, out bool _fOpeningTag)
		{
			throw new NotSupportedException();
		}

		public void OnTyping(IVwGraphics _vg, string input, VwShiftStatus shiftStatus, ref int _wsPending)
		{
			const string BackSpace = "\b";

			if (input == BackSpace)
			{
				if (Text.Length <= 0)
				{
					return;
				}
				m_dummySelection.Anchor -= 1;
				m_dummySelection.End -= 1;
				Text = Text.Substring(0, Text.Length - 1);
				return;
			}

			var ws = m_dummySimpleRootSite.WritingSystemFactory.UserWs;
			m_dummySelection.ReplaceWithTsString(TsStringUtils.MakeString(input, ws));
		}

		public void DeleteRangeIfComplex(IVwGraphics _vg, out bool _fWasComplex)
		{
			_fWasComplex = false;
		}

		public void OnChar(int chw)
		{
			throw new NotSupportedException();
		}

		public void OnSysChar(int chw)
		{
			throw new NotSupportedException();
		}

		public int OnExtendedKey(int chw, VwShiftStatus ss, int nFlags)
		{
			throw new NotSupportedException();
		}

		public void FlashInsertionPoint()
		{
			throw new NotSupportedException();
		}

		public void MouseDown(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void MouseDblClk(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void MouseMoveDrag(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void MouseDownExtended(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void MouseUp(int xd, int yd, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void Activate(VwSelectionState vss)
		{
			throw new NotSupportedException();
		}

		public VwPrepDrawResult PrepareToDraw(IVwGraphics _vg, Rect rcSrc, Rect rcDst)
		{
			throw new NotSupportedException();
		}

		public void DrawRoot(IVwGraphics _vg, Rect rcSrc, Rect rcDst, bool fDrawSel)
		{
			throw new NotSupportedException();
		}

		public void Layout(IVwGraphics _vg, int dxsAvailWidth)
		{
			throw new NotSupportedException();
		}

		public void InitializePrinting(IVwPrintContext _vpc)
		{
			throw new NotSupportedException();
		}

		public int GetTotalPrintPages(IVwPrintContext _vpc)
		{
			throw new NotSupportedException();
		}

		public void PrintSinglePage(IVwPrintContext _vpc, int nPageNo)
		{
			throw new NotSupportedException();
		}

		public bool LoseFocus()
		{
			throw new NotSupportedException();
		}

		public void Close()
		{
		}

		public void Reconstruct()
		{
			throw new NotSupportedException();
		}

		public void OnStylesheetChange()
		{
			throw new NotSupportedException();
		}

		public void DrawingErrors(IVwGraphics _vg)
		{
			throw new NotSupportedException();
		}

		public void SetTableColWidths(VwLength[] _rgvlen, int cvlen)
		{
			throw new NotSupportedException();
		}

		public bool IsDirty()
		{
			throw new NotSupportedException();
		}

		public void GetRootObject(out int _hvo, out IVwViewConstructor _pvwvc, out int _frag, out IVwStylesheet _pss)
		{
			throw new NotSupportedException();
		}

		public void DrawRoot2(IVwGraphics _vg, Rect rcSrc, Rect rcDst, bool fDrawSel, int ysTop, int dysHeight)
		{
			throw new NotSupportedException();
		}

		public bool DoSpellCheckStep()
		{
			throw new NotSupportedException();
		}

		public bool IsSpellCheckComplete()
		{
			throw new NotSupportedException();
		}

		public void RestartSpellChecking()
		{
			throw new NotSupportedException();
		}

		public void SetSpellingRepository(IGetSpellChecker _gsp)
		{
		}

		public ISilDataAccess DataAccess
		{
			get
			{
				return m_dummyDataAccess;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public IRenderEngineFactory RenderEngineFactory { get; set; }

		public ITsStrFactory TsStrFactory { get; set; }

		public IVwOverlay Overlay
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public IVwSelection Selection => m_dummySelection;

		public VwSelectionState SelectionState
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int Height => 0;

		public int Width => 0;

		public IVwRootSite Site => m_dummySimpleRootSite;

		public IVwStylesheet Stylesheet
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int XdPos
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public IVwSynchronizer Synchronizer
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int MaxParasToScan
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public bool IsCompositionInProgress
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool IsPropChangedInProgress => false;
		#endregion

		private sealed class DummyDataAccess : ISilDataAccess
		{
			IActionHandler m_actionHandler = new NullOpActionHandler();

			#region ISilDataAccess implementation
			public int get_ObjectProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public int get_VecItem(int hvo, int tag, int index)
			{
				throw new NotSupportedException();
			}

			public int get_VecSize(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public int get_VecSizeAssumeCached(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public void VecProp(int hvo, int tag, int chvoMax, out int _chvo, ArrayPtr _rghvo)
			{
				throw new NotSupportedException();
			}

			public void BinaryPropRgb(int obj, int tag, ArrayPtr _rgb, int cbMax, out int _cb)
			{
				throw new NotSupportedException();
			}

			public Guid get_GuidProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public int get_ObjFromGuid(Guid uid)
			{
				throw new NotSupportedException();
			}

			public int get_IntProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public long get_Int64Prop(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public bool get_BooleanProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
			{
				throw new NotSupportedException();
			}

			public ITsMultiString get_MultiStringProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public object get_Prop(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public ITsString get_StringProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public long get_TimeProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public string get_UnicodeProp(int obj, int tag)
			{
				throw new NotSupportedException();
			}

			public void set_UnicodeProp(int obj, int tag, string bstr)
			{
				throw new NotSupportedException();
			}

			public void UnicodePropRgch(int obj, int tag, ArrayPtr _rgch, int cchMax, out int _cch)
			{
				throw new NotSupportedException();
			}

			public object get_UnknownProp(int hvo, int tag)
			{
				throw new NotSupportedException();
			}

			public void BeginUndoTask(string bstrUndo, string bstrRedo)
			{
				throw new NotSupportedException();
			}

			public void EndUndoTask()
			{
				throw new NotSupportedException();
			}

			public void ContinueUndoTask()
			{
				throw new NotSupportedException();
			}

			public void EndOuterUndoTask()
			{
				throw new NotSupportedException();
			}

			public void Rollback()
			{
				throw new NotSupportedException();
			}

			public void BreakUndoTask(string bstrUndo, string bstrRedo)
			{
				throw new NotSupportedException();
			}

			public void BeginNonUndoableTask()
			{
				throw new NotSupportedException();
			}

			public void EndNonUndoableTask()
			{
				throw new NotSupportedException();
			}

			public IActionHandler GetActionHandler()
			{
				return m_actionHandler;
			}

			public void SetActionHandler(IActionHandler _acth)
			{
				m_actionHandler = _acth;
			}

			public void DeleteObj(int hvoObj)
			{
				throw new NotSupportedException();
			}

			public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
			{
				throw new NotSupportedException();
			}

			public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
			{
				throw new NotSupportedException();
			}

			public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
			{
				throw new NotSupportedException();
			}

			public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd, int hvoDstOwner, int tagDst, int ihvoDstStart)
			{
				throw new NotSupportedException();
			}

			public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst, int ihvoDstStart)
			{
				throw new NotSupportedException();
			}

			public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
			{
				throw new NotSupportedException();
			}

			public void SetObjProp(int hvo, int tag, int hvoObj)
			{
				throw new NotSupportedException();
			}

			public void RemoveObjRefs(int hvo)
			{
				throw new NotSupportedException();
			}

			public void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
			{
				throw new NotSupportedException();
			}

			public void SetGuid(int hvo, int tag, Guid uid)
			{
				throw new NotSupportedException();
			}

			public void SetInt(int hvo, int tag, int n)
			{
				throw new NotSupportedException();
			}

			public void SetInt64(int hvo, int tag, long lln)
			{
				throw new NotSupportedException();
			}

			public void SetBoolean(int hvo, int tag, bool n)
			{
				throw new NotSupportedException();
			}

			public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
			{
				throw new NotSupportedException();
			}

			public void SetString(int hvo, int tag, ITsString _tss)
			{
				throw new NotSupportedException();
			}

			public void SetTime(int hvo, int tag, long lln)
			{
				throw new NotSupportedException();
			}

			public void SetUnicode(int hvo, int tag, string _rgch, int cch)
			{
				throw new NotSupportedException();
			}

			public void SetUnknown(int hvo, int tag, object _unk)
			{
				throw new NotSupportedException();
			}

			public void AddNotification(IVwNotifyChange _nchng)
			{
				throw new NotSupportedException();
			}

			public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				throw new NotSupportedException();
			}

			public void RemoveNotification(IVwNotifyChange _nchng)
			{
				throw new NotSupportedException();
			}

			public int GetDisplayIndex(int hvoOwn, int tag, int ihvo)
			{
				throw new NotSupportedException();
			}

			public int get_WritingSystemsOfInterest(int cwsMax, ArrayPtr _ws)
			{
				throw new NotSupportedException();
			}

			public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
			{
				throw new NotSupportedException();
			}

			public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
			{
				throw new NotSupportedException();
			}

			public string GetRelExtra(int hvoSrc, int tag, int ihvo)
			{
				throw new NotSupportedException();
			}

			public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
			{
				throw new NotSupportedException();
			}

			public bool IsDirty()
			{
				throw new NotSupportedException();
			}

			public void ClearDirty()
			{
				throw new NotSupportedException();
			}

			public bool get_IsValidObject(int hvo)
			{
				throw new NotSupportedException();
			}

			public bool get_IsDummyId(int hvo)
			{
				throw new NotSupportedException();
			}

			public int GetObjIndex(int hvoOwn, int flid, int hvo)
			{
				throw new NotSupportedException();
			}

			public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
			{
				throw new NotSupportedException();
			}

			public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim, int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
			{
				throw new NotSupportedException();
			}

			public ILgWritingSystemFactory WritingSystemFactory
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			public IFwMetaDataCache MetaDataCache
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}
			#endregion

			private sealed class NullOpActionHandler : IActionHandler
			{
				#region IActionHandler implementation
				public void BeginUndoTask(string bstrUndo, string bstrRedo)
				{
				}

				public void EndUndoTask()
				{
				}

				public void ContinueUndoTask()
				{
				}

				public void EndOuterUndoTask()
				{
				}

				public void BreakUndoTask(string bstrUndo, string bstrRedo)
				{
				}

				public void BeginNonUndoableTask()
				{
				}

				public void EndNonUndoableTask()
				{
				}

				public void CreateMarkIfNeeded(bool fCreateMark)
				{
				}

				public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
				{
				}

				public void AddAction(IUndoAction _uact)
				{
				}

				public string GetUndoText()
				{
					return string.Empty;
				}

				public string GetUndoTextN(int iAct)
				{
					return string.Empty;
				}

				public string GetRedoText()
				{
					return string.Empty;
				}

				public string GetRedoTextN(int iAct)
				{
					return string.Empty;
				}

				public bool CanUndo()
				{
					return false;
				}

				public bool CanRedo()
				{
					return false;
				}

				public UndoResult Undo()
				{
					return default(UndoResult);
				}

				public UndoResult Redo()
				{
					return default(UndoResult);
				}

				public void Rollback(int nDepth)
				{
				}

				public void Commit()
				{
				}

				public void Close()
				{
				}

				public int Mark()
				{
					return 0;
				}

				public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
				{
					return false;
				}

				public void DiscardToMark(int hMark)
				{
				}

				public bool get_TasksSinceMark(bool fUndo)
				{
					return false;
				}

				public int CurrentDepth => 0;

				public int TopMarkHandle => 0;

				public int UndoableActionCount => 0;

				public int UndoableSequenceCount => 0;

				public int RedoableSequenceCount => 0;

				public bool IsUndoOrRedoInProgress => false;

				public bool SuppressSelections => false;
				#endregion
			}
		}
	}
}