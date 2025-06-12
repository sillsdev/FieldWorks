// Copyright (c) 2014-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using SIL.LCModel;
using XCore;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Currently serves as the controller and the model for the UploadToWebonaryView
	/// </summary>
	public class UploadToWebonaryController : IDisposable
	{
		private readonly LcmCache m_cache;
		private readonly PropertyTable m_propertyTable;
		private readonly DictionaryExportService m_exportService;
		private DictionaryExportService.PublicationActivator m_publicationActivator;
		/// <summary>
		/// This action creates the WebClient for accessing webonary. Protected to enable a mock client for unit testing.
		/// </summary>
		protected Func<IWebonaryClient> CreateWebClient = () => new WebonaryClient { Encoding = Encoding.UTF8 };

		public UploadToWebonaryController(LcmCache cache, PropertyTable propertyTable, Mediator mediator)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			m_exportService = new DictionaryExportService(propertyTable, mediator);
			m_publicationActivator = new DictionaryExportService.PublicationActivator(propertyTable);
		}

		public bool IsSortingOnAlphaHeaders()
		{
			var clerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
			return RecordClerk.IsClerkSortingByHeadword(clerk);
		}

		#region Disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && m_publicationActivator != null)
				m_publicationActivator.Dispose();
			m_publicationActivator = null;
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
			return m_exportService.CountDictionaryEntries(config);
		}

		/// <summary>
		/// Table of reversal indexes and their counts.
		/// </summary>
		public SortedDictionary<string,int> GetCountsOfReversalIndexes(IEnumerable<string> requestedIndexes)
		{
			return m_exportService.GetCountsOfReversalIndexes(requestedIndexes);
		}

		public void ActivatePublication(string publication)
		{
			m_publicationActivator.ActivatePublication(publication);
		}

		/// <summary>
		/// Exports the dictionary xhtml and css for the publication and configuration that the user had selected in the dialog.
		/// </summary>
		private void ExportDictionaryContent(string tempDirectoryToCompress, UploadToWebonaryModel model, IUploadToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(String.Format(xWorksStrings.ExportingEntriesToWebonary, model.SelectedPublication, model.SelectedConfiguration));
			var xhtmlPath = Path.Combine(tempDirectoryToCompress, "configured.xhtml");
			var configuration = model.Configurations[model.SelectedConfiguration];
			m_exportService.ExportDictionaryContent(xhtmlPath, configuration);
			webonaryView.UpdateStatus(xWorksStrings.ExportingEntriesToWebonaryCompleted);
		}

		private JObject GenerateDictionaryMetadataContent(UploadToWebonaryModel model,
			IEnumerable<string> templateFileNames, string tempDirectoryForExport)
		{
			return m_exportService.ExportDictionaryContentJson(model.SiteName, templateFileNames,
				model.Reversals.Where(kvp => model.SelectedReversals.Contains(kvp.Key)).Select(kvp => kvp.Value),
				tempDirectoryForExport);
		}

		internal static void CompressExportedFiles(string tempDirectoryToCompress, string zipFileToUpload, IUploadToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(xWorksStrings.BeginCompressingDataForWebonary);
			using(var zipFile = new ZipFile(Encoding.UTF8))
			{
				RecursivelyAddFilesToZip(zipFile, tempDirectoryToCompress, "", webonaryView);
				zipFile.Save(zipFileToUpload);
			}
			webonaryView.UpdateStatus(xWorksStrings.FinishedCompressingDataForWebonary);
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
					webonaryView.UpdateStatus(string.Format(xWorksStrings.ksExcludingXXFormatUnsupported,
						Path.GetFileName(file), Path.GetExtension(file)));
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
		/// This method will recurse into a directory and add upload all the files through the webonary api to an amazon s3 bucket
		/// </summary>
		private bool RecursivelyPutFilesToWebonary(UploadToWebonaryModel model, string dirToUpload, IUploadToWebonaryView webonaryView, string subFolder = "")
		{
			bool allFilesSucceeded = true;
			foreach (var file in Directory.EnumerateFiles(dirToUpload))
			{
				if (!IsSupportedWebonaryFile(file))
				{
					webonaryView.UpdateStatus(string.Format(xWorksStrings.ksExcludingXXFormatUnsupported,
						Path.GetFileName(file), Path.GetExtension(file)));
					continue;
				}
				dynamic fileToSign = new JObject();
				// ReSharper disable once AssignNullToNotNullAttribute - This file has a filename, the OS told us so.
				var relativeFilePath = Path.Combine(model.SiteName, subFolder, Path.GetFileName(file));
				if (Platform.IsWindows)
					relativeFilePath = relativeFilePath.Replace('\\', '/');
				fileToSign.objectId = relativeFilePath;
				fileToSign.action = "putObject";
				var signedUrl = PostContentToWebonary(model, webonaryView, "post/file", fileToSign);
				if (string.IsNullOrEmpty(signedUrl))
				{
					// Sleep briefly and try one more time (To compensate for a potential lambda cold start)
					Thread.Sleep(500);
					signedUrl = PostContentToWebonary(model, webonaryView, "post/file", fileToSign);
					if (string.IsNullOrEmpty(signedUrl))
					{
						webonaryView.UpdateStatus(string.Format(xWorksStrings.ksPutFilesToWebonaryFailed, relativeFilePath));
						return false;
					}
				}
				allFilesSucceeded &= UploadFileToWebonary(signedUrl, file, webonaryView);
				webonaryView.UpdateStatus(string.Format(xWorksStrings.ksPutFilesToWebonaryUploaded, Path.GetFileName(file)));
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
				return;
			foreach (var reversal in model.SelectedReversals)
			{
				var revWsRFC5646 = model.Reversals.Where(prop => prop.Value.Label == reversal).Select(prop => prop.Value.WritingSystem).FirstOrDefault();
				webonaryView.UpdateStatus(string.Format(xWorksStrings.ExportingReversalsToWebonary, reversal));
				var reversalWs = m_cache.LangProject.AnalysisWritingSystems.FirstOrDefault(ws => ws.LanguageTag == revWsRFC5646);
				// The reversalWs should always match the RFC5646 of one of the AnalysisWritingSystems, this exception is for future programming errors
				if (reversalWs == null)
				{
					throw new ApplicationException(string.Format("Could not locate reversal writing system for {0}", reversal));
				}
				var xhtmlPath = Path.Combine(tempDirectoryToCompress, string.Format("reversal_{0}.xhtml", reversalWs.IcuLocale));
				var configuration = model.Reversals[reversal];
				m_exportService.ExportReversalContent(xhtmlPath, revWsRFC5646, configuration);
				webonaryView.UpdateStatus(xWorksStrings.ExportingReversalsToWebonaryCompleted);
			}
		}

		/// <summary>
		/// Converts siteName to lowercase and removes https://www.webonary.org, if present. LT-21224, LT-21387
		/// </summary>
		internal static string NormalizeSiteName(string siteName)
		{
			siteName = siteName.ToLowerInvariant();
			// trim a leading [http[s]://]webonary.org/
			const string domainSlash = WebonaryOrg + "/";
			var domainIndex = siteName.IndexOf(domainSlash, StringComparison.InvariantCulture);
			if (domainIndex != -1)
			{
				siteName = siteName.Substring(domainIndex + domainSlash.Length);
			}

			// Remove a trailing '/'
			if (siteName.EndsWith("/"))
			{
				siteName = siteName.Substring(0, siteName.Length - 1);
			}
			return siteName;
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

		internal const string WebonaryOrg = "webonary.org";

		internal static string Server
		{
			get
			{
				// For local testing, set the WEBONARYSERVER environment variable to something like 192.168.33.10
				var server = Environment.GetEnvironmentVariable("WEBONARYSERVER");
				return string.IsNullOrEmpty(server) ? WebonaryOrg : server;
			}
		}

		internal virtual bool UseJsonApi => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBONARY_API"));

		internal void UploadToWebonary(string zipFileToUpload, UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			Guard.AgainstNull(zipFileToUpload, nameof(zipFileToUpload));
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(view, nameof(view));

			view.UpdateStatus(xWorksStrings.ksConnectingToWebonary);
			var targetURI = DestinationURI(model.SiteName);

			using (var client = CreateWebClient())
			{
				var credentials = string.Format("{0}:{1}", model.UserName, model.Password);
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)));
				client.Headers.Add("user-agent", string.Format("FieldWorks Language Explorer v.{0}", Assembly.GetExecutingAssembly().GetName().Version));
				client.Headers[HttpRequestHeader.Accept] = "*/*";

				byte[] response = null;
				try
				{
					response = client.UploadFileToWebonary(targetURI, zipFileToUpload);
				}
				catch (WebonaryClient.WebonaryException e)
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

			view.UpdateStatus(xWorksStrings.ksConnectingToWebonary);
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
				catch (WebonaryClient.WebonaryException e)
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

			view.UpdateStatus(xWorksStrings.ksConnectingToWebonary);
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
					response = client.PostDictionaryMetadata(targetURI, postContent.ToString(Formatting.None));
				}
				catch (WebonaryClient.WebonaryException e)
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

			view.UpdateStatus(xWorksStrings.ksConnectingToWebonary);
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
				catch (WebonaryClient.WebonaryException e)
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

			view.UpdateStatus(xWorksStrings.ksConnectingToWebonary);
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
					response = client.PostEntry(targetURI, postContent.ToString(Formatting.None), isReversal);
				}
				catch (WebonaryClient.WebonaryException e)
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

		private static void UpdateViewWithWebonaryException(IUploadToWebonaryView view, WebonaryClient.WebonaryException e)
		{
			if (e.StatusCode == HttpStatusCode.Redirect)
			{
				view.UpdateStatus(xWorksStrings.ksErrorWebonarySiteName);
			}
			else
			{
				view.UpdateStatus(string.Format(xWorksStrings.ksErrorCannotConnectToWebonary,
					Environment.NewLine, e.StatusCode, e.Message));
			}
			view.SetStatusCondition(WebonaryStatusCondition.Error);
			TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Failed,
				new Dictionary<string, string>
				{
					{
						"statusCode", Enum.GetName(typeof(HttpStatusCode), e.StatusCode)
					}
				});

		}

		private static void UpdateViewWithWebonaryResponse(IUploadToWebonaryView view, IWebonaryClient client, string responseText)
		{
			if (client.ResponseStatusCode == HttpStatusCode.Found)
			{
				view.UpdateStatus(xWorksStrings.ksErrorWebonarySiteName);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else if (responseText.Contains("Upload successful"))
			{
				if (!responseText.Contains("error"))
				{
					view.UpdateStatus(xWorksStrings.ksWebonaryUploadSuccessful);
					view.SetStatusCondition(WebonaryStatusCondition.Success);
					TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Succeeded);
					return;
				}

				view.UpdateStatus(xWorksStrings.ksWebonaryUploadSuccessfulErrorProcessing);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}

			if (responseText.Contains("Wrong username or password"))
			{
				view.UpdateStatus(xWorksStrings.ksErrorUsernameOrPassword);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else if (responseText.Contains("User doesn't have permission to import data"))
			{
				view.UpdateStatus(xWorksStrings.ksErrorUserDoesntHavePermissionToImportData);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
			}
			else // Unknown error, display the server response, but cut it off at 100 characters
			{
				view.UpdateStatus(string.Format("{0}{1}{2}{1}", xWorksStrings.ksResponseFromServer, Environment.NewLine,
					responseText.Substring(0, Math.Min(100, responseText.Length))));
			}
			TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Failed,
				new Dictionary<string, string>
				{
					{
						"statusCode", Enum.GetName(typeof(HttpStatusCode), client.ResponseStatusCode)
					}
				});
		}

		///<summary>This stub is intended for other files related to front- and backmatter (things not really managed by FLEx itself)</summary>
		private void ExportOtherFilesContent(string tempDirectoryToCompress, UploadToWebonaryModel logTextbox, object outputLogTextbox)
		{
			//TODO: Copy the user selected other files into the temp directory and normalize filenames to NFC
		}

		public void UploadToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Launched);
			view.UpdateStatus(xWorksStrings.ksUploadingToWebonary);
			view.SetStatusCondition(WebonaryStatusCondition.None);

			if (string.IsNullOrEmpty(model.SiteName))
			{
				view.UpdateStatus(xWorksStrings.ksErrorNoSiteName);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.UserName))
			{
				view.UpdateStatus(xWorksStrings.ksErrorNoUsername);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if (string.IsNullOrEmpty(model.Password))
			{
				view.UpdateStatus(xWorksStrings.ksErrorNoPassword);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.SelectedPublication))
			{
				view.UpdateStatus(xWorksStrings.ksErrorNoPublication);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.SelectedConfiguration))
			{
				view.UpdateStatus(xWorksStrings.ksErrorNoConfiguration);
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Attempted,
				new Dictionary<string, string>
				{
					{
						"cloudApi", UseJsonApi.ToString()
					}
				});
			var tempDirectoryForExport = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectoryForExport);
			if (UseJsonApi)
			{
				try
				{
					var deleteResponse =
						DeleteContentFromWebonary(model, view, "delete/dictionary");
					if (deleteResponse != string.Empty)
					{
						view.UpdateStatus(string.Format(
							xWorksStrings.UploadToWebonary_DeletingProjFiles, Environment.NewLine,
							deleteResponse));
					}

					var configuration = model.Configurations[model.SelectedConfiguration];
					var templateFileNames =
						GenerateConfigurationTemplates(configuration, m_cache,
							tempDirectoryForExport);
					view.UpdateStatus(xWorksStrings.ksPreparingDataForWebonary);
					var metadataContent = GenerateDictionaryMetadataContent(model,
						templateFileNames, tempDirectoryForExport);
					view.UpdateStatus(xWorksStrings.ksWebonaryFinishedDataPrep);
					var entries =
						m_exportService.ExportConfiguredJson(tempDirectoryForExport,
							configuration);
					var allRequestsSucceeded = PostEntriesToWebonary(model, view, entries, false);

					var reversalClerk = RecordClerk.FindClerk(m_propertyTable, "AllReversalEntries");
					foreach (var selectedReversal in model.SelectedReversals)
					{
						int[] entryIds;
						var writingSystem = model.Reversals[selectedReversal].WritingSystem;
						entries = m_exportService.ExportConfiguredReversalJson(
							tempDirectoryForExport, writingSystem, out entryIds,
							model.Reversals[selectedReversal]);
						allRequestsSucceeded &= PostEntriesToWebonary(model, view, entries, true);
						var reversalLetters =
							LcmJsonGenerator.GenerateReversalLetterHeaders(model.SiteName,
								writingSystem, entryIds, m_cache, reversalClerk);
						AddReversalHeadword(metadataContent, writingSystem, reversalLetters);
					}

					allRequestsSucceeded &=
						RecursivelyPutFilesToWebonary(model, tempDirectoryForExport, view);
					var postResult = PostContentToWebonary(model, view, "post/dictionary",
						metadataContent);
					allRequestsSucceeded &= !string.IsNullOrEmpty(postResult);
					if (allRequestsSucceeded)
					{
						view.UpdateStatus(xWorksStrings.ksWebonaryUploadSuccessful);
						view.SetStatusCondition(WebonaryStatusCondition.Success);
						TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Succeeded);
					}
				}
				catch (Exception e)
				{
					using (var reporter = new SilErrorReportingAdapter(view as Form, m_propertyTable))
					{
						reporter.ReportNonFatalExceptionWithMessage(e, xWorksStrings.Webonary_UnexpectedUploadError);
					}
					view.UpdateStatus(xWorksStrings.Webonary_UnexpectedUploadError);
					view.SetStatusCondition(WebonaryStatusCondition.Error);
					TrackingHelper.TrackExport("lexicon", "webonary", ImportExportStep.Failed);
				}
			}
			else
			{
				var zipBasename = UploadFilename(model, view);
				if (zipBasename == null)
					return;
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
			if (basedOnModel == null)
				throw new ArgumentNullException(nameof(basedOnModel));
			if (string.IsNullOrEmpty(basedOnModel.SiteName))
				throw new ArgumentException(nameof(basedOnModel));
			var disallowedCharacters = MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterProjName) + "_ $.%";
			if (basedOnModel.SiteName.IndexOfAny(disallowedCharacters.ToCharArray()) >= 0)
			{
				view.UpdateStatus(xWorksStrings.ksErrorInvalidCharacters);
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
				".jpg", ".jpeg", ".gif", ".png", ".mp3", ".mp4", ".3gp", ".webm"
			};
			return supportedFileExtensions.Any(path.ToLowerInvariant().EndsWith);
		}
	}
}
