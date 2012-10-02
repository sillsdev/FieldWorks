using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists to implement the fluent language, in expressions like Paragraph.Containing(Display.Of(...)).
	/// </summary>
	public abstract class Paragraph : Flow
	{
		static public Flow Containing(Flow flow)
		{
			return MakeFlowBeParagraph(flow);
		}

		private static Flow MakeFlowBeParagraph(Flow flow)
		{
			flow.BoxMaker = childStyle => new ParaBox(childStyle);
			return flow;
		}

		/// <summary>
		/// This overload allows several Flows to be concatenated into a single paragraph.
		/// </summary>
		static public Flow Containing(params Flow[] flows)
		{
			return MakeFlowBeParagraph(new SequenceFlow(flows));
		}
	}

}
