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
		Dictionary<String, List<Guid>> m_Homographs = new Dictionary<String, List<Guid>>();
		Dictionary<Guid, String> m_LexEntryHomographNumbers = new Dictionary<Guid, String>();
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
				case "MoStemAllomorph":
					m_MoStemAllomorph.Add(guid, rt);
					break;
				default:
					break;
			}
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids)
		{
			base.FinalFixerInitialization(owners, guids); // Sets base class member variables

			List<Guid> guidsForHomograph = new List<Guid>();

			// Create a dictionary with the Form and MorphType guid as the key and a list of ownerguid's as the value.  This
			// will show us which LexEntries should have homograph numbers.  If the list of ownerguids has only one entry then
			// it's homograph number should be kept as zero. If the list of owerguids has more than one guid then the LexEntries
			// associated with those guids are homographs and will require unique homograph numbers.
			//
			foreach (var msa in m_MoStemAllomorph)
			{
				var rtElem = msa.Value;
				var rtForm = rtElem.Element("Form");
				if (rtForm == null)
					continue;
				var rtFormText = rtForm.Element("AUni").Value;

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
					var i = 1;
					foreach (var lexEntryGuid in lexEntryGuids)
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
			var guid = new Guid(rt.Attribute("guid").Value);
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? kUnknown : xaClass.Value;
			switch (className)
			{
				case "LexEntry":
					string homographNum;
					if (m_LexEntryHomographNumbers.TryGetValue(guid, out homographNum))
					{
						var homographElement = rt.Element("HomographNumber");
						Debug.Assert(homographElement != null, "HomographNumber element of LexEntry should never be null.");
						var homographAttribue = homographElement.Attribute("val");
						Debug.Assert(homographAttribue != null, "HomographNumber element val attribute should never be null.");
						homographAttribue.Value = homographNum;
					}
					break;
				default:
					break;
			}
			return true;
		}
	}
}
