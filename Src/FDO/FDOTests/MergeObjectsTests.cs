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
using System.Collections;
using System.Runtime.InteropServices; // needed for Marshal
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Summary description for MergeObjectsTests.
	/// </summary>
	[TestFixture]
	public class MergeObjectsTests : InMemoryFdoTestBase
	{
		#region Member variables

		private ILexDb m_ldb;
		private FdoOwningCollection<ILexEntry> m_entriesCol;

		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();

			m_ldb = Cache.LangProject.LexDbOA;
			m_entriesCol = m_ldb.EntriesOC;
		}
		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_entriesCol = null;
			m_ldb = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for boolean values.
		/// </summary>
		/// <remarks>FieldType.kcptBoolean</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBooleans()
		{
			CheckDisposed();

			// Use LexEntry.ExcludeAsHeadword
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			lmeKeeper.ExcludeAsHeadword = false;
			lmeSrc.ExcludeAsHeadword = false;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsFalse(lmeKeeper.ExcludeAsHeadword);

			lmeKeeper.ExcludeAsHeadword = false;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.ExcludeAsHeadword = true;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.ExcludeAsHeadword);

			lmeKeeper.ExcludeAsHeadword = true;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.ExcludeAsHeadword = true;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.ExcludeAsHeadword);

			lmeKeeper.ExcludeAsHeadword = true;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.ExcludeAsHeadword = false;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.ExcludeAsHeadword);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for integer values.
		/// </summary>
		/// <remarks>FieldType.kcptInteger</remarks>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[Ignore("Merging homograph numbers doesn't work, nwo that homograph renumbering works better.")]
		public void MergeIntegers()
		{
			CheckDisposed();

			// Use LexEntry.HomographNumber.
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			lmeKeeper.HomographNumber = 0;
			lmeSrc.HomographNumber = 1;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 1;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.HomographNumber = 2;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 1;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.HomographNumber = 0;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.HomographNumber);

			lmeKeeper.HomographNumber = 0;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.HomographNumber = 0;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(0, lmeKeeper.HomographNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for DateTime values.
		/// </summary>
		/// <remarks>FieldType.kcptTime</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeTimes()
		{
			CheckDisposed();

			// Use LexEntry.DateModified.
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			// We have to change something else - otherwise the merge won't change any data.
			lmeKeeper.ExcludeAsHeadword = false;
			lmeSrc.ExcludeAsHeadword = true;

			// This one gets set to 'now' in the MergeObject method,
			// so we don't know what it will end up being, except newer than lmeSrc.DateModified.
			lmeKeeper.DateModified = new DateTime(2005, 3, 31, 15, 50, 0);
			lmeSrc.DateModified = new DateTime(2005, 3, 31, 15, 59, 59);
			DateTime srcTime = lmeSrc.DateModified;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.DateModified > srcTime, "Wrong modified time for DateModified (T-#1).");

			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.DateModified = new DateTime(2005, 3, 31, 15, 50, 0);
			DateTime mod = lmeSrc.DateModified;
			lmeKeeper.DateModified = new DateTime(2005, 3, 31, 15, 59, 59);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.IsTrue(lmeKeeper.DateModified > mod, "Wrong modified time for DateModified (T-#2).");

			lmeSrc = m_entriesCol.Add(new LexEntry());
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
		/// <remarks>FieldType.kcptGuid</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeGuids()
		{
			CheckDisposed();

			// Use LangProject.Filters.
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter keeper = filtersCol.Add(new CmFilter());
			ICmFilter src = filtersCol.Add(new CmFilter());

			// Ensure empty (new) guid does not get changed.
			Assert.IsTrue(keeper.App == Guid.Empty);
			src.App = Guid.NewGuid();
			Guid oldSrcGuid = src.App;
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == oldSrcGuid);

			// Should not change extant guid in either of the next two checks.
			Guid newGuid = Guid.NewGuid();
			keeper.App = newGuid;
			src = filtersCol.Add(new CmFilter());
			src.App = Guid.NewGuid();
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == newGuid);

			src = filtersCol.Add(new CmFilter());
			src.App = Guid.Empty;
			keeper.MergeObject(src);
			Assert.IsTrue(keeper.App == newGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for image values.
		/// </summary>
		/// <remarks>FieldType.kcptImage</remarks>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[Ignore("FDO doesn't appear to support this data type.")]
		public void MergeImages()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for GenDate values.
		/// </summary>
		/// <remarks>FieldType.kcptGenDate</remarks>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[Ignore("Setter is not implemented in FDO.")]
		public void MergeGenDates()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for binary values.
		/// </summary>
		/// <remarks>FieldType.kcptBinary</remarks>
		/// ------------------------------------------------------------------------------------
		//[Test]
		[Ignore("TODO.")]
		public void MergeBinary()
		{
			CheckDisposed();

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
			CheckDisposed();

			int engWs = Cache.LanguageEncodings.GetWsFromIcuLocale("en");
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			MoStemAllomorph amKeeper = new MoStemAllomorph();
			lmeKeeper.LexemeFormOA = amKeeper;
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());
			MoStemAllomorph amSrc = new MoStemAllomorph();
			lmeSrc.LexemeFormOA = amSrc;

			string oldForm = "old form";
			string newForm = "new form";
			amKeeper.Form.SetAlternative(oldForm, engWs);
			amSrc.Form.SetAlternative(newForm, engWs);

			lmeKeeper.MergeObject(lmeSrc, true);
			Assert.AreEqual(oldForm + ' ' + newForm, amKeeper.Form.GetAlternative(engWs));

			// Nothing should happen if the child objects are of different types.
			MoAffixAllomorph maa = new MoAffixAllomorph();
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.LexemeFormOA = maa;
			maa.Form.SetAlternative(newForm, engWs);
			amKeeper.Form.SetAlternative(oldForm, engWs);
			lmeKeeper.MergeObject(lmeSrc, true);
			Assert.AreEqual(oldForm, amKeeper.Form.GetAlternative(engWs));
		}

		private void AssertEqualTss(ITsString tss1, ITsString tss2)
		{
			Assert.IsTrue(tss1.Equals(tss2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for String and BigString values.
		/// </summary>
		/// <remarks>FieldType.kcptString and FieldType.kcptBigString.
		/// (JohnT) We should use real TsStrings here, with non-uniform values,
		/// to check all information is preserved.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeStrings()
		{
			CheckDisposed();

			int engWs = Cache.LanguageEncodings.GetWsFromIcuLocale("en");

			string virginiaCreeper = "Vitaceae Parthenocissus quinquefolia";
			string whiteOak = "Quercus alba";
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexSense lsKeeper = lmeKeeper.SensesOS.Append(new LexSense());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());
			ILexSense lsSrc = lmeSrc.SensesOS.Append(new LexSense());

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, engWs);
			ITsTextProps ttpEng = propsBldr.GetTextProps();

			ITsStrBldr tsb = TsStrBldrClass.Create();
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

			lsSrc.ScientificName.UnderlyingTsString = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			AssertEqualTss(lsKeeper.ScientificName.UnderlyingTsString, tssVirginiaCreeper);

			lsKeeper.ScientificName.UnderlyingTsString = tssWhiteOak;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName.UnderlyingTsString, tssWhiteOak);

			lsKeeper.ScientificName.UnderlyingTsString = tssWhiteOak;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssEmpty;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName.UnderlyingTsString, tssWhiteOak);

			lsKeeper.ScientificName.UnderlyingTsString = tssEmpty;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName.UnderlyingTsString, tssVirginiaCreeper);

			lsKeeper.ScientificName.UnderlyingTsString = tssVirginiaCreeper;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssEmpty;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(lsKeeper.ScientificName.UnderlyingTsString, tssVirginiaCreeper);

			// Now test the append case.
			lsKeeper.ScientificName.UnderlyingTsString = tssWhiteOak;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssVirginiaCreeper;
			lsKeeper.MergeObject(lsSrc, true);
			AssertEqualTss(lsKeeper.ScientificName.UnderlyingTsString, tssConcat);

			// But don't append if equal
			lsKeeper.ScientificName.UnderlyingTsString = tssWhiteOak;
			lsSrc = lmeSrc.SensesOS.Append(new LexSense());
			lsSrc.ScientificName.UnderlyingTsString = tssWhiteOak;
			lsKeeper.MergeObject(lsSrc, true);
			Assert.AreEqual(lsKeeper.ScientificName.UnderlyingTsString, tssWhiteOak);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for MultiString and MultiBigString values.
		/// </summary>
		/// <remarks>FieldType.kcptMultiString and FieldType.kcptMultiBigString.
		/// JohnT: should test with non-uniform property strings.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeMultiStrings()
		{
			CheckDisposed();

			// 14 Use LexEntry.Bibliography
			string eng = "English Bib. Info";
			string es = "Inf. Bib. Esp.";
			int engWs = Cache.LanguageEncodings.GetWsFromIcuLocale("en");
			int esWs = Cache.LanguageEncodings.GetWsFromIcuLocale("es");
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, engWs);
			ITsTextProps ttpEng = propsBldr.GetTextProps();

			ITsStrBldr tsb = TsStrBldrClass.Create();
			ITsString tssEmpty = tsb.GetString();
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
			lmeSrc.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssEng);

			// Try to merge empty string into null content.
			lmeKeeper.Bibliography.SetAlternative("", engWs);
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.Bibliography.SetAlternative("", engWs);
			Assert.IsNull(lmeKeeper.Bibliography.GetAlternative(engWs).Text);

			// Merge content into empty string.
			lmeKeeper.Bibliography.SetAlternative("", engWs); // This actually sets the content to null, not an empty string.
			lmeSrc.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssEng);

			// Try to merge into existing content.
			lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.Bibliography.SetAlternative("Should fail to merge, blah.", engWs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssEng);

			// Make sure extant content isn't wrecked.
			lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.Bibliography.GetAlternative(esWs).UnderlyingTsString = tssEs;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssEng);
			Assert.AreEqual(lmeKeeper.Bibliography.GetAlternative(esWs).UnderlyingTsString, tssEs);

			// Now tests involving concatenation.
			lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.Bibliography.SetAlternative(tssAppend, engWs);
			lmeKeeper.MergeObject(lmeSrc, true);
			AssertEqualTss(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssConcat);

			// Don't concatenate if initial values are equal.
			lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString = tssEng;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.Bibliography.SetAlternative(tssEng, engWs);
			lmeKeeper.MergeObject(lmeSrc, true);
			AssertEqualTss(lmeKeeper.Bibliography.GetAlternative(engWs).UnderlyingTsString, tssEng);
		}

		/// <summary>
		/// LexSense has a special override to insert semi-colons.
		/// </summary>
		[Test]
		public void MergeSenses()
		{
			CheckDisposed();

			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexSense lsKeeper = lmeKeeper.SensesOS.Append(new LexSense());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());
			ILexSense lsSrc = lmeSrc.SensesOS.Append(new LexSense());

			lsKeeper.Definition.SetAnalysisDefaultWritingSystem("English defn keep");
			lsKeeper.Gloss.AnalysisDefaultWritingSystem = "English gloss keep";
			lsSrc.Definition.SetAnalysisDefaultWritingSystem("English defn src");
			lsSrc.Gloss.AnalysisDefaultWritingSystem = "English gloss src";

			lsKeeper.MergeObject(lsSrc, true);
			Assert.AreEqual("English defn keep; English defn src", lsKeeper.Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("English gloss keep; English gloss src", lsKeeper.Gloss.AnalysisDefaultWritingSystem);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for Unicode and BigUnicode values.
		/// </summary>
		/// <remarks>FieldType.kcptUnicode and FieldType.kcptBigUnicode</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeUnicode()
		{
			CheckDisposed();

			// 15 CmFilter.Name
			string goodFilter = "Fram";
			string junkFilter = "Brand X";
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter keeper = filtersCol.Add(new CmFilter());
			ICmFilter src = filtersCol.Add(new CmFilter());

			// Merge content into null original.
			src.Name = goodFilter;
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Try to merge empty string into null.
			keeper.Name = null;
			src = filtersCol.Add(new CmFilter());
			src.Name = "";
			keeper.MergeObject(src);
			Assert.IsNull(keeper.Name);

			// Try to merge empty string into content.
			keeper.Name = goodFilter;
			src = filtersCol.Add(new CmFilter());
			src.Name = "";
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Try to merge content into content.
			keeper.Name = goodFilter;
			src = filtersCol.Add(new CmFilter());
			src.Name = junkFilter;
			keeper.MergeObject(src);
			Assert.AreEqual(keeper.Name, goodFilter);

			// Test merge append
			src = filtersCol.Add(new CmFilter());
			src.Name = junkFilter;
			keeper.MergeObject(src, true);
			Assert.AreEqual(keeper.Name, goodFilter +  ' ' + junkFilter);

			// But don't append if equal.
			keeper.Name = goodFilter;
			src = filtersCol.Add(new CmFilter());
			src.Name = goodFilter;
			keeper.MergeObject(src, true);
			Assert.AreEqual(keeper.Name, goodFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for MultiUnicode and MultiBigUnicode values.
		/// </summary>
		/// <remarks>FieldType.kcptMultiUnicode and FieldType.kcptMultiBigUnicode</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeMultiUnicode()
		{
			CheckDisposed();

			// 16 LexEntry.CitationForm
			// 20 Not used in database.
			string eng = "dog";
			string es = "perro";
			int engWs = Cache.LanguageEncodings.GetWsFromIcuLocale("en");
			int esWs = Cache.LanguageEncodings.GetWsFromIcuLocale("es");
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			// Merge content into null alternatives
			lmeSrc.CitationForm.SetAlternative(eng, engWs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.GetAlternative(engWs), eng);

			// Try to merge empty string into null content.
			lmeKeeper.CitationForm.SetAlternative(null, engWs);
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.CitationForm.SetAlternative("", engWs);
			Assert.IsNull(lmeKeeper.CitationForm.GetAlternative(engWs));

			// Merge content into empty string.
			lmeKeeper.CitationForm.SetAlternative("", engWs); // This actually sets the content to null, not an empty string.
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.CitationForm.SetAlternative(eng, engWs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.GetAlternative(engWs), eng);

			// Try to merge into existing content.
			lmeKeeper.CitationForm.SetAlternative(eng, engWs);
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.CitationForm.SetAlternative("cat", engWs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.GetAlternative(engWs), eng);

			// Make sure extant content isn't wrecked.
			lmeKeeper.CitationForm.SetAlternative(eng, engWs);
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.CitationForm.SetAlternative(es, esWs);
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(lmeKeeper.CitationForm.GetAlternative(engWs), eng);
			Assert.AreEqual(lmeKeeper.CitationForm.GetAlternative(esWs), es);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>FieldType.kcptOwningAtom</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningAtomic()
		{
			CheckDisposed();

			// 23 LexEntry.Pronunciation:LexPronunciation
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			// Merge content into null target.
			lmeSrc.LexemeFormOA = new MoStemAllomorph();
			lmeKeeper.MergeObject(lmeSrc);
			//Assert.IsNull(lmeSrc.LexemeFormOA);

			// Try to merge content into content.
			lmeKeeper.LexemeFormOA = new MoStemAllomorph();
			int hvoKeeper = lmeKeeper.LexemeFormOAHvo;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.LexemeFormOA = new MoStemAllomorph();
			int hvoSrc = lmeSrc.LexemeFormOAHvo;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(hvoKeeper, lmeKeeper.LexemeFormOAHvo);
			//Assert.AreEqual(hvoSrc, lmeSrc.LexemeFormOAHvo);

			// Try to merge null into content.
			lmeKeeper.LexemeFormOA = new MoStemAllomorph();
			hvoKeeper = lmeKeeper.LexemeFormOAHvo;
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.LexemeFormOA = null;
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(hvoKeeper, lmeKeeper.LexemeFormOAHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference atomic values.
		/// </summary>
		/// <remarks>FieldType.kcptReferenceAtom</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceAtomic()
		{
			CheckDisposed();

			// 24 LexSense.MorphoSyntaxAnalysis (refers to MSA)
			ILexEntry lme = m_entriesCol.Add(new LexEntry());

			ILexSense lsKeeper = lme.SensesOS.Append(new LexSense());
			ILexSense lsSrc = lme.SensesOS.Append(new LexSense());
			IMoStemMsa msa1 = (IMoStemMsa)lme.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			IMoStemMsa msa2 = (IMoStemMsa)lme.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());

			// Merge content into null.
			lsSrc.MorphoSyntaxAnalysisRAHvo = msa1.Hvo;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1.Hvo, lsKeeper.MorphoSyntaxAnalysisRAHvo);

			// Merge content into content.
			lsKeeper.MorphoSyntaxAnalysisRAHvo = msa1.Hvo;
			lsSrc = lme.SensesOS.Append(new LexSense());
			lsSrc.MorphoSyntaxAnalysisRAHvo = msa2.Hvo;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1.Hvo, lsKeeper.MorphoSyntaxAnalysisRAHvo);

			// Merge null into content.
			lsKeeper.MorphoSyntaxAnalysisRAHvo = msa1.Hvo;
			lsSrc = lme.SensesOS.Append(new LexSense());
			lsSrc.MorphoSyntaxAnalysisRA = null;
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(msa1.Hvo, lsKeeper.MorphoSyntaxAnalysisRAHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>FieldType.kcptOwningCollection</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningCollection()
		{
			CheckDisposed();

			// 25 LexEntry.MorphoSyntaxAnalyses
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());
			FdoOwningSequence<ICmPossibility> posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			posSeq.Append(new PartOfSpeech());
			posSeq.Append(new PartOfSpeech());

			// Merge content into null.
			IMoStemMsa msaSrc = (IMoStemMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			msaSrc.PartOfSpeechRA = (IPartOfSpeech)posSeq[0];
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual(msaSrc.Hvo, lmeKeeper.MorphoSyntaxAnalysesOC.HvoArray[0]);

			// Merge content into content.
			lmeSrc = m_entriesCol.Add(new LexEntry());
			IMoStemMsa msaSrc2 = (IMoStemMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			msaSrc2.PartOfSpeechRA = (IPartOfSpeech)posSeq[1];
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owned atomic values.
		/// </summary>
		/// <remarks>FieldType.kcptReferenceCollection</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceCollection()
		{
			CheckDisposed();

			// 26 LexSense.UsageTypes(refers to CmPossibility)
			ILexEntry lme = m_entriesCol.Add(new LexEntry());
			ILexSense lsKeeper = lme.SensesOS.Append(new LexSense());
			ILexSense lsSrc = lme.SensesOS.Append(new LexSense());
			FdoOwningSequence<ICmPossibility> usageSeq = m_ldb.UsageTypesOA.PossibilitiesOS;
			usageSeq.Append(new CmPossibility());
			usageSeq.Append(new CmPossibility());
			usageSeq.Append(new CmPossibility());
			int[] srcTypes = new int[2];

			// Merge content into null.
			srcTypes[0] = usageSeq[0].Hvo;
			srcTypes[1] = usageSeq[1].Hvo;
			lsSrc.UsageTypesRC.Add(srcTypes);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(2, lsKeeper.UsageTypesRC.Count);

			// Merge content into content.
			lsSrc = lme.SensesOS.Append(new LexSense());
			lsSrc.UsageTypesRC.Add(usageSeq[2].Hvo);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);

			// Merge duplicate content into content.
			lsSrc = lme.SensesOS.Append(new LexSense());
			lsSrc.UsageTypesRC.Add(usageSeq[2].Hvo);
			Assert.AreEqual(1, lsSrc.UsageTypesRC.Count);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);

			// Merge null into content.
			lsSrc = lme.SensesOS.Append(new LexSense());
			Assert.AreEqual(0, lsSrc.UsageTypesRC.Count);
			lsKeeper.MergeObject(lsSrc);
			Assert.AreEqual(3, lsKeeper.UsageTypesRC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for owning sequence values.
		/// </summary>
		/// <remarks>FieldType.kcptOwningSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeOwningSequence()
		{
			CheckDisposed();

			// 27 LexEntry.Senses
			ILexEntry lmeKeeper = m_entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = m_entriesCol.Add(new LexEntry());

			// Merge content into null.
			lmeSrc.SensesOS.Append(new LexSense());
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(1, lmeKeeper.SensesOS.Count);

			// Merge content into content.
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeSrc.SensesOS.Append(new LexSense());
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.SensesOS.Count);

			// Merge null into content.
			lmeSrc = m_entriesCol.Add(new LexEntry());
			lmeKeeper.MergeObject(lmeSrc);
			Assert.AreEqual(2, lmeKeeper.SensesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>FieldType.kcptReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeReferenceSequence()
		{
			CheckDisposed();

			// 28 MoAffixAllomorph.Position(refers to PhEnvironment)
			ILexEntry lme = m_entriesCol.Add(new LexEntry());

			IMoAffixAllomorph aKeeper = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			IMoAffixAllomorph aSrc = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			FdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			envsSeq.Append(new PhEnvironment());
			envsSeq.Append(new PhEnvironment());
			envsSeq.Append(new PhEnvironment());

			// Merge content into null.
			aSrc.PositionRS.Append(envsSeq[0].Hvo);
			aSrc.PositionRS.Append(envsSeq[1].Hvo);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(2, aKeeper.PositionRS.Count);

			// Merge duplicate content into content.
			aSrc = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			aSrc.PositionRS.Append(envsSeq[0].Hvo);
			aSrc.PositionRS.Append(envsSeq[1].Hvo);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(4, aKeeper.PositionRS.Count);

			// Merge content into content.
			aSrc = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			aSrc.PositionRS.Append(envsSeq[0].Hvo);
			aSrc.PositionRS.Append(envsSeq[1].Hvo);
			aSrc.PositionRS.Append(envsSeq[2].Hvo);
			aKeeper.MergeObject(aSrc);
			Assert.AreEqual(7, aKeeper.PositionRS.Count);

			// Merge null into content.
			aSrc = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
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
			CheckDisposed();

			ILexEntry lme = m_entriesCol.Add(new LexEntry());
			IMoStemMsa msaKeeper = (IMoStemMsa)lme.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			IMoStemMsa msaSrc = (IMoStemMsa)lme.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
			ILexSense ls1 = lme.SensesOS.Append(new LexSense());
			ls1.MorphoSyntaxAnalysisRAHvo = msaKeeper.Hvo;
			ILexSense ls2 = lme.SensesOS.Append(new LexSense());
			ls2.MorphoSyntaxAnalysisRAHvo = msaSrc.Hvo;

			// Shift atomic back reference.
			msaKeeper.MergeObject(msaSrc);
			Assert.AreEqual(ls2.MorphoSyntaxAnalysisRAHvo, msaKeeper.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>FieldType.kcptReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSequencePropertyBackReferences()
		{
			CheckDisposed();

			ILexEntry lme = m_entriesCol.Add(new LexEntry());
			FdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			IPhEnvironment envKeeper = envsSeq.Append(new PhEnvironment());
			IPhEnvironment envSrc = envsSeq.Append(new PhEnvironment());

			// Shift back references from reference sequence property.
			IMoAffixAllomorph referrer = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			referrer.PositionRS.Append(envSrc);
			envKeeper.MergeObject(envSrc);
			Assert.AreEqual(envKeeper.Hvo, referrer.PositionRS[0].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MergeObject method for reference sequence values.
		/// </summary>
		/// <remarks>FieldType.kcptReferenceSequence</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeCollectionPropertyBackReferences()
		{
			CheckDisposed();

			ILexEntry lme = m_entriesCol.Add(new LexEntry());
			FdoOwningSequence<IPhEnvironment> envsSeq = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			IPhEnvironment envKeeper = envsSeq.Append(new PhEnvironment());
			IPhEnvironment envSrc = envsSeq.Append(new PhEnvironment());

			// Shift back references from reference sequence property.
			IMoAffixAllomorph referrer = (IMoAffixAllomorph)lme.AlternateFormsOS.Append(new MoAffixAllomorph());
			referrer.PhoneEnvRC.Add(envSrc);
			envKeeper.MergeObject(envSrc);
			Assert.AreEqual(envKeeper.Hvo, referrer.PhoneEnvRC.HvoArray[0]);
		}
	}
}
