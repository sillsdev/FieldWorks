// AssemblyParser.cs
//
// This file defines a class named AssemblyParser that takes a path
// to an arbitrary PE file as input to its constructor, parses that
// files PE headers, and then makes available a property (IsAssembly)
// indicating whether or not the specified PE file is a valid .NET
// assembly.  If so, other properties indicate whether the assembly
// has an entry point, is strongly named, and whether or not it only
// has IL in it (MC++ assemblies might contain a mix of IL & x86 instructions).
//
// Note that the RuntimeMajorVersion & RuntimeMinorVersion properties
// return the contents of the same-named fields in the CLI part of
// the PE header, but that those values are hard coded to 2.0
// in the current CLI specification.  These values do not represent
// the version of mscorlib.dll that the target assembly was
// built against.
//
// Comments throughout the parsing code indicate which part of the
// CLI spec the code was based on.  The CLI specs are installed as
// part of the .NET SDK/VS.NET and are located in
// C:\program files\Microsoft Visual Studio .NET 2003\SDK\v1.1\Tool Developers Guide\docs.
//
// Finally, this implementation uses C# pointer syntax to efficiently
// 'walk through' the relevant parts of the PE headers.  Therefore the
// assembly you include this source in must be built to allow for
// unsafe code blocks; which means the resulting assembly will be
// unverifiable - which has certain implications for code access security.
//
// Usage is demonstrated in the included TestApp.cs file.
//
// 1/5/06: Updated to show whether an entry point token is present in the CLI header.
//         Fixed a long-standing bug that caused the DLL/EXE mapped into the address
//         space not to be unmapped (was erroneously calling CloseHandle instead of
//         FreeLibrary).
//
// Mike Woodring
// Bear Canyon Consulting LLC
// http://www.bearcanyon.com
//
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;

namespace BearCanyon
{
	public class AssemblyParser : IDisposable
	{
		uint _hLib = 0;
		bool _isAssembly = false;
		bool _isILOnly = false;
		bool _hasStrongName = false;
		short _runtimeMajorVer = 0;
		short _runtimeMinorVer = 0;
		bool _hasEntryPoint = false;

		public AssemblyParser( string fileName )
		{
			_hLib = LoadLibraryEx( fileName, 0,
								   DONT_RESOLVE_DLL_REFERENCES | LOAD_IGNORE_CODE_AUTHZ_LEVEL );

			if( _hLib == 0 )
			{
				throw new FileNotFoundException(string.Format("Failed to load {0}.\nSpecified file was either not found, or is not a valid PE file.", fileName));
			}

			ScanHeaders();
		}

		public bool IsAssembly
		{
			get { return(_isAssembly); }
		}

		public bool IsILOnly
		{
			get { return(_isILOnly); }
		}

		public bool HasStrongName
		{
			get { return(_hasStrongName); }
		}

		public bool HasEntryPoint
		{
			get { return(_hasEntryPoint); }
		}

		public short RuntimeMajorVersion
		{
			// In the current (Sept, 2003) spec, this should
			// always be 2.
			//
			get { return(_runtimeMajorVer); }
		}

		public short RuntimeMinorVersion
		{
			// In the current (Sept, 2003) spec, this should
			// always be 0.
			//
			get { return(_runtimeMinorVer); }
		}

		public void Dispose()
		{
			Cleanup(false);
		}

		~AssemblyParser()
		{
			Cleanup(true);
		}

		void Cleanup( bool finalizing )
		{
			if( _hLib != 0 )
			{
				FreeLibrary(_hLib);
				_hLib = 0;
			}
		}

		unsafe void ScanHeaders()
		{
			// Verify file starts with "MZ" signature.
			//
			byte *pDosHeader = (byte *)_hLib;

			if( (pDosHeader[0] != 0x4d) || (pDosHeader[1] != 0x5a) )
			{
				// Not a PE file.
				//
				return;
			}

			// Partion II, 24.2.1
			//
			const uint OFFSET_TO_PE_HEADER_OFFSET = 0x3c;
			uint offsetToPESig = UIntFromBytes(pDosHeader + OFFSET_TO_PE_HEADER_OFFSET);
			byte *pPESig = pDosHeader + offsetToPESig;

			// Verify PE header starts with 'PE\0\0'.
			//
			if( (pPESig[0] != 0x50) || (pPESig[1] != 0x45) ||
				(pPESig[2] != 0) || (pPESig[3] != 0) )
			{
				// Not a PE file.
				//
				return;
			}

			// It's a PE file, verify that it has the right "machine" code.
			// Partion II, 24.2.2
			//
			const uint PE_FILE_HEADER_SIZE = 20;

			byte *pPEHeader = pPESig + 4;
			ushort machineCode = UShortFromBytes(pPEHeader);

			if( machineCode != 0x014c )
			{
				// Invalid or unrecognized PE file of some kind.
				//
				return;
			}

			// Locate the PE_OPTIONAL_HEADER & verify that the
			// number of data directories is 0x10.
			//
			byte *pPEOptionalHeader = pPEHeader + PE_FILE_HEADER_SIZE;  // Partition II, 24.2.3
			uint numDataDirs = UIntFromBytes(pPEOptionalHeader + 92);   // Partition II, 24.2.3.2

			if( numDataDirs != 0x10 ) // Partition II, 24.2.3.2
			{
				// Invalid or unrecognized PE file of some kind.
				//
				return;
			}

			// Check for the existance of a non-null CLI header.
			// If found, this is an assembly of some kind, otherwise
			// it's a native PE file of one kind or another.
			//
			uint rvaCLIHeader = UIntFromBytes(pPEOptionalHeader + 208);  // Partition II, 24.2.3.3, CLI Header (rva)
			uint cliHeaderSize = UIntFromBytes(pPEOptionalHeader + 212); // Partition II, 24.2.3.3, CLI Header (size)

			if( rvaCLIHeader == 0 )
			{
				// Not an assembly.
				//
				return;
			}

			// It's an assembly.  Just grab a few bits of information
			// to be returned by the public properties of this class.
			//
			_isAssembly = true;

			// Partition II, 24.3.3 (CLI Header)
			//
			byte *pCLIHeader = pDosHeader + rvaCLIHeader;

			const uint OFFSET_TO_MAJOR_RUNTIME_VERSION = 4; // ushort
			const uint OFFSET_TO_MINOR_RUNTIME_VERSION = 6; // ushort
			const uint OFFSET_TO_CLI_FLAGS = 16;            // uint
			const uint OFFSET_TO_ENTRYPOINT_TOKEN = 20;     // uint

			uint cliFlags = UIntFromBytes(pCLIHeader + OFFSET_TO_CLI_FLAGS);

			// Partition II, 24.3.3.1
			//
			const uint CLI_FLAG_IL_ONLY = 0x00000001;
			const uint CLI_FLAG_STRONG_NAME_SIGNED = 0x00000008;

			_isILOnly = ((cliFlags & CLI_FLAG_IL_ONLY) == CLI_FLAG_IL_ONLY);
			_hasStrongName = ((cliFlags & CLI_FLAG_STRONG_NAME_SIGNED) == CLI_FLAG_STRONG_NAME_SIGNED);
			_runtimeMajorVer = (short)UShortFromBytes(pCLIHeader + OFFSET_TO_MAJOR_RUNTIME_VERSION);
			_runtimeMinorVer = (short)UShortFromBytes(pCLIHeader + OFFSET_TO_MINOR_RUNTIME_VERSION);

			uint entryPointToken = UIntFromBytes(pCLIHeader + OFFSET_TO_ENTRYPOINT_TOKEN);
			_hasEntryPoint = (entryPointToken != 0);
		}

		unsafe uint UIntFromBytes( byte *p )
		{
			uint intVal = (uint)p[0];
			uint byteVal;

			byteVal = (uint)p[1];
			byteVal <<= 8;
			intVal |= byteVal;

			byteVal = (uint)p[2];
			byteVal <<= 16;
			intVal |= byteVal;

			byteVal = (uint)p[3];
			byteVal <<= 24;
			intVal |= byteVal;

			return(intVal);
		}

		unsafe ushort UShortFromBytes( byte *p )
		{
			ushort shortVal = (ushort)p[1];
			shortVal <<= 8;
			shortVal |= (ushort)p[0];

			return(shortVal);
		}

		// From winbase.h in the Win32 platform SDK.
		//
		const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
		const uint LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010;

		[DllImport("kernel32.dll"), SuppressUnmanagedCodeSecurity]
		static extern uint LoadLibraryEx( string fileName, uint notUsedMustBeZero, uint flags );

		[DllImport("kernel32.dll"), SuppressUnmanagedCodeSecurity ]
		static extern bool FreeLibrary( uint h );
	}
}
