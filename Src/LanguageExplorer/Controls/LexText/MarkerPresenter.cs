// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.IO;
using System.Linq;
using System.Xml;
using Sfm2Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class manages the data needed for diplaying the marker information :"Content Mappings".
	/// It will read the different files and then store and use the underlying data.
	/// </summary>
	public class MarkerPresenter: Converter
	{
		private SfmFileReader m_DataInfo;	// this is a reader for the actual data file
		private Hashtable m_htUILangInfo;			// key=LanguageInfoUI.Key, data=LanguageInforUI

		private string m_mapFile;		// file to use for the mdf mapfile
		private string m_dataFile;		// this is the data file
		private ArrayList m_SortOrder;	// current ascending/descending flag (bool)
		private string m_rootDir;

		public static string AutoDescriptionText() { return LexTextControls.ksImportResidue_Auto;}
		public Hashtable ContentMappingItems { get; private set; }

		public ContentMapping ContentMappingItem(string sfmKEY)
		{
			return ContentMappingItems[sfmKEY] as ContentMapping;
		}

		public bool ReplaceContentMappingItem(ContentMapping contentMapping)
		{
			var sfmKey = contentMapping.Marker;
			if (ContentMappingItems.ContainsKey(sfmKey))
			{
				ContentMappingItems.Remove(sfmKey);
				ContentMappingItems.Add(sfmKey, contentMapping);
				return true;
			}
			return false;
		}

		public LexImportFields LexImportFields { get; private set; }

		public bool GetAndChangeColumnSortOrder(int col)
		{
			var rval = (bool)m_SortOrder[col];
			m_SortOrder[col] = !rval;
			return rval;
		}

		public MarkerPresenter(string rootDir, Hashtable uiLangInfo, string topAnalysisWS, string mapfile, string datafile, string fwFile, int numColumns)
		{
			m_rootDir = rootDir;
			m_topAnalysisWS = topAnalysisWS;
			m_mapFile = mapfile;
			m_dataFile = datafile;
			m_SortOrder = new ArrayList();
			for (var i = 0; i < numColumns; i++)
			{
				m_SortOrder.Add(true);
			}
			m_SortOrder[1] = false;	// handle first click in column one

			InitFromMapFile(m_mapFile);
			m_DataInfo = new SfmFileReader(m_dataFile);
			bool changed;
			UpdateLexFieldsWithCustomFields(LexImportWizard.Wizard().ReadCustomFieldsFromDB(out changed) as LexImportFields);

			// get a list of the languages that are defined/editied in the GUI
			m_htUILangInfo = uiLangInfo; //LexImportWizard.Wizard().GetUILanguages();
			// this will now fill the m_htMarkerData hashtable with contentmapping objects
			MergeData(false);

		}

		public void UpdateLexFieldsWithCustomFields(ILexImportFields customFields)
		{
			LexImportFields = customFields as LexImportFields;
			var sImportFields = Path.Combine(m_rootDir, "Language Explorer", "Import", "ImportFields.xml");
			LexImportFields.ReadLexImportFields(sImportFields);
		}

		public bool UpdateSfmDataChanged()
		{
			m_DataInfo = new SfmFileReader(m_dataFile);
			return MergeData(true);
		}

		private void GetLangInfoForAutoFields(out string langDesc, out string ws)
		{
			ws = langDesc = string.Empty;	// empty in worst case
			var firstOne = new DictionaryEntry(string.Empty, string.Empty);
			var notIgnore = new DictionaryEntry(string.Empty, string.Empty);
			var first = true;
			foreach(DictionaryEntry uiLang in m_htUILangInfo)
			{
				if ((uiLang.Value as LanguageInfoUI).ICUName == "en")	// looking for English
				{
					ws = "en";
					langDesc = uiLang.Key as string;
					return;
				}
				if (notIgnore.Key as string == "" && (uiLang.Value as LanguageInfoUI).ICUName != STATICS.Ignore)
				{
					notIgnore = uiLang;
				}
				else if (first)
				{
					first = false;	// only needed if notignore and en as first one
					firstOne = uiLang;
				}
			}
			if (notIgnore.Key as string != string.Empty)
			{
				ws = (notIgnore.Value as LanguageInfoUI).ICUName;
				langDesc = notIgnore.Key as string;
				return;
			}
			if (firstOne.Key as string != string.Empty)
			{
				ws = (firstOne.Value as LanguageInfoUI).ICUName;
				langDesc = firstOne.Key as string;
			}
		}

		public CFChanges GetCustomFieldChangeStatus(LexImportCustomField oldlexCustomField)
		{
			var containsKEY = LexImportFields.ContainsCustomField(oldlexCustomField.CustomKey);
			if (!containsKEY)
			{
				return CFChanges.DoesntExist;
			}
			return LexImportFields.GetCustomField(oldlexCustomField).CRC == oldlexCustomField.CRC ? CFChanges.NoChanges : CFChanges.ASD;
		}

		public bool IsValidCustomField(LexImportCustomField lexCustomField)
		{
			// first make sure if it's a custom field, that the field exists in the database
			return LexImportFields.ContainsCustomField(lexCustomField.CustomKey);
		}

		public bool IsValidCustomField(ClsFieldDescription mapField)
		{
			// get the field description info from the map file for this marker
			if (mapField is ClsCustomFieldDescription)
			{
				// first make sure if it's a custom field, that the field exists in the database
				if (LexImportFields.ContainsCustomField(((ClsCustomFieldDescription)mapField).CustomKey))
				{
					return true;
				}
			}
			return false;
		}

		private bool MergeData(bool mergeWithExistingDataInUI)
		{
			var result = false; // return true if the we're updating from a currently loaded file and it's different

			if (!mergeWithExistingDataInUI)
			{
				ContentMappingItems = new Hashtable();
			}
			else
			{
				var keysToDelete = new System.Collections.Generic.List<object>();
				// remove ones from memory that no longer exist in the data
				foreach (DictionaryEntry de in ContentMappingItems)
				{
					if (!m_DataInfo.ContainsSfm(de.Key as string))
					{
						keysToDelete.Add(de.Key);
					}
				}
				// now if we found any to delete - do it.
				foreach (var goner in keysToDelete)
				{
					ContentMappingItems.Remove(goner);
				}
				result = keysToDelete.Any();	// if we've deleted something than we know the UI data has to be different
			}

			foreach (string sfmKEY in m_DataInfo.SfmInfo)
			{
				// LT-1926 Ignore all markers that start with underscore (shoebox markers)
				if (sfmKEY.StartsWith("_"))
				{
					continue;
				}

				// if the marker contains invalid characters for the xml element, ignore it???
				var marker = sfmKEY;
				var count = m_DataInfo.GetSFMWithDataCount(sfmKEY);
				var order = m_DataInfo.GetSFMOrder(sfmKEY);
				var fwID = string.Empty;
				var isCustom = false;

				if (ContentMappingItems.ContainsKey(sfmKEY))
				{
					// just update the count and order as the other fields could have been editied in the UI
					var uiData = ContentMappingItems[sfmKEY] as ContentMapping;
					uiData.Count = count;
					uiData.Order = order;
				}
				else
				{
					if (mergeWithExistingDataInUI)
					{
						result = true;	// we are adding a new one, so the UI data will be different
					}

					// get the field description info from the map file for this marker
					var mapField = FieldMarkerHashTable[marker] as ClsFieldDescription;
					if (mapField is ClsCustomFieldDescription)
					{
						isCustom = true;
						// first make sure if it's a custom field, that the field exists in the database
						if (!IsValidCustomField(mapField))
						{
							mapField = null;	// treat like not existint (not in DB at this point)
						}
					}

					string desc;
					string className;
					string langDesc;
					string ws;
					string dest;
					if (mapField == null)
					{
						// case where the marker in the data isn't in the map file - Now an AutoImport field by default
						desc = string.Empty;	// leave empty now
						className = string.Empty;
						dest = string.Empty;
						// search through the langs and find the autolang to use
						GetLangInfoForAutoFields(out langDesc, out ws);
					}
					else
					{

						desc = mapField.Name;

						if (mapField.IsAutoImportField)
						{
							desc = string.Empty;
							dest = string.Empty;	// AutoDescriptionText();
							className = string.Empty;
						}
						else if (mapField.MeaningApp == "fw.sil.org")
						{
							// use the meaning id and get the lex import field with this name
							if (mapField.MeaningID.Length > 0)
							{
								fwID = mapField.MeaningID;

								if (mapField is ClsCustomFieldDescription)
								{
									var custom = (ClsCustomFieldDescription)mapField;
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
									if (!LexImportFields.GetDestinationForName(mapField.MeaningID, out className, out dest))
									{
										className = dest = ContentMapping.Unknown();
									}
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
						var mapWS = mapField.Language;
						if (m_htUILangInfo.ContainsKey(mapWS))
						{
							ws = (m_htUILangInfo[mapWS] as Sfm2Xml.LanguageInfoUI).FwName;
							langDesc = mapWS;
						}

					}

					var startMapFieldData = GetFieldDescription(marker);
					if (startMapFieldData is ClsCustomFieldDescription)
					{
						var licf = LexImportFields.GetCustomField((ClsCustomFieldDescription)startMapFieldData);
						if (licf == null)
						{
							startMapFieldData = null;
						}
						else
						{
							startMapFieldData = licf.ClsFieldDescriptionWith(startMapFieldData);
							licf.UIClass = (startMapFieldData as Sfm2Xml.ClsCustomFieldDescription).ClassNameUI;
						}
						isCustom = true;
					}

					var uiData = new ContentMapping(marker, desc, className, dest, ws, langDesc, count, order, startMapFieldData, isCustom);
					uiData.FwId = fwID;
					uiData.AddLexImportField(LexImportFields.GetField(className, fwID));
					ContentMappingItems.Add(marker, uiData);
				}
			}

			// Now for each HierarchyEntry, set the individual marker begin fields
			var htHierarchy = HierarchyHashTable;
			foreach(DictionaryEntry dictEentry in htHierarchy)
			{
				var hierarchy = dictEentry.Value as ClsHierarchyEntry;

				foreach (string beginSfm in hierarchy.BeginFields)
				{
					if (ContentMappingItems.ContainsKey(beginSfm))
					{
						var uiData = ContentMappingItems[beginSfm] as ContentMapping;
						uiData.IsBeginMarker = true;
					}
				}
			}
			return result;
		}

		public ContentMapping DefaultContent(string sfmKEY)
		{
			string ws, langDesc;
			var marker = sfmKEY;
			var desc = string.Empty;
			// search through the langs and find the autolang to use
			GetLangInfoForAutoFields(out langDesc, out ws);
			var count = m_DataInfo.GetSFMWithDataCount(sfmKEY);
			var order = m_DataInfo.GetSFMOrder(sfmKEY);
			return new ContentMapping(marker, desc, string.Empty, string.Empty, ws, langDesc, count, order, null, false);
		}

		/// <summary>
		/// See if the passed in file is a valid XML mapping file.
		/// </summary>
		public static bool IsValidMapFile(string mapFile)
		{
			var success = false;
			var xmlMap = new XmlDocument();
			try
			{
				while (true)
				{
					xmlMap.Load(mapFile);

					// make sure it has a root node of sfmMapping
					var node = xmlMap.SelectSingleNode("sfmMapping");
					if (node == null)
					{
						break;	// not found
					}

					// look for a languages child
					node = xmlMap.SelectSingleNode("sfmMapping/languages");
					if (node == null)
					{
						break;	// not found
					}

					// look for a hierarchy child
					node = xmlMap.SelectSingleNode("sfmMapping/hierarchy");
					if (node == null)
					{
						break;	// not found
					}

					// look for a fieldDescriptions child
					node = xmlMap.SelectSingleNode("sfmMapping/fieldDescriptions");
					if (node == null)
					{
						break;	// not found
					}

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
		private bool InitFromMapFile(string mapFile)
		{
			bool success;
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				ReadHierarchy(xmlMap);
				ReadSettings(xmlMap);
				ReadFieldDescriptions(xmlMap);
				ReadCustomFieldDescriptions(xmlMap);

				success = true;
			}
			catch
			{
				success = false;
			}
			return success;
		}
	}
}