// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This decorator provides the chart body (in particular the MakeCellsMethod method object)
	/// the ability to store up a right to left generation of a chart row and spit it out to the
	/// view constructor in the left to right fashion that it needs. If the chart is left to right
	/// (the more common case), the decorator just feeds through to the base class.
	/// </summary>
	internal class ChartRowEnvDecorator : IVwEnv
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
			{
				return;
			}
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
				switch (m_calledMethods[index].MethodType)
				{
					case DecoratorMethodTypes.OpenTableCell:
						index = FindMatchingEndEmbeddingIndex(index);
						PutOutTableCellStartingAt(index);
						continue;
					case DecoratorMethodTypes.AddObjProp:
						RegurgitateIVwEnvCall(m_calledMethods[index]);
						break;
				}
			}
			m_fInRegurgitation = false;
		}

		private int FindMatchingEndEmbeddingIndex(int index)
		{
			// Back up the index to include SetIntProp or NoteDependency calls before the OpenTableCell
			// call, but don't go back as far as a CloseTableCell call or AddObjProp.
			for (var i = index - 1; i > -1; i--)
			{
				if (m_calledMethods[i].MethodType == DecoratorMethodTypes.SetIntProperty || m_calledMethods[i].MethodType == DecoratorMethodTypes.NoteDependency)
				{
					continue;
				}
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
				{
					fsentCloseCell = true;
				}
				RegurgitateIVwEnvCall(m_calledMethods[i]);
			}
		}

		private void RegurgitateIVwEnvCall(StoredMethod storedMethod)
		{
			switch (storedMethod.MethodType)
			{
				case DecoratorMethodTypes.AddObj:
					m_vwEnv.AddObj((int)storedMethod.ParamArray[0], (IVwViewConstructor)storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.AddObjProp:
					m_vwEnv.AddObjProp((int)storedMethod.ParamArray[0], (IVwViewConstructor)storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.AddObjVec:
					m_vwEnv.AddObjVec((int)storedMethod.ParamArray[0], (IVwViewConstructor)storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.AddObjVecItems:
					m_vwEnv.AddObjVecItems((int)storedMethod.ParamArray[0], (IVwViewConstructor)storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.AddString:
					m_vwEnv.AddString((ITsString)storedMethod.ParamArray[0]);
					break;
				case DecoratorMethodTypes.AddStringProp:
					m_vwEnv.AddStringProp((int)storedMethod.ParamArray[0], (IVwViewConstructor)storedMethod.ParamArray[1]);
					break;
				case DecoratorMethodTypes.CloseParagraph:
					m_vwEnv.CloseParagraph();
					break;
				case DecoratorMethodTypes.CloseTableCell:
					m_vwEnv.CloseTableCell();
					break;
				case DecoratorMethodTypes.NoteDependency:
					m_vwEnv.NoteDependency((int[])storedMethod.ParamArray[0], (int[])storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.OpenParagraph:
					m_vwEnv.OpenParagraph();
					break;
				case DecoratorMethodTypes.OpenTableCell:
					m_vwEnv.OpenTableCell((int)storedMethod.ParamArray[0], (int)storedMethod.ParamArray[1]);
					break;
				case DecoratorMethodTypes.SetIntProperty:
					m_vwEnv.set_IntProperty((int)storedMethod.ParamArray[0], (int)storedMethod.ParamArray[1], (int)storedMethod.ParamArray[2]);
					break;
				case DecoratorMethodTypes.PropsSetter:
					m_vwEnv.Props = (ITsTextProps)storedMethod.ParamArray[0];
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

		public virtual void AddObjVec(int tag, IVwViewConstructor vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObjVec(tag, vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObjVec, new object[] { tag, vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddObjVecItems(int tag, IVwViewConstructor vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObjVecItems(tag, vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObjVecItems, new object[] { tag, vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddObj(int hvo, IVwViewConstructor vwvc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddObj(hvo, vwvc, frag);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddObj, new object[] { hvo, vwvc, frag }));
			m_numOfCalls++;
		}

		public virtual void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddProp(tag, vc, frag);
				return;
			}
			throw new NotSupportedException();
		}

		public virtual void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
		{
			if (!IsRtL)
			{
				m_vwEnv.NoteDependency(rghvo, rgtag, chvo);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.NoteDependency, new object[] { rghvo, rgtag, chvo }));
			m_numOfCalls++;
		}

		public virtual void AddStringProp(int tag, IVwViewConstructor vwvc)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddStringProp(tag, vwvc);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddStringProp, new object[] { tag, vwvc }));
			m_numOfCalls++;
		}

		public virtual void AddString(ITsString tss)
		{
			if (!IsRtL)
			{
				m_vwEnv.AddString(tss);
				return;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.AddString, new object[] { tss }));
			m_numOfCalls++;
		}

		/// <summary>
		/// Current flow object is a paragraph. (But being in a span it will still be true.)
		/// </summary>
		public bool IsParagraphOpen()
		{
			throw new NotSupportedException();
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
									new object[]
									{
										(int) FwTextPropType.ktptRightToLeft,
										(int) FwTextPropVar.ktpvEnum,
										(int) FwTextToggleVal.kttvForceOn
									}));
				m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.SetIntProperty,
									new object[]
									{
										(int) FwTextPropType.ktptAlign,
										(int) FwTextPropVar.ktpvEnum,
										(int) FwTextAlign.ktalRight
									}));
			}
			else // Only count OpenParagraph if regurgitating, since other calls will be counted separately.
			{
				m_numOfCalls++;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenParagraph, new object[]{}));
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
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenInnerPile, new object[] { }));
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
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenSpan, new object[] { }));
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
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.OpenTableCell, new object[] { nRowSpan, nColSpan }));
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
			if (tpt == (int)FwTextPropType.ktptBorderTrailing)
			{
				tpt = (int)FwTextPropType.ktptBorderLeading;
			}
			if (tpt == (int)FwTextPropType.ktptMarginTrailing)
			{
				tpt = (int)FwTextPropType.ktptMarginLeading;
			}
			m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.SetIntProperty, new object[] { tpt, tpv, nValue }));
			m_numOfCalls++;
		}

		public ISilDataAccess DataAccess => m_vwEnv.DataAccess;

		public ITsTextProps Props
		{
			set
			{
				if (!IsRtL)
				{
					m_vwEnv.Props = value;
					return;
				}
				m_calledMethods.Add(new StoredMethod(DecoratorMethodTypes.PropsSetter, new object[] { value }));
				m_numOfCalls++;
			}
		}

		#endregion

		#region Unimplemented Methods

		public void AddReversedObjVecItems(int tag, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public void AddLazyVecItems(int tag, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public void AddLazyItems(int[] rghvo, int chvo, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public void AddDerivedProp(int[] rgtag, int ctag, IVwViewConstructor vwvc, int frag)
		{
			throw new NotSupportedException();
		}

		public void NoteStringValDependency(int hvo, int tag, int ws, ITsString tssVal)
		{
			throw new NotSupportedException();
		}

		public void AddUnicodeProp(int tag, int ws, IVwViewConstructor vwvc)
		{
			throw new NotSupportedException();
		}

		public void AddIntProp(int tag)
		{
			throw new NotSupportedException();
		}

		public void AddIntPropPic(int tag, IVwViewConstructor vc, int frag, int nMin, int nMax)
		{
			throw new NotSupportedException();
		}

		public void AddStringAltMember(int tag, int ws, IVwViewConstructor vwvc)
		{
			throw new NotSupportedException();
		}

		public void AddStringAlt(int tag)
		{
			throw new NotSupportedException();
		}

		public void AddStringAltSeq(int tag, int[] rgenc, int cws)
		{
			throw new NotSupportedException();
		}

		public void AddTimeProp(int tag, uint flags)
		{
			throw new NotSupportedException();
		}

		public int CurrentObject()
		{
			throw new NotSupportedException();
		}

		public void GetOuterObject(int ichvoLevel, out int hvo, out int tag, out int ihvo)
		{
			throw new NotSupportedException();
		}

		public void AddWindow(IVwEmbeddedWindow ew, int dmpAscent, bool fJustifyRight, bool fAutoShow)
		{
			throw new NotSupportedException();
		}

		public void AddSeparatorBar()
		{
			throw new NotSupportedException();
		}

		public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		{
			throw new NotSupportedException();
		}

		public void OpenDiv()
		{
			throw new NotSupportedException();
		}

		public void CloseDiv()
		{
			throw new NotSupportedException();
		}

		public void OpenTaggedPara()
		{
			throw new NotSupportedException();
		}

		public void OpenMappedPara()
		{
			throw new NotSupportedException();
		}

		public void OpenMappedTaggedPara()
		{
			throw new NotSupportedException();
		}

		public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign)
		{
			throw new NotSupportedException();
		}

		public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] rgOverrideProperties)
		{
			throw new NotSupportedException();
		}

		public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
		{
			throw new NotSupportedException();
		}

		public void CloseTable()
		{
			throw new NotSupportedException();
		}

		public void OpenTableRow()
		{
			throw new NotSupportedException();
		}

		public void CloseTableRow()
		{
			throw new NotSupportedException();
		}

		public void CloseTableBody()
		{
			throw new NotSupportedException();
		}

		public void OpenTableHeaderCell(int nRowSpan, int nColSpan)
		{
			throw new NotSupportedException();
		}

		public void CloseTableHeaderCell()
		{
			throw new NotSupportedException();
		}

		public void MakeColumns(int nColSpan, VwLength vlWidth)
		{
			throw new NotSupportedException();
		}

		public void MakeColumnGroup(int nColSpan, VwLength vlWidth)
		{
			throw new NotSupportedException();
		}

		public void OpenTableHeader()
		{
			throw new NotSupportedException();
		}

		public void CloseTableHeader()
		{
			throw new NotSupportedException();
		}

		public void OpenTableFooter()
		{
			throw new NotSupportedException();
		}

		public void CloseTableFooter()
		{
			throw new NotSupportedException();
		}

		public void OpenTableBody()
		{
			throw new NotSupportedException();
		}

		public void set_StringProperty(int sp, string bstrValue)
		{
			throw new NotSupportedException();
		}

		public void get_StringWidth(ITsString tss, ITsTextProps ttp, out int dmpx, out int dmpy)
		{
			throw new NotSupportedException();
		}

		public void AddPictureWithCaption(IPicture pict, int tag, ITsTextProps ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor vwvc)
		{
			throw new NotSupportedException();
		}

		public void AddPicture(IPicture pict, int tag, int dxmpWidth, int dympHeight)
		{
			throw new NotSupportedException();
		}

		public void SetParagraphMark(VwBoundaryMark boundaryMark)
		{
			throw new NotSupportedException();
		}

		public void EmptyParagraphBehavior(int behavior)
		{
			throw new NotSupportedException();
		}

		public int OpenObject
		{
			get { throw new NotSupportedException(); }
		}

		public int EmbeddingLevel
		{
			get { throw new NotSupportedException(); }
		}

		#endregion
	}
}