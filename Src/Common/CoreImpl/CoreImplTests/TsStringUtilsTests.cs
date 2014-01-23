// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StringUtilsTest.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using NUnit.Framework;
using SIL.Utils;
using System.IO;
using System.Text;	// for ILgWritingSystemFactory
using NMock;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TsStringUtils tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_DebugProces gets disposed in FixtureTeardown")]
	public class TsStringUtilsTests
	// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;
		private ILgWritingSystemFactory m_wsf;

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			m_wsf = new PalasoWritingSystemManager();
			m_DebugProcs = new DebugProcs();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
		}

		#endregion

		#region Get(Owned)GuidFromRun tests
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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);
			FwObjDataTypes odt;

			Guid returnGuid = TsStringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtGuidMoveableObjDisp, 1);
			FwObjDataTypes odt;

			Guid returnGuid = TsStringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtPictOddHot, 1);
			FwObjDataTypes odt;

			Guid returnGuid = TsStringUtils.GetOwnedGuidFromRun(tss, 0, out odt);

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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = TsStringUtils.GetGuidFromRun(tss, 0,
				FwObjDataTypes.kodtOwnNameGuidHot);

			Assert.AreEqual(testGuid, returnGuid);
			int var;
			int ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(1, ws);
		}
		#endregion

		#region ORC-handling tests
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
			ITsString tssORC1 = TsStringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = TsStringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsTextProps plainProps = StyleUtils.CharStyleTextProps(null, 1);
			tssBldr.ReplaceRgch(0, 0, "String start", 12, plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC1);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " middle", 7, plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC2);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " End", 4, plainProps);
			ITsString tss = tssBldr.GetString();
			Assert.AreEqual("String start" + StringUtils.kChObject + " middle" + StringUtils.kChObject + " End", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			// Test GetOwnedORCs
			ITsString orcTss = TsStringUtils.GetOwnedORCs(tss);

			// Confirm that the ORCs were returned correctly.
			Assert.AreEqual(2, orcTss.Length);
			Assert.AreEqual(StringUtils.kChObject.ToString() + StringUtils.kChObject, orcTss.Text);
			Assert.AreEqual(testGuid1, TsStringUtils.GetGuidFromRun(orcTss, 0));
			Assert.AreEqual(testGuid2, TsStringUtils.GetGuidFromRun(orcTss, 1));
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
		public void GetCleanTsString_TssHasORCs()
		{
			// Create two owned ORCs
			Guid testGuid1 = Guid.NewGuid();
			Guid testGuid2 = Guid.NewGuid();
			ITsString tssORC1 = TsStringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = TsStringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsString tss;
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsTextProps plainProps = StyleUtils.CharStyleTextProps(null, 1);
			tssBldr.ReplaceRgch(0, 0, "String start", 12, plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC1);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " middle", 7, plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC2);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " End", 4, plainProps);
			tss = tssBldr.GetString();
			Assert.AreEqual("String start" + StringUtils.kChObject + " middle" + StringUtils.kChObject + " End", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			// Test RemoveOwnedORCs
			ITsString noORCText = TsStringUtils.GetCleanTsString(tss, null);

			// Confirm that the ORCs were removed.
			Assert.IsFalse(noORCText.Text.Contains(new string(StringUtils.kChObject, 1)));
			Assert.AreEqual("String start middle End", noORCText.Text);
			Assert.AreEqual(1, noORCText.RunCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString when an ITsString begins and ends with spaces and
		/// contains an ORC. Just within the spaces are numbers and punctuation (TE-7795).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_NumbersWithinSpaces()
		{
			// Create two owned ORCs
			Guid testGuid1 = Guid.NewGuid();
			Guid testGuid2 = Guid.NewGuid();
			ITsString tssORC1 = TsStringUtils.CreateOrcFromGuid(testGuid1, FwObjDataTypes.kodtOwnNameGuidHot, 1);
			ITsString tssORC2 = TsStringUtils.CreateOrcFromGuid(testGuid2, FwObjDataTypes.kodtOwnNameGuidHot, 1);

			// Embed the ORCs in an ITsString
			ITsString tss;
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsTextProps plainProps = StyleUtils.CharStyleTextProps(null, 1);
			tssBldr.Replace(0, 0, " 55String start", plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC1);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, " middle", plainProps);
			tssBldr.ReplaceTsString(tssBldr.Length, tssBldr.Length, tssORC2);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, "End!22 ", plainProps);
			tss = tssBldr.GetString();
			Assert.AreEqual(" 55String start" + StringUtils.kChObject + " middle" + StringUtils.kChObject + "End!22 ", tss.Text);
			Assert.AreEqual(5, tss.RunCount);

			ITsString tssORCsRemoved = TsStringUtils.GetCleanTsString(tss, null);

			// We expect that the text would include the numbers, but not the leading and trailing spaces
			// nor the ORCs.
			Assert.AreEqual("55String start middleEnd!22", tssORCsRemoved.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString when an ITsString consists of a single space (TE-7795).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_SingleSpace()
		{
			ITsString tssClean = TsStringUtils.GetCleanTsString(TsStringUtils.MakeTss(" ", 42), null);
			Assert.AreEqual(0, tssClean.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString when an ITsString is initially empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_Empty()
		{
			ITsString tssClean = TsStringUtils.GetCleanTsString(
				TsStringUtils.MakeTss(String.Empty, 42), null);
			Assert.AreEqual(0, tssClean.Length);
			Assert.AreEqual(42, tssClean.get_WritingSystemAt(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString when an ITsString consists of a single ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_SingleUnknownORC_Remove()
		{
			ITsString tssClean = TsStringUtils.GetCleanTsString(
				TsStringUtils.MakeTss(StringUtils.kChObject.ToString(), 42), null);
			Assert.AreEqual(0, tssClean.Length);
			Assert.AreEqual(42, tssClean.get_WritingSystemAt(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString when an ITsString consists of a single ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_SingleUnknownORC_Preserve()
		{
			ITsString tss = TsStringUtils.MakeTss(StringUtils.kChObject.ToString(), 42);
			ITsString tssClean = TsStringUtils.GetCleanTsString(tss, null, false, true, false);
			Assert.AreEqual(1, tssClean.Length);
			Assert.AreEqual(StringUtils.kChObject, tssClean.get_RunText(0)[0]);
			Assert.AreEqual(42, tssClean.get_WritingSystemAt(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCleanTsString with a null ITsString (TE-8225).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCleanTsString_NullTsString()
		{
			Assert.IsNull(TsStringUtils.GetCleanTsString(null));
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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = TsStringUtils.GetGuidFromRun(tss, 0,
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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtOwnNameGuidHot, 1);

			Guid returnGuid = TsStringUtils.GetGuidFromRun(tss, 0);
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
			ITsString tss = TsStringUtils.CreateOrcFromGuid(testGuid,
				FwObjDataTypes.kodtNameGuidHot, 1);

			Guid returnGuid = TsStringUtils.GetGuidFromRun(tss, 0);
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

			Guid returnGuid = TsStringUtils.GetGuidFromRun(tss, 0);
			Assert.AreEqual(Guid.Empty, returnGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TurnOwnedOrcIntoUnownedOrc with a kodtOwnNameGuidHot ORC type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TurnOwnedOrcIntoUnownedOrc_OwnedOrc_Run0()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			TsStringUtils.InsertOrcIntoPara(expectedGuid, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, 5);
			TsStringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, 0);

			FwObjDataTypes odt;
			Guid guid = TsStringUtils.GetGuidFromProps(bldr.get_Properties(0), null, out odt);
			Assert.AreEqual(1, bldr.RunCount);
			Assert.AreEqual(StringUtils.kChObject.ToString(), bldr.Text);
			Assert.AreEqual(1, bldr.get_Properties(0).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(0).IntPropCount);
			Assert.AreEqual(expectedGuid, guid);
			Assert.AreEqual(FwObjDataTypes.kodtNameGuidHot, odt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TurnOwnedOrcIntoUnownedOrc with a kodtOwnNameGuidHot ORC type in a run
		/// greater than zero
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TurnOwnedOrcIntoUnownedOrc_OwnedOrc_DifferentRun()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "monkey", StyleUtils.CharStyleTextProps(null, 5));
			TsStringUtils.InsertOrcIntoPara(expectedGuid, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 6, 6, 5);
			TsStringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, 1);

			Assert.AreEqual(2, bldr.RunCount);
			Assert.AreEqual("monkey" + StringUtils.kChObject, bldr.Text);
			Assert.AreEqual(0, bldr.get_Properties(0).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(0).IntPropCount);

			FwObjDataTypes odt;
			Guid guid = TsStringUtils.GetGuidFromProps(bldr.get_Properties(1), null, out odt);
			Assert.AreEqual(1, bldr.get_Properties(1).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(1).IntPropCount);
			Assert.AreEqual(expectedGuid, guid);
			Assert.AreEqual(FwObjDataTypes.kodtNameGuidHot, odt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TurnOwnedOrcIntoUnownedOrc with a kodtNameGuidHot ORC type -- should be no
		/// change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TurnOwnedOrcIntoUnownedOrc_UnownedOrc()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			TsStringUtils.InsertOrcIntoPara(expectedGuid, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, 5);
			TsStringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, 0);

			FwObjDataTypes odt;
			Guid guid = TsStringUtils.GetGuidFromProps(bldr.get_Properties(0), null, out odt);
			Assert.AreEqual(1, bldr.RunCount);
			Assert.AreEqual(StringUtils.kChObject.ToString(), bldr.Text);
			Assert.AreEqual(1, bldr.get_Properties(0).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(0).IntPropCount);
			Assert.AreEqual(expectedGuid, guid);
			Assert.AreEqual(FwObjDataTypes.kodtNameGuidHot, odt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TurnOwnedOrcIntoUnownedOrc with a kodtGuidMoveableObjDisp ORC type -- need
		/// to decide what this should do, since for now we don't have an owned/unowned
		/// distinction for this kind of ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TurnOwnedOrcIntoUnownedOrc_PictureOrc()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			TsStringUtils.InsertOrcIntoPara(expectedGuid, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 0, 0, 5);
			TsStringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, 0);

			FwObjDataTypes odt;
			Guid guid = TsStringUtils.GetGuidFromProps(bldr.get_Properties(0), null, out odt);
			Assert.AreEqual(1, bldr.RunCount);
			Assert.AreEqual(StringUtils.kChObject.ToString(), bldr.Text);
			Assert.AreEqual(1, bldr.get_Properties(0).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(0).IntPropCount);
			Assert.AreEqual(expectedGuid, guid);
			Assert.AreEqual(FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TurnOwnedOrcIntoUnownedOrc on a run with no ORC -- should be no change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TurnOwnedOrcIntoUnownedOrc_NoOrc()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "test", StyleUtils.CharStyleTextProps(null, 5));
			TsStringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, 0);

			Assert.AreEqual(1, bldr.RunCount);
			Assert.AreEqual("test", bldr.Text);
			Assert.AreEqual(0, bldr.get_Properties(0).StrPropCount);
			Assert.AreEqual(1, bldr.get_Properties(0).IntPropCount);
		}
		#endregion

		#region Intprop-related tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RemoveIntProp to remove a font size property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveIntProp()
		{
			// Set up a ITsString with font property set.
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr tppBldr = TsPropsBldrClass.Create();
			tppBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 9250);
			tppBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 17);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, "This string has a font size property.", tppBldr.GetTextProps());
			ITsString tss = tssBldr.GetString();
			// Confirm that the ITsString has the font size property set.
			int value, nvar;
			tppBldr.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar, out value);
			Assert.AreEqual(17, value);
			Assert.IsTrue(FindIntPropInTss(tss, (int)FwTextPropType.ktptFontSize));

			ITsString newTss = TsStringUtils.RemoveIntProp(tss, (int)FwTextPropType.ktptFontSize);

			// Confirm that the ITsString has had the font size property removed.
			Assert.IsFalse(FindIntPropInTss(newTss, (int)FwTextPropType.ktptFontSize));
			// Confirm that the writing system property is still the same.
			newTss.get_PropertiesAt(0).GetBldr().GetIntPropValues((int)FwTextPropType.ktptWs, out nvar, out value);
			Assert.AreEqual(17, value);
			Assert.AreEqual("This string has a font size property.", newTss.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RemoveIntProp to remove a font size property when the string is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveIntProp_EmptyTss()
		{
			// Set up a ITsString with font property set.
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr tppBldr = TsPropsBldrClass.Create();
			tppBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 9250);
			tppBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 17);
			tssBldr.Replace(0, 0, string.Empty, tppBldr.GetTextProps());
			ITsString tss = tssBldr.GetString();
			// Confirm that the ITsString has the font size property set.
			int value, nvar;
			tppBldr.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar, out value);
			Assert.AreEqual(17, value);
			Assert.IsTrue(FindIntPropInTss(tss, (int)FwTextPropType.ktptFontSize));

			ITsString newTss = TsStringUtils.RemoveIntProp(tss, (int)FwTextPropType.ktptFontSize);

			// Confirm that the ITsString has had the font size property removed.
			Assert.IsFalse(FindIntPropInTss(newTss, (int)FwTextPropType.ktptFontSize));
			// Confirm that the writing system property is still the same.
			newTss.get_PropertiesAt(0).GetBldr().GetIntPropValues((int)FwTextPropType.ktptWs, out nvar, out value);
			Assert.AreEqual(17, value);
			Assert.IsNull(newTss.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified integer property is used in the specified ITsString.
		/// </summary>
		/// <param name="tss">The ITsString.</param>
		/// <param name="intProp"></param>
		/// <returns><c>true</c> if any run in the tss uses the specified property; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool FindIntPropInTss(ITsString tss, int intProp)
		{
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				// Check the integer properties of each run.
				ITsTextProps tpp = tss.get_PropertiesAt(iRun);

				for (int iProp = 0; iProp < tpp.IntPropCount; iProp++)
				{
					int var;
					int propType;
					tpp.GetIntProp(iProp, out propType, out var);
					if (propType == intProp)
						return true;
				}
			}
			return false;
		}
		#endregion

		#region TrimNonWordFormingChars tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest()
		{
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", m_wsf));
			Assert.AreEqual(null, TrimNonWordFormingChars(string.Empty, m_wsf));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel!", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars(" : angel!", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars(":angel!", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("!angel : ", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("1angel2", m_wsf));
			Assert.AreEqual("angel baby", TrimNonWordFormingChars("angel baby", m_wsf));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel" + StringUtils.kChObject, m_wsf));
			Assert.AreEqual("angel\uFF40", TrimNonWordFormingChars("{angel\uFF40}", m_wsf));
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
			ILgWritingSystem ws = m_wsf.get_Engine("en");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "This is my text", StyleUtils.CharStyleTextProps(null, ws.Handle));
			bldr.Replace(0, 0, Environment.NewLine, StyleUtils.CharStyleTextProps(null, -1));

			ITsString result = TsStringUtils.TrimNonWordFormingChars(bldr.GetString(), m_wsf);
			Assert.AreEqual("This is my text", result.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from the start of a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest_AtStart()
		{
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", m_wsf, true, false));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", m_wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars("angel!", m_wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars(" : angel!", m_wsf, true, false));
			Assert.AreEqual("angel!", TrimNonWordFormingChars(":angel!", m_wsf, true, false));
			Assert.AreEqual("angel : ", TrimNonWordFormingChars("!angel : ", m_wsf, true, false));
			Assert.AreEqual("angel2", TrimNonWordFormingChars("1angel2", m_wsf, true, false));
			Assert.AreEqual("angel" + StringUtils.kChObject, TrimNonWordFormingChars(
								StringUtils.kChObject + "angel" + StringUtils.kChObject, m_wsf, true, false));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that non-word forming characters are trimmed from the end of a character string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TrimNonWordFormingCharsTest_AtEnd()
		{
			Assert.AreEqual("a", TrimNonWordFormingChars("a ", m_wsf, false, true));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel", m_wsf, false, true));
			Assert.AreEqual(null, TrimNonWordFormingChars("123.90", m_wsf, false, true));
			Assert.AreEqual("angel", TrimNonWordFormingChars("angel!", m_wsf, false, true));
			Assert.AreEqual(" : angel", TrimNonWordFormingChars(" : angel!", m_wsf, false, true));
			Assert.AreEqual(":angel", TrimNonWordFormingChars(":angel!", m_wsf, false, true));
			Assert.AreEqual("!angel", TrimNonWordFormingChars("!angel : ", m_wsf, false, true));
			Assert.AreEqual("1angel", TrimNonWordFormingChars("1angel2", m_wsf, false, true));
			Assert.AreEqual(StringUtils.kChObject + "angel", TrimNonWordFormingChars(
								StringUtils.kChObject + "angel" + StringUtils.kChObject, m_wsf, false, true));
		}

		bool FindWordFormInString(string wordForm, string source,
			ILgWritingSystemFactory wsf, out int ichMin, out int ichLim)
		{
			int ws = wsf.get_Engine("en").Handle;
			ITsString tssWordForm = TsStringUtils.MakeTss(wordForm, ws);
			ITsString tssSource = TsStringUtils.MakeTss(source, ws);
			return TsStringUtils.FindWordFormInString(tssWordForm, tssSource, wsf, out ichMin, out ichLim);
		}
		#endregion

		#region FindWordFormInString tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordFormInString method - basic test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordFormInString_Basic()
		{
			int ichStart, ichEnd;
			// single word when it is the only thing in the string
			Assert.IsTrue(FindWordFormInString("Hello", "Hello", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word in the middle
			Assert.IsTrue(FindWordFormInString("hello", "Say hello to someone you know.", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(4, ichStart);
			Assert.AreEqual(9, ichEnd);

			// single word at the start
			Assert.IsTrue(FindWordFormInString("hello", "hello there", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word at the end
			Assert.IsTrue(FindWordFormInString("hello", "hey, hello", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(5, ichStart);
			Assert.AreEqual(10, ichEnd);

			// word does not exist
			Assert.IsFalse(FindWordFormInString("hello", "What? I can't hear you!", m_wsf, out ichStart, out ichEnd));

			// word does not match case
			Assert.IsFalse(FindWordFormInString("say", "Say hello to someone you know.", m_wsf, out ichStart, out ichEnd));

			// word occurs as the start of another word
			Assert.IsFalse(FindWordFormInString("me", "I meant to say hello.", m_wsf, out ichStart, out ichEnd));

			// word occurs as the end of another word
			Assert.IsFalse(FindWordFormInString("me", "I want to go home", m_wsf, out ichStart, out ichEnd));

			// word occurs in the middle of another word
			Assert.IsFalse(FindWordFormInString("me", "I say amen!", m_wsf, out ichStart, out ichEnd));

			// word occurs in the middle of another word, then later as a stand-alone word
			Assert.IsTrue(FindWordFormInString("me", "I say amen and me!", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(15, ichStart);
			Assert.AreEqual(17, ichEnd);

			// empty source string
			Assert.IsFalse(FindWordFormInString("me", string.Empty, m_wsf, out ichStart, out ichEnd));

			// empty word form string
			Assert.IsFalse(FindWordFormInString(string.Empty, "I say amen!", m_wsf, out ichStart, out ichEnd));
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
			// word has word-forming "punctuation"
			Assert.IsTrue(FindWordFormInString("what's", "hello, what's your name?", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(7, ichStart);
			Assert.AreEqual(13, ichEnd);

			// wordform with non word-forming medial (the only kind allowed) punctuation
			Assert.IsTrue(FindWordFormInString("ngel-baby", "Hello there, @ngel-baby!", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(14, ichStart);
			Assert.AreEqual(23, ichEnd);

			// wordform with non-matching punctuation
			Assert.IsFalse(FindWordFormInString("ngel-baby", "Hello there, ngel=baby!", m_wsf, out ichStart, out ichEnd));

			// wordform with non-matching punctuation
			Assert.IsFalse(FindWordFormInString("ngel-baby", "Hello there, ngel-=-baby!", m_wsf, out ichStart, out ichEnd));
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

			// single word with punctuation at end of word
			Assert.IsTrue(FindWordFormInString("hello", "hello, I am fine", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word with punctuation at beginning of word
			Assert.IsTrue(FindWordFormInString("hello", "\"hello shmello,\" said Bill.", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual(6, ichEnd);
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

			// multiple words
			Assert.IsTrue(FindWordFormInString("hello there", "Well, hello there, who are you?", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(6, ichStart);
			Assert.AreEqual(17, ichEnd);
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

			// searching for an accented E in a string that contains decomposed Unicode characters.
			Assert.IsTrue(FindWordFormInString("h\u00c9llo", "hE\u0301llo",
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(6, ichEnd);

			// searching for an accented E with decomposed Unicode characters in a string that has it composed.
			Assert.IsTrue(FindWordFormInString("hE\u0301llo", "h\u00c9llo",
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// searching for non-matching diacritics (decomposed).
			Assert.IsFalse(FindWordFormInString("hE\u0301llo", "hE\u0300llo",
				m_wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (composed).
			Assert.IsFalse(FindWordFormInString("h\u00c9llo", "hE\u00c8llo",
				m_wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (wordform composed, source decomposed).
			Assert.IsFalse(FindWordFormInString("h\u00c9llo", "hE\u0300llo",
				m_wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (wordform decomposed, source composed).
			Assert.IsFalse(FindWordFormInString("hE\u0300llo", "h\u00c9llo",
				m_wsf, out ichStart, out ichEnd));

			// searching for non-matching diacritics (decomposed) at end of source.
			Assert.IsFalse(FindWordFormInString("hE\u0301", "I say hE\u0300",
				m_wsf, out ichStart, out ichEnd));
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

			// single word with ORC at end of word (TE-3673)
			Assert.IsTrue(FindWordFormInString("hello", "hello" + StringUtils.kChObject,
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(5, ichEnd);

			// single word with ORC embedded in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", "he" + StringUtils.kChObject + "llo",
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(0, ichStart);
			Assert.AreEqual(6, ichEnd);

			// multiple embedded ORCs in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", "first words, then he" +
				StringUtils.kChObject + "ll" + StringUtils.kChObject + "o" + StringUtils.kChObject,
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(18, ichStart);
			Assert.AreEqual(25, ichEnd);

			// single word with multiple embedded ORCs in word (TE-5309)
			Assert.IsTrue(FindWordFormInString("hello", StringUtils.kChObject + "he" +
				StringUtils.kChObject + "ll" + StringUtils.kChObject + "o" + StringUtils.kChObject,
				m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual(8, ichEnd);

			// multiple ORCs preceeding word
			Assert.IsTrue(FindWordFormInString("hello", StringUtils.kChObject + "first " +
				StringUtils.kChObject + "hello world", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(8, ichStart);
			Assert.AreEqual(13, ichEnd);
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

			Assert.IsTrue(FindWordFormInString("hello", "Say hello to someone who said hello to you.", m_wsf, out ichStart, out ichEnd));
			Assert.AreEqual(4, ichStart);
			Assert.AreEqual(9, ichEnd);
		}
		#endregion

		#region FindWordBoundary tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when ich is negative or greater than the length
		/// of the string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_IchOutOfRange()
		{
			ITsString tss = TsStringUtils.MakeTss("funky munky", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => tss.FindWordBoundary(4000, cpe));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => tss.FindWordBoundary(-1, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when the character property engine is null
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_NullCharacterPropertyEngine()
		{
			ITsString tss = TsStringUtils.MakeTss("funky munky", m_wsf.UserWs);
			Assert.Throws(typeof(ArgumentNullException), () => tss.FindWordBoundary(0, null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when already at the start of a word
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AlreadyAtStartOfWord()
		{
			ITsString tss = TsStringUtils.MakeTss("A munky", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(2, tss.FindWordBoundary(2, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when at the start of the string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AtStartOfString()
		{
			ITsString tss = TsStringUtils.MakeTss("Another munky", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when at the end of the string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AtEndOfString()
		{
			ITsString tss = TsStringUtils.MakeTss("One guy", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(7, tss.FindWordBoundary(7, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when in the middle of a word
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_MiddleOfWord()
		{
			ITsString tss = TsStringUtils.MakeTss("Happiness is good.", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(4, cpe));
			Assert.AreEqual(13, tss.FindWordBoundary(tss.Length - 3, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when at the end of a word (in the middle of the
		/// string)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_EndOfWord()
		{
			ITsString tss = TsStringUtils.MakeTss("Gold is good.", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(5, tss.FindWordBoundary(4, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around punctuation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundPunctuation()
		{
			ITsString tss = TsStringUtils.MakeTss("God 'is good.'", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(tss.Length, tss.FindWordBoundary(tss.Length - 2, cpe));
			Assert.AreEqual(tss.Length, tss.FindWordBoundary(tss.Length - 1, cpe));
			Assert.AreEqual(tss.Length, tss.FindWordBoundary(tss.Length, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when at the end of a sentence (before sentence-
		/// ending punctuation)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_EndOfSentence()
		{
			ITsString tss = TsStringUtils.MakeTss("Good. Yeah!", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(6, tss.FindWordBoundary(4, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundNumbers()
		{
			ITsString tss = TsStringUtils.MakeTss("Gideon had 300 men.", m_wsf.UserWs);
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(11, tss.FindWordBoundary(11, cpe));
			Assert.AreEqual(11, tss.FindWordBoundary(12, cpe));
			Assert.AreEqual(15, tss.FindWordBoundary(14, cpe));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around a valid chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundChapterNumber_Valid()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "12", StyleUtils.CharStyleTextProps("Chap Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Chap Num"), "Failed to find position following chapter number when ich == 0");
			for (int ich = 1; ich < 4; ich++)
				Assert.AreEqual(2, tss.FindWordBoundary(ich, cpe, "Chap Num"), "Failed to find position following chapter number when ich == " + ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when before a valid chapter number that is not at
		/// the start of the string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_BeforeChapterNumber_MidString()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Preceding text. ", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			int ichEndOfPrecedingText = bldr.Length;
			bldr.Replace(bldr.Length, bldr.Length, "2", StyleUtils.CharStyleTextProps("Chap Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Following text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(ichEndOfPrecedingText - 6, tss.FindWordBoundary(ichEndOfPrecedingText - 3, cpe, "Chap Num"));
			Assert.AreEqual(ichEndOfPrecedingText, tss.FindWordBoundary(ichEndOfPrecedingText - 2, cpe, "Chap Num"));
			Assert.AreEqual(ichEndOfPrecedingText, tss.FindWordBoundary(ichEndOfPrecedingText - 1, cpe, "Chap Num"));
			Assert.AreEqual(ichEndOfPrecedingText, tss.FindWordBoundary(ichEndOfPrecedingText, cpe, "Chap Num"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around an invalid chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundChapterNumber_Invalid()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "a2b", StyleUtils.CharStyleTextProps("Chap Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Chap Num"), "Failed to find position following invalid chapter number when ich == 0");
			for (int ich = 1; ich < 5; ich++)
				Assert.AreEqual(3, tss.FindWordBoundary(ich, cpe, "Chap Num"), "Failed to find position following invalid chapter number when ich == " + ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when bewteen a chapter number and a verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_BetweenChapterAndVerseNumbers()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "2", StyleUtils.CharStyleTextProps("Chap Num", m_wsf.UserWs));
			bldr.Replace(1, 1, "5", StyleUtils.CharStyleTextProps("Vers Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Chap Num", "Vers Num"));
			Assert.AreEqual(1, tss.FindWordBoundary(1, cpe, "Chap Num", "Vers Num"));
			Assert.AreEqual(2, tss.FindWordBoundary(2, cpe, "Chap Num", "Vers Num"));
			Assert.AreEqual(2, tss.FindWordBoundary(3, cpe, "Chap Num", "Vers Num"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around a valid simple verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundVerseNumber_Valid()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "51", StyleUtils.CharStyleTextProps("Vers Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Vers Num"), "Failed to find position following verse number when ich == 0");
			for (int ich = 1; ich < 4; ich++)
				Assert.AreEqual(2, tss.FindWordBoundary(ich, cpe, "Vers Num"), "Failed to find position following verse number when ich == " + ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around an invalid verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundVerseNumber_Invalid()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "a1b", StyleUtils.CharStyleTextProps("Vers Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Vers Num"), "Failed to find position following invalid verse number when ich == 0");
			for (int ich = 1; ich < 5; ich++)
				Assert.AreEqual(3, tss.FindWordBoundary(ich, cpe, "Vers Num"), "Failed to find position following invalid verse number when ich == " + ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindWordBoundary method when around a valid verse bridge
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWordBoundary_AroundVerseNumberBridge()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "5-8", StyleUtils.CharStyleTextProps("Vers Num", m_wsf.UserWs));
			bldr.Replace(bldr.Length, bldr.Length, "Some text", StyleUtils.CharStyleTextProps(null, m_wsf.UserWs));
			ITsString tss = bldr.GetString();
			ILgCharacterPropertyEngine cpe = m_wsf.get_CharPropEngine(m_wsf.UserWs);
			Assert.AreEqual(0, tss.FindWordBoundary(0, cpe, "Vers Num"), "Failed to find position following verse bridge when ich == 0");
			for (int ich = 1; ich < 5; ich++)
				Assert.AreEqual(3, tss.FindWordBoundary(ich, cpe, "Vers Num"), "Failed to find position following verse bridge when ich == " + ich);
		}
		#endregion

		#region WriteHref tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a text prop type other than
		/// FwTextPropType.ktptObjData.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_WrongTextPropType()
		{
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					Assert.IsFalse(TsStringUtils.WriteHref(-56, new string(new[] {
						Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), 'a', 'b', 'c'}),
						writer));
					Assert.AreEqual(String.Empty, stream.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed string prop with no URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NoURL()
		{
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					Assert.IsFalse(TsStringUtils.WriteHref((int)FwTextPropType.ktptObjData, new string(
						Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), 1),
						writer));
					Assert.AreEqual(String.Empty, stream.ToString());
				}
			}
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
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					Assert.IsFalse(TsStringUtils.WriteHref((int)FwTextPropType.ktptObjData, "abc", writer));
					Assert.AreEqual(String.Empty, stream.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a null string prop.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NullStringProp()
		{
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					Assert.IsFalse(TsStringUtils.WriteHref((int)FwTextPropType.ktptObjData, null, writer));
					Assert.AreEqual(String.Empty, stream.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a file URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_File()
		{
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					writer.WriteStartElement("span");

					StringBuilder strBldr = new StringBuilder("c:\\autoexec.bat");
					strBldr.Insert(0, Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName));

					Assert.IsTrue(TsStringUtils.WriteHref((int)FwTextPropType.ktptObjData,
						strBldr.ToString(), writer));
					writer.WriteEndElement();

					Assert.AreEqual("<span href=\"file://c:/autoexec.bat\" />", stream.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WriteHref method when passed a normal URL.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteHref_NormalURL()
		{
			using (var stream = new StringWriter())
			{
				using (var writer = new XmlTextWriter(stream))
				{
					writer.WriteStartElement("span");

					StringBuilder strBldr = new StringBuilder("http://www.myspace.com");
					strBldr.Insert(0, Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName));

					Assert.IsTrue(TsStringUtils.WriteHref((int)FwTextPropType.ktptObjData,
						strBldr.ToString(), writer));
					writer.WriteEndElement();

					Assert.AreEqual("<span href=\"http://www.myspace.com\" />", stream.ToString());
				}
			}
		}
		#endregion

		#region Valid Character tests
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
			Assert.AreEqual("\u2028", TsStringUtils.ValidateCharacterSequence("\u2028",
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
			Assert.AreEqual(" ", TsStringUtils.ValidateCharacterSequence(" ",
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
			Assert.AreEqual("\u200c", TsStringUtils.ValidateCharacterSequence("\u200c",
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
			Assert.AreEqual("c", TsStringUtils.ValidateCharacterSequence("c",
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
			Assert.AreEqual("2", TsStringUtils.ValidateCharacterSequence("2",
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
			Assert.AreEqual("\uE000", TsStringUtils.ValidateCharacterSequence("\uE000",
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
			Assert.AreEqual(string.Empty, TsStringUtils.ValidateCharacterSequence("\uE000",
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
			Assert.AreEqual("(", TsStringUtils.ValidateCharacterSequence("(",
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
			Assert.AreEqual("$", TsStringUtils.ValidateCharacterSequence("$",
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
			Assert.AreEqual("n\u0301", TsStringUtils.ValidateCharacterSequence("n\u0301",
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
				TsStringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u0596",
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
			Assert.AreEqual(string.Empty, TsStringUtils.ValidateCharacterSequence("\u0301",
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
			Assert.AreEqual("n", TsStringUtils.ValidateCharacterSequence("no",
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
			Assert.AreEqual("o", TsStringUtils.ValidateCharacterSequence("\u0301o",
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
				TsStringUtils.ValidateCharacterSequence("\u1100\u1161\u11B7",
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
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(TsStringUtils), "IsValidChar",
				"\uAC10", (ILgCharacterPropertyEngine)cpe.MockInstance));
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
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(TsStringUtils), "IsValidChar",
				"\u1100\u1161\u11B7", (ILgCharacterPropertyEngine)cpe.MockInstance));
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
				TsStringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u200D\u0596",
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
				TsStringUtils.ValidateCharacterSequence("\u05E9\u05C1\u05B4\u200C\u0596",
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
				TsStringUtils.ValidateCharacterSequence("\u200C", (ILgCharacterPropertyEngine)cpe.MockInstance));
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
				TsStringUtils.ValidateCharacterSequence("\u200D", (ILgCharacterPropertyEngine)cpe.MockInstance));
		}
		#endregion

		#region ParseCharString tests
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
			List<string> validChars = TsStringUtils.ParseCharString("a b c", " ",
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
			List<string> validChars = TsStringUtils.ParseCharString("  a b", " ",
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
		//[ExpectedException(typeof(ArgumentException),
		//	ExpectedMessage = "The character \u0301 (U+0301) is not valid\r\nParameter name: chars")]
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
			List<string> validChars = TsStringUtils.ParseCharString("\u0301", " ",
				(ILgCharacterPropertyEngine)cpe.MockInstance, out invalidChars);
			Assert.AreEqual(0, validChars.Count);
			Assert.AreEqual(1, invalidChars.Count);
			Assert.AreEqual("\u0301", invalidChars[0]);
		}
		#endregion

		#region Words tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TsString Words extension method when the TsString contains only one run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Words_OneRun()
		{
			ITsStrFactory fact = TsStrFactoryClass.Create();
			ITsString tss = fact.MakeString("   This is  some text.  ", 1);
			string[] expectedWords = new[] { "This", "is", "some", "text." };

			int i = 0;
			foreach (TsRunPart word in tss.Words())
				Assert.AreEqual(expectedWords[i++], word.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TsString Words extension method when the TsString contains multiple runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Words_MultipleRuns()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("   This  is", StyleUtils.CharStyleTextProps("Monkey", 1));
			bldr.Append("some text.  ", StyleUtils.CharStyleTextProps("Soup", 1));
			string[] expectedWords = new[] { "This", "is", "some", "text." };

			int i = 0;
			foreach (TsRunPart word in bldr.GetString().Words())
				Assert.AreEqual(expectedWords[i++], word.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TsString LastWord extension method when the TsString contains only one run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LastWord_OneRun()
		{
			ITsStrFactory fact = TsStrFactoryClass.Create();
			ITsString tss = fact.MakeString("  This is  some text. ", 1);
			Assert.AreEqual("text.", tss.LastWord().Text);
			tss = fact.MakeString("  text. ", 1);
			Assert.AreEqual("text.", tss.LastWord().Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TsString LastWord extension method when the TsString contains multiple runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LastWord_MultipleRuns()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("This is", StyleUtils.CharStyleTextProps("Monkey", 1));
			bldr.Append("text.", StyleUtils.CharStyleTextProps("Soup", 1));
			Assert.AreEqual("text.", bldr.GetString().LastWord().Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TsString LastWord extension method when the TsString ends with runs that
		/// are all whitespace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LastWord_TrailingSpaceInDifferentRun()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("This is", StyleUtils.CharStyleTextProps("Monkey", 1));
			bldr.Append("text.", StyleUtils.CharStyleTextProps("Soup", 1));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, 1));
			bldr.Append("     ", StyleUtils.CharStyleTextProps("Guppies", 1));
			Assert.AreEqual("text.", bldr.GetString().LastWord().Text);
		}
		#endregion

		#region Helper methods
		void VerifyStringDiffs(ITsString tss1, ITsString tss2, bool fEqual, int ichMinEx, int cchInsEx, int cchDelEx, string id)
		{
			int ichMin, cchIns, cchDel;
			TsStringDiffInfo diffInfo = TsStringUtils.GetDiffsInTsStrings(tss1, tss2);
			if (fEqual)
				Assert.IsNull(diffInfo);
			else
			{
				Assert.IsNotNull(diffInfo, id + " result");
				Assert.AreEqual(ichMinEx, diffInfo.IchFirstDiff, id + " ichMin");
				Assert.AreEqual(cchInsEx, diffInfo.CchInsert, id + " cchIns");
				Assert.AreEqual(cchDelEx, diffInfo.CchDeleteFromOld, id + " cchDel");
			}
		}

		private void VerifyConcatenate(String first, String second, String output)
		{
			var strFactory = TsStrFactoryClass.Create();
			var firstInput = strFactory.MakeString(first, 1);
			var secondInput = strFactory.MakeString(second, 1);

			Assert.AreEqual(output, firstInput.ConcatenateWithSpaceIfNeeded(secondInput).Text,
				"Concatenating '" + first + "' and '" + second + "' did not produce correct result.");
		}

		string TrimNonWordFormingChars(string test, ILgWritingSystemFactory wsf)
		{
			return TsStringUtils.TrimNonWordFormingChars(TsStringUtils.MakeTss(test, wsf.get_Engine("en").Handle), wsf).Text;
		}

		string TrimNonWordFormingChars(string test, ILgWritingSystemFactory wsf, bool atStart, bool atEnd)
		{
			return TsStringUtils.TrimNonWordFormingChars(TsStringUtils.MakeTss(test, wsf.get_Engine("en").Handle), wsf, atStart, atEnd).Text;
		}
		#endregion

		#region Misc tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the string stored as ObjData has the right length
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ObjDataCorrect()
		{
			Guid guid = Guid.NewGuid();
			byte[] objData = TsStringUtils.GetObjData(guid, (byte)'X');

			Assert.AreEqual(18, objData.Length);
			Assert.AreEqual((byte)'X', objData[0]);
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
			string composed = TsStringUtils.Compose(decomposed);
			Assert.IsFalse(decomposed == composed);
			Assert.AreEqual("\u00c9\u0324P\u00CETRE", composed);

			composed = TsStringUtils.Compose("A\u030A\u0301");
			Assert.AreEqual("\u01FA", composed);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an ITsString from an xml source. The source XML is in composed form. When
		/// creating the tss, the text should be decomposed. (FWR-148)
		/// </summary>
		/// <remarks>
		/// The TsStringUtils.GetTsString() method for converting from XML to an ITsString
		/// has been replaced by a new method on TsStringSerializer, as shown in the test below.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateTsStringFromXml()
		{
			const string threeRunString = "<Str><Run ws=\"en\" namedStyle=\"Chapter Number\">1</Run><Run ws=\"en\" namedStyle=\"Verse Number\">1</Run><Run ws=\"en\">Laa yra la m\u00E9n ne nak xpenkwlal Jesucrist nee ne z\u00EB\u00EBd xn\u00EBz rey David ne z\u00EB\u00EBd xn\u00EBz Abraham.</Run></Str>";
			// This works sans the chars with diacritics. var threeRunString = "<Str><Run ws=\"en\" namedStyle=\"Chapter Number\">1</Run><Run ws=\"en\" namedStyle=\"Verse Number\">1</Run><Run ws=\"en\">Laa yra la men ne nak xpenkwlal Jesucrist nee ne zeed xnez rey David ne zeed xnez Abraham.</Run></Str>";
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(threeRunString, m_wsf);
			Assert.AreEqual("11Laa yra la me\u0301n ne nak xpenkwlal Jesucrist nee ne ze\u0308e\u0308d xne\u0308z rey David ne ze\u0308e\u0308d xne\u0308z Abraham.",
				tss.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetDiffsInTsStrings method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

			ITsTextProps props1 = TsStringUtils.PropsForWs(1);
			ITsTextProps props2 = TsStringUtils.PropsForWs(2);
			ITsTextProps props3 = TsStringUtils.PropsForWs(3);

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
			bldr.Replace(6, 6, "xyz", props1);
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

			var s1 = tsf.MakeString("abc. def.", 1);
			var s2 = tsf.MakeString("abc. insert. def.", 1);
			VerifyStringDiffs(s1, s2, false, 5, 8, 0, "insert with dup material before and in insert");
			VerifyStringDiffs(s2, s1, false, 5, 0, 8, "delete with dup material before and at and of stuff deleted.");

			s1 = tsf.MakeString("xxxabc xxxdef.", 1);
			s2 = tsf.MakeString("xxxdef.", 1);
			VerifyStringDiffs(s1, s2, false, 0, 0, 7, "delete whole word ambiguous with delete part of two words");
			VerifyStringDiffs(s2, s1, false, 0, 7, 0, "insert whole word ambiguous with insert part of two words");

			s1 = tsf.MakeString("pus pus yalola.", 1);
			s2 = tsf.MakeString("pus yalola.", 1);
			VerifyStringDiffs(s1, s2, false, 4, 0, 4, "delete first word ambiguous with delete second word");
			VerifyStringDiffs(s2, s1, false, 4, 4, 0, "insert first word ambiguous with insert second words");

		}

		[Test]
		public void ConcatenateWithSpaceIfNeeded()
		{
			VerifyConcatenate("", "", null);
			VerifyConcatenate("A", "B", "A B");
			VerifyConcatenate("A ", "B", "A B");
			VerifyConcatenate("A", " B", "A B");
			VerifyConcatenate("A ", " B", "A  B");
			VerifyConcatenate("A", "", "A");
			VerifyConcatenate("", "B", "B");
			VerifyConcatenate("A\x3000", "B", "A\x3000B"); // ideographic space
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCharPropEngineAtOffset method when the ich is pointing to a newline
		/// character that is in the tss. (TE-8335)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCharPropEngineAtOffset_AtNewline()
		{
			ILgWritingSystem ws = m_wsf.get_Engine("en");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "This is my text", StyleUtils.CharStyleTextProps(null, ws.Handle));
			bldr.Replace(4, 4, Environment.NewLine, StyleUtils.CharStyleTextProps(null, -1));
			Assert.IsNull(TsStringUtils.GetCharPropEngineAtOffset(bldr.GetString(),
			m_wsf, 4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that result of GetXmlRep with a Empty TsString.
		/// Confirming that a ws info is produced
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetXmlRep_EmptyTss()
		{
			// Setup an empty TsString
			ITsString tssClean = TsStringUtils.GetCleanTsString(TsStringUtils.MakeTss(String.Empty, m_wsf.UserWs), null);
			Assert.AreEqual(null, tssClean.Text);
			Assert.AreEqual(1, tssClean.RunCount);
			Assert.AreEqual(m_wsf.UserWs, tssClean.get_WritingSystem(0));

			// Test method GetXmlRep
			string result = TsStringUtils.GetXmlRep(tssClean, m_wsf, 0, true); // 0 means Str not AStr

			// Confirm that the xml output has 'ws' information in it.
			Assert.AreEqual(String.Format("<Str>{0}<Run ws=\"en\"></Run>{0}</Str>{0}", Environment.NewLine), result);
		}

		/// <summary>
		/// Test various cases of TsStringUtils.RemoveIllegalXmlChars().
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Unit test")]
		public void RemoveIllegalXmlChars()
		{
			var tsf = TsStrFactoryClass.Create();
			var ws = m_wsf.UserWs;
			var empty = tsf.MakeString("", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(empty), Is.EqualTo(empty));
			var good = tsf.MakeString("good", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(good), Is.EqualTo(good));
			var controlChar = tsf.MakeString("ab\x001ecd", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(controlChar).Text, Is.EqualTo("abcd"));
			var twoBadChars = tsf.MakeString("\x000eabcde\x001f", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(twoBadChars).Text, Is.EqualTo("abcde"));
			var allBad = tsf.MakeString("\x0000\x0008\x000b\x000c\xfffe\xffff", ws);
			Assert.That(string.IsNullOrEmpty(TsStringUtils.RemoveIllegalXmlChars(allBad).Text));
			var goodSpecial = tsf.MakeString("\x0009\x000a\x000d \xfffd", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(goodSpecial), Is.EqualTo(goodSpecial));
			var badIsolatedLeadingSurrogate = tsf.MakeString("ab\xd800c\xdbff", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(badIsolatedLeadingSurrogate).Text, Is.EqualTo("abc"));
			var goodSurrogates = tsf.MakeString("\xd800\xdc00 \xdbff\xdfff", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(goodSurrogates), Is.EqualTo(goodSurrogates));
			var badIsolatedTrailingSurrogate = tsf.MakeString("\xdc00xy\xdcffz", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(badIsolatedTrailingSurrogate).Text, Is.EqualTo("xyz"));
			var outOfOrderSurrogates = tsf.MakeString("\xd800\xdc00\xdc00\xdbffz", ws);
			Assert.That(TsStringUtils.RemoveIllegalXmlChars(outOfOrderSurrogates).Text, Is.EqualTo("\xd800\xdc00z"));
		}

		#endregion
	}
}