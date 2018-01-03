// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using LanguageExplorer.Works.DictionaryDetailsView;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// Controls the portion of the dialog where an element in a dictionary entry is configured in detail, including Writing Systems,
	/// Complex Form types, Lexical Relation types, Sense numbers, etc.
	/// This class does not control whether this particular element of the entry is displayed, nor any preview of the entry.
	/// </summary>
	public class DictionaryDetailsController
	{
		private readonly IPropertyTable m_propertyTable;
		private readonly LcmCache m_cache;
		private readonly LcmStyleSheet m_styleSheet;

		private List<StyleComboItem> m_charStyles;
		private List<StyleComboItem> m_paraStyles;

		/// <summary>ConfigurableDictionaryNode to model the dictionary element being configured</summary>
		private ConfigurableDictionaryNode m_node;

		/// <summary>The DictionaryConfigurationModel that owns the node being configured.</summary>
		private DictionaryConfigurationModel m_configModel;

		/// <summary>Model for options specific to the element type, such as writing systems or relation types</summary>
		private DictionaryNodeOptions Options { get { return m_node.DictionaryNodeOptions; } }

		/// <summary>The DetailsView controlled by this controller</summary>
		public IDictionaryDetailsView View { get; private set; }

		/// <summary>Fired whenever the model is changed, so that the dictionary preview can be refreshed</summary>
		public event EventHandler DetailsModelChanged;

		/// <summary>Fired whenever the Styles dialog makes changes that require the dictionary preview to be refreshed</summary>
		public event EventHandler StylesDialogMadeChanges;

		/// <summary>Fired whenever the selected node is changed, so that the node tree can be refreshed</summary>
		public event EventHandler SelectedNodeChanged;

		public DictionaryDetailsController(IDictionaryDetailsView view, IPropertyTable propertyTable)
		{
			// one-time setup
			m_propertyTable = propertyTable;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			LoadStylesLists();
			View = view;
		}

		#region LoadModel
		/// <summary>
		/// (Re)initializes the controller and view to configure the given node
		/// </summary>
		public void LoadNode(DictionaryConfigurationModel model, ConfigurableDictionaryNode node)
		{
			m_configModel = model;
			m_node = node;

			View.SuspendLayout();

			ResetView(View, node);

			// Populate Styles dropdown
			var isPara = m_node.StyleType == StyleTypes.Paragraph || m_node.Parent == null;
			View.SetStyles(isPara ? m_paraStyles : m_charStyles, m_node.Style, isPara);

			// Test for Options type
			UserControl optionsView = null;
			if (Options != null)
			{
				if (Options is DictionaryNodeWritingSystemAndParaOptions)
				{
					optionsView = LoadWsAndParaOptions((DictionaryNodeWritingSystemAndParaOptions)Options);
				}
				else if (Options is DictionaryNodeWritingSystemOptions)
				{
					optionsView = LoadWsOptions((DictionaryNodeWritingSystemOptions) Options);
				}
				else if (Options is DictionaryNodeSenseOptions)
				{
					optionsView = LoadSenseOptions((DictionaryNodeSenseOptions)Options, node.Parent != null && node.FieldDescription == node.Parent.FieldDescription,
						node.Parent != null && node.Parent.Label == "MainEntrySubsenses");
				}
				else if (Options is DictionaryNodeListOptions)
				{
					optionsView = LoadListOptions((DictionaryNodeListOptions) Options);
				}
				else if (Options is DictionaryNodeGroupingOptions)
				{
					optionsView = LoadGroupingOptions((DictionaryNodeGroupingOptions)Options);
				}
				else if (Options is DictionaryNodePictureOptions)
				{
					// todo: loading options here once UX has been worked out
				}
				else
				{
					throw new ArgumentException("Unrecognised type of DictionaryNodeOptions");
				}
			}
			else if ("MorphoSyntaxAnalysisRA".Equals(m_node.FieldDescription) && m_node.Parent.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				// Special Grammatical Info. options are needed only if the parent is Senses.
				optionsView = LoadGrammaticalInfoOptions();
			}
			// else, show only the default details (style, before, between, after)

			// Notify users of shared nodes
			if (node.ReferencedNode != null) //REVIEW: make sure ReferencedNodes always have no options
			{
				var nodePath = DictionaryConfigurationServices.BuildPathStringFromNode(node, false);
				if (node.IsMasterParent) // node is the Master Parent
				{
					var sharingParents = FindNodes(model.Parts, n => ReferenceEquals(node.ReferencedNode, n.ReferencedNode));
					var sharingParentsStringBuilder = new StringBuilder();
					foreach (var sharingParent in sharingParents.Where(s => !ReferenceEquals(node, s)))
						sharingParentsStringBuilder.Append(Environment.NewLine)
							.Append(DictionaryConfigurationServices.BuildPathStringFromNode(sharingParent, false));
					if (sharingParentsStringBuilder.Length > 0)
					{
						optionsView = new LabelOverPanel
						{
							PanelContents = optionsView,
							LabelText = xWorksStrings.ThisConfigurationIsShared,
							LabelToolTip = string.Format(xWorksStrings.SharesWithTheseNodes, nodePath, sharingParentsStringBuilder)
						};
					}
				}
				else // node is a Subordinate Parent
				{
					var masterParent = node.ReferencedNode.Parent;
					var masterParentPath = DictionaryConfigurationServices.BuildPathStringFromNode(masterParent, false);
					var goToView = new ButtonOverPanel
					{
						PanelContents = optionsView,
						ButtonText = xWorksStrings.ksConfigureNow,
						ButtonToolTip = string.Format(xWorksStrings.ClickToJumpTo, masterParentPath)
					};
					goToView.ButtonClicked += (sender, args) =>
					{
						if (SelectedNodeChanged != null)
							SelectedNodeChanged(masterParent, args);
					};
					optionsView = new LabelOverPanel
					{
						PanelContents = goToView,
						LabelText = xWorksStrings.ThisIsConfiguredElsewhere,
						LabelToolTip = string.Format(xWorksStrings.ksUsesTheSameConfigurationAs, nodePath, masterParentPath)
					};
				}
			}
			else
			{
				ConfigurableDictionaryNode masterParent;
				if (node.TryGetMasterParent(out masterParent)) // node is a shared descendant
				{
					optionsView = new LabelOverPanel
					{
						PanelContents = optionsView,
						LabelText = xWorksStrings.ThisConfigurationIsShared,
						LabelToolTip = string.Format(xWorksStrings.SeeAffectedNodesUnder,
							DictionaryConfigurationServices.BuildPathStringFromNode(masterParent, false))
					};
				}
			}

			View.OptionsView = optionsView;

			// Register eventhandlers
			View.StyleSelectionChanged += OnViewOnStyleSelectionChanged;
			View.StyleButtonClick += OnViewOnStyleButtonClick;
			View.BeforeTextChanged += OnViewOnBeforeTextChanged;
			View.BetweenTextChanged += OnViewOnBetweenTextChanged;
			View.AfterTextChanged += OnViewOnAfterTextChanged;

			View.ResumeLayout();
		}

		internal static IEnumerable<ConfigurableDictionaryNode> FindNodes(
			List<ConfigurableDictionaryNode> nodes, Func<ConfigurableDictionaryNode, bool> match)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			foreach (var node in nodes)
			{
				if (match(node))
					yield return node;
				if (node.IsMasterParent)
					foreach (var child in FindNodes(node.ReferencedOrDirectChildren, match))
						yield return child;
				else if (node.Children != null)
					foreach (var child in FindNodes(node.Children, match))
						yield return child;
			}
		}

		private void OnViewOnAfterTextChanged(object sender, EventArgs e)
		{
			AfterTextChanged();
		}

		private void OnViewOnBetweenTextChanged(object sender, EventArgs e)
		{
			BetweenTextChanged();
		}

		private void OnViewOnBeforeTextChanged(object sender, EventArgs e)
		{
			BeforeTextChanged();
		}

		private void OnViewOnStyleButtonClick(object sender, EventArgs e)
		{
			HandleStylesBtn((ComboBox)sender, View.Style);
		}

		private void OnViewOnStyleSelectionChanged(object sender, EventArgs e)
		{
			StyleChanged();
		}

		private void ResetView(IDictionaryDetailsView view, ConfigurableDictionaryNode node)
		{
			// Deregister event handlers before resetting view content to avoid unnecessary slow down
			view.StyleSelectionChanged -= OnViewOnStyleSelectionChanged;
			view.StyleButtonClick -= OnViewOnStyleButtonClick;
			view.BeforeTextChanged -= OnViewOnBeforeTextChanged;
			view.BetweenTextChanged -= OnViewOnBetweenTextChanged;
			view.AfterTextChanged -= OnViewOnAfterTextChanged;

			view.BeforeText = node.Before;
			view.BetweenText = node.Between;
			view.AfterText = node.After;
			view.Visible = true;
			view.StylesVisible = true;
			view.StylesEnabled = true;
			view.SurroundingCharsVisible = node.Parent != null; // top-level nodes don't need Surrounding Characters (Before, Between, After)
		}

		/// <summary>Initialize options for DictionaryNodeWritingSystemOptions</summary>
		private UserControl LoadWsOptions(DictionaryNodeWritingSystemOptions wsOptions)
		{
			var wsOptionsView = new ListOptionsView
			{
				DisplayOptionCheckBoxChecked = wsOptions.DisplayWritingSystemAbbreviations
			};

			var availableWSs = DictionaryConfigurationController.LoadAvailableWsList(wsOptions, m_cache); // REVIEW (Hasso) 2017.04: is this redundant to the model.Load sync?

			wsOptionsView.AvailableItems = availableWSs;

			// Displaying WS Abbreviations is available only when multiple WS's are selected.
			wsOptionsView.DisplayOptionCheckBoxEnabled = (availableWSs.Count(item => item.Checked) >= 2);
			wsOptionsView.DisplayOptionCheckBox2Visible = false;

			// Prevent events from firing while the view is being initialized
			wsOptionsView.Load += WritingSystemEventHandlerAdder(wsOptionsView, wsOptions);

			if (!m_node.IsHeadWord)
				return wsOptionsView;
			// show the Configure Headword Numbers... button
			var optionsView = new ButtonOverPanel { PanelContents = wsOptionsView };
			optionsView.ButtonClicked += (o, e) => HandleHeadwordNumbersButton();
			return optionsView;
		}


		private UserControl LoadWsAndParaOptions(DictionaryNodeWritingSystemAndParaOptions wsapoptions) // REVIEW (Hasso) 2017.04: reuse existing LoadWsOpts
		{
			var wsapOptionsView = new ListOptionsView
			{
				DisplayOptionCheckBoxChecked = wsapoptions.DisplayWritingSystemAbbreviations,
				DisplayOptionCheckBox2Checked = wsapoptions.DisplayEachInAParagraph
			};

			var availableWSs = DictionaryConfigurationController.LoadAvailableWsList(wsapoptions, m_cache);

			wsapOptionsView.AvailableItems = availableWSs;

			// Displaying WS Abbreviations is available only when multiple WS's are selected.
			wsapOptionsView.DisplayOptionCheckBoxEnabled = (availableWSs.Count(item => item.Checked) >= 2);

			wsapOptionsView.DisplayOptionCheckBox2Visible = true;
			wsapOptionsView.DisplayOptionCheckBox2Label = xWorksStrings.ksDisplayNoteInParagraphs;
			wsapOptionsView.DisplayOptionCheckBox2Checked = wsapoptions.DisplayEachInAParagraph;
			ToggleViewForShowInPara(wsapoptions.DisplayEachInAParagraph);

			// Prevent events from firing while the view is being initialized
			wsapOptionsView.Load += WritingSystemEventHandlerAdder(wsapOptionsView, wsapoptions);
			wsapOptionsView.Load += WritingSystemAndParaEventHandlerAdder(wsapOptionsView, wsapoptions);

			if (!m_node.IsHeadWord)
				return wsapOptionsView;
			// show the Configure Headword Numbers... button
			var optionsView = new ButtonOverPanel { PanelContents = wsapOptionsView };
			optionsView.ButtonClicked += (o, e) => HandleHeadwordNumbersButton();
			return optionsView;
		}

		private EventHandler WritingSystemAndParaEventHandlerAdder(IDictionaryListOptionsView wsapOptionsView, DictionaryNodeWritingSystemAndParaOptions wsapOptions)
		{
			return (o, args) =>
			{
				wsapOptionsView.DisplayOptionCheckBox2Changed += (sender, e) => DisplayInParaChecked(wsapOptionsView, wsapOptions);
				wsapOptionsView.Load -= WritingSystemAndParaEventHandlerAdder(wsapOptionsView, wsapOptions);
			};
		}

		private void DisplayInParaChecked(IDictionaryListOptionsView wsapOptionsView,
			DictionaryNodeWritingSystemAndParaOptions wsapOptions)
		{
			wsapOptions.DisplayEachInAParagraph = wsapOptionsView.DisplayOptionCheckBox2Checked;
			m_node.Style = ParagraphStyleForSubentries(wsapOptions.DisplayEachInAParagraph, m_node.FieldDescription);
			ToggleViewForShowInPara(wsapOptions.DisplayEachInAParagraph);
			RefreshPreview();
		}

		private EventHandler WritingSystemEventHandlerAdder(IDictionaryListOptionsView wsOptionsView, DictionaryNodeWritingSystemOptions wsOptions)
		{
			return (o, args) =>
			{
				wsOptionsView.UpClicked += (sender, e) =>
					Reorder(wsOptionsView.AvailableItems.First(item => item.Selected), Direction.Up);
				wsOptionsView.DownClicked += (sender, e) =>
					Reorder(wsOptionsView.AvailableItems.First(item => item.Selected), Direction.Down);
				wsOptionsView.ListItemSelectionChanged += (sender, e) => ListViewSelectionChanged(wsOptionsView, e);
				wsOptionsView.ListItemCheckBoxChanged += (sender, e) => ListItemCheckedChanged(wsOptionsView, wsOptions, e);
				wsOptionsView.DisplayOptionCheckBoxChanged += (sender, e) =>
				{
					wsOptions.DisplayWritingSystemAbbreviations = wsOptionsView.DisplayOptionCheckBoxChecked;
					RefreshPreview();
				};

				wsOptionsView.Load -= WritingSystemEventHandlerAdder(wsOptionsView, wsOptions);
			};
		}

		/// <summary>
		/// Initialize options for DictionaryNodeSenseOptions
		/// </summary>
		private UserControl LoadSenseOptions(DictionaryNodeSenseOptions senseOptions, bool isSubsense, bool isSubSubsense)
		{
			// initialize SenseOptionsView
			//For senses disallow the 1 1.2 1.2.3 option, that is now handled in subsenses
			var disallowedNumberingStyles = "%O";
			var senseOptionsView = new SenseOptionsView(isSubsense)
			{
				BeforeText = senseOptions.BeforeNumber,
				// load list of available NumberingStyles before setting NumberingStyle's value
				NumberingStyles = disallowedNumberingStyles == string.Empty
									? XmlVcDisplayVec.SupportedNumberingStyles
									: XmlVcDisplayVec.SupportedNumberingStyles.Where(prop => prop.FormatString != disallowedNumberingStyles).ToList(),
				NumberingStyle = senseOptions.NumberingStyle,
				ParentSenseNumberingStyleVisible = false,
				AfterText = senseOptions.AfterNumber,
				NumberSingleSense = senseOptions.NumberEvenASingleSense,
				ShowGrammarFirst = senseOptions.ShowSharedGrammarInfoFirst,
				SenseInPara = senseOptions.DisplayEachSenseInAParagraph,
				FirstSenseInline = senseOptions.DisplayFirstSenseInline
			};

			if (isSubsense)
			{
				senseOptionsView.ParentSenseNumberingStyleVisible = true;
				senseOptionsView.ParentSenseNumberingStyles = XmlVcDisplayVec.SupportedParentSenseNumberStyles;
				senseOptionsView.ParentSenseNumberingStyle = senseOptions.ParentSenseNumberingStyle;
			}
			// load character Style (number) and paragraph Style (sense)
			senseOptionsView.SetStyles(m_charStyles, senseOptions.NumberStyle);
			View.SetStyles(m_paraStyles, m_node.Style, true);

			// (dis)actviate appropriate parts of the view
			senseOptionsView.NumberMetaConfigEnabled = !string.IsNullOrEmpty(senseOptions.NumberingStyle);
			ToggleViewForShowInPara(senseOptions.DisplayEachSenseInAParagraph);
			senseOptionsView.FirstSenseInlineVisible = senseOptions.DisplayEachSenseInAParagraph;

			// Register eventhandlers
			senseOptionsView.BeforeTextChanged += (sender, e) => { senseOptions.BeforeNumber = senseOptionsView.BeforeText; RefreshPreview(); };
			senseOptionsView.NumberingStyleChanged += (sender, e) => SenseNumbingStyleChanged(senseOptions, senseOptionsView, isSubsense, isSubSubsense);
			senseOptionsView.AfterTextChanged += (sender, e) => { senseOptions.AfterNumber = senseOptionsView.AfterText; RefreshPreview(); };
			senseOptionsView.NumberStyleChanged += (sender, e) => { senseOptions.NumberStyle = senseOptionsView.NumberStyle; RefreshPreview(); };
			senseOptionsView.ParentSenseNumberingStyleChanged += (sender, e) => ParentSenseNumbingStyleChanged(senseOptions, senseOptionsView, isSubsense, isSubSubsense);
			// ReSharper disable ImplicitlyCapturedClosure
			// Justification: senseOptions, senseOptionsView, and all of these lambda functions will all disappear at the same time.
			senseOptionsView.StyleButtonClick += (sender, e) => HandleStylesBtn((ComboBox)sender, senseOptionsView.NumberStyle);
			// ReSharper restore ImplicitlyCapturedClosure
			senseOptionsView.NumberSingleSenseChanged += (sender, e) =>
			{
				senseOptions.NumberEvenASingleSense = senseOptionsView.NumberSingleSense;
				RefreshPreview();
			};
			senseOptionsView.ShowGrammarFirstChanged += (sender, e) =>
			{
				senseOptions.ShowSharedGrammarInfoFirst = senseOptionsView.ShowGrammarFirst;
				RefreshPreview();
			};
			senseOptionsView.SenseInParaChanged += (sender, e) => SenseInParaChanged(senseOptions, senseOptionsView);
			senseOptionsView.FirstSenseInlineChanged += (sender, e) =>
			{
				senseOptions.DisplayFirstSenseInline = senseOptionsView.FirstSenseInline;
				RefreshPreview();
			};

			// add senseOptionsView to the DetailsView
			return senseOptionsView;
		}

		/// <summary>Initialize options for DictionaryNodeListOptions other than WritingSystem options</summary>
		private UserControl LoadListOptions(DictionaryNodeListOptions listOptions)
		{
			var listOptionsView = new ListOptionsView();

			var listAndParaOptions = listOptions as DictionaryNodeListAndParaOptions;
			if (listAndParaOptions == null)
			{
				// DictionaryNodeListAndParaOptions makes use of the Display Option CheckBox below the list; regular List Options do not.
				listOptionsView.DisplayOptionCheckBoxVisible = false;
			}
			else
			{
				LoadParagraphOptions(listAndParaOptions, listOptionsView);
			}
			listOptionsView.DisplayOptionCheckBox2Visible = false;
			InternalLoadList(listOptions, listOptionsView);

			// Prevent events from firing while the view is being initialized
			listOptionsView.Load += ListEventHandlerAdder(listOptionsView, listOptions);

			return listOptionsView;
		}

		private void InternalLoadList(DictionaryNodeListOptions listOptions, IDictionaryListOptionsView listOptionsView)
		{
			if (listOptions.ListId == DictionaryNodeListOptions.ListIds.None)
			{
				listOptionsView.ListViewVisible = false;
			}
			else
			{
				string label;
				// REVIEW (Hasso) 2017.04: verifying available options is already accomplished in model.Load; here it is redundant.
				var availableOptions = GetListItemsAndLabel(listOptions.ListId, out label);
				listOptionsView.ListViewLabel = label;

				// Insert saved items in their saved order, with their saved check-state
				int insertionIdx = 0;
				foreach (var optn in listOptions.Options)
				{
					var savedItem = availableOptions.FirstOrDefault(item => optn.Id.Equals((item.Tag)));
					if (savedItem != null && availableOptions.Remove(savedItem))
					{
						savedItem.Checked = optn.IsEnabled;
						availableOptions.Insert(insertionIdx++, savedItem);
					}
				}

				listOptionsView.AvailableItems = availableOptions;
			}
		}

		private void LoadParagraphOptions(DictionaryNodeListAndParaOptions listAndParaOptions, IDictionaryListOptionsView listOptionsView)
		{
			listOptionsView.DisplayOptionCheckBoxLabel = xWorksStrings.ksDisplayComplexFormsInParagraphs;

			if (m_node.FieldDescription == "Subentries" || m_node.FieldDescription == "SubentriesOS")
			{
				listOptionsView.DisplayOptionCheckBoxLabel = xWorksStrings.ksDisplaySubentriesInParagraphs;
			}
			else if (m_node.FieldDescription == "ExamplesOS")
			{
				listOptionsView.DisplayOptionCheckBoxLabel = xWorksStrings.ksDisplayExamplesInParagraphs;
			}
			else if (m_node.FieldDescription == "ExtendedNoteOS")
			{
				listOptionsView.DisplayOptionCheckBoxLabel = xWorksStrings.ksDisplayExtendedNoteInParagraphs;
			}
			listOptionsView.DisplayOptionCheckBoxChecked = listAndParaOptions.DisplayEachInAParagraph;
			ToggleViewForShowInPara(listAndParaOptions.DisplayEachInAParagraph);
		}

		private EventHandler ListEventHandlerAdder(IDictionaryListOptionsView listOptionsView, DictionaryNodeListOptions listOptions)
		{
			return (o, args) =>
			{
				if (listOptions.ListId != DictionaryNodeListOptions.ListIds.None)
				{
					listOptionsView.UpClicked += (sender, e) =>
						Reorder(listOptionsView.AvailableItems.First(item => item.Selected), Direction.Up);
					listOptionsView.DownClicked += (sender, e) =>
						Reorder(listOptionsView.AvailableItems.First(item => item.Selected), Direction.Down);
					listOptionsView.ListItemSelectionChanged += (sender, e) => ListViewSelectionChanged(listOptionsView, e);
					listOptionsView.ListItemCheckBoxChanged += (sender, e) => ListItemCheckedChanged(listOptionsView, null, e);
				}

				var listAndParaOptions = listOptions as DictionaryNodeListAndParaOptions;
				if (listAndParaOptions != null)
				{
					listOptionsView.DisplayOptionCheckBoxChanged += (sender, e) =>
					{
						listAndParaOptions.DisplayEachInAParagraph = listOptionsView.DisplayOptionCheckBoxChecked;
						m_node.Style = ParagraphStyleForSubentries(listAndParaOptions.DisplayEachInAParagraph, m_node.FieldDescription);
						ToggleViewForShowInPara(listAndParaOptions.DisplayEachInAParagraph);
						RefreshPreview();
					};
				}

				listOptionsView.Load -= ListEventHandlerAdder(listOptionsView, listOptions);
			};
		}

		private UserControl LoadGroupingOptions(DictionaryNodeGroupingOptions options)
		{
			var groupOptionsView = new GroupingOptionsView
			{
				Description = options.Description,
				DisplayInParagraph = options.DisplayEachInAParagraph
			};
			ToggleViewForShowInPara(options.DisplayEachInAParagraph);
			groupOptionsView.Load += GroupingEventHandlerAdder(groupOptionsView, options);
			return groupOptionsView;
		}

		private EventHandler GroupingEventHandlerAdder(IDictionaryGroupingOptionsView groupOptionsView, DictionaryNodeGroupingOptions groupOptions)
		{
			return (o, args) =>
			{
				groupOptionsView.DisplayInParagraphChanged += (sender, e) =>
				{
					groupOptions.DisplayEachInAParagraph = groupOptionsView.DisplayInParagraph;
					ToggleViewForShowInPara(groupOptions.DisplayEachInAParagraph);
					RefreshPreview();
				};

				groupOptionsView.DescriptionChanged += (sender, e) =>
				{
					groupOptions.Description = groupOptionsView.Description;
				};
				groupOptionsView.Load -= GroupingEventHandlerAdder(groupOptionsView, groupOptions);
			};
		}

		private static string ParagraphStyleForSubentries(bool showInParagraph, string field)
		{
			string styleName = null;
			if (showInParagraph)
			{
				if (field == "SubentriesOS") // only Reversal Subentries use SubentriesOS
					styleName = "Reversal-Subentry";
				else if (field == "ExamplesOS" || DictionaryConfigurationModel.NoteInParaStyles.Contains(field))
					styleName = "Bulleted List";
				else if (field == "ExtendedNoteOS" || field == "SensesOS")
					styleName = "Dictionary-Sense";
				else
					styleName = "Dictionary-Subentry";
			}
			return styleName;
		}

		private UserControl LoadGrammaticalInfoOptions()
		{
			var optionsView = new ListOptionsView
			{
				ListViewVisible = false,
				DisplayOptionCheckBoxLabel = SenseOptionsView.ksShowGrammarFirst,
				DisplayOptionCheckBox2Visible = false
			};

			// The option to show grammatical info first is stored on the Sense node, which should be Grammatical Info's direct parent
			var senseOptions = (DictionaryNodeSenseOptions)m_node.Parent.DictionaryNodeOptions;

			optionsView.DisplayOptionCheckBoxChecked = senseOptions.ShowSharedGrammarInfoFirst;

			optionsView.DisplayOptionCheckBoxChanged += (sender, e) =>
			{
				senseOptions.ShowSharedGrammarInfoFirst = optionsView.DisplayOptionCheckBoxChecked;
				RefreshPreview();
			};

			return optionsView;
		}

		#region Load more-static parts

		private void LoadStylesLists()
		{
			if (m_charStyles == null)
				m_charStyles = new List<StyleComboItem>();
			else
				m_charStyles.Clear();
			if (m_paraStyles == null)
				m_paraStyles = new List<StyleComboItem>();
			else
				m_paraStyles.Clear();

			m_charStyles.Add(new StyleComboItem(null));
			// Per LT-10950, we don't want 'none' as an option for paragraph style, so don't add null to ParaStyles
			foreach (var style in m_styleSheet.Styles)
			{
				if (style.IsCharacterStyle)
					m_charStyles.Add(new StyleComboItem(style));
				else if (style.IsParagraphStyle)
					m_paraStyles.Add(new StyleComboItem(style));
			}

			m_charStyles.Sort();
			m_paraStyles.Sort();
		}

		/// <summary>
		/// Creates an Action for the Styles dialog to run to fix the Styles in the Model [and Combobox] and cause a refresh.
		/// StylesDialogMadeChanges tells the main controller to check all Styles in the Model, refresh, and register that a change has been saved.
		/// View.SetStyles changes the selected Style in the Combo, triggering a refresh and register that a change has been made but not saved.
		/// </summary>
		private Action FixStyles(bool repopulate)
		{
			return () =>
			{
				RefreshStylesAndPreview();
				if (!repopulate)
					return;
			LoadStylesLists();
				var isPara = m_node.StyleType == StyleTypes.Paragraph;
				View.SetStyles(isPara ? m_paraStyles : m_charStyles, m_node.Style, isPara);
			};
		}

		/// <summary>
		/// Get the list label and ListViewItems for the given List ID.
		/// Each item's Tag is a String representation of the item's GUID (with forward and reverse flags on applicable lex relations).
		/// Items are checked by default.
		/// </summary>
		internal List<ListViewItem> GetListItemsAndLabel(DictionaryNodeListOptions.ListIds listId, out string listLabel)
		{
			switch (listId)
			{
				case DictionaryNodeListOptions.ListIds.Minor:
					listLabel = xWorksStrings.ksMinorEntryTypes;
					return GetMinorEntryTypes();
				case DictionaryNodeListOptions.ListIds.Complex:
					listLabel = xWorksStrings.ksComplexFormTypes;
					return GetComplexFormTypes();
				case DictionaryNodeListOptions.ListIds.Note:
					listLabel = xWorksStrings.ksExtendedNoteTypes;
					return GetNoteTypes();
				case DictionaryNodeListOptions.ListIds.Variant:
					listLabel = xWorksStrings.ksVariantTypes;
					return GetVariantTypes();
				case DictionaryNodeListOptions.ListIds.Sense:
				case DictionaryNodeListOptions.ListIds.Entry:
					listLabel = xWorksStrings.ksLexicalRelationTypes;
					return GetLexicalRelationTypes(listId);
				default:
					throw new ArgumentException("Unrecognised List ID: " + listId);
			}
		}
		// REVIEW (Hasso) 2017.04: clean up some of this boilerplate code (GetXxxTypes) and move to DictionaryConfigurationController or DictionaryModelLoad(er|Controller)
		private List<ListViewItem> GetMinorEntryTypes()
		{
			var result = GetVariantTypes();
			// TODO pH 2014.05: AddRange iff this is Root-Based (not Lexeme-Based)
			result.AddRange(GetComplexFormTypes());
			return result;
		}

		private List<ListViewItem> GetComplexFormTypes()
		{
			var result = FlattenSortAndConvertList(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA);
			result.Insert(0, new ListViewItem("<" + xWorksStrings.ksNoComplexFormType + ">")
			{
				Checked = true,
				Tag = XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString()
			});
			return result;
		}

		private List<ListViewItem> GetNoteTypes()
		{
			var result = FlattenSortAndConvertList(m_cache.LangProject.LexDbOA.ExtendedNoteTypesOA);
			result.Insert(0, new ListViewItem("<" + xWorksStrings.ksNoExtendedNoteType + ">")
			{
				Checked = true,
				Tag = XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType().ToString()
			});
			return result;
		}

		private List<ListViewItem> GetVariantTypes()
		{
			var result = FlattenSortAndConvertList(m_cache.LangProject.LexDbOA.VariantEntryTypesOA);
			result.Insert(0, new ListViewItem("<" + xWorksStrings.ksNoVariantType + ">")
			{
				Checked = true,
				Tag = XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString()
			});
			return result;
		}

		/// <summary>Flattens hierarchy, sorts by name, and converts to ListViewItems</summary>
		private static List<ListViewItem> FlattenSortAndConvertList(ICmPossibilityList sequence)
		{
			var result = sequence.ReallyReallyAllPossibilities.ToList(); // flatten list
			result.Sort(ComparePossibilitiesByName);
			return result.Select(item => new ListViewItem(item.Name.BestAnalysisVernacularAlternative.Text)
			{
				Checked = true,
				Tag = item.Guid.ToString()
			}).ToList();
		}

		// REVIEW (Hasso) 2014.05: This method is currently optimised for loading and caching both Sense and Entry lists at once.
		// REVIEW (Hasso) 2017.04: Two years later, is the above comment still the case? Consider before moving to
		// REVIEW (continued): DictionaryConfigurationController or DictionaryModelLoad(er|Controller)
		private List<ListViewItem> GetLexicalRelationTypes(DictionaryNodeListOptions.ListIds listId)
		{
			var lexRelTypesSubset = new List<ListViewItem>();

			var allRelationTypes = m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.ToList();
			allRelationTypes.Sort(ComparePossibilitiesByName);
			foreach (var relType in allRelationTypes)
			{
				var listViewItemS = new List<ListViewItem>();
				var lexRelType = (ILexRefType)relType;
				var mappingType = (LexRefTypeTags.MappingTypes)lexRelType.MappingType;
				if (LexRefTypeTags.IsAsymmetric(mappingType))
				{
					listViewItemS.Add(new ListViewItem(lexRelType.Name.BestAnalysisVernacularAlternative.Text)
					{
						Checked = true,
						// REVIEW (Hasso) 2014.05: is there a less-expensive, equally-robust, way of appending the direction marker (:f or :r)?
						Tag = new LexReferenceInfo(true, relType.Guid)
						{
							SubClass = LexReferenceInfo.TypeSubClass.Forward
						}.StorageString.Substring(1) // substring removes the leading "+";
					});
					if (!LexRefTypeTags.IsUnidirectional(mappingType))
					{
						listViewItemS.Add(new ListViewItem(lexRelType.ReverseName.BestAnalysisVernacularAlternative.Text)
						{
							Checked = true,
							Tag = new LexReferenceInfo(true, relType.Guid)
							{
								SubClass = LexReferenceInfo.TypeSubClass.Reverse
							}.StorageString.Substring(1)
						});
					}
				}
				else
				{
					listViewItemS.Add(new ListViewItem(lexRelType.Name.BestAnalysisVernacularAlternative.Text)
					{
						Checked = true,
						Tag = new LexReferenceInfo(true, relType.Guid)
						{
							SubClass = LexReferenceInfo.TypeSubClass.Normal
						}.StorageString.Substring(1)
					});
				}

				switch (mappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
						if (listId == DictionaryNodeListOptions.ListIds.Entry)
							lexRelTypesSubset.AddRange(listViewItemS);
						break;
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
						if (listId == DictionaryNodeListOptions.ListIds.Sense)
							lexRelTypesSubset.AddRange(listViewItemS);
						break;
					default:
						lexRelTypesSubset.AddRange(listViewItemS);
						break;
				}
			}

			return lexRelTypesSubset;
		}

		// REVIEW pH 2014.05: we could convert to ListViewItems first, if we don't need forward and reverse lex relations to be in pairs
		private static int ComparePossibilitiesByName(ICmPossibility x, ICmPossibility y)
		{
			if (x == null)
				return y == null ? 0 : -1;
			if (y == null)
				return 1;
			var xName = x.Name.BestAnalysisVernacularAlternative.Text;
			var yName = y.Name.BestAnalysisVernacularAlternative.Text;
			if (xName == null)
				return yName == null ? 0 : -1;
			if (yName == null)
				return 1;
			return string.Compare(xName, yName, StringComparison.InvariantCulture);
		}
		#endregion Load more-static parts
		#endregion LoadModel

		#region HandleChanges
		private void RefreshPreview()
		{
			if (DetailsModelChanged != null)
				DetailsModelChanged(m_node, new EventArgs());
		}

		private void RefreshStylesAndPreview()
		{
			if (StylesDialogMadeChanges != null)
				StylesDialogMadeChanges(m_node, new EventArgs());
		}

		private void HandleHeadwordNumbersButton()
		{
			using (var dlg = new HeadwordNumbersDlg())
			{
				var controller = new HeadwordNumbersController(dlg, m_configModel, m_cache);
				// ReSharper disable once AccessToDisposedClosure - can only be used before the dialog is disposed
				dlg.RunStylesDialog += (sender, e) => HandleStylesBtn((ComboBox) sender, ((ComboBox)sender).Text);
				dlg.SetupDialog(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
				dlg.SetStyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
				//dlg.StartPosition = FormStartPosition.CenterScreen;
				if (dlg.ShowDialog(View.TopLevelControl) != DialogResult.OK)
					return;
				controller.Save();
				RefreshPreview();
			}
		}

		/// <summary>
		/// Run the Styles dialog for the Styles Combo. If the Combo is enabled, update its selected value.
		/// </summary>
		private void HandleStylesBtn(ComboBox combo, string defaultStyle)
		{
#if RANDYTODO
			// TODO: Needs to be able to create FlexStylesXmlAccessor, which is now in LangExp.
			// TODO: Enable after xWorks is merged into Lang Exp.
			// If the combo is not enabled, don't allow the Styles dialog to change it (pass null instead). FixStyles will ensure a refresh.
			FwStylesDlg.RunStylesDialogForCombo(combo.Enabled ? combo : null, FixStyles(combo.Enabled),
				defaultStyle, m_styleSheet, 0, 0, m_cache, View.TopLevelControl, m_propertyTable.GetValue<IApp>("App"),
				m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), new FlexStylesXmlAccessor(m_cache.LanguageProject.LexDbOA).SetPropsToFactorySettings);
#endif
		}

		private void BeforeTextChanged()
		{
			m_node.Before = View.BeforeText;
			RefreshPreview();
		}

		private void BetweenTextChanged()
		{
			m_node.Between = View.BetweenText;
			RefreshPreview();
		}

		private void AfterTextChanged()
		{
			m_node.After = View.AfterText;
			RefreshPreview();
		}

		private void StyleChanged()
		{
			m_node.Style = View.Style;
			RefreshPreview();
		}

		#region ListChanges
		/// <summary>
		/// Called when an item in the ListView is checked or unchecked.  Validates the new set of checked items (preventing if invalid),
		/// serializes, and refreshes the preview
		/// </summary>
		/// <param name="listOptionsView"></param>
		/// <param name="wsOptions">Null if the list doesn't represent writing systems</param>
		/// <param name="e"></param>
		private void ListItemCheckedChanged(IDictionaryListOptionsView listOptionsView, DictionaryNodeWritingSystemOptions wsOptions, ItemCheckedEventArgs e)
		{
			var items = e.Item.ListView.Items;

			// Validate: Default and Specific WS's cannot be concurrently checked; at least one is always checked
			if (!e.Item.Checked)
			{
				if (!items.Cast<ListViewItem>().Any(item => item.Checked))
				{
					// At least one WS must be checked
					e.Item.Checked = true;
					return;
				}
			}
			else if (e.Item.Tag is int) // int represents a Default WS; all others must be deselected
			{
				foreach (var item in items.Cast<ListViewItem>().Where(item => item != e.Item))
					item.Checked = false;
			}
			else // A specific WS was selected; deselect defaults
			{
				foreach (var item in items.Cast<ListViewItem>().Where(item => item.Tag is int))
					item.Checked = false;
			}

			if (wsOptions != null)
			{
				// Displaying WS Abbreviations is available only when multiple WS's are selected.
				listOptionsView.DisplayOptionCheckBoxEnabled = (items.Cast<ListViewItem>().Count(item => item.Checked) >= 2);
				// Don't clear the checkbox while users are working, but don't persist an invalid value.
				wsOptions.DisplayWritingSystemAbbreviations = listOptionsView.DisplayOptionCheckBoxEnabled &&
															  listOptionsView.DisplayOptionCheckBoxChecked;
			}

			SerializeListOptionsAndRefreshPreview(items);
		}

		private void SerializeListOptionsAndRefreshPreview(ListView.ListViewItemCollection items)
		{
			// build a collection of DictionaryNodeOptions out of the collection from the ListView
			var options = items.Cast<ListViewItem>().Select(item => new DictionaryNodeListOptions.DictionaryNodeOption
			{
				Id = item.Tag is int ? WritingSystemServices.GetMagicWsNameFromId((int)item.Tag) : (string)item.Tag,
				IsEnabled = item.Checked
			}).ToList();

			if (Options is DictionaryNodeWritingSystemOptions)
				((DictionaryNodeWritingSystemOptions) Options).Options = options;
			else if (Options is DictionaryNodeListOptions)
				((DictionaryNodeListOptions) Options).Options = options;
			else
				throw new InvalidCastException("Options could not be cast to WS- or ListOptions type.");

			RefreshPreview();
		}

		private void ListViewSelectionChanged(IDictionaryListOptionsView loView, ListViewItemSelectionChangedEventArgs e)
		{
			if (e.IsSelected)
			{
				loView.MoveUpEnabled = CanReorder(e.Item, Direction.Up);
				loView.MoveDownEnabled = CanReorder(e.Item, Direction.Down);
			}
			else
			{
				loView.MoveUpEnabled = loView.MoveDownEnabled = false;
			}
		}

		private static bool CanReorder(ListViewItem item, Direction direction)
		{
			if (direction == Direction.Up)
			{
				// Cannot move up a default WS, the first item, or the first item below a default WS
				return !((item.Tag is int) || (item.Index == 0) || (item.ListView.Items[item.Index - 1].Tag is int));
			}
			// Cannot move down a default WS or the last item
			return !((item.Tag is int) || (item.Index >= item.ListView.Items.Count - 1));
		}

		internal void Reorder(ListViewItem item, Direction direction)
		{
			if (!CanReorder(item, direction))
				throw new ArgumentOutOfRangeException();

			int newIdx;
			if (direction == Direction.Up)
				newIdx = item.Index - 1;
			else
				newIdx = item.Index + 1;

			var items = item.ListView.Items;
			items.RemoveAt(item.Index);
			items.Insert(newIdx, item);

			SerializeListOptionsAndRefreshPreview(items);
		}
		#endregion ListChanges

		#region SenseChanges
		private void SenseNumbingStyleChanged(DictionaryNodeSenseOptions senseOptions, IDictionarySenseOptionsView senseOptionsView, bool isSubsense, bool isSubSubsense)
		{
			var hc = m_cache.ServiceLocator.GetInstance<HomographConfiguration>();
			if (isSubSubsense)
				hc.ksSubSubSenseNumberStyle = senseOptionsView.NumberingStyle;
			else if (isSubsense)
				hc.ksSubSenseNumberStyle = senseOptionsView.NumberingStyle;
			else
				hc.ksSenseNumberStyle = senseOptionsView.NumberingStyle;
			senseOptions.NumberingStyle = senseOptionsView.NumberingStyle;
			senseOptionsView.NumberMetaConfigEnabled = !string.IsNullOrEmpty(senseOptions.NumberingStyle);
			RefreshPreview();
		}

		private void ParentSenseNumbingStyleChanged(DictionaryNodeSenseOptions senseOptions, IDictionarySenseOptionsView senseOptionsView, bool isSubsense, bool isSubSubsense)
		{
			var hc = m_cache.ServiceLocator.GetInstance<HomographConfiguration>();
			if (isSubSubsense)
				hc.ksParentSubSenseNumberStyle = senseOptionsView.ParentSenseNumberingStyle;
			else if (isSubsense)
				hc.ksParentSenseNumberStyle = senseOptionsView.ParentSenseNumberingStyle;
			senseOptions.ParentSenseNumberingStyle = senseOptionsView.ParentSenseNumberingStyle;
			RefreshPreview();
		}

		private void SenseInParaChanged(DictionaryNodeSenseOptions senseOptions, IDictionarySenseOptionsView senseOptionsView)
		{
			senseOptions.DisplayEachSenseInAParagraph = senseOptionsView.SenseInPara;
			// If we are not showing each sense in a paragraph then the paragraph style should no longer be in the configuration.
			// The default style "Dictionary-Sense" will be used if the user turns this option on.
			m_node.Style = senseOptions.DisplayEachSenseInAParagraph ? "Dictionary-Sense" : null;
			ToggleViewForShowInPara(senseOptions.DisplayEachSenseInAParagraph);
			senseOptionsView.FirstSenseInlineVisible = senseOptions.DisplayEachSenseInAParagraph;
			RefreshPreview();
		}
		#endregion SenseChanges

		private void ToggleViewForShowInPara(bool showInPara)
		{
			View.StylesVisible = true;
			View.SurroundingCharsVisible = !showInPara;
			if (showInPara)
			{
				View.SetStyles(m_paraStyles, m_node.Style, true);
				m_node.StyleType = StyleTypes.Paragraph;
			}
			else
			{
				View.SetStyles(m_charStyles, m_node.Style, false);
				m_node.StyleType = StyleTypes.Character;
			}
		}
		#endregion HandleChanges
	}
}
