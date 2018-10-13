// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Keyboarding;
using SIL.ObjectModel;
using SIL.Windows.Forms.Keyboarding;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary/>
	public class DummyKeyboardAdaptor : DisposableBase, IKeyboardRetrievingAdaptor, IKeyboardSwitchingAdaptor
	{
		private readonly KeyboardDescription m_defaultKeyboard;

		/// <summary/>
		public DummyKeyboardAdaptor()
		{
			m_defaultKeyboard = new KeyboardDescription("en_US", "US", "US", "en", true, this);
		}

		/// <summary/>
		public void Initialize()
		{
		}

		/// <summary/>
		public void UpdateAvailableKeyboards()
		{
		}

		/// <summary/>
		public bool ActivateKeyboard(KeyboardDescription keyboard)
		{
			return true;
		}

		/// <summary/>
		public void DeactivateKeyboard(KeyboardDescription keyboard)
		{
		}

		/// <summary/>
		public KeyboardDescription GetKeyboardForInputLanguage(IInputLanguage inputLanguage)
		{
			return null;
		}

		/// <summary/>
		public KeyboardDescription CreateKeyboardDefinition(string id)
		{
			string[] parts = id.Split('_');
			return new KeyboardDescription(id, parts[1], parts[1], parts[0], false, this);
		}

		/// <summary/>
		public bool CanHandleFormat(KeyboardFormat format)
		{
			return true;
		}

		/// <summary/>
		public Action GetKeyboardSetupAction()
		{
			return ()=>
			{
				throw new NotImplementedException();
			};
		}

		/// <summary/>
		public KeyboardDescription DefaultKeyboard
		{
			get { return m_defaultKeyboard; }
		}

		/// <summary/>
		public KeyboardDescription ActiveKeyboard
		{
			get { return null; }
		}

		/// <summary/>
		public KeyboardAdaptorType Type
		{
			get { return KeyboardAdaptorType.System; }
		}

		/// <summary/>
		public bool IsApplicable
		{
			get { return true; }
		}

		/// <summary/>
		public IKeyboardSwitchingAdaptor SwitchingAdaptor
		{
			get { return this; }
		}

		/// <summary/>
		public bool IsSecondaryKeyboardSetupApplication
		{
			get { return false; }
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}
