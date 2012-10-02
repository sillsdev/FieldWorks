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
// File: VectorReferenceLauncher.cs
// Responsibility: Steve McConnel (was RandyR)
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
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class VectorReferenceLauncher : ReferenceLauncher
	{
		#region Data Members

		private System.ComponentModel.IContainer components = null;
		protected VectorReferenceView m_vectorRefView;

		/// <summary>
		/// This allows the launcher to communicate size changes to the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Data Members

		#region Construction, Initialization, and Disposal

		public VectorReferenceLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

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
			}
			m_vectorRefView = null; // Should all be disposed automatically, since it is in the Controls collection.

			base.Dispose(disposing);
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator,
			string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_vectorRefView.Initialize(obj, flid, cache, displayNameProperty, mediator, displayWs);
		}

		#endregion // Construction, Initialization, and Disposal

		#region Overrides

		/// <summary>
		/// Get the mediator from the view.
		/// </summary>
		protected override XCore.Mediator Mediator
		{
			get { return m_vectorRefView.Mediator; }
		}

		/// <summary>
		/// Allow our chooser to be invoked from another slice.  See LT-5913 for
		/// motivation.
		/// </summary>
		public void HandleExternalChooser()
		{
			HandleChooser();
		}

		/// <summary>
		/// Make our field id accessible to other slices, again to support LT-5913.
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
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
			// null indicates that we cancelled out of the chooser dialog -- we shouldn't get
			// here with that value, but just in case...
			if (rghvosChosen == null)
				return;
			int h1 = m_vectorRefView.RootBox.Height;
			int citemsOld = m_cache.GetVectorSize(m_obj.Hvo, m_flid);
			// Find out whether we're adding or deleting items.
			bool fChange = false;
			if (citemsOld != rghvosChosen.Count)
			{
				fChange = true;
			}
			else if (citemsOld > 0)
			{
				int[] hvosOld = m_cache.GetVectorProperty(m_obj.Hvo, m_flid, false);
				Debug.Assert(citemsOld == hvosOld.Length);
				// First check whether we're deleting any items (that data is already in place).
				for (int i = 0; i < hvosOld.Length; ++i)
				{
					if (!rghvosChosen.Contains(hvosOld[i]))
					{
						fChange = true;
						break;
					}
				}
				if (!fChange)
				{
					// Nothing was deleted, now check whether anything was added.
					List<int> rghvosOld = new List<int>();
					for (int i = 0; i < citemsOld; ++i)
						rghvosOld.Add(hvosOld[i]);
					for (int i = 0; i < rghvosChosen.Count; ++i)
					{
						if (!rghvosOld.Contains(rghvosChosen[i]))
						{
							fChange = true;
							break;
						}
					}
				}
			}
			if (fChange)
			{
				ResetProperty(DetailControlsStrings.ksSetItem, rghvosChosen.ToArray());
				m_vectorRefView.ReloadVector();
				int h2 = m_vectorRefView.RootBox.Height;
				CheckViewSizeChanged(h1, h2);
			}
		}

		protected void CheckViewSizeChanged(int h1, int h2)
		{
			if (h1 != h2 && ViewSizeChanged != null)
			{
				ViewSizeChanged(this,
					new FwViewSizeEventArgs(h2, m_vectorRefView.RootBox.Width));
			}
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			base.OnBackColorChanged(e);
			if (m_vectorRefView != null)
			{
				m_vectorRefView.BackColor = this.BackColor;
			}
		}

		int RootBoxHeight
		{
			get
			{
				if (m_vectorRefView == null || m_vectorRefView.RootBox == null)
					return 0;
				return m_vectorRefView.RootBox.Height;
			}
		}

		/// <summary>
		/// Keep the view width equal to the launcher width minus the button width.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (this.m_panel != null && this.m_vectorRefView != null)
			{
				int w = this.Width - this.m_panel.Width;
				int h1 = RootBoxHeight;
				if (w < 0)
					w = 0;
				if (w == this.m_vectorRefView.Width)
					return; // cuts down on recursive calls.
				this.m_vectorRefView.Width = w;
				m_vectorRefView.PerformLayout();
				int h2 = RootBoxHeight;
				CheckViewSizeChanged(h1, h2);
			}
		}

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave (e);
			if (this.m_vectorRefView != null && this.m_vectorRefView.RootBox != null)
				this.m_vectorRefView.RootBox.DestroySelection();
		}

		#endregion // Overrides

		#region Other methods

		protected void ResetProperty(string action, int[] hvos)
		{
			int oldCount = m_cache.GetVectorSize(m_obj.Hvo, m_flid);
			m_cache.BeginUndoTask(
				String.Format(DetailControlsStrings.ksUndoItemAction, action, m_fieldName),
				String.Format(DetailControlsStrings.ksRedoItemAction, action, m_fieldName));
			m_cache.ReplaceReferenceProperty(m_obj.Hvo, m_flid, 0, oldCount, ref hvos);
			m_cache.EndUndoTask();
			// JohnT: don't do this, ReplaceReferenceProperty already does a PropChanged,
			// and doing it twice (even if you get the arguments right) can cause it to
			// duplicate items or remove too many on the screen. Anyway it's wasted.
//			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_obj.Hvo,
//				m_flid, 0, hvos.Length, (action == "deleted") ? oldCount - hvos.Length : 0);
			UpdateDisplayFromDatabase();
		}

		//		protected CmObject[] Contents
		//		{
		//			get
		//			{
		//				int[] hvos = ContentsHvos;
		//				CmObject[] cmos = new CmObject[hvos.Length];
		//				int i = 0;
		//				foreach(int hvo in hvos)
		//					cmos[i++] = CmObject.CreateFromDBObject(m_cache, hvo);
		//				return cmos;
		//			}
		//		}
		//
		//		protected int[] ContentsHvos
		//		{
		//			get { return m_cache.GetVectorProperty(m_obj.Hvo, m_flid); }
		//		}

		#endregion // Other methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(VectorReferenceLauncher));
			this.m_vectorRefView = CreateVectorReverenceView();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel.Name = "m_panel";
			//
			// m_btnLauncher
			//
			this.m_btnLauncher.Name = "m_btnLauncher";
			//
			// m_vectorRefView
			//
			this.m_vectorRefView.AutoScroll = false;
			this.m_vectorRefView.EditingHelper.DefaultCursor = null;
			this.m_vectorRefView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_vectorRefView.Location = new System.Drawing.Point(0, 0);
			this.m_vectorRefView.Name = "m_vectorRefView";
			this.m_vectorRefView.Size = new System.Drawing.Size(250, 20);
			this.m_vectorRefView.TabIndex = 2;
			//
			// VectorReferenceLauncher
			//
			this.Controls.Add(this.m_vectorRefView);
			this.Controls.Add(this.m_panel);
			this.MainControl = this.m_vectorRefView;
			this.Name = "VectorReferenceLauncher";
			this.Size = new System.Drawing.Size(250, 20);
			this.Controls.SetChildIndex(this.m_vectorRefView, 0);
			this.Controls.SetChildIndex(this.m_panel, 1);
			this.ResumeLayout(false);
		}
		#endregion

		protected virtual VectorReferenceView CreateVectorReverenceView()
		{
			return new VectorReferenceView();
		}
	}
}
