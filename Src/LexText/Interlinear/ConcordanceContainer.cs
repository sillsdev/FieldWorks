using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is a specialized MultiPane. It handles the RefreshDisplay differently to avoid crashes, and possibly to do a more efficient job
	/// then the base MultiPane would do.
	/// </summary>
	public class ConcordanceContainer : XCore.MultiPane, IRefreshableRoot
	{
		public bool RefreshDisplay()
		{
			ConcordanceControl concordanceControl = ReCurseControls(this);
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
		/// of the other views will be inconsistant with the ConcordanceControl and will lead to crashes or incorrect display behavior.
		/// </summary>
		/// <param name="parentControl">The control to Recurse</param>
		private ConcordanceControl ReCurseControls(Control parentControl)
		{
			ConcordanceControl concordanceControl = null;
			foreach (Control control in parentControl.Controls)
			{
				if (control is ConcordanceControl)
				{
					concordanceControl = control as ConcordanceControl;
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
						concordanceControl = ReCurseControls(control);
					}
					else
					{
						var thereCanBeOnlyOne = ReCurseControls(control);
						Debug.Assert(thereCanBeOnlyOne == null,
									 "Two concordance controls in the same window is not supported. One won't refresh properly.");
					}
				}
			}
			return concordanceControl;
		}
	}
}