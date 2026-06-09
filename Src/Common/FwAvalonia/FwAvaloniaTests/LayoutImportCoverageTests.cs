// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 4.9: the importer must report every silently dropped layout construct/attribute as a
	/// diagnostic, and importer coverage over the shipped layout files must be a measured number.
	/// </summary>
	[TestFixture]
	public class XmlLayoutImporterDropDiagnosticsTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='CitationForm' editor='multistring' field='CitationForm' ws='vernacular'/>
  </part>
  <part id='LexEntry-Detail-MenuSection'>
    <slice label='Section' menu='mnuDataTree-VariantForms' hotlinks='mnuDataTree-VariantForms-Hotlinks'/>
  </part>
  <part id='LexEntry-Detail-StyledSlice'>
    <slice label='Styled' editor='string' field='X' ws='analysis' style='Dictionary-Normal' before='[' after=']'/>
  </part>
  <part id='LexEntry-Detail-SliceWithProperties'>
    <slice label='HasProps' editor='string' field='X' ws='analysis'>
      <properties><bold value='on'/></properties>
    </slice>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses'/>
  </part>
  <part id='LexEntry-Detail-SubstitutionWs'>
    <slice label='Subst' editor='string' field='X' ws='$ws=vernacular'/>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string layoutXml)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
		}

		[Test]
		public void UnhandledFunctionalAttribute_OnCallerPart_RaisesWarning()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='CitationForm' menu='mnuDataTree-Object'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "unhandled-attribute");
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Warning), "menu wiring is functional");
			Assert.That(diag.Message, Does.Contain("'menu'"));
		}

		[Test]
		public void UnhandledPresentationalAttribute_OnSliceContent_RaisesInfo()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='StyledSlice'/>
</layout>");

			var dropped = model.Diagnostics.Where(d => d.Code == "unhandled-attribute").ToList();
			Assert.That(dropped.Select(d => d.Severity), Is.All.EqualTo(ViewDiagnosticSeverity.Info));
			Assert.That(dropped.Count, Is.EqualTo(3), "style, before, after");
		}

		[Test]
		public void MenuAndHotlinks_OnSliceContent_RaiseWarnings()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='MenuSection'/>
</layout>");

			var dropped = model.Diagnostics.Where(d => d.Code == "unhandled-attribute").ToList();
			Assert.That(dropped.Count, Is.EqualTo(2), "menu + hotlinks");
			Assert.That(dropped.Select(d => d.Severity), Is.All.EqualTo(ViewDiagnosticSeverity.Warning));
		}

		[Test]
		public void GenerateElement_RaisesNamedDropDiagnostic()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <generate class='LexEntry' fieldType='mlstring' restrictions='customOnly'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "generated-content-dropped");
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Warning));
		}

		[Test]
		public void ConditionalElements_RaiseNamedDropDiagnostics()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <if target='$fieldName' is='StText'><part ref='CitationForm'/></if>
  <ifnot target='$fieldName' is='StText'><part ref='CitationForm'/></ifnot>
</layout>");

			Assert.That(model.Diagnostics.Count(d => d.Code == "conditional-dropped"), Is.EqualTo(2));
		}

		[Test]
		public void SublayoutElement_RaisesInfoDropDiagnostic()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <sublayout name='domainLabel' group='para'/>
</layout>");

			Assert.That(model.Diagnostics.Single(d => d.Code == "sublayout-dropped").Severity,
				Is.EqualTo(ViewDiagnosticSeverity.Info));
		}

		[Test]
		public void CallerChildren_IndentAndPart_UnderSliceContentPart_AreImportedAsChildren()
		{
			// Mirrors the real AsLexemeFormBasic shape: a slice-content part whose caller nests
			// <indent><part .../></indent>. DataTree realizes these as indented child slices, so the
			// importer must too (task 4.10).
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='MenuSection'>
    <indent><part ref='CitationForm'/></indent>
  </part>
</layout>");

			Assert.That(model.Diagnostics.Any(d => d.Code == "caller-children-dropped"), Is.False);
			var section = model.Roots.Single();
			Assert.That(section.Children.Count, Is.EqualTo(1));
			Assert.That(section.Children[0].Field, Is.EqualTo("CitationForm"));
			Assert.That(section.Children[0].Indented, Is.True);
		}

		[Test]
		public void CallerChildren_OfOtherKinds_UnderSliceContentPart_AreStillReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='MenuSection'>
    <chooserInfo text='pick one'/>
  </part>
</layout>");

			Assert.That(model.Diagnostics.Any(d => d.Code == "caller-children-dropped"), Is.True,
				"non-structural caller children must still be reported, not silently dropped");
		}

		[Test]
		public void NonPartCallerChildren_UnderSequencePart_AreReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='Senses'>
    <indent><part ref='CitationForm'/></indent>
  </part>
</layout>");

			Assert.That(model.Diagnostics.Any(d => d.Code == "injected-child-dropped"), Is.True);
		}

		[Test]
		public void SliceContentChildren_OtherThanStructural_AreReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='SliceWithProperties'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "slice-content-dropped");
			Assert.That(diag.Message, Does.Contain("properties"));
		}

		[Test]
		public void SubstitutionValues_InHandledAttributes_AreReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='SubstitutionWs'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "param-substitution");
			Assert.That(diag.Message, Does.Contain("$ws=vernacular"));
		}

		[Test]
		public void CleanLayout_StillProducesNoDiagnostics()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='CitationForm'/>
</layout>");

			Assert.That(model.Diagnostics, Is.Empty, "drop diagnostics must not add noise to clean layouts");
		}
	}

	/// <summary>
	/// Task 4.9: runs the importer over every shipped .fwlayout/Parts.xml pair and regenerates the
	/// committed coverage report so importer coverage is a tracked number, not an assumption.
	/// </summary>
	[TestFixture]
	public class LayoutImportCoverageTests
	{
		private static string FindRepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
			{
				dir = dir.Parent;
			}

			Assert.That(dir, Is.Not.Null, "could not locate the repo root from the test directory");
			return dir.FullName;
		}

		private static (List<LayoutSourceFile> layouts, List<LayoutSourceFile> parts) LoadShippedFiles(string repoRoot)
		{
			var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");
			Assert.That(Directory.Exists(partsDir), Is.True, $"missing shipped parts directory: {partsDir}");

			var layouts = Directory.GetFiles(partsDir, "*.fwlayout")
				.OrderBy(f => f, StringComparer.Ordinal)
				.Select(f => new LayoutSourceFile(Path.GetFileName(f), XElement.Load(f)))
				.ToList();
			var parts = Directory.GetFiles(partsDir, "*Parts.xml")
				.OrderBy(f => f, StringComparer.Ordinal)
				.Select(f => new LayoutSourceFile(Path.GetFileName(f), XElement.Load(f)))
				.ToList();

			Assert.That(layouts, Is.Not.Empty, "no .fwlayout files found");
			Assert.That(parts, Is.Not.Empty, "no *Parts.xml files found");
			return (layouts, parts);
		}

		[Test]
		public void ShippedLayouts_CoverageReport_IsGeneratedAndMeasured()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);

			var report = LayoutImportCoverage.Run(layouts, parts);

			Assert.That(report.DetailLayoutsImported, Is.GreaterThan(0), "no detail layouts were imported");
			Assert.That(report.NodesProduced, Is.GreaterThan(0), "import produced no typed nodes");

			// The whole point of task 4.9: the gap must be visible. If these start failing because the
			// numbers hit zero, the importer has reached full vocabulary coverage — celebrate, then
			// tighten the assertions.
			TestContext.WriteLine(
				$"element coverage {report.ElementCoveragePercent:F1}%, attribute coverage {report.AttributeCoveragePercent:F1}%, " +
				$"layouts {report.DetailLayoutsImported}, nodes {report.NodesProduced}");

			var markdown = report.ToMarkdown();
			Assert.That(markdown, Does.Contain("## Summary"));

			WriteReportArtifacts(repoRoot, markdown);
		}

		[Test]
		public void ShippedLayouts_EveryDiagnosticCode_IsInTheKnownTaxonomy()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);

			var knownCodes = new HashSet<string>(StringComparer.Ordinal)
			{
				// structural / resolution
				"unknown-container-element", "unknown-part-content", "part-without-ref",
				"unresolved-part", "cross-object-deferred",
				// editors
				"dynamic-editor", "obsolete-editor", "unknown-editor",
				// task 4.9 drop taxonomy
				"unhandled-attribute", "param-substitution", "generated-content-dropped",
				"conditional-dropped", "sublayout-dropped", "caller-children-dropped",
				"injected-child-dropped", "slice-content-dropped"
			};

			var report = LayoutImportCoverage.Run(layouts, parts);
			var unknown = report.DiagnosticsByCode.Keys
				.Select(k => k.Substring(0, k.IndexOf(" (", StringComparison.Ordinal)))
				.Where(code => !knownCodes.Contains(code))
				.Distinct()
				.ToList();

			Assert.That(unknown, Is.Empty,
				"every emitted diagnostic code must be a classified drop, not an accidental one: " + string.Join(", ", unknown));
		}

		private static void WriteReportArtifacts(string repoRoot, string markdown)
		{
			// Always write next to the test results.
			var workCopy = Path.Combine(TestContext.CurrentContext.WorkDirectory, "layout-import-coverage.md");
			File.WriteAllText(workCopy, markdown);
			TestContext.WriteLine($"coverage report: {workCopy}");

			// Best effort: refresh the committed copy in the openspec change so the documented number
			// can never drift from the measured one. Skipped silently on read-only checkouts.
			try
			{
				var openspecCopy = Path.Combine(repoRoot, "openspec", "changes",
					"lexical-edit-avalonia-migration", "layout-import-coverage.md");
				if (Directory.Exists(Path.GetDirectoryName(openspecCopy)))
				{
					File.WriteAllText(openspecCopy, markdown);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
