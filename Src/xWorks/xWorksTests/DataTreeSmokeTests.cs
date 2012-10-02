// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Test.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
//	the lives here, rather than in the DetailControlsTests.DLL, because it relies on XCore
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using NUnit.Framework;
using XCore;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL;

namespace SIL.FieldWorks.XWorks
{

	public  abstract class  DataTreeSmokeTests : XWorksAppTestBase
	{
		public DataTreeSmokeTests()
		{
		}

		protected DataTree  DatTree
		{
			get
			{
				RecordEditView rev = (RecordEditView)this.m_window.CurrentContentControl;// .Properties.GetValue("currentContentControl");
				return rev.DatTree;
			}
		}

		protected void StandardViewTest()
		{
			VisitAllNodesTest();
		}

		protected void StandardViewTest(string viewName)
		{
			SetTool(viewName);
			StandardViewTest();
		}

		public void VisitAllNodesTest()
		{
			DataTree tree = DatTree;
			tree.GotoFirstSlice();
			if(tree.CurrentSlice.Expansion == DataTree.TreeItemState.ktisCollapsed)
				tree.CurrentSlice.Expand();
			while(tree.LastSlice != tree.CurrentSlice)
			{
				tree.GotoNextSlice();
				if(tree.CurrentSlice.Expansion == DataTree.TreeItemState.ktisCollapsed)
					tree.CurrentSlice.Expand();
			}
		}

	}

}