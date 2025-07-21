// Copyright (c) 2019-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Icu;
using SilEncConverters40;
using SIL.Code;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Keyboarding;
using SIL.LCModel;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Lexicon;
using SIL.Windows.Forms.WritingSystems;
using SIL.WritingSystems;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Presentation model for the WritingSystemConfiguration dialog. Handles one list worth of writing systems.
	/// e.g. Analysis, or Vernacular
	/// </summary>
	public class FwWritingSystemSetupModel
	{
		/// <summary/>
		public enum ListType
		{
			/// <summary/>
			Vernacular,
			/// <summary/>
			Analysis,
			/// <summary/>
			Pronunciation
		}

		/// <summary/>
		public List<WSListItemModel> WorkingList
		{
			get;
			set;
		}
		private CoreWritingSystemDefinition _currentWs;
		private readonly ListType _listType;
		private readonly IWritingSystemManager _wsManager;
		private string _languageName;
		private WritingSystemSetupModel _currentWsSetupModel;
		private readonly ISet<string> _wsIdsToDelete = new HashSet<string>();
		private readonly Dictionary<CoreWritingSystemDefinition, CoreWritingSystemDefinition> _mergedWritingSystems = new Dictionary<CoreWritingSystemDefinition, CoreWritingSystemDefinition>();


		// function for retrieving Encoding converter keys, internal to allow mock results in unit tests
		internal Func<ICollection> EncodingConverterKeys = () =>
		{
			try
			{
				var encConverters = new EncConverters();
				return encConverters.Keys;
			}
			catch (Exception)
			{
				// If we can't use encoding converters don't crash, just return an empty list
				return new string[] { };
			}
		};

		/// <summary/>
		public readonly LcmCache Cache;

		private readonly Mediator _mediator;

		/// <summary/>
		public event EventHandler WritingSystemListUpdated;

		/// <summary>
		/// This event is fired only when the id or abbreviation for a writing system changes
		/// </summary>
		public event EventHandler WritingSystemUpdated;

		/// <param name="message">the  message to display</param>
		/// <param name="needResponse">True if the user needs to provide a response (Yes or No); false otherwise (only an OK button is shown)</param>
		/// <returns>True if the user clicks Yes</returns>
		public delegate bool ShowMessageBoxDelegate(string message, bool needResponse = false);

		/// <summary/>
		public delegate bool ChangeLanguageDelegate(out LanguageInfo info);

		/// <summary/>
		public delegate void ViewHiddenWritingSystemsDelegate(ViewHiddenWritingSystemsModel model);

		/// <summary/>
		public delegate void ValidCharacterDelegate();

		/// <summary/>
		public delegate bool ModifyConvertersDelegate(string originalConverter, out string selectedConverter);

		/// <summary/>
		public delegate bool ConfirmDeleteWritingSystemDelegate(string wsDisplayLabel);

		/// <summary/>
		public delegate bool ConfirmMergeWritingSystemDelegate(string wsToMerge, out CoreWritingSystemDefinition wsTag);

		/// <summary/>
		public delegate void ImportTranslatedListDelegate(string icuLocaleToImport);

		/// <summary/>
		public delegate bool SharedWsChangeDelegate(string originalLanguage);

		/// <summary/>
		public delegate bool AddNewVernacularLanguageDelegate();

		/// <summary/>
		public delegate bool ChangeHomographWs(string newHomographWs);

		/// <summary/>
		public delegate bool ConfirmClearAdvancedDelegate();

		/// <returns>True if the user clicks Yes</returns>
		public ShowMessageBoxDelegate ShowMessageBox;

		/// <summary/>
		public ChangeLanguageDelegate ShowChangeLanguage;

		/// <summary/>
		public ViewHiddenWritingSystemsDelegate ViewHiddenWritingSystems;

		/// <summary/>
		public ValidCharacterDelegate ShowValidCharsEditor;

		/// <summary/>
		public ModifyConvertersDelegate ShowModifyEncodingConverters;

		/// <summary/>
		public ConfirmDeleteWritingSystemDelegate ConfirmDeleteWritingSystem;

		/// <summary/>
		public SharedWsChangeDelegate AcceptSharedWsChangeWarning;

		/// <summary/>
		public AddNewVernacularLanguageDelegate AddNewVernacularLanguageWarning;

		/// <summary/>
		public ChangeHomographWs ShouldChangeHomographWs;

		/// <summary/>
		public ImportTranslatedListDelegate ImportListForNewWs;

		/// <summary/>
		public ConfirmMergeWritingSystemDelegate ConfirmMergeWritingSystem;

		/// <summary/>
		public ConfirmClearAdvancedDelegate ConfirmClearAdvanced;

		private IWritingSystemContainer _wsContainer;
		private ProjectLexiconSettingsDataMapper _projectLexiconSettingsDataMapper;
		private ProjectLexiconSettings _projectLexiconSettings;
		// We need to know if the homographWs was equal to the top vernacular writing system on construction
		// to be able to show warnings triggered by changing this.
		private bool _homographWsWasTopVern;
		// We need to know if the homographWs was in the current list on construction
		// to be able to show warnings triggered by removing it from the current list
		private bool _homographWsWasInCurrent;
		// backing variable for when the user checks the box on something that doesn't require advanced view yet
		private bool _showAdvancedView;

		/// <summary>
		/// event raised when the writing system has been changed by the presenter
		/// </summary>
		public event EventHandler OnCurrentWritingSystemChanged = delegate { };

		/// <summary/>
		public FwWritingSystemSetupModel(IWritingSystemContainer container, ListType type, IWritingSystemManager wsManager = null, LcmCache cache = null, Mediator mediator = null)
		{
			switch (type)
			{
				case ListType.Analysis:
					WorkingList = BuildWorkingList(container.AnalysisWritingSystems, container.CurrentAnalysisWritingSystems);
					break;
				case ListType.Vernacular:
					WorkingList = BuildWorkingList(container.VernacularWritingSystems, container.CurrentVernacularWritingSystems);
					break;
				case ListType.Pronunciation:
					throw new NotImplementedException();
			}

			_currentWs = WorkingList.First().WorkingWs;
			_listType = type;
			_wsManager = wsManager;
			SetCurrentWsSetupModel(_currentWs);
			Cache = cache;
			_mediator = mediator;
			_wsContainer = container;
			_projectLexiconSettings = new ProjectLexiconSettings();
			// ignore on disk settings if we are testing without a cache
			if (Cache != null)
			{
				_projectLexiconSettingsDataMapper = new ProjectLexiconSettingsDataMapper(Cache?.ServiceLocator.DataSetup.ProjectSettingsStore);
				_projectLexiconSettingsDataMapper.Read(_projectLexiconSettings);
				_homographWsWasTopVern = WorkingList.First(ws => ws.InCurrentList).OriginalWs.Id == Cache.LangProject.HomographWs;
				// guard against homograph ws not being in the project for paranoia's sake
				var homographWs = WorkingList.FirstOrDefault(ws => ws.OriginalWs.Id == Cache.LangProject.HomographWs);
				_homographWsWasInCurrent = homographWs?.InCurrentList ?? false;
			}
		}

		private List<WSListItemModel> BuildWorkingList(ICollection<CoreWritingSystemDefinition> allForType, IList<CoreWritingSystemDefinition> currentForType)
		{
			var list = new List<WSListItemModel>();
			// Don't reorder the writing systems here even though the lexical data fields are reordered (cf. LT-22136).
			var wssInPreferredOrder = allForType;
			foreach (var ws in wssInPreferredOrder)
			{
				list.Add(new WSListItemModel(currentForType.Contains(ws), ws, new CoreWritingSystemDefinition(ws, true)));
			}
			return list;
		}

		/// <summary/>
		public WritingSystemSetupModel CurrentWsSetupModel
		{
			get { return _currentWsSetupModel; }

			private set
			{
				_currentWsSetupModel = value;
				_languageName = _currentWsSetupModel.CurrentLanguageName;
			}
		}

		private void SetCurrentWsSetupModel(WritingSystemDefinition ws)
		{
			CurrentWsSetupModel = new WritingSystemSetupModel(ws);
			if (IsTheOriginalPlainEnglish())
			{
				CurrentWsSetupModel.CurrentItemUpdated += PreventChangingPlainEnglish;
			}
		}

		private void PreventChangingPlainEnglish(object sender, EventArgs args)
		{
			if (CurrentWsSetupModel.CurrentLanguageTag != "en")
			{
				_currentWs.Script = null;
				_currentWs.Region = null;
				_currentWs.Variants.Clear();
				ShowMessageBox(string.Format(FwCoreDlgs.kstidCantChangeEnglishSRV, CurrentWsSetupModel.CurrentDisplayLabel));
				// TODO (Hasso) 2019.05: reset the Special combobox to None (possibly by refreshing the entire view)
			}
		}

		/// <summary>
		/// This indicates if the advanced Script/Region/Variant view should be used
		/// </summary>
		public bool ShowAdvancedScriptRegionVariantView
		{
			get
			{
				return _showAdvancedView || _currentWs.Language.IsPrivateUse ||
					_currentWs.Script != null && _currentWs.Script.IsPrivateUse ||
					_currentWs.Region != null && _currentWs.Region.IsPrivateUse ||
					_currentWs.Variants.Count > 1 && !_currentWs.Variants.First().IsPrivateUse;
			}
			set
			{
				if (ShowAdvancedScriptRegionVariantView && !_currentWs.Language.IsPrivateUse && ConfirmClearAdvanced())
				{
					if (_currentWs.Region != null && _currentWs.Region.IsPrivateUse)
					{
						_currentWs.Region = null;
					}
					if (_currentWs.Script != null && _currentWs.Script.IsPrivateUse)
					{
						_currentWs.Script = null;
					}

					if (_currentWs.Variants.Count > 1)
					{
						_currentWs.Variants.RemoveRangeAt(1, _currentWs.Variants.Count - 1);
					}
					_showAdvancedView = value;
				}
				else if (!ShowAdvancedScriptRegionVariantView)
				{
					_showAdvancedView = true;
				}
			}
		}

		/// <summary>
		/// This is used to determine if the 'Advanced' checkbox should be shown under the writing system identity control
		/// </summary>
		public bool ShowAdvancedScriptRegionVariantCheckBox
		{
			get
			{
				return CurrentWsSetupModel.SelectionForSpecialCombo == WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant ||
					ShowAdvancedScriptRegionVariantView;
			}
		}

		/// <summary>
		/// This indicates if the Graphite Font options should be configurable
		/// </summary>
		public bool EnableGraphiteFontOptions
		{
			get
			{
				return _currentWs?.DefaultFont != null && _currentWs.DefaultFont.Engines.HasFlag(FontEngines.Graphite);
			}
		}

		/// <summary/>
		public bool TryGetFont(string text, out FontDefinition font)
		{
			return _currentWs.Fonts.TryGet(text, out font);
		}

		/// <summary/>
		public bool CanMoveUp()
		{
			return WorkingList.Count > 1 && WorkingList.First().WorkingWs != _currentWs;
		}

		/// <summary/>
		public bool CanMoveDown()
		{
			return WorkingList.Count > 1 && WorkingList.Last().WorkingWs != _currentWs;
		}

		/// <summary/>
		public bool CanMerge()
		{
			return WorkingList.Count > 1 && !IsCurrentWsNew() && !IsPlainEnglish();
		}

		/// <summary/>
		public bool CanDelete()
		{
			// The only remaining WS cannot be deleted from the list.
			// Plain English is a required Analysis WS, but it can be removed from other lists.
			return WorkingList.Count > 1 && (_listType != ListType.Analysis || !IsTheOriginalPlainEnglish());
		}

		private bool IsPlainEnglish()
		{
			return CurrentWsSetupModel.CurrentLanguageTag == "en";
		}

		/// <remarks>The original plain English is a required WS that cannot be changed or deleted</remarks>
		private bool IsTheOriginalPlainEnglish()
		{
			var origWs = WorkingList[CurrentWritingSystemIndex].OriginalWs;
			return origWs != null && origWs.LanguageTag == "en";
		}

		/// <summary/>
		public void SelectWs(string wsTag)
		{
			// didn't change, no-op
			if (wsTag == _currentWs.LanguageTag)
				return;
			SelectWs(WorkingList.First(ws => ws.WorkingWs.LanguageTag == wsTag).WorkingWs);
		}

		/// <summary/>
		public void SelectWs(int index)
		{
			// didn't change, no-op
			if (index == CurrentWritingSystemIndex)
				return;
			SelectWs(WorkingList[index].WorkingWs);
		}

		private void SelectWs(CoreWritingSystemDefinition ws)
		{
			_currentWs = ws;
			SetCurrentWsSetupModel(_currentWs);
			OnCurrentWritingSystemChanged(this, EventArgs.Empty);
		}

		/// <summary/>
		public void ToggleInCurrentList()
		{
			var index = CurrentWritingSystemIndex;
			var newListItem = new WSListItemModel(!WorkingList[index].InCurrentList,
				WorkingList[index].OriginalWs,
				WorkingList[index].WorkingWs);
			WorkingList.RemoveAt(index);
			WorkingList.Insert(index, newListItem);
			CurrentWsListChanged = true;
		}

		/// <summary/>
		public void MoveUp()
		{
			var currentItem = WorkingList.Find(ws => ws.WorkingWs == _currentWs);
			var currentIndex = WorkingList.IndexOf(currentItem);
			Guard.Against(currentIndex >= WorkingList.Count, "Programming error: Invalid state for MoveUp");

			WorkingList.Remove(currentItem);
			WorkingList.Insert(currentIndex - 1, currentItem);
			if (currentItem.InCurrentList)
				CurrentWsListChanged = true;
		}

		/// <summary/>
		public void MoveDown()
		{
			var currentItem = WorkingList.Find(ws => ws.WorkingWs == _currentWs);
			var currentIndex = WorkingList.IndexOf(currentItem);
			Guard.Against(currentIndex >= WorkingList.Count, "Programming error: Invalid state for MoveUp");

			WorkingList.Remove(currentItem);
			WorkingList.Insert(currentIndex + 1, currentItem);
			if (currentItem.InCurrentList)
				CurrentWsListChanged = true;
		}

		/// <summary/>
		public bool IsListValid
		{
			get
			{
				return IsAtLeastOneSelected && FirstDuplicateWs == null;
			}
		}

		/// <summary/>
		public bool IsAtLeastOneSelected
		{
			get
			{
				return WorkingList.Any(item => item.InCurrentList);
			}
		}

		/// <returns>the DisplayLabel of the first duplicate WS; if there are no duplcates, <c>null</c></returns>
		public string FirstDuplicateWs
		{
			get
			{
				var langTagSet = new HashSet<string>();
				foreach (var ws in WorkingList)
				{
					if (langTagSet.Contains(ws.WorkingWs.LanguageTag))
						return ws.WorkingWs.DisplayLabel;
					langTagSet.Add(ws.WorkingWs.LanguageTag);
				}

				return null;
			}
		}

		/// <summary/>
		public string Title
		{
			get { return string.Format("{0} Writing System Properties", _listType.ToString()); }
		}

		/// <summary>
		/// The code for just the language part of the language tag. e.g. the en in en-Latn-US
		/// </summary>
		public string LanguageCode
		{
			get { return _currentWs?.Language.Iso3Code; }
		}

		/// <summary>
		/// The language name corresponding to just the language part of the tag e.g. French for fr-fonipa
		/// </summary>
		public string LanguageName
		{
			get { return _languageName; }
			set
			{
				if (!string.IsNullOrEmpty(value) && value != _languageName)
				{
					foreach (var relatedWs in WorkingList.Where(ws => ws.WorkingWs.Language.Code == _currentWs.Language.Code))
					{
						relatedWs.WorkingWs.Language = new LanguageSubtag(relatedWs.WorkingWs.Language, value);
					}
				}
				_languageName = value;
			}
		}

		/// <summary>
		/// The descriptive name for the current writing system
		/// </summary>
		public string WritingSystemName
		{
			get { return _currentWs.DisplayLabel; }
		}

		/// <summary/>
		public string EthnologueLabel => string.Format(FwCoreDlgs.ksWSPropEthnologueEntryFor, LanguageCode);

		/// <summary/>
		public string EthnologueLink => $"https://www.ethnologue.com/show_language.asp?code={LanguageCode}";

		/// <summary/>
		public int CurrentWritingSystemIndex
		{
			get { return WorkingList.FindIndex(ws => ws.WorkingWs == _currentWs); }
		}

		/// <summary/>
		public bool IsGraphiteEnabled
		{
			get { return _currentWs.IsGraphiteEnabled; }
			set { _currentWs.IsGraphiteEnabled = value; }
		}

		/// <summary/>
		public string CurrentDefaultFontFeatures
		{
			get { return _currentWs.DefaultFontFeatures; }
		}

		/// <summary/>
		public FontDefinition CurrentDefaultFont
		{
			get { return _currentWs.DefaultFont; }
			set { _currentWs.DefaultFont = value; }
		}

		/// <summary/>
		public void ChangeLanguage()
		{
			if (_currentWs.Language.Code == "en")
			{
				ShowMessageBox(FwCoreDlgs.kstidCantChangeEnglishWS);
				return;
			}

			if (ShowChangeLanguage(out var info))
			{
				if (!IetfLanguageTag.TryGetSubtags(info.LanguageTag, out var languageSubtag, out var scriptSubtag, out var regionSubtag, out _) ||
					WorkingList.Exists(ws => ws.WorkingWs.Language.Code == languageSubtag.Code) &&
						!ShowMessageBox(string.Format(FwCoreDlgs.ksWouldCauseDuplicateWSConfirm, info.LanguageTag, info.DesiredName), true) ||
					!CheckChangingWSForSRProject())
				{
					return;
				}

				var languagesToChange = new List<WSListItemModel>(WorkingList.Where(ws => ws.WorkingWs.LanguageName == _languageName));
				languageSubtag = new LanguageSubtag(languageSubtag, info.DesiredName);
				IetfLanguageTag.TryGetSubtags(languagesToChange[0].WorkingWs.Language.Code, out _, out var oldDefaultScriptSubtag, out _, out _);

				foreach (var ws in languagesToChange)
				{
					ws.WorkingWs.Language = languageSubtag;
					if (ws.WorkingWs.Script == null || ws.WorkingWs.Script == oldDefaultScriptSubtag)
						ws.WorkingWs.Script = scriptSubtag;
					if (ws.WorkingWs.Region == null)
						ws.WorkingWs.Region = regionSubtag;
				}

				// Set the private language name
				_languageName = info.DesiredName;
			}
		}

		/// <summary>
		/// Check if the writing system is being changed and prompt the user with instructions to successfully perform the change
		/// </summary>
		private bool CheckChangingWSForSRProject()
		{
			bool hasFlexOrLiftRepo = FLExBridgeHelper.DoesProjectHaveFlexRepo(Cache?.ProjectId) || FLExBridgeHelper.DoesProjectHaveLiftRepo(Cache?.ProjectId);

			if (hasFlexOrLiftRepo)
			{
				foreach (var ws in WorkingList)
				{
					if (ws.OriginalWs == null)
						continue;
					if (ws.WorkingWs.LanguageTag != ws.OriginalWs.LanguageTag)
					{
						if (AcceptSharedWsChangeWarning(ws.OriginalWs.LanguageName))
						{
							return true;
						}
					}
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Save all the writing system changes into the container
		/// </summary>
		public void Save()
		{
			// Update the writing system data
			// when this dialog is called from the new language project dialog, there is no FDO cache,
			// but we still need to update the WorkingWs manager, so we have to execute the save even if Cache is null
			NonUndoableUnitOfWorkHelper uowHelper = null;
			if (Cache != null)
			{
				uowHelper = new NonUndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor);
			}

			try
			{
				IList<CoreWritingSystemDefinition> currentWritingSystems;
				ICollection<CoreWritingSystemDefinition> allWritingSystems;
				// track the other list to see if a removed writing system should actually be deleted
				ICollection<CoreWritingSystemDefinition> otherWritingSystems;
				switch (_listType)
				{
					case ListType.Vernacular:
					{
						currentWritingSystems = _wsContainer.CurrentVernacularWritingSystems;
						allWritingSystems = _wsContainer.VernacularWritingSystems;
						otherWritingSystems = _wsContainer.AnalysisWritingSystems;
						break;
					}
					case ListType.Analysis:
					{
						currentWritingSystems = _wsContainer.CurrentAnalysisWritingSystems;
						allWritingSystems = _wsContainer.AnalysisWritingSystems;
						otherWritingSystems = _wsContainer.VernacularWritingSystems;
						break;
					}
					default:
						throw new NotImplementedException($"{_listType} not yet supported.");
				}

				// Track the new writing systems for importing translated lists
				var newWritingSystems = new List<CoreWritingSystemDefinition>();
				// Adjust the homograph writing system after possibly interacting with the user
				HandleHomographWsChanges(_homographWsWasTopVern, WorkingList, Cache?.LangProject.HomographWs, _homographWsWasInCurrent);

				// Handle hidden and deleted writing systems
				RemoveWritingSystems(currentWritingSystems, allWritingSystems, otherWritingSystems, WorkingList.Select(ws => ws.WorkingWs));

				for (int workingListIndex = 0, curIndex = 0; workingListIndex < WorkingList.Count; ++workingListIndex)
				{
					var wsListItem = WorkingList[workingListIndex];
					var workingWs = wsListItem.WorkingWs;
					var origWs = wsListItem.OriginalWs;

					if (IsNew(wsListItem))
					{
						// origWs is used to update the order
						origWs = workingWs;
						// Create the new writing system, overwriting any existing ws of the same id
						_wsManager.Replace(origWs);
						newWritingSystems.Add(origWs);
					}
					else if (workingWs.IsChanged)
					{
						var didAbbrevOrIdChange = origWs.Abbreviation != workingWs.Abbreviation;
						var oldId = origWs.Id;
						var oldHandle = origWs.Handle;
						// copy the working writing system content into the original writing system
						origWs.Copy(workingWs);
						if (string.IsNullOrEmpty(oldId) || !IetfLanguageTag.AreTagsEquivalent(oldId, workingWs.LanguageTag))
						{
							// update the ID
							_wsManager.Replace(origWs);
							if (uowHelper != null)
							{
								WritingSystemServices.UpdateWritingSystemId(Cache, origWs, oldHandle, oldId);
							}
							didAbbrevOrIdChange = true;
						}
						if (didAbbrevOrIdChange)
						{
							WritingSystemUpdated?.Invoke(this, EventArgs.Empty);
						}
						_mediator?.SendMessage("WritingSystemUpdated", origWs.Id);
					}

					// whether or not the WS was created or changed, its list position may have changed (LT-19788)
					AddOrMoveInList(allWritingSystems, workingListIndex, origWs);
					if (wsListItem.InCurrentList)
					{
						AddOrMoveInList(currentWritingSystems, curIndex, origWs);
						++curIndex;
					}
					else
					{
						SafelyRemoveFromList(currentWritingSystems, wsListItem);
					}
				}
				// Handle any merged writing systems
				foreach (var mergedWs in _mergedWritingSystems)
				{
					WritingSystemServices.MergeWritingSystems(Cache, _wsManager.Get(mergedWs.Key.Id), _wsManager.Get(mergedWs.Value.Id));
				}
				// Save all the changes to the current writing systems
				_wsManager.Save();
				foreach (var newWs in newWritingSystems)
				{
					ImportListForNewWs(newWs.IcuLocale);
				}

				_projectLexiconSettingsDataMapper?.Write(_projectLexiconSettings);
				if (uowHelper != null)
				{
					uowHelper.RollBack = false;
				}
			}
			finally
			{
				if (CurrentWsListChanged)
				{
					WritingSystemListUpdated?.Invoke(this, EventArgs.Empty);
				}
				if (uowHelper != null)
					uowHelper.Dispose();
			}
		}

		private static void AddOrMoveInList(ICollection<CoreWritingSystemDefinition> allWritingSystems, int desiredIndex, CoreWritingSystemDefinition workingWs)
		{
			// copy original contents into a list
			var updatedList = new List<CoreWritingSystemDefinition>(allWritingSystems);
			var ws = updatedList.Find(listItem => listItem.Id == (string.IsNullOrEmpty(workingWs.Id) ? workingWs.LanguageTag : workingWs.Id));
			if (ws != null)
			{
				updatedList.Remove(ws);
			}
			if (desiredIndex > updatedList.Count)
			{
				updatedList.Add(workingWs);
			}
			else
			{
				updatedList.Insert(desiredIndex, workingWs);
			}

			allWritingSystems.Clear();
			allWritingSystems.AddRange(updatedList);
		}

		/// <summary>
		/// Remove the writing system associated with this list item from the given list (unless it didn't exist there before)
		/// </summary>
		/// <returns>true if item was removed</returns>
		private static void SafelyRemoveFromList(IList<CoreWritingSystemDefinition> currentWritingSystems, WSListItemModel wsListItem)
		{
			if (wsListItem.OriginalWs != null)
			{
				currentWritingSystems.Remove(wsListItem.OriginalWs);
			}
		}

		private void HandleHomographWsChanges(bool homographWsWasTopVern, List<WSListItemModel> workingList, string homographWs, bool wasSelected)
		{
			if (_listType != ListType.Vernacular || Cache == null)
			{
				return;
			}
			// If the homograph writing system has been removed then change to the top current vernacular with no user interaction
			if (workingList.All(ws => ws.OriginalWs?.Id != homographWs))
			{
				Cache.LangProject.HomographWs = workingList.First(ws => ws.InCurrentList).WorkingWs.Id;
				return;
			}
			var userWantsChange = false;
			var newTopVernacular = workingList.First(ws => ws.InCurrentList);
			if (homographWsWasTopVern)
			{
				// if the top language is new or different then display the question
				if (newTopVernacular.OriginalWs == null || newTopVernacular.OriginalWs.Id != homographWs)
				{
					userWantsChange = ShouldChangeHomographWs(newTopVernacular.WorkingWs.DisplayLabel);
				}
			}
			else if(wasSelected && !workingList.First(ws => ws.OriginalWs?.Id == homographWs).InCurrentList)
			{
				userWantsChange = ShouldChangeHomographWs(newTopVernacular.WorkingWs.DisplayLabel);
			}
			if (userWantsChange)
			{
				Cache.LangProject.HomographWs = workingList.First(ws => ws.InCurrentList).WorkingWs.Id ?? workingList.First(ws => ws.InCurrentList).WorkingWs.LanguageTag;
			}
		}

		/// <summary>
		/// Hide any writing systems that were removed from the active list.
		/// If any are marked for deletion and are not in the other list, delete their data, too.
		/// </summary>
		/// <param name="currentWritingSystems">WS's currently displayed for this type</param>
		/// <param name="allWritingSystems">All WS's in the project for this type</param>
		/// <param name="otherWritingSystems">All WS's in the project for the opposite type</param>
		/// <param name="workingWritingSystems">WS list that the user has been editing in this dialog session</param>
		private void RemoveWritingSystems(
			ICollection<CoreWritingSystemDefinition> currentWritingSystems,
			ICollection<CoreWritingSystemDefinition> allWritingSystems,
			ICollection<CoreWritingSystemDefinition> otherWritingSystems,
			IEnumerable<CoreWritingSystemDefinition> workingWritingSystems)
		{
			var removedWritingSystems = new List<CoreWritingSystemDefinition>(allWritingSystems);
			removedWritingSystems.RemoveAll(ws => workingWritingSystems.Any(wws => wws.Id == ws.Id));
			_wsIdsToDelete.RemoveAll(wsId => workingWritingSystems.Any(wws => wws.Id == wsId));
			foreach (var deleteCandidate in removedWritingSystems)
			{
				currentWritingSystems.Remove(deleteCandidate);
				allWritingSystems.Remove(deleteCandidate);
			}

			if (Cache == null)
			{
				// The cache will be null while creating a new project, in which case we aren't really deleting anything
				return;
			}

			var deletedWsIds = new List<string>();
			foreach (var deleteCandidateId in _wsIdsToDelete)
			{
				if (Cache.ServiceLocator.WritingSystemManager.TryGet(deleteCandidateId, out var deleteCandidate)
					&& !otherWritingSystems.Contains(deleteCandidate)
					&& !_mergedWritingSystems.Keys.Contains(deleteCandidate))
				{
					WritingSystemServices.DeleteWritingSystem(Cache, deleteCandidate);
					deletedWsIds.Add(deleteCandidateId);
				}
			}

			if (deletedWsIds.Count > 0)
			{
				_mediator?.SendMessage("WritingSystemDeleted", deletedWsIds.ToArray());
			}
		}

		private static bool IsNew(WSListItemModel tempWs)
		{
			return tempWs.OriginalWs == null;
		}

		private bool IsCurrentWsNew()
		{
			return IsNew(WorkingList[CurrentWritingSystemIndex]);
		}

		/// <summary/>
		public List<WSMenuItemModel> GetAddMenuItems()
		{
			var addIpaInputSystem = FwCoreDlgs.WritingSystemList_AddIpa;
			var addAudioInputSystem = FwCoreDlgs.WritingSystemList_AddAudio;
			var addDialect = FwCoreDlgs.WritingSystemList_AddDialect;
			var addNewLanguage = FwCoreDlgs.WritingSystemList_AddNewLanguage;
			var menuItemList = new List<WSMenuItemModel>();
			if (!ListHasIpaForSelectedWs())
			{
				menuItemList.Add(new WSMenuItemModel(string.Format(addIpaInputSystem, CurrentWsSetupModel.CurrentLanguageName),
					AddIpaHandler));
			}
			if (!ListHasVoiceForSelectedWs())
			{
				menuItemList.Add(new WSMenuItemModel(string.Format(addAudioInputSystem, CurrentWsSetupModel.CurrentLanguageName),
					AddAudioHandler));
			}

			menuItemList.Add(new WSMenuItemModel(string.Format(addDialect, CurrentWsSetupModel.CurrentLanguageName),
				AddDialectHandler));
			menuItemList.Add(new WSMenuItemModel(addNewLanguage, AddNewLanguageHandler));
			// If there is a cache, allow the user to view all WS's with text. If there is no cache, there is no text.
			if (Cache != null)
			{
				menuItemList.Add(new WSMenuItemModel(FwCoreDlgs.WritingSystemList_ViewHiddenWSs, ViewHiddenWritingSystemsHandler));
			}
			return menuItemList;
		}

		/// <summary/>
		public List<WSMenuItemModel> GetRightClickMenuItems()
		{
			var deleteWritingSystem = FwCoreDlgs.WritingSystemList_DeleteWs;
			var hideWritingSystem = FwCoreDlgs.WritingSystemList_HideWs;
			var mergeWritingSystem = FwCoreDlgs.WritingSystemList_MergeWs;
			var updateWritingSystem = FwCoreDlgs.WritingSystemList_UpdateWs;
			var menuItemList = new List<WSMenuItemModel>();
			if (CanMerge())
			{
				menuItemList.Add(new WSMenuItemModel(mergeWritingSystem, MergeWritingSystem));
			}
			menuItemList.Add(new WSMenuItemModel(string.Format(updateWritingSystem, CurrentWsSetupModel.CurrentDisplayLabel),
				UpdateCurrentWritingSystem, !IsCurrentWsNew(), FwCoreDlgs.WritingSystemList_UpdateWsTooltip));
			menuItemList.Add(new WSMenuItemModel(string.Format(hideWritingSystem, CurrentWsSetupModel.CurrentDisplayLabel),
				HideCurrentWritingSystem, CanDelete() && !IsCurrentWsNew()));
			menuItemList.Add(new WSMenuItemModel(string.Format(deleteWritingSystem, CurrentWsSetupModel.CurrentDisplayLabel),
				DeleteCurrentWritingSystem, CanDelete()));
			return menuItemList;
		}

		private void MergeWritingSystem(object sender, EventArgs e)
		{
			CoreWritingSystemDefinition mergeWithWsId;
			if (ConfirmMergeWritingSystem(CurrentWsSetupModel.CurrentDisplayLabel, out mergeWithWsId))
			{
				// If we are in the new language project dialog we do not need to track the merged writing systems
				if (Cache != null)
				{
					_mergedWritingSystems[WorkingList[CurrentWritingSystemIndex].OriginalWs] = mergeWithWsId;
				}
				WorkingList.RemoveAt(CurrentWritingSystemIndex);
				CurrentWsListChanged = true;
				SelectWs(WorkingList.First().WorkingWs);
			}
		}

		private void HideCurrentWritingSystem(object sender, EventArgs e)
		{
			_wsIdsToDelete.Remove(_wsIdsToDelete.FirstOrDefault(wsId => wsId == _currentWs.Id));
			HideCurrentWritingSystem();
		}

		// REVIEW (Hasso) 2021.03: should we offer to delete the current working WS (current behaviour), or the original WS associated with the current WS?
		private void DeleteCurrentWritingSystem(object sender, EventArgs e)
		{
			// If the writing system is in the other list as well, simply hide it silently.
			var otherList = _listType == ListType.Vernacular ? _wsContainer.AnalysisWritingSystems : _wsContainer.VernacularWritingSystems;
			if (otherList.Contains(_currentWs) || // will be hidden, not deleted
				IsCurrentWsNew()) // it hasn't been created yet, so it has no data
			{
				HideCurrentWritingSystem();
				return;
			}

			if (ConfirmDeleteWritingSystem(CurrentWsSetupModel.CurrentDisplayLabel)) // prompt the user to delete the WS and its data
			{
				_wsIdsToDelete.Add(_currentWs.Id);
				HideCurrentWritingSystem();
			}
		}

		private void HideCurrentWritingSystem()
		{
			if (WorkingList[CurrentWritingSystemIndex].InCurrentList)
			{
				CurrentWsListChanged = true;
			}
			WorkingList.RemoveAt(CurrentWritingSystemIndex);
			SelectWs(WorkingList.First().WorkingWs);
		}

		private void UpdateCurrentWritingSystem(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				var langTag = WorkingList[CurrentWritingSystemIndex].WorkingWs.LanguageTag;
				Cache.UpdateWritingSystemsFromGlobalStore(langTag);
				var updatedWs = new CoreWritingSystemDefinition((CoreWritingSystemDefinition)Cache.WritingSystemFactory.get_Engine(langTag), true);
				switch (_listType)
				{
					case ListType.Analysis:
						WorkingList[CurrentWritingSystemIndex] = new WSListItemModel(_wsContainer.CurrentAnalysisWritingSystems.Contains(WorkingList[CurrentWritingSystemIndex].OriginalWs), WorkingList[CurrentWritingSystemIndex].OriginalWs, updatedWs);
						break;
					case ListType.Vernacular:
						WorkingList[CurrentWritingSystemIndex] = new WSListItemModel(_wsContainer.CurrentVernacularWritingSystems.Contains(WorkingList[CurrentWritingSystemIndex].OriginalWs), WorkingList[CurrentWritingSystemIndex].OriginalWs, updatedWs);
						break;
					case ListType.Pronunciation:
						throw new NotImplementedException();
				}

				_currentWs = updatedWs;
				SelectWs(WorkingList[CurrentWritingSystemIndex].WorkingWs);
			}
		}

		private bool ListHasVoiceForSelectedWs()
		{
			// build a string that represents the tag for an audio input system for
			// the current language and return if it is found in the list.
			var languageCode = _currentWs.Language.Code;
			var audioCode = $"{languageCode}-Zxxx-x-audio";
			return WorkingList.Exists(item => item.WorkingWs.LanguageTag == audioCode);
		}

		private bool ListHasIpaForSelectedWs()
		{
			// build a regex that will match an ipa input system for the current language
			// and return if it is found in the list.
			var languageCode = _currentWs.Language.Code;
			var ipaMatch = $"^{languageCode}(-.*)?-fonipa.*";
			return WorkingList.Exists(item => Regex.IsMatch(item.WorkingWs.LanguageTag, ipaMatch));
		}

		private void AddNewLanguageHandler(object sender, EventArgs e)
		{
			if (_listType == ListType.Vernacular && !AddNewVernacularLanguageWarning())
			{
				return;
			}

			if (ShowChangeLanguage(out var langInfo))
			{
				AddNewLanguage(langInfo);
			}
		}

		private void AddDialectHandler(object sender, EventArgs e)
		{
			AddDialectOf(_currentWs);
		}

		private void AddNewLanguage(LanguageInfo langInfo)
		{
			WSListItemModel wsListItem;
			if (_wsManager.TryGet(langInfo.LanguageTag, out var wsDef))
			{
				// (LT-19728) At this point, wsDef is a live reference to an actual WS in this project.
				// We don't want the user modifying plain English, or modifying any WS without performing the necessary update steps,
				// so create a "new dialect" (if the selected WS is already in the current list)
				// or set the OriginalWS and create a copy for editing (if this is the first instance of the selected WS in the current list)
				if (WorkingList.Any(wItem => wItem.WorkingWs == wsDef))
				{
					// The requested WS already exists in the list; create a dialect
					AddDialectOf(wsDef);
					return;
				}
				// Set the WS up as an existing WS, the same way as existing WS's are set up when the dialog is opened:
				// (later in this method, we set wsDef's Language Name to the user's DesiredName. This needs to happen on the working WS)
				var origWs = wsDef;
				wsDef = new CoreWritingSystemDefinition(wsDef, true);
				wsListItem = new WSListItemModel(true, origWs, wsDef);
			}
			else
			{
				wsDef = _wsManager.Set(langInfo.LanguageTag);
				wsListItem = new WSListItemModel(true, null, wsDef);
			}

			wsDef.Language = new LanguageSubtag(wsDef.Language, langInfo.DesiredName);
			WorkingList.Insert(CurrentWritingSystemIndex + 1, wsListItem);
			CurrentWsListChanged = true;
			SelectWs(wsDef);
		}

		private void AddDialectOf(CoreWritingSystemDefinition baseWs)
		{
			var wsDef = new CoreWritingSystemDefinition(baseWs);
			WorkingList.Insert(CurrentWritingSystemIndex + 1, new WSListItemModel(true, null, wsDef));
			CurrentWsListChanged = true;
			// Set language name to be based on current language
			wsDef.Language = new LanguageSubtag(wsDef.Language, baseWs.LanguageName);
			// Can't use SelectWs because it won't select ScriptRegionVariant in the combobox when no SRV info has been entered
			CurrentWsSetupModel = new WritingSystemSetupModel(wsDef, WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant);
			_currentWs = wsDef;
			OnCurrentWritingSystemChanged(this, EventArgs.Empty);
		}

		private void AddAudioHandler(object sender, EventArgs e)
		{
			var wsDef = new CoreWritingSystemDefinition(_currentWs) {IsVoice = true};
			// Set language name to be based on current language
			wsDef.Language = new LanguageSubtag(wsDef.Language, _currentWs.LanguageName);
			WorkingList.Insert(CurrentWritingSystemIndex + 1, new WSListItemModel(true, null, wsDef));
			CurrentWsListChanged = true;
			SelectWs(wsDef);
		}

		private void AddIpaHandler(object sender, EventArgs e)
		{
			var variants = new List<VariantSubtag> { WellKnownSubtags.IpaVariant };
			variants.AddRange(_currentWs.Variants.Where(variant => variant != WellKnownSubtags.AudioPrivateUse));
			// The script for ipa is not meaningful, we drop any script here to discourage its use
			var ipaLanguageTag = IetfLanguageTag.Create(_currentWs.Language, null, _currentWs.Region, variants);
			CoreWritingSystemDefinition wsDef;
			if (!_wsManager.TryGet(ipaLanguageTag, out wsDef))
			{
				wsDef = new CoreWritingSystemDefinition(ipaLanguageTag);
				_wsManager.Set(wsDef);
			}
			wsDef.Abbreviation = "ipa";
			IKeyboardDefinition ipaKeyboard = Keyboard.Controller.AvailableKeyboards.FirstOrDefault(k => k.Id.ToLower().Contains("ipa"));
			if (ipaKeyboard != null)
			{
				wsDef.Keyboard = ipaKeyboard.Id;
			}
			// Set language name to be based on current language
			wsDef.Language = new LanguageSubtag(wsDef.Language, _currentWs.LanguageName);
			WorkingList.Insert(CurrentWritingSystemIndex + 1, new WSListItemModel(true, null, wsDef));
			CurrentWsListChanged = true;
			SelectWs(wsDef);
		}

		private void ViewHiddenWritingSystemsHandler(object sender, EventArgs e)
		{
			var model = new ViewHiddenWritingSystemsModel(_listType, Cache,
					WorkingList.Select(li => li.OriginalWs).Where(ws => ws != null).ToList(), _wsIdsToDelete)
				{ ConfirmDeleteWritingSystem = ConfirmDeleteWritingSystem };
			ViewHiddenWritingSystems(model);

			_wsIdsToDelete.AddRange(model.DeletedWritingSystems.Select(ws => ws.Id));
			foreach (var addedWS in model.AddedWritingSystems)
			{
				AddNewLanguage(new LanguageInfo{DesiredName = addedWS.LanguageName, LanguageTag = addedWS.LanguageTag});
			}
		}

		/// <summary/>
		public void EditValidCharacters()
		{
			ShowValidCharsEditor();
		}

		/// <summary/>
		public List<string> GetEncodingConverters()
		{
			var encodingConverters = new List<string> {FwCoreDlgs.kstidNone};
			foreach (string key in EncodingConverterKeys())
			{
				encodingConverters.Add(key);
			}
			return encodingConverters;
		}

		/// <summary/>
		public void ModifyEncodingConverters()
		{
			string selectedConverter;
			var oldConverter = CurrentLegacyConverter;
			if (ShowModifyEncodingConverters(oldConverter, out selectedConverter))
			{
				CurrentLegacyConverter = selectedConverter;
			}
			else
			{
				if (!GetEncodingConverters().Contains(oldConverter))
				{
					CurrentLegacyConverter = null;
				}
			}
		}

		/// <summary/>
		public string CurrentLegacyConverter
		{
			get { return _currentWs?.LegacyMapping; }
			set { _currentWs.LegacyMapping = value; }
		}

		/// <summary>
		/// Any writing system that existed before we started working and that is not the current writing system is
		/// a potential merge target
		/// </summary>
		public IEnumerable<WSListItemModel> MergeTargets
		{
			get
			{
				return WorkingList.Where(item =>
					item.OriginalWs != null && item.WorkingWs != _currentWs);
			}
		}

		/// <summary>
		/// Are we displaying the share with SLDR setting
		/// </summary>
		public bool ShowSharingWithSldr
		{
			get { return _listType == ListType.Vernacular; }
		}

		/// <summary>
		/// Should the vernacular language data be shared with the SLDR
		/// </summary>
		public bool IsSharingWithSldr {
			get
			{
				return _projectLexiconSettings.AddWritingSystemsToSldr;
			}
			set { _projectLexiconSettings.AddWritingSystemsToSldr = value; }
		}

		/// <summary>
		/// Set to true if anything that would update the displayed view of writing systems has changed.
		/// Moving current writing systems up or down. Adding a writing system, removing a writing system,
		/// or changing the selection state of a writing system.
		/// </summary>
		/// <remarks>We are not currently attempting to set back to false if a user undoes some work</remarks>
		public bool CurrentWsListChanged { get; private set; }

		/// <summary/>
		public SpellingDictionaryItem SpellingDictionary
		{
			get
			{
				return string.IsNullOrEmpty(_currentWs?.SpellCheckingId)
					? new SpellingDictionaryItem(null, null)
					: new SpellingDictionaryItem(_currentWs.SpellCheckingId.Replace('_', '-'), _currentWs.SpellCheckingId);
			}
			set
			{
				if (_currentWs != null)
				{
					_currentWs.SpellCheckingId = value?.Id;
				}
			}
		}

		/// <summary/>
		public SpellingDictionaryItem[] GetSpellingDictionaryComboBoxItems()
		{
			// Do not localize this data string
			const string idForNoDictionary = "<None>";
			var dictionaries = new List<SpellingDictionaryItem> { new SpellingDictionaryItem(FwCoreDlgs.ksWsNoDictionaryMatches, idForNoDictionary) };

			string spellCheckingDictionary = _currentWs.SpellCheckingId;
			if (string.IsNullOrEmpty(spellCheckingDictionary))
			{
				dictionaries.Add(new SpellingDictionaryItem(_currentWs.LanguageTag, _currentWs.LanguageTag.Replace('-', '_')));
			}

			bool fDictionaryExistsForLanguage = false;
			bool fAlternateDictionaryExistsForLanguage = false;
			foreach (var languageId in SpellingHelper.GetDictionaryIds().OrderBy(di => GetDictionaryName(di)))
			{
				dictionaries.Add(new SpellingDictionaryItem(GetDictionaryName(languageId), languageId));
			}

			return dictionaries.ToArray();
		}

		private static string GetDictionaryName(string languageId)
		{
			var locale = new Locale(languageId);
			var country = locale.GetDisplayCountry("en");
			var languageName = locale.GetDisplayLanguage("en");
			var languageAndCountry = new StringBuilder(languageName);
			if (!string.IsNullOrEmpty(country))
				languageAndCountry.AppendFormat(" ({0})", country);
			if (languageName != languageId)
				languageAndCountry.AppendFormat(" [{0}]", languageId);
			return languageAndCountry.ToString();
		}
	}

	/// <summary>
	/// This class models a menu item for interacting with the the writing system model.
	/// It holds the string to display in the menu item and the event handler for the menu item click.
	/// </summary>
	public class WSMenuItemModel : Tuple<string, EventHandler, bool, string>
	{
		/// <summary/>
		public WSMenuItemModel(string menuText, EventHandler clickHandler, bool enabled = true, string toolTip = null) : base(menuText, clickHandler, enabled, toolTip)
		{
		}

		/// <summary/>
		public string MenuText => Item1;

		/// <summary/>
		public EventHandler ClickHandler => Item2;

		/// <summary/>
		public bool IsEnabled => Item3;

		/// <summary/>
		public string ToolTip => Item4;
	}

	/// <summary>
	/// This class models a list item for a writing system.
	/// The boolean indicates if the item is in the Current list and should be ticked in the UI.
	/// </summary>
	public class WSListItemModel : Tuple<bool, CoreWritingSystemDefinition, CoreWritingSystemDefinition>
	{
		/// <summary/>
		public WSListItemModel(bool isInCurrent, CoreWritingSystemDefinition originalWsDef, CoreWritingSystemDefinition workingWs) : base(isInCurrent, originalWsDef, workingWs)
		{
		}

		/// <summary/>
		public bool InCurrentList => Item1;

		/// <summary/>
		public CoreWritingSystemDefinition WorkingWs => Item3;

		/// <summary/>
		public CoreWritingSystemDefinition OriginalWs => Item2;

		/// <summary/>
		public override string ToString()
		{
			return WorkingWs.DisplayLabel;
		}
	}

	/// <summary/>
	public class SpellingDictionaryItem : Tuple<string, string>, IEquatable<SpellingDictionaryItem>
	{
		/// <summary/>
		public SpellingDictionaryItem(string item1, string item2) : base(item1, item2)
		{
		}

		/// <summary/>
		public string Name => Item1;

		/// <summary/>
		public string Id => Item2;

		/// <summary/>
		public override string ToString()
		{
			return Name;
		}

		/// <summary/>
		public bool Equals(SpellingDictionaryItem other)
		{
			return Id.Equals(other?.Id);
		}

		/// <summary/>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((SpellingDictionaryItem) obj);
		}

		/// <summary/>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}