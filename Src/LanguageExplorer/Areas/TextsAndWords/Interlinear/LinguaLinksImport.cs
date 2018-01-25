// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using ECInterfaces;
using SilEncConverters40;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	public partial class LinguaLinksImport
	{
		private int m_phaseProgressStart, m_phaseProgressEnd, m_shownProgress;
		private IThreadedProgress m_progress;
		private LanguageMapping[] m_languageMappings;
		private LanguageMapping m_current;
		private int m_version; // of FLExText being imported. 0 if no version found.
		private EncConverters m_converters;
		private string m_sTempDir;
		private string m_sRootDir;
		private LcmCache m_cache;
		private string m_LinguaLinksXmlFileName;

		public delegate void ErrorHandler(object sender, string message, string caption);
		public event ErrorHandler Error;

		/// <summary>
		/// Initializes a new instance of the <see cref="LinguaLinksImport"/> class.
		/// </summary>
		public LinguaLinksImport(LcmCache cache, string tempDir, string rootDir)
		{
			m_cache = cache;
			m_sTempDir = tempDir;
			m_sRootDir = rootDir;
		}

		/// <summary>
		/// Gets the error message.
		/// </summary>
		public string ErrorMessage { get; private set; }

		/// <summary>
		/// Gets or sets the next input.
		/// </summary>
		public string NextInput { get; set; }

		/// <summary>
		/// Does the import.
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion flag, 2) array of
		/// LanguageMappings, 3) start phase.</param>
		/// <returns>Returns <c>true</c> if we did the complete import, false if we
		/// quit early.</returns>
		public object Import(IThreadedProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			var runToCompletion = (bool)parameters[0];
			m_languageMappings = (LanguageMapping[])parameters[1];
			var startPhase = (int)parameters[2];
			m_progress = dlg;
			m_LinguaLinksXmlFileName = NextInput;

			ErrorMessage = ITextStrings.ksTransformProblem;
			m_shownProgress = m_phaseProgressStart = 0;
			m_phaseProgressEnd = 150;
			if (startPhase < 2)
			{
				dlg.Title = ITextStrings.ksLLImportProgress;
				dlg.Message = ITextStrings.ksLLImportPhase1;
				ErrorMessage = ITextStrings.ksTransformProblem1;
				if (!Convert1())
				{
					return false;
				}
			}
			m_progress.Step(150);
			if (m_progress.Canceled)
			{
				return false;
			}

			if (startPhase < 3)
			{
				ErrorMessage = ITextStrings.ksTransformProblem2;
				dlg.Message = ITextStrings.ksLLImportPhase2;
				if (!Convert2())
				{
					return false;
				}
			}
			m_progress.Step(75);
			if (m_progress.Canceled)
			{
				return false;
			}

			if (startPhase < 4)
			{
				ErrorMessage = ITextStrings.ksTransformProblem3;
				dlg.Message = ITextStrings.ksLLImportPhase3;
				if (!Convert3())
					return false;
			}
			m_progress.Step(25);
			if (startPhase < 5)
			{
				ErrorMessage = ITextStrings.ksTransformProblem3A;
				if (!Convert4())
				{
					return false;
				}
			}
			m_progress.Step(25);
			if (startPhase < 6)
			{
				ErrorMessage = ITextStrings.ksTransformProblem3B;
				if (!Convert5())
				{
					return false;
				}
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
			var fi = new FileInfo(m_sTempDir + "LLPhase3Output.xml");
			if (fi.Length == 0)
			{
				ReportError(string.Format(ITextStrings.ksInvalidLLFile, m_LinguaLinksXmlFileName), ITextStrings.ksLLImport);
				throw new InvalidDataException();
			}

			// There's no way to cancel from here on out.
			dlg.AllowCancel = false;

			if (!runToCompletion)
			{
				return false;
			}
			ErrorMessage = ITextStrings.ksXMLParsingProblem4;
			dlg.Message = ITextStrings.ksLLImportPhase4;
			if (!Convert6())
			{
				return false;
			}
			ErrorMessage = ITextStrings.ksFinishLLTextsProblem5;
			dlg.Message = ITextStrings.ksLLImportPhase5;
			m_shownProgress = m_phaseProgressStart = dlg.Position;
			m_phaseProgressEnd = 500;
			Convert7();
			return true;
		}

		public void ImportWordsFrag(Func<Stream> createWordsFragDocStream, ImportAnalysesLevel analysesLevel)
		{
			using (var stream = createWordsFragDocStream.Invoke())
			{
				var serializer = new XmlSerializer(typeof(WordsFragDocument));
				var wordsFragDoc = (WordsFragDocument)serializer.Deserialize(stream);
				NormalizeWords(wordsFragDoc.Words);
				ImportWordsFrag(wordsFragDoc.Words, analysesLevel);
			}
		}

		internal void ImportWordsFrag(Word[] words, ImportAnalysesLevel analysesLevel)
		{
			s_importOptions = new ImportInterlinearOptions {AnalysesLevel = analysesLevel};
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				foreach (var word in words)
				{
					CreateWordAnalysisStack(m_cache, word);
				}
			});
		}

		/// <summary>
		/// The first text created by ImportInterlinear.
		/// </summary>
		public IText FirstNewText { get; set; }
		/// <summary>
		/// Import a file which looks like a FieldWorks interlinear XML export.
		/// </summary>
		public object ImportInterlinear(IThreadedProgress dlg, object[] parameters)
		{
			var retValue = false;
			Debug.Assert(parameters.Length == 1);
			using (var stream = new FileStream((string) parameters[0], FileMode.Open, FileAccess.Read))
			{
				IText firstNewText = null;
				retValue = ImportInterlinear(dlg, stream, 100, ref firstNewText);
				FirstNewText = firstNewText;
			}
			return retValue;
		}

		public bool ImportInterlinear(IThreadedProgress progress, Stream birdData, int allottedProgress, ref IText firstNewText)
		{
			return ImportInterlinear(new ImportInterlinearOptions { Progress = progress, BirdData = birdData, AllottedProgress = allottedProgress }, ref firstNewText);
		}

		/// <summary>
		/// Import a file which looks like a FieldWorks interlinear XML export. This file can contain many interlinear texts.
		/// If a text was previously imported then attempt to merge it. If a text has not been imported before then a new text
		/// is created and it is poplulated with the input if possible.
		/// </summary>
		public bool ImportInterlinear(ImportInterlinearOptions options, ref IText firstNewText)
		{
			var progress = options.Progress;
			var birdData = options.BirdData;
			var allottedProgress = options.AllottedProgress;
			var mergeSucceeded = false;
			var continueMerge = false;
			firstNewText = null;
			var initialProgress = progress.Position;
			try
			{
				m_cache.DomainDataByFlid.BeginNonUndoableTask();
				progress.Message = ITextStrings.ksInterlinImportPhase1of2;
				var serializer = new XmlSerializer(typeof(BIRDDocument));
				var doc = (BIRDDocument)serializer.Deserialize(birdData);
				Normalize(doc);
				var version = 0;
				if (!string.IsNullOrEmpty(doc.version))
				{
					int.TryParse(doc.version, out version);
				}
				progress.Position = initialProgress + allottedProgress / 2;
				progress.Message = ITextStrings.ksInterlinImportPhase2of2;
				if (doc.interlineartext != null)
				{
					var step = 0;
					foreach (var interlineartext in doc.interlineartext)
					{
						step++;
						IText newText;
						if (!string.IsNullOrEmpty(interlineartext.guid))
						{
							ICmObject repoObj;
							m_cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(interlineartext.guid), out repoObj);
							newText = repoObj as IText;
							if (newText != null && ShowPossibleMergeDialog(progress) == DialogResult.Yes)
							{
								continueMerge = MergeTextWithBIRDDoc(ref newText,
												new TextCreationParams
												{
													Cache = m_cache,
													InterlinText = interlineartext,
													Progress = progress,
													ImportOptions = options,
													Version = version
												});
							}
							else if (newText == null)
							{
								newText = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create(m_cache, new Guid(interlineartext.guid));
								continueMerge = PopulateTextIfPossible(options, ref newText, interlineartext, progress, version);
							}
							else //user said do not merge.
							{
								//ignore the Guid; we shouldn't create another text with the same guid
								newText = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
								continueMerge = PopulateTextIfPossible(options, ref newText, interlineartext, progress, version);
							}
						}
						else
						{
							newText = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
							continueMerge = PopulateTextIfPossible(options, ref newText, interlineartext, progress, version);
						}

						if (!continueMerge)
						{
							break;
						}
						progress.Position = initialProgress + allottedProgress/2 + allottedProgress*step/2/doc.interlineartext.Length;
						if (firstNewText == null)
						{
							firstNewText = newText;
						}

					}
					mergeSucceeded = continueMerge;

				}
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
				Debug.Print(e.StackTrace);
			}
			finally
			{
				m_cache.DomainDataByFlid.EndNonUndoableTask();
			}
			return mergeSucceeded;
		}

		/// <summary>
		/// Attempt to populate a new FieldWorks text with a BIRD format interlinear text. If this fails
		/// for some reason then the new text is deleted and also return false to tell the calling method to abort the import.
		/// </summary>
		/// <returns>true if operation completed, false if the import operation should be aborted</returns>
		private bool PopulateTextIfPossible(ImportInterlinearOptions options, ref IText newText, Interlineartext interlineartext, IThreadedProgress progress, int version)
		{
			if (!PopulateTextFromBIRDDoc(ref newText,
					new TextCreationParams
					{
						Cache = m_cache,
						InterlinText = interlineartext,
						Progress = progress,
						ImportOptions = options,
						Version = version
					})) //if the user aborted this text
			{
				newText.Delete(); //remove it from the list
				return false;
			}
			return true;
		}

		/// <summary>
		/// We want everything to be NFD, particularly so we match wordforms correctly.
		/// </summary>
		private static void Normalize(BIRDDocument doc)
		{
			foreach (var text in doc.interlineartext)
			{
				NormalizeItems(text.Items);
				if (text.paragraphs == null)
				{
					continue;
				}
				foreach (var para in text.paragraphs)
				{
					if (para.phrases == null)
					{
						continue;
					}
					foreach (var phrase in para.phrases)
					{
						NormalizeItems(phrase.Items);
						if (phrase.WordsContent == null || phrase.WordsContent.Words == null)
						{
							continue;
						}
						var words = phrase.WordsContent.Words;
						NormalizeWords(words);
					}
				}
			}
		}

		private static void NormalizeWords(IEnumerable<Word> words)
		{
			foreach (var word in words)
			{
				NormalizeItems(word.Items);
			}
		}

		private static void NormalizeItems(item[] items)
		{
			if (items == null)
			{
				return;
			}
			foreach (var item in items)
			{
				if (item.Value == null)
				{
					continue;
				}
				item.Value = item.Value.Normalize(NormalizationForm.FormD);
			}
		}

		/// <summary>
		/// This method exists for testing purposes only, we don't want to show this dialog when we are testing the merge behavior.
		/// </summary>
		protected virtual DialogResult ShowPossibleMergeDialog(IThreadedProgress progress)
		{
			//we need to invoke the dialog on the main thread so we can use the progress dialog as the parent.
			//otherwise the message box can be displayed behind everything
			var asyncResult = progress.SynchronizeInvoke.BeginInvoke(new ShowDialogAboveProgressbarDelegate(ShowDialogAboveProgressbar),
				new object[]
				{
					progress,
					ITextStrings.ksAskMergeInterlinearText,
					ITextStrings.ksAskMergeInterlinearTextTitle,
					MessageBoxButtons.YesNo
				});
			return (DialogResult)progress.SynchronizeInvoke.EndInvoke(asyncResult);
		}

		private static ITsString GetSpaceAdjustedPunctString(ILgWritingSystemFactory wsFactory, item item, ITsString wordString, char space, bool followsWord)
		{
			if (item.Value.Length == 0)
			{
				return wordString;
			}
			var index = 0;
			var tempValue = AdjustPunctStringForCharacter(wsFactory, item, wordString, item.Value[index], index, space, followsWord);
			if (item.Value.Length < 2)
			{
				return tempValue;
			}
			index = item.Value.Length - 1;
			tempValue = AdjustPunctStringForCharacter(wsFactory, item, tempValue, item.Value[index], index, space, followsWord);
			return tempValue;
		}

		private static ITsString AdjustPunctStringForCharacter(ILgWritingSystemFactory wsFactory, item item, ITsString wordString, char punctChar, int index, char space, bool followsWord)
		{
			var spaceBefore = false;
			var spaceAfter = false;
			var spaceHere = false;
			const char quote = '"';
			var charType = Icu.GetCharType(punctChar);
			switch (charType)
			{
				case Icu.UCharCategory.U_END_PUNCTUATION:
				case Icu.UCharCategory.U_FINAL_PUNCTUATION:
					spaceAfter = true;
					break;
				case Icu.UCharCategory.U_START_PUNCTUATION:
				case Icu.UCharCategory.U_INITIAL_PUNCTUATION:
					spaceBefore = true;
					break;
				case Icu.UCharCategory.U_OTHER_PUNCTUATION: //handle special characters
					if(wordString.Text.LastIndexOfAny(new[] {',','.',';',':','?','!',quote}) == wordString.Length - 1) //treat as ending characters
					{
						//quote characters are extra special, if we find them on their own
						//it is near impossible to know what to do, but it's usually nothing.
						spaceAfter = punctChar != '"' || wordString.Length > 1;
					}
					if (punctChar == '\xA1' || punctChar == '\xBF')
					{
						spaceHere = true;
					}
					if (punctChar == quote && wordString.Length == 1)
					{
						spaceBefore = followsWord; //if we find a lonely quotation mark after a word, we'll put a space before it.
					}
					break;
			}
			var wordBuilder = wordString.GetBldr();
			if(spaceBefore) //put a space to the left of the punct
			{
				ILgWritingSystem wsEngine;
				if (TryGetWsEngine(wsFactory, item.lang, out wsEngine))
				{
					wordBuilder.ReplaceTsString(0, 0, TsStringUtils.MakeString("" + space, wsEngine.Handle));
				}
				wordString = wordBuilder.GetString();
			}
			if (spaceHere && followsWord && !LastCharIsSpaceOrQuote(wordString, index, space, quote))
			{
				ILgWritingSystem wsEngine;
				if (TryGetWsEngine(wsFactory, item.lang, out wsEngine))
				{
					wordBuilder.ReplaceTsString(index, index, TsStringUtils.MakeString(string.Empty + space, wsEngine.Handle));
				}
				wordString = wordBuilder.GetString();
			}
			if (spaceAfter) //put a space to the right of the punct
			{
				ILgWritingSystem wsEngine;
				if (TryGetWsEngine(wsFactory, item.lang, out wsEngine))
				{
					wordBuilder.ReplaceTsString(wordBuilder.Length, wordBuilder.Length, TsStringUtils.MakeString("" + space, wsEngine.Handle));
				}
				wordString = wordBuilder.GetString();
			}
			//otherwise punct doesn't deserve a space.
			return wordString;
		}

		private static bool LastCharIsSpaceOrQuote(ITsString wordString, int index, char space, char quote)
		{
			index -= 1;
			if (index < 0)
			{
				return false;
			}
			var charString = wordString.GetChars(index, index + 1);
			return (charString[0] == space || charString[0] == quote);
		}

		private static bool TryGetWsEngine(ILgWritingSystemFactory wsFact, string langCode, out ILgWritingSystem wsEngine)
		{
			wsEngine = null;
			try
			{
				wsEngine = wsFact.get_Engine(langCode);
			}
			catch (ArgumentException e)
			{
				Debug.Assert(false, "We hit the non-existant ws in AdjustPunctStringForCharacter().");
				return false;
			}
			return true;
		}

		private void ProcessSearchBuffer(string searchBuffer, int size, bool bufferIt, ref string buffer, BinaryWriter bw)
		{
			for (var j = 0; j < size; j++)
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
			var found = false;
			foreach (var mapping in m_languageMappings)
			{
				if (found || mapping.LlCode != buffer)
				{
					continue;
				}
				found = true;
				foreach (var code in mapping.FwCode)
				{
					bw.Write((byte)code);
				}
				m_current = mapping;
			}
			//We shouldn't need the following code, but sometimes LL dumps unexpected language codes
			if (found)
			{
				return;
			}
			foreach (var character in buffer)
			{
				bw.Write((byte)character);
			}

			if (m_languageMappings.Length > 0)
			{
				m_current = m_languageMappings[0];
			}
		}

		private void ProcessLanguageCode2(string buffer)
		{
			var found = false;
			foreach (var mapping in m_languageMappings)
			{
				if (found || mapping.LlCode != buffer)
				{
					continue;
				}
				found = true;
				m_current = mapping;
			}
			//We shouldn't need the following code, but sometimes LL dumps unexpected language codes
			if (!found && m_languageMappings.Length > 0)
			{
				m_current = m_languageMappings[0];
			}
		}

		private void ProcessLanguageData(string buffer, BinaryWriter bw)
		{
			if (buffer.Length <= 0)
			{
				return;
			}
			IEncConverter converter = null;
			var result = string.Empty;

			if (!string.IsNullOrEmpty(m_current.EncodingConverter))
			{
				converter = m_converters[m_current.EncodingConverter];
			}

			if (converter != null)
			{
				// Replace any make sure the &lt; &gt; &amp; and &quot;
				var specialEntities = new[] { "&lt;", "&gt;", "&quot;", "&amp;"};
				var actualXML = new[] { "<", ">", "\"", "&"};
				var replaced = new[] { false, false, false, false };
				var anyReplaced = false;
				Debug.Assert(specialEntities.Length == actualXML.Length && actualXML.Length == replaced.Length, "Programming error...");

				var sb = new StringBuilder(buffer);	// use a string builder for performance
				for (var i = 0; i < specialEntities.Length; i++)
				{
					if (!buffer.Contains(specialEntities[i]))
					{
						continue;
					}
					replaced[i] = anyReplaced = true;
					sb = sb.Replace(specialEntities[i], actualXML[i]);
				}

				var len = sb.Length;	// buffer.Length;
				var subData = new byte[len];
				for (var j = 0; j < len; j++)
				{
					subData[j] = (byte)sb[j];	// buffer[j];
				}

				try
				{
					result = converter.ConvertToUnicode(subData);
				}
				catch (Exception e)
				{
					ReportError(string.Format(ITextStrings.ksEncConvFailed, converter.Name, e.Message), ITextStrings.ksLLEncConv);
				}

				// now put any of the four back to the Special Entity notation
				if (anyReplaced)	// only if we changed on input
				{
					sb = new StringBuilder(result);
					for (var i = specialEntities.Length-1; i >= 0; i--)
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
			foreach (var character in result)
			{
				if (128 > character)
				{
					//0XXX XXXX
					bw.Write((byte)character);
				}
				else if (2048 > character)
				{
					//110X XXXX 10XX XXXX
					bw.Write((byte)(192 + character / 64));
					bw.Write((byte)(128 + (character & 63)));
				}
				else
				{
					//1110 XXXX 10XX XXXX 10XX XXXX}
					bw.Write((byte)(224 + character / 4096));
					bw.Write((byte)(128 + (character & 4095) / 64));
					bw.Write((byte)(128 + (character & 63)));
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

		/// <summary>
		/// JohnT: I didn't write this, but a preliminary read and the comment in the progress dialog indicate that it is
		/// attempting to convert the language data elements of the input file (a) using the specified encoding converter
		/// for the appropriate writing system, if any, and then (b) to UTF8. Also, it converts the ws attribute values to
		/// the appropriate FW encoding identifiers the user has selected.
		///
		/// To do this it uses a complex state machine. It's basic purpose seems to be to isolate the data needing conversion
		/// in one buffer, then save any intermediate markup in another buffer, until it gets the correspondig ws attribute
		/// which allows it to determine the appropriate converter to use. Each state corresponds to some step in the process.
		///
		/// For each state there are up to eight keywords being looked for...usually only one or two except in the initial
		/// state, where there are seven (see InitializeSearches). Searches [0] is reserved for the most likely WRONG thing
		/// that would indicate that the thing we're looking for is missing.
		///
		/// Processing proceeds one character at a time. An array loc keeps track of how much of each search string has been
		/// matched by the preceeding and current characters.
		/// </summary>
		private bool Convert1()
		{
			const int maxSearch = 8;

			var searches = new string[maxSearch];
			var locs = new int[maxSearch];
			var okay = true;

			if (!File.Exists(NextInput))
			{
				ReportError(string.Format(ITextStrings.ksInputFileNotFound, NextInput), ITextStrings.ksLLEncConv);
				return false;
			}
			m_converters = new EncConverters();
			using (FileStream fsi = new FileStream(NextInput, FileMode.Open, FileAccess.Read), fso = new FileStream(m_sTempDir + "LLPhase1Output.xml", FileMode.Create, FileAccess.Write))
			using (var br = new BinaryReader(fsi))
			using (var bw = new BinaryWriter(fso))
			{
				var buffer = string.Empty;
				var searchBuffer = string.Empty;
				var rieBufferXML = string.Empty;
				var rieBufferLangData = string.Empty;

				foreach (var lm in m_languageMappings)
				{
					var mapping = lm;
					if (mapping.FwName != ITextStrings.ksIgnore && mapping.FwCode != string.Empty)
					{
						continue;
					}
					mapping.FwName = "zzzIgnore";
					mapping.FwCode = "zzzIgnore";
				}

				//Start
				var mode = Modes.kStart;
				InitializeSearches(searches);
				int j;
				for (j = 0; j < maxSearch; j++)
				{
					locs[j] = 0;
				}
				var bufferIt = false;

				while (okay && fsi.Position < fsi.Length)
				{
					var currentProgress = m_phaseProgressStart + (int)Math.Floor((double)((m_phaseProgressEnd - m_phaseProgressStart) * fsi.Position / fsi.Length));
					if (currentProgress > m_shownProgress && currentProgress <= m_phaseProgressEnd)
					{
						m_progress.Step(currentProgress - m_shownProgress);
						m_shownProgress = currentProgress;
					}
					var c = (char)br.ReadByte();
					var found = false;
					var tempDiff = 0;
					for (j = 0; j < maxSearch; j++)
					{
						if (c == searches[j][locs[j]])
						{
							found = true;
							locs[j]++;
							if (locs[j] > tempDiff)
							{
								tempDiff = locs[j];
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
						tempDiff = searchBuffer.Length - tempDiff;
						if (tempDiff > 0)
						{
							ProcessSearchBuffer(searchBuffer, tempDiff, bufferIt, ref buffer, bw);
							searchBuffer = searchBuffer.Substring(tempDiff, searchBuffer.Length - tempDiff);
						}
						if (locs[0] == searches[0].Length && (locs[0] == searchBuffer.Length))
						{
							var s = string.Format(ITextStrings.ksExpectedXButGotY, searches[1], searches[0]);
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
						else if (locs[1] == searches[1].Length && (locs[1] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kRun1;
							}
							else if (mode == Modes.kRun1)
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
								mode = Modes.kRun2;
							}
							else if (mode == Modes.kRun2)
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
								mode = Modes.kRun3;
							}
							else if (mode == Modes.kRun3)
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
								mode = Modes.kRun4;
							}
							else if (mode == Modes.kRun4)
							{
								// <Run ws="en">Data</Run>
								ProcessLanguageData(buffer, bw);
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kAUni1)
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
								mode = Modes.kAUni2;
							}
							else if (mode == Modes.kAUni2)
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
								mode = Modes.kAUni3;
							}
							else if (mode == Modes.kAUni3)
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
								mode = Modes.kAUni4;
							}
							else if (mode == Modes.kAUni4)
							{
								// <AUni ws="en">Data</AUni>
								ProcessLanguageData(buffer, bw);
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kAStr1)
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
								mode = Modes.kAStr2;
							}
							else if (mode == Modes.kAStr2)
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
								mode = Modes.kAStr3;
							}
							else if (mode == Modes.kAStr3)
							{
								// <AStr ws="en">
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kRIE1)
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
								mode = Modes.kRIE2;
							}
							else if (mode == Modes.kRIE2)
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
								mode = Modes.kRIE3;
							}
							else if (mode == Modes.kRIE3)
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
								mode = Modes.kRIE4;
							}
							else if (mode == Modes.kRIE4)
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
								mode = Modes.kRIE5;
							}
							else if (mode == Modes.kRIE5)
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
								mode = Modes.kRIE6;
							}
							else if (mode == Modes.kRIE6)
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
								mode = Modes.kRIE7;
							}
							else if (mode == Modes.kRIE7)
							{
								// <ReversalIndexEntry ... <Uni>Data</Uni> ... <WritingSystem5053> ... <Link ws="en" ... </WritingSystem5053>
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kLink1)
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
								mode = Modes.kLink2;
							}
							else if (mode == Modes.kLink2)
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
								mode = Modes.kLink3;
							}
							else if (mode == Modes.kLink3)
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
								mode = Modes.kLink4;
							}
							else if (mode == Modes.kLink4)
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
								mode = Modes.kLink3;
							}
							else if (mode == Modes.kLinkA2)
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
								mode = Modes.kLinkA3;
							}
							else if (mode == Modes.kLinkA3)
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
								mode = Modes.kLinkA4;
							}
							else if (mode == Modes.kLinkA4)
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
								mode = Modes.kLinkA3;
							}
							else if (mode == Modes.kICU1)
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
								mode = Modes.kICU2;
							}
							else if (mode == Modes.kICU2)
							{
								// <ICULocale24> <Uni>en</Uni>
								ProcessLanguageCode(buffer, bw);
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kDtd)
							{
								// <!DOCTYPE ... >
								ProcessSearchBuffer(searchBuffer, locs[1], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								bufferIt = false;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[2] == searches[2].Length) && (locs[2] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kAUni1;
							}
							else if ((mode == Modes.kLink1) || (mode == Modes.kLink3) || (mode == Modes.kLinkA3))
							{
								// <Link ... /> or <Link ... ws="en" ... />
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[2], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							else if (mode == Modes.kRIE1)
							{
								// <ReversalIndexEntry ... </ReversalIndexEntry>
								bufferIt = false;
								ProcessSearchBuffer(searchBuffer, locs[2], bufferIt, ref buffer, bw);
								searchBuffer = string.Empty;
								InitializeSearches(searches);
								mode = Modes.kStart;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[3] == searches[3].Length) && (locs[3] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kRIE1;
							}
							else if (mode == Modes.kLink1)
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
								mode = Modes.kLinkA2;
							}
							else if (mode == Modes.kLinkA3)
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
								mode = Modes.kLinkA2;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[4] == searches[4].Length) && (locs[4] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kAStr1;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[5] == searches[5].Length) && (locs[5] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kLink1;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[6] == searches[6].Length) && (locs[6] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kICU1;
							}
							for (j = 0; j < maxSearch; j++)
							{
								locs[j] = 0;
							}
						}
						else if ((locs[7] == searches[7].Length) && (locs[7] == searchBuffer.Length))
						{
							if (mode == Modes.kStart)
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
								mode = Modes.kDtd;
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
					NextInput = m_sTempDir + "LLPhase1Output.xml";
				}
				return okay;
			}
		}

		private bool DoTransform(string xsl, string xml, string outputName)
		{
			try
			{
				Encoding Utf8 = new UTF8Encoding(false);

				//Create the XslTransform and load the stylesheet.
				var xslt = new XslCompiledTransform();
				xslt.Load(xsl, XsltSettings.TrustedXslt, null);

				//Load the XML data file.
				var doc = new XPathDocument(xml);

				//Create an XmlTextWriter to output to the appropriate file. First make sure the expected
				//directory exists.
				if (!Directory.Exists(Path.GetDirectoryName(outputName)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(outputName));
				}
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
				return false; // LT-7223 transform was failing with an invalid character and then going on!
			}

		}

		private bool Convert2()
		{
			var res = DoTransform(m_sRootDir + "LLImportPhase1.xsl", NextInput, m_sTempDir + "LLPhase2Output.xml");
			NextInput = m_sTempDir + "LLPhase2Output.xml";
			return res;
		}

		private bool Convert3()
		{
			var res = DoTransform(m_sRootDir + "LLImportPhase2.xsl", NextInput, m_sTempDir + "LLPhase3Output.xml");
			NextInput = m_sTempDir + "LLPhase3Output.xml";
			return res;
		}

		private bool Convert4()
		{
			var res = DoTransform(m_sRootDir + "LLImportPhase3.xsl", NextInput, m_sTempDir + "LLPhase4Output.xml");
			NextInput = m_sTempDir + "LLPhase4Output.xml";
			return res;
		}

		private bool Convert5()
		{
			var res = DoTransform(m_sRootDir + "LLImportPhase4.xsl", NextInput, m_sTempDir + "LLPhase5Output.xml");
			NextInput = m_sTempDir + "LLPhase5Output.xml";
			return res;
		}

		private bool Convert6()
		{
			try
			{
				var xid = new XmlImportData(m_cache, true);
				xid.ImportData(NextInput, m_progress);
				return true;
			}
			catch
			{
				var sLogFile = Path.Combine(m_sTempDir, NextInput);
				ReportError(string.Format(ITextStrings.ksFailedLoadingLL,
					m_LinguaLinksXmlFileName, m_cache.ProjectId.Name,
					Environment.NewLine, sLogFile),
					ITextStrings.ksLLImportFailed);
				return false;
			}

		}

		public string LogFile => Path.Combine(m_sTempDir, "LLPhase5Output-Import.log");

		private void Convert7()
		{
			try
			{
				m_cache.DomainDataByFlid.BeginNonUndoableTask();

				// Delete any WfiWordform that contain a digit.  FieldWorks doesn't allow
				// numbers in its wordforms, whereas LinguaLinks did.  Letting them through
				// causes interesting crashes...
				var repoWf = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
				var rgwfDel = new List<IWfiWordform>();
				foreach (var wf in repoWf.AllInstances())
				{
					var s = wf.Form.BestAnalysisVernacularAlternative.Text;
					if (string.IsNullOrEmpty(s))
					{
						continue;
					}
					foreach (var character in s)
					{
						if (Icu.IsNumeric(character))
						{
							rgwfDel.Add(wf);
							break;
						}
					}
				}
				foreach (var wf in rgwfDel)
				{
					m_cache.DomainDataByFlid.DeleteObj(wf.Hvo);
				}

				// Delete the CmAgent from LL, changing all imported references to the corresponding
				// CmAgent that already existed.
				var agentRepository = m_cache.ServiceLocator.GetInstance<ICmAgentRepository>();
				var badAgent = agentRepository.AllInstances().FirstOrDefault(agent => agent.Human && agent.Version == "LinguaLinksImport");
				var goodAgent = agentRepository.AllInstances().FirstOrDefault(agent => agent.Human && agent.Version != "LinguaLinksImport");
				if (badAgent == null || goodAgent == null)
				{
					return;
				}
				var badPositive = badAgent.ApprovesOA;
				var badNegative = badAgent.DisapprovesOA;
				var goodPositive = goodAgent.ApprovesOA;
				var goodNegative = goodAgent.DisapprovesOA;
				Debug.Assert(badPositive != null);
				Debug.Assert(badNegative != null);
				Debug.Assert(goodPositive != null);
				Debug.Assert(goodNegative != null);

				var repoWfiAnal = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
				foreach (var wfa in repoWfiAnal.AllInstances())
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
			finally
			{
				m_cache.DomainDataByFlid.EndNonUndoableTask();
			}
		}

		/// <summary>
		/// Reports an error.
		/// </summary>
		private void ReportError(string message, string caption)
		{
			Error?.Invoke(this, message, caption);
		}

		private enum Modes { kStart, kRun1, kRun2, kRun3, kRun4, kAUni1, kAUni2, kAUni3, kAUni4, kAStr1, kAStr2, kAStr3, kRIE1, kRIE2, kRIE3, kRIE4, kRIE5, kRIE6, kRIE7, kLink1, kLink2, kLink3, kLink4, kLink5, kLinkA2, kLinkA3, kLinkA4, kICU1, kICU2, kDtd }
	}
}