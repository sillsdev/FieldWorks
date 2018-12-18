// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class SliceHotlinksMenuFactory : IDisposable
	{
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
			Guard.AgainstNullOrEmptyString(hotlinksMenuId, nameof(hotlinksMenuId));
			Guard.AgainstNull(hotlinksMenuCreatorMethod, nameof(hotlinksMenuCreatorMethod));
			Guard.AssertThat(!_hotLinksCreatorMethods.ContainsKey(hotlinksMenuId), $"The method to create '{hotlinksMenuId}' has already been registered.");

			_hotLinksCreatorMethods.Add(hotlinksMenuId, hotlinksMenuCreatorMethod);
		}

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

		#region IDisposable

		private bool _isDisposed;

		~SliceHotlinksMenuFactory()
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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_hotLinksCreatorMethods.Clear();
			}
			_hotLinksCreatorMethods = null;

			_isDisposed = true;
		}

		#endregion
	}
}