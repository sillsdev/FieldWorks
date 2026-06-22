// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The native-Views/C++ viewing capabilities a migrated lexical-edit region historically leaned
	/// on (task 8.1 inventory) and must now provide itself in managed/Avalonia form (task 8.3). One
	/// entry per capability the legacy RootSite/Views pipeline owned.
	/// </summary>
	public enum RegionViewingCapability
	{
		/// <summary>Glyph shaping/segmenting (legacy native Uniscribe/Graphite render engines).</summary>
		TextShaping,

		/// <summary>Box/paragraph layout and text measurement (legacy native box-layout interface).</summary>
		Measurement,

		/// <summary>Selection range and anchor metadata (legacy native selection object).</summary>
		SelectionMetadata,

		/// <summary>Point-to-caret/cluster hit testing (legacy native root-box hit test).</summary>
		HitTesting,

		/// <summary>Scrolling of the detail surface (legacy native RootSite auto-scroll host).</summary>
		Scrolling,

		/// <summary>On-screen drawing of view content (legacy native buffered draw path).</summary>
		Rendering,

		/// <summary>Turning a field definition into a live editor (legacy native RootSite slices).</summary>
		EditorRealization
	}

	/// <summary>
	/// One viewing capability mapped to the FieldWorks-owned managed/Avalonia type that provides it
	/// inside the migrated region. This is the as-built replacement contract task 8.3 records: the
	/// positive complement to the <c>EngineIsolationAuditTests</c> negative audit (which proves no
	/// native symbol is named). The exact native symbol each capability supersedes is documented in
	/// `native-views-audit.md` §8.3 and cross-checked by `RegionViewingServiceReplacementTests`; it is
	/// intentionally NOT named here, because the isolation audit forbids production source from naming
	/// the native pipeline at all.
	/// </summary>
	public sealed class RegionViewingServiceDescriptor
	{
		public RegionViewingServiceDescriptor(RegionViewingCapability capability, Type managedOwner,
			string notes)
		{
			Capability = capability;
			ManagedOwner = managedOwner;
			Notes = notes;
		}

		public RegionViewingCapability Capability { get; }

		/// <summary>The FieldWorks-owned managed type that owns this capability in the region.</summary>
		public Type ManagedOwner { get; }

		public string Notes { get; }
	}

	/// <summary>
	/// A viewing concern deliberately left out of the migrated region's managed replacement, recorded
	/// so the deferral is explicit (named with a reason, owning phase, and user-visible fallback)
	/// rather than a silent gap. Tasks 8.3/8.5 require deferrals to be named, not assumed.
	/// </summary>
	public sealed class DeferredViewingConcern
	{
		public DeferredViewingConcern(string name, string reason, string owningPhase, string fallbackBehavior)
		{
			Name = name;
			Reason = reason;
			OwningPhase = owningPhase;
			FallbackBehavior = fallbackBehavior;
		}

		public string Name { get; }
		public string Reason { get; }

		/// <summary>The change/phase that owns closing this deferral.</summary>
		public string OwningPhase { get; }

		/// <summary>What the user gets meanwhile (never silent data loss).</summary>
		public string FallbackBehavior { get; }
	}

	/// <summary>
	/// The migrated lexical-edit region's viewing-service decommissioning contract (task 8.3/8.5):
	/// the as-built map from each native-Views viewing capability to the managed/Avalonia type that
	/// now owns it, plus the explicitly-deferred concerns. This is the reusable foundation a later
	/// region copies — it names what "replace the native viewing/render/editor seam" means in
	/// checkable terms and is asserted by <c>RegionViewingServiceReplacementTests</c>.
	/// </summary>
	public static class RegionViewingServices
	{
		/// <summary>
		/// Every native viewing capability the region now provides managed, with its owner and the
		/// native symbol it supersedes. Owners all live in the FwAvalonia production assembly, which
		/// (per <c>EngineIsolationAuditTests</c>) cannot load native Views — so by construction these
		/// replacements use Avalonia's own Skia/HarfBuzz text stack, not the C++ engine.
		/// </summary>
		public static IReadOnlyList<RegionViewingServiceDescriptor> Replacements { get; } =
			new List<RegionViewingServiceDescriptor>
			{
				new RegionViewingServiceDescriptor(RegionViewingCapability.TextShaping,
					typeof(FwMultiWsTextField),
					"Glyph shaping comes from Avalonia's text stack (Skia/HarfBuzz) inside the owned editor; "
					+ "no native Uniscribe/Graphite shaping engine is selected. Graphite parity is the "
					+ "separate graphite-transition-support policy, not a native-engine dependency here."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.Measurement,
					typeof(LexicalEditRegionView),
					"Row/field measurement and layout are Avalonia layout passes over the region view's "
					+ "panels; no native box-layout pass is built."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.SelectionMetadata,
					typeof(RegionBidirectionalTextNavigation),
					"Selection range/anchor and mixed-direction caret semantics are computed by the managed "
					+ "navigator over the run model (RegionSelectionRange), not a native selection object."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.HitTesting,
					typeof(RegionTextGraphemeClusters),
					"Point-to-caret resolution uses Avalonia's TextBox hit test, normalized to grapheme "
					+ "clusters by the managed model; no native root-box hit-test call."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.Scrolling,
					typeof(LexicalEditRegionView),
					"The region scrolls through an Avalonia ScrollViewer (task 11.12), not a native RootSite "
					+ "auto-scroll host."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.Rendering,
					typeof(LexicalEditRegionView),
					"All on-screen drawing is Avalonia's renderer (Skia); the visual parity frame is captured "
					+ "from it (task 6.9). No native buffered-draw path."),
				new RegionViewingServiceDescriptor(RegionViewingCapability.EditorRealization,
					typeof(FwMultiWsTextField),
					"Field definitions become live editors via the owned controls (FwMultiWsTextField / "
					+ "FwChooserField / FwReferenceVectorField / FwDialogLauncherField over the IR), not "
					+ "native RootSite-derived slices.")
			};

		/// <summary>
		/// Viewing concerns explicitly deferred out of the region's managed replacement. Each is named
		/// with its reason, owning phase, and the user-visible fallback so "decommissioned" carries no
		/// silent asterisk.
		/// </summary>
		public static IReadOnlyList<DeferredViewingConcern> Deferred { get; } =
			new List<DeferredViewingConcern>
			{
				new DeferredViewingConcern("StText multi-paragraph editing",
					"Paragraph layout and document-style editing are materially broader than run-aware "
					+ "string editing and were scoped out of the first text-foundation wave.",
					"avalonia-multi-writing-system-text-foundation (StText follow-on)",
					"sttext fields render read-only paragraph content; full editing stays in the legacy view."),
				new DeferredViewingConcern("Embedded-object (ORC) rich-run editing",
					"Object Replacement Character runs are not a structural feature of the default lexeme "
					+ "string editors (census: multistring/string editors are plain; structural object "
					+ "content lives in sttext). Editing them couples back to object editors/lifetime.",
					"later text wave (revisit if real data shows common ORC runs in plain string fields)",
					"ORC-bearing values render read-only with an explicit affordance "
					+ "(FwAvaloniaStrings.EmbeddedObjectReadOnly); the TsString is preserved losslessly and "
					+ "remains editable in the legacy view."),
				new DeferredViewingConcern("Context-menu command routing",
					"The xCore command/menu pipeline is not yet managed; insert/delete/move commands still "
					+ "need the legacy colleague chain and CurrentSlice context.",
					"shell phase (avalonia-command-focus global phase, task 10.9)",
					"A hidden, detached legacy DataTree + DTMenuHandler is driven only as the active-host "
					+ "contract's approved 'command-menu-routing' baseline adapter (task 3.10/13.4), built "
					+ "lazily on first right-click and never shown.")
			};
	}
}
