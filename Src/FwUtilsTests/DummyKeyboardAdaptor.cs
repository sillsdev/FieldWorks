// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Keyboarding;
using SIL.ObjectModel;
using SIL.Windows.Forms.Keyboarding;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	public class DummyKeyboardAdaptor : DisposableBase, IKeyboardRetrievingAdaptor, IKeyboardSwitchingAdaptor
	{
		/// <summary />
		public DummyKeyboardAdaptor()
		{
			DefaultKeyboard = new KeyboardDescription("en_US", "US", "US", "en", true, this);
		}

		/// <summary />
		public void Initialize()
		{
		}

		/// <summary />
		public void UpdateAvailableKeyboards()
		{
		}

		/// <summary />
		public bool ActivateKeyboard(KeyboardDescription keyboard)
		{
			return true;
		}

		/// <summary />
		public void DeactivateKeyboard(KeyboardDescription keyboard)
		{
		}

		/// <summary />
		public KeyboardDescription CreateKeyboardDefinition(string id)
		{
			var parts = id.Split('_');
			return new KeyboardDescription(id, parts[1], parts[1], parts[0], false, this);
		}

		/// <summary />
		public bool CanHandleFormat(KeyboardFormat format)
		{
			return true;
		}

		/// <summary />
		public Action GetKeyboardSetupAction()
		{
			return () =>
			{
				throw new NotSupportedException();
			};
		}

		/// <summary />
		public KeyboardDescription DefaultKeyboard { get; }

		/// <summary />
		public KeyboardDescription ActiveKeyboard => null;

		/// <summary />
		public KeyboardAdaptorType Type => KeyboardAdaptorType.System;

		/// <summary />
		public bool IsApplicable => true;

		/// <summary />
		public IKeyboardSwitchingAdaptor SwitchingAdaptor => this;

		/// <summary />
		public bool IsSecondaryKeyboardSetupApplication => false;

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}