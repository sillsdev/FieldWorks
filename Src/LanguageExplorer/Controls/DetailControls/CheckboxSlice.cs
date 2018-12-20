// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class CheckboxSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		private CheckBox m_cb;
		protected XElement m_node;
		private bool m_fToggleValue;

		/// <summary />
		public CheckboxSlice(LcmCache cache, ICmObject obj, int flid, XElement node)
			: base(new CheckBox(), cache, obj, flid)
		{
			m_cb = (CheckBox)Control;
			m_cb.Dock = DockStyle.Left;
			m_cb.Width = 20; // was taking whole length of slice
			m_cb.Height = 20;   // on Mono, is set to 100 by default
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

			base.OnSizeChanged(e);
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

			m_cb.Checked = IntBoolPropertyConverter.GetBoolean(Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), MyCmObject.Hvo, m_flid);
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
			if (!MyCmObject.IsValidObject)
			{
				return;
			}

			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoChange, m_fieldName), string.Format(DetailControlsStrings.ksRedoChange, m_fieldName), MyCmObject, () =>
			{
				var fValue = ((CheckBox)obj).Checked;
				if (m_fToggleValue)
				{
					fValue = !fValue;
				}
				IntBoolPropertyConverter.SetValueFromBoolean(Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), MyCmObject.Hvo, m_flid, fValue);
			});
		}

		private void m_cb_GotFocus(object sender, EventArgs e)
		{
			OnGotFocus(e);
			ContainingDataTree.CurrentSlice = this;
		}
	}
}