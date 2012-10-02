using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Xml.Xsl; // Bizarre location for TempFileCollection
using System.IO;
using System.Xml;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for InterlinearExportDialog.
	/// </summary>
	public class InterlinearExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		int m_hvoRoot;
		InterlinVc m_vc;
		FDO.IText m_text;
		private ITsString m_tssTextName;
		private ITsString m_tssTextAbbreviation;
		List<int> m_hvos = new List<int>();
		private List<int> m_scrHvos;
		private event EventHandler OnLaunchFilterScrScriptureSectionsDialog;

		public InterlinearExportDialog(Mediator mediator, int hvoRoot, InterlinVc vc, List<int> scrHvos)
			: base(mediator)
		{
			m_hvoRoot = hvoRoot;
			m_vc = vc;
			m_scrHvos = scrHvos;

			m_helpTopic = "khtpExportInterlinear";
			columnHeader1.Text = ITextStrings.ksFormat;
			columnHeader2.Text = ITextStrings.ksExtension;
			GetTextProps();
			Text = ITextStrings.ksExportInterlinear;
			if (MiscUtils.IsTEInstalled)
				OnLaunchFilterScrScriptureSectionsDialog += new EventHandler(LaunchFilterScrScriptureSectionsDialog);
		}

		private void GetTextProps()
		{
			IStText txt = CmObject.CreateFromDBObject(m_cache, m_hvoRoot) as IStText;
			if (txt != null)
			{
				int hvoOwner = txt.OwnerHVO;
				m_text = CmObject.CreateFromDBObject(m_cache, hvoOwner) as FDO.IText;
				if (m_text != null)
				{
					m_tssTextName = m_text.Name.BestVernacularAnalysisAlternative;
					m_tssTextAbbreviation = m_text.Abbreviation.BestVernacularAnalysisAlternative;
				}
				else if (SIL.FieldWorks.FDO.Scripture.Scripture.IsResponsibleFor(txt as SIL.FieldWorks.FDO.Cellar.StText))
				{
					m_tssTextName = txt.ShortNameTSS;
					// sorry, no abbreviation...
				}
			}
		}

		protected override string ConfigurationFilePath
		{
			get { return @"Language Explorer\Export Templates\Interlinear"; }
		}

		// Items in this version are never disabled.
		protected override bool ItemDisabled(string tag)
		{
			return false;
		}
		/// <summary>
		/// Override to do nothing since not configuring an FXT export process.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="ft"></param>
		/// <param name="item"></param>
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			m_ddNodes.Add(ddNode);
			columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
			//columnHeader2.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		/// <summary>
		/// If the selected text is from Scripture, pop up a selection dialog to allow the user to
		/// export more than one section (maybe even more than one book!) to a single file.
		/// </summary>
		/// <returns>true iff export can proceed to choosing an output file</returns>
		protected override bool PrepareForExport()
		{
			m_hvos.Clear();
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, m_hvoRoot);
			if (obj.OwningFlid != (int)SIL.FieldWorks.FDO.Ling.Text.TextTags.kflidContents)
			{
				Debug.Assert(m_scrHvos != null && m_scrHvos.Count > 0);
				if (m_scrHvos != null && m_scrHvos.Count > 0)
				{
					if (OnLaunchFilterScrScriptureSectionsDialog != null)
						OnLaunchFilterScrScriptureSectionsDialog(this, EventArgs.Empty);
				}
				return m_hvos.Count > 0;
			}
			return base.PrepareForExport();
		}

		/// <summary>
		/// In SE Fieldworks we can't depend upon ScrControls.dll We need some decoupling to
		/// prevent the linker from trying to load ScrControls.dll and crashing.
		/// This seems to work. (Although I wonder if we could get away with just calling this method
		/// if IsTEInstalled?)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void LaunchFilterScrScriptureSectionsDialog(object sender, EventArgs args)
		{
			using (FilterScrSectionDialog dlg = new FilterScrSectionDialog(m_cache, m_scrHvos.ToArray()))
			{
				dlg.PruneToSelectedSections(m_hvoRoot);
				dlg.Text = ITextStrings.ksExportInterlinearizedScripture;
				dlg.TreeViewLabel = ITextStrings.ksSelectSectionsExported;
				if (dlg.ShowDialog() == DialogResult.OK)
					m_hvos.AddRange(dlg.GetListOfIncludedSections());
			}
		}

		/// <summary>
		/// Export the data according to specifications.
		/// </summary>
		/// <param name="outPath"></param>
		protected override void DoExport(string outPath)
		{
			if (m_hvos.Count == 0)
				m_hvos.Add(m_hvoRoot);

			string fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			XmlNode ddNode = m_ddNodes[NodeIndex(fxtPath)];
			string mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			InterlinearExporter exporter;
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(this))
			{
				try
				{
					ExportPhase1(mode, out exporter, outPath);
					string rootDir = SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory;
					string transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					string sTransformPath = Path.Combine(rootDir, @"Language Explorer\Export Templates\Interlinear");
					switch (mode)
					{
						default:
							// no further processing needed.
							break;
						case "doNothing":
						case "xml":
						case "elan":
							// no further processing needed.
							break;
						case "applySingleTransform":
							string sTransform = Path.Combine(sTransformPath, transform);
							exporter.PostProcess(sTransform, outPath, 1);
							break;
						case "openOffice":
							string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							// Paranoia...probably GetRandomFileName will never make the name of an existing folder, but make sure of it.
							while (Directory.Exists(tempDir))
								tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							Directory.CreateDirectory(tempDir);
							try
							{
								XslCompiledTransform xsl = new XslCompiledTransform();
								XmlNode implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
								string styleFileTransform = "xml2OOStyles.xsl";
								if (implementation != null)
									styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation,
																							"styleTransform",
																							styleFileTransform);
								xsl.Load(rootDir + @"\Language Explorer\Export Templates\Interlinear\" +
										 styleFileTransform);
								xsl.Transform(outPath, Path.Combine(tempDir, "styles.xml"));

								// Now generate the content. Do this after using outPath as the source above, because it renames the file.
								string contentFileTransform = "xml2OO.xsl";
								if (implementation != null)
									contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation,
																							  "contentTransform",
																							  contentFileTransform);
								XslCompiledTransform xsl2 = new XslCompiledTransform();
								xsl.Load(rootDir + @"\Language Explorer\Export Templates\Interlinear\" +
										 contentFileTransform);
								xsl.Transform(outPath, Path.Combine(tempDir, "content.xml"));
								string mimetypePath = Path.Combine(tempDir, "mimetype");
								File.Copy(rootDir + @"\Language Explorer\Export Templates\Interlinear\mimetype",
										  mimetypePath);
								File.SetAttributes(mimetypePath, File.GetAttributes(mimetypePath) & ~FileAttributes.ReadOnly);
								string manifestDir = Path.Combine(tempDir, "META-INF");
								string manifestPath = Path.Combine(manifestDir, "manifest.xml");
								Directory.CreateDirectory(manifestDir);
								File.Copy(rootDir + @"\Language Explorer\Export Templates\Interlinear\manifest.xml",
										  manifestPath);
								File.SetAttributes(manifestPath, File.GetAttributes(manifestPath) & ~FileAttributes.ReadOnly);
								FastZip zf = new FastZip();
								zf.CreateZip(outPath, tempDir, true, string.Empty);
							}
							finally
							{
								Directory.Delete(tempDir, true);
							}
							//// Generate the styles first, based on the original export file.
							//string styleFilePath;
							//using (TempFileCollection tempFiles = new TempFileCollection()) // wanted only to get the default temp file dir and name
							//{
							//    styleFilePath = tempFiles.AddExtension("xml", false);
							//}
							//XslCompiledTransform xsl = new XslCompiledTransform();
							//XmlNode implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
							//string styleFileTransform = "xml2OOStyles.xsl";
							//if (implementation != null)
							//    styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "styleTransform", styleFileTransform);
							//xsl.Load(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + styleFileTransform);
							//xsl.Transform(outPath, styleFilePath);

							//// Now generate the content. Do this after using outPath as the source above, because it renames the file.
							//string contentFileTransform = "xml2OO.xsl";
							//if (implementation != null)
							//    contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "contentTransform", contentFileTransform);
							//exporter.PostProcess(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + contentFileTransform, outPath, 1);
							//string intermediateFile = CollectorEnv.RenameOutputToPassN(outPath, 2);
							//using (FileStream outFile = new FileStream(outPath, FileMode.Create))
							//{
							//    using (ZipOutputStream zipFile = new ZipOutputStream(outFile))
							//    {
							//        WriteFileToZipUncompressed("mimetype", rootDir + @"\Language Explorer\Export Templates\Interlinear\mimetype", zipFile);
							//        WriteFileToZipUncompressed("META-INF/manifest.xml", rootDir + @"\Language Explorer\Export Templates\Interlinear\manifest.xml", zipFile);
							//        WriteFileToZip("styles.xml", styleFilePath, zipFile);
							//        WriteFileToZip("content.xml", intermediateFile, zipFile);
							//        zipFile.Finish();
							//        zipFile.Close();
							//    }
							//    outFile.Close();
							//}
							//File.Delete(styleFilePath);
							//File.Delete(intermediateFile);
							//System.IO.File.Copy(rootDir + @"\Language Explorer\Export Templates\Interlinear\EmptyOfficeDoc.odt",
							//    outPath);
							//ZipFile OOFile = new ZipFile(outPath, ZipConstants.GZIP, System.IO.FileMode.Open);
							//System.IO.File.Delete("content.xml");
							//System.IO.File.Move(intermediateFile, "content.xml");
							//OOFile.Add("content.xml");
							////OOFile.AddAs("content.xml", intermediateFile);
							//OOFile.Close();
							break;
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(this, String.Format(ITextStrings.ksExportErrorMsg, e.Message));
				}
			}
			this.Close();
		}

		protected int NodeIndex(string tag)
		{
			string file = tag.Substring(tag.LastIndexOf('\\') + 1);
			for (int i = 0; i < m_ddNodes.Count; i++)
			{
				string fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
					return i;
			}
			return 0;
		}

		/// <summary>
		/// Attempt to write the file to the zip file uncompressed (this is especially useful for very small files).
		/// Unfortunately, the version of the #ZipLib we currently have doesn't seem to support this, so it
		/// will currently do a deflated version as usual.
		/// </summary>
		/// <param name="zipName"></param>
		/// <param name="pathName"></param>
		/// <param name="zipFile"></param>
		private static void WriteFileToZipUncompressed(string zipName, string pathName, ZipOutputStream zipFile)
		{
			ZipEntry entry = new ZipEntry(zipName);
			entry.CompressionMethod = CompressionMethod.Stored;
			WriteFileToZip(entry, pathName, zipFile);
		}

		private static void WriteFileToZip(string zipName, string pathName, ZipOutputStream zipFile)
		{
			WriteFileToZip(new ZipEntry(zipName), pathName, zipFile);
		}

		private static void WriteFileToZip(ZipEntry entry, string pathName, ZipOutputStream zipFile)
		{
			zipFile.PutNextEntry(entry);
			FileStream contentsFile = new FileStream(pathName, FileMode.Open, FileAccess.Read);
			int len = (int)contentsFile.Length;
			byte[] contents = new byte[contentsFile.Length];
			contentsFile.Read(contents, 0, len);
			zipFile.Write(contents, 0, len);
			contentsFile.Close();
		}

		internal bool ExportPhase1(string mode, out InterlinearExporter exporter, string fileName)
		{
			CheckDisposed();
			exporter = null;
			XmlWriter writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
			exporter = InterlinearExporter.Create(mode, m_cache, writer, m_hvos[0], m_vc.LineChoices, m_vc, m_tssTextName, m_tssTextAbbreviation);
			exporter.WriteBeginDocument();
			exporter.ExportDisplay();
			for (int i = 1; i < m_hvos.Count; ++i)
			{
				exporter.SetRootObject(m_hvos[i]);
				exporter.ExportDisplay();
			}
			exporter.WriteEndDocument();
			writer.Close();
			return true;
		}
	}
}
