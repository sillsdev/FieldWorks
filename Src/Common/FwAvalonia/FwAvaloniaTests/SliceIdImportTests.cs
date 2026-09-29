// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// A slice's authored <c>id=</c> is the name a tool's filter list uses to withhold the row.
	/// It has to survive import, or the composer has nothing to match a filter against
	/// (LT-22802).
	/// </summary>
	[TestFixture]
	public class SliceIdImportTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='CmPossibility-Detail-Status'>
    <slice id='CmPossibilityStatus' label='Status' editor='string' field='Status'/>
  </part>
  <part id='CmPossibility-Detail-Name'>
    <slice label='Name' editor='string' field='Name'/>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string layoutXml)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
		}

		private static IEnumerable<ViewNode> Flatten(ViewNode n)
		{
			yield return n;
			foreach (var c in n.Children)
				foreach (var d in Flatten(c))
					yield return d;
		}

		private static ViewDefinitionModel BothRows() => Import(@"
<layout class='CmPossibility' type='detail' name='default'>
  <part ref='Status'/>
  <part ref='Name'/>
</layout>");

		[Test]
		public void Slice_WithAnId_CarriesItOntoTheNode()
		{
			var nodes = BothRows().Roots.SelectMany(Flatten).ToList();

			Assert.That(nodes.Select(n => n.SliceId), Does.Contain("CmPossibilityStatus"),
				"the filter list names rows by this id; dropping it at import leaves the "
				+ "composer unable to apply the tool's filter at all");
		}

		[Test]
		public void Slice_WithoutAnId_LeavesItNull()
		{
			var name = BothRows().Roots.SelectMany(Flatten)
				.Single(n => n.Field == "Name");

			Assert.That(name.SliceId, Is.Null,
				"most slices author no id, and a synthesized one could collide with a real "
				+ "entry in some tool's filter list");
		}

		[Test]
		public void SliceId_IsNotReportedAsAnUnhandledAttribute()
		{
			var unhandled = BothRows().Diagnostics
				.Where(d => d.Code == "unhandled-attribute" && d.Message.Contains("id"))
				.ToList();

			Assert.That(unhandled, Is.Empty,
				"id is consumed now, so it must leave the unhandled-attribute report -- that "
				+ "report is the list of things the Avalonia view still ignores");
		}
	}
}
