// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.FieldWorks.Common.Keyboarding.InternalInterfaces;
using SIL.Utils;
#if __MonoCS__
using SIL.FieldWorks.Views;
using SIL.FieldWorks.Common.Keyboarding.Linux;
#else
using SIL.FieldWorks.Common.Keyboarding.Windows;
#endif
using SIL.FieldWorks.Common.Keyboarding.Types;

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// Singleton class with methods for registering different keyboarding engines (e.g. Windows
	/// system, Keyman, XKB, IBus keyboards), and activating keyboards.
	/// </summary>
	public static class KeyboardController
	{
		#region Nested Manager class
		/// <summary>
		/// Allows setting different keyboard adapters which is needed for tests. Also allows
		/// registering keyboard layouts.
		/// </summary>
		public static class Manager
		{
			/// <summary>
			/// Sets the available keyboard adaptors
			/// </summary>
			internal static void SetKeyboardAdaptors(IKeyboardAdaptor[] adaptors)
			{
				if (SingletonsContainer.Contains<IKeyboardController>())
				{
					// we're modifying an existent KeyboardController.
					Instance.Keyboards.Clear();

					if (Adaptors != null)
					{
						foreach (var adaptor in Adaptors)
							adaptor.Close();
					}

					Adaptors = adaptors;

					InitializeAdaptors();
				}
				else
				{
					// KeyboardController doesn't exist yet. We'll initialize the adaptors
					// when the KeyboardController gets created
					Adaptors = adaptors;
				}
			}

			/// <summary>
			/// Resets the keyboard adaptors to the default ones.
			/// </summary>
			public static void Reset()
			{
				SetKeyboardAdaptors(new IKeyboardAdaptor[] {
#if __MonoCS__
					new XkbKeyboardAdaptor(), new IbusKeyboardAdaptor()
#else
					new WinKeyboardAdaptor()
#endif
				});
			}

			internal static void InitializeAdaptors()
			{
				// this will also populate m_keyboards
				foreach (var adaptor in Adaptors)
					adaptor.Initialize();
			}

			/// <summary>
			/// Adds a keyboard to the list of installed keyboards
			/// </summary>
			/// <param name='description'>Keyboard description object</param>
			public static void RegisterKeyboard(IKeyboardDescription description)
			{
				if (!Instance.Keyboards.Contains(description))
					Instance.Keyboards.Add(description);
			}
		}
		#endregion

		#region Class KeyboardControllerImpl
		private sealed class KeyboardControllerImpl: IKeyboardController, IDisposable
		{
			public KeyboardCollection Keyboards { get; private set; }
			public IKeyboardEventHandler InternalEventHandler { get; set; }
			public IKeyboardMethods InternalMethods { get; set; }

			public KeyboardControllerImpl()
			{
				Keyboards = new KeyboardCollection();
				ActiveKeyboard = new KeyboardDescriptionNull();
			}

			#region Disposable stuff
#if DEBUG
			/// <summary/>
			~KeyboardControllerImpl()
			{
				Dispose(false);
			}
#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing,
					"****** Missing Dispose() call for " + GetType() + ". *******");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					if (Adaptors != null)
					{
						foreach (var adaptor in Adaptors)
							adaptor.Close();
						Adaptors = null;
					}
				}
				IsDisposed = true;
			}
			#endregion

			public IKeyboardDescription GetKeyboard(string layoutNameWithLocale)
			{
				if (string.IsNullOrEmpty(layoutNameWithLocale))
					return KeyboardDescription.Zero;

				return Keyboards.Contains(layoutNameWithLocale) ?
					Keyboards[layoutNameWithLocale] : KeyboardDescription.Zero;
			}

			public IKeyboardDescription GetKeyboard(string layoutName, string locale)
			{
				if (string.IsNullOrEmpty(layoutName) && string.IsNullOrEmpty(locale))
					return KeyboardDescription.Zero;

				return Keyboards.Contains(layoutName, locale) ?
					Keyboards[layoutName, locale] : KeyboardDescription.Zero;
			}

			/// <summary>
			/// Tries to get the keyboard for the specified <paramref name="writingSystem"/>.
			/// </summary>
			/// <returns>
			/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
			/// </returns>
			public IKeyboardDescription GetKeyboard(IWritingSystem writingSystem)
			{
				// TODO: use writingSystem.LocalKeyboard
				if (writingSystem == null)
					return KeyboardDescription.Zero;
				return GetKeyboard(writingSystem.Keyboard);
			}

			/// <summary>
			/// Sets the keyboard.
			/// </summary>
			/// <param name='layoutName'>Keyboard layout name</param>
			public void SetKeyboard(string layoutName)
			{
				SetKeyboard(GetKeyboard(layoutName));
			}

			public void SetKeyboard(string layoutName, string locale)
			{
				SetKeyboard(GetKeyboard(layoutName, locale));
			}

			public void SetKeyboard(IWritingSystem writingSystem)
			{
				// TODO: use writingSystem.LocalKeyboard
				SetKeyboard(writingSystem.Keyboard);
			}

			public void SetKeyboard(IKeyboardDescription keyboard)
			{
				if (ActiveKeyboard.Id != keyboard.Id)
				{
					ActiveKeyboard.Deactivate();
					keyboard.Activate();
				}
			}

			/// <summary>
			/// Gets the installed keyboard layouts/languages.
			/// </summary>
			public List<IKeyboardDescription> InstalledKeyboards
			{
				get
				{
					return Keyboards.ToList();
				}
			}

			/// <summary>
			/// List of keyboard layouts that either gave an exception or other error trying to
			/// get more information. We don't have enough information for these keyboard layouts
			/// to include them in the list of installed keyboards.
			/// </summary>
			public List<IKeyboardErrorDescription> ErrorKeyboards
			{
				get
				{
					if (Adaptors == null)
						Manager.Reset();

					return Adaptors.SelectMany(adaptor => adaptor.ErrorKeyboards).ToList();
				}
			}

			/// <summary>
			/// Gets or sets the currently active keyboard
			/// </summary>
			public IKeyboardDescription ActiveKeyboard { get; set; }
		}
		#endregion

		#region Static methods and properties
		/// <summary>
		/// Create an instance of IKeyboardController. This gets called if SingletonsContainer
		/// doesn't already contain one.
		/// </summary>
		private static IKeyboardController Create()
		{
			var controller = new KeyboardControllerImpl();
			if (Adaptors == null)
				Manager.Reset();

#if __MonoCS__
			var keyboardHelper = new LinuxKeyboardHelper();
			controller.InternalEventHandler = keyboardHelper;
			controller.InternalMethods = keyboardHelper;
#else
			// On Windows the Views code implements unmanaged VwTextSource
			// so that we don't need the InternalEventHandler/InternalMethods.
			// It is implemented in unmanaged code for performance reasons
			// because it interacts with TSF.
#endif

			return controller;
		}

		/// <summary>
		/// Gets the current keyboard controller singleton.
		/// </summary>
		private static IKeyboardController Instance
		{
			get
			{
				return SingletonsContainer.Get(() => Create(),
					() => Manager.InitializeAdaptors());
			}
		}

		/// <summary>
		/// Gets the event handler that processes events that are forwarded to the keyboard.
		/// </summary>
		public static IKeyboardEventHandler EventHandler
		{
			get
			{
				return Instance.InternalEventHandler;
			}
		}

		/// <summary>
		/// Gets the object that implements additional methods.
		/// </summary>
		public static IKeyboardMethods Methods
		{
			get
			{
				return Instance.InternalMethods;
			}
		}

		/// <summary>
		/// Gets the installed keyboard layouts/languages.
		/// </summary>
		public static List<IKeyboardDescription> InstalledKeyboards
		{
			get
			{
				return Instance.InstalledKeyboards;
			}
		}

		/// <summary>
		/// List of keyboard layouts that either gave an exception or other error trying to
		/// get more information. We don't have enough information for these keyboard layouts
		/// to include them in the list of installed keyboards.
		/// </summary>
		public static List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get
			{
				return Instance.ErrorKeyboards;
			}
		}

		/// <summary>
		/// Tries to get the keyboard specified by <paramref name="layoutName"/>.
		/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
		/// </summary>
		public static IKeyboardDescription GetKeyboard(string layoutName)
		{
			return Instance.GetKeyboard(layoutName);
		}

		public static IKeyboardDescription GetKeyboard(string layoutName, string locale)
		{
			return Instance.GetKeyboard(layoutName, locale);
		}

		public static IKeyboardDescription GetKeyboard(IWritingSystem writingSystem)
		{
			return Instance.GetKeyboard(writingSystem);
		}

		public static void SetKeyboard(IKeyboardDescription keyboard)
		{
			Instance.SetKeyboard(keyboard);
		}

		public static void SetKeyboard(string layoutName)
		{
			Instance.SetKeyboard(layoutName);
		}

		public static void SetKeyboard(IWritingSystem writingSystem)
		{
			Instance.SetKeyboard(writingSystem);
		}

		/// <summary>
		/// Gets or sets the available keyboard adaptors.
		/// </summary>
		internal static IKeyboardAdaptor[] Adaptors { get; private set; }

		/// <summary>
		/// Gets the active keyboard
		/// </summary>
		public static IKeyboardDescription ActiveKeyboard
		{
			get { return Instance.ActiveKeyboard; }
			internal set { Instance.ActiveKeyboard = value; }
		}

		#endregion

	}
}
