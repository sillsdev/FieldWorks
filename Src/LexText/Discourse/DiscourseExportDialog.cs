using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Discourse export dialog implements a dialog for exporting the discourse chart.
	/// Considerable refactoring is in order to share more code with InterlinearExportDialog,
	/// or move common code down to ExportDialog. This has been postponed in the interests
	/// of being able to release FW 5.2.1 without requiring changes to DLLs other than Discourse.
	/// </summary>
	public class DiscourseExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		int m_hvoRoot;
		IVwViewConstructor m_vc;
		int m_wsLineNumber;

		public DiscourseExportDialog(Mediator mediator, PropertyTable propertyTable, int hvoRoot, IVwViewConstructor vc,
			int wsLineNumber) : base(mediator, propertyTable)
		{
			m_hvoRoot = hvoRoot;
			m_vc = vc;

			m_helpTopic = "khtpExportDiscourse";
			columnHeader1.Text = DiscourseStrings.ksFormat;
			columnHeader2.Text = DiscourseStrings.ksExtension;
			Text = DiscourseStrings.ksExportDiscourse;
			m_wsLineNumber = wsLineNumber;
		}

		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Discourse",
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
		/// <param name="item"></param>
		/// <param name="ddNode"></param>
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			m_ddNodes.Add(ddNode);
			columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
			//columnHeader2.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		// Export the data according to specifications.
		// Prime candidate for refactoring, almost identical to base class method once we reinstate OO, as we
		// will want to. Main diffs are using a different class of exporter and a different directory path.
		protected override void DoExport(string outPath)
		{
			var fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			var ddNode = m_ddNodes[NodeIndex(fxtPath)];
			var mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			DiscourseExporter exporter;
			using (new WaitCursor(this))
			{
				try
				{
					ExportPhase1(out exporter, outPath);
					var rootDir = FwDirectoryFinder.CodeDirectory;
					var transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					var sTransformPath = Path.Combine(rootDir,
						String.Format("Language Explorer{0}Export Templates{0}Discourse",
						Path.DirectorySeparatorChar));
					switch (mode)
					{
						case "doNothing":
							break;
						case "applySingleTransform":
							string sTransform = Path.Combine(sTransformPath, transform);
							exporter.PostProcess(sTransform, outPath, 1);
							break;
						//case "openOffice":
						//    // Generate the styles first, based on the original export file.
						//    string styleFilePath;
						//    using (TempFileCollection tempFiles = new TempFileCollection()) // wanted only to get the default temp file dir and name
						//    {
						//        styleFilePath = tempFiles.AddExtension("xml", false);
						//    }
						//    XslCompiledTransform xsl = new XslCompiledTransform();
						//    XmlNode implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
						//    string styleFileTransform = "xml2OOStyles.xsl";
						//    if (implementation != null)
						//        styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "styleTransform", styleFileTransform);
						//    xsl.Load(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + styleFileTransform);
						//    xsl.Transform(outPath, styleFilePath);

						//    // Now generate the content. Do this after using outPath as the source above, because it renames the file.
						//    string contentFileTransform = "xml2OO.xsl";
						//    if (implementation != null)
						//        contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "contentTransform", contentFileTransform);
						//    exporter.PostProcess(rootDir + @"\Language Explorer\Export Templates\Interlinear\" + contentFileTransform, outPath, 1);
						//    string intermediateFile = CollectorEnv.RenameOutputToPassN(outPath, 2);
						//    using (FileStream outFile = new FileStream(outPath, FileMode.Create))
						//    {
						//        using (ZipOutputStream zipFile = new ZipOutputStream(outFile))
						//        {
						//            WriteFileToZipUncompressed("mimetype", rootDir + @"\Language Explorer\Export Templates\Interlinear\mimetype", zipFile);
						//            WriteFileToZipUncompressed("META-INF/manifest.xml", rootDir + @"\Language Explorer\Export Templates\Interlinear\manifest.xml", zipFile);
						//            WriteFileToZip("styles.xml", styleFilePath, zipFile);
						//            WriteFileToZip("content.xml", intermediateFile, zipFile);
						//            zipFile.Finish();
						//            zipFile.Close();
						//        }
						//        outFile.Close();
						//    }
						//    File.Delete(styleFilePath);
						//    File.Delete(intermediateFile);
						//    //System.IO.File.Copy(rootDir + @"\Language Explorer\Export Templates\Interlinear\EmptyOfficeDoc.odt",
						//    //    outPath);
						//    //ZipFile OOFile = new ZipFile(outPath, ZipConstants.GZIP, System.IO.FileMode.Open);
						//    //System.IO.File.Delete("content.xml");
						//    //System.IO.File.Move(intermediateFile, "content.xml");
						//    //OOFile.Add("content.xml");
						//    ////OOFile.AddAs("content.xml", intermediateFile);
						//    //OOFile.Close();
						//    break;
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(this, String.Format(DiscourseStrings.ksExportErrorMsg, e.Message));
				}
			}
			this.Close();
		}

		protected int NodeIndex(string tag)
		{
			var file = tag.Substring(tag.LastIndexOf('\\') + 1);
			for (var i = 0; i < m_ddNodes.Count; i++)
			{
				var fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
					return i;
			}
			return 0;
		}

		internal bool ExportPhase1(out DiscourseExporter exporter, string fileName)
		{
			CheckDisposed();

			using (var writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("document");
				exporter = new DiscourseExporter(m_cache, writer, m_hvoRoot, m_vc, m_wsLineNumber);
				exporter.ExportDisplay();
				writer.WriteEndElement();
				writer.WriteEndDocument();
				writer.Close();
			}
			return true;
		}
	}
}
