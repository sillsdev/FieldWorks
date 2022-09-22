// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.XWorks;
using XCore;
using FileMode = System.IO.FileMode;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is a specialized MultiPane. It handles the RefreshDisplay differently to avoid crashes, and possibly to do a more efficient job
	/// then the base MultiPane would do.
	/// </summary>
	public class ConcordanceContainer : MultiPane, IRefreshableRoot
	{
		private RecordBrowseView WordOccuranceList => ReCurseControls<RecordBrowseView>(Panel1);


		public bool OnDisplayExportConcordanceResults(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true;
		}

		public void OnExportConcordanceResults(object arguments)
		{
			string fileName;
			using (var dlg = new SaveFileDialogAdapter())
			{
				dlg.AddExtension = true;
				dlg.DefaultExt = "csv";
				dlg.Filter = ITextStrings.ksConcordanceExportFilter;
				dlg.Title = ITextStrings.ksConcordanceExportTitle;
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
// ReSharper disable HeuristicUnreachableCode
// (because it's wrong)
			return false;
// ReSharper restore HeuristicUnreachableCode
		}

		/// <summary>
		/// This method will handle the RefreshDisplay calls for all the child controls of the ConcordanceContainer, the ConcordanceControl needs to be
		/// refreshed last because its interaction with the Mediator will update the other views, if it isn't called last then the caches and contents
		/// of the other views will be inconsistent with the ConcordanceControl and will lead to crashes or incorrect display behavior.
		/// </summary>
		/// <param name="parentControl">The control to Recurse</param>
		private T ReCurseControls<T>(Control parentControl) where T : Control
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
				if (cv != null)
					cv.ClearValues();
				var refreshable = control as IRefreshableRoot;
				bool childrenRefreshed = false;
				if (refreshable != null)
				{
					childrenRefreshed = refreshable.RefreshDisplay();
				}
				if(!childrenRefreshed)
				{
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