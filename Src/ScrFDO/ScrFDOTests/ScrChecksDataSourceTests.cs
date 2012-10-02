using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.Scripture.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test fixture for ScrChecksDataSource.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrChecksDataSourceTests : ScrInMemoryFdoTestBase
	{
		private ScrChecksDataSource m_dataSource = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void SetupFixture()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			m_dataSource = new ScrChecksDataSource(Cache);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetParameterValue method to get information about the styles.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetParameterValue_StylesInfo()
		{
			string xmlStyleInfo = m_dataSource.GetParameterValue("StylesInfo");
			StylePropsInfo styleInfo = (StylePropsInfo)ReflectionHelper.GetProperty(
				m_dataSource, "StyleInfo");

			List<StyleInfo> sentenceInitialStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_sentenceInitial");
			List<StyleInfo> properNounStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_properNoun");
			List<StyleInfo> tableStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_table");
			List<StyleInfo> listStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_list");
			List<StyleInfo> specialStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_special");
			List<StyleInfo> titleStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_title");
			List<StyleInfo> headingStyles = (List<StyleInfo>)ReflectionHelper.GetField(
				typeof(StylePropsInfo), "s_heading");

			Assert.AreEqual(21, sentenceInitialStyles.Count);
			Assert.AreEqual(1, properNounStyles.Count);
			Assert.AreEqual(5, tableStyles.Count);
			Assert.AreEqual(8, listStyles.Count);
			Assert.AreEqual(5, specialStyles.Count);
			Assert.AreEqual(3, titleStyles.Count);
			Assert.AreEqual(11, headingStyles.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetParameterValue method to get sentence final punctuation.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetParameterValue_SentenceFinalPunctuation()
		{
			// Set up the punctuation for a Roman-script vernacular writing system.
			int hvoWs = Cache.DefaultVernWs;
			ILgWritingSystemFactory lgwsf = Cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem ws = lgwsf.get_EngineOrNull(hvoWs);
			LanguageDefinition langDef = new LanguageDefinition(ws);
			// The test writing system has the following characters for punctuation:
			//   .?!;-)(:
			langDef.PunctuationPatterns = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
				"<PunctuationPatterns>" +
				"<pattern value=\". \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"? \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"! \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"; \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"- \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\") \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"( \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\": \" context=\"WordFinal\" valid=\"true\" /></PunctuationPatterns>";

			// Get the sentence-final punctuation
			string sentenceFinalPunc = m_dataSource.GetParameterValue("SentenceFinalPunctuation");

			// We expect that only sentence-final punctuation would be returned.
			Assert.AreEqual(".?!", sentenceFinalPunc);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetParameterValue method to get sentence final punctuation for a non-Roman
		/// script.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetParameterValue_SentenceFinalPunctuationNR()
		{
			// Set up the punctuation for a Roman-script vernacular writing system.
			int hvoWs = Cache.DefaultVernWs;
			ILgWritingSystemFactory lgwsf = Cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem ws = lgwsf.get_EngineOrNull(hvoWs);
			LanguageDefinition langDef = new LanguageDefinition(ws);
			// We add the following Arabic punctuation: percent sign (066A), decimal separator (066B),
			// thousands separator (066C), five pointed star (066D), full stop (06D4), question mark (061F)
			langDef.PunctuationPatterns = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
				"<PunctuationPatterns>" +
				"<pattern value=\"\u066A \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"\u066B \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"\u066C \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"\u066D \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"\u06D4 \" context=\"WordFinal\" valid=\"true\" />" +
				"<pattern value=\"\u061F \" context=\"WordFinal\" valid=\"true\" /></PunctuationPatterns>";

			// Get the sentence-final punctuation
			string sentenceFinalPunc = m_dataSource.GetParameterValue("SentenceFinalPunctuation");

			// We expect that only sentence-final punctuation would be returned.
			Assert.AreEqual("\u06D4\u061F", sentenceFinalPunc);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetParameterValue method to get the Verse Bridge parameter for left-to-
		/// right and right-to-left writing systems.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetParameterValue_VerseBridge()
		{
			ILgWritingSystem lgws = new LgWritingSystem(Cache, Cache.DefaultVernWs);

			lgws.RightToLeft = true;
			Assert.AreEqual("\u200f-\u200f", m_dataSource.GetParameterValue("Verse Bridge"));
			lgws.RightToLeft = false;
			Assert.AreEqual("-", m_dataSource.GetParameterValue("Verse Bridge"));
		}
	}
}
