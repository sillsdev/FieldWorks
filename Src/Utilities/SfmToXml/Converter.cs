//#define TracingOutput	// used for output in the debug window

using System;
using System.Collections;
using System.Collections.Generic;
using ECInterfaces;
using SilEncConverters40;

namespace Sfm2Xml
{


	/// <summary>
	/// Used to contain hierarchy-level elements plus a list of in-use SFMs at each level
	/// </summary>
	public class ClsPathObject
	{
		protected class PendingSfmData
		{
			public PendingSfmData(string marker, byte[] data, int line)
			{
				m_marker = marker;
				m_data = data;
				m_line = line;
			}

			private string m_marker;	// marker
			private byte[] m_data;	// data for marker
			private int m_line;	// line number in src file

			public string Marker	{ get { return m_marker; } }
			public byte[] Data		{ get { return m_data; } }
			public int LineNumber	{ get { return m_line; } }
		}

		private Hashtable m_InUseSfms;	// hashtable of sfms for this object
		private string m_HierarchyLevelName;	// name of object
		private ArrayList m_pendingSfms;	// list of PendingSfmData to be output still
		private bool m_containsUniqueSfm;	// true if this already contains a unique sfm
//		private bool m_isClosed;			// true if this entry is unable to accept data: IE it's closed

		/// <summary>
		/// Create a ClsPathObject with the given name
		/// </summary>
		/// <param name="name"></param>
		public ClsPathObject(string name)
		{
			m_InUseSfms = new Hashtable();
			m_HierarchyLevelName = name;
			m_pendingSfms = new ArrayList();
			m_containsUniqueSfm = false;
//			m_isClosed = false;
		}

		/// <summary>
		/// This method is used to add markers and data to an object that is open and on the stack,
		/// but is not the topmost and so the data can't be output at this time.  The data will be
		/// flushed when the object is removed.
		/// </summary>
		/// <param name="marker"></param>
		/// <param name="data"></param>
		public void AddPendingSfmData(string marker, byte[] data, int line, bool unique)
		{
			m_pendingSfms.Add(new PendingSfmData(marker, data, line));
			// DH: this really should be added to the 'used' list also
			AddSfm(marker, unique);
		}

		public string GetPendingSFMs()
		{
			System.Text.StringBuilder results = new System.Text.StringBuilder();
			foreach(PendingSfmData pendingObj in m_pendingSfms)
			{
				results.Append(pendingObj.Marker + " line#:" + pendingObj.LineNumber.ToString() + ", ");
			}
			return results.ToString();
		}
		/// <summary>
		/// If there are markers and their data waiting (pending) to be output then flush
		/// them out before removing this object.
		/// </summary>
		/// <param name="convObj">used for the ProcessSFMData method</param>
		/// <param name="xmlOutput">output file</param>
		public void FlushPendingSfmData(Converter convObj, System.Xml.XmlTextWriter xmlOutput)
		{
			foreach(PendingSfmData pendingObj in m_pendingSfms)
			{
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("sfm Data: " + pendingObj.Marker + " line#:" + pendingObj.LineNumber.ToString());
#endif
				convObj.ProcessSFMandData(pendingObj.Marker, pendingObj.Data, pendingObj.LineNumber, xmlOutput);
			}
		}

		/// <summary>
		/// Return the path object name
		/// </summary>
		public string Name
		{
			get { return m_HierarchyLevelName; }
		}

		/// <summary>
		/// Add a sfm to the list of inuse sfm's
		/// </summary>
		/// <param name="sfm"></param>
		public void AddSfm(string sfm, bool unique)
		{
			// only add if it's not already in the list
			if (!m_InUseSfms.ContainsKey(sfm))
			{
				m_InUseSfms.Add(sfm, null);
				m_containsUniqueSfm |= unique;
			}
		}

		public bool AlreadContainsUniqueSfm { get { return m_containsUniqueSfm; }}
//		public bool IsClosed
//		{
//			get { return m_isClosed;}
//			set { m_isClosed = value; }
//		}

		/// <summary>
		/// return true if the sfm is in the list of used sfm's
		/// </summary>
		/// <param name="sfm"></param>
		/// <returns></returns>
		public bool ContainsSfm(string sfm)
		{
			return m_InUseSfms.ContainsKey(sfm);
		}

	}


	// Making all previously private methods that we want to test protected.

	/// <summary>
	/// Summary description for Converter.
	/// </summary>
	public class Converter
	{
		/// <summary>
		/// Public class that handles the SFM transformation to utf8 xml format with encoding applied
		///
		/// methods called:
		///		AddPossibleAutoField
		///		Convert
		///
		/// This class requires that the map file already exist.  So this is just a black box that
		/// acts on the input files that it's given - with Convert being the main entry point.
		///
		/// </summary>
		public Converter()
		{
			m_Languages = new Hashtable(); // maps the langId to a ClsLanguage object
//			m_LangIdToXmlLang = new Hashtable(); // maps the 'id' of a langDef element to the 'xml:lang'
			m_LangsToIgnore = new Hashtable();	// langDef 'id' values to ignore
			m_FieldsToIgnore = new Hashtable(); // fields that are of a 'lang' that is to be ignored
			m_Hierarchy = new Hashtable();
			m_HierarchyChildren = new Hashtable();	// key=string, value=arraylist containing children string names (future StringDictionary)
			m_FieldDescriptionsTable = new Hashtable();
			m_FieldDescriptionsTableNotFound = new Hashtable();
			m_InFieldMarkers = new Hashtable();
			m_converters = new EncConverters();
			m_BeginMarkerHierarchyEntries = new Hashtable();
			m_sfmToHierarchy = new Hashtable();
			m_MarkersNotInHierarchy = new Hashtable();
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
			return false;	// not added
		}

		private int m_NumElements;	// number of elements processed
		public int LevelOneElements
		{
			get { return m_NumElements; }
		}

		// Container for languages section of map file:
		protected Hashtable m_Languages;
//		private Hashtable m_LangIdToXmlLang;	// maps the 'id' of a langDef element to the 'xml:lang'
		private Hashtable m_LangsToIgnore;	// langDef 'id' values to ignore
		private Hashtable m_FieldsToIgnore;	// fields to ignore from the above langs

		// Container for hierarchy section of map file:
		private Hashtable m_Hierarchy;
		private Hashtable m_HierarchyChildren;		// keeps track of children for each hierarchy entry
		// Container for FieldDescriptions section of map file:
		private Hashtable m_FieldDescriptionsTable;
		private Hashtable m_FieldDescriptionsTableNotFound;
		// Container for InfieldMarkers section of map file:
		private Hashtable m_InFieldMarkers;

		// Counter to limit the number of error reports made for illegal characters.
		private int m_cIllegalCodes = 0;

		private Hashtable m_autoFieldsPossible; // key=string[className], value=string[fwid]
		private Hashtable m_autoFieldsUsed;	// key=string["classname_sfmName"], value=AutoFieldInfo
		private Hashtable m_autoFieldsBySFM; // key=string[sfmName], value=ArrayList of AutoFieldInfo

		protected string m_topAnalysisWS;	// top analysis ws; ex: "en", ...

		public class AutoFieldInfo
		{
			public string className;	// class that the marker is in
			public string sfmName;		// marker
			public string fwDest;		// destination in fieldworks {fwid}
			public AutoFieldInfo(string cls, string sfm, string dest)
			{
				className = cls;
				sfmName = sfm;
				fwDest = dest;
			}
			public string KEY { get { return className + "_" + sfmName; }}
		}

		protected ClsFieldDescription GetFieldDescription(string key)
		{
			if (m_FieldDescriptionsTable.ContainsKey(key))
				return m_FieldDescriptionsTable[key] as ClsFieldDescription;
			return null;
		}
		// accessors for derived classes to get to map read data
		protected Hashtable LanguagesHashTable { get { return m_Languages; }}
		protected Hashtable HierarchyHashTable { get { return m_Hierarchy; }}
		protected Hashtable FieldMarkerHashTable { get { return m_FieldDescriptionsTable; }}
		protected Hashtable InFieldMarkerHashTable { get { return m_InFieldMarkers; }}


		// This enables us to get hold of encoding converters:
		private EncConverters m_converters;
		// Placeholder for the implied root of the hierarchy (we assume there's only one):
		private ClsHierarchyEntry m_Root;
		// Hash table of Begin Markers, telling us which hierarchy entry each initiates. This
		// gets populated during ValidateHierarchy().
		private Hashtable m_BeginMarkerHierarchyEntries;
		// this hash will allow a quick look up for non-auto fields to get the hierachy entry it belongs to.
		private Hashtable m_sfmToHierarchy;
		// this hash keeps track of all the markers that aren't used in the hierarchy
		private Hashtable m_MarkersNotInHierarchy;

		public static ClsLog Log = new ClsLog();

		private int m_SfmLineNumber;
		private string m_SfmFileName;
		private string m_MappingFileName;
		private string m_OutputFileName;

		private void TestFile(string FileName)
		{
			if (!System.IO.File.Exists(FileName))
				throw new System.Exception(FileName + "does not exist.");

			// Open the file to read from.
			using (System.IO.StreamReader sr = System.IO.File.OpenText(FileName))
			{
				// Make sure there's at least one line:
				if (sr.ReadLine() == null)
					throw new System.Exception(FileName + " contains no data.");
			}
		}

		private bool TestFileWrite(string fileName)
		{
			bool valid = true;
			if (!System.IO.File.Exists(fileName))
				return valid; // assume that the file can be created and written to

			// Open the file to read from.
			try
			{
				using (System.IO.FileStream fs = new System.IO.FileStream(fileName,
					System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write))
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


		protected Hashtable TESTLanguages
		{
			get { return m_Languages; }
		}

		protected Hashtable LanguagesToIgnore
		{
			get { return m_LangsToIgnore; }
		}

		private void ClearIgnoreIfSet(string langId)
		{
			// remove it from the Ignore list if it's present
			m_LangsToIgnore.Remove(langId);
		}

		private void NoConvertWs(string langId)
		{
			if (m_Languages.ContainsKey(langId))
			{
				ClsLanguage lang = m_Languages[langId] as ClsLanguage;
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
			if (m_Languages.ContainsKey(langId))
			{
				if (!m_LangsToIgnore.ContainsKey(langId))
					m_LangsToIgnore.Add(langId, null);
			}
			else
			{
				// ignore set on a lang that isn't defined, possible future warning
			}
		}

		private void ConvertWs(string langId, string data)
		{
			if (m_Languages.ContainsKey(langId))
			{
				char [] delim = new char [] {':'};
				string [] values = data.Split(delim);
				if (values.Length != 2)	// should only be two elements per string
					return;
				string xmlLang = values[0].Trim();
				string map = values[1].Trim();
				ClsLanguage lang = m_Languages[langId] as ClsLanguage;
//				if (lang == null)
//				{
//					System.Diagnostics.Debug.WriteLine("No language with landId of '" + langId +"'");
//					return;
//				}
				// if it was previously 'ignore' then remove from ignore list
				if (m_LangsToIgnore.ContainsKey(langId))  //lang.XmlLang == "ignore")
				{
					m_LangsToIgnore.Remove(langId);
				}
				bool xmlLangDiff = (lang.XmlLang != xmlLang);
				bool mapDiff = (lang.EncCvtrMap != map);
				if (xmlLangDiff || mapDiff)
				{
					string msg;
					if (xmlLangDiff && mapDiff)
					{
						if (map != null && map.Length > 0)
						{
							msg = String.Format(Sfm2XmlStrings.LanguagesEntry_0_lang_1_2_map_3_4,
								langId, lang.XmlLang, xmlLang, lang.EncCvtrMap, map);
						}
						else
						{
							msg = String.Format(Sfm2XmlStrings.LanguagesEntry_0_lang_1_2_map_3,
								langId, lang.XmlLang, xmlLang, lang.EncCvtrMap);
						}
					}
					else if (xmlLangDiff)
					{
						msg = String.Format(Sfm2XmlStrings.LanguagesEntry_0_lang_1_2,
							langId, lang.XmlLang, xmlLang);
					}
					else
					{
						if (map != null && map.Length > 0)
						{
							msg = String.Format(Sfm2XmlStrings.LanguagesEntry_0_map_1_2,
								langId, lang.EncCvtrMap, map);
						}
						else
						{
							msg = String.Format(Sfm2XmlStrings.LanguagesEntry_0_map_1,
								langId, lang.EncCvtrMap);
						}
					}
					Log.AddWarning(msg);
					lang.Convert(xmlLang, map);
				}
			}
			else
			{
				// lang that isn't defined, possible future warning
			}

		}


		protected void TESTmethod()
		{
		}

		private bool UseFiles(string SfmFileName, string MappingFileName, string OutputFileName)
		{
			m_SfmFileName = SfmFileName;
			m_MappingFileName = MappingFileName;
			m_OutputFileName = OutputFileName;

			// Check we have read access to both files, and that they aren't empty:
			TestFile(SfmFileName);
			TestFile(MappingFileName);
			bool ok = true;
			while (TestFileWrite(OutputFileName) == false)
			{
				ok = false;
				string msg = String.Format(Sfm2XmlStrings.PleaseCloseAnyEditorsLookingAt0,
					OutputFileName);
				if (System.Windows.Forms.MessageBox.Show(msg, Sfm2XmlStrings.UnableToOpenOutputFile,
					System.Windows.Forms.MessageBoxButtons.RetryCancel,
					System.Windows.Forms.MessageBoxIcon.Asterisk,
					System.Windows.Forms.MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Cancel)
					break;
				ok = true;
			}
			return ok;
		}

		public void Convert(string SfmFileName, string MappingFileName, string OutputFileName)
		{
			Convert(SfmFileName, MappingFileName, OutputFileName, "", "", "");
		}

		public void Convert(string SfmFileName, string MappingFileName, string OutputFileName,
							string vernWs, string regWs, string natWs)
		{
			m_NumElements = 0;
			// Log.Open(LogFileName);
			if (!UseFiles(SfmFileName, MappingFileName, OutputFileName))
			{
				return;
			}

			using (System.Xml.XmlTextWriter xmlOutput = new System.Xml.XmlTextWriter(m_OutputFileName, System.Text.Encoding.UTF8))
			{
				xmlOutput.Formatting = System.Xml.Formatting.Indented;
				xmlOutput.Indentation = 2;

				WriteOutputFileComment(SfmFileName, MappingFileName, OutputFileName, xmlOutput);

				xmlOutput.WriteComment(" database is the root element for this file ");
				xmlOutput.WriteStartElement("database");

				System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
				try
				{
					xmlMap.Load(m_MappingFileName);
				}
				catch (System.Xml.XmlException e)
				{
					string ErrMsg = String.Format(Sfm2XmlStrings.InvalidMappingFile0_1, m_MappingFileName, e.Message);
					Log.AddError(ErrMsg);
					// put out the warnings and errors
					Log.FlushTo(xmlOutput);
					xmlOutput.WriteEndElement(); // Close the Database node
					xmlOutput.Close();
					return;
				}

				ReadLanguages(xmlMap);

				// === Process the command line args relating to languages ===
				// National ws
				if (natWs.ToLowerInvariant() == STATICS.Ignore)
					IgnoreWs("nat");
				else if (natWs.ToLowerInvariant() == "no-convert")
					NoConvertWs("nat");
				else if (natWs.Length > 0)
					ConvertWs("nat", natWs);

				// Regional ws
				if (regWs.ToLowerInvariant() == STATICS.Ignore)
					IgnoreWs("reg");
				else if (regWs.ToLowerInvariant() == "no-convert")
					NoConvertWs("reg");
				else if (regWs.Length > 0)
					ConvertWs("reg", regWs);

				// Vern ws
	//			if (vernWs.ToLowerInvariant() == "ignore")
	//				IgnoreWs("vern");
				if (vernWs.ToLowerInvariant() == "no-convert")
					NoConvertWs("vern");
				else if (vernWs.Length > 0)
					ConvertWs("vern", vernWs);

				try
				{
					ReadHierarchy(xmlMap);
					ReadAndOutputSettings(xmlMap, xmlOutput);

					// read the mapping file and build internal classes / objects and
					// add field descriptions to output file
					ReadFieldDescriptions(xmlMap);	//  ReadAndOutputFieldDescriptions(xmlMap, xmlOutput);
					ReadCustomFieldDescriptions(xmlMap);

					// read the mapping file inline markers
					ReadInFieldMarkers(xmlMap);

					// Now vaildate the data read in. This must be done in the follwoing order:
					// Languages, Field Descriptions, Hierarchy. Infield Markers must be validated
					// after Languages. This order is needed because the later checks rely on
					// success of the earlier ones.
					ValidateLanguages();	// throw if bad language data
					ValidateFieldDescriptions();
					ValidateCustomFieldDescriptions();
					ValidateHierarchy();
					ValidateInfieldMarkers();
				}
				catch (System.Exception e)
				{
					string ErrMsg = String.Format(Sfm2XmlStrings.UnhandledException0, e.Message);
					Log.AddError(ErrMsg);
				}

				string nl = System.Environment.NewLine;
				string comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: " + m_Root.Name + nl;
				comments += "  This element contains the inputfile in an XML format." + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);
	//			xmlOutput.WriteComment(" This element contains the inputfile in an XML format ");

				try
				{
					//			xmlOutput.WriteStartElement(m_Root.Name);
					//			ProcessSfmFile(xmlOutput);
					ProcessSfmFileNewLogic(xmlOutput);
				}
				catch (System.Exception e)
				{
					string ErrMsg = String.Format(Sfm2XmlStrings.UnhandledException0, e.Message);
					Log.AddError(ErrMsg);
				}
#if false
				if (m_autoFieldsUsed.Count > 0)
				{
					xmlOutput.WriteComment(" This is where the autofield info goes after the data has been processed. ");
					xmlOutput.WriteStartElement("autofields");
					foreach(DictionaryEntry autoEntry in m_autoFieldsUsed)
					{
						AutoFieldInfo afi = autoEntry.Value as AutoFieldInfo;
						xmlOutput.WriteStartElement("field");
						xmlOutput.WriteAttributeString("class", afi.className);
						xmlOutput.WriteAttributeString("sfm", afi.sfmName);
						xmlOutput.WriteAttributeString("fwid", afi.fwDest);
						xmlOutput.WriteEndElement();
					}
					xmlOutput.WriteEndElement();
				}
#endif
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
				foreach(DictionaryEntry fieldEntry in m_FieldDescriptionsTable)
				{
					ClsFieldDescription fd = fieldEntry.Value as ClsFieldDescription;
					if (fd is ClsCustomFieldDescription)
						continue;	// the custom fields will be put out in a CustomFields section following...

					if (m_autoFieldsBySFM.ContainsKey(fd.SFM))
					{
						ArrayList afiBysfm = m_autoFieldsBySFM[fd.SFM] as ArrayList;
						foreach(AutoFieldInfo afi in afiBysfm)
						{
							fd.AddAutoFieldInfo(afi.className, afi.fwDest);
						}
					}
					fd.ToXmlLangString(xmlOutput);
	//				string xmldata = fd.ToXmlLangString(xmlOutput);
					// xmlOutput.WriteRaw(xmldata);
				}
				xmlOutput.WriteEndElement();

				// put out the field descriptions with the autofield info integrated in: for xslt processing...
				comments = nl;
				comments += " ================================================================" + nl;
				comments += " Element: CustomFieldDescriptions" + nl;
				comments += " ================================================================" + nl;
				xmlOutput.WriteComment(comments);

				xmlOutput.WriteStartElement("CustomFieldDescriptions");
				foreach (DictionaryEntry fieldEntry in m_FieldDescriptionsTable)
				{
					ClsCustomFieldDescription fd = fieldEntry.Value as ClsCustomFieldDescription;
					if (fd == null)
						continue;	// not a custom field

					fd.ToXmlLangString(xmlOutput);
	//				string xmldata = fd.ToXmlLangString(xmlOutput);
					// xmlOutput.WriteRaw(xmldata);
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

#if false

				<!-- This is where the autofield information goes.  Needed to make sense
					-- of the field and Records elements that use autofields -->
				<autofields>
					<field class="Entry" sfm="dan" fwid="eires"/>
				</autofields>
#endif

				// put out the warnings and errors
				Log.FlushTo(xmlOutput);
				xmlOutput.WriteEndElement(); // Close the Database node
				xmlOutput.Close();
			}
		}

		protected bool ReadSettings(System.Xml.XmlDocument xmlMap)
		{
			return ReadAndOutputSettings(xmlMap, null);
		}
		private bool ReadAndOutputSettings(System.Xml.XmlDocument xmlMap, System.Xml.XmlTextWriter xmlOutput)
		{
			int numFound = 0;
			Hashtable foundAttributes = new Hashtable();
			System.Xml.XmlNodeList settingsList = xmlMap.SelectNodes("sfmMapping/settings/meaning");
			foreach (System.Xml.XmlNode settingNode in settingsList)
			{
				bool fwSetting = false;
				foreach(System.Xml.XmlAttribute attribute in settingNode.Attributes)
				{
					switch (attribute.Name)
					{
						case "app":
							if (attribute.Value.ToLowerInvariant() == "fw.sil.org")
								fwSetting = true;
							break;
						default:
							foundAttributes[attribute.Name] = attribute.Value;
							break;
					}
				}

				if (fwSetting && foundAttributes.Count > 0)
				{
					numFound++;
					if (xmlOutput != null)	// produce output
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
			if (numFound > 1 )
				Log.AddWarning(Sfm2XmlStrings.DuplicateSettingsFoundForFwSilOrg);

			return true;
		}

		protected bool ReadLanguages(System.Xml.XmlDocument xmlMap)
		{
			bool success = true;
			System.Xml.XmlNodeList LanguageList = xmlMap.SelectNodes("sfmMapping/languages/langDef");
			foreach (System.Xml.XmlNode LanguageNode in LanguageList)
			{
				ClsLanguage Language = new ClsLanguage();
				bool isValidLanguageNode = Language.ReadXmlNode(LanguageNode);
				if (m_Languages.ContainsKey(Language.KEY))
				{
					Log.AddError(String.Format(Sfm2XmlStrings.DuplicateId0InLanguages, Language.KEY));
					success = false;
				}
				else if (Language.KEY == null || Language.KEY.Length == 0)
				{
					Log.AddError(Sfm2XmlStrings.LanguageWithEmptyMissingIdInLanguages);
					success = false;
				}
				else
				{
					m_Languages.Add(Language.KEY, Language);
					if (Language.XmlLang.ToLowerInvariant() == STATICS.Ignore || isValidLanguageNode == false)
					{
						if (!m_LangsToIgnore.ContainsKey(Language.KEY))
							m_LangsToIgnore.Add(Language.KEY, null);
						if (isValidLanguageNode == false)
							success = false;
					}
				}
			}
			if (m_Languages.Count == 0)
			{
				Log.AddError(Sfm2XmlStrings.NoValidLanguagesDefined);
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
			foreach (DictionaryEntry languageEntry in m_Languages)
			{
				ClsLanguage language = languageEntry.Value as ClsLanguage;
				// Assign the encoding converter specified in the mapping file:
				if (language.EncCvtrMap != null && language.EncCvtrMap.Length > 0 && !language.SetConverter(m_converters))
				{
					Log.AddFatalError(String.Format(Sfm2XmlStrings.UnknownEncodingConvertersMap0InLanguage1, language.EncCvtrMap, language.KEY));
				}
			}
		}

		protected bool ReadHierarchy(System.Xml.XmlDocument xmlMap)
		{
			bool success = true;
			System.Xml.XmlNodeList HierarchyList = xmlMap.SelectNodes("sfmMapping/hierarchy/level");
			foreach (System.Xml.XmlNode Level in HierarchyList)
			{
				ClsHierarchyEntry HierarchyEntry = new ClsHierarchyEntry();
				if (HierarchyEntry.ReadXmlNode(Level))
				{
					if (m_Hierarchy.ContainsKey(HierarchyEntry.KEY))
					{
						Log.AddError(String.Format(Sfm2XmlStrings.HierarchyEntry0MultiplyDefined, HierarchyEntry.Name));
						success = false;
					}
					else
						m_Hierarchy.Add(HierarchyEntry.KEY, HierarchyEntry);
				}
				else
					success = false;
			}
			if (m_Hierarchy.Count == 0)
			{
				Log.AddError(Sfm2XmlStrings.NoValidHierarchyEntriesDefined);
				success = false;
			}

			// Now populate the children hashtable: key is parent and value is arraylist of children.
			foreach(DictionaryEntry dictEentry in m_Hierarchy)
			{
				ClsHierarchyEntry entry = dictEentry.Value as ClsHierarchyEntry;
				// add this node as a child of all the ancestors of it
				foreach(string name in entry.Ancestors)
				{
					ArrayList children = m_HierarchyChildren[name] as ArrayList;
					if (children == null)
					{
						children = new ArrayList();
						m_HierarchyChildren.Add(name, children);
					}
					if (!children.Contains(entry.Name))
					{
						children.Add(entry.Name);
					}
					else
					{
						Log.AddWarning(String.Format(Sfm2XmlStrings.DuplicateHierarchy0IsAlreadyChildOf1, entry.Name, name));
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
			Hashtable leaf = new Hashtable();
			Hashtable root = new Hashtable();
			foreach(DictionaryEntry dictEentry in m_Hierarchy)
			{
				ClsHierarchyEntry entry = dictEentry.Value as ClsHierarchyEntry;
				leaf.Add(entry.Name, null);		// in C++ this would be a 'set' type
				foreach(string name in entry.Ancestors)
				{
					if (!root.ContainsKey(name))
						root.Add(name, null);
				}
			}
			// now walk through one of the lists and mark items that exist in both lists
			IDictionaryEnumerator myEnumerator = leaf.GetEnumerator();
			while ( myEnumerator.MoveNext() )
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
					Log.AddError(Sfm2XmlStrings.NoRootImpliedInTheHierarchy);
				}
				else
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					foreach (DictionaryEntry dictEntry in root)
						sb.AppendFormat(Sfm2XmlStrings.ksQuotedItem, dictEntry.Key);
					string ErrMsg = String.Format(Sfm2XmlStrings.ThereAre0RootsImpliedInTheHierarchy1,
						root.Count, sb.ToString());
					Log.AddError(ErrMsg);
				}

				// Add a dummy root to avoid a crash later:
				m_Root = new ClsHierarchyEntry("UnknownRoot");
				m_Hierarchy.Add(m_Root.KEY, m_Root);
			}
			else
			{
				// create the root level-node
				myEnumerator = root.GetEnumerator();
				myEnumerator.MoveNext();	// get on the first element
				string rootNode = myEnumerator.Key.ToString();
				m_Root = new ClsHierarchyEntry(rootNode);
				// We are confident by now that the root is a unique entry:
				m_Hierarchy.Add(m_Root.KEY, m_Root);
			}

			// this list will contain all the sfms used in all the hierarchy entries
			Hashtable allHierarchySfms = new Hashtable();

			// Check every Hierarchy Entry:
			foreach(DictionaryEntry dictEentry in m_Hierarchy)
			{
				ClsHierarchyEntry hierarchy = dictEentry.Value as ClsHierarchyEntry;

				// Check for uniqueness of the beginfields:
				foreach (string beginSfm in hierarchy.BeginFields)
				{
					if (beginSfm != STATICS.Ignore)	// special case to ignore the begin marker {needed for prev tests though}
					{
						if (m_BeginMarkerHierarchyEntries.ContainsKey(beginSfm))
						{
							Log.AddError(String.Format(Sfm2XmlStrings.HierarchyEntry0HasDuplicateBeginField1_2,
								hierarchy.Name, beginSfm, (m_BeginMarkerHierarchyEntries[beginSfm] as ClsHierarchyEntry).Name));
						}
						else
						{
							m_BeginMarkerHierarchyEntries.Add(beginSfm, hierarchy);		// add the key and its first occurance
							if (!allHierarchySfms.ContainsKey(beginSfm))
								allHierarchySfms.Add(beginSfm, hierarchy);
						}

						// Make sure beginfields don't appear in addtionalfields:
						if (hierarchy.AdditionalFieldsContains(beginSfm))
						{
							Log.AddError(String.Format(Sfm2XmlStrings.HierarchyEntry0HasField1WhichIsInBeginFields,
								hierarchy.Name, beginSfm));
						}
					}
				}

				// Make sure multifields exist in either beginfields or additionalfields:
				foreach (string multiSfm in hierarchy.MultiFields)
				{
					if (!hierarchy.BeginFieldsContains(multiSfm) &&
						!hierarchy.AdditionalFieldsContains(multiSfm))
					{
						Log.AddError(String.Format(Sfm2XmlStrings.HierarchyEntry0HasUnexpectedMultiField1,
							hierarchy.Name, multiSfm));
					}
				}

				// Continue to build list of all hierarchy SFMs:
				foreach (string addtlSfm in hierarchy.AdditionalFields)
				{
					if (!allHierarchySfms.Contains(addtlSfm))
						allHierarchySfms.Add(addtlSfm, hierarchy);
					if (!m_sfmToHierarchy.Contains(addtlSfm))
						m_sfmToHierarchy.Add(addtlSfm, hierarchy);
				}
			}

			// Test to make sure each of our Hierarchy SFMs is defined in the FieldDescriptions:
			foreach (DictionaryEntry hierarchySfmPair in allHierarchySfms)
			{
				if (!m_FieldDescriptionsTable.ContainsKey(hierarchySfmPair.Key) &&
					!m_FieldsToIgnore.ContainsKey(hierarchySfmPair.Key))
				{
					Log.AddError(String.Format(Sfm2XmlStrings.HierarchyEntry0RefersToInvalidSFM1,
						(hierarchySfmPair.Value as ClsHierarchyEntry).Name, hierarchySfmPair.Key));
				}
			}
		}

		protected bool ReadInFieldMarkers(System.Xml.XmlDocument xmlMap)
		{
			bool success = true;

			// Iterate through all the input "ifm" elements:
			System.Xml.XmlNodeList InFieldMarkerList = xmlMap.SelectNodes("sfmMapping/inFieldMarkers/ifm");
			Hashtable htMarkers = new Hashtable(InFieldMarkerList.Count);
			foreach (System.Xml.XmlNode InFieldMarkerNode in InFieldMarkerList)
			{
				ClsInFieldMarker marker = new ClsInFieldMarker();
				if (marker.ReadXmlNode(InFieldMarkerNode, m_Languages))
				{
					if (m_InFieldMarkers.ContainsKey(marker.KEY))
					{
						Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0MultiplyDefined,
							marker.Begin));
						success = false;
					}
					else
					{
						marker.GenerateElementName(htMarkers);
						m_InFieldMarkers.Add(marker.KEY, marker);
					}
				}
				else
				{
					success = false;
				}
			}
			return success;
		}

		private bool OutputInFieldMarkers(System.Xml.XmlTextWriter xmlOutput)
		{
			if (xmlOutput == null)
				return false;

			bool useXmlLang = true;
			xmlOutput.WriteStartElement("inFieldMarkers");
			foreach (DictionaryEntry dictEntry in m_InFieldMarkers)
			{
				ClsInFieldMarker inFieldMarker = dictEntry.Value as ClsInFieldMarker;
				inFieldMarker.OutputXml(xmlOutput, useXmlLang);
			}
			xmlOutput.WriteEndElement();
			return true;
		}


		private void ValidateInfieldMarkers()
		{
			ArrayList xmlElementNames = new ArrayList();

			foreach (DictionaryEntry dictEntry in m_InFieldMarkers)
			{
				ClsInFieldMarker inFieldMarker = dictEntry.Value as ClsInFieldMarker;

				// Check that the given language exists in the languages list:
				if (inFieldMarker.Language != null && inFieldMarker.Language.Length > 0 &&
					!m_Languages.ContainsKey(inFieldMarker.Language))
				{
					Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0RefersToInvalidLanguage1,
						inFieldMarker.Begin, inFieldMarker.Language));
				}

				// Check that the XML element name is unique:
				if (xmlElementNames.Contains(inFieldMarker.ElementName))
					Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0DuplicatesUsingElement1,
						inFieldMarker.Begin, inFieldMarker.ElementName));
				else
					xmlElementNames.Add(inFieldMarker.ElementName);
			}
		}

		private string MakeValidXMLComment(string text)
		{
			// remove "--" from the comments
			if (text.Contains("--"))
			{
				int pos = text.IndexOf("--", StringComparison.Ordinal);
				while (pos >= 0)
				{
					text = text.Remove(pos, 1);
					pos = text.IndexOf("--", pos, StringComparison.Ordinal);
				}
				text += " (double hyphens have been removed)";
			}

			// don't allow the comments to end with a hyphen "-"
			if (text.EndsWith("-", StringComparison.Ordinal))
				text += " ";

			return text;
		}

		private void WriteOutputFileComment(string SfmFileName, string MappingFileName,
			string OutputFileName, System.Xml.XmlTextWriter xmlOutput)
		{
			string nl = System.Environment.NewLine;
			string comments = nl;
			comments += " ================================================================" + nl;
			comments += " " + System.DateTime.Now.ToString() + nl;
			comments += " Created by " + System.Reflection.Assembly.GetExecutingAssembly().ToString() + nl;
			comments +=  nl;
			comments += " The command line parameters were :" + nl;
			comments += "  sfmFileName : " + MakeValidXMLComment(SfmFileName) + nl;
			comments += "  mappingFile : " + MakeValidXMLComment(MappingFileName) + nl;
			comments += "  xmlOutput   : " + MakeValidXMLComment(OutputFileName) + nl;
			comments += " ================================================================" + nl;
			xmlOutput.WriteComment(comments);
		}

		protected bool ReadFieldDescriptions(System.Xml.XmlDocument xmlMap)
		{
			// passing null as the XmlTextWriter causes the output to not happen
			return ReadAndOutputFieldDescriptions(xmlMap, null);
		}
		private bool ReadAndOutputFieldDescriptions(System.Xml.XmlDocument xmlMap, System.Xml.XmlTextWriter xmlOutput)
		{
			bool success = true;
			if (xmlOutput != null)
				xmlOutput.WriteStartElement("fieldDescriptions");

			// Iterate through all the input "field" elements:
			System.Xml.XmlNodeList FieldList = xmlMap.SelectNodes("sfmMapping/fieldDescriptions/field");
			foreach (System.Xml.XmlNode FieldNode in FieldList)
			{
				ClsFieldDescription FieldDescription = new ClsFieldDescription();
				if (FieldDescription.ReadAndOutputXmlNode(FieldNode, m_Languages, m_topAnalysisWS, xmlOutput, ref m_LangsToIgnore, ref m_FieldsToIgnore))
				{
					if (m_FieldDescriptionsTable.ContainsKey(FieldDescription.KEY))
					{
						Log.AddError(String.Format(Sfm2XmlStrings.FieldDescriptionsHaveMoreThanOneInstanceOf0,
							FieldDescription.SFM));
						success = false;
					}
					else
						m_FieldDescriptionsTable.Add(FieldDescription.KEY, FieldDescription);
				}
				else
					success = false;
			}
			if (xmlOutput != null)
				xmlOutput.WriteEndElement(); // Close FieldDescriptions node
			if (m_FieldDescriptionsTable.Count == 0)
			{
				Log.AddError(Sfm2XmlStrings.NoValidFieldDescriptionsDefined);
				success = false;
			}
			return success;
		}

		private void ValidateFieldDescriptions()
		{
			foreach (DictionaryEntry dictEntry in m_FieldDescriptionsTable)
			{
				ClsFieldDescription fldDesc = dictEntry.Value as ClsFieldDescription;
				// Check that the given language exists in the languages list:
				if (!m_Languages.ContainsKey(fldDesc.Language))
				{
					Log.AddError(String.Format(Sfm2XmlStrings.FieldDescription0RefersToInvalidLanguage1,
						fldDesc.SFM, fldDesc.Language));
					if (!m_FieldsToIgnore.ContainsKey(fldDesc.SFM))
						m_FieldsToIgnore.Add(fldDesc.SFM, null);
					Log.AddWarning(String.Format(Sfm2XmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, fldDesc.SFM));
					continue;
				}

				if (fldDesc.IsAutoImportField)
					continue;	// auto import fields aren't used in the hierarchy

				// Make sure that each sfm is used in atleast one hierarchy entry
				bool bUsed = false;
				foreach (DictionaryEntry hierarchy in m_Hierarchy)
				{
					ClsHierarchyEntry entry = hierarchy.Value as ClsHierarchyEntry;
					if (entry.UsesSFM(fldDesc.SFM))
					{
						bUsed = true;
						break;
					}
				}
				if (!bUsed)
				{
					// LT-2217: only show errors for fields that are not 'ignored'
					if (!m_FieldsToIgnore.ContainsKey(fldDesc.SFM))
						Log.AddSFMError(fldDesc.SFM,
							String.Format(Sfm2XmlStrings.FieldDescription0UnusedInHierarchy, fldDesc.SFM));

					if (!m_MarkersNotInHierarchy.ContainsKey(fldDesc.SFM))
						m_MarkersNotInHierarchy.Add(fldDesc.SFM, null);
				}
			}


		}

		protected bool ReadCustomFieldDescriptions(System.Xml.XmlDocument xmlMap)
		{
			return ReadAndOutputCustomFieldDescriptions(xmlMap, null);
		}
		private bool ReadAndOutputCustomFieldDescriptions(System.Xml.XmlDocument xmlMap, System.Xml.XmlTextWriter xmlOutput)
		{
			bool success = true;
			if (xmlOutput != null)
				xmlOutput.WriteStartElement("CustomFieldDescriptions");

			// Iterate through all the input "CustomField" elements:
			System.Xml.XmlNodeList FieldList = xmlMap.SelectNodes("sfmMapping/CustomFieldDescriptions/CustomField");
			foreach (System.Xml.XmlNode FieldNode in FieldList)
			{
				ClsCustomFieldDescription FieldDescription = new ClsCustomFieldDescription();
				if (FieldDescription.ReadAndOutputXmlNode(FieldNode, m_Languages, m_topAnalysisWS, xmlOutput, ref m_LangsToIgnore, ref m_FieldsToIgnore))
				{
					if (m_FieldDescriptionsTable.ContainsKey(FieldDescription.KEY))
					{
						Log.AddError(String.Format(Sfm2XmlStrings.FieldDescriptionsHaveMoreThanOneInstanceOf0,
							FieldDescription.SFM));
						success = false;
					}
					else
					{
						m_FieldDescriptionsTable.Add(FieldDescription.KEY, FieldDescription);

					}
				}
				else
					success = false;
			}
			if (xmlOutput != null)
				xmlOutput.WriteEndElement(); // Close FieldDescriptions node
			if (m_FieldDescriptionsTable.Count == 0)
			{
				Log.AddError(Sfm2XmlStrings.NoValidFieldDescriptionsDefined);
				success = false;
			}
			return success;
		}

		private void ValidateCustomFieldDescriptions()
		{
			foreach (DictionaryEntry dictEntry in m_FieldDescriptionsTable)
			{
				ClsFieldDescription fldDesc = dictEntry.Value as ClsFieldDescription;
				// Check that the given language exists in the languages list:
				if (!m_Languages.ContainsKey(fldDesc.Language))
				{
					Log.AddError(String.Format(Sfm2XmlStrings.FieldDescription0RefersToInvalidLanguage1,
						fldDesc.SFM, fldDesc.Language));
					if (!m_FieldsToIgnore.ContainsKey(fldDesc.SFM))
						m_FieldsToIgnore.Add(fldDesc.SFM, null);
					Log.AddWarning(String.Format(Sfm2XmlStrings.FieldDescWithSFMOf0IsBeingIGNORED, fldDesc.SFM));
					continue;
				}

				if (fldDesc.IsAutoImportField)
					continue;	// auto import fields aren't used in the hierarchy

				// Make sure that each sfm is used in atleast one hierarchy entry
				bool bUsed = false;
				foreach (DictionaryEntry hierarchy in m_Hierarchy)
				{
					ClsHierarchyEntry entry = hierarchy.Value as ClsHierarchyEntry;
					if (entry.UsesSFM(fldDesc.SFM))
					{
						bUsed = true;
						break;
					}
				}
				if (!bUsed)
				{
					// LT-2217: only show errors for fields that are not 'ignored'
					if (!m_FieldsToIgnore.ContainsKey(fldDesc.SFM))
						Log.AddSFMError(fldDesc.SFM,
							String.Format(Sfm2XmlStrings.FieldDescription0UnusedInHierarchy, fldDesc.SFM));

					if (!m_MarkersNotInHierarchy.ContainsKey(fldDesc.SFM))
						m_MarkersNotInHierarchy.Add(fldDesc.SFM, null);
				}
			}


		}


//		/// <summary>
//		/// Call this only after reading mapping file and validating all read-in data.
//		/// </summary>
//		private void FinishDataStructures()
//		{
//			// Nothing to do!
//		}

//		private void ShowStackElements(Stack currentPath)
//		{
//			string msg;
//			msg = System.Environment.NewLine + " ==== Stack Elements: count=" + currentPath.Count;
//			System.Diagnostics.Debug.WriteLine(msg);
////			Log.AddWarning(msg);
//			foreach (ClsPathObject currentPathItem in currentPath)
//			{
//				msg = " ==== " + currentPathItem.Name;
//				System.Diagnostics.Debug.WriteLine(msg);
////				Log.AddWarning(msg);
//			}
//			System.Diagnostics.Debug.WriteLine(" ==== Stack Elements ====");
////			Log.AddWarning(" ==== Stack Elements ====");
//		}

//		private ClsHierarchyEntry GetClsHierarchyEntryForSfm(string sfm)
//		{
//			foreach(DictionaryEntry dictEentry in m_Hierarchy)
//			{
//				ClsHierarchyEntry entry = dictEentry.Value as ClsHierarchyEntry;
//				if (entry.AdditionalFieldsContains(sfm) || entry.BeginFieldsContains(sfm))
//					return entry;
//			}
//			return null;
//		}

		protected bool AddToOpenObjects(Stack currentPath, string sfm, byte[] sfmData, int line)
		{
			bool retVal = false;	// not added

			// At this point we know that the sfm is not part of the top most object.
			// See if it is valid for any of the open objects, and if so add it to the
			// pending list of markers to be output.

			if (currentPath.Count == 1)	// only one item
				return retVal;

			bool topMostItem = true;
			ArrayList openSfmObjects = new ArrayList(currentPath.ToArray());
			foreach (ClsPathObject openSfmObject in openSfmObjects)
			{
				ClsHierarchyEntry currentLocation = m_Hierarchy[openSfmObject.Name] as ClsHierarchyEntry;
				// The current SFM does not start a new hierarchy level. See if we can use it at
				// the current location:
				bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
				bool appearsInMulti = currentLocation.MultiFieldsContains(sfm);
				bool appearsInAdditional = currentLocation.AdditionalFieldsContains(sfm);
				bool usedAlready = openSfmObject.ContainsSfm(sfm);
				bool noUniqueProblem = !uniqueSfm || !openSfmObject.AlreadContainsUniqueSfm;
				bool canUseHere = appearsInMulti || (appearsInAdditional && !usedAlready && noUniqueProblem);
				if (canUseHere)
				{
					if (!topMostItem)
					{
						openSfmObject.AddSfm(sfm, uniqueSfm);
						openSfmObject.AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
						retVal = true;
					}
					break;
				}
				// if it's an autofield, see if there's a match at this 'class' / entry
				ClsFieldDescription possibleAuto = GetFieldDescription(sfm);
				if (possibleAuto != null && possibleAuto.IsAutoImportField)
				{
					// if there's a autofield for this class, put it out
					if (m_autoFieldsPossible.ContainsKey(currentLocation.Name))
					{
						openSfmObject.AddSfm(sfm, uniqueSfm);
						openSfmObject.AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
						retVal = true;
						// add it to the list of autofields used
						string fwDest = m_autoFieldsPossible[currentLocation.Name] as String;
						AutoFieldInfo afi = new AutoFieldInfo(currentLocation.Name, sfm, fwDest);
						if (!m_autoFieldsUsed.ContainsKey(afi.KEY))
						{
////								m_autoFieldsPossible[currentLocation.Name] as AutoFieldInfo;	//  AutoFieldInfo(currentLocation.Name, sfm, "ASDF");
							m_autoFieldsUsed.Add(afi.KEY, afi);
							ArrayList sfms;
							if (m_autoFieldsBySFM.ContainsKey(afi.sfmName))
							{
								sfms = m_autoFieldsBySFM[afi.sfmName] as ArrayList;
							}
							else
							{
								sfms = new ArrayList();
								m_autoFieldsBySFM.Add(afi.sfmName, sfms);
							}
							sfms.Add(afi);
						}
						break;
					}
				}

				topMostItem = false;	// not the first frame, so now we can add it for later processing
			}


			return retVal;
		}

		/// <summary>
		/// Determines the shortest path to navigate from the given path to the given destination in
		/// the Hierarchy.
		/// For this implememntation, we will not accept any path requiring more than 1 step
		/// </summary>
		/// <param name="currentPath"></param>
		/// <returns>A stack describing the shortest possible path through the hierarchy.</returns>
		protected Stack OptimalPathFromCommonAncestorToHierarchyEntry(Stack currentPath, ClsHierarchyEntry destination)
		{
			// create our return path stack
			Stack downPath = new Stack();

			// First lets take the currentPath items and put them into a hash and add the depth
			// as the value item.
			Hashtable currentPathDepths = new Hashtable();
			int currentPathDepth = 0;

			// walk the stack building the hashtable
			foreach (ClsPathObject currentPathItem in currentPath)
				currentPathDepths.Add(currentPathItem.Name, currentPathDepth++);

			// see if the destination is in the currentPath
			if (currentPathDepths.ContainsKey(destination.KEY))
			{
				downPath.Push( new ClsPathObject(destination.KEY));
			}
			else
			{
				// check each ancestor of the destination item
				string currentBestAncestor = null;
				int currentBestDepth = Int32.MaxValue;
				foreach(string name in destination.Ancestors)
				{
					if (currentPathDepths.ContainsKey(name))
					{
						int depth = (int)(currentPathDepths[name]);
						if (depth < currentBestDepth)
						{
							currentBestAncestor = name;
							currentBestDepth = depth;
						}
					}
				}

				// see if we've found a common ancestor
				if (currentBestAncestor != null)
				{
					downPath.Push(new ClsPathObject(destination.KEY));
					downPath.Push(new ClsPathObject(currentBestAncestor));
				}
			}

			return downPath;
		}

//		private string GetPathObjectKey(object _object)
//		{
//			ClsPathObject pathObject = _object as ClsPathObject;
//			return pathObject.Name;
//		}

		/// <summary>
		/// This method will read the input file from the current position in the proper
		/// encoding (UTF8, BYTES, ...) that is based on the encoding of the SFM.
		/// </summary>
		/// <returns></returns>
		protected string GetSFMData()
		{
			string output = "";
			return output;
		}


		/// <summary>
		/// Find the first match of 'item' in the 'inData' returning the index or -1 if not found.
		/// </summary>
		/// <param name="inData">data to search through</param>
		/// <param name="startPos">index in the data to start at</param>
		/// <param name="item">data to search for</param>
		/// <returns>index where data starts or -1 if not found</returns>
		public static long FindFirstMatch( byte[] inData, int startPos, byte[] item)
		{
			long index = -1;	// none
			// if the indata length is to small, just return
			if (inData.Length-startPos < item.Length)
				return index;

			int matchPos = 0;		// position into the item

			for (int pos = startPos; pos < inData.Length; pos++)
			{
				if (inData[pos] == item[matchPos])	// match at this item index
				{
					if (matchPos == item.Length-1)	// complete item match
					{
						index = pos - matchPos;			// found match
						break;
					}
					matchPos++;		// keep looking through the whole item
				}
				else
					matchPos = 0;
			}
			return index;
		}


		protected class ClsFoundInFieldMarker
		{
			private string m_Marker;
			private bool m_IsBeginMarker; // false if marker is an end marker
			private ClsInFieldMarker m_InFieldMarker;
//			private IEncConverter m_iconverter;

			// Constructor for a Begin Marker
			public ClsFoundInFieldMarker(string marker, ClsInFieldMarker inFieldMarker)
			{
				m_Marker = marker;
				m_IsBeginMarker = true;
				m_InFieldMarker = inFieldMarker;
			}

			// Constructor for an End Marker
			public ClsFoundInFieldMarker(string marker)
			{
				m_Marker = marker;
				m_IsBeginMarker = false;
			}

			public ClsInFieldMarker ClsInFieldMarker { get { return m_InFieldMarker; }}
			public string Marker	{ get { return m_Marker; }}
			public int Length		{ get { return m_Marker.Length; }}
			public bool IsBegin		{ get { return m_IsBeginMarker; }}

			public string LanguageEncoding { get { return m_InFieldMarker.Language; }}
			public string ElementName { get { return m_InFieldMarker.ElementName; }}

			public bool HasLangEncoding
			{
				get
				{
					if (m_InFieldMarker.Language != null && m_InFieldMarker.Language.Length > 0)
						return true;
					else
						return false;
				}
			}

			public bool ContainsEndMarker(string endMarker)
			{
				return m_InFieldMarker.ContainsEndMarker(endMarker);
			}
		}

		public static byte[] WideToMulti(string wide, System.Text.Encoding encodingToUse)
		{
			System.Text.Encoding encoding = new System.Text.UTF8Encoding(false, true);
			if (encodingToUse == System.Text.Encoding.ASCII)
				encoding = new System.Text.ASCIIEncoding();

			int byteLength = encoding.GetByteCount(wide.ToCharArray());
			byte[] data = new byte[byteLength];
			encoding.GetBytes(wide, 0, wide.Length, data, 0);
			return data;
		}
		public enum MultiToWideError { InvalidCodePoint, None };
		public static string MultiToWide(byte[] multi, int start, int end, System.Text.Encoding encodingToUse)
		{
			MultiToWideError error;
			byte[] badBytes;
			return MultiToWideWithERROR(multi, start, end, encodingToUse, out error, out badBytes);
		}
		public static string MultiToWideWithERROR(byte[] multi, int start, int end,
			System.Text.Encoding encodingToUse, out MultiToWideError err, out byte[] badBytes)
		{
			err = MultiToWideError.None;
			badBytes = null;
			System.Text.Encoding encoding = new System.Text.UTF8Encoding(false, true);
			if (encodingToUse == System.Text.Encoding.ASCII)
				encoding = new System.Text.ASCIIEncoding();

			try
			{
				int charCount = encoding.GetCharCount(multi, start, end-start+1);
				Char[] chars = new Char[charCount];
				encoding.GetChars(multi, start, end - start + 1, chars, 0);
				return new string(chars);
			}
			catch (System.Text.DecoderFallbackException dfe)	//(Exception e)
			{
				err = MultiToWideError.InvalidCodePoint;
				badBytes = dfe.BytesUnknown;
				// have an invalid utf8 char most likely, so switch to ascii
				if (encoding.EncodingName == System.Text.UTF8Encoding.UTF8.EncodingName)
					encoding = new System.Text.ASCIIEncoding();
				int charCount = encoding.GetCharCount(multi, start, end - start + 1);
				Char[] chars = new Char[charCount];
				encoding.GetChars(multi, start, end - start + 1, chars, 0);
				return new string(chars);
			}
		}

		public static string MultiToWide(byte[] multi, System.Text.Encoding encodingToUse)
		{
			return MultiToWide(multi, 0, multi.Length-1, encodingToUse);
		}

		class ClsStringToOrFromBytes
		{
			public class DP
			{
				public int offset;
				public byte data;
				public DP(int o, byte d)
				{
					offset = o;
					data = d;
				}
			}
			byte[] m_Data;
			int m_ByteLength;
			string m_String;
			const byte kLeft1BitMask = 0x80;
			const byte kLeft2BitsMask = 0xC0;
			const byte kLeft3BitsMask = 0xE0;
			const byte kLeft4BitsMask = 0xF0;
			const byte kLeft5BitsMask = 0xF8;
			Queue<DP> m_invalidBytes;
			public static int MaxInvalidBytes { get { return 10; } }

			public ClsStringToOrFromBytes(byte[] data)
			{
				m_Data = data;
				m_ByteLength = data.Length;
				m_invalidBytes = new Queue<DP>(MaxInvalidBytes);
			}

			public ClsStringToOrFromBytes(string str)
			{
				m_String = str;
				m_invalidBytes = new Queue<DP>(MaxInvalidBytes);
			}

			public string InvalidByteString()
			{
				if (m_invalidBytes.Count == 0)
					return "";
				System.Text.StringBuilder sb = new System.Text.StringBuilder(Sfm2XmlStrings.Index_Byte___);
				while (m_invalidBytes.Count > 0)
				{
					DP data = m_invalidBytes.Dequeue();
					sb.Append(string.Format("{0}:0x{1}", data.offset, System.Convert.ToString(data.data, 16)));
					if (m_invalidBytes.Count > 0)
						sb.Append(", ");
				}
				return sb.ToString();
			}

			/// <summary>
			/// Test to see if the passed in byte is a valid start byte for an UTF8 data point.
			/// </summary>
			/// <param name="b"></param>
			/// <returns>true if it is, otherwise false</returns>
			private bool IsValidBeginByte(byte b)
			{
				if ((b & kLeft1BitMask) == 0)
					return true;	// sequenceLen = 1
				else if((b & kLeft3BitsMask) == kLeft2BitsMask)
					return true;	// sequenceLen = 2
				else if((b & kLeft4BitsMask) == kLeft3BitsMask)
					return true;	// sequenceLen = 3
				else if((b & kLeft5BitsMask) == kLeft4BitsMask)
					return true;	// sequenceLen = 4
				return false;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determine if the data is UTF8 encoded
			/// </summary>
			/// <param name="utf8"></param>
			/// <returns></returns>
			/// <remarks>This algorithm uses the pattern described in the Unicode book for
			/// UTF-8 data patterns.  It has been customized.</remarks>
			/// ------------------------------------------------------------------------------------
			public bool IsUTF8String(out byte badCodePoint, out int bytePosition, out int badBytes)
			{
				bool findAllErrors = true;
				bool foundANYError = false;
				// If there is no data, it is not UTF8
				badCodePoint = 0;
				bytePosition = 0;
				badBytes = 0;
				int bytesUntilNextCodePoint = 0;
				if (m_Data.Length < 1)
					return false;

				int sequenceLen = 1;
//				bool multiByteSequenceFound = false;

				// look through the buffer but stop 10 bytes before so we don't run off the
				// end while checking
				for (int i = 0; i < m_Data.Length; i += sequenceLen)
				{
					byte by = m_Data[i];
					bool foundError = false;
					// If the leftmost bit is 0, then this is a 1-byte character
					if ((by & kLeft1BitMask) == 0)
						sequenceLen = 1;
					else if((by & kLeft3BitsMask) == kLeft2BitsMask)
					{
						// If the byte starts with 110, then this will be the first byte
						// of a 2-byte sequence
						sequenceLen = 2;
						// if the second byte does not start with 10 then the sequence is invalid
						if(m_ByteLength <= i+1 || (m_Data[i + 1] & kLeft2BitsMask) != 0x80)
						{
							badCodePoint = by;
							bytePosition = i+1;	// not zero based, but 1 based for user
							if (!findAllErrors)
								return false;
							foundError = true;
						}
					}
					else if((by & kLeft4BitsMask) == kLeft3BitsMask)
					{
						// If the byte starts with 1110, then this will be the first byte of
						// a 3-byte sequence
						sequenceLen = 3;
						if ((m_ByteLength <= i+1 || (m_Data[i+1] & kLeft2BitsMask) != 0x80) ||
							(m_ByteLength <= i+2 || (m_Data[i+2] & kLeft2BitsMask) != 0x80))
						{
							badCodePoint = by;
							bytePosition = i+1;	// not zero based, but 1 based for user
							if (!findAllErrors)
								return false;
							foundError = true;
						}
					}
					else if((by & kLeft5BitsMask) == kLeft4BitsMask)
					{
						// if the byte starts with 11110, then this will be the first byte of
						// a 4-byte sequence
						sequenceLen = 4;
						if ((m_ByteLength <= i+1 || (m_Data[i+1] & kLeft2BitsMask) != 0x80) ||
							(m_ByteLength <= i+2 || (m_Data[i+2] & kLeft2BitsMask) != 0x80) ||
							(m_ByteLength <= i+3 || (m_Data[i+3] & kLeft2BitsMask) != 0x80))
						{
							badCodePoint = by;
							bytePosition = i+1;	// not zero based, but 1 based for user
							if (!findAllErrors)
								return false;
							foundError = true;
						}
					}
					else
					{
						badCodePoint = by;
						bytePosition = i+1;	// not zero based, but 1 based for user
						if (!findAllErrors)
							return false;
						foundError = true;
					}

					if (foundError)
					{
						if (m_invalidBytes.Count < MaxInvalidBytes)
							m_invalidBytes.Enqueue(new DP(bytePosition, badCodePoint));
						System.Diagnostics.Debug.WriteLine("badData: index=" + System.Convert.ToString(i + 1) + " ");
						bytesUntilNextCodePoint = i;
						// now search until we find a valid begin byte for utf8 sequence
						while (i < m_ByteLength && IsValidBeginByte(m_Data[++i]) == false)
						{
							if (m_invalidBytes.Count < MaxInvalidBytes)
								m_invalidBytes.Enqueue(new DP(i + 1, m_Data[i]));
						}
						sequenceLen = 0;
						bytesUntilNextCodePoint = i - bytesUntilNextCodePoint;
						badBytes += bytesUntilNextCodePoint;	// bytes including bytes to get to start byte
						foundANYError = true;
					}
//					else if (sequenceLen > 1)
//						multiByteSequenceFound = true;
				}
				return !foundANYError;
//				if (foundANYError)
//					return false;
//				return true || multiByteSequenceFound;	// didn't hit an invalid byte, so should be utf8
			}


			public string String()
			{
				if (m_String == null)
				{
					System.Text.Decoder utf8Decoder = System.Text.Encoding.UTF8.GetDecoder();

					int charCount = utf8Decoder.GetCharCount(m_Data, 0, m_ByteLength);
					Char[] chars = new Char[charCount];
					utf8Decoder.GetChars(m_Data, 0, m_ByteLength, chars, 0);
					m_String = new string(chars);
				}
				return m_String;
			}

			public byte[] ToBytes()
			{
				if (m_Data == null)
				{
					System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding(false, true);
					m_ByteLength = utf8.GetByteCount(m_String.ToCharArray());
					m_Data = new Byte[m_ByteLength];
					utf8.GetBytes(m_String, 0, m_String.Length, m_Data, 0);
				}
				return m_Data;
			}

			public int ByteLength
			{
				get { ToBytes(); return m_ByteLength; }
			}
		}

		/// <summary>
		/// Searches the given data bytes for the given infield marker.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="inFieldMarker"></param>
		/// <param name="markerByteLength"></param>
		/// <returns>index into array where start of marker was found</returns>
		long SearchForInFieldMarker(byte[] data, int startIndex, string inFieldMarker)
		{
			ClsStringToOrFromBytes stringToBytes = new ClsStringToOrFromBytes(inFieldMarker);
			return FindFirstMatch(data, startIndex, stringToBytes.ToBytes());
		}

		string ConvertBytes(string marker, byte[] data, int start, int end, IEncConverter converter, int lineNumber)
		{
			string result="";
			if (end - start <= 0)
			{
				// if the data to convert is of length zero (or less) then just return an empty string
				return result;
			}
			else if (converter != null)
			{
				int len = end-start;
				byte[] subData = new byte[len];
				for (int i=0; i<len; i++)
					subData[i] = data[start+i];

				try
				{
					result = converter.ConvertToUnicode(subData);
				}
				catch (System.Exception e)
				{
					Log.AddUniqueHighPriorityError(converter.Name,
						String.Format(Sfm2XmlStrings.EncodingConverter0Failed1, converter.Name, e.Message));
				}
			}
			else
			{
				// We have no Encoding Converter, so assume text is in UTF8
				byte[] workspace = new byte[end - start];
				for (int i = 0; i < end - start; i++)
					workspace[i] = data[start + i];
				ClsStringToOrFromBytes workspaceAsString = new ClsStringToOrFromBytes(workspace);
				int cMaxCodes = 20;
				if (m_cIllegalCodes < cMaxCodes)
				{
					byte badCodePoint;
					int badBytePosition;
					int badByteCount;	// add a count msg to the line.
					if (!workspaceAsString.IsUTF8String(out badCodePoint, out badBytePosition, out badByteCount))
					{
						// contains non-utf8 data
						if (badByteCount > ClsStringToOrFromBytes.MaxInvalidBytes)
							Log.AddError(m_SfmFileName, lineNumber,
								String.Format(Sfm2XmlStrings.Line0_SFM1ContainsIllegalUTF8Count2Max3IndexData4,
								lineNumber, marker, badByteCount, ClsStringToOrFromBytes.MaxInvalidBytes,
								workspaceAsString.InvalidByteString()));
						else if (badByteCount > 1)
							Log.AddError(m_SfmFileName, lineNumber,
								String.Format(Sfm2XmlStrings.Line0_SFM1ContainsIllegalUTF8Count2IndexData3,
								lineNumber, marker, badByteCount, workspaceAsString.InvalidByteString()));
						else
							Log.AddError(m_SfmFileName, lineNumber,
								String.Format(Sfm2XmlStrings.Line0_SFM1ContainsIllegalUTF8Code2,
								lineNumber, marker, System.Convert.ToString(badCodePoint, 16)));
						m_cIllegalCodes++;	// += badByteCount;
						if (m_cIllegalCodes == cMaxCodes)
						{
							Log.AddError(m_SfmFileName, m_SfmLineNumber, String.Format(Sfm2XmlStrings.InvalidCodepointsHaveBeenLogged, cMaxCodes));
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

		private bool AddUniqueMarker(ref SortedList foundMarkers, ref byte[] rawData, string markerData, ClsInFieldMarker inFieldMarker, int foundPos, bool isBeginMarker)
		{
#if false
			System.Diagnostics.Debug.WriteLine(" ** Found <" + markerData + "> at position="+foundPos.ToString()+", isBegin="+isBeginMarker.ToString());
#endif
			// If the found index already exists in the collection, then we have a possible error condition.
			if (foundMarkers.Contains(foundPos))
			{
				// see which one is a longer marker and use that one.
				int index = foundMarkers.IndexOfKey(foundPos);
				ClsFoundInFieldMarker existing = foundMarkers.GetByIndex(index) as ClsFoundInFieldMarker;
				int length = existing.Length;
				if (length == markerData.Length)
				{
					// Log.AddError("Error: InFieldMarker '" + InFieldMarker.Begin + "' is defined in more than one inFieldMarker entry.");
				}
				else
				{
					if (markerData.Length < length)
					{
						// the new one is smaller, so keep looking to see if there's a place for this one in the data
						foundPos = (int)SearchForInFieldMarker(rawData, foundPos+markerData.Length, markerData);
						if (foundPos >= 0)
						{
							System.Diagnostics.Debug.WriteLine(" ** Above not added --- still looking");
							return AddUniqueMarker(ref foundMarkers, ref rawData, markerData, inFieldMarker, foundPos, isBeginMarker);
						}
						else
						{
							System.Diagnostics.Debug.WriteLine(" ** Above not added --- can't replace what was there and no others found");
							return false;
						}
					}
					else
					{
						// the one we just found is longer so remove the previous one and add this one.
						ClsFoundInFieldMarker oldMarker = foundMarkers.GetByIndex(index) as ClsFoundInFieldMarker;
						foundMarkers.RemoveAt(index);
						ClsFoundInFieldMarker newFoundMarker = null;
						if (isBeginMarker)
							newFoundMarker = new ClsFoundInFieldMarker(inFieldMarker.Begin, inFieldMarker);
						else
							newFoundMarker = new ClsFoundInFieldMarker(markerData);
						foundMarkers.Add(foundPos, newFoundMarker);

						// now look for the removed marker ahead in the data
						foundPos = (int)SearchForInFieldMarker(rawData, foundPos+oldMarker.Length, oldMarker.Marker);
						if (foundPos >= 0)
						{
							System.Diagnostics.Debug.WriteLine(" ** Above added --- now looking for one removed");
							return AddUniqueMarker(ref foundMarkers, ref rawData, oldMarker.Marker, oldMarker.ClsInFieldMarker, foundPos, oldMarker.IsBegin);
						}

					}
				}
			}
			else
			{
				// save the found information
				ClsFoundInFieldMarker newFoundMarker = null;
				if (isBeginMarker)
					newFoundMarker = new ClsFoundInFieldMarker(inFieldMarker.Begin, inFieldMarker);
				else
					newFoundMarker = new ClsFoundInFieldMarker(markerData);
				foundMarkers.Add(foundPos, newFoundMarker);
			}

//			// Make sure this marker isn't a subset of an existing one, or that this one isn't partialy contained
//			//  by an existing infield
//			// save the found information
//			ClsFoundInFieldMarker newFoundMarker = new ClsFoundInFieldMarker(inFieldMarker.Begin, inFieldMarker);
//			foundMarkers.Add(foundPos, newFoundMarker);

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
			string output = "";

			// Determine the default Encoding Converter based on the given SFM marker:
			ClsFieldDescription mainFieldDescription = m_FieldDescriptionsTable[markerSFM] as ClsFieldDescription;
			ClsLanguage cLang = m_Languages[mainFieldDescription.Language] as ClsLanguage;
			IEncConverter iDefaultConverter = null;
			if (cLang != null)
				iDefaultConverter = cLang.EncCvtr;
			else
				iDefaultConverter = null;

			IEncConverter currentConverter = iDefaultConverter;

			// It is possible that markers will not be closed in the order they are opended, so we will
			// provide an ordererd list to store open markers, to help with closing them in the right order:
			System.Collections.ArrayList openMarkers = new System.Collections.ArrayList();

			// For efficiecy, we will maintain a sorted container which keeps track of the first
			// instance of each infield marker. Every time we process an infield marker, we remove
			// it from the container, search for the next instance of that marker, and add this new
			// instance (if found) to the container.
			// To start with, we will search for the first instance of every possible marker. From then
			// on, we only search for further instances of ones we've found.
			SortedList foundMarkers = new SortedList();

			// Find the first instance of each begin and end markers:
			foreach (DictionaryEntry fieldEntry in m_InFieldMarkers)
			{
				ClsInFieldMarker InFieldMarker = fieldEntry.Value as ClsInFieldMarker;

				// There can be only one begin marker:
				int foundPos = (int)SearchForInFieldMarker(markerData, 0, InFieldMarker.Begin);
				if (foundPos >= 0)
				{
					AddUniqueMarker(ref foundMarkers, ref markerData, InFieldMarker.Begin, InFieldMarker, foundPos, true);
					// There can be more than one end marker:
					foreach (string endMarker in InFieldMarker.End)
					{
						foundPos = (int)SearchForInFieldMarker(markerData, 0, endMarker);
						if (foundPos >= 0)
						{
							AddUniqueMarker(ref foundMarkers, ref markerData, endMarker, InFieldMarker, foundPos, false);
						}
					}
				}
			}

			// Iterate through the sorted list of markers, removing and processing the "first" one each time:
			int iMarkerDataPos = 0;	// current pointer to text after a marker
			while (foundMarkers.Count > 0)
			{
				// Get and remove the top marker in the sorted list:
				ClsFoundInFieldMarker currentMarker = foundMarkers.GetByIndex(0) as ClsFoundInFieldMarker;
				int markerStartPos = (int)(foundMarkers.GetKey(0));
				foundMarkers.RemoveAt(0);

				if (markerStartPos >= iMarkerDataPos)
				{
					// Convert the text between this marker and the previous one and add it to the main output:
					string convertedData = ConvertBytes(mainFieldDescription.SFM, markerData, iMarkerDataPos, markerStartPos, currentConverter, lineNumber);
					output += convertedData;

					// Processing for Begin Markers is different from that for End Markers:
					if (currentMarker.IsBegin)
					{
						// Stack the new marker:
						openMarkers.Add(currentMarker);

						// Add the XML equivalent of the current marker to the main output:
						output += "<"+currentMarker.ClsInFieldMarker.ElementName+">";	// ElementAndAttributes();

						// If the inline marker has its own encoding, use that from now on:
						if (currentMarker.HasLangEncoding)
						{
							string langEncoding = currentMarker.LanguageEncoding;
							currentConverter = (m_Languages[langEncoding] as ClsLanguage).EncCvtr;
						}

						// We can't store "EndWithWord" or "EndWithField" markers ahead of time, so
						// compute those here and now.
						if (currentMarker.ClsInFieldMarker.EndWithWord)
						{
							int ichFound = SearchForEndOfWord(markerData, markerStartPos);
							if (ichFound >= 0)
								AddUniqueMarker(ref foundMarkers, ref markerData, " ",
									currentMarker.ClsInFieldMarker, ichFound, false);
						}
						else if (currentMarker.ClsInFieldMarker.EndWithField)
						{
							int ichFound = markerData.Length;
							AddUniqueMarker(ref foundMarkers, ref markerData, "",
								currentMarker.ClsInFieldMarker, ichFound, false);
						}
					}
					else // currentMarker is an end marker
					{
						// Because and End Marker can close more than one type of Begin Marker at the same time,
						// we need to build a list of all Begin Markers closed by this End Marker:
						System.Collections.Stack affectedBeginMarkers = new Stack();
						foreach (ClsFoundInFieldMarker openMarker in openMarkers)
						{
							if (openMarker.ContainsEndMarker(currentMarker.Marker))
								affectedBeginMarkers.Push(openMarker);
						}

						// If markers are not closed in the order they were opened, we will have to do some
						// extra closing and re-opening:
						System.Collections.Stack markersToBeReopened = new System.Collections.Stack();

						// Now scan back through the open markers (latest towards earliest), closing
						// them all and storing those that need to be reopened after we close the
						// earliest of them:
						for (int openMarkerIndex = openMarkers.Count - 1; affectedBeginMarkers.Count > 0; openMarkerIndex--)
						{
							ClsFoundInFieldMarker markerToClose = openMarkers[openMarkerIndex] as ClsFoundInFieldMarker;

							// Close this marker:
							output += "</" + markerToClose.ElementName + ">";
							openMarkers.RemoveAt(openMarkerIndex);

							// If the marker we just closed is not the top element in our Affected Markers stack, then
							// we need to re-open it later:
							ClsFoundInFieldMarker affectedMarker = affectedBeginMarkers.Peek() as ClsFoundInFieldMarker;
							if (affectedMarker.Marker == markerToClose.Marker)
								affectedBeginMarkers.Pop(); // We really did need to close this one!
							else
							{
								// We'll have to repoen this one later:
								markersToBeReopened.Push(markerToClose);
							}
						}

						// Now re-open those that were not supposed to be closed:
						while (markersToBeReopened.Count > 0)
						{
							ClsFoundInFieldMarker markerToReopen = markersToBeReopened.Pop() as ClsFoundInFieldMarker;
							output += "<"+markerToReopen.ClsInFieldMarker.ElementName+">";	// ElementAndAttributes();
							openMarkers.Add(markerToReopen);
						}
					}
					// Update our position in the raw data to skip past the marker we've just processed:
					// (But don't skip over the marker itself if the "marker" was the "EndOfWord" or
					// "EndOfField".
					ClsStringToOrFromBytes markerAsBytes = new ClsStringToOrFromBytes(currentMarker.Marker);
					if (currentMarker.Length > 0 && currentMarker.Marker != " ")
						iMarkerDataPos = markerStartPos + markerAsBytes.ByteLength;
					else
						iMarkerDataPos = markerStartPos;
				}

				// Find next instance of currentMarker in input data: but not if we're handling an
				// end marker for EndOfWord or EndOfField.
				if (currentMarker.Marker.Length > 0 && currentMarker.Marker != " ")
				{
					int foundPos = (int)SearchForInFieldMarker(markerData, iMarkerDataPos, currentMarker.Marker);
					if (foundPos >= 0)
					{
						// If the found index already exists in the collection, then we have an error condition.
						if (foundMarkers.Contains(foundPos) && currentMarker.IsBegin)
						{
							// FIX: needs to handle (again) the case where the begin marker is a subset of the end marker
							ClsFoundInFieldMarker matchingMarker = foundMarkers[foundPos] as ClsFoundInFieldMarker;
							if (matchingMarker.IsBegin)	// can't also be begin , must be end to be valid
								Log.AddError(String.Format(Sfm2XmlStrings.InFieldMarker0IsMultiplyDefined, currentMarker.Marker));
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
			string convertedDataString = ConvertBytes(mainFieldDescription.SFM, markerData, iMarkerDataPos, markerData.Length, currentConverter, lineNumber);

			if (mainFieldDescription.Type.ToLowerInvariant() == "date")
			{
				// handle any date processing here - validating and changing forms
				try
				{
					DateTime dt = System.DateTime.Parse(convertedDataString);
					if (dt.Year < 1800)
						throw new Exception();	// SQL Server insists year >= 1753 for datetime.  See LT-8073.
					string newDate = dt.ToString("yyy-MM-dd hh:mm:ss.fff");
					convertedDataString = convertedDataString.Replace(System.Environment.NewLine, "");	// remove newlines

					if (newDate.IndexOf(convertedDataString) < 0)
					{
						Log.AddSFMWarning(mainFieldDescription.SFM,
							String.Format(Sfm2XmlStrings.DateChangedFrom1To2,
								mainFieldDescription.SFM, convertedDataString, newDate));
					}
					convertedDataString = newDate;
				}
				catch
				{
					// LT-5352: don't log date errors if the dt field is empty
					if (convertedDataString.Trim().Length > 0)
					{
						Log.AddError(m_SfmFileName, lineNumber, String.Format(Sfm2XmlStrings.X1HasAnUnrecognizedDateForm2, lineNumber.ToString(), mainFieldDescription.SFM, convertedDataString));
						convertedDataString = "";	// don't pass it on - ignore it
					}
				}
			}
			output += convertedDataString;

			// Close any markers left open by lazy SFM code:
			for (int openMarkerIndex = openMarkers.Count - 1; openMarkerIndex >= 0; openMarkerIndex--)
			{
				ClsFoundInFieldMarker markerToClose = openMarkers[openMarkerIndex] as ClsFoundInFieldMarker;
				// Close this marker:
				output += "</" + markerToClose.ElementName + ">";
			}

			return output;
		}

		private int SearchForEndOfWord(byte[] markerData, int ichStart)
		{
			int foundPos = -1;
			int ichSpace = (int)SearchForInFieldMarker(markerData, ichStart, " ");
			int ichTab = (int)SearchForInFieldMarker(markerData, ichStart, "\t");
			int ichReturn = (int)SearchForInFieldMarker(markerData, ichStart, "\r");
			int ichLinefeed = (int)SearchForInFieldMarker(markerData, ichStart, "\n");
			if (ichSpace >= 0)
				foundPos = ichSpace;
			if (ichTab >= 0 && ichTab < foundPos)
				foundPos = ichTab;
			if (ichReturn >= 0 && ichReturn < foundPos)
				foundPos = ichReturn;
			if (ichLinefeed >= 0 && ichLinefeed < foundPos)
				foundPos = ichLinefeed;
			return foundPos;
		}

//        private void AddHelpOutputError(System.Xml.XmlTextWriter xmlHelpOutput, string msg, string sfm, int line, long errCount)
//        {
//            xmlHelpOutput.WriteStartElement("Error");
//            xmlHelpOutput.WriteAttributeString("sfm", sfm);
//            xmlHelpOutput.WriteAttributeString("msg", msg);
//            xmlHelpOutput.WriteAttributeString("cnt", errCount.ToString());
//            xmlHelpOutput.WriteAttributeString("line", line.ToString());
//            xmlHelpOutput.WriteEndElement();
//        }

//        private void AddHelpOutputMsg(System.Xml.XmlTextWriter xmlHelpOutput, string msg)
//        {
//            xmlHelpOutput.WriteStartElement("Trace");
//            xmlHelpOutput.WriteAttributeString("msg", msg);
//            xmlHelpOutput.WriteEndElement();
//        }

//        private void OpenHelpOutputElement(System.Xml.XmlTextWriter xmlHelpOutput, string elementName, string sfm, int line)
//        {
//            xmlHelpOutput.WriteStartElement(elementName);
//            xmlHelpOutput.WriteAttributeString("startLine", line.ToString());
//            xmlHelpOutput.WriteAttributeString("startSfm", sfm);
//        }


//        private void CloseHelpOutputElement(System.Xml.XmlTextWriter xmlHelpOutput, string sfm, int line)
//        {
//            // add an entry to the help xml file
////			xmlHelpOutput.WriteAttributeString("endLine", line.ToString());
////			xmlHelpOutput.WriteAttributeString("endSfm", sfm);
//            xmlHelpOutput.WriteEndElement();
//        }

//		private void ProcessSfmFile(System.Xml.XmlTextWriter xmlOutput)
//		{
//			// Add another xml output file that is similiar to phase one but includes
//			// additional information that will be/is useful for helping diagnose problems
//			// during the import process.
//			long errCount=0;
//			try
//			{
//				int confusedLineStart=0;		// Line number that the confusion started on
//				bool amConfused = false;	// True if a marker is found that doesn't fit in the current context
//				// In that case, the process is to bump up to a first level and then
//				// wait until we are valid again and then continue.
//				ByteReader reader = new ByteReader(m_SfmFileName, ref Log);
//				byte[] sfmData;
//				byte[] badSfmData;

//				string currentSfm = null;
//				Stack currentPath = new Stack();	// stack of ClsPathObject's

//				// Create a new root entry for our current path. It will contain the name of the root
//				// hierarchy entry, and a used SFM list containing no items.
//				currentPath.Push(new ClsPathObject(m_Root.Name));

//				while (reader.GetNextSfmMarkerAndData(out currentSfm, out sfmData, out badSfmData))
//				{
//					m_SfmLineNumber = reader.LineNumber;
//					// If the currentSfm is empty, then this is a case where there is data before the marker
//					if (currentSfm.Length == 0)
//					{
//						bool nonWSdata = false;	// non White Space data found
//						// see if it's just white space
//						for (int i=0; i < sfmData.Length && !nonWSdata; i++)
//						{
//							switch (sfmData[i])
//							{
//								case 0x0a:	// cr
//								case 0x0d:	// lf
//								case 0x20:	// space
//								case 0x09:	// tab
//									break;
//								default:
//									nonWSdata = true;
//									break;
//							}
//						}

//						if (nonWSdata == false)
//							continue;

//						string emsg = CharDataBeforeMarkerMessage(sfmData, reader.LineNumber);
//						Log.AddError(m_SfmFileName, reader.LineNumber, emsg);
//						// add an entry to the help xml file
//						errCount++;
//						////AddHelpOutputError(xmlHelpOutput, emsg, currentSfm, reader.LineNumber, errCount);
//						////					xmlHelpOutput.WriteStartElement("Error");
//						////					xmlHelpOutput.WriteAttributeString("msg", emsg);
//						////					xmlHelpOutput.WriteAttributeString("cnt", errCount.ToString());
//						////					xmlHelpOutput.WriteAttributeString("line", reader.LineNumber.ToString());
//						////					xmlHelpOutput.WriteEndElement();
//						continue;
//					}

//					// LT-1926 Ignore all markers that start with underscore (shoebox markers)
//					if (currentSfm.StartsWith("_"))
//						continue;

//					// Make sure the marker is valid for the xml output file [?,<,&, and etc are invalid]
//					// If this is a marker that has been taged to be ignored - then ignore it
//					if (m_FieldsToIgnore.ContainsKey(currentSfm))
//						continue;

//					// Get the current location's hierarchy entry:
//					if (currentPath.Count <= 0)
//						throw new System.Exception("Current path is empty at start of loop.");

//					// Test that the currentSfm exists in the FieldDescriptions table:
//					if (!m_FieldDescriptionsTable.ContainsKey(currentSfm))
//					{
//						// allow the unknown marker to be treated as a residue field
//						if (true)
//						{
//							// if in a sense add residue there
//							// if in a entry add residue there
//						}

//						// only log it once [lt-2217 and only if not in the list of ignored fields]
//						if (!m_FieldDescriptionsTableNotFound.ContainsKey(currentSfm) &&
//							!m_FieldsToIgnore.ContainsKey(currentSfm))
//						{
//							m_FieldDescriptionsTableNotFound.Add(currentSfm,null);
//							Log.AddSFMError(m_SfmFileName, reader.LineNumber, currentSfm,
//								String.Format(Sfm2XmlStrings.SFM1IsUndefinedOrInvalid, reader.LineNumber, currentSfm));
//						}
//						Log.AddSFMNotDefined(currentSfm);
//						continue;
//					}

//
//					ClsHierarchyEntry currentLocation = m_Hierarchy[(currentPath.Peek() as ClsPathObject).Name] as ClsHierarchyEntry;
//					if (currentLocation == null)
//						throw new System.Exception("Current path location is undefined.");

//					// see if this sfm is labeled in the hierarchy as 'unique' - only one unique marker per entry
//					bool uniqueSfm = currentLocation.UniqueFieldsContains(currentSfm);

//					// See if the current SFM appears in the BeginFields list of any hierarchy entry:
//					ClsHierarchyEntry newBeginHierarchyEntry = m_BeginMarkerHierarchyEntries[currentSfm] as ClsHierarchyEntry;
//					if (newBeginHierarchyEntry == null)
//					{
//						// see if this is a marker that isn't used in the hierarchy
//						if (m_MarkersNotInHierarchy.ContainsKey(currentSfm))
//						{
//							//						Log.AddError("Error in SFM file at line " + reader.LineNumber + ": SFM '" + currentSfm + "' is not used in any hierarchy items.");
//							Log.AddSFMError(m_SfmFileName, reader.LineNumber, currentSfm,
//								String.Format(Sfm2XmlStrings.SFM1IsNotUsedInAnyHierarchyItems, reader.LineNumber, currentSfm));
//							continue;
//						}

//						// First check to see if the marker is valid for any of the currently open objects in the
//						// path.  If so, and it's not valid for the top most one, just add it and continue on in
//						// the current frame (no change to the stack/path).
//						// If it is valid at the top most level, then put it out now instead of waiting for later.
//						if (AddToOpenObjects(currentPath, currentSfm, sfmData, reader.LineNumber))
//							continue;

//						bool loggedError = false;
//						//
//						// loop until either we find a place we can use the current SFM, or until we find
//						// an error condition (such as navigated to the root of the hierarchy):
//						do
//						{
//							// The current SFM does not start a new hierarchy level. See if we can use it at
//							// the current location:
//							bool appearsInMulti = currentLocation.MultiFieldsContains(currentSfm);
//							bool appearsInAdditional = currentLocation.AdditionalFieldsContains(currentSfm);
//							bool usedAlready = (currentPath.Peek() as ClsPathObject).ContainsSfm(currentSfm);
//							bool noUniqueProblem = !uniqueSfm || !(currentPath.Peek() as ClsPathObject).AlreadContainsUniqueSfm;
//							bool canUseHere = appearsInMulti || (appearsInAdditional && !usedAlready && noUniqueProblem);
//							if (canUseHere)
//								break;
//							else
//							{
//								if (currentPath.Count > 1)
//								{
//									// add an entry to the help xml file
//									////CloseHelpOutputElement(xmlHelpOutput, currentSfm, reader.LineNumber);

//									// Move up one level:
//									xmlOutput.WriteEndElement();
//									currentPath.Pop();
//									// Have to reset the currentLocation based on the new top of the stack
//									currentLocation = m_Hierarchy[(currentPath.Peek() as ClsPathObject).Name] as ClsHierarchyEntry;
//								}
//								else
//								{
//									string errMsg = String.Format(Sfm2XmlStrings.CurrentSFM1CannotBeUsed, reader.LineNumber, currentSfm);
//									ClsHierarchyEntry entry = GetClsHierarchyEntryForSfm(currentSfm);
//									if (entry != null)
//									{
//										errMsg += String.Format(Sfm2XmlStrings.NoOpen0ItemToContainThisMarker, entry.KEY);
//									}
//									Log.AddError(errMsg);
//									//////AddHelpOutputError(xmlHelpOutput, errMsg, currentSfm, reader.LineNumber, ++errCount);
//									loggedError = true;
//								}
//							}
//						} while (currentPath.Count > 1);

//						if (currentPath.Count <= 1 && !loggedError)		// make sure we didn't just show the error above
//						{
//							string errMsg = String.Format(Sfm2XmlStrings.CurrentSFM1CannotBeUsedCurrently, reader.LineNumber, currentSfm);
//							ClsHierarchyEntry entry = GetClsHierarchyEntryForSfm(currentSfm);
//							if (entry != null)
//							{
//								errMsg += String.Format(Sfm2XmlStrings.NoOpen0ItemToContainThisMarker, entry.KEY);
//							}
//							Log.AddError(errMsg);
//							////AddHelpOutputError(xmlHelpOutput, errMsg, currentSfm, reader.LineNumber, ++errCount);
//							loggedError = true;
//						}
//						if (loggedError)
//							continue;	// don't process the sfm and data if we've just processed an error condition
//					}
//					else
//					{
//						// see if this sfm is labeled in the hierarchy as 'unique' - only one unique marker per entry
//						uniqueSfm = newBeginHierarchyEntry.UniqueFieldsContains(currentSfm);

//						bool beginFieldsBelongsToCurrentLocation = (newBeginHierarchyEntry.KEY == currentLocation.KEY);
//						bool currentSfmAppearsInCurrentMulti = currentLocation.MultiFieldsContains(currentSfm);
//						bool usedAlready = (currentPath.Peek() as ClsPathObject).ContainsSfm(currentSfm);
//						bool noUniqueProblem = !uniqueSfm || !(currentPath.Peek() as ClsPathObject).AlreadContainsUniqueSfm;
//						bool canReuseSfm = beginFieldsBelongsToCurrentLocation &&
//							(currentSfmAppearsInCurrentMulti || !usedAlready) && noUniqueProblem;
//						if (!canReuseSfm)
//						{
//							// We will be opening a new hierarchy level.
//							// Deduce optimal path to get us from currentLocation to newBeginHierarchyEntry:
//							Stack downPath = OptimalPathFromCommonAncestorToHierarchyEntry(currentPath, newBeginHierarchyEntry);

//							// Check that a path was created:
//							if (downPath == null || downPath.Count == 0)
//							{
//								if (!amConfused)
//								{
//									string errMsg = String.Format(Sfm2XmlStrings.CurrentSFM1JumpsTooManyLevels, reader.LineNumber, currentSfm);
//									ClsHierarchyEntry entry = GetClsHierarchyEntryForSfm(currentSfm);
//									if (entry != null)
//									{
//										errMsg += String.Format(Sfm2XmlStrings.NoOpen0ItemToContainThisMarker + entry.KEY);
//									}
//									Log.AddFatalError(m_SfmFileName, reader.LineNumber, errMsg);
//									Log.AddFatalError(Sfm2XmlStrings.InputProcessingHasBeenPostponedForHeadWord);
//									////AddHelpOutputError(xmlHelpOutput, errMsg, currentSfm, reader.LineNumber, ++errCount);

//									// System.Diagnostics.Debug.WriteLine(errMsg);
//									// ShowStackElements(currentPath);

//									amConfused = true;
//									confusedLineStart = reader.LineNumber;

//									while(currentPath.Count > 1)
//									{
//										ClsPathObject openObj = currentPath.Pop() as ClsPathObject;
//										openObj.FlushPendingSfmData(this, xmlOutput);
//										xmlOutput.WriteEndElement();

//										// add an entry to the help xml file
////										CloseHelpOutputElement(xmlHelpOutput, currentSfm, reader.LineNumber);
//									}
//								}
//								continue;
//							}

//							// Close elements
//							string commonAncestorKey = GetPathObjectKey(downPath.Peek());
//							while (GetPathObjectKey(currentPath.Peek()) != commonAncestorKey)
//							{
//								ClsPathObject openObj = currentPath.Pop() as ClsPathObject;
//								openObj.FlushPendingSfmData(this, xmlOutput);
//								xmlOutput.WriteEndElement();
//								//CloseHelpOutputElement(xmlHelpOutput, currentSfm, reader.LineNumber);
//							}
//							// If the destination is the Common Ancestor, then close the CA and reopen it:
//							if (newBeginHierarchyEntry.KEY == commonAncestorKey)
//							{
//								ClsPathObject openObj = currentPath.Pop() as ClsPathObject;
//								openObj.FlushPendingSfmData(this, xmlOutput);
//								xmlOutput.WriteEndElement();
//								//CloseHelpOutputElement(xmlHelpOutput, currentSfm, reader.LineNumber);

//								// Reopen:
//								//OpenHelpOutputElement(xmlHelpOutput, commonAncestorKey, currentSfm, reader.LineNumber);
//								xmlOutput.WriteStartElement(commonAncestorKey);
//								currentPath.Push(new ClsPathObject(commonAncestorKey));
//								if (currentPath.Count == 2)
//									m_NumElements++;
//							}
//							downPath.Pop(); // Skip Common Ancestor
//

//							// Open element(s) along the downPath:
//							while (downPath.Count > 0)
//							{
//								if (amConfused)
//								{
//									amConfused = false;
//									int endline = reader.LineNumber;
//									int difLines = endline-confusedLineStart;
//									Log.AddFatalError(m_SfmFileName, endline,
//										String.Format(Sfm2XmlStrings.Skipped0LinesOfInputDueToError, difLines.ToString()));
//								}
//								string currentLevelKey = GetPathObjectKey(downPath.Pop());
//								xmlOutput.WriteStartElement(currentLevelKey);
//								//OpenHelpOutputElement(xmlHelpOutput, currentLevelKey, currentSfm, reader.LineNumber);

//								currentPath.Push(new ClsPathObject(currentLevelKey));
//								if (currentPath.Count == 2)
//									m_NumElements++;
//							}
//						}
//					}
//					// This is where we add the SFM to the XML output. It is common to all logic paths
//					// which don't give errors.

//					// Add current SFM to our current InUse list:
//					(currentPath.Peek() as ClsPathObject).AddSfm(currentSfm, uniqueSfm);

//					// now process the data for this sfm
//					ProcessSFMandData(currentSfm, sfmData, reader.LineNumber, xmlOutput);
//				}

//				// Close the open elements back up to the root one still being open
//				while(currentPath.Count > 0)
//				{
//					ClsPathObject openObj = currentPath.Pop() as ClsPathObject;
//					openObj.FlushPendingSfmData(this, xmlOutput);
//					xmlOutput.WriteEndElement();
//					//CloseHelpOutputElement(xmlHelpOutput, currentSfm, reader.LineNumber);
//				}
//			}
//			catch (Exception e)
//			{
//				//AddHelpOutputError(xmlHelpOutput, "SomeException", e.Message, -999, -99);
//				System.Diagnostics.Debug.WriteLine(" SomeException:" + e.Message);
//			}

////			xmlHelpOutput.Flush();
////			xmlHelpOutput.Close();
//		}

		public void ProcessSFMandData(string currentSfm, byte[] sfmData, int lineNumber, System.Xml.XmlTextWriter xmlOutput)
		{
			// now process the data for this sfm
			string processedText = ProcessSFMData(currentSfm, sfmData, lineNumber);
			processedText = processedText.Replace(System.Environment.NewLine, " ");	// remove newlines
			processedText = processedText.Trim();	// remove whitespace

			// make sure the data is only in the following range:
			// 0x09, 0x0a, 0x0d, 0x20-0xd7ff, 0xe000-0xfffd
			// not using foreach so chars can be removed during processing w/o messing up the iterator
			System.Text.StringBuilder strTemp = new System.Text.StringBuilder(processedText);
			int lastPos = strTemp.Length;
			int pos = 0;
			while (pos < lastPos)
			{
				char c = strTemp[pos];
				if ((c >= 0x0020 && c<=0xd7ff) ||		// try to test by order of most common/expected
					(c >= 0xe000 && c<=0xfffd) ||
					c == 0x09 || c == 0x0d || c== 0x0a)
				{
					pos++;	// valid data
				}
				else
				{
					Log.AddError(m_SfmFileName, lineNumber,
						String.Format(Sfm2XmlStrings.SFM1ContainsInvalidCharValue2H, lineNumber, currentSfm, ((int)c).ToString("X")));
					strTemp.Remove(pos, 1);
					lastPos--;
					// don't bump the pos as we need to process the new char at this position
				}
			}

			if (strTemp.Length > 0)
			{
				Log.AddSFMWithData(currentSfm);
				// use the xml safe name
				ClsFieldDescription cfd = m_FieldDescriptionsTable[currentSfm] as ClsFieldDescription;
				xmlOutput.WriteStartElement(cfd.SFMxmlSafe);
				xmlOutput.WriteAttributeString("srcLine", lineNumber.ToString());
				string sForm = strTemp.ToString();
				// some users may mark the citation form, but not the lexeme form, so
				// leaving the citation form alone preserves that information for later
				// cleanup.  On the other hand, if both are marked, the citation forms
				// will have to be cleaned up.  You can't win, you can't break even,
				// and they won't let you quit playing...  :-(
				if (cfd.MeaningID == "lex" || // cfd.MeaningID == "cit"  ||
					cfd.MeaningID == "allo" || cfd.MeaningID == "ulf" ||
					cfd.MeaningID == "var" || cfd.MeaningID == "sub" )
					//cfd.MeaningID == "vard" || cfd.MeaningID == "varf" ||
					//cfd.MeaningID == "vari" || cfd.MeaningID == "vars" ||
					//cfd.MeaningID == "subc" || cfd.MeaningID == "subd" ||
					//cfd.MeaningID == "subi" || cfd.MeaningID == "subk" ||
					//cfd.MeaningID == "subp" || cfd.MeaningID == "subs")
				{
					try
					{
						string sAlloClass;
						string sMorphTypeWs;
						string sMorphType = GetMorphTypeInfo(ref sForm, out sAlloClass, out sMorphTypeWs);
						xmlOutput.WriteAttributeString("morphTypeWs", sMorphTypeWs);
						xmlOutput.WriteAttributeString("morphType", sMorphType);
						xmlOutput.WriteAttributeString("allomorphClass", sAlloClass);
						if (cfd.IsRef)
						{
							// add the new Var and Sub attributes
							string attribPrefix = cfd.MeaningID.ToLowerInvariant();
							xmlOutput.WriteAttributeString(attribPrefix + "TypeWs", cfd.RefFuncWS);	// comes from the combo box WS
							xmlOutput.WriteAttributeString(attribPrefix + "Type", cfd.RefFunc);		// combo box text
						}
					}
					catch (Exception ex)
					{
						// We have something we can't interpret. Give the user an Error message, but continue on if
						// the user ignores it. To continue on we use the entire string and assume it is a stem.
						string errMsg = String.Format(Sfm2XmlStrings.BadMorphMarkers012, lineNumber, cfd.SFMxmlSafe, ex.Message);
						Log.AddFatalError(m_SfmFileName, lineNumber, errMsg);
						xmlOutput.WriteAttributeString("morphTypeWs", "en");
						xmlOutput.WriteAttributeString("morphType", "stem");
						xmlOutput.WriteAttributeString("allomorphClass", "MoStemAllomorph");
					}
				}
				xmlOutput.WriteRaw(sForm);
				xmlOutput.WriteEndElement();
			}
			else
			{
				Log.AddSFMNoData(currentSfm);
			}

			// add to the help output file even if there is no data for the element
			//xmlHelpOutput.WriteStartElement((m_FieldDescriptionsTable[currentSfm] as ClsFieldDescription).SFMxmlSafe);
			//xmlHelpOutput.WriteAttributeString("line", lineNumber.ToString());
			//xmlHelpOutput.WriteEndElement();
		}

		protected virtual string GetMorphTypeInfo(ref string sForm, out string sAlloClass, out string sMorphTypeWs)
		{
			string sType = "stem";				// default
			sAlloClass = "MoStemAllomorph";		// default
			sMorphTypeWs = "en";				// default
			if (sForm.StartsWith("-") && sForm.EndsWith("-"))
			{
				sForm = sForm.Trim(new char[] { '-' });
				sType = "infix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("-"))
			{
				sForm = sForm.TrimStart(new char[] { '-' });
				sType = "suffix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.EndsWith("-"))
			{
				sForm = sForm.TrimEnd(new char[] { '-' });
				sType = "prefix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("~") && sForm.EndsWith("~"))
			{
				sForm = sForm.Trim(new char[] { '~' });
				sType = "suprafix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("=") && sForm.EndsWith("="))
			{
				sForm = sForm.Trim(new char[] { '=' });
				sType = "simulfix";
				sAlloClass = "MoAffixAllomorph";
			}
			else if (sForm.StartsWith("="))
			{
				sForm = sForm.TrimStart(new char[] { '=' });
				sType = "enclitic";
			}
			else if (sForm.EndsWith("="))
			{
				sForm = sForm.TrimEnd(new char[] { '=' });
				sType = "proclitic";
			}
			else if (sForm.StartsWith("*"))
			{
				sType = "bound stem";
			}
			return sType;
		}

		public class TreeNode
		{
			private string m_name;
			private ArrayList m_ancestors;
			private TreeNode m_leaf;

			public TreeNode Leaf { get { return m_leaf; }}
			public string Name { get { return m_name;}}
			public ICollection Ancestors {get { return m_ancestors; }}
			public void AddAncestor(TreeNode ancestor) { m_ancestors.Add(ancestor); ancestor.m_leaf = this; }
			public TreeNode(string name)
			{
				m_name = name;
				m_ancestors = new ArrayList();
			}
		}

		public class Tree
		{
//			private TreeNode m_leaf;

			public Tree(TreeNode leaf) { /*m_leaf = leaf; */}
		}

		public class ImportObjectManager
		{
			private ImportObject m_root;
			private ImportObject m_current;
			private Converter m_converter;
			private Hashtable m_OpenNodes;		// hash of nodes currently open on the tree: key="name"(ex:"entry") value="arraylist of ImportObjects"
			// private bool m_outputHasStarted = false;

			public ImportObjectManager(string rootName, Converter conv)
			{
				m_root = new ImportObject(rootName, null);
				m_current = m_root;
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("  Mgr::Current = " + m_current.AncestorLongNames());
#endif
				m_converter = conv;

				m_OpenNodes = new Hashtable();
				ArrayList rootLevel = new ArrayList();
				rootLevel.Add(m_root);
				m_OpenNodes.Add(rootName, rootLevel);

			}

			private void AddNewObject(ImportObject newEntry)	// will set current and add to m_opennodes hash
			{
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("Mgr::AddNewObject: " + newEntry.AncestorLongNames());
#endif
				// make sure there aren't any children nodes of the new entry at the same level as the newEntry
				RemoveChildrenForNewImportObject(newEntry.Parent, newEntry.Name, m_converter.m_HierarchyChildren, newEntry);

				// make sure there aren't any open like nodes at the same level
				foreach (ImportObject child in newEntry.Parent.Children)
				{
					if (child == newEntry)	// don't close the one we just added!
						continue;
					if (child.Name == newEntry.Name)
					{
						if (!child.Closed)
							CloseImportObject(child);
					}
				}

				m_current = newEntry;
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("  Mgr::Current = " + m_current.AncestorLongNames());
#endif
				if (m_OpenNodes.ContainsKey(newEntry.Name))
				{
					ArrayList nodes = m_OpenNodes[newEntry.Name] as ArrayList;
					nodes.Add(newEntry);
				}
				else
				{
					ArrayList nodes = new ArrayList();
					nodes.Add(newEntry);
					m_OpenNodes.Add(newEntry.Name, nodes);
				}
			}

			public ImportObject Current { get { return m_current;}}

			public void WriteOutClosedLexEntries(System.Xml.XmlTextWriter xmlOutput)
			{
				foreach (ImportObject child in m_root.Children)
				{
					if (child.Depth == 1 && child.Closed)	// right level and closed for 'business'
					{
#if TracingOutput
						System.Diagnostics.Debug.WriteLine(" *** Flushing " + child.AncestorLongNames());
#endif
						child.Flush(m_converter, xmlOutput);
						RemoveNodeAndChildren(child);
						break;	// only can have one entry open at a time
					}
				}
			}

			public void RemoveNodeAndChildren(ImportObject node)
			{
				// just remove this node from it's parent and it will be gone...
				if (node.Parent != null)
				{
					ImportObject parent = node.Parent;
					parent.RemoveChild(node);
				}
			}

			public void Flush(System.Xml.XmlTextWriter xmlOutput)
			{
				foreach (ImportObject child in m_root.Children)
				{
					child.Flush(m_converter, xmlOutput);
				}
				// m_root.Flush(m_converter, xmlOutput, xmlHelpOutput);
				xmlOutput.WriteEndElement();
			}

			private bool RemoveObjFromOpenList(ImportObject obj)
			{
				// remove this node from the hash of nodes
				if (m_OpenNodes.ContainsKey(obj.Name))
				{
					ArrayList objs = m_OpenNodes[obj.Name] as ArrayList;
					objs.Remove(obj);
					if (objs.Count == 0)	// empty now, remove from hash
					{
						m_OpenNodes.Remove(obj.Name);
					}
					return true;
				}
				return false;
			}

			private void CloseImportObject(ImportObject obj)
			{
				obj.MakeClosed();	// will also close all child obj's
				RemoveObjFromOpenList(obj);

				foreach(ImportObject child in obj.Children)
				{
//					RemoveObjFromOpenList(child);
					CloseImportObject(child);
				}
			}

			public bool AddNewEntry(ClsHierarchyEntry newHierarchy, out ImportObject addedNode)
			{
				// get the ancestors of this hierarchy
				// get the best ancestor of the currently open ones
				// if none found, look for the ancestors of the ancestors - repeat until a best ancestor is found
				//  once found, back down the list of ancestors and add them
				addedNode = null;
				string name = newHierarchy.Name;
				TreeNode leaf = new TreeNode(name);
				ArrayList nodes = new ArrayList();
				ArrayList nextLevel = new ArrayList();
				nodes.Add(leaf);
				ImportObject bestParent = null;
				TreeNode foundNode = null;
				bool done = false;
				while (!done)
				{
					foreach(TreeNode node in nodes)
					{
						ArrayList possibleParents = new ArrayList();
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
							TreeNode posparent = new TreeNode(ancestor);
							node.AddAncestor(posparent);
							nextLevel.Add(posparent);
						}
					}
					nodes = nextLevel;
				}

				if (bestParent != null)
				{
					// this is a level one node, or requires one to be created so bump the counter
					if (bestParent.Depth == 0)
						this.m_converter.m_NumElements++;

					while (foundNode != null)
					{
						ImportObject newEntry = new ImportObject(foundNode.Name, bestParent);
						addedNode = newEntry;
						bestParent.AddChild(newEntry);
						this.AddNewObject(newEntry);	// will set current and add to m_opennodes hash
//						System.Diagnostics.Debug.WriteLine("**** POINT TO LOOK FOR FLUSHING OUT THE ENTRY ***** c");

						// set up variables for next level in tree
						bestParent = newEntry;
						foundNode = foundNode.Leaf;
					}
					return true;
				}

				if (bestParent != null)
				{
					// the best parent is the starting point to walk to the leaf
					this.AddNewObject(bestParent);
//					System.Diagnostics.Debug.WriteLine("**** POINT TO LOOK FOR FLUSHING OUT THE ENTRY ***** b");

				}
				// create the best parent
				// then walk the 'foundnode.leaf' methods and add until it's null,
				// then add the pending sfm info
				return false;
#if false






				// find all the currently open parent objects for this new entry
				ArrayList possibleParents = new ArrayList();
				SearchHierarcyForParentsOf(m_root, newHierarchy.Name, ref possibleParents);
				ImportObject bestParent = GetBestOpenParent(possibleParents);
				if (bestParent != null)
				{
					// create the new 'entry'
					AddNewObject(new ImportObject(newHierarchy.Name, bestParent));
					return true;
				}
				return false;
//				eded = m_sfmToHierarchy[currentSfm] as ClsHierarchyEntry;
//				if (mgr.AddNewEntry(needed))
#endif
			}


			// Any class that has children (Entry, Sense, Subentry), when that class is
			//  created/started any open classes at the current level that are also
			//  children of the new class will be closed.
			//
			// Deterministic - Describes an algorithm in which the correct next step
			//  depends only on the current state
			protected void RemoveChildrenForNewImportObject(ImportObject parent, string importClassName, Hashtable childrenTable, ImportObject objNotToRemove)
			{
				// Get a list of children names of the new 'importClassName' and then
				// close any of the children nodes of the 'parent' if they are the same.
				if (childrenTable.ContainsKey(importClassName))
				{
					ArrayList childNames = childrenTable[importClassName] as ArrayList;
					// small performance enhancement by getting rid of the foreach and using a for loop
					int count = parent.ChildrenCount;	//  .ChildrenList.Count;
					for (int i = 0; i<count; i++)
					{
						ImportObject child = parent.ChildAt(i);
						if (child == objNotToRemove)
							continue;
						if (!child.Closed && childNames.Contains(child.Name))	// found child node that should be closed
						{
							CloseImportObject(child);	// don't be 'open' for anymore markers
						}
					}
				}
				//				parent.AddChild(importClassName);
			}

			private void SearchChildrenForMatches(ImportObject start, string sfm, ref ArrayList possibleObjects)
			{
				// small performance enhancement by getting rid of the foreach and using a for loop
				int count = start.ChildrenCount;	//  .ChildrenList.Count;
				for (int i = 0; i < count; i++)
				{
					ImportObject child = start.ChildAt(i);	// ChildrenList[i];
					if (child.Closed == false && (child.CanAddSFM(sfm, m_converter) || child.CanAddSFMasAutoField(sfm, m_converter)))
					{
						possibleObjects.Add(child);	// save it as a possible resting place for this sfm and data
					}
					SearchChildrenForMatches(child, sfm, ref possibleObjects);
				}
			}

			private void GetAncestorsOf(string childName, ref ArrayList possibleObjects)
			{
				if (m_converter.m_Hierarchy.ContainsKey(childName))
				{
					ClsHierarchyEntry currentLocation = m_converter.m_Hierarchy[childName] as ClsHierarchyEntry;
					foreach (string ancestor in currentLocation.Ancestors)
						possibleObjects.Add(ancestor);
				}
				else
					possibleObjects.Clear();
			}

			private void SearchHierarcyForParentsOf(ImportObject start, string childName, ref ArrayList possibleObjects)
			{
				if (m_converter.m_HierarchyChildren.ContainsKey(start.Name))
				{
					ArrayList children = m_converter.m_HierarchyChildren[start.Name] as ArrayList;
					if (children.Contains(childName))
						possibleObjects.Add(start.Name);
				}
				foreach (ImportObject child in start.Children)
				{
					SearchHierarcyForParentsOf(child, childName, ref possibleObjects);
				}
			}

			// walk the tree of open objects and get the lowest one (start at leaves and work up would be more efficent)
			//
			private ImportObject GetBestOpenParent(ArrayList possibleParents)
			{
				ImportObject rval = null;
				// use the open nodes hash to find all the open parents and pick the deepest one
				foreach (string name in possibleParents)
				{
					if (m_OpenNodes.ContainsKey(name))
					{
						ArrayList objs = m_OpenNodes[name] as ArrayList;
						foreach (ImportObject posParent in objs)
						{
							if (posParent.Closed)
								continue;	// can't pick a closed parent...
							if (rval == null || rval.Depth < posParent.Depth)
								rval = posParent;
						}
					}
				}

				return rval;
			}

			///	------------------------------------------------------------------
			/// Rules for knowing where to add new classes (Entry, Sense, ...)
			/// This set of rules is for adding a new entry (sfm was begin marker)
			/// -------------------------------------------------------------------
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
			///	------------------------------------------------------------------
			///	1 - if the current entry can take the marker, add it here
			///	2 - starting at the deepest level, look for an entry that can take the marker
			///		+ if an entry is found, add it here
			///		  else
			///			pretend it starts an entry and process as a begin marker
			///
			///	------------------------------------------------------------------



			public bool CanUseBeginMarkerOnExistingEntry(string entryName, string sfm, byte[] sfmData, int line, bool isUnique)
			{
				if (m_OpenNodes.ContainsKey(entryName))
				{
					ImportObject rval = null;
					ArrayList objs = m_OpenNodes[entryName] as ArrayList;
					foreach (ImportObject posParent in objs)
					{
						if (posParent.Closed)
							continue;		// just to make sure there aren't any closed items in the open list
						if (rval == null || rval.Depth < posParent.Depth)
							rval = posParent;
					}
					if (rval != null && rval.CanAddBeginSFM(sfm, m_converter))
					{
						rval.AddBeginSFM(sfm,sfmData, line, m_converter);
						m_current = rval;
#if TracingOutput
						System.Diagnostics.Debug.WriteLine("  Mgr::Current = " + m_current.AncestorLongNames());
#endif
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
			/// <param name="name"></param>
			/// <returns></returns>
			public bool AddNewEntry(string name, string sfm, byte[] sfmData, int line, bool isUnique, out ImportObject addedEntry)
			{
				addedEntry = null;
//				ClsHierarchyEntry entry = m_converter.m_Hierarchy[name] as ClsHierarchyEntry;

				// find all the currently open parent objects for this new entry
				ArrayList possibleParents = new ArrayList();
				SearchHierarcyForParentsOf(m_root, name, ref possibleParents);

				ImportObject bestParent = GetBestOpenParent(possibleParents);
				if (bestParent != null)
				{
					ImportObject newEntry = new ImportObject(name, bestParent);
					addedEntry = newEntry;
					bestParent.AddChild(newEntry);
					this.AddNewObject(newEntry);	// will set current and add to m_opennodes hash
					newEntry.AddPendingSfmData(sfm, sfmData, line, isUnique);
#if TracingOutput
					System.Diagnostics.Debug.WriteLine("Adding pending sfm to <" + newEntry.Name + "> : " + sfm);
#endif
					return true;
				}
				else	// no possible open parent, have to add atleast one entry
				{
				}
//				if (possibleParents.Count == 0)
//				{
//					ArrayList neededParents = new ArrayList();
//					//neededParents.Add(
//					// this is a case where one or more parents are needed first
//					// ex: found the begin marker for a 'picture' but there isn't
//					//     currently a 'sense' open.
//				}
#if false

				// First lets take the currentPath items and put them into a hash and add the depth
				// as the value item.
				Hashtable currentPathDepths = new Hashtable();
				int currentPathDepth = 0;

				// walk the tree building the hashtable, just from current up to root
				ImportObject node = this.Current;
				while (node != null)
				{
					currentPathDepths.Add(node.Name, currentPathDepth++);
					node = node.Parent;
				}

					// see if the destination is in the currentPath
					if (currentPathDepths.ContainsKey(name))
					{
						downPath.Push( new ClsPathObject(destination.KEY));
					}
					else
					{
						// check each ancestor of the destination item
						string currentBestAncestor = null;
						int currentBestDepth = Int32.MaxValue;
						foreach(string name in destination.Ancestors)
						{
							if (currentPathDepths.ContainsKey(name))
							{
								int depth = (int)(currentPathDepths[name]);
								if (depth < currentBestDepth)
								{
									currentBestAncestor = name;
									currentBestDepth = depth;
								}
							}
						}

						// see if we've found a common ancestor
						if (currentBestAncestor != null)
						{
							downPath.Push(new ClsPathObject(destination.KEY));
							downPath.Push(new ClsPathObject(currentBestAncestor));
						}
					}

					return downPath;
#endif
				return false;
			}

			public bool AddToOpenObjects(string sfm, byte[] sfmData, int line)
			{
				// first check to see if it can be added to the current element
				if (m_current.AddSFM(sfm, sfmData, line, m_converter))
					return true;

				bool retVal = false;	// not added


				// search the tree of open objects and get a list of valid objects to recieve this 'sfm'
				// once it is correct - it can be optimized, but first get it working...
				//				if (m_current.CanAddSFM(sfm, m_converter))
				//				{
				//					// add the sfm and data to the ImportObject here
				//					// m_current.a
				//					return false;
				//				}
				//				if (m_current.CanAddSFMasAutoField(sfm, m_converter))
				//				{
				//					// add the sfm and data to the ImportObject here
				//					// m_current.a
				//					return false;
				//				}

				// can't add to current, so starting from the root - check the open items
				ImportObject start = m_root;
				ArrayList possibleObjects = new ArrayList();
				SearchChildrenForMatches(start, sfm, ref possibleObjects);

				if (possibleObjects.Count == 0)
				{
					// no matches for this sfm - now what ... open items possibly already contain this sfm
					if (!Current.AddSFMasAutoField(sfm, sfmData, line, m_converter))
					{
//						System.Diagnostics.Debug.WriteLine("******** hmmm ERROR??? ****************");
//						System.Diagnostics.Debug.WriteLine("Can't add <"+sfm+"> to open items, and not an auto field.");
//						System.Diagnostics.Debug.WriteLine("Possibly a case where the sfm is already used and a new one needs to be started.?");
					}

				}
				else if (possibleObjects.Count > 1)
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
							else
							{
								retVal = true;
								break;
							}
						}
						else
						{
							retVal = true;
							break;
						}
					}
					if (!retVal)
						System.Diagnostics.Debug.WriteLine("******** ERROR??? ****************");
				}
				else
				{
					// only one place to add this one
					ImportObject found = possibleObjects[0] as ImportObject;
					if (!found.AddSFM(sfm, sfmData, line, m_converter))
					{
						if (!found.AddSFMasAutoField(sfm, sfmData, line, m_converter))
						{
							System.Diagnostics.Debug.WriteLine("******** ERROR??? ****************");
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

		}

		public class ImportObject : ClsPathObject
		{
			ImportObject m_parent;			// parent object
			List<ImportObject> m_children;	// ImportObject 's
			int m_childrenCount;			// test count value for performance
			bool m_closed;					// this object is not open for new sfm's
			int m_depth;					// how deep in the open objects is this item: 0=root..n=leaf
			string m_marker;				// marker that created this ImportObject
			int m_markerLine;				// line number of this marker
			byte[] m_markerData;			// marker data for the marker that created this ImportObject

			public ImportObject(string name, ImportObject parent)
				: base(name)
			{
				m_marker = "";
				m_markerData = null;
				m_markerLine = -1;
				m_parent = parent;
				m_children = new List<ImportObject>();
				m_childrenCount = 0;
				m_closed = false;
				if (parent != null)
					m_depth = parent.Depth+1;
				else
					m_depth = 0;	// case where there is no parent : root
			}

			public void AddCreationMarker(int line, string marker, byte[] markerData)
			{
				m_markerLine = line;
				m_marker = marker;
				m_markerData = markerData;
			}

			public void GetCreationMarkerData(out int line, out string marker, out byte[] markerdata)
			{
				line = m_markerLine;
				marker = m_marker;
				markerdata = m_markerData;
			}

//			private void AddChild(string name)
//			{
//				m_children.Add(new ImportObject(name, this));
//				m_childrenCount++;
//			}

			public void AddChild(ImportObject child)
			{
				m_children.Add(child);
				m_childrenCount++;
			}

			public bool RemoveChild(ImportObject child)
			{
				if (m_children.Contains(child))
				{
					m_children.Remove(child);
					m_childrenCount--;
					return true;
				}
				return false;
			}

			public ICollection Children
			{
				get { return m_children; }
			}

			public List<ImportObject> ChildrenList
			{
				get { return m_children; }
			}

			public ImportObject ChildAt(int index)
			{
				return m_children[index];
			}

			public int ChildrenCount { get { return m_childrenCount; /*.Count;*/ } }

			public string NameLong
			{
				get
				{
					string OorC = "O:";
					if (this.Closed)
						OorC = "C:";
					return Name + "<" + OorC + (this as Object).GetHashCode() +">";
				}
			}

			public string AncestorLongNames()
			{
				Stack parents = new Stack();
				ImportObject obj = this;
				while (obj.Parent != null)
				{
					parents.Push(obj.Parent);
					obj = obj.Parent;
				}
				string result = "";
				while (parents.Count > 0)
				{
					obj = parents.Pop() as ImportObject;
					result += obj.NameLong + ": ";
				}
				result += NameLong;
				return result;
			}

			public ImportObject Parent { get { return m_parent;}}
			public int Depth { get { return m_depth;}}
			public ClsPathObject Self { get { return this as ClsPathObject;}}
			public bool Closed
			{
				get { return m_closed;}
			}
			public void MakeClosed()
			{
				if (!Closed)
				{
					m_closed = true;
//					string pendingSFMs = this.GetPendingSFMs();
#if TracingOutput
					System.Diagnostics.Debug.WriteLine("  ** Closing item: " + AncestorLongNames());
#endif
				}
				foreach( ImportObject child in m_children)
					child.MakeClosed();
			}

			public void Flush(Converter convObj, System.Xml.XmlTextWriter xmlOutput)
			{
				// open the current node for output
				xmlOutput.WriteStartElement(Self.Name);
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("xmlElement Start: " + Self.Name);
#endif
				// output the pending sfm Data
				Self.FlushPendingSfmData(convObj, xmlOutput);

				// output children
				foreach( ImportObject child in m_children)
				{
					child.Flush(convObj, xmlOutput);
				}

				// close this current node
#if TracingOutput
				System.Diagnostics.Debug.WriteLine("xmlElement End: " + Self.Name);
#endif
				xmlOutput.WriteEndElement();

				m_closed = true;
			}

			public bool CanAddBeginSFM(string sfm, Converter converter)
			{
				if (Closed)
					return false;
				ClsHierarchyEntry currentLocation = converter.m_Hierarchy[this.Name] as ClsHierarchyEntry;
				bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
				bool appearsInBegin = currentLocation.BeginFieldsContains(sfm);
				bool usedAlready = this.ContainsSfm(sfm);
				bool noUniqueProblem = !uniqueSfm || !this.AlreadContainsUniqueSfm;
				bool canUseHere = appearsInBegin && !usedAlready && noUniqueProblem;
				return canUseHere;
			}

			public bool CanAddSFM(string sfm, Converter converter)
			{
				if (Closed)
					return false;

				// The current SFM does not start a new hierarchy level. See if we can use it at
				// the current location:
				ClsHierarchyEntry currentLocation = converter.m_Hierarchy[this.Name] as ClsHierarchyEntry;
				bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
				bool appearsInMulti = currentLocation.MultiFieldsContains(sfm);
				bool appearsInAdditional = currentLocation.AdditionalFieldsContains(sfm);
				bool usedAlready = this.ContainsSfm(sfm);
				bool noUniqueProblem = !uniqueSfm || !this.AlreadContainsUniqueSfm;
				bool canUseHere = appearsInMulti || (appearsInAdditional && !usedAlready && noUniqueProblem);
				return canUseHere;
			}

			public bool AddSFM(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddSFM(sfm, converter))
				{
					ClsHierarchyEntry currentLocation = converter.m_Hierarchy[this.Name] as ClsHierarchyEntry;
					bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
					this.AddSfm(sfm, uniqueSfm);
					this.AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
#if TracingOutput
					System.Diagnostics.Debug.WriteLine("Adding sfm<"+sfm+"> to: " + this.AncestorLongNames());
#endif
					return true;
				}
				return false;
			}

			public bool AddBeginSFM(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddBeginSFM(sfm, converter))
				{
					ClsHierarchyEntry currentLocation = converter.m_Hierarchy[this.Name] as ClsHierarchyEntry;
					bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);
					this.AddSfm(sfm, uniqueSfm);
					this.AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
#if TracingOutput
					System.Diagnostics.Debug.WriteLine("Adding begin sfm<"+sfm+"> to: " + this.AncestorLongNames());
#endif
					return true;
				}
				return false;
			}

			public bool CanAddSFMasAutoField(string sfm, Converter converter)
			{
				if (Closed)
					return false;

				// if it's an autofield, see if there's a match at this 'class' / entry
				ClsFieldDescription possibleAuto = converter.GetFieldDescription(sfm);
				if (possibleAuto != null && possibleAuto.IsAutoImportField)
				{
					// if there's a autofield for this class, we can use the sfm here
					if (converter.m_autoFieldsPossible.ContainsKey(this.Name))
						return true;
				}
				return false;
			}

			public bool AddSFMasAutoField(string sfm, byte[] sfmData, int line, Converter converter)
			{
				if (CanAddSFMasAutoField(sfm, converter))
				{
					ClsHierarchyEntry currentLocation = converter.m_Hierarchy[this.Name] as ClsHierarchyEntry;
					bool uniqueSfm = currentLocation.UniqueFieldsContains(sfm);

					this.AddSfm(sfm, uniqueSfm);
					this.AddPendingSfmData(sfm, sfmData, line, uniqueSfm);
#if TracingOutput
					System.Diagnostics.Debug.WriteLine("Adding sfm<"+sfm+"> to: " + AncestorLongNames());
#endif

					// add it to the list of autofields used
					string fwDest = converter.m_autoFieldsPossible[Name] as String;
					AutoFieldInfo afi = new AutoFieldInfo(Name, sfm, fwDest);
					if (!converter.m_autoFieldsUsed.ContainsKey(afi.KEY))
					{
						////								m_autoFieldsPossible[currentLocation.Name] as AutoFieldInfo;	//  AutoFieldInfo(currentLocation.Name, sfm, "ASDF");
						converter.m_autoFieldsUsed.Add(afi.KEY, afi);
						ArrayList sfms;
						if (converter.m_autoFieldsBySFM.ContainsKey(afi.sfmName))
						{
							sfms = converter.m_autoFieldsBySFM[afi.sfmName] as ArrayList;
						}
						else
						{
							sfms = new ArrayList();
							converter.m_autoFieldsBySFM.Add(afi.sfmName, sfms);
						}
						sfms.Add(afi);
					}
					return true;
				}
				return false;
			}

		}


		private void ProcessSfmFileNewLogic(System.Xml.XmlTextWriter xmlOutput)
		{
			// Add another xml output file that is similiar to phase one but includes
			// additional information that will be/is useful for helping diagnose problems
			// during the import process.
			long errCount=0;
			try
			{
				ByteReader reader = new ByteReader(m_SfmFileName, ref Log);
				byte[] sfmData;
				byte[] badMarkerBytes;
				string currentSfm = null;

				ImportObjectManager mgr = new ImportObjectManager(m_Root.Name, this);
				xmlOutput.WriteStartElement(m_Root.Name);

				while (reader.GetNextSfmMarkerAndData(out currentSfm, out sfmData, out badMarkerBytes))
				{
					m_SfmLineNumber = reader.FoundLineNumber;

					// If the currentSfm is empty, then this is a case where there is data before the marker
					if (currentSfm.Length == 0)
					{
						bool nonWSdata = false;	// non White Space data found
						// see if it's just white space
						for (int i=0; i < sfmData.Length && !nonWSdata; i++)
						{
							switch (sfmData[i])
							{
								case 0x0a:	// cr
								case 0x0d:	// lf
								case 0x20:	// space
								case 0x09:	// tab
									break;
								default:
									nonWSdata = true;
									break;
							}
						}

						if (nonWSdata == false)
							continue;

						string emsg = CharDataBeforeMarkerMessage(sfmData, m_SfmLineNumber);
						Log.AddError(m_SfmFileName, m_SfmLineNumber, emsg);
						// add an entry to the help xml file
						errCount++;
						continue;
					}

					// LT-1926 Ignore all markers that start with underscore (shoebox markers)
					if (currentSfm.StartsWith("_"))
						continue;

					// Make sure the marker is valid for the xml output file [?,<,&, and etc are invalid]
					// If this is a marker that has been taged to be ignored - then ignore it
					if (m_FieldsToIgnore.ContainsKey(currentSfm))
						continue;

					// Test that the currentSfm exists in the FieldDescriptions table:
					if (!m_FieldDescriptionsTable.ContainsKey(currentSfm))
					{
						// only log it once [lt-2217 and only if not in the list of ignored fields]
						if (!m_FieldDescriptionsTableNotFound.ContainsKey(currentSfm) &&
							!m_FieldsToIgnore.ContainsKey(currentSfm))
						{
							m_FieldDescriptionsTableNotFound.Add(currentSfm,null);
							Log.AddSFMError(m_SfmFileName, m_SfmLineNumber, currentSfm,
								String.Format(Sfm2XmlStrings.SFM1IsUndefinedOrInvalid, m_SfmLineNumber, currentSfm));
						}
						Log.AddSFMNotDefined(currentSfm);
						continue;
					}
					// log the case where bad bytes are in the marker itself, but continue processing
					if (badMarkerBytes != null && badMarkerBytes.Length > 0)
					{
						Log.AddSFMError(m_SfmFileName, m_SfmLineNumber, currentSfm,
							String.Format(Sfm2XmlStrings.Line0SFM1Count2InvalidBytes, m_SfmLineNumber, currentSfm, badMarkerBytes.Length));
					}

					ClsHierarchyEntry currentLocation = m_Hierarchy[mgr.Current.Name] as ClsHierarchyEntry;

					if (currentLocation == null)
						throw new System.Exception("Current path location is undefined.");

					// see if this sfm is labeled in the hierarchy as 'unique' - only one unique marker per entry
					bool uniqueSfm = currentLocation.UniqueFieldsContains(currentSfm);

					// See if the current SFM appears in the BeginFields list of any hierarchy entry:
					ClsHierarchyEntry newBeginHierarchyEntry = m_BeginMarkerHierarchyEntries[currentSfm] as ClsHierarchyEntry;
					if (newBeginHierarchyEntry == null)
					{
						// see if this is a marker that isn't used in the hierarchy
						if (m_MarkersNotInHierarchy.ContainsKey(currentSfm))
						{
							Log.AddSFMError(m_SfmFileName, m_SfmLineNumber, currentSfm,
								String.Format(Sfm2XmlStrings.SFM1IsNotUsedInAnyHierarchyItems, m_SfmLineNumber, currentSfm));
							continue;
						}

						// if we are able to add the current sfm to any open objects then do so and continue processing
						if (mgr.AddToOpenObjects(currentSfm, sfmData, m_SfmLineNumber))
							continue;

						if (m_sfmToHierarchy.ContainsKey(currentSfm))	// hierarchy entry that should contain this sfm
						{
							ClsHierarchyEntry needed = m_sfmToHierarchy[currentSfm] as ClsHierarchyEntry;
							if (needed.ContainsAncestor(m_Root.Name))	// dont' add a new entry or base level node
							{
								string errMsg = String.Format(Sfm2XmlStrings.SFM1AlreadyExistsIn2MarkerIsIgnored,
									m_SfmLineNumber, currentSfm, needed.Name);
								Log.AddError(m_SfmFileName, m_SfmLineNumber, errMsg);
								continue;
							}
							// for all other elements - > be hopefull ....
							// (hopefull that there will be the needed other items for this element to follow)
							ImportObject newElement;
							if (mgr.AddNewEntry(needed, out newElement))
							{
								// log the 'OutOfOrder' caution
								// go up the tree from this entry until we get the parent that equals the m_root.name and stop
								while (newElement.Parent.Name != m_Root.Name)
									newElement = newElement.Parent;

								string eleMarker;
								byte[] eleData;
								int eleLine;
								newElement.GetCreationMarkerData(out eleLine, out eleMarker, out eleData);
								string entryData = ProcessSFMData(eleMarker, eleData, eleLine);
								entryData = entryData.Replace(System.Environment.NewLine, " ");	// remove newlines
								entryData = entryData.Trim();	// remove whitespace
								entryData = eleLine.ToString() + ":" + entryData;
								Log.AddOutOfOrderCaution(entryData, currentSfm, m_SfmLineNumber);

								mgr.WriteOutClosedLexEntries(xmlOutput);
								if (mgr.AddToOpenObjects(currentSfm, sfmData, m_SfmLineNumber))
									continue;
							}
						}
						System.Diagnostics.Debug.WriteLine(" $$$$$$$$$$$$$ Possible logic error - CHECK!!! ++++++++++++++");
					}
					else
					{
						// can not assume it is the current location that this is a begin marker for now, first we have
						//  to get the open entry for this begin marker if one is present.
						uniqueSfm = newBeginHierarchyEntry.UniqueFieldsContains(currentSfm);
						if (mgr.CanUseBeginMarkerOnExistingEntry(newBeginHierarchyEntry.Name, currentSfm, sfmData, m_SfmLineNumber, uniqueSfm))
							continue;	// handled in existing entry

						// We will be opening a new hierarchy level.
						//// if (!mgr.AddNewEntry(newBeginHierarchyEntry.Name, currentSfm, sfmData, reader.LineNumber, uniqueSfm))
						///
						ImportObject newElement;
						if (mgr.AddNewEntry(newBeginHierarchyEntry, out newElement))
						{
							// Oflnly needed here as the 'Entry' is the only one that can't be 'guessed' into creation and its the one
							// that is included in the log.
							newElement.AddCreationMarker(m_SfmLineNumber, currentSfm, sfmData);
							mgr.Current.AddPendingSfmData(currentSfm, sfmData, m_SfmLineNumber, uniqueSfm);
							////// log the 'OutOfOrder' caution
							////// go up the tree from this entry until we get the parent that equals the m_root.name and stop
							////while (newElement.Parent.Name != m_Root.Name)
							////    newElement = newElement.Parent;

							////Log.AddOutOfOrderCaution(newElement.NameLong, currentSfm, m_SfmLineNumber);
#if TracingOutput
							System.Diagnostics.Debug.WriteLine("Adding sfm<"+currentSfm+"> to: " + mgr.Current.AncestorLongNames());
#endif
							continue;
						}
						if (mgr.AddNewEntry(newBeginHierarchyEntry.Name, currentSfm, sfmData, m_SfmLineNumber, uniqueSfm, out newElement))
						{
							//////// log the 'OutOfOrder' caution
							//////// go up the tree from this entry until we get the parent that equals the m_root.name and stop
							//////while (newElement.Parent.Name != m_Root.Name)
							//////    newElement = newElement.Parent;

							//////Log.AddOutOfOrderCaution(newElement.NameLong, currentSfm, m_SfmLineNumber);
							continue;
						}
						System.Diagnostics.Debug.WriteLine(" $$$$$$$$$$$$$ Possible logic error 2 - CHECK!!! ++++++++++++++");
					}
					// This is where we add the SFM to the XML output. It is common to all logic paths
					// which don't give errors.
					mgr.Current.AddPendingSfmData(currentSfm, sfmData, m_SfmLineNumber, uniqueSfm);
				}
				mgr.Flush(xmlOutput);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
			}
		}

		private static string CharDataBeforeMarkerMessage(byte[] sfmData, int nLineNumber)
		{
			string emsg;
			if (sfmData.Length > 50)
			{
				emsg = String.Format(Sfm2XmlStrings.X1CharactersFoundBeforeMarker,
					nLineNumber, sfmData.Length);
			}
			else
			{
				try
				{
					emsg = String.Format(Sfm2XmlStrings.Data1FoundBeforeMarker,
						nLineNumber, Converter.MultiToWide(sfmData, System.Text.Encoding.UTF8).Replace(System.Environment.NewLine, " ").Trim());
				}
				catch
				{
					emsg = String.Format(Sfm2XmlStrings.X1CharactersSomeInvalidFoundBeforeMarker,
						nLineNumber, sfmData.Length);
				}
			}
			return emsg;
		}
	}
}
