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
// File: EnumComboSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Implements the an XDE editor which displays a combobox of labels, but underlyingly based on integers.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Drawing;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.Utils;
using System.Reflection;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Displays a combobox of labels, but underlyingly based on integers.
	/// </summary>
	public class EnumComboSlice : FieldSlice, IVwNotifyChange
	{
		protected ComboBox m_combo;
		int m_comboWidth;		// computed width of m_combo

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		/// <param name="stringTable"></param>
		/// <param name="parameters"></param>
		public EnumComboSlice(FdoCache cache, ICmObject obj, int flid, SIL.Utils.StringTable stringTable, XmlNode parameters)
			: base(new FwOverrideComboBox(), cache, obj, flid)
		{
			m_combo = (ComboBox)this.Control;
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			//note: no exception is thrown if it can't find it.
			m_combo.Font = new System.Drawing.Font("Arial Unicode MS", 10);
			m_combo.SelectedValueChanged += new EventHandler(this.SelectionChanged);
			m_combo.GotFocus += new EventHandler(m_combo_GotFocus);
			m_combo.DropDownClosed += new EventHandler(m_combo_DropDownClosed);
#if __MonoCS__	// FWNX-545
			m_combo.Parent.SizeChanged += new EventHandler(OnComboParentSizeChanged);
#endif
			StringTbl = stringTable;
			PopulateCombo(parameters);
			// We need to watch the cache for changes to our property.
			cache.DomainDataByFlid.AddNotification(this);
		}

		public override void Install(DataTree parent)
		{
			base.Install(parent);
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_combo != null)
				{
					m_combo.SelectedValueChanged -= new EventHandler(this.SelectionChanged);
					m_combo.GotFocus -= new EventHandler(m_combo_GotFocus);
					if (m_combo.Parent == null)
						m_combo.Dispose();
				}
				if (m_cache != null && m_cache.DomainDataByFlid != null)
					m_cache.DomainDataByFlid.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_combo = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected void PopulateCombo(XmlNode parameters)
		{
			Debug.Assert(StringTbl != null, "A StringTable must be provided for the enum combo slice.");
			m_combo.Items.Clear();
			XmlNode node =  parameters.SelectSingleNode("stringList");
			if (node == null)
				throw new ApplicationException ("The Enum editor requires a <stringList> element in the <deParams>");

			string[] labels = StringTbl.GetStringsFromStringListNode(node);
			int width = 0;
			using (Graphics g = m_combo.CreateGraphics())
			{
				foreach (string label in labels)
				{
					SizeF size = g.MeasureString(label, m_combo.Font);
					if (size.Width > width)
						width = (int)Math.Ceiling(size.Width);
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
				m_combo.Width = m_comboWidth;
		}
#endif

		protected override void UpdateDisplayFromDatabase()
		{
			int currentValue = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid);

			//nb: we are assuming that an enumerations start with 0
			m_combo.SelectedIndex = currentValue;
		}

		/// <summary>
		/// Event handler for selection changed in combo box.
		/// </summary>
		/// <param name="sender">Source control</param>
		/// <param name="e"></param>
		protected void SelectionChanged(object sender, EventArgs e)
		{
			Debug.Assert(m_combo != null);
			if (m_combo.DroppedDown)
				return; // don't want to update things while the user is manipulating the list. (See FWR-1728.)
			int oldValue = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid);
			int newValue = m_combo.SelectedIndex;
			// No sense in setting it to the same value.
			if (oldValue != newValue)
			{
				m_cache.DomainDataByFlid.BeginUndoTask(
					String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
					String.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
				m_cache.DomainDataByFlid.SetInt(Object.Hvo, m_flid, newValue);
				string sideEffectMethod = XmlUtils.GetAttributeValue(m_configurationNode, "sideEffect", null);
				if (!string.IsNullOrEmpty(sideEffectMethod))
				{
					MethodInfo info = Object.GetType().GetMethod(sideEffectMethod);
					if (info != null)
						info.Invoke(Object, new object[] { oldValue, newValue });
				}
				m_cache.DomainDataByFlid.EndUndoTask();
				// The changing value may affect the datatree display.  See LT-6539.
				bool fRefresh = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "refreshDataTreeOnChange", false);
				if (fRefresh)
					ContainingDataTree.RefreshList(false);
			}
		}

		void m_combo_DropDownClosed(object sender, EventArgs e)
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
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == Object.Hvo && tag == m_flid)
				UpdateDisplayFromDatabase();
		}

		#endregion
	}
}
