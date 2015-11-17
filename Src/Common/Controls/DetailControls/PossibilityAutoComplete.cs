// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityAutoComplete : FwDisposableBase
	{
		private readonly FdoCache m_cache;
		private readonly Control m_control;
		private readonly string m_displayNameProperty;
		private readonly string m_displayWs;
		private readonly Mediator m_mediator;

		private readonly ComboListBox m_listBox;
		private readonly StringSearcher<ICmPossibility> m_searcher;
		private readonly List<ICmPossibility> m_possibilities;

		private int m_curPossIndex;

		private bool m_changingSelection;

		public event EventHandler PossibilitySelected;

		public PossibilityAutoComplete(FdoCache cache, Mediator mediator, ICmPossibilityList list, Control control,
			string displayNameProperty, string displayWs)
		{
			m_cache = cache;
			m_mediator = mediator;
			m_control = control;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;

			m_listBox = new ComboListBox {DropDownStyle = ComboBoxStyle.DropDownList, ActivateOnShow = false};
			m_listBox.SelectedIndexChanged += HandleSelectedIndexChanged;
			m_listBox.SameItemSelected += HandleSameItemSelected;
			m_listBox.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_listBox.WritingSystemFactory = cache.WritingSystemFactory;
			m_searcher = new StringSearcher<ICmPossibility>(SearchType.Prefix, cache.ServiceLocator.WritingSystemManager);
			m_possibilities = new List<ICmPossibility>();
			var stack = new Stack<ICmPossibility>(list.PossibilitiesOS);
			while (stack.Count > 0)
			{
				ICmPossibility poss = stack.Pop();
				m_possibilities.Add(poss);
				foreach (ICmPossibility child in poss.SubPossibilitiesOS)
					stack.Push(child);
			}

			m_control.KeyDown += HandleKeyDown;
			m_control.KeyPress += HandleKeyPress;
		}

		public IEnumerable<ICmPossibility> Possibilities
		{
			get { return m_listBox.Items.Cast<CmPossibilityLabel>().Select(label => label.Possibility); }
		}

		public ICmPossibility SelectedPossibility
		{
			get { return ((CmPossibilityLabel) m_listBox.SelectedItem).Possibility; }
		}

		protected override void DisposeManagedResources()
		{
			m_listBox.SelectedIndexChanged -= HandleSelectedIndexChanged;
			m_listBox.SameItemSelected -= HandleSameItemSelected;
			m_listBox.Dispose();

			base.DisposeManagedResources();
		}

		protected virtual void OnItemSelected(EventArgs e)
		{
			m_listBox.HideForm();
			if (PossibilitySelected != null)
				PossibilitySelected(this, e);
		}

		private void HandleSelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_changingSelection || !m_listBox.Visible)
				return;

			OnItemSelected(new EventArgs());
		}

		private void HandleSameItemSelected(object sender, EventArgs eventArgs)
		{
			if (m_changingSelection || !m_listBox.Visible)
				return;

			OnItemSelected(new EventArgs());
		}

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			if (!m_listBox.Visible)
				return;

			switch (e.KeyCode)
			{
				case Keys.Up:
					try
					{
						m_changingSelection = true;
						m_listBox.SelectedIndex = Math.Max(m_listBox.SelectedIndex - 1, 0);
						m_listBox.ScrollHighlightIntoView();
					}
					finally
					{
						m_changingSelection = false;
					}
					e.Handled = true;
					break;

				case Keys.Down:
					try
					{
						m_changingSelection = true;
						m_listBox.SelectedIndex = Math.Min(m_listBox.SelectedIndex + 1, m_listBox.Items.Count - 1);
						m_listBox.ScrollHighlightIntoView();
					}
					finally
					{
						m_changingSelection = false;
					}
					e.Handled = true;
					break;
			}
		}

		private void HandleKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char) Keys.Enter && m_listBox.Visible)
			{
				OnItemSelected(new EventArgs());
				e.Handled = true;
			}
		}

		public void Hide()
		{
			m_listBox.HideForm();
		}

		public void Update(ITsString tss)
		{
			m_mediator.IdleQueue.Add(IdleQueuePriority.Low, PerformUpdate, tss);
		}

		private bool PerformUpdate(object param)
		{
			CreateSearcher();
			try
			{
				m_changingSelection = true;
				try
				{
					m_listBox.BeginUpdate();
					m_listBox.Items.Clear();
					// TODO: sort the results
					foreach (ICmPossibility poss in m_searcher.Search(0, (ITsString) param))
					{
						// Every so often see whether the user has typed something that makes our search irrelevant.
						if (ShouldAbort())
							return false;

						m_listBox.Items.Add(ObjectLabel.CreateObjectLabel(m_cache, poss, m_displayNameProperty, m_displayWs));
					}
				}
				finally
				{
					m_listBox.EndUpdate();
				}

				if (m_listBox.Items.Count > 0)
				{
					m_listBox.AdjustSize(500, 400);
					m_listBox.SelectedIndex = 0;
					m_listBox.Launch(m_control.RectangleToScreen(m_control.Bounds), Screen.GetWorkingArea(m_control));
				}
				else
				{
					m_listBox.HideForm();
				}
			}
			finally
			{
				m_changingSelection = false;
			}
			return true;
		}

		private void CreateSearcher()
		{
			int control = 0;
			for (; m_curPossIndex < m_possibilities.Count; m_curPossIndex++)
			{
				// Every so often see whether the user has typed something that makes our search irrelevant.
				if (control++ % 50 == 0 && ShouldAbort())
					return;

				ICmPossibility poss = m_possibilities[m_curPossIndex];
				ITsString name = null;
				foreach (int ws in WritingSystemServices.GetWritingSystemIdsFromLabel(m_cache, m_displayWs, m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem,
					poss.Hvo, CmPossibilityTags.kflidName, null))
				{
					ITsString tss = poss.Name.StringOrNull(ws);
					if (tss != null && tss.Length > 0)
					{
						name = tss;
						m_searcher.Add(poss, 0, tss);
						break;
					}
				}

				foreach (int ws in WritingSystemServices.GetWritingSystemIdsFromLabel(m_cache, m_displayWs, m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem,
					poss.Hvo, CmPossibilityTags.kflidAbbreviation, null))
				{
					ITsString tss = poss.Abbreviation.StringOrNull(ws);
					if (tss != null && tss.Length > 0)
					{
						m_searcher.Add(poss, 0, tss);
						if (name != null)
						{
							var tisb = TsIncStrBldrClass.Create();
							tisb.AppendTsString(tss);
							tisb.AppendTsString(m_cache.TsStrFactory.MakeString(" - ", m_cache.DefaultUserWs));
							tisb.AppendTsString(name);
							m_searcher.Add(poss, 0, tisb.GetString());
						}
						break;
					}
				}
			}
		}

		/// <summary>
		/// Abort resetting if the user types anything, anywhere.
		/// Also sets the flag (if it returns true) to indicate the search WAS aborted.
		/// </summary>
		/// <returns></returns>
		private static bool ShouldAbort()
		{
#if !__MonoCS__
			var msg = new Win32.MSG();
			return Win32.PeekMessage(ref msg, IntPtr.Zero, (uint)Win32.WinMsgs.WM_KEYDOWN, (uint)Win32.WinMsgs.WM_KEYDOWN,
				(uint)Win32.PeekFlags.PM_NOREMOVE);
#else
			// ShouldAbort seems to be used for optimization purposes so returing false
			// just loses the optimization.
			return false;
#endif
		}
	}
}
