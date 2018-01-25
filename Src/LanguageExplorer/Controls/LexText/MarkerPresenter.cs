// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using Sfm2Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class manages the data needed for diplaying the marker information :"Content Mappings".
	/// It will read the different files and then store and use the underlying data.
	/// </summary>
	public class MarkerPresenter: Converter
	{
		private Hashtable m_htMarkerData;			// key=marker, data=ContentMapping object
		private Sfm2Xml.SfmFileReader m_DataInfo;	// this is a reader for the actual data file
		private Sfm2Xml.LexImportFields m_LexFields;		// this is the manager of the FW Import Fields
		private Hashtable m_htUILangInfo;			// key=LanguageInfoUI.Key, data=LanguageInforUI

		private string m_mapFile;		// file to use for the mdf mapfile
		private string m_dataFile;		// this is the data file
		private ArrayList m_SortOrder;	// current ascending/descending flag (bool)
		private string m_rootDir;

		public static string AutoDescriptionText() { return LexTextControls.ksImportResidue_Auto;}
		public Hashtable ContentMappingItems { get { return m_htMarkerData; }}
		public ContentMapping ContentMappingItem(string sfmKEY)
		{
			return m_htMarkerData[sfmKEY] as ContentMapping;
		}

		public bool ReplaceContentMappingItem(ContentMapping contentMapping)
		{
			string sfmKey = contentMapping.Marker;
			if (m_htMarkerData.ContainsKey(sfmKey))
			{
				m_htMarkerData.Remove(sfmKey);
				m_htMarkerData.Add(sfmKey, contentMapping);
				return true;
			}
			return false;
		}

		public Sfm2Xml.LexImportFields LexImportFields { get { return m_LexFields; } }

		public bool GetAndChangeColumnSortOrder(int col)
		{
			bool rval = (bool)m_SortOrder[col];
			m_SortOrder[col] = !rval;
			return rval;
		}

		public MarkerPresenter(string rootDir, Hashtable uiLangInfo, string topAnalysisWS,
			string mapfile, string datafile, string fwFile, int numColumns)
		{
			m_rootDir = rootDir;
			m_topAnalysisWS = topAnalysisWS;
			m_mapFile = mapfile;
			m_dataFile = datafile;
			m_SortOrder = new ArrayList();
			for (int i = 0; i < numColumns; i++)
			{
				m_SortOrder.Add(true);
			}
			m_SortOrder[1] = false;	// handle first click in column one

			InitFromMapFile(m_mapFile);
			m_DataInfo = new Sfm2Xml.SfmFileReader(m_dataFile);
			bool changed;
			UpdateLexFieldsWithCustomFields(LexImportWizard.Wizard().ReadCustomFieldsFromDB(out changed) as Sfm2Xml.LexImportFields);

			// get a list of the languages that are defined/editied in the GUI
			m_htUILangInfo = uiLangInfo; //LexImportWizard.Wizard().GetUILanguages();
			// this will now fill the m_htMarkerData hashtable with contentmapping objects
			MergeData(false);

		}

		public void UpdateLexFieldsWithCustomFields(Sfm2Xml.ILexImportFields customFields)
		{
			m_LexFields = customFields as Sfm2Xml.LexImportFields;
			string sRootDir = m_rootDir;	// SIL.Utils.FwDirectoryFinder.CodeDirectory;
			if (!sRootDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
				sRootDir += Path.DirectorySeparatorChar;
			string sImportFields = sRootDir + String.Format("Language Explorer{0}Import{0}ImportFields.xml", Path.DirectorySeparatorChar);
			m_LexFields.ReadLexImportFields(sImportFields);
		}

		public bool UpdateSfmDataChanged()
		{
			m_DataInfo = new Sfm2Xml.SfmFileReader(m_dataFile);
			return MergeData(true);
		}

		private void GetLangInfoForAutoFields(out string langDesc, out string ws)
		{
			ws = langDesc = "";	// empty in worst case
			DictionaryEntry firstOne = new DictionaryEntry("","");
			DictionaryEntry notIgnore = new DictionaryEntry("","");
			bool first = true;
			foreach(DictionaryEntry uiLang in m_htUILangInfo)
			{
				if ((uiLang.Value as Sfm2Xml.LanguageInfoUI).ICUName == "en")	// looking for English
				{
					ws = "en";
					langDesc = uiLang.Key as string;
					return;
				}
				else if (notIgnore.Key as string == "" &&
					(uiLang.Value as Sfm2Xml.LanguageInfoUI).ICUName != Sfm2Xml.STATICS.Ignore)
					notIgnore = uiLang;
				else if (first)
				{
					first = false;	// only needed if notignore and en as first one
					firstOne = uiLang;
				}
			}
			if (notIgnore.Key as string != "")
			{
				ws = (notIgnore.Value as Sfm2Xml.LanguageInfoUI).ICUName;
				langDesc = notIgnore.Key as string;
				return;
			}
			if (firstOne.Key as string != "")
			{
				ws = (firstOne.Value as Sfm2Xml.LanguageInfoUI).ICUName;
				langDesc = firstOne.Key as string;
				return;
			}
		}

		public CFChanges TESTTESTTEST(Sfm2Xml.LexImportCustomField oldlexCustomField)
		{
			bool containsKEY = m_LexFields.ContainsCustomField(oldlexCustomField.CustomKey);
			if (!containsKEY)
			{
				return CFChanges.DoesntExist;
			}
			else
			{
				Sfm2Xml.ILexImportCustomField curICF = m_LexFields.GetCustomField(oldlexCustomField);
				if (curICF.CRC == oldlexCustomField.CRC)
				{
					return CFChanges.NoChanges;
				}
				else
				{
					return CFChanges.ASD;
				}
			}
		}

		public bool IsValidCustomField(Sfm2Xml.LexImportCustomField lexCustomField)
		{
			// first make sure if it's a custom field, that the field exists in the database
			if (m_LexFields.ContainsCustomField(lexCustomField.CustomKey))
				return true;
			return false;
		}

		public bool IsValidCustomField(Sfm2Xml.ClsFieldDescription mapField)
		{
			// get the field description info from the map file for this marker
			if (mapField is Sfm2Xml.ClsCustomFieldDescription)
			{
				// first make sure if it's a custom field, that the field exists in the database
				if (m_LexFields.ContainsCustomField((mapField as Sfm2Xml.ClsCustomFieldDescription).CustomKey))
					return true;
			}
			return false;
		}

		private bool MergeData(bool mergeWithExistingDataInUI)
		{
			string marker, desc, className="", dest="", ws, langDesc, fwID;
			int count, order;
			bool result = false; // return true if the we're updating from a currently loaded file and it's different
			bool isCustom = false;

			if (!mergeWithExistingDataInUI)
				m_htMarkerData = new Hashtable();
			else
			{
				System.Collections.Generic.List<object> keysToDelete = new System.Collections.Generic.List<object>();
				// remove ones from memory that no longer exist in the data
				foreach (DictionaryEntry de in m_htMarkerData)
				{
					if (!m_DataInfo.ContainsSfm(de.Key as string))
						keysToDelete.Add(de.Key);
				}
				// now if we found any to delete - do it.
				for (int index = 0; index < keysToDelete.Count; index++)
				{
					m_htMarkerData.Remove(keysToDelete[index]);
				}
				result = keysToDelete.Count > 0;	// if we've deleted something than we know the UI data has to be different
			}

			foreach( string sfmKEY in m_DataInfo.SfmInfo)
			{
				// LT-1926 Ignore all markers that start with underscore (shoebox markers)
				if (sfmKEY.StartsWith("_"))
					continue;

				// if the marker contains invalid characters for the xml element, ignore it???
				marker = sfmKEY;
				count = m_DataInfo.GetSFMWithDataCount(sfmKEY);
				order = m_DataInfo.GetSFMOrder(sfmKEY);
				fwID = "";
				isCustom = false;

				if (m_htMarkerData.ContainsKey(sfmKEY))
				{
					// just update the count and order as the other fields could have been editied in the UI
					ContentMapping uiData = m_htMarkerData[sfmKEY] as ContentMapping;
					uiData.Count = count;
					uiData.Order = order;
				}
				else
				{
					if (mergeWithExistingDataInUI)
						result = true;	// we are adding a new one, so the UI data will be different

					// get the field description info from the map file for this marker
					Sfm2Xml.ClsFieldDescription mapField = this.FieldMarkerHashTable[marker] as Sfm2Xml.ClsFieldDescription;
					if (mapField is Sfm2Xml.ClsCustomFieldDescription)
					{
						isCustom = true;
						// first make sure if it's a custom field, that the field exists in the database
						if (!IsValidCustomField(mapField))
							mapField = null;	// treat like not existint (not in DB at this point)
					}

					if (mapField == null)
					{
						// case where the marker in the data isn't in the map file - Now an AutoImport field by default
						desc = className = dest = ws = langDesc = ContentMapping.Unknown();
						desc = "";	// leave empty now
						//					desc = AutoDescriptionText();
						className = "";
						dest = "";	// AutoDescriptionText();
						// search through the langs and find the autolang to use
						GetLangInfoForAutoFields(out langDesc, out ws);
					}
					else
					{

						desc = mapField.Name;

						if (mapField.IsAutoImportField)
						{
							//						desc = AutoDescriptionText();
							//						className = dest = "";
							desc = "";
							dest = "";	// AutoDescriptionText();
							className = "";
						}
						else if (mapField.MeaningApp == "fw.sil.org")
						{
							// use the meaning id and get the lex import field with this name
							if (mapField.MeaningID.Length > 0)
							{
								fwID = mapField.MeaningID;

								if (mapField is Sfm2Xml.ClsCustomFieldDescription)
								{
									Sfm2Xml.ClsCustomFieldDescription custom = mapField as Sfm2Xml.ClsCustomFieldDescription;
									className = custom.ClassNameUI;
									dest = custom.Name;// +" (Custom " + className + ")";	// MeaningID;	//  custom.Name;
									if (fwID != custom.Name && fwID.StartsWith("custom"))
									{
										// must have an old 6.0 mapping file - see FWR-1707.
										mapField.MeaningID = custom.Name;
										fwID = custom.Name;
									}
								}
								else
								{
									// dest = m_LexFields.GetUIDestForName(mapField.MeaningID);
									if (!m_LexFields.GetDestinationForName(mapField.MeaningID, out className, out dest))
										className = dest = ContentMapping.Unknown();
								}
							}
							else
							{
								className = ContentMapping.Unknown();
								dest = ContentMapping.Unknown(); // "Unknown " + mapField.MeaningApp + " ID<" + mapField.MeaningID + ">";
							}
						}
						else
						{
							className = dest = ContentMapping.Unknown();
						}

						// now get the writing system:
						// - default to "Unknown" if not found
						// - see if there is a map value
						// - see if the map value key's into the UI languages
						ws = langDesc = ContentMapping.Unknown();
						string mapWS = mapField.Language;
						if (m_htUILangInfo.ContainsKey(mapWS))
						{
							ws = (m_htUILangInfo[mapWS] as Sfm2Xml.LanguageInfoUI).FwName;
							langDesc = mapWS;
						}

					}

					Sfm2Xml.ClsFieldDescription startMapFieldData = this.GetFieldDescription(marker);
					if (startMapFieldData is Sfm2Xml.ClsCustomFieldDescription)
					{
						Sfm2Xml.ILexImportCustomField licf = m_LexFields.GetCustomField(startMapFieldData as Sfm2Xml.ClsCustomFieldDescription);
						if (licf == null)
							startMapFieldData = null;
						else
						{
							startMapFieldData = licf.ClsFieldDescriptionWith(startMapFieldData);
							licf.UIClass = (startMapFieldData as Sfm2Xml.ClsCustomFieldDescription).ClassNameUI;
						}
						isCustom = true;
					}

					ContentMapping uiData = new ContentMapping(marker, desc, className, dest, ws, langDesc, count, order, startMapFieldData, isCustom);
					uiData.FwId = fwID;
					uiData.AddLexImportField(m_LexFields.GetField(className, fwID));
					m_htMarkerData.Add(marker, uiData);
				}
			}

			// Now for each HierarchyEntry, set the individual marker begin fields
			Hashtable htHierarchy = HierarchyHashTable;
			foreach(DictionaryEntry dictEentry in htHierarchy)
			{
				Sfm2Xml.ClsHierarchyEntry hierarchy = dictEentry.Value as Sfm2Xml.ClsHierarchyEntry;

				foreach (string beginSfm in hierarchy.BeginFields)
				{
					if (m_htMarkerData.ContainsKey(beginSfm))
					{
						ContentMapping uiData = m_htMarkerData[beginSfm] as ContentMapping;
						uiData.IsBeginMarker = true;
					}
				}
			}

			return result;
		}

		public ContentMapping DefaultContent(string sfmKEY)
		{
			string marker, desc, className="", dest="", ws, langDesc;
			int count, order;
			marker = sfmKEY;
			desc = className = dest = ws = langDesc = ContentMapping.Unknown();
			desc = "";	// leave empty now
			className = "";
			dest = "";	// AutoDescriptionText();
			// search through the langs and find the autolang to use
			GetLangInfoForAutoFields(out langDesc, out ws);

//			Sfm2Xml.ClsFieldDescription startMapFieldData = this.GetFieldDescription(sfmKEY);
			count = m_DataInfo.GetSFMWithDataCount(sfmKEY);
			order = m_DataInfo.GetSFMOrder(sfmKEY);

			ContentMapping uiData = new ContentMapping(marker, desc, className, dest, ws, langDesc, count, order, null, false);
			return uiData;
		}

		/// <summary>
		/// See if the passed in file is a valid XML mapping file.
		/// </summary>
		/// <param name="mapFile">file name to check</param>
		/// <returns>true if valid</returns>
		public static bool IsValidMapFile(string mapFile)
		{
			bool success = false;
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				while (true)
				{
					xmlMap.Load(mapFile);

					// make sure it has a root node of sfmMapping
					System.Xml.XmlNode node = xmlMap.SelectSingleNode("sfmMapping");
					if (node == null)
						break;	// not found

					// look for a languages child
					node = xmlMap.SelectSingleNode("sfmMapping/languages");
					if (node == null)
						break;	// not found

					// look for a hierarchy child
					node = xmlMap.SelectSingleNode("sfmMapping/hierarchy");
					if (node == null)
						break;	// not found

					// look for a fieldDescriptions child
					node = xmlMap.SelectSingleNode("sfmMapping/fieldDescriptions");
					if (node == null)
						break;	// not found

					success = true;
					break;
				}
			}
			catch// (System.Xml.XmlException e)
			{
				success = false;
			}
			return success;
		}

		/// <summary>
		/// This method will read the mapfile pulling out the sections that it's interested in.
		/// It currently reads in the Settings and FieldDescriptions sections.  The language section
		/// is handeled elsewhere and infield markers aren't currently used.
		/// </summary>
		/// <param name="mapFile"></param>
		/// <returns></returns>
		private bool InitFromMapFile(string mapFile)
		{
			bool success = false;
			System.Xml.XmlDocument xmlMap = new System.Xml.XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				//ReadLanguages(xmlMap);
				ReadHierarchy(xmlMap);
				ReadSettings(xmlMap);
				ReadFieldDescriptions(xmlMap);
				ReadCustomFieldDescriptions(xmlMap);

				success = true;
			}
			catch// (System.Xml.XmlException e)
			{
				success = false;
			}
			return success;
		}
	}
}