// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class StringSliceView : RootSiteControl, INotifyControlInCurrentSlice
	{
		ICmObject m_obj;
		readonly int m_hvoObj;
		readonly int m_flid;
		readonly int m_ws = -1; // -1 signifies not a multilingual property
		IVwViewConstructor m_vc;

		public StringSliceView(int hvo, int flid, int ws)
		{
			m_hvoObj = hvo;
			m_flid = flid;
			m_ws = ws;
			DoSpellCheck = true;
		}

		bool m_fShowWsLabel;
		/// <summary>
		/// Set the flag to display writing system labels even for monolingual strings.
		/// </summary>
		public bool ShowWsLabel
		{
			set
			{
				CheckDisposed();
				m_fShowWsLabel = value;
				if (m_vc is StringSliceVc)
				{
					((StringSliceVc)m_vc).ShowWsLabel = value;
				}
			}
		}

		/// <summary>
		/// Set the default writing system for this string.
		/// </summary>
		public int DefaultWs
		{
			set
			{
				CheckDisposed();
				if (m_vc is StringSliceVc)
				{
					((StringSliceVc)m_vc).DefaultWs = value;
				}
			}
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
			//Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				(m_vc as IDisposable)?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_obj = null;
			m_vc = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Make a selection at the specified character offset.
		/// </summary>
		/// <param name="ich"></param>
		public void SelectAt(int ich)
		{
			CheckDisposed();
			try
			{
				RootBox.MakeTextSelection(0, 0, null, m_flid, 0, ich, ich, 0, true, -1, null, true);
			}
			catch
			{
			}
		}

		#region INotifyControlInCurrentSlice implementation

		/// <summary>
		/// Adjust controls based on whether the slice is the current slice.
		/// </summary>
		public bool SliceIsCurrent
		{
			set
			{
				CheckDisposed();
				if (!value)
				{
					DoValidation();
				}
			}
		}

		private void DoValidation()
		{
			// This may be called in the process of deleting the object after the object
			// has been partially cleared out and thus would certainly fail the constraint
			// check, then try to instantiate an error annotation which wouldn't have an
			// owner, causing bad things to happen.
			if (m_obj == null || !m_obj.IsValidObject)
			{
				return;
			}
			ConstraintFailure failure;
			if (m_obj is IPhEnvironment)
			{
				((IPhEnvironment)m_obj).CheckConstraints(m_flid, true, out failure, /* adjust squiggly line */ true);
			}
			else
				m_obj.CheckConstraints(m_flid, true, out failure);
		}

		/// <summary>
		/// This method seems to get called when we are switching to another tool (or area, or slice) AND when the
		/// program is shutting down. This makes it a good point to check constraints, since in some of these
		/// cases, SliceIsCurrent may not get set false.
		/// </summary>
		protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
		{
			base.OnValidating(e);
			DoValidation();
		}

		#endregion INotifyControlInCurrentSlice implementation

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (m_obj.IsValidObject)
			{
				base.OnKeyPress(e);
			}
			else
			{
				e.Handled = true;
			}
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
				return;

			// A crude way of making sure the property we want is loaded into the cache.
			m_obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoObj);

			var type = (CellarPropertyType)m_cache.DomainDataByFlid.MetaDataCache.GetFieldType(m_flid);
			switch (type)
			{
				case CellarPropertyType.Unicode:
					m_vc = new UnicodeStringSliceVc(m_flid, m_ws, m_cache);
					break;
				case CellarPropertyType.String:
					// Even if we were given a writing system, we must not use it if not a multistring,
					// otherwise the VC crashes when it tries to read the property as multilingual.
					m_vc = new StringSliceVc(m_flid, m_cache, Publisher);
					((StringSliceVc)m_vc).ShowWsLabel = m_fShowWsLabel;
					break;
				default:
					m_vc = new StringSliceVc(m_flid, m_ws, m_cache, Publisher);
					((StringSliceVc)m_vc).ShowWsLabel = m_fShowWsLabel;
					break;
			}

			base.MakeRoot();

			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
		}

		static bool s_fProcessingSelectionChanged;
		/// <summary>
		/// Try to keep the selection from including any of the characters in a writing system label.
		/// Also update the writing system label if needed.
		/// </summary>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(prootb, vwselNew);

			// 1) We don't want to recurse into here.
			// 2) If the selection is invalid we can't use it.
			if (s_fProcessingSelectionChanged || !vwselNew.IsValid)
				return;
			try
			{
				s_fProcessingSelectionChanged = true;

				// If the selection is entirely formattable ("IsSelectionInOneFormattableProp"), we don't need to do
				// the following selection truncation.
				var hlpr = SelectionHelper.Create(vwselNew, this);
				if (!EditingHelper.IsSelectionInOneFormattableProp())
				{
					var fRange = hlpr.IsRange;
					var fChangeRange = false;
					if (fRange)
					{
						var fAnchorEditable = vwselNew.IsEditable;
						hlpr.GetIch(SelectionHelper.SelLimitType.Anchor);
						var tagAnchor = hlpr.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
						hlpr.GetIch(SelectionHelper.SelLimitType.End);
						var tagEnd = hlpr.GetTextPropId(SelectionHelper.SelLimitType.End);
						var fEndBeforeAnchor = vwselNew.EndBeforeAnchor;
						if (fEndBeforeAnchor)
						{
							if (fAnchorEditable && tagAnchor > 0 && tagEnd < 0)
							{
								hlpr.SetTextPropId(SelectionHelper.SelLimitType.End, tagAnchor);
								hlpr.SetIch(SelectionHelper.SelLimitType.End, 0);
								fChangeRange = true;
							}
						}
						else
						{
							if (!fAnchorEditable && tagAnchor < 0 && tagEnd > 0)
							{
								hlpr.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tagEnd);
								hlpr.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
								fChangeRange = true;
							}
						}
					}
					if (fChangeRange)
						hlpr.SetSelection(true);
				}

				if (!m_fShowWsLabel)
				{
					return;
				}
				// Might not be, especially when messing with the selection during Undoing the creation of a record.
				if (!Cache.ServiceLocator.IsValidObjectId(m_hvoObj))
				{
					return;
				}
				var tss = m_rootb.DataAccess.get_StringProp(m_hvoObj, m_flid);
				var ttp = tss.get_Properties(0);
				int var;
				var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ws != 0 && m_vc is StringSliceVc && ws != ((StringSliceVc)m_vc).WsLabel)
				{
					m_rootb.Reconstruct();
					hlpr.SetSelection(true);
				}
			}
			finally
			{
				s_fProcessingSelectionChanged = false;
			}
		}
	}
}