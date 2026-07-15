// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 9.2 (override migrator, project-file side): reads a whole-copy <c>.fwlayout</c> override from
	/// disk, diffs it against the shipped layout, and writes the canonical JSON patch — verified with temp
	/// files and inline XML (no XCore/Inventory).
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideFileMigratorTests
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

		private static IPartResolver Parts() => new DictionaryPartResolver(XElement.Parse(PartsXml));

		private string _overrideFile;
		private string _outputFile;

		[SetUp]
		public void SetUp()
		{
			_overrideFile = Path.Combine(Path.GetTempPath(), "fwlayout-" + Guid.NewGuid().ToString("N") + ".fwlayout");
			_outputFile = Path.Combine(Path.GetTempPath(), "patch-" + Guid.NewGuid().ToString("N") + ".json");
		}

		[TearDown]
		public void TearDown()
		{
			if (File.Exists(_overrideFile)) File.Delete(_overrideFile);
			if (File.Exists(_outputFile)) File.Delete(_outputFile);
		}

		[Test]
		public void MigrateOverrideFile_ReadsOverride_ReturnsPatch_AndWritesJsonFile()
		{
			File.WriteAllText(_overrideFile, OverrideLayout);

			var patch = ViewDefinitionOverrideFileMigrator.MigrateOverrideFile(
				XElement.Parse(ShippedLayout), _overrideFile, Parts(), _outputFile);

			// Returned patch captures the customer's edit.
			var op = patch.Operations.Single();
			Assert.That(op.Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(op.StableId, Is.EqualTo("LexEntry/CfAndBib/#1"));
			Assert.That(op.Visibility, Is.EqualTo(ViewVisibility.Never));

			// And the canonical JSON patch file was written and round-trips.
			Assert.That(File.Exists(_outputFile), Is.True);
			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(File.ReadAllText(_outputFile));
			Assert.That(restored.Operations.Single().StableId, Is.EqualTo("LexEntry/CfAndBib/#1"));
		}

		[Test]
		public void MigrateOverrideFile_NoCustomization_WritesEmptyPatch()
		{
			File.WriteAllText(_overrideFile, ShippedLayout);

			var patch = ViewDefinitionOverrideFileMigrator.MigrateOverrideFile(
				XElement.Parse(ShippedLayout), _overrideFile, Parts(), _outputFile);

			Assert.That(patch.IsEmpty, Is.True);
			Assert.That(File.Exists(_outputFile), Is.True, "an empty patch is still written (records that the layout was reconciled)");
		}

		[Test]
		public void MigrateOverrideFile_MissingFile_Throws()
		{
			Assert.That(() => ViewDefinitionOverrideFileMigrator.MigrateOverrideFile(
					XElement.Parse(ShippedLayout), _overrideFile, Parts()),
				Throws.InstanceOf<FileNotFoundException>());
		}
	}
}
