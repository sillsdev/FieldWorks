// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public abstract class MorpherAnaProducer
	{
		protected bool UseUniqueWordForms { get; set; }
		public string AnaFilePath { get; set; }
		protected LcmCache Cache { get; set; }
		const string kAnaFileName = "ToneParsInvoker.ana";
		public string IntxCtlFile { get; set; }

		public MorpherAnaProducer()
		{
			UseUniqueWordForms = false;
			Cache = null;
			AnaFilePath = Path.Combine(Path.GetTempPath(), kAnaFileName);
		}

		public MorpherAnaProducer(bool useUniqueWordForms, LcmCache cache, string intxCtlFile)
		{
			UseUniqueWordForms = useUniqueWordForms;
			Cache = cache;
			IntxCtlFile = intxCtlFile;
			AnaFilePath = Path.Combine(Path.GetTempPath(), kAnaFileName);
		}

		public abstract void ProduceANA(SegmentToShow segmentToShow);
		public abstract void ProduceANA(IText selectedTextToShow);

		// following borrowed from SIL.FieldWorks.WordWorks.Parser (ParserCore.dll)
		/// <summary>
		/// Convert any characters in the name which are higher than 0x00FF to hex.
		/// Neither XAmple nor PC-PATR can read a file name containing letters above 0x00FF.
		/// </summary>
		/// <param name="originalName">The original name to be converted</param>
		/// <returns>Converted name</returns>
		internal static string ConvertNameToUseAnsiCharacters(string originalName)
		{
			var sb = new StringBuilder();
			char[] letters = originalName.ToCharArray();
			foreach (var letter in letters)
			{
				int value = Convert.ToInt32(letter);
				if (value > 255)
				{
					string hex = value.ToString("X4");
					sb.Append(hex);
				}
				else
				{
					sb.Append(letter);
				}
			}
			return sb.ToString();
		}
	}
}
