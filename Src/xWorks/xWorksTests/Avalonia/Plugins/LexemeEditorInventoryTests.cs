// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The trimmed lexeme editor's custom-slice census. The region has exactly ONE native-conversion
	/// route (the <see cref="IRegionEditorPlugin"/> contract with <see cref="ReversalIndexEntryPlugin"/>)
	/// plus the composer's reference-vector absorption; every OTHER dynamically loaded custom slice
	/// composes as the labeled Unsupported worklist row. This census pins that only ReversalIndexEntrySlice
	/// is plugin-claimed, that the composer-absorbed classes compose as reference vectors, and that the
	/// feature-launcher slices plus the Chorus notes bar are unclaimed (so they render Unsupported).
	/// </summary>
	[TestFixture]
	public class LexemeEditorInventoryTests
	{
		// The legacy class identities the census parser and the route assertions reference. Held here
		// so the census is self-contained.
		private const string MessageSliceClassName = "SIL.FieldWorks.XWorks.LexEd.MessageSlice";
		private const string MsaFeatureSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.MsaInflectionFeatureListDlgLauncherSlice";
		private const string PhonologicalFeatureSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.PhonologicalFeatureListDlgLauncherSlice";
		private const string AudioVisualSliceClassName =
			"SIL.FieldWorks.Common.Framework.DetailControls.AudioVisualSlice";
		private const string EntrySequenceSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice";
		private const string GhostLexRefSliceClassName = "SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice";
		private const string LexReferenceMultiSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.LexReferenceMultiSlice";

		// The classes the composer recognizes by legacy identity and absorbs as native ReferenceVector
		// rows (no plugin). Kept in one place so the census and the route tests agree.
		private static readonly string[] ComposerAbsorbedClassNames =
		{
			EntrySequenceSliceClassName, GhostLexRefSliceClassName, LexReferenceMultiSliceClassName
		};

		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null, "could not locate the repo root from the test directory");
			return dir.FullName;
		}

		/// <summary>
		/// The lexeme editor's custom-slice census: every dynamically loaded editor class in
		/// LexEntryParts.xml + LexSenseParts.xml. The DynamicLoader signature is the class= +
		/// assemblyPath= attribute pair — a plain class= attribute is a model-class bin/part declaration,
		/// not an editor. Non-UI handlers (anything ending ChangeHandler) have no editor to migrate and
		/// are excluded.
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
		public void Census_EveryCustomSliceClass_IsPluginRouted_ComposerAbsorbed_Or_Unsupported()
		{
			var census = LexemeEditorCustomSliceCensus();
			Assert.That(census, Is.Not.Empty, "the lexeme editor part files ship custom slices");

			foreach (var cls in census)
			{
				var pluginRouted = RegionEditorPluginRegistry.Default.Resolve(cls) != null;
				var composerAbsorbed = ComposerAbsorbedClassNames.Contains(cls, StringComparer.Ordinal);

				// A class is claimed by AT MOST one explicit route; everything else is the Unsupported
				// worklist (the default). Plugin and composer-absorbed are mutually exclusive.
				Assert.That(pluginRouted && composerAbsorbed, Is.False,
					$"'{cls}' cannot be both plugin-routed and composer-absorbed");
			}
		}

		[Test]
		public void Census_FindsTheMeasuredProblemClasses()
		{
			// The census parser must keep seeing these known classes — if the
			// attribute shapes in the part files ever change, the census must change with them rather
			// than silently going empty (which would make every class "classified").
			var census = LexemeEditorCustomSliceCensus();
			Assert.That(census, Does.Contain(MessageSliceClassName));
			Assert.That(census, Does.Contain(EntrySequenceSliceClassName));
			Assert.That(census, Has.None.EndsWith("ChangeHandler"),
				"non-UI change handlers are not editors and stay out of the census");
		}

		[Test]
		public void DefaultRegistry_BuiltinsAreExactlyTheReversalIndexEntryPlugin()
		{
			// The single native-conversion route. Every other custom slice not absorbed by a composer
			// route renders the Unsupported worklist row (no other native route exists).
			Assert.That(RegionEditorPluginRegistry.Default.RegisteredClassNames,
				Is.EquivalentTo(new[] { ReversalIndexEntryPlugin.ReversalIndexEntrySliceClassName }));
			Assert.That(RegionEditorPluginRegistry.Default.Resolve(ReversalIndexEntryPlugin.ReversalIndexEntrySliceClassName),
				Is.InstanceOf<ReversalIndexEntryPlugin>());
		}

		[Test]
		public void FormerlyLauncherRoutedClasses_AndTheChorusNotesBar_AreUnclaimed()
		{
			// The MSA/phonological feature launchers, the audio-visual media slice, and the Chorus notes
			// bar (MessageSlice) are not claimed by any plugin, so each composes as the labeled
			// Unsupported worklist row.
			foreach (var cls in new[]
			{
				MsaFeatureSliceClassName, PhonologicalFeatureSliceClassName,
				AudioVisualSliceClassName, MessageSliceClassName
			})
			{
				Assert.That(RegionEditorPluginRegistry.Default.Resolve(cls), Is.Null,
					$"'{cls}' is a dropped editor: nothing claims it, so it renders the Unsupported worklist row");
				Assert.That(ComposerAbsorbedClassNames, Has.None.EqualTo(cls),
					$"'{cls}' is not one of the D3 composer-absorbed reference-vector routes");
			}
		}

		[Test]
		public void ComposerAbsorbedClasses_AreNotPluginClaimed()
		{
			// The entry/sense reference-vector, ghost reference-vector, and lexical-relation slices
			// are recognized by the composer's own routing (no plugin) and compose as native
			// ReferenceVector rows, so the registry must NOT claim them.
			foreach (var cls in ComposerAbsorbedClassNames)
				Assert.That(RegionEditorPluginRegistry.Default.Resolve(cls), Is.Null,
					$"'{cls}' is composer-absorbed (D3), not plugin-routed");
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
	}

	/// <summary>
	/// The composer's resolution order for a custom slice is
	/// plugin registry → Unsupported row. A plugin claiming a slice's legacy class composes it as a
	/// DetailFieldKind.Custom row carrying the plugin's deferred control factory; an unclaimed custom
	/// slice composes as the labeled Unsupported worklist row.
	/// </summary>
	[TestFixture]
	public class RegionEditorPluginResolutionOrderTests : MemoryOnlyBackendProviderTestBase
	{
		private const string MessageSliceClassName = "SIL.FieldWorks.XWorks.LexEd.MessageSlice";
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
			public SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.ViewNode LastNode;
			public IDetailEditContext LastEditContext;
			public LcmCache LastCache;

			public string LegacyClassName => MessageSliceClassName;

			public Avalonia.Controls.Control BuildControl(RegionEditorBuildContext context)
			{
				BuildCalls++;
				LastObject = context.Target;
				LastNode = context.Node;
				LastEditContext = context.EditContext;
				LastCache = context.Cache;
				return null; // never rendered in this fixture; the view's null guard covers this
			}
		}

		[Test]
		public void Compose_UnclaimedCustomSlice_ComposesAsUnsupportedWorklistRow()
		{
			// No plugin claims the Chorus notes bar (MessageSlice), so the node composes as the labeled
			// Unsupported worklist row — never a Custom row,
			// never silently omitted.
			var composed = RegionComposer.Compose(m_entry, Cache,
				plugins: new RegionEditorPluginRegistry());

			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Custom), Is.False,
				"an unclaimed custom slice is not a Custom row");
			var messages = composed.Model.Fields.FirstOrDefault(f => f.Label == "Messages");
			Assert.That(messages, Is.Not.Null, "the Messages slice still composes a row (not silently dropped)");
			Assert.That(messages.Kind, Is.EqualTo(DetailFieldKind.Unsupported),
				"the unclaimed Messages slice renders the labeled Unsupported worklist row");
		}

		[Test]
		public void Compose_PluginClaim_ComposesTheClaimedNodeAsACustomRow()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeMessagesPlugin();
			registry.Register(plugin);

			var composed = RegionComposer.Compose(m_entry, Cache, plugins: registry);

			var customRows = composed.Model.Fields.Where(f => f.Kind == DetailFieldKind.Custom).ToList();
			Assert.That(customRows.Count, Is.EqualTo(1),
				"the claimed Messages node composes as exactly one Custom row");
			var row = customRows[0];
			Assert.That(row.Label, Is.EqualTo("Messages"), "the plugin row keeps the slice label");
			Assert.That(row.MenuId, Is.EqualTo("mnuDataTree-Help"),
				"the plugin row carries the layout's slice menu binding");
			Assert.That(row.ObjectHvo, Is.EqualTo(m_entry.Hvo), "field='Self' binds the entry itself");
			Assert.That(row.ControlFactory, Is.Not.Null, "the row carries the plugin's control factory");
			Assert.That(plugin.BuildCalls, Is.EqualTo(0),
				"compose defers control building to the view (factory, not control)");
		}

		[Test]
		public void PluginRowFactory_ClosesOverObjectNodeCacheAndTheComposedEditContext()
		{
			var registry = new RegionEditorPluginRegistry();
			var plugin = new FakeMessagesPlugin();
			registry.Register(plugin);

			var composed = RegionComposer.Compose(m_entry, Cache, plugins: registry);
			var row = composed.Model.Fields.Single(f => f.Kind == DetailFieldKind.Custom);

			row.ControlFactory();

			Assert.That(plugin.BuildCalls, Is.EqualTo(1));
			Assert.That(plugin.LastObject?.Hvo, Is.EqualTo(m_entry.Hvo));
			Assert.That(plugin.LastNode?.CustomEditorClass, Is.EqualTo(MessageSliceClassName));
			Assert.That(plugin.LastCache, Is.SameAs(Cache));
			Assert.That(plugin.LastEditContext, Is.SameAs(composed.EditContext),
				"the deferred accessor resolves to the region's own composed edit context");
		}
	}
}
