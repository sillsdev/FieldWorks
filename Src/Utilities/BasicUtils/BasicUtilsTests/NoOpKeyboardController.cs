// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Palaso.UI.WindowsForms.Keyboarding.InternalInterfaces;
using Palaso.UI.WindowsForms.Keyboarding.Types;
using Palaso.WritingSystems;

namespace SIL.Utils
{
#pragma warning disable 67 // The event 'SIL.Utils.NoOpKeyboardController.ControlAdded'/ControlRemoved is never used

	/// <summary>
	/// A do-nothing keyboard controller implementation.
	/// </summary>
	public class NoOpKeyboardController: IKeyboardControllerImpl, IKeyboardController
	{
		public IKeyboardDefinition GetKeyboard(string layoutName)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition GetKeyboard(string layoutName, string locale)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition GetKeyboard(IWritingSystemDefinition writingSystem)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition GetKeyboard(IInputLanguage language)
		{
			throw new NotImplementedException();
		}

		public void SetKeyboard(IKeyboardDefinition keyboard)
		{
		}

		public void SetKeyboard(string layoutName)
		{
		}

		public void SetKeyboard(string layoutName, string locale)
		{
		}

		public void SetKeyboard(IWritingSystemDefinition writingSystem)
		{
		}

		public void SetKeyboard(IInputLanguage language)
		{
		}

		public void ActivateDefaultKeyboard()
		{
		}

		public void UpdateAvailableKeyboards()
		{
		}

		public IKeyboardDefinition DefaultForWritingSystem(IWritingSystemDefinition ws)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition LegacyForWritingSystem(IWritingSystemDefinition ws)
		{
			throw new NotImplementedException();
		}

		public IKeyboardDefinition CreateKeyboardDefinition(string layout, string locale)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IKeyboardDefinition> AllAvailableKeyboards
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#region IKeyboardControllerImpl implementation

		public event RegisterEventHandler ControlAdded;

		public event ControlEventHandler ControlRemoving;

		public void RegisterControl(Control control, object eventHandler)
		{
		}

		public void UnregisterControl(Control control)
		{
		}

		public KeyboardCollection Keyboards
		{
			get { return new KeyboardCollection(); }
		}

		public IKeyboardDefinition ActiveKeyboard
		{
			get { return null; }
			set { }
		}

		public Dictionary<Control, object> EventHandlers
		{
			get { return new Dictionary<Control, object>(); }
		}
		#endregion

		#region IDisposable implementation
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#if DEBUG
		~NoOpKeyboardController()
		{
			Dispose(false);
		}
		#endif

		protected virtual void Dispose(bool disposing)
		{
			// We do NOT want the standard message, because Jenkins decides to mark the build UNSTABLE if it sees it!
			// And this is used only in test code, and only in situations where's it looks to be impossible
			// to dispose it properly.
			System.Diagnostics.Debug.WriteLineIf(!disposing, "ignoring missing Dispose() call for " + GetType() + ".");
		}
	}
#pragma warning restore 67
}
