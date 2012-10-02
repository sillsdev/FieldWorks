// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhoneEnvReferenceLauncher.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PhoneEnvReferenceLauncher : ReferenceLauncher
	{
		#region Data Members

		private PhoneEnvReferenceView m_phoneEnvRefView;
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the launcher to communicate size changes to the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Data Members

		#region Construction, Initialization, and Disposal

		public PhoneEnvReferenceLauncher()
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

			if ( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			m_phoneEnvRefView = null; // Disposed automatically, since it is in the Controls collection.

			base.Dispose( disposing );
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			Debug.Assert(obj is MoAffixAllomorph || obj is MoStemAllomorph);

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_phoneEnvRefView.Initialize((MoForm)obj, flid, cache);
		}

		#endregion // Construction, Initialization, and Disposal

		#region Overrides

		/// <summary>
		/// Get the mediator from the view.
		/// </summary>
		protected override XCore.Mediator Mediator
		{
			get { return m_phoneEnvRefView.Mediator; }
		}

		/// <summary>
		/// Overridden to provide a chooser with multiple selections (checkboxes and all).
		/// </summary>
		protected override SimpleListChooser GetChooser(ObjectLabelCollection labels)
		{
			return new SimpleListChooser(m_persistProvider, labels, m_fieldName, m_cache,
				m_cache.GetVectorProperty(m_obj.Hvo, m_flid, false));
		}

		/// <summary>
		/// Overridden to set the new values selected from the chooser dialog.
		/// </summary>
		/// <param name="rghvosChosen"></param>
		public override void SetItems(List<int> rghvosChosen)
		{
			CheckDisposed();
			m_phoneEnvRefView.Commit();
			// null indicates that we cancelled out of the chooser dialog -- we shouldn't get
			// here with that value, but just in case...
			if (rghvosChosen == null)
				return;
			int h1 = m_phoneEnvRefView.RootBox.Height;
			// First, we need a list of hvos added and a list of hvos deleted.
			int citemsOld = m_cache.GetVectorSize(m_obj.Hvo, m_flid);
			List<int> rghvosNew = new List<int>(rghvosChosen);
			List<int> rghvosDel = new List<int>(citemsOld);
			if (citemsOld > 0)
			{
				int[] hvosOld = m_cache.GetVectorProperty(m_obj.Hvo, m_flid, false);
				Debug.Assert(citemsOld == hvosOld.Length);
				for (int i = 0; i < citemsOld; ++i)
				{
					if (rghvosNew.Contains(hvosOld[i]))
						rghvosNew.Remove(hvosOld[i]);
					else
						rghvosDel.Add(hvosOld[i]);
				}
			}
			// Add all the new environments.
			using (new UndoRedoTaskHelper(m_cache,
				String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				String.Format(DetailControlsStrings.ksRedoSet, m_fieldName)))
			{
				for (int i = 0; i < rghvosNew.Count; ++i)
				{
					int hvo = (int)rghvosNew[i];
					m_phoneEnvRefView.AddNewItem(hvo);
					if (m_flid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition)
					{
						((MoAffixAllomorph)m_obj).PositionRS.Append(hvo);
					}
					else
					{
						if (m_obj is MoAffixAllomorph)
							((MoAffixAllomorph)m_obj).PhoneEnvRC.Add(hvo);
						else
							((MoStemAllomorph)m_obj).PhoneEnvRC.Add(hvo);
					}
				}
				for (int i = 0; i < rghvosDel.Count; ++i)
				{
					int hvo = (int)rghvosDel[i];
					m_phoneEnvRefView.RemoveItem(hvo);
					if (m_flid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition)
					{
						((MoAffixAllomorph)m_obj).PositionRS.Remove(hvo);
					}
					else
					{
						if (m_obj is MoAffixAllomorph)
							((MoAffixAllomorph)m_obj).PhoneEnvRC.Remove(hvo);
						else
							((MoStemAllomorph)m_obj).PhoneEnvRC.Remove(hvo);
					}
				}
			}
			int h2 = m_phoneEnvRefView.RootBox.Height;
			if (h1 != h2 && ViewSizeChanged != null)
			{
				ViewSizeChanged(this,
					new FwViewSizeEventArgs(h2, m_phoneEnvRefView.RootBox.Width));
			}
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			base.OnBackColorChanged(e);
			if (m_phoneEnvRefView != null)
			{
				m_phoneEnvRefView.BackColor = this.BackColor;
			}
		}

		/// <summary>
		/// Keep the view width equal to the launcher width minus the button width.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (this.m_panel != null && this.m_phoneEnvRefView != null)
			{
				int w = this.Width - this.m_panel.Width;
				this.m_phoneEnvRefView.Width = w > 0 ? w : 0;
			}
		}

		#endregion // Overrides

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_phoneEnvRefView = new PhoneEnvReferenceView();
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
			// m_phoneEnvRefView
			//
			this.m_phoneEnvRefView.AutoScroll = false;
			this.m_phoneEnvRefView.EditingHelper.DefaultCursor = null;
			this.m_phoneEnvRefView.Location = new System.Drawing.Point(0, 0);
			this.m_phoneEnvRefView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |AnchorStyles.Left;
			this.m_phoneEnvRefView.Name = "m_phoneEnvRefView";
			this.m_phoneEnvRefView.Size = new System.Drawing.Size(150, 20);
			this.m_phoneEnvRefView.TabIndex = 2;
			//
			// PhoneEnvReferenceLauncher
			//
			this.Controls.Add(this.m_phoneEnvRefView);
			this.MainControl = this.m_phoneEnvRefView;
			this.Name = "PhoneEnvReferenceLauncher";
			this.Size = new System.Drawing.Size(150, 20);
			this.Controls.SetChildIndex(this.m_phoneEnvRefView, 0);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
