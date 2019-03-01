using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SilEncConverters40;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
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
		public ImportTranslatedListDelegate ImportListForNewWs;

		/// <summary/>
		public ConfirmMergeWritingSystemDelegate ConfirmMergeWritingSystem;

		private IWritingSystemContainer _wsContainer;
		private ProjectLexiconSettingsDataMapper _projectLexiconSettingsDataMapper;
		private ProjectLexiconSettings _projectLexiconSettings;

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
			get => _currentWsSetupModel;

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
			get { return _currentWs.Language.IsPrivateUse || _currentWs.Script.IsPrivateUse || _currentWs.Region.IsPrivateUse; }
		}

		/// <summary>
		/// This indicates if the Graphite Font options should be configurable
		/// </summary>
		public bool EnableGraphiteFontOptions => _currentWs?.DefaultFont != null && _currentWs.DefaultFont.Engines.HasFlag(FontEngines.Graphite);

		/// <summary/>
		public bool TryGetFont(string text, out FontDefinition font)
		{
			return _currentWs.Fonts.TryGet(text, out font);
		}

		/// <summary/>
		public bool CanMoveUp() => WorkingList.Count > 1 && WorkingList.First().WorkingWs != _currentWs;

		/// <summary/>
		public bool CanMoveDown() => WorkingList.Count > 1 && WorkingList.Last().WorkingWs != _currentWs;

		/// <summary/>
		public bool CanMerge() => WorkingList.Count > 1;

		/// <summary/>
		public bool CanDelete() => WorkingList.Count > 1;

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
		}

		/// <summary/>
		public void MoveUp()
		{
			var currentItem = WorkingList.Find(ws => ws.WorkingWs == _currentWs);
			var currentIndex = WorkingList.IndexOf(currentItem);
			Guard.Against(currentIndex >= WorkingList.Count, "Programming error: Invalid state for MoveUp");

			WorkingList.Remove(currentItem);
			WorkingList.Insert(currentIndex - 1, currentItem);
		}

		/// <summary/>
		public void MoveDown()
		{
			var currentItem = WorkingList.Find(ws => ws.WorkingWs == _currentWs);
			var currentIndex = WorkingList.IndexOf(currentItem);
			Guard.Against(currentIndex >= WorkingList.Count, "Programming error: Invalid state for MoveUp");

			WorkingList.Remove(currentItem);
			WorkingList.Insert(currentIndex + 1, currentItem);
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
			get => _currentWs?.Language.Iso3Code;
		}

		/// <summary>
		/// The language name corresponding to just the language part of the tag e.g. French for fr-fonipa
		/// </summary>
		public string LanguageName
		{
			get => _languageName;
			set
			{
				if (!string.IsNullOrEmpty(value) && value != _languageName)
				{
					foreach (var relatedWs in WorkingList.Where(ws => ws.WorkingWs.LanguageName == _languageName))
					{
						relatedWs.WorkingWs.Language = new LanguageSubtag(relatedWs.WorkingWs.Language.Code, value);
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
			get => _currentWs.DisplayLabel;
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
			get => _currentWs.IsGraphiteEnabled;
			set => _currentWs.IsGraphiteEnabled = value;
		}

		/// <summary/>
		public string CurrentDefaultFontFeatures
		{
			get => _currentWs.DefaultFontFeatures;
		}

		/// <summary/>
		public FontDefinition CurrentDefaultFont
		{
			get => _currentWs.DefaultFont;
			set => _currentWs.DefaultFont = value;
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
			// but we still need to update the WorkingWs manager, so we have to execute the save even if m_cache is null
			NonUndoableUnitOfWorkHelper uowHelper = null;
			if (Cache != null)
				uowHelper = new NonUndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor);
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
				// All writing systems that are no longer present may be deleted
				var deletedWritingSystems = new List<CoreWritingSystemDefinition>(allWritingSystems);
				// Track the new writing systems for importing translated lists
				var newWritingSystems = new List<CoreWritingSystemDefinition>();
				currentWritingSystems.Clear();
				allWritingSystems.Clear();
				var atLeastOneChange = false;
				foreach (var wsListItem in WorkingList)
				{
					if (wsListItem.OriginalWs != null)
					{
						deletedWritingSystems.Remove(wsListItem.OriginalWs);
					}
					var workingWs = wsListItem.WorkingWs;
					var origWS = wsListItem.OriginalWs;

					if (IsNew(wsListItem))
					{
						// Create the new writing system overwriting any existing ws of the same id
						_wsManager.Replace(workingWs);
						newWritingSystems.Add(workingWs);
						atLeastOneChange = true;
					}
					else if (workingWs.IsChanged)
					{
						var oldId = origWS.Id;
						var oldHandle = origWS.Handle;
						// copy the working writing system content into the original writing system
						origWS.Copy(workingWs);
						if (oldId != workingWs.LanguageTag)
						{
							// update the ID
							_wsManager.Set(origWS);
							if (uowHelper != null)
								WritingSystemServices.UpdateWritingSystemId(Cache, origWS, oldHandle, oldId);
						}
						atLeastOneChange = true;
						_mediator?.SendMessage("WritingSystemUpdated", origWS.Id);
					}
					allWritingSystems.Add(workingWs);
					if (wsListItem.InCurrentList)
					{
						currentWritingSystems.Add(workingWs);
					}
				}
				_wsManager.Save();
				foreach (var mergedWs in _mergedWritingSystems)
				{
					WritingSystemServices.MergeWritingSystems(Cache, mergedWs.Key, mergedWs.Value);
				}
				// Delete any writing systems that were removed from the active list and are not present in the other list
				var deletedWsIds = new List<string>();
				foreach (var deleteCandidate in deletedWritingSystems)
				{
					if (!otherWritingSystems.Contains(deleteCandidate))
					{
						WritingSystemServices.DeleteWritingSystem(Cache, deleteCandidate);
						deletedWsIds.Add(deleteCandidate.Id);
						atLeastOneChange = true;
					}
				}
				if (deletedWsIds.Count > 0)
				{
					_mediator?.SendMessage("WritingSystemDeleted", deletedWsIds.ToArray());
				}
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
				if (uowHelper != null)
					uowHelper.Dispose();
			}
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
				_mergedWritingSystems[_currentWs] = mergeWithWsId;
				WorkingList.RemoveAt(CurrentWritingSystemIndex);
				SelectWs(WorkingList.First().WorkingWs.LanguageTag);
			}
		}

		private void DeleteCurrentWritingSystem(object sender, EventArgs e)
		{

			// If the writing system is in the other list as well, simply hide it silently.
			var otherList = _listType == ListType.Vernacular ? _wsContainer.AnalysisWritingSystems : _wsContainer.VernacularWritingSystems;
			if (otherList.Contains(_currentWs))
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
				Cache?.ServiceLocator.WritingSystemManager.GetOrSet(langInfo.LanguageTag, out wsDef);
				wsDef.Language = new LanguageSubtag(wsDef.Language, langInfo.DesiredName);
				WorkingList.Add(new WSListItemModel(true, null, wsDef));
				SelectWs(wsDef.LanguageTag);
			}
		}

		private void AddDialectHandler(object sender, EventArgs e)
		{
			var wsDef = new CoreWritingSystemDefinition(_currentWs);
			WorkingList.Add(new WSListItemModel(true, null, wsDef));
			CurrentWsSetupModel = new WritingSystemSetupModel(wsDef, WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant);
			_currentWs = wsDef;
			OnCurrentWritingSystemChanged(this, EventArgs.Empty);
		}

		private void AddAudioHandler(object sender, EventArgs e)
		{
			var wsDef = new CoreWritingSystemDefinition(_currentWs) {IsVoice = true};
			WorkingList.Add(new WSListItemModel(true, null, wsDef));
			SelectWs(wsDef.LanguageTag);
		}

		private void AddIpaHandler(object sender, EventArgs e)
		{
			var variants = new List<VariantSubtag> { WellKnownSubtags.IpaVariant };
			variants.AddRange(_currentWs.Variants);
			var ipaLanguageTag = IetfLanguageTag.Create(_currentWs.Language, _currentWs.Script, _currentWs.Region, variants);
			CoreWritingSystemDefinition wsDef = null;
			Cache?.ServiceLocator.WritingSystemManager.GetOrSet(ipaLanguageTag, out wsDef);
			wsDef.Abbreviation = "ipa";
			IKeyboardDefinition ipaKeyboard = Keyboard.Controller.AvailableKeyboards.FirstOrDefault(k => k.Id.ToLower().Contains("ipa"));
			if (ipaKeyboard != null)
			{
				wsDef.Keyboard = ipaKeyboard.Id;
			}

			WorkingList.Add(new WSListItemModel(true, null, wsDef));
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
			get => _currentWs?.LegacyMapping;
			set => _currentWs.LegacyMapping = value;
		}

		/// <summary>
		/// Any writing system that existed before we started working and that is not the current writing system is
		/// a potential merge target
		/// </summary>
		public IEnumerable<WSListItemModel> MergeTargets
		{
			get => WorkingList.Where(item => item.OriginalWs != null && item.WorkingWs != _currentWs);
		}

		/// <summary>
		/// Are we displaying the share with SLDR setting
		/// </summary>
		public bool ShowSharingWithSldr => _listType == ListType.Vernacular;

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