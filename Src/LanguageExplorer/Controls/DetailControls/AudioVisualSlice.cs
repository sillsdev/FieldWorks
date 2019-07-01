// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A slice that displays a media file (CmMedia).
	/// </summary>
	internal class AudioVisualSlice : ViewSlice
	{
		private System.ComponentModel.IContainer components = null;

		/// <summary />
		public AudioVisualSlice()
		{
			InitializeComponent();
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// MsaInflectionFeatureListDlgLauncherSlice
			//
			this.Name = "AudioVisualSlice";
			this.Size = new System.Drawing.Size(208, 32);
		}
		#endregion

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				components?.Dispose();
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		public ICmMedia Media => (ICmMedia)MyCmObject;

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			Control = new AudioVisualLauncher();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parentDataTree"></param>
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			var ctrl = (AudioVisualLauncher)Control;
			ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			ctrl.Initialize(PropertyTable.GetValue<LcmCache>(FwUtils.cache), Media.MediaFileRA, CmFileTags.kflidInternalPath, "InternalPath",
				ContainingDataTree.PersistenceProvder, "InternalPath", "user");
		}

		/// <summary>
		/// Get the rootsite, which is embedded inside the control.
		/// </summary>
		public override RootSite RootSite => ((AudioVisualLauncher)Control).RootSite;

		/// <summary>
		/// Overridden because we have things to do when the control is set.
		/// </summary>
		public override Control Control
		{
			get
			{
				return base.Control;
			}
			set
			{
				base.Control = value;
				SimpleRootSite rs = RootSite;
				// Don't allow it to lay out until we have a realistic size, while the DataTree is
				// actually being laid out.
				rs.AllowLayout = false;
				// Embedded forms should not do their own scrolling. Rather we resize them as needed, and scroll the whole
				// DE view.
				rs.AutoScroll = false;
				rs.LayoutSizeChanged += HandleLayoutSizeChanged;
				// This is usually done by the DataTree method that creates and initializes slices.
				// However, for most view slices doing it before the control is set does no good.
				// On the other hand, we don't want to do it during the constructor, and have it done again
				// unnecessarily by this method (which the constructor calls).
				// In any case we can't do it until our node is set.
				// So, do it only if the node is known.
				if (ConfigurationNode != null)
				{
					OverrideBackColor(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "backColor"));
				}
			}
		}

		/// <summary>
		/// We need at least "20" for the button with the left arrow to look okay.
		/// </summary>
		public override int LabelHeight => Math.Max(20, Convert.ToInt32(m_fontLabel.GetHeight()));
	}
}