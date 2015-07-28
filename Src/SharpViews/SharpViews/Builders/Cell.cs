// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists to implement the fluent language, in expressions like Cell.Containing(Display.Of(...)).
	/// </summary>
	public abstract class Cell : Flow
	{
		static public Flow Containing(Flow flow)
		{
			return MakeFlowBeCell(new AtomicFlow(flow));
		}

		private static Flow MakeFlowBeCell(Flow flow)
		{
			flow.BoxMaker = childStyle => new CellBox(childStyle);
			return flow;
		}

		/// <summary>
		/// This overload allows several Flows to be concatenated into a single cell.
		/// </summary>
		static public Flow Containing(params Flow[] flows)
		{
			return MakeFlowBeCell(new SequenceFlow(flows));
		}
	}
}
