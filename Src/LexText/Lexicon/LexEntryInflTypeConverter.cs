using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexEntryTypeConverter.
	/// </summary>
	public abstract class LexEntryTypeConverters : IUtility
	{
		protected UtilityDlg m_dlg;
		protected FdoCache m_cache;
		protected int m_flid;
		protected ICmObject m_obj;

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		protected static void DisableNodes(TreeNodeCollection nodes, int classId)
		{
			foreach (TreeNode tnode in nodes)
			{
				var node = tnode as ReallySimpleListChooser.LabelNode;
				if (node == null)
					continue;
				var label = node.Label;
				var obj = label.Object;
				if (obj.ClassID == classId)
					node.Enabled = false;
				DisableNodes(node.Nodes, classId);
			}
		}

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

		public abstract void OnSelection();

		#endregion IUtility implementation

		/// <summary>
		/// Overridden to provide a chooser with multiple selections (checkboxes and all).
		/// </summary>
		protected SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels, int classId)
		{
			var contents = from lexEntryType in m_cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities
						   where lexEntryType.ClassID == classId
						   select lexEntryType;
			var mediator = m_dlg.Mediator;
			var persistProvider =(IPersistenceProvider) mediator.PropertyTable.GetValue("persistProvider");
			string fieldName = mediator.StringTbl.GetString("VariantEntryTypes", "PossibilityListItemTypeNames");
			return new SimpleListChooser(persistProvider,
										 labels,
										 fieldName,
										 m_cache,
										 contents,
										 mediator.HelpTopicProvider);
		}

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
	public class LexEntryInflTypeConverter : LexEntryTypeConverters
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
				return LexEdStrings.ksConvertIrregularlyInflectedFormVariants;
			}
		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LexEdStrings.ksWhenToConvertIrregularlyInflectedFormVariants;
			m_dlg.WhatDescription = LexEdStrings.ksWhatIsConvertIrregularlyInflectedFormVariants;
			m_dlg.RedoDescription = LexEdStrings.ksCannotRedoConvertIrregularlyInflectedFormVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			Debug.Assert(m_dlg != null);
						m_cache = (FdoCache) m_dlg.Mediator.PropertyTable.GetValue("cache");
			UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoConvertIrregularlyInflectedFormVariants, LexEdStrings.ksRedoConvertIrregularlyInflectedFormVariants,
										m_cache.ActionHandlerAccessor,
										() => ShowDialogAndConvert(LexEntryInflTypeTags.kClassId));
		}

		#endregion IUtility implementation

		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryInflTypes(m_dlg.ProgressBar, itemsToChange);
		}

	}

	/// <summary>
	/// Summary description for LexEntryTypeConverter.
	/// </summary>
	public class LexEntryTypeConverter : LexEntryTypeConverters
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
				return LexEdStrings.ksConvertVariants;
			}
		}


		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LexEdStrings.ksWhenToConvertVariants;
			m_dlg.WhatDescription = LexEdStrings.ksWhatIsConvertVariants;
			m_dlg.RedoDescription = LexEdStrings.ksCannotRedoConvertVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			Debug.Assert(m_dlg != null);
			m_cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
			UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoConvertVariants, LexEdStrings.ksRedoConvertVariants,
										m_cache.ActionHandlerAccessor,
										() => ShowDialogAndConvert(LexEntryTypeTags.kClassId));

		}

		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryTypes(m_dlg.ProgressBar, itemsToChange);
		}

		#endregion IUtility implementation
	}
}
