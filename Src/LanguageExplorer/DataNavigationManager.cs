// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.XWorks;
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
		internal const string First = "First";
		internal const string Previous = "Previous";
		internal const string Next = "Next";
		internal const string Last = "Last";
		private readonly Dictionary<string, Tuple<ToolStripMenuItem, ToolStripButton>> _menuItems;
		private RecordClerk _clerk;

		/// <summary />
		internal DataNavigationManager(Dictionary<string, Tuple<ToolStripMenuItem, ToolStripButton>>  menuItems)
		{
			if (menuItems == null)
			{
				throw new ArgumentNullException(nameof(menuItems));
			}

			_menuItems = menuItems;

			var currentTuple = _menuItems[First];
			currentTuple.Item1.Click += First_Click;
			currentTuple.Item2.Click += First_Click;

			currentTuple = _menuItems[Previous];
			currentTuple.Item1.Click += Previous_Click;
			currentTuple.Item2.Click += Previous_Click;

			currentTuple = _menuItems[Next];
			currentTuple.Item1.Click += Next_Click;
			currentTuple.Item2.Click += Next_Click;

			currentTuple = _menuItems[Last];
			currentTuple.Item1.Click += Last_Click;
			currentTuple.Item2.Click += Last_Click;
		}

		private void First_Click(object sender, EventArgs e)
		{
			MoveToIndex(0);
		}

		private void Previous_Click(object sender, EventArgs e)
		{
			MoveToIndex(_clerk.CurrentIndex - 1);
		}

		private void Next_Click(object sender, EventArgs e)
		{
			MoveToIndex(_clerk.CurrentIndex + 1);
		}

		private void Last_Click(object sender, EventArgs e)
		{
			MoveToIndex(_clerk.ListSize - 1);
		}

		internal RecordClerk Clerk
		{
			set
			{
				if (_clerk != null)
				{
					// Unwire from older clerk
					_clerk.RecordChanged -= Clerk_RecordChanged;
				}
				_clerk = value;
				if (_clerk != null)
				{
					// Wire up to new clerk.
					_clerk.RecordChanged += Clerk_RecordChanged;
				}

				SetEnabledStateForWidgets();
			}
		}

		private void Clerk_RecordChanged(object sender, RecordNavigationEventArgs recordNavigationEventArgs)
		{
			SetEnabledStateForWidgets();
		}

		private void MoveToIndex(int newIndex)
		{
			_clerk.MoveToIndex(newIndex);
			SetEnabledStateForWidgets();
		}

		private void SetEnabledStateForWidgets()
		{
			if (_clerk == null || _clerk.ListSize == 0)
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
				var currentTuple = _menuItems[First];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _clerk.CurrentIndex > 0;
				currentTuple = _menuItems[Previous];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _clerk.CurrentIndex > 0;
				currentTuple = _menuItems[Next];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _clerk.CurrentIndex < _clerk.ListSize - 1;
				currentTuple = _menuItems[Last];
				currentTuple.Item1.Enabled = currentTuple.Item2.Enabled = _clerk.CurrentIndex < _clerk.ListSize - 1;
			}
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
				return; // Only run once.

			if (disposing)
			{
				if (_clerk != null)
				{
					_clerk.RecordChanged -= Clerk_RecordChanged;
				}
				var currentTuple = _menuItems[First];
				currentTuple.Item1.Click -= First_Click;
				currentTuple.Item2.Click -= First_Click;

				currentTuple = _menuItems[Previous];
				currentTuple.Item1.Click -= Previous_Click;
				currentTuple.Item2.Click -= Previous_Click;

				currentTuple = _menuItems[Next];
				currentTuple.Item1.Click -= Next_Click;
				currentTuple.Item2.Click -= Next_Click;

				currentTuple = _menuItems[Last];
				currentTuple.Item1.Click -= Last_Click;
				currentTuple.Item2.Click -= Last_Click;
			}

			base.Dispose(disposing);
		}
	}
}