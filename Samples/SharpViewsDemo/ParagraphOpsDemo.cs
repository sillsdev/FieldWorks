// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SharpViewsDemo
{
	class ParagraphOpsDemo : ParagraphOperations<ParagraphDemo>
	{
		private ParagraphOwnerDemo m_owner;
		public ParagraphOpsDemo(ParagraphOwnerDemo owner)
		{
			m_owner = owner;
			List = owner.Paragraphs;
		}

		// We override the default insert function because we need to call a method that
		// raises the event as well as inserting the item.
		public override ParagraphDemo MakeListItem(int index)
		{
			var paragraphDemo = new ParagraphDemo();
			m_owner.InsertParagraph(index, paragraphDemo);
			return paragraphDemo;
		}
	}
}
