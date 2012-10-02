// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2005' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: fdoStringsTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.CoreTests.MultiFooTests
{
	/// <summary>
	/// Test the ITsMultiString implementation on MultiAccessor.
	/// </summary>
	[TestFixture]
	public class MultiAccessorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Set up class.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Add strings needed for tests.
			var tsf = Cache.TsStrFactory;
			var englishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("en");
			var spanishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("es");
			var lp = Cache.LangProject;

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, ()=>
			{
				// Set LP's WorldRegion.
				lp.WorldRegion.set_String(
					englishWsHvo,
					tsf.MakeString("Stateful FDO Test Project: World Region", englishWsHvo));
				lp.WorldRegion.set_String(
					spanishWsHvo,
					tsf.MakeString("Proyecto de prueba: FDO: Región del Mundo ", spanishWsHvo));

				// Set LP's Description.
				lp.Description.set_String(
					englishWsHvo,
					tsf.MakeString("Stateful FDO Test Language Project: Desc", englishWsHvo));
				lp.Description.set_String(
					spanishWsHvo,
					tsf.MakeString("Proyecto de prueba: FDO: desc", spanishWsHvo));

				// Add Spanish as Anal WS.
				IWritingSystem span = Cache.ServiceLocator.WritingSystemManager.Get(spanishWsHvo);
				lp.AddToCurrentAnalysisWritingSystems(span);
			});
		}

		/// <summary>
		///Make sure it has the right number of strings.
		/// </summary>
		[Test]
		public void StringCountTests1()
		{
			Assert.AreEqual(2, Cache.LangProject.WorldRegion.StringCount);
		}

		/// <summary>
		///Make sure it has the right number of strings.
		/// </summary>
		[Test]
		public void StringCountTests2()
		{
			Assert.AreEqual(0, Cache.LangProject.FieldWorkLocation.StringCount);
		}

		/// <summary>
		/// Make sure we can spin through the collection of strings,
		/// and get each one two ways, and that each retursn the same string.
		/// </summary>
		[Test]
		public void GetStringFromIndexAndget_StringTests()
		{
			var msa = Cache.LangProject.WorldRegion;
			Assert.AreEqual(2, msa.StringCount);
			for (var i = 0; i < msa.StringCount; ++i)
			{
				int ws;
				var tss = msa.GetStringFromIndex(i, out ws);
				var tss2 = msa.get_String(ws);
				Assert.AreSame(tss2, tss);
			}
		}

		/// <summary>
		/// Make sure it returns null for ws that is not present.
		/// </summary>
		[Test]
		public void MissingWsTest()
		{
			IWritingSystem fr = Cache.ServiceLocator.WritingSystemManager.Get("fr");
			var phantom = Cache.LangProject.WorldRegion.get_String(fr.Handle);
			Assert.IsTrue(string.IsNullOrEmpty(phantom.Text));

			// Make sure a made up ws is missing.
			phantom = Cache.LangProject.Description.get_String(1000);
			Assert.IsTrue(string.IsNullOrEmpty(phantom.Text));
		}

		/// <summary>
		/// Make sure it blows up on bad index.
		/// </summary>
		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Bad0IndexTest()
		{
			int ws;
			Cache.LangProject.FieldWorkLocation.GetStringFromIndex(0, out ws);
		}

		/// <summary>
		///Make sure it blows up on bad index.
		/// </summary>
		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void BadHighIndexTest()
		{
			var count = Cache.LangProject.WorldRegion.StringCount;
			int ws;
			Cache.LangProject.WorldRegion.GetStringFromIndex(count, out ws);
		}

		/// <summary>
		/// Make sure we can add a good string.
		/// </summary>
		[Test]
		public void CountTest()
		{
			// Start with expected information.
			Assert.AreEqual(2, Cache.LangProject.Description.StringCount, "Wrong number of alternatives for Cache.LangProject.DescriptionAccessor");

			// Create a good string.
			IWritingSystem german = Cache.ServiceLocator.WritingSystemManager.Get("de");

			var factory = Cache.TsStrFactory;
			var tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, german.Handle);
			tisb.Append("Deutchland");
			Cache.LangProject.Description.set_String(german.Handle, tisb.GetString());
			//// Make sure it is in there now.
			Assert.AreEqual(3, Cache.LangProject.Description.StringCount, "Wrong number of alternatives for Cache.LangProject.DescriptionAccessor");

			//// Add the same ws string, but with different text.
			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, german.Handle);
			tisb.Append("heilige");
			Cache.LangProject.Description.set_String(german.Handle, tisb.GetString());
			//// Make sure it is in there now.
			Assert.AreEqual(3, Cache.LangProject.Description.StringCount, "Wrong number of alternatives for Cache.LangProject.DescriptionAccessor");
		}

		// Since users can change ws in the middle of a line by hitting Alt-Shift, and
		// our code can't prevent it, we shouldn't kill the program by throwing exceptions.
		// So the next three tests are now invalid, and hence commented out.
		///// <summary>
		/////Make sure it only has one run in it.
		///// </summary>
		//[Test]
		//[ExpectedException(typeof(ArgumentException))]
		//public void MultipleRunsTest()
		//{
		//    var english = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(0);
		//    var spanish = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(1);
		//    var factory = Cache.TsStrFactory;
		//    var tisb = factory.GetIncBldr();
		//    var en = factory.MakeString("Mexico", english.Handle);
		//    tisb.AppendTsString(en);
		//    var es = factory.MakeString("Mejico", spanish.Handle);
		//    tisb.AppendTsString(es);
		//    Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());
		//}

		///// <summary>
		///// Make sure the string has no string properties.
		///// </summary>
		//[Test]
		//[ExpectedException(typeof(ArgumentException))]
		//public void ExtantStringPropertiesTest()
		//{
		//    var english = Cache.LangProject.CurrentAnalysisWritingSystems.First();
		//    var factory = Cache.TsStrFactory;
		//    var tisb = factory.GetIncBldr();
		//    tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Arial");
		//    tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
		//    tisb.Append("Mexico");
		//    Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());
		//}

		///// <summary>
		///// Make sure we can't add a string with more than one int property.
		///// </summary>
		//[Test]
		//[ExpectedException(typeof(ArgumentException))]
		//public void TooManyIntPropertiesTest()
		//{
		//    var english = Cache.LangProject.CurrentAnalysisWritingSystems.First();
		//    var factory = Cache.TsStrFactory;
		//    var tisb = factory.GetIncBldr();
		//    tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
		//    tisb.SetIntPropValues((int)FwTextPropType.ktptFontSize,
		//        (int)FwTextPropVar.ktpvMilliPoint, 8000);
		//    tisb.Append("Mexico");
		//    Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());
		//}

		/// <summary>
		/// Make sure we can add a good string.
		/// </summary>
		[Test]
		public void GoodMultiUnicodeTest()
		{
			// Start with expected information.
			Assert.AreEqual(0, Cache.LangProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LangProject.MainCountryAccessor");

			// Create a good string.
			var english = Cache.LangProject.CurrentAnalysisWritingSystems.First();
			var factory = Cache.TsStrFactory;
			var tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Mexico");
			Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());

			// Make sure it is in there now.
			Assert.AreEqual(1, Cache.LangProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LangProject.MainCountryAccessor");
			int ws;
			var mexico = Cache.LangProject.MainCountry.GetStringFromIndex(0, out ws);
			Assert.AreEqual(english.Handle, ws, "Wrong writing system.");
			Assert.AreEqual("Mexico", mexico.Text, "Wrong text.");

			// Add the same ws string, but with different text.
			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Guatemala");
			Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());

			// Make sure it is in there now.
			Assert.AreEqual(1, Cache.LangProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LangProject.MainCountryAccessor");
			var guatemala = Cache.LangProject.MainCountry.GetStringFromIndex(0, out ws);
			Assert.AreEqual(english.Handle, ws, "Wrong writing system.");
			Assert.AreEqual("Guatemala", guatemala.Text, "Wrong text.");
		}

		/// <summary>
		/// Test a regular single ITsString property (not a multi-).
		/// </summary>
		[Test]
		public void PlainStringTest()
		{
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var irOriginalValue = Cache.TsStrFactory.MakeString("import residue",
				Cache.WritingSystemFactory.UserWs);
			le.ImportResidue = irOriginalValue;
			Assert.AreSame(irOriginalValue, le.ImportResidue, "Wrong string.");
			var irNewValue = Cache.TsStrFactory.MakeString("New import residue",
				Cache.WritingSystemFactory.UserWs);
			le.ImportResidue = irNewValue;
			Assert.AreSame(irNewValue, le.ImportResidue, "Wrong string.");
		}

		/// <summary>
		/// Test the MergeAlternatives method.
		/// </summary>
		[Test]
		public void MergeAlternativesTest()
		{
			var english = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(0);
			var spanish = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(1);
			var factory = Cache.TsStrFactory;
			var tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Mexico");
			Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, spanish.Handle);
			tisb.Append("Mejico");
			Cache.LangProject.MainCountry.set_String(spanish.Handle, tisb.GetString());

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Saltillo");
			Cache.LangProject.FieldWorkLocation.set_String(english.Handle, tisb.GetString());

			Cache.LangProject.FieldWorkLocation.MergeAlternatives(Cache.LangProject.MainCountry);
			Assert.AreEqual("Saltillo", Cache.LangProject.FieldWorkLocation.get_String(english.Handle).Text);
			Assert.AreEqual("Mejico", Cache.LangProject.FieldWorkLocation.get_String(spanish.Handle).Text);

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, spanish.Handle);
			tisb.Append("Saltillo");
			Cache.LangProject.FieldWorkLocation.set_String(spanish.Handle, tisb.GetString());

			Cache.LangProject.FieldWorkLocation.MergeAlternatives(Cache.LangProject.MainCountry, true, ", ");
			Assert.AreEqual("Saltillo, Mexico", Cache.LangProject.FieldWorkLocation.get_String(english.Handle).Text);
			Assert.AreEqual("Saltillo, Mejico", Cache.LangProject.FieldWorkLocation.get_String(spanish.Handle).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AppendAlternatives method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AppendAlternativesTest()
		{
			var english = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(0);
			var spanish = Cache.LangProject.CurrentAnalysisWritingSystems.ElementAt(1);
			var factory = Cache.TsStrFactory;
			var tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Mexico");
			Cache.LangProject.MainCountry.set_String(english.Handle, tisb.GetString());

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, spanish.Handle);
			tisb.Append("Mejico");
			Cache.LangProject.MainCountry.set_String(spanish.Handle, tisb.GetString());

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, english.Handle);
			tisb.Append("Saltillo");
			Cache.LangProject.FieldWorkLocation.set_String(english.Handle, tisb.GetString());

			Cache.LangProject.FieldWorkLocation.AppendAlternatives(Cache.LangProject.MainCountry);
			Assert.AreEqual("Saltillo Mexico", Cache.LangProject.FieldWorkLocation.get_String(english.Handle).Text);
			Assert.AreEqual("Mejico", Cache.LangProject.FieldWorkLocation.get_String(spanish.Handle).Text);

			tisb = factory.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, spanish.Handle);
			tisb.Append("Saltillo");
			Cache.LangProject.FieldWorkLocation.set_String(spanish.Handle, tisb.GetString());

			((ITsMultiString)Cache.LangProject.MainCountry).set_String(english.Handle, null);

			Cache.LangProject.FieldWorkLocation.AppendAlternatives(Cache.LangProject.MainCountry);
			Assert.AreEqual("Saltillo Mexico", Cache.LangProject.FieldWorkLocation.get_String(english.Handle).Text);
			Assert.AreEqual("Saltillo Mejico", Cache.LangProject.FieldWorkLocation.get_String(spanish.Handle).Text);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for fdoStringsTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoStringsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region data members
		IMultiAccessorBase m_multi;
		IText m_text;
		private IWritingSystem m_wsGerman;
		private IWritingSystem m_wsFrench;
		private IWritingSystem m_wsSpanish;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does setup for all the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out m_wsGerman);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out m_wsFrench);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out m_wsSpanish);
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				ChangeDefaultAnalWs(m_wsGerman);
				Cache.LangProject.AddToCurrentAnalysisWritingSystems(m_wsSpanish);
			});
	}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Create a MultiUnicodeAccessor
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(m_text);
			IStText stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_text.ContentsOA = stText;
			m_multi = stText.Title;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the default analysis WS.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_DefaultAnalExists()
		{
			m_text.Name.SetAnalysisDefaultWritingSystem("Hallo");
			m_text.Name.set_String(m_wsSpanish.Handle, "Hola");
			m_text.Name.SetUserWritingSystem("YeeHaw");
			m_text.Name.set_String(m_wsSpanish.Handle, "Hello");
			Assert.AreEqual("Hallo", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the default analysis WS.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_EnglishExists()
		{
			m_text.Name.set_String(m_wsSpanish.Handle, "Hello");
			Assert.AreEqual("Hello", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has no
		/// alternatives stored.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_NoAlternativesExist()
		{
			Assert.AreEqual("***", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored analysis WS's other than the default.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_OtherAnalExists()
		{
			m_text.Name.set_String(m_wsSpanish.Handle, "Hola");
			m_text.Name.SetUserWritingSystem("YeeHaw");
			m_text.Name.set_String(Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en"), "Hello");
			Assert.AreEqual("Hola", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the UI writing system, but none of the analysis WS's.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_UIExists()
		{
			m_text.Name.SetUserWritingSystem("YeeHaw");
			m_text.Name.set_String(m_wsFrench.Handle, "Hello");
			Assert.AreEqual("YeeHaw", m_multi.BestAnalysisAlternative.Text);
		}
	}
}
