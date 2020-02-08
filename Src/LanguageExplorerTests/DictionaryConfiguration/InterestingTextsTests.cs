// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	/// <summary>
	/// Tests the InterestingTextsList class.
	/// </summary>
	[TestFixture]
	public class InterestingTextsTests
	{
		private static int s_nextHvo = 1;
		private FlexComponentParameters _flexComponentParameters;
		MockStTextRepository _mockStTextRepo;
		private InterestingTextsChangedArgs _lastTextsChangedArgs;
		private readonly List<IScrSection> _mockedSections = new List<IScrSection>();

		[SetUp]
		public void Initialize()
		{
			_mockStTextRepo = new MockStTextRepository();
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			_mockedSections.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
		}

		/// <summary>
		/// Verify we can make one and it obtains the texts from Language Project.
		/// </summary>
		[Test]
		public void GetCoreTexts()
		{
			var mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(_flexComponentParameters.PropertyTable, mockTextRep, _mockStTextRepo);
			VerifyList(CurrentTexts(mockTextRep), testObj.InterestingTexts, "texts from initial list of two");
			// Make sure it works if there are none.
			Assert.AreEqual(0, new InterestingTextList(_flexComponentParameters.PropertyTable, new MockTextRepository(), _mockStTextRepo).InterestingTexts.Count());
			Assert.IsTrue(testObj.IsInterestingText(mockTextRep._texts[0].ContentsOA));
			Assert.IsFalse(testObj.IsInterestingText(new MockStText()));
		}

		[Test]
		public void AddAndRemoveCoreTexts()
		{
			var mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(_flexComponentParameters.PropertyTable, mockTextRep, _mockStTextRepo);
			Assert.AreEqual(0, testObj.ScriptureTexts.Count());
			testObj.InterestingTextsChanged += TextsChangedHandler;
			AddMockText(mockTextRep, testObj);
			VerifyList(CurrentTexts(mockTextRep), testObj.InterestingTexts, "texts from initial list of two");
			VerifyTextsChangedArgs(2, 1, 0);
			var removed = mockTextRep._texts[1].ContentsOA;
			RemoveText(mockTextRep, testObj,1);
			VerifyList(CurrentTexts(mockTextRep), testObj.InterestingTexts, "texts from initial list of two");
			VerifyTextsChangedArgs(1, 0, 1);
			Assert.IsTrue(testObj.IsInterestingText(mockTextRep._texts[1].ContentsOA), "text not removed still interesting");
			Assert.IsFalse(testObj.IsInterestingText(removed), "removed text no longer interesting");
		}

		[Test]
		public void ReplaceCoreText()
		{
			var mockTextRepo = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(_flexComponentParameters.PropertyTable, mockTextRepo, _mockStTextRepo);
			var firstStText = testObj.InterestingTexts.First();
			var firstText = (IText)firstStText.Owner;
			var replacement = new MockStText();
			testObj.InterestingTextsChanged += TextsChangedHandler;
			firstText.ContentsOA = replacement;
			testObj.PropChanged(firstText.Hvo, TextTags.kflidContents, 0, 1, 1);
			VerifyList(CurrentTexts(mockTextRepo), testObj.InterestingTexts, "texts after replace");
			// Various possibilities could be valid for the arguments...for now just verify we got something.
			Assert.That(_lastTextsChangedArgs, Is.Not.Null);
		}

		/// <summary>
		/// Should just omit any GUIDs that don't correspond to valid objects.
		/// </summary>
		[Test]
		public void PropertyTableHasInvalidObjects()
		{
			var mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			MakeMockScriptureSection();
			_flexComponentParameters.PropertyTable.SetProperty(InterestingTextList.PersistPropertyName, InterestingTextList.MakeIdList(new ICmObject[] { _mockedSections[0].ContentOA, _mockedSections[0].HeadingOA }) + "," + Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + ",$%^#@+", true, true);
			var testObj = new InterestingTextList(_flexComponentParameters.PropertyTable, mockTextRep, _mockStTextRepo, true);
			testObj.InterestingTextsChanged += TextsChangedHandler;
			var expectedScripture = new List<IStText>
			{
				_mockedSections[0].ContentOA,
				_mockedSections[0].HeadingOA
			};
			VerifyList(expectedScripture, testObj.ScriptureTexts, "Just two valid Guids");
		}

		private void MakeMockScriptureSection()
		{
			IScrSection section = new MockScrSection();
			_mockedSections.Add(section);
			section.ContentOA = new MockStText();
			section.HeadingOA = new MockStText();
			_mockStTextRepo._texts.Add(section.ContentOA);
			_mockStTextRepo._texts.Add(section.HeadingOA);
		}

		private void VerifyTextsChangedArgs(int insertAt, int inserted, int deleted)
		{
			Assert.AreEqual(insertAt, _lastTextsChangedArgs.InsertedAt);
			Assert.AreEqual(inserted, _lastTextsChangedArgs.NumberInserted);
			Assert.AreEqual(deleted, _lastTextsChangedArgs.NumberDeleted);
		}

		private void TextsChangedHandler(object sender, InterestingTextsChangedArgs e)
		{
			_lastTextsChangedArgs = e;
		}

		private static List<IStText> CurrentTexts(MockTextRepository mockTextRep)
		{
			return (mockTextRep._texts.Select(text => text.ContentsOA)).ToList();
		}

		private static void RemoveText(MockTextRepository mockTextRep, InterestingTextList testObj, int index)
		{
			var oldTextHvo = mockTextRep._texts[index].Hvo;
			((MockText)mockTextRep._texts[index]).IsValidObject = false;
			((MockStText) mockTextRep._texts[index].ContentsOA).IsValidObject = false;
			mockTextRep._texts.RemoveAt(index);
			testObj.PropChanged(oldTextHvo, TextTags.kflidContents, 0, 0, 1);
		}

		private static void AddMockText(MockTextRepository mockTextRep, InterestingTextList testObj)
		{
			IText newText = new MockText();
			mockTextRep._texts.Add(newText);
			testObj.PropChanged(newText.Hvo, TextTags.kflidContents, 0, 1, 0);
		}

		private static int NextHvo() { return s_nextHvo++; }

		// Verify the two lists have the same members (not necessarily in the same order)
		private static void VerifyList(List<IStText> expected, IEnumerable<IStText> actual, string comment)
		{
			Assert.AreEqual(expected.Count, actual.Count(), comment + " count");
			var expectedSet = new HashSet<IStText>(expected);
			var actualSet = new HashSet<IStText>(actual);
			var unexpected = actualSet.Except(expectedSet);
			Assert.AreEqual(0, unexpected.Count(), comment + " has extra elements");
			var missing = expectedSet.Except(actualSet);
			Assert.AreEqual(0, missing.Count(), comment + " has missing elements");
		}

		private static MockTextRepository MakeMockTextRepoWithTwoMockTexts()
		{
			var mockTextRep = new MockTextRepository();
			var mockText1 = new MockText();
			var mockText2 = new MockText();
			mockTextRep._texts.Add(mockText1);
			mockTextRep._texts.Add(mockText2);
			return mockTextRep;
		}

		private sealed class MockStTextRepository : IStTextRepository
		{
			internal readonly List<IStText> _texts = new List<IStText>();

			public IEnumerable<ICmObject> AllInstances(int classId)
			{
				throw new NotSupportedException();
			}

			public IStText GetObject(ICmObjectId id)
			{
				throw new NotSupportedException();
			}

			public IStText GetObject(Guid id)
			{
				foreach (var st in _texts.Where(st => st.Guid == id))
				{
					return st;
				}
				Assert.Fail("Looking for invalid Guid " + id);
				return null; // make compiler happy.
			}

			public bool TryGetObject(Guid id, out IStText obj)
			{
				foreach (var st in _texts.Where(st => st.Guid == id))
				{
					obj = st;
					return true;
				}
				obj = null;
				return false;
			}

			public IStText GetObject(int hvo)
			{
				foreach (var st in _texts.Where(st => st.Hvo == hvo))
				{
					return st;
				}
				Assert.Fail("Looking for invalid HVO " + hvo);
				return null; // make compiler happy.
			}

			public bool TryGetObject(int hvo, out IStText obj)
			{
				throw new NotSupportedException();
			}

			public IEnumerable<IStText> AllInstances()
			{
				throw new NotSupportedException();
			}

			public int Count
			{
				get { throw new NotSupportedException(); }
			}

			public IList<IStText> GetObjects(IList<int> hvos)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class MockTextRepository : ITextRepository
		{

			internal readonly List<IText> _texts = new List<IText>();

			public IEnumerable<ICmObject> AllInstances(int classId)
			{
				throw new NotSupportedException();
			}

			public IText GetObject(ICmObjectId id)
			{
				throw new NotSupportedException();
			}

			public IText GetObject(Guid id)
			{
				throw new NotSupportedException();
			}

			public bool TryGetObject(Guid guid, out IText obj)
			{
				throw new NotSupportedException();
			}

			public IText GetObject(int hvo)
			{
				foreach (var st in _texts.Where(st => st.Hvo == hvo))
				{
					return st;
				}
				Assert.Fail("Looking for invalid HVO " + hvo);
				return null; // make compiler happy.
			}

			public bool TryGetObject(int hvo, out IText obj)
			{
				throw new NotSupportedException();
			}

			public IEnumerable<IText> AllInstances()
			{
				return _texts.ToArray();
			}

			public int Count
			{
				get { throw new NotSupportedException(); }
			}
		}

		private class MockCmObject : ICmObject
		{
			private ICmObject _mockOwnerOfClass;
			private readonly int _hvo;
			private readonly Guid _guid;

			internal MockCmObject()
			{
				_hvo = NextHvo();
				IsValidObject = true;
				_guid = Guid.NewGuid();
			}

			private IEnumerable<ICmObject> AllOwnedObjects { get; set; }

			IEnumerable<ICmObject> ICmObject.AllOwnedObjects => AllOwnedObjects;

			int ICmObject.Hvo => _hvo;

			public ICmObject Owner { get; set; }

			int ICmObject.OwningFlid
			{
				get { throw new NotSupportedException(); }
			}

			int ICmObject.OwnOrd
			{
				get { throw new NotSupportedException(); }
			}

			int ICmObject.ClassID
			{
				get { throw new NotSupportedException(); }
			}

			Guid ICmObject.Guid => _guid;

			ICmObjectId ICmObjectOrId.Id
			{
				get { throw new NotSupportedException(); }
			}

			ICmObject ICmObjectOrId.GetObject(ICmObjectRepository repo)
			{
				throw new NotSupportedException();
			}

			string ICmObject.ClassName
			{
				get { throw new NotSupportedException(); }
			}

			/// <summary>
			/// Delete the recipient object.
			/// </summary>
			void ICmObject.Delete()
			{
				throw new NotSupportedException();
			}

			ILcmServiceLocator ICmObject.Services
			{
				get { throw new NotSupportedException(); }
			}

			ICmObject ICmObject.OwnerOfClass(int clsid)
			{
				return _mockOwnerOfClass;
			}

			T ICmObject.OwnerOfClass<T>()
			{
				throw new NotSupportedException();
			}

			ICmObject ICmObject.Self
			{
				get { throw new NotSupportedException(); }
			}

			bool ICmObject.CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
			{
				throw new NotSupportedException();
			}

			void ICmObject.PostClone(Dictionary<int, ICmObject> copyMap)
			{
				throw new NotSupportedException();
			}

			void ICmObject.AllReferencedObjects(List<ICmObject> collector)
			{
				throw new NotSupportedException();
			}

			bool ICmObject.IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
			{
				throw new NotSupportedException();
			}

			bool ICmObject.IsOwnedBy(ICmObject possibleOwner)
			{
				throw new NotSupportedException();
			}

			ICmObject ICmObject.ReferenceTargetOwner(int flid)
			{
				throw new NotSupportedException();
			}

			bool ICmObject.IsFieldRequired(int flid)
			{
				throw new NotSupportedException();
			}

			int ICmObject.IndexInOwner
			{
				get { throw new NotSupportedException(); }
			}

			IEnumerable<ICmObject> ICmObject.ReferenceTargetCandidates(int flid)
			{
				throw new NotSupportedException();
			}

			public bool IsValidObject { get; set; }

			LcmCache ICmObject.Cache
			{
				get { throw new NotSupportedException(); }
			}

			void ICmObject.MergeObject(ICmObject objSrc)
			{
				throw new NotSupportedException();
			}

			void ICmObject.MergeObject(ICmObject objSrc, bool fLoseNoStringData)
			{
				throw new NotSupportedException();
			}

			bool ICmObject.CanDelete
			{
				get { throw new NotSupportedException(); }
			}

			ITsString ICmObject.ObjectIdName
			{
				get { throw new NotSupportedException(); }
			}

			string ICmObject.ShortName
			{
				get { throw new NotSupportedException(); }
			}

			ITsString ICmObject.ShortNameTSS
			{
				get { throw new NotSupportedException(); }
			}

			ITsString ICmObject.DeletionTextTSS
			{
				get { throw new NotSupportedException(); }
			}

			ITsString ICmObject.ChooserNameTS
			{
				get { throw new NotSupportedException(); }
			}

			string ICmObject.SortKey
			{
				get { throw new NotSupportedException(); }
			}

			string ICmObject.SortKeyWs
			{
				get { throw new NotSupportedException(); }
			}

			int ICmObject.SortKey2
			{
				get { throw new NotSupportedException(); }
			}

			string ICmObject.SortKey2Alpha
			{
				get { throw new NotSupportedException(); }
			}

			HashSet<ICmObject> ICmObject.ReferringObjects
			{
				get { throw new NotSupportedException(); }
			}

			IEnumerable<ICmObject> ICmObject.OwnedObjects
			{
				get { throw new NotSupportedException(); }
			}
		}

		private sealed class MockScrSection : MockCmObject, IScrSection
		{
			private IStText _heading;
			private IStText _content;

			IStText IScrSection.HeadingOA
			{
				get { return _heading; }
				set
				{
					_heading = value;
					if (_heading != null)
					{
						((MockCmObject)_heading).Owner = this;
					}
				}
			}

			IStText IScrSection.ContentOA
			{
				get { return _content; }
				set
				{
					_content = value;
					if (_content != null)
					{
						((MockCmObject)_content).Owner = this;
					}
				}
			}

			int IScrSection.VerseRefStart
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			int IScrSection.VerseRefEnd
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			int IScrSection.VerseRefMin
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			int IScrSection.VerseRefMax
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			bool IScrSection.ContainsChapter(int chapter)
			{
				throw new NotSupportedException();
			}

			bool IScrSection.IsIntro
			{
				get { throw new NotSupportedException(); }
			}

			int IScrSection.ContentParagraphCount
			{
				get { throw new NotSupportedException(); }
			}

			int IScrSection.HeadingParagraphCount
			{
				get { throw new NotSupportedException(); }
			}

			IEnumerable<IScrTxtPara> IScrSection.Paragraphs
			{
				get { throw new NotSupportedException(); }
			}

			bool IScrSection.IsFirstScriptureSection
			{
				get { throw new NotSupportedException(); }
			}

			IScrSection IScrSection.PreviousSection
			{
				get { throw new NotSupportedException(); }
			}

			IScrSection IScrSection.NextSection
			{
				get { throw new NotSupportedException(); }
			}

			IStTxtPara IScrSection.FirstContentParagraph
			{
				get { throw new NotSupportedException(); }
			}

			IStTxtPara IScrSection.LastContentParagraph
			{
				get { throw new NotSupportedException(); }
			}

			IStTxtPara IScrSection.FirstHeadingParagraph
			{
				get { throw new NotSupportedException(); }
			}

			IStTxtPara IScrSection.LastHeadingParagraph
			{
				get { throw new NotSupportedException(); }
			}

			ContextValues IScrSection.Context
			{
				get { throw new NotSupportedException(); }
			}

			bool IScrSection.StartsWithVerseOrChapterNumber
			{
				get { throw new NotSupportedException(); }
			}

			bool IScrSection.StartsWithChapterNumber
			{
				get { throw new NotSupportedException(); }
			}

			bool IScrSection.ContainsReference(ScrReference reference)
			{
				throw new NotSupportedException();
			}

			void IScrSection.MoveHeadingParasToContent(int indexFirstPara, IStStyle newStyle)
			{
				throw new NotSupportedException();
			}

			IScrSection IScrSection.SplitSectionHeading_atIP(int iParaSplit, int ichSplit)
			{
				throw new NotSupportedException();
			}

			IScrSection IScrSection.SplitSectionContent_atIP(int iParaSplit, ITsString headingText, string headingParaStyle)
			{
				throw new NotSupportedException();
			}

			IScrSection IScrSection.SplitSectionContent_atIP(int iParaSplit, int ichSplit)
			{
				throw new NotSupportedException();
			}

			IScrSection IScrSection.SplitSectionContent_atIP(int iParaSplit, int ichSplit, IStText newHeading)
			{
				throw new NotSupportedException();
			}

			void IScrSection.GetDisplayRefs(out BCVRef startRef, out BCVRef endRef)
			{
				throw new NotSupportedException();
			}

			List<IScrFootnote> IScrSection.GetFootnotes()
			{
				throw new NotSupportedException();
			}

			IScrFootnote IScrSection.FindFirstFootnote(out int iPara, out int ich, out int tag)
			{
				throw new NotSupportedException();
			}

			IScrFootnote IScrSection.FindLastFootnote(out int iPara, out int ich, out int tag)
			{
				throw new NotSupportedException();
			}

			void IScrSection.MoveContentParasToHeading(int indexLastPara, IStStyle newStyle)
			{
				throw new NotSupportedException();
			}

			IScrSection IScrSection.SplitSectionContent_ExistingParaBecomesHeading(int iPara, int cParagraphs, IStStyle newStyle)
			{
				throw new NotSupportedException();
			}

			void IScrSection.SplitSectionHeading_ExistingParaBecomesContent(int iParaStart, int iParaEnd, IStStyle newStyle)
			{
				throw new NotSupportedException();
			}

			void ICloneableCmObject.SetCloneProperties(ICmObject clone)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class MockText : MockCmObject, IText
		{
			private IStText _contents;

			private IText AsIText => this;

			internal MockText()
			{
				AsIText.ContentsOA = new MockStText();
			}

			public IRnGenericRec AssociatedNotebookRecord
			{
				get { throw new NotSupportedException(); }
			}

			/// <summary>
			/// Get or set the MediaFiles
			/// </summary>
			public ICmMediaContainer MediaFilesOA
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			public void AssociateWithNotebook(bool makeYourOwnUow)
			{
				throw new NotSupportedException();
			}

			IMultiString IText.Source
			{
				get { throw new NotSupportedException(); }
			}

			IStText IText.ContentsOA
			{
				get { return _contents; }
				set
				{
					if (_contents != null)
					{
						((MockStText)_contents).IsValidObject = false;
					}
					_contents = value;
					if (_contents != null)
					{
						((MockStText)_contents).Owner = this;
					}
				}
			}

			ILcmReferenceCollection<ICmPossibility> IText.GenresRC
			{
				get { throw new NotSupportedException(); }
			}

			IMultiUnicode IText.Abbreviation
			{
				get { throw new NotSupportedException(); }
			}

			bool IText.IsTranslated
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			IMultiUnicode ICmMajorObject.Name
			{
				get { throw new NotSupportedException(); }
			}

			DateTime ICmMajorObject.DateCreated
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			DateTime ICmMajorObject.DateModified
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			IMultiString ICmMajorObject.Description
			{
				get { throw new NotSupportedException(); }
			}

			ILcmOwningCollection<IPublication> ICmMajorObject.PublicationsOC
			{
				get { throw new NotSupportedException(); }
			}

			ILcmOwningCollection<IPubHFSet> ICmMajorObject.HeaderFooterSetsOC
			{
				get { throw new NotSupportedException(); }
			}

			IPubHFSet ICmMajorObject.FindHeaderFooterSetByName(string name)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class MockStText : MockCmObject, IStText
		{
			ILcmOwningSequence<IStPara> IStText.ParagraphsOS
			{
				get { throw new NotSupportedException(); }
			}

			bool IStText.RightToLeft
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			ILcmOwningCollection<ITextTag> IStText.TagsOC
			{
				get { throw new NotSupportedException(); }
			}

			DateTime IStText.DateModified
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			IStTxtPara IStText.this[int i]
			{
				get { throw new NotSupportedException(); }
			}

			bool IStText.IsEmpty
			{
				get { throw new NotSupportedException(); }
			}

			IScrFootnote IStText.FindFirstFootnote(out int iPara, out int ich)
			{
				throw new NotSupportedException();
			}

			IScrFootnote IStText.FindLastFootnote(out int iPara, out int ich)
			{
				throw new NotSupportedException();
			}

			IScrFootnote IStText.FindNextFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition)
			{
				throw new NotSupportedException();
			}

			IScrFootnote IStText.FindPreviousFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition)
			{
				throw new NotSupportedException();
			}

			IStTxtPara IStText.AddNewTextPara(string paraStyleName)
			{
				throw new NotSupportedException();
			}

			IStTxtPara IStText.InsertNewTextPara(int iPos, string paraStyleName)
			{
				throw new NotSupportedException();
			}

			IStTxtPara IStText.InsertNewPara(int paragraphIndex, string paraStyleName, ITsString tss)
			{
				throw new NotSupportedException();
			}

			IMultiAccessorBase IStText.Title
			{
				get { throw new NotSupportedException(); }
			}

			IMultiAccessorBase IStText.Source
			{
				get { throw new NotSupportedException(); }
			}

			IMultiAccessorBase IStText.Comment
			{
				get { throw new NotSupportedException(); }
			}

			List<ICmPossibility> IStText.GenreCategories
			{
				get { throw new NotSupportedException(); }
			}

			int IStText.MainWritingSystem
			{
				get { throw new NotSupportedException(); }
			}

			bool IStText.IsTranslation
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			HashSet<IWfiWordform> IStText.UniqueWordforms()
			{
				throw new NotSupportedException();
			}

			void IStText.DeleteParagraph(IStTxtPara para)
			{
				throw new NotSupportedException();
			}
		}
	}
}
