using System;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.XWorks.MorphologyEditor;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class PhonologicalFeatureListDlgLauncher : ButtonLauncher
	{
		private SIL.FieldWorks.XWorks.LexEd.PhonologicalFeatureListDlgLauncherView m_PhonologicalFeatureListDlgLauncherView;
		private System.ComponentModel.IContainer components = null;

		public PhonologicalFeatureListDlgLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Initialize the launcher.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="fieldName"></param>
		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName,
			IPersistenceProvider persistProvider, Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_PhonologicalFeatureListDlgLauncherView.Init(mediator, obj as FsFeatStruc);
			if (Slice.Object.ClassID == PhPhoneme.kclsidPhPhoneme)
				m_PhonologicalFeatureListDlgLauncherView.Phoneme = Slice.Object as IPhPhoneme;
		}

		/// <summary>
		/// Handle launching of the phonological feature editor.
		/// </summary>
		protected override void HandleChooser()
		{
			VectorReferenceLauncher vrl = null;
			using (PhonologicalFeatureChooserDlg dlg = new PhonologicalFeatureChooserDlg())
			{
				IFsFeatStruc originalFs = null;
				Slice parentSlice = Slice;
				int parentSliceClass = parentSlice.Object.ClassID;
				int owningFlid = (parentSlice as PhonologicalFeatureListDlgLauncherSlice).Flid;
				switch (parentSliceClass)
				{
					case PhPhoneme.kclsidPhPhoneme:
						IPhPhoneme phoneme = parentSlice.Object as IPhPhoneme;
						if (phoneme.FeaturesOAHvo != 0)
							originalFs = phoneme.FeaturesOA;
						break;
					case PhNCFeatures.kclsidPhNCFeatures:
						IPhNCFeatures features = parentSlice.Object as IPhNCFeatures;
						if (features.FeaturesOAHvo != 0)
							originalFs = features.FeaturesOA;
						break;
				}

				int longNameOldLen = 0;
				if (originalFs != null && originalFs.LongName != null)
					longNameOldLen = originalFs.LongName.Length;

				if (originalFs == null)
					dlg.SetDlgInfo(m_cache, m_mediator, parentSlice.Object, owningFlid);
				else
					dlg.SetDlgInfo(m_cache, m_mediator, originalFs);

				DialogResult result = dlg.ShowDialog(parentSlice.FindForm());
				if (result == DialogResult.OK)
				{
					if (dlg.FS != null)
					{
						int tagLongName = m_cache.VwCacheDaAccessor.GetVirtualHandlerName("FsFeatStruc", "LongNameTSS").Tag;
						m_obj = dlg.FS;
						m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, parentSlice.Object.Hvo, owningFlid,
											  0, 1, 0);
						m_PhonologicalFeatureListDlgLauncherView.UpdateFS(dlg.FS);
						dlg.FS.UpdateFeatureLongName(tagLongName, longNameOldLen);
					}
				}
				else if (result != DialogResult.Cancel)
				{
					if (vrl == null)
					{
						dlg.HandleJump();
					}
					else
					{
						vrl.HandleExternalChooser();
					}
				}
			}
		}

		protected override void OnClick(Object sender, EventArgs arguments)
		{
			HandleChooser();
		}

		/// <summary>
		/// Get the mediator.
		/// </summary>
		protected override XCore.Mediator Mediator
		{
			get { return m_mediator; }
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
			this.m_PhonologicalFeatureListDlgLauncherView = new SIL.FieldWorks.XWorks.LexEd.PhonologicalFeatureListDlgLauncherView();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_PhonologicalFeatureListDlgLauncherView
			//
			this.m_PhonologicalFeatureListDlgLauncherView.BackColor = System.Drawing.SystemColors.Window;
			this.m_PhonologicalFeatureListDlgLauncherView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_PhonologicalFeatureListDlgLauncherView.DoSpellCheck = false;
			this.m_PhonologicalFeatureListDlgLauncherView.Group = null;
			this.m_PhonologicalFeatureListDlgLauncherView.IsTextBox = false;
			this.m_PhonologicalFeatureListDlgLauncherView.Location = new System.Drawing.Point(0, 0);
			this.m_PhonologicalFeatureListDlgLauncherView.Mediator = null;
			this.m_PhonologicalFeatureListDlgLauncherView.Name = "m_PhonologicalFeatureListDlgLauncherView";
			this.m_PhonologicalFeatureListDlgLauncherView.Phoneme = null;
			this.m_PhonologicalFeatureListDlgLauncherView.ReadOnlyView = false;
			this.m_PhonologicalFeatureListDlgLauncherView.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_PhonologicalFeatureListDlgLauncherView.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_PhonologicalFeatureListDlgLauncherView.ShowRangeSelAfterLostFocus = false;
			this.m_PhonologicalFeatureListDlgLauncherView.Size = new System.Drawing.Size(130, 24);
			this.m_PhonologicalFeatureListDlgLauncherView.SizeChangedSuppression = false;
			this.m_PhonologicalFeatureListDlgLauncherView.TabIndex = 0;
			this.m_PhonologicalFeatureListDlgLauncherView.WritingSystemFactory = null;
			this.m_PhonologicalFeatureListDlgLauncherView.WsPending = -1;
			this.m_PhonologicalFeatureListDlgLauncherView.Zoom = 1F;
			//
			// PhonologicalFeatureListDlgLauncher
			//
			this.Controls.Add(this.m_PhonologicalFeatureListDlgLauncherView);
			this.MainControl = this.m_PhonologicalFeatureListDlgLauncherView;
			this.Name = "PhonologicalFeatureListDlgLauncher";
			this.Size = new System.Drawing.Size(150, 24);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.Controls.SetChildIndex(this.m_PhonologicalFeatureListDlgLauncherView, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
