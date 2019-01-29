// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main View menu for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolViewMenuManager : IPartialToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private IPropertyTable _propertyTable;
		private ISubscriber _subscriber;
		private IPublisher _publisher;
		private ToolStripMenuItem _viewMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newViewMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _show_DictionaryPubPreviewMenu;
		private ToolStripMenuItem _showHiddenFieldsMenu;
		private string _extendedPropertyName;
		private MultiPane _innerMultiPane;

		internal LexiconEditToolViewMenuManager(string extendedPropertyName, MultiPane innerMultiPane)
		{
			Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));
			Guard.AgainstNull(innerMultiPane, nameof(innerMultiPane));

			_extendedPropertyName = extendedPropertyName;
			_innerMultiPane = innerMultiPane;
		}

		#region Implementation of IPartialToolUiWidgetManager

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IToolUiWidgetManager toolUiWidgetManager, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_subscriber = majorFlexComponentParameters.FlexComponentParameters.Subscriber;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			_sharedEventHandlers.Add(LexiconEditToolConstants.Show_Dictionary_Preview_Clicked, Show_Dictionary_Preview_Clicked);

			MyRecordList = recordList;

			_subscriber.Subscribe("ShowHiddenFields", ShowHiddenFields_Handler);

			_viewMenu = MenuServices.GetViewMenu(majorFlexComponentParameters.MenuStrip);
			_viewMenu.DropDownOpening += ViewMenu_DropDown_Opening;
			// <item label="Show _Dictionary Preview" boolProperty="Show_DictionaryPubPreview" defaultVisible="false"/>
			_show_DictionaryPubPreviewMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newViewMenusAndHandlers, _viewMenu, Show_Dictionary_Preview_Clicked, LexiconResources.Show_DictionaryPubPreview, insertIndex: _viewMenu.DropDownItems.Count - 2);
			_show_DictionaryPubPreviewMenu.Checked = _propertyTable.GetValue<bool>(LexiconEditToolConstants.Show_DictionaryPubPreview);
			// <item label="_Show Hidden Fields" boolProperty="ShowHiddenFields" defaultVisible="false"/>
			_showHiddenFieldsMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newViewMenusAndHandlers, _viewMenu, Show_Hidden_Fields_Clicked, LanguageExplorerResources.ksShowHiddenFields, insertIndex: _viewMenu.DropDownItems.Count - 2);
			_showHiddenFieldsMenu.Checked = _propertyTable.GetValue(_extendedPropertyName, false);
		}

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolViewMenuManager()
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				_sharedEventHandlers.Remove(LexiconEditToolConstants.Show_Dictionary_Preview_Clicked);
				_subscriber.Unsubscribe("ShowHiddenFields", ShowHiddenFields_Handler);
				_viewMenu.DropDownOpening -= ViewMenu_DropDown_Opening;
				foreach (var menuTuple in _newViewMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_viewMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newViewMenusAndHandlers.Clear();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			_propertyTable = null;
			_subscriber = null;
			_publisher = null;
			_viewMenu = null;
			_newViewMenusAndHandlers = null;
			_show_DictionaryPubPreviewMenu = null;
			_showHiddenFieldsMenu = null;
			_extendedPropertyName = null;
			_innerMultiPane = null;

			_isDisposed = true;
		}

		#endregion

		private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			_propertyTable.SetProperty(LexiconEditToolConstants.Show_DictionaryPubPreview, menuItem.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
			_innerMultiPane.Panel1Collapsed = !menuItem.Checked;
		}

		private void Show_Hidden_Fields_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			_propertyTable.SetProperty(_extendedPropertyName, menuItem.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
			_publisher.Publish("ShowHiddenFields", menuItem.Checked);
			_innerMultiPane.Panel1Collapsed = !menuItem.Checked;
		}

		private void ShowHiddenFields_Handler(object obj)
		{
			_showHiddenFieldsMenu.Checked = (bool)obj;
		}

		private void ViewMenu_DropDown_Opening(object sender, EventArgs e)
		{
			var storedCheckedValue = _propertyTable.GetValue<bool>(LexiconEditToolConstants.Show_DictionaryPubPreview);
			if (_show_DictionaryPubPreviewMenu.Checked != storedCheckedValue)
			{
				_show_DictionaryPubPreviewMenu.Checked = storedCheckedValue;
			}
		}
	}
}