using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	[NUnit.Framework.TestFixture]
	public class DumperTests : InMemoryFdoTestBase
	{
		//FdoCache m_fdoCache;
		protected string m_databaseName;

		/// <summary>
		/// any filters that we want, for example, to only output items which satisfy their constraint.
		/// </summary>
		protected IFilterStrategy[] m_filters;

		private int _germanId;
		private int _frenchId;

		protected override void CreateTestData()
		{
		 //   m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();
			m_inMemoryCache.InitializeWordFormInventory();

			//set the second analysis alternative, german
			_germanId = Cache.LanguageEncodings.GetWsFromIcuLocale("de");
			_frenchId = Cache.LanguageEncodings.GetWsFromIcuLocale("fr");
			Cache.LangProject.Name.SetAlternative("bestAnalName-german", _germanId);
			Cache.LangProject.Name.SetAlternative("frenchNameOfProject", _frenchId);
			Cache.LangProject.WorldRegion.SetAlternative("arctic-german", _germanId);
			Cache.LangProject.LexDbOA.Name.SetAlternative("frenchLexDBName", _frenchId);
			//clear the first analysis alternative
			Cache.LangProject.Name.AnalysisDefaultWritingSystem = null;


		}

//    	[TestFixtureSetUp]
//		public void FixtureSetup()
//		{
//			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
//			cacheOptions.Add("db", "TestLangProj");
//			m_fdoCache = FdoCache.Create(cacheOptions);
//		}


//		[TestFixtureTearDown]
//		public void FixtureCleanUp()
//		{
//			if (m_fdoCache != null)
//			{
//				m_fdoCache.Dispose();
//				m_fdoCache = null;
//			}
//        }

		[Test]
		public void CanFindTemplateForExactMatch()
		{
			GetResultString("<objAtomic objProperty='WordformInventoryOA'/>",
				"<class name='WordformInventory'><wfi></wfi></class>");
		}

		[Test]
		public void CanFindTemplateForBaseClass()
		{
			GetResultString("<objAtomic objProperty='WordformInventoryOA'/>",
				"<class name='CmObject'><object></object></class>");
		}

		[Test]
		public void DefaultIs_RequireClassTemplatesForEverything_False()
		{
			GetResultString("<objAtomic objProperty='WordformInventoryOA'/>", "", "");
		}

		[Test]
		public void WritingSystemAttributeStyle()
		{
			string result = GetResultString("<test><multilingualStringElement ws='all' name='name' simpleProperty='Name'/></test>", "",
				"writingSystemAttributeStyle='LIFT'");
			Assert.AreEqual("<test><name lang=\"de\">bestAnalName-german</name>\r\n<name lang=\"fr\">frenchNameOfProject</name>\r\n</test>", result.Trim());
		}

		[Test]
		public void GetBestAnalysisAttribute()
		{
			string result = GetResultString("<test><attribute ws='BestAnalysis' name='lang' simpleProperty='Name'/></test>", "", "");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.ChildNodes[0].Attributes["lang"].Value;
			Assert.AreEqual("bestAnalName-german", attr);
		}


		[Test]
		public void GetMissingVernacularAttribute()
		{
			string result = GetResultString("<test><attribute ws='DefaultVernacular' name='lang' simpleProperty='Name'/></test>", "", "");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = XmlUtils.GetOptionalAttributeValue(doc.ChildNodes[0], "lang");
			Assert.AreEqual("", attr);
		}

		[Test]
		public void GetBestVernacularAnalysisAttribute()
		{
			Cache.LangProject.Name.SetAlternative("bestAnalName-german", _germanId);
			Cache.LangProject.Name.SetAlternative(null, _frenchId);
			string result = GetResultString("<test><attribute ws='BestVernacularOrAnalysis' name='lang' simpleProperty='Name'/></test>", "", "");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.ChildNodes[0].Attributes["lang"].Value;
			Assert.AreEqual("bestAnalName-german", attr);
		}

		[Test, ExpectedException(typeof(RuntimeConfigurationException))]
		public void MissingClassTemplatesThrowsInStrictMode()
		{
			GetResultString("<objAtomic objProperty='WordformInventoryOA'/>", "", "requireClassTemplatesForEverything='true'");
		}


		[Test]
		public void OutputComment()
		{
			Check("<comment>hello word</comment>", "<!--hello word-->");
		}


		[Test]
		public void MultilingualStringBasedonStringDictionary()
		{
			FdoReferenceSequence<ILgWritingSystem> systems = Cache.LangProject.CurVernWssRS;
			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			IMoForm m = MoForm.MakeMorph(Cache, le, "-is");
			m.Form.SetAlternative("iz", systems[1].Hvo);

			string t = "<entry><objAtomic objProperty='LexemeFormOA'/></entry>";
			string c = "<class name='MoForm'><multilingualStringElement name='form' simpleProperty='NamesWithMarkers'/></class>";
			string result = GetResultStringFromEntry(le, t, c);

			Assert.AreEqual("<entry><form ws=\"fr\">-is</form>\r\n<form ws=\"ur\">-iz</form>\r\n</entry>",result.Trim());
		}


		[Test]
		public void MultilingualStringElementNoWrapperSpecified()
		{
			Check("<multilingualStringElement name=\"form\" simpleProperty=\"WorldRegion\"/>"
				, "<form ws=\"de\">arctic-german</form>\r\n");
		}

		[Test]
		public void NotEmptyMultilingualStringElementOutputsWrapper()
		{
			Check("<multilingualStringElement wrappingElementName='region' name=\"form\" simpleProperty=\"WorldRegion\"/>"
				, "<region>\r\n<form ws=\"de\">arctic-german</form>\r\n</region>\r\n");
		}

		[Test]
		public void EmptyMultilingualStringElementDoesntOutputWrapper()
		{
			Check("<multilingualStringElement wrappingElementName='region' ws='all vernacular' name=\"form\" simpleProperty=\"WorldRegion\"/>"
				, "");
		}

		[Test]
		public void OutputGuidOfAtomicReferencedObjectWhichIsNull()
		{
			Check("<element name='test'><attributeIndirect  name='ref' simpleProperty='Guid' target='ThesaurusRA'/></element>"
				, "<test></test>\r\n");
		}

		[Test]
		public void OutputNameOfAtomicObjectAsAttribute()
		{
			string result = GetResultString("<element name='test'><attributeIndirect  name='value'  ws='BestAnalysisOrVernacular' simpleProperty='Name' target='LexDbOA'/></element>");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.ChildNodes[0].Attributes["value"].Value;

			Assert.AreEqual("frenchLexDBName", attr);
		}

		[Test]
		public void OutputGuidOfAtomicObjectAsAttribute()
		{
			string result = GetResultString("<element name='test'><attributeIndirect  name='superDuperRef'  simpleProperty='Guid' target='WordformInventoryOA'/></element>");
			XmlDocument doc= new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.ChildNodes[0].Attributes["superDuperRef"].Value;

			Assert.IsNotNull(new Guid(attr));
		}

		[Test]
		public void OutputHvoOfAtomicObjectAsAttribute()
		{
			string result = GetResultString("<element name='test'><attributeIndirect  name='hvoRef'  simpleProperty='Hvo' target='WordformInventoryOA'/></element>");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.ChildNodes[0].Attributes["hvoRef"].Value;

			Assert.IsNotNull(int.Parse(attr));
		}


		[Test,Ignore("apparent memory cache bug prevents test")]
		public void OutputGuidOfOwnerAsAttribute()
		{
			Assert.AreEqual(Cache.LangProject.Hvo,Cache.LangProject.WordformInventoryOA.OwnerHVO);
			Assert.IsNotNull(Cache.LangProject.WordformInventoryOA);
			Assert.Greater(Cache.LangProject.WordformInventoryOA.OwnerHVO, 0);
			string result = GetResultString("<objAtomic objProperty='WordformInventoryOA'/>",
			   "<class name='WordformInventory'><wfi><attributeIndirect  name='parent' simpleProperty='Guid' target='Owner'/></wfi></class>");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			string attr = doc.SelectSingleNode("wfi").Attributes["parent"].Value;

			Assert.AreEqual(Cache.LangProject.Guid.ToString(), attr);
		}

		private void Check(string content, string expectedResult)
		{
			Assert.AreEqual(expectedResult, GetResultString(content));
		}

		private string GetResultString(string insideLangProjClass)
		{
			return GetResultString(insideLangProjClass, string.Empty);
		}
		private string GetResultString(string insideLangProjClass, string afterLangProjClass)
		{
			return GetResultString(insideLangProjClass, afterLangProjClass, "requireClassTemplatesForEverything='true' doUseBaseClassTemplatesIfNeeded='true'");
		}
		private string GetResultString(string insideLangProjClass, string afterLangProjClass, string templateAttributes)
		{
			XDumper dumper = new XDumper(Cache);
			dumper.FxtDocument = new XmlDocument();
			//sadly, the current dumper code requires the first thing inside the template to be a class node
			dumper.FxtDocument.LoadXml(string.Format("<template {0}><class name=\"LangProject\">{1}</class>{2}</template>", templateAttributes, insideLangProjClass, afterLangProjClass));
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			StringWriter writer = new StringWriter(builder);
			dumper.Go(Cache.LangProject as CmObject, writer, new IFilterStrategy[] { });
			return builder.ToString();
		}

		private string GetResultStringFromEntry(ILexEntry entry, string insideClass, string afterClass)
		{
			string templateAttributes = "requireClassTemplatesForEverything='true' doUseBaseClassTemplatesIfNeeded='true'";
			XDumper dumper = new XDumper(Cache);
			dumper.FxtDocument = new XmlDocument();
			//sadly, the current dumper code requires the first thing inside the template to be a class node
			dumper.FxtDocument.LoadXml(string.Format("<template {0}><class name=\"LexEntry\">{1}</class>{2}</template>", templateAttributes, insideClass, afterClass));
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			StringWriter writer = new StringWriter(builder);
			dumper.Go(entry as CmObject, writer, new IFilterStrategy[] { });
			return builder.ToString();
		}
	}

}