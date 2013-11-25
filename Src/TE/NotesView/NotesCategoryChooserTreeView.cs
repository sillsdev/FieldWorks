// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: NotesCategoryChooserTreeView.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NotesCategoryChooserTreeView : ChooserTreeView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the tree with the annotation categories from the specified cache's
		/// translated scripture object. Then the categories selected in the filter are
		/// checked in the tree. The value returned is a flag indicating whether or not
		/// the filter contained any categories (including whether or not the filter
		/// specifies no categories).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Load(FdoCache cache, ICmFilter filter)
		{
			return Load(cache, filter, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the tree with the annotation categories from the specified cache's
		/// translated scripture object. Then the categories selected in the filter are
		/// checked in the tree. The value returned is a flag indicating whether or not
		/// the filter contained any categories (including whether or not the filter
		/// specifies no categories).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Load(FdoCache cache, ICmFilter filter, Label lblSelectedCategories)
		{
			List<int> initiallySelectedList;

			bool fFilterContainsCategories = InitializeFromFilter(cache, filter,
				out initiallySelectedList);

			base.Load(cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA,
				initiallySelectedList, lblSelectedCategories);

			UpdateSelectedLabel();
			return fFilterContainsCategories;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the list of categories based on categories in the specified filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool InitializeFromFilter(FdoCache cache, ICmFilter filter,
			out List<int> initiallySelectedList)
		{
			initiallySelectedList = new List<int>();

			if (filter == null || filter.RowsOS.Count == 0 || filter.RowsOS[0].CellsOS.Count == 0)
				return false;

			bool fFilterContainsCategories = false;

			// Get the pairs of class ids and flids.
			string[] pairs = filter.ColumnInfo.Split('|');
			Debug.Assert(filter.RowsOS[0].CellsOS.Count == pairs.Length);

			for (int i = 0; i < pairs.Length; i++)
			{
				ICmCell cell = filter.RowsOS[0].CellsOS[i];
				Guid guid;

				// Get the flid for this cell.
				string[] pair = pairs[i].Split(',');
				int flid = 0;
				int.TryParse(pair[1], out flid);

				if (flid == ScrScriptureNoteTags.kflidCategories)
				{
					fFilterContainsCategories = true;
					cell.ParseObjectMatchCriteria();
					if (cell.Contents.RunCount > 1)
					{
						guid = TsStringUtils.GetGuidFromRun(cell.Contents, 1);
						int hvo = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid).Hvo;
						initiallySelectedList.Add(hvo);
					}
				}
			}

			return fFilterContainsCategories;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the specified filter with the selected category hvos.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateFilter(ICmFilter filter)
		{
			ICmCell cell;
			string catColInfo = ScrScriptureNoteTags.kClassId + "," +
				ScrScriptureNoteTags.kflidCategories + "|";

			StringBuilder bldr = new StringBuilder();

			bool fPossibilitiesAdded = false;
			foreach (ICmPossibility matchVal in SelectedPossibilities)
			{
				bldr.Append(catColInfo);
				cell = filter.Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
				filter.RowsOS[0].CellsOS.Add(cell);
				cell.SetObjectMatchCriteria(matchVal, true, false);
				fPossibilitiesAdded = true;
			}
			if (!fPossibilitiesAdded)
			{
				bldr.Append(catColInfo);
				cell = filter.Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
				filter.RowsOS[0].CellsOS.Add(cell);
				cell.SetEmptyObjectMatchCriteria();
			}

			if (bldr.Length > 0)
			{
				catColInfo = "|" + bldr.ToString().TrimEnd('|');
				if (filter.ColumnInfo == null)
					filter.ColumnInfo = catColInfo;
				else
					filter.ColumnInfo += catColInfo;
				filter.ColumnInfo = filter.ColumnInfo.TrimStart('|');
			}
		}
	}
}
