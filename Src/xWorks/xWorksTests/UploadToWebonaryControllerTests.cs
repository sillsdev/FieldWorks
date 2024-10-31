// Copyright (c) 2014-2020 SIL International
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
using SIL.LCModel.Core.Text;
using SIL.IO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
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
		private LcmStyleSheet m_styleSheet;
		private StyleInfoTable m_owningTable;
		private RecordClerk m_Clerk;

		#region Environment
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_propertyTable.SetProperty("AppSettings", new FwApplicationSettings(), false);
			m_propertyTable.SetPropertyPersistence("AppSettings", false);
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
					<clerk id='AllReversalEntries'>
						<recordList owner = 'ReversalIndex' property='AllEntries'>
						<dynamicloaderinfo assemblyPath = 'LexEdDll.dll' class='SIL.FieldWorks.XWorks.LexEd.AllReversalEntriesRecordList'/>
						</recordList>
					</clerk>
				</clerks>
				<tools>
					<tool label='Dictionary' value='lexiconDictionary' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='entries' layout='Bartholomew' layoutProperty='DictionaryPublicationLayout' editable='false' configureObjectName='Dictionary'/>
						</control>
					</tool>
					<tool label='ReversalIndex' value='lexiconReversalIndex' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.RecordEditView'/>
							<parameters area = 'lexicon' clerk = 'AllReversalEntries' layout = 'Normal' treeBarAvailability = 'NotAllowed' emptyTitleId = 'No-ReversalIndexEntries' />
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(reversalIndexClerk);

			XmlNode clerkNode = doc.SelectSingleNode("//tools/tool[@label='Dictionary']//parameters[@area='lexicon']");
			m_Clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);

			clerkNode = doc.SelectSingleNode("//tools/tool[@label='ReversalIndex']//parameters[@area='lexicon']");
			m_Clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);

			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "lexiconDictionary", false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			// setup style sheet and style to allow the css to generate during the UploadToWebonaryController driven export
			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);

			Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(Cache.DefaultAnalWs);

			m_owningTable = new StyleInfoTable("AbbySomebody", Cache.ServiceLocator.WritingSystemManager);
			var fontInfo = new FontInfo();
			var letHeadStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.LetterHeadingStyleName, IsParagraphStyle = false };
			var dictNormStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.DictionaryNormal, IsParagraphStyle = true };
			m_styleSheet.Styles.Add(letHeadStyle);
			m_styleSheet.Styles.Add(dictNormStyle);
			m_owningTable.Add(CssGenerator.LetterHeadingStyleName, letHeadStyle);
			m_owningTable.Add(CssGenerator.DictionaryNormal, dictNormStyle);
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			ConfiguredLcmGenerator.Init();
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

		[TestCase("English", "english")]
		[TestCase(UploadToWebonaryController.WebonaryOrg + "/thAI", "thai")]
		[TestCase("httpS://www.Webonary.org/tPi", "tpi")]
		[TestCase("httpS://www.Webonary.org/tPi/", "tpi")]
		public void NormalizeSiteName(string userEntered, string expected)
		{
			Assert.That(UploadToWebonaryController.NormalizeSiteName(userEntered), Is.EqualTo(expected));
		}

		[Test]
		public void UploadToWebonaryUsesViewConfigAndPub()
		{
			using (var controller = SetUpController())
			{
				var mockView = SetUpView();
				//SUT
				Assert.DoesNotThrow(() => controller.UploadToWebonary(mockView.Model, mockView));
				Assert.That(mockView.StatusStrings.Any(s => s.Contains(mockView.Model.SelectedPublication) && s.Contains(mockView.Model.SelectedConfiguration)),
					string.Concat(mockView.StatusStrings));
			}
		}

		[Test]
		public void UploadToWebonaryExportsXhtmlAndCss()
		{
			using (var controller = SetUpController())
			{
				var mockView = SetUpView();
				var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
				var reversalConfig = new Dictionary<string, DictionaryConfigurationModel>();
				mockView.Model.Configurations = testConfig;
				mockView.Model.Reversals = reversalConfig;
				// Build model sufficient to generate xhtml and css
				ConfiguredLcmGenerator.Init();
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

				var reversalFormNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "ReversalForm",
					//DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] { "en" }),
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
					{
						WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"}
						},
						DisplayWritingSystemAbbreviations = false
					},
					Label = "Reversal Form"
				};
				var reversalEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { reversalFormNode },
					FieldDescription = "ReversalIndexEntry"
				};
				model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { reversalEntryNode } };
				CssGeneratorTests.PopulateFieldsForTesting(model);
				reversalConfig["English"] = model;
				model.Label = "English";
				model.WritingSystem = "en";
				List<string> reversalLanguage = new List<string> { "English" };
				mockView.Model.SelectedReversals = reversalLanguage;

				// create entry sufficient to generate xhtml and css
				var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var entry = factory.Create();
				var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
				entry.CitationForm.set_String(wsFr, TsStringUtils.MakeString("Headword", wsFr));
				//SUT
				Assert.DoesNotThrow(() => controller.UploadToWebonary(mockView.Model, mockView));

				// The names of the files being sent to webonary are listed while logging the zip
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("lexentry.xhtml")), "xhtml not logged as sent: ");
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("configured.css")), "css not logged as sent");
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("Exporting entries for English reversal")), "English reversal not exported");
			}
		}

		/// <summary>
		/// Just give an error if not all the info is supplied rather than crashing.
		/// </summary>
		[Test]
		public void UploadToWebonaryDoesNotCrashWithoutAllItsInfo()
		{
			using (var controller = SetUpController())
			{
				var view = SetUpView();
				var model = view.Model;

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
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wav"));
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.webm"));

			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wmf"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.tif"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.tiff"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.ico"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.pcx"));
			Assert.False(UploadToWebonaryController.IsSupportedWebonaryFile("foo.cgm"));

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
		public void ResetsPropTablesPublicationOnExit()
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

		[Test]
		public void DeleteDictionaryHandles404()
		{
			// Test with an exception which indicates a redirect should happen
			var redirectException = new WebonaryClient.WebonaryException(new WebException("File Not Found"));
			redirectException.StatusCode = HttpStatusCode.NotFound;
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, redirectException, new byte[0], HttpStatusCode.NotFound))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						SiteName = "test-india",
						UserName = "software",
						Password = "4API-testing"
					}
				};
				controller.DeleteContentFromWebonary(view.Model, view, "delete/dictionary");
				Assert.That(!view.StatusStrings.Any(s => s.ToLower().Contains("exception")));
			}
		}

		[Test]
		public void DeleteDictionaryResponseReturnedAsString()
		{
			const string responseString = "Deleted 1000 files";
			var response = Encoding.UTF8.GetBytes(responseString);
			// Test with an exception which indicates a redirect should happen
			using (var controller = new MockUploadToWebonaryController(Cache, m_propertyTable, m_mediator, null, response))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(m_propertyTable)
					{
						SiteName = "test-india",
						UserName = "software",
						Password = "4API-testing"
					}
				};
				var result = controller.DeleteContentFromWebonary(view.Model, view, "delete/dictionary");
				Assert.That(result, Does.Contain(responseString));
			}
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
			ConfiguredLcmGenerator.AssemblyFile = "xWorksTests";

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
				Configurations = testConfig,
				Reversals = new Dictionary<string, DictionaryConfigurationModel>(),
				SelectedReversals = new List<string>(),
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
			public void UpdateStatus(string statusString, WebonaryStatusCondition condition)
			{
				StatusStrings.Add(statusString);
			}

			public void UploadCompleted()
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

			internal override bool UseJsonApi => false;

			/// <summary>
			/// This constructor should be used in tests that will actually hit a server, and are marked [ByHand]
			/// </summary>
			public MockUploadToWebonaryController(LcmCache cache, PropertyTable propertyTable, Mediator mediator)
				: base(cache, propertyTable, mediator)
			{
			}

			/// <summary>
			/// Tests using this constructor do not need to be marked [ByHand]; an exception, response, and response code can all be set.
			/// </summary>
			public MockUploadToWebonaryController(LcmCache cache, PropertyTable propertyTable, Mediator mediator, WebonaryClient.WebonaryException exceptionResponse,
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

				public WebHeaderCollection Headers { get; }

				public byte[] UploadFileToWebonary(string address, string fileName, string method = null)
				{
					return MockByteArrayResponse();
				}

				public HttpStatusCode ResponseStatusCode { get; }

				public string PostDictionaryMetadata(string address, string postBody)
				{
					return MockStringResponse();
				}

				public string PostEntry(string address, string postBody, bool isReversal)
				{
					return MockStringResponse();
				}

				public byte[] DeleteContent(string targetURI)
				{
					return MockByteArrayResponse();
				}

				public string GetSignedUrl(string address, string filePath)
				{
					return MockStringResponse();
				}

				private string MockStringResponse()
				{
					if (_exceptionResponse != null)
						throw _exceptionResponse;
					return Encoding.UTF8.GetString(_responseContents);
				}

				private byte[] MockByteArrayResponse()
				{
					if (_exceptionResponse != null)
						throw _exceptionResponse;
					return _responseContents;
				}
			}

			internal override string DestinationURI(string siteName)
			{
				return UploadURI ?? "http://192.168.33.10/test/wp-json/webonary/import";
			}
		}
		#endregion
	}
}
