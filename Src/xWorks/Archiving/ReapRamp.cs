// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SIL.Archiving;
using SIL.FieldWorks.Common.FwUtils;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using System.Collections.Generic;
using System;
using System.Threading;
using SIL.LCModel;
using SIL.FieldWorks.Resources;
using SIL.Reporting;
using SIL.Windows.Forms.Archiving;
using SIL.Windows.Forms.PortableSettingsProvider;
using XCore;
using SIL.LCModel.Core.WritingSystems;
using SIL.Keyboarding;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.XWorks.Archiving
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Packages selected files to be archived in the REAP repository and launches the RAMP
	/// program to do the actual transfer.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	class ReapRamp
	{

		private DateTime m_earliest = DateTime.MaxValue;
		private DateTime m_latest = DateTime.MinValue;

		private IEnumerable<string> m_filesToArchive;

		static ReapRamp()
		{
			var exePath = RampArchivingDlgViewModel.GetExeFileLocation();
			Installed = !string.IsNullOrEmpty(exePath) && File.Exists(exePath);
		}

		public static bool Installed { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares the selected files to be uploaded to REAP using RAMP.
		/// </summary>
		/// <param name="owner">RAMP dialog owner</param>
		/// <param name="dialogFont">RAMP dialog font (for localization and consistency)</param>
		/// <param name="localizationDialogIcon"></param>
		/// <param name="filesToArchive"></param>
		/// <param name="propertyTable"></param>
		/// <param name="thisapp"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool ArchiveNow(Form owner, Font dialogFont, Icon localizationDialogIcon,
			IEnumerable<string> filesToArchive, PropertyTable propertyTable, FwApp thisapp, LcmCache cache)
		{
			var viProvider = new VersionInfoProvider(Assembly.LoadFile(thisapp.ProductExecutableFile), false);
			WritingSystemManager wsMgr = cache.ServiceLocator.WritingSystemManager;
			var appName = thisapp.ApplicationName;
			var title = cache.LanguageProject.ShortName;
			var uiLocale = wsMgr.Get(cache.DefaultUserWs).IcuLocale;
			var projectId = cache.LanguageProject.ShortName;
			m_filesToArchive = filesToArchive;

			var model = new RampArchivingDlgViewModel(Application.ProductName, title, projectId, SetFilesToArchive, GetFileDescription);

			// image files should be labeled as Graphic rather than Photograph (the default).
			model.ImagesArePhotographs = false;

			// show the count of media files, not the duration
			model.ShowRecordingCountNotLength = true;

			// set the general description, in each available language
			IMultiString descr = cache.LanguageProject.Description;
			var descriptions = new Dictionary<string, string>();
			foreach (int wsid in descr.AvailableWritingSystemIds)
			{
				var descrText = descr.get_String(wsid).Text;
				if ((!string.IsNullOrEmpty(descrText)) && (descrText != "***"))
					descriptions[wsMgr.Get(wsid).GetIso3Code()] = descrText;
			}

			if (descriptions.Count > 0)
				model.SetDescription(descriptions);

			AddMetsPairs(model, viProvider.ShortNumericAppVersion, cache);

			// create the dialog
			using (var dlg = new ArchivingDlg(model, string.Empty, "Palaso", dialogFont, new FormSettings()))
			using (var reportingAdapter = new SilErrorReportingAdapter(dlg, propertyTable))
			{
				ErrorReport.SetErrorReporter(reportingAdapter);
				dlg.ShowDialog(owner);
				ErrorReport.SetErrorReporter(null);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called by the Archiving Dialog to get the file descriptions included in the
		/// RAMP package.
		/// </summary>
		/// <param name="key">A group name returned by GetFilesToArchive.</param>
		/// <param name="file">A file name returned by GetFilesToArchive.</param>
		/// <returns>A description of the file.</returns>
		/// ------------------------------------------------------------------------------------
		private string GetFileDescription(string key, string file)
		{
			// TODO: Extend to supply "relationship" also (source, presentation or supporting)

			if (Path.GetExtension(file) == LcmFileHelper.ksFwBackupFileExtension)
				return "FieldWorks backup";
			if (Path.GetExtension(file) == FwFileExtensions.ksLexiconInterchangeFormat)
				return "Lexical Interchange Format Standard file";
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets required and optional information that describes the files being submitted in
		/// the RAMP package. METS = Metadata Encoding & Transmission Standard
		/// (see http://www.loc.gov/METS/).
		/// </summary>
		/// <param name="model">Object provided by SIL.Archiving for setting application
		/// specific archiving information.</param>
		/// <param name="fieldWorksVersion">Fieldworks version to display.</param>
		/// <param name="cache"></param>
		/// <returns>A list of JSON encoded pairs that describe the information in the RAMP
		/// package.</returns>
		/// ------------------------------------------------------------------------------------
		private void AddMetsPairs(RampArchivingDlgViewModel model, string fieldWorksVersion, LcmCache cache)
		{
			WritingSystemManager wsManager = cache.ServiceLocator.WritingSystemManager;
			var wsDefaultVern = wsManager.Get(cache.DefaultVernWs);
			var vernIso3Code = wsDefaultVern.GetIso3Code();

			model.SetScholarlyWorkType(ScholarlyWorkType.PrimaryData);

			// use year range for CreationDate if possible
			GetCreateDateRange(cache);
			var yearStart = m_earliest.Year;
			var yearEnd = m_latest.Year;

			if (yearEnd > yearStart)
				model.SetCreationDate(yearStart, yearEnd);
			else
				model.SetCreationDate(m_earliest);

			model.SetModifiedDate(cache.LangProject.DateModified);

			if (!string.IsNullOrEmpty(vernIso3Code))
				model.SetSubjectLanguage(vernIso3Code, wsDefaultVern.LanguageName);

			var contentLanguages = new List<ArchivingLanguage>();
			var softwareRequirements = new HashSet<string>();
			bool fWsUsesKeyman = false;

			softwareRequirements.Add(string.Format("FieldWorks Language Explorer, Version {0} or later", fieldWorksVersion));

			foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Union(
				cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems).Union(
				cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems))
			{
				var iso3Code = ws.GetIso3Code();

				if (!string.IsNullOrEmpty(iso3Code))
					contentLanguages.Add(new ArchivingLanguage(iso3Code, ws.Language.Name));

				if (!string.IsNullOrEmpty(ws.DefaultFontName))
					softwareRequirements.Add(ws.DefaultFontName);
				fWsUsesKeyman |= DoesWritingSystemUseKeyman(ws);
			}

			if (fWsUsesKeyman)
				softwareRequirements.Add("Keyman");

			model.SetContentLanguages(contentLanguages);
			model.SetSoftwareRequirements(softwareRequirements);

			SilDomain domains = SilDomain.Linguistics;
			var cNotebookRecords = cache.LangProject.ResearchNotebookOA.AllRecords.Count();
			if (cNotebookRecords > 0)
			{
				domains |= SilDomain.Anthropology;
				domains |= SilDomain.Anth_Ethnography; // Data notebook data is considered a (partial) ethnography.
			}

			var cLexicalEntries = cache.LangProject.LexDbOA.Entries.Count();
			if (cLexicalEntries > 0)
				domains |= SilDomain.Ling_Lexicon;

			// Determine if there are any interlinearized texts
			if (cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().Any(a => a.OccurrencesInTexts.Any() &&
				a.GetAgentOpinion(cache.LangProject.DefaultUserAgent) == Opinions.approves))
				domains |= SilDomain.Ling_InterlinearizedText;

			var cTexts = cache.LangProject.Texts.Count();
			if (cTexts > 0)
				domains |= SilDomain.Ling_Text;

			/* TODO: If files to include in archive includes a Lift file, set the correct schema */
			//if (filesToArchive.Contains( FwFileExtensions.ksLexiconInterchangeFormat )
			//	model.SetSchemaConformance("LIFT");

			/* TODO: If files to include in archive includes a grammar sketch, set the correct subdomain */
			//if (filesToArchive.Contains(...)
			//	domains |= SilDomain.Ling_GrammaticalDescription;

			model.SetDomains(domains);

			// get the information for DatasetExtent
			var datasetExtent = new StringBuilder();
			const string delimiter = "; ";

			if (cNotebookRecords > 0)
				datasetExtent.AppendLineFormat("{0} Notebook record{1}", new object[] { cNotebookRecords, (cNotebookRecords == 1) ? "" : "s" }, delimiter);

			if (cLexicalEntries > 0)
				datasetExtent.AppendLineFormat("{0} Lexical entr{1}", new object[] { cLexicalEntries, (cLexicalEntries == 1) ? "y" : "ies" }, delimiter);

			if (cTexts > 0)
				datasetExtent.AppendLineFormat("{0} Text{1}", new object[] { cTexts, (cTexts == 1) ? "" : "s" }, delimiter);

			if (datasetExtent.Length > 0)
				model.SetDatasetExtent(datasetExtent + ".");
		}

		/// <summary>
		/// Returns true if the writing system has an active keyman keyboard
		/// </summary>
		/// <remarks>Internal for testing, uses reflection to identify a keyboard as keyman</remarks>
		internal static bool DoesWritingSystemUseKeyman(CoreWritingSystemDefinition ws)
		{
			if (Platform.IsLinux) // Keyman is not required on linux
				return false;
			if (ws.KnownKeyboards.Any())
			{
				foreach (IKeyboardDefinition keyboard in ws.KnownKeyboards)
				{
					if (!keyboard.IsAvailable)
						continue;
					if (keyboard.Format == KeyboardFormat.CompiledKeyman || keyboard.Format == KeyboardFormat.Keyman)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called by the Archiving Dialog to retrieve the lists of the files and description
		/// to be included in the RAMP package.
		/// </summary>
		/// <param name="filesToArchive">The files to include</param>
		/// ------------------------------------------------------------------------------------
		private void SetFilesToArchive(ArchivingDlgViewModel advModel, CancellationToken token)
		{
			advModel.AddFileGroup(string.Empty, m_filesToArchive, ResourceHelper.GetResourceString("kstidAddingFwProject"));
		}

		private void GetCreateDateRange(LcmCache cache)
		{
			foreach (var obj in cache.LangProject.ResearchNotebookOA.AllRecords)
				CompareDateCreated(obj.DateCreated);

			foreach (var obj in cache.LangProject.LexDbOA.Entries.Where(o => (o.DateCreated < m_earliest) || (o.DateCreated > m_latest)))
				CompareDateCreated(obj.DateCreated);

			foreach (var obj in cache.LangProject.Texts.Where(o => (o.DateCreated < m_earliest) || (o.DateCreated > m_latest)))
				CompareDateCreated(obj.DateCreated);
		}

		private void CompareDateCreated(DateTime created)
		{
			if (created < m_earliest)
				m_earliest = created;

			if (created > m_latest)
				m_latest = created;
		}
	}
}
