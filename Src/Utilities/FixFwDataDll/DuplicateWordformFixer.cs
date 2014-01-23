// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DuplicateWordformFixer.cs
// Responsibility: ThomsonJ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// If two users doing Send/Receive each creates wordforms with the same form, kill one of them.
	/// </summary>
	internal class DuplicateWordformFixer : RtFixer
	{
		/// <summary>
		/// Map from string (ws>form & ws>form...) to (guid, Analyses), for all wordforms already encountered and not to be deleted.
		/// Analyses is the string representation of the Analyses element of the wordform, or null if it has none.
		///
		/// (We can't just keep the XElement, because a different XElement will be used in the second pass through the file. Also it
		/// may be that keeping the Analyses XElement around would prevent garbage collection of all the XElements for all the wordforms
		/// in the project, which is likely to run us out of memory.)
		///
		/// The ws's in the key are in alphabetical order, for all forms in the wordform that has the guid.
		/// (We don't really put spaces around the ampersand, but C# is confused by things that look like XML entities in these comments.)
		/// A list of pairs for the key would be cleaner, but use quite a lot more memory.
		/// > and & are safe as separators because they can't occur in an XML attribute value.
		/// </summary>
		private Dictionary<string, WordformInfo> m_formToGuid = new Dictionary<string, WordformInfo>();

		/// <summary>
		/// For each wordform we need to modify, stores the changed Analyses string.
		/// (This is a duplicate of the string in the corresponding WordformInfo, but we need those even
		/// for unchanged wordforms. Relatively few will be modified in a typical merge, and we gain some
		/// efficiency by being able to get the modified analyses directly for those ones.)
		/// </summary>
		private Dictionary<string, string> m_modifiedWordforms = new Dictionary<string, string>();

		/// <summary>
		/// Map from guid to be deleted to the guid that replaces it.
		/// </summary>
		private Dictionary<string, string> m_guidsToDelete = new Dictionary<string, string>();

		/// <summary>
		/// This is called in a preliminary pass before anything is changed. In it we figure out which
		/// wordforms need to be deleted and find the corresponding ones to keep. For the ones we will keep
		/// which correspond to ones being deleted, we accumulate a modified list of owned analyses, the union
		/// of those for all the wordforms with the same set of forms.
		///
		/// The preliminary pass is needed so that in the second pass, where the change is made, we know
		/// what GUID substitutions to make in Segments and an analysis owners, even if the element
		/// that needs to be modified comes before the deleted wordform. Also, when we come to a surviving
		/// wordform that needs analyses added to it, we have accumulated all the analyses that need to
		/// be added.
		/// </summary>
		/// <param name="rt"></param>
		internal override void InspectElement(XElement rt)
		{
			var className = rt.Attribute("class").Value;
			if (className == "WfiWordform")
			{
				var key = GetWordformKey(rt);
				WordformInfo previousWordform;
				var guid = rt.Attribute("guid").Value;
				var analyses = rt.Element("Analyses");
				if (!m_formToGuid.TryGetValue(key, out previousWordform))
				{
					// Never seen this key before, just remember the critical information from it.
					m_formToGuid.Add(key, new WordformInfo(guid, (analyses == null ? null : analyses.ToString())));
				}
				else
				{
					// We will keep the previous wordform and (eventually) delete this one
					m_guidsToDelete[guid] = previousWordform.Guid;
					if (analyses != null)
					{
						// We need to transfer the analyses from this wordform to the one that will replace it.
						if (previousWordform.Analyses == null)
						{
							// the wordform in this set which we plan to keep has no analyses, but this one does.
							// Tempting to change our minds and keep this one, but we may already have decided to merge
							// another empty one with the previous one!
							// Just insert the whole analysis string here as the thing to put in the survivor.
							var analysis = analyses.ToString();
							m_formToGuid[key] = new WordformInfo(previousWordform.Guid, analysis);
							m_modifiedWordforms[previousWordform.Guid] = analysis;
						}
						else
						{
							// We will merge the analyses lists
							var mergedAnalyses = XElement.Parse(previousWordform.Analyses);
							foreach (var link in analyses.Elements())
							{
								mergedAnalyses.Add(link);
							}
							var analysis = mergedAnalyses.ToString();
							m_formToGuid[key] = new WordformInfo(previousWordform.Guid, analysis);
							m_modifiedWordforms[previousWordform.Guid] = analysis;
						}
					}
				}
			}
		}

		private static string GetWordformKey(XElement rt)
		{
			var xElement = rt.Element("Form");
			if (xElement == null)
				return ""; // probably will never happen??
			var alternatives = xElement.Elements("AUni").Select(form => form.Attribute("ws").Value + ">" + form.Value).ToList();
			alternatives.Sort(); // Really any sort will do, just has to be the same for all.
			return alternatives.Aggregate((first, second) => first + "&" + second);
		}

		/// <summary>
		/// This is called in the second pass that actually changes the objects. It does the following:
		/// - Deletes wordforms that have been merged (by returning false for them).
		/// - Adds analyses of deleted wordforms to the Analysis of the survivor that replaces them.
		/// - Fixes the ownerguid of those transferred analyses.
		/// - Fixes any segments which refer to deleted wordforms to refer instead to the replacement.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <returns></returns>
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var className = rt.Attribute("class").Value;
			if (className == "WfiWordform")
			{
				// Arrange for it to be deleted if that's what we decided to do.
				string replacement;
				var guid = rt.Attribute("guid").Value;
				if (m_guidsToDelete.TryGetValue(guid, out replacement))
				{
					logger(guid, DateTime.Now.ToShortDateString(),
						String.Format(Strings.ksDuplicateWordform, guid, GetWordformKey(rt), replacement));
					return false; // discard this element
				}
				// Modify it if we need to add to its analyses.
				string modifiedAnalyses;
				if (m_modifiedWordforms.TryGetValue(guid, out modifiedAnalyses))
				{
					var analyses = rt.Element("Analyses");
					if (analyses != null)
						analyses.Remove();
					var mergedAnalyses = XElement.Parse(modifiedAnalyses);
					rt.Add(mergedAnalyses);
				}
			}
			else if (className == "Segment")
			{
				// Fix refs to deleted wordforms.
				var analyses = rt.Element("Analyses");
				if (analyses != null)
				{
					foreach (var objsur in analyses.Elements())
					{
						var oldGuid = objsur.Attribute("guid").Value;
						string newGuid;
						if (m_guidsToDelete.TryGetValue(oldGuid, out newGuid))
						{
							objsur.Attribute("guid").SetValue(newGuid);
						}
					}
				}
			}
			else if (className == "WfiAnalysis")
			{
				// Fix ownerguid for moved analyses.
				var ownerAttr = rt.Attribute("ownerguid");
				if (ownerAttr != null)
				{
					string newGuid;
					if (m_guidsToDelete.TryGetValue(ownerAttr.Value, out newGuid))
					{
						ownerAttr.SetValue(newGuid);
					}
				}
			}
			return true; // keep the current element.
		}

		private class WordformInfo : Tuple<string, string>
		{
			public WordformInfo(string guid, string analysis)
				: base(guid, analysis)
			{
			}

			public string Guid { get { return Item1; } }
			public string Analyses { get { return Item2; } }
		}
	}
}
