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
// File: PeFileProcessor.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Winterdom.IO.FileMap;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows reading a PE file and ignore any timestamps and GUIDs that may change between
	/// compiles of the same source code.
	/// </summary>
	/// <remarks>We use a .NET implementation of memory mapped file available from
	/// http://www.winterdom.com/dev/dotnet/index.html </remarks>
	/// ----------------------------------------------------------------------------------------
	public class PeFileProcessor: IDisposable
	{
		#region Defines
		private readonly string[] m_directoryNames = {
			// 0			1				2
			"Export Table", "Import Table", "Resource Table",
			// 3				4					5
			"Exception Table", "Certificate Table", "Base Relocation Table",
			// 6		7				8			9			10
			"Debug", "Architecture", "Global Ptr", "TLS Table", "Load Config Table",
			// 11			12		13							14
			"Bound Import", "IAT", "Delay Import Descriptor", "CLR Runtime Header",
			// 15
			"Reserved" };

		private enum ImageDebugType
		{
			Unknown = 0,
			Coff = 1,
			Codeview = 2,
			Fpo = 3,
			Misc = 4,
			Exception = 5,
			Fixup = 6,
			OmapToSrc = 7,
			OmapFromSrc = 8,
			Borland = 9,
			Reserved10 = 10,
			Clsid = 11
		}
		#endregion

		#region Member Variables
		private bool m_fImageFile;
		private MemoryMappedFile m_mappedFile;
		private BinaryReader m_reader;
		private BinaryWriter m_writer;
		private Stream m_stream;
		private string m_fileName;
		private Hashtable m_htDirectories;
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PeFileProcessor"/> class.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// ------------------------------------------------------------------------------------
		public PeFileProcessor(string fileName)
		{
			m_htDirectories = new Hashtable();
			m_fileName = fileName;
			m_mappedFile = MemoryMappedFile.Create(fileName, MapProtection.PageWriteCopy);
			ProcessFile();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the stream.
		/// </summary>
		/// <value>The stream.</value>
		/// ------------------------------------------------------------------------------------
		public Stream Stream
		{
			get { return m_stream; }
		}
		#endregion

		#region Dispose stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:WindowsApplication1.PeFileProcessor"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~PeFileProcessor()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		/// <param name="fFromDispose">if set to <c>true</c> called from Dispose() - safe
		/// to do stuff with managed objects; if <c>false</c> called from Finalizer - managed
		/// objects might already be disposed.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fFromDispose)
		{
			if (fFromDispose)
			{
				if (m_reader != null)
					m_reader.Close();
				if (m_writer != null)
					m_writer.Close();
				if (m_stream != null)
					m_stream.Dispose();
				if (m_mappedFile != null)
					m_mappedFile.Dispose();
			}

			m_reader = null;
			m_writer = null;
			m_stream = null;
			m_mappedFile = null;
		}

		#endregion

		#region Methods to process parts of PE file
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the file
		/// </summary>
		/// <returns><c>true</c> if it's a PE file, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool ProcessFile()
		{
			FileInfo fileInfo = new FileInfo(m_fileName);
			m_stream = m_mappedFile.MapView(MapAccess.FileMapCopy, 0, (int)fileInfo.Length);
			Debug.WriteLine(string.Format("Position={0}, Length={1}",
				m_stream.Position, m_stream.Length));

			m_reader = new BinaryReader(m_stream);
			m_writer = new BinaryWriter(m_stream);

			// MS-DOS Stub (Image Only)
			m_fImageFile = ProcessMsDosStub();
			if (!m_fImageFile)
				return false;

			short nSections;
			short sizeOfOptionalHeader = ProcessCoffFileHeader(out nSections);

			long posAfterFileHeader = m_stream.Position;

			if (sizeOfOptionalHeader > 0)
				ProcessOptionalHeader();

			m_stream.Position = posAfterFileHeader + sizeOfOptionalHeader;
			ProcessSections(nSections);

			m_stream.Position = 0;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the MS-DOS Stub (Image Only).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ProcessMsDosStub()
		{
			long nMsDosStubBase = m_stream.Position;
			bool fRet = false;
			if (m_reader.ReadUInt16() == 0x5A4D)
			{
				// Image file (executable or DLL)
				Debug.WriteLine("Executable file");
				fRet = true;

				// Position 3C has the address of the Signature
				m_stream.Position = 0x3C;
				int addressOfPE = m_reader.ReadInt32();

				// Signature (Image Only)
				m_stream.Position = addressOfPE;
				if ((char)m_reader.ReadByte() != 'P' ||
					(char)m_reader.ReadByte() != 'E' ||
					m_reader.ReadByte() != 0 ||
					m_reader.ReadByte() != 0)
				{
					throw new ApplicationException("No valid PE file");
				}
			}
			else
				m_stream.Position = nMsDosStubBase;

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the COFF File Header (Object and Image).
		/// </summary>
		/// <param name="nSections">Number of sections</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private short ProcessCoffFileHeader(out short nSections)
		{
			// COFF File Header (Object and Image)
			// comes directly after signature resp. at beginning of file
			long nCoffHeaderBase = m_stream.Position;
			m_stream.Position += 2;
			nSections = m_reader.ReadInt16();

			// we want to overwrite the timestamp
			OverwriteTimestamp();

			m_stream.Position = nCoffHeaderBase + 16;
			short sizeOfOptionalHeader = m_reader.ReadInt16();
			m_stream.Position = nCoffHeaderBase + 20;
			return sizeOfOptionalHeader;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the Optional Header (usually Image only)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessOptionalHeader()
		{
			long nOptHeaderBase = m_stream.Position;
			short magicNumber = m_reader.ReadInt16();
			if (magicNumber != 0x10b && magicNumber != 0x20b)
				throw new ApplicationException("No valid magic number in Optional Header");

			// Zero out checksum
			m_stream.Position = nOptHeaderBase + 64;
			m_writer.Write((int)0);

			bool fPE32plus = (magicNumber == 0x20b);

			m_stream.Position = nOptHeaderBase + (fPE32plus ? 108 : 92);
			int nDirectories = m_reader.ReadInt32();

			for (int i = 0; i < nDirectories; i++)
			{
				m_htDirectories[m_directoryNames[i]] =
					new ImageDataDirectory(m_reader.ReadInt32(), m_reader.ReadInt32(), i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the Section Table (Section Headers).
		/// </summary>
		/// <param name="nSections">The number of sections.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessSections(short nSections)
		{
			for (short i = 0; i < nSections; i++)
			{
				long nSectionHeaderBase = m_stream.Position;

				string name = ReadName(8);
				int sizeVirtual = m_reader.ReadInt32();
				int addressVirtual = m_reader.ReadInt32();
				int sizeOfRawData = m_reader.ReadInt32();
				int addressOfRawData = m_reader.ReadInt32();

				m_stream.Position = addressOfRawData;
				ProcessSection(addressVirtual, sizeVirtual);

				m_stream.Position = nSectionHeaderBase + 40;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes one section.
		/// </summary>
		/// <param name="addressVirtual">The address when loaded.</param>
		/// <param name="sizeVirtual">The virtual size (size on disk).</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessSection(int addressVirtual, int sizeVirtual)
		{
			long nSectionBase = m_stream.Position;

			foreach (DictionaryEntry entry in m_htDirectories)
			{
				m_stream.Position = nSectionBase;
				ImageDataDirectory dir = (ImageDataDirectory)entry.Value;
				if (dir.VirtualAddress >= addressVirtual &&
					dir.VirtualAddress < addressVirtual + sizeVirtual)
				{
					m_stream.Position = dir.VirtualAddress - addressVirtual + nSectionBase;

					switch (dir.Index)
					{
						case 0: // Export Table
						case 1: // Import Table
							m_stream.Position += 4;
							OverwriteTimestamp();
							break;
						case 2: // Resource Table
							ResourceProcessor resourceProcessor = new ResourceProcessor(m_stream,
								m_reader, m_writer);
							resourceProcessor.ProcessResourceSection(dir.VirtualAddress);
							break;
						case 6: // Debug
							ProcessDebugInfo();
							break;
						case 14: // CLI Header
							CLIFileProcessor cliFileProcessor = new CLIFileProcessor(m_stream,
								m_reader, m_writer);
							cliFileProcessor.ProcessCLIHeader(nSectionBase, addressVirtual);
							break;
						default:
							break;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the debug info.
		/// </summary>
		/// <remarks>Info about debug info from
		/// http://www.debuginfo.com/articles/debuginfomatch.html</remarks>
		/// ------------------------------------------------------------------------------------
		private void ProcessDebugInfo()
		{
			m_stream.Position += 4;
			OverwriteTimestamp();
			m_stream.Position += 4;
			ImageDebugType type = (ImageDebugType)m_reader.ReadInt32();
			if (type != ImageDebugType.Codeview)
				throw new ApplicationException(
					"Can handle only CodeView debug information at the moment");

			// Size
			// Address of Raw Data
			m_stream.Position += 8;
			int nPointerToDebugInfo = m_reader.ReadInt32();

			m_stream.Position = nPointerToDebugInfo;

			string cvSignature = new string(m_reader.ReadChars(4));
			if (cvSignature == "RSDS") // PDB 7.0
			{
				// wipe out GUID
				byte[] buffer = new byte[Marshal.SizeOf(typeof(Guid))];
				m_writer.Write(buffer);

				// wipe out Age
				m_writer.Write((int)0);

				// PDB Filename
			}
			else
			{
				throw new ApplicationException(
					"Unsupported CodeView format (can handle only PDB 7.0)");
			}
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the name.
		/// </summary>
		/// <param name="nLen">The n len.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string ReadName(int nLen)
		{
			char[] chars = m_reader.ReadChars(nLen);
			int i;
			for (i = 0; i < nLen; i++)
			{
				if (chars[i] == '\0')
					break;
			}
			return new string(chars, 0, i);
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
		#endregion
	}
}
