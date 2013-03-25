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
		private Dictionary<string, string> senseToMSA = new Dictionary<string, string>();
		private Dictionary<string, HashSet<string>> entryToMSA = new Dictionary<string, HashSet<string>>();
		private Dictionary<string, HashSet<string>> entryToSense = new Dictionary<string, HashSet<string>>();
		private HashSet<string> goodMsas = new HashSet<string>();
		private HashSet<string> allMsas = new HashSet<string>();

		internal override void InspectElement(XElement rt)
		{
			base.InspectElement(rt);
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
								senseGuids.Add(sense.Attribute("guid").Value);
						}
						entryToSense.Add(rtGuid, senseGuids); // even if it has no senses, we want to know about it.
						var msas = rt.Element("MorphoSyntaxAnalyses");
						var msaGuids = new HashSet<string>();
						if (msas != null)
						{
							foreach (var msa in msas.Descendants("objsur"))
							{
								msaGuids.Add(msa.Attribute("guid").Value);
							}
						}
						entryToMSA.Add(rtGuid, msaGuids);
					}
					break;
				case "LexSense":
					if (!senseToMSA.ContainsKey(rtGuid))
					{
						var msa = rt.Element("MorphoSyntaxAnalysis");
						if (msa == null)
							break;
						var objsur = msa.Element("objsur");
						if (objsur == null || objsur.Attribute("guid") == null)
							break;
						senseToMSA[rtGuid] = objsur.Attribute("guid").Value;
					}
					break;
				case "MoStemMsa":
				case "MoDerivAffMsa":
				case "MoInflAffMsa":
				case "MoUnclassifiedAffixMsa":
				case "MoDerivStepMsa":
					if (!allMsas.Contains(rtGuid))
					{
						allMsas.Add(rtGuid);
					}
					break;
				default:
					break;
			}
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids,
			Dictionary<string, HashSet<string>> parentToOwnedObjsur, HashSet<string> rtElementsToDelete)
		{
			base.FinalFixerInitialization(owners, guids, parentToOwnedObjsur, rtElementsToDelete);
			//Go through the maps and find out if any references to MSAs ought to be dropped by the entry
			foreach (var entry in entryToSense)
			{
				var keepMSAs = new HashSet<string>();
				foreach (var senseGuid in entry.Value)
				{
					string msaGuid;
					if (senseToMSA.TryGetValue(senseGuid, out msaGuid))
						keepMSAs.Add(msaGuid);
				}
				// If by any chance some of the senses are pointing at other entry's msas, that does NOT
				// make them keepers.
				keepMSAs.IntersectWith(entryToMSA[entry.Key]);
				goodMsas.UnionWith(keepMSAs);
			}

			foreach (var msa in allMsas)
			{
				if (IsRejectedMsa(msa))
				{
					MarkObjForDeletionAndDecendants(msa);
				}
			}
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
					var entryGuidString = guid.Value;
					foreach (var objsur in MSAs.Descendants("objsur"))
					{
						if (IsRejectedMsa(objsur))
						{
							rejects.Add(objsur);
						}
					}
					foreach (var reject in rejects)
					{
						logger(entryGuidString, DateTime.Now.ToShortDateString(), Strings.ksRemovedUnusedMsa);
						reject.Remove();
					}
				}
			}
			return true;
		}

		// Determine whether a particular objsur should be rejected.
		// This can also be used when examining a MoStemMsa to determine if it should be deleted.
		internal bool IsRejectedMsa(XElement elementWithMsaGuid)
		{
			return !goodMsas.Contains(elementWithMsaGuid.Attribute("guid").Value);
		}

		internal bool IsRejectedMsa(String guid)
		{
			return !goodMsas.Contains(guid);
		}
	}
}
