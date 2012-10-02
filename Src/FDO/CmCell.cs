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
// File: CmCell.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// CmCell is used for filtering (owned by CmRow).
	/// It implements matching criteria for the filter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CmCell
	{
		/// <summary> the comparison options</summary>
		public enum ComparisionTypes
		{
			/// <summary> </summary>
			kUndefined,
			/// <summary> </summary>
			kEquals,
			/// <summary> </summary>
			kGreaterThanEqual,
			/// <summary> </summary>
			kLessThanEqual,
			/// <summary> </summary>
			kMatches,
			/// <summary> </summary>
			kEmpty,
		}

		#region member data
		/// <summary>The comparison type for this cell</summary>
		protected ComparisionTypes m_comparisonType = ComparisionTypes.kUndefined;
		/// <summary>The match value (this is the min when comparing against a range, which
		/// is currently not supported)</summary>
		protected int m_matchValue;
		/// <summary>List of match values used when matching subitems</summary>
		protected List<int> m_matchValues;
		/// <summary>The maximum match value (only used when comparing against a range, which
		/// is currently not supported)</summary>
		protected int m_maxMatchValue;
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
			get { return m_comparisonType == ComparisionTypes.kEmpty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the match value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MatchValue
		{
			get { return m_matchValue; }
			set
			{
				// Do nothing if the value being set is the same as the existing value
				if (value == m_matchValue)
					return;
				m_matchValue = value;

				switch(m_comparisonType)
				{
					case ComparisionTypes.kEquals:
						SaveIntegerMatchCriteria();
						break;
					case ComparisionTypes.kMatches:
						SaveObjectMatchCriteria();
						InitializeMatchValuesArray();
						break;
					default:
						Debug.Fail("Illegal to set match value without first setting comparison type");
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the comparison.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComparisionTypes ComparisonType
		{
			get { return m_comparisonType; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="comparisonType">Type of camparison</param>
		/// <param name="min">Minimum (or only) match value</param>
		/// <param name="max"></param>
		/// ------------------------------------------------------------------------------------
		public void BuildIntegerMatchCriteria(CmCell.ComparisionTypes comparisonType, int min,
			int max)
		{
			m_comparisonType = comparisonType;
			m_matchValue = min;
			m_maxMatchValue = max;
			SaveIntegerMatchCriteria();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a match criteria that will match on an empty object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BuildObjectMatchCriteria()
		{
			m_comparisonType = ComparisionTypes.kEmpty;
			m_matchValue = 0;
			m_matchSubitems = false;
			m_matchEmpty = true;
			SaveObjectMatchCriteria();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="matchVal">Match value</param>
		/// <param name="fIncludeSubitems">Indicates whether to include the subitems of the
		/// given macthVal (which is assumed to be the HVO of a cmPossibility) when looking for
		/// a match</param>
		/// <param name="fMatchEmpty">Indicates whether an empty collection should be counted
		/// as a match</param>
		/// ------------------------------------------------------------------------------------
		public void BuildObjectMatchCriteria(int matchVal, bool fIncludeSubitems, bool fMatchEmpty)
		{
			m_comparisonType = ComparisionTypes.kMatches;
			m_matchValue = matchVal;
			m_matchSubitems = fIncludeSubitems;
			m_matchEmpty = fMatchEmpty;
			SaveObjectMatchCriteria();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save this set of match criteria.
		/// </summary>
		/// <param name="matchVal">Match value</param>
		/// <param name="fIncludeSubitems">Indicates whether to include the subitems of the
		/// given macthVal (which is assumed to be the Guid of a cmPossibility) when looking for
		/// a match</param>
		/// <param name="fMatchEmpty">Indicates whether an empty collection should be counted
		/// as a match</param>
		/// ------------------------------------------------------------------------------------
		public void BuildObjectMatchCriteria(Guid matchVal, bool fIncludeSubitems, bool fMatchEmpty)
		{
			BuildObjectMatchCriteria(Cache.GetIdFromGuid(matchVal), fIncludeSubitems, fMatchEmpty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the integer match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ParseIntegerMatchCriteria()
		{
			string filterCellContents = Contents.Text;
			int iSpace = filterCellContents.IndexOf(' ');
			string strOperator = filterCellContents.Substring(0, iSpace).Trim();
			string strMatchValue = filterCellContents.Substring(iSpace).Trim();

			switch (strOperator)
			{
				case "=":
					m_comparisonType = ComparisionTypes.kEquals;
					break;
				case ">=":
					m_comparisonType = ComparisionTypes.kGreaterThanEqual;
					break;
				case "<=":
					m_comparisonType = ComparisionTypes.kLessThanEqual;
					break;
				default:
					Debug.Fail("Unexpected operator!");
					break;
			}
			m_matchValue = Int32.Parse(strMatchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the existing Contents to determine the object match criteria and set member
		/// data needed by MatchesCriteria.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ParseObjectMatchCriteria()
		{
			string criteria = Contents.Text;
			m_comparisonType = ComparisionTypes.kUndefined;
			m_matchValue = 0;
			m_matchSubitems = criteria.IndexOf("+subitems") >= 0;
			m_matchEmpty = criteria.IndexOf("Empty") >= 0;

			if (criteria.IndexOf("Matches") >= 0)
				m_comparisonType = ComparisionTypes.kMatches;
			else if (m_matchEmpty)
				m_comparisonType = ComparisionTypes.kEmpty;

			Debug.Assert(m_comparisonType != ComparisionTypes.kUndefined);

			m_matchValues = null;

			// Check to see whether the criteria specifies a specific (default) object.
			if (criteria.IndexOf(StringUtils.kchObject) >= 0)
			{
				ITsTextProps ttp = Contents.UnderlyingTsString.get_Properties(1); //assume second run
				string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (objData[0] == (char)FwObjDataTypes.kodtNameGuidHot)
				{
					Guid guid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
					m_matchValue = m_cache.GetIdFromGuid(guid);
				}


				if (m_matchValue != 0) //Filter is no longer valid if zero, happens when note categories are modified in TLE (TE-8571)
					InitializeMatchValuesArray();
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
			switch (m_comparisonType)
			{
				case ComparisionTypes.kEquals:
					return (val == m_matchValue);
				case ComparisionTypes.kGreaterThanEqual:
					return (val >= m_matchValue);
				case ComparisionTypes.kLessThanEqual:
					return (val <= m_matchValue);
				case ComparisionTypes.kMatches:
					return (m_matchValues.Contains(val));
				default:
					Debug.Assert(false,"Undefined comparison type");
					return false;
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
			switch (m_comparisonType)
			{
				case ComparisionTypes.kEmpty:
					return (val.Length == 0 && m_matchEmpty);

				case ComparisionTypes.kMatches:
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
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the list of match values with the specified object (plus optionally any subitems)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeMatchValuesArray()
		{
			m_matchValues = new List<int>();
			m_matchValues.Add(m_matchValue);
			if (m_matchSubitems)
				AddPossibilitySubitems(m_matchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the integer match criteria in this cell's Contents field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveIntegerMatchCriteria()
		{
			ITsStrFactory factory = TsStrFactoryClass.Create();
			switch (m_comparisonType)
			{
				case ComparisionTypes.kEquals:
					Contents.UnderlyingTsString = factory.MakeString("= " + m_matchValue, m_cache.DefaultUserWs);
					break;
				case ComparisionTypes.kGreaterThanEqual:
					Contents.UnderlyingTsString = factory.MakeString(">= " + m_matchValue, m_cache.DefaultUserWs);
					break;
				case ComparisionTypes.kLessThanEqual:
					Contents.UnderlyingTsString = factory.MakeString("<= " + m_matchValue, m_cache.DefaultUserWs);
					break;
				default:
					Debug.Assert(false,"Undefined comparison type");
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the object match criteria in this cell's Contents field as an ITsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveObjectMatchCriteria()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			string sCriteria = string.Empty;

			if (m_comparisonType == ComparisionTypes.kMatches)
				sCriteria = (m_matchEmpty ? "Empty or Matches " : "Matches ");
			else if (m_matchEmpty)
				sCriteria = "Empty";

			ITsTextProps ttp = StyleUtils.CharStyleTextProps(null, m_cache.DefaultUserWs);
			bldr.Replace(0, 0, sCriteria, ttp);

			if (m_matchValue > 0)
			{
				StringUtils.InsertOrcIntoPara(m_cache.GetGuidFromId(m_matchValue),
					FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
					bldr.Length, m_cache.DefaultUserWs);
			}

			if (m_matchSubitems)
			{
				sCriteria = (m_matchValue > 0) ? " +subitems" : "+subitems";
				bldr.Replace(bldr.Length, bldr.Length, sCriteria, ttp);
			}

			Contents.UnderlyingTsString = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively add any subitems of the given CmPossibility to the list
		/// </summary>
		/// <param name="hvoCmPossibility">The possibility whose subitems are to be added</param>
		/// ------------------------------------------------------------------------------------
		private void AddPossibilitySubitems(int hvoCmPossibility)
		{
			foreach(int hvoSubItem in m_cache.GetVectorProperty(
				hvoCmPossibility, (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, false))
			{
				m_matchValues.Add(hvoSubItem);
				AddPossibilitySubitems(hvoSubItem);
			}
		}
		#endregion
	}
}
