// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CmFilter.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Application;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CmFilters own CmRows, which own CmCells, which actually contain the specifics of the
	/// filter criteria.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class CmFilter
	{
		#region Data members
		// ENHANCE: For now this filter class only supports simple filters (single CmRow owning
		// a single CmCell).
		private List<ICmCell> m_cells = new List<ICmCell>();
		private ICmPossibilitySupplier m_possSupplier = null;
		private List<int> m_filteredFlids = new List<int>();
		private List<CellarPropertyType> m_filteredFieldTypes = new List<CellarPropertyType>();
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
			set
			{
				lock (SyncRoot)
					m_possSupplier = value;
			}
		}
		#endregion

		#region IFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// <value>The name of the filter.</value>
		/// ------------------------------------------------------------------------------------
		public string FilterName
		{
			get { return Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="MatchesCriteria"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitCriteria()
		{
			lock (SyncRoot)
			{
				m_filteredFieldTypes.Clear();
				m_filteredFlids.Clear();
				m_cells.Clear();

				// Get the filter's essential details
				string[] columns = ColumnInfo.Split('|');

				ICmRow row = RowsOS[0];
				for (int i = 0; i < row.CellsOS.Count; i++)
				{
					string[] columnInfo = columns[i].Split(new char[] {','}, 2);
					m_filteredFlids.Add(Int32.Parse(columnInfo[1]));
					m_filteredFieldTypes.Add((CellarPropertyType) m_cache.MetaDataCache.GetFieldType(m_filteredFlids[i]));
					ICmCell tempCell = row.CellsOS[i];
					m_cells.Add(tempCell);

					// Determine the field value this filter wants to match
					switch (m_filteredFieldTypes[i])
					{
						case CellarPropertyType.Integer:
							// ENHANCE: Someday handle prompting user
							Debug.Assert(ShowPrompt == 0, "Can't prompt for int values yet.");
							tempCell.ParseIntegerMatchCriteria();
							break;

						case CellarPropertyType.ReferenceAtomic:
						case CellarPropertyType.ReferenceSequence:
						case CellarPropertyType.ReferenceCollection:
							InitReferenceCriteria(tempCell, m_filteredFlids[i]);
							break;
						default:
							throw new Exception("Attempt to filter on unexpected type of field.");
					}
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
				throw new NotImplementedException("Need to implement prompting the user for filter information");
				// Prompt the user for a possibility.
				//Debug.Assert(m_possSupplier != null);
				//int hvoPossList = m_userView.GetPossibilityListForProperty(filteredFlid);
				//if (hvoPossList == 0)
				//    throw new Exception("Couldn't find appropriate Possibility List to prompt for filter criteria");

				//// The possibility supplier actually displays the Chooser dialog to prompt the user.
				//ICmPossibility pss = m_possSupplier.GetPossibility((CmPossibilityList)Services.GetObject(hvoPossList),
				//    (CmPossibility)Services.GetObject(cell.MatchValue));
				//if (pss == null)
				//    throw new Exception("User cancelled Filter");

				//cell.MatchValue = pss.Hvo;
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
			lock (SyncRoot)
			{
				for (int i = 0; i < m_cells.Count; i++)
				{
					switch (m_filteredFieldTypes[i])
					{
						case CellarPropertyType.Integer:
							if (!m_cells[i].MatchesCriteria(Cache.DomainDataByFlid.get_IntProp(hvoObj, m_filteredFlids[i])))
								return false;
							break;
						case CellarPropertyType.ReferenceAtomic:
							{
								int hvoRefObj = Cache.DomainDataByFlid.get_ObjectProp(hvoObj, m_filteredFlids[i]);
								if (!m_cells[i].MatchesCriteria(hvoRefObj))
									return false;
								break;
							}
						case CellarPropertyType.ReferenceSequence:
						case CellarPropertyType.ReferenceCollection:
							// Look through the sequence/collection for any item that matches
							if (!m_cells[i].MatchesCriteria(((ISilDataAccessManaged) Cache.DomainDataByFlid).VecProp(hvoObj,
																													 m_filteredFlids[i])))
							{
								return false;
							}
							break;
						default:
							throw new Exception("Attempt to filter on unexpected type of field.");
					}
				}
			}
			return true;
		}
		#endregion
	}
}
