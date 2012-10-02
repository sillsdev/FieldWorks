// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MergeObjectsTests.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements Tests that excercise the MergeObject methods in FDO.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Summary description for MergeObjectsTests.
	/// </summary>
	[TestFixture]
	public class MergeObjectsTests : ScrInMemoryFdoTestBase
	{
		#region Member variables

		private ILexDb m_ldb;
		private IFdoOwningCollection<ILexEntry> m_entriesCol;

		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_ldb = Cache.LangProject.LexDbOA;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for ShowMainEntryIn list.
		/// </summary>
		/// <remarks>ShowMainEntryIn Slice</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeShowMainEntryIn()
		{
			// Use LexEntry.ShowMainEntryIn
			ILexEntry lme1 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lme2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Main Dictionary is there by default
			Assert.AreEqual(1, lme1.ShowMainEntryIn.Count);
			Assert.AreEqual("Main Dictionary", lme1.ShowMainEntryIn.ToArray()[0].ToString());
			lme1.ShowMainEntryIn.Clear();
			lme2.ShowMainEntryIn.Clear();

			// Merge nul into null.
			Assert.AreEqual(0, lme1.ShowMainEntryIn.Count); // lme2 created same way should also be 0
			lme1.MergeObject(lme2); // deletes lme2
			Assert.AreEqual(0, lme1.ShowMainEntryIn.Count);

			// Merge content into null.
			var publication1 = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "Pub 1");
			lme1.ShowMainEntryIn.Add(publication1);
			var publication2 = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "Pub 2");
			lme1.ShowMainEntryIn.Add(publication2);

			lme2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme2.ShowMainEntryIn.Clear();
			lme1.MergeObject(lme2); // deletes lme2
			Assert.AreEqual(2, lme1.ShowMainEntryIn.Count);

			// Merge duplicate content into content.
			lme2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme2.ShowMainEntryIn.Clear();
			lme2.ShowMainEntryIn.Add(publication2);
			lme1.MergeObject(lme2); // deletes lme2
			Assert.AreEqual(2, lme1.ShowMainEntryIn.Count);

			// Merge content into content.
			var publication3 = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "Pub 3");
			lme2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme2.ShowMainEntryIn.Clear();
			lme2.ShowMainEntryIn.Add(publication3);
			lme1.MergeObject(lme2); // deletes lme2
			Assert.AreEqual(3, lme1.ShowMainEntryIn.Count);

			// Merge null into content.
			ILexEntry lme3 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			Assert.AreEqual(4, lme3.ShowMainEntryIn.Count);
			lme3.ShowMainEntryIn.Clear();
			Assert.AreEqual(0, lme3.ShowMainEntryIn.Count);
			lme3.MergeObject(lme1); // deletes lme1
			Assert.AreEqual(3, lme3.ShowMainEntryIn.Count);
		}

		private ITsString AnalysisTss(string form)
		{
			return Cache.TsStrFactory.MakeString(form, Cache.DefaultAnalWs);
		}

		private ICmPossibility MakePossibility(ICmPossibilityList list, string name)
		{
			ICmPossibility result = null;
			//UndoableUnitOfWorkHelper.Do("do", "undo", m_actionHandler,
				//() =>
				{
					result = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					list.PossibilitiesOS.Add(result);
					result.Name.AnalysisDefaultWritingSystem = AnalysisTss(name);
				} //);
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for integer values.
		/// Currently disabled, because homgraph numbers are handled specially, and all other
		/// classes with interger properties are currently not enabled for merging. Most of them
		/// would need some special handling for the integer properties if we DID make a way to
		/// merge them. So this is YAGNI. Keeping the test because it does document what at
		/// one point we thought should be the default way to merge integers.
		/// </summary>
		/// <remarks>CellarPropertyType.Integer</remarks>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[Ignore("Merging homograph numbers doesn't work, nwo that homograph renumbering works better.")]
		public void MergeIntegers()
		{
			// Use LexEntry.HomographNumber.
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			lmeKeeper.HomographNumber = 0;
			lmeSrc.HomographNumber = 1;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 1;
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.HomographNumber = 2;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 1;
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.HomographNumber = 0;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 0;
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.HomographNumber = 0;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(0, lmeKeeper.HomographNumber);
		}

		/// <summary>
		/// Test that merging entries produces the expected homograph sequence.
		/// </summary>
		[Test]
		public void MergeEntryAdjustsHomographsAndMakesAllomorphs()
		{
			var homo1 = MakeEntry("kick");
			var homo2 = MakeEntry("kick");
			var homo3 = MakeEntry("kick");
			var homo4 = MakeEntry("kick");
			Assert.That(homo1.HomographNumber, Is.EqualTo(1));
			Assert.That(homo2.HomographNumber, Is.EqualTo(2));
			Assert.That(homo3.HomographNumber, Is.EqualTo(3));
			Assert.That(homo4.HomographNumber, Is.EqualTo(4));
			homo3.MergeObject(homo2);
			Assert.That(homo2.IsValidObject, Is.False);
			Assert.That(homo1.HomographNumber, Is.EqualTo(1));
			Assert.That(homo3.HomographNumber, Is.EqualTo(2));
			Assert.That(homo4.HomographNumber, Is.EqualTo(3));
			Assert.That(homo3.AlternateFormsOS, Is.Empty, "should not make an allomorph for homographs");

			var other = MakeEntry("punt");
			var kickLf = homo3.LexemeFormOA;
			other.MergeObject(homo3);
			Assert.That(homo3.IsValidObject, Is.False);
			Assert.That(homo1.HomographNumber, Is.EqualTo(1));
			Assert.That(homo4.HomographNumber, Is.EqualTo(2));

			// Should have made 'kick' an allomorph of punt
			Assert.That(other.AlternateFormsOS, Has.Count.EqualTo(1));
			Assert.That(other.AlternateFormsOS[0], Is.EqualTo(kickLf));
		}

		private ILexEntry MakeEntry(string form)
		{
			var result = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var lf = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			result.LexemeFormOA = lf;
			lf.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for DateTime values.
		/// </summary>
		/// <remarks>CellarPropertyType.Time</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeTimes()
		{
			// Use LexEntry.DateModified.
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// This one gets set to 'now' in the MergeObject method,
			// so we don't know what it will end up being, except newer than lmeSrc.DateModified.
			lmeKeeper.DateModified = new DateTime(2005, 3, 31, 15, 50, 0);
			lmeSrc.DateModified = new DateTime(2005, 3, 31, 15, 59, 59);
			DateTime srcTime = lmeSrc.DateModified;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.DateModified > srcTime, "Wrong modified time for DateModified (T-#1).");

			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.DateModified = new DateTime(2005, 3, 31, 15, 50, 0);
			DateTime mod = lmeSrc.DateModified;
			lmeKeeper.DateModified = new DateTime(2005, 3, 31, 15, 59, 59);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.DateModified > mod, "Wrong modified time for DateModified (T-#2).");

			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			DateTime dt = new DateTime(2005, 3, 31, 15, 59, 59);
			lmeSrc.DateCreated = DateTime.Now;
			lmeKeeper.DateCreated = new DateTime(2005, 3, 31, 15, 59, 59);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.DateCreated.Equals(dt), "Wrong created time for DateModified (T-#3).");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for Guid values.
		/// </summary>
		/// <remarks>CellarPropertyType.Guid</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeGuids()
		{
			// Use LangProject.Filters.
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			ICmFilter keeper = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(keeper);


			// Ensure empty (new) guid does not get changed.
			Assert.IsTrue(keeper.App == Guid.Empty);
			src.App = Guid.NewGuid();
			Guid oldSrcGuid = src.App;
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == oldSrcGuid);

			// Should not change extant guid in either of the next two checks.
			Guid newGuid = Guid.NewGuid();
			keeper.App = newGuid;
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.App = Guid.NewGuid();
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == newGuid);

			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.App = Guid.Empty;
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == newGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method (with fLoseNoData true) for Owning atomic and
		/// MultiUnicode values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeAppendAtomic()
		{
			int engWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph amKeeper = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lmeKeeper.LexemeFormOA = amKeeper;
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph amSrc = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lmeSrc.LexemeFormOA = amSrc;

			string oldForm = "old form";
			string newForm = "new form";
			amKeeper.Form.set_String(engWs, oldForm);
			amSrc.Form.set_String(engWs, newForm);

			lmeKeeper.MergeObject(lmeSrc, true);
			Assert.AreEqual(oldForm + ' ' + newForm, amKeeper.Form.get_String(engWs).Text);

			// Nothing should happen if the child objects are of different types.
			IMoAffixAllomorph maa = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.LexemeFormOA = maa;
			maa.Form.set_String(engWs, newForm);
			amKeeper.Form.set_String(engWs, oldForm);
			lmeKeeper.MergeObject(lmeSrc, true);
			Assert.AreEqual(oldForm, amKeeper.Form.get_String(engWs).Text);
		}

		private void AssertEqualTss(ITsString tss1, ITsString tss2)
		{
			Assert.IsTrue(tss1.Equals(tss2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for String and BigString values.
		/// </summary>
		/// <remarks>CellarPropertyType.String and CellarPropertyType.BigString.
		/// (JohnT) We should use real TsStrings here, with non-uniform values,
		/// to check all information is preserved.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeStrings()
		{
			int engWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");

			string virginiaCreeper = "Vitaceae Parthenocissus quinquefolia";
			string whiteOak = "Quercus alba";
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense lsKeeper = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeKeeper.SensesOS.Add(lsKeeper);
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, engWs);
			ITsTextProps ttpEng = propsBldr.GetTextProps();

			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.SetProperties(0, 0, ttpEng);
			ITsString tssEmpty = tsb.GetString();
			tsb.Replace(0, 0, virginiaCreeper, ttpEng);
			tsb.SetIntPropValues(10, 20, (int) FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 50);
			ITsString tssVirginiaCreeper = tsb.GetString();

			ITsStrBldr tsb2 = TsStrBldrClass.Create();
			tsb2.Replace(0, 0, whiteOak, ttpEng);
			tsb2.SetIntPropValues(5, 10, (int) FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 500);
			ITsString tssWhiteOak = tsb2.GetString();

			tsb2.Replace(tsb2.Length, tsb2.Length, " ", null);
			tsb2.ReplaceTsString(tsb2.Length, tsb2.Length, tssVirginiaCreeper);
			ITsString tssConcat = tsb2.GetString();

			lsSrc.ScientificName = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			AssertEqualTss(lsKeeper.ScientificName, tssVirginiaCreeper);

			lsKeeper.ScientificName = tssWhiteOak;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName, tssWhiteOak);

			lsKeeper.ScientificName = tssWhiteOak;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssEmpty;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName, tssWhiteOak);

			lsKeeper.ScientificName = tssEmpty;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName, tssVirginiaCreeper);

			lsKeeper.ScientificName = tssVirginiaCreeper;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssEmpty;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName, tssVirginiaCreeper);

			// Now test the append case.
			lsKeeper.ScientificName = tssWhiteOak;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc, true);
			AssertEqualTss(lsKeeper.ScientificName, tssConcat);

			// But don't append if equal
			lsKeeper.ScientificName = tssWhiteOak;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);
			lsSrc.ScientificName = tssWhiteOak;
			lsKeeper.MergeObject(lsSrc, true);
			Assert.AreEqual(lsKeeper.ScientificName, tssWhiteOak);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for MultiString and MultiBigString values.
		/// </summary>
		/// <remarks>CellarPropertyType.MultiString and CellarPropertyType.MultiBigString.
		/// JohnT: should test with non-uniform property strings.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeMultiStrings()
		{
			// 14 Use LexEntry.Bibliography
			string eng = "English Bib. Info";
			string es = "Inf. Bib. Esp.";
			int engWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			int esWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es");
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, engWs);
			ITsTextProps ttpEng = propsBldr.GetTextProps();

			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.Replace(0, 0, eng, ttpEng);
			tsb.SetIntPropValues(7, 10, (int) FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 50);
			ITsString tssEng = tsb.GetString();

			string append = "Append";
			ITsStrBldr tsb2 = TsStrBldrClass.Create();
			tsb2.Replace(0, 0, append, ttpEng);
			tsb2.SetIntPropValues(2, 4, (int) FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 500);
			ITsString tssAppend = tsb2.GetString();

			// reuse the original tsb to get the concatenation with the append string.
			tsb.Replace(tsb.Length, tsb.Length, " ", null);
			tsb.ReplaceTsString(tsb.Length, tsb.Length, tssAppend);
			ITsString tssConcat = tsb.GetString();

			ITsStrBldr tsb3 = TsStrBldrClass.Create();
			tsb3.Replace(0, 0, es, ttpEng);
			tsb3.SetIntPropValues(5, 8, (int) FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 500);
			ITsString tssEs = tsb3.GetString();

			// Merge content into null alternatives
			lmeSrc.Bibliography.set_String(engWs, tssEng);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.get_String(engWs), tssEng);

			// Try to merge empty string into null content.
			lmeKeeper.Bibliography.set_String(engWs, string.Empty);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.Bibliography.set_String(engWs, string.Empty);
			Assert.IsNull(lmeKeeper.Bibliography.get_String(engWs).Text);

			// Merge content into empty string.
			lmeKeeper.Bibliography.set_String(engWs, string.Empty); // This actually sets the content to null, not an empty string.
			lmeSrc.Bibliography.set_String(engWs, tssEng);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.get_String(engWs), tssEng);

			// Try to merge into existing content.
			lmeKeeper.Bibliography.set_String(engWs, tssEng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.Bibliography.set_String(engWs, "Should fail to merge, blah.");
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.get_String(engWs), tssEng);

			// Make sure extant content isn't wrecked.
			lmeKeeper.Bibliography.set_String(engWs, tssEng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.Bibliography.set_String(esWs, tssEs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.get_String(engWs), tssEng);
			Assert.AreEqual(lmeKeeper.Bibliography.get_String(esWs), tssEs);

			// Now tests involving concatenation.
			lmeKeeper.Bibliography.set_String(engWs, tssEng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.Bibliography.set_String(engWs, tssAppend);
			lmeKeeper.MergeObject(lmeSrc, true);
			AssertEqualTss(lmeKeeper.Bibliography.get_String(engWs), tssConcat);

			// Don't concatenate if initial values are equal.
			lmeKeeper.Bibliography.set_String(engWs, tssEng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.Bibliography.set_String(engWs, tssEng);
			lmeKeeper.MergeObject(lmeSrc, true);
			AssertEqualTss(lmeKeeper.Bibliography.get_String(engWs), tssEng);
		}

		/// <summary>
		/// LexSense has a special override to insert semi-colons.
		/// </summary>
		[Test]
		public void MergeSenses()
		{
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense lsKeeper = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeKeeper.SensesOS.Add(lsKeeper);
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lmeSrc.SensesOS.Add(lsSrc);

			lsKeeper.Definition.SetAnalysisDefaultWritingSystem("English defn keep");
			lsKeeper.Gloss.SetAnalysisDefaultWritingSystem("English gloss keep");
			lsSrc.Definition.SetAnalysisDefaultWritingSystem("English defn src");
			lsSrc.Gloss.SetAnalysisDefaultWritingSystem("English gloss src");

			lsKeeper.MergeObject(lsSrc, true);
			Assert.AreEqual("English defn keep; English defn src", lsKeeper.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("English gloss keep; English gloss src", lsKeeper.Gloss.AnalysisDefaultWritingSystem.Text);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for Unicode and BigUnicode values.
		/// </summary>
		/// <remarks>CellarPropertyType.Unicode and CellarPropertyType.BigUnicode</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeUnicode()
		{
			// 15 CmFilter.Name
			string goodFilter = "Fram";
			string junkFilter = "Brand X";
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter keeper = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(keeper);
			ICmFilter src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);

			// Merge content into null original.
			src.Name = goodFilter;
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Try to merge empty string into null.
			keeper.Name = null;
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.Name = "";
			keeper.MergeObject(src);
			Assert.IsNull(keeper.Name);

			// Try to merge empty string into content.
			keeper.Name = goodFilter;
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.Name = "";
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Try to merge content into content.
			keeper.Name = goodFilter;
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.Name = junkFilter;
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Test merge append
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.Name = junkFilter;
			keeper.MergeObject(src, true);
			Assert.AreEqual(keeper.Name, goodFilter +  ' ' + junkFilter);

			// But don't append if equal.
			keeper.Name = goodFilter;
			src = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(src);
			src.Name = goodFilter;
			keeper.MergeObject(src, true);
			Assert.AreEqual(keeper.Name, goodFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for MultiUnicode and MultiBigUnicode values.
		/// </summary>
		/// <remarks>CellarPropertyType.MultiUnicode and CellarPropertyType.MultiBigUnicode</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeMultiUnicode()
		{
			// 16 LexEntry.CitationForm
			// 20 Not used in database.
			string eng = "dog";
			string es = "perro";
			int engWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			int esWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es");
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Merge content into null alternatives
			lmeSrc.CitationForm.set_String(engWs, eng);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.get_String(engWs).Text, eng);

			// Try to merge empty string into null content.
			((ITsMultiString)lmeKeeper.CitationForm).set_String(engWs, null);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.CitationForm.set_String(engWs, string.Empty);
			Assert.IsNull(lmeKeeper.CitationForm.get_String(engWs).Text);

			// Merge content into empty string.
			lmeKeeper.CitationForm.set_String(engWs, string.Empty); // This actually sets the content to null, not an empty string.
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.CitationForm.set_String(engWs, eng);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.get_String(engWs).Text, eng);

			// Try to merge into existing content.
			lmeKeeper.CitationForm.set_String(engWs, eng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.CitationForm.set_String(engWs, "cat");
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.get_String(engWs).Text, eng);

			// Make sure extant content isn't wrecked.
			lmeKeeper.CitationForm.set_String(engWs, eng);
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.CitationForm.set_String(esWs, es);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.get_String(engWs).Text, eng);
			Assert.AreEqual(lmeKeeper.CitationForm.get_String(esWs).Text, es);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>CellarPropertyType.OwningAtomic</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningAtomic()
		{
			// 23 LexEntry.Pronunciation:LexPronunciation
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Merge content into null target.
			lmeSrc.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lmeKeeper.MergeObject(lmeSrc);
			//Assert.IsNull(lmeSrc.LexemeFormOA);

			// Try to merge content into content.
			lmeKeeper.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			IMoForm keeper = lmeKeeper.LexemeFormOA;
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			//IMoForm src = lmeSrc.LexemeFormOA;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(keeper, lmeKeeper.LexemeFormOA);
			//Assert.AreEqual(hvoSrc, lmeSrc.LexemeFormOAHvo);

			// Try to merge null into content.
			lmeKeeper.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			keeper = lmeKeeper.LexemeFormOA;
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.LexemeFormOA = null;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(keeper, lmeKeeper.LexemeFormOA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference atomic values.
		/// </summary>
		/// <remarks>CellarPropertyType.ReferenceAtomic</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceAtomic()
		{
			// 24 LexSense.MorphoSyntaxAnalysis (refers to MSA)
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			ILexSense lsKeeper = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsKeeper);
			ILexSense lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			IMoStemMsa msa1 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lme.MorphoSyntaxAnalysesOC.Add(msa1);
			IMoStemMsa msa2 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lme.MorphoSyntaxAnalysesOC.Add(msa2);

			// Merge content into null.
			lsSrc.MorphoSyntaxAnalysisRA = msa1;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1, lsKeeper.MorphoSyntaxAnalysisRA);

			// Merge content into content.
			lsKeeper.MorphoSyntaxAnalysisRA = msa1;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			lsSrc.MorphoSyntaxAnalysisRA = msa2;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1, lsKeeper.MorphoSyntaxAnalysisRA);

			// Merge null into content.
			lsKeeper.MorphoSyntaxAnalysisRA = msa1;
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			lsSrc.MorphoSyntaxAnalysisRA = null;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1, lsKeeper.MorphoSyntaxAnalysisRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>CellarPropertyType.OwningCollection</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningCollection()
		{
			// 25 LexEntry.MorphoSyntaxAnalyses
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IFdoOwningSequence<ICmPossibility> posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			posSeq.Add(Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create());
			posSeq.Add(Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create());

			// Merge content into null.
			IMoStemMsa msaSrc = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lmeSrc.MorphoSyntaxAnalysesOC.Add(msaSrc);
			msaSrc.PartOfSpeechRA = (IPartOfSpeech)posSeq[0];
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual(msaSrc, lmeKeeper.MorphoSyntaxAnalysesOC.ToArray()[0]);

			// Merge content into content.
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemMsa msaSrc2 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lmeSrc.MorphoSyntaxAnalysesOC.Add(msaSrc2);
			msaSrc2.PartOfSpeechRA = (IPartOfSpeech)posSeq[1];
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>CellarPropertyType.ReferenceCollection</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceCollection()
		{
			// 26 LexSense.UsageTypes(refers to CmPossibility)
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexSense lsKeeper = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsKeeper);
			ILexSense lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			IFdoOwningSequence<ICmPossibility> usageSeq = m_ldb.UsageTypesOA.PossibilitiesOS;
			usageSeq.Add(Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create());
			usageSeq.Add(Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create());
			usageSeq.Add(Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create());

			// Merge content into null.
			lsSrc.UsageTypesRC.Add(usageSeq[0]);
			lsSrc.UsageTypesRC.Add(usageSeq[1]);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(2, lsKeeper.UsageTypesRC.Count);

			// Merge content into content.
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			lsSrc.UsageTypesRC.Add(usageSeq[2]);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);

			// Merge duplicate content into content.
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			lsSrc.UsageTypesRC.Add(usageSeq[2]);
			Assert.AreEqual(1, lsSrc.UsageTypesRC.Count);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);

			// Merge null into content.
			lsSrc = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(lsSrc);
			Assert.AreEqual(0, lsSrc.UsageTypesRC.Count);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owning sequence values.
		/// </summary>
		/// <remarks>CellarPropertyType.OwningSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningSequence()
		{
			// 27 LexEntry.Senses
			ILexEntry lmeKeeper = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			ILexEntry lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Merge content into null.
			lmeSrc.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.SensesOS.Count);

			// Merge content into content.
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeSrc.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.SensesOS.Count);

			// Merge null into content.
			lmeSrc = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.SensesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>CellarPropertyType.ReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceSequence()
		{
			// 28 MoAffixAllomorph.Position(refers to PhEnvironment)
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			IMoAffixAllomorph aKeeper = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(aKeeper);
			IMoAffixAllomorph aSrc = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(aSrc);
			IFdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			envsSeq.Add(Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create());
			envsSeq.Add(Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create());
			envsSeq.Add(Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create());

			// Merge content into null.
			aSrc.PositionRS.Add(envsSeq[0]);
			aSrc.PositionRS.Add(envsSeq[1]);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(2, aKeeper.PositionRS.Count);

			// Merge duplicate content into content.
			aSrc = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(aSrc);
			aSrc.PositionRS.Add(envsSeq[0]);
			aSrc.PositionRS.Add(envsSeq[1]);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(4, aKeeper.PositionRS.Count);

			// Merge content into content.
			aSrc = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(aSrc);
			aSrc.PositionRS.Add(envsSeq[0]);
			aSrc.PositionRS.Add(envsSeq[1]);
			aSrc.PositionRS.Add(envsSeq[2]);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(7, aKeeper.PositionRS.Count);

			// Merge null into content.
			aSrc = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(aSrc);
			Assert.AreEqual(0, aSrc.PositionRS.Count);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(7, aKeeper.PositionRS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for resetting atomic back references.
		/// </summary>
		/// <remarks>This tests reference properties that are atomic.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeAtomicPropertyBackReferences()
		{
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemMsa msaKeeper = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lme.MorphoSyntaxAnalysesOC.Add(msaKeeper);
			IMoStemMsa msaSrc = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lme.MorphoSyntaxAnalysesOC.Add(msaSrc);
			ILexSense ls1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(ls1);
			ls1.MorphoSyntaxAnalysisRA = msaKeeper;
			ILexSense ls2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(ls2);
			ls2.MorphoSyntaxAnalysisRA = msaSrc;

			// Shift atomic back reference.
			msaKeeper.MergeObject(msaSrc);
			Assert.AreEqual(ls2.MorphoSyntaxAnalysisRA, msaKeeper);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>CellarPropertyType.ReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSequencePropertyBackReferences()
		{
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IFdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			IPhEnvironment envKeeper = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			envsSeq.Add(envKeeper);
			IPhEnvironment envSrc = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			envsSeq.Add(envSrc);

			// Shift back references from reference sequence property.
			IMoAffixAllomorph referrer = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(referrer);
			referrer.PositionRS.Add(envSrc);
			envKeeper.MergeObject(envSrc);
			Assert.AreEqual(envKeeper.Hvo, referrer.PositionRS[0].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>CellarPropertyType.ReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeCollectionPropertyBackReferences()
		{
			ILexEntry lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IFdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			IPhEnvironment envKeeper = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			envsSeq.Add(envKeeper);
			IPhEnvironment envSrc = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			envsSeq.Add(envSrc);

			// Shift back references from reference sequence property.
			IMoAffixAllomorph referrer = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(referrer);
			referrer.PhoneEnvRC.Add(envSrc);
			envKeeper.MergeObject(envSrc);
			Assert.AreEqual(envKeeper, referrer.PhoneEnvRC.ToArray()[0]);
		}

		/// <summary>
		/// Tests the special case of merging an entry that is a component of another entry. (FWR-3535)
		/// </summary>
		[Test]
		public void MergeComponentEntry()
		{
			var leBaba = MakeEntry("baba");
			var leUbaba = MakeEntry("ubaba");
			var leU = MakeEntry("u");
			var lePa = MakeEntry("pa");
			var leref = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			leUbaba.EntryRefsOS.Add(leref);
			leref.ComponentLexemesRS.Add(leBaba);
			leref.ComponentLexemesRS.Add(leU);
			leref.PrimaryLexemesRS.Add(leBaba);
			lePa.MergeObject(leBaba);
			Assert.That(leref.ComponentLexemesRS[0], Is.EqualTo(lePa));
			Assert.That(leref.ComponentLexemesRS[1], Is.EqualTo(leU));
			Assert.That(leref.PrimaryLexemesRS[0], Is.EqualTo(lePa));
			Assert.That(leref.ComponentLexemesRS.Count, Is.EqualTo(2));
			Assert.That(leref.PrimaryLexemesRS.Count, Is.EqualTo(1));
		}
	}
}
