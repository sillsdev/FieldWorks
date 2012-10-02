// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000044.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Palaso.WritingSystems.Migration;
using Palaso.WritingSystems.Migration.WritingSystemsLdmlV0To1Migration;
using SIL.FieldWorks.Common.FwUtils;
using System;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix the LDML files in the project's local store so all are version 2.
	/// In the process some writing system tags may be changed. Also, there may be some tags
	/// that are not valid for LDML version 2 writing systems, but which don't have corresponding
	/// LDML files in the project's local store. Therefore, whether or not we change a tag in
	/// the store, we need to scan all tags in the project to see whether any need to change.
	/// We never merge two writing systems together in this migration, so if the natural result
	/// of migrating one that needs to change is a duplicate, we append -dupN to the variation
	/// to make it unique.
	/// While we are scanning all the strings, we take the opportunity to remove any empty
	/// multistring alterntives. They are redundant (ignored when reading the object) and
	/// therefore both waste space, and may also confuse things by being left behind if the
	/// user subsequently merges two writing systems. (They get left behind because, not being
	/// read in, they don't show up as an existing alternative, and then there is no change to
	/// their object so no reason to write itout.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000044 : IDataMigration
	{
		Dictionary<string, string> m_tagMap = new Dictionary<string, string>();
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000043);

			if (repoDto.ProjectFolder != Path.GetTempPath())
			{
				// Skip migrating the global repository if we're just running tests. Slow and may not be wanted.
				// In a real project we do this first; thus if by any chance a WS is differently renamed in
				// the two folders, the renaming that is right for this project wins.
				var globalWsFolder = DirectoryFinder.GlobalWritingSystemStoreDirectory;
				var globalMigrator = new LdmlInFolderWritingSystemRepositoryMigrator(globalWsFolder, NoteMigration);
				globalMigrator.Migrate();
			}

			var ldmlFolder = Path.Combine(repoDto.ProjectFolder, DirectoryFinder.ksWritingSystemsDir);
			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration);
			migrator.Migrate();
			UpdateTags(repoDto);

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		// We update every instance of an AUni, AStr, Run, or WsProp element that has a ws attribute.
		// Also the value of every top-level WritingSystem element that has a Uni child
		// Finally several ws-list properties of langProject.
		// AUni, ASTr, and Run are very common; WsProp and WritingSystem are relatively rare. So there's some
		// inefficiency in checking for them everywhere. I'm guess it won't add all that much overhead, and
		// it simplifies the code and testing.
		private void UpdateTags(IDomainObjectDTORepository repoDto)
		{
			foreach (var dto in repoDto.AllInstances())
			{
				var changed = false;
				XElement data = XElement.Parse(dto.Xml);
				var elementsToRemove = new List<XElement>();
				foreach (var elt in data.XPathSelectElements("//*[name()='AUni' or name()='AStr' or name()='Run' or name()='WsProp' or name()='Prop']"))
				{
					if ((elt.Name == "AUni" || elt.Name == "AStr") && string.IsNullOrEmpty(elt.Value))
					{
						changed = true;
						elementsToRemove.Add(elt); // don't remove right away, messes up the iteration.
						continue;
					}
					var attr = elt.Attribute("ws");
					if (attr == null)
						continue; // pathological, but let's try to survive
					var oldTag = attr.Value;
					string newTag;
					if (TryGetNewTag(oldTag, out newTag))
					{
						changed = true;
						attr.Value = newTag;
					}
				}
				foreach (var elt in elementsToRemove)
					elt.Remove();
				var wsElt = data.Element("WritingSystem");
				if (wsElt != null)
				{
					var uniElt = wsElt.Element("Uni");
					if (uniElt != null)
					{
						string newTag1;
						if (TryGetNewTag(uniElt.Value, out newTag1))
						{
							changed = true;
							uniElt.Value = newTag1;
						}
					}
				}
				var residueElt = data.Element("LiftResidue");
				if (residueElt != null)
				{
					bool changedResidue = false;
					var uniElt = residueElt.Element("Uni");
					if (uniElt != null)
					{
						// We may have more than one root element which .Parse can't handle. LT-11856, LT-11698.
						var contentElt = XElement.Parse("<x>" + uniElt.Value + "</x>");
						foreach (var elt in contentElt.XPathSelectElements("//*[@lang]"))
						{
							var attr = elt.Attribute("lang");
							if (attr == null)
								continue; // pathological, but let's try to survive
							var oldTag = attr.Value;
							string newTag;
							if (TryGetNewTag(oldTag, out newTag))
							{
								changedResidue = true;
								attr.Value = newTag;
							}
						}
						if (changedResidue)
						{
							changed = true;
							uniElt.Value = "";
							foreach (var node in contentElt.Nodes())
								uniElt.Value += node.ToString();
						}
					}
				}
				if (changed)
				{
					DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
				}
			}
			var langProjDto = repoDto.AllInstancesSansSubclasses("LangProject").First();
			var langProj = XElement.Parse(langProjDto.Xml);
			bool lpChanged = UpdateAttr(langProj, "AnalysisWss");
			lpChanged |= UpdateAttr(langProj, "CurVernWss");
			lpChanged |= UpdateAttr(langProj, "CurAnalysisWss");
			lpChanged |= UpdateAttr(langProj, "CurPronunWss");
			lpChanged |= UpdateAttr(langProj, "VernWss");
			if (lpChanged)
				DataMigrationServices.UpdateDTO(repoDto, langProjDto, langProj.ToString());
			var settingsFolder = Path.Combine(repoDto.ProjectFolder, DirectoryFinder.ksConfigurationSettingsDir);
			if (Directory.Exists(settingsFolder))
			{
				m_tagMap["$wsname"] = "$wsname"; // should never be changed.
				foreach (var layoutFile in Directory.GetFiles(settingsFolder, "*_Layouts.xml"))
				{
					var layout = XElement.Parse(File.ReadAllText(layoutFile, Encoding.UTF8));
					bool changedFile = false;
					foreach (var elt in layout.XPathSelectElements("//*[@ws]"))
					{
						var attr = elt.Attribute("ws");
						var oldTag = attr.Value;
						var prefix = "";
						if (oldTag.StartsWith("$ws="))
						{
							prefix = "$ws=";
							oldTag = oldTag.Substring(prefix.Length);
						}
						string newTag;
						if (WritingSystemServices.GetMagicWsIdFromName(oldTag) == 0 && TryGetNewTag(oldTag, out newTag))
						{
							changedFile = true;
							attr.Value = prefix + newTag;
						}
					}
					if (changedFile)
					{
						using (var xmlWriter = XmlWriter.Create(layoutFile, new XmlWriterSettings() { Encoding = Encoding.UTF8 }))
							layout.WriteTo(xmlWriter);
					}
				}
				var localSettingsPath = Path.Combine(settingsFolder, "db$local$Settings.xml");
				if (File.Exists(localSettingsPath))
				{
					var settings = XElement.Parse(File.ReadAllText(localSettingsPath, Encoding.UTF8));
					bool changedFile = false;
					var namesAndPatterns = new Dictionary<string, string>();
					// Each item in this dictionary should be a property name that occurs in the <name> attribute of a <Property> element
					// in the db$local$Settings.xml file, mapping to a regular expression that will pick out the writing system tag
					// in the corresponding <value> element in the property table. Each regex must have a 'target' group which matches the
					// writing system.
					// Looking in a list like this, we want number followed by % followed by ws code,5062001%,5062001%x-kal,5112002%,5112002%x-kal,
					namesAndPatterns["db$local$InterlinConfig_Edit_Interlinearizer"] = ",[0-9]+%(?'target'[^,]+)";
					// Here we expect to find something like  ws="x-kal"
					namesAndPatterns["db$local$LexDb.Entries_sorter"] = "ws=\"(?'target'[^\"]+)\"";
					foreach (var elt in settings.Elements("Property"))
					{
						var nameElt = elt.Element("name");
						if (nameElt == null)
							continue;
						string pattern;
						if (!namesAndPatterns.TryGetValue(nameElt.Value, out pattern))
							continue;
						var valueElt = elt.Element("value");
						if (valueElt == null)
							continue; // paranoia
						// Process matches in reverse order so length changes will not invalidate positions of earlier ones.
						var matches = new List<Match>();
						foreach (Match match in Regex.Matches(valueElt.Value, pattern))
							matches.Insert(0, match);
						foreach (Match match in matches)
						{
							var target = match.Groups["target"];
							var oldTag = target.Value;
							var prefixTag = "";
							if (oldTag.StartsWith("$ws="))
							{
								prefixTag = "$ws=";
								oldTag = oldTag.Substring(prefixTag.Length);
							}
							string newTag;
							if (WritingSystemServices.GetMagicWsIdFromName(oldTag) == 0 && TryGetNewTag(oldTag, out newTag))
							{
								newTag = prefixTag + newTag;
								changedFile = true;
								string prefix = valueElt.Value.Substring(0, target.Index);
								string suffix = valueElt.Value.Substring(target.Index + target.Length);
								valueElt.Value = prefix + newTag + suffix;
							}
						}
					}
					if (changedFile)
					{
						using (var xmlWriter = XmlWriter.Create(localSettingsPath, new XmlWriterSettings() { Encoding = Encoding.UTF8 }))
							settings.WriteTo(xmlWriter);
					}
				}
			}
		}

		// There's some confusion between the Palaso migrator and our version 19 migrator about whether an old language
		// tag should have multiple X's if it has more than one private-use component. Since such X's are not
		// significant, ignore them.
		private bool TryGetNewTag(string oldTag, out string newTag)
		{
			var key = RemoveMultipleX(oldTag.ToLowerInvariant());
			if (m_tagMap.TryGetValue(key, out newTag))
				return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
			var cleaner = new Rfc5646TagCleaner(oldTag);
			cleaner.Clean();
			// FieldWorks needs to handle this special case.
			if (cleaner.Language.ToLowerInvariant() == "cmn")
			{
				var region = cleaner.Region;
				if (string.IsNullOrEmpty(region))
					region = "CN";
				cleaner = new Rfc5646TagCleaner("zh", cleaner.Script, region, cleaner.Variant, cleaner.PrivateUse);
			}
			newTag = cleaner.GetCompleteTag();
			while (m_tagMap.Values.Contains(newTag, StringComparer.OrdinalIgnoreCase))
			{
				// We can't use this tag because it would conflict with what we are mapping something else to.
				cleaner = new Rfc5646TagCleaner(cleaner.Language, cleaner.Script, cleaner.Region, cleaner.Variant,
					GetNextDuplPart(cleaner.PrivateUse));
				newTag = cleaner.GetCompleteTag();
			}
			m_tagMap[key] = newTag;
			return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
		}

		// Given an initial private use tag, if it ends with a part that follows the pattern duplN,
		// return one made by incrementing N.
		// Otherwise, return one made by appending dupl1.
		internal static string GetNextDuplPart(string privateUse)
		{
			if (string.IsNullOrEmpty(privateUse))
				return "dupl1";
			var lastPart = privateUse.Split('-').Last();
			if (Regex.IsMatch(lastPart, "dupl[0-9]+", RegexOptions.IgnoreCase))
			{
				// Replace the old lastPart with the result of incrementing the number
				int val = int.Parse(lastPart.Substring("dupl".Length));
				return privateUse.Substring(0, privateUse.Length - lastPart.Length) + ("dupl" + (val + 1));
			}
			// Append dupl1. We know privateUse is not empty.
			return privateUse + "-dupl1";
		}

		private bool UpdateAttr(XElement langProj, string eltName)
		{
			var parent = langProj.Element(eltName);
			if (parent == null)
				return false;
			var uni = parent.Element("Uni");
			if (uni == null)
				return false;
			var newTag = uni.Value.Split(' ').Select(tag => MapTag(tag)).Aggregate((x, y) => x + " " + y);
			if (newTag != uni.Value)
			{
				uni.Value = newTag;
				return true;
			}
			return false;
		}

		private string MapTag(string input)
		{
			string newTag;
			if (TryGetNewTag(input, out newTag))
				return newTag;
			return input;
		}

		internal void NoteMigration(IEnumerable<LdmlVersion0MigrationStrategy.MigrationInfo> migrationInfo)
		{
			foreach (var info in migrationInfo)
			{
				// Due to earlier bugs, FieldWorks projects sometimes contain cmn* writing systems in zh* files,
				// and the fwdata incorrectly labels this data using a tag based on the file name rather than the
				// language tag indicated by the internal properties. We attempt to correct this by also converting the
				// file tag to the new tag for this writing system.
				if (info.FileName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
				{
					var fileNameTag = Path.GetFileNameWithoutExtension(info.FileName);
					if (fileNameTag != info.RfcTagBeforeMigration)
						m_tagMap[RemoveMultipleX(fileNameTag.ToLowerInvariant())] = info.RfcTagAfterMigration;
				}
				else
				{
					// Add the unchanged writing systems so that they can be handled properly in UpdateTags
					m_tagMap[RemoveMultipleX(info.RfcTagBeforeMigration.ToLowerInvariant())] = info.RfcTagAfterMigration;
				}
			}
		}

		string RemoveMultipleX(string input)
		{
			bool gotX = false;
			var result = new List<string>();
			foreach (var item in input.Split('-'))
			{
				if (item == "x")
				{
					if (gotX)
						continue;
					else
						gotX = true; // and include this first X
				}
				result.Add(item);
			}
			return string.Join("-", result.ToArray());
		}
	}
}
