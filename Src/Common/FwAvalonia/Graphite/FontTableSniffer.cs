// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.FwAvalonia.Graphite
{
	/// <summary>
	/// Reads SFNT table-directory evidence from fonts (graphite-transition-support task 1.3):
	/// <c>Silf</c> marks Graphite shaping data, <c>GSUB</c>/<c>GPOS</c> mark OpenType shaping.
	/// Two sources: a font file/byte parse (deterministic, used by tests and file-based resolution)
	/// and a GDI query for an installed font family (used at the product edge where only the
	/// writing system's font *name* is known). Never throws on malformed input — unreadable fonts
	/// yield <see cref="GraphiteFontTableEvidence.Unknown"/> so classification fails safe to G0.
	/// </summary>
	public static class FontTableSniffer
	{
		private const uint TtcfTag = 0x74746366; // 'ttcf'
		private const uint GdiError = 0xFFFFFFFF;

		/// <summary>Sniffs a font file (TTF/OTF or first font of a TTC).</summary>
		public static GraphiteFontTableEvidence FromFile(string path)
		{
			try
			{
				return FromBytes(File.ReadAllBytes(path));
			}
			catch (IOException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
			catch (UnauthorizedAccessException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
		}

		/// <summary>Sniffs font bytes (TTF/OTF or first font of a TTC).</summary>
		public static GraphiteFontTableEvidence FromBytes(byte[] font)
		{
			if (font == null || font.Length < 12)
			{
				return GraphiteFontTableEvidence.Unknown;
			}

			try
			{
				var offset = 0;
				if (ReadUInt32BE(font, 0) == TtcfTag)
				{
					// TrueType collection: evidence from the first font's table directory.
					offset = checked((int)ReadUInt32BE(font, 12));
				}

				int numTables = (font[offset + 4] << 8) | font[offset + 5];
				var hasSilf = false;
				var hasGsubOrGpos = false;
				for (var i = 0; i < numTables; i++)
				{
					var entry = offset + 12 + 16 * i;
					if (entry + 4 > font.Length)
					{
						break;
					}

					var tag = System.Text.Encoding.ASCII.GetString(font, entry, 4);
					if (tag == "Silf")
						hasSilf = true;
					else if (tag == "GSUB" || tag == "GPOS")
						hasGsubOrGpos = true;
				}

				return new GraphiteFontTableEvidence(hasSilf, hasGsubOrGpos);
			}
			catch (OverflowException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
			catch (ArgumentOutOfRangeException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
			catch (IndexOutOfRangeException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
		}

		/// <summary>
		/// Sniffs an installed font family via GDI <c>GetFontData</c>. Returns
		/// <see cref="GraphiteFontTableEvidence.Unknown"/> when the family is not installed (GDI
		/// would silently substitute a fallback face, which must not produce false evidence).
		/// </summary>
		public static GraphiteFontTableEvidence FromInstalledFamily(string familyName)
		{
			if (string.IsNullOrWhiteSpace(familyName))
			{
				return GraphiteFontTableEvidence.Unknown;
			}

			try
			{
				using (var font = new Font(familyName, 12f))
				{
					// GDI substitutes a default family for unknown names; reject substitutions.
					if (!string.Equals(font.Name, familyName, StringComparison.OrdinalIgnoreCase))
					{
						return GraphiteFontTableEvidence.Unknown;
					}

					using (var bitmap = new Bitmap(1, 1))
					using (var graphics = Graphics.FromImage(bitmap))
					{
						var hdc = graphics.GetHdc();
						var hfont = font.ToHfont();
						try
						{
							var oldFont = SelectObject(hdc, hfont);
							try
							{
								var hasSilf = HasTable(hdc, "Silf");
								var hasGsubOrGpos = HasTable(hdc, "GSUB") || HasTable(hdc, "GPOS");
								return new GraphiteFontTableEvidence(hasSilf, hasGsubOrGpos);
							}
							finally
							{
								SelectObject(hdc, oldFont);
							}
						}
						finally
						{
							DeleteObject(hfont);
							graphics.ReleaseHdc(hdc);
						}
					}
				}
			}
			catch (ArgumentException)
			{
				return GraphiteFontTableEvidence.Unknown;
			}
		}

		private static bool HasTable(IntPtr hdc, string tag)
		{
			// GetFontData takes the table tag packed in byte order (first character lowest byte).
			var dwTable = (uint)(tag[0] | (tag[1] << 8) | (tag[2] << 16) | (tag[3] << 24));
			return GetFontData(hdc, dwTable, 0, null, 0) != GdiError;
		}

		private static uint ReadUInt32BE(byte[] data, int offset)
			=> ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

		[DllImport("gdi32.dll", SetLastError = true)]
		private static extern uint GetFontData(IntPtr hdc, uint dwTable, uint dwOffset, byte[] lpvBuffer, uint cbData);

		[DllImport("gdi32.dll")]
		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);
	}
}
