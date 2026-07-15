// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Which feature system a create-feature flow targets — the inflection (morphosyntactic) feature system
	/// (<c>MsFeatureSystemOA</c>) or the phonological feature system (<c>PhFeatureSystemOA</c>). The two differ in
	/// where the new feature is added and what default values it gets (see <see cref="LcmCreateFeatureLauncher"/>).
	/// </summary>
	public enum FeatureSystemKind
	{
		/// <summary>The morphosyntactic / inflection feature system (<c>MsFeatureSystemOA</c>).</summary>
		Inflection,

		/// <summary>The phonological feature system (<c>PhFeatureSystemOA</c>).</summary>
		Phonological
	}

	/// <summary>
	/// The LCModel-aware launcher for the "Create a new feature" / "Add a value to a feature" flows (Phase-1 §19b
	/// Stage 3) — the New-UI replacement for the WinForms <c>MasterInflectionFeatureListDlg</c> /
	/// <c>MasterPhonologicalFeatureListDlg</c> BLANK-CREATE affordance (the "create a brand-new feature" link) and the
	/// feature-system add-value flow. It is the feature-structure analogue of
	/// <see cref="LcmCreatePartOfSpeechLauncher"/>: it opens a small Avalonia name-entry dialog
	/// (<see cref="CreateFeatureDialogViewModel"/>), and on OK creates the feature/value in the feature system in ONE
	/// undoable step, returning a fresh LCModel-free <see cref="FwFeatureNode"/> (+ value children) the editor adds +
	/// selects. The created model object never crosses the Avalonia seam — only the node does.
	///
	/// Create logic mirrors the WinForms blank-create verbatim:
	///   * Inflection feature: create an <c>IFsClosedFeature</c>, add it to <c>MsFeatureSystemOA.FeaturesOC</c>,
	///     ensure an "Infl" <c>IFsFeatStrucType</c> exists (labelled across the analysis writing systems), and add the
	///     feature to that type's <c>FeaturesRS</c> — exactly <c>MasterInflectionFeatureListDlg.linkLabel1_LinkClicked</c>.
	///   * Phonological feature: create an <c>IFsClosedFeature</c>, add it to <c>PhFeatureSystemOA.FeaturesOC</c>, and
	///     create the two default symbolic values <c>SimpleInit("+","positive")</c> / <c>SimpleInit("-","negative")</c>
	///     — exactly <c>MasterPhonologicalFeatureListDlg.linkLabel1_LinkClicked</c>.
	///   * Add value: create an <c>IFsSymFeatVal</c> under the closed feature's <c>ValuesOC</c>, named from the dialog.
	///
	/// PARITY (§19b Stage 3): the MGA-catalog IMPORT path (pick a feature from EticGlossList.xml /
	/// PhonFeatsEticGlossList.xml via <c>AddFeatureFromXml</c>) is NOT ported here — it needs the MGA assembly + the
	/// WinForms GlossList tree parser, outside this stage's clean reach. The blank-create primitive (the common
	/// "I need a feature that doesn't exist yet" case) is fully wired + tested; the catalog import remains the legacy
	/// MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg flow.
	///
	/// Layering mirrors <see cref="LcmCreatePartOfSpeechLauncher"/>: the create cores
	/// (<see cref="CreateClosedFeature"/> / <see cref="CreateValue"/>) are internal static so the
	/// name → new-feature/value + node round-trip is unit-testable against a real cache (via InternalsVisibleTo)
	/// without running the modal.
	/// </summary>
	public sealed class LcmCreateFeatureLauncher
		: AvaloniaDialogLauncher<CreateFeatureDialogViewModel, CreateFeatureDialogViewModel,
			LcmCreateFeatureLauncher.CreateFeaturePayload>
	{
		private readonly LcmCache _cache;
		private readonly FeatureSystemKind _systemKind;
		private readonly string _addValueToClosedFeatureId; // null = create a feature; non-null = add a value to it
		private CreateFeatureDialogViewModel _viewModel;

		private LcmCreateFeatureLauncher(LcmCache cache, FeatureSystemKind systemKind, string addValueToClosedFeatureId)
		{
			_cache = cache;
			_systemKind = systemKind;
			_addValueToClosedFeatureId = addValueToClosedFeatureId;
		}

		/// <summary>The follow-up signals from an accepted create flow: the created node + (for a feature) its value children.</summary>
		public struct CreateFeaturePayload
		{
			/// <summary>The LCModel-free node for the created feature/value; null on cancel.</summary>
			public FwFeatureNode Node;

			/// <summary>For a created CLOSED feature, its value children (e.g. the phonological +/- values); null/empty otherwise.</summary>
			public System.Collections.Generic.IReadOnlyList<FwFeatureNode> ValueChildren;
		}

		/// <summary>The created feature/value node on OK; null when cancelled.</summary>
		public FwFeatureNode CreatedNode { get; private set; }

		/// <summary>For a created closed feature, its value children (the phonological +/- values); else null.</summary>
		public System.Collections.Generic.IReadOnlyList<FwFeatureNode> CreatedValueChildren { get; private set; }

		/// <summary>
		/// Shows the create-FEATURE dialog over <paramref name="owner"/> and, on OK, creates a new closed feature in
		/// the requested feature system (inflection / phonological). Returns the new node (and, for the phonological
		/// system, its +/- value children via <see cref="CreatedValueChildren"/>) or null when cancelled.
		/// </summary>
		public static FwFeatureNode CreateFeature(LcmCache cache, FeatureSystemKind systemKind, IWin32Window owner,
			out System.Collections.Generic.IReadOnlyList<FwFeatureNode> valueChildren)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			var launcher = new LcmCreateFeatureLauncher(cache, systemKind, addValueToClosedFeatureId: null);
			var outcome = launcher.Run(owner);
			valueChildren = outcome.Accepted ? outcome.Payload.ValueChildren : null;
			if (!outcome.Accepted)
				return null;
			launcher.CreatedNode = outcome.Payload.Node;
			launcher.CreatedValueChildren = outcome.Payload.ValueChildren;
			return outcome.Payload.Node;
		}

		/// <summary>
		/// Shows the add-VALUE dialog over <paramref name="owner"/> and, on OK, creates a new symbolic value under the
		/// closed feature identified by <paramref name="closedFeatureId"/> (a guid string). Returns the new value node
		/// or null when cancelled. Scoped to the inflection feature system (the editor's add-value affordance lives on
		/// the inflection chooser).
		/// </summary>
		public static FwFeatureNode AddValue(LcmCache cache, FeatureSystemKind systemKind, string closedFeatureId,
			IWin32Window owner)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (string.IsNullOrEmpty(closedFeatureId))
				return null;
			var launcher = new LcmCreateFeatureLauncher(cache, systemKind, closedFeatureId);
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted)
				return null;
			launcher.CreatedNode = outcome.Payload.Node;
			return outcome.Payload.Node;
		}

		// ----- scaffold steps -----

		private bool IsAddValue => _addValueToClosedFeatureId != null;

		protected override string DialogTitle =>
			IsAddValue ? FwAvaloniaDialogsStrings.CreateValueTitle : FwAvaloniaDialogsStrings.CreateFeatureTitle;
		protected override int DialogWidth => 340;
		protected override int DialogHeight => 200;

		protected override CreateFeatureDialogViewModel BuildState() =>
			IsAddValue ? CreateFeatureDialogViewModel.ForValue() : CreateFeatureDialogViewModel.ForFeature();

		protected override CreateFeatureDialogViewModel CreateViewModel(CreateFeatureDialogViewModel state)
		{
			_viewModel = state;
			return _viewModel;
		}

		protected override AvControl CreateView(CreateFeatureDialogViewModel viewModel) =>
			new CreateFeatureDialogView { DataContext = viewModel };

		protected override CreateFeaturePayload Apply(CreateFeatureDialogViewModel state)
		{
			var name = _viewModel?.ChosenName;
			var abbr = _viewModel?.ChosenAbbreviation;
			if (string.IsNullOrEmpty(name))
				return new CreateFeaturePayload();

			if (IsAddValue)
			{
				var valueNode = CreateValue(_cache, _systemKind, _addValueToClosedFeatureId, name, abbr);
				return new CreateFeaturePayload { Node = valueNode };
			}

			var (node, children) = CreateClosedFeature(_cache, _systemKind, name, abbr);
			return new CreateFeaturePayload { Node = node, ValueChildren = children };
		}

		// ----- create-in-feature-system cores (mirror the WinForms blank-create) -----

		/// <summary>
		/// Creates a new <c>IFsClosedFeature</c> in the requested feature system in ONE undoable step, mirroring the
		/// WinForms blank-create links. For the inflection system it ensures the "Infl" <c>IFsFeatStrucType</c> and
		/// adds the feature to it; for the phonological system it adds the two default <c>+/-</c> symbolic values.
		/// Returns the new feature's node (depth 0) and, for the phonological system, its value-children nodes (depth 1).
		/// Internal so the create is unit-testable against a real cache.
		/// </summary>
		internal static (FwFeatureNode node, System.Collections.Generic.IReadOnlyList<FwFeatureNode> children)
			CreateClosedFeature(LcmCache cache, FeatureSystemKind systemKind, string name, string abbr)
		{
			if (cache == null || string.IsNullOrEmpty(name))
				return (null, null);

			IFsClosedFeature feature = null;
			System.Collections.Generic.List<FwFeatureNode> children = null;

			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				feature = cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				var analWs = cache.DefaultAnalWs;
				if (systemKind == FeatureSystemKind.Phonological)
				{
					cache.LangProject.PhFeatureSystemOA.FeaturesOC.Add(feature);
					SetNameAndAbbr(feature, name, abbr, analWs);

					// The two default phonological values (MasterPhonologicalFeatureListDlg parity).
					var symValFactory = cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>();
					children = new System.Collections.Generic.List<FwFeatureNode>();
					var plus = symValFactory.Create();
					feature.ValuesOC.Add(plus);
					plus.SimpleInit("+", "positive");
					children.Add(new FwFeatureNode(plus.Guid.ToString(), ValueName(plus), FwFeatureNodeKind.Value, 1));
					var minus = symValFactory.Create();
					feature.ValuesOC.Add(minus);
					minus.SimpleInit("-", "negative");
					children.Add(new FwFeatureNode(minus.Guid.ToString(), ValueName(minus), FwFeatureNodeKind.Value, 1));
				}
				else
				{
					cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.Add(feature);
					SetNameAndAbbr(feature, name, abbr, analWs);

					// Ensure the "Infl" feature-structure type and add the feature to it
					// (MasterInflectionFeatureListDlg parity).
					var type = cache.LanguageProject.MsFeatureSystemOA.GetFeatureType("Infl");
					if (type == null)
					{
						type = cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>().Create();
						cache.LanguageProject.MsFeatureSystemOA.TypesOC.Add(type);
						type.CatalogSourceId = "Infl";
						foreach (CoreWritingSystemDefinition ws in
							cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
						{
							var tss = TsStringUtils.MakeString("Infl", ws.Handle);
							type.Abbreviation.set_String(ws.Handle, tss);
							type.Name.set_String(ws.Handle, tss);
						}
					}
					type.FeaturesRS.Add(feature);
				}
			});

			if (feature == null)
				return (null, null);
			var node = new FwFeatureNode(feature.Guid.ToString(), Name(feature), FwFeatureNodeKind.Closed, 0);
			return (node, children);
		}

		/// <summary>
		/// Creates a new symbolic value (<c>IFsSymFeatVal</c>) under the closed feature with id
		/// <paramref name="closedFeatureId"/> in ONE undoable step, mirroring the feature-system add-value flow.
		/// Returns the new value's node (depth 1). A null/unresolvable feature id is a no-op (returns null). Internal
		/// so the create is unit-testable against a real cache.
		/// </summary>
		internal static FwFeatureNode CreateValue(LcmCache cache, FeatureSystemKind systemKind, string closedFeatureId,
			string name, string abbr)
		{
			if (cache == null || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(closedFeatureId)
				|| !Guid.TryParse(closedFeatureId, out var guid))
				return null;

			IFsClosedFeature closed;
			try { closed = cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>().GetObject(guid); }
			catch { return null; }
			if (closed == null)
				return null;

			IFsSymFeatVal symVal = null;
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				symVal = cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				closed.ValuesOC.Add(symVal);
				// SimpleInit sets abbreviation + name for the value (the legacy add-value primitive).
				symVal.SimpleInit(string.IsNullOrEmpty(abbr) ? name : abbr, name);
			});

			return symVal == null
				? null
				: new FwFeatureNode(symVal.Guid.ToString(), ValueName(symVal), FwFeatureNodeKind.Value, 1);
		}

		// ----- small helpers -----

		private static void SetNameAndAbbr(IFsFeatDefn feature, string name, string abbr, int analWs)
		{
			feature.Name.set_String(analWs, TsStringUtils.MakeString(name, analWs));
			if (!string.IsNullOrEmpty(abbr))
				feature.Abbreviation.set_String(analWs, TsStringUtils.MakeString(abbr, analWs));
		}

		private static string Name(IFsFeatDefn defn)
			=> defn.Name?.BestAnalysisAlternative?.Text ?? defn.ShortName ?? defn.Guid.ToString();

		private static string ValueName(IFsSymFeatVal val)
			=> val.Name?.BestAnalysisAlternative?.Text ?? val.ShortName ?? val.Guid.ToString();
	}
}
