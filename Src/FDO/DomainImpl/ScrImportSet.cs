// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoScripture.cs
// Responsibility: TE Team

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrImportSet keep track of all the settings for a Paratext or SF import project.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrImportSet
	{
		#region data members
		private readonly ScrImportFileInfoFactory m_scrImpFinfoFact = new ScrImportFileInfoFactory();

		/// <summary>The stylesheet</summary>
		protected IVwStylesheet m_stylesheet;
		/// <summary>
		/// Contains an in-memory list of all of the Scripture domain files for import
		/// </summary>
		private ScrSfFileList m_scrFileInfoList;
		/// <summary>
		/// Contains a hashtable of array lists for all of the back translation domain files
		/// </summary>
		private Hashtable m_btFileInfoLists = new Hashtable();
		/// <summary>
		/// Contains a hashtable of array lists for all of the notes domain files
		/// </summary>
		private Hashtable m_notesFileInfoLists = new Hashtable();

		private IOverlappingFileResolver m_resolver;

		private ScrMappingList m_scrMappingsList;
		private ScrMappingList m_notesMappingsList;
		private string m_defaultBtWsId;

		/// <summary>Scripture Paratext project ID</summary>
		protected string m_ParatextScrProject;
		/// <summary>BT Paratext project ID</summary>
		protected string m_ParatextBTProject;
		/// <summary>Notes Paratext project ID</summary>
		protected string m_ParatextNotesProject;

		private bool m_fImportTranslation;
		private bool m_fImportBackTrans;
		private bool m_fImportBookIntros;
		private bool m_fImportAnnotations;
		private BCVRef m_startRef;
		private BCVRef m_endRef;
		#endregion

		#region Construction & initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the ScrImportSet. Sets the default values after the initialization of a
		/// CmObject. At the point that this method is called, the object should have an HVO,
		/// Guid, and a cache set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			DoCommonNonModelSetup();
		}

		private void DoCommonNonModelSetup()
		{
			m_scrMappingsList = new ScrMappingList(MappingSet.Main, m_stylesheet);
			m_notesMappingsList = new ScrMappingList(MappingSet.Notes, m_stylesheet);

			LoadInMemoryMappingLists();
			LoadSources(false);
		}

		protected override void DoAdditionalReconstruction()
		{
			base.DoAdditionalReconstruction();
			DoCommonNonModelSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load Standard Format files or Paratext projects from DB into memory
		/// </summary>
		/// <param name="createSourcesIfNeeded">True to create the sources if they do not
		/// already exist</param>
		/// ------------------------------------------------------------------------------------
		private void LoadSources(bool createSourcesIfNeeded)
		{
			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
				case TypeOfImport.Unknown:
					LoadInMemoryFileLists();
					break;

				case TypeOfImport.Paratext6:
					LoadParatextProjects(createSourcesIfNeeded);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the in-memory lists of the marker mappings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadInMemoryMappingLists()
		{
			foreach (ScrMarkerMapping mapping in ScriptureMappingsOC)
				m_scrMappingsList.Add(mapping.ToImportMappingInfo());
			foreach (ScrMarkerMapping mapping in NoteMappingsOC)
				m_notesMappingsList.Add(mapping.ToImportMappingInfo());
			m_scrMappingsList.ResetChangedFlags();
			m_notesMappingsList.ResetChangedFlags();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the in-memory copies of file lists from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadInMemoryFileLists()
		{
			foreach (IScrImportSource source in ScriptureSourcesOC)
			{
				if (source is ScrImportSFFiles)
				{
					m_scrFileInfoList = new ScrSfFileList((ScrImportSFFiles)source,
						m_scrMappingsList, ImportDomain.Main,
						(ImportTypeEnum == TypeOfImport.Paratext5));
					m_scrFileInfoList.OverlappingFileResolver = m_resolver;
					break;
				}
			}
			if (m_scrFileInfoList == null)
				m_scrFileInfoList = new ScrSfFileList(m_resolver);

			foreach (IScrImportSource source in BackTransSourcesOC)
			{
				if (source is ScrImportSFFiles)
				{
					string wsId = source.WritingSystem ?? string.Empty;
					m_btFileInfoLists[wsId] = new ScrSfFileList((ScrImportSFFiles)source,
						m_scrMappingsList, ImportDomain.BackTrans,
						(ImportTypeEnum == TypeOfImport.Paratext5));
				}
			}

			foreach (IScrImportSource source in NoteSourcesOC)
			{
				if (source is ScrImportSFFiles)
				{
					string wsId = source.WritingSystem ?? string.Empty;
					string key = ScriptureServices.CreateImportSourceKey(wsId,
						((ScrImportSFFiles)source).NoteTypeRA);
					m_notesFileInfoLists[key] = new ScrSfFileList((ScrImportSFFiles)source,
						m_notesMappingsList, ImportDomain.Annotations,
						(ImportTypeEnum == TypeOfImport.Paratext5));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the in-memory IDs of Paratext Projects from the database.
		/// </summary>
		/// <param name="createSourcesIfNeeded">True to create the sources if they do not
		/// already exist</param>
		/// ------------------------------------------------------------------------------------
		private void LoadParatextProjects(bool createSourcesIfNeeded)
		{
			ScrImportP6Project project = (ScrImportP6Project)GetSourceForScripture(createSourcesIfNeeded);
			m_ParatextScrProject = (project != null) ? project.ParatextID : null;

			project = (ScrImportP6Project)GetSourceForBT(null, createSourcesIfNeeded);
			m_ParatextBTProject = (project != null) ? project.ParatextID : null;

			project = (ScrImportP6Project)GetSourceForNotes(null, null, createSourcesIfNeeded);
			m_ParatextNotesProject = (project != null) ? project.ParatextID : null;
		}
		#endregion

		#region Overridden ScrImportSet properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets ImportType. When it is changed, we need to delete the old Sources.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TypeOfImport ImportTypeEnum
		{
			get
			{
				return (TypeOfImport)ImportType;
			}
			set
			{
				if (value != ImportTypeEnum)
				{
					TypeOfImport oldValue = (TypeOfImport)ImportType;
					ImportType = (int)value;

					if (value == TypeOfImport.Paratext6 ||
						oldValue == TypeOfImport.Paratext6 ||
						oldValue == TypeOfImport.Unknown)
					{
						if (oldValue != TypeOfImport.Unknown)
						{
							GetMappingList(MappingSet.Main).ResetInUseFlags();
							GetMappingList(MappingSet.Notes).ResetInUseFlags();
						}

						LoadSources(true);
					}
					else if (value == TypeOfImport.Paratext5)
					{
						// There may be additional P5-style in-line markers that were not detected
						// previously. Rescan all files to look for them.
						m_scrFileInfoList.Rescan(true);
						foreach (ScrSfFileList fileList in m_btFileInfoLists.Values)
							fileList.Rescan(true);
						foreach (ScrSfFileList fileList in m_notesFileInfoLists.Values)
							fileList.Rescan(true);
					}
					else
					{
						GetMappingList(MappingSet.Main).ResetInUseFlagsForInlineMappings();
						GetMappingList(MappingSet.Notes).ResetInUseFlagsForInlineMappings();
					}
				}
			}
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starting reference for the import; for now, we ignore the
		/// chapter and verse since import will always start at the beginning of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef StartRef
		{
			get
			{
				lock (SyncRoot)
					return new BCVRef(m_startRef);
			}
			set
			{
				lock (SyncRoot)
					m_startRef = new BCVRef(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ending reference for the import; for now, we ignore the
		/// chapter and verse since import will always end at the end of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get
			{
				lock (SyncRoot)
					return new BCVRef(m_endRef);
			}
			set
			{
				lock (SyncRoot)
					m_endRef = new BCVRef(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import the vernacular Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportTranslation
		{
			get
			{
				lock (SyncRoot)
					return m_fImportTranslation;
			}
			set
			{
				lock (SyncRoot)
					m_fImportTranslation = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import back translations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportBackTranslation
		{
			get
			{
				lock (SyncRoot)
					return m_fImportBackTrans;
			}
			set
			{
				lock (SyncRoot)
					m_fImportBackTrans = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import introductions to books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportBookIntros
		{
			get
			{
				lock (SyncRoot)
					return m_fImportBookIntros;
			}
			set
			{
				lock (SyncRoot)
					m_fImportBookIntros = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import Annotations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportAnnotations
		{
			get
			{
				lock (SyncRoot)
					return m_fImportAnnotations;
			}
			set
			{
				lock (SyncRoot)
					m_fImportAnnotations = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets wheter project includes separate source(s) with a back translation. If project
		/// has no BT or the BT is interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public bool HasNonInterleavedBT
		{
			get
			{
				lock (SyncRoot)
				{
					switch (ImportTypeEnum)
					{
						case TypeOfImport.Paratext6:
							return (ParatextBTProj != null);
						case TypeOfImport.Other:
						case TypeOfImport.Paratext5:
							return (GetImportFiles(ImportDomain.BackTrans).Count > 0);
						default:
							throw new NotSupportedException("Unexpected type of Import Project");
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether project includes separate source(s) with annotations, or notes. If
		/// project has no annotations or they are interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public bool HasNonInterleavedNotes
		{
			get
			{
				lock (SyncRoot)
				{
					switch (ImportTypeEnum)
					{
						case TypeOfImport.Paratext6:
							return (ParatextNotesProj != null);
						case TypeOfImport.Other:
						case TypeOfImport.Paratext5:
							return (GetImportFiles(ImportDomain.Annotations).Count > 0);
						default:
							throw new NotSupportedException("Unexpected type of Import Project");
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates if basic properties are set to allow import. For example, if this is a
		/// Paratext project import, at least the vernacular project must be specified for this
		/// to return true. If this is an Other project, at least one filename must be
		/// specified for this to return true.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool BasicSettingsExist
		{
			get
			{
				lock (SyncRoot)
				{
					switch (ImportTypeEnum)
					{
						case TypeOfImport.Paratext6:
							return m_ParatextScrProject != null;

						case TypeOfImport.Other:
						case TypeOfImport.Paratext5:
							return (m_scrFileInfoList.Count > 0);

						default:
							return false;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default writing system identifier to use for the Back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultBtWsId
		{
			get
			{
				lock (SyncRoot)
				{
					return m_defaultBtWsId ?? Services.WritingSystems.DefaultAnalysisWritingSystem.ID;
				}
			}
			set
			{
				lock (SyncRoot)
					m_defaultBtWsId = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Overlapping File Resolver
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IOverlappingFileResolver OverlappingFileResolver
		{
			set
			{
				lock (SyncRoot)
				{
					m_resolver = value;

					if (m_scrFileInfoList != null)
						m_scrFileInfoList.OverlappingFileResolver = m_resolver;

					foreach (ScrSfFileList list in m_btFileInfoLists.Values)
						list.OverlappingFileResolver = m_resolver;

					foreach (ScrSfFileList list in m_notesFileInfoLists.Values)
						list.OverlappingFileResolver = m_resolver;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this <see cref="ScrImportSet"/> is valid.
		/// </summary>
		/// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
		/// <exception cref="InvalidOperationException">thrown if basic import settings do not
		/// exist</exception>
		/// <exception cref="T:SIL.FieldWorks.Common.ScriptureUtils.ScriptureUtilsException">If this is a non-P6 import project and
		/// the strict file scan finds a data error.</exception>
		/// ------------------------------------------------------------------------------------
		public bool Valid
		{
			get
			{
				lock (SyncRoot)
				{
					if (!BasicSettingsExist)
						throw new InvalidOperationException("Do not call ScrImportSet.Valid unless basic import settings exist.");

					if (ImportTypeEnum == TypeOfImport.Paratext6)
						return true;

					// Scan the Main (Scripture) files
					m_scrFileInfoList.PerformStrictScan();

					// Scan the BT files
					foreach (ScrSfFileList fileList in m_btFileInfoLists.Values)
						fileList.PerformStrictScan();

					// Scan the Notes files
					foreach (ScrSfFileList fileList in m_notesFileInfoLists.Values)
						fileList.PerformStrictScan();

					return true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets stylesheet for settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get
			{
				lock (SyncRoot)
					return m_stylesheet;
			}
			set
			{
				lock (SyncRoot)
				{
					m_stylesheet = value;
					// need to set this on the mapping lists also.
					m_scrMappingsList.StyleSheet = value;
					m_notesMappingsList.StyleSheet = value;
				}
			}
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check all files that are about to be imported in the given reference range to see
		/// if there are any reference overlaps. If so, resolve the conflict.
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End Reference</param>
		/// ------------------------------------------------------------------------------------
		public void CheckForOverlappingFilesInRange(ScrReference start, ScrReference end)
		{
			lock (SyncRoot)
			{
				if (ImportTypeEnum != TypeOfImport.Other && ImportTypeEnum != TypeOfImport.Paratext5)
					throw new InvalidOperationException(
						"Don't call CheckForOverlappingFilesInRange for anything but file-based imports.");

				if (ImportTranslation)
					m_scrFileInfoList.CheckForOverlappingFilesInRange(start, end);

				if (ImportBackTranslation)
				{
					foreach (ScrSfFileList list in m_btFileInfoLists.Values)
						list.CheckForOverlappingFilesInRange(start, end);
				}

				if (ImportAnnotations)
				{
					foreach (ScrSfFileList list in m_notesFileInfoLists.Values)
						list.CheckForOverlappingFilesInRange(start, end);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Accesses the in-memory copy of the requested mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <returns>An enumerator for accessing all the ImportMappingInfo objects for the
		/// given domain</returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable Mappings(MappingSet mappingSet)
		{
			lock (SyncRoot)
				return GetMappingList(mappingSet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mapping info for a given begin marker and set
		/// </summary>
		/// <param name="marker">The begin marker</param>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <returns>An ImportMappingInfo representing an import mapping for the begin
		/// marker</returns>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo MappingForMarker(string marker, MappingSet mappingSet)
		{
			lock (SyncRoot)
				return GetMappingList(mappingSet)[marker];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an import file source that will provide all of the files for an import
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ImportFileSource GetImportFiles(ImportDomain domain)
		{
			lock (SyncRoot)
			{
				switch (domain)
				{
					case ImportDomain.Main:
						return new ImportFileSource(m_scrFileInfoList);
					case ImportDomain.BackTrans:
						return new ImportFileSource(m_btFileInfoLists, m_cache);
					case ImportDomain.Annotations:
						return new ImportFileSource(m_notesFileInfoLists, m_cache);
					default:
						throw new ArgumentException("unexpected domain");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a file from the file list
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="domain">The domain to remove the file from</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type (ignored for back
		/// trans and scripture domains)</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveFile(string fileName, ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType)
		{
			lock (SyncRoot)
			{
				ScrSfFileList fileList = GetFileList(domain, wsId, noteType, false);
				if (fileList == null)
					return;

				foreach (IScrImportFileInfo info in fileList)
				{
					if (info.FileName.ToUpper() == fileName.ToUpper())
					{
						fileList.Remove(info);
						if (fileList.Count == 0)
						{
							GetMappingListForDomain(domain).ResetInUseFlags(domain, wsId, noteType);
						}
						return;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="wsId">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteType">The CmAnnotationDefn for the note type (ignored for back
		/// trans and scripture domains)</param>
		/// <returns>The IScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		public IScrImportFileInfo AddFile(string fileName, ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType)
		{
			return AddFile(fileName, domain, wsId, noteType, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="fileRemovedHandler">Handler for FileRemoved event (can be null if
		/// caller doesn't need to know if a file is removed as a result of a overlapping
		/// conflict</param>
		/// <returns>The IScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		public IScrImportFileInfo AddFile(string fileName, ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType, ScrImportFileEventHandler fileRemovedHandler)
		{
			lock (SyncRoot)
			{
				if (ImportTypeEnum == TypeOfImport.Paratext6)
					throw new InvalidOperationException("Cannot add files to Paratext 6 import Projects");

				// first check to see if the file is already in the project,
				// if so - then remove the file.
				RemoveFile(fileName, domain, wsId, noteType);

				// Make a new file info entry for the added file
				IScrImportFileInfo info = m_scrImpFinfoFact.Create(fileName, GetMappingListForDomain(domain),
																   domain, wsId, noteType,
																   (ImportTypeEnum == TypeOfImport.Paratext5));

				ScrSfFileList fileList = GetFileList(domain, wsId, noteType, true);

				if (fileRemovedHandler != null)
					fileList.FileRemoved += fileRemovedHandler;

				int index;
				try
				{
					index = fileList.Add(info);
				}
				finally
				{
					if (fileRemovedHandler != null)
						fileList.FileRemoved -= fileRemovedHandler;
				}

				return (index == -1) ? null : info;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Scripture project ID.
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual string ParatextScrProj
		{
			get
			{
				lock (SyncRoot)
					return m_ParatextScrProject == string.Empty ? null : m_ParatextScrProject;
			}
			set
			{
				lock (SyncRoot)
				{
					// make sure that the project name is not already in use by the other projects
					if (value != null && (value == ParatextNotesProj || value == ParatextBTProj))
						throw new ArgumentException(ScrFdoResources.kstidPtScrAlreadyUsed);

					m_ParatextScrProject = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Back Translation project name
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual string ParatextBTProj
		{
			get
			{
				lock (SyncRoot)
					return m_ParatextBTProject == string.Empty ? null : m_ParatextBTProject;
			}
			set
			{
				lock (SyncRoot)
				{
					// make sure that the project name is not already in use by the other projects
					if (value != null && (value == ParatextScrProj || value == ParatextNotesProj))
						throw new ArgumentException(ScrFdoResources.kstidPtBtAlreadyUsed);

					m_ParatextBTProject = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Notes project name
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual string ParatextNotesProj
		{
			get
			{
				lock (SyncRoot)
					return m_ParatextNotesProject == string.Empty ? null : m_ParatextNotesProject;
			}
			set
			{
				lock (SyncRoot)
				{
					// make sure that the project name is not already in use by the other projects
					if (value != null && (value == ParatextScrProj || value == ParatextBTProj))
						throw new ArgumentException(ScrFdoResources.kstidPtNotesAlreadyUsed);

					m_ParatextNotesProject = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commit the "temporary" in-memory settings to the permanent properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings()
		{
			SaveMappings();
			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
					SaveFileLists();
					break;

				case TypeOfImport.Paratext6:
					ScrImportP6Project project = (ScrImportP6Project)GetSourceForScripture(true);
					project.ParatextID = m_ParatextScrProject;
					project = (ScrImportP6Project)GetSourceForBT(null, true);
					project.ParatextID = m_ParatextBTProject;
					project = (ScrImportP6Project)GetSourceForNotes(null, null, true);
					project.ParatextID = m_ParatextNotesProject;
					break;

				default:
					throw new InvalidOperationException("Can't save project with unknown import type.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Abandon the "temporary" in-memory settings and re-load from the permanent properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RevertToSaved()
		{
			lock (SyncRoot)
			{
				m_scrFileInfoList = null;
				m_btFileInfoLists = new Hashtable();
				m_notesFileInfoLists = new Hashtable();
				SetDefaultValuesAfterInit();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set (add or modify) a mapping in the designated mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <param name="mapping">The mapping info</param>
		/// ------------------------------------------------------------------------------------
		public void SetMapping(MappingSet mappingSet, ImportMappingInfo mapping)
		{
			lock (SyncRoot)
				GetMappingList(mappingSet).Add(mapping);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a mapping from the designated mapping list
		/// </summary>
		/// <param name="mappingSet">Indicates the mapping list to delete from.</param>
		/// <param name="mapping">The mapping info</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteMapping(MappingSet mappingSet, ImportMappingInfo mapping)
		{
			lock (SyncRoot)
				GetMappingList(mappingSet).Delete(mapping);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the string starts with one of the markers given.
		/// </summary>
		/// <returns>the marker found, else null</returns>
		/// ------------------------------------------------------------------------------------
		private string StartsWithMarker(string line, StringCollection markersList)
		{
			foreach (string marker in markersList)
				if (line.StartsWith(marker))
					return marker;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a MappingSet that is appropriate for the ImportDomain
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public MappingSet GetMappingSetForDomain(ImportDomain domain)
		{
			switch (domain)
			{
				default:
				case ImportDomain.Main:
				case ImportDomain.BackTrans:
					return MappingSet.Main;

				case ImportDomain.Annotations:
					return MappingSet.Notes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a mapping list based on the import domain
		/// </summary>
		/// <param name="domain">The import domain</param>
		/// <returns>The mapping list</returns>
		/// ------------------------------------------------------------------------------------
		public ScrMappingList GetMappingListForDomain(ImportDomain domain)
		{
			lock (SyncRoot)
				return GetMappingList(GetMappingSetForDomain(domain));
		}
		#endregion

		#region helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the mapping list for an import domain
		/// </summary>
		/// <param name="mappingSet">Indicates the desired mapping list.</param>
		/// <returns>The mapping list</returns>
		/// ------------------------------------------------------------------------------------
		private ScrMappingList GetMappingList(MappingSet mappingSet)
		{
			switch (mappingSet)
			{
				default:
				case MappingSet.Main:
					return m_scrMappingsList;
				case MappingSet.Notes:
					return m_notesMappingsList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the ScrImportSource for the requested domain, WS, and note type
		/// </summary>
		/// <param name="domain">The source domain</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteTypeGuid">The GUID of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="createSourceIfNeeded">True to create the source if it does not already
		/// exist for a given domain, writing system identifier and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSource(ImportDomain domain, string wsId,
			Guid noteTypeGuid, bool createSourceIfNeeded)
		{
			switch (domain)
			{
				case ImportDomain.Main:
					return GetSourceForScripture(createSourceIfNeeded);

				case ImportDomain.BackTrans:
					return GetSourceForBT(wsId, createSourceIfNeeded);

				case ImportDomain.Annotations:
					{
						ICmAnnotationDefn noteType;
						try
						{
							noteType = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(noteTypeGuid);
						}
						catch
						{
							noteType = null;
						}
						return GetSourceForNotes(wsId, noteType, createSourceIfNeeded);
					}

				default:
					throw new ArgumentException("unexpected domain");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ScrImportSource for the scripture import domain
		/// </summary>
		/// <param name="createSourceIfNeeded">True to create the Scripture source if it does
		/// not already exist</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSourceForScripture(bool createSourceIfNeeded)
		{
			IScrImportSource result = null;
			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
				{
					foreach (IScrImportSource source in ScriptureSourcesOC)
					{
						if (source is ScrImportSFFiles)
							return source;
					}
					if (createSourceIfNeeded)
					{
						result = Services.GetInstance<IScrImportSFFilesFactory>().Create();
						ScriptureSourcesOC.Add(result);
					}
					break;
				}
				case TypeOfImport.Paratext6:
				{
					foreach (IScrImportSource source in ScriptureSourcesOC)
					{
						if (source is ScrImportP6Project)
							return source;
					}
					if (createSourceIfNeeded)
					{
						result = Services.GetInstance<IScrImportP6ProjectFactory>().Create();
						ScriptureSourcesOC.Add(result);
					}
					break;
				}
				default:
					throw new InvalidOperationException("Unexpected import type");
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ScrImportSource for the back translation import domain with the specified
		/// writing system identifier
		/// </summary>
		/// <param name="wsId"></param>
		/// <param name="createSourceIfNeeded">True to create the BT source if it does not already
		/// exist for a given writing system identifier</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSourceForBT(string wsId, bool createSourceIfNeeded)
		{
			int classId = (ImportTypeEnum == TypeOfImport.Paratext6) ?
				ScrImportP6ProjectTags.kClassId : ScrImportSFFilesTags.kClassId;

			foreach (IScrImportSource source in BackTransSourcesOC)
			{
				if (source.WritingSystem == wsId && source.ClassID == classId)
					return source;
			}
			if (!createSourceIfNeeded)
				return null;

			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
				{
					IScrImportSFFiles source = Services.GetInstance<IScrImportSFFilesFactory>().Create();
					BackTransSourcesOC.Add(source);
					source.WritingSystem = wsId;
					return source;
				}
				case TypeOfImport.Paratext6:
				{
					IScrImportP6Project source = Services.GetInstance<IScrImportP6ProjectFactory>().Create();
					BackTransSourcesOC.Add(source);
					source.WritingSystem = wsId;
					return source;
				}
				default:
					throw new InvalidOperationException("Unexpected import type");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ScrImportSource for the notes import domain with the specified writing
		/// system identifier and having the specified note type.
		/// </summary>
		/// <param name="wsId"></param>
		/// <param name="noteType">The CmAnnotationDefn for the type of note to
		/// get the source for. Use <c>null</c> to get notes of any type that matches the given
		/// writing system identifier.</param>
		/// <param name="createSourceIfNeeded">True to create the source if it does not already
		/// exist for the given writing system identifier and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSourceForNotes(string wsId, ICmAnnotationDefn noteType,
			bool createSourceIfNeeded)
		{
			int classId = (ImportTypeEnum == TypeOfImport.Paratext6) ?
				ScrImportP6ProjectTags.kClassId : ScrImportSFFilesTags.kClassId;

			foreach (IScrImportSource noteSource in NoteSourcesOC)
			{
				if (noteSource.WritingSystem == wsId &&
					noteSource.NoteTypeRA == noteType &&
					noteSource.ClassID == classId)
				{
					return noteSource;
				}
			}
			if (!createSourceIfNeeded)
				return null;

			IScrImportSource source;
			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
					source = Services.GetInstance<IScrImportSFFilesFactory>().Create();
					break;
				case TypeOfImport.Paratext6:
					source = Services.GetInstance<IScrImportP6ProjectFactory>().Create();
					break;
				default:
					throw new InvalidOperationException("Unexpected import type");

			}
			NoteSourcesOC.Add(source);
			source.WritingSystem = wsId;
			source.NoteTypeRA = noteType;
			return source;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the file list for the given domain, writing system identifier and note type.
		/// </summary>
		/// <param name="domain">The source domain</param>
		/// <param name="wsId">The writing system identifier for the source (ignored for
		/// scripture domain)</param>
		/// <param name="noteType">The CmAnnotationDefn for the note type (ignored for back
		/// trans and scripture domains)</param>
		/// <param name="createListIfNeeded">True to create the list if it does not already
		/// exist for a given writing system identifier and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ScrSfFileList GetFileList(ImportDomain domain, string wsId,
			ICmAnnotationDefn noteType,	bool createListIfNeeded)
		{
			Debug.Assert(ImportTypeEnum == TypeOfImport.Other || ImportTypeEnum == TypeOfImport.Paratext5);

			if (wsId == null)
				wsId = string.Empty;
			switch (domain)
			{
				default:
				case ImportDomain.Main:
					return m_scrFileInfoList;

				case ImportDomain.BackTrans:
				{
					// Look for a back trans source with the given writing system identifier.
					ScrSfFileList btList = m_btFileInfoLists[wsId] as ScrSfFileList;
					if (btList == null && createListIfNeeded)
					{
						btList = new ScrSfFileList(m_resolver);
						m_btFileInfoLists[wsId] = btList;
					}
					return btList;
				}
				case ImportDomain.Annotations:
				{
					// Look for a annotations source with the given writing system identifier.
					string key = ScriptureServices.CreateImportSourceKey(wsId, noteType);
					ScrSfFileList noteList = m_notesFileInfoLists[key] as ScrSfFileList;
					if (noteList == null && createListIfNeeded)
					{
						noteList = new ScrSfFileList(m_resolver);
						m_notesFileInfoLists[key] = noteList;
					}
					return noteList;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the in-memory mapping lists to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveMappings()
		{
			if (m_scrMappingsList.HasChanged)
				SaveMappings(ScriptureMappingsOC, m_scrMappingsList);
			if (m_notesMappingsList.HasChanged)
				SaveMappings(NoteMappingsOC, m_notesMappingsList);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the in-memory mapping lists for the given mapping set
		/// </summary>
		/// <param name="mappingsOC">The owning collection of ScrMarkerMapping objects in the
		/// database</param>
		/// <param name="mappingInfoList">The in-memory list of ImportMappingInfo objects</param>
		/// ------------------------------------------------------------------------------------
		private void SaveMappings(IFdoOwningCollection<IScrMarkerMapping> mappingsOC, ScrMappingList mappingInfoList)
		{
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Save mappings", "Save mappings",
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				mappingsOC.Clear();
				foreach (ImportMappingInfo info in mappingInfoList)
				{
					IScrMarkerMapping mapping = Services.GetInstance<IScrMarkerMappingFactory>().Create();
					mappingsOC.Add(mapping);
					// The "Default Paragraph Characters" style is not a real style. So, we save it as
					// as separate target type. We want to set the style now for the in-memory info.
					if (info.StyleName == StyleUtils.DefaultParaCharsStyleName)
						info.MappingTarget = MappingTargetType.DefaultParaChars;
					else if (info.Style == null || info.Style.Name != info.StyleName)
						info.SetStyle(m_cache.LangProject.TranslatedScriptureOA.FindStyle(info.StyleName));
					((ScrMarkerMapping)mapping).InitFromImportMappingInfo(info);
				}
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the in-memory file lists to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveFileLists()
		{
			// Save the Main (Scripture) source
			SaveFileList(m_scrFileInfoList, (ScrImportSFFiles)GetSource(ImportDomain.Main,
				null, Guid.Empty, true));

			// Save the BT sources
			foreach (string wsId in m_btFileInfoLists.Keys)
			{
				ScrSfFileList fileList = (ScrSfFileList)m_btFileInfoLists[wsId];
				SaveFileList(fileList, (ScrImportSFFiles)GetSource(ImportDomain.BackTrans,
					wsId, Guid.Empty, true));
			}

			// Save the Notes sources
			foreach (string key in m_notesFileInfoLists.Keys)
			{
				string[] keyTokens = key.Split(new char[] {ScriptureServices.kKeyTokenSeparator}, 2);
				string wsId = keyTokens[0] == string.Empty ? null : keyTokens[0];
				Guid guidNoteType = new Guid(keyTokens[1]);
				ScrSfFileList fileList = (ScrSfFileList)m_notesFileInfoLists[key];
				SaveFileList(fileList, (ScrImportSFFiles)GetSource(ImportDomain.Annotations,
					wsId, guidNoteType, true));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save an in-memory list of files to the given source
		/// </summary>
		/// <param name="files">ScrSfFileList containing in-memory file list</param>
		/// <param name="source">ScrImportSFFiles in the database</param>
		/// ------------------------------------------------------------------------------------
		private void SaveFileList(ScrSfFileList files, ScrImportSFFiles source)
		{
			// If the file list has not changed, don't try to save it
			if (!files.Modified)
				return;

			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Save file list", "Save file list",
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				source.FilesOC.Clear();
				foreach (IScrImportFileInfo fileInfo in files)
				{
					ICmFile file = Services.GetInstance<ICmFileFactory>().Create();
					source.FilesOC.Add(file);
					file.InternalPath = fileInfo.FileName;
				}
			});
		}
		#endregion
	}
}
