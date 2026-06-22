// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D1 — the one plugin contract for every remaining custom
	/// editor: builds an Avalonia control for (object, node, edit context) so a legacy dynamically
	/// loaded slice (<c>editor="Custom" class=...</c>) can render in-tree at the slice's real
	/// position instead of an unsupported row or the WinForms companion strip. Plugins are keyed by
	/// the <b>legacy layout identity</b> (<see cref="LegacyClassName"/>, the layout's `class=`
	/// attribute already carried on the typed node) — zero layout edits per migration, and the
	/// identical mechanism serves the next DataTree tools (Notebook, Morphology) for free.
	/// </summary>
	public interface IRegionEditorPlugin
	{
		/// <summary>
		/// The fully qualified legacy slice class this plugin claims (the layout `class=`
		/// attribute, e.g. <c>SIL.FieldWorks.XWorks.LexEd.MessageSlice</c>).
		/// </summary>
		string LegacyClassName { get; }

		/// <summary>
		/// Builds the Avalonia control that replaces the legacy slice for one composed row. Invoked
		/// lazily by the view (never during compose); the context carries the region's own edit
		/// context so plugin edits ride the same fenced session as every other row, plus the
		/// optional host services (review task 13: the former IServiceAwareRegionEditorPlugin
		/// marker interface and its five-argument overload collapsed into this ONE contract).
		/// </summary>
		Control BuildControl(RegionEditorBuildContext context);
	}

	/// <summary>
	/// Review task 13 — everything the composer hands a plugin factory, bundled into one contract:
	/// the row's object and typed node, the region's edit context (resolved lazily through the
	/// composer's deferred accessor — the context object is created during compose, BEFORE the
	/// edit context exists; plugin factories run at render time, after), the cache, and the
	/// host-injected <see cref="RegionEditorServices"/> (D4; null when the host supplies none —
	/// services are always optional and plugins must tolerate null).
	/// </summary>
	public sealed class RegionEditorBuildContext
	{
		private readonly Func<IRegionEditContext> _editContextAccessor;

		public RegionEditorBuildContext(ICmObject target, ViewNode node,
			Func<IRegionEditContext> editContextAccessor, LcmCache cache,
			RegionEditorServices services = null)
		{
			Target = target;
			Node = node;
			_editContextAccessor = editContextAccessor;
			Cache = cache;
			Services = services;
		}

		/// <summary>The composed row's own object (the slice's object in legacy terms).</summary>
		public ICmObject Target { get; }

		/// <summary>The row's typed view node (layout identity, field, label, menu bindings).</summary>
		public ViewNode Node { get; }

		/// <summary>The region's edit context, resolved on read (null until the region composed).</summary>
		public IRegionEditContext EditContext => _editContextAccessor?.Invoke();

		public LcmCache Cache { get; }

		/// <summary>Host-injected services (the legacy-dialog launcher seam); may be null.</summary>
		public RegionEditorServices Services { get; }
	}

	/// <summary>
	/// winforms-free-lexeme-editor.md D1 — maps legacy slice class names to their
	/// <see cref="IRegionEditorPlugin"/>. The composer consults <see cref="Resolve"/> per node
	/// while walking, FIRST in the resolution order (plugin → companion strip → unsupported row).
	/// Thread-safe by immutable snapshot: registration copies under a lock, resolution reads the
	/// current snapshot without one, so a compose mid-registration sees a coherent table.
	/// </summary>
	public sealed class RegionEditorPluginRegistry
	{
		private readonly object _sync = new object();
		private volatile IReadOnlyDictionary<string, IRegionEditorPlugin> _snapshot
			= new Dictionary<string, IRegionEditorPlugin>(StringComparer.Ordinal);

		/// <summary>The process-wide registry the composer uses unless a caller supplies its own.</summary>
		public static RegionEditorPluginRegistry Default { get; } = CreateDefault();

		/// <summary>
		/// Registers a plugin for its <see cref="IRegionEditorPlugin.LegacyClassName"/>. A legacy
		/// class has exactly one owner: re-registering an already-claimed class throws, so two
		/// migrations cannot silently fight over a slice.
		/// </summary>
		public void Register(IRegionEditorPlugin plugin)
		{
			if (plugin == null)
				throw new ArgumentNullException(nameof(plugin));
			if (string.IsNullOrEmpty(plugin.LegacyClassName))
				throw new ArgumentException("A region editor plugin must claim a legacy class name (D1).",
					nameof(plugin));
			lock (_sync)
			{
				if (_snapshot.ContainsKey(plugin.LegacyClassName))
					throw new ArgumentException(
						$"'{plugin.LegacyClassName}' is already claimed by another plugin.", nameof(plugin));
				var next = new Dictionary<string, IRegionEditorPlugin>((IDictionary<string, IRegionEditorPlugin>)_snapshot,
					StringComparer.Ordinal)
				{
					[plugin.LegacyClassName] = plugin
				};
				_snapshot = next;
			}
		}

		/// <summary>The plugin claiming the legacy class, or null when the class is unclaimed.</summary>
		public IRegionEditorPlugin Resolve(string legacyClassName)
		{
			if (string.IsNullOrEmpty(legacyClassName))
				return null;
			return _snapshot.TryGetValue(legacyClassName, out var plugin) ? plugin : null;
		}

		/// <summary>The currently claimed legacy class names (a snapshot; burn-down governance, D5).</summary>
		public IReadOnlyCollection<string> RegisteredClassNames
		{
			get
			{
				var snapshot = _snapshot;
				return new List<string>(snapshot.Keys);
			}
		}

		private static RegionEditorPluginRegistry CreateDefault()
		{
			var registry = new RegionEditorPluginRegistry();
			RegisterBuiltins(registry);
			return registry;
		}

		// The builtin plugin list. Wave 2 (D2) landed ChorusNotesPlugin — the native Avalonia notes
		// bar over LibChorus, retiring the companion strip's only designated class. Wave 3 (D3) is
		// a composer lane, not a plugin. Wave 4 (D4) landed the dialog-launcher plugins: value row
		// + "..." button calling the host's ILegacyDialogLauncher seam. The
		// LexemeEditorBurnDownTests census measures coverage as they land.
		internal static void RegisterBuiltins(RegionEditorPluginRegistry registry)
		{
			registry.Register(new ChorusNotesPlugin());
			registry.Register(new ReversalIndexEntryPlugin());
			// avalonia-interlinear-editor (W-4): the native Avalonia interlinear editor for the Words
			// Analyses detail pane, claiming the legacy InterlinearSlice. Read-only this wave; the
			// editable write-back + MSA prune (W-5) layer onto the same plugin.
			registry.Register(new InterlinearSlicePlugin());
			registry.Register(DialogLauncherPlugins.CreateMsaInflectionFeatures());
			registry.Register(DialogLauncherPlugins.CreatePhonologicalFeatures());
			registry.Register(DialogLauncherPlugins.CreateAudioVisual());
		}
	}

	/// <summary>
	/// winforms-free-lexeme-editor.md D5 — the lexeme editor's burn-down lanes that are not
	/// expressed in code elsewhere. Together with the plugin registry (<see cref="RegionEditorPluginRegistry"/>)
	/// and the companion designated set (<see cref="AvaloniaCompanionSlices.DesignatedClassNames"/>),
	/// these classify every custom slice class in the lexeme-editor census; the
	/// LexemeEditorBurnDownTests census fails on any unclassified class.
	/// </summary>
	public static class LexemeEditorBurnDown
	{
		/// <summary>
		/// Classes that render as an Avalonia value row plus a legacy-dialog launcher button
		/// through the ILegacyDialogLauncher host seam (D4, wave 4), each WITH its citation. These
		/// classes are ALSO claimed in the default plugin registry (by a
		/// <see cref="LauncherRegionPlugin"/>); the census counts that pairing as the single
		/// "LauncherRouted" lane. The MSA/phonological launchers live in MSA/FsFeatStruc part
		/// files, beyond the LexEntry/LexSense census — registered anyway, forward-looking, for
		/// the per-sense "Grammatical Info. Details" sections and the Grammar tools.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string> LauncherRoutedClassNames =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ DialogLauncherPlugins.MsaFeatureSliceClassName, "D4 launcher lane" },
				{ DialogLauncherPlugins.PhonologicalFeatureSliceClassName, "D4 launcher lane" },
				{ DialogLauncherPlugins.AudioVisualSliceClassName, "D4 launcher lane" }
			};

		/// <summary>
		/// Explicitly deferred classes, each WITH the gate/lane it rides (D5: deferral is only
		/// legitimate with a citation — "documented, not forgotten").
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string> ExplicitlyDeferredClassNames =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				// AudioVisualSlice graduated to LauncherRoutedClassNames in wave 4 (D4).
				// ReversalIndexEntrySlice graduated to a native Avalonia plugin (ReversalIndexEntryPlugin):
				// the sense's reversal-entry forms now compose as an editable multi-WS text field through
				// the D1 plugin lane, retiring the lone Unsupported row. It is therefore PluginRouted, no
				// longer deferred. This set is now EMPTY — every census class is actively classified.
			};

		/// <summary>
		/// Classes absorbed by a composer lane (no plugin needed: the composer recognizes the node
		/// by metadata and composes a native editable row), each WITH the lane that absorbed it.
		/// Wave 3: EntrySequenceReferenceSlice's entry-reference vectors compose as editable
		/// ReferenceVector rows with type-ahead lexicon search (D3). Deferred note for that lane:
		/// the slice's VIRTUAL back-ref fields (ComplexFormEntries, Subentries,
		/// VisibleComplexFormBackRefs, VariantFormEntries) still render read-only — their writes
		/// land on the other entry's LexEntryRef (the legacy launcher's AddNewObjectsToProperty
		/// overrides) and ride the D3 follow-up with the relation-type walk.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string> LaneAbsorbedClassNames =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice", "D3 ReferenceVector lane" },
				{ "SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice", "D3 ghost reference-vector lane" },
				{ "SIL.FieldWorks.XWorks.LexEd.LexReferenceMultiSlice", "D3 lexical relation lane" }
			};
	}
}
