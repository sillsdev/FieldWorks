// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using ECInterfaces;
using SilEncConverters40;

namespace LanguageExplorer.SfmToXml
{
	/// <summary />
	public class Converter
	{
		// Container for options section of map file:
		// Container for languages section of map file:
		private Hashtable m_langsToIgnore;  // langDef 'id' values to ignore
		private Hashtable m_fieldsToIgnore; // fields to ignore from the above langs
		private Hashtable m_hierarchyChildren;      // keeps track of children for each hierarchy entry
		private Hashtable m_fieldDescriptionsTableNotFound;
		// Counter to limit the number of error reports made for illegal characters.
		private int m_illegalCodes;
		private Hashtable m_autoFieldsPossible; // key=string[className], value=string[fwid]
		private Hashtable m_autoFieldsUsed; // key=string["classname_sfmName"], value=AutoFieldInfo
		private Hashtable m_autoFieldsBySFM; // key=string[sfmName], value=ArrayList of AutoFieldInfo
		// top analysis ws; ex: "en", ...
		protected string m_topAnalysisWS;
		// This enables us to get hold of encoding converters:
		private EncConverters m_converters;
		// Placeholder for the implied root of the hierarchy (we assume there's only one):
		private ClsHierarchyEntry m_root;
		// Hash table of Begin Markers, telling us which hierarchy entry each initiates. This
		// gets populated during ValidateHierarchy().
		private Hashtable m_beginMarkerHierarchyEntries;
		// this hash will allow a quick look up for non-auto fields to get the hierarchy entry it belongs to.
		private Hashtable m_sfmToHierarchy;
		// this hash keeps track of all the markers that aren't used in the hierarchy
		private Hashtable m_markersNotInHierarchy;
		public static ClsLog Log = new ClsLog();
		private int m_sfmLineNumber;
		private string m_sfmFileName;
		private string m_mappingFileName;
		private string m_outputFileName;

		/// <summary>
		/// Public class that handles the SFM transformation to utf8 xml format with encoding applied
		///
		/// methods called:
		///		AddPossibleAutoField
		///		Convert
		///
		/// This class requires that the map file already exist.  So this is just a black box that
		/// acts on the input files that it's given - with Convert being the main entry point.
		/// </summary>
		public Converter()
			: this(new EncConverters())
		{
		}

		/// <summary>
		/// internal method to allow testing of some functionality without requiring setup for EncConverters on a developer machine.
		/// </summary>
		internal Converter(EncConverters converters)
		{
			m_converters = converters;
			GetOptions = new Dictionary<string, bool>(); // maps options (for now a checkbox Checked value) to a key string
			LanguagesHashTable = new Hashtable(); // maps the langId to a ClsLanguage object
			m_langsToIgnore = new Hashtable();  // langDef 'id' values to ignore
			m_fieldsToIgnore = new Hashtable(); // fields that are of a 'lang' that is to be ignored
			HierarchyHashTable = new Hashtable();
			m_hierarchyChildren = new Hashtable();  // key=string, value=arraylist containing children string names (future StringDictionary)
			FieldMarkerHashTable = new Hashtable();
			m_fieldDescriptionsTableNotFound = new Hashtable();
			InFieldMarkerHashTable = new Hashtable();
			m_beginMarkerHierarchyEntries = new Hashtable();
			m_sfmToHierarchy = new Hashtable();
			m_markersNotInHierarchy = new Hashtable();
			m_autoFieldsPossible = new Hashtable();
			m_autoFieldsUsed = new Hashtable();
			m_autoFieldsBySFM = new Hashtable();
			m_topAnalysisWS = "en";
		}

		// only one entry per class - iow, the key is the classname
		public bool AddPossibleAutoField(string className, string fwID)
		{
			if (!m_autoFieldsPossible.ContainsKey(className))
			{
				m_autoFieldsPossible.Add(className, fwID);
				return true;
			}
			return false;   // not added
		}

		public int LevelOneElements { get; private set; }

		protected ClsFieldDescription GetFieldDescription(string key)
		{
			return FieldMarkerHashTable.ContainsKey(key) ? FieldMarkerHashTable[key] as ClsFieldDescription : null;
		}
		// accessors for derived classes to get to map read data
		protected Hashtable LanguagesHashTable { get; }
		protected Hashtable HierarchyHashTable { get; }
		protected Hashtable FieldMarkerHashTable { get; }
		protected Hashtable InFieldMarkerHashTable { get; }

		private void TestFile(string fileName)
		{
			if (!File.Exists(fileName))
			{
				throw new Exception($"{fileName} does not exist.");
			}
			// Open the file to read from.
			using (var sr = File.OpenText(fileName))
			{
				// Make sure there's at least one line:
				if (sr.ReadLine() == null)
				{
					throw new Exception($"{fileName} contains no data.");
				}
			}
		}

		private static bool TestFileWrite(string fileName)
		{
			if (!File.Exists(fileName))
			{
				return true; // assume that the file can be created and written to
			}
			var valid = true;
			// Open the file to read from.
			try
			{
				using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write))
				{
					fs.Close();
				}
			}
			catch
			{
				valid = false;
				// We need to block here. An example where it is needed is when you have
				// a large Phase1Output.xml file open in Zedit. Zedit memory-maps the file
				// so the open file above will fail. Without this, there is no indication
				// given to the operator that the operation failed and we show the same
				// html log file to the user as before. Thus it appears that no matter
				// what you try to do to fix the problem, nothing works. KenZ can testify
				// to losing several hours due to this problem.
				//valid = true;	// until case found for LT-2340 - don't allow to retry
			}
			return valid;
		}

		/// <summary>
		/// Returns the Options section of the .map file as a dictionary of booleans
		/// keyed by the string referring to the appropriate checkbox.
		/// </summary>
		protected Dictionary<string, bool> GetOptions { get; }

		/// <summary>
		/// Returns a hashtable of the entries in the Languages section of the .map file.
		/// </summary>
		protected Hashtable GetLanguages => LanguagesHashTable;

		private void ClearIgnoreIfSet(string langId)
		{
			// remove it from the Ignore list if it's present
			m_langsToIgnore.Remove(langId);
		}

		private void NoConvertWs(string langId)
		{
			if (LanguagesHashTable.ContainsKey(langId))
			{
				var lang = (ClsLanguage)LanguagesHashTable[langId];
				lang.NoConvert();
				ClearIgnoreIfSet(langId);
			}
			else
			{
				// lang isn't defined, possible future warning
			}
		}

		private void IgnoreWs(string langId)
		{
			if (LanguagesHashTable.ContainsKey(langId))
			{
				if (!m_langsToIgnore.ContainsKey(langId))
				{
					m_langsToIgnore.Add(langId, null);
				}
			}
		}

		private void ConvertWs(string langId, string data)
		{
			if (LanguagesHashTable.ContainsKey(langId))
			{
				var delim = new[] { ':' };
				var values = data.Split(delim);
				if (values.Length != 2) // should only be two elements per string
				{
					return;
				}
				var xmlLang = values[0].Trim();
				var map = values[1].Trim();
				var lang = (ClsLanguage)LanguagesHashTable[langId];
				// if it was previously 'ignore' then remove from ignore list
				if (m_langsToIgnore.ContainsKey(langId))
				{
					m_langsToIgnore.Remove(langId);
				}
				var xmlLangDiff = (lang.XmlLang != xmlLang);
				var mapDiff = (lang.EncCvtrMap != map);
				if (xmlLangDiff || mapDiff)
				{
					string msg;
					if (xmlLangDiff && mapDiff)
					{
						msg = !string.IsNullOrEmpty(map) ? string.Format(SfmToXmlStrings.LanguagesEntry_0_lang_1_2_map_3_4, langId, lang.XmlLang,
							xmlLang, lang.EncCvtrMap, map) : string.Format(SfmToXmlStrings.LanguagesEntry_0_lang_1_2_map_3, langId, lang.XmlLang, xmlLang, lang.EncCvtrMap);
					}
					else if (xmlLangDiff)
					{
						msg = string.Format(SfmToXmlStrings.LanguagesEntry_0_lang_1_2, langId, lang.XmlLang, xmlLang);
					}
					else
					{
						msg = !string.IsNullOrEmpty(map) ? string.Format(SfmToXmlStrings.LanguagesEntry_0_map_1_2, langId, lang.EncCvtrMap, map)
							: string.Format(SfmToXmlStrings.LanguagesEntry_0_map_1, langId, lang.EncCvtrMap);
					}
					Log.AddWarning(msg);
					lang.Convert(xmlLang, map);
				}
			}
		}

		private bool UseFiles(string sfmFileName, string mappingFileName, string outputFileName)
		{
			m_sfmFileName = sfmFileName;
			m_mappingFileName = mappingFileName;
			m_outputFileName = outputFileName;

			// Check we have read access to both files, and that they aren't empty:
			TestFile(sfmFileName);
			TestFile(mappingFileName);
			var ok = true;
			while (!TestFileWrite(outputFileName))
			{
				ok = false;
				var msg = string.Format(SfmToXmlStrings.PleaseCloseAnyEditorsLookingAt0, outputFileName);
				if (MessageBox.Show(msg, SfmToXmlStrings.UnableToOpenOutputFile, MessageBoxButtons.RetryCancel, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1) == DialogResult.Cancel)
				{
					break;
				}
				ok = true;
			}
			return ok;
		}

		public void Convert(string sfmFileName, string mappingFileName, string outputFileName)
		{
			Convert(sfmFileName, mappingFileName, outputFileName, string.Empty, string.Empty, string.Empty);
		}

		public void Convert(string sfmFileName, string mappingFileName, string outputFileName, string vernWs, string regWs, string natWs)
		{
			LevelOneElements = 0;
			// Log.Open(LogFileName);
			if (!UseFiles(sfmFileName, mappingFileName, outputFileName))
			{
				return;
			}
			using (var xmlOutput = new XmlTextWriter(m_outputFileName, Encoding.UTF8))
			{
				xmlOutput.Formatting = Formatting.Indented;
				xmlOutput.Indentation = 2;
				WriteOutputFileComment(sfmFileName, mappingFileName, outputFileName, xmlOutput);
				xmlOutput.WriteComment(" database is the root element for this file ");
				xmlOutput.WriteStartElement("database");
				var xmlMap = new XmlDocument();
				try
				{
					xmlMap.Load(m_mappingFileName);
				}
				catch (XmlException e)
				{
					Log.AddError(string.Format(SfmToXmlStrings.InvalidMappingFile0_1, m_mappingFileName, e.Message));
					// put out the warnings and errors
					Log.FlushTo(xmlOutput);
					xmlOutput.WriteEndElement(); // Close the Database node
					xmlOutput.Close();
					return;
				}
				ReadLanguages(xmlMap);
				// === Process the command line args relating to languages ===
				// National ws
				if (natWs.ToLowerInvariant() == SfmToXmlServices.Ignore)
				{
					IgnoreWs("nat");
				}
				else if (natWs.ToLowerInvariant() == "no-convert")
				{
					NoConvertWs("nat");
				}
				else if (natWs.Length > 0)
				{
					ConvertWs("nat", natWs);
				}
				// Regional ws
				if (regWs.ToLowerInvariant() == SfmToXmlServices.Ignore)
				{
					IgnoreWs("reg");
				}
				else if (regWs.ToLowerInvariant() == "no-convert")
				{
					NoConvertWs("reg");
				}
				else if (regWs.Length > 0)
				{
					ConvertWs("reg", regWs);
				}
				// Vern ws
				if (vernWs.ToLowerInvariant() == "no-convert")
				{
					NoConvertWs("vern");
				}
				else if (vernWs.Length > 0)
				{
					ConvertWs("vern", vernWs);
				}
				try
				{
					ReadHierarchy(xmlMap);
					ReadAndOutputSettings(xmlMap, xmlOutput);
					// read the mapping file and build internal classes / objects and
					// add field descriptions to output file
					ReadFieldDescriptions(xmlMap);  //  ReadAndOutputFieldDescriptions(xmlMap, xmlOutput);
					ReadCustomFieldDescriptions(xmlMap);
					// read the mapping file inline markers
					ReadInFieldMarkers(xmlMap);
					// Now validate the data read in. This must be done in the following order:
					// Languages, Field Descriptions, Hierarchy. Infield Markers must be validated
					// after Languages. This order is needed because the later checks rely on
					// success of the earlier ones.
					ValidateLanguages();    // throw if bad language data
					ValidateFieldDescriptions();
					ValidateCustomFieldDescriptions();
					ValidateHierarchy();
					ValidateInfieldMarkers();
				}
				catch (Exception e)
				{
					Log.AddError(string.Format(SfmToXmlStrings.UnhandledException0, e.Message));
				}
				var nl = Environment.NewLine;
				var comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: " + m_root.Name + nl;
				comments += "  This element contains the inputfile in an XML format." + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);
				try
				{
					ProcessSfmFileNewLogic(xmlOutput);
				}
				catch (Exception e)
				{
					Log.AddError(string.Format(SfmToXmlStrings.UnhandledException0, e.Message));
				}
				// put out the field descriptions with the autofield info integrated in: for xslt processing...
				comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: fieldDescriptions" + nl;
				comments += "  This element is put out after the data so that auto fields can be" + nl;
				comments += "  added here.  Otherwise, we'd have to make two passes over the data." + nl;
				comments += "  The additional information related to auto fields is used in the" + nl;
				comments += "  XSLT processing for building the phase2 output file." + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);
				xmlOutput.WriteStartElement("fieldDescriptions");
				foreach (DictionaryEntry fieldEntry in FieldMarkerHashTable)
				{
					var fd = fieldEntry.Value as ClsFieldDescription;
					if (fd is ClsCustomFieldDescription)
					{
						continue;   // the custom fields will be put out in a CustomFields section following...
					}
					if (m_autoFieldsBySFM.ContainsKey(fd.SFM))
					{
						var afiBysfm = (ArrayList)m_autoFieldsBySFM[fd.SFM];
						foreach (AutoFieldInfo afi in afiBysfm)
						{
							fd.AddAutoFieldInfo(afi.ClassName, afi.FWDest);
						}
					}
					fd.ToXmlLangString(xmlOutput);
				}
				xmlOutput.WriteEndElement();
				// put out the field descriptions with the autofield info integrated in: for xslt processing...
				comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: CustomFieldDescriptions" + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);
				xmlOutput.WriteStartElement("CustomFieldDescriptions");
				foreach (DictionaryEntry fieldEntry in FieldMarkerHashTable)
				{
					var fd = fieldEntry.Value as ClsCustomFieldDescription;
					fd?.ToXmlLangString(xmlOutput);
				}
				xmlOutput.WriteEndElement();
				// put out the infield descriptions for xslt processing...
				comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: inFieldMarkers" + nl;
				comments += "  This is where the infield / inline markers are put out." + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);
				OutputInFieldMarkers(xmlOutput);
				// put out the warnings and errors
				Log.FlushTo(xmlOutput);
				xmlOutput.WriteEndElement(); // Close the Database node
				xmlOutput.Close();
			}
		}

		protected bool ReadSettings(XmlDocument xmlMap)
		{
			return ReadAndOutputSettings(xmlMap, null);
		}

		private static bool ReadAndOutputSettings(XmlDocument xmlMap, XmlTextWriter xmlOutput)
		{
			var numFound = 0;
			var foundAttributes = new Hashtable();
			var settingsList = xmlMap.SelectNodes("sfmMapping/settings/meaning");
			foreach (XmlNode settingNode in settingsList)
			{
				var fwSetting = false;
				foreach (XmlAttribute attribute in settingNode.Attributes)
				{
					switch (attribute.Name)
					{
						case "app":
							if (attribute.Value.ToLowerInvariant() == "fw.sil.org")
							{
								fwSetting = true;
							}
							break;
						default:
							foundAttributes[attribute.Name] = attribute.Value;
							break;
					}
				}
				if (fwSetting && foundAttributes.Count > 0)
				{
					numFound++;
					if (xmlOutput != null)  // produce output
					{
						xmlOutput.WriteStartElement("setting");
						foreach (DictionaryEntry attrs in foundAttributes)
						{
							xmlOutput.WriteAttributeString(attrs.Key as string, attrs.Value as string);
						}
						xmlOutput.WriteEndElement();
					}
				}
			}
			if (numFound > 1)
			{
				Log.AddWarning(SfmToXmlStrings.DuplicateSettingsFoundForFwSilOrg);
			}
			return true;
		}

		protected bool ReadLanguages(XmlDocument xmlMap)
		{
			var success = true;
			var languageList = xmlMap.SelectNodes("sfmMapping/languages/langDef");
			foreach (XmlNode languageNode in languageList)
			{
				var language = new ClsLanguage();
				var isValidLanguageNode = language.ReadXmlNode(languageNode);
				if (LanguagesHashTable.ContainsKey(language.KEY))
				{
					Log.AddError(string.Format(SfmToXmlStrings.DuplicateId0InLanguages, language.KEY));
					success = false;
				}
				else if (string.IsNullOrEmpty(language.KEY))
				{
					Log.AddError(SfmToXmlStrings.LanguageWithEmptyMissingIdInLanguages);
					success = false;
				}
				else
				{
					LanguagesHashTable.Add(language.KEY, language);
					if (language.XmlLang.ToLowerInvariant() == SfmToXmlServices.Ignore || !isValidLanguageNode)
					{
						if (!m_langsToIgnore.ContainsKey(language.KEY))
						{
							m_langsToIgnore.Add(language.KEY, null);
						}
						if (isValidLanguageNode == false)
						{
							success = false;
						}
					}
				}
			}
			if (LanguagesHashTable.Count == 0)
			{
				Log.AddError(SfmToXmlStrings.NoValidLanguagesDefined);
				success = false;
			}
			return success;
		}

		protected bool ReadOptions(XmlDocument xmlMap)
		{
			var success = true;
			var optionList = xmlMap.SelectNodes("sfmMapping/options/option");
			foreach (XmlNode optionNode in optionList)
			{
				ILexImportOption option = new LexImportOption();
				if (!option.ReadXmlNode(optionNode))
				{
					continue;
				}
				if (GetOptions.ContainsKey(option.Id))
				{
					Log.AddError(string.Format(SfmToXmlStrings.DuplicateId0InOptions, option.Id));
					success = false;
				}
				else
				{
					GetOptions.Add(option.Id, option.IsChecked);
				}
			}
			if (GetOptions.Count == 0)
			{
				Log.AddError(SfmToXmlStrings.NoValidOptionsDefined);
				success = false;
			}
			return success;
		}

		/// <summary>
		/// Check that the read-in data makes sense.
		/// Currently, there are no errors judged as fatal. this is so the user can determine if there
		/// are any more errors, as we attempt to process their invalid data.
		/// </summary>
		private void ValidateLanguages()
		{
			// Check each language
			foreach (DictionaryEntry languageEntry in LanguagesHashTable)
			{
				var language = (ClsLanguage)languageEntry.Value;
				// Assign the encoding converter specified in the mapping file:
				if (!string.IsNullOrEmpty(language.EncCvtrMap) && !language.SetConverter(m_converters))
				{
					Log.AddFatalError(string.Format(SfmToXmlStrings.UnknownEncodingConvertersMap0InLanguage1, language.EncCvtrMap, language.KEY));
				}
			}
		}

		protected bool ReadHierarchy(XmlDocument xmlMap)
		{
			var success = true;
			var hierarchyList = xmlMap.SelectNodes("sfmMapping/hierarchy/level");
			foreach (XmlNode level in hierarchyList)
			{
				var hierarchyEntry = new ClsHierarchyEntry();
				if (hierarchyEntry.ReadXmlNode(level))
				{
					if (HierarchyHashTable.ContainsKey(hierarchyEntry.KEY))
					{
						Log.AddError(string.Format(SfmToXmlStrings.HierarchyEntry0MultiplyDefined, hierarchyEntry.Name));
						success = false;
					}
					else
					{
						HierarchyHashTable.Add(hierarchyEntry.KEY, hierarchyEntry);
					}
				}
				else
				{
					success = false;
				}
			}
			if (HierarchyHashTable.Count == 0)
			{
				Log.AddError(SfmToXmlStrings.NoValidHierarchyEntriesDefined);
				success = false;
			}
			// Now populate the children hashtable: key is parent and value is arraylist of children.
			foreach (DictionaryEntry dictionaryEntry in HierarchyHashTable)
			{
				var entry = (ClsHierarchyEntry)dictionaryEntry.Value;
				// add this node as a child of all the ancestors of it
				foreach (var name in entry.Ancestors)
				{
					var children = m_hierarchyChildren[name] as ArrayList;
					if (children == null)
					{
						children = new ArrayList();
						m_hierarchyChildren.Add(name, children);
					}
					if (!children.Contains(entry.Name))
					{
						children.Add(entry.Name);
					}
					else
					{
						Log.AddWarning(string.Format(SfmToXmlStrings.DuplicateHierarchy0IsAlreadyChildOf1, entry.Name, name));
					}
				}
			}
			return success;
		}

		/// <summary>
		/// Check that the read-in data makes sense.
		/// Currently, there are no errors judged as fatal. this is so the user can determine if there
		/// are any more errors, as we attempt to process their invalid data.
		/// </summary>
		private void ValidateHierarchy()
		{
			// some initial checks are handled in the 'ReadXmlNode' method of the ClsHierarchyEntry

			// determine what the leaf and root nodes are
			var leaf = new Hashtable();
			var root = new Hashtable();
			foreach (DictionaryEntry dictionaryEntry in HierarchyHashTable)
			{
				var entry = (ClsHierarchyEntry)dictionaryEntry.Value;
				leaf.Add(entry.Name, null);
				foreach (var name in entry.Ancestors)
				{
					if (!root.ContainsKey(name))
					{
						root.Add(name, null);
					}
				}
			}
			// now walk through one of the lists and mark items that exist in both lists
			var myEnumerator = leaf.GetEnumerator();
			while (myEnumerator.MoveNext())
			{
				// see if it's in both lists
				if (root.ContainsKey(myEnumerator.Key))
				{
					root.Remove(myEnumerator.Key);
					leaf.Remove(myEnumerator.Key);
					myEnumerator = leaf.GetEnumerator();
				}
			}
			// if we have more or less than 1 root item, this is an error
			if (root.Count != 1)
			{
				if (root.Count == 0)
				{
					Log.AddError(SfmToXmlStrings.NoRootImpliedInTheHierarchy);
				}
				else
				{
					var sb = new StringBuilder();
					foreach (DictionaryEntry dictEntry in root)
					{
						sb.AppendFormat(SfmToXmlStrings.ksQuotedItem, dictEntry.Key);
					}
					Log.AddError(string.Format(SfmToXmlStrings.ThereAre0RootsImpliedInTheHierarchy1, root.Count, sb));
				}

				// Add a dummy root to avoid a crash later:
				m_root = new ClsHierarchyEntry("UnknownRoot");
				HierarchyHashTable.Add(m_root.KEY, m_root);
			}
			else
			{
				// create the root level-node
				myEnumerator = root.GetEnumerator();
				myEnumerator.MoveNext();    // get on the first element
				var rootNode = myEnumerator.Key.ToString();
				m_root = new ClsHierarchyEntry(rootNode);
				// We are confident by now that the root is a unique entry:
				HierarchyHashTable.Add(m_root.KEY, m_root);
			}
			// this list will contain all the sfms used in all the hierarchy entries
			var allHierarchySfms = new Hashtable();
			// Check every Hierarchy Entry:
			foreach (DictionaryEntry dictionaryEntry in HierarchyHashTable)
			{
				var hierarchy = (ClsHierarchyEntry)dictionaryEntry.Value;
				// Check for uniqueness of the beginfields:
				foreach (var beginSfm in hierarchy.BeginFields)
				{
					if (beginSfm != SfmToXmlServices.Ignore) // special case to ignore the begin marker {needed for prev tests though}
					{
						if (m_beginMarkerHierarchyEntries.ContainsKey(beginSfm))
						{
							Log.AddError(string.Format(SfmToXmlStrings.HierarchyEntry0HasDuplicateBeginField1_2, hierarchy.Name, beginSfm, (m_beginMarkerHierarchyEntries[beginSfm] as ClsHierarchyEntry).Name));
						}
						else
						{
							m_beginMarkerHierarchyEntries.Add(beginSfm, hierarchy);     // add the key and its first occurence
							if (!allHierarchySfms.ContainsKey(beginSfm))
							{
								allHierarchySfms.Add(beginSfm, hierarchy);
							}
						}
						// Make sure beginfields don't appear in addtionalfields:
						if (hierarchy.AdditionalFieldsContains(beginSfm))
						{
							Log.AddError(string.Format(SfmToXmlStrings.HierarchyEntry0HasField1WhichIsInBeginFields, hierarchy.Name, beginSfm));
						}
					}
				}
				// Make sure multifields exist in either beginfields or additionalfields:
				foreach (var multiSfm in hierarchy.MultiFields)
				{
					if (!hierarchy.BeginFieldsContains(multiSfm) && !hierarchy.AdditionalFieldsContains(multiSfm))
					{
						Log.AddError(string.Format(SfmToXmlStrings.HierarchyEntry0HasUnexpectedMultiField1, hierarchy.Name, multiSfm));
					}
				}
				// Continue to build list of all hierarchy SFMs:
				foreach (var addtlSfm in hierarchy.AdditionalFields)
				{
					if (!allHierarchySfms.Contains(addtlSfm))
					{
						allHierarchySfms.Add(addtlSfm, hierarchy);
					}
					if (!m_sfmToHierarchy.Contains(addtlSfm))
					{
						m_sfmToHierarchy.Add(addtlSfm, hierarchy);
					}
				}
			}
			// Test to make sure each of our Hierarchy SFMs is defined in the FieldDescriptions:
			foreach (DictionaryEntry hierarchySfmPair in allHierarchySfms)
			{
				if (!FieldMarkerHashTable.ContainsKey(hierarchySfmPair.Key) && !m_fieldsToIgnore.ContainsKey(hierarchySfmPair.Key))
				{
					Log.AddError(string.Format(SfmToXmlStrings.HierarchyEntry0RefersToInvalidSFM1, ((ClsHierarchyEntry)hierarchySfmPair.Value).Name, hierarchySfmPair.Key));
				}
			}
		}

		protected bool ReadInFieldMarkers(XmlDocument xmlMap)
		{
			var success = true;
			// Iterate through all the input "ifm" elements:
			var inFieldMarkerList = xmlMap.SelectNodes("sfmMapping/inFieldMarkers/ifm");
			var htMarkers = new Hashtable(inFieldMarkerList.Count);
			foreach (XmlNode inFieldMarkerNode in inFieldMarkerList)
			{
				var marker = new ClsInFieldMarker();
				if (marker.ReadXmlNode(inFieldMarkerNode, LanguagesHashTable))
				{
					if (InFieldMarkerHashTable.ContainsKey(marker.KEY))
					{
						Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0MultiplyDefined, marker.Begin));
						success = false;
					}
					else
					{
						marker.GenerateElementName(htMarkers);
						InFieldMarkerHashTable.Add(marker.KEY, marker);
					}
				}
				else
				{
					success = false;
				}
			}
			return success;
		}

		private void OutputInFieldMarkers(XmlTextWriter xmlOutput)
		{
			if (xmlOutput == null)
			{
				return;
			}
			xmlOutput.WriteStartElement("inFieldMarkers");
			foreach (DictionaryEntry dictEntry in InFieldMarkerHashTable)
			{
				var inFieldMarker = (ClsInFieldMarker)dictEntry.Value;
				inFieldMarker.OutputXml(xmlOutput, true);
			}
			xmlOutput.WriteEndElement();
		}


		private void ValidateInfieldMarkers()
		{
			var xmlElementNames = new ArrayList();
			foreach (DictionaryEntry dictEntry in InFieldMarkerHashTable)
			{
				var inFieldMarker = (ClsInFieldMarker)dictEntry.Value;
				// Check that the given language exists in the languages list:
				if (!string.IsNullOrEmpty(inFieldMarker.Language) && !LanguagesHashTable.ContainsKey(inFieldMarker.Language))
				{
					Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0RefersToInvalidLanguage1, inFieldMarker.Begin, inFieldMarker.Language));
				}
				// Check that the XML element name is unique:
				if (xmlElementNames.Contains(inFieldMarker.ElementName))
				{
					Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0DuplicatesUsingElement1, inFieldMarker.Begin, inFieldMarker.ElementName));
				}
				else
				{
					xmlElementNames.Add(inFieldMarker.ElementName);
				}
			}
		}

		private static string MakeValidXMLComment(string text)
		{
			// remove "--" from the comments
			if (text.Contains("--"))
			{
				var pos = text.IndexOf("--", StringComparison.Ordinal);
				while (pos >= 0)
				{
					text = text.Remove(pos, 1);
					pos = text.IndexOf("--", pos, StringComparison.Ordinal);
				}
				text += " (double hyphens have been removed)";
			}
			// don't allow the comments to end with a hyphen "-"
			if (text.EndsWith("-", StringComparison.Ordinal))
			{
				text += " ";
			}
			return text;
		}

		private void WriteOutputFileComment(string sfmFileName, string mappingFileName, string outputFileName, XmlTextWriter xmlOutput)
		{
			var nl = Environment.NewLine;
			var comments = nl;
			comments += " ================================================================" + nl;
			comments += " " + DateTime.Now + nl;
			comments += " Created by " + Assembly.GetExecutingAssembly().ToString() + nl;
			comments += nl;
			comments += " The command line parameters were :" + nl;
			comments += "  sfmFileName : " + MakeValidXMLComment(sfmFileName) + nl;
			comments += "  mappingFile : " + MakeValidXMLComment(mappingFileName) + nl;
			comments += "  xmlOutput   : " + MakeValidXMLComment(outputFileName) + nl;
			comments += " ================================================================" + nl;
			xmlOutput.WriteComment(comments);
		}

		protected bool ReadFieldDescriptions(XmlDocument xmlMap)
		{
			// passing null as the XmlTextWriter causes the output to not happen
			return ReadAndOutputFieldDescriptions(xmlMap, null);
		}

		private bool ReadAndOutputFieldDescriptions(XmlDocument xmlMap, XmlTextWriter xmlOutput)
		{
			var success = true;
			xmlOutput?.WriteStartElement("fieldDescriptions");
			// Iterate through all the input "field" elements:
			var fieldList = xmlMap.SelectNodes("sfmMapping/fieldDescriptions/field");
			foreach (XmlNode FieldNode in fieldList)
			{
				var fieldDescription = new ClsFieldDescription();
				if (fieldDescription.ReadAndOutputXmlNode(FieldNode, LanguagesHashTable, m_topAnalysisWS, xmlOutput, ref m_langsToIgnore, ref m_fieldsToIgnore))
				{
					if (FieldMarkerHashTable.ContainsKey(fieldDescription.KEY))
					{
						Log.AddError(string.Format(SfmToXmlStrings.FieldDescriptionsHaveMoreThanOneInstanceOf0, fieldDescription.SFM));
						success = false;
					}
					else
					{
						FieldMarkerHashTable.Add(fieldDescription.KEY, fieldDescription);
					}
				}
				else
				{
					success = false;
				}
			}
			xmlOutput?.WriteEndElement(); // Close FieldDescriptions node
			if (FieldMarkerHashTable.Count == 0)
			{
				Log.AddError(SfmToXmlStrings.NoValidFieldDescriptionsDefined);
				success = false;
			}
			return success;
		}

		private void ValidateFieldDescriptions()
		{
			foreach (DictionaryEntry dictEntry in FieldMarkerHashTable)
			{
				var fldDesc = (ClsFieldDescription)dictEntry.Value;
				// Check that the given language exists in the languages list:
				if (!LanguagesHashTable.ContainsKey(fldDesc.Language))
				{
					Log.AddError(string.Format(SfmToXmlStrings.FieldDescription0RefersToInvalidLanguage1, fldDesc.SFM, fldDesc.Language));
					if (!m_fieldsToIgnore.ContainsKey(fldDesc.SFM))
					{
						m_fieldsToIgnore.Add(fldDesc.SFM, null);
					}
					Log.AddWarning(string.Format(SfmToXmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, fldDesc.SFM));
					continue;
				}
				if (fldDesc.IsAutoImportField)
				{
					continue;   // auto import fields aren't used in the hierarchy
				}
				// Make sure that each sfm is used in atleast one hierarchy entry
				var bUsed = false;
				foreach (DictionaryEntry hierarchy in HierarchyHashTable)
				{
					var entry = (ClsHierarchyEntry)hierarchy.Value;
					if (entry.UsesSFM(fldDesc.SFM))
					{
						bUsed = true;
						break;
					}
				}
				if (!bUsed)
				{
					// LT-2217: only show errors for fields that are not 'ignored'
					if (!m_fieldsToIgnore.ContainsKey(fldDesc.SFM))
					{
						Log.AddSFMError(fldDesc.SFM, string.Format(SfmToXmlStrings.FieldDescription0UnusedInHierarchy, fldDesc.SFM));
					}
					if (!m_markersNotInHierarchy.ContainsKey(fldDesc.SFM))
					{
						m_markersNotInHierarchy.Add(fldDesc.SFM, null);
					}
				}
			}
		}

		protected bool ReadCustomFieldDescriptions(XmlDocument xmlMap)
		{
			return ReadAndOutputCustomFieldDescriptions(xmlMap, null);
		}

		private bool ReadAndOutputCustomFieldDescriptions(XmlDocument xmlMap, XmlTextWriter xmlOutput)
		{
			var success = true;
			xmlOutput?.WriteStartElement("CustomFieldDescriptions");
			// Iterate through all the input "CustomField" elements:
			var fieldList = xmlMap.SelectNodes("sfmMapping/CustomFieldDescriptions/CustomField");
			foreach (XmlNode fieldNode in fieldList)
			{
				var fieldDescription = new ClsCustomFieldDescription();
				if (fieldDescription.ReadAndOutputXmlNode(fieldNode, LanguagesHashTable, m_topAnalysisWS, xmlOutput, ref m_langsToIgnore, ref m_fieldsToIgnore))
				{
					if (FieldMarkerHashTable.ContainsKey(fieldDescription.KEY))
					{
						Log.AddError(string.Format(SfmToXmlStrings.FieldDescriptionsHaveMoreThanOneInstanceOf0, fieldDescription.SFM));
						success = false;
					}
					else
					{
						FieldMarkerHashTable.Add(fieldDescription.KEY, fieldDescription);

					}
				}
				else
				{
					success = false;
				}
			}
			xmlOutput?.WriteEndElement(); // Close FieldDescriptions node
			if (FieldMarkerHashTable.Count == 0)
			{
				Log.AddError(SfmToXmlStrings.NoValidFieldDescriptionsDefined);
				success = false;
			}
			return success;
		}

		private void ValidateCustomFieldDescriptions()
		{
			foreach (DictionaryEntry dictEntry in FieldMarkerHashTable)
			{
				var fldDesc = (ClsFieldDescription)dictEntry.Value;
				// Check that the given language exists in the languages list:
				if (!LanguagesHashTable.ContainsKey(fldDesc.Language))
				{
					Log.AddError(string.Format(SfmToXmlStrings.FieldDescription0RefersToInvalidLanguage1, fldDesc.SFM, fldDesc.Language));
					if (!m_fieldsToIgnore.ContainsKey(fldDesc.SFM))
					{
						m_fieldsToIgnore.Add(fldDesc.SFM, null);
					}
					Log.AddWarning(string.Format(SfmToXmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, fldDesc.SFM));
					continue;
				}
				if (fldDesc.IsAutoImportField)
				{
					continue;   // auto import fields aren't used in the hierarchy
				}
				// Make sure that each sfm is used in atleast one hierarchy entry
				var bUsed = false;
				foreach (DictionaryEntry hierarchy in HierarchyHashTable)
				{
					var entry = (ClsHierarchyEntry)hierarchy.Value;
					if (entry.UsesSFM(fldDesc.SFM))
					{
						bUsed = true;
						break;
					}
				}
				if (!bUsed)
				{
					// LT-2217: only show errors for fields that are not 'ignored'
					if (!m_fieldsToIgnore.ContainsKey(fldDesc.SFM))
					{
						Log.AddSFMError(fldDesc.SFM, string.Format(SfmToXmlStrings.FieldDescription0UnusedInHierarchy, fldDesc.SFM));
					}
					if (!m_markersNotInHierarchy.ContainsKey(fldDesc.SFM))
					{
						m_markersNotInHierarchy.Add(fldDesc.SFM, null);
					}
				}
			}
		}


		/// <summary>
		/// Find the first match of 'item' in the 'inData' returning the index or -1 if not found.
		/// </summary>
		/// <param name="inData">data to search through</param>
		/// <param name="startPos">index in the data to start at</param>
		/// <param name="item">data to search for</param>
		/// <returns>index where data starts or -1 if not found</returns>
		public static long FindFirstMatch(byte[] inData, int startPos, byte[] item)
		{
			// none
			long index = -1;
			// if the indata length is to small, just return
			if (inData.Length - startPos < item.Length)
			{
				return index;
			}
			// position into the item
			var matchPos = 0;
			for (var pos = startPos; pos < inData.Length; pos++)
			{
				if (inData[pos] == item[matchPos])  // match at this item index
				{
					if (matchPos == item.Length - 1)    // complete item match
					{
						index = pos - matchPos;         // found match
						break;
					}
					matchPos++;     // keep looking through the whole item
				}
				else
				{
					matchPos = 0;
				}
			}
			return index;
		}

		public static byte[] WideToMulti(string wide, Encoding encodingToUse)
		{
			Encoding encoding = new UTF8Encoding(false, true);
			if (encodingToUse == Encoding.ASCII)
			{
				encoding = new ASCIIEncoding();
			}
			var data = new byte[encoding.GetByteCount(wide.ToCharArray())];
			encoding.GetBytes(wide, 0, wide.Length, data, 0);
			return data;
		}

		public static string MultiToWide(byte[] multi, int start, int end, Encoding encodingToUse)
		{
			MultiToWideError error;
			byte[] badBytes;
			return MultiToWideWithERROR(multi, start, end, encodingToUse, out error, out badBytes);
		}

		public static string MultiToWideWithERROR(byte[] multi, int start, int end, Encoding encodingToUse, out MultiToWideError err, out byte[] badBytes)
		{
			err = MultiToWideError.None;
			badBytes = null;
			Encoding encoding = new UTF8Encoding(false, true);
			if (encodingToUse == Encoding.ASCII)
			{
				encoding = new ASCIIEncoding();
			}
			try
			{
				var charCount = encoding.GetCharCount(multi, start, end - start + 1);
				var chars = new char[charCount];
				encoding.GetChars(multi, start, end - start + 1, chars, 0);
				return new string(chars);
			}
			catch (DecoderFallbackException dfe)    //(Exception e)
			{
				err = MultiToWideError.InvalidCodePoint;
				// TODO-Linux: BytesUnknown is marked with a [MonoTODO] attribute.
				badBytes = dfe.BytesUnknown;
				// have an invalid utf8 char most likely, so switch to ascii
				if (encoding.EncodingName == Encoding.UTF8.EncodingName)
				{
					encoding = new ASCIIEncoding();
				}
				var charCount = encoding.GetCharCount(multi, start, end - start + 1);
				var chars = new char[charCount];
				encoding.GetChars(multi, start, end - start + 1, chars, 0);
				return new string(chars);
			}
		}

		public static string MultiToWide(byte[] multi, Encoding encodingToUse)
		{
			return MultiToWide(multi, 0, multi.Length - 1, encodingToUse);
		}

		/// <summary>
		/// Searches the given data bytes for the given infield marker.
		/// </summary>
		private static long SearchForInFieldMarker(byte[] data, int startIndex, string inFieldMarker)
		{
			var stringToBytes = new ClsStringToOrFromBytes(inFieldMarker);
			return FindFirstMatch(data, startIndex, stringToBytes.ToBytes());
		}

		private string ConvertBytes(string marker, byte[] data, int start, int end, IEncConverter converter, int lineNumber)
		{
			var result = string.Empty;
			if (end - start <= 0)
			{
				// if the data to convert is of length zero (or less) then just return an empty string
				return result;
			}
			if (converter != null)
			{
				var len = end - start;
				var subData = new byte[len];
				for (var i = 0; i < len; i++)
				{
					subData[i] = data[start + i];
				}
				try
				{
					result = converter.ConvertToUnicode(subData);
				}
				catch (Exception e)
				{
					Log.AddUniqueHighPriorityError(converter.Name, string.Format(SfmToXmlStrings.EncodingConverter0Failed1, converter.Name, e.Message));
				}
			}
			else
			{
				// We have no Encoding Converter, so assume text is in UTF8
				var workspace = new byte[end - start];
				for (var i = 0; i < end - start; i++)
				{
					workspace[i] = data[start + i];
				}
				var workspaceAsString = new ClsStringToOrFromBytes(workspace);
				var cMaxCodes = 20;
				if (m_illegalCodes < cMaxCodes)
				{
					byte badCodePoint;
					int badBytePosition;
					int badByteCount;   // add a count msg to the line.
					if (!workspaceAsString.IsUTF8String(out badCodePoint, out badBytePosition, out badByteCount))
					{
						// contains non-utf8 data
						if (badByteCount > ClsStringToOrFromBytes.MaxInvalidBytes)
						{
							Log.AddError(m_sfmFileName, lineNumber, string.Format(SfmToXmlStrings.Line0_SFM1ContainsIllegalUTF8Count2Max3IndexData4, lineNumber, marker, badByteCount, ClsStringToOrFromBytes.MaxInvalidBytes, workspaceAsString.InvalidByteString()));
						}
						else if (badByteCount > 1)
						{
							Log.AddError(m_sfmFileName, lineNumber, string.Format(SfmToXmlStrings.Line0_SFM1ContainsIllegalUTF8Count2IndexData3, lineNumber, marker, badByteCount, workspaceAsString.InvalidByteString()));
						}
						else
						{
							Log.AddError(m_sfmFileName, lineNumber, string.Format(SfmToXmlStrings.Line0_SFM1ContainsIllegalUTF8Code2, lineNumber, marker, System.Convert.ToString(badCodePoint, 16)));
						}

						m_illegalCodes++;  // += badByteCount;
						if (m_illegalCodes == cMaxCodes)
						{
							Log.AddError(m_sfmFileName, m_sfmLineNumber, string.Format(SfmToXmlStrings.InvalidCodepointsHaveBeenLogged, cMaxCodes));
						}
					}
				}
				result = workspaceAsString.String();
			}
			// replace invalid utf8 chars with valid replacement chars
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");

			return result;
		}

		private static bool AddUniqueMarker(ref SortedList foundMarkers, ref byte[] rawData, string markerData, ClsInFieldMarker inFieldMarker, int foundPos, bool isBeginMarker)
		{
			// If the found index already exists in the collection, then we have a possible error condition.
			if (foundMarkers.Contains(foundPos))
			{
				// see which one is a longer marker and use that one.
				var index = foundMarkers.IndexOfKey(foundPos);
				var existing = (ClsFoundInFieldMarker)foundMarkers.GetByIndex(index);
				var length = existing.Length;
				if (length != markerData.Length)
				{
					if (markerData.Length < length)
					{
						// the new one is smaller, so keep looking to see if there's a place for this one in the data
						foundPos = (int)SearchForInFieldMarker(rawData, foundPos + markerData.Length, markerData);
						if (foundPos >= 0)
						{
							Debug.WriteLine(" ** Above not added --- still looking");
							return AddUniqueMarker(ref foundMarkers, ref rawData, markerData, inFieldMarker, foundPos, isBeginMarker);
						}
						Debug.WriteLine(" ** Above not added --- can't replace what was there and no others found");
						return false;
					}
					// the one we just found is longer so remove the previous one and add this one.
					var oldMarker = foundMarkers.GetByIndex(index) as ClsFoundInFieldMarker;
					foundMarkers.RemoveAt(index);
					var newFoundMarker = isBeginMarker ? new ClsFoundInFieldMarker(inFieldMarker.Begin, inFieldMarker) : new ClsFoundInFieldMarker(markerData);
					foundMarkers.Add(foundPos, newFoundMarker);
					// now look for the removed marker ahead in the data
					foundPos = (int)SearchForInFieldMarker(rawData, foundPos + oldMarker.Length, oldMarker.Marker);
					if (foundPos >= 0)
					{
						Debug.WriteLine(" ** Above added --- now looking for one removed");
						return AddUniqueMarker(ref foundMarkers, ref rawData, oldMarker.Marker, oldMarker.ClsInFieldMarker, foundPos, oldMarker.IsBegin);
					}
				}
			}
			else
			{
				// save the found information
				foundMarkers.Add(foundPos, isBeginMarker ? new ClsFoundInFieldMarker(inFieldMarker.Begin, inFieldMarker) : new ClsFoundInFieldMarker(markerData));
			}
			return true;
		}

		/// <summary>
		/// This method is given the sfm and the data for it.  It is then parsed and the
		/// inField markers are processed as is the data for the sfm.  This includes,
		/// but is not limited to the encoding of the sfm data and possibly each inField
		/// marker as well.
		/// </summary>
		/// <param name="markerSFM">key to the sfms</param>
		/// <param name="markerData">data that has been read in the proper encoding</param>
		/// <param name="lineNumber">line number for this data so error msgs are correct</param>
		/// <returns></returns>
		public string ProcessSFMData(string markerSFM, byte[] markerData, int lineNumber)
		{
			var output = string.Empty;
			// Determine the default Encoding Converter based on the given SFM marker:
			var mainFieldDescription = (ClsFieldDescription)FieldMarkerHashTable[markerSFM];
			var cLang = (ClsLanguage)LanguagesHashTable[mainFieldDescription.Language];
			var iDefaultConverter = cLang?.EncCvtr;
			var currentConverter = iDefaultConverter;
			// It is possible that markers will not be closed in the order they are opened, so we will
			// provide an ordered list to store open markers, to help with closing them in the right order:
			var openMarkers = new ArrayList();
			// For efficiency, we will maintain a sorted container which keeps track of the first
			// instance of each infield marker. Every time we process an infield marker, we remove
			// it from the container, search for the next instance of that marker, and add this new
			// instance (if found) to the container.
			// To start with, we will search for the first instance of every possible marker. From then
			// on, we only search for further instances of ones we've found.
			var foundMarkers = new SortedList();
			// Find the first instance of each begin and end markers:
			foreach (DictionaryEntry fieldEntry in InFieldMarkerHashTable)
			{
				var inFieldMarker = (ClsInFieldMarker)fieldEntry.Value;
				// There can be only one begin marker:
				var foundPos = (int)SearchForInFieldMarker(markerData, 0, inFieldMarker.Begin);
				if (foundPos >= 0)
				{
					AddUniqueMarker(ref foundMarkers, ref markerData, inFieldMarker.Begin, inFieldMarker, foundPos, true);
					// There can be more than one end marker:
					foreach (string endMarker in inFieldMarker.End)
					{
						foundPos = (int)SearchForInFieldMarker(markerData, 0, endMarker);
						if (foundPos >= 0)
						{
							AddUniqueMarker(ref foundMarkers, ref markerData, endMarker, inFieldMarker, foundPos, false);
						}
					}
				}
			}
			// Iterate through the sorted list of markers, removing and processing the "first" one each time:
			var iMarkerDataPos = 0; // current pointer to text after a marker
			while (foundMarkers.Count > 0)
			{
				// Get and remove the top marker in the sorted list:
				var currentMarker = (ClsFoundInFieldMarker)foundMarkers.GetByIndex(0);
				var markerStartPos = (int)(foundMarkers.GetKey(0));
				foundMarkers.RemoveAt(0);
				if (markerStartPos >= iMarkerDataPos)
				{
					// Convert the text between this marker and the previous one and add it to the main output:
					var convertedData = ConvertBytes(mainFieldDescription.SFM, markerData, iMarkerDataPos, markerStartPos, currentConverter, lineNumber);
					output += convertedData;
					// Processing for Begin Markers is different from that for End Markers:
					if (currentMarker.IsBegin)
					{
						// Stack the new marker:
						openMarkers.Add(currentMarker);
						// Add the XML equivalent of the current marker to the main output:
						output += $"<{currentMarker.ClsInFieldMarker.ElementName}>";
						// If the inline marker has its own encoding, use that from now on:
						if (currentMarker.HasLangEncoding)
						{
							currentConverter = ((ClsLanguage)LanguagesHashTable[currentMarker.LanguageEncoding]).EncCvtr;
						}
						// We can't store "EndWithWord" or "EndWithField" markers ahead of time, so
						// compute those here and now.
						if (currentMarker.ClsInFieldMarker.EndWithWord)
						{
							var ichFound = SearchForEndOfWord(markerData, markerStartPos);
							if (ichFound >= 0)
							{
								AddUniqueMarker(ref foundMarkers, ref markerData, " ", currentMarker.ClsInFieldMarker, ichFound, false);
							}
						}
						else if (currentMarker.ClsInFieldMarker.EndWithField)
						{
							AddUniqueMarker(ref foundMarkers, ref markerData, string.Empty, currentMarker.ClsInFieldMarker, markerData.Length, false);
						}
					}
					else // currentMarker is an end marker
					{
						// Because and End Marker can close more than one type of Begin Marker at the same time,
						// we need to build a list of all Begin Markers closed by this End Marker:
						var affectedBeginMarkers = new Stack();
						foreach (ClsFoundInFieldMarker openMarker in openMarkers)
						{
							if (openMarker.ContainsEndMarker(currentMarker.Marker))
							{
								affectedBeginMarkers.Push(openMarker);
							}
						}
						// If markers are not closed in the order they were opened, we will have to do some
						// extra closing and re-opening:
						var markersToBeReopened = new Stack();
						// Now scan back through the open markers (latest towards earliest), closing
						// them all and storing those that need to be reopened after we close the
						// earliest of them:
						for (var openMarkerIndex = openMarkers.Count - 1; affectedBeginMarkers.Count > 0; openMarkerIndex--)
						{
							var markerToClose = (ClsFoundInFieldMarker)openMarkers[openMarkerIndex];
							// Close this marker:
							output += $"</{markerToClose.ElementName}>";
							openMarkers.RemoveAt(openMarkerIndex);
							// If the marker we just closed is not the top element in our Affected Markers stack, then
							// we need to re-open it later:
							var affectedMarker = (ClsFoundInFieldMarker)affectedBeginMarkers.Peek();
							if (affectedMarker.Marker == markerToClose.Marker)
							{
								affectedBeginMarkers.Pop(); // We really did need to close this one!
							}
							else
							{
								// We'll have to reopen this one later:
								markersToBeReopened.Push(markerToClose);
							}
						}
						// Now re-open those that were not supposed to be closed:
						while (markersToBeReopened.Count > 0)
						{
							var markerToReopen = (ClsFoundInFieldMarker)markersToBeReopened.Pop();
							output += $"<{markerToReopen.ClsInFieldMarker.ElementName}>";
							openMarkers.Add(markerToReopen);
						}
					}
					// Update our position in the raw data to skip past the marker we've just processed:
					// (But don't skip over the marker itself if the "marker" was the "EndOfWord" or
					// "EndOfField".
					var markerAsBytes = new ClsStringToOrFromBytes(currentMarker.Marker);
					iMarkerDataPos = currentMarker.Length > 0 && currentMarker.Marker != " " ? markerStartPos + markerAsBytes.ByteLength : markerStartPos;
				}
				// Find next instance of currentMarker in input data: but not if we're handling an
				// end marker for EndOfWord or EndOfField.
				if (currentMarker.Marker.Length > 0 && currentMarker.Marker != " ")
				{
					var foundPos = (int)SearchForInFieldMarker(markerData, iMarkerDataPos, currentMarker.Marker);
					if (foundPos >= 0)
					{
						// If the found index already exists in the collection, then we have an error condition.
						if (foundMarkers.Contains(foundPos) && currentMarker.IsBegin)
						{
							// FIX: needs to handle (again) the case where the begin marker is a subset of the end marker
							var matchingMarker = (ClsFoundInFieldMarker)foundMarkers[foundPos];
							if (matchingMarker.IsBegin) // can't also be begin , must be end to be valid
							{
								Log.AddError(string.Format(SfmToXmlStrings.InFieldMarker0IsMultiplyDefined, currentMarker.Marker));
							}
						}
						else
						{
							// save the found information:
							foundMarkers.Add(foundPos, currentMarker);
						}
					}
				}
			}
			// Process last fragment of text outside of last inFieldMarker:
			// Convert the text between this marker and the previous one and add it to the main output:
			var convertedDataString = ConvertBytes(mainFieldDescription.SFM, markerData, iMarkerDataPos, markerData.Length, currentConverter, lineNumber);
			if (mainFieldDescription.Type.ToLowerInvariant() == "date")
			{
				// handle any date processing here - validating and changing forms
				try
				{
					var dt = DateTime.Parse(convertedDataString);
					if (dt.Year < 1800)
					{
						throw new Exception();
					}
					var newDate = dt.ToString("yyy-MM-dd hh:mm:ss.fff");
					convertedDataString = convertedDataString.Replace(Environment.NewLine, string.Empty);

					if (newDate.IndexOf(convertedDataString) < 0)
					{
						Log.AddSFMWarning(mainFieldDescription.SFM, string.Format(SfmToXmlStrings.DateChangedFrom1To2, mainFieldDescription.SFM, convertedDataString, newDate));
					}
					convertedDataString = newDate;
				}
				catch
				{
					// LT-5352: don't log date errors if the dt field is empty
					if (convertedDataString.Trim().Length > 0)
					{
						Log.AddError(m_sfmFileName, lineNumber, string.Format(SfmToXmlStrings.X1HasAnUnrecognizedDateForm2, lineNumber.ToString(), mainFieldDescription.SFM, convertedDataString));
						convertedDataString = string.Empty;   // don't pass it on - ignore it
					}
				}
			}
			output += convertedDataString;
			// Close any markers left open by lazy SFM code:
			for (var openMarkerIndex = openMarkers.Count - 1; openMarkerIndex >= 0; openMarkerIndex--)
			{
				var markerToClose = (ClsFoundInFieldMarker)openMarkers[openMarkerIndex];
				// Close this marker:
				output += "</" + markerToClose.ElementName + ">";
			}
			return output;
		}

		private static int SearchForEndOfWord(byte[] markerData, int ichStart)
		{
			var foundPos = -1;
			var ichSpace = (int)SearchForInFieldMarker(markerData, ichStart, " ");
			var ichTab = (int)SearchForInFieldMarker(markerData, ichStart, "\t");
			var ichReturn = (int)SearchForInFieldMarker(markerData, ichStart, "\r");
			var ichLinefeed = (int)SearchForInFieldMarker(markerData, ichStart, "\n");
			if (ichSpace >= 0)
			{
				foundPos = ichSpace;
			}
			if (ichTab >= 0 && ichTab < foundPos)
			{
				foundPos = ichTab;
			}
			if (ichReturn >= 0 && ichReturn < foundPos)
			{
				foundPos = ichReturn;
			}
			if (ichLinefeed >= 0 && ichLinefeed < foundPos)
			{
				foundPos = ichLinefeed;
			}
			return foundPos;
		}

		public void ProcessSFMandData(string currentSfm, byte[] sfmData, int lineNumber, XmlTextWriter xmlOutput)
		{
			// now process the data for this sfm
			var processedText = ProcessSFMData(currentSfm, sfmData, lineNumber);
			processedText = processedText.Replace(Environment.NewLine, " "); // remove newlines
			processedText = processedText.Trim();   // remove whitespace
			// make sure the data is only in the following range:
			// 0x09, 0x0a, 0x0d, 0x20-0xd7ff, 0xe000-0xfffd
			// not using foreach so chars can be removed during processing w/o messing up the iterator
			var strTemp = new StringBuilder(processedText);
			var lastPos = strTemp.Length;
			var pos = 0;
			while (pos < lastPos)
			{
				var c = strTemp[pos];
				// try to test by order of most common/expected
				if (c >= 0x0020 && c <= 0xd7ff || c >= 0xe000 && c <= 0xfffd || c == 0x09 || c == 0x0d || c == 0x0a)
				{
					pos++;  // valid data
				}
				else
				{
					Log.AddError(m_sfmFileName, lineNumber, string.Format(SfmToXmlStrings.SFM1ContainsInvalidCharValue2H, lineNumber, currentSfm, ((int)c).ToString("X")));
					strTemp.Remove(pos, 1);
					lastPos--;
					// don't bump the pos as we need to process the new char at this position
				}
			}
			var cfd = (ClsFieldDescription)FieldMarkerHashTable[currentSfm];
			if (strTemp.Length > 0)
			{
				Log.AddSFMWithData(currentSfm);
			}
			else
			{
				Log.AddSFMNoData(currentSfm);
			}
			// Per LT-11134 we want to generate the element for the main lex field even if it is empty.
			// This allows us to import an entry with no form and keep POS information.
			// Per LT-10739 we want to generate the element for POS even if it is empty, so that we do
			// not incorrectly duplicate the pos information for previous senses.
			if (strTemp.Length > 0 || cfd.MeaningID == "lex" || cfd.MeaningID == "pos")
			{
				// use the xml safe name
				xmlOutput.WriteStartElement(cfd.SFMxmlSafe);
				xmlOutput.WriteAttributeString("srcLine", lineNumber.ToString());
				var sForm = strTemp.ToString();
				switch (cfd.MeaningID)
				{
					// some users may mark the citation form, but not the lexeme form, so
					// leaving the citation form alone preserves that information for later
					// cleanup.  On the other hand, if both are marked, the citation forms
					// will have to be cleaned up.  You can't win, you can't break even,
					// and they won't let you quit playing...  :-(
					case "lex":
					case "allo":
					case "ulf":
					case "var":
					case "sub":
						try
						{
							string sAlloClass;
							string sMorphTypeWs;
							var sMorphType = GetMorphTypeInfo(ref sForm, out sAlloClass, out sMorphTypeWs);
							xmlOutput.WriteAttributeString("morphTypeWs", sMorphTypeWs);
							xmlOutput.WriteAttributeString("morphType", sMorphType);
							xmlOutput.WriteAttributeString("allomorphClass", sAlloClass);
							if (cfd.IsRef)
							{
								// add the new Var and Sub attributes
								var attribPrefix = cfd.MeaningID.ToLowerInvariant();
								xmlOutput.WriteAttributeString(attribPrefix + "TypeWs", cfd.RefFuncWS); // comes from the combo box WS
								xmlOutput.WriteAttributeString(attribPrefix + "Type", cfd.RefFunc);     // combo box text
							}
						}
						catch (Exception ex)
						{
							// We have something we can't interpret. Give the user an Error message, but continue on if
							// the user ignores it. To continue on we use the entire string and assume it is a stem.
							var errMsg = string.Format(SfmToXmlStrings.BadMorphMarkers012, lineNumber, cfd.SFMxmlSafe, ex.Message);
							Log.AddFatalError(m_sfmFileName, lineNumber, errMsg);
							xmlOutput.WriteAttributeString("morphTypeWs", "en");
							xmlOutput.WriteAttributeString("morphType", "stem");
							xmlOutput.WriteAttributeString("allomorphClass", "MoStemAllomorph");
						}
						break;
				}
				xmlOutput.WriteRaw(sForm);
				xmlOutput.WriteEndElement();
			}
		}

		protected virtual string GetMorphTypeInfo(ref string sForm, out string sAlloClass, out string sMorphTypeWs)
		{
			var sType = "stem";              // default
			sAlloClass = "MoStemAllomorph";     // default
			sMorphTypeWs = "en";                // default
			if (sForm.StartsWith("-") && sForm.EndsWith("-"))
			{
				sForm = sForm.Trim('-');
				sType = "infix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("-"))
			{
				sForm = sForm.TrimStart('-');
				sType = "suffix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.EndsWith("-"))
			{
				sForm = sForm.TrimEnd('-');
				sType = "prefix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("~") && sForm.EndsWith("~"))
			{
				sForm = sForm.Trim('~');
				sType = "suprafix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("=") && sForm.EndsWith("="))
			{
				sForm = sForm.Trim('=');
				sType = "simulfix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("="))
			{
				sForm = sForm.TrimStart('=');
				sType = "enclitic";
			}
			else if (sForm.EndsWith("="))
			{
				sForm = sForm.TrimEnd('=');
				sType = "proclitic";
			}
			else if (sForm.StartsWith("*"))
			{
				sType = "bound stem";
			}
			return sType;
		}

		private void ProcessSfmFileNewLogic(XmlTextWriter xmlOutput)
		{
			// Add another xml output file that is similar to phase one but includes
			// additional information that will be/is useful for helping diagnose problems
			// during the import process.
			long errCount = 0;
			try
			{
				var reader = new ByteReader(m_sfmFileName, ref Log);
				byte[] sfmData;
				byte[] badMarkerBytes;
				string currentSfm;
				var mgr = new ImportObjectManager(m_root.Name, this);
				xmlOutput.WriteStartElement(m_root.Name);
				while (reader.GetNextSfmMarkerAndData(out currentSfm, out sfmData, out badMarkerBytes))
				{
					m_sfmLineNumber = reader.FoundLineNumber;
					// If the currentSfm is empty, then this is a case where there is data before the marker
					if (currentSfm.Length == 0)
					{
						// non White Space data found
						var nonWSdata = false;
						// see if it's just white space
						for (var i = 0; i < sfmData.Length && !nonWSdata; i++)
						{
							switch (sfmData[i])
							{
								case 0x0a:  // cr
								case 0x0d:  // lf
								case 0x20:  // space
								case 0x09:  // tab
									break;
								default:
									nonWSdata = true;
									break;
							}
						}
						if (nonWSdata == false)
						{
							continue;
						}
						Log.AddError(m_sfmFileName, m_sfmLineNumber, CharDataBeforeMarkerMessage(sfmData, m_sfmLineNumber));
						// add an entry to the help xml file
						errCount++;
						continue;
					}
					// LT-1926 Ignore all markers that start with underscore (shoebox markers)
					if (currentSfm.StartsWith("_"))
					{
						continue;
					}
					// Make sure the marker is valid for the xml output file [?,<,&, and etc are invalid]
					// If this is a marker that has been tagged to be ignored - then ignore it
					if (m_fieldsToIgnore.ContainsKey(currentSfm))
					{
						continue;
					}
					// Test that the currentSfm exists in the FieldDescriptions table:
					if (!FieldMarkerHashTable.ContainsKey(currentSfm))
					{
						// only log it once [lt-2217 and only if not in the list of ignored fields]
						if (!m_fieldDescriptionsTableNotFound.ContainsKey(currentSfm) && !m_fieldsToIgnore.ContainsKey(currentSfm))
						{
							m_fieldDescriptionsTableNotFound.Add(currentSfm, null);
							Log.AddSFMError(m_sfmFileName, m_sfmLineNumber, currentSfm, string.Format(SfmToXmlStrings.SFM1IsUndefinedOrInvalid, m_sfmLineNumber, currentSfm));
						}
						Log.AddSFMNotDefined(currentSfm);
						continue;
					}
					// log the case where bad bytes are in the marker itself, but continue processing
					if (badMarkerBytes != null && badMarkerBytes.Length > 0)
					{
						Log.AddSFMError(m_sfmFileName, m_sfmLineNumber, currentSfm, string.Format(SfmToXmlStrings.Line0SFM1Count2InvalidBytes, m_sfmLineNumber, currentSfm, badMarkerBytes.Length));
					}
					var currentLocation = HierarchyHashTable[mgr.Current.Name] as ClsHierarchyEntry;
					if (currentLocation == null)
					{
						throw new System.Exception("Current path location is undefined.");
					}
					// see if this sfm is labeled in the hierarchy as 'unique' - only one unique marker per entry
					var uniqueSfm = currentLocation.UniqueFieldsContains(currentSfm);
					// See if the current SFM appears in the BeginFields list of any hierarchy entry:
					var newBeginHierarchyEntry = m_beginMarkerHierarchyEntries[currentSfm] as ClsHierarchyEntry;
					if (newBeginHierarchyEntry == null)
					{
						// see if this is a marker that isn't used in the hierarchy
						if (m_markersNotInHierarchy.ContainsKey(currentSfm))
						{
							Log.AddSFMError(m_sfmFileName, m_sfmLineNumber, currentSfm, string.Format(SfmToXmlStrings.SFM1IsNotUsedInAnyHierarchyItems, m_sfmLineNumber, currentSfm));
							continue;
						}
						// if we are able to add the current sfm to any open objects then do so and continue processing
						if (mgr.AddToOpenObjects(currentSfm, sfmData, m_sfmLineNumber))
						{
							continue;
						}
						if (m_sfmToHierarchy.ContainsKey(currentSfm))   // hierarchy entry that should contain this sfm
						{
							var needed = (ClsHierarchyEntry)m_sfmToHierarchy[currentSfm];
							if (needed.ContainsAncestor(m_root.Name))   // don't add a new entry or base level node
							{
								var errMsg = string.Format(SfmToXmlStrings.SFM1AlreadyExistsIn2MarkerIsIgnored, m_sfmLineNumber, currentSfm, needed.Name);
								Log.AddError(m_sfmFileName, m_sfmLineNumber, errMsg);
								continue;
							}
							// for all other elements - > be hopeful ....
							// (hopeful that there will be the needed other items for this element to follow)
							ImportObject newElement;
							if (mgr.AddNewEntry(needed, out newElement))
							{
								// log the 'OutOfOrder' caution
								// go up the tree from this entry until we get the parent that equals the m_root.name and stop
								while (newElement.Parent.Name != m_root.Name)
								{
									newElement = newElement.Parent;
								}
								string eleMarker;
								byte[] eleData;
								int eleLine;
								newElement.GetCreationMarkerData(out eleLine, out eleMarker, out eleData);
								var entryData = ProcessSFMData(eleMarker, eleData, eleLine);
								entryData = entryData.Replace(System.Environment.NewLine, " "); // remove newlines
								entryData = entryData.Trim();   // remove whitespace
								entryData = $"{eleLine}:{entryData}";
								Log.AddOutOfOrderCaution(entryData, currentSfm, m_sfmLineNumber);

								mgr.WriteOutClosedLexEntries(xmlOutput);
								if (mgr.AddToOpenObjects(currentSfm, sfmData, m_sfmLineNumber))
								{
									continue;
								}
							}
						}
						Debug.WriteLine(" $$$$$$$$$$$$$ Possible logic error - CHECK!!! ++++++++++++++");
					}
					else
					{
						// can not assume it is the current location that this is a begin marker for now, first we have
						//  to get the open entry for this begin marker if one is present.
						uniqueSfm = newBeginHierarchyEntry.UniqueFieldsContains(currentSfm);
						if (mgr.CanUseBeginMarkerOnExistingEntry(newBeginHierarchyEntry.Name, currentSfm, sfmData, m_sfmLineNumber, uniqueSfm))
						{
							continue;   // handled in existing entry
						}
						// We will be opening a new hierarchy level.
						ImportObject newElement;
						if (mgr.AddNewEntry(newBeginHierarchyEntry, out newElement))
						{
							// Only needed here as the 'Entry' is the only one that can't be 'guessed' into creation and its the one
							// that is included in the log.
							newElement.AddCreationMarker(m_sfmLineNumber, currentSfm, sfmData);
							mgr.Current.AddPendingSfmData(currentSfm, sfmData, m_sfmLineNumber, uniqueSfm);
							continue;
						}
						if (mgr.AddNewEntry(newBeginHierarchyEntry.Name, currentSfm, sfmData, m_sfmLineNumber, uniqueSfm, out newElement))
						{
							continue;
						}
						Debug.WriteLine(" $$$$$$$$$$$$$ Possible logic error 2 - CHECK!!! ++++++++++++++");
					}
					// This is where we add the SFM to the XML output. It is common to all logic paths
					// which don't give errors.
					mgr.Current.AddPendingSfmData(currentSfm, sfmData, m_sfmLineNumber, uniqueSfm);
				}
				mgr.Flush(xmlOutput);
			}
			catch (Exception e)
			{
				Debug.WriteLine("Exception: " + e.Message);
			}
		}

		private static string CharDataBeforeMarkerMessage(byte[] sfmData, int nLineNumber)
		{
			string emsg;
			if (sfmData.Length > 50)
			{
				emsg = string.Format(SfmToXmlStrings.X1CharactersFoundBeforeMarker, nLineNumber, sfmData.Length);
			}
			else
			{
				try
				{
					emsg = string.Format(SfmToXmlStrings.Data1FoundBeforeMarker, nLineNumber, MultiToWide(sfmData, Encoding.UTF8).Replace(Environment.NewLine, " ").Trim());
				}
				catch
				{
					emsg = string.Format(SfmToXmlStrings.X1CharactersSomeInvalidFoundBeforeMarker, nLineNumber, sfmData.Length);
				}
			}
			return emsg;
		}

		private sealed class ClsStringToOrFromBytes
		{
			byte[] m_data;
			int m_byteLength;
			string m_string;
			private const byte kLeft1BitMask = 0x80;
			private const byte kLeft2BitsMask = 0xC0;
			private const byte kLeft3BitsMask = 0xE0;
			private const byte kLeft4BitsMask = 0xF0;
			private const byte kLeft5BitsMask = 0xF8;
			Queue<DP> m_invalidBytes;
			public static int MaxInvalidBytes => 10;

			public ClsStringToOrFromBytes(byte[] data)
			{
				m_data = data;
				m_byteLength = data.Length;
				m_invalidBytes = new Queue<DP>(MaxInvalidBytes);
			}

			public ClsStringToOrFromBytes(string str)
			{
				m_string = str;
				m_invalidBytes = new Queue<DP>(MaxInvalidBytes);
			}

			public string InvalidByteString()
			{
				if (m_invalidBytes.Count == 0)
				{
					return string.Empty;
				}
				var sb = new StringBuilder(SfmToXmlStrings.Index_Byte___);
				while (m_invalidBytes.Count > 0)
				{
					var data = m_invalidBytes.Dequeue();
					sb.Append($"{data.Offset}:0x{System.Convert.ToString(data.Data, 16)}");
					if (m_invalidBytes.Count > 0)
					{
						sb.Append(", ");
					}
				}
				return sb.ToString();
			}

			/// <summary>
			/// Test to see if the passed in byte is a valid start byte for an UTF8 data point.
			/// </summary>
			private bool IsValidBeginByte(byte b)
			{
				if ((b & kLeft1BitMask) == 0)
				{
					return true;    // sequenceLen = 1
				}
				if ((b & kLeft3BitsMask) == kLeft2BitsMask)
				{
					return true;    // sequenceLen = 2
				}
				if ((b & kLeft4BitsMask) == kLeft3BitsMask)
				{
					return true;    // sequenceLen = 3
				}
				if ((b & kLeft5BitsMask) == kLeft4BitsMask)
				{
					return true;    // sequenceLen = 4
				}
				return false;
			}

			/// <summary>
			/// Determine if the data is UTF8 encoded
			/// </summary>
			/// <remarks>This algorithm uses the pattern described in the Unicode book for
			/// UTF-8 data patterns.  It has been customized.</remarks>
			public bool IsUTF8String(out byte badCodePoint, out int bytePosition, out int badBytes)
			{
				var foundANYError = false;
				// If there is no data, it is not UTF8
				badCodePoint = 0;
				bytePosition = 0;
				badBytes = 0;
				if (m_data.Length < 1)
				{
					return false;
				}
				var sequenceLen = 1;
				// look through the buffer but stop 10 bytes before so we don't run off the
				// end while checking
				for (var i = 0; i < m_data.Length; i += sequenceLen)
				{
					var by = m_data[i];
					var foundError = false;
					// If the leftmost bit is 0, then this is a 1-byte character
					if ((by & kLeft1BitMask) == 0)
					{
						sequenceLen = 1;
					}
					else if ((by & kLeft3BitsMask) == kLeft2BitsMask)
					{
						// If the byte starts with 110, then this will be the first byte
						// of a 2-byte sequence
						sequenceLen = 2;
						// if the second byte does not start with 10 then the sequence is invalid
						if (m_byteLength <= i + 1 || (m_data[i + 1] & kLeft2BitsMask) != 0x80)
						{
							badCodePoint = by;
							bytePosition = i + 1;   // not zero based, but 1 based for user
							foundError = true;
						}
					}
					else if ((by & kLeft4BitsMask) == kLeft3BitsMask)
					{
						// If the byte starts with 1110, then this will be the first byte of
						// a 3-byte sequence
						sequenceLen = 3;
						if ((m_byteLength <= i + 1 || (m_data[i + 1] & kLeft2BitsMask) != 0x80) || (m_byteLength <= i + 2 || (m_data[i + 2] & kLeft2BitsMask) != 0x80))
						{
							badCodePoint = by;
							bytePosition = i + 1;   // not zero based, but 1 based for user
							foundError = true;
						}
					}
					else if ((by & kLeft5BitsMask) == kLeft4BitsMask)
					{
						// if the byte starts with 11110, then this will be the first byte of
						// a 4-byte sequence
						sequenceLen = 4;
						if (m_byteLength <= i + 1 || (m_data[i + 1] & kLeft2BitsMask) != 0x80 || m_byteLength <= i + 2 || (m_data[i + 2] & kLeft2BitsMask) != 0x80
						    || m_byteLength <= i + 3 || (m_data[i + 3] & kLeft2BitsMask) != 0x80)
						{
							badCodePoint = by;
							bytePosition = i + 1;   // not zero based, but 1 based for user
							foundError = true;
						}
					}
					else
					{
						badCodePoint = by;
						bytePosition = i + 1;   // not zero based, but 1 based for user
						foundError = true;
					}
					if (foundError)
					{
						if (m_invalidBytes.Count < MaxInvalidBytes)
						{
							m_invalidBytes.Enqueue(new DP(bytePosition, badCodePoint));
						}
						Debug.WriteLine("badData: index=" + System.Convert.ToString(i + 1) + " ");
						var bytesUntilNextCodePoint = i;
						// now search until we find a valid begin byte for utf8 sequence
						while (++i < m_byteLength && IsValidBeginByte(m_data[i]) == false)
						{
							if (m_invalidBytes.Count < MaxInvalidBytes)
							{
								m_invalidBytes.Enqueue(new DP(i + 1, m_data[i]));
							}
						}
						sequenceLen = 0;
						bytesUntilNextCodePoint = i - bytesUntilNextCodePoint;
						badBytes += bytesUntilNextCodePoint;    // bytes including bytes to get to start byte
						foundANYError = true;
					}
				}
				return !foundANYError;
			}


			public string String()
			{
				if (m_string == null)
				{
					var utf8Decoder = Encoding.UTF8.GetDecoder();
					var charCount = utf8Decoder.GetCharCount(m_data, 0, m_byteLength);
					var chars = new char[charCount];
					utf8Decoder.GetChars(m_data, 0, m_byteLength, chars, 0);
					m_string = new string(chars);
				}
				return m_string;
			}

			public byte[] ToBytes()
			{
				if (m_data == null)
				{
					var utf8 = new UTF8Encoding(false, true);
					m_byteLength = utf8.GetByteCount(m_string.ToCharArray());
					m_data = new byte[m_byteLength];
					utf8.GetBytes(m_string, 0, m_string.Length, m_data, 0);
				}
				return m_data;
			}

			public int ByteLength
			{
				get { ToBytes(); return m_byteLength; }
			}

			private sealed class DP
			{
				internal int Offset { get; }
				internal byte Data { get; }

				internal DP(int o, byte d)
				{
					Offset = o;
					Data = d;
				}
			}
		}

		private sealed class ClsFoundInFieldMarker
		{
			// Constructor for a Begin Marker
			public ClsFoundInFieldMarker(string marker, ClsInFieldMarker inFieldMarker)
			{
				Marker = marker;
				IsBegin = true;
				ClsInFieldMarker = inFieldMarker;
			}

			// Constructor for an End Marker
			public ClsFoundInFieldMarker(string marker)
			{
				Marker = marker;
				IsBegin = false;
			}

			public ClsInFieldMarker ClsInFieldMarker { get; }

			public string Marker { get; }

			public int Length => Marker.Length;

			public bool IsBegin { get; }

			public string LanguageEncoding => ClsInFieldMarker.Language;

			public string ElementName => ClsInFieldMarker.ElementName;

			public bool HasLangEncoding => !string.IsNullOrEmpty(ClsInFieldMarker.Language);

			public bool ContainsEndMarker(string endMarker)
			{
				return ClsInFieldMarker.ContainsEndMarker(endMarker);
			}
		}

		private sealed class AutoFieldInfo
		{
			internal string ClassName { get; }
			internal string SfmName { get; }
			internal string FWDest { get; }
			internal string KEY => $"{ClassName}_{SfmName}";

			internal AutoFieldInfo(string cls, string sfm, string dest)
			{
				ClassName = cls;
				SfmName = sfm;
				FWDest = dest;
			}
		}

		private sealed class ImportObjectManager
		{
			private ImportObject m_root;
			private Converter m_converter;
			private Hashtable m_OpenNodes;      // hash of nodes currently open on the tree: key="name"(ex:"entry") value="arraylist of ImportObjects"

			internal ImportObjectManager(string rootName, Converter conv)
			{
				m_root = new ImportObject(rootName, null);
				Current = m_root;
				m_converter = conv;
				m_OpenNodes = new Hashtable();
				var rootLevel = new ArrayList
				{
					m_root
				};
				m_OpenNodes.Add(rootName, rootLevel);

			}

			private void AddNewObject(ImportObject newEntry)    // will set current and add to m_opennodes hash
			{
				// make sure there aren't any children nodes of the new entry at the same level as the newEntry
				RemoveChildrenForNewImportObject(newEntry.Parent, newEntry.Name, m_converter.m_hierarchyChildren, newEntry);
				// make sure there aren't any open like nodes at the same level
				foreach (ImportObject child in newEntry.Parent.Children)
				{
					if (child == newEntry)  // don't close the one we just added!
					{
						continue;
					}
					if (child.Name == newEntry.Name)
					{
						if (!child.Closed)
						{
							CloseImportObject(child);
						}
					}
				}
				Current = newEntry;
				if (m_OpenNodes.ContainsKey(newEntry.Name))
				{
					var nodes = m_OpenNodes[newEntry.Name] as ArrayList;
					nodes.Add(newEntry);
				}
				else
				{
					var nodes = new ArrayList
					{
						newEntry
					};
					m_OpenNodes.Add(newEntry.Name, nodes);
				}
			}

			internal ImportObject Current { get; private set; }

			internal void WriteOutClosedLexEntries(System.Xml.XmlTextWriter xmlOutput)
			{
				foreach (ImportObject child in m_root.Children)
				{
					if (child.Depth == 1 && child.Closed)   // right level and closed for 'business'
					{
						child.Flush(m_converter, xmlOutput);
						RemoveNodeAndChildren(child);
						break;  // only can have one entry open at a time
					}
				}
			}

			private static void RemoveNodeAndChildren(ImportObject node)
			{
				// just remove this node from it's parent and it will be gone...
				node.Parent?.RemoveChild(node);
			}

			internal void Flush(XmlTextWriter xmlOutput)
			{
				foreach (ImportObject child in m_root.Children)
				{
					child.Flush(m_converter, xmlOutput);
				}
				xmlOutput.WriteEndElement();
			}

			private void RemoveObjFromOpenList(ImportObject obj)
			{
				// remove this node from the hash of nodes
				if (m_OpenNodes.ContainsKey(obj.Name))
				{
					var objs = (ArrayList)m_OpenNodes[obj.Name];
					objs.Remove(obj);
					if (objs.Count == 0)    // empty now, remove from hash
					{
						m_OpenNodes.Remove(obj.Name);
					}
				}
			}

			private void CloseImportObject(ImportObject obj)
			{
				obj.MakeClosed();   // will also close all child obj's
				RemoveObjFromOpenList(obj);
				foreach (ImportObject child in obj.Children)
				{
					CloseImportObject(child);
				}
			}

			internal bool AddNewEntry(ClsHierarchyEntry newHierarchy, out ImportObject addedNode)
			{
				// get the ancestors of this hierarchy
				// get the best ancestor of the currently open ones
				// if none found, look for the ancestors of the ancestors - repeat until a best ancestor is found
				//  once found, back down the list of ancestors and add them
				addedNode = null;
				var name = newHierarchy.Name;
				var leaf = new TreeNode(name);
				var nodes = new ArrayList
				{
					leaf
				};
				ImportObject bestParent = null;
				TreeNode foundNode = null;
				var done = false;
				while (!done)
				{
					var nextLevel = new ArrayList();
					foreach (TreeNode node in nodes)
					{
						var possibleParents = new ArrayList();
						GetAncestorsOf(node.Name, ref possibleParents);
						bestParent = GetBestOpenParent(possibleParents);
						if (bestParent != null)
						{
							done = true;
							foundNode = node;
							break;
						}
						foreach (string ancestor in possibleParents)
						{
							var posparent = new TreeNode(ancestor);
							node.AddAncestor(posparent);
							nextLevel.Add(posparent);
						}
					}
					nodes = nextLevel;
				}
				// this is a level one node, or requires one to be created so bump the counter
				if (bestParent.Depth == 0)
				{
					m_converter.LevelOneElements++;
				}
				while (foundNode != null)
				{
					var newEntry = new ImportObject(foundNode.Name, bestParent);
					addedNode = newEntry;
					bestParent.AddChild(newEntry);
					// will set current and add to m_opennodes hash
					AddNewObject(newEntry);
					// set up variables for next level in tree
					bestParent = newEntry;
					foundNode = foundNode.Leaf;
				}
				return true;
			}


			// Any class that has children (Entry, Sense, Subentry), when that class is
			//  created/started any open classes at the current level that are also
			//  children of the new class will be closed.
			//
			// Deterministic - Describes an algorithm in which the correct next step
			//  depends only on the current state
			private void RemoveChildrenForNewImportObject(ImportObject parent, string importClassName, Hashtable childrenTable, ImportObject objNotToRemove)
			{
				// Get a list of children names of the new 'importClassName' and then
				// close any of the children nodes of the 'parent' if they are the same.
				if (childrenTable.ContainsKey(importClassName))
				{
					var childNames = (ArrayList)childrenTable[importClassName];
					// small performance enhancement by getting rid of the foreach and using a for loop
					var count = parent.ChildrenCount;
					for (var i = 0; i < count; i++)
					{
						var child = parent.ChildAt(i);
						if (child == objNotToRemove)
						{
							continue;
						}
						if (!child.Closed && childNames.Contains(child.Name))   // found child node that should be closed
						{
							CloseImportObject(child);   // don't be 'open' for anymore markers
						}
					}
				}
			}

			private void SearchChildrenForMatches(ImportObject start, string sfm, ref ArrayList possibleObjects)
			{
				// small performance enhancement by getting rid of the foreach and using a for loop
				var count = start.ChildrenCount;
				for (var i = 0; i < count; i++)
				{
					var child = start.ChildAt(i);
					if (child.Closed == false && (child.CanAddSFM(sfm, m_converter) || child.CanAddSFMasAutoField(sfm, m_converter)))
					{
						possibleObjects.Add(child); // save it as a possible resting place for this sfm and data
					}
					SearchChildrenForMatches(child, sfm, ref possibleObjects);
				}
			}

			private void GetAncestorsOf(string childName, ref ArrayList possibleObjects)
			{
				if (m_converter.HierarchyHashTable.ContainsKey(childName))
				{
					var currentLocation = (ClsHierarchyEntry)m_converter.HierarchyHashTable[childName];
					foreach (var ancestor in currentLocation.Ancestors)
					{
						possibleObjects.Add(ancestor);
					}
				}
				else
				{
					possibleObjects.Clear();
				}
			}

			private void SearchHierarchyForParentsOf(ImportObject start, string childName, ref ArrayList possibleObjects)
			{
				if (m_converter.m_hierarchyChildren.ContainsKey(start.Name))
				{
					var children = (ArrayList)m_converter.m_hierarchyChildren[start.Name];
					if (children.Contains(childName))
					{
						possibleObjects.Add(start.Name);
					}
				}
				foreach (ImportObject child in start.Children)
				{
					SearchHierarchyForParentsOf(child, childName, ref possibleObjects);
				}
			}

			// walk the tree of open objects and get the lowest one (start at leaves and work up would be more efficient)
			private ImportObject GetBestOpenParent(ArrayList possibleParents)
			{
				ImportObject rval = null;
				// use the open nodes hash to find all the open parents and pick the deepest one
				foreach (string name in possibleParents)
				{
					if (m_OpenNodes.ContainsKey(name))
					{
						var objs = (ArrayList)m_OpenNodes[name];
						foreach (ImportObject posParent in objs)
						{
							if (posParent.Closed)
							{
								continue;   // can't pick a closed parent...
							}
							if (rval == null || rval.Depth < posParent.Depth)
							{
								rval = posParent;
							}
						}
					}
				}
				return rval;
			}

			/// <summary>Rules for knowing where to add new classes (Entry, Sense, ...)
			/// This set of rules is for adding a new entry (sfm was begin marker)
			///
			/// 1 - if the current entry starts with this sfm
			///		+ if it doesn't have one then use it here as this must have been 'guessed' to start.
			///		  else the current entry can't take this sfm, close it and add a new one
			///	2 - find all possible parents for this element
			/// 3 - if one or more of the parents are open in the tree
			///		+ use the parent with the largest depth and add the child there
			///		else
			///		+ create a parent tree for this entry (root:entry:sense:...)
			///		+ use the current open entries and remove from root as far as possible
			///			what remains should be added and the child added at the end
			///
			///	Rules for determining where to add a marker when it isn't a begin marker
			///
			///	1 - if the current entry can take the marker, add it here
			///	2 - starting at the deepest level, look for an entry that can take the marker
			///		+ if an entry is found, add it here
			///		  else
			///			pretend it starts an entry and process as a begin marker
			/// </summary>
			public bool CanUseBeginMarkerOnExistingEntry(string entryName, string sfm, byte[] sfmData, int line, bool isUnique)
			{
				if (m_OpenNodes.ContainsKey(entryName))
				{
					ImportObject rval = null;
					var objs = (ArrayList)m_OpenNodes[entryName];
					foreach (ImportObject posParent in objs)
					{
						if (posParent.Closed)
						{
							continue;       // just to make sure there aren't any closed items in the open list
						}
						if (rval == null || rval.Depth < posParent.Depth)
						{
							rval = posParent;
						}
					}
					if (rval != null && rval.CanAddBeginSFM(sfm, m_converter))
					{
						rval.AddBeginSFM(sfm, sfmData, line, m_converter);
						Current = rval;
						return true;
					}
				}
				return false;
			}


			/// <summary>
			/// Once we know that we need to create a new entry/import class, we need to know where
			/// to insert the new entry.  We used to be linear (stack based) but are now tree based so
			/// a more robust routine is now needed.
			/// This will make the newly added entry the 'current' as well as closing any that need
			/// to be closed.
			/// </summary>
			public bool AddNewEntry(string name, string sfm, byte[] sfmData, int line, bool isUnique, out ImportObject addedEntry)
			{
				addedEntry = null;
				// find all the currently open parent objects for this new entry
				var possibleParents = new ArrayList();
				SearchHierarchyForParentsOf(m_root, name, ref possibleParents);
				var bestParent = GetBestOpenParent(possibleParents);
				if (bestParent != null)
				{
					var newEntry = new ImportObject(name, bestParent);
					addedEntry = newEntry;
					bestParent.AddChild(newEntry);
					AddNewObject(newEntry);    // will set current and add to m_opennodes hash
					newEntry.AddPendingSfmData(sfm, sfmData, line, isUnique);
					return true;
				}
				return false;
			}

			public bool AddToOpenObjects(string sfm, byte[] sfmData, int line)
			{
				// first check to see if it can be added to the current element
				if (Current.AddSFM(sfm, sfmData, line, m_converter))
				{
					return true;
				}
				// not added
				var retVal = false;
				// can't add to current, so starting from the root - check the open items
				var start = m_root;
				var possibleObjects = new ArrayList();
				SearchChildrenForMatches(start, sfm, ref possibleObjects);
				if (possibleObjects.Count > 1)
				{
					// more than one match for this sfm - now what ...
					// check each one and see if it will go there
					possibleObjects.Reverse();
					// reversed the order of the items so that it is deepest first,
					// ex: a subentry would now be tested before the entry - needed for context
					foreach (ImportObject found in possibleObjects)
					{
						if (!found.AddSFM(sfm, sfmData, line, m_converter))
						{
							if (!found.AddSFMasAutoField(sfm, sfmData, line, m_converter))
							{
								continue;
							}
							retVal = true;
							break;
						}
						retVal = true;
						break;
					}
					if (!retVal)
					{
						Debug.WriteLine("******** ERROR??? ****************");
					}
				}
				else
				{
					// only one place to add this one
					var found = (ImportObject)possibleObjects[0];
					if (!found.AddSFM(sfm, sfmData, line, m_converter))
					{
						if (!found.AddSFMasAutoField(sfm, sfmData, line, m_converter))
						{
							Debug.WriteLine("******** ERROR??? ****************");
						}
						else
						{
							retVal = true;
						}
					}
					else
					{
						retVal = true;
					}
				}
				return retVal;
			}

			private sealed class TreeNode
			{
				private ArrayList m_ancestors;
				public TreeNode Leaf { get; private set; }
				public string Name { get; }

				public TreeNode(string name)
				{
					Name = name;
					m_ancestors = new ArrayList();
				}

				public void AddAncestor(TreeNode ancestor)
				{
					m_ancestors.Add(ancestor);
					ancestor.Leaf = this;
				}
			}
		}

		/// <summary>
		/// Used to contain hierarchy-level elements plus a list of in-use SFMs at each level
		/// </summary>
		private class ClsPathObject
		{
			private Hashtable m_inUseSfms;  // hashtable of sfms for this object
			private ArrayList m_pendingSfms;    // list of PendingSfmData to be output still

			/// <summary>
			/// Create a ClsPathObject with the given name
			/// </summary>
			protected ClsPathObject(string name)
			{
				m_inUseSfms = new Hashtable();
				Name = name;
				m_pendingSfms = new ArrayList();
				AlreadyContainsUniqueSfm = false;
			}

			/// <summary>
			/// This method is used to add markers and data to an object that is open and on the stack,
			/// but is not the topmost and so the data can't be output at this time.  The data will be
			/// flushed when the object is removed.
			/// </summary>
			public void AddPendingSfmData(string marker, byte[] data, int line, bool unique)
			{
				m_pendingSfms.Add(new PendingSfmData(marker, data, line));
				// DH: this really should be added to the 'used' list also
				AddSfm(marker, unique);
			}

			/// <summary>
			/// If there are markers and their data waiting (pending) to be output then flush
			/// them out before removing this object.
			/// </summary>
			/// <param name="convObj">used for the ProcessSFMData method</param>
			/// <param name="xmlOutput">output file</param>
			public void FlushPendingSfmData(Converter convObj, XmlTextWriter xmlOutput)
			{
				foreach (PendingSfmData pendingObj in m_pendingSfms)
				{
					convObj.ProcessSFMandData(pendingObj.Marker, pendingObj.Data, pendingObj.LineNumber, xmlOutput);
				}
			}

			/// <summary>
			/// Return the path object name
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// Add a sfm to the list of in use sfm's
			/// </summary>
			protected void AddSfm(string sfm, bool unique)
			{
				// only add if it's not already in the list
				if (!m_inUseSfms.ContainsKey(sfm))
				{
					m_inUseSfms.Add(sfm, null);
					AlreadyContainsUniqueSfm |= unique;
				}
			}

			protected bool AlreadyContainsUniqueSfm { get; private set; }

			/// <summary>
			/// return true if the sfm is in the list of used sfm's
			/// </summary>
			protected bool ContainsSfm(string sfm)
			{
				return m_inUseSfms.ContainsKey(sfm);
			}

			private sealed class PendingSfmData
			{
				internal PendingSfmData(string marker, byte[] data, int line)
				{
					Marker = marker;
					Data = data;
					LineNumber = line;
				}

				internal string Marker { get; }
				internal byte[] Data { get; }
				internal int LineNumber { get; }
			}
		}

		private sealed class ImportObject : ClsPathObject
		{
			private string m_marker;                // marker that created this ImportObject
			private int m_markerLine;               // line number of this marker
			private byte[] m_markerData;            // marker data for the marker that created this ImportObject

			internal ImportObject(string name, ImportObject parent)
				: base(name)
			{
				m_marker = string.Empty;
				m_markerData = null;
				m_markerLine = -1;
				Parent = parent;
				ChildrenList = new List<ImportObject>();
				ChildrenCount = 0;
				Closed = false;
				Depth = parent?.Depth + 1 ?? 0;
			}

			internal void AddCreationMarker(int line, string marker, byte[] markerData)
			{
				m_markerLine = line;
				m_marker = marker;
				m_markerData = markerData;
			}

			internal void GetCreationMarkerData(out int line, out string marker, out byte[] markerdata)
			{
				line = m_markerLine;
				marker = m_marker;
				markerdata = m_markerData;
			}

			internal void AddChild(ImportObject child)
			{
				ChildrenList.Add(child);
				ChildrenCount++;
			}

			internal bool RemoveChild(ImportObject child)
			{
				if (ChildrenList.Contains(child))
				{
					ChildrenList.Remove(child);
					ChildrenCount--;
					return true;
				}
				return false;
			}

			internal ICollection Children => ChildrenList;

			private List<ImportObject> ChildrenList { get; }

			internal ImportObject ChildAt(int index)
			{
				return ChildrenList[index];
			}

			internal int ChildrenCount { get; private set; }

			private string NameLong
			{
				get
				{
					var OorC = "O:";
					if (Closed)
					{
						OorC = "C:";
					}
					return $"{Name}<{OorC + GetHashCode()}>";
				}
			}

			internal ImportObject Parent { get; }

			internal int Depth { get; }

			private ClsPathObject Self => this;

			internal bool Closed { get; private set; }

			internal void MakeClosed()
			{
				if (!Closed)
				{
					Closed = true;
				}
				foreach (var child in ChildrenList)
				{
					child.MakeClosed();
				}
			}

			internal void Flush(Converter convObj, XmlTextWriter xmlOutput)
			{
				// open the current node for output
				xmlOutput.WriteStartElement(Self.Name);
				// output the pending sfm Data
				Self.FlushPendingSfmData(convObj, xmlOutput);
				// output children
				foreach (var child in ChildrenList)
				{
					child.Flush(convObj, xmlOutput);
				}
				// close this current node
				xmlOutput.WriteEndElement();

				Closed = true;
			}

			internal bool CanAddBeginSFM(string sfm, Converter converter)
			{
				if (Closed)
				{
					return false;
				}
				var currentLocation = (ClsHierarchyEntry)converter.HierarchyHashTable[Name];
				var uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
				var appearsInBegin = currentLocation.BeginFieldsContains(sfm);
				var usedAlready = ContainsSfm(sfm);
				var noUniqueProblem = !uniqueSfm || !AlreadyContainsUniqueSfm;
				return appearsInBegin && !usedAlready && noUniqueProblem;
			}

			internal bool CanAddSFM(string sfm, Converter converter)
			{
				if (Closed)
				{
					return false;
				}
				// The current SFM does not start a new hierarchy level. See if we can use it at
				// the current location:
				var currentLocation = (ClsHierarchyEntry)converter.HierarchyHashTable[Name];
				var uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
				var appearsInMulti = currentLocation.MultiFieldsContains(sfm);
				var appearsInAdditional = currentLocation.AdditionalFieldsContains(sfm);
				var usedAlready = ContainsSfm(sfm);
				var noUniqueProblem = !uniqueSfm || !AlreadyContainsUniqueSfm;
				return appearsInMulti || appearsInAdditional && !usedAlready && noUniqueProblem;
			}

			internal bool AddSFM(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddSFM(sfm, converter))
				{
					var currentLocation = (ClsHierarchyEntry)converter.HierarchyHashTable[Name];
					var uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
					AddSfm(sfm, uniqueSfm);
					AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
					return true;
				}
				return false;
			}

			internal void AddBeginSFM(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddBeginSFM(sfm, converter))
				{
					var currentLocation = (ClsHierarchyEntry)converter.HierarchyHashTable[Name];
					var uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
					AddSfm(sfm, uniqueSfm);
					AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
				}
			}

			internal bool CanAddSFMasAutoField(string sfm, Converter converter)
			{
				if (Closed)
				{
					return false;
				}
				// if it's an autofield, see if there's a match at this 'class' / entry
				var possibleAuto = converter.GetFieldDescription(sfm);
				if (possibleAuto != null && possibleAuto.IsAutoImportField)
				{
					// if there's a autofield for this class, we can use the sfm here
					if (converter.m_autoFieldsPossible.ContainsKey(this.Name))
					{
						return true;
					}
				}
				return false;
			}

			internal bool AddSFMasAutoField(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddSFMasAutoField(sfm, converter))
				{
					var currentLocation = (ClsHierarchyEntry)converter.HierarchyHashTable[Name];
					var uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
					AddSfm(sfm, uniqueSfm);
					AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
					// add it to the list of autofields used
					var fwDest = converter.m_autoFieldsPossible[Name] as string;
					var afi = new AutoFieldInfo(Name, sfm, fwDest);
					if (!converter.m_autoFieldsUsed.ContainsKey(afi.KEY))
					{
						converter.m_autoFieldsUsed.Add(afi.KEY, afi);
						ArrayList sfms;
						if (converter.m_autoFieldsBySFM.ContainsKey(afi.SfmName))
						{
							sfms = converter.m_autoFieldsBySFM[afi.SfmName] as ArrayList;
						}
						else
						{
							sfms = new ArrayList();
							converter.m_autoFieldsBySFM.Add(afi.SfmName, sfms);
						}
						sfms.Add(afi);
					}
					return true;
				}
				return false;
			}
		}
	}
}