// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal class CaptionParaFragment : IParaFragment
	{
		private int m_beginOffset;
		private int m_endOffset;

		//For this case the begin and end offsets are relative to the caption.
		public int GetMyBeginOffsetInPara()
		{
			return m_beginOffset;
		}

		public int GetMyEndOffsetInPara()
		{
			return m_endOffset;
		}

		public void SetMyBeginOffsetInPara(int begin)
		{
			m_beginOffset = begin;
		}

		public void SetMyEndOffsetInPara(int end)
		{
			m_endOffset = end;
		}

		public ISegment Segment => null;

		public int ContainingParaOffset { get; set; }

		public ICmPicture Picture { get; set; }

		public ITsString Reference => Paragraph.Reference(Paragraph.SegmentsOS.Last(seg => seg.BeginOffset <= ContainingParaOffset), ContainingParaOffset);

		public IStTxtPara Paragraph { get; set; }

		public ICmObject TextObject => Picture;

		public int TextFlid => CmPictureTags.kflidCaption;

		public bool IsValid => true;

		public IAnalysis Analysis => null;

		public AnalysisOccurrence BestOccurrence => null;
	}
}