// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists.Tools
{
	/// <summary>
	/// This class sets up all lists that only contain instances of the ICmPossibility base class, and none of its sub-classes.
	/// The list determines if sub-items can be created. "if (currentPossibilityList.Depth > 1)"
	/// </summary>
	/// <remarks>
	/// This class will be owned by a list tool, or by its menu helper class.
	/// Either way, the creator must call "SetupToolUiWidgets" and feed it the ToolUiWidgetParameterObject and add the handlers to the UI controller.
	/// </remarks>
	internal sealed class SharedForPlainVanillaListToolMenuHelper : IDisposable
	{
		private readonly MajorFlexComponentParameters _majorFlexComponentParameters;
		private readonly ITool _tool;
		private readonly ICmPossibilityList _list;
		private readonly IRecordList _recordList;
		private PartiallySharedListToolMenuHelper _partiallySharedListToolMenuHelper;

		internal SharedForPlainVanillaListToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, PartiallySharedForToolsWideMenuHelper partiallySharedForToolsWideMenuHelper, ITool tool, ICmPossibilityList list, IRecordList recordList, DataTree dataTree)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(partiallySharedForToolsWideMenuHelper, nameof(partiallySharedForToolsWideMenuHelper));
			Guard.AgainstNull(tool, nameof(tool));
			Guard.AgainstNull(list, nameof(list));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			var plainVanillaToolNames = new HashSet<string>
			{
				AreaServices.ChartmarkEditMachineName,
				AreaServices.CharttempEditMachineName,
				AreaServices.ConfidenceEditMachineName,
				AreaServices.DialectsListEditMachineName,
				AreaServices.DomainTypeEditMachineName,
				AreaServices.EducationEditMachineName,
				AreaServices.ExtNoteTypeEditMachineName,
				AreaServices.GenresEditMachineName,
				AreaServices.LanguagesListEditMachineName,
				AreaServices.PositionsEditMachineName,
				AreaServices.PublicationsEditMachineName,
				AreaServices.RecTypeEditMachineName,
				AreaServices.RestrictionsEditMachineName,
				AreaServices.RoleEditMachineName,
				AreaServices.SenseTypeEditMachineName,
				AreaServices.StatusEditMachineName,
				AreaServices.TextMarkupTagsEditMachineName,
				AreaServices.TimeOfDayEditMachineName,
				AreaServices.TranslationTypeEditMachineName,
				AreaServices.UsageTypeEditMachineName
			};
			Require.That(plainVanillaToolNames.Contains(tool.MachineName));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_tool = tool;
			_list = list;
			_recordList = recordList;
			_partiallySharedListToolMenuHelper = new PartiallySharedListToolMenuHelper(_majorFlexComponentParameters, partiallySharedForToolsWideMenuHelper, _list, _recordList, dataTree);
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			_partiallySharedListToolMenuHelper.SetupToolUiWidgets(toolUiWidgetParameterObject);
			// <command id="CmdInsertPossibility" label="_Item" message="InsertItemInVector" icon="AddItem">
			// <command id="CmdDataTree-Insert-Possibility" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
			var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
			var insertToolbarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
			insertMenuDictionary.Add(Command.CmdInsertPossibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertPossibility_Click, () => CanCmdInsertPossibility));
			insertToolbarDictionary.Add(Command.CmdInsertPossibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertPossibility_Click, () => CanCmdInsertPossibility));
			insertMenuDictionary.Add(Command.CmdDataTree_Insert_Possibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_Possibility_Click, () => CanCmdDataTree_Insert_Possibility));
			insertToolbarDictionary.Add(Command.CmdDataTree_Insert_Possibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_Possibility_Click, () => CanCmdDataTree_Insert_Possibility));
		}

		private static Tuple<bool, bool> CanCmdInsertPossibility => new Tuple<bool, bool>(true, true);

		private void CmdInsertPossibility_Click(object sender, EventArgs e)
		{
			var newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), _list);
			if (newPossibility != null)
			{
				_recordList.UpdateRecordTreeBar();
			}
		}

		private Tuple<bool, bool> CanCmdDataTree_Insert_Possibility => new Tuple<bool, bool>(true, _list.Depth > 1 && _recordList.CurrentObject != null);

		private void CmdDataTree_Insert_Possibility_Click(object sender, EventArgs e)
		{
			var newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), (ICmPossibility)_recordList.CurrentObject);
			if (newPossibility != null)
			{
				_recordList.UpdateRecordTreeBar();
			}
		}

		#region Implementation of IDisposable
		private bool _isDisposed;

		~SharedForPlainVanillaListToolMenuHelper()
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
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_partiallySharedListToolMenuHelper.Dispose();
			}
			_partiallySharedListToolMenuHelper = null;

			_isDisposed = true;
		}
		#endregion
	}
}