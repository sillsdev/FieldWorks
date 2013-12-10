// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportDialogTests.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using NUnit.Framework;

using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test exporting.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportDialogTests : BaseTest
	{
		/// <summary>
		/// The XML representation of a (tiny) subset of the Semantic Domains list.
		/// </summary>
		public readonly string s_ksSemanticDomainsXml =
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
			"        <CmSemanticDomain guid=\"63403699-07C1-43F3-A47C-069D6E4316E5\">" + Environment.NewLine +
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
			"                <AUni ws=\"fr\">(1) Quels mots se réfèrent à tout ce que nous pouvons voir?</AUni>" + Environment.NewLine +
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

		private FdoCache m_cache;

		#region Setup and Helper Methods

		/// <summary>
		/// Setup method: create a memory-only mock cache and empty language project.
		/// </summary>
		[SetUp]
		public void CreateMockCache()
		{
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", "fr", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories);
			var xl = new XmlList();
			using (var reader = new StringReader(s_ksSemanticDomainsXml))
				xl.ImportList(m_cache.LangProject, "SemanticDomainList", reader, null);
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

		#endregion

		/// <summary>
		/// Test the main options of ExportSemanticDomains.
		/// Note: this test is in some ways too precise. For example, it would really be better to produce the red
		/// effect with different styles, which would change the result here. Also, order of attributes is not significant.
		/// There are many other things about the export we could verify.
		/// </summary>
		[Test]
		public void ExportSemanticDomains()
		{
			using (var exportDlg = new ExportDialog())
			{
				exportDlg.SetCache(m_cache);
				var tempPath = Path.GetTempFileName();
				var fxt = new ExportDialog.FxtType();
				fxt.m_sXsltFiles = "SemDomQs.xsl";
				var fxtPath = Path.Combine(exportDlg.FxtDirectory, "SemDomQs.xml");
				var wss = new List<int>();
				wss.Add(m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en"));
				exportDlg.SetTranslationWritingSystems(wss);

				exportDlg.ExportSemanticDomains(new DummyProgressDlg(), new object[] {tempPath, fxt, fxtPath, false});

				string result;
				using (var reader = new StreamReader(tempPath, Encoding.UTF8))
					result = reader.ReadToEnd();
				File.Delete(tempPath);
				Assert.That(result, Is.StringContaining("What words refer to the sun?"));
				Assert.That(result, Is.Not.StringContaining("1.1.1.11.1.1.1"), "should not output double abbr for en");
				Assert.That(result, Is.Not.StringContaining("class: english"),
					"English should not give anything the missing translation style");

				wss.Clear();
				wss.Add(m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("fr"));

				exportDlg.ExportSemanticDomains(new DummyProgressDlg(), new object[] {tempPath, fxt, fxtPath, false});

				using (var reader = new StreamReader(tempPath, Encoding.UTF8))
					result = reader.ReadToEnd();
				File.Delete(tempPath);
				Assert.That(result, Is.Not.StringContaining("What words refer to the sun?"),
					"french export should not have english questions");
				Assert.That(result, Is.StringContaining("<p class=\"quest1\" lang=\"fr\">(1) Quels mots se"),
					"French export should have the French question (not english class)");

				exportDlg.ExportSemanticDomains(new DummyProgressDlg(), new object[] {tempPath, fxt, fxtPath, true});
				using (var reader = new StreamReader(tempPath, Encoding.UTF8))
					result = reader.ReadToEnd();
				File.Delete(tempPath);
				Assert.That(result, Is.StringContaining("<span class=\"english\" lang=\"en\">(1) What words refer to the sun?"),
					"french export with all questions should have english where french is missing (in red)");
				Assert.That(result, Is.StringContaining("<p class=\"quest1\" lang=\"fr\">(1) Quels mots se"),
					"French export should have the French question (not red)");
				Assert.That(result, Is.Not.StringContaining("What words refer to everything we can see"),
					"French export should not have English alternative where French is present");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ExportTranslatedLists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExportTranslatedLists()
		{
			Assert.AreEqual(2, m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count, "The number of top-level semantic domains");
			ICmSemanticDomainRepository repoSemDom = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			Assert.AreEqual(11, repoSemDom.Count, "The total number of semantic domains");
			int wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");
			Assert.AreNotEqual(0, wsFr, "French (fr) should be defined");

			List<ICmPossibilityList> lists = new List<ICmPossibilityList>();
			lists.Add(m_cache.LangProject.SemanticDomainListOA);
			List<int> wses = new List<int>();
			wses.Add(wsFr);
			ExportDialog.TranslatedListsExporter exporter = new ExportDialog.TranslatedListsExporter(
				lists, wses, null);
			using (StringWriter w = new StringWriter())
			{
				exporter.ExportTranslatedLists(w);
				using (StringReader r = new StringReader(w.ToString()))
				{
					w.Close();
					Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", r.ReadLine());
					Assert.IsTrue(r.ReadLine().StartsWith("<Lists date=\""));
					Assert.AreEqual("<List owner=\"LangProject\" field=\"SemanticDomainList\" itemClass=\"CmSemanticDomain\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Semantic Domains</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Sem</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Possibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"63403699-07c1-43f3-a47c-069d6e4316e5\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Universe, creation</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to everything we can see?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">(1) Quels mots se réfèrent à tout ce que nous pouvons voir?</AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("<ExampleSentences>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">In the beginning God created &lt;the heavens and the earth&gt;.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</ExampleSentences>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("<SubPossibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Sky</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the sky.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words are used to refer to the sky?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">sky, firmament, canopy, vault</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to the air around the earth?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">air, atmosphere, airspace, stratosphere, ozone layer</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(3) What words are used to refer to the place or area beyond the sky?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">heaven, space, outer space, ether, void, solar system</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("<SubPossibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"dc1a2c6f-1b32-4631-8823-36dacc8cb7bb\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Sun</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1.1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the sun. The sun does three basic things. It moves, it gives light, and it gives heat. These three actions are involved in the meanings of most of the words in this domain. Since the sun moves below the horizon, many words refer to it setting or rising. Since the sun is above the clouds, many words refer to it moving behind the clouds and the clouds blocking its light. The sun's light and heat also produce secondary effects. The sun causes plants to grow, and it causes damage to things.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to the sun?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">sun, solar, sol, daystar, our star</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to how the sun moves?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">rise, set, cross the sky, come up, go down, sink</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(3) What words refer to the time when the sun rises?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">dawn, sunrise, sunup, daybreak, cockcrow, </AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("<ExampleSentences>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">We got up before &lt;dawn&gt;, in order to get an early start.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</ExampleSentences>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("<SubPossibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"1bd42665-0610-4442-8d8d-7c666fee3a6d\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Moon</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1.1.1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the moon. In your culture people may believe things about the moon. For instance in European culture people used to believe that the moon caused people to become crazy. So in English we have words like &quot;moon-struck&quot; and &quot;lunatic.&quot; You should include such words in this domain.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to the moon?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">moon, lunar, satellite</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"b044e890-ce30-455c-aede-7e9d5569396e\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Star</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1.1.2</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the stars and other heavenly bodies.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words are used to refer to the stars?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">star, starry, stellar</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words describe the sky when the stars are shining?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">starlit (sky), (sky is) ablaze with stars, starry (sky), star studded (sky), stars are shining</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("</SubPossibilities>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"e836b01b-6c1a-4d41-b90a-ea5f349f88d4\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Air</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1.2</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the air around us, including the air we breathe and the atmosphere around the earth.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to the air we breathe?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">air</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to how much water is in the air?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">humid, humidity, damp, dry, sticky, muggy</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("</SubPossibilities>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"b47d2604-8b23-41e9-9158-01526dd83894\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">World</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.2</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words referring to the planet we live on.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to the planet we live on?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">the world, earth, the Earth, the globe, the planet</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"60364974-a005-4567-82e9-7aaeff894ab0\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Water</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.3</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for general words referring to water.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What general words refer to water?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">water, H2O, moisture</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words describe something that belongs to the water or is found in water?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">watery, aquatic, amphibious</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(3) What words describe something that water cannot pass through?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">waterproof, watertight</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("</SubPossibilities>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"ba06de9e-63e1-43e6-ae94-77bea498379a\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Person</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">2</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for general words for a person or all mankind.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to a single member of the human race?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">person, human being, man, individual, figure</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to a person when you aren't sure who the person is?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">someone, somebody</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("<SubPossibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"1b0270a5-babf-4151-99f5-279ba5a4b044\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Body</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">2.1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for general words for the whole human body, and general words for any part of the body. Use a drawing or photo to label each part. Some words may be more general than others are and include some of the other words. For instance 'head' is more general than 'face' or 'nose'. Be sure that both general and specific parts are labeled.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to the body?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">body, </AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to the shape of a person's body?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
						Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">build, figure, physique, </AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(3) What general words refer to a part of the body?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">part of the body, body part, anatomy, appendage, member, orifice, </AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"7fe69c4c-2603-4949-afca-f39c010ad24e\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Body functions</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">2.2</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for the functions and actions of the whole body. Use the subdomains in this section  for functions, actions, secretions, and products of various parts of the body. In each domain include any special words that are used of animals.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What general words refer to the functions of the body?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">function</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What general words refer to secretions of the body?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">secrete, secretion, excrete, excretion, product, fluid, body fluids, discharge, flux, </AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("</SubPossibilities>", r.ReadLine());
					Assert.AreEqual("</CmSemanticDomain>", r.ReadLine());
					Assert.AreEqual("</Possibilities>", r.ReadLine());
					Assert.AreEqual("</List>", r.ReadLine());
					Assert.AreEqual("</Lists>", r.ReadLine());
					Assert.AreEqual("", r.ReadToEnd());
					r.Close();
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ExportTranslatedLists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExportTranslatedLists2()
		{
			Assert.AreEqual(2, m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count, "The number of top-level semantic domains");
			ICmSemanticDomainRepository repoSemDom = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			Assert.AreEqual(11, repoSemDom.Count, "The total number of semantic domains");
			int wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");
			Assert.AreNotEqual(0, wsFr, "French (fr) should be defined");

			List<ICmPossibilityList> lists = new List<ICmPossibilityList>();
			lists.Add(m_cache.LangProject.SemanticDomainListOA);
			List<int> wses = new List<int>();
			wses.Add(wsFr);
			ExportDialog.TranslatedListsExporter exporter = new ExportDialog.TranslatedListsExporter(
				lists, wses, null);

			using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor,
				"Undo test", "Redo test"))
			{
				m_cache.LangProject.SemanticDomainListOA.Name.set_String(wsFr, "Domaines sémantiques");
				ICmSemanticDomain sem1 = repoSemDom.GetObject(new Guid("63403699-07C1-43F3-A47C-069D6E4316E5"));
				Assert.IsNotNull(sem1);
				sem1.Name.set_String(wsFr, "L'univers physique");
				sem1.QuestionsOS[0].Question.set_String(wsFr, "Quels sont les mots qui font référence à tout ce qu'on peut voir?");
				sem1.QuestionsOS[0].ExampleWords.set_String(wsFr, "univers, ciel, terre");
				sem1.QuestionsOS[0].ExampleSentences.set_String(wsFr, "Le rôle du prophète est alors de réveiller le courage et la foi en Dieu.");
				ICmSemanticDomain sem11 = sem1.SubPossibilitiesOS[0] as ICmSemanticDomain;
				Assert.IsNotNull(sem11);
				sem11.Name.set_String(wsFr, "Ciel");
				sem11.QuestionsOS[0].Question.set_String(wsFr, "Quels sont les mots qui signifient le ciel?");
				sem11.QuestionsOS[0].ExampleWords.set_String(wsFr, "ciel, firmament");
				sem11.QuestionsOS[2].Question.set_String(wsFr, "Quels sont les mots qui signifient l'endroit ou le pays au-delà du ciel?");

				string translatedList;
				using (var w = new StringWriter())
				{
					exporter.ExportTranslatedLists(w);
					translatedList = w.ToString();
				}
				using (var r = new StringReader(translatedList))
				{
					Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", r.ReadLine());
					Assert.IsTrue(r.ReadLine().StartsWith("<Lists date=\""));
					Assert.AreEqual("<List owner=\"LangProject\" field=\"SemanticDomainList\" itemClass=\"CmSemanticDomain\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Semantic Domains</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">Domaines sémantiques</AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Sem</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Possibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"63403699-07c1-43f3-a47c-069d6e4316e5\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Universe, creation</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">L'univers physique</AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words refer to everything we can see?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">Quels sont les mots qui font référence à tout ce qu'on peut voir?</AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">univers, ciel, terre</AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("<ExampleSentences>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">In the beginning God created &lt;the heavens and the earth&gt;.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\">Le rôle du prophète est alors de réveiller le courage et la foi en Dieu.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</ExampleSentences>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("</Questions>", r.ReadLine());
					Assert.AreEqual("<SubPossibilities>", r.ReadLine());
					Assert.AreEqual("<CmSemanticDomain guid=\"999581c4-1611-4acb-ae1b-5e6c1dfe6f0c\">", r.ReadLine());
					Assert.AreEqual("<Name>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">Sky</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">Ciel</AUni>", r.ReadLine());
					Assert.AreEqual("</Name>", r.ReadLine());
					Assert.AreEqual("<Abbreviation>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">1.1</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Abbreviation>", r.ReadLine());
					Assert.AreEqual("<Description>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"en\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"en\">Use this domain for words related to the sky.</Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("<AStr ws=\"fr\">", r.ReadLine());
					Assert.AreEqual("<Run ws=\"fr\"></Run>", r.ReadLine());
					Assert.AreEqual("</AStr>", r.ReadLine());
					Assert.AreEqual("</Description>", r.ReadLine());
					Assert.AreEqual("<Questions>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(1) What words are used to refer to the sky?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">Quels sont les mots qui signifient le ciel?</AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">sky, firmament, canopy, vault</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">ciel, firmament</AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(2) What words refer to the air around the earth?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">air, atmosphere, airspace, stratosphere, ozone layer</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<CmDomainQ>", r.ReadLine());
					Assert.AreEqual("<Question>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">(3) What words are used to refer to the place or area beyond the sky?</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\">Quels sont les mots qui signifient l'endroit ou le pays au-delà du ciel?</AUni>", r.ReadLine());
					Assert.AreEqual("</Question>", r.ReadLine());
					Assert.AreEqual("<ExampleWords>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"en\">heaven, space, outer space, ether, void, solar system</AUni>", r.ReadLine());
					Assert.AreEqual("<AUni ws=\"fr\"></AUni>", r.ReadLine());
					Assert.AreEqual("</ExampleWords>", r.ReadLine());
					Assert.AreEqual("</CmDomainQ>", r.ReadLine());
					r.Close();
				}
			}
		}
	}
}
