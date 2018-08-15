// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal partial class FindExampleSentenceDlg : Form, IFlexComponent
	{
		private StatusBar _statusBar;
		private LcmCache _cache;
		private IRecordList _recordList;
		private ILexExampleSentence _lexExampleSentence;
		private ILexSense _owningSense;
		private ConcOccurrenceBrowseView _concOccurrenceBrowseView;
		private XmlView _previewPane;
		private string _helpTopic = "khtpFindExampleSentence";

		/// <summary />
		public FindExampleSentenceDlg()
		{
			InitializeComponent();
		}

		internal FindExampleSentenceDlg(StatusBar statusBar, ICmObject sourceObject, IRecordList recordList) : this()
		{
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(sourceObject, nameof(sourceObject));
			Guard.AgainstNull(recordList, nameof(recordList));

			_statusBar = statusBar;
			_cache = sourceObject.Cache;
			_recordList = recordList;

			// Find the sense we want examples for, which depends on the kind of source object.
			if (sourceObject is ILexSense)
			{
				_owningSense = (ILexSense)sourceObject;
			}
			else
			{
				_owningSense = sourceObject.OwnerOfClass<ILexSense>();
			}
			if (sourceObject is ILexExampleSentence)
			{
				_lexExampleSentence = (ILexExampleSentence)sourceObject;
			}
			if (_owningSense == null)
			{
				throw new ArgumentException("Invalid object type for sourceObject.");
			}
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
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetShowHelp(this, true);
			var helpToicProvider = PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			if (helpToicProvider != null)
			{
				helpProvider.HelpNamespace = helpToicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, helpToicProvider.GetHelpString(_helpTopic));
				btnHelp.Enabled = true;
			}

			AddConfigurableControls();
		}

		#endregion

		private static XElement BrowseViewControlParameters => XDocument.Parse(TextAndWordsResources.ConcordanceColumns).Root;

		private void AddConfigurableControls()
		{
			var flexParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			// Load the controls.
			// 1. Initialize the preview pane (lower pane)
			_previewPane = new XmlView(0, "publicationNew", false)
			{
				Cache = _cache,
				StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable)
			};
			_previewPane.InitializeFlexComponent(flexParameters);

			var pbc = new BasicPaneBarContainer();
			pbc.Init(PropertyTable, _previewPane, new PaneBar());
			pbc.Dock = DockStyle.Fill;
			pbc.PaneBar.Text = LexiconResources.ksFindExampleSentenceDlgPreviewPaneTitle;
			panel2.Controls.Add(pbc);
			if (_previewPane.RootBox == null)
			{
				_previewPane.MakeRoot();
			}

			// 2. load the browse view. (upper pane)
			// First create our record list (ConcRecordList).
			// This record list is a "TemporaryRecordList" subclass, so we dispose it when the dlg goes away.
			var concDecorator = new ConcDecorator(_cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexParameters);
			_recordList = new ConcRecordList(_statusBar, _cache, concDecorator, _owningSense);
			_recordList.InitializeFlexComponent(flexParameters);

			_concOccurrenceBrowseView = new ConcOccurrenceBrowseView(BrowseViewControlParameters, _cache, _recordList);
			_concOccurrenceBrowseView.InitializeFlexComponent(flexParameters);
			_concOccurrenceBrowseView.Init(_previewPane, _recordList.VirtualListPublisher);
			_concOccurrenceBrowseView.CheckBoxChanged += ConcOccurrenceBrowseViewCheckBoxChanged;

			// add it to our controls.
			var pbc1 = new BasicPaneBarContainer();
			pbc1.Init(PropertyTable, _concOccurrenceBrowseView, new PaneBar());
			pbc1.BorderStyle = BorderStyle.FixedSingle;
			pbc1.Dock = DockStyle.Fill;
			pbc1.PaneBar.Text = LexiconResources.ksFindExampleSentenceDlgBrowseViewPaneTitle;
			panel1.Controls.Add(pbc1);

			CheckAddBtnEnabling();
		}

		void ConcOccurrenceBrowseViewCheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			CheckAddBtnEnabling();
		}

		private void CheckAddBtnEnabling()
		{
			btnAdd.Enabled = _concOccurrenceBrowseView.CheckedItems.Any();
		}
		private void btnAdd_Click(object sender, EventArgs e)
		{
			// Get the checked occurrences;
			var occurrences = _concOccurrenceBrowseView.CheckedItems;
			if (!occurrences.Any())
			{
				// do nothing.
				return;
			}
			var uniqueSegments = (occurrences.Select(fake => _recordList.VirtualListPublisher.get_ObjectProp(fake, ConcDecorator.kflidSegment))).Distinct().ToList();
			var insertIndex = _owningSense.ExamplesOS.Count; // by default, insert at the end.
			if (_lexExampleSentence != null)
			{
				// we were given a LexExampleSentence, so set our insertion index after the given one.
				insertIndex = _owningSense.ExamplesOS.IndexOf(_lexExampleSentence) + 1;
			}

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAddExamples, LanguageExplorerResources.ksRedoAddExamples, _cache.ActionHandlerAccessor, () =>
			{
				var cNewExamples = 0;
				foreach (var segHvo in uniqueSegments)
				{
					var seg = _cache.ServiceLocator.GetObject(segHvo) as ISegment;
					ILexExampleSentence newLexExample;
					if (cNewExamples == 0 && _lexExampleSentence != null &&
						_lexExampleSentence.Example.BestVernacularAlternative.Text == "***" &&
						(_lexExampleSentence.TranslationsOC == null || _lexExampleSentence.TranslationsOC.Count == 0) &&
						_lexExampleSentence.Reference.Length == 0)
					{
						// we were given an empty LexExampleSentence, so use this one for our first new Example.
						newLexExample = _lexExampleSentence;
					}
					else
					{
						// create a new example sentence.
						newLexExample = _cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
						_owningSense.ExamplesOS.Insert(insertIndex + cNewExamples, newLexExample);
						cNewExamples++;
					}
					// copy the segment string into the new LexExampleSentence
					// Enhance: bold the relevant occurrence(s).
					// LT-11388 Make sure baseline text gets copied into correct ws
					var baseWs = GetBestVernWsForNewExample(seg);
					newLexExample.Example.set_String(baseWs, seg.BaselineText);
					if (seg.FreeTranslation.AvailableWritingSystemIds.Length > 0)
					{
						var trans = _cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(newLexExample,
							_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation));
						trans.Translation.CopyAlternatives(seg.FreeTranslation);
					}
					if (seg.LiteralTranslation.AvailableWritingSystemIds.Length > 0)
					{
						var trans = _cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(newLexExample,
							_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranLiteralTranslation));
						trans.Translation.CopyAlternatives(seg.LiteralTranslation);
					}
					// copy the reference.
					var tssRef = seg.Paragraph.Reference(seg, seg.BeginOffset);
					// convert the plain reference string into a link.
					var tsb = tssRef.GetBldr();
					var fwl = new FwLinkArgs(AreaServices.InterlinearEditMachineName, seg.Owner.Owner.Guid);
					tsb.SetStrPropValue(0, tsb.Length, (int)FwTextPropType.ktptObjData, (char)FwObjDataTypes.kodtExternalPathName + fwl.ToString());
					tsb.SetStrPropValue(0, tsb.Length, (int)FwTextPropType.ktptNamedStyle, "Hyperlink");
					newLexExample.Reference = tsb.GetString();
				}
			});
		}

		private int GetBestVernWsForNewExample(ISegment seg)
		{
			var baseWs = seg.BaselineText.get_WritingSystem(0);
			if (baseWs < 1)
			{
				return _cache.DefaultVernWs;
			}

			var possibleWss = _cache.ServiceLocator.WritingSystems.VernacularWritingSystems;
			var wsObj = _cache.ServiceLocator.WritingSystemManager.Get(baseWs);
			return possibleWss.Contains(wsObj) ? baseWs : _cache.DefaultVernWs;
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), _helpTopic);
		}
	}
}