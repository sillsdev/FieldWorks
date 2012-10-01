// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: LangProject.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		LangProject : BaseLanguageProject (formerly AfLpInfo)
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils; // Needed for Set class.
using SIL.FieldWorks.Common.FwUtils; // for LanguageDefinitionFactory
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The LangProject class additions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class LangProject
	{
		private int m_srsFlid;
		private readonly bool m_teInstalled = FwUtils.IsTEInstalled;
		private WritingSystemCollection m_analysisWritingSystems;
		private WritingSystemCollection m_vernacularWritingSystems;
		private WritingSystemList m_currentAnalysisWritingSystems;
		private WritingSystemList m_currentVernacularWritingSystems;
		private WritingSystemList m_currentPronunciationWritingSystems;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the name of the database that stores this project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get { return m_cache.ProjectId.Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the name of the database that stores this project. This is made accessible
		/// as a virtual string property (e.g., for generating the grammar sketch).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString Name
		{
			get { return ShortNameTSS;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return all the WfiWordform objects.  These aren't owned anymore, but FXT export
		/// needs a hook to get at them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "WfiWordform")]
		public IEnumerable<IWfiWordform> AllWordforms
		{
			get
			{
				IWfiWordformRepository repo = Services.GetInstance<IWfiWordformRepository>();
				return repo.AllInstances();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return LinkedFilesRootDir if explicitly set, otherwise FWDataDirectory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ModelProperty(CellarPropertyType.Unicode, 6001042, "string")]
		public string LinkedFilesRootDir
		{
			get
			{
				return String.IsNullOrEmpty(LinkedFilesRootDir_Generated)
					? Path.Combine(m_cache.ProjectId.SharedProjectFolder, DirectoryFinder.ksLinkedFilesDir)
					: DirectoryFinderRelativePaths.GetLinkedFilesFullPathFromRelativePath(LinkedFilesRootDir_Generated,
						m_cache.ProjectId.SharedProjectFolder);
			}
			set
			{
				string relativePath = DirectoryFinderRelativePaths.GetLinkedFilesRelativePathFromFullPath(value,
					m_cache.ProjectId.SharedProjectFolder, ShortName);

				LinkedFilesRootDir_Generated = relativePath;
			}
		}

		/// <summary>
		/// Every picture file's PathNameTSS is typically changed when the LinkedFiles directory changes.
		/// </summary>
		partial void LinkedFilesRootDirSideEffects(string originalValue, string newValue)
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(CmPictureTags.kClassId, "PathNameTSS", false);
			foreach (ICmPicture pict in m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().AllInstances())
			{
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(pict,
					flid, m_cache.MakeUserTss(""), ((CmPicture)pict).PathNameTSS);
			}
		}

		/// <summary>
		/// Virtual list of texts. Replaces TextsOC now that Text objects are unowned.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "Text")]
		public IList<IText> Texts
		{
			get
			{
				// Get regular texts.
				var txtRepo = Cache.ServiceLocator.GetInstance<ITextRepository>();
				return txtRepo.AllInstances().ToList();
			}
		}

		/// <summary>
		/// Virtual list of texts that can be interlinearized.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "StText")]
		public IList<IStText> InterlinearTexts
		{
			get
			{
				// TODO: Move to some Repository
				var stTextIds = (from txt in Texts where txt.ContentsOA != null select txt.ContentsOA).ToList();

				// Get regular texts.

				if (m_teInstalled && TranslatedScriptureOA != null)
				{
					// TE installed, so also get them from Sripture.
					foreach (var book in TranslatedScriptureOA.ScriptureBooksOS)
					{
						foreach (var section in book.SectionsOS)
						{
							if (section.ContentOA != null)
								stTextIds.Add(section.ContentOA);
							if (section.HeadingOA != null)
								stTextIds.Add(section.HeadingOA);
						}
					}
				}

				return stTextIds;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of CmAnnotationDefns that belong to Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFdoOwningSequence<ICmPossibility> ScriptureAnnotationDfns
		{
			get
			{
				// TODO: Move to ICmPossibilityRepository
				foreach (var dfn in AnnotationDefsOA.PossibilitiesOS)
				{
					if (dfn.Guid == CmAnnotationDefnTags.kguidAnnNote)
						return dfn.SubPossibilitiesOS;
				}
				throw new Exception("Scripture annotation definitions are missing from project!");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the agent to assigning "constraint failure" annotations to
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="ApplicationException"/>
		/// ------------------------------------------------------------------------------------
		public ICmAgent ConstraintCheckerAgent
		{
			get
			{
				// TODO: Move to ICmAgentRepository
				// This will do until we decide that we need more than 1 computational agent.
				return DefaultParserAgent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current parser
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="KeyNotFoundException"/>
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultParserAgent
		{
			get
			{
				// TODO: Move to ICmAgentRepository
				Guid agentGuid = CmAgentTags.kguidAgentXAmpleParser; // M3Parser
				if (MorphologicalDataOA.ActiveParser == "HC")
					agentGuid = CmAgentTags.kguidAgentHermitCrabParser; // HCParser

				return Cache.ServiceLocator
					.GetInstance<ICmAgentRepository>()
					.GetObject(agentGuid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get the analyzing agent representing the current user
		/// </summary>
		/// <returns>a CmAgent object</returns>
		/// <exception cref="KeyNotFoundException"/>
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultUserAgent
		{
			get
			{
				// TODO: Move to ICmAgentRepository
				return Cache.ServiceLocator
					.GetInstance<ICmAgentRepository>()
					.GetObject(CmAgentTags.kguidAgentDefUser);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the analyzing agent representing the computer.
		/// Do not use this for the parser;
		/// there is a dedicated agent for that purpose.
		/// </summary>
		/// <returns>a CmAgent Object</returns>
		/// <exception cref="KeyNotFoundException">
		/// There was no default computer agent.
		/// This should be part of the NewLangProj.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public ICmAgent DefaultComputerAgent
		{
			get
			{
				// TODO: Move to ICmAgentRepository
				return Cache.ServiceLocator
					.GetInstance<ICmAgentRepository>()
					.GetObject(CmAgentTags.kguidAgentComputer);
			}
		}



		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Key Terms list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibilityList KeyTermsList
		{
			get
			{
				// TODO: Move to a Repository, probably for ICmPossibilityList.
				foreach (var list in CheckListsOC)
				{
					if (list.Guid == CmPossibilityListTags.kguidChkKeyTermsList)
						return list;
				}

				var servLoc = Cache.ServiceLocator;
				var listFactory = (ICmPossibilityListFactoryInternal)servLoc.GetInstance<ICmPossibilityListFactory>();
				ICmPossibilityList keyTermList = listFactory.Create(
					CmPossibilityListTags.kguidChkKeyTermsList,
					((IDataReader)servLoc.GetInstance<IDataSetup>()).GetNextRealHvo());
				CheckListsOC.Add(keyTermList);
				return keyTermList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a plausible guess for interpreting the "magic" ws as a real ws.  An invalid
		/// "magic" ws returns itself.
		/// </summary>
		/// <param name="wsMagic">The ws magic.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int DefaultWsForMagicWs(int wsMagic)
		{
			switch (wsMagic)
			{
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsFirstAnal:
				case WritingSystemServices.kwsAnalVerns:
				case WritingSystemServices.kwsFirstAnalOrVern:
				case WritingSystemServices.kwsAllReversalIndex:
				case WritingSystemServices.kwsReversalIndex:
					return m_cache.DefaultAnalWs;
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
				case WritingSystemServices.kwsFirstVern:
				case WritingSystemServices.kwsVernAnals:
				case WritingSystemServices.kwsFirstVernOrAnal:
				case WritingSystemServices.kwsPronunciations:
				case WritingSystemServices.kwsPronunciation:
				case WritingSystemServices.kwsVernInParagraph:
					return m_cache.DefaultVernWs;
				default:
					return wsMagic;
			}
		}

		/// <summary>
		/// Return a flat list of all the parts of speech in this language project.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "PartOfSpeech")]
		public List<IPartOfSpeech> AllPartsOfSpeech
		{
			get
			{
				var rgpos = new List<IPartOfSpeech>();
				foreach (IPartOfSpeech pos in PartsOfSpeechOA.PossibilitiesOS)
				{
					rgpos.Add(pos);
					AddSubPartsOfSpeech(pos, rgpos);
				}
				return rgpos;
			}
		}

		private void AddSubPartsOfSpeech(IPartOfSpeech posRoot, List<IPartOfSpeech> rgpos)
		{
			foreach (IPartOfSpeech pos in posRoot.SubPossibilitiesOS)
			{
				rgpos.Add(pos);
				AddSubPartsOfSpeech(pos, rgpos);
			}
		}

		#region Chart template creation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default Constituent Chart template (creating it and any superstructure to hold it as needed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ICmPossibility GetDefaultChartTemplate()
		{
			if (m_cache.LanguageProject.DiscourseDataOA == null
				|| m_cache.LanguageProject.DiscourseDataOA.ConstChartTemplOA == null
				|| m_cache.LanguageProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.Count == 0)
			{
				CreateDefaultTemplate();
			}
			return m_cache.LanguageProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS[0];
		}

		/// <summary>
		/// Create a default template. What happened to the version in the Blank Database?
		/// </summary>
		/// <returns></returns>
		private ICmPossibility CreateDefaultTemplate()
		{
			var doc = new XmlDocument();
			doc.LoadXml(
				"<template name=\"default If you ever get this report it. There is a problem.\">"
				+ "<column name=\"prenuclear\">"
				+ "<column name=\"prenuc1\"/>"
				+ "<column name=\"prenuc2\"/>"
				+ "</column>"
				+ "<column name=\"nucleus\">"
				+ "<column name=\"subject\"/>"
				+ "<column name=\"verb\"/>"
				+ "<column name=\"object\"/>"
				+ "</column>"
				+ "<column name=\"postnuc\"/>"
				+ "</template>");
			return CreateChartTemplate(doc.DocumentElement);
		}

		/// <summary>
		/// Create a CmPossibility based on an XML specification of a constituent chart template.
		/// See CreateDefaultTemplate for an example.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns></returns>
		public ICmPossibility CreateChartTemplate(XmlNode spec)
		{
			// Make sure we have the containing objects; if not create them.
			var dData = m_cache.LanguageProject.DiscourseDataOA;
			if (dData == null)
			{
				dData = new DsDiscourseData();
				m_cache.LanguageProject.DiscourseDataOA = dData;
			}
			// Also make sure it has a templates list
			var templates = dData.ConstChartTemplOA;
			if (templates == null)
			{
				templates = new CmPossibilityList();
				dData.ConstChartTemplOA = templates;
			}
			var template = new CmPossibility();
			m_cache.LanguageProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.Add(template);
			SetNameAndChildColumns(template, spec);
			return template;
		}

		private void SetNameAndChildColumns(ICmPossibility parent, XmlNode spec)
		{
			var defAnalWs = m_cache.DefaultAnalWs;
			parent.Name.set_String(
				defAnalWs,
				Cache.TsStrFactory.MakeString(
					XmlUtils.GetManditoryAttributeValue(spec, "name"),
					defAnalWs));
			foreach (XmlNode child in spec.ChildNodes)
			{
				if (child.Name == "column")
					CreateColumn(parent, child);
			}
		}

		internal ICmPossibility CreateColumn(ICmPossibility parent, XmlNode spec)
		{
			var result = new CmPossibility();
			parent.SubPossibilitiesOS.Add(result);
			SetNameAndChildColumns(result, spec);
			return result;
		}

		#endregion template creation

		#region DefaultChartMarkers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default list of Constituent Chart markers (creating it and any superstructure
		/// to hold it as needed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibility GetDefaultChartMarkers()
		{
			if (m_cache.LangProject.DiscourseDataOA == null
				|| m_cache.LangProject.DiscourseDataOA.ChartMarkersOA == null
				|| m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS.Count == 0)
			{
				MakeDefaultChartMarkers();
			}
			return m_cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[0];
		}

		/// <summary>
		/// Create a default set of markers. What happened to the version in the Blank Database?
		/// </summary>
		/// <returns></returns>
		private ICmPossibilityList MakeDefaultChartMarkers()
		{
			string xml =
				"<list>"
				+ " <item name=\"Group1 If you ever get this report it. There is a problem.\" abbr=\"G1\">"
				+ " <item name=\"Group1.1\" abbr=\"G1.1\">"
				+ " <item name=\"Item1\" abbr=\"I1\"/>"
				+ " </item>"
				+ " </item>"
				+ " <item name=\"Group2\" abbr=\"G2\">"
				+ " <item name=\"Item2\" abbr=\"I2\"/>"
				+ " <item name=\"Item3\" abbr=\"I3\"/>"
				+ " </item>"
				+ "</list>";
			return MakeChartMarkers(xml);
		}

		/// <summary>
		/// Create a CmPossibilityList based on a string of XML specifying Constituent Chart markers.
		/// See MakeDefaultChartMarkers for an example.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public ICmPossibilityList MakeChartMarkers(string xml)
		{
			m_cache.LanguageProject.DiscourseDataOA.ChartMarkersOA = new CmPossibilityList();
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			MakeListXml(m_cache.LanguageProject.DiscourseDataOA.ChartMarkersOA, doc.DocumentElement);
			return m_cache.LanguageProject.DiscourseDataOA.ChartMarkersOA;
		}

		private void MakeListXml(ICmPossibilityList list, XmlElement root)
		{
			foreach (XmlNode item in root)
			{
				var poss = new CmPossibility();
				list.PossibilitiesOS.Add(poss);
				InitItem(item, poss);
			}
		}

		private static void InitItem(XmlNode item, ICmPossibility poss)
		{
			var defAnalWs = poss.Cache.DefaultAnalWs;
			var strFact = poss.Cache.TsStrFactory;

			// Set name property
			poss.Name.set_String(
				defAnalWs,
				strFact.MakeString(
					XmlUtils.GetManditoryAttributeValue(item, "name"),
					defAnalWs));

			// Set Abbreviation.
			var abbr = XmlUtils.GetOptionalAttributeValue(item, "abbr");
			if (String.IsNullOrEmpty(abbr))
				abbr = poss.Name.AnalysisDefaultWritingSystem.Text;
			poss.Abbreviation.set_String(
				defAnalWs,
				strFact.MakeString(
					abbr,
					defAnalWs));

			// Create optional child items.
			foreach (XmlNode subItem in item.ChildNodes)
			{
				var poss2 = new CmPossibility();
				poss.SubPossibilitiesOS.Add(poss2);
				InitItem(subItem, poss2);
			}
		}

		#endregion // DefaultChartMarkers

		#region Default Text Tags

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default list of TextTagging tags (creating it and any superstructure
		/// to hold it as needed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibilityList GetDefaultTextTagList()
		{
			if (m_cache.LangProject.TextMarkupTagsOA == null
				|| m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS.Count == 0)
			{
				MakeDefaultTextTags();
			}
			return m_cache.LangProject.TextMarkupTagsOA;
		}

		private void MakeDefaultTextTags()
		{
			string xml =
				"<taglist name=\"Text Markup TagsIf you ever get this report it. There is a problem.\">"
				+ "<possibility name=\"RRG Semantics\">"
				+ "<subpossibility name=\"ACTOR\" abbreviation=\"ACT\"/>"
				+ "<subpossibility name=\"NON-MACROROLE\" abbreviation=\"NON-MR\"/>"
				+ "<subpossibility name=\"UNDERGOER\" abbreviation=\"UND\"/>"
				+ "</possibility>"
				+ "<possibility name=\"Syntax\">"
				+ "<subpossibility name=\"Noun Phrase\" abbreviation=\"NP\"/>"
				+ "<subpossibility name=\"Verb Phrase\" abbreviation=\"VP\"/>"
				+ "<subpossibility name=\"Adjective Phrase\" abbreviation=\"AdjP\"/>"
				+ "</possibility>"
				+ "</taglist>";
			MakeTextTagsList(xml);
		}

		/// <summary>
		/// Create a CmPossibilityList based on a string of XML specifying Text Tagging tags.
		/// See MakeDefaultTextTags for an example.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public ICmPossibilityList MakeTextTagsList(string xml)
		{
			m_cache.LanguageProject.TextMarkupTagsOA = new CmPossibilityList();
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			MakeListXml(m_cache.LanguageProject.TextMarkupTagsOA, doc.DocumentElement);
			return m_cache.LanguageProject.TextMarkupTagsOA;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the feature type for exception features.  Look in the Feature System.
		/// </summary>
		/// <returns>The feature type for exception features or null if there are none</returns>
		/// ------------------------------------------------------------------------------------
		public IFsFeatStrucType ExceptionFeatureType
		{
			get
			{
				IFsFeatStrucType exceps = null;
				IFsFeatureSystem fsys = MsFeatureSystemOA;
				if (fsys != null)
				{
					foreach (IFsFeatStrucType type in fsys.TypesOC)
						if (type.Name.AnalysisDefaultWritingSystem.Text == "Exception Features") // TODO: this needs to be made general
						{
							exceps = type;
							break;
						}
				}
				return exceps;
			}
		}

		partial void SetDefaultValuesInConstruction()
		{
			m_analysisWritingSystems = new WritingSystemCollection(this, LangProjectTags.kflidAnalysisWss);
			m_vernacularWritingSystems = new WritingSystemCollection(this, LangProjectTags.kflidVernWss);
			m_currentAnalysisWritingSystems = new WritingSystemList(this, LangProjectTags.kflidCurAnalysisWss);
			m_currentVernacularWritingSystems = new WritingSystemList(this, LangProjectTags.kflidCurVernWss);
			m_currentPronunciationWritingSystems = new WritingSystemList(this, LangProjectTags.kflidCurPronunWss);
			m_currentAnalysisWritingSystems.Changed += WritingSystemListChanged;
			m_currentVernacularWritingSystems.Changed += WritingSystemListChanged;
			m_currentPronunciationWritingSystems.Changed += WritingSystemListChanged;
			// Just in the unlikely event...except perhaps in tests...that this gets called when we have an existing cache.
			WritingSystemListChanged(this, new EventArgs());
		}

		void WritingSystemListChanged(object obj, EventArgs args)
		{
			if (m_cache != null)
				m_cache.ResetDefaultWritingSystems();
		}

		/// <summary>
		/// Gets all writing systems.
		/// </summary>
		/// <value>All writing systems.</value>
		public IEnumerable<IWritingSystem> AllWritingSystems
		{
			get
			{
				return AnalysisWritingSystems.Union(VernacularWritingSystems);
			}
		}

		/// <summary>
		/// Gets the analysis writing systems.
		/// </summary>
		/// <value>The analysis writing systems.</value>
		public ICollection<IWritingSystem> AnalysisWritingSystems
		{
			get
			{
				return m_analysisWritingSystems;
			}
		}

		/// <summary>
		/// Gets the vernacular writing systems.
		/// </summary>
		/// <value>The vernacular writing systems.</value>
		public ICollection<IWritingSystem> VernacularWritingSystems
		{
			get
			{
				return m_vernacularWritingSystems;
			}
		}

		/// <summary>
		/// Gets the current analysis writing systems.
		/// </summary>
		/// <value>The current analysis writing systems.</value>
		public IList<IWritingSystem> CurrentAnalysisWritingSystems
		{
			get
			{
				return m_currentAnalysisWritingSystems;
			}
		}

		/// <summary>
		/// Gets the current vernacular writing systems.
		/// </summary>
		/// <value>The current vernacular writing systems.</value>
		public IList<IWritingSystem> CurrentVernacularWritingSystems
		{
			get
			{
				return m_currentVernacularWritingSystems;
			}
		}

		/// <summary>
		/// Gets the current pronunciation writing systems.
		/// </summary>
		/// <value>The current pronunciation writing systems.</value>
		public IList<IWritingSystem> CurrentPronunciationWritingSystems
		{
			get
			{
				return m_currentPronunciationWritingSystems;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given writing system to the current analysis writing systems
		/// and also to the collection of all analysis writing systems if necessary.
		/// </summary>
		/// <param name="ws">The writing system to add.</param>
		/// ------------------------------------------------------------------------------------
		public void AddToCurrentAnalysisWritingSystems(IWritingSystem ws)
		{
			m_analysisWritingSystems.Add(ws);
			m_currentAnalysisWritingSystems.Add(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given writing system to the current vernacular writing systems
		/// and also to the collection of all vernacular writing systems if necessary.
		/// </summary>
		/// <param name="ws">The writing system to add.</param>
		/// ------------------------------------------------------------------------------------
		public void AddToCurrentVernacularWritingSystems(IWritingSystem ws)
		{
			m_vernacularWritingSystems.Add(ws);
			m_currentVernacularWritingSystems.Add(ws);
		}


		/// <summary>
		/// Another way the current analysis writing systems get changed is by directly setting the string.
		/// This defeats the change monitoring built into the WritingSystemsList collection, because it
		/// changes the sequence of values read from the collection, without invoking any method of the collection.
		/// </summary>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		partial void CurAnalysisWssSideEffects(string originalValue, string newValue)
		{
			m_currentAnalysisWritingSystems.RaiseChanged();
			m_cache.ActionHandlerAccessor.AddAction(new UndoWsChangeAction() { Cache = m_cache });
		}

		/// <summary>
		/// Gets the default analysis writing system.
		/// </summary>
		/// <value>The default analysis writing system.</value>
		public IWritingSystem DefaultAnalysisWritingSystem
		{
			get
			{
				return m_currentAnalysisWritingSystems.FirstOrDefault();
			}
			set
			{
				if (DefaultAnalysisWritingSystem == value)
					return;
				if (!m_analysisWritingSystems.Contains(value))
					m_analysisWritingSystems.Add(value);
				m_currentAnalysisWritingSystems.Remove(value);
				m_currentAnalysisWritingSystems.Insert(0, value);
			}
		}

		/// <summary>
		/// Another way the current vernacular writing systems get changed is by directly setting the string.
		/// This defeats the change monitoring built into the WritingSystemsList collection, because it
		/// changes the sequence of values read from the collection, without invoking any method of the collection.
		/// </summary>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		partial void CurVernWssSideEffects(string originalValue, string newValue)
		{
			m_currentVernacularWritingSystems.RaiseChanged();
			m_cache.ActionHandlerAccessor.AddAction(new UndoWsChangeAction() {Cache = m_cache});
		}

		/// <summary>
		/// Class that allows us to clear the WS caches when an action that (might have) changed them is undone or redone.
		/// </summary>
		class UndoWsChangeAction : UndoActionBase
		{
			public FdoCache Cache { get; set; }
			public override bool Undo()
			{
				Cache.ResetDefaultWritingSystems();
				return true;
			}

			public override bool Redo()
			{
				Cache.ResetDefaultWritingSystems();
				return true;
			}
		}

		/// <summary>
		/// Gets the default vernacular writing system.
		/// </summary>
		/// <value>The default vernacular writing system.</value>
		public IWritingSystem DefaultVernacularWritingSystem
		{
			get
			{
				return m_currentVernacularWritingSystems.FirstOrDefault();
			}
			set
			{
				if (DefaultVernacularWritingSystem == value)
					return;
				if (!m_vernacularWritingSystems.Contains(value))
					m_vernacularWritingSystems.Add(value);
				m_currentVernacularWritingSystems.Remove(value);
				m_currentVernacularWritingSystems.Insert(0, value);
			}
		}

		/// <summary>
		/// Another way the current analysis writing systems get changed is by directly setting the string.
		/// This defeats the change monitoring built into the WritingSystemsList collection, because it
		/// changes the sequence of values read from the collection, without invoking any method of the collection.
		/// </summary>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		partial void CurPronunWssSideEffects(string originalValue, string newValue)
		{
			m_currentPronunciationWritingSystems.RaiseChanged();
			m_cache.ActionHandlerAccessor.AddAction(new UndoWsChangeAction() { Cache = m_cache });
		}

		/// <summary>
		/// Gets the default pronunciation writing system.
		/// </summary>
		/// <value>The default pronunciation writing system.</value>
		public IWritingSystem DefaultPronunciationWritingSystem
		{
			get
			{
				InitializePronunciationWritingSystems(); // make sure there is one.
				return m_currentPronunciationWritingSystems.FirstOrDefault();
			}
		}

		/// <summary>
		/// If there are no pronunciation writing systems selected, make a default set, with IPA variants
		/// coming before EMC variants (if either of those exist).  If neither exists, the primary
		/// vernacular writing system is selected.
		/// </summary>
		private void InitializePronunciationWritingSystems()
		{
			if (m_currentPronunciationWritingSystems.Count > 0)
				return;

			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
				() =>
					{
						var writingSystems = m_vernacularWritingSystems;
						var wsVern = DefaultVernacularWritingSystem;
						var sVern = wsVern.IcuLocale.ToLower();
						var idx = sVern.IndexOf("_");
						sVern = idx > 0 ? sVern.Substring(0, idx + 1) : sVern + '_';
						// Add any relevant IPA writing systems: those that match the default vernacular at the start and end with _ipa
						foreach (var nws in writingSystems)
						{
							var icuLocale = nws.IcuLocale.ToLower();
							if (icuLocale.IndexOf(sVern) != 0) continue;

							idx = icuLocale.LastIndexOf("_ipa");
							if (idx >= sVern.Length && idx == icuLocale.Length - 4)
								m_currentPronunciationWritingSystems.Add(nws);
						}
						// Add any relevant EMC writing systems: match default vern at start and end with _emc.
						foreach (var nws in writingSystems)
						{
							var icuLocale = nws.IcuLocale.ToLower();
							if (icuLocale.IndexOf(sVern) != 0) continue;

							idx = icuLocale.LastIndexOf("_emc");
							if (idx < sVern.Length || idx != icuLocale.Length - 4) continue;

							m_currentPronunciationWritingSystems.Add(nws);
						}
						// Add the primary vernacular writing system if nothing else fits.
						if (m_currentPronunciationWritingSystems.Count == 0)
						{
							m_currentPronunciationWritingSystems.Add(wsVern);
						}
					});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to get a more suitable way to display a LangProject.
		/// </summary>
		/// <returns>The name of the language project.</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ShortName;
		}
	}
}
