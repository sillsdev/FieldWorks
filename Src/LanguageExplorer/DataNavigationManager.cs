// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.ObjectModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Manager for the four navigation buttons.
	/// </summary>
	/// <remarks>
	/// The idea is that individual tools that care about such record navigation
	/// can manage the enabled state of the four navigation buttons.
	/// </remarks>
	internal sealed class DataNavigationManager : DisposableBase
	{
		private readonly Dictionary<Navigation, Tuple<ToolStripMenuItem, ToolStripButton>> _menuItems;
		private IRecordList _recordList;

		/// <summary />
		internal DataNavigationManager(Dictionary<Navigation, Tuple<ToolStripMenuItem, ToolStripButton>>  menuItems)
		{
			if (menuItems == null)
			{
				throw new ArgumentNullException(nameof(menuItems));
			}

			_menuItems = menuItems;

			var currentTuple = _menuItems[Navigation.First];
			currentTuple.Item1.Click += First_Click;
			currentTuple.Item2.Click += First_Click;

			currentTuple = _menuItems[Navigation.Previous];
			currentTuple.Item1.Click += Previous_Click;
			currentTuple.Item2.Click += Previous_Click;

			currentTuple = _menuItems[Navigation.Next];
			currentTuple.Item1.Click += Next_Click;
			currentTuple.Item2.Click += Next_Click;

			currentTuple = _menuItems[Navigation.Last];
			currentTuple.Item1.Click += Last_Click;
			currentTuple.Item2.Click += Last_Click;
		}

		private void First_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.First);
		}

		private void Previous_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Previous);
		}

		private void Next_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Next);
		}

		private void Last_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Last);
		}

		internal IRecordList RecordList
		{
			set
			{
				if (_recordList != null)
				{
					// Unwire from older record list
					_recordList.RecordChanged -= RecordListRecordChanged;
				}
				_recordList = value;
				if (_recordList != null)
				{
					// Wire up to new record list.
					_recordList.RecordChanged += RecordListRecordChanged;
				}

				SetEnabledStateForWidgets();
			}
		}

		private void RecordListRecordChanged(object sender, RecordNavigationEventArgs recordNavigationEventArgs)
		{
			SetEnabledStateForWidgets();
		}

		private void MoveToIndex(Navigation navigateTo)
		{
			_recordList.MoveToIndex(navigateTo);
			SetEnabledStateForWidgets();
		}

		internal void SetEnabledStateForWidgets()
		{
			if (_recordList == null || _recordList.ListSize == 0)
			{
				// Disable menu items.
				foreach (var tuple in _menuItems.Values)
				{
					tuple.Item1.Enabled = false;
					tuple.Item2.Enabled = false;
				}
			}
			else
			{
				var currentTuple = _menuItems[Navigation.First];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _recordList.CanMoveTo(Navigation.First);
				currentTuple = _menuItems[Navigation.Previous];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _recordList.CanMoveTo(Navigation.Previous);
				currentTuple = _menuItems[Navigation.Next];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _recordList.CanMoveTo(Navigation.Next);
				currentTuple = _menuItems[Navigation.Last];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _recordList.CanMoveTo(Navigation.Last);
			}
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
				return; // Only run once.

			if (disposing)
			{
				if (_recordList != null)
				{
					_recordList.RecordChanged -= RecordListRecordChanged;
				}
				var currentTuple = _menuItems[Navigation.First];
				currentTuple.Item1.Click -= First_Click;
				currentTuple.Item2.Click -= First_Click;

				currentTuple = _menuItems[Navigation.Previous];
				currentTuple.Item1.Click -= Previous_Click;
				currentTuple.Item2.Click -= Previous_Click;

				currentTuple = _menuItems[Navigation.Next];
				currentTuple.Item1.Click -= Next_Click;
				currentTuple.Item2.Click -= Next_Click;

				currentTuple = _menuItems[Navigation.Last];
				currentTuple.Item1.Click -= Last_Click;
				currentTuple.Item2.Click -= Last_Click;
			}

			base.Dispose(disposing);
		}
	}
}