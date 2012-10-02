using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class provides for required behavior when a RootSite class is used as a control on a form.
	/// Specifically, it deals with Tabbing issues.
	/// </summary>
	public class RootSiteControl : RootSite
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RootSiteControl() : base(null)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// RootSiteControl should handle tabs like a control
			AcceptsTab = false;
			//this.VScroll = false; // no vertical scroll bar visible.
			//this.AutoScroll = false; // not even if the root box is bigger than the window.
			SuppressPrintHandling = true; // Controls shouldn't handle OnPrint.
		}

		#region Control handling methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we get focus, start filtering messages to catch characters
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			if (Parent is IContainerControl)
			{
				IContainerControl uc = (IContainerControl)Parent;
				uc.ActiveControl = this;
			}
			base.OnGotFocus(e);
			try
			{
				if (m_rootb.Selection == null)
					//!!!!!!!!!!!!! if you stop here on an exception, it's
					//just because you have the "break into debugger" set in the Debug:exceptions menu.
					//!!!!!!!!!!!!!!!
					m_rootb.MakeSimpleSel(true, true, false, true); // Add IP.
			}
			catch
			{ /* Do nothing, if there isn't a selection. */ }
		}

		#endregion Control Handling methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose( disposing );

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
