// FmtParaDlgRtlModel.cs
// User: Jean-Marc Giffin at 4:29 P 16/06/2008

using System;
using Gdk;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtParaDlgRtlModel : IDialogModel
	{
		private Color bgColor_;
		private double leftInd_;
		private double rightInd_;
		private double beforeSpac_;
		private double afterSpac_;
		private bool rightToLeft_;
		private SpacingType spacing_;
		private IndentationType indent_;
		private AlignmentType align_;

		// THE FOLLOW CODE IS TEMPORARY AND WILL BE LATER REMOVED ONCE VIEWS IS READY
		public static string[] DIRECTIONS = { "Left to Right", "Right to Left" };
		public static string[] ALIGNMENTS = { "Unspecified", "Leading", "Left", "Centered", "Right",
			"Trailing", "Justified" };
		public static string[] INDENTATIONS = { "None", "First Line", "Hanging" };
		public static string[] SPACINGS = { "Unspecified", "Single", "1.5", "Double" };

		public enum SpacingType
		{
			Unspecified = 0,
			Single = 1,
			OneAndHalf = 2,
			Double = 3
		}
		public enum IndentationType
		{
			None,
			FirstLine,
			Hanging
		}

		public enum AlignmentType
		{
			Unspecified,
			Leading,
			Left,
			Centered,
			Right,
			Trailing,
			Justified
		}

		public FmtParaDlgRtlModel()
		{
			bgColor_ = new Gdk.Color(255, 255, 255);
		}

		public Color BackgroundColor
		{
			get { return bgColor_; }
			set { bgColor_ = value; }
		}

		public double LeftIndentation
		{
			get { return leftInd_; }
			set { leftInd_ = value; }
		}

		public double RightIndentation
		{
			get { return rightInd_; }
			set { rightInd_ = value; }
		}

		public double BeforeSpacing
		{
			get { return beforeSpac_; }
			set { beforeSpac_ = value; }
		}

		public double AfterSpacing
		{
			get { return afterSpac_; }
			set { afterSpac_ = value; }
		}

		public bool RightToLeft
		{
			get { return rightToLeft_; }
			set { rightToLeft_ = value; }
		}

		public AlignmentType Alignment
		{
			get { return align_; }
			set { align_ = value; }
		}

		public IndentationType Indentation
		{
			get { return indent_; }
			set { indent_ = value; }
		}

		public SpacingType Spacing
		{
			get { return spacing_; }
			set { spacing_ = value; }
		}
	}
}
