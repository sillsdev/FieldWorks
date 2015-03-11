// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeKeyTermsInit.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Initializes the KeyTerms in the database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeKeyTermsInit : ExternalSettingsAccessorBase<BiblicalTermsList>
	{
		#region Member variables
		/// <summary>The FDO Scripture object which will own the new styles</summary>
		protected IScripture m_scr;
		private IApp m_app;
		private IFdoServiceLocator m_servLoc;
		private ILgWritingSystemFactory m_wsf;
		private ITsStrFactory m_strFactory = TsStrFactoryClass.Create();
		private int m_wsDefault;
		private int m_wsGreek;
		private int m_wsHebrew;
		private readonly uint uintSize = (uint)Marshal.SizeOf(typeof(int));
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeKeyTermsInit"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected TeKeyTermsInit(IScripture scr, IApp app)
		{
			m_scr = scr;
			m_app = app;
			m_wsf = scr.Cache.LanguageWritingSystemFactoryAccessor;
			m_servLoc = scr.Cache.ServiceLocator;
		}
		#endregion

		#region Overidden properties
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the relative path to key terms
		/// file from the FieldWorks install folder.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceFilePathFromFwInstall
		{
			get { return Path.Combine(FwDirectoryFinder.ksTeFolderName, ResourceFileName); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name (no path, no extension) of the settings file.
		/// For example, "Testyles"
		/// </summary>
		/// <value>"BiblicalTerms"</value>
		/// ------------------------------------------------------------------------------------
		protected override string ResourceName
		{
			get { return TeResourceHelper.BiblicalTermsResourceName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<ICmResource> ResourceList
		{
			get { return m_scr.ResourcesOC; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FdoCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_scr.Cache; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TODO: for now, this just creates the key terms list if the list isn't in the DB...
		/// If the current key terms version in the DD doesn't match that of the current XML
		/// file, update the DB.
		/// </summary>
		/// <param name="lp">language project</param>
		/// <param name="app">The TE application.</param>
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureCurrentKeyTerms(ILangProject lp, FwApp app,
			IThreadedProgress existingProgressDlg)
		{
			TeKeyTermsInit keyTermsInit = new TeKeyTermsInit(lp.TranslatedScriptureOA, app);
			keyTermsInit.EnsureCurrentResource(existingProgressDlg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the specified localization is up-to-date with respect to the localization
		/// file.
		/// </summary>
		/// <param name="locale">The ICU locale for the localization to check/update.</param>
		/// <param name="scr">Scripture object (which owns the resources that store the version
		/// info.</param>
		/// <param name="app">The application (needed for error reporting info).</param>
		/// <param name="caller">The form that is calling this method (to be used as the owner
		/// of the progress dialog box).</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureCurrentLocalization(string locale, IScripture scr, FwApp app,
			Form caller)
		{
			TeKeyTermsInit keyTermsInit = new TeKeyTermsInit(scr, app);
			keyTermsInit.EnsureCurrentLocalization(locale, caller, null);
		}
		#endregion

		#region Overridden Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the settings file.
		/// </summary>
		/// <returns>The loaded document</returns>
		/// ------------------------------------------------------------------------------------
		protected override BiblicalTermsList LoadDoc()
		{
			return DeserializeBiblicalTermsFile(Path.Combine(FwDirectoryFinder.CodeDirectory,
				ResourceFilePathFromFwInstall));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <returns>
		/// A GUID based on the version attribute node
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid GetVersion(BiblicalTermsList document)
		{
			return document.Version;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (Add Biblical Terms info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="doc">The loaded document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessResources(IThreadedProgress dlg, BiblicalTermsList doc)
		{
			dlg.RunTask(CreateKeyTerms, doc);
		}
		#endregion

		#region Protected methods for reading XML files into classes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes the biblical terms file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>a BiblicalTermsList object</returns>
		/// ------------------------------------------------------------------------------------
		static protected BiblicalTermsList DeserializeBiblicalTermsFile(string filename)
		{
			try
			{
				using (TextReader reader = new StreamReader(filename))
				{
					XmlSerializer deserializer = new XmlSerializer(typeof(BiblicalTermsList));
					BiblicalTermsList data = (BiblicalTermsList)deserializer.Deserialize(reader);
					reader.Close();
					return data;
				}
			}
			catch (Exception e)
			{
				string message;
#if DEBUG
				message = "Error reading " + filename + ": file is missing or has invalid XML syntax.";
#else
				message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
				throw new InstallationException(message, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes a biblical terms localization file.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>a BiblicalTermsLocalization object</returns>
		/// ------------------------------------------------------------------------------------
		static protected BiblicalTermsLocalization DeserializeBiblicalTermsLocFile(string filename)
		{
			try
			{
				using (TextReader reader = new StreamReader(filename))
				{
					XmlSerializer deserializer = new XmlSerializer(typeof(BiblicalTermsLocalization));
					BiblicalTermsLocalization data =
						(BiblicalTermsLocalization)deserializer.Deserialize(reader);
					reader.Close();
					return data;
				}
			}
			catch (Exception e)
			{
				string message;
#if DEBUG
				message = "Error reading " + filename + ": file is missing or has invalid XML syntax.";
#else
				message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
				throw new InstallationException(message, e);
			}
		}
		#endregion

		#region Top-level entry points for initilizing the biblical terms list
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the KeyTerms XML file into the given possibility list. This is the version
		/// that gets called by TeScrInitializer when Scripture is first
		/// being added to a new DB.
		/// </summary>
		/// <param name="dlg">Progress dialog box</param>
		/// <param name="keyTermsList">The possibility list where the biblical key terms are to
		/// be stored</param>
		/// <param name="app">The TE application.</param>
		/// ------------------------------------------------------------------------------------
		public static void CreateKeyTerms(IProgress dlg, ICmPossibilityList keyTermsList, FwApp app)
		{
			new TeKeyTermsInit(keyTermsList.Cache.LangProject.TranslatedScriptureOA, app).LoadKeyTerms(
				dlg, keyTermsList);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the given BiblicalTermsList. This is the version that gets called when opening
		/// an existing project if the resource version number doesn't match.
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// <param name="parameters">Only parameter is the class that holds the key terms info.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected object CreateKeyTerms(IProgress progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			ICmPossibilityList ktList = m_scr.Cache.LangProject.KeyTermsList;
			Debug.Assert(ktList.PossibilitiesOS.Count > 0, "I (TomB) doubt it really matters if this fails (though it's never been tried), but theoretically I think this should never happen since all released versions of TE have had key terms. If this assertion fails, check to make sure the biblical terms list was created correctly and try to figure out how this DB came into existence.");
			ICmPossibilityList tempList = m_scr.Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			m_scr.Cache.LangProject.CheckListsOC.Add(tempList);
			// Copy terms from previous list into the new list.
			foreach (ICmPossibility poss in ktList.PossibilitiesOS)
				tempList.PossibilitiesOS.Add(poss);
			LoadKeyTerms(progressDlg, tempList, ktList, (BiblicalTermsList)parameters[0]);
			tempList.Delete();
			Debug.Assert(!m_servLoc.GetInstance<IChkTermRepository>().AllInstances().Any(t => t.TermId == 0));
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that all the localizations of the key terms list for the UI and current
		/// analysis writing systems are up-to-date.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EnsureCurrentLocalizations(IThreadedProgress progressDlg)
		{
			string locale = Cache.WritingSystemFactory.GetStrFromWs(Cache.DefaultUserWs);
			EnsureCurrentLocalization(locale, null, progressDlg);
			foreach (CoreWritingSystemDefinition ws in Cache.LangProject.AnalysisWritingSystems.Where(w => w.Handle != Cache.DefaultUserWs))
				EnsureCurrentLocalization(ws.IcuLocale, null, progressDlg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the given localization is at the current version.
		/// </summary>
		/// <param name="locale">The locale representing the required localization.</param>
		/// <param name="caller">The form that is calling this method (used as the owner
		/// of the progress dialog box - can be null if progress dialog is supplied).</param>
		/// <param name="existingProgressDlg">The existing progress dialog box if any.</param>
		/// ------------------------------------------------------------------------------------
		private void EnsureCurrentLocalization(string locale, Form caller, IThreadedProgress existingProgressDlg)
		{
			string localizationFile = FwDirectoryFinder.GetKeyTermsLocFilename(locale);
			if (!FileUtils.FileExists(localizationFile))
				return; // There is no localization available for this locale, so we're as current as we're going to get.

			BiblicalTermsLocalization loc;
			try
			{
				loc = DeserializeBiblicalTermsLocFile(localizationFile);
			}
			catch (InstallationException e)
			{
				ErrorReporter.ReportException(e, m_app.SettingsKey, m_app.SupportEmailAddress, caller, false);
				return;
			}

			string resourceName = GetLocalizationResourceName(locale);
			if (IsResourceOutdated(resourceName, loc.Version))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_servLoc.GetInstance<IActionHandler>(),
					() => {
						existingProgressDlg.RunTask(true, UpdateLocalization, loc, locale);
						SetNewResourceVersion(resourceName, loc.Version);
					});
			}
		}
		#endregion

		#region Main methods that process the biblical terms
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the key terms from the bilical terms file.
		/// </summary>
		/// <param name="dlg">The DLG.</param>
		/// <param name="biblicalTermsPossList">The biblical terms poss list.</param>
		/// ------------------------------------------------------------------------------------
		protected void LoadKeyTerms(IProgress dlg, ICmPossibilityList biblicalTermsPossList)
		{
			LoadKeyTerms(dlg, null, biblicalTermsPossList, LoadDoc());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the key terms from the given list
		/// </summary>
		/// <param name="dlg">Progress dialog box</param>
		/// <param name="oldKtPossList">The old Key Terms possibility list (can be null).</param>
		/// <param name="biblicalTermsPossList">The new biblical terms possibility list</param>
		/// <param name="list">The source list of biblical terms as read in from the XML file.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected void LoadKeyTerms(IProgress dlg, ICmPossibilityList oldKtPossList,
			ICmPossibilityList biblicalTermsPossList, BiblicalTermsList list)
		{
			if (list.Version != new Guid("00FEA689-3B63-4bd4-B640-7262A274D1A8"))
				ReportInvalidInstallation(String.Format(
					"This version of Translation Editor only supports {0} version '00FEA689-3B63-4bd4-B640-7262A274D1A8' (see TE-2901).",
					TeResourceHelper.BiblicalTermsResourceName));
			List<BiblicalTermsLocalization> localizations = GetLocalizations();
			LoadKeyTerms(dlg, oldKtPossList, biblicalTermsPossList, list, localizations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the key terms from the given list
		/// </summary>
		/// <param name="dlg">Progress dialog box</param>
		/// <param name="oldKtPossList">The old Key Terms possibility list (can be null).</param>
		/// <param name="biblicalTermsPossList">The biblical terms possibility list</param>
		/// <param name="list">The source list of biblical terms as read in from the XML file.
		/// </param>
		/// <param name="localizations">The localizations.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		protected void LoadKeyTerms(IProgress dlg, ICmPossibilityList oldKtPossList,
			ICmPossibilityList biblicalTermsPossList,
			BiblicalTermsList list, List<BiblicalTermsLocalization> localizations)
		{
			FdoCache cache = biblicalTermsPossList.Cache;
			Debug.Assert(cache != null);

			EnsureGreekAndHebrewWsExist();

			m_wsDefault = m_servLoc.WritingSystemManager.GetWsFromStr("en");

			Dictionary<string, ICmPossibility> categories = new Dictionary<string, ICmPossibility>(5);
			List<Term> terms = list.KeyTerms;
			string message = null;
			if (dlg != null)
			{
				dlg.Position = 0;
				dlg.Minimum = 0;
				dlg.Maximum = terms.Count +
					((oldKtPossList != null) ? oldKtPossList.PossibilitiesOS.Count : 0);
				dlg.Title = TeResourceHelper.GetResourceString("kstidLoadKeyTermsInDBCaption");
				message = TeResourceHelper.GetResourceString("kstidLoadKeyTermsInDBStatus");
			}

			// TODO (TE-2901): Load any existing categories into the hashtable (needed when upgrading)
			//foreach (biblicalTerms.PossibilitiesOS
			// Load all of the keyterms
			foreach (Term term in terms)
			{
				// Update dialog message.
				if (dlg != null)
					dlg.Message = string.Format(message, term.Gloss);

				ICmPossibility cat;
				if (!categories.TryGetValue(term.Category, out cat))
				{
					cat = m_servLoc.GetInstance<ICmPossibilityFactory>().Create();
					biblicalTermsPossList.PossibilitiesOS.Add(cat);
					cat.Abbreviation.set_String(m_wsDefault, term.Category);
					foreach (BiblicalTermsLocalization loc in localizations)
					{
						string name = loc.GetCategoryName(term.Category);
						if (name != null)
							cat.Name.set_String(loc.WritingSystemHvo, name);
					}
					categories.Add(term.Category, cat);
				}

				AddTerm(cat, term, localizations);
				if (dlg != null)
					dlg.Step(1);
			}

			if (oldKtPossList != null)
			{
				if (dlg != null)
					dlg.Message = TeResourceHelper.GetResourceString("kstidMigratingKeyTermReferences");
				if (categories.ContainsKey("KT"))
				{
					CopyRenderingsFromOldCheckRefs(categories["KT"],
						oldKtPossList.PossibilitiesOS, dlg);
				}
			}

			// Finally, update biblical terms list version (and versions of localizations)
			// in the database.
			SetNewResourceVersion(GetVersion(list));
			foreach (BiblicalTermsLocalization localization in localizations)
			{
				SetNewResourceVersion(GetLocalizationResourceName(m_wsf.GetStrFromWs(localization.WritingSystemHvo)),
					localization.Version);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the existing categories and terms with new localized strings.
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) the BiblicalTermsLocalization object
		/// representing the contents of the XML file with the localized strings; 2) The ICU
		/// locale (string).</param>
		/// <returns>always null</returns>
		/// ------------------------------------------------------------------------------------
		protected object UpdateLocalization(IThreadedProgress dlg, params object[] parameters)
		{
			BiblicalTermsLocalization loc = (BiblicalTermsLocalization)parameters[0];
			string locale = (string)parameters[1];

			const int kcStepsToBuildLookupTable = 4;
			dlg.Position = 0;
			dlg.Minimum = 0;
			dlg.Maximum = loc.Categories.Count + loc.Terms.Count + kcStepsToBuildLookupTable;
			dlg.Title = TeResourceHelper.GetResourceString("kstidLoadKeyTermsInDBCaption");
			dlg.Message = TeResourceHelper.FormatResourceString("kstidLoadKeyTermsLocalizations",
					m_wsf.get_Engine(locale).LanguageName);

			m_wsDefault = m_servLoc.WritingSystemManager.GetWsFromStr("en");

			int hvoLocWs = loc.WritingSystemHvo = m_wsf.GetWsFromStr(locale);

			IEnumerable<ICmPossibility> categories = ((ILangProject)m_scr.Owner).KeyTermsList.PossibilitiesOS;
			foreach (CategoryLocalization localizedCategory in loc.Categories)
			{
				ICmPossibility category = categories.FirstOrDefault(
					p => p.Abbreviation.get_String(m_wsDefault).Text == localizedCategory.Id);
				if (category != null)
					category.Name.set_String(hvoLocWs, localizedCategory.Gloss);
				dlg.Step(1);
			}

			Dictionary<int, IChkTerm> termLookupTable =
				m_servLoc.GetInstance<IChkTermRepository>().AllInstances().ToDictionary(t => t.TermId);
			dlg.Step(kcStepsToBuildLookupTable);

			IChkTerm term;
			string message = TeResourceHelper.GetResourceString("kstidLoadKeyTermsInDBStatus");
			foreach (TermLocalization localizedTerm in loc.Terms)
			{
				dlg.Message = string.Format(message, localizedTerm.Gloss);

				if (termLookupTable.TryGetValue(localizedTerm.Id, out term))
					SetLocalizedGlossAndDescription(term, localizedTerm, loc.WritingSystemHvo);
				dlg.Step(1);
			}
			return null;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Matches the old check refs to the new check refs and copies over any renderings,
		/// then adds the ids of the old ChkTerms and ChkRefs to the set of IDs to be deleted.
		/// </summary>
		/// <param name="newKeyTerms">The CmPossibility whose SubPossiblities are the new key
		/// terms.</param>
		/// <param name="oldTerms">The old (top-level) key terms.</param>
		/// <param name="dlg">Progress dialog box (can be null)</param>
		/// ------------------------------------------------------------------------------------
		private void CopyRenderingsFromOldCheckRefs(ICmPossibility newKeyTerms,
			IFdoOwningSequence<ICmPossibility> oldTerms, IProgress dlg)
		{
			foreach (ICmPossibility poss in oldTerms)
			{
				// TE-7697, oldTerms contained something that wasn't a IChkTerm, so changed
				// using IChkTerm as loop variable to explicit cast and check for failure
				IChkTerm term = poss as IChkTerm;
				if (term != null)
				{
					foreach (IChkRef oldChkRef in term.OccurrencesOS)
					{
						if (oldChkRef.Status != KeyTermRenderingStatus.Unassigned)
						{
							foreach (IChkRef newChkRef in ChkRefMatcher.FindCorrespondingChkRefs(
								newKeyTerms, oldChkRef))
							{
								newChkRef.Status = oldChkRef.Status;
								newChkRef.RenderingRA = oldChkRef.RenderingRA;
								if (newChkRef.RenderingRA != null)
								{
									IChkTerm owningTerm = (IChkTerm)newChkRef.Owner;
									bool fRenderingAlreadyInCollection = false;
									foreach (IChkRendering rendering in owningTerm.RenderingsOC)
									{
										if (rendering.SurfaceFormRA == newChkRef.RenderingRA)
										{
											fRenderingAlreadyInCollection = true;
											break;
										}
									}
									if (!fRenderingAlreadyInCollection)
									{
										IChkRendering rendering = m_servLoc.GetInstance<IChkRenderingFactory>().Create();
										owningTerm.RenderingsOC.Add(rendering);
										rendering.SurfaceFormRA = newChkRef.RenderingRA;
									}
								}
							}
						}
					}
					if (dlg != null)
						dlg.Step(1);
				}
				CopyRenderingsFromOldCheckRefs(newKeyTerms, poss.SubPossibilitiesOS, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource name for the given locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetLocalizationResourceName(string locale)
		{
			return ResourceName + "_" + locale;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the available Key Terms localizations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<BiblicalTermsLocalization> GetLocalizations()
		{
			int defaultUserWs = m_scr.Cache.DefaultUserWs;
			string[] locFiles = FwDirectoryFinder.KeyTermsLocalizationFiles;
			List<BiblicalTermsLocalization> localizations =
				new List<BiblicalTermsLocalization>(locFiles.Length);
			bool fFoundDefaultLoc = false;
			foreach (string localizationFile in locFiles)
			{
				int hvoWs = GetWsFromLocFile(m_wsf, localizationFile);
				if (hvoWs > 0)
				{
					BiblicalTermsLocalization loc = DeserializeBiblicalTermsLocFile(localizationFile);
					if (loc != null)
					{
						fFoundDefaultLoc |= (hvoWs == defaultUserWs);
						loc.WritingSystemHvo = hvoWs;
						localizations.Add(loc);
					}
				}
			}
			if (!fFoundDefaultLoc || localizations.Count == 0)
			{
				string icuLocale = m_wsf.GetStrFromWs(defaultUserWs);
				string message = String.Format("File {0} is missing", FwDirectoryFinder.GetKeyTermsLocFilename(icuLocale));
				Debug.Fail(message);
				Logger.WriteEvent(message);
				if (icuLocale == "en" || localizations.Count == 0)
				{
#if !DEBUG
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new InstallationException(message, null);
				}
			}
			return localizations;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws from loc file.
		/// </summary>
		/// <param name="wsf">The Writing System factory.</param>
		/// <param name="localizationFile">The localization file path.</param>
		/// <returns>The HVO of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetWsFromLocFile(ILgWritingSystemFactory wsf, string localizationFile)
		{
			return wsf.GetWsFromStr(FwDirectoryFinder.GetLocaleFromKeyTermsLocFile(localizationFile));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the Greek and Hebrew writing systems exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnsureGreekAndHebrewWsExist()
		{
			m_wsGreek = EnsureWritingSystemExists("grc", "grk");
			m_wsHebrew = EnsureWritingSystemExists("hbo", "heb");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the given writing system exists.
		/// </summary>
		/// <param name="wsId">The identifier.</param>
		/// <param name="sAbbrev">The abbreviation.</param>
		/// <returns>The HVO of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		private int EnsureWritingSystemExists(string wsId, string sAbbrev)
		{
			CoreWritingSystemDefinition ws;
			if (!Cache.ServiceLocator.WritingSystemManager.GetOrSet(wsId, out ws))
				ws.Abbreviation = sAbbrev;
			return ws.Handle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in a keyterm node in the database from a Term
		/// </summary>
		/// <param name="owner">CmPossibility that is above the term in the hierarchy (probably
		/// a category)</param>
		/// <param name="term">Term to get info from</param>
		/// <param name="localizations">The localizations.</param>
		/// ------------------------------------------------------------------------------------
		private void AddTerm(ICmPossibility owner, Term term,
			IEnumerable<BiblicalTermsLocalization> localizations)
		{
			int hvoWs = (term.Language == "Hebrew") ? m_wsHebrew : m_wsGreek;
			// Strip off any sense numbers
			string lemma = term.Lemma.TrimEnd(
				'-', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
			IChkTerm newTerm = CreateChkTerm(term.Id, hvoWs, lemma, term.Including, owner);
			Debug.Assert(newTerm != null);

			foreach (BiblicalTermsLocalization loc in localizations)
			{
				TermLocalization termLoc = loc.FindTerm(term.Id);
				if (termLoc != null)
				{
					// First "gloss" is the primary one and will become the name of the
					// possibility. Susequent glosses will be stored in the SeeAlso field.
					SetLocalizedGlossAndDescription(newTerm, termLoc, loc.WritingSystemHvo);
				}
			}

			// If there are references on this node then add them to the keyterm node
			AddRefsToKeyTerm(term.Id, newTerm, hvoWs, term.References,
				term.Form ?? lemma);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the localized gloss, description, and "see also" information in the given
		/// writing system for a key term.
		/// </summary>
		/// <param name="term">The key term</param>
		/// <param name="termLoc">The localization information</param>
		/// <param name="ws">The HVO of the writing system</param>
		/// ------------------------------------------------------------------------------------
		private void SetLocalizedGlossAndDescription(IChkTerm term, TermLocalization termLoc, int ws)
		{
			string[] glosses = termLoc.Gloss.Split(new [] { ';' }, 2);
			SetLocalizedInfo(term, ws, glosses[0].Trim(), termLoc.DescriptionText == null ? null :
				termLoc.DescriptionText.Trim().Trim('-'), (glosses.Length == 2) ? glosses[1].Trim() : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add references to a sense or keyterm node in the database from XML data.
		/// </summary>
		/// <param name="termId">The term ID.</param>
		/// <param name="term">The ChkTerm</param>
		/// <param name="hvoWs">The HVO of the original-language writing system.</param>
		/// <param name="references">List of long integers representing BBCCCVVV references plus
		/// two digits indicating the number of the word in the verse in the original text</param>
		/// <param name="keyword">The specific Greek or Hebrew word(s) in the original text,
		/// though not necessarily the surface (i.e., inflected) form.</param>
		/// ------------------------------------------------------------------------------------
		private void AddRefsToKeyTerm(int termId, IChkTerm term, int hvoWs,
			List<long> references, string keyword)
		{
			// Get all of the ref items from the sense node
			foreach (long reference in references)
			{
				int scrRef = (int)(reference / 100); // Get rid of last two digits
				int location = (int)(reference % 100);
				CreateChkRef(term, hvoWs, keyword, scrRef, location);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a Check Term
		/// </summary>
		/// <param name="termId">The term id.</param>
		/// <param name="hvoWs">The HVO of the original-language writing system.</param>
		/// <param name="name">The Lemma form of the term in the original language (i.e., Greek
		/// or Hebrew)</param>
		/// <param name="description">A list of other derived forms of the term in the original
		/// language (can be null)</param>
		/// <param name="owner">CmPossibility that is above the term in the hierarchy (probably
		/// a category)</param>
		/// <returns>The new ChkTerm</returns>
		/// ------------------------------------------------------------------------------------
		private IChkTerm CreateChkTerm(int termId, int hvoWs, string name, string description,
			ICmPossibility owner)
		{
			IChkTerm newTerm = m_servLoc.GetInstance<IChkTermFactory>().Create();
			owner.SubPossibilitiesOS.Add(newTerm);
			newTerm.TermId = termId;
			newTerm.Name.set_String(hvoWs, name);

			if (!String.IsNullOrEmpty(description))
				newTerm.Description.set_String(hvoWs, description);

			return newTerm;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds Gloss, Definition and SeeAlso info to a Check Term for the given writing system
		/// </summary>
		/// <param name="term">The ChkTerm</param>
		/// <param name="hvoWs">The HVO of the writing system.</param>
		/// <param name="gloss">The primary gloss.</param>
		/// <param name="description">The description.</param>
		/// <param name="seeAlso">Alternate glosses, separated by semi-colons.</param>
		/// ------------------------------------------------------------------------------------
		private void SetLocalizedInfo(IChkTerm term, int hvoWs, string gloss,
			string description, string seeAlso)
		{
			term.Name.set_String(hvoWs, gloss);
			term.Description.set_String(hvoWs, description);
			term.SeeAlso.set_String(hvoWs, seeAlso);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a ChkRef object
		/// </summary>
		/// <param name="term">The ChkTerm that will own the ChkRef</param>
		/// <param name="hvoWs">The HVO of the original-language writing system.</param>
		/// <param name="keyword">The specific Greek or Hebrew word(s) in the original text,
		/// though not necessarily the surface (i.e., inflected) form.</param>
		/// <param name="reference">Scripture reference in BBCCCVVV format</param>
		/// <param name="location">The 1-based index of the word in the verse in the original
		/// language</param>
		/// ------------------------------------------------------------------------------------
		private void CreateChkRef(IChkTerm term, int hvoWs, string keyword, int reference,
			int location)
		{
			IChkRef newRef = m_servLoc.GetInstance<IChkRefFactory>().Create();
			term.OccurrencesOS.Add(newRef);
			newRef.Ref = reference;
			newRef.KeyWord = m_scr.Cache.TsStrFactory.MakeString(keyword, hvoWs);
			newRef.Location = location;
		}
		#endregion
	}
}
