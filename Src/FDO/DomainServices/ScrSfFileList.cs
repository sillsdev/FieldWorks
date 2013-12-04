// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2006' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrSfFileList.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region ScrImportFileEventArgs class and ScrImportFileEventHandler delegate
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Argument type used by the ScrImportFileEventHandler
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrImportFileEventArgs : EventArgs
	{
		private IScrImportFileInfo m_fileinfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileinfo">File that was removed</param>
		/// ------------------------------------------------------------------------------------
		public ScrImportFileEventArgs(IScrImportFileInfo fileinfo)
		{
			m_fileinfo = fileinfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrImportFileInfo FileInfo
		{
			get { return m_fileinfo; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Declaration of delegate for events that need to supply a IScrImportFileInfo
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public delegate void ScrImportFileEventHandler(object sender, ScrImportFileEventArgs e);
	#endregion

	#region class ScrSfFileList
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrSfFileList is an ArrayList that holds <see cref="IScrImportFileInfo"/> objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrSfFileList : ArrayList
	{
		private IOverlappingFileResolver m_resolver;
		private bool m_modified = false;

		/// <summary>
		/// Subscribe to this event if you need to know when a file is removed as a result of
		/// the resolution of an overlap conflict.
		/// </summary>
		public event ScrImportFileEventHandler FileRemoved;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrSfFileList"/> class.
		/// </summary>
		/// <param name="resolver">An Overlapping File Resolver</param>
		/// ------------------------------------------------------------------------------------
		public ScrSfFileList(IOverlappingFileResolver resolver)
		{
			m_resolver = resolver;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrSfFileList"/> class based on an
		/// existing ScrImportSFFiles in the DB.
		/// </summary>
		/// <param name="source">A DB-based collection of Standard Format files</param>
		/// <param name="mappingList">The mapping list to which mappings will be added if any
		/// new ones are found when scanning the files</param>
		/// <param name="importDomain">Main (vernacular Scripture), BT or Annotations</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public ScrSfFileList(IScrImportSFFiles source, ScrMappingList mappingList,
			ImportDomain importDomain, bool scanInlineBackslashMarkers)
			: this(null)
		{
			var deleteList = new List<ICmFile>();
			// Load the files into an in-memory list
			foreach (ICmFile file in source.FilesOC)
			{
				try
				{
					IScrImportFileInfo info = new ScrImportFileInfo(file, mappingList, importDomain,
						source.WritingSystem, source.NoteTypeRA, scanInlineBackslashMarkers);
					Add(info);
				}
				catch (ScriptureUtilsException e)
				{
					var userAction = source.Services.GetInstance<IFdoUI>();
					userAction.DisplayMessage(MessageType.Error, string.Format(ScrFdoResources.kstidImportBadFile, e.Message), Strings.ksErrorCaption, e.HelpTopic);
					//MessageBoxUtils.Show(, "", MessageBoxButtons.OK,
					//	MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, helpFile, HelpNavigator.Topic, e.HelpTopic);
					deleteList.Add(file);
				}
			}

			// delete all of the files that caused errors
			foreach (ICmFile deleteItem in deleteList)
				source.FilesOC.Remove(deleteItem);

			m_modified = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new IScrImportFileInfo to the list
		/// </summary>
		/// <param name="value">a IScrImportFileInfo object</param>
		/// <returns>The <c>ArrayList</c> index at which the value has been added or
		/// -1 if it was not added</returns>
		/// ------------------------------------------------------------------------------------
		public override int Add(object value)
		{
			IScrImportFileInfo fileToAdd = (IScrImportFileInfo)value;
			// TODO: make sure it does not already exist
			if (fileToAdd.IsReadable)
			{
				if (!CheckForOverlaps(fileToAdd))
					return -1;
				for (int index = 0; index < Count; index++)
				{
					IScrImportFileInfo file = (IScrImportFileInfo)this[index];
					if (file.StartRef > fileToAdd.StartRef)
					{
						base.Insert(index, fileToAdd);
						m_modified = true;
						return index;
					}
				}
			}
			m_modified = true;
			return base.Add(fileToAdd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overriden to allow us to raise the FileRemoved event
		/// </summary>
		/// <param name="obj"></param>
		/// ------------------------------------------------------------------------------------
		public override void Remove(object obj)
		{
			base.Remove(obj);
			m_modified = true;
			if (FileRemoved != null)
				FileRemoved(this, new ScrImportFileEventArgs((IScrImportFileInfo)obj));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the file list has any accessible files and get a list of any
		/// inaccessible ones.
		/// </summary>
		/// <param name="filesNotFound">A list of files that couldn't be found.</param>
		/// <returns>true if any SFM files are accessible. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool FilesAreAccessible(ref StringCollection filesNotFound)
		{
			bool found = false;
			foreach (ScrImportFileInfo info in this)
			{
				if (info.RecheckAccessibility())
					found = true;
				else
					filesNotFound.Add(info.FileName);
			}
			return found;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-scan files for mappings. This is mainly to enable us to look for additional
		/// P5-style in-line markers that were not detected previously when the import type is
		/// changing.
		/// </summary>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public void Rescan(bool scanInlineBackslashMarkers)
		{
			foreach (IScrImportFileInfo info in this)
				info.Rescan(scanInlineBackslashMarkers);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve an IScrImportFileInfo from the list at the given index
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new IScrImportFileInfo this[int index]
		{
			get { return (IScrImportFileInfo)base[index]; }
			set { throw new NotImplementedException("Use the Add method instead, please."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the file list has been modified else it is false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Modified
		{
			get { return m_modified; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Overlapping File Resolver
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IOverlappingFileResolver OverlappingFileResolver
		{
			set { m_resolver = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if this file overlaps any other files. Resolve the conflict and
		/// insert this file if it is ok.
		/// </summary>
		/// <param name="fileToAdd"></param>
		/// <returns>true if fileToAdd should be inserted into the list, else false</returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckForOverlaps(IScrImportFileInfo fileToAdd)
		{
			if (m_resolver == null)
				return true;

			List<IScrImportFileInfo> removedList = new List<IScrImportFileInfo>();

			foreach (IScrImportFileInfo file2 in this)
			{
				if (ScrImportFileInfo.CheckForOverlap(fileToAdd, file2))
				{
					IScrImportFileInfo removedFile;
					// TE-4808: First make sure file2 is still accessible. If not, just let
					// the added file replace it quietly.
					if (file2.IsStillReadable)
					{
						removedFile = m_resolver.ChooseFileToRemove(fileToAdd, file2);
						// If we're removing the file being added, no need to continue looking for overlaps
						if (removedFile == fileToAdd)
							return false;
					}
					else
						removedFile = file2;
					removedList.Add(removedFile);
				}
			}

			// Remove all of the overlapping files from the import files collection
			// that we decided we didn't want.
			foreach (IScrImportFileInfo file in removedList)
				Remove(file);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check all files in the given reference range to see if there are any reference
		/// overlaps. If so, resolve the conflict.
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End Reference</param>
		/// ------------------------------------------------------------------------------------
		internal void CheckForOverlappingFilesInRange(ScrReference start, ScrReference end)
		{
			List<IScrImportFileInfo> removedList = new List<IScrImportFileInfo>();
			foreach (IScrImportFileInfo file1 in this)
			{
				if (removedList.Contains(file1))
					continue;
				foreach (ReferenceRange range in file1.BookReferences)
				{
					if (range.OverlapsRange(start, end))
					{
						foreach (IScrImportFileInfo file2 in this)
						{
							if (file1 == file2 || removedList.Contains(file2))
								continue;
							if (ScrImportFileInfo.CheckForOverlap(file1, file2))
							{
								Debug.Assert(m_resolver != null, "Must set OverlappingFileResolver before calling CheckForOverlappingFilesInRange.");
								removedList.Add(m_resolver.ChooseFileToRemove(file1, file2));
							}
						}
					}
				}
			}
			// Remove all of the overlapping files from the import files collection
			// that we decided we didn't want.
			foreach (IScrImportFileInfo file in removedList)
				Remove(file);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a strict scan of all files.
		/// </summary>
		/// <exception cref="ScriptureUtilsException">If the strict file scan finds a data\
		/// error.</exception>
		/// ------------------------------------------------------------------------------------
		internal void PerformStrictScan()
		{
			foreach (IScrImportFileInfo info in this)
				info.PerformStrictScan();
		}
	}
	#endregion
}
