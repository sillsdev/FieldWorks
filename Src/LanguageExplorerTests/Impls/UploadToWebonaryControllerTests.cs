// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Ionic.Zip;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Reversals;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using LanguageExplorerTests.DictionaryConfiguration;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Impls
{
	public class UploadToWebonaryControllerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private bool _isDisposed;
		private FlexComponentParameters _flexComponentParameters;
		private LcmStyleSheet _styleSheet;
		private StyleInfoTable _owningStyleInfoTable;
		private IRecordList _entriesRecordList;
		private IRecordList _allReversalEntriesRecordList;
		private StubContentControlProvider _stubContentControlProvider;
		private StatusBar _statusBar;

		#region Environment
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, includeFwApplicationSettings: true);
			var propertyTable = _flexComponentParameters.PropertyTable;
			var reversalIndexRepository = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			reversalIndexRepository.EnsureReversalIndicesExist(Cache, propertyTable);
			FwRegistrySettings.Init();
			_stubContentControlProvider = new StubContentControlProvider();
			_stubContentControlProvider.InitializeFlexComponent(_flexComponentParameters);
			_statusBar = new StatusBar();
			var recordListRepositoryForTools = propertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
			_entriesRecordList = recordListRepositoryForTools.GetRecordList(LanguageExplorerConstants.Entries, _statusBar, LexiconArea.EntriesFactoryMethod);
			recordListRepositoryForTools.ActiveRecordList = _entriesRecordList;
			_allReversalEntriesRecordList = recordListRepositoryForTools.GetRecordList(LanguageExplorerConstants.AllReversalEntries, _statusBar, ReversalServices.AllReversalEntriesFactoryMethod);
			recordListRepositoryForTools.ActiveRecordList = _allReversalEntriesRecordList;
			_flexComponentParameters.PropertyTable.SetProperty($"{AreaServices.ToolForAreaNamed_}_{AreaServices.LexiconAreaMachineName}", AreaServices.LexiconDictionaryMachineName);
			Cache.ProjectId.Path = DictionaryConfigurationServices.TestDataPath;
			// setup style sheet and style to allow the css to generate during the UploadToWebonaryController driven export
			_styleSheet = new LcmStyleSheet();
			_styleSheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
			Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(Cache.DefaultAnalWs);
			_owningStyleInfoTable = new StyleInfoTable("AbbySomebody", Cache.ServiceLocator.WritingSystemManager);
			var fontInfo = new FontInfo();
			var letHeadStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.LetterHeadingStyleName, IsParagraphStyle = false };
			var dictNormStyle = new TestStyle(fontInfo, Cache) { Name = CssGenerator.DictionaryNormal, IsParagraphStyle = true };
			_styleSheet.Styles.Add(letHeadStyle);
			_styleSheet.Styles.Add(dictNormStyle);
			_owningStyleInfoTable.Add(CssGenerator.LetterHeadingStyleName, letHeadStyle);
			_owningStyleInfoTable.Add(CssGenerator.DictionaryNormal, dictNormStyle);
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			try
			{
				ConfiguredXHTMLGenerator.AssemblyFile = "SIL.LCModel";
				Dispose();
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} FixtureTeardown method.", err);
			}
			finally
			{
				base.FixtureTeardown();
			}
		}

		#region disposal
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_statusBar.Dispose();
				_entriesRecordList.Dispose();
				_allReversalEntriesRecordList.Dispose();
				TestSetupServices.DisposeTrash(_flexComponentParameters);
			}
			_statusBar = null;
			_entriesRecordList = null;
			_allReversalEntriesRecordList = null;
			_flexComponentParameters = null;

			_isDisposed = true;
		}

		~UploadToWebonaryControllerTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
				var reversalConfig = new Dictionary<string, DictionaryConfigurationModel>();
				mockView.Model.Configurations = testConfig;
				mockView.Model.Reversals = reversalConfig;
				// Build model sufficient to generate xhtml and css
				ConfiguredXHTMLGenerator.AssemblyFile = "SIL.LCModel";
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
						WsType = WritingSystemType.Reversal,
						Options = new List<DictionaryNodeOption>
						{
							new DictionaryNodeOption {Id = "en"}
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
				var reversalLanguage = new List<string>();
				reversalLanguage.Add("English");
				mockView.Model.SelectedReversals = reversalLanguage;
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
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("reversal_en.xhtml")), "reversal_enxhtml not logged as compressed");
				Assert.That(mockView.StatusStrings.Any(s => s.Contains("Exporting entries for English reversal")), "English reversal not exported");
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

		#region Test connection to local Webonary instance

		/// <summary>
		/// Helper
		/// </summary>
		private static string ConnectAndUpload(WebClient client)
		{
			const string targetURI = "http://192.168.33.10/test/wp-json/webonary/import";
			const string inputFile = "../../Src/LanguageExplorerTests/Works/lubwisi-d-new.zip";
			var response = client.UploadFile(targetURI, inputFile);
			var responseText = Encoding.ASCII.GetString(response);
			return responseText;
		}
		#endregion

		[Test]
		public void UploadToWebonaryThrowsOnNullInput()
		{
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, null, null))
			{
				var view = new MockWebonaryDlg();
				var model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable);
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary(null, model, view));
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", null, view));
				Assert.Throws<ArgumentNullException>(() => controller.UploadToWebonary("notNull", model, null));
			}
		}

		[Test]
		public void UploadToWebonaryReportsFailedAuthentication()
		{
			var responseContents = Encoding.UTF8.GetBytes("Wrong username or password.\nauthentication failed\n");
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, responseContents: responseContents))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
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
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, responseContents: new byte[] { }, responseStatus: HttpStatusCode.Found))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
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
			var redirectException = new WebonaryException(new WebException("Redirected."))
			{
				StatusCode = HttpStatusCode.Redirect
			};
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, redirectException, new byte[] { }))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
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
			var ex = new WebonaryException(new WebException("Unable to connect to Webonary.  Please check your username and password and your Internet connection."))
			{
				StatusCode = HttpStatusCode.BadRequest
			};
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, ex))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
					{
						SiteName = "test-india",
						UserName = "software",
						Password = "4APItesting"
					}
				};
				controller.UploadToWebonary("../../Src/LanguageExplorerTests/Works/lubwisi-d-new.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Unable to connect to Webonary.  Please check your username and password and your Internet connection.")));
			}
		}

		[Test]
		public void UploadToWebonaryReportsSuccess()
		{
			const string success = "Upload successful.";
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, responseContents: Encoding.UTF8.GetBytes(success)))
			{
				var view = new MockWebonaryDlg()
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
					{
						UserName = "webonary",
						Password = "webonary"
					}
				};
				controller.UploadToWebonary("../../Src/LanguageExplorerTests/Works/lubwisi-d-new.zip", view.Model, view);
				Assert.That(view.StatusStrings.Any(s => s.Contains("Upload successful")));
			}
		}

		[Test]
		public void UploadToWebonaryErrorInProcessingHandled()
		{
			var webonaryProcessingErrorContent = Encoding.UTF8.GetBytes("Error processing data: bad data.");
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, responseContents: webonaryProcessingErrorContent))
			{
				var view = new MockWebonaryDlg
				{
					Model = new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
					{
						UserName = "webonary",
						Password = "webonary"
					}
				};
				// Contains a filename in the zip that isn't correct, so no data will be found by webonary.
				controller.UploadToWebonary("fakebaddata.zip", view.Model, view);
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
			Assert.True(UploadToWebonaryController.IsSupportedWebonaryFile("foo.wav"));
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
				var tiffMagicNumber = new byte[] { 0x49, 0x49, 0x2A };
				File.WriteAllBytes(tiffPath, tiffMagicNumber);
				// JPEG
				var jpegFilename = Path.GetFileName(Path.GetTempFileName() + ".jpg");
				var jpegPath = Path.Combine(tempDirectoryToCompress, jpegFilename);
				var jpegMagicNumber = new byte[] { 0xff, 0xd8 };
				File.WriteAllBytes(jpegPath, jpegMagicNumber);
				// MP4
				var mp4Filename = Path.GetFileName(Path.GetTempFileName() + ".mp4");
				var mp4Path = Path.Combine(tempDirectoryToCompress, mp4Filename);
				var mp4MagicNumber = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6d, 0x70, 0x34, 0x32 };
				File.WriteAllBytes(mp4Path, mp4MagicNumber);
				var xhtmlFilename = Path.GetFileName(Path.GetTempFileName() + ".xhtml");
				var xhtmlPath = Path.Combine(tempDirectoryToCompress, xhtmlFilename);
				const string xhtmlContent = "<xhtml/>";
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
				Assert.True(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count == 1), "Lack of support for the tiff file should have been reported to the user.");
				query = string.Format(unsupportedRegex, jpegFilename);
				Assert.False(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count == 1), "Should not have reported lack of support for the jpeg file.");
				query = string.Format(unsupportedRegex, mp4Filename);
				Assert.False(view.StatusStrings.Exists(statusString => Regex.Matches(statusString, query).Count == 1), "Should not have reported lack of support for the mp4 file.");
				Assert.That(view.StatusStrings.Count(statusString => Regex.Matches(statusString, unsupported).Count > 0), Is.EqualTo(1), "Too many unsupported files reported.");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(tempDirectoryToCompress);
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
			const string expectedFilename = "mySiteName.zip";
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
			var originalPub = _flexComponentParameters.PropertyTable.GetValue("SelectedPublication", "Main Dictionary");
			_flexComponentParameters.PropertyTable.SetProperty("SelectedPublication", originalPub, false); // just in case we fell back on the default
			using (var controller = new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar))
			{
				controller.ActivatePublication("Wiktionary");
				Assert.AreEqual("Wiktionary", _flexComponentParameters.PropertyTable.GetValue<string>("SelectedPublication", null), "Didn't activate temp publication");
			}
			Assert.AreEqual("Main Dictionary", _flexComponentParameters.PropertyTable.GetValue<string>("SelectedPublication", null), "Didn't reset publication");
		}

		#region Helpers
		/// <summary/>
		private MockWebonaryDlg SetUpView()
		{
			return new MockWebonaryDlg
			{
				Model = SetUpModel()
			};
		}

		/// <summary>
		/// Helper.
		/// </summary>
		public UploadToWebonaryModel SetUpModel()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var testConfig = new Dictionary<string, DictionaryConfigurationModel>();
			testConfig["Test Config"] = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						FieldDescription = "LanguageExplorerTests.DictionaryConfiguration.TestRootClass"
					}
				}
			};

			return new UploadToWebonaryModel(_flexComponentParameters.PropertyTable)
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
			return new MockUploadToWebonaryController(Cache, _flexComponentParameters.PropertyTable, _flexComponentParameters.Publisher, _statusBar, responseContents: Encoding.UTF8.GetBytes("Upload successful"));
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

		private sealed class MockUploadToWebonaryController : UploadToWebonaryController
		{
			/// <summary>
			/// URI to upload data to.
			/// </summary>
			private string UploadURI { get; set; }

			/// <summary>
			/// Tests using this constructor do not need to be marked [ByHand]; an exception, response, and response code can all be set.
			/// </summary>
			internal MockUploadToWebonaryController(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, StatusBar statusBar, WebonaryException exceptionResponse = null,
				byte[] responseContents = null, HttpStatusCode responseStatus = HttpStatusCode.OK) : base(cache, propertyTable, publisher, statusBar)
			{
				CreateWebClient = () => new MockWebonaryClient(exceptionResponse, responseContents, responseStatus);
			}

			/// <summary>
			/// Fake web client to allow unit testing of controller code without needing to connect to a server
			/// </summary>
			private sealed class MockWebonaryClient : IWebonaryClient
			{
				private readonly WebonaryException _exceptionResponse;
				private readonly byte[] _responseContents;

				internal MockWebonaryClient(WebonaryException exceptionResponse, byte[] responseContents, HttpStatusCode responseStatus)
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

				private void Dispose(bool disposing)
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
					{
						throw _exceptionResponse;
					}
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

		/// <summary>
		/// This class is for use in unit tests that need to use the ActivateClerk functionality. For instance to imitate switching
		/// tools for testing export functionality.
		/// </summary>
		/// <remarks>To use add the following to your TestFixtureSetup: m_mediator.AddColleague(new StubContentControlProvider());</remarks>
		private sealed class StubContentControlProvider : IFlexComponent
		{
			private const string m_contentControlDictionary =
				@"<control>
					<parameters PaneBarGroupId='PaneBar_Dictionary'>
						<control>
							<parameters area='lexicon' clerk='entries' />
						</control>
						<!-- The following configureLayouts node is only required to help migrate old configurations to the new format -->
						<configureLayouts>
							<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'>
								<configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' />
							</layoutType>
							<layoutType label='Root-based (complex forms as subentries)' layout='publishRoot'>
								<configure class='LexEntry' label='Main Entry' layout='publishRootEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishRootMinorEntry' hideConfig='true' />
							</layoutType>
						</configureLayouts>
					</parameters>
				</control>";
			private readonly XmlNode m_testControlDictNode;

			private const string m_contentControlReversal =
				@"<control>
					<parameters id='reversalIndexEntryList' PaneBarGroupId='PaneBar-ReversalIndicesMenu'>
						<control>
							<parameters area='lexicon' clerk='AllReversalEntries' />
						</control>
						<configureLayouts>
							<layoutType label='All Reversal Indexes' layout='publishReversal'>
								<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalIndexEntry' />
							</layoutType>
							<layoutType label='$wsName' layout='publishReversal-$ws'>
								<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalIndexEntry-$ws' />
							</layoutType>
						</configureLayouts>
					</parameters>
				</control>";
			private readonly XmlNode m_testControlRevNode;

			public StubContentControlProvider()
			{
				var doc = new XmlDocument();
				doc.LoadXml(m_contentControlDictionary);
				m_testControlDictNode = doc.DocumentElement;
				var reversalDoc = new XmlDocument();
				reversalDoc.LoadXml(m_contentControlReversal);
				m_testControlRevNode = reversalDoc.DocumentElement;
			}

			/// <summary>
			/// This is called by reflection through the mediator. We need so that we can migrate through the PreHistoricMigrator.
			/// </summary>
			// ReSharper disable once UnusedMember.Local
			private bool OnGetContentControlParameters(object parameterObj)
			{
				var param = parameterObj as Tuple<string, string, XmlNode[]>;
				if (param == null)
				{
					return false;
				}
				var result = param.Item3;
				Assert.That(param.Item2 == "lexiconDictionary" || param.Item2 == "reversalToolEditComplete", "No params for tool: " + param.Item2);
				result[0] = param.Item2 == "lexiconDictionary" ? m_testControlDictNode : m_testControlRevNode;
				return true;
			}

			public IPropertyTable PropertyTable { get; private set; }
			public IPublisher Publisher { get; private set; }
			public ISubscriber Subscriber { get; private set; }
			public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

				PropertyTable = flexComponentParameters.PropertyTable;
				Publisher = flexComponentParameters.Publisher;
				Subscriber = flexComponentParameters.Subscriber;
			}
		}
	}
}