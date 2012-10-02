using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace XMLViewsTests
{
	[TestFixture]
	public class TestXmlBrowseView
	{
		[Test]
		public void DoubleClickEmptyItem()
		{
			using (var bv = new XmlBrowseView())
			{
				var xdoc = new XmlDocument();
				// Largely copied from a live one. We probably don't really need all these for this test.
				xdoc.LoadXml(@"<parameters id='textsChooser' clerk='interlinearTexts' filterBar='true' treeBarAvailability='NotAllowed'
						defaultCursor='Arrow' altTitleId='Text-Plural' editable='false'>
						<columns>
							<column label='Title' width='144000'><string field='Title' ws='$ws=best vernoranal' /></column>
						</columns>
					</parameters>");
				using (var mediator = new Mediator())
				{
					bv.Init(mediator, xdoc.DocumentElement);
					bv.SimulateDoubleClick(new EventArgs());
				}
			}
		}
	}
}
