// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl.Text;
using SIL.CoreImpl.WritingSystems;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Widgets
{
	[TestFixture]
	public class FwMultilingualPropViewTests
	{
		/// <summary>
		/// Dummy implementation of IFwMultilingualPropViewDataSource to allow testing FwMultilingualPropView
		/// </summary>
		internal class DummyFwMultilingualPropViewDataSource : IFwMultilingualPropViewDataSource
		{
			protected WritingSystemManager m_writingSystemManager = new WritingSystemManager();
			protected List<int> m_list = new List<int>();

			public DummyFwMultilingualPropViewDataSource()
			{
				CoreWritingSystemDefinition ws;
				m_writingSystemManager.GetOrSet("en", out ws);
				m_list.Add(ws.Handle);
				m_writingSystemManager.GetOrSet("fr", out ws);
				m_list.Add(ws.Handle);
			}

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
				var bldr = TsStringUtils.MakeStrBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, 0, m_list[0]);
				return bldr.GetString();
			}

			public void AddColumn(string name, int widthPct)
			{
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

			public WritingSystemManager WritingSystemManager {
				get { return m_writingSystemManager; }
			}

			/// <summary>Not used</summary>
			FdoCache IFwMultilingualPropViewDataSource.Cache { get; set;}

			/// <summary>Not used</summary>
			int IFwMultilingualPropViewDataSource.RootObject { get; set;}
			#endregion
		}

		/// <summary> </summary>
		[Test]
		public void OnHandleCreated_NewFwMultilingualPropView_HandleGetsCreated()
		{
			var dataSource = new DummyFwMultilingualPropViewDataSource();
			using (var control = new FwMultilingualPropView(dataSource))
			{
				Assert.AreNotEqual(IntPtr.Zero, control.Handle);
			}
		}

		/// <summary>
		/// Test that displaying a FwMultilingualPropView and calling CommitEdit doesn't throw an exception.
		/// This test is to reproduce issue FWNX-472.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void CommitEdit_PopulatedFwMultilingualPropView_ShouldNotThrowException()
		{
			var dataSource = new DummyFwMultilingualPropViewDataSource();
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
