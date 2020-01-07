// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit;
using LanguageExplorer.Controls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.Areas.Lists
{
	/// <summary />
	[TestFixture]
	public class MsaInflectionFeatureListDlgTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILangProject _langProject;

		#region Overrides of LcmTestBase

		protected override void CreateTestData()
		{
			base.CreateTestData();
			_langProject = Cache.LanguageProject;
			_langProject.PartsOfSpeechOA.PossibilitiesOS.Add(Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create());
		}
		#endregion

		private IFsFeatStruc CreateFeatureSystem()
		{
			// Set up the xml fs description
			var doc = new XmlDocument();
			doc.Load(Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "FeatureSystem2.xml"));
			var itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");
			// Add the feature for first time
			var msfs = _langProject.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			// Now add a feature that differs only in value
			var itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			msfs.AddFeatureFromXml(itemFem);
			// now add to feature structure
			var pos = (IPartOfSpeech)_langProject.PartsOfSpeechOA.PossibilitiesOS[0];
			pos.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			var featStruct = pos.DefaultFeaturesOA;
			// Add the first feature
			featStruct.AddFeatureFromXml(itemNeut, msfs);
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(itemFem, msfs);
			// Now add another feature
			var item1st = doc.SelectSingleNode("//item[@id='v1']");
			featStruct.AddFeatureFromXml(item1st, msfs);
			// Update inflectable features on pos
			var subjAgr = doc.SelectSingleNode("//item[@id='cSubjAgr']");
			pos.AddInflectableFeatsFromXml(subjAgr);
			pos.AddInflectableFeatsFromXml(itemNeut);
			return featStruct;
		}

		private FeatureStructureTreeView SetUpSampleData(out IFsFeatStruc featStruct)
		{
			featStruct = CreateFeatureSystem();
			// load some feature system values into treeview
			var pos = _langProject.PartsOfSpeechOA.PossibilitiesOS[0] as IPartOfSpeech;
			var tv = new FeatureStructureTreeView();
			tv.PopulateTreeFromInflectableFeats(pos.InflectableFeatsRC);
			Assert.AreEqual(1, tv.Nodes.Count, "Count of top level nodes in tree view");
			var col = tv.Nodes[0].Nodes;
			Assert.AreEqual(1, col.Count, "Count of first level nodes in tree view");
			foreach (TreeNode node in col)
			{
				var col2 = node.Nodes;
				Assert.AreEqual(2, col2.Count, "Count of second level nodes in tree view");
				if (node.PrevNode == null)
				{
					node.Checked = true;
				}
			}
			return tv;
		}

		private void MakeFeatureStructure(FeatureStructureTreeView tv, IFsFeatStruc featStruct)
		{
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var dlg = new MsaInflectionFeatureListDlg())
				{
					foreach (var spec in featStruct.FeatureSpecsOC)
					{
						featStruct.FeatureSpecsOC.Remove(spec);
					}
					dlg.SetDlgInfo(Cache, flexComponentParameters.PropertyTable, featStruct, MoStemMsaTags.kflidMsFeatures);
					dlg.UpdateFeatureStructure(tv.Nodes);
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}

		private static void LoadFeatureValuesIntoTreeview(FeatureStructureTreeView tv, IFsFeatStruc featStruct)
		{
			TreeNodeCollection col;
			tv.PopulateTreeFromFeatureStructure(featStruct);
			Assert.AreEqual(1, tv.Nodes.Count, "Count of top level after feature structure");
			col = tv.Nodes[0].Nodes;
			Assert.AreEqual(2, col.Count, "Count of first level nodes in tree view");
			foreach (TreeNode node in col)
			{
				var col2 = node.Nodes;
				if (node.Text == "gender")
				{
					Assert.AreEqual(2, col2.Count, "Count of second level nodes in tree view");
				}
				if (node.Text == "person")
				{
					Assert.AreEqual(1, col2.Count, "Count of second level nodes in tree view");
				}
			}
		}

		private static void TestFeatureStructureContent(IFsFeatStruc featStruct)
		{
			var specCol = featStruct.FeatureSpecsOC;
			Assert.AreEqual(1, specCol.Count, "Count of top level feature specs");
			foreach (var spec in specCol)
			{
				var complex = spec as IFsComplexValue;
				Assert.IsNotNull(complex, "complex feature value is null and should not be");
				Assert.AreEqual("subject agreement", complex.FeatureRA.Name.AnalysisDefaultWritingSystem.Text, "Expected complex feature name");
				var fsNested = complex.ValueOA as IFsFeatStruc;
				var fsNestedCol = fsNested.FeatureSpecsOC;
				Assert.AreEqual(2, fsNestedCol.Count, "Nested fs has one feature");
				foreach (var specNested in fsNestedCol)
				{
					var closed = specNested as IFsClosedValue;
					Assert.IsNotNull(closed, "closed feature value is null and should not be");
					if (!(closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "gender" && closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "feminine gender"
						  || closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "person" && closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "first person"))
					{
						Assert.Fail("Unexpected value found: {0}:{1}", closed.FeatureRA.Name.AnalysisDefaultWritingSystem.Text, closed.ValueRA.Name.AnalysisDefaultWritingSystem.Text);
					}
				}
			}
		}

		[Test]
		public void LoadInflectableFeats()
		{
			// Set up sample data
			IFsFeatStruc featStruct;
			using (var tv = SetUpSampleData(out featStruct))
			{
				// load some feature structure values into treeview
				LoadFeatureValuesIntoTreeview(tv, featStruct);
				// Make feature structure based on values in treeview
				MakeFeatureStructure(tv, featStruct);
				TestFeatureStructureContent(featStruct);
			}
		}

		[Test]
		public void PopulateTreeFromFeatureSystem()
		{
			// Set up sample data
			CreateFeatureSystem();
			var dir = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls");
			// Set up the xml fs description
			var doc = new XmlDocument();
			var sFile = Path.Combine(dir, "FeatureSystem2.xml");
			doc.Load(sFile);
			var itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");
			// Add some complex features
			var msfs = _langProject.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			// Now add a feature that differs only in value
			var itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			msfs.AddFeatureFromXml(itemFem);
			// Now add another feature to the complex one
			var item1st = doc.SelectSingleNode("//item[@id='v1']");
			msfs.AddFeatureFromXml(item1st);
			// now get a simple, top-level closed feature
			sFile = Path.Combine(dir, "FeatureSystem3.xml");
			doc.Load(sFile);
			var itemImpfv = doc.SelectSingleNode("//item[@id='vImpfv']");
			msfs.AddFeatureFromXml(itemImpfv);
			var itemCont = doc.SelectSingleNode("//item[@id='vCont']");
			msfs.AddFeatureFromXml(itemCont);

			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var dlg = new FeatureSystemInflectionFeatureListDlg())
				{
					var cobj = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>().Create();
					_langProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(cobj);
					dlg.SetDlgInfo(Cache, flexComponentParameters.PropertyTable, cobj, 0);

					// load some feature system values into treeview
					var tv = dlg.TreeView;

					Assert.AreEqual(2, tv.Nodes.Count, "Count of top level nodes in tree view");
					var col = tv.Nodes[0].Nodes;
					Assert.AreEqual(3, col.Count, "Count of first level nodes in tree view");
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}
	}
}