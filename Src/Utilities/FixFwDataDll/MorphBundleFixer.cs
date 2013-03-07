using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// This fixer handles cases where a WfiMorphBundle has a dangling MSA pointer.
	///		- if it has a sense that has an MSA, the MSA is fixed to point to it.
	///		- if it has a morph that belongs to a valid entry that has only one MSA, the MSA is fixed to point to it.
	/// Also, if it has a dangling Morph pointer,
	///		- if it has a sense that belongs to an entry with only a LexemeForm and no allomorphs,
	///			the Morph is fixed to point to that Lexeme form.
	/// If a good fix cannot be made the dangling objsur should be removed.
	/// (The usual removal in OriginalFixer has been suppressed to let this one try for a better fix first).
	/// Also correctly fixes an MSA pointer that is not originally dangling, but will become so
	/// because the MSA will be deleted for lack of any referring senses.
	/// </summary>
	class MorphBundleFixer : RtFixer
	{
		// Maps guids of LexSenses to guids of their MSAs.
		private Dictionary<string, string> m_senseToMsa = new Dictionary<string, string>();
		// Maps guids of LexEntries to the element's XML.
		private Dictionary<string, XElement> m_entrys = new Dictionary<string, XElement>();

		// A fixer which runs just before this one, some of whose data we need to do a good job.
		private GrammaticalSenseFixer m_senseFixer;

		public MorphBundleFixer(GrammaticalSenseFixer senseFixer)
		{
			m_senseFixer = senseFixer;
		}
		/// <summary>
		/// First pass: gather information: remember all the lex entries, and the senses which have msas.
		/// Enhance JohnT: should we do something to verify that the msa refs in the senses are not also dangling?
		/// </summary>
		/// <param name="rt"></param>
		internal override void InspectElement(XElement rt)
		{
			var guid = rt.Attribute("guid").Value;
			var xaClass = rt.Attribute("class");
			if (xaClass == null)
				return; // weird, but we can't use it.
			var className =xaClass.Value;
			switch (className)
			{
				case "LexSense":
					var msaGuid = ChildSurrogateGuid(rt, "MorphoSyntaxAnalysis");
					if (msaGuid == null)
						break; // we can't use this sense to help fix MSAs
					m_senseToMsa[guid] = msaGuid;
					break;
				case "LexEntry":
					m_entrys[guid] = rt;
					break;
			}
		}

		/// <summary>
		/// If parent has a child with the specified name, which has an objsur child with a guid attribute, return
		/// the value of that attribute. Otherwise, null.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="childName"></param>
		/// <returns></returns>
		internal string ChildSurrogateGuid(XElement parent, string childName)
		{
			var child = parent.Element(childName);
			if (child == null)
				return null;
			return SurrogateGuid(child);
		}

		// If parent has an <objsur> child with a guid attribute, return its value. Otherwise return null.
		internal string SurrogateGuid(XElement parent)
		{
			var objsur = parent.Element("objsur");
			if (objsur == null)
				return null;
			XAttribute destAttr = objsur.Attribute("guid");
			if (destAttr == null)
				return null;
			return destAttr.Value;
		}

		/// <summary>
		/// This is the main routine that does actual repair on morph bundles.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <returns>true (always, currently) to indicate that the bundle should survive.</returns>
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var guidString = rt.Attribute("guid").Value;
			var xaClass = rt.Attribute("class");
			if (xaClass == null)
				return true; // we can't fix it, anyway.
			var className = xaClass.Value;
			switch (className)
			{
				case "WfiMorphBundle":
					FixMsa(rt, logger, guidString);
					FixMorph(rt, logger, guidString);
					break;
			}
			return true;
		}

		/// <summary>
		/// Make any required fixes to the MSA child element.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <param name="guidString"></param>
		private void FixMsa(XElement rt, FwDataFixer.ErrorLogger logger, string guidString)
		{
			var destGuidString = ChildSurrogateGuid(rt, "Msa");
			if (destGuidString == null)
				return;
			var objsur = rt.Element("Msa").Element("objsur"); // parts of path must exist, we got destGuidString
			if (m_guids.Contains(new Guid(destGuidString)) && !m_senseFixer.IsRejectedMsa(objsur))
				return; // msa is not dangling.
			var senseGuid = ChildSurrogateGuid(rt, "Sense");
			string senseMsaGuid;
			if (senseGuid != null && m_senseToMsa.TryGetValue(senseGuid, out senseMsaGuid))
			{
				// We can repair it to match the sense's msa, an ideal repair.
				objsur.SetAttributeValue("guid", senseMsaGuid);
				logger(guidString, DateTime.Now.ToShortDateString(),
					String.Format(Strings.ksRepairingMorphBundleFromSense, guidString));
				return;
			}
			// Next see if we can link to a unique msa via our morph child.
			var morphGuidString = ChildSurrogateGuid(rt, "Morph");
			Guid entryGuid;
			XElement entryElt;
			if (morphGuidString != null
				&& m_owners.TryGetValue(new Guid(morphGuidString), out entryGuid)
				&& m_entrys.TryGetValue(entryGuid.ToString(), out entryElt))
			{
				var soleEntryMsa = GetSoleMsaGuid(entryElt);
				if (soleEntryMsa != null)
				{
					objsur.SetAttributeValue("guid", soleEntryMsa);
					logger(guidString, DateTime.Now.ToShortDateString(),
						String.Format(Strings.ksRepairingMorphBundleFromEntry, guidString));
					return;
				}
			}
			// If we can't fix it nicely, just clobber it.
			logger(guidString, DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingDanglingMsa,
				destGuidString, guidString));
			objsur.Remove();
		}

		/// <summary>
		/// Deal with problems of the Morph child, if any.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <param name="guidString"></param>
		private void FixMorph(XElement rt, FwDataFixer.ErrorLogger logger, string guidString)
		{
			var destGuidString = ChildSurrogateGuid(rt, "Morph");
			if (destGuidString == null)
				return;
			var objsur = rt.Element("Morph").Element("objsur"); // parts of path must exist, we got destGuidString
			if (m_guids.Contains(new Guid(destGuidString)))
				return; // all is well, not dangling.
			var senseGuid = ChildSurrogateGuid(rt, "Sense");
			Guid entryGuid;
			XElement entryElt;
			if (senseGuid != null && m_owners.TryGetValue(new Guid(senseGuid), out entryGuid) && m_entrys.TryGetValue(entryGuid.ToString(), out entryElt))
			{
				var soleForm = GetSoleForm(entryElt);
				if (soleForm != null)
				{
					// We can repair it to use the only form of the right entry, a very promising repair.
					objsur.SetAttributeValue("guid", soleForm);
					logger(guidString, DateTime.Now.ToShortDateString(),
						String.Format(Strings.ksRepairingBundleFormFromEntry, guidString));
					return;
				}
			}
			// If we can't fix it nicely, just clobber it.
			logger(guidString, DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingDanglingMorph,
				destGuidString, guidString));
			objsur.Remove();
		}

		/// <summary>
		/// Get the Lexeme Form guid, if one is present and there are no alternate forms; otherwise, return null.
		/// Enhance JohnT: arguably we could return a single AlternateForm if there is no LF, but this is pathological
		/// and unlikely to be useful.
		/// </summary>
		/// <param name="entryElt"></param>
		/// <returns></returns>
		private string GetSoleForm(XElement entryElt)
		{
			var lfGuid = ChildSurrogateGuid(entryElt, "LexemeForm");
			if (lfGuid == null)
				return null;
			var afElt = entryElt.Element("AlternateForms");
			if (afElt == null)
				return lfGuid;
			if (afElt.Elements("objsur").Count() == 0)
				return lfGuid;
			return null;
		}

		/// <summary>
		/// Get the guid of the one and only msa of rt (which is an Entry)
		/// (or return null if it does not have exactly one MSA that will survive fixup)
		/// </summary>
		/// <param name="rt"></param>
		/// <returns></returns>
		string GetSoleMsaGuid(XElement rt)
		{
			var msasElt = rt.Element("MorphoSyntaxAnalyses");
			if (msasElt == null)
				return null;
			// The entry might have msas that are going to be deleted, but have not yet been.
			// Don't consider those in evaluating whether it has exactly one.
			var msas = msasElt.Elements("objsur").Where(msa => !m_senseFixer.IsRejectedMsa(msa)).ToList();
			if (msas.Count != 1)
				return null; // can't use it to fix MSAs of bundles reliably, since it is not unique
			var objsur = msas[0];
			XAttribute destAttr = objsur.Attribute("guid");
			if (destAttr == null)
				return null; // weird, but none of our business
			return destAttr.Value;
		}
	}
}
