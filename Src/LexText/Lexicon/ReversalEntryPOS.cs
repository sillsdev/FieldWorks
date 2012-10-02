using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Xml;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.XWorks.LexEd;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This clerk is used to deal with the POSes of a ReversalIndex.
	/// </summary>
	public class ReversalEntryPOSClerk : ReversalClerk
	{
		public ReversalEntryPOSClerk()
		{
		}

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
		//need a default constructor for dynamic loading
		public ReversalPOSMenuHandler()
		{
		}

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
			ObjectLabelCollection labels = new ObjectLabelCollection();
			foreach (IPartOfSpeech pos in MergeOrMoveCandidates)
				labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos.Hvo, "ShortNameTSS", "best analysis"));
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, labels, 0,
				LexEdStrings.ksCategoryToMoveTo, null))
			{
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					IPartOfSpeech currentPOS = POS;
					IPartOfSpeech newOwner = PartOfSpeech.CreateFromDBObject(cache, dlg.ChosenOne.Hvo);
					cache.BeginUndoTask(LexEdStrings.ksUndoMoveRevCategory,
						LexEdStrings.ksRedoMoveRevCategory);
					ICmObject newOwningObj = newOwner.MoveIfNeeded(currentPOS);
					newOwner.SubPossibilitiesOS.Append(currentPOS);
					cache.EndUndoTask();
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordClerk doesn't know anything about subcategories.
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", newOwner.MainPossibility.Hvo);
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
			ObjectLabelCollection labels = new ObjectLabelCollection();
			foreach (IPartOfSpeech pos in MergeOrMoveCandidates)
				labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos.Hvo, "ShortNameTSS", "best analysis"));
			using (SimpleListChooser dlg = new SimpleListChooser(cache, null, labels, 0,
				LexEdStrings.ksCategoryToMergeInto, null))
			{
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					IPartOfSpeech currentPOS = POS;
					IPartOfSpeech survivor = PartOfSpeech.CreateFromDBObject(cache, dlg.ChosenOne.Hvo);
					cache.BeginUndoTask(LexEdStrings.ksUndoMergeRevCategory,
						LexEdStrings.ksRedoMergeRevCategory);
					survivor.MergeObject(currentPOS, false); // Use false, since we really don;t want to merge the string info.
					cache.EndUndoTask();
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
				ICmPossibility sliceObj = slice.Object as ICmPossibility;
				int hvoNewOwner = cache.GetOwnerOfObject(sliceObj.OwnerHVO);
				switch (cache.GetClassOfObject(hvoNewOwner))
				{
					default:
						throw new ArgumentException("Illegal class.");
					case PartOfSpeech.kclsidPartOfSpeech:
					{
						IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(cache, hvoNewOwner);
						pos.SubPossibilitiesOS.Append(sliceObj);
						break;
					}
					case CmPossibilityList.kclsidCmPossibilityList:
					{
						ICmPossibilityList posList = CmPossibilityList.CreateFromDBObject(cache, hvoNewOwner);
						posList.PossibilitiesOS.Append(sliceObj);
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
				IPartOfSpeech pos = POS;
				Set<ICmPossibility> candidates = pos.OwningList.ReallyReallyAllPossibilities;
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
		public ReversalIndexPOSRecordList()
		{
		}

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList owner="ReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, recordListNode);
			m_flid = (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities;
			int rih = AllReversalEntriesRecordList.GetReversalIndexHvo(mediator);
			if (rih > 0)
			{
				IReversalIndex ri = ReversalIndex.CreateFromDBObject(cache, rih);
				m_owningObject = ri.PartsOfSpeechOA;
				m_fontName = ri.WritingSystemRA.DefaultSerif;
			}
			m_oldLength = 0;
		}

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			ICmPossibilityList list = m_owningObject as ICmPossibilityList;
			int[] items = list.PossibilitiesOS.HvoArray;
			return new FdoObjectSet<ICmObject>(m_cache, items, true);
		}

		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			if (className != "PartOfSpeech")
				return null;

			// A possibility list only allows one type of possibility to be owned in the list.
			ICmPossibilityList pssl = (ICmPossibilityList)m_owningObject;
			int possClass = pssl.ItemClsid;
			string sPossClass = m_cache.MetaDataCacheAccessor.GetClassName((uint)possClass);
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
