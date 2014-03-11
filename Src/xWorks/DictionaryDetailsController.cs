// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
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
			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			SetStylesLists();

			// load node
			LoadNode(node);
		}

		/// <summary>
		/// (Re)initializes the controller and view to configure the given node
		/// </summary>
		/// <param name="node"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "View is disposed by its parent")]
		public void LoadNode(ConfigurableDictionaryNode node)
		{
			m_node = node;

			View = new DetailsView()
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
					InitWsOptions(Options as DictionaryNodeWritingSystemOptions);
				}
				else if (Options is DictionaryNodeSenseOptions)
				{
					InitSenseOptions(Options as DictionaryNodeSenseOptions);
				}
				else if (Options is DictionaryNodeListOptions)
				{
					InitListOptions(Options as DictionaryNodeListOptions);
				}
				else
				{
					throw new ArgumentException("Unrecognised type of DictionaryNodeOptions");
				}
			}
			else if ("Main Entry".Equals(m_node.Label)) // TODO pH 2014.02: more-reliable test
			{
				// There is nothing to configure on the Main Entry itself
				View.Visible = false;
			}
			else if ("Subsubentries".Equals(m_node.Label)) // TODO pH 2014.02: more-reliable test
			{
				// REVIEW (Hasso) 2014.02: Styles were hidden for subsubentries in the old dialog.  Is this still what we want?
				View.StylesVisible = false;
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
		private void InitWsOptions(DictionaryNodeWritingSystemOptions wsOptions)
		{
			// TODO: initialize WS view
		}

		/// <summary>Initialize options for DictionaryNodeSenseOptions</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "senseOptionsView is disposed by its parent")]
		private void InitSenseOptions(DictionaryNodeSenseOptions senseOptions)
		{
			var senseOptionsView = new SenseOptionsView()
			{
				BeforeText = senseOptions.BeforeNumber,
				FormatMark = senseOptions.NumberMark,
				AfterText = senseOptions.AfterNumber,
				// TODO pH 2014.02: bold, italic, font
				NumberSingleSense = senseOptions.NumberEvenASingleSense,
				ShowGrammarFirst = senseOptions.ShowSharedGrammarInfoFirst,
				SenseInPara = senseOptions.DisplayEachSenseInAParagraph
			};

			// load paragraph Style
			View.SetStyles(m_paraStyles, m_node.Style, true);

			// TODO: action or event to dis- and enable Style and Context

			View.OptionsView = senseOptionsView;
		}

		/// <summary>Initialize options for DictionaryNodeListOptions other than WritingSystem options</summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "listOptionsView is disposed by its parent")]
		private void InitListOptions(DictionaryNodeListOptions listOptions)
		{
			var listOptionsView = new ListOptionsView();

			if (listOptions is DictionaryNodeComplexFormOptions)
			{
				InitComplexFormOptions(listOptions as DictionaryNodeComplexFormOptions, listOptionsView);
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

		private void InitComplexFormOptions(DictionaryNodeComplexFormOptions complexFormOptions, ListOptionsView listOptionsView)
		{
			listOptionsView.CheckBoxLabel = "Display each complex form in a paragraph"; // TODO: localize
			listOptionsView.CheckBoxChanged += (sender, e) =>
			{
				complexFormOptions.DisplayEachComplexFormInAParagraph = listOptionsView.CheckBoxChecked;
			};
			View.SetStyles(m_paraStyles, m_node.Style, true);
		}

		private void SetStylesLists()
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

		private void HandleStylesBtn(ComboBox combo, string defaultStyle)
		{
			FwStylesDlg.RunStylesDialogForCombo(combo, SetStylesLists, defaultStyle, m_styleSheet, 0, 0,
				(FdoCache)m_mediator.PropertyTable.GetValue("cache"), View.TopLevelControl,
				((IApp)m_mediator.PropertyTable.GetValue("App")), m_mediator.HelpTopicProvider);
		}

		private void RefreshPreview()
		{
			if (DetailsModelChanged != null)
				DetailsModelChanged(m_node, new EventArgs());
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
	}
}
