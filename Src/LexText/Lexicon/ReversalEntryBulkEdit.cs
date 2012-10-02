using System;
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

			// <recordList owner="ReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, recordListNode);
			//string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			IVwVirtualHandler vh =  cache.VwCacheDaAccessor.GetVirtualHandlerName("ReversalIndex", "AllEntries");
			if (vh == null)
			{
				vh = new AllReversalEntriesVh(null, cache);
				cache.VwCacheDaAccessor.InstallVirtual(vh);
			}
			m_flid = vh.Tag;
			int rih = GetReversalIndexHvo(mediator);
			if (rih > 0)
			{
				IReversalIndex ri = ReversalIndex.CreateFromDBObject(cache, rih);
				m_owningObject = ri;
				m_fontName = ri.WritingSystemRA.DefaultSerif;
			}
			m_oldLength = 0;
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

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			IVwVirtualHandler handler = (m_cache.MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId(m_flid);
			Debug.Assert(handler != null);

			IReversalIndex ri = m_owningObject as IReversalIndex;
			Debug.Assert(ri != null && ri.IsValidObject(), "The owning ReversalIndex object is invalid!?");
			// Force the handler to (re)load the property.
			handler.Load(ri.Hvo, m_flid, ri.WritingSystemRAHvo, m_cache.MainCacheAccessor as IVwCacheDa);
			int[] items = m_cache.GetVectorProperty(ri.Hvo, m_flid, true);
			return new FdoObjectSet<ICmObject>(m_cache, items, true);
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		/// <param name="state"></param>
		public override void DeleteCurrentObject(ProgressState state)
		{
			CheckDisposed();

			base.DeleteCurrentObject(state);

			ReloadListIfPossible();
		}

		#region IVwNotifyChange implementation

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (hvo == m_cache.LangProject.LexDbOAHvo &&
				tag == (int)LexDb.LexDbTags.kflidReversalIndexes &&
				cvDel > 0)
			{
				// Our owning object may have been deleted.
				int rih = int.Parse((string)m_mediator.PropertyTable.GetValue("ReversalIndexHvo"));
				if (rih > 0 && !m_cache.IsValidObject(rih))
				{
					if (m_owningObject != null && m_owningObject.Hvo != rih && m_owningObject.IsValidObject())
					{
						rih = m_owningObject.Hvo;
					}
					else
					{
						int cobjNew;
						rih = ReversalClerk.ReversalIndexAfterDeletion(m_cache, out cobjNew);
					}
					m_mediator.PropertyTable.SetProperty("ReversalIndexHvo", rih.ToString());
				}
				if (rih > 0 && (m_owningObject == null || m_owningObject.Hvo != rih))
				{
					IReversalIndex ri = ReversalIndex.CreateFromDBObject(m_cache, rih);
					m_owningObject = ri;
					m_fontName = ri.WritingSystemRA.DefaultSerif;
				}
				else
				{
					return;		// We're still okay without any changes.
				}
			}
			else if (m_owningObject != null && m_owningObject.Hvo != hvo)
			{
				return;		// This PropChanged doesn't apply to us.
			}
			ReloadListIfPossible();
		}

		private void ReloadListIfPossible()
		{
			IVwVirtualHandler handler = (m_cache.MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId(m_flid);
			if (handler != null)
				ReloadList();
		}

		#endregion IVwNotifyChange implementation
	}

	public class AllReversalEntriesVh : BaseVirtualHandler
	{
		private FdoCache m_cache;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="configuration">the XML that configures this handler</param>
		/// <param name="cache"></param>
		public AllReversalEntriesVh(XmlNode configuration, FdoCache cache)
		{
			m_cache = cache;
			ClassName = "ReversalIndex";
			FieldName = "AllEntries";
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
			Writeable = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The value of this property is the hvo of the owning object that is of class m_clid (or is a subclass of that).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			// Do we want the subentries?
			string sql = string.Format("SELECT Id FROM dbo.fnGetOwnedIds({0},{1},{2})",
				hvo,
				(int)ReversalIndex.ReversalIndexTags.kflidEntries,
				(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidSubentries);
			int[] vals = DbOps.ReadIntArrayFromCommand(m_cache, sql, null);
			cda.CacheVecProp(hvo, tag, vals, vals.Length);
		}

		/// <summary>
		/// This property may as well be computed every time it is used.
		/// </summary>
		public override bool ComputeEveryTime
		{
			get
			{
				return true;
			}
			set
			{	//review JT(JH): could you add a comment saying why this is not symmetric with the get?
				base.ComputeEveryTime = value;
			}
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
					IReversalIndex ri = ReversalIndex.CreateFromDBObject(m_cache, rih);
					list = ri.PartsOfSpeechOA;
				}
				return list;
			}
		}

		public override List<int> FieldPath
		{
			get
			{
				return new List<int>(new int[] { (int)ReversalIndexEntry.ReversalIndexEntryTags.kflidPartOfSpeech,
					(int)CmPossibility.CmPossibilityTags.kflidName});
			}
		}

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want the PartOfSpeech indicated by m_selectedHvo, even if 0,
		/// to become the POS of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an MoStemMsa.
		/// (a) If the owning entry has an MoStemMsa with the
		/// right POS, set the sense to use it.
		/// (b) If the sense already refers to an MoStemMsa, and any other senses
		/// of that entry which point at it are also to be changed, change the POS
		/// of the MSA.
		/// (c) If the entry has an MoStemMsa which is not used at all, change it to the
		/// required POS and use it.
		/// (d) Make a new MoStemMsa in the LexEntry with the required POS and point the sense at it.
		/// </summary>
		public override void DoIt(Set<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			m_cache.BeginUndoTask(LexEdStrings.ksUndoBulkEditRevPOS, LexEdStrings.ksRedoBulkEditRevPOS);
			BulkEditBar.ForceRefreshOnUndoRedo(m_cache.MainCacheAccessor);
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
				IReversalIndexEntry entry = ReversalIndexEntry.CreateFromDBObject(m_cache, entryId);
				entry.PartOfSpeechRAHvo = m_selectedHvo;
			}
			m_cache.EndUndoTask();
		}

		protected override bool CanFakeIt(int hvo)
		{
			return true;
		}
	}
}
