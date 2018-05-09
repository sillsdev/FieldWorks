// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// The MacroMenuHandler class allows FLEx to have user-defined macros anywhere there is a view-based selection.
	/// To create a macro, make an Assembly with a name starting with "Macro" that implements IFlexMacro, build it,
	/// and drop the DLL in the FieldWorks root directory.
	/// </summary>
	[Export(typeof(MacroMenuHandler))]
	public class MacroMenuHandler : IDisposable
	{
		[ImportMany]
		private List<IFlexMacro> _importedMacros;
		private List<IFlexMacro> _currentMacros;
		private IFwMainWnd _mainWindow;
		private LcmCache _cache;
		private ToolStripMenuItem _toolsMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newToolsMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		// Number of distinct macros we support.
		// Note that just increasing this won't help. You will also need to add new commands and menu items to the
		// XML configuration, typically Main.xml, with appropriate key parameters. Even then some work will be needed in GetMacroIndex
		// to remove the assumption that slots are assigned to consecutive keys.
		private const int MacroCount = 11;

		internal void InitializeForTests(LcmCache cache)
		{
			_cache = cache;
		}

		/// <summary>
		/// Work over any imported macros: 1) put them in order, 2) resolve conflicts in shortcut keys, and 3) set up menus.
		/// </summary>
		internal void Initialize(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_mainWindow = majorFlexComponentParameters.MainWindow;
			_cache = majorFlexComponentParameters.LcmCache;

			if (!_importedMacros.Any())
			{
				// No imported macros to add.
				return;
			}
			AssignMacrosToSlots(_importedMacros);
			if (!_currentMacros.Any())
			{
				return;
			}
			_toolsMenu = MenuServices.GetToolsMenu(majorFlexComponentParameters.MenuStrip);
			_toolsMenu.DropDownOpening += ToolsMenu_DropDownOpening;
			// They all go to the end of the Tools menu.
			var shortcutKeys = new Dictionary<int, Keys>
			{
				{ 0, Keys.F2 },
				{ 1, Keys.F3 },
				{ 2, Keys.F4 },
				{ 3, Keys.F5 },
				{ 4, Keys.F6 },
				{ 5, Keys.F7 },
				{ 6, Keys.F8 },
				{ 7, Keys.F9 },
				{ 8, Keys.F10 },
				{ 9, Keys.F11 },
				{ 10, Keys.F12 }
			};
			var idx = 0;
			foreach (var macro in _currentMacros)
			{
				if (macro == null)
				{
					continue;
				}
				var newMacroMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newToolsMenusAndHandlers, _toolsMenu, Macro_Clicked, macro.CommandName, shortcutKeys: shortcutKeys[idx++]);
				newMacroMenu.Tag = macro;
			}
		}

		private void ToolsMenu_DropDownOpening(object sender, EventArgs e)
		{
			foreach (var tuple in _newToolsMenusAndHandlers)
			{
				var macroMenu = tuple.Item1;
				var macro = (IFlexMacro)macroMenu.Tag;
				macroMenu.Enabled = SafeToDoMacro(macro);
			}
		}

		private void Macro_Clicked(object sender, EventArgs e)
		{
			var macro = (IFlexMacro)((ToolStripMenuItem)sender).Tag;

			ICmObject obj;
			int flid;
			int ws;
			int start;
			int length;
			if (!SafeToDoMacro(macro, out obj, out flid, out ws, out start, out length))
			{
				return;
			}
			var commandName = macro.CommandName;
			// We normally let undo and redo be localized independently, but we compromise in the interests of making macros
			// easier to create.
			UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.ksUndoMacro, commandName), string.Format(LanguageExplorerResources.ksRedoMacro, commandName), obj.Cache.ActionHandlerAccessor,
				() => macro.RunMacro(obj, flid, ws, start, length));
		}

		/// <summary>
		/// This is only internal, because tests call it.
		/// </summary>
		internal IFlexMacro[] AssignMacrosToSlots(List<IFlexMacro> macroImplementors)
		{
			var macros = new IFlexMacro[MacroCount];
			var conflicts = new List<IFlexMacro>();
			// Put each at its preferred key if possible
			foreach (var macro in macroImplementors)
			{
				var index = GetMacroIndex(macro.PreferredFunctionKey);
				if (macros[index] == null)
				{
					macros[index] = macro;
				}
				else
				{
					conflicts.Add(macro);
				}
			}
			// Put any conflicts in remaining slots; if too many, arbitrary ones will be left out.
			foreach (var macro in conflicts)
			{
				for (var index = 0; index < MacroCount; index++)
				{
					if (macros[index] == null)
					{
						macros[index] = macro;
						break;
					}
				}
			}
			_currentMacros = new List<IFlexMacro>(macros);
			return macros;
		}

		private static int GetMacroIndex(Keys key)
		{
			var result = key - Keys.F2;
			if (result < 0 || result >= MacroCount)
			{
				throw new ArgumentException("Key assigned to macro must currently be between F2 and F12");
			}
			return result;
		}

		internal bool SafeToDoMacro(IFlexMacro macro)
		{
			ICmObject obj;
			int flid;
			int ws;
			int start;
			int length;
			return SafeToDoMacro(macro, out obj, out flid, out ws, out start, out length);
		}

		private bool SafeToDoMacro(IFlexMacro macro, out ICmObject obj, out int flid, out int ws, out int start, out int length)
		{
			var selection = (_mainWindow?.ActiveView as IVwRootSite)?.RootBox?.Selection;
			return SafeToDoMacro(macro, selection, out obj, out flid, out ws, out start, out length);
		}

		internal bool SafeToDoMacro(IFlexMacro macro, IVwSelection sel, out ICmObject obj, out int flid, out int ws, out int start, out int length)
		{
			start = flid = ws = length = 0; // defaults so we can return early.
			obj = null;
			if (macro == null || sel == null || sel.SelType != VwSelType.kstText)
			{
				return false;
			}
			ITsString dummy;
			int hvoA, hvoE, flidE, ichA, ichE, wsE;
			bool fAssocPrev;
			sel.TextSelInfo(false, out dummy, out ichA, out fAssocPrev, out hvoA, out flid, out ws);
			sel.TextSelInfo(true, out dummy, out ichE, out fAssocPrev, out hvoE, out flidE, out wsE);
			// for safety require selection to be in a single property.
			if (hvoA != hvoE || flid != flidE || ws != wsE)
			{
				return false;
			}
			obj = _cache.ServiceLocator.ObjectRepository.GetObject(hvoA);
			start = Math.Min(ichA, ichE);
			length = Math.Max(ichA, ichE) - start;
			return macro.Enabled(obj, flid, ws, start, length);
		}

		#region IDisposable

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MacroMenuHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (_toolsMenu != null)
				{
					_toolsMenu.DropDownOpening -= ToolsMenu_DropDownOpening;
					// Unwire each macro menu.
					foreach (var tuple in _newToolsMenusAndHandlers)
					{
						var macroMenu = tuple.Item1;
						_toolsMenu.DropDownItems.Remove(macroMenu);
						macroMenu.Click -= tuple.Item2;
						macroMenu.Dispose();
					}
					_newToolsMenusAndHandlers.Clear();
				}
				_importedMacros?.Clear();
			}
			_importedMacros = null;
			_mainWindow = null;
			_cache = null;
			_toolsMenu = null;
			_newToolsMenusAndHandlers = null;

			IsDisposed = true;
		}

		private bool IsDisposed { get; set; }

		#endregion
	}
}
