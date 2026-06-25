// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D5 — the burn-down is enforced by tests, not intentions:
	/// every dynamically loaded custom slice the lexeme editor's part files actually use must be
	/// classified in exactly one migration lane (plugin-routed / companion-designated /
	/// launcher-routed / explicitly deferred WITH the gate it rides), and the companion-strip
	/// designated set may only shrink. A new custom slice appearing in the layouts fails the
	/// census until a developer consciously classifies it.
	/// </summary>
	[TestFixture]
	public class LexemeEditorBurnDownTests
	{
		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null, "could not locate the repo root from the test directory");
			return dir.FullName;
		}

		/// <summary>
		/// The lexeme editor's custom-slice census (the table in winforms-free-lexeme-editor.md):
		/// every dynamically loaded editor class in LexEntryParts.xml + LexSenseParts.xml. The
		/// DynamicLoader signature is the class= + assemblyPath= attribute pair — a plain class=
		/// attribute is a model-class bin/part declaration, not an editor. Non-UI handlers
		/// (LexEntryChangeHandler and anything else ending ChangeHandler) have no editor to
		/// migrate and are excluded.
		/// </summary>
		private static IReadOnlyList<string> LexemeEditorCustomSliceCensus()
		{
			var partsDir = Path.Combine(RepoRoot(), "DistFiles", "Language Explorer", "Configuration", "Parts");
			var classes = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var file in new[] { "LexEntryParts.xml", "LexSenseParts.xml" })
			{
				var path = Path.Combine(partsDir, file);
				Assert.That(File.Exists(path), Is.True, $"census source '{path}' must exist");
				foreach (var element in XDocument.Load(path).Descendants())
				{
					var className = (string)element.Attribute("class");
					if (string.IsNullOrEmpty(className) || element.Attribute("assemblyPath") == null)
						continue;
					if (className.EndsWith("ChangeHandler", StringComparison.Ordinal))
						continue;
					classes.Add(className);
				}
			}
			return classes.ToList();
		}

		[Test]
		public void Census_EveryCustomSliceClass_IsClassifiedInExactlyOneLane()
		{
			var census = LexemeEditorCustomSliceCensus();
			Assert.That(census, Is.Not.Empty, "the lexeme editor part files ship custom slices");

			foreach (var cls in census)
			{
				var lanes = new List<string>();
				// D4: a launcher-routed class is DELIVERED through the plugin registry (its
				// LauncherRegionPlugin claims it), so the registry claim plus the launcher list
				// count as the one "LauncherRouted" lane, not two lanes.
				var launcherRouted = LexemeEditorBurnDown.LauncherRoutedClassNames.ContainsKey(cls);
				if (RegionEditorPluginRegistry.Default.Resolve(cls) != null && !launcherRouted)
					lanes.Add("PluginRouted");
				if (AvaloniaCompanionSlices.DesignatedClassNames.Contains(cls))
					lanes.Add("CompanionDesignated");
				if (launcherRouted)
					lanes.Add("LauncherRouted");
				if (LexemeEditorBurnDown.ExplicitlyDeferredClassNames.ContainsKey(cls))
					lanes.Add("ExplicitlyDeferred");
				if (LexemeEditorBurnDown.LaneAbsorbedClassNames.ContainsKey(cls))
					lanes.Add("LaneAbsorbed");

				Assert.That(lanes.Count, Is.EqualTo(1),
					$"'{cls}' must be classified in exactly one burn-down lane, found "
					+ (lanes.Count == 0 ? "none" : string.Join(" + ", lanes))
					+ ". A new custom slice in the lexeme-editor layouts must be consciously classified "
					+ "before it ships: register an IRegionEditorPlugin for it, designate it for the "
					+ "WinForms companion strip, list it launcher-routed (wave 4), or defer it explicitly "
					+ "WITH the gate it rides (winforms-free-lexeme-editor.md D1/D5).");
			}
		}

		[Test]
		public void Census_FindsTheMeasuredProblemClasses()
		{
			// The census parser must keep seeing the classes the decision doc measured — if the
			// attribute shapes in the part files ever change, the census must change with them
			// rather than silently going empty (which would make every class "classified").
			var census = LexemeEditorCustomSliceCensus();
			Assert.That(census, Does.Contain(AvaloniaCompanionSlices.MessageSliceClassName));
			Assert.That(census, Does.Contain("SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice"));
			Assert.That(census, Has.None.EndsWith("ChangeHandler"),
				"non-UI change handlers are not editors and stay out of the census");
		}

		[Test]
		public void CompanionDesignatedSet_IsEmpty_AndMayOnlyShrink()
		{
			// winforms-free-lexeme-editor.md D5: the companion-strip designated set may only SHRINK
			// (a class graduates unsupported → companion → plugin, never the other way). Wave 2
			// (ChorusNotesPlugin, D2) emptied it: the Messages slice graduated to the native
			// Avalonia plugin, and the mechanism stays as the documented coexistence lane for
			// future tools' WinForms-only slices. If this assertion is failing because the set
			// GREW, that is a new WinForms dependency inside the pane — do not edit this test
			// without a written justification in the change doc.
			Assert.That(AvaloniaCompanionSlices.DesignatedClassNames, Is.Empty);
		}

		[Test]
		public void ExplicitlyDeferredClasses_AreEmpty_EveryClassIsActivelyClassified()
		{
			// D5: deferral is only legitimate with the gate it rides spelled out. The set is now EMPTY:
			// Wave 4 (D4) graduated AudioVisualSlice into the launcher lane, Wave 3 absorbed the ghost
			// and multislice relation lanes into the composer, and ReversalIndexEntrySlice graduated to
			// a native Avalonia plugin (ReversalIndexEntryPlugin) — the sense's reversal-entry forms
			// compose as an editable multi-WS text field through the D1 plugin lane. Every census class
			// is now actively classified (plugin / companion / launcher / lane-absorbed), none deferred.
			Assert.That(LexemeEditorBurnDown.ExplicitlyDeferredClassNames, Is.Empty);
			Assert.That(LexemeEditorBurnDown.ExplicitlyDeferredClassNames.Values,
				Has.All.Not.Empty, "a deferral without a citation is a forgotten class, not a decision");
		}

		[Test]
		public void LaneAbsorbedClasses_AreTheD3ReferenceVectorLane_WithCitations()
		{
			// Wave 3 (D3): EntrySequenceReferenceSlice graduated out of ExplicitlyDeferred — its
			// nodes now compose as editable ReferenceVector rows with type-ahead lexicon search
			// (no plugin: the composer recognizes them by metadata + the legacy class identity).
			// GhostLexRefSlice joins the same absorbed family: empty Components / Variant of rows are
			// search-backed reference vectors whose first add creates the missing LexEntryRef.
			// LexReferenceMultiSlice joined once the composer walked ILexReference objects directly
			// and emitted one Avalonia row per relation with forward/reverse label semantics.
			var expected = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice", "D3 ReferenceVector lane" },
				{ "SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice", "D3 ghost reference-vector lane" },
				{ "SIL.FieldWorks.XWorks.LexEd.LexReferenceMultiSlice", "D3 lexical relation lane" }
			};
			Assert.That(LexemeEditorBurnDown.LaneAbsorbedClassNames, Is.EquivalentTo(expected));
			Assert.That(LexemeEditorBurnDown.LaneAbsorbedClassNames.Values, Has.All.Not.Empty,
				"a lane absorption without a citation is unverifiable");
		}

		[Test]
		public void LauncherRoutedClasses_AreTheD4LauncherLane_WithCitations()
		{
			// Wave 4 (D4): the dialog-launcher slices render as an Avalonia value row + "..."
			// button calling the host's ILegacyDialogLauncher seam. AudioVisualSlice graduated
			// here from ExplicitlyDeferred; the MSA/phonological launchers live in MSA/FsFeatStruc
			// part files beyond the LexEntry/LexSense census — registered anyway, forward-looking.
			var expected = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ DialogLauncherPlugins.MsaFeatureSliceClassName, "D4 launcher lane" },
				{ DialogLauncherPlugins.PhonologicalFeatureSliceClassName, "D4 launcher lane" },
				{ DialogLauncherPlugins.AudioVisualSliceClassName, "D4 launcher lane" }
			};
			Assert.That(LexemeEditorBurnDown.LauncherRoutedClassNames, Is.EquivalentTo(expected));
			Assert.That(LexemeEditorBurnDown.LauncherRoutedClassNames.Values, Has.All.Not.Empty,
				"a launcher routing without a citation is unverifiable");
		}

		[Test]
		public void LauncherRoutedClasses_AreEachClaimedByALauncherPlugin()
		{
			// The launcher lane is delivered through the D1 plugin registry: every launcher-routed
			// class must be claimed by a LauncherRegionPlugin in the default registry, or the row
			// would silently fall back to the unsupported lane while the burn-down claims victory.
			foreach (var cls in LexemeEditorBurnDown.LauncherRoutedClassNames.Keys)
			{
				Assert.That(RegionEditorPluginRegistry.Default.Resolve(cls),
					Is.InstanceOf<LauncherRegionPlugin>(),
					$"'{cls}' is launcher-routed and must be claimed by a LauncherRegionPlugin");
			}
		}

		private sealed class StubPlugin : IRegionEditorPlugin
		{
			public StubPlugin(string legacyClassName)
			{
				LegacyClassName = legacyClassName;
			}

			public string LegacyClassName { get; }

			public Avalonia.Controls.Control BuildControl(RegionEditorBuildContext context) => null;
		}

		[Test]
		public void Registry_RegisterAndResolve_RoundTrips_AndUnknownReturnsNull()
		{
			var registry = new RegionEditorPluginRegistry();
			Assert.That(registry.Resolve("No.Such.Class"), Is.Null, "an unclaimed class resolves null");
			Assert.That(registry.Resolve(null), Is.Null);

			var plugin = new StubPlugin("SIL.FieldWorks.XWorks.LexEd.SomeSlice");
			registry.Register(plugin);
			Assert.That(registry.Resolve(plugin.LegacyClassName), Is.SameAs(plugin));
			Assert.That(registry.Resolve("No.Such.Class"), Is.Null);
		}

		[Test]
		public void Registry_RejectsInvalidAndDuplicateRegistrations()
		{
			var registry = new RegionEditorPluginRegistry();
			Assert.That(() => registry.Register(null), Throws.ArgumentNullException);
			Assert.That(() => registry.Register(new StubPlugin(null)), Throws.ArgumentException,
				"a plugin without a legacy class identity cannot be resolved by the composer");

			registry.Register(new StubPlugin("A.Class"));
			Assert.That(() => registry.Register(new StubPlugin("A.Class")), Throws.ArgumentException,
				"a legacy class has exactly one owner (single resolution, D1)");
		}

		[Test]
		public void DefaultRegistry_BuiltinsAreExactlyTheLandedWaves()
		{
			// The burn-down measured: wave 2 (D2) promoted the Messages slice from
			// CompanionDesignated to PluginRouted (ChorusNotesPlugin, the native notes bar over
			// LibChorus); wave 3 (D3) was a composer lane, no plugin; wave 4 (D4) added the three
			// dialog-launcher plugins; the reversal-entries editor (ReversalIndexEntryPlugin) graduated
			// the last deferred class to a native Avalonia editable multi-WS text field. The census test
			// above keeps every class in exactly one lane.
			// PHASE-1 FOLLOW-UP PRs: the avalonia-interlinear-editor (InterlinearSlicePlugin) and the
			// avalonia-rule-formula-editor family (five plugins: the three rule-formula grids plus the
			// environment-string and Basic IPA symbol editors) ship in their own follow-up PRs. Each
			// follow-up restores its plugin registration in RegionEditorPlugins.RegisterBuiltins and adds
			// its class name(s) back to this census. The base registry is exactly the always-on lanes below.
			Assert.That(RegionEditorPluginRegistry.Default.RegisteredClassNames,
				Is.EquivalentTo(new[]
				{
					AvaloniaCompanionSlices.MessageSliceClassName,
					ReversalIndexEntryPlugin.ReversalIndexEntrySliceClassName,
					DialogLauncherPlugins.MsaFeatureSliceClassName,
					DialogLauncherPlugins.PhonologicalFeatureSliceClassName,
					DialogLauncherPlugins.AudioVisualSliceClassName
				}));
			Assert.That(RegionEditorPluginRegistry.Default.Resolve(AvaloniaCompanionSlices.MessageSliceClassName),
				Is.InstanceOf<ChorusNotesPlugin>());
			Assert.That(RegionEditorPluginRegistry.Default.Resolve(ReversalIndexEntryPlugin.ReversalIndexEntrySliceClassName),
				Is.InstanceOf<ReversalIndexEntryPlugin>());
		}
	}

	/// <summary>
	/// winforms-free-lexeme-editor.md D1 — the composer's resolution order for a custom slice is
	/// plugin registry → companion strip (designated set) → unsupported row. A plugin claiming the
	/// Messages slice's legacy class therefore wins over the companion lane: the node composes as a
	/// RegionFieldKind.Custom row carrying the plugin's deferred control factory, and the
	/// companion-promotion list no longer sees it.
	/// </summary>
	[TestFixture]
	public class RegionEditorPluginResolutionOrderTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			});
		}

		private sealed class FakeMessagesPlugin : IRegionEditorPlugin
		{
			public int BuildCalls;
			public ICmObject LastObject;
			public ViewNode LastNode;
			public IRegionEditContext LastEditContext;
			public LcmCache LastCache;

			public string LegacyClassName => AvaloniaCompanionSlices.MessageSliceClassName;

			public Avalonia.Controls.Control BuildControl(RegionEditorBuildContext context)
			{
				BuildCalls++;
				LastObject = context.Target;
				LastNode = context.Node;
				LastEditContext = context.EditContext;
				LastCache = context.Cache;
				return null; // never rendered in this fixture; the view's guard lane covers null
			}
		}

		[Test]
		public void Compose_PluginClaim_WinsOverTheCompanionDesignatedSet()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeMessagesPlugin();
			registry.Register(plugin);

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, plugins: registry);

			var customRows = composed.Model.Fields.Where(f => f.Kind == RegionFieldKind.Custom).ToList();
			Assert.That(customRows.Count, Is.EqualTo(1),
				"the claimed Messages node composes as exactly one Custom row");
			var row = customRows[0];
			Assert.That(row.Label, Is.EqualTo("Messages"), "the plugin row keeps the slice label");
			Assert.That(row.MenuId, Is.EqualTo("mnuDataTree-Help"),
				"the plugin row carries the layout's slice menu binding");
			Assert.That(row.ObjectHvo, Is.EqualTo(m_entry.Hvo), "field='Self' binds the entry itself");
			Assert.That(row.ControlFactory, Is.Not.Null, "the row carries the plugin's control factory");

			Assert.That(composed.CustomEditorFields.Select(f => f.ClassName),
				Has.No.Member(AvaloniaCompanionSlices.MessageSliceClassName),
				"a plugin-claimed class never reaches the companion lane (D1 resolution order)");
			Assert.That(plugin.BuildCalls, Is.EqualTo(0),
				"compose defers control building to the view (factory, not control)");
		}

		[Test]
		public void PluginRowFactory_ClosesOverObjectNodeCacheAndTheComposedEditContext()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeMessagesPlugin();
			registry.Register(plugin);

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, plugins: registry);
			var row = composed.Model.Fields.Single(f => f.Kind == RegionFieldKind.Custom);

			row.ControlFactory();

			Assert.That(plugin.BuildCalls, Is.EqualTo(1));
			Assert.That(plugin.LastObject?.Hvo, Is.EqualTo(m_entry.Hvo));
			Assert.That(plugin.LastNode?.CustomEditorClass,
				Is.EqualTo(AvaloniaCompanionSlices.MessageSliceClassName));
			Assert.That(plugin.LastCache, Is.SameAs(Cache));
			Assert.That(plugin.LastEditContext, Is.SameAs(composed.EditContext),
				"the deferred accessor resolves to the region's own composed edit context");
		}

		[Test]
		public void Compose_WithoutAPluginClaim_KeepsTheCompanionLane()
		{
			// Second slot in the D1 order: an unclaimed designated class still rides the companion
			// strip (and an unclaimed, undesignated class keeps its unsupported row).
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache,
				plugins: new RegionEditorPluginRegistry());

			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Custom), Is.False);
			Assert.That(composed.CustomEditorFields.Select(f => f.ClassName),
				Has.Member(AvaloniaCompanionSlices.MessageSliceClassName));
		}
	}
}
