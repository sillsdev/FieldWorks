// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Ionic.Zip;
using SIL.FieldWorks.FDO;
using XCore;
using System.Net;
using System.Text;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Currently serves as the controller and the model for the PublishToWebonaryView
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache and Mediator are references")]
	public class PublishToWebonaryController
	{
		public IEnumerable<string> Reversals { private get; set; }

		public Dictionary<string, DictionaryConfigurationModel> Configurations { private get; set; }

		public List<string> Publications { private get; set; }

		public FdoCache Cache { private get; set; }

		public Mediator Mediator { private get; set; }

		public void PopulateReversalsCheckboxList(IPublishToWebonaryView publishToWebonaryView)
		{
			publishToWebonaryView.PopulateReversalsCheckboxList(Reversals);
		}

		public void PopulateConfigurationsList(IPublishToWebonaryView publishToWebonaryView)
		{
			publishToWebonaryView.PopulateConfigurationsList(Configurations.Keys);
		}

		public void PopulatePublicationsList(IPublishToWebonaryView publishToWebonaryView)
		{
			publishToWebonaryView.PopulatePublicationsList(Publications);
		}

		/// <summary>
		/// Exports the dictionary xhtml and css for the publication and configuration that the user had selected in the dialog.
		/// </summary>
		private void ExportDictionaryContent(string tempDirectoryToCompress, IPublishToWebonaryView webonaryView)
		{
			webonaryView.UpdateStatus(String.Format(xWorksStrings.ExportingEntriesToWebonary, webonaryView.Publication, webonaryView.Configuration));
			var xhtmlPath = Path.Combine(tempDirectoryToCompress, "configured.xhtml");
			var cssPath = Path.Combine(tempDirectoryToCompress, "configured.css");
			int[] entriesToSave;
			var publicationDecorator = ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(Mediator, out entriesToSave);
			var configuration = Configurations[webonaryView.Configuration];
			ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, configuration, Mediator, xhtmlPath, cssPath, null);
			webonaryView.UpdateStatus(xWorksStrings.ExportingEntriesToWebonaryCompleted);
		}

		private void CompressExportedFiles(string tempDirectoryToCompress, string zipFileToUpload, IPublishToWebonaryView webonaryView)
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
		private void RecursivelyAddFilesToZip(ZipFile zipFile, string dirToCompress, string dirInZip, IPublishToWebonaryView webonaryView)
		{
			foreach(var file in Directory.EnumerateFiles(dirToCompress))
			{
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
		private void ExportReversalContent(string tempDirectoryToCompress, IPublishToWebonaryView logTextbox)
		{
			//TODO:Actually export the reversal content into the temp directory
		}

		/// <summary>
		/// Return upload URI, based on siteName.
		/// </summary>
		internal virtual string DestinationURI(string siteName)
		{
			// TODO use specified site with respect to webonary domain, rather than using value
			// for current testing. eg $siteName.webonary.org/something or
			// www.webonary.org/$sitename/wp-json/something .
			var targetURI = "http://192.168.33.10/test/wp-json/webonary/import";
			return targetURI;
		}

		internal void UploadToWebonary(string zipFileToUpload, IPublishToWebonaryView view)
		{
			if (zipFileToUpload == null)
				throw new ArgumentNullException("zipFileToUpload");
			if (view == null)
				throw new ArgumentNullException("view");

			view.UpdateStatus("Connecting to Webonary.");
			var targetURI = DestinationURI(view.SiteName);
			using (var client = new WebClient())
			{
				var credentials = string.Format("{0}:{1}", view.UserName, view.Password);
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes(credentials)));

				byte[] response = null;
				try
				{
					response = client.UploadFile(targetURI, zipFileToUpload);
				}
				catch (WebException e)
				{
					view.UpdateStatus(string.Format("An error occurred uploading your data: {0}", e.Message));
					return;
				}
				var responseText = System.Text.Encoding.ASCII.GetString(response);

				if (responseText.Contains("Upload successful"))
				{
					if (responseText.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0)
					{
						view.UpdateStatus("Upload successful. " +
							"Preparing your data for publication. " +
							"This may take several minutes to a few hours depending on the size of your dictionary. " +
							"You will receive an email when the process is complete. " +
							"You can examine the progress on the admin page of your Webonary site. "+
							"You may now safely close this dialog.");
						return;
					}

					view.UpdateStatus("The upload was successful, however there were errors.");
				}

				if (responseText.Contains("Wrong username or password"))
					view.UpdateStatus("Error: Wrong username or password");
				if (responseText.Contains("User doesn't have permission to import data"))
					view.UpdateStatus("Error: User doesn't have permission to import data");

				view.UpdateStatus(string.Format("Response from server:{0}{1}{0}", Environment.NewLine, responseText));
			}
		}

		private void ExportOtherFilesContent(string tempDirectoryToCompress, object outputLogTextbox)
		{
			//TODO:Copy the user selected other files into the temp directory
		}

		public void PublishToWebonary(IPublishToWebonaryView view)
		{
			view.UpdateStatus("Publishing to Webonary.");

			if (view.SiteName == null)
			{
				view.UpdateStatus("Error: No site name specified.");
				return;
			}

			if (view.UserName == null)
			{
				view.UpdateStatus("Error: No username specified.");
				return;
			}

			if (view.Password == null)
			{
				view.UpdateStatus("Error: No Password specified.");
				return;
			}

			if (view.Publication == null)
			{
				view.UpdateStatus("Error: No Publication specified.");
				return;
			}

			if (view.Configuration == null)
			{
				view.UpdateStatus("Error: No Configuration specified.");
				return;
			}

			var tempDirectoryToCompress = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var zipFileToUpload = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
			Directory.CreateDirectory(tempDirectoryToCompress);
			ExportDictionaryContent(tempDirectoryToCompress, view);
			ExportReversalContent(tempDirectoryToCompress, view);
			ExportOtherFilesContent(tempDirectoryToCompress, view);
			CompressExportedFiles(tempDirectoryToCompress, zipFileToUpload, view);
			UploadToWebonary(zipFileToUpload, view);
		}
	}
}
