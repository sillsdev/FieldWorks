using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Sfm2Xml
{
	/// <summary>
	/// Replacement reader class for byte reading.
	/// </summary>
	public /*internal*/ class ByteReader
	{
		private int m_LineNumber;		// current line number in the file
		private int m_FoundLineNumber;	// line number when the marker was found
		private byte[] m_NL;		// bytes for newline
		private byte[] m_EOL;		// end of line for this file
		private const byte m_backSlash = 0x5c;
		protected byte[] m_FileData;
		protected string m_FileName;	// name of the file
		public string FileName { get { return m_FileName; } }
		private long m_position;	// current position into the file data (m_FileData)
		private string m_sfmLookAheadMarker;
		private byte[] m_sfmLookAheadMarkerBadBytes = null;	// null or array of bytes that are invalid for the marker
		private byte[] m_sfmLookAheadData;
		private bool m_hasLookAhead;		// true if the GetSfmData return true;
		private int m_LookAheadLineNumber;	// line number of look ahead sfm

		// BOM related data
		private System.Text.Encoding m_BOMEncoding;
		private int m_BOMLength;
		private bool m_foundBOM;
		private ClsLog m_Log = null;

		/// <summary>
		/// Constructor requires a file name
		/// </summary>
		/// <param name="filename"></param>
		public ByteReader(string filename, ref ClsLog Log)
		{
			m_Log = Log;
			Init(filename);
		}

		/// <summary>
		/// Constructor requires a file name
		/// </summary>
		/// <param name="filename"></param>
		public ByteReader(string filename)
		{
			Init(filename);
		}

		public void Rewind()
		{
			m_LineNumber = 0;
			m_FoundLineNumber = 0;
			m_position = 0;
			m_foundBOM = false;
			CheckforAndHandleBOM();
			m_hasLookAhead = false;
			m_LookAheadLineNumber = 0;
		}

		private void Init(string filename)
		{
			m_FileName = filename;
			m_LineNumber = 0;
			m_FoundLineNumber = 0;
			m_position = 0;
			m_foundBOM = false;
			m_LookAheadLineNumber = 0;

			// save the new line as a byte[] for future testing
			m_NL = Converter.WideToMulti(System.Environment.NewLine, System.Text.Encoding.ASCII);

			try
			{
				using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					m_FileData = new byte[reader.Length];
					reader.Read(m_FileData, 0, (int)(reader.Length));
					reader.Close();
				}
			}
			catch
			{
				m_FileData = new byte[0];
			}

			// read and process a BOM if present
			CheckforAndHandleBOM();
			m_EOL = GetEOLForThisFile();

		}

		/// <summary>
		/// Search the m_FileData array at the current position and see if the passed in
		/// data is an exact match for that number of bytes.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool IsCurrentData(byte[] data)
		{
			for (int i=0; i<data.Length && m_position+i<m_FileData.Length; i++)
			{
				if (m_FileData[m_position+i] != data[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// This will look at the start of the file for the BOM and process it
		/// if it's found.
		/// </summary>
		private void CheckforAndHandleBOM()
		{
			byte[] utf32be = new byte[] { 0x00, 0x00, 0xfe, 0xff };
			byte[] utf32le = new byte[] { 0xff, 0xfe, 0x00, 0x00 };
			byte[] utf16be = new byte[] { 0xfe, 0xff };
			byte[] utf16le = new byte[] { 0xff, 0xfe };
			byte[] utf8    = new byte[] { 0xef, 0xbb, 0xbf };

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
				m_BOMEncoding = System.Text.Encoding.UTF8;
				m_position = m_BOMLength;
			}
			else if (IsCurrentData(utf16le))
			{
				m_foundBOM = true;
				m_BOMLength = utf16le.Length;
				m_BOMEncoding = System.Text.Encoding.Unicode;
				m_position = m_BOMLength;
			}
			else if (IsCurrentData(utf16be))
			{
				m_foundBOM = true;
				m_BOMLength = utf16be.Length;
				m_BOMEncoding = System.Text.Encoding.BigEndianUnicode;
				m_position = m_BOMLength;
			}

			if (m_foundBOM)	// has one
			{
				if (m_BOMEncoding == System.Text.Encoding.UTF8)
				{
					// no extra processing needed for UTF8 - this is the default format
				}
				else if (m_BOMEncoding == System.Text.Encoding.Unicode ||
					m_BOMEncoding == System.Text.Encoding.BigEndianUnicode)
				{
					System.Text.UTF8Encoding utf8Encoder = new System.Text.UTF8Encoding(false, true);
					try
					{
						// decode the wide Unicode byte data to wide chars
						System.Text.Decoder uniDecoder = m_BOMEncoding.GetDecoder();
						int charCount = uniDecoder.GetCharCount(m_FileData, 2, m_FileData.Length-2);
						char[] chars = new Char[charCount];
						uniDecoder.GetChars(m_FileData, 2, m_FileData.Length-2, chars, 0);

						// decode the wide chars to utf8 bytes
						int newLength = utf8Encoder.GetByteCount(chars);
						m_FileData = new byte[newLength];
						utf8Encoder.GetBytes(chars, 0, chars.Length, m_FileData, 0);

						// log msg for user to see
						if (m_Log != null)
							m_Log.AddWarning(String.Format(Sfm2XmlStrings.FileConvertedFrom0To1,
								m_BOMEncoding.WebName, utf8Encoder.WebName));
					}
					catch (System.Exception e)
					{
						if (m_Log != null)
						{
							m_Log.AddFatalError(String.Format(Sfm2XmlStrings.CannotConvertFileFrom0To1,
								m_BOMEncoding.WebName, utf8Encoder.WebName));
							m_Log.AddFatalError(String.Format(Sfm2XmlStrings.Exception0, e.Message));
						}
						m_position = 0;
						m_FileData = new byte[0];	// don't process anything
					}
				}
				else
				{
					m_position = 0;
					m_FileData = new byte[0];	// don't process anything
					if (m_Log != null)
						m_Log.AddFatalError(Sfm2XmlStrings.CannotProcessUtf32Files);
				}
			}
		}

		/// <summary>
		/// Get the line number of the last read newline
		/// </summary>
		public int LineNumber
		{
			get { return m_LineNumber; }
		}

		/// <summary>
		/// Return the last line number that the marker was found on (can be different from "LineNumber"
		/// </summary>
		public int FoundLineNumber { get { return m_FoundLineNumber+1;}}


		/// <summary>
		/// Helper method to look through an array of bytes and return true if it's found
		/// </summary>
		/// <param name="dataArray"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		protected bool InArray(byte[] dataArray, byte data)
		{
			for (int i=0; i<dataArray.Length; i++)
			{
				if (dataArray[i] == data)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Return the next sfm marker and its data for view.
		/// </summary>
		/// <param name="sfmMarker"></param>
		/// <param name="sfmData"></param>
		/// <returns></returns>
		public bool GetLookAheadSfmMarkerAndData(out string sfmMarker, out byte[] sfmData, out byte[] sfmBadBytes)
		{
			sfmMarker = m_sfmLookAheadMarker;
			sfmData = m_sfmLookAheadData;
			sfmBadBytes = m_sfmLookAheadMarkerBadBytes;
			return m_hasLookAhead;
		}

		/// <summary>
		/// This is the public method that returns the 'next' marker and data and boolean as
		/// to the success of the operation.  This method will return the look ahead values
		/// and then fetch the next ones.  This supports the ability to look ahead a single
		/// sfm now for beter planning.  (Possible enhancement would be to read the whole
		/// file in and store the infor in an internal structure, but that would lead to a
		/// duplication of memory - the contents being stored twice.)  So, for now as it isn't
		/// needed, we'll just read ahead one token.
		/// </summary>
		/// <param name="sfmMarker"></param>
		/// <param name="sfmData"></param>
		/// <returns></returns>
		public bool GetNextSfmMarkerAndData(out string sfmMarker, out byte[] sfmData, out byte[] badSfmData)
		{
			// on the first time through, get the 'look ahead' sfm and data too.
			// on following calls, return the look ahead and continue to find next and put in lookahead
			if (m_position == 0 || (m_foundBOM && m_position == m_BOMLength))
			{
				// get the first and next (look ahead) and return the first
				bool success = getNextSfmMarkerAndData(out sfmMarker, out sfmData, out badSfmData);
				if (!success)
					return false;
				int saveLineNum = m_FoundLineNumber;
				m_hasLookAhead = getNextSfmMarkerAndData(out m_sfmLookAheadMarker, out m_sfmLookAheadData, out m_sfmLookAheadMarkerBadBytes);
				m_LookAheadLineNumber = m_FoundLineNumber;
				m_FoundLineNumber = saveLineNum;	// restore line number for current marker
				return true;
			}

			if (m_hasLookAhead && (m_position <= m_FileData.Length))
			{
				// return the look ahead and get the next one
				sfmMarker = m_sfmLookAheadMarker;
				sfmData = m_sfmLookAheadData;
				badSfmData = m_sfmLookAheadMarkerBadBytes;	// bad bytes in the marker
				// handle possible last line where count's aren't updated
				if (m_FoundLineNumber == m_LookAheadLineNumber)
					m_LookAheadLineNumber++;
				m_FoundLineNumber = m_LookAheadLineNumber;
				m_hasLookAhead = getNextSfmMarkerAndData(out m_sfmLookAheadMarker, out m_sfmLookAheadData, out m_sfmLookAheadMarkerBadBytes);

				// make sure the line number is correct
				int saveLineNum = m_FoundLineNumber;
				m_FoundLineNumber = m_LookAheadLineNumber;
				m_LookAheadLineNumber = saveLineNum;
				return true;
			}

			// there is no look ahead available, so we're done
			return getNextSfmMarkerAndData(out sfmMarker, out sfmData, out badSfmData);
		}

		/// <summary>
		/// Worker method that returns the sfm marker as a string and the data for
		/// it as an array of bytes.
		/// </summary>
		/// <param name="sfmMarker"></param>
		/// <param name="sfmData"></param>
		/// <returns>false when there is no more data to process, else true</returns>
		private bool getNextSfmMarkerAndData(out string sfmMarker, out byte[] sfmData, out byte[] badSfmBytes)
		{
			badSfmBytes = null;
			// exit condition and false return value
			if (m_position >= m_FileData.Length)
			{
				// no data to process
				sfmMarker = "";
				sfmData = new byte[0];
				return false;
			}

			int startOfMarker = (int)m_position;
			int endOfMarker;

			// currently positioned at a backslash byte
			if (m_FileData[m_position] == m_backSlash)
			{
				// now start to get the marker
				m_position++;	// move off the backslash

				// The current byte is a backslash, use all bytes until white space
				// as part of the marker
				byte[] whitespace = new byte[] {0x20, 0x09};	// , 0x0a, 0x0d};
				byte[] eol = new byte[] {0x0a, 0x0d};

				while (!InArray(whitespace, m_FileData[m_position]) && !InArray(eol, m_FileData[m_position]))
				{
					// not a whitespace byte, bump the count
					m_position++;
					if (m_position >= m_FileData.Length)
						break;
				}
				// have hit our first whitespace data or the end of the file
				endOfMarker = (int)m_position-1;	// save the end position of the marker

				// convert the bytes of the sfm marker to the string for it
				Converter.MultiToWideError mwError;
				sfmMarker = Converter.MultiToWideWithERROR(m_FileData, startOfMarker+1, endOfMarker,
					System.Text.Encoding.UTF8, out mwError, out badSfmBytes);

				if (m_position < m_FileData.Length)
				{
					// eat all the white space after the marker
					while (InArray(whitespace, m_FileData[m_position]))
					{
						m_position++;
						if (m_position >= m_FileData.Length)
							break;
					}
				}
				// m_position now points to the start of the data portion
			}
			else
				sfmMarker = "";

			m_FoundLineNumber = m_LineNumber;	// save the line for the found marker

			sfmData = GetBytesUptoNextSfmMarker();

			return true;
		}

		/// <summary>
		/// Can't assume that it's a newline sequence, it could be a cr or lf or ??
		/// Check the first XXXX bytes and see what the counts are for the characters and
		/// then determine the EOL.
		/// </summary>
		/// <returns></returns>
		private byte[] GetEOLForThisFile()
		{
			// search for the possible end markers CR/LF
			byte cr = 0x0d;		// Carriage Return
			byte lf = 0x0a;		// Line Feed
			int crCnt = 0;
			int lfCnt = 0;
			int nlCnt = 0;
			int endPos = 1000;	// only search upto 1000 bytes
			if (endPos > m_FileData.Length)
				endPos = m_FileData.Length;

			for (int pos = 0; pos < endPos; pos++)
			{
				if (m_FileData[pos] == cr)
				{
					if (pos+1 < endPos && m_FileData[pos+1] == lf)
					{
						nlCnt++;
						pos++;
					}
					else
						crCnt++;
				}
				else if (m_FileData[pos] == lf)
				{
					lfCnt++;
				}
			}
			// most common case where file is cr/lf terminated
			if (nlCnt >= crCnt && nlCnt >= lfCnt)
				return m_NL;

			// case of cr or lf line termination
			byte[] rval = new byte[1];
			rval[0] = cr;
			if (lfCnt > crCnt)
				rval[0] = lf;
			return rval;
		}
		/// <summary>
		/// Assumption ************************************************
		/// All sfm markers are assumed to begin a new line, ie follow
		/// a newline, and begin with a backslash '\'.
		/// The following parsing code is based on this assumption.
		/// Assumption ************************************************
		/// </summary>
		/// <returns></returns>
		private byte[] GetBytesUptoNextSfmMarker()
		{
			// handle special case of start of file
			if (m_position == 0 || (m_foundBOM && m_position == m_BOMLength))
			{
				if (m_FileData.Length == 0)
					return new byte[0];		// empty, no data to process

				if (m_FileData[m_position] == m_backSlash)		// '\' byte
					return new byte[0];		// first byte is backslash, no data to return
			}

			long foundPos;
			long loopPos = m_position;
			while (true)
			{
				// find the next newline sequence of bytes
				foundPos = Converter.FindFirstMatch(m_FileData, (int)loopPos, m_EOL);	// m_NL);
				if (foundPos == -1 ||			// not found
					foundPos == m_FileData.Length-m_EOL.Length)	// hit end of file, following newline data
				{
					// not found, then return everything
					foundPos = m_FileData.Length-1;
					break;
				}

				foundPos += m_EOL.Length;
				loopPos = foundPos;
				m_LineNumber++;	// bump the line number
				if (m_FileData[foundPos] == m_backSlash)
				{
					// found complete match, exit the loop
					foundPos--;
					break;
				}
			}

			byte[] returnData = new byte[foundPos-m_position+1];
			for (long i = 0; i <= foundPos-m_position; i++)
				returnData[i] = m_FileData[m_position + i];

			m_position = foundPos+1;	// update the member position variable
			return returnData;
		}
	}

	/// <summary>
	/// This class if for checking to see if the passed in file name is a valid sfm file.
	/// </summary>
	public class IsSfmFile : ByteReader
	{
		private bool m_IsValid = false;
		public IsSfmFile(string filename) : base(filename)
		{
			try
			{
				string sfm;
				byte[] sfmData;
				byte[] badSfmData;
				bool readData = GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData);
				// The test makes sure there is data and that the first non white space is the escape char (//)
				if (readData)
				{
					if (sfm != null && sfm.Length > 0)	// first thing in file is marker
						m_IsValid = true;
					else
					{
						if (sfmData != null)	// data is found before first marker
						{
							// make sure the data is only whitespace as defined below
							byte[] whitespace = new byte[] { 0x20, 0x09, 0x0a, 0x0d };
							m_IsValid = true;
							foreach (byte b in sfmData)
							{
								if (!InArray(whitespace, b))
								{
									m_IsValid = false;	// non-whitespace data - done looking
									break;
								}
							}
						}
					}
				}
			}
			catch
			{
				// If we got an exception in the reading of the file - it's not valid.
				m_IsValid = false;
			}
		}
		public bool IsValid { get { return m_IsValid; }}
	}

	public class SfmFileReader : ByteReader
	{
		protected int m_longestSfmSize = 0;	// number of bytes in the longes sfm
		public int LongestSfm { get { return m_longestSfmSize; } }

		//protected Hashtable m_sfmUsage;
		protected Dictionary<string, int> m_sfmUsage;			// count of all usage of a sfm
		protected Dictionary<string, int> m_sfmWithDataUsage;	// count of sfms with data
		//protected ArrayList m_sfmOrder;
		protected List<string> m_sfmOrder;
		public SfmFileReader(string filename) : base(filename)
		{
			m_sfmUsage = new Dictionary<string, int>();	// Hashtable();
			m_sfmWithDataUsage = new Dictionary<string, int>();
			m_sfmOrder = new List<string>();	// ArrayList();

			Init();

		}

		protected virtual void Init()
		{
			try
			{
				string sfm;
				byte[] sfmData;
				byte[] badSfmData;
				while (GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData))
				{
					if (sfm.Length == 0)
						continue; // no action if empty sfm - case where data before first marker
					if (m_sfmUsage.ContainsKey(sfm))
					{
						int val = m_sfmUsage[sfm] + 1;
						m_sfmUsage[sfm] = val;
					}
					else
					{
						// LT-1926 Ignore all markers that start with underscore (shoebox markers)
						if (sfm.StartsWith("_"))
							continue;

						if (sfm.Length > m_longestSfmSize)
							m_longestSfmSize = sfm.Length;

						m_sfmUsage.Add(sfm, 1);
						m_sfmOrder.Add(sfm);
						m_sfmWithDataUsage.Add(sfm, 0);	// create the key - not sure on data yet
					}
					// if there is data, then bump the sfm count with data
					if (HasDataAfterRemovingWhiteSpace(sfmData))
					{
						int val = m_sfmWithDataUsage[sfm] + 1;
						m_sfmWithDataUsage[sfm] = val;
					}
				}
			}
			catch
			{
				// just eat the exception sense the data members will be empty
			}
		}

		private bool HasDataAfterRemovingWhiteSpace(byte[] data)
		{
			byte[] whitespace = new byte[] {0x20, 0x09, 0x0a, 0x0d};

			for (int i = 0; i < data.Length; i++)
			{
				int j;
				for (j = 0; j < whitespace.Length; j++)
				{
					if (data[i] == whitespace[j])
						break;	// found white space char, check the next one
				}
				if (j == whitespace.Length)
					return true;
			}
			return false;
		}

		public int GetSFMWithDataCount(string sfm)
		{
			if (m_sfmWithDataUsage.ContainsKey(sfm))
				return m_sfmWithDataUsage[sfm];
			return 0;
		}

		public int GetSFMCount(string sfm)
		{
			if (m_sfmUsage.ContainsKey(sfm))
				return m_sfmUsage[sfm];
			return 0;
		}

		public int GetSFMOrder(string sfm)
		{
			if (m_sfmOrder.Contains(sfm))
				return m_sfmOrder.IndexOf(sfm)+1;
			return -1;
		}

		public int Count
		{
			get { return m_sfmUsage.Count; }
		}

		public ICollection SfmInfo
		{
			get { return m_sfmUsage.Keys; }
		}

		public bool ContainsSfm(string sfm)
		{
			return m_sfmUsage.ContainsKey(sfm);
		}
	}

	public struct FollowedByInfo
	{
		string firstSFM;
		string lastSFM;
		int count;
		public FollowedByInfo(string a, string b) { firstSFM = a; lastSFM = b; count = 1; }
		public int Count { get { return count; } }
		public void IncCount() { count++; }
		public string First { get { return firstSFM; } }
		public string Last { get { return lastSFM; } }
		public override string ToString()
		{
			return firstSFM + "-"+lastSFM+"-"+count.ToString();
		}
	}

	public class SfmFileReaderEx : SfmFileReader
	{
		int[] m_byteCount;		// array of counts for byte values 0-255
		private Dictionary<string, Dictionary<string, int>> m_sfmFollowedBy;// tree type view of data
		private Dictionary<string, FollowedByInfo> m_followedByInfo;		// flat view of data

		public Dictionary<string, Dictionary<string, int>> GetFollowedByInfo() { return m_sfmFollowedBy; }

		public SfmFileReaderEx(string filename)	: base(filename)
		{
			m_byteCount = new int[256];
			CountBytes();
		}

		/// <summary>
		/// property that returns an array 0-255 with counts for each occurance
		/// </summary>
		public int[] GetByteCounts
		{
			get { return m_byteCount; }
		}

		/// <summary>
		/// read the internal file data and save the byte counts
		/// </summary>
		private void CountBytes()
		{
			for (int i = 0; i < m_FileData.Length; i++)
			{
				byte b = m_FileData[i];
				m_byteCount[b]++;	// bump the count at this byte index
			}
		}

		private string BuildKey(string first, string last)
		{
			return first + "-" + last;
		}

		public ICollection GetFollowedByInfoValues
		{
			get { return m_followedByInfo.Values; }
		}

		/// <summary>
		/// Called by the base class in it's constructor, this is the method that
		/// reads the contents and gathers the sfm information.
		/// </summary>
		protected override void Init()
		{
			m_sfmFollowedBy = new Dictionary<string, Dictionary<string, int>>();
			m_followedByInfo = new Dictionary<string,FollowedByInfo>();
			try
			{
				string sfm;
				string sfmLast = null;
				byte[] sfmData;
				byte[] badSfmData;
				while (GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData))
				{
					if (sfm.Length == 0)
						continue; // no action if empty sfm - case where data before first marker
					if (m_sfmUsage.ContainsKey(sfm))
					{
						int val = m_sfmUsage[sfm] + 1;
						m_sfmUsage[sfm] = val;
					}
					else
					{
						if (sfm.Length > m_longestSfmSize)
							m_longestSfmSize = sfm.Length;
						//// LT-1926 Ignore all markers that start with underscore (shoebox markers)
						//if (sfm.StartsWith("_"))
						//    continue;
						m_sfmUsage.Add(sfm, 1);
						m_sfmOrder.Add(sfm);
					}

					// handle the marker and following counts
					int count;
					if (sfmLast != null)
					{
						Dictionary<string, int> markerHash;
						if (m_sfmFollowedBy.TryGetValue(sfmLast, out markerHash))
						{
							if (markerHash.TryGetValue(sfm, out count))
							{
								count++;
								markerHash[sfm] = count;
							}
							else
							{
								markerHash[sfm] = 1;
							}
						}
						else
						{
							markerHash = new Dictionary<string, int>();
							markerHash[sfm] = 1;
							m_sfmFollowedBy[sfmLast] = markerHash;
						}

						// new logic with List container
						string key = BuildKey(sfmLast, sfm);
						FollowedByInfo fbi;
						if (m_followedByInfo.TryGetValue(key, out fbi))
						{
							fbi.IncCount();
							m_followedByInfo[key] = fbi;
						}
						else
						{
							m_followedByInfo[key] = new FollowedByInfo(sfmLast, sfm);
						}

					}
					sfmLast = sfm;
				}
			}
			catch
			{
				// just eat the exception sense the data members will be empty
			}
		}
	}

	/// <summary>
	/// This class represents one field (which may be one line or multiple lines) from a
	/// standard format file.
	/// </summary>
	public class SfmField
	{
		private string m_sMkr;
		private byte[] m_rgbData;
		private string m_sData;
		private Sfm2Xml.Converter.MultiToWideError m_mwError;
		private byte[] m_badBytes;
		private int m_lineNumber;

		public SfmField(string sMkr, byte[] rgbData, int lineNum)
		{
			m_sMkr = sMkr;
			m_rgbData = rgbData;
			string sData = Sfm2Xml.Converter.MultiToWideWithERROR(rgbData, 0,
				rgbData.Length - 1, System.Text.Encoding.UTF8, out m_mwError, out m_badBytes);
			m_sData = sData.Trim();
			m_lineNumber = lineNum;
		}

		public string Marker
		{
			get { return m_sMkr; }
		}

		public string Data
		{
			get { return m_sData; }
			set
			{
				m_sData = value;
				m_mwError = Converter.MultiToWideError.None;
			}
		}

		public byte[] RawData
		{
			get { return m_rgbData; }
		}

		public bool ErrorConvertingData
		{
			get { return m_mwError != Converter.MultiToWideError.None; }
		}

		public int LineNumber
		{
			get { return m_lineNumber; }
		}
	}

	/// <summary>
	/// This is like SfmReader except that it stores the data for all the lines.
	/// It also stores the Shoebox private marker lines like SfmReaderEx.
	/// </summary>
	public class SfmFile : SfmFileReader
	{
		List<SfmField> m_rgFields = new List<SfmField>();

		public SfmFile(string filename) : base(filename)
		{
		}

		protected override void Init()
		{
			try
			{
				string sfm;
				byte[] sfmData;
				byte[] badSfmData;
				int lineNum = 0;
				while (GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData))
				{
					if (sfm.Length == 0)
					{
						lineNum = LineNumber;
						continue; // no action if empty sfm - case where data before first marker
					}
					if (m_sfmUsage.ContainsKey(sfm))
					{
						int val = m_sfmUsage[sfm] + 1;
						m_sfmUsage[sfm] = val;
					}
					else
					{
						if (sfm.Length > m_longestSfmSize)
							m_longestSfmSize = sfm.Length;

						m_sfmUsage.Add(sfm, 1);
						m_sfmOrder.Add(sfm);
						m_sfmWithDataUsage.Add(sfm, 0);	// create the key - not sure on data yet
					}
					SfmField line = new SfmField(sfm, sfmData, lineNum);
					m_rgFields.Add(line);
					// if there is data, then bump the sfm count with data
					if (sfmData.Length > 0 && (!String.IsNullOrEmpty(line.Data) || line.ErrorConvertingData))
					{
						int val = m_sfmWithDataUsage[sfm] + 1;
						m_sfmWithDataUsage[sfm] = val;
					}
					lineNum = LineNumber;
				}
			}
			catch
			{
				// just eat the exception since the data members will be empty
			}
		}

		/// <summary>
		/// These are actually the consecutive fields from the file, but Lines better
		/// communicates the iterative, exhaustive nature of the returned data.
		/// </summary>
		public List<SfmField> Lines
		{
			get { return m_rgFields; }
		}
	}
}
