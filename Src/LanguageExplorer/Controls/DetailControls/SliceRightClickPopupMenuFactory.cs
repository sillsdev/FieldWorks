// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class SliceRightClickPopupMenuFactory : IDisposable
	{
		private Dictionary<string, Func<Slice, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _popupContextMenuCreatorMethods = new Dictionary<string, Func<Slice, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given right-click popup context menu for the given Slice.
		/// </summary>
		internal Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> GetPopupContextMenu(Slice slice, string ordinaryMenuId)
		{
			return _popupContextMenuCreatorMethods.ContainsKey(ordinaryMenuId) ? _popupContextMenuCreatorMethods[ordinaryMenuId].Invoke(slice, ordinaryMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the right-click popup context menu and its menu items.
		/// </summary>
		internal void RegisterPopupContextCreatorMethod(string popupContextMenuId, Func<Slice, string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>> popupContextMenuCreatorMethod)
		{
			Guard.AgainstNullOrEmptyString(popupContextMenuId, nameof(popupContextMenuId));
			Guard.AgainstNull(popupContextMenuCreatorMethod, nameof(popupContextMenuCreatorMethod));
			Guard.AssertThat(!_popupContextMenuCreatorMethods.ContainsKey(popupContextMenuId), $"The method to create '{nameof(popupContextMenuId)}' has already been registered.");

			_popupContextMenuCreatorMethods.Add(popupContextMenuId, popupContextMenuCreatorMethod);
		}


		internal void DisposePopupContextMenu(Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> popupContextMenuTuple)
		{
			if (popupContextMenuTuple == null)
			{
				return;
			}
			// Unwire event handlers.
			foreach (var popupMenuItemTuple in popupContextMenuTuple.Item2)
			{
				popupMenuItemTuple.Item1.Click -= popupMenuItemTuple.Item2;
			}

			// Dispose menu and its items.
			// It needs to do it on that "ToList", since simply disposing it will remove it from the "Items" collection,
			// which then throws with a changing contents while iterating.
			foreach (var item in popupContextMenuTuple.Item1.Items.Cast<IDisposable>().ToList())
			{
				item.Dispose();
			}
			popupContextMenuTuple.Item1.Dispose();

			// Clear out the list of ToolStripMenuItem items.
			popupContextMenuTuple.Item2.Clear();
		}

		#region IDisposable

		private bool _isDisposed;

		~SliceRightClickPopupMenuFactory()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

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


		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_popupContextMenuCreatorMethods.Clear();
			}
			_popupContextMenuCreatorMethods = null;

			_isDisposed = true;
		}

		#endregion
	}
}