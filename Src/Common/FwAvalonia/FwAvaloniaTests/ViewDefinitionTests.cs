// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Tests for importing legacy XML Parts/Layout into the typed view definition. The inline XML
	/// mirrors the real schema used by DetailControlsTests/Test.fwlayout and TestParts.xml.
	/// </summary>
	[TestFixture]
	public class XmlLayoutImporterTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='CitationForm' editor='multistring' field='CitationForm' ws='vernacular'/>
  </part>
  <part id='LexEntry-Detail-Bibliography'>
    <slice label='Bibliography' editor='multistring' field='Bibliography' ws='analysis'/>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses' />
  </part>
  <part id='LexEntry-Detail-Nested-Expanded'>
    <slice label='Header' expansion='expanded'>
      <slice label='Citation form' editor='string' field='CitationForm' ws='vernacular'/>
      <slice label='Bibliography' editor='string' field='Bibliography' ws='analysis'/>
    </slice>
  </part>
  <part id='LexEntry-Detail-CustomEditor'>
    <slice label='Custom' editor='custom' field='testField' ws='analysis'/>
  </part>
  <part id='LexEntry-Detail-WeirdEditor'>
    <slice label='Weird' editor='weirdeditor' field='X' ws='analysis'/>
  </part>
  <part id='LexEntry-Detail-ObsoleteEditor'>
    <slice label='Old' editor='message' field='Y' ws='analysis'/>
  </part>
  <part id='LexEntry-Detail-JtView'>
    <slice label='Pronunciation' editor='jtview' field='Pronunciations' layout='PublishPron'/>
  </part>
  <part id='LexEntry-Detail-PerFieldWs'>
    <slice label='Form' editor='multistring' field='CitationForm' ws='all analysis'/>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string layoutXml)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
		}

		[Test]
		public void Import_CfAndBib_ProducesTwoFieldsWithStableBindings()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='ifdata'/>
</layout>");

			Assert.That(model.Roots.Count, Is.EqualTo(2));
			Assert.That(model.Diagnostics, Is.Empty);

			var cf = model.Roots[0];
			Assert.That(cf.StableId, Is.EqualTo("LexEntry/CfAndBib/#0"));
			Assert.That(cf.Kind, Is.EqualTo(ViewNodeKind.Field));
			Assert.That(cf.Field, Is.EqualTo("CitationForm"));
			Assert.That(cf.RawEditor, Is.EqualTo("multistring"));
			Assert.That(cf.EditorClassification, Is.EqualTo(EditorClassification.Known));
			Assert.That(cf.WritingSystem, Is.EqualTo("vernacular"));
			Assert.That(cf.Visibility, Is.EqualTo(ViewVisibility.Always));

			var bib = model.Roots[1];
			Assert.That(bib.Visibility, Is.EqualTo(ViewVisibility.IfData), "caller visibility overrides");
		}

		[Test]
		public void Import_Snapshot_IsDeterministic()
		{
			const string layout = @"
<layout class='LexEntry' type='detail' name='CfAndBib'>
  <part ref='CitationForm'/>
  <part ref='Bibliography' visibility='ifdata'/>
</layout>";

			var first = Import(layout).ToSnapshot();
			var second = Import(layout).ToSnapshot();

			Assert.That(second, Is.EqualTo(first), "import snapshot must be deterministic");
			Assert.That(first, Does.Contain(
				"LexEntry/CfAndBib/#0 | Field | label=CitationForm | field=CitationForm | editor=multistring(Known)"));
			Assert.That(first, Does.Contain("vis=IfData"));
		}

		[Test]
		public void Import_NestedGrouping_ProducesGroupWithChildren()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Nested-Expanded'>
  <part ref='Nested-Expanded'/>
</layout>");

			Assert.That(model.Roots.Count, Is.EqualTo(1));
			var header = model.Roots[0];
			Assert.That(header.Kind, Is.EqualTo(ViewNodeKind.Group));
			Assert.That(header.Expansion, Is.EqualTo(ViewExpansion.Expanded));
			Assert.That(header.Children.Count, Is.EqualTo(2));
			Assert.That(header.Children[0].StableId, Is.EqualTo("LexEntry/Nested-Expanded/#0/#0"));
			Assert.That(header.Children[0].Field, Is.EqualTo("CitationForm"));
			Assert.That(header.Children[1].Field, Is.EqualTo("Bibliography"));
		}

		[Test]
		public void Import_SequenceAndCustomFieldPlaceholder()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Normal'>
  <part ref='CitationForm'/>
  <part ref='Senses' visibility='ifdata' param='GlossSn' expansion='expanded'/>
  <part ref='_CustomFieldPlaceholder' customFields='here'/>
</layout>");

			Assert.That(model.Roots.Count, Is.EqualTo(3));

			var senses = model.Roots[1];
			Assert.That(senses.Kind, Is.EqualTo(ViewNodeKind.Sequence));
			Assert.That(senses.Field, Is.EqualTo("Senses"));
			Assert.That(senses.TargetLayout, Is.EqualTo("GlossSn"), "param supplies the item layout");
			Assert.That(senses.Expansion, Is.EqualTo(ViewExpansion.Expanded));

			var placeholder = model.Roots[2];
			Assert.That(placeholder.Kind, Is.EqualTo(ViewNodeKind.CustomFieldPlaceholder));
		}

		[Test]
		public void Import_JtViewSlice_CapturesNestedLayoutAsTargetLayout()
		{
			// §19e: a jtview slice's param/layout names the nested layout to compose inline; it must ride
			// the node as TargetLayout so the composer's WalkEmbeddedView can descend into it. The caller's
			// param wins over the slice's layout attribute (legacy SliceFactory jtview).
			var model = Import(@"
<layout class='LexEntry' type='detail' name='JtV'>
  <part ref='JtView' param='PronInEntry'/>
</layout>");

			var jt = model.Roots[0];
			Assert.That(jt.RawEditor, Is.EqualTo("jtview"));
			Assert.That(jt.TargetLayout, Is.EqualTo("PronInEntry"),
				"the caller param supplies the nested layout for the embedded view");
		}

		[Test]
		public void Import_JtViewSlice_FallsBackToSliceLayoutAttribute()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='JtV'>
  <part ref='JtView'/>
</layout>");
			Assert.That(model.Roots[0].TargetLayout, Is.EqualTo("PublishPron"),
				"with no caller param, the slice's own layout attribute is the nested layout");
		}

		[Test]
		public void Import_VisibleWritingSystems_CapturesThePerFieldWsOverride()
		{
			// §19e: a per-field visibleWritingSystems override (on the part ref) restricts the displayed
			// writing systems. The ordered specs ride the node for the composer to intersect with the set.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='PFW'>
  <part ref='PerFieldWs' visibleWritingSystems='fr en'/>
</layout>");

			var field = model.Roots[0];
			Assert.That(field.VisibleWritingSystems, Is.Not.Null);
			Assert.That(field.VisibleWritingSystems, Is.EqualTo(new[] { "fr", "en" }),
				"the override's ordered specs ride the node");
		}

		[Test]
		public void Import_NoVisibleWritingSystems_LeavesOverrideNull()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='PFW'>
  <part ref='PerFieldWs'/>
</layout>");
			Assert.That(model.Roots[0].VisibleWritingSystems, Is.Null,
				"a field with no override shows the full configured set (null = no restriction)");
		}

		[Test]
		public void Import_DynamicEditor_RaisesInfoDiagnostic()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Custom'>
  <part ref='CustomEditor'/>
</layout>");

			Assert.That(model.Roots[0].EditorClassification, Is.EqualTo(EditorClassification.Dynamic));
			Assert.That(model.Diagnostics.Any(d => d.Code == "dynamic-editor"), Is.True);
		}

		[Test]
		public void Import_UnknownEditor_RaisesWarningDiagnostic()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Weird'>
  <part ref='WeirdEditor'/>
</layout>");

			Assert.That(model.Roots[0].EditorClassification, Is.EqualTo(EditorClassification.Unknown));
			var diag = model.Diagnostics.Single(d => d.Code == "unknown-editor");
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Warning));
			Assert.That(diag.NodePath, Is.EqualTo("LexEntry/Weird/#0"));
		}

		[Test]
		public void Import_ObsoleteEditor_RaisesErrorDiagnostic()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Old'>
  <part ref='ObsoleteEditor'/>
</layout>");

			Assert.That(model.Roots[0].EditorClassification, Is.EqualTo(EditorClassification.Obsolete));
			Assert.That(model.Diagnostics.Single(d => d.Code == "obsolete-editor").Severity,
				Is.EqualTo(ViewDiagnosticSeverity.Error));
		}

		[Test]
		public void Import_UnresolvedPart_RaisesErrorDiagnostic_AndDoesNotThrow()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Broken'>
  <part ref='DoesNotExist'/>
</layout>");

			Assert.That(model.Roots, Is.Empty);
			Assert.That(model.Diagnostics.Single().Code, Is.EqualTo("unresolved-part"));
		}
	}

	[TestFixture]
	public class ViewDefinitionCompilerTests
	{
		private const string PartsXml =
			"<PartInventory><bin>" +
			"<part id='LexEntry-Detail-CitationForm'><slice label='CitationForm' editor='multistring' field='CitationForm' ws='vernacular'/></part>" +
			"</bin></PartInventory>";

		private static ViewDefinitionSourceSnapshot Snapshot(string layoutName, string partsXml = PartsXml)
			=> new ViewDefinitionSourceSnapshot(
				"LexEntry",
				"detail",
				$"<layout class='LexEntry' type='detail' name='{layoutName}'><part ref='CitationForm'/></layout>",
				partsXml);

		[Test]
		public void Compile_CachesByFingerprint_ReturnsSameInstance()
		{
			var compiler = new ViewDefinitionCompiler();
			var snap = Snapshot("CfOnly");

			var a = compiler.Compile(snap);
			var b = compiler.Compile(Snapshot("CfOnly"));

			Assert.That(ReferenceEquals(a, b), Is.True, "identical source should hit the cache");
			Assert.That(compiler.Cache.Count, Is.EqualTo(1));
		}

		[Test]
		public void Invalidate_ForcesRecompile()
		{
			var compiler = new ViewDefinitionCompiler();
			var snap = Snapshot("CfOnly");
			var a = compiler.Compile(snap);

			compiler.Cache.Invalidate(snap.ToKey());
			var b = compiler.Compile(snap);

			Assert.That(ReferenceEquals(a, b), Is.False, "after invalidation a fresh instance is compiled");
		}

		[Test]
		public void DifferentSource_ProducesDifferentKey_AndRecompiles()
		{
			var compiler = new ViewDefinitionCompiler();
			compiler.Compile(Snapshot("CfOnly"));
			compiler.Compile(Snapshot("CfOther"));

			Assert.That(compiler.Cache.Count, Is.EqualTo(2));
			Assert.That(Snapshot("CfOnly").ToKey(), Is.Not.EqualTo(Snapshot("CfOther").ToKey()));
		}

		[Test]
		public void SameSource_ProducesEqualKeys()
		{
			Assert.That(Snapshot("CfOnly").ToKey(), Is.EqualTo(Snapshot("CfOnly").ToKey()));
			Assert.That(Snapshot("CfOnly").ToKey().GetHashCode(), Is.EqualTo(Snapshot("CfOnly").ToKey().GetHashCode()));
		}

		[Test]
		public async System.Threading.Tasks.Task CompileAsync_MatchesSync_OverImmutableSnapshot()
		{
			var compiler = new ViewDefinitionCompiler();
			var snap = Snapshot("CfOnly");

			var sync = compiler.Compile(snap);
			var async = await compiler.CompileAsync(Snapshot("CfOnly"), CancellationToken.None);

			Assert.That(async.ToSnapshot(), Is.EqualTo(sync.ToSnapshot()));
		}

		[Test]
		public void CompileAsync_HonorsCancellation()
		{
			var compiler = new ViewDefinitionCompiler();
			using (var cts = new CancellationTokenSource())
			{
				cts.Cancel();
				Assert.That(async () => await compiler.CompileAsync(Snapshot("CfOnly"), cts.Token),
					Throws.InstanceOf<System.OperationCanceledException>());
			}
		}
	}
}
