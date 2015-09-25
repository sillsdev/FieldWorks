// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using ProgressBarWrapper = SIL.FieldWorks.FdoUi.ProgressBarWrapper;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexEntryTypeConverter.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_dlg and m_cache are references")]
	internal abstract class LexEntryTypeConverters : IUtility
	{
		protected UtilityDlg m_dlg;
		protected FdoCache m_cache;
		protected int m_flid;
		protected ICmObject m_obj;
		protected const string s_helpTopic = "khtpToolsConvertVariants";

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
		/// Set the UtilityDlg.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);

				m_dlg = value;
			}
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public abstract void Process();

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public virtual string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return "should never see";
			}
		}
		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);

		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public abstract void OnSelection();

		#endregion IUtility implementation

		/// <summary>
		/// Overridden to provide a chooser with multiple selections (checkboxes and all).
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "mediator is a reference")]
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="m_dlg.FindForm() returns a reference")]
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

	/// <summary>
	/// Summary description for LexEntryInflTypeConverter.
	/// </summary>
	internal class LexEntryInflTypeConverter : LexEntryTypeConverters
	{
		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public override string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return LanguageExplorerResources.ksConvertIrregularlyInflectedFormVariants;
			}
		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToConvertIrregularlyInflectedFormVariants;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsConvertIrregularlyInflectedFormVariants;
			m_dlg.RedoDescription = LanguageExplorerResources.ksCannotRedoConvertIrregularlyInflectedFormVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			m_cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertIrregularlyInflectedFormVariants, LanguageExplorerResources.ksRedoConvertIrregularlyInflectedFormVariants,
										m_cache.ActionHandlerAccessor,
										() => ShowDialogAndConvert(LexEntryInflTypeTags.kClassId));
		}

		#endregion IUtility implementation

		/// <summary />
		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryInflTypes(new ProgressBarWrapper(m_dlg.ProgressBar), itemsToChange);
		}

	}

	/// <summary>
	/// Summary description for LexEntryTypeConverter.
	/// </summary>
	internal class LexEntryTypeConverter : LexEntryTypeConverters
	{
		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public override string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return LanguageExplorerResources.ksConvertVariants;
			}
		}


		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToConvertVariants;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsConvertVariants;
			m_dlg.RedoDescription = LanguageExplorerResources.ksCannotRedoConvertVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			m_cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertVariants, LanguageExplorerResources.ksRedoConvertVariants,
										m_cache.ActionHandlerAccessor,
										() => ShowDialogAndConvert(LexEntryTypeTags.kClassId));

		}

		#endregion IUtility implementation

		/// <summary />
		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryTypes(new ProgressBarWrapper(m_dlg.ProgressBar), itemsToChange);
		}
	}
}
