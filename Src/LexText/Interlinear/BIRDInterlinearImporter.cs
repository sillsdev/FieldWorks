// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.IText.FlexInterlinModel;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

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
			IDictionary<string, INote> groupNote = new Dictionary<string, INote>();
			if (s_importOptions.CheckAndAddLanguages == null)
				s_importOptions.CheckAndAddLanguages = CheckAndAddLanguagesInternal;

			ILgWritingSystemFactory wsFactory = cache.WritingSystemFactory;
			const char space = ' ';
			//handle the languages(writing systems) section alerting the user if new writing systems are encountered
			if (!s_importOptions.CheckAndAddLanguages(cache, interlinText, wsFactory, progress))
				return false;

			//handle the header(info or meta) information
			SetTextMetaAndMergeMedia(cache, interlinText, wsFactory, newText, false);

			if (newText.ContentsOA == null)
			{
				// Create ContentsOA even if there are no paragraphs.
				newText.ContentsOA = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			}
			//create all the paragraphs
			foreach (var paragraph in interlinText.paragraphs)
			{
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
					AddSegmentItemData(cache, wsFactory, phrase, newSegment, groupNote, ref textInFile, ref phraseText);
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
									UpdatePhraseTextForWordItems(wsFactory, ref phraseText, word,
										ref lastWasWord, space);
								var writingSystemForText =
								TsStringUtils.IsNullOrEmpty(phraseText) ? newText.ContentsOA.MainWritingSystem : phraseText.get_WritingSystem(0);
								AddWordToSegment(newSegment, word, writingSystemForText);
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
			IDictionary<string, INote> groupNote = new Dictionary<string, INote>();
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
					AddSegmentItemData(cache, wsFactory, phrase, newSegment, groupNote, ref textInFile, ref phraseText);

					bool lastWasWord = false;
					if (phrase.WordsContent != null && phrase.WordsContent.Words != null)
					{
						//Rewrite our analyses
						newSegment.AnalysesRS.Clear();
						foreach (var word in phrase.WordsContent.Words)
						{
							//If the text of the phrase was not found in a "txt" item for this segment then build it from the words.
							if (!textInFile)
								UpdatePhraseTextForWordItems(wsFactory, ref phraseText, word,
									ref lastWasWord, space);
							AddWordToSegment(newSegment, word, newContents.MainWritingSystem);
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
						ITsString wordString = GetItemValue(item, wsFactory);
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

		private static bool IsGuess(Morphemes item)
		{
			if (item != null && item.analysisStatusSpecified &&
				(item.analysisStatus != analysisStatusTypes.humanApproved))
				return true;
			return false;
    }

		private static ITsString GetItemValue(item item, ILgWritingSystemFactory wsFactory)
		{
			if (item.run != null)
			{
				ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
				foreach (var run in item.run)
				{
					int runWs = GetWsEngine(wsFactory, run.lang).Handle;
					strBldr.Append(run.Value ?? " ", StyleUtils.CharStyleTextProps(run.style, runWs));
				}
				return strBldr.GetString();
			}
			int itemWs = GetWsEngine(wsFactory, item.lang).Handle;
			return TsStringUtils.MakeString(item.Value, itemWs);
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
		private static void AddSegmentItemData(LcmCache cache, ILgWritingSystemFactory wsFactory, Phrase phrase, ISegment newSegment, IDictionary<string, INote> groupNote, ref bool textInFile, ref ITsString phraseText)
		{
			if (phrase.Items != null)
			{
				foreach (var item in phrase.Items)
				{
					switch (item.type)
					{
						case "reference-label":
							newSegment.Reference = GetItemValue(item, wsFactory);
							break;
						case "gls":
							newSegment.FreeTranslation.set_String(GetWsEngine(wsFactory, item.lang).Handle, GetItemValue(item, wsFactory));
							break;
						case "lit":
							newSegment.LiteralTranslation.set_String(GetWsEngine(wsFactory, item.lang).Handle, GetItemValue(item, wsFactory));
							break;
						case "note":
							int ws = GetWsEngine(wsFactory, item.lang).Handle;
							INote newNote = newSegment.NotesOS.FirstOrDefault(note => note.Content.get_String(ws).Text == item.Value);
							if (newNote == null)
							{
								// In the file note items with the same group id are different translations of the same note. Read them into the same INote.
								string groupid = item.groupid;
								if (!String.IsNullOrEmpty(groupid) && groupNote.ContainsKey(groupid))
								{
									newNote = groupNote[groupid];
								}
								else
								{
									newNote = cache.ServiceLocator.GetInstance<INoteFactory>().Create();
									newSegment.NotesOS.Add(newNote);
									// Save note for groupid.
									if (!String.IsNullOrEmpty(groupid))
									{
										groupNote[groupid] = newNote;
									}
								}
								newNote.Content.set_String(GetWsEngine(wsFactory, item.lang).Handle, GetItemValue(item, wsFactory));
							}
							break;
						case "txt":
							phraseText = GetItemValue(item, wsFactory);
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
									var customTierText = GetItemValue(item, wsFactory);
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
					{
					if (person.Name.BestVernacularAnalysisAlternative.Text.Normalize().Equals(speaker.Normalize()))
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
			catch (ArgumentException)
			{
				CoreWritingSystemDefinition ws;
				WritingSystemServices.FindOrCreateSomeWritingSystem(cache, FwDirectoryFinder.TemplateDirectory, lang.lang,
					!fIsVernacular, fIsVernacular, out ws);
				writingSystem = ws;
				s_wsMapper.Add(lang.lang, writingSystem); // old id string -> new langWs mapping
			}
			return writingSystem;
		}

		private static void AddWordToSegment(ISegment newSegment, Word word,
			int mainWritingSystem)
		{
			//use the items under the word to determine what kind of thing to add to the segment
			var cache = newSegment.Cache;
			var analysis = CreateWordformWithWfiAnalysis(cache, word, mainWritingSystem);
			// Add to segment
			if (analysis != null)
			{
				newSegment.AnalysesRS.Add(analysis);
			}
		}

		private static IAnalysis CreateWordformWithWfiAnalysis(LcmCache cache, Word word, int mainWritingSystem)
		{
			if (FindOrCreateWfiAnalysis(cache, word, mainWritingSystem, out var matchingWf)
				|| matchingWf is IPunctuationForm)
			{
				return matchingWf;
			}
			IAnalysis wordForm = matchingWf;
			var wsFact = cache.WritingSystemFactory;
			IWfiMorphBundle bundle = null;

			if (wordForm != null)
				UpgradeToWordGloss(word, ref wordForm);
			else
				// There was an invalid analysis in the file. We can't do anything with it.
				return null;

			// Fill in morphemes, lex. entries, lex. gloss, and lex.gram.info
			if (word.morphemes != null && word.morphemes.morphs.Length > 0 &&
				word.morphemes.analysisStatus == analysisStatusTypes.humanApproved)
			{
				var lex_entry_repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
				var msa_repo = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
				foreach (var morpheme in word.morphemes.morphs)
				{
					var itemDict = new Dictionary<string, Tuple<string, ITsString>>();
					if (wordForm.Analysis == null)
						break;

					foreach (var item in morpheme.items)
						itemDict[item.type] = new Tuple<string, ITsString>(item.lang, GetItemValue(item, wsFact));

					if (itemDict.ContainsKey("txt")) // Morphemes
					{
						var ws = GetWsEngine(wsFact, itemDict["txt"].Item1).Handle;
						var morphForm = itemDict["txt"].Item2;
						var wf = morphForm;

						// Otherwise create a new bundle and add it to analysis
						bundle = cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>()
							.Create();
						wordForm.Analysis.MorphBundlesOS.Add(bundle);
						bundle.Form.set_String(ws, wf);
					}

					if (itemDict.ContainsKey("cf")) // Lex. Entries
					{
						// NB: "cf" records the lexeme, not the headword/citation form (in spite of the name).
						int ws_cf = GetWsEngine(wsFact, itemDict["cf"].Item1).Handle;
						ILexEntry entry = null;
						var entries = lex_entry_repo.AllInstances().Where(
							m => DecorateFormWithAffixMarkers(m.LexemeFormOA?.MorphTypeRA, m.LexemeFormOA?.Form?.get_String(ws_cf)?.Text) == itemDict["cf"].Item2.Text);

						// Filter entries by homograph number.
						// If the lexeme and the headword are different,
						// then there may be more than one entry with the given homograph number.
						// This is because homograph numbers distinguish headwords rather than lexemes.
						// If there is no "hn" entry, then the hn is 0.
						string hn = "0";
						if (itemDict.ContainsKey("hn")) // Homograph Number
						{
							hn = itemDict["hn"].Item2.Text;
						}
						var hnEntries = entries.Where(m => m.HomographNumber.ToString() == hn);
						if (hnEntries.Count() > 0)
						{
							entries = hnEntries;
						}

						if (itemDict.ContainsKey("gls")) // Lex. Gloss
						{
							// Filter senses by gloss.
							int ws_gls = GetWsEngine(wsFact, itemDict["gls"].Item1).Handle;
							IList<ILexSense> senses = new List<ILexSense>();
							foreach (var e in entries)
							{
								senses.AddRange(e.SensesOS.Where(s => s.Gloss.get_String(ws_gls).Text == itemDict["gls"].Item2.Text));
							}
							if (senses.Count() > 1 && itemDict.ContainsKey("msa"))
							{
								// Filter senses by MSA.
								IList<ILexSense> msaSenses = senses.Where(s => s.MorphoSyntaxAnalysisRA?.InterlinearAbbr == itemDict["msa"].Item2.Text).ToList();
								if (msaSenses.Count() > 0)
								{
									senses = msaSenses;
								}
							}
							// Record sense.
							if (senses.Count() > 0)
							{
								bundle.SenseRA = senses.FirstOrDefault();
								entry = bundle.SenseRA.Entry;
							}
						}

						if (entry == null && entries.Count() > 0)
						{
							entry = entries.First();
						}

						// Record morpheme.
						if (entry != null)
						{
							if (itemDict.ContainsKey("txt"))
							{
								// Try allomorph first.
								var ws_txt = GetWsEngine(wsFact, itemDict["txt"].Item1).Handle;
								bundle.MorphRA = entry.AllAllomorphs.Where(
									m => DecorateFormWithAffixMarkers(m.MorphTypeRA, m.Form.get_String(ws_txt).Text) == itemDict["txt"].Item2.Text).FirstOrDefault();
							}
							if (bundle.MorphRA == null)
							{
								bundle.MorphRA = entry.LexemeFormOA;
							}
						}
					}

					if (itemDict.ContainsKey("msa")) // Lex. Gram. Info
					{
						if (bundle.SenseRA != null && bundle.SenseRA.MorphoSyntaxAnalysisRA?.InterlinearAbbr == itemDict["msa"].Item2.Text)
						{
							bundle.MsaRA = bundle.SenseRA.MorphoSyntaxAnalysisRA;
						}
						else
						{
							IMoMorphSynAnalysis match = msa_repo.AllInstances().FirstOrDefault(m => m.InterlinearAbbr == itemDict["msa"].Item2.Text);
							if (match != null)
							{
								bundle.MsaRA = match;
							}
						}
					}
				}
			}

			// Try to fill in category.
			if (word.Items != null && wordForm.Analysis != null)
			{
				// Look for an existing category that matches a "pos".
				bool hasPOS = false;
				foreach (var item in word.Items)
				{
					if (wordForm.Analysis.CategoryRA != null)
					{
						// Category filled in.
						break;
					}
					if (item.type == "pos")
					{
						hasPOS = true;
						ILgWritingSystem writingSystem = GetWsEngine(cache.WritingSystemFactory, item.lang);
						if (writingSystem != null)
						{
							foreach (var cat in cache.LanguageProject.AllPartsOfSpeech)
							{
								if (MatchesCatNameOrAbbreviation(writingSystem.Handle, item.Value, cat))
								{
									wordForm.Analysis.CategoryRA = cat;
									break;
								}
							}
						}
					}
				}
				if (hasPOS && wordForm.Analysis.CategoryRA == null)
				{
					// Create a new category.
					IPartOfSpeech cat = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
					cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(cat);
					foreach (var item in word.Items)
					{
						if (item.type == "pos")
						{
							ILgWritingSystem writingSystem = GetWsEngine(cache.WritingSystemFactory, item.lang);
							if (writingSystem != null)
							{
								cat.Name.set_String(writingSystem.Handle, GetItemValue(item, cache.WritingSystemFactory));
								cat.Abbreviation.set_String(writingSystem.Handle, GetItemValue(item, cache.WritingSystemFactory));
							}
						}
					}
					wordForm.Analysis.CategoryRA = cat;
				}
			}

			return wordForm;
		}

		// Based on StringServices.DecorateFormWithAffixMarkers.
		private static string DecorateFormWithAffixMarkers(IMoMorphType mmt, string form)
		{
			if (mmt == null || form == null)
				return form;
			// Add pre- post markers, if any.
			if (!String.IsNullOrEmpty(mmt.Prefix))
			{
				form = mmt.Prefix + form;
			}
			if (!String.IsNullOrEmpty(mmt.Postfix))
			{
				form = form + mmt.Postfix;
			}
			return form;
		}

		private static bool FindOrCreateWfiAnalysis(LcmCache cache, Word word,
			int mainWritingSystem,
			out IAnalysis analysis)
		{
			var wsFact = cache.WritingSystemFactory;

			// First, collect all expected forms and glosses from the Word
			var expectedForms = new Dictionary<int, string>(); // wsHandle -> expected value
			var expectedGlosses = new Dictionary<int, string>(); // wsHandle -> expected gloss
			var expectedCats = new Dictionary<int, string>(); // wsHandle -> expected cat
			IAnalysis candidateForm = null;
			ITsString wordForm = null;
			ITsString punctForm = null;

			if (!String.IsNullOrEmpty(word.guid))
			{
				// Base candidateForm on guid rather than "txt" when available.
				// This works better for upper case versions of lower case words.
				ICmObject repoObj;
				cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(word.guid), out repoObj);
				var modelWord = repoObj as IAnalysis;
				if (modelWord != null)
				{
					candidateForm = modelWord.Wordform;
				}
			}

			foreach (var wordItem in word.Items)
			{
				if (wordItem.Value == null)
					continue;

				var ws = GetWsEngine(wsFact, wordItem.lang);

				switch (wordItem.type)
				{
					case "txt":
						wordForm = TsStringUtils.MakeString(wordItem.Value, ws.Handle);
						expectedForms[ws.Handle] = wordItem.Value;

						// Try to find a candidate wordform if we haven't found one yet
						if (candidateForm == null)
						{
							candidateForm = cache.ServiceLocator
								.GetInstance<IWfiWordformRepository>()
								.GetMatchingWordform(ws.Handle, wordItem.Value);
						}

						break;

					case "punct":
						punctForm = TsStringUtils.MakeString(wordItem.Value, ws.Handle);
						expectedForms[ws.Handle] = wordItem.Value;

						if (candidateForm == null)
						{
							IPunctuationForm pf;
							if (cache.ServiceLocator.GetInstance<IPunctuationFormRepository>()
								.TryGetObject(punctForm, out pf))
							{
								candidateForm = pf;
							}
						}

						break;

					case "gls":
						// Only consider human-approved glosses
						if (wordItem.analysisStatusSpecified &&
							wordItem.analysisStatus != analysisStatusTypes.humanApproved)
							continue;

						expectedGlosses[ws.Handle] = wordItem.Value;
						break;

					case "pos":
						expectedCats[ws.Handle] = wordItem.Value;
						break;
				}
			}
			if (word.morphemes != null && word.morphemes.analysisStatus != analysisStatusTypes.humanApproved)
			{
				// If the morphemes were guessed then the glosses and cats were also guessed.
				expectedGlosses.Clear();
				expectedCats.Clear();
			}

			if (candidateForm == null || !MatchPrimaryFormAndAddMissingAlternatives(candidateForm, expectedForms, mainWritingSystem))
			{
				analysis = CreateMissingForm(cache, wordForm, punctForm, expectedForms);
				return false;
			}

			var candidateWordform = candidateForm as IWfiWordform;
			if (candidateWordform == null)
			{
				// candidate is a punctuation form, nothing else to match
				analysis = candidateForm;
				return true;
			}
			analysis = candidateWordform;
			// If no glosses or morphemes are expected the wordform itself is the match
			if (expectedGlosses.Count == 0
				&& (word.morphemes == null || word.morphemes.morphs.Length == 0 ||
					word.morphemes.analysisStatus != analysisStatusTypes.humanApproved))
			{
				analysis = GetMostSpecificAnalysisForWordForm(candidateWordform);
				return true;
			}

			analysis = FindMatchingAnalysis(cache, candidateWordform, word, expectedGlosses, expectedCats);
			if (analysis != null)
			{
				return true;
			}

			if (wordForm.Text.ToLower() != wordForm.Text)
			{
				// Try lowercase.
				var lcCandidateForm = cache.ServiceLocator
								.GetInstance<IWfiWordformRepository>()
								.GetMatchingWordform(wordForm.get_WritingSystemAt(0), wordForm.Text.ToLower());
				if (lcCandidateForm is IWfiWordform lcCandidateWordform)
				{
					analysis = FindMatchingAnalysis(cache, lcCandidateWordform, word, expectedGlosses, expectedCats);
					if (analysis != null)
					{
						return true;
					}
				}
			}

			// No matching analysis found with all expected gloss and morpheme data
			analysis = AddEmptyAnalysisToWordform(cache, candidateWordform);
			return false;
		}

		private static IAnalysis FindMatchingAnalysis(LcmCache cache, IWfiWordform candidateWordform, Word word,
			Dictionary<int, string> expectedGlosses, Dictionary<int, string> expectedCats)
		{
			IAnalysis analysis = null;
			var wsFact = cache.WritingSystemFactory;
			// Look for an analysis that has the correct morphemes and a matching gloss
			foreach (var wfiAnalysis in candidateWordform.AnalysesOC)
			{
				var morphemeMatch = true;
				// verify that the analysis has a Morph Bundle with the expected morphemes from the import
				if (word.morphemes != null && wfiAnalysis.MorphBundlesOS.Count == word.morphemes?.morphs.Length &&
					!IsGuess(word.morphemes))
				{
					analysis = GetMostSpecificAnalysisForWordForm(wfiAnalysis);
					for (var i = 0; i < wfiAnalysis.MorphBundlesOS.Count; ++i)
					{
						var morphBundle = wfiAnalysis.MorphBundlesOS[i];
						var extantMorphForm = morphBundle.Form;
						var importMorphForm = word.morphemes.morphs[i].items.FirstOrDefault(item => item.type == "txt");
						var importFormWs = GetWsEngine(wsFact, importMorphForm?.lang);
						// compare the import item to the extant morph form
						if (importMorphForm == null || extantMorphForm == null ||
							TsStringUtils.IsNullOrEmpty(extantMorphForm.get_String(importFormWs.Handle)) ||
							!DecorateFormWithAffixMarkers(morphBundle.MorphRA?.MorphTypeRA, extantMorphForm.get_String(importFormWs.Handle).Text).Normalize()
								.Equals(importMorphForm.Value?.Normalize()))
						{
							morphemeMatch = false;
							break;
						}
					}
				}

				if (morphemeMatch)
				{
					var matchingGloss = wfiAnalysis.MeaningsOC.FirstOrDefault(g => VerifyGlossesMatch(g, expectedGlosses, expectedCats));
					if (matchingGloss != null)
					{
						return matchingGloss;
					}
				}
			}
			return null;
		}

		private static IAnalysis GetMostSpecificAnalysisForWordForm(IAnalysis candidateWordform)
		{
			var analysisTree = new AnalysisTree(candidateWordform);
			if(analysisTree.Gloss != null)
				return analysisTree.Gloss;
			if(analysisTree.WfiAnalysis != null)
				return analysisTree.WfiAnalysis;
			return candidateWordform;
		}

		private static IAnalysis CreateMissingForm(LcmCache cache, ITsString wordFormText,
			ITsString punctFormText, Dictionary<int, string> expectedForms)
		{
			if (wordFormText != null)
			{
				var wordForm = cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(wordFormText);
				foreach (var expected in expectedForms)
				{
					var wsHandle = expected.Key;
					var expectedValue = expected.Value;
					if (TsStringUtils.IsNullOrEmpty(wordForm.Form.get_String(wsHandle)))
					{
						wordForm.Form.set_String(wsHandle, TsStringUtils.MakeString(expectedValue, wsHandle));
					}
				}
				return wordForm;
			}
			if (punctFormText != null)
			{
				var punctForm = cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
				punctForm.Form = punctFormText;
				return punctForm;
			}

			return null;
		}

		private static IAnalysis AddEmptyAnalysisToWordform(LcmCache cache, IWfiWordform owningWordform)
		{
			var analysis = cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			owningWordform.AnalysesOC.Add(analysis);
			return analysis;
		}

		/// <summary>
		/// Match the wordform or punctuation form on the first vernacular writing system.
		/// Add any extra writing system data if the import data has it, but do not overwrite what is
		/// already in the cache.
		/// If there is not a match on the primary vernacular form nothing is set and false is returned
		/// </summary>
		private static bool MatchPrimaryFormAndAddMissingAlternatives(IAnalysis wordForm,
			Dictionary<int, string> expectedForms, int mainWritingSystem)
		{
			IWfiWordform wf = null;
			IPunctuationForm pf = null;

			// Assign wf or pf based on the type of wordForm
			switch (wordForm)
			{
				case IWfiWordform wordFormAsWf:
					wf = wordFormAsWf;
					break;
				case IPunctuationForm wordFormAsPf:
					pf = wordFormAsPf;
					break;
			}

			// We could have ended up here if there was a matched on an alternative writing system
			if(!expectedForms.TryGetValue(mainWritingSystem, out _))
				return false;

			foreach (var kvp in expectedForms)
			{
				var wsHandle = kvp.Key;
				var expectedValue = kvp.Value;
				var storedForm = wf?.Form.get_String(wsHandle) ?? pf?.GetForm(wsHandle);
				var newForm = TsStringUtils.MakeString(expectedValue, wsHandle);
				if (TsStringUtils.IsNullOrEmpty(storedForm)) // Extra data found in the import
				{
					if (wf != null)
					{
						wf.Form.set_String(wsHandle, newForm);
					}
					else if (pf != null)
					{
						pf.Form = newForm;
					}
				}
			}
			return true;
		}

		// Helper method to verify that all expected glosses match the stored glosses
		private static bool VerifyGlossesMatch(IWfiGloss wfiGloss,
			Dictionary<int, string> expectedGlosses,
			Dictionary<int, string> expectedCats)
		{
			foreach (var expectedGloss in expectedGlosses)
			{
				var wsHandle = expectedGloss.Key;
				var expectedValue = expectedGloss.Value;

				var storedGloss = wfiGloss.Form.get_String(wsHandle);
				if (storedGloss == null || storedGloss.Text != expectedValue)
					return false; // Mismatch found
			}
			foreach (var expectedCat in expectedCats)
			{
				if (!MatchesCatNameOrAbbreviation(expectedCat.Key, expectedCat.Value, wfiGloss.Analysis?.CategoryRA))
					return false;
			}

			return true;
		}

		private static bool MatchesCatNameOrAbbreviation(int ws, string text, IPartOfSpeech cat)
		{
			if (cat == null)
				return false;
			ITsString name = cat.Name.get_String(ws);
			if (name != null && name.Text == text)
				return true;
			ITsString abbr = cat.Abbreviation.get_String(ws);
			if (abbr != null && abbr.Text == text)
				return true;
			return false;
		}

		/// <summary>
		/// </summary>
		/// <param name="wordForm">The word Gloss. If multiple glosses, returns the last one created.</param>
		private static void UpgradeToWordGloss(Word word, ref IAnalysis wordForm)
		{
			var cache = wordForm.Cache;
			var wsFact = cache.WritingSystemFactory;
			if (s_importOptions.AnalysesLevel == ImportAnalysesLevel.WordGloss)
			{
				// test for adding multiple glosses in the same language. If so, create separate analyses with separate glosses.
				var fHasMultipleGlossesInSameLanguage = false;
				var dictMapLangToGloss = new Dictionary<string, string>();
				var processedGlossLangs = new HashSet<string>();
				foreach (var wordGlossItem in word.Items.Select(i => i)
							 .Where(i => i.type == "gls"))
				{
					string gloss;
					if (!dictMapLangToGloss.TryGetValue(wordGlossItem.lang, out gloss))
					{
						dictMapLangToGloss.Add(wordGlossItem.lang, wordGlossItem.Value);
						continue;
					}

					if (wordGlossItem.Value.Normalize().Equals(gloss?.Normalize())) continue;
					fHasMultipleGlossesInSameLanguage = true;
					break;
				}

				AnalysisTree analysisTree = new AnalysisTree(wordForm);
				foreach (var wordGlossItem in word.Items.Select(i => i)
							 .Where(i => i.type == "gls"))
				{
					if (wordGlossItem.analysisStatusSpecified &&
						wordGlossItem.analysisStatus != analysisStatusTypes.humanApproved)
						continue;
					// first make sure that an existing gloss does not already exist. (i.e. don't add duplicate glosses)
					var wsNewGloss = GetWsEngine(wsFact, wordGlossItem.lang).Handle;
					var wfiWord = wordForm.Wordform;

					if (fHasMultipleGlossesInSameLanguage && processedGlossLangs.Contains(wordGlossItem.lang))
						// create a new WfiAnalysis to store a new gloss
						analysisTree = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(wfiWord);
					// else, reuse the same analysisTree for setting a gloss alternative
					if (analysisTree.Gloss == null)
					{
						var wfiGloss = cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
						var analysis = analysisTree.WfiAnalysis;
						if (analysis == null)
						{
							analysis = (IWfiAnalysis)AddEmptyAnalysisToWordform(cache, wfiWord);
						}
						analysis.MeaningsOC.Add(wfiGloss);
						analysisTree = new AnalysisTree(wfiGloss);
					}
					analysisTree.Gloss.Form.set_String(wsNewGloss, wordGlossItem.Value);
					if (!IsGuess(word.morphemes))
						// Make sure this analysis is marked as user-approved (green check mark)
						cache.LangProject.DefaultUserAgent.SetEvaluation(
							analysisTree.WfiAnalysis, Opinions.approves);
					wordForm = analysisTree.Gloss;
					// If there are no morphemes defined for the word define a single one for the word.
					if(word.morphemes == null || word.morphemes.morphs.Length == 0)
					{
						// Create a morpheme form that matches the wordform.
						var morphemeBundle = cache.ServiceLocator
							.GetInstance<IWfiMorphBundleFactory>().Create();
						var wordItem = word.Items.Select(i => i).First(i => i.type == "txt");
						var wsWord = GetWsEngine(wsFact, wordItem.lang).Handle;
						analysisTree.WfiAnalysis.MorphBundlesOS.Add(morphemeBundle);
						morphemeBundle.Form.set_String(wsWord, wordItem.Value);
					}

					processedGlossLangs.Add(wordGlossItem.lang);
				}
			}

			if (wordForm != null && IsGuess(word.morphemes))
				// Ignore gloss if morphological analysis was only a guess.
				wordForm = wordForm.Wordform;
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
			InterlinearObjects objects = new InterlinearObjects();

			// Set top-level metadata properties.
			SetObjectPropertyValues(newText, interlinText.Items, objects.GetXmlPropertyMap("Text"), cache);

			// Create objects except for links.
			if (interlinText.objects != null)
			{
				foreach (var obj in interlinText.objects)
				{
					CreateFullObject(obj, objects, cache);
				}
			}

			// Create links after all objects have been created.
			CreateItemLinks(interlinText.guid, interlinText.Items, objects, cache);
			if (interlinText.objects != null)
			{
				foreach (var obj in interlinText.objects)
				{
					CreateItemLinks(obj.guid, obj.item, objects, cache);
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

		/// <summary>
		/// Create object and fill in item properties.
		/// </summary>
		private static void CreateFullObject(Interlineartext.Object obj, InterlinearObjects objects, LcmCache cache)
		{
			Guid guid = new Guid(obj.guid);
			ICmObjectRepository repository = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repository.TryGetObject(guid, out ICmObject icmObject))
			{
				icmObject = CreateObject(obj, objects, cache);
				Dictionary<string, string> xmlPropertyMap = objects.GetXmlPropertyMap(obj.type);
				SetObjectPropertyValues(icmObject, obj.item, xmlPropertyMap, cache);
			}
		}

		private static void SetObjectPropertyValues(ICmObject icmObject, item[] items, Dictionary<string, string> xmlPropertyMap, LcmCache cache)
		{
			if (items == null) return;
			Type objType = icmObject.GetType();
			/// Set item properties.
			foreach (var item in items)
			{
				if (item.guid != null)
				{
					continue;
				}
				if (item.type == "owner" && icmObject is ICmPossibility possibility)
				{
					// Add possibility to a possibility list rooted in LanguageProject.
					foreach (PropertyInfo langPropInfo in cache.LanguageProject.GetType().GetProperties())
					{
						string langPropName = langPropInfo.Name;
						if (langPropName.EndsWith("OA"))
							langPropName = langPropName.Substring(0, langPropName.Length - 2);
						if (langPropName == item.Value)
						{
							var langPropValue = langPropInfo.GetValue(cache.LanguageProject);
							if (langPropValue is ICmPossibilityList possibilityList)
							{
								possibilityList.PossibilitiesOS.Add(possibility);
							}
						}
					}
					continue;
				}
				object value = null;
				string propName = null;
				if (xmlPropertyMap.ContainsKey(item.type))
					propName = xmlPropertyMap[item.type];
				else if (item.type == "date-created")
					propName = "DateCreated";
				else if (item.type == "date-modified")
					propName = "DateModified";
				PropertyInfo propInfo = objType.GetProperty(propName);
				object currentValue = propInfo.GetValue(icmObject, null);
				if (currentValue is bool)
				{
					value = item.Value.ToLower() == "true";
				}
				else if (currentValue is DateTime)
				{
					value = DateTime.Parse(item.Value, null, System.Globalization.DateTimeStyles.AssumeUniversal);
				}
				else if (currentValue is IMultiString)
				{
					// value is an ITsString.
					value = GetItemValue(item, cache.WritingSystemFactory);
				}
				else if (currentValue is IMultiUnicode)
				{
					// value is an ITsString.
					value = GetItemValue(item, cache.WritingSystemFactory);
				}
				else if (currentValue is int)
				{
					int intValue = 0;
					if (int.TryParse(item.Value, out intValue))
						value = intValue;
				}
				SetPropertyValue(icmObject, propName, value);
			}
		}

		/// <summary>
		/// Create object.
		/// </summary>
		private static ICmObject CreateObject(Interlineartext.Object obj, InterlinearObjects objects, LcmCache cache)
		{
			string objType = objects.XmlTypeMap[obj.type];
			switch (objType)
			{
				case "CmAnthroItem":
					return cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create(new Guid(obj.guid));
				case "CmLocation":
					return cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create(new Guid(obj.guid));
				case "CmPerson":
					return cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create(new Guid(obj.guid));
				case "CmPossibility":
					return cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(new Guid(obj.guid));
				case "RnGenericRec":
					IRnGenericRec record = cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create(new Guid(obj.guid));
					cache.LanguageProject.ResearchNotebookOA.RecordsOC.Add(record);
					return record;
				case "RnRoledPartic":
					return cache.ServiceLocator.GetInstance<IRnRoledParticFactory>().Create(new Guid(obj.guid));
			}
			return null;
		}

		private static void CreateItemLinks(string objGuid, item[] items, InterlinearObjects objects, LcmCache cache)
		{
			if (items != null)
			{
				foreach (var item in items)
				{
					if (item.guid != null)
					{
						CreateLink(objGuid, item.type, item.guid, item.lang, item.Value, objects, cache);
					}
				}
			}
		}


		/// <summary>
		/// Create given link.
		/// </summary>
		private static void CreateLink(string objGuid, string xmlPropName, string valueGuid, string lang, string valueName, InterlinearObjects objects, LcmCache cache)
		{
			ICmObjectRepository repository = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			ICmObject obj = repository.GetObject(new Guid(objGuid));
			Dictionary<string, string> xmlPropertyMap = objects.InvertMap(objects.GetPropertyMap(obj.GetType().Name));
			string propName = (xmlPropName == "owner") ? "Owner" : xmlPropertyMap[xmlPropName];
			ICmObject value = null;
			if (!repository.TryGetObject(new Guid(valueGuid), out value))
			{
				value = CreateObjectByName(obj, propName, lang, valueName, valueGuid, cache);
			}
			SetPropertyValue(obj, propName, value);
		}

		private static ICmObject CreateObjectByName(ICmObject obj, string propName, string lang, string valueName, string valueGuid, LcmCache cache)
		{
			ICmPossibilityList possibilityList = null;
			string possibilityType = "ICmPossibility";
			switch (propName)
			{
				case "AnthroCodesRC":
					{
						possibilityList = cache.LanguageProject.AnthroListOA;
						possibilityType = "ICmAnthroItem";
						break;
					}
				case "GenresRC":
					{
						possibilityList = cache.LanguageProject.GenreListOA;
						break;
					}
				case "ParticipantsRC":
				case "ResearchersRC":
				case "Source":
					{
						possibilityList = cache.LanguageProject.PeopleOA;
						possibilityType = "ICmPerson";
						break;
					}
				case "RoleRA":
					{
						possibilityList = cache.LanguageProject.RolesOA;
						break;
					}
				case "LocationsRC":
					{
						possibilityList = cache.LanguageProject.LocationsOA;
						possibilityType = "ICmLocation";
						break;
					}
			}
			if (possibilityList == null)
			{
				return null;
			}
			// Look for an existing possibility with the given name.
			int ws = GetWsEngine(cache.WritingSystemFactory, lang).Handle;
			foreach (var candidate in possibilityList.ReallyReallyAllPossibilities)
			{
				ITsString name = candidate.Name.BestAnalysisAlternative;
				if (name.Text == valueName && name.get_WritingSystemAt(0) == ws)
				{
					// Should we set the candidate's guid to valueGuid?
					return candidate;
				}
			}
			// Create a new possibility.
			ICmPossibility newPossibility = null;
			Guid guid = new Guid(valueGuid);
			switch (possibilityType)
			{
				case "ICmAnthroItem":
					newPossibility = cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create(guid);
					break;
				case "ICmLocation":
					newPossibility = cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create(guid);
					break;
				case "ICmPerson":
					newPossibility = cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create(guid);
					break;
				case "ICmPossibility":
					newPossibility = cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(guid);
					break;
			}
			if (newPossibility != null)
			{
				newPossibility.Name.set_String(ws, valueName);
				possibilityList.PossibilitiesOS.Add(newPossibility);
			}
			return newPossibility;
		}

		/// <summary>
		/// Set object property to value.
		private static void SetPropertyValue(ICmObject obj, string propName, object value)
		{
			if (value == null)
				return;
			if (propName == "Owner")
			{
				// We store Owner but set SubPossibilitiesOS.
				SetPropertyValue((ICmObject)value, "SubPossibilitiesOS", obj);
				return;
			}
			if (propName == "AssociatedNotebookRecord")
			{
				SetPropertyValue((ICmObject)value, "TextRA", obj);
				return;
			}
			PropertyInfo propInfo = obj.GetType().GetProperty(propName);
			object currentValue = propInfo.GetValue(obj, null);
			if (currentValue == null)
			{
				propInfo.SetValue(obj, value);
				return;
			}

			Type currentValueType = currentValue.GetType();
			if (value.GetType().IsInstanceOfType(currentValueType))
			{
				propInfo.SetValue(obj, value);
			}
			else if (currentValueType.IsGenericType &&
				(currentValueType.GetGenericTypeDefinition().Name == "LcmOwningCollection`1" ||
				 currentValueType.GetGenericTypeDefinition().Name == "LcmOwningSequence`1" ||
				 currentValueType.GetGenericTypeDefinition().Name == "LcmReferenceCollection`1" ||
				 currentValueType.GetGenericTypeDefinition().Name == "LcmReferenceSequence`1"))
			{
				Type itemType = currentValueType.GetGenericArguments()[0];
				if (itemType.IsAssignableFrom(value.GetType()))
				{
					var addMethod = currentValueType.GetMethod("Add");
					addMethod?.Invoke(currentValue, new[] { value });
				}
			}
			else if (currentValue is IMultiString multiString)
			{
				if (value is ITsString itsString)
				{
					multiString.set_String(itsString.get_WritingSystemAt(0), itsString);
				}
			}
			else if (currentValue is IMultiUnicode multiUnicode)
			{
				if (value is ITsString itsString)
				{
					multiUnicode.set_String(itsString.get_WritingSystemAt(0), itsString.Text);
				}
			}
			else
			{
				propInfo.SetValue(obj, value);
			}
		}

	}
}