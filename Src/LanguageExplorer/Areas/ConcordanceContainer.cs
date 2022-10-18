// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
using LanguageExplorer.Controls;
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
using SIL.FieldWorks.Common.Controls;
=======
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.XWorks;
using XCore;
using FileMode = System.IO.FileMode;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This class is a specialized MultiPane. It handles the RefreshDisplay differently to avoid crashes, and possibly to do a more efficient job
	/// then the base MultiPane would do.
	/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
	internal class ConcordanceContainer : MultiPane, IRefreshableRoot
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
	public class ConcordanceContainer : XCore.MultiPane, IRefreshableRoot
=======
	public class ConcordanceContainer : MultiPane, IRefreshableRoot
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
	{
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
		/// <summary />
		internal ConcordanceContainer(MultiPaneParameters parameters)
			: base(parameters)
		{
		}

||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
=======
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

>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
		public bool RefreshDisplay()
		{
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
			var concordanceControl = ReCurseControls(this);
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
			ConcordanceControlBase concordanceControl = ReCurseControls(this);
=======
			ConcordanceControlBase concordanceControl = ReCurseControls<ConcordanceControlBase>(this);
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
			if (concordanceControl != null)
			{
				concordanceControl.RefreshDisplay();
				return true;
			}
			Debug.Assert(concordanceControl != null, "ConcordanceContainer is missing the concordance control.");
			return false;
		}

		/// <summary>
		/// This method will handle the RefreshDisplay calls for all the child controls of the ConcordanceContainer, the ConcordanceControl needs to be
		/// refreshed last because its interaction with the Mediator will update the other views, if it isn't called last then the caches and contents
		/// of the other views will be inconsistent with the ConcordanceControl and will lead to crashes or incorrect display behavior.
		/// </summary>
		/// <param name="parentControl">The control to Recurse</param>
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
		private static ConcordanceControlBase ReCurseControls(Control parentControl)
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
		private ConcordanceControlBase ReCurseControls(Control parentControl)
=======
		private T ReCurseControls<T>(Control parentControl) where T : Control
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
		{
			T concordanceControl = default(T);
			foreach (Control control in parentControl.Controls)
			{
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
				if (control is ConcordanceControlBase concordanceControlBase)
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
				if (control is ConcordanceControlBase)
=======
				if (control is T)
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
				{
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
					concordanceControl = concordanceControlBase;
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
					concordanceControl = control as ConcordanceControlBase;
=======
					concordanceControl = control as T;
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
					continue;
				}
				var cv = control as IClearValues;
				cv?.ClearValues();
				var childrenRefreshed = false;
				if (control is IRefreshableRoot refreshable)
				{
					childrenRefreshed = refreshable.RefreshDisplay();
				}
				if (childrenRefreshed)
				{
<<<<<<< HEAD:Src/LanguageExplorer/Areas/ConcordanceContainer.cs
					continue;
				}
				//Recurse into the child controls, make sure we only have one concordanceControl
				if (concordanceControl == null)
				{
					concordanceControl = ReCurseControls(control);
				}
				else
				{
					var thereCanBeOnlyOne = ReCurseControls(control);
					Debug.Assert(thereCanBeOnlyOne == null, "Two concordance controls in the same window is not supported. One won't refresh properly.");
||||||| f013144d5:Src/LexText/Interlinear/ConcordanceContainer.cs
					//Recurse into the child controls, make sure we only have one concordanceControl
					if(concordanceControl == null)
					{
						concordanceControl = ReCurseControls(control);
					}
					else
					{
						var thereCanBeOnlyOne = ReCurseControls(control);
						Debug.Assert(thereCanBeOnlyOne == null,
									 "Two concordance controls in the same window is not supported. One won't refresh properly.");
					}
=======
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
>>>>>>> develop:Src/LexText/Interlinear/ConcordanceContainer.cs
				}
			}
			return concordanceControl;
		}
	}
}