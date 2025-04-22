using System;
using System.Xml.Serialization;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Configuration shared by all pictures in a dictionary configuration.
	/// </summary>
	public class PictureConfiguration
	{
		public PictureConfiguration(PictureConfiguration other)
		{
			Alignment = other.Alignment;
			Width = other.Width;
		}

		public PictureConfiguration() { }

		[XmlAttribute("alignment")]
		public AlignmentType Alignment { get; set; } = AlignmentType.Right;

		/// <summary>
		/// Maximum width of the picture in inches.
		/// </summary>
		[XmlAttribute("width")]
		public double Width { get; set; } = 1.0f;

		/// <summary>
		/// Maximum height of the picture in inches.
		/// </summary>
		[XmlAttribute("height")]
		public double Height { get; set; } = 1.0f;
	}
}