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
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.Utils;
#if __MonoCS__
using SIL.FieldWorks.Views;
using SIL.FieldWorks.Common.Keyboarding.Linux;
#else
using SIL.FieldWorks.Common.Keyboarding.Windows;
#endif

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
			public static void SetKeyboardAdaptors(IKeyboardAdaptor[] adaptors)
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
					new WinKeyboardAdaptor(), new KeymanKeyboardAdapter()
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
			/// <param name='lcid'>Identifier of the language/keyboard layout (LCID), or hash code
			/// of the keyboard name.</param>
			/// <param name='description'>Keyboard description object</param>
			public static void RegisterKeyboard(int lcid, IKeyboardDescription description)
			{
				Debug.Assert(!Instance.Keyboards.ContainsKey(lcid),
					String.Format("KeyboardController.Manager.RegisterKeyboard called with duplicate keyboard lcid '{0}', with description '{1}'.", lcid, description));

				Instance.Keyboards[lcid] = description;
			}
		}
		#endregion

		#region Class KeyboardControllerImpl
		private class KeyboardControllerImpl: IKeyboardController, IDisposable
		{
			public Dictionary<int, IKeyboardDescription> Keyboards { get; private set; }
			public IKeyboardEventHandler InternalEventHandler { get; set; }
			public IKeyboardMethods InternalMethods { get; set; }

			public KeyboardControllerImpl()
			{
				Keyboards = new Dictionary<int, IKeyboardDescription>();
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
			protected virtual void Dispose(bool fDisposing)
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

			/// <summary>
			/// Tries to get the keyboard specified by <paramref name="otherImKeyboard"/> or (if
			/// not found) <paramref name="lcid"/>. Returns <c>KeyboardDescription.Zero</c> if
			/// no keyboard can be found.
			/// </summary>
			public IKeyboardDescription GetKeyboard(int? lcid, string otherImKeyboard)
			{
				IKeyboardDescription systemKeyboard = null;
				IKeyboardDescription otherKeyboard = null;
				if (lcid.HasValue)
					Keyboards.TryGetValue(lcid.Value, out systemKeyboard);

				if (!string.IsNullOrEmpty(otherImKeyboard))
					Keyboards.TryGetValue(otherImKeyboard.GetHashCode(), out otherKeyboard);

				if (otherKeyboard != null && systemKeyboard != null)
					return new KeyboardDescriptionWrapper(systemKeyboard, otherKeyboard);
				return systemKeyboard ?? otherKeyboard ?? KeyboardDescription.Zero;
			}

			public IKeyboardDescription GetKeyboard(int lcid)
			{
				return GetKeyboard(lcid, null);
			}

			public IKeyboardDescription GetKeyboard(string otherImKeyboard)
			{
				return GetKeyboard(null, otherImKeyboard);
			}

			/// <summary>
			/// Sets the keyboard.
			/// </summary>
			/// <param name='lcid'>Keyboard identifier of system keyboard</param>
			/// <param name='otherImKeyboard'>Identifier for other input method keyboard (Keyman/ibus)
			/// </param>
			/// <param name='nActiveLangId'>The active keyboard lcid.</param>
			/// <param name='activeOtherImKeyboard'>Active other input method keyboard.</param>
			/// <param name='fSelectLangPending'></param>
			public void SetKeyboard(int lcid, string otherImKeyboard, ref int nActiveLangId,
				ref string activeOtherImKeyboard, ref bool fSelectLangPending)
			{
				var keyboard = GetKeyboard(lcid, otherImKeyboard);
				keyboard.Activate();
				nActiveLangId = lcid;
				activeOtherImKeyboard = otherImKeyboard;
				fSelectLangPending = true;
			}

			/// <summary>
			/// Gets the installed keyboard layouts/languages.
			/// </summary>
			public List<IKeyboardDescription> InstalledKeyboards
			{
				get
				{
					return Keyboards.Values.ToList();
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
			// TODO: No keyboard event handler class implemented on Windows
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
		/// Tries to get the keyboard specified by <paramref name="otherImKeyboard"/> or (if not
		/// found) <paramref name="lcid"/>. Returns <c>KeyboardDescription.Zero</c> if no
		/// keyboard can be found.
		/// </summary>
		public static IKeyboardDescription GetKeyboard(int? lcid, string otherImKeyboard)
		{
			return Instance.GetKeyboard(lcid, otherImKeyboard);
		}

		/// <summary>
		/// Tries to get the keyboard specified by <paramref name="lcid"/>.
		/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
		/// </summary>
		public static IKeyboardDescription GetKeyboard(int lcid)
		{
			return Instance.GetKeyboard(lcid);
		}

		/// <summary>
		/// Tries to get the keyboard specified by <paramref name="otherImKeyboard"/>.
		/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
		/// </summary>
		public static IKeyboardDescription GetKeyboard(string otherImKeyboard)
		{
			return Instance.GetKeyboard(otherImKeyboard);
		}

		/// <summary>
		/// Sets the keyboard.
		/// </summary>
		/// <param name='lcid'>Keyboard identifier of system keyboard</param>
		/// <param name='otherImKeyboard'>Identifier for other input method keyboard (Keyman/ibus)
		/// </param>
		/// <param name='nActiveLangId'>The active keyboard lcid.</param>
		/// <param name='activeOtherImKeyboard'>Active other input method keyboard.</param>
		/// <param name='fSelectLangPending'></param>
		public static void SetKeyboard(int lcid, string otherImKeyboard, ref int nActiveLangId,
			ref string activeOtherImKeyboard, ref bool fSelectLangPending)
		{
			Instance.SetKeyboard(lcid, otherImKeyboard, ref nActiveLangId,
				ref activeOtherImKeyboard, ref fSelectLangPending);
		}

		/// <summary>
		/// Gets or sets the available keyboard adaptors.
		/// </summary>
		private static IKeyboardAdaptor[] Adaptors { get; set; }
		#endregion

	}
}
