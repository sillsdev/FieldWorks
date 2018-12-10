// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Windows.Forms;

namespace LanguageExplorer.SfmToXml
{
	public class CRC
	{
		private const uint CRC32_POLYNOMIAL = 0xEDB88320;
		private const uint CRC32_STARTVALUE = 0xFFFFFFFF;
		private static uint[] m_CRCTable;

		public CRC()
		{
			m_CRCTable = new uint[256];
			BuildCRCTable();
		}

		private static void BuildCRCTable()
		{
			for (uint i = 0; i <= 255; i++)
			{
				var crc = i;
				for (var j = 8; j > 0; j--)
				{
					if ((crc & 1) == 1)
					{
						crc = (crc >> 1) ^ CRC32_POLYNOMIAL;
					}
					else
					{
						crc >>= 1;
					}
				}
				m_CRCTable[i] = crc;
			}
		}

		public uint CalculateCRC(byte[] buffer, int count)
		{
			var crc = CRC32_STARTVALUE;
			uint index = 0;
			while (index < count)
			{
				var b = buffer[index++];
				var temp1 = (crc >> 8) & 0x00FFFFFF;
				var temp2 = m_CRCTable[((int)crc ^ b) & 0xff];
				crc = temp1 ^ temp2;
			}
			return crc ^ CRC32_STARTVALUE;
		}

		private uint CalculateCRC_N(byte[] buffer, int count, uint lastValue, bool bFirst, bool bLast)
		{
			var crc = CRC32_STARTVALUE;
			if (!bFirst)
			{
				crc = lastValue;
			}
			uint index = 0;
			while (index < count)
			{
				var b = buffer[index++];
				var temp1 = (crc >> 8) & 0x00FFFFFF;
				var temp2 = m_CRCTable[((int)crc ^ b) & 0xff];
				crc = temp1 ^ temp2;
			}

			return bLast ? crc ^ CRC32_STARTVALUE : crc;
		}

		public uint FileCRC(string fileName)
		{
			uint crc = 0;
			if (!File.Exists(fileName))
			{
				return 0;
			}
			FileStream fs = null;
			try
			{
				fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (IOException ex)
			{
				//check if message is for a File IO
				// REVIEW: is ex.Message guaranteed to contain English text even when running on a localized Windows?
				if (ex.Message.Contains("being used by another process"))
				{
					MessageBox.Show($"File {fileName}is in use.");
					return 0;
				}
			}
			try
			{
				using (var binReader = new BinaryReader(fs))
				{
					const int buffSize = 0x8000;
					// can process the file in just one pass of the crc generator
					if (fs.Length <= buffSize)
					{
						var testArray = binReader.ReadBytes((int)fs.Length);
						crc = CalculateCRC(testArray, testArray.Length);
						return crc;
					}
					long bytesRead = 0;
					var firstTime = true;
					while (bytesRead < fs.Length)
					{
						var testArray = binReader.ReadBytes(buffSize);
						bytesRead += testArray.Length;
						crc = CalculateCRC_N(testArray, testArray.Length, crc, firstTime, bytesRead == fs.Length);
						firstTime = false;
					}
				}
			}
			catch
			{
			}
			finally
			{
				fs.Dispose();
			}
			return crc;
		}
	}
}