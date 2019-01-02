// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This 'log' class servers as a common location for capturing error and
	/// warning messages that are captured during the conversion process.
	/// They are accumulated and then added to the output xml file at the end.
	/// </summary>
	public class ClsLog
	{
		// Errors and warnings are kept and shown separately
		private List<WrnErrInfo> m_fatalErrors;
		private List<WrnErrInfo> m_errors;
		private List<WrnErrInfo> m_warnings;
		private int m_outOfOrderWarningsCount;
		/// <summary>
		/// key=uniqueEntryName(string), value=hashtable of (key=sfm, value=list of lines(int))
		/// </summary>
		private Dictionary<string, Dictionary<string, List<int>>> m_outOfOrderWarningsEntries;
		private List<Dictionary<string, List<int>>> m_outOfOrderWarningsEntriesOrdered;
		private List<string> m_outOfOrderWarningsEntriesKEYS;
		private Dictionary<string, int> m_sfmWarningsAndErrors;
		private HashSet<string> m_uniqueMsgs;
		private int m_sfmsWithOutData;
		/// <summary>
		/// Dictionary of SfmData objects
		/// </summary>
		private Dictionary<string, SfmData> m_sfmData;
		private const int NumErrorsWithInfo = 2000; // after this number of errors just keep a count
		private const int NumCautionsMAX = 2000; // after this number of cautions, just keep a count...

		public ClsLog()
		{
			Reset();
		}

		public void Reset()
		{
			m_fatalErrors = new List<WrnErrInfo>();
			m_errors = new List<WrnErrInfo>();
			m_warnings = new List<WrnErrInfo>();
			m_outOfOrderWarningsEntries = new Dictionary<string, Dictionary<string, List<int>>>();
			m_outOfOrderWarningsEntriesOrdered = new List<Dictionary<string, List<int>>>();
			m_outOfOrderWarningsEntriesKEYS = new List<string>();
			m_outOfOrderWarningsCount = 0;
			m_sfmWarningsAndErrors = new Dictionary<string, int>();
			m_uniqueMsgs = new HashSet<string>();
			m_sfmData = new Dictionary<string, SfmData>();
			m_sfmsWithOutData = 0;
		}

		private void FoundSFMNotDefined(string sfm)
		{
			SfmData data;
			if (!m_sfmData.TryGetValue(sfm, out data))
			{
				data = new SfmData();
				m_sfmData.Add(sfm, data);
			}
			data.NotDefined++;
		}

		private void FoundSFMWithData(string sfm)
		{
			SfmData data;
			if (!m_sfmData.TryGetValue(sfm, out data))
			{
				data = new SfmData();
				m_sfmData.Add(sfm, data);
			}
			data.WithData++;
		}

		private void FoundSFMWithoutData(string sfm)
		{
			m_sfmsWithOutData++;
			SfmData data;
			if (!m_sfmData.TryGetValue(sfm, out data))
			{
				data = new SfmData();
				m_sfmData.Add(sfm, data);
			}
			data.WithoutData++;
		}

		private void FoundSFMError(string sfm)
		{
			SfmData data;
			if (!m_sfmData.TryGetValue(sfm, out data))
			{
				data = new SfmData();
				m_sfmData.Add(sfm, data);
			}
			data.Errors++;
		}

		private void FoundSFMWarning(string sfm)
		{
			SfmData data;
			if (!m_sfmData.TryGetValue(sfm, out data))
			{
				data = new SfmData();
				m_sfmData.Add(sfm, data);
			}
			data.Warnings++;
		}

		public void AddUniqueHighPriorityError(string key, string msg)
		{
			if (!m_uniqueMsgs.Contains(key))
			{
				m_uniqueMsgs.Add(key);
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

		private static string MakeSafeXML(string text)
		{
			var result = text;
			// minor mods to improve performance - only call the replace if the character find is good
			if (result.IndexOf('&') != -1)
			{
				result = result.Replace("&", "&amp;");
			}
			if (result.IndexOf('<') != -1)
			{
				result = result.Replace("<", "&lt;");
			}
			if (result.IndexOf('>') != -1)
			{
				result = result.Replace(">", "&gt;");
			}
			return result;
		}

		public void AddSFMWarning(string sfm, string text)
		{
			AddSFMWarning(string.Empty, -1, sfm, text);
		}

		public void AddSFMWarning(string file, int line, string sfm, string text)
		{
			int counter;
			if (m_sfmWarningsAndErrors.TryGetValue(sfm, out counter))
			{
				m_sfmWarningsAndErrors[sfm] = ++counter;
			}
			else
			{
				m_sfmWarningsAndErrors.Add(sfm, 1);
				text = MakeSafeXML(text);
				AddWarning(file, line, text);
			}
			FoundSFMWarning(sfm);
		}
		public void AddSFMError(string sfm, string text)
		{
			AddSFMError(string.Empty, -1, sfm, text);
		}


		public void AddSFMError(string file, int line, string sfm, string text)
		{
			int counter;
			if (m_sfmWarningsAndErrors.TryGetValue(sfm, out counter))
			{
				m_sfmWarningsAndErrors[sfm] = ++counter;
			}
			else
			{
				m_sfmWarningsAndErrors.Add(sfm, 1);
				text = MakeSafeXML(text);
				AddError(file, line, text);
			}
			FoundSFMError(sfm);
		}

		public void AddWarning(string text)
		{
			AddWarning(string.Empty, -1, text);
		}

		public void AddWarning(string file, int line, string text)
		{
			if (m_warnings.Count > NumErrorsWithInfo)
			{
				return;
			}
			text = MakeSafeXML(text);
			m_warnings.Add(new WrnErrInfo(file, line, text));

		}

		public void AddError(string text)
		{
			AddError(string.Empty, -1, text);
		}

		public void AddError(string file, int line, string text)
		{
			if (m_errors.Count > NumErrorsWithInfo)
			{
				return;
			}
			text = MakeSafeXML(text);
			m_errors.Add(new WrnErrInfo(file, line, text));
		}

		public void AddFatalError(string text)
		{
			AddFatalError(string.Empty, -1, text);
		}

		public void AddFatalError(string file, int line, string text)
		{
			if (m_fatalErrors.Count > NumErrorsWithInfo)
			{
				return;
			}
			text = MakeSafeXML(text);
			m_fatalErrors.Add(new WrnErrInfo(file, line, text));
		}

		public void AddOutOfOrderCaution(string entryKey, string marker, int line)
		{
			Dictionary<string, List<int>> markers;
			if (m_outOfOrderWarningsEntries.ContainsKey(entryKey))
			{
				markers = m_outOfOrderWarningsEntries[entryKey];
			}
			else
			{
				if (++m_outOfOrderWarningsCount > NumCautionsMAX)
				{
					// just bump the counter - don't add the information
					return;
				}
				markers = new Dictionary<string, List<int>>();
				m_outOfOrderWarningsEntries.Add(entryKey, markers);
				m_outOfOrderWarningsEntriesOrdered.Add(markers);
				m_outOfOrderWarningsEntriesKEYS.Add(entryKey);
			}
			List<int> lines;
			if (markers.ContainsKey(marker))
			{
				lines = markers[marker];
			}
			else
			{
				lines = new List<int>();
				markers.Add(marker, lines);
			}
			lines.Add(line);
		}

		/// <summary>
		/// Put the contents of the error and warning information to the
		/// passed in xml text writer.
		/// </summary>
		public void FlushTo(XmlTextWriter xmlOutput)
		{
			xmlOutput.Flush();
			var ttlWarnings = m_warnings.Count + m_sfmsWithOutData; // warnings and sfm's w/o data
			var ttlErrors = m_fatalErrors.Count + m_errors.Count;   // fatal and reg errors
			if (ttlErrors + ttlWarnings == 0)
			{
				xmlOutput.WriteComment(" There were No errors or warnings during the creation of this file");
			}
			// put out the root error element
			xmlOutput.WriteComment(" This element contains error, warning and sfm data related information ");
			xmlOutput.WriteStartElement("ErrorLog");
			OutputErrorElement(xmlOutput);                  // errors
			OutputWarningElement(xmlOutput);                // warnings
			OutputOutOfOrderInfo(xmlOutput);                       // out of order cautions
			OutputSfmInfo(xmlOutput);                       // general statistics on sfm usage
			xmlOutput.WriteEndElement();                    // end the "ErrorLog" element
			// remove all the errors and warnings
			m_errors.Clear();
			m_warnings.Clear();
			m_sfmWarningsAndErrors.Clear();
			m_fatalErrors.Clear();
			m_uniqueMsgs.Clear();
			m_sfmsWithOutData = 0;
			m_sfmData.Clear();
			m_outOfOrderWarningsCount = 0;
			m_outOfOrderWarningsEntries.Clear();
			m_outOfOrderWarningsEntriesKEYS.Clear();
			m_outOfOrderWarningsEntriesOrdered.Clear();
		}

		private void OutputErrorElement(XmlTextWriter xmlOutput)
		{
			// put out the errors
			var ttlErrors = m_fatalErrors.Count + m_errors.Count;   // fatal and reg errors
			xmlOutput.WriteStartElement("Errors");
			xmlOutput.WriteAttributeString("count", ttlErrors.ToString());
			if (m_fatalErrors.Count > NumErrorsWithInfo || m_errors.Count > NumErrorsWithInfo)
			{
				var ttl = 0;
				ttl += m_fatalErrors.Count > NumErrorsWithInfo ? NumErrorsWithInfo : m_fatalErrors.Count;
				ttl += m_errors.Count > NumErrorsWithInfo ? NumErrorsWithInfo : m_errors.Count;

				xmlOutput.WriteAttributeString("listed", ttl.ToString());
			}
			foreach (var info in m_fatalErrors)
			{
				if (info == null)   // end of real messages, rest are null for a total count
				{
					break;
				}
				xmlOutput.WriteStartElement("Error");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			foreach (var info in m_errors)
			{
				if (info == null)   // end of real messages, rest are null for a total count
				{
					break;
				}
				xmlOutput.WriteStartElement("Error");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			xmlOutput.WriteEndElement();
		}

		private void OutputWarningElement(XmlTextWriter xmlOutput)
		{
			var ttlWarnings = m_warnings.Count;
			xmlOutput.WriteStartElement("Warnings");
			xmlOutput.WriteAttributeString("count", ttlWarnings.ToString());
			foreach (var info in m_warnings)
			{
				if (info == null)   // end of real messages, rest are null for a total count
				{
					break;
				}
				xmlOutput.WriteStartElement("Warning");
				xmlOutput.WriteAttributeString("File", info.FileName);
				xmlOutput.WriteAttributeString("Line", info.LineNumber.ToString());
				xmlOutput.WriteRaw(info.Message);
				xmlOutput.WriteEndElement();
			}
			xmlOutput.WriteEndElement();
		}

		private void OutputSfmInfo(XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartElement("SfmInfoList");
			foreach (var sfmInfoKvp in m_sfmData)
			{
				var data = sfmInfoKvp.Value;
				var sfm = sfmInfoKvp.Key;
				var ttlCount = data.WithData + data.WithoutData + data.NotDefined;
				if (ttlCount > 0)
				{
					xmlOutput.WriteStartElement("SfmInfo");
					xmlOutput.WriteAttributeString("sfm", sfm);
					var usagePercent = (int)(data.WithData / (double)ttlCount * 100);
					xmlOutput.WriteAttributeString("ttlCount", ttlCount.ToString());
					if (data.WithoutData > 0)
					{
						xmlOutput.WriteAttributeString("emptyCount", data.WithoutData.ToString());
					}
					xmlOutput.WriteAttributeString("usagePercent", usagePercent.ToString());
					// add the raw counts for other calculations
					xmlOutput.WriteAttributeString("withDataCount", data.WithData.ToString());
					xmlOutput.WriteAttributeString("withoutDataCount", data.WithoutData.ToString());
					xmlOutput.WriteAttributeString("notDefinedCount", data.NotDefined.ToString());

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
		private void OutputOutOfOrderInfo(XmlTextWriter xmlOutput)
		{
			xmlOutput.WriteStartElement("OutOfOrder");
			xmlOutput.WriteAttributeString("count", m_outOfOrderWarningsCount.ToString());
			if (m_outOfOrderWarningsCount > NumCautionsMAX)
			{
				xmlOutput.WriteAttributeString("listed", NumCautionsMAX.ToString());
			}
			// Don't use the hashtable for outputting, use the array list that has been
			// maintained to be sequential in the order of occurence.
			for (var cautionNum = 0; cautionNum < m_outOfOrderWarningsEntriesOrdered.Count; cautionNum++)
			{
				var entryName = m_outOfOrderWarningsEntriesKEYS[cautionNum];
				var markers = m_outOfOrderWarningsEntriesOrdered[cautionNum];
				xmlOutput.WriteStartElement("Caution");
				var startPos = entryName.IndexOf(':') + 1;
				var nameKey = entryName.Substring(startPos);
				xmlOutput.WriteAttributeString("name", nameKey);
				xmlOutput.WriteAttributeString("count", markers.Count.ToString());
				foreach (var markerInfoKvp in markers)
				{
					var marker = markerInfoKvp.Key;
					var lines = markerInfoKvp.Value;
					xmlOutput.WriteStartElement("Marker");
					xmlOutput.WriteAttributeString("name", marker);
					xmlOutput.WriteAttributeString("count", lines.Count.ToString());
					foreach (var line in lines)
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

		private sealed class SfmData
		{
			internal int Warnings { get; set; }
			internal int Errors { get; set; }
			internal int WithData { get; set; }
			internal int WithoutData { get; set; }
			internal int NotDefined { get; set; }

			internal SfmData()
			{
				Warnings = 0;
				Errors = 0;
				NotDefined = 0;
				WithData = 0;
				WithoutData = 0;
			}
		}

		private sealed class WrnErrInfo
		{
			internal WrnErrInfo(string file, int line, string msg)
			{
				FileName = file;
				LineNumber = line;
				Message = msg;
			}

			internal string FileName { get; }

			internal string Message { get; }

			internal int LineNumber { get; }
		}
	}
}