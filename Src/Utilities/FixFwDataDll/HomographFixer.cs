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
// File: HomographFixer.cs
// Responsibility: RickM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// For projects using Send/Receive, two users on separate machines can create Lexical entries which have the same form
	/// and MorphType.  When merging is done these items will have different GUIDs and their Homograph numbers will both be set to
	/// zero '0'.   If these were created on the same machine they would show as having Homograph numbers 1 and 2.
	///
	/// This RtFixer is designed to correct this problem.  In the above example the Homograph numbers should end up being 1 and 2.
	///
	/// </summary>
	internal class HomographFixer : RtFixer
	{
		Dictionary<Guid, XElement> m_MoStemAllomorph = new Dictionary<Guid, XElement>();
		Dictionary<Guid, string> m_oldHomographNumbers = new Dictionary<Guid, string>();
		HashSet<Guid> m_firstAllomorphs = new HashSet<Guid>();
			Dictionary<String, List<Guid>> m_Homographs = new Dictionary<String, List<Guid>>();
		Dictionary<Guid, String> m_LexEntryHomographNumbers = new Dictionary<Guid, String>();
		const string kUnknown = "<unknown>";
		private string m_homographWs = null;

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
					// Enhance JohnT: can't affixes have homographs??
				case "MoStemAllomorph":
					m_MoStemAllomorph.Add(guid, rt);
					break;
				case "LexEntry":
					var homographElement = rt.Element("HomographNumber");
					string homographVal = "0";
					if (homographElement != null)
					{
						var homographAttribue = homographElement.Attribute("val");
						if (homographAttribue != null)
							homographVal = homographAttribue.Value;
					}
					m_oldHomographNumbers[guid] = homographVal;

					var lf = rt.Element("LexemeForm");
					if (lf != null)
					{
						var os = lf.Element("objsur");
						if (os != null)
						{
							var lfGuid = new Guid(os.Attribute("guid").Value);
							m_firstAllomorphs.Add(lfGuid);
						}
					}
					// Enhance JohnT: possibly we should consider AlternateForms, if no LexemeForm?
					break;
				case "LangProject":
					var homoWsElt = rt.Element("HomographWs");
					if (homoWsElt == null)
						break;
					var uniElt = homoWsElt.Element("Uni");
					if (uniElt == null)
						break;
					m_homographWs = uniElt.Value;
					break;
				default:
					break;
			}
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids,
			Dictionary<string, HashSet<string>> parentToOwnedObjsur, HashSet<string> rtElementsToDelete)
		{
			base.FinalFixerInitialization(owners, guids, parentToOwnedObjsur, rtElementsToDelete); // Sets base class member variables

			List<Guid> guidsForHomograph = new List<Guid>();

			// Create a dictionary with the Form and MorphType guid as the key and a list of ownerguid's as the value.  This
			// will show us which LexEntries should have homograph numbers.  If the list of ownerguids has only one entry then
			// it's homograph number should be zero. If the list of owerguids has more than one guid then the LexEntries
			// associated with those guids are homographs and will require unique homograph numbers.
			foreach (var morphKvp in m_MoStemAllomorph)
			{
				var rtElem = morphKvp.Value;
				var morphGuid = new Guid(rtElem.Attribute("guid").Value);
				// We don't assign homographs based on allomorphs.
				if (!m_firstAllomorphs.Contains(morphGuid))
					continue;
				var rtForm = rtElem.Element("Form");
				if (rtForm == null)
					continue;
				var rtFormAlt = rtForm.Elements("AUni").FirstOrDefault(form => form.Attribute("ws") != null && form.Attribute("ws").Value == m_homographWs);
				if (rtFormAlt == null)
					continue; // no data in relevant ws for homographs.
				var rtFormText = rtFormAlt.Value;
				if (rtFormText == null || rtFormText.Trim().Length == 0)
					continue; // entries with no lexeme form are not considered homographs.

				var rtMorphType = rtElem.Element("MorphType");
				if (rtMorphType == null)
					continue;
				var rtObjsur = rtMorphType.Element("objsur");
				if (rtObjsur == null)
					continue;
				var guid = rtObjsur.Attribute("guid").Value;

				var key = rtFormText + guid;

				var ownerguid = new Guid(rtElem.Attribute("ownerguid").Value);
				if (m_Homographs.TryGetValue(key, out guidsForHomograph))
				{
					guidsForHomograph.Add(ownerguid);
					m_Homographs.Remove(key);
					m_Homographs.Add(key, guidsForHomograph);
				}
				else
				{
					guidsForHomograph = new List<Guid>();
					guidsForHomograph.Add(ownerguid);
					m_Homographs.Add(key, guidsForHomograph);
				}
			}

			//Now assign a homograph number to each LexEntry that needs one.
			foreach (List<Guid> lexEntryGuids in m_Homographs.Values)
			{
				if (lexEntryGuids.Count > 1)
				{
					var orderedGuids = new Guid[lexEntryGuids.Count];
					var mustChange = new List<Guid>();
					foreach (var guid in lexEntryGuids)
					{
						// if it can keep its current HN, put it in orderedGuids at the correct position to give it that HN.
						// otherwise, remember that it must be put somewhere else.
						string oldHn;
						if (!m_oldHomographNumbers.TryGetValue(guid, out oldHn))
							oldHn = "0";
						int index;
						if (int.TryParse(oldHn, out index) && index > 0 && index <= orderedGuids.Length &&
							orderedGuids[index - 1] == Guid.Empty)
						{
							orderedGuids[index - 1] = guid;
						}
						else
						{
							mustChange.Add(guid);
						}
					}
					// The ones that have to change get slotted into whatever slots are still empty.
					// There must be enough slots because we made the array just big enough.
					foreach (var guid in mustChange)
					{
						for (int j = 0; j < orderedGuids.Length; j++)
						{
							if (orderedGuids[j] == Guid.Empty)
							{
								orderedGuids[j] = guid;
								break;
							}
						}
					}
					var i = 1;
					foreach (var lexEntryGuid in orderedGuids)
					{
						m_LexEntryHomographNumbers.Add(lexEntryGuid, i.ToString());
						i++;
					}
				}
			}

		}

		//For each LexEntry element, set the homograph number as determined in the FinalFixerInitialization method.
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var guidString = rt.Attribute("guid").Value;
			var guid = new Guid(guidString);
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? kUnknown : xaClass.Value;
			switch (className)
			{
				case "LexEntry":
					string homographNum;
					if (!m_LexEntryHomographNumbers.TryGetValue(guid, out homographNum))
						homographNum = "0"; // If it's not a homograph, its HN must be zero.
					var homographElement = rt.Element("HomographNumber");
					if (homographElement == null)
					{
						if (homographNum != "0") // no need to make a change if already implicitly zero.
						{
							rt.Add(new XElement("HomographNumber", new XAttribute("val", homographNum)));
							logger(guidString, DateTime.Now.ToShortDateString(), Strings.ksAdjustedHomograph);
						}
						break;
					}
					if (homographElement.Attribute("val") == null || homographElement.Attribute("val").Value != homographNum)
					{
						homographElement.SetAttributeValue("val", homographNum);
						logger(guidString, DateTime.Now.ToShortDateString(), Strings.ksAdjustedHomograph);
					}
					break;
				default:
					break;
			}
			return true;
		}
	}
}
