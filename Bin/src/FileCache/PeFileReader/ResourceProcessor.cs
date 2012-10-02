// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ResourceProcessor.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ResourceProcessor: SubProcessorBase
	{
		private ArrayList m_TypeStack = new ArrayList();
		private long m_SectionBase;
		private long m_BaseAddress;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ResourceProcessor"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="writer">The writer.</param>
		/// ------------------------------------------------------------------------------------
		public ResourceProcessor(Stream stream, BinaryReader reader, BinaryWriter writer)
			: base(stream, reader, writer)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the resource section.
		/// </summary>
		/// <param name="nSectionBase">The section base address.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessResourceSection(long nSectionBase)
		{
			m_SectionBase = nSectionBase;
			m_BaseAddress = m_stream.Position;
			ProcessResourceDirectoryTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the resource directory table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessResourceDirectoryTable()
		{
			m_stream.Position += 4;
			OverwriteTimestamp();

			m_stream.Position += 4;
			short nNameEntries = m_reader.ReadInt16();
			short nIdEntries = m_reader.ReadInt16();

			for (int i = 0; i < nNameEntries; i++)
				ProcessDirectoryEntry(false);

			for (int i = 0; i < nIdEntries; i++)
				ProcessDirectoryEntry(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the directory entry.
		/// </summary>
		/// <param name="fId">set to <c>true</c> if its an ID entry, <c>false</c> if its a Name
		/// entry.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessDirectoryEntry(bool fId)
		{
			long nBasePos = m_stream.Position;

			int id;
			int nameRVA;
			if (fId)
			{
				id = m_reader.ReadInt32();
				m_TypeStack.Add(id);
			}
			else
			{
				nameRVA = m_reader.ReadInt32() & 0x7FFFFFFF;
				m_stream.Position = m_BaseAddress + nameRVA;

				short nStringLen = m_reader.ReadInt16();
				UTF8Encoding encoding = new UTF8Encoding();
				byte[] buffer = UnicodeEncoding.Convert(new UnicodeEncoding(),
					encoding, m_reader.ReadBytes(2 * nStringLen));
				string name = new string(encoding.GetChars(buffer));
				m_TypeStack.Add(name);

				m_stream.Position = nBasePos + 4;
			}

			int value = m_reader.ReadInt32();
			bool fSubDir = (value & 0x80000000) != 0;
			int nVirtualAddress = value & 0x7FFFFFFF;

			m_stream.Position = m_BaseAddress + nVirtualAddress;
			if (fSubDir)
				ProcessResourceDirectoryTable();
			else
				ProcessDataEntry();

			m_TypeStack.RemoveAt(m_TypeStack.Count - 1);
			m_stream.Position = nBasePos + 8;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the data entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessDataEntry()
		{
			int nVirtualAddress = m_reader.ReadInt32();
			int nSize = m_reader.ReadInt32();

			long nTypeLibAddress = m_BaseAddress + nVirtualAddress - m_SectionBase;
			m_stream.Position = nTypeLibAddress;

			object obj = m_TypeStack[0];
			if (obj is string && ((string)obj) == "TYPELIB")
			{
				// HACK: I can't find the spec for TypeLib, so we just search for
				// the typical string and zero out the date that follows, as well
				// as a few additional bytes that seem to change
				int nPos = BoyerMooreBinarySearch.IndexOf(m_reader.ReadBytes(nSize),
					"Created by MIDL version");
				if (nPos > 0)
				{
					// Overwrite time stamp that MIDL compiler generated:
					// Created by MIDL version 6.00.0366 at Thu Jul 27 11:27:58 2006\n
					m_stream.Position = nTypeLibAddress + nPos + 37;
					byte[] buffer = new byte[32]; // Thu Jul 27 11:27:58 2006\n
					m_writer.Write(buffer);
				}
			}
			else if (obj is int)
			{
				int id = (int)obj;
				if (id == 16) // Version Info
				{
					ProcessVersionInfo();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the version info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessVersionInfo()
		{
			// VS_VERSIONINFO struct
			short length = m_reader.ReadInt16();
			short valueLength = m_reader.ReadInt16();
			short type = m_reader.ReadInt16();
			// VS_VERSION_INFO = 2 * 15 bytes
			// Padding: 4 bytes
			m_stream.Position += 34;

			// VS_FIXEDFILEINFO
			uint signature = m_reader.ReadUInt32();
			if (signature != 0xFEEF04BD)
				throw new ApplicationException("Unexpected Version Info signature");

			// Structure version (4 bytes)
			m_stream.Position += 4;
			// FileVersion (8 bytes)
			m_writer.Write((long)0); // zero out
			// Product Version (8 bytes)
			m_writer.Write((long)0); // zero out

			// FileFlagsMask (4 bytes)
			// FileFlags (4 bytes)
			// FileOS (4 bytes)
			// FileType (4 bytes)
			// FileSubtype (4 bytes)
			m_stream.Position += 20;

			// FileDate (8 bytes)
			m_writer.Write((long)0); // zero out

			// Padding
			AdjustAlignment();

			// StringFileInfo or VarFileInfo
			ProcessChildFileInfo();


		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the child file info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessChildFileInfo()
		{
			// Length (4 bytes)
			// Value Length (always 0)
			// Type (4 bytes)
			m_stream.Position += 6;
			string key = ReadString();
			AdjustAlignment();

			if (key == "StringFileInfo")
				ProcessStringFileInfo();
			// Otherwise just ignore
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the string file info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessStringFileInfo()
		{
			long nBaseAddress = m_stream.Position;
			short length = m_reader.ReadInt16();
			// All kinds of stuff from beginning of StringTable structure
			m_stream.Position += 4;
			ReadString();
			AdjustAlignment();

			// Array of String structures
			while (m_stream.Position < nBaseAddress + length)
			{
				ProcessString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessString()
		{
			long nBaseAddress = m_stream.Position;
			short length = m_reader.ReadInt16();
			short valLength = m_reader.ReadInt16();
			// Type
			m_stream.Position += 2;
			string key = ReadString();

			if (valLength > 0)
			{
				AdjustAlignment();
				if (key == "FileVersion" || key == "ProductVersion")
				{
					byte[] buffer = new byte[valLength * 2]; // Unicode characters
					m_writer.Write(buffer);
				}
				else
					m_stream.Position += valLength * 2; // Unicode characters
			}
			AdjustAlignment();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a string.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string ReadString()
		{
			List<byte> buffer = new List<byte>();
			while (true)
			{
				byte b = m_reader.ReadByte();
				if (b == 0)
				{
					m_reader.ReadByte();
					break;
				}
				buffer.Add(b);
				buffer.Add(m_reader.ReadByte());
			}

			UnicodeEncoding encoding = new UnicodeEncoding();
			return encoding.GetString(buffer.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the stream position so that it is aligned on a 32-bit boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustAlignment()
		{
			if (m_stream.Position % 4 > 0)
				m_stream.Position += m_stream.Position % 4;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrites the timestamp.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OverwriteTimestamp()
		{
			//int timestamp = m_reader.ReadInt32();
			//m_stream.Position -= 4;
			m_writer.Write((int)0);
		}
	}
}
