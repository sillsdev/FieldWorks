// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §20.1.4 (F-7) — the importer must capture a boolean slice's legacy <c>toggleValue="true"</c> onto the
	/// ViewNode (the composer then inverts read display + write commit so a toggled checkbox round-trips with
	/// the same sense the WinForms slice shows). Mirrors the existing forVariant import coverage.
	/// </summary>
	[TestFixture]
	public class ToggleValueImportTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-Toggled'>
    <slice label='Final' editor='checkbox' field='IsAbstract' toggleValue='true'/>
  </part>
  <part id='LexEntry-Detail-Plain'>
    <slice label='Abstract' editor='checkbox' field='IsAbstract'/>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string layoutXml)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
		}

		private static bool AnyToggled(ViewDefinitionModel model)
			=> model.Roots.SelectMany(Flatten).Any(n => n.ToggleValue);

		private static System.Collections.Generic.IEnumerable<ViewNode> Flatten(ViewNode n)
		{
			yield return n;
			foreach (var c in n.Children)
				foreach (var d in Flatten(c))
					yield return d;
		}

		[Test]
		public void Slice_WithToggleValueTrue_SetsViewNodeToggleValue()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='Toggled'/>
</layout>");
			Assert.That(AnyToggled(model), Is.True,
				"a boolean slice with toggleValue='true' imports a node with ToggleValue so the composer inverts read/write (F-7)");
		}

		[Test]
		public void Slice_WithoutToggleValue_DefaultsToFalse()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='Plain'/>
</layout>");
			Assert.That(AnyToggled(model), Is.False,
				"an ordinary boolean slice is not inverted");
		}
	}
}
