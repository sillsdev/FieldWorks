// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the "Create a new Part of Speech" flow — the New-UI replacement for the
	/// WinForms <see cref="MasterCategoryListDlg"/> that the legacy <c>POSPopupTreeManager</c> launches from the POS
	/// tree's "More..." item (MSA-port Stage 4). Rather than build a brand-new tree dialog, it reuses the existing
	/// reusable Avalonia <see cref="ChooserDialogViewModel"/>/<c>ChooserDialogView</c> in HIERARCHICAL single-select
	/// mode, fed the master-category (GOLDEtic) catalog as depth-tagged <see cref="RegionChoiceOption"/> candidates
	/// (key = the catalog id). On OK it mirrors <see cref="MasterCategoryListDlg"/>'s create-in-project logic exactly:
	/// the chosen master category is added to <c>cache.LangProject.PartsOfSpeechOA</c> (or under its catalog parent)
	/// via <see cref="MasterCategory.AddToDatabase"/> — which creates the <c>IPartOfSpeech</c> with the catalog's
	/// fixed guid + name/abbr/description + CatalogSourceId, in ONE undoable step — and returns a fresh
	/// <see cref="FwPosNode"/> for it (guid id + name + abbr) for the requesting chooser to add + select.
	///
	/// Layering mirrors <see cref="LcmChooserDialogLauncher"/>: BuildState / Apply are internal so the chosen-id →
	/// new-IPartOfSpeech + FwPosNode round-trip is unit-testable against a real cache (via InternalsVisibleTo) without
	/// running the modal. The only runtime-only aspect is loading GOLDEtic.xml off disk (the
	/// <see cref="FwDirectoryFinder.TemplateDirectory"/> the WinForms dialog uses); tests feed an in-memory catalog
	/// document to the same builder + create core.
	/// </summary>
	public sealed class LcmCreatePartOfSpeechLauncher
		: AvaloniaDialogLauncher<ChooserDialogInput, ChooserDialogViewModel, LcmCreatePartOfSpeechLauncher.CreatePosPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly IReadOnlyList<CatalogCategory> _catalog;
		private ChooserDialogViewModel _viewModel;

		private LcmCreatePartOfSpeechLauncher(LcmCache cache, IReadOnlyList<CatalogCategory> catalog,
			Mediator mediator, PropertyTable propertyTable, IHelpTopicProvider helpProvider)
		{
			_cache = cache;
			_catalog = catalog;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
		}

		/// <summary>The follow-up signals from an accepted create-POS flow: the new POS (or an existing chosen one).</summary>
		public struct CreatePosPayload
		{
			/// <summary>The IPartOfSpeech the flow resolved (created, or an existing catalog match); null on cancel.</summary>
			public IPartOfSpeech Pos;

			/// <summary>The LCModel-free node for <see cref="Pos"/> fed back to the requesting chooser; null on cancel.</summary>
			public FwPosNode Node;
		}

		/// <summary>The POS created (or chosen) on OK; null when cancelled or nothing resolved.</summary>
		public IPartOfSpeech CreatedPos { get; private set; }

		/// <summary>The LCModel-free node for <see cref="CreatedPos"/> (guid id + name + abbr); null when cancelled.</summary>
		public FwPosNode CreatedNode { get; private set; }

		/// <summary>
		/// Shows the master-category catalog (loaded from GOLDEtic.xml) over <paramref name="owner"/> as a
		/// hierarchical single-select chooser and, on OK, creates the chosen category as an <c>IPartOfSpeech</c> in
		/// the project (mirroring <see cref="MasterCategoryListDlg"/>). Returns the new node (guid id + name + abbr)
		/// or null when cancelled. Nested-modal over the Insert Entry dialog is fine (the host owns this launcher).
		/// </summary>
		public static FwPosNode CreateInProject(LcmCache cache, IWin32Window owner, Mediator mediator = null,
			PropertyTable propertyTable = null, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var catalog = LoadCatalog(cache);
			var launcher = new LcmCreatePartOfSpeechLauncher(cache, catalog, mediator, propertyTable, helpProvider);
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted)
				return null;
			launcher.CreatedPos = outcome.Payload.Pos;
			launcher.CreatedNode = outcome.Payload.Node;
			return outcome.Payload.Node;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.CreatePosTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 420;
		protected override int DialogHeight => 460;

		protected override ChooserDialogInput BuildState() => BuildInput(_catalog);

		/// <summary>
		/// Builds the LCModel-free <see cref="ChooserDialogInput"/> from the loaded catalog: each category becomes a
		/// hierarchical single-select candidate (key = catalog id, depth = catalog nesting), the chooser opens as a
		/// collapsible tree (the same reuse the possibility-list chooser uses), and OK is gated until the user picks a
		/// category (a create flow must choose something). Internal so the catalog → candidates mapping is testable.
		/// </summary>
		internal static ChooserDialogInput BuildInput(IReadOnlyList<CatalogCategory> catalog)
		{
			return new ChooserDialogInput
			{
				Candidates = BuildCandidates(catalog),
				SelectionMode = ChooserSelectionMode.Single,
				InitialSelectedKeys = Array.Empty<string>(),
				AllowEmpty = false,
				Hierarchical = true,
				Prompt = FwAvaloniaDialogsStrings.CreatePosPrompt,
				HelpTopic = null,
				// A create flow must pick a category before OK (the legacy dialog disables OK until a not-yet-in-DB
				// category is selected); keep the simple "require a selection" rule.
				ForbidEmptySelection = true
			};
		}

		/// <summary>
		/// Projects the catalog (document order, depth-tagged) into chooser candidates. The key is the catalog id (the
		/// GOLDEtic "id" attribute), the name is the category's display term, and the depth carries the catalog
		/// nesting so the reused chooser folds it into the same tree the WinForms TreeView showed. Internal for tests.
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildCandidates(IReadOnlyList<CatalogCategory> catalog)
			=> catalog.Select(c => new RegionChoiceOption(c.Id, c.DisplayName, c.Depth)).ToList();

		protected override ChooserDialogViewModel CreateViewModel(ChooserDialogInput state)
		{
			_viewModel = new ChooserDialogViewModel(state);
			return _viewModel;
		}

		protected override AvControl CreateView(ChooserDialogViewModel viewModel) =>
			new ChooserDialogView { DataContext = viewModel };

		protected override CreatePosPayload Apply(ChooserDialogInput state)
		{
			// Run executes Apply on the OK path only, AFTER the VM snapshotted ChosenKeys (the picked catalog id).
			var chosenId = _viewModel?.ChosenKeys?.FirstOrDefault();
			var pos = CreatePosFromCatalog(_cache, _catalog, chosenId);
			return new CreatePosPayload { Pos = pos, Node = BuildNode(pos) };
		}

		// ----- create-in-project (mirrors MasterCategoryListDlg_Closing's OK branch) -----

		/// <summary>
		/// Creates (or resolves) the <c>IPartOfSpeech</c> for the chosen catalog id, mirroring
		/// <see cref="MasterCategoryListDlg"/>'s OK logic: the chosen <see cref="MasterCategory"/> is added to the
		/// project's parts-of-speech list (under its catalog parent when the parent is itself a category) via
		/// <see cref="MasterCategory.AddToDatabase"/> — which creates the POS with the catalog's fixed guid, populates
		/// name/abbr/description for every writing system from the catalog node, and sets <c>CatalogSourceId</c>, all
		/// in ONE undoable step. A category already present in the project resolves to its existing POS (AddToDatabase
		/// no-ops). Returns null for a missing/unparsable chosen id. Internal so the chosen-id → IPartOfSpeech
		/// round-trip is unit-testable against a real cache without running the modal.
		/// </summary>
		internal static IPartOfSpeech CreatePosFromCatalog(LcmCache cache, IReadOnlyList<CatalogCategory> catalog,
			string chosenId)
		{
			if (cache == null || string.IsNullOrEmpty(chosenId))
				return null;
			var chosen = catalog.FirstOrDefault(c => c.Id == chosenId);
			if (chosen == null)
				return null;

			var posList = cache.LangProject.PartsOfSpeechOA;
			// AddToDatabase opens its own UndoableUnitOfWorkHelper.Do (one undoable step) and is a no-op when the
			// category is already in the database (then chosen.MasterCategory.POS is its existing POS). The catalog
			// parent (when it is itself a category) is where the legacy dialog nests the new POS.
			chosen.MasterCategory.AddToDatabase(cache, posList, chosen.Parent?.MasterCategory, subItemOwner: null);
			return chosen.MasterCategory.POS;
		}

		/// <summary>Builds the LCModel-free <see cref="FwPosNode"/> for a freshly created/resolved POS (guid id + name + abbr).</summary>
		internal static FwPosNode BuildNode(IPartOfSpeech pos)
		{
			if (pos == null)
				return null;
			var name = pos.Name.BestAnalysisAlternative?.Text ?? pos.ShortName ?? pos.Guid.ToString();
			var abbr = pos.Abbreviation?.BestAnalysisAlternative?.Text;
			// The created node is appended to the existing chooser node list (depth 0); the host re-feeds the whole
			// project hierarchy afterwards so the new POS lands at its real depth in both choosers.
			return new FwPosNode(pos.Guid.ToString(), name, depth: 0, abbreviation: abbr);
		}

		// ----- catalog load (mirrors MasterCategoryListDlg.LoadMasterCategories) -----

		/// <summary>
		/// Loads the master-category catalog from GOLDEtic.xml (the same source + parse the WinForms
		/// <see cref="MasterCategoryListDlg"/> uses): from <see cref="FwDirectoryFinder.TemplateDirectory"/>, walking
		/// <c>/eticPOSList/item</c>, skipping the top <c>PartOfSpeechValue</c> grouping node. This disk read is the
		/// one runtime-only aspect; <see cref="BuildCatalog(LcmCache, XmlNode)"/> is the shared, testable core.
		/// </summary>
		internal static IReadOnlyList<CatalogCategory> LoadCatalog(LcmCache cache)
		{
			var doc = new XmlDocument();
			doc.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory, "GOLDEtic.xml"));
			return BuildCatalog(cache, doc.DocumentElement);
		}

		/// <summary>
		/// Builds the flat, document-order, depth-tagged catalog from an <c>eticPOSList</c> root element, mirroring
		/// <see cref="MasterCategoryListDlg.AddNodes"/>/<c>AddNode</c>: each <c>item</c> becomes a
		/// <see cref="CatalogCategory"/> wrapping a <see cref="MasterCategory"/> built against the project's existing
		/// POSes (so an already-installed category resolves to its existing POS), the top <c>PartOfSpeechValue</c>
		/// grouping node is skipped, and children nest one level deeper. Internal so the catalog model is unit-testable
		/// from an in-memory XML document.
		/// </summary>
		internal static IReadOnlyList<CatalogCategory> BuildCatalog(LcmCache cache, XmlNode root)
		{
			// The set of POSes already in the project, so MasterCategory.Create can mark each catalog entry that is
			// already installed (and resolve it to its existing POS) — exactly as the WinForms dialog seeds it.
			var posSet = new HashSet<IPartOfSpeech>(
				cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.OfType<IPartOfSpeech>());

			var catalog = new List<CatalogCategory>();
			void AddNode(XmlNode node, CatalogCategory parent, int depth)
			{
				// The top-level grouping node is not a real category — descend through it (legacy AddNode behavior).
				if (node.Attributes?["id"]?.InnerText == "PartOfSpeechValue")
				{
					foreach (XmlNode child in node.SelectNodes("item"))
						AddNode(child, parent, depth);
					return;
				}

				var mc = MasterCategory.Create(posSet, node, cache);
				var entry = new CatalogCategory(node.Attributes["id"].InnerText, mc, parent, depth);
				catalog.Add(entry);
				foreach (XmlNode child in node.SelectNodes("item"))
					AddNode(child, entry, depth + 1);
			}

			foreach (XmlNode item in root.SelectNodes("/eticPOSList/item"))
				AddNode(item, parent: null, depth: 0);
			return catalog;
		}

		/// <summary>
		/// A node in the loaded master-category catalog: the catalog id, the <see cref="MasterCategory"/> that carries
		/// the catalog's strings + guid + the create-in-project logic, the catalog parent (for nesting the new POS),
		/// and the catalog depth (fed to the reused chooser tree).
		/// </summary>
		internal sealed class CatalogCategory
		{
			public CatalogCategory(string id, MasterCategory masterCategory, CatalogCategory parent, int depth)
			{
				Id = id;
				MasterCategory = masterCategory;
				Parent = parent;
				Depth = depth < 0 ? 0 : depth;
			}

			public string Id { get; }
			public MasterCategory MasterCategory { get; }
			public CatalogCategory Parent { get; }
			public int Depth { get; }

			/// <summary>The catalog display term (the MasterCategory's term; "X (in FW project)" when already installed).</summary>
			public string DisplayName => TsStringUtils.NormalizeToNFC(MasterCategory.ToString());
		}
	}
}
