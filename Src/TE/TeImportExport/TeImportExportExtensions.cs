// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	///
	/// </summary>
	public static class TeImportExportExtensions
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the in-memory import projects/files are currently accessible from
		/// this machine.
		/// </summary>
		/// <param name="importSettings">The import settings.</param>
		/// <param name="thingsNotFound">A list of Paratext project IDs or file paths that
		/// could not be found.</param>
		/// <remarks>
		/// For Paratext projects, this will only return true if all projects are accessible.
		/// For Standard Format, this will return true if any of the files are accessible.
		/// We think this might make sense, but we aren't sure why.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public static bool ImportProjectIsAccessible(this IScrImportSet importSettings, out StringCollection thingsNotFound)
		{
			if (importSettings.ImportTypeEnum == TypeOfImport.Paratext6)
				return ParatextProjectsAccessible(importSettings, out thingsNotFound);
			if (importSettings.ImportTypeEnum == TypeOfImport.Other || importSettings.ImportTypeEnum == TypeOfImport.Paratext5)
				return SFProjectFilesAccessible(importSettings, out thingsNotFound);
			thingsNotFound = null;
			return false;
		}

		private static bool ParatextProjectsAccessible(IScrImportSet importSettings, out StringCollection projectsNotFound)
		{
			projectsNotFound = new StringCollection();

			if (importSettings.ParatextScrProj == null)
				return false;

			// Paratext seems to want to have write access to do an import...
			string filename = Path.Combine(ParatextHelper.ProjectsDirectory, importSettings.ParatextScrProj + ".ssf");
			if (!FileUtils.IsFileReadableAndWritable(filename) ||
				!ParatextHelper.GetProjectBooks(importSettings.ParatextScrProj).Any())
			{
				projectsNotFound.Add(importSettings.ParatextScrProj);
			}

			if (importSettings.ParatextBTProj != null)
			{
				filename = Path.Combine(ParatextHelper.ProjectsDirectory, importSettings.ParatextBTProj + ".ssf");
				if (!FileUtils.IsFileReadableAndWritable(filename) ||
					!ParatextHelper.GetProjectBooks(importSettings.ParatextBTProj).Any())
				{
					projectsNotFound.Add(importSettings.ParatextBTProj);
				}
			}

			if (importSettings.ParatextNotesProj != null)
			{
				filename = Path.Combine(ParatextHelper.ProjectsDirectory, importSettings.ParatextNotesProj + ".ssf");
				if (!FileUtils.IsFileReadableAndWritable(filename) ||
					!ParatextHelper.GetProjectBooks(importSettings.ParatextNotesProj).Any())
				{
					projectsNotFound.Add(importSettings.ParatextNotesProj);
				}
			}

			return (projectsNotFound.Count == 0);
		}

		private static bool SFProjectFilesAccessible(IScrImportSet importSettings, out StringCollection filesNotFound)
		{
			filesNotFound = new StringCollection();

			bool fProjectFileFound = false;

			fProjectFileFound |= FilesAreAccessible(importSettings.GetImportFiles(ImportDomain.Main), ref filesNotFound);
			fProjectFileFound |= FilesAreAccessible(importSettings.GetImportFiles(ImportDomain.BackTrans), ref filesNotFound);
			fProjectFileFound |= FilesAreAccessible(importSettings.GetImportFiles(ImportDomain.Annotations), ref filesNotFound);

			return (fProjectFileFound);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the file list has any accessible files and get a list of any
		/// inaccessible ones.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="filesNotFound">A list of files that couldn't be found.</param>
		/// <returns>true if any SFM files are accessible. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool FilesAreAccessible(ImportFileSource source, ref StringCollection filesNotFound)
		{
			bool found = false;
			foreach (IScrImportFileInfo info in source)
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
		/// Gets a list of books that exist for all of the files in this project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <exception cref="NotSupportedException">If project is not a supported type</exception>
		/// ------------------------------------------------------------------------------------
		public static List<int> BooksForProject(this IScrImportSet importSettings)
		{
			Debug.Assert(importSettings.BasicSettingsExist, "Vernacular Scripture project not defined.");
			switch (importSettings.ImportTypeEnum)
			{
				case TypeOfImport.Paratext6:
						// TODO (TE-5903): Check BT and Notes projects as well.
						return ParatextHelper.GetProjectBooks(importSettings.ParatextScrProj).ToList();
				case TypeOfImport.Paratext5:
				case TypeOfImport.Other:
					List<int> booksPresent = new List<int>();
					foreach (IScrImportFileInfo file in importSettings.GetImportFiles(ImportDomain.Main))
						foreach (int iBook in file.BooksInFile)
						{
							if (!booksPresent.Contains(iBook))
								booksPresent.Add(iBook);
						}

					foreach (IScrImportFileInfo file in importSettings.GetImportFiles(ImportDomain.BackTrans))
						foreach (int iBook in file.BooksInFile)
						{
							if (!booksPresent.Contains(iBook))
								booksPresent.Add(iBook);
						}

					foreach (IScrImportFileInfo file in importSettings.GetImportFiles(ImportDomain.Annotations))
						foreach (int iBook in file.BooksInFile)
						{
							if (!booksPresent.Contains(iBook))
								booksPresent.Add(iBook);
						}
					booksPresent.Sort();
					return booksPresent;
				default:
					throw new NotSupportedException("Unexpected type of Import Project");
			}
		}
	}
}
