using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Partial test of FsFeatureSystem
	/// Note: there are other tests of this class in CellarTests.cs, class "MoreCellarTests".
	/// Eventually they should probably be moved here.
	/// </summary>
	[TestFixture]
	public class FeatureSystemTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Basic test of adding the simpler kinds of features.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Multiline string - git uses platform specific line endings")]
		[Test]
		public void AddFeatureFromXml_HandlesClosedFeatures()
		{
			string input =
				@"<item id='gAdjective' posid='Adjective' guid='32367b32-b93c-403e-a305-997e54e96289' status='visible' type='group'>
		<abbrev ws='en'>adj.r</abbrev>
		<term ws='en'>adjective-related</term>
		<def ws='en'>Morphosyntactic features that are generally associated with adjectives and adjective phrases.</def>
		<item id='fDeg' guid='b2646044-b47e-46d7-8dd3-d57c079e7b5f' type='feature'>
			<abbrev ws='en'>deg</abbrev>
			<term ws='en'>degree</term>
			<item id='vPositive' guid='a2c5215b-86f7-4851-ac71-ab04c47137cf' type='value'>
				<abbrev ws='en'>pos</abbrev>
				<term ws='en'>positive</term>
				<fs id='vPositiveFS' type='Infl' typeguid='f1a078e1-4991-4eab-a4e8-cffac73e0ea0'>
					<f name='deg'>
						<sym value='pos' />
					</f>
				</fs>
			</item>
			<item id='vComp' guid='05c3dd95-8ec3-4b13-bd18-734705b00cf7' type='value'>
				<abbrev ws='en'>cmpr</abbrev>
				<term ws='en'>comparative</term>
				<fs id='vCompFS' type='Infl' typeguid='f1a078e1-4991-4eab-a4e8-cffac73e0ea0'>
					<f name='deg'>
						<sym value='cmpr' />
					</f>
				</fs>
			</item>
	  </item>
	</item>";
			var featureSystem = Cache.LangProject.MsFeatureSystemOA;
			Assert.That(featureSystem, Is.Not.Null);
			Assert.That(featureSystem.TypesOC.Count, Is.EqualTo(0), "there should not be pre-existing feature types");
			Assert.That(featureSystem.FeaturesOC.Count, Is.EqualTo(0), "there should not be pre-existing features");
			//m_actionHandler.EndUndoTask(); // AddToDatabase makes its own

			CheckObjDoesNotExist("b2646044-b47e-46d7-8dd3-d57c079e7b5f");
			CheckObjDoesNotExist("a2c5215b-86f7-4851-ac71-ab04c47137cf");
			CheckObjDoesNotExist("05c3dd95-8ec3-4b13-bd18-734705b00cf7");
			CheckObjDoesNotExist("f1a078e1-4991-4eab-a4e8-cffac73e0ea0");

			var doc = new XmlDocument();
			doc.LoadXml(input);
			var rootItem = doc.DocumentElement;
			var degItem = rootItem.ChildNodes[3];

			var positiveItem = degItem.ChildNodes[2];
			var feature = featureSystem.AddFeatureFromXml(positiveItem);

			var featureType = CheckFsFeatStrucType("f1a078e1-4991-4eab-a4e8-cffac73e0ea0", featureSystem, "Infl");
			var cf = CheckFsClosedFeature("b2646044-b47e-46d7-8dd3-d57c079e7b5f", featureSystem, "fDeg");
			Assert.That(cf, Is.EqualTo(feature));
			Assert.That(featureType.FeaturesRS, Has.Member(cf));
			var fv = CheckFsSymFeatVal("a2c5215b-86f7-4851-ac71-ab04c47137cf", cf, "vPositive");

			var compItem = degItem.ChildNodes[3];
			var feature2 = featureSystem.AddFeatureFromXml(compItem);
			Assert.That(featureSystem.TypesOC.Count, Is.EqualTo(1), "should not create a duplicate Infl feature type");
			Assert.That(feature2, Is.EqualTo(feature), "new value should be part of the same feature");
			Assert.That(featureType.FeaturesRS.Count, Is.EqualTo(1), "same feature should not be added twice to feature type");
			CheckFsSymFeatVal("05c3dd95-8ec3-4b13-bd18-734705b00cf7", cf, "vComp");
		}

		/// <summary>
		/// Make sure we can properly add complex features from XML.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Multiline string - git uses platform specific line endings")]
		public void AddFeatureFromXml_HandlesComplexFeatures()
		{
			// Note: mostly this is a subsection of EticGloss.xml. I have added some ID and guid attributes by hand
			// (See guids using CAPS), as is done in GlossListTreeView.FleshOutProxy
			string input =
				@"<item id='tAdjAgr' guid='3293182e-dbf8-460f-a1db-947a5a7bb03b' type='fsType'>
			<abbrev ws='en'>adjAgr</abbrev>
			<term ws='en'>Adjective agreement</term>
			<def ws='en'>Features common to agreement on adjectives.</def>
			<item id='cAdjAgr' guid='58a4cedb-7ed8-4548-8dae-770294804ec9' type='complex'>
				<abbrev ws='en'>adjagr</abbrev>
				<term ws='en'>adjective agreement</term>
				<def ws='en'>This contains the set of features used in adjective agreement.</def>
				<item status='proxy' target='tCommonAgr'>
					<abbrev ws='en'>agr</abbrev>
					<term ws='en'>common agreement</term>
					<item status='proxy' target='fGender' id='fGender' guid='BD0C721D-595B-47FD-A4D5-2C5DE1944978'>
						<abbrev ws='en'>gen</abbrev>
						<term ws='en'>gender</term>
						<def ws='en'>Grammatical gender is a noun class system, ....</def>
						<item status='proxy' target='vMasc' id='vMasc' guid='28AC5D80-A20B-4BA7-86EE-11EBD0824031'>
							<abbrev ws='en'>m</abbrev>
							<term ws='en'>masculine gender</term>
							<def ws='en'>Masculine gender is a grammatical gender that ....</def>
							<fs type='tAdjAgr' typeguid='3293182e-dbf8-460f-a1db-947a5a7bb03b'>
								<f name='adjagr'>
									<fs type='tAdjAgr' typeguid='3293182e-dbf8-460f-a1db-947a5a7bb03b'>
										<f name='gen'>
											<sym value='m' />
										</f>
									</fs>
								</f>
							</fs>
						</item>
						<item status='proxy' target='vFem'>
							<abbrev ws='en'>f</abbrev>
							<term ws='en'>feminine gender</term>
							<def ws='en'>Feminine gender is a grammatical gender that ....</def>
							<fs type='tAdjAgr' typeguid='3293182e-dbf8-460f-a1db-947a5a7bb03b'>
								<f name='adjagr'>
									<fs type='tAdjAgr' typeguid='3293182e-dbf8-460f-a1db-947a5a7bb03b'>
										<f name='gen'>
											<sym value='f' />
										</f>
									</fs>
								</f>
							</fs>
						</item>
					</item>
				</item>
			</item>
		</item>";
			var featureSystem = Cache.LangProject.MsFeatureSystemOA;
			Assert.That(featureSystem, Is.Not.Null);
			Assert.That(featureSystem.TypesOC.Count, Is.EqualTo(0), "there should not be pre-existing feature types");
			Assert.That(featureSystem.FeaturesOC.Count, Is.EqualTo(0), "there should not be pre-existing features");
			//m_actionHandler.EndUndoTask(); // AddToDatabase makes its own

			CheckObjDoesNotExist("3293182e-dbf8-460f-a1db-947a5a7bb03b");
			CheckObjDoesNotExist("58a4cedb-7ed8-4548-8dae-770294804ec9");

			var doc = new XmlDocument();
			doc.LoadXml(input);
			var rootItem = doc.DocumentElement;
			var adjAgrItem = rootItem.ChildNodes[3];
			var commonAgrItem = adjAgrItem.ChildNodes[3];
			var genderItem = commonAgrItem.ChildNodes[2];

			var mascItem = genderItem.ChildNodes[3];
			Assert.That(mascItem, Is.Not.Null);
			var feature = featureSystem.AddFeatureFromXml(mascItem);

			var featureType = CheckFsFeatStrucType("3293182e-dbf8-460f-a1db-947a5a7bb03b", featureSystem, "tAdjAgr");
			var complexFeatureType = CheckFsComplexFeature("58a4cedb-7ed8-4548-8dae-770294804ec9", featureSystem, "cAdjAgr");
			// Lots more we could check, but the essential point of this test is two paths that must create objects with the right guids.
		}

		private void CheckObjDoesNotExist(string id)
		{
			ICmObject obj;
			Assert.That(Cache.ServiceLocator.ObjectRepository.TryGetObject(new Guid(id), out obj), Is.False,
				"feature system should not already contain objects the test is supposed to create");
		}

		private IFsFeatStrucType CheckFsFeatStrucType(string guid, ICmObject owner, string catalogueId)
		{
			IFsFeatStrucType fs;
			Assert.That(Cache.ServiceLocator.GetInstance<IFsFeatStrucTypeRepository>().TryGetObject(new Guid(guid), out fs),
				Is.True,
				"expected FsFeatStrucType should be created with the right guid");
			Assert.That(fs.Owner, Is.EqualTo(owner), "FsFeatStrucType should be created at the right place in the hierarchy");
			Assert.That(fs.CatalogSourceId, Is.EqualTo(catalogueId));
			return fs;
		}

		private IFsClosedFeature CheckFsClosedFeature(string guid, ICmObject owner, string catalogueId)
		{
			IFsClosedFeature feature;
			Assert.That(Cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>().TryGetObject(new Guid(guid), out feature),
				Is.True,
				"expected FsClosedFeature should be created with the right guid");
			Assert.That(feature.Owner, Is.EqualTo(owner), "FsClosedFeature should be created at the right place in the hierarchy");
			Assert.That(feature.CatalogSourceId, Is.EqualTo(catalogueId));
			return feature;
		}

		private IFsComplexFeature CheckFsComplexFeature(string guid, ICmObject owner, string catalogueId)
		{
			IFsComplexFeature feature;
			Assert.That(Cache.ServiceLocator.GetInstance<IFsComplexFeatureRepository>().TryGetObject(new Guid(guid), out feature),
				Is.True,
				"expected FsComplexFeature should be created with the right guid");
			Assert.That(feature.Owner, Is.EqualTo(owner), "FsComplexFeature should be created at the right place in the hierarchy");
			Assert.That(feature.CatalogSourceId, Is.EqualTo(catalogueId));
			return feature;
		}

		private IFsSymFeatVal CheckFsSymFeatVal(string guid, ICmObject owner, string catalogueId)
		{
			IFsSymFeatVal feature;
			Assert.That(Cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>().TryGetObject(new Guid(guid), out feature),
				Is.True,
				"expected FsSymFeatVal should be created with the right guid");
			Assert.That(feature.Owner, Is.EqualTo(owner), "FsSymFeatVal should be created at the right place in the hierarchy");
			Assert.That(feature.CatalogSourceId, Is.EqualTo(catalogueId));
			return feature;
		}
	}
}
