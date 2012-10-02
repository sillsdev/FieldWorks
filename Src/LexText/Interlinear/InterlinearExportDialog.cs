using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Xml.Xsl; // Bizarre location for TempFileCollection
using System.IO;
using System.Xml;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for InterlinearExportDialog.
	/// </summary>
	public class InterlinearExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		ICmObject m_objRoot;
		InterlinVc m_vc;
		FDO.IText m_text;
		List<ICmObject> m_objs = new List<ICmObject>();
		private List<ICmObject> m_scrObjs;
		private event EventHandler OnLaunchFilterScrScriptureSectionsDialog;

		public InterlinearExportDialog(Mediator mediator, ICmObject objRoot, InterlinVc vc, List<ICmObject> scrObjs)
			: base(mediator)
		{
			m_objRoot = objRoot;
			m_vc = vc;
			m_scrObjs = scrObjs;

			m_helpTopic = "khtpExportInterlinear";
			columnHeader1.Text = ITextStrings.ksFormat;
			columnHeader2.Text = ITextStrings.ksExtension;
			Text = ITextStrings.ksExportInterlinear;
			if (FwUtils.IsTEInstalled)
				OnLaunchFilterScrScriptureSectionsDialog += LaunchFilterScrScriptureSectionsDialog;
		}

		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Interlinear",
				Path.DirectorySeparatorChar); }
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
			m_objs.Clear();
			if (m_objRoot.OwningFlid != TextTags.kflidContents)
			{
				Debug.Assert(m_scrObjs != null && m_scrObjs.Count > 0);
				if (m_scrObjs != null && m_scrObjs.Count > 0)
				{
					if (OnLaunchFilterScrScriptureSectionsDialog != null)
						OnLaunchFilterScrScriptureSectionsDialog(this, EventArgs.Empty);
				}
				return m_objs.Count > 0;
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
			IFilterScrSectionDialog<IStText> dlg = null;
			try
			{
				dlg = (IFilterScrSectionDialog<IStText>)DynamicLoader.CreateObject(
					"ScrControls.dll", "SIL.FieldWorks.Common.Controls.FilterScrSectionDialog",
					m_cache, m_scrObjs.Cast<IStText>().ToArray(), m_mediator.HelpTopicProvider);
				dlg.PruneToSelectedSections((IStText)m_objRoot);
				dlg.Text = ITextStrings.ksExportInterlinearizedScripture;
				dlg.TreeViewLabel = ITextStrings.ksSelectSectionsExported;
				if (dlg.ShowDialog() == DialogResult.OK)
					m_objs.AddRange(dlg.GetListOfIncludedScripture());
			}
			finally
			{
				if (dlg != null)
					((IDisposable)dlg).Dispose();
			}
		}

		/// <summary>
		/// Export the data according to specifications.
		/// </summary>
		/// <param name="outPath"></param>
		protected override void DoExport(string outPath)
		{
			if (m_objs.Count == 0)
				m_objs.Add(m_objRoot);

			string fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			XmlNode ddNode = m_ddNodes[NodeIndex(fxtPath)];
			string mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			InterlinearExporter exporter;
			using (new SIL.Utils.WaitCursor(this))
			{
				try
				{
					ExportPhase1(mode, out exporter, outPath);
					string rootDir = DirectoryFinder.FWCodeDirectory;
					string transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					string sTransformPath = Path.Combine(rootDir,
							String.Format("Language Explorer{0}Export Templates{0}Interlinear",
							Path.DirectorySeparatorChar));
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
								xsl.Load(Path.Combine(sTransformPath, styleFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "styles.xml"));

								// Now generate the content. Do this after using outPath as the source above, because it renames the file.
								string contentFileTransform = "xml2OO.xsl";
								if (implementation != null)
									contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation,
																							  "contentTransform",
																							  contentFileTransform);
#pragma warning disable 219
								XslCompiledTransform xsl2 = new XslCompiledTransform();
#pragma warning restore 219
								xsl.Load(Path.Combine(sTransformPath, contentFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "content.xml"));
								string mimetypePath = Path.Combine(tempDir, "mimetype");
								File.Copy(Path.Combine(sTransformPath, "mimetype"),
										  mimetypePath);
								File.SetAttributes(mimetypePath, File.GetAttributes(mimetypePath) & ~FileAttributes.ReadOnly);
								string manifestDir = Path.Combine(tempDir, "META-INF");
								string manifestPath = Path.Combine(manifestDir, "manifest.xml");
								Directory.CreateDirectory(manifestDir);
								File.Copy(Path.Combine(sTransformPath, "manifest.xml"),
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
							//	styleFilePath = tempFiles.AddExtension("xml", false);
							//}
							//XslCompiledTransform xsl = new XslCompiledTransform();
							//XmlNode implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
							//string styleFileTransform = "xml2OOStyles.xsl";
							//if (implementation != null)
							//	styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "styleTransform", styleFileTransform);
							//xsl.Load(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + styleFileTransform);
							//xsl.Transform(outPath, styleFilePath);

							//// Now generate the content. Do this after using outPath as the source above, because it renames the file.
							//string contentFileTransform = "xml2OO.xsl";
							//if (implementation != null)
							//	contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "contentTransform", contentFileTransform);
							//exporter.PostProcess(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + contentFileTransform, outPath, 1);
							//string intermediateFile = CollectorEnv.RenameOutputToPassN(outPath, 2);
							//using (FileStream outFile = new FileStream(outPath, FileMode.Create))
							//{
							//	using (ZipOutputStream zipFile = new ZipOutputStream(outFile))
							//	{
							//		WriteFileToZipUncompressed("mimetype", rootDir + @"\Language Explorer\Export Templates\Interlinear\mimetype", zipFile);
							//		WriteFileToZipUncompressed("META-INF/manifest.xml", rootDir + @"\Language Explorer\Export Templates\Interlinear\manifest.xml", zipFile);
							//		WriteFileToZip("styles.xml", styleFilePath, zipFile);
							//		WriteFileToZip("content.xml", intermediateFile, zipFile);
							//		zipFile.Finish();
							//		zipFile.Close();
							//	}
							//	outFile.Close();
							//}
							//File.Delete(styleFilePath);
							//File.Delete(intermediateFile);
							//System.IO.File.Copy(rootDir + @"\Language Explorer\Export Templates\Interlinear\EmptyOfficeDoc.odt",
							//	outPath);
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

		protected int NodeIndex(string pathname)
		{
			string file = Path.GetFileName(pathname);
			for (int i = 0; i < m_ddNodes.Count; i++)
			{
				string fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
					return i;
			}
			return 0;
		}

#if false // CS0169
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
#endif

		internal bool ExportPhase1(string mode, out InterlinearExporter exporter, string fileName)
		{
			CheckDisposed();
			exporter = null;
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = System.Text.Encoding.UTF8;
			settings.Indent = false;
			using (XmlWriter writer = XmlTextWriter.Create(fileName, settings))
			{
				exporter = InterlinearExporter.Create(mode, m_cache, writer, m_objs[0], m_vc.LineChoices, m_vc);
				exporter.WriteBeginDocument();
				exporter.ExportDisplay();
				for (int i = 1; i < m_objs.Count; ++i)
				{
					exporter.SetRootObject(m_objs[i]);
					exporter.ExportDisplay();
				}
				exporter.WriteEndDocument();
				writer.Close();
				return true;
			}
		}
	}
}
