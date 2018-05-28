// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Utils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Displays a combobox of labels, but underlyingly based on integers.
	/// </summary>
	internal class EnumComboSlice : FieldSlice, IVwNotifyChange
	{
		protected ComboBox m_combo;
		int m_comboWidth;		// computed width of m_combo

		/// <summary />
		public EnumComboSlice(LcmCache cache, ICmObject obj, int flid, XElement parameters)
			: base(new FwOverrideComboBox(), cache, obj, flid)
		{
			m_combo = (ComboBox)Control;
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			//note: no exception is thrown if it can't find it.
			m_combo.Font = new Font(MiscUtils.StandardSansSerif, 10);
			SetForeColor(parameters);
			m_combo.SelectedValueChanged += this.SelectionChanged;
			m_combo.GotFocus += m_combo_GotFocus;
			m_combo.DropDownClosed += m_combo_DropDownClosed;
#if __MonoCS__	// FWNX-545
			m_combo.Parent.SizeChanged += new EventHandler(OnComboParentSizeChanged);
#endif
			PopulateCombo(parameters);
			// We need to watch the cache for changes to our property.
			cache.DomainDataByFlid.AddNotification(this);
		}

		private void SetForeColor(XElement parameters)
		{
			var node = parameters.Element("forecolor");
			if (node != null)
			{
				m_combo.ForeColor = Color.FromName(node.GetInnerText());
			}
		}
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			m_combo.Dock = DockStyle.Left;
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

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_combo != null)
				{
					m_combo.SelectedValueChanged -= SelectionChanged;
					m_combo.GotFocus -= m_combo_GotFocus;
					if (m_combo.Parent == null)
					{
						m_combo.Dispose();
					}
				}

				Cache?.DomainDataByFlid?.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_combo = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected void PopulateCombo(XElement parameters)
		{
			m_combo.Items.Clear();
			var node =  parameters.Element("stringList");
			if (node == null)
			{
				throw new ApplicationException("The Enum editor requires a <stringList> element in the <deParams>");
			}

			var labels = StringTable.Table.GetStringsFromStringListNode(node);
			var width = 0;
			using (var g = m_combo.CreateGraphics())
			{
				foreach (var label in labels)
				{
					var size = g.MeasureString(label, m_combo.Font);
					if (size.Width > width)
					{
						width = (int)Math.Ceiling(size.Width);
					}
					m_combo.Items.Add(label);
				}
			}
			m_comboWidth = width + 25;
			m_combo.Width = m_comboWidth;
			m_combo.MaxDropDownItems = Math.Min(m_combo.Items.Count, 20);
		}

#if __MonoCS__
		/// <summary>
		/// In .Net/Windows, shrinking the parent SplitContainer doesn't appear to shrink the
		/// ComboBox permanently. However, it does in Mono/Linux.  See FWNX-545.
		/// </summary>
		private void OnComboParentSizeChanged(object sender, EventArgs e)
		{
			if (m_combo.Width < m_comboWidth)
			{
				m_combo.Width = m_comboWidth;
			}
		}
#endif

		protected override void UpdateDisplayFromDatabase()
		{
			if (!MyCmObject.IsValidObject)
			{
				return; // If the object is not valid our data needs to be refreshed, skip until data is valid again
			}
			var currentValue = Cache.DomainDataByFlid.get_IntProp(MyCmObject.Hvo, m_flid);

			//nb: we are assuming that an enumerations start with 0
			m_combo.SelectedIndex = currentValue;
		}

		/// <summary>
		/// Event handler for selection changed in combo box.
		/// </summary>
		protected void SelectionChanged(object sender, EventArgs e)
		{
			Debug.Assert(m_combo != null);
			if (m_combo.DroppedDown)
			{
				return; // don't want to update things while the user is manipulating the list. (See FWR-1728.)
			}
			if (!MyCmObject.IsValidObject)
			{
				return; // If the object is not valid our data needs to be refreshed, skip until data is valid again
			}
			var oldValue = Cache.DomainDataByFlid.get_IntProp(MyCmObject.Hvo, m_flid);
			var newValue = m_combo.SelectedIndex;
			if (oldValue == newValue)
			{
				// No sense in setting it to the same value.
				return;
			}
			Cache.DomainDataByFlid.BeginUndoTask(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
			Cache.DomainDataByFlid.SetInt(MyCmObject.Hvo, m_flid, newValue);
			var sideEffectMethod = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "sideEffect", null);
			if (!string.IsNullOrEmpty(sideEffectMethod))
			{
				var info = MyCmObject.GetType().GetMethod(sideEffectMethod);
				if (info != null)
				{
					info.Invoke(MyCmObject, new object[] { oldValue, newValue });
				}
			}
			Cache.DomainDataByFlid.EndUndoTask();
			// The changing value may affect the datatree display.  See LT-6539.
			var fRefresh = XmlUtils.GetOptionalBooleanAttributeValue(ConfigurationNode, "refreshDataTreeOnChange", false);
			if (fRefresh)
			{
				ContainingDataTree.RefreshList(false);
			}
		}

		private void m_combo_DropDownClosed(object sender, EventArgs e)
		{
			SelectionChanged(sender, e);
		}
		private void m_combo_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}

		#region IVwNotifyChange Members

		/// <summary>
		/// Implemented to make combo update automatically when property changes.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == MyCmObject.Hvo && tag == m_flid)
			{
				UpdateDisplayFromDatabase();
			}
		}

		#endregion
	}
}
