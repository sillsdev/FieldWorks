// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000010.cs
// Responsibility: RandyR
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Xml;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000009 to 7000010.
	/// </summary>
	///
	/// <remarks>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000010 : IDataMigration
	{
		private const string ktempXficInstanceOfGuid = "6b04dc11-a516-4cfb-89a7-abb1838df3c0";

		private static readonly ElementTags s_tagsCmIndirect = new ElementTags("CmIndirectAnnotation");
		private static readonly ElementTags s_tagsAppliesTo = new ElementTags("AppliesTo");
		private static readonly ElementTags s_tagsText = new ElementTags("Text");
		private static readonly ElementTags s_tagsStText = new ElementTags("StText");
		private static readonly ElementTags s_tagsParagraphs = new ElementTags("Paragraphs");
		private static readonly ElementTags s_tagsStTxtPara = new ElementTags("StTxtPara");
		private static readonly ElementTags s_tagsContents = new ElementTags("Contents");
		private static readonly ElementTags s_tagsStr = new ElementTags("Str");
		private static readonly ElementTags s_tagsUni = new ElementTags("Uni");
		private static readonly ElementTags s_tagsInstanceOf = new ElementTags("InstanceOf");
		private static readonly ElementTags s_tagsCompDetails = new ElementTags("CompDetails");
		private static readonly ElementTags s_tagsCmBaseAnnotation = new ElementTags("CmBaseAnnotation");
		private static readonly ElementTags s_tagsBeginObject = new ElementTags("BeginObject");
		private static readonly ElementTags s_tagsComment = new ElementTags("Comment");
		private static readonly ElementTags s_tagsCmAnnotation = new ElementTags("CmAnnotation");
		private static readonly ElementTags s_tagsAnnotationType = new ElementTags("AnnotationType");
		private static readonly ElementTags s_tagsAStr = new ElementTags("<AStr ", "</AStr>");
		private static readonly ElementTags s_tagsRun = new ElementTags("<Run ", "</Run>");
		private static readonly ElementTags s_tagsBeginOffset = new ElementTags("<BeginOffset ", ">");
		private static readonly ElementTags s_tagsEndOffset = new ElementTags("<EndOffset ", ">");
		private static readonly ElementTags s_tagsParseIsCurrent = new ElementTags("<ParseIsCurrent ", ">");
		private static readonly ElementTags s_tagsObjsur = new ElementTags("<objsur ", ">");

		private static readonly ElementTags s_tagsBegAnalysisIndex = new ElementTags("<BeginAnalysisIndex ", ">");
		private static readonly ElementTags s_tagsEndAnalysisIndex = new ElementTags("<EndAnalysisIndex ", ">");

		private static readonly byte[] s_endXmlTag = Encoding.UTF8.GetBytes(">");

		private static readonly byte[] s_mergeAfterAttr = Encoding.UTF8.GetBytes(" mergeAfter=");
		private static readonly byte[] s_mergeBeforeAttr = Encoding.UTF8.GetBytes(" mergeBefore=");
		private static readonly byte[] s_guidAttr = Encoding.UTF8.GetBytes(" guid=");
		private static readonly byte[] s_classAttr = Encoding.UTF8.GetBytes(" class=");
		private static readonly byte[] s_wsAttr = Encoding.UTF8.GetBytes(" ws=");
		private static readonly byte[] s_valAttr = Encoding.UTF8.GetBytes(" val=");

		private static readonly byte[] s_ltlt = Encoding.UTF8.GetBytes("&lt;&lt;");

		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Migrate data from 7000009 to 7000010.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository dtoRepos)
		{
			DataMigrationServices.CheckVersionNumber(dtoRepos, 7000009);

			// The old objects to be removed go into 'goners'.
			// In this case, it is the annotation defn type objects.
			var goners = new List<DomainObjectDTO>((int) (dtoRepos.Count*0.80));
			DomainObjectDTO dtoGoner;
			if (dtoRepos.TryGetValue(DataMigrationServices.kSegmentAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			if (dtoRepos.TryGetValue(DataMigrationServices.kTwficAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			if (dtoRepos.TryGetValue(DataMigrationServices.kPficAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			if (dtoRepos.TryGetValue(DataMigrationServices.kFreeTransAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			if (dtoRepos.TryGetValue(DataMigrationServices.kLitTransAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			if (dtoRepos.TryGetValue(DataMigrationServices.kTextTagAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);
			// Some old projects somehow didn't get this annotation type, so make all of these
			// dependent on actually existing.  (See LT-11416.)
			if (dtoRepos.TryGetValue(DataMigrationServices.kDiscourseChartComponentAnnDefnGuid, out dtoGoner))
				goners.Add(dtoGoner);

			// Don't delete it, since it owns two other type defns which are not part of this DM.
			//goners.Add(domainObjectDtoRepository.GetDTO(CmAnnotationDefnTags.kguidAnnNote.ToString()));

			// Gather up CmBaseAnnotations of interest.
			// Typically (if there are enough to be a possible problem, anyway) most of them are xfics.
			// Therefore the dictionaries which will get one entry per xfic are initialized to be large
			// enough to hold an item for each annotation.
			// This wastes a little memory, but repeatedly resizing the dictionaries can waste a LOT more,
			// as chunks of address space become unuseable. It also saves time.
			int annotationCount = dtoRepos.AllInstancesSansSubclasses("CmBaseAnnotation").Count();
			var paraToOldSegments = new Dictionary<string, SortedList<int, byte[]>>(annotationCount/5);
			// Gather old segments that have duplicate BeginOffsets for a given paragraph.
			var paraToDuplicateOldSegments = new Dictionary<string, SortedList<int, List<byte[]>>>();
			var paraToOldXfics = new Dictionary<string, SortedList<int, byte[]>>(/*annotationCount/24*/);
			// Gather old xfics that have duplicate BeginOffsets for a given paragraph.
			var paraToDuplicateOldXfics = new Dictionary<string, SortedList<int, List<byte[]>>>();
			// Key is old ann guid. Value is old ann XElement.
			var oldCCAs = new Dictionary<string, byte[]>(annotationCount/5);
			// Key is old CCA ann guid. Value is the new CCA guid.
			var ccaGuidMap = new Dictionary<Guid, Guid>(annotationCount);
			// Keep track of xfics with inbound refs from Text tags and/or discourse,
			// which is used to help select among duplicates.
			var xficHasTextOrDiscourseBackReference = new Dictionary<string, int>(annotationCount/5);
			CollectBaseAnnotations(dtoRepos, goners,
				oldCCAs, ccaGuidMap, xficHasTextOrDiscourseBackReference,
				paraToDuplicateOldSegments, paraToOldSegments,
				paraToDuplicateOldXfics, paraToOldXfics);

			// Gather up CmIndirectAnnotations of interest.
			var freeTrans = new Dictionary<string, List<byte[]>>(annotationCount/3);
			var litTrans = new Dictionary<string, List<byte[]>>();
			var notes = new Dictionary<string, List<byte[]>>();
			var oldCCRows = new List<byte[]>();
			var oldTextTags = new List<byte[]>();
			CollectIndirectAnnotations(dtoRepos, goners,
				oldCCAs, ccaGuidMap, xficHasTextOrDiscourseBackReference,
				oldCCRows,
				oldTextTags,
				freeTrans, litTrans,
				notes);

			// Delete (right now, not later) any CCRs and any CCAs the CCRs reference,
			// which are not referenced by any DsConstChart (old Rows ref col prop).
			// Also, remove any immediately deleted CCRs and CCAs from the 'goners'
			// delayed deletion list, and the 'oldCCAs' and 'oldCCRows' lists.
			// We do this deletion/removal, so the duplicate selection
			// code that is called next won't have to worry about such clutter.
			DeleteImmediatelyAllUnlovedCCRs(dtoRepos, goners, xficHasTextOrDiscourseBackReference,
				oldCCRows, oldCCAs, ccaGuidMap);

			// Sort out duplicates.
			SelectFromDuplicates(dtoRepos,
				xficHasTextOrDiscourseBackReference,
				paraToOldXfics, paraToDuplicateOldXfics);
			paraToDuplicateOldSegments.Clear();
			paraToDuplicateOldXfics.Clear();
			paraToDuplicateOldSegments = null;
			paraToDuplicateOldXfics = null;

			// Look for any xfics that have multiple CCA backrefs.
			// If we find any, look through backrefs of CCAs and backrefs of
			// xfic.BeginObj to see which, if any, CCA is not in a chart BasedOn the right text.
			SortOutMultipleXficBackrefs(dtoRepos, xficHasTextOrDiscourseBackReference,
				oldCCAs, ccaGuidMap, oldCCRows, goners);

			// Feed 'halfBakedCcwgItems' into ProcessParagraphs (and thence, into the seg processor).
			// Then, the new kvp 'Values' can be finished (seg & twfic indices).
			// kvp Key is the orginal twfic/pfic element.
			// Kvp Value is a new ConstChartWordGroup that uses the xfic.
			var halfBakedCcwgItems = ProcessDiscourseData(
				dtoRepos,
				paraToOldSegments,
				oldCCRows,
				oldCCAs, ccaGuidMap);
			oldCCRows.Clear();
			oldCCRows = null;

			// One reason for running out memory is adding to dtoRepos while in
			// ProcessParagraphs().  (See LT-11241.)  So we'll delete all the goners that aren't
			// referenced in ProcessParagraphs().  Fortunately, it's fairly simple to find out
			// which those are.
			var neededGoners = new HashSet<string>();
			foreach (var kvp in halfBakedCcwgItems)
			{
				var refs = GetAppliesToObjsurGuids(kvp.Key);
				if (refs == null || refs.Count == 0)
					continue;
				var guid = refs[0];
				neededGoners.Add(guid);
			}
			DeleteUnneededGoners(dtoRepos, goners, neededGoners);
			if (neededGoners.Count == 0)
				goners.Clear();
			neededGoners.Clear();

			// Process each paragraph.
			ProcessParagraphs(dtoRepos,
				oldCCAs,
				halfBakedCcwgItems,
				paraToOldSegments,
				paraToOldXfics,
				ccaGuidMap,
				oldTextTags,
				freeTrans, litTrans, notes);
			oldCCAs.Clear();
			halfBakedCcwgItems.Clear();
			paraToOldSegments.Clear();
			paraToOldXfics.Clear();
			ccaGuidMap.Clear();
			oldTextTags.Clear();
			freeTrans.Clear();
			litTrans.Clear();
			notes.Clear();
			oldCCAs = null;
			halfBakedCcwgItems = null;
			paraToOldSegments = null;
			paraToOldXfics = null;
			ccaGuidMap = null;
			oldTextTags = null;
			freeTrans = null;
			litTrans = null;
			notes = null;

			if (goners.Count > 0)
			{
				DeleteUnneededGoners(dtoRepos, goners, neededGoners);
				goners.Clear();
			}
			// There may be zombies left from earlier DMs,
			// since they didn't do recursive deletions on a then zombie's owned objects.
			// This left additional zombies.
			// This won't affect regular users that come from FW 6.0,
			// as the original issue was fixed.
			// Nevertheless, given the drastic nature of this DM,
			// it's best to clean out any unexpected inbound refs to stuff that is going away,
			// rather than let FW crash, when trying to reconstitute zombies/dangling references.
			DataMigrationServices.Delint(dtoRepos);

			// LT-11593 Apparently somehow it is possible to have a wfic that doesn't belong
			// to a text that is referred to by a CCWG! One db had 9 chart cells that referred
			// to this type of wfic that has no other backreferences. At this point in the migration,
			// the offending wfic will have been deleted. Now we will delete the CCWG because it
			// will have Begin/EndAnalysisIndex = some guid instead of an integer!
			DeleteCcwgWithGuidIndex(dtoRepos);

			DataMigrationServices.IncrementVersionNumber(dtoRepos);
		}

		private static void DeleteCcwgWithGuidIndex(IDomainObjectDTORepository dtoRepos)
		{
			const int kLongIntLength = 10;
			var goners = new List<DomainObjectDTO>();
			var ccwgs = dtoRepos.AllInstancesSansSubclasses("ConstChartWordGroup");
			foreach (var dto in ccwgs)
			{
				int dummy;
				var ebBegIndex = new ElementBounds(dto.XmlBytes, s_tagsBegAnalysisIndex);
				var ebEndIndex = new ElementBounds(dto.XmlBytes, s_tagsEndAnalysisIndex);
				if (!(ebBegIndex.IsValid && ebEndIndex.IsValid))
					continue; // Hopefully this isn't a problem!
				var sBegAnalysisIndexValue = ebBegIndex.GetAttributeValue(s_valAttr);
				var sEndAnalysisIndexValue = ebEndIndex.GetAttributeValue(s_valAttr);
				if (!Int32.TryParse(sBegAnalysisIndexValue, out dummy) ||
					!Int32.TryParse(sEndAnalysisIndexValue, out dummy))
				{
					// Found something that needs fixing!
					// There's likely a guid instead of an integer, due to an unresolved
					// reference where the wfic it used to refer to has been deleted
					// for some (valid) reason.
					goners.Add(dto);
					continue;
				}
			}
			// need to remove goners from dtoRepos and make sure that empty rows get deleted too.

			if (goners.Count > 0)
			{
				var neededGoners = new Set<string>();
				DeleteUnneededGoners(dtoRepos, goners, neededGoners);
				goners.Clear();
			}
		}

		private static void DeleteUnneededGoners(IDomainObjectDTORepository dtoRepos,
			IEnumerable<DomainObjectDTO> goners, ICollection<string> neededGoners)
		{
			// We have run out of memory during this data migration on large projects.  (See
			// FWR-3849.) One possible reason is that we can make 15000+ copies of LangProject
			// as we slowly squeeze out the owning links to Annotations.  So let's collect them
			// all up at once, and remove them with a single change to the LangProject dto.
			var dtoLangProject = dtoRepos.AllInstancesSansSubclasses("LangProject").FirstOrDefault();
			if (dtoLangProject != null)
			{
				var xeLangProject = XElement.Parse(dtoLangProject.Xml);
				var cAnn = xeLangProject.Descendants("objsur").
					Where(t => (t.Attribute("t") != null && t.Attribute("t").Value == "o")).Count();
				if (cAnn > 0)
				{
					var gonersInLangProject = new List<DomainObjectDTO>(cAnn);
					foreach (var goner in goners)
					{
						DomainObjectDTO gonerActual;
						if (!dtoRepos.TryGetValue(goner.Guid, out gonerActual))
							continue; // Not in repos.
						if (neededGoners.Contains(goner.Guid))
							continue; // still need this in the repository.
						var ownerDto = dtoRepos.GetOwningDTO(goner);
						if (ownerDto != null && ownerDto.Guid == dtoLangProject.Guid)
							gonersInLangProject.Add(gonerActual);
					}
					if (gonersInLangProject.Count > 0)
					{
						DataMigrationServices.RemoveMultipleIncludingOwnedObjects(dtoRepos,
							gonersInLangProject, dtoLangProject);
						gonersInLangProject.Clear();
						// We don't try removing those from goners because that operation may
						// involve a linear lookup in the list to find the object to remove.
						// And RemoveIncludingOwnedObjects() checks whether the goner still
						// exists before doing anything else.
					}
				}
			}
			// Remove old stuff.
			foreach (var goner in goners)
			{
				if (neededGoners.Contains(goner.Guid))
					continue;
				DataMigrationServices.RemoveIncludingOwnedObjects(dtoRepos, goner, true);
			}
		}

		private static void SortOutMultipleXficBackrefs(IDomainObjectDTORepository dtoRepos,
			Dictionary<string, int> xficHasTextOrDiscourseBackReference,
			Dictionary<string, byte[]> oldCcas,
			Dictionary<Guid, Guid> ccaGuidMap,
			IEnumerable<byte[]> oldCcrs,
			List<DomainObjectDTO> goners)
		{
			// Look for any xfics that have multiple CCA backrefs.
			// If we find any, look through backrefs of CCAs and backrefs of xfic.BeginObj
			// to see which if any, CCA is not in a chart BasedOn the right text.
			// And delete it!
			var allBadRefsToXfics = new List<string>();
			foreach (var xficKvp in xficHasTextOrDiscourseBackReference)
			{
				// Key is old xfic Guid
				// Value is number of backrefs
				if (xficKvp.Value <= 1)
					continue; // Only one backref; not interesting.

				// Now it gets tricky. Two backrefs is okay as long as only one is from a CCA
				// and only one is from a TextTag.
				var xficGuid = xficKvp.Key;
				var refsToXfic = new List<string>();
				foreach (var ccaKvp in oldCcas)
				{
					// Key is old CCA Guid
					var ccaGuid = ccaKvp.Key;
					// Value is Annotation XElement
					var ccaElement = ccaKvp.Value;
					// Make sure we are dealing with a CCA
					if (!AnnotationIsCca(ccaElement))
						continue;

					// Check to see if this one "AppliesTo" the xfic under surveillance.
					// If so, add it to a temporary collection of references to this xfic
					var refsFromAppliesTo = GetAppliesToObjsurGuids(ccaElement);
					if (refsFromAppliesTo != null && refsFromAppliesTo.Count > 0)
					{
						refsToXfic.AddRange(from r in refsFromAppliesTo
											select r
											into guid where guid == xficGuid select ccaGuid);
					}
				}
				if (refsToXfic.Count < 2)
					continue; // Xfic must have a TextTag reference

				// If we get here, we need to delete something!
				// Only delete the one that refers to a different text than its chart.
				DistinguishBetweenCcasLinkedToSameXfic(dtoRepos, refsToXfic, xficGuid, oldCcrs);
				allBadRefsToXfics.AddRange(refsToXfic);
			}
			// Do the delete and backref count reduction here outside of 'foreach'.
			foreach (var oneXficRef in allBadRefsToXfics)
				DeleteImmediatelyBadCca(dtoRepos, oneXficRef, goners, ccaGuidMap, oldCcas,
										xficHasTextOrDiscourseBackReference);
		}

		private static void DistinguishBetweenCcasLinkedToSameXfic(
			IDomainObjectDTORepository dtoRepos,
			List<string> refsToXfic,
			string xficGuid,
			IEnumerable<byte[]> oldCcrs)
		{
			// We need to distinguish between the Ccas that reference xficGuid correctly
			// and the ones that are spurious. Delete the spurious ones.
			var textGuid = GetStTextGuidFromXfic(dtoRepos, xficGuid);
			// Now look through 'refsToXfic' to see which CCA is in a chart that is
			// BasedOn 'textGuid'. Remove it from the list of refs to get deleted.
			string goodCcaGuid = null;
			foreach (var ccaGuid in refsToXfic)
			{
				var ccrGuid = GetCcrGuidThatRefsCca(oldCcrs, ccaGuid);
				if (ccrGuid == null)
					continue;
				var chartElement = GetChartElementThatRefsCcr(ccrGuid, dtoRepos);
				if (chartElement == null)
					continue;
				var chartBasedOnGuid = GetBasedOnGuid(chartElement);
				if (chartBasedOnGuid != textGuid)
					continue;
				goodCcaGuid = ccaGuid;
				break;
			}
			if (!String.IsNullOrEmpty(goodCcaGuid))
				refsToXfic.Remove(goodCcaGuid);
		}

		private static string GetBasedOnGuid(XElement chartElement)
		{
			return GetGuid(chartElement.Element("DsConstChart").Element("BasedOn").Element("objsur"));
		}

		private static XElement GetChartElementThatRefsCcr(string ccrGuid,
			IDomainObjectDTORepository dtoRepos)
		{
			var allChartDtos = dtoRepos.AllInstancesSansSubclasses("DsConstChart");
			XElement chartElement = null;
			foreach (var chartDto in allChartDtos)
			{
				var tempElement = XElement.Parse(chartDto.Xml);
				var rowsElement = tempElement.Element("DsConstChart").Element("Rows");
				if (rowsElement == null)
					continue;
				var rowObjSurs = rowsElement.Elements("objsur");
				if (!FoundCcrInRows(rowObjSurs, ccrGuid))
					continue;
				chartElement = tempElement;
				break;
			}
			return chartElement;
		}

		private static bool FoundCcrInRows(IEnumerable<XElement> rows, string ccrGuid)
		{
			return rows.Any(rowElement => ccrGuid == GetGuid(rowElement));
		}

		private static string GetCcrGuidThatRefsCca(IEnumerable<byte[]> oldCcrs, string ccaGuid)
		{
			// Look through 'oldCcrs' for 'ccaGuid'
			foreach (var ccrElement in oldCcrs)
			{
				foreach (var guid in GetAppliesToObjsurGuids(ccrElement))
				{
					if (guid == ccaGuid)
						return GetGuid(ccrElement);
				}
			}
			return null;
		}

		private static string GetStTextGuidFromXfic(IDomainObjectDTORepository dtoRepos, string xficGuid)
		{
			var dtoXfic = dtoRepos.GetDTO(xficGuid.ToLowerInvariant());
			var paraGuid = GetBeginObjectGuid(dtoXfic.XmlBytes);
			return dtoRepos.GetOwningDTO(dtoRepos.GetDTO(paraGuid.ToLowerInvariant())).Guid;
		}

		private static bool AnnotationIsCca(byte[] xmlBytes)
		{
			var classVal = GetClassValue(xmlBytes);
			if (classVal != "CmIndirectAnnotation")
				return false;
			var surrGuid = GetAnnotationTypeGuid(xmlBytes);
			return surrGuid == DataMigrationServices.kConstituentChartAnnotationAnnDefnGuid;
		}


		private static List<KeyValuePair<byte[], XElement>> ProcessDiscourseData(IDomainObjectDTORepository dtoRepos,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments,
			IEnumerable<byte[]> oldCCRs,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap) // Key is old CCA guid. Value is new CCA guid.
		{
			// Make a mapping between old CCA anns and new ConstChartWordGroup objects (both XElements),
			// which are the ones where the old ann had twfics in AppliesTo.
			// These will be fed into the code that converts the twfics,
			// so as to have access to the right conversion context
			// (i.e., be able to get at the Segment and start/end indices for the twfics).
			var halfBakedCcwgItems = new List<KeyValuePair<byte[], XElement>>();

			// Map between old CCR ann guid and new CCR guid, so code in here can keep track of them.
			// Key is the original CCR guid. Value is the new CCR guid.
			//var ccrRowsGuidMap = new Dictionary<string, string>();
			// Key is the new CCR guid. Value is the guid of its new owning chart.
			var ccrOwnerGuidMap = new Dictionary<Guid, Guid>();

			// Migrate the DsConstChart(s).
			foreach (var chartDto in dtoRepos.AllInstancesSansSubclasses("DsConstChart"))
			{
				var chartElement = XElement.Parse(chartDto.Xml);

				foreach (var objsurElement in chartElement.Element("DsConstChart").Elements("Rows").Elements("objsur"))
				{
					// Change to owning.
					objsurElement.Attribute("t").Value = "o";

					// Change the guid.
					var guidAttr = objsurElement.Attribute("guid");
					var newCCRGuid = ccaGuidMap[new Guid(guidAttr.Value)];
					// Remember the owner guid (Chart) for the new CCR guid.
					// Key is the new guid for the new CCR.
					// Value is the owning chart.
					ccrOwnerGuidMap.Add(newCCRGuid, new Guid(chartDto.Guid));
					guidAttr.Value = newCCRGuid.ToString().ToLowerInvariant();
				}

				// Tell dto repos of the modification of the chart.
				chartDto.Xml = chartElement.ToString();
				dtoRepos.Update(chartDto);
			}

			// Migrate the CCR and CCA annotations.
			foreach (var oldCCR in oldCCRs)
			{
				// Collect up the inner class-level elements.
				var cmAnnotationBounds = new ElementBounds(oldCCR, s_tagsCmAnnotation);
				// May be null.
				var oldCompDetailsBounds = new ElementBounds(oldCCR, s_tagsCompDetails,
					cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
				var oldTextBounds = new ElementBounds(oldCCR, s_tagsText,
					cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
				// oldCommentBounds is unused. Hopefully by design?!
				//var oldCommentBounds = new ElementBounds(oldCCR, s_tagsComment,
				//    cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
				// May be null, or at least have no 'objsur' elements.
				var refsFromAppliesTo = GetAppliesToObjsurGuids(oldCCR);

				// Try to make a Notes element. It may be null.
				XElement notesElement = null;
				if (oldTextBounds.IsValid)
				{
					// Get the StText dto and element.
					var stTextGuid = GetObjsurGuid(oldCCR, oldTextBounds.BeginTagOffset, oldTextBounds.EndTagOffset);
					if (!String.IsNullOrEmpty(stTextGuid))
					{
						var stTextDto = dtoRepos.GetDTO(stTextGuid);
						var stTextBounds = new ElementBounds(stTextDto.XmlBytes, s_tagsStText);
						var paragraphsBounds = new ElementBounds(stTextDto.XmlBytes, s_tagsParagraphs,
							stTextBounds.BeginTagOffset, stTextBounds.EndTagOffset);
						// See if stTextElement has any paras (StTxtPara)
						if (paragraphsBounds.IsValid)
						{
							// Get the first para.
							var firstParaGuid = GetObjsurGuid(stTextDto.XmlBytes,
								paragraphsBounds.BeginTagOffset, paragraphsBounds.EndTagOffset);
							if (!String.IsNullOrEmpty(firstParaGuid))
							{
								var firstParaDto = dtoRepos.GetDTO(firstParaGuid);
								var stTxtParaBounds = new ElementBounds(firstParaDto.XmlBytes,
									s_tagsStTxtPara);
								var contentsBounds = new ElementBounds(firstParaDto.XmlBytes,
									s_tagsContents,
									stTxtParaBounds.BeginTagOffset, stTxtParaBounds.EndTagOffset);
								var strBounds = new ElementBounds(firstParaDto.XmlBytes, s_tagsStr,
									contentsBounds.BeginTagOffset, contentsBounds.EndTagOffset);
								// See if it has any Contents.
								if (strBounds.IsValid)
								{
									// Move the Contents into a new Notes element.
									notesElement = new XElement("Notes",
										XElement.Parse(Encoding.UTF8.GetString(firstParaDto.XmlBytes,
											strBounds.BeginTagOffset, strBounds.Length)));
								}
							}
						}
					}
				}

				// Deal with 'ClauseType' property.
				var clauseTypeElement = new XElement("ClauseType",
					new XAttribute("val", "0"));
				// Deal with 'ClauseType' property.
				var endParagraphElement = new XElement("EndParagraph",
					new XAttribute("val", "False"));
				// Deal with 'ClauseType' property.
				var endSentenceElement = new XElement("EndSentence",
					new XAttribute("val", "False"));
				// Deal with 'ClauseType' property.
				var startDependentClauseGroupElement = new XElement("StartDependentClauseGroup",
					new XAttribute("val", "False"));
				// Deal with 'ClauseType' property.
				var endDependentClauseGroupElement = new XElement("EndDependentClauseGroup",
					new XAttribute("val", "False"));
				// See if some optional stuff in 'oldCompDetailsElement' will change it.
				var uniBounds = new ElementBounds(oldCCR, s_tagsUni,
					oldCompDetailsBounds.BeginTagOffset, oldCompDetailsBounds.EndTagOffset);
				if (uniBounds.IsValid)
				{
					// Turn its pseudo-xml string content into a real XElement.
					// It's string won't have angle brackets, so turn the entities into '<' and '>'
					// <CompDetails><Uni>&lt;ccinfo endSent="true"/&gt;</Uni></CompDetails>
					var ichMin = uniBounds.BeginTagOffset + s_tagsUni.BeginTag.Length;
					var ichLim = uniBounds.EndTagOffset;
					var cch = ichLim - ichMin;
					var details = Encoding.UTF8.GetString(oldCCR, ichMin, cch);
					if (details.Contains("&"))
						details = XmlUtils.DecodeXmlAttribute(details);
					var compDetailsElement = XElement.Parse(details);
					var optionalAttr = compDetailsElement.Attribute("dependent");
					var foundOverride = false;
					if (optionalAttr != null && optionalAttr.Value.ToLower() == "true")
					{
						clauseTypeElement.Attribute("val").Value = "1";
						foundOverride = true;
					}
					optionalAttr = compDetailsElement.Attribute("song");
					if (!foundOverride && optionalAttr != null && optionalAttr.Value.ToLower() == "true")
					{
						clauseTypeElement.Attribute("val").Value = "2";
						foundOverride = true;
					}
					optionalAttr = compDetailsElement.Attribute("speech");
					if (!foundOverride && optionalAttr != null && optionalAttr.Value.ToLower() == "true")
					{
						clauseTypeElement.Attribute("val").Value = "3";
					}
					// No more ClauseType attrs.
					// Move on to the other four optional boolean attrs.
					optionalAttr = compDetailsElement.Attribute("endSent");
					if (optionalAttr != null && optionalAttr.Value.ToLower() == "true")
						endSentenceElement.Attribute("val").Value = "True";

					optionalAttr = compDetailsElement.Attribute("endPara");
					if (optionalAttr != null && optionalAttr.Value.ToLower() == "true")
						endParagraphElement.Attribute("val").Value = "True";

					optionalAttr = compDetailsElement.Attribute("firstDep");
					if (optionalAttr != null && optionalAttr.Value.ToLower() == "true")
						startDependentClauseGroupElement.Attribute("val").Value = "True";

					optionalAttr = compDetailsElement.Attribute("endDep");
					if (optionalAttr != null && optionalAttr.Value.ToLower() == "true")
						endDependentClauseGroupElement.Attribute("val").Value = "True";
				}

				// Required 'Label' prop, which comes from the old 'Comment'
				// May be null, or at least have no 'objsur' elements.
				var enAltBounds = GetEnglishCommentBounds(oldCCR,
					cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
				var enAlt = Encoding.UTF8.GetString(oldCCR, enAltBounds.BeginTagOffset, enAltBounds.Length);
				var enAltElement = XElement.Parse(enAlt);
				// Convert it to "Str" element with no "ws" attr.
				enAltElement.Name = "Str";
				enAltElement.Attribute("ws").Remove();

				// Create new ConstChartRow class (XElement & DTO).
				var oldGuid = GetGuid(oldCCR);
				var newGuid = ccaGuidMap[new Guid(oldGuid)];
				var owningGuid = ccrOwnerGuidMap[newGuid];
				ccrOwnerGuidMap.Remove(newGuid);
				const string className = "ConstChartRow";
				var newCCRElement = new XElement("rt",
					new XAttribute("class", className),
					new XAttribute("guid", newGuid),
					new XAttribute("ownerguid", owningGuid),
					new XElement("CmObject"),
					new XElement(className,
						notesElement, // May be null.
						clauseTypeElement,
						endParagraphElement,
						endSentenceElement,
						startDependentClauseGroupElement,
						endDependentClauseGroupElement,
						new XElement("Label", enAltElement),
						AddCells(dtoRepos,
							paraToOldSegments,
							halfBakedCcwgItems, refsFromAppliesTo, oldCCAs, ccaGuidMap, newGuid.ToString().ToLowerInvariant())));

				// Add DTO to repos.
				dtoRepos.Add(new DomainObjectDTO(newGuid.ToString().ToLowerInvariant(), className, newCCRElement.ToString()));
			}

			return halfBakedCcwgItems;
		}

		private static string GetObjsurGuid(byte[] xmlBytes, int ichMin, int ichLim)
		{
			var objsurBounds = new ElementBounds(xmlBytes, s_tagsObjsur, ichMin, ichLim);
			if (!objsurBounds.IsValid)
				return null;
			return GetGuid(xmlBytes, objsurBounds.BeginTagOffset, objsurBounds.EndTagOffset);
		}

		private static XElement AddCells(IDomainObjectDTORepository dtoRepos,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments,
			ICollection<KeyValuePair<byte[], XElement>> halfBakedCcwgItems,
			List<string> refsFromAppliesTo,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap, // Key is old CCA guid. Value is new CCA guid.
			string owningCCRGuid)
		{
			if (refsFromAppliesTo == null || refsFromAppliesTo.Count == 0)
			{
				// Row with no AppliesTo content.
				return null;
			}

			return new XElement("Cells",
				ConvertCCAsAndAddCCRObjSurElements(
					dtoRepos,
					paraToOldSegments,
					halfBakedCcwgItems,
					oldCCAs, ccaGuidMap,
					refsFromAppliesTo,
					owningCCRGuid));
		}

		private static IEnumerable<XElement> ConvertCCAsAndAddCCRObjSurElements(
			IDomainObjectDTORepository dtoRepos,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments,
			ICollection<KeyValuePair<byte[], XElement>> halfBakedCcwgItems,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap,
			List<string> objsurElementGuids,
			string owningCCRGuid)
		{
			// 'retval' will be put into the new CCR of the caller as owning objsur elements.
			var retval = new List<XElement>(objsurElementGuids.Count());

			// Decide which class to convert the old CCA to:
			//	1. CmBaseAnnotations -> ConstChartTag
			//	2. CmIndirectAnnotation
			//		A. 1 (or more) CCRs in AppliesTo-> ConstChartClauseMarker
			//		B. 1 (only) CCA in AppliesTo -> ConstChartMovedTextMarker
			//		C. null AppliesTo -> ConstChartWordGroup
			//		D. 1 (or more) twfics (pfics?) -> ConstChartWordGroup
			const string kChangeMe = "CHANGEME";
			foreach (var oldCCAGuid in objsurElementGuids)
			{
				var guidOldCCA = new Guid(oldCCAGuid);
				// Earlier 'SortOutMultipleXficBackRefs()' may have left a dangling reference.
				// If so, skip it.
				//XElement oldAnnElement;
				byte[] oldAnnElement;
				if (!oldCCAs.TryGetValue(oldCCAGuid, out oldAnnElement))
					continue;
				//var oldAnnElement = oldCCAs[oldCCAGuid];
				// Leave it in, so we can get at its XElement, whenever needed.
				//oldCCAs.Remove(oldCCAGuid);
				var guidNew = ccaGuidMap[guidOldCCA];
				var newGuid = guidNew.ToString().ToLowerInvariant();
				var newClassName = kChangeMe;
				// Collect up the inner class-level elements.
				var cmAnnotationBounds = new ElementBounds(oldAnnElement, s_tagsCmAnnotation);
				if (!cmAnnotationBounds.IsValid)
					continue;
				// Fix FWR-2139 crash migrating because of missing InstanceOf
				// Skip chart annotation with no column reference.
				var guidInstanceOf = GetInstanceOfGuid(oldAnnElement);
				if (String.IsNullOrEmpty(guidInstanceOf))
					continue;
				var mergesAfterElement = new XElement("MergesAfter", new XAttribute("val", "False"));
				var mergesBeforeElement = new XElement("MergesBefore", new XAttribute("val", "False"));
				// May be null.
				var compDetailsBounds = new ElementBounds(oldAnnElement, s_tagsCompDetails,
					cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
				if (compDetailsBounds.IsValid)
				{
					var uniBounds = new ElementBounds(oldAnnElement, s_tagsUni,
						compDetailsBounds.BeginTagOffset, compDetailsBounds.EndTagOffset);
					if (uniBounds.IsValid)
					{
						// See if some optional stuff in 'oldCompDetailsElement' will change MergesAfter or MergesBefore.
						var mergeAfter = GetAttributeValue(oldAnnElement, s_mergeAfterAttr,
							uniBounds.BeginTagOffset, uniBounds.EndTagOffset);
						if (mergeAfter == "true")
							mergesAfterElement.Attribute("val").Value = "True";
						var mergeBefore = GetAttributeValue(oldAnnElement, s_mergeBeforeAttr,
							uniBounds.BeginTagOffset, uniBounds.EndTagOffset);
						if (mergeBefore == "true")
							mergesBeforeElement.Attribute("val").Value = "True";
					}
				}
				// Reset the Name and add other elements really soon now,
				// depending on which subclass of ConstituentChartCellPart is used.
				var newSpecificClassElement = new XElement(newClassName);
				var newClassAttr = new XAttribute("class", newClassName);
				var newCCAElement = new XElement("rt",
					newClassAttr,
					new XAttribute("guid", newGuid),
					new XAttribute("ownerguid", owningCCRGuid),
					new XElement("CmObject"),
					new XElement("ConstituentChartCellPart",
						new XElement("Column",
							new XElement("objsur",
								new XAttribute("t", "r"),
								new XAttribute("guid", guidInstanceOf))),
						mergesAfterElement,
						mergesBeforeElement),
					newSpecificClassElement);

				var classValue = GetClassValue(oldAnnElement);
				switch (classValue)
				{
					default:
						throw new InvalidOperationException("Unrecognized annotation class used as CCA.");
					case "CmBaseAnnotation":
						// #1.
						newClassName = "ConstChartTag";
						newSpecificClassElement.Name = newClassName;
						newClassAttr.Value = newClassName;
						// Tag is atomic ref.
						var guidBeginObject = GetBeginObjectGuid(oldAnnElement);
						if (!String.IsNullOrEmpty(guidBeginObject))
						{
							newSpecificClassElement.Add(
								new XElement("Tag",
									new XElement("objsur",
										new XAttribute("t", "r"),
										new XAttribute("guid", guidBeginObject))));
						}
						break;
					case "CmIndirectAnnotation":
						// #2.
						// Get the optional 'AppliesTo' element.
						var refsFromAppliesTo = GetAppliesToObjsurGuids(oldAnnElement);
						if (refsFromAppliesTo == null || refsFromAppliesTo.Count == 0)
						{
							// 2.C
							newClassName = "ConstChartWordGroup";
							newSpecificClassElement.Name = newClassName;
							newClassAttr.Value = newClassName;
							// BeginSegment & EndSegment are to be null, so leave them out altogether.
							// BeginAnalysisIndex & EndAnalysisIndex are both -1.
							// Note: This is actually wrong; this should be a ConstChartTag with no Tag
							//    But it gets fixed in DM7000013.
							newSpecificClassElement.Add(new XElement("BeginAnalysisIndex", new XAttribute("val", "-1")));
							newSpecificClassElement.Add(new XElement("EndAnalysisIndex", new XAttribute("val", "-1")));
						}
						else
						{
							// Get the class of the first (or only) objsur.
							var currentRefGuid = refsFromAppliesTo[0];
							var currentInnerTarget = oldCCAs[refsFromAppliesTo[0]];
							switch (GetAnnotationTypeGuid(currentInnerTarget))
							{
								default:
									throw new InvalidOperationException("Unrecognized annotation type for CCA.");
								case DataMigrationServices.kConstituentChartRowAnnDefnGuid:
									// One, or more, CCRs.
									// 2.A
									newClassName = "ConstChartClauseMarker";
									newSpecificClassElement.Name = newClassName;
									newClassAttr.Value = newClassName;
									var dependentClausesElement = new XElement("DependentClauses");
									newSpecificClassElement.Add(dependentClausesElement);
									foreach (var guid in refsFromAppliesTo)
									{
										// DependentClauses is ref. seq. prop.
										dependentClausesElement.Add(
											DataMigrationServices.CreateReferenceObjSurElement(
												ccaGuidMap[new Guid(guid)].ToString().ToLowerInvariant()));
									}
									break;
								case DataMigrationServices.kConstituentChartAnnotationAnnDefnGuid:
									// Single CCA.
									// 2.B
									newClassName = "ConstChartMovedTextMarker";
									newSpecificClassElement.Name = newClassName;
									newClassAttr.Value = newClassName;
									// WordGroup - Get new guid from cca guid map using old CCA guid.
									newSpecificClassElement.Add(new XElement("WordGroup",
										DataMigrationServices.CreateReferenceObjSurElement(ccaGuidMap[new Guid(currentRefGuid)].ToString().ToLowerInvariant())));
									// Preposed - Boolean.
									// The data migration for the Preposed boolean is simple.
									// If the old Marker's Comment property contains a string "<<" then it's true,
									// false otherwise.
									newSpecificClassElement.Add(new XElement("Preposed",
										new XAttribute("val",
											GetPreposedBooleanFromComment(oldAnnElement,
												cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset))));
									break;
								case DataMigrationServices.kPficAnnDefnGuid: // Fall through.
								case DataMigrationServices.kTwficAnnDefnGuid:
									// One, or more, twfics/pfics
									// These all go into halfBakedCcwgItems,
									// and will be finished when Segments and xfics are finished.

									// NB: While there may be multiple xfics,
									// only the first and last are stored in the two indices.
									var firstXficGuid = currentRefGuid;
									var lastXficGuid = refsFromAppliesTo[refsFromAppliesTo.Count - 1];
									var firstXficInnerAnnElement = XElement.Parse(dtoRepos.GetDTO(firstXficGuid).Xml).Element("CmBaseAnnotation");
									// Gotta make sure the xfics and segments are in the same paragraph.
									var paraGuid = GetGuid(firstXficInnerAnnElement.Element("BeginObject").Element("objsur"));
									var beginOffsetElement = firstXficInnerAnnElement.Element("BeginOffset");
									var firstXfixBeginOffset = beginOffsetElement == null ? 0 : int.Parse(beginOffsetElement.Attribute("val").Value);
									var newSegmentGuid = kChangeMe;

									try
									{
										foreach (var kvp in paraToOldSegments[paraGuid].TakeWhile(kvp => kvp.Key <= firstXfixBeginOffset))
										{
											// Found the right segment, so get its new segment guid.
											newSegmentGuid = ccaGuidMap[new Guid(GetGuid(kvp.Value))].ToString().ToLowerInvariant();
										}
									}
									catch (KeyNotFoundException)
									{
										// Upon finding an orphaned chart cell with an invalid paragraph, skip it.
										continue;
									}
									if (newSegmentGuid == kChangeMe)
									{
										// We might have some segments (better check), but there are xfics that aren't
										// covered by a segment, so try to recover the broken data, as much as possible.
										newSegmentGuid = AddExtraInitialSegment(paraGuid, ccaGuidMap, paraToOldSegments);
									}

									halfBakedCcwgItems.Add(new KeyValuePair<byte[], XElement>(oldAnnElement, newCCAElement));
									newClassName = "ConstChartWordGroup";
									newSpecificClassElement.Name = newClassName;
									newClassAttr.Value = newClassName;
									newSpecificClassElement.Add(new XElement("BeginSegment",
										DataMigrationServices.CreateReferenceObjSurElement(newSegmentGuid)));
									newSpecificClassElement.Add(new XElement("EndSegment",
										DataMigrationServices.CreateReferenceObjSurElement(newSegmentGuid)));
									// For now, just store the guid of the first xfic.
									// It's the wrong data type, but xml won't care,
									// and, they will get changed later on.
									newSpecificClassElement.Add(new XElement("BeginAnalysisIndex",
										new XAttribute("val", firstXficGuid)));
									// For now, just store the guid of the last xfic.
									// It's the wrong data type, but xml won't care,
									// and, they will get changed later on.
									newSpecificClassElement.Add(new XElement("EndAnalysisIndex",
										new XAttribute("val", lastXficGuid)));
									break;
							}
						}
						break;
				}

				// Create new owning objSur Element.
				retval.Add(DataMigrationServices.CreateOwningObjSurElement(newGuid));

				// Add newly converted CCA to repos.
				dtoRepos.Add(new DomainObjectDTO(newGuid, newClassName, newCCAElement.ToString()));
			}

			return retval;
		}

		private static string AddExtraInitialSegment(string paraGuid, IDictionary<Guid, Guid> ccaGuidMap,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments)
		{
			Debug.Assert(paraToOldSegments[paraGuid].Count > 0, "No segments? How did this happen?");
			// Need to create a new old segment XElement (not dto), to try and and keep old data.
			var guidBrandNewSeg = Guid.NewGuid();
			var brandNewSegGuid = guidBrandNewSeg.ToString().ToLower();
			ccaGuidMap.Add(guidBrandNewSeg, guidBrandNewSeg);
			var segsForCurrentPara = paraToOldSegments[paraGuid];
			var tmp = new XElement("rt",
				new XAttribute("guid", brandNewSegGuid),
				new XElement("CmObject"),
					new XElement("CmBaseAnnotation",
						new XElement("BeginOffset", new XAttribute("val", 0)),
						new XElement("EndOffset", new XAttribute("val", paraToOldSegments[paraGuid].First().Key))));
			segsForCurrentPara.Add(0, Encoding.UTF8.GetBytes(tmp.ToString()));
			return brandNewSegGuid;
		}

		private static string GetPreposedBooleanFromComment(byte[] xmlBytes, int ichMin, int ichLim)
		{
			var astrBounds = GetEnglishCommentBounds(xmlBytes, ichMin, ichLim);
			// If you can't find an English comment, take the first one you can find.
			if (astrBounds == null || !astrBounds.IsValid)
			{
				var commentBounds = new ElementBounds(xmlBytes, s_tagsComment, ichMin, ichLim);
				if (commentBounds.IsValid)
				{
					astrBounds = new ElementBounds(xmlBytes, s_tagsAStr,
						commentBounds.BeginTagOffset, commentBounds.EndTagOffset);
				}
			}
			if (astrBounds != null && astrBounds.IsValid)
			{
				var ichEndTag = xmlBytes.IndexOfSubArray(s_endXmlTag, astrBounds.BeginTagOffset);
				var ichLtlt = xmlBytes.IndexOfSubArray(s_ltlt, ichEndTag);
				if (ichLtlt > 0 && ichLtlt < astrBounds.EndTagOffset)
					return "True";
			}
			return "False";
		}

		private static ElementBounds GetEnglishCommentBounds(byte[] xmlBytes, int ichMin, int ichLim)
		{
			var commentBounds = new ElementBounds(xmlBytes, s_tagsComment, ichMin, ichLim);
			if (!commentBounds.IsValid)
				return null;
			var astrBounds = new ElementBounds(xmlBytes, s_tagsAStr,
				commentBounds.BeginTagOffset, commentBounds.EndTagOffset);
			while (astrBounds.IsValid)
			{
				var ws = astrBounds.GetAttributeValue(s_wsAttr);
				if (ws == "en")
					return astrBounds;
				astrBounds.Reset(astrBounds.EndTagOffset, commentBounds.EndTagOffset);
			}
			return null;
		}

		private static List<XElement> GetAStrElements(byte[] xmlBytes, int ichMin, int ichLim)
		{
			var retval = new List<XElement>();
			var astrBounds = new ElementBounds(xmlBytes, s_tagsAStr, ichMin, ichLim);
			while (astrBounds.IsValid)
			{
				var astr = Encoding.UTF8.GetString(xmlBytes, astrBounds.BeginTagOffset, astrBounds.Length);
				var xAStr = XElement.Parse(astr);
				retval.Add(xAStr);
				astrBounds.Reset(astrBounds.EndTagOffset, ichLim);
			}
			return retval;
		}

		private static void DeleteImmediatelyAllUnlovedCCRs(
			IDomainObjectDTORepository dtoRepos, ICollection<DomainObjectDTO> goners,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference,
			ICollection<byte[]> oldCCRs,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap)
		{
			var allChartElements = from chartDto in dtoRepos.AllInstancesSansSubclasses("DsConstChart")
								   select XElement.Parse(chartDto.Xml);
			var outboundFromChartsCCRGuids = new HashSet<string>(
				from objsurElement in allChartElements.Descendants("Rows").Elements("objsur")
				select GetGuid(objsurElement));

			var unlovedCCRElements = (from unlovedCCR in oldCCRs
									  where !outboundFromChartsCCRGuids.Contains(GetGuid(unlovedCCR))
									 select unlovedCCR).ToList();
			foreach (var gonerCCR in unlovedCCRElements)
			{
				// Get its dto, and remove the dto from 'goners',
				var hasBeenDto = dtoRepos.GetDTO(GetGuid(gonerCCR));
				goners.Remove(hasBeenDto);
				// Remove gonerCCR element from 'oldCCRs',
				oldCCRs.Remove(gonerCCR);
				ccaGuidMap.Remove(new Guid(hasBeenDto.Guid));
				// and, Delete gonerCCR from dtoRepos.
				DataMigrationServices.RemoveIncludingOwnedObjects(dtoRepos, hasBeenDto, true);

				// Then do similar stuff to any CCA that gonerCCR references in its 'AppliesTo' property.
				var deadbeatCCAGuids = GetAppliesToObjsurGuids(gonerCCR);
				foreach (var gonerCCAGuid in deadbeatCCAGuids)
				{
					// Get dto, and remove it from 'goners',
					DeleteImmediatelyBadCca(dtoRepos, gonerCCAGuid, goners, ccaGuidMap, oldCCAs, xficHasTextOrDiscourseBackReference);
				}
			}
		}

		private static void DeleteImmediatelyBadCca(IDomainObjectDTORepository dtoRepos,
			string gonerCcaGuid,
			ICollection<DomainObjectDTO> goners,
			IDictionary<Guid, Guid> ccaGuidMap,
			IDictionary<string, byte[]> oldCcas,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference)
		{
			var gonerCCADto = dtoRepos.GetDTO(gonerCcaGuid.ToLower());
			goners.Remove(gonerCCADto);
			ccaGuidMap.Remove(new Guid(gonerCCADto.Guid));

			//XElement referencedCCAElement;
			byte[] referencedCCAElement;
			if (oldCcas.TryGetValue(gonerCcaGuid.ToLower(), out referencedCCAElement))
			{
				string classVal = GetClassValue(referencedCCAElement);
				if (classVal == "CmIndirectAnnotation")
				{
					ReduceStoredBackRefCount(xficHasTextOrDiscourseBackReference,
						oldCcas, referencedCCAElement);
				}

				// Remove gonerCCADto's element from 'oldCCAs'.
				oldCcas.Remove(gonerCcaGuid.ToLower());
			}

			// and, Delete gonerCCADto from dtoRepos.
			DataMigrationServices.RemoveIncludingOwnedObjects(dtoRepos, gonerCCADto, true);
		}

		private static void ReduceStoredBackRefCount(
			IDictionary<string, int> xficHasTextOrDiscourseBackReference,
			IDictionary<string, byte[]> oldCcas,
			byte[] referencedCcaElement)
		{
			var refsFromAppliesTo = GetAppliesToObjsurGuids(referencedCcaElement);
			if (refsFromAppliesTo != null && refsFromAppliesTo.Count > 0)
			{
				var objsurGuid = refsFromAppliesTo[0];
				string annTypeGuid;
				try
				{
					annTypeGuid = GetAnnotationTypeGuid(oldCcas[objsurGuid]);
				}
				catch (KeyNotFoundException)
				{
					return; // CmIndirectAnnotations with no annotation type! How can this be! Skip this backref.
				}
				if (annTypeGuid == DataMigrationServices.kTwficAnnDefnGuid || annTypeGuid == DataMigrationServices.kPficAnnDefnGuid)
				{
					// Decrement 'xficHasTextOrDiscourseBackReference' for each xfic in 'AppliesTo'prop.
					foreach (var referencedXfix in refsFromAppliesTo)
					{
						xficHasTextOrDiscourseBackReference[referencedXfix] = --xficHasTextOrDiscourseBackReference[referencedXfix];
					}
				}
			}
		}

		private static void SelectFromDuplicates(
			IDomainObjectDTORepository dtoRepos,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference,
			IDictionary<string, SortedList<int, byte[]>> paraToOldXfics,
			ICollection<KeyValuePair<string, SortedList<int, List<byte[]>>>> paraToDuplicateOldXfics)
		{
			// Look for duplicates that are referenced by discourse or syntax annotations.
			// These 'trump' the others, unless this is not decisive for some reason.

			if (paraToDuplicateOldXfics.Count == 0)
			{
				return; // No duplicates.
			}

			foreach (var paraKvp in paraToDuplicateOldXfics)
			{
				// Key is the paragraph guid.
				// Value is a SortedList of duplicates.
				var sortedDuplicates = paraKvp.Value;
				foreach (var sortedKvp in sortedDuplicates)
				{
					var beginOffset = sortedKvp.Key;
					var duplicateList = sortedKvp.Value;
					byte[] winnerElement = null;
					var previousClass = "";
					var previousRefCount = 0;
					foreach (var duplicate in duplicateList)
					{
						var annTypeGuid = GetAnnotationTypeGuid(duplicate);
						if (annTypeGuid != DataMigrationServices.kTwficAnnDefnGuid && annTypeGuid !=DataMigrationServices.kPficAnnDefnGuid)
							continue;

						var currentClass = GetClassOfTarget(dtoRepos, duplicate);
						var currentRefCount = xficHasTextOrDiscourseBackReference[GetGuid(duplicate)];
						switch (previousClass)
						{
							case "":
								previousRefCount = currentRefCount;
								winnerElement = duplicate;
								break;
							case "WfiGloss":
								// WfiGloss is preferable to the others, but pick one with ref count.
								if (previousRefCount == 0 && currentRefCount > 0)
								{
									previousRefCount = currentRefCount;
									winnerElement = duplicate;
								}
								break;
							case "WfiAnalysis":
								if (currentClass == "WfiGloss")
								{
									// WfiGloss is better than the analysis,
									// even if analysis has a ref count > 0.
									previousRefCount = currentRefCount;
									winnerElement = duplicate;
								}
								break;
							case "WfiWordform":
								if (currentClass == "WfiGloss" || currentClass == "WfiAnalysis")
								{
									// WfiGloss or WfiAnalysis are better than the wordform,
									// even if wordform has a ref count > 0.
									previousRefCount = currentRefCount;
									winnerElement = duplicate;
								}
								break;
						}
						previousClass = currentClass;
					}
					if (winnerElement == null)
						continue;

					// Replace with winner.
					var sortedList = paraToOldXfics[paraKvp.Key];
					if (sortedList[beginOffset] != winnerElement)
						sortedList[beginOffset] = winnerElement;
				}
			}
		}

		private static string GetInstanceOfGuid(byte[] element)
		{
			var cmAnnotationBounds = new ElementBounds(element, s_tagsCmAnnotation);
			if (!cmAnnotationBounds.IsValid)
				return null;
			var instanceOfBounds = new ElementBounds(element, s_tagsInstanceOf,
				cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
			if (!instanceOfBounds.IsValid)
				return null;
			return GetObjsurGuid(element, instanceOfBounds.BeginTagOffset, instanceOfBounds.EndTagOffset);
		}

		private static string GetClassOfTarget(IDomainObjectDTORepository dtoRepos, byte[] xmlBytes)
		{
			var guid = GetInstanceOfGuid(xmlBytes);
			return dtoRepos.GetDTO(guid).Classname;
		}

		private static void ProcessParagraphs(
			IDomainObjectDTORepository dtoRepos,
			IDictionary<string, byte[]> oldCCAs,
			IEnumerable<KeyValuePair<byte[], XElement>> halfBakedCcwgItems,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments,
			IDictionary<string, SortedList<int, byte[]>> paraToOldXfics,
			IDictionary<Guid, Guid> ccaGuidMap,
			ICollection<byte[]> oldTextTags,
			Dictionary<string, List<byte[]>> freeTrans,
			Dictionary<string, List<byte[]>> litTrans,
			Dictionary<string, List<byte[]>> notes)
		{
			var dtos = dtoRepos.AllInstancesWithSubclasses("StTxtPara");
			//var count = dtos.Count();
			//var num = 0;
			//var cpara = 0;
			foreach (var currentParaDto in dtos)
			{
				//++num;
				// If it has no contents, then skip it.
				var stTxtParaBounds = new ElementBounds(currentParaDto.XmlBytes, s_tagsStTxtPara);
				if (!stTxtParaBounds.IsValid)
					continue;
				var contentsBounds = new ElementBounds(currentParaDto.XmlBytes, s_tagsContents,
					stTxtParaBounds.BeginTagOffset, stTxtParaBounds.EndTagOffset);
				if (!contentsBounds.IsValid)
					continue;
				//++cpara;

				// Mark the paragraph as needing retokenization.
				MarkParaAsNeedingTokenization(dtoRepos, currentParaDto);

				var currentParaGuid = currentParaDto.Guid.ToLower();
				SortedList<int, byte[]> xficsForCurrentPara;
				paraToOldXfics.TryGetValue(currentParaGuid, out xficsForCurrentPara);

				SortedList<int, byte[]> segsForCurrentPara;
				if (!paraToOldSegments.TryGetValue(currentParaGuid, out segsForCurrentPara)
					&& xficsForCurrentPara != null
					&& xficsForCurrentPara.Count > 0)
				{
					// We have no segments at all, but there are xfics, so try to recover the broken data,
					// as much as possible.
					// Need to create a new old segment XElement (not dto), to try and and keep old data.
					var guidBrandNewSeg = Guid.NewGuid();
					var brandNewSegGuid = guidBrandNewSeg.ToString().ToLower();
					ccaGuidMap.Add(guidBrandNewSeg, guidBrandNewSeg);
					segsForCurrentPara = new SortedList<int, byte[]>();
					paraToOldSegments.Add(currentParaGuid, segsForCurrentPara);
					var bldr = new StringBuilder();
					bldr.AppendFormat("<rt guid=\"{0}\"", brandNewSegGuid);
					bldr.Append("<CmObject/>");
					bldr.Append("<CmBaseAnnotation>");
					bldr.Append("<BeginOffset val=\"0\"/>");
					bldr.AppendFormat("<EndOffset val=\"{0}\"/>", int.MaxValue);
					bldr.Append("</CmBaseAnnotation>");
					bldr.Append("</rt>");
					segsForCurrentPara.Add(0, Encoding.UTF8.GetBytes(bldr.ToString()));
				}

				// If the para has no segs or xfics, skip the following work.
				if (segsForCurrentPara == null)
					continue;

				if (xficsForCurrentPara != null && xficsForCurrentPara.Count > 0 && segsForCurrentPara.Count > 0)
				{
					// We have both segments and xfics. Check for odd case (like FWR-3081)
					// where the first segment starts AFTER the first xfic, and add a new
					// segment that covers the text up to the first current segment.
					if (xficsForCurrentPara.First().Key < segsForCurrentPara.First().Key)
						AddExtraInitialSegment(currentParaGuid, ccaGuidMap, paraToOldSegments);
				}
				var halfBakedCcwgItemsForCurrentPara = new List<KeyValuePair<byte[], XElement>>();
				List<string> writingSystems;
				var runs = GetParagraphContentRuns(currentParaDto.XmlBytes, out writingSystems);
				// We may well have segments with no xfics, for example, Scripture that has segmented BT.
				if (xficsForCurrentPara != null)
				{

					// Since pfics/wfics were 'optional' and usually not maintained in the db,
					// we need to make sure there is a dummy one in xficsForCurrentPara
					// in order to get the correct Begin/EndAnalysisIndex for chart and tagging objects
					// It turns out we don't need to worry about ws and exact begin/end character offsets.
					// All we need to end up with correct indices is the correct NUMBER of xfics.
					var context = new ParagraphContext(currentParaGuid, xficsForCurrentPara);
					EnsureAllXfics(runs, context);

					// Find any 'halfbaked' items for the current paragraph.
					// Get the para for the first objsur's guid (some twfic ann),
					// in the CmIndirectAnnotation's AppliesTo prop.
					foreach (var kvp in halfBakedCcwgItems)
					{
						var refs = GetAppliesToObjsurGuids(kvp.Key);
						if (refs == null || refs.Count == 0)
							continue;
						var guid = refs[0];
						var dto = dtoRepos.GetDTO(guid);
						var guidBegin = GetBeginObjectGuid(dto.XmlBytes);
						if (guidBegin == currentParaGuid)
							halfBakedCcwgItemsForCurrentPara.Add(kvp);
					}
				}
				var bldrSegmentsElement = new StringBuilder();
				var numberOfOldSegmentsInCurrentPara = segsForCurrentPara.Values.Count;
				var currentOldSegmentIdx = 1;
				foreach (var currentOldSegInCurrentPara in segsForCurrentPara.Values)
				{
					var isLastOldSegment = (currentOldSegmentIdx++ == numberOfOldSegmentsInCurrentPara);
					var oldSegGuid = GetGuid(currentOldSegInCurrentPara);
					var guidOldSeg = new Guid(oldSegGuid);
					var newSegGuid = ccaGuidMap[guidOldSeg].ToString().ToLowerInvariant();
					// Add it to Segments prop of currentParaElement,
					var objsur = DataMigrationServices.CreateOwningObjSurElement(newSegGuid);
					bldrSegmentsElement.AppendLine(objsur.ToString());

					// Create new XElement for new segment.
					var newSegmentElement =
						new XElement("rt",
							new XAttribute("class", "Segment"),
							new XAttribute("guid", newSegGuid),
							new XAttribute("ownerguid", currentParaDto.Guid.ToLower()),
							new XElement("CmObject"),
							new XElement("Segment",
								AddBeginOffset(GetBeginOffset(currentOldSegInCurrentPara)),
								AddFreeTranslation(oldSegGuid, freeTrans),
								AddLiteralTranslation(oldSegGuid, litTrans),
								AddNotes(dtoRepos, newSegGuid, oldSegGuid, notes),
								AddSegmentAnalyses(dtoRepos,
									halfBakedCcwgItemsForCurrentPara,
									currentOldSegInCurrentPara,
									xficsForCurrentPara,
									oldTextTags,
									newSegGuid,
									isLastOldSegment,
									currentParaDto)));
					newSegmentElement = DeleteTemporaryAnalyses(newSegmentElement);
					// Create a new Segment instance DTO from the 'newSegmentElement',
					// and add it to repos.
					var newSegDto = new DomainObjectDTO(newSegGuid, "Segment", newSegmentElement.ToString());
					dtoRepos.Add(newSegDto);
				}

				paraToOldSegments.Remove(currentParaGuid.ToLower());
				paraToOldXfics.Remove(currentParaGuid.ToLower());

				if (bldrSegmentsElement.Length == 0)
					continue;
				bldrSegmentsElement.Insert(0, "<Segments>");
				bldrSegmentsElement.Append("</Segments>");

				// Add paraSegmentsElement to current para.
				var segBytes = Encoding.UTF8.GetBytes(bldrSegmentsElement.ToString());
				var xmlNew = new List<byte>(currentParaDto.XmlBytes.Length + segBytes.Length);
				xmlNew.AddRange(currentParaDto.XmlBytes);
				stTxtParaBounds = new ElementBounds(currentParaDto.XmlBytes, s_tagsStTxtPara);
				xmlNew.InsertRange(stTxtParaBounds.EndTagOffset, segBytes);
				// Tell DTO repos about the modification.
				DataMigrationServices.UpdateDTO(dtoRepos, currentParaDto, xmlNew.ToArray());
			}
		}

		private static List<string> GetParagraphContentRuns(byte[] xmlStTxtPara, out List<string> writingSystems)
		{
			var retval = new List<string>();
			writingSystems = new List<string>();
			var stTxtParaBounds = new ElementBounds(xmlStTxtPara, s_tagsStTxtPara);
			var contentsBounds = new ElementBounds(xmlStTxtPara, s_tagsContents,
				stTxtParaBounds.BeginTagOffset, stTxtParaBounds.EndTagOffset);
			var strBounds = new ElementBounds(xmlStTxtPara, s_tagsStr,
				contentsBounds.BeginTagOffset, contentsBounds.EndTagOffset);
			if (!strBounds.IsValid)
				return retval;
			var runBounds = new ElementBounds(xmlStTxtPara, s_tagsRun,
				strBounds.BeginTagOffset, strBounds.EndTagOffset);
			while (runBounds.IsValid)
			{
				var ws = runBounds.GetAttributeValue(s_wsAttr);
				writingSystems.Add(ws);
				var ichText = runBounds.EndOfStartTag + 1;	// move past the >
				var runText = Encoding.UTF8.GetString(xmlStTxtPara, ichText, runBounds.EndTagOffset - ichText);
				retval.Add(XmlUtils.DecodeXmlAttribute(runText));
				runBounds.Reset(runBounds.EndTagOffset, contentsBounds.EndTagOffset);
			}
			return retval;
		}

		private static XElement DeleteTemporaryAnalyses(XElement newSegmentElement)
		{
			// Assuming we decided to use a ktempXficInstanceOfGuid to create
			// temporary InstanceOf items for temporary xfics that we no longer need
			// (after setting CCWordGroup and TextTag Begin/EndAnalysis indices),
			// we need to remove them now before they get added to the dto repository.
			// We'll let the 'real' parser create the 'real' IAnalysis objects on load.
			if (newSegmentElement.Element("Segment").Element("Analyses") == null)
				return newSegmentElement; // nothing to delete.
			var allAnalysesObjSurElements = newSegmentElement.Element("Segment").Element("Analyses").Elements("objsur");
			var newAnalysesList = new XElement("Analyses");
			foreach (var objSurElement in allAnalysesObjSurElements)
			{
				if (objSurElement.Attribute("guid").Value != ktempXficInstanceOfGuid)
					newAnalysesList.Add(objSurElement);
			}
			newSegmentElement.Element("Segment").Element("Analyses").ReplaceWith(newAnalysesList);
			return newSegmentElement;
		}

		private static void EnsureAllXfics(IList<string> runs, ParagraphContext context)
		{
			if (runs == null || runs.Count == 0)
				return; // No <Run> elements; can't have any xfics.
			var bldr = new StringBuilder();
			for (var i = 0; i < runs.Count; ++i)
				bldr.Append(runs[i]);
			var text = Icu.Normalize(bldr.ToString(), Icu.UNormalizationMode.UNORM_NFD);
			ParseTextAndCheckForXfics(text, context);
		}

		private static void ParseTextAndCheckForXfics(string text, ParagraphContext context)
		{
			List<Tuple<int, int, bool>> neededXficForms;
			new XficParser(text, context.ParaXfics).Run(out neededXficForms);

			if (neededXficForms.Count == 0)
				return;

			CreateTemporaryXfics(neededXficForms, context);
			return;
		}

		private static void CreateTemporaryXfics(IEnumerable<Tuple<int, int, bool>> neededXficForms,
			ParagraphContext context)
		{
			// Since xfics were 'optional' and usually not maintained in the db,
			// we need to make sure there is a temporary dummy one in xficsForCurrentPara
			// in order to get the correct Begin/EndAnalysisIndex for chart and tagging objects
			// N.B. We use a temporary InstanceOf guid that is stripped out after creating these objects.
			foreach (var xficTuple in neededXficForms)
				CreateTemporaryXfic(xficTuple.Item3, xficTuple.Item1, xficTuple.Item2, context);
		}

		private static void CreateTemporaryXfic(bool fWfic, int beginOffset, int endOffset, ParagraphContext context)
		{
			var currentParaGuid = context.ParaGuid;
			var xficsForCurrentPara = context.ParaXfics;

			// if fWfic, create temporary wfic; if !fWfic, create temporary pfic
			var annoTypeGuid = fWfic ? DataMigrationServices.kTwficAnnDefnGuid : DataMigrationServices.kPficAnnDefnGuid;

			// Need to create a new old xfic XElement (not dto), to try and and maintain analysis indices.
			var brandNewPficGuid = Guid.NewGuid().ToString().ToLower();
			const int paraContentsFlid = StTxtParaTags.kflidContents;
			var tmp = new XElement("rt",
				new XAttribute("class", "CmBaseAnnotation"),
				new XAttribute("guid", brandNewPficGuid),
				new XElement("CmObject"),
				new XElement("CmAnnotation",
					new XElement("AnnotationType",
						 DataMigrationServices.CreateReferenceObjSurElement(annoTypeGuid)),
					new XElement("InstanceOf",
						 DataMigrationServices.CreateReferenceObjSurElement(ktempXficInstanceOfGuid))),
				new XElement("CmBaseAnnotation",
					new XElement("BeginOffset", new XAttribute("val", beginOffset)),
					new XElement("EndOffset", new XAttribute("val", endOffset)),
					new XElement("Flid", new XAttribute("val", paraContentsFlid)),
					new XElement("BeginObject",
						 DataMigrationServices.CreateReferenceObjSurElement(currentParaGuid))));
			xficsForCurrentPara.Add(beginOffset, Encoding.UTF8.GetBytes(tmp.ToString()));
		}

		private static void CollectIndirectAnnotations(
			IDomainObjectDTORepository dtoRepos, ICollection<DomainObjectDTO> goners,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference,
			ICollection<byte[]> oldCCRows,
			ICollection<byte[]> oldTextTags,
			Dictionary<string, List<byte[]>> freeTrans, Dictionary<string, List<byte[]>> litTrans,
			Dictionary<string, List<byte[]>> notes)
		{
			var dtos = dtoRepos.AllInstancesSansSubclasses("CmIndirectAnnotation");
			var count = dtos.Count();
			var num = 0;
			var cann = 0;
			foreach (var indirectAnnDto in dtos)
			{
				++num;
				var annElement = indirectAnnDto.XmlBytes;
				var typeGuid = GetAnnotationTypeGuid(annElement);
				if (String.IsNullOrEmpty(typeGuid))
					continue;
				++cann;
				switch (typeGuid)
				{
					default:
						// Let these go, since they are not interesting.
						break;
					case DataMigrationServices.kTextTagAnnDefnGuid:
						var refsFromAppliesTo = GetAppliesToObjsurGuids(annElement);
						if (refsFromAppliesTo == null || refsFromAppliesTo.Count == 0)
						{
							// Remove defective TextTag from consideration.
							goners.Add(indirectAnnDto);
							break;
						}

						if (CheckAgainstXfics(dtoRepos, annElement, xficHasTextOrDiscourseBackReference))
							PreprocessTextTagAnnotation(goners, oldTextTags, indirectAnnDto, annElement);
						break;
					case DataMigrationServices.kConstituentChartRowAnnDefnGuid:
						var indAnnDtoGuid = indirectAnnDto.Guid.ToLower();
						ccaGuidMap.Add(new Guid(indAnnDtoGuid), Guid.NewGuid());
						oldCCAs.Add(indAnnDtoGuid, annElement);
						PreprocessDiscourseAnnotation(goners, oldCCRows, indirectAnnDto, annElement);
						break;
					case DataMigrationServices.kConstituentChartAnnotationAnnDefnGuid:
						// These can be either indirect anns or base anns.
						if (CheckAgainstXfics(dtoRepos, annElement, xficHasTextOrDiscourseBackReference))
							PreprocessDiscourseAnnotation(goners, oldCCAs, ccaGuidMap, indirectAnnDto, annElement);
						break;
					case DataMigrationServices.kFreeTransAnnDefnGuid:
						PreprocessTranslationOrNoteAnnotation(goners, freeTrans, indirectAnnDto, annElement);
						break;
					case DataMigrationServices.kLitTransAnnDefnGuid:
						PreprocessTranslationOrNoteAnnotation(goners, litTrans, indirectAnnDto, annElement);
						break;
					case DataMigrationServices.kNoteAnnDefnGuid:
						PreprocessTranslationOrNoteAnnotation(goners, notes, indirectAnnDto, annElement);
						break;
				}
			}
		}

		private static bool CheckAgainstXfics(IDomainObjectDTORepository dtoRepos,
			byte[] annElement,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference)
		{
			// Return true if annElement's type is a CCR.
			var annDefnTypeGuid = GetAnnotationTypeGuid(annElement);
			if (annDefnTypeGuid == DataMigrationServices.kConstituentChartRowAnnDefnGuid)
				return true;
			// Return true, if annElement's AppliesTo is null.
			var appliesToGuids = GetAppliesToObjsurGuids(annElement);
			if (appliesToGuids == null || appliesToGuids.Count == 0)
				return true;
			var originalCount = appliesToGuids.Count;

			var firstGuidInAppliesTo = appliesToGuids[0];
			// Get ann defn type of the guid.
			annDefnTypeGuid = GetAnnotationTypeGuid(dtoRepos.GetDTO(firstGuidInAppliesTo).XmlBytes);
			// Return true if the first object in annElement's AppliesTo is not an xfic at all.
			if (annDefnTypeGuid != DataMigrationServices.kTwficAnnDefnGuid && annDefnTypeGuid != DataMigrationServices.kPficAnnDefnGuid)
				return true;

			// By this point, annElement must be either a text tag or discourse ann that refs xfics.
			var danglingXfixRefs = new List<string>();
			foreach (var xficGuid in appliesToGuids)
			{
				int inboundRefsCount;
				if (!xficHasTextOrDiscourseBackReference.TryGetValue(xficGuid, out inboundRefsCount))
				{
					// Dangling ref, so remove from property.
					danglingXfixRefs.Add(xficGuid);
					continue;
				}
				// Xfic has reference from either a text tag or a discourse ann,
				// so increment the Value in xficHasTextOrDiscourseBackReference.
				xficHasTextOrDiscourseBackReference[xficGuid] = ++inboundRefsCount;
			}
			foreach (var danglingRef in danglingXfixRefs)
			{
				while(appliesToGuids.Remove(danglingRef))
				{}
			}

			// If text tag or discourse ann has any surviving refs, then return true, otherwise return false.
			if (appliesToGuids.Count == 0)
				return false; // No more outbound refs at all.

			if (originalCount != appliesToGuids.Count)
			{
				// Reset the old AppliesTo prop to survivors.
				RemoveMismatchedAppliesToRefs(annElement, appliesToGuids);
			}

			return true;
		}

		private static void RemoveMismatchedAppliesToRefs(byte[] xmlBytes, ICollection<string> appliesToGuids)
		{
			var cmIndirectBounds = new ElementBounds(xmlBytes, s_tagsCmIndirect);
			if (!cmIndirectBounds.IsValid)
				return;
			var appliesToBounds = new ElementBounds(xmlBytes, s_tagsAppliesTo,
				cmIndirectBounds.BeginTagOffset, cmIndirectBounds.EndTagOffset);
			if (!appliesToBounds.IsValid)
				return;
			var objsurBounds = new ElementBounds(xmlBytes, s_tagsObjsur,
				appliesToBounds.BeginTagOffset, appliesToBounds.EndTagOffset);
			while (objsurBounds.IsValid)
			{
				var ichMin = objsurBounds.BeginTagOffset;
				var guid = objsurBounds.GetAttributeValue(s_guidAttr);
				if (!String.IsNullOrEmpty(guid))
					guid = guid.ToLowerInvariant();
				objsurBounds.Reset(objsurBounds.EndTagOffset, appliesToBounds.EndTagOffset);
				if (!String.IsNullOrEmpty(guid) && !appliesToGuids.Contains(guid))
				{
					var ichLim = objsurBounds.BeginTagOffset;
					if (ichLim < 0)
						ichLim = appliesToBounds.EndTagOffset;
					// Remove the <objsur> element by overwriting it with spaces.
					for (var i = ichMin; i < ichLim; ++i)
						xmlBytes[i] = 0x20;
				}
			}
		}

		private static void CollectBaseAnnotations(
			IDomainObjectDTORepository dtoRepos, ICollection<DomainObjectDTO> goners,
			IDictionary<string, byte[]> oldCCAs,
			IDictionary<Guid, Guid> ccaGuidMap,
			IDictionary<string, int> xficHasTextOrDiscourseBackReference,
			IDictionary<string, SortedList<int, List<byte[]>>> paraToDuplicateOldSegments,
			IDictionary<string, SortedList<int, byte[]>> paraToOldSegments,
			IDictionary<string, SortedList<int, List<byte[]>>> paraToDuplicateOldXfics,
			IDictionary<string, SortedList<int, byte[]>> paraToOldXfics)
		{
			var newPunctForms = new Dictionary<string, DomainObjectDTO>();
			foreach (var baseAnnDto in dtoRepos.AllInstancesSansSubclasses("CmBaseAnnotation"))
			{
				var surrGuid = GetAnnotationTypeGuid(baseAnnDto.XmlBytes);
				if (String.IsNullOrEmpty(surrGuid))
					continue;

				var baseAnnDtoGuid = baseAnnDto.Guid.ToLower();
				switch (surrGuid)
				{
					default:
						// Let these go, since they are not interesting.
						break;
					case DataMigrationServices.kConstituentChartAnnotationAnnDefnGuid:
						// These can be either indirect anns or base anns.
						PreprocessDiscourseAnnotation(goners, oldCCAs, ccaGuidMap, baseAnnDto, baseAnnDto.XmlBytes);
						break;
					case DataMigrationServices.kSegmentAnnDefnGuid:
						// Segments aren't really CCA anns,
						// but ccaGuidMap really ought to be more generic to hold any old+new guid mapping.
						ccaGuidMap.Add(new Guid(baseAnnDtoGuid), Guid.NewGuid());
						PreprocessBaseAnnotation(dtoRepos, goners,
												 paraToDuplicateOldSegments,
												 paraToOldSegments, baseAnnDto, baseAnnDto.XmlBytes, false);
						break;
					case DataMigrationServices.kTwficAnnDefnGuid:
						xficHasTextOrDiscourseBackReference.Add(baseAnnDtoGuid, 0);
						ccaGuidMap.Add(new Guid(baseAnnDtoGuid), Guid.NewGuid());
						oldCCAs.Add(baseAnnDtoGuid, baseAnnDto.XmlBytes);
						PreprocessBaseAnnotation(dtoRepos, goners,
												 paraToDuplicateOldXfics,
												 paraToOldXfics, baseAnnDto, baseAnnDto.XmlBytes, true);
						break;
					case DataMigrationServices.kPficAnnDefnGuid:
						if (EnsurePficHasInstanceOf(dtoRepos, baseAnnDto, newPunctForms))
						{
							xficHasTextOrDiscourseBackReference.Add(baseAnnDtoGuid, 0);
							oldCCAs.Add(baseAnnDtoGuid, baseAnnDto.XmlBytes);
							// New guid will be in InstanceOf.
							ccaGuidMap.Add(new Guid(baseAnnDtoGuid),
								new Guid(GetInstanceOfGuid(baseAnnDto.XmlBytes)));
							PreprocessBaseAnnotation(dtoRepos, goners,
													 paraToDuplicateOldXfics,
													 paraToOldXfics, baseAnnDto, baseAnnDto.XmlBytes, false);
						}
						else
						{
							// Don't add to other maps, since this one has no matching text in the para.
							goners.Add(baseAnnDto);
						}
						break;
				}
			}
			newPunctForms.Clear();
		}


		private static string GetAnnotationTypeGuid(byte[] xmlBytes)
		{
			var cmAnnotationBounds = new ElementBounds(xmlBytes, s_tagsCmAnnotation);
			if (!cmAnnotationBounds.IsValid)
				return null;
			var annotationTypeBounds = new ElementBounds(xmlBytes, s_tagsAnnotationType,
				cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
			if (!annotationTypeBounds.IsValid)
				return null;
			return GetObjsurGuid(xmlBytes, annotationTypeBounds.BeginTagOffset, annotationTypeBounds.EndTagOffset);
		}

		private static bool EnsurePficHasInstanceOf(
			IDomainObjectDTORepository dtoRepos,
			DomainObjectDTO dtoPfic,
			IDictionary<string, DomainObjectDTO> newPunctForms)
		{
			var pficElement = dtoPfic.XmlBytes;
			/*
<Contents>
	<Str>
		<Run ws="eZPI">Ne Wlalo lo San Juan. </Run>
		<Run ws="es">Otras cosas.</Run>
	</Str>
</Contents>
			*/
			// Find relevant paragraph from BeginObject property of 'pficElement'.
			var paraDto = dtoRepos.GetDTO(GetBeginObjectGuid(pficElement));
			var beginOffset = GetBeginOffset(pficElement);
			var endOffset = GetEndOffset(pficElement);

			if (beginOffset > endOffset)
			{
				// Bad begin or end offset.
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);
				return false;
			}
			if (beginOffset < 0)
			{
				// Bad begin offset.
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);
				return false;
			}
			// Concatenate data from all runs.
			List<string> writingSystems;
			var runs = GetParagraphContentRuns(paraDto.XmlBytes, out writingSystems);
			Debug.Assert(runs.Count == writingSystems.Count);
			var bldr = new StringBuilder();
			for (int i = 0; i < runs.Count; ++i)
				bldr.Append(runs[i]);
			var fullParaContents = Icu.Normalize(bldr.ToString(), Icu.UNormalizationMode.UNORM_NFD);
			if (endOffset > fullParaContents.Length)
			{
				// Total string is too short (end offset beyond end of string).
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);
				return false;
			}
			// Find the form of the punctuation mark.
			var newForm = fullParaContents.Substring(beginOffset, endOffset - beginOffset);
			if (newForm == String.Empty)
			{
				// No punctuation form at all.
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);
				return false;
			}
			var icuLocale = String.Empty;
			// Find the ws's IcuLocale at the begin offset in whichever run it is in.
			var currentTotalLength = 0;
			for (var i = 0; i < runs.Count; ++i)
			{
				var currentRunText = Icu.Normalize(runs[i], Icu.UNormalizationMode.UNORM_NFD);
				currentTotalLength += currentRunText.Length;
				if (beginOffset >= currentTotalLength)
					continue; // Not in this run.

				if (endOffset > currentTotalLength)
				{
					// It starts in one run and ends in another, so bail out.
					MarkParaAsNeedingTokenization(dtoRepos, paraDto);
					return false;
				}
				// It's all in this run, so quit looking at runs.
				icuLocale = writingSystems[i];
				break;
			}

			if (icuLocale == String.Empty)
			{
				// Hard to say how we can get here, but something is very wrong.
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);
				return false;
			}

			// If the new PF is all in one run, and we have its IcuLocale,
			// then make the new PF object, and return true.
			// Find/Create PunctuationForm object that has a Form in the matching IcuLocale & matching string.
			var key = icuLocale + "-" + newForm;
			DomainObjectDTO dtoMatchingPf;
			if (!newPunctForms.TryGetValue(key, out dtoMatchingPf))
			{
				// Create new PunctuationForm dto.
				var newPunctFormGuid = Guid.NewGuid().ToString().ToLower();
				const string className = "PunctuationForm";
				var newPfElement = new XElement("rt",
					new XAttribute("class", className),
					new XAttribute("guid", newPunctFormGuid),
					new XElement("CmObject"),
					new XElement("PunctuationForm",
						new XElement("Form",
							new XElement("Str",
								new XElement("Run", new XAttribute("ws", icuLocale), newForm)))));
				dtoMatchingPf = new DomainObjectDTO(newPunctFormGuid, className, newPfElement.ToString());
				// Add new PunctuationForm to dtoRepos.
				dtoRepos.Add(dtoMatchingPf);
				// Add new PunctuationForm to newPunctForms.
				newPunctForms.Add(key, dtoMatchingPf);
			}

			// Assign InstanceOf for pficElement to matching PunctuationForm object.
			// NB: No need to mess with registering it as modified,
			// since it gets deleted anyway later on.
			var innerBounds = new ElementBounds(pficElement, s_tagsCmAnnotation);
			Debug.Assert(innerBounds.IsValid);
			var pficBytes = new List<byte>(pficElement);
			var instanceOf = String.Format("<InstanceOf>\r\n{0}\r\n</InstanceOf>\r\n",
				DataMigrationServices.CreateReferenceObjSurElement(dtoMatchingPf.Guid));
			pficBytes.InsertRange(innerBounds.EndTagOffset, Encoding.UTF8.GetBytes(instanceOf));
			dtoPfic.XmlBytes = pficBytes.ToArray();
			return true;
		}

		private static void MarkParaAsNeedingTokenization(IDomainObjectDTORepository dtoRepos,
			DomainObjectDTO paraDto)
		{
			var stTxtParaBounds = new ElementBounds(paraDto.XmlBytes, s_tagsStTxtPara);
			if (!stTxtParaBounds.IsValid)
				return;		// should never happen!
			var parseIsCurrentBounds = new ElementBounds(paraDto.XmlBytes, s_tagsParseIsCurrent,
				stTxtParaBounds.BeginTagOffset, stTxtParaBounds.EndTagOffset);
			List<byte> newXml;
			int ichInsert;
			if (parseIsCurrentBounds.IsValid)
			{
				var val = parseIsCurrentBounds.GetAttributeValue(s_valAttr);
				if (val.ToLowerInvariant() != "true")
					return;
				newXml = new List<byte>(paraDto.XmlBytes.Length + 5);
				newXml.AddRange(paraDto.XmlBytes);
				newXml.RemoveRange(parseIsCurrentBounds.BeginTagOffset, parseIsCurrentBounds.Length);
				ichInsert = parseIsCurrentBounds.BeginTagOffset;
			}
			else
			{
				newXml = new List<byte>(paraDto.XmlBytes.Length + 40);
				newXml.AddRange(paraDto.XmlBytes);
				ichInsert = stTxtParaBounds.EndTagOffset;
			}
			newXml.InsertRange(ichInsert, Encoding.UTF8.GetBytes("<ParseIsCurrent val=\"False\"/>"));
			// Tell DTO repos about the modification to the para.
			DataMigrationServices.UpdateDTO(dtoRepos, paraDto, newXml.ToArray());
		}

		private static XElement AddBeginOffset(int offset)
		{
			return new XElement("BeginOffset", new XAttribute("val", offset));
		}

		private static XElement AddFreeTranslation(string oldSegGuid, Dictionary<string, List<byte[]>> freeTransPairs)
		{
			return CreateATranslation(oldSegGuid, freeTransPairs, "FreeTranslation");
		}

		private static XElement AddLiteralTranslation(string oldSegGuid, Dictionary<string, List<byte[]>> literalTransPairs)
		{
			return CreateATranslation(oldSegGuid, literalTransPairs, "LiteralTranslation");
		}

		private static List<string> GetAppliesToObjsurGuids(byte[] xmlBytes)
		{
			var retval = new List<string>();
			var indirectBounds = new ElementBounds(xmlBytes, s_tagsCmIndirect);
			if (!indirectBounds.IsValid)
				return retval;
			var appliesToBounds = new ElementBounds(xmlBytes, s_tagsAppliesTo,
				indirectBounds.BeginTagOffset, indirectBounds.EndTagOffset);
			var ichNext = appliesToBounds.BeginTagOffset + s_tagsAppliesTo.BeginTag.Length;
			var objsurBounds = new ElementBounds(xmlBytes, s_tagsObjsur,
				appliesToBounds.BeginTagOffset, appliesToBounds.EndTagOffset);
			while (objsurBounds.IsValid)
			{
				var guid = GetGuid(xmlBytes, objsurBounds.BeginTagOffset, objsurBounds.EndTagOffset);
				if (!String.IsNullOrEmpty(guid))
					retval.Add(guid.ToLowerInvariant());
				objsurBounds.Reset(objsurBounds.EndTagOffset, appliesToBounds.EndTagOffset);
			}
			return retval;
		}

		private static XElement CreateATranslation(string oldSegGuid, Dictionary<string, List<byte[]>> inputPairs,
			string elementName)
		{
			List<byte[]> allOldTrans;
			if (!inputPairs.TryGetValue(oldSegGuid, out allOldTrans))
				return null; // Nothing to move.
			if (allOldTrans == null || allOldTrans.Count == 0)
				return null;
			var firstOldTrans = allOldTrans[0];

			// Move optional Comment from indirect ann to new element.
			var cmAnnotationBounds = new ElementBounds(firstOldTrans, s_tagsCmAnnotation);
			if (!cmAnnotationBounds.IsValid)
				return null;
			var commentBounds = new ElementBounds(firstOldTrans, s_tagsComment,
				cmAnnotationBounds.BeginTagOffset, cmAnnotationBounds.EndTagOffset);
			if (!commentBounds.IsValid)
				return null;
			var actualCommentNodes = GetAStrElements(firstOldTrans, commentBounds.BeginTagOffset, commentBounds.EndTagOffset);
			return new XElement(elementName, actualCommentNodes);
		}

		private static void PreprocessTextTagAnnotation(ICollection<DomainObjectDTO> goners,
			ICollection<byte[]> annElements,
			DomainObjectDTO annDto,
			byte[] annElement)
		{
			goners.Add(annDto);
			annElements.Add(annElement);
		}

		private static void PreprocessDiscourseAnnotation(ICollection<DomainObjectDTO> goners,
			IDictionary<string, byte[]> annElements,
			IDictionary<Guid, Guid> ccaGuidMap,
			DomainObjectDTO annDto, byte[] annElement)
		{
			goners.Add(annDto);
			var oldGuid = GetGuid(annElement);
			annElements.Add(oldGuid, annElement);
			// Add new guid here, so the new guid can be used,
			// when converting CCAs that reference other CCAs.
			ccaGuidMap.Add(new Guid(oldGuid), Guid.NewGuid());
		}

		private static void PreprocessDiscourseAnnotation(ICollection<DomainObjectDTO> goners,
			ICollection<byte[]> annElements, DomainObjectDTO annDto, byte[] annElement)
		{
			goners.Add(annDto);
			var annoBounds = new ElementBounds(annElement, s_tagsCmAnnotation);
			var commentBounds = new ElementBounds(annElement, s_tagsComment,
				annoBounds.BeginTagOffset, annoBounds.EndTagOffset);
			var astrBounds = new ElementBounds(annElement, s_tagsAStr,
				commentBounds.BeginTagOffset, commentBounds.EndTagOffset);
			if (astrBounds.IsValid)
				annElements.Add(annElement);
		}

		private static void PreprocessTranslationOrNoteAnnotation(ICollection<DomainObjectDTO> goners,
			Dictionary<string, List<byte[]>> annElements, DomainObjectDTO annDto, byte[] annElement)
		{
			goners.Add(annDto);
			var annoBounds = new ElementBounds(annElement, s_tagsCmAnnotation);
			var commentBounds = new ElementBounds(annElement, s_tagsComment,
				annoBounds.BeginTagOffset, annoBounds.EndTagOffset);
			var astrBounds = new ElementBounds(annElement, s_tagsAStr,
				commentBounds.BeginTagOffset, commentBounds.EndTagOffset);
			if (!astrBounds.IsValid)
				return;

			var appliesToElement = GetAppliesToObjsurGuids(annElement);
			if (appliesToElement != null && appliesToElement.Count > 0)
			{
				var guid = appliesToElement[0];
				List<byte[]> elements = null;
				if (!annElements.TryGetValue(guid, out elements))
				{
					elements = new List<byte[]>();
					annElements[guid] = elements;
				}
				elements.Add(annElement);
			}
		}

		private static void PreprocessBaseAnnotation(
			IDomainObjectDTORepository dtoRepos,
			ICollection<DomainObjectDTO> goners,
			IDictionary<string, SortedList<int, List<byte[]>>> paraToDuplicates,
			IDictionary<string, SortedList<int, byte[]>> paraToAnnotation,
			DomainObjectDTO baseAnnDto, byte[] annElement, bool checkOnInstanceOf)
		{
			goners.Add(baseAnnDto);

			if (checkOnInstanceOf)
			{
				// Filter out twfics with no InstanceOf property value.
				var guidInstanceOf = GetInstanceOfGuid(annElement);
				if (String.IsNullOrEmpty(guidInstanceOf))
				{
					// Make sure it has at least an empty sorted list,
					// so later code doesn't crash on a null ref on the list.
					var beginObjectGuidInner = GetBeginObjectGuid(annElement);
					SortedList<int, byte[]> listInner;
					if (!paraToAnnotation.TryGetValue(beginObjectGuidInner, out listInner))
					{
						listInner = new SortedList<int, byte[]>();
						paraToAnnotation.Add(beginObjectGuidInner, listInner);
					}
					return;
				}
			}

			// The 'beginObjectGuid' will always be some StTxtPara (or ScrTxtPara).
			var beginObjectGuid = GetBeginObjectGuid(annElement);
			SortedList<int, byte[]> list;
			if (!paraToAnnotation.TryGetValue(beginObjectGuid, out list))
			{
				list = new SortedList<int, byte[]>();
				paraToAnnotation.Add(beginObjectGuid, list);
			}
			var beginOffset = GetBeginOffset(annElement);

			if (list.ContainsKey(beginOffset))
			{
				// Deal with the duplicate beginOffset issue, as per JT's instructions.
				// At this point, we just gsther up the duplicates for processing later.
				// The duplicates can be either old segments of old xfics,
				// depending on the input parameters.
				// We don't care at this point which they are, as later code handles the duplicates.
				// When the followup processing is done, 'lists' will have the 'winner' annotation.
				// The larger para list should also have the duplicates removed,
				// since other clients use stuff in it, and they don't need to clutter.

				// Set para to be retokenized.
				// Reset ParseIsCurrent to False on Para, so it gets retokenized.
				// It's fine to make the XElement for the para here,
				// since the main para handling with its new XElement hasn't been made yet.
				// Thus, the main element will reflect this change, when it gets created.
				var paraDto = dtoRepos.GetDTO(beginObjectGuid);
				MarkParaAsNeedingTokenization(dtoRepos, paraDto);

				// See if we've seen this one before.
				SortedList<int, List<byte[]>> sortedList;
				List<byte[]> innerElementList;
				if (!paraToDuplicates.TryGetValue(paraDto.Guid, out sortedList))
				{
					// First time we've seen a duplicate for this para, so create sorted list.
					sortedList = new SortedList<int, List<byte[]>>();
					paraToDuplicates.Add(paraDto.Guid, sortedList);
					// Add the first arrival into the sorted list.
					innerElementList = new List<byte[]>
										{
											list[beginOffset]
										};
					sortedList.Add(beginOffset, innerElementList);
				}
				if (!sortedList.TryGetValue(beginOffset, out innerElementList))
				{
					// We've seen the paragraph before, but with some other duplicate.
					innerElementList = new List<byte[]>
										{
											list[beginOffset]
										};
					sortedList.Add(beginOffset, innerElementList);
				}
				// Add the new duplicate.
				innerElementList.Add(annElement);
			}
			else
			{
				list.Add(beginOffset, annElement);
			}
		}

		private static XElement GetOptionalComment(byte[] xmlBytes, string elementName)
		{
			var annoBounds = new ElementBounds(xmlBytes, s_tagsCmAnnotation);
			var commentBounds = new ElementBounds(xmlBytes, s_tagsComment,
				annoBounds.BeginTagOffset, annoBounds.EndTagOffset);
			if (!commentBounds.IsValid)
				return null;
			return new XElement(elementName,
				GetAStrElements(xmlBytes, commentBounds.BeginTagOffset, commentBounds.EndTagOffset));
		}

		private static XElement AddNotes(IDomainObjectDTORepository dtoRepos, string newSegGuid, string oldSegGuid, Dictionary<string, List<byte[]>> notes)
		{
			// This method will create zero, or more, new Nt
			// instances, which will be owned by the new segment.
			// (Added to the new segment, when returned.)
			// Each new Nt object needs to be added to the repos,
			// and added as an owned object in the Notes prop of the new Segment.
			XElement retval = null;
			List<byte[]> oldNotes;
			if (notes.TryGetValue(oldSegGuid, out oldNotes))
			{
				retval = new XElement("Notes");
				foreach (var oldNotePair in oldNotes)
				{
					var newNoteGuid = Guid.NewGuid().ToString().ToLower();

					// Add to Notes element as owned objsur element.
					retval.Add(DataMigrationServices.CreateOwningObjSurElement(newNoteGuid));

					// Create the new Nt object.
					var newNoteElement = new XElement("rt",
						new XAttribute("class", "Note"),
						new XAttribute("guid", newNoteGuid),
						new XAttribute("ownerguid", newSegGuid.ToLower()),
						new XElement("CmObject"),
						new XElement("Note", GetOptionalComment(oldNotePair, "Content")));
					// Create new dto and add to repos.
					var newNoteDto = new DomainObjectDTO(newNoteGuid, "Note", newNoteElement.ToString());
					dtoRepos.Add(newNoteDto);
				}
			}
			return retval;
		}

		private static XElement AddSegmentAnalyses(
			IDomainObjectDTORepository dtoRepos,
			IEnumerable<KeyValuePair<byte[], XElement>> halfBakedCcwgItemsForCurrentPara,
			byte[] oldSegElement,
			IDictionary<int, byte[]> xficsForCurrentPara,
			ICollection<byte[]> oldTextTags,
			string newSegmentGuid,
			bool isLastOldSegment,
			DomainObjectDTO paraDto)
		{
			XElement retval = null;

			var oldSegBeginOffset = GetBeginOffset(oldSegElement);
			var oldSegEndOffset = GetEndOffset(oldSegElement);

			var xficsForCurrentOldSegment = new SortedList<int, byte[]>();
			var halfBakedCcwgsForCurrentOldSegment = new List<XElement>();
			if (xficsForCurrentPara != null)
			{
				foreach (var kvp in xficsForCurrentPara)
				{
					var xficGuid = GetGuid(kvp.Value);
					var beginOffset = kvp.Key;

					// Try to find a CCWG that has a matching instanceOfGuid.
					XElement halfBakedCcwg = null;
					foreach (var halfBakedKvp in halfBakedCcwgItemsForCurrentPara)
					{
						// NB: halfBakedKvp.Value.Element("ConstChartWordGroup").Element("BeginAnalysisIndex").Attribute("val").Value.ToLower()
						// This is the 'InstanceOf' guid of the xfic, not the xfic's guid.
						if (
							halfBakedKvp.Value.Element("ConstChartWordGroup").Element("BeginAnalysisIndex").Attribute(
								"val").Value.ToLower() != xficGuid)
							continue;

						// If there happen to be more than one CCWG pointing to the same xfic, only one gets 'finished'!
						halfBakedCcwg = halfBakedKvp.Value;
						break;
					}

					if (beginOffset >= oldSegBeginOffset && beginOffset < oldSegEndOffset)
					{
						xficsForCurrentOldSegment.Add(beginOffset, kvp.Value);
						if (halfBakedCcwg != null)
							halfBakedCcwgsForCurrentOldSegment.Add(halfBakedCcwg);
					}
					else if (isLastOldSegment)
					{
						if (halfBakedCcwg != null)
							halfBakedCcwgsForCurrentOldSegment.Add(halfBakedCcwg);
						xficsForCurrentOldSegment.Add(beginOffset, kvp.Value);
					}
				}
			}

			if (xficsForCurrentOldSegment.Count > 0)
			{
				foreach (var key in xficsForCurrentOldSegment.Keys)
					xficsForCurrentPara.Remove(key);

				// The one returned element is "Analyses" (or null, if no twfics/pfics).
				// It will have one, or more, 'objsur' type 'r' elements in it.
				// The 'Analyses' property is a seq,
				// which is why is xficsForCurrentOldSegment is sorted the BeginOffset of the contained twfics/pfics.

				// All xfics will have an 'InstanceOf' by now, even pfics.
				// The pfics had a new InstanceOf prop set earlier
				// (or were removed, if InstanceOf could not be set),
				// and twfics with no InstanceOf prop were filtered out earlier.
				retval = new XElement("Analyses",
					from xfix in xficsForCurrentOldSegment.Values
					select DataMigrationServices.CreateReferenceObjSurElement(GetInstanceOfGuid(xfix)));

				// Finish converting half-baked CCWG stuff.
				if (halfBakedCcwgsForCurrentOldSegment.Count > 0)
				{
					var allOldXficGuids = (from xfix in xficsForCurrentOldSegment.Values
										   select GetGuid(xfix).ToLower()).ToList();
					foreach (var halfBakedCcwg in halfBakedCcwgsForCurrentOldSegment)
					{
						// Fix up halfbaked CCWG items here.
						var innerElement = halfBakedCcwg.Element("ConstChartWordGroup");
						// NB: The guids temporarily stored in the begin/end index props are for
						// the xfic guid, but we want to look up the index of its InstanceOf property,
						// which will be what is actually in the new "Analyses" prop of the new Segment.
						var guidAttr = innerElement.Element("BeginAnalysisIndex").Attribute("val");
						guidAttr.Value = allOldXficGuids.IndexOf(guidAttr.Value.ToLower()).ToString();
						guidAttr = innerElement.Element("EndAnalysisIndex").Attribute("val");
						guidAttr.Value = allOldXficGuids.IndexOf(guidAttr.Value.ToLower()).ToString();
						// NB: The recently created CCWG has already been added to dto repos,
						// so the only remaining task is to update the xml on the CCWG dto.
						var dto = dtoRepos.GetDTO(GetGuid(halfBakedCcwg));
						dto.Xml = halfBakedCcwg.ToString();
					}
				}

				// Find and convert any any old Text Tag indirect annotations for this segment.
				// NB: xficsForCurrentOldSegment.Values may have pfics,
				// and we don't want those here.
				// oldTextTags is all old TextTag annotations for everything.
				// This variable holds the twfics (in BeginOffset order),
				// where pfics are removed from xficsForCurrentOldSegment.
				var twficBeginOffsetListForCurrentSegment = new List<int>();
				var twficGuidListForCurrentSegment = new List<string>();
				var twficElementListForCurrentSegment = new List<byte[]>();
				var twficMap = new Dictionary<string, byte[]>();
				foreach (var twficKvp in from xficKvp in xficsForCurrentOldSegment
										 where GetAnnotationTypeGuid(xficKvp.Value) == DataMigrationServices.kTwficAnnDefnGuid
										 select xficKvp)
				{
					twficBeginOffsetListForCurrentSegment.Add(twficKvp.Key);
					var twficGuid = GetGuid(twficKvp.Value);
					twficGuidListForCurrentSegment.Add(twficGuid);
					twficElementListForCurrentSegment.Add(twficKvp.Value);
					twficMap.Add(twficGuid, twficKvp.Value);
				}

				var textTagElementsForCurrentSegment = new List<byte[]>();
				foreach (var oldTextTagAnnElement in oldTextTags)
				{
					// Find the ones that are used in the current segment,
					// and add them to textTagElementsForCurrentSegment.

					var appliesToGuids = new List<string>();
					foreach (var guid in GetAppliesToObjsurGuids(oldTextTagAnnElement))
					{
						if (twficGuidListForCurrentSegment.Contains(guid))
							appliesToGuids.Add(guid);
					}
					if (appliesToGuids.Count() <= 0)
						continue;

					// Store them for a while.
					textTagElementsForCurrentSegment.Add(oldTextTagAnnElement);

					// Get the index of the First twfic in appliesToGuids collection (which may hold pfics)
					// and the index of the Last twfic in appliesToGuids collection (which may hold pfics).
					var beginIdx = 0;
					for (var i = 0; i < appliesToGuids.Count(); ++i)
					{
						var currentXfixGuid = appliesToGuids[i].ToLower();
						byte[] currentTwficElement;
						if (!twficMap.TryGetValue(currentXfixGuid, out currentTwficElement))
							continue;

						beginIdx = xficsForCurrentOldSegment.IndexOfValue(currentTwficElement);
						break;
					}
					var endIdx = 0;
					for (var i = appliesToGuids.Count() - 1; i > -1; --i)
					{
						var currentXfixGuid = appliesToGuids[i].ToLower();
						byte[] currentTwficElement;
						if (!twficMap.TryGetValue(currentXfixGuid, out currentTwficElement))
							continue;

						endIdx = xficsForCurrentOldSegment.IndexOfValue(currentTwficElement);
						break;
					}

					var owningStText = dtoRepos.GetOwningDTO(paraDto);
					var newTextTagGuid = Guid.NewGuid().ToString().ToLower();
					var newTextTagElement = new XElement("rt",
						new XAttribute("class", "TextTag"),
						new XAttribute("guid", newTextTagGuid),
						new XAttribute("ownerguid", owningStText.Guid.ToLower()),
						new XElement("CmObject"),
						new XElement("TextTag",
							new XElement("BeginSegment", DataMigrationServices.CreateReferenceObjSurElement(newSegmentGuid)),
							new XElement("EndSegment", DataMigrationServices.CreateReferenceObjSurElement(newSegmentGuid)),
							new XElement("BeginAnalysisIndex", new XAttribute("val", beginIdx)),
							new XElement("EndAnalysisIndex", new XAttribute("val", endIdx)),
							new XElement("Tag", DataMigrationServices.CreateReferenceObjSurElement(GetInstanceOfGuid(oldTextTagAnnElement)))));

					// Add new DTO to repos.
					var newTextTagDto = new DomainObjectDTO(newTextTagGuid, "TextTag", newTextTagElement.ToString());
					dtoRepos.Add(newTextTagDto);
					// Add new TextTag to owning prop on owner as objsur element.
					var owningStTextElement = XElement.Parse(owningStText.Xml);
					var innerStTextElement = owningStTextElement.Element("StText");
					var tagsPropElement = innerStTextElement.Element("Tags");
					if (tagsPropElement == null)
					{
						tagsPropElement = new XElement("Tags");
						innerStTextElement.Add(tagsPropElement);
					}
					tagsPropElement.Add(DataMigrationServices.CreateOwningObjSurElement(newTextTagGuid));
					// Tell repos of the modification.
					owningStText.Xml = owningStTextElement.ToString();
					dtoRepos.Update(owningStText);
				}
				// Remove current text tags from input list
				foreach (var currentTextTagElement in textTagElementsForCurrentSegment)
					oldTextTags.Remove(currentTextTagElement);
			}
			//else
			//{
			//    // No xfics at all, so make sure para is set to be tokenized again.
			//    // Done globally for each Para in ProcessParagraphs
			//    //MarkParaAsNeedingTokenization(dtoRepos, paraDto, paraElement);
			//}

			return retval;
		}

		private static string GetGuid(XElement rootElement)
		{
			return rootElement.Attribute("guid").Value.ToLowerInvariant();
		}



		private static string GetGuid(byte[] xmlBytes, int idxObjsur, int idxEndObjsur)
		{
			var guidVal = GetAttributeValue(xmlBytes, s_guidAttr, idxObjsur, idxEndObjsur);
			if (guidVal != null)
				return guidVal.ToLowerInvariant();
			return null;
		}

		private static string GetGuid(byte[] xmlBytes)
		{
			var idxEndOfTag = xmlBytes.IndexOfSubArray(s_endXmlTag);
			return GetGuid(xmlBytes, 0, idxEndOfTag);
		}


		private static string GetClassValue(byte[] xmlBytes)
		{
			var idxEndOfTag = xmlBytes.IndexOfSubArray(s_endXmlTag);
			return GetAttributeValue(xmlBytes, s_classAttr, 0, idxEndOfTag);
		}

		private static string GetAttributeValue(byte[] xmlBytes, byte[] attrWithEquals, int idxStart, int idxEnd)
		{
			var idxAttr = xmlBytes.IndexOfSubArray(attrWithEquals, idxStart);
			if (idxAttr < 0 || idxAttr > idxEnd)
				return null;
			var cQuote = xmlBytes[idxAttr + attrWithEquals.Length];
			var idxMin = idxAttr + attrWithEquals.Length + 1;
			var idxLim = Array.IndexOf(xmlBytes, cQuote, idxMin);
			var length = idxLim - idxMin;
			return Encoding.UTF8.GetString(xmlBytes, idxMin, length);
		}

		internal static int GetBeginOffset(byte[] xmlBytes)
		{
			var baseBounds = new ElementBounds(xmlBytes, s_tagsCmBaseAnnotation);
			if (!baseBounds.IsValid)
				return 0;
			var offsetBounds = new ElementBounds(xmlBytes, s_tagsBeginOffset,
				baseBounds.BeginTagOffset, baseBounds.EndTagOffset);
			if (!offsetBounds.IsValid)
				return 0;
			var val = offsetBounds.GetAttributeValue(s_valAttr);
			return String.IsNullOrEmpty(val) ? 0 : int.Parse(val);
		}

		internal static int GetEndOffset(byte[] xmlBytes)
		{
			var baseBounds = new ElementBounds(xmlBytes, s_tagsCmBaseAnnotation);
			if (!baseBounds.IsValid)
				return 0;
			var offsetBounds = new ElementBounds(xmlBytes, s_tagsEndOffset,
				baseBounds.BeginTagOffset, baseBounds.EndTagOffset);
			if (!offsetBounds.IsValid)
				return 0;
			var val = offsetBounds.GetAttributeValue(s_valAttr);
			return String.IsNullOrEmpty(val) ? 0 : int.Parse(val);
		}

		private static string GetBeginObjectGuid(byte[] xmlBytes)
		{
			var baseBounds = new ElementBounds(xmlBytes, s_tagsCmBaseAnnotation);
			if (!baseBounds.IsValid)
				return null;
			var beginObjectBounds = new ElementBounds(xmlBytes, s_tagsBeginObject,
				baseBounds.BeginTagOffset, baseBounds.EndTagOffset);
			if (!beginObjectBounds.IsValid)
				return null;
			var objsurBounds = new ElementBounds(xmlBytes, s_tagsObjsur,
				beginObjectBounds.BeginTagOffset, beginObjectBounds.EndTagOffset);
			if (!objsurBounds.IsValid)
				return null;
			return GetGuid(xmlBytes, objsurBounds.BeginTagOffset, objsurBounds.EndTagOffset);
		}

		#endregion
	}

	internal class XficParser
	{

		#region Member Data

		private readonly string m_text;
		private readonly SortedList<int, byte[]> m_paraXfics;
		private int m_index;
		private int m_collectionStart;

		private FinderState m_mode;

		private readonly List<Tuple<int, int, bool>> m_neededXficForms;

		private enum FinderState
		{
			punctMode,
			whiteSpaceMode,
			wordFormingMode
		}

		#endregion

		internal XficParser(string text, SortedList<int, byte[]> paraXfics)
		{
			m_text = text;
			m_paraXfics = paraXfics;
			m_index = 0;
			m_collectionStart = -1;
			m_neededXficForms = new List<Tuple<int,int, bool>>();
			m_mode = FinderState.whiteSpaceMode;
		}

		internal void Run(out List<Tuple<int, int, bool>> xficsToBuild)
		{
			var clen = m_text.Length;
			while (m_index < clen)
			{
				switch (m_mode)
				{
					case FinderState.punctMode:
						if (CurrentCharIsWhitespace())
						{
							ExitPunctState();
							EnterWhiteSpaceState();
							continue;
						}
						if (CurrentCharIsAlphabetic())
						{
							ExitPunctState();
							EnterWficState();
							continue;
						}
						m_index++;
						break;
					case FinderState.whiteSpaceMode:
						if (CurrentCharIsWhitespace())
						{
							m_index++;
							continue;
						}
						if (CurrentCharIsAlphabetic())
						{
							EnterWficState();
							continue;
						}
						EnterPunctState();
						break;
					case FinderState.wordFormingMode:
						if (CurrentCharIsWhitespace())
						{
							ExitWficState();
							EnterWhiteSpaceState();
							continue;
						}
						if (CurrentCharIsAlphabetic())
						{
							m_index++;
							continue;
						}
						ExitWficState();
						EnterPunctState();
						break;
				}
			}
			if (m_mode == FinderState.punctMode)
				ExitPunctState();
			if (m_mode == FinderState.wordFormingMode)
				ExitWficState();
			xficsToBuild = m_neededXficForms;
		}

		private void ExitWficState()
		{
			if (m_collectionStart == -1)
				return; // we don't need to collect this one

			// 'True' means we need to create a temporary WFIC
			m_neededXficForms.Add(new Tuple<int, int, bool>(m_collectionStart, m_index, true));
		}

		private void EnterWficState()
		{
			m_mode = FinderState.wordFormingMode;

			// In the odd case where there might BE a wfic on file already...
			byte[] xficElement;
			if (m_paraXfics.TryGetValue(m_index, out xficElement))
			{
				var endOffset = DataMigration7000010.GetEndOffset(xficElement);
				var begOffset = DataMigration7000010.GetBeginOffset(xficElement);
				m_index += endOffset - begOffset;
				m_collectionStart = -1; // we found an existing xfic, don't collect this one
			}
			// ... there wasn't a wfic on file.
			else
			{
				m_collectionStart = m_index;
				m_index++;
			}
		}

		private void EnterWhiteSpaceState()
		{
			m_mode = FinderState.whiteSpaceMode;
			m_collectionStart = -1;
		}

		private bool CurrentCharIsAlphabetic()
		{
			return Icu.IsAlphabetic(m_text[m_index]);
		}

		private bool CurrentCharIsWhitespace()
		{
			return Icu.IsSpace(m_text[m_index]);
		}

		private void EnterPunctState()
		{
			m_mode = FinderState.punctMode;

			// In the odd case where there might BE a pfic on file already...
			byte[] xficElement;
			if (m_paraXfics.TryGetValue(m_index, out xficElement))
			{
				var endOffset = DataMigration7000010.GetEndOffset(xficElement);
				var begOffset = DataMigration7000010.GetBeginOffset(xficElement);
				m_index += endOffset - begOffset;
				m_collectionStart = -1; // we found an existing xfic, don't collect this one
			}
			// ... there wasn't a pfic on file.
			else
			{
				m_collectionStart = m_index;
				m_index++;
			}
		}

		private void ExitPunctState()
		{
			if (m_collectionStart == -1)
				return; // we don't need to collect this one

			// 'False' means we need to create a temporary PFIC
			m_neededXficForms.Add(new Tuple<int, int, bool>(m_collectionStart, m_index, false));
		}
	}

	/// <summary>
	/// A simple parameter object for getting data down to CreateTemporaryXfic()
	/// </summary>
	internal class ParagraphContext
	{
		public string ParaGuid { get; private set; }
		public SortedList<int, byte[]> ParaXfics { get; private set; }

		public ParagraphContext(string paraGuid, SortedList<int, byte[]> paraXfics)
		{
			ParaGuid = paraGuid;
			ParaXfics = paraXfics;
		}
	}
}
