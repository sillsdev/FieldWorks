// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

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
			var labels = ObjectLabel.CreateObjectLabels(Cache, MyCmObject.ReferenceTargetCandidates(m_flid), displayNameProperty);
			var currentValue = Cache.DomainDataByFlid.get_ObjectProp(MyCmObject.Hvo, m_flid);
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
			UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), MyCmObject, () =>
			{
				Cache.DomainDataByFlid.SetObjProp(MyCmObject.Hvo, m_flid, newValue);
			});
		}
	}
}