// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// A lightweight, LCModel-FREE node in the Part-of-Speech hierarchy fed to <see cref="FwPosChooser"/>.
	/// The host (Stage 3) builds these from the project's parts-of-speech possibility list in DOCUMENT
	/// ORDER, tagging each with its <see cref="Depth"/> (0 for a top-level POS, +1 per sub-POS nesting) —
	/// the same depth-folding seam <c>RegionChoiceOption</c> uses, so the chooser can rebuild the tree
	/// without any model reference. <see cref="Id"/> is an opaque stable identifier (a guid string in the
	/// product) the chooser round-trips verbatim; the chooser never interprets it.
	/// </summary>
	public sealed class FwPosNode
	{
		public FwPosNode(string id, string name, int depth = 0, string abbreviation = null)
		{
			Id = id;
			Name = name;
			Depth = depth < 0 ? 0 : depth;
			Abbreviation = abbreviation;
		}

		/// <summary>Opaque stable identifier (a guid string in the product); round-tripped verbatim.</summary>
		public string Id { get; }

		/// <summary>The display name shown on the row and (when selected) in the collapsed box.</summary>
		public string Name { get; }

		/// <summary>Hierarchy level: 0 for a top-level POS, +1 per sub-POS nesting, in document order.</summary>
		public int Depth { get; }

		/// <summary>Optional abbreviation (the legacy tree can show the abbr column); null when none.</summary>
		public string Abbreviation { get; }
	}

	/// <summary>
	/// A reusable, LCModel-FREE hierarchical Part-of-Speech chooser — the Avalonia replacement for the
	/// WinForms <c>TreeCombo</c> + <c>POSPopupTreeManager</c> pair (<c>MSAGroupBox</c>'s
	/// <c>m_tcMainPOS</c>/<c>m_tcSecondaryPOS</c>). It is a COLLAPSED dropdown (a toggle button showing
	/// the selected POS name, or a "not specified" prompt) that opens a hierarchical TREE popup ON TOP on
	/// focus/click, exactly like the MorphType dropdown mode of <see cref="Region.FwOptionPicker"/>. The
	/// popup offers, top to bottom:
	///   * an optional "&lt;Not sure&gt;"/"&lt;Any&gt;" empty row (when <paramref name="allowEmpty"/>),
	///   * a type-ahead filter box,
	///   * the parts-of-speech TREE (expand/collapse, keyboard nav), and
	///   * an inline "Create a new Part of Speech..." row that raises <see cref="CreateNewPosRequested"/>.
	///
	/// The seam is deliberately tiny: the host feeds a flat, depth-tagged <see cref="FwPosNode"/> list
	/// (the chooser folds it into the tree), the current <see cref="SelectedPosId"/>, and the allow-empty
	/// option. Picking a node commits + collapses + updates <see cref="SelectedPosId"/> and raises
	/// <see cref="SelectionChanged"/>. The chooser holds NO model reference and performs NO create — it
	/// just raises <see cref="CreateNewPosRequested"/> and accepts a returned new node (Stage 3 wires the
	/// actual create-POS flow). Built in pure C# (no XAML) to match the rest of FwAvalonia.
	/// </summary>
	public sealed class FwPosChooser : Border
	{
		private readonly string _automationId;
		private readonly bool _allowEmpty;
		private string _emptyLabelText;

		// Collapsed presentation (the field-sized box the user sees when the popup is closed).
		private readonly ToggleButton _dropdownButton;
		private readonly TextBlock _dropdownLabel;
		private readonly Popup _popup;

		// Popup content: a filter box stacked over EITHER the tree (no filter) or a flat result list (filter).
		private readonly TextBox _filterBox;
		private readonly TreeView _tree;
		private readonly ListBox _filterList;
		private readonly Border _createRow;
		private readonly Border _popupPanel;

		// The flat, depth-tagged source and the folded tree roots (rebuilt on SetNodes).
		private IReadOnlyList<FwPosNode> _nodes = Array.Empty<FwPosNode>();
		private readonly ObservableCollection<PosTreeNode> _roots = new ObservableCollection<PosTreeNode>();
		private readonly Dictionary<string, PosTreeNode> _byId =
			new Dictionary<string, PosTreeNode>(StringComparer.Ordinal);

		private string _selectedPosId;
		private bool _suppressTreeSelection;

		/// <param name="automationId">Stable, nonlocalized AutomationId stem (e.g. "MainPos").</param>
		/// <param name="allowEmpty">When true the popup leads with a selectable "not specified" row.</param>
		/// <param name="emptyLabel">The empty-row text; defaults to "&lt;Not sure&gt;". Pass
		/// <see cref="FwAvaloniaStrings.PosAny"/> for the MSAGroupBox "&lt;Any&gt;" variant.</param>
		public FwPosChooser(string automationId, bool allowEmpty = true, string emptyLabel = null)
		{
			_automationId = automationId ?? string.Empty;
			_allowEmpty = allowEmpty;
			_emptyLabelText = emptyLabel ?? FwAvaloniaStrings.PosNotSure;

			// The collapsed control is a transparent host so it sits cleanly inside an fwFieldHost frame;
			// the toggle button supplies the box look (mirrors FwOptionPicker dropdown mode).
			Background = Brushes.Transparent;
			BorderThickness = new Thickness(0);
			Padding = new Thickness(0);
			MinWidth = FwAvaloniaDensity.DropdownMinWidth;
			AutomationProperties.SetAutomationId(this, _automationId + ".PosChooser");
			AutomationProperties.SetName(this, FwAvaloniaStrings.PosChooserName);

			_dropdownLabel = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brushes.Black
			};
			var chevron = new TextBlock
			{
				Text = "▾", // ▾ collapsed-dropdown affordance
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(FwAvaloniaDensity.CheckboxLabelGap, 0, 0, 0),
				Foreground = Brushes.Gray
			};
			var buttonContent = new DockPanel { LastChildFill = true };
			DockPanel.SetDock(chevron, Dock.Right);
			buttonContent.Children.Add(chevron);
			buttonContent.Children.Add(_dropdownLabel);

			_dropdownButton = new ToggleButton
			{
				Content = buttonContent,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				Background = FwAvaloniaDensity.PickerBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(3)
			};
			AutomationProperties.SetAutomationId(_dropdownButton, _automationId + ".Dropdown");
			AutomationProperties.SetName(_dropdownButton, FwAvaloniaStrings.PosChooserName);
			_dropdownButton.IsCheckedChanged += OnDropdownButtonCheckedChanged;

			_filterBox = new TextBox
			{
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = Brushes.Transparent,
				BorderBrush = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Watermark = FwAvaloniaStrings.SearchPrompt
			};
			AutomationProperties.SetAutomationId(_filterBox, _automationId + ".Search");
			AutomationProperties.SetName(_filterBox, FwAvaloniaStrings.SearchPrompt);
			_filterBox.TextChanged += (s, e) => ApplyFilter();

			_tree = new TreeView
			{
				ItemsSource = _roots,
				MaxHeight = FwAvaloniaDensity.OptionListMaxHeight,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				ItemContainerTheme = CompactTreeItemTheme(),
				ItemTemplate = TreeNodeTemplate()
			};
			AutomationProperties.SetAutomationId(_tree, _automationId + ".Tree");
			_tree.SelectionChanged += OnTreeSelectionChanged;

			// The flat filtered list shown WHILE typing (the tree is hidden then): a contains match over
			// names, depth-indented so the user still reads the hierarchy level. Reuses the option-picker
			// row density. Focus stays in the filter box (the list never steals it).
			_filterList = new ListBox
			{
				Focusable = false,
				IsVisible = false,
				SelectionMode = SelectionMode.Single,
				MaxHeight = FwAvaloniaDensity.OptionListMaxHeight,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(0),
				ItemContainerTheme = CompactListItemTheme(),
				ItemTemplate = FilterRowTemplate()
			};
			AutomationProperties.SetAutomationId(_filterList, _automationId + ".Filtered");
			_filterList.AddHandler(InputElement.PointerReleasedEvent, OnFilterListPointerReleased,
				RoutingStrategies.Bubble, handledEventsToo: true);

			// The inline "Create a new Part of Speech..." affordance — the legacy "More..." item, pinned to
			// the bottom of the popup so it is always reachable regardless of filter/scroll.
			var createLabel = new TextBlock
			{
				Text = FwAvaloniaStrings.PosCreateNew,
				Foreground = FwAvaloniaDensity.LabelBrush,
				VerticalAlignment = VerticalAlignment.Center
			};
			_createRow = new Border
			{
				Background = Brushes.Transparent,
				Padding = FwAvaloniaDensity.OptionItemPadding,
				Margin = new Thickness(0, FwAvaloniaDensity.RowSpacing, 0, 0),
				BorderBrush = FwAvaloniaDensity.SliceRuleBrush,
				BorderThickness = new Thickness(0, 1, 0, 0),
				Child = createLabel,
				Cursor = new Cursor(StandardCursorType.Hand)
			};
			AutomationProperties.SetAutomationId(_createRow, _automationId + ".CreateNew");
			AutomationProperties.SetName(_createRow, FwAvaloniaStrings.PosCreateNew);
			_createRow.AddHandler(InputElement.PointerReleasedEvent, OnCreateRowPointerReleased,
				RoutingStrategies.Bubble, handledEventsToo: true);

			// Popup body: filter on top, create-row on the bottom, the tree/filter list fills the middle.
			var listHost = new Panel();
			listHost.Children.Add(_tree);
			listHost.Children.Add(_filterList);

			var body = new DockPanel { LastChildFill = true };
			DockPanel.SetDock(_filterBox, Dock.Top);
			DockPanel.SetDock(_createRow, Dock.Bottom);
			body.Children.Add(_filterBox);
			body.Children.Add(_createRow);
			body.Children.Add(listHost);

			_popupPanel = new Border
			{
				Background = FwAvaloniaDensity.PickerBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(3),
				Padding = new Thickness(4),
				MinWidth = FwAvaloniaDensity.DropdownMinWidth + 20,
				Child = body
			};
			// The popup renders in its OWN top-level (PopupRoot), so keys typed in the filter box do not
			// bubble to the chooser root. Register the navigation handler on the popup panel so
			// Up/Down/Enter/Escape work while it is open (same pattern as FwOptionPicker dropdown mode).
			_popupPanel.AddHandler(InputElement.KeyDownEvent, OnPopupKeyDown,
				RoutingStrategies.Bubble, handledEventsToo: true);

			_popup = new Popup
			{
				PlacementTarget = _dropdownButton,
				Placement = PlacementMode.Bottom,
				IsLightDismissEnabled = true,
				// Zero footprint when closed so the layout tripwire never sees it overlapping the button.
				Width = 0,
				Height = 0,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = _popupPanel
			};
			AutomationProperties.SetAutomationId(_popup, _automationId + ".Popup");
			_popup.Opened += OnPopupOpened;
			_popup.Closed += OnPopupClosed;

			var root = new Panel();
			root.Children.Add(_dropdownButton);
			root.Children.Add(_popup);
			Child = root;

			UpdateCollapsedLabel();
		}

		// ----- public seam -----

		/// <summary>
		/// Feeds the chooser its POS hierarchy: a flat, document-order, depth-tagged node list. The
		/// chooser folds it into the tree (a node attaches under the most recent node one depth shallower).
		/// Re-applies the current <see cref="SelectedPosId"/> (kept if still present, else cleared).
		/// </summary>
		public void SetNodes(IReadOnlyList<FwPosNode> nodes)
		{
			_nodes = nodes ?? Array.Empty<FwPosNode>();
			RebuildTree();
			// Re-resolve the selection against the new tree (may have been added/removed).
			ApplySelectionToTree();
			UpdateCollapsedLabel();
		}

		/// <summary>
		/// The selected POS id, or null when nothing/"not specified" is selected. Setting it updates the
		/// collapsed label and tree highlight WITHOUT raising <see cref="SelectionChanged"/> (that fires
		/// only on a user pick) — the host uses the setter to seed the initial value.
		/// </summary>
		public string SelectedPosId
		{
			get => _selectedPosId;
			set
			{
				_selectedPosId = value;
				ApplySelectionToTree();
				UpdateCollapsedLabel();
			}
		}

		/// <summary>The display name shown collapsed (the selected POS name, or the empty-row label).</summary>
		public string SelectedDisplayText => _dropdownLabel.Text ?? string.Empty;

		/// <summary>True when the tree popup is currently open.</summary>
		public bool IsOpen => _popup.IsOpen;

		/// <summary>The collapsed toggle button (the field-sized box the user clicks). For tests/hosts.</summary>
		public ToggleButton DropdownButton => _dropdownButton;

		/// <summary>The popup's type-ahead filter editor (focused on open). For tests.</summary>
		public TextBox FilterBox => _filterBox;

		/// <summary>The hierarchical POS tree (shown when not filtering). For tests.</summary>
		public TreeView Tree => _tree;

		/// <summary>The flat filtered result list (shown while typing). For tests.</summary>
		public ListBox FilteredList => _filterList;

		/// <summary>
		/// Detaches and returns the on-top popup CONTENT panel (filter + tree/filter-list + create row)
		/// for headless PNG capture only: a <see cref="Popup"/> renders in its own top-level, which the
		/// host window's CaptureRenderedFrame does not include, so a snapshot of the chooser shows only the
		/// collapsed box. Hosting THIS panel in a capture window renders the actual on-top tree the user
		/// sees. The chooser is left non-functional afterward (the popup is emptied), so call it on a
		/// throwaway instance — never in production.
		/// </summary>
		public Control DetachPopupContentForCapture()
		{
			_popup.Child = null;
			return _popupPanel;
		}

		/// <summary>Raised when the user PICKS a POS (or the empty row). Carries the new selected id (null
		/// for the empty/"not specified" pick). Not raised by the <see cref="SelectedPosId"/> setter.</summary>
		public event Action<string> SelectionChanged;

		/// <summary>
		/// Raised when the user clicks the inline "Create a new Part of Speech..." row. The host opens its
		/// create-POS flow and, on success, calls <see cref="AcceptCreatedNode"/> with the new node so the
		/// chooser adds and selects it. The chooser itself performs NO create.
		/// TODO Stage 3: the host wires this to MasterCategoryListDlg's Avalonia replacement (the
		/// create-POS sub-dialog). Stage 1 ships only the event + AcceptCreatedNode acceptance hook.
		/// </summary>
		public event Action CreateNewPosRequested;

		/// <summary>
		/// Host callback after a successful create-POS flow (Stage 3): appends the new node to the source
		/// list, rebuilds the tree, and selects it (raising <see cref="SelectionChanged"/>). The chooser
		/// stays LCModel-free — the host supplies the already-built <see cref="FwPosNode"/>.
		/// </summary>
		public void AcceptCreatedNode(FwPosNode created)
		{
			if (created == null)
				return;
			var merged = new List<FwPosNode>(_nodes) { created };
			_nodes = merged;
			RebuildTree();
			CommitSelection(created.Id, created.Name, raise: true);
			CloseDropdown();
		}

		/// <summary>Opens the tree popup (same effect as clicking the box). No-op if already open.</summary>
		public void Open()
		{
			_dropdownButton.IsChecked = true;
		}

		// ----- tree building (depth-fold, mirrors ChooserTreeBuilder) -----

		private void RebuildTree()
		{
			_roots.Clear();
			_byId.Clear();
			var lastAtDepth = new List<PosTreeNode>();
			foreach (var node in _nodes)
			{
				if (node == null)
					continue;
				var depth = node.Depth;
				var treeNode = new PosTreeNode(node);
				if (node.Id != null)
					_byId[node.Id] = treeNode;

				PosTreeNode parent = null;
				if (depth > 0)
				{
					for (var d = Math.Min(depth - 1, lastAtDepth.Count - 1); d >= 0; d--)
					{
						if (lastAtDepth[d] != null)
						{
							parent = lastAtDepth[d];
							break;
						}
					}
				}

				if (parent != null)
					parent.Children.Add(treeNode);
				else
					_roots.Add(treeNode);

				while (lastAtDepth.Count <= depth)
					lastAtDepth.Add(null);
				lastAtDepth[depth] = treeNode;
				for (var d = depth + 1; d < lastAtDepth.Count; d++)
					lastAtDepth[d] = null;
			}
		}

		// ----- collapsed label / selection state -----

		private void UpdateCollapsedLabel()
		{
			if (_selectedPosId != null && _byId.TryGetValue(_selectedPosId, out var node))
				_dropdownLabel.Text = node.Source.Name;
			else if (_allowEmpty)
				_dropdownLabel.Text = _emptyLabelText;
			else
				_dropdownLabel.Text = string.Empty;
		}

		private void ApplySelectionToTree()
		{
			_suppressTreeSelection = true;
			try
			{
				if (_selectedPosId != null && _byId.TryGetValue(_selectedPosId, out var node))
				{
					// Expand the chain to the selected node so it is visible when the popup opens.
					ExpandAncestors(node);
					_tree.SelectedItem = node;
				}
				else
				{
					_tree.SelectedItem = null;
				}
			}
			finally
			{
				_suppressTreeSelection = false;
			}
		}

		private void ExpandAncestors(PosTreeNode node)
		{
			// Walk the roots/children to expand every ancestor of the target (small lists; linear is fine).
			foreach (var r in _roots)
				ExpandTowards(r, node);
		}

		private static bool ExpandTowards(PosTreeNode current, PosTreeNode target)
		{
			if (ReferenceEquals(current, target))
				return true;
			foreach (var child in current.Children)
			{
				if (ExpandTowards(child, target))
				{
					current.IsExpanded = true;
					return true;
				}
			}
			return false;
		}

		private void CommitSelection(string id, string display, bool raise)
		{
			_selectedPosId = id;
			ApplySelectionToTree();
			_dropdownLabel.Text = display;
			if (raise)
				SelectionChanged?.Invoke(id);
		}

		// ----- dropdown open/close (focus-gated) -----

		private void OnDropdownButtonCheckedChanged(object sender, RoutedEventArgs e)
		{
			_popup.IsOpen = _dropdownButton.IsChecked == true;
		}

		private void OnPopupOpened(object sender, EventArgs e)
		{
			// Start each open with a clean filter (tree shown), then focus the filter box so typing filters
			// immediately. Posted at Input priority so it runs after the popup's own layout/template.
			if (!string.IsNullOrEmpty(_filterBox.Text))
				_filterBox.Text = string.Empty;
			else
				ApplyFilter(); // ensure the tree (not a stale filter list) is showing
			Avalonia.Threading.Dispatcher.UIThread.Post(() => _filterBox.Focus(),
				Avalonia.Threading.DispatcherPriority.Input);
		}

		private void OnPopupClosed(object sender, EventArgs e)
		{
			if (_dropdownButton.IsChecked == true)
				_dropdownButton.IsChecked = false;
		}

		private void CloseDropdown()
		{
			_popup.IsOpen = false;
			_dropdownButton.IsChecked = false;
		}

		// ----- filtering (tree when empty, flat contains-match list while typing) -----

		private IReadOnlyList<FwPosNode> _filterResults = Array.Empty<FwPosNode>();

		private void ApplyFilter()
		{
			var query = _filterBox.Text ?? string.Empty;
			if (string.IsNullOrWhiteSpace(query))
			{
				_filterResults = Array.Empty<FwPosNode>();
				_filterList.ItemsSource = null;
				_filterList.IsVisible = false;
				_tree.IsVisible = true;
				return;
			}

			_filterResults = _nodes
				.Where(n => n != null && n.Name != null
					&& n.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
				.ToList();
			_filterList.ItemsSource = _filterResults;
			_filterList.SelectedIndex = _filterResults.Count > 0 ? 0 : -1;
			_filterList.IsVisible = true;
			_tree.IsVisible = false;
		}

		// ----- selection gestures -----

		private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressTreeSelection)
				return;
			if (_tree.SelectedItem is PosTreeNode node)
			{
				// A click on a tree row commits + collapses (matching the legacy ByMouse AfterSelect).
				CommitSelection(node.Source.Id, node.Source.Name, raise: true);
				CloseDropdown();
			}
		}

		private void OnFilterListPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton != MouseButton.Left || !IsReleaseOverOwnItem(e.Source))
				return;
			CommitHighlightedFilterRow();
		}

		private bool IsReleaseOverOwnItem(object source)
		{
			var item = (source as Visual)?.GetSelfAndVisualAncestors()
				.OfType<ListBoxItem>().FirstOrDefault();
			return item != null && item.GetVisualAncestors().Contains(_filterList);
		}

		private void CommitHighlightedFilterRow()
		{
			var node = (_filterList.SelectedItem as FwPosNode) ?? _filterResults.FirstOrDefault();
			if (node == null)
				return;
			CommitSelection(node.Id, node.Name, raise: true);
			CloseDropdown();
		}

		private void OnCreateRowPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton != MouseButton.Left)
				return;
			RaiseCreateNew();
			e.Handled = true;
		}

		/// <summary>Raises <see cref="CreateNewPosRequested"/> (the host opens the create flow). For tests/hosts.</summary>
		public void RaiseCreateNew()
		{
			// The popup hides while the host runs its create flow (legacy hides the tree before the dialog).
			CloseDropdown();
			CreateNewPosRequested?.Invoke();
		}

		// ----- keyboard navigation (Down/Up/Enter/Escape) -----

		private void OnPopupKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					CloseDropdown();
					e.Handled = true;
					break;
				case Key.Enter:
					if (_filterList.IsVisible)
						CommitHighlightedFilterRow();
					else if (_tree.SelectedItem is PosTreeNode node)
					{
						CommitSelection(node.Source.Id, node.Source.Name, raise: true);
						CloseDropdown();
					}
					e.Handled = true;
					break;
				case Key.Down:
					if (_filterList.IsVisible)
					{
						MoveFilterHighlight(1);
						e.Handled = true;
					}
					// In tree mode, let the TreeView's own arrow-key navigation move the highlight.
					break;
				case Key.Up:
					if (_filterList.IsVisible)
					{
						MoveFilterHighlight(-1);
						e.Handled = true;
					}
					break;
			}
		}

		private void MoveFilterHighlight(int delta)
		{
			if (_filterResults.Count == 0)
				return;
			var current = _filterList.SelectedIndex;
			var next = current < 0 ? (delta > 0 ? 0 : _filterResults.Count - 1) : current + delta;
			if (next < 0 || next >= _filterResults.Count)
				return;
			_filterList.SelectedIndex = next;
			_filterList.ScrollIntoView(next);
		}

		// ----- item templates / density -----

		private IDataTemplate TreeNodeTemplate()
		{
			return new FuncTreeDataTemplate<PosTreeNode>(
				(node, _) =>
				{
					var label = new TextBlock
					{
						Text = node.Source.Name,
						VerticalAlignment = VerticalAlignment.Center,
						Foreground = Brushes.Black
					};
					AutomationProperties.SetAutomationId(label, _automationId + ".Node");
					AutomationProperties.SetName(label, node.Source.Name);
					return label;
				},
				node => node.Children);
		}

		private IDataTemplate FilterRowTemplate()
		{
			return new FuncDataTemplate<FwPosNode>((node, _) =>
			{
				if (node == null)
					return null;
				return new TextBlock
				{
					Text = node.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Black,
					// Keep the hierarchy readable even in the flat filter list, by depth indent.
					Margin = new Thickness(node.Depth * FwAvaloniaDensity.TreeIndentPerLevel, 0, 0, 0)
				};
			});
		}

		private static ControlTheme CompactTreeItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(TreeViewItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(TreeViewItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(TreeViewItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, 0d));
			// Bind each container's expansion to its node so ExpandAncestors/keyboard collapse work.
			theme.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
				new Avalonia.Data.Binding(nameof(PosTreeNode.IsExpanded)) { Mode = Avalonia.Data.BindingMode.TwoWay }));
			return theme;
		}

		private static ControlTheme CompactListItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(ListBoxItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(ListBoxItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(ListBoxItem.MinHeightProperty, 0d));
			return theme;
		}

		/// <summary>
		/// A tree node bound by the chooser's TreeView. Carries the expansion state (two-way bound to the
		/// container) and the children, mirroring ChooserTreeNode but POS-specific and LCModel-free.
		/// </summary>
		private sealed class PosTreeNode : Avalonia.AvaloniaObject
		{
			public static readonly DirectProperty<PosTreeNode, bool> IsExpandedProperty =
				AvaloniaProperty.RegisterDirect<PosTreeNode, bool>(nameof(IsExpanded),
					o => o._isExpanded, (o, v) => o._isExpanded = v);

			private bool _isExpanded;

			public PosTreeNode(FwPosNode source)
			{
				Source = source;
				Children = new ObservableCollection<PosTreeNode>();
			}

			public FwPosNode Source { get; }

			public ObservableCollection<PosTreeNode> Children { get; }

			public bool IsExpanded
			{
				get => _isExpanded;
				set => SetAndRaise(IsExpandedProperty, ref _isExpanded, value);
			}
		}
	}
}
