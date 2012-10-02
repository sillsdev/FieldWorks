using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists to implement the fluent language, in expressions like Row.Containing(Display.Of(...)).
	/// </summary>
	public abstract class Row : Flow
	{
		 internal static Flow MakeFlowBeRow(Flow flow, IControlColumnWidths columnWidths, bool WrapRow)
		{
			flow.BoxMaker = childStyle => new RowBox(childStyle, columnWidths, WrapRow);
			return flow;
		}

		public static RowMaker WithWidths(IControlColumnWidths columnWidths)
		{
			return new RowMaker(columnWidths);
		}

		//public Flow Containing(Flow flow)
		//{
		//    return MakeFlowBeRow(flow);
		//}

		///// <summary>
		///// This overload allows several Flows to be concatenated into a single row.
		///// </summary>
		//static public Flow Containing(params Flow[] flows)
		//{
		//    return MakeFlowBeRow(new SequenceFlow(flows));
		//}
	}

	public class RowMaker
	{
		private IControlColumnWidths m_columnWidths;
		public RowMaker(IControlColumnWidths columnWidths)
		{
			m_columnWidths = columnWidths;
		}
		public Flow Containing(Flow flow)
		{
			return Row.MakeFlowBeRow(flow, m_columnWidths, WrapRow);
		}

		public Flow Containing(params Flow[] flows)
		{
			return Row.MakeFlowBeRow(new SequenceFlow(flows), m_columnWidths, WrapRow);
		}

		private bool WrapRow;

		public RowMaker WithWrap
		{
			get { WrapRow = true; return this; }
		}
	}
}
