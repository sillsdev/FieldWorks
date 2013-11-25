// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeScrNoteCategoriesInit.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Initializes the ScrNoteCategories in the database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeScrNoteCategoriesInit
	{
		/// <summary>The FDO Scripture object which will own the new note categories</summary>
		protected IScripture m_scr;
		/// <summary>The progress dialog (may be null)</summary>
		protected IProgress m_progressDlg;
		/// <summary>The XmlNode from which to get the note category info</summary>
		protected XmlNode m_categories;

		private ITsStrFactory m_strFactory = TsStrFactoryClass.Create();
		private string m_wsId;
		private int m_defUserWs;
		private IFdoServiceLocator m_servLoc;
		private string m_fallbackUserWsId;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeScrNoteCategoriesInit"/> class.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="scr">The scripture object.</param>
		/// <param name="categories">The categories.</param>
		/// ------------------------------------------------------------------------------------
		protected TeScrNoteCategoriesInit(IProgress progressDlg, IScripture scr,
			XmlNode categories)
		{
			m_scr = scr;
			m_servLoc = scr.Cache.ServiceLocator;
			m_progressDlg = progressDlg;
			m_categories = categories;
			m_defUserWs = m_servLoc.WritingSystemManager.UserWs;
			m_wsId = m_servLoc.WritingSystemManager.UserWritingSystem.Id;
			m_fallbackUserWsId = WritingSystemServices.FallbackUserWsId;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the current Scripture Note categories list version in the Db doesn't match that of
		/// the current XML file, update the DB.
		/// </summary>
		/// <param name="lp">The language project</param>
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureCurrentScrNoteCategories(ILangProject lp,
			IProgress existingProgressDlg)
		{
			XmlNode scrNoteCategories = LoadScrNoteCategoriesDoc();
			IScripture scr = lp.TranslatedScriptureOA;
			Guid newVersion = Guid.Empty;
			try
			{
				XmlNode version = scrNoteCategories.Attributes.GetNamedItem("version");
				newVersion = new Guid(version.Value);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation("List version attribute invalid in TeScrNoteCategories.xml", e);
			}
			if (scr.NoteCategoriesOA == null || newVersion != scr.NoteCategoriesOA.ListVersion)
			{
				using (var dlg = new ProgressDialogWithTask(existingProgressDlg))
				{
					dlg.RunTask(CreateScrNoteCategories, scrNoteCategories, scr);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the scripture note categories.
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: first parameter is a XmlNode with the
		/// notes categories, second parameter is the scripture object.</param>
		/// <returns>Always <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private static object CreateScrNoteCategories(IThreadedProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 2);
			XmlNode scrNoteCategories = (XmlNode)parameters[0];
			IScripture scr = (IScripture)parameters[1];

			TeScrNoteCategoriesInit noteCategoriesInitializer =
				new TeScrNoteCategoriesInit(dlg, scr, scrNoteCategories);
			noteCategoriesInitializer.CreateScrNoteCategories();

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the ScrNoteCategories XML file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static XmlNode LoadScrNoteCategoriesDoc()
		{
			try
			{
				using (var xmlTextReader = new XmlTextReader(Path.Combine(DirectoryFinder.FWCodeDirectory,
					"Translation Editor/TeScrNoteCategories.xml")))
				{
					XmlReaderSettings settings = new XmlReaderSettings();
					settings.ValidationType = ValidationType.DTD;
					using (XmlReader reader = XmlReader.Create(xmlTextReader, settings))
					{
						XmlDocument doc = new XmlDocument();
						doc.Load(reader);

						XmlNode noteCategories = doc.SelectSingleNode("TEScrNoteCategories");
						XmlNode DtdVersion = noteCategories.Attributes.GetNamedItem("DTDver");
						string sDtdRequiredVersion = "BE90BB67-8F02-4241-A39A-78181C2AE523";
						if (DtdVersion == null || DtdVersion.Value != sDtdRequiredVersion)
							throw new Exception("TeScrNoteCategories.xml conforms to a DTD which is incompatible with this version of TE." +
								Environment.NewLine + "This version of TE requires version " + sDtdRequiredVersion + ".");

						return noteCategories;
					}
				}
			}
			catch (XmlSchemaException e)
			{
				ReportInvalidInstallation(e.Message, e);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation("File TeScrNoteCategories.xml could not be loaded." + Environment.NewLine + e.Message, e);
			}
			return null; // Can't actually get here. If you're name is Tim, tell it to the compiler.
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create factory note categories from the TE ScrNoteCategories XML file.
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// <param name="scr">The Scripture</param>
		/// -------------------------------------------------------------------------------------
		public static void CreateFactoryScrNoteCategories(IProgress progressDlg, IScripture scr)
		{
			TeScrNoteCategoriesInit noteCategoriesInitializer = new TeScrNoteCategoriesInit(progressDlg,
				scr, LoadScrNoteCategoriesDoc());
			noteCategoriesInitializer.CreateScrNoteCategories();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the Scr notes categories from the given XML document
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected virtual void CreateScrNoteCategories()
		{
			// Save the previous categories for all of the notes.
			Dictionary<int, List<string>> notesToCategories = CreateNoteToCategoryMap();

			m_scr.NoteCategoriesOA = m_servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			m_scr.NoteCategoriesOA.ItemClsid = CmPossibilityTags.kClassId;
			m_scr.NoteCategoriesOA.WsSelector = WritingSystemServices.kwsAnals;
			m_scr.NoteCategoriesOA.Name.set_String(m_defUserWs,
				m_strFactory.MakeString(TeResourceHelper.GetResourceString("kstidScrNoteCategoriesListName"),
				m_defUserWs));


			XmlNodeList scrNoteCategoriesList = m_categories.SelectNodes("category");

			string message = null;
			if (m_progressDlg != null)
			{
				m_progressDlg.Title = TeResourceHelper.GetResourceString("kstidLoadingNoteCategoriesCaption");
				m_progressDlg.Message = string.Empty;
				m_progressDlg.Position = 0;
				m_progressDlg.Minimum = 0;
				m_progressDlg.Maximum = scrNoteCategoriesList.Count;
				message = TeResourceHelper.GetResourceString("kstidLoadNoteCategoryInDBStatus");
			}

			string localeXpath = "name[@wsId='" + m_wsId + "']";
			string fallbackLocaleXpath = "name[@wsId='" + m_fallbackUserWsId + "']";

			// Load all of the categories from the XML document
			int index = 0;
			foreach (XmlNode noteCategoryNode in scrNoteCategoriesList)
			{
				// Update dialog message.
				if (m_progressDlg != null)
				{
					XmlNode node = noteCategoryNode.SelectSingleNode(localeXpath);
					if (node == null)
						node = noteCategoryNode.SelectSingleNode(fallbackLocaleXpath);

					if (node != null)
						m_progressDlg.Message = string.Format(message, node.InnerText);
				}

				CreateNoteCategory(m_scr.NoteCategoriesOA, index++, noteCategoryNode);

				if (m_progressDlg != null)
					m_progressDlg.Step(1);
			}

			m_scr.NoteCategoriesOA.ListVersion =
				new Guid(m_categories.Attributes.GetNamedItem("version").Value);

			// Attempt to map the previous categories of any notes to the new categories.
			ConnectNotesToNewCategories(notesToCategories);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a map from the note HVOs to the full category path name.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private Dictionary<int, List<string>> CreateNoteToCategoryMap()
		{
			Dictionary<int, List<string>> map = new Dictionary<int, List<string>>();

			foreach (IScrBookAnnotations bookNotes in m_scr.BookAnnotationsOS)
			{
				foreach (IScrScriptureNote note in bookNotes.NotesOS)
				{
					List<string> categories = null;
					foreach (ICmPossibility category in note.CategoriesRS)
					{
						if (categories == null)
							categories = new List<string>();
						categories.Add(category.NameHierarchyString);
					}

					if (categories != null)
						map[note.Hvo] = categories;
				}
			}

			return map;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Connects the notes to new categories using a map created before the new categories
		/// were loaded. If the note category does not already exist, it will create the
		/// category.
		/// </summary>
		/// <param name="map">The map (for each writing system) from the note id to the
		/// full string path of the category name.</param>
		/// <remarks>Limitation: If any category is renamed, any notes that used the previous
		/// name will continue to use the old category. To fix this problem, we would need to
		/// specify in the category file (for each category with a changed name) how the old
		/// category maps to its renamed/relocated category).</remarks>
		/// ------------------------------------------------------------------------------------
		private void ConnectNotesToNewCategories(Dictionary<int, List<string>> map)
		{
			foreach (int noteHvo in map.Keys)
			{
				IScrScriptureNote note = m_scr.Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(noteHvo);
				note.CategoriesRS.Clear(); // Just in case

				List<string> strCategories = map[noteHvo];
				foreach (string strCategory in strCategories)
				{
					note.CategoriesRS.Add(m_scr.NoteCategoriesOA.FindOrCreatePossibility(
						strCategory, m_scr.Cache.DefaultAnalWs));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in a note category node in the database from an XML node
		/// </summary>
		/// <param name="owner">owner. Must be a CmPossibilityList or CmPossibility</param>
		/// <param name="index">0-based position in the list of possibilities</param>
		/// <param name="node">XML node to get info from</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void CreateNoteCategory(ICmObject owner, int index, XmlNode node)
		{
			if (owner is ICmPossibilityList || owner is ICmPossibility)
			{
				ICmPossibility category = m_servLoc.GetInstance<ICmPossibilityFactory>().Create();
				if (owner is ICmPossibilityList)
					((ICmPossibilityList)owner).PossibilitiesOS.Add(category);
				else
					((ICmPossibility)owner).SubPossibilitiesOS.Add(category);

				SetMultiUnicodeAlternatives(category.Name, node.SelectNodes("name"));
				SetMultiUnicodeAlternatives(category.Abbreviation, node.SelectNodes("abbreviation"));

				// get the description for the note category
				XmlNodeList descNodes = node.SelectNodes("description");
				if (descNodes != null)
				{
					foreach (XmlNode descNode in descNodes)
					{
						int ws = GetWs(descNode.Attributes);
						string alternative = descNode.InnerText;
						if (ws > 0 && alternative != null && alternative != string.Empty)
							category.Description.set_String(ws, m_strFactory.MakeString(alternative, ws));
						// REVIEW: What should we do when the writing system is not defined in the database?
					}
				}

				// create note categories for any subcategories
				int subIndex = 0;
				foreach (XmlNode categoryNode in node.SelectNodes("category"))
					CreateNoteCategory(category, subIndex++, categoryNode);
			}
			else
				throw new ArgumentException("Wrong class of owner");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set string values for the given property for each writing system represented in the
		/// nodelist
		/// </summary>
		/// <param name="multiUnicodeproperty">A MultiUnicodeAccessor representing the property
		/// whose value is to be set</param>
		/// <param name="nodes">An XmlNodeList with the strings in one or more writing systems
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void SetMultiUnicodeAlternatives(IMultiUnicode multiUnicodeproperty,
			XmlNodeList nodes)
		{
			foreach (XmlNode node in nodes)
			{
				int ws = GetWs(node.Attributes);
				string alternative = node.InnerText;
				if (ws > 0 && alternative != null && alternative != string.Empty)
					multiUnicodeproperty.set_String(ws, m_strFactory.MakeString(alternative, ws));
				// REVIEW: What should we do when the writing system is not defined in the database?
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ws value (hvo) from the writing system identifier contained in the given
		/// attributes
		/// </summary>
		/// <param name="attribs">Collection of attributes that better have a "wsId"
		/// attribute</param>
		/// <returns></returns>
		/// -------------------------------------------------------------------------------------
		private int GetWs(XmlAttributeCollection attribs)
		{
			string wsId = attribs.GetNamedItem("wsId").Value;
			if (string.IsNullOrEmpty(wsId))
				return 0;

			return m_scr.Services.WritingSystemManager.GetWsFromStr(wsId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message)
		{
			ReportInvalidInstallation(message, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// <param name="e">Optional inner exception</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message, Exception e)
		{
#if !DEBUG
			message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
			throw new InstallationException(message, e);
		}
	}
}
