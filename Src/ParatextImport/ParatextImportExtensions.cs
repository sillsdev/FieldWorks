// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace ParatextImport
{
	/// <summary>
	///
	/// </summary>
	public static class ParatextImportExtensions
	{
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

#if DEBUG
		/// <summary />
		public static void AssertValid(this List<OverlapInfo> items)
		{
			if (items.Count <= 1)
				return;

			HashSet<IScrTxtPara> paras = new HashSet<IScrTxtPara>();
			foreach (IScrTxtPara para in items.Select(info => (IScrTxtPara)info.myObj))
			{
				if (paras.Contains(para))
					Debug.Fail("A ParaStructure cluster must have a different paragraph in each ScrVerse");
				paras.Add(para);
			}
		}
#endif
	}
}
