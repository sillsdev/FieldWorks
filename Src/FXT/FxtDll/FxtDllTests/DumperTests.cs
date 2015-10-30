// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
#if WANTTESTPORT //(FLEx) Need to port these tests to the new FDO
	[TestFixture]
	public class DumperTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// any filters that we want, for example, to only output items which satisfy their constraint.
		/// </summary>
		protected IFilterStrategy[] m_filters;
		private int m_germanId;
		private int m_frenchId;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			//set the second analysis alternative, german
			m_germanId = Cache.WritingSystemFactory.GetWsFromStr("de");
			m_frenchId = Cache.WritingSystemFactory.GetWsFromStr("fr");

			ITsStrFactory tsf = Cache.TsStrFactory;
			Cache.LangProject.MainCountry.set_String(m_germanId, tsf.MakeString("bestAnalName-german", m_germanId));
			Cache.LangProject.MainCountry.set_String(m_frenchId, tsf.MakeString("frenchNameOfProject", m_frenchId));
			Cache.LangProject.WorldRegion.set_String(m_germanId, tsf.MakeString("arctic-german", m_germanId));
			Cache.LangProject.LexDbOA.Name.set_String(m_frenchId, tsf.MakeString("frenchLexDBName", m_frenchId));
			//clear the first analysis alternative
			Cache.LangProject.MainCountry.AnalysisDefaultWritingSystem = null;
		}

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
			Assert.AreEqual(String.Format("<test><name lang=\"de\">bestAnalName-german</name>{0}<name lang=\"fr\">frenchNameOfProject</name>{0}</test>", Environment.NewLine), result.Trim());

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
			Cache.LangProject.MainCountry.set_String(m_germanId, Cache.TsStrFactory.MakeString("bestAnalName-german", m_germanId));
			Cache.LangProject.MainCountry.set_String(m_frenchId, null);
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
			FdoReferenceSequence<LgWritingSystem> systems = Cache.LangProject.CurVernWssRS;
			LexDb ld = Cache.LangProject.LexDbOA;
			LexEntry le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			MoForm m = MoForm.MakeMorph(le, "-is");
			// It will already have the right form.
			//m.FormAccessor.set_String(systems[1].Hvo, Cache.TsStrFactory.MakeString("iz", systems[1].Hvo));

			string t = "<entry><objAtomic objProperty='LexemeFormOA'/></entry>";
			string c = "<class name='MoForm'><multilingualStringElement name='form' simpleProperty='NamesWithMarkers'/></class>";
			string result = GetResultStringFromEntry(le, t, c);

			Assert.AreEqual(String.Format("<entry><form ws=\"fr\">-is</form>{0}<form ws=\"ur\">-iz</form>{0}</entry>", Environment.NewLine), result.Trim());
		}


		[Test]
		public void MultilingualStringElementNoWrapperSpecified()
		{
			Check(String.Format("<multilingualStringElement name=\"form\" simpleProperty=\"WorldRegion\"/>"
				, "<form ws=\"de\">arctic-german</form>{0}", Environment.NewLine));
		}

		[Test]
		public void NotEmptyMultilingualStringElementOutputsWrapper()
		{
			Check(String.Format("<multilingualStringElement wrappingElementName='region' name=\"form\" simpleProperty=\"WorldRegion\"/>"
				, "<region>{0}<form ws=\"de\">arctic-german</form>{0}</region>{0}", Environment.NewLine);
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
			Check(String.Format("<element name='test'><attributeIndirect  name='ref' simpleProperty='Guid' target='ThesaurusRA'/></element>"
				, "<test></test>{0}", Environment.NewLine));
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
			Assert.AreEqual(Cache.LangProject.Hvo, Cache.LangProject.WordformInventoryOA.Owner.Hvo);
			Assert.IsNotNull(Cache.LangProject.WordformInventoryOA);
			Assert.Greater(Cache.LangProject.WordformInventoryOA.Owner.Hvo, 0);
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

		private string GetResultStringFromEntry(LexEntry entry, string insideClass, string afterClass)
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
#endif
}
