// --------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MatchedPairsTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using NUnit.Framework;
using Microsoft.Win32;
using System.Reflection;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the VersificationTable class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MatchedPairsTests
	{
		private const string kXml =
				"<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
				"<MatchedPairs>" +
					"<pair open=\"[\" close=\"]\" permitParaSpanning=\"true\" />" +
					"<pair open=\"{\" close=\"}\" permitParaSpanning=\"false\" />" +
					"<pair open=\"(\" close=\")\" permitParaSpanning=\"true\" />" +
				"</MatchedPairs>";

		private MatchedPairList m_pairList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_pairList = MatchedPairList.Load(kXml, "Test WS");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Load method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadTest()
		{
			Assert.IsNotNull(m_pairList);
			Assert.AreEqual(3, m_pairList.Count);

			Assert.AreEqual("[", m_pairList[0].Open);
			Assert.AreEqual("]", m_pairList[0].Close);
			Assert.IsTrue(m_pairList[0].PermitParaSpanning);

			Assert.AreEqual("{", m_pairList[1].Open);
			Assert.AreEqual("}", m_pairList[1].Close);
			Assert.IsFalse(m_pairList[1].PermitParaSpanning);

			Assert.AreEqual("(", m_pairList[2].Open);
			Assert.AreEqual(")", m_pairList[2].Close);
			Assert.IsTrue(m_pairList[2].PermitParaSpanning);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the XmlString property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XmlStringTest()
		{
			string xml = m_pairList.XmlString;
			xml = xml.Replace(Environment.NewLine + "    ", string.Empty);
			xml = xml.Replace(Environment.NewLine + "   ", string.Empty);
			xml = xml.Replace(Environment.NewLine + "  ", string.Empty);
			xml = xml.Replace(Environment.NewLine + " ", string.Empty);
			xml = xml.Replace(Environment.NewLine, string.Empty);

			Assert.AreEqual(kXml, xml);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BelongsToPair method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BelongsToPairTest()
		{
			Assert.IsTrue(m_pairList.BelongsToPair("{"));
			Assert.IsTrue(m_pairList.BelongsToPair("["));
			Assert.IsTrue(m_pairList.BelongsToPair("("));
			Assert.IsTrue(m_pairList.BelongsToPair("}"));
			Assert.IsTrue(m_pairList.BelongsToPair("]"));
			Assert.IsTrue(m_pairList.BelongsToPair(")"));
			Assert.IsFalse(m_pairList.BelongsToPair("<"));
			Assert.IsFalse(m_pairList.BelongsToPair("."));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsMatchedPair method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsMatchedPairTest()
		{
			Assert.IsTrue(m_pairList.IsMatchedPair("[", "]"));
			Assert.IsTrue(m_pairList.IsMatchedPair("{", "}"));
			Assert.IsTrue(m_pairList.IsMatchedPair("(", ")"));

			Assert.IsFalse(m_pairList.IsMatchedPair(")", "("));
			Assert.IsFalse(m_pairList.IsMatchedPair("[", ")"));
			Assert.IsFalse(m_pairList.IsMatchedPair(".", "]"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetPairForOpen method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPairForOpenTest()
		{
			Assert.AreEqual(m_pairList[0], m_pairList.GetPairForOpen("["));
			Assert.IsNull(m_pairList.GetPairForOpen("]"));

			Assert.AreEqual(m_pairList[1], m_pairList.GetPairForOpen("{"));
			Assert.IsNull(m_pairList.GetPairForOpen("}"));

			Assert.AreEqual(m_pairList[2], m_pairList.GetPairForOpen("("));
			Assert.IsNull(m_pairList.GetPairForOpen(")"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetPairForClose method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPairForCloseTest()
		{
			Assert.AreEqual(m_pairList[0], m_pairList.GetPairForClose("]"));
			Assert.IsNull(m_pairList.GetPairForClose("["));

			Assert.AreEqual(m_pairList[1], m_pairList.GetPairForClose("}"));
			Assert.IsNull(m_pairList.GetPairForClose("{"));

			Assert.AreEqual(m_pairList[2], m_pairList.GetPairForClose(")"));
			Assert.IsNull(m_pairList.GetPairForClose("("));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsOpen method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsOpenTest()
		{
			Assert.IsTrue(m_pairList.IsOpen("["));
			Assert.IsTrue(m_pairList.IsOpen("{"));
			Assert.IsTrue(m_pairList.IsOpen("("));

			Assert.IsFalse(m_pairList.IsOpen("]"));
			Assert.IsFalse(m_pairList.IsOpen("}"));
			Assert.IsFalse(m_pairList.IsOpen(")"));

			Assert.IsFalse(m_pairList.IsOpen("."));
			Assert.IsFalse(m_pairList.IsOpen(";"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsClose method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsCloseTest()
		{
			Assert.IsTrue(m_pairList.IsClose("]"));
			Assert.IsTrue(m_pairList.IsClose("}"));
			Assert.IsTrue(m_pairList.IsClose(")"));

			Assert.IsFalse(m_pairList.IsClose("["));
			Assert.IsFalse(m_pairList.IsClose("{"));
			Assert.IsFalse(m_pairList.IsClose("("));

			Assert.IsFalse(m_pairList.IsClose("."));
			Assert.IsFalse(m_pairList.IsClose(";"));
		}
	}
}
