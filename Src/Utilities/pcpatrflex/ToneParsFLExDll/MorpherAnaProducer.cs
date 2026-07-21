// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.DisambiguateInFLExDB;
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

	}
}
