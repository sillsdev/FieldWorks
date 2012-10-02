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
// File: DropDownContainer.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DropDownContainer.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DropDownContainer : Form, IFWDisposable
	{
		/// <summary>Handles AfterDropDownClose events.</summary>
		public delegate void AfterDropDownClosedHandler(DropDownContainer dropDownContainer,
			object eventData);

		/// <summary>Event which occurs after a drop-down is closed.</summary>
		public event AfterDropDownClosedHandler AfterDropDownClosed;

		/// <summary>Handles BeforeDropDownClose events.</summary>
		public delegate void BeforeDropDownOpenedHandler(DropDownContainer dropDownContainer,
			object eventData);

		/// <summary>Event which occurs before a drop-down is closed.</summary>
		public event BeforeDropDownOpenedHandler BeforeDropDownOpened;

		private bool m_canceled = true;
		private Control m_attachedControl = null;
		private object m_afterDropDownClosedEventData;
		private object m_beforeDropDownOpenedEventData;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DropDownContainer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DropDownContainer()
		{
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(168, 144);
			this.ControlBox = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DropDownContainer";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the drop-down form was canceled. Delegates
		/// to the AfterDropDownClosed event can check this property to determine whether or
		/// not to update whatever the drop-down is attached to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Canceled
		{
			get
			{
				CheckDisposed();
				return m_canceled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control to which this drop-down is attached.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control AttachedControl
		{
			get
			{
				CheckDisposed();
				return m_attachedControl;
			}
			set
			{
				CheckDisposed();
				m_attachedControl = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets data that will be passed to delegates of the AfterDropDownClosed
		/// event. Inheritors of the DropDownContainer should set this appropriately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected object AfterDropDownClosedEventData
		{
			get { return m_afterDropDownClosedEventData; }
			set { m_afterDropDownClosedEventData = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets data that will be passed to delegates of the AfterDropDownClosed
		/// event. Inheritors of the DropDownContainer should set this appropriately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected object BeforeDropDownOpenedEventData
		{
			get {return m_beforeDropDownOpenedEventData;}
			set {m_beforeDropDownOpenedEventData = value;}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cancel"></param>
		/// ------------------------------------------------------------------------------------
		protected void SetCanceled(bool cancel)
		{
			m_canceled = cancel;
		}

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(System.EventArgs e)
		{
			m_canceled = true;
			AfterDropDownClosedEventData = null;

			base.OnActivated(e);

			// Call this delegate if there are any subscribers.
			if (BeforeDropDownOpened != null)
				BeforeDropDownOpened(this, BeforeDropDownOpenedEventData);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDeactivate(System.EventArgs e)
		{
			if (this.Visible)
				this.Hide();

			base.OnDeactivate(e);

			BeforeDropDownOpenedEventData = null;

			// Call this delegate if there are any subscribers.
			if (AfterDropDownClosed != null)
				AfterDropDownClosed(this, AfterDropDownClosedEventData);
		}

		#endregion
	}
}
