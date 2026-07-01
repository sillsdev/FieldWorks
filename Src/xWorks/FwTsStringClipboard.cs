// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product cross-framework clipboard bridge (task 3.13): implements the LCModel-free
	/// <see cref="IFwClipboard"/> seam over the legacy OS clipboard contract that native-Views
	/// surfaces already speak (<c>EditingHelper.SetTsStringOnClipboard</c>/<c>GetTsStringFromClipboard</c>):
	/// the <see cref="TsStringWrapper.TsStringFormat"/> data format carrying a serialized
	/// <see cref="TsStringWrapper"/> (TsString XML rep) plus an NFC-normalized <c>UnicodeText</c>
	/// plain-text lane. Because both frameworks read and write the same formats, copy/paste
	/// round-trips bidirectionally during coexistence; see <see cref="FwClipboardText"/> for what
	/// deliberately does not round-trip (ORC object references, external consumers, paragraph structure).
	/// Goes through <see cref="ClipboardUtils"/> so tests can swap the OS clipboard for a stub.
	/// </summary>
	public sealed class FwTsStringClipboard : IFwClipboard
	{
		private const int MaxRetry = 3;
		private const int RetrySleepMs = 200;

		private readonly ILgWritingSystemFactory _writingSystemFactory;

		public FwTsStringClipboard(ILgWritingSystemFactory writingSystemFactory)
		{
			_writingSystemFactory = writingSystemFactory ?? throw new System.ArgumentNullException(nameof(writingSystemFactory));
		}

		/// <inheritdoc />
		public bool ContainsText()
		{
			return GetText() != null;
		}

		/// <inheritdoc />
		public FwClipboardText GetText()
		{
			var dataObject = ClipboardUtils.GetDataObject();
			if (dataObject == null)
				return null;

			var wrapper = dataObject.GetData(TsStringWrapper.TsStringFormat) as TsStringWrapper;
			var plain = dataObject.GetData(DataFormats.UnicodeText) as string;
			if (wrapper == null && plain == null)
				return null;
			var richText = wrapper == null ? null : RegionRichTextAdapter.FromTsString(
				TsStringSerializer.DeserializeTsStringFromXml(wrapper.Xml, _writingSystemFactory),
				_writingSystemFactory);

			return new FwClipboardText(plain ?? PlainTextFromXml(wrapper.Xml), wrapper?.Xml, richText);
		}

		/// <inheritdoc />
		public void SetText(FwClipboardText payload)
		{
			ClipboardUtils.SetDataObject(CreateDataObject(payload), true, MaxRetry, RetrySleepMs);
		}

		/// <summary>
		/// Builds the dual-lane OS data object for a payload — the same entries legacy
		/// <c>EditingHelper.SetTsStringOnClipboard</c> writes, so legacy surfaces consume the rich
		/// lane exactly as if another Views surface had produced it. Shared by the clipboard seam
		/// (3.13) and the drag-and-drop bridge (3.14), which carry identical text payloads.
		/// </summary>
		public static DataObject CreateDataObject(FwClipboardText payload)
		{
			if (payload == null)
				throw new System.ArgumentNullException(nameof(payload));

			var dataObject = new DataObject();
			if (!string.IsNullOrEmpty(payload.RichXml))
			{
				var wrapper = TsStringWrapper.FromXml(payload.RichXml);
				dataObject.SetData(TsStringWrapper.TsStringFormat, false, wrapper);
				dataObject.SetData(DataFormats.Serializable, true, wrapper);
			}

			dataObject.SetData(DataFormats.UnicodeText, true,
				(payload.PlainText ?? string.Empty).Normalize(NormalizationForm.FormC));
			return dataObject;
		}

		/// <summary>Builds the dual-lane payload from a TsString (rich + NFC plain text).</summary>
		public FwClipboardText FromTsString(ITsString tsString)
		{
			if (tsString == null)
				throw new System.ArgumentNullException(nameof(tsString));

			var plain = (tsString.Text ?? string.Empty).Normalize(NormalizationForm.FormC);
			return new FwClipboardText(plain, TsStringUtils.GetXmlRep(tsString, _writingSystemFactory, 0),
				RegionRichTextAdapter.FromTsString(tsString, _writingSystemFactory));
		}

		/// <summary>
		/// Materializes the rich lane back into a TsString (writing systems resolved/added through the
		/// factory), or null when the payload has no rich lane.
		/// </summary>
		public ITsString ToTsString(FwClipboardText payload)
		{
			if (payload?.RichXml == null)
				return null;

			return TsStringSerializer.DeserializeTsStringFromXml(payload.RichXml, _writingSystemFactory);
		}

		private string PlainTextFromXml(string xml)
		{
			var tss = TsStringSerializer.DeserializeTsStringFromXml(xml, _writingSystemFactory);
			return (tss?.Text ?? string.Empty).Normalize(NormalizationForm.FormC);
		}
	}
}
