using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests the InterestingTextsList class.
	/// </summary>
	[TestFixture]
	public class InterestingTextsTests: Test.TestUtils.BaseTest
	{
		MockStTextRepository m_mockStTextRepo;
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;

		[SetUp]
		public void Initialize()
		{
			m_mockStTextRepo = new MockStTextRepository();
			m_mediator = new Mediator();
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(new Test.TestUtils.MockPublisher());
			m_sections.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			m_mediator.Dispose();
			m_mediator = null;
			m_propertyTable.Dispose();
			m_propertyTable = null;
		}

		/// <summary>
		/// Verify we can make one and it obtains the texts from Language Project.
		/// </summary>
		[Test]
		public void GetCoreTexts()
		{
			MockTextRepository mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(m_mediator, m_propertyTable, mockTextRep, m_mockStTextRepo);
			VerifyList(CurrentTexts(mockTextRep),
				testObj.InterestingTexts, "texts from initial list of two");
			// Make sure it works if there are none.
			Assert.AreEqual(0, new InterestingTextList(m_mediator, m_propertyTable, new MockTextRepository(), m_mockStTextRepo).InterestingTexts.Count());
			Assert.IsTrue(testObj.IsInterestingText(mockTextRep.m_texts[0].ContentsOA));
			Assert.IsFalse(testObj.IsInterestingText(new MockStText()));
		}

		[Test]
		public void AddAndRemoveCoreTexts()
		{
			MockTextRepository mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(m_mediator, m_propertyTable, mockTextRep, m_mockStTextRepo);
			Assert.AreEqual(0, testObj.ScriptureTexts.Count());
			testObj.InterestingTextsChanged += TextsChangedHandler;
			MockText newText = AddMockText(mockTextRep, testObj);
			VerifyList(CurrentTexts(mockTextRep),
				testObj.InterestingTexts, "texts from initial list of two");
			VerifyTextsChangedArgs(2, 1, 0);
			var removed = mockTextRep.m_texts[1].ContentsOA;
			RemoveText(mockTextRep, testObj,1);
			VerifyList(CurrentTexts(mockTextRep),
				testObj.InterestingTexts, "texts from initial list of two");
			VerifyTextsChangedArgs(1, 0, 1);
			Assert.IsTrue(testObj.IsInterestingText(mockTextRep.m_texts[1].ContentsOA), "text not removed still interesting");
			Assert.IsFalse(testObj.IsInterestingText(removed), "removed text no longer interesting");
		}

		[Test]
		public void ReplaceCoreText()
		{
			MockTextRepository mockTextRepo = MakeMockTextRepoWithTwoMockTexts();
			var testObj = new InterestingTextList(m_mediator, m_propertyTable, mockTextRepo, m_mockStTextRepo);
			var firstStText = testObj.InterestingTexts.First();
			MockText firstText = firstStText.Owner as MockText;
			var replacement = new MockStText();
			testObj.InterestingTextsChanged += TextsChangedHandler;
			firstText.ContentsOA = replacement;
			testObj.PropChanged(firstText.Hvo, TextTags.kflidContents, 0, 1, 1);

			VerifyList(CurrentTexts(mockTextRepo),
				testObj.InterestingTexts, "texts after replace");
			// Various possibilities could be valid for the arguments...for now just verify we got something.
			Assert.That(m_lastTextsChangedArgs, Is.Not.Null);
		}
		[Test]
		[Ignore("Temporary until we figure out propchanged for unowned Texts.")]
		public void AddAndRemoveScripture()
		{
			List<IStText> expectedScripture;
			List<IStText> expected;
			InterestingTextList testObj = SetupTwoMockTextsAndOneScriptureSection(true, out expectedScripture, out expected);
			MakeMockScriptureSection();
			testObj.PropChanged(m_sections[1].Hvo, ScrSectionTags.kflidContent, 0, 1, 0);
			testObj.PropChanged(m_sections[1].Hvo, ScrSectionTags.kflidHeading, 0, 1, 0);
			VerifyList(expected, testObj.InterestingTexts, "new Scripture objects are not added automatically");
			VerifyScriptureList(testObj, expectedScripture, "new Scripture objects are not added automatically to ScriptureTexts");
			Assert.IsTrue(testObj.IsInterestingText(expectedScripture[0]));
			Assert.IsTrue(testObj.IsInterestingText(expectedScripture[1]));

			var remove = ((MockStText) m_sections[0].ContentOA);
			remove.IsValidObject = false;
			expected.Remove(m_sections[0].ContentOA); // before we clear ContentsOA!
			expectedScripture.Remove(m_sections[0].ContentOA);
			m_sections[0].ContentOA = null; // not normally valid, but makes things somewhat more consistent for test.
			testObj.PropChanged(m_sections[0].Hvo, ScrSectionTags.kflidContent, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (ContentsOA)");
			VerifyScriptureList(testObj, expectedScripture, "deleted Scripture texts are removed from ScriptureTexts (ContentsOA");
			VerifyTextsChangedArgs(2, 0, 1);
			Assert.IsFalse(testObj.IsInterestingText(remove));
			Assert.IsTrue(testObj.IsInterestingText(expectedScripture[0]));

			((MockStText)m_sections[0].HeadingOA).IsValidObject = false;
			expected.Remove(m_sections[0].HeadingOA); // before we clear ContentsOA!
			m_sections[0].HeadingOA = null; // not normally valid, but makes things somewhat more consistent for test.
			testObj.PropChanged(m_sections[0].Hvo, ScrSectionTags.kflidHeading, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (HeadingOA)");

			m_sections[0].ContentOA = new MockStText();
			var newTexts = new IStText[] {expected[0], expected[1], m_sections[0].ContentOA, m_sections[1].ContentOA, m_sections[1].HeadingOA};
			testObj.SetInterestingTexts(newTexts);
			VerifyTextsChangedArgs(2, 3, 0);
			expected.AddRange(new[] { m_sections[0].ContentOA, m_sections[1].ContentOA, m_sections[1].HeadingOA });
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (HeadingOA)");
			// Unfortunately, I don't think we actually get PropChanged on the direct owning property,
			// if the owning object (Text or ScrSection) gets deleted. We need to detect deleted objects
			// if things are deleted from any of the possible owning properties.
			// This is also a chance to verify that being owned by an ScrDraft does not count as valid.
			// It's not a very realistic test, as we aren't trying to make everything about the test data consistent.
			((MockStText) m_sections[0].ContentOA).m_mockOwnerOfClass = new MockScrDraft(); // not allowed in list.
			testObj.PropChanged(m_sections[0].Hvo, ScrBookTags.kflidSections, 0, 0, 1);
			expected.RemoveAt(2);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (ScrBook.SectionsOS)");
			VerifyTextsChangedArgs(2, 0, 1);

			((MockStText)expected[3]).IsValidObject = false;
			expected.RemoveAt(3);
			testObj.PropChanged(m_sections[0].Hvo, ScriptureTags.kflidScriptureBooks, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (Scripture.ScriptureBooks)");
			VerifyTextsChangedArgs(3, 0, 1);

			((MockStText)expected[2]).IsValidObject = false;
			expected.RemoveAt(2);
			testObj.PropChanged(m_sections[0].Hvo, ScrBookTags.kflidTitle, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (ScrBookTags.Title)");
			VerifyTextsChangedArgs(2, 0, 1);
			Assert.AreEqual(0, testObj.ScriptureTexts.Count(), "by now we've removed all ScriptureTexts");

			((MockStText)expected[1]).IsValidObject = false;
			expected.RemoveAt(1);
			//testObj.PropChanged(1, LangProjectTags.kflidTexts, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted texts are removed (LangProject.Texts)");
			VerifyTextsChangedArgs(1, 0, 1);
		}

		private InterestingTextList SetupTwoMockTextsAndOneScriptureSection(bool fIncludeScripture, out List<IStText> expectedScripture,
			out List<IStText> expected)
		{
			MockTextRepository mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			MakeMockScriptureSection();
			m_propertyTable.SetProperty(InterestingTextList.PersistPropertyName, InterestingTextList.MakeIdList(
				new ICmObject[] { m_sections[0].ContentOA, m_sections[0].HeadingOA }), true, true);
			var testObj = new InterestingTextList(m_mediator, m_propertyTable, mockTextRep, m_mockStTextRepo, fIncludeScripture);
			testObj.InterestingTextsChanged += TextsChangedHandler;
			expectedScripture = new List<IStText>();
			expectedScripture.Add(m_sections[0].ContentOA);
			expectedScripture.Add(m_sections[0].HeadingOA);
			VerifyScriptureList(testObj, expectedScripture, "Initially two Scripture texts");

			expected = new List<IStText>(CurrentTexts(mockTextRep));
			if (fIncludeScripture)
				expected.AddRange(expectedScripture);
			VerifyList(expected, testObj.InterestingTexts, "two ordinary and two Scripture texts");
			return testObj;
		}

		/// <summary>
		/// Should just omit any GUIDs that don't correspond to valid objects.
		/// </summary>
		[Test]
		public void PropertyTableHasInvalidObjects()
		{
			MockTextRepository mockTextRep = MakeMockTextRepoWithTwoMockTexts();
			MakeMockScriptureSection();
			m_propertyTable.SetProperty(InterestingTextList.PersistPropertyName, InterestingTextList.MakeIdList(
				new ICmObject[] { m_sections[0].ContentOA, m_sections[0].HeadingOA }) + "," + Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + ",$%^#@+", true, true);
			var testObj = new InterestingTextList(m_mediator, m_propertyTable, mockTextRep, m_mockStTextRepo, true);
			testObj.InterestingTextsChanged += TextsChangedHandler;
			var expectedScripture = new List<IStText>();
			expectedScripture.Add(m_sections[0].ContentOA);
			expectedScripture.Add(m_sections[0].HeadingOA);
			VerifyList(expectedScripture, testObj.ScriptureTexts, "Just two valid Guids");
		}

		/// <summary>
		/// The same method sets things up here and in another test, the only difference being
		/// that here Scripture is not to be included.
		/// </summary>
		[Test]
		[Ignore("Temporary until we figure out propchanged for unowned Texts.")]
		public void ShouldIncludeScripture()
		{
			List<IStText> expectedScripture;
			List<IStText> expected;
			var testObj = SetupTwoMockTextsAndOneScriptureSection(false, out expectedScripture, out expected);
			Assert.IsFalse(testObj.IsInterestingText(expectedScripture[1]), "in this mode no Scripture is interesting");

			// Invalidating a Scripture book should NOT generate PropChanged etc. when Scripture is not included.
			((MockStText)m_sections[0].ContentOA).IsValidObject = false;
			expectedScripture.Remove(m_sections[0].ContentOA);
			m_sections[0].ContentOA = null; // not normally valid, but makes things somewhat more consistent for test.
			m_lastTextsChangedArgs = null;
			testObj.PropChanged(m_sections[0].Hvo, ScrSectionTags.kflidContent, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted Scripture texts are removed (ContentsOA)");
			VerifyScriptureList(testObj, expectedScripture, "deleted Scripture texts are removed from ScriptureTexts (ContentsOA");
			Assert.IsNull(m_lastTextsChangedArgs, "should NOT get change notification deleting Scripture when not included");

			((MockStText)expected[1]).IsValidObject = false;
			expected.RemoveAt(1);
			//testObj.PropChanged(1, LangProjectTags.kflidTexts, 0, 0, 1);
			VerifyList(expected, testObj.InterestingTexts, "deleted texts are removed (LangProject.Texts)");
			VerifyTextsChangedArgs(1, 0, 1); // but, we still get PropChanged when deleting non-Scripture texts.
		}
		private void VerifyScriptureList(InterestingTextList testObj, List<IStText> expectedScripture, string comment)
		{
			VerifyList(expectedScripture, testObj.ScriptureTexts, comment);
			Assert.AreEqual(InterestingTextList.MakeIdList(expectedScripture.Cast<ICmObject>()),
				m_propertyTable.GetValue<string>(InterestingTextList.PersistPropertyName));
		}

		private List<MockScrSection> m_sections = new List<MockScrSection>();

		private void MakeMockScriptureSection()
		{
			var section = new MockScrSection();
			m_sections.Add(section);
			section.ContentOA = new MockStText();
			section.HeadingOA = new MockStText();
			m_mockStTextRepo.m_texts.Add(section.ContentOA);
			m_mockStTextRepo.m_texts.Add(section.HeadingOA);
		}

		private void VerifyTextsChangedArgs(int insertAt, int inserted, int deleted)
		{
			Assert.AreEqual(insertAt, m_lastTextsChangedArgs.InsertedAt);
			Assert.AreEqual(inserted, m_lastTextsChangedArgs.NumberInserted);
			Assert.AreEqual(deleted, m_lastTextsChangedArgs.NumberDeleted);
		}

		private InterestingTextsChangedArgs m_lastTextsChangedArgs;

		private void TextsChangedHandler(object sender, InterestingTextsChangedArgs e)
		{
			m_lastTextsChangedArgs = e;
		}

		private List<IStText> CurrentTexts(MockTextRepository mockTextRep)
		{
			return (from text in mockTextRep.m_texts select text.ContentsOA).ToList();
		}

		private void RemoveText(MockTextRepository mockTextRep, InterestingTextList testObj, int index)
		{
			var oldTextHvo = mockTextRep.m_texts[index].Hvo;
			((MockText)mockTextRep.m_texts[index]).IsValidObject = false;
			((MockStText) mockTextRep.m_texts[index].ContentsOA).IsValidObject = false;
			mockTextRep.m_texts.RemoveAt(index);
			testObj.PropChanged(oldTextHvo, TextTags.kflidContents, 0, 0, 1);
		}

		private MockText AddMockText(MockTextRepository mockTextRep, InterestingTextList testObj)
		{
			var newText = new MockText();
			mockTextRep.m_texts.Add(newText);
			testObj.PropChanged(newText.Hvo, TextTags.kflidContents, 0, 1, 0);
			return newText;
		}

		static int s_nextHvo = 1;

		static public int NextHvo() {
			return s_nextHvo++; }

		// Verify the two lists have the same members (not necessarily in the same order)
		private void VerifyList(List<IStText> expected, IEnumerable<IStText> actual, string comment)
		{
			Assert.AreEqual(expected.Count, actual.Count(), comment + " count");
			var expectedSet = new HashSet<IStText>(expected);
			var actualSet = new HashSet<IStText>(actual);
			var unexpected = actualSet.Except(expectedSet);
			Assert.AreEqual(0, unexpected.Count(), comment + " has extra elements");
			var missing = expectedSet.Except(actualSet);
			Assert.AreEqual(0, missing.Count(), comment + " has missing elements");
		}

		private MockTextRepository MakeMockTextRepoWithTwoMockTexts()
		{
			var mockTextRep = new MockTextRepository();
			var mockText1 = new MockText();
			var mockText2 = new MockText();
			mockTextRep.m_texts.Add(mockText1);
			mockTextRep.m_texts.Add(mockText2);
			return mockTextRep;
		}
	}

	// REVIEW: The following looks like it should be using Rhino mocks, so it doesn't get broken every time the interfaces change
	internal class MockCmObject : ICmObject
	{
		internal MockCmObject()
		{
			Hvo = InterestingTextsTests.NextHvo();
			IsValidObject = true;
			Guid = Guid.NewGuid();

		}

		public IEnumerable<ICmObject> AllOwnedObjects { get; private set; }
		public int Hvo { get; private set;}

		public ICmObject Owner { get; set;}

		public int OwningFlid
		{
			get { throw new NotImplementedException(); }
		}

		public int OwnOrd
		{
			get { throw new NotImplementedException(); }
		}

		public int ClassID
		{
			get { throw new NotImplementedException(); }
		}

		public Guid Guid { get; private set;}

		public ICmObjectId Id
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObject GetObject(ICmObjectRepository repo)
		{
			throw new NotImplementedException();
		}

		public string ToXmlString()
		{
			throw new NotImplementedException();
		}

		public string ClassName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Delete the recipient object.
		/// </summary>
		public void Delete()
		{
			throw new NotImplementedException();
		}

		public IFdoServiceLocator Services
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObject m_mockOwnerOfClass;
		public ICmObject OwnerOfClass(int clsid)
		{
			return m_mockOwnerOfClass;
		}

		public T OwnerOfClass<T>() where T : ICmObject
		{
			throw new NotImplementedException();
		}

		public ICmObject Self
		{
			get { throw new NotImplementedException(); }
		}

		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		public void PostClone(Dictionary<int, ICmObject> copyMap)
		{
			throw new NotImplementedException();
		}

		public void AllReferencedObjects(List<ICmObject> collector)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			throw new NotImplementedException();
		}

		public bool IsOwnedBy(ICmObject possibleOwner)
		{
			throw new NotImplementedException();
		}

		public ICmObject ReferenceTargetOwner(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRequired(int flid)
		{
			throw new NotImplementedException();
		}

		public int IndexInOwner
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsValidObject { get; set; }

		public FdoCache Cache
		{
			get { throw new NotImplementedException(); }
		}

		public void MergeObject(ICmObject objSrc)
		{
			throw new NotImplementedException();
		}

		public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			throw new NotImplementedException();
		}

		public bool CanDelete
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString ObjectIdName
		{
			get { throw new NotImplementedException(); }
		}

		public string ShortName
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString ShortNameTSS
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString DeletionTextTSS
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString ChooserNameTS
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKey
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKeyWs
		{
			get { throw new NotImplementedException(); }
		}

		public int SortKey2
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKey2Alpha
		{
			get { throw new NotImplementedException(); }
		}

		public HashSet<ICmObject> ReferringObjects
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<ICmObject> OwnedObjects { get; private set; }
	}

	internal class MockScrSection : MockCmObject, IScrSection
	{
		private IStText m_heading;
		public IStText HeadingOA
		{
			get { return m_heading; }
			set
			{
				m_heading = value;
				if (m_heading != null)
					((MockCmObject)m_heading).Owner = this;
			}
		}

		private IStText m_content;
		public IStText ContentOA
		{
			get { return m_content; }
			set
			{
				m_content = value;
				if (m_content != null)
					((MockCmObject) m_content).Owner = this;
			}
		}

		public int VerseRefStart
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int VerseRefEnd
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int VerseRefMin
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int VerseRefMax
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public bool ContainsChapter(int chapter)
		{
			throw new NotImplementedException();
		}

		public bool IsIntro
		{
			get { throw new NotImplementedException(); }
		}

		public int ContentParagraphCount
		{
			get { throw new NotImplementedException(); }
		}

		public int HeadingParagraphCount
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<IScrTxtPara> Paragraphs
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsFirstScriptureSection
		{
			get { throw new NotImplementedException(); }
		}

		public IScrSection PreviousSection
		{
			get { throw new NotImplementedException(); }
		}

		public IScrSection NextSection
		{
			get { throw new NotImplementedException(); }
		}

		public IStTxtPara FirstContentParagraph
		{
			get { throw new NotImplementedException(); }
		}

		public IStTxtPara LastContentParagraph
		{
			get { throw new NotImplementedException(); }
		}

		public IStTxtPara FirstHeadingParagraph
		{
			get { throw new NotImplementedException(); }
		}

		public IStTxtPara LastHeadingParagraph
		{
			get { throw new NotImplementedException(); }
		}

		public ContextValues Context
		{
			get { throw new NotImplementedException(); }
		}

		public bool StartsWithVerseOrChapterNumber
		{
			get { throw new NotImplementedException(); }
		}

		public bool StartsWithChapterNumber
		{
			get { throw new NotImplementedException(); }
		}

		public bool ContainsReference(ScrReference reference)
		{
			throw new NotImplementedException();
		}

		public void MoveHeadingParasToContent(int indexFirstPara, IStStyle newStyle)
		{
			throw new NotImplementedException();
		}

		public void MoveContentParasToHeading(int indexLastPara)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionHeading_atIP(int iParaSplit, int ichSplit)
		{
			throw new NotImplementedException();
		}

		public void SplitSectionHeading_ExistingParaBecomesContent(int iParaStart, int iParaEnd)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionContent_atIP(int iParaSplit, ITsString headingText, string headingParaStyle)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit, IStText newHeading)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionContent_ExistingParaBecomesHeading(int iPara, int cParagraphs)
		{
			throw new NotImplementedException();
		}

		public void DeleteParagraph(IScrTxtPara para)
		{
			throw new NotImplementedException();
		}

		public void GetDisplayRefs(out BCVRef startRef, out BCVRef endRef)
		{
			throw new NotImplementedException();
		}

		public List<IScrFootnote> GetFootnotes()
		{
			throw new NotImplementedException();
		}

		public IScrFootnote FindFirstFootnote(out int iPara, out int ich, out int tag)
		{
			throw new NotImplementedException();
		}

		public IScrFootnote FindLastFootnote(out int iPara, out int ich, out int tag)
		{
			throw new NotImplementedException();
		}

		public void MoveContentParasToHeading(int indexLastPara, IStStyle newStyle)
		{
			throw new NotImplementedException();
		}

		public IScrSection SplitSectionContent_ExistingParaBecomesHeading(int iPara, int cParagraphs, IStStyle newStyle)
		{
			throw new NotImplementedException();
		}

		public void SplitSectionHeading_ExistingParaBecomesContent(int iParaStart, int iParaEnd, IStStyle newStyle)
		{
			throw new NotImplementedException();
		}

		public void SetCloneProperties(ICmObject clone)
		{
			throw new NotImplementedException();
		}
	}

	internal class MockStTextRepository : IStTextRepository
	{
		public IEnumerable<ICmObject> AllInstances(int classId)
		{
			throw new NotImplementedException();
		}

		public List<IStText> m_texts = new List<IStText>();

		public IStText GetObject(ICmObjectId id)
		{
			throw new NotImplementedException();
		}

		public IStText GetObject(Guid id)
		{
			foreach (var st in m_texts)
				if (st.Guid == id)
					return st;
			Assert.Fail("Looking for invalid Guid " + id);
			return null; // make compiler happy.
		}

		public bool TryGetObject(Guid id, out IStText obj)
		{
			foreach (var st in m_texts)
			{
				if (st.Guid == id)
				{
					obj = st;
					return true;
				}
			}
			obj = null;
			return false;
		}

		public IStText GetObject(int hvo)
		{
			foreach (var st in m_texts)
				if (st.Hvo == hvo)
					return st;
			Assert.Fail("Looking for invalid HVO " + hvo);
			return null; // make compiler happy.
		}

		public bool TryGetObject(int hvo, out IStText obj)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IStText> AllInstances()
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { throw new NotImplementedException(); }
		}

		public IList<IStText> GetObjects(IList<int> hvos)
		{
			throw new NotImplementedException();
		}
	}


	internal class MockTextRepository : ITextRepository
	{

		public List<IText> m_texts = new List<IText>();

		public IEnumerable<ICmObject> AllInstances(int classId)
		{
			throw new NotImplementedException();
		}

		public IText GetObject(ICmObjectId id)
		{
			throw new NotImplementedException();
		}

		public IText GetObject(Guid id)
		{
			throw new NotImplementedException();
		}

		public bool TryGetObject(Guid guid, out IText obj)
		{
			throw new NotImplementedException();
		}

		public IText GetObject(int hvo)
		{
			foreach (var st in m_texts)
				if (st.Hvo == hvo)
					return st;
			Assert.Fail("Looking for invalid HVO " + hvo);
			return null; // make compiler happy.
		}

		public bool TryGetObject(int hvo, out IText obj)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IText> AllInstances()
		{
			return m_texts.ToArray();
		}

		public int Count
		{
			get { throw new NotImplementedException(); }
		}
	}

	internal class MockText : MockCmObject, IText
	{
		public MockText()
		{
			ContentsOA = new MockStText();
		}

		public IRnGenericRec AssociatedNotebookRecord
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the MediaFiles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmMediaContainer MediaFilesOA
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public void AssociateWithNotebook(bool makeYourOwnUow)
		{
			throw new NotImplementedException();
		}

		public IMultiString Source
		{
			get { throw new NotImplementedException(); }
		}

		public string SoundFilePath
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}


		private IStText m_contents;
		public IStText ContentsOA
		{
			get { return m_contents; }
			set
			{
				if (m_contents != null)
					((MockStText)m_contents).IsValidObject = false;
				m_contents = value;
				if (m_contents != null)
					((MockCmObject)m_contents).Owner = this;
			}
		}
		public IFdoReferenceCollection<ICmPossibility> GenresRC
		{
			get { throw new NotImplementedException(); }
		}

		public IMultiUnicode Abbreviation
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsTranslated
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}


		public IMultiUnicode Name
		{
			get { throw new NotImplementedException(); }
		}

		public DateTime DateCreated
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public DateTime DateModified
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IMultiString Description
		{
			get { throw new NotImplementedException(); }
		}

		public IFdoOwningCollection<IPublication> PublicationsOC
		{
			get { throw new NotImplementedException(); }
		}

		public IFdoOwningCollection<IPubHFSet> HeaderFooterSetsOC
		{
			get { throw new NotImplementedException(); }
		}

		public IPubHFSet FindHeaderFooterSetByName(string name)
		{
			throw new NotImplementedException();
		}
	}

	internal class MockStText : MockCmObject, IStText
	{
		public MockStText()
		{
		}

		public IFdoOwningSequence<IStPara> ParagraphsOS
		{
			get { throw new NotImplementedException(); }
		}

		public bool RightToLeft
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IFdoOwningCollection<ITextTag> TagsOC
		{
			get { throw new NotImplementedException(); }
		}

		public DateTime DateModified
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IStTxtPara this[int i]
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsEmpty
		{
			get { throw new NotImplementedException(); }
		}

		public IScrFootnote FindFirstFootnote(out int iPara, out int ich)
		{
			throw new NotImplementedException();
		}

		public IScrFootnote FindLastFootnote(out int iPara, out int ich)
		{
			throw new NotImplementedException();
		}

		public IScrFootnote FindNextFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition)
		{
			throw new NotImplementedException();
		}

		public IScrFootnote FindPreviousFootnote(ref int iPara, ref int ich, bool fSkipCurrentPosition)
		{
			throw new NotImplementedException();
		}

		public IStTxtPara AddNewTextPara(string paraStyleName)
		{
			throw new NotImplementedException();
		}

		public IStTxtPara InsertNewTextPara(int iPos, string paraStyleName)
		{
			throw new NotImplementedException();
		}

		public IStTxtPara InsertNewPara(int paragraphIndex, string paraStyleName, ITsString tss)
		{
			throw new NotImplementedException();
		}

		public IMultiAccessorBase Title
		{
			get { throw new NotImplementedException(); }
		}

		public IMultiAccessorBase Source
		{
			get { throw new NotImplementedException(); }
		}

		public IMultiAccessorBase Comment
		{
			get { throw new NotImplementedException(); }
		}

		public List<ICmPossibility> GenreCategories
		{
			get { throw new NotImplementedException(); }
		}

		public int MainWritingSystem
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsTranslation
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public HashSet<IWfiWordform> UniqueWordforms()
		{
			throw new NotImplementedException();
		}

		public void DeleteParagraph(IStTxtPara para)
		{
			throw new NotImplementedException();
		}
	}

	internal class MockScrDraft : MockCmObject, IScrDraft
	{
		public IScrBook FindBook(int bookOrd)
		{
			throw new NotImplementedException();
		}

		public string Description
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IFdoOwningSequence<IScrBook> BooksOS
		{
			get { throw new NotImplementedException(); }
		}

		public DateTime DateCreated
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public ScrDraftType Type
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public bool Protected
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IScrBook AddBookCopy(IScrBook book)
		{
			throw new NotImplementedException();
		}

		public void AddBookMove(IScrBook book)
		{
			throw new NotImplementedException();
		}
	}
}
