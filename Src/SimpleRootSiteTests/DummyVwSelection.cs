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
	/// <summary />
	internal sealed class DummyVwSelection : IVwSelection
	{
		public int Anchor;
		public int End;
		private readonly DummyRootBox m_rootBox;

		public DummyVwSelection(DummyRootBox rootbox, int anchor, int end)
		{
			m_rootBox = rootbox;
			Anchor = anchor;
			End = end;
		}

		public string SelectionText
		{
			get
			{
				if (Anchor >= m_rootBox.Text.Length)
				{
					return string.Empty;
				}
				var begin = Math.Min(Anchor, End);
				var end = Math.Max(Anchor, End);
				return m_rootBox.Text.Substring(begin, end - begin);
			}
		}

		#region IVwSelection implementation
		public void GetSelectionProps(int cttpMax, ArrayPtr _rgpttp, ArrayPtr _rgpvps, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetHardAndSoftCharProps(int cttpMax, ArrayPtr _rgpttpSel, ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetParaProps(int cttpMax, ArrayPtr _rgpvps, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetHardAndSoftParaProps(int cttpMax, ITsTextProps[] _rgpttpPara, ArrayPtr _rgpttpHard, ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			_cttp = 0;
		}

		public void SetSelectionProps(int cttp, ITsTextProps[] _rgpttp)
		{
		}

		public void TextSelInfo(bool fEndPoint, out ITsString _ptss, out int _ich, out bool _fAssocPrev, out int _hvoObj, out int _tag, out int _ws)
		{
			_ptss = null;
			_ich = 0;
			_fAssocPrev = false;
			_hvoObj = 0;
			_tag = 0;
			_ws = 0;
		}

		public int CLevels(bool fEndPoint)
		{
			return 0;
		}

		public void PropInfo(bool fEndPoint, int ilev, out int _hvoObj, out int _tag, out int _ihvo, out int _cpropPrevious, out IVwPropertyStore _pvps)
		{
			_hvoObj = 0;
			_tag = 0;
			_ihvo = 0;
			_cpropPrevious = 0;
			_pvps = null;
		}

		public void AllTextSelInfo(out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli, out int _tagTextProp, out int _cpropPrevious, out int _ichAnchor,
			out int _ichEnd, out int _ws, out bool _fAssocPrev, out int _ihvoEnd, out ITsTextProps _pttp)
		{
			_ihvoRoot = 0;
			_tagTextProp = 0;
			_cpropPrevious = 0;
			_ichAnchor = 0;
			_ichEnd = 0;
			_ws = 0;
			_fAssocPrev = false;
			_ihvoEnd = 0;
			_pttp = null;
		}

		public void AllSelEndInfo(bool fEndPoint, out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli, out int _tagTextProp, out int _cpropPrevious,
			out int _ich, out int _ws, out bool _fAssocPrev, out ITsTextProps _pttp)
		{
			_ihvoRoot = 0;
			_tagTextProp = 0;
			_cpropPrevious = 0;
			_ich = fEndPoint ? End : Anchor;
			_ws = 0;
			_fAssocPrev = false;
			_pttp = null;
		}

		public bool CompleteEdits(out VwChangeInfo _ci)
		{
			_ci = default(VwChangeInfo);
			return true;
		}

		public void ExtendToStringBoundaries()
		{
		}

		public void Location(IVwGraphics _vg, Rect rcSrc, Rect rcDst, out Rect _rdPrimary, out Rect _rdSecondary, out bool _fSplit, out bool _fEndBeforeAnchor)
		{
			_rdPrimary = default(Rect);
			_rdSecondary = default(Rect);
			_fSplit = false;
			_fEndBeforeAnchor = false;
		}

		public void GetParaLocation(out Rect _rdLoc)
		{
			_rdLoc = default(Rect);
		}

		public void ReplaceWithTsString(ITsString _tss)
		{
			var selectionText = (_tss != null ? _tss.Text : string.Empty) ?? string.Empty;
			var begin = Math.Min(Anchor, End);
			var end = Math.Max(Anchor, End);
			if (begin < m_rootBox.Text.Length)
			{
				m_rootBox.Text = m_rootBox.Text.Remove(begin, end - begin);
			}
			if (begin < m_rootBox.Text.Length)
			{
				m_rootBox.Text = m_rootBox.Text.Insert(begin, selectionText);
			}
			else
			{
				m_rootBox.Text += selectionText;
			}
			Anchor = End = begin + selectionText.Length;
		}

		public void GetSelectionString(out ITsString _ptss, string bstrSep)
		{
			_ptss = TsStringUtils.MakeString(SelectionText, m_rootBox.m_dummySimpleRootSite.WritingSystemFactory.UserWs);
		}

		public void GetFirstParaString(out ITsString _ptss, string bstrSep, out bool _fGotItAll)
		{
			throw new NotSupportedException();
		}

		public void SetIPLocation(bool fTopLine, int xdPos)
		{
			throw new NotSupportedException();
		}

		public void Install()
		{
			throw new NotSupportedException();
		}

		public bool get_Follows(IVwSelection _sel)
		{
			throw new NotSupportedException();
		}

		public int get_ParagraphOffset(bool fEndPoint)
		{
			throw new NotSupportedException();
		}

		public IVwSelection GrowToWord()
		{
			throw new NotSupportedException();
		}

		public IVwSelection EndPoint(bool fEndPoint)
		{
			throw new NotSupportedException();
		}

		public void SetTypingProps(ITsTextProps _ttp)
		{
			throw new NotSupportedException();
		}

		public int get_BoxDepth(bool fEndPoint)
		{
			throw new NotSupportedException();
		}

		public int get_BoxIndex(bool fEndPoint, int iLevel)
		{
			throw new NotSupportedException();
		}

		public int get_BoxCount(bool fEndPoint, int iLevel)
		{
			throw new NotSupportedException();
		}

		public VwBoxType get_BoxType(bool fEndPoint, int iLevel)
		{
			throw new NotSupportedException();
		}

		public bool IsRange => End != Anchor;

		public bool EndBeforeAnchor => End < Anchor;

		public bool CanFormatPara
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool CanFormatChar
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool CanFormatOverlay
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool IsValid => true;

		public bool AssocPrev
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

		public VwSelType SelType => VwSelType.kstText;

		public IVwRootBox RootBox => m_rootBox;

		public bool IsEditable
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool IsEnabled
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		#endregion
	}
}