// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class ReferenceComboBoxSlice : FieldSlice
	{
		protected bool m_processSelectionEvent = true;
		protected int m_currentSelectedIndex;
		protected FwComboBox m_combo;
		protected IPersistenceProvider m_persistProvider;
		private string m_sNullItemLabel;

		/// <summary />
		public ReferenceComboBoxSlice(LcmCache cache, ICmObject obj, int flid, IPersistenceProvider persistenceProvider)
			: base(new UserControl(), cache, obj, flid)
		{
			m_persistProvider = persistenceProvider;
			m_combo = new FwComboBox
			{
				WritingSystemFactory = cache.WritingSystemFactory,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new System.Drawing.Font(cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName, 10),
				Dock = DockStyle.Left,
				Width = 200
			};
			if (!Application.RenderWithVisualStyles)
			{
				m_combo.HasBorder = false;
			}
			Control.Height = m_combo.Height; // Combo has sensible default height, UserControl does not.
			Control.Controls.Add(m_combo);

			m_combo.SelectedIndexChanged += SelectionChanged;
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			// Load the special strings from the string table if possible.  If not, use the
			// default (English) values.
			m_sNullItemLabel = StringTable.Table.GetString("NullItemLabel", "DetailControls/ReferenceComboBox");

			if (string.IsNullOrEmpty(m_sNullItemLabel) || m_sNullItemLabel == "*NullItemLabel*")
			{
				m_sNullItemLabel = DetailControlsStrings.ksNullLabel;
			}
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
					m_combo.SelectedIndexChanged -= SelectionChanged;
					Control.Controls.Remove(m_combo);
					m_combo.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_combo = null;
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
			var labels = ObjectLabel.CreateObjectLabels(Cache, Object.ReferenceTargetCandidates(m_flid), displayNameProperty);
			var currentValue = Cache.DomainDataByFlid.get_ObjectProp(Object.Hvo, m_flid);
			var idx = 0;
			foreach (var ol in labels)
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
		protected virtual string NullItemLabel => m_sNullItemLabel;

		/// <summary>
		/// Event handler for selection changed in combo box.
		/// </summary>
		protected virtual void SelectionChanged(object sender, EventArgs e)
		{
			Debug.Assert(m_combo != null);

			if (!m_processSelectionEvent)
			{
				return;
			}

			var newValue = m_combo.SelectedItem.ToString() == NullItemLabel ? 0 : (m_combo.SelectedItem as ObjectLabel).Object.Hvo;

			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), Object, () =>
			{
				Cache.DomainDataByFlid.SetObjProp(Object.Hvo, m_flid, newValue);
			});
		}

		/// <summary />
		public override void RegisterWithContextHelper()
		{
			if (Control != null)
			{
#if RANDYTODO
				// TODO: Skip it for now, and figure out what to do with those context menus
				string caption = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "label", ""));
				Publisher.Publish("RegisterHelpTargetWithId", new object[]{m_combo.Controls[0], caption, HelpId});
				//balloon was making it hard to actually click this
				//Mediator.SendMessage("RegisterHelpTargetWithId",
				//	new object[]{launcher.Controls[1], caption, HelpId, "Button"}, false);
#endif
			}
		}
	}
}