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

		#region Environment
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
		#endregion Environment

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void PublishToWebonaryUsesViewConfigAndPub()
		{
			var controller = SetUpController();
			var mockView = SetUpView();
			//SUT
			Assert.DoesNotThrow(() => controller.PublishToWebonary(mockView.Model, mockView));
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains(mockView.Model.SelectedPublication) && s.Contains(mockView.Model.SelectedConfiguration))));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void PublishToWebonaryExportsXhtmlAndCss()
		{
			var controller = new PublishToWebonaryController { Cache = Cache, Mediator = m_mediator };
			var mockView = SetUpView();
			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			mockView.Model.Configurations = testConfig;
			// Build model sufficient to generate xhtml and css
			ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
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
			Assert.DoesNotThrow(() => controller.PublishToWebonary(mockView.Model, mockView));

			// The names of the files being sent to webonary are listed while logging the zip
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("configured.xhtml"))), "xhtml not logged as compressed");
			Assert.That(!String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("configured.css"))), "css not logged as compressed");
		}

		/// <summary>
		/// Just give an error if not all the info is supplied rather than crashing.
		/// </summary>
		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void PublishToWebonaryDoesNotCrashWithoutAllItsInfo()
		{
			var controller = SetUpController();
			var view = SetUpView();
			var model = view.Model;

			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("Publishing"))), "Inform that the process has started");
			model.SiteName = null;
			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
			model.SiteName="site";
			model.UserName=null;
			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
			model.UserName="user";
			model.Password=null;
			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
			model.Password="password";
			model.SelectedPublication = null;
			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
			model.SelectedPublication = "Test publication";
			model.SelectedConfiguration=null;
			Assert.DoesNotThrow(()=>controller.PublishToWebonary(model, view));
		}

		[Test]
		[Ignore("Until get working. Doesn't seem to put together the right kind of data to not get an error yet.")]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void PublishToWebonaryCanCompleteWithoutError()
		{
			var controller = SetUpController();
			var mockView = SetUpView();
			var model = mockView.Model;
			model.UserName = "webonary";
			model.Password = "webonary";
			//SUT
			Assert.DoesNotThrow(() => controller.PublishToWebonary(model, mockView));
			mockView.StatusStrings.ForEach(Console.WriteLine); // Remove this output line once this test works.
			Assert.That(String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("Error") || s.Contains("ERROR") || s.Contains("error"))));
		}

		#region Test connection to local Webonary instance
		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void CanConnectAtAll()
		{
			using (var client = new WebClient())
			{
				string responseText = null;
				Assert.DoesNotThrow(()=>{responseText = ConnectAndUpload(client);});
				Assert.That(responseText, Contains.Substring("username"),"Should get some sort of response from server, like an error message about authenticating.");
			}
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void CanAuthenticate()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("webonary:webonary")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.Not.StringContaining("authentication failed"));
				Assert.That(responseText, Is.Not.StringContaining("Wrong username or password"));
			}
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void ZipFileExtracts()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("webonary:webonary")));
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

		[Test]
		public void UploadToWebonaryThrowsOnNullInput()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg();
			var model = new PublishToWebonaryModel(m_mediator);
			Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary(null, model, view));
			Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", null, view));
			Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", model, null));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryReportsFailedAuthentication()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg()
			{
				Model = new PublishToWebonaryModel(m_mediator)
				{
					UserName = "nouser",
					Password = "nopassword"
				}
			};
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("Error: Wrong username or password"))));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryReportsLackingPermissionsToUpload()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg()
			{
				Model = new PublishToWebonaryModel(m_mediator)
				{
					UserName = "software",
					Password = "4APItesting"
				}
			};
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("Error: User doesn't have permission to import data"))));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryReportsSuccess()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg()
			{
				Model = new PublishToWebonaryModel(m_mediator)
				{
					UserName = "webonary",
					Password = "webonary"
				}
			};
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("Upload successful."))));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryReportsErrorsInProcessingData()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg()
			{
				Model = new PublishToWebonaryModel(m_mediator)
				{
					UserName = "webonary",
					Password = "webonary"
				}
			};
			// Contains a filename in the zip that isn't correct, so no data will be found by webonary.
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new-bad.zip", view.Model, view);
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("ERROR: No headwords found."))));
		}

		/// <summary>
		/// Does not crash. Reports error in upload.
		/// Marked ByHand since I don't want the build servers poking around on
		/// places on the network like this, and it also takes a few minutes to timeout.
		/// </summary>
		[Test]
		[Category("ByHand")]
		[Ignore("Takes too long to timeout. Enable if want to test.")]
		public void UploadToWebonaryHandlesNetworkErrors()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg();
			var filepath = "../../Src/xWorks/xWorksTests/lubwisi-d-new.zip";

			controller.UploadURI = "http://nameresolutionfailure.local/import.php";
			Assert.DoesNotThrow(()=>controller.UploadToWebonary(filepath, view.Model, view));
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("An error occurred uploading your data:"))));
			controller.UploadURI = "http://localhost:12345/import/connectfailure.php";
			Assert.DoesNotThrow(() => controller.UploadToWebonary(filepath, view.Model, view));
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("An error occurred uploading your data:"))));
			controller.UploadURI = "http://192.168.0.1/import/requesttimedout.php";
			Assert.DoesNotThrow(() => controller.UploadToWebonary(filepath, view.Model, view));
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("An error occurred uploading your data:"))));
		}

		#region Helpers
		/// <summary>
		/// Helper.
		/// </summary>
		public MockWebonaryDlg SetUpView()
		{
			return new MockWebonaryDlg {
				Model = SetUpModel()
			};
		}

		/// <summary>
		/// Helper.
		/// </summary>
		public PublishToWebonaryModel SetUpModel()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";

			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			testConfig["Test Config"] = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> {
					new ConfigurableDictionaryNode { FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass"}
				}
			};

			return new PublishToWebonaryModel(m_mediator)
			{
				SiteName = "site",
				UserName = "user",
				Password = "password",
				SelectedPublication = "Test publication",
				SelectedConfiguration = "Test Config",
				Configurations = testConfig
			};
		}

		/// <summary>
		/// Helper.
		/// </summary>
		public PublishToWebonaryController SetUpController()
		{
			return new PublishToWebonaryController { Cache = Cache, Mediator = m_mediator };
		}

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

			public PublishToWebonaryModel Model { get; set; }
		}

		public class MockPublishToWebonaryController : PublishToWebonaryController
		{
			/// <summary>
			/// URI to upload data to.
			/// </summary>
			public string UploadURI { get; set; }

			public MockPublishToWebonaryController() : base()
			{
			}

			internal override string DestinationURI(string siteName)
			{
				return UploadURI ?? "http://192.168.33.10/test/wp-json/webonary/import";
			}
		}
		#endregion
	}
}
