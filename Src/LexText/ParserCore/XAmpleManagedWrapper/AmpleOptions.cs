// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace XAmpleManagedWrapper
{
	public class AmpleOptions
	{
		public AmpleOptions ()
		{
			MaxMorphnameLength = 40;
			MaxAnalysesToReturn = 20;
			OutputRootGlosses = false;
			PrintTestParseTrees = false;
			ReportAmbiguityPercentages = true;
			WriteDecompField = true;
			WritePField = true;
			WriteWordField = true;
			Trace = false;
			OutputStyle = "FWParse";
		}

		public string OutputStyle { get; set; }
		public int MaxMorphnameLength { get; set; }
		public int MaxAnalysesToReturn { get; set; }
		public bool OutputRootGlosses;
		public bool ReportAmbiguityPercentages;
		public bool CheckMorphnames;
		public bool PrintTestParseTrees;
		public bool WriteDecompField;
		public bool WritePField;
		public bool WriteWordField;
		public bool Trace;
	}
}
