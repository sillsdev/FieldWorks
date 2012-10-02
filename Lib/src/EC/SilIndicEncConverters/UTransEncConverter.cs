using System;
using System.IO;                        // for StreamWriter
using System.Runtime.InteropServices;   // for ComRegisterFunctionAttribute
using ECInterfaces;                     // for IEncConverter
using SilEncConverters31;

namespace SilEncConverters31
{
	/// <summary>
	/// UTransEncConverter implements the EncConverter interface to provide a UTrans<>UNICODE
	/// converter (transliterator)
	/// </summary>
	[GuidAttribute("483BDA07-39D4-4270-A272-F9D1223FBA29")]
	public class UTransEncConverter : ExeEncConverter
	{
		#region Member Variable Definitions
		public const string strLhsEncoding  = "UTrans";
		public const string strImplType     = "SIL.UTrans";
		public const string strExeDefPath   = @"\SIL\Indic\UTrans"; // (from \pf\cf...)
		#endregion Member Variable Definitions

		#region Initialization
		public UTransEncConverter()
		  : base
			(
			typeof(UTransEncConverter).FullName,    // strProgramID
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

			// we know that our CP input and output are "1252" and "65001" respectively
			// (unless specified)
			if( codePageInput == 0 )
				this.CodePageInput = 1252;
			if( codePageOutput == 0 )
				this.CodePageOutput = 1252;
		}
		#endregion Initialization

		#region Abstract Base Class Overrides
		public override string ExeName
		{
			get{ return "u-trans.exe"; }
		}

		public override void WriteToExeInputStream(string strInput, StreamWriter input)
		{
			input.WriteLine("#beginurdu");
			base.WriteToExeInputStream(strInput,input);
		}

		public override string ReadFromExeOutputStream(StreamReader srOutput, StreamReader srError)
		{
			// just get the whole string read in (from the base class method)
			string strReturn = base.ReadFromExeOutputStream(srOutput, srError);
			string strOutput = null;
			if( strReturn.IndexOf("U-TRANS error") != -1 )
			{
				throw new Exception("U-Trans failed with error: " + strReturn);
			}
			else
			{
				// the data comes in the form: "&#65165;&#65167;   <br>\r\n"
				int nStartIndex = 0;
				do
				{
					int nLength = 0;
					char ch;
					if( (strReturn[nStartIndex] == '&') && (strReturn[nStartIndex+1] == '#') )
					{
						nStartIndex += 2;   // skip past and then find the end
						nLength = strReturn.IndexOf(';',nStartIndex) - nStartIndex;
						string strNumber = strReturn.Substring(nStartIndex,nLength);
						ch = (char)System.Convert.ToChar(System.Convert.ToInt32(strNumber));
						nLength++; // to skip past the ";"
					}
					else
					{
						ch = strReturn[nStartIndex];
						nLength = 1;
					}

					strOutput += ch;
					nStartIndex += nLength;
				} while(strReturn.Substring(nStartIndex,8) != "  <br>\r\n");
			}

			return strOutput;
		}

		protected override string GetConfigTypeName
		{
			get { return null; }
		}

		#endregion Abstract Base Class Overrides
	}
}
