// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BasicTypeSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for Checkbox.
	/// </summary>
	public class CheckboxSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckboxSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private CheckBox m_cb;
		protected XmlNode m_node;
		bool m_fToggleValue;

		public CheckboxSlice(FdoCache cache, ICmObject obj, int flid, XmlNode node)
			: base(new CheckBox(), cache, obj, flid)
		{
			m_cb = ((CheckBox)this.Control);
			m_cb.Dock = System.Windows.Forms.DockStyle.Left;
			m_cb.Width = 20; // was taking whole length of slice
			m_cb.Height = 20;	// on Mono, is set to 100 by default
			m_node = node;
			m_cb.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_node, "editable", true);
			SetToggleValue(node);
		}

		private void SetToggleValue(XmlNode node)
		{
			string sToggle = XmlUtils.GetOptionalAttributeValue(node, "toggleValue", "false");
			if (sToggle.ToLower() == "true")
				m_fToggleValue = true;
			else
				m_fToggleValue = false;
		}

		/// <summary>
		/// Called when the slice is first created, but also when it is
		/// "reused" (e.g. refresh or new target object)
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			if (m_cb.Dock != DockStyle.Left)
				m_cb.Dock = System.Windows.Forms.DockStyle.Left;
			if (m_cb.Width != 20)
				m_cb.Width = 20; // was taking whole length of slice

			m_sda = Cache.DomainDataByFlid;
			m_sda.AddNotification(this);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_cb != null)
				{
					m_cb.CheckedChanged -= new EventHandler(OnChanged);
					m_cb.GotFocus -= new EventHandler(m_cb_GotFocus);
					if (m_cb.Parent == null)
						m_cb.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cb = null;
			m_sda = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void OnSizeChanged(EventArgs e)
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
				return;

			// Restore the docking style (gets changed to Fill elsewhere).
			if (m_cb.Dock != DockStyle.Left)
				m_cb.Dock = System.Windows.Forms.DockStyle.Left;
			if (m_cb.Width != 20)
				m_cb.Width = 20; // was taking whole length of slice

			base.OnSizeChanged (e);
		}

		#region IVwNotifyChange methods
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (tag == MoInflAffixSlotTags.kflidOptional)
				UpdateDisplayFromDatabase();
		}
		#endregion

		protected override void UpdateDisplayFromDatabase()
		{
			m_cb.CheckedChanged -= new EventHandler(OnChanged);
			m_cb.GotFocus -= new EventHandler(m_cb_GotFocus);

			m_cb.Checked = IntBoolPropertyConverter.GetBoolean(
				m_cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				Object.Hvo,
				m_flid);
			if (m_fToggleValue)
				m_cb.Checked = !m_cb.Checked;
			// Restore the docking style and size (may get changed to Fill elsewhere).
			if (m_cb.Dock != DockStyle.Left)
				m_cb.Dock = DockStyle.Left;
			if (m_cb.Width != 20)
				m_cb.Width = 20; // was taking whole length of slice

			m_cb.CheckedChanged += OnChanged;
			m_cb.GotFocus += m_cb_GotFocus;
		}

		public void OnChanged(Object obj, EventArgs args)
		{
			CheckDisposed();
			if (!Object.IsValidObject) return;

			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoChange, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoChange, m_fieldName), Object, () =>
			{
				var fValue = ((CheckBox)obj).Checked;
				if (m_fToggleValue)
					fValue = !fValue;

				IntBoolPropertyConverter.SetValueFromBoolean(
					m_cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
					Object.Hvo,
					m_flid,
					fValue);
			});
		}

		// Overhaul Aug 05: want all Window backgrounds in Detail controls.
		//		/// <summary>
		//		/// This is passed the color that the XDE specified, if any, otherwise null.
		//		/// The default is to use the normal window color for editable text.
		//		/// Subclasses which know they should have a different default should
		//		/// override this method, but normally should use the specified color if not
		//		/// null.
		//		/// </summary>
		//		/// <param name="clr"></param>
		//		public override void OverrideBackColor(String backColorName)
		//		{
		//			CheckDisposed();
		//
		//			if (this.Control == null)
		//				return;
		//			this.Control.BackColor = System.Drawing.SystemColors.Control;
		//		}

		private void m_cb_GotFocus(object sender, EventArgs e)
		{
			base.OnGotFocus(e);
			ContainingDataTree.CurrentSlice = this;
		}

		//// ************************************************************************
		//// The following overriden method would get focus events after the above
		//// handler had been removed from processing; specifically in the Dispose
		//// handler.  That is needed as the base.Dispose in UserControl changes
		//// the focus which causes the OnGotFocus to be called, even after the
		//// object is all but disposed.  The call to ContainingDataTree then
		//// gives a nice green error box (Crash).  The steps for reproducing
		//// this are still uncertian.  I was able to reproduce it twice, then
		//// changed the code and have not been able to reproduce the problem.
		//// I think this has taken care of that issue: getting the focus event
		//// from deep within the dispose call - actually reentrant.  See the
		//// stack frames below:
		////    DetailControls.dll!SIL.FieldWorks.Common.Framework.DetailControls.CheckboxSlice.OnGotFocus(System.EventArgs e = {System.EventArgs}) Line 238 + 0xd bytes	C#
		////    [External Code]
		//// 	DetailControls.dll!SIL.FieldWorks.Common.Framework.DetailControls.Slice.Dispose(bool disposing = true) Line 977 + 0xb bytes	C#
		////    DetailControls.dll!SIL.FieldWorks.Common.Framework.DetailControls.FieldSlice.Dispose(bool disposing = true) Line 110 + 0xf bytes	C#
		////    DetailControls.dll!SIL.FieldWorks.Common.Framework.DetailControls.CheckboxSlice.Dispose(bool disposing = true) Line 150 + 0xb bytes	C#
		////    ....
		//// This was found by investigating LT-5817, but the problem is much broader
		//// than that issue and could likely happen anwhere the CheckboxSlice is used.
		//// ************************************************************************
		////protected override void OnGotFocus(EventArgs e)
		////{
		////    base.OnGotFocus (e);
		////    ContainingDataTree.CurrentSlice = this;
		////}
	}

	/// <summary>
	/// Summary description for Checkbox.
	/// </summary>
	public class CheckboxRefreshSlice : CheckboxSlice
	{


		public CheckboxRefreshSlice(FdoCache cache, ICmObject obj, int flid, XmlNode node)
			: base(cache, obj, flid, node)
		{
		}

		private void RefreshDisplay()
		{
			CheckDisposed();
			var dt = ContainingDataTree;
			var result = dt.RefreshDisplay();
			if (result)
			{
				dt.RefreshList(true);
			}
		}

		#region IVwNotifyChange methods
		// We use PropChanged instead of OnChanged so that the BrowseActiveViewer can tell us when
		// a select column item has been changed.
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (tag == PhSegmentRuleTags.kflidDisabled ||
				tag == MoCompoundRuleTags.kflidDisabled ||
				tag == MoAdhocProhibTags.kflidDisabled)
			{
				UpdateDisplayFromDatabase();
				RefreshDisplay();
			}
		}
		#endregion
	}

	/// <summary>
	/// Summary description for DateSlice.
	/// </summary>
	public class DateSlice : FieldSlice, IVwNotifyChange
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DateSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DateSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new RichTextBox(), cache, obj, flid)
		{
			// JohnT: per comment at the end of LT-7073, we want the normal window color for this
			// slice. It's also nice to be able to select and copy. Setting ReadOnly is enough to prevent
			// editing. And setting Enabled to false prevents control of BackColor.
			//Control.Enabled = false;
			Control.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Window);
			Control.ForeColor = System.Drawing.SystemColors.GrayText;
			Control.TabStop = false;
			Control.Size = new System.Drawing.Size(128, 16);
			((RichTextBox)Control).BorderStyle = System.Windows.Forms.BorderStyle.None;
			((RichTextBox)Control).ReadOnly = true;
			Control.GotFocus += new EventHandler(Control_GotFocus);
			// We need to watch the cache for changes to our property.
			cache.DomainDataByFlid.AddNotification(this);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Control.GotFocus -= new EventHandler(Control_GotFocus);
				if (m_cache != null && m_cache.DomainDataByFlid != null)
					m_cache.DomainDataByFlid.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void UpdateDisplayFromDatabase()
		{
			RichTextBox rtb = ((RichTextBox)this.Control);
			DateTime dt = SilTime.GetTimeProperty(m_cache.DomainDataByFlid, Object.Hvo, m_flid);
			if (dt == DateTime.MinValue)
			{
				rtb.Text = "Date/Time not set";
			}
			else
			{
				rtb.Text = String.Format(DetailControlsStrings.ksDateAndTime,
					dt.ToLongDateString(), dt.ToShortTimeString());
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus (e);
			ContainingDataTree.CurrentSlice = this;
		}

		private void Control_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == Object.Hvo && tag == m_flid)
				UpdateDisplayFromDatabase();
		}
	}

	/// <summary>
	/// Summary description for IntegerSlice.
	/// </summary>
	public class IntegerSlice : FieldSlice
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IntegerSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		TextBox m_tb;
		int m_previousValue;

		public IntegerSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new TextBox(), cache, obj, flid)
		{
			m_tb = ((TextBox)this.Control);
			m_tb.LostFocus += new EventHandler(m_tb_LostFocus);
			m_tb.GotFocus += new EventHandler(m_tb_GotFocus);
			m_tb.BorderStyle = System.Windows.Forms.BorderStyle.None;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_tb != null)
				{
					m_tb.LostFocus -= new EventHandler(m_tb_LostFocus);
					m_tb.GotFocus -= new EventHandler(m_tb_GotFocus);
					if (m_tb.Parent == null)
						m_tb.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_tb = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void UpdateDisplayFromDatabase()
		{
			m_previousValue = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid);
			m_tb.Text = m_previousValue.ToString();
		}

		private void m_tb_LostFocus(object sender, EventArgs e)
		{
			try
			{
				int i = Convert.ToInt32(m_tb.Text);
				if(i == m_previousValue)
					return;
				m_previousValue = i;
				UndoableUnitOfWorkHelper.Do(
					String.Format(DetailControlsStrings.ksUndoChangeTo, m_fieldName),
					String.Format(DetailControlsStrings.ksRedoChangeTo, m_fieldName),
					Cache.ActionHandlerAccessor,
					() =>m_cache.DomainDataByFlid.SetInt(Object.Hvo, m_flid, i));
			}
			catch(FormatException error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksEnterNumber);
			}
			catch(Exception error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksInvalidNumber);
			}
			// In case the update failed, make sure the box is consistent.
			m_tb.Text = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid).ToString();
		}

		private void m_tb_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}
	}
}
