// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;
using LanguageExplorer.Dumpster;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;

#if RANDYTODO
// TODO: Break up file into one class per file after move commit is in.
#endif
namespace LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS
{
	/// <summary>
	/// This clerk is used to deal with the POSes of a IReversalIndex.
	/// </summary>
	internal class ReversalEntryPOSClerk : ReversalClerk
	{
		///// <summary>
		///// Contructor.
		///// </summary>
		///// <param name="id">Clerk id/name.</param>
		///// <param name="recordList">Record list for the clerk.</param>
		///// <param name="defaultSorter">The default record sorter.</param>
		///// <param name="defaultSortLabel"></param>
		///// <param name="defaultFilter">The default filter to use.</param>
		///// <param name="allowDeletions"></param>
		///// <param name="shouldHandleDeletion"></param>
		internal ReversalEntryPOSClerk(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base("ReversalEntriesPOS", new ReversalIndexPOSRecordList(serviceLocator, decorator, reversalIndex), new PropertyRecordSorter("ShortName"), "Default", null, true, true)
		{
		}

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri.PartsOfSpeechOA;
		}
	}

	/// <summary>
	/// ReversalPOSMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	internal class ReversalPOSMenuHandler : DTMenuHandler
	{
#if RANDYTODO
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public virtual bool OnDisplayMoveReversalPOS(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = CanMergeOrMove;
				display.Visible = InFriendlyArea;
			}
			if(!display.Enabled)
				display.Text += StringTable.Table.GetString("(cannot move this)");
			return true; //we've handled this
		}
#endif

		/// <summary />
		public bool OnMoveReversalPOS(object cmd)
		{
			FdoCache cache = Cache;
			var labels = new List<ObjectLabel>();
			foreach (IPartOfSpeech pos in MergeOrMoveCandidates)
			{
				if (!pos.SubPossibilitiesOS.Contains(POS))
				{
					labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos, "ShortNameTSS", "best analysis"));
				}
			}
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), labels, null,
				LanguageExplorerResources.ksCategoryToMoveTo, null))
			{
				dlg.SetHelpTopic("khtpChoose-CategoryToMoveTo");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					IPartOfSpeech currentPOS = POS;
					IPartOfSpeech newOwner = (IPartOfSpeech)dlg.ChosenOne.Object;
					UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveRevCategory,
						LanguageExplorerResources.ksRedoMoveRevCategory, cache.ActionHandlerAccessor,
						() =>
							{
								newOwner.MoveIfNeeded(currentPOS); //important when an item is moved into it's own subcategory
								if (!newOwner.SubPossibilitiesOS.Contains(currentPOS)) //this is also prevented in the interface, but I'm paranoid
								{
									newOwner.SubPossibilitiesOS.Add(currentPOS);
								}
							});
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordClerk doesn't know anything about subcategories.
					Publisher.Publish("JumpToRecord", newOwner.MainPossibility.Hvo);
				}
			}

			return true;
		}

#if RANDYTODO
		protected override bool CanInsert(Command command, Slice currentSlice, out int index)
		{
			if (base.CanInsert(command, currentSlice, out index))
			{
				switch (command.Id)
				{
					case "CmdDataTree-Insert-POS-AffixSlot":
					case "CmdDataTree-Insert-POS-AffixTemplate":
					case "CmdDataTree-Insert-POS-InflectionClass":
						return false;
					default:
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public virtual bool OnDisplayMergeReversalPOS(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = CanMergeOrMove;
				display.Visible = InFriendlyArea;
			}
			if(!display.Enabled)
				display.Text += StringTable.Table.GetString("(cannot merge this)");
			return true; //we've handled this
		}
#endif

		/// <summary />
		public bool OnMergeReversalPOS(object cmd)
		{
			FdoCache cache = Cache;
			var labels = new List<ObjectLabel>();
			foreach (IPartOfSpeech pos in MergeOrMoveCandidates)
				labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos, "ShortNameTSS", "best analysis"));
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), labels, null,
				LanguageExplorerResources.ksCategoryToMergeInto, null))
			{
				dlg.SetHelpTopic("khtpMergeCategories");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var currentPOS = POS;
					var survivor = (IPartOfSpeech)dlg.ChosenOne.Object;
					// Pass false to MergeObject, since we really don't want to merge the string info.
					UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeRevCategory,
						LanguageExplorerResources.ksRedoMergeRevCategory, cache.ActionHandlerAccessor,
						()=> survivor.MergeObject(currentPOS, false));
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordClerk doesn't know anything about subcategories.
					Publisher.Publish("JumpToRecord", survivor.MainPossibility.Hvo);
				}
			}

			return true;
		}

#if RANDYTODO
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public virtual bool OnDisplayPromoteReversalSubPOS(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null || slice.Object == null)
				display.Enabled = false;
			else
				display.Enabled = true;

			return true; //we've handled this
		}
#endif

		/// <summary />
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice and cache are references")]
		public bool OnPromoteReversalSubPOS(object cmd)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				FdoCache cache = m_dataEntryForm.Cache;
				var sliceObj = slice.Object as ICmPossibility;
				var newOwner = sliceObj.Owner.Owner;
				switch (newOwner.ClassID)
				{
					default:
						throw new ArgumentException("Illegal class.");
					case PartOfSpeechTags.kClassId:
					{
						var pos = (IPartOfSpeech)newOwner;
						UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoPromote, LanguageExplorerResources.ksRedoPromote,
							pos.Cache.ActionHandlerAccessor,
							()=>pos.SubPossibilitiesOS.Add(sliceObj));
						break;
					}
					case CmPossibilityListTags.kClassId:
					{
						var posList = (ICmPossibilityList)newOwner;
						UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoPromote, LanguageExplorerResources.ksRedoPromote,
							posList.Cache.ActionHandlerAccessor,
							()=>posList.PossibilitiesOS.Add(sliceObj));
						break;
					}
				}
			}
			return true;
		}

		private bool CanMergeOrMove
		{
			get
			{
				return POS.OwningList.ReallyReallyAllPossibilities.Count > 1;
			}
		}

		private Set<ICmPossibility> MergeOrMoveCandidates
		{
			get
			{
				var pos = POS;
				var candidates = pos.OwningList.ReallyReallyAllPossibilities;
				candidates.Remove(pos);
				return candidates;
			}
		}

		private IPartOfSpeech POS
		{
			get
			{
				return m_dataEntryForm.CurrentSlice.Object as IPartOfSpeech;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == "lists"
					&& PropertyTable.GetValue<string>("ToolForAreaNamed_lists") == "reversalToolReversalIndexPOS");
			}
		}
	}

	/// <summary>
	/// Summary description for ListExtension.
	/// </summary>
	internal class ReversalIndexPOSRecordList : RecordList
	{
		/// <summary />
		internal ReversalIndexPOSRecordList(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base(decorator, true, CmPossibilityListTags.kflidPossibilities, reversalIndex.PartsOfSpeechOA, string.Empty)
		{
			m_flid = CmPossibilityListTags.kflidPossibilities;
			m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
			m_oldLength = 0;
		}

		/// <summary />
		protected override IEnumerable<int> GetObjectSet()
		{
			ICmPossibilityList list = m_owningObject as ICmPossibilityList;
			return list.PossibilitiesOS.ToHvoArray();
		}

		/// <summary />
		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			if (className != "PartOfSpeech")
				return null;

			// A possibility list only allows one type of possibility to be owned in the list.
			ICmPossibilityList pssl = (ICmPossibilityList)m_owningObject;
			int possClass = pssl.ItemClsid;
			string sPossClass = m_cache.DomainDataByFlid.MetaDataCache.GetClassName((int)possClass);
			if (sPossClass != className)
				return null;
			foreach(ClassAndPropInfo cpi in m_insertableClasses)
			{
				if (cpi.signatureClassName == className)
				{
					return cpi;
				}
			}
			return null;
		}

		#region IVwNotifyChange implementation

		/// <summary />
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_owningObject != null && m_owningObject.Hvo != hvo)
				return;		// This PropChanged doesn't really apply to us.
			if (tag == m_flid)
				ReloadList();
			else
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		#endregion IVwNotifyChange implementation
	}
}
