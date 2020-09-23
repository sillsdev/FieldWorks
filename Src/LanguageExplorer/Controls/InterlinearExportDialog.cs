// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using ICSharpCode.SharpZipLib.Zip;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Xml;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Controls
{
	/// <summary />
	internal sealed class InterlinearExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		private ICmObject m_objRoot;
		private InterlinVc m_vc;
		private IText m_text;
		private List<ICmObject> m_objs = new List<ICmObject>();

		internal InterlinearExportDialog(ICmObject objRoot, InterlinVc vc)
		{
			m_objRoot = objRoot;
			m_vc = vc;
		}

		#region Overrides of ExportDialog

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_helpTopic = "khtpExportInterlinear";
			columnHeader1.Text = LanguageExplorerResources.ksFormat;
			columnHeader2.Text = LanguageExplorerResources.ksExtension;
			Text = LanguageExplorerResources.ksExportInterlinear;
		}

		#endregion

		protected override string ConfigurationFilePath => $"Language Explorer{Path.DirectorySeparatorChar}Export Templates{Path.DirectorySeparatorChar}Interlinear";

		// Items in this version are never disabled.
		protected override bool ItemDisabled(string tag)
		{
			return false;
		}

		/// <summary>
		/// Override to do nothing since not configuring an FXT export process.
		/// </summary>
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			m_ddNodes.Add(ddNode);
			columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		/// <summary>
		/// Allow the user to export more than one to a single file (LT-11483)
		/// </summary>
		/// <returns>true iff export can proceed to choosing an output file</returns>
		protected override bool PrepareForExport()
		{
			m_objs.Clear();
			var interestingTextsList = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, m_cache.ServiceLocator);
			var textsToShow = interestingTextsList.InterestingTexts;
			var isOkToDisplayScripture = m_cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any();
			if (!isOkToDisplayScripture)
			{   // Mustn't show any Scripture, so remove scripture from the list
				textsToShow = textsToShow.Where(text => !ScriptureServices.ScriptureIsResponsibleFor(text));
			}
			var selectedTexts = new List<IStText> { (IStText)m_objRoot };
			using (var dlg = new FilterTextsDialog(PropertyTable.GetValue<IApp>("App"), m_cache, selectedTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.TextsToShow = textsToShow.ToList();
				dlg.TreeViewLabel = LanguageExplorerResources.ksSelectSectionsExported;
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_objs.AddRange(dlg.GetListOfIncludedTexts());
				}
			}
			return m_objs.Count > 0;
		}

		/// <summary>Export the data according to specifications.</summary>
		protected override void DoExport(string outPath)
		{
			using (var dlg = new ProgressDialogWithTask(this) { IsIndeterminate = true, AllowCancel = false, Message = LanguageExplorerResources.ksExporting_ })
			{
				try
				{
					var fxtPath = (string)m_exportList.SelectedItems[0].Tag; // read fxtPath here to prevent access to m_exportList on another thread
					dlg.RunTask(DoExportWithProgress, outPath, fxtPath);
				}
				finally
				{
					Close();
				}
			}
		}

		private object DoExportWithProgress(IThreadedProgress progressDlg, params object[] args)
		{
			var outPath = (string)args[0];
			if (m_objs.Count == 0)
			{
				m_objs.Add(m_objRoot);
			}
			var ddNode = m_ddNodes[NodeIndex((string)args[1])];
			var mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			using (new WaitCursor(this))
			{
				try
				{
					ExportPhase1(mode, out var exporter, outPath);
					var sTransformPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Export Templates", "Interlinear");
					switch (mode)
					{
						// ReSharper disable RedundantCaseLabel
						default:
						case "doNothing":
						case "xml":
						case "elan":
							// no further processing needed.
							break;
						// ReSharper restore RedundantCaseLabel
						case "applySingleTransform":
							exporter.PostProcess(Path.Combine(sTransformPath, XmlUtils.GetOptionalAttributeValue(ddNode, "transform", string.Empty)), outPath, 1);
							break;
						case "openOffice":
							var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							// Paranoia...probably GetRandomFileName will never make the name of an existing folder, but make sure of it.
							while (Directory.Exists(tempDir))
							{
								tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							}
							Directory.CreateDirectory(tempDir);
							try
							{
								var xsl = new XslCompiledTransform();
								var implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
								var styleFileTransform = "xml2OOStyles.xsl";
								if (implementation != null)
								{
									styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "styleTransform", styleFileTransform);
								}
								xsl.Load(Path.Combine(sTransformPath, styleFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "styles.xml"));
								// Now generate the content. Do this after using outPath as the source above, because it renames the file.
								var contentFileTransform = "xml2OO.xsl";
								if (implementation != null)
								{
									contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation, "contentTransform", contentFileTransform);
								}
								xsl.Load(Path.Combine(sTransformPath, contentFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "content.xml"));
								var mimetypePath = Path.Combine(tempDir, "mimetype");
								File.Copy(Path.Combine(sTransformPath, "mimetype"), mimetypePath);
								File.SetAttributes(mimetypePath, File.GetAttributes(mimetypePath) & ~FileAttributes.ReadOnly);
								var manifestDir = Path.Combine(tempDir, "META-INF");
								var manifestPath = Path.Combine(manifestDir, "manifest.xml");
								Directory.CreateDirectory(manifestDir);
								File.Copy(Path.Combine(sTransformPath, "manifest.xml"), manifestPath);
								File.SetAttributes(manifestPath, File.GetAttributes(manifestPath) & ~FileAttributes.ReadOnly);
								var zf = new FastZip();
								zf.CreateZip(outPath, tempDir, true, string.Empty);
							}
							finally
							{
								Directory.Delete(tempDir, true);
							}
							break;
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(this, string.Format(LanguageExplorerResources.ksExportErrorMsg, e.Message));
				}
			}
			return null;
		}

		private int NodeIndex(string pathname)
		{
			var filename = Path.GetFileName(pathname);
			for (var i = 0; i < m_ddNodes.Count; i++)
			{
				if (m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1) == filename)
				{
					return i;
				}
			}
			return 0;
		}

		internal bool ExportPhase1(string mode, out InterlinearExporter exporter, string fileName)
		{
			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};
			using (var writer = XmlWriter.Create(fileName, settings))
			{
				exporter = InterlinearExporter.Create(mode, m_cache, writer, m_objs[0], m_vc.LineChoices, m_vc);
				exporter.WriteBeginDocument();
				exporter.ExportDisplay();
				for (var i = 1; i < m_objs.Count; ++i)
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
