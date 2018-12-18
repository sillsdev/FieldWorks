// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// This class handles all interaction for the Lists Area common menus.
	/// </summary>
	internal sealed class ListsAreaMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private Dictionary<string, IToolUiWidgetManager> _listAreaUiWidgetManagers;
		private ISharedEventHandlers _sharedEventHandlers;
		private IListArea _listArea;
		private IRecordList MyRecordList { get; set; }
		private DataTree MyDataTree { get; }
		private const string editMenu = "editMenu";
		private const string insertMenu = "insertMenu";
		private const string toolsMenu = "toolsMenu";
		private const string insertToolbar = "insertToolbar";
		private const string dataTreeStack = "dataTreeStack";
		internal const string AddNewPossibilityListItem = "AddNewPossibilityListItem";
		internal const string AddNewSubPossibilityListItem = "AddNewSubPossibilityListItem";
		internal const string InsertFeatureType = "InsertFeatureType";
		internal AreaWideMenuHelper MyAreaWideMenuHelper { get; private set; }

		internal ListsAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, DataTree dataTree, IListArea listArea, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(listArea, nameof(listArea));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_listArea = listArea;
			MyRecordList = recordList;
			MyDataTree = dataTree;
			MyAreaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, recordList); // We want this to get the shared AreaServices.DataTreeDelete handler.
		}

		internal void Initialize()
		{
			// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
			MyAreaWideMenuHelper.SetupFileExportMenu();
			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);

			_listAreaUiWidgetManagers = new Dictionary<string, IToolUiWidgetManager>
			{
				{ editMenu, new ListsAreaEditMenuManager(_listArea) },
				{ insertMenu, new ListsAreaInsertMenuManager(MyDataTree, _listArea) },
				{ toolsMenu, new ListsAreaToolsMenuManager(_listArea) },
				// The ListsAreaInsertMenuManager instance adds shared event handlers that ListsAreaToolbarManager needs to use.
				{ insertToolbar, new ListsAreaToolbarManager(MyDataTree, _listArea) },
				{ dataTreeStack, new ListsAreaDataTreeStackManager(MyDataTree, _listArea) }
			};

			// Now, it is fine to finish up the initialization of the managers, since all shared event handlers are in '_sharedEventHandlers'.
			foreach (var manager in _listAreaUiWidgetManagers.Values)
			{
				manager.Initialize(_majorFlexComponentParameters, MyRecordList);
			}
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~ListsAreaMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
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
				foreach (var manager in _listAreaUiWidgetManagers.Values)
				{
					manager.UnwireSharedEventHandlers();
				}
				foreach (var manager in _listAreaUiWidgetManagers.Values)
				{
					manager.Dispose();
				}
				_listAreaUiWidgetManagers.Clear();
				MyAreaWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			MyAreaWideMenuHelper = null;
			_listArea = null;
			MyRecordList = null;
			_sharedEventHandlers = null;
			_listAreaUiWidgetManagers = null;

			_isDisposed = true;
		}
		#endregion

		internal static ICmPossibilityList GetPossibilityList(IRecordList recordList)
		{
			return recordList.OwningObject as ICmPossibilityList; // This will be null for the AreaServices.FeatureTypesAdvancedEditMachineName tool, which isn't a list at all.
		}
	}
}