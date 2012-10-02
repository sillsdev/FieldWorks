// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MsaInflectionFeatureListDlgTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Utils;

using NUnit.Framework;
//using XmlUnit;
//using XmlUnit.Tests;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MsaInflectionFeatureListDlgTests.
	/// </summary>
	[TestFixture]
	public class MsaInflectionFeatureListDlgTests : InDatabaseFdoTestBase
	{

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateTestData()
		{
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(new PartOfSpeech());
		}
		#endregion

		private ILangProject CreateFeatureSystem(out IFsFeatStruc featStruct)
		{
			featStruct = null;
			ILangProject lp = Cache.LangProject;

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			string sFileDir = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory, @"FDO\FDOTests\TestData");
			string sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");

			doc.Load(sFile);
			XmlNode itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");

			// Add the feature for first time
			FsFeatureSystem.AddFeatureAsXml(Cache, itemNeut);
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			FsFeatureSystem.AddFeatureAsXml(Cache, itemFem);

			// now add to feature structure
			IPartOfSpeech pos = (IPartOfSpeech)lp.PartsOfSpeechOA.PossibilitiesOS.FirstItem;

			pos.DefaultFeaturesOA = new FsFeatStruc();
			featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(Cache, itemNeut);
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(Cache, itemFem);
			// Now add another feature
			XmlNode item1st = doc.SelectSingleNode("//item[@id='v1']");
			featStruct.AddFeatureFromXml(Cache, item1st);
			// Update inflectable features on pos
			XmlNode subjAgr = doc.SelectSingleNode("//item[@id='cSubjAgr']");
			pos.AddInflectableFeatsFromXml(Cache, subjAgr);
			pos.AddInflectableFeatsFromXml(Cache, itemNeut);

			return lp;
		}
		[Test]
		public void LoadInflectableFeats()
		{
			CheckDisposed();

			CreateTestData();

			// Set up sample data
			IFsFeatStruc featStruct;
			FeatureStructureTreeView tv = SetUpSampleData(out featStruct);
			// load some feature structure values into treeview
			LoadFeatureValuesIntoTreeview(tv, featStruct);
			// Make feature structure based on values in treeview
			MakeFeatureStructure(tv, featStruct);
			TestFeatureStructureContent(featStruct);
		}

		private void TestFeatureStructureContent(IFsFeatStruc featStruct)
		{
			FdoOwningCollection<IFsFeatureSpecification> specCol = featStruct.FeatureSpecsOC;
			Assert.AreEqual(1, specCol.Count, "Count of top level feature specs");
			foreach (IFsFeatureSpecification spec in specCol)
			{
				IFsComplexValue complex = spec as IFsComplexValue;
				Assert.IsNotNull(complex, "complex feature value is null and should not be");
				Assert.AreEqual("subject agreement", complex.FeatureRA.Name.AnalysisDefaultWritingSystem, "Expected complex feature name");
				IFsFeatStruc fsNested = (IFsFeatStruc)complex.ValueOA;
				FdoOwningCollection<IFsFeatureSpecification> fsNestedCol = fsNested.FeatureSpecsOC;
				Assert.AreEqual(2, fsNestedCol.Count, "Nested fs has one feature");
				foreach (IFsFeatureSpecification specNested in fsNestedCol)
				{
					IFsClosedValue closed = specNested as IFsClosedValue;
					Assert.IsNotNull(closed, "closed feature value is null and should not be");
					if (!(( (closed.FeatureRA.Name.AnalysisDefaultWritingSystem == "gender")  &&
							(closed.ValueRA.Name.AnalysisDefaultWritingSystem == "feminine gender") ) ||
						  ( (closed.FeatureRA.Name.AnalysisDefaultWritingSystem == "person")  &&
							(closed.ValueRA.Name.AnalysisDefaultWritingSystem == "first person") ) ) )
						Assert.Fail("Unexpected value found: {0}:{1}", closed.FeatureRA.Name.AnalysisDefaultWritingSystem,
									closed.ValueRA.Name.AnalysisDefaultWritingSystem);
				}
			}
		}

		private void MakeFeatureStructure(FeatureStructureTreeView tv, IFsFeatStruc featStruct)
		{
			using (MsaInflectionFeatureListDlg dlg = new MsaInflectionFeatureListDlg())
			{
				foreach (IFsFeatureSpecification spec in featStruct.FeatureSpecsOC)
					featStruct.FeatureSpecsOC.Remove(spec);
				dlg.SetDlgInfo(Cache, null, featStruct);
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
			IPartOfSpeech pos = (IPartOfSpeech)lp.PartsOfSpeechOA.PossibilitiesOS.FirstItem;
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
