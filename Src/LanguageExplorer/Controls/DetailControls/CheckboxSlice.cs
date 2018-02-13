// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Infrastructure;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for Checkbox.
	/// </summary>
	internal class CheckboxSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckboxSlice"/> class.
		/// </summary>
		private CheckBox m_cb;
		protected XElement m_node;
		bool m_fToggleValue;

		public CheckboxSlice(LcmCache cache, ICmObject obj, int flid, XElement node)
			: base(new CheckBox(), cache, obj, flid)
		{
			m_cb = (CheckBox)Control;
			m_cb.Dock = DockStyle.Left;
			m_cb.Width = 20; // was taking whole length of slice
			m_cb.Height = 20;	// on Mono, is set to 100 by default
			m_node = node;
			m_cb.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_node, "editable", true);
			SetToggleValue(node);
		}

		private void SetToggleValue(XElement node)
		{
			m_fToggleValue = XmlUtils.GetOptionalAttributeValue(node, "toggleValue", "false").ToLower() == "true";
		}

		/// <summary>
		/// Called when the slice is first created, but also when it is
		/// "reused" (e.g. refresh or new target object)
		/// </summary>
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			if (m_cb.Dock != DockStyle.Left)
			{
				m_cb.Dock = DockStyle.Left;
			}
			if (m_cb.Width != 20)
			{
				m_cb.Width = 20; // was taking whole length of slice
			}

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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				m_sda?.RemoveNotification(this);

				if (m_cb != null)
				{
					m_cb.CheckedChanged -= OnChanged;
					m_cb.GotFocus -= m_cb_GotFocus;
					if (m_cb.Parent == null)
					{
						m_cb.Dispose();
					}
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
			{
				return;
			}

			// Restore the docking style (gets changed to Fill elsewhere).
			if (m_cb.Dock != DockStyle.Left)
			{
				m_cb.Dock = DockStyle.Left;
			}
			if (m_cb.Width != 20)
			{
				m_cb.Width = 20; // was taking whole length of slice
			}

			base.OnSizeChanged (e);
		}

		#region IVwNotifyChange methods
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == MoInflAffixSlotTags.kflidOptional)
			{
				UpdateDisplayFromDatabase();
			}
		}
		#endregion

		protected override void UpdateDisplayFromDatabase()
		{
			m_cb.CheckedChanged -= OnChanged;
			m_cb.GotFocus -= m_cb_GotFocus;

			m_cb.Checked = IntBoolPropertyConverter.GetBoolean(Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), Object.Hvo, m_flid);
			if (m_fToggleValue)
			{
				m_cb.Checked = !m_cb.Checked;
			}
			// Restore the docking style and size (may get changed to Fill elsewhere).
			if (m_cb.Dock != DockStyle.Left)
			{
				m_cb.Dock = DockStyle.Left;
			}
			if (m_cb.Width != 20)
			{
				m_cb.Width = 20; // was taking whole length of slice
			}

			m_cb.CheckedChanged += OnChanged;
			m_cb.GotFocus += m_cb_GotFocus;
		}

		public void OnChanged(object obj, EventArgs args)
		{
			if (!Object.IsValidObject)
			{
				return;
			}

			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoChange, m_fieldName), string.Format(DetailControlsStrings.ksRedoChange, m_fieldName), Object, () =>
			{
				var fValue = ((CheckBox)obj).Checked;
				if (m_fToggleValue)
				{
					fValue = !fValue;
				}
				IntBoolPropertyConverter.SetValueFromBoolean(Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), Object.Hvo, m_flid, fValue);
			});
		}

		private void m_cb_GotFocus(object sender, EventArgs e)
		{
			OnGotFocus(e);
			ContainingDataTree.CurrentSlice = this;
		}
	}
}
