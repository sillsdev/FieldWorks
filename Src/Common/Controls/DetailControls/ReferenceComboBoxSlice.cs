// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReferenceComboBoxSlice.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implements the "referenceComboBox" XDE editor.
// </remarks>
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	public class ReferenceComboBoxSlice : FieldSlice
	{
		protected bool m_processSelectionEvent = true;
		protected int m_currentSelectedIndex;
		protected FwComboBox m_combo;
		protected IPersistenceProvider m_persistProvider;
		private string m_sNullItemLabel;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		public ReferenceComboBoxSlice(FdoCache cache, ICmObject obj, int flid,
			IPersistenceProvider persistenceProvider)
			: base(new UserControl(), cache, obj, flid)
		{
			m_persistProvider = persistenceProvider;
			m_combo = new FwComboBox();
			m_combo.WritingSystemFactory = cache.WritingSystemFactory;
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			m_combo.Font = new System.Drawing.Font(
				cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName,
				10);
			if (!Application.RenderWithVisualStyles)
				m_combo.HasBorder = false;
			m_combo.Dock = DockStyle.Left;
			m_combo.Width = 200;
			Control.Height = m_combo.Height; // Combo has sensible default height, UserControl does not.
			Control.Controls.Add(m_combo);

			m_combo.SelectedIndexChanged += SelectionChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified mediator.
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters">The configuration parameters.</param>
		/// ------------------------------------------------------------------------------------
		public override void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			base.Init(mediator, propertyTable, configurationParameters);

			// Load the special strings from the string table if possible.  If not, use the
			// default (English) values.
			m_sNullItemLabel = StringTable.Table.GetString("NullItemLabel",
				"DetailControls/ReferenceComboBox");

			if (string.IsNullOrEmpty(m_sNullItemLabel) || m_sNullItemLabel == "*NullItemLabel*")
				m_sNullItemLabel = DetailControlsStrings.ksNullLabel;
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
					m_combo.SelectedIndexChanged -= new EventHandler(SelectionChanged);
					Control.Controls.Remove(m_combo);
					m_combo.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_combo = null;
			m_mediator = null;
			m_persistProvider = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected void UpdateDisplayFromDatabase(string displayNameProperty)
		{
			DoUpdateDisplayFromDatabase(displayNameProperty);
		}
		protected override void UpdateDisplayFromDatabase()
		{
			DoUpdateDisplayFromDatabase(null);
		}
		private void DoUpdateDisplayFromDatabase(string displayNameProperty)
		{
			m_processSelectionEvent = false;
			m_currentSelectedIndex = 0;
			m_combo.Items.Clear();
			var labels = ObjectLabel.CreateObjectLabels(m_cache, Object.ReferenceTargetCandidates(m_flid),
				displayNameProperty);
			int currentValue = m_cache.DomainDataByFlid.get_ObjectProp(Object.Hvo, m_flid);
			int idx = 0;
			foreach(ObjectLabel ol in labels)
			{
				m_combo.Items.Add(ol);
				if (ol.Object.Hvo == currentValue)
				{
					m_combo.SelectedItem = ol;
					m_currentSelectedIndex = idx;
				}
				idx++;
			}
			idx = m_combo.Items.Add(NullItemLabel);
			if (currentValue == 0)
			{
				m_combo.SelectedIndex = idx;
				m_currentSelectedIndex = idx;
			}
			m_processSelectionEvent = true;
		}

		/// <summary>
		/// what is the default label for the null state
		/// </summary>
		protected virtual string NullItemLabel
		{
			get { return m_sNullItemLabel; }
		}

		/// <summary>
		/// Event handler for selection changed in combo box.
		/// </summary>
		/// <param name="sender">Source control</param>
		/// <param name="e"></param>
		protected virtual void SelectionChanged(object sender, EventArgs e)
		{
			Debug.Assert(m_combo != null);

			if (!m_processSelectionEvent)
				return;

			int newValue;
			if (m_combo.SelectedItem.ToString() == NullItemLabel)
				newValue = 0;
			else
				newValue = (m_combo.SelectedItem as ObjectLabel).Object.Hvo;

			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
				string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), m_obj, () =>
			{
				m_cache.DomainDataByFlid.SetObjProp(Object.Hvo, m_flid, newValue);
			});
		}

		/// <summary></summary>
		public override void RegisterWithContextHelper()
		{
			CheckDisposed();

			if (Control != null)
			{
				string caption = XmlUtils.GetLocalizedAttributeValue(ConfigurationNode, "label", "");
				Mediator.SendMessage("RegisterHelpTargetWithId",
					new object[]{m_combo.Controls[0], caption, HelpId}, false);
				//balloon was making it hard to actually click this
				//Mediator.SendMessage("RegisterHelpTargetWithId",
				//	new object[]{launcher.Controls[1], caption, HelpId, "Button"}, false);
			}
		}
	}
}
