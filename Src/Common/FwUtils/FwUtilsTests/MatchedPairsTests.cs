// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
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
		[OneTimeSetUp]
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
			Assert.That(m_pairList, Is.Not.Null);
			Assert.That(m_pairList.Count, Is.EqualTo(3));

			Assert.That(m_pairList[0].Open, Is.EqualTo("["));
			Assert.That(m_pairList[0].Close, Is.EqualTo("]"));
			Assert.That(m_pairList[0].PermitParaSpanning, Is.True);

			Assert.That(m_pairList[1].Open, Is.EqualTo("{"));
			Assert.That(m_pairList[1].Close, Is.EqualTo("}"));
			Assert.That(m_pairList[1].PermitParaSpanning, Is.False);

			Assert.That(m_pairList[2].Open, Is.EqualTo("("));
			Assert.That(m_pairList[2].Close, Is.EqualTo(")"));
			Assert.That(m_pairList[2].PermitParaSpanning, Is.True);
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

			Assert.That(xml, Is.EqualTo(kXml));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BelongsToPair method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BelongsToPairTest()
		{
			Assert.That(m_pairList.BelongsToPair("{"), Is.True);
			Assert.That(m_pairList.BelongsToPair("["), Is.True);
			Assert.That(m_pairList.BelongsToPair("("), Is.True);
			Assert.That(m_pairList.BelongsToPair("}"), Is.True);
			Assert.That(m_pairList.BelongsToPair("]"), Is.True);
			Assert.That(m_pairList.BelongsToPair(")"), Is.True);
			Assert.That(m_pairList.BelongsToPair("<"), Is.False);
			Assert.That(m_pairList.BelongsToPair("."), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsMatchedPair method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsMatchedPairTest()
		{
			Assert.That(m_pairList.IsMatchedPair("[", "]"), Is.True);
			Assert.That(m_pairList.IsMatchedPair("{", "}"), Is.True);
			Assert.That(m_pairList.IsMatchedPair("(", ")"), Is.True);

			Assert.That(m_pairList.IsMatchedPair(")", "("), Is.False);
			Assert.That(m_pairList.IsMatchedPair("[", ")"), Is.False);
			Assert.That(m_pairList.IsMatchedPair(".", "]"), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetPairForOpen method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPairForOpenTest()
		{
			Assert.That(m_pairList.GetPairForOpen("["), Is.EqualTo(m_pairList[0]));
			Assert.That(m_pairList.GetPairForOpen("]"), Is.Null);

			Assert.That(m_pairList.GetPairForOpen("{"), Is.EqualTo(m_pairList[1]));
			Assert.That(m_pairList.GetPairForOpen("}"), Is.Null);

			Assert.That(m_pairList.GetPairForOpen("("), Is.EqualTo(m_pairList[2]));
			Assert.That(m_pairList.GetPairForOpen(")"), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetPairForClose method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPairForCloseTest()
		{
			Assert.That(m_pairList.GetPairForClose("]"), Is.EqualTo(m_pairList[0]));
			Assert.That(m_pairList.GetPairForClose("["), Is.Null);

			Assert.That(m_pairList.GetPairForClose("}"), Is.EqualTo(m_pairList[1]));
			Assert.That(m_pairList.GetPairForClose("{"), Is.Null);

			Assert.That(m_pairList.GetPairForClose(")"), Is.EqualTo(m_pairList[2]));
			Assert.That(m_pairList.GetPairForClose("("), Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsOpen method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsOpenTest()
		{
			Assert.That(m_pairList.IsOpen("["), Is.True);
			Assert.That(m_pairList.IsOpen("{"), Is.True);
			Assert.That(m_pairList.IsOpen("("), Is.True);

			Assert.That(m_pairList.IsOpen("]"), Is.False);
			Assert.That(m_pairList.IsOpen("}"), Is.False);
			Assert.That(m_pairList.IsOpen(")"), Is.False);

			Assert.That(m_pairList.IsOpen("."), Is.False);
			Assert.That(m_pairList.IsOpen(";"), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsClose method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsCloseTest()
		{
			Assert.That(m_pairList.IsClose("]"), Is.True);
			Assert.That(m_pairList.IsClose("}"), Is.True);
			Assert.That(m_pairList.IsClose(")"), Is.True);

			Assert.That(m_pairList.IsClose("["), Is.False);
			Assert.That(m_pairList.IsClose("{"), Is.False);
			Assert.That(m_pairList.IsClose("("), Is.False);

			Assert.That(m_pairList.IsClose("."), Is.False);
			Assert.That(m_pairList.IsClose(";"), Is.False);
		}
	}
}
