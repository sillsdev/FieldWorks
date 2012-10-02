using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;                  // for RegistryKey
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// Managed CC EncConverter
	/// </summary>
	[GuidAttribute("287CD0CA-E924-409a-9A7A-8DC6E32BA02A")]
	// normally these subclasses are treated as the base class (i.e. the
	//  client can use them orthogonally as IEncConverter interface pointers
	//  so normally these individual subclasses would be invisible), but if
	//  we add 'ComVisible = false', then it doesn't get the registry
	//  'HKEY_CLASSES_ROOT\SilEncConverters31.TecEncConverter' which is the basis of
	//  how it is started (see EncConverters.AddEx).
	// [ComVisible(false)]
	public class CcEncConverter : EncConverter
	{
		#region DLLImport Statements
		[DllImport("Cc32", SetLastError=true)]
		static extern unsafe int CCLoadTable(byte* lpszCCTableFile,
			Int32* hpLoadHandle,
			Int32 hinstCurrent);

		[DllImport("Cc32", SetLastError=true)]
		static extern unsafe int CCUnloadTable(Int32 hUnlHandle);

		[DllImport("Cc32", SetLastError=true)]
		static extern unsafe int CCProcessBuffer(Int32 hProHandle,
			byte* lpInputBuffer, int nInBufLen,
			byte* lpOutputBuffer, int *npOutBufLen);
		#endregion DLLImport Statements

		#region Member Variable Definitions
		private Int32       m_hTable = 0;
		private DateTime    m_timeModified = DateTime.MinValue;
		private bool        m_bUseDelimiters = false;

		public const string strDisplayName = "CC Table";
		public const string strHtmlFilename = "CC Table Plug-in About box.mht";
		#endregion Member Variable Definitions

		#region Initialization
		/// <summary>
		/// The class constructor. </summary>
		public CcEncConverter() : base(typeof(CcEncConverter).FullName,EncConverters.strTypeSILcc)
		{
		}

		/// <summary>
		/// The class destructor. </summary>
		~CcEncConverter()
		{
			if( IsFileLoaded() )
				CCUnloadTable(m_hTable);
		}

		public override void Initialize(string converterName, string converterSpec, ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType, ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			// let the base class have first stab at it
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID,
				ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );

			// the only thing we want to add (now that the convType can be less than accurate)
			//  is to make sure it's unidirectional
			switch(conversionType)
			{
				case ConvType.Legacy_to_from_Legacy:
					conversionType = ConvType.Legacy_to_Legacy;
					break;
				case ConvType.Legacy_to_from_Unicode:
					conversionType = ConvType.Legacy_to_Unicode;
					break;
				case ConvType.Unicode_to_from_Legacy:
					conversionType = ConvType.Unicode_to_Legacy;
					break;
				case ConvType.Unicode_to_from_Unicode:
					conversionType = ConvType.Unicode_to_Unicode;
					break;
				default:
					break;
			}

			// if this is a spelling fixer cc table, then use delimiters around
			//  the input string so we can do word boundary testing (the delimiter
			//  will be considered whitespace). This is okay because we know that
			//  spelling fixer cc tables will not otherwise be looking for the
			//  delimiter, which might cause us problems.
			if( (processTypeFlags & (long)ProcessTypeFlags.SpellingFixerProject) != 0 )
				m_bUseDelimiters = true;

			// if we're supposedly adding this one, then clobber our copy of its last modified
			// (there was a problem with us instantiating lots of these things in a row and
			//  not detecting the change because the modified date was within a second of each
			//  other)
			if( bAdding )
				m_timeModified = DateTime.MinValue;
		}

		#endregion Initialization

		#region Misc helpers
		protected bool IsFileLoaded()
		{
			return (m_hTable != 0);
		}

		protected void Unload()
		{
			if( IsFileLoaded() )
			{
				CCUnloadTable(m_hTable);
				m_hTable = 0;
			}
		}

		protected override EncodingForm  DefaultUnicodeEncForm(bool bForward, bool bLHS)
		{
			// if it's unspecified, then we want UTF-16
			return EncodingForm.UTF16;
		}

		protected unsafe void Load(string strTablePath)
		{
			// first make sure it's there and get the last time it was modified
			DateTime timeModified = DateTime.Now; // don't care really, but have to initialize it.
			if( !DoesFileExist(strTablePath, ref timeModified) )
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, strTablePath);

			// if it has been modified or it's not already loaded...
			if( timeModified > m_timeModified )
			{
				// keep track of the modified date, so we can detect a new version to reload
				m_timeModified = timeModified;

				if( IsFileLoaded() )
					Unload();

				Int32 hInstanceHandle = 0;  // don't know what else to use here...
				byte [] baTablePath = Encoding.ASCII.GetBytes(strTablePath);
				fixed(byte* pszTablePath = baTablePath)
					fixed(Int32* phTable = &m_hTable)
					{
						int status = CCLoadTable( pszTablePath, phTable, hInstanceHandle );
						if( status != 0 )
						{
							TranslateErrStatus(status);
						}
					}
			}
		}

		protected void    TranslateErrStatus(int status)
		{
			switch(status)
			{
				case -1:    // what CC returns when the buffer we provided wasn't big enough.
					EncConverters.ThrowError(ErrStatus.NotEnoughBuffer);
					break;
				case -2:    // CC_SYNTAX_ERROR from ccdll.h
					EncConverters.ThrowError(ErrStatus.SyntaxErrorInTable);
					break;

				default:
					break;
			}
		}
		#endregion Misc helpers

		#region Abstract Base Class Overrides
		protected override void PreConvert
			(
			EncodingForm        eInEncodingForm,
			ref EncodingForm    eInFormEngine,
			EncodingForm        eOutEncodingForm,
			ref EncodingForm    eOutFormEngine,
			ref NormalizeFlags  eNormalizeOutput,
			bool                bForward
			)
		{
			// let the base class do it's thing first
			base.PreConvert( eInEncodingForm, ref eInFormEngine,
							eOutEncodingForm, ref eOutFormEngine,
							ref eNormalizeOutput, bForward);

			// The CC DLL (conversion engine) usually works in UTF8 for Unicode. As a future
			//	enhancement, it might be possible to get a (marked) value from the repository
			//	telling us what form to use (which would be UTF8Bytes by default and could be
			//	something else if the user developed a UTF32 cc table--using the xYYYY syntax
			//	rather than the uXXXX syntax). But for now, assume that all CC tables that
			//	use Unicode want UTF8.
			if( NormalizeLhsConversionType(ConversionType) == NormConversionType.eUnicode )
			{
				// returning this value will cause the input Unicode data (of any form, UTF16, BE, etc.)
				//	to be converted to UTF8 narrow bytes before calling DoConvert.
				eInFormEngine = EncodingForm.UTF8Bytes;
			}
			else
			{
				// legacy
				eInFormEngine = EncodingForm.LegacyBytes;
			}

			if( NormalizeRhsConversionType(ConversionType) == NormConversionType.eUnicode )
			{
				eOutFormEngine = EncodingForm.UTF8Bytes;
			}
			else
			{
				eOutFormEngine = EncodingForm.LegacyBytes;
			}

			// do the load at this point.
			Load(ConverterIdentifier);
		}

		[CLSCompliant(false)]
		protected override unsafe void DoConvert
			(
			byte*       lpInBuffer,
			int         nInLen,
			byte*       lpOutBuffer,
			ref int     rnOutLen
			)
		{
/*          rde1.2.1.0 don't pad with space anymore
			rde2.2.0.0 Ahh... now I remember why this was there before: if you use boundary
			condition testing in CC (e.g. "prec(ws) 'i' fol(ws)", where  'ws' contains things
			like spaces, nl, tabs, punctuation, etc) then those tests will fail on the first
			and last character in the stream (which are at a boundary conditions, but can't be
			detected by CC). Anyway, so I want to put back in the stream delimiting, but the
			reason this was originally taken out was because someone had a CC table which was
			eating spaces, so I'll use 'd10' (which never comes in on an Windows system by itself)
			to delimit the stream AND only then if it's a spelling fixer cc table (see Initialize)
*/
#if !rde220
			// the delimiter (if used) is actually '\n', but this normally isn't received by CC
			// without '\r' as well, so it makes a good delimiter in that CC tables aren't likely
			// to be looking to eat it up (which was the problem we had when we delimited with
			// a space).
			const byte byDelim = 10;
			if( m_bUseDelimiters )
			{
				// move the input data down to make room for the initial delimiter
				ECNormalizeData.MemMove(lpInBuffer+1, lpInBuffer, nInLen);

				lpInBuffer[0] = byDelim;
				lpInBuffer[nInLen + 1] = byDelim;
				nInLen += 2;
			}
#else
			bool bLastWasD10 = false;
			if( lpInBuffer[nInLen-1] == ' ' )
			{
				bLastWasSpace = true;
			}
			else
			{
				lpInBuffer[nInLen++] = (byte)' ';
				lpInBuffer[nInLen] = 0;
			}
#endif

			int status = 0;
			fixed(int* pnOut = &rnOutLen)
			{
				status = CCProcessBuffer(m_hTable, lpInBuffer, nInLen, lpOutBuffer, pnOut);
			}

			if( status != 0 )
			{
				TranslateErrStatus(status);
			}
#if !rde220
			else if( m_bUseDelimiters )
			{
				if( lpOutBuffer[0] == byDelim )
					ECNormalizeData.MemMove(lpOutBuffer, lpOutBuffer + 1, --rnOutLen);
				if( lpOutBuffer[rnOutLen - 1] == byDelim )
					rnOutLen--;
			}
#else
/*
			// otherwise strip out that final space we added (sometimes it goes away by itself!!??,
			//  so check first...)
			//  also only if the last of the input was *NOT* a space...
			else if( !bLastWasSpace && (lpOutBuffer[rnOutLen-1] == ' ') )
			{
				rnOutLen--;
			}
*/
#endif
		}

		protected override string   GetConfigTypeName
		{
			get { return typeof(CcEncConverterConfig).AssemblyQualifiedName; }
		}

		#endregion Abstract Base Class Overrides
	}
}
