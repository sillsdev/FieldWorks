using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Palaso.Reporting;
using SIL.Archiving;
using SIL.FieldWorks.Common.FwUtils;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using L10NSharp;
using Palaso.UI.WindowsForms.PortableSettingsProvider;
using System.Collections.Generic;
using System;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using XCore;
using SIL.CoreImpl;

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
		private static LocalizationManager s_localizationMgr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares the selected files to be uploaded to REAP using RAMP.
		/// </summary>
		/// <param name="owner">RAMP dialog owner</param>
		/// <param name="dialogFont">RAMP dialog font (for localization and consistency)</param>
		/// <param name="localizationDialogIcon"></param>
		/// <param name="filesToArchive"></param>
		/// <param name="mediator"></param>
		/// <param name="thisapp"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool ArchiveNow(Form owner, Font dialogFont, Icon localizationDialogIcon,
			IEnumerable<string> filesToArchive, Mediator mediator, FwApp thisapp, FdoCache cache)
		{
			var viProvider = new FwVersionInfoProvider(Assembly.LoadFile(thisapp.ProductExecutableFile), false);
			var wsMgr = cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			var appName = thisapp.ApplicationName;
			var title = cache.LanguageProject.ShortName;
			var uiLocale = wsMgr.Get(cache.DefaultUserWs).IcuLocale;
			var projectId = cache.LanguageProject.ShortName;

			var model = new ArchivingDlgViewModel(Application.ProductName, title, projectId, GetFileDescription);

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
					descriptions[GetIso3Code(wsMgr.Get(wsid))] = descrText;
			}

			if (descriptions.Count > 0)
				model.SetDescription(descriptions);

			AddMetsPairs(model, viProvider.ShortNumericAppVersion, cache);

			// this is a Palaso dialog, so we need to tell it the correct font to use.
			model.ProgramDialogFont = dialogFont;

			const string localizationMgrId = "Archiving";

			if (s_localizationMgr == null)
			{
				s_localizationMgr = LocalizationManager.Create(
					uiLocale,
					localizationMgrId, viProvider.ProductName, viProvider.NumericAppVersion,
					DirectoryFinder.GetFWCodeSubDirectory("ArchivingLocalizations"),
					DirectoryFinder.CommonAppDataFolder(appName),
					localizationDialogIcon, "FLExDevteam@sil.org", "SIL.Archiving");
			}
			else
			{
				LocalizationManager.SetUILanguage(uiLocale, true);
			}

			// create the dialog
			using (var dlg = new ArchivingDlg(model, localizationMgrId, string.Empty,
				() => GetFilesToArchive(filesToArchive), new FormSettings()))
			using (var reportingAdapter = new PalasoErrorReportingAdapter(dlg, mediator))
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

			if (Path.GetExtension(file) == FwFileExtensions.ksFwBackupFileExtension)
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
		private void AddMetsPairs(ArchivingDlgViewModel model, string fieldWorksVersion, FdoCache cache)
		{
			IWritingSystemManager wsManager = cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			var wsDefaultVern = wsManager.Get(cache.DefaultVernWs);
			var vernIso3Code = GetIso3Code(wsDefaultVern);

			model.SetScholarlyWorkType(ScholarlyWorkType.PrimaryData);

			// use year range for CreationDate if possible
			var yearStart = cache.LangProject.DateCreated.Year;
			var yearEnd = cache.LangProject.DateModified.Year;

			if (yearEnd > yearStart)
				model.SetCreationDate(yearStart, yearEnd);
			else
				model.SetCreationDate(cache.LangProject.DateCreated);

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
				var iso3Code = GetIso3Code(ws);

				if (!string.IsNullOrEmpty(iso3Code))
					contentLanguages.Add(new ArchivingLanguage(iso3Code, ws.LanguageSubtag.Name));

				if (!string.IsNullOrEmpty(ws.DefaultFontName))
					softwareRequirements.Add(ws.DefaultFontName);

				fWsUsesKeyman |= !string.IsNullOrEmpty(ws.Keyboard);
			}

			if (fWsUsesKeyman)
				softwareRequirements.Add("Keyman");

			model.SetContentLanguages(contentLanguages);
			model.SetSoftwareRequirements(softwareRequirements);

			SilDomain domains = SilDomain.Linguistics;
			if (cache.LangProject.ResearchNotebookOA.AllRecords.Any())
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

			if (cLexicalEntries > 0 || cTexts > 0)
			{
				model.SetDatasetExtent(string.Format("{0} Lexical entries; {1} Texts", cLexicalEntries, cTexts));
			}
		}

		private string GetIso3Code(IWritingSystem ws)
		{
			var iso3Code = ws.LanguageSubtag.ISO3Code;
			if (!string.IsNullOrEmpty(iso3Code))
				return iso3Code;

			iso3Code = ((PalasoWritingSystem)ws).RFC5646;

			// return "mis" for uncoded languages
			if (string.IsNullOrEmpty(iso3Code) || (iso3Code.Length != 3))
				return "mis";

			return iso3Code;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called by the Archiving Dialog to retrieve the lists of the files and description
		/// to be included in the RAMP package.
		/// </summary>
		/// <param name="filesToArchive">The files to include</param>
		/// <returns>Groups of files to archive and descriptive progress messages</returns>
		/// ------------------------------------------------------------------------------------
		private IDictionary<string, Tuple<IEnumerable<string>, string>> GetFilesToArchive(IEnumerable<string> filesToArchive)
		{
			// Explanation:
			//   IDictionary<string1, Tuple<IEnumerable<string2>, string3>>
			//     string1 = group name or key (used for normalizing file names in the zip file)
			//     string2 = file name (a list of the files in this group)
			//     string3 = progress message (a progress message for this group)
			var files = new Dictionary<string, Tuple<IEnumerable<string>, string>>();
			files[string.Empty] = new Tuple<IEnumerable<string>, string>(filesToArchive,
				ResourceHelper.GetResourceString("kstidAddingFwProject"));
			return files;
		}
	}
}
