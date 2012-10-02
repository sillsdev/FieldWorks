using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Xml;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for AllReversalEntriesRecordList.
	/// </summary>
	public class AllReversalEntriesRecordList : RecordList
	{
		public AllReversalEntriesRecordList()
		{
		}

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList owner="IReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, recordListNode);
			//string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			m_flid = ObjectListPublisher.OwningFlid;
			int rih = GetReversalIndexHvo(mediator);
			if (rih > 0)
			{
				IReversalIndex ri = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetObject(rih);
				m_owningObject = ri;
				m_fontName = cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem).DefaultFontName;
			}
			m_oldLength = 0;
		}

		public override bool CanInsertClass(string className)
		{
			if (base.CanInsertClass(className))
				return true;
			return className == "ReversalIndexEntry";
		}

		public override bool CreateAndInsert(string className)
		{
			if (className != "ReversalIndexEntry")
				return base.CreateAndInsert(className);
			m_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			var ri = (IReversalIndex)m_owningObject;
			ri.EntriesOC.Add(m_newItem);
			var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if (extensions != null)
				extensions.PropChangedCompleted += SelectNewItem;
			return true;
		}

		private IReversalIndexEntry m_newItem;

		void SelectNewItem(object sender, bool fromUndoRedo)
		{
			((IActionHandlerExtensions) m_cache.ActionHandlerAccessor).PropChangedCompleted -= SelectNewItem;
			Clerk.OnJumpToRecord(m_newItem.Hvo);
		}

		/// <summary>
		/// Get the current reversal index hvo.  If there is none, create a new reversal index
		/// since there must not be any.  This fixes LT-6653.
		/// </summary>
		/// <param name="mediator"></param>
		/// <returns></returns>
		internal static int GetReversalIndexHvo(Mediator mediator)
		{
			string sHvo = (string)mediator.PropertyTable.GetValue("ReversalIndexHvo");
			if (String.IsNullOrEmpty(sHvo))
			{
				mediator.SendMessage("InsertReversalIndex_FORCE", null);
				sHvo = (string)mediator.PropertyTable.GetValue("ReversalIndexHvo");
			}
			int rih = int.Parse(sHvo);
			return rih;
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			IReversalIndex ri = m_owningObject as IReversalIndex;
			Debug.Assert(ri != null && ri.IsValidObject, "The owning IReversalIndex object is invalid!?");
			// Review: is there a better to to convert from List<Subclass> to List<Class>???
			List<IReversalIndexEntry> rgrie = ri.AllEntries;
			var rgcmo = new List<int>(rgrie.Count);
			foreach (IReversalIndexEntry rie in rgrie)
				rgcmo.Add(rie.Hvo);
			return rgcmo;
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		/// <param name="state"></param>
		public override void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete)
		{
			CheckDisposed();

			base.DeleteCurrentObject(state, thingToDelete);

			ReloadList();
		}
	}

	public class BulkReversalEntryPosEditor : BulkPosEditorBase
	{
		public BulkReversalEntryPosEditor()
		{
		}

		protected override ICmPossibilityList List
		{
			get
			{
				ICmPossibilityList list = null;
				int rih = int.Parse((string)m_mediator.PropertyTable.GetValue("ReversalIndexHvo"));
				if (rih > 0)
				{
					IReversalIndex ri = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetObject(rih);
					list = ri.PartsOfSpeechOA;
				}
				return list;
			}
		}

		public override List<int> FieldPath
		{
			get
			{
				return new List<int>(new int[] { ReversalIndexEntryTags.kflidPartOfSpeech,
					CmPossibilityTags.kflidName});
			}
		}

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want the PartOfSpeech indicated by m_selectedHvo, even if 0,
		/// to become the POS of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an IMoStemMsa.
		/// (a) If the owning entry has an IMoStemMsa with the
		/// right POS, set the sense to use it.
		/// (b) If the sense already refers to an IMoStemMsa, and any other senses
		/// of that entry which point at it are also to be changed, change the POS
		/// of the MSA.
		/// (c) If the entry has an IMoStemMsa which is not used at all, change it to the
		/// required POS and use it.
		/// (d) Make a new IMoStemMsa in the ILexEntry with the required POS and point the sense at it.
		/// </summary>
		public override void DoIt(Set<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			m_cache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksUndoBulkEditRevPOS,
				LexEdStrings.ksRedoBulkEditRevPOS);
			int i = 0;
			int interval = Math.Min(100, Math.Max(itemsToChange.Count / 50, 1));
			foreach (int entryId in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / itemsToChange.Count + 20;
					state.Breath();
				}
				IReversalIndexEntry entry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(entryId);
				if (m_selectedHvo == 0)
					entry.PartOfSpeechRA = null;
				else
					entry.PartOfSpeechRA = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(m_selectedHvo);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		protected override bool CanFakeIt(int hvo)
		{
			return true;
		}
	}
}
