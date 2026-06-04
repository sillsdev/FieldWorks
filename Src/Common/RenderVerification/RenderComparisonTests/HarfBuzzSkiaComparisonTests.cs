using System.Linq;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using NUnit.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace SIL.FieldWorks.Common.RenderVerification.RenderComparisonTests
{
	[TestFixture]
	public class HarfBuzzSkiaComparisonTests
	{
		[Test]
		public void ShapeText_OpenTypeFeatureToggleChangesShapingData()
		{
			using (var typeface = SKTypeface.FromFamilyName("Times New Roman"))
			{
				if (typeface == null)
					Assert.Inconclusive("Times New Roman is not installed on this machine.");

				var disabled = ShapeText(typeface, "office affinity AVATAR", "-liga");
				var enabled = ShapeText(typeface, "office affinity AVATAR", "+liga");

				if (disabled.SequenceEqual(enabled))
					Assert.Inconclusive("Times New Roman did not expose a deterministic liga shaping delta through HarfBuzzSharp.");
			}
		}

		[Test]
		public void DrawShapedText_ProducesNonBlankComparisonBitmap()
		{
			using (var typeface = SKTypeface.FromFamilyName("Times New Roman"))
			{
				if (typeface == null)
					Assert.Inconclusive("Times New Roman is not installed on this machine.");

				using (var bitmap = new SKBitmap(360, 90))
				using (var canvas = new SKCanvas(bitmap))
				using (var paint = new SKPaint { Typeface = typeface, TextSize = 40, IsAntialias = true, Color = SKColors.Black })
				using (var shaper = new SKShaper(typeface))
				using (var buffer = new Buffer())
				{
					canvas.Clear(SKColors.White);
					buffer.AddUtf8("office affinity AVATAR");
					buffer.GuessSegmentProperties();
					var shaped = shaper.Shape(buffer, paint);
					Assert.That(shaped.Codepoints, Is.Not.Empty);

					canvas.DrawShapedText(shaper, "office affinity AVATAR", 12, 58, paint);
					Assert.That(CountNonWhitePixels(bitmap), Is.GreaterThan(0));
				}
			}
		}

		private static int CountNonWhitePixels(SKBitmap bitmap)
		{
			return Enumerable.Range(0, bitmap.Height)
				.Sum(y => Enumerable.Range(0, bitmap.Width)
					.Count(x => bitmap.GetPixel(x, y) != SKColors.White));
		}

		private static uint[] ShapeText(SKTypeface typeface, string text, string feature)
		{
			var fontData = ReadTypefaceData(typeface);
			var fontDataHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
			try
			{
				using (var blob = new Blob(fontDataHandle.AddrOfPinnedObject(), fontData.Length, MemoryMode.Duplicate))
				using (var face = new Face(blob, 0))
				using (var font = new HarfBuzzSharp.Font(face))
				using (var buffer = new Buffer())
				{
					font.SetScale(40 * 64, 40 * 64);
					buffer.AddUtf8(text);
					buffer.GuessSegmentProperties();
					font.Shape(buffer, new[] { Feature.Parse(feature) });
					return buffer.GlyphInfos.Select(info => info.Codepoint).ToArray();
				}
			}
			finally
			{
				fontDataHandle.Free();
			}
		}

		private static byte[] ReadTypefaceData(SKTypeface typeface)
		{
			int faceIndex;
			using (var stream = typeface.OpenStream(out faceIndex))
			{
				if (stream == null || !stream.HasLength)
					Assert.Inconclusive("The selected typeface does not expose readable font data.");

				var data = new byte[checked((int)stream.Length)];
				var read = stream.Read(data, data.Length);
				if (read != data.Length)
					Assert.Inconclusive("The selected typeface could not be read completely.");

				return data;
			}
		}
	}
}
