// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Style info for export purposes, adds methods for inspecting style contents to aid in export
	/// operations.
	/// </summary>
	public class ExportStyleInfo : BaseStyleInfo
	{
		/// <summary />
		public ExportStyleInfo(IStStyle style, ITsTextProps props)
			: base(style, props)
		{
		}

		/// <summary>
		/// Copy constructor, builds an ExportStyleInfo from a BaseStyleInfo
		/// </summary>
		public ExportStyleInfo(BaseStyleInfo style) : base(style, "export" + style.Name)
		{
		}

		/// <summary>
		/// Returns the rtl value, or TriStateBool.triNotSet
		/// </summary>
		public new TriStateBool DirectionIsRightToLeft => m_rtl.ValueIsSet ? m_rtl.Value : TriStateBool.triNotSet;

		/// <summary />
		public bool HasKeepTogether => m_keepTogether.ValueIsSet;

		/// <summary />
		public bool HasKeepWithNext => m_keepWithNext.ValueIsSet;

		/// <summary />
		public bool HasWidowOrphanControl => m_widowOrphanControl.ValueIsSet;

		/// <summary />
		public bool HasAlignment => m_alignment.ValueIsSet;

		/// <summary />
		public bool HasLineSpacing => m_lineSpacing.ValueIsSet;

		/// <summary />
		public bool HasSpaceBefore => m_spaceBefore.ValueIsSet;

		/// <summary />
		public bool HasSpaceAfter => m_spaceAfter.ValueIsSet;

		/// <summary />
		public bool HasFirstLineIndent => m_firstLineIndent.ValueIsSet;

		/// <summary />
		public bool HasLeadingIndent => m_leadingIndent.ValueIsSet;

		/// <summary />
		public bool HasTrailingIndent => m_trailingIndent.ValueIsSet;

		/// <summary />
		public bool HasBorder => m_border.ValueIsSet;

		/// <summary />
		public bool HasBorderColor => m_borderColor.ValueIsSet;

		/// <summary />
		public string InheritsFrom => m_basedOnStyleName;

		/// <summary />
		public VwBulNum NumberScheme => m_bulletInfo != null && m_bulletInfo.ValueIsSet ? m_bulletInfo.Value.m_numberScheme : VwBulNum.kvbnNone;
	}
}