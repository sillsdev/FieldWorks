// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BinarySettings.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Manages reading and writing a settings binary blob
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BinarySettings
	{
		private byte[] m_blob;
		private int m_readIndex;
		private int m_writeIndex;
		private int m_version;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct the binary settings from an existing settings array. This constructor
		/// is used to read the values from an existing array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BinarySettings(byte[] blob, string settingsName)
		{
			m_blob = blob;
			m_readIndex = 0;
			m_writeIndex = -1;
			ValidateCRC((uint)ReadInt());
			EatInt(); // length
			m_version = ReadInt();

			if (ReadString() != settingsName)
				throw new Exception("The settings name does not match");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct the binary settings array to prepare it for writing new settings
		/// </summary>
		/// <param name="settingsName"></param>
		/// ------------------------------------------------------------------------------------
		public BinarySettings(string settingsName)
		{
			m_blob = new byte[4096];
			m_writeIndex = 0;
			m_readIndex = -1;

			// write header information
			AddInt(123);	// dummy CRC
			AddInt(123);	// dummy length
			AddIntShort(0x0104);	// version
			AddStringShort(settingsName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the settings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Version
		{
			get { return m_version; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalize the settings after writing and return a byte array. Once finalized,
		/// no more writing is allowed.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public byte[] Finalize()
		{
			// Resize the array to the exact size
			Resize(m_writeIndex);

			// Set the size in the header
			m_writeIndex = 5;
			AddInt(m_blob.Length);

			// Calculate the crc and write it
			m_writeIndex = 0;
			AddInt((int)CalculateCRC());

			// Put the blob into a state to read
			m_writeIndex = -1;
			m_readIndex = 0;

			return m_blob;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate a crc from the settings and validate it against the given crc
		/// </summary>
		/// <param name="crcCheck"></param>
		/// ------------------------------------------------------------------------------------
		private void ValidateCRC(uint crcCheck)
		{
			uint crc = CalculateCRC();
			if (crcCheck != crc)
				throw new Exception("CRC does not match");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the CRC for the settings array
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private uint CalculateCRC()
		{
			const uint CRC32_POLYNOMIAL = 0xEDB88320;
			const uint CRC32_STARTVALUE = 0xFFFFFFFF;
			uint[] CRCTable = new uint[256];

			// build the CRC table
			uint crc;
			for (int i = 0; i <= 255 ; i++ )
			{
				crc = (uint)i;
				for (int j = 0; j < 8; j++)
				{
					if ((crc & 1) == 1)
						crc = (crc >> 1) ^ CRC32_POLYNOMIAL;
					else
						crc >>= 1;
				}
				CRCTable[i] = crc;
			}

			crc = CRC32_STARTVALUE;
			for (int i = 5; i < m_blob.Length; i++)
			{
				uint temp1 = (crc >> 8) & 0x00FFFFFF;
				uint temp2 = CRCTable[((int)crc ^ m_blob[i]) & 0xff];
				crc = temp1 ^ temp2;
			}
			crc ^= CRC32_STARTVALUE;

			return crc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an 32-bit integer to the end of the blob
		/// </summary>
		/// <param name="val">value to add</param>
		/// ------------------------------------------------------------------------------------
		public void AddInt(int val)
		{
			WriteByte(0x30);
			WriteByte(val >> 24);
			WriteByte((val >> 16) & 0xff);
			WriteByte((val >> 8) & 0xff);
			WriteByte(val & 0xff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an 16-bit integer to the end of the blob
		/// </summary>
		/// <param name="val">value to add</param>
		/// ------------------------------------------------------------------------------------
		public void AddIntShort(int val)
		{
			WriteByte(val == 0 ? 0x21 : 0x20);
			if (val != 0)
			{
				WriteByte(val >> 8);
				WriteByte(val & 0xff);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add unicode string to the end of the blob
		/// </summary>
		/// <param name="val">value to add</param>
		/// ------------------------------------------------------------------------------------
		public void AddString(string val)
		{
			// If there is no data then write an abbreviated string marker. This marker
			// occurs by itself with no data to indicate an empty string.
			if (val == string.Empty || val == null)
			{
				WriteByte (0x52);
				return;
			}

			// Write a long string marker
			WriteByte(0x50);

			// Write the string length in bytes (count the trailing 0 character)
			int length = (val.Length + 1) * 2;
			WriteByte(length >> 24);
			WriteByte((length >> 16) & 0xff);
			WriteByte((length >> 8) & 0xff);
			WriteByte(length & 0xff);

			// write out the characters (low byte - high byte)
			foreach (char ch in val)
			{
				WriteByte((int)ch & 0xff);
				WriteByte((int)ch >> 8);
			}

			// Add a trailing 0 character (2 bytes)
			WriteByte(0);
			WriteByte(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add ASCII string to the end of the blob
		/// </summary>
		/// <param name="val">value to add</param>
		/// ------------------------------------------------------------------------------------
		public void AddStringShort(string val)
		{
			// Write a short string marker
			WriteByte(0x60);

			// Write the string length (count the trailing 0 byte)
			int length = val.Length + 1;
			WriteByte(length >> 24);
			WriteByte((length >> 16) & 0xff);
			WriteByte((length >> 8) & 0xff);
			WriteByte(length & 0xff);

			// write out the characters
			foreach (char ch in val)
				WriteByte((int) ch);

			// Add a trailing 0 byte
			WriteByte(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a byte to the settings array
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteByte(int val)
		{
			if (m_writeIndex < 0)
				throw new Exception("Can not write while reading.");

			// Make sure that there is space to hold the new byte
			if (m_writeIndex == m_blob.Length)
				Resize(m_blob.Length + 1024);

			// Add the byte to the array
			m_blob[m_writeIndex++] = (byte)val;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resize the blob array
		/// </summary>
		/// <param name="newSize"></param>
		/// ------------------------------------------------------------------------------------
		private void Resize(int newSize)
		{
			// Don't allow the resize to throw away valid data
			if (m_writeIndex > newSize)
				newSize = m_writeIndex;

			byte[] tempBlob = new byte[newSize];
			Array.Copy(m_blob, 0, tempBlob, 0, m_writeIndex);
			m_blob = tempBlob;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the next byte from the settings
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private byte ReadByte()
		{
			if (m_readIndex >= m_blob.Length)
				throw new Exception("Read past the end of the settings");
			if (m_readIndex < 0)
				throw new Exception("Can not read settings while writing.");
			return m_blob[m_readIndex++];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a 4-byte 32-bit integer value from the data
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int Read32BitInt()
		{
			return ((int)ReadByte() << 24) +
				((int)ReadByte() << 16) +
				((int)ReadByte() << 8) +
				(int)ReadByte();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a 2-byte 16-bit integer value from the data
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int Read16BitInt()
		{
			return ((int)ReadByte() << 8) +	(int)ReadByte();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a 2-byte 16-bit character value from the data (little-endian)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int Read16BitChar()
		{
			return (int)ReadByte() + ((int)ReadByte() << 8);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read an int value from the settings
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ReadInt()
		{
			byte typeByte = ReadByte();
			switch (typeByte)
			{
				case 0x20:	// short int (2 bytes)
					return Read16BitInt();

				case 0x21: // int value of zero
					return 0;

				case 0x30:	// long int (4 bytes)
					return Read32BitInt();
			}
			throw new Exception("Unexpected type for int");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read an int value from the settings and throw it away
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EatInt()
		{
			ReadInt();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a string value from the settings
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ReadString()
		{
			StringBuilder builder = new StringBuilder();
			int length;
			int stringType;
			switch (stringType = ReadByte())
			{
				case 0x50: // unicode string
					length = Read32BitInt() / 2;
					for (int i = 0; i < length - 1; i++)
						builder.Append((char)Read16BitChar());
					Read16BitInt(); // read the trailing 0 character
					return builder.ToString();

				case 0x52: // empty string
					return null;

				case 0x60: // 8-bit char string
					length = Read32BitInt();
					for (int i = 0; i < length - 1; i++)
						builder.Append((char)ReadByte());
					ReadByte(); // read the trailing 0 character
					return builder.ToString();
			}
			throw new Exception("Unexpected type for string");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a string value from the settings and throw it away
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EatString()
		{
			ReadString();
		}
	}
}
