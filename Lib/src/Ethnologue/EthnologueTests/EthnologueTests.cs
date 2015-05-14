// ---------------------------------------------------------------------------------------------
// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EthnologueTests.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.Ethnologue
{
	/// <summary>
	/// These tests test the Ethnologue class methods.
	/// </summary>
	[TestFixture]
	public class EthnologueTests
	{
		/// <summary>
		/// Test the GetIcuCode() method.
		/// </summary>
		[Test]
		public void TestGetIcuCode()
		{
			Ethnologue eth = new Ethnologue();
			string sIcu = eth.GetIcuCode("eng");
			Assert.AreEqual("en", sIcu);
			sIcu = eth.GetIcuCode("cmn");
			Assert.AreEqual("zh", sIcu);
			sIcu = eth.GetIcuCode("zho");
			Assert.AreEqual("zh", sIcu);
			sIcu = eth.GetIcuCode("aaa");
			Assert.AreEqual("aaa", sIcu);
			sIcu = eth.GetIcuCode("kar");
			Assert.AreEqual("xkar", sIcu);
		}

		/// <summary>
		/// Test the GetIsoCode() method.
		/// </summary>
		[Test]
		public void TestGetIsoCode()
		{
			Ethnologue eth = new Ethnologue();
			string sIso = eth.GetIsoCode("en");
			Assert.AreEqual("eng", sIso);
			sIso = eth.GetIsoCode("zh");
			Assert.AreEqual("cmn", sIso);
			sIso = eth.GetIsoCode("xkar");
			Assert.AreEqual(null, sIso);
			sIso = eth.GetIsoCode("ekar");
			Assert.AreEqual("kar", sIso);
		}

		/// <summary>
		/// Test the GetLanguageNamesLike() method.
		/// </summary>
		[Test]
		public void TestGetLanguageNamesLike()
		{
			Ethnologue eth = new Ethnologue();
			List<Ethnologue.Names> res = eth.GetLanguageNamesLike("Amha", 'x');
			Assert.GreaterOrEqual(res.Count, 8);
			Assert.AreEqual("Amharic", res[0].LangName);
			Assert.AreEqual("amh", res[0].EthnologueCode);

			List<Ethnologue.Names> res2 = eth.GetLanguageNamesLike("Amha", 'L');
			Assert.Less(res2.Count, res.Count);
			Assert.AreEqual("Amharic", res2[res2.Count - 1].LangName);
			Assert.AreEqual("amh", res2[res2.Count - 1].EthnologueCode);

			List<Ethnologue.Names> res3 = eth.GetLanguageNamesLike("Amha", 'R');
			Assert.AreEqual(res.Count, res3.Count);		// Not what I like, but SQL code did this.

			List<Ethnologue.Names> res4 = eth.GetLanguageNamesLike("chao", 'R');
			Assert.GreaterOrEqual(res4.Count, 1);
			Assert.AreEqual(res4[0].LangName, "Biao Chao");
			Assert.AreEqual(res4[0].EthnologueCode, "bje");
		}

		/// <summary>
		/// Test the GetLanguagesForIso() method.
		/// </summary>
		[Test]
		public void TestGetLanguagesForIso()
		{
			Ethnologue eth = new Ethnologue();
			List<Ethnologue.Names> res = eth.GetLanguagesForIso("eng");
			Assert.GreaterOrEqual(res.Count, 150);
			Assert.AreEqual("AAVE", res[0].LangName);
			Assert.AreEqual("US", res[0].CountryId);
			Assert.AreEqual("United States", res[0].CountryName);

			List<Ethnologue.Names> res2 = eth.GetLanguagesForIso("aaa");
			Assert.AreEqual(1, res2.Count);
			Assert.AreEqual("Ghotuo", res2[0].LangName);
			Assert.AreEqual("NG", res2[0].CountryId);
			Assert.AreEqual("Nigeria", res2[0].CountryName);
		}

		/// <summary>
		/// Test the GetLanguagesInCountry() method.
		/// </summary>
		[Test]
		public void TestGetLanguagesInCountry()
		{
			Ethnologue eth = new Ethnologue();
			List<Ethnologue.Names> res = eth.GetLanguagesInCountry("United States", true);
			Assert.GreaterOrEqual(res.Count, 200);
			Assert.LessOrEqual(res.Count, 250);
			Assert.AreEqual("aaq", res[0].EthnologueCode);

			List<Ethnologue.Names> res2 = eth.GetLanguagesInCountry("United States", false);
			Assert.GreaterOrEqual(res2.Count, 1000);
			Assert.AreEqual("eng", res2[0].EthnologueCode);
			Assert.AreEqual("AAVE", res2[0].LangName);
		}

		/// <summary>
		/// Test the GetOtherLanguageNames() method.
		/// </summary>
		[Test]
		public void TestGetOtherLanguageNames()
		{
			Ethnologue eth = new Ethnologue();
			List<Ethnologue.OtherNames> res = eth.GetOtherLanguageNames("eng");
			Assert.GreaterOrEqual(res.Count, 50);
			Assert.IsTrue(res[0].IsPrimaryName);
			Assert.AreEqual("English", res[0].LangName);
			Assert.IsFalse(res[1].IsPrimaryName);
			Assert.AreNotEqual("English", res[1].LangName);
		}
	}
}
