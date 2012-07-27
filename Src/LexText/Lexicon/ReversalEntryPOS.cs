using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This clerk is used to deal with the POSes of a IReversalIndex.
	/// </summary>
	public class ReversalEntryPOSClerk : ReversalClerk
	{
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri.PartsOfSpeechOA;
		}

		#region XCORE Message Handlers

		#endregion XCORE Message Handlers
	}
	/// <summary>
	/// ReversalPOSMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	public class ReversalPOSMenuHandler : DTMenuHandler
	{
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
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
				display.Text += StringTbl.GetString("(cannot move this)");
			return true; //we've handled this
		}

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
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, m_mediator.HelpTopicProvider, labels, null,
				LexEdStrings.ksCategoryToMoveTo, null))
			{
				dlg.SetHelpTopic("khtpChoose-CategoryToMoveTo");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					IPartOfSpeech currentPOS = POS;
					IPartOfSpeech newOwner = (IPartOfSpeech)dlg.ChosenOne.Object;
					UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoMoveRevCategory,
						LexEdStrings.ksRedoMoveRevCategory, cache.ActionHandlerAccessor,
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
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", newOwner.MainPossibility.Hvo);
				}
			}

			return true;
		}

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
				display.Text += StringTbl.GetString("(cannot merge this)");
			return true; //we've handled this
		}

		public bool OnMergeReversalPOS(object cmd)
		{
			FdoCache cache = Cache;
			var labels = new List<ObjectLabel>();
			foreach (IPartOfSpeech pos in MergeOrMoveCandidates)
				labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos, "ShortNameTSS", "best analysis"));
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, m_mediator.HelpTopicProvider, labels, null,
				LexEdStrings.ksCategoryToMergeInto, null))
			{
				dlg.SetHelpTopic("khtpMergeCategories");
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var currentPOS = POS;
					var survivor = (IPartOfSpeech)dlg.ChosenOne.Object;
					// Pass false to MergeObject, since we really don't want to merge the string info.
					UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoMergeRevCategory,
						LexEdStrings.ksRedoMergeRevCategory, cache.ActionHandlerAccessor,
						()=> survivor.MergeObject(currentPOS, false));
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordClerk doesn't know anything about subcategories.
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", survivor.MainPossibility.Hvo);
				}
			}

			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
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
						UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoPromote, LexEdStrings.ksRedoPromote,
							pos.Cache.ActionHandlerAccessor,
							()=>pos.SubPossibilitiesOS.Add(sliceObj));
						break;
					}
					case CmPossibilityListTags.kClassId:
					{
						var posList = (ICmPossibilityList)newOwner;
						UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoPromote, LexEdStrings.ksRedoPromote,
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
		protected  bool InFriendlyArea
		{
			get
			{
				return (m_mediator.PropertyTable.GetStringProperty("areaChoice", null) == "lists"
					&& m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lists", null) == "reversalToolReversalIndexPOS");
			}
		}
	}

	/// <summary>
	/// Summary description for ListExtension.
	/// </summary>
	public class ReversalIndexPOSRecordList : RecordList
	{


		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList owner="IReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, recordListNode);
			m_flid = CmPossibilityListTags.kflidPossibilities;
			Guid riGuid = AllReversalEntriesRecordList.GetReversalIndexGuid(mediator);
			if (riGuid != Guid.Empty)
			{
				IReversalIndex ri = cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
				m_owningObject = ri.PartsOfSpeechOA;
				m_fontName = cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem).DefaultFontName;
			}
			m_oldLength = 0;
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			ICmPossibilityList list = m_owningObject as ICmPossibilityList;
			return list.PossibilitiesOS.ToHvoArray();
		}

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
