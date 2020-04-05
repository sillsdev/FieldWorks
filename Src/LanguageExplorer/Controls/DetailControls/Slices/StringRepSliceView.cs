// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Phonology;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	internal sealed class StringRepSliceView : RootSiteControl, INotifyControlInCurrentSlice
	{
		private IPhEnvironment m_env;
		private int m_hvoObj;
		private StringRepSliceVc m_vc;
		private PhonEnvRecognizer m_validator;
		private SliceRightClickPopupMenuFactory _rightClickPopupMenuFactory;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _mnuEnvChoicesTuple;

		internal StringRepSliceView(int hvo)
		{
			m_hvoObj = hvo;
		}

		internal void ResetValidator()
		{
			m_validator = new PhonEnvRecognizer(m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(), m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
			}

			m_env = null;
			m_vc = null;
			m_validator = null;
		}

		#region INotifyControlInCurrentSlice implementation

		/// <summary>
		/// Adjust controls based on whether the slice is the current slice.
		/// </summary>
		public bool SliceIsCurrent
		{
			set
			{
				// SliceIsCurrent may be called in the process of deleting the object after the object
				// has been partially cleared out and thus would certainly fail the constraint
				// check, then try to instantiate an error annotation which wouldn't have an
				// owner, causing bad things to happen.
				if (DesignMode || RootBox == null || !m_env.IsValidObject)
				{
					return;
				}
				if (!value)
				{
					DoValidation(true); // JohnT: do we really always want a Refresh? Trying to preserve the previous behavior...
				}
			}
		}

		private Slice MySlice => (Slice)Parent;

		#endregion INotifyControlInCurrentSlice implementation

		private void DoValidation(bool refresh)
		{
			var frm = FindForm();
			// frm may be null, if the record has been switched
			WaitCursor wc = null;
			try
			{
				if (frm != null)
				{
					wc = new WaitCursor(frm);
				}
				m_env.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, true, out _, /* adjust the squiggly line */ true);
				// This will make the record list update to the new value.
				if (refresh)
				{
					Publisher.Publish(new PublisherParameterObject("Refresh"));
				}
			}
			finally
			{
				wc?.Dispose();
			}
		}

		/// <summary>
		/// This method seems to get called when we are switching to another tool (or area, or slice) AND when the
		/// program is shutting down. This makes it a good point to check constraints, since in some of these
		/// cases, SliceIsCurrent may not get set false.
		/// </summary>
		protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
		{
			base.OnValidating(e);
			// Only necessary to ensure that validation is done when window is going away. We don't need a Refresh then!
			// Also, in some cases (LT-15730) we come back through here on Undo when we have a deleted object.
			// Don't do validation then.
			if (m_env.IsValidObject)
			{
				DoValidation(false);
			}
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			// A crude way of making sure the property we want is loaded into the cache.
			m_env = m_cache.ServiceLocator.GetInstance<IPhEnvironmentRepository>().GetObject(m_hvoObj);
			m_vc = new StringRepSliceVc();
			base.MakeRoot();
			// And maybe this too, at least by default?
			RootBox.DataAccess = m_cache.MainCacheAccessor;
			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			RootBox.SetRootObject(m_hvoObj, m_vc, StringRepSliceVc.Flid, null);
		}

		internal bool CanShowEnvironmentError()
		{
			var text = m_env.StringRepresentation.Text;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			return (!m_validator.Recognize(text));
		}

		internal void ShowEnvironmentError()
		{
			var text = m_env.StringRepresentation.Text;
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (!m_validator.Recognize(text))
			{
				PhonEnvRecognizer.CreateErrorMessageFromXml(text, m_validator.ErrorMessage, out _, out var sMsg);
				MessageBox.Show(sMsg, LanguageExplorerControls.ksErrorInEnvironment, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		internal bool CanInsertSlash
		{
			get
			{
				var s = m_env.StringRepresentation.Text;
				return string.IsNullOrEmpty(s) || s.IndexOf('/') < 0;
			}
		}

		private int GetSelectionEndPoint(bool fEnd)
		{
			var vwsel = RootBox.Selection;
			if (vwsel == null)
			{
				return -1;
			}
			vwsel.TextSelInfo(fEnd, out _, out var ichEnd, out _, out var hvo, out var flid, out _);
			Debug.Assert(hvo == m_env.Hvo);
			Debug.Assert(flid == PhEnvironmentTags.kflidStringRepresentation);
			return ichEnd;
		}

		internal bool CanInsertEnvBar
		{
			get
			{
				var text = m_env.StringRepresentation.Text;
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				var ichSlash = text.IndexOf('/');
				if (ichSlash < 0)
				{
					return false;
				}
				var ichEnd = GetSelectionEndPoint(true);
				if (ichEnd < 0)
				{
					return false;
				}
				var ichAnchor = GetSelectionEndPoint(false);
				return ichAnchor >= 0 && (ichEnd > ichSlash && ichAnchor > ichSlash && text.IndexOf('_') < 0);
			}
		}

		internal bool CanInsertItem
		{
			get
			{
				var text = m_env.StringRepresentation.Text;
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				var ichEnd = GetSelectionEndPoint(true);
				var ichAnchor = GetSelectionEndPoint(false);
				return PhonEnvRecognizer.CanInsertItem(text, ichEnd, ichAnchor);
			}
		}

		internal bool CanInsertHashMark
		{
			get
			{
				var text = m_env.StringRepresentation.Text;
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				var ichEnd = GetSelectionEndPoint(true);
				var ichAnchor = GetSelectionEndPoint(false);
				return PhonEnvRecognizer.CanInsertHashMark(text, ichEnd, ichAnchor);
			}
		}

		#region Handle right click menu
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (_mnuEnvChoicesTuple != null)
			{
				_rightClickPopupMenuFactory.DisposePopupContextMenu(_mnuEnvChoicesTuple);
				_mnuEnvChoicesTuple = null;
			}
			if (m_env == null)
			{
				return false;
			}

			_mnuEnvChoicesTuple = _rightClickPopupMenuFactory.GetPopupContextMenu(MySlice, ContextMenuName.mnuEnvChoices);
			_mnuEnvChoicesTuple?.Item1.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
			return true;
		}
		#endregion

		internal void SetRightClickPopupMenuFactory(SliceRightClickPopupMenuFactory rightClickPopupMenuFactory)
		{
			_rightClickPopupMenuFactory = rightClickPopupMenuFactory;
		}
	}
}