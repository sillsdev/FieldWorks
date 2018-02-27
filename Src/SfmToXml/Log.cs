// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;

namespace Sfm2Xml
{
	public class SfmData
	{
		public int m_Warnings;		// number of warnings on these markers
		public int m_Errors;		// number of errors on these markers
		//public int m_FoundInData;	// total number found in the data
		public int m_WithData;		// number found with data
		public int m_WithoutData;	// number found with out data
		public int m_NotDefined;	// count of sfm used but not defined in map

		public SfmData()
		{
			m_Warnings = 0;
			m_Errors = 0;
		//	m_FoundInData = 0;
			m_NotDefined = 0;
			m_WithData = 0;
			m_WithoutData = 0;
		}
	}

	public class WrnErrInfo
	{
		private string m_msg;
		private string m_fileName;
		private int m_lineNo;

		public WrnErrInfo(string file, int line, string msg)
		{
			m_fileName = file;
			m_lineNo = line;
			m_msg = msg;
		}

		public string FileName { get { return m_fileName;}}
		public string Message { get { return m_msg; }}
		public int LineNumber { get { return m_lineNo; }}
	}


	/// <summary>
	/// This 'log' class servers as a common location for capturing error and
	/// warning messages that are captured during the conversion process.
	/// They are accumulated and then added to the output xml file at the end.
	/// </summary>
	public class ClsLog
	{
		// Errors and warnings are kept and shown seperatly
		// A simple arraylist is adequate for storage
		private ArrayList m_FatalErrors;
		private ArrayList m_Errors;
		private ArrayList m_Warnings;
		private ArrayList m_OOO;		// out of order warnings
		private int m_OOOCnt;			// count for all cautions on all entries
		private Hashtable m_OOOEntries;	// key=uniqueEntryName(string), value=hashtable of (key=sfm, value=arraylist of lines(int)
		private ArrayList m_OOOEntriesOrdered;	// this is the orderd list of occurance of the value of the above hashtable
		private ArrayList m_OOOEntriesKEYS;		// keys for the ooo entries hash used at same index as 'orderd' list
		private Hashtable m_sfmWarningsAndErrors;
		private Hashtable m_uniqueMsgs;
		private int m_sfmsWithOutData;
		private int m_sfmsWithData;
		private Hashtable m_sfmData;		// hash of SfmData objects

		private static int NumErrorsWithInfo = 2000;	// after this number of errors just keep a count
		private static int NumCautionsMAX = 2000;		// after this number of cautions, just keep a count...

		public ClsLog()
		{
			Reset();
		}

		public void Reset()
		{
			m_FatalErrors = new ArrayList();
			m_Errors = new ArrayList();
			m_Warnings = new ArrayList();
			m_OOO = new ArrayList();
			m_OOOEntries = new Hashtable();
			m_OOOEntriesOrdered = new ArrayList();
			m_OOOEntriesKEYS = new ArrayList();
			m_OOOCnt = 0;
			m_sfmWarningsAndErrors = new Hashtable();
			m_uniqueMsgs = new Hashtable();
			m_sfmData = new Hashtable();
			m_sfmsWithOutData = 0;
			m_sfmsWithData = 0;
		}

		private void FoundSFMNotDefined(string sfm)
		{
			SfmData data = new SfmData();
			if (m_sfmData.ContainsKey(sfm))
				data = m_sfmData[sfm] as SfmData;
			data.m_NotDefined++;
			m_sfmData[sfm] = data;
		}

		private void FoundSFMWithData(string sfm)
		{
			m_sfmsWithData++;
			SfmData data = new SfmData();
			if (m_sfmData.ContainsKey(sfm))
				data = m_sfmData[sfm] as SfmData;
			data.m_WithData++;
			m_sfmData[sfm] = data;
		}

		private void FoundSFMWithoutData(string sfm)
		{
			m_sfmsWithOutData++;
			SfmData data = new SfmData();
			if (m_sfmData.ContainsKey(sfm))
				data = m_sfmData[sfm] as SfmData;
			data.m_WithoutData++;
			m_sfmData[sfm] = data;
		}

		private void FoundSFMError(string sfm)
		{
			SfmData data = new SfmData();
			if (m_sfmData.ContainsKey(sfm))
				data = m_sfmData[sfm] as SfmData;
			data.m_Errors++;
			m_sfmData[sfm] = data;
		}

		private void FoundSFMWarning(string sfm)
		{
			SfmData data = new SfmData();
			if (m_sfmData.ContainsKey(sfm))
				data = m_sfmData[sfm] as SfmData;
			data.m_Warnings++;
			m_sfmData[sfm] = data;
		}

		public void AddUniqueHighPriorityError(string key, string msg)
		{
			if (!m_uniqueMsgs.ContainsKey(key))
			{
				m_uniqueMsgs.Add(key, null);
				AddFatalError(msg);
			}
		}

		public void AddSFMWithData(string sfm)
		{
			FoundSFMWithData(sfm);
		}

		public void AddSFMNotDefined(string sfm)
		{
			FoundSFMNotDefined(sfm);
		}

		public void AddSFMNoData(string sfm)
		{
			FoundSFMWithoutData(sfm);
		}

		private string MakeSafeXML(string text)
		{
			string result = text;
			// minor mods to improve performance - only call the replace if the character find is good
			if (result.IndexOf('&') != -1)
				result = result.Replace("&", "&amp;");
			if (result.IndexOf('<') != -1)
				result = result.Replace("<", "&lt;");
			if (result.IndexOf('>') != -1)
				result = result.Replace(">", "&gt;");
			return result;
		}

		public void AddSFMWarning(string sfm, string text)
		{
			AddSFMWarning("", -1, sfm, text);
		}

		public void AddSFMWarning(string file, int line, string sfm, string text)
		{
			if (m_sfmWarningsAndErrors.ContainsKey(sfm))
			{
				int val = (int)m_sfmWarningsAndErrors[sfm] + 1;
				m_sfmWarningsAndErrors[sfm] = val;
			}
			else
			{
				m_sfmWarningsAndErrors.Add(sfm, (int)1);
				text = MakeSafeXML(text);
				AddWarning(file, line, text);
			}
			FoundSFMWarning(sfm);
		}
		public void AddSFMError(string sfm, string text)
		{
			AddSFMError("", -1, sfm, text);
		}


		public void AddSFMError(string file, int line, string sfm, string text)
		{
			if (m_sfmWarningsAndErrors.ContainsKey(sfm))
			{
				int val = (int)m_sfmWarningsAndErrors[sfm] + 1;
				m_sfmWarningsAndErrors[sfm] = val;
			}
			else
			{
				m_sfmWarningsAndErrors.Add(sfm, (int)1);
				text = MakeSafeXML(text);
				AddError(file, line, text);
			}
			FoundSFMError(sfm);
		}

		public void AddWarning(string text)
		{
			AddWarning("", -1, text);
		}

		public void AddWarning(string file, int line, string text)
		{
			if (m_Warnings.Count > NumErrorsWithInfo)
			{
				m_Warnings.Add(null);	// just a counter
				return;
			}

			text = MakeSafeXML(text);
			m_Warnings.Add(new WrnErrInfo(file, line, text));

		}

		public void AddError(string text)
		{
			AddError("", -1, text);
		}

		public void AddError(string file, int line, string text)
		{
			if (m_Errors.Count > NumErrorsWithInfo)
			{
				m_Errors.Add(null);	// just a counter
				return;
			}

			text = MakeSafeXML(text);
			m_Errors.Add(new WrnErrInfo(file, line, text));
		}

		public void AddFatalError(string text)
		{
			AddFatalError("", -1, text);
		}

		public void AddFatalError(string file, int line, string text)
		{
			if (m_FatalErrors.Count > NumErrorsWithInfo)
			{
				m_FatalErrors.Add(null);	// just a counter
				return;
			}

			text = MakeSafeXML(text);
			m_FatalErrors.Add(new WrnErrInfo(file, line, text));
		}

		public void AddOutOfOrderCaution(string EntryKey, string marker, int line)
		{
			Hashtable markers;
			if (m_OOOEntries.ContainsKey(EntryKey))
			{
				markers = m_OOOEntries[EntryKey] as Hashtable;
			}
			else
			{
				if (++m_OOOCnt > NumCautionsMAX)	// just bump the counter - don't add the information
					return;
				markers = new Hashtable();
				m_OOOEntries.Add(EntryKey, markers);
				m_OOOEntriesOrdered.Add(markers);
				m_OOOEntriesKEYS.Add(EntryKey);
			}

			ArrayList lines;
			if (markers.ContainsKey(marker))
			{
				lines = markers[marker] as ArrayList;
			}
			else
			{
				lines = new ArrayList();
				markers.Add(marker, lines);
			}
			lines.Add(line);
		}

		/// <summary>
		/// Put the contents of the error and warning information to the
		/// passed in xml text writer.
		/// </summary>
		/// <param name="xmlOutput"></param>
		public void FlushTo(System.Xml.XmlTextWriter xmlOutput)
		{
			xmlOutput.Flush();
			int ttlWarnings = m_Warnings.Count + m_sfmsWithOutData;	// warnings and sfm's w/o data
			int ttlErrors = m_FatalErrors.Count + m_Errors.Count;	// fatal and reg errors

			if (ttlErrors + ttlWarnings == 0)
				xmlOutput.WriteComment(" There were No errors or warnings during the creation of this file");

			// put out the root error element
			xmlOutput.WriteComment(" This element contains error, warning and sfm data related information ");
			xmlOutput.WriteStartElement("ErrorLog");

			OutputErrorElement(xmlOutput);					// errors
			OutputWarningElement(xmlOutput);				// warnings
			OutputOOOInfo(xmlOutput);						// out of order cautions
			OutputDuplicateSfmWarnErrElement(xmlOutput);	// duplicate SFM errors and warnings
			OutputSfmInfo(xmlOutput);						// general statistics on sfm usage
			xmlOutput.WriteEndElement();					// end the "ErrorLog" element

			// remove all the errors and warnings
			m_Errors.Clear();
			m_Warnings.Clear();
			m_sfmWarningsAndErrors.Clear();
			m_FatalErrors.Clear();
			m_uniqueMsgs.Clear();
			m_sfmsWithOutData = 0;
			m_sfmsWithData = 0;
			m_sfmData.Clear();
			m_OOO.Clear();
			m_OOOCnt = 0;
			m_OOOEntries.Clear();
			m_OOOEntriesKEYS.Clear();
			m_OOOEntriesOrdered.Clear();
		}

		private void OutputErrorElement(System.Xml.XmlTextWriter xmlOutput)
		{
			// put out the errors
			int ttlErrors = m_FatalErrors.Count + m_Errors.Count;	// fatal and reg errors
			xmlOutput.WriteStartElement("Errors");
			xmlOutput.WriteAttributeString("count", ttlErrors.ToString());
			if (m_FatalErrors.Count > NumErrorsWithInfo || m_Errors.Count > NumErrorsWithInfo)
			{
				int ttl = 0;
				if (m_FatalErrors.Count > NumErrorsWithInfo)
					ttl += NumErrorsWithInfo;
				else
					ttl +=m_FatalErrors.Count;

				if (m_Errors.Count > NumErrorsWithInfo)
					ttl += NumErrorsWithInfo;
				else
					ttl +=m_Errors.Count;

				xmlOutput.WriteAttributeString("listed", ttl.ToString());
			}
//			foreach (string msg in m_FatalErrors)
			foreach (WrnErrInfo info in m_FatalErrors)
			{
				if (info == null)	// end of real messages, rest are null for a total count
					break;
				xmlOutput.WriteStartElement("Error");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			foreach (WrnErrInfo info in m_Errors)
			{
				if (info == null)	// end of real messages, rest are null for a total count
					break;
				xmlOutput.WriteStartElement("Error");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			xmlOutput.WriteEndElement();
		}

		private void OutputWarningElement(System.Xml.XmlTextWriter xmlOutput)
		{
			int ttlWarnings = m_Warnings.Count;// + m_sfmsWithOutData;	// warnings and sfm's w/o data
			xmlOutput.WriteStartElement("Warnings");
			xmlOutput.WriteAttributeString("count", ttlWarnings.ToString());
			foreach (WrnErrInfo info in m_Warnings)
			{
				if (info == null)	// end of real messages, rest are null for a total count
					break;
				xmlOutput.WriteStartElement("Warning");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			xmlOutput.WriteEndElement();
		}

		private void OutputDuplicateSfmWarnErrElement(System.Xml.XmlTextWriter xmlOutput)
		{
			/*
			xmlOutput.WriteStartElement("SfmDups");
			foreach (DictionaryEntry dupSFM in m_sfmWarningsAndErrors)
			{
				int count = (int)dupSFM.Value;
				if (count > 0)
				{
					xmlOutput.WriteStartElement("SfmDup");
					xmlOutput.WriteAttributeString("sfm", dupSFM.Key as string);
					xmlOutput.WriteAttributeString("count", count.ToString());
					xmlOutput.WriteEndElement();
				}
			}
			xmlOutput.WriteEndElement();
			*/
		}

		private void OutputSfmInfo(System.Xml.XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartElement("SfmInfoList");
			foreach (DictionaryEntry sfmInfo in m_sfmData)
			{
				SfmData data = sfmInfo.Value as SfmData;
				string sfm = sfmInfo.Key as string;
				int ttlCount = data.m_WithData + data.m_WithoutData + data.m_NotDefined;
				if (ttlCount > 0)
				{
					xmlOutput.WriteStartElement("SfmInfo");
					xmlOutput.WriteAttributeString("sfm", sfm);
					int usagePercent = (int)((double)data.m_WithData / (double)ttlCount * 100);
					xmlOutput.WriteAttributeString("ttlCount", ttlCount.ToString());
					if (data.m_WithoutData > 0)
						xmlOutput.WriteAttributeString("emptyCount", data.m_WithoutData.ToString());
					xmlOutput.WriteAttributeString("usagePercent", usagePercent.ToString());

					// add the raw counts for other calculations
					xmlOutput.WriteAttributeString("withDataCount", data.m_WithData.ToString());
					xmlOutput.WriteAttributeString("withoutDataCount", data.m_WithoutData.ToString());
					xmlOutput.WriteAttributeString("notDefinedCount", data.m_NotDefined.ToString());

					xmlOutput.WriteEndElement();
				}
			}
			xmlOutput.WriteEndElement();
		}

		// sample of the output generated by the OutputOOOInfo method
		//	<OutOfOrder count="1">
		//        <Caution name="Entry" count="2">
		//            <marker name="lx" count="1">
		//                <line value="1"/>
		//            </marker>
		//            <marker name="de" count="2">
		//                <line value="3"/>
		//                <line value="24"/>
		//            </marker>
		//        </Caution>
		//    </OutOfOrder>
		private void OutputOOOInfo(System.Xml.XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartElement("OutOfOrder");
			xmlOutput.WriteAttributeString("count", m_OOOCnt.ToString());
			if (m_OOOCnt > NumCautionsMAX)
				xmlOutput.WriteAttributeString("listed", NumCautionsMAX.ToString());

			// Don't use the hashtable for outputing, use the arraylist that has been
			// maintained to be sequential in the order of occurance.
			for (int cautionNum=0; cautionNum<m_OOOEntriesOrdered.Count; cautionNum++)
			{
				string entryName = m_OOOEntriesKEYS[cautionNum] as string;
				Hashtable markers = m_OOOEntriesOrdered[cautionNum] as Hashtable;

				xmlOutput.WriteStartElement("Caution");
				// The key has the line number prepended to the text "123:name",
				// so strip off the "123:"
				//xmlOutput.WriteAttributeString("name", entryName.Substring(0,5));
				int startPos = entryName.IndexOf(':')+1;
				string nameKey = entryName.Substring(startPos);
				xmlOutput.WriteAttributeString("name", nameKey);
				xmlOutput.WriteAttributeString("count", markers.Count.ToString());

				foreach (DictionaryEntry markerInfo in markers)
				{
					string marker = markerInfo.Key as string;
					ArrayList lines = markerInfo.Value as ArrayList;

					xmlOutput.WriteStartElement("Marker");
					xmlOutput.WriteAttributeString("name", marker);
					xmlOutput.WriteAttributeString("count", lines.Count.ToString());
					foreach (int line in lines)
					{
						xmlOutput.WriteStartElement("Line");
						xmlOutput.WriteAttributeString("value", line.ToString());
						xmlOutput.WriteEndElement();
					}
					xmlOutput.WriteEndElement();
				}
				xmlOutput.WriteEndElement();
			}
			xmlOutput.WriteEndElement();
		}

	}
}
