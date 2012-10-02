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
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using System.Xml.Serialization;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.Framework;
using System.Threading;
using SIL.Utils;
using System.Text;
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
		private ITsStrFactory m_strFactory = TsStrFactoryClass.Create();
		private int m_wsDefault;
		private int m_wsGreek;
		private int m_wsHebrew;
		private SqlConnection m_conn = null;
		private IOleDbEncap m_oleDbEncap = null;
		private readonly uint uintSize = (uint)Marshal.SizeOf(typeof(int));
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeKeyTermsInit"/> class.
		/// </summary>
		/// <param name="scr">The Scripture object.</param>
		/// ------------------------------------------------------------------------------------
		protected TeKeyTermsInit(IScripture scr)
		{
			m_scr = scr;
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
		/// Gets the DB object which owns the CmResource corresponding to the settings file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override ICmObject ResourceOwner
		{
			get { return m_scr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid of the property in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int ResourcesFlid
		{
			get { return (int)Scripture.ScriptureTags.kflidResources; }
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
		public static void EnsureCurrentKeyTerms(ILangProject lp, IAdvInd4 existingProgressDlg)
		{
			TeKeyTermsInit keyTermsInit = new TeKeyTermsInit(lp.TranslatedScriptureOA);
			keyTermsInit.EnsureCurrentResource(existingProgressDlg);
			keyTermsInit.DeleteOldKeyTerms();
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
		protected override void ProcessResources(ProgressDialogWithTask dlg, IAdvInd4 progressDlg,
			BiblicalTermsList doc)
		{
			dlg.RunTask(progressDlg, true, new BackgroundTaskInvoker(CreateKeyTerms), doc);
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
				throw new Exception(message, e);
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
				throw new Exception(message, e);
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
		public static void CreateKeyTerms(IAdvInd4 dlg, ICmPossibilityList keyTermsList)
		{
			FdoCache cache = keyTermsList.Cache;

			BiblicalTermsList list = DeserializeBiblicalTermsFile(
				DirectoryFinder.FWCodeDirectory + @"\BiblicalTerms.xml");

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
		protected object CreateKeyTerms(IAdvInd4 progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			ICmPossibilityList oldList = m_scr.Cache.LangProject.KeyTermsList;
			Debug.Assert(oldList.PossibilitiesOS.Count > 0, "I (TomB) doubt it really matters if this fails (though it's never been tried), but theoretically I think this should never happen since all released versions of TE have had key terms. If this assertion fails, chekc to make sure the biblical terms list was created correctly and try to figure out how this DB came into existence.");
			// If there is an "oldList," it will have the correct (new) GUID, so we need to set
			// it now to be the "old" list so we can find it later in DeleteOldKeyTerms.
			m_scr.Cache.SetGuidProperty(oldList.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				LangProject.kguidOldKeyTermsList);
			// The following will cause a new list to get created with the correct GUID.
			ICmPossibilityList newList = m_scr.Cache.LangProject.KeyTermsList;
			LoadKeyTerms(progressDlg, oldList, newList,
				(BiblicalTermsList)parameters[0]);
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
		protected void LoadKeyTerms(IAdvInd4 dlg, ICmPossibilityList oldKtPossList,
			ICmPossibilityList biblicalTermsPossList, BiblicalTermsList list)
		{
			if (list.Version != new Guid("00FEA689-3B63-4bd4-B640-7262A274D1A8"))
				ReportInvalidInstallation("This version of Translation Editor only supports BiblicalTerms.xml version '00FEA689-3B63-4bd4-B640-7262A274D1A8' (see TE-2901).");
			FdoCache cache = m_scr.Cache;
			List<BiblicalTermsLocalization> localizations = GetLocalizations(cache);
			// Prevent creation of undo task.
			using (new SuppressSubTasks(cache))
			{
				LoadKeyTerms(dlg, oldKtPossList, biblicalTermsPossList, list, localizations);
				cache.VwCacheDaAccessor.ClearInfoAbout(biblicalTermsPossList.Hvo,
					VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);
			}
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
		protected void LoadKeyTerms(IAdvInd4 dlg, ICmPossibilityList oldKtPossList,
			ICmPossibilityList biblicalTermsPossList,
			BiblicalTermsList list, List<BiblicalTermsLocalization> localizations)
		{
			FdoCache cache = biblicalTermsPossList.Cache;
			Debug.Assert(cache != null);
			m_oleDbEncap = cache.DatabaseAccessor;
			Debug.Assert(m_oleDbEncap != null);

			bool fNewTransaction = false;
			string sSavePointName = string.Empty;
			if (m_oleDbEncap != null && !m_oleDbEncap.IsTransactionOpen())
			{
				m_oleDbEncap.SetSavePointOrBeginTrans(out sSavePointName);
				fNewTransaction = true;
			}

			try
			{
				EnsureGreekAndHebrewWsExist(cache);

				m_wsDefault = cache.LanguageEncodings.GetWsFromIcuLocale("en");

				Dictionary<string, CmPossibility> categories = new Dictionary<string, CmPossibility>(5);
				List<Term> terms = list.KeyTerms;
				string message = null;
				if (dlg != null)
				{
					dlg.Position = 0;
					dlg.SetRange(0, terms.Count +
						((oldKtPossList != null) ? oldKtPossList.PossibilitiesOS.Count : 0));
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
					{
						dlg.Message = string.Format(message, term.Gloss);
					}

					CmPossibility cat;
					if (!categories.TryGetValue(term.Category, out cat))
					{
						cat = new CmPossibility();
						biblicalTermsPossList.PossibilitiesOS.Append(cat);
						cat.Abbreviation.SetAlternative(term.Category, m_wsDefault);
						foreach (BiblicalTermsLocalization loc in localizations)
						{
							string name = loc.GetCategoryName(term.Category);
							if (name != null)
								cat.Name.SetAlternative(name, loc.WritingSystemHvo);
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
			catch
			{
				if(m_oleDbEncap != null && fNewTransaction)
					m_oleDbEncap.RollbackSavePoint(sSavePointName);
				throw;
			}
			if (m_oleDbEncap != null && fNewTransaction)
				m_oleDbEncap.CommitTrans();
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
			FdoOwningSequence<ICmPossibility> oldTerms, IAdvInd4 dlg)
		{
			foreach (ICmPossibility poss in oldTerms)
			{
				// TE-7697, oldTerms contained something that wasn't a IChkTerm, so changed
				// using IChkTerm as loop variable to explicit cast and check for failure
				IChkTerm term = poss as IChkTerm;
				if (term == null)
				{
					Debug.Fail("TeKeyTermInit.CopyRenderingsFromOldCheckRefs - invalid item in oldTerms list");
					continue;
				}
				foreach (IChkRef oldChkRef in term.OccurrencesOS)
				{
					if (oldChkRef.Status != KeyTermRenderingStatus.Unassigned)
					{
						foreach (IChkRef newChkRef in ChkRefMatcher.FindCorrespondingChkRefs(
							newKeyTerms, oldChkRef))
						{
							newChkRef.Status = oldChkRef.Status;
							newChkRef.RenderingRAHvo = oldChkRef.RenderingRAHvo;
							if (newChkRef.RenderingRAHvo > 0)
							{
								ChkTerm owningTerm = new ChkTerm(newChkRef.Cache, newChkRef.OwnerHVO);
								bool fRenderingAlreadyInCollection = false;
								foreach (IChkRendering rendering in owningTerm.RenderingsOC)
								{
									if (rendering.SurfaceFormRAHvo == newChkRef.RenderingRAHvo)
									{
										fRenderingAlreadyInCollection = true;
										break;
									}
								}
								if (!fRenderingAlreadyInCollection)
								{
									ChkRendering rendering = new ChkRendering();
									owningTerm.RenderingsOC.Add(rendering);
									rendering.SurfaceFormRA = newChkRef.RenderingRA;
								}
							}
						}
					}
				}
				if (dlg != null)
					dlg.Step(1);
				CopyRenderingsFromOldCheckRefs(newKeyTerms, term.SubPossibilitiesOS, null);
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
		private static List<BiblicalTermsLocalization> GetLocalizations(FdoCache cache)
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
					throw new Exception(message);
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
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			m_wsGreek = EnsureWritingSystemExists(cache, wsf, "grc", "grk", "Classical Greek");
			m_wsHebrew = EnsureWritingSystemExists(cache, wsf, "hbo", "heb", "Ancient Hebrew");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the given writing system exists.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// <param name="sIcuLocale">The ICU locale.</param>
		/// <param name="sAbbrev">The abbreviation.</param>
		/// <param name="sName">Name of the writing system.</param>
		/// <returns>The HVO of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		private int EnsureWritingSystemExists(FdoCache cache, ILgWritingSystemFactory wsf,
			string sIcuLocale, string sAbbrev, string sName)
		{
			int wsHvo = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sIcuLocale);
			if (wsHvo > 0)
				return wsHvo;

			// This finds or creates a writing system for the given key.
			IWritingSystem iws = cache.LanguageWritingSystemFactoryAccessor.get_Engine(sIcuLocale);
			cache.ResetLanguageEncodings();
			wsHvo = iws.WritingSystem;
			Debug.Assert(wsHvo >= 1);
			ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(cache, wsHvo);
			lgws.Abbr.UserDefaultWritingSystem = sAbbrev;
			lgws.Name.UserDefaultWritingSystem = sName;
			lgws.ICULocale = sIcuLocale;
			return wsHvo;
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
		private void AddTerm(CmPossibility owner, Term term,
			List<BiblicalTermsLocalization> localizations)
		{
			int hvoWs = (term.Language == "Hebrew") ? m_wsHebrew : m_wsGreek;
			// Strip off any sense numbers
			string lemma = term.Lemma.TrimEnd(
				'-', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
			int hvoChkTerm = CreateChkTerm(term.Id, hvoWs, lemma, term.Including, owner);
			Debug.Assert(hvoChkTerm > 0);

			foreach (BiblicalTermsLocalization loc in localizations)
			{
				TermLocalization termLoc = loc.FindTerm(term.Id);
				if (termLoc != null)
				{
					// First "gloss" is the primary one and will become the name of the
					// possibility. Susequent glosses will be stored in the SeeAlso field.
					string[] glosses = termLoc.Gloss.Split(new char[] { ';' }, 2);
					SetLocalizedInfo(hvoChkTerm, term.Id, loc.WritingSystemHvo,
						glosses[0].Trim(), termLoc.DescriptionText,
						(glosses.Length == 2) ? glosses[1].Trim() : null);
				}
			}

			// If there are references on this node then add them to the keyterm node
			AddRefsToKeyTerm(term.Id, hvoChkTerm, hvoWs, term.References,
				term.Form ?? lemma);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add references to a sense or keyterm node in the database from XML data.
		/// </summary>
		/// <param name="termId">The term ID.</param>
		/// <param name="hvoChkTerm">HVO of the ChkTerm</param>
		/// <param name="hvoWs">The HVO of the original-language writing system.</param>
		/// <param name="references">List of long integers representing BBCCCVVV references plus
		/// two digits indicating the number of the word in the verse in the original text</param>
		/// <param name="keyword">The specific Greek or Hebrew word(s) in the original text,
		/// though not necessarily the surface (i.e., inflected) form.</param>
		/// ------------------------------------------------------------------------------------
		private void AddRefsToKeyTerm(int termId, int hvoChkTerm, int hvoWs,
			List<long> references, string keyword)
		{
			// Get all of the ref items from the sense node
			foreach (long reference in references)
			{
				int scrRef = (int)(reference / 100); // Get rid of last two digits
				int location = (int)(reference % 100);
				CreateChkRef(termId, hvoWs, keyword, scrRef, location, hvoChkTerm);
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
		/// <returns>ID of new ChkTerm</returns>
		/// ------------------------------------------------------------------------------------
		private int CreateChkTerm(int termId, int hvoWs, string name, string description,
			CmPossibility owner)
		{
			IOleDbCommand cmd;
			int hvoNewChkTerm = -1;
			m_oleDbEncap.CreateCommand(out cmd);
			try
			{
				StringBuilder sCmdBldr = new StringBuilder("exec MakeObj_ChkTerm ");
				sCmdBldr.AppendFormat("@CmPossibility_Name_ws = {0}, @CmPossibility_Name_txt = ?, ",
					hvoWs);
				uint iParam = 1;
				SetStringParameter(cmd, iParam++, name);
				sCmdBldr.AppendFormat("@ChkTerm_TermId = {0}, @Owner = {1}, ", termId, owner.Hvo);
				sCmdBldr.AppendFormat("@OwnFlid = {0}, @NewObjId = ? output, @NewObjGuid = ? output",
					(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities);

				cmd.SetParameter(iParam++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[1] { 0 }, uintSize);

				cmd.SetParameter(iParam++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_GUID, new uint[1] { 0 },
					(uint)Marshal.SizeOf(typeof(Guid)));

				if (description != null)
					description = description.Trim();

				if (!String.IsNullOrEmpty(description))
				{
					sCmdBldr.AppendFormat(", @CmPossibility_Description_ws = {0}, @CmPossibility_Description_txt = ?, @CmPossibility_Description_fmt = ?",
						hvoWs);
					SetFormattedStringParameters(cmd, ref iParam, description, hvoWs);
				}

				cmd.ExecCommand(sCmdBldr.ToString(), (int)SqlStmtType.knSqlStmtNoResults);
				bool fIsNull;
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					cmd.GetParameter(2, rgHvo, uintSize, out fIsNull);
					if (!fIsNull)
						hvoNewChkTerm = DbOps.IntFromStartOfUintArrayPtr(rgHvo);
					if (hvoNewChkTerm < 0)
						throw new Exception("Could not create an object for Checking Term: " + termId.ToString());
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref cmd);
			}

			return hvoNewChkTerm;
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Recursively collects a list of ids of any existing terms that do not have vernacular
		///// equivalents assigned for any of their occurrences.
		///// </summary>
		///// <param name="hvoOwner">The hvo owner.</param>
		///// <param name="cache">The cache.</param>
		///// ------------------------------------------------------------------------------------
		//private Set<int> GetUnrenderedTerms(int hvoOwner, FdoCache cache)
		//{
		//    SqlCommand cmd = m_conn.CreateCommand();
		//cmd.Transaction = m_trans;
		//
		//    //PROBLEM: this will delete any ChkTerms that have any unrendered ChkRefs. We only
		//    //    want to delete the ones that have ONLY undendered ChkRefs (and probably also
		//    //        any unrendered ChkRefs, too)
		//
		//    cmd.CommandText = "SELECT ChkTerm_.id from ChkTerm_ " +
		//        "LEFT OUTER JOIN ChkTerm_Occurrences o on o.Src = ChkTerm_.id " +
		//        "LEFT OUTER JOIN ChkRef r on r.id = o.Dst " +
		//        "WHERE (r.Status = 0 OR r.Rendering IS NULL) " +
		//        "AND ChkTerm_.Owner$ = " + hvoOwner;
		//
		//    SqlDataReader reader = null;
		//    Set<int> idsToDelete = new Set<int>();
		//    try
		//    {
		//        reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
		//        while (reader.Read())
		//        {
		//            Debug.Assert(reader.FieldCount == 1);
		//            Debug.Assert(!reader.IsDBNull(0));
		//            idsToDelete.Add(reader.GetInt32(0));
		//        }
		//    }
		//    finally
		//    {
		//        if (reader != null)
		//            reader.Close();
		//    }
		//    if (idsToDelete.Count > 0)
		//    {
		//        Set<int> subIdsToDelete = new Set<int>();
		//        foreach (int hvo in idsToDelete)
		//        {
		//            subIdsToDelete.AddRange(GetUnrenderedTerms(hvo, cache));
		//        }
		//        idsToDelete.AddRange(subIdsToDelete);
		//    }
		//    return idsToDelete;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds Gloss, Definition and SeeAlso info to a Check Term for the given writing system
		/// </summary>
		/// <param name="hvoChkTerm">HVO of the ChkTerm</param>
		/// <param name="termId">The term ID.</param>
		/// <param name="hvoWs">The HVO of the writing system.</param>
		/// <param name="gloss">The primary gloss.</param>
		/// <param name="description">The description.</param>
		/// <param name="seeAlso">Alternate glosses, separated by semi-colons.</param>
		/// ------------------------------------------------------------------------------------
		private void SetLocalizedInfo(int hvoChkTerm, int termId, int hvoWs, string gloss,
			string description, string seeAlso)
		{
			IOleDbCommand cmd;
			m_oleDbEncap.CreateCommand(out cmd);
			try
			{
				StringBuilder sCmdBldr = new StringBuilder("DECLARE @rc int; EXEC @rc = SetChkTermLocalizedInfo @ObjId = ");
				sCmdBldr.Append(hvoChkTerm);
				sCmdBldr.Append(", @WritingSystem = ");
				sCmdBldr.Append(hvoWs);
				sCmdBldr.Append(", @Gloss = ?");
				uint iParam = 1;
				SetStringParameter(cmd, iParam++, gloss);

				if (description != null)
					description = description.Trim();

				if (!String.IsNullOrEmpty(description))
				{
					sCmdBldr.Append(", @Description_txt = ?, @Description_fmt = ?");
					SetFormattedStringParameters(cmd, ref iParam, description, hvoWs);
				}
				if (!String.IsNullOrEmpty(seeAlso))
				{
					sCmdBldr.Append(", @SeeAlso = ?");
					SetStringParameter(cmd, iParam++, seeAlso);
				}

				sCmdBldr.Append("; SELECT @rc");

				string sCmd = sCmdBldr.ToString();
				cmd.ExecCommand(sCmd, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);

				if (GetCommandResult(cmd) != 0)
				{
					throw new Exception("Stored procedure SetChkTermLocalizedInfo returned a result of 0." +
						Environment.NewLine + "Command: " + sCmd);
				}
			}
			catch (Exception e)
			{
				throw new Exception("Could not set localized info for Checking Term: " + termId.ToString(), e);
			}
			finally
			{
				DbOps.ShutdownODC(ref cmd);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a ChkRef object
		/// </summary>
		/// <param name="termId">The term id (used for error reporting)</param>
		/// <param name="hvoWs">The HVO of the original-language writing system.</param>
		/// <param name="keyword">The specific Greek or Hebrew word(s) in the original text,
		/// though not necessarily the surface (i.e., inflected) form.</param>
		/// <param name="reference">Scripture reference in BBCCCVVV format</param>
		/// <param name="location">The 1-based index of the word in the verse in the original
		/// language</param>
		/// <param name="hvoOwner">HVO of the ChkTerm that will own the ChkRef</param>
		/// ------------------------------------------------------------------------------------
		private void CreateChkRef(int termId, int hvoWs, string keyword, int reference, int location, int hvoOwner)
		{
			IOleDbCommand cmd;
			m_oleDbEncap.CreateCommand(out cmd);
			try
			{
				StringBuilder sCmdBldr = new StringBuilder("DECLARE @rc int; EXEC @rc = MakeObj_ChkRef @ChkRef_Ref = ");
				sCmdBldr.Append(reference);
				sCmdBldr.Append(", @ChkRef_KeyWord = ?, @ChkRef_KeyWord_fmt = ?");
				uint iParam = 1;
				SetFormattedStringParameters(cmd, ref iParam, keyword, hvoWs);
				sCmdBldr.Append(", @ChkRef_Location = ");
				sCmdBldr.Append(location);
				sCmdBldr.Append(", @Owner = ");
				sCmdBldr.Append(hvoOwner);
				sCmdBldr.Append(", @OwnFlid = ");
				sCmdBldr.Append((int)ChkTerm.ChkTermTags.kflidOccurrences);
				sCmdBldr.Append(", @NewObjId = ? output, @NewObjGuid = ? output");

				cmd.SetParameter(iParam++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[1] { 0 }, uintSize);

				cmd.SetParameter(iParam++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
					null, (ushort)DBTYPEENUM.DBTYPE_GUID, new uint[1] { 0 },
					(uint)Marshal.SizeOf(typeof(Guid)));

				sCmdBldr.Append("; SELECT @rc");

				cmd.ExecCommand(sCmdBldr.ToString(), (int)SqlStmtType.knSqlStmtSelectWithOneRowset);

				if (GetCommandResult(cmd) > 0)
				{
					ScrReference scrRef = new ScrReference(reference, Paratext.ScrVers.English);
					throw new Exception("Could not create an item for Checking Term Reference: " +
						scrRef.AsString + ", Term ID " + termId.ToString());
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref cmd);
			}
		}
		#endregion

		#region Methods for passing parameters to and getting results from OLE DB commands
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the format bytes for a string value.
		/// </summary>
		/// <param name="source">The string.</param>
		/// <param name="ws">The HVO of the writing system.</param>
		/// <param name="formatLength">Length of format string in bytes</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private uint[] GetStringFormat(string source, int ws, out int formatLength)
		{
			ITsString tss = m_strFactory.MakeString(source, ws);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(byte)))
			{
				formatLength = tss.SerializeFmtRgb(arrayPtr, 1000);
				int nbrUints = (int) (formatLength/uintSize) + 1;
				return (uint[])MarshalEx.NativeToArray(arrayPtr, nbrUints, typeof(uint));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets two SQL parameters (iParamS and iParamF) to the string value and bytes needed
		/// to represent the given value of a formatted string in the given writing system.
		/// </summary>
		/// <param name="cmd">The OLE DB command object.</param>
		/// <param name="iParam">In: the 1-based index of the string parameter in the SQL
		/// command. Out: the 1-based index of the string parameter for any subsequent parameter
		/// later in the SQL command (i.e., the incoming value will be incremented by 2 in this
		/// method)</param>
		/// <param name="value">The string value of the field whose format string is being set.
		/// </param>
		/// <param name="hvoWs">The HVO of the writing system which should be used for
		/// formatting the string value.</param>
		/// ------------------------------------------------------------------------------------
		private void SetFormattedStringParameters(IOleDbCommand cmd, ref uint iParam,
			string value, int hvoWs)
		{
			SetStringParameter(cmd, iParam++, value);

			int formatLength;
			uint[] format = GetStringFormat(value, hvoWs, out formatLength);
			cmd.SetParameter(iParam++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
				null, (ushort)DBTYPEENUM.DBTYPE_BYTES, format, (uint) formatLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the given parameter to the given string value.
		/// </summary>
		/// <param name="cmd">The OLE DB command object.</param>
		/// <param name="iParam">The 1-based index of the parameter to the SQL command.</param>
		/// <param name="value">The string value of the parameter.</param>
		/// ------------------------------------------------------------------------------------
		private void SetStringParameter(IOleDbCommand cmd, uint iParam, string value)
		{
			cmd.SetStringParameter(iParam, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
				null /*flags*/, value, (uint)value.Length); // despite doc, impl makes clear this is char count
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the command result, which should be the only value in the first (and only)
		/// rowset.
		/// </summary>
		/// <param name="cmd">The OLE DB command object.</param>
		/// ------------------------------------------------------------------------------------
		private int GetCommandResult(IOleDbCommand cmd)
		{
			cmd.GetRowset(0);
			bool fMoreRows;
			cmd.NextRow(out fMoreRows);
			bool fIsNull;
			uint cbSpaceTaken;
			int nResult = -1;
			using (ArrayPtr rgResult = MarshalEx.ArrayToNative(1, typeof(uint)))
			{
				if (fMoreRows)
				{
					cmd.GetColValue(1, rgResult, uintSize, out cbSpaceTaken, out fIsNull, 0);
					if (!fIsNull)
					{
						nResult = DbOps.IntFromStartOfUintArrayPtr(rgResult);
					}
				}
			}
			return nResult;
		}
		#endregion

		#region Methods for deleting the old key terms list asynchronously
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the db connection.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private void OpenDbConnection(FdoCache cache)
		{
			Debug.Assert(m_conn == null);
			string sConnection = "Server=" + cache.ServerName +
				"; Database=" + cache.DatabaseName + "; User ID=FWDeveloper; " +
				"Password=careful; Pooling=false; Asynchronous Processing=true;";
			m_conn = new SqlConnection(sConnection);
			m_conn.Open();

			SqlCommand cmd = m_conn.CreateCommand();
			cmd.CommandText = "SET DEADLOCK_PRIORITY -10;";
			cmd.ExecuteNonQuery();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for and (if found) deletes the old key terms list (in a separate thread).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeleteOldKeyTerms()
		{
			try
			{
				int hvoOldKtPossList = 0;
				foreach (ICmPossibilityList list in m_scr.Cache.LangProject.CheckListsOC)
				{
					if (list.Guid == LangProject.kguidOldKeyTermsList)
					{
						hvoOldKtPossList = list.Hvo;
						break;
					}
				}
				if (hvoOldKtPossList == 0)
				{
					m_scr = null;
					return;
				}

				OpenDbConnection(m_scr.Cache);

				SqlCommand cmd = m_conn.CreateCommand();
				cmd.CommandTimeout = 0;
				cmd.CommandText = string.Format("EXEC DeleteObjects '{0}'", hvoOldKtPossList);
				cmd.BeginExecuteNonQuery(new AsyncCallback(this.DeleteOldKeyTermsAsynch), cmd);
			}
			catch
			{
				// ENHANCE: Deal with the possibility that it fails. We would need to clean up when
				// this DB was opened later.
				if (m_conn != null && m_conn.State != ConnectionState.Closed)
					m_conn.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Callback for deleting the old key terms list on a separate thread. Closes the SQL
		/// connection when the old Key Terms list has been deleted.
		/// </summary>
		/// <param name="result">The resultof the asynchronous SQL call.</param>
		/// ------------------------------------------------------------------------------------
		internal void DeleteOldKeyTermsAsynch(IAsyncResult result)
		{
			SqlCommand cmd = null;
			bool fRetry = false;
			try
			{
				// Retrieve the original command object, passed to this procedure in the
				// AsyncState property of the IAsyncResult parameter.
				cmd = (SqlCommand)result.AsyncState;
				// Despite its name, the following call is what actually kicks off the
				// execution of the SQL command. This statement hangs (on this thread) until
				// execution completes.
				Debug.Assert(cmd.EndExecuteNonQuery(result) > 0);
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				if (!e.Message.Contains("deadlock victim"))
					Debug.Fail(e.Message);
				else
				{
					Debug.Assert(m_scr != null);
					fRetry = !m_scr.Cache.IsDisposed;
				}

				// Because we are now running code in a separate thread,  if we do not handle
				// the exception here, none of our other code catches the exception. Because
				// none of our code is on the call stack in this thread, there is nothing
				// higher up the stack to catch the exception if we do not handle it here.
				// In no case can we simply display the error without executing a delegate.
			}
			finally
			{
				if (m_conn != null)
				{
					m_conn.Close();
					m_conn = null;
				}
			}

			// If we failed because we were chosen as a deadlock victim, we can go ahead
			// and keep trying.
			if (fRetry)
				DeleteOldKeyTerms();
			else
				m_scr = null;
		}
		#endregion
	}
}
