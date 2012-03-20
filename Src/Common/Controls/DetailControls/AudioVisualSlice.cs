// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AudioVisualSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// This file contains AudioVisualSlice, AudioVisualLauncher, AudioVisualView, and AudioVisualVc
// classes.  These encapsulate a CmMedia object for display in a detail view.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	#region AudioVisualSlice class
	/// <summary>
	/// A slice that displays a media file (CmMedia).
	/// </summary>
	public class AudioVisualSlice : ViewSlice
	{
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public AudioVisualSlice()
		{
			// This call is required by the Windows Form Designer.
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		public ICmMedia Media
		{
			get
			{
				CheckDisposed();
				return m_obj as ICmMedia;
			}
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new AudioVisualLauncher();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			AudioVisualLauncher ctrl = Control as AudioVisualLauncher;
			ctrl.Initialize(
				(FdoCache)Mediator.PropertyTable.GetValue("cache"),
				Media.MediaFileRA,
				CmFileTags.kflidInternalPath,
				"InternalPath",
				ContainingDataTree.PersistenceProvder,
				Mediator,
				"InternalPath",
				"user");
		}

		/// <summary>
		/// Get the rootsite, which is embedded inside the control.
		/// </summary>
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return (this.Control as AudioVisualLauncher).RootSite;
			}
		}

		/// <summary>
		/// Overridden because we have things to do when the control is set.
		/// </summary>
		public override Control Control
		{
			get
			{
				CheckDisposed();
				return base.Control;
			}
			set
			{
				CheckDisposed();
				base.Control = value;
				SimpleRootSite rs = RootSite;
				// Don't allow it to lay out until we have a realistic size, while the DataTree is
				// actually being laid out.
				rs.AllowLayout = false;

				// Embedded forms should not do their own scrolling. Rather we resize them as needed, and scroll the whole
				// DE view.
				rs.AutoScroll = false;
				rs.LayoutSizeChanged += new EventHandler(this.HandleLayoutSizeChanged);

				// This is usually done by the DataTree method that creates and initializes slices.
				// However, for most view slices doing it before the control is set does no good.
				// On the other hand, we don't want to do it during the constructor, and have it done again
				// unnecessarily by this method (which the constructor calls).
				// In any case we can't do it until our node is set.
				// So, do it only if the node is known.
				if (ConfigurationNode != null)
					OverrideBackColor(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "backColor"));
			}
		}

		/// <summary>
		/// We need at least "20" for the button with the left arrow to look okay.
		/// </summary>
		public override int LabelHeight
		{
			get
			{
				CheckDisposed();
				return Math.Max(20, Convert.ToInt32(m_fontLabel.GetHeight()));
			}
		}
	}
	#endregion AudioVisualSlice class

	#region AudioVisualLauncher class
	/// <summary>
	/// The button for launching the media player along with the view showing the filename
	/// </summary>
	public class AudioVisualLauncher : ButtonLauncher
	{
		private SIL.FieldWorks.Common.Framework.DetailControls.AudioVisualView m_view;
		private System.ComponentModel.IContainer components = null;
		private System.Media.SoundPlayer m_player = null;

		public AudioVisualLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			Height = m_panel.Height;
			this.m_btnLauncher.ImageIndex = 1;		// use LeftArrow ("Play") instead of ellipsis.
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_view = new SIL.FieldWorks.Common.Framework.DetailControls.AudioVisualView();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.Name = "m_panel";
			//
			// m_btnLauncher
			//
			this.m_btnLauncher.Name = "m_btnLauncher";
			//
			// m_view
			//
			this.m_view.BackColor = System.Drawing.SystemColors.Window;
			this.m_view.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_view.Group = null;
			this.m_view.Location = new System.Drawing.Point(0, 0);
			this.m_view.Mediator = null;
			this.m_view.Name = "m_view";
			this.m_view.ReadOnlyView = false;
			this.m_view.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_view.ShowRangeSelAfterLostFocus = false;
			this.m_view.Size = new System.Drawing.Size(250, 24);
			this.m_view.SizeChangedSuppression = false;
			this.m_view.TabIndex = 0;
			this.m_view.WsPending = -1;
			this.m_view.Zoom = 1F;
			//
			// AudioVisualLauncher
			//
			this.Controls.Add(this.m_view);
			this.MainControl = this.m_view;
			this.Name = "AudioVisualLauncher";
			this.Size = new System.Drawing.Size(250, 24);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.Controls.SetChildIndex(this.m_view, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_player != null)
				{
					m_player.Dispose();
					m_player = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="fieldName"></param>
		/// <param name="persistProvider"></param>
		/// <param name="mediator"></param>
		/// <param name="displayNameProperty"></param>
		/// <param name="displayWs"></param>
		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator,
			string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator,
				displayNameProperty, displayWs);
			m_view.Init(mediator, obj as ICmFile, flid);
		}
		/// <summary>
		/// Handle launching of the media player.
		/// </summary>
		protected override void HandleChooser()
		{
			var file = m_obj as ICmFile;
			// Open the file with Media Player or whatever the user has set up.
			try
			{
				string sPathname = FileUtils.ActualFilePath(file.AbsoluteInternalPath);
				if (IsWavFile(sPathname))
				{
					using (System.Media.SoundPlayer simpleSound = new System.Media.SoundPlayer(sPathname))
					{
						simpleSound.Play();
					}
				}
				else
				{
					using (System.Diagnostics.Process.Start(sPathname))
					{
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, DetailControlsStrings.ksNoPlayMedia);
			}
		}

		private bool IsWavFile(string sFilename)
		{
			// Look inside the file to verify whether it's a wav file.
			using (FileStream fs = File.OpenRead(sFilename))
			{
				int cbFile = (int)fs.Length;
				byte[] rgb = new byte[12];
				fs.Read(rgb, 0, 12);
				fs.Close();
				if (rgb[0] == 'R' && rgb[1] == 'I' && rgb[2] == 'F' && rgb[3] == 'F' &&
				rgb[8] == 'W' && rgb[9] == 'A' && rgb[10] == 'V' && rgb[11] == 'E')
				{
					int cbSize = rgb[4] + (rgb[5] << 8) + (rgb[6] << 16) + (rgb[7] << 24);
					return cbSize == cbFile - 8;
				}
				return false;
			}
		}

		public virtual RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return (RootSite)m_view;
			}
		}
	}
	#endregion // AudioVisualLauncher class

	#region AudioVisualView class
	/// <summary>
	/// The display of the media original file pathname.
	/// </summary>
	public class AudioVisualView : RootSiteControl
	{
		internal const int kfragPathname = 0;

		private System.ComponentModel.IContainer components = null;
		private ICmFile m_file;
		private int m_flid;
		private AudioVisualVc m_vc;

		public AudioVisualView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// MsaInflectionFeatureListDlgLauncherView
			//
			this.Name = "AudioVisualView";
			this.Size = new System.Drawing.Size(168, 24);

		}
		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			m_vc = null;
		}

		public void Init(Mediator mediator, ICmFile obj, int flid)
		{
			CheckDisposed();
			m_fdoCache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			m_file = obj;
			m_flid = flid;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_file != null)
			{
				m_rootb.SetRootObject(m_file.Hvo, m_vc, AudioVisualView.kfragPathname,
					m_rootb.Stylesheet);
				m_rootb.Reconstruct();
			}
		}

		#region RootSite required methods

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;
			m_vc = new AudioVisualVc(m_fdoCache, m_flid, "InternalPath");
			if (m_file != null)
			{
				m_rootb.SetRootObject(m_file.Hvo, m_vc, AudioVisualView.kfragPathname,
					m_rootb.Stylesheet);
			}

		}

		#endregion // RootSite required methods
	}
	#endregion AudioVisualView class

	#region AudioVisualVc class
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class AudioVisualVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;

		public AudioVisualVc(FdoCache cache, int flid, string displayNameProperty)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AudioVisualView.kfragPathname:
					// Display the filename.
					ILgWritingSystemFactory wsf =
						m_cache.WritingSystemFactory;
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					ITsString tss;
					ITsStrFactory tsf = m_cache.TsStrFactory;
					Debug.Assert(hvo != 0);
					Debug.Assert(m_cache != null);
					var file = m_cache.ServiceLocator.GetInstance<ICmFileRepository>().GetObject(hvo);
					Debug.Assert(file != null);
					string path = file.AbsoluteInternalPath;
					tss = tsf.MakeString(path, m_cache.WritingSystemFactory.UserWs);
					vwenv.OpenParagraph();
					vwenv.NoteDependency( new [] { m_cache.LangProject.Hvo, file.Hvo},
						new [] {LangProjectTags.kflidLinkedFilesRootDir, CmFileTags.kflidInternalPath}, 2);
					vwenv.AddString(tss);
					vwenv.CloseParagraph();
					break;

				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}
	}
	#endregion // AudioVisualVc class
}
