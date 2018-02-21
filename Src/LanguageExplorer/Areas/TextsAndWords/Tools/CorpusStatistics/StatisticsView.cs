// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics
{
	/// <summary>
	/// The main view for the "corpusStatistics" tool in the "textsWords" area.
	/// </summary>
	internal sealed partial class StatisticsView : UserControl, IMainContentControl
	{
		private IRecordList _recordList;
		private ToolStrip _toolStripView;
		private ToolStripButton _chooseTextsToolStripButton;
		private ToolStripMenuItem _viewToolStripMenuItem;
		private ToolStripMenuItem _chooseTextsToolStripMenuItem;

		/// <summary />
		public StatisticsView()
		{
			InitializeComponent();
		}

		/// <summary />
		public StatisticsView(MajorFlexComponentParameters majorFlexComponentParameters) : this()
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			var cm = new ContextMenu();
			var mi = new MenuItem("Copy");
			mi.Click += Copy_Menu_Item_Click;
			cm.MenuItems.Add(mi);
			statisticsBox.ContextMenu = cm;

			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = this;
			InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(TextAndWordsArea.InterlinearTexts, majorFlexComponentParameters.Statusbar, TextAndWordsArea.InterlinearTextsFactoryMethod);

			// Add toolbar button.
			_toolStripView = ToolbarServices.GetViewToolStrip(majorFlexComponentParameters.ToolStripContainer);
			_chooseTextsToolStripButton = new ToolStripButton(LanguageExplorerResources.AddScripture.ToBitmap())
			{
				DisplayStyle = ToolStripItemDisplayStyle.Image,
				ToolTipText = LanguageExplorerResources.chooseTextsToDisplayAndUse,
				ImageTransparentColor = Color.Magenta,
				Size = new Size(24, 24),
				Text = LanguageExplorerResources.chooseTexts
			};
			_toolStripView.Items.Add(_chooseTextsToolStripButton);

			// Add menu item to View menu.
			_chooseTextsToolStripMenuItem = new ToolStripMenuItem(LanguageExplorerResources.chooseTexts, LanguageExplorerResources.AddScripture.ToBitmap())
			{
				ImageTransparentColor = Color.Magenta,
				ToolTipText = LanguageExplorerResources.chooseTexts
			};
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			_viewToolStripMenuItem = (ToolStripMenuItem)majorFlexComponentParameters.MenuStrip.Items.Find("_viewToolStripMenuItem", true)[0];
			_viewToolStripMenuItem.DropDownItems.Add(_chooseTextsToolStripMenuItem);

			_chooseTextsToolStripButton.Click += AddTexts_Clicked;
			_chooseTextsToolStripMenuItem.Click += AddTexts_Clicked;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			RebuildStatisticsTable();
			//add our current state to the history system
			PropertyTable.GetValue<LinkHandler>("LinkHandler").AddLinkToHistory(new FwLinkArgs(PropertyTable.GetValue<string>(AreaServices.ToolChoice), Guid.Empty));
		}

		#endregion

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get { return LanguageExplorerResources.ksTextAreaStatisticsViewName; }
			set { /* Do nothing. */ ; }
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger { get; set; }

		#endregion

		#region Implementation of ICtrlTabProvider

		/// <summary>
		/// Gather up suitable targets to Ctrl(+Shift)+Tab into.
		/// </summary>
		/// <param name="targetCandidates">List of places to move to.</param>
		/// <returns>A suitable target for moving to in Ctrl(+Shift)+Tab.
		/// This returned value should also have been added to the main list.</returns>
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			targetCandidates.Add(this);
			return this;
		}

		#endregion

		#region Implementation of IMainContentControl

		/// <summary>
		/// The control is about to go away, so do something first.
		/// </summary>
		public bool PrepareToGoAway()
		{
			return true;
		}

		/// <summary>
		/// The Area name that uses this control.
		/// </summary>
		public string AreaName => AreaServices.TextAndWordsAreaMachineName;

		#endregion

		#region Implementation of IDisposable

		/// <summary>Disposes of the resources (other than memory) used by the <see cref="T:System.Windows.Forms.Form" />.</summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				return; // Only need to run it on once.
			}

			if (disposing)
			{
				components?.Dispose();

				_chooseTextsToolStripButton.Click -= AddTexts_Clicked;
				_chooseTextsToolStripMenuItem.Click -= AddTexts_Clicked;

				// Remove menu item.
				_viewToolStripMenuItem.DropDownItems.Remove(_chooseTextsToolStripMenuItem);
				_chooseTextsToolStripMenuItem.Dispose();
				_chooseTextsToolStripMenuItem = null;

				// Remove toolbar button.
				_toolStripView.Items.Remove(_chooseTextsToolStripButton);
				_chooseTextsToolStripButton.Dispose();
			}
			_recordList = null;
			_toolStripView = null;
			_chooseTextsToolStripButton = null;
			_viewToolStripMenuItem = null;
			_chooseTextsToolStripMenuItem = null;

			base.Dispose(disposing);
		}
		#endregion

		private void RebuildStatisticsTable()
		{
			statisticsBox.Clear();
			// TODO-Linux: SelectionTabs isn't implemented on Mono
			statisticsBox.SelectionTabs = new[] { 10, 300};
			//retrieve the default UI font.
			var cache = PropertyTable.GetValue<LcmCache>("cache");
			var font = FontHeightAdjuster.GetFontForStyle(StyleServices.NormalStyleName, FwUtils.StyleSheetFromPropertyTable(PropertyTable), cache.DefaultUserWs, cache.WritingSystemFactory);
			//increase the size of the default UI font and make it bold for the header.
			var headerFont = new Font(font.FontFamily, font.SizeInPoints + 1.0f, FontStyle.Bold, font.Unit, font.GdiCharSet);
			//refresh the statisticsDescription (in case of font changes)
			statisticsBox.Text = LanguageExplorerResources.ksStatisticsView_HeaderText;
			var textList = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, cache.ServiceLocator);
			var numberOfSegments = 0;
			var wordCount = 0;
			var uniqueWords = 0;
			var languageCount = new Dictionary<int, int>();
			var languageTypeCount = new Dictionary<int, HashSet<string>>();
			foreach (var interestingText in textList.InterestingTexts)
			{
				//if a text is deleted in Interlinear there could be a text in this list which has invalid data.
				if (interestingText.Hvo < 0)
				{
					continue;
				}
				for (var index = 0; index < interestingText.ParagraphsOS.Count; ++index)
				{
					//count the segments in this paragraph
					numberOfSegments += interestingText[index].SegmentsOS.Count;
					//count all the things analyzed as words
					var words = new List<IAnalysis>(interestingText[index].Analyses);
					foreach (var word in words)
					{
						var wordForm = word.Wordform;
						if (wordForm == null)
						{
							continue;
						}
						var valdWSs = wordForm.Form.AvailableWritingSystemIds;
						foreach (var ws in valdWSs)
						{
							// increase the count of words(tokens) for this language
							int count;
							if (languageCount.TryGetValue(ws, out count))
							{
								languageCount[ws] = count + 1;
							}
							else
							{
								languageCount.Add(ws, 1);
							}
							//increase the count of unique words(types) for this language
							HashSet<string> pair;
							if (languageTypeCount.TryGetValue(ws, out pair))
							{
								//add the string for this writing system in all lower case to the set, unique count is case insensitive
								pair.Add(word.Wordform.Form.get_String(ws).Text.ToLower());
							}
							else
							{
								//add the string for this writing system in all lower case to the set, unique count is case insensitive
								languageTypeCount.Add(ws, new HashSet<String> { word.Wordform.Form.get_String(ws).Text.ToLower() });
							}
						}
					}
					words.RemoveAll(item => !item.HasWordform);
					wordCount += words.Count;
				}
			}

			const string tabCharacter = "\t";
			// insert total word type count
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + tabCharacter + LanguageExplorerResources.ksStatisticsViewTotalWordTypesText + tabCharacter;

			//add one row for the unique words in each language.
			foreach (var keyValuePair in languageTypeCount)
			{
				var ws = cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				var labText = (ws?.ToString() ?? "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + tabCharacter + labText + tabCharacter;
				statisticsBox.Text += "" + keyValuePair.Value.Count;
				uniqueWords += keyValuePair.Value.Count; //increase the total of unique words
			}

			// next insert the word count.
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + tabCharacter + LanguageExplorerResources.ksStatisticsViewTotalWordTokensText + tabCharacter;
			statisticsBox.Text += wordCount;
			//add one row for the token count for each language.
			foreach (var keyValuePair in languageCount)
			{
				var ws = cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				var labText = (ws?.ToString() ?? "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + tabCharacter + labText + tabCharacter;
				statisticsBox.Text += "" + keyValuePair.Value;
			}
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + tabCharacter + LanguageExplorerResources.ksStatisticsViewTotalSentencesText + tabCharacter;

			// next insert the sentence count.
			statisticsBox.Text += numberOfSegments;

			// insert the total word type count into the richTextBox (it wasn't available earlier)
			statisticsBox.SelectionStart = statisticsBox.Find(LanguageExplorerResources.ksStatisticsViewTotalWordTypesText) + LanguageExplorerResources.ksStatisticsViewTotalWordTypesText.Length;
			statisticsBox.SelectionLength = 1;
			statisticsBox.SelectedText = tabCharacter + uniqueWords;

			// Set the font for the header. Do this after we add the other stuff to make sure
			// it doesn't apply to extra text added adjacent to it.
			statisticsBox.Select(0, LanguageExplorerResources.ksStatisticsView_HeaderText.Length);
			statisticsBox.SelectionFont = headerFont;
			statisticsBox.Select(0, 0);
		}

		private void AddTexts_Clicked(object sender, EventArgs e)
		{
			if (((InterlinearTextsRecordList)_recordList).AddTexts())
			{
				RebuildStatisticsTable();
			}
		}

		// Code to add right click
		private void Copy_Menu_Item_Click(object sender, EventArgs e)
		{
			statisticsBox.Copy();
		}
	}
}