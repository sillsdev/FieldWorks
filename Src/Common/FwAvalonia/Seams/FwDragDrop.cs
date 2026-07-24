// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// The cross-surface drag-and-drop payload contract (task 3.14). Both WinForms and Avalonia
	/// surfaces drag from and drop onto each other through the OS DnD pipeline using the same shared
	/// formats the clipboard seam (3.13) established:
	/// - text drags: the legacy <c>"TsString"</c> OS format (rich) + <c>UnicodeText</c> (plain) — the dual-format pair;
	/// - object moves: the <see cref="RecordKeyFormat"/> string payload defined here (a guid-based
	///   record key), framework-neutral and resolvable on either side via the LCModel object repository.
	/// In-surface reorder semantics stay surface-local; specific drag interactions land with their
	/// editors (6.x/7.x). This layer stays LCModel-free.
	/// </summary>
	public static class FwDragDropFormats
	{
		/// <summary>OS DnD/clipboard data format name for a FieldWorks record key.</summary>
		public const string RecordKeyFormat = "FieldWorks.RecordKey";
	}

	/// <summary>
	/// A framework-neutral record identity for object drags: serialized as
	/// <c>fwrecord:{guid}</c> so any surface (or a future external consumer) can parse it without
	/// binary serialization.
	/// </summary>
	public sealed class FwRecordKeyPayload
	{
		private const string Prefix = "fwrecord:";

		public FwRecordKeyPayload(Guid objectGuid)
		{
			if (objectGuid == Guid.Empty) throw new ArgumentException("record key requires a non-empty guid", nameof(objectGuid));
			ObjectGuid = objectGuid;
		}

		/// <summary>The dragged object's CmObject guid (stable across surfaces and sessions).</summary>
		public Guid ObjectGuid { get; }

		/// <summary>Serializes to the wire form carried in <see cref="FwDragDropFormats.RecordKeyFormat"/>.</summary>
		public string Serialize() => Prefix + ObjectGuid.ToString("D");

		/// <summary>Parses the wire form; false for anything that is not a well-formed record key.</summary>
		public static bool TryParse(string serialized, out FwRecordKeyPayload payload)
		{
			payload = null;
			if (string.IsNullOrEmpty(serialized) || !serialized.StartsWith(Prefix, StringComparison.Ordinal))
				return false;

			if (!Guid.TryParseExact(serialized.Substring(Prefix.Length), "D", out var guid) || guid == Guid.Empty)
				return false;

			payload = new FwRecordKeyPayload(guid);
			return true;
		}
	}
}
