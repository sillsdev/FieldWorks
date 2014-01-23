// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwDataFixer.cs
// Responsibility: mcconnel

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.FixData
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix errors in a FieldWorks data XML file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwDataFixer
	{
		string m_filename;
		IProgress m_progress;
		int m_crt;

		public delegate void ErrorLogger(string guid, string date, string description);

		private ErrorLogger errorLogger;

		List<RtFixer> m_rtLevelFixers = new List<RtFixer>();

		Dictionary<Guid, Guid> m_owners = new Dictionary<Guid, Guid>();
		HashSet<Guid> m_guids = new HashSet<Guid>();
		List<XElement> m_dupGuidElements = new List<XElement>();
		List<XElement> m_dupOwnedElements = new List<XElement>();

		//This structure is used for the deletion of rt elements
		Dictionary<string, HashSet<string>> m_parentToOwnedObjsur = new Dictionary<string, HashSet<string>>();
		private HashSet<string> m_rtElementsToDelete = new HashSet<string>();

		/// <summary>
		/// Constructor.  Reads the file and stores any data needed for corrections later on.
		/// </summary>
		public FwDataFixer(string filename, IProgress progress, ErrorLogger logger)
		{
			m_filename = filename;
			m_progress = progress;
			errorLogger = logger;

			m_progress.Minimum = 0;
			m_progress.Maximum = 1000;
			m_progress.Position = 0;
			m_progress.Message = String.Format(Strings.ksReadingTheInputFile, m_filename);
			m_crt = 0;
			// The following fixers will be run on each rt element during FixErrorsAndSave()
			// Note: every change to the file MUST log an error. This is used in FixFwData to set a return code indicating whether anything changed.
			// This in turn is used in Send/Receive to determine whether we need to re-split the file before committing.
			// N.B.: Order is important here!!!!!!!
			m_rtLevelFixers.Add(new OriginalFixer());
			m_rtLevelFixers.Add(new CustomPropertyFixer());
			m_rtLevelFixers.Add(new BasicCustomPropertyFixer());
			var senseFixer = new GrammaticalSenseFixer();
			m_rtLevelFixers.Add(senseFixer);
			m_rtLevelFixers.Add(new MorphBundleFixer(senseFixer)); // after we've possibly removed MSAs in GrammaticalSenseFixer
			m_rtLevelFixers.Add(new SequenceFixer());
			m_rtLevelFixers.Add(new HomographFixer());
			m_rtLevelFixers.Add(new DuplicateWordformFixer());
			m_rtLevelFixers.Add(new CustomListNameFixer());
			using (XmlReader xrdr = XmlReader.Create(m_filename))
			{
				xrdr.MoveToContent();
				if (xrdr.Name != "languageproject")
					throw new Exception(String.Format("Unexpected outer element (expected <Lists>): {0}", xrdr.Name));
				xrdr.Read();
				xrdr.MoveToContent();
				if (xrdr.Name == "AdditionalFields")
				{
					string customsNode = xrdr.ReadOuterXml();
					XElement additionalFieldsElem = XElement.Parse(customsNode);
					// Give each fixer a chance to gather data on defined custom fields.
					foreach (var fixer in m_rtLevelFixers)
						fixer.InspectAdditionalFieldsElement(additionalFieldsElem);
					xrdr.ReadToFollowing("rt");
				}
				while (xrdr.Name == "rt")
				{
					string rtXml = xrdr.ReadOuterXml();
					XElement rt = XElement.Parse(rtXml);
					StoreGuidInfoAndOwnership(rt, errorLogger);
					// Give each fixer a chance to gather data on the first pass,
					// if it needs two passes to fix its sort of problem.
					foreach (var fixer in m_rtLevelFixers)
						fixer.InspectElement(rt);
					xrdr.MoveToContent();
					++m_crt;
					if (m_progress.Position == m_progress.Maximum)
						m_progress.Position = 0;
					if ((m_crt % 1000) == 0)
						m_progress.Step(1);
				}
				foreach (var fixer in m_rtLevelFixers)
					fixer.FinalFixerInitialization(m_owners, m_guids, m_parentToOwnedObjsur, m_rtElementsToDelete);
				xrdr.Close();
			}
		}

		/// <summary>
		/// Fix any errors you can, and write the results out.  If successful, the input
		/// file is renamed with a ".bak" extension, the output file is renamed to the
		/// original input filename, and a log file with a ".fixes" extension is written.
		/// </summary>
		public void FixErrorsAndSave()
		{
			m_progress.Maximum = m_crt;
			m_progress.Position = 0;
			m_progress.Message = String.Format(Strings.ksLookingForAndFixingErrors, m_filename);
			string outfile = m_filename + "-x";
			XmlWriterSettings settings = new XmlWriterSettings {Indent = true, IndentChars = String.Empty};
			using (XmlWriter xw = XmlWriter.Create(outfile, settings))
			{
				xw.WriteStartDocument();

				using (XmlReader xrdr = XmlReader.Create(m_filename))
				{
					xrdr.MoveToContent();
					if (xrdr.Name != "languageproject")
						throw new Exception(String.Format("Unexpected outer element (expected <Lists>): {0}", xrdr.Name));
					xw.WriteStartElement("languageproject");
					xw.WriteAttributes(xrdr, false);
					xrdr.Read();
					xrdr.MoveToContent();
					if (xrdr.Name == "AdditionalFields")
					{
						string sXml = xrdr.ReadOuterXml();
						var xe = XElement.Parse(sXml);
						xe.WriteTo(xw);
						xrdr.MoveToContent();
					}
					while (xrdr.Name == "rt")
					{
						var rtXml = xrdr.ReadOuterXml();
						var rt = XElement.Parse(rtXml);
						// set flag to false if we don't want to write out this rt element, i.e. delete it!
						// N.B.: Any deleting of owned objects requires two passes, so that the reference
						// to the object being deleted can be cleaned up too!
						var guid = rt.Attribute("guid").Value;
						if (!m_rtElementsToDelete.Contains(guid))
						{
							var fwrite = true;
							foreach (var fixer in m_rtLevelFixers)
							{
								if (!fixer.FixElement(rt, errorLogger))
									fwrite = false;
							}
							if (fwrite)
								rt.WriteTo(xw);
						}
						else
						{
							var className = rt.Attribute("class").Value;
							var errorMessage = String.Format(Strings.ksUnusedRtElement, className, guid);
							errorLogger(guid, DateTime.Now.ToShortDateString(), errorMessage);
						}
						xrdr.MoveToContent();
						m_progress.Step(1);
					}
					xrdr.Close();
				}
				xw.WriteEndDocument();
				xw.Close();
			}

			var bakfile = Path.ChangeExtension(m_filename, FdoFileHelper.ksFwDataFallbackFileExtension);
			if (File.Exists(bakfile))
				File.Delete(bakfile);
			File.Move(m_filename, bakfile);
			File.Move(outfile, m_filename);
		}

		/// <summary>
		/// This class contains the adapted code for the original FwDataFixer which tried to handle a small set
		/// of reference and writing system problems.
		/// It attempts to repair dangling links, duplicate writing systems, and incorrectly formatted dates
		/// It also identifies items with duplicate guids, but does not attempt to repair them.
		/// </summary>
		internal class OriginalFixer : RtFixer
		{
			static List<XElement> m_danglingLinks = new List<XElement>();

			/// <summary>
			/// Do any fixes to this particular root element here.
			/// Return true if we are done fixing this element and can write it out.
			/// Return false if we need to delete this root element.
			/// </summary>
			/// <param name="rt"></param>
			/// <param name="errorLogger"></param>
			/// <returns></returns>
			internal override bool FixElement(XElement rt, ErrorLogger errorLogger)
			{
				Guid guid = new Guid(rt.Attribute("guid").Value);
				Guid storedOwner;
				if (!m_owners.TryGetValue(guid, out storedOwner))
					storedOwner = Guid.Empty;
				var xaClass = rt.Attribute("class");
				var className = xaClass == null ? "<unknown>" : xaClass.Value;
				XAttribute xaOwner = rt.Attribute("ownerguid");
				if (xaOwner != null)
				{
					Guid guidOwner = new Guid(xaOwner.Value);
					if (guidOwner != storedOwner)
					{
						if (!storedOwner.ToString().Equals(Guid.Empty.ToString()) && m_guids.Contains(storedOwner))
						{
							errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksChangingOwnerGuidValue,
								guidOwner, storedOwner, className, guid));
							xaOwner.Value = storedOwner.ToString();
						}
						else if (!m_guids.Contains(guidOwner))
						{
							errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingLinkToNonexistentOwner,
								guidOwner, className, guid));
							xaOwner.Remove();
						}
					}
				}
				else if (storedOwner != Guid.Empty)
				{
					errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksAddingLinkToOwner,
						storedOwner, className, guid));
					xaOwner = new XAttribute("ownerguid", storedOwner);
					rt.Add(xaOwner);
				}
				foreach (var objsur in rt.Descendants("objsur"))
				{
					XAttribute xaType = objsur.Attribute("t");
					if (xaType == null)
						continue;
					XAttribute xaGuidObj = objsur.Attribute("guid");
					Guid guidObj = new Guid(xaGuidObj.Value);
					if (!m_guids.Contains(guidObj))
					{
						// MSAs and morphs of morph bundles are handled (later) by MorphBundleFixer, which can often do something smarter.
						if (className == "WfiMorphBundle")
						{
							switch (objsur.Parent.Name.LocalName)
							{
								case "Msa":
								case "Morph":
									continue;
							}
						}
						errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingLinkToNonexistingObject,
							guidObj, className, guid, objsur.Parent.Name));
						m_danglingLinks.Add(objsur);
						continue;
					}
					if (xaType.Value == "o")
					{
						Guid guidStored;
						if (m_owners.TryGetValue(guidObj, out guidStored))
						{
							if (guidStored != guid)
							{
								errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingMultipleOwnershipLink,
									guidObj, className, guid, objsur.Parent.Name));
								m_danglingLinks.Add(objsur);	// excessive ownership
							}
						}
					}
				}
				foreach (var objsur in m_danglingLinks)
				{
					var parent = objsur.Parent;
					objsur.Remove();
					if (!parent.Elements().Any())
					{
						// LT-14189: don't keep empty atomic ref property elements.
						// Removing empty sequence and collection property elements is less necessary, but generally
						// desirable, since we don't normally write them out empty, so it reduces diffs for Send/Receive.
						// The parent of an objsur is always a property, and we never want those elements if they
						// are empty.
						parent.Remove();
					}
				}
				m_danglingLinks.Clear();

				foreach (var run in rt.Descendants("Run"))
				{
					XAttribute xa = run.Attribute("editable");
					if (xa != null)
					{
						errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksRemovingEditableAttribute,
							rt.Attribute("class")));
						run.SetAttributeValue("editable", null);
					}
				}
				FixDuplicateWritingSystems(rt, guid, "AUni", errorLogger);
				FixDuplicateWritingSystems(rt, guid, "AStr", errorLogger);
				switch (className)
				{
					case "RnGenericRec":
						FixGenericDate("DateOfEvent", rt, className, guid, errorLogger);
						break;
					case "CmPerson":
						FixGenericDate("DateOfBirth", rt, className, guid, errorLogger);
						FixGenericDate("DateOfDeath", rt, className, guid, errorLogger);
						break;
				}
				return true;
			}
		}

		private void StoreGuidInfoAndOwnership(XElement rt, ErrorLogger errorLogger)
		{
			Guid guid = new Guid(rt.Attribute("guid").Value);
			if (m_guids.Contains(guid))
			{
				errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksObjectWithGuidAlreadyExists, guid));
				m_dupGuidElements.Add(rt);
			}
			else
			{
				m_guids.Add(guid);
			}
			var objsurGuids = new HashSet<string>();
			foreach (var objsur in rt.Descendants("objsur"))
			{
				XAttribute xaType = objsur.Attribute("t");
				if (xaType == null || xaType.Value != "o")
					continue;
				XAttribute xaGuidObj = objsur.Attribute("guid");
				Guid guidObj = new Guid(xaGuidObj.Value);
				if (m_owners.ContainsKey(guidObj))
				{
					errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(), String.Format(Strings.ksObjectWithGuidAlreadyOwned,
						guidObj, guid));
					m_dupOwnedElements.Add(objsur);
				}
				else
				{
					m_owners.Add(guidObj, guid);
				}
				objsurGuids.Add(objsur.Attribute("guid").Value);
			}
			//Now that all we have collected all the objects owned by the current rt element store it for later use.
			//There is no need to store this information if the rt element does not own anything.
			if (objsurGuids.Count > 0)
				m_parentToOwnedObjsur.Add(rt.Attribute("guid").Value, objsurGuids);
		}

		internal static void FixGenericDate(string fieldName, XElement rt, string className, Guid guid, ErrorLogger errorLogger)
		{
			foreach (var xeGenDate in rt.Descendants(fieldName).ToList()) // ToList because we may modify things and mess up iterator.
			{
				var genDateAttr = xeGenDate.Attribute("val");
				if (genDateAttr == null)
					continue;
				var genDateStr = genDateAttr.Value;
				GenDate someDate;
				if (GenDate.TryParse(genDateStr, out someDate))
					continue; // all is well, valid GenDate
				genDateAttr.Value = "0"; //'Remove' the date if we could not load or parse it
				errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(),
					string.Format(Strings.ksRemovingGenericDate, genDateStr, fieldName, className, guid));
			}
		}

		/// <summary>
		/// Fix any cases where a multistring has duplicate writing systems.
		/// </summary>
		/// <param name="rt">The element to repair</param>
		/// <param name="guid">for logging</param>
		/// <param name="eltName"></param>
		/// <param name="errorLogger">The logger to use</param>
		internal static void FixDuplicateWritingSystems(XElement rt, Guid guid, string eltName, ErrorLogger errorLogger)
		{
			// Get all the alternatives of the given type.
			var alternatives = rt.Descendants(eltName);
			// group them by parent
			var groups = new Dictionary<XElement, List<XElement>>();
			foreach (var item in alternatives)
			{
				List<XElement> children;
				if (!groups.TryGetValue(item.Parent, out children))
				{
					children = new List<XElement>();
					groups[item.Parent] = children;
				}
				children.Add(item);
			}
			foreach (var kvp in groups)
			{
				var list = kvp.Value;
				list.Sort((x, y) => x.Attribute("ws").Value.CompareTo(y.Attribute("ws").Value));
				for (int i = 0; i < list.Count - 1; i++)
				{
					if (list[i].Attribute("ws").Value == list[i + 1].Attribute("ws").Value)
					{
						errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(),
							string.Format(Strings.ksRemovingDuplicateAlternative, list[i + 1], kvp.Key.Name.LocalName, guid, list[i]));
						list[i + 1].Remove();
						// Note that we did not remove it from the LIST, only from its parent.
						// It is still available to be compared to the NEXT item, which might also have the same WS.
					}
				}
			}
		}
	}

	/// <summary>
	/// This abstract class provides the interface for fixing problems on an element\row\CmObject level.
	/// The members m_guids and m_owners can be used by fixes which need global information.
	/// </summary>
	internal abstract class RtFixer
	{
		protected HashSet<Guid> m_guids = new HashSet<Guid>();
		protected Dictionary<Guid, Guid> m_owners = new Dictionary<Guid, Guid>();
		protected Dictionary<string, HashSet<string>> m_parentToOwnedObjsur = new Dictionary<string, HashSet<string>>();
		protected HashSet<string> m_rtElementsToDelete = new HashSet<string>();

		internal virtual void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids,
			Dictionary<string, HashSet<string>> parentToOwnedObjsur, HashSet<string> rtElementsToDelete)
		{
			m_owners = owners;
			m_guids = guids;
			m_parentToOwnedObjsur = parentToOwnedObjsur;
			m_rtElementsToDelete = rtElementsToDelete;
		}

		/// <summary>
		/// This method gives each fixer the opportunity to look at any custom fields the db might have.
		/// </summary>
		/// <param name="additionalFieldsElem">CustomField elements are direct children of this XElement.</param>
		internal virtual void InspectAdditionalFieldsElement(XElement additionalFieldsElem)
		{
		}

		/// <summary>
		/// Do any fixes to this particular root element here.
		/// Return true if we are done fixing this element and can write it out.
		/// Return false if we need to delete this root element.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <returns></returns>
		internal abstract bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger);

		/// <summary>
		/// Override this method if a Fixer needs to gather information on one pass in FwDataFixer.ProcessDocument().
		/// in order to fix everything on another pass (with FixElement). Try hard to limit your time in here to a
		/// small subset of the available rt elements!
		/// </summary>
		/// <param name="rt"></param>
		internal virtual void InspectElement(XElement rt)
		{
			// Base class does nothing.
		}

		//Delete an object and recursively find and delete all owned decendants of the object.
		internal void MarkObjForDeletionAndDecendants(string rtElementGuid)
		{
			m_rtElementsToDelete.Add(rtElementGuid);

			//If the element has any owned decendants then mark those for deletion also.
			HashSet<string> ownedObjsurs;
			m_parentToOwnedObjsur.TryGetValue(rtElementGuid, out ownedObjsurs);
			if (ownedObjsurs != null)
			{
				foreach (var ownedObj in ownedObjsurs)
				{
					MarkObjForDeletionAndDecendants(ownedObj);
				}
			}
		}
	}
}
