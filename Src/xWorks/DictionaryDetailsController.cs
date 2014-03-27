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
		private List<ListViewItem> m_vernWSs;
		private List<ListViewItem> m_analWSs;

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
				// Main Entry and Minor Entry are the only two where field=LexEntry; only Main Entry has Options=null
				// There is nothing to configure on the Main Entry itself
				View.Visible = false;
			}
			// else, show the default details (style, before, between, after)

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
				CheckBoxChecked = wsOptions.DisplayWritingSystemAbbreviations
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
			wsOptionsView.CheckBoxEnabled = (availableWSs.Count(item => item.Checked) >= 2);

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
				wsOptionsView.ListItemCheckBoxChanged += (sender, e) => WritingSystemCheckedChanged(wsOptionsView, wsOptions, e);
				wsOptionsView.CheckBoxChanged += (sender, e) =>
				{
					wsOptions.DisplayWritingSystemAbbreviations = wsOptionsView.CheckBoxChecked;
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
			if ("Subentries".Equals(m_node.FieldDescription) && "Subentries".Equals(m_node.Parent.FieldDescription))
			{
				// REVIEW (Hasso) 2014.02: Styles were hidden for subsubentries in the old dialog.  Is this still what we want?
				// Subsubentries have no unique style
				View.StylesVisible = false;
			}

			var listOptionsView = new ListOptionsView();

			if (listOptions is DictionaryNodeComplexFormOptions)
			{
				LoadComplexFormOptions(listOptions as DictionaryNodeComplexFormOptions, listOptionsView);
			}
			else
			{
				listOptionsView.CheckBoxVisible = false;
			}

			if (listOptions.Options != null)
			{
				// TODO pH 2014.02: find list label
				// TODO: populate list
			}
			else
			{
				listOptionsView.ListViewVisible = false;
			}

			View.OptionsView = listOptionsView;
		}

		private void LoadComplexFormOptions(DictionaryNodeComplexFormOptions complexFormOptions, ListOptionsView listOptionsView)
		{
			listOptionsView.CheckBoxLabel = xWorksStrings.ksDisplayComplexFormsInParagraphs;
			listOptionsView.CheckBoxChanged += (sender, e) =>
			{
				complexFormOptions.DisplayEachComplexFormInAParagraph = listOptionsView.CheckBoxChecked;
			};
			View.SetStyles(m_paraStyles, m_node.Style, true);
		}

		#region Load more-static parts
		/// <param name="wsType"></param>
		/// <returns>
		/// A list of ListViewItem's representing this project's WritingSystems, with "magic" default WS's at the beginning of the list.
		/// Each LVI's Tag is the WS Id: negative int for "magic" default WS's, and a string like "en" or "fr" for normal WS's.
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
		#region WritingSystemChanges
		private void WritingSystemCheckedChanged(ListOptionsView wsOptionsView, DictionaryNodeWritingSystemOptions wsOptions, ItemCheckedEventArgs e)
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

			// Displaying WS Abbreviations is available only when multiple WS's are selected.
			wsOptionsView.CheckBoxEnabled = (items.Cast<ListViewItem>().Count(item => item.Checked) >= 2);
			// Don't clear the checkbox while users are working, but don't persist an invalid value.
			wsOptions.DisplayWritingSystemAbbreviations = wsOptionsView.CheckBoxEnabled && wsOptionsView.CheckBoxChecked;

			SerializeListOptionsAndRefreshPreview(items);
		}
		#endregion WritingSystemChanges

		private void SerializeListOptionsAndRefreshPreview(ListView.ListViewItemCollection items)
		{
			var options = ((DictionaryNodeWritingSystemOptions)Options).Options;
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
