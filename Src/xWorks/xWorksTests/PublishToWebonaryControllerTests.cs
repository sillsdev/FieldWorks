// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;
using NUnit.Framework;
using Palaso.IO;
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
		private PropertyTable m_propertyTable;
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
			m_propertyTable = m_window.PropTable;
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
			m_Clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);
			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "lexiconDictionary", false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			// setup style sheet and style to allow the css to generate during the PublishToWebonaryController driven export
			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			m_owningTable = new StyleInfoTable("AbbySomebody", (IWritingSystemManager)Cache.WritingSystemFactory);
			var fontInfo = new FontInfo();
			var style = new TestStyle(fontInfo, Cache) { Name = CssGenerator.LetterHeadingStyleName, IsParagraphStyle = false };
			m_styleSheet.Styles.Add(style);
			m_owningTable.Add(CssGenerator.LetterHeadingStyleName, style);
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
				if(m_propertyTable != null)
					m_propertyTable.Dispose();
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
			var controller = new PublishToWebonaryController { Cache = Cache, PropertyTable = m_propertyTable };
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
			var model = new PublishToWebonaryModel(m_propertyTable);
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
				Model = new PublishToWebonaryModel(m_propertyTable)
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
				Model = new PublishToWebonaryModel(m_propertyTable)
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
				Model = new PublishToWebonaryModel(m_propertyTable)
				{
					UserName = "webonary",
					Password = "webonary"
				}
			};
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
			//view.StatusStrings.ForEach(Console.WriteLine); // Debugging output
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.Contains("Upload successful"))));
		}

		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryReportsErrorsInProcessingData()
		{
			var controller = new MockPublishToWebonaryController();
			var view = new MockWebonaryDlg()
			{
				Model = new PublishToWebonaryModel(m_propertyTable)
				{
					UserName = "webonary",
					Password = "webonary"
				}
			};
			// Contains a filename in the zip that isn't correct, so no data will be found by webonary.
			controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new-bad.zip", view.Model, view);
			//view.StatusStrings.ForEach(Console.WriteLine); // Debugging output
			Assert.That(!String.IsNullOrEmpty(view.StatusStrings.Find(s => s.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)), "Should be an error reported");
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

		[Test]
		public void IsSupportedWebonaryFile_reportsAccurately()
		{
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.xhtml"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.css"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.html"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.htm"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.json"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.xml"));

			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.jpg"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.jpeg"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.gif"));
			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.png"));

			Assert.True(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mp3"));

			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.wmf"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.tif"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.tiff"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.ico"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.pcx"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.cgm"));

			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.wav"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.snd"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.au"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.aif"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.aifc"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.wma"));

			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.avi"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.wmv"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.wvx"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mpg"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mpe"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.m1v"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mp2"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mpv2"));
			Assert.False(PublishToWebonaryController.IsSupportedWebonaryFile("foo.mpa"));
		}

		[Test]
		public void CompressExportedFiles_IncludesAcceptableMediaTypes()
		{
			var view = new MockWebonaryDlg();

			var tempDirectoryToCompress = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectoryToCompress);
			try
			{
				var zipFileToUpload = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

				// TIFF
				var tiffFilename = Path.GetFileName(Path.GetTempFileName() + ".tif");
				var tiffPath = Path.Combine(tempDirectoryToCompress, tiffFilename);
				var tiffMagicNumber = new byte[] {0x49, 0x49, 0x2A};
				File.WriteAllBytes(tiffPath, tiffMagicNumber);

				// JPEG
				var jpegFilename = Path.GetFileName(Path.GetTempFileName() + ".jpg");
				var jpegPath = Path.Combine(tempDirectoryToCompress, jpegFilename);
				var jpegMagicNumber = new byte[] {0xff, 0xd8};
				File.WriteAllBytes(jpegPath, jpegMagicNumber);

				var xhtmlFilename = Path.GetFileName(Path.GetTempFileName() + ".xhtml");
				var xhtmlPath = Path.Combine(tempDirectoryToCompress, xhtmlFilename);
				var xhtmlContent = "<xhtml/>";
				File.WriteAllText(xhtmlPath, xhtmlContent);

				// SUT
				PublishToWebonaryController.CompressExportedFiles(tempDirectoryToCompress, zipFileToUpload, view);

				using (var uploadZip = new ZipFile(zipFileToUpload))
				{
					Assert.False(uploadZip.EntryFileNames.Contains(tiffFilename), "Should not have included unsupported TIFF file in file to upload.");
					Assert.True(uploadZip.EntryFileNames.Contains(jpegFilename), "Should have included supported JPEG file in file to upload.");
				}

				var query = string.Format(".*{0}.*nsupported.*", tiffFilename);
				Assert.True(view.StatusStrings.Exists((statusString) => Regex.Matches(statusString, query).Count==1), "Lack of support for the tiff file should have been reported to the user.");
				query = string.Format(".*{0}.*nsupported.*", jpegFilename);
				Assert.False(view.StatusStrings.Exists((statusString) => Regex.Matches(statusString, query).Count==1), "Should not have reported lack of support for the jpeg file.");

				Assert.That(view.StatusStrings.Count(statusString => Regex.Matches(statusString, ".*nsupported.*").Count > 0), Is.EqualTo(1), "Too many unsupported files reported.");
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempDirectoryToCompress);
			}
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

			return new PublishToWebonaryModel(m_propertyTable)
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
			return new PublishToWebonaryController { Cache = Cache, PropertyTable = m_propertyTable };
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
