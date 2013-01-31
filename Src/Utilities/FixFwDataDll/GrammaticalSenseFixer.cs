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
// File: GrammaticalSenseFixer.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{

	/// <summary>
	/// Fix up any entries that are found to contain MSA references which none of the senses have. Those references
	/// are simply removed.
	/// </summary>
	internal class GrammaticalSenseFixer : RtFixer
	{
		private Dictionary<string, HashSet<string>> senseToMSA = new Dictionary<string, HashSet<string>>();
		private Dictionary<string, HashSet<string>> entryToMSA = new Dictionary<string, HashSet<string>>();
		private Dictionary<string, HashSet<string>> entryToSense = new Dictionary<string, HashSet<string>>();

		internal override void InspectElement(XElement rt)
		{
			base.InspectElement(rt);
			var elementGuids = new HashSet<string>();
			var senseGuids = new HashSet<string>();
			string rtClass = rt.Attribute("class").Value;
			string rtGuid = rt.Attribute("guid").Value;

			switch (rtClass)
			{
				case "LexEntry":
					if (!entryToSense.ContainsKey(rtGuid))
					{
						var senses = rt.Element("Senses");
						if (senses != null)
						{
							foreach (var sense in senses.Descendants("objsur"))
							{
								if (sense.Name.LocalName == "objsur")
								{
									senseGuids.Add(sense.Attribute("guid").Value);
								}
							}
							entryToSense.Add(rtGuid, senseGuids);
						}
					}
					break;
				case "LexSense":
					if (!senseToMSA.ContainsKey(rtGuid))
					{
						senseToMSA.Add(rtGuid, elementGuids);
						AddElementGuids(rt, "MorphoSyntaxAnalysis", elementGuids);
					}
					break;
				default:
					break;
			}
		}

		private static void AddElementGuids(XElement rt, string elementName, HashSet<string> elementGuids)
		{
			var analyses = rt.Element(elementName);
			if (analyses == null)
				return;
			foreach (var xElement in analyses.Descendants("objsur"))
			{
				elementGuids.Add(xElement.Attribute("guid").Value);
			}
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids)
		{
			base.FinalFixerInitialization(owners, guids);
			//Go through the maps and find out if any references to MSAs ought to be dropped by the entry
			var finalEntryToMSA = new Dictionary<string, HashSet<string>>();
			foreach (var entry in entryToSense)
			{
				var actualMSAs = new HashSet<string>();
				finalEntryToMSA.Add(entry.Key, actualMSAs);
				foreach (var senseGuid in entry.Value)
				{
					actualMSAs.UnionWith(senseToMSA[senseGuid]);
				}
			}
			entryToMSA = finalEntryToMSA;
		}

		//Go through the list of MSA references in the file and remove those which do not occur in any sense
		//Why? Because the Chorus merge will flag a conflict for the users but end up placing both references
		//in the entry. This incorrectly causes parts of speech to be referenced in the Entry which no sense uses.
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var guid = rt.Attribute("guid");
			if (guid == null)
				return true;
			if (rt.Attribute("class").Value == "LexEntry")
			{
				var MSAs = rt.Element("MorphoSyntaxAnalyses");
				var rejects = new List<XNode>();
				if (MSAs != null)
				{
					foreach (var item in MSAs.Descendants("objsur"))
					{
						// the guid may not be in the dictionary, if the entry has NO senses. In that case,
						// all the MSAs will be rejected. If it's in the dictionary, the value is a list of msas that some sense
						// uses; if this one isn't used by a sense, it's a reject.
						HashSet<string> msasUsedBySomeSense;
						if (!entryToMSA.TryGetValue(guid.Value, out msasUsedBySomeSense) || !msasUsedBySomeSense.Contains(item.Attribute("guid").Value))
						{
							rejects.Add(item);
						}
					}
					foreach (var reject in rejects)
					{
						reject.Remove();
					}
				}
			}
			return true;
		}
	}
}
