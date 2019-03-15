// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.Code;
using SIL.ObjectModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Manager for the four navigation buttons.
	/// </summary>
	/// <remarks>
	/// The idea is that individual tools that care about such record navigation
	/// can manage the enabled state of the four navigation buttons.
	/// </remarks>
	internal sealed class DataNavigationManager : DisposableBase
	{
		private IRecordList _recordList;

		/// <summary />
		internal DataNavigationManager(UiWidgetController uiWidgetController)
		{
			Guard.AgainstNull(uiWidgetController, nameof(uiWidgetController));

			var globalSendReceiveMenuHandlers = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
			{
				{ Command.CmdFirstRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(First_Click, ()=> CanDoCmdFirstRecord) },
				{ Command.CmdPreviousRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Previous_Click, ()=> CanDoCmdPreviousRecord) },
				{ Command.CmdNextRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Next_Click, ()=> CanDoCmdNextRecord) },
				{ Command.CmdLastRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Last_Click, ()=> CanDoCmdLastRecord) }
			};
			var globalMenuData = new Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>
			{
				{MainMenu.Data,  globalSendReceiveMenuHandlers}
			};
			var globalToolBarHandlers = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
			{
				{ Command.CmdFirstRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(First_Click, ()=> CanDoCmdFirstRecord) },
				{ Command.CmdPreviousRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Previous_Click, ()=> CanDoCmdPreviousRecord) },
				{ Command.CmdNextRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Next_Click, ()=> CanDoCmdNextRecord) },
				{ Command.CmdLastRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Last_Click, ()=> CanDoCmdLastRecord) }
			};
			var globalToolBarData = new Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>
			{
				{ ToolBar.Standard, globalToolBarHandlers }
			};
			uiWidgetController.AddGlobalHandlers(globalMenuData, globalToolBarData);
		}

		private Tuple<bool, bool> CanDoCmdFirstRecord => new Tuple<bool, bool>(true, _recordList != null && _recordList.ListSize != 0 && _recordList.CanMoveToOptions[Navigation.First]);

		private Tuple<bool, bool> CanDoCmdPreviousRecord => new Tuple<bool, bool>(true, _recordList != null && _recordList.ListSize != 0 && _recordList.CanMoveToOptions[Navigation.Previous]);

		private Tuple<bool, bool> CanDoCmdNextRecord => new Tuple<bool, bool>(true, _recordList != null && _recordList.ListSize != 0 && _recordList.CanMoveToOptions[Navigation.Next]);

		private Tuple<bool, bool> CanDoCmdLastRecord => new Tuple<bool, bool>(true, _recordList != null && _recordList.ListSize != 0 && _recordList.CanMoveToOptions[Navigation.Last]);

		private void First_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.First);
		}

		private void Previous_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Previous);
		}

		private void Next_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Next);
		}

		private void Last_Click(object sender, EventArgs e)
		{
			MoveToIndex(Navigation.Last);
		}

		internal IRecordList RecordList
		{
			set
			{
				_recordList = value;
			}
		}

		private void MoveToIndex(Navigation navigateTo)
		{
			_recordList.MoveToIndex(navigateTo);
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}
	}
}