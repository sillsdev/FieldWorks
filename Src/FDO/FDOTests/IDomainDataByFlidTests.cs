// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2002' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ISilDataAccessTests.cs
// Responsibility: Randy Regnier
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application.Impl;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.CoreTests.DomainDataByFlidTest
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FdoCache ISilDataAccess implementation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DomainDataByFlidTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ISilDataAccessManaged m_sda;
		private ICmPossibilityListFactory m_possListFactory;
		private int m_customCertifiedFlid;
		private int m_customITsStringFlid;
		private int m_customVernTsStringFlid;
		private int m_customMultiUnicodeFlid;
		private int m_customAtomicReferenceFlid;
		//private int m_customReferenceSequenceFlid;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			var servLoc = Cache.ServiceLocator;
			var mdc = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			m_customCertifiedFlid = mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
			m_customITsStringFlid = mdc.AddCustomField("WfiWordform", "NewTsStringProp", CellarPropertyType.String, 0);
			m_customVernTsStringFlid = mdc.AddCustomField("WfiWordform", "NewVernStringProp", CellarPropertyType.String, 0, "helpId",
				WritingSystemServices.kwsVern, Guid.Empty);
			m_customMultiUnicodeFlid = mdc.AddCustomField("WfiWordform", "MultiUnicodeProp", CellarPropertyType.MultiUnicode, 0);
			m_customAtomicReferenceFlid = mdc.AddCustomField("WfiWordform", "NewAtomicRef", CellarPropertyType.ReferenceAtomic, CmPersonTags.kClassId);
			//m_customReferenceSequenceFlid = mdc.AddCustomField("WfiWordform", "NewRefSeq", CellarPropertyType.ReferenceSequence, CmPersonTags.kClassId);

			m_sda = servLoc.GetInstance<ISilDataAccessManaged>();
			m_possListFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			var lp = Cache.LanguageProject;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				lp.PeopleOA = m_possListFactory.Create();
				var personFactory = servLoc.GetInstance<ICmPersonFactory>();
				var person1 = personFactory.Create();
				lp.PeopleOA.PossibilitiesOS.Add(person1);
				person1.DateOfBirth = new GenDate(GenDate.PrecisionType.Approximate, 1, 1, 3000, true);
				var person2 = personFactory.Create();
				lp.PeopleOA.PossibilitiesOS.Add(person2);

				lp.LocationsOA = m_possListFactory.Create();
				var location = servLoc.GetInstance<ICmLocationFactory>().Create();
				lp.LocationsOA.PossibilitiesOS.Add(location);
				lp.LocationsOA.IsSorted = true;

				person1.PlaceOfBirthRA = location;

				lp.EthnologueCode = "ZPI";

				lp.TranslatedScriptureOA = servLoc.GetInstance<IScriptureFactory>().Create();
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			m_sda = null;
			m_possListFactory = null;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Make sure the MDC is not null.
		/// </summary>
		[Test]
		public void MetaDataCacheGetterTest()
		{
			Assert.IsNotNull(m_sda.MetaDataCache, "Null MDC.");
		}

		/// <summary>
		/// Make sure the methnod has not been implemented.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void get_IsDummyIdTest()
		{
			m_sda.get_IsDummyId(-1);
		}

		/// <summary>
		/// Make sure the ActionHandler is the FdoCache.
		/// </summary>
		[Test]
		public void GetActionHandlerTest()
		{
			var ah = m_sda.GetActionHandler();
			Assert.IsNotNull(ah, "Action handler is null");
			Assert.IsTrue(ah.GetType().Name == "UndoStack");
		}

		/// <summary>
		/// Make sure the ActionHandler cannot be (re)set.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void SetActionHandlerTest()
		{
			m_sda.SetActionHandler(null);
		}

		/// <summary>
		/// See if object is valid.
		/// </summary>
		[Test]
		public void get_IsValidObjectTest()
		{
			Assert.IsTrue(m_sda.get_IsValidObject(Cache.LanguageProject.Hvo));
			Assert.IsFalse(m_sda.get_IsValidObject(-1));
		}

		/// <summary>
		/// See if object is valid with a zero Hvo.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void get_IsValidObjectZeroHvoTest()
		{
			m_sda.get_IsValidObject(0);
		}

		/// <summary>
		/// Get an atomic reference property, including owner.
		/// </summary>
		[Test]
		public void get_ObjectPropTest()
		{
			// Test owning property values.
			Assert.AreEqual(
				Cache.LanguageProject.PeopleOA.Owner.Hvo,
				m_sda.get_ObjectProp(Cache.LanguageProject.PeopleOA.Hvo, (int)CmObjectFields.kflidCmObject_Owner));
			Assert.AreEqual(
				FdoCache.kNullHvo,
				m_sda.get_ObjectProp(Cache.LanguageProject.Hvo, (int)CmObjectFields.kflidCmObject_Owner));

			// Test regular atomic reference properties.
			var person = Cache.LanguageProject.PeopleOA.PossibilitiesOS[1];
			Assert.AreEqual(
				FdoCache.kNullHvo,
				m_sda.get_ObjectProp(person.Hvo, CmPersonTags.kflidPlaceOfBirth));

			person = Cache.LanguageProject.PeopleOA.PossibilitiesOS[0];
			var location = Cache.LanguageProject.LocationsOA.PossibilitiesOS[0];
			Assert.AreEqual(
				location.Hvo,
				m_sda.get_ObjectProp(person.Hvo, CmPersonTags.kflidPlaceOfBirth));
		}

		/// <summary>
		/// Test the InsertNew function.
		/// </summary>
		[Test]
		public void InsertNew()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = stText.AddNewTextPara(null);
			// We should be able to insert one with no style and no stylesheet.
			m_sda.InsertNew(stText.Hvo, StTextTags.kflidParagraphs, 0, 1, null);
			Assert.AreEqual(2, stText.ParagraphsOS.Count);
			Assert.AreEqual(para, stText.ParagraphsOS[0]);

			// Even without a stylesheet, any styles should be copied.
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "style1");
			ITsTextProps props = bldr.GetTextProps();
			IStTxtPara para1 = stText.ParagraphsOS[1] as IStTxtPara;
			para1.StyleRules = props;
			m_sda.InsertNew(stText.Hvo, StTextTags.kflidParagraphs, 1, 1, null);
			Assert.AreEqual(3, stText.ParagraphsOS.Count);
			Assert.AreEqual(para, stText.ParagraphsOS[0]);
			Assert.AreEqual(para1, stText.ParagraphsOS[1]);
			IStTxtPara para2 = stText.ParagraphsOS[2] as IStTxtPara;
			Assert.AreEqual(props, para2.StyleRules);

			// With a stylesheet, we get a smart next-style behavior.
			IVwStylesheet ss = new MockStyleSheet();
			m_sda.InsertNew(stText.Hvo, StTextTags.kflidParagraphs, 1, 1, ss);
			Assert.AreEqual(4, stText.ParagraphsOS.Count);
			Assert.AreEqual(para, stText.ParagraphsOS[0]);
			Assert.AreEqual(para1, stText.ParagraphsOS[1]);
			Assert.AreEqual(para2, stText.ParagraphsOS[3]); // new one is inserted before it.
			IStTxtPara para3 = stText.ParagraphsOS[2] as IStTxtPara;
			Assert.IsNotNull(para3.StyleRules);
			string stylename = para3.StyleRules.GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("NextStyle", stylename);

			// We should also be able to copy one with no style, even passing a stylesheet
			m_sda.InsertNew(stText.Hvo, StTextTags.kflidParagraphs, 0, 1, ss);
			Assert.AreEqual(5, stText.ParagraphsOS.Count);
			Assert.AreEqual(para, stText.ParagraphsOS[0]);
			Assert.AreEqual(para1, stText.ParagraphsOS[2]); // moved up one
			Assert.IsNull(stText.ParagraphsOS[1].StyleRules);
		}

		/// <summary>
		/// Test the MoveString function, obviously.
		/// </summary>
		[Test]
		public void MoveString()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para1 = stText.AddNewTextPara(null);
			var para2 = stText.AddNewTextPara(null);

			// Move a complete string into an empty one.
			string firstPara = "First para";
			para1.Contents = MakeAnalysisString(firstPara);
			m_sda.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0, 0, firstPara.Length, para2.Hvo,
				StTxtParaTags.kflidContents, 0, 0, false);
			Assert.AreEqual(firstPara, para2.Contents.Text);
			Assert.AreEqual(0, para1.Contents.Length);

			// Enhance JohnT: we could reinstate something like this when we implement move string more fully.
			// Currently we can only move from the end of one string to the end of another, which is enough for
			// inserting and deleting newlines and Paste.
			//// Move half of it back.
			//m_sda.MoveString(para2.Hvo, StTxtParaTags.kflidContents, 0, 0, 5, para1.Hvo, StTxtParaTags.kflidContents, 0, 0);
			//Assert.AreEqual("First", para1.Contents.Text);
			//Assert.AreEqual(" para", para2.Contents.Text);

			// Move a piece from the end to the empty string.
			m_sda.MoveString(para2.Hvo, StTxtParaTags.kflidContents, 0, 5, 10, para1.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			Assert.AreEqual(" para", para1.Contents.Text);
			Assert.AreEqual("First", para2.Contents.Text);

			// Move a piece from the end to the end of a non-empty (the final "ra").
			m_sda.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0, 3, 5, para2.Hvo, StTxtParaTags.kflidContents, 0, 5, false);
			Assert.AreEqual(" pa", para1.Contents.Text);
			Assert.AreEqual("Firstra", para2.Contents.Text);

			// Enhance JohnT: we could reinstate something like this when we implement move string more fully.
			// Currently we can only move from the end of one string to the end of another, which is enough for
			// inserting and deleting newlines and Paste.
			// Move middle to middle.
			//m_sda.MoveString(para2.Hvo, StTxtParaTags.kflidContents, 0, 1, 2, para1.Hvo, StTxtParaTags.kflidContents, 0, 5);
			//Assert.AreEqual("Firstpra", para1.Contents.Text);
			//Assert.AreEqual(" a", para2.Contents.Text);
		}

		ITsString MakeAnalysisString(string input)
		{
			return Cache.TsStrFactory.MakeString(input, Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
		}

		/// <summary>
		/// Set an atomic reference property (i.e., owner).
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void set_ObjectProp_Owning_Test()
		{
			var lexEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_sda.SetObjProp(lexEntry.Hvo, (int)CmObjectFields.kflidCmObject_Owner, FdoCache.kNullHvo);
		}

		/// <summary>
		/// Set an atomic reference property.
		/// </summary>
		[Test]
		public void set_ObjectProp_Ref_Test()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			var anal = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(anal);

			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);

			// Set the anal's Category property.
			m_sda.SetObjProp(anal.Hvo, WfiAnalysisTags.kflidCategory, pos.Hvo);
			Assert.IsNotNull(anal.CategoryRA, "Category is null.");
			Assert.AreEqual(pos.Hvo, anal.CategoryRA.Hvo, "Wrong ref Hvo.");

			// Set it to null.
			m_sda.SetObjProp(anal.Hvo, WfiAnalysisTags.kflidCategory, FdoCache.kNullHvo);
			Assert.IsNull(anal.CategoryRA, "Category is not null.");
		}

		/// <summary>
		/// Get a boolean property value test.
		/// </summary>
		[Test]
		public void get_BooleanPropTest()
		{
			Assert.AreEqual(
				Cache.LanguageProject.LocationsOA.IsSorted,
				m_sda.get_BooleanProp(Cache.LanguageProject.LocationsOA.Hvo, CmPossibilityListTags.kflidIsSorted));
		}

		/// <summary>
		/// Test the get accessor for boolean properties, which the client thinks is an int.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void GetIntAsBooleanBadTests()
		{
			m_sda.get_BooleanProp(Cache.LanguageProject.Hvo, (int)CmObjectFields.kflidCmObject_Class);
		}

		/// <summary>
		/// Test the get accessor for boolean properties, which the client thinks is an int.
		/// </summary>
		[Test]
		public void GetIntAsBooleanGoodTests()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();

			wf.SpellingStatus = -1;
			var spellStatus = IntBoolPropertyConverter.GetBoolean(m_sda, wf.Hvo, WfiWordformTags.kflidSpellingStatus);
			Assert.IsTrue(spellStatus, "Wrong spelling status.");
			wf.SpellingStatus = 0;
			spellStatus = IntBoolPropertyConverter.GetBoolean(m_sda, wf.Hvo, WfiWordformTags.kflidSpellingStatus);
			Assert.IsFalse(spellStatus, "Wrong spelling status.");
			wf.SpellingStatus = 1;
			spellStatus = IntBoolPropertyConverter.GetBoolean(m_sda, wf.Hvo, WfiWordformTags.kflidSpellingStatus);
			Assert.IsTrue(spellStatus, "Wrong spelling status.");
		}

		/// <summary>
		/// Test the set accessor for boolean properties, which the client thinks is an int.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void SetBooleanAsIntBadTests()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			m_sda.SetBoolean(wf.Hvo, WfiWordformTags.kflidSpellingStatus, true);
		}

		/// <summary>
		/// Test the set accessor for boolean properties, which the client thinks is an int.
		/// </summary>
		[Test]
		public void SetBooleanAsIntGoodTests()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			IntBoolPropertyConverter.SetValueFromBoolean(m_sda, wf.Hvo, WfiWordformTags.kflidSpellingStatus, true);
			Assert.AreEqual(wf.SpellingStatus, 1, "Wrong spelling status.");
			IntBoolPropertyConverter.SetValueFromBoolean(m_sda, wf.Hvo, WfiWordformTags.kflidSpellingStatus, false);
			Assert.AreEqual(wf.SpellingStatus, 0, "Wrong spelling status.");
		}

		/// <summary>
		/// Set a boolean property value test.
		/// </summary>
		[Test]
		public void set_BooleanPropTest()
		{
			var locList = Cache.LanguageProject.LocationsOA;
			var originalValue = locList.IsSorted;

			// Set to toggled value.
			m_sda.SetBoolean(locList.Hvo, CmPossibilityListTags.kflidIsSorted, !originalValue);
			Assert.IsTrue(locList.IsSorted == !originalValue, "Wrong boolean value.");

			// Switch back to original value.
			m_sda.SetBoolean(locList.Hvo, CmPossibilityListTags.kflidIsSorted, originalValue);
			Assert.IsTrue(locList.IsSorted == originalValue, "Wrong boolean value.");
		}

		/// <summary>
		/// Set an interface to the wrong kind of object.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void SetUnknownBadTest()
		{
			// Don't worry about the hvo or tag,
			// since it should blow up wel, before discovering the LP has no such tag.
			m_sda.SetUnknown(Cache.LanguageProject.Hvo, 1, Guid.NewGuid());
		}

		/// <summary>
		/// Get and set "Unknown".
		/// </summary>
		[Test]
		public void UnknownTests()
		{
			var style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LanguageProject.StylesOC.Add(style);
			var userWs = Cache.WritingSystemFactory.UserWs;
			var bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Arial");
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var tpp = bldr.GetTextProps();
			m_sda.SetUnknown(style.Hvo, StStyleTags.kflidRules, tpp);
			Assert.AreSame(tpp, style.Rules, "Not the same text props via FDO.");

			var tppViaSDA = m_sda.get_UnknownProp(style.Hvo, StStyleTags.kflidRules);
			Assert.AreSame(tpp, tppViaSDA, "Not the same text props via SDA.");
		}

		/// <summary>
		/// Get an HVO from a non-extant Guid test.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void get_ObjFromGuidBadTest()
		{
			m_sda.get_ObjFromGuid(Guid.NewGuid());
		}

		/// <summary>
		/// Get an HVO from an object's Guid test.
		/// </summary>
		[Test]
		public void get_ObjFromGuidTest()
		{
			var lp = Cache.LanguageProject;
			var lpHvo = m_sda.get_ObjFromGuid(lp.Guid);
			Assert.AreEqual(lp.Hvo, lpHvo, "Wrong HVO.");
		}

		/// <summary>
		/// Get a Guid property value test.
		/// </summary>
		[Test]
		public void get_GuidPropTest()
		{
			var poss = m_possListFactory.Create();
			Cache.LanguageProject.TimeOfDayOA = poss;
			poss.ListVersion = Guid.NewGuid();
			var guid = poss.ListVersion;
			var guid2 = m_sda.get_GuidProp(poss.Hvo, CmPossibilityListTags.kflidListVersion);
			Assert.AreEqual(guid, guid2, "Wrong Guid property.");
		}

		/// <summary>
		/// Set a Guid property value test.
		/// </summary>
		[Test]
		public void set_GuidPropTest()
		{
			var poss = m_possListFactory.Create();
			Cache.LanguageProject.TimeOfDayOA = poss;
			var newAppGuid = Guid.NewGuid();
			m_sda.SetGuid(poss.Hvo, CmPossibilityListTags.kflidListVersion, newAppGuid);
			Assert.AreEqual(poss.ListVersion, newAppGuid, "Wrong Guid property.");
		}

		/// <summary>
		/// Get a Class Id from an object test.
		/// </summary>
		[Test]
		public void get_IntPropTest()
		{
			var lp = Cache.LanguageProject;
			var lpClsid = m_sda.get_IntProp(lp.Hvo, (int)CmObjectFields.kflidCmObject_Class);
			Assert.AreEqual(lp.ClassID, lpClsid, "Wrong Class Id.");
		}

		/// <summary>
		/// Set a Class Id from an object test.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void set_Class_IntPropTest()
		{
			m_sda.SetInt(Cache.LanguageProject.Hvo, (int)CmObjectFields.kflidCmObject_Class, 25);
		}

		/// <summary>
		/// Set an OwnFlid for an object test.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void set_OwnFlid_IntPropTest()
		{
			m_sda.SetInt(Cache.LanguageProject.Hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, 25);
		}

		/// <summary>
		/// Set an OwnOrd for an object test.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void set_OwnOrd_IntPropTest()
		{
			m_sda.SetInt(Cache.LanguageProject.Hvo, CmObjectTags.kflidOwnOrd, 25);
		}

		/// <summary>
		/// Set an int property for an object test.
		/// </summary>
		[Test]
		public void set_IntPropTest()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();

			m_sda.SetInt(wf.Hvo, WfiWordformTags.kflidSpellingStatus, 0);
			Assert.AreEqual(0, wf.SpellingStatus, "Wrong spelling status.");
			m_sda.SetInt(wf.Hvo, WfiWordformTags.kflidSpellingStatus, 1);
			Assert.AreEqual(1, wf.SpellingStatus, "Wrong spelling status.");
		}

		/// <summary>
		/// Get a GenDate property value test.
		/// </summary>
		[Test]
		public void get_GenDatePropTest()
		{
			var person1 = Cache.LanguageProject.PeopleOA.PossibilitiesOS[0];
			var genDate = m_sda.get_GenDateProp(person1.Hvo, CmPersonTags.kflidDateOfBirth);
			Assert.AreEqual(new GenDate(GenDate.PrecisionType.Approximate, 1, 1, 3000, true), genDate);
		}

		/// <summary>
		/// Set a GenDate property value test.
		/// </summary>
		[Test]
		public void set_GenDatePropTest()
		{
			var person2 = Cache.LanguageProject.PeopleOA.PossibilitiesOS[1] as ICmPerson;
			var genDate = new GenDate(GenDate.PrecisionType.Exact, 0, 0, 300, false);
			m_sda.SetGenDate(person2.Hvo, CmPersonTags.kflidDateOfBirth, genDate);
			Assert.AreEqual(genDate, person2.DateOfBirth);
		}

		/// <summary>
		/// Get a Unicode string property value test.
		/// </summary>
		[Test]
		public void get_UnicodePropTest()
		{
			var lp = Cache.LanguageProject;
			var ethCode = m_sda.get_UnicodeProp(lp.Hvo, LangProjectTags.kflidEthnologueCode);
			Assert.AreEqual(lp.EthnologueCode, ethCode, "Wrong Ethnologue Code.");
		}

		/// <summary>
		/// Set a Unicode string property value test (using both SDA 'set' methods).
		/// </summary>
		[Test]
		public void set_UnicodePropTest()
		{
			var lp = Cache.LanguageProject;
			var ethCode = lp.EthnologueCode;
			const string newValue = "New Value";
			m_sda.SetUnicode(lp.Hvo, LangProjectTags.kflidEthnologueCode, newValue, newValue.Length);
			Assert.AreEqual(newValue, lp.EthnologueCode, "Wrong Ethnologue code.");

			m_sda.set_UnicodeProp(lp.Hvo, LangProjectTags.kflidEthnologueCode, ethCode);
			Assert.AreEqual(ethCode, lp.EthnologueCode, "Wrong Ethnologue code.");
		}

		/// <summary>
		/// Get a Unicode string property value test.
		/// </summary>
		[Test]
		public void UnicodePropRgchTest()
		{
			var lp = Cache.LanguageProject;
			var ecOriginal = lp.EthnologueCode;
			const int tag = LangProjectTags.kflidEthnologueCode;
			// Set its 'EthnologueCode' property.
			const string ecNew = "ZPI";
			lp.EthnologueCode = ecNew;
			int len;
			m_sda.UnicodePropRgch(lp.Hvo, tag, null, 0, out len);
			Assert.AreEqual(ecNew.Length, len);

			using (var arrayPtr = MarshalEx.StringToNative(len + 1, true))
			{
				int cch;
				m_sda.UnicodePropRgch(lp.Hvo, tag, arrayPtr, len + 1, out cch);
				var ecNew2 = MarshalEx.NativeToString(arrayPtr, cch, true);
				Assert.AreEqual(ecNew, ecNew2);
				Assert.AreEqual(ecNew2.Length, cch);
			}

			// Restore orginal value,
			// in case another test *in this class* expects the original.
			// There is no need to restore it otherwise,
			// since the lp is created freash for every test class.
			lp.EthnologueCode = ecOriginal;
		}

		/// <summary>
		/// Get a long (GenDate) property value test.
		/// </summary>
		[Test]
		public void get_TimePropTest()
		{
			var lp = Cache.LanguageProject;
			var now = DateTime.Now;
			lp.DateCreated = now;
			long dateCreated = m_sda.get_TimeProp(lp.Hvo, CmProjectTags.kflidDateCreated);
			long nowDate = SilTime.ConvertToSilTime(now);
			Assert.AreEqual(nowDate, dateCreated, "Wrong DateCreated.");
		}

		/// <summary>
		/// Set a long (GenDate) property value test.
		/// </summary>
		[Test]
		public void SetTimeTest()
		{
			var lp = Cache.LanguageProject;
			var now = DateTime.Now;
			m_sda.SetTime(lp.Hvo, CmProjectTags.kflidDateModified, SilTime.ConvertToSilTime(now));
			Assert.AreEqual(now.Ticks / 10000, lp.DateModified.Ticks / 10000, "Wrong DateModified.");
		}

		/// <summary>
		/// Get a binary property value test.
		/// </summary>
		[Test]
		public void BinaryPropRgbTest()
		{
			IUserConfigAcct acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
			Cache.LanguageProject.UserAccountsOC.Add(acct);
			acct.Sid = new byte[] { 1, 2, 3, 4, 5 };
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(5))
			{
				int chvo;
				m_sda.BinaryPropRgb(acct.Hvo, UserConfigAcctTags.kflidSid, arrayPtr, 5, out chvo);
				var prgbNew = MarshalEx.NativeToArray<byte>(arrayPtr, chvo);
				Assert.AreEqual(acct.Sid.Length, prgbNew.Length);
				for (var i = 0; i < prgbNew.Length; i++)
					Assert.AreEqual(acct.Sid[i], prgbNew[i]);
			}
		}

		 /// <summary>
		 ///  Get a managed binary property test.
		 /// </summary>
		[Test]
		public void get_BinaryTest()
		{
			IUserConfigAcct acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
			Cache.LanguageProject.UserAccountsOC.Add(acct);
			acct.Sid = new byte[] { 1, 2, 3, 4, 5 };
			byte[] arrayPtr;
			int cbytes = m_sda.get_Binary(acct.Hvo, UserConfigAcctTags.kflidSid, out arrayPtr);
			Assert.AreEqual(acct.Sid.Length, cbytes);
			for (var i = 0; i < arrayPtr.Length; i++)
				Assert.AreEqual(acct.Sid[i], arrayPtr[i]);
		}

		/// <summary>
		/// Set a binary property value test.
		/// </summary>
		[Test]
		public void SetBinaryTest()
		{
			IUserConfigAcct acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
			Cache.LanguageProject.UserAccountsOC.Add(acct);
			var newValue = new byte[] { 1, 2, 3, 4, 5 };
			m_sda.SetBinary(acct.Hvo, UserConfigAcctTags.kflidSid, newValue, newValue.Length);

			Assert.AreEqual(acct.Sid.Length, newValue.Length, "Wrong length for byte array.");
			for (var i = 0; i < newValue.Length; i++)
				Assert.AreEqual(acct.Sid[i], newValue[i], "Wrong byte value at index: " + i);
		}

		/// <summary>
		/// Get a TsString property value test.
		/// </summary>
		[Test]
		public void get_StringPropTest()
		{
			var lp = Cache.LanguageProject;
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Check for null .Text, before setting it.
			var originalvalue = m_sda.get_StringProp(le.Hvo, LexEntryTags.kflidImportResidue);
			Assert.IsNull(originalvalue.Text, "Default for null property should have null for the Text of the returned ITsString.");

			le.ImportResidue = Cache.TsStrFactory.MakeString("Junk", Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);

			var residue = m_sda.get_StringProp(le.Hvo, LexEntryTags.kflidImportResidue);
			Assert.AreEqual(le.ImportResidue, residue, "Wrong import residue.");
		}

		/// <summary>
		/// Set a TsString property value test.
		/// </summary>
		[Test]
		public void SetStringTest()
		{
			var lp = Cache.LanguageProject;
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			Assert.IsNull(le.ImportResidue.Text, "Default for null property should have null for the Text of the returned ITsString.");
			var residue = Cache.TsStrFactory.MakeString("Junk", Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
			m_sda.SetString(le.Hvo, LexEntryTags.kflidImportResidue, residue);
			Assert.AreEqual(le.ImportResidue, residue, "Wrong import residue.");
		}

		/// <summary>
		/// Get various multi-string/unicode values test.
		/// </summary>
		[Test]
		public void get_MultiTest()
		{
			var lp = Cache.LanguageProject;
			var englishWS = Cache.WritingSystemFactory.GetWsFromStr("en");
			var orig = lp.Description.get_String(englishWS);
			// Test for a multistring alt.
			var viaSda = m_sda.get_MultiStringAlt(
				lp.Hvo,
				CmProjectTags.kflidDescription,
				englishWS);
			Assert.AreEqual(orig, viaSda, "Wrong description.");

			//We should do a multiUnicode alt test and test MultiStringProp from here.
		}

		/// <summary>
		/// Set multi-string/unicode values test.
		/// </summary>
		[Test]
		public void set_MultiTest()
		{
			var lp = Cache.LanguageProject;
			var englishWS = Cache.WritingSystemFactory.GetWsFromStr("en");
			var orig = lp.Description.get_String(englishWS);

			// Test for a multistring alt.
			var viaSda = Cache.TsStrFactory.MakeString("Newby", englishWS);
			m_sda.SetMultiStringAlt(
				lp.Hvo,
				CmProjectTags.kflidDescription,
				englishWS,
				viaSda);
			Assert.AreEqual(lp.Description.get_String(englishWS),
				viaSda, "Wrong description.");
			lp.Description.set_String(englishWS, orig);

			//We should do a MultiUnicode alt test here.
		}

		/// <summary>
		/// Get a vector size test, using both methods.
		/// </summary>
		/// <remarks>
		/// This test the 'get_VecSizeAssumeCached' method, which simply calls the
		/// get_VecSize method. This works on all of the four main FDO vector classes,
		/// since get_VecSize accepts all of the four main vector flid types.
		/// </remarks>
		[Test]
		public void get_VecSizeTest()
		{
			var lp = Cache.LanguageProject;
			const int flid = LangProjectTags.kflidStyles;
			var orig = m_sda.get_VecSize(lp.Hvo, flid);
			Assert.AreEqual(Cache.LanguageProject.StylesOC.Count, orig, "Wrong original count.");

			// Add a text.
			var style1 = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			lp.StylesOC.Add(style1);
			var newCount = m_sda.get_VecSize(lp.Hvo, flid);
			Assert.AreEqual(Cache.LanguageProject.StylesOC.Count, newCount, "Wrong new count (get_VecSize).");

			newCount = m_sda.get_VecSizeAssumeCached(lp.Hvo, flid);
			Assert.AreEqual(Cache.LanguageProject.StylesOC.Count, newCount, "Wrong new count (get_VecSizeAssumeCached).");
		}

		/// <summary>
		/// Get an entire vector test.
		/// </summary>
		[Test]
		public void VecPropTest()
		{
			var lp = Cache.LanguageProject;

			// Add a couple texts.
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var style1 = styleFactory.Create();
			lp.StylesOC.Add(style1);
			var style2 = styleFactory.Create();
			lp.StylesOC.Add(style2);

			const int flid = LangProjectTags.kflidStyles;
			var cnt = lp.StylesOC.Count;

			using (var arrayPtr = MarshalEx.ArrayToNative<int>(cnt))
			{
				int chvo;
				m_sda.VecProp(lp.Hvo, flid, cnt, out chvo, arrayPtr);
				Assert.AreEqual(cnt, chvo, "Wrong number of Hvos.");

				var hvos = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				Assert.AreEqual(style1.Hvo, hvos[0], "Wrong Hvo.");
				Assert.AreEqual(style2.Hvo, hvos[1], "Wrong Hvo.");
			}
		}

		/// <summary>
		/// Get a vector item test.
		/// </summary>
		[Test]
		public void get_VecItemTest()
		{
			var lp = Cache.LanguageProject;
			const int flid = LangProjectTags.kflidStyles;
			var orig = m_sda.get_VecSize(lp.Hvo, flid);
			Assert.AreEqual(Cache.LanguageProject.StylesOC.Count, orig, "Wrong original count.");

			// Add a text.
			var txt = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			lp.StylesOC.Add(txt);
			var newCount = m_sda.get_VecSize(lp.Hvo, flid);
			var newItemHvo = m_sda.get_VecItem(lp.Hvo, flid, newCount - 1);
			Assert.AreEqual(txt.Hvo, newItemHvo, "Wrong item Hvo.");
		}

		/// <summary>
		/// Get the index of an item test.
		/// </summary>
		[Test]
		public void GetObjIndexTest()
		{
			var lp = Cache.LanguageProject;
			const int flid = LangProjectTags.kflidStyles;

			// Add a text.
			var style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			lp.StylesOC.Add(style);
			var hvos = new List<int>(lp.StylesOC.ToHvoArray());
			var idx1 = hvos.IndexOf(style.Hvo);
			var idx2 = m_sda.GetObjIndex(lp.Hvo, flid, style.Hvo);
			Assert.AreEqual(idx1, idx2, "Wrong index for new Text.");

			lp.StylesOC.Remove(style);

			Assert.AreEqual(-1, m_sda.GetObjIndex(lp.Hvo, flid, lp.Hvo), "Wrong index for LP.");
		}

#if false

		/// <summary>
		/// See if object is valid.
		/// </summary>
		[Test]
		public void get_IsValidObjectTest()
		{
		}

		/// <summary>
		/// See if object is valid.
		/// </summary>
		[Test]
		public void get_IsValidObjectTest()
		{
		}

		/// <summary>
		/// See if object is valid.
		/// </summary>
		[Test]
		public void get_IsValidObjectTest()
		{
		}

		/// <summary>
		/// See if object is valid.
		/// </summary>
		[Test]
		public void get_IsValidObjectTest()
		{
		}
#endif

		/// <summary>
		/// Make sure an object can be deleted using the DeleteObj method.
		/// </summary>
		[Test]
		public void DeleteObjTest()
		{
			// Test deleting atomic owned property.
			// Add a new WFI to the language project.
			var lexDbFactory = Cache.ServiceLocator.GetInstance<ILexDbFactory>();
			var lexDb = Cache.LanguageProject.LexDbOA;

			m_sda.DeleteObj(lexDb.Hvo);
			Assert.IsNull(Cache.LanguageProject.LexDbOA);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, lexDb.Hvo);

			// TODO: Delete vector owned item.
			// TODO: Delete unowned object.
		}

		/// <summary>
		/// Test the DeleteObjOwner method.
		/// </summary>
		[Test]
		public void DeleteObjOwnerTest()
		{
			var lexDb = Cache.LanguageProject.LexDbOA;
			// Test Owning collection.
			var originalCount = lexDb.ResourcesOC.Count();
			var res = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
			lexDb.ResourcesOC.Add(res);
			Assert.AreEqual(originalCount + 1, lexDb.ResourcesOC.Count());
			var flid = res.OwningFlid;
			m_sda.DeleteObjOwner(lexDb.Hvo, res.Hvo, flid, 1);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, res.Hvo);

			// Test deleting atomic owned item.
			flid = lexDb.OwningFlid;
			m_sda.DeleteObjOwner(Cache.LanguageProject.Hvo, lexDb.Hvo, flid, 2);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, lexDb.Hvo);

			// Test owning sequences.
			var people = Cache.LanguageProject.PeopleOA.PossibilitiesOS;
			originalCount = people.Count;
			var person = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			people.Add(person);
			Assert.AreEqual(originalCount + 1, people.Count);
			flid = person.OwningFlid;
			m_sda.DeleteObjOwner(Cache.LanguageProject.PeopleOA.Hvo, person.Hvo, flid, 1);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, person.Hvo);

			// Test deleting unowned object.
			//LgWritingSystem ws = new LgWritingSystem(Cache);
			//Assert.IsTrue(ws.Hvo > 0);
			//Assert.IsTrue(ws.OwningFlid == 0);
			//Assert.IsNull(ws.Owner);
			//m_sda.DeleteObjOwner(0, ws.Hvo, 0, 0);
			//Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, ws.Hvo);
		}

		/// <summary>
		/// Make sure a non-existant HVO can't be used.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void get_IsPropInCacheBadHvoTest()
		{
			m_sda.get_IsPropInCache(-1, 2, 0, 0);
		}

		/// <summary>
		/// Make sure a bogus flid can't be used.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void get_IsPropInCacheBadTagTest()
		{
			m_sda.get_IsPropInCache(Cache.LanguageProject.Hvo, -2, 0, 0);
		}

		/// <summary>
		/// Make sure a mismatched HVO+Flid can't be used.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void get_IsPropInCacheMismatchedHvoAndTagTest()
		{
			m_sda.get_IsPropInCache(Cache.LanguageProject.Hvo, CmPersonTags.kflidGender, 0, 0);
		}

		/// <summary>
		/// Make sure a good property is in the 'cache'.
		/// </summary>
		[Test]
		public void get_IsPropInCacheTest()
		{
			Assert.IsTrue(m_sda.get_IsPropInCache(Cache.LanguageProject.Hvo, LangProjectTags.kflidStatus, 0, 0), "Property is not in 'cache'.");
		}

		/// <summary>
		/// Make sure a bogus flid blows up.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void IllegalFlidTest1()
		{
			m_sda.get_Prop(Cache.LanguageProject.Hvo, Int32.MinValue);
		}

		/// <summary>
		/// Make sure a bogus flid blows up.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void IllegalFlidTest2()
		{
			m_sda.get_IntProp(Cache.LanguageProject.Hvo, Int32.MinValue);
		}

		/// <summary>
		/// Test the SDA on some virtual property.
		/// </summary>
		[Test]
		public void CheckVirtualPropertiesTest()
		{
			var flid = m_sda.MetaDataCache.GetFieldId("LangProject", "InterlinearTexts", false);
			Assert.AreEqual(0, m_sda.get_VecSize(Cache.LanguageProject.Hvo, flid), "Wrong count.");
		}

		/// <summary>
		/// Test the SDA on some custom property.
		/// </summary>
		[Test]
		public void CheckCustomPropertiesTest()
		{
			var servLoc = Cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
			m_actionHandler.EndUndoTask(); // don't want to undo creating it.

			m_actionHandler.BeginUndoTask("undo", "redo");

			// Set custom boolean property.
			m_sda.SetBoolean(wf.Hvo, m_customCertifiedFlid, true);
			Assert.IsTrue(m_sda.get_BooleanProp(wf.Hvo, m_customCertifiedFlid), "Custom prop is not 'true'.");

			// Set custom ITsString property.
			var tsf = Cache.TsStrFactory;
			var userWs = Cache.WritingSystemFactory.UserWs;
			var newStringValue = tsf.MakeString("New ITsString", userWs);
			var emptyStr = tsf.EmptyString(userWs);
			var emptyVernStr = tsf.EmptyString(Cache.DefaultVernWs);
			m_sda.SetString(wf.Hvo, m_customITsStringFlid, newStringValue);
			Assert.AreSame(newStringValue, m_sda.get_StringProp(wf.Hvo, m_customITsStringFlid), "Wrong TsString in custom property.");

			m_actionHandler.EndUndoTask();

			m_actionHandler.Undo();
			Assert.IsFalse(m_sda.get_BooleanProp(wf.Hvo, m_customCertifiedFlid), "Custom bool prop is not undone.");
			Assert.AreSame(emptyStr, m_sda.get_StringProp(wf.Hvo, m_customITsStringFlid), "Wrong TsString undo.");

			m_actionHandler.Redo();
			Assert.IsTrue(m_sda.get_BooleanProp(wf.Hvo, m_customCertifiedFlid), "Custom bool prop is not redone.");
			Assert.AreSame(newStringValue, m_sda.get_StringProp(wf.Hvo, m_customITsStringFlid), "Wrong TsString redo.");

			m_actionHandler.BeginUndoTask("undo", "redo");

			m_sda.SetString(wf.Hvo, m_customITsStringFlid, null);
			// There really are no null ITsStrings in this scenario, as m_sda.get_StringProp
			// returns tsf.EmptyString(userWs) in cases of the data being null, unless a more specific WS is given for the field.
			Assert.AreSame(emptyStr, m_sda.get_StringProp(wf.Hvo, m_customITsStringFlid), "TsString custom property is not tsf.EmptyString.");
			Assert.AreSame(emptyVernStr, m_sda.get_StringProp(wf.Hvo, m_customVernTsStringFlid), "default value for custom vern string");

			// Set custom MultiUnicode property.
			var newUnicodeTsStringValue = tsf.MakeString("New unicode ITsString", userWs);
			m_sda.SetMultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs, newUnicodeTsStringValue);
			Assert.AreSame(newUnicodeTsStringValue, m_sda.get_MultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs), "MultiUnicode custom property is not newUnicodeTsStringValue.");

			m_actionHandler.EndUndoTask();

			m_actionHandler.Undo();
			Assert.AreSame(emptyStr, m_sda.get_MultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs), "MultiUnicode custom property is not undone.");

			m_actionHandler.Redo();
			Assert.AreSame(newUnicodeTsStringValue, m_sda.get_MultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs), "MultiUnicode custom property is not redone.");

			m_actionHandler.BeginUndoTask("undo", "redo");
			m_sda.SetMultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs, null);
			Assert.AreSame(emptyStr, m_sda.get_MultiStringAlt(wf.Hvo, m_customMultiUnicodeFlid, userWs), "MultiUnicode custom property is not newUnicodeTsStringValue.");

			// Set atomic reference custom property.
			var person = (ICmPerson)Cache.LanguageProject.PeopleOA.PossibilitiesOS[0];
			var crefs = person.ReferringObjects.Count;
			m_sda.SetObjProp(wf.Hvo, m_customAtomicReferenceFlid, person.Hvo);
			Assert.AreEqual(person.Hvo, m_sda.get_ObjectProp(wf.Hvo, m_customAtomicReferenceFlid), "Wrong atomic ref custom value.");
			Assert.AreEqual(crefs + 1, person.ReferringObjects.Count, "Wrong number of incoming references.");

			m_actionHandler.EndUndoTask();

			m_actionHandler.Undo();
			Assert.AreEqual(0, m_sda.get_ObjectProp(wf.Hvo, m_customAtomicReferenceFlid), "Wrong atomic ref undo.");

			m_actionHandler.Redo();
			Assert.AreEqual(person.Hvo, m_sda.get_ObjectProp(wf.Hvo, m_customAtomicReferenceFlid), "Wrong atomic ref redo.");
		}

		/// <summary>
		/// Test the SDA on some custom property using an incorrect data type.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FDOInvalidFieldException))]
		public void WrongDataTypeForCustomPropertyTest()
		{
			var servLoc = Cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();

			m_sda.SetInt(wf.Hvo, m_customCertifiedFlid, 25);
		}

		/// <summary>
		/// Shouldn't be able to make a new abstract object using MakeNewObject.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MakeNewAbstractObjectTest()
		{
			m_sda.MakeNewObject(CmObjectTags.kClassId, Cache.LangProject.Hvo, LangProjectTags.kflidStyles, -1);
		}

		/// <summary>
		/// Test ways of calling MakeNewObject.
		/// </summary>
		[Test]
		public void MakeNewObjectTest()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LangProject;
			// collection
			var hvoStyle = m_sda.MakeNewObject(StStyleTags.kClassId, lp.Hvo, LangProjectTags.kflidStyles, -1);
			var aStyle = servLoc.GetInstance<IStStyleRepository>().GetObject(hvoStyle);
			Assert.AreEqual(aStyle.Owner, lp);

			// atomic
			var aText = servLoc.GetInstance<ITextFactory>().Create();
			var hvoStText = m_sda.MakeNewObject(StTextTags.kClassId, aText.Hvo, TextTags.kflidContents, -2);
			var anStText = servLoc.GetInstance<IStTextRepository>().GetObject(hvoStText);
			Assert.AreEqual(aText.ContentsOA, anStText);
			Assert.AreEqual(anStText.Owner, aText);

			// sequence
			var hvoPara1 = m_sda.MakeNewObject(StTxtParaTags.kClassId, hvoStText, StTextTags.kflidParagraphs, 0);
			var para1 = servLoc.GetInstance<IStTxtParaRepository>().GetObject(hvoPara1);
			var hvoPara2 = m_sda.MakeNewObject(StTxtParaTags.kClassId, hvoStText, StTextTags.kflidParagraphs, 1);
			var para2 = servLoc.GetInstance<IStTxtParaRepository>().GetObject(hvoPara2);
			var hvoPara0 = m_sda.MakeNewObject(StTxtParaTags.kClassId, hvoStText, StTextTags.kflidParagraphs, 0);
			var para0 = servLoc.GetInstance<IStTxtParaRepository>().GetObject(hvoPara0);
			Assert.AreEqual(para0, anStText.ParagraphsOS[0]);
			Assert.AreEqual(para1, anStText.ParagraphsOS[1]);
			Assert.AreEqual(para2, anStText.ParagraphsOS[2]);
			Assert.AreEqual(para2.Owner, anStText);
		}

		/// <summary>
		/// Test calling MakeNewObject when asking for an StTxtPara on an StText.
		/// </summary>
		[Test]
		public void MakeNewObjectTest_StTxtPara()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IStText title;
			IScrBook book = servLoc.GetInstance<IScrBookFactory>().Create(1, out title);

			int hvoPara = m_sda.MakeNewObject(StTxtParaTags.kClassId, title.Hvo, StTextTags.kflidParagraphs, 0);
			IStTxtPara para = servLoc.GetInstance<IStTxtParaRepository>().GetObject(hvoPara);
			Assert.AreEqual(para.Owner, title);
			Assert.AreEqual(para, title.ParagraphsOS[0]);
			Assert.IsTrue(para is IScrTxtPara);
		}

		/// <summary>
		/// Test calling MakeNewObject when asking for an StTxtPara on an StFootnote
		/// </summary>
		[Test]
		public void MakeNewObjectTest_StFootnote()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBook book = servLoc.GetInstance<IScrBookFactory>().Create(1);
			IScrFootnote footnote = servLoc.GetInstance<IScrFootnoteFactory>().Create();
			book.FootnotesOS.Add(footnote);

			int hvoPara = m_sda.MakeNewObject(StTxtParaTags.kClassId, footnote.Hvo, StFootnoteTags.kflidParagraphs, 0);
			IStTxtPara para = servLoc.GetInstance<IStTxtParaRepository>().GetObject(hvoPara);
			Assert.AreEqual(para.Owner, footnote);
			Assert.AreEqual(para.Guid, footnote.ParagraphsOS[0].Guid);
			Assert.IsTrue(para is IScrTxtPara);
		}

		/// <summary>
		/// Test Replace method.
		/// </summary>
		[Test]
		public void ReplaceTest()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le = leFactory.Create();
			var ler = servLoc.GetInstance<ILexEntryRefFactory>().Create();
			le.EntryRefsOS.Add(ler);
			for (var i = 0; i < 5; i++)
			{
				var leNew = leFactory.Create();
				ler.ComponentLexemesRS.Add(leNew);
			}
			var le2 = leFactory.Create();

			var hvos = (from entryRef in ler.ComponentLexemesRS.Skip(1).Take(2)
						select entryRef.Hvo).ToArray();

			// sequence replace
			m_sda.Replace(ler.Hvo, LexEntryRefTags.kflidComponentLexemes, 1, 3, new[] { le2.Hvo }, 1);
			Assert.AreEqual(4, ler.ComponentLexemesRS.Count);
			Assert.AreEqual(le2, ler.ComponentLexemesRS[1]);

			// sequence insert
			m_sda.Replace(ler.Hvo, LexEntryRefTags.kflidComponentLexemes, 4, 4, hvos, hvos.Length);
			Assert.AreEqual(6, ler.ComponentLexemesRS.Count);
			Assert.AreEqual(hvos[0], ler.ComponentLexemesRS[4].Hvo);

			// sequence delete
			m_sda.Replace(ler.Hvo, LexEntryRefTags.kflidComponentLexemes, 0, 1, new int[0], 0);
			Assert.AreEqual(5, ler.ComponentLexemesRS.Count);
			Assert.AreEqual(le2, ler.ComponentLexemesRS[0]);

			var sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(sense);
			var ri = servLoc.GetInstance<IReversalIndexFactory>().Create();
			lp.LexDbOA.ReversalIndexesOC.Add(ri);
			var rieFactory = servLoc.GetInstance<IReversalIndexEntryFactory>();
			for (int i = 0; i < 10; i++)
			{
				var rieNew = rieFactory.Create();
				ri.EntriesOC.Add(rieNew);
				sense.ReversalEntriesRC.Add(rieNew);
			}
			var rie1 = rieFactory.Create();
			ri.EntriesOC.Add(rie1);

			var rie2 = rieFactory.Create();
			ri.EntriesOC.Add(rie2);

			// collection replace
			m_sda.Replace(sense.Hvo, LexSenseTags.kflidReversalEntries, 0, 5, new[] { rie1.Hvo }, 1);
			Assert.AreEqual(6, sense.ReversalEntriesRC.Count);
			Assert.IsTrue(sense.ReversalEntriesRC.Contains(rie1));

			// collection insert
			m_sda.Replace(sense.Hvo, LexSenseTags.kflidReversalEntries, 0, 0, new[] { rie2.Hvo }, 1);
			Assert.AreEqual(7, sense.ReversalEntriesRC.Count);
			Assert.IsTrue(sense.ReversalEntriesRC.Contains(rie2));

			// collection delete
			m_sda.Replace(sense.Hvo, LexSenseTags.kflidReversalEntries, 3, 7, new int[0], 0);
			Assert.AreEqual(3, sense.ReversalEntriesRC.Count);
		}

		/// <summary>
		/// Test MoveOwnSeq method.
		/// </summary>
		[Test]
		public void MoveOwnSeqTest()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();

			var alloFactory = servLoc.GetInstance<IMoStemAllomorphFactory>();
			var allo1 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo1);
			var allo2 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo2);
			var allo3 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo3);

			m_sda.MoveOwnSeq(le1.Hvo, LexEntryTags.kflidAlternateForms, 0, 1, le1.Hvo, LexEntryTags.kflidAlternateForms, 3);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo3, allo1, allo2 }));
			Assert.AreEqual(1, allo1.OwnOrd);
			Assert.AreEqual(2, allo2.OwnOrd);
			Assert.AreEqual(0, allo3.OwnOrd);

			m_sda.MoveOwnSeq(le1.Hvo, LexEntryTags.kflidAlternateForms, 1, 2, le1.Hvo, LexEntryTags.kflidAlternateForms, 0);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			var le2 = leFactory.Create();
			m_sda.MoveOwnSeq(le1.Hvo, LexEntryTags.kflidAlternateForms, 0, 0, le2.Hvo, LexEntryTags.kflidAlternateForms, 0);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);

			m_sda.MoveOwnSeq(le1.Hvo, LexEntryTags.kflidAlternateForms, 0, 1, le2.Hvo, LexEntryTags.kflidAlternateForms, 1);
			Assert.AreEqual(0, le1.AlternateFormsOS.Count);
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);
		}

		/// <summary>
		/// Test MoveOwn method.
		/// </summary>
		[Test]
		public void MoveOwnTest()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();

			var alloFactory = servLoc.GetInstance<IMoStemAllomorphFactory>();
			var allo1 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo1);
			var allo2 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo2);
			var allo3 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo3);

			m_sda.MoveOwn(le1.Hvo, LexEntryTags.kflidAlternateForms, allo1.Hvo, le1.Hvo, LexEntryTags.kflidAlternateForms, 3);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3, allo1 }));
			Assert.AreEqual(2, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);

			m_sda.MoveOwn(le1.Hvo, LexEntryTags.kflidAlternateForms, allo1.Hvo, le1.Hvo, LexEntryTags.kflidAlternateForms, 0);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			m_sda.MoveOwn(le1.Hvo, LexEntryTags.kflidAlternateForms, allo1.Hvo, le1.Hvo, LexEntryTags.kflidLexemeForm, 0);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3 }));
			Assert.AreEqual(allo1, le1.LexemeFormOA);
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);

			m_sda.MoveOwn(le1.Hvo, LexEntryTags.kflidLexemeForm, allo1.Hvo, le1.Hvo, LexEntryTags.kflidAlternateForms, 0);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.IsNull(le1.LexemeFormOA);
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			var msaFactory = servLoc.GetInstance<IMoStemMsaFactory>();
			var msa1 = msaFactory.Create();
			le1.MorphoSyntaxAnalysesOC.Add(msa1);
			var msa2 = msaFactory.Create();
			le1.MorphoSyntaxAnalysesOC.Add(msa2);
			var msa3 = msaFactory.Create();
			le1.MorphoSyntaxAnalysesOC.Add(msa3);

			var le2 = leFactory.Create();

			m_sda.MoveOwn(le1.Hvo, LexEntryTags.kflidMorphoSyntaxAnalyses, msa2.Hvo, le2.Hvo, LexEntryTags.kflidMorphoSyntaxAnalyses, 0);
			Assert.AreEqual(le1.MorphoSyntaxAnalysesOC.Count, le1.MorphoSyntaxAnalysesOC.Intersect(new[] { msa1, msa3 }).Count());
			Assert.AreEqual(le2.MorphoSyntaxAnalysesOC.Count, le2.MorphoSyntaxAnalysesOC.Intersect(new[] { msa2 }).Count());
		}

		/// <summary>
		/// Test GetVectorProperty on a virtual property.
		/// </summary>
		[Test]
		public void GetVectorPropertyVirtual()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();
			var mdc = servLoc.MetaDataCache;
			int flid = mdc.GetFieldId("LexEntry", "MinimalLexReferences", false);
			var sda = servLoc.GetInstance<ISilDataAccessManaged>();
			int[] minRefs = sda.VecProp(le1.Hvo, flid);
			Assert.AreEqual(0, minRefs.Length);
		}

		/// <summary>
		/// Test GetVectorProperty on a custom property.
		/// </summary>
		[Test]
		public void GetVectorPropertyCustom()
		{
			var servLoc = Cache.ServiceLocator;
			var mdc = servLoc.MetaDataCache;
			var customSeqFlid = mdc.AddCustomField("WfiWordform", "DummySeq", CellarPropertyType.ReferenceSequence, LexEntryTags.kClassId);

			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();

			var sda = servLoc.GetInstance<ISilDataAccessManaged>();

			sda.Replace(wf.Hvo, customSeqFlid, 0, 0, new int[] {le1.Hvo}, 1);

			int[] customSeq = sda.VecProp(wf.Hvo, customSeqFlid);
			Assert.AreEqual(1, customSeq.Length);
			Assert.AreEqual(le1.Hvo, customSeq[0]);
		}

	}

	class MockStyleSheet : IVwStylesheet
	{
		#region IVwStylesheet Members

		public int CStyles
		{
			get { throw new NotImplementedException(); }
		}

		public void CacheProps(int cch, string _rgchName, int hvoStyle, ITsTextProps _ttp)
		{
			throw new NotImplementedException();
		}

		public ISilDataAccess DataAccess
		{
			get { throw new NotImplementedException(); }
		}

		public void Delete(int hvoStyle)
		{
			throw new NotImplementedException();
		}

		public string GetBasedOn(string bstrName)
		{
			throw new NotImplementedException();
		}

		public int GetContext(string bstrName)
		{
			throw new NotImplementedException();
		}

		public string GetDefaultBasedOnStyleName()
		{
			throw new NotImplementedException();
		}

		public string GetDefaultStyleForContext(int nContext, bool fCharStyle)
		{
			throw new NotImplementedException();
		}

		public string GetNextStyle(string bstrName)
		{
			return "NextStyle";
		}

		public ITsTextProps GetStyleRgch(int cch, string _rgchName)
		{
			throw new NotImplementedException();
		}

		public int GetType(string bstrName)
		{
			throw new NotImplementedException();
		}

		public bool IsBuiltIn(string bstrName)
		{
			throw new NotImplementedException();
		}

		public bool IsModified(string bstrName)
		{
			throw new NotImplementedException();
		}

		public int MakeNewStyle()
		{
			throw new NotImplementedException();
		}

		public ITsTextProps NormalFontStyle
		{
			get { throw new NotImplementedException(); }
		}

		public void PutStyle(string bstrName, string bstrUsage, int hvoStyle, int hvoBasedOn, int hvoNext, int nType, bool fBuiltIn, bool fModified, ITsTextProps _ttp)
		{
			throw new NotImplementedException();
		}

		public bool get_IsStyleProtected(string bstrName)
		{
			throw new NotImplementedException();
		}

		public int get_NthStyle(int ihvo)
		{
			throw new NotImplementedException();
		}

		public string get_NthStyleName(int ihvo)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}
