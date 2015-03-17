using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	internal delegate bool TryGetNewLangTag(string oldLangTag, out string newLangTag);

	internal class WritingSystemIdMigrator
	{
		private readonly IDomainObjectDTORepository m_repoDto;
		private readonly TryGetNewLangTag m_newLangTagGetter;
		private readonly string m_layoutFilePattern;

		public WritingSystemIdMigrator(IDomainObjectDTORepository repoDto, TryGetNewLangTag newLangTagGetter, string layoutFilePattern)
		{
			m_repoDto = repoDto;
			m_newLangTagGetter = newLangTagGetter;
			m_layoutFilePattern = layoutFilePattern;
		}

		// We update every instance of an AUni, AStr, Run, or WsProp element that has a ws attribute.
		// Also the value of every top-level WritingSystem element that has a Uni child
		// Finally several ws-list properties of langProject.
		// AUni, ASTr, and Run are very common; WsProp and WritingSystem are relatively rare. So there's some
		// inefficiency in checking for them everywhere. I'm guess it won't add all that much overhead, and
		// it simplifies the code and testing.
		public void Migrate()
		{
			foreach (DomainObjectDTO dto in m_repoDto.AllInstances())
			{
				var changed = false;
				XElement data = XElement.Parse(dto.Xml);
				var elementsToRemove = new List<XElement>();
				foreach (XElement elt in data.XPathSelectElements("//*[name()='AUni' or name()='AStr' or name()='Run' or name()='WsProp' or name()='Prop']"))
				{
					if ((elt.Name == "AUni" || elt.Name == "AStr") && string.IsNullOrEmpty(elt.Value))
					{
						changed = true;
						elementsToRemove.Add(elt); // don't remove right away, messes up the iteration.
						continue;
					}
					XAttribute attr = elt.Attribute("ws");
					if (attr == null)
						continue; // pathological, but let's try to survive
					string oldTag = attr.Value;
					string newTag;
					if (TryGetNewTag(oldTag, out newTag))
					{
						changed = true;
						attr.Value = newTag;
					}
				}
				foreach (XElement elt in elementsToRemove)
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
				XElement residueElt = data.Element("LiftResidue");
				if (residueElt != null)
				{
					bool changedResidue = false;
					var uniElt = residueElt.Element("Uni");
					if (uniElt != null)
					{
						// We may have more than one root element which .Parse can't handle. LT-11856, LT-11698.
						XElement contentElt = XElement.Parse("<x>" + uniElt.Value + "</x>");
						foreach (XElement elt in contentElt.XPathSelectElements("//*[@lang]"))
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
					DataMigrationServices.UpdateDTO(m_repoDto, dto, data.ToString());
				}
			}
			DomainObjectDTO langProjDto = m_repoDto.AllInstancesSansSubclasses("LangProject").First();
			XElement langProj = XElement.Parse(langProjDto.Xml);
			bool lpChanged = UpdateAttr(langProj, "AnalysisWss");
			lpChanged |= UpdateAttr(langProj, "CurVernWss");
			lpChanged |= UpdateAttr(langProj, "CurAnalysisWss");
			lpChanged |= UpdateAttr(langProj, "CurPronunWss");
			lpChanged |= UpdateAttr(langProj, "VernWss");
			if (lpChanged)
				DataMigrationServices.UpdateDTO(m_repoDto, langProjDto, langProj.ToString());
			string settingsFolder = Path.Combine(m_repoDto.ProjectFolder, FdoFileHelper.ksConfigurationSettingsDir);
			if (Directory.Exists(settingsFolder))
			{
				foreach (string layoutFile in Directory.GetFiles(settingsFolder, m_layoutFilePattern))
				{
					XElement layout = XElement.Parse(File.ReadAllText(layoutFile, Encoding.UTF8));
					bool changedFile = false;
					foreach (XElement elt in layout.XPathSelectElements("//*[@ws]"))
					{
						changedFile |= FixWSAtttribute(elt.Attribute("ws"));
					}
					foreach (XElement elt in layout.XPathSelectElements("//*[@visibleWritingSystems]"))
					{
						changedFile |= FixWSAtttribute(elt.Attribute("visibleWritingSystems"));
					}
					if (changedFile)
					{
						using (var xmlWriter = XmlWriter.Create(layoutFile, new XmlWriterSettings {Encoding = Encoding.UTF8}))
							layout.WriteTo(xmlWriter);
					}
				}
				string localSettingsPath = Path.Combine(settingsFolder, "db$local$Settings.xml");
				if (File.Exists(localSettingsPath))
				{
					XElement settings = XElement.Parse(File.ReadAllText(localSettingsPath, Encoding.UTF8));
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
					// The value of this one simply IS a writing system.
					namesAndPatterns["db$local$ConcordanceWs"] = "^(?'target'.*)$";
					foreach (XElement elt in settings.Elements("Property"))
					{
						XElement nameElt = elt.Element("name");
						if (nameElt == null)
							continue;
						string pattern;
						string propName = nameElt.Value;
						if (namesAndPatterns.TryGetValue(propName, out pattern))
							ReplaceWSIdInValue(elt, pattern, ref changedFile);
						else if (propName.EndsWith("_sorter") || propName.EndsWith("_filter") || propName.EndsWith("_ColumnList"))
							ReplaceWSIdInValue(elt, "ws=\"(?'target'[^\"]+)\"", ref changedFile);
					}
					if (changedFile)
					{
						using (var xmlWriter = XmlWriter.Create(localSettingsPath, new XmlWriterSettings {Encoding = Encoding.UTF8}))
							settings.WriteTo(xmlWriter);
					}
				}
			}
		}

		private bool TryGetNewTag(string oldLangTag, out string newLangTag)
		{
			// should never be changed.
			if (oldLangTag.Equals("$wsname", StringComparison.InvariantCultureIgnoreCase))
			{
				newLangTag = oldLangTag;
				return false;
			}

			return m_newLangTagGetter(oldLangTag, out newLangTag);
		}

		// Replace any updated writing systems in the attribute value.
		// Handles an input which may begin with $ws= and/or may have multiple WSs separated by comma.
		private bool FixWSAtttribute(XAttribute attr)
		{
			bool changedFile = false;
			var oldTag = attr.Value;
			string prefix = "";
			if (oldTag.StartsWith("$ws="))
			{
				prefix = "$ws=";
				oldTag = oldTag.Substring(prefix.Length);
			}
			string combinedTags = "";
			foreach (string input in oldTag.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
			{
				string convertedTag;
				string outputTag = input;
				if (WritingSystemServices.GetMagicWsIdFromName(oldTag) == 0 && TryGetNewTag(input, out convertedTag))
				{
					changedFile = true;
					outputTag = convertedTag;
				}
				if (combinedTags != "")
					combinedTags += ",";
				combinedTags += outputTag;
			}
			if (changedFile)
				attr.Value = prefix + combinedTags;
			return changedFile;
		}

		private void ReplaceWSIdInValue(XElement elt, string pattern, ref bool changedFile)
		{
			XElement valueElt = elt.Element("value");
			if (valueElt == null)
				return;
			// Process matches in reverse order so length changes will not invalidate positions of earlier ones.
			var matches = new List<Match>();
			foreach (Match match in Regex.Matches(valueElt.Value, pattern))
				matches.Insert(0, match);
			foreach (Match match in matches)
			{
				Group target = match.Groups["target"];
				string oldTag = target.Value;
				string prefixTag = "";
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

		private bool UpdateAttr(XElement langProj, string eltName)
		{
			var parent = langProj.Element(eltName);
			if (parent == null)
				return false;
			var uni = parent.Element("Uni");
			if (uni == null)
				return false;
			var newTag = uni.Value.Split(' ').Select(MapTag).Aggregate((x, y) => x + " " + y);
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
	}
}
