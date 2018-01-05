// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Populate the writing systems combo box (Format toolbar) and the Format-Writing System menu with writing systems from the given set.
	/// </summary>
	internal sealed class WritingSystemListHandler : IDisposable
	{
		private IFwMainWnd _mainWnd;
		private LcmCache _cache;
		private ISubscriber _subscriber;
		private ToolStripComboBox _formatToolStripComboBox;
		private ToolStripMenuItem _writingSystemToolStripMenuItem;
		private List<CoreWritingSystemDefinition> _allWritingSystemDefinitions;


		internal WritingSystemListHandler(IFwMainWnd mainWnd, LcmCache cache, ISubscriber subscriber, ToolStripComboBox formatToolStripComboBox, ToolStripMenuItem writingSystemToolStripMenuItem)
		{
			Guard.AgainstNull(mainWnd, nameof(mainWnd));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(subscriber, nameof(subscriber));
			Guard.AgainstNull(formatToolStripComboBox, nameof(formatToolStripComboBox));
			Guard.AgainstNull(writingSystemToolStripMenuItem, nameof(writingSystemToolStripMenuItem));

			_mainWnd = mainWnd;
			_cache = cache;
			_subscriber = subscriber;

			_allWritingSystemDefinitions = _cache.ServiceLocator.WritingSystems.AllWritingSystems.ToList();

			_formatToolStripComboBox = formatToolStripComboBox;
			_writingSystemToolStripMenuItem = writingSystemToolStripMenuItem;
			_subscriber.Subscribe("WritingSystemHvo", UpdateComboboxSelectedItem);

			SetupControlsForWritingSystems();

			Application.Idle += ApplicationOnIdle;
		}

		#region IDisposable & Co. implementation
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool _isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. _isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~WritingSystemListHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose any combobox items or menu items that remain.
				_subscriber.Unsubscribe("WritingSystemHvo", UpdateComboboxSelectedItem);
				_formatToolStripComboBox.SelectedIndexChanged -= FormatToolStripComboBoxOnSelectedIndexChanged;
				foreach (ToolStripMenuItem submenu in _writingSystemToolStripMenuItem.DropDownItems)
				{
					submenu.Click -= WritingSystemToolStripMenuItemOnClick;
				}
				_allWritingSystemDefinitions.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_subscriber = null;
			_mainWnd = null;
			_cache = null;
			_formatToolStripComboBox = null;
			_writingSystemToolStripMenuItem = null;
			_allWritingSystemDefinitions = null;

			_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Update enabled status for the combobox on the toolbar and for the main menu on the Format menu.
		/// </summary>
		private void ApplicationOnIdle(object sender, EventArgs eventArgs)
		{
			var activeView = _mainWnd.ActiveView as SimpleRootSite;
			var enableControls = activeView != null && !(activeView is SandboxBase) && activeView.IsSelectionFormattable;
			_formatToolStripComboBox.Enabled = enableControls;
			_writingSystemToolStripMenuItem.Enabled = enableControls;
		}

		private void SetupControlsForWritingSystems()
		{
			_formatToolStripComboBox.ComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			foreach (var coreWritingSystemDefinition in _allWritingSystemDefinitions)
			{
				// 1. Add the WSes to the combobox.
				_formatToolStripComboBox.Items.Add(coreWritingSystemDefinition);
				// 2. Add the WSes to the WS menu.
				var newWsMenu = new ToolStripMenuItem(coreWritingSystemDefinition.DisplayLabel)
				{
					Tag = coreWritingSystemDefinition
				};
				_writingSystemToolStripMenuItem.DropDownItems.Add(newWsMenu);
				newWsMenu.Click += WritingSystemToolStripMenuItemOnClick;
			}
			// 3. Wire up combobox event handler.
			_formatToolStripComboBox.SelectedIndexChanged += FormatToolStripComboBoxOnSelectedIndexChanged;
		}

		private void WritingSystemToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
		{
			NotifyClientsOfWritingSystemChange(_allWritingSystemDefinitions.First(wsDefn => wsDefn.DisplayLabel == (string)_formatToolStripComboBox.SelectedItem));
		}

		private void FormatToolStripComboBoxOnSelectedIndexChanged(object sender, EventArgs eventArgs)
		{
			NotifyClientsOfWritingSystemChange((CoreWritingSystemDefinition)((ToolStripMenuItem)sender).Tag);
		}

		private void NotifyClientsOfWritingSystemChange(CoreWritingSystemDefinition newlySelectedWritingSystem)
		{
			_mainWnd.ActiveView?.EditingHelper.SetKeyboardForWs(newlySelectedWritingSystem);
		}

		private void UpdateComboboxSelectedItem(object newValue)
		{
			var newWs = (string)newValue;
			foreach (CoreWritingSystemDefinition item in _formatToolStripComboBox.Items)
			{
				if (item.Handle.ToString() != newWs)
				{
					continue;
				}
				// We are responding to an update from afar, so we don't want to turn around and send off an update to 'afar'.
				// So, unwire handler during the change.
				_formatToolStripComboBox.SelectedIndexChanged -= FormatToolStripComboBoxOnSelectedIndexChanged;
				_formatToolStripComboBox.SelectedItem = item;
				_formatToolStripComboBox.SelectedIndexChanged += FormatToolStripComboBoxOnSelectedIndexChanged;
				break;
			}
		}
	}
}