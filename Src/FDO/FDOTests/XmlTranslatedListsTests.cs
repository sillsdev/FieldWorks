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
// File: XmlTranslatedListsTests.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class XmlTranslatedListsTests : BaseTest
	{
		private FdoCache m_cache;
		private int m_wsEn;
		private int m_wsEs;
		private int m_wsFr;
		private int m_wsDe;
		private IPartOfSpeechRepository m_repoPOS;
		private ICmSemanticDomainRepository m_repoSemDom;

		private static readonly string s_ksTranslationsXml =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?> " + Environment.NewLine +
			"<Lists date=\"2010-09-29 16:38:23.460\"> " + Environment.NewLine +
			"  <List owner=\"LangProject\" field=\"PartsOfSpeech\" itemClass=\"PartOfSpeech\">" + Environment.NewLine +
			"    <Name>" + Environment.NewLine +
			"      <AUni ws=\"en\">Parts Of Speech</AUni>" + Environment.NewLine +
			"      <AUni ws=\"de\">Parts-k Of-k Speech-k</AUni>" + Environment.NewLine +
			"    </Name>" + Environment.NewLine +
			"    <Abbreviation>" + Environment.NewLine +
			"      <AUni ws=\"en\">Pos</AUni>" + Environment.NewLine +
			"      <AUni ws=\"de\">Pos-k</AUni>" + Environment.NewLine +
			"    </Abbreviation>" + Environment.NewLine +
			"    <Possibilities>" + Environment.NewLine +
			"      <PartOfSpeech>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Adverb</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Adverb-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">adv</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">adv-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">An adverb, narrowly defined, is a part of speech whose members modify verbs for such categories as time, manner, place, or direction. An adverb, broadly defined, is a part of speech whose members modify any constituent class of words other than nouns, such as verbs, adjectives, adverbs, phrases, clauses, or sentences. Under this definition, the possible type of modification depends on the class of the constituent being modified.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">An adverb-k, narrowly-k defined-k, is-k a-k part-k of-k speech-k whose-k members-k modify-k verbs-k for-k such-k categories-k as-k time-k, manner-k, place-k, or-k direction-k. An-k adverb-k, broadly-k defined-k, is-k a-k part-k of-k speech-k whose-k members-k modify-k any-k constituent-k class-k of-k words-k other-k than-k nouns-k, such-k as-k verbs-k, adjectives-k, adverbs-k, phrases-k, clauses-k, or-k sentences-k. Under-k this-k definition-k, the-k possible-k type-k of-k modification-k depends-k on-k the-k class-k of-k the-k constituent-k being-k modified-k.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"      </PartOfSpeech>" + Environment.NewLine +
			"      <PartOfSpeech>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Noun</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Noun-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">n</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">n-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">A noun is a broad classification of parts of speech which include substantives and nominals.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">A noun-k is-k a-k broad-k classification-k of-k parts-k of-k speech-k which-k include-k substantives-k and-k nominals-k.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"      </PartOfSpeech>" + Environment.NewLine +
			"      <PartOfSpeech>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Pro-form</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Pro-form-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">pro-form</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">pro-form-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">A pro-form is a part of speech whose members usually substitute for other constituents, including phrases, clauses, or sentences, and whose meaning is recoverable from the linguistic or extralinguistic context.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">A pro-form-k is-k a-k part-k of-k speech-k whose-k members-k usually-k substitute-k for-k other-k constituents-k, including-k phrases-k, clauses-k, or-k sentences-k, and-k whose-k meaning-k is-k recoverable-k from-k the-k linguistic-k or-k extralinguistic-k context-k.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"        <SubPossibilities>" + Environment.NewLine +
			"          <PartOfSpeech>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">Pronoun</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">Pronoun-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">pro</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">pro-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">A pronoun is a pro-form which functions like a noun and substitutes for a noun or noun phrase.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">A pronoun-k is-k a-k pro-form-k which-k functions-k like-k a-k noun-k and-k substitutes-k for-k a-k noun-k or-k noun-k phrase-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"          </PartOfSpeech>" + Environment.NewLine +
			"        </SubPossibilities>" + Environment.NewLine +
			"      </PartOfSpeech>" + Environment.NewLine +
			"      <PartOfSpeech>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Verb</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Verb-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">v</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">v-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">A verb is a part of speech whose members typically signal events and actions; constitute, singly or in a phrase, a minimal predicate in a clause; govern the number and types of other constituents which may occur in the clause; and, in inflectional languages, may be inflected for tense, aspect, voice, modality, or agreement with other constituents in person, number, or grammatical gender.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">A verb-k is-k a-k part-k of-k speech-k whose-k members-k typically-k signal-k events-k and-k actions-k; constitute-k, singly-k or-k in-k a-k phrase-k, a-k minimal-k predicate-k in-k a-k clause-k; govern-k the-k number-k and-k types-k of-k other-k constituents-k which-k may-k occur-k in-k the-k clause-k; and-k, in-k inflectional-k languages-k, may-k be-k inflected-k for-k tense-k, aspect-k, voice-k, modality-k, or-k agreement-k with-k other-k constituents-k in-k person-k, number-k, or-k grammatical-k gender-k.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"      </PartOfSpeech>" + Environment.NewLine +
			"    </Possibilities>" + Environment.NewLine +
			"  </List>" + Environment.NewLine +
			"  <List owner=\"LangProject\" field=\"SemanticDomainList\" itemClass=\"CmSemanticDomain\">" + Environment.NewLine +
			"    <Name>" + Environment.NewLine +
			"      <AUni ws=\"en\">Semantic Domains</AUni>" + Environment.NewLine +
			"      <AUni ws=\"de\">Semantic-k Domains-k</AUni>" + Environment.NewLine +
			"    </Name>" + Environment.NewLine +
			"    <Abbreviation>" + Environment.NewLine +
			"      <AUni ws=\"en\">Sem</AUni>" + Environment.NewLine +
			"      <AUni ws=\"de\">Sem-k</AUni>" + Environment.NewLine +
			"    </Abbreviation>" + Environment.NewLine +
			"    <Possibilities>" + Environment.NewLine +
			"      <CmSemanticDomain guid=\"63403699-07C1-43F3-A47C-069D6E4316E5\">" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">1</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">1-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Universe, creation</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Universe-k, creation-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">Use-k this-k domain-k for-k general-k words-k referring-k to-k the-k physical-k universe-k. Some-k languages-k may-k not-k have-k a-k single-k word-k for-k the-k universe-k and-k may-k have-k to-k use-k a-k phrase-k such-k as-k 'rain-k, soil-k, and-k things-k of-k the-k sky'-k or-k 'sky-k, land-k, and-k water'-k or-k a-k descriptive-k phrase-k such-k as-k 'everything-k you-k can-k see-k' or-k 'everything-k that-k exists-k'.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"        <Questions>" + Environment.NewLine +
			"          <CmDomainQ>" + Environment.NewLine +
			"            <Question>" + Environment.NewLine +
			"              <AUni ws=\"en\">(1) What words refer to everything we can see?</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k everything-k we-k can-k see-k?</AUni>" + Environment.NewLine +
			"            </Question>" + Environment.NewLine +
			"            <ExampleWords>" + Environment.NewLine +
			"              <AUni ws=\"en\">universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">universe-k, creation-k, cosmos-k, heaven-k and-k earth-k, macrocosm-k, everything-k that-k exists-k</AUni>" + Environment.NewLine +
			"            </ExampleWords>" + Environment.NewLine +
			"            <ExampleSentences>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">In the beginning God created &lt;the heavens and the earth&gt;.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">In-k the-k beginning-k God-k created-k &lt;the-k heavens-k and-k the-k earth-k&gt;.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </ExampleSentences>" + Environment.NewLine +
			"          </CmDomainQ>" + Environment.NewLine +
			"        </Questions>" + Environment.NewLine +
			"        <SubPossibilities>" + Environment.NewLine +
			"          <CmSemanticDomain guid=\"999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C\">" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">1.1</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">1.1-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">Sky</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">Sky-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">Use this domain for words related to the sky.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">Use-k this-k domain-k for-k words-k related-k to-k the-k sky-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"            <Questions>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(1) What words are used to refer to the sky?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(1)-k What-k words-k are-k used-k to-k refer-k to-k the-k sky-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">sky, firmament, canopy, vault</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">sky-k, firmament-k, canopy-k, vault-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(2) What words refer to the air around the earth?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(2)-k What-k words-k refer-k to-k the-k air-k around-k the-k earth-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">air, atmosphere, airspace, stratosphere, ozone layer</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">air-k, atmosphere-k, airspace-k, stratosphere-k, ozone-k layer-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(3) What words are used to refer to the place or area beyond the sky?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(3)-k What-k words-k are-k used-k to-k refer-k to-k the-k place-k or-k area-k beyond-k the-k sky-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">heaven, space, outer space, ether, void, solar system</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">heaven-k, space-k, outer-k space-k, ether-k, void-k, solar-k system-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"            </Questions>" + Environment.NewLine +
			"            <SubPossibilities>" + Environment.NewLine +
			"              <CmSemanticDomain guid=\"DC1A2C6F-1B32-4631-8823-36DACC8CB7BB\">" + Environment.NewLine +
			"                <Abbreviation>" + Environment.NewLine +
			"                  <AUni ws=\"en\">1.1.1</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">1.1.1-k</AUni>" + Environment.NewLine +
			"                </Abbreviation>" + Environment.NewLine +
			"                <Name>" + Environment.NewLine +
			"                  <AUni ws=\"en\">Sun</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">Sun-k</AUni>" + Environment.NewLine +
			"                </Name>" + Environment.NewLine +
			"                <Description>" + Environment.NewLine +
			"                  <AStr ws=\"en\">" + Environment.NewLine +
			"                    <Run ws=\"en\">Use this domain for words related to the sun. The sun does three basic things. It moves, it gives light, and it gives heat. These three actions are involved in the meanings of most of the words in this domain. Since the sun moves below the horizon, many words refer to it setting or rising. Since the sun is above the clouds, many words refer to it moving behind the clouds and the clouds blocking its light. The sun's light and heat also produce secondary effects. The sun causes plants to grow, and it causes damage to things.</Run>" + Environment.NewLine +
			"                  </AStr>" + Environment.NewLine +
			"                  <AStr ws=\"de\">" + Environment.NewLine +
			"                    <Run ws=\"de\">Use-k this-k domain-k for-k words-k related-k to-k the-k sun.-k The-k sun-k does-k three-k basic-k things.-k It-k moves-k, it-k gives-k light-k, and-k it-k gives-k heat.-k These-k three-k actions-k are-k involved-k in-k the-k meanings-k of-k most-k of-k the-k words-k in-k this-k domain.-k Since-k the-k sun-k moves-k below-k the-k horizon-k, many-k words-k refer-k to-k it-k setting-k or-k rising.-k Since-k the-k sun-k is-k above-k the-k clouds-k, many-k words-k refer-k to-k it-k moving-k behind-k the-k clouds-k and-k the-k clouds-k blocking-k its-k light.-k The-k sun's-k light-k and-k heat-k also-k produce-k secondary-k effects.-k The-k sun-k causes-k plants-k to-k grow-k, and-k it-k causes-k damage-k to-k things-k.</Run>" + Environment.NewLine +
			"                  </AStr>" + Environment.NewLine +
			"                </Description>" + Environment.NewLine +
			"                <Questions>" + Environment.NewLine +
			"                  <CmDomainQ>" + Environment.NewLine +
			"                    <Question>" + Environment.NewLine +
			"                      <AUni ws=\"en\">(1) What words refer to the sun?</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k the-k sun-k?</AUni>" + Environment.NewLine +
			"                    </Question>" + Environment.NewLine +
			"                    <ExampleWords>" + Environment.NewLine +
			"                      <AUni ws=\"en\">sun, solar, sol, daystar, our star</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">sun-k, solar-k, sol-k, daystar-k, our-k star-k</AUni>" + Environment.NewLine +
			"                    </ExampleWords>" + Environment.NewLine +
			"                  </CmDomainQ>" + Environment.NewLine +
			"                  <CmDomainQ>" + Environment.NewLine +
			"                    <Question>" + Environment.NewLine +
			"                      <AUni ws=\"en\">(2) What words refer to how the sun moves?</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">(2)-k What-k words-k refer-k to-k how-k the-k sun-k moves-k?</AUni>" + Environment.NewLine +
			"                    </Question>" + Environment.NewLine +
			"                    <ExampleWords>" + Environment.NewLine +
			"                      <AUni ws=\"en\">rise, set, cross the sky, come up, go down, sink</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">rise-k, set-k, cross-k the-k sky-k, come-k up-k, go-k down-k, sink-k</AUni>" + Environment.NewLine +
			"                    </ExampleWords>" + Environment.NewLine +
			"                  </CmDomainQ>" + Environment.NewLine +
			"                  <CmDomainQ>" + Environment.NewLine +
			"                    <Question>" + Environment.NewLine +
			"                      <AUni ws=\"en\">(3) What words refer to the time when the sun rises?</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">(3)-k What-k words-k refer-k to-k the-k time-k when-k the-k sun-k rises-k?</AUni>" + Environment.NewLine +
			"                    </Question>" + Environment.NewLine +
			"                    <ExampleWords>" + Environment.NewLine +
			"                      <AUni ws=\"en\">dawn, sunrise, sunup, daybreak, cockcrow, </AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">dawn-k, sunrise-k, sunup-k, daybreak-k, cockcrow-k-k, </AUni>" + Environment.NewLine +
			"                    </ExampleWords>" + Environment.NewLine +
			"                    <ExampleSentences>" + Environment.NewLine +
			"                      <AStr ws=\"en\">" + Environment.NewLine +
			"                        <Run ws=\"en\">We got up before &lt;dawn&gt;, in order to get an early start.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                      <AStr ws=\"de\">" + Environment.NewLine +
			"                        <Run ws=\"de\">We-k got-k up-k before-k &lt;dawn-k&gt;, in-k order-k to-k get-k an-k early-k start-k.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                    </ExampleSentences>" + Environment.NewLine +
			"                  </CmDomainQ>" + Environment.NewLine +
			"                </Questions>" + Environment.NewLine +
			"                <SubPossibilities>" + Environment.NewLine +
			"                  <CmSemanticDomain guid=\"1BD42665-0610-4442-8D8D-7C666FEE3A6D\">" + Environment.NewLine +
			"                    <Abbreviation>" + Environment.NewLine +
			"                      <AUni ws=\"en\">1.1.1.1</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">1.1.1.1-k</AUni>" + Environment.NewLine +
			"                    </Abbreviation>" + Environment.NewLine +
			"                    <Name>" + Environment.NewLine +
			"                      <AUni ws=\"en\">Moon</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">Moon-k</AUni>" + Environment.NewLine +
			"                    </Name>" + Environment.NewLine +
			"                    <Description>" + Environment.NewLine +
			"                      <AStr ws=\"en\">" + Environment.NewLine +
			"                        <Run ws=\"en\">Use this domain for words related to the moon. In your culture people may believe things about the moon. For instance in European culture people used to believe that the moon caused people to become crazy. So in English we have words like \"moon-struck\" and \"lunatic.\" You should include such words in this domain.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                      <AStr ws=\"de\">" + Environment.NewLine +
			"                        <Run ws=\"de\">Use-k this-k domain-k for-k words-k related-k to-k the-k moon.-k In-k your-k culture-k people-k may-k believe-k things-k about-k the-k moon.-k For-k instance-k in-k European-k culture-k people-k used-k to-k believe-k that-k the-k moon-k caused-k people-k to-k become-k crazy.-k So-k in-k English-k we-k have-k words-k like-k \"moon-struck\"-k and-k \"lunatic.\"-k You-k should-k include-k such-k words-k in-k this-k domain-k.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                    </Description>" + Environment.NewLine +
			"                    <Questions>" + Environment.NewLine +
			"                      <CmDomainQ>" + Environment.NewLine +
			"                        <Question>" + Environment.NewLine +
			"                          <AUni ws=\"en\">(1) What words refer to the moon?</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k the-k moon-k?</AUni>" + Environment.NewLine +
			"                        </Question>" + Environment.NewLine +
			"                        <ExampleWords>" + Environment.NewLine +
			"                          <AUni ws=\"en\">moon, lunar, satellite</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">moon-k, lunar-k, satellite-k</AUni>" + Environment.NewLine +
			"                        </ExampleWords>" + Environment.NewLine +
			"                      </CmDomainQ>" + Environment.NewLine +
			"                    </Questions>" + Environment.NewLine +
			"                  </CmSemanticDomain>" + Environment.NewLine +
			"                  <CmSemanticDomain guid=\"B044E890-CE30-455C-AEDE-7E9D5569396E\">" + Environment.NewLine +
			"                    <Abbreviation>" + Environment.NewLine +
			"                      <AUni ws=\"en\">1.1.1.2</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">1.1.1.2-k</AUni>" + Environment.NewLine +
			"                    </Abbreviation>" + Environment.NewLine +
			"                    <Name>" + Environment.NewLine +
			"                      <AUni ws=\"en\">Star</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">Star-k</AUni>" + Environment.NewLine +
			"                    </Name>" + Environment.NewLine +
			"                    <Description>" + Environment.NewLine +
			"                      <AStr ws=\"en\">" + Environment.NewLine +
			"                        <Run ws=\"en\">Use this domain for words related to the stars and other heavenly bodies.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                      <AStr ws=\"de\">" + Environment.NewLine +
			"                        <Run ws=\"de\">Use-k this-k domain-k for-k words-k related-k to-k the-k stars-k and-k other-k heavenly-k bodies-k.</Run>" + Environment.NewLine +
			"                      </AStr>" + Environment.NewLine +
			"                    </Description>" + Environment.NewLine +
			"                    <Questions>" + Environment.NewLine +
			"                      <CmDomainQ>" + Environment.NewLine +
			"                        <Question>" + Environment.NewLine +
			"                          <AUni ws=\"en\">(1) What words are used to refer to the stars?</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">(1)-k What-k words-k are-k used-k to-k refer-k to-k the-k stars-k?</AUni>" + Environment.NewLine +
			"                        </Question>" + Environment.NewLine +
			"                        <ExampleWords>" + Environment.NewLine +
			"                          <AUni ws=\"en\">star, starry, stellar</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">star-k, starry-k, stellar-k</AUni>" + Environment.NewLine +
			"                        </ExampleWords>" + Environment.NewLine +
			"                      </CmDomainQ>" + Environment.NewLine +
			"                      <CmDomainQ>" + Environment.NewLine +
			"                        <Question>" + Environment.NewLine +
			"                          <AUni ws=\"en\">(2) What words describe the sky when the stars are shining?</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">(2)-k What-k words-k describe-k the-k sky-k when-k the-k stars-k are-k shining-k?</AUni>" + Environment.NewLine +
			"                        </Question>" + Environment.NewLine +
			"                        <ExampleWords>" + Environment.NewLine +
			"                          <AUni ws=\"en\">starlit (sky), (sky is) ablaze with stars, starry (sky), star studded (sky), stars are shining</AUni>" + Environment.NewLine +
			"                          <AUni ws=\"de\">starlit-k (sky)-k, (sky-k is)-k ablaze-k with-k stars-k, starry-k (sky)-k, star-k studded-k (sky)-k, stars-k are-k shining-k</AUni>" + Environment.NewLine +
			"                        </ExampleWords>" + Environment.NewLine +
			"                      </CmDomainQ>" + Environment.NewLine +
			"                    </Questions>" + Environment.NewLine +
			"                  </CmSemanticDomain>" + Environment.NewLine +
			"                </SubPossibilities>" + Environment.NewLine +
			"              </CmSemanticDomain>" + Environment.NewLine +
			"              <CmSemanticDomain guid=\"E836B01B-6C1A-4D41-B90A-EA5F349F88D4\">" + Environment.NewLine +
			"                <Abbreviation>" + Environment.NewLine +
			"                  <AUni ws=\"en\">1.1.2</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">1.1.2-k</AUni>" + Environment.NewLine +
			"                </Abbreviation>" + Environment.NewLine +
			"                <Name>" + Environment.NewLine +
			"                  <AUni ws=\"en\">Air</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">Air-k</AUni>" + Environment.NewLine +
			"                </Name>" + Environment.NewLine +
			"                <Description>" + Environment.NewLine +
			"                  <AStr ws=\"en\">" + Environment.NewLine +
			"                    <Run ws=\"en\">Use this domain for words related to the air around us, including the air we breathe and the atmosphere around the earth.</Run>" + Environment.NewLine +
			"                  </AStr>" + Environment.NewLine +
			"                  <AStr ws=\"de\">" + Environment.NewLine +
			"                    <Run ws=\"de\">Use-k this-k domain-k for-k words-k related-k to-k the-k air-k around-k us-k, including-k the-k air-k we-k breathe-k and-k the-k atmosphere-k around-k the-k earth-k.</Run>" + Environment.NewLine +
			"                  </AStr>" + Environment.NewLine +
			"                </Description>" + Environment.NewLine +
			"                <Questions>" + Environment.NewLine +
			"                  <CmDomainQ>" + Environment.NewLine +
			"                    <Question>" + Environment.NewLine +
			"                      <AUni ws=\"en\">(1) What words refer to the air we breathe?</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k the-k air-k we-k breathe-k?</AUni>" + Environment.NewLine +
			"                    </Question>" + Environment.NewLine +
			"                    <ExampleWords>" + Environment.NewLine +
			"                      <AUni ws=\"en\">air</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">air-k</AUni>" + Environment.NewLine +
			"                    </ExampleWords>" + Environment.NewLine +
			"                  </CmDomainQ>" + Environment.NewLine +
			"                  <CmDomainQ>" + Environment.NewLine +
			"                    <Question>" + Environment.NewLine +
			"                      <AUni ws=\"en\">(2) What words refer to how much water is in the air?</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">(2)-k What-k words-k refer-k to-k how-k much-k water-k is-k in-k the-k air-k?</AUni>" + Environment.NewLine +
			"                    </Question>" + Environment.NewLine +
			"                    <ExampleWords>" + Environment.NewLine +
			"                      <AUni ws=\"en\">humid, humidity, damp, dry, sticky, muggy</AUni>" + Environment.NewLine +
			"                      <AUni ws=\"de\">humid-k, humidity-k, damp-k, dry-k, sticky-k, muggy-k</AUni>" + Environment.NewLine +
			"                    </ExampleWords>" + Environment.NewLine +
			"                  </CmDomainQ>" + Environment.NewLine +
			"                </Questions>" + Environment.NewLine +
			"              </CmSemanticDomain>" + Environment.NewLine +
			"            </SubPossibilities>" + Environment.NewLine +
			"          </CmSemanticDomain>" + Environment.NewLine +
			"          <CmSemanticDomain guid=\"B47D2604-8B23-41E9-9158-01526DD83894\">" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">1.2</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">1.2-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">World</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">World-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">Use this domain for words referring to the planet we live on.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">Use-k this-k domain-k for-k words-k referring-k to-k the-k planet-k we-k live-k on-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"            <Questions>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(1) What words refer to the planet we live on?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k the-k planet-k we-k live-k on-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">the world, earth, the Earth, the globe, the planet</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">the-k world-k, earth-k, the-k Earth-k, the-k globe-k, the-k planet-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"            </Questions>" + Environment.NewLine +
			"          </CmSemanticDomain>" + Environment.NewLine +
			"          <CmSemanticDomain guid=\"60364974-A005-4567-82E9-7AAEFF894AB0\">" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">1.3</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">1.3-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">Water</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">Water-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">Use this domain for general words referring to water.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">Use-k this-k domain-k for-k general-k words-k referring-k to-k water-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"            <Questions>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(1) What general words refer to water?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(1)-k What-k general-k words-k refer-k to-k water-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">water, H2O, moisture</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">water-k, H2O-k, moisture-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(2) What words describe something that belongs to the water or is found in water?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(2)-k What-k words-k describe-k something-k that-k belongs-k to-k the-k water-k or-k is-k found-k in-k water-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">watery, aquatic, amphibious</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">watery-k, aquatic-k, amphibious-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(3) What words describe something that water cannot pass through?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(3)-k What-k words-k describe-k something-k that-k water-k cannot-k pass-k through-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">waterproof, watertight</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">waterproof-k, watertight-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"            </Questions>" + Environment.NewLine +
			"          </CmSemanticDomain>" + Environment.NewLine +
			"        </SubPossibilities>" + Environment.NewLine +
			"      </CmSemanticDomain>" + Environment.NewLine +
			"      <CmSemanticDomain guid=\"BA06DE9E-63E1-43E6-AE94-77BEA498379A\">" + Environment.NewLine +
			"        <Abbreviation>" + Environment.NewLine +
			"          <AUni ws=\"en\">2</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">2-k</AUni>" + Environment.NewLine +
			"        </Abbreviation>" + Environment.NewLine +
			"        <Name>" + Environment.NewLine +
			"          <AUni ws=\"en\">Person</AUni>" + Environment.NewLine +
			"          <AUni ws=\"de\">Person-k</AUni>" + Environment.NewLine +
			"        </Name>" + Environment.NewLine +
			"        <Description>" + Environment.NewLine +
			"          <AStr ws=\"en\">" + Environment.NewLine +
			"            <Run ws=\"en\">Use this domain for general words for a person or all mankind.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"          <AStr ws=\"de\">" + Environment.NewLine +
			"            <Run ws=\"de\">Use-k this-k domain-k for-k general-k words-k for-k a-k person-k or-k all-k mankind-k.</Run>" + Environment.NewLine +
			"          </AStr>" + Environment.NewLine +
			"        </Description>" + Environment.NewLine +
			"        <Questions>" + Environment.NewLine +
			"          <CmDomainQ>" + Environment.NewLine +
			"            <Question>" + Environment.NewLine +
			"              <AUni ws=\"en\">(1) What words refer to a single member of the human race?</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k a-k single-k member-k of-k the-k human-k race-k?</AUni>" + Environment.NewLine +
			"            </Question>" + Environment.NewLine +
			"            <ExampleWords>" + Environment.NewLine +
			"              <AUni ws=\"en\">person, human being, man, individual, figure</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">person-k, human-k being-k, man-k, individual-k, figure-k</AUni>" + Environment.NewLine +
			"            </ExampleWords>" + Environment.NewLine +
			"          </CmDomainQ>" + Environment.NewLine +
			"          <CmDomainQ>" + Environment.NewLine +
			"            <Question>" + Environment.NewLine +
			"              <AUni ws=\"en\">(2) What words refer to a person when you aren't sure who the person is?</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">(2)-k What-k words-k refer-k to-k a-k person-k when-k you-k aren't-k sure-k who-k the-k person-k is-k?</AUni>" + Environment.NewLine +
			"            </Question>" + Environment.NewLine +
			"            <ExampleWords>" + Environment.NewLine +
			"              <AUni ws=\"en\">someone, somebody</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">someone-k, somebody-k</AUni>" + Environment.NewLine +
			"            </ExampleWords>" + Environment.NewLine +
			"          </CmDomainQ>" + Environment.NewLine +
			"        </Questions>" + Environment.NewLine +
			"        <SubPossibilities>" + Environment.NewLine +
			"          <CmSemanticDomain guid=\"1B0270A5-BABF-4151-99F5-279BA5A4B044\">" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">2.1</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">2.1-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">Body</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">Body-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">Use this domain for general words for the whole human body, and general words for any part of the body. Use a drawing or photo to label each part. Some words may be more general than others are and include some of the other words. For instance 'head' is more general than 'face' or 'nose'. Be sure that both general and specific parts are labeled.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">Use-k this-k domain-k for-k general-k words-k for-k the-k whole-k human-k body-k, and-k general-k words-k for-k any-k part-k of-k the-k body.-k Use-k a-k drawing-k or-k photo-k to-k label-k each-k part.-k Some-k words-k may-k be-k more-k general-k than-k others-k are-k and-k include-k some-k of-k the-k other-k words.-k For-k instance-k 'head'-k is-k more-k general-k than-k 'face'-k or-k 'nose'.-k Be-k sure-k that-k both-k general-k and-k specific-k parts-k are-k labeled-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"            <Questions>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(1) What words refer to the body?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(1)-k What-k words-k refer-k to-k the-k body-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">body, </AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">body-k-k, </AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(2) What words refer to the shape of a person's body?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(2)-k What-k words-k refer-k to-k the-k shape-k of-k a-k person's-k body-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">build, figure, physique, </AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">build-k, figure-k, physique-k-k, </AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(3) What general words refer to a part of the body?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(3)-k What-k general-k words-k refer-k to-k a-k part-k of-k the-k body-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">part of the body, body part, anatomy, appendage, member, orifice, </AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">part-k of-k the-k body-k, body-k part-k, anatomy-k, appendage-k, member-k, orifice-k-k, </AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"            </Questions>" + Environment.NewLine +
			"          </CmSemanticDomain>" + Environment.NewLine +
			"          <CmSemanticDomain guid=\"7FE69C4C-2603-4949-AFCA-F39C010AD24E\">" + Environment.NewLine +
			"            <Abbreviation>" + Environment.NewLine +
			"              <AUni ws=\"en\">2.2</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">2.2-k</AUni>" + Environment.NewLine +
			"            </Abbreviation>" + Environment.NewLine +
			"            <Name>" + Environment.NewLine +
			"              <AUni ws=\"en\">Body functions</AUni>" + Environment.NewLine +
			"              <AUni ws=\"de\">Body-k functions-k</AUni>" + Environment.NewLine +
			"            </Name>" + Environment.NewLine +
			"            <Description>" + Environment.NewLine +
			"              <AStr ws=\"en\">" + Environment.NewLine +
			"                <Run ws=\"en\">Use this domain for the functions and actions of the whole body. Use the subdomains in this section  for functions, actions, secretions, and products of various parts of the body. In each domain include any special words that are used of animals.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"              <AStr ws=\"de\">" + Environment.NewLine +
			"                <Run ws=\"de\">Use-k this-k domain-k for-k the-k functions-k and-k actions-k of-k the-k whole-k body.-k Use-k the-k subdomains-k in-k this-k section-k -k for-k functions-k, actions-k, secretions-k, and-k products-k of-k various-k parts-k of-k the-k body.-k In-k each-k domain-k include-k any-k special-k words-k that-k are-k used-k of-k animals-k.</Run>" + Environment.NewLine +
			"              </AStr>" + Environment.NewLine +
			"            </Description>" + Environment.NewLine +
			"            <Questions>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(1) What general words refer to the functions of the body?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(1)-k What-k general-k words-k refer-k to-k the-k functions-k of-k the-k body-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">function</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">function-k</AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"              <CmDomainQ>" + Environment.NewLine +
			"                <Question>" + Environment.NewLine +
			"                  <AUni ws=\"en\">(2) What general words refer to secretions of the body?</AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">(2)-k What-k general-k words-k refer-k to-k secretions-k of-k the-k body-k?</AUni>" + Environment.NewLine +
			"                </Question>" + Environment.NewLine +
			"                <ExampleWords>" + Environment.NewLine +
			"                  <AUni ws=\"en\">secrete, secretion, excrete, excretion, product, fluid, body fluids, discharge, flux, </AUni>" + Environment.NewLine +
			"                  <AUni ws=\"de\">secrete-k, secretion-k, excrete-k, excretion-k, product-k, fluid-k, body-k fluids-k, discharge-k, flux-k-k, </AUni>" + Environment.NewLine +
			"                </ExampleWords>" + Environment.NewLine +
			"              </CmDomainQ>" + Environment.NewLine +
			"            </Questions>" + Environment.NewLine +
			"          </CmSemanticDomain>" + Environment.NewLine +
			"        </SubPossibilities>" + Environment.NewLine +
			"      </CmSemanticDomain>" + Environment.NewLine +
			"    </Possibilities>" + Environment.NewLine +
			"  </List>" + Environment.NewLine +
			"</Lists>" + Environment.NewLine;

		/// <summary>
		/// Setup method: create a memory-only mock cache and empty language project.
		/// </summary>
		[SetUp]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ThreadHelper is disposed in DestroyMockCache()")]
		public void CreateMockCache()
		{
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", "es", "en", new ThreadHelper());

			var xl = new XmlList();
			using (var reader = new StringReader(XmlListTests.s_ksPartsOfSpeechXml))
			{
				xl.ImportList(m_cache.LangProject, "PartsOfSpeech", reader, null);
				reader.Close();
			}
			using (var reader = new StringReader(XmlListTests.s_ksSemanticDomainsXml))
			{
				xl.ImportList(m_cache.LangProject, "SemanticDomainList", reader, null);
				reader.Close();
			}

			m_repoPOS = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>();
			m_repoSemDom = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();

			m_wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsEs = m_cache.WritingSystemFactory.GetWsFromStr("es");
			m_wsFr = m_cache.WritingSystemFactory.GetWsFromStr("fr");
			m_wsDe = m_cache.WritingSystemFactory.GetWsFromStr("de");
		}

		/// <summary>
		/// Teardown method: destroy the memory-only mock cache.
		/// </summary>
		[TearDown]
		public void DestroyMockCache()
		{
			m_cache.ThreadHelper.Dispose();
			m_cache.Dispose();
			m_cache = null;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ImportTranslatedLists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ImportTranslatedLists()
		{
			var listPOS = m_cache.LangProject.PartsOfSpeechOA;
			Assert.AreEqual(2, listPOS.PossibilitiesOS.Count);
			Assert.AreEqual(127, listPOS.Depth);
			Assert.AreEqual(-3, listPOS.WsSelector);
			Assert.IsTrue(listPOS.IsSorted);
			Assert.IsTrue(listPOS.UseExtendedFields);
			Assert.AreEqual(5049, listPOS.ItemClsid);
			Assert.AreEqual(1, listPOS.Abbreviation.StringCount);
			Assert.AreEqual("Pos", listPOS.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual(3, listPOS.Name.StringCount);
			Assert.AreEqual("Parts Of Speech", listPOS.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Categori\u0301as Grama\u0301ticas", listPOS.Name.get_String(m_wsEs).Text);
			Assert.AreEqual("Parties du Discours", listPOS.Name.get_String(m_wsFr).Text);

			List<IPartOfSpeech> allPartsOfSpeech = new List<IPartOfSpeech>();
			allPartsOfSpeech.AddRange(m_repoPOS.AllInstances());
			Assert.AreEqual(5, allPartsOfSpeech.Count);

			var listSemDom = m_cache.LangProject.SemanticDomainListOA;
			Assert.AreEqual(2, listSemDom.PossibilitiesOS.Count);
			Assert.AreEqual(127, listSemDom.Depth);
			Assert.AreEqual(-3, listSemDom.WsSelector);
			Assert.IsTrue(listSemDom.IsSorted);
			Assert.IsFalse(listSemDom.UseExtendedFields);
			Assert.AreEqual(66, listSemDom.ItemClsid);
			Assert.AreEqual(1, listSemDom.Abbreviation.StringCount);
			Assert.AreEqual("Sem", listSemDom.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual(1, listSemDom.Name.StringCount);
			Assert.AreEqual("Semantic Domains", listSemDom.Name.get_String(m_wsEn).Text);

			List<ICmSemanticDomain> allSemanticDomains = new List<ICmSemanticDomain>();
			allSemanticDomains.AddRange(m_repoSemDom.AllInstances());
			Assert.AreEqual(11, allSemanticDomains.Count);

			var xtl = new XmlTranslatedLists();
			var reader = new StringReader(s_ksTranslationsXml);
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
				() => xtl.ImportTranslatedLists(reader, m_cache, null));

			CheckPartsOfSpeech(listPOS, allPartsOfSpeech);
			CheckSemanticDomains(listSemDom, allSemanticDomains);
		}

		private void CheckPartsOfSpeech(ICmPossibilityList listPOS, List<IPartOfSpeech> allPartsOfSpeech)
		{
			Assert.AreEqual(2, listPOS.PossibilitiesOS.Count);
			Assert.AreEqual(2, listPOS.Abbreviation.StringCount);
			Assert.AreEqual("Pos", listPOS.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("Pos-k", listPOS.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(4, listPOS.Name.StringCount);
			Assert.AreEqual("Parts Of Speech", listPOS.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Categori\u0301as Grama\u0301ticas", listPOS.Name.get_String(m_wsEs).Text);
			Assert.AreEqual("Parties du Discours", listPOS.Name.get_String(m_wsFr).Text);
			Assert.AreEqual("Parts-k Of-k Speech-k", listPOS.Name.get_String(m_wsDe).Text);

			// verify that adding translations hasn't added (or removed!) any new items
			List<IPartOfSpeech> newPartsOfSpeech = new List<IPartOfSpeech>();
			newPartsOfSpeech.AddRange(m_repoPOS.AllInstances());
			Assert.AreEqual(5, newPartsOfSpeech.Count);
			foreach (var pos in newPartsOfSpeech)
				Assert.IsTrue(allPartsOfSpeech.Contains(pos));

			var adverb = listPOS.PossibilitiesOS[0] as IPartOfSpeech;
			Assert.IsNotNull(adverb);
			Assert.AreEqual(6303632, adverb.ForeColor);
			Assert.AreEqual(-1073741824, adverb.BackColor);
			Assert.AreEqual(-1073741824, adverb.UnderColor);
			Assert.AreEqual(1, adverb.UnderStyle);
			Assert.AreEqual(4, adverb.Abbreviation.StringCount);
			Assert.AreEqual("adv", adverb.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("adv", adverb.Abbreviation.get_String(m_wsEs).Text);
			Assert.AreEqual("adv", adverb.Abbreviation.get_String(m_wsFr).Text);
			Assert.AreEqual("adv-k", adverb.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(4, adverb.Name.StringCount);
			Assert.AreEqual("Adverb", adverb.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Adverbio", adverb.Name.get_String(m_wsEs).Text);
			Assert.AreEqual("Adverbe", adverb.Name.get_String(m_wsFr).Text);
			Assert.AreEqual("Adverb-k", adverb.Name.get_String(m_wsDe).Text);
			Assert.AreEqual(2, adverb.Description.StringCount);
			var desc = adverb.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("An adverb, narrowly defined, is a part of"));
			Assert.IsTrue(desc.EndsWith(" the class of the constituent being modified."));
			Assert.AreEqual(432, desc.Length);
			desc = adverb.Description.get_String(m_wsDe).Text;
			Assert.IsTrue(desc.StartsWith("An adverb-k, narrowly-k defined-k, is-k a-k part-k of-k"));
			Assert.IsTrue(desc.EndsWith(" the-k class-k of-k the-k constituent-k being-k modified-k."));
			Assert.AreEqual(566, desc.Length);
			Assert.AreEqual("Adverb", adverb.CatalogSourceId);
			Assert.AreEqual(0, adverb.SubPossibilitiesOS.Count);

			var noun = listPOS.PossibilitiesOS[1] as IPartOfSpeech;
			Assert.IsNotNull(noun);
			Assert.AreEqual(6303632, noun.ForeColor);
			Assert.AreEqual(-1073741824, noun.BackColor);
			Assert.AreEqual(-1073741824, noun.UnderColor);
			Assert.AreEqual(1, noun.UnderStyle);
			Assert.AreEqual(4, noun.Abbreviation.StringCount);
			Assert.AreEqual("n", noun.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("sus", noun.Abbreviation.get_String(m_wsEs).Text);
			Assert.AreEqual("n", noun.Abbreviation.get_String(m_wsFr).Text);
			Assert.AreEqual("n-k", noun.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(4, noun.Name.StringCount);
			Assert.AreEqual("Noun", noun.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Nombre", noun.Name.get_String(m_wsEs).Text);
			Assert.AreEqual("Nom", noun.Name.get_String(m_wsFr).Text);
			Assert.AreEqual("Noun-k", noun.Name.get_String(m_wsDe).Text);
			Assert.AreEqual(2, noun.Description.StringCount);
			desc = noun.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("A noun is a broad classification of parts of speech"));
			Assert.IsTrue(desc.EndsWith(" which include substantives and nominals."));
			Assert.AreEqual(92, desc.Length);
			desc = noun.Description.get_String(m_wsDe).Text;
			Assert.IsTrue(desc.StartsWith("A noun-k is-k a-k broad-k classification-k of-k parts-k of-k speech-k"));
			Assert.IsTrue(desc.EndsWith(" which-k include-k substantives-k and-k nominals-k."));
			Assert.AreEqual(120, desc.Length);
			Assert.AreEqual("Noun", noun.CatalogSourceId);
			Assert.AreEqual(2, noun.SubPossibilitiesOS.Count);

			// Note that the following items are unchanged by the import translated list process.

			var nominal = noun.SubPossibilitiesOS[0] as IPartOfSpeech;
			Assert.IsNotNull(nominal);
			Assert.AreEqual(3, nominal.Abbreviation.StringCount);
			Assert.AreEqual("nom", nominal.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("nom", nominal.Abbreviation.get_String(m_wsEs).Text);
			Assert.AreEqual("nom", nominal.Abbreviation.get_String(m_wsFr).Text);
			Assert.AreEqual(3, nominal.Name.StringCount);
			Assert.AreEqual("Nominal", nominal.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Nominal", nominal.Name.get_String(m_wsEs).Text);
			Assert.AreEqual("Nominal", nominal.Name.get_String(m_wsFr).Text);
			Assert.AreEqual(2, nominal.Description.StringCount);
			desc = nominal.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("A nominal is a part of speech whose members differ"));
			Assert.IsTrue(desc.EndsWith(" from a substantive but which functions as one."));
			Assert.AreEqual(111, desc.Length);
			desc = nominal.Description.get_String(m_wsFr).Text;
			Assert.IsTrue(desc.StartsWith("Un nominal est un constituant syntaxique caractérisé par"));
			Assert.IsTrue(desc.EndsWith(", ainsi que les syntagmes nominaux et les pronoms.)"));
			Assert.AreEqual(602, desc.Length);
			Assert.AreEqual("Nominal", nominal.CatalogSourceId);
			Assert.AreEqual(1, nominal.SubPossibilitiesOS.Count);

			var gerund = nominal.SubPossibilitiesOS[0] as IPartOfSpeech;
			Assert.IsNotNull(gerund);
			Assert.AreEqual(2, gerund.Abbreviation.StringCount);
			Assert.AreEqual("ger", gerund.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("ger", gerund.Abbreviation.get_String(m_wsFr).Text);
			Assert.AreEqual(2, gerund.Name.StringCount);
			Assert.AreEqual("Gerund", gerund.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Ge\u0301rundif", gerund.Name.get_String(m_wsFr).Text);
			Assert.AreEqual(1, gerund.Description.StringCount);
			desc = gerund.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("A part of speech derived from a verb and used as a noun,"));
			Assert.IsTrue(desc.EndsWith(" usually restricted to non-finite forms of the verb."));
			Assert.AreEqual(108, desc.Length);
			Assert.AreEqual("Gerund", gerund.CatalogSourceId);
			Assert.AreEqual(0, gerund.SubPossibilitiesOS.Count);

			var substantive = noun.SubPossibilitiesOS[1] as IPartOfSpeech;
			Assert.IsNotNull(substantive);
			Assert.AreEqual(2, substantive.Abbreviation.StringCount);
			Assert.AreEqual("subs", substantive.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("subst", substantive.Abbreviation.get_String(m_wsFr).Text);
			Assert.AreEqual(2, substantive.Name.StringCount);
			Assert.AreEqual("Substantive", substantive.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Substantif", substantive.Name.get_String(m_wsFr).Text);
			Assert.AreEqual(1, substantive.Description.StringCount);
			desc = substantive.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("A substantive is a member of the syntactic class in which"));
			Assert.IsTrue(desc.EndsWith(" grammatical gender (in languages which inflect for gender)."));
			Assert.AreEqual(309, desc.Length);
			Assert.AreEqual("Substantive", substantive.CatalogSourceId);
			Assert.AreEqual(0, substantive.SubPossibilitiesOS.Count);
		}


		private void CheckSemanticDomains(ICmPossibilityList listSemDom, List<ICmSemanticDomain> allSemanticDomains)
		{
			Assert.AreEqual(2, listSemDom.PossibilitiesOS.Count);
			Assert.AreEqual(2, listSemDom.Abbreviation.StringCount);
			Assert.AreEqual("Sem", listSemDom.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("Sem-k", listSemDom.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(2, listSemDom.Name.StringCount);
			Assert.AreEqual("Semantic Domains", listSemDom.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Semantic-k Domains-k", listSemDom.Name.get_String(m_wsDe).Text);

			// verify that adding translations hasn't added (or removed!) any new items
			List<ICmSemanticDomain> newSemanticDomains = new List<ICmSemanticDomain>();
			newSemanticDomains.AddRange(m_repoSemDom.AllInstances());
			Assert.AreEqual(11, newSemanticDomains.Count);
			foreach (var sem in newSemanticDomains)
				Assert.IsTrue(allSemanticDomains.Contains(sem));

			ICmSemanticDomain sem1 = listSemDom.PossibilitiesOS[0] as ICmSemanticDomain;
			Assert.IsNotNull(sem1);
			Assert.AreEqual(2, sem1.Abbreviation.StringCount);
			Assert.AreEqual("1", sem1.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1-k", sem1.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem1.Name.StringCount);
			Assert.AreEqual("Universe, creation", sem1.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Universe-k, creation-k", sem1.Name.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem1.Description.StringCount);
			string desc = sem1.Description.get_String(m_wsEn).Text;
			Assert.IsTrue(desc.StartsWith("Use this domain for general words referring to the physical universe."));
			Assert.IsTrue(desc.EndsWith(" such as 'everything you can see' or 'everything that exists'."));
			Assert.AreEqual(313, desc.Length);
			desc = sem1.Description.get_String(m_wsDe).Text;
			Assert.IsTrue(desc.StartsWith("Use-k this-k domain-k for-k general-k words-k referring-k to-k the-k physical-k universe-k."));
			Assert.IsTrue(desc.EndsWith(" such-k as-k 'everything-k you-k can-k see-k' or-k 'everything-k that-k exists-k'."));
			Assert.AreEqual(427, desc.Length);
			Assert.AreEqual("772 Cosmology;  130 Geography", sem1.OcmCodes);
			Assert.AreEqual("1A Universe, Creation;  14 Physical Events and States", sem1.LouwNidaCodes);
			Assert.AreEqual(1, sem1.QuestionsOS.Count);
			ICmDomainQ cdq = sem1.QuestionsOS[0];
			Assert.AreEqual(2, cdq.Question.StringCount);
			Assert.AreEqual("(1) What words refer to everything we can see?", cdq.Question.get_String(m_wsEn).Text);
			Assert.AreEqual("(1)-k What-k words-k refer-k to-k everything-k we-k can-k see-k?",
				cdq.Question.get_String(m_wsDe).Text);
			Assert.AreEqual(2, cdq.ExampleWords.StringCount);
			Assert.AreEqual("universe, creation, cosmos, heaven and earth, macrocosm, everything that exists",
				cdq.ExampleWords.get_String(m_wsEn).Text);
			Assert.AreEqual("universe-k, creation-k, cosmos-k, heaven-k and-k earth-k, macrocosm-k, everything-k that-k exists-k",
				cdq.ExampleWords.get_String(m_wsDe).Text);
			Assert.AreEqual(2, cdq.ExampleSentences.StringCount);
			Assert.AreEqual("In the beginning God created <the heavens and the earth>.",
				cdq.ExampleSentences.get_String(m_wsEn).Text);
			Assert.AreEqual("In-k the-k beginning-k God-k created-k <the-k heavens-k and-k the-k earth-k>.",
				cdq.ExampleSentences.get_String(m_wsDe).Text);
			Assert.AreEqual(3, sem1.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem11 = sem1.SubPossibilitiesOS[0] as ICmSemanticDomain;
			Assert.IsNotNull(sem11);
			Assert.AreEqual(2, sem11.Abbreviation.StringCount);
			Assert.AreEqual("1.1", sem11.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.1-k", sem11.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem11.Name.StringCount);
			Assert.AreEqual("Sky", sem11.Name.get_String(m_wsEn).Text);
			Assert.AreEqual("Sky-k", sem11.Name.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem11.Description.StringCount);
			Assert.AreEqual("Use this domain for words related to the sky.",
				sem11.Description.get_String(m_wsEn).Text);
			Assert.AreEqual("Use-k this-k domain-k for-k words-k related-k to-k the-k sky-k.",
				sem11.Description.get_String(m_wsDe).Text);
			Assert.IsTrue(String.IsNullOrEmpty(sem11.OcmCodes));
			Assert.AreEqual("1B Regions Above the Earth", sem11.LouwNidaCodes);
			Assert.AreEqual(3, sem11.QuestionsOS.Count);
			cdq = sem11.QuestionsOS[0];
			Assert.AreEqual(2, cdq.Question.StringCount);
			Assert.AreEqual("(1) What words are used to refer to the sky?",
				cdq.Question.get_String(m_wsEn).Text);
			Assert.AreEqual("(1)-k What-k words-k are-k used-k to-k refer-k to-k the-k sky-k?",
				cdq.Question.get_String(m_wsDe).Text);
			Assert.AreEqual(2, cdq.ExampleWords.StringCount);
			Assert.AreEqual("sky, firmament, canopy, vault",
				cdq.ExampleWords.get_String(m_wsEn).Text);
			Assert.AreEqual("sky-k, firmament-k, canopy-k, vault-k",
				cdq.ExampleWords.get_String(m_wsDe).Text);
			Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
			cdq = sem11.QuestionsOS[1];
			Assert.AreEqual(2, cdq.Question.StringCount);
			Assert.AreEqual("(2) What words refer to the air around the earth?",
				cdq.Question.get_String(m_wsEn).Text);
			Assert.AreEqual("(2)-k What-k words-k refer-k to-k the-k air-k around-k the-k earth-k?",
				cdq.Question.get_String(m_wsDe).Text);
			Assert.AreEqual(2, cdq.ExampleWords.StringCount);
			Assert.AreEqual("air, atmosphere, airspace, stratosphere, ozone layer",
				cdq.ExampleWords.get_String(m_wsEn).Text);
			Assert.AreEqual("air-k, atmosphere-k, airspace-k, stratosphere-k, ozone-k layer-k",
				cdq.ExampleWords.get_String(m_wsDe).Text);
			Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
			cdq = sem11.QuestionsOS[2];
			Assert.AreEqual(2, cdq.Question.StringCount);
			Assert.AreEqual("(3) What words are used to refer to the place or area beyond the sky?",
				cdq.Question.get_String(m_wsEn).Text);
			Assert.AreEqual("(3)-k What-k words-k are-k used-k to-k refer-k to-k the-k place-k or-k area-k beyond-k the-k sky-k?",
				cdq.Question.get_String(m_wsDe).Text);
			Assert.AreEqual(2, cdq.ExampleWords.StringCount);
			Assert.AreEqual("heaven, space, outer space, ether, void, solar system",
				cdq.ExampleWords.get_String(m_wsEn).Text);
			Assert.AreEqual("heaven-k, space-k, outer-k space-k, ether-k, void-k, solar-k system-k",
				cdq.ExampleWords.get_String(m_wsDe).Text);
			Assert.AreEqual(0, cdq.ExampleSentences.StringCount);
			Assert.AreEqual(2, sem11.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem111 = sem11.SubPossibilitiesOS[0] as ICmSemanticDomain;
			Assert.IsNotNull(sem111);
			Assert.AreEqual(2, sem111.Abbreviation.StringCount);
			Assert.AreEqual("1.1.1", sem111.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.1.1-k", sem111.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem111.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem1111 = sem111.SubPossibilitiesOS[0] as ICmSemanticDomain;
			Assert.IsNotNull(sem1111);
			Assert.AreEqual(2, sem1111.Abbreviation.StringCount);
			Assert.AreEqual("1.1.1.1", sem1111.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.1.1.1-k", sem1111.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem1111.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem1112 = sem111.SubPossibilitiesOS[1] as ICmSemanticDomain;
			Assert.IsNotNull(sem1112);
			Assert.AreEqual(2, sem1112.Abbreviation.StringCount);
			Assert.AreEqual("1.1.1.2", sem1112.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.1.1.2-k", sem1112.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem1112.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem112 = sem11.SubPossibilitiesOS[1] as ICmSemanticDomain;
			Assert.IsNotNull(sem112);
			Assert.AreEqual(2, sem112.Abbreviation.StringCount);
			Assert.AreEqual("1.1.2", sem112.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.1.2-k", sem112.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem112.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem12 = sem1.SubPossibilitiesOS[1] as ICmSemanticDomain;
			Assert.IsNotNull(sem12);
			Assert.AreEqual(2, sem12.Abbreviation.StringCount);
			Assert.AreEqual("1.2", sem12.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.2-k", sem12.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem12.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem13 = sem1.SubPossibilitiesOS[2] as ICmSemanticDomain;
			Assert.IsNotNull(sem13);
			Assert.AreEqual(2, sem13.Abbreviation.StringCount);
			Assert.AreEqual("1.3", sem13.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("1.3-k", sem13.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem13.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem2 = listSemDom.PossibilitiesOS[1] as ICmSemanticDomain;
			Assert.IsNotNull(sem2);
			Assert.AreEqual(2, sem2.Abbreviation.StringCount);
			Assert.AreEqual("2", sem2.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("2-k", sem2.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(2, sem2.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem21 = sem2.SubPossibilitiesOS[0] as ICmSemanticDomain;
			Assert.IsNotNull(sem21);
			Assert.AreEqual(2, sem21.Abbreviation.StringCount);
			Assert.AreEqual("2.1", sem21.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("2.1-k", sem21.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem21.SubPossibilitiesOS.Count);

			ICmSemanticDomain sem22 = sem2.SubPossibilitiesOS[1] as ICmSemanticDomain;
			Assert.IsNotNull(sem22);
			Assert.AreEqual(2, sem22.Abbreviation.StringCount);
			Assert.AreEqual("2.2", sem22.Abbreviation.get_String(m_wsEn).Text);
			Assert.AreEqual("2.2-k", sem22.Abbreviation.get_String(m_wsDe).Text);
			Assert.AreEqual(0, sem22.SubPossibilitiesOS.Count);
		}
	}
}
