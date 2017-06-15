// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer
{
	/// <summary>
	/// Manager for the four navigation buttons.
	/// </summary>
	/// <remarks>
	/// The idea is that individual tools that care about such record navigation
	/// can manage the enabled state of the four navigation buttons.
	/// </remarks>
	internal sealed class DataNavigationManager : IDisposable
	{
		internal const string First = "First";
		internal const string Previous = "Previous";
		internal const string Next = "Next";
		internal const string Last = "Last";
		private readonly ISubscriber _subscriber;
		private readonly Dictionary<string, Tuple<ToolStripMenuItem, ToolStripButton>> _menuItems;
		private RecordClerk _clerk;

		/// <summary />
		internal DataNavigationManager(ISubscriber subscriber, Dictionary<string, Tuple<ToolStripMenuItem, ToolStripButton>>  menuItems)
		{
			if (subscriber == null)
				throw new ArgumentNullException(nameof(subscriber));
			if (menuItems == null)
				throw new ArgumentNullException(nameof(menuItems));

			_subscriber = subscriber;
			_menuItems = menuItems;

			_subscriber.Subscribe("RecordNavigation", RecordNavigation_Message_Handler);

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

		private void RecordNavigation_Message_Handler(object obj)
		{
			SetEnabledStateForWidgets();
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
				_clerk = value;

				SetEnabledStateForWidgets();
			}
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

#region Implementation of IDisposable

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DataNavigationManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
				return; // Only run once.

			if (disposing)
			{
				_subscriber.Unsubscribe("RecordNavigation", RecordNavigation_Message_Handler);

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

			IsDisposed = true;
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

#endregion
	}
}