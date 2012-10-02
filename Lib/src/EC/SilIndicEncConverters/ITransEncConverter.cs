using System;
using System.IO;                        // for StreamWriter
using System.Runtime.InteropServices;   // for GuidAttribute
using ECInterfaces;                     // for IEncConverter
using SilEncConverters31;

namespace SilEncConverters31
{
	/// <summary>
	/// ITransEncConverter implements the EncConverter interface to provide a ITrans<>UNICODE
	/// converter (transliterator)
	/// </summary>
	[GuidAttribute("06EDA16B-1831-4fda-81F9-8601776351A2")]
	public class ITransEncConverter : ExeEncConverter
	{
		#region Member Variable Definitions
		private string  m_strIfmFile;

		public const string strLhsEncoding  = "ITrans";
		public const string strImplType     = "SIL.ITrans";
		public const string strExeDefPath   = @"\SIL\Indic\ITrans"; // (from \pf\cf...)

		public string IfmFile
		{
			get { return m_strIfmFile; }
			set { m_strIfmFile = value; }
		}
		#endregion Member Variable Definitions

		#region Initialization
		public ITransEncConverter()
		  : base
			(
			typeof(ITransEncConverter).FullName,    // strProgramID
			strImplType,                            // strImplType
			ConvType.Legacy_to_Unicode,             // conversionType
			strLhsEncoding,                         // lhsEncodingID
			EncConverters.strDefUnicodeEncoding,    // rhsEncodingID
			(Int32)ProcessTypeFlags.Transliteration, // lProcessType
			strExeDefPath
			)
		{
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );

			converterSpec = converterSpec.ToLower();
			if( converterSpec == "hindi" )
			{
				IfmFile = "udvng.ifm";
			}
			else if( converterSpec == "marathi" )
			{
				IfmFile = "udvng.ifm";
			}
			else if( converterSpec == "sanskrit" )
			{
				IfmFile = "udvng.ifm";
			}
			else if( converterSpec == "bengali" )
			{
				IfmFile = "ubeng.ifm";
			}
			else if( converterSpec == "gujarati" )
			{
				IfmFile = "uguj.ifm";
			}
			else if( converterSpec == "gurmukhi" )
			{
				IfmFile = "ugur.ifm";
			}
			else if( converterSpec == "telugu" )
			{
				IfmFile = "utel.ifm";
			}
			else if( converterSpec == "tamil" )
			{
				IfmFile = "utml.ifm";
			}
			else if( converterSpec == "kannada" )
			{
				IfmFile = "ukan.ifm";
			}
			else if( converterSpec == "roman" )
			{
				IfmFile = "uroman.ifm";
			}
			else if( converterSpec == "oriya" )
			{
				IfmFile = "uoriya.ifm";
			}
			else if( converterSpec == "malayalam" )
			{
				IfmFile = "umal.ifm";
			}
			else
			{
				// otherwise, maybe they gave us the name of the ifm file itself
				string strFilename = this.WorkingDir + @"\" + converterSpec;
				if( File.Exists(strFilename) )
					IfmFile = converterSpec;
				else
					EncConverters.ThrowError(ErrStatus.NameNotFound, strFilename);
			}

			// we know that our CP input and output are "1252" and "65001" respectively
			// (unless specified)
			if( codePageInput == 0 )
				this.CodePageInput = 1252;
			if( codePageOutput == 0 )
				this.CodePageOutput = 65001;
		}
		#endregion Initialization

		#region Abstract Base Class Overrides
		public override string ExeName
		{
			get{ return "itrans.exe"; }
		}

		public override void WriteToExeInputStream(string strInput, StreamWriter input)
		{
			input.WriteLine("#output=UTF_8");
			input.WriteLine(String.Format("#{0}ifm={1}",this.ConverterIdentifier,this.IfmFile));
			input.WriteLine(String.Format("#{0}",this.ConverterIdentifier));

			// let the base class do the rest
			base.WriteToExeInputStream(strInput,input);
		}

		public override string ReadFromExeOutputStream(StreamReader srOutput, StreamReader srError)
		{
			// just get the whole string read in (from the base class method)
			string strReturn = base.ReadFromExeOutputStream(srOutput, srError);

			// skip past the commercial...
			string strCommercial = "\r\n%\r\n% ---------------------------------------------------------\r\n% ITRANS 5.30 (8 July 2001) (HTML [Unicode UTF-8] Interface)\r\n% Copyright 1991--2001 Avinash Chopde, All Rights Reserved.\r\n% ---------------------------------------------------------\r\n%\r\n\r\n\r\n";
			int nIndex = strReturn.IndexOf(strCommercial);
			if( nIndex == -1 )
			{
				throw new Exception("Unrecognized ITrans reply: " + strReturn);
			}
			else
			{
				int len = strCommercial.Length;
				strReturn = strReturn.Substring( nIndex + len, strReturn.Length - len - 2 );
			}

			if( strReturn.IndexOf("ITRANS error") != -1 )
			{
				throw new Exception("ITrans failed with error: " + strReturn);
			}
			else
			{
#if !rde210
				// by setting the output code page explicitly, the string is already unicode.
				return strReturn;
#else
				// the data comes in the form: UTF-8 in a String.
				//  so first narrowize it according to the default code page, and
				//  then convert it to Unicode
				byte [] ba = Encoding.GetEncoding(0).GetBytes(strReturn);
				return Encoding.UTF8.GetString(ba);
#endif
			}
		}

		protected override string GetConfigTypeName
		{
			get { return null; }
		}

		#endregion Abstract Base Class Overrides
	}
}
