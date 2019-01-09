// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer
{
	internal static class UowHelpers
	{
		internal static void UndoExtension(string baseText, IActionHandler actionHandler, Action task)
		{
			UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.Undo_0, baseText), string.Format(LanguageExplorerResources.Redo_0, baseText), actionHandler, task);
		}

		internal static void UndoExtensionUsingNewOrCurrentUOW(string baseText, IActionHandler actionHandler, Action task)
		{
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(string.Format(LanguageExplorerResources.Undo_0, baseText), string.Format(LanguageExplorerResources.Redo_0, baseText), actionHandler, task);
		}
	}
}
