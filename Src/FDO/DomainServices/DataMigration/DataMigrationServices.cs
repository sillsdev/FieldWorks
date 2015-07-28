// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Class that performs services for data migrations.
	/// </summary>
	internal static class DataMigrationServices
	{
		// These guid strings are the fixed guids for various now-obsolete annotation defns.
		// The old constants, not being available, have to be preserved in the DM system,
		// so this is where they continue to live.
		internal const string kSegmentAnnDefnGuid = "b63f0702-32f7-4abb-b005-c1d2265636ad";
		internal const string kTwficAnnDefnGuid = "eb92e50f-ba96-4d1d-b632-057b5c274132";
		internal const string kPficAnnDefnGuid = "cfecb1fe-037a-452d-a35b-59e06d15f4df";
		internal const string kFreeTransAnnDefnGuid = "9ac9637a-56b9-4f05-a0e1-4243fbfb57db";
		internal const string kLitTransAnnDefnGuid = "b0b1bb21-724d-470a-be94-3d9a436008b8";
		internal const string kNoteAnnDefnGuid = "7ffc4eab-856a-43cc-bc11-0db55738c15b"; // Constant *not* removed. And, it ought not be removed either.
		internal const string kTextTagAnnDefnGuid = "084a3afe-0d00-41da-bfcf-5d8deafa0296";
		internal const string kDiscourseChartComponentAnnDefnGuid = "a39a1272-38a0-4354-bdac-8636d64c1eec";
		internal const string kConstituentChartAnnotationAnnDefnGuid = "ec0a4dad-7e90-4e73-901a-21d25f0692e3";
		internal const string kConstituentChartRowAnnDefnGuid = "50c1a53d-925d-4f55-8ed7-64a297905346";

		/// <summary>
		/// Check the current verison number in the Repository against the given version number.
		/// </summary>
		internal static void CheckVersionNumber(IDomainObjectDTORepository dtoRepos, int expectedStartingModelVersionNumber)
		{
			if (dtoRepos == null) throw new ArgumentNullException("dtoRepos");
			if (dtoRepos.CurrentModelVersion != expectedStartingModelVersionNumber)
				throw new DataMigrationException("Wrong version number to increment from.");
		}

		/// <summary>
		/// Increment the model version number for the repository.
		/// </summary>
		internal static void IncrementVersionNumber(IDomainObjectDTORepository dtoRepos)
		{
			if (dtoRepos == null) throw new ArgumentNullException("dtoRepos");

			dtoRepos.CurrentModelVersion = dtoRepos.CurrentModelVersion + 1;
		}

		/// <summary>
		/// Rest the xml in the DTO and register the DTO as udated with the repository.
		/// Use this overload only if the class name is NOT changing.
		/// </summary>
		/// <remarks>
		/// There is no validation of the xml, other than making sure it is not null,
		/// or an emty string.
		/// </remarks>
		internal static void UpdateDTO(IDomainObjectDTORepository dtoRepos,
			DomainObjectDTO dirtball, string newXmlValue)
		{
			if (dtoRepos == null) throw new ArgumentNullException("dtoRepos");
			if (dirtball == null) throw new ArgumentNullException("dirtball");
			if (String.IsNullOrEmpty(newXmlValue)) throw new ArgumentNullException("newXmlValue");

			dirtball.Xml = newXmlValue;
			dtoRepos.Update(dirtball);
		}

		/// <summary>
		/// Rest the xml in the DTO and register the DTO as udated with the repository.
		/// Use this overload only if the class name is NOT changing.
		/// </summary>
		/// <remarks>
		/// There is no validation of the xml, other than making sure it is not null,
		/// or an emty string.
		/// </remarks>
		internal static void UpdateDTO(IDomainObjectDTORepository dtoRepos,
			DomainObjectDTO dirtball, byte[] newXmlBytes)
		{
			if (dtoRepos == null) throw new ArgumentNullException("dtoRepos");
			if (dirtball == null) throw new ArgumentNullException("dirtball");
			if (newXmlBytes == null || newXmlBytes.Length == 0) throw new ArgumentNullException("newXmlBytes");

			dirtball.XmlBytes = newXmlBytes;
			dtoRepos.Update(dirtball);
		}

		/// <summary>
		/// Reset the xml in the DTO and register the DTO as updated with the repository.
		/// Use this overload if the class name is changing.
		/// </summary>
		/// <remarks>
		/// There is no validation of the xml, other than making sure it is not null,
		/// or an emty string.
		/// </remarks>
		internal static void UpdateDTO(IDomainObjectDTORepository dtoRepos,
			DomainObjectDTO dirtball, string newXmlValue, string oldClassName)
		{
			dtoRepos.ChangeClass(dirtball, oldClassName);
			UpdateDTO(dtoRepos, dirtball, newXmlValue);
		}

		/// <summary>
		/// Remove <paramref name="goner"/> and everything it owns.
		/// Be sure to include removing goner from its optional owning property.
		/// </summary>
		internal static void RemoveIncludingOwnedObjects(IDomainObjectDTORepository dtoRepos, DomainObjectDTO goner, bool removeFromOwner)
		{
			DomainObjectDTO gonerActual;
			if (!dtoRepos.TryGetValue(goner.Guid, out gonerActual))
				return; // Not in repos.

			if (removeFromOwner)
			{
				var ownerDto = dtoRepos.GetOwningDTO(goner);
				if (ownerDto != null)
				{
					var ownerElement = XElement.Parse(ownerDto.Xml);
					var ownObjSurElement = (from objSurNode in ownerElement.Descendants("objsur")
											where objSurNode.Attribute("t").Value == "o" && objSurNode.Attribute("guid").Value.ToLower() == goner.Guid.ToLower()
											select objSurNode).FirstOrDefault(); // Ought not be null, but play it safe.
					if (ownObjSurElement != null)
						ownObjSurElement.Remove();

					if (!RemoveEmptyPropertyElements(dtoRepos, ownerDto, ownerElement))
					{
						// No empty property elements removed, so we have to do the update.
						UpdateDTO(dtoRepos, ownerDto, ownerElement.ToString());
					}
				}
			}

			foreach (var ownedDto in dtoRepos.GetDirectlyOwnedDTOs(goner.Guid))
				RemoveIncludingOwnedObjects(dtoRepos, ownedDto, false);

			dtoRepos.Remove(goner);
		}

		/// <summary>
		/// Remove a number of objects with a common owner, and everything they own.
		/// </summary>
		internal static void RemoveMultipleIncludingOwnedObjects(IDomainObjectDTORepository dtoRepos,
			List<DomainObjectDTO> goners, DomainObjectDTO ownerDto)
		{
			if (ownerDto != null)
			{
				var ownerElement = XElement.Parse(ownerDto.Xml);
				foreach (var goner in goners)
				{
					var goner1 = goner;
					var ownObjSurElement = (from objSurNode in ownerElement.Descendants("objsur")
											where
												objSurNode.Attribute("t").Value == "o" &&
												objSurNode.Attribute("guid").Value.ToLower() == goner1.Guid.ToLower()
											select objSurNode).FirstOrDefault(); // Ought not be null, but play it safe.
					if (ownObjSurElement != null)
						ownObjSurElement.Remove();
				}
				if (!RemoveEmptyPropertyElements(dtoRepos, ownerDto, ownerElement))
				{
					// No empty property elememtns removed, so we have to do the update.
					UpdateDTO(dtoRepos, ownerDto, ownerElement.ToString());
				}
			}
			foreach (var goner in goners)
			{
				foreach (var ownedDto in dtoRepos.GetDirectlyOwnedDTOs(goner.Guid))
					RemoveIncludingOwnedObjects(dtoRepos, ownedDto, false);
				dtoRepos.Remove(goner);
			}
		}

		/// <summary>
		///	1. Remove objects (zombies) that:
		///		A. claim to have owners, but the owners do not exist, or
		///		B. owners don't know they own it, or
		///		C. objects with no owners that are not supported as allowing no owners.
		///	2. Remove 'dangling' references to objects that no longer exist.
		/// 3. Remove properties that have no attributes or content.
		/// </summary>
		internal static void Delint(IDomainObjectDTORepository dtoRepos)
		{
			// Remove zombies.
			// By removing zombies first, any references to them will be 'dangling',
			// so can be removed in the following step.
			var allInstancesWithValidClasses = dtoRepos.AllInstancesWithValidClasses().ToList();
			RemoveZombies(dtoRepos, allInstancesWithValidClasses);
			// Ask again, since zombies will have been removed.
			allInstancesWithValidClasses = dtoRepos.AllInstancesWithValidClasses().ToList();
			RemoveDanglingReferences(dtoRepos, allInstancesWithValidClasses);
			// Get rid of all property elements that are empty,
			// since the xml loader code isn't happy with certain empty elements.
			RemoveEmptyPropertyElements(dtoRepos, allInstancesWithValidClasses);
		}

		private static void RemoveZombies(
			IDomainObjectDTORepository dtoRepos,
			IList<DomainObjectDTO> allDtos)
		{
			var count = allDtos.Count;
			var legalOwnerlessClasses = new HashSet<string>
				{
					// Start at 7.0
					"LangProject", // Started as no owner allowed.
					"ScrRefSystem", // Started as no owner allowed.
					"CmPossibilityList", // Started as required owner. // Optionally unowed 7000010. Required owner re-added and removed again by 7000020.
					"CmPicture", // Started as no owner allowed. Optionally unowed by 7000019
					//"UserView", // Started as no owner allowed. Removed in 7000031
					//"LgWritingSystem", // Started as no owner allowed. Removed in 7000019
					"WfiWordform", // none 7000001
					"PunctuationForm", // Added to model in 7000010. Added 'none' between 7000010 and 7000011
					"LexEntry", // 7000028
					// Initial release: 7.0.6 at DM 7000037

					//"VirtualOrdering", // 7000040 (added class in 7000038. Added 'none' with no model change number between 39 and 40).
					// Release 7.1.1 at DM 7000044
					// Release 7.2.x at DM 7000051 (51 added in 7.2 branch.)
					// Release 7.3.x at DM 70000xx
				};
			if (dtoRepos.CurrentModelVersion >= 7000040)
				legalOwnerlessClasses.Add("VirtualOrdering");
			if (dtoRepos.CurrentModelVersion >= 7000059)
				legalOwnerlessClasses.Add("Text");


			var goners = new List<DomainObjectDTO>(count);
			// Key is guid of owner. Value is set of guids it owns.
			// In one very large project that ran out of memory, it had 1281871 dtos, and
			// 115694 of them owned more than one other dto.  So we'll guess that 1/10th
			// of the total count is a reasonable estimate for the capacity of ownerMap.
			var ownerMap = new Dictionary<DomainObjectDTO, HashSet<string>>(count/10);
			foreach (var currentDto in allDtos)
			{
				DomainObjectDTO owningDto;
				if (dtoRepos.TryGetOwner(currentDto.Guid, out owningDto))
				{
					if (owningDto == null)
					{
						if (dtoRepos.CurrentModelVersion >= 7000060 && !legalOwnerlessClasses.Contains(currentDto.Classname))
							goners.Add(currentDto); // Not allowed to be unowned, so zap it.
						continue;
					}

					// Has owner, but does owner know that it owns it?
					HashSet<string> ownees;
					if (!ownerMap.TryGetValue(owningDto, out ownees))
					{
						ownees = GetOwnees(owningDto.XmlBytes);
						// Cache it only if it's really useful to do so.
						if (ownees.Count > 2)
							ownerMap[owningDto] = ownees;
					}
					if (ownees.Contains(currentDto.Guid.ToLowerInvariant()))
						continue;
					// Current dto  is a zombie, so remove it, and everything it owns.
					goners.Add(currentDto);
				}
				else
				{
					// Current dto  is a zombie, so remove it, and everything it owns.
					goners.Add(currentDto);
				}
			}
			ownerMap.Clear();
			foreach (var goner in goners)
			{
				RemoveIncludingOwnedObjects(dtoRepos, goner, false);
			}
		}

		private static readonly ElementTags s_tagsObjsur = new ElementTags("<objsur ", ">");
		private static readonly byte[] s_guidAttr = Encoding.UTF8.GetBytes(" guid=");
		private static readonly byte[] s_tAttr = Encoding.UTF8.GetBytes(" t=");
		private static HashSet<string> GetOwnees(byte[] xmlBytes)
		{
			var ownees = new HashSet<string>();
			var ichEnd = xmlBytes.Length;
			var objsurBounds = new ElementBounds(xmlBytes, s_tagsObjsur);
			while (objsurBounds.IsValid)
			{
				var type = objsurBounds.GetAttributeValue(s_tAttr);
				if (type == "o")
				{
					var guid = objsurBounds.GetAttributeValue(s_guidAttr);
					if (!String.IsNullOrEmpty(guid))
						ownees.Add(guid.ToLowerInvariant());
				}
				objsurBounds.Reset(objsurBounds.EndTagOffset, ichEnd);
			}
			return ownees;
		}

		private static void RemoveDanglingReferences(
			IDomainObjectDTORepository dtoRepos,
			IEnumerable<DomainObjectDTO> allDtos)
		{
			foreach (var currentDto in allDtos)
			{
				 // Fetch the referred (regular reference or owning reference) to object guids.
			   var referredToGuids = ExtractReferencedObjects(currentDto);

				// See if it is a dangling ref, where target object has been deleted.
				foreach (var targetGuid in referredToGuids)
				{
					DomainObjectDTO referencedDto;
					if (dtoRepos.TryGetValue(targetGuid, out referencedDto))
						continue;

					// targetGuid is a dangling reference.
					// Remove the <objsur> element from referring object (kvp Key).
					// The <objsur> will have its 'guid' attribute set to 'targetGuid'.
					// This will work for owned as well as standard referenced objects.
					var targetGuidAsString = targetGuid.ToLower();
					var referringDto = dtoRepos.GetDTO(currentDto.Guid);
					var referringElement = XElement.Parse(referringDto.Xml);
					var emptyPropElements = new List<XElement>();
					foreach (var danglingRef in (referringElement.Descendants("objsur").Where(
						objSurrElement => objSurrElement.Attribute("guid").Value.ToLower() == targetGuidAsString)).ToList())
					{
						var propElement = danglingRef.Parent;
						danglingRef.Remove();
						if (!propElement.HasAttributes && !propElement.HasElements)
							emptyPropElements.Add(propElement);
					}
					foreach (var emptyPropElement in emptyPropElements)
						emptyPropElement.Remove(); // Remove now empty property element.
					// Reset the xml.
					referringDto.Xml = referringElement.ToString();
					dtoRepos.Update(referringDto);
				}
			}
		}

		private static void RemoveEmptyPropertyElements(
			IDomainObjectDTORepository dtoRepos,
			IEnumerable<DomainObjectDTO> allInstancesWithValidClasses)
		{
			foreach (var currentDto in allInstancesWithValidClasses)
			{
				RemoveEmptyPropertyElements(dtoRepos, currentDto, XElement.Parse(currentDto.Xml));
			}
		}

		private static bool RemoveEmptyPropertyElements(IDomainObjectDTORepository dtoRepos, DomainObjectDTO currentDto, XContainer rtElement)
		{
			var propertyElements = (rtElement.Element("CmObject") != null)
											? rtElement.Elements().Elements() // Two levels for old stuff before DM15
											: rtElement.Elements();
			// ToArray is required or Remove will end loop early.
			var emptyPropertyElements = (propertyElements.Where(propertyElement => !propertyElement.HasAttributes && !propertyElement.HasElements)).ToArray();
			foreach (var emptyPropertyElement in emptyPropertyElements)
			{
				emptyPropertyElement.Remove();
			}
			// Notify of update, if it changed.
			var results = false;
			if (emptyPropertyElements.Any())
			{
				UpdateDTO(dtoRepos, currentDto, rtElement.ToString());
				results = true;
			}
			return results;
		}

		/// <summary>
		/// Does two components of changing the class of a DTO representation of the object:
		/// Fixes the class attribute, and fixes the Classname of the DTO.
		/// Caller should arrange to move it from one list to another, and fix the
		/// embedded elements (see e.g. ChangeToSubClass).
		/// </summary>
		/// <param name="target"></param>
		/// <param name="oldClass"></param>
		/// <param name="newClass"></param>
		private static void ChangeClass(DomainObjectDTO target, string oldClass, string newClass)
		{
			// If there's no unexpected white space we can do this efficiently.
			// This depends (like various other code) on NOT having unexpected white space around the '='.
			byte[] classBytes = Encoding.UTF8.GetBytes("class=\"" + oldClass + "\"");
			int index = target.XmlBytes.IndexOfSubArray(classBytes);
			byte[] newClassBytes = Encoding.UTF8.GetBytes("class=\"" + newClass + "\"");
			target.XmlBytes = target.XmlBytes.ReplaceSubArray(index, classBytes.Length, newClassBytes);
			target.Classname = newClass;
		}

		private static readonly byte[] ClosingRt = Encoding.UTF8.GetBytes("</rt>");

		/// <summary>
		/// Change class of object to a new subclass of the original class.
		/// Caller still needs to move it from one collection to another in the repository.
		/// </summary>
		internal static void ChangeToSubClass(DomainObjectDTO target, string oldClass, string newClass)
		{
			ChangeClass(target, oldClass, newClass);
			// Need to fill in the new empty element. It will be right before the closing <\rt>.
			byte[] input = target.XmlBytes;
			int index = input.Length - ClosingRt.Length;
			for (int i = 0; i < ClosingRt.Length; i++)
				if (input[i + index] != ClosingRt[i])
				{
					index = input.IndexOfSubArray(ClosingRt);
				}
			byte[] insertBytes = Encoding.UTF8.GetBytes("<" + newClass + "/>");
			target.XmlBytes = input.ReplaceSubArray(index, 0, insertBytes);
		}

		private static string[] ExtractReferencedObjects(DomainObjectDTO dto)
		{
			var rootElement = XElement.Parse(dto.Xml);

			return (from objSurElement in rootElement.Descendants("objsur") select objSurElement.Attribute("guid").Value).ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the <paramref name="referencedObjects"/> forward reference data,
		/// extract related back reference information
		/// </summary>
		/// <remarks>
		/// This method does not worry about ownership as a reference property,
		/// even though formally ownership is a special kind of reference.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private static Dictionary<Guid, List<Guid>> GetBackReferences(
			IEnumerable<KeyValuePair<Guid, Dictionary<string, List<Guid>>>> referencedObjects)
		{
			var backReferences = new Dictionary<Guid, List<Guid>>();
			// kvp key is the referencing object
			// kvp "r" is list of objects 'key' refers to.
			foreach (var kvp in referencedObjects)
			{
				// Fetch the referring object guids.
				foreach (var targetGuid in kvp.Value["r"].ToArray())
				{
					List<Guid> referees;
					if (!backReferences.TryGetValue(targetGuid, out referees))
					{
						referees = new List<Guid>();
						backReferences.Add(targetGuid, referees);
					}
					referees.Add(kvp.Key);
				}
			}
			return backReferences;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the <paramref name="guid"/>, return a new 'objsur' XElement.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static XElement CreateOwningObjSurElement(string guid)
		{
			return CreateObjSurElement(guid, ObjSurType.Owning);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the <paramref name="guid"/>, return a new 'objsur' XElement.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static XElement CreateReferenceObjSurElement(string guid)
		{
			return CreateObjSurElement(guid, ObjSurType.Reference);
		}

		private static XElement CreateObjSurElement(string guid, ObjSurType type)
		{
			new Guid(guid); // Will throw if 'guid' is not legal.
			return new XElement("objsur",
				new XAttribute("t", type == ObjSurType.Owning ? "o" : "r"),
				new XAttribute("guid", guid));
		}

		private enum ObjSurType
		{
			Owning,
			Reference
		}

		/// <summary>
		/// Changes the GUID of the specified DTO. It updates the owner and all specified referrers to point to the new GUID.
		/// </summary>
		/// <param name="dtoRepos">The dto repos.</param>
		/// <param name="dto">The dto.</param>
		/// <param name="newGuid">The new GUID.</param>
		/// <param name="possibleReferrers">The possible referrers.</param>
		internal static void ChangeGuid(IDomainObjectDTORepository dtoRepos, DomainObjectDTO dto, string newGuid,
			IEnumerable<DomainObjectDTO> possibleReferrers)
		{
			// if the DTO already has the new GUID, don't do anything
			if (dto.Guid.ToLowerInvariant() == newGuid.ToLowerInvariant())
				return;

			XElement rtElem = XElement.Parse(dto.Xml);
			rtElem.Attribute("guid").Value = newGuid;
			dtoRepos.Add(new DomainObjectDTO(newGuid, dto.Classname, rtElem.ToString()));
			foreach (DomainObjectDTO ownedDto in dtoRepos.GetDirectlyOwnedDTOs(dto.Guid))
			{
				XElement ownedElem = XElement.Parse(ownedDto.Xml);
				ownedElem.Attribute("ownerguid").Value = newGuid;
				UpdateDTO(dtoRepos, ownedDto, ownedElem.ToString());
			}

			var ownerDto = dtoRepos.GetOwningDTO(dto);
			if (ownerDto != null)
				UpdateObjSurElement(dtoRepos, ownerDto, dto.Guid, newGuid);

			if (possibleReferrers != null)
			{
				foreach (DomainObjectDTO referrer in possibleReferrers)
					UpdateObjSurElement(dtoRepos, referrer, dto.Guid, newGuid);
			}
			dtoRepos.Remove(dto);
		}

		private static void UpdateObjSurElement(IDomainObjectDTORepository dtoRepos, DomainObjectDTO dto, string oldGuid, string newGuid)
		{
			var rtElem = XElement.Parse(dto.Xml);
			var ownObjSurGuidAttr = (from objSurNode in rtElem.Descendants("objsur")
									 where objSurNode.Attribute("guid").Value.ToLower() == oldGuid.ToLower()
									 select objSurNode.Attribute("guid")).FirstOrDefault(); // Ought not be null, but play it safe.
			if (ownObjSurGuidAttr != null)
				ownObjSurGuidAttr.Value = newGuid;
			UpdateDTO(dtoRepos, dto, rtElem.ToString());
		}
	}

	/// <summary>
	/// This class stores the begin and end tags for an XML element as byte arrays.
	/// This is useful in conjuction with the ElementBounds class, which is useful
	/// to avoid creating XElement objects and thus consuming time and memory.
	/// </summary>
	internal class ElementTags
	{
		public byte[] BeginTag { get; private set; }
		public byte[] EndTag { get; private set; }

		internal ElementTags(string start, string finish)
		{
			BeginTag = Encoding.UTF8.GetBytes(start);
			EndTag = Encoding.UTF8.GetBytes(finish);
		}

		internal ElementTags(string name)
		{
			BeginTag = Encoding.UTF8.GetBytes(String.Format("<{0}>", name));
			EndTag = Encoding.UTF8.GetBytes(String.Format("</{0}>", name));
		}
	}

	/// <summary>
	/// This class stores some bounds of an XML element stored inside a byte array.
	/// </summary>
	internal class ElementBounds
	{
		private readonly byte[] m_xmlBytes;
		private readonly ElementTags m_tags;
		/// <summary>
		/// Index where the start tag begins.
		/// </summary>
		public int BeginTagOffset { get; private set; }
		/// <summary>
		/// Index of the '>' that terminates the start tag.
		/// </summary>
		public int EndOfStartTag { get; private set; }
		/// <summary>
		/// Index where the end tag begins (or possibly where the start tag ends for
		/// elements with attributes and no content).
		/// </summary>
		public int EndTagOffset { get; private set; }
		/// <summary>
		/// Number of bytes from the beginning of the start tag to the end of the end tag.
		/// </summary>
		public int Length { get; private set; }
		/// <summary>
		/// Whether the element boundaries are valid (and thus the element does exist).
		/// </summary>
		public bool IsValid
		{
			get { return BeginTagOffset >= 0 && EndOfStartTag > BeginTagOffset && EndTagOffset > BeginTagOffset; }
		}

		private static readonly byte[] s_endXmlTag = Encoding.UTF8.GetBytes(">");

		/// <summary>
		/// Constructor for an element to be found anywhere inside the byte array.
		/// </summary>
		public ElementBounds(byte[] xmlBytes, ElementTags tags)
		{
			m_xmlBytes = xmlBytes;
			m_tags = tags;
			BeginTagOffset = xmlBytes.IndexOfSubArray(tags.BeginTag);
			if (BeginTagOffset >= 0)
			{
				EndOfStartTag = xmlBytes.IndexOfSubArray(s_endXmlTag, BeginTagOffset);
				EndTagOffset = xmlBytes.IndexOfSubArray(tags.EndTag, BeginTagOffset + tags.BeginTag.Length);
			}
			else
			{
				EndOfStartTag = -1;
				EndTagOffset = -1;
			}
			SetLength(tags.EndTag.Length);
		}

		private void SetLength(int endtagLength)
		{
			if (IsValid)
				Length = (EndTagOffset + endtagLength) - BeginTagOffset;
			else
				Length = 0;
		}

		public ElementBounds(byte[] xmlBytes, ElementTags tags, int ichMin, int ichLim)
		{
			m_xmlBytes = xmlBytes;
			m_tags = tags;
			Reset(ichMin, ichLim);
		}

		/// <summary>
		/// Constructor for an element to be found inside another element that was previously
		/// located in the byte array.
		/// </summary>
		public ElementBounds(byte[] xmlBytes, ElementTags tags, ElementBounds bounds)
		{
			m_xmlBytes = xmlBytes;
			m_tags = tags;
			Reset(bounds.BeginTagOffset, bounds.EndTagOffset);
		}

		public void Reset(int ichMin, int ichLim)
		{
			if (ichMin < 0 || ichLim < 0)
			{
				BeginTagOffset = -1;
				EndOfStartTag = -1;
				EndTagOffset = -1;
			}
			else
			{
				BeginTagOffset = m_xmlBytes.IndexOfSubArray(m_tags.BeginTag, ichMin);
				if (BeginTagOffset >= 0 && BeginTagOffset < ichLim)
				{
					EndOfStartTag = m_xmlBytes.IndexOfSubArray(s_endXmlTag, BeginTagOffset);
					EndTagOffset = m_xmlBytes.IndexOfSubArray(m_tags.EndTag, BeginTagOffset + m_tags.BeginTag.Length);
					if (EndTagOffset >= ichLim)
						EndTagOffset = -1;
				}
				else
				{
					EndOfStartTag = -1;
					EndTagOffset = -1;
				}
			}
			SetLength(m_tags.EndTag.Length);
		}

		/// <summary>
		/// Get the value of the indicated attribute, or return null.
		/// </summary>
		/// <param name="attrWithEquals">The attribute name with a leading space and a trailing =.</param>
		/// <returns></returns>
		public string GetAttributeValue(byte[] attrWithEquals)
		{
			var idxAttr = m_xmlBytes.IndexOfSubArray(attrWithEquals, BeginTagOffset);
			if (idxAttr < 0 || idxAttr > EndOfStartTag)
				return null;
			var cQuote = m_xmlBytes[idxAttr + attrWithEquals.Length];
			var idxMin = idxAttr + attrWithEquals.Length + 1;
			var idxLim = Array.IndexOf(m_xmlBytes, cQuote, idxMin);
			var length = idxLim - idxMin;
			return Encoding.UTF8.GetString(m_xmlBytes, idxMin, length);
		}
	}
}