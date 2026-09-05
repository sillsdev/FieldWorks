// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
  <part id='LexEntry-Detail-ExtendedWs'>
    <slice label='Form' editor='multistring' field='CitationForm' ws='all pronunciation'
      optionalWs='all vernacular' forceIncludeEnglish='true'/>
  </part>
  <part id='LexEntry-Detail-SelectedObject'>
    <obj field='LexemeForm' layout='ContentLayout' layoutChoiceField='MorphType'/>
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
		public void Import_ObjectLayoutAndChoiceFieldMatchLegacyPrecedence()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='T'>
  <part ref='SelectedObject' param='CallerLayout'/>
</layout>");

			var node = model.Roots.Single();
			Assert.That(node.TargetLayout, Is.EqualTo("ContentLayout"));
			Assert.That(node.LayoutChoiceField, Is.EqualTo("MorphType"));
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
			// A jtview slice's param/layout names the nested layout to compose inline; it must ride
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
			// A per-field visibleWritingSystems override (on the part ref) restricts the displayed
			// writing systems. The ordered specs ride the node for the composer to intersect with the set.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='PFW'>
  <part ref='PerFieldWs' visibleWritingSystems='fr,en'/>
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
		public void Import_WritingSystemOptions_CaptureOptionalSetAndEnglishFlag()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='PFW'>
  <part ref='ExtendedWs'/>
</layout>");

			var field = model.Roots.Single();
			Assert.That(field.OptionalWritingSystem, Is.EqualTo("all vernacular"));
			Assert.That(field.ForceIncludeEnglish, Is.True);
			Assert.That(model.Diagnostics, Is.Empty);
		}

		[Test]
		public void Import_EmptyVisibleWritingSystems_PreservesExplicitEmptyOverride()
		{
			var model = Import(@"<layout class='LexEntry' type='detail' name='Normal'>
  <part ref='PerFieldWs' visibleWritingSystems=''/>
</layout>");

			Assert.That(model.Roots[0].VisibleWritingSystems, Is.EqualTo(new[] { string.Empty }));
		}

		[Test]
		public void Import_SpaceSeparatedVisibleWritingSystems_RemainsOneLegacyToken()
		{
			var model = Import(@"<layout class='LexEntry' type='detail' name='Normal'>
  <part ref='PerFieldWs' visibleWritingSystems='fr en'/>
</layout>");

			Assert.That(model.Roots[0].VisibleWritingSystems, Is.EqualTo(new[] { "fr en" }));
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
		public void Import_UnresolvedPart_RaisesErrorDiagnostic_AndOmitsTheCaller()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Broken'>
  <part ref='DoesNotExist'/>
</layout>");

			Assert.That(model.Roots, Is.Empty, "DataTree omits an unresolved part");
			Assert.That(model.Diagnostics.Single().Code, Is.EqualTo("unresolved-part"));
		}

		[Test]
		public void Import_ContainerConstructsDataTreeIgnores_AreOmittedWithDiagnostics()
		{
			// DataTree.ProcessPartRefNode only handles sublayout/indent/part, so a <generate>
			// or an unknown element in a detail layout renders nothing in WinForms.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Ignored'>
  <generate class='LexEntry' fieldType='custom'/>
  <mystery/>
</layout>");

			Assert.That(model.Roots, Is.Empty, "an Unsupported row here would be a divergence");
			var generated = model.Diagnostics.Single(d => d.Code == "generated-content-dropped");
			var unknown = model.Diagnostics.Single(d => d.Code == "unknown-container-element");
			Assert.That(generated.NodePath, Is.Not.EqualTo(unknown.NodePath),
				"omitted siblings must not share a diagnostic path");
		}

		[Test]
		public void Import_PartWithoutRef_IsOmittedWithDiagnostic()
		{
			// DataTree reads the ref with GetMandatoryAttributeValue and throws, so no
			// row renders.
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Orphan'>
  <part label='Orphan'/>
</layout>");

			Assert.That(model.Roots, Is.Empty);
			Assert.That(model.Diagnostics.Single().Code, Is.EqualTo("part-without-ref"));
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
		public void Snapshot_DistinguishesAbsentAndEmptyChoiceGuid()
		{
			var withoutChoice = new ViewDefinitionModel("RnGenericRec", "Normal", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);
			var emptyChoice = new ViewDefinitionModel("RnGenericRec", "Normal", "detail",
				new ViewNode[0], new ViewDiagnostic[0], string.Empty);

			Assert.That(emptyChoice.ToSnapshot(), Is.Not.EqualTo(withoutChoice.ToSnapshot()));
			Assert.That(emptyChoice.ToSnapshot(), Does.Contain(" choice="));
		}

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

		// A source hands every snapshot the same parts string, so its hash is computed once per
		// instance; the fingerprint still keys identical content identically across instances.
		[Test]
		public void Fingerprint_HashesSharedPartsSourceOncePerInstance_AndStillKeysByContent()
		{
			var shared = new string(PartsXml.ToCharArray());
			var before = ViewDefinitionSourceSnapshot.PartsHashComputeCount;

			var first = Snapshot("CfOnly", shared).ComputeFingerprint();
			var other = Snapshot("CfOther", shared).ComputeFingerprint();

			Assert.That(ViewDefinitionSourceSnapshot.PartsHashComputeCount - before, Is.EqualTo(1),
				"two snapshots over one parts instance hash the parts once");
			Assert.That(other, Is.Not.EqualTo(first), "the layout still tells the fingerprints apart");
			Assert.That(Snapshot("CfOnly", new string(PartsXml.ToCharArray())).ComputeFingerprint(),
				Is.EqualTo(first), "identical parts content in another instance fingerprints identically");
			Assert.That(Snapshot("CfOnly", PartsXml + " ").ComputeFingerprint(), Is.Not.EqualTo(first),
				"changed parts content still changes the fingerprint");
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

		[Test]
		public void Snapshot_PreservesRequestedIdentityWhenResolvedLayoutFallsBack()
		{
			var snapshot = new ViewDefinitionSourceSnapshot(
				"LexEntry", "detail",
				"<layout class='LexEntry' type='detail' name='default' choiceGuid='resolved'/>",
				PartsXml, requestedLayoutName: "Requested", requestedChoiceGuid: null);

			Assert.That(snapshot.RequestedLayoutName, Is.EqualTo("Requested"));
			Assert.That(snapshot.RequestedChoiceGuid, Is.Null);
			Assert.That(snapshot.ResolvedLayoutName, Is.EqualTo("default"));
			Assert.That(snapshot.ResolvedChoiceGuid, Is.EqualTo("resolved"));

			var model = new ViewDefinitionCompiler().Compile(snapshot);
			Assert.That(model.RequestedLayoutName, Is.EqualTo("Requested"));
			Assert.That(model.RequestedChoiceGuid, Is.Null);
			Assert.That(model.ResolvedLayoutName, Is.EqualTo("default"));
			Assert.That(model.ResolvedChoiceGuid, Is.EqualTo("resolved"));
		}

		[Test]
		public void Snapshot_BaseClassMapLookupIsCaseInsensitiveAfterCopy()
		{
			var snapshot = new ViewDefinitionSourceSnapshot(
				"derivedclass", "detail",
				"<layout class='derivedclass' type='detail' name='Normal'><part ref='CitationForm'/></layout>",
				"<PartInventory><bin><part id='BaseClass-Detail-CitationForm'>"
				+ "<slice field='CitationForm'/></part></bin></PartInventory>",
				new Dictionary<string, string> { ["DerivedClass"] = "BaseClass" });

			var model = new ViewDefinitionCompiler().Compile(snapshot);

			Assert.That(model.Roots, Has.Count.EqualTo(1));
			Assert.That(model.Roots[0].Field, Is.EqualTo("CitationForm"));
		}

		[Test]
		public void Compile_RetainsConcreteRequestedIdentityWhenXmlUsesBaseLayout()
		{
			var snapshot = new ViewDefinitionSourceSnapshot(
				"ConcreteClass", "detail",
				"<layout class='BaseClass' type='detail' name='default' choiceGuid='resolved'/>",
				"<PartInventory/>", requestedLayoutName: "Requested", requestedChoiceGuid: "requested");

			var model = new ViewDefinitionCompiler().Compile(snapshot);

			Assert.That(model.RequestedIdentity,
				Is.EqualTo(new ViewDefinitionIdentity("ConcreteClass", "detail", "Requested", "requested")));
			Assert.That(model.ResolvedIdentity,
				Is.EqualTo(new ViewDefinitionIdentity("BaseClass", "detail", "default", "resolved")));
		}

		[Test]
		public void CacheKey_UsesCaseInsensitiveFourPartIdentityButDistinguishesNullAndEmptyChoice()
		{
			var upper = new ViewDefinitionCacheKey("LexEntry", "Normal", "Detail", null, "fingerprint");
			var mixed = new ViewDefinitionCacheKey("lexentry", "normal", "detail", null, "fingerprint");
			var empty = new ViewDefinitionCacheKey("lexentry", "normal", "detail", string.Empty, "fingerprint");

			Assert.That(upper, Is.EqualTo(mixed));
			Assert.That(upper.GetHashCode(), Is.EqualTo(mixed.GetHashCode()));
			Assert.That(upper, Is.Not.EqualTo(empty));
		}

		[Test]
		public void CacheKey_FingerprintRetainsSourceXmlDifferences()
		{
			var first = Snapshot("Normal").ToKey();
			var changed = new ViewDefinitionSourceSnapshot("LexEntry", "detail",
				"<layout class='LexEntry' type='detail' name='Normal'><part ref='citationform'/></layout>",
				PartsXml).ToKey();

			Assert.That(first, Is.Not.EqualTo(changed));
		}

		[Test]
		public void Snapshot_LazilyCachesFingerprintWhenOriginalBaseClassMapChanges()
		{
			var baseClassMap = new Dictionary<string, string>
			{
				["LexEntry"] = "LexObject"
			};
			var snapshot = new ViewDefinitionSourceSnapshot("LexEntry", "detail",
				"<layout class='LexEntry' type='detail' name='Normal'/>", PartsXml, baseClassMap);
			var fingerprintField = typeof(ViewDefinitionSourceSnapshot).GetField("_fingerprint",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(fingerprintField, Is.Not.Null);
			var lazyFingerprint = (System.Lazy<string>)fingerprintField.GetValue(snapshot);
			Assert.That(lazyFingerprint.IsValueCreated, Is.False);

			var first = snapshot.ComputeFingerprint();
			Assert.That(lazyFingerprint.IsValueCreated, Is.True);
			Assert.That(ReferenceEquals(first, snapshot.ComputeFingerprint()), Is.True);
			baseClassMap["LexEntry"] = "ChangedBase";

			Assert.That(snapshot.ComputeFingerprint(), Is.EqualTo(first));
			Assert.That(snapshot.ToKey().SourceFingerprint, Is.EqualTo(first));
		}

		[Test]
		public void Snapshot_CustomFieldsExpandedMetadataDefaultsFalseAndCanBeSet()
		{
			var unexpanded = Snapshot("Unexpanded");
			var expanded = new ViewDefinitionSourceSnapshot(
				"LexEntry", "detail", unexpanded.LayoutXml, unexpanded.PartsXml, null, null, null, true);

			Assert.That(unexpanded.CustomFieldsExpanded, Is.False);
			Assert.That(expanded.CustomFieldsExpanded, Is.True);
		}

		[Test]
		public void Cache_EvictsOldestEntryAtConfiguredCapacity()
		{
			var cache = new ViewDefinitionCache(2);
			var firstKey = new ViewDefinitionCacheKey("LexEntry", "First", "detail", "first");
			var secondKey = new ViewDefinitionCacheKey("LexEntry", "Second", "detail", "second");
			var thirdKey = new ViewDefinitionCacheKey("LexEntry", "Third", "detail", "third");
			var first = new ViewDefinitionModel("LexEntry", "First", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);
			var second = new ViewDefinitionModel("LexEntry", "Second", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);
			var third = new ViewDefinitionModel("LexEntry", "Third", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);

			Assert.That(cache.Capacity, Is.EqualTo(2));
			Assert.That(cache.GetOrAdd(firstKey, () => first), Is.SameAs(first));
			Assert.That(cache.GetOrAdd(secondKey, () => second), Is.SameAs(second));
			Assert.That(cache.GetOrAdd(thirdKey, () => third), Is.SameAs(third));

			Assert.That(cache.Count, Is.EqualTo(2));
			Assert.That(cache.TryGet(firstKey, out _), Is.False);
			Assert.That(cache.TryGet(secondKey, out var cachedSecond), Is.True);
			Assert.That(cachedSecond, Is.SameAs(second));
			Assert.That(cache.TryGet(thirdKey, out var cachedThird), Is.True);
			Assert.That(cachedThird, Is.SameAs(third));
		}

		[Test]
		public void Cache_GetOrAdd_ConcurrentCallersShareOneCreatedValue()
		{
			var cache = new ViewDefinitionCache(4);
			var key = new ViewDefinitionCacheKey("LexEntry", "Concurrent", "detail", "source");
			var calls = 0;
			var results = new ViewDefinitionModel[8];
			using (var start = new ManualResetEventSlim(false))
			{
				var tasks = Enumerable.Range(0, results.Length).Select(index =>
					System.Threading.Tasks.Task.Run(() =>
					{
						start.Wait();
						results[index] = cache.GetOrAdd(key, () =>
						{
							Interlocked.Increment(ref calls);
							Thread.Sleep(10);
							return new ViewDefinitionModel("LexEntry", "Concurrent", "detail",
								new ViewNode[0], new ViewDiagnostic[0]);
						});
					})).ToArray();
				start.Set();
				System.Threading.Tasks.Task.WaitAll(tasks);
			}

			Assert.That(calls, Is.EqualTo(1));
			Assert.That(results.All(result => ReferenceEquals(result, results[0])), Is.True);
		}

		[Test]
		public void Cache_GetOrAdd_DoesNotHoldGlobalLockWhileCreatingValue()
		{
			var cache = new ViewDefinitionCache(2);
			var slowKey = new ViewDefinitionCacheKey("LexEntry", "Slow", "detail", "slow");
			var fastKey = new ViewDefinitionCacheKey("LexEntry", "Fast", "detail", "fast");
			var slow = new ViewDefinitionModel("LexEntry", "Slow", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);
			var fast = new ViewDefinitionModel("LexEntry", "Fast", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);
			using (var slowStarted = new ManualResetEventSlim(false))
			using (var releaseSlow = new ManualResetEventSlim(false))
			{
				var slowTask = System.Threading.Tasks.Task.Run(() => cache.GetOrAdd(slowKey, () =>
				{
					slowStarted.Set();
					releaseSlow.Wait();
					return slow;
				}));
				try
				{
					Assert.That(slowStarted.Wait(System.TimeSpan.FromSeconds(1)), Is.True);
					var fastTask = System.Threading.Tasks.Task.Run(() => cache.GetOrAdd(fastKey, () => fast));
					Assert.That(fastTask.Wait(System.TimeSpan.FromSeconds(1)), Is.True);
					Assert.That(fastTask.Result, Is.SameAs(fast));
				}
				finally
				{
					releaseSlow.Set();
					Assert.That(slowTask.Wait(System.TimeSpan.FromSeconds(1)), Is.True);
				}
			}
		}

		[Test]
		public void Cache_GetOrAdd_RemovesFailedLazySoTheNextCallCanRetry()
		{
			var cache = new ViewDefinitionCache(2);
			var key = new ViewDefinitionCacheKey("LexEntry", "Retry", "detail", "retry");
			var calls = 0;
			var model = new ViewDefinitionModel("LexEntry", "Retry", "detail",
				new ViewNode[0], new ViewDiagnostic[0]);

			Assert.That(() => cache.GetOrAdd(key, () =>
			{
				if (Interlocked.Increment(ref calls) == 1)
					throw new System.InvalidOperationException("first attempt");
				return model;
			}), Throws.TypeOf<System.InvalidOperationException>());

			Assert.That(cache.GetOrAdd(key, () =>
			{
				Interlocked.Increment(ref calls);
				return model;
			}), Is.SameAs(model));
			Assert.That(calls, Is.EqualTo(2));
		}
	}
}
