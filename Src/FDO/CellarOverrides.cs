// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CellarOverrides.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// This file holds the overrides of the generated classes for the Cellar module.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices; // needed for Marshal
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace  SIL.FieldWorks.FDO.Cellar
{
	/// <summary>
	/// Just a shell class for containing runtime Switches for controling the diagnostic output.
	/// </summary>
	public class RuntimeSwitches
	{
		/// Tracing variable - used to control when and what is output to the debug and trace listeners
		public static TraceSwitch CellarTimingSwitch = new TraceSwitch("CellarTiming", "Used for diagnostic timing output", "Off");
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CmProject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual DateTime DateModified
		{
			get { return DateModified_Generated; }
			set { DateModified_Generated = value; }
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmMajorObject
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return Name.AnalysisDefaultWritingSystem;
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				string name = Name.AnalysisDefaultWritingSystem;
				int ws = m_cache.DefaultAnalWs;
				if (name == null || name == String.Empty)
				{
					name = Name.VernacularDefaultWritingSystem;
					ws = m_cache.DefaultVernWs;
				}
				return tsf.MakeString(name, ws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for a header/footer set with the specified name in the DB
		/// </summary>
		/// <param name="name">The name of the header/footer set</param>
		/// <returns>
		/// The header/footer set with the given name if it was found, null otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IPubHFSet FindHeaderFooterSetByName(string name)
		{
			foreach (IPubHFSet hfSet in HeaderFooterSetsOC)
			{
				if (hfSet.Name == name)
					return hfSet;
			}
			return null;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmPossibilityList
	{
		private Dictionary<int, Dictionary<string, int>> m_possibilityMap;

		/// <summary>
		/// Get all possibilities, recursively, that are ultimately owned by the list.
		/// </summary>
		public Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get
			{
				Set<ICmPossibility> set = new Set<ICmPossibility>();
				foreach (ICmPossibility pss in PossibilitiesOS)
				{
					set.Add(pss);
					set.AddRange(pss.ReallyReallyAllPossibilities);
				}
				return set;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="alIDs"></param>
		/// <returns></returns>
		public static CmPossibilityListCollection Load(FdoCache fdoCache, Set<int> alIDs)
		{
			CmPossibilityListCollection cpl = new CmPossibilityListCollection(fdoCache);
			foreach (int hvo in alIDs)
				cpl.Add(new CmPossibilityList(fdoCache, hvo));
			return cpl;
		}

		/// <summary>
		/// The type of items contained in this list.
		/// </summary>
		/// <param name="stringTbl">string table containing mappings for list item names.</param>
		/// <returns></returns>
		public string ItemsTypeName(StringTable stringTbl)
		{
			string owningFieldName = Cache.MetaDataCacheAccessor.GetFieldName((uint)this.OwningFlid);
			string itemsTypeName = stringTbl.GetString(owningFieldName, "PossibilityListItemTypeNames");
			if (itemsTypeName != "*" + owningFieldName + "*")
				return itemsTypeName;
			if (this.PossibilitiesOS.Count > 0)
				return stringTbl.GetString(this.PossibilitiesOS[0].GetType().Name, "ClassNames");
			else
				return itemsTypeName;
		}

		/// <summary>
		/// Look up a possibility in a list having a known GUID value
		/// </summary>
		/// <param name="guid">The GUID value</param>
		/// <returns>the possibility</returns>
		public ICmPossibility LookupPossibilityByGuid(Guid guid)
		{
			foreach (ICmPossibility poss in PossibilitiesOS)
			{
				if (poss.Guid == guid)
					return poss;
			}
			throw new ArgumentException("List does not contain the requested CmPossibility.");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public int FindOrCreatePossibility(string possibilityPath, int ws)
		{
			return FindOrCreatePossibility(possibilityPath, ws, true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Using the possibility name and writing system, find a possibility, or create a new
		/// one if it doesn't exist.
		/// </summary>
		/// <param name="possibilityPath">name of the possibility path delimited by ORCs (if
		/// the fFullPath is <c>false</c></param>
		/// <param name="ws">writing system</param>
		/// <param name="fFullPath">whether the full path is provided (possibilities that are
		/// not on the top level will provide the full possibilityPath with ORCs between the
		/// possibility names of each level)</param>
		/// -----------------------------------------------------------------------------------
		public int FindOrCreatePossibility(string possibilityPath, int ws, bool fFullPath)
		{
			if (m_possibilityMap == null)
				m_possibilityMap = new Dictionary<int, Dictionary<string, int>>();

			if (!m_possibilityMap.ContainsKey(ws))
				CacheNoteCategories(PossibilitiesOS, ws);

			int hvo;
			if (!fFullPath)
			{
				// The category name is not found in the hash table for the first level and
				// only the category name is provided. So... we will search through subpossibilities
				// for the first name that matches. If we don't find a matching name, we will
				// create a new possibility.
				hvo = FindPossibilityByName(PossibilitiesOS, possibilityPath, ws);
				if (hvo > 0)
					return hvo;
			}

			Dictionary<string, int> wsHashTable;
			if (m_possibilityMap.TryGetValue(ws, out wsHashTable))
			{
				if (wsHashTable.TryGetValue(possibilityPath, out hvo))
					return hvo;

				// Parse the category path and create any missing categories.
				FdoOwningSequence<ICmPossibility> possibilityList = PossibilitiesOS;
				int level = 1;
				foreach (string strName in possibilityPath.Split(StringUtils.kchObject))
				{
					string possibilityKey = GetPossibilitySubPath(possibilityPath, level);
					ICmPossibility possibility;
					if (!wsHashTable.TryGetValue(possibilityKey, out hvo))
					{
						// Category is missing, so create a new one.
						possibility = new CmPossibility();
						possibilityList.Append(possibility);
						possibility.Abbreviation.SetAlternative(strName, ws);
						possibility.Name.SetAlternative(strName, ws);
						m_possibilityMap[ws][possibilityKey] = hvo = possibility.Hvo;
					}
					else
					{
						// Get the category for this level that already exists.
						possibility = new CmPossibility(m_cache, hvo);
					}

					// Set the current possibility list to the category subpossibility list
					// as we continue our search.
					possibilityList = possibility.SubPossibilitiesOS;
					level++;
				}

				if (hvo != -1)
					return hvo; // only return the hvo if we were able to create a valid one
			}

			throw new InvalidProgramException(
				"Unable to create a dictionary for the writing system in the annotation category hash table");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the name of the possibility by name only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int FindPossibilityByName(FdoOwningSequence<ICmPossibility> possList, string possibilityPath, int ws)
		{

			foreach (CmPossibility poss in possList)
			{
				int dummyWs;
				if (possibilityPath.Equals(poss.Name.GetAlternativeOrBestTss(ws, out dummyWs).Text) ||
					possibilityPath.Equals(poss.Abbreviation.GetAlternativeOrBestTss(ws, out dummyWs).Text))
				{
					return poss.Hvo;
				}

				// Search any subpossibilities of this possibility.
				int hvo = FindPossibilityByName(poss.SubPossibilitiesOS, possibilityPath, ws);
				if (hvo != -1)
					return hvo;
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the portion of the possibilityPath as specified by the level.
		/// </summary>
		/// <param name="possibilityPath">The possibility path with levels delimited by ORCs.</param>
		/// <param name="level">The level to which we want the path. For example, if the path
		/// contains three levels and the path is only needed to level two, category level should
		/// be two.</param>
		/// ------------------------------------------------------------------------------------
		private static string GetPossibilitySubPath(string possibilityPath, int level)
		{
			StringBuilder strBldr = new StringBuilder();
			int iLevel = 0;
			foreach (string possibility in possibilityPath.Split(StringUtils.kchObject))
			{
				if (iLevel < level)
				{
					if (strBldr.Length > 0)
						strBldr.Append(StringUtils.kchObject); // category previously added to string, so add delimiter

					strBldr.Append(possibility);
				}
				else
					break;
				iLevel++;
			}

			return strBldr.ToString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Fill the possibility map from name to HVO for looking up possibilities.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void CacheNoteCategories(FdoOwningSequence<ICmPossibility> possibilityList, int ws)
		{
			if (!m_possibilityMap.ContainsKey(ws))
				m_possibilityMap[ws] = new Dictionary<string, int>();

			string s;
			foreach (ICmPossibility poss in possibilityList)
			{
				string sNotFound = poss.Abbreviation.NotFoundTss.Text;

				s = poss.AbbrevHierarchyString;
				if (!string.IsNullOrEmpty(s) && s != sNotFound)
					m_possibilityMap[ws][s] = poss.Hvo;

				s = poss.NameHierarchyString;
				if (!string.IsNullOrEmpty(s) && s != sNotFound)
					m_possibilityMap[ws][s] = poss.Hvo;

				CacheNoteCategories(poss.SubPossibilitiesOS, ws);
			}
		}
	}


	/// <summary>
	///
	/// </summary>
	public partial class CmPossibility
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this constructor where you want to load an existing object based on a guid.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="guid"></param>
		/// ------------------------------------------------------------------------------------
		public CmPossibility(FdoCache fcCache, Guid guid)
			:base()
		{
			InitExisting(fcCache, guid);
		}

		/// <summary>
		/// Init a CmPossibility (or derived class) based on a guid...typically one of our fixed guids used for
		/// objects we must identify absolutely.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="guid"></param>
		public void InitExisting(FdoCache fcCache, Guid guid)
		{
			int hvo = fcCache.GetIdFromGuid(guid);
			if (hvo == 0)
				throw new Exception("Failed to find object with guid " + guid);
			InitExisting(fcCache, hvo, false, false);
		}

		/// <summary>
		/// Get all possibilities, recursively, that are ultimately owned by the possibility.
		/// </summary>
		public Set<ICmPossibility> ReallyReallyAllPossibilities
		{
			get
			{
				Set<ICmPossibility> set = new Set<ICmPossibility>();
				foreach (ICmPossibility pss in SubPossibilitiesOS)
				{
					set.Add(pss);
					set.AddRange(pss.ReallyReallyAllPossibilities);
				}
				return set;
			}
		}

		/// <summary>
		/// Gets the list that ultimately owns this CmPossibility.
		/// </summary>
		public ICmPossibilityList OwningList
		{
			get
			{
				int ownerHvo = OwnerHVO;
				if (m_cache.GetClassOfObject(ownerHvo) == CmPossibilityList.kClassId)
					return new CmPossibilityList(m_cache, ownerHvo) as ICmPossibilityList;
				else
					return OwningPossibility.OwningList;
			}
		}

		/// <summary>
		/// Gets the CmPossibility that owns this CmPossibility,
		/// or null, if it is owned by the list.
		/// </summary>
		public ICmPossibility OwningPossibility
		{
			get
			{
				int ownerHvo = OwnerHVO;
				if (m_cache.IsSameOrSubclassOf(m_cache.GetClassOfObject(ownerHvo), CmPossibility.kClassId))
					return new CmPossibility(m_cache, ownerHvo) as ICmPossibility;
				else
					return null;
			}
		}

		/// <summary>
		/// Gets the CmPossibility that is owned by the list.
		/// </summary>
		/// <remarks>
		/// It may return itself, if it is owned by the list,
		/// otherwise, it will move up the ownership chain to find the one
		/// that is owned by the list.
		/// </remarks>
		public ICmPossibility MainPossibility
		{
			get
			{
				ICmPossibility owningPoss = OwningPossibility;
				if (owningPoss == null)
					return this;
				else
					return owningPoss.MainPossibility;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility name hierarchy in the default analysis
		/// writing system with the top-level category first and the last item at the end of the
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NameHierarchyString
		{
			get
			{
				return GetHierarchyString(true, m_cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility abbreviation hierarchy in the default
		/// analysis writing system with the top-level category first and the last item at the
		/// end of the string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AbbrevHierarchyString
		{
			get
			{
				return GetHierarchyString(false, m_cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ORC-delimited string of the possibility hierarchy with the top-level possibility
		/// first and the last item at the end of the string.
		/// </summary>
		/// <param name="fGetName">if <c>true</c> get the name of the possibility; if <c>false</c>
		/// get the abbreviation for the possibility.</param>
		/// <param name="ws">Writing system of possibility name or abbreviation.</param>
		/// ------------------------------------------------------------------------------------
		private string GetHierarchyString(bool fGetName, int ws)
		{
			StringBuilder strBldr = new StringBuilder();
			string strPossibility = null;

			// Go through the hierarchy getting the name of categories until the top-level
			// CmPossibility is found.
			ICmObject categoryObj = this;
			int dummy;
			while (categoryObj is ICmPossibility)
			{
				if (fGetName)
					strPossibility = ((ICmPossibility)categoryObj).Name.GetAlternativeOrBestTss(ws, out dummy).Text;
				else
					strPossibility = ((ICmPossibility)categoryObj).Abbreviation.GetAlternativeOrBestTss(ws, out dummy).Text;
				bool fTextFound = !strPossibility.Equals(Name.NotFoundTss.Text) && !string.IsNullOrEmpty(strPossibility);
				if (fTextFound)
					strBldr.Insert(0, strPossibility);

				categoryObj = categoryObj.Owner;

				if (categoryObj is ICmPossibility && fTextFound)
					strBldr.Insert(0, StringUtils.kchObject); // character delimiter (ORC)
			}

			return strBldr.ToString();
		}
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return ShortNameTSS.Text;
			}
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public override ITsString ShortNameTSS
		{
			get
			{
				return BestAnalysisName(m_cache, Hvo);
			}
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static ITsString BestAnalysisName(FdoCache cache, int hvo)
		{
			return BestAlternative(cache, hvo,
				LangProject.kwsFirstAnal,
				(int)CmPossibility.CmPossibilityTags.kflidName, Strings.ksQuestions);
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static ITsString BestAnalysisOrVernName(FdoCache cache, int hvo)
		{
			// JohnT: how about this for a default?
			// "a " + this.GetType().Name + " with no name"
			return BestAnalysisOrVernName(cache, hvo, Strings.ksQuestions);
		}

		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="defValue">string to use (in default user ws) if hvo is zero</param>
		/// <returns></returns>
		public static ITsString BestAnalysisOrVernName(FdoCache cache, int hvo, string defValue)
		{
			return BestAlternative(cache, hvo,
				LangProject.kwsFirstAnalOrVern,
				(int)CmPossibility.CmPossibilityTags.kflidName, defValue);
		}

		private static ITsString BestAlternative(FdoCache cache, int hvo, int wsMagic, int flid, string defValue)
		{
			ITsString tss = null;
			if (hvo != 0)
				tss = cache.LangProject.GetMagicStringAlt(wsMagic, hvo, flid);
			if (tss == null || tss.Length == 0)
				tss = cache.MakeUserTss(defValue);
			return tss;
		}
		/// <summary>
		/// Return the name for the specified CmPossibility (or '???' if it has no name
		/// or hvo is 0). Return the best available analysis or vernacular name (in that order).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static ITsString BestAnalysisOrVernAbbr(FdoCache cache, int hvo)
		{
			ITsString tss = null;
			if (hvo != 0)
			{
				tss = cache.LangProject.GetMagicStringAlt(
					LangProject.kwsFirstAnalOrVern,
					hvo, (int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
			}
			if (tss == null || tss.Length == 0)
			{
				tss = cache.MakeUserTss(Strings.ksQuestions);
				// JohnT: how about this?
				//return cache.MakeUserTss("a " + this.GetType().Name + " with no name");
			}
			return tss;
		}

		/// <summary>
		/// Return the Abbreviation for the specified CmPossibility if one exists for ws (or '???' if it has no name
		/// or hvo is 0).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public static ITsString TSSAbbrforWS(FdoCache cache, int hvo, int ws)
		{
			ITsString tss = null;
			if (hvo != 0)
			{
				tss = cache.LangProject.GetMagicStringAlt(ws,
					hvo, (int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
			}
			if (tss == null || tss.Length == 0)
			{
				tss = cache.MakeUserTss(Strings.ksQuestions);
			}
			return tss;
		}

		/// <summary>
		/// Gets a TsString that represents the shortname of this object, preferring vernacular names.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortName string.
		/// </remarks>
		public virtual ITsString VernShortNameTss
		{
			get
			{
				return BestVernOrAnalysisName(m_cache, Hvo);
			}
		}

		/// <summary>
		/// Similarly get a name in some WS, preferring vernacular.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static ITsString BestVernOrAnalysisName(FdoCache cache, int hvo)
		{
			ITsString tss = null;
			if (hvo != 0)
			{
				tss = cache.LangProject.GetMagicStringAlt(
					LangProject.kwsFirstVernOrAnal,
					hvo, (int)CmPossibility.CmPossibilityTags.kflidName);
			}
			if (tss == null || tss.Length == 0)
			{
				tss = cache.MakeUserTss(Strings.ksQuestions);
				// JohnT: how about this?
				//return cache.MakeUserTss("a " + this.GetType().Name + " with no name");
			}
			return tss;
		}

		/// <summary>
		/// Abbreviation and Name with hyphen between.
		/// </summary>
		public string AbbrAndName
		{
			get
			{
				return AbbrAndNameTSS.Text;
			}
		}

		/// <summary>
		/// Abbreviation and Name with hyphen between.
		/// </summary>
		public ITsString AbbrAndNameTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tisb.AppendTsString(Abbreviation.BestAnalysisAlternative);
				tisb.AppendTsString(tsf.MakeString(Strings.ksNameAbbrSep,
					m_cache.LangProject.DefaultUserWritingSystem));
				tisb.AppendTsString(Name.BestAnalysisAlternative);
				return  tisb.GetString();
			}
		}

		/// <summary>
		/// LIFT (WeSay) doesn't put hyphen between the abbreviation and the name.
		/// </summary>
		public string LiftAbbrAndName
		{
			get
			{
				return String.Format("{0} {1}",
					Abbreviation.BestAnalysisAlternative.Text,
					Name.BestAnalysisAlternative.Text);
			}
		}

		/// <summary>
		/// Overridden to handle Type property.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)CmPossibility.CmPossibilityTags.kflidRestrictions:
					return m_cache.LangProject.RestrictionsOA;
				case (int)CmPossibility.CmPossibilityTags.kflidConfidence:
					return m_cache.LangProject.ConfidenceLevelsOA;
				case (int)CmPossibility.CmPossibilityTags.kflidStatus:
					return m_cache.LangProject.AnalysisStatusOA;
				case (int)CmPossibility.CmPossibilityTags.kflidResearchers:
					return m_cache.LangProject.PeopleOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Override the method to see if the objSrc owns 'this',
		/// in which case, we will need to move 'this' to a safe spot where it won't be deleted,
		/// when objSrc gets deleted.
		/// </summary>
		/// <param name="objSrc"></param>
		/// <param name="fLoseNoStringData"></param>
		public override void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			MoveIfNeeded(objSrc as ICmPossibility);

			base.MergeObject(objSrc, fLoseNoStringData);
		}

		/// <summary>
		/// Move 'this' to a safe place, if needed.
		/// </summary>
		/// <param name="possSrc"></param>
		/// <remarks>
		/// When merging or moving a CmPossibility, the new home ('this') may actually be owned by
		/// the other CmPossibility, in which case 'this' needs to be relocated, before the merge/move.
		/// </remarks>
		/// <returns>
		/// 1. The new owner (CmPossibilityList or CmPossibility), or
		/// 2. null, if no move was needed.
		/// </returns>
		public ICmObject MoveIfNeeded(ICmPossibility possSrc)
		{
			Debug.Assert(possSrc != null);
			ICmObject newOwner = null;
			ICmPossibility possOwner = this;
			while (true)
			{
				possOwner = possOwner.OwningPossibility;
				if (possOwner == null || possOwner.Equals(possSrc))
					break;
			}
			if (possOwner != null && possOwner.Equals(possSrc))
			{
				// Have to move 'this' to a safe location.
				possOwner = possSrc.OwningPossibility;
				if (possOwner != null)
				{
					possOwner.SubPossibilitiesOS.Append(this);
					newOwner = possOwner;
				}
				else
				{
					// Move it clear up to the list.
					ICmPossibilityList list = possSrc.OwningList;
					list.PossibilitiesOS.Append(this);
					newOwner = list;
				}
			}
			// 'else' means there is no ownership issues to using normal merging/moving.

			return newOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the cache with all of the data needed to display a hierarchical list, plus
		/// strings which will be needed by the ShortName property (which may include
		/// abbreviation). For performance only.
		/// Using this preload plus keeping the tree view from inadvertently loading extra data
		/// reduced the number of queries to start LexText on the Semantic Domain list from
		/// around 19,000 queries to 200.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoList">The hvo list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Set<int> PreLoadList(FdoCache cache, int hvoList)
		{
			DateTime dt1 = DateTime.Now;
			int tc1 = System.Environment.TickCount;

			int flidPssl = (int)CellarModuleDefns.kflidCmPossibilityList_Possibilities;
			int flidPss = (int)CellarModuleDefns.kflidCmPossibility_SubPossibilities;
			// Note: This query will not run from code with nocount off.
			string squery =
				" declare @fIsNocountOn int" +
				" set @fIsNocountOn = @@options & 512" +
				" if @fIsNocountOn = 0 set nocount on" +
				" select goi.Owner$, goi.OwnFlid$, goi.id, goi.Class$, goi.UpdStmp, cn.Txt, ca.Txt " +
				" from dbo.fnGetOwnedIds(" + hvoList + ", " + flidPssl + ", " + flidPss + ") goi" +
				" left outer join CmPossibility_Name cn on cn.obj = goi.id and cn.ws = " + cache.DefaultAnalWs +
				" left outer join CmPossibility_Abbreviation ca on ca.obj = goi.id and ca.ws = " + cache.DefaultAnalWs +
				" order by goi.Owner$, goi.OwnOrd$ " +
				" if @fIsNocountOn = 0 set nocount off";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctFlid, 1, 0, 0);	// flid for vector
			dcs.Push((int)DbColType.koctObjVecOwn, 1, 0, 0);
			dcs.Push((int)DbColType.koctInt, 3, (int)CmObjectFields.kflidCmObject_Class, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 3, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 3,
				(int)CmPossibility.CmPossibilityTags.kflidName, cache.DefaultAnalWs);
			dcs.Push((int)DbColType.koctMltAlt, 3,
				(int)CmPossibility.CmPossibilityTags.kflidAbbreviation, cache.DefaultAnalWs);
			cache.LoadData(squery, dcs, 0);

			// Get a list of all of the items that we've already cached.
			ISilDataAccess sda = cache.MainCacheAccessor;
			int beginSize = 500;
			Set<int> alHvos = new Set<int>(beginSize);
			CmPossibility.GetItems(hvoList, flidPssl, alHvos, sda, true);

			// only do this if the cellar timing switch is info or verbose
			if (RuntimeSwitches.CellarTimingSwitch.TraceInfo)
			{
			int tc2 = System.Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "PreLoadList for CmPossibility took " + (tc2 - tc1) + " ticks," +
				" or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.CellarTimingSwitch.DisplayName);
			}

			return alHvos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get Subpossibilities of the CmPossibility.
		/// For Performance (used in conjunction with PreLoadList).
		/// </summary>
		/// <returns>Set of subpossibilities</returns>
		/// ------------------------------------------------------------------------------------
		public Set<int> SubPossibilities()
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int beginSize = 500;
			Set<int> subs = new Set<int>(beginSize);
			int flidPss = (int)CellarModuleDefns.kflidCmPossibility_SubPossibilities;
			CmPossibility.GetItems(this.Hvo, flidPss, subs, sda, false);
			return subs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all the possibilities (or subpossibiities) of the owner and (optionally their
		/// subpossibilities).
		/// </summary>
		/// <param name="hvoOwn">The hvo own.</param>
		/// <param name="flid">flid for Possibilities for a CmPossibilityList, or
		/// SubPossibilities for a CmPossibility</param>
		/// <param name="list">list of ids to return.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="fGetAllDescendants">if true, recurse to get ALL subpossibilities.</param>
		/// ------------------------------------------------------------------------------------
		private static void GetItems(int hvoOwn, int flid, Set<int> list, ISilDataAccess sda,
			bool fGetAllDescendants)
		{
			int flidPss = (int)CellarModuleDefns.kflidCmPossibility_SubPossibilities;
			int chvo = 0;
			// Note, calling get_VecSize directly will result in a SQL query for all of the nodes of
			// the tree to verify that they are zero. This generates around 1400 queries for the
			// semantic domain list during loading. These will likely never be needed for anything
			// else, so it simply wastes time.
			if (sda.get_IsPropInCache(hvoOwn, flid, (int)FieldType.kcptOwningCollection, 0))
				chvo = sda.get_VecSize(hvoOwn, flid);
			for (int ihvo = 0; ihvo < chvo; ++ihvo)
			{
				int hvo = sda.get_VecItem(hvoOwn, flid, ihvo);
				list.Add(hvo);
				if (fGetAllDescendants)
					CmPossibility.GetItems(hvo, flidPss, list, sda, fGetAllDescendants);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We don't want to delete items that are protected.
		/// </summary>
		/// <returns>True if Ok to delete.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool ValidateOkToDelete()
		{
			if (CheckAndReportProtectedChartColumn())
				return false;
			if (CheckAndReportUsedMarkupTag())
				return false;
			if (IsProtected == false)
				return true;
			string information = Strings.ksRequiredItem;
			MessageBox.Show(information, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}

		/// <summary>
		/// If the recipient is a TextMarkup tag (or tag type) used in a text, it shouldn't be deleted.
		/// Report accordingly and return true. Return false if OK to delete.
		/// </summary>
		/// <returns>TRUE if there is a problem!</returns>
		public bool CheckAndReportUsedMarkupTag()
		{
			int[] tagTypes = m_cache.LangProject.TextMarkupTagsOA.PossibilitiesOS.HvoArray;
			if (tagTypes.Length == 1 && Hvo == tagTypes[0])
			{
				MessageBox.Show(Strings.ksCantDeleteLastTagList, Strings.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			List<int> hvoArray = new List<int>();
			hvoArray.Add(Hvo);
			if (SubPossibilitiesOS.Count > 0)
			{
				// The presence of SubPossibilities means that this is a tag type
				// (if we're in that list), so we need to check all of the SubPossibilities
				// to see if they are used.
				hvoArray.AddRange(SubPossibilities());
			}
			int iNextGroup = 0;
			string idResult = DbOps.MakePartialIdList(ref iNextGroup, hvoArray.ToArray());
			// This query tests whether the CmPossibility is used in the tagging of a text.
			string sql = "select top 1 (st.id) from StText_ st "
				+ "join StText_Paragraphs stp on st.id = stp.src "
				+ "join StTxtPara_ stp2 on stp2.id = stp.dst "
				+ "join CmBaseAnnotation_ cb on stp2.id = cb.BeginObject "
				+ "join CmIndirectAnnotation_AppliesTo at on at.dst = cb.id "
				+ "join CmAnnotation_ ca on ca.id = at.src "
				+ "where ca.InstanceOf in (" + idResult + ")";

			int hvoText;
			if (!DbOps.ReadOneIntFromCommand(m_cache, sql, null, out hvoText))
				return false; // no text is tagged using this CmPossibility (or its SubPossibilities).
			// Try to get a nice title.
			string textName = null;
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache, "StText", "Title");
			if (vh != null)
				textName = m_cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnalOrVern, hvoText, vh.Tag).Text;
			if (String.IsNullOrEmpty(textName))
				textName = CmObject.CreateFromDBObject(m_cache, m_cache.GetOwnerOfObject(hvoText)).ShortName;
			string msg;
			if (SubPossibilitiesOS.Count == 0)
				msg = string.Format(Strings.ksCantDeleteMarkupTagInUse, textName);
			else
				msg = string.Format(Strings.ksCantDeleteMarkupTypeInUse, textName);
			MessageBox.Show(msg, Strings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		/// <summary>
		/// If the recipient is a column in a chart that shouldn't be moved or deleted, report
		/// accordingly and return true. Return false if OK to delete or move.
		/// </summary>
		/// <returns>TRUE if there is a problem!</returns>
		public bool CheckAndReportProtectedChartColumn()
		{
			int[] discourseTemplates = m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.HvoArray;
			if (discourseTemplates.Length == 1 && Hvo == discourseTemplates[0])
			{
				MessageBox.Show(Strings.ksCantDeleteDefaultDiscourseTemplate, Strings.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			int hvoRootItem = Hvo;
			for (; ; )
			{
				int hvoOwner = m_cache.GetOwnerOfObject(hvoRootItem);
				if (hvoOwner == 0)
					return false; // no owner is a list, can't be a discourse template.
				int clsid = m_cache.GetClassOfObject(hvoOwner);
				if (clsid == CmPossibilityList.kclsidCmPossibilityList)
					break;
				hvoRootItem = hvoOwner;
			}
			// This query by itself tests whether the CmPossibility is part of the template of a non-empty chart.
			string sql = "select top 1 (st.id) from StText st join DsConstChart_ cc on st.id = cc.BasedOn and cc.Template = "
				+ hvoRootItem + " join DsConstChart_Rows cr on cc.id = cr.src ";

			if (SubPossibilitiesOS.Count == 0)
			{
				// Adding this means that to find a match, there must be a CCA actually linked to this possibility.
				// That is, the query will produce nothing and the change will be allowed if no chart has content
				// in this column. We only allow this for leaves (actual columns).
				sql += "join CmIndirectAnnotation r on r.id = cr.dst "
				+ "join CmIndirectAnnotation_AppliesTo at on at.src = r.id "
				+ "join CmAnnotation cca on cca.id = at.dst and cca.InstanceOf = " + Hvo;
			}
			int hvoText;
			if (!DbOps.ReadOneIntFromCommand(m_cache, sql, null, out hvoText))
				return false; // no chart using this template (or it isn't a template at all, may be in another list).
			// Try to get a nice title.
			string textName = null;
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache, "StText", "Title");
			if (vh != null)
				textName = m_cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnalOrVern, hvoText, vh.Tag).Text;
			if (String.IsNullOrEmpty(textName))
				textName = CmObject.CreateFromDBObject(m_cache, m_cache.GetOwnerOfObject(hvoText)).ShortName;
			string msg;
			if (SubPossibilitiesOS.Count == 0)
				msg = string.Format(Strings.ksCantPromoteOrMoveTemplateColumn, textName);
			else
				msg = string.Format(Strings.ksCantPromoteOrMoveGroupInUse, textName);
			MessageBox.Show(msg, Strings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name for the type of CmPossibility. Subclasses may override.
		/// </summary>
		/// <param name="strTable">string table containing mappings for list item names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ItemTypeName(StringTable strTable)
		{
			int ownerHvo = this.OwnerHVO;
			ICmPossibilityList owningList = this.OwningList;
			string owningFieldName =
				Cache.MetaDataCacheAccessor.GetFieldName((uint)owningList.OwningFlid);
			string itemsTypeName = (owningList as CmPossibilityList).ItemsTypeName(strTable);
			if (itemsTypeName != "*" + owningFieldName + "*")
				return itemsTypeName;
			return strTable.GetString(this.GetType().Name, "ClassNames");
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmSemanticDomain
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// For now this needs to include the abbreviation to sort correctly
		/// in the record list.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return AbbrAndName;
			}
		}

		/// <summary>
		/// Overridden to handle Type property.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)CmSemanticDomain.CmSemanticDomainTags.kflidOcmRefs:
					return m_cache.LangProject.AnthroListOA;
				case (int)CmSemanticDomain.CmSemanticDomainTags.kflidRelatedDomains:
					return m_cache.LangProject.SemanticDomainListOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

	}


	/// <summary>
	///
	/// </summary>
	public partial class CmAnthroItem
	{
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ShortName;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// For now this needs to include the abbreviation to sort correctly
		/// in the record list.
		/// </summary>
		public override string ShortName
		{
			get
			{
				return AbbrAndName;
			}
		}

	}

	/// <summary>
	/// Add to the generated class.
	/// </summary>
	public partial class CmAgent
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an evaluation for the given object. This sets the time to the current time.
		/// Note that for no opinion (accepted = 2), the CmAgentEvaluation is removed from
		/// the database. Otherwise an item is created, if not already present, and the
		/// value set to 0 or 1.
		/// </summary>
		/// <param name="hvoTarget">The target id of the object we are evaluating</param>
		/// <param name="accepted">0 = not accepted, 1 = accepted, 2 = don't know</param>
		/// <param name="details">Details for the evaluation</param>
		/// ------------------------------------------------------------------------------------
		public void SetEvaluation(int hvoTarget, int accepted, string details)
		{
			CmAgentEvaluation cae = null;
			foreach (CmAgentEvaluation caeCandidate in EvaluationsOC)
			{
				if (caeCandidate.TargetRAHvo == hvoTarget)
				{
					cae = caeCandidate;
					if (accepted == 2)
					{
						cae.DeleteUnderlyingObject();
						return;
					}
					break;
				}
			}
			if (accepted == 2)
				return; // none found or wanted.
			if (cae == null)
			{
				cae = new CmAgentEvaluation();
				EvaluationsOC.Add(cae);
				cae.TargetRAHvo = hvoTarget;
			}
			cae.Accepted = (accepted == 1);
			cae.DateCreated = DateTime.Now;
			cae.Details = details;

			//IOleDbCommand odc = null;
			//try
			//{
			//    Debug.Assert(m_cache != null); // The cache must be set.
			//    m_cache.DatabaseAccessor.CreateCommand(out odc);
			//    uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
			//    odc.SetParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
			//        null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)Hvo }, uintSize);
			//    odc.SetParameter(2, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT, null,
			//        (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)hvoTarget }, uintSize);
			//    odc.SetParameter(3, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT, null,
			//        (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { (uint)accepted }, uintSize);
			//    odc.SetStringParameter(4, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
			//        null, details, (uint)details.Length);
			//    string sSql =
			//        "declare @date datetime set @date = getdate() " +
			//        "exec SetAgentEval ?, ?, ?, ?, @date";
			//    odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtStoredProcedure);
			//}
			//finally
			//{
			//    DbOps.ShutdownODC(ref odc);
			//}
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmAnnotation
	{
		/// <summary>
		/// Override to handle Source.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)CmAnnotation.CmAnnotationTags.kflidSource:
					return m_cache.LangProject;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Note that in this case we override this as WELL as ReferenceTargetOwner, in order
		/// to filter the list to human agents.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		/// ------------------------------------------------------------------------------------
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)CmAnnotation.CmAnnotationTags.kflidSource:
					set = new Set<int>();
					foreach (ICmAgent agent in m_cache.LangProject.AnalyzingAgentsOC)
					{
						if (agent.Human)
							set.Add(agent.Hvo);
					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		internal static int CreateDummyCmAnnotation(FdoCache cache, int clsid, int hvoAnnType, int hvoInstanceOf)
		{
			int hvoAnn;
			cache.CreateDummyID(out hvoAnn);
			SetDummyAnnotationInfo(cache, clsid, hvoAnnType, hvoInstanceOf, hvoAnn);
			return hvoAnn;
		}

		internal static void SetDummyAnnotationInfo(FdoCache cache, int clsid, int hvoAnnType, int hvoInstanceOf, int hvoAnn)
		{
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			cda.CacheIntProp(hvoAnn, (int)CmObjectFields.kflidCmObject_Class, clsid);
			cda.CacheObjProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType, hvoAnnType);
			cda.CacheObjProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoInstanceOf);
		}

		/// <summary>
		/// Clone basic annotation information, so that we can preserve it in the cache.
		/// </summary>
		/// <returns>hvo of dummy CmAnnotation</returns>
		public int CloneIntoDummy()
		{
			int hvoCaDummy = CreateDummyCmAnnotation(Cache, ClassID, AnnotationTypeRAHvo, InstanceOfRAHvo);
			// cache the comment alternatives.
			foreach (int ws in Cache.LangProject.CurrentAnalysisAndVernWss)
			{
				ITsString tssCommentAlt = this.Comment.GetAlternativeTss(ws);
				Cache.VwCacheDaAccessor.CacheStringAlt(hvoCaDummy, (int)CmAnnotationTags.kflidComment, ws, tssCommentAlt);
			}
			return hvoCaDummy;
		}

		/* note, as far as I can tell, we have a odd model where the basic class,
		 * CmAnnotation, cannot actually annotate anything. It has no target property.
			/// <summary>
				/// Get all annotations that refer to the given object.
				/// </summary>
		//		/// <returns>nothing; throws an exception.</returns>
		//		public static FdoObjectSet AnnotationsForObject(FdoCache cache, int hvo)
		//		{
		//			throw new ApplicationException ("The base class, CmAnnotation, cannot actually point at anything. Call the static on a subclass.");
		//		}


				/// <summary>
				/// The shortest, non abbreviated, label for the content of this object.
				/// </summary>
		//		public override string ShortName
		//		{
		//			get
		//			{
		//				if(this.InstanceOfRA != null)
		//					return InstanceOfRA.ShortName;
		//				else
		//					return "*Orphan Annotation";
		//			}
		//		}
		 */
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmBaseAnnotation
	{
		/// <summary>
		/// This is the hvo of the object where we store annotations for later reuse.
		/// </summary>
		static public int kHvoBeginObjectOrphanage = 0;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The shortest, non abbreviated, label for the content of this object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get
			{
				if(this.BeginObjectRA != null)
					return BeginObjectRA.ShortName;
				else
					return Strings.ksOrphanAnnotation;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all annotations that refer to the given object in the InstanceOf property,
		/// and which do not have null for the BeginObject property.
		/// </summary>
		/// <param name="cache">The cache to use.</param>
		/// <param name="hvo">The object that may have annotations.</param>
		/// <returns>A List that contains zero, or more, integers for the values in the InstanceOf property.</returns>
		/// ------------------------------------------------------------------------------------
		public static List<int> AnnotationsForInstanceOf(FdoCache cache, int hvo)
		{
			Debug.Assert(!cache.IsDummyObject(hvo), "Currently only handles real objects.");
			string qry = " select cba.Id" +
				" from CmBaseAnnotation_ cba" +
				" where cba.InstanceOf=? AND cba.BeginObject is not null";
			return DbOps.ReadIntsFromCommand(cache, qry, hvo);
		}

		/// <summary>
		/// This should be the preferred way to create CmBaseAnnotations that ought not to be owned:
		/// currently Wfics, Pfics, and paragraph segments.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static ICmBaseAnnotation CreateUnownedCba(FdoCache cache)
		{
			int hvoNew = cache.CreateObject(kclsidCmBaseAnnotation);
			return
				CmObject.CreateFromDBObject(cache, hvoNew, typeof (CmBaseAnnotation), false, false) as
				ICmBaseAnnotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all annotations that refer to the given object.
		/// </summary>
		/// <param name="cache">The cache to use.</param>
		/// <param name="hvo">The object that may have annotations.</param>
		/// <returns>A set of annotations that refer to the given object.
		/// There may not be any.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoObjectSet<ICmBaseAnnotation> AnnotationsForObject(FdoCache cache, int hvo)
		{
			string qry = string.Format("select Id, Class$ from CmBaseAnnotation_ "
				+ "where BeginObject={0}", hvo);
			return new FdoObjectSet<ICmBaseAnnotation>(cache, qry, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all annotations from a given agent which refer to the given object.
		/// </summary>
		/// <param name="cache">The cache to use.</param>
		/// <param name="hvoTarget">The object that may have annotations.</param>
		/// <param name="agent">The agent listed as the source of the annotation.</param>
		/// <returns>A set of annotations that refer to the given object.
		/// There may not be any.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoObjectSet<ICmBaseAnnotation> AnnotationsForObject(FdoCache cache, int hvoTarget, ICmAgent agent)
		{
			//enhance: this would be faster if we were allowed to give parameterized queries
			//	so that the server could cache execution plans.
			string qry = string.Format("select Id, Class$ from CmBaseAnnotation_ "
				+ "where BeginObject={0} and Source={1}",
				 hvoTarget, agent.Hvo) ;
			return new FdoObjectSet<ICmBaseAnnotation>(cache, qry, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all annotations pointing at the given object and listing the constraint checker asked their source.
		/// </summary>
		/// <param name="cache">The cache to use.</param>
		/// <param name="hvoTarget">The object that may have annotations.</param>
		/// ------------------------------------------------------------------------------------
		public static void RemoveErrorAnnotationsForObject(FdoCache cache, int hvoTarget)
		{
			//review: currently, this is based on the source agent. Once a notation definitions
			//	become available (currently they are lacking a home in the model),
			//	we may want to switch this to selecting annotations of a certain type or possibly, severity.
			foreach(CmAnnotation a in CmBaseAnnotation.ErrorAnnotationsForObject(cache, hvoTarget))
			{
				a.DeleteUnderlyingObject();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get all of the annotations attributed to the constraint checker
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static FdoObjectSet<ICmBaseAnnotation> ErrorAnnotationsForObject(FdoCache cache, int hvoTarget)
		{
			//review: currently, this is based on the source agent. Once a notation definitions
			//	become available (currently they are lacking a home in the model),
			//	we probably want to switch this to selecting annotations of a certain type or possibly, severity.
			return AnnotationsForObject(cache, hvoTarget, cache.LangProject.ConstraintCheckerAgent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ID of the virtual property used to retrieve a string that is the text of the
		/// substring the annotation refers to.
		/// (This handles real properties of type string...we need a slightly different one for
		/// multistrings.)
		/// </summary>
		/// <param name="fcCcache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int StringValuePropId(FdoCache fcCcache)
		{
			IVwCacheDa cda = fcCcache.MainCacheAccessor as IVwCacheDa;
			IVwVirtualHandler vh = cda.GetVirtualHandlerName("CmBaseAnnotation", "StringValue");
			if (vh == null)
			{
				vh = new AnnotationStringValueVh();
				cda.InstallVirtual(vh);
			}
			return vh.Tag;
		}

		/// <summary>
		/// This virtual property stores all the annotations in a StTxtPara.Segment (CmAnnotationDefn.Twfic and CmAnnotationDefn.Punctuation).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		internal static int SegmentFormsFlid(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "SegmentForms", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
		}

		/// <summary>
		/// Freeform annotations for the segment annotation.
		/// </summary>
		public List<int> SegmentFreeformAnnotations
		{
			get
			{
				// simply load nothing. this should get overwritten.
				return new List<int>();
			}
		}

		/// <summary>
		/// same as SetCbaFields, except also sets the hvoInstanceOf.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnnotation"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="hvoPara"></param>
		/// <param name="hvoInstanceOf"></param>
		/// <param name="fNewAnnotation"></param>
		public static void SetCbaFields(FdoCache cache, int hvoAnnotation, int ichMin, int ichLim, int hvoPara, int hvoInstanceOf, bool fNewAnnotation)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			if (fNewAnnotation || sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf) != hvoInstanceOf)
			{
				if (cache.IsDummyObject(hvoAnnotation))
					cda.CacheObjProp(hvoAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoInstanceOf);
				else
					sda.SetObjProp(hvoAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoInstanceOf);
			}
			CmBaseAnnotation.SetCbaFields(cache, hvoAnnotation, ichMin, ichLim, hvoPara, fNewAnnotation);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnnotation"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="hvoPara"></param>
		/// <param name="fNewAnnotation"></param>
		public static void SetCbaFields(FdoCache cache, int hvoAnnotation, int ichMin, int ichLim, int hvoPara, bool fNewAnnotation)
		{
			if (ichMin < 0 || ichLim < 0)
				throw new ArgumentException("new offsets (" + ichMin + ", " + ichLim + ") will extend cba offsets beyond paragraph limits.");
			bool fIsDummyAnnotation = cache.IsDummyObject(hvoAnnotation);
			// Adjust the character offsets whether an old or new annotation, but to reduce database
			// traffic, only if they've changed.
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			if (fNewAnnotation || sda.get_IntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset) != ichMin)
			{
				if (!fIsDummyAnnotation)
					sda.SetInt(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, ichMin);
				else
					cda.CacheIntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, ichMin);
			}
			if (fNewAnnotation || sda.get_IntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset) != ichLim)
			{
				if (!fIsDummyAnnotation)
					sda.SetInt(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, ichLim);
				else
					cda.CacheIntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, ichLim);
			}
			if (fNewAnnotation || sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject) != hvoPara)
			{
				// They're always the same, so we can just set both.
				if (!fIsDummyAnnotation)
				{
					sda.SetObjProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, hvoPara);
					sda.SetObjProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndObject, hvoPara);
				}
				else
				{
					cda.CacheObjProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, hvoPara);
					cda.CacheObjProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndObject, hvoPara);
				}
			}
		}

		/// <summary>
		/// try to find the field resulting in the first value difference in the order given by the input params. (excluding original zero values)
		/// NOTE: change in begin/end offsets are not considered significant, since shifting the same InstanceOf within the text
		/// is probably safe to reuse its info.
		/// </summary>
		/// <returns>true, if flidObjDiff is a reference of field with difference</returns>
		public static bool TryFindFirstSignificantDiffInCbaObjLinkInfo(FdoCache cache, int hvoAnnotation,
			int hvoInstanceOf, int hvoAnnType, int hvoPara, out int flidObjDiff)
		{
			flidObjDiff = 0;
			ISilDataAccess sda = cache.MainCacheAccessor;
			int origInstanceOf = sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf);
			if (origInstanceOf != 0 && origInstanceOf != hvoInstanceOf)
			{
				flidObjDiff = (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf;
				return true;
			}
			int origAnnTypeOrig = sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidAnnotationType);
			if (origAnnTypeOrig != 0 && origAnnTypeOrig != hvoAnnType)
			{
				flidObjDiff = (int)CmBaseAnnotation.CmAnnotationTags.kflidAnnotationType;
				return true;
			}
			int origPara = sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			if (origPara != 0 && origPara != hvoPara)
			{
				flidObjDiff = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
				return true;
			}
			return false;
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnnType"></param>
		/// <param name="hvoInstanceOf"></param>
		/// <param name="hvoBeginObject"></param>
		/// <param name="flid"></param>
		/// <param name="beginOffset"></param>
		/// <param name="endOffset"></param>
		/// <returns></returns>
		public static ICmBaseAnnotation CreateRealAnnotation(FdoCache cache, int hvoAnnType, int hvoInstanceOf, int hvoBeginObject, int flid, int beginOffset, int endOffset)
		{
			// This stored procedure is optimized to create an annotation with exactly the information we need here.
			//			string sql = "exec CreateCmBaseAnnotation " + cache.LangProject.Hvo + "," + hvoAnnType + "," + hvoInstanceOf + "," + hvoBeginObject
			//				+ "," + flid +  "," + beginOffset + "," + endOffset;
			//	@Owner int = null,
			//	@annotationType int = null,
			//	@InstanceOf int = null,
			//	@BeginObject int = null,
			//	@CmBaseAnnotation_Flid int = 0,
			//	@CmBaseAnnotation_BeginOffset int = 0,
			//	@CmBaseAnnotation_EndOffset int = 0
			//			DbOps.ExecuteStoredProc(cache, sql, null);
			// can we assume that the object will be inserted at the end?

			// Since we only create real annotations upon a 'confirm', using CmObject shouldn't be too expensive.
			// The advantage of doing it this way is that we readily have the new hvo, and it does the PropChanged for us.
			// (I think CreateCmBaseAnnotation would need to be enhanced to return the new hvo).
			ICmBaseAnnotation cba = CreateUnownedCba(cache);
			cba.Flid = flid;
			cba.AnnotationTypeRAHvo = hvoAnnType;
			if (cache.IsDummyObject(hvoInstanceOf))
			{
				// convert this to a real object.
				ICmObject realInstanceOf = CmObject.ConvertDummyToReal(cache, hvoInstanceOf);
				hvoInstanceOf = realInstanceOf.Hvo;
			}
			Debug.Assert(hvoInstanceOf >= 0, "dummy objects cannot be referenced by a real annotation");
			cba.InstanceOfRAHvo = hvoInstanceOf;
			SetCbaFields(cache, cba.Hvo, beginOffset, endOffset, hvoBeginObject, true);
			return cba;
		}


		/// <summary>
		/// Create's a new dummy (ownerless) CmBaseAnnotation in the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="beginAndEndObject"></param>
		/// <param name="hvoAnnType"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="hvoInstanceOf"></param>
		/// <returns>id of the dummy annotation in the cache.</returns>
		static public int CreateDummyAnnotation(FdoCache cache, int beginAndEndObject, int hvoAnnType, int ichMin, int ichLim, int hvoInstanceOf)
		{
			int hvoAnn = CmAnnotation.CreateDummyCmAnnotation(cache, CmBaseAnnotation.kclsidCmBaseAnnotation, hvoAnnType, hvoInstanceOf);
			SetOwnDummyAnnotationInfo(cache, hvoAnn, beginAndEndObject, ichMin, ichLim);
			return hvoAnn;
		}

		/// <summary>
		/// Set all the important properties (at least for Wfics) for a dummy annotation.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnn"></param>
		/// <param name="hvoAnnType"></param>
		/// <param name="hvoInstanceOf"></param>
		/// <param name="beginAndEndObject"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		public static void SetDummyAnnotationInfo(FdoCache cache, int hvoAnn, int beginAndEndObject, int hvoAnnType, int ichMin, int ichLim, int hvoInstanceOf)
		{
			CmAnnotation.SetDummyAnnotationInfo(cache, CmBaseAnnotation.kclsidCmBaseAnnotation, hvoAnnType, hvoInstanceOf, hvoAnn);
			SetOwnDummyAnnotationInfo(cache, hvoAnn, beginAndEndObject, ichMin, ichLim);
		}

		private static void SetOwnDummyAnnotationInfo(FdoCache cache, int hvoAnn, int beginAndEndObject, int ichMin, int ichLim)
		{
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			cda.CacheObjProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, beginAndEndObject);
			cda.CacheObjProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndObject, beginAndEndObject);
			cda.CacheIntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, ichMin);
			cda.CacheIntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, ichLim);
			cda.CacheIntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidFlid, (int)StTxtPara.StTxtParaTags.kflidContents);
			// not sure we really need this WritingSystemRA property or how to set it to something meaningful.
			// it is only used in RawTextPane.SelectAnnotation().  also used by wordform bulk edit.
			int ws = GetAnnotationWritingSystem(cache, beginAndEndObject, ichMin, (int)StTxtPara.StTxtParaTags.kflidContents);
			cda.CacheObjProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem, ws);
		}

		/// <summary>
		/// Calculate the WritingSystemHvo for a CmBaseAnnotation.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoObject"></param>
		/// <param name="beginOffset"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		public static int GetAnnotationWritingSystem(FdoCache cache, int hvoObject, int beginOffset,
			int flid)
		{
			if (flid == (int)StTxtPara.StTxtParaTags.kflidContents)
			{
				TsStringAccessor tsa = new TsStringAccessor(cache, hvoObject, flid);
				ITsString tss = tsa.UnderlyingTsString;
				if (beginOffset >= 0 && beginOffset < tss.Length)
				{
					ITsTextProps ttp = tss.get_PropertiesAt(beginOffset);
					int nVar;
					int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					if (ws > 0)
						return ws;
				}
			}
			return cache.DefaultVernWs;
		}

		private StText OwningStText()
		{
			if (BeginObjectRAHvo != 0 && Cache.GetClassOfObject(BeginObjectRAHvo) == StTxtPara.kClassId)
			{
				StTxtPara para = BeginObjectRA as StTxtPara;
				return para.Owner as StText;
			}
			else
			{
				return null;
			}

		}

		/// <summary>
		/// Used to get Text.Title
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextTitleForWs(int ws)
		{
			StText text = OwningStText();
			if (text != null)
			{
				return text.Title.GetAlternativeTss(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Get Text.IsTranslation
		/// </summary>
		public bool TextIsTranslation
		{
			get
			{
				StText text = OwningStText();
				if (text != null)
				{
					return text.IsTranslation;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Used to get Text.Source
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextSourceForWs(int ws)
		{
			StText text = OwningStText();
			if (text != null)
			{
				return text.SourceOfTextForWs(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Answer the string that is actually annotated: the range from BeginOffset to EndOffset in
		/// property Flid (and possibly alternative WsSelector) of BeginObject.
		/// If Flid has not been set and BeginObject is an StTxtPara assumes we want the Contents.
		/// Does not attempt to handle a case where EndObject is different.
		/// </summary>
		/// <returns></returns>
		public ITsString TextAnnotated
		{
			get
			{
				ITsString tssObj;
				int flid = Flid;
				if (flid == 0 && BeginObjectRA is StTxtPara)
					flid = (int) StTxtPara.StTxtParaTags.kflidContents;
				if (WsSelector == 0)
					tssObj = Cache.MainCacheAccessor.get_StringProp(BeginObjectRAHvo, flid);
				else
					tssObj = Cache.GetMultiStringAlt(BeginObjectRAHvo, flid, WsSelector);
				ITsStrBldr bldr = tssObj.GetBldr();
				if (EndOffset < bldr.Length)
					bldr.ReplaceTsString(EndOffset, bldr.Length, null);
				if (BeginOffset > 0)
					bldr.ReplaceTsString(0, BeginOffset, null);
				return bldr.GetString();
			}
		}

		/// <summary>
		/// Used to get Text.Genres
		/// </summary>
		public List<int> TextGenres
		{
			get
			{
				StText text = OwningStText();
				if (text != null)
				{
					return text.GenreCategories;
				}
				else
				{
					return new List<int>();
				}
			}
		}

		/// <summary>
		/// Used to get Text.Description
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString TextCommentForWs(int ws)
		{
			StText text = OwningStText();
			if (text != null)
			{
				return text.CommentForWs(ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Overriden so that we can clean up virtual references.
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			// if we are a twfic, remove us from our paragraph's SegForms.
			int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
			if (this.AnnotationTypeRAHvo == twficType)
				StTxtPara.RemoveTwficAnnotation(Cache, this.Hvo);
			// delete indirect annotations.
			CollectLinkedItemsForDeletion(ref objectsToDeleteAlso, false);
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		/// <summary>
		/// Collections of items that should be deleted as a side-effect of deleting cba.
		/// Note: this does not add self to objectsToDeleteAlso unless fIncludeSelf is true.
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="fIncludeSelf">if true, add this cba to objctesToDeleteAlso</param>
		public void CollectLinkedItemsForDeletion(ref Set<int> objectsToDeleteAlso, bool fIncludeSelf)
		{
			List<int> items = new List<int>(1);
			items.Add(Hvo);
			CollectLinkedItemsForDeletionFor(m_cache, items, objectsToDeleteAlso, fIncludeSelf);
		}

		// JohnT: this version was never checked it, but did work. It might be useful if we want an
		// FDO-based version.
		///// <summary>
		///// Collect the items that should be deleted along with the ones in sourceObjects.
		///// </summary>
		///// <param name="cache"></param>
		///// <param name="sourceObjects"></param>
		///// <param name="objectsToDeleteAlso"></param>
		///// <param name="fIncludeSource">true to also add SourceObjects.</param>
		//public static void CollectLinkedItemsForDeletionFor(FdoCache cache, List<int> sourceObjects,
		//    Set<int> objectsToDeleteAlso, bool fIncludeSource)
		//{
		//    List<LinkedObjectInfo> linkedObjects = cache.GetLinkedObjects(sourceObjects,
		//        LinkedObjectType.OwningAndReference, true, true, true,
		//        ReferenceDirection.Inbound, 0, false);

		//    foreach (LinkedObjectInfo loi in linkedObjects)
		//    {
		//        if (loi.RelObjClass == CmIndirectAnnotation.kClassId)
		//        {
		//            CmIndirectAnnotation cia = new CmIndirectAnnotation(cache, loi.RelObjId);
		//            // we want to delete this indirect annotation if it is an instanceOf a target object
		//            if (sourceObjects.Contains(cia.InstanceOfRAHvo))
		//            {
		//                objectsToDeleteAlso.Add(cia.Hvo);
		//                continue;
		//            }

		//            // otherwise we want to delete this indirect annotation if it only applies to things that
		//            // are going to be deleted.
		//            if (RefersOnlyToObjectsIn(cia, sourceObjects))
		//            {
		//                objectsToDeleteAlso.Add(cia.Hvo);
		//            }
		//        }
		//    }
		//    if (fIncludeSource)
		//        foreach (int hvo in sourceObjects)
		//            objectsToDeleteAlso.Add(hvo);
		//}
		//private static bool RefersOnlyToObjectsIn(CmIndirectAnnotation cia, List<int> sourceObjects)
		//{
		//    foreach (ICmAnnotation ca in cia.AppliesToRS)
		//    {
		//        // if cia applies to something else that is not in one of the sets, fail.
		//        if (!sourceObjects.Contains(ca.Hvo))
		//            return false;
		//    }
		//    return true; // everything it appliesTo is in one of the arguments.
		//}


		/// <summary>
		/// Collect the items that should be deleted along with the ones in sourceObjects.
		/// These are the ones whose InstanceOf is in sourceObjects (first query), or all of whose AppliesTo
		/// are in sourceObjects(second query in union).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sourceObjects"></param>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="fIncludeSource">true to also add SourceObjects.</param>
		public static void CollectLinkedItemsForDeletionFor(FdoCache cache, List<int> sourceObjects,
			Set<int> objectsToDeleteAlso, bool fIncludeSource)
		{
			string sql =
				@"declare @ids NVARCHAR(MAX)
				set @ids = ?

				select distinct ia.id
				from CmIndirectAnnotation_ ia
				join fnGetIdsFromString(@ids) AS ids0 on ids0.Id = ia.InstanceOf
				union
				select distinct iaa.Src
				from CmIndirectAnnotation_AppliesTo iaa
				join fnGetIdsFromString(@ids) AS ids on ids.Id = iaa.Dst
				left outer join (
					select Src
					from CmIndirectAnnotation_AppliesTo iaa2
					left outer join fnGetIdsFromString(@ids) as ids2 on ids2.id = iaa2.Dst
					where ids2.id is null) AS x on x.Src = iaa.Src
				where x.Src is null
				";
			if (sourceObjects.Count == 0)
				return;
			StringBuilder bldr = new StringBuilder(sourceObjects.Count * 6);
			foreach (int hvo in sourceObjects)
			{
				bldr.Append(hvo);
				bldr.Append(',');
			}
			bldr.Remove(bldr.Length - 1, 1); // remove final comma
			List<int> itemsToRemoveAlso = DbOps.ReadIntsFromCommand(cache, sql, bldr.ToString());
			objectsToDeleteAlso.AddRange(itemsToRemoveAlso);
			if (fIncludeSource)
				foreach (int hvo in sourceObjects)
					objectsToDeleteAlso.Add(hvo);
		}


		/// <summary>
		/// Reserve the annotation for later reuse. This saves time spending creating/deleting annotations.
		/// (these annotations will be stored in BeginObject kHvoBeginObjectOrphanage.)
		/// Refactor: should this be with AnnotationsOC?
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="cbaIdsToReserve"></param>
		/// <param name="fDeleteUnreferencedInstanceOf">if true, it will try to delete instanceOf, if no longer used.</param>
		static public void ReserveAnnotations(FdoCache cache, Set<int> cbaIdsToReserve, bool fDeleteUnreferencedInstanceOf)
		{
			bool fChangedWordformsOC = false;
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			// Move annotations to kHvoBeginObjectOrphanage.
			Set<int> delObjIds = new Set<int>(cbaIdsToReserve.Count);
			foreach (int hvoCba in cbaIdsToReserve)
			{
				// clear out references to InstanceOf
				int hvoInstanceOf = sda.get_ObjectProp(hvoCba, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				cda.ClearInfoAbout(hvoCba, VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);	// clear virtual props.
				sda.SetObjProp(hvoCba, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, 0);
				// see if we can delete the InstanceOf

				if (fDeleteUnreferencedInstanceOf)
				{
					ICmObject objInstanceOf = CmObject.CreateFromDBObject(cache, hvoInstanceOf);
					if (objInstanceOf.CanDelete)
						delObjIds.Add(hvoInstanceOf);
					if (objInstanceOf.ClassID == WfiWordform.kClassId)
						fChangedWordformsOC = true;
				}
				sda.SetObjProp(hvoCba,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, kHvoBeginObjectOrphanage);
			}
			if (delObjIds.Count > 0)
			{
				CmObject.DeleteObjects(delObjIds, cache);
				if (fChangedWordformsOC)
					WordformInventory.OnChangedWordformsOC();
			}
		}

		/// <summary>
		/// CmBaseAnnotations need to check BeginObject and other fields depending upon their type.
		/// </summary>
		/// <returns></returns>
		public override bool IsValidObject()
		{
			bool fIsValidObj = base.IsValidObject() && Cache.IsValidObject(this.BeginObjectRAHvo);
			if (!fIsValidObj)
				return false;
			// if it's real and a twfic, we should also expect it to be an InstanceOf a real object.
			if (AnnotationTypeRAHvo == CmAnnotationDefn.Twfic(Cache).Hvo)
				fIsValidObj = Cache.IsValidObject(this.InstanceOfRAHvo);
			return fIsValidObj;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="dummyAnnHvo"></param>
		/// <returns></returns>
		public static ICmBaseAnnotation ConvertBaseAnnotationToReal(FdoCache fdoCache, int dummyAnnHvo)
		{
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(fdoCache, true))
			{
				Debug.Assert(fdoCache.IsDummyObject(dummyAnnHvo));
				if (!fdoCache.IsDummyObject(dummyAnnHvo) ||
					fdoCache.GetClassOfObject(dummyAnnHvo) != CmBaseAnnotation.kclsidCmBaseAnnotation)
				{
					return null; // indicate no change.
				}
				ISilDataAccess sda = fdoCache.MainCacheAccessor;
				ICmBaseAnnotation cbaDummy = (ICmBaseAnnotation)CmObject.CreateFromDBObject(fdoCache, dummyAnnHvo, false);
				ICmBaseAnnotation cbaReal = CreateRealAnnotation(fdoCache, cbaDummy.AnnotationTypeRAHvo, cbaDummy.InstanceOfRAHvo,
					cbaDummy.BeginObjectRAHvo, cbaDummy.Flid, cbaDummy.BeginOffset, cbaDummy.EndOffset);
				cbaReal.WritingSystemRAHvo = cbaDummy.WritingSystemRAHvo;
				int hvoRealAnn = cbaReal.Hvo;

				// transfer any default analysis (guess)
				int ktagTwficDefault = StTxtPara.TwficDefaultFlid(fdoCache);
				int hvoTwficAnalysisGuess = fdoCache.GetObjProperty(dummyAnnHvo, ktagTwficDefault);
				if (hvoTwficAnalysisGuess != 0)
				{
					fdoCache.VwCacheDaAccessor.CacheObjProp(hvoRealAnn, ktagTwficDefault, hvoTwficAnalysisGuess);
				}
				int textSegType = CmAnnotationDefn.TextSegment(fdoCache).Hvo;
				int twficType = CmAnnotationDefn.Twfic(fdoCache).Hvo;
				if (cbaDummy.AnnotationTypeRAHvo == twficType)
					StTxtPara.CacheReplaceTWFICAnnotation(fdoCache, dummyAnnHvo, hvoRealAnn);
				else if (cbaDummy.AnnotationTypeRAHvo == textSegType)
					StTxtPara.CacheReplaceTextSegmentAnnotation(fdoCache, dummyAnnHvo, hvoRealAnn);
				else
					Debug.Assert(true, "CacheReplace does not yet support annotation type " + cbaDummy.AnnotationTypeRAHvo);
				// now clear it from the cache, since we're done with it.
				if (fdoCache.ActionHandlerAccessor != null)
					fdoCache.ActionHandlerAccessor.AddAction(new ClearInfoOnCommitUndoAction(fdoCache, dummyAnnHvo));
				return cbaReal;
			}
		}
	}

	public partial class CmIndirectAnnotation
	{
		/// <summary>
		/// This should be the preferred way to create CmBaseAnnotations that ought not to be owned:
		/// currently Wfics, Pfics, paragraph segments, free translations, literal translations, and notes.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static ICmIndirectAnnotation CreateUnownedIndirectAnnotation(FdoCache cache)
		{
			int hvoNew = cache.CreateObject(kclsidCmIndirectAnnotation);
			return
				CmObject.CreateFromDBObject(cache, hvoNew, typeof(CmIndirectAnnotation), false, false) as
				ICmIndirectAnnotation;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmAnnotationDefn
	{
		/// <summary>
		/// This constructor creates one based on a guid rather than an hvo. Mainly used by static methods
		/// that return a particular one.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="guid"></param>
		public CmAnnotationDefn(FdoCache fcCache, Guid guid) : base()
		{
			InitExisting(fcCache, guid);
		}

		/// <summary>
		/// Static method to retrieve the one for text segments.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn TextSegment(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidAnnTextSegment));
		}

		/// <summary>
		/// Static method to retrieve the one for punctuation within interlinear text.
		/// Todo: use the correct magic guid.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn Punctuation(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidAnnPunctuationInContext));
		}

		/// <summary>
		/// Static method to retrieve the one for Twfics (Text Wordforms in Context) within interlinear text.
		/// </summary>
		/// <param name="fcCache">The fc cache.</param>
		/// <returns></returns>
		public static ICmAnnotationDefn Twfic(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidAnnWordformInContext));
		}

		/// <summary>
		/// Static method to retrieve the one for Constituent Chart Annotations (CCA).
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn ConstituentChartAnnotation(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidConstituentChartAnnotation));
		}

		/// <summary>
		/// Static method to retrieve the one for Text Tags.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn TextMarkupTag(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidAnnTextTag));
		}

		/// <summary>
		/// Static method to retrieve the one for Constituent Chart Rows (CCR).
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn ConstituentChartRow(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidConstituentChartRow));
		}

		/// <summary>
		/// Static method to retrieve the one for checking errors.
		/// </summary>
		/// <param name="fcCache">The fc cache.</param>
		/// <returns></returns>
		public static ICmAnnotationDefn Errors(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				LangProject.kguidAnnCheckingError);
		}

		/// <summary>
		/// Static method to retrieve the one for the time an object was processed in some way.
		/// </summary>
		/// <param name="fcCache"></param>
		/// <returns></returns>
		public static ICmAnnotationDefn ProcessTime(FdoCache fcCache)
		{
			return (ICmAnnotationDefn)new CmAnnotationDefn(fcCache,
				new Guid(LangProject.kguidAnnProcessTime));
		}

	}

	/// <summary>
	/// Summary description for FsClosedFeature
	/// </summary>
	public partial class FsClosedFeature
	{
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}

	}

	/// <summary>
	/// Summary description for FsComplexFeature
	/// </summary>
	public partial class FsComplexFeature
	{
		/// <summary>
		/// Overridden to handle Type.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsComplexFeature.FsComplexFeatureTags.kflidType:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsComplexFeature.FsComplexFeatureTags.kflidType:
					set = new Set<int>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.TypesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.AppendTsString(Name.BestAnalysisAlternative);
				if (TypeRA != null)
				{
					FdoReferenceSequence<IFsFeatDefn> rs = TypeRA.FeaturesRS;
					if (rs.Count > 0)
					{
						tisb.Append("(");
						for (int i=0; i < rs.Count; i++)
						{
							IFsFeatDefn defn = rs[i];
							if (defn != null)
							{
								if (i > 0)
									tisb.Append(Strings.ksListSep);

								tisb.AppendTsString(defn.Name.BestAnalysisAlternative);
							}
						}
						tisb.Append(")");
					}
				}
				return tisb.GetString();
			}
		}
	}

	/// <summary>
	/// Summary description for FsFeatStrucType
	/// </summary>
	public partial class FsFeatStrucType
	{
		/// <summary>
		/// Overridden to handle Features.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsFeatStrucType.FsFeatStrucTypeTags.kflidFeatures:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsFeatStrucType.FsFeatStrucTypeTags.kflidFeatures:
					set = new Set<int>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}
	}

	/// <summary>
	/// Summary description for FsFeatureSystem.
	/// </summary>
	public partial class FsFeatureSystem
	{
		/// <summary>
		/// Determine if a FsSymFeatVal exists based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>true if the value exists, false otherwise</returns>
		static public bool HasSymbolicValue(FdoCache cache, string sMasterInflectionFeatureId)
		{
			int hvo = GetSymbolicValue(cache, sMasterInflectionFeatureId);
			if (hvo > 0)
				return true;
			return false;
		}
		/// <summary>
		/// Obtain a FsSymFeatVal based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>hvo of the FsSymFeatVal</returns>
		static public int GetSymbolicValue(FdoCache cache, string sMasterInflectionFeatureId)
		{
			int hvo;
			string qry = String.Format("select sym.Id "
				+ "from FsSymFeatVal sym "
				+ "WHERE sym.CatalogSourceId = '{0}'",
				sMasterInflectionFeatureId);
			DbOps.ReadOneIntFromCommand(cache, qry, null, out hvo);
			return hvo;
		}
		/// <summary>
		/// Obtain a FsFeatStrucType based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>hvo of the FsFeatStrucType</returns>
		static public int GetFeatureType(FdoCache cache, string sMasterInflectionFeatureId)
		{
			string qry = String.Format("select type.Id "
				+ "from FsFeatStrucType type "
				+ "WHERE type.CatalogSourceId = '{0}'",
				sMasterInflectionFeatureId);
			int hvo;
			DbOps.ReadOneIntFromCommand(cache, qry, null, out hvo);
			return hvo;
		}
		/// <summary>
		/// Obtain a FsComplexFeature based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>hvo of the FsComplexFeature</returns>
		static public int GetComplexFeature(FdoCache cache, string sMasterInflectionFeatureId)
		{
			string qry = String.Format("SELECT complex.Id FROM FsComplexFeature complex "
				+ "JOIN FsFeatDefn defn ON defn.CatalogSourceId = '{0}' "
				+ "WHERE defn.Id = complex.Id",
				sMasterInflectionFeatureId);
			int hvo;
			DbOps.ReadOneIntFromCommand(cache, qry, null, out hvo);
			return hvo;
		}
		/// <summary>
		/// Obtain a FsClosedFeature based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>hvo of the FsClosedFeature</returns>
		static public int GetClosedFeature(FdoCache cache, string sMasterInflectionFeatureId)
		{
			string qry = String.Format("SELECT closed.Id FROM FsClosedFeature closed "
				+ "JOIN FsFeatDefn defn ON defn.CatalogSourceId = '{0}' "
				+ "WHERE defn.Id = closed.Id",
				sMasterInflectionFeatureId);
			int hvo;
			DbOps.ReadOneIntFromCommand(cache, qry, null, out hvo);
			return hvo;
		}
		/// <summary>
		/// Determine if a FsClosedFeature exists based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>true if the value exists, false otherwise</returns>
		static public bool HasClosedFeature(FdoCache cache, string sMasterInflectionFeatureId)
		{
			int hvo = GetClosedFeature(cache, sMasterInflectionFeatureId);
			if (hvo > 0)
				return true;
			return false;
		}
		/// <summary>
		/// Determine if a FsFeatStrucType exists based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureTypeId">the id of the feature structure type to look for</param>
		/// <param name="sMasterInflectionFeatureId">the id of the feature defn to look for</param>
		/// <returns>true if the value exists, false otherwise</returns>
		static public bool FsFeatStrucTypeHasFeature(FdoCache cache, string sMasterInflectionFeatureTypeId, string sMasterInflectionFeatureId)
		{
			List<int> list = null;
			string qry = String.Format("SELECT feats.Src FROM FsFeatStrucType_Features feats " +
									   "JOIN FsFeatStrucType type ON type.CatalogSourceId = '{0}' " +
									   "JOIN FsFeatDefn defn ON defn.CatalogSourceId = '{1}' " +
									   "WHERE feats.Src=type.Id AND feats.Dst=defn.Id",
				sMasterInflectionFeatureTypeId, sMasterInflectionFeatureId);
			list = DbOps.ReadIntsFromCommand(cache, qry, null);
			if (list.Count > 0)
				return true;
			return false;
		}
		/// <summary>
		/// Determine if a FsComplexFeature exists based on the master inflection feature catalog id
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="sMasterInflectionFeatureId">the id to look for</param>
		/// <returns>true if the value exists, false otherwise</returns>
		static public bool HasComplexFeature(FdoCache cache, string sMasterInflectionFeatureId)
		{
			int hvo = GetComplexFeature(cache, sMasterInflectionFeatureId);
			if (hvo > 0)
				return true;
			return false;
		}

		/// <summary>
		/// Search the feature structure types for the (first) one which refers to the specified feature defn
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		/// <param name="iComplexHvo">the FsComplexFeature to match</param>
		/// <returns></returns>
		static public int GetTypeFromFsComplexFeature(FdoCache cache, int iComplexHvo)
		{
			int iTypeHvo;
			string qry = String.Format("select src from FsFeatStrucType_Features "
				+ "WHERE dst = '{0}'",
				iComplexHvo);
			DbOps.ReadOneIntFromCommand(cache, qry, null, out iTypeHvo);
			return iTypeHvo;
		}

		/// <summary>
		/// Add a feature to the feature system (unless it's already there)
		/// Uses the default morpho-syntactic feature system
		/// </summary>
		/// <param name="cache">FDO cache</param>
		/// <param name="item">the node containing a description of the feature</param>
		static public IFsFeatDefn AddFeatureAsXml(FdoCache cache, XmlNode item)
		{
			XmlNode type;
			if (!FeatureCanBeAdded(cache, item, out type))
				return null;
			ILangProject lp = cache.LangProject;
			IFsFeatureSystem featsys = lp.MsFeatureSystemOA as IFsFeatureSystem;
			return AddFeatureAsXml(cache, featsys, item);
		}
		/// <summary>
		/// Add a feature to the feature system (unless it's already there)
		/// </summary>
		/// <param name="cache">FDO cache</param>
		/// <param name="featsys">the feature system to use</param>
		/// <param name="item">the node containing a description of the feature</param>
		static public IFsFeatDefn AddFeatureAsXml(FdoCache cache, IFsFeatureSystem featsys, XmlNode item)
		{
			XmlNode type;
			if (!FeatureCanBeAdded(cache, item, out type))
				return null;
			IFsFeatStrucType fst = FindOrCreateFeatureTypeBasedOnXmlNode(featsys, type.InnerText, item);
			IFsFeatDefn defn = FindOrCreateFeatureDefnBasedOnXmlNode(item, featsys, fst);
			return defn;
		}

		static private bool FeatureCanBeAdded(FdoCache cache, XmlNode item, out XmlNode type)
		{
			if (item == null | cache == null)
			{
				type = null;
				return false;
			}
			type = item.SelectSingleNode("fs/@type");
			if (type == null)
				return false;
			return true;
		}
		/// <summary>
		/// Find feature defn based on XML or create it if not found
		/// </summary>
		/// <param name="item">Xml description of the item</param>
		/// <param name="featsys">FsFeatureSystem to add to</param>
		/// <param name="fst">the type</param>
		static public IFsFeatDefn FindOrCreateFeatureDefnBasedOnXmlNode(XmlNode item,
			IFsFeatureSystem featsys, IFsFeatStrucType fst)
		{
			IFsClosedFeature closed = null;
			IFsComplexFeature complex = null;
			XmlNodeList features = item.SelectNodes("fs/f");
			foreach (XmlNode feature in features)
			{
				XmlNode featName = feature.SelectSingleNode("@name");
				XmlNode fs = feature.SelectSingleNode("fs");
				if (fs != null)
				{ // do complex part
					//
					XmlNode type = fs.SelectSingleNode("@type");
					string sType;
					if (type == null)
						sType = fst.Name.AnalysisDefaultWritingSystem;
					else
						sType = type.InnerText;
					IFsFeatStrucType complexFst = FindOrCreateFeatureTypeBasedOnXmlNode(featsys, sType, item);
					complex = FindOrCreateComplexFeatureBasedOnXmlNodes(featsys, featName, item, fst, complexFst);
					// use the type of the complex feature for the closed feature
					fst = complexFst;
				}
				closed = FindOrCreateClosedFeatureBasedOnXmlNodes(featsys, featName, item);
				SetFeaturesInTypeBasedOnXmlNode(fst, item, closed);
				FindOrCreateSymbolicValueBasedOnXmlNode(feature, closed, item);
			}
			if (complex != null)
				return complex;
			else
				return closed;
		}

		/// <summary>
		/// Find a symbolic feature value or create it if not already there; use XML item to do it
		/// </summary>
		/// <param name="feature">Xml description of the fs</param>
		/// <param name="closed">closed feature containing symbolic feature values</param>
		/// <param name="item">Xml item</param>
		/// <returns>FsSymFeatVal corresponding to the feature</returns>
		static public IFsSymFeatVal FindOrCreateSymbolicValueBasedOnXmlNode(XmlNode feature,
			IFsClosedFeature closed, XmlNode item)
		{
			XmlNode id = feature.SelectSingleNode("ancestor::item[@id][position()=1]/@id");
			if (id == null)
				return null;

			IFsSymFeatVal symFV = null;
#if ForRandy
						bool fAlreadyThere;
			fAlreadyThere = false;
			foreach (IFsSymFeatVal fsfv in closed.ValuesOC)
			{
				if (fsfv.CatalogSourceId == id.InnerText)
				{
					symFV = fsfv;
					fAlreadyThere = true;
					break;
				}
			}
			if (!fAlreadyThere)
			{
				ILgWritingSystemFactory wsFactory = closed.Cache.LanguageWritingSystemFactoryAccessor;
				symFV = closed.ValuesOC.Add(new FsSymFeatVal());
				symFV.CatalogSourceId = id.InnerText;
				XmlNode abbr = item.SelectSingleNode("abbrev");
				SetInnerText(symFV.Abbreviation, wsFactory, abbr);
				XmlNode term = item.SelectSingleNode("term");
				SetInnerText(symFV.Name, wsFactory, term);
				XmlNode def = item.SelectSingleNode("def");
				SetInnerXml(symFV.Description, wsFactory, def);
				symFV.ShowInGloss = true;
			}
#else
			FdoCache cache = closed.Cache;
			int hvo = GetSymbolicValue(cache, id.InnerText);
			if (hvo > 0)
				symFV = CmObject.CreateFromDBObject(cache, hvo) as IFsSymFeatVal;
			else
			{
				symFV = closed.ValuesOC.Add(new FsSymFeatVal());
				string sId = id.InnerText;
				symFV.CatalogSourceId = sId;
				foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
				{
					string sValue = GetValueOfPattern(item, "abbrev", ws);
					if (sValue != null)
						symFV.Abbreviation.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(item, "term", ws);
					if (sValue != null)
						symFV.Name.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(item, "def", ws);
					if (sValue != null)
						symFV.Description.SetAlternative(sValue, ws.Hvo);
				}
				symFV.ShowInGloss = true;
			}
#endif
			return symFV;
		}

		/// <summary>
		/// Find a close feature or create it if not already there; use XML item to do it
		/// </summary>
		/// <param name="featsys">feature system to use</param>
		/// <param name="featName">XML node containing the name of the closed feature</param>
		/// <param name="item">XML item</param>
		/// <returns>FsClosedFeature corresponding to the name</returns>
		static public IFsClosedFeature FindOrCreateClosedFeatureBasedOnXmlNodes(IFsFeatureSystem featsys,
			XmlNode featName, XmlNode item)
		{
			IFsClosedFeature closed = null;
			XmlNode id = item.SelectSingleNode("ancestor::item[@type='feature']/@id");
			if (id == null)
				return closed;
#if ForRandy
						bool fAlreadyThere = false;
			foreach (IFsFeatDefn defn in featsys.FeaturesOC)
			{
				if (defn.CatalogSourceId == id.InnerText)
				{
					closed = defn as FsClosedFeature;
					fAlreadyThere = true;
					break;
				}
			}
			if (!fAlreadyThere)
			{
				// Had following, but causes problem in record clerk browse view because it invokes a PropChanged
				//closed = (FsClosedFeature)featsys.FeaturesOC.Add(new FsClosedFeature());
				// CreateObject creates the entry without a PropChanged.
				ILgWritingSystemFactory wsFactory = cache.LanguageWritingSystemFactoryAccessor;
				int flid = (int) FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
				int featureHvo = cache.CreateObject(FsClosedFeature.kClassId, featsys.Hvo, flid, 0);
				// 0 is fine, since the owning prop is not a sequence.
				closed = FsClosedFeature.CreateFromDBObject(cache, featureHvo);

				closed.CatalogSourceId = id.InnerText;
				XmlNode featureNode = item.SelectSingleNode("ancestor::item[@type='feature']");
				XmlNode abbr = featureNode.SelectSingleNode("abbrev");
				SetInnerText(closed.Abbreviation, wsFactory, abbr);
				XmlNode term = featureNode.SelectSingleNode("term");
				SetInnerText(closed.Name, wsFactory, term);
				XmlNode def = featureNode.SelectSingleNode("def");
				SetInnerXml(closed.Description, wsFactory, def);
			}
#else
			FdoCache cache = featsys.Cache;
			int hvo = GetClosedFeature(cache, id.InnerText);
			if (hvo > 0)
				closed = CmObject.CreateFromDBObject(cache, hvo) as IFsClosedFeature;
			else
			{
				// Had following, but causes problem in record clerk browse view because it invokes a PropChanged
				//closed = (FsClosedFeature)featsys.FeaturesOC.Add(new FsClosedFeature());
				// CreateObject creates the entry without a PropChanged.
				int flid = (int) FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
				int featureHvo = cache.CreateObject(FsClosedFeature.kClassId, featsys.Hvo, flid, 0);
				// 0 is fine, since the owning prop is not a sequence.
				closed = FsClosedFeature.CreateFromDBObject(cache, featureHvo);
				closed.CatalogSourceId = id.InnerText;
				XmlNode featureNode = item.SelectSingleNode("ancestor::item[@type='feature']");
				foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
				{
					string sValue = GetValueOfPattern(featureNode, "abbrev", ws);
					if (sValue != null)
						closed.Abbreviation.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(featureNode, "term", ws);
					if (sValue != null)
						closed.Name.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(featureNode, "def", ws);
					if (sValue != null)
						closed.Description.SetAlternative(sValue, ws.Hvo);
				}
			}
#endif
			return closed;
		}

		/// <summary>
		/// Find a complex feature or create it if not already there; use XML item to do it
		/// </summary>
		/// <param name="featsys">feature system to use</param>
		/// <param name="featName">XML node containing the name of the complex feature</param>
		/// <param name="item">XML item</param>
		/// <param name="fst">feature structure type which refers to this complex feature</param>
		/// <param name="complexFst">feature structure type of the complex feature</param>
		/// <returns>IFsComplexFeature corresponding to the name</returns>
		static public IFsComplexFeature FindOrCreateComplexFeatureBasedOnXmlNodes(IFsFeatureSystem featsys,
			XmlNode featName, XmlNode item, IFsFeatStrucType fst, IFsFeatStrucType complexFst)
		{
			IFsComplexFeature complex = null;
			XmlNode id = item.SelectSingleNode("../../../@id");
			if (id == null)
				return null;
			FdoCache cache = featsys.Cache;
#if ForRandy
			bool fAlreadyThere = false;
			foreach (IFsFeatDefn defn in featsys.FeaturesOC)
			{
				if (defn.CatalogSourceId == id.InnerText)
				{
					complex = defn as IFsComplexFeature;
					fAlreadyThere = true;
					break;
				}
			}
			if (!fAlreadyThere)
			{
#else
			int hvo = GetComplexFeature(cache, id.InnerText);
			if (hvo > 0)
				complex = CmObject.CreateFromDBObject(cache, hvo) as IFsComplexFeature;
			else
			{
#endif
				// Had following, but causes problem in record clerk browse view because it invokes a PropChanged
				//complex = (FsComplexFeature)featsys.FeaturesOC.Add(new FsComplexFeature());
				// CreateObject creates the entry without a PropChanged.
				int flid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
				int featureHvo = cache.CreateObject(FsComplexFeature.kClassId, featsys.Hvo, flid, 0); // 0 is fine, since the owning prop is not a sequence.
				complex = FsComplexFeature.CreateFromDBObject(cache, featureHvo);
				complex.CatalogSourceId = id.InnerText;
				foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
				{
					string sValue = GetValueOfPattern(item, "../../../abbrev", ws);
					if (sValue != null)
						complex.Abbreviation.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(item, "../../../term", ws);
					if (sValue != null)
						complex.Name.SetAlternative(sValue, ws.Hvo);
					sValue = GetValueOfPattern(item, "../../../def", ws);
					if (sValue != null)
						complex.Description.SetAlternative(sValue, ws.Hvo);
				}
				SetFeaturesInTypeBasedOnXmlNode(fst, item, complex);
				complex.TypeRA = complexFst;
			}
			return complex;
		}

		static private void SetFeaturesInTypeBasedOnXmlNode(IFsFeatStrucType fst, XmlNode item, IFsFeatDefn featDefn)
		{
			if (fst == null)
				return;
			XmlNode id = item.SelectSingleNode("../@id");
			bool fAlreadyThere = false;
			foreach (IFsFeatDefn defn in fst.FeaturesRS)
			{
				if (defn.CatalogSourceId == id.InnerText)
				{
					fAlreadyThere = true;
					break;
				}
			}
			if (!fAlreadyThere)
			{
				if (featDefn.ClassID == FsComplexFeature.kclsidFsComplexFeature)
				{ // for complex features, if they have a higher containing feature structure type, then all we want are the closed features
					if (item.SelectSingleNode("ancestor::item[@type='fsType' and not(@status)]") != null)
						return;
				}
				fst.FeaturesRS.Append(featDefn);
			}
		}

		/// <summary>
		/// Find a feature structure type or create it if not already there; use XML item to do it
		/// </summary>
		/// <param name="featsys">feature system to use</param>
		/// <param name="sType">the type</param>
		/// <param name="item">the item that is going to be added</param>
		/// <returns>FsFeatStrucType corresponding to the type</returns>
		static public IFsFeatStrucType FindOrCreateFeatureTypeBasedOnXmlNode(IFsFeatureSystem featsys, string sType, XmlNode item)
		{
			IFsFeatStrucType fst = null;
#if ForRandy
						bool fAlreadyThere = false;
			foreach (IFsFeatStrucType fst2 in featsys.TypesOC)
			{
				if (fst2.CatalogSourceId == sType)
				{
					fAlreadyThere = true;
					fst = fst2;
					break;
				}
			}
			if (!fAlreadyThere)
			{
				fst = featsys.TypesOC.Add(new FsFeatStrucType());
				fst.CatalogSourceId = sType;
				XmlNode parentFsType = item.SelectSingleNode("ancestor::item[@type='fsType' and not(@status)]");
				if (parentFsType == null)
				{ // do not have any real values for abbrev, name, or description.  Just use the abbreviation
					foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
					{
						fst.Abbreviation.SetAlternative(sType, ws.Hvo);
						fst.Name.SetAlternative(sType, ws.Hvo);
					}
				}
				else
				{
					foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
					{
						string sValue = GetValueOfPattern(sType, parentFsType, "abbrev", ws);
						fst.Abbreviation.SetAlternative(sValue, ws.Hvo);
						sValue = GetValueOfPattern(sType, parentFsType, "term", ws);
						fst.Name.SetAlternative(sValue, ws.Hvo);
						sValue = GetValueOfPattern(sType, parentFsType, "def", ws);
						fst.Description.SetAlternative(sValue, ws.Hvo);
					}
				}
			}
#else
			FdoCache cache = featsys.Cache;
			int hvo = GetFeatureType(cache, sType);
			if (hvo > 0)
				fst = CmObject.CreateFromDBObject(cache, hvo) as IFsFeatStrucType;
			else
			{
				fst = featsys.TypesOC.Add(new FsFeatStrucType());
				fst.CatalogSourceId = sType;
				XmlNode parentFsType = item.SelectSingleNode("ancestor::item[@type='fsType' and not(@status)]");
				if (parentFsType == null)
				{ // do not have any real values for abbrev, name, or description.  Just use the abbreviation
					foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
					{
						fst.Abbreviation.SetAlternative(sType, ws.Hvo);
						fst.Name.SetAlternative(sType, ws.Hvo);
					}
				}
				else
				{
					foreach (ILgWritingSystem ws in cache.LangProject.AnalysisWssRC)
					{
						string sValue = GetValueOfPattern(parentFsType, "abbrev", ws);
						if (sValue != null)
							fst.Abbreviation.SetAlternative(sValue, ws.Hvo);
						sValue = GetValueOfPattern(parentFsType, "term", ws);
						if (sValue != null)
							fst.Name.SetAlternative(sValue, ws.Hvo);
						sValue = GetValueOfPattern(parentFsType, "def", ws);
						if (sValue != null)
							fst.Description.SetAlternative(sValue, ws.Hvo);
					}
				}
			}
#endif
			return fst;
		}

		private static string GetValueOfPattern(XmlNode textNode, string sPattern, ILgWritingSystem ws)
		{
			XmlNode xn = textNode.SelectSingleNode(sPattern + "[@ws='" + ws.ICULocale + "']");
			string sValue = null;
			if (xn != null)
				sValue = xn.InnerText;
			return sValue;
		}
	}

	/// <summary>
	/// Summary description for FsClosedValue
	/// </summary>
	public partial class FsClosedValue
	{
		const string m_ksUnknown = "???";

		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;

				tisb.AppendTsString(tsf.MakeString("[", analWs));

				IFsFeatDefn feature = FeatureRA;
				if (feature != null)
					tisb.AppendTsString(tsf.MakeString(feature.Name.BestAnalysisAlternative.Text, analWs));
				else
					tisb.AppendTsString(tsf.MakeString(Strings.ksQuestions, analWs));

				tisb.AppendTsString(tsf.MakeString(" : ", analWs));

				IFsSymFeatVal value = ValueRA;
				if (value != null)
					tisb.AppendTsString(tsf.MakeString(value.Name.BestAnalysisAlternative.Text, analWs));
				else
					tisb.AppendTsString(tsf.MakeString(Strings.ksQuestions, analWs));

				tisb.AppendTsString(tsf.MakeString("]", analWs));

				return tisb.GetString();
			}
		}

		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}
		/// <summary>
		/// A bracketed form e.g. [Gen:Masc]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [Gen:Masc]
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
			}
		}
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// Get feature:value string
		/// </summary>
		/// <param name="fLongForm">use long form</param>
		/// <returns></returns>
		public ITsString GetFeatureValueString(bool fLongForm)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, Cache.DefaultAnalWs);
			string sFeature = GetFeatureString(fLongForm);
			string sValue = GetValueString(fLongForm);
			if ((!fLongForm) &&
				(FeatureRA != null) &&
				(FeatureRA.DisplayToRightOfValues))
			{
				tisb.Append(sValue);
				tisb.Append(sFeature);
			}
			else
			{
				tisb.Append(sFeature);
				tisb.Append(sValue);
			}
			return tisb.GetString();
		}

		private string GetValueString(bool fLongForm)
		{
			string sValue = "";
			if (ValueRA != null)
			{
				if (fLongForm || ValueRA.ShowInGloss)
				{
					sValue = ValueRA.Abbreviation.BestAnalysisAlternative.Text;
					if (sValue == null || sValue.Length == 0)
						sValue = ValueRA.Name.BestAnalysisAlternative.Text;
					if (!fLongForm)
						sValue = sValue + ValueRA.RightGlossSep.AnalysisDefaultWritingSystem;
				}
			}
			else
				sValue = m_ksUnknown;
			return sValue;
		}

		private string GetFeatureString(bool fLongForm)
		{
			string sFeature = "";
			if (FeatureRA != null)
			{
				if (fLongForm || FeatureRA.ShowInGloss)
				{
					sFeature = FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
					if (sFeature == null || sFeature.Length == 0)
						sFeature = FeatureRA.Name.BestAnalysisAlternative.Text;
					if (fLongForm)
						sFeature = sFeature + ":";
					else
						sFeature = sFeature + FeatureRA.RightGlossSep.BestAnalysisAlternative.Text;
				}
			}
			else
				sFeature = m_ksUnknown;
			return sFeature;
		}

		/// <summary>
		/// Overridden to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsClosedValue.FsFeatureSpecificationTags.kflidFeature:
					return CmObject.CreateFromDBObject(m_cache, OwnerHVO);
				case (int)FsClosedValue.FsClosedValueTags.kflidValue:
					return CmObject.CreateFromDBObject(m_cache, FeatureRAHvo);
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>A set of hvos.</returns>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsClosedValue.FsFeatureSpecificationTags.kflidFeature:
					// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
					set = GetFeatureList();
					break;
				case (int)FsClosedValue.FsClosedValueTags.kflidValue:
					set = new Set<int>();
					if (FeatureRAHvo > 0)
					{
						IFsClosedFeature feat = CmObject.CreateFromDBObject(m_cache, FeatureRAHvo) as IFsClosedFeature;
						if (feat != null)
							set.AddRange(feat.ValuesOC.HvoArray);
					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsClosedValue).ValueRAHvo == this.ValueRAHvo;
		}
	}

	/// <summary>
	/// Summary description for FsFeatureSpecification
	/// </summary>
	public partial class FsFeatureSpecification
	{
		/// <summary>
		/// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
		/// </summary>
		/// <returns>list of these features</returns>
		protected Set<int> GetFeatureList()
		{
			Set<int> set = new Set<int>();
			IFsFeatStruc fs = CmObject.CreateFromDBObject(m_cache, OwnerHVO) as IFsFeatStruc;
			if (fs != null)
			{
				int msaOwnerHvo = GetMsaOwnerOfFs(fs);
				IMoMorphSynAnalysis msa = CmObject.CreateFromDBObject(m_cache, msaOwnerHvo) as IMoMorphSynAnalysis;
				if (msa == null)
				{
					TryEndocentricCompound(fs, out msa);
				}
				if (msa != null)
				{
					int posHvo = 0;
					switch (msa.ClassID)
					{
						case MoStemMsa.kclsidMoStemMsa:
							IMoStemMsa stemMsa = msa as IMoStemMsa;
							posHvo = stemMsa.PartOfSpeechRAHvo;
							break;
						case MoInflAffMsa.kclsidMoInflAffMsa:
							IMoInflAffMsa inflMsa = msa as IMoInflAffMsa;
							posHvo = inflMsa.PartOfSpeechRAHvo;
							break;
						case MoDerivAffMsa.kclsidMoDerivAffMsa:
							IMoDerivAffMsa derivMsa = msa as IMoDerivAffMsa;
							if (derivMsa.FromProdRestrictRC.Contains(fs.Hvo))
								posHvo = derivMsa.FromPartOfSpeechRAHvo;
							else
								posHvo = derivMsa.ToPartOfSpeechRAHvo;
							break;
						case MoUnclassifiedAffixMsa.kclsidMoUnclassifiedAffixMsa:
							IMoUnclassifiedAffixMsa unclassMsa = msa as IMoUnclassifiedAffixMsa;
							posHvo = unclassMsa.PartOfSpeechRAHvo;
							break;
					}
					if (posHvo != 0)
					{
						IPartOfSpeech pos = CmObject.CreateFromDBObject(m_cache, posHvo) as IPartOfSpeech;
						while (pos != null)
						{
							switch(GetNonFSOwningFlid(fs))
							{
								case (int)PartOfSpeech.PartOfSpeechTags.kflidDefaultFeatures: // fall through
								case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromMsFeatures: // fall through
								case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToMsFeatures: // fall through
								case (int)MoInflAffMsa.MoInflAffMsaTags.kflidInflFeats: // fall through
								case (int)MoStemMsa.MoStemMsaTags.kflidMsFeatures:
									set.AddRange(pos.InflectableFeatsRC.HvoArray);
									break;
								case (int)MoCompoundRule.MoCompoundRuleTags.kflidToProdRestrict: // fall through
								case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidFromProdRestrict: // fall through
								case (int)MoDerivAffMsa.MoDerivAffMsaTags.kflidToProdRestrict: // fall through
								case (int)MoInflAffMsa.MoInflAffMsaTags.kflidFromProdRestrict: // fall through
								case (int)MoStemMsa.MoStemMsaTags.kflidProdRestrict:
									set.AddRange(pos.BearableFeaturesRC.HvoArray);
									break;
							}
							pos = CmObject.CreateFromDBObject(m_cache, pos.OwnerHVO) as IPartOfSpeech;
						}
					}
				}
			}
			return set;
		}
		private int GetNonFSOwningFlid(IFsFeatStruc fs)
		{
			if (fs == null)
				return 0;
			int flid = fs.OwningFlid;
			int ownerHvo = fs.OwnerHVO; // prime the pump
			while (true)
			{ // pump
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, ownerHvo);
				IFsComplexValue complex = obj as IFsComplexValue;
				if (complex == null)
					return flid; // it's not nested; return owning flid
				IFsFeatStruc fsOwner = CmObject.CreateFromDBObject(m_cache, complex.OwnerHVO) as IFsFeatStruc;
				if (fsOwner == null)
					return 0; // give up; don't know what's going on
				ownerHvo = fsOwner.OwnerHVO;
				flid = fsOwner.OwningFlid;
			}

		}
		private int GetMsaOwnerOfFs(IFsFeatStruc fs)
		{
			if (fs == null)
				return 0;
			int ownerHvo = fs.OwnerHVO; // prime the pump
			while (true)
			{ // pump
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, ownerHvo);
				IMoMorphSynAnalysis msa = obj as IMoMorphSynAnalysis;
				if (msa != null)
					return msa.Hvo; // found msa owner
				IFsComplexValue complex = obj as IFsComplexValue;
				if (complex == null)
					return 0; // give up; don't know what's going on
				IFsFeatStruc fsOwner = CmObject.CreateFromDBObject(m_cache, complex.OwnerHVO) as IFsFeatStruc;
				if (fsOwner == null)
					return 0; // give up; don't know what's going on
				ownerHvo = fsOwner.OwnerHVO;
			}

		}
		/// <summary>
		/// See if the owner of the feature structure is an endocentric compound.
		/// If so, use the stem msa of the head.
		/// </summary>
		/// <param name="fs">feature structure we're checking</param>
		/// <param name="msa">resulting msa, if any</param>
		private void TryEndocentricCompound(IFsFeatStruc fs, out IMoMorphSynAnalysis msa)
		{
			msa = null; // assume it is not
			IMoEndoCompound endo = CmObject.CreateFromDBObject(m_cache, fs.OwnerHVO) as IMoEndoCompound;
			if (endo != null)
			{
				int msaHvo;
				if (endo.HeadLast)
					msaHvo = endo.RightMsaOAHvo;
				else
					msaHvo = endo.LeftMsaOAHvo;
				msa = CmObject.CreateFromDBObject(m_cache, msaHvo) as IMoMorphSynAnalysis;
			}
		}

		/// <summary>
		/// Overridden to handle Features.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsFeatureSpecification.FsFeatureSpecificationTags.kflidFeature:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsFeatureSpecification.FsFeatureSpecificationTags.kflidFeature:
					set = new Set<int>();
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.HvoArray);
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (other == null)
				return false;

			if (other.GetType() != this.GetType())
				return false;

			IFsFeatureSpecification fs = other as IFsFeatureSpecification;
			if (fs.RefNumber != this.RefNumber)
				return false;
			if (fs.ValueState != this.ValueState)
				return false;
			if (fs.FeatureRAHvo != this.FeatureRAHvo)
				return false;
			return true;
		}
	}


	#region FsFeatDefn

	/// <summary>
	/// We need to override the DeleteObjectSideEffects method (see LT-4155).
	/// </summary>
	public partial class FsFeatDefn
	{
		/// <summary>
		/// Get rid of referring FsFeatureSpecification objects as well, plus any of their owners which would then be empty.
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			List<LinkedObjectInfo> backRefs = BackReferences;
			for (int i = 0; i < backRefs.Count; i++)
			{
				LinkedObjectInfo loi = backRefs[i];

				if (loi.RelObjClass == (int)FsFeatureSpecification.kclsidFsFeatureSpecification &&
					loi.RelObjField == (int)FsFeatureSpecification.FsFeatureSpecificationTags.kflidFeature)
				{
					objectsToDeleteAlso.Add(loi.RelObjId);

					int ownerId = m_cache.GetOwnerOfObject(loi.RelObjId);
					int ownerClass = m_cache.GetClassOfObject(ownerId);

					RemoveUnwantedFeatureStuff(objectsToDeleteAlso, ownerId, ownerClass);
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		private void RemoveUnwantedFeatureStuff(Set<int> objectsToDeleteAlso, int hvo, int clid)
		{
			bool fDelete = false;

			switch (clid)
			{
				case (int)FsFeatStruc.kclsidFsFeatStruc:
					int cDisj = m_cache.GetVectorSize(hvo,
						(int)FsFeatStruc.FsFeatStrucTags.kflidFeatureDisjunctions);
					int cSpec = m_cache.GetVectorSize(hvo,
						(int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs);
					if (cDisj + cSpec == 1)
						fDelete = true;
					break;
				case (int)FsFeatStrucDisj.kclsidFsFeatStrucDisj:
					int cContents = m_cache.GetVectorSize(hvo,
						(int)FsFeatStrucDisj.FsFeatStrucDisjTags.kflidContents);
					if (cContents == 1)
						fDelete = true;
					break;
				case (int)FsComplexValue.kclsidFsComplexValue:
					fDelete = true;
					break;
			}

			if (fDelete)
			{
				objectsToDeleteAlso.Add(hvo);
				int ownerId = m_cache.GetOwnerOfObject(hvo);
				int ownerClass = m_cache.GetClassOfObject(ownerId);
				RemoveUnwantedFeatureStuff(objectsToDeleteAlso, ownerId, ownerClass);
			}
		}
	}

	#endregion

	/// <summary>
	/// Summary description for FsComplexValue
	/// </summary>
	public partial class FsComplexValue
	{
		const string m_ksUnknown = "???";

		/// <summary>
		/// Create other required elements.
		/// </summary>
		public override void InitNewInternal()
		{
			base.InitNewInternal();
			ValueOA = new FsFeatStruc();
		}
		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}

		private ITsString GetFeatureValueString(bool fLongForm)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, Cache.DefaultAnalWs);
			string sFeature = GetFeatureString(fLongForm);
			string sValue = GetValueString(fLongForm);
			tisb.Append(sFeature);
			tisb.Append(sValue);
			return tisb.GetString();
		}

		private string GetValueString(bool fLongForm)
		{
			string sValue = "";
			if (ValueOA != null)
			{
				FsFeatStruc fs = ValueOA as FsFeatStruc;
				if (fs != null)
				{
					if (fLongForm)
						sValue = fs.LongName;
					else
						sValue = fs.ShortName;
				}
			}
			else
				sValue = m_ksUnknown;
			return sValue;
		}

		private string GetFeatureString(bool fLongForm)
		{
			string sFeature = "";
			if (FeatureRA != null)
			{
				if (fLongForm || FeatureRA.ShowInGloss)
				{
					sFeature = FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
					if (sFeature == null || sFeature.Length == 0)
						sFeature = FeatureRA.Name.BestAnalysisAlternative.Text;
					if (fLongForm)
						sFeature = sFeature + ":";
					else
						sFeature = sFeature + FeatureRA.RightGlossSep.BestAnalysisAlternative.Text;
				}
			}
			else
				sFeature = m_ksUnknown;
			return sFeature;
		}
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
			}
		}
		/// <summary>
		/// True if the objects are considered equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			IFsAbstractStructure otherValue = (other as IFsComplexValue).ValueOA;
			IFsAbstractStructure thisValue = ValueOA;
			if (otherValue == null && thisValue == null)
				return true;
			return otherValue.IsEquivalent(thisValue);
		}

	}

	/// <summary>
	///
	/// </summary>
	public partial class FsAbstractStructure
	{
		/// <summary>
		/// True if the objects are considered equivalent. This base class has no properties so
		/// the objects are trivially equivalent if of the same class.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual bool IsEquivalent(IFsAbstractStructure other)
		{
			if (other == null)
				return false;
			return other.GetType() == GetType();
		}

	}

	/// <summary>
	/// Summary description for FsFeatStruc
	/// </summary>
	public partial class FsFeatStruc
	{
		/// <summary>
		/// This was added so that we have a more meaningful string presented to the user in the Delete dialog.
		/// This will help when there is no string in the Name field.
		/// </summary>
		public override ITsString DeletionTextTSS
		{
			get
			{
				int userWs = m_cache.DefaultUserWs;
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
				tisb.Append(String.Format(Strings.ksDeleteFeatureSet, " "));
				tisb.AppendTsString(LongNameTSS);
				return tisb.GetString();
			}
		}

		/// <summary>
		/// overridden because the ShortName can very well be empty.
		/// </summary>
		public override ITsString ChooserNameTS
		{
			get
			{
				ITsString result = ShortNameTSS;
				if (result != null && result.Length > 0)
					return result;
				result = LongNameTSS;
				if (result != null && result.Length > 0)
					return result;
				return m_cache.MakeUserTss("an empty feature structure");
			}
		}

		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return GetFeatureValueString(false);
			}
		}

		private ITsString GetFeatureValueString(bool fLongForm)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, Cache.DefaultUserWs);
			int iCount = FeatureSpecsOC.Count;
			if (fLongForm && iCount > 0)
				tisb.Append("[");
			foreach (IFsFeatureSpecification spec in FeatureSpecsOC)
			{
				int i = spec.IndexInOwner;
				if (i >0)
					tisb.Append(" "); // insert space except for first item
				FsClosedValue cv = spec as FsClosedValue;
				if (cv != null)
				{
					if (fLongForm)
					{
						tisb.AppendTsString(cv.LongNameTSS);
					}
					else
					{
						tisb.AppendTsString(cv.ShortNameTSS);
					}
				}
				else
				{
					FsComplexValue complex = spec as FsComplexValue;
					if (complex != null)
					{
						if (fLongForm)
							tisb.AppendTsString(complex.LongNameTSS);
						else
							tisb.AppendTsString(complex.ShortNameTSS);
					}
				}
			}
			if (fLongForm && iCount > 0)
				tisb.Append("]");
			return tisb.GetString();
		}
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public string LongName
		{
			get { return LongNameTSS.Text; }
		}

		/// <summary>
		/// A bracketed form e.g. [NounAgr:[Gen:Masc]]
		/// </summary>
		public ITsString LongNameTSS
		{
			get
			{
				return GetFeatureValueString(true);
			}
		}
		/// <summary>
		/// Provide a "Name" for this (is a long name)
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return LongName;
		}

		/// <summary>
		/// Add features based on an XML description
		/// </summary>
		/// <param name="cache">Cache to use</param>
		/// <param name="item">the node containing a description of the feature</param>
		public void AddFeatureFromXml(FdoCache cache, XmlNode item)
		{
			if (item == null)
				return;

			XmlNode type = item.SelectSingleNode("fs/@type");
			if (type == null)
				return;

			ILangProject lp = cache.LangProject;
			IFsFeatureSystem featsys = lp.MsFeatureSystemOA;
			IFsFeatStrucType fst = FsFeatureSystem.FindOrCreateFeatureTypeBasedOnXmlNode(featsys, type.InnerText, item);
			if (fst != null)
				TypeRA = fst;

			XmlNodeList features = item.SelectNodes("fs/f");
			foreach (XmlNode feature in features)
			{
				XmlNode featName = feature.SelectSingleNode("@name");
				XmlNode fs = feature.SelectSingleNode("fs");
				IFsFeatStruc featStruct = this;
				if (fs != null)
				{ // do complex part
					XmlNode complexType = item.SelectSingleNode("fs/@type");
					string sType;
					if (type == null)
						sType = fst.Name.AnalysisDefaultWritingSystem;
					else
						sType = complexType.InnerText;
					IFsFeatStrucType complexFst = FsFeatureSystem.FindOrCreateFeatureTypeBasedOnXmlNode(featsys, sType, item);
					IFsComplexFeature complexFeat = FsFeatureSystem.FindOrCreateComplexFeatureBasedOnXmlNodes(featsys, featName, item, fst, complexFst);
					IFsComplexValue complexValue = FindOrCreateComplexValue(complexFeat.Hvo);
					if (complexFeat != null)
						complexValue.FeatureRA = complexFeat;
					if (complexValue.ValueOA == null)
						complexValue.ValueOA = new FsFeatStruc();
					featStruct = (IFsFeatStruc)complexValue.ValueOA;
					if (fst != null)
						featStruct.TypeRA = fst;
				}
				IFsClosedFeature closedFeat = FsFeatureSystem.FindOrCreateClosedFeatureBasedOnXmlNodes(featsys, featName, item);
				IFsClosedValue closedValue = (featStruct as FsFeatStruc).FindOrCreateClosedValue(closedFeat.Hvo);
				if (closedFeat != null)
					closedValue.FeatureRA = closedFeat;

				IFsSymFeatVal fsfv = FsFeatureSystem.FindOrCreateSymbolicValueBasedOnXmlNode(feature, closedFeat, item);
				if (fsfv != null)
					closedValue.ValueRA = fsfv;
				//return;
			}
		}

		/// <summary>
		/// Test equivalent of two feature structues (either of which might be null).
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static bool AreEquivalent(IFsFeatStruc first, IFsFeatStruc second)
		{
			if (first == null)
				return second == null || second.IsEmpty;
			return first.IsEquivalent(second);
		}

		/// <summary>
		/// Answer true if this feature structure is 'empty', that is, equivalent to not having
		/// one at all.
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty
		{
			get { return TypeRAHvo == 0 && FeatureDisjunctionsOC.Count == 0 && FeatureSpecsOC.Count == 0; }
		}

		/// <summary>
		/// Answer true if the argument feature structure is equivalent to the recipient.
		/// Review JohnT: should we just call this Equals?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsAbstractStructure other)
		{
			if (other == null)
				return this.IsEmpty;
			if (!base.IsEquivalent(other))
				return false;

			IFsFeatStruc fs = other as IFsFeatStruc;
			if (fs.TypeRAHvo != this.TypeRAHvo)
				return false;
			FdoOwningCollection<IFsFeatureSpecification> otherFeatures = fs.FeatureSpecsOC;
			FdoOwningCollection<IFsFeatureSpecification> thisFeatures = FeatureSpecsOC;
			if (otherFeatures.Count != thisFeatures.Count)
				return false;
			FdoOwningCollection<IFsFeatStrucDisj> otherDisjunctions = fs.FeatureDisjunctionsOC;
			FdoOwningCollection<IFsFeatStrucDisj> thisDisjunctions = FeatureDisjunctionsOC;
			if (otherDisjunctions.Count != thisDisjunctions.Count)
				return false;
			foreach (IFsFeatureSpecification fsOther in otherFeatures)
			{
				bool fMatch = false;
				foreach(IFsFeatureSpecification fsThis in thisFeatures)
				{
					if (fsThis.IsEquivalent(fsOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			foreach (IFsFeatStrucDisj fsdOther in otherDisjunctions)
			{
				bool fMatch = false;
				foreach(IFsFeatStrucDisj fsdThis in thisDisjunctions)
				{
					if (fsdThis.IsEquivalent(fsdOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Find an extant complex value or create a new one if not already there.
		/// </summary>
		/// <param name="complexFeatHvo">hvo of FsComplexFeature to find</param>
		/// <returns>FsComplexValue</returns>
		public IFsComplexValue FindOrCreateComplexValue(int complexFeatHvo)
		{
			foreach (IFsFeatureSpecification spec in FeatureSpecsOC)
			{
				if (spec.FeatureRAHvo == complexFeatHvo)
					return (IFsComplexValue)spec;
			}
			// not found; create a new one
			return (IFsComplexValue)FeatureSpecsOC.Add(new FsComplexValue());
		}
		/// <summary>
		/// Find an extant closed value or create a new one if not already there.
		/// </summary>
		/// <param name="closedFeatHvo">hvo of FsClosedFeature to find</param>
		/// <returns>FsClosedValue</returns>
		public IFsClosedValue FindClosedValue(int closedFeatHvo)
		{
			foreach (IFsFeatureSpecification spec in FeatureSpecsOC)
			{
				if (spec.FeatureRAHvo == closedFeatHvo)
					return (IFsClosedValue)spec;
			}
			// not found; create a new one
			return null;
		}
		/// <summary>
		/// Find an extant closed value or create a new one if not already there.
		/// </summary>
		/// <param name="closedFeatHvo">hvo of FsClosedFeature to find</param>
		/// <returns>FsClosedValue</returns>
		public IFsClosedValue FindOrCreateClosedValue(int closedFeatHvo)
		{
			IFsClosedValue closed = FindClosedValue(closedFeatHvo);
			if (closed != null)
				return closed;
			// not found; create a new one
			return (IFsClosedValue)FeatureSpecsOC.Add(new FsClosedValue());
		}
		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsFeatStruc.FsFeatStrucTags.kflidType:
					return m_cache.LangProject.MsFeatureSystemOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// <summary>
		/// Get a set of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsFeatStruc.FsFeatStrucTags.kflidType:
					set = new Set<int>();
#if NotNow
					// for now when only have exception features...
					FsFeatStrucType fsType = m_cache.LangProject.GetExceptionFeatureType();
					list.Add(fsType.Hvo);
#endif
					set.AddRange(m_cache.LangProject.MsFeatureSystemOA.TypesOC.HvoArray);
					break;

				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tagLongName"></param>
		/// <param name="longNameOldLen"></param>
		public void UpdateFeatureLongName(int tagLongName, int longNameOldLen)
		{
			ITsString tssLongName = LongNameTSS;
			int longNameNewLen = tssLongName.Length;
			// Make sure it is up to date in the cache (since LongNameTSS is not computer-every-time.
			Cache.VwCacheDaAccessor.CacheStringProp(Hvo, tagLongName, tssLongName);
			// Let the view know it has changed.
			Cache.PropChanged(Hvo, tagLongName, 0, longNameNewLen, longNameOldLen);
		}

	}

	/// <summary>
	/// Summary description for FsNegatedValue.
	/// </summary>
	public partial class FsNegatedValue
	{
		/// <summary>
		/// Override to handle reference props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)FsNegatedValue.FsFeatureSpecificationTags.kflidFeature:
					return m_cache.LangProject.MsFeatureSystemOA;
				case (int)FsNegatedValue.FsNegatedValueTags.kflidValue:
					if (FeatureRAHvo > 0)
						return CmObject.CreateFromDBObject(m_cache, FeatureRAHvo);
					else
						return null;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
		/// <summary>
		/// Get a list of hvos that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of IDs.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override Set<int> ReferenceTargetCandidates(int flid)
		{
			Set<int> set = null;
			switch (flid)
			{
				case (int)FsNegatedValue.FsFeatureSpecificationTags.kflidFeature:
					// Find all exception features for the "owning" PartOfSpeech and all of its owning POSes
					set = GetFeatureList();
					break;
				case (int)FsNegatedValue.FsNegatedValueTags.kflidValue:
					set = new Set<int>();
					if (FeatureRAHvo > 0)
					{
						IFsClosedFeature feat = CmObject.CreateFromDBObject(m_cache, FeatureRAHvo) as IFsClosedFeature;
						if (feat != null)
							set.AddRange(feat.ValuesOC.HvoArray);

					}
					break;
				default:
					set = base.ReferenceTargetCandidates(flid);
					break;
			}
			return set;
		}
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsSharedValue).ValueRAHvo == this.ValueRAHvo;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class FsDisjunctiveValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsDisjunctiveValue).ValueRC.IsEquivalent(ValueRC);
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class FsOpenValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// Since FdoOpenValue isn't used yet, this is something of a skeleton. We just check
		/// the analysis writing systems.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return this.Value.AnalysisDefaultWritingSystem == (other as IFsOpenValue).Value.AnalysisDefaultWritingSystem;
		}

	}


	/// <summary>
	///
	/// </summary>
	public partial class FsFeatStrucDisj
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsAbstractStructure other)
		{
			if (!base.IsEquivalent(other))
				return false;

			FdoOwningCollection<IFsFeatStruc> otherContents = (other as IFsFeatStrucDisj).ContentsOC;
			FdoOwningCollection<IFsFeatStruc> thisContents = ContentsOC;
			if (otherContents.Count != thisContents.Count)
				return false;
			foreach (IFsFeatStruc fsOther in otherContents)
			{
				bool fMatch = false;
				foreach(IFsFeatStruc fsThis in thisContents)
				{
					if (fsThis.IsEquivalent(fsOther))
					{
						fMatch = true;
						break;
					}
				}
				if (!fMatch)
					return false;
			}
			return true;
		}

	}

	/// <summary>
	///
	/// </summary>
	public partial class FsSharedValue
	{
		/// <summary>
		/// True if the two are 'equivalent'.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool IsEquivalent(IFsFeatureSpecification other)
		{
			if (!base.IsEquivalent(other))
				return false;

			return (other as IFsSharedValue).ValueRAHvo == this.ValueRAHvo;
		}
	}

	/// <summary>
	/// Summary description for FsSymFeatVal
	/// </summary>
	public partial class FsSymFeatVal
	{
		/// <summary>
		/// The shortest, non abbreviated label for the content of this object.
		/// </summary>
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}
		/// <summary>
		/// The shortest label for the content of this object.
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				return Name.BestAnalysisAlternative;
			}
		}
		/// <summary>
		/// Set abbreviation and name; also set show in gloss to true
		/// </summary>
		/// <param name="sAbbrev"></param>
		/// <param name="sName"></param>
		public void SimpleInit(string sAbbrev, string sName)
		{
			foreach (ILgWritingSystem ws in m_cache.LangProject.AnalysisWssRC)
			{
				Abbreviation.SetAlternative(sAbbrev, ws.Hvo);
				if (ws.ICULocale == "en")
					Name.SetAlternative(sName, ws.Hvo);
			}
			ShowInGloss = true;
		}

		/// <summary>
		/// This method is the one to override if you need side effects when DeleteUnderlyingObject
		/// is called. If other objects should be deleted also, do NOT delete them directly; this
		/// tends to produce abysmal performance. Rather, add them to objectsToDeleteAlso, and the
		/// whole lot (including this) will be deleted in one relatively efficient operation.
		/// You should not modify objectsToDeleteAlso except to add HVOs to it.
		/// You must not use the FDO object after calling this, it has been put into the deleted state.
		/// </summary>
		/// <param name="objectsToDeleteAlso">hashtable of HVOs (value typically just true, it's really a set).</param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			List<LinkedObjectInfo> linkedObjects = LinkedObjects;
			foreach (LinkedObjectInfo loi in linkedObjects)
			{
				if (loi.RelObjClass == FsClosedValue.kclsidFsClosedValue)
					objectsToDeleteAlso.Add(loi.RelObjId);
			}

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

	}

	/// <summary>
	/// Summary description for UserView.
	/// </summary>
	public partial class UserView
	{
		#region Data members not stored in the database.

		/// <summary>Alternative writing system for MultiUnicode fields anywhere in ownership tree.</summary>
		protected int m_wsAlt;
		// TODO TomB: Determine what this is in .Net and how we should deal with it.
		/// <summary>Index of the child window in the MDI client corresponding to the view.</summary>
		protected int m_iwndClient;

		// NOTE: m_MaxLines and m_fIgnoreHier are only used in Browse views.
		// This is a bit of a problem since we want to be able to save and load in AppCore
		// without knowing the view type, which is defined at the application level. For the
		// moment we can handle this since there is only one type of field that is currently
		// storing info in details. Once we go beyond this, we will need to pass details back
		// to the application to parse.
		/// <summary>max line per record in Browse view</summary>
		protected int m_nMaxLines;
		/// <summary>Ignore Hierarchy. Used in Browse view.</summary>
		protected bool m_fIgnoreHier;

		#endregion	// Data members not stored in the database.

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the task (on the sidebar) which owns this view.
		/// </summary>
		/// <exception cref="ArgumentException">Throws an exception when trying to set the task
		/// name and the Name property already contains a value (that doesn't consist of task
		/// name and short view name separated by '/')</exception>
		/// ------------------------------------------------------------------------------------
		public string TaskName
		{
			get
			{
				string taskName, shortName;
				ParseName(out taskName, out shortName);
				return taskName;
			}
			set
			{
				string taskName, shortName;
				if (ParseName(out taskName, out shortName))
					Name.SetAlternative(value + "/" + shortName, Cache.DefaultUserWs);
				else
					throw new ArgumentException("The Name property already has a value.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Name of view within its task (on the sidebar)
		/// </summary>
		/// <exception cref="ArgumentException">Throws an exception when trying to set the short
		/// name and the Name property already contains a value (that doesn't consist of task
		/// name and short view name separated by '/')</exception>
		/// ------------------------------------------------------------------------------------
		public string ViewNameShort
		{
			get
			{
				string taskName, shortName;
				ParseName(out taskName, out shortName);
				return shortName;
			}
			set
			{
				string taskName, shortName;
				if (ParseName(out taskName, out shortName))
					Name.SetAlternative(taskName + "/" + value, Cache.DefaultUserWs);
				else
					throw new ArgumentException("The Name property already has a value.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Alternative writing system for MultiUnicode fields anywhere in ownership tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int AlternativeEncoding
		{
			get {return m_wsAlt;}
			set	{m_wsAlt = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Index of the child window in the MDI client corresponding to the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WindowClient
		{
			get {return m_iwndClient;}
			set {m_iwndClient = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ignore Hierarchy. Used in Browse view.
		/// </summary>
		/// <remarks>See MaxBrowseLines for further information.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreHierarchy
		{
			get {return m_fIgnoreHier;}
			set
			{
				m_fIgnoreHier = value;
				ResetDetails();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Maximum lines per record in Browse view.
		/// </summary>
		/// <remarks>NOTE: m_MaxLines and m_fIgnoreHier are only used in Browse views.
		/// This is a bit of a problem since we want to be able to save and load in AppCore
		/// without knowing the view type, which is defined at the application level. For the
		/// moment we can handle this since there is only one type of field that is currently
		/// storing info in details. Once we go beyond this, we will need to pass details back
		/// to the application to parse.</remarks>
		/// ------------------------------------------------------------------------------------
		public int MaxBrowseLines
		{
			get {return m_nMaxLines;}
			set
			{
				m_nMaxLines = value;
				ResetDetails();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// FactoryView if true, AddedView (user-defined view) if false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FactoryView
		{
			get { return System; }
			set { System = value;}
		}
		#endregion	// Properties

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the details.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetDetails()
		{
			byte[] vb = new Byte[4];
			if (m_fIgnoreHier)
				vb[0] = (byte)(0x80000000 | (uint)m_nMaxLines);
			else
				vb[0] = (byte)((uint)m_nMaxLines);
			Details = vb;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the Name property and return the task name and view short name.
		/// </summary>
		/// <param name="taskName">Name of the task.</param>
		/// <param name="shortName">The short name.</param>
		/// <returns><c>true</c> if the Name contains a task name and short view name (or is
		/// empty), <c>false</c> if the Name property has a value but doesn't contain a '/'
		/// character.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ParseName(out string taskName, out string shortName)
		{
			taskName = string.Empty;
			shortName = string.Empty;
			if (Name == null || string.IsNullOrEmpty(Name.UserDefaultWritingSystem))
				return true;

			string[] nameParts = Name.UserDefaultWritingSystem.Split('/');
			if (nameParts.Length < 2)
				return false;

			taskName = nameParts[0];
			shortName = nameParts[1];
			return true;
		}
		#endregion

		#region Construction and Initializing

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified data in the fdo cache.
		/// </summary>
		/// <param name="fdoCache">The fdo cache.</param>
		/// <param name="alIDs">The al I ds.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static UserViewCollection Load(FdoCache fdoCache, Set<int> alIDs)
		{
			UserViewCollection uvc = new UserViewCollection(fdoCache);
			foreach (int hvo in alIDs)
			{
				uvc.Add((UserView)CmObject.CreateFromDBObject(fdoCache,
					hvo,
					UserView.GetTypeFromFWClassID(fdoCache, UserView.kClassId),
					false/*No validation*/,
					true/*Load into cache*/));
			}
			return uvc;
		}

		#endregion	// Construction and Initializing

		/// <summary>
		/// Make a deep copy of a UserView.
		/// </summary>
		/// <returns>A deep copy of a UserView, but with new database IDs for all objects.</returns>
		internal IUserView Clone()
		{
			int iID = m_cache.CreateObject(ClassID);
			UserView uvClone = new UserView(m_cache , iID);
			// Regular properties.
			uvClone.Name.AnalysisDefaultWritingSystem = Name.AnalysisDefaultWritingSystem;
			uvClone.Name.VernacularDefaultWritingSystem = Name.VernacularDefaultWritingSystem;
			uvClone.Type = Type;
			uvClone.App = App;
			uvClone.System = System;
			uvClone.SubType = SubType;
			// UserViewRec objects.
			foreach (UserViewRec uvr in RecordsOC)
			{
				IUserViewRec uvrClone = uvr.Clone(uvClone);
			}
			// Handle extra data members.
			uvClone.m_wsAlt = m_wsAlt;
			uvClone.m_iwndClient = m_iwndClient;
			uvClone.m_fIgnoreHier = m_fIgnoreHier;
			uvClone.m_nMaxLines = m_nMaxLines;
			ResetDetails();

			return uvClone as IUserView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the CmPossibilityList that should be used to supply the
		/// possibilities for the given field in this view.
		/// </summary>
		/// <param name="flid">The field ID</param>
		/// <returns>HVO of a CmPossibilityList, or 0 of the given field is not displayed in
		/// this view or if it is not associated with a possibility list</returns>
		/// ------------------------------------------------------------------------------------
		public int GetPossibilityListForProperty(int flid)
		{
			foreach (IUserViewRec rec in RecordsOC)
				foreach (IUserViewField field in rec.FieldsOS)
					if (field.Flid == flid && field.PossListRAHvo > 0)
						return field.PossListRAHvo;
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid to filter on.
		/// </summary>
		/// <param name="hvoOwner">The hvo of the requested class.</param>
		/// <returns>The flid to filter on</returns>
		/// ------------------------------------------------------------------------------------
		public int GetFlidForPropOwner(int hvoOwner)
		{
			int classOfOwner = m_cache.GetClassOfObject(hvoOwner);

			foreach (IUserViewRec rec in RecordsOC)
			{
				if (rec.Clsid == classOfOwner) // e.g., ScrBookAnnotations
				{
					Debug.Assert(rec.FieldsOS.Count == 1,
						"TODO: Handle filtering on records with more than one field in the user view spec");
					IUserViewField field = rec.FieldsOS[0];
					return field.Flid; // e.g. Notes
				}
			}
			Debug.Fail("UserViewSpec is not complete. Can't find flid to filter on");
			Logger.WriteEvent("UserViewSpec is not complete. Can't find flid to filter on");
			return 0;
		}
	}


	/// <summary>
	/// Summary description for UserViewField.
	/// </summary>
	public partial class UserViewField
	{
		#region Data members

		/// <summary></summary>
		public const int  kdxpDefBrowseColumn = 100;

		private FdoObjectSet<IUserViewField> m_osUserViewFields;

		// Information used for Possibility lists.
		// Name format (kpntName/kpntNameAndAbbrev/kpntAbbreviation).
		private PossNameType m_pnt = PossNameType.kpntName;
		// Show names using hierarchy (e.g., noun:common).
		private bool m_fHier = false;
		// List multiple items vertically instead of in a paragraph.
		private bool m_fVert = false;

		// Information used for hierarchical fields (e.g., subrecords).
		// Always expand tree nodes.
		private bool m_fExpand = false;
		// Way to show outline numbers (konsNone/konsNum/konsNumDot).
		private OutlineNumSty m_ons = OutlineNumSty.konsNone;

		// Information for document view.
		private bool m_fHideLabel = true; // true to hide label in Document view.

		// Information used for browse view.
		private int m_dxpColumn = kdxpDefBrowseColumn;

		// Flag used internally by custom field dialog.
		private bool m_fNewFld = false;

		#endregion	// Data members

		#region Properties

		/// <summary>
		///
		/// </summary>
		public FdoObjectSet<IUserViewField> Subfields
		{
			get
			{
				if (m_osUserViewFields == null)
				{
					string sSql = string.Format("select ID, Class$, OwnOrd$ from UserViewField_" +
						" where SubFieldOf={0} order by OwnOrd$", m_hvo );
					m_osUserViewFields = new FdoObjectSet<IUserViewField>(m_cache, sSql, true); // has ord column
				}
				return m_osUserViewFields;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the class name for the object that owns the displayed field.
		/// </summary>
		/// <value>The name of the class.</value>
		/// ------------------------------------------------------------------------------------
		public string ClassName
		{
			get
			{
				return m_cache.MetaDataCacheAccessor.GetOwnClsName((uint)Flid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the field being displayed.
		/// </summary>
		/// <value>The name of the field.</value>
		/// ------------------------------------------------------------------------------------
		public string FieldName
		{
			get
			{
				return m_cache.MetaDataCacheAccessor.GetFieldName((uint)Flid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of the possibility name.
		/// </summary>
		/// <value>The type of the possibility name.</value>
		/// ------------------------------------------------------------------------------------
		public PossNameType PossibilityNameType
		{
			get
			{
				return m_pnt;
			}
			set
			{
				m_pnt = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool IsHierarchy
		{
			get
			{
				return m_fHier;
			}
			set
			{
				m_fHier = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool IsVertical
		{
			get
			{
				return m_fVert;
			}
			set
			{
				m_fVert = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool ExpandOutline
		{
			get
			{
				return m_fExpand;
			}
			set
			{
				m_fExpand = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public OutlineNumSty OutlineNumberStyle
		{
			get
			{
				return m_ons;
			}
			set
			{
				m_ons = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool HideLabel
		{
			get
			{
				return m_fHideLabel;
			}
			set
			{
				m_fHideLabel = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public int BrowseColumnWidth
		{
			get
			{
				return m_dxpColumn;
			}
			set
			{
				Debug.Assert(value >= 0);
				m_dxpColumn = value;
				// TODO: Rewrite all properties that call ResetDetails
				// to live without their data members.
				// They should just deal directly with ResetDetails
				// for setting, and some other extractor method(s)
				// to get relevant data from Details.
				ResetDetails();
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool IsNewField
		{
			get
			{
				return m_fNewFld;
			}
			set
			{
				m_fNewFld = value;
			}
		}

		#endregion	// Properties

		#region Construction and Initializing

		private void ResetDetails()
		{
			byte[] vb = new Byte[8];

			// Convert bools to ints.
			int iVert = 0;
			if (m_fVert)
				iVert = 1;
			int iHier = 0;
			if (m_fHier)
				iHier = 1;
			int iExpand = 0;
			if (m_fExpand)
				iExpand = 1;
			int n;
			switch (this.Type)
			{
				case (int)FldType.kftRefAtomic:
				case (int)FldType.kftRefCombo:
				case (int)FldType.kftRefSeq:
				{
					// It is a Choices List field type.
					// The second int stores m_fVert as bit 31,
					// m_fHier as bit 30, and m_pnt as the low bits (29-0).
					n = iVert << 31 | iHier << 30 | (int)m_pnt;
					SetInByteArray(ref vb, 4, n);
					break;
				}
				case (int)FldType.kftExpandable:
				{
					// It is an Expandable List field type.
					// The second int stores m_fExpand as bit 31, m_fHier as bit 30,
					// and m_pnt as the low bits (29-0).
					n = iExpand << 31 | iHier << 30 | (int)m_pnt;
					SetInByteArray(ref vb, 4, n);
					break;
				}
				case (int)FldType.kftSubItems:
				{
					// It is a Hierarchical field type.
					// The second int stores m_fExpand as bit 31 and m_ons as low bits.
					n = iExpand << 31 | (int)m_ons;
					SetInByteArray(ref vb, 4, n);
					break;
				}
				default:
				{
					// Other types.
					// The second int is not stored.
					vb = new byte[4];
					break;
				}
			}

			int iHideLabel = 0;
			if (m_fHideLabel)
				iHideLabel = 1;
			n = iHideLabel << 31 | m_dxpColumn;
			SetInByteArray(ref vb, 0, n);
			Details = vb;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an integer value in the byte array
		/// </summary>
		/// <param name="vb">Byte array</param>
		/// <param name="iStart">Start index in byte array</param>
		/// <param name="value">Integer value</param>
		/// ------------------------------------------------------------------------------------
		private void SetInByteArray(ref byte[] vb, int iStart, int value)
		{
			Debug.Assert(iStart + 4 <= vb.Length);
			vb[iStart+3] = (byte)((value & 0xff000000) >> 24);
			vb[iStart+2] = (byte)((value & 0x00ff0000) >> 16);
			vb[iStart+1] = (byte)((value & 0x0000ff00) >> 8);
			vb[iStart]   = (byte) (value & 0x000000ff);
		}

		#endregion	// Construction and Initializing

		/// <summary>
		/// Makes a deep copy of a UserViewField, which is really a nested object.
		/// The C++ code called this clone a FldSpec, rather than a top-level BlockSpec.
		/// </summary>
		/// <param name="uvrCloneOwner">The owner for the new clone.</param>
		/// <param name="hvoSuperField">Database ID for the Subfield property.</param>
		/// <returns>
		/// A deep copy of a UserViewField, but with new database IDs for all objects.
		/// </returns>
		internal IUserViewField Clone(IUserViewRec uvrCloneOwner, int hvoSuperField)
		{
			IUserViewField uvfClone = Clone(uvrCloneOwner);
			uvfClone.SubfieldOfRAHvo = hvoSuperField;

			return uvfClone;
		}

		/// <summary>
		/// Makes a deep copy of a UserViewField.
		/// </summary>
		/// <param name="uvrCloneOwner">The owner for the new clone.</param>
		/// <returns>A deep copy of a UserViewField, but with new database IDs for all objects.</returns>
		internal IUserViewField Clone(IUserViewRec uvrCloneOwner)
		{
			UserViewField uvfClone = (UserViewField)uvrCloneOwner.FieldsOS.Append(new UserViewField());
			Debug.Assert(uvfClone != null);
			uvfClone.Label.AnalysisDefaultWritingSystem = this.Label.AnalysisDefaultWritingSystem;
			uvfClone.Label.VernacularDefaultWritingSystem = this.Label.VernacularDefaultWritingSystem;
			uvfClone.HelpString.AnalysisDefaultWritingSystem = HelpString.AnalysisDefaultWritingSystem;
			uvfClone.HelpString.VernacularDefaultWritingSystem = HelpString.VernacularDefaultWritingSystem;
			uvfClone.Type = Type;
			uvfClone.Flid = Flid;
			uvfClone.Visibility = Visibility;
			uvfClone.Required = Required;
			uvfClone.Style = Style;
			//uvfClone.WritingSystem = WritingSystem;
			uvfClone.WritingSystemRAHvo = WritingSystemRAHvo;
			uvfClone.WsSelector = WsSelector;
			uvfClone.IsCustomField = IsCustomField;
			uvfClone.PossListRAHvo = PossListRAHvo;
			// Other members.
			uvfClone.m_pnt = m_pnt;
			uvfClone.m_fHier = m_fHier;
			uvfClone.m_fVert = m_fVert;
			uvfClone.m_fExpand = m_fExpand;
			uvfClone.m_ons = m_ons;
			uvfClone.m_fHideLabel = m_fHideLabel;
			uvfClone.m_dxpColumn = m_dxpColumn;
			uvfClone.m_fNewFld = m_fNewFld;
			uvfClone.ResetDetails();
			// SubfieldOfRA is handled by caller, since we really want the target to be a new
			// clone, not the original object.

			return uvfClone as IUserViewField;
		}
	}


	/// <summary>
	/// Summary description for UserViewRec.
	/// </summary>
	public partial class UserViewRec
	{
		#region Data members

		private FdoObjectSet<IUserViewField> m_osUserViewFields;
		// View type that helps sort out BlockSpec.
		private UserViewType m_vwt;

		#endregion	// Data members

		#region Properties

		/// <summary>
		/// Select group of UserViewField objects (in FieldsOS property),
		/// which are not subfields of another UserViewField.
		/// </summary>
		public FdoObjectSet<IUserViewField> BlockSpecs
		{
			get
			{
				if (m_osUserViewFields == null)
				{
					string sSql = string.Format("select ID, Class$, OwnOrd$ from UserViewField_" +
						" where Owner$={0} and SubFieldOf is null" +
						" order by OwnOrd$", m_hvo );
					m_osUserViewFields = new FdoObjectSet<IUserViewField>(m_cache, sSql, true);	// has ord column
				}
				return m_osUserViewFields;
			}
		}

		/// <summary>
		///
		/// </summary>
		public UserViewType ViewType
		{
			get
			{
				return m_vwt;
			}
			set
			{
				m_vwt = value;
			}
		}

		#endregion	// Properties

		#region Construction and Initializing

		/// <summary>
		///
		/// </summary>
		/// <param name="clid"></param>
		/// <param name="iLevel"></param>
		/// <param name="vwt"></param>
		public void Init(int clid, int iLevel, UserViewType vwt)
		{
			Clsid = clid;
			Level = iLevel;
			m_vwt = vwt;
		}

		#endregion	// Construction and Initializing

		/******* C++ methods.
					FldSpec * AddField(bool fTopLevel, int stidLabel, int flid, FldType ft, int ws = kwsAnal,
						int stidHelp = 0, FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq,
						LPCOLESTR pszSty = L"", bool fCustFld = false, bool fHideLabel = false);

					void AddPossField(bool fTopLevel, int stidLabel, int flid, FldType ft, int stidHelp,
						HVO hvoPssl, PossNameType pnt = kpntName, bool fHier = false, bool fVert = false,
						int ws = kwsAnal, FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq,
						LPCOLESTR pszSty = L"", bool fCustFld = false, bool fHideLabel = false);

					void AddHierField(bool fTopLevel, int stidLabel, int flid, int ws = kwsAnal,
						int stidHelp = 0, OutlineNumSty ons = konsNone, bool fExpand = false,
						FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq, LPCOLESTR pszSty = L"",
						bool fCustFld = false, bool fHideLabel = false);
		*******/

		/// <summary>
		/// Make a deep copy of a UserViewRec.
		/// </summary>
		/// <returns>A deep copy of a UserViewRec, but with new database IDs for all objects.</returns>
		internal IUserViewRec Clone(IUserView uvCloneOwner)
		{
			UserViewRec uvrClone =
				(UserViewRec)uvCloneOwner.RecordsOC.Add(new UserViewRec());
			Debug.Assert(uvrClone != null);
			uvrClone.Clsid = Clsid;
			// Not used yet. uvrClone.Details = Details;
			uvrClone.Level = Level;
			uvrClone.ViewType = ViewType;
			// Clone each "BlockSpec".
			foreach (UserViewField uvfBlock in BlockSpecs)
			{
				IUserViewField uvfBlockClone = uvfBlock.Clone(uvrClone);
				foreach (UserViewField uvfSubfield in uvfBlock.Subfields)
				{
					IUserViewField uvfSfClone = uvfSubfield.Clone(uvrClone, uvfBlockClone.Hvo);
				}
			}

			return uvrClone as IUserViewRec;
		}
	}

	/// <summary>
	///
	/// </summary>
	public partial class CmPerson
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		/// <param name="flid">The reference property that can store the IDs.</param>
		/// <returns>An array of hvos.</returns>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)CmPerson.CmPersonTags.kflidPositions:
					return m_cache.LangProject.PositionsOA;
				case (int)CmPerson.CmPersonTags.kflidPlacesOfResidence:
					return m_cache.LangProject.LocationsOA;
				case (int)CmPerson.CmPersonTags.kflidEducation:
					return m_cache.LangProject.EducationOA;
				case (int)CmPerson.CmPersonTags.kflidPlaceOfBirth:
					return m_cache.LangProject.LocationsOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}
	}

	#region CmMedia

	/// <summary>
	///
	/// </summary>
	public partial class CmMedia
	{
		/// <summary>
		/// </summary>
		public void InitializeNewMedia(string sFile, string sLabel, string sCmFolderName,
			int ws)
		{
			if (sLabel != null && sLabel.Length != 0)
				this.Label.SetAlternative(sLabel, ws);

			if (sFile != null && sFile.Length != 0)
			{
				ICmFolder folder = CmFolder.FindOrCreateFolder(m_cache, (int)LangProject.LangProjectTags.kflidMedia, sCmFolderName);
				this.MediaFileRA = CmFile.FindOrCreateFile(folder, sFile);
			}
		}

		/// <summary>
		/// Add the CmFile referenced by MediaFile to the list of objects to delete if we're
		/// the only reference to it.
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso,
			SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			if (MediaFileRAHvo != 0)
			{
				List<LinkedObjectInfo> backRefs = MediaFileRA.BackReferences;
				if (backRefs != null && backRefs.Count == 1)
				{
					LinkedObjectInfo loi = backRefs[0];
					if (loi != null && loi.RelObjId == this.Hvo)
						objectsToDeleteAlso.Add(MediaFileRAHvo);
				}
			}
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
	}
	#endregion // CmMedia

	#region CmFolder

	/// <summary>
	///
	/// </summary>
	public partial class CmFolder
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Locate CmFolder with given name or create it, if neccessary
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="flid">The field identifier that owns sFolder</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		public static ICmFolder FindOrCreateFolder(FdoCache cache, int flid, string sFolder)
		{
			FdoOwningCollection<ICmFolder> prop;
			switch (flid)
			{
				case (int)LangProject.LangProjectTags.kflidMedia:
					prop = cache.LangProject.MediaOC;
					break;
				case (int)LangProject.LangProjectTags.kflidPictures:
					prop = cache.LangProject.PicturesOC;
					break;
				default:
					return null; // We have an invalid Flid.
			}
			foreach (ICmFolder folder in prop)
			{
				if (folder.Name != null)
				{
					if (folder.Name.AnalysisDefaultWritingSystem != null &&
						folder.Name.AnalysisDefaultWritingSystem.CompareTo(sFolder) == 0)
						return folder;
					if (folder.Name.BestAnalysisAlternative != null &&
						folder.Name.BestAnalysisAlternative.Text != null &&
						folder.Name.BestAnalysisAlternative.Text.CompareTo(sFolder) == 0)
						return folder;
					if (folder.Name.BestAnalysisVernacularAlternative != null &&
						folder.Name.BestAnalysisVernacularAlternative.Text != null &&
						folder.Name.BestAnalysisVernacularAlternative.Text.CompareTo(sFolder) == 0)
						return folder;
					if (folder.Name.UserDefaultWritingSystem != null &&
						folder.Name.UserDefaultWritingSystem.CompareTo(sFolder) == 0)
						return folder;
				}
			}

			ICmFolder foldr = prop.Add(new CmFolder());
			foldr.Name.AnalysisDefaultWritingSystem = sFolder;

			return foldr;
		}
	}
	#endregion // CmFolder

	#region CmFile

	/// <summary>
	///
	/// </summary>
	public partial class CmFile
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the InternalPath (relative to the FW Data directory)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InternalPath
		{
			get
			{
				return InternalPath_Generated;
			}
			set
			{
				InternalPath_Generated = value;
			}
		}

		/// <summary>
		/// Gets the base filename of the InternalPath.
		/// </summary>
		public string InternalBasename
		{
			get
			{
				string path = InternalPath_Generated;
				return Path.GetFileName(path);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the absolute InternalPath (either the InternalPath itself, or if relative,
		/// combined with either the project's external link directory or the FW Data Directory)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AbsoluteInternalPath
		{
			get
			{
				string internalPath = InternalPath;
				if (String.IsNullOrEmpty(internalPath))
					internalPath = EmptyFileName;
				if (Path.IsPathRooted(internalPath))
					return internalPath;
				string directory = m_cache.LangProject.ExternalLinkRootDir;
				return Path.Combine(directory, internalPath);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the empty name of the file.
		/// </summary>
		/// <value>The empty name of the file.</value>
		/// ------------------------------------------------------------------------------------
		public static string EmptyFileName
		{
			get { return ".__NONE__"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the internal path either as a relative path or an absolute path.
		/// </summary>
		/// <param name="srcFilename">The filename.</param>
		/// ------------------------------------------------------------------------------------
		public void SetInternalPath(string srcFilename)
		{
			string directory = m_cache.LangProject.ExternalLinkRootDir;
			if (srcFilename.ToLowerInvariant().StartsWith(directory.ToLowerInvariant()) &&
				srcFilename.Length > directory.Length + 1)
			{
				srcFilename = srcFilename.Substring(directory.Length);
				if (srcFilename[0] == Path.DirectorySeparatorChar)
					srcFilename = srcFilename.Substring(1);
			}
			this.InternalPath = srcFilename;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds another CmFile object whose AbsoluteInternalPath is the same as srcFile. If
		/// one is found then the CmFile object is returned. Otherwise a new one is created for
		/// the srcFile.
		/// </summary>
		/// <param name="folder">CmFolder whose CmFile collection will be searched.</param>
		/// <param name="srcFile">Full path of the picture file being searched for.</param>
		/// <returns>A CmFile object</returns>
		/// ------------------------------------------------------------------------------------
		public static ICmFile FindOrCreateFile(ICmFolder folder, string srcFile)
		{
			if (String.IsNullOrEmpty(srcFile))
				throw new ArgumentException("File path not specified.", "srcFile");

			char[] bad = Path.GetInvalidPathChars();
			int idx = srcFile.IndexOfAny(bad);
			if (idx >= 0)
				throw new ArgumentException("File path (" + srcFile + ") contains at least one invalid character.", "srcFile");

			if (!Path.IsPathRooted(srcFile))
				throw new ArgumentException("File does not have a rooted pathname: " + srcFile, "srcFile");

			string newName = Path.GetFileName(srcFile);
			foreach (ICmFile file in folder.FilesOC)
			{
				string internalName = Path.GetFileName(file.AbsoluteInternalPath);
				if (internalName == newName)
				{
					if (FileUtils.AreFilesIdentical(srcFile, file.AbsoluteInternalPath))
						return file;
				}
			}

			ICmFile cmFile = folder.FilesOC.Add(new CmFile());
			((CmFile)cmFile).SetInternalPath(srcFile);

			return cmFile;
		}
	}
	#endregion // CmFile

	#region CmPicture
	public partial class CmPicture
	{
		/// <summary>
		/// Get the sense number of the owning LexSense.
		/// </summary>
		public ITsString SenseNumberTSS
		{
			get
			{
				string sNumber;
				if (this.OwnerHVO == 0 ||
					m_cache.GetClassOfObject(this.OwnerHVO) != LexSense.kclsidLexSense)
				{
					sNumber = Strings.ksZero;
				}
				else
				{
					ILexSense ls = LexSense.CreateFromDBObject(m_cache, this.OwnerHVO);
					sNumber = (ls as LexSense).SenseNumber;
				}
				return m_cache.MakeUserTss(sNumber);
			}
		}
	}
	#endregion // CmPicture
}
