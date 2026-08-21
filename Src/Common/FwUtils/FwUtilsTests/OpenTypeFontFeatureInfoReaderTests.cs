// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class OpenTypeFontFeatureInfoReaderTests
	{
		private IReadOnlyList<OpenTypeFontFeatureInfo> m_features;

		[OneTimeSetUp]
		public void ReadCharisFeatures()
		{
			var fontPath = CharisTestFontPath();
			if (!File.Exists(fontPath))
				Assert.Ignore("Charis SIL 6.200 test font not present; run the build so PackageRestore downloads it.");

			var tableSource = FileTableSource(File.ReadAllBytes(fontPath));
			m_features = OpenTypeFontFeatureInfoReader.Read(tableSource);
		}

		private OpenTypeFontFeatureInfo Feature(string tag)
		{
			var info = m_features.FirstOrDefault(f => f.Tag == tag);
			Assert.That(info, Is.Not.Null, $"expected font to declare feature '{tag}'");
			return info;
		}

		[Test]
		public void CharacterVariant_ReadsFontLabelAndOrderedOptions()
		{
			var cv43 = Feature("cv43");
			Assert.That(cv43.FontSuppliedLabel, Is.EqualTo("Capital Eng"));
			Assert.That(cv43.Options,
				Is.EqualTo(new[] { "Lowercase no descender", "Capital form", "Lowercase short stem" }));
		}

		[Test]
		public void CharacterVariant_ReadsTwoOptions()
		{
			var cv25 = Feature("cv25");
			Assert.That(cv25.FontSuppliedLabel, Is.EqualTo("Lowercase rams horn"));
			Assert.That(cv25.Options, Is.EqualTo(new[] { "Large bowl", "Small gamma" }));
		}

		[Test]
		public void CharacterVariant_ReadsSingleOption()
		{
			var cv13 = Feature("cv13");
			Assert.That(cv13.FontSuppliedLabel, Is.EqualTo("Capital B hook"));
			Assert.That(cv13.Options, Is.EqualTo(new[] { "Single bowl" }));
		}

		[Test]
		public void StylisticSet_ReadsFontName()
		{
			var ss01 = Feature("ss01");
			Assert.That(ss01.FontSuppliedLabel, Is.EqualTo("Single-story a and g"));
			Assert.That(ss01.Options, Is.Empty);
		}

		[Test]
		public void RegisteredFeatures_AreDiscoveredWithoutFontLabels()
		{
			foreach (var tag in new[] { "liga", "smcp" })
			{
				var info = Feature(tag);
				Assert.That(info.FontSuppliedLabel, Is.Null, $"'{tag}' should have no font-supplied label");
				Assert.That(info.Options, Is.Empty);
			}
		}

		[Test]
		public void GposFeatures_AreDiscovered()
		{
			// mark and mkmk live in GPOS, not GSUB; the reader must scan both layout tables.
			Assert.That(m_features.Select(f => f.Tag), Does.Contain("mark"));
			Assert.That(m_features.Select(f => f.Tag), Does.Contain("mkmk"));
		}

		[Test]
		public void Read_ReturnsFeaturesSortedByTag()
		{
			var tags = m_features.Select(f => f.Tag).ToArray();
			Assert.That(tags, Is.Ordered.Using<string>(StringComparer.Ordinal));
			Assert.That(tags, Is.Unique);
		}

		private static string CharisTestFontPath()
		{
			var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return Path.Combine(assemblyDir, "TestData", "Fonts", "CharisSIL", "CharisSIL-Regular.ttf");
		}

		/// <summary>
		/// Builds a table-source delegate over raw sfnt bytes by parsing the table directory, so
		/// tests exercise the reader with the same per-table byte slices GDI GetFontData yields.
		/// </summary>
		internal static Func<string, byte[]> FileTableSource(byte[] font)
		{
			var tables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
			int numTables = (font[4] << 8) | font[5];
			for (var i = 0; i < numTables; i++)
			{
				var record = 12 + i * 16;
				var tag = Encoding.ASCII.GetString(font, record, 4);
				var offset = ReadUInt32(font, record + 8);
				var length = ReadUInt32(font, record + 12);
				if (offset + length <= (uint)font.Length)
				{
					var bytes = new byte[length];
					Array.Copy(font, offset, bytes, 0, length);
					tables[tag] = bytes;
				}
			}
			return tag =>
			{
				byte[] value;
				return tables.TryGetValue(tag, out value) ? value : null;
			};
		}

		private static uint ReadUInt32(byte[] data, int offset)
		{
			return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
				((uint)data[offset + 2] << 8) | data[offset + 3];
		}
	}
}
