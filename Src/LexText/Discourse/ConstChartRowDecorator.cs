// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConstChartRowDecorator.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Discourse
{

	/// <summary>
	/// This decorator provides the chart body (in particular the MakeCellsMethod method object)
	/// the ability to store up a right to left generation of a chart row and spit it out to the
	/// view constructor in the left to right fashion that it needs. If the chart is left to right
	/// (the more common case), the decorator just feeds through to the base class.
	/// </summary>
	class ChartRowEnvDecorator : IVwEnv
	{
		protected IVwEnv m_vwEnv; // protected for testing
		protected int m_numOfCalls;
		protected List<StoredMethod> m_calledMethods;
		protected List<int> m_iStartEmbedding;
		protected bool m_fInRegurgitation;

		public ChartRowEnvDecorator(IVwEnv vwEnv)
			: this()
		{
			m_vwEnv = vwEnv;
		}

		/// <summary>
		/// For testing without a 'real' vwEnv object.
		/// </summary>
		public ChartRowEnvDecorator()
		{
			m_vwEnv = null;
			IsRtL = false;
			m_numOfCalls = 0;
			m_calledMethods = new List<StoredMethod>();
			m_iStartEmbedding = new List<int>();
			m_fInRegurgitation = false;
		}

		/// <summary>
		/// Must get set after the decorator object is created, but before anything is added to the vwEnv.
		/// </summary>
		internal bool IsRtL { get; set; }

		/// <summary>
		/// Contains the logic to put out the stored vwEnv calls in the order needed for
		/// a RtL presentation of the 'logical' row.
		/// Also resets variables for a possible next pass.
		/// </summary>
		internal void FlushDecorator()
		{
			if (!IsRtL)
				return;

			InternalFlush();

			ResetVariables();
		}

		protected virtual void InternalFlush()
		{
			// virtual to allow test spy to record state before flushing occurs.
			m_fInRegurgitation = true;
			// Loop in reverse over OpenTableCell calls (or other embedding objects)
			for (var i = m_iStartEmbedding.Count - 1; i > -1; i--)
			{
				var index = m_iStartEmbedding[i];
				if (m_calledMethods[index].MethodType == DecoratorMethodTypes.OpenTableCell)
				{
					index = FindMatchingEndEmbeddingIndex(index);
					PutOutTableCellStartingAt(index);
					continue;
				}
				if (m_calledMethods[index].MethodType == DecoratorMethodTypes.AddObjProp)
					RegurgitateIVwEnvCall(m_calledMethods[index]);
			}
			m_fInRegurgitation = false;
		}

		private int FindMatchingEndEmbeddingIndex(int index)
		{
			// Back up the index to include SetIntProp or NoteDependency calls before the OpenTableCell
			// call, but don't go back as far as a CloseTableCell call or AddObjProp.
			for (var i = index - 1; i > -1; i--)
			{
				if (m_calledMethods[i].MethodType == DecoratorMethodTypes.SetIntProperty ||
					m_calledMethods[i].MethodType == DecoratorMethodTypes.NoteDependency)
					continue;
				return i + 1;
			}
			return 0;
		}

		private void PutOutTableCellStartingAt(int iopenCell)
		{
			var fsentCloseCell = false;
			for (var i = iopenCell; (i < m_calledMethods.Count && !fsentCloseCell); i++)
			{
				if (m_calledMethods[i].MethodType == DecoratorMethodTypes.CloseTableCell)
					fsentCloseCell = true;
				RegurgitateIVwEnvCall(m_calledMethods[i]);
			}
		}

		private void RegurgitateIVwEnvCall(StoredMethod storedMethod)
		{
			int tag;
			int frag;
			IVwViewConstructor vc;

			switch (storedMethod.MethodType)
			{
				case DecoratorMethodTypes.AddObj:
					var hvo = (int)storedMethod.ParamArray[0];
					vc  = (IVwViewConstructor)storedMethod.ParamArray[1];
					frag = (int) storedMethod.ParamArray[2];
					m_vwEnv.AddObj(hvo, vc, frag);
					break;
				case DecoratorMethodTypes.AddObjProp:
					tag = (int)storedMethod.ParamArray[0];
					vc = (IVwViewConstructor)storedMethod.ParamArray[1];
					frag = (int)storedMethod.ParamArray[2];
					m_vwEnv.AddObjProp(tag, vc, frag);
					break;
				case DecoratorMethodTypes.AddObjVec:
					tag = (int) storedMethod.ParamArray[0];
					vc = (IVwViewConstructor) storedMethod.ParamArray[1];
					frag = (int)storedMethod.ParamArray[2];
					m_vwEnv.AddObjVec(tag, vc, frag);
					break;
				case DecoratorMethodTypes.AddObjVecItems:
					tag = (int)storedMethod.ParamArray[0];
					vc = (IVwViewConstructor)storedMethod.ParamArray[1];
					frag = (int)storedMethod.ParamArray[2];
					m_vwEnv.AddObjVecItems(tag, vc, frag);
					break;
				case DecoratorMethodTypes.AddString:
					var tsStr = (ITsString)storedMethod.ParamArray[0];
					m_vwEnv.AddString(tsStr);
					break;
				case DecoratorMethodTypes.AddStringProp:
					tag = (int)storedMethod.ParamArray[0];
					vc = (IVwViewConstructor)storedMethod.ParamArray[1];
					m_vwEnv.AddStringProp(tag, vc);
					break;
				case DecoratorMethodTypes.CloseParagraph:
					m_vwEnv.CloseParagraph();
					break;
				case DecoratorMethodTypes.CloseTableCell:
					m_vwEnv.CloseTableCell();
					break;
				case DecoratorMethodTypes.NoteDependency:
					var _rghvo = (int[])storedMethod.ParamArray[0];
					var _rgtag = (int[])storedMethod.ParamArray[1];
					var chvo = (int) storedMethod.ParamArray[2];
					m_vwEnv.NoteDependency(_rghvo, _rgtag, chvo);
					break;
				case DecoratorMethodTypes.OpenParagraph:
					m_vwEnv.OpenParagraph();
					break;
				case DecoratorMethodTypes.OpenTableCell:
					var nRows = (int) storedMethod.ParamArray[0];
					var nCols = (int)storedMethod.ParamArray[1];
					m_vwEnv.OpenTableCell(nRows, nCols);
					break;
				case DecoratorMethodTypes.SetIntProperty:
					var tpt = (int)storedMethod.ParamArray[0];
					var tpv = (int)storedMethod.ParamArray[1];
					var nValue = (int)storedMethod.ParamArray[2];
					m_vwEnv.set_IntProperty(tpt, tpv, nValue);
					break;
				case DecoratorMethodTypes.PropsSetter:
					var value = (ITsTextProps) storedMethod.ParamArray[0];
					m_vwEnv.Props = value;
					break;
				default:
					Debug.Assert(false, "Unknown DecoratorMethodType!");
					break;
			}
		}

		protected virtual void ResetVariables()
		{
			// Reset variables for next row.
			m_numOfCalls = 0;
			m_calledMethods = null;
		}

		#region Implemented IVwEnv Methods and Props

		public virtual void AddObjProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObjProp(tag, _vwvc, frag);
				return;
			}
			m_iStartEmbedding.Add(m_numOfCalls);
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObjProp, new object[] { tag, _vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddObjVec(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObjVec(tag, _vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObjVec, new object[] { tag, _vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddObjVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObjVecItems(tag, _vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObjVecItems, new object[] { tag, _vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddObj(int hvo, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObj(hvo, _vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObj, new object[] { hvo, _vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddProp(tag, _vwvc, frag);
				return;
			}
			throw new NotImplementedException();
		}

		public virtual void NoteDependency(int[] _rghvo, int[] _rgtag, int chvo)
		{
			if (!IsRtL)
			{
				m_vwEnv.NoteDependency(_rghvo, _rgtag, chvo);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.NoteDependency,
				new object[] { _rghvo, _rgtag, chvo }));
			m_numOfCalls++;
		}

		public virtual void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddStringProp(tag, _vwvc);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddStringProp, new object[] { tag, _vwvc }));
			m_numOfCalls++;
		}

		public virtual void AddString(ITsString _ss)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddString(_ss);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddString, new object[] { _ss }));
			m_numOfCalls++;
		}

		public virtual void OpenParagraph()
		{
			if (!IsRtL)
			{
				m_vwEnv.OpenParagraph();
				return;
			}
			// Stuff going in here will need to be added in 'logical' order.
			if (!m_fInRegurgitation)
			{
				m_numOfCalls += 3;
				m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.SetIntProperty,
									new object[] {
										(int) FwTextPropType.ktptRightToLeft,
										(int) FwTextPropVar.ktpvEnum,
										(int) FwTextToggleVal.kttvForceOn
									}));
				m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.SetIntProperty,
									new object[] {
										(int) FwTextPropType.ktptAlign,
										(int) FwTextPropVar.ktpvEnum,
										(int) FwTextAlign.ktalRight
									}));
			}
			else // Only count OpenParagraph if regurgitating, since other calls will be counted separately.
				m_numOfCalls++;
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenParagraph, new object[] { }));
		}

		public virtual void CloseParagraph()
		{
			if (!IsRtL)
			{
				m_vwEnv.CloseParagraph();
				return;
			}
			// Stuff that went in here needs to now be added in 'logical' order.
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.CloseParagraph, new object[] { }));
			m_numOfCalls++;
		}

		public virtual void OpenInnerPile()
		{
			if (!IsRtL)
			{
				m_vwEnv.OpenInnerPile();
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenInnerPile, new object[] {}));
			m_numOfCalls++;
		}

		public virtual void CloseInnerPile()
		{
			if (!IsRtL)
			{
				m_vwEnv.CloseInnerPile();
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.CloseInnerPile, new object[] { }));
			m_numOfCalls++;
		}

		public virtual void OpenSpan()
		{
			if (!IsRtL)
			{
				m_vwEnv.OpenSpan();
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenSpan, new object[] {}));
			m_numOfCalls++;
		}

		public virtual void CloseSpan()
		{
			if (!IsRtL)
			{
				m_vwEnv.CloseSpan();
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.CloseSpan, new object[] { }));
			m_numOfCalls++;
		}

		public virtual void OpenTableCell(int nRowSpan, int nColSpan)
		{
			if (!IsRtL)
			{
				m_vwEnv.OpenTableCell(nRowSpan, nColSpan);
				return;
			}
			// Table cells need to be opened in LTR ('display'; reverse of 'logical') order.

			// Record index of each OpenTableCell
			m_iStartEmbedding.Add(m_numOfCalls);
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenTableCell, new object[] {nRowSpan, nColSpan}));
			m_numOfCalls++;
		}

		public virtual void CloseTableCell()
		{
			if (!IsRtL)
			{
				m_vwEnv.CloseTableCell();
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.CloseTableCell, new object[] { }));
			m_numOfCalls++;
		}

		public virtual void set_IntProperty(int tpt, int tpv, int nValue)
		{
			if (!IsRtL)
			{
				m_vwEnv.set_IntProperty(tpt, tpv, nValue);
				return;
			}
			// Right to Left modifications
			if (tpt == (int) FwTextPropType.ktptBorderTrailing)
				tpt = (int) FwTextPropType.ktptBorderLeading;
			if (tpt == (int) FwTextPropType.ktptMarginTrailing)
				tpt = (int) FwTextPropType.ktptMarginLeading;

			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.SetIntProperty,
				new object[] { tpt, tpv, nValue }));
			m_numOfCalls++;
		}

		public ISilDataAccess DataAccess
		{
			get { return m_vwEnv.DataAccess; }
		}

		public ITsTextProps Props
		{
			set
			{
				if (!IsRtL)
				{
					m_vwEnv.Props = value;
					return;
				}
				m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.PropsSetter,
					new object[] { value }));
				m_numOfCalls++;
			}
		}

		#endregion

		#region Unimplemented Methods

		public void AddReversedObjVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		public void AddLazyVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		public void AddLazyItems(int[] _rghvo, int chvo, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		public void AddDerivedProp(int[] _rgtag, int ctag, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		public void NoteStringValDependency(int hvo, int tag, int ws, ITsString _tssVal)
		{
			throw new NotImplementedException();
		}

		public void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			throw new NotImplementedException();
		}

		public void AddIntProp(int tag)
		{
			throw new NotImplementedException();
		}

		public void AddIntPropPic(int tag, IVwViewConstructor _vc, int frag, int nMin, int nMax)
		{
			throw new NotImplementedException();
		}

		public void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			throw new NotImplementedException();
		}

		public void AddStringAlt(int tag)
		{
			throw new NotImplementedException();
		}

		public void AddStringAltSeq(int tag, int[] _rgenc, int cws)
		{
			throw new NotImplementedException();
		}

		public void AddTimeProp(int tag, uint flags)
		{
			throw new NotImplementedException();
		}

		public int CurrentObject()
		{
			throw new NotImplementedException();
		}

		public void GetOuterObject(int ichvoLevel, out int _hvo, out int _tag, out int _ihvo)
		{
			throw new NotImplementedException();
		}

		public void AddWindow(IVwEmbeddedWindow _ew, int dmpAscent, bool fJustifyRight, bool fAutoShow)
		{
			throw new NotImplementedException();
		}

		public void AddSeparatorBar()
		{
			throw new NotImplementedException();
		}

		public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		{
			throw new NotImplementedException();
		}

		public void OpenDiv()
		{
			throw new NotImplementedException();
		}

		public void CloseDiv()
		{
			throw new NotImplementedException();
		}

		public void OpenTaggedPara()
		{
			throw new NotImplementedException();
		}

		public void OpenMappedPara()
		{
			throw new NotImplementedException();
		}

		public void OpenMappedTaggedPara()
		{
			throw new NotImplementedException();
		}

		public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign)
		{
			throw new NotImplementedException();
		}

		public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] _rgOverrideProperties)
		{
			throw new NotImplementedException();
		}

		public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
		{
			throw new NotImplementedException();
		}

		public void CloseTable()
		{
			throw new NotImplementedException();
		}

		public void OpenTableRow()
		{
			throw new NotImplementedException();
		}

		public void CloseTableRow()
		{
			throw new NotImplementedException();
		}

		public void CloseTableBody()
		{
			throw new NotImplementedException();
		}

		public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
		{
			throw new NotImplementedException();
		}

		public void CloseTableHeaderCell()
		{
			throw new NotImplementedException();
		}

		public void MakeColumns(int nColSpan, VwLength vlWidth)
		{
			throw new NotImplementedException();
		}

		public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
		{
			throw new NotImplementedException();
		}

		public void OpenTableHeader()
		{
			throw new NotImplementedException();
		}

		public void CloseTableHeader()
		{
			throw new NotImplementedException();
		}

		public void OpenTableFooter()
		{
			throw new NotImplementedException();
		}

		public void CloseTableFooter()
		{
			throw new NotImplementedException();
		}

		public void OpenTableBody()
		{
			throw new NotImplementedException();
		}

		public void set_StringProperty(int sp, string bstrValue)
		{
			throw new NotImplementedException();
		}

		public void get_StringWidth(ITsString _tss, ITsTextProps _ttp, out int dmpx, out int dmpy)
		{
			throw new NotImplementedException();
		}

		public void AddPictureWithCaption(IPicture _pict, int tag, ITsTextProps _ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor _vwvc)
		{
			throw new NotImplementedException();
		}

		public void AddPicture(IPicture _pict, int tag, int dxmpWidth, int dympHeight)
		{
			throw new NotImplementedException();
		}

		public void SetParagraphMark(VwBoundaryMark boundaryMark)
		{
			throw new NotImplementedException();
		}

		public void EmptyParagraphBehavior(int behavior)
		{
			throw new NotImplementedException();
		}

		public int OpenObject
		{
			get { throw new NotImplementedException(); }
		}

		public int EmbeddingLevel
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

	}

	enum DecoratorMethodTypes
	{
		UnknownMethod,
		AddObj,
		AddObjProp,
		AddObjVec,
		AddObjVecItems,
		AddString,
		AddStringProp,
		CloseInnerPile,
		CloseParagraph,
		CloseSpan,
		CloseTableCell,
		NoteDependency,
		OpenInnerPile,
		OpenParagraph,
		OpenSpan,
		OpenTableCell,
		PropsSetter,
		SetIntProperty
	}

	class StoredMethod
	{
		public StoredMethod(DecoratorMethodTypes mtype, object[] paramArray)
		{
			MethodType = mtype;
			ParamArray = paramArray;
		}
		public DecoratorMethodTypes MethodType { get; private set; }
		public object[] ParamArray { get; private set; }
		public int ParamCount { get { return ParamArray.Length; } }
	}

}
