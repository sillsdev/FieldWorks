// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Retargets Semi's CheckBox/RadioButton size tokens to FieldWorks' compact density. Semi
	/// reads those sizes from overridable DynamicResources, so an Application-level resource
	/// override is enough and no replacement ControlTheme is needed.
	/// </summary>
	public static class FwSemiDensity
	{
		/// <summary>Sets the Semi CheckBox/RadioButton size tokens on <paramref name="app"/>'s
		/// resources.</summary>
		public static void ApplyTo(Application app)
		{
			var box = FwAvaloniaDensity.CheckboxBoxSize;
			app.Resources["CheckBoxBoxWidth"] = box;
			app.Resources["CheckBoxBoxHeight"] = box;
			// Semi's ratio is 1:1 -- the padding is baked into the check-glyph geometry, not the
			// box -- so matching it keeps the glyph inside the box.
			app.Resources["CheckBoxBoxGlyphWidth"] = box;
			app.Resources["CheckBoxBoxGlyphHeight"] = box;

			var radio = FwAvaloniaDensity.RadioBoxSize;
			app.Resources["RadioButtonIconRadius"] = radio;
			// 0.45, not Semi's 6/16 = 0.375: the ring shrinks 16 to 14, so 0.45 holds the dot
			// near Semi's absolute 6 (6.3); 0.375 would give 5.25.
			app.Resources["RadioButtonGlyphRadius"] = radio * 0.45;
		}
	}
}
