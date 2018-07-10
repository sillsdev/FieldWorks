// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExplorer.DictionaryConfiguration;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	internal static class TreeBarHandlerUtils
	{
		internal static void Tree_Duplicate(ICmPossibility original, int ownerHvo, LcmCache cache)
		{
			ICmPossibility node;
			if (ownerHvo == 0) // Possibility
				node = CreateDuplicate(original, original.Owner.Hvo, original.OwningFlid, original.Owner.OwnedObjects.Count(), cache);
			else // SubPossibility
				node = (ICmPossibility)cache.ServiceLocator.GetObject(ownerHvo);
			// Children
			if (original.SubPossibilitiesOS.Count > 0)
			{
				foreach (ICmPossibility poss in original.SubPossibilitiesOS)
				{
					if (!poss.ShortNameTSS.Text.Equals("???")) // Don't duplicate unnamed subitems
					{
						ICmPossibility newNode = CreateDuplicate(poss, node.Hvo, poss.OwningFlid, node.OwnedObjects.Count(), cache);
						// If cloned object should have children make the cloned object the parent, else creating a sibling so pass in same parent
						int nextOwnerHvo = (poss.SubPossibilitiesOS.Count > 0) ? newNode.Hvo : node.Hvo;
						Tree_Duplicate(poss, nextOwnerHvo, cache);
					}
				}
			}
		}

		private static ICmPossibility CreateDuplicate(ICmPossibility orig, int ownerHvo, int flid, int order, LcmCache cache)
		{
			ICmPossibility newObj = null;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(string.Format(DictionaryConfigurationStrings.ksUndoDuplicate, "Item"),
				string.Format(DictionaryConfigurationStrings.ksRedoDuplicate, "Item"), cache.ActionHandlerAccessor, () =>
				{
					const int itemFlid = 8008;
					ISilDataAccess sda = cache.MainCacheAccessor;
					int hvoNew = sda.MakeNewObject(orig.ClassID, ownerHvo, flid, order);
					newObj = (ICmPossibility)cache.ServiceLocator.GetObject(hvoNew);
					int ws = orig.Name.AvailableWritingSystemIds[0];
					newObj.Name.set_String(ws, orig.Name.UiString);
					newObj.Name.AnalysisDefaultWritingSystem = orig.Name.AnalysisDefaultWritingSystem;
					newObj.Name.VernacularDefaultWritingSystem = orig.Name.VernacularDefaultWritingSystem;
					if (flid == itemFlid) //Only the top node needs a name change
						newObj.Name.set_String(ws, ChangeName(newObj));
					if (!String.IsNullOrEmpty(orig.Abbreviation.UiString))
						newObj.Abbreviation.set_String(ws, orig.Abbreviation.UiString);
					if (!String.IsNullOrEmpty(orig.Description.UiString))
						newObj.Description.set_String(ws, orig.Description.UiString);
					newObj.StatusRA = orig.StatusRA;
					if(newObj.DiscussionOA != null)
						newObj.DiscussionOA = orig.DiscussionOA;
					newObj.ConfidenceRA = orig.ConfidenceRA;
					foreach (ICmPerson person in orig.ResearchersRC)
					{
						newObj.ResearchersRC.Add(person);
					}
					foreach (ICmPossibility poss in orig.RestrictionsRC)
					{
						newObj.RestrictionsRC.Add(poss);
					}
				});
			return newObj;
		}

		private static string ChangeName(ICmPossibility obj)
		{
			int max = 0;
			// Captures the name up to a (Copy) and captures the duplicate number if it exists
			Regex regex = new Regex(@"(.[a-z0-9A-Z\s]*) \(Copy\) \((.[0-9]*)\)$");
			Match match = regex.Match(obj.Name.UiString);
			//Only need the name up to the (Copy) so grab that if a duplicate else use original name
			string prefix = (match.Success) ? match.Groups[1].Value : obj.Name.UiString;
			//Find the highest duplicate number for that template
			foreach (ICmObject possibility in obj.OwningList.AllOwnedObjects)
			{
				match = regex.Match(possibility.ShortNameTSS.Text);
				if (match.Success && match.Groups[1].Value.Equals(prefix))
				{
					int temp = Convert.ToInt32(match.Groups[2].Value);
					if (temp > max)
						max = temp;
				}
			}
			max++; //Increment so the latest duplicate has the highest duplicate number
			return prefix + " (Copy) (" + max + ")";
		}
	}
}
