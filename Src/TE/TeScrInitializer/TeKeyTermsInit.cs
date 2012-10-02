// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeKeyTermsInit.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Xml.Serialization;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using System.Runtime.InteropServices;

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
		private IFdoServiceLocator m_servLoc;
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
		/// <param name="scr">The Scripture object.</param>
		/// ------------------------------------------------------------------------------------
		protected TeKeyTermsInit(IScripture scr)
		{
			m_scr = scr;
			m_servLoc = scr.Cache.ServiceLocator;
		}
		#endregion

		#region Overidden properties
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the relative path to stylesheet
		/// configuration file from the FieldWorks install folder.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceFilePathFromFwInstall
		{
			get { return ResourceFileName; }
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
			get { return "BiblicalTerms"; }
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
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureCurrentKeyTerms(ILangProject lp, IProgress existingProgressDlg)
		{
			TeKeyTermsInit keyTermsInit = new TeKeyTermsInit(lp.TranslatedScriptureOA);
			keyTermsInit.EnsureCurrentResource(existingProgressDlg);
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
			return DeserializeBiblicalTermsFile(Path.Combine(DirectoryFinder.FWCodeDirectory,
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
		/// <param name="progressDlg">The progress dialog box itself.</param>
		/// <param name="doc">The loaded document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessResources(ProgressDialogWithTask dlg, IProgress progressDlg,
			BiblicalTermsList doc)
		{
			dlg.RunTask(progressDlg, true, CreateKeyTerms, doc);
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
		/// ------------------------------------------------------------------------------------
		public static void CreateKeyTerms(IProgress dlg, ICmPossibilityList keyTermsList)
		{
			FdoCache cache = keyTermsList.Cache;

			BiblicalTermsList list = DeserializeBiblicalTermsFile(
				Path.Combine(DirectoryFinder.FWCodeDirectory, "BiblicalTerms.xml"));

			new TeKeyTermsInit(cache.LangProject.TranslatedScriptureOA).LoadKeyTerms(
					dlg, null, keyTermsList, list);
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
			m_scr.Cache.LangProject.CheckListsOC.Remove(tempList);
			return null;
		}
		#endregion

		#region Main methods that process the biblical terms
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
				ReportInvalidInstallation("This version of Translation Editor only supports BiblicalTerms.xml version '00FEA689-3B63-4bd4-B640-7262A274D1A8' (see TE-2901).");
			FdoCache cache = m_scr.Cache;
			List<BiblicalTermsLocalization> localizations = GetLocalizations(cache);
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
		protected void LoadKeyTerms(IProgress dlg, ICmPossibilityList oldKtPossList,
			ICmPossibilityList biblicalTermsPossList,
			BiblicalTermsList list, List<BiblicalTermsLocalization> localizations)
		{
			FdoCache cache = biblicalTermsPossList.Cache;
			Debug.Assert(cache != null);

			EnsureGreekAndHebrewWsExist(cache);

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

			// Finally, update biblical terms list version in database.
			SetNewResourceVersion(GetVersion(list));
		}

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
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the localizations.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>A list of BiblicalTermsLocalization</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual List<BiblicalTermsLocalization> GetLocalizations(FdoCache cache)
		{
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			string[] locFiles = Directory.GetFiles(DirectoryFinder.FWCodeDirectory,
				"BiblicalTerms-*.xml", SearchOption.TopDirectoryOnly);
			List<BiblicalTermsLocalization> localizations =
				new List<BiblicalTermsLocalization>(locFiles.Length);
			bool fFoundDefaultLoc = false;
			foreach (string localizationFile in locFiles)
			{
				int hvoWs = GetWsFromLocFile(wsf, localizationFile);
				if (hvoWs > 0)
				{
					BiblicalTermsLocalization loc = DeserializeBiblicalTermsLocFile(localizationFile);
					if (loc != null)
					{
						fFoundDefaultLoc |= (hvoWs == cache.DefaultUserWs);
						loc.WritingSystemHvo = hvoWs;
						localizations.Add(loc);
					}
				}
			}
			if (!fFoundDefaultLoc || localizations.Count == 0)
			{
				string icuLocale = wsf.GetStrFromWs(cache.DefaultUserWs);
				Debug.Fail(String.Format("File BiblicalTerms-{0}.xml is missing", icuLocale));
				if (icuLocale == "en" || localizations.Count == 0)
				{
#if DEBUG
					string message = String.Format("File BiblicalTerms-{0}.xml is missing",
						wsf.GetStrFromWs(cache.DefaultUserWs));
#else
					string message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
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
			string icuLocale = Path.GetFileName(localizationFile).Replace("BiblicalTerms-",
				String.Empty).Replace(".xml", String.Empty);
			return wsf.GetWsFromStr(icuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the Greek and Hebrew writing systems exist.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private void EnsureGreekAndHebrewWsExist(FdoCache cache)
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
			IWritingSystem ws;
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
			List<BiblicalTermsLocalization> localizations)
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
					string[] glosses = termLoc.Gloss.Split(new char[] { ';' }, 2);
					SetLocalizedInfo(newTerm, term.Id, loc.WritingSystemHvo,
						glosses[0].Trim(), termLoc.DescriptionText,
						(glosses.Length == 2) ? glosses[1].Trim() : null);
				}
			}

			// If there are references on this node then add them to the keyterm node
			AddRefsToKeyTerm(term.Id, newTerm, hvoWs, term.References,
				term.Form ?? lemma);
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
		/// <param name="termId">The term ID.</param>
		/// <param name="hvoWs">The HVO of the writing system.</param>
		/// <param name="gloss">The primary gloss.</param>
		/// <param name="description">The description.</param>
		/// <param name="seeAlso">Alternate glosses, separated by semi-colons.</param>
		/// ------------------------------------------------------------------------------------
		private void SetLocalizedInfo(IChkTerm term, int termId, int hvoWs, string gloss,
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
