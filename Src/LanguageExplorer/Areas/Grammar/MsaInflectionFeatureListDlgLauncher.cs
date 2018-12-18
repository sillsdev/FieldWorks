// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal class MsaInflectionFeatureListDlgLauncher : ButtonLauncher
	{
		protected MsaInflectionFeatureListDlgLauncherView m_msaInflectionFeatureListDlgLauncherView;
		private System.ComponentModel.IContainer components = null;

		/// <summary />
		public MsaInflectionFeatureListDlgLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			Height = m_panel.Height;
		}

		/// <summary>
		/// Initialize the launcher.
		/// </summary>
		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_msaInflectionFeatureListDlgLauncherView.Init(cache, obj as IFsFeatStruc);
		}

		/// <summary>
		/// Handle launching of the MSA editor.
		/// </summary>
		protected override void HandleChooser()
		{
			VectorReferenceLauncher vrl = null;
			using (var dlg = new MsaInflectionFeatureListDlg())
			{
				var originalFs = m_obj as IFsFeatStruc;
				var parentSlice = Slice;
				if (originalFs == null)
				{
					int owningFlid;
					var parentSliceClass = (int)parentSlice.MyCmObject.ClassID;
					switch (parentSliceClass)
					{
						case MoAffixAllomorphTags.kClassId:
							var allo = parentSlice.MyCmObject as IMoAffixAllomorph;
							owningFlid = (parentSlice as MsaInflectionFeatureListDlgLauncherSlice).Flid;
							dlg.SetDlgInfo(m_cache, PropertyTable, allo, owningFlid);
							break;
						default:
							var msa = parentSlice.MyCmObject as IMoMorphSynAnalysis;
							owningFlid = (parentSlice as MsaInflectionFeatureListDlgLauncherSlice).Flid;
							dlg.SetDlgInfo(m_cache, PropertyTable, msa, owningFlid);
							break;
					}
				}
				else
				{
					dlg.SetDlgInfo(m_cache, PropertyTable, originalFs, (parentSlice as MsaInflectionFeatureListDlgLauncherSlice).Flid);
				}
				const string ksPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='FeatureChooser']/";
				dlg.Text = StringTable.Table.GetStringWithXPath("InflectionFeatureTitle", ksPath);
				dlg.Prompt = StringTable.Table.GetStringWithXPath("InflectionFeaturePrompt", ksPath);
				dlg.LinkText = StringTable.Table.GetStringWithXPath("InflectionFeatureLink", ksPath);
				var result = dlg.ShowDialog(Slice.FindForm());
				switch (result)
				{
					case DialogResult.OK:
						// Note that this may set m_obj to null. dlg.FS will be null if all inflection features have been
						// removed. That is a valid state for this slice; m_obj deleted is not.
						m_obj = dlg.FS;
						m_msaInflectionFeatureListDlgLauncherView.Init(m_cache, dlg.FS);
						break;
					case DialogResult.Yes:
						// Get the VectorReferenceLauncher for the Inflection Features slice.
						// Since we're not changing tools, we want to change the chooser dialog.
						// See LT-5913 for motivation.
						var ctl = Parent;
						while (ctl != null && !(ctl is Slice))
						{
							ctl = ctl.Parent;
						}
						var slice = ctl as Slice;
						if (slice != null)
						{
							var dt = slice.ContainingDataTree;
							for (var i = 0; i < dt.Slices.Count; ++i)
							{
								var sliceT = dt.FieldOrDummyAt(i);
								vrl = sliceT.Control as VectorReferenceLauncher;
								if (vrl != null)
								{
									if (vrl.Flid == PartOfSpeechTags.kflidInflectableFeats)
									{
										break;
									}
									vrl = null;
								}
							}
						}
						if (vrl == null)
						{
							LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(AreaServices.PosEditMachineName, dlg.HighestPOS.Guid));
						}
						else
						{
							vrl.HandleExternalChooser();
						}
						break;
				}
			}
		}

		/// <summary />
		protected override void OnClick(object sender, EventArgs arguments)
		{
			HandleChooser();
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
			}
			base.Dispose(disposing);
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_msaInflectionFeatureListDlgLauncherView = new LanguageExplorer.Areas.Grammar.MsaInflectionFeatureListDlgLauncherView();
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
			// m_msaInflectionFeatureListDlgLauncherView
			//
			this.m_msaInflectionFeatureListDlgLauncherView.BackColor = System.Drawing.SystemColors.Window;
			this.m_msaInflectionFeatureListDlgLauncherView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_msaInflectionFeatureListDlgLauncherView.Group = null;
			this.m_msaInflectionFeatureListDlgLauncherView.Location = new System.Drawing.Point(0, 0);
			this.m_msaInflectionFeatureListDlgLauncherView.Name = "m_msaInflectionFeatureListDlgLauncherView";
			this.m_msaInflectionFeatureListDlgLauncherView.ReadOnlyView = false;
			this.m_msaInflectionFeatureListDlgLauncherView.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_msaInflectionFeatureListDlgLauncherView.ShowRangeSelAfterLostFocus = false;
			this.m_msaInflectionFeatureListDlgLauncherView.Size = new System.Drawing.Size(128, 24);
			this.m_msaInflectionFeatureListDlgLauncherView.SizeChangedSuppression = false;
			this.m_msaInflectionFeatureListDlgLauncherView.TabIndex = 0;
			this.m_msaInflectionFeatureListDlgLauncherView.WsPending = -1;
			this.m_msaInflectionFeatureListDlgLauncherView.Zoom = 1F;
			//
			// MsaInflectionFeatureListDlgLauncher
			//
			this.Controls.Add(this.m_msaInflectionFeatureListDlgLauncherView);
			this.MainControl = this.m_msaInflectionFeatureListDlgLauncherView;
			this.Name = "MsaInflectionFeatureListDlgLauncher";
			this.Size = new System.Drawing.Size(150, 24);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.Controls.SetChildIndex(this.m_msaInflectionFeatureListDlgLauncherView, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}