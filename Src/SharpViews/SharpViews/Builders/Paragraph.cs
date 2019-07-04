﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists to implement the fluent language, in expressions like Paragraph.Containing(Display.Of(...)).
	/// </summary>
	public abstract class Paragraph : Flow
	{
		static public Flow Containing(Flow flow)
		{
			return MakeFlowBeParagraph(new AtomicFlow(flow));
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