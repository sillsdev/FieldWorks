// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.IText.FlexInterlinModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Extensions;

namespace SIL.FieldWorks.IText
{
	public partial class LinguaLinksImport
	{
		//this delegate is used for alerting the user of new writing systems found in the import
		//or a text that is already found.
		private delegate DialogResult ShowDialogAboveProgressbarDelegate(IThreadedProgress progress,
			string text, string title, MessageBoxButtons buttons);

		private static ImportInterlinearOptions s_importOptions;

		private static Dictionary<string, ILgWritingSystem> s_wsMapper = new Dictionary<string, ILgWritingSystem>();

		/// <summary>
		/// This method will display a message box above the progress dialog.
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="text"></param>
		/// <param name="title"></param>
		/// <param name="buttons"></param>
		/// <returns></returns>
		private static DialogResult ShowDialogAboveProgressbar(IThreadedProgress progress,
			string text, string title, MessageBoxButtons buttons)
		{
			return MessageBox.Show(
				text,
				title,
				buttons,
				MessageBoxIcon.Warning);
		}

		internal class TextCreationParams
		{
			internal Interlineartext InterlinText;
			internal LcmCache Cache;
			internal IThreadedProgress Progress;
			internal ImportInterlinearOptions ImportOptions;
			internal int Version;
		}

		/// <summary>
		/// This method will create a new Text document from the given BIRD format Interlineartext. If this fails
		/// for some reason then return false to tell the calling method to abort the import.
		/// </summary>
		/// <param name="newText">The text to populate, could be set to null.</param>
		/// <param name="textParams">This contains the interlinear text.</param>
		/// <returns>The imported text may be in a writing system that is not part of this project. Return false if the user
		/// rejects the text which tells the caller of this method to abort the import.</returns>
		private static bool PopulateTextFromBIRDDoc(ref LCModel.IText newText, TextCreationParams textParams)
		{
			s_importOptions = textParams.ImportOptions;
			Interlineartext interlinText = textParams.InterlinText;
			LcmCache cache = textParams.Cache;
			IThreadedProgress progress = textParams.Progress;
			if (s_importOptions.CheckAndAddLanguages == null)
				s_importOptions.CheckAndAddLanguages = CheckAndAddLanguagesInternal;

			ILgWritingSystemFactory wsFactory = cache.WritingSystemFactory;
			const char space = ' ';
			//handle the languages(writing systems) section alerting the user if new writing systems are encountered
			if (!s_importOptions.CheckAndAddLanguages(cache, interlinText, wsFactory, progress))
				return false;

			//handle the header(info or meta) information
			SetTextMetaAndMergeMedia(cache, interlinText, wsFactory, newText, false);

			//create all the paragraphs
			foreach (var paragraph in interlinText.paragraphs)
			{
				if (newText.ContentsOA == null)
				{
					newText.ContentsOA = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				}
				IStTxtPara newTextPara = newText.ContentsOA.AddNewTextPara("");
				int offset = 0;
				if (paragraph.phrases == null)
				{
					continue;
				}
				foreach (var phrase in paragraph.phrases)
				{
					ICmObject oldSegment = null;
					//Try and locate a segment with this Guid.
					if (!String.IsNullOrEmpty(phrase.guid))
					{
						if (cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(phrase.guid), out oldSegment))
						{
							//We aren't merging, but we have this guid in our system; ignore the file Guid
							oldSegment = cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset);
						}
						else
						{
							//The segment is identified by a Guid, but apparently we don't have it in our current document, so make one with the guid
							oldSegment = cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset, cache,
																									new Guid(phrase.guid));
						}
					}
					//set newSegment to the old, or create a brand new one.
					ISegment newSegment = oldSegment as ISegment ?? cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset);
					//Fill in the ELAN time information if it is present.
					AddELANInfoToSegment(cache, phrase, newSegment);
					ITsString phraseText = null;
					bool textInFile = false;
					//Add all of the data from <item> elements into the segment.
					AddSegmentItemData(cache, wsFactory, phrase, newSegment, ref textInFile, ref phraseText);
					bool lastWasWord = false;
					if (phrase.WordsContent != null && phrase.WordsContent.Words != null)
					{
						if (textParams.Version == 0 && PhraseHasExactlyOneTxtItemNotAKnownWordform(newSegment.Cache, phrase))
						{
							// It might be a SayMore text that makes the whole segment a single txt item.
							// We want to add the text anyway (unless a higher level did so), but we will skip making
							// a wordform. Eventual parsing of the text will do so.
							if (!textInFile)
							{
								UpdatePhraseTextForWordItems(wsFactory, ref phraseText, phrase.WordsContent.Words[0], ref lastWasWord, space);
							}
						}
						else
						{
							foreach (var word in phrase.WordsContent.Words)
							{
								//If the text of the phrase was not given in the document build it from the words.
								if (!textInFile)
								{
									UpdatePhraseTextForWordItems(wsFactory, ref phraseText, word, ref lastWasWord, space);
								}
								AddWordToSegment(newSegment, word);
							}
						}
					}
					UpdateParagraphTextForPhrase(newTextPara, ref offset, phraseText);
				}
			}
			return true;
		}

		/// <summary>
		/// Return true if the phrase has exactly one word which has exactly one item of type txt,
		/// and that item is not a known wordform.
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="phrase"></param>
		/// <returns></returns>
		private static bool PhraseHasExactlyOneTxtItemNotAKnownWordform(LcmCache fdoCache, Phrase phrase)
		{
			if (phrase.WordsContent.Words.Length != 1 || phrase.WordsContent.Words[0].Items.Length != 1
				|| phrase.WordsContent.Words[0].Items[0].type != "txt")
				return false;
			var wsFact = fdoCache.WritingSystemFactory;
			var wordItem = phrase.WordsContent.Words[0].Items[0];
			int ws = GetWsEngine(wsFact, wordItem.lang).Handle;
			if (string.IsNullOrEmpty(wordItem.Value))
				return true; // if it has no text, it can't be a known wordform...
			var wf =
				fdoCache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetMatchingWordform(ws, wordItem.Value);
			return wf == null;
		}

		/// <summary>
		/// Merge the contents of the given Text into the exising one. If this fails
		/// for some reason then return false to tell the calling method to abort the import.
		/// </summary>
		/// <param name="newText"></param>
		/// <param name="textParams"></param>
		/// <returns>The imported text may be in a writing system that is not part of this project. Return false if the user
		/// rejects the text  which tells the caller of this method to abort the import.</returns>
		private static bool MergeTextWithBIRDDoc(ref LCModel.IText newText, TextCreationParams textParams)
		{
			s_importOptions = textParams.ImportOptions;
			Interlineartext interlinText = textParams.InterlinText;
			LcmCache cache = textParams.Cache;
			IThreadedProgress progress = textParams.Progress;
			if (s_importOptions.CheckAndAddLanguages == null)
				s_importOptions.CheckAndAddLanguages = CheckAndAddLanguagesInternal;

			ILgWritingSystemFactory wsFactory = cache.WritingSystemFactory;
			char space = ' ';
			//handle the languages(writing systems) section alerting the user if new writing systems are encountered
			if (!s_importOptions.CheckAndAddLanguages(cache, interlinText, wsFactory, progress))
				return false;

			//handle the header(info or meta) information as well as any media-files sections
			SetTextMetaAndMergeMedia(cache, interlinText, wsFactory, newText, true);

			IStText newContents = newText.ContentsOA;
			//create or reuse the paragraphs available. NOTE: Currently the paragraph guids are being ignored, this might be wrong.
			foreach (var paragraph in interlinText.paragraphs)
			{
				IStTxtPara newTextPara = null;
				if (newContents == null)
				{
					newContents = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
					newText.ContentsOA = newContents;
					newTextPara = newContents.AddNewTextPara("");
				}
				int offset = 0;
				if (paragraph.phrases == null)
				{
					continue;
				}
				foreach (var phrase in paragraph.phrases)
				{
					ICmObject oldSegment = null;
					//Try and locate a segment with this Guid. Assign newTextPara to the paragraph we're working on if we haven't already
					if(!String.IsNullOrEmpty(phrase.guid))
					{
						if (cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(phrase.guid), out oldSegment))
						{
							if (oldSegment as ISegment != null) //The segment matches, add it into our paragraph.
							{
								IStTxtPara segmentOwner = newContents.ParagraphsOS.FirstOrDefault(para =>
									para.Guid.Equals((oldSegment as ISegment).Owner.Guid)) as IStTxtPara;
								if (segmentOwner != null && newTextPara == null) //We found the StTxtPara that correspond to this paragraph
									newTextPara = segmentOwner;
							}
							else if (oldSegment == null) //The segment is identified by a Guid, but apparently we don't have it in our current document, so make one
							{
								if (newTextPara == null)
									newTextPara = newContents.AddNewTextPara("");
								oldSegment = cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset, cache, new Guid(phrase.guid));
							}
							else //The Guid is in use, but not by a segment. This is bad.
							{
								return false;
							}
						}
					}
					//If newTextPara is null, try to use a paragraph that a sibling phrase belongs to.
					//Note: newTextPara is only assigned once, and won't be reassigned until we iterate through a new paragraph.
					if (newTextPara == null)
					{
						var phraseGuids = paragraph.phrases.Select(p => p.guid);
						foreach (IStTxtPara para in newContents.ParagraphsOS)
						{
							if (para.SegmentsOS.Any(seg => phraseGuids.Contains(seg.Guid.ToString())))
							{
								newTextPara = para;
								break;
							}
						}
					}
					//Can't find any paragraph for our phrase, create a brand new paragraph
					if (newTextPara == null)
						newTextPara = newContents.AddNewTextPara("");

					//set newSegment to the old, or create a brand new one.
					ISegment newSegment = oldSegment as ISegment;
					if (newSegment == null)
					{
						if (!string.IsNullOrEmpty(phrase.guid))
						{
							//The segment is identified by a Guid, but apparently we don't have it in our current document, so make one with the guid
							newSegment = cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset, cache, new Guid(phrase.guid));
						}
						else
							newSegment = cache.ServiceLocator.GetInstance<ISegmentFactory>().Create(newTextPara, offset);
					}
					//Fill in the ELAN time information if it is present.
					AddELANInfoToSegment(cache, phrase, newSegment);

					ITsString phraseText = null;
					bool textInFile = false;
					//Add all of the data from <item> elements into the segment.
					AddSegmentItemData(cache, wsFactory, phrase, newSegment, ref textInFile, ref phraseText);

					bool lastWasWord = false;
					if (phrase.WordsContent != null && phrase.WordsContent.Words != null)
					{
						//Rewrite our analyses
						newSegment.AnalysesRS.Clear();
						foreach (var word in phrase.WordsContent.Words)
						{
							//If the text of the phrase was not found in a "txt" item for this segment then build it from the words.
							if (!textInFile)
							{
								UpdatePhraseTextForWordItems(wsFactory, ref phraseText, word, ref lastWasWord, space);
							}
							MergeWordToSegment(newSegment, word);
						}
					}
					UpdateParagraphTextForPhrase(newTextPara, ref offset, phraseText);
				}
			}
			return true;
		}

		/// <summary>
		/// This method will update the newTextPara param appending the phraseText and possibly modifying the segment ending
		/// to add an end of segment character. The offset parameter will be set to the value where a following segment would start
		/// from. The paragraph text will be replaced if the offset is 0.
		/// </summary>
		/// <param name="newTextPara"></param>
		/// <param name="offset"></param>
		/// <param name="phraseText"></param>
		private static void UpdateParagraphTextForPhrase(IStTxtPara newTextPara, ref int offset, ITsString phraseText)
		{
			if (phraseText != null && phraseText.Length > 0)
			{
				var bldr = newTextPara.Contents.GetBldr();
				if (offset == 0)
				{
					bldr.Replace(0, bldr.Length, "", null);
				}
				offset += phraseText.Length;
				var oldText = (bldr.Text ?? "").Trim();
				if (oldText.Length > 0 && !TsStringUtils.IsEndOfSentenceChar(oldText[oldText.Length - 1],
					Icu.Character.UCharCategory.OTHER_PUNCTUATION))
				{
					// 'segment' does not end with recognizable EOS character. Add our special one.
					bldr.Replace(bldr.Length, bldr.Length, "\x00A7", null);
				}
				// Insert a space between phrases unless there is already one
				if (bldr.Length > 0 && phraseText.Text[0] != ' ' && bldr.Text[bldr.Length - 1] != ' ')
					bldr.Replace(bldr.Length, bldr.Length, " ", null);
				bldr.ReplaceTsString(bldr.Length, bldr.Length, phraseText);
				newTextPara.Contents = bldr.GetString();
			}
		}

		private static ILgWritingSystem GetWsEngine(ILgWritingSystemFactory wsFactory, string langCode)
		{
			ILgWritingSystem result;
			if (s_wsMapper.TryGetValue(langCode, out result))
				return result;

			return wsFactory.get_Engine(langCode);
		}

		/// <summary>
		/// This method will update the phraseText ref item with the contents of the item entries under the word
		/// </summary>
		private static void UpdatePhraseTextForWordItems(ILgWritingSystemFactory wsFactory, ref ITsString phraseText, Word word, ref bool lastWasWord, char space)
		{
			bool isWord = false;
			foreach (var item in word.Items)
			{
				switch (item.type)
				{
					case "txt": //intentional fallthrough
						isWord = true;
						goto case "punct";
					case "punct":
						ITsString wordString = TsStringUtils.MakeString(item.Value,
							GetWsEngine(wsFactory, item.lang).Handle);
						if (phraseText == null)
						{
							phraseText = wordString;
						}
						else
						{
							var phraseBldr = phraseText.GetBldr();
							if (lastWasWord && isWord) //two words next to each other deserve a space between
							{
								phraseBldr.ReplaceTsString(phraseText.Length, phraseText.Length,
								   TsStringUtils.MakeString("" + space, GetWsEngine(wsFactory, item.lang).Handle));
							}
							else if (!isWord) //handle punctuation
							{
								wordString = GetSpaceAdjustedPunctString(wsFactory,
									item, wordString, space, lastWasWord);
							}
							phraseBldr.ReplaceTsString(phraseBldr.Length, phraseBldr.Length, wordString);
							phraseText = phraseBldr.GetString();
						}
						lastWasWord = isWord;
						return; // only handle the baseline "txt" or "punct" once per "word" bundle, especially don't want extra writing system content in the baseline.
				}
			}
		}

		/// <summary>
		/// Add all the data from items in the FLExText file into their proper spots in the segment.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wsFactory"></param>
		/// <param name="phrase"></param>
		/// <param name="newSegment"></param>
		/// <param name="textInFile">This reference boolean indicates if there was a text item in the phrase</param>
		/// <param name="phraseText">This reference string will be filled with the contents of the "txt" item in the phrase if it is there</param>
		private static void AddSegmentItemData(LcmCache cache, ILgWritingSystemFactory wsFactory, Phrase phrase, ISegment newSegment, ref bool textInFile, ref ITsString phraseText)
		{
			if (phrase.Items != null)
			{
				foreach (var item in phrase.Items)
				{
					switch (item.type)
					{
						case "reference-label":
							newSegment.Reference = TsStringUtils.MakeString(item.Value,
								GetWsEngine(wsFactory, item.lang).Handle);
							break;
						case "gls":
							newSegment.FreeTranslation.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
						case "lit":
							newSegment.LiteralTranslation.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
						case "note":
							int ws = GetWsEngine(wsFactory, item.lang).Handle;
							INote newNote = newSegment.NotesOS.FirstOrDefault(note => note.Content.get_String(ws).Text == item.Value);
							if (newNote == null)
							{
								newNote = cache.ServiceLocator.GetInstance<INoteFactory>().Create();
								newSegment.NotesOS.Add(newNote);
								newNote.Content.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							}
							break;
						case "txt":
							phraseText = TsStringUtils.MakeString(item.Value, GetWsEngine(wsFactory, item.lang).Handle);
							textInFile = true;
							break;
						case "segnum":
							break; // The segnum item is not associated to a property, and also not a custom field. Skip merging it.
						default:
							var classId = cache.MetaDataCacheAccessor.GetClassId("Segment");
							var mdc = (IFwMetaDataCacheManaged) cache.MetaDataCacheAccessor;
							foreach (int flid in mdc.GetFields(classId, false, (int) CellarPropertyTypeFilter.All))
							{
								if (!mdc.IsCustom(flid))
									continue;
								var customId = mdc.GetFieldId2(classId, item.type, true);
								if (customId != 0)
								{
									var customWs = GetWsEngine(wsFactory, item.lang).Handle;
									var customTierText = TsStringUtils.MakeString(item.Value, customWs);
									cache.MainCacheAccessor.SetString(newSegment.Hvo, customId, customTierText);
								}
							}
							break;
					}
				}
			}
		}

		private static void AddELANInfoToSegment(LcmCache cache, Phrase phrase, ISegment newSegment)
		{
			if (!String.IsNullOrEmpty(phrase.mediaFile))
			{
				if (!String.IsNullOrEmpty(phrase.speaker))
				{
					newSegment.SpeakerRA = FindOrCreateSpeaker(phrase.speaker, cache);
				}
				newSegment.BeginTimeOffset = phrase.beginTimeOffset;
				newSegment.EndTimeOffset = phrase.endTimeOffset;
				newSegment.MediaURIRA = cache.ServiceLocator.ObjectRepository.GetObject(new Guid(phrase.mediaFile)) as ICmMediaURI;
			}
		}

		private static ICmPerson FindOrCreateSpeaker(string speaker, LcmCache cache)
		{
			if(cache.LanguageProject.PeopleOA != null)
			{
				//find and return a person in this project whose name matches the speaker
				foreach (var person in cache.LanguageProject.PeopleOA.PossibilitiesOS)
				{
					if (person.Name.BestVernacularAnalysisAlternative.Text.Equals(speaker))
					{
						return (ICmPerson)person;
					}
				}
			}
			else
			{
				cache.LanguageProject.PeopleOA = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}

			//person not found create one and add it.
			var newPerson = cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
			newPerson.Name.set_String(cache.DefaultVernWs, speaker);
			return newPerson;
		}

		private static void MergeWordToSegment(ISegment newSegment, Word word)
		{
			if(!String.IsNullOrEmpty(word.guid))
			{
				ICmObject repoObj;
				newSegment.Cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(word.guid), out repoObj);
				IAnalysis modelWord = repoObj as IAnalysis;
				if(modelWord != null)
				{
					UpgradeToWordGloss(word, ref modelWord);
					newSegment.AnalysesRS.Add(modelWord);
				}
				else
				{
					AddWordToSegment(newSegment, word);
				}
			}
			else
			{
				AddWordToSegment(newSegment, word);
			}
		}

		private static bool SomeLanguageSpecifiesVernacular(Interlineartext interlinText)
		{
			// return true if any language in the languages section is vernacular
			return interlinText.languages.language.Any(lang => lang.vernacularSpecified);
		}

		/// <summary>
		/// The imported text may be in a writing system that is not part of this project. Return false if the user
		/// rejects the text which tells the caller of this method to abort the import.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="interlinText"></param>
		/// <param name="wsFactory"></param>
		/// <param name="progress"></param>
		/// <returns>return false to abort import</returns>
		private static bool CheckAndAddLanguagesInternal(LcmCache cache, Interlineartext interlinText, ILgWritingSystemFactory wsFactory, IThreadedProgress progress)
		{
			DialogResult result;
			if (interlinText.languages != null && interlinText.languages.language != null)
			{
				if (!SomeLanguageSpecifiesVernacular(interlinText))
				{
					// Saymore file? something else that doesn't know to do this? We will confuse the user if we try to treat all as analysis.
					SetVernacularLanguagesByUsage(interlinText);
				}
				foreach (var lang in interlinText.languages.language)
				{
					bool fIsVernacular;
					var writingSystem = SafelyGetWritingSystem(cache, wsFactory, lang, out fIsVernacular);
					if (fIsVernacular)
					{
						if (!cache.LanguageProject.CurrentVernacularWritingSystems.Contains(writingSystem.Handle))
						{
							//we need to invoke the dialog on the main thread so we can use the progress dialog as the parent.
							//otherwise the message box can be displayed behind everything
							var instructions = GetInstructions(interlinText, writingSystem.LanguageName, ITextStrings.ksImportVernacLangMissing);
							IAsyncResult asyncResult = progress.SynchronizeInvoke.BeginInvoke(new ShowDialogAboveProgressbarDelegate(ShowDialogAboveProgressbar),
																		 new object[]
																			{
																				progress,
																				instructions,
																				ITextStrings.ksImportVernacLangMissingTitle,
																				MessageBoxButtons.OKCancel
																			});
							result = (DialogResult)progress.SynchronizeInvoke.EndInvoke(asyncResult);
							if (result == DialogResult.OK)
							{
								cache.LanguageProject.AddToCurrentVernacularWritingSystems((CoreWritingSystemDefinition) writingSystem);
							}
							else if (result == DialogResult.Cancel)
							{
								return false;
							}
						}
					}
					else
					{
						if (!cache.LanguageProject.CurrentAnalysisWritingSystems.Contains(writingSystem.Handle))
						{
							var instructions = GetInstructions(interlinText, writingSystem.LanguageName,
															   ITextStrings.ksImportAnalysisLangMissing);
							IAsyncResult asyncResult = progress.SynchronizeInvoke.BeginInvoke(new ShowDialogAboveProgressbarDelegate(ShowDialogAboveProgressbar),
																		 new object[]
																			{
																				progress,
																				instructions,
																				ITextStrings.ksImportAnalysisLangMissingTitle,
																				MessageBoxButtons.OKCancel
																			});
							result = (DialogResult)progress.SynchronizeInvoke.EndInvoke(asyncResult);
							//alert the user
							if (result == DialogResult.OK)
							{
								//alert the user
								cache.LanguageProject.AddToCurrentAnalysisWritingSystems((CoreWritingSystemDefinition) writingSystem);
								// We already have progress indications up.
								XmlTranslatedLists.ImportTranslatedListsForWs(writingSystem.Id, cache, FwDirectoryFinder.TemplateDirectory, null);
							}
							else if (result == DialogResult.Cancel)
							{
								return false;
							}
						}
					}
				}
			}
			return true;
		}

		private static string GetInstructions(Interlineartext interlinText, String wsName, String instructions)
		{
			var strBldr = new StringBuilder(wsName);
			strBldr.Append(instructions);
			strBldr.Append(Environment.NewLine); strBldr.Append(Environment.NewLine);
			strBldr.Append(GetPartOfPhrase(interlinText));
			return strBldr.ToString();
		}

		private static string GetPartOfPhrase(Interlineartext interlinText)
		{
			int i = 0;
			var strBldr = new StringBuilder(ITextStrings.ksImportLangMissingTextStartsWith);
			foreach (var paragraph in interlinText.paragraphs)
			{
				foreach (var phrase in paragraph.phrases)
				{
					foreach (var word in phrase.WordsContent.Words)
					{
						strBldr.Append(word.Items[0].Value);
						strBldr.Append(" ");
						i++;
						if (i > 6)
						{
							strBldr.Append(" ...");
							return strBldr.ToString();
						}
					}
				}
			}
			return strBldr.ToString();
		}

		private static void SetVernacularLanguagesByUsage(Interlineartext interlinText)
		{
			foreach (var para in interlinText.paragraphs)
			{
				if(para.phrases == null) // if there are no phrases, they have no languages we are interested in.
				{
					continue;
				}
				foreach (var phrase in para.phrases)
				{
					foreach (var item in phrase.Items)
					{
						if (item.type == "txt")
							EnsureVernacularLanguage(interlinText, item.lang);
					}
					if(phrase.WordsContent.Words != null)
					{
						foreach (var word in phrase.WordsContent.Words)
						{
							foreach (var item in word.Items)
							{
								if (item.type == "txt")
									EnsureVernacularLanguage(interlinText, item.lang);
							}
							// We could dig into the morphemes, but any client generating morphemes probably
							// does things right, and anyway we don't import that yet.
						}
					}
				}
			}
		}

		private static void EnsureVernacularLanguage(Interlineartext interlinText,string langName)
		{
			foreach (var lang in interlinText.languages.language)
			{
				if (lang.lang == langName)
				{
					lang.vernacularSpecified = true;
					lang.vernacular = true;
					return;
				}
			}
		}

		private static ILgWritingSystem SafelyGetWritingSystem(LcmCache cache, ILgWritingSystemFactory wsFactory,
			FlexInterlinModel.Language lang, out bool fIsVernacular)
		{
			fIsVernacular = lang.vernacularSpecified && lang.vernacular;
			ILgWritingSystem writingSystem = null;
			try
			{
				writingSystem = wsFactory.get_Engine(lang.lang);
			}
			catch (ArgumentException e)
			{
				CoreWritingSystemDefinition ws;
				WritingSystemServices.FindOrCreateSomeWritingSystem(cache, FwDirectoryFinder.TemplateDirectory, lang.lang,
					!fIsVernacular, fIsVernacular, out ws);
				writingSystem = ws;
				s_wsMapper.Add(lang.lang, writingSystem); // old id string -> new langWs mapping
			}
			return writingSystem;
		}

		private static void AddWordToSegment(ISegment newSegment, Word word)
		{
			//use the items under the word to determine what kind of thing to add to the segment
			var cache = newSegment.Cache;
			IAnalysis analysis = CreateWordAnalysisStack(cache, word);

			// Add to segment
			if (analysis != null)
			{
				newSegment.AnalysesRS.Add(analysis);
			}
		}

		private static IAnalysis CreateWordAnalysisStack(LcmCache cache, Word word)
		{
			if (word.Items == null || word.Items.Length <= 0) return null;
			IAnalysis analysis = null;
			var wsFact = cache.WritingSystemFactory;
			ILgWritingSystem wsMainVernWs = null;
			IWfiMorphBundle bundle = null;

			foreach (var wordItem in word.Items)
			{
				ITsString wordForm = null;
				switch (wordItem.type)
				{
					case "txt":
						wsMainVernWs = GetWsEngine(wsFact, wordItem.lang);
						wordForm = TsStringUtils.MakeString(wordItem.Value, wsMainVernWs.Handle);
						analysis = WfiWordformServices.FindOrCreateWordform(cache, wordForm);
						break;
					case "punct":
						wordForm = TsStringUtils.MakeString(wordItem.Value,
														 GetWsEngine(wsFact, wordItem.lang).Handle);
						analysis = WfiWordformServices.FindOrCreatePunctuationform(cache, wordForm);
						break;
				}
				if (wordForm != null)
					break;
			}

			// now add any alternative word forms. (overwrite any existing)
			if (analysis != null && analysis.HasWordform)
			{
				AddAlternativeWssToWordform(analysis, word, wsMainVernWs);
			}

			if (analysis != null)
			{
				UpgradeToWordGloss(word, ref analysis);
			}
			else
			{
				Debug.Assert(analysis != null, "What else could this do?");
			}

			// Fill in morphemes, lex. entries, lex. gloss, and lex.gram.info
			if (word.morphemes != null && word.morphemes.morphs.Length > 0)
			{
				ILexEntryRepository lex_entry_repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
				IMoMorphSynAnalysisRepository msa_repo = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
				int morphIdx = 0;
				foreach (var morpheme in word.morphemes.morphs)
				{
					var itemDict = new Dictionary<string, Tuple<string, string>>();
					if (analysis.Analysis == null)
					{
						break;
					}

					foreach (item item in morpheme.items)
					{
						itemDict[item.type] = new Tuple<string, string>(item.lang, item.Value);
					}

					if (itemDict.ContainsKey("txt")) // Morphemes
					{
						int ws = GetWsEngine(wsFact, itemDict["txt"].Item1).Handle;
						var morphForm = itemDict["txt"].Item2;
						ITsString wf = TsStringUtils.MakeString(morphForm, ws);

						// If we already have a bundle use that one
						bundle = analysis.Analysis.MorphBundlesOS.ElementAtOrDefault(morphIdx);
						if (bundle == null || bundle.Form.get_String(ws).Text != morphForm)
						{
							// Otherwise create a new bundle and add it to analysis
							bundle = cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
							if (analysis.Analysis.MorphBundlesOS.Count >= word.morphemes.morphs.Length)
							{
								analysis.Analysis.MorphBundlesOS.RemoveAt(morphIdx);
							}
							analysis.Analysis.MorphBundlesOS.Insert(morphIdx, bundle);
						}
						bundle.Form.set_String(ws, wf);
					}

					if (itemDict.ContainsKey("cf")) // Lex. Entries
					{
						int ws_cf = GetWsEngine(wsFact, itemDict["cf"].Item1).Handle;
						ILexEntry entry = null;
						var entries = lex_entry_repo.AllInstances().Where(
							m => StringServices.CitationFormWithAffixTypeStaticForWs(m, ws_cf, string.Empty) == itemDict["cf"].Item2);
						if (entries.Count() == 1)
						{
							entry = entries.First();
						}
						else if (itemDict.ContainsKey("hn")) // Homograph Number
						{
							entry = entries.FirstOrDefault(m => m.HomographNumber.ToString() == itemDict["hn"].Item2);
						}
						if (entry != null)
						{
							bundle.MorphRA = entry.LexemeFormOA;

							if (itemDict.ContainsKey("gls")) // Lex. Gloss
							{
								int ws_gls = GetWsEngine(wsFact, itemDict["gls"].Item1).Handle;
								ILexSense sense = entry.SensesOS.FirstOrDefault(s => s.Gloss.get_String(ws_gls).Text == itemDict["gls"].Item2);
								if (sense != null)
								{
									bundle.SenseRA = sense;
								}
							}
						}
					}

					if (itemDict.ContainsKey("msa")) // Lex. Gram. Info
					{
						IMoMorphSynAnalysis match = msa_repo.AllInstances().FirstOrDefault(m => m.InterlinearAbbr == itemDict["msa"].Item2);
						if (match != null)
						{
							bundle.MsaRA = match;
						}
					}
					morphIdx++;
				}
			}
			return analysis;
		}

		/// <summary>
		/// add any alternative forms (in alternative writing systems) to the wordform.
		/// Overwrite any existing alternative form in a given alternative writing system.
		/// </summary>
		private static void AddAlternativeWssToWordform(IAnalysis analysis, Word word, ILgWritingSystem wsMainVernWs)
		{
			ILgWritingSystemFactory wsFact = analysis.Cache.WritingSystemFactory;
			var wf = analysis.Wordform;
			foreach (var wordItem in word.Items)
			{
				switch (wordItem.type)
				{
					case "txt":
						var wsAlt = GetWsEngine(wsFact, wordItem.lang);
						if (wsAlt.Handle == wsMainVernWs.Handle)
							continue;
						ITsString wffAlt = TsStringUtils.MakeString(wordItem.Value, wsAlt.Handle);
						if (wffAlt.Length > 0)
							wf.Form.set_String(wsAlt.Handle, wffAlt);
						break;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="word"></param>
		/// <param name="analysis">the new analysis Gloss. If multiple glosses, returns the last one created.</param>
		private static void UpgradeToWordGloss(Word word, ref IAnalysis analysis)
		{
			LcmCache cache = analysis.Cache;
			var wsFact = cache.WritingSystemFactory;
			if (s_importOptions.AnalysesLevel == ImportAnalysesLevel.WordGloss)
			{
				// test for adding multiple glosses in the same language. If so, create separate analyses with separate glosses.
				bool fHasMultipleGlossesInSameLanguage = false;
				var dictMapLangToGloss = new Dictionary<string, string>();
				foreach (var wordGlossItem in word.Items.Select(i => i).Where(i => i.type == "gls"))
				{
					string gloss;
					if (!dictMapLangToGloss.TryGetValue(wordGlossItem.lang, out gloss))
					{
						dictMapLangToGloss.Add(wordGlossItem.lang, wordGlossItem.Value);
						continue;
					}
					if (wordGlossItem.Value == gloss) continue;
					fHasMultipleGlossesInSameLanguage = true;
					break;
				}

				AnalysisTree analysisTree = null;
				foreach (var wordGlossItem in word.Items.Select(i => i).Where(i => i.type == "gls"))
				{
					if (wordGlossItem == null) continue;
					if (wordGlossItem.analysisStatusSpecified &&
						wordGlossItem.analysisStatus != analysisStatusTypes.humanApproved) continue;
					// first make sure that an existing gloss does not already exist. (i.e. don't add duplicate glosses)
					int wsNewGloss = GetWsEngine(wsFact, wordGlossItem.lang).Handle;
					ITsString newGlossTss = TsStringUtils.MakeString(wordGlossItem.Value,
																	wsNewGloss);
					var wfiWord = analysis.Wordform;
					bool hasGlosses = wfiWord.AnalysesOC.Any(wfia => wfia.MeaningsOC.Any());
					IWfiGloss matchingGloss = null;
					if (hasGlosses)
					{
						foreach (var wfa in wfiWord.AnalysesOC)
						{
							matchingGloss = wfa.MeaningsOC.FirstOrDefault(wfg => wfg.Form.get_String(wsNewGloss).Equals(newGlossTss));
							if (matchingGloss != null)
								break;
						}
					}

					if (matchingGloss != null)
						analysis = matchingGloss;
					else
					{
						// TODO: merge with analysis having same morpheme breakdown (or at least the same stem)

						if (analysisTree == null || dictMapLangToGloss.Count == 1 || fHasMultipleGlossesInSameLanguage)
						{
							// create a new WfiAnalysis to store a new gloss
							analysisTree = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(wfiWord);
						}
						// else, reuse the same analysisTree for setting a gloss alternative

						analysisTree.Gloss.Form.set_String(wsNewGloss, wordGlossItem.Value);
						// Make sure this analysis is marked as user-approved (green check mark)
						cache.LangProject.DefaultUserAgent.SetEvaluation(analysisTree.WfiAnalysis, Opinions.approves);
						// Create a morpheme form that matches the wordform.
						var morphemeBundle = cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
						var wordItem = word.Items.Select(i => i).First(i => i.type == "txt");
						int wsWord = GetWsEngine(wsFact, wordItem.lang).Handle;
						analysisTree.WfiAnalysis.MorphBundlesOS.Add(morphemeBundle);
						morphemeBundle.Form.set_String(wsWord, wordItem.Value);

						analysis = analysisTree.Gloss;
					}
				}
			}
		}

		/// <summary>
		/// Set text metadata, create or merge media file URI's.
		/// <note>media files (ELAN initiated) need to be processed before the paragraphs, as segments could reference these parts.</note>
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="interlinText">The source text</param>
		/// <param name="wsFactory"></param>
		/// <param name="newText">The target text</param>
		/// <param name="merging">True if we are merging into an existing text; False if we are creating everything new</param>
		private static void SetTextMetaAndMergeMedia(LcmCache cache, Interlineartext interlinText, ILgWritingSystemFactory wsFactory,
			LCModel.IText newText, bool merging)
		{
			if (interlinText.Items != null) // apparently it is null if there are no items.
			{
				foreach (var item in interlinText.Items)
				{
					switch (item.type)
					{
						case "title":
							newText.Name.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
						case "title-abbreviation":
							newText.Abbreviation.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
						case "source":
							newText.Source.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
						case "comment":
							newText.Description.set_String(GetWsEngine(wsFactory, item.lang).Handle, item.Value);
							break;
					}
				}
			}

			if (interlinText.mediafiles != null)
			{
				if (newText.MediaFilesOA == null)
					newText.MediaFilesOA = cache.ServiceLocator.GetInstance<ICmMediaContainerFactory>().Create();
				newText.MediaFilesOA.OffsetType = interlinText.mediafiles.offsetType;

				foreach (var mediaFile in interlinText.mediafiles.media)
				{
					ICmObject extantObject;
					cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(mediaFile.guid), out extantObject);
					var media = extantObject as ICmMediaURI;
					if (media == null)
					{
						media = cache.ServiceLocator.GetInstance<ICmMediaURIFactory>().Create(cache, new Guid(mediaFile.guid));
						newText.MediaFilesOA.MediaURIsOC.Add(media);
					}
					else if (!merging)
					{
						// If a media URI with the same GUID exists, and we are not merging, create a new media URI with a new GUID
						media = cache.ServiceLocator.GetInstance<ICmMediaURIFactory>().Create();
						newText.MediaFilesOA.MediaURIsOC.Add(media);

						// Update references to this Media URI
						foreach (var phrase in interlinText.paragraphs.SelectMany(para => para.phrases))
						{
							if (mediaFile.guid.Equals(phrase.mediaFile))
								phrase.mediaFile = media.Guid.ToString();
						}
					}
					// else, the media URI already exists and we are merging; simply update the location
					media.MediaURI = mediaFile.location;
				}
			}
		}
	}
}
