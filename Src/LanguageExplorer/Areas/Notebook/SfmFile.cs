// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// This is like SfmReader except that it stores the data for all the lines.
	/// It also stores the Shoebox private marker lines like SfmReaderEx.
	/// </summary>
	internal sealed class SfmFile : SfmFileReader
	{
		internal SfmFile(string filename) : base(filename)
		{
		}

		protected override void Init()
		{
			try
			{
				string sfm;
				byte[] sfmData;
				byte[] badSfmData;
				var lineNum = 0;
				while (GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData))
				{
					if (sfm.Length == 0)
					{
						lineNum = LineNumber;
						continue; // no action if empty sfm - case where data before first marker
					}
					if (m_sfmUsage.ContainsKey(sfm))
					{
						var val = m_sfmUsage[sfm] + 1;
						m_sfmUsage[sfm] = val;
					}
					else
					{
						if (sfm.Length > m_longestSfmSize)
						{
							m_longestSfmSize = sfm.Length;
						}
						m_sfmUsage.Add(sfm, 1);
						m_sfmOrder.Add(sfm);
						m_sfmWithDataUsage.Add(sfm, 0); // create the key - not sure on data yet
					}
					var line = new SfmField(sfm, sfmData, lineNum);
					Lines.Add(line);
					// if there is data, then bump the sfm count with data
					if (sfmData.Length > 0 && (!String.IsNullOrEmpty(line.Data) || line.ErrorConvertingData))
					{
						var val = m_sfmWithDataUsage[sfm] + 1;
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
		internal List<SfmField> Lines { get; } = new List<SfmField>();
	}
}