// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 9.4: a gated (migrated) surface loads the committed canonical JSON instead of runtime XML,
	/// with the XML import retained as the audit/fallback path and every fallback recorded as a diagnostic.
	/// Pure logic over the real compiler + serializer.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionLoaderTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='CitationForm' editor='multistring' field='CitationForm' ws='vernacular'/>
  </part>
</bin></PartInventory>";

		private const string LayoutXml = @"
<layout class='LexEntry' type='detail' name='CfOnly'>
  <part ref='CitationForm'/>
</layout>";

		private static ViewDefinitionSourceSnapshot Snapshot()
			=> new ViewDefinitionSourceSnapshot("LexEntry", "detail", LayoutXml, PartsXml);

		private static ViewDefinitionLoader Loader(
			System.Func<ViewDefinitionSourceSnapshot, bool> gated = null,
			System.Func<ViewDefinitionSourceSnapshot, string> json = null)
			=> new ViewDefinitionLoader(new ViewDefinitionCompiler(), gated, json);

		[Test]
		public void NotGated_LoadsFromXml()
		{
			var result = Loader().Load(Snapshot());

			Assert.That(result.SourceKind, Is.EqualTo(ViewDefinitionSourceKind.Xml));
			Assert.That(result.Model.Roots.Single().Field, Is.EqualTo("CitationForm"));
			Assert.That(result.LoadDiagnostics, Is.Empty);
		}

		[Test]
		public void Gated_WithValidJson_LoadsFromJson_NotXml()
		{
			// Canonical JSON produced from the same definition (what the build would have committed).
			var canonicalJson = ViewDefinitionJsonSerializer.Serialize(new ViewDefinitionCompiler().Compile(Snapshot()));

			var result = Loader(gated: _ => true, json: _ => canonicalJson).Load(Snapshot());

			Assert.That(result.SourceKind, Is.EqualTo(ViewDefinitionSourceKind.Json));
			Assert.That(result.Model.Roots.Single().Field, Is.EqualTo("CitationForm"));
			Assert.That(result.LoadDiagnostics, Is.Empty);
		}

		[Test]
		public void Gated_MissingJson_FallsBackToXml_WithDiagnostic()
		{
			var result = Loader(gated: _ => true, json: _ => null).Load(Snapshot());

			Assert.That(result.SourceKind, Is.EqualTo(ViewDefinitionSourceKind.Xml));
			Assert.That(result.LoadDiagnostics.Any(d => d.Code == "json-source-missing"), Is.True);
		}

		[Test]
		public void Gated_InvalidJson_FallsBackToXml_WithDiagnostic_NotThrow()
		{
			var result = Loader(gated: _ => true, json: _ => "{ this is not valid json").Load(Snapshot());

			Assert.That(result.SourceKind, Is.EqualTo(ViewDefinitionSourceKind.Xml));
			Assert.That(result.LoadDiagnostics.Any(d => d.Code == "json-load-failed"), Is.True);
			Assert.That(result.Model.Roots.Single().Field, Is.EqualTo("CitationForm"));
		}
	}
}
