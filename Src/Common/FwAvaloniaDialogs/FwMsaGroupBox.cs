// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// A reusable, LCModel-FREE grammatical-info editor — the Avalonia replacement for the WinForms
	/// <c>MSAGroupBox</c> (the "grammatical info" group inside the Insert Entry dialog). It mirrors that box's
	/// ADAPTIVE layout: which widgets are visible is driven entirely by the current <see cref="MsaType"/> (which
	/// the host sets from the entry's morph type), reconfiguring live when the type changes, exactly as the
	/// WinForms box does when the affix-type or morph type changes.
	///
	/// The widgets, mirroring the WinForms members:
	///   * Main POS — an <see cref="FwPosChooser"/> ("MainPos"), the replacement for <c>m_tcMainPOS</c>.
	///   * Secondary POS — a second <see cref="FwPosChooser"/> ("SecondaryPos") for <c>m_tcSecondaryPOS</c>,
	///     shown ONLY for derivational affixes.
	///   * Affix Type — a combo ("&lt;Not sure&gt;" / Inflectional / Derivational), the replacement for
	///     <c>m_fwcbAffixTypes</c>, shown ONLY for affix morph types. Changing it reconfigures the box (it
	///     re-derives the <see cref="MsaType"/>), exactly like <c>HandleComboMSATypesChange</c>.
	///   * Slot — a combo of inflectional-affix slots, the replacement for <c>m_fwcbSlots</c>, shown ONLY for
	///     inflectional affixes.
	///
	/// MsaType → visible widgets (mirrors MSAGroupBox's switch exactly):
	///   * <see cref="FwMsaType.Stem"/> / <see cref="FwMsaType.Root"/> → Main POS only (label "Category").
	///   * <see cref="FwMsaType.Unclassified"/> → Affix-Type (= Not sure) + Main POS ("Attaches to Category").
	///   * <see cref="FwMsaType.Inflectional"/> → Affix-Type + Main POS + Slot ("Fills Slot").
	///   * <see cref="FwMsaType.Derivational"/> → Affix-Type + Main POS + Secondary POS ("Changes to Category").
	///
	/// The seam is LCModel-FREE: the host feeds the POS node list (for both choosers), the slot options, and the
	/// current values; the control exposes the selection as a <see cref="FwSandboxMsa"/> payload + a change event,
	/// and forwards each chooser's <see cref="FwPosChooser.CreateNewPosRequested"/>. The control holds NO model
	/// reference. Built in pure C# (no XAML) to match <see cref="FwPosChooser"/> and the rest of FwAvalonia.
	/// </summary>
	public sealed class FwMsaGroupBox : Border
	{
		// The column panels (label-over-widget), mirroring the WinForms m_afxTypePanel / m_mainCatPanel /
		// m_slotsPanel. Each is shown/hidden per MsaType. The inflection-class panel (Stage 6) is the Avalonia parity
		// of the legacy InsertEntryDlg inflection-class affordance, shown alongside the main POS for stem/root.
		private readonly StackPanel _affixTypePanel;
		private readonly StackPanel _mainCatPanel;
		private readonly StackPanel _slotsPanel;
		private readonly StackPanel _inflClassPanel;
		// The inflection-feature column (Phase-1 §19b Stage 2). NOTE (19i.10): the legacy four-widget MSAGroupBox did
		// NOT edit inflection features inline — that was the separate MsaInflectionFeatureListDlg slice. This column
		// surfaces that dialog's capability INLINE (net-new convenience, functionally equivalent), over
		// IMoInflAffMsa.InflFeatsOA / IMoDerivAffMsa.FromMsFeaturesOA, shown alongside the POS for infl/deriv affixes.
		private readonly StackPanel _inflFeaturesPanel;

		private readonly TextBlock _affixTypeLabel;
		private readonly TextBlock _mainCatLabel;
		private readonly TextBlock _slotsLabel;
		private readonly TextBlock _inflClassLabel;
		private readonly TextBlock _inflFeaturesLabel;

		// The affix-type combo, the two POS choosers, the slot combo, the inflection-class combo, and the
		// inflection-feature editor (the interactive widgets).
		private readonly ComboBox _affixTypeCombo;
		private readonly FwPosChooser _mainPos;
		private readonly FwPosChooser _secondaryPos;
		private readonly ComboBox _slotCombo;
		private readonly ComboBox _inflClassCombo;
		private readonly FwFeatureStructureEditor _inflFeaturesEditor;

		// The slots panel holds BOTH the slot combo and the secondary-POS chooser; only one shows at a time
		// (the WinForms m_slotsPanel holds m_fwcbSlots + m_tcSecondaryPOS the same way).
		private IReadOnlyList<FwInflectionSlot> _slots = Array.Empty<FwInflectionSlot>();

		// The inflection-class options (the &lt;None&gt; sentinel row + the selected main POS's classes). Re-fed by
		// the host whenever the main POS changes (mirroring how the slot list follows the POS).
		private IReadOnlyList<FwInflectionClass> _inflClasses = Array.Empty<FwInflectionClass>();
		// The sentinel "<None>" row (a null id), prepended so the empty pick is selectable (the legacy AddNotSureItem).
		private static readonly FwInflectionClass s_inflClassNone =
			new FwInflectionClass(null, FwAvaloniaDialogsStrings.MsaInflectionClassNone, 0);
		// The last main-POS id the inflection-class refresh observed, so a POS change can be detected for MainPosChanged.
		private string _lastMainPosId;

		private FwMsaType _msaType = FwMsaType.NotSet;
		private bool _suppressEvents;

		// The affix-type combo items, in WinForms order: Not Sure (0), Inflectional (1), Derivational (2).
		private static readonly string[] s_affixTypeItems =
		{
			FwAvaloniaDialogsStrings.MsaAffixTypeNotSure,
			FwAvaloniaDialogsStrings.MsaAffixTypeInflectional,
			FwAvaloniaDialogsStrings.MsaAffixTypeDerivational
		};

		public FwMsaGroupBox()
		{
			// A dense, bordered host (the group-box frame), following the shared density tokens.
			Background = FwAvaloniaDensity.PickerBackgroundBrush;
			BorderBrush = FwAvaloniaDensity.PickerBorderBrush;
			BorderThickness = new Thickness(1);
			CornerRadius = new CornerRadius(3);
			Padding = new Thickness(4);
			AutomationProperties.SetAutomationId(this, "MsaGroupBox");

			_affixTypeCombo = new ComboBox
			{
				ItemsSource = s_affixTypeItems,
				MinHeight = 0,
				MinWidth = FwAvaloniaDensity.DropdownMinWidth,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = FwAvaloniaDensity.PickerBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush
			};
			AutomationProperties.SetAutomationId(_affixTypeCombo, "MsaGroupBox.AffixType");
			AutomationProperties.SetName(_affixTypeCombo, FwAvaloniaDialogsStrings.MsaAffixTypeLabel);
			_affixTypeCombo.SelectionChanged += OnAffixTypeChanged;

			_mainPos = new FwPosChooser("MainPos", allowEmpty: true, emptyLabel: FwAvaloniaStrings.PosAny);
			_mainPos.SelectionChanged += _ => OnMainPosSelectionChanged();
			_mainPos.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke();

			_secondaryPos = new FwPosChooser("SecondaryPos", allowEmpty: true, emptyLabel: FwAvaloniaStrings.PosAny);
			_secondaryPos.SelectionChanged += _ => OnSelectionChanged();
			_secondaryPos.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke();

			_slotCombo = new ComboBox
			{
				MinHeight = 0,
				MinWidth = FwAvaloniaDensity.DropdownMinWidth,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = FwAvaloniaDensity.PickerBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FwInflectionSlot>(
					(slot, _) => new TextBlock
					{
						Text = slot?.Name ?? string.Empty,
						VerticalAlignment = VerticalAlignment.Center,
						Foreground = Brushes.Black
					})
			};
			AutomationProperties.SetAutomationId(_slotCombo, "MsaGroupBox.Slot");
			_slotCombo.SelectionChanged += (s, e) =>
			{
				if (!_suppressEvents)
					OnSelectionChanged();
			};

			// The inflection-class combo (Stage 6): the stem/root MSA's inflection class, populated from the selected
			// main POS's classes (incl. nested subclasses, indented by depth). The "<None>" sentinel row keeps the
			// empty pick selectable. Mirrors the slot combo's template + density.
			_inflClassCombo = new ComboBox
			{
				MinHeight = 0,
				MinWidth = FwAvaloniaDensity.DropdownMinWidth,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = FwAvaloniaDensity.PickerBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FwInflectionClass>(
					(cls, _) => new TextBlock
					{
						// Indent nested subclasses two spaces per depth so the hierarchy reads (the WinForms tree indents).
						Text = (cls == null ? string.Empty : new string(' ', cls.Depth * 2) + cls.Name),
						VerticalAlignment = VerticalAlignment.Center,
						Foreground = Brushes.Black
					})
			};
			AutomationProperties.SetAutomationId(_inflClassCombo, "MsaGroupBox.InflClass");
			AutomationProperties.SetName(_inflClassCombo, FwAvaloniaDialogsStrings.MsaInflectionClassLabel);
			_inflClassCombo.SelectionChanged += (s, e) =>
			{
				if (!_suppressEvents)
					OnSelectionChanged();
			};

			// The inflection-feature editor (Phase-1 §19b Stage 2): the LCModel-free FwFeatureStructureEditor, the
			// parity of the WinForms box's "Inflection Features" affordance. Shown only for infl/deriv affixes (where
			// the legacy box opens MsaInflectionFeatureListDlg). The host feeds it the POS's inflectable-feature
			// system; a value pick raises MsaChanged so the dialog's payload tracks the live assignment set. The
			// create-feature / create-value affordances forward to the box's events (Stage 3 wires the dialogs).
			_inflFeaturesEditor = new FwFeatureStructureEditor("MsaGroupBox.InflFeatures");
			_inflFeaturesEditor.AssignmentsChanged += _ =>
			{
				if (!_suppressEvents)
					OnSelectionChanged();
			};
			_inflFeaturesEditor.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			_inflFeaturesEditor.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);

			_affixTypeLabel = FieldLabel(FwAvaloniaDialogsStrings.MsaAffixTypeLabel, "MsaGroupBox.AffixTypeLabel");
			_mainCatLabel = FieldLabel(FwAvaloniaDialogsStrings.MsaCategoryLabel, "MsaGroupBox.CategoryLabel");
			_slotsLabel = FieldLabel(FwAvaloniaDialogsStrings.MsaFillsSlotLabel, "MsaGroupBox.SlotsLabel");
			_inflClassLabel = FieldLabel(FwAvaloniaDialogsStrings.MsaInflectionClassLabel, "MsaGroupBox.InflClassLabel");
			_inflFeaturesLabel = FieldLabel(FwAvaloniaDialogsStrings.MsaInflectionFeaturesLabel, "MsaGroupBox.InflFeaturesLabel");

			_affixTypePanel = ColumnPanel(_affixTypeLabel, _affixTypeCombo, "MsaGroupBox.AffixTypePanel");
			_mainCatPanel = ColumnPanel(_mainCatLabel, _mainPos, "MsaGroupBox.MainCatPanel");
			// The slots panel stacks the slot combo and the secondary-POS chooser; only one is visible per type.
			_slotsPanel = ColumnPanel(_slotsLabel, null, "MsaGroupBox.SlotsPanel");
			_slotsPanel.Children.Add(_slotCombo);
			_slotsPanel.Children.Add(_secondaryPos);
			_inflClassPanel = ColumnPanel(_inflClassLabel, _inflClassCombo, "MsaGroupBox.InflClassPanel");
			_inflFeaturesPanel = ColumnPanel(_inflFeaturesLabel, _inflFeaturesEditor, "MsaGroupBox.InflFeaturesPanel");

			var row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = FwAvaloniaDensity.GroupSeparation
			};
			row.Children.Add(_affixTypePanel);
			row.Children.Add(_mainCatPanel);
			row.Children.Add(_slotsPanel);
			row.Children.Add(_inflClassPanel);
			row.Children.Add(_inflFeaturesPanel);
			Child = row;

			// Seed the inflection-class combo with just the "<None>" sentinel so it is non-empty before the host feeds
			// the real classes.
			SetInflectionClasses(Array.Empty<FwInflectionClass>());

			ApplyMsaTypeLayout();
		}

		// ----- public seam (LCModel-free) -----

		/// <summary>
		/// Feeds the POS hierarchy (a flat, document-order, depth-tagged node list) to BOTH choosers — the
		/// host's project parts-of-speech list, identical to what <see cref="FwPosChooser.SetNodes"/> expects.
		/// </summary>
		public void SetPosNodes(IReadOnlyList<FwPosNode> nodes)
		{
			_mainPos.SetNodes(nodes ?? Array.Empty<FwPosNode>());
			_secondaryPos.SetNodes(nodes ?? Array.Empty<FwPosNode>());
		}

		/// <summary>
		/// Feeds the inflectional-affix slot options shown in the Slot combo (the host builds these from the
		/// main POS's affix slots). Re-applies the current <see cref="SlotId"/> if it is still present.
		/// </summary>
		public void SetSlots(IReadOnlyList<FwInflectionSlot> slots)
		{
			_slots = slots ?? Array.Empty<FwInflectionSlot>();
			var previouslySelected = (_slotCombo.SelectedItem as FwInflectionSlot)?.Id;
			_suppressEvents = true;
			try
			{
				_slotCombo.ItemsSource = _slots;
				_slotCombo.SelectedItem = previouslySelected == null
					? null
					: _slots.FirstOrDefault(s => s.Id == previouslySelected);
			}
			finally
			{
				_suppressEvents = false;
			}
		}

		/// <summary>
		/// Feeds the inflection-class options shown in the inflection-class picker (Stage 6) — the host builds these
		/// from the currently-selected main POS's <c>InflectionClassesOC</c> (incl. nested subclasses, depth-tagged).
		/// A leading "&lt;None&gt;" sentinel row (a null id) is always prepended so the empty pick stays selectable.
		/// Re-applies the current <see cref="InflectionClassId"/> if it is still present (otherwise falls back to
		/// "&lt;None&gt;", mirroring how the WinForms POS change invalidates a now-irrelevant inflection class).
		/// </summary>
		public void SetInflectionClasses(IReadOnlyList<FwInflectionClass> inflClasses)
		{
			var list = new List<FwInflectionClass> { s_inflClassNone };
			if (inflClasses != null)
				list.AddRange(inflClasses.Where(c => c != null));
			_inflClasses = list;

			var previouslySelected = (_inflClassCombo.SelectedItem as FwInflectionClass)?.Id;
			_suppressEvents = true;
			try
			{
				_inflClassCombo.ItemsSource = _inflClasses;
				_inflClassCombo.SelectedItem = previouslySelected == null
					? s_inflClassNone
					: _inflClasses.FirstOrDefault(c => c.Id == previouslySelected) ?? s_inflClassNone;
			}
			finally
			{
				_suppressEvents = false;
			}
		}

		/// <summary>
		/// Feeds the inflection-feature system shown in the inflection-feature editor (Phase-1 §19b Stage 2) — the
		/// host builds these <see cref="FwFeatureNode"/>s from the currently-selected main POS's
		/// <c>InflectableFeatsRC</c> (incl. its parent POSes', depth-tagged, document order — the lift of
		/// <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c>). Re-fed when the main POS changes, exactly how the
		/// slot list follows the POS. The editor shows them only for infl/deriv affixes. Re-applies the current
		/// <see cref="InflectionFeatures"/> if still present (dropped otherwise).
		/// </summary>
		public void SetInflectionFeatureNodes(IReadOnlyList<FwFeatureNode> nodes)
		{
			_inflFeaturesEditor.SetNodes(nodes ?? Array.Empty<FwFeatureNode>());
		}

		/// <summary>
		/// Seeds the inflection-feature assignments shown in the editor (Phase-1 §19b Stage 2) — the launcher builds
		/// these from the MSA's existing <c>IFsFeatStruc</c> (the lift of
		/// <c>FeatureStructureTreeView.PopulateTreeFromFeatureStructure</c>). Does NOT raise <see cref="MsaChanged"/>
		/// (the host's seed path).
		/// </summary>
		public void SetInflectionFeatureAssignments(IReadOnlyList<FwFeatureValueAssignment> assignments)
		{
			_inflFeaturesEditor.SetAssignments(assignments ?? Array.Empty<FwFeatureValueAssignment>());
		}

		/// <summary>Host callback after a successful create-feature flow (Stage 3): see <see cref="FwFeatureStructureEditor.AcceptCreatedFeature"/>.</summary>
		public void AcceptCreatedInflectionFeature(FwFeatureNode created, IReadOnlyList<FwFeatureNode> valueChildren = null)
			=> _inflFeaturesEditor.AcceptCreatedFeature(created, valueChildren);

		/// <summary>Host callback after a successful add-value flow (Stage 3): see <see cref="FwFeatureStructureEditor.AcceptCreatedValue"/>.</summary>
		public void AcceptCreatedInflectionFeatureValue(string closedFeatureId, FwFeatureNode createdValue)
			=> _inflFeaturesEditor.AcceptCreatedValue(closedFeatureId, createdValue);

		/// <summary>
		/// The current grammatical-info class, mirroring <c>MSAGroupBox.MSAType</c>. Setting it reconfigures
		/// which widgets are visible (show/hide + relayout) and re-titles the field labels — exactly as the
		/// WinForms box does. The setter does NOT raise <see cref="MsaChanged"/> (the host uses it to seed).
		/// </summary>
		public FwMsaType MsaType
		{
			get => _msaType;
			set
			{
				if (value == _msaType)
					return;
				_msaType = value;
				ApplyMsaTypeLayout();
			}
		}

		/// <summary>
		/// The main POS id (the &lt;Any&gt; pick is null). Setter seeds without raising <see cref="MsaChanged"/>.
		/// </summary>
		public string MainPosId
		{
			get => _mainPos.SelectedPosId;
			set
			{
				_mainPos.SelectedPosId = value;
				// Keep the change-detection baseline in sync with the seeded value so the FIRST user POS change fires
				// MainPosChanged (the seed itself must not).
				_lastMainPosId = _mainPos.SelectedPosId;
			}
		}

		/// <summary>
		/// The secondary ("changes to") POS id (derivational only). Setter seeds without raising <see cref="MsaChanged"/>.
		/// </summary>
		public string SecondaryPosId
		{
			get => _secondaryPos.SelectedPosId;
			set => _secondaryPos.SelectedPosId = value;
		}

		/// <summary>
		/// The selected inflectional-affix slot id (inflectional only), or null when none is selected. Setter
		/// seeds the combo selection without raising <see cref="MsaChanged"/>.
		/// </summary>
		public string SlotId
		{
			get => (_slotCombo.SelectedItem as FwInflectionSlot)?.Id;
			set
			{
				_suppressEvents = true;
				try
				{
					_slotCombo.SelectedItem = value == null
						? null
						: _slots.FirstOrDefault(s => s.Id == value);
				}
				finally
				{
					_suppressEvents = false;
				}
			}
		}

		/// <summary>
		/// The selected inflection-class id (stem/root only), or null when "&lt;None&gt;" is selected. Setter seeds
		/// the combo selection without raising <see cref="MsaChanged"/> (an unknown id falls back to "&lt;None&gt;").
		/// </summary>
		public string InflectionClassId
		{
			get => (_inflClassCombo.SelectedItem as FwInflectionClass)?.Id;
			set
			{
				_suppressEvents = true;
				try
				{
					_inflClassCombo.SelectedItem = value == null
						? s_inflClassNone
						: _inflClasses.FirstOrDefault(c => c.Id == value) ?? s_inflClassNone;
				}
				finally
				{
					_suppressEvents = false;
				}
			}
		}

		/// <summary>
		/// The current inflection-feature assignments (the flat <c>(closedFeatureId, valueId)</c> set the editor
		/// emitted), or empty when none chosen. Carried in the payload only for infl/deriv (see <see cref="SandboxMsa"/>).
		/// </summary>
		public IReadOnlyList<FwFeatureValueAssignment> InflectionFeatures => _inflFeaturesEditor.Assignments;

		/// <summary>
		/// The current selection as the LCModel-free payload (the mirror of <c>MSAGroupBox.SandboxMSA</c>):
		/// only the fields relevant to the current <see cref="MsaType"/> are populated. The inflection class is
		/// carried only for the STEM/ROOT MSA (the WinForms InsertEntryDlg sets it on the stem/deriv-step MSA); the
		/// inflection features are carried only for the INFLECTIONAL/DERIVATIONAL MSA (Phase-1 §19b Stage 2 — the
		/// WinForms box edits them via MsaInflectionFeatureListDlg over InflFeatsOA / FromMsFeaturesOA).
		/// </summary>
		public FwSandboxMsa SandboxMsa
		{
			get
			{
				switch (_msaType)
				{
					case FwMsaType.Stem:
					case FwMsaType.Root:
						return new FwSandboxMsa(_msaType, mainPosId: MainPosId, inflectionClassId: InflectionClassId);
					case FwMsaType.Unclassified:
						return new FwSandboxMsa(_msaType, mainPosId: MainPosId);
					case FwMsaType.Inflectional:
						return new FwSandboxMsa(_msaType, mainPosId: MainPosId, slotId: SlotId,
							inflectionFeatures: InflectionFeatures);
					case FwMsaType.Derivational:
						return new FwSandboxMsa(_msaType, mainPosId: MainPosId, secondaryPosId: SecondaryPosId,
							inflectionFeatures: InflectionFeatures);
					default:
						return new FwSandboxMsa(_msaType);
				}
			}
		}

		/// <summary>Raised whenever the user changes any populated field (POS pick, slot pick, or affix-type pick).</summary>
		public event Action<FwSandboxMsa> MsaChanged;

		/// <summary>
		/// Raised when the MAIN POS selection changes, carrying the new main-POS id (null for "&lt;Any&gt;"). The host
		/// re-supplies the inflection-class options (and slot options) for the new POS — the parity of the WinForms
		/// POS-change path that resets the inflection-class tree (InsertEntryDlg.POS setter). Fired before
		/// <see cref="MsaChanged"/>.
		/// </summary>
		public event Action<string> MainPosChanged;

		/// <summary>
		/// Forwards either chooser's "Create a new Part of Speech..." request to the host (Stage 3 wires the
		/// create-POS flow and calls back into the chooser via <see cref="AcceptCreatedMainPos"/> /
		/// <see cref="AcceptCreatedSecondaryPos"/>).
		/// </summary>
		public event Action CreateNewPosRequested;

		/// <summary>
		/// Forwards the hosted inflection-feature editor's inline "Create a new feature..." request (Phase-1 §19b
		/// Stage 2). The host opens its create-feature flow (Stage 3 wires the feature dialog) and calls
		/// <see cref="AcceptCreatedInflectionFeature"/>. The box performs NO create.
		/// </summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>
		/// Forwards the hosted inflection-feature editor's per-feature "Add a value..." request, carrying the closed
		/// feature's id (Phase-1 §19b Stage 2). The host opens its add-value flow (Stage 3) and calls
		/// <see cref="AcceptCreatedInflectionFeatureValue"/>. The box performs NO create.
		/// </summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>Host callback after a successful create-POS flow targeting the MAIN POS chooser.</summary>
		public void AcceptCreatedMainPos(FwPosNode created) => _mainPos.AcceptCreatedNode(created);

		/// <summary>Host callback after a successful create-POS flow targeting the SECONDARY POS chooser.</summary>
		public void AcceptCreatedSecondaryPos(FwPosNode created) => _secondaryPos.AcceptCreatedNode(created);

		// ----- test/host accessors for the widgets -----

		/// <summary>The Main POS chooser (always present; the only widget for stem/root). For tests/hosts.</summary>
		public FwPosChooser MainPosChooser => _mainPos;

		/// <summary>The Secondary POS chooser (visible only for derivational affixes). For tests/hosts.</summary>
		public FwPosChooser SecondaryPosChooser => _secondaryPos;

		/// <summary>The Affix Type combo (visible only for affix morph types). For tests/hosts.</summary>
		public ComboBox AffixTypeCombo => _affixTypeCombo;

		/// <summary>The Slot combo (visible only for inflectional affixes). For tests/hosts.</summary>
		public ComboBox SlotCombo => _slotCombo;

		/// <summary>The inflection-class combo (visible only for stem/root). For tests/hosts.</summary>
		public ComboBox InflectionClassCombo => _inflClassCombo;

		/// <summary>The affix-type column panel (label + combo). For tests.</summary>
		public Control AffixTypePanel => _affixTypePanel;

		/// <summary>The main-category column panel (label + Main POS). For tests.</summary>
		public Control MainCatPanel => _mainCatPanel;

		/// <summary>The slots column panel (label + Slot combo OR Secondary POS). For tests.</summary>
		public Control SlotsPanel => _slotsPanel;

		/// <summary>The inflection-class column panel (label + inflection-class combo; stem/root only). For tests.</summary>
		public Control InflectionClassPanel => _inflClassPanel;

		/// <summary>The inflection-feature column panel (label + feature editor; infl/deriv only). For tests.</summary>
		public Control InflectionFeaturesPanel => _inflFeaturesPanel;

		/// <summary>The inflection-feature editor (visible only for infl/deriv affixes). For tests/hosts.</summary>
		public FwFeatureStructureEditor InflectionFeaturesEditor => _inflFeaturesEditor;

		// ----- adaptive layout (mirrors MSAGroupBox.MSAType's switch) -----

		private void ApplyMsaTypeLayout()
		{
			_suppressEvents = true;
			try
			{
				switch (_msaType)
				{
					case FwMsaType.Root:
					case FwMsaType.Stem:
						// Main POS + Inflection Class, label "Category"; affix-type and slots hidden. The inflection
						// class is the stem/root MSA's class (the legacy InsertEntryDlg inflection-class affordance).
						// PARITY (§19b): the inflection-FEATURE editor is scoped to infl/deriv affixes (where the legacy
						// box's "Inflection Features" affordance lives), so it stays hidden for stem/root.
						_mainCatLabel.Text = FwAvaloniaDialogsStrings.MsaCategoryLabel;
						_affixTypePanel.IsVisible = false;
						_slotsPanel.IsVisible = false;
						_inflClassPanel.IsVisible = true;
						_inflFeaturesPanel.IsVisible = false;
						break;

					case FwMsaType.Unclassified:
						// Affix-Type (= Not sure) + Main POS ("Attaches to Category"); slots + inflection class/features
						// hidden (an unclassified affix carries no inflection features in the legacy box).
						_mainCatLabel.Text = FwAvaloniaDialogsStrings.MsaAttachesToCategoryLabel;
						_affixTypePanel.IsVisible = true;
						_slotsPanel.IsVisible = false;
						_inflClassPanel.IsVisible = false;
						_inflFeaturesPanel.IsVisible = false;
						_affixTypeCombo.SelectedIndex = 0; // Not Sure
						break;

					case FwMsaType.Inflectional:
						// Affix-Type + Main POS + Slot ("Fills Slot") + Inflection Features; secondary POS + inflection
						// class hidden. The inflection-feature editor is the parity of the WinForms box editing
						// IMoInflAffMsa.InflFeatsOA via MsaInflectionFeatureListDlg.
						_mainCatLabel.Text = FwAvaloniaDialogsStrings.MsaAttachesToCategoryLabel;
						_slotsLabel.Text = FwAvaloniaDialogsStrings.MsaFillsSlotLabel;
						_affixTypePanel.IsVisible = true;
						_slotsPanel.IsVisible = true;
						_slotCombo.IsVisible = true;
						_secondaryPos.IsVisible = false;
						_inflClassPanel.IsVisible = false;
						_inflFeaturesPanel.IsVisible = true;
						_affixTypeCombo.SelectedIndex = 1; // Inflectional
						break;

					case FwMsaType.Derivational:
						// Affix-Type + Main POS + Secondary POS ("Changes to Category") + Inflection Features; slot combo
						// + inflection class hidden. The inflection-feature editor edits the derivational FROM features
						// (IMoDerivAffMsa.FromMsFeaturesOA) — the surface PopulateTreeFromPosInEntry/legacy box exposes.
						// PARITY: the legacy derivational MSA carries from/to inflection classes AND from/to features;
						// §19b scopes the inflection-class picker to stem/root and the feature editor to the FROM
						// features, the common create/insert case.
						_mainCatLabel.Text = FwAvaloniaDialogsStrings.MsaAttachesToCategoryLabel;
						_slotsLabel.Text = FwAvaloniaDialogsStrings.MsaChangesToCategoryLabel;
						_affixTypePanel.IsVisible = true;
						_slotsPanel.IsVisible = true;
						_slotCombo.IsVisible = false;
						_secondaryPos.IsVisible = true;
						_inflClassPanel.IsVisible = false;
						_inflFeaturesPanel.IsVisible = true;
						_affixTypeCombo.SelectedIndex = 2; // Derivational
						break;

					default: // NotSet — show only Main POS so the box is never empty; inflection class/features hidden.
						_mainCatLabel.Text = FwAvaloniaDialogsStrings.MsaCategoryLabel;
						_affixTypePanel.IsVisible = false;
						_slotsPanel.IsVisible = false;
						_inflClassPanel.IsVisible = false;
						_inflFeaturesPanel.IsVisible = false;
						break;
				}
			}
			finally
			{
				_suppressEvents = false;
			}
		}

		// ----- affix-type combo (re-derives MsaType, mirrors HandleComboMSATypesChange) -----

		private void OnAffixTypeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressEvents)
				return;
			// Map the picked label back to an MsaType, like the WinForms HandleComboMSATypesChange.
			switch (_affixTypeCombo.SelectedIndex)
			{
				case 0:
					MsaType = FwMsaType.Unclassified;
					break;
				case 1:
					MsaType = FwMsaType.Inflectional;
					break;
				case 2:
					MsaType = FwMsaType.Derivational;
					break;
			}
			OnSelectionChanged();
		}

		// A Main POS selection change: raise MainPosChanged FIRST (so the host re-feeds the inflection-class / slot
		// options for the new POS via SetInflectionClasses / SetSlots) BEFORE MsaChanged snapshots the box. Mirrors the
		// WinForms POS-change path that resets the inflection-class tree (InsertEntryDlg.POS setter).
		private void OnMainPosSelectionChanged()
		{
			if (_suppressEvents)
				return;
			var posId = MainPosId;
			if (!string.Equals(posId, _lastMainPosId, StringComparison.Ordinal))
			{
				_lastMainPosId = posId;
				MainPosChanged?.Invoke(posId);
			}
			OnSelectionChanged();
		}

		private void OnSelectionChanged()
		{
			if (_suppressEvents)
				return;
			MsaChanged?.Invoke(SandboxMsa);
		}

		// ----- small layout helpers -----

		private static TextBlock FieldLabel(string text, string automationId)
		{
			var label = new TextBlock
			{
				Text = text,
				Foreground = FwAvaloniaDensity.LabelBrush,
				FontSize = FwAvaloniaDensity.LabelFontSize,
				Margin = new Thickness(0, 0, 0, FwAvaloniaDensity.RowSpacing)
			};
			AutomationProperties.SetAutomationId(label, automationId);
			return label;
		}

		// A label-over-widget column (the WinForms panel pattern), the widget added below the label. When
		// `widget` is null the caller adds the column's widgets itself (the slots panel holds two).
		private static StackPanel ColumnPanel(TextBlock label, Control widget, string automationId)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Spacing = FwAvaloniaDensity.RowSpacing,
				VerticalAlignment = VerticalAlignment.Top
			};
			AutomationProperties.SetAutomationId(panel, automationId);
			panel.Children.Add(label);
			if (widget != null)
				panel.Children.Add(widget);
			return panel;
		}
	}
}
