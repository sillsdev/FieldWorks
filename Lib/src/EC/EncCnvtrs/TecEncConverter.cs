using System;
using System.IO;                        // for file I/O
using System.Collections;               // for the collection
using System.Runtime.InteropServices;   // for the class attributes
using System.Text;                      // for ASCIIEncoding
using System.Windows.Forms;             // for MessageBox (for showing compiler errors)
using Microsoft.Win32;                  // for RegistryKey
using ECInterfaces;                     // for ConvType

namespace SilEncConverters40
{
	/// <summary>
	/// Managed TECkit EncConverter.
	/// </summary>
	[GuidAttribute("C34427E2-CF00-4a62-89AA-2B7681615247")]
	// normally these subclasses are treated as the base class (i.e. the
	//  client can use them orthogonally as IEncConverter interface pointers
	//  so normally these individual subclasses would be invisible), but if
	//  we add 'ComVisible = false', then it doesn't get the registry
	//  'HKEY_CLASSES_ROOT\SilEncConverters40.TecEncConverter' which is the basis of
	//  how it is started (see EncConverters.AddEx).
	// [ComVisible(false)]
	public class TecEncConverter : EncConverter
	{
		#region Const Definitions
		public const string strEFCReq = "EncodingFormConversionRequest"; // use as ConverterIdentifier of Initialize
		public const string strLhsEncodingID   = "Left Hand Side EncodingID";
		public const string strRhsEncodingID   = "Right Hand Side EncodingID";
		public const string strLhsDescription  = "Left Hand Side Description";
		public const string strRhsDescription  = "Right Hand Side Description";
		public const string strMapVersion      = "Map Version";
		public const string strContact         = "Contact";
		public const string strRegisAuthority  = "Registration Authority";
		public const string strRegisName       = "Registration Name";
		public const string strCopyright       = "Copyright";
		public const string strDisplayName     = "TECkit map";
		internal const string strHtmlFilename  = "TECkit Map Plug-in About box.mht";

		// from TECkit_Engine.h (how to include .h files in C#???)
		private const int kFlags_Unicode = 0x00010000;  // from TECkit_Engine.h
		internal const int kNameID_LHS_Name		    = 0;    /* "source" or LHS encoding name, e.g. "SIL-EEG_URDU-2001" */
		internal const int kNameID_RHS_Name		    = 1;	/* "destination" or RHS encoding name, e.g. "UNICODE-3-1" */
		internal const int kNameID_LHS_Description	= 2;	/* source encoding description, e.g. "SIL East Eurasia Group Extended Urdu (Mac OS)" */
		internal const int kNameID_RHS_Description	= 3;	/* destination description, e.g. "Unicode 3.1" */
		/* additional recommended names (parallel to UTR-22) */
		internal const int kNameID_Version			= 4;	/* "1.0b1" */
		internal const int kNameID_Contact			= 5;	/* "mailto:jonathan_kew@sil.org" */
		internal const int kNameID_RegAuthority	    = 6;	/* "SIL International" */
		internal const int kNameID_RegName			= 7;	/* "Greek (Galatia)" */
		internal const int kNameID_Copyright		= 8;	/* "(c)2002 SIL International" */
		internal const int nNumPropertiesFromNames  = 9;

		#endregion Const Definitions

		#region Member Variable Definitions
		private Hashtable       m_mapConverters = new Hashtable();  // map of all TECkit converters being used (usually has only one item)
		private Int32           m_converter;            // current converter (should be some kind of pointer, but can't do includes in C#)
		private byte []         m_baMapping = null;     // have something so 'fixing' it won't crash (it'll be reallocated if loading a true map)

		private UInt32          m_nMapSize;
		private UInt32          m_lhsFlags;
		private UInt32          m_rhsFlags;
		private bool            m_bCompileable;
		protected bool          m_bEFCReq;			// whether this instance is providing a fileless
		private DateTime        m_timeModifiedTec = DateTime.MinValue;
		private string          m_strMapFileSpec;
		private string          m_strTecFileSpec;   // just in case we're using a temporary file

		#endregion Member Variable Definitions

		#region DLLImport Statements
		// one nice thing about *having* to do these DLL import statements in order to use
		//  unmanaged DLL functions (as in TECkit), is that this DLL doesn't actually load
		//  the DLL until it is needed).
		[DllImport("TECkit_x86", SetLastError=true)]
		static extern unsafe int TECkit_CreateConverter(
			byte*				mapping,
			UInt32				mappingSize,
			Byte				mapForward,
			UInt16				sourceForm,
			UInt16				targetForm,
			void*   	        converter);

		[DllImport("TECkit_x86", SetLastError=true)]
		static extern unsafe int TECkit_ConvertBuffer(
			Int32	    converter,
			byte*	    inBuffer,
			UInt32		inLength,
			UInt32*		inUsed,
			byte*		outBuffer,
			UInt32		outLength,
			UInt32*		outUsed,
			Byte		inputIsComplete);

		[DllImport("TECkit_x86", SetLastError=true)]
		static extern unsafe int TECkit_ResetConverter(
			Int32   	converter);

		[DllImport("TECkit_Compiler_x86", SetLastError=true)]
		static extern unsafe int TECkit_Compile(
			byte* txt,
			UInt32 len,
			byte doCompression,
			Delegate errFunc,
			void* userData,
			Byte** outTable,
			UInt32* outLen);

		[DllImport("TECkit_x86", SetLastError=true)]
		static extern unsafe int TECkit_GetMappingFlags(
			byte*				mapping,
			UInt32				mappingSize,
			UInt32*				lhsFlags,
			UInt32*				rhsFlags);

		[DllImport("TECkit_x86", SetLastError=true)]
		static extern unsafe int TECkit_DisposeConverter(
			Int32   	converter);

		[DllImport("TECkit_x86", SetLastError=true)]
		static unsafe extern int TECkit_GetMappingName(
			byte*				mapping,
			UInt32				mappingSize,
			UInt16				nameID,
			byte*				nameBuffer,
			UInt32				bufferSize,
			UInt32*				nameLength);

		[DllImport("TECkit_Compiler_x86", SetLastError=true)]
		static extern UInt32 TECkit_GetCompilerVersion();

		[DllImport("TECkit_x86", SetLastError=true)]
		static extern UInt32 TECkit_GetVersion();

		#endregion DLLImport Statements

		#region Initialization
		public TecEncConverter() : base(typeof(TecEncConverter).FullName,EncConverters.strTypeSILtec)
		{
			m_nMapSize = 0;
			m_lhsFlags = 0;
			m_rhsFlags = 0;
			m_bCompileable = false;
			m_bEFCReq = false;
		}

		protected TecEncConverter(string sProgId, string sImplementType) : base(sProgId,sImplementType)
		{
			m_nMapSize = 0;
			m_lhsFlags = 0;
			m_rhsFlags = 0;
			m_bCompileable = false;
			m_bEFCReq = false;
		}

		~TecEncConverter()
		{
			ResetConverters();
		}

		public override void Initialize(string converterName, string converterSpec, ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType, ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
#if AssemblyChecking
			MessageBox.Show(String.Format("You are listening to:{0}{0}'{1}'{0}{0}implemented in assembly:{0}{0}'{2}'{0}{0}executing from:{0}{0}'{3}'{0}{0}Trying to instantiate:{0}{0}{4}",
				Environment.NewLine,
				typeof(TecEncConverter).FullName,
				typeof(TecEncConverter).Assembly,
				typeof(TecEncConverter).Assembly.CodeBase,
				converterName));
#endif

			// let the base class have first stab at it
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID,
				ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );

			// since some of the other conversion engines do not natively support some of the more
			//	unusual Encoding Forms (e.g. UTF32), the TECkit engine will convert for them (since
			//	it a) can, and b) is guaranteed to be in the repository package. To do this, the
			//	converterSpec will be "EncodingFormConversionRequest"
			if( strEFCReq == converterSpec )
			{
				// in this case, there will be no map. Just (probably) a call to ConvertEx with
				//	the EncodingFormIn and Out--two Unicode forms (say UTF32 to UTF8)
				m_bEFCReq = true;
			}
			else
			{
				// it might fail if the user has hard-coded the .tec file extension, but we haven't
				//  compiled it yet.
				string strExt, strFilename;
				EncConverters.GetFileExtn(converterSpec, out strFilename, out strExt);

				// initialize our references to the two files for later use
				m_strTecFileSpec = strFilename + ".tec";
				m_strMapFileSpec = strFilename + ".map";

				strExt.ToLower();
				DateTime timeModified = DateTime.Now; // don't care really, but have to initialize it.
				if( strExt == ".tec" )
				{
					if( !DoesFileExist(m_strTecFileSpec, ref timeModified) )
					{
						// if the .tec file doesn't exist yet, then compile it.
						CompileMap(m_strMapFileSpec, ref m_strTecFileSpec);
					}
				}

				// if this is a .map file, then we'll need to deal with compiling it (if it
				//  gets old)
				else if( strExt == ".map" )
				{
					m_strImplementType = EncConverters.strTypeSILmap;
					m_bCompileable = true;
				}

				// in either case, if it doesn't exist at this point, then throw an error.
				if( !DoesFileExist(converterSpec,ref timeModified) )
					EncConverters.ThrowError(ErrStatus.CantOpenReadMap, converterSpec);

				// if we're just now adding this, then double check the info in the map
				if( bAdding )
				{
					Load(true);

					// if the user didn't specify the encoding values, then get them from
					//  the map.
					if( String.IsNullOrEmpty(lhsEncodingID) )
						lhsEncodingID = m_strLhsEncodingID;

					// the repository does this kind of defaulting now for the rhs, but we should too
					//  so it reflects what's in the map file.
					if( String.IsNullOrEmpty(rhsEncodingID) )
						rhsEncodingID = m_strRhsEncodingID;

					// finally, return that to the caller as well.
					conversionType = m_eConversionType;
				}
			}
		}
		#endregion Initialization

		#region Abstract Base Class Overrides
		protected unsafe override void PreConvert
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

			// If the user uses one of the *Byte forms, change that to the *String forms so
			//	the value matches what the TECkit engine is expecting (that is, the TECkit
			//	engine is expecting a value of '1' (=LegacyString) even if it comes in as
			//	LegacyBytes). It'll still get converted correctly later, but when create the
			//	the TECkit "converter" object, which happens during here, it is expecting
			//	to see the other value.
			if( eInEncodingForm == EncodingForm.LegacyBytes )
				eInEncodingForm = EncodingForm.LegacyString;
			else if( eInEncodingForm == EncodingForm.UTF8Bytes )
				eInEncodingForm = EncodingForm.UTF8String;

			if( eOutEncodingForm == EncodingForm.LegacyBytes )
				eOutEncodingForm = EncodingForm.LegacyString;
			else if( eOutEncodingForm == EncodingForm.UTF8Bytes )
				eOutEncodingForm = EncodingForm.UTF8String;

			// See if we have a converter already for this combination or whether we need to make a
			//  new one
			string strConverterKey =  eInEncodingForm.ToString()
				+ eOutEncodingForm.ToString()
				+ eNormalizeOutput.ToString()
				+ bForward.ToString();

			// If this is a compilable map (i.e. ImplType SIL.map), then see if the map file has changed
			bool bReload = false;
			if (m_bCompileable && !String.IsNullOrEmpty(m_strMapFileSpec))
			{
				// first make sure it's there and get the last time it was modified
				DateTime timeModified = DateTime.Now; // don't care really, but have to initialize it.
				if (!DoesFileExist(m_strMapFileSpec, ref timeModified))
					EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strMapFileSpec);

				// if it has been modified or it's not already loaded...
				if ((timeModified > m_timeModifiedTec) && m_mapConverters.ContainsKey(strConverterKey))
				{
					// ... just remove this key if it existed (so we fall thru and do Load)
					ResetConverter((Int32)m_mapConverters[strConverterKey]);
					m_mapConverters.Remove(strConverterKey);
					bReload = true;
				}
			}
			else if (IsFileLoaded())
			{
				// the tec file could also have changed out from underneath us (in which case we'd need to reload it).
				DateTime timeModified = DateTime.Now; // don't care really, but have to initialize it.
				if (!DoesFileExist(m_strTecFileSpec, ref timeModified))
					EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strTecFileSpec);

				// if it has been modified or it's not already loaded...
				if ((timeModified > m_timeModifiedTec) && m_mapConverters.ContainsKey(strConverterKey))
				{
					m_baMapping = null; // triggers a reload
					m_lhsFlags = m_rhsFlags = 0;

					// ... just remove this key if it existed (so we fall thru and do Load)
					ResetConverter((Int32)m_mapConverters[strConverterKey]);
					m_mapConverters.Remove(strConverterKey);
					bReload = true;
				}
			}

			if( m_mapConverters.ContainsKey(strConverterKey) )
			{
				m_converter = (Int32)m_mapConverters[strConverterKey];
			}
			else
			{
				int status = (int)ErrStatus.NoError;

				// load the map now
				Load(bReload);

				// is there no better way to do this?
				ushort eFormOut1 = System.Convert.ToUInt16((int)eOutEncodingForm);
				ushort eFormOut2 = System.Convert.ToUInt16((int)eNormalizeOutput);
				UInt16 eFormOut = System.Convert.ToUInt16(eFormOut1 | eFormOut2);

				// make a converter for this new combination.
				fixed(Int32* converter = &m_converter)
				{
					if( IsFileLoaded() )
					{
						fixed(byte* pbyMapping = m_baMapping)
						{
							status = TECkit_CreateConverter(
										pbyMapping,
										m_nMapSize,
										(byte)((bForward) ? 1 : 0),
										System.Convert.ToUInt16((int)eInEncodingForm),
										eFormOut,
										(void*)converter
										);
						}
					}
					else
					{
						status = TECkit_CreateConverter(
									(byte*)0,
									m_nMapSize,
									(byte)((bForward) ? 1 : 0),
									System.Convert.ToUInt16((int)eInEncodingForm),
									eFormOut,
									(void*)converter
									);
					}
				}

				if( status == (int)ErrStatus.NoError )
				{
					m_mapConverters[strConverterKey] = m_converter;
				}
				else
					EncConverters.ThrowError(status);
			}

			// since TEC can handle output normalization directly (by requesting it here
			//  in the creation of the converter), reset the requesting flag so we won't
			//  attempt to do it later (all other converters that can't do implicit output
			//  normalization will *not* have reset the flag and then after their conversion,
			//  if the flag is still set, we'll call TEC to do it for them see
			//  ECNormalizeData.GetString).
			eNormalizeOutput = NormalizeFlags.None;
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
			UInt32  inUsed = 0;
			UInt32  outUsed = 0;

			int status = TECkit_ConvertBuffer
				(
				m_converter,
				lpInBuffer,
				(uint)nInLen,
				&inUsed,
				(Byte*)lpOutBuffer,
				(uint)rnOutLen,
				&outUsed,
				(byte)1
				);

			if( status < (int)ErrStatus.NoError )
			{
				TECkit_ResetConverter(m_converter);
				EncConverters.ThrowError(status);
			}

			rnOutLen = (int)outUsed;
		}

		protected override string   GetConfigTypeName
		{
			get { return typeof(TecEncConverterConfig).AssemblyQualifiedName; }
		}

		#endregion Abstract Base Class Overrides

		#region Misc helpers
		unsafe delegate void errFunc(byte* pThis, byte* msg, byte* param, UInt32 line);
		static unsafe void DisplayCompilerError(byte* pszName, byte* msg, byte* param, UInt32 line)
		{
			byte [] baMsg = ECNormalizeData.ByteStarToByteArr(msg);
			Encoding enc = Encoding.ASCII;
			string str = new string(enc.GetChars(baMsg));

			if (param != (byte*)0)
			{
				str += ": \"";
				baMsg = ECNormalizeData.ByteStarToByteArr(param);
				str += new string(enc.GetChars(baMsg));
				str += "\"";
			}

			if (line != 0)
			{
				str += " at line ";
				str += line.ToString();
			}

			string strCaption = "Compilation feedback from TECkit for the '";

			baMsg = ECNormalizeData.ByteStarToByteArr(pszName);
			strCaption += new string(enc.GetChars(baMsg));
			strCaption += "' converter";

			if( MessageBox.Show(str, strCaption, MessageBoxButtons.OKCancel,MessageBoxIcon.Exclamation) == DialogResult.Cancel )
				EncConverters.ThrowError(ErrStatus.CompilationFailed);
		}

		protected unsafe void CompileMap(string strFilename, ref string strCompiledFilename)
		{
			int status = 0;
			try
			{
				FileStream fileMap = new FileStream(strFilename,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
				byte [] pTxt = new byte [fileMap.Length];
				uint nMapSize = (uint)fileMap.Read(pTxt,0,(int)fileMap.Length);
				byte*	compiledTable = (byte*)0;
				UInt32	compiledSize = 0;
				try
				{
					// do this in a try/catch so the user can cancel if there are too many
					//  errors.
					errFunc dsplyErr = new errFunc( TecEncConverter.DisplayCompilerError );
					byte [] baName = Encoding.ASCII.GetBytes(Name);

					fixed (byte* lpTxt = pTxt)
					fixed (byte* lpName = baName)
					status = TECkit_Compile(
						lpTxt,
						nMapSize,
						(byte)1,   // docompression
						dsplyErr,
						lpName,
						&compiledTable,
						&compiledSize);
				}
				catch
				{
					status = (int)ErrStatus.CompilationFailed;
				}

				if( status == (int)ErrStatus.NoError )
				{
					// put the data from TEC into a managed byte array for the following Write
					byte [] baOut = new byte [compiledSize];
					ECNormalizeData.ByteStarToByteArr(compiledTable,(int)compiledSize,baOut);

					// save the compiled mapping (but if it fails because it's locked, then
					//  try to save it with a temporary name.
					FileStream fileTec = null;
					try
					{
						fileTec = File.OpenWrite(strCompiledFilename);
					}
					catch(System.IO.IOException)
					{
						// temporary filename for temporary CC tables (to check portions of the file at a time)
						strCompiledFilename = Path.GetTempFileName();
						fileTec = File.OpenWrite(strCompiledFilename);
					}

					// get it's last created timestamp
					DoesFileExist(strCompiledFilename, ref m_timeModifiedTec);

					fileTec.Write(baOut,0,(int)compiledSize);
					fileTec.Close();
				}
			}
			catch
			{
				// compiling isn't crucial
			}

			if( status != (int)ErrStatus.NoError )
				EncConverters.ThrowError(status);
		}

		protected unsafe void Load(bool bReload)
		{
			// if this is an EncodingForm Conversion request, then there is no filename...
			if( m_bEFCReq )
				return;

			// both of these filename should have been set up during Initialize
			System.Diagnostics.Debug.Assert( !String.IsNullOrEmpty(m_strTecFileSpec) && !String.IsNullOrEmpty(m_strMapFileSpec) );

			// if the spec corresponds to the map...
			if (m_bCompileable)
			{
				// ... see if the map file is newer than the compiled version
				DateTime fsmap = new DateTime(0);
				if (!DoesFileExist(m_strMapFileSpec, ref fsmap))
					EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strMapFileSpec);

				// if the compiled version a) isn't there or b) is older than the map...
				DateTime fstec = DateTime.MinValue;
				if (!DoesFileExist(m_strTecFileSpec, ref fstec)
					|| (fstec > m_timeModifiedTec)  // become someone else could have compiled it...
					|| (fsmap > m_timeModifiedTec))
				{
					// ... reset everything and recompile the map
					ResetConverters();
					m_baMapping = null;
					m_lhsFlags = m_rhsFlags = 0;
					CompileMap(m_strMapFileSpec, ref m_strTecFileSpec); // may change m_strTecFileSpec if locked
				}
			}
			else if (!DoesFileExist(m_strTecFileSpec, ref m_timeModifiedTec) )
			{
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strTecFileSpec);
			}

			if( IsFileLoaded() )
				return;

			// otherwise, load it now.
			FileStream fileTec = File.OpenRead(m_strTecFileSpec);
			m_baMapping = new byte [fileTec.Length];
			m_nMapSize = (uint)fileTec.Read(m_baMapping,0,(int)fileTec.Length);

			int status = 0;
			fixed (byte* pbyMapping = m_baMapping)
			fixed (UInt32* pLhsFlags = &m_lhsFlags, pRhsFlags = &m_rhsFlags)
			{
				status = TECkit_GetMappingFlags(
					pbyMapping,
					m_nMapSize,
					pLhsFlags,
					pRhsFlags);
			}

			if( status != (int)ErrStatus.NoError )
				EncConverters.ThrowError(status);

			if (bReload)
			{
				// call this method to load the attributes from the map (don't care about retval)
				string[] asDontCare = AttributeKeys;

				// if the user didn't specify the encoding values, then get them from
				//  the map.
				m_strLhsEncodingID = AttributeValue(strLhsEncodingID);

				// the repository does this kind of defaulting now for the rhs, but we should too
				//  so it reflects what's in the map file.
				m_strRhsEncodingID = AttributeValue(strRhsEncodingID);

				// determine the conversion type from the map details (unless the
				//	user specifically tells us it is unidirectional (because we
				//	can't tell that from the map)
				bool bUnidirectional = EncConverters.IsUnidirectional(ConversionType);
				if ((m_lhsFlags & kFlags_Unicode) != 0)
				{
					if ((m_rhsFlags & kFlags_Unicode) != 0)
					{
						m_eConversionType = (bUnidirectional)
							? ConvType.Unicode_to_Unicode : ConvType.Unicode_to_from_Unicode;
					}
					else
					{
						m_eConversionType = (bUnidirectional)
							? ConvType.Unicode_to_Legacy : ConvType.Unicode_to_from_Legacy;
					}
				}
				else
				{
					if ((m_rhsFlags & kFlags_Unicode) != 0)
					{
						m_eConversionType = (bUnidirectional)
							? ConvType.Legacy_to_Unicode : ConvType.Legacy_to_from_Unicode;
					}
					else
					{
						m_eConversionType = (bUnidirectional)
							? ConvType.Legacy_to_Legacy : ConvType.Legacy_to_from_Legacy;
					}
				}
			}
		}

		protected void ResetConverters()
		{
			foreach( int converter in m_mapConverters.Values )
			{
				ResetConverter(converter);
			}
			m_mapConverters.Clear();
		}

		protected void ResetConverter(int converter)
		{
			TECkit_DisposeConverter(converter);
		}

		protected bool IsFileLoaded()
		{
			return (m_baMapping != null);
		}

		[CLSCompliant(false)]
		protected unsafe string GetTecAttributeName(int nID, byte* pbaNameBuffer, byte* pbyMapping)
		{
			// now ask TECkit for the values
			UInt16 sID = System.Convert.ToUInt16(nID);
			UInt32 nNameLength = 0;
			TECkit_GetMappingName(
				pbyMapping,
				m_nMapSize,
				sID,
				pbaNameBuffer,
				1000,
				&nNameLength);

			byte [] baName = new byte [nNameLength];
			ECNormalizeData.ByteStarToByteArr(pbaNameBuffer,(int)nNameLength,baName);
			return new string(Encoding.ASCII.GetChars(baName));
		}

		[CLSCompliant(false)]
		protected unsafe void LoadAttribute(out string rstr, string strAttrKey, int nID, byte* pbaNameBuffer, byte* pbyMapping)
		{
			rstr = strAttrKey;  // rSa.Add(strAttrKey);
			m_mapProperties[strAttrKey] = GetTecAttributeName(nID,pbaNameBuffer,pbyMapping);
		}

		protected unsafe override void GetAttributeKeys(out string [] rSa)
		{
			// first create the thing
			rSa = new string [ (m_bEFCReq) ? 2 : 11 ];

			// during this call, we'll add the keys for the attributes to rSa and we'll add
			//  the answers (and the keys) to the map we maintain (so the answer can be more
			//  easily given during get_AttributeValue).
			string strAttrKey = "TECkitCompilerVersion";
			rSa[0] = strAttrKey;
			m_mapProperties[strAttrKey] = TECkit_GetCompilerVersion().ToString();

			strAttrKey = "TECkitEngineVersion";
			rSa[1] = strAttrKey;
			m_mapProperties[strAttrKey] = TECkit_GetVersion().ToString();

			// if this is the EncodingForm converter, then we're done with the properties
			if( m_bEFCReq )
				return;

			// the rest of the attributes come from the table, so load it now if it isn't loaded.
			if( !IsFileLoaded() )
				Load(false);

			byte [] baNameBuffer = new byte [1000];
			fixed(byte* pbaNameBuffer = baNameBuffer)
			fixed(byte* pbyMapping = m_baMapping)
			{
				LoadAttribute(out rSa[2], strLhsEncodingID, kNameID_LHS_Name, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[3], strRhsEncodingID, kNameID_RHS_Name, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[4], strLhsDescription, kNameID_LHS_Description, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[5], strRhsDescription, kNameID_RHS_Description, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[6], strMapVersion, kNameID_Version, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[7], strContact, kNameID_Contact, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[8], strRegisAuthority, kNameID_RegAuthority, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[9], strRegisName, kNameID_RegName, pbaNameBuffer, pbyMapping);
				LoadAttribute(out rSa[10], strCopyright, kNameID_Copyright, pbaNameBuffer, pbyMapping);
			}
		}
		#endregion Misc helpers
	}

	#region Unicode Encoding Form TECkit sub-class
	public class TecFormEncConverter : TecEncConverter
	{
		public TecFormEncConverter() : base(typeof(TecFormEncConverter).FullName,EncConverters.strTypeSILtecForm)
		{
			m_bEFCReq = true;
			m_eConversionType = ConvType.Unicode_to_from_Unicode;
		}

		// users don't need to, but in case they do call this (we need to make sure the
		//  converterSpec is strEFCReq
		public override void Initialize(string converterName, string converterSpec, ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType, ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			// let the base class do the work (but give strEFCReq as the spec)
			base.Initialize(converterName, strEFCReq, ref lhsEncodingID, ref rhsEncodingID,
				ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );
		}
	}
	#endregion Unicode Encoding Form TECkit sub-class
}
