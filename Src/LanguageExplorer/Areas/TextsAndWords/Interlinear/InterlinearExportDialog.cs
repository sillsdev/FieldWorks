// Copyright (c) 2006-2018 SIL International
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
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Xml;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Summary description for InterlinearExportDialog.
	/// </summary>
	internal class InterlinearExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		ICmObject m_objRoot;
		InterlinVc m_vc;
		IText m_text;
		List<ICmObject> m_objs = new List<ICmObject>();
		private event EventHandler OnLaunchFilterScrScriptureSectionsDialog;

		public InterlinearExportDialog(ICmObject objRoot, InterlinVc vc)
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
			columnHeader1.Text = ITextStrings.ksFormat;
			columnHeader2.Text = ITextStrings.ksExtension;
			Text = ITextStrings.ksExportInterlinear;
			OnLaunchFilterScrScriptureSectionsDialog += LaunchFilterTextsDialog;
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
		/// If the selected text is from Scripture, pop up a selection dialog to allow the user to
		/// export more than one section (maybe even more than one book!) to a single file.
		/// </summary>
		/// <returns>true iff export can proceed to choosing an output file</returns>
		protected override bool PrepareForExport()
		{
			m_objs.Clear();
			OnLaunchFilterScrScriptureSectionsDialog?.Invoke(this, EventArgs.Empty);
			return m_objs.Count > 0;
		}

		/// <summary>
		/// Launch the appropriate dialog, depending on IsOkToDisplayScriptureIfPresent (currently always true).
		/// Note that this means even the SE edition of FW requires ScrControls.dll. This is the price of making
		/// even the SE edition able to work with Paratext, which we want to do because it was not obvious to
		/// users that they needed the BTE edition if using Paratext rather than TE.
		/// </summary>
		private void LaunchFilterTextsDialog(object sender, EventArgs args)
		{
			var interestingTextsList = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, m_cache.ServiceLocator);
			var textsToChooseFrom = new List<IStText>(interestingTextsList.InterestingTexts);
			var isOkToDisplayScripture = m_cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any();
			if (!isOkToDisplayScripture)
			{
				// Mustn't show any Scripture, so remove scripture from the list
				textsToChooseFrom = textsToChooseFrom.Where(text => !ScriptureServices.ScriptureIsResponsibleFor(text)).ToList();
			}
			var interestingTexts = textsToChooseFrom.ToArray();
			using (var dlg = new FilterTextsDialog(PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App), m_cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				// LT-12181: Was 'PruneToSelectedTexts(text) and most others were deleted.
				// We want 'PruneToInterestingTextsAndSelect(interestingTexts, selectedText)'
				dlg.PruneToInterestingTextsAndSelect(interestingTexts, (IStText)m_objRoot);
				dlg.TreeViewLabel = ITextStrings.ksSelectSectionsExported;
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_objs.AddRange(dlg.GetListOfIncludedTexts());
				}
			}
		}

		/// <summary>Export the data according to specifications.</summary>
		protected override void DoExport(string outPath)
		{
			using (var dlg = new ProgressDialogWithTask(this) { IsIndeterminate = true, AllowCancel = false, Message = ITextStrings.ksExporting_ })
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
			var fxtPath = (string)args[1];

			if (m_objs.Count == 0)
			{
				m_objs.Add(m_objRoot);
			}

			var ddNode = m_ddNodes[NodeIndex(fxtPath)];
			var mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			using (new WaitCursor(this))
			{
				try
				{
					InterlinearExporter exporter;
					ExportPhase1(mode, out exporter, outPath);
					var rootDir = FwDirectoryFinder.CodeDirectory;
					var transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					var sTransformPath = Path.Combine(rootDir, "Language Explorer", "Export Templates", "Interlinear");
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
							var sTransform = Path.Combine(sTransformPath, transform);
							exporter.PostProcess(sTransform, outPath, 1);
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
#pragma warning disable 219 // ReSharper disable UnusedVariable
								var xsl2 = new XslCompiledTransform();
#pragma warning restore 219 // ReSharper restore UnusedVariable
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
					MessageBox.Show(this, string.Format(ITextStrings.ksExportErrorMsg, e.Message));
				}
			}
			return null;
		}

		protected int NodeIndex(string pathname)
		{
			var file = Path.GetFileName(pathname);
			for (var i = 0; i < m_ddNodes.Count; i++)
			{
				var fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
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