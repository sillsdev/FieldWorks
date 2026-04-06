// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIL.DisambiguateInFLExDB;
//using SIL.HermitCrabWithTonePars;
using SIL.LCModel;

namespace SIL.ToneParsFLEx
{
	public class HCMorpherAnaProducer : MorpherAnaProducer
	{
		FLExDBExtractor extractor;
		OrthoChanger changer;
		ANABuilder anaBuilder;
		StringBuilder sb = new StringBuilder();

		public HCMorpherAnaProducer(bool useUniqueWordForms, LcmCache cache, string intxCtlFile)
		{
			UseUniqueWordForms = useUniqueWordForms;
			Cache = cache;
			IntxCtlFile = intxCtlFile;
			extractor = new FLExDBExtractor(Cache);
			changer = new OrthoChanger();
			changer.LoadOrthoChangesFile(IntxCtlFile);
			changer.CreateOrthoChanges();
			anaBuilder = new ANABuilder(Cache, extractor, changer);
		}

		public override void ProduceANA(SegmentToShow segmentToShow)
		{
			sb.Clear();
			sb.Append(anaBuilder.ExtractTextSegmentAndParseWordAsANA(segmentToShow.Segment));
			File.WriteAllText(AnaFilePath, sb.ToString());
		}

		public override void ProduceANA(IText selectedTextToShow)
		{
			sb.Clear();
			var stText = selectedTextToShow.ContentsOA;
			foreach (IStTxtPara stPara in stText.ParagraphsOS)
			{
				foreach (ISegment segment in stPara.SegmentsOS)
				{
					string ana = anaBuilder.ExtractTextSegmentAndParseWordAsANA(segment);
					sb.Append(ana);
				}
			}
			File.WriteAllText(AnaFilePath, sb.ToString());
		}
	}
}
