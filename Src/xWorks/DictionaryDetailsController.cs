// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
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
		/// <summary>Model for the dictionary element being configured</summary>
		private ConfigurableDictionaryNode m_node;
		/// <summary>Model for options specific to the element type, such as writing systems or relation types</summary>
		private DictionaryNodeOptions Options { get { return m_node.DictionaryNodeOptions; } }
		/// <summary>The DetailsView controlled by this controller</summary>
		public DetailsView View { get; private set; }

		public DictionaryDetailsController(ConfigurableDictionaryNode node, Mediator mediator)
		{
			Init(node, mediator);
		}

		/// <summary>
		/// (Re)initializes the controller and view to configure the given node
		/// </summary>
		/// <param name="node"></param>
		/// <param name="mediator"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "View is disposed by its parent")]
		public void Init(ConfigurableDictionaryNode node, Mediator mediator)
		{
			m_node = node;

			View = new DetailsView()
			{
				BeforeText = m_node.Before,
				BetweenText = m_node.Between,
				AfterText = m_node.After,
				Enabled = m_node.IsEnabled
				// TODO pH 2014.02: initialize styles list
			};

			View.SuspendLayout();

			// Test for Options type
			if (Options != null)
			{
				if(Options is DictionaryNodeWritingSystemOptions)
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
			else if (m_node.Label.Equals("Main Entry")) // TODO pH 2014.02: more-reliable test
			{
				// There is nothing to configure on the Main Entry itself
				View.Visible = false;
			}
			else if (m_node.Label.Equals("Subsubentries")) // TODO pH 2014.02: more-reliable test
			{
				// REVIEW (Hasso) 2014.02: Styles were hidden for subsubentries in the old dialog.  Is this still what we want?
				View.StylesVisible = false;
			}
			// else, show the default details (style, before, between, after)

			// Register eventhandlers
			View.BeforeTextChanged += (sender, e) => BeforeTextChanged();
			View.BetweenTextChanged += (sender, e) => BetweenTextChanged();
			View.AfterTextChanged += (sender, e) => AfterTextChanged();
			View.StyleSelectionChanged += (sender, e) => StyleChanged();

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
			// TODO: swap out character Style for paragraph Style
			// TODO: action or event to dis- and enable Style and Context

			View.OptionsView = senseOptionsView;
		}

		/// <summary>Initialize options for DictionaryNodeListOptions other than WritingSystem options</summary>
		private void InitListOptions(DictionaryNodeListOptions listOptions)
		{
			var listOptionsView = new ListOptionsView();

			var complexFormOptions = listOptions as DictionaryNodeComplexFormOptions;
			if (complexFormOptions != null)
			{
				listOptionsView.CheckBoxLabel = "Display each complex form in a paragraph"; // TODO: localize
				listOptionsView.CheckBoxChanged += (sender, e) =>
				{
					complexFormOptions.DisplayEachComplexFormInAParagraph = listOptionsView.CheckBoxChecked;
				};
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

		private void RefreshPreview(){/*TODO pH 2014.02*/}

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
