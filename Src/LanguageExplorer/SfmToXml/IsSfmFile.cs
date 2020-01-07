// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class if for checking to see if the passed in file name is a valid sfm file.
	/// </summary>
	internal sealed class IsSfmFile : ByteReader
	{
		internal IsSfmFile(string filename)
			: base(filename)
		{
			try
			{
				string sfm;
				byte[] sfmData;
				byte[] badSfmData;
				var readData = GetNextSfmMarkerAndData(out sfm, out sfmData, out badSfmData);
				// The test makes sure there is data and that the first non white space is the escape char (//)
				if (readData)
				{
					if (!string.IsNullOrEmpty(sfm))	// first thing in file is marker
					{
						IsValid = true;
					}
					else
					{
						if (sfmData != null)	// data is found before first marker
						{
							// make sure the data is only whitespace as defined below
							var whitespace = new byte[] { 0x20, 0x09, 0x0a, 0x0d };
							IsValid = true;
							foreach (var b in sfmData)
							{
								if (!InArray(whitespace, b))
								{
									IsValid = false;	// non-whitespace data - done looking
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
				IsValid = false;
			}
		}

		internal bool IsValid { get; }
	}
}