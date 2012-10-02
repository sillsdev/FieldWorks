using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;	// XmlNode

using System.Windows.Forms;

namespace Sfm2Xml
{
	/// <summary>
	/// This class exists as a common place for putting static methods for the Sfm2Xml namespace.
	/// </summary>
	public class STATICS
	{
		static public string MapFileVersion { get { return "6.0"; } }	// this maps to FW release 6.0, prev release was 5.4.1

		/// <summary>
		/// Determine if the passed in string represents a 'true' of 'false' string
		/// and return it.  If it's not determined, then return the default value.
		/// </summary>
		/// <param name="text">string to examine for t or f</param>
		/// <param name="defaultValueIfUnknown">true or false</param>
		/// <returns></returns>
		static public bool IsBoolString(string text, bool defaultValueIfUnknown)
		{
			if (text == null)
				return defaultValueIfUnknown;

			string bstring = text.ToLowerInvariant();
			if (bstring.Length > 0)
			{
				if ((bstring[0] == 't' || bstring[0] == '1'))
					return true;

				if ((bstring[0] == 'f' || bstring[0] == '0'))
					return false;
			}
			return defaultValueIfUnknown;
		}

		/// <summary>
		/// This routine will split the passed in string and return an
		/// array of those substrings.
		/// </summary>
		/// <param name="xyz">string to split</param>
		/// <returns>array of strings</returns>
		static public string[] SplitString(string xyz)
		{
			char[] delim = new char[] { ' ', '\n', (char)0x0D, (char)0x0A };
			string[] values = xyz.Split(delim);
			// now remove dups and empty entries
			ArrayList results = new ArrayList();
			foreach (string item in values)
			{
				if (item.Length > 0 && !results.Contains(item))
					results.Add(item);
			}
			// now back to a string []
			string[] strArray = new string[results.Count];
			int pos = 0;
			foreach (string item in results)
			{
				strArray[pos] = item;
				pos++;
			}
			return strArray;
		}

		/// <summary>
		/// Pull out the substrings from the passed in string.  Used for
		/// breaking up a string of markers.
		/// </summary>
		/// <param name="xyz">string to break up</param>
		/// <param name="list">hashtable to get the sub items</param>
		static public void SplitString(string xyz, ref Hashtable list)
		{
			char[] delim = new char[] { ' ', '\n', (char)0x0D, (char)0x0A };
			string[] values = xyz.Split(delim);
			foreach (string item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.ContainsKey(item))
					list.Add(item, null);
			}
		}

		/// <summary>
		/// Given a string, break it up based on the given delimiters and return
		/// those individual strings as elements in the arraylist param.
		/// </summary>
		/// <param name="xyz">string to break up</param>
		/// <param name="delim">list of delimeters to use</param>
		/// <param name="list">ref to arraylist results</param>
		public static void SplitString(string xyz, char[] delim, ref ArrayList list)
		{
			if (xyz == null || xyz.Length == 0)
				return;
			string[] values = xyz.Split(delim);
			foreach (string item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.Contains(item))
					list.Add(item);
			}
		}


		// Common strings: ignore, Unknown, and <Already in Unicode>
		public static string AlreadyInUnicode { get { return Sfm2XmlStrings.AlreadyInUnicode; } }
		public static string Ignore { get { return Sfm2XmlStrings.Ignore; } }
		public static string Unknown { get { return Sfm2XmlStrings.Unknown; } }

		/// <summary>
		/// Private helper method that is used for adding comment sections to the xml map and output
		/// files.
		/// </summary>
		/// <param name="comment"></param>
		/// <param name="XMLText"></param>
		static private void AddSectionComment(string comment, ref System.Text.StringBuilder XMLText)
		{
			string nl = System.Environment.NewLine;
			string dashes = "=====================================================================";
			XMLText.Append(nl + "<!--" + dashes + nl);
			XMLText.Append(comment + nl);
			XMLText.Append(dashes + " -->" + nl);
		}

		/// <summary>
		/// This is the common method for building a map file.  The map file is a key part of the import
		/// process: used for generating all the output files.
		/// </summary>
		/// <param name="uiLangs">list of language information</param>
		/// <param name="ILexFields"></param>
		/// <param name="sfmInfo"></param>
		/// <param name="listInFieldMarkers"></param>
		/// <param name="m_SaveAsFileName"></param>
		static public void NewMapFileBuilder(
			Hashtable uiLangs,
			ILexImportFields ILexFields,
			ILexImportFields ICustomFields,
			List<FieldHierarchyInfo> sfmInfo,
			List<Sfm2Xml.ClsInFieldMarker> listInFieldMarkers,	// was lvInFieldMarkers
			string saveAsFileName
			)
		{
			string nl = System.Environment.NewLine;
			//string mapFileVersion = "6.0";	// this maps to FW release 6.0, prev release was 5.4.1
			System.Text.StringBuilder XMLText = new System.Text.StringBuilder(8192);

			AddSectionComment("Created via the Lexical Import process: " + System.DateTime.Now.ToString(), ref XMLText);
			XMLText.Append("<sfmMapping version=\""+MapFileVersion+"\">" + nl);			// Start of the map file
			AddSectionComment("Global Settings", ref XMLText);
			// Global Settings section of XML map file
			XMLText.Append("<settings>" + nl);
			XMLText.Append("<meaning app=\"fw.sil.org\"/>" + nl);
			XMLText.Append("</settings>" + nl);
			// ====================================================================
			// Languages section of XML map file
			// ====================================================================
			AddSectionComment("Language Definitions", ref XMLText);
			XMLText.Append("<languages>" + nl);
			foreach (DictionaryEntry item in uiLangs)
			{
				LanguageInfoUI lang = item.Value as LanguageInfoUI;
				Sfm2Xml.ClsLanguage langObj = lang.ClsLanguage;
				if (lang.FwName == STATICS.Ignore)
				{
					langObj.XmlLang = lang.FwName;	// use 'ignore' for the xml:lang value
				}
				string xmlOutput = langObj.ToXmlString();
				XMLText.Append(xmlOutput + nl);
			}
			XMLText.Append("</languages>" + nl);
			// ====================================================================
			// Level Hierarchy section of XML map file
			// ====================================================================
			AddSectionComment("Level Hierarchy", ref XMLText);
			XMLText.Append("<hierarchy>" + nl);

			// now use the list of FieldHierarchyInfo and ILexFields to put out the two sections in the map file
			Dictionary<string, Sfm2Xml.ClsHierarchyEntry> hierarchyItems = new Dictionary<string, Sfm2Xml.ClsHierarchyEntry>();
			foreach (string className in ILexFields.Classes)
			{
				string partOf = ILexFields.HierarchForClass(className);
				hierarchyItems.Add(className, new Sfm2Xml.ClsHierarchyEntry(className, partOf, "", "", "", ""));
			}
			// now walk the list of FieldHierarchyInfo
			foreach (FieldHierarchyInfo fieldInfo in sfmInfo)
			{
				if (fieldInfo.IsAuto)
					continue;	// skip it for the Hierarchy list -- no change there
				// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
				string className;
				ILexImportField lfield = ILexFields.GetField(fieldInfo.FwDestID, out className);
				if (lfield == null)
				{
					className = fieldInfo.FwDestClass;	// currently only set for custom fields (7/08)
					lfield = ICustomFields.GetField(className, fieldInfo.FwDestID);
					// handle custom fields SOMEWHERE....?
					//if (lfield != null)
					//    continue;
				}
				System.Diagnostics.Debug.Assert(lfield != null, "Error in the data assumptions: fwDestID=<" + fieldInfo.FwDestID + ">");

				Sfm2Xml.ClsHierarchyEntry activeOne = hierarchyItems[className];
				if (fieldInfo.IsBegin)
					activeOne.AddBeginField(fieldInfo.SFM);	// if it's a begin, don't add to addtl and multi
				else
				{
					activeOne.AddAdditionalField(fieldInfo.SFM);	// not a begin then has to be additional
					if (lfield.IsMulti)
						activeOne.AddMultiField(fieldInfo.SFM);		// can also be a multi field
				}
				if (lfield.IsUnique)
					activeOne.AddUniqueField(fieldInfo.SFM);
			}
			// can now put out the hierarchy items
			foreach (Sfm2Xml.ClsHierarchyEntry hierarchyItem in hierarchyItems.Values)
			{
				if (hierarchyItem.BeginFields.Count == 0)
					hierarchyItem.AddBeginField(Ignore);
				string xmlOutput = hierarchyItem.ToXmlString();
				XMLText.Append(xmlOutput + nl);
			}

			XMLText.Append("</hierarchy>" + nl);
			// ====================================================================
			// Field Descriptions of XML map file
			// ====================================================================
			AddSectionComment("Field Descriptions", ref XMLText);
			XMLText.Append("<fieldDescriptions>" + nl);

			// now put out each Field Description
			foreach (FieldHierarchyInfo fieldInfo in sfmInfo)
			{
				ClsFieldDescriptionWrapper tmp;
				if (fieldInfo.IsAuto)
				{
					tmp = new ClsFieldDescriptionWrapper(fieldInfo.SFM, " ", "string", fieldInfo.Lang, false, "");
					tmp.IsAutoImportField = true;
				}
				else
				{
					// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
					string className;
					ILexImportField lfield = ILexFields.GetField(fieldInfo.FwDestID, out className);
					if (lfield == null)
					{
						className = fieldInfo.FwDestClass;	// currently only set for custom fields (7/08)
						lfield = ICustomFields.GetField(className, fieldInfo.FwDestID);
						if (lfield != null)
							continue;
					}
					System.Diagnostics.Debug.Assert(lfield != null, "Error in the data assumptions: fwDestID=<" + fieldInfo.FwDestID + ">");

					tmp = new ClsFieldDescriptionWrapper(fieldInfo.SFM, lfield.UIName, lfield.DataType, fieldInfo.Lang, lfield.IsAbbrField, fieldInfo.FwDestID);
				}
				tmp.RefFunc = fieldInfo.RefFunc;
				tmp.RefFuncWS = fieldInfo.RefFuncWS;
				tmp.IsExcluded = fieldInfo.IsExcluded;
				if (fieldInfo.IsAbbrvField)			// if it's an abbreviation field then
					tmp.IsAbbr = fieldInfo.IsAbbr;	//  set the value for it.

				string xmlOutput = tmp.ToXmlString();
				//string xmlOutputNew = fieldInfo.ClsFieldDescription.ToXmlString();
				//if (xmlOutput != xmlOutputNew)
				//    System.Diagnostics.Debug.WriteLine("xml string are different.");

				XMLText.Append(xmlOutput + nl);
			}
			XMLText.Append("</fieldDescriptions>" + nl);

			// ====================================================================
			// InField markers of XML map file
			// ====================================================================
			AddSectionComment("In Field Markers", ref XMLText);
			XMLText.Append("<inFieldMarkers>" + nl);

			foreach (Sfm2Xml.ClsInFieldMarker marker in listInFieldMarkers)
			{
				if (marker == null)
					continue;
				XMLText.Append(marker.ToXmlString() + nl);
			}
			XMLText.Append("</inFieldMarkers>" + nl);

			// ====================================================================
			// Custom Field Descriptions of XML map file
			// ====================================================================
			AddSectionComment("Custom Field Descriptions", ref XMLText);
			XMLText.Append("<CustomFieldDescriptions>" + nl);

			// now put out each Field Description
			foreach (FieldHierarchyInfo fieldInfo in sfmInfo)
			{
				//// ClsFieldDescriptionWrapper tmp;
				if (fieldInfo.IsAuto)	continue;	// skip if not a custom field

				// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
				string className = fieldInfo.FwDestClass;	// currently only set for custom fields (7/08)
				ILexImportCustomField lfield = ICustomFields.GetField(className, fieldInfo.FwDestID) as ILexImportCustomField;

//				ILexImportCustomField lfield = ICustomFields.GetField(fieldInfo.FwDestID, out className) as ILexImportCustomField;
				if (lfield == null)	continue;
				//// tmp = new ClsFieldDescriptionWrapper(fieldInfo.SFM, lfield.UIName, lfield.DataType, fieldInfo.Lang, lfield.IsAbbrField, fieldInfo.FwDestID);
				ClsCustomFieldDescription tmp = new ClsCustomFieldDescription(lfield.Class, className, /*lfield.CustomFieldID,*/ lfield.FLID, lfield.Big, lfield.WsSelector,
					fieldInfo.SFM, lfield.UIName, lfield.Signature/*DataType*/, fieldInfo.Lang, lfield.IsAbbrField, fieldInfo.FwDestID);

				tmp.RefFunc = fieldInfo.RefFunc;
				tmp.RefFuncWS = fieldInfo.RefFuncWS;
				tmp.IsExcluded = fieldInfo.IsExcluded;
				if (fieldInfo.IsAbbrvField)			// if it's an abbreviation field then
					tmp.IsAbbr = fieldInfo.IsAbbr;	//  set the value for it.

				string xmlOutput = tmp.ToXmlString();
				XMLText.Append(xmlOutput + nl);
			}
			XMLText.Append("</CustomFieldDescriptions>" + nl);


			// ====================================================================
			// now close out the map file
			// ====================================================================
			XMLText.Append("</sfmMapping>" + nl);

			try
			{
				using (System.IO.StreamWriter outMapFile = new System.IO.StreamWriter(saveAsFileName, false))
				{
					outMapFile.Write(XMLText);
					outMapFile.Close();
					//				m_dirtySenseLastSave = false;
				}
			}
			catch (System.Exception ex)
			{
				throw (ex);
			//    System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
			//    MessageBox.Show(this, "Problem saving settings: " + ex.Message, "Cannot Save Settings",
			//        MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
