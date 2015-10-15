using System;
using XCore;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for RecordBarListHandler.
	/// </summary>
	public class RecordBarListHandler : RecordBarHandler
	{
		protected Dictionary<int, ListViewItem> m_hvoToListViewItemTable = new Dictionary<int, ListViewItem>();

		//must have a constructor with no parameters, to use with the dynamic loader.
		public RecordBarListHandler()
		{
		}

		public override void ReloadItem(ICmObject currentObject)
		{
			CheckDisposed();

			throw new Exception("RecordBarListHandler.ReloadItem() is not implemented.");
		}

		public override void PopulateRecordBar(RecordList recList)
		{
			CheckDisposed();

			// The ListBar has a problem in that when it is populated for the first time the horizonal
			// scroll scrolls over a little ways over hiding the left most + or -. I (Rand) sent some
			// time searching this out and found that it is a bug in the ListView control.  It is not
			// our bug.  The scrolling happens when EnsureVisible() is called on the listview.  I found
			// a way around it. By calling this method twice the bug goes away, it looks like the list
			// must be populated, cleared, then repopulated before the bug is bypassed. There are also
			// other things that have an effect on it, such as ClearListBar() must be before
			// BeginUpdate().  Also selection must be made before ExpandAll() or CollapseAll() is called.

			// JohnT: no, the problem is when we EnsureVisible of a node that is wider than the window.
			// EnsureVisble tries to show as much as possible of the label; since it won't fit, it scrolls
			// horizontally and hides the plus/minus.
			// To avoid this if it is desired to EnsureVisible, use the EnsureSelectedNodeVisible routine
			// (which temporarily makes the label short while calling EnsureVisible).
			// (I'm not sure why Rand's comment is in this exact location, so I'm not deleting it.)

			if (this.IsShowing)
			{
				m_fOutOfDate = false;
			}
			else
			{
				m_fOutOfDate = true;
				return;
			}

			XWindow window = m_propertyTable.GetValue<XWindow>("window");
			window.TreeBarControl.IsFlatList = true;
			using (new WaitCursor(window))
			{
				ListView list = (ListView)window.ListStyleRecordList;
				list.BeginUpdate();
				window.ClearRecordBarList();	//don't want to directly clear the nodes, because that causes an event to be fired as every single node is removed!
				m_hvoToListViewItemTable.Clear();

				AddListViewItems(recList.SortedObjects, list);
				try
				{
					list.Font = new System.Drawing.Font(recList.FontName, recList.TypeSize);
				}
				catch(Exception error)
				{
					IApp app = m_propertyTable.GetValue<IApp>("App");
					ErrorReporter.ReportException(error, app.SettingsKey,
						m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress, null, false);
				}


				UpdateSelection(recList.CurrentObject);
				list.EndUpdate();

				if (list.SelectedItems.Count >0)
				{}//list.s .EnsureVisible();
			}
		}

		/// <summary>
		/// This implementation has nothing to do.
		/// </summary>
		public override void ReleaseRecordBar()
		{
			CheckDisposed();

		}

		protected virtual void AddListViewItems(ArrayList sortedObjects, ListView list)
		{
			//list.Visible=false;
			foreach (IManyOnePathSortItem item in sortedObjects)
			{
				var obj = item.RootObjectUsing(m_cache);

				if (obj.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
					continue;

				ListViewItem node =	AddListViewItem(obj, list);
				if (!m_hvoToListViewItemTable.ContainsKey(obj.Hvo))
					m_hvoToListViewItemTable.Add(obj.Hvo, node);
			}
		}

		protected virtual ListViewItem AddListViewItem(ICmObject obj, ListView list)
		{
			ListViewItem node = new ListViewItem(TsStringUtils.NormalizeToNFC(obj.ShortName) );
			node.Tag = obj.Hvo; //note that we could store the whole object instead.
			list.Items.Add(node);
			return node;
		}

		protected ListView List
		{
			get
			{
				XWindow window = m_propertyTable.GetValue<XWindow>("window");
				if (window != null)
					return (ListView)window.ListStyleRecordList;
				return null;
			}
		}

		public override void UpdateSelection(ICmObject currentObject)
		{
			CheckDisposed();

			if (currentObject == null)
			{
				ListView list = List;
				if (list != null)
					List.SelectedItems.Clear();
				return;
			}

			ListViewItem node = null;
			if (m_hvoToListViewItemTable.ContainsKey(currentObject.Hvo))
				node = m_hvoToListViewItemTable[currentObject.Hvo];
			//Debug.Assert(node != null);
			if(node != null && (Selected != node))
			{
				Selected = node;
				node.EnsureVisible();
			}
		}

		protected ListViewItem Selected
		{
			get
			{
				ListView list = List;
				if (list.SelectedItems == null || list.SelectedItems.Count == 0)
					return null;
				return list.SelectedItems[0];
			}
			set
			{
				value.Selected = true;
			}
		}
	}
}
