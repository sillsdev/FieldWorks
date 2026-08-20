// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Retargets Semi's CheckBox/RadioButton size tokens to FieldWorks' compact density. Unlike
	/// Fluent (which
	/// hardcoded those sizes as template LOCAL values, requiring a whole replacement
	/// ControlTheme), Semi reads
	/// them from overridable DynamicResources, so an Application-level resource override is
	/// enough.
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
			// Semi's own default ratio is 1:1 (glyph fills the box) because the padding is baked
			// into the
			// check-glyph geometry itself, not the box; matching that ratio here keeps the glyph
			// inside the box.
			app.Resources["CheckBoxBoxGlyphWidth"] = box;
			app.Resources["CheckBoxBoxGlyphHeight"] = box;

			var radio = FwAvaloniaDensity.RadioBoxSize;
			app.Resources["RadioButtonIconRadius"] = radio;
			// NOT the same value as IconRadius: IconRadius is the outer ring, GlyphRadius is the
			// inner checked
			// dot (Semi default ratio ~0.375) -- setting them equal would make a checked radio a
			// solid disc.
			app.Resources["RadioButtonGlyphRadius"] = radio * 0.45;
		}
	}
}
