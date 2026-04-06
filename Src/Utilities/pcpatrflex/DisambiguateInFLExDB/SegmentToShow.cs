using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public class SegmentToShow
	{
		public ISegment Segment { get; set; }
		public string Baseline { get; set; }

		public SegmentToShow(ISegment segment, string baseline)
		{
			Segment = segment;
			Baseline = baseline;
		}
	}
}
