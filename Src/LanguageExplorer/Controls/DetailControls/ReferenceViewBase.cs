// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Common.RootSites;

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

		#region Construction

		public ReferenceViewBase()
		{
		}

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
			Cache = cache;		// Set cache on EditingHelper as well if needed.  (See FWR-1426.)
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

		#endregion // Construction

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// if we don't install the selection here, a previous selection may give us
			// spurious results later on when handling the UI this right click brings up;
			// see LT-12154.
			var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
			var tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button != MouseButtons.Left || (ModifierKeys & Keys.Control) != Keys.Control)
			{
				return;
			}
			// Control-click: take the first jump-to-tool command from the right-click menu for this location.
			// Create a selection where we right clicked
			var sel = GetSelectionAtPoint(new Point(e.X, e.Y), false);
			var tsi = new TextSelInfo(sel);
			var hvoTarget = tsi.Hvo(false);
			// May be (for example) dummy ID used for type-ahead object in reference vector list.
			if (hvoTarget == 0 || !Cache.ServiceLocator.IsValidObjectId(hvoTarget))
			{
				return;
			}
			using (var ui = GetCmObjectUiForRightClickMenu(hvoTarget))
			{
				ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				ui.HandleCtrlClick(this);
			}
		}

		private ReferenceBaseUi GetCmObjectUiForRightClickMenu(int hvoTarget)
		{
			return ReferenceBaseUi.MakeLcmModelUiObject(Cache, m_rootObj, m_rootFlid, hvoTarget);
		}

		protected virtual bool HandleRightClickOnObject(int hvo)
		{
			if (hvo == 0)
			{
				return false;
			}
			// We do NOT want a Using here. The temporary colleague created inside HandleRightClick should dispose
			// of the object. (Not working as of the time of writing, but disposing it makes a much more definite
			// problem, because it is gone before the user can choose one of the menu items. (FWR-2798 reopened)
			var ui = GetCmObjectUiForRightClickMenu(hvo);
			if (ui != null)
			{
				ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				return ui.HandleRightClick(this, true, adjustMenu: CmObjectUi.MarkCtrlClickItem);
			}
			return false;
		}
	}
}
