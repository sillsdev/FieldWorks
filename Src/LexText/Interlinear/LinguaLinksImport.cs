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
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using ECInterfaces;
using SilEncConverters31;
using SIL.FieldWorks.FDO.Application.ApplicationServices;


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
		private IProgress m_progress;
		private string m_sErrorMsg;
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
		/// Initializes a new instance of the <see cref="LinguaLinksImport"/> class.
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
		/// Does the import.
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion flag, 2) array of
		/// LanguageMappings, 3) start phase.</param>
		/// <returns>Returns <c>true</c> if we did the complete import, false if we
		/// quit early.</returns>
		/// ------------------------------------------------------------------------------------
		public object Import(IProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			bool runToCompletion = (bool)parameters[0];
			m_languageMappings = (LanguageMapping[])parameters[1];
			int startPhase = (int)parameters[2];
			m_progress = dlg;
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
			m_progress.Step(150);
			if (m_progress.Canceled)
				return false;

			if (startPhase < 3)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem2;
				dlg.Message = ITextStrings.ksLLImportPhase2;
				if (!Convert2())
					return false;
			}
			m_progress.Step(75);
			if (m_progress.Canceled)
				return false;

			if (startPhase < 4)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem3;
				dlg.Message = ITextStrings.ksLLImportPhase3;
				if (!Convert3())
					return false;
			}
			m_progress.Step(25);
			if (startPhase < 5)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem3A;
				if (!Convert4())
					return false;
			}
			m_progress.Step(25);
			if (startPhase < 6)
			{
				m_sErrorMsg = ITextStrings.ksTransformProblem3B;
				if (!Convert5())
					return false;
				m_progress.Step(25);
			}
			else
			{
				m_progress.Step(75);
			}
			if (m_progress.Canceled)
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
				if (Convert6())
				{

					m_sErrorMsg = ITextStrings.ksFinishLLTextsProblem5;
					dlg.Message = ITextStrings.ksLLImportPhase5;
					m_shownProgress = m_phaseProgressStart = dlg.Position;
					m_phaseProgressEnd = 500;
					Convert7();
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
		public object ImportInterlinear(IProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			m_nextInput = (string)parameters[0];
			if (!DoTransform(m_sRootDir + "FWExport2FWDump.xsl", m_nextInput, m_sTempDir + "IIPhaseOneOutput.xml"))
				return false;
			m_nextInput = m_sTempDir + "IIPhaseOneOutput.xml";
			if (!Convert4())
				return false;
			if (!Convert5())
				return false;
			m_sErrorMsg = ITextStrings.ksInterlinImportErrorPhase1;
			dlg.Message = ITextStrings.ksInterlinImportPhase1of2;
			if (Convert6())
			{
				m_sErrorMsg = ITextStrings.ksInterlinImportErrorPhase2;
				dlg.Message = ITextStrings.ksInterlinImportPhase2of2;
				m_shownProgress = dlg.Position;
				m_phaseProgressEnd = 500;
				Convert7();		// probably not needed now, and useless, but historically speaking ....
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
			using (FileStream fsi = new FileStream(m_nextInput, FileMode.Open, FileAccess.Read),
				fso = new FileStream(m_sTempDir + "LLPhase1Output.xml", FileMode.Create, FileAccess.Write))
			{
				using (BinaryReader br = new BinaryReader(fsi))
				{
					using (BinaryWriter bw = new BinaryWriter(fso))
					{
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
								m_progress.Step(m_currentProgress - m_shownProgress);
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
				}
			}
		}

		private bool DoTransform(string xsl, string xml, string outputName)
		{
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
				using (var writer = new XmlTextWriter(outputName, Utf8))
				{
					writer.Formatting = Formatting.Indented;
					writer.Indentation = 2;

					//Transform the file.
					xslt.Transform(doc.CreateNavigator(), null, writer);
					writer.Close();
					return true;
				}
			}
			catch (Exception ex)
			{
				// For some reason during debugging of LLImportPhase1.xsl we hit an exception here
				// with an error saying "Common Language Runtime detected an invalid program".
				// It seems to be choking over the ThesaurusItems5016 template. Yet when the program
				//  is run outside the debugger, it doesn't hit this. The transform also works fine via MSXSL.???
				ReportError(ex.Message, ITextStrings.ksLLEncConv);
				//Console.WriteLine("{0} Exception caught.", ex);
				//Console.ReadLine();
				return false; // LT-7223 transform was failing with an invalid character and then going on!
			}

		}

		private bool Convert2()
		{
			bool res = DoTransform(m_sRootDir + "LLImportPhase1.xsl", m_nextInput, m_sTempDir + "LLPhase2Output.xml");
			m_nextInput = m_sTempDir + "LLPhase2Output.xml";
			return res;
		}

		private bool Convert3()
		{
			bool res = DoTransform(m_sRootDir + "LLImportPhase2.xsl", m_nextInput, m_sTempDir + "LLPhase3Output.xml");
			m_nextInput = m_sTempDir + "LLPhase3Output.xml";
			return res;
		}

		private bool Convert4()
		{
			bool res = DoTransform(m_sRootDir + "LLImportPhase3.xsl", m_nextInput, m_sTempDir + "LLPhase4Output.xml");
			m_nextInput = m_sTempDir + "LLPhase4Output.xml";
			return res;
		}

		private bool Convert5()
		{
			bool res = DoTransform(m_sRootDir + "LLImportPhase4.xsl", m_nextInput, m_sTempDir + "LLPhase5Output.xml");
			m_nextInput = m_sTempDir + "LLPhase5Output.xml";
			return res;
		}

		private bool Convert6()
		{
			try
			{
				XmlImportData xid = new XmlImportData(m_cache);
				return xid.ImportData(m_nextInput, m_progress);
			}
			catch
			{
				string sLogFile = Path.Combine(m_sTempDir, m_nextInput);
				ReportError(string.Format(ITextStrings.ksFailedLoadingLL,
					m_LinguaLinksXmlFileName, m_cache.ProjectId.Name,
					System.Environment.NewLine, sLogFile),
					ITextStrings.ksLLImportFailed);
				return false;
			}

		}

		public string LogFile
		{
			get { return Path.Combine(m_sTempDir, "LLPhase5Output-Import.log"); }
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
		public struct AnnotationInfo2
		{
			public ICmBaseAnnotation cba;
			public IWfiWordform wf;
		}

		private void Convert7()
		{
			try
			{
				m_cache.DomainDataByFlid.BeginNonUndoableTask();

				// Delete any WfiWordform that contain a digit.  FieldWorks doesn't allow
				// numbers in its wordforms, whereas LinguaLinks did.  Letting them through
				// causes interesting crashes...
				IWfiWordformRepository repoWf = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
				List<IWfiWordform> rgwfDel = new List<IWfiWordform>();
				foreach (IWfiWordform wf in repoWf.AllInstances())
				{
					string s = wf.Form.BestAnalysisVernacularAlternative.Text;
					if (!String.IsNullOrEmpty(s))
					{
						for (int i = 0; i < s.Length; ++i)
						{
							if (Icu.IsNumeric(s[i]))
							{
								rgwfDel.Add(wf);
								break;
							}
						}
					}
				}
				foreach (IWfiWordform wf in rgwfDel)
				{
					m_cache.DomainDataByFlid.DeleteObj(wf.Hvo);
				}

				// Delete the CmAgent from LL, changing all imported references to the corresponding
				// CmAgent that already existed.
				ICmAgentRepository repoAgent = m_cache.ServiceLocator.GetInstance<ICmAgentRepository>();
				ICmAgent goodAgent = null;
				ICmAgent badAgent = null;

				foreach (ICmAgent agent in repoAgent.AllInstances())
				{
					if (agent.Human && agent.Version == "LinguaLinksImport")
					{
						badAgent = agent;
						break;
					}
				}
				foreach (ICmAgent agent in repoAgent.AllInstances())
				{
					if (agent.Human && agent.Version != "LinguaLinksImport")
					{
						goodAgent = agent;
						break;
					}
				}
				if (badAgent != null && goodAgent != null)
				{
					ICmAgentEvaluation badPositive = badAgent.ApprovesOA;
					ICmAgentEvaluation badNegative = badAgent.DisapprovesOA;
					ICmAgentEvaluation goodPositive = goodAgent.ApprovesOA;
					ICmAgentEvaluation goodNegative = goodAgent.DisapprovesOA;
					Debug.Assert(badPositive != null);
					Debug.Assert(badNegative != null);
					Debug.Assert(goodPositive != null);
					Debug.Assert(goodNegative != null);

					IWfiAnalysisRepository repoWfiAnal = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
					foreach (IWfiAnalysis wfa in repoWfiAnal.AllInstances())
					{
						if (wfa.EvaluationsRC.Contains(badPositive))
						{
							wfa.EvaluationsRC.Remove(badPositive);
							wfa.EvaluationsRC.Add(goodPositive);
						}
						else if (wfa.EvaluationsRC.Contains(badNegative))
						{
							wfa.EvaluationsRC.Remove(badNegative);
							wfa.EvaluationsRC.Add(goodNegative);
						}
					}
					// delete the LinguaLinks agent.
					m_cache.LangProject.AnalyzingAgentsOC.Remove(badAgent);
				}
			}
			finally
			{
				m_cache.DomainDataByFlid.EndNonUndoableTask();
			}
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
