// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml;
using LanguageExplorer.MGA;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.MGA
{
	/// <summary>
	/// Test sets for the GlossListBox class.
	/// </summary>
	[TestFixture]
	public class GlossListBoxTest : MemoryOnlyBackendProviderTestBase
	{
		private GlossListBox m_LabelGlosses;
		private XmlDocument m_doc;

		/// <summary>
		/// This method is called before each test
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			var sXmlFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "MGA", "GlossLists", "EticGlossList.xml");
			m_doc = new XmlDocument();
			m_doc.Load(sXmlFile);
			m_LabelGlosses = new GlossListBox
			{
				Sorted = false
			};
			var node = m_doc.SelectSingleNode("//item[@id='vPositive']");
			var glbi = new GlossListBoxItem(Cache, node, ".", string.Empty, false);
			m_LabelGlosses.Items.Add(glbi);
		}

		/// <summary>
		/// This method is called after each test
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			try
			{
				m_LabelGlosses.Dispose();
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

		[Test]
		public void GlossListBoxCountTest()
		{
			Assert.AreEqual(1, m_LabelGlosses.Items.Count);
		}
		[Test]
		public void GlossListBoxContentTest()
		{
			Assert.AreEqual("positive: pos", m_LabelGlosses.Items[0].ToString());
		}
		[Test]
		public void GlossListItemConflicts()
		{
			// check another terminal node, but with different parent, so no conflict
			var node = m_doc.SelectSingleNode("//item[@id='cAdjAgr']/item[@target='tCommonAgr']/item[@target='fGender']/item[@target='vMasc']");
			var glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			GlossListBoxItem glbiConflict;
			var fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			var sMsg = glbiConflict != null ? $"Masculine gender should not conflict, but did with {glbiConflict.Abbrev}." : "Masculine gender should not conflict";
			Assert.IsFalse(fResult, sMsg);
			// check a non-terminal node, so no conflict
			node = m_doc.SelectSingleNode("//item[@id='fDeg']");
			glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			sMsg = glbiConflict != null ? $"Feature degree should not conflict, but did with {glbiConflict.Abbrev}" : "Feature degree should not conflict";
			Assert.IsFalse(fResult, sMsg);
			// check another terminal node with same parent, so there is conflict
			node = m_doc.SelectSingleNode("//item[@id='vComp']");
			glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			Assert.IsTrue(fResult, "Comparative should conflict with positive, but did not");
		}
	}
}