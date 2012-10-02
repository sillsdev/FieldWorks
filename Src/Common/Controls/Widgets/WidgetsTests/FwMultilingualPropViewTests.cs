using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Widgets
{
	[TestFixture()]
	public class FwMultilingualPropViewTests
	{
		/// <summary>
		/// Dummy implementation of IFwMultilingualPropViewDataSource to allow testing FwMultilingualPropView
		/// </summary>
		internal class DummyFwMultilingualPropViewDataSource : IFwMultilingualPropViewDataSource, IDisposable
		{
			protected PalasoWritingSystemManager m_writingSystemManager = new PalasoWritingSystemManager();
			protected List<int> m_list = new List<int>();

			public DummyFwMultilingualPropViewDataSource()
			{
				IWritingSystem ws;
				m_writingSystemManager.GetOrSet("en", out ws);
				m_list.Add(ws.Handle);
				m_writingSystemManager.GetOrSet("fr", out ws);
				m_list.Add(ws.Handle);
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~DummyFwMultilingualPropViewDataSource()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed
			{
				get;
				private set;
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					if (m_writingSystemManager != null)
						m_writingSystemManager.Dispose();
				}
				m_writingSystemManager = null;
				IsDisposed = true;
			}
			#endregion

			#region IFwMultilingualPropViewDataSource implementation
			public void SaveMultiLingualStrings()
			{
			}

			public FwMultilingualPropView Grid { get; set; }

			public void CheckSettings()
			{
			}

			public ITsString GetMultiStringAlt(int tag, int ws)
			{
				var bldr = COMInterfaces.TsStrBldrClass.Create();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, 0, m_list[0]);
				return bldr.GetString();
			}

			public void AddColumn(string name, int widthPct)
			{
				FwTextBoxColumn col = new FwTextBoxColumn();
				col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
				col.HeaderText = name;
				col.FillWeight = (float)widthPct;
				col.Resizable = DataGridViewTriState.True;
				col.UseTextPropsFontForCell = true;
				Grid.Columns.Add(col);
			}

			public List<FwMultilingualPropView.ColumnInfo> FieldsToDisplay {
				get {
					var list = new List<FwMultilingualPropView.ColumnInfo>();
					list.Add(new FwMultilingualPropView.ColumnInfo(1, "hello", 10));
					list.Add(new FwMultilingualPropView.ColumnInfo(2, "hello", 20));
					return list;
				}
			}

			public List<int> WritingSystemsToDisplay {
				get { return m_list; }
			}

			public CoreImpl.IWritingSystemManager WritingSystemManager {
				get { return m_writingSystemManager; }
			}

			/// <summary>Not used</summary>
			FdoCache IFwMultilingualPropViewDataSource.Cache { get; set;}

			/// <summary>Not used</summary>
			int IFwMultilingualPropViewDataSource.RootObject { get; set;}
			#endregion
		}

		/// <summary> </summary>
		[Test()]
		public void OnHandleCreated_NewFwMultilingualPropView_HandleGetsCreated()
		{
			using (var dataSource = new DummyFwMultilingualPropViewDataSource())
			using (var control = new FwMultilingualPropView(dataSource))
			{
			Assert.AreNotEqual(IntPtr.Zero, control.Handle);
		}
		}

		/// <summary>
		/// Test that displaying a FwMultilingualPropView and calling CommitEdit doesn't throw an exception.
		/// This test is to reproduce issue FWNX-472.
		/// </summary>
		[Test()]
		[Category("ByHand")]
		public void CommitEdit_PopulatedFwMultilingualPropView_ShouldNotThrowException()
		{
			using (var dataSource = new DummyFwMultilingualPropViewDataSource())
			using (var control = new FwMultilingualPropView(dataSource))
			{
				using (var f = new Form())
				{
			f.Controls.Add(control);
			f.Show();
			Application.DoEvents();

			control.CommitEdit(DataGridViewDataErrorContexts.Commit);
		}
	}
}
	}
}
