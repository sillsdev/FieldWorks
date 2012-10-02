// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: OtherTests.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Misc. unit tests for the GAFAWS data layer.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

using NUnit.Framework;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Class to do general tests.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class OtherTests : DataLayerBase
	{
		protected string m_fileName;
		protected Other m_otherTop;
		List<XmlElement> m_anyTop;
		XmlElement m_xe;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public OtherTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a class before each test is run.
		/// This is called by NUnit before each test.
		/// It ensures each test will have a brand new GAFAWSData object to work with.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_fileName = MakeFile();
			m_gd = GAFAWSData.Create();
			m_otherTop = new Other();
		}

		/// <summary>
		/// Clean out the stuff after running each test.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			m_fileName = null;
			m_gd = null;
			m_otherTop = null;
		}

		public void AddOtherContents()
		{
			m_anyTop = m_otherTop.XmlElements;
			Assert.IsNotNull(m_anyTop);
			XmlDocument doc = new XmlDocument();
			m_xe = doc.CreateElement("MyStuff");
			string name = m_xe.Name;
			m_xe.SetAttribute("val", "true");
			m_anyTop.Add(m_xe);
			XmlElement xeys = doc.CreateElement("YourStuff");
			xeys.SetAttribute("ID", "YS1");
			m_xe.AppendChild(xeys);
			m_gd.SaveData(m_fileName);

			m_otherTop = null;
			m_anyTop = null;
			m_gd = null;

			// Make sure it is there.
			m_gd = GAFAWSData.LoadData(m_fileName);
		}

		public void CheckOtherContents()
		{
			Assert.IsNotNull(m_otherTop, "otherTop is null.");
			m_anyTop = m_otherTop.XmlElements;
			Assert.IsNotNull(m_anyTop, "anyTop is null.");
			Assert.AreEqual(1, m_anyTop.Count, "Wrong count");
			m_xe = m_anyTop[0];
			Assert.AreEqual("true", m_xe.GetAttribute("val"), "Wrong value for 'val'");
			XmlElement xeys = m_xe["YourStuff"];
			Assert.AreEqual("YS1", xeys.GetAttribute("ID"), "Wrong value for 'ID'");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add some contents to an 'Other'.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AddOtherToGAFAWSData()
		{
			try
			{
				m_gd.Other = m_otherTop;

				AddOtherContents();

				m_otherTop = m_gd.Other;

				CheckOtherContents();
			}
			finally
			{
				DeleteFile(m_fileName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add some contents to an 'Other' for Stem.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AddOtherToStem()
		{
			try
			{
				WordRecord wr = new WordRecord();
				m_gd.WordRecords.Add(wr);
				Stem stem = new Stem();
				wr.Stem = stem;
				stem.Other = m_otherTop;

				AddOtherContents();

				m_otherTop = m_gd.WordRecords[0].Stem.Other;

				CheckOtherContents();
			}
			finally
			{
				DeleteFile(m_fileName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add some contents to an 'Other' for Stem.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AddOtherToAffix()
		{
			try
			{
				WordRecord wr = new WordRecord();
				m_gd.WordRecords.Add(wr);
				wr.Prefixes = new List<Affix>();
				Affix afx = new Affix();
				wr.Prefixes.Add(afx);

				afx.Other = m_otherTop;

				AddOtherContents();

				m_otherTop = m_gd.WordRecords[0].Prefixes[0].Other;

				CheckOtherContents();
			}
			finally
			{
				DeleteFile(m_fileName);
			}
		}
	}
}
