// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Windows.Forms;
using Ionic.Zip;
using LanguageExplorer.DictionaryConfiguration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Currently serves as the controller and the model for the UploadToWebonaryView
	/// </summary>
	public class UploadToWebonaryController : IDisposable
	{
		private readonly LcmCache _cache;
		private readonly DictionaryExportService _exportService;
		private PublicationActivator _publicationActivator;
		private bool _isDisposed;

		/// <summary>
		/// This action creates the WebClient for accessing webonary. Protected to enable a mock client for unit testing.
		/// </summary>
		protected Func<IWebonaryClient> CreateWebClient = () => new WebonaryClient { Encoding = Encoding.UTF8 };

		public IPropertyTable PropertyTable { get; }

		public UploadToWebonaryController(LcmCache cache, IPropertyTable propertyTable, StatusBar statusBar)
		{
			_cache = cache;
			PropertyTable = propertyTable;
			_exportService = new DictionaryExportService(cache, PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList, propertyTable, statusBar);
			_publicationActivator = new PublicationActivator(propertyTable);
		}

		public bool IsSortingOnAlphaHeaders => PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList.IsSortingByHeadword;

		#region Disposal
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_publicationActivator?.Dispose();
			}
			_publicationActivator = null;
			_isDisposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~UploadToWebonaryController()
		{
			Dispose(false);
		}
		#endregion Disposal

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			return _exportService.CountDictionaryEntries(config);
		}

		/// <summary>
		/// Table of reversal indexes and their counts.
		/// </summary>
		public SortedDictionary<string, int> GetCountsOfReversalIndexes(IEnumerable<string> requestedIndexes)
		{
			return _exportService.GetCountsOfReversalIndexes(requestedIndexes);
		}

		public void ActivatePublication(string publication)
		{
			_publicationActivator.ActivatePublication(publication);
		}

		/// <summary>
		/// Exports the dictionary xhtml and css for the publication and configuration that the user had selected in the dialog.
		/// </summary>
		private void ExportDictionaryContent(string tempDirectoryToCompress, UploadToWebonaryModel model, IUploadToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ExportingEntriesToWebonary, model.SelectedPublication, model.SelectedConfiguration));
			var xhtmlPath = Path.Combine(tempDirectoryToCompress, "configured.xhtml");
			var configuration = model.Configurations[model.SelectedConfiguration];
			_exportService.ExportDictionaryContent(xhtmlPath, configuration);
			webonaryView.UpdateStatus(LanguageExplorerResources.ExportingEntriesToWebonaryCompleted);
		}

		private JObject GenerateDictionaryMetadataContent(UploadToWebonaryModel model,
			IEnumerable<string> templateFileNames, string tempDirectoryForExport)
		{
			return _publicationActivator.ExportDictionaryContentJson(model.SiteName, templateFileNames,
				model.Reversals.Where(kvp => model.SelectedReversals.Contains(kvp.Key)).Select(kvp => kvp.Value),
				model.Configurations[model.SelectedConfiguration].FilePath,
				tempDirectoryForExport);
		}

		internal static void CompressExportedFiles(string tempDirectoryToCompress, string zipFileToUpload, IUploadToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(LanguageExplorerResources.BeginCompressingDataForWebonary);
			using(var zipFile = new ZipFile(Encoding.UTF8))
			{
				RecursivelyAddFilesToZip(zipFile, tempDirectoryToCompress, "", webonaryView);
				zipFile.Save(zipFileToUpload);
			}
			webonaryView.UpdateStatus(LanguageExplorerResources.FinishedCompressingDataForWebonary);
		}

		/// <summary>
		/// This method will recurse into a directory and add files into the zip file with their relative path
		/// to the original dirToCompress.
		/// </summary>
		private static void RecursivelyAddFilesToZip(ZipFile zipFile, string dirToCompress, string dirInZip, IUploadToWebonaryView webonaryView)
		{
			foreach (var file in Directory.EnumerateFiles(dirToCompress))
			{
				if (!IsSupportedWebonaryFile(file))
				{
					webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ksExcludingXXFormatUnsupported, Path.GetFileName(file), Path.GetExtension(file)));
					continue;
				}
				zipFile.AddFile(file, dirInZip);
				webonaryView.UpdateStatus(Path.GetFileName(file));
			}
			foreach (var dir in Directory.EnumerateDirectories(dirToCompress))
			{
				RecursivelyAddFilesToZip(zipFile, dir, Path.Combine(dirInZip, Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar))), webonaryView);
			}
		}

		/// <summary>
		/// This method will recurse into a directory and add files into the zip file with their relative path
		/// to the original dirToUpload.
		/// </summary>
		private bool RecursivelyPutFilesToWebonary(UploadToWebonaryModel model, string dirToUpload, IUploadToWebonaryView webonaryView, string subFolder = "")
		{
			bool allFilesSucceeded = true;
			foreach (var file in Directory.EnumerateFiles(dirToUpload))
			{
				if (!IsSupportedWebonaryFile(file))
				{
					webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ksExcludingXXFormatUnsupported,
						Path.GetFileName(file), Path.GetExtension(file)));
					continue;
				}
				dynamic fileToSign = new JObject();
				// ReSharper disable once AssignNullToNotNullAttribute - This file has a filename, the OS told us so.
				var relativeFilePath = Path.Combine(model.SiteName, subFolder, Path.GetFileName(file));
				if (MiscUtils.IsWindows)
				{
					relativeFilePath = relativeFilePath.Replace('\\', '/');
				}
				fileToSign.objectId = relativeFilePath;
				fileToSign.action = "putObject";
				var signedUrl = PostContentToWebonary(model, webonaryView, "post/file", fileToSign);
				if (signedUrl == null)
				{
					webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ksPutFilesToWebonaryFailed, relativeFilePath));
					return false;
				}
				allFilesSucceeded &= UploadFileToWebonary(signedUrl, file, webonaryView);
				webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ksPutFilesToWebonaryUploaded, Path.GetFileName(file)));
			}
			foreach (var dir in Directory.EnumerateDirectories(dirToUpload))
			{
				allFilesSucceeded &= RecursivelyPutFilesToWebonary(model, dir, webonaryView, Path.Combine(subFolder, Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar))));
			}

			return allFilesSucceeded;
		}

		/// <summary>
		/// Exports the reversal xhtml and css for the reversals that the user had selected in the dialog
		/// </summary>
		private void ExportReversalContent(string tempDirectoryToCompress, UploadToWebonaryModel model, IUploadToWebonaryView webonaryView)
		{
			if (model.Reversals == null)
			{
				return;
			}
			foreach (var reversal in model.SelectedReversals)
			{
				var revWsRFC5646 = model.Reversals.Where(prop => prop.Value.Label == reversal).Select(prop => prop.Value.WritingSystem).FirstOrDefault();
				webonaryView.UpdateStatus(string.Format(LanguageExplorerResources.ExportingReversalsToWebonary, reversal));
				var reversalWs = _cache.LangProject.AnalysisWritingSystems.FirstOrDefault(ws => ws.LanguageTag == revWsRFC5646);
				// The reversalWs should always match the RFC5646 of one of the AnalysisWritingSystems, this exception is for future programming errors
				if (reversalWs == null)
				{
					throw new ApplicationException($"Could not locate reversal writing system for {reversal}");
				}
				var xhtmlPath = Path.Combine(tempDirectoryToCompress, $"reversal_{reversalWs.IcuLocale}.xhtml");
				var configuration = model.Reversals[reversal];
				_exportService.ExportReversalContent(xhtmlPath, revWsRFC5646, configuration);
				webonaryView.UpdateStatus(LanguageExplorerResources.ExportingReversalsToWebonaryCompleted);
			}
		}

		/// <summary>
		/// Return upload URI, based on siteName.
		/// </summary>
		internal virtual string DestinationURI(string siteName)
		{
			return $"https://{Server}/{siteName}/wp-json/webonary/import";
		}

		/// <summary>
		/// Return upload URI, based on siteName.
		/// </summary>
		internal virtual string DestinationApiURI(string siteName, string apiEndpoint)
		{
			return $"https://cloud-api.{Server}/v1/{apiEndpoint}/{siteName}?client=Flex&version='{Assembly.GetExecutingAssembly().GetName().Version}'";
		}

		internal static string Server
		{
			get
			{
				// For local testing, set the WEBONARYSERVER environment variable to something like 192.168.33.10
				var server = Environment.GetEnvironmentVariable("WEBONARYSERVER");
				return string.IsNullOrEmpty(server) ? "webonary.org" : server;
			}
		}

		internal static bool UseJsonApi => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBONARY_API"));

		internal void UploadToWebonary(string zipFileToUpload, UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			Guard.AgainstNull(zipFileToUpload, nameof(zipFileToUpload));
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(LanguageExplorerResources.ksConnectingToWebonary);
			var targetURI = DestinationURI(model.SiteName);
			using (var client = CreateWebClient())
			{
				var credentials = $"{model.UserName}:{model.Password}";
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)));
				client.Headers.Add("user-agent", $"FieldWorks Language Explorer v.{Assembly.GetExecutingAssembly().GetName().Version}");
				client.Headers[HttpRequestHeader.Accept] = "*/*";
				byte[] response;
				try
				{
					response = client.UploadFileToWebonary(targetURI, zipFileToUpload);
				}
				catch (WebonaryException e)
				{
					UpdateViewWithWebonaryException(view, e);
					return;
				}
				var responseText = Encoding.ASCII.GetString(response);
				UpdateViewWithWebonaryResponse(view, client, responseText);
			}
		}

		internal bool UploadFileToWebonary(string signedUrl, string fileName, IUploadToWebonaryView view)
		{
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(LanguageExplorerResources.ksConnectingToWebonary);
			using (var client = CreateWebClient())
			{
				client.Headers.Add("Content-Type", MimeMapping.GetMimeMapping(fileName));
				client.Headers.Add("user-agent", string.Format("FieldWorks Language Explorer v.{0}", Assembly.GetExecutingAssembly().GetName().Version));
				client.Headers[HttpRequestHeader.Accept] = "*/*";

				byte[] response = null;
				try
				{
					response = client.UploadFileToWebonary(signedUrl, fileName, "PUT");
				}
				catch (WebonaryException e)
				{
					UpdateViewWithWebonaryException(view, e);
					return false;
				}
				var responseText = Encoding.ASCII.GetString(response);
#if DEBUG
				UpdateViewWithWebonaryResponse(view, client, responseText);
#endif
			}
			return true;
		}

		private string PostContentToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view, string apiEndpoint, JContainer postContent)
		{
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(LanguageExplorerResources.ksConnectingToWebonary);
			var targetURI = DestinationApiURI(model.SiteName, apiEndpoint);

			using (var client = CreateWebClient())
			{
				var credentials = $"{model.UserName}:{model.Password}";
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)) + "=");
				client.Headers.Add("user-agent", $"FieldWorks Language Explorer v.{Assembly.GetExecutingAssembly().GetName().Version}");
				client.Headers[HttpRequestHeader.Accept] = "*/*";

				string response;
				try
				{
					response = client.PostDictionaryMetadata(targetURI, postContent.ToString(Formatting.None));
				}
				catch (WebonaryException e)
				{
					UpdateViewWithWebonaryException(view, e);
					return string.Empty;
				}

				return response;
			}
		}

		internal string DeleteContentFromWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view, string apiEndpoint)
		{
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(LanguageExplorerResources.ksConnectingToWebonary);
			var targetURI = DestinationApiURI(model.SiteName, apiEndpoint);

			using (var client = CreateWebClient())
			{
				var credentials = string.Format("{0}:{1}", model.UserName, model.Password);
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)) + "=");
				client.Headers.Add("user-agent", string.Format("FieldWorks Language Explorer v.{0}", Assembly.GetExecutingAssembly().GetName().Version));
				client.Headers[HttpRequestHeader.Accept] = "*/*";

				string response;
				try
				{
					response = Encoding.UTF8.GetString(client.DeleteContent(targetURI));
				}
				catch (WebonaryException e)
				{
					if (e.StatusCode == HttpStatusCode.NotFound)
						return string.Empty;
					UpdateViewWithWebonaryException(view, e);
					return string.Empty;
				}

				return response;
			}
		}

		private bool PostEntriesToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view, List<JArray> entries, bool isReversal)
		{
			var allPostsSucceeded = true;
			foreach (var entryBatch in entries)
			{
				allPostsSucceeded &= PostEntriesToWebonary(model, view, "post/entry", entryBatch, isReversal);
			}

			return allPostsSucceeded;
		}

		private bool PostEntriesToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view, string apiEndpoint, JContainer postContent, bool isReversal)
		{
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(LanguageExplorerResources.ksConnectingToWebonary);
			var targetURI = DestinationApiURI(model.SiteName, apiEndpoint);

			using (var client = CreateWebClient())
			{
				var credentials = $"{model.UserName}:{model.Password}";
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)) + "=");
				client.Headers.Add("user-agent", $"FieldWorks Language Explorer v.{Assembly.GetExecutingAssembly().GetName().Version}");
				client.Headers[HttpRequestHeader.Accept] = "*/*";

				string response;
				try
				{
					response = client.PostEntry(targetURI, postContent.ToString(Formatting.None), isReversal);
				}
				catch (WebonaryException e)
				{
					UpdateViewWithWebonaryException(view, e);
					return false;
				}
#if DEBUG
				view.UpdateStatus(response);
#endif
				return true;
			}
		}

		private static void UpdateViewWithWebonaryException(IUploadToWebonaryView view, WebonaryException e)
		{
			if (e.StatusCode == HttpStatusCode.Redirect)
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorWebonarySiteName);
			}
			else
			{
				view.UpdateStatus(string.Format(LanguageExplorerResources.ksErrorCannotConnectToWebonary, Environment.NewLine, e.StatusCode, e.Message));
			}

			view.SetStatusCondition(WebonaryStatusCondition.Error);
		}

		private static void UpdateViewWithWebonaryResponse(IUploadToWebonaryView view, IWebonaryClient client, string responseText)
		{
			if (client.ResponseStatusCode == HttpStatusCode.Found)
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorWebonarySiteName);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else if (responseText.Contains("Upload successful"))
			{
				if (!responseText.Contains("error"))
				{
					view.UpdateStatus(LanguageExplorerResources.ksWebonaryUploadSuccessful);
					view.SetStatusCondition(WebonaryStatusCondition.Success);
					return;
				}

				view.UpdateStatus(LanguageExplorerResources.ksWebonaryUploadSuccessfulErrorProcessing);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}

			if (responseText.Contains("Wrong username or password"))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorUsernameOrPassword);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else if (responseText.Contains("User doesn't have permission to import data"))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorUserDoesntHavePermissionToImportData);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else // Unknown error, display the server response, but cut it off at 100 characters
			{
				view.UpdateStatus(string.Format("{0}{1}{2}{1}", LanguageExplorerResources.ksResponseFromServer, Environment.NewLine,
					responseText.Substring(0, Math.Min(100, responseText.Length))));
			}
		}

		///<summary>This stub is intended for other files related to front- and backmatter (things not really managed by FLEx itself)</summary>
		private void ExportOtherFilesContent(string tempDirectoryToCompress, UploadToWebonaryModel logTextbox, object outputLogTextbox)
		{
			//TODO: Copy the user selected other files into the temp directory and normalize filenames to NFC
		}

		public void UploadToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			view.UpdateStatus(LanguageExplorerResources.ksUploadingToWebonary);
			view.SetStatusCondition(WebonaryStatusCondition.None);
			if (string.IsNullOrEmpty(model.SiteName))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorNoSiteName);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}
			if (string.IsNullOrEmpty(model.UserName))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorNoUsername);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}
			if (string.IsNullOrEmpty(model.Password))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorNoPassword);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}
			if (string.IsNullOrEmpty(model.SelectedPublication))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorNoPublication);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}
			if (string.IsNullOrEmpty(model.SelectedConfiguration))
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorNoConfiguration);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}
			var tempDirectoryForExport = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectoryForExport);
			if (UseJsonApi)
			{
				var deleteResponse = DeleteContentFromWebonary(model, view, "delete/dictionary");
				if (deleteResponse != string.Empty)
				{
					view.UpdateStatus(string.Format(LanguageExplorerResources.UploadToWebonary_DeletingProjFiles, Environment.NewLine, deleteResponse));
				}
				var configuration = model.Configurations[model.SelectedConfiguration];
				var templateFileNames = GenerateConfigurationTemplates(configuration, _cache, tempDirectoryForExport);
				view.UpdateStatus(LanguageExplorerResources.ksPreparingDataForWebonary);
				var metadataContent = GenerateDictionaryMetadataContent(model, templateFileNames, tempDirectoryForExport);
				view.UpdateStatus(LanguageExplorerResources.ksWebonaryFinishedDataPrep);
				var entries = _exportService.ExportConfiguredJson(tempDirectoryForExport, configuration);
				var allRequestsSucceeded = PostEntriesToWebonary(model, view, entries, false);

				foreach (var selectedReversal in model.SelectedReversals)
				{
					var writingSystem = model.Reversals[selectedReversal].WritingSystem;
					entries = _exportService.ExportConfiguredReversalJson(tempDirectoryForExport, writingSystem, out var entryIds, model.Reversals[selectedReversal]);
					allRequestsSucceeded &= PostEntriesToWebonary(model, view, entries, true);
					var reversalLetters = LcmJsonGenerator.GenerateReversalLetterHeaders(model.SiteName, writingSystem, entryIds, _cache);
					AddReversalHeadword(metadataContent, writingSystem, reversalLetters);
				}
				allRequestsSucceeded &= RecursivelyPutFilesToWebonary(model, tempDirectoryForExport, view);
				var postResult = PostContentToWebonary(model, view, "post/dictionary", metadataContent);
				allRequestsSucceeded &= !string.IsNullOrEmpty(postResult);
				if (allRequestsSucceeded)
				{
					view.UpdateStatus(LanguageExplorerResources.ksWebonaryUploadSuccessful);
					view.SetStatusCondition(WebonaryStatusCondition.Success);
				}
			}
			else
			{
				var zipBasename = UploadFilename(model, view);
				if (zipBasename == null)
				{
					return;
				}
				var zipFileToUpload = Path.Combine(Path.GetTempPath(), zipBasename);
				ExportDictionaryContent(tempDirectoryForExport, model, view);
				ExportReversalContent(tempDirectoryForExport, model, view);
				ExportOtherFilesContent(tempDirectoryForExport, model, view);
				CompressExportedFiles(tempDirectoryForExport, zipFileToUpload, view);
				UploadToWebonary(zipFileToUpload, model, view);
			}
		}

		private void AddReversalHeadword(JObject metaData, string writingSystem, JArray reversalLetters)
		{
			var reversals = (JArray)metaData["reversalLanguages"];
			var reversalToUpdate = reversals.First(reversal => (string)((JObject)reversal)["lang"] == writingSystem);
			reversalToUpdate["letters"] = reversalLetters;
		}

		private string[] GenerateConfigurationTemplates(DictionaryConfigurationModel configuration, LcmCache cache, string tempDirectoryForExport)
		{
			var partFileNames = configuration.Parts.Where(pt => pt.IsEnabled).Select(c => CssGenerator.GetClassAttributeForConfig(c) + ".xhtml").ToArray();
			var partTemplates = LcmXhtmlGenerator.GenerateXHTMLTemplatesForConfigurationModel(configuration, cache);
			if (partTemplates.Count != partFileNames.Count())
			{
				throw new ApplicationException("Programming error generating xhtml templates from a configuration.");
			}

			for (var i = 0; i < partTemplates.Count; ++i)
			{
				File.WriteAllText(Path.Combine(tempDirectoryForExport, partFileNames[i]), partTemplates[i]);
			}
			return partFileNames;
		}

		/// <summary>
		/// Filename of zip file to upload to webonary, based on a particular model.
		/// If there are any characters that might cause a problem, null is returned.
		/// </summary>
		internal static string UploadFilename(UploadToWebonaryModel basedOnModel, IUploadToWebonaryView view)
		{
			Guard.AgainstNull(basedOnModel, nameof(basedOnModel));
			if (string.IsNullOrEmpty(basedOnModel.SiteName))
			{
				throw new ArgumentException(nameof(basedOnModel));
			}
			var disallowedCharacters = MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterProjName) + "_ $.%";
			if (basedOnModel.SiteName.IndexOfAny(disallowedCharacters.ToCharArray()) >= 0)
			{
				view.UpdateStatus(LanguageExplorerResources.ksErrorInvalidCharacters);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return null;
			}
			return basedOnModel.SiteName + ".zip";
		}

		/// <summary>
		/// True if given a path to a file type that is acceptable to upload to Webonary. Otherwise false.
		/// </summary>
		/// <remarks>Could be changed to consider the magic number instead of file extension, if helpful.</remarks>
		internal static bool IsSupportedWebonaryFile(string path)
		{
			var supportedFileExtensions = new List<string>
			{
				".xhtml", ".css", ".html", ".htm", ".json", ".xml", ".wav",
				".jpg", ".jpeg", ".gif", ".png", ".mp3", ".mp4", ".3gp"
			};
			return supportedFileExtensions.Any(path.ToLowerInvariant().EndsWith);
		}
	}
}
