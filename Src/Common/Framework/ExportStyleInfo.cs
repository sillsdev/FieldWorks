using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Style info for export purposes, adds methods for inspecting style contents to aid in export
	/// operations.
	/// </summary>
	public class ExportStyleInfo : BaseStyleInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExportStyleInfo"/> class based on a FW
		/// style and text properties.
		/// </summary>
		public ExportStyleInfo(IStStyle style, ITsTextProps props)
			: base(style, props)
		{
		}

		/// <summary>
		/// Copy constructor, builds an ExportStyleInfo from a BaseStyleInfo
		/// </summary>
		/// <param name="style"></param>
		public ExportStyleInfo(BaseStyleInfo style) : base(style, "export" + style.Name)
		{
		}

		/// <summary>
		/// Returns the rtl value, or TriStateBool.triNotSet
		/// </summary>
		public new TriStateBool DirectionIsRightToLeft
		{
			get
			{
				if(m_rtl.ValueIsSet)
					return m_rtl.Value;
				else
					return TriStateBool.triNotSet;
			}
		}

		/// <summary/>
		public bool HasKeepTogether
		{
			get { return m_keepTogether.ValueIsSet; }
		}

		/// <summary/>
		public bool HasKeepWithNext
		{
			get { return m_keepWithNext.ValueIsSet; }
		}

		/// <summary/>
		public bool HasWidowOrphanControl
		{
			get { return m_widowOrphanControl.ValueIsSet; }
		}

		/// <summary/>
		public bool HasAlignment
		{
			get { return m_alignment.ValueIsSet; }
		}

		/// <summary/>
		public bool HasLineSpacing
		{
			get { return m_lineSpacing.ValueIsSet; }
		}

		/// <summary/>
		public bool HasSpaceBefore
		{
			get { return m_spaceBefore.ValueIsSet; }
		}

		/// <summary/>
		public bool HasSpaceAfter
		{
			get { return m_spaceAfter.ValueIsSet; }
		}

		/// <summary/>
		public bool HasFirstLineIndent
		{
			get { return m_firstLineIndent.ValueIsSet; }
		}

		/// <summary/>
		public bool HasLeadingIndent
		{
			get { return m_leadingIndent.ValueIsSet; }
		}

		/// <summary/>
		public bool HasTrailingIndent
		{
			get { return m_trailingIndent.ValueIsSet; }
		}

		/// <summary/>
		public bool HasBorder
		{
			get { return m_border.ValueIsSet; }
		}

		/// <summary/>
		public bool HasBorderColor
		{
			get { return m_borderColor.ValueIsSet; }
		}

		/// <summary/>
		public string InheritsFrom
		{
			get { return m_basedOnStyleName; }
		}

		/// <summary/>
		public VwBulNum NumberScheme
		{
			get
			{
				return m_bulletInfo != null && m_bulletInfo.ValueIsSet
							 ? m_bulletInfo.Value.m_numberScheme
							 : VwBulNum.kvbnNone;
			}
		}
	}
}