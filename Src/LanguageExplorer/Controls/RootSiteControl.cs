// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This class provides for required behavior when a RootSite class is used as a control on a form.
	/// Specifically, it deals with Tabbing issues.
	/// </summary>
	internal class RootSiteControl : RootSite
	{
		private Container components;

		/// <summary />
		public RootSiteControl() : base(null)
		{
			InitializeComponent();
			// RootSiteControl should handle tabs like a control
			AcceptsTab = false;
			SuppressPrintHandling = true; // Controls shouldn't handle OnPrint.
		}

		#region Control handling methods

		/// <summary>
		/// When we get focus, start filtering messages to catch characters
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			if (Parent is IContainerControl)
			{
				var uc = (IContainerControl)Parent;
				uc.ActiveControl = this;
			}
			base.OnGotFocus(e);
			try
			{
				if (RootBox.Selection == null)
				{
					//!!!!!!!!!!!!! if you stop here on an exception, it's
					//just because you have the "break into debugger" set in the Debug:exceptions menu.
					//!!!!!!!!!!!!!!!
					RootBox.MakeSimpleSel(true, true, false, true); // Add IP.
				}
			}
			catch
			{ /* Do nothing, if there isn't a selection. */ }
		}

		#endregion Control Handling methods

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new Container();
		}
		#endregion
	}
}