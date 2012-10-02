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
// File: CLIFileProcessor.cs
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

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class to process CLI part of PE file.
	/// </summary>
	/// <remarks>For documentation about CLI part of PE file see ECMA-335 Partition II.</remarks>
	/// ----------------------------------------------------------------------------------------
	internal class CLIFileProcessor: SubProcessorBase
	{
		#region Defines
		private class StreamInfo
		{
			public int Offset;
			public int Size;
			public string Name;
		}

		private object[] m_MetaDataTableInfo = {
			0x20, "Assembly",
			0x22, "AssemblyOS",
			0x21, "AssemblyProcessor",
			0x23, "AssemblyRef",
			0x25, "AssemblyRefOS",
			0x24, "AssemblyRefProcessor",
			0x0F, "ClassLayout",
			0x0B, "Constant",
			0x0C, "CustomAttribute",
			0x0E, "DeclSecurity",
			0x12, "EventMap",
			0x14, "Event",
			0x27, "ExportedType",
			0x04, "Field",
			0x10, "FieldLayout",
			0x0D, "FieldMarshal",
			0x1D, "FieldRVA",
			0x26, "File",
			0x2A, "GenericParam",
			0x2C, "GenericParamConstraint",
			0x1C, "ImplMap",
			0x09, "InterfaceImpl",
			0x28, "ManifestResource",
			0x0A, "MemberRef",
			0x06, "MethodDef",
			0x19, "MethodImpl",
			0x18, "MethodSemantics",
			0x2B, "MethodSpec",
			0x00, "Module",
			0x1A, "ModuleRef",
			0x29, "NestedClass",
			0x08, "Param",
			0x17, "Property",
			0x15, "PropertyMap",
			0x11, "StandAloneSig",
			0x02, "TypeDef",
			0x01, "TypeRef",
			0x1B, "TypeSpec"
		};
		#endregion

		#region Member variables
		private Hashtable m_MetaDataTable;
		private StreamInfo[] m_StreamInfo;
		private int m_nMetaDataRootRVA;
		private int m_nMetaDataRoot;
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CLIFileProcessor"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="reader">The reader.</param>
		/// <param name="writer">The writer.</param>
		/// ------------------------------------------------------------------------------------
		public CLIFileProcessor(Stream stream, BinaryReader reader, BinaryWriter writer):
			base(stream, reader, writer)
		{
			m_MetaDataTable = new Hashtable();
			for (int i = 0; i < m_MetaDataTableInfo.Length / 2; i++)
				m_MetaDataTable[m_MetaDataTableInfo[i * 2]] = m_MetaDataTableInfo[i * 2 + 1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the CLI header.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ProcessCLIHeader(long nSectionBase, long nSectionRVA)
		{
			long nCLIHeaderBase = m_stream.Position;
			m_stream.Position += 8;
			m_nMetaDataRootRVA = m_reader.ReadInt32();
			int nMetaDataSize = m_reader.ReadInt32();
			// Flags (4)
			// EntryPointToken (4)
			// Resources (8)
			m_stream.Position += 16;
			int nStrongNameSigRVA = m_reader.ReadInt32();
			int nStrongNameSize = m_reader.ReadInt32();

			// Zero out strong name
			if (nStrongNameSize > 0)
			{
				m_stream.Position = nStrongNameSigRVA - nSectionRVA + nSectionBase;
				byte[] buffer = new byte[nStrongNameSize];
				m_writer.Write(buffer);
			}

			if (nMetaDataSize > 0)
			{
				m_stream.Position = m_nMetaDataRootRVA - nSectionRVA + nSectionBase;
				m_nMetaDataRoot = (int)m_stream.Position;
				ProcessMetaData(nMetaDataSize);
			}
		}
		#endregion

		#region Methods to process parts of CLI section of PE file
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the meta data.
		/// </summary>
		/// <param name="size">The size.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMetaData(int size)
		{
			long nMetaDataBase = m_stream.Position;
			int signature = m_reader.ReadInt32();
			Debug.Assert(signature == 0x424A5342);
			m_stream.Position = nMetaDataBase + 12;
			int nVersionLength = m_reader.ReadInt32();
			m_stream.Position += nVersionLength + 2;
			short nStreams = m_reader.ReadInt16();

			m_StreamInfo = new StreamInfo[nStreams];

			for (int i = 0; i < nStreams; i++)
			{
				m_StreamInfo[i] = new StreamInfo();
				ProcessStreamHeader(nMetaDataBase, m_StreamInfo[i]);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the stream header.
		/// </summary>
		/// <param name="nMetaDataRoot">The meta data root position.</param>
		/// <param name="streamInfo">The stream info.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessStreamHeader(long nMetaDataRoot, StreamInfo streamInfo)
		{
			long nStreamBase = m_stream.Position;

			streamInfo.Offset = m_reader.ReadInt32();
			streamInfo.Size = m_reader.ReadInt32();

			// Read Name. This is a null-terminated string with max length of 32
			streamInfo.Name = ReadName();
			m_stream.Position = nMetaDataRoot + streamInfo.Offset;
			ProcessStream(streamInfo);

			int nameFieldLength = streamInfo.Name.Length + 1;
			if (nameFieldLength % 4 > 0)
				nameFieldLength += 4;
			m_stream.Position = nStreamBase + 8 + (nameFieldLength / 4) * 4;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the stream.
		/// </summary>
		/// <param name="streamInfo">The stream info.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessStream(StreamInfo streamInfo)
		{
			switch (streamInfo.Name)
			{
				case "#GUID":
					ProcessGuidStream(streamInfo);
					break;
				case "#~":
					// At the moment we don't do anything with the tilde stream, so we don't
					// need to call that method...
					//ProcessTildeStream(streamInfo);
					break;
				default:
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the GUID stream.
		/// </summary>
		/// <param name="streamInfo">The stream info.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessGuidStream(StreamInfo streamInfo)
		{
			// blank out the GUIDs
			int nGuidSize = Marshal.SizeOf(typeof(Guid));
			int nGuids = streamInfo.Size / nGuidSize;
			byte[] buffer = new byte[nGuidSize];
			for (int i = 0; i < nGuids; i++)
				m_writer.Write(buffer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the #~ stream.
		/// </summary>
		/// <param name="streamInfo">The stream info.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessTildeStream(StreamInfo streamInfo)
		{
			long nStreamBase = m_stream.Position;
			m_stream.Position += 8;
			ulong vValid = m_reader.ReadUInt64();
			int cValid = CountSetBits(vValid);
			ulong vSorted = m_reader.ReadUInt64();

			uint[] rows = new uint[cValid];
			for (int i = 0; i < cValid; i++)
			{
				rows[i] = m_reader.ReadUInt32();
			}

			// Handle tables
			for (int i = 0; i < cValid; i++)
			{
				if (((vValid >> i) & 0x01) == 0)
					continue;

				for (int j = 0; j < rows[i]; j++)
				{
					//Debug.WriteLine(string.Format("Reading row {1} of table {0} at position 0x{2:x}",
					//    m_MetaDataTable[i], j, m_stream.Position));
					switch (i)
					{
						case 0:
							ProcessModule();
							break;
						case 1:
							ProcessTypeRef();
							break;
						case 6:
							ProcessMethodDef();
							break;
						default:
							break;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the module.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessModule()
		{
			// Generation (2 byte)
			// Name (index; 4 byte?)
			// Mvid (index; 4 byte?)
			// EncId (index; 4 byte?)
			// EncBaseId (index; 4 byte?)
			m_stream.Position += 18;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the type ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessTypeRef()
		{
			// ResolutionScope (index; 4 byte)
			// TypeName (index; 4 byte)
			// TypeNamespace (index; 4 byte)
			m_stream.Position += 12;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the method def.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessMethodDef()
		{
			// RVA (4 byte)
			int rva = m_reader.ReadInt32();
			Debug.WriteLine(string.Format("RVA={0:x}, in file={1:x}", rva,
				rva - m_nMetaDataRootRVA + m_nMetaDataRoot));

			// ImplFlags (2 byte)
			// Flags (2 byte)
			// Name (index; 4 byte)
			// Signature (index; 4 byte)
			// ParamList (index; 4 byte)
			m_stream.Position += 16;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Counts the number of bits set to 1.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Number of bits set to 1</returns>
		/// ------------------------------------------------------------------------------------
		private int CountSetBits(ulong value)
		{
			int cCount = 0;
			for (int i = 0; i < 64; i++)
			{
				if (((value >> i) & 0x01) == 1)
					cCount++;
			}
			return cCount;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the name.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string ReadName()
		{
			StringBuilder name = new StringBuilder();
			for (int i = 0; i < 32; i++)
			{
				char c = m_reader.ReadChar();
				if (c == '\0')
					break;
				name.Append(c);
			}

			return name.ToString();
		}
		#endregion
	}
}
