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
	/// Task 9.2 (override migrator): a legacy whole-copy <c>.fwlayout</c> override imports + diffs into a
	/// sparse patch capturing exactly the customer's edits. Reuses the real <see cref="XmlLayoutImporter"/>
	/// over inline XML — no XCore/file I/O.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideMigratorTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='CitationForm' editor='multistring' field='CitationForm' ws='vernacular'/>
  </part>
  <part id='LexEntry-Detail-Bibliography'>
    <slice label='Bibliography' editor='multistring' field='Bibliography' ws='analysis'/>
  </part>
</bin></PartInventory>";

		private static IPartResolver Parts() => new DictionaryPartResolver(XElement.Parse(PartsXml));

		private const string ShippedLayout = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='ifdata'/>
</layout>";

		[Test]
		public void MigrateLayout_NoCustomization_ProducesEmptyPatch()
		{
			var patch = ViewDefinitionOverrideMigrator.MigrateLayout(ShippedLayout, ShippedLayout, Parts());
			Assert.That(patch.IsEmpty, Is.True);
		}

		[Test]
		public void MigrateLayout_VisibilityCustomization_ProducesSetVisibilityPatch()
		{
			// The project hid Bibliography (ifdata -> never), the legacy whole-copy override.
			const string overridden = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='never'/>
</layout>";

			var patch = ViewDefinitionOverrideMigrator.MigrateLayout(ShippedLayout, overridden, Parts());

			var op = patch.Operations.Single();
			Assert.That(op.Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(op.StableId, Is.EqualTo("LexEntry/CfAndBib/#1"), "Bibliography is the second root part");
			Assert.That(op.Visibility, Is.EqualTo(ViewVisibility.Never));
		}

		[Test]
		public void MigrateLayoutToJson_RoundTripsToTheSamePatch()
		{
			const string overridden = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='never'/>
</layout>";

			var json = ViewDefinitionOverrideMigrator.MigrateLayoutToJson(
				XElement.Parse(ShippedLayout), XElement.Parse(overridden), Parts());
			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(json);

			Assert.That(restored.Operations.Single().Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(restored.Operations.Single().StableId, Is.EqualTo("LexEntry/CfAndBib/#1"));
		}
	}
}
