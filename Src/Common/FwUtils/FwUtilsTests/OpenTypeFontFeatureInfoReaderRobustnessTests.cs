// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Confirms the reader degrades to tag-only records instead of throwing when a font's layout
	/// or name tables are truncated, misdirected, or undecodable. LT-22638.
	/// </summary>
	[TestFixture]
	public class OpenTypeFontFeatureInfoReaderRobustnessTests
	{
		[Test]
		public void NullTable_ReturnsNoFeatures()
		{
			var features = OpenTypeFontFeatureInfoReader.Read(tag => null);
			Assert.That(features, Is.Empty);
		}

		[Test]
		public void ThrowingTableSource_IsSwallowed()
		{
			Assert.That(() => OpenTypeFontFeatureInfoReader.Read(tag => throw new InvalidOperationException()),
				Throws.Nothing);
		}

		[Test]
		public void TruncatedFeatureList_ReturnsFeaturesBeforeTheBreak()
		{
			// Claim three features but only supply bytes for one record, then cut the table off.
			var gsub = new BeWriter();
			gsub.UInt16(1).UInt16(0);                 // version 1.0
			gsub.UInt16(0);                           // scriptList offset (unused)
			var featureListOffsetPos = gsub.Reserve();// featureList offset (back-patched)
			gsub.UInt16(0);                           // lookupList offset (unused)
			gsub.Patch(featureListOffsetPos, (ushort)gsub.Length);
			gsub.UInt16(3);                           // featureCount lies: says 3
			gsub.Tag("liga").UInt16(0);               // one record, offset patched below is bogus
			// Table ends here: the 2nd record read runs off the end and the reader stops safely.

			var features = Read(gsub.ToArray(), null);
			// No exception: the one parseable record is returned and reading stops at the break
			// instead of running past the end of the table.
			Assert.That(features.Select(f => f.Tag), Is.EqualTo(new[] { "liga" }));
		}

		[Test]
		public void FeatureParamsOffsetPastTableEnd_YieldsTagWithoutLabel()
		{
			var gsub = BuildGsub(new FeatureSpec("cv01", featureParamsOffset: 0x7000));
			var features = Read(gsub, null);

			var cv01 = features.Single(f => f.Tag == "cv01");
			Assert.That(cv01.FontSuppliedLabel, Is.Null);
			Assert.That(cv01.Options, Is.Empty);
		}

		[Test]
		public void NameRecordOutsideStorage_FallsBackToNoLabel()
		{
			var gsub = BuildGsub(new FeatureSpec("ss01", ssUiNameId: 256));
			var name = BuildNameTable(new NameSpec(3, 1, 0x0409, 256, "ignored", forcedLength: 4000));

			var features = Read(gsub, name);

			Assert.That(features.Single(f => f.Tag == "ss01").FontSuppliedLabel, Is.Null);
		}

		[Test]
		public void UndecodablePlatform_FallsBackToNoLabel()
		{
			var gsub = BuildGsub(new FeatureSpec("ss01", ssUiNameId: 256));
			// Platform 2 (ISO) is not decodable here; the reader must skip it, not guess.
			var name = BuildNameTable(new NameSpec(2, 0, 0, 256, "Nope"));

			var features = Read(gsub, name);

			Assert.That(features.Single(f => f.Tag == "ss01").FontSuppliedLabel, Is.Null);
		}

		[Test]
		public void ZeroLengthName_FallsBackToNoLabel()
		{
			var gsub = BuildGsub(new FeatureSpec("ss01", ssUiNameId: 256));
			var name = BuildNameTable(new NameSpec(3, 1, 0x0409, 256, ""));

			var features = Read(gsub, name);

			Assert.That(features.Single(f => f.Tag == "ss01").FontSuppliedLabel, Is.Null);
		}

		[Test]
		public void WellFormedSynthetic_ReadsLabelAndOptions()
		{
			// Positive control so the builders themselves are trustworthy.
			var gsub = BuildGsub(new FeatureSpec("cv01", cvLabelNameId: 256, cvOptionNameIds: new[] { 257, 258 }));
			var name = BuildNameTable(
				new NameSpec(3, 1, 0x0409, 256, "Alpha"),
				new NameSpec(3, 1, 0x0409, 257, "First"),
				new NameSpec(3, 1, 0x0409, 258, "Second"));

			var cv01 = Read(gsub, name).Single(f => f.Tag == "cv01");

			Assert.That(cv01.FontSuppliedLabel, Is.EqualTo("Alpha"));
			Assert.That(cv01.Options, Is.EqualTo(new[] { "First", "Second" }));
		}

		private static IReadOnlyList<OpenTypeFontFeatureInfo> Read(byte[] gsub, byte[] name)
		{
			return OpenTypeFontFeatureInfoReader.Read(tag =>
			{
				switch (tag)
				{
					case "GSUB": return gsub;
					case "name": return name;
					default: return null;
				}
			});
		}

		private sealed class FeatureSpec
		{
			public FeatureSpec(string tag, int cvLabelNameId = 0, int[] cvOptionNameIds = null,
				int ssUiNameId = 0, int featureParamsOffset = -1)
			{
				Tag = tag;
				CvLabelNameId = cvLabelNameId;
				CvOptionNameIds = cvOptionNameIds ?? Array.Empty<int>();
				SsUiNameId = ssUiNameId;
				ForcedFeatureParamsOffset = featureParamsOffset;
			}

			public string Tag { get; }
			public int CvLabelNameId { get; }
			public int[] CvOptionNameIds { get; }
			public int SsUiNameId { get; }
			public int ForcedFeatureParamsOffset { get; }
			public bool HasParams => CvLabelNameId > 0 || SsUiNameId > 0 || ForcedFeatureParamsOffset >= 0;
		}

		private static byte[] BuildGsub(params FeatureSpec[] specs)
		{
			var gsub = new BeWriter();
			gsub.UInt16(1).UInt16(0);            // version 1.0
			gsub.UInt16(0);                      // scriptList offset (unused)
			var featureListOffsetPos = gsub.Reserve();
			gsub.UInt16(0);                      // lookupList offset (unused)

			var featureListStart = gsub.Length;
			gsub.Patch(featureListOffsetPos, (ushort)featureListStart);
			gsub.UInt16((ushort)specs.Length);

			// Reserve the record offsets, then lay out feature tables after all records.
			var recordOffsetPositions = new int[specs.Length];
			for (var i = 0; i < specs.Length; i++)
			{
				gsub.Tag(specs[i].Tag);
				recordOffsetPositions[i] = gsub.Reserve();
			}

			for (var i = 0; i < specs.Length; i++)
			{
				var spec = specs[i];
				var featureTableStart = gsub.Length;
				gsub.Patch(recordOffsetPositions[i], (ushort)(featureTableStart - featureListStart));

				var paramsBlock = BuildParamsBlock(spec);
				ushort paramsOffset = 0;
				if (spec.ForcedFeatureParamsOffset >= 0)
					paramsOffset = (ushort)spec.ForcedFeatureParamsOffset;
				else if (paramsBlock != null)
					paramsOffset = 4;             // params sit right after the 4-byte feature header

				gsub.UInt16(paramsOffset);
				gsub.UInt16(0);                   // lookupIndexCount
				if (spec.ForcedFeatureParamsOffset < 0 && paramsBlock != null)
					gsub.Bytes(paramsBlock);
			}

			return gsub.ToArray();
		}

		private static byte[] BuildParamsBlock(FeatureSpec spec)
		{
			if (spec.SsUiNameId > 0)
			{
				var ss = new BeWriter();
				ss.UInt16(0);                     // version
				ss.UInt16((ushort)spec.SsUiNameId);
				return ss.ToArray();
			}
			if (spec.CvLabelNameId > 0 || spec.CvOptionNameIds.Length > 0)
			{
				var cv = new BeWriter();
				cv.UInt16(0);                     // format
				cv.UInt16((ushort)spec.CvLabelNameId);
				cv.UInt16(0);                     // tooltip
				cv.UInt16(0);                     // sample text
				cv.UInt16((ushort)spec.CvOptionNameIds.Length);
				cv.UInt16((ushort)(spec.CvOptionNameIds.FirstOrDefault()));
				cv.UInt16(0);                     // charCount
				return cv.ToArray();
			}
			return null;
		}

		private sealed class NameSpec
		{
			public NameSpec(int platformId, int encodingId, int languageId, int nameId, string value,
				int forcedLength = -1)
			{
				PlatformId = platformId;
				EncodingId = encodingId;
				LanguageId = languageId;
				NameId = nameId;
				Value = value;
				ForcedLength = forcedLength;
			}

			public int PlatformId { get; }
			public int EncodingId { get; }
			public int LanguageId { get; }
			public int NameId { get; }
			public string Value { get; }
			public int ForcedLength { get; }
		}

		private static byte[] BuildNameTable(params NameSpec[] records)
		{
			var storage = new BeWriter();
			var encoded = new List<(int offset, int length)>();
			foreach (var record in records)
			{
				var offset = storage.Length;
				var bytes = Encoding.BigEndianUnicode.GetBytes(record.Value ?? string.Empty);
				storage.Bytes(bytes);
				encoded.Add((offset, record.ForcedLength >= 0 ? record.ForcedLength : bytes.Length));
			}

			var table = new BeWriter();
			table.UInt16(0);                      // format
			table.UInt16((ushort)records.Length); // count
			var storageOffset = 6 + records.Length * 12;
			table.UInt16((ushort)storageOffset);
			for (var i = 0; i < records.Length; i++)
			{
				table.UInt16((ushort)records[i].PlatformId);
				table.UInt16((ushort)records[i].EncodingId);
				table.UInt16((ushort)records[i].LanguageId);
				table.UInt16((ushort)records[i].NameId);
				table.UInt16((ushort)encoded[i].length);
				table.UInt16((ushort)encoded[i].offset);
			}
			table.Bytes(storage.ToArray());
			return table.ToArray();
		}

		private sealed class BeWriter
		{
			private readonly List<byte> m_bytes = new List<byte>();

			public int Length => m_bytes.Count;

			public BeWriter UInt16(int value)
			{
				m_bytes.Add((byte)((value >> 8) & 0xFF));
				m_bytes.Add((byte)(value & 0xFF));
				return this;
			}

			public BeWriter Tag(string tag)
			{
				m_bytes.AddRange(Encoding.ASCII.GetBytes(tag));
				return this;
			}

			public BeWriter Bytes(byte[] bytes)
			{
				m_bytes.AddRange(bytes);
				return this;
			}

			public int Reserve()
			{
				var position = m_bytes.Count;
				m_bytes.Add(0);
				m_bytes.Add(0);
				return position;
			}

			public void Patch(int position, ushort value)
			{
				m_bytes[position] = (byte)((value >> 8) & 0xFF);
				m_bytes[position + 1] = (byte)(value & 0xFF);
			}

			public byte[] ToArray()
			{
				return m_bytes.ToArray();
			}
		}
	}
}
