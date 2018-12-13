// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Controls;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This is a pane which shows the title and Description of the current record.
	/// </summary>
	internal class TitleContentsPane : RootSiteControl, IInterlinearTabControl, IStyleSheet
	{
		private int m_hvoRoot; // The Text.
		private TitleContentsVc m_vc;

		public TitleContentsPane()
		{
			// Note: the following line may be redundant. I (AlistairI) observed BackColor
			// had been overwritten by the time AdjustHeight() was first called:
			BackColor = Color.FromKnownColor(KnownColor.ControlLight);
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
		}

		#endregion IDisposable override

		#region implemention of IChangeRootObject

		public void SetRoot(int hvo)
		{
			if (hvo != 0)
			{
				var stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvo);
				m_hvoRoot = ScriptureServices.ScriptureIsResponsibleFor(stText) ? hvo : stText.Owner.Hvo;
				SetupVc();
			}
			else
			{
				m_hvoRoot = 0;
				ReadOnlyView = true;
				if (m_vc != null)
				{
					m_vc.IsScripture = false;
					m_vc.Editable = false;
				}
			}
			ChangeOrMakeRoot(m_hvoRoot, m_vc, TitleContentsVc.kfragRoot, m_styleSheet);
		}

		private void SetupVc()
		{
			if (m_vc == null || m_hvoRoot == 0)
			{
				return;
			}
			Debug.Assert(m_hvoRoot != 0, "m_hvoRoot should be set before using SetupVc().");
			var co = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoRoot);
			m_vc.IsScripture = ScriptureServices.ScriptureIsResponsibleFor(co as IStText);
			// don't allow editing scripture titles.
			m_vc.Editable = !m_vc.IsScripture;
			ReadOnlyView = !m_vc.Editable;
		}

		#endregion

		#region Overrides of RootSite
		/// <summary>
		/// Make the root box.
		/// </summary>
		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode /*|| m_hvoRoot == 0*/)
			{
				return;
			}

			base.MakeRoot();

			m_vc = new TitleContentsVc(m_cache);
			SetupVc();

			RootBox.DataAccess = m_cache.MainCacheAccessor;
			RootBox.SetRootObject(m_hvoRoot, m_vc, TitleContentsVc.kfragRoot, m_styleSheet);

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		public override void RootBoxSizeChanged(IVwRootBox prootb)
		{
			base.RootBoxSizeChanged(prootb);
			AdjustHeight();
		}

		#endregion

		public override bool RefreshDisplay()
		{
			if (m_cache != null)
			{
				m_vc?.SetupWritingSystemsForTitle(m_cache);
			}
			return base.RefreshDisplay();
		}

		/// <summary>
		/// Adjust your height up to some reasonable limit to accommodate the entire title and contents.
		/// </summary>
		public bool AdjustHeight()
		{
			if (RootBox == null)
			{
				return false; // nothing useful we can do.
			}
			// Ideally we want to be about 5 pixels bigger than the root. This suppresses the scroll bar
			// and makes everything neat. (Anything smaller leaves us with a scroll bar.)
			var desiredHeight = RootBox.Height + 8;
			// But we're not the main event. Let's not use more than half the window.
			if (Parent != null)
			{
				desiredHeight = Math.Min(desiredHeight, Parent.Height / 2);
			}
			// On the other hand, we'd better have SOME space.
			desiredHeight = Math.Max(5, desiredHeight);
			// But not MORE than the parent.
			if (Parent != null)
			{
				desiredHeight = Math.Min(desiredHeight, Parent.Height);
			}

			if (Height == desiredHeight)
			{
				return false;
			}
			Height = desiredHeight;
			return true;
		}

		protected override void EnsureDefaultSelection()
		{
			// if we have an editable title, try putting cursor in editable text location.
			if (!ReadOnlyView)
			{
				EnsureDefaultSelection(true);
			}
			else
			{
				base.EnsureDefaultSelection();
			}
		}
	}
}