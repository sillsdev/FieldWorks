// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class UowHelpers
	{
		public static void UndoExtension(string baseText, IActionHandler actionHandler, Action task)
		{
			UndoableUnitOfWorkHelper.Do(string.Format(FwUtilsStrings.Undo_0, baseText.Replace("_", string.Empty)), string.Format(FwUtilsStrings.Redo_0, baseText.Replace("_", string.Empty)), actionHandler, task);
		}

		public static void UndoExtensionUsingNewOrCurrentUOW(string baseText, IActionHandler actionHandler, Action task)
		{
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(string.Format(FwUtilsStrings.Undo_0, baseText.Replace("_", string.Empty)), string.Format(FwUtilsStrings.Redo_0, baseText.Replace("_", string.Empty)), actionHandler, task);
		}
	}
}
