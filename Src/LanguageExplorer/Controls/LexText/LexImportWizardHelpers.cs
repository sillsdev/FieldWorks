// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		public MarkerPresenter(/*LcmCache cache, */ string rootDir, Hashtable uiLangInfo, string topAnalysisWS,
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

		public enum CFChanges { NoChanges, DoesntExist, DifferentGUID, ASD };
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

		/// <summary>
		/// This class is the IComparer derived class for sorting the Marker
		/// list view.  It uses the columns and sort order
		/// </summary>
		public class ListViewItemComparer : IComparer
		{
			private int col;
			private bool ascendingOrder;
			public ListViewItemComparer(int column, bool order)
			{
				col = column;
				ascendingOrder = order;
			}
			public int Compare(object x, object y)
			{
				ContentMapping a,b;
				a = ((ListViewItem)x).Tag as ContentMapping;
				b = ((ListViewItem)y).Tag as ContentMapping;

				if (col == 1)	// source order case
				{
					if (ascendingOrder)
						return b.Order - a.Order;
					return a.Order - b.Order;
				}
				else if (col == 2)	// count case
				{
					if (ascendingOrder)
						return b.Count - a.Count;
					return a.Count - b.Count;
				}

				string aText="";
				string bText="";

				switch (col)
				{
					case 0:
						aText = a.Marker;
						bText = b.Marker;
						break;
					case 3:
						aText = a.Description + "__" + a.Marker;
						bText = b.Description + "__" + b.Marker;
						break;
					case 4:
						aText = a.DestinationField + "__" + a.Marker;
						bText = b.DestinationField + "__" + b.Marker;
						break;
					case 5:
						aText = a.WritingSystem + "__" + a.Marker;
						bText = b.WritingSystem + "__" + b.Marker;
						break;
					default:
						break;
				}

				if (ascendingOrder)
					return String.Compare(aText, bText);
				return String.Compare(bText,aText);
			}
		}

		/// <summary>
		/// This class contains the object that is used for containing the data that is
		/// used in the content mapping display of the data.  It lives in the MarkerPresenter
		/// class as a public subclass.
		/// </summary>
		public class ContentMapping
		{
			private string m_marker;			// marker
			private string m_description;		// description
			private string m_destinationClass;	// FW destination class (Sense, Entry, ...)
			private string m_FWDestination;		// FW destination field (Date Created, ...)
			private string m_writingSystem;		// writing system
			private string m_langDescriptor;	// the display language descriptor for the UI
			private int m_srcOrder;				// the relative order of the sfm in the src file
			private int m_count;				// the number of times this sfm is in the src file

			private Sfm2Xml.ClsFieldDescription m_clsFieldDescription;
			private Sfm2Xml.LexImportField m_LexImportField;
			private bool m_isBeginMarker;		// true if this is a begin marker for the FW Dest class


			public static string Ignore() { return LexTextControls.ksIgnore;}
			public static string Unknown() { return LexTextControls.ksUnknown;}

			public string Marker { get { return m_marker; }}
			public string Description
			{
				get { return m_description; }
				set { m_description = value;}
			}
			public string DestinationClass
			{
				get { return m_destinationClass; }
				set { m_destinationClass = value; }
			}
			public string rawDestinationField
			{
				get { return m_FWDestination; }
				set { m_FWDestination = value; }
			}
			public string LanguageDescriptorRaw
			{
				get { return m_langDescriptor; }
			}
			public string LanguageDescriptor
			{
				get
				{
					if (IsLangIgnore)
						return String.Format(LexTextControls.ksX_Ignored, m_langDescriptor);
					return m_langDescriptor;
				}
				set { m_langDescriptor = value;}
			}
			public string WritingSystem => m_writingSystem;

			public bool IsBeginMarker
			{
				get { return m_isBeginMarker;}
				set { m_isBeginMarker = value; }
			}
			public void UpdateLangaugeValues(string writingsystemName, string shortName, string langDescriptor)
			{
				m_writingSystem = writingsystemName;
				m_langDescriptor = langDescriptor;
				m_clsFieldDescription.UpdateLanguageValues(m_langDescriptor, shortName);
			}


			public int Count { get { return m_count; } set { m_count = value; } }
			public int Order { get { return m_srcOrder; } set { m_srcOrder = value; } }

			public void AddLexImportCustomField(Sfm2Xml.ILexImportField field, string uiClass)
			{
				AddLexImportField(field);
				(m_LexImportField as Sfm2Xml.ILexImportCustomField).UIClass = uiClass;
				Sfm2Xml.ClsFieldDescription xyz = LexImportField.ClsFieldDescriptionWith(ClsFieldDescription);
				m_clsFieldDescription = xyz;
			}

			public void AddLexImportField(Sfm2Xml.ILexImportField field)
			{
				m_LexImportField = field as Sfm2Xml.LexImportField;
				m_clsFieldDescription.Type = field != null ? field.DataType : "string";
			}

			public Sfm2Xml.ILexImportField LexImportField
			{
				get {return m_LexImportField;}
			}

			public bool Exclude
			{
				get { return m_clsFieldDescription.IsExcluded; }
				set { m_clsFieldDescription.IsExcluded= value; }
			}

			public bool AutoImport
			{
				get { return m_clsFieldDescription.IsAutoImportField; }
				set { m_clsFieldDescription.IsAutoImportField = value; }
			}

			public void ClearRef()
			{
				m_clsFieldDescription.ClearRef();
			}
			public bool IsRefField => m_clsFieldDescription.IsRef;

			public string RefField
			{
				get { return m_clsFieldDescription.RefFunc; }
				set { m_clsFieldDescription.RefFunc = value; }
			}

			public string RefFieldWS
			{
				get { return m_clsFieldDescription.RefFuncWS; }
				set { m_clsFieldDescription.RefFuncWS = value; }
			}

			public string FwId
			{
				get { return m_clsFieldDescription.MeaningID; }
				set { m_clsFieldDescription.MeaningID = value;}
			}

			public bool IsAbbrvField
			{
				get
				{
					bool defaultValue = false;
					if (m_LexImportField == null)
						return defaultValue;
					return m_LexImportField.IsAbbrField;
				}
			}

			public bool IsAbbr
			{
				get
				{
					bool defaultValue = false;
					if (m_LexImportField == null)
						return defaultValue;
					return m_clsFieldDescription.IsAbbr;
				}
			}

			public void UpdateAbbrValue(bool isAbbr)
			{
				if (!IsAbbrvField)
					return;

				m_clsFieldDescription.IsAbbr = isAbbr;
			}

			public string[] ListViewStrings()
			{
				string customTag = "";
				if (this.LexImportField is Sfm2Xml.LexImportCustomField)
				{
					customTag = " (Custom "+ (this.LexImportField as Sfm2Xml.LexImportCustomField).UIClass + ")";
				}
				else if (this.IsRefField)
				{
					customTag = " (" + this.RefField + ")";
				}
				return new string[] {"\\"+Marker,						// col 1
										System.Convert.ToString(Order), // col 2
										System.Convert.ToString(Count),	// col 3
										Description,					// col 4
										DestinationField + customTag,	// col 5
										LanguageDescriptor };			// col 6
			}

			public bool IsLangIgnore
			{
				get
				{
					if (m_writingSystem == Ignore())
						return true;
					return false;
				}
			}

			public string DestinationField
			{
				get
				{
//					// ws level exclusion
//					if (m_writingSystem == Ignore())
//						return Ignore();
					// field level exclusion
					if (Exclude)
						return LexTextControls.ksDoNotImport;	// "Not imported";
					if (AutoImport)
						return MarkerPresenter.AutoDescriptionText();
					// fw destination field
					return m_FWDestination;
				}
			}

			public ContentMapping(string marker, string desc, string className, string fwDest,
				string ws, string langDescriptor, int count, int order, Sfm2Xml.ClsFieldDescription fdesc, bool isCustom)
			{
				m_marker = marker;
				m_description = desc;
				m_FWDestination = fwDest;
				m_writingSystem = ws;
				m_langDescriptor = langDescriptor;
				m_count = count;
				m_srcOrder = order;
				//m_excluded = false;
				m_destinationClass = className;
				m_clsFieldDescription = fdesc;	// saved for now, used at end for producing map file
				m_LexImportField = null;
				if (m_clsFieldDescription == null)
				{
					if (!isCustom)
						m_clsFieldDescription = ClsFieldDescription;
					else
					{
						int shouldNotBeHere = 0;
						shouldNotBeHere++;
						m_clsFieldDescription = new Sfm2Xml.ClsCustomFieldDescription("", "", /*System.Guid.NewGuid(),*/ 0, false, 0,
							m_marker, " ", "string", this.m_langDescriptor, this.IsAbbrvField, this.m_FWDestination);
					}
				}
				m_isBeginMarker = false;
			}

			public Sfm2Xml.ClsFieldDescription ClsFieldDescription
			{
				get
				{
					if (m_clsFieldDescription != null)
						return m_clsFieldDescription;

					string dataType = "string";
					if (m_LexImportField != null)
						dataType = m_LexImportField.DataType;

					Sfm2Xml.ClsFieldDescriptionWrapper wrapper = new Sfm2Xml.ClsFieldDescriptionWrapper(m_marker,
						" ", dataType, this.m_langDescriptor, this.IsAbbrvField, this.m_FWDestination);
					return wrapper as Sfm2Xml.ClsFieldDescription;
				}
			}

			public void DoAutoImportWork()
			{
				//m_clsFieldDescription = null;					// remove the custom version of this class
				//m_clsFieldDescription = ClsFieldDescription;	// create a simple version for the auto import field
				m_LexImportField = null;
			}

		}
	}

	/// <summary>
	/// Quick and dirty class for reading the InFieldMarkers section of the 'map' file.
	/// </summary>
	class IFMReader : Converter
	{
		public Hashtable IFMS(string mapFile, Hashtable languages)
		{
			var xmlMap = new XmlDocument();
			try
			{
				// pull out the clsLanguage objects and put in local hash for them
				foreach(DictionaryEntry entry in languages)
				{
					var lang = entry.Value as LanguageInfoUI;
					m_Languages.Add(lang.Key, lang.ClsLanguage);
				}
				xmlMap.Load(mapFile);
				ReadInFieldMarkers(xmlMap);
				return InFieldMarkerHashTable;
			}
			catch
			{
			}
			return null;
		}
	}

	/// <summary>
	/// Quick and dirty class for reading the Options section of the 'map' file.
	/// </summary>
	class OptionConverter : Converter
	{
		public Dictionary<string, bool> Options(string mapFile)
		{
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				ReadOptions(xmlMap);
				return base.GetOptions;
			}
			catch
			{
				var xmlFile = mapFile.Split('\\');
				MessageBox.Show(String.Format("Xml file {0} is invalid.", xmlFile[xmlFile.Length - 1]));
			}
			return null;
		}
	}

	/// <summary>
	/// Quick and dirty class for reading the Languages section of the 'map' file.
	/// </summary>
	class LangConverter : Converter
	{
		public Hashtable Languages(string mapFile)
		{
			var xmlMap = new XmlDocument();
			try
			{
				xmlMap.Load(mapFile);
				ReadLanguages(xmlMap);
				return base.GetLanguages;
			}
			catch
			{
			}
			return null;
		}
	}
}
