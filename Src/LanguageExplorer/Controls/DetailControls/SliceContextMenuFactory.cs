// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// <para>Factory class that is given to each Slice, so the Slice can then get the context menu(s) it may need.</para>
	/// <para>Each slice is responsible to dispose of any context menu it asks the factory to create.
	/// The slice can pass on the dispose obligation to one of its internal Control instances, or the Slice can do the dispose itself.
	/// The context menu disposal *must* (read: it is imperative that) unwire the event handlers, which are provided.</para>
	/// </summary>
	internal sealed class SliceContextMenuFactory: IDisposable
	{
		#region Slice Hotlinks handling
		private Dictionary<string, Func<Slice, string, List<Tuple<ToolStripMenuItem, EventHandler>>>> _hotLinksCreatorMethods = new Dictionary<string, Func<Slice, string, List<Tuple<ToolStripMenuItem, EventHandler>>>>();

		/// <summary>
		/// Get the list of ToolStripMenuItem items for the given menu for the given Slice.
		/// </summary>
		internal List<Tuple<ToolStripMenuItem, EventHandler>> GetHotlinksMenuItems(Slice slice, string hotlinksMenuId)
		{
			return _hotLinksCreatorMethods.ContainsKey(hotlinksMenuId) ? _hotLinksCreatorMethods[hotlinksMenuId].Invoke(slice, hotlinksMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the hotlinks ToolStripMenuItem items.
		/// </summary>
		internal void RegisterHotlinksMenuCreatorMethod(string hotlinksMenuId, Func<Slice, string, List<Tuple<ToolStripMenuItem, EventHandler>>> hotlinksMenuCreatorMethod)
		{
			if (string.IsNullOrWhiteSpace(hotlinksMenuId)) throw new ArgumentNullException(nameof(hotlinksMenuId));
			if (hotlinksMenuCreatorMethod == null) throw new ArgumentNullException(nameof(hotlinksMenuCreatorMethod));
			if (_hotLinksCreatorMethods.ContainsKey(hotlinksMenuId)) throw new InvalidOperationException($"The method to create '{nameof(hotlinksMenuId)}' has already been registered.");

			_hotLinksCreatorMethods.Add(hotlinksMenuId, hotlinksMenuCreatorMethod);
		}
		#endregion Slice Hotlinks handling

		#region Slice Ordinary (blue triangle on left of slice) context menu handling
		private Dictionary<string, Func<Slice, string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _ordinaryMenuCreatorMethods = new Dictionary<string, Func<Slice, string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given menu for the given Slice.
		/// </summary>
		internal Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> GetOrdinaryMenu(Slice slice, string ordinaryMenuId)
		{
			return _ordinaryMenuCreatorMethods.ContainsKey(ordinaryMenuId) ? _ordinaryMenuCreatorMethods[ordinaryMenuId].Invoke(slice, ordinaryMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the Ordinary (blue triangle on left of slice) context menu and its menu items.
		/// </summary>
		internal void RegisterOrdinaryMenuCreatorMethod(string ordinaryMenuId, Func<Slice, string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>> ordinaryMenuCreatorMethod)
		{
			if (string.IsNullOrWhiteSpace(ordinaryMenuId)) throw new ArgumentNullException(nameof(ordinaryMenuId));
			if (ordinaryMenuCreatorMethod == null) throw new ArgumentNullException(nameof(ordinaryMenuCreatorMethod));
			if (_ordinaryMenuCreatorMethods.ContainsKey(ordinaryMenuId)) throw new InvalidOperationException($"The method to create '{nameof(ordinaryMenuId)}' has already been registered.");

			_ordinaryMenuCreatorMethods.Add(ordinaryMenuId, ordinaryMenuCreatorMethod);
		}
		#endregion Slice Ordinary (blue triangle on left of slice) handling

		#region PanelMenu context menu handling
		private Dictionary<string, Func<string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>>> _panelMenuCreatorMethods = new Dictionary<string, Func<string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>>>();

		/// <summary>
		/// Get the ContextMenuStrip and a list of its ToolStripMenuItem items for the given menu id, which are then used by a PanelMenu instance.
		/// </summary>
		internal Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> GetPanelMenu(string panelMenuId)
		{
			return _panelMenuCreatorMethods.ContainsKey(panelMenuId) ? _panelMenuCreatorMethods[panelMenuId].Invoke(panelMenuId) : null;
		}

		/// <summary>
		/// Register a method that can be used to create the context menu and its menu items for an instance of PanelMenu.
		/// </summary>
		internal void RegisterPanelMenuCreatorMethod(string panelMenuId, Func<string, Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>> panelMenuCreatorMethod)
		{
			if (string.IsNullOrWhiteSpace(panelMenuId)) throw new ArgumentNullException(nameof(panelMenuId));
			if (panelMenuCreatorMethod == null) throw new ArgumentNullException(nameof(panelMenuCreatorMethod));
			if (_panelMenuCreatorMethods.ContainsKey(panelMenuId)) throw new InvalidOperationException($"The method to create '{nameof(panelMenuId)}' has already been registered.");

			_panelMenuCreatorMethods.Add(panelMenuId, panelMenuCreatorMethod);
		}
		#endregion PanelMenu context menu handling

		#region Dispose the context menus and unwire handlers

		/// <summary>
		/// Dispose the ToolStripMenuItem instances
		/// </summary>
		internal void DisposeHotLinksMenus(List<Tuple<ToolStripMenuItem, EventHandler>> hotLinksMenus)
		{
			if (hotLinksMenus == null)
			{
				return;
			}
			foreach (var tuple in hotLinksMenus)
			{
				tuple.Item1.Click -= tuple.Item2;
				tuple.Item1.Dispose();
			}
			hotLinksMenus.Clear();
		}

		/// <summary>
		/// Dispose the ContextMenuStrip and all of its items, whether previously registered with event handlers, or not.
		/// </summary>
		/// <remarks>
		/// This method can/should be called to dispose menus and items that were created by GetPanelMenu or by GetOrdinaryMenu.
		/// </remarks>
		internal void DisposeContextMenu(Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> contextMenuTuple)
		{
			if (contextMenuTuple == null)
			{
				return;
			}
			// Unwire event handlers.
			foreach (var menuItemTuple in contextMenuTuple.Item3)
			{
				menuItemTuple.Item1.Click -= menuItemTuple.Item2;
			}
			contextMenuTuple.Item1.Opening -= contextMenuTuple.Item2;

			// Dispose menu and its items
			foreach (var item in contextMenuTuple.Item1.Items.Cast<IDisposable>())
			{
				item.Dispose();
			}
			contextMenuTuple.Item1.Dispose();

			// Clear out the list of ToolStripMenuItem items.
			contextMenuTuple.Item3.Clear();
		}
		#endregion Dispose the context menus and unwire handlers

		#region IDisposable
		private bool _isDisposed;

		~SliceContextMenuFactory()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
				return; // No need to do it more than once.

			if (disposing)
			{
				_hotLinksCreatorMethods.Clear();
				_ordinaryMenuCreatorMethods.Clear();
				_panelMenuCreatorMethods.Clear();
			}
			_hotLinksCreatorMethods = null;
			_ordinaryMenuCreatorMethods = null;
			_panelMenuCreatorMethods = null;
		}
		#endregion
	}
}
