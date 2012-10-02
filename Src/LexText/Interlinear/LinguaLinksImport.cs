// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LinguaLinksImport.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.LexText.Controls;
using ECInterfaces;
using SilEncConverters31;
using System.Data.SqlClient;


namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LinguaLinksImport
	{
		private int m_phaseProgressStart, m_phaseProgressEnd, m_shownProgress;
		private IAdvInd m_ai;
		private string m_sErrorMsg;
		private bool m_fCancel;
		private LanguageMapping[] m_languageMappings;
		private LanguageMapping m_current;
		private EncConverters m_converters;
		private string m_nextInput;
		private string m_sTempDir;
		private string m_sRootDir;
		private FdoCache m_cache;
		private string m_LinguaLinksXmlFileName;

		public delegate void ErrorHandler(object sender, string message, string caption);
		public event ErrorHandler Error;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LinguaLinksImport"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="tempDir">The temp directory.</param>
		/// <param name="rootDir">The root directory.</param>
		/// ------------------------------------------------------------------------------------
		public LinguaLinksImport(FdoCache cache, string tempDir, string rootDir)
		{
			m_cache = cache;
			m_sTempDir = tempDir;
			m_sRootDir = rootDir;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the error message.
		/// </summary>
		/// <value>The error message.</value>
		/// ------------------------------------------------------------------------------------
		public string ErrorMessage
		{
			get { return m_sErrorMsg; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the next input.
		/// </summary>
		/// <value>The next input.</value>
		/// ------------------------------------------------------------------------------------
		public string NextInput
		{
			get { return m_nextInput; }
			set { m_nextInput = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicks the cancel button on the progress dialog.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// ------------------------------------------------------------------------------------
		public void On_ProgressDlg_Cancel(object sender)
		{
			// TODO make the cancel work
			// TODO make the cancel button disappear
			m_fCancel = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the import.
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion flag, 2) array of
		/// LanguageMappings, 3) start phase.</param>
		/// <returns>Returns <c>true</c> if we did the complete import, false if we
		/// quit early.</returns>
		/// ------------------------------------------------------------------------------------
		public object Import(IAdvInd4 dlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			bool runToCompletion = (bool)parameters[0];
			m_languageMappings = (LanguageMapping[])parameters[1];
			int startPhase = (int)parameters[2];
			m_ai = dlg as IAdvInd;
			m_LinguaLinksXmlFileName = m_nextInput;

			m_sErrorMsg = ITextStrings.ksTransformProblem;
			m_shownProgress = m_phaseProgressStart = 0;
			m_phaseProgressEnd = 150;
			if (startPhase < 2)
			{
				dlg.Title = ITextStrings.ksLLImportProgress;
				dlg.Message = ITextStrings.ksLLImportPhase1;
				m_sErrorMsg = ITextStrings.ksTransformProblem1;
				if (!Convert1())
					return false;
			}
			else
			{
				m_ai.Step(150);
			}
			if (m_fCancel)
				return false;

			if (startPhase < 3)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem2;
				dlg.Message = ITextStrings.ksLLImportPhase2;
				Convert2();
			}
			m_ai.Step(75);
			if (m_fCancel)
				return false;

			if (startPhase < 4)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem3;
				dlg.Message = ITextStrings.ksLLImportPhase3;
				Convert3();
			}
			m_ai.Step(75);
			if (m_fCancel)
				return false;

			// There should be some contents in the phase 3 file if
			// the process is valid and is using a valid file.
			// Make sure the file isn't empty and show msg if it is.
			FileInfo fi = new FileInfo(m_sTempDir + "LLPhase3Output.xml");
			if (fi.Length == 0)
			{
				ReportError(String.Format(ITextStrings.ksInvalidLLFile, m_LinguaLinksXmlFileName),
					ITextStrings.ksLLImport);
				throw new InvalidDataException();
			}

			// There's no way to cancel from here on out.
			if (dlg is ProgressDialogWithTask)
				((ProgressDialogWithTask)dlg).CancelButtonVisible = false;

			if (runToCompletion)
			{
				m_sErrorMsg = ITextStrings.ksXMLParsingProblem4;
				dlg.Message = ITextStrings.ksLLImportPhase4;
				if (Convert4())
				{

					m_sErrorMsg = ITextStrings.ksFinishLLTextsProblem5;
					dlg.Message = ITextStrings.ksLLImportPhase5;
					m_shownProgress = m_phaseProgressStart = dlg.Position;
					m_phaseProgressEnd = 500;
					Convert5();
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Import a file which looks like a FieldWorks interlinear XML export.
		/// </summary>
		/// <param name="dlg"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public object ImportInterlinear(IAdvInd4 dlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			m_nextInput = (string)parameters[0];
			DoTransform(m_sRootDir + "FWExport2FWDump.xsl", m_nextInput, m_sTempDir + "IIPhaseOneOutput.xml");
			if (m_fCancel)
				return false;
			m_nextInput = m_sTempDir + "IIPhaseOneOutput.xml";
			m_sErrorMsg = ITextStrings.ksInterlinImportErrorPhase1;
			dlg.Message = ITextStrings.ksInterlinImportPhase1of2;
			if (Convert4())
			{
				m_sErrorMsg = ITextStrings.ksInterlinImportErrorPhase2;
				dlg.Message = ITextStrings.ksInterlinImportPhase2of2;
				m_shownProgress = dlg.Position;
				m_phaseProgressEnd = 500;
				Convert5();
				return true;
			}
			return false;
		}

			enum modes { kStart, kRun1, kRun2, kRun3, kRun4, kAUni1, kAUni2, kAUni3, kAUni4, kAStr1, kAStr2, kAStr3, kRIE1, kRIE2, kRIE3, kRIE4, kRIE5, kRIE6, kRIE7, kLink1, kLink2, kLink3, kLink4, kLink5, kLinkA2, kLinkA3, kLinkA4, kICU1, kICU2, kDtd };

		private void ProcessSearchBuffer(string searchBuffer, int size, bool bufferIt, ref string buffer, BinaryWriter bw)
		{
			for (int j = 0; j < size; j++)
			{
				if (bufferIt)
				{
					buffer += searchBuffer[j];
				}
				else
				{
					bw.Write((byte)searchBuffer[j]);
				}
			}
		}

		private void ProcessLanguageCode(string buffer, BinaryWriter bw)
		{
			bool found = false;

			foreach (LanguageMapping mapping in m_languageMappings)
			{
				if (!found && mapping.LlCode == buffer)
				{
					found = true;
					for (int j = 0; j < mapping.FwCode.Length; j++)
					{
						bw.Write((byte)mapping.FwCode[j]);
					}
					m_current = mapping;
				}
			}
			//We shouldn't need the following code, but sometimes LL dumps unexpected language codes
			if (!found)
			{
				for (int j = 0; j < buffer.Length; j++)
				{
					bw.Write((byte)buffer[j]);
				}
				if (m_languageMappings.Length > 0)
					m_current = m_languageMappings[0];
			}
		}

		private void ProcessLanguageCode2(string buffer)
		{
			bool found = false;

			foreach (LanguageMapping mapping in m_languageMappings)
			{
				if (!found && mapping.LlCode == buffer)
				{
					found = true;
					m_current = mapping;
				}
			}
			//We shouldn't need the following code, but sometimes LL dumps unexpected language codes
			if (!found && m_languageMappings.Length > 0)
				m_current = m_languageMappings[0];
		}

		private void ProcessLanguageData(string buffer, BinaryWriter bw)
		{
			if (buffer.Length > 0)
			{
				IEncConverter converter = null;
				string result = string.Empty;

				if (m_current.EncodingConverter != null && m_current.EncodingConverter.Length > 0)
				{
					converter = m_converters[m_current.EncodingConverter];
				}

				if (converter != null)
				{
					// Replace any make sure the &lt; &gt; &amp; and &quot;
					string[] specialEntities = new string[] { "&lt;", "&gt;", "&quot;", "&amp;"};
					string[] actualXML = new string[] { "<", ">", "\"", "&"};
					bool[] replaced = new bool[] { false, false, false, false };
					bool anyReplaced = false;
					Debug.Assert(specialEntities.Length == actualXML.Length && actualXML.Length == replaced.Length, "Programming error...");

					StringBuilder sb = new StringBuilder(buffer);	// use a string builder for performance
					for (int i = 0; i < specialEntities.Length; i++)
					{
						if (buffer.Contains(specialEntities[i]))
						{
							replaced[i] = anyReplaced = true;
							sb = sb.Replace(specialEntities[i], actualXML[i]);
						}
					}

					int len = sb.Length;	// buffer.Length;
					byte[] subData = new byte[len];
					for (int j = 0; j < len; j++)
					{
						subData[j] = (byte)sb[j];	// buffer[j];
					}

					try
					{
						result = converter.ConvertToUnicode(subData);
					}
					catch (System.Exception e)
					{
						ReportError(string.Format(ITextStrings.ksEncConvFailed,
							converter.Name, e.Message), ITextStrings.ksLLEncConv);
					}

					// now put any of the four back to the Special Entity notation
					if (anyReplaced)	// only if we changed on input
					{
						sb = new StringBuilder(result);
						for (int i = specialEntities.Length-1; i >= 0; i--)
						{
							if (replaced[i])
							{
								sb = sb.Replace(actualXML[i], specialEntities[i]);
							}
						}
						result = sb.ToString();
					}
				}
				else
				{
					result = buffer;
				}
				for (int j = 0; j < result.Length; j++)
				{
					if (128 > (ushort)result[j])
					{
						//0XXX XXXX
						bw.Write((byte)result[j]);
					}
					else if (2048 > (ushort)result[j])
					{
						//110X XXXX 10XX XXXX
						bw.Write((byte)(192 + ((ushort)result[j]) / 64));
						bw.Write((byte)(128 + (((ushort)result[j]) & 63)));
					}
					else
					{
						//1110 XXXX 10XX XXXX 10XX XXXX}
						bw.Write((byte)(224 + ((ushort)result[j]) / 4096));
						bw.Write((byte)(128 + (((ushort)result[j]) & 4095) / 64));
						bw.Write((byte)(128 + (((ushort)result[j]) & 63)));
					}
				}
			}
		}

		private void InitializeSearches(string[] searches)
		{
			searches[0] = ">Dummy>";
			searches[1] = "<Run";
			searches[2] = "<AUni";
			searches[3] = "<ReversalIndexEntry";
			searches[4] = "<AStr";
			searches[5] = "<Link ";
			searches[6] = "<ICULocale24>";
			searches[7] = "<!DOCTYPE";
		}

		private bool Convert1()
		{
			const int maxSearch = 8;

			char c;
			string[] searches = new string[maxSearch];
			string buffer, searchBuffer, rieBufferLangData, rieBufferXML;
			int[] locs = new int[maxSearch];
			modes mode;
			bool bufferIt;
			int j;
			bool found;
			bool okay = true;

			if (!File.Exists(m_nextInput))
			{
				ReportError(string.Format(ITextStrings.ksInputFileNotFound, m_nextInput),
					ITextStrings.ksLLEncConv);
				return false;
			}
			m_converters = new EncConverters();
			FileStream fsi = new FileStream(m_nextInput, FileMode.Open, FileAccess.Read);
			FileStream fso = new FileStream(m_sTempDir + "LLPhase1Output.xml", FileMode.Create, FileAccess.Write);
			BinaryReader br = new BinaryReader(fsi);
			BinaryWriter bw = new BinaryWriter(fso);
			buffer = string.Empty;
			searchBuffer = string.Empty;
			rieBufferXML = string.Empty;
			rieBufferLangData = string.Empty;

			for (int i = 0; i < m_languageMappings.Length; i++)
			{
				LanguageMapping mapping = m_languageMappings[i];
				if (mapping.FwName == ITextStrings.ksIgnore || mapping.FwCode == string.Empty)
				{
					mapping.FwName = "zzzIgnore";
					mapping.FwCode = "zzzIgnore";
				}
			}


			//Start
			mode = modes.kStart;
			InitializeSearches(searches);
			for (j = 0; j < maxSearch; j++)
			{
				locs[j] = 0;
			}
			bufferIt = false;

			while (okay && fsi.Position < fsi.Length)
			{
				int m_currentProgress = m_phaseProgressStart + (int)Math.Floor((double)((m_phaseProgressEnd
					- m_phaseProgressStart) * fsi.Position / fsi.Length));
				if (m_currentProgress > m_shownProgress && m_currentProgress <= m_phaseProgressEnd)
				{
					m_ai.Step(m_currentProgress - m_shownProgress);
					m_shownProgress = m_currentProgress;
				}
				c = (char)br.ReadByte();
				found = false;
				int TempDiff = 0;
				for (j = 0; j < maxSearch; j++)
				{
					if (c == searches[j][locs[j]])
					{
						found = true;
						locs[j]++;
						if (locs[j] > TempDiff)
						{
							TempDiff = locs[j];
						}
					}
					else
					{
						locs[j] = 0;
					}
				}
				if (found)
				{
					searchBuffer += c;
					TempDiff = searchBuffer.Length - TempDiff;
					if (TempDiff > 0)
					{
						ProcessSearchBuffer(searchBuffer, TempDiff, bufferIt, ref buffer, bw);
						searchBuffer = searchBuffer.Substring(TempDiff, searchBuffer.Length - TempDiff);
					}
					if ((locs[0] == searches[0].Length) && (locs[0] == searchBuffer.Length))
					{
						string s = string.Format(ITextStrings.ksExpectedXButGotY, searches[1], searches[0]);
						ReportError(s, ITextStrings.ksLLEncConv);
						s = "</Error>Parsing Error.  " + s;

						for (j = 0; j < s.Length; j++)
						{
							bw.Write((byte)s[j]);
						}
						bw.Close();
						fso.Close();
						okay = false;
					}
					else if ((locs[1] == searches[1].Length) && (locs[1] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <Run
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</Run>";
							searches[1] = "ws=\"";
							mode = modes.kRun1;
						}
						else if (mode == modes.kRun1)
						{
							// <Run ws="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</Run>";
							searches[1] = "\"";
							mode = modes.kRun2;
						}
						else if (mode == modes.kRun2)
						{
							// <Run ws="en"
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</Run>";
							searches[1] = ">";
							mode = modes.kRun3;
						}
						else if (mode == modes.kRun3)
						{
							// <Run ws="en">
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<Run>";
							searches[1] = "</Run>";
							mode = modes.kRun4;
						}
						else if (mode == modes.kRun4)
						{
							// <Run ws="en">Data</Run>
							ProcessLanguageData(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kAUni1)
						{
							// <AUni ws="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</AUni>";
							searches[1] = "\"";
							mode = modes.kAUni2;
						}
						else if (mode == modes.kAUni2)
						{
							// <AUni ws="en"
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</AUni>";
							searches[1] = ">";
							mode = modes.kAUni3;
						}
						else if (mode == modes.kAUni3)
						{
							// <AUni ws="en">
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<AUni>";
							searches[1] = "</AUni>";
							mode = modes.kAUni4;
						}
						else if (mode == modes.kAUni4)
						{
							// <AUni ws="en">Data</AUni>
							ProcessLanguageData(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kAStr1)
						{
							// <AStr ws="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<Run>";
							searches[1] = "\"";
							mode = modes.kAStr2;
						}
						else if (mode == modes.kAStr2)
						{
							// <AStr ws="en"
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<Run>";
							searches[1] = ">";
							mode = modes.kAStr3;
						}
						else if (mode == modes.kAStr3)
						{
							// <AStr ws="en">
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kRIE1)
						{
							// <ReversalIndexEntry ... <Uni>
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</ReversalIndexEntry>";
							searches[1] = "</Uni>";
							mode = modes.kRIE2;
						}
						else if (mode == modes.kRIE2)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni>
							rieBufferLangData = buffer;
							bufferIt = true;
							buffer = string.Empty;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</ReversalIndexEntry>";
							searches[1] = "<WritingSystem5053>";
							mode = modes.kRIE3;
						}
						else if (mode == modes.kRIE3)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053>
							bufferIt = true;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</ReversalIndexEntry>";
							searches[1] = "<Link";
							mode = modes.kRIE4;
						}
						else if (mode == modes.kRIE4)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053> ... <Link
							bufferIt = true;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</WritingSystem5053>";
							searches[1] = "ws=\"";
							mode = modes.kRIE5;
						}
						else if (mode == modes.kRIE5)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053> ... <ws="
							bufferIt = true;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							rieBufferXML = buffer;
							buffer = string.Empty;
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</WritingSystem5053>";
							searches[1] = "\"";
							mode = modes.kRIE6;
						}
						else if (mode == modes.kRIE6)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053> ... <Link ws="en"
							ProcessLanguageCode2(buffer);
							if (m_current.FwCode != null)
								rieBufferXML += m_current.FwCode;
							ProcessLanguageData(rieBufferLangData, bw);
							bufferIt = false;
							ProcessSearchBuffer(rieBufferXML, rieBufferXML.Length, bufferIt, ref buffer, bw);
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</Entries5052>";
							searches[1] = "</WritingSystem5053>";
							mode = modes.kRIE7;
						}
						else if (mode == modes.kRIE7)
						{
							// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053> ... <Link ws="en" ... </WritingSystem5053>
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kLink1)
						{
							// <Link target="XXXXXXX" ws="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "/>";
							searches[1] = "\"";
							mode = modes.kLink2;
						}
						else if (mode == modes.kLink2)
						{
							// <Link target="XXXXXXX" ws="en"
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "\"";
							searches[2] = "/>";
							mode = modes.kLink3;
						}
						else if (mode == modes.kLink3)
						{
							// <Link target="XXXXXXX" ws="en" form="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "/>";
							searches[1] = "\"";
							mode = modes.kLink4;
						}
						else if (mode == modes.kLink4)
						{
							// <Link target="XXXXXXX" ws="en" form="Data"
							ProcessLanguageData(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "\"";
							searches[2] = "/>";
							mode = modes.kLink3;
						}
						else if (mode == modes.kLinkA2)
						{
							// <Link target="XXXXXXX" wsa="en"
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "\"";
							searches[2] = "/>";
							searches[3] = "wsv=\"";
							mode = modes.kLinkA3;
						}
						else if (mode == modes.kLinkA3)
						{
							// <Link target="XXXXXXX" wsa="en" abbr="
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "/>";
							searches[1] = "\"";
							mode = modes.kLinkA4;
						}
						else if (mode == modes.kLinkA4)
						{
							// <Link target="XXXXXXX" wsa="en" form="Data"
							ProcessLanguageData(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "\"";
							searches[2] = "/>";
							searches[3] = "wsv=\"";
							mode = modes.kLinkA3;
						}
						else if (mode == modes.kICU1)
						{
							// <ICULocale24> <Uni>
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</ICULocale24>";
							searches[1] = "</Uni>";
							mode = modes.kICU2;
						}
						else if (mode == modes.kICU2)
						{
							// <ICULocale24> <Uni>en</Uni>
							ProcessLanguageCode(buffer, bw);
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kDtd)
						{
							// <!DOCTYPE ... >
							ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[2] == searches[2].Length) && (locs[2] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <AUni
							ProcessSearchBuffer(searchBuffer, locs[2], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</AUni>";
							searches[1] = "ws=\"";
							mode = modes.kAUni1;
						}
						else if ((mode == modes.kLink1) || (mode == modes.kLink3) || (mode == modes.kLinkA3))
						{
							// <Link ... /> or <Link ... ws="en" ... />
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[2], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						else if (mode == modes.kRIE1)
						{
							// <ReversalIndexEntry ... </ReversalIndexEntry>
							bufferIt = false;
							ProcessSearchBuffer(searchBuffer, locs[2], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							InitializeSearches(searches);
							mode = modes.kStart;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[3] == searches[3].Length) && (locs[3] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <ReversalIndexEntry
							ProcessSearchBuffer(searchBuffer, locs[3], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "<Uni>";
							searches[2] = "</ReversalIndexEntry>";
							mode = modes.kRIE1;
						}
						else if (mode == modes.kLink1)
						{
							// <Link target="XXXXXXX" wsa="
							ProcessSearchBuffer(searchBuffer, locs[3], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "/>";
							searches[1] = "\"";
							mode = modes.kLinkA2;
						}
						else if (mode == modes.kLinkA3)
						{
							// <Link target="XXXXXXX" wsa="en" abbr="llcr" wsv="
							ProcessSearchBuffer(searchBuffer, locs[3], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = true;
							buffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "/>";
							searches[1] = "\"";
							mode = modes.kLinkA2;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[4] == searches[4].Length) && (locs[4] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <AStr
							ProcessSearchBuffer(searchBuffer, locs[4], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<Run>";
							searches[1] = "ws=\"";
							mode = modes.kAStr1;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[5] == searches[5].Length) && (locs[5] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <Link
							ProcessSearchBuffer(searchBuffer, locs[5], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[1] = "ws=\"";
							searches[2] = "/>";
							searches[3] = "wsa=\"";
							mode = modes.kLink1;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[6] == searches[6].Length) && (locs[6] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <ICULocale24>
							ProcessSearchBuffer(searchBuffer, locs[6], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							bufferIt = false;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "</ICULocale24>";
							searches[1] = "<Uni>";
							mode = modes.kICU1;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
					else if ((locs[7] == searches[7].Length) && (locs[7] == searchBuffer.Length))
					{
						if (mode == modes.kStart)
						{
							// <!DOCTYPE
							bufferIt = true;
							ProcessSearchBuffer(searchBuffer, locs[7], bufferIt, ref buffer, bw);
							searchBuffer = string.Empty;
							for (j = 0; j < maxSearch; j++)
							{
								searches[j] = ">Dummy>";
							}
							searches[0] = "<";
							searches[1] = ">";
							mode = modes.kDtd;
						}
						for (j = 0; j < maxSearch; j++)
						{
							locs[j] = 0;
						}
					}
				}
				else
				{
					if (searchBuffer.Length > 0)
					{
						ProcessSearchBuffer(searchBuffer, searchBuffer.Length, bufferIt, ref buffer, bw);
						searchBuffer = string.Empty;
					}
					if (bufferIt)
					{
						buffer += c;
					}
					else
					{
						bw.Write((byte)c);
					}
				}
			}
			if (okay)
			{
				if (searchBuffer.Length > 0)
				{
					ProcessSearchBuffer(searchBuffer, searchBuffer.Length, bufferIt, ref buffer, bw);
				}
				bw.Close();
				br.Close();
				fsi.Close();
				fso.Close();
				m_nextInput = m_sTempDir + "LLPhase1Output.xml";
			}
			return okay;
		}

		private void DoTransform(string xsl, string xml, string outputName)
		{
			XmlTextWriter writer = null;
			try
			{
				Encoding Utf8 = new UTF8Encoding(false);

				//Create the XslTransform and load the stylesheet.
				XslCompiledTransform xslt = new XslCompiledTransform();
				xslt.Load(xsl, XsltSettings.TrustedXslt, null);

				//Load the XML data file.
				XPathDocument doc = new XPathDocument(xml);

				//Create an XmlTextWriter to output to the appropriate file. First make sure the expected
				//directory exists.
				if (!Directory.Exists(Path.GetDirectoryName(outputName)))
					Directory.CreateDirectory(Path.GetDirectoryName(outputName));
				File.Delete(outputName); // prevents re-importing the previous file if first step fails.
				writer = new XmlTextWriter(outputName, Utf8);
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 2;

				//Transform the file.
				xslt.Transform(doc.CreateNavigator(), null, writer);
				writer.Close();
			}
			catch (Exception ex)
			{
				// For some reason during debugging of LLImportPhase1.xsl we hit an exception here
				// with an error saying "Common Language Runtime detected an invalid program".
				// It seems to be choking over the ThesaurusItems5016 template. Yet when the program
				//  is run outside the debugger, it doesn't hit this. The transform also works fine via MSXSL.???
				ReportError(ex.Message, ITextStrings.ksLLEncConv);
				m_fCancel = true; // LT-7223 transform was failing with an invalid character and then going on!
				if (writer != null)
					writer.Close(); // Otherwise the next pass will fail to open the file.
				//Console.WriteLine("{0} Exception caught.", ex);
				//Console.ReadLine();
			}

		}

		private void Convert2()
		{
			DoTransform(m_sRootDir + "LLImportPhase1.xsl", m_nextInput, m_sTempDir + "LLPhase2Output.xml");
			m_nextInput = m_sTempDir + "LLPhase2Output.xml";
		}

		private void Convert3()
		{
			DoTransform(m_sRootDir + "LLImportPhase2.xsl", m_nextInput, m_sTempDir + "LLPhase3Output.xml");
			m_nextInput = m_sTempDir + "LLPhase3Output.xml";
		}

		private bool Convert4()
		{
			try
			{
				IFwXmlData2 fwxd2 = FwXmlDataClass.Create();
				fwxd2.Open(m_cache.ServerName, m_cache.DatabaseName);
				fwxd2.ImportMultipleXmlFields(m_nextInput, m_cache.LangProject.Hvo, m_ai);
				// no need to call ReleaseComObject here - GC will take care of that
				return true;
			}
			catch
			{
				string sLogFile = m_sTempDir + "LLPhase3Output-Import.log";
				ReportError(string.Format(ITextStrings.ksFailedLoadingLL,
					m_LinguaLinksXmlFileName, m_cache.DatabaseName,
					System.Environment.NewLine, sLogFile),
					ITextStrings.ksLLImportFailed);
				return false;
			}

		}

		public struct AnnotationInfo
		{
			public int annotationId;
			public Guid annotationType;
			public int beginOffset;
			public int endOffset;
			public int paragraphId;
			public int wfId;
		}

		private void Convert5()
		{
			// What we have at this point is annotations for wordforms, punctuation forms, and
			// segments, but the offsets are useless. Even if LL could produce some offsets, they
			// would be based on 8-bit data which may have been changed when converted to Unicode.
			// Fortunately, the annotations can be ordered by id since they are created in the
			// order in which they occurred in LL (e.g., word/punctuation annotations followed by
			// the closing segment annotation. There may be mismatches in areas between LL and FW.
			// LL segments may have any punctuation mid-segment. LL can also have wordforms with
			// digits. FW can not have either of these (at this point).

			SqlConnection dbConnection = null;
			SqlCommand sqlCommand = null;
			SqlDataReader reader = null;
			string sConnection = string.Format("Server={0}; Database={1}; User ID=FWDeveloper; " +
				"Password=careful; Pooling=false;", m_cache.ServerName, m_cache.DatabaseName);

			dbConnection = new SqlConnection(sConnection);
			dbConnection.Open();
			try
			{

				// We need to process all of the word/punctuation annotations by
				// locating them in the corresponding baseline text and setting all of their begin/end
				// offsets.
				// This gives us the wordform and punctuation annotations in text order, along with the
				// paragraph id and the wordform text. The order by here only works directly after import
				// from LinguaLinks. With this information we need to set the offsets for each annotation.
				// Note 20 is a wordform, but is considered as punctuation by Flex. What will happen here?
				// Return values:
				//   Id of CmBaseAnnotation
				//   Guid of annotation type
				//   Id of paragraph holding wordform
				//   BeginOffset of wordform in paragraph (not really, so this isn't used)
				//   EndOffset of wordform in paragraph (actually length of word from LL. This is only
				//       used for punctuation forms.
				//   Text of wordform (null for puncutation and segment markers)
				// We are assuming that we can ignore the writing system when finding wordforms in text.
				List<AnnotationInfo> annotations = null;
				annotations = new List<AnnotationInfo>();
				string sqlCmd =
				"select cba.id, cad.guid$, cba.BeginObject, cba.BeginOffset, cba.EndOffset, wf.Id" +
				" from CmBaseAnnotation_ cba" +
				" join CmAnnotationDefn_ cad on cad.Id = cba.AnnotationType" +
				" left outer join CmObject co1 on co1.Id = cba.InstanceOf" +
				" left outer join CmObject co2 on co2.Id = co1.Owner$" +
				" left outer join CmObject co3 on co3.Id = co2.Owner$" +
				" left outer join WfiWordform wf on wf.Id in (co1.Id, co2.Id, co3.Id)" +
				" where cba.CompDetails like 'LLImport'" +
				" order by cba.id";
				sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = sqlCmd;
				reader = sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
				while (reader.Read())
				{
					AnnotationInfo annotation = new AnnotationInfo();
					annotation.annotationId = reader.GetInt32(0);
					annotation.annotationType = reader.GetGuid(1);
					if (reader.IsDBNull(2))
						annotation.paragraphId = 0;
					else
						annotation.paragraphId = reader.GetInt32(2);
					annotation.beginOffset = reader.GetInt32(3);
					annotation.endOffset = reader.GetInt32(4);
					if (reader.IsDBNull(5))
						annotation.wfId = 0;
					else
						annotation.wfId = reader.GetInt32(5);
					// For some reason we are getting a null paragraph for incomplete texts.
					if (annotation.paragraphId > 0)
						annotations.Add(annotation);
				}
				reader.Close();

				// Load the alternative forms for each WfiWordform.  This is needed to handle
				// texts from multiple writing systems.  See LT-7290.
				// We don't currently use the ws value, but load it anyway.
				Dictionary<int, Dictionary<int, string>> wordforms = new Dictionary<int, Dictionary<int, string>>();
				int obj;
				int ws;
				string txt;
				Dictionary<int, string> wfalts;
				sqlCmd = "SELECT Obj, Ws, Txt FROM WfiWordform_Form";
				sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = sqlCmd;
				reader = sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
				while (reader.Read())
				{
					obj = reader.GetInt32(0);
					ws = reader.GetInt32(1);
					if (reader.IsDBNull(2))
						txt = "";
					else
						txt = reader.GetString(2);
					if (wordforms.TryGetValue(obj, out wfalts))
					{
						wfalts.Add(ws, txt);
					}
					else
					{
						wfalts = new Dictionary<int, string>();
						wfalts.Add(ws, txt);
						wordforms.Add(obj, wfalts);
					}
				}
				reader.Close();

				// This retrieves the interlinear text paragraphs
				//   Id of paragraph
				//   Text of paragraph (ignoring formatting and ws)
				Dictionary<int, string> paragraphs = new Dictionary<int, string>();
				sqlCmd = "select stp.Id, stp.Contents from Text_Contents tc" +
					" join StText_Paragraphs stps on stps.src = tc.dst" +
					" join StTxtPara_ stp on stp.id = stps.dst";
				sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = sqlCmd;
				reader = sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
				int paragraphId;
				string paraContents = null;
				while (reader.Read())
				{
					paragraphId = reader.GetInt32(0);
					if (reader.IsDBNull(1))
						paraContents = "";
					else
						paraContents = reader.GetString(1).ToLower();
					paragraphs.Add(paragraphId, paraContents);
				}
				reader.Close();

				// Go through all of the WordformsInContext and set their offsets
				// based on the baseline string.
				int offsetInString = 0;
				int currentContentId = 0;
				string contents = null;
				int cUnfound = 0;
				for (int iann = 0; iann < annotations.Count; ++iann)
				{
					AnnotationInfo ann = annotations[iann];
					if (ann.annotationType.ToString() != LangProject.kguidAnnWordformInContext)
						continue;
					if (ann.paragraphId != currentContentId)
					{
						currentContentId = ann.paragraphId;
						paragraphs.TryGetValue(currentContentId, out contents);
						offsetInString = 0;
					}
					Debug.Assert(contents != null);
					if (ann.wfId == 0)
						continue;
					if (!wordforms.TryGetValue(ann.wfId, out wfalts))
						continue;
					Dictionary<int, string>.Enumerator wfit = wfalts.GetEnumerator();
					bool fFound = false;
					while (wfit.MoveNext())
					{
						string form = wfit.Current.Value;
						int offsetBegin = contents.IndexOf(form, offsetInString);
						if (offsetBegin != -1)
						{
							// REVIEW:  SHOULD WE DOUBLECHECK ws?
							ann.beginOffset = offsetBegin;
							ann.endOffset = ann.beginOffset + form.Length;
							fFound = true;
							break;
						}
					}
					if (!fFound)
					{
						// We can't find the wordform for some reason.
						ann.beginOffset = offsetInString;
						ann.endOffset = ann.beginOffset;
						++cUnfound;
					}
					offsetInString = ann.endOffset;
					annotations[iann] = ann;
				}

				// Now that the wordform offsets are set, we need to set the offsets for punctuation forms
				// and segments. We can use the begin offset of the first word/punct and the
				// last end offset to set the following segment begin/end offsets.
				int beginSegmentOffset = 0;
				int endSegmentOffset = 0;
				for (int iann = 0; iann < annotations.Count; ++iann)
				{
					AnnotationInfo ann = annotations[iann];
					if (ann.paragraphId != currentContentId)
					{
						currentContentId = ann.paragraphId;
						beginSegmentOffset = 0;
						endSegmentOffset = 0;
					}
					switch (ann.annotationType.ToString())
					{
						case LangProject.kguidAnnWordformInContext:
							endSegmentOffset = ann.endOffset;
							break;
						case LangProject.kguidAnnPunctuationInContext:
							ann.beginOffset = endSegmentOffset;
							// Note, endOffset in LL is actually the length of the punctuation
							// text. We'll assume here that the length didn't change when
							// converted to Unicode.
							endSegmentOffset = ann.beginOffset + ann.endOffset;
							ann.endOffset = endSegmentOffset;
							annotations[iann] = ann;
							break;
						case LangProject.kguidAnnTextSegment:
							ann.beginOffset = beginSegmentOffset;
							ann.endOffset = endSegmentOffset;
							annotations[iann] = ann;
							beginSegmentOffset = endSegmentOffset;
							break;
						default:
							break; // This shouldn't happen.
					}
				}

				// Now write all of the offsets to the database.
				sqlCommand = dbConnection.CreateCommand();
				foreach (AnnotationInfo ann in annotations)
				{
					sqlCmd = String.Format("update CmBaseAnnotation set BeginOffset = {0}, EndOffset = {1} where Id = {2}",
						ann.beginOffset.ToString(), ann.endOffset.ToString(), ann.annotationId.ToString());
					sqlCommand.CommandText = sqlCmd;
					sqlCommand.ExecuteNonQuery();
				}
			}
			finally
			{
				dbConnection.Close();
			}

			if (m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.CommitTrans();

			// Move the CmAgentEvaluations to the original CmAgent and delete the one from LL.
			// Also remove the LinguaLinks flags from CmAgent_Details and CmAnotation_CompDetails.
			m_cache.DatabaseAccessor.BeginTrans();
			DbOps.ExecuteStoredProc(
				m_cache,
				"declare @origOwner int, @LLOwner int, @sId nvarchar(20) " +
				"select top 1 @LLOwner = Owner$ from CmAgentEvaluation_ where Details = 'Imported from Lingualinks' " +
				"select top 1 @origOwner = id from CmAgent where Human = 1 and id != @LLOwner " +
				"update CmAgentEvaluation_ set Owner$ = @origOwner where Details = 'Imported from Lingualinks' " +
				"update CmAgentEvaluation set Details = NULL where Details = 'Imported from Lingualinks' " +
				"set @sId = convert(nvarchar(20), @LLOwner) " +
				"exec DeleteObjects @sId " +
				"UPDATE CmAnnotation SET CompDetails=NULL WHERE CompDetails LIKE 'LLImport'",
				null);
			m_cache.DatabaseAccessor.CommitTrans();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports an error.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="caption">The caption.</param>
		/// ------------------------------------------------------------------------------------
		private void ReportError(string message, string caption)
		{
			if (Error != null)
				Error(this, message, caption);
		}
	}
}
