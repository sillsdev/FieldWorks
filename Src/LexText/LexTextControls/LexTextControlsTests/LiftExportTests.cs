// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LiftExportTests.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Palaso.Lift.Validation;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using SIL.WritingSystems;

namespace LexTextControlsTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test LIFT export from FieldWorks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - Cache gets disposed in TearDown method")]
	public class LiftExportTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string kbasePictureOfTestFileName = "Picture of Test";
		private const string kpictureOfTestFileName = kbasePictureOfTestFileName + ".jpg"; // contents won't really be jpg

		private static readonly string s_sSemanticDomainsXml =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LangProject>" + Environment.NewLine +
			"<SemanticDomainList>" + Environment.NewLine +
			"<CmPossibilityList>" + Environment.NewLine +
			"<IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"<ItemClsid><Integer val=\"66\"/></ItemClsid>" + Environment.NewLine +
			"<Depth><Integer val=\"127\"/></Depth>" + Environment.NewLine +
			"<WsSelector><Integer val=\"-3\"/></WsSelector>" + Environment.NewLine +
			"<Name><AUni ws=\"en\">Semantic Domains</AUni></Name>" + Environment.NewLine +
			"<Abbreviation><AUni ws=\"en\">Sem</AUni></Abbreviation>" + Environment.NewLine +
			"<Possibilities>" + Environment.NewLine +
				"<CmSemanticDomain guid=\"63403699-07C1-43F3-A47C-069D6E4316E5\">" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">1</AUni></Abbreviation>" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Universe, creation</AUni></Name>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">Use this domain for general words referring to the physical universe. ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<OcmCodes><Uni>772 Cosmology;  130 Geography</Uni></OcmCodes>" + Environment.NewLine +
				"<LouwNidaCodes><Uni>1A Universe, Creation;  14 Physical Events and States</Uni></LouwNidaCodes>" + Environment.NewLine +
				"<Questions>" + Environment.NewLine +
				"<CmDomainQ>" + Environment.NewLine +
				"<Question><AUni ws=\"en\">(1) What words refer to everything we can see?</AUni></Question>" + Environment.NewLine +
				"<ExampleWords><AUni ws=\"en\">universe, creation, cosmos, heaven and earth, ...</AUni></ExampleWords>" + Environment.NewLine +
				"<ExampleSentences><AStr ws=\"en\"><Run ws=\"en\">In the beginning God created &lt;the heavens and the earth&gt;.</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</ExampleSentences>" + Environment.NewLine +
				"</CmDomainQ>" + Environment.NewLine +
				"</Questions>" + Environment.NewLine +

					"<SubPossibilities>" + Environment.NewLine +
						"<CmSemanticDomain guid=\"999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C\">" + Environment.NewLine +
						"<Abbreviation><AUni ws=\"en\">1.1</AUni></Abbreviation>" + Environment.NewLine +
						"<Name><AUni ws=\"en\">Sky</AUni></Name>" + Environment.NewLine +
						"<Description>" + Environment.NewLine +
						"<AStr ws=\"en\">" + Environment.NewLine +
						"<Run ws=\"en\">Use this domain for words related to the sky.</Run>" + Environment.NewLine +
						"</AStr>" + Environment.NewLine +
						"</Description>" + Environment.NewLine +
						"<LouwNidaCodes><Uni>1B Regions Above the Earth</Uni></LouwNidaCodes>" + Environment.NewLine +
						"<Questions>" + Environment.NewLine +
						"<CmDomainQ>" + Environment.NewLine +
						"<Question><AUni ws=\"en\">(1) What words are used to refer to the sky?</AUni></Question>" + Environment.NewLine +
						"<ExampleWords><AUni ws=\"en\">sky, firmament, canopy, vault</AUni></ExampleWords>" + Environment.NewLine +
						"</CmDomainQ>" + Environment.NewLine +
						"<CmDomainQ>" + Environment.NewLine +
						"<Question><AUni ws=\"en\">(2) What words refer to the air around the earth?</AUni></Question>" + Environment.NewLine +
						"<ExampleWords><AUni ws=\"en\">air, atmosphere, airspace, stratosphere, ozone layer</AUni></ExampleWords>" + Environment.NewLine +
						"</CmDomainQ>" + Environment.NewLine +
						"</Questions>" + Environment.NewLine +
						"</CmSemanticDomain>" + Environment.NewLine +

						"<CmSemanticDomain guid=\"B47D2604-8B23-41E9-9158-01526DD83894\">" + Environment.NewLine +
						"<Abbreviation><AUni ws=\"en\">1.2</AUni></Abbreviation>" + Environment.NewLine +
						"<Name><AUni ws=\"en\">World</AUni></Name>" + Environment.NewLine +
						"<Description>" + Environment.NewLine +
						"<AStr ws=\"en\">" + Environment.NewLine +
						"<Run ws=\"en\">Use this domain for words referring to the planet we live on.</Run>" + Environment.NewLine +
						"</AStr>" + Environment.NewLine +
						"</Description>" + Environment.NewLine +
						"<Questions>" + Environment.NewLine +
						"<CmDomainQ>" + Environment.NewLine +
						"<Question><AUni ws=\"en\">(1) What words refer to the planet we live on?</AUni></Question>" + Environment.NewLine +
						"<ExampleWords><AUni ws=\"en\">the world, earth, the Earth, the globe, the planet</AUni></ExampleWords>" + Environment.NewLine +
						"</CmDomainQ>" + Environment.NewLine +
						"<CmDomainQ>" + Environment.NewLine +
						"<Question><AUni ws=\"en\">(2) What words describe something that belongs to this world?</AUni></Question>" + Environment.NewLine +
						"<ExampleWords><AUni ws=\"en\">earthly, terrestrial</AUni></ExampleWords>" + Environment.NewLine +
						"</CmDomainQ>" + Environment.NewLine +
						"</Questions>" + Environment.NewLine +
						"</CmSemanticDomain>" + Environment.NewLine +
					"</SubPossibilities>" + Environment.NewLine +
				"</CmSemanticDomain>" + Environment.NewLine +

				"<CmSemanticDomain guid=\"BA06DE9E-63E1-43E6-AE94-77BEA498379A\">" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">2</AUni></Abbreviation>" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Person</AUni></Name>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">Use this domain for general words for a person or all mankind.</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<LouwNidaCodes><Uni>9 People;  9A Human Beings</Uni></LouwNidaCodes>" + Environment.NewLine +
				"<Questions>" + Environment.NewLine +
				"<CmDomainQ>" + Environment.NewLine +
				"<Question><AUni ws=\"en\">(1) What words refer to a single member of the human race?</AUni></Question>" + Environment.NewLine +
				"<ExampleWords><AUni ws=\"en\">person, human being, man, individual, figure</AUni></ExampleWords>" + Environment.NewLine +
				"</CmDomainQ>" + Environment.NewLine +
				"<CmDomainQ>" + Environment.NewLine +
				"<Question><AUni ws=\"en\">(2) What words refer to a person when you aren't sure who the person is?</AUni></Question>" + Environment.NewLine +
				"<ExampleWords><AUni ws=\"en\">someone, somebody</AUni></ExampleWords>" + Environment.NewLine +
				"</CmDomainQ>" + Environment.NewLine +
				"</Questions>" + Environment.NewLine +

				"<SubPossibilities>" + Environment.NewLine +
					"<CmSemanticDomain guid=\"1B0270A5-BABF-4151-99F5-279BA5A4B044\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.1</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Body</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for general words for the whole human body, ...</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>140 Human Biology;  141 Anthropometry;  142 Descriptive Somatology</Uni></OcmCodes>" + Environment.NewLine +
					"<LouwNidaCodes><Uni>8 Body, Body Parts, and Body Products;  8A Body;  8B Parts of the Body</Uni></LouwNidaCodes>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What words refer to the body?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">body, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(2) What words refer to the shape of a person's body?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">build, figure, physique, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +

					"<CmSemanticDomain guid=\"7FE69C4C-2603-4949-AFCA-F39C010AD24E\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.2</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Body functions</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for the functions and actions of the whole body. ...</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>147 Physiological Data;  514 Elimination</Uni></OcmCodes>" + Environment.NewLine +
					"<LouwNidaCodes><Uni>8C Physiological Products of the Body;  23 Physiological Processes and States</Uni></LouwNidaCodes>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What general words refer to the functions of the body?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">function</AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(2) What general words refer to secretions of the body?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">secrete, secretion, excrete, excretion, product, fluid, body fluids, discharge, flux, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +

					"<CmSemanticDomain guid=\"38BBB33A-90BF-4A2C-A0E5-4BDE7E134BD9\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.3</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Sense, perceive</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for general words related to all the senses--sight, hearing, ...</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>151 Sensation and Perception</Uni></OcmCodes>" + Environment.NewLine +
					"<LouwNidaCodes><Uni>24 Sensory Events and States;  24G General Sensory Perception</Uni></LouwNidaCodes>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What words refer to sensing something using one of the senses?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">feel, sense, perceive, notice, detect, distinguish</AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(2) What words refer to the ability to sense something?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">sense, perception, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +

					"<CmSemanticDomain guid=\"F7706644-542F-4FCB-B8E1-E91D04C8032A\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.4</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Body condition</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for general words related to the condition of the body.</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>147 Physiological Data</Uni></OcmCodes>" + Environment.NewLine +
					"<RelatedDomains>" + Environment.NewLine +
					"<Link guid=\"32BEBE7E-BDCC-4E40-8F0A-894CD6B26F25\"/>" + Environment.NewLine +
					"</RelatedDomains>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What general words refer to the condition of the body?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">condition, state, shape (as in 'to be in shape')</AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +

					"<CmSemanticDomain guid=\"32BEBE7E-BDCC-4E40-8F0A-894CD6B26F25\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.5</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Healthy</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for words related to a person being healthy--not sick.</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>147 Physiological Data;  740 Health and Welfare</Uni></OcmCodes>" + Environment.NewLine +
					"<LouwNidaCodes><Uni>23H Health, Vigor, Strength</Uni></LouwNidaCodes>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What words describe a person who is healthy?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">healthy, well, fine, in good health, able-bodied, hale, sound, whole, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(2) What words describe someone who is healthy and doesn't get sick?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">robust, have a strong constitution, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +

					"<CmSemanticDomain guid=\"50DB27B5-89EB-4FFB-AF82-566F51C8EC0B\">" + Environment.NewLine +
					"<Abbreviation><AUni ws=\"en\">2.6</AUni></Abbreviation>" + Environment.NewLine +
					"<Name><AUni ws=\"en\">Life</AUni></Name>" + Environment.NewLine +
					"<Description>" + Environment.NewLine +
					"<AStr ws=\"en\">" + Environment.NewLine +
					"<Run ws=\"en\">Use this domain for general words referring to being alive and to a person's lifetime.</Run>" + Environment.NewLine +
					"</AStr>" + Environment.NewLine +
					"</Description>" + Environment.NewLine +
					"<OcmCodes><Uni>761 Life and Death;  159 Life History Materials</Uni></OcmCodes>" + Environment.NewLine +
					"<LouwNidaCodes><Uni>23G Live, Die</Uni></LouwNidaCodes>" + Environment.NewLine +
					"<Questions>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(1) What words refer to being alive?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">be alive, live, life, stay alive, be going strong, </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"<CmDomainQ>" + Environment.NewLine +
					"<Question><AUni ws=\"en\">(2) What words describe someone who is alive?</AUni></Question>" + Environment.NewLine +
					"<ExampleWords><AUni ws=\"en\">living, alive, animate (adj), </AUni></ExampleWords>" + Environment.NewLine +
					"</CmDomainQ>" + Environment.NewLine +
					"</Questions>" + Environment.NewLine +
					"</CmSemanticDomain>" + Environment.NewLine +
				"</SubPossibilities>" + Environment.NewLine +

				"</CmSemanticDomain>" + Environment.NewLine +
			"</Possibilities>" + Environment.NewLine +
			"</CmPossibilityList>" + Environment.NewLine +
			"</SemanticDomainList>" + Environment.NewLine +
			"</LangProject>" + Environment.NewLine;

		private static readonly string s_sPartsOfSpeech =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LangProject>" + Environment.NewLine +
			"<PartsOfSpeech>" + Environment.NewLine +
			"<CmPossibilityList>" + Environment.NewLine +
			"<Depth><Integer val=\"127\"/></Depth>" + Environment.NewLine +
			"<IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"<UseExtendedFields><Boolean val=\"true\"/></UseExtendedFields>" + Environment.NewLine +
			"<ItemClsid><Integer val=\"5049\"/></ItemClsid>" + Environment.NewLine +
			"<WsSelector><Integer val=\"-3\"/></WsSelector>" + Environment.NewLine +
			"<Name><AUni ws=\"en\">Parts Of Speech</AUni></Name>" + Environment.NewLine +
			"<Abbreviation><AUni ws=\"en\">Pos</AUni></Abbreviation>" + Environment.NewLine +
			"<Possibilities>" + Environment.NewLine +
				"<PartOfSpeech guid=\"46e4fe08-ffa0-4c8b-bf98-2c56f38904d9\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Adverb</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">adv</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">An adverb, narrowly defined, is a part of speech ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<CatalogSourceId><Uni>Adverb</Uni></CatalogSourceId>" + Environment.NewLine +
				"</PartOfSpeech>" + Environment.NewLine +

				"<PartOfSpeech guid=\"a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Noun</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">n</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">A noun is a broad classification of parts of speech ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<CatalogSourceId><Uni>Noun</Uni></CatalogSourceId>" + Environment.NewLine +
				"</PartOfSpeech>" + Environment.NewLine +

				"<PartOfSpeech guid=\"a4fc78d6-7591-4fb3-8edd-82f10ae3739d\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Pro-form</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">pro-form</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">A pro-form is a part of speech ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<CatalogSourceId><Uni>Pro-form</Uni></CatalogSourceId>" + Environment.NewLine +
					"<SubPossibilities>" + Environment.NewLine +
						"<PartOfSpeech guid=\"a3274cfd-225f-45fd-8851-a7b1a1e1037a\">" + Environment.NewLine +
						"<Name><AUni ws=\"en\">Pronoun</AUni></Name>" + Environment.NewLine +
						"<Abbreviation><AUni ws=\"en\">pro</AUni></Abbreviation>" + Environment.NewLine +
						"<Description>" + Environment.NewLine +
						"<AStr ws=\"en\">" + Environment.NewLine +
						"<Run ws=\"en\">A pronoun is a pro-form which ...</Run>" + Environment.NewLine +
						"</AStr>" + Environment.NewLine +
						"</Description>" + Environment.NewLine +
						"<CatalogSourceId><Uni>Pronoun</Uni></CatalogSourceId>" + Environment.NewLine +
						"</PartOfSpeech>" + Environment.NewLine +
					"</SubPossibilities>" + Environment.NewLine +
				"</PartOfSpeech>" + Environment.NewLine +

				"<PartOfSpeech guid=\"86ff66f6-0774-407a-a0dc-3eeaf873daf7\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">Verb</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">v</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">A verb is a part of speech ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"<CatalogSourceId><Uni>Verb</Uni></CatalogSourceId>" + Environment.NewLine +
				"</PartOfSpeech>" + Environment.NewLine +
			"</Possibilities>" + Environment.NewLine +
			"</CmPossibilityList>" + Environment.NewLine +
			"</PartsOfSpeech>" + Environment.NewLine +
			"</LangProject>" + Environment.NewLine;

		private static readonly string s_sPublications =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LexDb>" + Environment.NewLine +
			"<PublicationTypes>" + Environment.NewLine +
			"<CmPossibilityList>" + Environment.NewLine +
			"<Depth><Integer val=\"1\"/></Depth>" + Environment.NewLine +
			"<IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"<ItemClsid><Integer val=\"7\"/></ItemClsid>" + Environment.NewLine +
			"<WsSelector><Integer val=\"0\"/></WsSelector>" + Environment.NewLine +
			"<Name><AUni ws=\"en\">Publications</AUni></Name>" + Environment.NewLine +
			"<Possibilities>" + Environment.NewLine +
				// Main Dictionary is already there and doesn't need to be added

				"<CmPossibility>" + Environment.NewLine +
				"<Name><AUni ws=\"en\">School</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">Sch</AUni></Abbreviation>" + Environment.NewLine +
				"</CmPossibility>" + Environment.NewLine +
			"</Possibilities>" + Environment.NewLine +
			"</CmPossibilityList>" + Environment.NewLine +
			"</PublicationTypes>" + Environment.NewLine +
			"</LexDb>" + Environment.NewLine;

		private static readonly string s_sAcademicDomains =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LexDb>" + Environment.NewLine +
			"<DomainTypes>" + Environment.NewLine +

			"<CmPossibilityList>" + Environment.NewLine +
			"<IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"<ItemClsid><Integer val=\"7\"/></ItemClsid>" + Environment.NewLine +
			"<Depth><Integer val=\"127\"/></Depth>" + Environment.NewLine +
			"<WsSelector><Integer val=\"-3\"/></WsSelector>" + Environment.NewLine +
			"<Name><AUni ws=\"en\">Academic Domains</AUni></Name>" + Environment.NewLine +
			"<Abbreviation><AUni ws=\"en\">AcaDom</AUni></Abbreviation>" + Environment.NewLine +

			"<Possibilities>" + Environment.NewLine +
				"<CmPossibility guid=\"013E9C64-93C1-4e71-B535-936B6F5EDC23\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">computer science</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">Comp sci</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">comp sci is ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"</CmPossibility>" + Environment.NewLine +

				"<CmPossibility guid=\"F9CC2574-1932-4f32-B9A4-6BF6AFC7DEEA\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">anatomy</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">Anat</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">anatomy is ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"</CmPossibility>" + Environment.NewLine +

				"<CmPossibility guid=\"E6FDF895-2228-4854-8721-EA38D9B7FE3B\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">literature</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">Lit</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">literature is  ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
					"<SubPossibilities>" + Environment.NewLine +
						"<CmPossibility guid=\"29F92883-84B3-4d05-80F2-FB0EE441D1D3\">" + Environment.NewLine +
						"<Name><AUni ws=\"en\">poetry</AUni></Name>" + Environment.NewLine +
						"<Abbreviation><AUni ws=\"en\">Poet</AUni></Abbreviation>" + Environment.NewLine +
						"<Description>" + Environment.NewLine +
						"<AStr ws=\"en\">" + Environment.NewLine +
						"<Run ws=\"en\">poetry is ...</Run>" + Environment.NewLine +
						"</AStr>" + Environment.NewLine +
						"</Description>" + Environment.NewLine +
						"</CmPossibility>" + Environment.NewLine +
						"<CmPossibility guid=\"603F3823-692A-4432-AA53-633F367FD778\">" + Environment.NewLine +
						"<Name><AUni ws=\"en\">rhetoric</AUni></Name>" + Environment.NewLine +
						"<Abbreviation><AUni ws=\"en\">Rhet</AUni></Abbreviation>" + Environment.NewLine +
						"<Description>" + Environment.NewLine +
						"<AStr ws=\"en\">" + Environment.NewLine +
						"<Run ws=\"en\">rhetoric is ...</Run>" + Environment.NewLine +
						"</AStr>" + Environment.NewLine +
						"</Description>" + Environment.NewLine +
						"</CmPossibility>" + Environment.NewLine +
					"</SubPossibilities>" + Environment.NewLine +
				"</CmPossibility>" + Environment.NewLine +

				"<CmPossibility guid=\"AEEBBA64-EBCF-42a8-A55C-92F0B4A55F62\">" + Environment.NewLine +
				"<Name><AUni ws=\"en\">medicine</AUni></Name>" + Environment.NewLine +
				"<Abbreviation><AUni ws=\"en\">Medi</AUni></Abbreviation>" + Environment.NewLine +
				"<Description>" + Environment.NewLine +
				"<AStr ws=\"en\">" + Environment.NewLine +
				"<Run ws=\"en\">medicine is ...</Run>" + Environment.NewLine +
				"</AStr>" + Environment.NewLine +
				"</Description>" + Environment.NewLine +
				"</CmPossibility>" + Environment.NewLine +

			"</Possibilities>" + Environment.NewLine +

			"</CmPossibilityList>" + Environment.NewLine +

			"</DomainTypes>" + Environment.NewLine +
			"</LexDb>" + Environment.NewLine;

		private FdoCache m_cache;
		private readonly Dictionary<string, ICmSemanticDomain> m_mapSemanticDomains =
			new Dictionary<string, ICmSemanticDomain>();
		private readonly Dictionary<string, IPartOfSpeech> m_mapPartsOfSpeech =
			new Dictionary<string, IPartOfSpeech>();
		private readonly Dictionary<string, ICmPossibility> m_mapAcademicDomains =
			new Dictionary<string, ICmPossibility>();
		private readonly Dictionary<string, ICmPossibility> m_mapPublications =
			new Dictionary<string, ICmPossibility>();

		private string MockProjectFolder { get; set; }
		private string MockLinkedFilesFolder { get; set; }
		private int m_audioWsCode;

		private ISilDataAccessManaged m_sda;

		#region Setup and Helper Methods

		/// <summary>
		/// Setup method: create a memory-only mock cache and empty language project.
		/// </summary>
		[SetUp]
		public void CreateMockCache()
		{
			m_mapSemanticDomains.Clear();
			m_mapPartsOfSpeech.Clear();
			m_mapAcademicDomains.Clear();
			m_mapPublications.Clear();
			var mockProjectName = "xxyyzProjectFolderForLIFTTest";
			MockProjectFolder = Path.Combine(Path.GetTempPath(), mockProjectName);
			var mockProjectPath = Path.Combine(MockProjectFolder, mockProjectName + ".fwdata");
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, mockProjectPath), "en", "fr", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings());
			MockLinkedFilesFolder = Path.Combine(MockProjectFolder, FdoFileHelper.ksLinkedFilesDir);
			Directory.CreateDirectory(MockLinkedFilesFolder);
			//m_cache.LangProject.LinkedFilesRootDir = MockLinkedFilesFolder; this is already the default.

			WritingSystemManager writingSystemManager = m_cache.ServiceLocator.WritingSystemManager;
			LanguageSubtag languageSubtag = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Language;
			//var voiceTag = RFC5646Tag.RFC5646TagForVoiceWritingSystem(languageSubtag.Name, "");
			CoreWritingSystemDefinition audioWs = writingSystemManager.Create(languageSubtag,
				WellKnownSubtags.AudioScript, null, new VariantSubtag[] {WellKnownSubtags.AudioPrivateUse});
			audioWs.IsVoice = true; // should already be so? Make sure.
			writingSystemManager.Set(audioWs); // gives it a handle
			m_audioWsCode = audioWs.Handle;

			var semList = new XmlList();
			using (var reader = new StringReader(s_sSemanticDomainsXml))
				semList.ImportList(m_cache.LangProject, "SemanticDomainList", reader, null);
			var repoSem = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			foreach (var sem in repoSem.AllInstances())
				m_mapSemanticDomains.Add(sem.ShortName, sem);

			CreatePartsOfSpeechPossibilityList();
			CreateAcademicDomainsPossibilityList();
			CreatePublicationsList();

			AddLexEntries();
			CreateDirectoryForTest();
		}

		private void CreateDirectoryForTest()
		{
			LiftFolder = Path.Combine(Path.GetTempPath(), Path.Combine("LiftExportTests", Path.GetRandomFileName()));
			Directory.CreateDirectory(LiftFolder);
		}

		private void DestroyTestDirectory()
		{
			if(Directory.Exists(LiftFolder))
				Directory.Delete(LiftFolder, true);
		}

		private void CreatePartsOfSpeechPossibilityList()
		{
			var posList = new XmlList();
			using (var reader = new StringReader(s_sPartsOfSpeech))
				posList.ImportList(m_cache.LangProject, "PartsOfSpeech", reader, null);
			var repoPos = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>();
			foreach (var pos in repoPos.AllInstances())
				m_mapPartsOfSpeech.Add(pos.ShortName.ToLowerInvariant(), pos);
		}

		private void CreatePublicationsList()
		{
			var publicationList = new XmlList();
			using (var reader = new StringReader(s_sPublications))
				publicationList.ImportList(m_cache.LangProject.LexDbOA, "PublicationTypes", reader, null);
			var repoPub = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
			foreach (var publication in m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS)
				m_mapPublications.Add(publication.Name.AnalysisDefaultWritingSystem.Text, publication);
		}

		private void CreateAcademicDomainsPossibilityList()
		{
			var academicDomainsList = new XmlList();
			using (var reader = new StringReader(s_sAcademicDomains))
				academicDomainsList.ImportList(m_cache.LangProject.LexDbOA, "DomainTypes", reader, null);
			foreach (var pos in m_cache.LangProject.LexDbOA.DomainTypesOA.ReallyReallyAllPossibilities)
				m_mapAcademicDomains.Add(pos.ShortName.ToLowerInvariant(), pos);
		}

		private ILexEntry m_entryTest;
		private ILexEntry m_entryThis;
		private ILexEntry m_entryIs;
		private ILexEntry m_entryUnbelieving;

		private static string CreateDummyFile(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Delete(path);
			using (var wrtr = File.CreateText(path))
			{
				wrtr.WriteLine("This is a dummy file used in testing LIFT export");
				wrtr.Close();
			}
			return path;
		}

		private const string ksubFolderName = "sub";
		private const string kotherPicOfTestFileName = "Another picture of test.jpg";
		private const string kaudioFileName = "Sound of test.wav";
		private const string kpronunciationFileName = "Pronunciation of test.wav";
		private const string klexemeFormFileName = "Form of test.wav";
		private const string kotherLinkedFileName = "File linked to in defn of test.doc";
		private const string kcitationFormFileName = "Citation form test.wav";
		private const string kcustomMultiFileName = "Custom multilingual file.wav";

		private void AddLexEntries()
		{
			var entryFact = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var msaNoun = new SandboxGenericMSA { MsaType = MsaType.kRoot, MainPOS = m_mapPartsOfSpeech["noun"] };
			var msaPronoun = new SandboxGenericMSA { MsaType = MsaType.kRoot, MainPOS = m_mapPartsOfSpeech["pronoun"] };
			var msaVerb = new SandboxGenericMSA { MsaType = MsaType.kRoot, MainPOS = m_mapPartsOfSpeech["verb"] };
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_entryTest = entryFact.Create("test & trouble", "trials & tribulations", msaNoun);
					m_entryTest.CitationForm.VernacularDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("citation", m_cache.DefaultVernWs);
					m_entryTest.Bibliography.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("bibliography entry", m_cache.DefaultAnalWs);
					m_entryTest.Comment.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("I like this comment.", m_cache.DefaultAnalWs);
					m_entryTest.LiteralMeaning.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("Literally we need this.", m_cache.DefaultAnalWs);
					m_entryTest.Restrictions.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("There are some restrictions on where this can be used.",
														m_cache.DefaultAnalWs);
					m_entryTest.SummaryDefinition.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("In summary dot dot dot.", m_cache.DefaultAnalWs);
					m_entryTest.DoNotPublishInRC.Add(m_mapPublications["Main Dictionary"]);

					var tssDefn = m_cache.TsStrFactory.MakeString("Definition for sense.\x2028Another para of defn", m_cache.DefaultAnalWs);
					var bldr = tssDefn.GetBldr();
					int len = bldr.Length;
					var otherFileFolder = Path.Combine(MockLinkedFilesFolder, FdoFileHelper.ksOtherLinkedFilesDir);
					var otherFilePath = Path.Combine(otherFileFolder, kotherLinkedFileName);
					CreateDummyFile(otherFilePath);
					var mockStyle = new MockStyle() { Name = "hyperlink" };
					StringServices.MarkTextInBldrAsHyperlink(bldr, len - 4, len, otherFilePath, mockStyle, MockLinkedFilesFolder);

					var ls = m_entryTest.SensesOS[0];
					ls.Definition.AnalysisDefaultWritingSystem = bldr.GetString();
					ls.AnthroNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("Anthro Note.", m_cache.DefaultAnalWs);
					ls.Bibliography.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense Bibliography", m_cache.DefaultAnalWs);
					ls.DiscourseNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense Discoursing away...", m_cache.DefaultAnalWs);
					ls.EncyclopedicInfo.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense EncyclopedicInfo", m_cache.DefaultAnalWs);
					ls.GeneralNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense GeneralNote", m_cache.DefaultAnalWs);
					ls.GrammarNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense GrammarNote", m_cache.DefaultAnalWs);
					ls.PhonologyNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense PhonologyNote", m_cache.DefaultAnalWs);
					ls.Restrictions.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense Restrictions", m_cache.DefaultAnalWs);
					ls.SemanticsNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense SemanticsNote", m_cache.DefaultAnalWs);
					ls.SocioLinguisticsNote.AnalysisDefaultWritingSystem =
						m_cache.TsStrFactory.MakeString("sense SocioLinguisticsNote", m_cache.DefaultAnalWs);
					ls.DoNotPublishInRC.Add(m_mapPublications["School"]);
					m_entryTest.LiftResidue =
						"<lift-residue id=\"songanganya & nganga_63698066-52d6-46bd-8438-64ce2a820dc6\" dateCreated=\"2008-04-27T22:41:26Z\" dateModified=\"2007-07-02T17:00:00Z\"></lift-residue>";

					//Add an academic domain to the sense.
					ICmPossibility possibility;
					AddAcademicDomain(ls, "rhetoric");
					AddAcademicDomain(ls, "computer science");
					AddAcademicDomain(ls, "medicine");

					m_entryThis = entryFact.Create("this", "this", msaPronoun);
					m_entryIs = entryFact.Create("is", "to.be", msaVerb);

					var picFolder = m_cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
					m_cache.LangProject.PicturesOC.Add(picFolder);

					// Verify that picture files get copied to the right places, and how we handle various
					// kinds of source location.
					var picturesFolderPath = Path.Combine(MockLinkedFilesFolder, "Pictures");
					Directory.CreateDirectory(picturesFolderPath);
					var subfolder = Path.Combine(picturesFolderPath, ksubFolderName);

					MakePicture(picFolder, Path.Combine(picturesFolderPath, kpictureOfTestFileName));
					MakePicture(picFolder, Path.Combine(MockLinkedFilesFolder, kpictureOfTestFileName));
					MakePicture(picFolder, Path.Combine(subfolder, kotherPicOfTestFileName));
					m_tempPictureFilePath = Path.GetTempFileName();
					MakePicture(picFolder, m_tempPictureFilePath);

					// See if we can export audio writing system stuff.
					var audioFolderPath = Path.Combine(MockLinkedFilesFolder, FdoFileHelper.ksMediaDir);
					CreateDummyFile(Path.Combine(audioFolderPath, kaudioFileName));
					m_entryTest.SensesOS[0].Definition.set_String(m_audioWsCode, kaudioFileName);

					// Lexeme form is a special case
					CreateDummyFile(Path.Combine(audioFolderPath, klexemeFormFileName));
					m_entryTest.LexemeFormOA.Form.set_String(m_audioWsCode, klexemeFormFileName);
					// Citation form is written in a different way. Test it too.
					CreateDummyFile(Path.Combine(audioFolderPath, kcitationFormFileName));
					m_entryTest.CitationForm.set_String(m_audioWsCode, kcitationFormFileName);
					// Set this as a value later, when we create custom fields.
					CreateDummyFile(Path.Combine(audioFolderPath, kcustomMultiFileName));

					// Try a pronunciation media file.
					var pronunciationPath = Path.Combine(audioFolderPath, kpronunciationFileName);
					CreateDummyFile(pronunciationPath);
					var pronunciation = m_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
					m_entryTest.PronunciationsOS.Add(pronunciation);
					var media = m_cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
					pronunciation.MediaFilesOS.Add(media);
					var pronunFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
					picFolder.FilesOC.Add(pronunFile); // maybe not quite appropriate, but has to be owned somewhere.
					media.MediaFileRA = pronunFile;
					pronunFile.InternalPath = Path.Combine(FdoFileHelper.ksMediaDir, kpronunciationFileName);

					// We should be able to export LexEntryRefs. BaseForm is a special case.
					var entryUn = entryFact.Create("un", "not", new SandboxGenericMSA() { MsaType = MsaType.kDeriv });
					var entryBelieve = entryFact.Create("believe", "believe", msaVerb);
					var entryIng = entryFact.Create("ing", "with property", new SandboxGenericMSA() { MsaType = MsaType.kDeriv });
					m_entryUnbelieving = entryFact.Create("unbelieving", "not believing", msaNoun); // not really a noun, I know
					var ler1 = MakeComplexFormEntryRef(m_entryUnbelieving, new[] { entryUn, entryBelieve, entryIng },
						"Compound");
					ler1.PrimaryLexemesRS.Add(entryBelieve);
					var ler2 = MakeComplexFormEntryRef(m_entryUnbelieving, new[] { entryBelieve }, "BaseForm");
					ler2.PrimaryLexemesRS.Add(entryBelieve);

					var otherFolderPath = Path.Combine(MockLinkedFilesFolder, "Others");

					// one of these is an example and won't be published in either Publication
					AddCustomFields();
				});
		}

		private void AddAcademicDomain(ILexSense ls, String domain)
		{
			ICmPossibility possibility;
			if (m_mapAcademicDomains.TryGetValue(domain, out possibility))
			{
				if (!ls.DomainTypesRC.Contains(possibility))
					ls.DomainTypesRC.Add(possibility);
			}
		}

		private ILexEntryRef MakeComplexFormEntryRef(ILexEntry entryUnbelieving, ILexEntry[] components, string complexFormType)
		{
			var ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entryUnbelieving.EntryRefsOS.Add(ler);
			ler.RefType = LexEntryRefTags.krtComplexForm;
			foreach (var entry in components)
				ler.ComponentLexemesRS.Add(entry);
			if (m_cache.LangProject.LexDbOA.ComplexEntryTypesOA == null)
				m_cache.LangProject.LexDbOA.ComplexEntryTypesOA = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var entryType = (from et in m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
							 where et.Name.AnalysisDefaultWritingSystem.Text == complexFormType
							 select et).FirstOrDefault() as ILexEntryType;
			if (entryType == null)
			{
				entryType = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create();
				m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Add(entryType);
				entryType.Name.AnalysisDefaultWritingSystem = m_cache.TsStrFactory.MakeString(complexFormType, m_cache.DefaultAnalWs);
			}
			ler.ComplexEntryTypesRS.Add(entryType);
			return ler;
		}

		private void MakePicture(ICmFolder picFolder, string testPicturePath)
		{
			CreateDummyFile(testPicturePath);
			var picture1 = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			m_entryTest.SensesOS[0].PicturesOS.Add(picture1);
			var pictureFile1 = m_cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			picFolder.FilesOC.Add(pictureFile1);
			picture1.PictureFileRA = pictureFile1;
			pictureFile1.InternalPath = testPicturePath;
		}

		private List<int> m_customFieldEntryIds = new List<int>();
		private List<int> m_customFieldSenseIds = new List<int>();
		private List<int> m_customFieldAllomorphsIds = new List<int>();
		private List<int> m_customFieldExampleSentencesIds = new List<int>();
		private List<Guid> m_customListsGuids = new List<Guid>();
		private ICmPossibilityList m_customPossibilityList;

		private void AddCustomFields()
		{
			m_sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Assert.IsNotNull(m_sda);
			//---------------------------------------------------------------------------------------------------
			AddCustomFieldsInLexEntry();
			//---------------------------------------------------------------------------------------------------
			AddCustomFieldsInLexSense();
			//---------------------------------------------------------------------------------------------------
			AddCustomFieldsInExampleSentence();
			//---------------------------------------------------------------------------------------------------
			AddCustomFieldInAllomorph();
			//---------------------------------------------------------------------------------------------------
		}

		private void AddCustomFieldsInLexEntry()
		{
			var fd = MakeCustomField("CustomField1-LexEntry", LexEntryTags.kClassId,
									 WritingSystemServices.kwsAnal, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldEntryIds.Add(fd.Id);
			AddCustomFieldSimpleString(fd, m_cache.DefaultAnalWs, m_entryTest.Hvo);

			fd = MakeCustomField("CustomField2-LexEntry", LexEntryTags.kClassId,
								 WritingSystemServices.kwsVernAnals, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldEntryIds.Add(fd.Id);
			AddCustomFieldMultistringText(fd, m_entryTest.Hvo);
			m_cache.DomainDataByFlid.SetMultiStringAlt(m_entryTest.Hvo, fd.Id, m_audioWsCode,
				m_cache.TsStrFactory.MakeString(kcustomMultiFileName, m_audioWsCode));

			//---------------------------------------------------------------------------------------------------
			fd = MakeCustomField("CustomField3-LexEntry Date", LexEntryTags.kClassId,
								 WritingSystemServices.kwsAnal, CustomFieldType.Date, Guid.Empty);
			m_customFieldEntryIds.Add(fd.Id);
			var date = DateTime.Now;
			var genDate = new GenDate(GenDate.PrecisionType.Approximate, date.Month, date.Day, date.Year, true);
			m_sda.SetGenDate(m_entryTest.Hvo, fd.Id, genDate);

			//---------------------------------------------------------------------------------------------------
			fd = MakeCustomField("CustomField3-LexEntry CmPossibilitySemanticDomain", LexEntryTags.kClassId, WritingSystemServices.kwsAnal,
				CustomFieldType.ListRefAtomic, m_cache.LangProject.SemanticDomainListOA.Guid);
			m_customFieldEntryIds.Add(fd.Id);
			var repoSem = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			var firstSemEntry = repoSem.AllInstances().First();
			m_sda.SetObjProp(m_entryTest.Hvo, fd.Id, firstSemEntry.Hvo);

			//---------------------------------------------------------------------------------------------------
			fd = MakeCustomField("CustomField4-LexEntry ListRefCollection", LexEntryTags.kClassId, WritingSystemServices.kwsAnal,
				CustomFieldType.ListRefCollection, m_cache.LangProject.SemanticDomainListOA.Guid);
			m_customFieldEntryIds.Add(fd.Id);
			//Collect a few items from the semantic domains list and add them to the entry
			int i = 0;
			var listRefCollectionItems = new int[3];
			foreach (var listItem in repoSem.AllInstances())
			{
				listRefCollectionItems[i] = listItem.Hvo;
				i++;
				if (i == 3)
					break;
			}
			m_sda.Replace(m_entryTest.Hvo, fd.Id, 0, 0, listRefCollectionItems, 3);

			//------------------------------------------------------------------------------------------------------------
			m_customPossibilityList = AddCustomList();
			//---------------------------------------------------------------------------------------------------
			fd = MakeCustomField("CustomField5-LexEntry CmPossibilityCustomList", LexEntryTags.kClassId, WritingSystemServices.kwsAnal,
				CustomFieldType.ListRefAtomic, m_customPossibilityList.Guid);
			m_customFieldEntryIds.Add(fd.Id);
			var firstCustomListEntry = m_customPossibilityList.FindOrCreatePossibility("list item 1", m_cache.DefaultAnalWs);
			m_sda.SetObjProp(m_entryTest.Hvo, fd.Id, firstCustomListEntry.Hvo);

			fd = MakeCustomField("CustomField6-LexEntry", LexEntryTags.kClassId,
					 WritingSystemServices.kwsVernAnals, CustomFieldType.SingleLineString, Guid.Empty);
			m_customFieldEntryIds.Add(fd.Id);
			AddCustomFieldMultistringText(fd, m_entryTest.Hvo);
			m_cache.DomainDataByFlid.SetMultiStringAlt(m_entryTest.Hvo, fd.Id, m_audioWsCode,
				m_cache.TsStrFactory.MakeString(kcustomMultiFileName, m_audioWsCode));
		}

		private void AddCustomFieldSimpleString(FieldDescription fd, int ws, int hvo)
		{
			var tss = m_cache.TsStrFactory.MakeString(fd.Userlabel + " text.", ws);
			var bldr = tss.GetBldr();
			bldr.SetIntPropValues(5, 10, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws == m_cache.DefaultVernWs ? m_cache.DefaultAnalWs : m_cache.DefaultVernWs);
			m_cache.DomainDataByFlid.SetString(hvo, fd.Id, bldr.GetString());
		}

		private void AddCustomFieldsInLexSense()
		{
			// create new custom field in LexSense
			var fd = MakeCustomField("CustomField1-LexSense", LexSenseTags.kClassId,
									 WritingSystemServices.kwsVern, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldSenseIds.Add(fd.Id);
			AddCustomFieldSimpleString(fd, m_cache.DefaultVernWs, m_entryTest.SensesOS[0].Hvo);

			//---------------------------------------------------------------------------------------------------
			fd = MakeCustomField("CustomField2-LexSense Integer", LexSenseTags.kClassId,
								 WritingSystemServices.kwsAnal, CustomFieldType.Number, Guid.Empty);
			m_customFieldSenseIds.Add(fd.Id);
			//Now put some data in the Integer custom field.
			m_cache.DomainDataByFlid.SetInt(m_entryTest.SensesOS[0].Hvo, fd.Id, 5);
		}

		private ICmPossibilityList AddCustomList()
		{
			var ws = m_cache.DefaultUserWs; // get default ws
			var customPossibilityList = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(
				"CustomCmPossibiltyList", ws);
			customPossibilityList.Name.set_String(m_cache.DefaultAnalWs, "CustomCmPossibiltyList");

			// Set various properties of CmPossibilityList
			customPossibilityList.DisplayOption = (int)PossNameType.kpntName;
			customPossibilityList.PreventDuplicates = true;
			customPossibilityList.IsSorted = false;
			var wss = WritingSystemServices.kwsAnals;
			customPossibilityList.WsSelector = wss;
			if (wss == WritingSystemServices.kwsVerns || wss == WritingSystemServices.kwsVernAnals)
				customPossibilityList.IsVernacular = true;
			else
				customPossibilityList.IsVernacular = false;
			customPossibilityList.Depth = 1;
			customPossibilityList.Description.set_String(m_cache.DefaultAnalWs, "Description of CustomCmPossibiltyList");

			customPossibilityList.FindOrCreatePossibility("list item 1", m_cache.DefaultAnalWs);
			customPossibilityList.FindOrCreatePossibility("list item 2", m_cache.DefaultAnalWs);
			m_customListsGuids.Add(customPossibilityList.Guid);
			return customPossibilityList;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyCustomLists(XmlDocument xdoc)
		{
			var repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = repo.GetObject(m_customListsGuids[0]);
			var ranges = xdoc.SelectNodes("//range");
			Assert.IsNotNull(ranges);
			XmlNode xcustomListRef = null;
			foreach (XmlNode range in ranges)
			{
				var xrangeId = XmlUtils.GetOptionalAttributeValue(range, "id");
				if (xrangeId == "CustomCmPossibiltyList")
				{
					xcustomListRef = range;
					break;
				}
			}
			var xcustomListId = XmlUtils.GetOptionalAttributeValue(xcustomListRef, "id");
			Assert.AreEqual(customList.Name.BestAnalysisVernacularAlternative.Text, xcustomListId);
		}

		private FieldDescription MakeCustomField(string customFieldName, int classId, int ws, CustomFieldType fieldType,
			Guid listGuid)
		{
			var fd = new FieldDescription(m_cache)
					{
						Userlabel = customFieldName,
						HelpString = string.Empty,
						Class = classId
					};
			SetFieldType(fd, ws, fieldType);
			if (fieldType == CustomFieldType.ListRefAtomic || fieldType == CustomFieldType.ListRefCollection)
			{
				Assert.AreNotEqual(Guid.Empty, listGuid);
				fd.ListRootId = listGuid;
			}
			fd.UpdateCustomField();
			return fd;
		}

		private void AddCustomFieldsInExampleSentence()
		{
			//Create an Example Sentence
			var exampleFact = m_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var exampleSentence = exampleFact.Create();
			m_entryTest.SensesOS[0].ExamplesOS.Add(exampleSentence);
			exampleSentence.Example.VernacularDefaultWritingSystem =
				m_cache.TsStrFactory.MakeString("sense ExampleSentence", m_cache.DefaultVernWs);

			// Use this opportunity to also test LexExampleSentence Publish settings export
			exampleSentence.DoNotPublishInRC.Add(m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0]);
			exampleSentence.DoNotPublishInRC.Add(m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[1]);

			// create new custom field LexExampleSentence in LexSense
			var fd = MakeCustomField("CustomField1-Example", LexExampleSentenceTags.kClassId,
				WritingSystemServices.kwsAnal, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldExampleSentencesIds.Add(fd.Id);
			AddCustomFieldSimpleString(fd, m_cache.DefaultAnalWs, exampleSentence.Hvo);

			//Add a second Custom field.
			fd = MakeCustomField("CustomField2-Example Multi", LexExampleSentenceTags.kClassId,
				WritingSystemServices.kwsVernAnals, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldExampleSentencesIds.Add(fd.Id);
			AddCustomFieldMultistringText(fd, exampleSentence.Hvo);
		}

		private void AddCustomFieldMultistringText(FieldDescription fd, int hvo)
		{
			ITsString tss;
			tss = m_cache.TsStrFactory.MakeString("MultiString Analysis ws string", m_cache.DefaultAnalWs);
			if (fd.Type == CellarPropertyType.MultiString)
			{
				// A decent test of a multi-string property (as opposed to Multi-Unicode) requires more than one run.
				var bldr = tss.GetBldr();
				bldr.SetIntPropValues(5, 10, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, m_cache.DefaultVernWs);
				tss = bldr.GetString();
			}
			m_cache.DomainDataByFlid.SetMultiStringAlt(hvo, fd.Id, m_cache.DefaultAnalWs, tss);

			tss = m_cache.TsStrFactory.MakeString("MultiString Vernacular ws string", m_cache.DefaultVernWs);
			m_cache.DomainDataByFlid.SetMultiStringAlt(hvo, fd.Id, m_cache.DefaultVernWs, tss);
		}

		private void AddCustomFieldMultiParaText(FieldDescription fd, int hvo)
		{
			var text = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			var stText = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_cache.LangProject.Texts.Add(text);
			text.ContentsOA = stText;
			var para = stText.AddNewTextPara("normal");
			var seg = m_cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.Contents =
				m_cache.TsStrFactory.MakeString("MultiString Analysis ws string & ampersand check", m_cache.DefaultAnalWs);
			m_cache.DomainDataByFlid.SetObjProp(hvo, fd.Id, stText.Hvo);
		}

		private void AddCustomFieldInAllomorph()
		{
			//Create Allomorph
			var allomorph = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entryTest.AlternateFormsOS.Add(allomorph);
			var hvo = m_entryTest.AlternateFormsOS[0].Hvo;
			var tss = m_cache.TsStrFactory.MakeString("Allomorph of LexEntry", m_cache.DefaultVernWs);
			m_cache.DomainDataByFlid.SetMultiStringAlt(hvo, MoFormTags.kflidForm, m_cache.DefaultVernWs, tss);

			//Add String custom field to Allomorph
			var fd = MakeCustomField("CustomField1-Allomorph", MoFormTags.kClassId,
									 WritingSystemServices.kwsAnal, CustomFieldType.SingleLineText, Guid.Empty);
			m_customFieldAllomorphsIds.Add(fd.Id);
			AddCustomFieldSimpleString(fd, m_cache.DefaultAnalWs, allomorph.Hvo);
		}

		private enum CustomFieldType
		{
			SingleLineText,
			SingleLineString,
			MultiparagraphText,
			Number,
			Date,
			ListRefAtomic,
			ListRefCollection
		}

		private static void SetFieldType(FieldDescription fd, int ws, CustomFieldType fieldType)
		{
			fd.DstCls = 0;
			fd.WsSelector = 0;
			fd.ListRootId = Guid.Empty;
			switch (fieldType)
			{
				case CustomFieldType.SingleLineText:
					fd.Type = ws == WritingSystemServices.kwsAnal || ws == WritingSystemServices.kwsVern ?
						CellarPropertyType.String : CellarPropertyType.MultiUnicode;
					fd.WsSelector = ws;
					break;
				case CustomFieldType.SingleLineString:
					fd.Type = ws == WritingSystemServices.kwsAnal || ws == WritingSystemServices.kwsVern ?
						CellarPropertyType.String : CellarPropertyType.MultiString;
					fd.WsSelector = ws;
					break;
				case CustomFieldType.MultiparagraphText:
					fd.Type = CellarPropertyType.OwningAtomic;
					fd.DstCls = StTextTags.kClassId;
					break;

				case CustomFieldType.Number:
					fd.Type = CellarPropertyType.Integer;
					break;

				case CustomFieldType.Date:
					fd.Type = CellarPropertyType.GenDate;
					break;

				case CustomFieldType.ListRefAtomic:
					fd.Type = CellarPropertyType.ReferenceAtomic;
					fd.DstCls = CmPossibilityTags.kClassId;
					break;

				case CustomFieldType.ListRefCollection:
					fd.Type = CellarPropertyType.ReferenceCollection;
					fd.DstCls = CmPossibilityTags.kClassId;
					break;
			}
		}

		/// <summary>
		/// Teardown method: destroy the memory-only mock cache.
		/// </summary>
		[TearDown]
		public void DestroyMockCache()
		{
			Directory.Delete(MockLinkedFilesFolder, true);
			m_cache.Dispose();
			m_cache = null;
			DestroyTestDirectory();
		}

		#endregion

		private string LiftFolder { get; set; }

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Lift export using the LiftExporter class.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void LiftExport()
		{
			var exporter = new LiftExporter(m_cache);
			var xdoc = new XmlDocument();
			using (TextWriter w = new StringWriter())
			{
				exporter.ExportPicturesAndMedia = true;
				exporter.ExportLift(w, LiftFolder);
				xdoc.LoadXml(w.ToString());
			}
			VerifyExport(xdoc);

			var xdocRangeFile = new XmlDocument();
			using (var w = new StringWriter())
			{
				exporter.ExportLiftRanges(w);
				xdocRangeFile.LoadXml(w.ToString());
			}
			VerifyExportRanges(xdocRangeFile);
		}

		/// <summary>
		/// LT-15467 documents a Flex to WeSay S/R which had pronunciation audio files multiplying like bunny rabbits.
		/// Make sure the export doesn't make new files when two different references point to the same file.
		/// </summary>
		[Test]
		public void LiftExport_MultipleReferencesToSameMediaFileCausesNoDuplication()
		{
			using(var uowHelper = new NonUndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor))
			{
				var senseFactory = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				var pronunciation = m_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				var media = m_cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
				var pronunFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
				m_entryIs.PronunciationsOS.Add(pronunciation);
				pronunciation.MediaFilesOS.Add(media);
				m_cache.LangProject.PicturesOC.First().FilesOC.Add(pronunFile); // maybe not quite appropriate, but has to be owned somewhere.
				media.MediaFileRA = pronunFile;
				var internalPath = Path.Combine(FdoFileHelper.ksMediaDir, kpronunciationFileName);
				pronunFile.InternalPath = internalPath;
				var exporter = new LiftExporter(m_cache);
				var xdoc = new XmlDocument();
				using(TextWriter w = new StringWriter())
				{
					exporter.ExportPicturesAndMedia = true;
					exporter.ExportLift(w, LiftFolder);
					xdoc.LoadXml(w.ToString());
				}
				VerifyAudio(kpronunciationFileName);
				VerifyAudio(Path.GetFileNameWithoutExtension(kpronunciationFileName) + "_1" + Path.GetExtension(kpronunciationFileName), false);
			}
		}

		[Test]
		public void LiftExport_MultiParagraphWithAmpersandExports()
		{
			using(var uowhelper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, "undothis"))
			{
				var fd = MakeCustomField("MultiParaOnLexEntry", LexEntryTags.kClassId,
									 WritingSystemServices.kwsVernAnals, CustomFieldType.MultiparagraphText, Guid.Empty);
				m_customFieldEntryIds.Add(fd.Id);
				AddCustomFieldMultiParaText(fd, m_entryTest.Hvo);
				var exporter = new LiftExporter(m_cache);
				var xdoc = new XmlDocument();
				var liftFilePath = Path.Combine(LiftFolder, "test.lift");
				using(var liftFile = File.Create(liftFilePath))
				using(TextWriter w = new StreamWriter(liftFile))
				{
					exporter.ExportPicturesAndMedia = false;
					exporter.ExportLift(w, LiftFolder);
				}
				Assert.DoesNotThrow(() => Validator.CheckLiftWithPossibleThrow(liftFilePath));
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyExportRanges(XmlDocument xdoc)
		{
			var repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = repo.GetObject(m_customListsGuids[0]);
			var item1 = customList.FindOrCreatePossibility("list item 1", m_cache.DefaultAnalWs);
			var item2 = customList.FindOrCreatePossibility("list item 2", m_cache.DefaultAnalWs);

			var ranges = xdoc.SelectNodes("//range");
			Assert.IsNotNull(ranges);
			Assert.AreEqual(12, ranges.Count);
			XmlNode xcustomListRef = null;
			foreach (XmlNode range in ranges)
			{
				var xrangeId = XmlUtils.GetOptionalAttributeValue(range, "id");
				if (xrangeId == "CustomCmPossibiltyList")
				{
					xcustomListRef = range;
					break;
				}
			}
			Assert.IsNotNull(xcustomListRef);
			var xcustomListId = XmlUtils.GetOptionalAttributeValue(xcustomListRef, "id");
			Assert.AreEqual(customList.Name.BestAnalysisVernacularAlternative.Text, xcustomListId);

			var rangeElements = xcustomListRef.ChildNodes;
			Assert.IsNotNull(rangeElements);
			Assert.IsTrue(rangeElements.Count == 2);
			VerifyExportRangeElement(rangeElements[0], item1);
			VerifyExportRangeElement(rangeElements[1], item2);

			//Verify Academic Domains were output
			var acaDomList = m_cache.LangProject.LexDbOA.DomainTypesOA;
			var acDomItem0 = acaDomList.FindOrCreatePossibility("computer science", m_cache.DefaultAnalWs);
			var acDomItem1 = acaDomList.FindOrCreatePossibility("anatomy", m_cache.DefaultAnalWs);
			var acDomItem2 = acaDomList.FindOrCreatePossibility("literature", m_cache.DefaultAnalWs);

			var acDomItem3 = acDomItem2.SubPossibilitiesOS[0];
			var acDomItem4 = acDomItem2.SubPossibilitiesOS[1];

			var acDomItem5 = acaDomList.FindOrCreatePossibility("medicine", m_cache.DefaultAnalWs);

			XmlNode xDomainTypesList = null;
			foreach (XmlNode range in ranges)
			{
				var xrangeId = XmlUtils.GetOptionalAttributeValue(range, "id");
				if (xrangeId == "domain-type")
				{
					xDomainTypesList = range;
					break;
				}
			}
			Assert.IsNotNull(xDomainTypesList);
			var xDomainTypesListId = XmlUtils.GetOptionalAttributeValue(xDomainTypesList, "id");
			Assert.AreEqual("domain-type", xDomainTypesListId);

			rangeElements = xDomainTypesList.ChildNodes;
			Assert.IsNotNull(rangeElements);
			Assert.IsTrue(rangeElements.Count == 6);
			VerifyExportRangeElement(rangeElements[0], acDomItem0);
			VerifyExportRangeElement(rangeElements[1], acDomItem1);
			VerifyExportRangeElement(rangeElements[2], acDomItem2);
			VerifyExportRangeElement(rangeElements[3], acDomItem3);
			VerifyExportRangeElement(rangeElements[4], acDomItem4);
			VerifyExportRangeElement(rangeElements[5], acDomItem5);

			// Verify Publication Types were output
			var publicationList = m_cache.LangProject.LexDbOA.PublicationTypesOA;
			var publicItem0 = publicationList.FindOrCreatePossibility("Main Dictionary", m_cache.DefaultAnalWs);
			var publicItem1 = publicationList.FindOrCreatePossibility("School", m_cache.DefaultAnalWs);

			XmlNode xPublicationTypesList = null;
			foreach (XmlNode range in ranges)
			{
				var xrangeId = XmlUtils.GetOptionalAttributeValue(range, "id");
				if (xrangeId == "do-not-publish-in")
				{
					xPublicationTypesList = range;
					break;
				}
			}
			Assert.IsNotNull(xPublicationTypesList);
			var xPublicationTypesListId = XmlUtils.GetOptionalAttributeValue(xPublicationTypesList, "id");
			Assert.AreEqual("do-not-publish-in", xPublicationTypesListId);

			rangeElements = xPublicationTypesList.ChildNodes;
			Assert.IsNotNull(rangeElements);
			Assert.IsTrue(rangeElements.Count == 2);
			VerifyExportRangeElement(rangeElements[0], publicItem0);
			VerifyExportRangeElement(rangeElements[1], publicItem1);
		}

		private void VerifyExportRangeElement(XmlNode rangeElement1, ICmPossibility item1)
		{
			var id = XmlUtils.GetOptionalAttributeValue(rangeElement1, "id");
			var guid = XmlUtils.GetOptionalAttributeValue(rangeElement1, "guid");
			Assert.AreEqual(item1.Guid.ToString(), guid);
			Assert.AreEqual(item1.Name.BestAnalysisVernacularAlternative.Text, id);
			var rangeElementFormText = rangeElement1.FirstChild.FirstChild.FirstChild.InnerText;
			Assert.AreEqual(item1.Name.BestAnalysisVernacularAlternative.Text, rangeElementFormText);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyExport(XmlDocument xdoc)
		{
			var repoEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			Assert.IsNotNull(repoEntry, "Should have a lex entry repository");
			Assert.AreEqual(7, repoEntry.Count, "Should have 7 lex entries");
			var repoSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			Assert.IsNotNull(repoSense);
			Assert.AreEqual(7, repoSense.Count, "Each entry has one sense for a total of 7");
			var entries = xdoc.SelectNodes("//entry");
			Assert.IsNotNull(entries);
			Assert.AreEqual(7, entries.Count, "LIFT file should contain 7 entries");
			var formats = new string[] { "yyyy-MM-ddTHH:mm:sszzzz", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-dd" };
			VerifyCustomLists(xdoc);
			foreach (XmlNode xentry in entries)
			{
				var sCreated = XmlUtils.GetOptionalAttributeValue(xentry, "dateCreated");
				Assert.IsNotNull(sCreated, "an LIFT <entry> should have a dateCreated attribute");
				var dtCreated = DateTime.ParseExact(sCreated, formats, new DateTimeFormatInfo(),
													DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
				var delta = DateTime.UtcNow - dtCreated;
				Assert.Greater(300, delta.TotalSeconds);
				Assert.LessOrEqual(0, delta.TotalSeconds);	// allow time for breakpoints in debugging...
				var sModified = XmlUtils.GetOptionalAttributeValue(xentry, "dateModified");
				var dtModified = DateTime.ParseExact(sModified, formats, new DateTimeFormatInfo(),
													 DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
				delta = DateTime.UtcNow - dtModified;
				Assert.Greater(300, delta.TotalSeconds);
				Assert.LessOrEqual(0, delta.TotalSeconds);
				Assert.IsNotNull(sModified, "an LIFT <entry> should have a dateModified attribute");
				var sId = XmlUtils.GetOptionalAttributeValue(xentry, "id");
				Assert.IsNotNull(sId, "an LIFT <entry> should have a id attribute");
				var sGuid = XmlUtils.GetOptionalAttributeValue(xentry, "guid");
				Assert.IsNotNull(sGuid, "an LIFT <entry> should have a guid attribute");
				var guid = new Guid(sGuid);
				ILexEntry entry;
				Assert.IsTrue(repoEntry.TryGetObject(guid, out entry));
				var xform = xentry.SelectSingleNode("lexical-unit/form");
				Assert.IsNotNull(xform);
				var sLang = XmlUtils.GetOptionalAttributeValue(xform, "lang");
				Assert.IsNotNullOrEmpty(sLang);
				var formWs = m_cache.WritingSystemFactory.get_Engine(sLang);
				Assert.AreEqual(m_cache.DefaultVernWs, formWs.Handle);
				Assert.AreEqual(entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, xform.FirstChild.InnerText);
				var traitlist = xentry.SelectNodes("trait");
				Assert.IsNotNull(traitlist);
				if (entry == m_entryTest)
				{
					Assert.AreEqual(8, traitlist.Count);
					VerifyPublishInExport(xentry);
				}
				else
				{
					Assert.AreEqual(1, traitlist.Count);
					VerifyEmptyPublishIn(xentry);
				}
				var xtrait = traitlist[0];
				var sName = XmlUtils.GetOptionalAttributeValue(xtrait, "name");
				Assert.AreEqual("morph-type", sName);
				var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
				if (entry == m_entryTest)
					Assert.AreEqual("phrase", sValue);
				else
					Assert.AreEqual("stem", sValue);
				var senselist = xentry.SelectNodes("sense");
				Assert.IsNotNull(senselist);
				Assert.AreEqual(1, senselist.Count);
				var xsense = senselist[0];
				sId = XmlUtils.GetOptionalAttributeValue(xsense, "id");
				Assert.IsNotNull(sId);
				if (sId.Contains("_"))
					guid = new Guid(sId.Substring(sId.LastIndexOf('_')+1));
				else
					guid = new Guid(sId);
				ILexSense sense;
				Assert.IsTrue(repoSense.TryGetObject(guid, out sense));
				Assert.AreEqual(entry.SensesOS[0], sense);
				var xgram = xsense.SelectSingleNode("grammatical-info");
				Assert.IsNotNull(xgram);
				sValue = XmlUtils.GetOptionalAttributeValue(xgram, "value");
				var msa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
				if (msa != null)
					Assert.AreEqual(msa.PartOfSpeechRA.Name.AnalysisDefaultWritingSystem.Text, sValue);
				var xgloss = xsense.SelectSingleNode("gloss");
				Assert.IsNotNull(xgloss);
				sLang = XmlUtils.GetOptionalAttributeValue(xgloss, "lang");
				Assert.IsNotNullOrEmpty(sLang);
				var glossWs = m_cache.WritingSystemFactory.get_Engine(sLang);
				Assert.AreEqual(m_cache.DefaultAnalWs, glossWs.Handle);
				Assert.AreEqual(sense.Gloss.AnalysisDefaultWritingSystem.Text, xgloss.FirstChild.InnerText);
				if (entry == m_entryTest)
					VerifyEntryExtraStuff(entry, xentry);
				if (entry == m_entryUnbelieving)
					VerifyLexEntryRefs(entry, xentry);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyEmptyPublishIn(XmlNode xentry)
		{
			var dnpiXpath = "trait[@name = 'do-not-publish-in']";
			var dnpiNodes = xentry.SelectNodes(dnpiXpath);
			Assert.AreEqual(0, dnpiNodes.Count, "Should not contain any 'do-not-publish-in' nodes!");
			var senseNodes = xentry.SelectNodes("sense");
			Assert.AreEqual(1, senseNodes.Count, "Should have one sense");
			dnpiNodes = senseNodes[0].SelectNodes(dnpiXpath);
			Assert.AreEqual(0, dnpiNodes.Count, "Should not contain any sense-level 'do-not-publish-in' nodes!");
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyPublishInExport(XmlNode xentry)
		{
			var dnpiXpath = "trait[@name = 'do-not-publish-in']";

			// Verify LexEntry level
			var dnpiNodes = xentry.SelectNodes(dnpiXpath);
			Assert.AreEqual(1, dnpiNodes.Count, "Should contain Main Dictionary");
			Assert.AreEqual("Main Dictionary", XmlUtils.GetAttributeValue(dnpiNodes[0], "value"), "Wrong publication!");

			// Verify LexSense level
			var senseNodes = xentry.SelectNodes("sense");
			Assert.AreEqual(1, senseNodes.Count, "Should have one sense");
			var xsense = senseNodes[0];
			dnpiNodes = xsense.SelectNodes(dnpiXpath);
			Assert.AreEqual(1, dnpiNodes.Count, "Should contain School");
			Assert.AreEqual("School", XmlUtils.GetAttributeValue(dnpiNodes[0], "value"), "Wrong publication!");

			// Verify LexExampleSentence level
			var exampleNodes = xsense.SelectNodes("example");
			Assert.AreEqual(1, exampleNodes.Count, "Should have one example sentence");
			dnpiNodes = exampleNodes[0].SelectNodes(dnpiXpath);
			Assert.AreEqual(2, dnpiNodes.Count, "Should contain both publications");
			Assert.AreEqual("Main Dictionary", XmlUtils.GetAttributeValue(dnpiNodes[0], "value"), "Wrong publication!");
			Assert.AreEqual("School", XmlUtils.GetAttributeValue(dnpiNodes[1], "value"), "Wrong publication!");
		}

		/// <summary>
		/// Verify the Unbelieving entry's complex form stuff. It is a compound of "un", "believe", and "ing",
		/// and has a BaseForm of "believe".
		/// <code>
		/// <relation type="_component-lexeme" ref="un-_c442d6c6-3b70-4747-ba0b-6d09d696d0c4">
		///		<trait name="complex-form-type" value="Compound"/>
		/// </relation>
		/// <relation type="_component-lexeme" ref="believe_3dc38a5a-5a33-4a84-a7c4-a71c1966a119">
		///		<trait name="is-primary" value="true"/>
		///		<trait name="complex-form-type" value="Compound"/>
		/// </relation>
		/// <relation type="_component-lexeme" ref="-ing_3dc38a5a-5a33-4a84-a7c4-a71c1966a119">
		///		<trait name="complex-form-type" value="Compound"/>
		/// </relation>
		/// <relation type="BaseForm" ref="believe_3dc38a5a-5a33-4a84-a7c4-a71c1966a119">
		///		<trait name="is-primary" value="true"/>
		///		<trait name="complex-form-type" value="BaseForm"/>
		/// </relation>
		/// </code>
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyLexEntryRefs(ILexEntry entryUnbelieving, XmlNode xentry)
		{
			var relations = xentry.SelectNodes("relation");
			Assert.That(relations, Has.Count.EqualTo(4));
			VerifyRelation(relations[0], "_component-lexeme", "Compound", false, "un");
			VerifyRelation(relations[1], "_component-lexeme", "Compound", true, "believe");
			VerifyRelation(relations[2], "_component-lexeme", "Compound", false, "ing");
			VerifyRelation(relations[3], "BaseForm", "BaseForm", true, "believe");
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyRelation(XmlNode relation, string type, string complexFormType, bool isPrimary, string target)
		{
			Assert.That(relation.Attributes["type"].Value, Is.EqualTo(type));
			var traitType = relation.SelectNodes("trait[@name='complex-form-type']");
			Assert.That(traitType[0].Attributes["value"].Value, Is.EqualTo(complexFormType));
			if (isPrimary)
			{
				var traitPrimary = relation.SelectNodes("trait[@name='is-primary']");
				Assert.That(traitPrimary[0].Attributes["value"].Value, Is.EqualTo("true"));
			}

			var repoEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var refValue = relation.Attributes["ref"].Value;
			Assert.That(refValue.StartsWith(target));
			var guid = new Guid(refValue.Substring(target.Length + 1));
			ILexEntry relatedEntry;
			Assert.IsTrue(repoEntry.TryGetObject(guid, out relatedEntry));
			Assert.That(relatedEntry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo(target));
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyEntryExtraStuff(ILexEntry entry, XmlNode xentry)
		{
			var citations = xentry.SelectNodes("citation");
			Assert.IsNotNull(citations);
			Assert.AreEqual(1, citations.Count);
			VerifyMultiStringAlt(citations[0], m_cache.DefaultVernWs, 2, entry.CitationForm.VernacularDefaultWritingSystem);

			var notes = xentry.SelectNodes("note");
			Assert.IsNotNull(notes);
			Assert.AreEqual(3, notes.Count);
			foreach (XmlNode xnote in notes)
			{
				var sType = XmlUtils.GetOptionalAttributeValue(xnote, "type");
				if (sType == null)
					VerifyTsString(xnote, m_cache.DefaultAnalWs, entry.Comment.AnalysisDefaultWritingSystem);
				else if (sType == "bibliography")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, entry.Bibliography.AnalysisDefaultWritingSystem);
				else if (sType == "restrictions")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, entry.Restrictions.AnalysisDefaultWritingSystem);
				else
					Assert.IsNull(sType, "Unrecognized type attribute");
			}
			VerifyEntryCustomFields(xentry, entry);
			VerifyAllomorphCustomFields(xentry, entry);

			var xsenses = xentry.SelectNodes("sense");
			Assert.IsNotNull(xsenses);
			Assert.AreEqual(1, xsenses.Count);
			VerifyExtraSenseStuff(entry.SensesOS[0], xsenses[0]);

			var xpronun = xentry.SelectNodes("pronunciation");
			Assert.That(xpronun, Has.Count.EqualTo(1));
			var xmedia = xpronun[0].SelectNodes("media");
			Assert.That(xmedia, Has.Count.EqualTo(1));
			var hrefMedia = xmedia[0].Attributes["href"];
			Assert.That(hrefMedia.Value, Is.EqualTo(kpronunciationFileName));
			VerifyAudio(kpronunciationFileName);
			var xlf = xentry.SelectSingleNode("lexical-unit");
			VerifyMultiStringAlt(xlf, m_audioWsCode, 2, m_cache.TsStrFactory.MakeString(klexemeFormFileName, m_audioWsCode));
			VerifyAudio(klexemeFormFileName);
			VerifyMultiStringAlt(citations[0], m_audioWsCode, 2, m_cache.TsStrFactory.MakeString(kcitationFormFileName, m_audioWsCode));
			VerifyAudio(kcitationFormFileName);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyEntryCustomFields(XmlNode xentry, ILexEntry entry)
		{
			var xfields = xentry.SelectNodes("field");
			Assert.IsNotNull(xfields);
			Assert.AreEqual(5, xfields.Count);
			foreach (XmlNode xfield in xfields)
			{
				var sType = XmlUtils.GetOptionalAttributeValue(xfield, "type");
				Assert.IsNotNull(sType);
				if (sType == "literal-meaning")
					VerifyTsString(xfield, m_cache.DefaultAnalWs, entry.LiteralMeaning.AnalysisDefaultWritingSystem);
				else if (sType == "summary-definition")
					VerifyTsString(xfield, m_cache.DefaultAnalWs, entry.SummaryDefinition.AnalysisDefaultWritingSystem);
				else if (sType == "CustomField1-LexEntry")
				{
					var tssString = m_cache.DomainDataByFlid.get_StringProp(entry.Hvo, m_customFieldEntryIds[0]);
					VerifyTsString(xfield, m_cache.DefaultAnalWs, tssString);
				}
				else if (sType == "CustomField2-LexEntry" || sType == "CustomField6-LexEntry")
				{
					var tssMultiString = m_cache.DomainDataByFlid.get_MultiStringProp(entry.Hvo, m_customFieldEntryIds[1]);
					VerifyMultiStringAnalVern(xfield, tssMultiString, true);
					VerifyAudio(kcustomMultiFileName);
				}
				else
					Assert.IsNull(sType, "Unrecognized type attribute");
			}

			var xtraits = xentry.SelectNodes("trait");
			Assert.IsNotNull(xtraits);
			var sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Assert.IsNotNull(sda);
			int listIndex = 0;
			foreach (XmlNode xtrait in xtraits)
			{
				var sName = XmlUtils.GetOptionalAttributeValue(xtrait, "name");
				Assert.IsNotNull(sName);
				if (sName == "CustomField3-LexEntry Date")
				{
					var genDate = sda.get_GenDateProp(m_entryTest.Hvo, m_customFieldEntryIds[2]);
					VerifyGenDate(xtrait, genDate);
				}
				else if (sName == "CustomField3-LexEntry CmPossibilitySemanticDomain")
				{
					var repoSem = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
					var firstSemEntry = repoSem.AllInstances().First();
					var possHvo = sda.get_ObjectProp(m_entryTest.Hvo, m_customFieldEntryIds[3]);
					var strPoss = LiftExporter.GetPossibilityBestAlternative(possHvo, m_cache);
					var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
					Assert.AreEqual(strPoss, sValue);
				}
				else if (sName == "CustomField4-LexEntry ListRefCollection")
				{
					//we added 3 items to this RefCollection. Make sure each once was correctly saved to LIFT.
					var possHvo = sda.get_VecItem(m_entryTest.Hvo, m_customFieldEntryIds[4], listIndex);
					var strPoss = LiftExporter.GetPossibilityBestAlternative(possHvo, m_cache);
					listIndex++;
					var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
					Assert.AreEqual(strPoss, sValue);
				}
				else if (sName == "CustomField5-LexEntry CmPossibilityCustomList")
				{
					//This one is referencing a custom possibility list.
					var possHvo = sda.get_ObjectProp(m_entryTest.Hvo, m_customFieldEntryIds[5]);
					var strPoss = LiftExporter.GetPossibilityBestAlternative(possHvo, m_cache);
					var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
					Assert.AreEqual(strPoss, sValue);
				}
			}
		}

		private void VerifyGenDate(XmlNode xtrait, GenDate genDate)
		{
			//<trait name="CustomField2-LexSense Integer" value="201105112"></trait>
			//   '-'(BC and ''AD) 2011 05(May) 11(Day) 2(GenDate.PrecisionType (Before, Exact, Approximate, After)
			var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
			Assert.IsNotNull(sValue);
			var liftGenDate = LiftExporter.GetGenDateFromInt(Convert.ToInt32(sValue));
			Assert.AreEqual(liftGenDate.Precision, genDate.Precision);
			Assert.AreEqual(liftGenDate.IsAD, genDate.IsAD);
			Assert.AreEqual(liftGenDate.Year, genDate.Year);
			Assert.AreEqual(liftGenDate.Month, genDate.Month);
			Assert.AreEqual(liftGenDate.Day, genDate.Day);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyAllomorphCustomFields(XmlNode xentry, ILexEntry entry)
		{
			//<variant>
			//<form lang="fr"><text>Allomorph of LexEntry</text></form>
			//<trait name="morph-type" value="stem"></trait>
			//<field type="CustomRick5">
			//          <form lang="en"><text>CustomRick5Text.</text></form>
			//</field>
			//</variant>
			var xallomorphs = xentry.SelectNodes("variant");
			Assert.IsNotNull(xallomorphs);
			Assert.AreEqual(1, xallomorphs.Count);
			foreach (XmlNode xallomorph in xallomorphs)
			{
				var xfield = xallomorph.SelectSingleNode("field");
				var sType = XmlUtils.GetOptionalAttributeValue(xfield, "type");
				Assert.IsNotNull(sType);
				if (sType == "CustomField1-Allomorph")
				{
					var tssString = m_cache.DomainDataByFlid.get_StringProp(entry.AlternateFormsOS[0].Hvo, m_customFieldAllomorphsIds[0]);
					VerifyTsString(xfield, m_cache.DefaultAnalWs, tssString);
				}
				else
					Assert.IsNull(sType, "Unrecognized type attribute");

			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyExtraSenseStuff(ILexSense sense, XmlNode xsense)
		{
			var xdefs = xsense.SelectNodes("definition");
			Assert.IsNotNull(xdefs);
			Assert.AreEqual(1, xdefs.Count);
			VerifyMultiStringAlt(xdefs[0], m_cache.DefaultAnalWs, 2, sense.Definition.AnalysisDefaultWritingSystem);
			VerifyMultiStringAlt(xdefs[0], m_audioWsCode, 2, m_cache.TsStrFactory.MakeString(kaudioFileName, m_audioWsCode));

			// Check the hyperlink
			var defnSpan = xdefs[0].SelectSingleNode("form/text/span");
			var defnHref = defnSpan.Attributes["href"];
			Assert.That(defnHref.Value, Is.EqualTo("file://others/" + kotherLinkedFileName));
			var liftOtherFolder = Path.Combine(LiftFolder, "others");
			Assert.IsTrue(File.Exists(Path.Combine(liftOtherFolder, kotherLinkedFileName)));

			var xnotes = xsense.SelectNodes("note");
			Assert.IsNotNull(xnotes);
			Assert.AreEqual(10, xnotes.Count);
			foreach (XmlNode xnote in xnotes)
			{
				var sType = XmlUtils.GetOptionalAttributeValue(xnote, "type");
				if (sType == null)
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.GeneralNote.AnalysisDefaultWritingSystem);
				else if (sType == "anthropology")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.AnthroNote.AnalysisDefaultWritingSystem);
				else if (sType == "bibliography")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.Bibliography.AnalysisDefaultWritingSystem);
				else if (sType == "discourse")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.DiscourseNote.AnalysisDefaultWritingSystem);
				else if (sType == "encyclopedic")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.EncyclopedicInfo.AnalysisDefaultWritingSystem);
				else if (sType == "grammar")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.GrammarNote.AnalysisDefaultWritingSystem);
				else if (sType == "phonology")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.PhonologyNote.AnalysisDefaultWritingSystem);
				else if (sType == "restrictions")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.Restrictions.AnalysisDefaultWritingSystem);
				else if (sType == "semantics")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.SemanticsNote.AnalysisDefaultWritingSystem);
				else if (sType == "sociolinguistics")
					VerifyTsString(xnote, m_cache.DefaultAnalWs, sense.SocioLinguisticsNote.AnalysisDefaultWritingSystem);
				else
					Assert.IsNull(sType, "Unrecognized type attribute");
			}
			VerifySenseCustomFields(xsense, sense);
			VerifyExampleSentenceCustomFields(xsense, sense);
			VerifyPictures(xsense, sense);
			VerifyAudio(kaudioFileName);
		}

		private void VerifyAudio(string audioFileName, bool exists = true)
		{
			var liftAudioFolder = Path.Combine(LiftFolder, "audio");
			var filePath = Path.Combine(liftAudioFolder, audioFileName);
			var failureMsg = String.Format("{0} should {1}have been found after export", filePath,
													 exists ? "" : "not ");
			Assert.AreEqual(exists, File.Exists(filePath), failureMsg);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyPictures(XmlNode xsense, ILexSense sense)
		{
			var pictureNodes = xsense.SelectNodes("illustration");
			var pictures = sense.PicturesOS.ToArray();
			Assert.That(pictureNodes.Count, Is.EqualTo(pictures.Length));
			var firstPic =
				(from XmlNode node in pictureNodes
				 where XmlUtils.GetOptionalAttributeValue(node, "href") == kpictureOfTestFileName
				 select node).First();
			// If that got one, we're good on the XmlNode.
			var liftPicsFolder = Path.Combine(LiftFolder, "pictures");
			Assert.IsTrue(File.Exists(Path.Combine(liftPicsFolder, kpictureOfTestFileName)));

			var secondPicName = kbasePictureOfTestFileName + "_1" + ".jpg";
			var secondPic =
				(from XmlNode node in pictureNodes
				 where XmlUtils.GetOptionalAttributeValue(node, "href") == secondPicName
				 select node).First();
			Assert.IsTrue(File.Exists(Path.Combine(liftPicsFolder, secondPicName)));

			var thirdPicName = Path.Combine(ksubFolderName, kotherPicOfTestFileName);
			var thirdPic =
				(from XmlNode node in pictureNodes
				 where XmlUtils.GetOptionalAttributeValue(node, "href") == thirdPicName
				 select node).First();
			Assert.IsTrue(File.Exists(Path.Combine(liftPicsFolder, thirdPicName)));

			var fourthPicName = Path.GetFileName(m_tempPictureFilePath);
			var fourthPic =
				(from XmlNode node in pictureNodes
				 where XmlUtils.GetOptionalAttributeValue(node, "href") == fourthPicName
				 select node).First();
			Assert.IsTrue(File.Exists(Path.Combine(liftPicsFolder, fourthPicName)));
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifySenseCustomFields(XmlNode xsense, ILexSense sense)
		{
			var xfields = xsense.SelectNodes("field");
			Assert.IsNotNull(xfields);
			Assert.AreEqual(1, xfields.Count);
			foreach (XmlNode xfield in xfields)
			{
				var sType = XmlUtils.GetOptionalAttributeValue(xfield, "type");
				Assert.IsNotNull(sType);
				if (sType == "CustomField1-LexSense")
				{
					var tssString = m_cache.DomainDataByFlid.get_StringProp(sense.Hvo, m_customFieldSenseIds[0]);
					VerifyTsString(xfield, m_cache.DefaultVernWs, tssString);
				}
				else
					Assert.IsNull(sType, "Unrecognized type attribute");
			}
			//<trait name="CustomField2-LexSense Integer" value="5"></trait>
			var xtraits = xsense.SelectNodes("trait");
			Assert.IsNotNull(xtraits);
			Assert.AreEqual(5, xtraits.Count); // 4 custom field traits + 1 DoNotPublishIn trait

			int listIndex = 0;
			var mdc = m_cache.DomainDataByFlid.MetaDataCache;
			var flidOfDomainTypes = mdc.GetFieldId("LexSense", "DomainTypes", true); //should be 5016006

			foreach (XmlNode xtrait in xtraits)
			{
				var sName = XmlUtils.GetOptionalAttributeValue(xtrait, "name");
				Assert.IsNotNull(sName);
				if (sName == "CustomField2-LexSense Integer")
				{
					var intVal = m_cache.DomainDataByFlid.get_IntProp(sense.Hvo, m_customFieldSenseIds[1]);
					VerifyInteger(xtrait, intVal);
				}
				else if (sName == "do-not-publish-in")
				{
					continue; // already verified elsewhere
				}
				else if (sName == "domain-type")
				{
					var possHvo = m_sda.get_VecItem(sense.Hvo, flidOfDomainTypes, listIndex);
					var strPoss = LiftExporter.GetPossibilityBestAlternative(possHvo, m_cache);
					listIndex++;
					var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
					Assert.AreEqual(strPoss, sValue);
				}
				else
					Assert.IsNull(sName, "Unrecognized type attribute");
			}
		}

		private static void VerifyInteger(XmlNode xtrait, int intVal)
		{
			//<trait name="CustomField2-LexSense Integer" value="5"></trait>
			var sValue = XmlUtils.GetOptionalAttributeValue(xtrait, "value");
			Assert.IsNotNull(sValue);
			Assert.AreEqual(sValue, intVal.ToString());
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyExampleSentenceCustomFields(XmlNode xsense, ILexSense sense)
		{
			//<example>
			//    <form lang=\"fr\"><text>sense ExampleSentence</text></form>
			//    <field type=\"CustomField2-Example Multi\">
			//        <form lang=\"en\"><text>MultiString Analysis ws string</text></form>
			//        <form lang=\"fr\"><text>MultiString Vernacular ws string</text></form>
			//    </field>
			//    <field type=\"CustomField1-Example\">
			//        <form lang=\"en\"><text>CustomField1-Example</text></form>
			//    </field>
			//</example>"
			var xexamples = xsense.SelectNodes("example");
			Assert.IsNotNull(xexamples);
			Assert.AreEqual(1, xexamples.Count);
			foreach (XmlNode xexample in xexamples)
			{
				var xfields = xexample.SelectNodes("field");
				Assert.IsNotNull(xfields);
				Assert.AreEqual(2, xfields.Count);
				foreach (XmlNode xfield in xfields)
				{
					var sType = XmlUtils.GetOptionalAttributeValue(xfield, "type");
					Assert.IsNotNull(sType);
					if (sType == "CustomField1-Example")
					{
						var tssString = m_cache.DomainDataByFlid.get_StringProp(sense.ExamplesOS[0].Hvo, m_customFieldExampleSentencesIds[0]);
						VerifyTsString(xfield, m_cache.DefaultAnalWs, tssString);
					}
					else if (sType == "CustomField2-Example Multi")
					{
						var tssMultiString = m_cache.DomainDataByFlid.get_MultiStringProp(sense.ExamplesOS[0].Hvo, m_customFieldExampleSentencesIds[1]);
						VerifyMultiStringAnalVern(xfield, tssMultiString, false);
					}
					else
						Assert.IsNull(sType, "Unrecognized type attribute");
				}
			}
		}

		private bool DontExpectNewlinesCorrected;
		private string m_tempPictureFilePath;

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyTsString(XmlNode xitem, int wsItem, ITsString tssText)
		{
			var xforms = xitem.SelectNodes("form");
			Assert.IsNotNull(xforms);
			Assert.AreEqual(1, xforms.Count);
			var sLang = XmlUtils.GetOptionalAttributeValue(xforms[0], "lang");
			Assert.AreEqual(m_cache.WritingSystemFactory.GetStrFromWs(wsItem), sLang);
			VerifyForm(xforms[0], tssText, sLang);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyMultiStringAlt(XmlNode xitem, int wsItem, int wsCount, ITsString tssText)
		{
			var xforms = xitem.SelectNodes("form");
			Assert.IsNotNull(xforms);
			Assert.AreEqual(wsCount, xforms.Count);
			var langWanted = m_cache.WritingSystemFactory.GetStrFromWs(wsItem);
			foreach (XmlNode form in xforms)
			{
				var sLang = XmlUtils.GetOptionalAttributeValue(form, "lang");
				if (sLang == langWanted)
				{
					VerifyForm(form, tssText, langWanted);
					return; // got it.
				}
			}
			Assert.Fail("expected Ws alternative not found");
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyForm(XmlNode form, ITsString tssText, string baseLang)
		{
			var sText = form.FirstChild.InnerText;
			var expected = tssText.Text;
			if (!DontExpectNewlinesCorrected)
				expected = expected.Replace("\x2028", Environment.NewLine);
			Assert.AreEqual(expected, sText);
			var runs = form.FirstChild.ChildNodes;
			Assert.That(runs, Has.Count.EqualTo(tssText.RunCount), "form should have correct run count");
			for (int i = 0; i < tssText.RunCount; i++)
			{
				var content = tssText.get_RunText(i);
				if (!DontExpectNewlinesCorrected)
					content = content.Replace("\x2028", Environment.NewLine);
				int val;
				var lang = tssText.get_Properties(i).GetIntPropValues((int) FwTextPropType.ktptWs, out val);
				var wsCode = m_cache.WritingSystemFactory.GetStrFromWs(lang);
				var run = runs[i];
				Assert.That(run.InnerText, Is.EqualTo(content));
				if (run is XmlElement)
				{
					Assert.That(run.Name, Is.EqualTo("span"), "element embedded in form text must be span");
					var runLang = XmlUtils.GetOptionalAttributeValue(run, "lang");
					if (string.IsNullOrEmpty(runLang))
						Assert.That(wsCode, Is.EqualTo(baseLang)); // could be some other reason for a span, in which case it will have the default ws.
					else
						Assert.That(runLang, Is.EqualTo(wsCode));
				}
				else
				{
					Assert.That(wsCode, Is.EqualTo(baseLang));
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyMultiStringAnalVern(XmlNode xitem, ITsMultiString tssMultiString, bool expectCustom)
		{
			var xforms = xitem.SelectNodes("form");
			Assert.IsNotNull(xforms);
			Assert.AreEqual(expectCustom ? 3 : 2, xforms.Count);

			var sLang = XmlUtils.GetOptionalAttributeValue(xforms[0], "lang");
			Assert.AreEqual(m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs), sLang);
			var sText = xforms[0].FirstChild.InnerText;
			Assert.AreEqual(tssMultiString.get_String(m_cache.DefaultAnalWs).Text, sText);

			sLang = XmlUtils.GetOptionalAttributeValue(xforms[1], "lang");
			Assert.AreEqual(m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultVernWs), sLang);
			sText = xforms[1].FirstChild.InnerText;
			Assert.AreEqual(tssMultiString.get_String(m_cache.DefaultVernWs).Text, sText);

			if (expectCustom)
			{
				sLang = XmlUtils.GetOptionalAttributeValue(xforms[2], "lang");
				Assert.AreEqual(m_cache.WritingSystemFactory.GetStrFromWs(m_audioWsCode), sLang);
				sText = xforms[2].FirstChild.InnerText;
				Assert.AreEqual(tssMultiString.get_String(m_audioWsCode).Text, kcustomMultiFileName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Lift export of a custom StText field using the LiftExporter class.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void LiftExportCustomStText()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, AddStTextCustomFieldAndData);

			var exporter = new LiftExporter(m_cache);
			var xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			using (TextWriter w = new StringWriter())
			{
				exporter.ExportPicturesAndMedia = true;
				exporter.ExportLift(w, LiftFolder);
				xdoc.LoadXml(w.ToString());
			}
			VerifyCustomStText(xdoc);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Lift export of a custom StText field using the LiftExporter class.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void LiftExportRanges_PartOfSpeechCatalogIdIsExported()
		{
			var loader = new XmlList();
			loader.ImportList(Cache.LangProject, "PartsOfSpeech", Path.Combine(FwDirectoryFinder.TemplateDirectory, "POS.xml"),
									new DummyProgressDlg());
			var servLoc = Cache.ServiceLocator;
			var exporter = new LiftExporter(Cache);
			var xdocRangeFile = new XmlDocument();
			using(var w = new StringWriter())
			{
				// SUT
				exporter.ExportLiftRanges(w);
				xdocRangeFile.LoadXml(w.ToString());
			}
			AssertThatXmlIn.Dom(xdocRangeFile).HasAtLeastOneMatchForXpath("//range[@id='grammatical-info']/range-element/trait[@name='catalog-source-id']");
		}

		private int m_flidLongText;

		private void AddStTextCustomFieldAndData()
		{
			var mdc = m_cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.IsNotNull(mdc);
			m_flidLongText = mdc.AddCustomField("LexEntry", "Long Text", CellarPropertyType.OwningAtomic, StTextTags.kClassId);
			var hvoText = m_cache.DomainDataByFlid.MakeNewObject(StTextTags.kClassId, m_entryTest.Hvo, m_flidLongText, -2);

			//text here is the custom StText field of  m_entryTest
			var text = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoText);
			var paraFact = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			var para1 = paraFact.Create();
			text.ParagraphsOS.Add(para1);
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int) FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
			tisb.Append("This is a ");
			tisb.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, "Emphasized Text");
			tisb.Append("test");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(".  This is only a test!");
			para1.Contents = tisb.GetString();
			para1.StyleName = "Bulleted Text";
			var para2 = paraFact.Create();
			text.ParagraphsOS.Add(para2);
			tisb.Clear();
			tisb.ClearProps();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.DefaultAnalWs);
			tisb.Append("Why is there air?  ");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Strong");
			tisb.Append("Which way is up?");
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append("  Inquiring minds want to know!");
			para2.Contents = tisb.GetString();
			var para3 = paraFact.Create();
			text.ParagraphsOS.Add(para3);
			para3.StyleName = "Canadian Bacon";
			para3.Contents = m_cache.TsStrFactory.MakeString("CiCi pizza is cheap, but not really gourmet when it comes to pizza.", m_cache.DefaultAnalWs);

			//LT-11639   we need to create second StText on another entry and make it empty
			//to ensure that if it is empty then we do not export it.
			hvoText = m_cache.DomainDataByFlid.MakeNewObject(StTextTags.kClassId, m_entryThis.Hvo, m_flidLongText, -2);
			//text here is the custom StText field of  m_entryThis
			text = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoText);
			//Now add a couple paragraphs which have no contents.
			var paraInEntryThis = paraFact.Create();
			text.ParagraphsOS.Add(paraInEntryThis);
			var paraInEntryThis2 = paraFact.Create();
			text.ParagraphsOS.Add(paraInEntryThis2);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyCustomStText(XmlDocument xdoc)
		{
			var repoEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var entries = xdoc.SelectNodes("//entry");
			Assert.IsNotNull(entries);
			foreach (XmlNode xentry in entries)
			{
				ILexEntry entry;
				var sGuid = XmlUtils.GetOptionalAttributeValue(xentry, "guid");
				Assert.IsNotNull(sGuid, "an LIFT <entry> should have a guid attribute");
				var guid = new Guid(sGuid);
				Assert.IsTrue(repoEntry.TryGetObject(guid, out entry));
				if (entry == m_entryTest)
				{
					VerifyCustomStTextForEntryTest(xentry);
				}
				else
				{
					VerifyCustomStTextForEntryThisAndAllOthers(xentry);
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyCustomStTextForEntryThisAndAllOthers(XmlNode xentry)
		{
			var xcustoms = xentry.SelectNodes("field[@type=\"Long Text\"]");
			Assert.IsNotNull(xcustoms);
			Assert.AreEqual(0, xcustoms.Count, "We should have zero \"Long Text\" fields for this entry.");
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void VerifyCustomStTextForEntryTest(XmlNode xentry)
		{
			var xcustoms = xentry.SelectNodes("field[@type=\"Long Text\"]");
			Assert.IsNotNull(xcustoms);
			Assert.AreEqual(1, xcustoms.Count, "We should have a single \"Long Text\" field.");
			var xforms = xcustoms[0].SelectNodes("form");
			Assert.IsNotNull(xforms);
			Assert.AreEqual(1, xforms.Count, "We should have a single form inside the \"Long Text\" field.");
			var xtexts = xforms[0].SelectNodes("text");
			Assert.IsNotNull(xtexts);
			Assert.AreEqual(1, xtexts.Count, "We should have a single text inside the \"Long Text\" field.");
			var xspans = xtexts[0].SelectNodes("span");
			Assert.IsNotNull(xspans);
			Assert.AreEqual(5, xspans.Count, "We should have 5 span elements inside the \"Long Text\" field.");
			var i = 0;
			var sLangExpected = m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs);
			foreach (var x in xtexts[0].ChildNodes)
			{
				string sLang = null;
				string sClass = null;
				var xe = x as XmlElement;
				var xw = x as XmlWhitespace;
				var xt = x as XmlText;
				if (xe != null)
				{
					sLang = XmlUtils.GetOptionalAttributeValue(xe, "lang");
					sClass = XmlUtils.GetOptionalAttributeValue(xe, "class");
				}
				Assert.IsNull(xw);
				switch (i)
				{
					case 0:
						Assert.IsNotNull(xe);
						Assert.AreEqual("span", xe.Name);
						Assert.IsNull(sLang);
						Assert.AreEqual("Bulleted Text", sClass);
						VerifyFirstParagraph(xe, sLangExpected);
						break;
					case 1:
						Assert.IsNotNull(xt);
						Assert.AreEqual("\u2029", xt.InnerText);
						break;
					case 2:
						Assert.IsNotNull(xe);
						Assert.AreEqual("span", xe.Name);
						Assert.AreEqual(sLangExpected, sLang);
						Assert.IsNull(sClass);
						Assert.AreEqual("Why is there air?  ", xe.InnerXml);
						break;
					case 3:
						Assert.IsNotNull(xe);
						Assert.AreEqual("span", xe.Name);
						Assert.AreEqual(sLangExpected, sLang);
						Assert.AreEqual("Strong", sClass);
						Assert.AreEqual("Which way is up?", xe.InnerXml);
						break;
					case 4:
						Assert.IsNotNull(xe);
						Assert.AreEqual("span", xe.Name);
						Assert.AreEqual(sLangExpected, sLang);
						Assert.IsNull(sClass);
						Assert.AreEqual("  Inquiring minds want to know!", xe.InnerXml);
						break;
					case 5:
						Assert.IsNotNull(xt);
						Assert.AreEqual("\u2029", xt.InnerText);
						break;
					case 6:
						Assert.IsNotNull(xe);
						Assert.AreEqual("span", xe.Name);
						Assert.IsNull(sLang);
						Assert.AreEqual("Canadian Bacon", sClass);
						VerifyThirdParagraph(xe, sLangExpected);
						break;
				}
				++i;
			}
			Assert.AreEqual(7, i, "There should be exactly 7 child nodes of the text element.");
		}

		private static void VerifyFirstParagraph(XmlElement xePara, string sLangExpected)
		{
			var i = 0;
			foreach (var x in xePara.ChildNodes)
			{
				var xe = x as XmlElement;
				Assert.IsNotNull(xe, "The first paragraph should only have elements as child nodes.");
				var sLang = XmlUtils.GetOptionalAttributeValue(xe, "lang");
				var sClass = XmlUtils.GetOptionalAttributeValue(xe, "class");
				switch (i)
				{
					case 0:
						Assert.AreEqual(sLangExpected, sLang);
						Assert.IsNull(sClass);
						Assert.AreEqual("This is a ", xe.InnerXml);
						break;
					case 1:
						Assert.AreEqual(sLangExpected, sLang);
						Assert.AreEqual("Emphasized Text", sClass);
						Assert.AreEqual("test", xe.InnerXml);
						break;
					case 2:
						Assert.AreEqual(sLangExpected, sLang);
						Assert.IsNull(sClass);
						Assert.AreEqual(".  This is only a test!", xe.InnerXml);
						break;
				}
				++i;
			}
			Assert.AreEqual(3, i, "There should be exactly 3 child nodes of the first paragraph.");
		}

		private static void VerifyThirdParagraph(XmlElement xePara, string sLangExpected)
		{
			var i = 0;
			foreach (var x in xePara.ChildNodes)
			{
				var xe = x as XmlElement;
				Assert.IsNotNull(xe, "The third paragraph should only have elements as child nodes.");
				var sLang = XmlUtils.GetOptionalAttributeValue(xe, "lang");
				var sClass = XmlUtils.GetOptionalAttributeValue(xe, "class");
				switch (i)
				{
					case 0:
						Assert.AreEqual(sLangExpected, sLang);
						Assert.IsNull(sClass);
						Assert.AreEqual("CiCi pizza is cheap, but not really gourmet when it comes to pizza.", xe.InnerXml);
						break;
				}
				++i;
			}
			Assert.AreEqual(1, i, "There should be exactly 1 child node of the third paragraph.");
		}
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - Cache isn't used here")]
	class MockStyle : IStStyle
	{
		public ICmObjectId Id { get; private set; }
		public ICmObject GetObject(ICmObjectRepository repo)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ICmObject> AllOwnedObjects { get; private set; }
		public int Hvo { get; private set; }
		public ICmObject Owner { get; private set; }
		public int OwningFlid { get; private set; }
		public int OwnOrd { get; private set; }
		public int ClassID { get; private set; }
		public Guid Guid { get; private set; }
		public string ClassName { get; private set; }
		public void Delete()
		{
			throw new NotImplementedException();
		}

		public IFdoServiceLocator Services { get; private set; }
		public ICmObject OwnerOfClass(int clsid)
		{
			throw new NotImplementedException();
		}

		public T OwnerOfClass<T>() where T : ICmObject
		{
			throw new NotImplementedException();
		}

		public ICmObject Self { get; private set; }
		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		public void PostClone(Dictionary<int, ICmObject> copyMap)
		{
			throw new NotImplementedException();
		}

		public void AllReferencedObjects(List<ICmObject> collector)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			throw new NotImplementedException();
		}

		public bool IsOwnedBy(ICmObject possibleOwner)
		{
			throw new NotImplementedException();
		}

		public ICmObject ReferenceTargetOwner(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRequired(int flid)
		{
			throw new NotImplementedException();
		}

		public int IndexInOwner { get; private set; }
		public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsValidObject { get; private set; }
		public FdoCache Cache { get; private set; }
		public void MergeObject(ICmObject objSrc)
		{
			throw new NotImplementedException();
		}

		public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			throw new NotImplementedException();
		}

		public bool CanDelete { get; private set; }
		public string ShortName { get; private set; }
		public ITsString ObjectIdName { get; private set; }
		public ITsString ShortNameTSS { get; private set; }
		public ITsString DeletionTextTSS { get; private set; }
		public ITsString ChooserNameTS { get; private set; }
		public string SortKey { get; private set; }
		public string SortKeyWs { get; private set; }
		public int SortKey2 { get; private set; }
		public string SortKey2Alpha { get; private set; }
		public HashSet<ICmObject> ReferringObjects { get; private set; }
		public IEnumerable<ICmObject> OwnedObjects { get; private set; }
		public string Name { get; set; }
		public IStStyle BasedOnRA { get; set; }
		public IStStyle NextRA { get; set; }
		public StyleType Type { get; set; }
		public ITsTextProps Rules { get; set; }
		public bool IsPublishedTextStyle { get; set; }
		public bool IsBuiltIn { get; set; }
		public bool IsModified { get; set; }
		public int UserLevel { get; set; }
		public ContextValues Context { get; set; }
		public StructureValues Structure { get; set; }
		public FunctionValues Function { get; set; }
		public IMultiUnicode Usage { get; private set; }
		public bool IsFootnoteStyle { get; private set; }
		public bool InUse { get; private set; }
	}
}
