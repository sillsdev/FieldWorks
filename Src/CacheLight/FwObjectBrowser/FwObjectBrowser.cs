using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace FwObjectBrowser
{
	public partial class FwObjectBrowser : Form, IFWDisposable
	{
		private RealDataCache m_cache;
		private Dictionary<int, uint> m_objects = new Dictionary<int, uint>();
		private Stack<SelectedObject> m_back = new Stack<SelectedObject>();
		private Stack<SelectedObject> m_forward = new Stack<SelectedObject>();
		private SelectedObject m_current;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		private IFwMetaDataCache MetaDataCache
		{
			get { return m_cache.MetaDataCache; }
		}

		private ISilDataAccess SilDataAccess
		{
			get { return m_cache; }
		}

		private IVwCacheDa VwCacheDa
		{
			get { return m_cache; }
		}

		public FwObjectBrowser()
		{
			InitializeComponent();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = "Open Fieldworks Language Project";
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = "Project files (*.xml)|*.xml";
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					Cursor = Cursors.WaitCursor;
					try
					{
						DateTime start = DateTime.Now;
						string modelDir = DirectoryFinder.FwSourceDirectory;
						modelDir = modelDir.Substring(0, modelDir.LastIndexOf('\\'));
						modelDir = Path.Combine(modelDir, @"Output\XMI");
						string mdcPathname = Path.Combine(modelDir, "xmi2cellar3.xml");
						using (RealCacheLoader loader = new RealCacheLoader())
						{
							m_objects.Clear();
							m_cache = loader.LoadCache(mdcPathname, dlg.FileName, m_objects);
						}
						DateTime end = DateTime.Now;
						TimeSpan span = new TimeSpan(end.Ticks - start.Ticks);
						string totalTime = String.Format("Minutes: {0}, Seconds: {1}, Millseconds: {2}",
							((span.Hours * 60) + span.Minutes).ToString(), span.Seconds.ToString(), span.Milliseconds.ToString());
						m_tstbLoadTime.Text = String.Format("Time to Load: {0}", totalTime);
					}
					finally
					{
						Cursor = Cursors.Default;
					}
#if FlushMemory
					MemoryManagement.FlushMemory();
#endif
					uint mainClid = MetaDataCache.GetClassId("LangProject");
					int mainHvo = 0;
					foreach (KeyValuePair<int, uint> kvp in m_objects)
					{
						//uint clid = (uint)m_cache.get_IntProp(kvp.Key, (int)CmObjectFields.kflidCmObject_Class);
						if (kvp.Value == mainClid)
						{
							mainHvo = kvp.Key;
							break;
						}

					}
					m_current = new SelectedObject(0, mainHvo, mainClid);
					PopulateMainListView();
				}
			}
		}

		private void SetNavButtons()
		{
			m_tsbBack.Enabled = m_back.Count > 0;
			m_tsbForward.Enabled = m_forward.Count > 0;
		}

		private void PopulateSecondListView(ITsMultiString mainObject)
		{
			m_lvDetails.Clear();
			m_lvDetails.View = View.Details;
			m_lvDetails.Sorting = SortOrder.Ascending;
			m_lvDetails.SmallImageList = null;
			m_lvDetails.SuspendLayout();
			m_lvDetails.Columns.Add("WS", 50);
			m_lvDetails.Columns.Add("Text", 200);
			ListViewItem[] lvis = new ListViewItem[mainObject.StringCount];
			for (int i = 0; i < mainObject.StringCount; ++i)
			{
				int ws;
				ITsString tss = mainObject.GetStringFromIndex(i, out ws);
				uint wsLocalFlid = MetaDataCache.GetFieldId("LgWritingSystem", "ICULocale", false);
				string wsLabel = SilDataAccess.get_UnicodeProp(ws, (int)wsLocalFlid);
				ListViewItem lvi = new ListViewItem(wsLabel);
				lvi.Tag = ws;
				lvi.SubItems.Add(tss.Text);
				lvis[i] = lvi;
			}
			m_lvDetails.Items.AddRange(lvis);
			m_lvDetails.ResumeLayout();
		}

		private void PopulateSecondListView(SelectedObject selObj)
		{
			m_lvDetails.Clear();
			m_lvDetails.View = View.Details;
			m_lvDetails.Sorting = SortOrder.Ascending;
			m_lvDetails.SmallImageList = null;
			m_lvDetails.Columns.Add("Field", 100);
			m_lvDetails.Columns.Add("Data", 100);
			PopulateListView(m_lvDetails, selObj);
		}

		private void PopulateSecondListView(SelectedVector selVector)
		{
			Cursor = Cursors.WaitCursor;
			m_lvDetails.Clear();
			SuspendLayout();
			m_lvDetails.Sorting = SortOrder.None;
			m_lvDetails.View = View.List;
			m_lvDetails.SmallImageList = m_ilSmall;
			int count = selVector.m_hvos.Count;
			ListViewItem[] lvis = new ListViewItem[count];
			for (int i = 0; i < count; ++i)
			{
				int hvo = selVector.m_hvos[i];
				uint clid = (uint)m_cache.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
				string classname = m_cache.MetaDataCache.GetClassName(clid);
				ListViewItem lvi = new ListViewItem(String.Format("{0}: ({1} of {2})", classname, i + 1, count), 0);
				SelectedObject so = new SelectedObject(0, hvo, clid);
				lvi.Tag = so;
				lvis[i] = lvi;
			}
			m_lvDetails.Items.AddRange(lvis);
			ResumeLayout();
			Cursor = Cursors.Default;
		}

		private void PopulateListView(ListView listView, SelectedObject selObj)
		{
			SuspendLayout();

			// Get all fields from mdc.
			uint[] uflids;
			// First find out how many there are.
			int flidSize = MetaDataCache.GetFields(selObj.m_clid, true,
				(int)CellarModuleDefns.kgrfcptAll, 0, null);
			// Now get them for real.
			using (ArrayPtr flids = MarshalEx.ArrayToNative(flidSize, typeof(uint)))
			{
				flidSize = MetaDataCache.GetFields(selObj.m_clid, true,
					(int)CellarModuleDefns.kgrfcptAll, flidSize, flids);
				uflids = (uint[])MarshalEx.NativeToArray(flids, flidSize, typeof(uint));
			}
			List<ListViewItem> list = new List<ListViewItem>();
			foreach (uint flid in uflids)
			{
				if (flid > 0)
				{
					string classname;
					string fieldname = MetaDataCache.GetFieldName(flid);
					ListViewItem lvi = new ListViewItem(fieldname, 0); // listView.Items.Add(fieldname);
					list.Add(lvi);
					lvi.Tag = flid;
					// TODO: Show some kind of data in the second column of the lvi.
					// For basic data types, just show the data, if it exists (NA, if not).
					// Selecting a basic data type field, will clear the right panel.
					// For objects, we need to distinguish between owning/reference and atomic/seq/coll:
					// Just use the FDO 'standard' of OA, OS, OC, RA, RS, and RC, plus:
					// atomic: Object class. Selection of the lvi will then show the object in the right pane.
					// seq/coll: Maybe size of vector. Selection shows basic list of items in the vector in the right pane.
					string data = "*N/A";
					object obj = SilDataAccess.get_Prop(selObj.m_hvo, (int)flid);
					if (obj != null)
					{
						int flidType = MetaDataCache.GetFieldType(flid);
						switch (flidType)
						{
							case (int)CellarModuleDefns.kcptOwningCollection:
							case (int)CellarModuleDefns.kcptOwningSequence:
							case (int)CellarModuleDefns.kcptReferenceSequence:
							case (int)CellarModuleDefns.kcptReferenceCollection:
								string pfx = String.Empty;
								switch (flidType)
								{
									case (int)CellarModuleDefns.kcptOwningCollection:
										pfx = "OC";
										break;
									case (int)CellarModuleDefns.kcptOwningSequence:
										pfx = "OS";
										break;
									case (int)CellarModuleDefns.kcptReferenceSequence:
										pfx = "RS";
										break;
									case (int)CellarModuleDefns.kcptReferenceCollection:
										pfx = "RC";
										break;
								}
								int vecSize = m_cache.get_VecSize(selObj.m_hvo, (int)flid);
								List<int> hvos = (List<int>)m_cache.get_Prop(selObj.m_hvo, (int)flid);
								data = String.Format("{0}: {1} items", pfx, vecSize);
								lvi.Tag = new SelectedVector(selObj.m_hvo, flid, hvos);
								break;
							case (int)CellarModuleDefns.kcptInteger:
								if (flid == (int)CmObjectFields.kflidCmObject_Class)
								{
									classname = MetaDataCache.GetClassName(selObj.m_clid);
									data = String.Format("{0}: {1}", obj.ToString(), classname);
								}
								else
								{
									data = obj.ToString();
								}
								break;
							case (int)CellarModuleDefns.kcptTime:
								DateTime dt = new DateTime((long)obj);
								data = dt.ToString();
								break;
							case (int)CellarModuleDefns.kcptOwningAtom: // Fall through.
							case (int)CellarModuleDefns.kcptReferenceAtom:
								int objId = (int)obj;
								int clid = SilDataAccess.get_IntProp(objId, (int)CmObjectFields.kflidCmObject_Class);
								classname = MetaDataCache.GetClassName((uint)clid);
								data = String.Format("{0}: a(n) {1}",
									(flidType == (int)CellarModuleDefns.kcptOwningAtom) ? "OA" : "RA",
									classname);
								lvi.Tag = new SelectedObject(flid, objId, (uint)clid);
								break;
							case (int)CellarModuleDefns.kcptString: // Fall through.
							case (int)CellarModuleDefns.kcptBigString:
								ITsString tssString = (ITsString)obj;
								data = tssString.Text;
								break;
							case (int)CellarModuleDefns.kcptMultiUnicode: // Fall through.
							case (int)CellarModuleDefns.kcptMultiBigUnicode: // Fall through.
							case (int)CellarModuleDefns.kcptMultiString: // Fall through.
							case (int)CellarModuleDefns.kcptMultiBigString:
								if (obj is ITsMultiString)
								{
									ITsMultiString tsms = obj as ITsMultiString;
									uint wsLocalFlid = MetaDataCache.GetFieldId("LgWritingSystem", "ICULocale", false);
									if (tsms.StringCount > 0)
									{
										int ws;
										ITsString tss = tsms.GetStringFromIndex(0, out ws);
										string wsLabel = SilDataAccess.get_UnicodeProp(ws, (int)wsLocalFlid);
										data = String.Format("{0}: {1}", wsLabel, tss.Text);
									}
									lvi.Tag = tsms;
								}
								break;
							case (int)CellarModuleDefns.kcptUnicode: // Fall through.
							case (int)CellarModuleDefns.kcptBigUnicode:
								data = (string)obj;
								break;
							default:
								data = obj.ToString();
								break;
						}
					}
					lvi.SubItems.Add(data);
				}
			}
			listView.Items.AddRange(list.ToArray());
			listView.Sorting = SortOrder.Ascending;
			listView.Sort();
			ResumeLayout();
		}

		private void PopulateMainListView()
		{
			m_lvMainObject.Items.Clear();
			m_lvDetails.Clear();
			PopulateListView(m_lvMainObject, m_current);
			SetNavButtons();
		}

		private void ResetCurrent(SelectedObject newCurrent)
		{
			m_back.Push(m_current);
			m_forward.Clear();
			m_current = newCurrent;
			PopulateMainListView();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_tsbForward_Click(object sender, EventArgs e)
		{
			m_back.Push(m_current);
			m_current = m_forward.Pop();
			PopulateMainListView();
			SetNavButtons();
		}

		private void m_tsbBack_Click(object sender, EventArgs e)
		{
			m_forward.Push(m_current);
			m_current = m_back.Pop();
			PopulateMainListView();
			SetNavButtons();
		}

		private void m_lvMainObject_DoubleClick(object sender, EventArgs e)
		{
			ListView.SelectedListViewItemCollection col = m_lvMainObject.SelectedItems;
			if (col.Count > 0)
			{
				m_lvDetails.Clear();
				ListViewItem lvi = m_lvMainObject.SelectedItems[0];
				object tag = lvi.Tag;
				if (tag is SelectedObject)
				{
					ResetCurrent((SelectedObject)tag);
				}
				else if (tag is ITsMultiString)
				{
					PopulateSecondListView(tag as ITsMultiString);
				}
			}
		}

		private void m_lvMainObject_Click(object sender, EventArgs e)
		{
			ListView.SelectedListViewItemCollection col = m_lvMainObject.SelectedItems;
			if (col.Count > 0)
			{
				m_lvDetails.Clear();
				ListViewItem lvi = m_lvMainObject.SelectedItems[0];
				object tag = lvi.Tag;
				if (tag is SelectedObject)
				{
					PopulateSecondListView((SelectedObject)tag);

				}
				else if (tag is ITsMultiString)
				{
					PopulateSecondListView(tag as ITsMultiString);
				}
				else if (tag is SelectedVector)
				{
					PopulateSecondListView((SelectedVector)tag);
				}
			}
		}

		private void m_lvDetails_DoubleClick(object sender, EventArgs e)
		{
			ListView.SelectedListViewItemCollection col = m_lvDetails.SelectedItems;
			if (col.Count > 0)
			{
				ListViewItem lvi = m_lvDetails.SelectedItems[0];
				object tag = lvi.Tag;
				if (tag is SelectedObject)
				{
					ResetCurrent((SelectedObject)tag);
				}
				/*
				else if (tag is SelectedVector)
				{
					SelectedVector selVec = (SelectedVector)tag;
					//uint clid = m_cache.get_IntProp
					//SelectedObject selObj = new SelectedObject(0, selVec.m_hvo);
				}*/
			}
		}
	}

	internal struct SelectedObject
	{
		public uint m_owningFild;
		public int m_hvo;
		public uint m_clid;

		public SelectedObject(uint owningFlid, int hvo, uint clid)
		{
			m_owningFild = owningFlid;
			m_hvo = hvo;
			m_clid = clid;
		}
	}

	internal struct SelectedVector
	{
		public int m_hvo;
		public uint m_vectorFlid;
		public List<int> m_hvos;

		public SelectedVector(int hvo, uint vectorFlid, List<int> hvos)
		{
			m_hvo = hvo;
			m_vectorFlid = vectorFlid;
			m_hvos = hvos;
		}
	}

#if FlushMemory
	internal class MemoryManagement
	{
		[DllImportAttribute("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

		public static void FlushMemory()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
			}
		}
	}
#endif
}