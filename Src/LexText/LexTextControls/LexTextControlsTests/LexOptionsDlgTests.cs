using System;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.Settings;
using XCore;

namespace LexTextControlsTests
{
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LexOptionsDlgTests
	{
		[Test]
		public void OkClick_SavesUIModeAndMirrorsItIntoPropertyTable()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new TestableLexOptionsDlg())
			{
				// Use the existing bare-bones path, then inject the test doubles.
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				var combo = (ComboBox)FindControlRecursive(dlg, "m_uiModeChooser");
				Assert.That(combo, Is.Not.Null);
				combo.SelectedIndex = 1; // New

				InvokeOk(dlg);

				Assert.That(settings.UIMode, Is.EqualTo("New"));
				Assert.That(settings.SaveCalls, Is.EqualTo(1));
				Assert.That(propertyTable.GetStringProperty("UIMode", "Legacy"), Is.EqualTo("New"));
				Assert.That(dlg.RestartPromptCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void OkClick_LeavesLegacyWhenUserDoesNotChangeSelection()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new TestableLexOptionsDlg())
			{
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				InvokeOk(dlg);

				Assert.That(settings.UIMode, Is.EqualTo("Legacy"));
				Assert.That(settings.SaveCalls, Is.EqualTo(1));
				Assert.That(propertyTable.GetStringProperty("UIMode", "Legacy"), Is.EqualTo("Legacy"));
				Assert.That(dlg.RestartPromptCount, Is.EqualTo(0));
			}
		}

		[Test]
		public void RestartToApplyButton_EnablesOnlyWhenUIModeChanges()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new TestableLexOptionsDlg())
			{
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				var combo = (ComboBox)FindControlRecursive(dlg, "m_uiModeChooser");
				var restartButton = (Button)FindControlRecursive(dlg, "m_restartToApplyButton");
				Assert.That(combo, Is.Not.Null);
				Assert.That(restartButton, Is.Not.Null);
				Assert.That(restartButton.Enabled, Is.False);

				combo.SelectedIndex = 1;
				Assert.That(restartButton.Enabled, Is.True);

				combo.SelectedIndex = 0;
				Assert.That(restartButton.Enabled, Is.False);
			}
		}

		[Test]
		public void RestartToApplyButton_ClickSavesChangedUIMode()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new TestableLexOptionsDlg())
			{
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				var combo = (ComboBox)FindControlRecursive(dlg, "m_uiModeChooser");
				var restartButton = (Button)FindControlRecursive(dlg, "m_restartToApplyButton");
				Assert.That(combo, Is.Not.Null);
				Assert.That(restartButton, Is.Not.Null);

				combo.SelectedIndex = 1;
				InvokeRestartToApply(dlg);

				Assert.That(settings.UIMode, Is.EqualTo("New"));
				Assert.That(settings.SaveCalls, Is.EqualTo(1));
				Assert.That(propertyTable.GetStringProperty("UIMode", "Legacy"), Is.EqualTo("New"));
				Assert.That(dlg.RestartPromptCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void UIModeControls_ReadDisplayTextFromResx()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new TestableLexOptionsDlg())
			{
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				var group = (GroupBox)FindControlRecursive(dlg, "m_uiModeGroup");
				var label = (Label)FindControlRecursive(dlg, "m_uiModeLabel");
				var combo = (ComboBox)FindControlRecursive(dlg, "m_uiModeChooser");
				var restartButton = (Button)FindControlRecursive(dlg, "m_restartToApplyButton");

				Assert.That(group, Is.Not.Null);
				Assert.That(label, Is.Not.Null);
				Assert.That(combo, Is.Not.Null);
				Assert.That(restartButton, Is.Not.Null);

				Assert.That(group.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeGroupTitle")));
				Assert.That(label.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeLabel")));
				Assert.That(combo.Items[0].ToString(), Is.EqualTo(ReadLexTextControlsResx("UiModeLegacy")));
				Assert.That(combo.Items[1].ToString(), Is.EqualTo(ReadLexTextControlsResx("UiModeNew")));
				Assert.That(restartButton.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeRestartToApply")));
				Assert.That(ReadLexTextControlsResx("RestartToForSettingsToTakeEffect_Title"), Is.Not.Empty);
				Assert.That(ReadLexTextControlsResx("RestartToForSettingsToTakeEffect_Content"), Is.Not.Empty);
			}
		}

		private static void InvokeOk(Form dlg)
		{
			var method = FindMethod(dlg.GetType(), "m_btnOK_Click");
			Assert.That(method, Is.Not.Null);
			method.Invoke(dlg, new object[] { null, EventArgs.Empty });
		}

		private static void InvokeOnLoad(Form dlg)
		{
			var method = FindMethod(dlg.GetType(), "OnLoad");
			Assert.That(method, Is.Not.Null);
			method.Invoke(dlg, new object[] { EventArgs.Empty });
		}

		private static void InvokeRestartToApply(Form dlg)
		{
			var method = FindMethod(dlg.GetType(), "m_restartToApplyButton_Click");
			Assert.That(method, Is.Not.Null);
			method.Invoke(dlg, new object[] { null, EventArgs.Empty });
		}

		private static TrackingTestFwApplicationSettings CreateSettings(string uiMode)
		{
			return new TrackingTestFwApplicationSettings
			{
				UIMode = uiMode,
				Reporting = new ReportingSettings(),
				Update = new UpdateSettings
				{
					Behavior = UpdateSettings.Behaviors.DoNotCheck,
					Channel = UpdateSettings.Channels.Stable
				}
			};
		}

		private static Control FindControlRecursive(Control root, string name)
		{
			if (root == null)
				return null;
			if (root.Name == name)
				return root;
			foreach (Control child in root.Controls)
			{
				var found = FindControlRecursive(child, name);
				if (found != null)
					return found;
			}
			return null;
		}

		private static void SetPrivateField(object target, string fieldName, object value)
		{
			var field = FindField(target.GetType(), fieldName);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			field.SetValue(target, value);
		}

		private static FieldInfo FindField(Type type, string fieldName)
		{
			while (type != null)
			{
				var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (field != null)
					return field;
				type = type.BaseType;
			}

			return null;
		}

		private static MethodInfo FindMethod(Type type, string methodName)
		{
			while (type != null)
			{
				var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (method != null)
					return method;
				type = type.BaseType;
			}

			return null;
		}

		private static string ReadLexTextControlsResx(string key)
		{
			var resxPath = System.IO.Path.GetFullPath(
				System.IO.Path.Combine(
					TestContext.CurrentContext.TestDirectory,
					"..",
					"..",
					"Src",
					"LexText",
					"LexTextControls",
					"LexTextControls.resx"));

			using (var reader = new ResXResourceReader(resxPath))
			{
				foreach (System.Collections.DictionaryEntry entry in reader)
				{
					if (string.Equals(entry.Key as string, key, StringComparison.Ordinal))
						return entry.Value as string ?? string.Empty;
				}
			}

			Assert.Fail("Missing LexTextControls.resx key: " + key);
			return string.Empty;
		}

		private sealed class TrackingTestFwApplicationSettings : TestFwApplicationSettings
		{
			public int SaveCalls { get; private set; }

			public override void Save()
			{
				SaveCalls++;
			}
		}

		private sealed class TestableLexOptionsDlg : SIL.FieldWorks.LexText.Controls.LexOptionsDlg
		{
			public int RestartPromptCount { get; private set; }

			protected override void ShowRestartRequiredPrompt()
			{
				RestartPromptCount++;
			}
		}
	}
}
