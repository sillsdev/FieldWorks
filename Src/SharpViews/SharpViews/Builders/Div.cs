using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists to implement the fluent language, in expressions like Div.Containing(Display.Of(...)).
	/// </summary>
	public abstract class Div : Flow
	{
		static public Flow Containing(Flow flow)
		{
			return MakeFlowBeDiv(flow);
		}

		private static Flow MakeFlowBeDiv(Flow flow)
		{
			flow.BoxMaker = childStyle => new DivBox(childStyle);
			return flow;
		}

		/// <summary>
		/// This overload allows several Flows to be concatenated into a single div.
		/// </summary>
		static public Flow Containing(params Flow[] flows)
		{
			return MakeFlowBeDiv(new SequenceFlow(flows));
		}
	}
}
