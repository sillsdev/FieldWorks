// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class is a specialized MultiPane. It handles the RefreshDisplay differently to avoid crashes, and possibly to do a more efficient job
	/// then the base MultiPane would do.
	/// </summary>
	internal class ConcordanceContainer : MultiPane, IRefreshableRoot
	{
		/// <summary />
		internal ConcordanceContainer(MultiPaneParameters parameters)
			: base(parameters)
		{
		}

		public bool RefreshDisplay()
		{
			var concordanceControl = ReCurseControls(this);
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
		private static ConcordanceControlBase ReCurseControls(Control parentControl)
		{
			ConcordanceControlBase concordanceControl = null;
			foreach (Control control in parentControl.Controls)
			{
				if (control is ConcordanceControlBase)
				{
					concordanceControl = control as ConcordanceControlBase;
					continue;
				}
				var cv = control as IClearValues;
				cv?.ClearValues();
				var refreshable = control as IRefreshableRoot;
				var childrenRefreshed = false;
				if (refreshable != null)
				{
					childrenRefreshed = refreshable.RefreshDisplay();
				}
				if (childrenRefreshed)
				{
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
				}
			}
			return concordanceControl;
		}
	}
}