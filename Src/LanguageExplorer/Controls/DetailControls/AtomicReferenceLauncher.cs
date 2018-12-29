// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.PlatformUtilities;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class AtomicReferenceLauncher : ReferenceLauncher
	{
		protected AtomicReferenceView m_atomicRefView;

		/// <summary>
		/// This allows either the launcher or the embedded view to communicate size changes to
		/// the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;
		public event FwSelectionChangedEventHandler ReferenceChanged;

		#region Construction, Initialization, and Disposition

		public AtomicReferenceLauncher()
		{
			InitializeComponent();
			if (Platform.IsMono)
			{
				// FWNX-266
				// Ensure parent get created before m_atomicRefView otherwise
				// m_atomicRefView Handle can be in an invalid state (in mono).
				CreateHandle();
			}
		}

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
			}
			m_atomicRefView = null; // Disposed automatically, since it is in the controls collection.

			base.Dispose(disposing);
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_atomicRefView.Initialize(obj, flid, fieldName, cache, displayNameProperty, displayWs);
		}
		#endregion // Construction, Initialization, and Disposition

		#region Button support methods

		protected override SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			var nullLabel = DetailControlsStrings.ksNullLabel;
			if (m_configurationNode != null)
			{
				var node = m_configurationNode.Element("deParams");
				nullLabel = XmlUtils.GetOptionalAttributeValue(node, "nullLabel", nullLabel);
				if (nullLabel == string.Empty)
				{
					nullLabel = null;
				}
			}
			return new SimpleListChooser(m_cache, m_persistProvider, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), labels, Target, m_fieldName, nullLabel, m_atomicRefView.StyleSheet);
		}

		public override void AddItem(ICmObject obj)
		{
			AddItem(obj, string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
		}

		/// <summary>
		/// Sets a reference collection or sequence.
		/// </summary>
		public override void SetItems(IEnumerable<ICmObject> chosenObjs)
		{
			throw new NotSupportedException();
		}

		protected void AddItem(ICmObject obj, string undoText, string redoText)
		{
			var h1 = m_atomicRefView.RootBox.Height;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, m_obj, () =>
			{
				Target = obj;
			});
			UpdateDisplayFromDatabase();
			ReferenceChanged?.Invoke(this, new FwObjectSelectionEventArgs(obj.Hvo));
			var h2 = m_atomicRefView.RootBox.Height;
			CheckViewSizeChanged(h1, h2);
		}

		protected internal virtual ICmObject Target
		{
			get
			{
				if (!m_obj.IsValidObject)
				{
					return null;
				}
				var hvo = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, m_flid);
				return hvo > 0 ? m_cache.ServiceLocator.GetObject(hvo) : null;
			}
			set
			{
				m_cache.DomainDataByFlid.SetObjProp(m_obj.Hvo, m_flid, value != null ? value.Hvo : 0);
			}
		}

		public override void UpdateDisplayFromDatabase()
		{
			m_atomicRefView.SetObject(m_obj);
		}

		#endregion // Button support methods

		#region Overrides

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			m_atomicRefView?.RootBox?.DestroySelection();
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
			this.Controls.Add(this.m_panel);            //?
			this.MainControl = this.m_atomicRefView;
			this.Name = "AtomicReferenceLauncher";
			this.Size = new System.Drawing.Size(250, 20);
			this.Controls.SetChildIndex(this.m_atomicRefView, 0);
			this.Controls.SetChildIndex(this.m_panel, 1);       //0
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Allow access to the AtomicRefView Control other than accessing by Control index.
		/// </summary>
		public Control AtomicRefViewControl => m_atomicRefView;

		/// <summary>
		/// Allow access to the panel Control other than accessing by Control index.
		/// </summary>
		public Control PanelControl => m_panel;

		/// <summary>
		/// Keep the view width equal to the launcher width minus the button width.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_panel == null || m_atomicRefView == null)
			{
				return;
			}
			var w = Width - m_panel.Width;
			var h1 = RootBoxHeight;
			if (w < 0)
			{
				w = 0;
			}
			if (w == m_atomicRefView.Width)
			{
				return; // cuts down on recursive calls.
			}
			m_atomicRefView.Width = w;
			m_atomicRefView.PerformLayout();
			var h2 = RootBoxHeight;
			CheckViewSizeChanged(h1, h2);
		}

		private int RootBoxHeight => m_atomicRefView?.RootBox?.Height ?? 0;

		protected void CheckViewSizeChanged(int h1, int h2)
		{
			if (h1 != h2)
			{
				ViewSizeChanged?.Invoke(this, new FwViewSizeEventArgs(h2, m_atomicRefView.RootBox.Width));
			}
		}

		protected virtual AtomicReferenceView CreateAtomicReferenceView()
		{
			return new AtomicReferenceView();
		}
	}
}