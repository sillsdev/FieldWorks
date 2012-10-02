using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;       // for Dictionary<>
using Microsoft.Win32;                  // for RegistryKey
using System.Diagnostics;               // for Debug
using System.IO;                        // for file I/O
using System.Reflection;				// for Assembly
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// Managed EncConverter for AdaptIt Knowledge Base data
	/// </summary>
	[GuidAttribute("13F51827-F675-43f5-BD9B-F0C164D6ECB7")]
	public class AdaptItGuesserEncConverter : AdaptItKBReader
	{
		public const string strDisplayName = "Target Word Guesser for AdaptIt";
		public const string strHtmlFilename = "AdaptIt Guesser Plug-in About box.mht";

		public override string strRegValueForConfigProgId
		{
			get { return typeof(AdaptItGuesserEncConverterConfig).AssemblyQualifiedName; }
		}

		#region Initialization
		public AdaptItGuesserEncConverter() : base(typeof(AdaptItGuesserEncConverter).FullName,EncConverters.strTypeSILadaptitGuesser)
		{
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding);

			if (bAdding)
			{
				// the only thing we want to add (now that the convType can be less than accurate)
				//  is to make sure it's bidirectional)
				if (!EncConverters.IsUnidirectional(conversionType))
				{
					switch (conversionType)
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
				}
			}
		}

		protected unsafe override bool Load()
		{
			if (base.Load())
			{
				fixed (void* pThis = this.Name)
				{
					// we only deal with single word "phrases"
					Dictionary<string,string> mapLookup;
					if (!m_mapOfMaps.TryGetValue(1, out mapLookup))
						return true;    // no words to deal with

					if (m_bLegacy)
					{
						ResetLegacyCorpus(pThis);
						foreach (KeyValuePair<string, string> kvp in mapLookup)
						{
							byte[] abyKey = Encoding.Default.GetBytes(kvp.Key);
							byte[] abyValue = Encoding.Default.GetBytes(kvp.Value);
							fixed (byte* pKey = abyKey)
							fixed (byte* pValue = abyValue)
							{
								AddPairToCorpus(pThis, pKey, pValue);
							}
						}
						return true;
					}
					else
					{
						ResetCorpus(pThis);
						foreach (KeyValuePair<string, string> kvp in mapLookup)
						{
							fixed (char* pKey = kvp.Key)
							fixed (char* pValue = kvp.Value)
							{
								AddPairToCorpus(pThis, pKey, pValue);
							}
						}
						return true;
					}
				}
			}

			return false;
		}
		#endregion Initialization

		#region DLLImport Statements

		[DllImport("GuesserEC", CharSet = CharSet.Unicode)]
		static extern unsafe int AddPairToCorpus(
			void* lpThis,
			char* lpszSrcString,
			char* lpszTgtString);

		[DllImport("GuesserECLegacy", CharSet = CharSet.Ansi)]
		static extern unsafe int AddPairToCorpus(
			void* lpThis,
			byte* lpszSrcString,
			byte* lpszTgtString);

		[DllImport("GuesserEC", CharSet = CharSet.Unicode)]
		static extern unsafe int MakeAGuess(
			void* lpThis,
			char* lpszInputString,
			char* lpszOutputString,
			int*  pnNumCharsOut);

		[DllImport("GuesserECLegacy", CharSet = CharSet.Ansi)]
		static extern unsafe int MakeAGuess(
			void* lpThis,
			byte* lpszInputString,
			byte* lpszOutputString,
			int* pnNumCharsOut);

		[DllImport("GuesserEC", CharSet = CharSet.Unicode)]
		static extern unsafe int ResetCorpus(void* lpThis);

		[DllImport("GuesserECLegacy", CharSet = CharSet.Ansi, EntryPoint="ResetCorpus")]
		static extern unsafe int ResetLegacyCorpus(void* lpThis);

		#endregion DLLImport Statements

		#region Abstract Base Class Overrides
		[CLSCompliant(false)]
		protected override unsafe void DoConvert
			(
			byte* lpInBuffer,
			int nInLen,
			byte* lpOutBuffer,
			ref int rnOutLen
			)
		{
			fixed (void* pThis = this.Name)
			fixed (int* pnOut = &rnOutLen)
			{
				if (m_bLegacy)
				{
					MakeAGuess(pThis, (byte*)lpInBuffer, (byte*)lpOutBuffer, pnOut);
				}
				else
				{
					rnOutLen /= 2;  // number of wide characters
					MakeAGuess(pThis, (char*)lpInBuffer, (char*)lpOutBuffer, pnOut);
					rnOutLen *= 2;  // back to number of bytes
				}
			}
		}

		#endregion Abstract Base Class Overrides
	}
}
