using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: Likely disposition: Dump RespellerDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the respelling.
	// TODO: Other classes in this file may need to live on.
	//
	// TODO: There is no DlgListenerBase base class now (not moved), since there will be no listeners.
	// TODO: Spin off: RespellerTemporaryRecordClerk, OccurrenceComparer, OccurrenceSorter, RespellerRecordList, etc
	//
	/// <summary>
	/// XCore listener for the Respeller dlg.
	/// </summary>
	public class RespellerDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		/// <summary>
		/// Override to get a suitable label.
		/// </summary>
		protected string PersistentLabel
		{
			get { return "RespellerDlg"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Try to find a WfiWordform object corresponding the the focus selection.
		/// If successful return its guid, otherwise, return Guid.Empty.
		/// </summary>
		/// <returns></returns>
		private ITsString ActiveWord()
		{
			if (InFriendliestTool)
			{
				// we should be able to get our info from the current record clerk.
				// but return null if we can't get the info, otherwise we allow the user to
				// bring up the change spelling dialog and crash because no wordform can be found (LT-8766).
				var clerk = PropertyTable.GetValue<RecordClerk>("ActiveClerk");
				if (clerk == null || clerk.CurrentObject == null)
					return null;
				var wfiWordform = clerk.CurrentObject as IWfiWordform;
				if (wfiWordform == null)
					return null;
				var tssVern = wfiWordform.Form.BestVernacularAlternative;
				return tssVern;
			}
			var app = PropertyTable.GetValue<IApp>("App");
			if (app == null)
				return null;
			var window = app.ActiveMainWindow as IFwMainWnd;
			if (window == null)
				return null;
			var activeView = window.ActiveView;
			if (activeView == null)
				return null;
			var roots = activeView.AllRootBoxes();
			if (roots.Count < 1)
				return null;
			var helper = SelectionHelper.Create(roots[0].Site);
			if (helper == null)
				return null;
			var tssWord = helper.SelectedWord;
			if (tssWord != null)
			{
				// Check for a valid vernacular writing system.  (See LT-8892.)
				var ws = TsStringUtils.GetWsAtOffset(tssWord, 0);
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				IWritingSystem wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
				if (cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj))
					return tssWord;
			}
			return null;
		}

#if RANDYTODO
		/// <summary>
		/// Determine whether to show (and enable) the command to launch the respeller dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayLaunchRespellerDlg(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = InFriendlyArea;		// See LT-8641.
			display.Enabled = display.Visible && ActiveWord() != null;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Launch the Respeller dlg.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnLaunchRespellerDlg(object argument)
		{
			CheckDisposed();
			if (!InFriendliestTool)		// See LT-8641.
			{
				LaunchRespellerDlgOnWord(ActiveWord());
				return true;
			}
			var clerk = PropertyTable.GetValue<RecordClerk>("ActiveClerk");
			using (var luh = new RecordClerk.ListUpdateHelper(clerk))
			{
				var changesWereMade = false;
				// Launch the Respeller Dlg.
				using (var dlg = new RespellerDlg())
				{
					dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
#if RANDYTODO
					// TODO: That null in the next call used to be the xml config node.
#endif
					if (dlg.SetDlgInfo(null))
					{
						dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window"));
						changesWereMade = dlg.ChangesWereMade;
					}
					else
					{
						MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
					}
				}
				// The Respeller dialog can't make all necessary updates, since things like occurrence
				// counts depend on which texts are included, not just the data. So make sure we reload.
				luh.TriggerPendingReloadOnDispose = changesWereMade;
				if (changesWereMade)
				{
					// further try to refresh occurrence counts.
					var sda = clerk.VirtualListPublisher;
					while (sda != null)
					{
						if (sda is ConcDecorator)
						{
							((ConcDecorator)sda).Refresh();
							break;
						}
						if (!(sda is DomainDataByFlidDecoratorBase))
							break;
						sda = ((DomainDataByFlidDecoratorBase) sda).BaseSda;
					}
				}
			}
			return true;
		}

		private void LaunchRespellerDlgOnWord(ITsString tss)
		{
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return;

			var cache = PropertyTable.GetValue<FdoCache>("cache");
			var wordform = WordformApplicationServices.GetWordformForForm(cache, tss);
			using (var luh = new RecordClerk.ListUpdateHelper(PropertyTable.GetValue<RecordClerk>("ActiveClerk")))
			{
				// Launch the Respeller Dlg.
				using (var dlg = new RespellerDlg())
				{
#if RANDYTODO
					// TODO: Deal with that null (was xml config node).
#endif
					if (dlg.SetDlgInfo(wordform, null))
					{
						dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window"));
					}
					else
					{
						MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
					}
				}
				//// We assume that RespellerDlg made all the necessary changes to relevant lists.
				//// no need to reload due to a propchange (e.g. change in paragraph contents).
				//luh.TriggerPendingReloadOnDispose = false;
			}
		}

	#endregion XCORE Message Handlers

		/// <summary>
		/// The entire "Texts & Words" area is our friend.  See LT-8641.
		/// </summary>
		private bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == "textsWords");
			}
		}

		/// <summary>
		/// Inside "Analyses" tool is handled differently than elsewhere in the "Texts & Words" area.
		/// (But of course we must be in that area to be in the friendliest tool.)
		/// </summary>
		private bool InFriendliestTool
		{
			get
			{
				return InFriendlyArea && PropertyTable.GetValue<string>("ToolForAreaNamed_textsWords") == "Analyses";
			}
		}
	}

	public class RespellerTemporaryRecordClerk : TemporaryRecordClerk
	{
		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return false; // assume this will be false.
			}
			set
			{
				// do not do anything here, unless you want to manage the "ActiveClerk" property.
			}
		}
	}

	public class OccurrenceComparer : IComparer
	{
		private readonly FdoCache m_cache;
		private ISilDataAccessManaged m_sda;

		public OccurrenceComparer(FdoCache cache, ISilDataAccessManaged sda)
		{
			m_cache = cache;
			m_sda = sda;
		}
	#region IComparer Members

		public int Compare(object x1, object y1)
		{
			var x = (IManyOnePathSortItem) x1;
			var y = (IManyOnePathSortItem) y1;
			int hvoX = m_sda.get_ObjectProp(x.KeyObject, ConcDecorator.kflidTextObject);
			int hvoY = m_sda.get_ObjectProp(y.KeyObject, ConcDecorator.kflidTextObject);
			if (hvoX == hvoY)
			{
				// In the same text object, we can compare offsets.
				int offsetX = m_sda.get_IntProp(x.KeyObject, ConcDecorator.kflidBeginOffset);
				int offsetY = m_sda.get_IntProp(y.KeyObject, ConcDecorator.kflidBeginOffset);
				return offsetX - offsetY;
			}
			hvoX = m_sda.get_ObjectProp(x.KeyObject, ConcDecorator.kflidParagraph);
			hvoY = m_sda.get_ObjectProp(y.KeyObject, ConcDecorator.kflidParagraph);
			if (hvoX == hvoY)
			{
				// In the same paragraph (and not nested in the same caption), we can compare offsets.
				int offsetX = m_sda.get_IntProp(x.KeyObject, ConcDecorator.kflidBeginOffset);
				int offsetY = m_sda.get_IntProp(y.KeyObject, ConcDecorator.kflidBeginOffset);
				return offsetX - offsetY;
			}

			// While owning objects are the same type, get the owner of each, if they are the same,
			// compare their position in owner. Special case to put heading before body.
			// If owners are not the same type, do some trick that will make FLEx texts come before Scripture.
			var paraRepo = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>();
			ICmObject objX = paraRepo.GetObject(hvoX);
			ICmObject objY = paraRepo.GetObject(hvoY);
			for (;;)
			{
				var ownerX = objX.Owner;
				var ownerY = objY.Owner;
				if (ownerX == null)
				{
					if (ownerY == null)
						return hvoY - hvoX; // totally arbitrary but at least consistent
					return -1; // also arbitrary
				}
				if (ownerY == null)
					return 1; // arbitrary, object with shorter chain comes first.
				if (ownerX == ownerY)
				{
					var flidX = objX.OwningFlid;
					var flidY = objY.OwningFlid;
					if (flidX != flidY)
					{
						return flidX - flidY; // typically body and heading.
					}
					var indexX = m_cache.MainCacheAccessor.GetObjIndex(ownerX.Hvo, flidX, objX.Hvo);
					var indexY = m_cache.MainCacheAccessor.GetObjIndex(ownerY.Hvo, flidX, objY.Hvo);
					return indexX - indexY;
				}
				var clsX = ownerX.ClassID;
				var clsY = ownerY.ClassID;
				if (clsX != clsY)
				{
					// Typically one is in Scripture, the other in a Text.
					// Arbitrarily order things by the kind of parent they're in.
					// Enhance JohnT: this will need improvement if we go to hierarchical
					// structures like nested sections or a folder organization of texts.
					// We could loop all the way up, and then back down till we find a pair
					// of owners that are different.
					// (We reverse the usual X - Y in order to put Texts before Scripture
					// in this list as in the Texts list in FLEx.)
					return clsY - clsX;
				}
				objX = ownerX;
				objY = ownerY;
			}
		}

	#endregion
	}

	public class OccurrenceSorter : RecordSorter
	{
		private FdoCache m_cache;
		public override FdoCache Cache
		{
			set
			{
				m_cache = value;
			}
		}
		private ISilDataAccessManaged m_sdaSpecial;
		public ISilDataAccessManaged SpecialDataAccess
		{
			get { return m_sdaSpecial; }
			set { m_sdaSpecial = value; }
		}

		protected override IComparer getComparer()
		{
			return new OccurrenceComparer(m_cache, m_sdaSpecial);
		}

		/// <summary>
		/// Do the actual sort.
		/// </summary>
		/// <param name="records"></param>
		public override void Sort(ArrayList records)
		{
			var comp = new OccurrenceComparer(m_cache, m_sdaSpecial);
			//foreach (IManyOnePathSortItem item in records)
			MergeSort.Sort(ref records, comp);
		}

		/// <summary>
		/// We only ever sort this list to start with, don't think we should need this,
		/// but it's an abstract method so we have to have it.
		/// </summary>
		/// <param name="records"></param>
		/// <param name="newRecords"></param>
		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}

	public class RespellerRecordList : RecordList
	{
		public override void Init(XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList class="WfiWordform" field="Occurrences"/>
			BaseInit(recordListNode);
			var mdc = VirtualListPublisher.MetaDataCache;
			m_flid = mdc.GetFieldId2(WfiWordformTags.kClassId, "Occurrences", false);
			Sorter = new OccurrenceSorter
						{
							Cache = PropertyTable.GetValue<FdoCache>("cache"),
							SpecialDataAccess = VirtualListPublisher
						};
		}

		public override void ReloadList()
		{
			base.ReloadList();
			if (SortedObjects.Count > 0)
			{
				CurrentIndex = 0;
			}
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			// get the list from our decorated SDA.
			int[] objs = VirtualListPublisher.VecProp(m_owningObject.Hvo, m_flid);
			// copy the list to where it's expected to be found. (should this be necessary?)
			int chvo = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, VirtualFlid);
			VirtualListPublisher.Replace(m_owningObject.Hvo, VirtualFlid, 0, chvo, objs, objs.Length);
			return objs;
		}

	#region IVwNotifyChange implementation

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_owningObject != null && hvo == m_owningObject.Hvo && tag == m_flid)
			{
				ReloadList();
			}
		}

	#endregion IVwNotifyChange implementation
	}

	/// <summary>
	/// Static application level service class for wordforms.
	/// </summary>
	public static class WordformApplicationServices
	{
		/// <summary>
		/// Find an extant wordform,
		/// or create one (with nonundoable UOW), if one does not exist.
		/// </summary>
		public static IWfiWordform GetWordformForForm(FdoCache cache, ITsString form)
		{
			var servLoc = cache.ServiceLocator;
			var wordformRepos = servLoc.GetInstance<IWfiWordformRepository>();
			IWfiWordform retval;
			if (!wordformRepos.TryGetObject(form, false, out retval))
			{
				// Have to make it.
				var wordformFactory = servLoc.GetInstance<IWfiWordformFactory>();
				NonUndoableUnitOfWorkHelper.Do(servLoc.GetInstance<IActionHandler>(), () =>
					{
						retval = wordformFactory.Create(form);
					});
			}
			return retval;
		}
	}
#endif
}
