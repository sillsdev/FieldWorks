// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The button for launching the media player along with the view showing the filename
	/// </summary>
	internal class AudioVisualLauncher : ButtonLauncher
	{
		private AudioVisualView m_view;
		private IContainer components = null;
		private SoundPlayer m_player;

		internal AudioVisualLauncher()
		{
			InitializeComponent();
			Height = m_panel.Height;
			m_btnLauncher.ImageIndex = 1;
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_view = new LanguageExplorer.Controls.DetailControls.AudioVisualView();
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
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_view.InitializeFlexComponent(flexComponentParameters);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
				components?.Dispose();
				m_player?.Dispose();
			}
			m_player = null;

			base.Dispose(disposing);
		}

		///  <summary />
		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_view.Init(obj as ICmFile, flid);
		}

		/// <summary>
		/// Handle launching of the media player.
		/// </summary>
		protected override void HandleChooser()
		{
			var file = (ICmFile)m_obj;
			try
			{
				// Open the file with Media Player or whatever the user has set up.
				var sPathname = FileUtils.ActualFilePath(file.AbsoluteInternalPath);
				if (IsWavFile(sPathname))
				{
					using (var simpleSound = new System.Media.SoundPlayer(sPathname))
					{
						simpleSound.Play();
					}
				}
				else
				{
					using (Process.Start(sPathname))
					{
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, DetailControlsStrings.ksNoPlayMedia);
			}
		}

		private static bool IsWavFile(string sFilename)
		{
			// Look inside the file to verify whether it's a wav file.
			using (var fs = File.OpenRead(sFilename))
			{
				var cbFile = (int)fs.Length;
				var rgb = new byte[12];
				fs.Read(rgb, 0, 12);
				fs.Close();
				if (rgb[0] == 'R' && rgb[1] == 'I' && rgb[2] == 'F' && rgb[3] == 'F' && rgb[8] == 'W' && rgb[9] == 'A' && rgb[10] == 'V' && rgb[11] == 'E')
				{
					var cbSize = rgb[4] + (rgb[5] << 8) + (rgb[6] << 16) + (rgb[7] << 24);
					return cbSize == cbFile - 8;
				}
				return false;
			}
		}

		public virtual RootSite RootSite => m_view;
	}
}