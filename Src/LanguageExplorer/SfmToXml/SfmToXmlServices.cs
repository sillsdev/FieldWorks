// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class exists as a common place for putting static methods for the SfmToXml namespace.
	/// </summary>
	public class SfmToXmlServices
	{
		/// <summary>
		/// This maps to FW release 8.3Beta, prev release was 6.0
		/// </summary>
		public static string MapFileVersion => "6.1";

		/// <summary>
		/// Determine if the passed in string represents a 'true' or 'false' string
		/// and return it.  If it's not determined, then return the default value.
		/// </summary>
		/// <param name="text">string to examine for t or f</param>
		/// <param name="defaultValueIfUnknown">true or false</param>
		public static bool IsBoolString(string text, bool defaultValueIfUnknown)
		{
			if (text == null)
			{
				return defaultValueIfUnknown;
			}
			var bstring = text.ToLowerInvariant();
			if (bstring.Length > 0)
			{
				switch (bstring[0])
				{
					case 't':
					case '1':
						return true;
					case 'f':
					case '0':
						return false;
				}
			}
			return defaultValueIfUnknown;
		}

		/// <summary>
		/// This routine will split the passed in string and return an
		/// array of those substrings.
		/// </summary>
		/// <param name="xyz">string to split</param>
		/// <returns>array of strings</returns>
		public static string[] SplitString(string xyz)
		{
			var delim = new[] { ' ', '\n', (char)0x0D, (char)0x0A };
			var values = xyz.Split(delim);
			// now remove dups and empty entries
			var results = new ArrayList();
			foreach (var item in values)
			{
				if (item.Length > 0 && !results.Contains(item))
				{
					results.Add(item);
				}
			}
			// now back to a string []
			var strArray = new string[results.Count];
			var pos = 0;
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
		public static void SplitString(string xyz, ref Hashtable list)
		{
			var delim = new[] { ' ', '\n', (char)0x0D, (char)0x0A };
			var values = xyz.Split(delim);
			foreach (var item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.ContainsKey(item))
				{
					list.Add(item, null);
				}
			}
		}

		/// <summary>
		/// Given a string, break it up based on the given delimiters and return
		/// those individual strings as elements in the arraylist param.
		/// </summary>
		/// <param name="xyz">string to break up</param>
		/// <param name="delim">list of delimeters to use</param>
		/// <param name="list">ref to array list results</param>
		public static void SplitString(string xyz, char[] delim, ref ArrayList list)
		{
			if (string.IsNullOrEmpty(xyz))
			{
				return;
			}
			var values = xyz.Split(delim);
			foreach (var item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}

		// Common strings: ignore, Unknown, and <Already in Unicode>
		public static string AlreadyInUnicode => SfmToXmlStrings.AlreadyInUnicode;

		public static string Ignore => SfmToXmlStrings.Ignore;

		public static string Unknown => SfmToXmlStrings.Unknown;

		/// <summary>
		/// Private helper method that is used for adding comment sections to the xml map and output
		/// files.
		/// </summary>
		private static void AddSectionComment(string comment, ref StringBuilder xmlText)
		{
			var nl = Environment.NewLine;
			const string dashes = "=====================================================================";
			xmlText.Append($"{nl}<!--{dashes}{nl}");
			xmlText.AppendLine(comment);
			xmlText.Append($"{dashes} -->{nl}");
		}

		/// <summary>
		/// This is the common method for building a map file.  The map file is a key part of the import
		/// process: used for generating all the output files.
		/// </summary>
		public static void NewMapFileBuilder(Hashtable uiLangs, ILexImportFields lexFields, ILexImportFields customFields, List<FieldHierarchyInfo> sfmInfo, List<ClsInFieldMarker> listInFieldMarkers,
			string saveAsFileName, List<ILexImportOption> listOptions = null)
		{
			var xmlText = new StringBuilder(8192);
			AddSectionComment($"Created via the Lexical Import process: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}", ref xmlText);
			// Start of the map file
			xmlText.AppendLine($"<sfmMapping version=\"{MapFileVersion}\">");
			// ====================================================================
			// Global Settings section of XML map file
			// ====================================================================
			AddSectionComment("Global Settings", ref xmlText);
			xmlText.AppendLine("<settings>");
			xmlText.AppendLine("<meaning app=\"fw.sil.org\"/>");
			xmlText.AppendLine("</settings>");
			// ====================================================================
			// Import Options section of XML map file
			// ====================================================================
			AddSectionComment("Import Options", ref xmlText);
			xmlText.AppendLine("<options>");
			if (listOptions == null)
			{
				listOptions = new List<ILexImportOption>();
			}
			foreach (var importOption in listOptions)
			{
				switch (importOption.Type)
				{
					case "Checkbox":
						xmlText.AppendLine(importOption.ToXmlString());
						break;
					default:
						Debug.Fail("Unknown LexImportOption Type: " + importOption.Type);
						continue;
				}
			}
			xmlText.AppendLine("</options>");
			// ====================================================================
			// Languages section of XML map file
			// ====================================================================
			AddSectionComment("Language Definitions", ref xmlText);
			xmlText.AppendLine("<languages>");
			foreach (DictionaryEntry item in uiLangs)
			{
				var lang = item.Value as LanguageInfoUI;
				var langObj = lang.ClsLanguage;
				if (lang.FwName == Ignore)
				{
					langObj.XmlLang = lang.FwName;  // use 'ignore' for the xml:lang value
				}
				var xmlOutput = langObj.ToXmlString();
				xmlText.AppendLine(xmlOutput);
			}
			xmlText.AppendLine("</languages>");
			// ====================================================================
			// Level Hierarchy section of XML map file
			// ====================================================================
			AddSectionComment("Level Hierarchy", ref xmlText);
			xmlText.AppendLine("<hierarchy>");
			// now use the list of FieldHierarchyInfo and ILexFields to put out the two sections in the map file
			var hierarchyItems = new Dictionary<string, ClsHierarchyEntry>();
			foreach (string className in lexFields.Classes)
			{
				var partOf = lexFields.HierarchyForClass(className);
				hierarchyItems.Add(className, new ClsHierarchyEntry(className, partOf, string.Empty, string.Empty, string.Empty, string.Empty));
			}
			// now walk the list of FieldHierarchyInfo
			foreach (var fieldInfo in sfmInfo)
			{
				if (fieldInfo.IsAuto)
				{
					continue;   // skip it for the Hierarchy list -- no change there
				}
				// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
				string className;
				var lfield = lexFields.GetField(fieldInfo.FwDestID, out className);
				if (lfield == null)
				{
					className = fieldInfo.FwDestClass;  // currently only set for custom fields (7/08)
					lfield = customFields.GetField(className, fieldInfo.FwDestID);
				}
				Debug.Assert(lfield != null, "Error in the data assumptions: fwDestID=<" + fieldInfo.FwDestID + ">");
				var activeOne = hierarchyItems[className];
				if (fieldInfo.IsBegin)
				{
					activeOne.AddBeginField(fieldInfo.SFM); // if it's a begin, don't add to addtl and multi
				}
				else
				{
					activeOne.AddAdditionalField(fieldInfo.SFM);    // not a begin then has to be additional
					if (lfield.IsMulti)
					{
						activeOne.AddMultiField(fieldInfo.SFM);     // can also be a multi field
					}
				}
				if (lfield.IsUnique)
				{
					activeOne.AddUniqueField(fieldInfo.SFM);
				}
			}
			// can now put out the hierarchy items
			foreach (var hierarchyItem in hierarchyItems.Values)
			{
				if (hierarchyItem.BeginFields.Count == 0)
				{
					hierarchyItem.AddBeginField(Ignore);
				}
				var xmlOutput = hierarchyItem.ToXmlString();
				xmlText.AppendLine(xmlOutput);
			}
			xmlText.AppendLine("</hierarchy>");
			// ====================================================================
			// Field Descriptions of XML map file
			// ====================================================================
			AddSectionComment("Field Descriptions", ref xmlText);
			xmlText.AppendLine("<fieldDescriptions>");
			// now put out each Field Description
			foreach (var fieldInfo in sfmInfo)
			{
				ClsFieldDescription tmp;
				if (fieldInfo.IsAuto)
				{
					tmp = new ClsFieldDescription(fieldInfo.SFM, " ", "string", fieldInfo.Lang, false, string.Empty)
					{
						IsAutoImportField = true
					};
				}
				else
				{
					// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
					string className;
					var lfield = lexFields.GetField(fieldInfo.FwDestID, out className);
					if (lfield == null)
					{
						className = fieldInfo.FwDestClass;  // currently only set for custom fields (7/08)
						lfield = customFields.GetField(className, fieldInfo.FwDestID);
						if (lfield != null)
						{
							continue;
						}
					}
					Debug.Assert(lfield != null, "Error in the data assumptions: fwDestID=<" + fieldInfo.FwDestID + ">");
					tmp = new ClsFieldDescription(fieldInfo.SFM, lfield.UIName, lfield.DataType, fieldInfo.Lang, lfield.IsAbbrField, fieldInfo.FwDestID);
				}
				tmp.RefFunc = fieldInfo.RefFunc;
				tmp.RefFuncWS = fieldInfo.RefFuncWS;
				tmp.IsExcluded = fieldInfo.IsExcluded;
				if (fieldInfo.IsAbbrvField)         // if it's an abbreviation field then
				{
					tmp.IsAbbr = fieldInfo.IsAbbr;  //  set the value for it.
				}
				var xmlOutput = tmp.ToXmlString();
				xmlText.AppendLine(xmlOutput);
			}
			xmlText.AppendLine("</fieldDescriptions>");
			// ====================================================================
			// InField markers of XML map file
			// ====================================================================
			AddSectionComment("In Field Markers", ref xmlText);
			xmlText.AppendLine("<inFieldMarkers>");
			foreach (var marker in listInFieldMarkers)
			{
				if (marker == null)
				{
					continue;
				}
				xmlText.AppendLine(marker.ToXmlString());
			}
			xmlText.AppendLine("</inFieldMarkers>");
			// ====================================================================
			// Custom Field Descriptions of XML map file
			// ====================================================================
			AddSectionComment("Custom Field Descriptions", ref xmlText);
			xmlText.AppendLine("<CustomFieldDescriptions>");
			// now put out each Field Description
			foreach (var fieldInfo in sfmInfo)
			{
				if (fieldInfo.IsAuto)
				{
					continue; // skip if not a custom field
				}
				// get the class for the destination field : ASSUMPTION EACH FWDESTID IS UNIQUE AND NOT DUPLICATED AMONG CLASSES
				var className = fieldInfo.FwDestClass;   // currently only set for custom fields (7/08)
				var lfield = customFields.GetField(className, fieldInfo.FwDestID) as ILexImportCustomField;
				if (lfield == null)
				{
					continue;
				}
				var tmp = new ClsCustomFieldDescription(lfield.Class, className, lfield.FLID, lfield.Big, lfield.WsSelector, fieldInfo.SFM, lfield.UIName, lfield.Signature, fieldInfo.Lang, lfield.IsAbbrField, fieldInfo.FwDestID)
				{
					RefFunc = fieldInfo.RefFunc,
					RefFuncWS = fieldInfo.RefFuncWS,
					IsExcluded = fieldInfo.IsExcluded
				};
				if (fieldInfo.IsAbbrvField)         // if it's an abbreviation field then
				{
					tmp.IsAbbr = fieldInfo.IsAbbr;  //  set the value for it.
				}
				xmlText.AppendLine(tmp.ToXmlString());
			}
			xmlText.AppendLine("</CustomFieldDescriptions>");
			// ====================================================================
			// now close out the map file
			// ====================================================================
			xmlText.AppendLine("</sfmMapping>");
			using (var outMapFile = new StreamWriter(saveAsFileName, false))
			{
				outMapFile.Write(xmlText);
				outMapFile.Close();
			}
		}
	}
}