// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal sealed class MSADlgLauncherSlice : ViewSlice
	{
		private System.ComponentModel.IContainer components = null;

		/// <summary />
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				components?.Dispose();
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

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			var ctrl = (MSADlgLauncher)Control;
			if (ctrl.PropertyTable == null)
			{
				ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			}
			Size = new Size(208, 32);
			ctrl.Initialize(PropertyTable.GetValue<LcmCache>("cache"),
				Object,
				1, // Maybe need a real flid?
				"InterlinearName",
				ContainingDataTree.PersistenceProvder,
				"InterlinearName",
				XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
			var view = (MSADlglauncherView)ctrl.MainControl;
			view.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
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

		/// <summary />
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return (Control as MSADlgLauncher).MainControl as RootSite;
			}
		}

		/// <summary />
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
			var ctrl = (MSADlgLauncher)Control;
			(ctrl.MainControl as SimpleRootSite).AboutToDiscard();
		}
	}
}
