// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoScripture.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region ImportDomain enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Used to indicate the source of import data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ImportDomain
	{
		/// <summary>
		/// Indicates the primary Scripture stream. Use this if Scripture is interleaved with
		/// BT and/or annotations.
		/// </summary>
		Main,
		/// <summary>Back translation</summary>
		BackTrans,
		/// <summary>Annotations, such as consultant notes</summary>
		Annotations,
	}
	#endregion

	#region FileFormatType enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// valid values for FileFormat
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FileFormatType
	{
		/// <summary>non-Paratext SF markup (e.g., from Toolbox)</summary>
		Other = 0,
		/// <summary>Paratext markup</summary>
		Paratext
	}
	#endregion

	#region IComparer classes
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to compare strings and sort by length (longest first)
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class LengthComparer : IComparer<string>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Comparison method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(string obj1, string obj2)
		{
			return (obj2.Length - obj1.Length);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Compare ScrImportFileInfo objects to sort them in canonical order by the
	/// starting reference in the file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileInfoComparer : IComparer<ScrImportFileInfo>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Comparison method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(ScrImportFileInfo x, ScrImportFileInfo y)
		{
			return (int)x.StartRef - (int)y.StartRef;
		}
	}
	#endregion

	#region class ScrImportSet
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrImportSet keep track of all the settings for a Paratext or SF import project.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrImportSet
	{
		#region data members

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

		private IOverlappingFileResolver m_resolver = null;

		private ScrMappingList m_scrMappingsList;
		private ScrMappingList m_notesMappingsList;
		private string m_defaultBtIcuLocale = null;

		/// <summary>Book marker</summary>
		public static string s_markerBook = @"\id";
		/// <summary>Chapter marker</summary>
		public static string s_markerChapter = @"\c";
		/// <summary>Verse marker</summary>
		public static string s_markerVerse = @"\v";

		private const char kKeyTokenSeparator = '\uffff';

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

		private string m_helpFile;
		#endregion

		#region Construction & initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor that creates a ScrImportSet object based on an HVO in the database.
		/// </summary>
		/// <param name="cache">Represents DB connection</param>
		/// <param name="hvo">Id of the ScrImportSet object in the DB</param>
		/// <param name="stylesheet">The stylesheet</param>
		/// <param name="helpFile">The path to the application help file.</param>
		/// ------------------------------------------------------------------------------------
		public ScrImportSet(FdoCache cache, int hvo, IVwStylesheet stylesheet,
			string helpFile) : base()
		{
			m_stylesheet = stylesheet;
			m_helpFile = helpFile;
			InitExisting(cache, hvo);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="fCheckValidity"></param>
		/// <param name="fLoadIntoCache"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InitExisting(FdoCache cache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
		{
			base.InitExisting(cache, hvo, fCheckValidity, fLoadIntoCache);
			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new FDO object, based on a newly created database object.
		/// Overriden to perform additional initialization.
		/// </summary>
		/// <param name="fcCache">The FDO cache object.</param>
		/// <param name="hvoOwner">ID of the owning object (must be Scripture).</param>
		/// <param name="flidOwning">Field ID that will own the new object.</param>
		/// <param name="ihvo">Ignored</param>
		/// ------------------------------------------------------------------------------------
		protected override void InitNew(FdoCache fcCache, int hvoOwner,
			int flidOwning, int ihvo)
		{
			base.InitNew(fcCache, hvoOwner, flidOwning, ihvo);
			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the ScrImportSet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Initialize()
		{
			m_scrMappingsList = new ScrMappingList(MappingSet.Main, m_stylesheet);
			m_notesMappingsList = new ScrMappingList(MappingSet.Notes, m_stylesheet);

			ConvertFromBlobSettings(DeprecatedImportSettings);
			LoadInMemoryMappingLists();
			LoadSources();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load Standard Format files or Paratext projects from DB into memory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadSources()
		{
			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
					LoadInMemoryFileLists();
					break;

				case TypeOfImport.Paratext6:
					LoadParatextProjects();
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Private method to retrieve the deprecated "blob" settings. This should only be used
		/// for the purposes of converting to the new style of settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private byte[] DeprecatedImportSettings
		{
			get
			{
				byte[] settings = ImportSettings_Generated;
				if (settings.Length == 0)
					return null;
				return DataZip.UnpackData(ImportSettings_Generated);
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
			foreach (int hvo in ScriptureSourcesOC.HvoArray)
			{
				if (m_cache.GetClassOfObject(hvo) == ScrImportSFFiles.kClassId)
				{
					m_scrFileInfoList = new ScrSfFileList(new ScrImportSFFiles(m_cache, hvo),
						m_scrMappingsList, ImportDomain.Main,
						(ImportTypeEnum == TypeOfImport.Paratext5),	m_helpFile);
					m_scrFileInfoList.OverlappingFileResolver = m_resolver;
					break;
				}
			}
			if (m_scrFileInfoList == null)
				m_scrFileInfoList = new ScrSfFileList(m_resolver);

			foreach (int hvo in BackTransSourcesOC.HvoArray)
			{
				if (m_cache.GetClassOfObject(hvo) == ScrImportSFFiles.kClassId)
				{
					ScrImportSFFiles source = new ScrImportSFFiles(m_cache, hvo);
					string icuLocale = source.ICULocale == null ? string.Empty : source.ICULocale;
					m_btFileInfoLists[icuLocale] = new ScrSfFileList(source, m_scrMappingsList,
						ImportDomain.BackTrans, (ImportTypeEnum == TypeOfImport.Paratext5),
						m_helpFile);
				}
			}

			foreach (int hvo in NoteSourcesOC.HvoArray)
			{
				if (m_cache.GetClassOfObject(hvo) == ScrImportSFFiles.kClassId)
				{
					ScrImportSFFiles source = new ScrImportSFFiles(m_cache, hvo);
					string icuLocale = source.ICULocale == null ? string.Empty : source.ICULocale;
					string key = CreateImportSourceKey(icuLocale, source.NoteTypeRAHvo);
					m_notesFileInfoLists[key] = new ScrSfFileList(source, m_notesMappingsList,
						ImportDomain.Annotations, (ImportTypeEnum == TypeOfImport.Paratext5),
						m_helpFile);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the in-memory IDs of Paratext Projects from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadParatextProjects()
		{
			ScrImportP6Project project = (ScrImportP6Project)GetSourceForScripture(true);
			m_ParatextScrProject = project.ParatextID;

			project = (ScrImportP6Project)GetSourceForBT(null, true);
			m_ParatextBTProject = project.ParatextID;

			project = (ScrImportP6Project)GetSourceForNotes(null, 0, true);
			m_ParatextNotesProject = project.ParatextID;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the settings array and set the import project values. This converts from the
		/// "blob", which is now a deprecated way of storing the import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ConvertFromBlobSettings(byte[] settings)
		{
			// If there is no blob (hurrah!) then don't do any conversion
			if (settings == null)
				return;

			using (new SuppressSubTasks(m_cache))
			{
				string sSavePoint = string.Empty;
				if (m_cache.DatabaseAccessor != null)
					m_cache.DatabaseAccessor.SetSavePointOrBeginTrans(out sSavePoint);
				try
				{
					Debug.Assert(ScriptureMappingsOC.Count == 0);
					Debug.Assert(ScriptureSourcesOC.Count == 0);
					switch (ImportTypeEnum)
					{
						case TypeOfImport.Paratext6:
							ConvertFromParatextBlob(settings);
							break;
						case TypeOfImport.Other:
							ConvertFromEcProjectBlob(settings);
							break;
						default:
							// Note: P5 projects were not supported in the days of blob settings
							throw new Exception("Invalid Import Settings type");
					}
					if (m_cache.DatabaseAccessor != null)
						m_cache.DatabaseAccessor.CommitTrans();
				}
				catch
				{
					// Ignore any exceptions. If problems arise, just throw away
					// the old blob since we will no longer use it. The rollback
					// will discard any partially converted settings because we
					// want all or nothing.
					if (m_cache.DatabaseAccessor != null)
						m_cache.DatabaseAccessor.RollbackSavePoint(sSavePoint);
				}
				finally
				{
					// Once the blob is converted (or at least attempted), delete it.
					ImportSettings = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the ECProject settings array and set the import project values. This converts
		/// from the "blob", which is now a deprecated way of storing the import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ConvertFromEcProjectBlob(byte[] settings)
		{
			BinarySettings blob = new BinarySettings(settings, "ECProject");
			int version = blob.Version;
			if (version < 0x102 || version > 0x0104)
				throw (new Exception("Version number in settings does not match"));

			// stuff at the start of the file
			blob.EatString();	// book marker -- It's always \id so ignore
			blob.EatString();	// data encoding
			blob.EatString();	// marker encoding
			blob.EatString();	// binary directory
			blob.EatString();	// SSF file name
			blob.EatString();	// STY file name

			Scripture scr = new Scripture(m_cache, OwnerHVO);
			// Marker mappings
			ReadBlobMappings(blob, MarkerDomain.Default, false, ScriptureMappingsOC, version);

			// file list
			ScrImportSFFiles files = new ScrImportSFFiles();
			ScriptureSourcesOC.Add(files);
			int fileCount = blob.ReadInt();
			for (int i = 0; i < fileCount; i++)
			{
				ICmFile file = files.FilesOC.Add(new CmFile());
				((CmFile)file).SetInternalPath(blob.ReadString());
				blob.EatInt();	// file encoding
				blob.EatInt();	// file encoding source
				blob.EatInt();	// percent certain
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the Paratext settings array and set the import project values. This converts
		/// from the "blob", which is now a deprecated way of storing the import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ConvertFromParatextBlob(byte[] settings)
		{
			BinarySettings blob = new BinarySettings(settings, "PTProject");

			int version = blob.Version;
			if (version < 0x102 || version > 0x0104)
				throw (new Exception("Version number in settings does not match"));

			// Read the mapping set count
			int mappingSetCount = blob.ReadInt();
			Debug.Assert(mappingSetCount >= 1 && mappingSetCount <= 3,"We assume that mappingSetCount is either 1,2,3");

			if (mappingSetCount < 1 || mappingSetCount > 3)
				return;

			// Read Vernacular mappings
			ReadBlobMappings(blob, MarkerDomain.DeprecatedScripture, true, ScriptureMappingsOC, version);

			if (mappingSetCount > 1)
			{
				// Read Back translation mappings. These get added to the regular
				// Scripture mappings because they will either have unique markers
				// or (if this is a non-interleaved project) they MUST map to the
				// same styles as the corresponding vernacular markers.
				ReadBlobMappings(blob, MarkerDomain.Default, true, ScriptureMappingsOC, version);

				// Read the notes mappings
				if (mappingSetCount > 2)
					ReadBlobMappings(blob, MarkerDomain.Note, true, NoteMappingsOC, version);
			}

			string vernProjectID = blob.ReadString();
			if (vernProjectID != null)
			{
				ScrImportP6Project paratextProject = new ScrImportP6Project();
				ScriptureSourcesOC.Add(paratextProject);
				paratextProject.ParatextID = vernProjectID;
			}
			string btProjectID = blob.ReadString();
			if (btProjectID != null)
			{
				ScrImportP6Project paratextProject = new ScrImportP6Project();
				BackTransSourcesOC.Add(paratextProject);
				paratextProject.ParatextID = btProjectID;
			}
			string notesProjectID = blob.ReadString();
			if (notesProjectID != null)
			{
				IScrImportP6Project paratextProject = (IScrImportP6Project)NoteSourcesOC.Add(new ScrImportP6Project());
				ILangProject lp = m_cache.LangProject;
				paratextProject.NoteTypeRA = new CmAnnotationDefn(m_cache, LangProject.kguidAnnTranslatorNote);
				paratextProject.ParatextID = notesProjectID;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a set of mappings from the blob and store them into the given collection of
		/// ScrMarkerMappings.
		/// </summary>
		/// <param name="blob">blob to read from</param>
		/// <param name="mappingSetDomain">source domain being processed</param>
		/// <param name="fParatext">true if this is for Paratext</param>
		/// <param name="mappings">mappings collection to add to</param>
		/// <param name="version">version of the blob data</param>
		/// ------------------------------------------------------------------------------------
		private void ReadBlobMappings(BinarySettings blob, MarkerDomain mappingSetDomain,
			bool fParatext, FdoOwningCollection<IScrMarkerMapping> mappings, int version)
		{
			// build a hashtable of the existing mappings keyed on the begin marker. This will
			// be used to check for existing mappings from other domains.
			Hashtable existingMappings = new Hashtable();
			foreach (IScrMarkerMapping m in mappings)
				existingMappings.Add(m.BeginMarker, m);

			int mappingCount = blob.ReadInt();
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
			ScrMarkerMapping mapping;
			for (int i = 0; i < mappingCount; i++)
			{
				// Read the fields of the marker in the blob
				string beginMarker = blob.ReadString();
				string endMarker = blob.ReadString();
				blob.EatInt(); // inline
				blob.EatString(); // marker encoding
				blob.EatString(); // data encoding - inferred from the WS
				int domain = blob.ReadInt();
				string styleName = blob.ReadString();
				string icuLocale = blob.ReadString();
				blob.EatInt();	// confirmed
				bool excluded = false;
				int target = (int)MappingTargetType.TEStyle;
				// mapping flags are only supported from version 103
				if (version >= 0x103)
				{
					// Looks like we accidentally got these two parameters in opposite order for
					// Paratext and ECProject - ugh!
					if (fParatext)
					{
						target = blob.ReadInt();
						excluded = blob.ReadInt() != 0;
					}
					else
					{
						excluded = blob.ReadInt() != 0;
						target = blob.ReadInt();
					}
				}
				else if (version == 0x102)
				{
					// for old version data, we need to check for the old funky special style names
					// and map them to the new functionality.
					switch(styleName)
					{
						case "~(Ekskloodid)~":
							excluded = true;
							styleName = null;
							break;

						case "~(Chaptr-Leibl)~":
							target = (int)MappingTargetType.ChapterLabel;
							styleName = null;
							break;

						case "~(Fygyur)~":
							target = (int)MappingTargetType.Figure;
							styleName = null;
							break;

						case "~(Taitl-Short)~":
							target = (int)MappingTargetType.TitleShort;
							styleName = null;
							break;
					}
				}

				// Find out if the marker already exists
				mapping = existingMappings[beginMarker] as ScrMarkerMapping;
				if (mapping != null)
				{
					DefaultBtIcuLocale = icuLocale;

					// TODO: Need to deal with weird possibility that user maps \rem to a
					// consultant note in vernacular but tries to map the same marker
					// to a Scripture style in the BT project. Can't just force the
					// Domain to be default because the chosen style won't be
					// appropriate in both contexts.
				}
				else
				{
					// this is a new mapping so create one and fill it in
					mapping = new ScrMarkerMapping();
					mappings.Add(mapping);
					mapping.BeginMarker = beginMarker;
					mapping.EndMarker = endMarker;

					MarkerDomain defaultDomain = ScrMarkerMapping.GetDefaultDomainForStyle(m_stylesheet, styleName);
					if (mappingSetDomain == MarkerDomain.Note)
					{
						// all markers that are in the Notes mapping set must be in the Default domain (TE-4987)
						domain = (int)MarkerDomain.Default;
					}
					else if (defaultDomain != MarkerDomain.Default)
					{
						domain = (int)defaultDomain;
					}
					else if ((domain & (int)mappingSetDomain) != 0)
					{
						// If this mapping is for the source domain being processed, then
						// clear that setting and allow it to be for the default domain.
						domain ^= (int)mappingSetDomain;
					}

					//// Except chapter and verse markers in Scripture and BT which
					//// will be Scripture domain.
					//// REVIEW: Do we also need to do this for the Annotations domain. What does the importer expect?
					//if ((beginMarker == s_markerChapter || beginMarker == s_markerVerse) &&
					//    mappingSetDomain == MarkerDomain.Scripture)
					//{
					//    domain = (int)MarkerDomain.Scripture;
					//}
					//else if ((domain & (int)mappingSetDomain) != 0)
					//    domain ^= (int)mappingSetDomain;
					mapping.Domain = domain;
					if (styleName != null)
						mapping.StyleRA = scr.FindStyle(styleName);
					mapping.ICULocale = icuLocale;
					mapping.Target = target;
					mapping.Excluded = excluded;
				}
			}
		}
		#endregion

		#region Overridden ScrImportSet properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Now deprecated. Allow setter only to set to null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public byte[] ImportSettings
		{
			get
			{
				throw new Exception("This puppy is deprecated!");
			}
			set
			{
				Debug.Assert(value == null || value.Length == 0); // The "blob" is deprecated
				ImportSettings_Generated = new byte[0];
			}
		}

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

						LoadSources();
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

		#region Import Project Accessibility Methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the in-memory import projects/files are currently accessible from
		/// this machine.
		/// </summary>
		/// <param name="thingsNotFound">A list of Paratext project IDs or file paths that
		/// could not be found.</param>
		/// <remarks>
		/// For Paratext projects, this will only return true if all projects are accessible.
		/// For Standard Format, this will return true if any of the files are accessible.
		/// We think this might make sense, but we aren't sure why.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public bool ImportProjectIsAccessible(out StringCollection thingsNotFound)
		{
			if (ImportTypeEnum == TypeOfImport.Paratext6)
				return ParatextProjectsAccessible(out thingsNotFound);
			else if (ImportTypeEnum == TypeOfImport.Other || ImportTypeEnum == TypeOfImport.Paratext5)
				return SFProjectFilesAccessible(out thingsNotFound);

			thingsNotFound = null;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a set of paratext projects can be found and are
		/// accessible.
		/// </summary>
		/// <param name="projectsNotFound">A list of the Paratext projects that couldn't
		/// be found or are inaccessible.</param>
		/// <returns>A value indicating whether or not the projects are accessible. True if
		/// all are. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ParatextProjectsAccessible(out StringCollection projectsNotFound)
		{
			projectsNotFound = new StringCollection();

			if (m_ParatextScrProject == null)
				return false;

			// Paratext seems to want to have write access to do an import...
			string filename = Path.Combine(ScrImportP6Project.ProjectDir, m_ParatextScrProject + ".ssf");
			if (!IsFileWritable(filename) || GetParatextProjectBooks(m_ParatextScrProject).Count == 0)
				projectsNotFound.Add(m_ParatextScrProject);

			if (m_ParatextBTProject != null)
			{
				filename = Path.Combine(ScrImportP6Project.ProjectDir, m_ParatextBTProject + ".ssf");
				if (!IsFileWritable(filename) ||
					GetParatextProjectBooks(m_ParatextBTProject).Count == 0)
					projectsNotFound.Add(m_ParatextBTProject);
			}

			if (m_ParatextNotesProject != null)
			{
				filename = Path.Combine(ScrImportP6Project.ProjectDir, m_ParatextNotesProject + ".ssf");
				if (!IsFileWritable(filename) ||
					GetParatextProjectBooks(m_ParatextNotesProject).Count == 0)
					projectsNotFound.Add(m_ParatextNotesProject);
			}

			return (projectsNotFound.Count == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a set of SFM project files can be found and are
		/// accessible.
		/// </summary>
		/// <param name="filesNotFound">A list of files that couldn't be found.</param>
		/// <returns>true if any SFM files are accessible. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool SFProjectFilesAccessible(out StringCollection filesNotFound)
		{
			filesNotFound = new StringCollection();

			bool fProjectFileFound = false;

			fProjectFileFound |= m_scrFileInfoList.FilesAreAccessible(ref filesNotFound);

			foreach (ScrSfFileList list in m_btFileInfoLists.Values)
				fProjectFileFound |= list.FilesAreAccessible(ref filesNotFound);

			foreach (ScrSfFileList list in m_notesFileInfoLists.Values)
				fProjectFileFound |= list.FilesAreAccessible(ref filesNotFound);

			return (fProjectFileFound);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified file is readable and writable
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>
		/// 	<c>true</c> if the file is readable and writable; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsFileWritable(string filename)
		{
			try
			{
				using (FileStream stream = File.OpenRead(filename))
				{
					stream.Close();
				}
				using (FileStream stream = File.OpenWrite(filename))
				{
					stream.Close();
				}
			}
			catch
			{
				return false;
			}
			return true;
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
			get {return new BCVRef(m_startRef);}
			set {m_startRef = new BCVRef(value);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ending reference for the import; for now, we ignore the
		/// chapter and verse since import will always end at the end of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get {return new BCVRef(m_endRef);}
			set {m_endRef = new BCVRef(value);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import the vernacular Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportTranslation
		{
			get {return m_fImportTranslation;}
			set {m_fImportTranslation = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import back translations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportBackTranslation
		{
			get {return m_fImportBackTrans;}
			set {m_fImportBackTrans = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import introductions to books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportBookIntros
		{
			get {return m_fImportBookIntros;}
			set {m_fImportBookIntros = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to import Annotations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportAnnotations
		{
			get {return m_fImportAnnotations;}
			set {m_fImportAnnotations = value;}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default ICU Locale (corresponds to a WS) to use for the
		/// Back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultBtIcuLocale
		{
			get
			{
				return m_defaultBtIcuLocale != null ? m_defaultBtIcuLocale :
					m_cache.LangProject.DefaultAnalysisWritingSystemICULocale;
			}
			set
			{
				m_defaultBtIcuLocale = value;
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
				m_resolver = value;

				if (m_scrFileInfoList != null)
					m_scrFileInfoList.OverlappingFileResolver = m_resolver;

				foreach (ScrSfFileList list in m_btFileInfoLists.Values)
					list.OverlappingFileResolver = m_resolver;

				foreach (ScrSfFileList list in m_notesFileInfoLists.Values)
					list.OverlappingFileResolver = m_resolver;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this <see cref="T:ScrImportSet"/> is valid.
		/// </summary>
		/// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
		/// <exception cref="InvalidOperationException">thrown if basic import settings do not
		/// exist</exception>
		/// <exception cref="ScriptureUtilsException">If this is a non-P6 import project and
		/// the strict file scan finds a data error.</exception>
		/// ------------------------------------------------------------------------------------
		public bool Valid
		{
			get
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets stylesheet for settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get { return m_stylesheet; }
			set
			{
				m_stylesheet = value;
				// need to set this on the mapping lists also.
				m_scrMappingsList.StyleSheet = value;
				m_notesMappingsList.StyleSheet = value;
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
			if (ImportTypeEnum != TypeOfImport.Other && ImportTypeEnum != TypeOfImport.Paratext5)
				throw new InvalidOperationException("Don't call CheckForOverlappingFilesInRange for anything but file-based imports.");

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
			return (IEnumerable)GetMappingList(mappingSet);
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
			return (ImportMappingInfo)GetMappingList(mappingSet)[marker];
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
			switch (domain)
			{
				case ImportDomain.Main: return new ImportFileSource(m_scrFileInfoList);
				case ImportDomain.BackTrans: return new ImportFileSource(m_btFileInfoLists, m_cache);
				case ImportDomain.Annotations: return new ImportFileSource(m_notesFileInfoLists, m_cache);
				default: throw new ArgumentException("unexpected domain");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a file from the file list
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="domain">The domain to remove the file from</param>
		/// <param name="icuLocale">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveFile(string fileName, ImportDomain domain, string icuLocale,
			int noteTypeHvo)
		{
			ScrSfFileList fileList = GetFileList(domain, icuLocale, noteTypeHvo, false);
			if (fileList == null)
				return;

			foreach (ScrImportFileInfo info in fileList)
			{
				if (info.FileName.ToUpper() == fileName.ToUpper())
				{
					fileList.Remove(info);
					if (fileList.Count == 0)
					{
						GetMappingListForDomain(domain).ResetInUseFlags(domain, icuLocale, noteTypeHvo);
					}
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="icuLocale">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <returns>The ScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		public ScrImportFileInfo AddFile(string fileName, ImportDomain domain, string icuLocale,
			int noteTypeHvo)
		{
			return AddFile(fileName, domain, icuLocale, noteTypeHvo, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the project, and determine the file encoding and mappings.
		/// </summary>
		/// <param name="fileName">file name to add</param>
		/// <param name="domain">The domain to add the file to</param>
		/// <param name="icuLocale">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="fileRemovedHandler">Handler for FileRemoved event (can be null if
		/// caller doesn't need to know if a file is removed as a result of a overlapping
		/// conflict</param>
		/// <returns>The ScrImportFileInfo representing the added file</returns>
		/// ------------------------------------------------------------------------------------
		public ScrImportFileInfo AddFile(string fileName, ImportDomain domain, string icuLocale,
			int noteTypeHvo, ScrImportFileEventHandler fileRemovedHandler)
		{
			if (ImportTypeEnum == TypeOfImport.Paratext6)
				throw new InvalidOperationException("Cannot add files to Paratext 6 import Projects");

			// first check to see if the file is already in the project,
			// if so - then remove the file.
			RemoveFile(fileName, domain, icuLocale, noteTypeHvo);

			// Make a new file info entry for the added file
			ScrImportFileInfo info = new ScrImportFileInfo(fileName, GetMappingListForDomain(domain),
				domain, icuLocale, noteTypeHvo, (ImportTypeEnum == TypeOfImport.Paratext5));

			ScrSfFileList fileList = GetFileList(domain, icuLocale, noteTypeHvo, true);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Paratext Scripture project ID.
		/// </summary>
		/// <remarks>Setter has side-effect of loading the mappings</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual string ParatextScrProj
		{
			get { return m_ParatextScrProject == string.Empty ? null : m_ParatextScrProject; }
			set
			{
				// make sure that the project name is not already in use by the other projects
				if (value != null && (value == ParatextNotesProj || value == ParatextBTProj))
					throw new ArgumentException(ScrFdoResources.kstidPtScrAlreadyUsed);

				m_ParatextScrProject = SetParaTextProject(value, ImportDomain.Main);
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
			get { return m_ParatextBTProject == string.Empty ? null : m_ParatextBTProject; }
			set
			{
				// make sure that the project name is not already in use by the other projects
				if (value != null && (value == ParatextScrProj || value == ParatextNotesProj))
					throw new ArgumentException(ScrFdoResources.kstidPtBtAlreadyUsed);

				m_ParatextBTProject = SetParaTextProject(value, ImportDomain.BackTrans);
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
			get { return m_ParatextNotesProject == string.Empty ? null : m_ParatextNotesProject; }
			set
			{
				// make sure that the project name is not already in use by the other projects
				if (value != null && (value == ParatextScrProj || value == ParatextBTProj))
					throw new ArgumentException(ScrFdoResources.kstidPtNotesAlreadyUsed);

				m_ParatextNotesProject = SetParaTextProject(value, ImportDomain.Annotations);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Paratext project name for the specified domain.
		/// </summary>
		/// <param name="value">The name of the Paratext project.</param>
		/// <param name="domain">The domain (Scripture, back translation or annotations).</param>
		/// <returns>name of the project, if not empty and the project loads without error;
		/// otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		private string SetParaTextProject(string value, ImportDomain domain)
		{
			string projName = (value == string.Empty) ? null : value;
			if (projName != null)
			{
				// use notes list for the annotations domain, otherwise use the scripture list.
				ScrMappingList loadedList = domain == ImportDomain.Annotations ? m_notesMappingsList : m_scrMappingsList;
				bool fValidProj = ScrImportP6Project.LoadParatextMappings(value,
					loadedList, domain);
				return fValidProj ? value : null;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commit the in-memory stuff to the database
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
					project = (ScrImportP6Project)GetSourceForNotes(null, 0, true);
					project.ParatextID = m_ParatextNotesProject;
					break;

				default:
					throw new InvalidOperationException("Can't save project with unknown import type.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Discard any in-memory settings and reload from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RevertToSaved()
		{
			m_scrFileInfoList = null;
			m_btFileInfoLists = new Hashtable();
			m_notesFileInfoLists = new Hashtable();
			Initialize();
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
		/// Returns a list of books that exist for all of the files in this project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <exception cref="NotSupportedException">If project is not a supported type</exception>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksForProject
		{
			get
			{
				Debug.Assert(BasicSettingsExist, "Vernacular Scripture project not defined.");
				List<int> booksPresent = new List<int>();
				switch (ImportTypeEnum)
				{
					case TypeOfImport.Paratext6:
						{
							// TODO (TE-5903): Check BT and Notes projects as well.
							booksPresent = GetParatextProjectBooks(ParatextScrProj);
							break;
						}
					case TypeOfImport.Paratext5:
					case TypeOfImport.Other:
						{
							foreach (ScrImportFileInfo file in m_scrFileInfoList)
								foreach (int iBook in file.BooksInFile)
								{
									if (!booksPresent.Contains(iBook))
										booksPresent.Add(iBook);
								}

							foreach (ScrSfFileList fileList in m_btFileInfoLists.Values)
								foreach (ScrImportFileInfo file in fileList)
									foreach (int iBook in file.BooksInFile)
									{
										if (!booksPresent.Contains(iBook))
											booksPresent.Add(iBook);
									}

							foreach (ScrSfFileList fileList in m_notesFileInfoLists.Values)
								foreach (ScrImportFileInfo file in fileList)
									foreach (int iBook in file.BooksInFile)
									{
										if (!booksPresent.Contains(iBook))
											booksPresent.Add(iBook);
									}
							booksPresent.Sort();
							break;
						}
					default:
						throw new NotSupportedException("Unexpected type of Import Project");
				}

				return booksPresent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of books that exist for the given Paratext project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <remark>The returned list will be empty if there is a problem with the Paratext
		/// installation.</remark>
		/// ------------------------------------------------------------------------------------
		private List<int> GetParatextProjectBooks(string projectId)
		{
			List<int> booksPresent = new List<int>();
			try
			{
				SCRIPTUREOBJECTSLib.ISCScriptureText3 scParatextText = new SCRIPTUREOBJECTSLib.SCScriptureTextClass();
				scParatextText.Load(projectId);
				string stringList = scParatextText.BooksPresent;

				for (int i = 0; i < BCVRef.LastBook && i < stringList.Length; i++)
				{
					if (stringList[i] == '1')
						booksPresent.Add(i + 1);
				}

			}
			catch
			{
				// ignore error - probably Paratext installation problem. Caller can check number
				// of books present.
			}

			return booksPresent;
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
			return GetMappingList(GetMappingSetForDomain(domain));
		}
		#endregion

		#region helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the import source key for a hashtable.
		/// </summary>
		/// <param name="icuLocale">The locale.</param>
		/// <param name="hvoNoteType">Type of the hvo note.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string CreateImportSourceKey(string icuLocale, int hvoNoteType)
		{
			return icuLocale + kKeyTokenSeparator + hvoNoteType.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the import source key for a hashtable.
		/// </summary>
		/// <param name="importDomain">Used as the key if the ICU locale is null</param>
		/// <param name="icuLocale">The locale.</param>
		/// <param name="hvoNoteType">Type of the hvo note.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static object CreateImportSourceKey(ImportDomain importDomain,
			string icuLocale, int hvoNoteType)
		{
			if (icuLocale == null)
				return importDomain;
			else
				return CreateImportSourceKey(icuLocale, hvoNoteType);
		}

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
		/// <param name="icuLocale">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="createSourceIfNeeded">True to create the source if it does not already
		/// exist for a given domain, ICU locale and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSource(ImportDomain domain, string icuLocale, int noteTypeHvo,
			bool createSourceIfNeeded)
		{
			switch (domain)
			{
				case ImportDomain.Main:
					return GetSourceForScripture(createSourceIfNeeded);

				case ImportDomain.BackTrans:
					return GetSourceForBT(icuLocale, createSourceIfNeeded);

				case ImportDomain.Annotations:
					return GetSourceForNotes(icuLocale, noteTypeHvo, createSourceIfNeeded);

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
					foreach (int hvo in ScriptureSourcesOC.HvoArray)
					{
						if (m_cache.GetClassOfObject(hvo) == ScrImportSFFiles.kClassId)
							return new ScrImportSFFiles(m_cache, hvo);
					}
					result = ScriptureSourcesOC.Add(new ScrImportSFFiles());
					break;
				}
				case TypeOfImport.Paratext6:
				{
					foreach (int hvo in ScriptureSourcesOC.HvoArray)
					{
						if (m_cache.GetClassOfObject(hvo) == ScrImportP6Project.kClassId)
							return new ScrImportP6Project(m_cache, hvo);
					}
					result = ScriptureSourcesOC.Add(new ScrImportP6Project());
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
		/// ICU locale
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <param name="createSourceIfNeeded">True to create the BT source if it does not already
		/// exist for a given ICU locale</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSourceForBT(string icuLocale, bool createSourceIfNeeded)
		{
			int classId = (ImportTypeEnum == TypeOfImport.Paratext6) ?
				ScrImportP6Project.kClassId : ScrImportSFFiles.kClassId;

			foreach (IScrImportSource source in BackTransSourcesOC)
			{
				if (source.ICULocale == icuLocale && source.ClassID == classId)
					return source;
			}
			if (!createSourceIfNeeded)
				return null;

			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
				{
					ScrImportSFFiles source = new ScrImportSFFiles();
					BackTransSourcesOC.Add(source);
					source.ICULocale = icuLocale;
					return source;
				}
				case TypeOfImport.Paratext6:
				{
					ScrImportP6Project source = new ScrImportP6Project();
					BackTransSourcesOC.Add(source);
					source.ICULocale = icuLocale;
					return source;
				}
				default:
					throw new InvalidOperationException("Unexpected import type");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ScrImportSource for the notes import domain with the specified ICU
		/// locale and having the specified note type.
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the type of note to
		/// get the source for. 0 to get notes of any type that matches the given ICU locale
		/// </param>
		/// <param name="createSourceIfNeeded">True to create the source if it does not already
		/// exist for the given ICU locale and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrImportSource GetSourceForNotes(string icuLocale, int noteTypeHvo,
			bool createSourceIfNeeded)
		{
			int classId = (ImportTypeEnum == TypeOfImport.Paratext6) ?
				ScrImportP6Project.kClassId : ScrImportSFFiles.kClassId;

			foreach (IScrImportSource source in NoteSourcesOC)
			{
				if (source.ICULocale == icuLocale &&
					source.NoteTypeRAHvo == noteTypeHvo &&
					source.ClassID == classId)
				{
					return source;
				}
			}
			if (!createSourceIfNeeded)
				return null;

			switch (ImportTypeEnum)
			{
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
				{
					ScrImportSFFiles source = new ScrImportSFFiles();
					NoteSourcesOC.Add(source);
					source.ICULocale = icuLocale;
					source.NoteTypeRAHvo = noteTypeHvo;
					return source;
				}
				case TypeOfImport.Paratext6:
				{
					ScrImportP6Project source = new ScrImportP6Project();
					NoteSourcesOC.Add(source);
					source.ICULocale = icuLocale;
					source.NoteTypeRAHvo = noteTypeHvo;
					return source;
				}
				default:
					throw new InvalidOperationException("Unexpected import type");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the file list for the given domain, ICU locale and note type.
		/// </summary>
		/// <param name="domain">The source domain</param>
		/// <param name="icuLocale">The icu locale for the source (ignored for scripture domain)
		/// </param>
		/// <param name="noteTypeHvo">The hvo of the CmAnnotationDefn for the note type
		/// (ignored for back trans and scripture domains)</param>
		/// <param name="createListIfNeeded">True to create the list if it does not already
		/// exist for a given ICU locale and note type</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ScrSfFileList GetFileList(ImportDomain domain, string icuLocale, int noteTypeHvo,
			bool createListIfNeeded)
		{
			Debug.Assert(ImportTypeEnum == TypeOfImport.Other || ImportTypeEnum == TypeOfImport.Paratext5);

			if (icuLocale == null)
				icuLocale = string.Empty;
			switch (domain)
			{
				default:
				case ImportDomain.Main:
					return m_scrFileInfoList;

				case ImportDomain.BackTrans:
				{
					// Look for a back trans source with the given ICU Locale.
					ScrSfFileList btList = m_btFileInfoLists[icuLocale] as ScrSfFileList;
					if (btList == null && createListIfNeeded)
					{
						btList = new ScrSfFileList(m_resolver);
						m_btFileInfoLists[icuLocale] = btList;
					}
					return btList;
				}
				case ImportDomain.Annotations:
				{
					// Look for a annotations source with the given ICU Locale.
					string key = CreateImportSourceKey(icuLocale, noteTypeHvo);
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
		private void SaveMappings(FdoOwningCollection<IScrMarkerMapping> mappingsOC, ScrMappingList mappingInfoList)
		{
			mappingsOC.RemoveAll();
			foreach (ImportMappingInfo info in mappingInfoList)
			{
				IScrMarkerMapping mapping = new ScrMarkerMapping();
				mappingsOC.Add(mapping);
				// The "Default Paragraph Characters" style is not a real style. So, we save it as
				// as separate target type
				// when saving, we want to set the style now for the in-memory info
				if (info.StyleName == FdoResources.DefaultParaCharsStyleName)
					info.MappingTarget = MappingTargetType.DefaultParaChars;
				else if (info.Style == null || info.Style.Name != info.StyleName)
					info.SetStyle((StStyle)m_cache.LangProject.TranslatedScriptureOA.FindStyle(info.StyleName));
				(mapping as ScrMarkerMapping).InitFromImportMappingInfo(info);
			}
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
				null, 0, true));

			// Save the BT sources
			foreach (string icuLocale in m_btFileInfoLists.Keys)
			{
				ScrSfFileList fileList = (ScrSfFileList)m_btFileInfoLists[icuLocale];
				SaveFileList(fileList, (ScrImportSFFiles)GetSource(ImportDomain.BackTrans,
					icuLocale, 0, true));
			}

			// Save the Notes sources
			foreach (string key in m_notesFileInfoLists.Keys)
			{
				string[] keyTokens = key.Split(new char[] {kKeyTokenSeparator}, 2);
				string icuLocale = keyTokens[0] == string.Empty ? null : keyTokens[0];
				int hvoNoteType = Int32.Parse(keyTokens[1]);
				ScrSfFileList fileList = (ScrSfFileList)m_notesFileInfoLists[key];
				SaveFileList(fileList, (ScrImportSFFiles)GetSource(ImportDomain.Annotations,
					icuLocale, hvoNoteType, true));
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

			source.FilesOC.RemoveAll();
			foreach (ScrImportFileInfo fileInfo in files)
			{
				ICmFile file = new CmFile();
				source.FilesOC.Add(file);
				((CmFile)file).SetInternalPath(fileInfo.FileName);
			}
		}
		#endregion
	}
	#endregion
}
