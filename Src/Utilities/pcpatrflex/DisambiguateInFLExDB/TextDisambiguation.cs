// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public class TextDisambiguation
	{
		public IText Text { get; set; }
		public string[] GuidBundles { get; set; }
		public String AndFile { get; set; }
#if ShowOutput
		string tempFileName = "";
#endif

		public TextDisambiguation(IText text, string[] guidBundles, string andFile)
		{
			Text = text;
			GuidBundles = guidBundles;
			AndFile = andFile;
#if ShowOutput
			tempFileName = Path.Combine(Path.GetTempPath(), "PcPatrFLExTextDisamDebug.txt");
			if (File.Exists(tempFileName))
				File.Delete(tempFileName);
#endif
		}

		public void Disambiguate(LcmCache cache)
		{
			var istText = Text.ContentsOA as IStText;
			var andGuids = AndFileLoader.GetGuidsFromAndFile(AndFile);
			int guidIndex = 0;
#if ShowOutput
			using (StreamWriter file = new StreamWriter(tempFileName, true))
			{
				file.WriteLine("Paragraph count =" + istText.ParagraphsOS.Count);
			}
#endif
			for (int i = 0; i < istText.ParagraphsOS.Count; i++)
			{
				var para = istText.ParagraphsOS.ElementAtOrDefault(i) as IStTxtPara;
				//Console.WriteLine("text='" + para.Contents.Text + "'");
				//Console.WriteLine("i=" + i + "; guidIndex=" + guidIndex);
#if ShowOutput
				using (StreamWriter file = new StreamWriter(tempFileName, true))
				{
					file.WriteLine("text='" + para.Contents.Text + "'");
					file.WriteLine("i=" + i + "; guidIndex=" + guidIndex);
				}
#endif
				foreach (ISegment segment in para.SegmentsOS)
				{
					if (segment == null)
					{
#if ShowOutput
						using (StreamWriter file = new StreamWriter(tempFileName, true))
						{
							file.WriteLine("\tSegment is null");
						}
#endif
						continue;
					}
					if (guidIndex < GuidBundles.Length)
					{
						if (Disambguated(cache, segment, GuidBundles.ElementAtOrDefault(guidIndex)))
						{
							//Console.WriteLine("did guid bundles for " + guidIndex);
#if ShowOutput
							using (StreamWriter file = new StreamWriter(tempFileName, true))
							{
								file.WriteLine("\tdid guid bundles for " + guidIndex);
							}
#endif
							guidIndex++;
							continue;
						}
					}
					if (guidIndex < andGuids.Length)
					{
						Disambguated(cache, segment, andGuids.ElementAtOrDefault(guidIndex));
						//Console.WriteLine("did and guids for " + guidIndex);
#if ShowOutput
						using (StreamWriter file = new StreamWriter(tempFileName, true))
						{
							file.WriteLine("\tdid and guids for " + guidIndex);
						}
#endif
						guidIndex++;
					}
				}
			}
		}

		private bool Disambguated(LcmCache cache, ISegment segment, string chosen)
		{
			if (!String.IsNullOrEmpty(chosen))
			{
				var guids = GuidConverter.CreateListFromString(chosen);
				var segmentDisam = new SegmentDisambiguation(segment, guids);
				segmentDisam.Disambiguate(cache);
				return true;
			}
			return false;
		}
	}
}
