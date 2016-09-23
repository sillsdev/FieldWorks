// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Ionic.Zip;
using SIL.FieldWorks.FDO;
using XCore;
using System.Net;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Currently serves as the controller and the model for the UploadToWebonaryView
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule", Justification="Cache and PropertyTable are references")]
	public class UploadToWebonaryController : IDisposable
	{
		private readonly FdoCache m_cache;
		private readonly PropertyTable m_propertyTable;
		private readonly DictionaryExportService m_exportService;
		private DictionaryExportService.PublicationActivator m_publicationActivator;
		/// <summary>
		/// This action creates the WebClient for accessing webonary. Protected to enable a mock client for unit testing.
		/// </summary>
		protected Func<IWebonaryClient> CreateWebClient = () => new WebonaryClient();

		public PropertyTable PropertyTable { private get; set; }

		public UploadToWebonaryController(FdoCache cache, PropertyTable propertyTable, Mediator mediator)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			m_exportService = new DictionaryExportService(propertyTable, mediator);
			m_publicationActivator = new DictionaryExportService.PublicationActivator(propertyTable);
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

		internal static void CompressExportedFiles(string tempDirectoryToCompress, string zipFileToUpload, IUploadToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(xWorksStrings.BeginCompressingDataForWebonary);
			using(var zipFile = new ZipFile())
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
			foreach(var file in Directory.EnumerateFiles(dirToCompress))
			{
				if (!IsSupportedWebonaryFile(file))
				{
					webonaryView.UpdateStatus(string.Format("Excluding {0},{1} format is unsupported by Webonary.",
						Path.GetFileName(file), Path.GetExtension(file)));
					continue;
				}
				zipFile.AddFile(file, dirInZip);
				webonaryView.UpdateStatus(Path.GetFileName(file));
			}
			foreach(var dir in Directory.EnumerateDirectories(dirToCompress))
			{
				RecursivelyAddFilesToZip(zipFile, dir, Path.Combine(dirInZip, Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar))), webonaryView);
			}
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
				var configurationFile = Path.Combine(m_propertyTable.UserSettingDirectory, "ReversalIndex", reversal + DictionaryConfigurationModel.FileExtension);
				var configuration = new DictionaryConfigurationModel(configurationFile, m_cache);
				m_exportService.ExportReversalContent(xhtmlPath, revWsRFC5646, configuration);
				webonaryView.UpdateStatus(xWorksStrings.ExportingReversalsToWebonaryCompleted);
			}
		}

		/// <summary>
		/// Return upload URI, based on siteName.
		/// </summary>
		internal virtual string DestinationURI(string siteName)
		{
			// To do local testing set the WEBONARYSERVER environment variable to something like 192.168.33.10
			var server = Environment.GetEnvironmentVariable("WEBONARYSERVER");
			server = string.IsNullOrEmpty(server) ? "webonary.org" : server;
			return string.Format("https://{0}.{1}/wp-json/webonary/import", siteName, server);
		}

		internal void UploadToWebonary(string zipFileToUpload, UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			if (zipFileToUpload == null)
				throw new ArgumentNullException("zipFileToUpload");
			if(model == null)
				throw new ArgumentNullException("model");
			if (view == null)
				throw new ArgumentNullException("view");

			view.UpdateStatus("Connecting to Webonary.");
			var targetURI = DestinationURI(model.SiteName);

			using (var client = CreateWebClient())
			{
				var credentials = string.Format("{0}:{1}", model.UserName, model.Password);
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)));

				byte[] response = null;
				try
				{
					response = client.UploadFileToWebonary(targetURI, zipFileToUpload);
				}
				catch (WebonaryClient.WebonaryException e)
				{
					if (e.StatusCode == HttpStatusCode.Redirect)
					{
						view.UpdateStatus("Error: There has been an error accessing webonary. Is your sitename correct?");
					}
					else
					{
						const string errorMessage = "Unable to connect to Webonary.  Please check your username and password and your Internet connection.";
						view.UpdateStatus(string.Format("An error occurred uploading your data: {0}{1}{2}:{3}",
							errorMessage, Environment.NewLine, e.StatusCode, e.Message));
					}
					view.SetStatusCondition(WebonaryStatusCondition.Error);
					return;
				}
				var responseText = Encoding.ASCII.GetString(response);

				if (client.ResponseStatusCode == HttpStatusCode.Found)
				{
					view.UpdateStatus("Error: There has been an error accessing webonary. Is your sitename correct?");
					view.SetStatusCondition(WebonaryStatusCondition.Error);
				}
				else if (responseText.Contains("Upload successful"))
				{
					if (!responseText.Contains("error"))
					{
						view.UpdateStatus("Upload successful. " +
							"Preparing your data for publication. " +
							"This may take several minutes to a few hours depending on the size of your dictionary. " +
							"You will receive an email when the process is complete. " +
							"You can examine the progress on the admin page of your Webonary site. "+
							"You may now safely close this dialog.");
						view.SetStatusCondition(WebonaryStatusCondition.Success);
						return;
					}

					view.UpdateStatus("The upload was successful; however, there were errors processing your data.");
					view.SetStatusCondition(WebonaryStatusCondition.Error);
				}

				if (responseText.Contains("Wrong username or password"))
				{
					view.UpdateStatus("Error: Wrong username or password");
					view.SetStatusCondition(WebonaryStatusCondition.Error);
				}
				else if (responseText.Contains("User doesn't have permission to import data"))
				{
					view.UpdateStatus("Error: User doesn't have permission to import data");
					view.SetStatusCondition(WebonaryStatusCondition.Error);
				}
				else // Unknown error, display the server response, but cut it off at 100 characters
				{
					view.UpdateStatus(string.Format("Response from server:{0}{1}{0}", Environment.NewLine,
						responseText.Substring(0, Math.Min(100, responseText.Length))));
				}
			}
		}

		private void ExportOtherFilesContent(string tempDirectoryToCompress, UploadToWebonaryModel logTextbox, object outputLogTextbox)
		{
			//TODO:Copy the user selected other files into the temp directory
		}

		public void UploadToWebonary(UploadToWebonaryModel model, IUploadToWebonaryView view)
		{
			view.UpdateStatus("Uploading to Webonary.");
			view.SetStatusCondition(WebonaryStatusCondition.None);

			if (string.IsNullOrEmpty(model.SiteName))
			{
				view.UpdateStatus("Error: No site name specified.");
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.UserName))
			{
				view.UpdateStatus("Error: No username specified.");
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if (string.IsNullOrEmpty(model.Password))
			{
				view.UpdateStatus("Error: No Password specified.");
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.SelectedPublication))
			{
				view.UpdateStatus("Error: No Publication specified.");
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			if(string.IsNullOrEmpty(model.SelectedConfiguration))
			{
				view.UpdateStatus("Error: No Configuration specified.");
				view.SetStatusCondition(WebonaryStatusCondition.Error);
				return;
			}

			var tempDirectoryToCompress = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var zipBasename = UploadFilename(model, view);
			if (zipBasename == null)
				return;
			var zipFileToUpload = Path.Combine(Path.GetTempPath(), zipBasename);
			Directory.CreateDirectory(tempDirectoryToCompress);
			ExportDictionaryContent(tempDirectoryToCompress, model, view);
			ExportReversalContent(tempDirectoryToCompress, model, view);
			ExportOtherFilesContent(tempDirectoryToCompress, model, view);
			CompressExportedFiles(tempDirectoryToCompress, zipFileToUpload, view);
			UploadToWebonary(zipFileToUpload, model, view);
		}

		/// <summary>
		/// Filename of zip file to upload to webonary, based on a particular model.
		/// If there are any characters that might cause a problem, null is returned.
		/// </summary>
		internal static string UploadFilename(UploadToWebonaryModel basedOnModel, IUploadToWebonaryView view)
		{
			if (basedOnModel == null)
				throw new ArgumentNullException("basedOnModel");
			if (string.IsNullOrEmpty(basedOnModel.SiteName))
				throw new ArgumentException("basedOnModel");
			var disallowedCharacters = MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterProjName) + "_ $.%";
			if (basedOnModel.SiteName.IndexOfAny(disallowedCharacters.ToCharArray()) >= 0)
			{
				view.UpdateStatus("Error: Invalid characters found in sitename.");
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
				".xhtml", ".css", ".html", ".htm", ".json", ".xml",
				".jpg", ".jpeg", ".gif", ".png", ".mp3", ".mp4", ".3gp"
			};
			return supportedFileExtensions.Any(path.ToLowerInvariant().EndsWith);
		}
	}
}
