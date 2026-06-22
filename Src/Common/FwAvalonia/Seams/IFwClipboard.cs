// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// The cross-framework clipboard payload (task 3.13). Two lanes, written/read together:
	/// - <see cref="PlainText"/>: the plain-text fallback every external consumer gets
	///   (legacy writes it NFC-normalized to the OS <c>UnicodeText</c> format).
	/// - <see cref="RichXml"/>: the FieldWorks rich lane — the TsString XML representation
	///   (<c>TsStringUtils.GetXmlRep</c>), which is exactly what legacy native-Views copy/paste already
	///   puts on the OS clipboard inside the <c>"TsString"</c> data format. Preserves writing-system
	///   runs, styles, and string properties.
	///
	/// Fidelity decision (task 3.13): the shared format IS the existing legacy <c>"TsString"</c>
	/// clipboard contract, not a new one, so Avalonia and native-Views surfaces round-trip multi-WS
	/// rich text bidirectionally during coexistence. What does NOT round-trip:
	/// - embedded-object runs (ORCs: pictures, footnotes, object links) reference objects in the source
	///   context; the text and properties survive but the object reference is not resolvable outside it;
	/// - external (non-FieldWorks) consumers only see the plain-text lane — WS/style metadata drops;
	/// - paragraph-level structure beyond a single TsString is out of scope for this seam.
	/// </summary>
	public sealed class FwClipboardText
	{
		public FwClipboardText(string plainText, string richXml = null)
		{
			PlainText = plainText ?? string.Empty;
			RichXml = richXml;
		}

		/// <summary>The plain-text lane (always present; possibly empty).</summary>
		public string PlainText { get; }

		/// <summary>The FieldWorks rich lane (TsString XML), or null when only plain text is available.</summary>
		public string RichXml { get; }
	}

	/// <summary>
	/// The shared clipboard seam (task 3.13). The product implementation (<c>FwTsStringClipboard</c> in
	/// xWorks) speaks the legacy <c>"TsString"</c> + <c>UnicodeText</c> OS clipboard formats so legacy
	/// native-Views surfaces and Avalonia surfaces read each other's copies; this layer stays
	/// LCModel-free so Avalonia controls can consume it directly.
	/// </summary>
	public interface IFwClipboard
	{
		/// <summary>Whether any text (plain or rich) is available.</summary>
		bool ContainsText();

		/// <summary>Reads the current clipboard text, or null when the clipboard has no text.</summary>
		FwClipboardText GetText();

		/// <summary>Writes both lanes of the payload.</summary>
		void SetText(FwClipboardText payload);
	}

	/// <summary>In-memory implementation for tests and headless previews.</summary>
	public sealed class InMemoryFwClipboard : IFwClipboard
	{
		private FwClipboardText _current;

		public bool ContainsText() => _current != null;

		public FwClipboardText GetText() => _current;

		public void SetText(FwClipboardText payload)
			=> _current = payload ?? throw new System.ArgumentNullException(nameof(payload));
	}
}
