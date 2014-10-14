// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	class PublishToWebonaryControllerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private FwXApp m_application;
		private FwXWindow m_window;
		private Mediator m_mediator;
		private FwStyleSheet m_styleSheet;
		private StyleInfoTable m_owningTable;
		private RecordClerk m_Clerk;

		[TestFixtureSetUp]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "Clerk disposed in TearDown")]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(configFilePath);
			// set up clerk to allow DictionaryPublicationDecorator to be created during the PublishToWebonaryController driven export
			const string reversalIndexClerk = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='entries'>
						<recordList owner='LexDb' property='Entries'/>
					</clerk>
				</clerks>
				<tools>
					<tool label='Dictionary' value='lexiconDictionary' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='entries' layout='Bartholomew' layoutProperty='DictionaryPublicationLayout' editable='false' configureObjectName='Dictionary'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(reversalIndexClerk);
			XmlNode clerkNode = doc.SelectSingleNode("//tools/tool[@label='Dictionary']//parameters[@area='lexicon']");
			m_Clerk = RecordClerkFactory.CreateClerk(m_mediator, clerkNode, false);
			m_mediator.PropertyTable.SetProperty("ActiveClerk", m_Clerk);
			m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_lexicon", "lexiconDictionary");
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			// setup style sheet and style to allow the css to generate during the PublishToWebonaryController driven export
			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_owningTable = new StyleInfoTable("AbbySomebody", (IWritingSystemManager)Cache.WritingSystemFactory);
			var fontInfo = new FontInfo();
			var style = new TestStyle(fontInfo, Cache) { Name = "Dictionary-LetterHeading", IsParagraphStyle = false };
			m_styleSheet.Styles.Add(style);
			m_owningTable.Add("Dictionary-LetterHeading", style);
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if(m_Clerk != null)
				   m_Clerk.Dispose();
				if (m_application != null)
					m_application.Dispose();
				if (m_window != null)
					m_window.Dispose();
				if (m_mediator != null)
					m_mediator.Dispose();
			}
		}

		~PublishToWebonaryControllerTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion disposal

		[Test]
		public void PublishToWebonaryUsesViewConfigAndPub()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var controller = new PublishToWebonaryController { Cache = Cache, Mediator = m_mediator };
			var mockView = new MockWebonaryDlg();
			var config = mockView.Configuration = "Test Config";
			var pub = mockView.Publication = "Test publication";
			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			controller.Configurations = testConfig;
			testConfig["Test Config"] = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> {
					new ConfigurableDictionaryNode { FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass"}
				}
			};
			//SUT
			Assert.DoesNotThrow(() => controller.PublishToWebonary(mockView));
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains(pub) && s.Contains(config))));
		}

		[Test]
		public void PublishToWebonaryExportsXhtmlAndCss()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
			var controller = new PublishToWebonaryController { Cache = Cache, Mediator = m_mediator };
			var mockView = new MockWebonaryDlg
				{
					Configuration = "Test Config",
					Publication = "Test publication"
				};
			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			controller.Configurations = testConfig;
			// Build model sufficient to generate xhtml and css
			var model = new DictionaryConfigurationModel();
			model.Parts = new List<ConfigurableDictionaryNode>();
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				CSSClassNameOverride = "entry",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions {Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { "fr" })},
				Before = "MainEntry: ",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			model.Parts.Add(mainEntryNode);
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			testConfig["Test Config"] = model;
			// create entry sufficient to generate xhtml and css
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entry.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("Headword", wsFr));
			//SUT
			Assert.DoesNotThrow(() => controller.PublishToWebonary(mockView));
			// The names of the files being sent to webonary are listed while logging the zip
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("configured.xhtml"))), "xhtml not logged as compressed");
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("configured.css"))), "css not logged as compressed");
		}
		
		#region Test connection to local Webonary instance
		[Test]
		[Category("ByHand")]
		public void CanConnectAtAll()
		{
			using (var client = new WebClient())
			{
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Contains.Substring("returnedData"));
			}
		}

		[Test]
		[Category("ByHand")]
		public void CanAuthenticate()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("username:password")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.Not.StringContaining("You are not logged in."));
			}
		}

		[Test]
		[Category("ByHand")]
		public void ZipFileExtracts()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("username:password")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.StringContaining("extracted successfully"));
			}
		}

		/// <summary>
		/// Helper
		/// </summary>
		static string ConnectAndUpload(WebClient client)
		{
			var targetURI = "http://192.168.33.10/test/wp-json/webonary/import";
			var inputFile = "../../Src/xWorks/xWorksTests/lubwisi-d-new.zip";
			var response = client.UploadFile(targetURI, inputFile);
			var responseText = System.Text.Encoding.ASCII.GetString(response);
			return responseText;
		}
		#endregion

		internal class MockWebonaryDlg : IPublishToWebonaryView
		{
			// Collect the status messages that are generated during the export
			public List<string> StatusStrings = new List<string>();
			public void UpdateStatus(string statusString)
			{
				StatusStrings.Add(statusString);
			}

			public void PopulatePublicationsList(IEnumerable<string> publications)
			{
				;
			}

			public void PopulateConfigurationsList(IEnumerable<string> configurations)
			{
				;
			}

			public void PopulateReversalsCheckboxList(IEnumerable<string> reversals)
			{
				;
			}

			public string Configuration { get;  set; }
			public string Publication { get;  set; }
		}
	}
}
