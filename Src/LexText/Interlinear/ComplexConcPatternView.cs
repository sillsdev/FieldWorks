using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcPatternView : RootSiteControl
	{
		private ComplexConcControl m_concordanceControl;
		private ComplexConcPatternVc m_vc;

		/// <summary>
		/// We MUST inherit from this, not from just EditingHelper; otherwise, the right event isn't
		/// connected (in an overide of OnEditingHelperCreated) for us to get selection change notifications.
		/// </summary>
		class ComplexConcPatternEditingHelper : RootSiteEditingHelper
		{
			public ComplexConcPatternEditingHelper(FdoCache cache, IEditingCallbacks callbacks)
				: base(cache, callbacks)
			{
			}

			public override bool CanCopy()
			{
				return false;
			}

			public override bool CanCut()
			{
				return false;
			}

			public override bool CanPaste()
			{
				return false;
			}

			/// <summary>
			/// We don't want typing to do anything.  On Linux, it does without this method,
			/// causing a crash immediately.  See FWNX-1337.
			/// </summary>
			protected override void CallOnTyping(string str, Keys modifiers)
			{
			}
		}

		protected override EditingHelper CreateEditingHelper()
		{
			// we can't just use the Editable property to disable copy/cut/paste, because we want
			// the view to be read only, so instead we use a custom EditingHelper
			return new ComplexConcPatternEditingHelper(Cache, this);
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_concordanceControl = null;
			m_vc = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		public void Init(Mediator mediator, IPropertyTable propertyTable, ComplexConcControl concordanceControl)
		{
			CheckDisposed();
			m_concordanceControl = concordanceControl;
			Mediator = mediator;
			m_propertyTable = propertyTable;
			Cache = m_propertyTable.GetValue<FdoCache>("cache");
			m_vc = new ComplexConcPatternVc(m_fdoCache, m_propertyTable);
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				m_rootb.SetRootObject(m_concordanceControl.PatternModel.Root.Hvo, m_vc, ComplexConcPatternVc.kfragPattern, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable));
				m_rootb.Reconstruct();
			}
		}

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_concordanceControl.PatternModel.DataAccess;
			m_rootb.SetRootObject(m_concordanceControl.PatternModel.Root.Hvo, m_vc, ComplexConcPatternVc.kfragPattern, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable));
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Delete.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				m_concordanceControl.RemoveNodes(true);
			}
			else
			{
				e.Handled = true;
				base.OnKeyDown(e);
			}
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Backspace.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Back)
			{
				m_concordanceControl.RemoveNodes(false);
			}
			e.Handled = true;
			base.OnKeyPress(e);
		}

		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			m_concordanceControl.UpdateSelection(prootb, vwselNew);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y,
				new Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
				new Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom),
				false);
			if (m_concordanceControl.DisplayContextMenu(sel))
				return true;

			return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}
	}
}
