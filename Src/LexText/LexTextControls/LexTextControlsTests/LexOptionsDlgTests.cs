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
				// UIMode flips live (RecordEditView settles and re-resolves on the spot) — no restart needed.
				Assert.That(dlg.RestartPromptCount, Is.EqualTo(0));
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

		/// <summary>
		/// Regression test for a "double-shift" layout bug: InitializeUIModeControls() injects a new
		/// GroupBox into an already-laid-out Interface tab and must grow the Form to make room. Controls
		/// below the injection point that are top-anchored (label4, m_autoOpenCheckBox) don't move on
		/// their own, so the code moves them by hand. Controls that are bottom-anchored (tabControl1,
		/// the OK/Cancel/Help buttons) already move/resize automatically as a side effect of Form.Height
		/// growing — a prior version of this code ALSO manually re-added the same delta to those, which
		/// shifts them by 2x delta and can push the buttons below the visible (non-scrollable) client
		/// area. This pins the "exactly once" invariant against the dialog's own designer-time positions
		/// (read from LexOptionsDlg.resx), so re-introducing the double shift fails this test.
		/// </summary>
		[Test]
		public void InitializeUIModeControls_ShiftsTopAnchoredControlsOnce_NotDoubleShiftingBottomAnchoredControls()
		{
			using (var dlg = new TestableLexOptionsDlg())
			{
				var uiModeGroup = FindControlRecursive(dlg, "m_uiModeGroup");
				var label4 = FindControlRecursive(dlg, "label4");
				var autoOpenCheckBox = FindControlRecursive(dlg, "m_autoOpenCheckBox");
				var tabControl1 = FindControlRecursive(dlg, "tabControl1");
				var btnOK = FindControlRecursive(dlg, "m_btnOK");
				var btnCancel = FindControlRecursive(dlg, "m_btnCancel");
				var btnHelp = FindControlRecursive(dlg, "m_btnHelp");

				Assert.That(uiModeGroup, Is.Not.Null);
				Assert.That(label4, Is.Not.Null);
				Assert.That(autoOpenCheckBox, Is.Not.Null);
				Assert.That(tabControl1, Is.Not.Null);
				Assert.That(btnOK, Is.Not.Null);
				Assert.That(btnCancel, Is.Not.Null);
				Assert.That(btnHelp, Is.Not.Null);

				var originalLabel4Top = ReadLexOptionsDlgResxPoint("label4.Location").Y;
				var originalAutoOpenTop = ReadLexOptionsDlgResxPoint("m_autoOpenCheckBox.Location").Y;
				var originalTabControlHeight = ReadLexOptionsDlgResxSize("tabControl1.Size").Height;
				var originalBtnOkTop = ReadLexOptionsDlgResxPoint("m_btnOK.Location").Y;
				var originalBtnCancelTop = ReadLexOptionsDlgResxPoint("m_btnCancel.Location").Y;
				var originalBtnHelpTop = ReadLexOptionsDlgResxPoint("m_btnHelp.Location").Y;

				// The delta InitializeUIModeControls() computed to make room for the injected group box,
				// derived from the group box's own (live) bottom edge rather than hardcoded, so this test
				// doesn't need updating if m_uiModeGroup's own size/position ever changes.
				var delta = uiModeGroup.Bottom + 8 - originalLabel4Top;
				Assert.That(delta, Is.GreaterThan(0),
					"precondition: the injected group box must actually require extra room for this test to exercise the shift logic");

				// Top-anchored controls: moved by hand, must shift by exactly delta.
				Assert.That(label4.Top, Is.EqualTo(originalLabel4Top + delta));
				Assert.That(autoOpenCheckBox.Top, Is.EqualTo(originalAutoOpenTop + delta));

				// Bottom-anchored controls: WinForms repositions/resizes these automatically as a side
				// effect of Height growing. They must NOT also be moved by hand -- that's the double-shift
				// bug -- so each should reflect exactly one delta, not two.
				Assert.That(tabControl1.Height, Is.EqualTo(originalTabControlHeight + delta),
					"tabControl1 is Top+Bottom anchored: it should grow by exactly delta via the anchor engine, not be resized by hand");
				Assert.That(btnOK.Top, Is.EqualTo(originalBtnOkTop + delta),
					"m_btnOK is Bottom-anchored: it should move by exactly delta via the anchor engine (a hand-added second delta is the double-shift bug)");
				Assert.That(btnCancel.Top, Is.EqualTo(originalBtnCancelTop + delta));
				Assert.That(btnHelp.Top, Is.EqualTo(originalBtnHelpTop + delta));

				// The concrete symptom the bug produces: buttons pushed below the visible (FixedDialog,
				// non-scrollable) client area.
				Assert.That(btnOK.Bottom, Is.LessThanOrEqualTo(dlg.ClientSize.Height));
				Assert.That(btnCancel.Bottom, Is.LessThanOrEqualTo(dlg.ClientSize.Height));
				Assert.That(btnHelp.Bottom, Is.LessThanOrEqualTo(dlg.ClientSize.Height));
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
				var betaWarning = (Label)FindControlRecursive(dlg, "m_uiModeBetaWarning");

				Assert.That(group, Is.Not.Null);
				Assert.That(label, Is.Not.Null);
				Assert.That(combo, Is.Not.Null);
				Assert.That(betaWarning, Is.Not.Null);

				Assert.That(group.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeGroupTitle")));
				Assert.That(label.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeLabel")));
				Assert.That(combo.Items[0].ToString(), Is.EqualTo(ReadLexTextControlsResx("UiModeLegacy")));
				Assert.That(combo.Items[1].ToString(), Is.EqualTo(ReadLexTextControlsResx("UiModeNew")));
				Assert.That(betaWarning.Text, Is.EqualTo(ReadLexTextControlsResx("UiModeBetaWarning")));
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

		/// <summary>The dialog's own designer-time positions (as opposed to LexTextControls.resx's
		/// translatable strings), used to pin the layout-shift invariant against real pre-shift values.</summary>
		private static System.Drawing.Point ReadLexOptionsDlgResxPoint(string key) =>
			(System.Drawing.Point)ReadLexOptionsDlgResxValue(key);

		private static System.Drawing.Size ReadLexOptionsDlgResxSize(string key) =>
			(System.Drawing.Size)ReadLexOptionsDlgResxValue(key);

		private static object ReadLexOptionsDlgResxValue(string key)
		{
			var resxPath = System.IO.Path.GetFullPath(
				System.IO.Path.Combine(
					TestContext.CurrentContext.TestDirectory,
					"..",
					"..",
					"Src",
					"LexText",
					"LexTextControls",
					"LexOptionsDlg.resx"));

			using (var reader = new ResXResourceReader(resxPath))
			{
				foreach (System.Collections.DictionaryEntry entry in reader)
				{
					if (string.Equals(entry.Key as string, key, StringComparison.Ordinal))
						return entry.Value;
				}
			}

			Assert.Fail("Missing LexOptionsDlg.resx key: " + key);
			return null;
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
