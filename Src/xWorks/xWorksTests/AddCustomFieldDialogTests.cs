using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests for (what small part has automated tests of) AddCustomFieldDialog.
	/// </summary>
	[TestFixture]
	public class AddCustomFieldDialogTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the code that populates the Lists combo.
		/// </summary>
		[Test]
		public void PopulateListsCombo()
		{
			string source =
				@"<window>
					  <lists>
						<list id='AreasList'>
						  <item label='Lists' value='lists'>
							<parameters id='lists'>
							  <clerks>
								<clerk id='SemanticDomainList'>
								  <recordList owner='LangProject' property='SemanticDomainList'>
								  </recordList>
								</clerk>
								<clerk id='customList'>
								  <recordList owner='unowned' property='placeholder'>
								  </recordList>
								</clerk>
								<clerk id='GenreList'>
								  <recordList owner='LangProject' property='GenreList'>
								  </recordList>
								</clerk>
							  </clerks>
							  <tools>
								<tool label='Genres' value='genresEdit'>
								  <control>
									<parameters>
									  <control>
										<parameters area='lists' clerk='GenreList'  />
									  </control>
									</parameters>
								  </control>
								</tool>
								<tool label='Semantic Domains' value='semanticDomainEdit' icon='SideBySideView'>
								  <control>
									<parameters>
									  <control>
										<parameters area='lists' dummy='control has no clerk' />
									  </control>
									  <SomeSillyWrapper>
										  <control>
											<parameters area='lists' clerk='SemanticDomainList' />
										  </control>
									  </SomeSillyWrapper>
									</parameters>
								  </control>
								</tool>
								<tool label='Custom 1' value='custom1list'>
								  <control>
									<parameters>
									  <control>
										<parameters area='lists' clerk='customList' />
									  </control>
									</parameters>
								  </control>
								</tool>
							  </tools>
							</parameters>
						  </item>
						  <item label='Grammar' value='grammar'>
							<parameters id='lists'>
							  <clerks>
								<clerk id='categories'>
								  <recordList owner='LangProject' property='PartsOfSpeech'>
								  </recordList>
								</clerk>
							  </clerks>
							  <tools>
								<tool label='Categories Browse' value='categories'>
								  <control>
									<parameters>
									  <control>
										<parameters area='grammar' clerk='categories'  />
									  </control>
									</parameters>
								  </control>
								</tool>
								<tool label='Categories Edit' value='categories'>
								  <control>
									<parameters>
									  <control>
										<parameters area='grammar' clerk='categories'  />
									  </control>
									</parameters>
								  </control>
								</tool>
							  </tools>
							</parameters>
						  </item>
						</list>
					  </lists>
					 </window>";
			var cmPossibilityListFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			var customList = cmPossibilityListFactory.Create();
			if (Cache.LangProject.SemanticDomainListOA == null)
				Cache.LangProject.SemanticDomainListOA = cmPossibilityListFactory.Create();
			if (Cache.LangProject.GenreListOA == null)
				Cache.LangProject.GenreListOA = cmPossibilityListFactory.Create();
			var realSource = source.Replace("placeholder", customList.Guid.ToString());
			var doc = new XmlDocument();
			doc.LoadXml(realSource);
			var windowConfiguration = doc.DocumentElement;
			var items = AddCustomFieldDlg.GetListsComboItems(Cache, windowConfiguration);
			Assert.That(items, Has.Count.EqualTo(4));
			Assert.That(items[0].Name, Is.EqualTo("Custom 1"));
			Assert.That(items[1].Name, Is.EqualTo("Genres"));
			Assert.That(items[2].Name, Is.EqualTo("PartsOfSpeech"));
			Assert.That(items[3].Name, Is.EqualTo("Semantic Domains"));
			Assert.That(items[0].Id, Is.EqualTo(customList.Guid));
			Assert.That(items[1].Id, Is.EqualTo(Cache.LangProject.GenreListOA.Guid));
			Assert.That(items[2].Id, Is.EqualTo(Cache.LangProject.PartsOfSpeechOA.Guid));
			Assert.That(items[3].Id, Is.EqualTo(Cache.LangProject.SemanticDomainListOA.Guid));
		}
	}
}
