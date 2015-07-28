// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using XCore;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class MSADlgLauncherSlice : ViewSlice
	{
		private System.ComponentModel.IContainer components = null;

		public MSADlgLauncherSlice()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// MSADlgLauncherSlice
			//
			this.Name = "MSADlgLauncherSlice";

		}
		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			MSADlgLauncher ctrl = (MSADlgLauncher)Control;
			this.Size = new System.Drawing.Size(208, 32);
			ctrl.Initialize((FdoCache)Mediator.PropertyTable.GetValue("cache"),
				Object,
				1, // Maybe need a real flid?
				"InterlinearName",
				ContainingDataTree.PersistenceProvder,
				Mediator,
				"InterlinearName",
				XmlUtils.GetOptionalAttributeValue(m_configurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
			MSADlglauncherView view = ctrl.MainControl as MSADlglauncherView;
			view.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(Mediator);
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new MSADlgLauncher();
		}

		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return (Control as MSADlgLauncher).MainControl as RootSite;
			}
		}

		protected override int DesiredHeight(RootSite rs)
		{
			return Math.Max(base.DesiredHeight(rs), (Control as MSADlgLauncher).LauncherButton.Height);
		}

		/// <summary>
		/// Somehow a slice (I think one that has never scrolled to become visible?)
		/// can get an OnLoad message for its view in the course of deleting it from the
		/// parent controls collection. This can be bad (at best it's a waste of time
		/// to do the Layout in the OnLoad, but it can be actively harmful if the object
		/// the view is displaying has been deleted). So suppress it.
		/// </summary>
		public override void AboutToDiscard()
		{
			CheckDisposed();
			base.AboutToDiscard ();
			MSADlgLauncher ctrl = (MSADlgLauncher)Control;
			(ctrl.MainControl as SimpleRootSite).AboutToDiscard();
		}
	}
}
