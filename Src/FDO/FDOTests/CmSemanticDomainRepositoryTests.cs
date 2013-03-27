// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CmSemanticDomainRepositoryTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test stuff related to Possibility List Repository functions.
	/// </summary>
	[TestFixture]
	public class CmSemanticDomainRepositoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ICmSemanticDomainRepository m_semdomRepo;
		private ILexEntryFactory m_entryFactory;
		private ILexSenseFactory m_senseFactory;

		private const string XMLTESTDATA =
	@"<?xml version='1.0' encoding='UTF-8'?>
		<LangProject>
			<SemanticDomainList>
				<CmPossibilityList>
					<ItemClsid><Integer val='66'/></ItemClsid>
					<WsSelector><Integer val='-3'/></WsSelector>
					<Name>
						<AUni ws='en'>Semantic Domains</AUni>
					</Name>
					<Abbreviation>
						<AUni ws='en'>Sem</AUni>
					</Abbreviation>
					<Possibilities>
						<CmSemanticDomain guid='63403699-07C1-43F3-A47C-069D6E4316E5'>
							<Abbreviation>
								<AUni ws='en'>1</AUni>
							</Abbreviation>
							<Name>
								<AUni ws='en'>Universe, creation</AUni>
							</Name>
							<Description>
								<AStr ws='en'>
									<Run ws='en'>Use this domain for the physical universe.</Run>
								</AStr>
							</Description>
							<Questions>
								<CmDomainQ>
									<ExampleWords>
										<AUni ws='en'>universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>
									</ExampleWords>
								</CmDomainQ>
							</Questions>
							<SubPossibilities>
								<CmSemanticDomain guid='999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C'>
									<Abbreviation>
										<AUni ws='en'>1.1</AUni>
									</Abbreviation>
									<Name>
										<AUni ws='en'>Sky</AUni>
									</Name>
									<Description>
										<AStr ws='en'>
											<Run ws='en'>Use this domain for words related to the sky.</Run>
										</AStr>
									</Description>
									<Questions>
										<CmDomainQ>
											<ExampleWords>
												<AUni ws='en'>sky, firmament</AUni>
											</ExampleWords>
										</CmDomainQ>
										<CmDomainQ>
											<ExampleWords>
												<AUni ws='en'>air, atmosphere</AUni>
											</ExampleWords>
										</CmDomainQ>
									</Questions>
									<SubPossibilities>
										<CmSemanticDomain guid='DC1A2C6F-1B32-4631-8823-36DACC8CB7BB'>
											<Abbreviation>
												<AUni ws='en'>1.1.8</AUni>
											</Abbreviation>
											<Name>
												<AUni ws='en'>Sun</AUni>
											</Name>
											<Description>
												<AStr ws='en'>
													<Run ws='en'>words related to the sun.</Run>
												</AStr>
											</Description>
											<Questions>
												<CmDomainQ>
													<ExampleWords>
														<AUni ws='en'>sun, solar</AUni>
													</ExampleWords>
												</CmDomainQ>
											</Questions>
										</CmSemanticDomain>
									</SubPossibilities>
								</CmSemanticDomain>
							</SubPossibilities>
						</CmSemanticDomain>
						<CmSemanticDomain guid='1C3F8996-362E-4EE0-AF02-0DD02887F6AA'>
							<Abbreviation>
								<AUni ws='en'>4.9.6</AUni>
							</Abbreviation>
							<Name>
								<AUni ws='en'>Heaven, hell</AUni>
							</Name>
							<Description>
								<AStr ws='en'>
									<Run ws='en'>words related to heaven and hell.</Run>
								</AStr>
							</Description>
							<Questions>
								<CmDomainQ>
									<ExampleWords>
										<AUni ws='en'>Heaven, highest heaven, abode of God</AUni>
									</ExampleWords>
								</CmDomainQ>
							</Questions>
						</CmSemanticDomain>
						<CmSemanticDomain guid='4BF411B7-2B5B-4673-B116-0E6C31FBD08A'>
							<Abbreviation>
								<AUni ws='en'>8.3.3</AUni>
							</Abbreviation>
							<Name>
								<AUni ws='en'>Light</AUni>
							</Name>
							<Description>
								<AStr ws='en'>
									<Run ws='en'>words related to light.</Run>
								</AStr>
							</Description>
							<Questions>
								<CmDomainQ>
									<ExampleWords>
										<AUni ws='en'>light, sunshine, skylight</AUni>
									</ExampleWords>
								</CmDomainQ>
							</Questions>
							<SubPossibilities>
								<CmSemanticDomain guid='A7824686-A3F3-4C8A-907E-5D841CF846C8'>
									<Abbreviation>
										<AUni ws='en'>8.3.3.1</AUni>
									</Abbreviation>
									<Name>
										<AUni ws='en'>Shine</AUni>
									</Name>
									<Description>
										<AStr ws='en'>
											<Run ws='en'>light source shining something to make light.</Run>
										</AStr>
									</Description>
									<Questions>
										<CmDomainQ>
											<ExampleWords>
												<AUni ws='en'>shine, glow (n), glow-worm, it's alive!</AUni>
											</ExampleWords>
										</CmDomainQ>
									</Questions>
								</CmSemanticDomain>
							</SubPossibilities>
						</CmSemanticDomain>
					</Possibilities>
				</CmPossibilityList>
			</SemanticDomainList>
		</LangProject>";

		private const string WRONG_NUMBER_OF_MATCHES = "Found the wrong number of results";
		private const string WRONG_SEMDOM_NUMBER = "Wrong Semantic Domain number returned.";
		private const string WRONG_SEMDOM_NAME = "Wrong Semantic Domain name returned.";

		/// <summary>
		/// Load the test data into the Semantic Domains list.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();

			Cache.ActionHandlerAccessor.EndUndoTask(); // I'll make my own Undo tasks, since ImportList has its own.
			var servLoc = Cache.ServiceLocator;
			m_semdomRepo = servLoc.GetInstance<ICmSemanticDomainRepository>();
			m_entryFactory = servLoc.GetInstance<ILexEntryFactory>();
			m_senseFactory = servLoc.GetInstance<ILexSenseFactory>();

			LoadSemDomTestData(XMLTESTDATA);
		}

		private void LoadSemDomTestData(string xmlSemDomData)
		{
			using (var trdr = new StringReader(xmlSemDomData))
			{
				var loader = new XmlList();
				loader.ImportList(Cache.LangProject, "SemanticDomainList", trdr, new DummyProgressDlg());
			}
		}

		private void LoadSemDomTestDataFromFile(string filePath)
		{
			var loader = new XmlList();
			loader.ImportList(Cache.LangProject, "SemanticDomainList", filePath, new DummyProgressDlg());
		}

		#region Helper Methods

		// Used to create a sense to search on for the SenseSearchStrategy.
		// Therefore it should create the gloss in the AnalysisDefaultWritingSystem.
		private ILexEntry CreateLexEntry(string form, string gloss)
		{
			ILexEntry entry = null;
			UndoableUnitOfWorkHelper.Do("Undo CreateEntry", "Redo CreateEntry", Cache.ActionHandlerAccessor, () =>
			{
				entry = MakeEntry(form, gloss);
			});

			return entry;
		}

		private ILexEntry MakeEntry(string form, string gloss)
		{
			var result = MakeEntryWithForm(form);
			var sense = m_senseFactory.Create();
			result.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(
				gloss, Cache.DefaultAnalWs);
			return result;
		}

		private ILexEntry MakeEntryWithForm(string form)
		{
			var entry = m_entryFactory.Create();
			entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(
				form, Cache.DefaultVernWs);
			return entry;
		}

		private void AddReversalsToSense(ILexSense sense, IEnumerable<string> reversalStrings)
		{
			var revEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			UndoableUnitOfWorkHelper.Do("Undo reversals", "Redo reversals",
				Cache.ActionHandlerAccessor, () =>
			{
				var revIndex = CreateEmptyReversalIndex();
				foreach (var reversalForm in reversalStrings)
				{
					var newEntry = revEntryFactory.Create();
					revIndex.EntriesOC.Add(newEntry);
					sense.ReversalEntriesRC.Add(newEntry);
					newEntry.ReversalForm.SetAnalysisDefaultWritingSystem(reversalForm);
				}
			});
		}

		private IReversalIndex CreateEmptyReversalIndex()
		{
			var result = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>().Create();
			Cache.LangProject.LexDbOA.ReversalIndexesOC.Add(result);
			return result;
		}

		#endregion

		/// <summary>
		/// Test finding matching semantic domains using an alphabetic search string.
		/// </summary>
		[Test]
		public void FindOneDomain_Alpha_WholeWordMatchesGroup1CaseInsensitive()
		{
			//Setup
			const string searchString = "sky";
			const string expectedNum = "1.1"; // group1 match
			const string expectedName = "Sky";
			const string expectedNum2 = "8.3.3"; // group3 match

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(2, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedName, resultList[0].Name.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NAME);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding partial match in 2nd question Example Words.
		/// </summary>
		[Test]
		public void FindOneDomain_Alpha_PartialWordMatchesExampleInSecondQuestion()
		{
			//Setup
			const string searchString = "atmos";
			const string expectedNum = "1.1"; // group3 match
			const string expectedName = "Sky";

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(1, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using 'shine' as the alphabetic search string.
		/// </summary>
		[Test]
		public void FindOneDomain_Alpha_DoesNotMatchFinally()
		{
			//Setup
			const string searchString = "shine";
			const string expectedNum1 = "8.3.3.1"; // group1 match 'shine'
			// specifically should NOT match 'sunshine'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(1, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a short alphabetic search string.
		/// </summary>
		[Test]
		public void FindDomains_Alpha_Groups1And3()
		{
			//Setup
			const string searchString = "sun";
			const string expectedNum1 = "1.1.8"; // group1 match 'sun'
			const string expectedName1 = "Sun";
			const string expectedNum2 = "8.3.3"; // group3 match 'sunshine', under 'Light'
			const string expectedName2 = "Light";

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(2, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedName1, resultList[0].Name.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NAME);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedName2, resultList[1].Name.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NAME);
		}

		/// <summary>
		/// Test finding matching semantic domains using a short alphabetic search string.
		/// </summary>
		[Test]
		[Category("LongRunning")]
		[Category("ByHand")]
		[Ignore("Only run manually as it messes up following tests.")]
		// because XmlList.ImportList(), which LoadSemDomTestDataFromFile() uses, uses a NonUndoableTask.
		public void FindDomains_TimingOnLargerDataSet()
		{
			//Setup
			const string searchString = "sun";
			const string expectedNum1 = "1.1.8"; // group1 match 'sun'
			const string expectedName1 = "Sun";
			const string expectedNum2 = "8.3.3"; // group3 match 'sunshine', under 'Light'
			const string expectedName2 = "Light";
			const string filePath = @"Templates\semdom.xml";
			var homeDir = DirectoryFinder.FWCodeDirectory;
			LoadSemDomTestDataFromFile(Path.Combine(homeDir, filePath));

			// SUT
			var watch = new Stopwatch();
			watch.Start();
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);
			watch.Stop();

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(22, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.Less(watch.ElapsedMilliseconds, 2000, "Too long!");
			Debug.WriteLine("Finding 'sun' in Semantic Domain search took: {0}ms", watch.ElapsedMilliseconds);
		}

		/// <summary>
		/// Test finding matching semantic domains using 's' as the alphabetic search string.
		/// </summary>
		[Test]
		public void FindDomains_Alpha_SingleChar_SeveralMatchesButNotDuplicateExampleWord()
		{
			//Setup
			const string searchString = "s";
			const string expectedNum1 = "1.1";     // group1 match 'sky'
			const string expectedNum2 = "1.1.8";   // group1 match 'sun'
			const string expectedNum3 = "8.3.3.1"; // group1 match 'shine'
			// no group2 matches
			const string expectedNum4 = "8.3.3";   // group3 match 'sunshine', under 'Light'
			// should NOT get an additional match for 'solar', because '1.1.8 Sun' is already a group1 match.

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(4, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, resultList[2].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum4, resultList[3].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test that finding group2 matching semantic domains in Example Words is case insensitive.
		/// </summary>
		[Test]
		public void FindDomain_Alpha_TestGroup2CaseInsensitivity_ExampleWords()
		{
			//Setup
			const string searchString = "Sunshine";
			// no group1 or group3 matches
			const string expectedNum = "8.3.3";   // group2 match 'sunshine', under 'Light'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(1, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test that finding group2 matching semantic domains in Name (not first word) is case insensitive.
		/// </summary>
		[Test]
		public void FindDomain_Alpha_TestGroup2CaseInsensitivity_Name()
		{
			//Setup
			const string searchString = "Creation";
			// no group1 or group3 matches
			const string expectedNum = "1";   // group2 match 'creation', after 'Universe'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(1, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding a group3 matching semantic domain in Name (not first word).
		/// </summary>
		[Test]
		public void FindDomain_Alpha_TestGroup3_Name()
		{
			//Setup
			const string searchString = "cre";
			// no group1 or group2 matches
			const string expectedNum = "1";   // group3 match 'creation', after 'Universe'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(1, resultList.Count, WRONG_NUMBER_OF_MATCHES); // should not have group1or2
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding and sorting a group2 match and a group1 match.
		/// </summary>
		[Test]
		public void FindDomain_Alpha_TestGroup1AndGroup2Difference()
		{
			//Setup
			const string searchString = "heaven";
			const string expectedNum1 = "4.9.6"; // group1 match 'Heaven, hell'
			const string expectedNum2 = "1";     // group2 match 'heaven and earth', under 'Universe, creation'
			// no group3 matches

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(2, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test that searching for 'n' does not match (n).
		/// </summary>
		[Test]
		public void DontFindDomain_Alpha_SurroundedByParentheses()
		{
			//Setup
			const string searchString = "n";
			// no matches expected

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(0, resultList.Count, WRONG_NUMBER_OF_MATCHES);
		}

		/// <summary>
		/// Test that searching for "glow-worm" works.
		/// </summary>
		[Test]
		public void FindDomain_Alpha_Hyphen()
		{
			//Setup
			const string searchString = "glow-worm";
			// no group1 or group3 matches
			const string expectedNum = "8.3.3.1";   // group2 match 'glow-worm', under 'Shine'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(1, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test that searching for "it's" works.
		/// </summary>
		[Test]
		public void FindDomain_Alpha_EmbeddedSingleQuote()
		{
			//Setup
			const string searchString = "it's";
			// no group1 or group3 matches
			const string expectedNum = "8.3.3.1";   // group2 match "it's", under 'Shine'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			Assert.AreEqual(1, resultList.Count, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a numeric search string.
		/// </summary>
		[Test]
		public void FindDomains_NumericSearchString_DoesntMatchFinally()
		{
			//Setup
			const string searchString = "8";
			const string expectedNum1 = "8.3.3";
			const string expectedName1 = "Light";
			const string expectedNum2 = "8.3.3.1";
			const string expectedName2 = "Shine";
			// specifically should NOT match '1.1.8'

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatch(searchString);

			// Verification
			var resultList = result.ToList();
			var cresult = resultList.Count;
			Assert.AreEqual(2, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedName1, resultList[0].Name.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NAME);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedName2, resultList[1].Name.BestAnalysisAlternative.Text,
				WRONG_SEMDOM_NAME);
		}

		/// <summary>
		/// Test finding matching semantic domains using a sense with only one word in a searchable field.
		/// </summary>
		[Test]
		public void SenseSearch_OneSearchKey()
		{
			//Setup
			const string searchString = "sun";
			const string expectedNum1 = "1.1.8"; // bucket1 match 'sun'
			const string expectedNum2 = "8.3.3"; // bucket2 match 'sunshine', under 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("soleil", searchString);
			var sense = entry.SensesOS[0];

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(1, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(1, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, partialsList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a sense with a two-word gloss.
		/// </summary>
		[Test]
		public void SenseSearch_MultiWordGloss()
		{
			//Setup
			const string searchString = "sky, God";
			const string expectedNum1 = "1.1";   // bucket1 match 'sky'
			const string expectedNum2 = "4.9.6"; // bucket1 match 'God', under 'Heaven, Hell'
			const string expectedNum3 = "8.3.3"; // bucket2 match 'skylight', under 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("waas", searchString);
			var sense = entry.SensesOS[0];

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(2, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(1, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, partialsList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a sense with a two-word gloss,
		/// but ignoring a definition that is 3 words long (as too long).
		/// </summary>
		[Test]
		public void SenseSearch_3WordDefinitionNotUsed()
		{
			//Setup
			const string searchString = "sky, God";
			const string expectedNum1 = "1.1";   // bucket1 match 'sky'
			const string expectedNum2 = "4.9.6"; // bucket1 match 'God', under 'Heaven, Hell'
			const string expectedNum3 = "8.3.3"; // bucket2 match 'skylight', under 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("waas", searchString);
			var sense = entry.SensesOS[0];
			// This definition is too long and won't be used. If it were used,
			// the 8.3.3 bucket2 match would become a bucket1 match of 'Light',
			// And there would be an additional bucket 1 match of '8.3.3.1 Shine'.
			UndoableUnitOfWorkHelper.Do("Undo def", "Redo def", Cache.ActionHandlerAccessor,
				() => sense.Definition.SetAnalysisDefaultWritingSystem("God's light shine"));

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(2, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(1, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, partialsList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a sense with a two-word gloss,
		/// and a 2 word definition (short enough).
		/// </summary>
		[Test]
		public void SenseSearch_2WordDefinitionUsed()
		{
			//Setup
			const string searchString = "sky, God";
			const string expectedNum1 = "1.1";   // bucket1 match 'sky'
			const string expectedNum2 = "1.1.8"; // bucket1 match 'solar', under 'Sun'
			const string expectedNum3 = "4.9.6"; // bucket1 match 'God', under 'Heaven, Hell'
			const string expectedNum4 = "8.3.3"; // bucket1 match 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("waas", searchString);
			var sense = entry.SensesOS[0];
			UndoableUnitOfWorkHelper.Do("Undo def", "Redo def", Cache.ActionHandlerAccessor,
				() => sense.Definition.SetAnalysisDefaultWritingSystem("Solar light"));

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(4, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(0, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, resultList[2].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum4, resultList[3].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test finding matching semantic domains using a sense with two reversal entries,
		/// each with two word forms.
		/// </summary>
		[Test]
		public void SenseSearch_OneGlossKey_TwoTwoWordReversals()
		{
			//Setup
			const string searchString = "sun";
			const string expectedNum1 = "1";     // bucket1 match 'earth' from reversal, under 'Universe, creation'
			const string expectedNum2 = "1.1";   // bucket1 match 'atmosphere', under 'Sky'
			const string expectedNum3 = "1.1.8"; // bucket1 match 'Sun'
			const string expectedNum4 = "4.9.6"; // bucket1 match 'Heaven, hell' because hell is in name
			const string expectedNum5 = "8.3.3"; // bucket2 match 'sunshine' from gloss sun, under 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("soleil", searchString);
			var sense = entry.SensesOS[0];
			AddReversalsToSense(sense, new string[] { "sunset, atmosphere", "hell on earth" });

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(4, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(1, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, resultList[2].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum4, resultList[3].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum5, partialsList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}

		/// <summary>
		/// Test that we can find matches when the default analysis writing system is not the one in which the domains
		/// and the sense have relevant information.
		/// </summary>
		[Test]
		public void SenseSearch_KeysNotInFirstAnalysisWs()
		{
			//Setup
			const string expectedNum1 = "1";     // bucket1 match 'earth' from reversal, under 'Universe, creation'
			const string expectedNum2 = "1.1";   // bucket1 match 'atmosphere' from reversal, under 'Sky'
			const string expectedNum3 = "1.1.8"; // bucket1 match 'Sun', in name and example words (but should only be found once)
			const string expectedNum4 = "4.9.6"; // bucket1 match 'Heaven, hell' because hell is in name
			const string expectedNum5 = "8.3.3"; // bucket2 match 'sunshine' from gloss sun, under 'Light'
			IEnumerable<ICmSemanticDomain> partialMatches;
			var entry = CreateLexEntry("soleil", "sun");
			var sense = entry.SensesOS[0];
			AddReversalsToSense(sense, new string[] { "sunset, atmosphere", "hell on earth" });

			var oldWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			IWritingSystem wsFr;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out wsFr);
			UndoableUnitOfWorkHelper.Do("Undo CreateEntry", "Redo CreateEntry", Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem = wsFr;
			});

			// SUT
			var result = m_semdomRepo.FindDomainsThatMatchWordsIn(sense, out partialMatches);

			// Verification
			UndoableUnitOfWorkHelper.Do("Undo CreateEntry", "Redo CreateEntry", Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem = oldWs; // so we can make the tests below
			});
			var resultList = result.ToList();
			var partialsList = partialMatches.ToList();
			var cresult = resultList.Count;
			var cpartials = partialsList.Count;
			Assert.AreEqual(4, cresult, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(1, cpartials, WRONG_NUMBER_OF_MATCHES);
			Assert.AreEqual(expectedNum1, resultList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum2, resultList[1].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum3, resultList[2].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum4, resultList[3].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
			Assert.AreEqual(expectedNum5, partialsList[0].Abbreviation.AnalysisDefaultWritingSystem.Text,
				WRONG_SEMDOM_NUMBER);
		}
	}
}
