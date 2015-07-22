// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: VectorReferenceLauncher.cs
// Responsibility: Steve McConnel (was RandyR)
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class VectorReferenceLauncher : ReferenceLauncher
	{
		#region Data Members

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

		public override bool SliceIsCurrent
		{
			set
			{
				int h1 = RootBoxHeight;
				base.SliceIsCurrent = value;
				CheckViewSizeChanged(h1, RootBoxHeight);
			}
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
			}
			m_vectorRefView = null; // Should all be disposed automatically, since it is in the Controls collection.

			base.Dispose(disposing);
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator, PropertyTable propertyTable,
			string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, propertyTable, displayNameProperty, displayWs);
			m_vectorRefView.Initialize(obj, flid, fieldName, cache, displayNameProperty, mediator, displayWs);
		}

		public override System.Xml.XmlNode ConfigurationNode
		{
			get
			{
				return base.ConfigurationNode;
			}
			set
			{
				base.ConfigurationNode = value;
				m_vectorRefView.ConfigurationNode = value;
			}
		}

		#endregion // Construction, Initialization, and Disposal

		#region Overrides

		protected override void OnObjectCreated()
		{
			base.OnObjectCreated();
			m_vectorRefView.UpdateRootObject(m_obj);
		}
		/// <summary>
		/// Get the mediator from the view.
		/// </summary>
		protected override Mediator Mediator
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
		protected override SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			var contents = from hvo in ((ISilDataAccessManaged) m_cache.DomainDataByFlid).VecProp(m_obj.Hvo, m_flid)
						   select m_cache.ServiceLocator.GetObject(hvo);

			return new SimpleListChooser(m_persistProvider,
				labels,
				m_fieldName,
				m_cache,
				contents,
				m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
		}

		public override void SetItems(IEnumerable<ICmObject> chosenObjs)
		{
			SetItems(chosenObjs, string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
		}

		public override void AddItem(ICmObject obj)
		{
			AddItem(obj, string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
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
				m_vectorRefView.BackColor = BackColor;
			}
		}

		protected virtual int RootBoxHeight
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
			if (m_panel != null && m_vectorRefView != null)
			{
				int w = Width - m_panel.Width;
				int h1 = RootBoxHeight;
				if (w < 0)
					w = 0;
				if (w == m_vectorRefView.Width)
					return; // cuts down on recursive calls.
				m_vectorRefView.Width = w;
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
			base.OnLeave(e);
			if (m_vectorRefView != null && m_vectorRefView.RootBox != null)
				m_vectorRefView.RootBox.DestroySelection();
		}

		#endregion // Overrides

		#region Other Members

		protected void AddItem(ICmObject obj, string undoText, string redoText)
		{
			var newTargets = Targets.ToList();
			newTargets.Add(obj);
			SetItems(newTargets, undoText, redoText);
		}

		protected void SetItems(IEnumerable<ICmObject> chosenObjs, string undoText, string redoText)
		{
			CheckDisposed();
			// null indicates that we cancelled out of the chooser dialog -- we shouldn't get
			// here with that value, but just in case...
			if (chosenObjs == null)
				return;

			int h1 = RootBoxHeight;

			var oldObjs = Targets;
			if (oldObjs.Count() != chosenObjs.Count() || oldObjs.Intersect(chosenObjs).Count() != chosenObjs.Count())
			{
				UndoableUnitOfWorkHelper.Do(undoText, redoText, m_obj, () =>
				{
					Targets = chosenObjs;
					// FWR-3238 Keep these lines inside the UOW block, since for some reason
					// 'this' is disposed after we come out of the block.
					UpdateDisplayFromDatabase();
					int h2 = RootBoxHeight;
					CheckViewSizeChanged(h1, h2);
				});
			}
		}

		public override void UpdateDisplayFromDatabase()
		{
			m_vectorRefView.ReloadVector();
		}

		protected virtual IEnumerable<ICmObject> Targets
		{
			get
			{
				if (m_obj == null)
					return new ICmObject[0];
				return from hvo in ((ISilDataAccessManaged) m_cache.DomainDataByFlid).VecProp(m_obj.Hvo, m_flid)
					   select m_cache.ServiceLocator.GetObject(hvo);
			}

			set
			{
				// The old and new arrays are compared and modified as little as possible.
				// We remove no longer needed items from their spots and add new ones to the end,
				// at least until we decide we need to be able to move things within the sequence.
				var oldObjs = Targets.ToArray();
				var objsChosen = value.ToArray();
				RemoveUnneededObjectsFromProperty(oldObjs, objsChosen);
				if (objsChosen.Length == 0)
					return;
				var objToAdd = objsChosen.Except(oldObjs);
				AddNewObjectsToProperty(objToAdd);
				// FWR-2841 Replace with start=0, deletes all and re-adds the chosen ones; not what we want.
				// The Replace all method generates too many side effects calls [RemoveObjectSideEffects()].
				//m_cache.DomainDataByFlid.Replace(m_obj.Hvo, m_flid, 0, oldCount, rghvosChosen, rghvosChosen.Length);
			}
		}

		/// <summary>
		/// Add the specified objects to the property the launcher is editing. Caller makes UOW.
		/// </summary>
		/// <param name="objectsToAdd"></param>
		protected virtual void AddNewObjectsToProperty(IEnumerable<ICmObject> objectsToAdd)
		{
			if (objectsToAdd.Count() == 0)
				return;
			var cvec = m_cache.DomainDataByFlid.get_VecSize(m_obj.Hvo, m_flid);
			var hvosToAdd = objectsToAdd.Select(obj => obj.Hvo).ToArray();
			m_cache.DomainDataByFlid.Replace(m_obj.Hvo, m_flid, cvec, cvec, hvosToAdd, hvosToAdd.Length);
		}

		private void RemoveUnneededObjectsFromProperty(ICmObject[] oldObjs, ICmObject[] objsChosen)
		{
			if (oldObjs.Length == 0)
				return;
			var i = 0;
			foreach (var oldObj in oldObjs)
			{
				if (!objsChosen.Contains(oldObj))
				{
					RemoveFromPropertyAt(i, oldObj);
					i--;
				}
				i++;
			}
		}

		/// <summary>
		/// Remove from the edited property the old object oldObj, expected to be at index index.
		/// </summary>
		protected virtual void RemoveFromPropertyAt(int index, ICmObject oldObj)
		{
			m_cache.DomainDataByFlid.Replace(m_obj.Hvo, m_flid, index, index + 1, new int[0], 0);
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
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
