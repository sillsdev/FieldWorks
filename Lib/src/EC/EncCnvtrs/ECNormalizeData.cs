#define v22_AllowEmptyReturn    // turn off the code that disallowed empty returns

using System;
using System.Text;
using System.Windows.Forms;             // for MessageBox (for showing compiler errors)
using System.Runtime.InteropServices;	// for ProgIdAttribute
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// This class is used to turn all the different EncodingForms allowed by encoding
	/// converters into an orthogonal array of bytes
	/// </summary>
	public class ECNormalizeData
	{
		public const int CCUnicode8 = 30;

		#region Interface
		// this is the helper method that returns the input data normalized
		internal static unsafe byte* GetBytes(string strInput, int cnCountIn, EncodingForm eEncFormIn, int nCodePageIn, EncodingForm eFormEngineIn, byte* pBuf, ref int nBufSize, ref bool bDebugDisplayMode)
		{
			// if the form the user gave is not what the engine wants (and it isn't legacy
			//  since legacy forms are already handled later)...
			if ((eEncFormIn != eFormEngineIn) && !EncConverter.IsLegacyFormat(eEncFormIn))
			{
				// we can do some of the conversions ourself. For example, if the input form
				//	is UTF16 and the desired form is UTF8, then simply use CCUnicode8 below
				if ((eEncFormIn == EncodingForm.UTF16) && (eFormEngineIn == EncodingForm.UTF8Bytes))
				{
					eEncFormIn = (EncodingForm)CCUnicode8;
				}
				// we can also do the following one
				else if ((eEncFormIn == EncodingForm.UTF8String) && (eFormEngineIn == EncodingForm.UTF8Bytes))
				{
					; // i.e. don't have TECkit do this one...
				}
				else
				{
					strInput = EncConverters.UnicodeEncodingFormConvertEx(strInput, eEncFormIn, cnCountIn, eFormEngineIn, NormalizeFlags.None, out cnCountIn);
					eEncFormIn = eFormEngineIn;
				}
			}

			int nInLen = 0;
			switch (eEncFormIn)
			{
				case EncodingForm.LegacyBytes:
				case EncodingForm.UTF8Bytes:
					{
						if (cnCountIn != 0)
							nInLen = cnCountIn; // item count should be the number of bytes directly.
						else
							// if the user didn't give the length (i.e. via ConvertEx), then get it
							//	from the BSTR length. nInLen will be the # of bytes.
							nInLen = strInput.Length * 2;

						// these forms are for C++ apps that want to use the BSTR to transfer
						//  bytes rather than OLECHARs.
						nInLen = StringToByteStar(strInput, pBuf, nInLen);

						if (eEncFormIn == EncodingForm.LegacyBytes)
							DisplayDebugCharValues(pBuf, nInLen, "Received (LegacyBytes) from client and sending to Converter/DLL...", ref bDebugDisplayMode);
						else
							DisplayDebugUCharValuesFromUTF8(pBuf, nInLen, "Received (UTF8Bytes) from client and sending to Converter/DLL...", ref bDebugDisplayMode);
						break;
					}
				case EncodingForm.LegacyString:
					{
						if (cnCountIn != 0)
							nInLen = cnCountIn;   // item count should be the number of bytes directly (after conversion below).
						else
							nInLen = strInput.Length; // the # of bytes will *be* the # of chars in the string after we're done.

						DisplayDebugUCharValues(strInput, "Received (LegacyString) from client...", ref bDebugDisplayMode);

						// use a code page converter to narrowize using the input string
						// (but the 'easier' Add method will send 0; if so, then
						//	fallback to the original method.
						byte[] ba = null;

						// first check if it's a symbol font (sometimes the user
						//	incorrectly sends a few spaces first, so check the
						//	first couple of bytes. If it is (and the code page is 0), then
						//  change the code page to be CP_SYMBOL
						if ((nCodePageIn == 0)
							&& (((strInput[0] & 0xF000) == 0xF000)
								|| ((strInput.Length > 1) && ((strInput[1] & 0xF000) == 0xF000))
								|| ((strInput.Length > 2) && ((strInput[2] & 0xF000) == 0xF000))
								)
							)
						{
							nCodePageIn = EncConverters.cnSymbolFontCodePage;
						}

						// if it's a symbol or iso-8859 encoding, then we can handle just
						//  taking the low byte (i.e. the catch case)
						if ((nCodePageIn == EncConverters.cnSymbolFontCodePage)
							|| (nCodePageIn == EncConverters.cnIso8859_1CodePage)
						)
						{
							try
							{
								Encoding enc = Encoding.GetEncoding(nCodePageIn);
								ba = enc.GetBytes(strInput);
							}
							catch
							{
								// for some reason, symbol fonts don't appear to be supported in
								//	.Net... Use cpIso8859 as the fallback
								// oops: cp8859 won't work for symbol data, so if GetBytes
								//  fails, just go back to stripping out the low byte as we had it
								//  originally. This'll work for both 8859 and symbol
								ba = new byte[nInLen];
								for (int i = 0; i < nInLen; i++)
									ba[i] = (byte)(strInput[i] & 0xFF);
							}
						}
						else
						{
							// otherwise, simply use CP_ACP (or the default code page) to
							//	narrowize it.
							Encoding enc = Encoding.GetEncoding(nCodePageIn);
							ba = enc.GetBytes(strInput);
						}

						// turn that byte array into a byte array...
						ByteArrToByteStar(ba, pBuf);

						if (cnCountIn != 0)
							nInLen = cnCountIn; // item count should be the number of bytes directly.
						else
							// if the user didn't give the length (i.e. via ConvertEx), then get it
							//	from the BSTR length. nInLen will be the # of bytes.
							nInLen = ba.Length;

						DisplayDebugCharValues(pBuf, nInLen, "Sending (LegacyBytes) to Converter/DLL...", ref bDebugDisplayMode);
						break;
					}
				// this following form *must* be widened UTF8 via the default code page
				case EncodingForm.UTF8String:
					{
						DisplayDebugUCharValues(strInput, "Received (UTF8String) from client...", ref bDebugDisplayMode);

						// use a code page converter to narrowize using the input string
						Encoding enc = Encoding.Default;
						byte[] ba = enc.GetBytes(strInput);

						// turn that byte array into a byte array...
						ByteArrToByteStar(ba, pBuf);

						if (cnCountIn != 0)
							nInLen = cnCountIn; // item count should be the number of bytes directly.
						else
							// if the user didn't give the length (i.e. via ConvertEx), then get it
							//	from the BSTR length. nInLen will be the # of bytes.
							nInLen = ba.Length;

						DisplayDebugUCharValuesFromUTF8(pBuf, nInLen, "Sending (UTF8Bytes) to Converter/DLL...", ref bDebugDisplayMode);
						break;
					}
				// this is a special case for CC where the input was actually UTF16, but the
				//	CC DLL is expecting (usually) UTF8, so convert from UTF16->UTF8 narrow
				case (EncodingForm)CCUnicode8:
					{
						DisplayDebugUCharValues(strInput, "Received (UTF16) from client...", ref bDebugDisplayMode);

						UTF8Encoding enc = new UTF8Encoding();
						byte[] ba = enc.GetBytes(strInput);

						// turn that byte array into a byte array...
						ByteArrToByteStar(ba, pBuf);

						// since we've changed the format, we don't care how many UTF16 words came in
						nInLen = ba.Length;

						DisplayDebugUCharValuesFromUTF8(pBuf, nInLen, "Sending (UTF8Bytes) to Converter/DLL...", ref bDebugDisplayMode);
						break;
					}
				case EncodingForm.UTF16:
					{
						if (cnCountIn != 0)
							nInLen = cnCountIn;   // item count should be the number of 16-bit words directly
						else
							nInLen = strInput.Length;

						DisplayDebugUCharValues(strInput, "Received (UTF16) from client and sending to Converter/DLL...", ref bDebugDisplayMode);

						// but this should be the count of bytes...
						nInLen *= 2;
						StringToByteStar(strInput, pBuf, nInLen);
						break;
					}
				case EncodingForm.UTF16BE:
				case EncodingForm.UTF32:
				case EncodingForm.UTF32BE:
					{
						if (cnCountIn != 0)
						{
							nInLen = cnCountIn; // item count is the number of Uni chars

							// for UTF32, the converter's actually expecting the length to be twice
							//	this much again.
							if (eEncFormIn != EncodingForm.UTF16BE)
								nInLen *= 2;
						}
						else
						{
							nInLen = strInput.Length;
						}

						DisplayDebugUCharValues(pBuf, nInLen, "Received (UTF16BE/32/32BE) from client/Sending to Converter/DLL...", ref bDebugDisplayMode);

						// for the byte count, double it (possibly again)
						nInLen *= 2;
						StringToByteStar(strInput, pBuf, nInLen);
						break;
					}

				default:
					EncConverters.ThrowError(ErrStatus.InEncFormNotSupported);
					break;
			}

			pBuf[nInLen] = pBuf[nInLen + 1] = pBuf[nInLen + 2] = pBuf[nInLen + 3] = 0;
			nBufSize = (int)nInLen;

			return pBuf;
		}

		internal static unsafe string GetString(byte* lpOutBuffer, int nOutLen, EncodingForm eOutEncodingForm, int nCodePageOut, EncodingForm eFormEngineOut, NormalizeFlags eNormalizeOutput, out int rciOutput, ref bool bDebugDisplayMode)
		{
			// null terminate the output and turn it into a (real) array of bytes
			lpOutBuffer[nOutLen] = lpOutBuffer[nOutLen + 1] = lpOutBuffer[nOutLen + 2] = lpOutBuffer[nOutLen + 3] = 0;
			byte[] baOut = new byte[nOutLen];
			ByteStarToByteArr(lpOutBuffer, nOutLen, baOut);

			// check to see if the engine handled the given output form. If not, then see
			//	if it's a conversion we can easily do (otherwise we'll ask TEC to do the
			//	conversion for us (later) so that all engines can handle all possible
			//	output encoding forms.
			if (eOutEncodingForm != eFormEngineOut)
			{
				if (EncConverter.IsLegacyFormat(eOutEncodingForm))
				{
					if ((eFormEngineOut == EncodingForm.LegacyBytes) && (eOutEncodingForm == EncodingForm.LegacyString))
					{
						// in this case, just *pretend* the engine outputs LegacyString (the
						//  LegacyString case below really means "convert LegacyBytes to
						//  LegacyString)
						eFormEngineOut = eOutEncodingForm;
					}
				}
				else    // unicode forms
				{
					// if the engine gives UTF8 and the client wants UTF16...
					if ((eOutEncodingForm == EncodingForm.UTF16) && (eFormEngineOut == EncodingForm.UTF8Bytes))
					{
						// use the special form to convert it below
						eOutEncodingForm = eFormEngineOut = (EncodingForm)CCUnicode8;
					}
					// or vise versa
					else if ((eFormEngineOut == EncodingForm.UTF16)
						&& ((eOutEncodingForm == EncodingForm.UTF8Bytes) || (eOutEncodingForm == EncodingForm.UTF8String)))
					{
						// engine gave UTF16, but user wants a UTF8 flavor.
						// Decoder d = Encoding.Unicode.GetChars(baOut);
						// d.GetChars(
						UTF8Encoding enc = new UTF8Encoding();
						baOut = enc.GetBytes(Encoding.Unicode.GetChars(baOut));
						eFormEngineOut = eOutEncodingForm;
						nOutLen = baOut.Length;
					}
					// these conversions we can do ourself
					else if ((eOutEncodingForm == EncodingForm.UTF8String)
						|| (eOutEncodingForm == EncodingForm.UTF16))
					{
						eFormEngineOut = eOutEncodingForm;
					}
				}
			}

			int nItems = 0, nCharsLen = 0;
			char[] caOut = null;
			switch (eFormEngineOut)
			{
				case EncodingForm.LegacyBytes:
				case EncodingForm.UTF8Bytes:
					{
						if (eFormEngineOut == EncodingForm.LegacyBytes)
							DisplayDebugCharValues(baOut, "Received (LegacyBytes) back from Converter/DLL (returning as LegacyBytes)...", ref bDebugDisplayMode);
						else
							DisplayDebugUCharValuesFromUTF8(baOut, "Received (UTF8Bytes) back from Converter/DLL (returning as UTF8Bytes)...", ref bDebugDisplayMode);

						// stuff the returned 'bytes' into the BSTR as narrow characters rather than
						//	converting to wide
						nItems = nOutLen;
						nCharsLen = (nOutLen + 1) / 2;
						caOut = new char[nCharsLen];
						ByteArrToCharArr(baOut, caOut);
						break;
					}
				case EncodingForm.LegacyString:
					{
						DisplayDebugCharValues(baOut, "Received (LegacyBytes) back from Converter/DLL (returning as LegacyString)...", ref bDebugDisplayMode);

						nCharsLen = nItems = nOutLen;

						try
						{
							// this will throw (for some reason) when doing symbol fonts
							//  (apparently, CP_SYMBOL is no longer supported).
							caOut = Encoding.GetEncoding(nCodePageOut).GetChars(baOut);
						}
						catch
						{
							if ((nCodePageOut == EncConverters.cnSymbolFontCodePage) || (nCodePageOut == EncConverters.cnIso8859_1CodePage))
							{
								char chMask = (char)0;
								if (nCodePageOut == EncConverters.cnSymbolFontCodePage)
									chMask = (char)0xF000;

								// do it the 'hard way'
								caOut = new char[nCharsLen];
								for (int i = 0; i < nCharsLen; i++)
									caOut[i] = (char)(baOut[i] | chMask);
							}
							else
								throw;
						}

						break;
					}
				case EncodingForm.UTF16:
					{
						nCharsLen = nItems = (nOutLen / 2);

						DisplayDebugUCharValues(baOut, "Received (UTF16) back from Converter/DLL (returning as UTF16)...", ref bDebugDisplayMode);

						caOut = Encoding.Unicode.GetChars(baOut);
						break;
					}
				case EncodingForm.UTF8String:
					{
						DisplayDebugUCharValuesFromUTF8(baOut, "Received (UTF8Bytes) back from Converter/DLL (returning as UTF8String)...", ref bDebugDisplayMode);

						// this encoding form is always encoded using the default code page.
						caOut = Encoding.Default.GetChars(baOut);

						nCharsLen = nItems = nOutLen;
						break;
					}
				case (EncodingForm)CCUnicode8:
					{
						DisplayDebugUCharValuesFromUTF8(baOut, "Received (UTF8Bytes) back from Converter/DLL (returning as UTF16)...", ref bDebugDisplayMode);

						caOut = Encoding.UTF8.GetChars(baOut);

						nCharsLen = nItems = caOut.Length;
						break;
					}
				case EncodingForm.UTF16BE:
				case EncodingForm.UTF32:
				case EncodingForm.UTF32BE:
					{
						nCharsLen = nItems = nOutLen / 2;

						DisplayDebugUCharValues(baOut, "Received (UTF16BE/32/32BE) back from Converter/DLL...", ref bDebugDisplayMode);

						caOut = new char[nCharsLen];
						ByteArrToCharArr(baOut, caOut);

						// for UTF32, it is half again as little in the item count.
						if (eFormEngineOut != EncodingForm.UTF16BE)
							nItems /= 2;
						break;
					}
				default:
					EncConverters.ThrowError(ErrStatus.OutEncFormNotSupported);
					break;
			}

#if !v22_AllowEmptyReturn
			if ((nCharsLen <= 0)
#if DEBUG
				|| (nCharsLen != caOut.Length)
#endif
)
			{
				EncConverters.ThrowError(ErrStatus.NoReturnDataBadOutForm);
			}
#endif

			// check to see if the engine handled the given output form. If not, then ask
			//	TEC to do the conversion for us so that all engines can handle all possible
			//	output encoding forms (e.g. caller requested utf32, but above CC could only
			//  give us utf16/8)
			// Also, if the caller wanted something other than "None" for the eNormalizeOutput,
			//  then we also have to call TEC for that as well (but I think this only makes
			//  sense if the output is utf16(be) or utf32(be))
			// p.s. if this had been a TEC converter, then the eNormalizeOutput flag would
			//  ahready have been reset to None (by this point), since we would have directly
			//  requested that normalized form when we created the converter--see
			//  TecEncConverter.PreConvert)
			string strOutput = new string(caOut);
			if ((eFormEngineOut != eOutEncodingForm)
				|| (eNormalizeOutput != NormalizeFlags.None))
			{
				strOutput = EncConverters.UnicodeEncodingFormConvertEx(strOutput, eFormEngineOut, nItems, eOutEncodingForm, eNormalizeOutput, out nItems);
			}

			DisplayDebugUCharValues(strOutput, "Returning back to client...", ref bDebugDisplayMode);

			rciOutput = nItems;
			return strOutput;
		}
		#endregion Interface

		#region Misc Unsafe Byte copying helpers
		[CLSCompliant(false)]
		public static unsafe void ByteStarToByteArr(byte* pSrc, int count, byte[] baDst)
		{
			// assume it already comes with the right size
			System.Diagnostics.Debug.Assert(baDst.Length == count);

			if (count == 0)
				return;

			// The following fixed statement pins the location of
			// the src and dst objects in memory so that they will
			// not be moved by garbage collection.
			fixed (byte* pDst = baDst)
			{
				byte* ps = pSrc;
				byte* pd = pDst;

				// Loop over the count in blocks of 4 bytes, copying an
				// integer (4 bytes) at a time:
				for (int n = count >> 2; n != 0; n--)
				{
					*((int*)pd) = *((int*)ps);
					pd += 4;
					ps += 4;
				}

				// Complete the copy by moving any bytes that weren't
				// moved in blocks of 4:
				for (count &= 3; count != 0; count--)
				{
					*pd = *ps;
					pd++;
					ps++;
				}
			}
		}

		// aka ByteStarToByteStar
		[CLSCompliant(false)]
		public static unsafe void MemMove(byte* pDst, byte* pSrc, int nCount)
		{
			if (nCount == 0)
				return;

			int count = nCount;
			bool bUsingStack = true;
			byte* ps = pSrc;
			byte* pd = stackalloc byte[count];   // start by assuming we'll need a buffer (since we can't put this in an 'if' stmt)

			// check whether these overlap and if not, then do it straight into the dest buffer
			if (Math.Abs(pDst - pSrc) >= count)
			{
				bUsingStack = false;
				pd = pDst;
			}

			// Loop over the count in blocks of 4 bytes, copying an
			// integer (4 bytes) at a time:
			for (int n = count >> 2; n != 0; n--)
			{
				*((int*)pd) = *((int*)ps);
				pd += 4;
				ps += 4;
			}

			// Complete the copy by moving any bytes that weren't
			// moved in blocks of 4:
			for (count &= 3; count != 0; count--)
			{
				*pd = *ps;
				pd++;
				ps++;
			}

			if (bUsingStack)
			{
				// then we have to move it back (ouch...)
				MemMove(pDst, pd - nCount, nCount);
			}
		}

		// same as above, but for when you don't know how long the byte* is (no strlen in C#!)
		[CLSCompliant(false)]
		public static unsafe byte[] ByteStarToByteArr(byte* pSrc)
		{
			int count = 0;
			for (; pSrc[count] != 0; count++)
				;   // noop

			byte[] baDst = new byte[count];
			ByteStarToByteArr(pSrc, count, baDst);
			return baDst;
		}

		[CLSCompliant(false)]
		public static unsafe void ByteArrToByteStar(byte[] baSrc, byte* pBuf)
		{
			int count = baSrc.Length;
			if (count == 0)
				return;

			// The following fixed statement pins the location of
			// the src and dst objects in memory so that they will
			// not be moved by garbage collection.
			fixed (byte* pSrc = baSrc)
			{
				byte* ps = pSrc;
				byte* pd = pBuf;
				// Loop over the count in blocks of 4 bytes, copying an
				// integer (4 bytes) at a time:
				for (int n = count >> 2; n != 0; n--)
				{
					*((int*)pd) = *((int*)ps);
					pd += 4;
					ps += 4;
				}
				// Complete the copy by moving any bytes that weren't
				// moved in blocks of 4:
				for (count &= 3; count != 0; count--)
				{
					*pd = *ps;
					pd++;
					ps++;
				}
			}
		}

		public static unsafe byte[] StringToByteArr(string strBytesString)
		{
			int nLengthBytes = strBytesString.Length * 2;
			byte* pBytes = stackalloc byte[nLengthBytes];
			nLengthBytes = StringToByteStar(strBytesString, pBytes, nLengthBytes);
			byte[] baOut = new byte[nLengthBytes];
			ByteStarToByteArr(pBytes, nLengthBytes, baOut);
			return baOut;
		}

		[CLSCompliant(false)]
		public static unsafe int StringToByteStar(string sSrc, byte* pBuf, int countBytes)
		{
			// keep track of the count so we can check the last byte
			int nCount = countBytes;
			if (nCount == 0)
				return nCount;

			// The following fixed statement pins the location of
			// the src and dst objects in memory so that they will
			// not be moved by garbage collection.
			fixed (char* pSrc = sSrc)
			{
				byte* ps = (byte*)pSrc;
				byte* pd = pBuf;
				// Loop over the countBytes in blocks of 4 bytes, copying an
				// integer (4 bytes) at a time:
				for (int n = countBytes >> 2; n != 0; n--)
				{
					*((int*)pd) = *((int*)ps);
					pd += 4;
					ps += 4;
				}
				// Complete the copy by moving any bytes that weren't
				// moved in blocks of 4:
				for (countBytes &= 3; countBytes != 0; countBytes--)
				{
					*pd = *ps;
					pd++;
					ps++;
				}

				// dilemma: it's possible that the user didn't make it an even number of
				//	bytes, in which case, this is actually one more than the actual
				//	length. otoh, apparently it is possible for this data to have '00'
				//	as a legitimate value (don't ask). At the very least check if the
				//	last byte is zero and if so, then reduce the count by one...
				if (((nCount % 2) == 0) && (pBuf[nCount - 2] != 0) && (pBuf[nCount - 1] == 0))
				{
					nCount--;
				}

				return nCount;
			}
		}

		public static string ByteArrToString(byte[] baSrc)
		{
			char[] caDst = new char[(baSrc.Length + 1) / 2];
			ByteArrToCharArr(baSrc, caDst);
			return new string(caDst);
		}

		private static unsafe void ByteArrToCharArr(byte[] baSrc, char[] caDst)
		{
			int count = baSrc.Length;
			if (count == 0)
				return;

			bool bOdd = ((baSrc.Length % 2) != 0);

			// The following fixed statement pins the location of
			// the src and dst objects in memory so that they will
			// not be moved by garbage collection.
			fixed (byte* pSrc = baSrc)
			fixed (char* pDst = caDst)
			{
				byte* ps = pSrc;
				byte* pd = (byte*)pDst;
				// Loop over the count in blocks of 4 bytes, copying an
				// integer (4 bytes) at a time:
				for (int n = count >> 2; n != 0; n--)
				{
					*((int*)pd) = *((int*)ps);
					pd += 4;
					ps += 4;
				}
				// Complete the copy by moving any bytes that weren't
				// moved in blocks of 4:
				for (count &= 3; count != 0; count--)
				{
					*pd = *ps;
					pd++;
					ps++;
				}

				// add an extra zero if odd length
				if (bOdd)
				{
					*pd = 0;
				}
			}
		}
		#endregion Misc Unsafe Byte copying helpers

		#region Debug helper
		private unsafe static void DisplayDebugCharValues(byte[] baInputString, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			int nLengthBytes = baInputString.Length;
			fixed (byte* lpszInputString = baInputString)
				DisplayDebugCharValues(lpszInputString, nLengthBytes, strCaption, ref bDebugDisplayMode);
		}

		private unsafe static void DisplayDebugCharValues(byte* lpszInputString, int nLengthBytes, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			string strWhole = null, strPiece = null;
			for (int i = 0; i < nLengthBytes; i++)
			{
				strPiece = String.Format("d{0:###} ", lpszInputString[i]);
				strWhole += strPiece;
			}

			string strMessage = String.Format("Character values: {0}\n\nCopy to the Clipboard?", strWhole);
			DialogResult ret = MessageBox.Show(strMessage, strCaption, MessageBoxButtons.YesNoCancel);
			if (ret == DialogResult.Yes)
			{
				// 'Yes' means put it on the clipboard
				Clipboard.SetDataObject(strWhole, true);
			}
			else if (ret == DialogResult.Cancel)
			{
				// 'Cancel' means stop!
				bDebugDisplayMode = false;
			}
		}

		private unsafe static void DisplayDebugUCharValuesFromUTF8(byte[] baInputString, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			DisplayDebugUCharValues(new string(Encoding.UTF8.GetChars(baInputString)), strCaption, ref bDebugDisplayMode);
		}

		private unsafe static void DisplayDebugUCharValuesFromUTF8(byte* lpszInputString, int nLengthBytes, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			byte[] baOut = new byte[nLengthBytes];
			ByteStarToByteArr(lpszInputString, nLengthBytes, baOut);
			DisplayDebugUCharValuesFromUTF8(baOut, strCaption, ref bDebugDisplayMode);
		}

		private unsafe static void DisplayDebugUCharValues(byte* lpszInputString, int nLengthWords, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			int nLengthBytes = nLengthWords * 2;
			byte[] baOut = new byte[nLengthBytes];
			ByteStarToByteArr(lpszInputString, nLengthBytes, baOut);
			DisplayDebugUCharValues(baOut, strCaption, ref bDebugDisplayMode);
		}

		private unsafe static void DisplayDebugUCharValues(byte[] baInputString, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			DisplayDebugUCharValues(ByteArrToString(baInputString), strCaption, ref bDebugDisplayMode);
		}

		private static void DisplayDebugUCharValues(string strInputString, string strCaption, ref bool bDebugDisplayMode)
		{
			if (!bDebugDisplayMode)
				return;

			string strWhole = null, strPiece = null, strUPiece = null;
			foreach (char ch in strInputString)
			{
				if (ch == 0)   // sometimes it's null (esp. for utf32)
					strPiece = "nul (u0000)  ";
				else
				{
					strUPiece = String.Format("{0:X}", (int)ch);

					// left pad with 0's (there may be a better way to do this, but
					//  I don't know what it is)
					while (strUPiece.Length < 4) strUPiece = "0" + strUPiece;

					strPiece = String.Format("{0:#} (u{1,4})  ", ch, strUPiece);
				}
				strWhole += strPiece;
			}

			string strMessage = String.Format("Character values: {0}\n\nCopy to the Clipboard?", strWhole);
			DialogResult ret = MessageBox.Show(strMessage, strCaption, MessageBoxButtons.YesNoCancel);
			if (ret == DialogResult.Yes)
			{
				// 'Yes' means put it on the clipboard
				Clipboard.SetDataObject(strWhole, true);
			}
			else if (ret == DialogResult.Cancel)
			{
				// 'Cancel' means stop!
				bDebugDisplayMode = false;
			}
		}
		#endregion Debug helper
	}
}
