// Copyright (c) 2014-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.IO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators;
using XCore;
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	public class UploadToWebonaryControllerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
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
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_mediator.AddColleague(new StubContentControlProvider());
			m_window.LoadUI(configFilePath);
			// set up clerk to allow DictionaryPublicationDecorator to be created during the UploadToWebonaryController driven export
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
			// setup style sheet and style to allow the css to generate during the UploadToWebonaryController driven export
			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			m_owningTable = new StyleInfoTable("AbbySomebody", Cache.ServiceLocator.WritingSystemManager);
			var fontInfo = new FontInfo();
			var letHeadStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.LetterHeadingStyleName, IsParagraphStyle = false };
			var dictNormStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.DictionaryNormal, IsParagraphStyle = true };
			m_styleSheet.Styles.Add(letHeadStyle);
			m_styleSheet.Styles.Add(dictNormStyle);
			m_owningTable.Add(CssGenerator.LetterHeadingStyleName, letHeadStyle);
			m_owningTable.Add(CssGenerator.DictionaryNormal, dictNormStyle);
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
			base.FixtureTeardown();
			Dispose();
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_Clerk?.Dispose();
				m_application?.Dispose();
				m_window?.Dispose();
				m_mediator?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~UploadToWebonaryControllerTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion disposal
		#endregion Environment

		[Test]
		public void UploadToWebonaryUsesViewConfigAndPub()
		{
			using (var controller = SetUpController())
			{
				var mockView = SetUpView();
				//SUT
				Assert.DoesNotThrow(() => controller.UploadToWebonary(mockView.Model, mockView));
				Assert.That(mockView.StatusStrings.Any(s => s.Contains(mockView.Model.SelectedPublication) && s.Contains(mockView.Model.SelectedConfiguration)));
			}
		}

		[Test]
		public void UploadToWebonaryExportsXhtmlAndCss()
		{
			using (var controller = SetUpController())
			{
				var mockView = SetUpView();
				var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
				mockView.Model.Configurations = testConfig;
				// Build model sufficient to generate xhtml and css
				ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
				var mainHeadwordNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "HeadWord",
					CSSClassNameOverride = "entry",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions { Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { "fr" }) },
					Before = "MainEntry: ",
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
					FieldDescription = "LexEntry",
				};
				var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
				CssGeneratorTests.PopulateFieldsForTesting(model);
				testConfig["Test Config"] = model;
				// create entry sufficient to generate xhtml and css
				var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var entry = factory.Create();
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				entry.CitationForm.set_String(wsFr, TsStringUtils.MakeString("Headword", wsFr));
				//SUT
				Assert.DoesNotThrow(() => controller.UploadToWebonary(mockView.Model, mockView));

				// The names of the files being sent to webonary are listed while logging the zip
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("configured.xhtml")), "xhtml not logged as compressed");
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("configured.css")), "css not logged as compressed");
			}
		}

		/// <summary>
		/// Just give an error if not all the info is supplied rather than crashing.
		/// </summary>
		[Test]
		[Category("ByHand")] // ByHand since uses local webonary instance
		public void UploadToWebonaryDoesNotCrashWithoutAllItsInfo()
		{
			using (var controller = SetUpController())
			{
				var view = SetUpView();
				var model = view.Model;

				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
				Assert.That(view.StatusStrings.Any(s => s.Contains("Uploading")), "Inform that the process has started");
				model.SiteName = null;
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
				model.SiteName = "site";
				model.UserName = null;
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
				model.UserName = "user";
				model.Password = null;
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
				model.Password = "password";
				model.SelectedPublication = null;
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
				model.SelectedPublication = "Test publication";
				model.SelectedConfiguration = null;
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, view));
			}
		}

		[Test]
		[Ignore("Until get working. Doesn't seem to put together the right kind of data to not get an error yet.")]
		public void UploadToWebonaryCanCompleteWithoutError()
		{
			using (var controller = SetUpController())
			{
				var mockView = SetUpView();
				var model = mockView.Model;
				model.UserName = "webonary";
				model.Password = "webonary";
				//SUT
				Assert.DoesNotThrow(() => controller.UploadToWebonary(model, mockView));
				mockView.StatusStrings.ForEach(Console.WriteLine); // Remove this output line once this test works.
				Assert.That(String.IsNullOrEmpty(mockView.StatusStrings.Find(s => s.Contains("Error") || s.Contains("ERROR") || s.Contains("error"))));
			}
		}

		#region Test connection to local Webonary instance
		[Test]
		[Category("ByHand")]
		[Ignore("Used for manual testing against a real Webonary instance")]
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
		[Category("ByHand")]
		[Ignore("Used for manual testing against a real Webonary instance")]
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
		[Category("ByHand")]
		[Ignore("Used for manual testing against a real Webonary instance")]
		public void ZipFileExtracts()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("webonary:webonary")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.StringContaining("extracted successfully"));
			}
		}

		[Test]
		[Category("ByHand")]
		[Ignore("Used for manual testing against a real Webonary instance")]
		public void RealUploadWithBadDataReportsErrorInProcessing()
		{
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator))
			{
				var view = new MockWebonaryDlg
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						UserName = "webonary",
						Password = "webonary"
					}
				};
				// Contains a filename in the zip that isn't correct, so no data will be found by webonary.
				controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new-bad.zip", view.Model, view);
				//view.StatusStrings.ForEach(Console.WriteLine); // Debugging output
				Assert.That(view.StatusStrings.Any(s => s.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0), "Should be an error reported");
			}
		}

		/// <summary>
		/// Does not crash. Reports error in upload.
		/// Marked ByHand since I don't want the build servers poking around on
		/// places on the network like this, and it also takes a few minutes to timeout.
		/// </summary>
		[Test]
		[Category("ByHand")]
		[Ignore("Takes too long to timeout. Enable if want to test.")]
		public void RealUploadToWebonaryHandlesNetworkErrors()
		{
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator))
			{
				var view = new MockWebonaryDlg();
				var filepath = "../../Src/xWorks/xWorksTests/lubwisi-d-new.zip";

				controller.UploadURI = "http://nameresolutionfailure.local/import.php";
				Assert.DoesNotThrow(() => controller.UploadToWebonary(filepath, view.Model, view));
				Assert.That(view.StatusStrings.Any(s => s.Contains("An error occurred uploading your data:")));
				controller.UploadURI = "http://localhost:12345/import/connectfailure.php";
				Assert.DoesNotThrow(() => controller.UploadToWebonary(filepath, view.Model, view));
				Assert.That(view.StatusStrings.Any(s => s.Contains("An error occurred uploading your data:")));
				controller.UploadURI = "http://192.168.0.1/import/requesttimedout.php";
				Assert.DoesNotThrow(() => controller.UploadToWebonary(filepath, view.Model, view));
				Assert.That(view.StatusStrings.Any(s => s.Contains("An error occurred uploading your data:")));
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
			var responseText = Encoding.ASCII.GetString(response);
			return responseText;
		}
		#endregion

		[Test]
		public void UploadToWebonaryThrowsOnNullInput()
		{
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, null))
			{
				var view = new MockWebonaryDlg();
				var model = new UploadToWebonaryModel(m_propertyTable);
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary(null, model, view));
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", null, view));
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", model, null));
			}
		}

		[Test]
		public void UploadToWebonaryReportsFailedAuthentication()
		{
			var responseText = Encoding.UTF8.GetBytes("Wrong username or password.\nauthentication failed\n");
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, responseText))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						UserName = "nouser",
						Password = "nopassword"
					}
				};
				controller.UploadToWebonary("Fakefile.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Error: Wrong username or password")));
			}
		}

		/// <summary>
		/// The webonary server has an automatic redirection for non-existant sites. This tests both ways that information can be returned.
		/// </summary>
		[Test]
		public void UploadToWebonaryReportsIncorrectSiteName()
		{
			// Test for a successful response indicating that a redirect should happen
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, new byte[] {}, HttpStatusCode.Found))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						SiteName = "test",
						UserName = "software",
						Password = "4APItesting"
					}
				};
				controller.UploadToWebonary("fakefile.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Error: There has been an error accessing webonary. Is your sitename correct?")));
			}

			// Test with an exception which indicates a redirect should happen
			var redirectException = new WebonaryClient.WebonaryException(new WebException("Redirected."));
			redirectException.StatusCode = HttpStatusCode.Redirect;
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, redirectException, new byte[] { }))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						SiteName = "test",
						UserName = "software",
						Password = "4APItesting"
					}
				};
				controller.UploadToWebonary("fakefile.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Error: There has been an error accessing webonary. Is your sitename correct?")));
			}
		}

		[Test]
		public void UploadToWebonaryReportsLackingPermissionsToUpload()
		{
			var ex = new WebonaryClient.WebonaryException(new WebException("Unable to connect to Webonary.  Please check your username and password and your Internet connection."));
			ex.StatusCode = HttpStatusCode.BadRequest;
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, ex, null))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						SiteName = "test-india",
						UserName = "software",
						Password = "4APItesting"
					}
				};
				controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Unable to connect to Webonary.  Please check your username and password and your Internet connection.")));
			}
		}

		[Test]
		public void UploadToWebonaryReportsSuccess()
		{
			var success = "Upload successful.";
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, Encoding.UTF8.GetBytes(success)))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						UserName = "webonary",
						Password = "webonary"
					}
				};
				controller.UploadToWebonary("../../Src/xWorks/xWorksTests/lubwisi-d-new.zip", view.Model, view);
				//view.StatusStrings.ForEach(Console.WriteLine); // Debugging output
				Assert.That(view.StatusStrings.Any(s => s.Contains("Upload successful")));
			}
		}

		[Test]
		public void UploadToWebonaryErrorInProcessingHandled()
		{
			var webonaryProcessingErrorContent = Encoding.UTF8.GetBytes("Error processing data: bad data.");
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, webonaryProcessingErrorContent))
			{
				var view = new MockWebonaryDlg
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						UserName = "webonary",
						Password = "webonary"
					}
				};
				// Contains a filename in the zip that isn't correct, so no data will be found by webonary.
				controller.UploadToWebonary("fakebaddata.zip", view.Model, view);
				//view.StatusStrings.ForEach(Console.WriteLine); // Debugging output
				Assert.That(view.StatusStrings.Any(s => s.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0), "Should be an error reported");
			}
		}

		[Test]
		public void IsSupportedWebonaryFile_reportsAccurately()
		{
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.xhtml"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.css"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.html"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.htm"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.json"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.xml"));

			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.jpg"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.jpeg"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.gif"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.png"));

			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mp3"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.MP4")); // avoid failure because of capitalization

			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wmf"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.tif"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.tiff"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.ico"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.pcx"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.cgm"));

			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wav"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.snd"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.au"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.aif"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.aifc"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wma"));

			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.avi"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wmv"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wvx"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mpg"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mpe"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.m1v"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mp2"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mpv2"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.mpa"));
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

				// MP4
				var mp4Filename = Path.GetFileName(Path.GetTempFileName() + ".mp4");
				var mp4Path = Path.Combine(tempDirectoryToCompress, mp4Filename);
				var mp4MagicNumber = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6d, 0x70, 0x34, 0x32 };
				File.WriteAllBytes(mp4Path, mp4MagicNumber);

				var xhtmlFilename = Path.GetFileName(Path.GetTempFileName() + ".xhtml");
				var xhtmlPath = Path.Combine(tempDirectoryToCompress, xhtmlFilename);
				var xhtmlContent = "<xhtml/>";
				File.WriteAllText(xhtmlPath, xhtmlContent);

				// SUT
				UploadToWebonaryController.CompressExportedFiles(tempDirectoryToCompress, zipFileToUpload, view);

				// Verification
				const string unsupported = ".*nsupported.*";
				const string unsupportedRegex = ".*{0}" + unsupported;
				using (var uploadZip = new ZipFile(zipFileToUpload))
				{
					Assert.False(uploadZip.EntryFileNames.Contains(tiffFilename), "Should not have included unsupported TIFF file in file to upload.");
					Assert.True(uploadZip.EntryFileNames.Contains(jpegFilename), "Should have included supported JPEG file in file to upload.");
					Assert.True(uploadZip.EntryFileNames.Contains(mp4Filename), "Should have included supported MP4 file in file to upload.");
				}

				var query = string.Format(unsupportedRegex, tiffFilename);
				Assert.True(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count==1), "Lack of support for the tiff file should have been reported to the user.");
				query = string.Format(unsupportedRegex, jpegFilename);
				Assert.False(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count==1), "Should not have reported lack of support for the jpeg file.");
				query = string.Format(unsupportedRegex, mp4Filename);
				Assert.False(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count == 1), "Should not have reported lack of support for the mp4 file.");

				Assert.That(view.StatusStrings.Count(statusString => Regex.Matches(statusString, unsupported).Count > 0), Is.EqualTo(1), "Too many unsupported files reported.");
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempDirectoryToCompress);
			}
		}

		/// <summary>
		/// LT-17149.
		/// </summary>
		[Test]
		public void UploadFilename_UsesSiteName()
		{
			var view = SetUpView();
			var model = view.Model;
			model.SiteName = "mySiteName";
			var expectedFilename = "mySiteName.zip";
			var actualFilename = UploadToWebonaryController.UploadFilename(model, view);
			Assert.That(actualFilename, Is.EqualTo(expectedFilename), "Incorrect filename for webonary export.");
		}

		[Test]
		public void UploadFilename_ThrowsForBadInput()
		{
			Assert.Throws<ArgumentNullException>(() => UploadToWebonaryController.UploadFilename(null, null));
			var view = SetUpView();
			var model = view.Model;
			model.SiteName = null;
			Assert.Throws<ArgumentException>(() => UploadToWebonaryController.UploadFilename(model, view));
			model.SiteName = "";
			Assert.Throws<ArgumentException>(() => UploadToWebonaryController.UploadFilename(model, view));
		}

		[TestCase("my.Site")]
		[TestCase("my Site")]
		[TestCase("my$Site")]
		[TestCase("my%Site")]
		[TestCase("my_Site")]
		[TestCase("my*Site")]
		[TestCase("my/Site")]
		[TestCase("my:Site")]
		public void UploadFilename_FailsForInvalidCharactersInSitename(string sitename)
		{
			var view = SetUpView();
			var model = view.Model;
			model.SiteName = sitename;

			// SUT
			var result = UploadToWebonaryController.UploadFilename(model, view);

			Assert.That(result, Is.Null, "Fail on invalid characters.");
			Assert.That(view.StatusStrings.Any(s => s.Contains("Invalid characters found in sitename")), "Inform that there was a problem");
		}

		[Test]
		public void ResetsProptablesPublicationOnExit()
		{
			var originalPub = m_propertyTable.GetStringProperty("SelectedPublication", "Main Dictionary");
			m_propertyTable.SetProperty("SelectedPublication", originalPub, false); // just in case we fell back on the default
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator))
			{
				controller.ActivatePublication("Wiktionary");
				Assert.AreEqual("Wiktionary", m_propertyTable.GetStringProperty("SelectedPublication", null), "Didn't activate temp publication");
			}
			Assert.AreEqual("Main Dictionary", m_propertyTable.GetStringProperty("SelectedPublication", null), "Didn't reset publication");
		}

		#region Helpers
		/// <summary/>
		private MockWebonaryDlg SetUpView()
		{
			return new MockWebonaryDlg {
				Model = SetUpModel()
			};
		}

		/// <summary>
		/// Helper.
		/// </summary>
		public UploadToWebonaryModel SetUpModel()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";

			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			testConfig["Test Config"] = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> {
					new ConfigurableDictionaryNode { FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass"}
				}
			};

			return new UploadToWebonaryModel(m_propertyTable)
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
		public UploadToWebonaryController SetUpController()
		{
			return new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, Encoding.UTF8.GetBytes("Upload successful"));
		}

		internal class MockWebonaryDlg : IUploadToWebonaryView
		{
			// Collect the status messages that are generated during the export
			public List<string> StatusStrings = new List<string>();
			public void UpdateStatus(string statusString)
			{
				StatusStrings.Add(statusString);
			}

			public void SetStatusCondition(WebonaryStatusCondition condition)
			{
			}

			public void PopulatePublicationsList(IEnumerable<string> publications)
			{
			}

			public void PopulateConfigurationsList(IEnumerable<string> configurations)
			{
			}

			public void PopulateReversalsCheckboxList(IEnumerable<string> reversals)
			{
			}

			public UploadToWebonaryModel Model { get; set; }
		}

		public class MockUploadToWebonaryController : UploadToWebonaryController
		{
			/// <summary>
			/// URI to upload data to.
			/// </summary>
			public string UploadURI { get; set; }

			/// <summary>
			/// This constructor should be used in tests that will actually hit a server, and are marked [ByHand]
			/// </summary>
			public MockUploadToWebonaryController(FdoCache cache, PropertyTable propertyTable, Mediator mediator)
				: base(cache, propertyTable, mediator)
			{
			}

			/// <summary>
			/// Tests using this constructor do not need to be marked [ByHand]; an exception, response, and response code can all be set.
			/// </summary>
			public MockUploadToWebonaryController(FdoCache cache, PropertyTable propertyTable, Mediator mediator, WebonaryClient.WebonaryException exceptionResponse,
				byte[] responseContents, HttpStatusCode responseStatus = HttpStatusCode.OK) : base(cache, propertyTable, mediator)
			{
				CreateWebClient = () => new MockWebonaryClient(exceptionResponse, responseContents, responseStatus);
			}

			/// <summary>
			/// Fake web client to allow unit testing of controller code without needing to connect to a server
			/// </summary>
			public class MockWebonaryClient : IWebonaryClient
			{
				private readonly WebonaryClient.WebonaryException _exceptionResponse;
				private readonly byte[] _responseContents;

				public MockWebonaryClient(WebonaryClient.WebonaryException exceptionResponse, byte[] responseContents, HttpStatusCode responseStatus)
				{
					_exceptionResponse = exceptionResponse;
					_responseContents = responseContents;
					Headers = new WebHeaderCollection();
					ResponseStatusCode = responseStatus;
				}

				public void Dispose()
				{
					Dispose(true);
					GC.SuppressFinalize(this);
				}

				protected virtual void Dispose(bool disposing)
				{
					System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				}

				~MockWebonaryClient()
				{
					Dispose(false);
				}

				public WebHeaderCollection Headers { get; private set; }
				public byte[] UploadFileToWebonary(string address, string fileName)
				{
					if (_exceptionResponse != null)
						throw _exceptionResponse;
					return _responseContents;
				}

				public HttpStatusCode ResponseStatusCode { get; private set; }
			}

			internal override string DestinationURI(string siteName)
			{
				return UploadURI ?? "http://192.168.33.10/test/wp-json/webonary/import";
			}
		}
		#endregion
	}
}
