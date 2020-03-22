// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer
{
	/// <summary>
	/// Style info for export purposes, adds methods for inspecting style contents to aid in export
	/// operations.
	/// </summary>
	internal sealed class ExportStyleInfo : BaseStyleInfo
	{
		/// <summary />
		internal ExportStyleInfo(IStStyle style, ITsTextProps props)
			: base(style, props)
		{
		}

		/// <summary>
		/// Copy constructor, builds an ExportStyleInfo from a BaseStyleInfo
		/// </summary>
		internal ExportStyleInfo(BaseStyleInfo style)
			: base(style, $"export{style.Name}")
		{
		}

		/// <summary>
		/// Returns the rtl value, or TriStateBool.triNotSet
		/// </summary>
		internal new TriStateBool DirectionIsRightToLeft => m_rtl.ValueIsSet ? m_rtl.Value : TriStateBool.triNotSet;

		/// <summary />
		internal bool HasKeepTogether => m_keepTogether.ValueIsSet;

		/// <summary />
		internal bool HasKeepWithNext => m_keepWithNext.ValueIsSet;

		/// <summary />
		internal bool HasWidowOrphanControl => m_widowOrphanControl.ValueIsSet;

		/// <summary />
		internal bool HasAlignment => m_alignment.ValueIsSet;

		/// <summary />
		internal bool HasLineSpacing => m_lineSpacing.ValueIsSet;

		/// <summary />
		internal bool HasSpaceBefore => m_spaceBefore.ValueIsSet;

		/// <summary />
		internal bool HasSpaceAfter => m_spaceAfter.ValueIsSet;

		/// <summary />
		internal bool HasFirstLineIndent => m_firstLineIndent.ValueIsSet;

		/// <summary />
		internal bool HasLeadingIndent => m_leadingIndent.ValueIsSet;

		/// <summary />
		internal bool HasTrailingIndent => m_trailingIndent.ValueIsSet;

		/// <summary />
		internal bool HasBorder => m_border.ValueIsSet;

		/// <summary />
		internal bool HasBorderColor => m_borderColor.ValueIsSet;

		/// <summary />
		internal string InheritsFrom => m_basedOnStyleName;

		/// <summary />
		internal VwBulNum NumberScheme => m_bulletInfo != null && m_bulletInfo.ValueIsSet ? m_bulletInfo.Value.m_numberScheme : VwBulNum.kvbnNone;
	}
}