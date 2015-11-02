using SIL.Keyboarding;
using SIL.Windows.Forms.Keyboarding;

namespace SIL.Utils
{
	public class DummyKeyboardAdaptor : FwDisposableBase, IKeyboardRetrievingAdaptor, IKeyboardSwitchingAdaptor
	{
		private readonly KeyboardDescription m_defaultKeyboard;

		public DummyKeyboardAdaptor()
		{
			m_defaultKeyboard = new KeyboardDescription("en_US", "US", "US", "en", true, this);
		}

		public void Initialize()
		{
		}

		public void UpdateAvailableKeyboards()
		{
		}

		public bool ActivateKeyboard(KeyboardDescription keyboard)
		{
			return true;
		}

		public void DeactivateKeyboard(KeyboardDescription keyboard)
		{
		}

		public KeyboardDescription GetKeyboardForInputLanguage(IInputLanguage inputLanguage)
		{
			return null;
		}

		public KeyboardDescription CreateKeyboardDefinition(string id)
		{
			string[] parts = id.Split('_');
			return new KeyboardDescription(id, parts[1], parts[1], parts[0], false, this);
		}

		public bool CanHandleFormat(KeyboardFormat format)
		{
			return true;
		}

		public string GetKeyboardSetupApplication(out string arguments)
		{
			arguments = null;
			return null;
		}

		public KeyboardDescription DefaultKeyboard
		{
			get { return m_defaultKeyboard; }
		}

		public KeyboardDescription ActiveKeyboard
		{
			get { return null; }
		}

		public KeyboardAdaptorType Type
		{
			get { return KeyboardAdaptorType.System; }
		}

		public bool IsApplicable
		{
			get { return true; }
		}

		public IKeyboardSwitchingAdaptor SwitchingAdaptor
		{
			get { return this; }
		}

		public bool IsSecondaryKeyboardSetupApplication
		{
			get { return false; }
		}
	}
}
