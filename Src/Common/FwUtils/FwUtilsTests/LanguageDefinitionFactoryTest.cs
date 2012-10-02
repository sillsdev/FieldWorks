// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageDefinitionFactoryTest.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the LanguageDefinitionFactory class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LanguageDefinitionFactoryTest
	{
		private ILgWritingSystemFactory m_wsf;
		private int m_wsIdEn;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_wsf = LgWritingSystemFactoryClass.Create();
			// This is typically run during the build process before InstallLanguage.exe has
			// been built, so we want to disable InstallLanguage for this test.
			m_wsf.BypassInstall = true;

			IWritingSystem wsEn = m_wsf.get_Engine("en");
			m_wsIdEn = m_wsf.GetWsFromStr("en");
			wsEn.set_Name(m_wsIdEn, "English");
			wsEn.set_Abbr(m_wsIdEn, "ENG");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends a test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests serializing and deserializing an LanguageDefinitionFactory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeAndDeserialize()
		{
			IWritingSystem ws = m_wsf.get_Engine("tl");
			int wsIdTl = m_wsf.GetWsFromStr("tl");
			ws.Locale = 13321;
			ws.DefaultMonospace = "Courier New";
			ws.DefaultSansSerif = "Arial";
			ws.DefaultBodyFont = "Charis SIL";
			ws.DefaultSerif = "Times New Roman";
			ws.set_Name(m_wsIdEn, "Tagalog");
			ws.set_Name(wsIdTl, "Tagalog");
			ws.set_Abbr(m_wsIdEn, "TGL");

			ICollation coll = CollationClass.Create();
			coll.WinLCID = 1033;
			coll.WinCollation = "Latin1_General_CI_AI";
			coll.set_Name(m_wsIdEn, "Default Collation");
			coll.WritingSystemFactory = m_wsf;

			ws.set_Collation(0, coll);

			LanguageDefinition langDef = new LanguageDefinition(ws);
			langDef.BaseLocale = "en_US";
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "tl";
			langDef.LocaleName = "Tagalog";
			langDef.LocaleScript = "";
			langDef.LocaleCountry = "";
			langDef.LocaleVariant = "";
			langDef.XmlWritingSystem.WritingSystem.Locale = 13321;
			langDef.CollationElements = "\"&amp; B &lt; ...";
			langDef.ValidChars = "abcdefg";
			langDef.LocaleResources = @"
				zoneStrings {
					{
						'Europe/London',
						'Greenwich Mean Time',
				  }
				}";
			CharDef[] charDefs = new CharDef[2];
			charDefs[0] = new CharDef(0xF170, "COMBINING SNAKE BELOW;Mn;202;NSM;;;;");
			charDefs[1] = new CharDef(0xF210, "LATIN SMALL LETTER P WITH STROKE;Ll;0;L;;;;");
			langDef.PuaDefinitions = charDefs;
			FileName[] fonts = new FileName[4];
			fonts[0] = new FileName("arial.ttf");
			fonts[1] = new FileName("arialbd.ttf");
			fonts[2] = new FileName("ariali.ttf");
			fonts[3] = new FileName("arialbi.ttf");
			langDef.Fonts = fonts;
			langDef.Keyboard = new FileName("Tagalog.kmx");
			langDef.EncodingConverter = new EncodingConverter("SIL_IPA93.tec.vbs", "SIL-IPA93.tec");

			string tmpFileName = Path.GetTempFileName();
			langDef.Serialize(tmpFileName);

			LanguageDefinitionFactory otherIcuWs = new LanguageDefinitionFactory();
			LanguageDefinitionFactory.WritingSystemFactory = m_wsf;
			otherIcuWs.Deserialize(tmpFileName);
			ILanguageDefinition newLangDef = otherIcuWs.LanguageDefinition;
			IWritingSystem deserializedWs = newLangDef.WritingSystem;
			ICollation deserializedColl = newLangDef.GetCollation(0);

			StreamReader reader = new StreamReader(tmpFileName);
			string line = reader.ReadLine();
			while (line != null)
			{
				Console.WriteLine(line);
				line = reader.ReadLine();
			}
			reader.Close();
			File.Delete(tmpFileName);

			Assert.AreEqual(ws.Locale, deserializedWs.Locale);
			Assert.AreEqual(ws.IcuLocale, deserializedWs.IcuLocale);
			Assert.AreEqual(ws.DefaultSansSerif, deserializedWs.DefaultSansSerif);
			Assert.AreEqual(ws.DefaultBodyFont, deserializedWs.DefaultBodyFont);
			Assert.AreEqual(ws.get_Name(m_wsIdEn), deserializedWs.get_Name(m_wsIdEn));
			Assert.AreEqual(coll.WinLCID, deserializedColl.WinLCID);
			Assert.AreEqual(coll.WinCollation, deserializedColl.WinCollation);
		// ENHANCE: Add ValidChars to the interface
		// Assert.AreEqual(ws.ValidChars, deserializedWs.ValidChars);
			Assert.AreEqual(coll.get_Name(m_wsIdEn), deserializedColl.get_Name(m_wsIdEn));
		}
	}
}
