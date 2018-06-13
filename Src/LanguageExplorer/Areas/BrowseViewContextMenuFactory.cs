// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer.Areas
{
	internal sealed class BrowseViewContextMenuFactory : IDisposable
	{
		private Dictionary<string, Func<IRecordList, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _browseViewContextMenuCreatorMethods = new Dictionary<string, Func<IRecordList, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given menu for the given Slice.
		/// </summary>
		internal Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> GetBrowseViewContextMenu(IRecordList recordList, string browseViewMenuId)
		{
			return _browseViewContextMenuCreatorMethods.ContainsKey(browseViewMenuId) ? _browseViewContextMenuCreatorMethods[browseViewMenuId].Invoke(recordList, browseViewMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the Ordinary (blue triangle on left of slice) context menu and its menu items.
		/// </summary>
		internal void RegisterBrowseViewContextMenuCreatorMethod(string browseViewContextMenuId, Func<IRecordList, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>> browseViewContextMenuCreatorMethod)
		{
			Guard.AgainstNullOrEmptyString(browseViewContextMenuId, nameof(browseViewContextMenuId));
			Guard.AgainstNull(browseViewContextMenuCreatorMethod, nameof(browseViewContextMenuCreatorMethod));
			Require.That(!_browseViewContextMenuCreatorMethods.ContainsKey(browseViewContextMenuId), $"The method to create '{browseViewContextMenuId}' has already been registered.");

			_browseViewContextMenuCreatorMethods.Add(browseViewContextMenuId, browseViewContextMenuCreatorMethod);
		}

		/// <summary>
		/// Dispose the ContextMenuStrip for a browse view, and all of its items, whether previously registered with event handlers, or not.
		/// </summary>
		internal void DisposeBrowseViewContextMenu(Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> browseViewContextMenuTuple)
		{
			if (browseViewContextMenuTuple == null)
			{
				return;
			}
			// Unwire event handlers.
			foreach (var menuItemTuple in browseViewContextMenuTuple.Item2)
			{
				menuItemTuple.Item1.Click -= menuItemTuple.Item2;
			}

			// Dispose menu and its items.
			// It needs to do it on that "ToList", since simply disposing it will remove it from the "Items" collection,
			// which then throws with a changing contents while iterating.
			foreach (var item in browseViewContextMenuTuple.Item1.Items.Cast<IDisposable>().ToList())
			{
				item.Dispose();
			}
			browseViewContextMenuTuple.Item1.Dispose();

			// Clear out the list of ToolStripMenuItem items.
			browseViewContextMenuTuple.Item2.Clear();
		}
		#region IDisposable

		private bool _isDisposed;

		/// <inheritdoc />
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

		/// <inheritdoc />
		~BrowseViewContextMenuFactory()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}


		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_browseViewContextMenuCreatorMethods?.Clear();
			}
			_browseViewContextMenuCreatorMethods = null;

			_isDisposed = true;
		}
		#endregion
	}
}