using System;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// SummarySlice is like ViewSlice, except that the context menu icon appears on the right,
	/// along with additional hot links derived from the context menu if there is room.
	/// </summary>
	public class SummarySlice : ViewSlice
	{
		RootSite m_view;
		int m_lastWidth;
		SummaryCommandControl m_commandControl;
		bool m_fActive;
		bool m_fLiteralString = false;

		/// <summary>
		/// Construct one, using the "part ref" element (caller) that
		/// invoked the "slice" node that specified this editor.
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="node"></param>
		public SummarySlice(ICmObject obj, XmlNode caller, XmlNode node, StringTable stringTbl)
			: base()
		{
			string paramType = XmlUtils.GetOptionalAttributeValue(node.ParentNode, "paramType");
			if (paramType == "LiteralString")
			{
				// Instead of the parameter being a layout name, it is literal text which will be
				// the whole contents of the slice, with standard properties.
				string text = XmlUtils.GetManditoryAttributeValue(caller, "label");
				if (stringTbl != null)
					text = stringTbl.LocalizeAttributeValue(text);
				m_view = new LiteralLabelView(text, this);
				m_fLiteralString = true;
			}
			else
			{
				string layout = XmlUtils.GetOptionalAttributeValue(caller, "param");
				if (layout == null)
					layout = XmlUtils.GetManditoryAttributeValue(node, "layout");
				m_view = new SummaryXmlView(obj.Hvo, layout, stringTbl, this);
			}
			UserControl mainControl = new UserControl();
			m_view.Dock = DockStyle.Left;
			m_view.LayoutSizeChanged += new EventHandler(m_view_LayoutSizeChanged);
			mainControl.Height = m_view.Height;
			Control = mainControl;

			m_commandControl = new SummaryCommandControl(this);
			m_commandControl.Dock = DockStyle.Fill;
			m_commandControl.Visible = XmlUtils.GetOptionalBooleanAttributeValue(caller, "commandVisible", false);
			mainControl.Controls.Add(m_commandControl);
			mainControl.Dock = DockStyle.Fill;
			mainControl.Controls.Add(m_view);
		}

		/// <summary>
		/// This is sent when something internal changes the size of the view. One example is a change
		/// in the definition of our stylesheet. This may affect our layout.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_view_LayoutSizeChanged(object sender, EventArgs e)
		{
			m_lastWidth = 0; // force AdjustMainViewWidth to really do something
			AdjustMainViewWidth();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_view = null; // Gets disposed elsewhere, since it is in the Controls collection of another active widget.
			m_commandControl = null; // Gets disposed elsewhere, since it is in the Controls collection of another active widget.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Indicates whether this is an active summary, which controls whether the command
		/// control is visible.
		///
		/// Note: it is tempting to use m_commandControl.Visible as the value of Active.
		/// Don't do it: setting m_commandControl.Visible to true will not necessarily
		/// make it true, if some parent is not visible.
		/// </summary>
		public override bool Active
		{
			get
			{
				CheckDisposed();
				return m_fActive;
			}
			set
			{
				CheckDisposed();
				if (m_fActive == value)
					return;
				m_fActive = value;
				if (TreeNode != null)
					TreeNode.Invalidate();
				if (m_commandControl == null)
				{
					// m_commandControl should be null only in the early part of the constructor
					// and the later part of Dispose. But it's possible to be disposing an
					// active slice, in which case, this may get set during Dispose. It should only
					// be set to false at that point, however.
					Debug.Assert(value == false);
					return;
				}
				m_commandControl.Visible = value;
			}
		}

		/// <summary>
		/// Become active if you are a parent of the specified child; otherwise become inactive.
		/// </summary>
		/// <param name="child"></param>
		public override void BecomeActiveIfParent(Slice child)
		{
			CheckDisposed();
			if (Key == null || child.Key == null || Key.Length > child.Key.Length)
			{
				Active = false;
				return;
			}
			for (int i = 0; i < Key.Length; i++)
			{
				if (Key[i] != child.Key[i])
				{
					// Integers masked as objects don't compare as expected!  See LT-9963.
					if (Key[i].GetType().FullName == "System.Int32" &&
						child.Key[i].GetType().FullName == "System.Int32")
					{
						if ((int)Key[i] == (int)child.Key[i])
							continue;
					}
					Active = false;
					return;
				}
			}
			Active = true;
		}

		/// <summary>
		/// Gets and sets the label used to identify the item in the tree diagram.
		/// </summary>
		public override string Label
		{
			get
			{
				CheckDisposed();
				return m_strLabel;
			}
			set
			{
				CheckDisposed();
				// For LiteralString Summary slices we don't want to set the label since
				// it's already been set in m_view and setting it here would double labels.
				if (m_fLiteralString == false)
					m_strLabel = value;
			}
		}

		/// <summary>
		/// This class's root site is NOT its control.
		/// </summary>
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return m_view;
			}
		}

		/// <summary>
		/// We display the context menu icon on the right in a summary slice whenever we show
		/// the other hot links on the right.
		/// </summary>
		/// <returns></returns>
		public override bool ShowContextMenuIconInTreeNode()
		{
			CheckDisposed();
			return Active;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			AdjustMainViewWidth();
			base.OnSizeChanged(e);
		}

		/// <summary>
		/// Adjust the width of the main view, giving it as much of the total available
		/// width as it wants to use.
		/// </summary>
		internal void AdjustMainViewWidth()
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
				return;
			IVwRootBox rootb = m_view.RootBox;
			if (rootb != null && m_lastWidth != this.Width)
			{
				m_lastWidth = this.Width; // only set this if we actually adjust the layout.
				m_view.Parent.SuspendLayout();
				m_view.Width = this.Width;
				m_view.PerformLayout();
				// Some layouts don't work with adding only 4 to the root box width, so we'll
				// add a little more.  See the later comments on LT-4821.
				m_view.Width = Math.Min(this.Width, rootb.Width + 20);
				this.ResumeLayout();
				m_view.Parent.ResumeLayout();
			}
		}

	}

	internal class LiteralLabelView : RootSiteControl
	{
		string m_text;
		LiteralLabelVc m_vc;
		SummarySlice m_slice;

		public LiteralLabelView(string text, SummarySlice slice)
		{
			m_text = text;
			m_slice = slice;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
			m_text = null;
			m_slice = null;
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			IVwRootBox rootb = VwRootBoxClass.Create();
			rootb.SetSite(this);
			m_vc = new LiteralLabelVc(m_fdoCache.MakeUserTss(m_text));

			rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			// Since the VC just displays a literal, both the root HVO and the root frag are arbitrary.
			rootb.SetRootObject(1, m_vc, 2, null);
			m_rootb = rootb;
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				m_slice.HandleMouseDown(new Point(e.X, e.Y));
			else
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			//base.OnMouseDown (e);
		}

		/// <summary>
		/// Summary slices don't need cursors. The blue context menu icon is sufficient.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			// don't set an IP.
		}
	}

	internal class LiteralLabelVc : VwBaseVc
	{
		ITsString m_text;

		public LiteralLabelVc(ITsString text)
		{
			m_text = text;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			if (m_text != null)
			{
				Marshal.ReleaseComObject(m_text);
				m_text = null;
			}

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();
			vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily,
				StStyle.DefaultHeadingFont);
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.ControlDarkDark)));
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.AddString(m_text);
		}
	}

	internal class SummaryXmlView : XmlView
	{
		SummarySlice m_slice;

		public SummaryXmlView(int hvo, string label, StringTable stringTbl, SummarySlice slice) : base( hvo, label, stringTbl, false)
		{
			m_slice = slice;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_slice = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				m_slice.HandleMouseDown(new Point(e.X,e.Y));
			else
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			//base.OnMouseDown (e);
		}
		public override void MakeRoot()
		{
			base.MakeRoot();
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Summary slices don't need cursors. The blue context menu icon is sufficient.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			// don't set an IP.
		}

	}
}
