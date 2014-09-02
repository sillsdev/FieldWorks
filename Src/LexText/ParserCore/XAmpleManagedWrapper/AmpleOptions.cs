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
