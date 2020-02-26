// Copyright (c) 2013-2020 SIL International
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
		public void GetSelectionProps(int cttpMax, ArrayPtr rgpttp, ArrayPtr rgpvps, out int cttp)
		{
			cttp = 0;
		}

		public void GetHardAndSoftCharProps(int cttpMax, ArrayPtr rgpttpSel, ArrayPtr rgpvpsSoft, out int cttp)
		{
			cttp = 0;
		}

		public void GetParaProps(int cttpMax, ArrayPtr rgpvps, out int cttp)
		{
			cttp = 0;
		}

		public void GetHardAndSoftParaProps(int cttpMax, ITsTextProps[] rgpttpPara, ArrayPtr rgpttpHard, ArrayPtr rgpvpsSoft, out int cttp)
		{
			cttp = 0;
		}

		public void SetSelectionProps(int cttp, ITsTextProps[] rgpttp)
		{
		}

		public void TextSelInfo(bool fEndPoint, out ITsString ptss, out int ich, out bool fAssocPrev, out int hvoObj, out int tag, out int ws)
		{
			ptss = null;
			ich = 0;
			fAssocPrev = false;
			hvoObj = 0;
			tag = 0;
			ws = 0;
		}

		public int CLevels(bool fEndPoint)
		{
			return 0;
		}

		public void PropInfo(bool fEndPoint, int ilev, out int hvoObj, out int tag, out int ihvo, out int cpropPrevious, out IVwPropertyStore pvps)
		{
			hvoObj = 0;
			tag = 0;
			ihvo = 0;
			cpropPrevious = 0;
			pvps = null;
		}

		public void AllTextSelInfo(out int ihvoRoot, int cvlsi, ArrayPtr rgvsli, out int tagTextProp, out int cpropPrevious, out int ichAnchor,
			out int ichEnd, out int ws, out bool fAssocPrev, out int ihvoEnd, out ITsTextProps pttp)
		{
			ihvoRoot = 0;
			tagTextProp = 0;
			cpropPrevious = 0;
			ichAnchor = 0;
			ichEnd = 0;
			ws = 0;
			fAssocPrev = false;
			ihvoEnd = 0;
			pttp = null;
		}

		public void AllSelEndInfo(bool fEndPoint, out int ihvoRoot, int cvlsi, ArrayPtr rgvsli, out int tagTextProp, out int cpropPrevious,
			out int ich, out int ws, out bool fAssocPrev, out ITsTextProps pttp)
		{
			ihvoRoot = 0;
			tagTextProp = 0;
			cpropPrevious = 0;
			ich = fEndPoint ? End : Anchor;
			ws = 0;
			fAssocPrev = false;
			pttp = null;
		}

		public bool CompleteEdits(out VwChangeInfo ci)
		{
			ci = default;
			return true;
		}

		public void ExtendToStringBoundaries()
		{
		}

		public void Location(IVwGraphics vg, Rect rcSrc, Rect rcDst, out Rect rdPrimary, out Rect rdSecondary, out bool fSplit, out bool fEndBeforeAnchor)
		{
			rdPrimary = default;
			rdSecondary = default;
			fSplit = false;
			fEndBeforeAnchor = false;
		}

		public void GetParaLocation(out Rect rdLoc)
		{
			rdLoc = default;
		}

		public void ReplaceWithTsString(ITsString tss)
		{
			var selectionText = (tss != null ? tss.Text : string.Empty) ?? string.Empty;
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

		public void GetSelectionString(out ITsString ptss, string bstrSep)
		{
			ptss = TsStringUtils.MakeString(SelectionText, m_rootBox.m_dummySimpleRootSite.WritingSystemFactory.UserWs);
		}

		public void GetFirstParaString(out ITsString ptss, string bstrSep, out bool fGotItAll)
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

		public bool get_Follows(IVwSelection sel)
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

		public void SetTypingProps(ITsTextProps ttp)
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

		public bool CanFormatPara => throw new NotSupportedException();

		public bool CanFormatChar => throw new NotSupportedException();

		public bool CanFormatOverlay => throw new NotSupportedException();

		public bool IsValid => true;

		public bool AssocPrev
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public VwSelType SelType => VwSelType.kstText;

		public IVwRootBox RootBox => m_rootBox;

		public bool IsEditable => throw new NotSupportedException();

		public bool IsEnabled => throw new NotSupportedException();
		#endregion
	}
}