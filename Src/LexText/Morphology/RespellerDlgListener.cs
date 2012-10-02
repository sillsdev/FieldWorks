using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Filters;
using XCore;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// XCore listener for the Concorder dlg.
	/// </summary>
	public class RespellerDlgListener : DlgListenerBase
	{
		#region Properties

		/// <summary>
		/// Override to get a suitable label.
		/// </summary>
		protected override string PersistentLabel
		{
			get { return "RespellerDlg"; }
		}

		#endregion Properties

		#region Construction and Initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public RespellerDlgListener()
		{
		}

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
				RecordClerk clerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				if (clerk == null || clerk.CurrentObject == null)
					return null;
				IWfiWordform wfiWordform = clerk.CurrentObject as IWfiWordform;
				if (wfiWordform == null)
					return null;
				ITsString tssVern = wfiWordform.Form.BestVernacularAlternative;
				return tssVern;
			}
			if (!(FwApp.App is FwXApp))
				return null;
			FwXWindow window = (FwApp.App as FwXApp).ActiveMainWindow as FwXWindow;
			if (window == null)
				return null;
			IRootSite activeView = window.ActiveView;
			if (activeView == null)
				return null;
			List<IVwRootBox> roots = activeView.AllRootBoxes();
			if (roots.Count < 1)
				return null;
			SelectionHelper helper = SelectionHelper.Create(roots[0].Site);
			if (helper == null)
				return null;
			ITsString tssWord = helper.SelectedWord;
			if (tssWord != null)
			{
				// Check for a valid vernacular writing system.  (See LT-8892.)
				int ws = StringUtils.GetWsAtOffset(tssWord, 0);
				FdoCache cache = m_mediator.PropertyTable.GetValue("cache") as FdoCache;
				if (cache.LangProject.VernWssRC.Contains(ws))
					return tssWord;
			}
			return null;
		}


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
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk))
			{
				// Launch the Respeller Dlg.
				using (RespellerDlg dlg = new RespellerDlg())
				{
					XWindow xwindow = m_mediator.PropertyTable.GetValue("window") as XWindow;
					if (dlg.SetDlgInfo(m_mediator, m_configurationParameters))
					{
						dlg.ShowDialog(xwindow);
					}
					else
					{
						MessageBox.Show(MEStrings.ksCannotRespellWordform);
					}
				}
				// We assume that RespellerDlg made all the necessary changes to relevant lists.
				// no need to reload due to a propchange (e.g. change in paragraph contents).
				luh.TriggerPendingReloadOnDispose = false;
			}
			return true;
		}

		private void LaunchRespellerDlgOnWord(ITsString tss)
		{
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return;
			FdoCache cache = m_mediator.PropertyTable.GetValue("cache") as FdoCache;
			int hvoWordform = WfiWordform.FindOrCreateWordform(cache, tss);
			if (hvoWordform == 0)
				return; // paranoia.
			IWfiWordform wf = WfiWordform.CreateFromDBObject(cache, hvoWordform);
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk))
			{
				// Launch the Respeller Dlg.
				using (RespellerDlg dlg = new RespellerDlg())
				{
					XWindow xwindow = m_mediator.PropertyTable.GetValue("window") as XWindow;
					if (dlg.SetDlgInfo2(wf, m_mediator, m_configurationParameters))
					{
						dlg.ShowDialog(xwindow);
					}
					else
					{
						MessageBox.Show(MEStrings.ksCannotRespellWordform);
					}
				}
				// We assume that RespellerDlg made all the necessary changes to relevant lists.
				// no need to reload due to a propchange (e.g. change in paragraph contents).
				luh.TriggerPendingReloadOnDispose = false;
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
				return (m_mediator.PropertyTable.GetStringProperty("areaChoice", null) == "textsWords");
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
				return InFriendlyArea && m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_textsWords", null) == "Analyses";
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
		private FdoCache m_cache;

		public OccurrenceComparer(FdoCache cache)
		{
			m_cache = cache;
		}
		#region IComparer Members

		public int Compare(object x1, object y1)
		{
			ManyOnePathSortItem x = x1 as ManyOnePathSortItem;
			ManyOnePathSortItem y = y1 as ManyOnePathSortItem;
			int hvoX = x.KeyObject; // CmBaseAnnotations
			int hvoY = y.KeyObject;
			int hvoParaX = m_cache.GetObjProperty(hvoX, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			int hvoParaY = m_cache.GetObjProperty(hvoY, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			if (hvoParaX == hvoParaY)
			{
				// Compare begin offsets and return
				int offsetX = m_cache.GetIntProperty(hvoX, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				int offsetY = m_cache.GetIntProperty(hvoY, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				return offsetX - offsetY;
			}
			// Enhance: do something about picture captions??

			// While owning objects are the same type, get the owner of each, if they are the same,
			// compare their position in owner. Special case to put heading before body.
			// If owners are not the same type, do some trick that will make FLEx texts come before Scripture.
			int hvoObjX = hvoParaX;
			int hvoObjY = hvoParaY;
			do
			{

				int hvoOwnerX = m_cache.GetOwnerOfObject(hvoObjX);
				int hvoOwnerY = m_cache.GetOwnerOfObject(hvoObjY);
				if (hvoOwnerX == 0)
				{
					if (hvoOwnerY == 0)
						return hvoY - hvoX; // totally arbitrary but at least consistent
					return -1; // also arbitrary
				}
				if (hvoOwnerY == 0)
					return 1; // arbitrary, object with shorter chain comes first.
				if (hvoOwnerX == hvoOwnerY)
				{
					int flidX = m_cache.GetOwningFlidOfObject(hvoObjX);
					int flidY = m_cache.GetOwningFlidOfObject(hvoObjY);
					if (flidX != flidY)
					{
						return flidX - flidY; // typically body and heading.
					}
					int indexX = m_cache.GetObjIndex(hvoOwnerX, flidX, hvoObjX);
					int indexY = m_cache.GetObjIndex(hvoOwnerY, flidY, hvoObjY);
					return indexX - indexY;
				}
				int clsX = m_cache.GetClassOfObject(hvoOwnerX);
				int clsY = m_cache.GetClassOfObject(hvoOwnerY);
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
				hvoObjX = hvoOwnerX;
				hvoObjY = hvoOwnerY;
			} while (true); // We'll eventually reach the top of the ownership hierarchy.
		}

		#endregion
	}

	public class OccurrenceSorter: RecordSorter
	{
		private FdoCache m_cache;
		public override FdoCache Cache
		{
			set
			{
				m_cache = value;
			}
		}

		protected override IComparer getComparer()
		{
			return new OccurrenceComparer(m_cache);
		}
		/// <summary>
		/// Releases the IComparer that was given by getComparer().  Any neccesary cleanup work should be done here
		/// </summary>
		/// <param name="comp">The IComparer that was given by getComparer()</param>
		protected override void releaseComparer(IComparer comp)
		{

		}
		/// <summary>
		/// Do the actual sort.
		/// </summary>
		/// <param name="records"></param>
		public override void Sort(ArrayList records)
		{
			OccurrenceComparer comp = new OccurrenceComparer(m_cache);
			//foreach (ManyOnePathSortItem item in records)
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
		public RespellerRecordList()
		{
		}

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList class="WfiWordform" field="OccurrencesInTexts"/>
			BaseInit(cache, mediator, recordListNode);
			//string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(cache, "WfiWordform", "OccurrencesInTexts");
			Debug.Assert(vh != null);
			m_flid = vh.Tag;
			Sorter = new OccurrenceSorter();
			Sorter.Cache = cache;
		}

		public override void ReloadList()
		{
			base.ReloadList();
			if (SortedObjects.Count > 0)
			{
				CurrentIndex = 0;
			}
		}

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			IVwVirtualHandler handler = BaseVirtualHandler.GetInstalledHandler(m_cache, "WfiWordform", "OccurrencesInTexts");
			Debug.Assert(handler != null);

			IWfiWordform wf = m_owningObject as IWfiWordform;
			List<int> occurrences = wf.OccurrencesInTexts;

			IVwVirtualHandler vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerName("WfiWordform", "OccurrencesInCaptions");
			if (vh != null)
				occurrences.AddRange(m_cache.GetVectorProperty(wf.Hvo, vh.Tag, true));

			return new FdoObjectSet<ICmObject>(m_cache, occurrences.ToArray(), true);
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
}
