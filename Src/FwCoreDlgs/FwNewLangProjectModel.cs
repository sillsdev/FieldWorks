// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Presentation model for the new language project wizard
	/// </summary>
	public class FwNewLangProjectModel
	{
		/// <summary/>
		public delegate void LoadProjectNameSetupDelegate();

		/// <summary/>
		public LoadProjectNameSetupDelegate LoadProjectNameSetup;

		/// <summary/>
		public delegate void LoadAnalysisSetupDelegate();

		/// <summary/>
		public LoadAnalysisSetupDelegate LoadAnalysisSetup;

		/// <summary/>
		public delegate void LoadAnalysisSameAsVernacularDelegate();

		/// <summary/>
		public LoadAnalysisSameAsVernacularDelegate LoadAnalysisSameAsVernacularWarning;

		/// <summary/>
		public delegate void LoadVernacularSetupDelegate();

		/// <summary/>
		public LoadVernacularSetupDelegate LoadVernacularSetup;

		/// <summary/>
		public delegate void LoadAnthropologySetupDelegate();

		/// <summary/>
		public LoadAnthropologySetupDelegate LoadAnthropologySetup;

		/// <summary/>
		public delegate void LoadAdvancedWsSetupDelegate();

		/// <summary/>
		public LoadAdvancedWsSetupDelegate LoadAdvancedWsSetup;

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
		public IEnumerable<IWizardStep> Steps { get { return _steps; } }

		/// <summary/>
		public IWritingSystemContainer WritingSystemContainer { get; set; }

		/// <summary/>
		public WritingSystemManager WritingSystemManager { get; }

		/// <summary/>
		public FwNewLangProjectModel(bool useMemoryWsManager = false)
		{
			WritingSystemManager = useMemoryWsManager ? new WritingSystemManager() : new WritingSystemManager(SingletonsContainer.Get<CoreGlobalWritingSystemRepository>());
			CoreWritingSystemDefinition englishWs;
			WritingSystemManager.GetOrSet("en", out englishWs);
			_allAnalysis.Add(englishWs);
			_curAnalysis.Add(englishWs);
			WritingSystemContainer = new MemoryWritingSystemContainer(_allAnalysis, _allVern, _curAnalysis, _curVernWss, _curPron);
		}

		/// <summary/>
		public void Next()
		{
			if (CurrentStepIsValid())
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
			if (CurrentStepIsValid())
			{
				_steps[(int)CurrentStep].IsComplete = true;
			}
		}

		private bool CurrentStepIsValid()
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
		public void Back()
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
		public string ProjectName
		{
			get { return _projectName; }
			set
			{
				_projectName = value;
				_steps[(int)CurrentStep].IsComplete = IsProjectNameValid;
				LoadProjectNameSetup();
			}
		}

		/// <summary/>
		public bool IsProjectNameValid
		{
			get
			{
				string errorMessage;
				var projName = ProjectName;
				return !string.IsNullOrEmpty(ProjectName?.Trim()) && CheckForValidProjectName(ref projName, out errorMessage);
			}

		}

		/// <summary/>
		public string InvalidProjectNameMessage
		{
			get
			{
				if (string.IsNullOrEmpty(ProjectName))
				{
					return "Enter a project name.";
				}

				string errorMessage;
				var projName = ProjectName;
				if (!CheckForValidProjectName(ref projName, out errorMessage))
				{
					return errorMessage;
				}

				return string.Empty;
			}
		}

		/// <summary/>
		public FwChooseAnthroListModel AnthroModel = new FwChooseAnthroListModel();

		/// <summary/>
		public string CreateNewLangProj(IThreadedProgress progressDialog, ISynchronizeInvoke threadHelper)
		{
			try
			{
				var defaultAnalysis = WritingSystemContainer.CurrentAnalysisWritingSystems.First();
				var defaultVernacular = WritingSystemContainer.CurrentVernacularWritingSystems.First();
				return LcmCache.CreateNewLangProj(progressDialog, ProjectName, FwDirectoryFinder.LcmDirectories,
					threadHelper,
					defaultAnalysis,
					defaultVernacular,
					"en",// TODO: replicate original
					new HashSet<CoreWritingSystemDefinition>(WritingSystemContainer.AnalysisWritingSystems.Skip(1)),
					new HashSet<CoreWritingSystemDefinition>(WritingSystemContainer.VernacularWritingSystems.Skip(1)),
					AnthroModel.AnthroFileName);
			}
			catch (WorkerThreadException wex)
			{
				Exception e = wex.InnerException;
				if (e is UnauthorizedAccessException)
				{
					if (MiscUtils.IsUnix)
					{
						// Tell Mono user he/she needs to logout and log back in
						MessageBox.Show(ResourceHelper.GetResourceString("ksNeedToJoinFwGroup"));
					}
					else
					{
						MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
							FwUtils.ksSuiteName);
					}
				}
				else if (e is ApplicationException)
				{
					MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
						FwUtils.ksSuiteName);
				}
				else if (e is LcmInitializationException)
				{
					MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
						FwUtils.ksSuiteName);
				}
				else
				{
					throw new Exception(FwCoreDlgs.kstidErrApp, e);
				}
			}

			return null;
		}

		/// <param name="projectName">The Project Name reference will be modified to remove illegal characters</param>
		/// <param name="errorMessage"></param>
		/// <returns><c>true</c> if the given project name is valid; otherwise, <c>false</c></returns>
		public static bool CheckForValidProjectName(ref string projectName, out string errorMessage)
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
			if (firstIllegalChar > 0)
			{
				if (firstIllegalChar < 127) // likely an illegal symbol (e.g. /:\)
				{
					// Prepare to show the message:
					// Remove characters that cannot be keyboarded (below code point 32). The user doesn't
					// need to be warned about these since they can't be entered via the keyboard.
					var sIllegalCharsForMessage = sIllegalChars;
					for (int n = 0; n < 32; n++)
					{
						int index = sIllegalCharsForMessage.IndexOf((char)n);
						if (index >= 0)
							sIllegalCharsForMessage = sIllegalCharsForMessage.Remove(index, 1);
					}

					errorMessage = string.Format(FwCoreDlgs.ksIllegalNameMsg, sIllegalCharsForMessage);
				}
				else if (firstIllegalChar >= '\u00C0' && firstIllegalChar < '\u02B0') // likely a Latin character with diacritics
				// (somewhere between u0190 and u02B0, "letters with diacritics" become interspersed with extended Latin letters)
				{
					errorMessage = string.Format(FwCoreDlgs.ksIllegalNameWithDiacriticsMsg, firstIllegalChar);
				}
				else // Unicode (which could be diacritics, non-Latin characters, spacing markers, emoji, or many other things)
				{
					errorMessage = FwCoreDlgs.ksIllegalNameNonRomanMsg;
				}
				return false;
			}
			// The project name is valid check so check for a duplicate name
			var projInfo = ProjectInfo.GetProjectInfoByName(FwDirectoryFinder.ProjectsDirectory, projectName);
			if (projInfo == null)
				return true;
			errorMessage = string.Format(FwCoreDlgs.NewProjectWizard_DuplicateProjectName, projectName);
			return false;
		}

		/// <summary/>
		public bool CanGoBack()
		{
			return CurrentStep > 0;
		}

		/// <summary/>
		public bool CanGoNext()
		{
			return CurrentStepIsValid() && (int)CurrentStep < _steps.Length - 1;
		}

		/// <summary/>
		public bool CanFinish()
		{
			return _steps.All(step => step.IsOptional || step.IsComplete);
		}

		/// <summary/>
		public void SetDefaultWs(LanguageInfo selectedLanguage)
		{
			ICollection<CoreWritingSystemDefinition> allList;
			IList<CoreWritingSystemDefinition> currentList;
			switch (CurrentStep)
			{
				case NewProjStep.Vernacular:
				{
					currentList = WritingSystemContainer.CurrentVernacularWritingSystems;
					allList = WritingSystemContainer.VernacularWritingSystems;
					SetDefaultWsInCurrentList(selectedLanguage, currentList, allList);
					LoadVernacularSetup();
					break;
				}
				case NewProjStep.Analysis:
				{
					currentList = WritingSystemContainer.CurrentAnalysisWritingSystems;
					allList = WritingSystemContainer.AnalysisWritingSystems;
					SetDefaultWsInCurrentList(selectedLanguage, currentList, allList);
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

		private static void AddDefaultWsInAllList(CoreWritingSystemDefinition defaultWs, ICollection<CoreWritingSystemDefinition> allList)
		{
			var itemInList = allList.FirstOrDefault(ws => ws.LanguageTag == defaultWs.LanguageTag);
			if (itemInList == null)
			{
				var newList = new List<CoreWritingSystemDefinition>(allList);
				newList.Insert(0, defaultWs);
				allList.Clear();
				allList.AddRange(newList);
			}
			else
			{
				var newList = new List<CoreWritingSystemDefinition>(allList);
				newList.Remove(itemInList);
				newList.Insert(0, defaultWs);
				allList.Clear();
				allList.AddRange(newList);
			}
		}

		private void SetDefaultWsInCurrentList(LanguageInfo selectedLanguage, IList<CoreWritingSystemDefinition> currentList,
			ICollection<CoreWritingSystemDefinition> allList)
		{
			var itemInList = currentList.FirstOrDefault(ws => ws.LanguageTag == selectedLanguage.LanguageTag);
			if (itemInList == null)
			{
				WritingSystemManager.GetOrSet(selectedLanguage.LanguageTag, out itemInList);
				currentList.Insert(0, itemInList);
			}
			else
			{
				currentList.Remove(itemInList);
				currentList.Insert(0, itemInList);
			}

			AddDefaultWsInAllList(itemInList, allList);
		}
	}

	/// <summary/>
	public interface IWizardStep
	{
		/// <summary/>
		string StepName { get; set; }

		/// <summary/>
		bool IsOptional { get; set; }

		/// <summary/>
		bool IsCurrent { get; set; }

		/// <summary/>
		bool IsComplete { get; set; }
	}

	/// <summary/>
	public class NewLangProjStep : IWizardStep
	{
		/// <summary/>
		public NewLangProjStep(string name, bool optional, bool isCurrent = false)
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