// Copyright (c) 2018-2020 SIL International
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
	/// <summary>
	/// Factory class that handles creation and disposal of the context menus that show up in the left edge of slices.
	/// </summary>
	internal sealed class SliceLeftEdgeContextMenuFactory : IDisposable
	{
		private Dictionary<ContextMenuName, Func<Slice, ContextMenuName, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _leftEdgeMenuCreatorMethods = new Dictionary<ContextMenuName, Func<Slice, ContextMenuName, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given menu for the given Slice.
		/// </summary>
		internal Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> GetLeftEdgeContextMenu(Slice slice, ContextMenuName ordinaryMenuId, bool addContextMenus = true)
		{
			var retval = _leftEdgeMenuCreatorMethods.ContainsKey(ordinaryMenuId) ? _leftEdgeMenuCreatorMethods[ordinaryMenuId].Invoke(slice, ordinaryMenuId) : null;
			if (addContextMenus)
			{
				slice.AddCoreContextMenus(ref retval);
			}
			return retval;
		}

		/// <summary>
		/// Register a method that can be used to create the Ordinary (blue triangle on left of slice) context menu and its menu items.
		/// </summary>
		internal void RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName leftEdgeContextMenuId, Func<Slice, ContextMenuName, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>> leftEdgeContextMenuCreatorMethod)
		{
			Require.That(leftEdgeContextMenuId != ContextMenuName.nullValue);
			Guard.AgainstNull(leftEdgeContextMenuCreatorMethod, nameof(leftEdgeContextMenuCreatorMethod));
			Guard.AssertThat(!_leftEdgeMenuCreatorMethods.ContainsKey(leftEdgeContextMenuId), $"The method to create '{leftEdgeContextMenuId}' has already been registered.");

			_leftEdgeMenuCreatorMethods.Add(leftEdgeContextMenuId, leftEdgeContextMenuCreatorMethod);
		}

		/// <summary>
		/// Dispose the ContextMenuStrip on the left edge of a Slice, and all of its items, whether previously registered with event handlers, or not.
		/// </summary>
		internal void DisposeLeftEdgeContextMenu(Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> leftEdgeContextMenuTuple)
		{
			if (leftEdgeContextMenuTuple == null)
			{
				return;
			}
			// Unwire event handlers.
			foreach (var menuItemTuple in leftEdgeContextMenuTuple.Item2)
			{
				menuItemTuple.Item1.Click -= menuItemTuple.Item2;
			}
			// Dispose menu and its items.
			// It needs to do it on that "ToList", since simply disposing it will remove it from the "Items" collection,
			// which then throws with a changing contents while iterating.
			foreach (var item in leftEdgeContextMenuTuple.Item1.Items.Cast<IDisposable>().ToList())
			{
				item.Dispose();
			}
			leftEdgeContextMenuTuple.Item1.Dispose();
			// Clear out the list of ToolStripMenuItem items.
			leftEdgeContextMenuTuple.Item2.Clear();
		}

		#region IDisposable

		private bool _isDisposed;

		~SliceLeftEdgeContextMenuFactory()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_leftEdgeMenuCreatorMethods.Clear();
			}
			_leftEdgeMenuCreatorMethods = null;

			_isDisposed = true;
		}

		#endregion
	}
}