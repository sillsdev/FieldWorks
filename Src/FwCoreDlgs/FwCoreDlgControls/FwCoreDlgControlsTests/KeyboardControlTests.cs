// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2010-11-22 KeyboardControlTests.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Keyboarding;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FwCoreDlgControlsTests
{
	/// <summary/>
	[TestFixture]
	[SetUICulture("en-US")]
	public class KeyboardControlTests: BaseTest
	{
		/// <summary>
		/// A message box stub that rembers the values that are passed in so that they can be
		/// verified in tests.
		/// </summary>
		private class RememberingMessageBox: IMessageBox
		{
			/// <summary>Gets the text that was passed in last</summary>
			public string Text { get; private set; }
			/// <summary>Gets the caption that was passed in last</summary>
			public string Caption { get; private set; }
			/// <summary>Gets the buttons that were passed in last</summary>
			public MessageBoxButtons Buttons { get; private set; }
			/// <summary>Gets the icon that was passed in last</summary>
			public MessageBoxIcon Icon { get; private set; }
			/// <summary>Gets the number of times MessageBox.Show has been called</summary>
			public int Count { get; private set; }


			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This implementation displays the message in the Console and returns the first
			/// button as dialog result.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public DialogResult Show(IWin32Window owner, string text, string caption,
				MessageBoxButtons buttons, MessageBoxIcon icon)
			{
				// When running tests, displaying a message box is usually not what we want so we
				// just write to the Console.
				// If we later change our mind we have to check Environment.UserInteractive. If it
				// is false we have to use MessageBoxOptions.ServiceNotification or
				// DefaultDesktopOnly so that it works when running from a service (build machine).
				Console.WriteLine("**** {0}: {1}{3}{2}", caption, text, buttons, Environment.NewLine);

				Text = text;
				Caption = caption;
				Buttons = buttons;
				Icon = icon;
				Count++;
				return TranslateButtons(buttons);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This implementation displays the message in the Console and returns the first
			/// button as dialog result.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public DialogResult Show(IWin32Window owner, string text, string caption,
				MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton,
				MessageBoxOptions options, string helpFilePath, HelpNavigator navigator, object param)
			{
				return Show(owner, text, caption, buttons, icon);
			}

			private static DialogResult TranslateButtons(MessageBoxButtons buttons)
			{
				switch (buttons)
				{
					case MessageBoxButtons.OK:
					case MessageBoxButtons.OKCancel:
						return DialogResult.OK;
					case MessageBoxButtons.YesNo:
					case MessageBoxButtons.YesNoCancel:
						return DialogResult.Yes;
					case MessageBoxButtons.RetryCancel:
						return DialogResult.Retry;
					case MessageBoxButtons.AbortRetryIgnore:
						return DialogResult.Abort;
					default:
						return DialogResult.OK;
				}
			}
		}

		#region DummyWritingSystem class
		/// <summary>
		/// Dummy writing system used for testing
		/// </summary>
		private class DummyWritingSystem : IWritingSystem
		{
			/// <summary></summary>
			public DummyWritingSystem(string identifier, int lcid)
			{
				Id = identifier;
				LCID = CurrentLCID = lcid;
			}

			#region IWritingSystem Members

			/// <summary></summary>
			public string Abbreviation
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public Palaso.WritingSystems.Collation.ICollator Collator
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public void Copy(IWritingSystem source)
			{
				throw new NotImplementedException();
			}

			/// <summary></summary>
			public string DisplayLabel
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string IcuLocale
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public bool IsGraphiteEnabled
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public LanguageSubtag LanguageSubtag
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string LegacyMapping
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public bool MarkedForDeletion
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string MatchedPairs
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public bool Modified
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string PunctuationPatterns
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string QuotationMarks
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public RegionSubtag RegionSubtag
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public ScriptSubtag ScriptSubtag
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string SortRules
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public Palaso.WritingSystems.WritingSystemDefinition.SortRulesType SortUsing
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string ValidChars
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public bool ValidateCollationRules(out string message)
			{
				throw new NotImplementedException();
			}

			/// <summary></summary>
			public VariantSubtag VariantSubtag
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public void WriteLdml(System.Xml.XmlWriter writer)
			{
				throw new NotImplementedException();
			}

			/// <summary></summary>
			public IWritingSystemManager WritingSystemManager
			{
				get { throw new NotImplementedException(); }
			}

			#endregion

			#region ILgWritingSystem Members

			/// <summary></summary>
			public SIL.FieldWorks.Common.COMInterfaces.ILgCharacterPropertyEngine CharPropEngine
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public int CurrentLCID { get; set; }

			/// <summary></summary>
			public string DefaultFontFeatures { get; set; }

			/// <summary></summary>
			public string DefaultFontName
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public int Handle
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string ISO3
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string Id { get; private set; }

			/// <summary></summary>
			public void InterpretChrp(ref SIL.FieldWorks.Common.COMInterfaces.LgCharRenderProps _chrp)
			{
				throw new NotImplementedException();
			}

			/// <summary></summary>
			public string Keyboard { get; set; }

			/// <summary></summary>
			public int LCID { get; set; }

			/// <summary></summary>
			public string LanguageName { get; set; }

			/// <summary></summary>
			public bool RightToLeftScript
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public string SpellCheckingId
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			/// <summary></summary>
			public IRenderEngine get_Renderer(IVwGraphics vg)
			{
				throw new NotImplementedException();
			}
			#endregion
		}
		#endregion

		#region DummyKeyboardAdaptor class
		private class DummyKeyboardAdaptor: IKeyboardAdaptor
		{
			public DummyKeyboardAdaptor()
			{
				if (DummyInstalledKeyboards == null)
					DummyInstalledKeyboards = new List<IKeyboardDescription>();
				if (DummyErrorKeyboards == null)
					DummyErrorKeyboards = new List<IKeyboardErrorDescription>();
			}

			public static void Reset()
			{
				DummyInstalledKeyboards = null;
				DummyErrorKeyboards = null;
			}

			public static List<IKeyboardDescription> DummyInstalledKeyboards { get; set; }
			public static List<IKeyboardErrorDescription> DummyErrorKeyboards { get; set; }

			public List<IKeyboardErrorDescription> ErrorKeyboards
			{
				get { return DummyErrorKeyboards; }
			}

			public void ActivateKeyboard(IKeyboardDescription keyboard,
				IKeyboardDescription systemKeyboard)
			{
				// do nothing
			}

			public void DeactivateKeyboard(IKeyboardDescription keyboard)
			{
			}

			public void Initialize()
			{
				foreach (var keyboard in DummyInstalledKeyboards)
					KeyboardController.Manager.RegisterKeyboard(keyboard.Id, keyboard);
			}

			public void Close()
			{
			}
		}
		#endregion

		private RememberingMessageBox m_MsgBox;

		[SetUp]
		public void Setup()
		{
			m_MsgBox = new RememberingMessageBox();
			MessageBoxUtils.Manager.SetMessageBoxAdapter(m_MsgBox);
			KeyboardController.Manager.Reset();
			KeyboardControl.ResetErrorMessages();
			DummyKeyboardAdaptor.Reset();
		}

		/// <summary>
		/// Get available ibus keyboards. Don't run automatically since automated test
		/// environment may not have the right keyboards set.
		/// </summary>
		[Test]
		[Category("ByHand")]
		[Platform(Include = "Linux", Reason = "Linux specific test")]
		public void GetAvailableKeyboards_GetsKeyboards()
		{
			var expectedKeyboards = new List<string>();
			expectedKeyboards.Add("ispell (m17n)");

			List<string> actualKeyboards = ReflectionHelper.CallStaticMethod("FwCoreDlgControls.dll",
				"SIL.FieldWorks.FwCoreDlgControls.KeyboardControl", "GetAvailableKeyboards",
				new object[] {null}) as List<string>;

			Assert.That(actualKeyboards, Is.EquivalentTo(expectedKeyboards),
				"Available keyboards do not match expected.");
		}

		/// <summary>
		/// Get available keyboards/languages. Don't run automatically since the installed
		/// keyboards/languages vary on different systems.
		/// </summary>
		[Test]
		[Category("ByHand")]
		[Platform(Exclude = "Linux", Reason = "Windows specific test")]
		public void InitLanguageCombo()
		{
			using (var sut = new KeyboardControl())
			{
				var ws = new DummyWritingSystem("en-US", 1033);
				// this fills the combo boxes
				sut.WritingSystem = ws;

				var combo = (ComboBox)sut.Controls["m_langIdComboBox"];
				bool found = false;
				foreach (IKeyboardDescription item in combo.Items)
				{
					Console.WriteLine("{0}: {1}", item.Id, item.Name);
					if (item.Id == 1033)
						found = true;
				}

				Assert.IsTrue(found,
					"keyboard layout combobox did not contain the 'English (United States)' keyboard");
			}
		}

		/// <summary>
		/// Get available keyboards/languages.
		/// </summary>
		[Test]
		public void InitLanguageCombo_AllOk()
		{
			DummyKeyboardAdaptor.DummyInstalledKeyboards = new List<IKeyboardDescription>(new []
				{
					new KeyboardDescription(1033, "English (United States)", null),
					new KeyboardDescription(1031, "German (Germany)", null)
				});
			KeyboardController.Manager.SetKeyboardAdaptors(new [] { new DummyKeyboardAdaptor() });

			using (var sut = new KeyboardControl())
			{
				var ws = new DummyWritingSystem("en-US", 1033);
				// this fills the combo boxes
				sut.WritingSystem = ws;

				var combo = (ComboBox)sut.Controls["m_langIdComboBox"];
				CollectionAssert.AreEqual(DummyKeyboardAdaptor.DummyInstalledKeyboards, combo.Items);
				Assert.AreEqual(2, combo.Items.Count);
				Assert.AreEqual(0, m_MsgBox.Count);
			}
		}

		/// <summary>
		/// Get available keyboards/languages when we get some errors.
		/// </summary>
		[Test]
		public void InitLanguageCombo_Errors()
		{
			DummyKeyboardAdaptor.DummyInstalledKeyboards = new List<IKeyboardDescription>(new []
				{
					new KeyboardDescription(1033, "English (United States)", null),
					new KeyboardDescription(1031, "German (Germany)", null)
				});
			DummyKeyboardAdaptor.DummyErrorKeyboards = new List<IKeyboardErrorDescription>(new []
				{
					new KeyboardErrorDescription(1111)
				});
			KeyboardController.Manager.SetKeyboardAdaptors(new [] { new DummyKeyboardAdaptor() });
			using (var sut = new KeyboardControl())
			{
				var ws = new DummyWritingSystem("en-US", 1033);
				// this fills the combo boxes
				sut.WritingSystem = ws;

				var combo = (ComboBox)sut.Controls["m_langIdComboBox"];
				CollectionAssert.AreEqual(DummyKeyboardAdaptor.DummyInstalledKeyboards, combo.Items);
				Assert.AreEqual(2, combo.Items.Count);

				Assert.AreEqual(1, m_MsgBox.Count);
				Assert.AreEqual("The following system locales are invalid, so will be omitted from " +
				"the list of System Languages for keyboard input: 1111", m_MsgBox.Text);
				Assert.AreEqual("Error", m_MsgBox.Caption);
			}
		}
	}
}
