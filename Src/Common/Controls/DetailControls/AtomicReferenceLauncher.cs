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
// File: AtomicReferenceLauncher.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class AtomicReferenceLauncher : ReferenceLauncher
	{
		protected AtomicReferenceView m_atomicRefView;
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows either the launcher or the embedded view to communicate size changes to
		/// the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;
		public event FwSelectionChangedEventHandler ReferenceChanged;

		#region Construction, Initialization, and Disposition

		public AtomicReferenceLauncher()
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
			m_atomicRefView = null; // Disposed automatically, since it is in the controls collection.

			base.Dispose(disposing);
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_atomicRefView.Initialize(obj, flid, cache, displayNameProperty, mediator);
		}
		#endregion // Construction, Initialization, and Disposition

		#region Button support methods

		protected override SimpleListChooser GetChooser(ObjectLabelCollection labels)
		{
			string nullLabel = DetailControlsStrings.ksNullLabel;
			if (m_configurationNode != null)
			{
				System.Xml.XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				nullLabel = XmlUtils.GetOptionalAttributeValue(node, "nullLabel", nullLabel);
			}
			SimpleListChooser c = new SimpleListChooser(m_cache, m_persistProvider, labels, TargetHvo,
				m_fieldName, nullLabel, m_atomicRefView.StyleSheet);
			return c;
		}

		/// <summary>
		/// Get the mediator from the view.
		/// </summary>
		protected override XCore.Mediator Mediator
		{
			get { return m_atomicRefView.Mediator; }
		}

		public override void AddItem(int hvo)
		{
			CheckDisposed();
			int h1 = m_atomicRefView.RootBox.Height;
			int w1 = m_atomicRefView.RootBox.Width;
			m_cache.BeginUndoTask(String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				String.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
			ReplaceObject(hvo);
			m_cache.EndUndoTask();
			m_atomicRefView.SetObject(hvo);
			UpdateDisplayFromDatabase();
			if (ReferenceChanged != null)
				ReferenceChanged(this, new FwObjectSelectionEventArgs(hvo));
			int h2 = m_atomicRefView.RootBox.Height;
			CheckViewSizeChanged(h1, h2);
		}

		protected virtual void ReplaceObject(int hvo)
		{
			// Use reflection here so the set function can do other things that may be needed when
			// a property is set.
			string sName = m_cache.MetaDataCacheAccessor.GetFieldName((uint)m_flid) + "RAHvo";
			m_obj.GetType().InvokeMember(sName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
				BindingFlags.SetProperty, null, m_obj, new object [] { hvo });
			//			m_cache.SetObjProperty(m_obj.Hvo, m_flid, hvo);
		}

		protected ICmObject Target
		{
			get
			{
				int hvo = TargetHvo;
				if (hvo > 0)
					return CmObject.CreateFromDBObject(m_cache, hvo);
				else
					return null;
			}
			set
			{
				if (value != null)
					TargetHvo = ((CmObject)value).Hvo;
				else
					TargetHvo = 0;
			}
		}

		public override void UpdateDisplayFromDatabase()
		{
			CheckDisposed();
			m_atomicRefView.SetObject(m_obj.Hvo);
		}

		protected virtual int TargetHvo
		{
			get
			{
				return m_cache.GetObjProperty(m_obj.Hvo, m_flid);
			}
			set
			{
				m_cache.SetObjProperty(m_obj.Hvo, m_flid, (int)value);
			}
		}

		#endregion // Button support methods

		#region Overrides

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave (e);
			if (this.m_atomicRefView != null && this.m_atomicRefView.RootBox != null)
				this.m_atomicRefView.RootBox.DestroySelection();
		}

		#endregion // Overrides

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_atomicRefView = CreateAtomicReferenceView();
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
			// m_atomicRefView
			//
			this.m_atomicRefView.AutoScroll = false;
			this.m_atomicRefView.EditingHelper.DefaultCursor = null;
			this.m_atomicRefView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_atomicRefView.Location = new System.Drawing.Point(0, 0);
			this.m_atomicRefView.Name = "m_atomicRefView";
			this.m_atomicRefView.Size = new System.Drawing.Size(250, 20);
			this.m_atomicRefView.TabIndex = 2;
			//
			// AtomicReferenceLauncher
			//
			this.Controls.Add(this.m_atomicRefView);
			this.Controls.Add(this.m_panel);			//?
			this.MainControl = this.m_atomicRefView;
			this.Name = "AtomicReferenceLauncher";
			this.Size = new System.Drawing.Size(250, 20);
			this.Controls.SetChildIndex(this.m_atomicRefView, 0);
			this.Controls.SetChildIndex(this.m_panel, 1);		//0
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Keep the view width equal to the launcher width minus the button width.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (this.m_panel != null && this.m_atomicRefView != null)
			{
				int w = this.Width - this.m_panel.Width;
				int h1 = RootBoxHeight;
				if (w < 0)
					w = 0;
				if (w == this.m_atomicRefView.Width)
					return; // cuts down on recursive calls.
				this.m_atomicRefView.Width = w;
				m_atomicRefView.PerformLayout();
				int h2 = RootBoxHeight;
				CheckViewSizeChanged(h1, h2);
			}
		}

		int RootBoxHeight
		{
			get
			{
				if (m_atomicRefView == null || m_atomicRefView.RootBox == null)
					return 0;
				return m_atomicRefView.RootBox.Height;
			}
		}

		protected void CheckViewSizeChanged(int h1, int h2)
		{
			if (h1 != h2 && ViewSizeChanged != null)
			{
				ViewSizeChanged(this,
					new FwViewSizeEventArgs(h2, m_atomicRefView.RootBox.Width));
			}
		}

		protected virtual AtomicReferenceView CreateAtomicReferenceView()
		{
			return new AtomicReferenceView();
		}
	}
}
