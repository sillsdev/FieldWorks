// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlListTests.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class XmlListTests : BaseTest
	{
		private FdoCache m_cache;
		#region s_ksPartsOfSpeechXml
		/// <summary>
		/// The XML representation of a subset of the Parts of Speech list.
		/// </summary>
		public static readonly string s_ksPartsOfSpeechXml =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LangProject>" + Environment.NewLine +
			"  <PartsOfSpeech>" + Environment.NewLine +
			"    <CmPossibilityList>" + Environment.NewLine +
			"      <Depth><Integer val=\"127\"/></Depth>" + Environment.NewLine +
			"      <IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"      <UseExtendedFields><Boolean val=\"true\"/></UseExtendedFields>" + Environment.NewLine +
			"      <ItemClsid><Integer val=\"5049\"/></ItemClsid>" + Environment.NewLine +
			"      <WsSelector><Integer val=\"-3\"/></WsSelector>" + Environment.NewLine +
			"      <Name>" + Environment.NewLine +
			"        <AUni ws=\"es\">Categorías Gramáticas</AUni>" + Environment.NewLine +
			"        <AUni ws=\"en\">Parts Of Speech</AUni>" + Environment.NewLine +
			"        <AUni ws=\"fr\">Parties du Discours</AUni>" + Environment.NewLine +
			"      </Name>" + Environment.NewLine +
			"      <Abbreviation>" + Environment.NewLine +
			"        <AUni ws=\"en\">Pos</AUni>" + Environment.NewLine +
			"      </Abbreviation>" + Environment.NewLine +
			"      <Possibilities>" + Environment.NewLine +
			"        <PartOfSpeech>" + Environment.NewLine +
			"          <ForeColor><Integer val=\"6303632\"/></ForeColor>" + Environment.NewLine +
			"          <BackColor><Integer val=\"-1073741824\"/></BackColor>" + Environment.NewLine +
			"          <UnderColor><Integer val=\"-1073741824\"/></UnderColor>" + Environment.NewLine +
			"          <UnderStyle><Integer val=\"1\"/></UnderStyle>" + Environment.NewLine +
			"          <Name>" + Environment.NewLine +
			"            <AUni ws=\"en\">Adverb</AUni>" + Environment.NewLine +
			"            <AUni ws=\"es\">Adverbio</AUni>" + Environment.NewLine +
			"            <AUni ws=\"fr\">Adverbe</AUni>" + Environment.NewLine +
			"          </Name>" + Environment.NewLine +
			"          <Abbreviation>" + Environment.NewLine +
			"            <AUni ws=\"en\">adv</AUni>" + Environment.NewLine +
			"            <AUni ws=\"es\">adv</AUni>" + Environment.NewLine +
			"            <AUni ws=\"fr\">adv</AUni>" + Environment.NewLine +
			"          </Abbreviation>" + Environment.NewLine +
			"          <Description>" + Environment.NewLine +
			"            <AStr ws=\"en\">" + Environment.NewLine +
			"              <Run ws=\"en\">An adverb, narrowly defined, is a part of speech whose members modify verbs for such categories as time, manner, place, or direction. An adverb, broadly defined, is a part of speech whose members modify any constituent class of words other than nouns, such as verbs, adjectives, adverbs, phrases, clauses, or sentences. Under this definition, the possible type of modification depends on the class of the constituent being modified.</Run>" + Environment.NewLine +
			"            </AStr>" + Environment.NewLine +
			"          </Description>" + Environment.NewLine +
			"          <CatalogSourceId><Uni>Adverb</Uni></CatalogSourceId>" + Environment.NewLine +
			"        </PartOfSpeech>" + Environment.NewLine +
			"        <PartOfSpeech>" + Environment.NewLine +
			"          <ForeColor><Integer val=\"6303632\"/></ForeColor>" + Environment.NewLine +
			"          <BackColor><Integer val=\"-1073741824\"/></BackColor>" + Environment.NewLine +
			"          <UnderColor><Integer val=\"-1073741824\"/></UnderColor>" + Environment.NewLine +
			"          <UnderStyle><Integer val=\"1\"/></UnderStyle>" + Environment.NewLine +
			"          <Name>" + Environment.NewLine +
			"            <AUni ws=\"en\">Noun</AUni>" + Environment.NewLine +
			"            <AUni ws=\"es\">Nombre</AUni>" + Environment.NewLine +
			"            <AUni ws=\"fr\">Nom</AUni>" + Environment.NewLine +
			"          </Name>" + Environment.NewLine +
			"          <Abbreviation>" + Environment.NewLine +
			"            <AUni ws=\"en\">n</AUni>" + Environment.NewLine +
			"            <AUni ws=\"es\">sus</AUni>" + Environment.NewLine +
			"            <AUni ws=\"fr\">n</AUni>" + Environment.NewLine +
			"          </Abbreviation>" + Environment.NewLine +
			"          <Description>" + Environment.NewLine +
			"            <AStr ws=\"en\">" + Environment.NewLine +
			"              <Run ws=\"en\">A noun is a broad classification of parts of speech which include substantives and nominals.</Run>" + Environment.NewLine +
			"            </AStr>" + Environment.NewLine +
			"          </Description>" + Environment.NewLine +
			"          <CatalogSourceId><Uni>Noun</Uni></CatalogSourceId>" + Environment.NewLine +
			"          <SubPossibilities>" +
			"            <PartOfSpeech>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Nominal</AUni>" + Environment.NewLine +
			"                <AUni ws=\"es\">Nominal</AUni>" + Environment.NewLine +
			"                <AUni ws=\"fr\">Nominal</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">nom</AUni>" + Environment.NewLine +
			"                <AUni ws=\"es\">nom</AUni>" + Environment.NewLine +
			"                <AUni ws=\"fr\">nom</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">A nominal is a part of speech whose members differ grammatically from a substantive but which functions as one.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"                <AStr ws=\"fr\">" + Environment.NewLine +
			"                  <Run ws=\"fr\">Un nominal est un constituant syntaxique caractérisé par ses latitudes de fonctions. Selon ce critère on distingue les noms (ainsi que les pronoms et les syntagmes nominaux), capables en particulier d’assumer les fonctions de sujet et d’objet, et les adjectifs, assumant celle de déterminant du nom. Dans bien des langues le nominal a aussi un certain nombre de caractéristiques morphologiques. (Dans la terminologie française, « nominal » est plus vaste que nom, et comporte les nominaux substantifs ou noms, les nominaux adjectifs ou adjectifs, ainsi que les syntagmes nominaux et les pronoms.)</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <CatalogSourceId><Uni>Nominal</Uni></CatalogSourceId>" + Environment.NewLine +
			"              <SubPossibilities>" +
			"                <PartOfSpeech>" + Environment.NewLine +
			"                  <Name>" + Environment.NewLine +
			"                    <AUni ws=\"en\">Gerund</AUni>" + Environment.NewLine +
			"                    <AUni ws=\"fr\">Gérundif</AUni>" + Environment.NewLine +
			"                  </Name>" + Environment.NewLine +
			"                  <Abbreviation>" + Environment.NewLine +
			"                    <AUni ws=\"en\">ger</AUni>" + Environment.NewLine +
			"                    <AUni ws=\"fr\">ger</AUni>" + Environment.NewLine +
			"                  </Abbreviation>" + Environment.NewLine +
			"                  <Description>" + Environment.NewLine +
			"                    <AStr ws=\"en\">" + Environment.NewLine +
			"                      <Run ws=\"en\">A part of speech derived from a verb and used as a noun, usually restricted to non-finite forms of the verb.</Run>" + Environment.NewLine +
			"                    </AStr>" + Environment.NewLine +
			"                  </Description>" + Environment.NewLine +
			"                  <CatalogSourceId><Uni>Gerund</Uni></CatalogSourceId>" + Environment.NewLine +
			"                </PartOfSpeech>" + Environment.NewLine +
			"              </SubPossibilities>" +
			"            </PartOfSpeech>" + Environment.NewLine +
			"            <PartOfSpeech>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Substantive</AUni>" + Environment.NewLine +
			"                <AUni ws=\"fr\">Substantif</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">subs</AUni>" + Environment.NewLine +
			"                <AUni ws=\"fr\">subst</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">A substantive is a member of the syntactic class in which the names of physical, concrete, relatively unchanging experiences are most typically found whose members may act as subjects and objects, and most of whose members have inherently determined grammatical gender (in languages which inflect for gender).</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <CatalogSourceId><Uni>Substantive</Uni></CatalogSourceId>" + Environment.NewLine +
			"            </PartOfSpeech>" + Environment.NewLine +
			"          </SubPossibilities>" +
			"        </PartOfSpeech>" + Environment.NewLine +
			"      </Possibilities>" + Environment.NewLine +
			"    </CmPossibilityList>" + Environment.NewLine +
			"  </PartsOfSpeech>" + Environment.NewLine +
			"</LangProject>";
		#endregion

		#region s_ksSemanticDomainsXml
		/// <summary>
		/// The XML representation of a (tiny) subset of the Semantic Domains list.
		/// </summary>
		public static readonly string s_ksSemanticDomainsXml =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
			"<LangProject>" + Environment.NewLine +
			"  <SemanticDomainList>" + Environment.NewLine +
			"    <CmPossibilityList>" + Environment.NewLine +
			"      <IsSorted><Boolean val=\"true\"/></IsSorted>" + Environment.NewLine +
			"      <ItemClsid><Integer val=\"66\"/></ItemClsid>" + Environment.NewLine +
			"      <Depth><Integer val=\"127\"/></Depth>" + Environment.NewLine +
			"      <WsSelector><Integer val=\"-3\"/></WsSelector>" + Environment.NewLine +
			"      <Name>" + Environment.NewLine +
			"        <AUni ws=\"en\">Semantic Domains</AUni>" + Environment.NewLine +
			"      </Name>" + Environment.NewLine +
			"      <Abbreviation>" + Environment.NewLine +
			"        <AUni ws=\"en\">Sem</AUni>" + Environment.NewLine +
			"      </Abbreviation>" + Environment.NewLine +
			"      <Possibilities>" + Environment.NewLine +
			"        <CmSemanticDomain>" + Environment.NewLine +
			"          <Abbreviation>" + Environment.NewLine +
			"            <AUni ws=\"en\">1</AUni>" + Environment.NewLine +
			"          </Abbreviation>" + Environment.NewLine +
			"          <Name>" + Environment.NewLine +
			"            <AUni ws=\"en\">Universe, creation</AUni>" + Environment.NewLine +
			"          </Name>" + Environment.NewLine +
			"          <Description>" + Environment.NewLine +
			"            <AStr ws=\"en\">" + Environment.NewLine +
			"              <Run ws=\"en\">Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</Run>" + Environment.NewLine +
			"            </AStr>" + Environment.NewLine +
			"          </Description>" + Environment.NewLine +
			"          <OcmCodes>" + Environment.NewLine +
			"            <Uni>772 Cosmology;  130 Geography</Uni>" + Environment.NewLine +
			"          </OcmCodes>" + Environment.NewLine +
			"          <LouwNidaCodes>" + Environment.NewLine +
			"            <Uni>1A Universe, Creation;  14 Physical Events and States</Uni>" + Environment.NewLine +
			"          </LouwNidaCodes>" + Environment.NewLine +
			"          <Questions>" + Environment.NewLine +
			"            <CmDomainQ>" + Environment.NewLine +
			"              <Question>" + Environment.NewLine +
			"                <AUni ws=\"en\">(1) What words refer to everything we can see?</AUni>" + Environment.NewLine +
			"              </Question>" + Environment.NewLine +
			"              <ExampleWords>" + Environment.NewLine +
			"                <AUni ws=\"en\">universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>" + Environment.NewLine +
			"              </ExampleWords>" + Environment.NewLine +
			"              <ExampleSentences>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">In the beginning God created &lt;the heavens and the earth&gt;.</Run></AStr>" + Environment.NewLine +
			"              </ExampleSentences>" + Environment.NewLine +
			"            </CmDomainQ>" + Environment.NewLine +
			"          </Questions>" + Environment.NewLine +
			"          <SubPossibilities>" + Environment.NewLine +
			"            <CmSemanticDomain guid=\"999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C\">" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">1.1</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Sky</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">Use this domain for words related to the sky.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <LouwNidaCodes>" + Environment.NewLine +
			"                <Uni>1B Regions Above the Earth</Uni>" + Environment.NewLine +
			"              </LouwNidaCodes>" + Environment.NewLine +
			"              <Questions>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(1) What words are used to refer to the sky?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">sky, firmament, canopy, vault</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(2) What words refer to the air around the earth?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">air, atmosphere, airspace, stratosphere, ozone layer</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(3) What words are used to refer to the place or area beyond the sky?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">heaven, space, outer space, ether, void, solar system</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"              </Questions>" + Environment.NewLine +
			"              <SubPossibilities>" + Environment.NewLine +
			"                <CmSemanticDomain guid=\"DC1A2C6F-1B32-4631-8823-36DACC8CB7BB\">" + Environment.NewLine +
			"                  <Abbreviation>" + Environment.NewLine +
			"                    <AUni ws=\"en\">1.1.1</AUni>" + Environment.NewLine +
			"                  </Abbreviation>" + Environment.NewLine +
			"                  <Name>" + Environment.NewLine +
			"                    <AUni ws=\"en\">Sun</AUni>" + Environment.NewLine +
			"                  </Name>" + Environment.NewLine +
			"                  <Description>" + Environment.NewLine +
			"                    <AStr ws=\"en\">" + Environment.NewLine +
			"                      <Run ws=\"en\">Use this domain for words related to the sun. The sun does three basic things. It moves, it gives light, and it gives heat. These three actions are involved in the meanings of most of the words in this domain. Since the sun moves below the horizon, many words refer to it setting or rising. Since the sun is above the clouds, many words refer to it moving behind the clouds and the clouds blocking its light. The sun's light and heat also produce secondary effects. The sun causes plants to grow, and it causes damage to things.</Run>" + Environment.NewLine +
			"                    </AStr>" + Environment.NewLine +
			"                  </Description>" + Environment.NewLine +
			"                  <LouwNidaCodes>" + Environment.NewLine +
			"                    <Uni>1D Heavenly Bodies</Uni>" + Environment.NewLine +
			"                  </LouwNidaCodes>" + Environment.NewLine +
			"                  <Questions>" + Environment.NewLine +
			"                    <CmDomainQ>" + Environment.NewLine +
			"                      <Question>" + Environment.NewLine +
			"                        <AUni ws=\"en\">(1) What words refer to the sun?</AUni>" + Environment.NewLine +
			"                      </Question>" + Environment.NewLine +
			"                      <ExampleWords>" + Environment.NewLine +
			"                        <AUni ws=\"en\">sun, solar, sol, daystar, our star</AUni>" + Environment.NewLine +
			"                      </ExampleWords>" + Environment.NewLine +
			"                    </CmDomainQ>" + Environment.NewLine +
			"                    <CmDomainQ>" + Environment.NewLine +
			"                      <Question>" + Environment.NewLine +
			"                        <AUni ws=\"en\">(2) What words refer to how the sun moves?</AUni>" + Environment.NewLine +
			"                      </Question>" + Environment.NewLine +
			"                      <ExampleWords>" + Environment.NewLine +
			"                        <AUni ws=\"en\">rise, set, cross the sky, come up, go down, sink</AUni>" + Environment.NewLine +
			"                      </ExampleWords>" + Environment.NewLine +
			"                    </CmDomainQ>" + Environment.NewLine +
			"                    <CmDomainQ>" + Environment.NewLine +
			"                      <Question>" + Environment.NewLine +
			"                        <AUni ws=\"en\">(3) What words refer to the time when the sun rises?</AUni>" + Environment.NewLine +
			"                      </Question>" + Environment.NewLine +
			"                      <ExampleWords>" + Environment.NewLine +
			"                        <AUni ws=\"en\">dawn, sunrise, sunup, daybreak, cockcrow, </AUni>" + Environment.NewLine +
			"                      </ExampleWords>" + Environment.NewLine +
			"                      <ExampleSentences>" + Environment.NewLine +
			"                        " + Environment.NewLine +
			"                        <AStr ws=\"en\">" + Environment.NewLine +
			"                        <Run ws=\"en\">We got up before &lt;dawn&gt;, in order to get an early start.</Run></AStr>" + Environment.NewLine +
			"                      </ExampleSentences>" + Environment.NewLine +
			"                    </CmDomainQ>" + Environment.NewLine +
			"                  </Questions>" + Environment.NewLine +
			"                  <SubPossibilities>" + Environment.NewLine +
			"                    <CmSemanticDomain guid=\"1BD42665-0610-4442-8D8D-7C666FEE3A6D\">" + Environment.NewLine +
			"                      <Abbreviation>" + Environment.NewLine +
			"                        <AUni ws=\"en\">1.1.1.1</AUni>" + Environment.NewLine +
			"                      </Abbreviation>" + Environment.NewLine +
			"                      <Name>" + Environment.NewLine +
			"                        <AUni ws=\"en\">Moon</AUni>" + Environment.NewLine +
			"                      </Name>" + Environment.NewLine +
			"                      <Description>" + Environment.NewLine +
			"                        <AStr ws=\"en\">" + Environment.NewLine +
			"                          <Run ws=\"en\">Use this domain for words related to the moon. In your culture people may believe things about the moon. For instance in European culture people used to believe that the moon caused people to become crazy. So in English we have words like \"moon-struck\" and \"lunatic.\" You should include such words in this domain.</Run>" + Environment.NewLine +
			"                        </AStr>" + Environment.NewLine +
			"                      </Description>" + Environment.NewLine +
			"                      <Questions>" + Environment.NewLine +
			"                        <CmDomainQ>" + Environment.NewLine +
			"                          <Question>" + Environment.NewLine +
			"                            <AUni ws=\"en\">(1) What words refer to the moon?</AUni>" + Environment.NewLine +
			"                          </Question>" + Environment.NewLine +
			"                          <ExampleWords>" + Environment.NewLine +
			"                            <AUni ws=\"en\">moon, lunar, satellite</AUni>" + Environment.NewLine +
			"                          </ExampleWords>" + Environment.NewLine +
			"                        </CmDomainQ>" + Environment.NewLine +
			"                      </Questions>" + Environment.NewLine +
			"                    </CmSemanticDomain>" + Environment.NewLine +
			"                    <CmSemanticDomain guid=\"B044E890-CE30-455C-AEDE-7E9D5569396E\">" + Environment.NewLine +
			"                      <Abbreviation>" + Environment.NewLine +
			"                        <AUni ws=\"en\">1.1.1.2</AUni>" + Environment.NewLine +
			"                      </Abbreviation>" + Environment.NewLine +
			"                      <Name>" + Environment.NewLine +
			"                        <AUni ws=\"en\">Star</AUni>" + Environment.NewLine +
			"                      </Name>" + Environment.NewLine +
			"                      <Description>" + Environment.NewLine +
			"                        <AStr ws=\"en\">" + Environment.NewLine +
			"                          <Run ws=\"en\">Use this domain for words related to the stars and other heavenly bodies.</Run>" + Environment.NewLine +
			"                        </AStr>" + Environment.NewLine +
			"                      </Description>" + Environment.NewLine +
			"                      <Questions>" + Environment.NewLine +
			"                        <CmDomainQ>" + Environment.NewLine +
			"                          <Question>" + Environment.NewLine +
			"                            <AUni ws=\"en\">(1) What words are used to refer to the stars?</AUni>" + Environment.NewLine +
			"                          </Question>" + Environment.NewLine +
			"                          <ExampleWords>" + Environment.NewLine +
			"                            <AUni ws=\"en\">star, starry, stellar</AUni>" + Environment.NewLine +
			"                          </ExampleWords>" + Environment.NewLine +
			"                        </CmDomainQ>" + Environment.NewLine +
			"                        <CmDomainQ>" + Environment.NewLine +
			"                          <Question>" + Environment.NewLine +
			"                            <AUni ws=\"en\">(2) What words describe the sky when the stars are shining?</AUni>" + Environment.NewLine +
			"                          </Question>" + Environment.NewLine +
			"                          <ExampleWords>" + Environment.NewLine +
			"                            <AUni ws=\"en\">starlit (sky), (sky is) ablaze with stars, starry (sky), star studded (sky), stars are shining</AUni>" + Environment.NewLine +
			"                          </ExampleWords>" + Environment.NewLine +
			"                        </CmDomainQ>" + Environment.NewLine +
			"                      </Questions>" + Environment.NewLine +
			"                    </CmSemanticDomain>" + Environment.NewLine +
			"                  </SubPossibilities>" + Environment.NewLine +
			"                </CmSemanticDomain>" + Environment.NewLine +
			"                <CmSemanticDomain guid=\"E836B01B-6C1A-4D41-B90A-EA5F349F88D4\">" + Environment.NewLine +
			"                  <Abbreviation>" + Environment.NewLine +
			"                    <AUni ws=\"en\">1.1.2</AUni>" + Environment.NewLine +
			"                  </Abbreviation>" + Environment.NewLine +
			"                  <Name>" + Environment.NewLine +
			"                    <AUni ws=\"en\">Air</AUni>" + Environment.NewLine +
			"                  </Name>" + Environment.NewLine +
			"                  <Description>" + Environment.NewLine +
			"                    <AStr ws=\"en\">" + Environment.NewLine +
			"                      <Run ws=\"en\">Use this domain for words related to the air around us, including the air we breathe and the atmosphere around the earth.</Run>" + Environment.NewLine +
			"                    </AStr>" + Environment.NewLine +
			"                  </Description>" + Environment.NewLine +
			"                  <LouwNidaCodes>" + Environment.NewLine +
			"                    <Uni>2B Air</Uni>" + Environment.NewLine +
			"                  </LouwNidaCodes>" + Environment.NewLine +
			"                  <RelatedDomains>" + Environment.NewLine +
			"                    <Link guid=\"999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C\"/>" + Environment.NewLine +
			"                    <Link guid=\"7FE69C4C-2603-4949-AFCA-F39C010AD24E\"/>" + Environment.NewLine +
			"                  </RelatedDomains>" + Environment.NewLine +
			"                  <Questions>" + Environment.NewLine +
			"                    <CmDomainQ>" + Environment.NewLine +
			"                      <Question>" + Environment.NewLine +
			"                        <AUni ws=\"en\">(1) What words refer to the air we breathe?</AUni>" + Environment.NewLine +
			"                      </Question>" + Environment.NewLine +
			"                      <ExampleWords>" + Environment.NewLine +
			"                        <AUni ws=\"en\">air</AUni>" + Environment.NewLine +
			"                      </ExampleWords>" + Environment.NewLine +
			"                    </CmDomainQ>" + Environment.NewLine +
			"                    <CmDomainQ>" + Environment.NewLine +
			"                      <Question>" + Environment.NewLine +
			"                        <AUni ws=\"en\">(2) What words refer to how much water is in the air?</AUni>" + Environment.NewLine +
			"                      </Question>" + Environment.NewLine +
			"                      <ExampleWords>" + Environment.NewLine +
			"                        <AUni ws=\"en\">humid, humidity, damp, dry, sticky, muggy</AUni>" + Environment.NewLine +
			"                      </ExampleWords>" + Environment.NewLine +
			"                    </CmDomainQ>" + Environment.NewLine +
			"                  </Questions>" + Environment.NewLine +
			"                </CmSemanticDomain>" + Environment.NewLine +
			"              </SubPossibilities>" + Environment.NewLine +
			"            </CmSemanticDomain>" + Environment.NewLine +
			"            <CmSemanticDomain guid=\"B47D2604-8B23-41E9-9158-01526DD83894\">" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">1.2</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">World</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">Use this domain for words referring to the planet we live on.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <Questions>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(1) What words refer to the planet we live on?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">the world, earth, the Earth, the globe, the planet</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"              </Questions>" + Environment.NewLine +
			"            </CmSemanticDomain>" + Environment.NewLine +
			"            <CmSemanticDomain guid=\"60364974-A005-4567-82E9-7AAEFF894AB0\">" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">1.3</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Water</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">Use this domain for general words referring to water.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <LouwNidaCodes>" + Environment.NewLine +
			"                <Uni>2D Water</Uni>" + Environment.NewLine +
			"              </LouwNidaCodes>" + Environment.NewLine +
			"              <Questions>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(1) What general words refer to water?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">water, H2O, moisture</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(2) What words describe something that belongs to the water or is found in water?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">watery, aquatic, amphibious</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(3) What words describe something that water cannot pass through?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">waterproof, watertight</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"              </Questions>" + Environment.NewLine +
			"            </CmSemanticDomain>" + Environment.NewLine +
			"          </SubPossibilities>" + Environment.NewLine +
			"        </CmSemanticDomain>" + Environment.NewLine +
			"        <CmSemanticDomain guid=\"BA06DE9E-63E1-43E6-AE94-77BEA498379A\">" + Environment.NewLine +
			"          <Abbreviation>" + Environment.NewLine +
			"            <AUni ws=\"en\">2</AUni>" + Environment.NewLine +
			"          </Abbreviation>" + Environment.NewLine +
			"          <Name>" + Environment.NewLine +
			"            <AUni ws=\"en\">Person</AUni>" + Environment.NewLine +
			"          </Name>" + Environment.NewLine +
			"          <Description>" + Environment.NewLine +
			"            <AStr ws=\"en\">" + Environment.NewLine +
			"              <Run ws=\"en\">Use this domain for general words for a person or all mankind.</Run>" + Environment.NewLine +
			"            </AStr>" + Environment.NewLine +
			"          </Description>" + Environment.NewLine +
			"          <LouwNidaCodes>" + Environment.NewLine +
			"            <Uni>9 People;  9A Human Beings</Uni>" + Environment.NewLine +
			"          </LouwNidaCodes>" + Environment.NewLine +
			"          <Questions>" + Environment.NewLine +
			"            <CmDomainQ>" + Environment.NewLine +
			"              <Question>" + Environment.NewLine +
			"                <AUni ws=\"en\">(1) What words refer to a single member of the human race?</AUni>" + Environment.NewLine +
			"              </Question>" + Environment.NewLine +
			"              <ExampleWords>" + Environment.NewLine +
			"                <AUni ws=\"en\">person, human being, man, individual, figure</AUni>" + Environment.NewLine +
			"              </ExampleWords>" + Environment.NewLine +
			"            </CmDomainQ>" + Environment.NewLine +
			"            <CmDomainQ>" + Environment.NewLine +
			"              <Question>" + Environment.NewLine +
			"                <AUni ws=\"en\">(2) What words refer to a person when you aren't sure who the person is?</AUni>" + Environment.NewLine +
			"              </Question>" + Environment.NewLine +
			"              <ExampleWords>" + Environment.NewLine +
			"                <AUni ws=\"en\">someone, somebody</AUni>" + Environment.NewLine +
			"              </ExampleWords>" + Environment.NewLine +
			"            </CmDomainQ>" + Environment.NewLine +
			"          </Questions>" + Environment.NewLine +
			"          <SubPossibilities>" + Environment.NewLine +
			"            <CmSemanticDomain guid=\"1B0270A5-BABF-4151-99F5-279BA5A4B044\">" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">2.1</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Body</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">Use this domain for general words for the whole human body, and general words for any part of the body. Use a drawing or photo to label each part. Some words may be more general than others are and include some of the other words. For instance 'head' is more general than 'face' or 'nose'. Be sure that both general and specific parts are labeled.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <OcmCodes>" + Environment.NewLine +
			"                <Uni>140 Human Biology;  141 Anthropometry;  142 Descriptive Somatology</Uni>" + Environment.NewLine +
			"              </OcmCodes>" + Environment.NewLine +
			"              <LouwNidaCodes>" + Environment.NewLine +
			"                <Uni>8 Body, Body Parts, and Body Products;  8A Body;  8B Parts of the Body</Uni>" + Environment.NewLine +
			"              </LouwNidaCodes>" + Environment.NewLine +
			"              <Questions>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(1) What words refer to the body?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">body, </AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(2) What words refer to the shape of a person's body?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">build, figure, physique, </AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(3) What general words refer to a part of the body?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">part of the body, body part, anatomy, appendage, member, orifice, </AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"              </Questions>" + Environment.NewLine +
			"            </CmSemanticDomain>" + Environment.NewLine +
			"            <CmSemanticDomain guid=\"7FE69C4C-2603-4949-AFCA-F39C010AD24E\">" + Environment.NewLine +
			"              <Abbreviation>" + Environment.NewLine +
			"                <AUni ws=\"en\">2.2</AUni>" + Environment.NewLine +
			"              </Abbreviation>" + Environment.NewLine +
			"              <Name>" + Environment.NewLine +
			"                <AUni ws=\"en\">Body functions</AUni>" + Environment.NewLine +
			"              </Name>" + Environment.NewLine +
			"              <Description>" + Environment.NewLine +
			"                <AStr ws=\"en\">" + Environment.NewLine +
			"                  <Run ws=\"en\">Use this domain for the functions and actions of the whole body. Use the subdomains in this section  for functions, actions, secretions, and products of various parts of the body. In each domain include any special words that are used of animals.</Run>" + Environment.NewLine +
			"                </AStr>" + Environment.NewLine +
			"              </Description>" + Environment.NewLine +
			"              <OcmCodes>" + Environment.NewLine +
			"                <Uni>147 Physiological Data;  514 Elimination</Uni>" + Environment.NewLine +
			"              </OcmCodes>" + Environment.NewLine +
			"              <LouwNidaCodes>" + Environment.NewLine +
			"                <Uni>8C Physiological Products of the Body;  23 Physiological Processes and States</Uni>" + Environment.NewLine +
			"              </LouwNidaCodes>" + Environment.NewLine +
			"              <Questions>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(1) What general words refer to the functions of the body?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">function</AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"                <CmDomainQ>" + Environment.NewLine +
			"                  <Question>" + Environment.NewLine +
			"                    <AUni ws=\"en\">(2) What general words refer to secretions of the body?</AUni>" + Environment.NewLine +
			"                  </Question>" + Environment.NewLine +
			"                  <ExampleWords>" + Environment.NewLine +
			"                    <AUni ws=\"en\">secrete, secretion, excrete, excretion, product, fluid, body fluids, discharge, flux, </AUni>" + Environment.NewLine +
			"                  </ExampleWords>" + Environment.NewLine +
			"                </CmDomainQ>" + Environment.NewLine +
			"              </Questions>" + Environment.NewLine +
			"            </CmSemanticDomain>" + Environment.NewLine +
			"          </SubPossibilities>" + Environment.NewLine +
			"        </CmSemanticDomain>" + Environment.NewLine +
			"      </Possibilities>" + Environment.NewLine +
			"    </CmPossibilityList>" + Environment.NewLine +
			"  </SemanticDomainList>" + Environment.NewLine +
			"</LangProject>" + Environment.NewLine;
		#endregion

		/// <summary>
		/// Setup method: create a memory-only mock cache and empty language project.
		/// </summary>
		[SetUp]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ThreadHelper is disposed in DestroyMockCache()")]
		public void CreateMockCache()
		{
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", "es", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories);
		}

		/// <summary>
		/// Teardown method: destroy the memory-only mock cache.
		/// </summary>
		[TearDown]
		public void DestroyMockCache()
		{
			m_cache.Dispose();
			m_cache = null;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportList.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportList()
		{
			var list = m_cache.LangProject.PartsOfSpeechOA;
			Assert.AreEqual(0, list.PossibilitiesOS.Count);
			Assert.AreEqual(0, list.Depth);
			Assert.AreEqual(0, list.WsSelector);
			Assert.IsFalse(list.IsSorted);
			Assert.IsFalse(list.UseExtendedFields);
			Assert.AreEqual(5049, list.ItemClsid);
			Assert.AreEqual(0, list.Abbreviation.StringCount);
			Assert.AreEqual(0, list.Name.StringCount);

			var xl = new XmlList();
			using (var reader = new StringReader(s_ksPartsOfSpeechXml))
			{
				xl.ImportList(m_cache.LangProject, "PartsOfSpeech", reader, null);

				var wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
				var wsEs = m_cache.WritingSystemFactory.GetWsFromStr("es");
				var wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");

				Assert.AreEqual(2, list.PossibilitiesOS.Count);
				Assert.AreEqual(127, list.Depth);
				Assert.AreEqual(-3, list.WsSelector);
				Assert.IsTrue(list.IsSorted);
				Assert.IsTrue(list.UseExtendedFields);
				Assert.AreEqual(5049, list.ItemClsid);
				Assert.AreEqual(1, list.Abbreviation.StringCount);
				Assert.AreEqual("Pos", list.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(3, list.Name.StringCount);
				Assert.AreEqual("Parts Of Speech", list.Name.get_String(wsEn).Text);
				Assert.AreEqual("Categori\u0301as Grama\u0301ticas", list.Name.get_String(wsEs).Text);
				Assert.AreEqual("Parties du Discours", list.Name.get_String(wsFr).Text);

				var adverb = list.PossibilitiesOS[0] as IPartOfSpeech;
				Assert.IsNotNull(adverb);
				Assert.AreEqual(6303632, adverb.ForeColor);
				Assert.AreEqual(-1073741824, adverb.BackColor);
				Assert.AreEqual(-1073741824, adverb.UnderColor);
				Assert.AreEqual(1, adverb.UnderStyle);
				Assert.AreEqual(3, adverb.Abbreviation.StringCount);
				Assert.AreEqual("adv", adverb.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual("adv", adverb.Abbreviation.get_String(wsEs).Text);
				Assert.AreEqual("adv", adverb.Abbreviation.get_String(wsFr).Text);
				Assert.AreEqual(3, adverb.Name.StringCount);
				Assert.AreEqual("Adverb", adverb.Name.get_String(wsEn).Text);
				Assert.AreEqual("Adverbio", adverb.Name.get_String(wsEs).Text);
				Assert.AreEqual("Adverbe", adverb.Name.get_String(wsFr).Text);
				Assert.AreEqual(1, adverb.Description.StringCount);
				var desc = adverb.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("An adverb, narrowly defined, is a part of"));
				Assert.IsTrue(desc.EndsWith(" the class of the constituent being modified."));
				Assert.AreEqual(432, desc.Length);
				Assert.AreEqual("Adverb", adverb.CatalogSourceId);
				Assert.AreEqual(0, adverb.SubPossibilitiesOS.Count);

				var noun = list.PossibilitiesOS[1] as IPartOfSpeech;
				Assert.IsNotNull(noun);
				Assert.AreEqual(6303632, noun.ForeColor);
				Assert.AreEqual(-1073741824, noun.BackColor);
				Assert.AreEqual(-1073741824, noun.UnderColor);
				Assert.AreEqual(1, noun.UnderStyle);
				Assert.AreEqual(3, noun.Abbreviation.StringCount);
				Assert.AreEqual("n", noun.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual("sus", noun.Abbreviation.get_String(wsEs).Text);
				Assert.AreEqual("n", noun.Abbreviation.get_String(wsFr).Text);
				Assert.AreEqual(3, noun.Name.StringCount);
				Assert.AreEqual("Noun", noun.Name.get_String(wsEn).Text);
				Assert.AreEqual("Nombre", noun.Name.get_String(wsEs).Text);
				Assert.AreEqual("Nom", noun.Name.get_String(wsFr).Text);
				Assert.AreEqual(1, noun.Description.StringCount);
				desc = noun.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("A noun is a broad classification of parts of speech"));
				Assert.IsTrue(desc.EndsWith(" which include substantives and nominals."));
				Assert.AreEqual(92, desc.Length);
				Assert.AreEqual("Noun", noun.CatalogSourceId);
				Assert.AreEqual(2, noun.SubPossibilitiesOS.Count);

				var nominal = noun.SubPossibilitiesOS[0] as IPartOfSpeech;
				Assert.IsNotNull(nominal);
				Assert.AreEqual(3, nominal.Abbreviation.StringCount);
				Assert.AreEqual("nom", nominal.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual("nom", nominal.Abbreviation.get_String(wsEs).Text);
				Assert.AreEqual("nom", nominal.Abbreviation.get_String(wsFr).Text);
				Assert.AreEqual(3, nominal.Name.StringCount);
				Assert.AreEqual("Nominal", nominal.Name.get_String(wsEn).Text);
				Assert.AreEqual("Nominal", nominal.Name.get_String(wsEs).Text);
				Assert.AreEqual("Nominal", nominal.Name.get_String(wsFr).Text);
				Assert.AreEqual(2, nominal.Description.StringCount);
				desc = nominal.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("A nominal is a part of speech whose members differ"));
				Assert.IsTrue(desc.EndsWith(" from a substantive but which functions as one."));
				Assert.AreEqual(111, desc.Length);
				desc = nominal.Description.get_String(wsFr).Text;
				Assert.IsTrue(desc.StartsWith("Un nominal est un constituant syntaxique caractérisé par"));
				Assert.IsTrue(desc.EndsWith(", ainsi que les syntagmes nominaux et les pronoms.)"));
				Assert.AreEqual(602, desc.Length);
				Assert.AreEqual("Nominal", nominal.CatalogSourceId);
				Assert.AreEqual(1, nominal.SubPossibilitiesOS.Count);

				var gerund = nominal.SubPossibilitiesOS[0] as IPartOfSpeech;
				Assert.IsNotNull(gerund);
				Assert.AreEqual(2, gerund.Abbreviation.StringCount);
				Assert.AreEqual("ger", gerund.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual("ger", gerund.Abbreviation.get_String(wsFr).Text);
				Assert.AreEqual(2, gerund.Name.StringCount);
				Assert.AreEqual("Gerund", gerund.Name.get_String(wsEn).Text);
				Assert.AreEqual("Ge\u0301rundif", gerund.Name.get_String(wsFr).Text);
				Assert.AreEqual(1, gerund.Description.StringCount);
				desc = gerund.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("A part of speech derived from a verb and used as a noun,"));
				Assert.IsTrue(desc.EndsWith(" usually restricted to non-finite forms of the verb."));
				Assert.AreEqual(108, desc.Length);
				Assert.AreEqual("Gerund", gerund.CatalogSourceId);
				Assert.AreEqual(0, gerund.SubPossibilitiesOS.Count);

				var substantive = noun.SubPossibilitiesOS[1] as IPartOfSpeech;
				Assert.IsNotNull(substantive);
				Assert.AreEqual(2, substantive.Abbreviation.StringCount);
				Assert.AreEqual("subs", substantive.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual("subst", substantive.Abbreviation.get_String(wsFr).Text);
				Assert.AreEqual(2, substantive.Name.StringCount);
				Assert.AreEqual("Substantive", substantive.Name.get_String(wsEn).Text);
				Assert.AreEqual("Substantif", substantive.Name.get_String(wsFr).Text);
				Assert.AreEqual(1, substantive.Description.StringCount);
				desc = substantive.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("A substantive is a member of the syntactic class in which"));
				Assert.IsTrue(desc.EndsWith(" grammatical gender (in languages which inflect for gender)."));
				Assert.AreEqual(309, desc.Length);
				Assert.AreEqual("Substantive", substantive.CatalogSourceId);
				Assert.AreEqual(0, substantive.SubPossibilitiesOS.Count);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Another test of the method ImportList.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportList2()
		{
			var list = m_cache.LangProject.SemanticDomainListOA;
			Assert.AreEqual(0, list.PossibilitiesOS.Count);
			Assert.AreEqual(0, list.Depth);
			Assert.AreEqual(0, list.WsSelector);
			Assert.IsFalse(list.IsSorted);
			Assert.IsFalse(list.UseExtendedFields);
			Assert.AreEqual(66, list.ItemClsid);
			Assert.AreEqual(0, list.Abbreviation.StringCount);
			Assert.AreEqual(0, list.Name.StringCount);

			var xl = new XmlList();
			using (var reader = new StringReader(s_ksSemanticDomainsXml))
			{
				xl.ImportList(m_cache.LangProject, "SemanticDomainList", reader, null);

				var wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
				var wsEs = m_cache.WritingSystemFactory.GetWsFromStr("es");
				var wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");

				Assert.AreEqual(2, list.PossibilitiesOS.Count);
				Assert.AreEqual(127, list.Depth);
				Assert.AreEqual(-3, list.WsSelector);
				Assert.IsTrue(list.IsSorted);
				Assert.IsFalse(list.UseExtendedFields);
				Assert.AreEqual(66, list.ItemClsid);
				Assert.AreEqual(1, list.Abbreviation.StringCount);
				Assert.AreEqual("Sem", list.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(1, list.Name.StringCount);
				Assert.AreEqual("Semantic Domains", list.Name.get_String(wsEn).Text);

				ICmSemanticDomain sem1 = list.PossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem1);
				Assert.AreNotEqual(sem1.Guid, Guid.Empty);
				Assert.AreEqual(1, sem1.Abbreviation.StringCount);
				Assert.AreEqual("1", sem1.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(1, sem1.Name.StringCount);
				Assert.AreEqual("Universe, creation", sem1.Name.get_String(wsEn).Text);
				Assert.AreEqual(1, sem1.Description.StringCount);
				string desc = sem1.Description.get_String(wsEn).Text;
				Assert.IsTrue(desc.StartsWith("Use this domain for general words referring to the physical universe."));
				Assert.IsTrue(desc.EndsWith(" such as 'everything you can see' or 'everything that exists'."));
				Assert.AreEqual(313, desc.Length);
				Assert.AreEqual("772 Cosmology;  130 Geography", sem1.OcmCodes);
				Assert.AreEqual("1A Universe, Creation;  14 Physical Events and States", sem1.LouwNidaCodes);
				Assert.AreEqual(1, sem1.QuestionsOS.Count);
				ICmDomainQ cdq = sem1.QuestionsOS[0];
				Assert.AreEqual(1, cdq.Question.StringCount);
				Assert.AreEqual("(1) What words refer to everything we can see?", cdq.Question.get_String(wsEn).Text);
				Assert.AreEqual(1, cdq.ExampleWords.StringCount);
				Assert.AreEqual("universe, creation, cosmos, heaven and earth, macrocosm, everything that exists",
					cdq.ExampleWords.get_String(wsEn).Text);
				Assert.AreEqual(1, cdq.ExampleSentences.StringCount);
				Assert.AreEqual("In the beginning God created <the heavens and the earth>.",
					cdq.ExampleSentences.get_String(wsEn).Text);
				Assert.AreEqual(3, sem1.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem11 = sem1.SubPossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem11);
				Assert.AreEqual(sem11.Guid.ToString(), "999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C".ToLowerInvariant());
				Assert.AreEqual(1, sem11.Abbreviation.StringCount);
				Assert.AreEqual("1.1", sem11.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(1, sem11.Name.StringCount);
				Assert.AreEqual("Sky", sem11.Name.get_String(wsEn).Text);
				Assert.AreEqual(1, sem11.Description.StringCount);
				Assert.AreEqual("Use this domain for words related to the sky.",
					sem11.Description.get_String(wsEn).Text);
				Assert.IsTrue(String.IsNullOrEmpty(sem11.OcmCodes));
				Assert.AreEqual("1B Regions Above the Earth", sem11.LouwNidaCodes);
				Assert.AreEqual(3, sem11.QuestionsOS.Count);
				cdq = sem11.QuestionsOS[0];
				Assert.AreEqual(1, cdq.Question.StringCount);
				Assert.AreEqual("(1) What words are used to refer to the sky?",
					cdq.Question.get_String(wsEn).Text);
				Assert.AreEqual(1, cdq.ExampleWords.StringCount);
				Assert.AreEqual("sky, firmament, canopy, vault",
					cdq.ExampleWords.get_String(wsEn).Text);
				Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
				cdq = sem11.QuestionsOS[1];
				Assert.AreEqual(1, cdq.Question.StringCount);
				Assert.AreEqual("(2) What words refer to the air around the earth?",
					cdq.Question.get_String(wsEn).Text);
				Assert.AreEqual(1, cdq.ExampleWords.StringCount);
				Assert.AreEqual("air, atmosphere, airspace, stratosphere, ozone layer",
					cdq.ExampleWords.get_String(wsEn).Text);
				Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
				cdq = sem11.QuestionsOS[2];
				Assert.AreEqual(1, cdq.Question.StringCount);
				Assert.AreEqual("(3) What words are used to refer to the place or area beyond the sky?",
					cdq.Question.get_String(wsEn).Text);
				Assert.AreEqual(1, cdq.ExampleWords.StringCount);
				Assert.AreEqual("heaven, space, outer space, ether, void, solar system",
					cdq.ExampleWords.get_String(wsEn).Text);
				Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
				Assert.AreEqual(2, sem11.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem111 = sem11.SubPossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem111);
				Assert.AreEqual(1, sem111.Abbreviation.StringCount);
				Assert.AreEqual("1.1.1", sem111.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(2, sem111.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem1111 = sem111.SubPossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem1111);
				Assert.AreEqual(1, sem1111.Abbreviation.StringCount);
				Assert.AreEqual("1.1.1.1", sem1111.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem1111.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem1112 = sem111.SubPossibilitiesOS[1] as ICmSemanticDomain;
				Assert.IsNotNull(sem1112);
				Assert.AreEqual(1, sem1112.Abbreviation.StringCount);
				Assert.AreEqual("1.1.1.2", sem1112.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem1112.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem112 = sem11.SubPossibilitiesOS[1] as ICmSemanticDomain;
				Assert.IsNotNull(sem112);
				Assert.AreEqual(1, sem112.Abbreviation.StringCount);
				Assert.AreEqual("1.1.2", sem112.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem112.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem12 = sem1.SubPossibilitiesOS[1] as ICmSemanticDomain;
				Assert.IsNotNull(sem12);
				Assert.AreEqual(1, sem12.Abbreviation.StringCount);
				Assert.AreEqual("1.2", sem12.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem12.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem13 = sem1.SubPossibilitiesOS[2] as ICmSemanticDomain;
				Assert.IsNotNull(sem13);
				Assert.AreEqual(1, sem13.Abbreviation.StringCount);
				Assert.AreEqual("1.3", sem13.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem13.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem2 = list.PossibilitiesOS[1] as ICmSemanticDomain;
				Assert.IsNotNull(sem2);
				Assert.AreEqual(1, sem2.Abbreviation.StringCount);
				Assert.AreEqual("2", sem2.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(2, sem2.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem21 = sem2.SubPossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem21);
				Assert.AreEqual(1, sem21.Abbreviation.StringCount);
				Assert.AreEqual("2.1", sem21.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem21.SubPossibilitiesOS.Count);

				ICmSemanticDomain sem22 = sem2.SubPossibilitiesOS[1] as ICmSemanticDomain;
				Assert.IsNotNull(sem22);
				Assert.AreEqual(1, sem22.Abbreviation.StringCount);
				Assert.AreEqual("2.2", sem22.Abbreviation.get_String(wsEn).Text);
				Assert.AreEqual(0, sem22.SubPossibilitiesOS.Count);
			}
		}
	}
}
