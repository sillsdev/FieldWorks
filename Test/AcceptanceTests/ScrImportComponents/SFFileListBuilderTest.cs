// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SFFileListBuilderTest.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.ScrImportComponents
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing test overrides for SFFileListBuilder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummySFFileListBuilder : SFFileListBuilder
	{
		#region data members
		/// <summary>
		///
		/// </summary>
		public string[] m_SimulatedAddFileNames;
		/// <summary>
		///
		/// </summary>
		public int m_iSelectedItem = 0;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the current list view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListView FwListView
		{
			get
			{
				CheckDisposed();
				return base.m_currentListView;
			}
		}

		#region overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="filename"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowBadFileMessage(string filename)
		{
			// do not show the message during the test
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Simulate allowing the user to choose the SF scripture files to import.
		/// </summary>
		/// <returns>Array of files to add</returns>
		/// -------------------------------------------------------------------------------
		protected override string[] QueryUserForNewFilenames()
		{
			return m_SimulatedAddFileNames;
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Processes clicking on the Remove button.
		/// </summary>
		/// -------------------------------------------------------------------------------
		public void CallbtnRemove_Click()
		{
			CheckDisposed();

			base.btnRemove_Click(null, null);
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Processes clicking on the Add button.
		/// </summary>
		/// -------------------------------------------------------------------------------
		public void CallbtnAdd_Click()
		{
			CheckDisposed();

			base.btnAdd_Click(null, null);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for SFFileListBuilder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SFFileListBuilderTest : BaseTest
	{
		#region data members
		private DummySFFileListBuilder m_builder;
		private FdoCache m_cache;
		private ScrImportSet m_settings;
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SFFileListBuilderTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SFFileListBuilderTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup called before each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_builder = new DummySFFileListBuilder();
			m_cache = FdoCache.Create("TestLangProj");

			if (!m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.BeginTrans();

			m_cache.BeginUndoTask("Undo SfFileListBuilderTest", "Redo SfFileListBuilderTest");

			m_settings = new ScrImportSet();
			m_cache.LangProject.TranslatedScriptureOA.DefaultImportSettings = m_settings;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up called after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			m_builder = null;
			m_cache.ActionHandlerAccessor.EndOuterUndoTask();
			m_settings = null;

			while (m_cache.Undo())
				;

			if (m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.RollbackTrans();

			m_cache.Dispose();
			m_cache = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AddFiles method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesToList()
		{
			CheckDisposed();
			TempSFFileMaker filemaker = new TempSFFileMaker();
			string[] files = new String[3];
			files[0] = filemaker.CreateFile("GEN", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[1] = filemaker.CreateFile("EXO", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[2] = filemaker.CreateFile("LEV", new String[] {@"\c 1"}, Encoding.UTF8, true);
			m_settings.AddFile(files[0], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[1], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[2], ImportDomain.Main, null, 0);
			m_builder.ImportSettings = m_settings;

			Assert.IsTrue(m_builder.FwListView.Items[0].Selected);
			Assert.AreEqual(3, m_builder.FwListView.Items.Count);
			int c1 = 0;
			int c2 = 0;
			int c3 = 0;
			foreach(ListViewItem lvi in m_builder.FwListView.Items)
			{
				if (lvi.Text == files[0])
				{
					c1++;
				}
				else if (lvi.Text == files[1])
				{
					c2++;
				}
				else if (lvi.Text == files[2])
				{
					c3++;
				}
			}
			Assert.AreEqual(1, c1);
			Assert.AreEqual(1, c2);
			Assert.AreEqual(1, c3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesThroughButton()
		{
			CheckDisposed();
			m_builder.ImportSettings = m_settings;

			TempSFFileMaker filemaker = new TempSFFileMaker();
			string[] files = new String[3];
			files[0] = filemaker.CreateFile("GEN", new String[] {@"\c 1"}, Encoding.ASCII, true);
			files[1] = filemaker.CreateFile("EXO", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[2] = filemaker.CreateFile("LEV", new String[] {@"\c 1"}, Encoding.UTF8, true);
			m_builder.m_SimulatedAddFileNames = files;
			m_builder.CallbtnAdd_Click();
			int c = 0;
			foreach(string s in m_builder.ScriptureFiles)
			{
				if (s == files[0])
					c++;
				else if (s == files[1])
					c++;
				else if (s == files[2])
					c++;
				else
					Assert.Fail("Something extra in the list :(");
			}
			Assert.AreEqual(3, c);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the remove button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveFilesThroughButton()
		{
			CheckDisposed();
			TempSFFileMaker filemaker = new TempSFFileMaker();
			string[] files = new String[3];
			files[0] = filemaker.CreateFile("GEN", new String[] {@"\c 1"}, Encoding.Unicode, true);
			files[1] = filemaker.CreateFile("EXO", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[2] = filemaker.CreateFile("LEV", new String[] {@"\c 1"}, Encoding.BigEndianUnicode, true);
			m_settings.AddFile(files[0], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[1], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[2], ImportDomain.Main, null, 0);
			m_builder.ImportSettings = m_settings;

			// Can't figure out how to make this work without showing the stupid list.
			Form m_ctrlOwner;
			m_ctrlOwner = new Form();
			m_ctrlOwner.Controls.Add(m_builder);
			m_ctrlOwner.Show();

			try
			{
				//remove the last item
				m_builder.m_iSelectedItem = 2;
				m_builder.CallbtnRemove_Click();
				Assert.AreEqual(2, m_builder.FwListView.Items.Count);
				Assert.AreEqual(files[0], m_builder.FwListView.Items[0].Text);
				Assert.AreEqual(files[1], m_builder.FwListView.Items[1].Text);
				Assert.AreEqual(1, m_builder.FwListView.SelectedItems[0].Index);

				//remove the first item
				m_builder.m_iSelectedItem = 0;
				m_builder.CallbtnRemove_Click();
				Assert.AreEqual(1, m_builder.FwListView.Items.Count);
				Assert.AreEqual(files[1], m_builder.FwListView.Items[0].Text);
				Assert.AreEqual(0, m_builder.FwListView.SelectedItems[0].Index);

				//remove the only item left
				m_builder.CallbtnRemove_Click();
				Assert.AreEqual(0, m_builder.FwListView.Items.Count);
				Assert.AreEqual(0, m_builder.FwListView.SelectedItems.Count);
			}
			finally
			{
				m_ctrlOwner.Close();
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that expected text encodings are displayed for added files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckFileTextEncodings()
		{
			CheckDisposed();
			TempSFFileMaker filemaker = new TempSFFileMaker();
			string[] files = new String[3];
			files[0] = filemaker.CreateFile("GEN", new String[] {@"\c 1"}, Encoding.Unicode, true);
			files[1] = filemaker.CreateFile("EXO", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[2] = filemaker.CreateFile("LEV", new String[] {@"\c 1"}, Encoding.BigEndianUnicode, true);
			m_settings.AddFile(files[0], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[1], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[2], ImportDomain.Main, null, 0);
			m_builder.ImportSettings = m_settings;

			int c = 0;
			foreach (ListViewItem lvi in m_builder.FwListView.Items)
			{
				ListViewItem.ListViewSubItem subitem = lvi.SubItems[2];
				if (lvi.Text == files[0])
					Assert.AreEqual("Unicode", subitem.Text);
				else if (lvi.Text == files[1])
					Assert.AreEqual("Unicode (UTF-8)", subitem.Text);
				else if (lvi.Text == files[2])
					Assert.AreEqual("Unicode (Big-Endian)", subitem.Text);
				else
					Assert.Fail("Unexpected file found in list");
				c++;
			}
			Assert.AreEqual(3, c);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add non-existent files to the settings.  This should be allowed because
		/// there may be network files that are not currently available.  We want to remember
		/// the file names because they may be accessible in the future.
		/// Jira task number is TE-510
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddNonexistentFilesToSettings()
		{
			CheckDisposed();
			TempSFFileMaker filemaker = new TempSFFileMaker();
			string[] files = new String[2];
			files[0] = filemaker.CreateFile("GEN", new String[] {@"\c 1"}, Encoding.UTF8, true);
			files[1] = filemaker.CreateFile("EXO", new String[] {@"\c 1"}, Encoding.UTF8, true);
			m_settings.AddFile(files[0], ImportDomain.Main, null, 0);
			m_settings.AddFile(files[1], ImportDomain.Main, null, 0);

			File.Delete(files[1]);

			m_builder.ImportSettings = m_settings;
			Assert.AreEqual(2, m_builder.FwListView.Items.Count);
		}
	}
}
