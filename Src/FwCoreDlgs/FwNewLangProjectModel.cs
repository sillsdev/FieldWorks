// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Presentation model for the new language project wizard
	/// </summary>
	internal sealed class FwNewLangProjectModel
	{
		/// <summary/>
		internal delegate void LoadProjectNameSetupDelegate();

		/// <summary/>
		internal LoadProjectNameSetupDelegate LoadProjectNameSetup;

		/// <summary/>
		internal delegate void LoadAnalysisSetupDelegate();

		/// <summary/>
		internal LoadAnalysisSetupDelegate LoadAnalysisSetup;

		/// <summary/>
		internal delegate void LoadAnalysisSameAsVernacularDelegate();

		/// <summary/>
		internal LoadAnalysisSameAsVernacularDelegate LoadAnalysisSameAsVernacularWarning;

		/// <summary/>
		internal delegate void LoadVernacularSetupDelegate();

		/// <summary/>
		internal LoadVernacularSetupDelegate LoadVernacularSetup;

		/// <summary/>
		internal delegate void LoadAnthropologySetupDelegate();

		/// <summary/>
		internal LoadAnthropologySetupDelegate LoadAnthropologySetup;

		/// <summary/>
		internal delegate void LoadAdvancedWsSetupDelegate();

		/// <summary/>
		internal LoadAdvancedWsSetupDelegate LoadAdvancedWsSetup;

		private enum NewProjStep
		{
			ProjectName = 0,
			Vernacular = 1,
			Analysis = 2,
			ExtraWsConfig = 3,
			Anthropology = 4
		}

		/// <summary/>
		private NewProjStep CurrentStep;

		private IWizardStep[] _steps = {
			new NewLangProjStep(FwCoreDlgs.NewProjectWizard_Name_Step, false, true),
			new NewLangProjStep(FwCoreDlgs.NewProjectWizard_Vernacular_Step, false),
			new NewLangProjStep(FwCoreDlgs.NewProjectWizard_Analysis_Step, false),
			new NewLangProjStep(FwCoreDlgs.NewProjectWizard_More_Step, true),
			new NewLangProjStep(FwCoreDlgs.NewProjectWizard_Anthro_Step, true)
		};

		private string _projectName;
		private IList<CoreWritingSystemDefinition> _allAnalysis = new List<CoreWritingSystemDefinition>();
		private IList<CoreWritingSystemDefinition> _allVern = new List<CoreWritingSystemDefinition>();
		private IList<CoreWritingSystemDefinition> _curAnalysis = new List<CoreWritingSystemDefinition>();
		private IList<CoreWritingSystemDefinition> _curVernWss = new List<CoreWritingSystemDefinition>();
		private IList<CoreWritingSystemDefinition> _curPron = new List<CoreWritingSystemDefinition>();

		/// <summary/>
		internal IEnumerable<IWizardStep> Steps => _steps;

		/// <summary/>
		internal IWritingSystemContainer WritingSystemContainer { get; set; }

		/// <summary/>
		internal WritingSystemManager WritingSystemManager { get; }

		/// <summary/>
		internal FwNewLangProjectModel(bool useMemoryWsManager = false)
		{
			WritingSystemManager = useMemoryWsManager ? new WritingSystemManager() : new WritingSystemManager(SingletonsContainer.Get<CoreGlobalWritingSystemRepository>());
			WritingSystemManager.GetOrSet("en", out var englishWs);
			_allAnalysis.Add(englishWs);
			_curAnalysis.Add(englishWs);
			WritingSystemContainer = new MemoryWritingSystemContainer(_allAnalysis, _allVern, _curAnalysis, _curVernWss, _curPron);
		}

		/// <summary/>
		internal void Next()
		{
			if (CurrentStepIsComplete())
			{
				var step = _steps[(int) CurrentStep];
				step.IsComplete = true;
				step.IsCurrent = false;
				LoadNext();
			}
		}

		private void LoadNext()
		{
			++CurrentStep;
			_steps[(int) CurrentStep].IsCurrent = true;
			switch (CurrentStep)
			{
				case NewProjStep.Vernacular:
					LoadVernacularSetup();
					break;
				case NewProjStep.Analysis:
					SetCompleteOnEnter();
					LoadAnalysisSetup();
					break;
				case NewProjStep.ExtraWsConfig:
					SetCompleteOnEnter();
					LoadAdvancedWsSetup();
					break;
				case NewProjStep.Anthropology:
					SetCompleteOnEnter();
					LoadAnthropologySetup();
					break;
				default:
					throw new IndexOutOfRangeException("Bad current step advance logic: " + CurrentStep);
			}
		}

		/// <summary>
		/// Use to mark a wizard step as complete when no changes are necessarily required
		/// </summary>
		private void SetCompleteOnEnter()
		{
			if (CurrentStepIsComplete())
			{
				_steps[(int)CurrentStep].IsComplete = true;
			}
		}

		private bool CurrentStepIsComplete()
		{
			switch (CurrentStep)
			{
				case NewProjStep.ProjectName:
					return IsProjectNameValid;
				case NewProjStep.Vernacular:
					return WritingSystemContainer.CurrentVernacularWritingSystems.Count > 0;
				case NewProjStep.Analysis:
					return WritingSystemContainer.CurrentAnalysisWritingSystems.Count > 0;
				case NewProjStep.ExtraWsConfig:
					return true;
				case NewProjStep.Anthropology:
					return true;
				default:
					throw new IndexOutOfRangeException("Bad current step advance logic: " + CurrentStep);
			}
		}

		/// <summary/>
		internal void Back()
		{
			_steps[(int)CurrentStep].IsCurrent = false;
			--CurrentStep;
			_steps[(int)CurrentStep].IsCurrent = true;
			switch (CurrentStep)
			{
				case NewProjStep.ProjectName:
					LoadProjectNameSetup();
					break;
				case NewProjStep.Vernacular:
					LoadVernacularSetup();
					break;
				case NewProjStep.Analysis:
					LoadAnalysisSetup();
					break;
				case NewProjStep.ExtraWsConfig:
					LoadAdvancedWsSetup();
					break;
				default:
					throw new InvalidEnumArgumentException("Unknown CurrentStep: " + CurrentStep);
			}
		}

		/// <summary/>
		internal string ProjectName
		{
			get => _projectName;
			set
			{
				_projectName = value;
				_steps[(int)CurrentStep].IsComplete = IsProjectNameValid;
				LoadProjectNameSetup();
			}
		}

		/// <summary/>
		internal bool IsProjectNameValid
		{
			get
			{
				var projName = ProjectName;
				return !string.IsNullOrEmpty(ProjectName?.Trim()) && CheckForSafeProjectName(ref projName, out _) && CheckForUniqueProjectName(projName);
			}

		}

		/// <summary/>
		internal string InvalidProjectNameMessage
		{
			get
			{
				if (string.IsNullOrEmpty(ProjectName))
				{
					return FwCoreDlgs.NewProjectWizard_EnterProjectName;
				}

				var projName = ProjectName;
				return !CheckForSafeProjectName(ref projName, out var errorMessage)
					? errorMessage : !CheckForUniqueProjectName(projName)
						? string.Format(FwCoreDlgs.NewProjectWizard_DuplicateProjectName, projName) : string.Empty;
			}
		}

		/// <summary/>
		internal FwChooseAnthroListModel AnthroModel = new FwChooseAnthroListModel();

		/// <summary/>
		internal string CreateNewLangProj(IThreadedProgress progressDialog, ISynchronizeInvoke threadHelper)
		{
			try
			{
				var defaultAnalysis = WritingSystemContainer.CurrentAnalysisWritingSystems.First();
				var defaultVernacular = WritingSystemContainer.CurrentVernacularWritingSystems.First();
				return LcmCache.CreateNewLangProj(progressDialog, ProjectName, FwDirectoryFinder.LcmDirectories,
					threadHelper, defaultAnalysis, defaultVernacular, "en",// TODO: replicate original
					new HashSet<CoreWritingSystemDefinition>(WritingSystemContainer.AnalysisWritingSystems.Skip(1)),
					new HashSet<CoreWritingSystemDefinition>(WritingSystemContainer.VernacularWritingSystems.Skip(1)),
					AnthroModel.AnthroFileName);
			}
			catch (WorkerThreadException wex)
			{
				var e = wex.InnerException;
				switch (e)
				{
<<<<<<< HEAD
					case UnauthorizedAccessException _ when Platform.IsUnix:
||||||| f013144d5
					if (MiscUtils.IsUnix)
					{
=======
					if (Platform.IsUnix)
					{
>>>>>>> develop
						// Tell Mono user he/she needs to logout and log back in
						MessageBoxUtils.Show(ResourceHelper.GetResourceString("ksNeedToJoinFwGroup"));
						break;
					case UnauthorizedAccessException _:
					case ApplicationException _:
					case LcmInitializationException _:
						MessageBoxUtils.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message), FwUtilsConstants.ksSuiteName);
						break;
					default:
						throw new Exception(FwCoreDlgs.kstidErrApp, e);
				}
			}

			return null;
		}

		/// <param name="projectName">The Project Name reference will be modified to remove illegal characters</param>
		/// <param name="errorMessage"></param>
		/// <returns><c>true</c> if the given project name is valid; otherwise, <c>false</c></returns>
		internal static bool CheckForSafeProjectName(ref string projectName, out string errorMessage)
		{
			errorMessage = null;
			// Don't allow illegal characters. () and [] have significance.
			// [] are typically used as delimiters for file names in SQL queries. () are used in older
			// backup file names and as such, they can cause grief when trying to restore. Old example:
			// Jim's (old) backup (Jim_s (old) backup) ....zip. The file name was Jim_s (old) backup.mdf.
			var sIllegalChars = MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterProjName);
			var firstIllegalChar = (char)0;
			var sbProjectName = new StringBuilder(projectName);
			for (var i = sbProjectName.Length - 1; i >= 0; i--)
			{
				if (sIllegalChars.Contains(sbProjectName[i]) || sbProjectName[i] > 126) // all non-ASCII characters are forbidden
				{
					firstIllegalChar = sbProjectName[i];
					sbProjectName.Remove(i, 1);
				}
			}
			projectName = sbProjectName.ToString();
			if (firstIllegalChar > 0)
			{
				var sbErrorMessage = new StringBuilder();
				if (firstIllegalChar < 127)
				{
					// likely an illegal symbol (e.g. /:\)
					// Prepare to show the message:
					// Remove characters that cannot be keyboarded (below code point 32). The user doesn't
					// need to be warned about these since they can't be entered via the keyboard.
					var sIllegalCharsForMessage = sIllegalChars;
					for (var n = 0; n < 32; n++)
					{
						var index = sIllegalCharsForMessage.IndexOf((char)n);
						if (index >= 0)
						{
							sIllegalCharsForMessage = sIllegalCharsForMessage.Remove(index, 1);
						}
					}
					sbErrorMessage.AppendFormat(FwUtilsStrings.ksIllegalNameMsg, sIllegalCharsForMessage);
				}
				else if (firstIllegalChar >= '\u00C0' && firstIllegalChar < '\u02B0')
				{
					// likely a Latin character with diacritics
					// (somewhere between u0190 and u02B0, "letters with diacritics" become interspersed with extended Latin letters)
					sbErrorMessage.AppendFormat(FwCoreDlgs.ksIllegalNameWithDiacriticsMsg, firstIllegalChar);
				}
				else
				{
					// Unicode (which could be diacritics, non-Latin characters, spacing markers, emoji, or many other things)
					sbErrorMessage.Append(FwCoreDlgs.ksIllegalNameNonRomanMsg);
				}
				sbErrorMessage.AppendLine().Append(FwUtilsStrings.ksIllegalNameExplanation);
				errorMessage = sbErrorMessage.ToString();
				return false;
			}

			return true;
		}

		/// <summary/>
		internal static bool CheckForUniqueProjectName(string projectName)
		{
			return ProjectInfo.GetProjectInfoByName(FwDirectoryFinder.ProjectsDirectory, projectName) == null;
		}

		/// <summary/>
		internal bool CanGoBack()
		{
			return CurrentStep > 0;
		}

		/// <summary/>
		internal bool CanGoNext()
		{
			return CurrentStepIsComplete() && (int)CurrentStep < _steps.Length - 1;
		}

		/// <summary/>
		internal bool CanFinish()
		{
			return _steps.All(step => step.IsOptional || step.IsComplete);
		}

		/// <summary/>
		internal void SetDefaultWs(LanguageInfo selectedLanguage)
		{
			ICollection<CoreWritingSystemDefinition> allList;
			IList<CoreWritingSystemDefinition> currentList;
			switch (CurrentStep)
			{
				case NewProjStep.Vernacular:
				{
					currentList = WritingSystemContainer.CurrentVernacularWritingSystems;
					allList = WritingSystemContainer.VernacularWritingSystems;
					SetDefaultWsInLists(selectedLanguage, currentList, allList);
					LoadVernacularSetup();
					break;
				}
				case NewProjStep.Analysis:
				{
					currentList = WritingSystemContainer.CurrentAnalysisWritingSystems;
					allList = WritingSystemContainer.AnalysisWritingSystems;
					SetDefaultWsInLists(selectedLanguage, currentList, allList);
					if (currentList.First().Equals(WritingSystemContainer.CurrentVernacularWritingSystems.First()))
					{
						LoadAnalysisSameAsVernacularWarning();
					}
					LoadAnalysisSetup();
					break;
				}
				default:
					throw new ApplicationException("Not on the right step to set a default ws");
			}
		}

		private void SetDefaultWsInLists(LanguageInfo selectedLanguage, IList<CoreWritingSystemDefinition> currentList, ICollection<CoreWritingSystemDefinition> allList)
		{
			SetDefaultWsInList(selectedLanguage, currentList);

			var newAllList = new List<CoreWritingSystemDefinition>(allList);
			SetDefaultWsInList(selectedLanguage, newAllList);
			allList.Clear();
			allList.AddRange(newAllList);
		}

		private void SetDefaultWsInList(LanguageInfo selectedLanguage, IList<CoreWritingSystemDefinition> list)
		{
			var itemInList = list.FirstOrDefault(ws => ws.LanguageTag == selectedLanguage.LanguageTag);
			if (itemInList == null)
			{
				WritingSystemManager.GetOrSet(selectedLanguage.LanguageTag, out itemInList);
				if (list.Any())
				{
					// If the user changes the Default in either Default WS step in the New Project Wizard,
					// don't keep an ever-growing list of old WS's (LT-19718)
					list.RemoveAt(0);
				}
			}
			else
			{
				// WS exists; simply promote it.
				list.Remove(itemInList);
			}
			list.Insert(0, itemInList);
		}


		/// <summary/>
		private sealed class NewLangProjStep : IWizardStep
		{
			/// <summary/>
			internal NewLangProjStep(string name, bool optional, bool isCurrent = false)
			{
				StepName = name;
				IsOptional = optional;
				IsCurrent = isCurrent;
			}

			/// <summary/>
			public string StepName { get; set; }
			/// <summary/>
			public bool IsOptional { get; set; }
			/// <summary/>
			public bool IsCurrent { get; set; }
			/// <summary/>
			public bool IsComplete { get; set; }
		}
	}
}