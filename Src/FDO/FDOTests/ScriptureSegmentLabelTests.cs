using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class ScriptureSegmentLabelTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IText m_text;
		private IStText m_stText;
		private IScrTxtPara m_para;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		int m_wsVern;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
										   FixtureSetupInternal);
		}

		private void FixtureSetupInternal()
		{
			//IWritingSystem wsEn = Cache.WritingSystemFactory.get_Engine("en");
			// Setup default analysis ws
			//m_wsEn = Cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetObject(wsEn.WritingSystem);
			m_wsVern = Cache.DefaultVernWs;

			m_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(m_text);
			m_stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_text.ContentsOA = m_stText;
			m_para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(m_stText, ScrStyleNames.NormalParagraph);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void OneSegPerVerse()
		{
			string pc1 = "Das Buch ist rot. ";
			string verse1 = "9";
			string pc2 = "Der Herr ist gross.";
			string verse2 = "10";
			string pc3 = "Ich spreche nicht viel Deutsch.";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + verse1 + pc2 + verse2 + pc3, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length, pc1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichV2 = pc1.Length + verse1.Length + pc2.Length;
			bldr.SetStrPropValue(ichV2, ichV2 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents = bldr.GetString();
			using (ParagraphParser pp = new ParagraphParser(m_para))
			{
				List<int> eosIndexes;
				var segments = pp.CollectSegments(m_para.Contents, out eosIndexes);
				Assert.AreEqual(5, segments.Count);
				Assert.AreEqual("", ScriptureServices.VerseSegLabel(m_para, 0));
				Assert.AreEqual("", ScriptureServices.VerseSegLabel(m_para, 2));
				Assert.AreEqual("", ScriptureServices.VerseSegLabel(m_para, 4));
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void TwoSegsPerVerse()
		{
			string pc1 = "Das Buch ist rot. ";
			string pc2 = "Das Maedchen ist schoen.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was ist das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + pc2.Length + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents = bldr.GetString();
			using (ParagraphParser pp = new ParagraphParser(m_para))
			{
				List<int> eosIndexes;
				var segments = pp.CollectSegments(m_para.Contents, out eosIndexes);
				Assert.AreEqual(8, segments.Count);
				Assert.AreEqual("a", ScriptureServices.VerseSegLabel(m_para, 0));
				Assert.AreEqual("b", ScriptureServices.VerseSegLabel(m_para, 1));
				Assert.AreEqual("a", ScriptureServices.VerseSegLabel(m_para, 3));
				Assert.AreEqual("b", ScriptureServices.VerseSegLabel(m_para, 4));
				Assert.AreEqual("a", ScriptureServices.VerseSegLabel(m_para, 6));
				Assert.AreEqual("b", ScriptureServices.VerseSegLabel(m_para, 7));
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MultipleParas()
		{
			var paraFactory = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>();
			IScrTxtPara paraPrev = paraFactory.CreateWithStyle(m_stText, 0, ScrStyleNames.NormalParagraph);
			IScrTxtPara paraFirst = paraFactory.CreateWithStyle(m_stText, 0, ScrStyleNames.NormalParagraph);
			IScrTxtPara paraNext = paraFactory.CreateWithStyle(m_stText, ScrStyleNames.NormalParagraph);
			IScrTxtPara paraLast = paraFactory.CreateWithStyle(m_stText, ScrStyleNames.NormalParagraph);

			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + verse2 + pc5, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + pc2.Length + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			var segments = GetSegments(bldr, m_para);

			string verse8 = "8";
			bldr = m_tsf.MakeString(verse8 + pc3 + pc4, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse8.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraFirst);

			bldr = m_tsf.MakeString(pc1 + pc2, m_wsVern).GetBldr();
			GetSegments(bldr, paraPrev);

			string verse11 = "11";
			bldr = m_tsf.MakeString(pc3 + verse11 + pc4, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc3.Length, pc3.Length + verse11.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraNext);

			string verse12 = "12";
			bldr = m_tsf.MakeString(verse12 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse12.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraLast);
			Assert.AreEqual(7, segments.Count);
			Assert.AreEqual("a", ScriptureServices.VerseSegLabel(paraFirst, 1));
			Assert.AreEqual("b", ScriptureServices.VerseSegLabel(paraFirst, 2));
			Assert.AreEqual("c", ScriptureServices.VerseSegLabel(paraPrev, 0));
			Assert.AreEqual("d", ScriptureServices.VerseSegLabel(paraPrev, 1));
			Assert.AreEqual("e", ScriptureServices.VerseSegLabel(m_para, 0));
			Assert.AreEqual("f", ScriptureServices.VerseSegLabel(m_para, 1));
			Assert.AreEqual("a", ScriptureServices.VerseSegLabel(m_para, 3));
			Assert.AreEqual("b", ScriptureServices.VerseSegLabel(m_para, 4));
			Assert.AreEqual("a", ScriptureServices.VerseSegLabel(m_para, 6), "should have label because seg in following para");
			Assert.AreEqual("b", ScriptureServices.VerseSegLabel(paraNext, 0), "should have label due to previous para");
			Assert.AreEqual("", ScriptureServices.VerseSegLabel(paraNext, 2),
							"should have no label because next para starts with verse");
		}

		private IList<ISegment> GetSegments(ITsStrBldr bldr, IScrTxtPara para)
		{
			para.Contents = bldr.GetString();
			using (ParagraphParser pp = new ParagraphParser(para))
			{
				List<int> eosIndexes;
				var segments = pp.CollectSegments(para.Contents, out eosIndexes);
				return segments;
			}
		}
	}
}
