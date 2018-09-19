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
	/// <summary>
	/// Factory class that creates context menus (via provided methods) for the PanelMenu class.
	/// </summary>
	/// <remarks>
	/// The PanelMenu class creates a new context menu each time one is to be displayed,
	/// since some tools (e.g., ReversalBulkEditReversalEntriesTool) need to rebuild the menu each time, depending on what currrently exists.
	/// </remarks>
	internal sealed class PanelMenuContextMenuFactory : IDisposable
	{
		private Dictionary<string, Func<string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _panelMenuCreatorMethods = new Dictionary<string, Func<string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given menu id, which are then used by a PanelMenu instance.
		/// </summary>
		internal Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> GetPanelMenu(string panelMenuId)
		{
			return _panelMenuCreatorMethods.ContainsKey(panelMenuId) ? _panelMenuCreatorMethods[panelMenuId].Invoke(panelMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the context menu and its menu items for an instance of PanelMenu.
		/// </summary>
		internal void RegisterPanelMenuCreatorMethod(string panelMenuId, Func<string, Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>> panelMenuCreatorMethod)
		{
			Guard.AgainstNullOrEmptyString(panelMenuId, nameof(panelMenuId));
			Guard.AgainstNull(panelMenuCreatorMethod, nameof(panelMenuCreatorMethod));
			Guard.AssertThat(!_panelMenuCreatorMethods.ContainsKey(panelMenuId), $"The method to create '{panelMenuId}' has already been registered.");

			// The client can decide to provide the "Opening" CancelEventHandler, or not, in the function.
			// If provided, the client wires up the handler to the menu, and we unwire it, when we dispose the menu.
			_panelMenuCreatorMethods.Add(panelMenuId, panelMenuCreatorMethod);
		}

		internal void RemovePanelMenuContextMenu(string panelMenuId)
		{
			if (!_panelMenuCreatorMethods.ContainsKey(panelMenuId))
			{
				return; // Nothing to remove.
			}
			_panelMenuCreatorMethods.Remove(panelMenuId);
		}

		#region IDisposable

		private bool _isDisposed;

		~PanelMenuContextMenuFactory()
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
				_panelMenuCreatorMethods.Clear();
			}
			_panelMenuCreatorMethods = null;

			_isDisposed = true;
		}

		#endregion
	}
}