// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public sealed class ToneParsInvokerOptions
	{
		private static readonly ToneParsInvokerOptions instance = new ToneParsInvokerOptions();
		public bool DoTracing { get; set; }
		public bool RuleTrace { get; set; }
		public bool TierAssignmentTrace { get; set; }
		public bool DomainAssignmentTrace { get; set; }
		public bool MorphemeToneAssignmentTrace { get; set; }
		public bool TBUAssignmentTrace { get; set; }
		public bool SyllableParsingTrace { get; set; }
		public bool MoraParsingTrace { get; set; }
		public bool MorphemeLinkingTrace { get; set; }
		public bool SegmentParsingTrace { get; set; }
		public bool VerifyInformation { get; set; }

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static ToneParsInvokerOptions() { }

		private ToneParsInvokerOptions()
		{
			DoTracing = false;
			RuleTrace = false;
			TierAssignmentTrace = false;
			DomainAssignmentTrace = false;
			MorphemeToneAssignmentTrace = false;
			TBUAssignmentTrace = false;
			SyllableParsingTrace = false;
			MoraParsingTrace = false;
			MorphemeLinkingTrace = false;
			SegmentParsingTrace = false;
			VerifyInformation = false;
		}

		// following needed for tests
		public void ResetAllOptions()
		{
			Instance.DomainAssignmentTrace = false;
			Instance.DoTracing = false;
			Instance.MoraParsingTrace = false;
			Instance.MorphemeLinkingTrace = false;
			Instance.MorphemeToneAssignmentTrace = false;
			Instance.RuleTrace = false;
			Instance.SegmentParsingTrace = false;
			Instance.SyllableParsingTrace = false;
			Instance.TBUAssignmentTrace = false;
			Instance.TierAssignmentTrace = false;
			Instance.VerifyInformation = false;
		}

		public static ToneParsInvokerOptions Instance
		{
			get { return instance; }
		}

		public string GetOptionsString()
		{
			int code = 0;
			var sb = new StringBuilder();
			if (VerifyInformation)
			{
				sb.Append("-v ");
			}
			if (DoTracing)
			{
				sb.Append("-T -D ");
				RuleTrace = true;
				if (DomainAssignmentTrace)
					code += 1024;
				if (MoraParsingTrace)
					code += 2;
				if (MorphemeLinkingTrace)
					code += 256;
				if (MorphemeToneAssignmentTrace)
					code += 4096;
				if (RuleTrace)
					code += 512;
				if (SegmentParsingTrace)
					code += 1;
				if (SyllableParsingTrace)
					code += 4;
				if (TBUAssignmentTrace)
					code += 128;
				if (TierAssignmentTrace)
					code += 2048;
				if (code != 0)
				{
					sb.Append(code);
					sb.Append(" ");
				}
			}
			//if (!DoTracing && (DomainAssignmentTrace || MoraParsingTrace || MorphemeLinkingTrace
			//	|| MorphemeToneAssignmentTrace || RuleTrace || SegmentParsingTrace
			//	|| SyllableParsingTrace || TBUAssignmentTrace || TierAssignmentTrace))
			//{
			//	sb.Append("-D ");
			//}
			return sb.ToString();
		}
	}
}
