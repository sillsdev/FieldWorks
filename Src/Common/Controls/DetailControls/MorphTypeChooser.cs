using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class MorphTypeChooser : SimpleListChooser
	{
		private CheckBox m_showAllTypesCheckBox;
		private readonly ICmObject m_obj;
		private readonly string m_displayNameProperty;
		private readonly int m_flid;

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		public MorphTypeChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IHelpTopicProvider helpTopicProvider) :
			base(persistProvider, labels, fieldName, helpTopicProvider)
		{
			InitMorphTypeForm(null);
		}

		private void InitMorphTypeForm(string sShowAllTypes)
		{
			sShowAllTypes = string.IsNullOrEmpty(sShowAllTypes) ? "&Show all types" : sShowAllTypes.Replace("_", "&");
			m_showAllTypesCheckBox = new CheckBox {Text = sShowAllTypes, AutoSize = true};
			m_showAllTypesCheckBox.CheckedChanged += m_showAllTypesCheckBox_CheckedChanged;
			m_checkBoxPanel.Controls.Add(m_showAllTypesCheckBox);
		}

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="obj">The obj.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="sShowAllTypes">The show all types string.</param>
		public MorphTypeChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, ICmObject obj, string displayNameProperty,
			int flid, string sShowAllTypes, IHelpTopicProvider helpTopicProvider) :
			base(persistProvider, labels, fieldName, helpTopicProvider)
		{
			m_obj = obj;
			m_displayNameProperty = displayNameProperty;
			m_flid = flid;
			InitMorphTypeForm(sShowAllTypes);
		}

		/// <summary>
		/// Get/set visibility of show all types check box
		/// </summary>
		public bool ShowAllTypesCheckBoxVisible
		{
			get
			{
				CheckDisposed();

				return m_showAllTypesCheckBox.Visible;
			}
			set
			{
				CheckDisposed();

				m_showAllTypesCheckBox.Visible = value;
			}
		}

		private void m_showAllTypesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			// If a node is selected, try selecting again when we get through.
			var selected = SelectedObject;
			IEnumerable<ICmObject> candidates;
			if (m_showAllTypesCheckBox.Checked)
			{
				var form = (IMoForm) m_obj;
				candidates = form.GetAllMorphTypeReferenceTargetCandidates();
			}
			else
			{
				candidates = m_obj.ReferenceTargetCandidates(m_flid);
			}
			IEnumerable<ObjectLabel> labels = ObjectLabel.CreateObjectLabels(m_cache, candidates,
				m_displayNameProperty, "best analorvern");
			LoadTree(labels, null, false);
			MakeSelection(selected);
		}
	}
}
