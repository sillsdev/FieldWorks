// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CmFilter.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CmFilters own CmRows, which own CmCells, which actually contain the specifics of the
	/// filter criteria.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CmFilter: IFilter
	{
		#region Data members
		// ENHANCE: For now this filter class only supports simple filters (single CmRow owning
		// a single CmCell).
		private List<ICmCell> m_cells = new List<ICmCell>();
		private ICmPossibilitySupplier m_possSupplier = null;
		private IUserView m_userView = null;
		private List<int> m_filteredFlids = new List<int>();
		private List<FieldType> m_filteredFieldTypes = new List<FieldType>();
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// An object that can supply a chosen CmPossibility to match on, given a possibility
		/// list (typically the Chooser dialog)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibilitySupplier PossibilitySupplier
		{
			set {m_possSupplier = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// UserView being filtered. If this filter filters on a field whose possible values
		/// come from a possibility list, the user view will be used to determine which
		/// possibility list should be used to retrieve the possibility to match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IUserView UserView
		{
			get {return m_userView;}
			set {m_userView = value;}
		}
		#endregion

		#region IFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="MatchesCriteria"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitCriteria()
		{
			m_filteredFieldTypes.Clear();
			m_filteredFlids.Clear();
			m_cells.Clear();

			// Get the filter's essential details
			string[] columns = ColumnInfo.Split(new char[] { '|' });

			ICmRow row = RowsOS[0];
			for (int i = 0; i < row.CellsOS.Count; i++)
			{
				string[] columnInfo = columns[i].Split(new char[] { ',' }, 2);
				m_filteredFlids.Add(Int32.Parse(columnInfo[1]));
				m_filteredFieldTypes.Add(m_cache.GetFieldType(m_filteredFlids[i]));
				ICmCell tempCell = row.CellsOS[i];
				m_cells.Add(tempCell);

				// Determine the field value this filter wants to match
				switch (m_filteredFieldTypes[i])
				{
					case FieldType.kcptInteger:
						// ENHANCE: Someday handle prompting user
						Debug.Assert(ShowPrompt == 0, "Can't prompt for int values yet.");
						tempCell.ParseIntegerMatchCriteria();
						break;

					case FieldType.kcptReferenceAtom:
					case FieldType.kcptReferenceSequence:
					case FieldType.kcptReferenceCollection:
						InitReferenceCriteria(tempCell, m_filteredFlids[i]);
						break;
					default:
						throw new Exception("Attempt to filter on unexpected type of field.");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches in reference collections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitReferenceCriteria(ICmCell cell, int filteredFlid)
		{
			cell.ParseObjectMatchCriteria();

			if (cell is CmCell && ((CmCell)cell).MatchOnlyEmptyRefCollection)
				return;

			// Do we need to prompt the user for the value he wants to match?
			if (cell.MatchValue == 0 || ShowPrompt == 1)
			{
				// Prompt the user for a possibility.
				Debug.Assert(m_userView != null);
				Debug.Assert(m_possSupplier != null);
				int hvoPossList = m_userView.GetPossibilityListForProperty(filteredFlid);
				if (hvoPossList == 0)
					throw new Exception("Couldn't find appropriate Possibility List to prompt for filter criteria");

				// The possibility supplier actually displays the Chooser dialog to prompt the user.
				int hvoPoss = m_possSupplier.GetPossibility(
					new CmPossibilityList(m_cache, hvoPossList), cell.MatchValue);

				if (hvoPoss == 0)
					throw new Exception("User cancelled Filter");

				cell.MatchValue = hvoPoss;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			for(int i = 0; i < m_cells.Count; i++)
			{
				switch (m_filteredFieldTypes[i])
				{
					case FieldType.kcptInteger:
						if(!m_cells[i].MatchesCriteria(m_cache.GetIntProperty(hvoObj, m_filteredFlids[i])))
							return false;
						break;
					case FieldType.kcptReferenceAtom:
						{
							int hvoRefObj = m_cache.GetObjProperty(hvoObj, m_filteredFlids[i]);
							if(!m_cells[i].MatchesCriteria(hvoRefObj))
								return false;
							break;
						}
					case FieldType.kcptReferenceSequence:
					case FieldType.kcptReferenceCollection:
						// Look through the sequence/collection for any item that matches
						if (!m_cells[i].MatchesCriteria(m_cache.GetVectorProperty(hvoObj,
							m_filteredFlids[i], false)))
						{
							return false;
						}
						break;
					default:
						throw new Exception("Attempt to filter on unexpected type of field.");
				}
			}
			return true;
		}
		#endregion
	}
}
