// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CellarTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Xml;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region CellarTests with real database - DON'T ADD TO THIS!
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests cellar overrides with real database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CellarTestsWithRealDb_DONTADDTOTHIS : InDatabaseFdoTestBase
	{
		const string m_ksFS1 = "<item id=\"fgender\" type=\"feature\"><abbrev ws=\"en\">Gen</abbrev><term ws=\"en\">gender</term>" +
	"<def ws=\"en\">Grammatical gender is a noun class system, composed of two or three classes,\r\nwhose nouns that have human male and female referents tend to be in separate classes.\r\nOther nouns that are classified in the same way in the language may not be classed by\r\nany correlation with natural sex distinctions.</def>" +
	"<citation>Hartmann and Stork 1972:93</citation><citation>Foley and Van Valin 1984:325</citation><citation>Mish et al. 1990:510</citation>" +
	"<citation>Crystal 1985:133</citation><citation>Dixon, R. 1968:105</citation><citation>Quirk, et al. 1985:314</citation>" +
	"<item id=\"vMasc\" type=\"value\"><abbrev ws=\"en\">Masc</abbrev><term ws=\"en\">masculine gender</term><def ws=\"en\">Masculine gender is a grammatical gender that - marks nouns having human or animal male referents, and - often marks nouns having referents that do not have distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:730</citation><fs id=\"vMascFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Masc\" /></f></fs></item><item id=\"vFem\" type=\"value\"><abbrev ws=\"en\">Fem</abbrev><term ws=\"en\">feminine gender</term><def ws=\"en\">Feminine gender is a grammatical gender that - marks nouns that have human or animal female referents, and - often marks nouns that have referents that do not carry distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:456</citation><fs id=\"vFemFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Fem\" /></f></fs></item><item id=\"vNeut\" type=\"value\"><abbrev ws=\"en\">Neut</abbrev><term ws=\"en\">neuter gender</term><def ws=\"en\">Neuter gender is a grammatical gender that - includes those nouns having referents which do not have distinctions of sex, and - often includes some which do have a natural sex distinction.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:795</citation><fs id=\"vNeutFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Neut\" /></f></fs></item><item id=\"vUnknownfgender\" type=\"value\"><abbrev ws=\"en\">?</abbrev><term ws=\"en\">unknown gender</term><fs id=\"vUnknownfgenderFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"?\" /></f></fs></item></item>";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests some error annotation stuff.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstraintErrors()
		{
			CheckDisposed();

			IMoMorphData target = m_fdoCache.LangProject.MorphologicalDataOA;
			CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_fdoCache, target.Hvo);
			Assert.AreEqual(0, CmBaseAnnotation.ErrorAnnotationsForObject(m_fdoCache, target.Hvo).Count);

			ConstraintFailure failure = new ConstraintFailure(target, 0, "testing");

			Assert.AreEqual(1, CmBaseAnnotation.ErrorAnnotationsForObject(m_fdoCache, target.Hvo).Count);

			CmBaseAnnotation.RemoveErrorAnnotationsForObject(m_fdoCache, target.Hvo);
			Assert.AreEqual(0, CmBaseAnnotation.ErrorAnnotationsForObject(m_fdoCache, target.Hvo).Count);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding closed features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddClosedFeaturesToFeatureSystemAndThenToAFeatureStructure()
		{
			ILangProject lp = Cache.LangProject;

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(m_ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");

			// Add the feature for first time
			FsFeatureSystem.AddFeatureAsXml(Cache, itemNeut);
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have one type");
			Assert.AreEqual(1, msfs.FeaturesOC.Count, "should have one feature");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				Assert.AreEqual("Agr", type.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have Agr type");
				Assert.AreEqual(1, type.FeaturesRS.Count, "Expect to have one feature in the type");
				IFsClosedFeature closed = (IFsClosedFeature)type.FeaturesRS.FirstItem;
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect name of gender");
			}
			foreach (IFsClosedFeature closed in msfs.FeaturesOC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect to have gender feature");
				foreach (IFsSymFeatVal value in closed.ValuesOC)
				{
					Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem, "Expect neuter value");
				}
			}
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			FsFeatureSystem.AddFeatureAsXml(Cache, itemFem);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have one type");
			Assert.AreEqual(1, msfs.FeaturesOC.Count, "should have one feature");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				Assert.AreEqual("Agr", type.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have Agr type");
			}
			foreach (IFsClosedFeature closed in msfs.FeaturesOC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect to have gender feature");
				Assert.AreEqual(2, closed.ValuesOC.Count, "should have two values");
				foreach (IFsSymFeatVal cv in closed.ValuesOC)
				{
					if (cv.Name.AnalysisDefaultWritingSystem != "neuter gender" &&
						cv.Name.AnalysisDefaultWritingSystem != "feminine gender")
						Assert.Fail("Unexpected value found: {0}", cv.Name.AnalysisDefaultWritingSystem);
				}
			}

			// now add to feature structure
			IPartOfSpeech pos = (IPartOfSpeech)lp.PartsOfSpeechOA.PossibilitiesOS.FirstItem;
			pos.DefaultFeaturesOA = new FsFeatStruc();
			IFsFeatStruc featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(Cache, itemNeut);
			Assert.AreEqual("Agr", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect type Agr");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsClosedValue cv in featStruct.FeatureSpecsOC)
			{
				Assert.AreEqual("Gen", cv.FeatureRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have Gen feature name");
				Assert.AreEqual("Neut", cv.ValueRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have Neut feature value");
			}
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(Cache, itemFem);
			Assert.AreEqual("Agr", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect type Agr");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsClosedValue cv in featStruct.FeatureSpecsOC)
			{
				if (cv.FeatureRA.Name.AnalysisDefaultWritingSystem != "gender" ||
					  cv.ValueRA.Name.AnalysisDefaultWritingSystem != "feminine gender")
					Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem);
			}
			// Update inflectable features on pos
			pos.AddInflectableFeatsFromXml(Cache, itemNeut);
			Assert.AreEqual(1, pos.InflectableFeatsRC.Count, "should have 1 inflectable feature in pos");
			foreach (IFsClosedFeature closed in pos.InflectableFeatsRC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "expect to find gender in pos inflectable features");
			}
			// Check for correct ShortName string in closed
			Assert.AreEqual("Fem", featStruct.ShortName, "Incorrect ShortName for closed");
			// Check for correct LongName string in complex
			Assert.AreEqual("[Gen:Fem]", featStruct.LongName, "Incorrect LongName for closed");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding complex features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddComplexFeaturesToFeatureSystemAndThenToAFeatureStructure()
		{
			ILangProject lp = Cache.LangProject;
			Assert.IsNotNull(lp.MsFeatureSystemOA, "Expect a feature system to be present");

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			string sFileDir = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory, @"FDO\FDOTests\TestData");
			string sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");

			doc.Load(sFile);
			XmlNode itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");

			// Add the feature for first time
			FsFeatureSystem.AddFeatureAsXml(Cache, itemNeut);
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have two types");
			Assert.AreEqual(2, msfs.FeaturesOC.Count, "should have two features");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				string sName = type.Name.AnalysisDefaultWritingSystem;
				if (sName != "Subject agreement")
					Assert.Fail("Unexpected fs type found: {0}", sName);
				Assert.AreEqual(1, type.FeaturesRS.Count, "Expect to have one feature in the type");
				IFsFeatDefn defn = (IFsFeatDefn)type.FeaturesRS.FirstItem;
				Assert.IsNotNull(defn, "first feature in type {0} is not null", sName);
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem, "Expect name of subject agreement");
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect to have gender feature");
					foreach (IFsSymFeatVal value in closed.ValuesOC)
					{
						Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem, "Expect neuter value");
					}
				}
			}
			foreach (IFsFeatDefn defn in msfs.FeaturesOC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
				{
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem, "Expect to have subject agreement feature");
				}
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect to have gender feature");
					foreach (IFsSymFeatVal value in closed.ValuesOC)
					{
						Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem, "Expect neuter value");
					}
				}
			}
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			FsFeatureSystem.AddFeatureAsXml(Cache, itemFem);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have two types");
			Assert.AreEqual(2, msfs.FeaturesOC.Count, "should have two features");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				string sName = type.Name.AnalysisDefaultWritingSystem;
				if (sName != "Subject agreement")
					Assert.Fail("Unexpected fs type found: {0}", sName);
			}
			foreach (IFsFeatDefn defn in msfs.FeaturesOC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
				{
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem, "Expect to have subject agreement feature");
				}
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem, "Expect to have gender feature");
					Assert.AreEqual(2, closed.ValuesOC.Count, "should have two values");
					foreach (IFsSymFeatVal cv in closed.ValuesOC)
					{
						if (cv.Name.AnalysisDefaultWritingSystem != "neuter gender" &&
							cv.Name.AnalysisDefaultWritingSystem != "feminine gender")
							Assert.Fail("Unexpected value found: {0}", cv.Name.AnalysisDefaultWritingSystem);
					}
				}
			}

			// now add to feature structure
			IPartOfSpeech pos = (IPartOfSpeech)lp.PartsOfSpeechOA.PossibilitiesOS.FirstItem;
			Assert.IsNotNull(pos, "Need one non-null pos");

			pos.DefaultFeaturesOA = new FsFeatStruc();
			IFsFeatStruc featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(Cache, itemNeut);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					Assert.AreEqual("gen", cv.FeatureRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have gen feature name");
					Assert.AreEqual("n", cv.ValueRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect to have 'n' feature value");
				}
			}
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(Cache, itemFem);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					if (cv.FeatureRA.Name.AnalysisDefaultWritingSystem != "gender" &&
						cv.ValueRA.Name.AnalysisDefaultWritingSystem != "feminine gender")
						Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem);
				}
			}
			// Now add another feature
			XmlNode item1st = doc.SelectSingleNode("//item[@id='v1']");
			featStruct.AddFeatureFromXml(Cache, item1st);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec at top feature structure");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				Assert.AreEqual(2, nestedFs.FeatureSpecsOC.Count, "should have two feature specs in nested feature structure");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					if (!(((cv.FeatureRA.Name.AnalysisDefaultWritingSystem == "gender") &&
							(cv.ValueRA.Name.AnalysisDefaultWritingSystem == "feminine gender")) ||
						  ((cv.FeatureRA.Name.AnalysisDefaultWritingSystem == "person") &&
							(cv.ValueRA.Name.AnalysisDefaultWritingSystem == "first person"))))
						Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem);
				}
			}
			// Update inflectable features on pos
			pos.AddInflectableFeatsFromXml(Cache, itemNeut);
			Assert.AreEqual(1, pos.InflectableFeatsRC.Count, "should have 1 inflectable feature in pos");
			foreach (IFsFeatDefn defn in pos.InflectableFeatsRC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem, "expect to find subject agreement in pos inflectable features");
			}
			// Check for correct ShortName string in complex
			Assert.AreEqual("1 f", featStruct.ShortName, "Incorrect ShortName for complex");
			// Check for correct LongName string in complex
			Assert.AreEqual("[sbj:[pers:1 gen:f]]", featStruct.LongName, "Incorrect LongName for complex");
			// Now add a closed feature not at the same level
			sFile = Path.Combine(sFileDir, "FeatureSystem3.xml");
			doc = null;
			doc = new XmlDocument();
			doc.Load(sFile);
			XmlNode itemAorist = doc.SelectSingleNode("//item[@id='xAor']");
			FsFeatureSystem.AddFeatureAsXml(Cache, itemAorist);
			pos.AddInflectableFeatsFromXml(Cache, itemAorist);
			featStruct.AddFeatureFromXml(Cache, itemAorist);
			// Check for correct LongName
			Assert.AreEqual("[asp:aor sbj:[pers:1 gen:f]]", featStruct.LongName, "Incorrect LongName for complex and closed");
			// Now add the features in the featurs struct in a different order
			pos.DefaultFeaturesOA = null;
			pos.DefaultFeaturesOA = new FsFeatStruc();
			featStruct = pos.DefaultFeaturesOA;
			featStruct.AddFeatureFromXml(Cache, itemAorist);
			featStruct.AddFeatureFromXml(Cache, item1st);
			featStruct.AddFeatureFromXml(Cache, itemFem);
			// check for correct short name
			Assert.AreEqual("f 1 aor", featStruct.ShortName, "Incorrect ShortName for complex");
			// Check for correct LongName
			Assert.AreEqual("[sbj:[gen:f pers:1] asp:aor]", featStruct.LongName, "Incorrect LongName for complex and closed");
		}

		/// <summary>
		/// Tests of CmAgent.SetAgentOpinion.
		/// Note that this does not verify doing and undoing setting time and details, as these are not currently publicly available.
		/// </summary>
		[Test]
		public void SetAgentOpinion()
		{
			ICmAgent agent = m_fdoCache.LangProject.DefaultComputerAgent;
			IWfiWordform wf = new WfiWordform(m_fdoCache, WfiWordform.FindOrCreateWordform(m_fdoCache,"xxxyyyzzz12234", m_fdoCache.DefaultVernWs, true));
			IWfiAnalysis wa = new WfiAnalysis();
			wf.AnalysesOC.Add(wa);
			ICmObject target = wa; // can pick anything as target for evaluation!

			m_fdoCache.BeginUndoTask("doit", "undoit");
			wa.SetAgentOpinion(agent, Opinions.approves);
			m_fdoCache.EndUndoTask();
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(agent));
			m_fdoCache.Undo();
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(agent));
			m_fdoCache.Redo();
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(agent));

			m_fdoCache.BeginUndoTask("changeit", "unchangeit");
			wa.SetAgentOpinion(agent, Opinions.disapproves);
			m_fdoCache.EndUndoTask();
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(agent));
			m_fdoCache.Undo();
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(agent));
			m_fdoCache.Redo();
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(agent));

			m_fdoCache.BeginUndoTask("clearit", "unclearit");
			wa.SetAgentOpinion(agent, Opinions.noopinion);
			m_fdoCache.EndUndoTask();
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(agent));
			m_fdoCache.Undo();
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(agent));
			m_fdoCache.Redo();
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(agent));
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests Cellar overrides
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CellarTests : InMemoryFdoTestBase
	{
		#region Data members
#if InMemoryOnly
		const string m_ksFS1 = "<item id=\"fgender\" type=\"feature\"><abbrev ws=\"en\">Gen</abbrev><term ws=\"en\">gender</term>" +
			"<def ws=\"en\">Grammatical gender is a noun class system, composed of two or three classes,\r\nwhose nouns that have human male and female referents tend to be in separate classes.\r\nOther nouns that are classified in the same way in the language may not be classed by\r\nany correlation with natural sex distinctions.</def>" +
			"<citation>Hartmann and Stork 1972:93</citation><citation>Foley and Van Valin 1984:325</citation><citation>Mish et al. 1990:510</citation>" +
			"<citation>Crystal 1985:133</citation><citation>Dixon, R. 1968:105</citation><citation>Quirk, et al. 1985:314</citation>" +
			"<item id=\"vMasc\" type=\"value\"><abbrev ws=\"en\">Masc</abbrev><term ws=\"en\">masculine gender</term><def ws=\"en\">Masculine gender is a grammatical gender that - marks nouns having human or animal male referents, and - often marks nouns having referents that do not have distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:730</citation><fs id=\"vMascFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Masc\" /></f></fs></item><item id=\"vFem\" type=\"value\"><abbrev ws=\"en\">Fem</abbrev><term ws=\"en\">feminine gender</term><def ws=\"en\">Feminine gender is a grammatical gender that - marks nouns that have human or animal female referents, and - often marks nouns that have referents that do not carry distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:456</citation><fs id=\"vFemFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Fem\" /></f></fs></item><item id=\"vNeut\" type=\"value\"><abbrev ws=\"en\">Neut</abbrev><term ws=\"en\">neuter gender</term><def ws=\"en\">Neuter gender is a grammatical gender that - includes those nouns having referents which do not have distinctions of sex, and - often includes some which do have a natural sex distinction.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:795</citation><fs id=\"vNeutFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Neut\" /></f></fs></item><item id=\"vUnknownfgender\" type=\"value\"><abbrev ws=\"en\">?</abbrev><term ws=\"en\">unknown gender</term><fs id=\"vUnknownfgenderFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"?\" /></f></fs></item></item>";
#endif
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeLexDb();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(new PartOfSpeech());
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the hide label and column width details
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserViewField_DetailsWithoutType()
		{
			m_inMemoryCache.InitializeUserViews();

			UserViewRec rec = new UserViewRec();
			Cache.UserViewSpecs.Item(0).RecordsOC.Add(rec);
			UserViewField field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.HideLabel = true;

			byte[] expected = new byte[] {0x64, 0x00, 0x00, 0x80};
			Assert.AreEqual(expected.Length, field.Details.Length);
			for (int i = 0; i < field.Details.Length; i++)
				Assert.AreEqual(expected[i], field.Details[i], "Byte " + i + " is wrong.");
			Assert.AreEqual(true, field.HideLabel);
			Assert.AreEqual(0x64, field.BrowseColumnWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the hide label, column width and type details
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserViewField_DetailsWithType()
		{
			m_inMemoryCache.InitializeUserViews();

			UserViewRec rec = new UserViewRec();
			Cache.UserViewSpecs.Item(0).RecordsOC.Add(rec);
			UserViewField field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.HideLabel = true;
			field.Type = (int)FldType.kftExpandable;
			field.ExpandOutline = true;
			field.IsHierarchy = true;
			field.PossibilityNameType = PossNameType.kpntNameAndAbbrev;

			byte[] expected = new byte[] {0x64, 0x00, 0x00, 0x80, 0x01, 0x00, 0x00, 0xC0};
			Assert.AreEqual(expected.Length, field.Details.Length);
			for (int i = 0; i < field.Details.Length; i++)
				Assert.AreEqual(expected[i], field.Details[i], "Byte " + i + " is wrong.");
			Assert.AreEqual(true, field.HideLabel);
			Assert.AreEqual(0x64, field.BrowseColumnWidth);
			Assert.AreEqual((int)FldType.kftExpandable, field.Type);
			Assert.AreEqual(true, field.ExpandOutline);
			Assert.AreEqual(true, field.IsHierarchy);
			Assert.AreEqual(PossNameType.kpntNameAndAbbrev, field.PossibilityNameType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the TaskName and ViewNameShort properties return the expected values
		/// when we set the Name property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserView_SetName()
		{
			m_inMemoryCache.InitializeUserViews();

			Cache.UserViewSpecs.Item(0).Name.SetAlternative("taskName/shortName", Cache.DefaultUserWs);

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			Assert.AreEqual("taskName", userView.TaskName);
			Assert.AreEqual("shortName", userView.ViewNameShort);
			Assert.AreEqual("taskName/shortName", userView.Name.GetAlternative(Cache.DefaultUserWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the TaskName and ViewNameShort properties return empty strings
		/// when we set the Name property to something that doesn't contain a '/' character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserView_SetName_NoTask()
		{
			m_inMemoryCache.InitializeUserViews();

			Cache.UserViewSpecs.Item(0).Name.SetAlternative("normal name", Cache.DefaultUserWs);

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			Assert.AreEqual(string.Empty, userView.TaskName);
			Assert.AreEqual(string.Empty, userView.ViewNameShort);
			Assert.AreEqual("normal name", userView.Name.GetAlternative(Cache.DefaultUserWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Name, TaskName and ViewNameShort properties return the expected values
		/// when we set the TaskName property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserView_SetTaskName()
		{
			m_inMemoryCache.InitializeUserViews();

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			userView.TaskName = "taskName";

			userView = (UserView)Cache.UserViewSpecs.Item(0);
			Assert.AreEqual("taskName", userView.TaskName);
			Assert.AreEqual(string.Empty, userView.ViewNameShort);
			Assert.AreEqual("taskName/", userView.Name.GetAlternative(Cache.DefaultUserWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the TaskName property throws an exception when we try to set it and the
		/// Name property already contains a value that doesn't contain a '/' character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType=typeof(ArgumentException))]
		public void UserView_SetTaskName_NameAlreadySet()
		{
			m_inMemoryCache.InitializeUserViews();

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			userView.Name.SetAlternative("bla", Cache.DefaultUserWs);
			userView.TaskName = "taskName";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Name, TaskName and ViewNameShort properties return the expected values
		/// when we set the ViewNameShort property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserView_ViewNameShort()
		{
			m_inMemoryCache.InitializeUserViews();

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			userView.ViewNameShort = "shortName";

			userView = (UserView)Cache.UserViewSpecs.Item(0);
			Assert.AreEqual(string.Empty, userView.TaskName);
			Assert.AreEqual("shortName", userView.ViewNameShort);
			Assert.AreEqual("/shortName", userView.Name.GetAlternative(Cache.DefaultUserWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ViewNameShort property throws an exception when we try to set it and
		/// the Name property already contains a value that doesn't contain a '/' character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType = typeof(ArgumentException))]
		public void UserView_SetViewNameShort_NameAlreadySet()
		{
			m_inMemoryCache.InitializeUserViews();

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			userView.Name.SetAlternative("bla", Cache.DefaultUserWs);
			userView.ViewNameShort = "shortName";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the TaskName and ViewNameShort properties return the expected values
		/// when we set the TaskName and ViewNameShort properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserView_ModifyName()
		{
			m_inMemoryCache.InitializeUserViews();

			UserView userView = (UserView)Cache.UserViewSpecs.Item(0);
			userView.Name.SetAlternative("taskName/shortName", Cache.DefaultUserWs);
			userView.TaskName = "New Task Name";
			userView.ViewNameShort = "New Name";

			userView = (UserView)Cache.UserViewSpecs.Item(0);
			Assert.AreEqual("New Task Name", userView.TaskName);
			Assert.AreEqual("New Name", userView.ViewNameShort);
			Assert.AreEqual("New Task Name/New Name", userView.Name.GetAlternative(Cache.DefaultUserWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the virtual handler for CmBaseAnnotation.StringValue.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnnotationStringValue()
		{
			// Use old-style creation to support in-memory testing; cf CmBaseAnnotation.CreateUnownedCba.
			CmBaseAnnotation ann = new CmBaseAnnotation();
			Cache.LangProject.AnnotationsOC.Add(ann);
			ann.BeginOffset = 4;
			ann.EndOffset = 10;
			// The below is a non-standard usage of the TextOA field. It works for this test as a place
			// to store the text we're testing, but normally it's purpose is for a (possibly) multi-paragraph
			// commentary on the target of the annotation (pointed to by BeginObject). This would be similar to
			// how a ScriptureNote.Discussion points to a JournalText (subclass of StText).
			ann.TextOA = new StText();
			StTxtPara para = new StTxtPara();
			ann.TextOA.ParagraphsOS.Append(para);
			para.Contents.Text = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			ann.BeginObjectRA = para;
			ann.EndObjectRA = para;
			ann.Flid = (int)StTxtPara.StTxtParaTags.kflidContents;

			// Install the virtual property and get its id.
			int flid = CmBaseAnnotation.StringValuePropId(Cache);

			// The text of the annotation should be 'yalola'.
			Assert.AreEqual("yalola", Cache.MainCacheAccessor.get_StringProp(ann.Hvo, flid).Text,
				"Wrong StringValue for 'yalola' annotation");
		}
	}
}
