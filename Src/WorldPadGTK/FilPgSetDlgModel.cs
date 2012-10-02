// FilPgSetDlgModel.cs
// User: Jean-Marc Giffin at 3:34 P 08/05/2008

using System;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// The Page Setup Dialog Model
	/// </summary>
	public class FilPgSetDlgModel : IDialogModel {
		/// <summary>The default value for the margin settings.</summary>
		public const double DEFAULT_MARGINS = 0.10;
		/// <summary>
		/// Whether or not protrait is the default page setup.
		/// If false, landscape is the default.
		/// </summary>
		public const bool DEFAULT_PORTRAIT = true;
		/// <summary>Whether or not "show header on first page" is default.</summary>
		public const bool DEAFULT_SHOW_HEADER_ON_FIRST_PAGE = false;

		/// <summary>Attributes of a "Letter"</summary>
		public static Paper PAPER_SIZE_LETTER = new Paper("Letter 8 1/2 x 11 in", 8.50, 11.00);
		/// <summary>Attributes of a "Legal Letter"</summary>
		public static Paper PAPER_SIZE_LEGAL = new Paper("Legal 8 1/2 x 14 in", 8.50, 14.00);
		/// <summary>Attributes of a "A4 Paper"</summary>
		public static Paper PAPER_SIZE_A4 = new Paper("A4 210 x 297 mm", 8.26, 11.69);
		/// <summary>Attributes for a custom-sized letter (ignored).</summary>
		public static Paper PAPER_SIZE_CUSTOM = new Paper("Custom size", -1, -1);

		/// <summary>A Collection of the various paper types.</summary>
		public static Paper[] PaperTypes = { PAPER_SIZE_LETTER, PAPER_SIZE_LEGAL, PAPER_SIZE_A4, PAPER_SIZE_CUSTOM };

		private double leftMarg_;
		private double rightMarg_;
		private double topMarg_;
		private double bottomMarg_;
		private double headEdge_;
		private double footEdge_;
		private PaperType paperSize_;
		private bool portrait_;
		private double paperWidth_;
		private double paperHeight_;
		private bool showHeaderOnFirstPage_;
		private string header_;
		private string footnote_;

		/// <summary>
		/// A struct containing the names and information about the various kinds of paper
		/// types that can be chosen from within the dialog.
		/// </summary>
		public struct Paper
		{
			private string name_;
			private double width_;
			private double height_;

			/// <summary>
			/// Create a new Paper type.
			/// </summary>
			/// <param name="name">Name of the paper type, displayed to the user.</param>
			/// <param name="width">Width of the paper type, in inches.</param>
			/// <param name="height">Height of the paper type, in inches.</param>
			public Paper(string name, double width, double height)
			{
				name_ = name;
				width_ = width;
				height_ = height;
			}

			/// <summary>Name of the paper type, displayed to the user.</summary>
			public string Name
			{
				get { return name_; }
				set { name_ = value; }
			}

			/// <param name="width">Width of the paper type, in inches.</param>
			public double Width
			{
				get { return width_; }
				set { width_ = value; }
			}

			/// <param name="height">Height of the paper type, in inches.</param>
			public double Height
			{
				get { return height_; }
				set { height_ = value; }
			}
		}

		/// <summary>The various paper types.</summary>
		public enum PaperType
		{
			Letter,
			Legal,
			A4,
			Custom
		}

		/// <summary>Create a new Model for File -> Page Setup. Set the defaults. </summary>
		public FilPgSetDlgModel() {
			portrait_ = DEFAULT_PORTRAIT;
		}

		/// <value>The Left Margins<</value>
		public double LeftMargins
		{
			get { return leftMarg_; }
			set { leftMarg_ = value; }
		}

		/// <value>The Right Margins</value>
		public double RightMargins
		{
			get { return rightMarg_; }
			set { rightMarg_ = value; }
		}

		/// <value>The Top Margins</value>
		public double TopMargins
		{
			get { return topMarg_; }
			set { topMarg_ = value; }
		}

		/// <value>The Bottom Margins</value>
		public double BottomMargins
		{
			get { return bottomMarg_; }
			set { bottomMarg_ = value; }
		}

		/// <value>The distance of the Header from the Edge</value>
		public double HeaderFromEdge
		{
			get { return headEdge_; }
			set { headEdge_ = value ; }
		}

		/// <value>The distance of the Footer from the Edge</value>
		public double FooterFromEdge
		{
			get { return footEdge_; }
			set { footEdge_ = value ; }
		}

		/// <value>The PaperType currently being used.</value>
		public PaperType PaperSize
		{
			get { return paperSize_; }
			set { paperSize_ = value ; }
		}

		/// <value>The Height of the Paper</value>
		public double PaperHeight
		{
			get { return paperHeight_; }
			set { paperHeight_ = value ; }
		}

		/// <value> The Width of the Paper</value>
		public double PaperWidth
		{
			get { return paperWidth_; }
			set { paperWidth_ = value ; }
		}

		/// <value>Header message</value>
		public string Header
		{
			get { return header_; }
			set { header_ = value; }
		}

		/// <value>Footnote messages</value>
		public string Footnote
		{
			get { return footnote_; }
			set { footnote_ = value; }
		}

		/// <value>Whether the document is Portrait (true) or Landscape (false)</value>
		public bool Portrait
		{
			get { return portrait_; }
			set { portrait_ = value; }
		}

		/// <value>Whether or not the header is visible on the first page.</value>
		public bool ShowHeaderOnFirstPage
		{
			get { return showHeaderOnFirstPage_; }
			set { showHeaderOnFirstPage_ = value; }
		}

		/// <value>Get a string list of the names of the Paper Types.</value>
		public string[] GetPaperTypeNames()
		{
			string[] result = new string[PaperTypes.Length];
			for (int i = 0; i < PaperTypes.Length; i++)
			{
				result[i] = PaperTypes[i].Name;
			}
			return result;
		}
	}
}
