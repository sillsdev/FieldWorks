// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics
{
#if RANDYTODO
	// TODO: The 'parameters' element is not yet handled, but the rest is done.
	/*
		<tool label="Statistics" value="corpusStatistics" icon="DocumentView">
			<control>
				<dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.StatisticsView"/>
				<parameters id="ITextContent" area="textsWords" clerk="interlinearTexts" treeBarAvailability="NotAllowed"/>
			</control>
		</tool>
	 */
#endif
	/// <summary>
	/// The main view for the "corpusStatistics" tool in the "textsWords" area.
	/// </summary>
	public partial class StatisticsView : UserControl, IMajorFlexComponent, IMainContentControl, IFWDisposable
	{
		private bool _shouldNotCall;
		private FdoCache _cache;
		private InterlinearTextsRecordClerk _interlinearTextsRecordClerk;

		/// <summary>
		/// Constructor
		/// </summary>
		public StatisticsView()
		{
			InitializeComponent();

			var cm = new ContextMenu();
			var mi = new MenuItem("Copy");
			mi.Click += mi_Copy;
			cm.MenuItems.Add(mi);
			statisticsBox.ContextMenu = cm;
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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			_cache = PropertyTable.GetValue<FdoCache>("cache");

			const string clerkName = "interlinearTexts";
			const string clerkPropertyTableName = "RecordClerk-" + clerkName;
			RecordClerk clerk;
			if (PropertyTable.TryGetValue(clerkPropertyTableName, out clerk))
			{
				if (clerk is TemporaryRecordClerk)
				{
					_interlinearTextsRecordClerk = new InterlinearTextsRecordClerk(_cache.LanguageProject, new InterestingTextsDecorator(_cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), _cache.ServiceLocator));
					_interlinearTextsRecordClerk.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
				}
				else
				{
					_interlinearTextsRecordClerk = (InterlinearTextsRecordClerk)clerk;
				}
			}
			else
			{
				_interlinearTextsRecordClerk = new InterlinearTextsRecordClerk(_cache.LanguageProject, new InterestingTextsDecorator(_cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), _cache.ServiceLocator));
				_interlinearTextsRecordClerk.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			}
			// There's no record bar for it to control, but it should control the status bar (e.g., it should update if we change
			// the set of selected texts).
			_interlinearTextsRecordClerk.ActivateUI(true);
			RebuildStatisticsTable();
			//add our current state to the history system
			Publisher.Publish("AddContextToHistory", new FwLinkArgs(PropertyTable.GetValue("currentContentControl", ""), Guid.Empty));
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
#if RANDYTODO
#endif
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
#if RANDYTODO
			// TODO: Add menu & toolbar item for original "CmdChooseTexts" command.
<command id="CmdChooseTexts" label="Choose Texts..." message="AddTexts" icon="addScripture" />

<menu id="View" label="_View" b4InsertMenu="true">
	<menu label="_Lexicon" list="LexicalToolsList" behavior="singlePropertyAtomicValue" property="currentContentControl" />
	<menu label="_Texts &amp;&amp; Words" list="WordToolsList" behavior="singlePropertyAtomicValue" property="currentContentControl" />
	<menu label="_Grammar" list="GrammarToolsList" behavior="singlePropertyAtomicValue" property="currentContentControl" />
	<menu label="_Notebook" list="NotebookToolsList" behavior="singlePropertyAtomicValue" property="currentContentControl" />
	<menu label="Li_sts" list="ListsToolsList" behavior="singlePropertyAtomicValue" property="currentContentControl" />
	<item label="Invisible Spaces" boolProperty="ShowInvisibleSpaces" defaultVisible="false" settingsGroup="local" />
	<item label="Show _Dictionary Preview" boolProperty="Show_DictionaryPubPreview" defaultVisible="false" />
	<item label="_Show Hidden Fields" boolProperty="ShowHiddenFields" defaultVisible="false" />
	<item label="-" translate="do not translate" />
	<menu id="FilterChoices" label="_Filters" list="FiltersList" behavior="singlePropertyAtomicValue" property="currentFilterForRecordClerk_THISCHANGES" settingsGroup="local" />
	<item command="CmdChooseTexts" defaultVisible="false" /> <-- This is it, and it is last on the View menu.
</menu>

<toolbar id="View">
	<item command="CmdChooseTexts" defaultVisible="false" /> <-- This is it, and it is first in the View toolbar, which we may also need to create, if so, it goes right before the "Standard" toolbar.
	<item command="CmdChangeFilterClearAll" />
</toolbar>
			// TODO: Add one event handler for the menu & the toolbar that does what the current "OnAddTexts" does.
#endif
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: I'm not sure if/where Font gets disposed)")]
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void RebuildStatisticsTable()
		{
			statisticsBox.Clear();
			// TODO-Linux: SelectionTabs isn't implemented on Mono
			statisticsBox.SelectionTabs = new int[] { 10, 300};
			//retrieve the default UI font.
			var font = FontHeightAdjuster.GetFontForStyle(StyleServices.NormalStyleName,
														  FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable),
														  Cache.DefaultUserWs, Cache.WritingSystemFactory);
			//increase the size of the default UI font and make it bold for the header.
			var headerFont = new Font(font.FontFamily, font.SizeInPoints + 1.0f, FontStyle.Bold, font.Unit, font.GdiCharSet);
			//refresh the statisticsDescription (in case of font changes)
			statisticsBox.Text = LanguageExplorerResources.ksStatisticsView_HeaderText;
			var textList = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, Cache.ServiceLocator);
			var numberOfSegments = 0;
			var wordCount = 0;
			var uniqueWords = 0;
			var languageCount = new Dictionary<int, int>();
			var languageTypeCount = new Dictionary<int, Set<string>>();
			//for each interesting text
			foreach (var interestingText in textList.InterestingTexts)
			{
				//if a text is deleted in Interlinear there could be a text in this list which has invalid data.
				if (interestingText.Hvo < 0)
					continue;
				//for every paragraph in the interesting text
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
							Set<string> pair;
							if (languageTypeCount.TryGetValue(ws, out pair))
							{
								//add the string for this writing system in all lower case to the set, unique count is case insensitive
								pair.Add(word.Wordform.Form.get_String(ws).Text.ToLower());
							}
							else
							{
								//add the string for this writing system in all lower case to the set, unique count is case insensitive
								languageTypeCount.Add(ws, new Set<String> { word.Wordform.Form.get_String(ws).Text.ToLower() });
							}
						}
					}
					words.RemoveAll(item => !item.HasWordform);
					wordCount += words.Count;
				}
			}
			// insert total word type count
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + LanguageExplorerResources.ksStatisticsViewTotalWordTypesText + "\t"; // Todo: find the right System.?.NewLine constant

			//add one row for the unique words in each language.
			foreach (var keyValuePair in languageTypeCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				var labText = (ws != null ? ws.ToString() : "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + "\t" + labText + "\t"; // Todo: find the right System.?.NewLine constant
				statisticsBox.Text += "" + keyValuePair.Value.Count;
				uniqueWords += keyValuePair.Value.Count; //increase the total of unique words
			}

			// next insert the word count.
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + LanguageExplorerResources.ksStatisticsViewTotalWordTokensText + "\t"; // Todo: find the right System.?.NewLine constant
			statisticsBox.Text += wordCount;
			//add one row for the token count for each language.
			foreach (var keyValuePair in languageCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				var labText = (ws != null ? ws.ToString() : "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + "\t" + labText + "\t"; // Todo: find the right System.?.NewLine constant
				statisticsBox.Text += "" + keyValuePair.Value;
			}
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + LanguageExplorerResources.ksStatisticsViewTotalSentencesText + "\t"; // Todo: find the right System.?.NewLine constant

			// next insert the sentence count.
			statisticsBox.Text += numberOfSegments;

			// insert the total word type count into the richTextBox (it wasn't available earlier)
			statisticsBox.SelectionStart = statisticsBox.Find(LanguageExplorerResources.ksStatisticsViewTotalWordTypesText) +
										   LanguageExplorerResources.ksStatisticsViewTotalWordTypesText.Length;
			statisticsBox.SelectionLength = 1;
			statisticsBox.SelectedText = "\t" + uniqueWords;

			// Set the font for the header. Do this after we add the other stuff to make sure
			// it doesn't apply to extra text added adjacent to it.
			statisticsBox.Select(0, LanguageExplorerResources.ksStatisticsView_HeaderText.Length);
			statisticsBox.SelectionFont = headerFont;
			statisticsBox.Select(0, 0);
		}

#if RANDYTODO
		public bool OnDisplayAddTexts(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = _interlinearTextsRecordClerk != null;
			display.Visible = display.Enabled;
			return true;
		}

		// TODO: Make this a regular event handler on the "Choose Texts..." menu, when it gets added.
		public bool OnAddTexts(object args)
		{
			bool result = _interlinearTextsRecordClerk.OnAddTexts(args);
			if(result)
			{
				RebuildStatisticsTable();
			}
			return result;
		}
#endif

		#region Implementation of IMainContentControl

		/// <summary>
		/// The control is about to go away, so do something first.
		/// </summary>
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		/// <summary>
		/// The Area name that uses this control.
		/// </summary>
		public string AreaName
		{
			get { return "textsWords"; }
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("StatisticsView has been disposed.");
		}

		#endregion

		/// <summary>
		/// FDO cache.
		/// </summary>
		protected FdoCache Cache
		{
			get
			{
				return _cache;
			}
		}

		// Code to add right click
		void mi_Copy(object sender, EventArgs e)
		{
			statisticsBox.Copy();
		}
	}
}
