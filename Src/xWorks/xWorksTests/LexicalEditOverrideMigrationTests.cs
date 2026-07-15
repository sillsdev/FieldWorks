// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task 9.2 (project-file side): the xWorks override-migration adapter composes the live inventory's
	/// parts + a shipped layout into the tested migration core. This exercises the framework-neutral
	/// XElement overload with inline XML + a temp override file (the live-<c>Inventory</c> overload is a
	/// thin XmlNode→XElement bridge over this same core, build-verified by the xWorks build).
	/// </summary>
	[TestFixture]
	public class LexicalEditOverrideMigrationTests
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

		private const string ShippedLayout = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='ifdata'/>
</layout>";

		private const string OverrideLayout = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='never'/>
</layout>";

		private string _overrideFile;
		private string _outputFile;

		[SetUp]
		public void SetUp()
		{
			_overrideFile = Path.Combine(Path.GetTempPath(), "fw-" + Guid.NewGuid().ToString("N") + ".fwlayout");
			_outputFile = Path.Combine(Path.GetTempPath(), "patch-" + Guid.NewGuid().ToString("N") + ".json");
		}

		[TearDown]
		public void TearDown()
		{
			if (File.Exists(_overrideFile)) File.Delete(_overrideFile);
			if (File.Exists(_outputFile)) File.Delete(_outputFile);
		}

		[Test]
		public void MigrateProjectOverride_FromXElements_ProducesPatch_AndWritesJson()
		{
			File.WriteAllText(_overrideFile, OverrideLayout);

			var patch = LexicalEditOverrideMigration.MigrateProjectOverride(
				XElement.Parse(ShippedLayout), XElement.Parse(PartsXml), _overrideFile, _outputFile);

			var op = patch.Operations.Single();
			Assert.That(op.Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(op.StableId, Is.EqualTo("LexEntry/CfAndBib/#1"));
			Assert.That(op.Visibility, Is.EqualTo(ViewVisibility.Never));

			Assert.That(File.Exists(_outputFile), Is.True);
			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(File.ReadAllText(_outputFile));
			Assert.That(restored.Operations.Single().StableId, Is.EqualTo("LexEntry/CfAndBib/#1"));
		}

		[Test]
		public void MigrateProjectOverride_NoCustomization_ProducesEmptyPatch()
		{
			File.WriteAllText(_overrideFile, ShippedLayout);

			var patch = LexicalEditOverrideMigration.MigrateProjectOverride(
				XElement.Parse(ShippedLayout), XElement.Parse(PartsXml), _overrideFile, _outputFile);

			Assert.That(patch.IsEmpty, Is.True);
		}
	}
}
