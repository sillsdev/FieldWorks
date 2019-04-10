// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

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
		/// <summary>
		/// Number of distinct macros we support.
		/// Note that just increasing this won't help. You will also need to add new commands and menu items to the
		/// XML configuration, typically Main.xml, with appropriate key parameters. Even then some work will be needed in GetMacroIndex
		/// to remove the assumption that slots are assigned to consecutive keys.
		/// </summary>
		private const int MacroCount = 11;

		internal void InitializeForTests(LcmCache cache)
		{
			_cache = cache;
		}

		/// <summary>
		/// Work over any imported macros: 1) put them in order, 2) resolve conflicts in shortcut keys, and 3) set up menus.
		/// </summary>
		internal void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, GlobalUiWidgetParameterObject globalParameterObject)
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
			var allMacroMenus = CollectMacroMenus(majorFlexComponentParameters.UiWidgetController.ToolsMenuDictionary);
			var shortcutKeys = new List<Keys>
			{
				Keys.F2,
				Keys.F3,
				Keys.F4,
				Keys.F6,
				Keys.F7,
				Keys.F8,
				Keys.F9,
				Keys.F10,
				Keys.F11,
				Keys.F12
			};
			var toolsMenuDictionary = globalParameterObject.GlobalMenuItems[MainMenu.Tools];
			var idx = 0;
			foreach (var macro in _currentMacros)
			{
				if (macro == null)
				{
					continue;
				}
				var macroMenu = allMacroMenus[idx];
				Command command;
				Enum.TryParse(macroMenu.Name, out command);
				macroMenu.ShortcutKeys = shortcutKeys[idx++];
				macroMenu.Tag = macro;
				toolsMenuDictionary.Add(command, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Macro_Clicked, () => CanDoMacro(macro)));
			}
		}

		private Tuple<bool, bool> CanDoMacro(IFlexMacro macro)
		{
			return new Tuple<bool, bool>(true, SafeToDoMacro(macro));
		}

		private static List<ToolStripMenuItem> CollectMacroMenus(IReadOnlyDictionary<Command, ToolStripItem> toolsMenuDictionary)
		{
			return new List<ToolStripMenuItem>
			{
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF2],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF3],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF4],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF6],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF7],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF8],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF9],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF10],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF11],
				(ToolStripMenuItem)toolsMenuDictionary[Command.CmdMacroF12]
			};
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
			UowHelpers.UndoExtension(commandName, obj.Cache.ActionHandlerAccessor, () => macro.RunMacro(obj, flid, ws, start, length));
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

		/// <summary>
		/// Only internal, because some test calls it.
		/// </summary>
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

		/// <summary>
		/// Only internal, because some test calls it.
		/// </summary>
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
			// Therefore, you should call GC.SuppressFinalize to
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
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_importedMacros?.Clear();
			}
			_importedMacros = null;
			_mainWindow = null;
			_cache = null;

			IsDisposed = true;
		}

		private bool IsDisposed { get; set; }

		#endregion
	}
}