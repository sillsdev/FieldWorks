// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal sealed class MSADlgLauncher : ButtonLauncher
	{
		private MSADlglauncherView m_msaDlglauncherView;
		private System.ComponentModel.IContainer components = null;

		/// <summary />
		public MSADlgLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			Height = m_panel.Height;
		}

		/// <summary>
		/// Initialize the launcher.
		/// </summary>
		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName,
			IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_msaDlglauncherView.Init(PropertyTable.GetValue<FdoCache>("cache"), obj as IMoMorphSynAnalysis);
		}

		/// <summary>
		/// Handle launching of the MSA editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				IMoMorphSynAnalysis originalMsa = m_obj as IMoMorphSynAnalysis;
				ILexEntry entry = originalMsa.Owner as ILexEntry;
				dlg.SetDlgInfo(m_cache,
					m_persistProvider,
					PropertyTable,
					Publisher,
					entry,
					SandboxGenericMSA.Create(originalMsa),
					originalMsa.Hvo,
					true,
					String.Format(LanguageExplorerResources.ksEditX, Slice.Label));
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					SandboxGenericMSA sandboxMsa = dlg.SandboxMSA;
					if (!originalMsa.EqualsMsa(sandboxMsa))
					{
						UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoEditFunction, LanguageExplorerResources.ksRedoEditFunction, entry, () =>
						{
							originalMsa.UpdateOrReplace(sandboxMsa);
						});
					}
				}
			}
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
			this.m_msaDlglauncherView = new LanguageExplorer.Areas.Grammar.MSADlglauncherView();
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
			// m_msaDlglauncherView
			//
			this.m_msaDlglauncherView.BackColor = System.Drawing.SystemColors.Window;
			this.m_msaDlglauncherView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_msaDlglauncherView.Group = null;
			this.m_msaDlglauncherView.Location = new System.Drawing.Point(0, 0);
			this.m_msaDlglauncherView.Name = "m_msaDlglauncherView";
			this.m_msaDlglauncherView.ReadOnlyView = false;
			this.m_msaDlglauncherView.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_msaDlglauncherView.ShowRangeSelAfterLostFocus = false;
			this.m_msaDlglauncherView.Size = new System.Drawing.Size(128, 24);
			this.m_msaDlglauncherView.SizeChangedSuppression = false;
			this.m_msaDlglauncherView.TabIndex = 0;
			this.m_msaDlglauncherView.WsPending = -1;
			this.m_msaDlglauncherView.Zoom = 1F;
			//
			// MSADlgLauncher
			//
			this.Controls.Add(this.m_msaDlglauncherView);
			this.MainControl = this.m_msaDlglauncherView;
			this.Name = "MSADlgLauncher";
			this.Size = new System.Drawing.Size(150, 24);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.Controls.SetChildIndex(this.m_msaDlglauncherView, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
