// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CmCell.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CmCell is used for filtering (owned by CmRow).
	/// It implements matching criteria for the filter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class CmCell
	{
		#region Data Members
		/// <summary>The comparison type for this cell</summary>
		protected ComparisonTypes m_comparisonType = ComparisonTypes.kUndefined;
		/// <summary>The match value</summary>
		protected int m_matchValue;
		/// <summary>List of match values used when matching subitems</summary>
		protected List<int> m_matchValues;
		/// <summary>True to match sub items, false otherwise. (only used for matching sub
		/// possibilities)</summary>
		protected bool m_matchSubitems;
		/// <summary>True to match empty items, false otherwise. (only for matching objects)
		/// </summary>
		protected bool m_matchEmpty;
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the match criteria is set to only match
		/// objects containing an empty reference collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchOnlyEmptyRefCollection
		{
			get
			{
				lock (SyncRoot)
					return m_comparisonType == ComparisonTypes.kEmpty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the match value (for objects, this is the HVO of the match object)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MatchValue
		{
			get
			{
				lock (SyncRoot)
					return m_matchValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the comparison.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComparisonTypes ComparisonType
		{
			get
			{
				lock (SyncRoot)
					return m_comparisonType;
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="comparisonType">Type of camparison</param>
		/// <param name="min">Minimum (or only) match value</param>
		/// ------------------------------------------------------------------------------------
		public void SetIntegerMatchCriteria(ComparisonTypes comparisonType, int min)
		{
			m_comparisonType = comparisonType;
			m_matchValue = min;
			SaveIntegerMatchCriteria();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a match criteria that will match on an empty object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetEmptyObjectMatchCriteria()
		{
			m_comparisonType = ComparisonTypes.kEmpty;
			m_matchValue = 0;
			m_matchSubitems = false;
			m_matchEmpty = true;
			SaveObjectMatchCriteria(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="matchVal">Match value</param>
		/// <param name="fIncludeSubitems">Indicates whether to include the subitems of the
		/// given <paramref name="matchVal"/> when looking for a match</param>
		/// <param name="fMatchEmpty">Indicates whether an empty collection should be counted
		/// as a match</param>
		/// ------------------------------------------------------------------------------------
		public void SetObjectMatchCriteria(ICmPossibility matchVal, bool fIncludeSubitems,
			bool fMatchEmpty)
		{
			m_comparisonType = ComparisonTypes.kMatches;
			m_matchValue = matchVal == null ? 0 : matchVal.Hvo;
			m_matchSubitems = fIncludeSubitems;
			m_matchEmpty = fMatchEmpty;
			SaveObjectMatchCriteria(matchVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the integer match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ParseIntegerMatchCriteria()
		{
			lock (SyncRoot)
			{
				m_comparisonType = ComparisonTypes.kUndefined;
				string filterCellContents = Contents.Text;
				int iSpace = filterCellContents.IndexOf(' ');
				string strOperator = filterCellContents.Substring(0, iSpace).Trim();
				string strMatchValue = filterCellContents.Substring(iSpace).Trim();

				switch (strOperator)
				{
					case "=":
						m_comparisonType = ComparisonTypes.kEquals;
						break;
					case ">=":
						m_comparisonType = ComparisonTypes.kGreaterThanEqual;
						break;
					case "<=":
						m_comparisonType = ComparisonTypes.kLessThanEqual;
						break;
					default:
						Debug.Fail("Unexpected operator!");
						break;
				}
				if (string.IsNullOrEmpty(strMatchValue))
					m_comparisonType = ComparisonTypes.kUndefined;
				else
					m_matchValue = Int32.Parse(strMatchValue);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the object match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ParseObjectMatchCriteria()
		{
			lock (SyncRoot)
			{
				string criteria = Contents.Text;
				m_comparisonType = ComparisonTypes.kUndefined;
				m_matchValue = 0;
				m_matchSubitems = criteria.IndexOf("+subitems") >= 0;
				m_matchEmpty = criteria.IndexOf("Empty") >= 0;

				if (criteria.IndexOf("Matches") >= 0)
					m_comparisonType = ComparisonTypes.kMatches;
				else if (m_matchEmpty)
					m_comparisonType = ComparisonTypes.kEmpty;

				Debug.Assert(m_comparisonType != ComparisonTypes.kUndefined);

				m_matchValues = null;

				// Check to see whether the criteria specifies a specific (default) object.
				if (criteria.IndexOf(StringUtils.kChObject) >= 0)
				{
					ITsTextProps ttp = Contents.get_Properties(1); //assume second run
					string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData[0] == (char)FwObjDataTypes.kodtNameGuidHot)
					{
						Guid guid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						ICmPossibility poss;
						ICmPossibilityRepository repo = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
						if (repo.TryGetObject(guid, out poss))
							m_matchValue = poss.Hvo;
					}

					InitializeMatchValuesArray();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given value matches this cell's match criteria.
		/// </summary>
		/// <param name="val"></param>
		/// <returns>True if a match, False if not</returns>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int val)
		{
			lock (SyncRoot)
			{
				switch (m_comparisonType)
				{
					case ComparisonTypes.kEquals:
						return (val == m_matchValue);
					case ComparisonTypes.kGreaterThanEqual:
						return (val >= m_matchValue);
					case ComparisonTypes.kLessThanEqual:
						return (val <= m_matchValue);
					case ComparisonTypes.kMatches:
						return (m_matchValues.Contains(val));
					default:
						Debug.Assert(false, "Undefined comparison type");
						return false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if anything in the given array matches this cell's match criteria.
		/// </summary>
		/// <param name="val"></param>
		/// <returns>True if a match, False if not</returns>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int[] val)
		{
			lock (SyncRoot)
			{
				switch (m_comparisonType)
				{
					case ComparisonTypes.kEmpty:
						return (val.Length == 0 && m_matchEmpty);

					case ComparisonTypes.kMatches:
						Debug.Assert(m_matchValues != null,
									 "Illegal to call MatchesCriteria without first setting a match values list");
						foreach (int hvoRefObj in val)
						{
							if (m_matchValues.Contains(hvoRefObj))
								return true;
						}
						return (val.Length == 0 && m_matchEmpty);
					default:
						Debug.Assert(false, "Undefined comparison type");
						return false;
				}
			}
		}
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the list of match values with the specified object (plus optionally any subitems)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeMatchValuesArray()
		{
			if (m_matchValue == 0)
			{
				// Filter is no longer valid if zero, which can happen if annotation
				// categories are modified in TLE (TE-8571)
				return;
			}
			m_matchValues = new List<int>();
			m_matchValues.Add(m_matchValue);
			if (m_matchSubitems)
				AddPossibilitySubitems(Services.GetInstance<ICmPossibilityRepository>().GetObject(m_matchValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the integer match criteria in this cell's Contents field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveIntegerMatchCriteria()
		{
			ITsStrFactory factory = Cache.TsStrFactory;
			string sFmtCriteria = "{0} " + m_matchValue;
			string sOperator;

			switch (m_comparisonType)
			{
				case ComparisonTypes.kEquals: sOperator = "="; break;
				case ComparisonTypes.kGreaterThanEqual: sOperator = ">="; break;
				case ComparisonTypes.kLessThanEqual: sOperator = "<="; break;
				default:
					throw new InvalidOperationException("Unexpected comparison type");
			}
			Contents = factory.MakeString(string.Format(sFmtCriteria, sOperator), m_cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the object match criteria in this cell's Contents field as an ITsString.
		/// </summary>
		/// <param name="matchVal">The match value.</param>
		/// ------------------------------------------------------------------------------------
		private void SaveObjectMatchCriteria(ICmPossibility matchVal)
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			string sCriteria = string.Empty;

			if (m_comparisonType == ComparisonTypes.kMatches)
				sCriteria = (m_matchEmpty ? "Empty or Matches " : "Matches ");
			else if (m_matchEmpty)
				sCriteria = "Empty";

			ITsTextProps ttp = StyleUtils.CharStyleTextProps(null, m_cache.DefaultUserWs);
			bldr.Replace(0, 0, sCriteria, ttp);
			if (matchVal != null)
				bldr.AppendOrc(matchVal.Guid, FwObjDataTypes.kodtNameGuidHot, m_cache.DefaultUserWs);

			if (m_matchSubitems)
			{
				sCriteria = (matchVal != null ? " " : string.Empty) + "+subitems";
				bldr.Replace(bldr.Length, bldr.Length, sCriteria, ttp);
			}

			Contents = bldr.GetString();

			InitializeMatchValuesArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively add any subitems of the given CmPossibility to the list
		/// </summary>
		/// <param name="possibility">The possibility whose subitems are to be added</param>
		/// ------------------------------------------------------------------------------------
		private void AddPossibilitySubitems(ICmPossibility possibility)
		{
			foreach(ICmPossibility subItem in possibility.SubPossibilitiesOS)
			{
				m_matchValues.Add(subItem.Hvo);
				AddPossibilitySubitems(subItem);
			}
		}
		#endregion
	}
}
