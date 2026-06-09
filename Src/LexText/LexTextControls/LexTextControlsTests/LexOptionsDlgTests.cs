using System;
using System.Reflection;
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
			using (var dlg = new SIL.FieldWorks.LexText.Controls.LexOptionsDlg())
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
				Assert.That(propertyTable.GetStringProperty("UIMode", "Legacy"), Is.EqualTo("New"));
			}
		}

		[Test]
		public void OkClick_LeavesLegacyWhenUserDoesNotChangeSelection()
		{
			var settings = CreateSettings("Legacy");
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var dlg = new SIL.FieldWorks.LexText.Controls.LexOptionsDlg())
			{
				dlg.InitBareBones(null);
				SetPrivateField(dlg, "m_settings", settings);
				SetPrivateField(dlg, "m_propertyTable", propertyTable);
				InvokeOnLoad(dlg);

				InvokeOk(dlg);

				Assert.That(settings.UIMode, Is.EqualTo("Legacy"));
				Assert.That(propertyTable.GetStringProperty("UIMode", "Legacy"), Is.EqualTo("Legacy"));
			}
		}

		private static void InvokeOk(Form dlg)
		{
			var method = dlg.GetType().GetMethod("m_btnOK_Click", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			method.Invoke(dlg, new object[] { null, EventArgs.Empty });
		}

		private static void InvokeOnLoad(Form dlg)
		{
			var method = dlg.GetType().GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			method.Invoke(dlg, new object[] { EventArgs.Empty });
		}

		private static TestFwApplicationSettings CreateSettings(string uiMode)
		{
			return new TestFwApplicationSettings
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
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			field.SetValue(target, value);
		}
	}
}
