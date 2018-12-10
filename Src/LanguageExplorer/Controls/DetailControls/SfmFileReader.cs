// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Controls.DetailControls
{
	public class SfmFileReader : ByteReader
	{
		protected int m_longestSfmSize;	// number of bytes in the longest sfm
		public int LongestSfm => m_longestSfmSize;
		protected Dictionary<string, int> m_sfmUsage;			// count of all usage of a sfm
		protected Dictionary<string, int> m_sfmWithDataUsage;	// count of sfms with data
		protected List<string> m_sfmOrder;
		public SfmFileReader(string filename) : base(filename)
		{
			m_sfmUsage = new Dictionary<string, int>();
			m_sfmWithDataUsage = new Dictionary<string, int>();
			m_sfmOrder = new List<string>();

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
					{
						continue; // no action if empty sfm - case where data before first marker
					}
					if (m_sfmUsage.ContainsKey(sfm))
					{
						var val = m_sfmUsage[sfm] + 1;
						m_sfmUsage[sfm] = val;
					}
					else
					{
						// LT-1926 Ignore all markers that start with underscore (shoebox markers)
						if (sfm.StartsWith("_"))
						{
							continue;
						}
						if (sfm.Length > m_longestSfmSize)
						{
							m_longestSfmSize = sfm.Length;
						}
						m_sfmUsage.Add(sfm, 1);
						m_sfmOrder.Add(sfm);
						m_sfmWithDataUsage.Add(sfm, 0);	// create the key - not sure on data yet
					}
					// if there is data, then bump the sfm count with data
					if (HasDataAfterRemovingWhiteSpace(sfmData))
					{
						var val = m_sfmWithDataUsage[sfm] + 1;
						m_sfmWithDataUsage[sfm] = val;
					}
				}
			}
			catch
			{
				// just eat the exception since the data members will be empty
			}
		}

		private static bool HasDataAfterRemovingWhiteSpace(byte[] data)
		{
			var whitespace = new byte[] {0x20, 0x09, 0x0a, 0x0d};
			foreach (var dataByte in data)
			{
				int j;
				for (j = 0; j < whitespace.Length; j++)
				{
					if (dataByte == whitespace[j])
					{
						break;	// found white space char, check the next one
					}
				}
				if (j == whitespace.Length)
				{
					return true;
				}
			}
			return false;
		}

		public int GetSFMWithDataCount(string sfm)
		{
			return m_sfmWithDataUsage.ContainsKey(sfm) ? m_sfmWithDataUsage[sfm] : 0;
		}

		public int GetSFMCount(string sfm)
		{
			return m_sfmUsage.ContainsKey(sfm) ? m_sfmUsage[sfm] : 0;
		}

		public int GetSFMOrder(string sfm)
		{
			return m_sfmOrder.Contains(sfm) ? m_sfmOrder.IndexOf(sfm) + 1 : -1;
		}

		public int Count => m_sfmUsage.Count;

		public ICollection SfmInfo => m_sfmUsage.Keys;

		public bool ContainsSfm(string sfm)
		{
			return m_sfmUsage.ContainsKey(sfm);
		}
	}
}