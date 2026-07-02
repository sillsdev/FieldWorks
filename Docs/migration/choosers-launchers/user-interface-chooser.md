# User Interface Chooser (`UserInterfaceChooser`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Widgets.UserInterfaceChooser` (`Src/Common/Controls/Widgets/UserInterfaceChooser.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | chooser |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (owned control on a Tools/Options tab) |
| **JIRA** | LT-XXXXX |

## What it is
A `ComboBox` subclass for a Tools/Options tab that lists the writing systems into which the program UI has been (at least partially) localized, each language name shown in its own language and script.

## Notes / gotchas
- It is an owned control (combobox), not a dialog — embeds in the Options dialog tab, so it migrates as a control on the OptionsDialog surface rather than a standalone screen.
- Items are `LanguageDisplayItem`s; display strings are localized UI language names — localization-sensitive.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
