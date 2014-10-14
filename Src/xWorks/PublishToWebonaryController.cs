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

		private void UploadToWebonary(string zipFileToUpload)
		{
			//TODO:Actually upload
		}

		private void ExportOtherFilesContent(string tempDirectoryToCompress, object outputLogTextbox)
		{
			//TODO:Copy the user selected other files into the temp directory
		}

		public void PublishToWebonary(IPublishToWebonaryView view)
		{
			var tempDirectoryToCompress = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var zipFileToUpload = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
			Directory.CreateDirectory(tempDirectoryToCompress);
			ExportDictionaryContent(tempDirectoryToCompress, view);
			ExportReversalContent(tempDirectoryToCompress, view);
			ExportOtherFilesContent(tempDirectoryToCompress, view);
			CompressExportedFiles(tempDirectoryToCompress, zipFileToUpload, view);
			UploadToWebonary(zipFileToUpload);
		}
	}
}