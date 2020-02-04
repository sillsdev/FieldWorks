// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Base class for handling things common to ReferenceView classes.
	/// </summary>
	internal class ReferenceViewBase : RootSiteControl
	{
		protected ICmObject m_rootObj;
		protected int m_rootFlid;
		protected string m_rootFieldName;
		protected string m_displayNameProperty;
		private string m_textStyle;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _rightClickTuple;

		/// <summary>
		/// Get or set the text style name
		/// </summary>
		public string TextStyle
		{
			get
			{
				if (string.IsNullOrEmpty(m_textStyle))
				{
					m_textStyle = "Default Paragraph Characters";
				}
				return m_textStyle;
			}
			set
			{
				m_textStyle = value;
			}
		}

		public void Initialize(ICmObject rootObj, int rootFlid, string rootFieldName, LcmCache cache, string displayNameProperty)
		{
			// We can reinitialize in some cases but should not reuse with a different cache.
			Debug.Assert(cache != null && (m_cache == null || m_cache == cache));
			m_displayNameProperty = displayNameProperty;
			Cache = cache;      // Set cache on EditingHelper as well if needed.  (See FWR-1426.)
			m_rootObj = rootObj;
			m_rootFlid = rootFlid;
			m_rootFieldName = rootFieldName;
			if (RootBox == null)
			{
				MakeRoot();
			}
			else
			{
				SetupRoot();
			}
		}

		/// <summary>
		/// Override this if you override MakeRoot (and if your class is reused...see SliceFactory.Create)
		/// </summary>
		protected virtual void SetupRoot()
		{
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// if we don't install the selection here, a previous selection may give us
			// spurious results later on when handling the UI this right click brings up;
			// see LT-12154.
			var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
			var tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		protected virtual bool HandleRightClickOnObject(int hvo)
		{
			if (hvo == 0)
			{
				return false;
			}
			var dataTree = PropertyTable.GetValue<DataTree>(LanguageExplorerConstants.DataTree);
			if (dataTree != null)
			{
				if (_rightClickTuple != null)
				{
					dataTree.DataTreeSliceContextMenuParameterObject.RightClickPopupMenuFactory.DisposePopupContextMenu(_rightClickTuple);
					_rightClickTuple = null;
				}
				_rightClickTuple = dataTree.DataTreeSliceContextMenuParameterObject.RightClickPopupMenuFactory.GetPopupContextMenu(dataTree.CurrentSlice, ContextMenuName.mnuReferenceChoices);
				if (_rightClickTuple == null)
				{
					// Nobody home (the menu).
					MessageBox.Show($"Popup menu: '{ContextMenuName.mnuReferenceChoices.ToString()}' not found.{Environment.NewLine}{Environment.NewLine}Register a creator method for it in dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.", "Implement missing popup menu", MessageBoxButtons.OK);
					return true;
				}
				if (_rightClickTuple.Item1.Items.Count > 0)
				{
					_rightClickTuple.Item1.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
				}
			}
			else
			{
				// Nobody home (DataTree).
				MessageBox.Show($"Add DataTree to the PropertyTable.", "Implement missing popup menu", MessageBoxButtons.OK);
				return true;
			}
			return true;
		}
	}
}