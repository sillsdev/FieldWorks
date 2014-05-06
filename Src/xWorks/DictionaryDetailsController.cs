// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Controls the portion of the dialog where an element in a dictionary entry is configured in detail, including Writing Systems,
	/// Complex Form types, Lexical Relation types, Sense numbers, etc.
	/// This class does not control whether this particular element of the entry is displayed, nor any preview of the entry.
	/// </summary>
	public class DictionaryDetailsController
	{
		private readonly Mediator m_mediator;
		private readonly FdoCache m_cache;
		private readonly FwStyleSheet m_styleSheet;

		private List<StyleComboItem> m_charStyles;
		private List<StyleComboItem> m_paraStyles;

		/// <summary>Model for the dictionary element being configured</summary>
		private ConfigurableDictionaryNode m_node;
		/// <summary>Model for options specific to the element type, such as writing systems or relation types</summary>
		private DictionaryNodeOptions Options { get { return m_node.DictionaryNodeOptions; } }

		/// <summary>The DetailsView controlled by this controller</summary>
		public DetailsView View { get; private set; }

		/// <summary>Fired whenever the model is changed so that the dictionary preview can be refreshed</summary>
		public event EventHandler DetailsModelChanged;

		public DictionaryDetailsController(ConfigurableDictionaryNode node, Mediator mediator)
		{
			// one-time setup
			m_mediator = mediator;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			LoadStylesLists();

			// load node
			LoadNode(node);
		}

		#region LoadModel
		/// <summary>
		/// (Re)initializes the controller and view to configure the given node
		/// </summary>
		/// <param name="node"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "View is disposed by its parent")]
		public void LoadNode(ConfigurableDictionaryNode node)
		{
			m_node = node;

			View = new DetailsView
			{
				BeforeText = m_node.Before,
				BetweenText = m_node.Between,
				AfterText = m_node.After,
				Enabled = m_node.IsEnabled
			};

			View.SuspendLayout();

			// Populate Styles dropdown
			View.SetStyles(m_charStyles, m_node.Style);

			// Test for Options type
			if (Options != null)
			{
				if (Options is DictionaryNodeWritingSystemOptions)
				{
					LoadWsOptions(Options as DictionaryNodeWritingSystemOptions);
				}
				else if (Options is DictionaryNodeSenseOptions)
				{
					LoadSenseOptions(Options as DictionaryNodeSenseOptions);
				}
				else if (Options is DictionaryNodeListOptions)
				{
					LoadListOptions(Options as DictionaryNodeListOptions);
				}
				else
				{
					throw new ArgumentException("Unrecognised type of DictionaryNodeOptions");
				}
			}
			else if ("LexEntry".Equals(m_node.FieldDescription))
			{
				// Main Entry and Minor Entry are the only two where field=LexEntry; of these, only Main Entry has Options=null
				// There is nothing to configure on the Main Entry itself
				View.Visible = false;
			}
			// else, show only the default details (style, before, between, after)

			// Register eventhandlers
			View.StyleSelectionChanged += (sender, e) => StyleChanged();
			View.StyleButtonClick += (sender, e) => HandleStylesBtn((ComboBox)sender, View.Style);
			View.BeforeTextChanged += (sender, e) => BeforeTextChanged();
			View.BetweenTextChanged += (sender, e) => BetweenTextChanged();
			View.AfterTextChanged += (sender, e) => AfterTextChanged();

			View.ResumeLayout();
		}

		/// <summary>Initialize options for DictionaryNodeWritingSystemOptions</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "wsOptionsView is disposed by its parent")]
		private void LoadWsOptions(DictionaryNodeWritingSystemOptions wsOptions)
		{
			var wsOptionsView = new ListOptionsView
			{
				DisplayOptionCheckBoxChecked = wsOptions.DisplayWritingSystemAbbreviations
			};

			// Find and add available and selected Writing Systems
			var selectedWSs = wsOptions.Options.Where(ws => ws.IsEnabled).ToList();
			var availableWSs = GetCurrentWritingSystems(wsOptions.WsType);

			bool atLeastOneWsChecked = false;
			// Check if the default WS is selected (it will be the one and only)
			if (selectedWSs.Count() == 1)
			{
				var selectedWsDefaultId = WritingSystemServices.GetMagicWsIdFromName(selectedWSs[0].Id);
				if (selectedWsDefaultId < 0)
				{
					var defaultWsItem = availableWSs.FirstOrDefault(item => item.Tag.Equals(selectedWsDefaultId));
					if (defaultWsItem != null)
					{
						defaultWsItem.Checked = true;
						atLeastOneWsChecked = true;
					}
				}
			}

			if (!atLeastOneWsChecked)
			{
				// Insert checked named WS's in their saved order, after the Default WS (2 Default WS's if Type is Both)
				int insertionIdx = wsOptions.WsType == DictionaryNodeWritingSystemOptions.WritingSystemType.Both ? 2 : 1;
				foreach (var ws in selectedWSs)
				{
					var selectedItem = availableWSs.FirstOrDefault(item => ws.Id.Equals(item.Tag));
					if (selectedItem != null && availableWSs.Remove(selectedItem))
					{
						selectedItem.Checked = true;
						availableWSs.Insert(insertionIdx++, selectedItem);
						atLeastOneWsChecked = true;
					}
				}
			}

			// If we still haven't checked one, check the first default (the previously-checked WS was removed)
			if (!atLeastOneWsChecked)
				availableWSs[0].Checked = true;

			wsOptionsView.AvailableItems = availableWSs;

			// Displaying WS Abbreviations is available only when multiple WS's are selected.
			wsOptionsView.DisplayOptionCheckBoxEnabled = (availableWSs.Count(item => item.Checked) >= 2);

			// Prevent events from firing while the view is being initialized
			wsOptionsView.Load += WritingSystemEventHandlerAdder(wsOptionsView, wsOptions);

			View.OptionsView = wsOptionsView;
		}

		private EventHandler WritingSystemEventHandlerAdder(ListOptionsView wsOptionsView, DictionaryNodeWritingSystemOptions wsOptions)
		{
			return (o, args) =>
			{
				wsOptionsView.UpClicked += (sender, e) =>
					Reorder(wsOptionsView.AvailableItems.First(item => item.Selected), DictionaryConfigurationController.Direction.Up);
				wsOptionsView.DownClicked += (sender, e) =>
					Reorder(wsOptionsView.AvailableItems.First(item => item.Selected), DictionaryConfigurationController.Direction.Down);
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

		/// <summary>Initialize options for DictionaryNodeSenseOptions</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "senseOptionsView is disposed by its parent")]
		private void LoadSenseOptions(DictionaryNodeSenseOptions senseOptions)
		{
			// parse style string
			var bold = CheckState.Indeterminate;
			var italic = CheckState.Indeterminate;
			var style = senseOptions.NumberStyle;
			if (!String.IsNullOrEmpty(style))
			{
				style = style.ToLowerInvariant();
				if (style.IndexOf("-bold", StringComparison.Ordinal) >= 0)
					bold = CheckState.Unchecked;
				else if (style.IndexOf("bold", StringComparison.Ordinal) >= 0)
					bold = CheckState.Checked;
				if (style.IndexOf("-italic", StringComparison.Ordinal) >= 0)
					italic = CheckState.Unchecked;
				else if (style.IndexOf("italic", StringComparison.Ordinal) >= 0)
					italic = CheckState.Checked;
			}

			// initialize SenseOptionsView
			var senseOptionsView = new SenseOptionsView
			{
				BeforeText = senseOptions.BeforeNumber,
				NumberingStyles = XmlVcDisplayVec.SupportedNumberingStyles, // load available list before setting value
				NumberingStyle = senseOptions.NumberingStyle,
				AfterText = senseOptions.AfterNumber,
				Bold = bold,
				Italic = italic,
				NumberFonts = AvailableFonts, // load list of available fonts before setting NumberFont
				NumberFont = senseOptions.NumberFont,
				NumberSingleSense = senseOptions.NumberEvenASingleSense,
				ShowGrammarFirst = senseOptions.ShowSharedGrammarInfoFirst,
				SenseInPara = senseOptions.DisplayEachSenseInAParagraph,
			};

			// load paragraph Style
			View.SetStyles(m_paraStyles, m_node.Style, true);

			// (dis)actviate appropriate parts of the view
			senseOptionsView.NumberMetaConfigEnabled = !string.IsNullOrEmpty(senseOptions.NumberingStyle);
			ToggleViewForShowInPara(senseOptions.DisplayEachSenseInAParagraph);

			// Register eventhandlers
			senseOptionsView.BeforeTextChanged += (sender, e) => { senseOptions.BeforeNumber = senseOptionsView.BeforeText; RefreshPreview(); };
			senseOptionsView.NumberingStyleChanged += (sender, e) => SenseNumbingStyleChanged(senseOptions, senseOptionsView);
			senseOptionsView.AfterTextChanged += (sender, e) => { senseOptions.AfterNumber = senseOptionsView.AfterText; RefreshPreview(); };
			senseOptionsView.NumberFontChanged += (sender, e) => SenseNumFontChanged(senseOptions, senseOptionsView.NumberFont);
// ReSharper disable ImplicitlyCapturedClosure
// Justification: senseOptions, senseOptionsView, and all of these lambda functions will all disappear at the same time.
			senseOptionsView.BoldChanged += (sender, e) => SenseNumStyleChanged(senseOptionsView.Bold, senseOptionsView.Italic);
			senseOptionsView.ItalicChanged += (sender, e) => SenseNumStyleChanged(senseOptionsView.Bold, senseOptionsView.Italic);
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

			// add senseOptionsView to the DetailsView
			View.OptionsView = senseOptionsView;
		}

		/// <summary>Initialize options for DictionaryNodeListOptions other than WritingSystem options</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "listOptionsView is disposed by its parent")]
		private void LoadListOptions(DictionaryNodeListOptions listOptions)
		{
			var listOptionsView = new ListOptionsView();

			if (listOptions is DictionaryNodeComplexFormOptions)
			{
				LoadComplexFormOptions(listOptions as DictionaryNodeComplexFormOptions, listOptionsView);
			}
			else
			{
				// Complex Forms are the only List type that make use of the Display Option CheckBox below the list
				listOptionsView.DisplayOptionCheckBoxVisible = false;
			}

			if ("Subentries".Equals(m_node.FieldDescription) && "Subentries".Equals(m_node.Parent.FieldDescription))
			{
				// Subsubentries inherit everything except context from Subentries.  We doubt users will even have Subsubentries.
				View.StylesVisible = false;
			}

			if (listOptions.ListId == DictionaryNodeListOptions.ListIds.Complex ||
				listOptions.ListId == DictionaryNodeListOptions.ListIds.Minor)
			{
				View.SetStyles(m_paraStyles, m_node.Style, true);
			}

			if (listOptions.ListId == DictionaryNodeListOptions.ListIds.None)
			{
				listOptionsView.ListViewVisible = false;
			}
			else
			{
				// TODO pH 2014.02: find list label

				var savedOptions = listOptions.Options = listOptions.Options ?? new List<DictionaryNodeListOptions.DictionaryNodeOption>();
				var availableOptions = GetListItems(listOptions.ListId);

				// Insert saved items in their saved order, with their saved check-state
				int insertionIdx = 0;
				foreach (var optn in savedOptions)
				{
					var savedItem = availableOptions.FirstOrDefault(item => optn.Id.Equals((item.Tag)));
					if (savedItem != null && availableOptions.Remove(savedItem))
					{
						savedItem.Checked = optn.IsEnabled;
						availableOptions.Insert(insertionIdx++, savedItem);
					}
				}

				listOptionsView.AvailableItems = availableOptions;

				// Prevent events from firing while the view is being initialized
				listOptionsView.Load += ListEventHandlerAdder(listOptionsView, listOptions);
			}

			View.OptionsView = listOptionsView;
		}

		private void LoadComplexFormOptions(DictionaryNodeComplexFormOptions complexFormOptions, ListOptionsView listOptionsView)
		{
			listOptionsView.DisplayOptionCheckBoxLabel = xWorksStrings.ksDisplayComplexFormsInParagraphs;
			listOptionsView.DisplayOptionCheckBoxChecked = complexFormOptions.DisplayEachComplexFormInAParagraph;
			ToggleViewForShowInPara(complexFormOptions.DisplayEachComplexFormInAParagraph);
		}

		private EventHandler ListEventHandlerAdder(ListOptionsView listOptionsView, DictionaryNodeListOptions listOptions)
		{
			return (o, args) =>
			{
				listOptionsView.UpClicked += (sender, e) =>
					Reorder(listOptionsView.AvailableItems.First(item => item.Selected), DictionaryConfigurationController.Direction.Up);
				listOptionsView.DownClicked += (sender, e) =>
					Reorder(listOptionsView.AvailableItems.First(item => item.Selected), DictionaryConfigurationController.Direction.Down);
				listOptionsView.ListItemSelectionChanged += (sender, e) => ListViewSelectionChanged(listOptionsView, e);
				listOptionsView.ListItemCheckBoxChanged += (sender, e) => ListItemCheckedChanged(listOptionsView, null, e);

				var complexFormOptions = listOptions as DictionaryNodeComplexFormOptions;
				if (complexFormOptions != null)
				{
					listOptionsView.DisplayOptionCheckBoxChanged += (sender, e) =>
					{
						complexFormOptions.DisplayEachComplexFormInAParagraph = listOptionsView.DisplayOptionCheckBoxChecked;
						ToggleViewForShowInPara(complexFormOptions.DisplayEachComplexFormInAParagraph);
						RefreshPreview();
					};
				}

				listOptionsView.Load -= ListEventHandlerAdder(listOptionsView, listOptions);
			};
		}

		#region Load more-static parts
		/// <param name="wsType"></param>
		/// <returns>
		/// A list of ListViewItem's representing this project's WritingSystems, with "magic" default WS's at the beginning of the list.
		/// Each LVI's Tag is the WS Id: negative int for "magic" default WS's, and a string like "en" or "fr" for normal WS's.
		/// LVI's are unchecked by default
		/// </returns>
		private List<ListViewItem> GetCurrentWritingSystems(DictionaryNodeWritingSystemOptions.WritingSystemType wsType)
		{
			var wsList = new List<ListViewItem>();
			switch (wsType)
			{
				case DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular:
					wsList.Add(new ListViewItem(xWorksStrings.ksDefaultVernacular) { Tag = WritingSystemServices.kwsVern });
					wsList.AddRange(m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(
							ws => new ListViewItem(ws.DisplayLabel) { Tag = ws.Id }));
					break;
				case DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis:
					wsList.Add(new ListViewItem(xWorksStrings.ksDefaultAnalysis) { Tag = WritingSystemServices.kwsAnal });
					wsList.AddRange(m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(
							ws => new ListViewItem(ws.DisplayLabel) { Tag = ws.Id }));
					break;
				case DictionaryNodeWritingSystemOptions.WritingSystemType.Both:
					wsList.Add(new ListViewItem(xWorksStrings.ksDefaultVernacular) { Tag = WritingSystemServices.kwsVern });
					wsList.Add(new ListViewItem(xWorksStrings.ksDefaultAnalysis) { Tag = WritingSystemServices.kwsAnal });
					wsList.AddRange(m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(
							ws => new ListViewItem(ws.DisplayLabel) { Tag = ws.Id }));
					wsList.AddRange(m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(
							ws => new ListViewItem(ws.DisplayLabel) { Tag = ws.Id }));
					break;
				case DictionaryNodeWritingSystemOptions.WritingSystemType.Pronunciation:
					wsList.Add(new ListViewItem(xWorksStrings.ksDefaultPronunciation) { Tag = WritingSystemServices.kwsPronunciation });
					wsList.AddRange(m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Select(
							ws => new ListViewItem(ws.DisplayLabel) { Tag = ws.Id }));
					break;
			}
			return wsList;
		}

		private static List<string> AvailableFonts
		{
			get
			{
				var fonts = new List<string> { xWorksStrings.ksUnspecified };
				using (var installedFontCollection = new InstalledFontCollection())
				{
					// The .NET framework is unforgiving of fonts that don't support the "regular" style, so we hide them.
					fonts.AddRange(installedFontCollection.Families.Where(family => family.IsStyleAvailable(FontStyle.Regular))
						.Select(family => family.Name));
				}
				return fonts;
			}
		}

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
		/// Get ListViewItems for the given List ID. Tag is a String representation of the item's GUID.  Items are checked by default.
		/// </summary>
		internal List<ListViewItem> GetListItems(DictionaryNodeListOptions.ListIds listId)
		{
			switch (listId)
			{
				case DictionaryNodeListOptions.ListIds.Minor:
					return GetMinorEntryTypes();
				case DictionaryNodeListOptions.ListIds.Complex:
					return GetComplexFormTypes();
				case DictionaryNodeListOptions.ListIds.Variant:
					return GetVariantTypes();
				case DictionaryNodeListOptions.ListIds.Sense:
				case DictionaryNodeListOptions.ListIds.Entry:
					return GetLexicalRelationTypes(listId);
				default:
					throw new ArgumentException("Unrecognised List ID: " + listId);
			}
		}

		private List<ListViewItem> GetMinorEntryTypes()
		{
			var result = GetVariantTypes();
			// TODO pH 2014.05: AddRange iff this is Root-Based (not Stem-Based)
			result.AddRange(GetComplexFormTypes());
			return result;
		}

		private List<ListViewItem> GetComplexFormTypes()
		{
			var result = FlattenSortAndConvertList(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS);
			result.Insert(0, new ListViewItem("<" + xWorksStrings.ksNoComplexFormType + ">")
			{
				Checked = true, Tag = XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString()
			});
			return result;
		}

		private List<ListViewItem> GetVariantTypes()
		{
			var result = FlattenSortAndConvertList(m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS);
			result.Insert(0, new ListViewItem("<" + xWorksStrings.ksNoVariantType + ">")
			{
				Checked = true, Tag = XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString()
			});
			return result;
		}

		/// <summary>Flattens hierarchy, sorts by name, and converts to ListViewItems</summary>
		private static List<ListViewItem> FlattenSortAndConvertList(IFdoOwningSequence<ICmPossibility> sequence)
		{
			var result = FlattenPossibilityList(sequence);
			result.Sort(ComparePossibilitiesByName);
			return result.Select(item => new ListViewItem(item.Name.BestAnalysisVernacularAlternative.Text)
			{
				Checked = true, Tag = item.Guid.ToString()
			}).ToList();
		}

		internal static List<ICmPossibility> FlattenPossibilityList(IFdoOwningSequence<ICmPossibility> sequence)
		{
			var list = sequence.ToList();
			foreach (var poss in sequence)
			{
				// Recurse to get all nested items
				list.AddRange(FlattenPossibilityList(poss.SubPossibilitiesOS));
			}
			return list;
		}

		// REVIEW (Hasso) 2014.05: This method is currently optimised for loading and caching both Sense and Entry lists at once. It
		// could be optimised for loading each as needed without caching: by checking first for whether each relType is applicable.
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
				if (mappingType == LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
					mappingType == LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
					mappingType == LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
					mappingType == LexRefTypeTags.MappingTypes.kmtEntryTree ||
					mappingType == LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree ||
					mappingType == LexRefTypeTags.MappingTypes.kmtSenseTree)
				{
					listViewItemS.Add(new ListViewItem(lexRelType.Name.BestAnalysisVernacularAlternative.Text)
					{
						Checked = true,
						// TODO pH 2014.05: update default configuration to use StorageString
						Tag = new LexReferenceInfo(true, relType.Guid)
						{
							SubClass = LexReferenceInfo.TypeSubClass.Forward
						}.StorageString.Substring(1) // substring removes the leading "+"; REVIEW (Hasso) 2014.05: do we want to?
					});
					listViewItemS.Add(new ListViewItem(lexRelType.ReverseName.BestAnalysisVernacularAlternative.Text)
					{
						Checked = true,
						Tag = new LexReferenceInfo(true, relType.Guid)
						{
							SubClass = LexReferenceInfo.TypeSubClass.Reverse
						}.StorageString.Substring(1)
					});
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
						if (listId == DictionaryNodeListOptions.ListIds.Entry)
							lexRelTypesSubset.AddRange(listViewItemS);
						break;
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
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
			return String.Compare(xName, yName);
		}
		#endregion Load more-static parts
		#endregion LoadModel

		#region HandleChanges
		private void RefreshPreview()
		{
			if (DetailsModelChanged != null)
				DetailsModelChanged(m_node, new EventArgs());
		}

		private void HandleStylesBtn(ComboBox combo, string defaultStyle)
		{
			FwStylesDlg.RunStylesDialogForCombo(combo, LoadStylesLists, defaultStyle, m_styleSheet, 0, 0, m_cache, View.TopLevelControl,
				((IApp)m_mediator.PropertyTable.GetValue("App")), m_mediator.HelpTopicProvider);
			RefreshPreview();
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
		private void ListItemCheckedChanged(ListOptionsView listOptionsView, DictionaryNodeWritingSystemOptions wsOptions, ItemCheckedEventArgs e)
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
			List<DictionaryNodeListOptions.DictionaryNodeOption> options;
			if (Options is DictionaryNodeWritingSystemOptions)
				options = (Options as DictionaryNodeWritingSystemOptions).Options;
			else if (Options is DictionaryNodeListOptions)
				options = (Options as DictionaryNodeListOptions).Options;
			else
				throw new InvalidCastException("Options could not be cast to WS- or ListOptions type.");

			options.Clear();
			options.AddRange(items.Cast<ListViewItem>().Select(item => new DictionaryNodeListOptions.DictionaryNodeOption
			{
				Id = item.Tag is int ? WritingSystemServices.GetMagicWsNameFromId((int)item.Tag) : (string)item.Tag,
				IsEnabled = item.Checked
			}));

			RefreshPreview();
		}

		private void ListViewSelectionChanged(ListOptionsView loView, ListViewItemSelectionChangedEventArgs e)
		{
			if (e.IsSelected)
			{
				loView.MoveUpEnabled = CanReorder(e.Item, DictionaryConfigurationController.Direction.Up);
				loView.MoveDownEnabled = CanReorder(e.Item, DictionaryConfigurationController.Direction.Down);
			}
			else
			{
				loView.MoveUpEnabled = loView.MoveDownEnabled = false;
			}
		}

		private static bool CanReorder(ListViewItem item, DictionaryConfigurationController.Direction direction)
		{
			if (direction == DictionaryConfigurationController.Direction.Up)
			{
				// Cannot move up a default WS, the first item, or the first item below a default WS
				return !((item.Tag is int) || (item.Index == 0) || (item.ListView.Items[item.Index - 1].Tag is int));
			}
			// Cannot move down a default WS or the last item
			return !((item.Tag is int) || (item.Index >= item.ListView.Items.Count - 1));
		}

		internal void Reorder(ListViewItem item, DictionaryConfigurationController.Direction direction)
		{
			if(!CanReorder(item, direction))
				throw new ArgumentOutOfRangeException();

			int newIdx;
			if (direction == DictionaryConfigurationController.Direction.Up)
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
		private void SenseNumbingStyleChanged(DictionaryNodeSenseOptions senseOptions, SenseOptionsView senseOptionsView)
		{
			senseOptions.NumberingStyle = senseOptionsView.NumberingStyle;
			senseOptionsView.NumberMetaConfigEnabled = !string.IsNullOrEmpty(senseOptions.NumberingStyle);
			RefreshPreview();
		}

		private void SenseNumStyleChanged(CheckState bold, CheckState italic)
		{
			var sbNumStyle = new StringBuilder();
			if (bold == CheckState.Checked)
				sbNumStyle.Append("bold");
			else if (bold == CheckState.Unchecked)
				sbNumStyle.Append("-bold");
			if (bold != CheckState.Indeterminate && italic != CheckState.Indeterminate)
				sbNumStyle.Append(" ");
			if (italic == CheckState.Checked)
				sbNumStyle.Append("italic");
			else if (italic == CheckState.Unchecked)
				sbNumStyle.Append("-italic");
			((DictionaryNodeSenseOptions)Options).NumberStyle = sbNumStyle.ToString();
			RefreshPreview();
		}

		private void SenseNumFontChanged(DictionaryNodeSenseOptions senseOptions, string font)
		{
			senseOptions.NumberFont = xWorksStrings.ksUnspecified.Equals(font) ? "" : font;
			RefreshPreview();
		}

		private void SenseInParaChanged(DictionaryNodeSenseOptions senseOptions, SenseOptionsView senseOptionsView)
		{
			senseOptions.DisplayEachSenseInAParagraph = senseOptionsView.SenseInPara;
			ToggleViewForShowInPara(senseOptions.DisplayEachSenseInAParagraph);
			RefreshPreview();
		}
		#endregion SenseChanges

		private void ToggleViewForShowInPara(bool showInPara)
		{
			View.StylesVisible = showInPara;
			View.SurroundingCharsVisible = !showInPara;
		}
		#endregion HandleChanges
	}
}
