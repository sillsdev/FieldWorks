// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SequenceFixer.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class contains code to fix situations where merges have resulted in sequences that
	/// are empty and shouldn't be. In normal operation, FDO takes care of these situations
	/// automatically, but in a Send/Receive situation FDO might not 'know' that one user deleted
	/// a sequence element while another user deleted the other with the result on merging that
	/// now both are gone. Usually the required fix is to delete the parent that holds the sequence.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SequenceFixer : RtFixer
	{
		Dictionary<Guid, XElement> m_charts = new Dictionary<Guid, XElement>();
		Dictionary<Guid, XElement> m_rows = new Dictionary<Guid, XElement>();
		Dictionary<Guid, List<Guid>> m_rowsToDelete = new Dictionary<Guid, List<Guid>>();
		Dictionary<Guid, List<Guid>> m_cellRefsToDelete = new Dictionary<Guid, List<Guid>>();
		Dictionary<Guid, List<Guid>> m_phContextRefsToDelete = new Dictionary<Guid, List<Guid>>();
		List<Guid> m_emptyClauseMarkers = new List<Guid>();
		List<Guid> m_emptySequenceContexts = new List<Guid>();
		const string kUnknown = "<unknown>";

		internal override void InspectElement(XElement rt)
		{
			// If this is a class I'm interested in, get the information I need.
			var guid = new Guid(rt.Attribute("guid").Value);
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? kUnknown : xaClass.Value;
			if (className == kUnknown)
				return;
			switch (className)
			{
				case "DsConstChart":
					// check for empty Rows sequence (if empty, skip)
					if(HoldsEmptySequence("Rows", rt))
						return; // don't bother, FLEx can handle this.
					m_charts.Add(guid, rt);
					break;
				case "ConstChartRow":
					m_rows.Add(guid, rt);
					break;
				case "ConstChartClauseMarker":
					// check for empty DependentClause sequence (if not empty, skip)
					if (!HoldsEmptySequence("DependentClauses", rt))
						return;
					m_emptyClauseMarkers.Add(guid);
					break;
				case "PhSequenceContext":
					// check for empty Members sequence (if not empty, skip)
					if (!HoldsEmptySequence("Members", rt))
						return;
					m_emptySequenceContexts.Add(guid);
					var owner = SafelyGetOwnerGuid(guid);
					UpdateDanglingReferenceDictionary(m_phContextRefsToDelete, owner, guid);
					break;
				case "PhSegRuleRHS":
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Determines whether the sequence held by this XElement object is empty or not.
		/// N.B. At this point, there must only be one sequence held by this object.
		/// </summary>
		/// <param name="propertyName"> </param>
		/// <param name="xeObject"></param>
		/// <returns></returns>
		private bool HoldsEmptySequence(string propertyName, XElement xeObject)
		{
			var xeProperty = xeObject.Element(propertyName);
			return !(xeProperty.Descendants("objsur").Any());
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids)
		{
			base.FinalFixerInitialization(owners, guids); // Sets base class member variables

			// Determine who needs to die
			foreach (var kvp in m_rows)
			{
				var guid = kvp.Key;
				var rtElem = kvp.Value;
				var cellList = rtElem.Descendants("objsur").ToList();
				var ccellsRemaining = cellList.Count;
				var refsToDeleteThisRow = new List<Guid>();
				foreach (var xeCell in cellList)
				{
					var cellGuid = GetObjsurGuid(xeCell);
					if (!m_emptyClauseMarkers.Contains(cellGuid))
						continue;
					refsToDeleteThisRow.Add(cellGuid);
					ccellsRemaining--;
				}
				if (ccellsRemaining == 0)
				{
					// This row won't have any cells left!
					var chartGuid = m_owners[guid];
					UpdateDanglingReferenceDictionary(m_rowsToDelete, chartGuid, guid);
				}
				else
					foreach (var cellGuid in refsToDeleteThisRow)
						UpdateDanglingReferenceDictionary(m_cellRefsToDelete, guid, cellGuid);
			}
			foreach (var contextGuid in m_emptySequenceContexts)
			{
				var owner = SafelyGetOwnerGuid(contextGuid);
				UpdateDanglingReferenceDictionary(m_phContextRefsToDelete, owner, contextGuid);
			}
		}

		/// <summary>
		/// Safely update a dictionary of dangling references to be deleted.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="owner"></param>
		/// <param name="ownee"></param>
		private void UpdateDanglingReferenceDictionary(Dictionary<Guid, List<Guid>> dict, Guid owner, Guid ownee)
		{
			List<Guid> rowsList;
			if (dict.TryGetValue(owner, out rowsList))
			{
				rowsList.Add(ownee);
				dict.Remove(owner);
				dict.Add(owner, rowsList);
			}
			else
			{
				dict.Add(owner, new List<Guid> { ownee });
			}
		}

		private static Guid GetObjsurGuid(XElement xeCell)
		{
			return new Guid(xeCell.Attribute("guid").Value);
		}

		/// <summary>
		/// Do any fixes to this particular root element here.
		/// Return true if we are done fixing this element and can write it out.
		/// Return false if we need to delete this root element.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="errorLogger"></param>
		/// <returns></returns>
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger errorLogger)
		{
			var guid = new Guid(rt.Attribute("guid").Value);
			var guidOwner = SafelyGetOwnerGuid(guid);
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? kUnknown : xaClass.Value;
			if (guidOwner == Guid.Empty || className == kUnknown)
				return true; // these should have been fixed by another fixer!
			List<Guid> danglingRefList; // used in 2 cases below.
			switch (className)
			{
				case "DsConstChart":
					// Check all chart rows for deletion and remove dangling references.
					if (!m_rowsToDelete.TryGetValue(guid, out danglingRefList))
						return true; // this chart has no rows to delete.

					rt.Descendants("objsur").Where(
						objsur => danglingRefList.Contains(GetObjsurGuid(objsur))).Remove();
					break;
				case "ConstChartRow":
					// Step 1: if row is set for deletion, remove, report, and return.
					if (!m_rowsToDelete.TryGetValue(guidOwner, out danglingRefList))
						return true; // this chart has no rows to delete.
					if (danglingRefList.Contains(guid))
					{
						ReportOwnerOfEmptySequence(guid, guidOwner, className, errorLogger);
						return false; // delete this rt element
					}

					// Step 2: otherwise check for cell refs to delete and remove reference.
					if (!m_cellRefsToDelete.TryGetValue(guid, out danglingRefList))
						return true;
					rt.Descendants("objsur").Where(
						objsur => danglingRefList.Contains(GetObjsurGuid(objsur))).Remove();
					break;
				case "ConstChartClauseMarker":
					// If marker guid is in m_emptyClauseMarkers remove and report.
					if (!m_emptyClauseMarkers.Contains(guid))
						return true;
					ReportOwnerOfEmptySequence(guid, guidOwner, className, errorLogger);
					return false; // delete this rt element
				case "PhSegRuleRHS":
					// Check for sequence context refs to delete and remove reference.
					if (!m_phContextRefsToDelete.TryGetValue(guid, out danglingRefList))
						return true;
					rt.Descendants("objsur").Where(
						objsur => danglingRefList.Contains(GetObjsurGuid(objsur))).Remove();
					break;
				case "PhSequenceContext":
					// Remove a PhSequenceContext from the correct side of its owning rule,
					// if it has no Members.
					if (!m_emptySequenceContexts.Contains(guid))
						return true;
					ReportOwnerOfEmptySequence(guid, guidOwner, className, errorLogger);
					return false; // delete this rt element
				default:
					break;
			}
			return true;
		}

		private static void ReportOwnerOfEmptySequence(Guid guid, Guid guidOwner, string className, FwDataFixer.ErrorLogger errorLogger)
		{
			// Example: if guid, className, and rt belong to a "ConstChartRow" that has no cells and guidOwner belongs
			//			to a DsConstChart, then this will remove the ConstChartRow from the file and report it.
			//			The objsur reference to the row gets removed elsewhere.
			errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingOwnerOfEmptySequence,
				guid, className, guidOwner));
		}

		/// <summary>
		/// Gets the guid of the owner. If not found, returns Guid.Empty.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		private Guid SafelyGetOwnerGuid(Guid guid)
		{
			Guid guidOwner;
			if (!m_owners.TryGetValue(guid, out guidOwner))
				guidOwner = Guid.Empty;
			return guidOwner;
		}
	}
}
