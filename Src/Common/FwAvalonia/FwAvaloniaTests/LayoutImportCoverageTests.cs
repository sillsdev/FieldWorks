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
      <deParams ws='best analysis'/>
    </slice>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses'/>
  </part>
  <part id='LexEntry-Detail-PublishIn'>
    <slice field='PublishIn' label='Publish Entry In' editor='defaultVectorReference'>
      <chooserInfo>
        <chooserLink type='goto' label='Edit the Publications list' tool='publicationsEdit'/>
      </chooserInfo>
    </slice>
  </part>
  <part id='LexEntry-Detail-MorphTypeTitled'>
    <slice field='MorphType' label='Morph Type' editor='MorphTypeAtomicReference'>
      <chooserInfo title='Choose Morpheme Type'/>
    </slice>
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
  <part ref='CitationForm' ghostInitMethod='CreateAllomorph'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "unhandled-attribute");
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Warning), "ghost wiring is functional");
			Assert.That(diag.Message, Does.Contain("'ghostInitMethod'"));
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
		public void MenuAndHotlinks_OnSliceContent_AreImported_NotDropped()
		{
			// Section 13.1 superseded the old drop-warning: menu bindings now land on the node.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='MenuSection'/>
</layout>");

			Assert.That(model.Diagnostics.Where(d => d.Code == "unhandled-attribute"), Is.Empty,
				"menu/hotlinks are handled attributes since 13.1");
			Assert.That(model.Roots[0].MenuId, Is.EqualTo("mnuDataTree-VariantForms"));
			Assert.That(model.Roots[0].HotlinksId, Is.EqualTo("mnuDataTree-VariantForms-Hotlinks"));
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
		public void ConditionalElements_WithSubstitutionValues_StillDropWithDiagnostics()
		{
			// B3 imports supported conditionals; a $-substituted condition value (the <generate>
			// custom-field shape) still needs runtime substitution (B9), so it keeps the drop lane.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <if target='$fieldName' is='StText'><part ref='CitationForm'/></if>
  <ifnot target='$fieldName' is='StText'><part ref='CitationForm'/></ifnot>
</layout>");

			Assert.That(model.Diagnostics.Count(d => d.Code == "conditional-dropped"), Is.EqualTo(2));
			Assert.That(model.Roots, Is.Empty, "an unevaluable condition must not import its content");
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

		// B7: the chooserLink jump links import as typed metadata on the slice node — the exact
		// shape LexEntryParts.xml:48-53 gives the Publish In field (the legacy chooser dialog's
		// "Edit the Publications list" link, ReallySimpleListChooser.cs:887-900).
		[Test]
		public void ChooserInfo_ChooserLinks_ImportOntoTheTypedNode()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='PublishIn'/>
</layout>");

			var node = model.Roots.Single();
			Assert.That(node.ChooserLinks, Has.Count.EqualTo(1));
			var link = node.ChooserLinks[0];
			Assert.That(link.Type, Is.EqualTo("goto"));
			Assert.That(link.Label, Is.EqualTo("Edit the Publications list"));
			Assert.That(link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(link.Target, Is.Null, "no target attribute in the Publications link");
			Assert.That(model.Diagnostics.Where(d => d.Code == "slice-content-dropped"), Is.Empty,
				"a chooserInfo that only carries links is fully consumed");
		}

		// B7 remainder: chooserInfo's OTHER facets (title/text/guicontrol/…) stay reported, not
		// silently dropped, so the remaining gap is still measured.
		[Test]
		public void ChooserInfo_NonLinkFacets_AreStillReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='MorphTypeTitled'/>
</layout>");

			Assert.That(model.Roots.Single().ChooserLinks, Is.Empty);
			var diag = model.Diagnostics.Single(d => d.Code == "slice-content-dropped");
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Info));
			Assert.That(diag.Message, Does.Contain("title"));
		}

		[Test]
		public void SliceContentChildren_OtherThanStructural_AreReported()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='SliceWithProperties'/>
</layout>");

			var diag = model.Diagnostics.Single(d => d.Code == "slice-content-dropped");
			Assert.That(diag.Message, Does.Contain("deParams"));
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
	/// B3 (xml-retirement-blockers): legacy <c>&lt;if&gt;</c>/<c>&lt;ifnot&gt;</c>/<c>&lt;choice&gt;</c>
	/// import as typed Conditional/ChoiceGroup nodes carrying structured ViewCondition metadata —
	/// the condition forms the shipped detail layouts use (boolequals/intequals/intlessthan/
	/// intmemberof/lengthatleast/lengthatmost/guidequals/is/target) — and round-trip canonical JSON.
	/// Unsupported (publishing-lane) forms keep the conditional-dropped lane.
	/// </summary>
	[TestFixture]
	public class ConditionalImportTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntryRef-Detail-VariantEntryTypes'>
    <if field='RefType' intequals='0'>
      <slice field='VariantEntryTypes' label='Variant Type' editor='possVectorReference'/>
    </if>
  </part>
  <part id='LexEntry-Detail-ShowMinorEntry'>
    <if field='EntryRefs' lengthatleast='1'>
      <slice field='PublishAsMinorEntry' label='Show Minor Entry' editor='checkbox'/>
    </if>
  </part>
  <part id='MoForm-Detail-NotAbstract'>
    <ifnot field='IsAbstract' boolequals='true'>
      <slice field='Form' label='Form' editor='multistring' ws='all vernacular'/>
    </ifnot>
  </part>
  <part id='MoAffixAllomorph-Detail-AsPosition'>
    <choice>
      <where field='MorphType' guidequals='D7F713DA-E8CF-11D3-9764-00C04F186933'>
        <slice label='Infix Positions' field='Position' editor='phoneEnvReference'/>
      </where>
      <otherwise>
        <slice label='Fallback' field='Form' editor='multistring'/>
      </otherwise>
    </choice>
  </part>
  <part id='MoAffixAllomorph-Detail-MsEnvFeatures'>
    <if target='owner' field='MorphoSyntaxAnalyses' lengthatleast='1'>
      <slice label='Required Features' field='MsEnvFeatures' editor='custom' assemblyPath='LexEdDll.dll' class='X.Y'/>
    </if>
  </part>
  <part id='LexRefType-Detail-ReverseName'>
    <if field='MappingType' intmemberof='2,3,7,8,12,13'>
      <slice field='ReverseName' label='Reverse Name' editor='multistring' ws='all analysis'/>
    </if>
  </part>
  <part id='LexSense-Detail-PublishingDropped'>
    <if field='Gloss' stringaltequals='' ws='analysis'>
      <slice field='Gloss' editor='multistring'/>
    </if>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string className, string refName)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(
				$"<layout class='{className}' type='detail' name='T'><part ref='{refName}'/></layout>"), parts);
		}

		[Test]
		public void If_ImportsConditionalNode_WithStructuredIntEqualsCondition()
		{
			var model = Import("LexEntryRef", "VariantEntryTypes");

			Assert.That(model.Diagnostics.Where(d => d.Code == "conditional-dropped"), Is.Empty);
			var node = model.Roots.Single();
			Assert.That(node.Kind, Is.EqualTo(ViewNodeKind.Conditional));
			Assert.That(node.Condition.Negated, Is.False);
			Assert.That(node.Condition.Field, Is.EqualTo("RefType"));
			Assert.That(node.Condition.IntEquals, Is.EqualTo(0));
			Assert.That(node.Children.Single().Field, Is.EqualTo("VariantEntryTypes"),
				"the conditional's content imports as child nodes");
		}

		[Test]
		public void If_WithLengthAtLeast_Imports()
		{
			var node = Import("LexEntry", "ShowMinorEntry").Roots.Single();
			Assert.That(node.Condition.LengthAtLeast, Is.EqualTo(1));
			Assert.That(node.Condition.Field, Is.EqualTo("EntryRefs"));
			Assert.That(node.Children.Single().Field, Is.EqualTo("PublishAsMinorEntry"));
		}

		[Test]
		public void Ifnot_SetsNegated_AndParsesBoolEquals()
		{
			var node = Import("MoForm", "NotAbstract").Roots.Single();
			Assert.That(node.Kind, Is.EqualTo(ViewNodeKind.Conditional));
			Assert.That(node.Condition.Negated, Is.True);
			Assert.That(node.Condition.BoolEquals, Is.True);
			Assert.That(node.Condition.Field, Is.EqualTo("IsAbstract"));
		}

		[Test]
		public void If_WithTargetOwner_AndIntMemberOf_Import()
		{
			var owner = Import("MoAffixAllomorph", "MsEnvFeatures").Roots.Single();
			Assert.That(owner.Condition.Target, Is.EqualTo("owner"));
			Assert.That(owner.Condition.LengthAtLeast, Is.EqualTo(1));

			var memberOf = Import("LexRefType", "ReverseName").Roots.Single();
			Assert.That(memberOf.Condition.IntMemberOf, Is.EqualTo("2,3,7,8,12,13"));
		}

		[Test]
		public void Choice_ImportsChoiceGroup_WithWhereAndOtherwiseBranches()
		{
			var node = Import("MoAffixAllomorph", "AsPosition").Roots.Single();

			Assert.That(node.Kind, Is.EqualTo(ViewNodeKind.ChoiceGroup));
			Assert.That(node.Children.Count, Is.EqualTo(2));
			Assert.That(node.Children[0].Kind, Is.EqualTo(ViewNodeKind.Conditional));
			Assert.That(node.Children[0].Condition.GuidEquals, Is.EqualTo("D7F713DA-E8CF-11D3-9764-00C04F186933"));
			Assert.That(node.Children[0].Children.Single().Label, Is.EqualTo("Infix Positions"));
			Assert.That(node.Children[1].Condition, Is.Null, "otherwise = unconditioned branch");
			Assert.That(node.Children[1].Children.Single().Label, Is.EqualTo("Fallback"));
		}

		[Test]
		public void UnsupportedPublishingConditionForm_KeepsTheDropLane()
		{
			// stringaltequals/ws is publishing-lane vocabulary (XmlVc string tests, all on Jt parts in
			// the shipped files); evaluating it wrongly would hide/show the wrong fields, so it drops.
			var model = Import("LexSense", "PublishingDropped");

			Assert.That(model.Roots, Is.Empty);
			Assert.That(model.Diagnostics.Single(d => d.Code == "conditional-dropped").Message,
				Does.Contain("stringaltequals").Or.Contain("ws"));
		}

		[Test]
		public void ConditionalNodes_RoundTripCanonicalJson_SnapshotIdentical()
		{
			foreach (var (cls, refName) in new[]
			{
				("LexEntryRef", "VariantEntryTypes"), ("MoForm", "NotAbstract"),
				("MoAffixAllomorph", "AsPosition"), ("MoAffixAllomorph", "MsEnvFeatures"),
				("LexRefType", "ReverseName")
			})
			{
				var model = Import(cls, refName);
				var reloaded = ViewDefinitionJsonSerializer.Deserialize(ViewDefinitionJsonSerializer.Serialize(model));
				// Deserialize intentionally drops import diagnostics, so compare the node tree only.
				var withoutDiagnostics = new ViewDefinitionModel(
					model.ClassName, model.LayoutName, model.LayoutType, model.Roots, null);
				Assert.That(reloaded.ToSnapshot(), Is.EqualTo(withoutDiagnostics.ToSnapshot()),
					$"{cls}-{refName}: the snapshot carries cond=[…], so a dropped condition fails here");
				var root = reloaded.Roots.Single();
				var original = model.Roots.Single();
				Assert.That(root.Condition?.ToString(), Is.EqualTo(original.Condition?.ToString()));
				Assert.That(root.Condition?.Negated, Is.EqualTo(original.Condition?.Negated));
			}
		}
	}

	/// <summary>
	/// B10 (cross-class part resolution): unit cases for the legacy-faithful resolution rules —
	/// the metadata-driven base-class walk (DataTree.cs:2444-2461) and case-insensitive part-id
	/// lookup (Inventory.GetElementKey lowercases key attrvals, Inventory.cs:1516).
	/// </summary>
	[TestFixture]
	public class CrossClassPartResolutionTests
	{
		private const string MoFormPartsXml = @"
<PartInventory><bin>
  <part id='MoForm-Detail-IsAbstractBasic'>
    <slice label='Is Abstract Form' field='IsAbstract' editor='checkbox'/>
  </part>
</bin></PartInventory>";

		[Test]
		public void ResolvePart_WalksMultiHopBaseClassChain()
		{
			// MoAffixAllomorph → MoAffixForm → MoForm: two hops, exactly the LCModel hierarchy.
			var map = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "MoAffixAllomorph", "MoAffixForm" },
				{ "MoAffixForm", "MoForm" }
			};
			var resolver = new DictionaryPartResolver(XElement.Parse(MoFormPartsXml), map);

			var content = resolver.ResolvePart("MoAffixAllomorph", "detail", "IsAbstractBasic");

			Assert.That(content, Is.Not.Null);
			Assert.That((string)content.Attribute("field"), Is.EqualTo("IsAbstract"));
		}

		[Test]
		public void ResolvePart_WithoutBaseClassMap_StillFailsAcrossClasses()
		{
			var resolver = new DictionaryPartResolver(XElement.Parse(MoFormPartsXml));

			Assert.That(resolver.ResolvePart("MoAffixAllomorph", "detail", "IsAbstractBasic"), Is.Null);
		}

		[Test]
		public void ResolvePart_IsCaseInsensitive_LikeLegacyInventory()
		{
			// Legacy Inventory.GetElementKey lowercases every key attrval (Inventory.cs:1516), so part
			// id lookup never depends on ref/id casing.
			var resolver = new DictionaryPartResolver(XElement.Parse(MoFormPartsXml));

			Assert.That(resolver.ResolvePart("MoForm", "detail", "isabstractbasic"), Is.Not.Null);
			Assert.That(resolver.ResolvePart("moform", "detail", "IsAbstractBasic"), Is.Not.Null);
		}

		[Test]
		public void BuildBaseClassMap_ParsesMasterModelClassHierarchy()
		{
			var masterModel = XElement.Parse(@"
<EntireModel>
  <CellarModule id='cellar'>
    <class num='0' id='CmObject' abstract='false'/>
    <class num='7' id='CmPossibility' abstract='false' base='CmObject'/>
    <class num='14' id='CmAnthroItem' abstract='false' base='CmPossibility'/>
  </CellarModule>
</EntireModel>");

			var map = LayoutImportCoverage.BuildBaseClassMap(masterModel);

			Assert.That(map["CmAnthroItem"], Is.EqualTo("CmPossibility"));
			Assert.That(map["CmPossibility"], Is.EqualTo("CmObject"));
			Assert.That(map.ContainsKey("CmObject"), Is.False, "the root class has no base entry");
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

		/// <summary>
		/// Loads the subclass → base class map from the pinned LCModel package's master model, the same
		/// hierarchy production resolution walks via the MDC (B10: metadata-driven, not hand-maintained).
		/// </summary>
		private static IReadOnlyDictionary<string, string> LoadBaseClassMap(string repoRoot)
		{
			var lcmPackageDir = Path.Combine(repoRoot, "packages", "sil.lcmodel");
			Assert.That(Directory.Exists(lcmPackageDir), Is.True, $"missing LCModel package dir: {lcmPackageDir}");

			// Prefer the version pinned in Build/SilVersions.props; otherwise the highest restored one.
			var pinned = System.Text.RegularExpressions.Regex
				.Match(File.ReadAllText(Path.Combine(repoRoot, "Build", "SilVersions.props")),
					@"<SilLcmVersion>([^<]+)</SilLcmVersion>").Groups[1].Value;
			var masterModelPath = Path.Combine(lcmPackageDir, pinned, "contentFiles", "MasterLCModel.xml");
			if (!File.Exists(masterModelPath))
			{
				masterModelPath = Directory.GetDirectories(lcmPackageDir)
					.OrderBy(d => d, StringComparer.Ordinal)
					.Select(d => Path.Combine(d, "contentFiles", "MasterLCModel.xml"))
					.LastOrDefault(File.Exists);
			}

			Assert.That(masterModelPath, Is.Not.Null.And.Property("Length").GreaterThan(0),
				"could not locate MasterLCModel.xml in the restored LCModel package");
			var map = LayoutImportCoverage.BuildBaseClassMap(XElement.Load(masterModelPath));
			Assert.That(map["MoStemAllomorph"], Is.EqualTo("MoForm"), "sanity: master model hierarchy parsed");
			return map;
		}

		[Test]
		public void ShippedLayouts_CoverageReport_IsGeneratedAndMeasured()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);

			var report = LayoutImportCoverage.Run(layouts, parts, LoadBaseClassMap(repoRoot));

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

			var report = LayoutImportCoverage.Run(layouts, parts, LoadBaseClassMap(repoRoot));
			var unknown = report.DiagnosticsByCode.Keys
				.Select(k => k.Substring(0, k.IndexOf(" (", StringComparison.Ordinal)))
				.Where(code => !knownCodes.Contains(code))
				.Distinct()
				.ToList();

			Assert.That(unknown, Is.Empty,
				"every emitted diagnostic code must be a classified drop, not an accidental one: " + string.Join(", ", unknown));
		}

		/// <summary>
		/// B10 (cross-class part resolution): with the metadata-driven base-class walk the importer
		/// resolves every shipped part ref that legacy DataTree resolves. What remains is EXACTLY the
		/// set of layout refs with no <c>{class-chain}-Detail-{ref}</c> part anywhere in the shipped
		/// inventories — refs legacy DataTree also silently omits ("Just omit the missing part",
		/// DataTree.cs:2455-2457; the detail lane has no PartGenerator). Asserting the exact set keeps
		/// both regressions (new unresolved refs) and silent improvements (parts added) visible.
		/// </summary>
		[Test]
		public void ShippedLayouts_UnresolvedParts_AreExactlyTheLegacyUnresolvableSet()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var report = LayoutImportCoverage.Run(layouts, parts, LoadBaseClassMap(repoRoot));

			// Every entry below was verified to have no matching part id (case-insensitive) on the
			// class or any base class in the shipped *Parts.xml files; legacy omits them too. Most are
			// summary/section header parts that were never shipped, plus the DateCreated/DateModified
			// refs Notebook hides (visibility='never') whose parts never existed in the detail lane.
			var legacyUnresolvable = new SortedDictionary<string, int>(StringComparer.Ordinal)
			{
				{ "CmAnthroItem-Summary", 1 },
				{ "CmPerson-Role", 1 },
				{ "CmPossibility-Summary", 1 },
				{ "CmSemanticDomain-Summary", 1 },
				{ "FsClosedValue-Summary", 1 },
				{ "FsComplexFeature-Message", 1 },
				{ "FsComplexValue-Summary", 1 },
				{ "FsFeatStruc-Blank", 1 },
				{ "FsFeatStrucType-Message", 1 },
				{ "FsFeatureSpecification-Summary", 1 },
				{ "LexEntry-ImportResidue", 1 },
				{ "LexEntryInflType-Summary", 1 },
				{ "LexEntryType-Summary", 2 },
				{ "LexEtymology-NormalSummary", 1 },
				{ "LexExtendedNote-NormalSummary", 1 },
				{ "LexPronunciation-MediaFiles", 1 },
				{ "LexReference-ShowSingleReference", 1 },
				{ "LexSense-HeavySummary", 1 },
				{ "LexSense-ImportResidue", 1 },
				{ "LexSense-Pictures", 1 },
				{ "MoAlloAdhocProhib-Message", 1 },
				{ "MoEndoCompound-HeadLast", 2 },
				{ "MoExoCompound-ToMsa", 1 },
				{ "MoInflAffixSlot-Optional", 1 },
				{ "MoInflClass-SubclassesAllA", 1 },
				{ "MoMorphAdhocProhib-Message", 1 },
				{ "PartOfSpeech-Section", 2 },
				{ "PhPhoneme-Codes", 1 },
				{ "ReversalIndexEntry-Section", 2 },
				{ "RnGenericRec-DateCreated", 12 },
				{ "RnGenericRec-DateModified", 12 },
				{ "Text-DateCreated", 1 },
				{ "Text-DateModified", 1 },
				{ "WfiAnalysis-HeavySummary", 3 },
				{ "WfiWordform-HeavySummary", 1 }
			};

			Assert.That(report.UnresolvedPartRefs, Is.EquivalentTo(legacyUnresolvable),
				"the unresolved-part set changed; if a part was added/renamed update this set, if a "
				+ "resolution rule regressed fix the resolver (B10 baseline: 63 occurrences, was 259)");
			Assert.That(report.UnresolvedPartRefs.Values.Sum(), Is.EqualTo(63),
				"B10 unresolved-part occurrence ceiling");
		}

		/// <summary>
		/// B10 fixture: the real CmAnthroItem 'default' layout previously raised 9 unresolved-part
		/// Errors because all its refs live on the CmPossibility base class. With the metadata map the
		/// whole layout imports clean except the constructs other blockers own.
		/// </summary>
		[Test]
		public void CmAnthroItemDefaultLayout_ResolvesAllParts_ViaCmPossibilityBaseClass()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "CmAnthroItem", "default");

			Assert.That(model.Diagnostics.Where(d => d.Code == "unresolved-part"), Is.Empty);
			// All 9 refs resolve and produce nodes; SubPossibilities' <choice> content imports as a
			// typed ChoiceGroup since B3 landed (one empty where branch + the Subitems otherwise).
			Assert.That(model.Roots.Count, Is.EqualTo(9), "NameAllA … SubPossibilities produce nodes");
			Assert.That(model.Diagnostics.Count(d => d.Code == "unknown-part-content"), Is.EqualTo(0),
				"the <choice> content is imported, no longer dropped");
			var choice = model.Roots.Single(r => r.Kind == ViewNodeKind.ChoiceGroup);
			Assert.That(choice.Children.Count, Is.EqualTo(2), "a where branch and an otherwise branch");
			Assert.That(choice.Children[0].Condition.ToString(),
				Is.EqualTo("target=owner is=CmPossibilityList field=Depth intlessthan=2"));
			Assert.That(choice.Children[1].Condition, Is.Null, "the otherwise branch has no condition");
		}

		/// <summary>
		/// B10 fixture: CmBaseAnnotation 'Edit' resolves 'TextOnly' from CmAnnotation (one hop) and
		/// 'BeginObjectLink' on the class itself.
		/// </summary>
		[Test]
		public void CmBaseAnnotationEditLayout_ResolvesTextOnly_ViaCmAnnotation()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "CmBaseAnnotation", "Edit");

			Assert.That(model.Diagnostics.Where(d => d.Code == "unresolved-part"), Is.Empty);
			Assert.That(model.Roots.Count, Is.EqualTo(2));
		}

		/// <summary>
		/// B10 fixture: the first-slice lane's hand-maintained MoForm map (4.10) is now subsumed by the
		/// metadata-driven hierarchy — MoStemAllomorph's 'AsLexemeFormBasic' resolves with no hand map.
		/// </summary>
		[Test]
		public void MoStemAllomorphAsLexemeFormBasic_ResolvesViaMetadataMap_WithoutHandMaintainedMap()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "MoStemAllomorph", "AsLexemeFormBasic");

			Assert.That(model.Diagnostics.Where(d => d.Code == "unresolved-part"), Is.Empty);
			Assert.That(model.Roots, Is.Not.Empty);
		}

		/// <summary>
		/// B10 fixture: a documented member of the remaining set. CmAnthroItem 'nested' refs 'Summary'
		/// and no Summary detail part exists on CmAnthroItem/CmPossibility/CmObject — legacy DataTree
		/// omits the slice the same way (DataTree.cs:2455-2457).
		/// </summary>
		[Test]
		public void CmAnthroItemNestedLayout_SummaryStaysUnresolved_MatchingLegacyOmission()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "CmAnthroItem", "nested");

			var unresolved = model.Diagnostics.Where(d => d.Code == "unresolved-part").ToList();
			Assert.That(unresolved.Count, Is.EqualTo(1));
			Assert.That(unresolved[0].Message, Does.Contain("'Summary'"));
		}

		/// <summary>
		/// B3 fixture: the real shipped variant/complex-form divergence. LexEntryRef/Normal's
		/// VariantEntryTypes and ComplexEntryTypes parts are <c>&lt;if field="RefType" intequals=…&gt;</c>
		/// twins — they must import as Conditional nodes with the structured condition, so the
		/// composer can show exactly one per record.
		/// </summary>
		[Test]
		public void ShippedLexEntryRefNormal_ImportsTheRefTypeConditionals()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "LexEntryRef", "Normal");

			var conditionals = model.Roots.Where(r => r.Kind == ViewNodeKind.Conditional).ToList();
			Assert.That(conditionals.Count, Is.GreaterThanOrEqualTo(2),
				"VariantEntryTypes and ComplexEntryTypes import as conditionals");
			var variant = conditionals.Single(c => c.Children.Any(ch => ch.Field == "VariantEntryTypes"));
			var complex = conditionals.Single(c => c.Children.Any(ch => ch.Field == "ComplexEntryTypes"));
			Assert.That(variant.Condition.Field, Is.EqualTo("RefType"));
			Assert.That(variant.Condition.IntEquals, Is.EqualTo(0), "RefType 0 = variant");
			Assert.That(complex.Condition.IntEquals, Is.EqualTo(1), "RefType 1 = complex form");
			Assert.That(model.Diagnostics.Where(d => d.Code == "conditional-dropped"), Is.Empty,
				"every condition form LexEntryRef/Normal uses is in the supported set");
		}

		/// <summary>
		/// B2 fixture: the shipped lexeme-form ghost configuration must arrive complete on the typed
		/// node — ghost/ghostWs/ghostLabel, the explicit ghostClass (MoStemAllomorph, differing from
		/// the abstract MoForm field signature) AND the ghostInitMethod hook (SetMorphTypeToRoot) the
		/// 14.1 lane used to drop.
		/// </summary>
		[Test]
		public void ShippedLexEntryNormal_LexemeFormNode_CarriesTheFullGhostConfiguration()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);
			var model = ImportShippedLayout(repoRoot, layouts, parts, "LexEntry", "Normal");

			var lexemeForm = model.Roots.Single(r => r.Field == "LexemeForm");
			Assert.That(lexemeForm.GhostField, Is.EqualTo("Form"));
			Assert.That(lexemeForm.GhostWs, Is.EqualTo("vernacular"));
			Assert.That(lexemeForm.GhostClass, Is.EqualTo("MoStemAllomorph"));
			Assert.That(lexemeForm.GhostLabel, Is.EqualTo("Lexeme Form"));
			Assert.That(lexemeForm.GhostInitMethod, Is.EqualTo("SetMorphTypeToRoot"));
		}

		/// <summary>
		/// B2 audit: every distinct ghost configuration in the shipped LEXICON detail parts imports
		/// with its complete metadata (no ghost attribute is dropped from obj/seq nodes anymore).
		/// The shipped `ghostAbbe` attribute is a typo legacy also ignores (it reads `ghostAbbr`,
		/// DataTree.cs:2827), so it intentionally stays an unhandled-attribute diagnostic.
		/// </summary>
		[Test]
		public void ShippedLexiconGhostConfigurations_AllImportWithCompleteMetadata()
		{
			var repoRoot = FindRepoRoot();
			var (layouts, parts) = LoadShippedFiles(repoRoot);

			// LexExampleSentence/Normal reaches TranslationsAllA: seq Translations ghost=Translation
			// ghostWs=analysis ghostInitMethod=SetTypeToFreeTrans (implicit class CmTranslation).
			var example = ImportShippedLayout(repoRoot, layouts, parts, "LexExampleSentence", "Normal");
			var translations = FindNode(example.Roots, n => n.Field == "Translations");
			Assert.That(translations, Is.Not.Null);
			Assert.That(translations.GhostField, Is.EqualTo("Translation"));
			Assert.That(translations.GhostWs, Is.EqualTo("analysis"));
			Assert.That(translations.GhostClass, Is.Null, "class comes from the field signature");
			Assert.That(translations.GhostInitMethod, Is.EqualTo("SetTypeToFreeTrans"));

			// LexSense/Normal reaches Examples (ghost=Example ghostWs=vernacular) and ExtendedNotes
			// (ghost=Discussion ghostWs=analysis), both without ghostClass/ghostInitMethod.
			var sense = ImportShippedLayout(repoRoot, layouts, parts, "LexSense", "Normal");
			var examples = FindNode(sense.Roots, n => n.Field == "Examples" && n.GhostField != null);
			Assert.That(examples?.GhostField, Is.EqualTo("Example"));
			Assert.That(examples?.GhostWs, Is.EqualTo("vernacular"));
			var notes = FindNode(sense.Roots, n => n.Field == "ExtendedNote");
			Assert.That(notes?.GhostField, Is.EqualTo("Discussion"));
			Assert.That(notes?.GhostWs, Is.EqualTo("analysis"));
		}

		private static ViewNode FindNode(IEnumerable<ViewNode> nodes, Func<ViewNode, bool> predicate)
		{
			foreach (var node in nodes)
			{
				if (predicate(node))
					return node;
				var inChildren = FindNode(node.Children, predicate);
				if (inChildren != null)
					return inChildren;
			}

			return null;
		}

		private static ViewDefinitionModel ImportShippedLayout(string repoRoot,
			List<LayoutSourceFile> layouts, List<LayoutSourceFile> parts, string className, string layoutName)
		{
			var layout = LayoutSourceLoader.FindLayout(layouts.Select(f => f.Root), className, layoutName);
			Assert.That(layout, Is.Not.Null, $"shipped layout {className}/{layoutName} not found");

			var mergedParts = new XElement("PartInventory", parts.Select(f => f.Root));
			var resolver = new DictionaryPartResolver(mergedParts, LoadBaseClassMap(repoRoot));
			return new XmlLayoutImporter().Import(layout, resolver);
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
