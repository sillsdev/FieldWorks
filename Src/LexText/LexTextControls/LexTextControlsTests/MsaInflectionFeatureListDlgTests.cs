// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MsaInflectionFeatureListDlgTests.cs
// Responsibility:

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using XCore;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MsaInflectionFeatureListDlgTests.
	/// </summary>
	[TestFixture]
	public class MsaInflectionFeatureListDlgTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Test setup
		protected override void CreateTestData()
		{
			base.CreateTestData();
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
		}
		#endregion

		private ILangProject CreateFeatureSystem(out IFsFeatStruc featStruct)
		{
			featStruct = null;
			ILangProject lp = Cache.LanguageProject;

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			string sFileDir = Path.Combine(SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder.SourceDirectory,
				Path.Combine(@"FDO", Path.Combine(@"FDOTests", @"TestData")));
			string sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");

			doc.Load(sFile);
			XmlNode itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");

			// Add the feature for first time
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			msfs.AddFeatureFromXml(itemFem);

			// now add to feature structure
			IPartOfSpeech pos = lp.PartsOfSpeechOA.PossibilitiesOS[0] as IPartOfSpeech;

			pos.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(itemNeut, msfs);
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(itemFem, msfs);
			// Now add another feature
			XmlNode item1st = doc.SelectSingleNode("//item[@id='v1']");
			featStruct.AddFeatureFromXml(item1st, msfs);
			// Update inflectable features on pos
			XmlNode subjAgr = doc.SelectSingleNode("//item[@id='cSubjAgr']");
			pos.AddInflectableFeatsFromXml(subjAgr);
			pos.AddInflectableFeatsFromXml(itemNeut);

			return lp;
		}

		[Test]
		public void LoadInflectableFeats()
		{
			// Set up sample data
			IFsFeatStruc featStruct;
			using (FeatureStructureTreeView tv = SetUpSampleData(out featStruct))
			{
				// load some feature structure values into treeview
				LoadFeatureValuesIntoTreeview(tv, featStruct);
				// Make feature structure based on values in treeview
				MakeFeatureStructure(tv, featStruct);
				TestFeatureStructureContent(featStruct);
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "tv is a reference")]
		public void PopulateTreeFromFeatureSystem()
		{
			// Set up sample data
			IFsFeatStruc featStruct;
			ILangProject lp = CreateFeatureSystem(out featStruct);


			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			string sFileDir = Path.Combine(SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder.SourceDirectory,
										   Path.Combine(@"FDO", Path.Combine(@"FDOTests", @"TestData")));
			string sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");
			doc.Load(sFile);
			XmlNode itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");
			// Add some complex features
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			msfs.AddFeatureFromXml(itemFem);
			// Now add another feature to the complex one
			XmlNode item1st = doc.SelectSingleNode("//item[@id='v1']");
			msfs.AddFeatureFromXml(item1st);
			// now get a simple, top-level closed feature
			sFile = Path.Combine(sFileDir, "FeatureSystem3.xml");
			doc.Load(sFile);
			XmlNode itemImpfv = doc.SelectSingleNode("//item[@id='vImpfv']");
			msfs.AddFeatureFromXml(itemImpfv);
			XmlNode itemCont = doc.SelectSingleNode("//item[@id='vCont']");
			msfs.AddFeatureFromXml(itemCont);

			using (var dlg = new FeatureSystemInflectionFeatureListDlg())
			{
				ILexEntryInflType cobj =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>().Create();
				lp.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(cobj);
				dlg.SetDlgInfo(Cache, null, null, cobj, 0);

				// load some feature system values into treeview
				FeatureStructureTreeView tv = dlg.TreeView;

				Assert.AreEqual(2, tv.Nodes.Count, "Count of top level nodes in tree view");
				TreeNodeCollection col = tv.Nodes[0].Nodes;
				Assert.AreEqual(3, col.Count, "Count of first level nodes in tree view");
			}
		}

		private void TestFeatureStructureContent(IFsFeatStruc featStruct)
		{
			IFdoOwningCollection<IFsFeatureSpecification> specCol = featStruct.FeatureSpecsOC;
			Assert.AreEqual(1, specCol.Count, "Count of top level feature specs");
			foreach (IFsFeatureSpecification spec in specCol)
			{
				IFsComplexValue complex = spec as IFsComplexValue;
				Assert.IsNotNull(complex, "complex feature value is null and should not be");
				Assert.AreEqual("subject agreement", complex.FeatureRA.Name.AnalysisDefaultWritingSystem.Text, "Expected complex feature name");
				IFsFeatStruc fsNested = complex.ValueOA as IFsFeatStruc;
				IFdoOwningCollection<IFsFeatureSpecification> fsNestedCol = fsNested.FeatureSpecsOC;
				Assert.AreEqual(2, fsNestedCol.Count, "Nested fs has one feature");
				foreach (IFsFeatureSpecification specNested in fsNestedCol)
				{
					IFsClosedValue closed = specNested as IFsClosedValue;
					Assert.IsNotNull(closed, "closed feature value is null and should not be");
					if (!(((closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "gender") &&
							(closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "feminine gender")) ||
						  ((closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "person") &&
							(closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "first person"))))
					{
						Assert.Fail("Unexpected value found: {0}:{1}",
							closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text,
							closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text);
					}
				}
			}
		}

		private void MakeFeatureStructure(FeatureStructureTreeView tv, IFsFeatStruc featStruct)
		{
			using (MsaInflectionFeatureListDlg dlg = new MsaInflectionFeatureListDlg())
			{
				foreach (IFsFeatureSpecification spec in featStruct.FeatureSpecsOC)
					featStruct.FeatureSpecsOC.Remove(spec);
				dlg.SetDlgInfo(Cache, null, null, featStruct, MoStemMsaTags.kflidMsFeatures);
				dlg.UpdateFeatureStructure(tv.Nodes);
			}
		}

		private void LoadFeatureValuesIntoTreeview(FeatureStructureTreeView tv, IFsFeatStruc featStruct)
		{
			TreeNodeCollection col;
			tv.PopulateTreeFromFeatureStructure(featStruct);
			Assert.AreEqual(1, tv.Nodes.Count, "Count of top level after feature structure");
			col = tv.Nodes[0].Nodes;
			Assert.AreEqual(2, col.Count, "Count of first level nodes in tree view");
			foreach (TreeNode node in col)
			{
				TreeNodeCollection col2 = node.Nodes;
				if (node.Text == "gender")
					Assert.AreEqual(2, col2.Count, "Count of second level nodes in tree view");
				if (node.Text == "person")
					Assert.AreEqual(1, col2.Count, "Count of second level nodes in tree view");
			}
		}

		private FeatureStructureTreeView SetUpSampleData(out IFsFeatStruc featStruct)
		{
			ILangProject lp = CreateFeatureSystem(out featStruct);
			// load some feature system values into treeview
			IPartOfSpeech pos = lp.PartsOfSpeechOA.PossibilitiesOS[0] as IPartOfSpeech;
			FeatureStructureTreeView tv = new FeatureStructureTreeView();
			tv.PopulateTreeFromInflectableFeats(pos.InflectableFeatsRC);
			Assert.AreEqual(1, tv.Nodes.Count, "Count of top level nodes in tree view");
			TreeNodeCollection col = tv.Nodes[0].Nodes;
			Assert.AreEqual(1, col.Count, "Count of first level nodes in tree view");
			foreach (TreeNode node in col)
			{
				TreeNodeCollection col2 = node.Nodes;
				Assert.AreEqual(2, col2.Count, "Count of second level nodes in tree view");
				if (node.PrevNode == null)
					node.Checked = true;
			}
			return tv;
		}
	}
}
