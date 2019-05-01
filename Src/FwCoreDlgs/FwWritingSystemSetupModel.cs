// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SilEncConverters40;
using SIL.Code;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Keyboarding;
using SIL.LCModel;
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
		private ListType _listType;
		private readonly IWritingSystemManager _wsManager;
		private string _languageName;
		private WritingSystemSetupModel _currentWsSetupModel;
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

		/// <summary/>
		public delegate bool ChangeLanguageDelegate(out LanguageInfo info);

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
		public delegate bool ChangeHomographWs(string newHomographWs);

		/// <summary/>
		public ChangeLanguageDelegate ShowChangeLanguage;

		/// <summary/>
		public ValidCharacterDelegate ShowValidCharsEditor;

		/// <summary/>
		public ModifyConvertersDelegate ShowModifyEncodingConverters;

		/// <summary/>
		public ConfirmDeleteWritingSystemDelegate ConfirmDeleteWritingSystem;

		/// <summary/>
		public SharedWsChangeDelegate AcceptSharedWsChangeWarning;

		/// <summary/>
		public ChangeHomographWs ShouldChangeHomographWs;

		/// <summary/>
		public ImportTranslatedListDelegate ImportListForNewWs;

		/// <summary/>
		public ConfirmMergeWritingSystemDelegate ConfirmMergeWritingSystem;

		private IWritingSystemContainer _wsContainer;
		private ProjectLexiconSettingsDataMapper _projectLexiconSettingsDataMapper;
		private ProjectLexiconSettings _projectLexiconSettings;
		// We need to know if the homographWs was equal to the top vernacular writing system on construction
		// to be able to show warnings triggered by changing this.
		private bool _homographWsWasTopVern;
		// We need to know if the homographWs was in the current list on construction
		// to be able to show warnings triggered by removing it from the current list
		private bool _homographWsWasInCurrent;

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

			_currentWs = WorkingList.FirstOrDefault().WorkingWs;
			_listType = type;
			_wsManager = wsManager;
			CurrentWsSetupModel = new WritingSystemSetupModel(_currentWs);
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
			foreach (var ws in allForType)
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

		/// <summary>
		/// This indicates if the advanced Script/Region/Variant view should be used
		/// </summary>
		public bool ShowAdvancedScriptRegionVariantView
		{
			get
			{
				return _currentWs.Language.IsPrivateUse ||
					(_currentWs.Script != null && _currentWs.Script.IsPrivateUse) ||
					(_currentWs.Region != null && _currentWs.Region.IsPrivateUse);
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
			return WorkingList.Count > 1;
		}

		/// <summary/>
		public bool CanDelete()
		{
			return WorkingList.Count > 1;
		}

		/// <summary/>
		public void SelectWs(string wsTag)
		{
			var oldWs = _currentWs;
			_currentWs = WorkingList.First(ws => ws.WorkingWs.LanguageTag == wsTag).WorkingWs;
			// didn't change, no-op
			if (oldWs.LanguageTag == _currentWs.LanguageTag)
				return;
			CurrentWsSetupModel = new WritingSystemSetupModel(_currentWs);
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
		public void ChangeCurrentStatus()
		{
			var currentItem = WorkingList.Find(item => item.WorkingWs.LanguageTag == _currentWs.LanguageTag);
			var currentIndex = WorkingList.IndexOf(currentItem);
			WorkingList.Remove(currentItem);
			WorkingList.Insert(currentIndex, new WSListItemModel(!currentItem.InCurrentList, currentItem.OriginalWs, currentItem.WorkingWs));
		}

		/// <summary/>
		public bool IsListValid
		{
			get
			{
				var langTagSet = new HashSet<string>();
				foreach (var ws in WorkingList)
				{
					if (langTagSet.Contains(ws.WorkingWs.LanguageTag))
						return false;
					langTagSet.Add(ws.WorkingWs.LanguageTag);
				}
				return WorkingList.Any(item => item.InCurrentList);
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
					foreach (var relatedWs in WorkingList.Where(ws => ws.WorkingWs.LanguageName == _languageName))
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
		public string EthnologueLabel
		{
			get { return string.Format("Ethnologue entry for {0}", LanguageCode); }
		}

		/// <summary/>
		public string EthnologueLink
		{
			get { return string.Format("https://www.ethnologue.com/show_language.asp?code={0}", LanguageCode); }
		}

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
			LanguageInfo info;
			if (ShowChangeLanguage(out info))
			{
				if (WorkingList.Exists(ws => ws.WorkingWs.LanguageTag == info.LanguageTag))
				{
					// RejectDuplicateLanguage();
					return;
				}
				var languagesToChange = new List<WSListItemModel>(WorkingList.Where(ws => ws.WorkingWs.LanguageName == _languageName));
				LanguageSubtag languageSubtag;
				ScriptSubtag scriptSubtag;
				RegionSubtag regionSubtag;
				IEnumerable<VariantSubtag> variantSubtags;
				if (!IetfLanguageTag.TryGetSubtags(info.LanguageTag, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtags))
					return;
				languageSubtag = new LanguageSubtag(languageSubtag, info.DesiredName);

				if (!CheckChangingWSForSRProject(languageSubtag))
					return;
				foreach (var ws in languagesToChange)
				{
					ws.WorkingWs.Language = languageSubtag;
					if (ws.WorkingWs.Script == null)
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
		/// <param name="newLangTag">The language tag of the original WritingSystem.</param>
		/// <returns></returns>
		private bool CheckChangingWSForSRProject(LanguageSubtag newLangTag)
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
				var atLeastOneChange = HandleHomographWsChanges(_homographWsWasTopVern, WorkingList, Cache?.LangProject.HomographWs, _homographWsWasInCurrent);

				// Handle any deleted writing systems
				var deletedSomeWritingSystems = DeleteWritingSystems(currentWritingSystems, allWritingSystems, otherWritingSystems, WorkingList.Select(ws => ws.WorkingWs));
				atLeastOneChange = atLeastOneChange || deletedSomeWritingSystems;

				for (int workinglistIndex = 0, curIndex = 0; workinglistIndex < WorkingList.Count; ++workinglistIndex)
				{
					var wsListItem = WorkingList[workinglistIndex];
					var workingWs = wsListItem.WorkingWs;
					var origWS = wsListItem.OriginalWs;

					if (IsNew(wsListItem))
					{
						// Create the new writing system overwriting any existing ws of the same id
						_wsManager.Replace(workingWs);
						newWritingSystems.Add(workingWs);
						atLeastOneChange = true;
						AddOrMoveInList(allWritingSystems, workinglistIndex, workingWs);
						if (wsListItem.InCurrentList)
						{
							AddOrMoveInList(currentWritingSystems, curIndex, workingWs);
							++curIndex;
						}
					}
					else if (workingWs.IsChanged)
					{
						var oldId = origWS.Id;
						var oldHandle = origWS.Handle;
						// copy the working writing system content into the original writing system
						origWS.Copy(workingWs);
						AddOrMoveInList(allWritingSystems, workinglistIndex, origWS);
						if (wsListItem.InCurrentList)
						{
							AddOrMoveInList(currentWritingSystems, curIndex, origWS);
							++curIndex;
						}
						else
						{
							SafelyRemoveFromList(currentWritingSystems, wsListItem);
						}
						if (string.IsNullOrEmpty(oldId) || !IetfLanguageTag.AreTagsEquivalent(oldId, workingWs.LanguageTag))
						{
							// update the ID
							_wsManager.Replace(origWS);
							if (uowHelper != null)
							{
								WritingSystemServices.UpdateWritingSystemId(Cache, origWS, oldHandle, oldId);
							}
						}
						atLeastOneChange = true;
						_mediator?.SendMessage("WritingSystemUpdated", origWS.Id);
					}
					else // Writing system is not new or changed, but we might have added or removed from the current list
					{
						if (wsListItem.InCurrentList && !currentWritingSystems.Contains(origWS))
						{
							AddOrMoveInList(currentWritingSystems, curIndex, origWS);
							++curIndex;
							atLeastOneChange = true;
						}
						else if(!wsListItem.InCurrentList)
						{
							atLeastOneChange = SafelyRemoveFromList(currentWritingSystems, wsListItem);
						}
					}
				}
				// Handle any merged writing systems
				foreach (var mergedWs in _mergedWritingSystems)
				{
					atLeastOneChange = true;
					WritingSystemServices.MergeWritingSystems(Cache, mergedWs.Key, mergedWs.Value);
				}
				atLeastOneChange = atLeastOneChange || deletedSomeWritingSystems;
				// Save all the changes to the current writing systems
				_wsManager.Save();
				foreach (var newWs in newWritingSystems)
				{
					ImportListForNewWs(newWs.IcuLocale);
				}

				_projectLexiconSettingsDataMapper?.Write(_projectLexiconSettings);
				if (atLeastOneChange && uowHelper != null)
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

		private void AddOrMoveInList(ICollection<CoreWritingSystemDefinition> allWritingSystems, int desiredIndex, CoreWritingSystemDefinition workingWs)
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
		private bool SafelyRemoveFromList(IList<CoreWritingSystemDefinition> currentWritingSystems, WSListItemModel wsListItem)
		{
			if (wsListItem.OriginalWs != null)
			{
				return currentWritingSystems.Remove(wsListItem.OriginalWs);
			}

			return false;
		}

		private bool HandleHomographWsChanges(bool homographWsWasTopVern, List<WSListItemModel> workingList, string homographWs, bool wasSelected)
		{
			if (_listType != ListType.Vernacular || Cache == null)
				return false;
			// If the homograph writing system has been removed then change to the top current vernacular with no user interaction
			if (workingList.All(ws => ws.OriginalWs?.Id != homographWs))
			{
				Cache.LangProject.HomographWs = workingList.First(ws => ws.InCurrentList).WorkingWs.Id;
				return true;
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
				return true;
			}

			return false;
		}

		private bool DeleteWritingSystems(
			ICollection<CoreWritingSystemDefinition> currentWritingSystems,
			ICollection<CoreWritingSystemDefinition> allWritingSystems,
			ICollection<CoreWritingSystemDefinition> otherWritingSystems,
			IEnumerable<CoreWritingSystemDefinition> workingWritingSystems)
		{
			var atLeastOneDeleted = false;
			// Delete any writing systems that were removed from the active list and are not present in the other list
			var deletedWsIds = new List<string>();
			var deletedWritingSystems = new List<CoreWritingSystemDefinition>(allWritingSystems);
			deletedWritingSystems.RemoveAll(ws => workingWritingSystems.Any(wws => wws.Id == ws.Id));
			foreach (var deleteCandidate in deletedWritingSystems)
			{
				currentWritingSystems.Remove(deleteCandidate);
				allWritingSystems.Remove(deleteCandidate);
				// The cache will be null while creating a new project, in which case we aren't really deleting anything
				if (!otherWritingSystems.Contains(deleteCandidate) && Cache != null)
				{
					WritingSystemServices.DeleteWritingSystem(Cache, deleteCandidate);
					deletedWsIds.Add(deleteCandidate.Id);
					atLeastOneDeleted = true;
				}
			}

			if (deletedWsIds.Count > 0)
			{
				_mediator?.SendMessage("WritingSystemDeleted", deletedWsIds.ToArray());
			}

			return atLeastOneDeleted;
		}

		private bool IsNew(WSListItemModel tempWs)
		{
			return tempWs.OriginalWs == null;
		}

		/// <summary/>
		public List<WSMenuItemModel> GetAddMenuItems()
		{
			var addIpaInputSystem = FwCoreDlgs.WritingSystemList_AddIpa;
			var addAudioInputSystem = FwCoreDlgs.WritingSystemList_AddAudio;
			var addDialect = FwCoreDlgs.WritingSystemList_AddDialect;
			var addNewLanguage = "Add new language...";
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
			return menuItemList;
		}

		/// <summary/>
		public List<WSMenuItemModel> GetRightClickMenuItems()
		{
			var deleteWritingSystem = FwCoreDlgs.WritingSystemList_DeleteWs;
			var mergeWritingSystem = FwCoreDlgs.WritingSystemList_MergeWs;
			var menuItemList = new List<WSMenuItemModel>();
			if (CanMerge())
			{
				menuItemList.Add(new WSMenuItemModel(mergeWritingSystem, MergeWritingSystem));
			}
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
					_mergedWritingSystems[_currentWs] = mergeWithWsId;
				}
				WorkingList.RemoveAt(CurrentWritingSystemIndex);
				CurrentWsListChanged = true;
				SelectWs(WorkingList.First().WorkingWs.LanguageTag);
			}
		}

		private void DeleteCurrentWritingSystem(object sender, EventArgs e)
		{
			// If the writing system is in the other list as well, simply hide it silently.
			var otherList = _listType == ListType.Vernacular ? _wsContainer.AnalysisWritingSystems : _wsContainer.VernacularWritingSystems;
			if (WorkingList[CurrentWritingSystemIndex].InCurrentList)
			{
				CurrentWsListChanged = true;
			}
			if (otherList.Contains(_currentWs) || WorkingList[CurrentWritingSystemIndex].OriginalWs == null)
			{
				WorkingList.RemoveAt(CurrentWritingSystemIndex);
				SelectWs(WorkingList.First().WorkingWs.LanguageTag);
				return;
			}

			if (ConfirmDeleteWritingSystem(CurrentWsSetupModel.CurrentDisplayLabel))
			{
				WorkingList.RemoveAt(CurrentWritingSystemIndex);
				SelectWs(WorkingList.First().WorkingWs.LanguageTag);
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
			var ipaMatch = $"^{languageCode}.*-fonipa.*";
			return WorkingList.Exists(item => Regex.IsMatch(item.WorkingWs.LanguageTag, ipaMatch));
		}

		private void AddNewLanguageHandler(object sender, EventArgs e)
		{
			LanguageInfo langInfo;
			if (ShowChangeLanguage(out langInfo))
			{
				CoreWritingSystemDefinition wsDef = null;
				if(!_wsManager.TryGet(langInfo.LanguageTag, out wsDef))
				{
					wsDef = _wsManager.Set(langInfo.LanguageTag);
				}
				wsDef.Language = new LanguageSubtag(wsDef.Language, langInfo.DesiredName);
				WorkingList.Insert(CurrentWritingSystemIndex + 1, new WSListItemModel(true, null, wsDef));
				CurrentWsListChanged = true;
				SelectWs(wsDef.LanguageTag);
			}
		}

		private void AddDialectHandler(object sender, EventArgs e)
		{
			var wsDef = new CoreWritingSystemDefinition(_currentWs);
			WorkingList.Insert(CurrentWritingSystemIndex + 1, new WSListItemModel(true, null, wsDef));
			CurrentWsListChanged = true;
			// Set language name to be based on current language
			wsDef.Language = new LanguageSubtag(wsDef.Language, _currentWs.LanguageName);
			// Can't use SelectWs because initially the new dialect is identical
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
			SelectWs(wsDef.LanguageTag);
		}

		private void AddIpaHandler(object sender, EventArgs e)
		{
			var variants = new List<VariantSubtag> { WellKnownSubtags.IpaVariant };
			variants.AddRange(_currentWs.Variants.Where(variant => variant != WellKnownSubtags.AudioPrivateUse));
			var cleanScript = _currentWs.Script == null ||_currentWs.Script.Code == WellKnownSubtags.AudioScript ? null : _currentWs.Script;
			var ipaLanguageTag = IetfLanguageTag.Create(_currentWs.Language, cleanScript, _currentWs.Region, variants);
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
			SelectWs(wsDef.LanguageTag);
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
	}

	/// <summary>
	/// This class models a menu item for interacting with the the writing system model.
	/// It holds the string to display in the menu item and the event handler for the menu item click.
	/// </summary>
	public class WSMenuItemModel : Tuple<string, EventHandler, bool>
	{
		/// <summary/>
		public WSMenuItemModel(string menuText, EventHandler clickHandler, bool enabled = true) : base(menuText, clickHandler, enabled)
		{
		}

		/// <summary/>
		public string MenuText => Item1;

		/// <summary/>
		public EventHandler ClickHandler => Item2;

		/// <summary/>
		public bool IsEnabled => Item3;
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
}