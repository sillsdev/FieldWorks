// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 8.3 (positive complement of the 8.4 negative audit): every native-Views viewing capability
	/// the migrated lexical-edit region used to lean on now has a FieldWorks-owned managed/Avalonia
	/// replacement, recorded in <see cref="RegionViewingServices"/> and exercised here. Where
	/// <c>EngineIsolationAuditTests</c> proves the region names NO native symbol, this proves the
	/// managed replacement is present, located in the isolated production assembly, and (for the
	/// deferred embedded-object case) degrades to an explicit, lossless read-only state — not a silent
	/// gap. Tasks 8.5/8.6 deferrals are asserted to be named, not assumed.
	/// </summary>
	[TestFixture]
	public class RegionViewingServiceReplacementTests
	{
		// The native symbol each capability supersedes. This cross-reference lives in the TEST (which
		// the isolation audit excludes from its source scan), NOT in production source — the audit
		// forbids production code from naming the native pipeline even in strings. The human-readable
		// version is `native-views-audit.md` §8.3.
		private static readonly Dictionary<RegionViewingCapability, string> SupersededNativeSymbol =
			new Dictionary<RegionViewingCapability, string>
			{
				{ RegionViewingCapability.TextShaping, "IRenderEngine" },
				{ RegionViewingCapability.Measurement, "IVwEnv" },
				{ RegionViewingCapability.SelectionMetadata, "IVwSelection" },
				{ RegionViewingCapability.HitTesting, "IVwRootBox" },
				{ RegionViewingCapability.Scrolling, "SimpleRootSite" },
				{ RegionViewingCapability.Rendering, "IVwDrawRootBuffered" },
				{ RegionViewingCapability.EditorRealization, "RootSiteControl" }
			};

		[Test]
		public void EveryViewingCapability_IsCoveredExactlyOnce()
		{
			var covered = RegionViewingServices.Replacements.Select(r => r.Capability).ToList();
			var expected = Enum.GetValues(typeof(RegionViewingCapability)).Cast<RegionViewingCapability>().ToList();

			Assert.That(covered, Is.EquivalentTo(expected),
				"every native viewing capability (shaping, measurement, selection, hit testing, "
				+ "scrolling, rendering, editor realization) must have a managed replacement entry");
			Assert.That(covered, Is.Unique, "each capability is mapped once");
		}

		[Test]
		public void EveryCapability_HasManagedOwner_InTheIsolatedFwAvaloniaAssembly()
		{
			var productionAssembly = typeof(LexicalEditRegionView).Assembly;

			foreach (var descriptor in RegionViewingServices.Replacements)
			{
				Assert.That(descriptor.ManagedOwner, Is.Not.Null,
					$"{descriptor.Capability} must name its managed owner");
				Assert.That(descriptor.ManagedOwner.Assembly, Is.EqualTo(productionAssembly),
					$"{descriptor.Capability} owner {descriptor.ManagedOwner.Name} must live in the "
					+ "isolated FwAvalonia assembly, which cannot load native Views");
			}
		}

		[Test]
		public void EveryCapability_SupersedesANamedNativeSymbol_AndExplainsItsReplacement()
		{
			foreach (var descriptor in RegionViewingServices.Replacements)
			{
				Assert.That(SupersededNativeSymbol.ContainsKey(descriptor.Capability), Is.True,
					$"{descriptor.Capability} must record (in §8.3 / this test) the native symbol it replaces");
				Assert.That(descriptor.Notes, Is.Not.Null.And.Not.Empty,
					$"{descriptor.Capability} must explain the managed replacement");
			}

			// Every documented native symbol must map to a real capability (no orphan cross-references).
			Assert.That(SupersededNativeSymbol.Keys, Is.EquivalentTo(
				RegionViewingServices.Replacements.Select(r => r.Capability)));
		}

		[Test]
		public void DeferredConcerns_AreNamed_WithReasonPhaseAndFallback()
		{
			Assert.That(RegionViewingServices.Deferred, Is.Not.Empty,
				"deferrals must be enumerated, not implied");

			foreach (var concern in RegionViewingServices.Deferred)
			{
				Assert.That(concern.Name, Is.Not.Null.And.Not.Empty);
				Assert.That(concern.Reason, Is.Not.Null.And.Not.Empty, $"{concern.Name} needs a reason");
				Assert.That(concern.OwningPhase, Is.Not.Null.And.Not.Empty, $"{concern.Name} needs an owning phase");
				Assert.That(concern.FallbackBehavior, Is.Not.Null.And.Not.Empty,
					$"{concern.Name} needs a named fallback (never silent data loss)");
			}

			var names = RegionViewingServices.Deferred.Select(c => c.Name).ToList();
			Assert.That(names.Any(n => n.IndexOf("StText", StringComparison.OrdinalIgnoreCase) >= 0),
				"StText multi-paragraph editing stays a named deferral");
			Assert.That(names.Any(n => n.IndexOf("ORC", StringComparison.OrdinalIgnoreCase) >= 0
					|| n.IndexOf("embedded-object", StringComparison.OrdinalIgnoreCase) >= 0),
				"embedded-object (ORC) editing stays a named deferral");
			Assert.That(names.Any(n => n.IndexOf("command", StringComparison.OrdinalIgnoreCase) >= 0),
				"the hidden-DataTree command-routing adapter stays a named, contract-gated deferral");
		}

		// Task 8.3 deferral made honest: a text value carrying an embedded object (ORC) the managed
		// editor cannot rebuild renders READ-ONLY with an explicit tooltip and stages nothing — rather
		// than a silently-editable box whose edits the edit context would reject.
		[AvaloniaTest]
		public void EmbeddedObjectValue_RendersReadOnly_WithExplicitAffordance_AndNeverStages()
		{
			var orcRich = new RegionRichTextValue("link",
				new List<RegionTextRun> { new RegionTextRun("link", "qaa-x-orc", objectData: "obj-ref-guid") },
				richXml: "<Str/>", requiresRichEditor: true, canEditRichText: false);
			var field = new LexicalEditRegionField("LexEntry/x/#orc", "Cross Reference", "Form", null,
				RegionFieldKind.Text, EditorClassification.Known, "OrcField", null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("vern", "link", wsTag: "qaa-x-orc", richText: orcRich)
				}, null, null);
			var context = new FakeRegionEditContext();
			var fieldControl = new FwMultiWsTextField(field, "OrcField", context, null);
			var window = new Window { Content = fieldControl, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = fieldControl.GetVisualDescendants().OfType<TextBox>().Single();
			Assert.That(box.IsReadOnly, Is.True, "an embedded-object value must be read-only");
			Assert.That(ToolTip.GetTip(box), Is.EqualTo(FwAvaloniaStrings.EmbeddedObjectReadOnly),
				"the read-only state must be explicit, not silent");

			// Even a programmatic text change must not stage: the staging handler is never wired for
			// an embedded-object value, so the lossless TsString is left untouched.
			box.Text = "tampered";
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.TextEdits, Is.Empty, "no plain-text stage from an ORC value");
			Assert.That(context.RichTextEdits, Is.Empty, "no rich-text stage from an ORC value");
		}
	}
}
