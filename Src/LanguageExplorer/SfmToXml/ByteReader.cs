// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// Replacement reader class for byte reading.
	/// </summary>
	public class ByteReader
	{
		private int m_FoundLineNumber;  // line number when the marker was found
		private byte[] m_NL;        // bytes for newline
		private byte[] m_EOL;       // end of line for this file
		private byte[] m_space;     // byte(s) for space character
		private const byte m_backSlash = 0x5c;
		protected byte[] m_FileData;
		protected string m_FileName;    // name of the file
		private long m_position;    // current position into the file data (m_FileData)
		private string m_sfmLookAheadMarker;
		private byte[] m_sfmLookAheadMarkerBadBytes; // null or array of bytes that are invalid for the marker
		private byte[] m_sfmLookAheadData;
		private bool m_hasLookAhead;        // true if the GetSfmData return true;
		private int m_LookAheadLineNumber;  // line number of look ahead sfm
		// BOM related data
		private Encoding m_BOMEncoding;
		private int m_BOMLength;
		private bool m_foundBOM;
		private ClsLog m_Log;

		/// <summary>
		/// Constructor requires a file name
		/// </summary>
		internal ByteReader(string filename, ref ClsLog Log)
		{
			m_Log = Log;
			Init(filename);
		}

		public string FileName => m_FileName;

		/// <summary>
		/// Constructor requires a file name
		/// </summary>
		public ByteReader(string filename)
		{
			Init(filename);
		}

		/// <summary>
		/// Constructor (used for tests) take spurious file name and pretends contents are the byte array
		/// </summary>
		public ByteReader(string filename, byte[] contents)
		{
			Init(filename, contents);
		}

		public void Rewind()
		{
			LineNumber = 0;
			m_FoundLineNumber = 0;
			m_position = 0;
			m_foundBOM = false;
			CheckforAndHandleBOM();
			m_hasLookAhead = false;
			m_LookAheadLineNumber = 0;
		}

		private void Init(string filename)
		{
			byte[] content;
			try
			{
				using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					content = new byte[reader.Length];
					reader.Read(content, 0, (int)(reader.Length));
					reader.Close();
				}
			}
			catch
			{
				content = new byte[0];
			}
			Init(filename, content);
		}

		private void Init(string filename, byte[] content)
		{
			m_FileName = filename;
			LineNumber = 0;
			m_FoundLineNumber = 0;
			m_position = 0;
			m_foundBOM = false;
			m_LookAheadLineNumber = 0;
			// save the new line and space as byte[] for future testing:
			m_NL = Converter.WideToMulti(Environment.NewLine, Encoding.ASCII);
			m_space = Converter.WideToMulti(" ", Encoding.ASCII);
			m_FileData = content;
			// read and process a BOM if present
			CheckforAndHandleBOM();
			m_EOL = GetEOLForThisFile();

		}

		/// <summary>
		/// Search the m_FileData array at the current position and see if the passed in
		/// data is an exact match for that number of bytes.
		/// </summary>
		private bool IsCurrentData(byte[] data)
		{
			for (var i = 0; i < data.Length && m_position + i < m_FileData.Length; i++)
			{
				if (m_FileData[m_position + i] != data[i])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// This will look at the start of the file for the BOM and process it
		/// if it's found.
		/// </summary>
		private void CheckforAndHandleBOM()
		{
			var utf32be = new byte[] { 0x00, 0x00, 0xfe, 0xff };
			var utf32le = new byte[] { 0xff, 0xfe, 0x00, 0x00 };
			var utf16be = new byte[] { 0xfe, 0xff };
			var utf16le = new byte[] { 0xff, 0xfe };
			var utf8 = new byte[] { 0xef, 0xbb, 0xbf };
			if (IsCurrentData(utf32le) || IsCurrentData(utf32be))
			{
				m_foundBOM = true;
				m_BOMLength = utf32be.Length;
				//m_BOMEncoding = System.Text.Encoding.???;	// not currently defined for 32
				m_position = m_BOMLength;
			}
			else if (IsCurrentData(utf8))
			{
				m_foundBOM = true;
				m_BOMLength = utf8.Length;
				m_BOMEncoding = Encoding.UTF8;
				m_position = m_BOMLength;
			}
			else if (IsCurrentData(utf16le))
			{
				m_foundBOM = true;
				m_BOMLength = utf16le.Length;
				m_BOMEncoding = Encoding.Unicode;
				m_position = m_BOMLength;
			}
			else if (IsCurrentData(utf16be))
			{
				m_foundBOM = true;
				m_BOMLength = utf16be.Length;
				m_BOMEncoding = Encoding.BigEndianUnicode;
				m_position = m_BOMLength;
			}
			if (m_foundBOM) // has one
			{
				if (m_BOMEncoding == Encoding.UTF8)
				{
					// no extra processing needed for UTF8 - this is the default format
				}
				else if (m_BOMEncoding == Encoding.Unicode || m_BOMEncoding == Encoding.BigEndianUnicode)
				{
					var utf8Encoder = new UTF8Encoding(false, true);
					try
					{
						// decode the wide Unicode byte data to wide chars
						var uniDecoder = m_BOMEncoding.GetDecoder();
						var charCount = uniDecoder.GetCharCount(m_FileData, 2, m_FileData.Length - 2);
						var chars = new char[charCount];
						uniDecoder.GetChars(m_FileData, 2, m_FileData.Length - 2, chars, 0);
						// decode the wide chars to utf8 bytes
						var newLength = utf8Encoder.GetByteCount(chars);
						m_FileData = new byte[newLength];
						utf8Encoder.GetBytes(chars, 0, chars.Length, m_FileData, 0);
						// log msg for user to see
						m_Log?.AddWarning(string.Format(SfmToXmlStrings.FileConvertedFrom0To1, m_BOMEncoding.WebName, utf8Encoder.WebName));
					}
					catch (Exception e)
					{
						if (m_Log != null)
						{
							m_Log.AddFatalError(string.Format(SfmToXmlStrings.CannotConvertFileFrom0To1, m_BOMEncoding.WebName, utf8Encoder.WebName));
							m_Log.AddFatalError(string.Format(SfmToXmlStrings.Exception0, e.Message));
						}
						m_position = 0;
						m_FileData = new byte[0];   // don't process anything
					}
				}
				else
				{
					m_position = 0;
					m_FileData = new byte[0];   // don't process anything
					m_Log?.AddFatalError(SfmToXmlStrings.CannotProcessUtf32Files);
				}
			}
		}

		/// <summary>
		/// Get the line number of the last read newline
		/// </summary>
		public int LineNumber { get; private set; }

		/// <summary>
		/// Return the last line number that the marker was found on (can be different from "LineNumber"
		/// </summary>
		public int FoundLineNumber => m_FoundLineNumber + 1;

		/// <summary>
		/// Helper method to look through an array of bytes and return true if it's found
		/// </summary>
		protected bool InArray(byte[] dataArray, byte data)
		{
			return dataArray.Any(dataByte => dataByte == data);
		}

		/// <summary>
		/// This is the public method that returns the 'next' marker and data and boolean as
		/// to the success of the operation.  This method will return the look ahead values
		/// and then fetch the next ones.  This supports the ability to look ahead a single
		/// sfm now for better planning.  (Possible enhancement would be to read the whole
		/// file in and store the information in an internal structure, but that would lead to a
		/// duplication of memory - the contents being stored twice.)  So, for now as it isn't
		/// needed, we'll just read ahead one token.
		/// </summary>
		public bool GetNextSfmMarkerAndData(out string sfmMarker, out byte[] sfmData, out byte[] badSfmData)
		{
			// on the first time through, get the 'look ahead' sfm and data too.
			// on following calls, return the look ahead and continue to find next and put in lookahead
			if (m_position == 0 || (m_foundBOM && m_position == m_BOMLength))
			{
				// get the first and next (look ahead) and return the first
				var success = ReallyGetNextSfmMarkerAndData(out sfmMarker, out sfmData, out badSfmData);
				if (!success)
				{
					return false;
				}
				var saveLineNum = m_FoundLineNumber;
				m_hasLookAhead = ReallyGetNextSfmMarkerAndData(out m_sfmLookAheadMarker, out m_sfmLookAheadData, out m_sfmLookAheadMarkerBadBytes);
				m_LookAheadLineNumber = m_FoundLineNumber;
				m_FoundLineNumber = saveLineNum;    // restore line number for current marker
				return true;
			}
			if (m_hasLookAhead && (m_position <= m_FileData.Length))
			{
				// return the look ahead and get the next one
				sfmMarker = m_sfmLookAheadMarker;
				sfmData = m_sfmLookAheadData;
				// bad bytes in the marker
				badSfmData = m_sfmLookAheadMarkerBadBytes;
				// handle possible last line where count's aren't updated
				if (m_FoundLineNumber == m_LookAheadLineNumber)
				{
					m_LookAheadLineNumber++;
				}
				m_FoundLineNumber = m_LookAheadLineNumber;
				m_hasLookAhead = ReallyGetNextSfmMarkerAndData(out m_sfmLookAheadMarker, out m_sfmLookAheadData, out m_sfmLookAheadMarkerBadBytes);
				// make sure the line number is correct
				var saveLineNum = m_FoundLineNumber;
				m_FoundLineNumber = m_LookAheadLineNumber;
				m_LookAheadLineNumber = saveLineNum;
				return true;
			}

			// there is no look ahead available, so we're done
			return ReallyGetNextSfmMarkerAndData(out sfmMarker, out sfmData, out badSfmData);
		}

		/// <summary>
		/// Worker method that returns the sfm marker as a string and the data for
		/// it as an array of bytes.
		/// </summary>
		/// <returns>false when there is no more data to process, else true</returns>
		private bool ReallyGetNextSfmMarkerAndData(out string sfmMarker, out byte[] sfmData, out byte[] badSfmBytes)
		{
			badSfmBytes = null;
			// exit condition and false return value
			if (m_position >= m_FileData.Length)
			{
				// no data to process
				sfmMarker = string.Empty;
				sfmData = new byte[0];
				return false;
			}
			var startOfMarker = (int)m_position;
			// currently positioned at a backslash byte
			var eol = new byte[] { 0x0a, 0x0d };
			if (m_FileData[m_position] == m_backSlash)
			{
				// now start to get the marker
				// // move off the backslash
				m_position++;
				// The current byte is a backslash, use all bytes until white space
				// as part of the marker
				var whitespace = new byte[] { 0x20, 0x09 };
				while (!InArray(whitespace, m_FileData[m_position]) && !InArray(eol, m_FileData[m_position]))
				{
					// not a whitespace byte, bump the count
					m_position++;
					if (m_position >= m_FileData.Length)
					{
						break;
					}
				}
				// have hit our first whitespace data or the end of the file
				var endOfMarker = (int)m_position - 1;
				// convert the bytes of the sfm marker to the string for it
				MultiToWideError mwError;
				sfmMarker = Converter.MultiToWideWithERROR(m_FileData, startOfMarker + 1, endOfMarker, Encoding.UTF8, out mwError, out badSfmBytes);
				if (m_position < m_FileData.Length)
				{
					// eat all the white space after the marker
					while (InArray(whitespace, m_FileData[m_position]))
					{
						m_position++;
						if (m_position >= m_FileData.Length)
						{
							break;
						}
					}
				}
				// m_position now points to the start of the data portion
			}
			else
			{
				sfmMarker = string.Empty;
			}
			m_FoundLineNumber = LineNumber;   // save the line for the found marker
			sfmData = GetBytesUptoNextSfmMarker();
			return true;
		}

		/// <summary>
		/// Can't assume that it's a newline sequence, it could be a cr or lf or ??
		/// Check the first XXXX bytes and see what the counts are for the characters and
		/// then determine the EOL.
		/// </summary>
		private byte[] GetEOLForThisFile()
		{
			// search for the possible end markers CR/LF
			const byte cr = 0x0d; // Carriage Return
			const byte lf = 0x0a; // Line Feed
			var crCnt = 0;
			var lfCnt = 0;
			var nlCnt = 0;
			var endPos = 1000;  // only search up to 1000 bytes
			if (endPos > m_FileData.Length)
			{
				endPos = m_FileData.Length;
			}
			for (var pos = 0; pos < endPos; pos++)
			{
				if (m_FileData[pos] == cr)
				{
					if (pos + 1 < endPos && m_FileData[pos + 1] == lf)
					{
						nlCnt++;
						pos++;
					}
					else
					{
						crCnt++;
					}
				}
				else if (m_FileData[pos] == lf)
				{
					lfCnt++;
				}
			}
			// most common case where file is cr/lf terminated
			if (nlCnt >= crCnt && nlCnt >= lfCnt)
			{
				return m_NL;
			}
			// case of cr or lf line termination
			var rval = new byte[1];
			rval[0] = cr;
			if (lfCnt > crCnt)
			{
				rval[0] = lf;
			}
			return rval;
		}
		/// <summary>
		/// All sfm markers are assumed to begin a new line, ie follow
		/// a newline, and begin with a backslash '\'.
		/// The following parsing code is based on this assumption.
		/// </summary>
		private byte[] GetBytesUptoNextSfmMarker()
		{
			// handle special case of start of file
			if (m_position == 0 || m_foundBOM && m_position == m_BOMLength)
			{
				if (m_FileData.Length == 0)
				{
					return new byte[0];     // empty, no data to process
				}
				if (m_FileData[m_position] == m_backSlash)      // '\' byte
				{
					return new byte[0];     // first byte is backslash, no data to return
				}
			}
			long foundPos;
			var loopPos = m_position;
			while (true)
			{
				// find the next newline sequence of bytes
				foundPos = Converter.FindFirstMatch(m_FileData, (int)loopPos, m_EOL);
				if (foundPos == -1 || foundPos == m_FileData.Length - m_EOL.Length)
				{
					// not found, then return everything
					foundPos = m_FileData.Length - 1;
					break;
				}
				foundPos += m_EOL.Length;
				loopPos = foundPos;
				LineNumber++; // bump the line number
				if (m_FileData[foundPos] == m_backSlash)
				{
					// found complete match, exit the loop
					foundPos--;
					break;
				}
			}
			var returnBufSize = foundPos - m_position + 1;
			var returnData = new byte[returnBufSize];
			var readPos = m_position;
			long writePos = 0;
			// Copy relevant portion of m_FileData into returnData,
			// replacing Tab, CR/LF characters (except for CR/LF at end) with spaces:
			var tab = Converter.WideToMulti("	", System.Text.Encoding.ASCII);
			while (readPos <= foundPos)
			{
				if (CopySpaceIfMatched(m_FileData, ref readPos, foundPos, ref returnData, ref writePos, m_EOL))
				{
					continue;
				}
				if (CopySpaceIfMatched(m_FileData, ref readPos, foundPos, ref returnData, ref writePos, m_NL))
				{
					continue;
				}
				if (CopySpaceIfMatched(m_FileData, ref readPos, foundPos, ref returnData, ref writePos, tab))
				{
					continue;
				}
				// Did not match any of the strings we were looking out for, so just copy data as is:
				returnData[writePos++] = m_FileData[readPos++];
			}
			m_position = foundPos + 1;  // update the member position variable
			// If we didn't replace any searched-for strings with spaces, we are good to go:
			if (returnBufSize == writePos)
			{
				return returnData;
			}
			// Create a new buffer whose size matches what we actually copied into returnData:
			var sizeAdjustedReturnData = new byte[writePos];
			for (long i = 0; i < writePos; i++)
			{
				sizeAdjustedReturnData[i] = returnData[i];
			}
			return sizeAdjustedReturnData;
		}

		/// <summary>
		/// Tests if the data bytes starting at the given position match the given search data.
		/// </summary>
		/// <param name="readData">Data bytes to examine</param>
		/// <param name="readPos">Index into readData to begin examination at</param>
		/// <param name="searchData">Bytes to compare with</param>
		/// <returns>True if readPos is an index into readData where a copy of searchData begins</returns>
		private bool BytesMatch(byte[] readData, long readPos, byte[] searchData)
		{
			// First test that there are enough bytes before the end of readData to possibly contain
			// searchData:
			if (readPos + searchData.Length > readData.Length)
			{
				return false; // Not enough space to contain searchData
			}
			// Iterate over searchData's bytes, checking that each one matches the corresponding
			// byte in readData:
			for (long i = 0; i < searchData.LongLength; i++)
			{
				if (readData[readPos + i] != searchData[i])
				{
					return false;
				}
			}
			// All bytes in searchData match:
			return true;
		}

		/// <summary>
		/// Tests the given data at the given position for a match with given search data.
		/// If there is no match, or the match is at the end of the given data, the data
		/// is copied into a separate buffer. Otherwise, a space is copied over.
		/// This effectively copies the given data to the output data, replacing matching
		/// searched-for data with a space, except at the end.
		/// </summary>
		/// <param name="readData">The source data</param>
		/// <param name="readPos">The current place in readData to start work</param>
		/// <param name="limitPos">The last place in readData to read</param>
		/// <param name="writeData">The output buffer</param>
		/// <param name="writePos">The current location to write to in the output buffer</param>
		/// <param name="searchData">the data to search for</param>
		/// <returns>True if the searchData was found (and thus something was written to writeData)</returns>
		private bool CopySpaceIfMatched(byte[] readData, ref long readPos, long limitPos, ref byte[] writeData, ref long writePos, byte[] searchData)
		{
			// Test if the byte sequence we are searching for exists in readData at the current read position:
			if (BytesMatch(readData, readPos, searchData))
			{
				// The search sequence exists in readData at the current position.
				// Test if the searched-for sequence constitutes the end of the readData:
				if (limitPos - readPos >= searchData.Length)
				{
					// We are not at the end, so we will just copy over a space, instead of the
					// readData itself:
					for (long i = 0; i < m_space.Length; i++)
					{
						writeData[writePos++] = m_space[i];
					}
					// Skip past the searchData:
					readPos += searchData.Length;
				}
				else
				{
					// We are at the end of the given readData, so the matching data does not
					// count for anything - we will copy it over raw:
					for (long i = 0; i < searchData.Length; i++)
					{
						writeData[writePos++] = readData[readPos++];
					}
				}
				return true;
			}
			// The byte sequence was not found, so return "no work done" value:
			return false;
		}
	}
}