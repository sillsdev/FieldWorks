// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PhoneEnvReferenceLauncher.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
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

			Debug.Assert(obj is IMoAffixAllomorph || obj is IMoStemAllomorph);

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);
			m_phoneEnvRefView.Initialize((IMoForm)obj, flid, cache);
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
		protected override SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			var contents = from hvo in (m_cache.DomainDataByFlid as ISilDataAccessManaged).VecProp(m_obj.Hvo, m_flid)
						   select m_cache.ServiceLocator.GetObject(hvo);

			return new SimpleListChooser(m_persistProvider,
				labels,
				m_fieldName,
				m_cache,
				contents,
				m_mediator.HelpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to set the new values selected from the chooser dialog.
		/// </summary>
		/// <param name="chosenObjs">The chosen objs.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetItems(IEnumerable<ICmObject> chosenObjs)
		{
			CheckDisposed();
			// null indicates that we cancelled out of the chooser dialog -- we shouldn't get
			// here with that value, but just in case...
			if (chosenObjs == null)
				return;
			int h1 = m_phoneEnvRefView.RootBox.Height;

			ICollection<IPhEnvironment> envs;
			if (m_flid == MoAffixAllomorphTags.kflidPosition)
			{
				envs = ((IMoAffixAllomorph)m_obj).PositionRS;
			}
			else
			{
				if (m_obj is IMoAffixAllomorph)
					envs = ((IMoAffixAllomorph)m_obj).PhoneEnvRC;
				else
					envs = ((IMoStemAllomorph)m_obj).PhoneEnvRC;
			}

			// First, we need a list of hvos added and a list of hvos deleted.
			HashSet<IPhEnvironment> newEnvs = new HashSet<IPhEnvironment>(chosenObjs.Cast<IPhEnvironment>());
			HashSet<IPhEnvironment> delEnvs = new HashSet<IPhEnvironment>();
			foreach (IPhEnvironment env in envs)
			{
				if (newEnvs.Contains(env))
					newEnvs.Remove(env);
				else
					delEnvs.Add(env);
			}

			// Add all the new environments.
			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), m_obj, () =>
			{
				foreach (IPhEnvironment env in newEnvs)
				{
					m_phoneEnvRefView.AddNewItem(env);
					envs.Add(env);
				}

				foreach (IPhEnvironment env in delEnvs)
				{
					m_phoneEnvRefView.RemoveItem(env);
					envs.Remove(env);
				}
			});
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
