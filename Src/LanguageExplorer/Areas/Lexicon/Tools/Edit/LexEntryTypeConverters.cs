// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.UtilityTools;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Abstract base class for the two implementations: LexEntryTypeConverter & LexEntryInflTypeConverter.
	///
	/// LexEntryTypeConverter.
	/// What: This utility allows you to select which irregularly inflected form variant types should be converted
	///		to variant types (irregularly inflected form variant types are a special sub-kind of variant types).
	/// When: Run this utility when you need to convert one or more of your existing irregularly inflected form
	///		variant types to be variant types.  When a variant type is an irregularly inflected form variant type,
	///		it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
	///
	/// LexEntryInflTypeConverter.
	/// What: This utility allows you to select which variant types should be converted
	///		to irregularly inflected form variant types, which are a special sub-kind of variant types.
	/// When: Run this utility when you need to convert one or more of your existing variant types to be irregularly inflected form variant types.
	///		When a variant type is an irregularly inflected form variant type, it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
	/// </summary>
	internal abstract class LexEntryTypeConverters
	{
		/// <summary />
		protected UtilityDlg m_dlg;
		/// <summary />
		protected LcmCache m_cache;
		/// <summary />
		protected int m_flid;
		/// <summary />
		protected ICmObject m_obj;
		/// <summary />
		protected const string s_helpTopic = "khtpToolsConvertVariants";

		protected LexEntryTypeConverters(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		protected static void DisableNodes(TreeNodeCollection nodes, int classId)
		{
			foreach (TreeNode tnode in nodes)
			{
				var node = tnode as LabelNode;
				if (node == null)
					continue;
				var label = node.Label;
				var obj = label.Object;
				if (obj.ClassID == classId)
					node.Enabled = false;
				DisableNodes(node.Nodes, classId);
			}
		}

		/// <summary />
		protected abstract void Convert(IEnumerable<ILexEntryType> itemsToChange);

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public abstract string Label { get; }

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public abstract void OnSelection();

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public abstract void Process();

		#endregion IUtility implementation

		/// <summary>
		/// Overridden to provide a chooser with multiple selections (checkboxes and all).
		/// </summary>
		protected SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels, int classId)
		{
			var contents = from lexEntryType in m_cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities
				where lexEntryType.ClassID == classId
				select lexEntryType;
			var persistProvider = m_dlg.PropertyTable.GetValue<IPersistenceProvider>("persistProvider");
			string fieldName = StringTable.Table.GetString("VariantEntryTypes", "PossibilityListItemTypeNames");
			return new SimpleListChooser(persistProvider,
				labels,
				fieldName,
				m_cache,
				contents,
				m_dlg.PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
		}

		/// <summary />
		protected void ShowDialogAndConvert(int targetClassId)
		{
			// maybe there's a better way, but
			// this creates a temporary LexEntryRef in a temporary LexEntry
			var leFactory = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = leFactory.Create();
			var lerFactory = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			var ler = lerFactory.Create();
			entry.EntryRefsOS.Add(ler);
			m_flid = LexEntryRefTags.kflidVariantEntryTypes;
			m_obj = ler;
			var labels = ObjectLabel.CreateObjectLabels(m_cache,
				m_obj.ReferenceTargetCandidates(m_flid),
				"LexEntryType" /*"m_displayNameProperty*/,
				"best analysis");
			using (SimpleListChooser chooser = GetChooser(labels, targetClassId))
			{
				chooser.Cache = m_cache;
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);
				chooser.SetHelpTopic(s_helpTopic);
				var tv = chooser.TreeView;
				DisableNodes(tv.Nodes, targetClassId);
				m_dlg.Visible = false; // no reason to show the utility dialog, too
				var res = chooser.ShowDialog(m_dlg.FindForm());
				if (res == DialogResult.OK && chooser.ChosenObjects.Any())
				{
					var itemsToChange = (from lexEntryType in chooser.ChosenObjects
						where lexEntryType.ClassID != targetClassId
						select lexEntryType).Cast<ILexEntryType>();
					Convert(itemsToChange);
				}
			}
			entry.Delete(); // remove the temporary LexEntry
			m_dlg.Visible = true; // now we show the utility dialog again
		}
	}
}