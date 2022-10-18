// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using DialogAdapters;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SILLanguageExplorer.Areas.TextsAndWords.Tools;
using FileMode = System.IO.FileMode;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This class is a specialized MultiPane. It handles the RefreshDisplay differently to avoid crashes, and possibly to do a more efficient job
	/// then the base MultiPane would do.
	/// </summary>
	internal class ConcordanceContainer : MultiPane, IRefreshableRoot
	{
		private RecordBrowseView WordOccuranceList => ReCurseControls<RecordBrowseView>(Panel1);

		/// <summary />
		internal ConcordanceContainer(MultiPaneParameters parameters)
			: base(parameters)
		{
		}
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			var concControl = ReCurseControls<ConcordanceControlBase>(this);
			var toolMenuHelper = new UserControlUiWidgetParameterObject(concControl);
			toolMenuHelper.MenuItemsForUserControl[MainMenu.File].Add(Command.CmdExportConcordanceResults, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(OnExportConcordanceResults, () => UiWidgetServices.CanSeeAndDo));
		}

		private void OnExportConcordanceResults(object sender, EventArgs e)
		{
			string fileName;
			using (var dlg = new SaveFileDialogAdapter())
			{
				dlg.AddExtension = true;
				dlg.DefaultExt = "csv";
				dlg.Filter = TextAndWordsResources.ksConcordanceExportFilter;
				dlg.Title = TextAndWordsResources.ksConcordanceExportTitle;
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;
				fileName = dlg.FileName;
			}
			DesktopAnalytics.Analytics.Track("ExportConcordanceResults", new Dictionary<string, string>());

			using (var fs = new FileStream(fileName, FileMode.Create))
			using (var textWriter = new StreamWriter(fs))
			{
				var exporter = new ConcordanceResultsExporter(textWriter,
				WordOccuranceList.BrowseViewer.BrowseView.Vc,
				WordOccuranceList.BrowseViewer.BrowseView.DataAccess,
				WordOccuranceList.BrowseViewer.BrowseView.RootObjectHvo);
				exporter.Export();
			}
		}

		public bool RefreshDisplay()
		{
			ConcordanceControlBase concordanceControl = ReCurseControls<ConcordanceControlBase>(this);
			if (concordanceControl != null)
			{
				concordanceControl.RefreshDisplay();
				return true;
			}
			Debug.Assert(concordanceControl != null, "ConcordanceContainer is missing the concordance control.");
			return false;
		}

		/// <summary>
		/// TODO: Justify this method's existance through testing. The comment below is historical, the new Pub/Sub system may not need this, or may have
		///       a better solution.
		/// This method will handle the RefreshDisplay calls for all the child controls of the ConcordanceContainer, the ConcordanceControl needs to be
		/// refreshed last because its interaction with the Mediator system will update the other views, if it isn't called last then the caches and contents
		/// of the other views could be inconsistent with the ConcordanceControl and will lead to crashes or incorrect display behavior.
		/// </summary>
		/// <param name="parentControl">The control to Recurse</param>
		private static T ReCurseControls<T>(Control parentControl) where T : Control
		{
			T concordanceControl = default(T);
			foreach (Control control in parentControl.Controls)
			{
				if (control is T)
				{
					concordanceControl = control as T;
					continue;
				}
				var cv = control as IClearValues;
				cv?.ClearValues();
				var childrenRefreshed = false;
				if (control is IRefreshableRoot refreshable)
				{
					childrenRefreshed = refreshable.RefreshDisplay();
				}
				if (!childrenRefreshed)
				{
					//Review: Randy had added a continue here...why?
					//Recurse into the child controls, make sure we only have one concordanceControl
					if(concordanceControl == null)
					{
						concordanceControl = ReCurseControls<T>(control);
					}
					else
					{
						var thereCanBeOnlyOne = ReCurseControls<T>(control);
						Debug.Assert(thereCanBeOnlyOne == null,
									 "Two concordance controls in the same window is not supported. One won't refresh properly.");
					}
				}
			}
			return concordanceControl;
		}
	}
}