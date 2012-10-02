// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2004' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringUtilsTest.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using System.IO;
using System.Text;	// for ILgWritingSystemFactory
using NMock;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StringUtils tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StringUtilsTest
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			Icu.InitIcuDataDir();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			SIL.FieldWorks.Common.FwUtils.Icu.Cleanup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a string returned in normalized form, decomposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsNameOnly()
		{
			string decomposed = "E\u0324\u0301PI\u0302TRE";
			string composed = StringUtils.Compose(decomposed);
			Assert.IsFalse(decomposed == composed);
			Assert.AreEqual("É\u0324PÎTRE", composed);

			composed = StringUtils.Compose("A\u030A\u0301");
			Assert.AreEqual("\u01FA", composed);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when there is an OwnNameGuidHot
		/// ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetOwnedGuidFromRun_WithOwnNameGuidHotORC()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);
			FwObjDataTypes odt;

			Guid returnGuid = StringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

			Assert.AreEqual(testGuid, returnGuid);
			Assert.AreEqual(FwObjDataTypes.kodtOwnNameGuidHot, odt);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when there is a
		/// GuidMoveableObjDisp ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetOwnedGuidFromRun_WithGuidMoveableObjDispORC()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtGuidMoveableObjDisp, 1);
			FwObjDataTypes odt;

			Guid returnGuid = StringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

			Assert.AreEqual(testGuid, returnGuid);
			Assert.AreEqual(FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests not :-) getting a Guid from a structured text string when there isn't an
		/// owned ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetOwnedGuidFromRun_ORCForUnownedObject()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtPictOdd, 1);
			FwObjDataTypes odt;

			Guid returnGuid = StringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

			Assert.AreEqual(Guid.Empty, returnGuid);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when the ORC is the type
		/// requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromRun_ORCMatchesSpecifiedType()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = StringUtils.GetGuidFromRun(tss, 0,
				FwObjDataTypes.kodtOwnNameGuidHot);

			Assert.AreEqual(testGuid, returnGuid);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting owned ORCs from a structured text string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetOwnedORCs_TssHasORCs()
		{
			// Create two owned ORCs
			Guid testGuid1 = Guid.NewGuid();
			Guid testGuid2 = Guid.NewGuid();
			ITsString tssORC1 = StringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = StringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsString tss;
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			tssBldr.ReplaceRgch(0, 0, "String start", 12, TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC1);
			tssBldr.ReplaceRgch(tssBldr.GetString().Length, tssBldr.GetString().Length, " middle", 7, TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC2);
			tssBldr.ReplaceRgch(tssBldr.GetString().Length, tssBldr.GetString().Length, " End", 4, TsPropsBldrClass.Create().GetTextProps());
			tss = tssBldr.GetString();
			Assert.AreEqual("String start" + StringUtils.kchObject + " middle" + StringUtils.kchObject + " End", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			// Test GetOwnedORCs
			ITsString orcTss = StringUtils.GetOwnedORCs(tss);

			// Confirm that the ORCs were returned correctly.
			Assert.AreEqual(2, orcTss.Length);
			Assert.AreEqual(StringUtils.kchObject.ToString() + StringUtils.kchObject.ToString(), orcTss.Text);
			Assert.AreEqual(testGuid1, StringUtils.GetGuidFromRun(orcTss, 0));
			Assert.AreEqual(testGuid2, StringUtils.GetGuidFromRun(orcTss, 1));
			int var;
			int ws = orcTss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);

			ws = orcTss.get_Properties(1).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing owned ORCs from a structured text string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedORCs_TssHasORCs()
		{
			// Create two owned ORCs
			Guid testGuid1 = Guid.NewGuid();
			Guid testGuid2 = Guid.NewGuid();
			ITsString tssORC1 = StringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = StringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsString tss;
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			tssBldr.ReplaceRgch(0, 0, "String start", 12, TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC1);
			tssBldr.ReplaceRgch(tssBldr.GetString().Length, tssBldr.GetString().Length, " middle", 7, TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC2);
			tssBldr.ReplaceRgch(tssBldr.GetString().Length, tssBldr.GetString().Length, " End", 4, TsPropsBldrClass.Create().GetTextProps());
			tss = tssBldr.GetString();
			Assert.AreEqual("String start" + StringUtils.kchObject + " middle" + StringUtils.kchObject + " End", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			// Test RemoveOwnedORCs
			ITsString noORCText = StringUtils.RemoveOwnedORCs(tss);

			// Confirm that the ORCs were removed.
			Assert.IsFalse(noORCText.Text.Contains(new string(StringUtils.kchObject, 1)));
			Assert.AreEqual("String start middle End", noORCText.Text);
			Assert.AreEqual(1, noORCText.RunCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests RemoveORCsAndStylesFromTSS when an ITsString begins and ends with spaces and
		/// contains an ORC. Just within the spaces are numbers and punctuation (TE-7795).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveORCsAndStylesFromTSS_NumbersWithinSpaces()
		{
			// Create two owned ORCs
			Guid testGuid1 = Guid.NewGuid();
			Guid testGuid2 = Guid.NewGuid();
			ITsString tssORC1 = StringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = StringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsString tss;
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			tssBldr.Replace(0, 0, " 55String start", TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC1);
			tssBldr.Replace(tssBldr.GetString().Length, tssBldr.GetString().Length, " middle", TsPropsBldrClass.Create().GetTextProps());
			tssBldr.ReplaceTsString(tssBldr.GetString().Length, tssBldr.GetString().Length, tssORC2);
			tssBldr.Replace(tssBldr.GetString().Length, tssBldr.GetString().Length, "End!22 ", TsPropsBldrClass.Create().GetTextProps());
			tss = tssBldr.GetString();
			Assert.AreEqual(" 55String start" + StringUtils.kchObject + " middle" + StringUtils.kchObject + "End!22 ", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			// Test RemoveOwnedORCs
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			ITsString tssORCsRemoved = StringUtils.RemoveORCsAndStylesFromTSS(tss, null, true, wsf);

			// We expect that the text would include the numbers, but not the leading and trailing spaces
			// nor the ORCs.
			Assert.AreEqual("55String start middleEnd!22", tssORCsRemoved.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests RemoveORCsAndStylesFromTSS when an ITsString consists of a single space (TE-7795).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveORCsAndStylesFromTSS_SingleSpace()
		{
			ITsStrFactory factory = TsStrFactoryClass.Create();
			ITsString tss = factory.MakeString(" ", 42);
			// Test RemoveOwnedORCs
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			Assert.AreEqual(0, StringUtils.RemoveORCsAndStylesFromTSS(tss, null, true, wsf).Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests RemoveORCsAndStylesFromTSS with a null ITsString (TE-8225).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveORCsAndStylesFromTSS_NullTsString()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			Assert.IsNull(StringUtils.RemoveORCsAndStylesFromTSS(null, null, false, wsf));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCharPropEngineAtOffset method when the ich is pointing to a newline
		/// character that is in the tss (which has a magic WS). (TE-8335)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCharPropEngineAtOffset_AtNewline()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			try
			{
				IWritingSystem ws = wsf.get_Engine("en");
				ITsStrBldr bldr = TsStrBldrClass.Create();
				bldr.Replace(0, 0, "This is my text", StyleUtils.CharStyleTextProps(null, ws.WritingSystem));
				bldr.Replace(4, 4, Environment.NewLine, StyleUtils.CharStyleTextProps(null, -1));
				Assert.IsNull(StringUtils.GetCharPropEngineAtOffset(bldr.GetString(),
					wsf, 4));
			}
			finally
			{
				// This is needed to prevent some memory read error after running the tests in
				// the GUI.
				wsf.Shutdown();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when the ORC is not the type
		/// requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromRun_ORCDoesNotMatchSpecifiedType()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = StringUtils.GetGuidFromRun(tss, 0,
				FwObjDataTypes.kodtGuidMoveableObjDisp);

			Assert.AreEqual(Guid.Empty, returnGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when there is an owning ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromRun_WithOwningORC()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = StringUtils.GetGuidFromRun(tss, 0);
			Assert.AreEqual(testGuid, returnGuid);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a Guid from a structured text string when there is a reference ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromRun_WithRefORC()
		{
			Guid testGuid = Guid.NewGuid();
			ITsString tss = StringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtNameGuidHot, 1);

			Guid returnGuid = StringUtils.GetGuidFromRun(tss, 0);
			Assert.AreEqual(testGuid, returnGuid);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests not :-) getting a Guid from a structured text string when there isn't any
		/// ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromRun_NoORC()
		{
			ITsStrFactory strFactory = TsStrFactoryClass.Create();
			ITsString tss = strFactory.MakeString("This string has no ORCS", 1);

			Guid returnGuid = StringUtils.GetGuidFromRun(tss, 0);
			Assert.AreEqual(Guid.Empty, returnGuid);
		}

		string TrimNonWordFormingChars(string test, ILgWritingSystemFactory wsf)
		{
			return StringUtils.TrimNonWordFormingChars(StringUtils.MakeTss(test, wsf.get_Engine("en").WritingSystem), wsf).Text;
		}
		string TrimNonWordFormingChars(string test, ILgWritingSystemFactory wsf, bool atStart, bool atEnd)
		{
			return StringUtils.TrimNonWordFormingChars(StringUtils.MakeTss(test, wsf.get_Engine("en").WritingSystem), wsf, atStart, atEnd).Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", wsf));
			Assert.AreEqual(null, TrimNonWordFormingChars(string.Empty, wsf));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel!", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars(" : angel!", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars(":angel!", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("!angel : ", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("1angel2", wsf));
			Assert.AreEqual("angel baby", TrimNonWordFormingChars("angel baby", wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars(
								"angel" + StringUtils.kchObject, wsf));
			Assert.AreEqual("angel\uFF40",
				TrimNonWordFormingChars("{angel\uFF40}", wsf));
			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from a character string when the
		/// tss contains a newline character (which has a magic WS). (TE-8335)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest_WithNewLine()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			try
			{
				IWritingSystem ws = wsf.get_Engine("en");
				ITsStrBldr bldr = TsStrBldrClass.Create();
				bldr.Replace(0, 0, "This is my text", StyleUtils.CharStyleTextProps(null, ws.WritingSystem));
				bldr.Replace(0, 0, Environment.NewLine, StyleUtils.CharStyleTextProps(null, -1));

				ITsString result = StringUtils.TrimNonWordFormingChars(bldr.GetString(), wsf);
				Assert.AreEqual("This is my text", result.Text);
			}
			finally
			{
				// This is needed to prevent some memory read error after running the tests in
				// the GUI.
				wsf.Shutdown();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from the start of a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest_AtStart()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", wsf, true, false));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars("angel!", wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars(" : angel!", wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars(":angel!", wsf, true, false));
			Assert.AreEqual("angel : ", TrimNonWordFormingChars("!angel : ", wsf, true, false));
			Assert.AreEqual("angel2", TrimNonWordFormingChars("1angel2", wsf, true, false));
			Assert.AreEqual("angel" + StringUtils.kchObject, TrimNonWordFormingChars(
								StringUtils.kchObject + "angel" + StringUtils.kchObject, wsf, true, false));
			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from the end of a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest_AtEnd()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			Assert.AreEqual("a", TrimNonWordFormingChars("a ", wsf, false, true));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", wsf, false, true));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", wsf, false, true));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel!", wsf, false, true));
			Assert.AreEqual(" : angel", TrimNonWordFormingChars(" : angel!", wsf, false, true));
			Assert.AreEqual(":angel", TrimNonWordFormingChars(":angel!", wsf, false, true));
			Assert.AreEqual("!angel", TrimNonWordFormingChars("!angel : ", wsf, false, true));
			Assert.AreEqual("1angel", TrimNonWordFormingChars("1angel2", wsf, false, true));
			Assert.AreEqual(StringUtils.kchObject + "angel", TrimNonWordFormingChars(
								StringUtils.kchObject + "angel" + StringUtils.kchObject, wsf, false, true));
			wsf.Shutdown();
		}

		bool FindWordFormInString(string wordForm, string source,
			ILgWritingSystemFactory wsf, out int ichMin, out int ichLim)
		{
			int ws = wsf.get_Engine("en").WritingSystem;
			ITsString tssWordForm = StringUtils.MakeTss(wordForm, ws);
			ITsString tssSource = StringUtils.MakeTss(source, ws);
			return StringUtils.FindWordFormInString(tssWordForm, tssSource, wsf, out ichMin, out ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method - basic test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_Basic()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// single word when it is the only thing in the string
			Assert.IsTrue(FindWordFormInString("Hello", "Hello", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word in the middle
			Assert.IsTrue(FindWordFormInString("hello", "Say hello to someone you know.", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(4, ichStart);
			Assert.AreEqual(9, ichEnd);

			// single word at the start
			Assert.IsTrue(FindWordFormInString("hello", "hello there", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word at the end
			Assert.IsTrue(FindWordFormInString("hello", "hey, hello", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(5, ichStart);
			Assert.AreEqual(10, ichEnd);

			// word does not exist
			Assert.IsFalse(FindWordFormInString("hello", "What? I can't hear you!", wsf, out ichStart, out ichEnd));

			// word does not match case
			Assert.IsFalse(FindWordFormInString("say", "Say hello to someone you know.", wsf, out ichStart, out ichEnd));

			// word occurs as the start of another word
			Assert.IsFalse(FindWordFormInString("me", "I meant to say hello.", wsf, out ichStart, out ichEnd));

			// word occurs as the end of another word
			Assert.IsFalse(FindWordFormInString("me", "I want to go home", wsf, out ichStart, out ichEnd));

			// word occurs in the middle of another word
			Assert.IsFalse(FindWordFormInString("me", "I say amen!", wsf, out ichStart, out ichEnd));

			// word occurs in the middle of another word, then later as a stand-alone word
			Assert.IsTrue(FindWordFormInString("me", "I say amen and me!", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(15, ichStart);
			Assert.AreEqual(17, ichEnd);

			// empty source string
			Assert.IsFalse(FindWordFormInString("me", string.Empty, wsf, out ichStart, out ichEnd));

			// empty word form string
			Assert.IsFalse(FindWordFormInString(string.Empty, "I say amen!", wsf, out ichStart, out ichEnd));

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the wordform contains punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_PunctuationInWordForm()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// word has word-forming "punctuation"
			Assert.IsTrue(FindWordFormInString("what's", "hello, what's your name?", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(7, ichStart);
			Assert.AreEqual(13, ichEnd);

			// wordform with non word-forming medial (the only kind allowed) punctuation
			Assert.IsTrue(FindWordFormInString("ngel-baby", "Hello there, @ngel-baby!", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(14, ichStart);
			Assert.AreEqual(23, ichEnd);

			// wordform with non-matching punctuation
			Assert.IsFalse(FindWordFormInString("ngel-baby", "Hello there, ngel=baby!", wsf, out ichStart, out ichEnd));

			// wordform with non-matching punctuation
			Assert.IsFalse(FindWordFormInString("ngel-baby", "Hello there, ngel-=-baby!", wsf, out ichStart, out ichEnd));

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the source contains punctuation next to
		/// the matching form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_PunctuationInSource()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// single word with punctuation at end of word
			Assert.IsTrue(FindWordFormInString("hello", "hello, I am fine", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word with punctuation at beginning of word
			Assert.IsTrue(FindWordFormInString("hello", "\"hello shmello,\" said Bill.", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual(6, ichEnd);

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the word form consists of multiple words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_MultipleWordWordForm()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// multiple words
			Assert.IsTrue(FindWordFormInString("hello there", "Well, hello there, who are you?", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(6, ichStart);
			Assert.AreEqual(17, ichEnd);

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the source strings that have different
		/// normalized forms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_DifferentNormalizedForms()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// searching for an accented E in a string that contains decomposed Unicode characters.
			Assert.IsTrue(FindWordFormInString("h\u00c9llo", "hE\u0301llo",
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(6, ichEnd);

			// searching for an accented E with decomposed Unicode characters in a string that has it composed.
			Assert.IsTrue(FindWordFormInString("hE\u0301llo", "h\u00c9llo",
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// searching for non-matching diacritics (decomposed).
			Assert.IsFalse(FindWordFormInString("hE\u0301llo", "hE\u0300llo",
				wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (composed).
			Assert.IsFalse(FindWordFormInString("h\u00c9llo", "hE\u00c8llo",
				wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (wordform composed, source decomposed).
			Assert.IsFalse(FindWordFormInString("h\u00c9llo", "hE\u0300llo",
				wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (wordform decomposed, source composed).
			Assert.IsFalse(FindWordFormInString("hE\u0300llo", "h\u00c9llo",
				wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (decomposed) at end of source.
			Assert.IsFalse(FindWordFormInString("hE\u0301", "I say hE\u0300",
				wsf, out ichStart, out ichEnd));

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the source string contains ORCs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_WithORCs()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			// single word with ORC at end of word (TE-3673)
			Assert.IsTrue(FindWordFormInString("hello", "hello" + StringUtils.kchObject,
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word with ORC embedded in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", "he" + StringUtils.kchObject + "llo",
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(6, ichEnd);

			// multiple embedded ORCs in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", "first words, then he" +
				StringUtils.kchObject + "ll" + StringUtils.kchObject + "o" + StringUtils.kchObject,
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(18, ichStart);
			Assert.AreEqual(25, ichEnd);

			// single word with multiple embedded ORCs in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", StringUtils.kchObject + "he" +
				StringUtils.kchObject + "ll" + StringUtils.kchObject + "o" + StringUtils.kchObject,
				wsf, out ichStart, out ichEnd));
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual(8, ichEnd);

			// multiple ORCs preceeding word
			Assert.IsTrue(FindWordFormInString("hello", StringUtils.kchObject + "first " +
				StringUtils.kchObject + "hello world", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(8, ichStart);
			Assert.AreEqual(13, ichEnd);

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method when the source string contains multiple
		/// occurrences of the word form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_MultipleMatches()
		{
			int ichStart, ichEnd;
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			Assert.IsTrue(FindWordFormInString("hello", "Say hello to someone who said hello to you.", wsf, out ichStart, out ichEnd));
			Assert.AreEqual(4, ichStart);
			Assert.AreEqual(9, ichEnd);

			wsf.Shutdown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a text prop type other than
		/// FwTextPropType.ktptObjData.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_WrongTextPropType()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);

			Assert.IsFalse(StringUtils.WriteHref(-56, new string(new char[] {
				Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), 'a', 'b', 'c'}),
				writer));
			Assert.AreEqual(String.Empty, stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed string prop with no URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NoURL()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);

			Assert.IsFalse(StringUtils.WriteHref((int)FwTextPropType.ktptObjData, new string(
				Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), 1),
				writer));
			Assert.AreEqual(String.Empty, stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a string prop whose first character is not
		/// FwObjDataTypes.kodtExternalPathName.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_WrongStringPropObjDataType()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);

			Assert.IsFalse(StringUtils.WriteHref((int)FwTextPropType.ktptObjData, "abc", writer));
			Assert.AreEqual(String.Empty, stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a null string prop.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NullStringProp()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);

			Assert.IsFalse(StringUtils.WriteHref((int)FwTextPropType.ktptObjData, null, writer));
			Assert.AreEqual(String.Empty, stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a file URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_File()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.WriteStartElement("span");

			StringBuilder strBldr = new StringBuilder("c:\\autoexec.bat");
			strBldr.Insert(0, Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName));

			Assert.IsTrue(StringUtils.WriteHref((int)FwTextPropType.ktptObjData,
				strBldr.ToString(), writer));
			writer.WriteEndElement();

			Assert.AreEqual("<span href=\"file://c:/autoexec.bat\" />", stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a normal URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NormalURL()
		{
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.WriteStartElement("span");

			StringBuilder strBldr = new StringBuilder("http://www.myspace.com");
			strBldr.Insert(0, Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName));

			Assert.IsTrue(StringUtils.WriteHref((int)FwTextPropType.ktptObjData,
				strBldr.ToString(), writer));
			writer.WriteEndElement();

			Assert.AreEqual("<span href=\"http://www.myspace.com\" />", stream.ToString());
		}

		/// <summary>
		/// Test the GetDiffsInTsStrings method.
		/// </summary>
		[Test]
		public void FindStringDiffs()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tssEmpty1 = tsf.MakeString("", 1);
			VerifyStringDiffs(tssEmpty1, tssEmpty1, true, -1, 0, 0, "empty string equals itself");
			ITsString tssAbc1 = tsf.MakeString("abc", 1);
			VerifyStringDiffs(tssAbc1, tssAbc1, true, -1, 0, 0, "one-run string equals itself");
			VerifyStringDiffs(tssEmpty1, tssAbc1, false, 0, 3, 0, "added 3 chars to empty string");
			ITsString tssEmpty2 = tsf.MakeString("", 2);
			VerifyStringDiffs(tssEmpty1, tssEmpty2, false, 0, 0, 0, "two empty strings in different wss are not equal");
			ITsString tssAbc2 = tsf.MakeString("abc", 2);
			VerifyStringDiffs(tssAbc1, tssAbc2, false, 0, 3, 3, "two non-empty strings in different wss are not equal");
			ITsString tssAbc1b = tsf.MakeString("abc", 1);
			VerifyStringDiffs(tssAbc1, tssAbc1b, true, -1, 0, 0, "one-run string equals an identical string");

			ITsTextProps props1 = StringUtils.PropsForWs(1);
			ITsTextProps props2 = StringUtils.PropsForWs(2);
			ITsTextProps props3 = StringUtils.PropsForWs(3);

			ITsStrBldr bldr = tssAbc1.GetBldr();
			bldr.Replace(3, 3, "def", props2);
			ITsString tssAbc1Def2 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2, tssAbc1Def2, true, -1, 0, 0, "two-run string equals itself");
			VerifyStringDiffs(tssAbc1Def2, tssAbc1Def2.GetBldr().GetString(), true, -1, 0, 0, "two-run string equals identical string");
			VerifyStringDiffs(tssAbc1Def2, tssAbc1, false, 3, 0, 3, "two-run string shortened to one-run");
			VerifyStringDiffs(tssAbc1, tssAbc1Def2, false, 3, 3, 0, "one-run string added second run");

			ITsString tssAbd1 = tsf.MakeString("abd", 1);
			VerifyStringDiffs(tssAbc1, tssAbd1, false, 2, 1, 1, "one-run string different last character");
			ITsString tssAb1 = tsf.MakeString("ab", 1);
			VerifyStringDiffs(tssAbc1, tssAb1, false, 2, 0, 1, "one-run string remove last character");
			VerifyStringDiffs(tssAb1, tssAbc1, false, 2, 1, 0, "one-run string add last character");

			bldr = tssAbc1Def2.GetBldr();
			bldr.Replace(6, 6, "ghi", props1);
			ITsString tssAbc1Def2Ghi1 = bldr.GetString();

			bldr = tssAbc1Def2Ghi1.GetBldr();
			bldr.SetProperties(3, 6, props3);
			ITsString tssAbc1Def3Ghi1 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi1, tssAbc1Def3Ghi1, false, 3, 3, 3, "three-run string differs by middle props");

			VerifyStringDiffs(tssAbc1Def2, tssAbc1Def2Ghi1, false, 6, 3, 0, "two-run string added run at end");
			VerifyStringDiffs(tssAbc1Def2Ghi1, tssAbc1Def2, false, 6, 0, 3, "three-run string deleted run at end");

			bldr = tssAbc1Def2Ghi1.GetBldr();
			bldr.SetProperties(6, 9, props3);
			ITsString tssAbc1Def2Ghi3 = bldr.GetString();
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(3, 6, null, null);
			ITsString tssAbc1Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Ghi3, tssAbc1Def2Ghi3, false, 3, 3, 0, "two-run string added run middle");
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Ghi3, false, 3, 0, 3, "three-run string deleted run middle");

			VerifyStringDiffs(tssAbc1, tssAbc1Def2Ghi3, false, 3, 6, 0, "one-run string added two runs end");
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1, false, 3, 0, 6, "three-run string deleted last two runs");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(0, 3, null, null);
			ITsString tssDef2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssDef2Ghi3, tssAbc1Def2Ghi3, false, 0, 3, 0, "two-run string added run start");
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssDef2Ghi3, false, 0, 0, 3, "three-run string deleted run start");

			ITsString tssAxc1 = tsf.MakeString("axc", 1);
			VerifyStringDiffs(tssAbc1, tssAxc1, false, 1, 1, 1, "one-run string different mid character");
			ITsString tssAc1 = tsf.MakeString("ac", 1);
			VerifyStringDiffs(tssAbc1, tssAc1, false, 1, 0, 1, "one-run string remove mid character");
			VerifyStringDiffs(tssAc1, tssAbc1, false, 1, 1, 0, "one-run string add mid character");

			ITsString tssXbc1 = tsf.MakeString("xbc", 1);
			VerifyStringDiffs(tssAbc1, tssXbc1, false, 0, 1, 1, "one-run string different first character");
			ITsString tssBc1 = tsf.MakeString("bc", 1);
			VerifyStringDiffs(tssAbc1, tssBc1, false, 0, 0, 1, "one-run string remove first character");
			VerifyStringDiffs(tssBc1, tssAbc1, false, 0, 1, 0, "one-run string add first character");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(0, 1, "x", null);
			ITsString tssXbc1Def2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssXbc1Def2Ghi3, false, 0, 1, 1, "three-run string different first character");
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(0, 1, "", null);
			ITsString tssBc1Def2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssBc1Def2Ghi3, false, 0, 0, 1, "three-run string delete first character");
			VerifyStringDiffs(tssBc1Def2Ghi3, tssAbc1Def2Ghi3, false, 0, 1, 0, "three-run string insert first character");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(8, 9, "x", null);
			ITsString tssAbc1Def2Ghx3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Def2Ghx3, false, 8, 1, 1, "three-run string different last character");
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(8, 9, "", null);
			ITsString tssAbc1Def2Gh3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Def2Gh3, false, 8, 0, 1, "three-run string delete last character");
			VerifyStringDiffs(tssAbc1Def2Gh3, tssAbc1Def2Ghi3, false, 8, 1, 0, "three-run string insert last character");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(4, 5, "x", null);
			ITsString tssAbc1Dxf2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Dxf2Ghi3, false, 4, 1, 1, "three-run string different mid character");
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(4, 5, "", null);
			ITsString tssAbc1Df2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Df2Ghi3, false, 4, 0, 1, "three-run string delete mid character");
			VerifyStringDiffs(tssAbc1Df2Ghi3, tssAbc1Def2Ghi3, false, 4, 1, 0, "three-run string insert mid character");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(3, 4, "x", null);
			ITsString tssAbc1Xef2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Xef2Ghi3, false, 3, 1, 1, "three-run string replace first char of mid run");
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(3, 4, "", null);
			ITsString tssAbc1Ef2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Ef2Ghi3, false, 3, 0, 1, "three-run string delete first char of mid run");
			VerifyStringDiffs(tssAbc1Ef2Ghi3, tssAbc1Def2Ghi3, false, 3, 1, 0, "three-run string insert first char of mid run");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(5, 6, "x", null);
			ITsString tssAbc1Dex2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Dex2Ghi3, false, 5, 1, 1, "three-run string replace last char of mid run");
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(5, 6, "", null);
			ITsString tssAbc1De2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1De2Ghi3, false, 5, 0, 1, "three-run string delete last char of mid run");
			VerifyStringDiffs(tssAbc1De2Ghi3, tssAbc1Def2Ghi3, false, 5, 1, 0, "three-run string insert last char of mid run");

			// Different numbers of runs, part of each border run the same.
			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(4, 5, "x", null);
			bldr.Replace(6,6, "xyz", props1);
			bldr.Replace(9, 9, "xyf", props2);
			ITsString tssAbc1Dxf2Xyz1Xyf2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Dxf2Xyz1Xyf2Ghi3, false, 4, 7, 1, "three-run string replace runs and text mid");
			VerifyStringDiffs(tssAbc1Dxf2Xyz1Xyf2Ghi3, tssAbc1Def2Ghi3, false, 4, 1, 7, "five-run string replace runs and text mid");

			VerifyStringDiffs(tssAbc1Def2Ghi3, tssXbc1, false, 0, 3, 9, "three-run string replace all one run");
			VerifyStringDiffs(tssXbc1, tssAbc1Def2Ghi3, false, 0, 9, 3, "one-run string replace all three runs");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(5, 9, "x", null);
			ITsString tssAbc1Dex2 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssAbc1Dex2, false, 5, 1, 4, "three-run string replace last and part of mid");
			VerifyStringDiffs(tssAbc1Dex2, tssAbc1Def2Ghi3, false, 5, 4, 1, "two-run string replace text and add run at end");

			bldr = tssAbc1Def2Ghi3.GetBldr();
			bldr.Replace(0, 4, "", null);
			ITsString tssEf2Ghi3 = bldr.GetString();
			VerifyStringDiffs(tssAbc1Def2Ghi3, tssEf2Ghi3, false, 0, 0, 4, "three-run string delete first and part of mid");
			VerifyStringDiffs(tssEf2Ghi3, tssAbc1Def2Ghi3, false, 0, 4, 0, "two-run string insert run and text at start");
		}

		void VerifyStringDiffs(ITsString tss1, ITsString tss2, bool fEqual, int ichMinEx, int cchInsEx, int cchDelEx, string id)
		{
			int ichMin, cchIns, cchDel;
			Assert.AreEqual(fEqual, StringUtils.GetDiffsInTsStrings(tss1, tss2, out ichMin, out cchIns, out cchDel), id + " result");
			Assert.AreEqual(ichMinEx, ichMin, id + " ichMin");
			Assert.AreEqual(cchInsEx, cchIns, id + " cchIns");
			Assert.AreEqual(cchDelEx, cchDel, id + " cchDel");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single line separator character
		/// (U+2028).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleLineSeparator()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccZl, typeof(int));
			Assert.AreEqual("\u2028", StringUtils.ValidateCharacterSequence("\u2028", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single space character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleSpace()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccZs, typeof(int));
			Assert.AreEqual(" ", StringUtils.ValidateCharacterSequence(" ", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single format (other) character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleFormatCharacter()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccCf, typeof(int));
			Assert.AreEqual("\u200c", StringUtils.ValidateCharacterSequence("\u200c", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single word-forming character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleLetter()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccLl, typeof(int));
			cpe.SetupResult("get_IsLetter", true, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("c", StringUtils.ValidateCharacterSequence("c", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single numeric character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleNumber()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccLl, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", true, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("2", StringUtils.ValidateCharacterSequence("2", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single PUA character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SinglePUA()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccCo, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("\uE000", StringUtils.ValidateCharacterSequence("\uE000", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single undefined character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleUndefinedChar()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccCn, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual(string.Empty, StringUtils.ValidateCharacterSequence("\uE000", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single punctuation character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SinglePunctuation()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccPs, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", true, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("(", StringUtils.ValidateCharacterSequence("(", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single symbol character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleSymbol()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccSc, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("$", StringUtils.ValidateCharacterSequence("$", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a letter with a diacritic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_BaseCharacterPlusDiacritic()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'n');
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x0301);
			cpe.SetupResultForParams("get_IsLetter", true, (int)'n');
			cpe.SetupResultForParams("get_IsLetter", false, 0x0301);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResultForParams("get_IsMark", false, (int)'n');
			cpe.SetupResultForParams("get_IsMark", true, 0x0301);
			Assert.AreEqual("n\u0301", StringUtils.ValidateCharacterSequence("n\u0301", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a letter with three diacritics (Hebrew).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_BaseCharacterPlusMultipleDiacritics()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, 0x05E9);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05C1);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05B4);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x0596);
			cpe.SetupResultForParams("get_IsLetter", true, 0x05E9);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05C1);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05B4);
			cpe.SetupResultForParams("get_IsLetter", false, 0x0596);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResultForParams("get_IsMark", false, 0x05E9);
			cpe.SetupResultForParams("get_IsMark", true, 0x05C1);
			cpe.SetupResultForParams("get_IsMark", true, 0x05B4);
			cpe.SetupResultForParams("get_IsMark", true, 0x0596);
			Assert.AreEqual("\u05E9\u05C1\u05B4\u0596",
				StringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u0596", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single diacritic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_SingleDiacritic()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccMn, typeof(int));
			cpe.SetupResult("get_IsLetter", false, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", true, typeof(int));
			Assert.AreEqual(string.Empty, StringUtils.ValidateCharacterSequence("\u0301", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of two word-forming base characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_MultipleLetters()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'n');
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'o');
			cpe.SetupResultForParams("get_IsLetter", true, (int)'n');
			cpe.SetupResultForParams("get_IsLetter", true, (int)'o');
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("n", StringUtils.ValidateCharacterSequence("no", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a single diacritic followed by a letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_DiacriticBeforeLetter()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, 0x0301);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'o');
			cpe.SetupResultForParams("get_IsLetter", false, 0x0301);
			cpe.SetupResultForParams("get_IsLetter", true, (int)'o');
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResultForParams("get_IsMark", true, 0x0301);
			cpe.SetupResultForParams("get_IsMark", false, (int)'o');
			Assert.AreEqual("o", StringUtils.ValidateCharacterSequence("\u0301o", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of three (Korean) base characters that can
		/// be composed to form a single (syllabic) base character (U+AC10).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_MultipleBaseCharsThatComposeIntoASingleBaseChar()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1100);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1161);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x11B7);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0xAC10);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1100);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1161);
			cpe.SetupResultForParams("get_IsLetter", true, 0x11B7);
			cpe.SetupResultForParams("get_IsLetter", true, 0xAC10);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			Assert.AreEqual("\uAC10",
				StringUtils.ValidateCharacterSequence("\u1100\u1161\u11B7", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a (Korean) character sequence consisting of a single (syllabic) base
		/// character that can be decomposed to form three (phonemic) base characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsValidChar_SingleBaseCharThatDecomposesIntoMultipleBaseChars()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1100);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1161);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x11B7);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0xAC10);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1100);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1161);
			cpe.SetupResultForParams("get_IsLetter", true, 0x11B7);
			cpe.SetupResultForParams("get_IsLetter", true, 0xAC10);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			cpe.SetupResultForParams("NormalizeD", "\u1100\u1161\u11B7", "\uAC10");
			cpe.SetupResultForParams("NormalizeD", "\u1100\u1161\u11B7", "\u1100\u1161\u11B7");
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(StringUtils), "IsValidChar",
				"\uAC10", (LanguageDefinition)null, (ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of three (Korean) base characters that can
		/// be composed to form a single (syllabic) base character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsValidChar_MultipleBaseCharsThatComposeIntoASingleBaseChar()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1100);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x1161);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0x11B7);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLo, 0xAC10);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1100);
			cpe.SetupResultForParams("get_IsLetter", true, 0x1161);
			cpe.SetupResultForParams("get_IsLetter", true, 0x11B7);
			cpe.SetupResultForParams("get_IsLetter", true, 0xAC10);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			cpe.SetupResultForParams("NormalizeD", "\u1100\u1161\u11B7", "\uAC10");
			cpe.SetupResultForParams("NormalizeD", "\u1100\u1161\u11B7", "\u1100\u1161\u11B7");
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(StringUtils), "IsValidChar",
				"\u1100\u1161\u11B7", (LanguageDefinition)null, (ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a base character followed by multiple
		/// (Hebrew) diacritics joined by the zero-width joiner (U+200D).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_MultipleBaseCharsJoinedByZWJ()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, 0x05E9);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05C1);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05B4);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x0596);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccCf, 0x200D);
			cpe.SetupResultForParams("get_IsLetter", true, 0x05E9);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05C1);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05B4);
			cpe.SetupResultForParams("get_IsLetter", false, 0x0596);
			cpe.SetupResultForParams("get_IsLetter", false, 0x200D);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResultForParams("get_IsMark", false, 0x05E9);
			cpe.SetupResultForParams("get_IsMark", true, 0x05C1);
			cpe.SetupResultForParams("get_IsMark", true, 0x05B4);
			cpe.SetupResultForParams("get_IsMark", true, 0x0596);
			cpe.SetupResultForParams("get_IsMark", false, 0x200D);
			Assert.AreEqual("\u05E9\u05C1\u05B4\u200D\u0596",
				StringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u200D\u0596", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates a character sequence consisting of a base character followed by multiple
		/// (Hebrew) diacritics joined by the zero-width non-joiner (U+200C).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_MultipleBaseCharsJoinedByZWNJ()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, 0x05E9);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05C1);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x05B4);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x0596);
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccCf, 0x200C);
			cpe.SetupResultForParams("get_IsLetter", true, 0x05E9);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05C1);
			cpe.SetupResultForParams("get_IsLetter", false, 0x05B4);
			cpe.SetupResultForParams("get_IsLetter", false, 0x0596);
			cpe.SetupResultForParams("get_IsLetter", false, 0x200C);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", true, typeof(int));
			cpe.SetupResultForParams("get_IsMark", false, 0x05E9);
			cpe.SetupResultForParams("get_IsMark", true, 0x05C1);
			cpe.SetupResultForParams("get_IsMark", true, 0x05B4);
			cpe.SetupResultForParams("get_IsMark", true, 0x0596);
			cpe.SetupResultForParams("get_IsMark", false, 0x200C);
			Assert.AreEqual("\u05E9\u05C1\u05B4\u200C\u0596",
				StringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u200C\u0596", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Zero-width Non-joiner (U+200C)character is considered valid. TE-8318
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_AllowZwnj()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccCf, 0x200C);
			cpe.SetupResultForParams("get_IsLetter", false, 0x200C);
			cpe.SetupResultForParams("get_IsNumber", false, 0x200C);
			cpe.SetupResultForParams("get_IsPunctuation", false, 0x200C);
			cpe.SetupResultForParams("get_IsSymbol", true, 0x200C);
			cpe.SetupResultForParams("get_IsMark", false, 0x200C);
			Assert.AreEqual("\u200C",
				StringUtils.ValidateCharacterSequence("\u200C", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Zero-width Joiner (U+200D) character is considered valid. TE-8318
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ValidateCharacterSequence_AllowZwnjAndZwj()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccCf, 0x200D);
			cpe.SetupResultForParams("get_IsLetter", false, 0x200D);
			cpe.SetupResultForParams("get_IsNumber", false, 0x200D);
			cpe.SetupResultForParams("get_IsPunctuation", false, 0x200D);
			cpe.SetupResultForParams("get_IsSymbol", true, 0x200D);
			cpe.SetupResultForParams("get_IsMark", false, 0x200D);
			Assert.AreEqual("\u200D",
				StringUtils.ValidateCharacterSequence("\u200D", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when passed a simple string of space-
		/// delimited letters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_Simple()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResult("get_GeneralCategory", LgGeneralCharCategory.kccLl, typeof(int));
			cpe.SetupResult("get_IsLetter", true, typeof(int));
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			List<string> invalidChars;
			List<string> validChars = StringUtils.ParseCharString("a b c", " ", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance, out invalidChars);
			Assert.AreEqual(3, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("b", validChars[1]);
			Assert.AreEqual("c", validChars[2]);
			Assert.AreEqual(0, invalidChars.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when passed a simple string of space-
		/// delimited letters that also has a leading space.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_LeadingSpace()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'a');
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccLl, (int)'b');
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccZs, (int)' ');
			cpe.SetupResultForParams("get_IsLetter", true, (int)'a');
			cpe.SetupResultForParams("get_IsLetter", true, (int)'b');
			cpe.SetupResultForParams("get_IsLetter", false, (int)' ');
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResult("get_IsMark", false, typeof(int));
			List<string> invalidChars;
			List<string> validChars = StringUtils.ParseCharString("  a b", " ", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance, out invalidChars);
			Assert.AreEqual(3, validChars.Count);
			Assert.AreEqual(" ", validChars[0]);
			Assert.AreEqual("a", validChars[1]);
			Assert.AreEqual("b", validChars[2]);
			Assert.AreEqual(0, invalidChars.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when passed a string containing a single
		/// isolated diacritic.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		//[ExpectedException(ExceptionType = typeof(ArgumentException),
		//    ExpectedMessage = "The character \u0301 (U+0301) is not valid\r\nParameter name: chars")]
		public void ParseCharString_BogusCharacter()
		{
			DynamicMock cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			cpe.SetupResultForParams("get_GeneralCategory", LgGeneralCharCategory.kccMn, 0x0301);
			cpe.SetupResultForParams("get_IsLetter", false, 0x0301);
			cpe.SetupResult("get_IsNumber", false, typeof(int));
			cpe.SetupResult("get_IsPunctuation", false, typeof(int));
			cpe.SetupResult("get_IsSymbol", false, typeof(int));
			cpe.SetupResultForParams("get_IsMark", true, 0x0301);
			List<string> invalidChars;
			List<string> validChars = StringUtils.ParseCharString("\u0301", " ", null,
				(ILgCharacterPropertyEngine)cpe.MockInstance, out invalidChars);
			Assert.AreEqual(0, validChars.Count);
			Assert.AreEqual(1, invalidChars.Count);
			Assert.AreEqual("\u0301", invalidChars[0]);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when when passed a string of space-
		/// delimited letters that contains an illegal digraph
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_BogusDigraph()
		{
			DummyCharPropEngine cpe = new DummyCharPropEngine();
			List<string> invalidChars;
			List<string> validChars = StringUtils.ParseCharString("ch a b c", " ", null, cpe,
				out invalidChars);
			Assert.AreEqual(3, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("b", validChars[1]);
			Assert.AreEqual("c", validChars[2]);
			Assert.AreEqual(1, invalidChars.Count);
			Assert.AreEqual("ch", invalidChars[0]);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when passed a string of space-
		/// delimited letters that contains an illegal digraph in the mode where we ignore
		/// bogus characters (i.e. when we don't pass an empty list of invalid characters).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_IgnoreDigraph()
		{
			DummyCharPropEngine cpe = new DummyCharPropEngine();
			List<string> validChars = StringUtils.ParseCharString("ch a c", " ", cpe);
			Assert.AreEqual(2, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("c", validChars[1]);
		}
	}
}
