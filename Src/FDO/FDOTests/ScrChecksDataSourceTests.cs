// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrChecksDataSourceTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests
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
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dataSource = new ScrChecksDataSource(Cache, DirectoryFinder.TeStylesPath);
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

			Assert.AreEqual(20, sentenceInitialStyles.Count);
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
			IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Get(hvoWs);
			PuncPatternsList list = new PuncPatternsList();
			list.Add(new PuncPattern(". ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern("? ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern("! ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern("; ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern("- ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern(") ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern("( ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			list.Add(new PuncPattern(": ", ContextPosition.WordFinal, PuncPatternStatus.Valid));
			ws.PunctuationPatterns = list.XmlString;

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
			IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Get(hvoWs);
			// We add the following Arabic punctuation: percent sign (066A), decimal separator (066B),
			// thousands separator (066C), five pointed star (066D), full stop (06D4), question mark (061F)
			var list = new PuncPatternsList
						{
							new PuncPattern("\u066A ", ContextPosition.WordFinal, PuncPatternStatus.Valid),
							new PuncPattern("\u066B ", ContextPosition.WordFinal, PuncPatternStatus.Valid),
							new PuncPattern("\u066C ", ContextPosition.WordFinal, PuncPatternStatus.Valid),
							new PuncPattern("\u066D ", ContextPosition.WordFinal, PuncPatternStatus.Valid),
							new PuncPattern("\u06D4 ", ContextPosition.WordFinal, PuncPatternStatus.Valid),
							new PuncPattern("\u061F ", ContextPosition.WordFinal, PuncPatternStatus.Valid)
						};
			ws.PunctuationPatterns = list.XmlString;

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
			IWritingSystem ws = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			ws.RightToLeftScript = true;
			Assert.AreEqual("\u200f-\u200f", m_dataSource.GetParameterValue("Verse Bridge"));
			ws.RightToLeftScript = false;
			Assert.AreEqual("-", m_dataSource.GetParameterValue("Verse Bridge"));
		}
	}
}
