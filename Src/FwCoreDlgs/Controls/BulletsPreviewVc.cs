// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// View constructor for the bullets preview view
	/// </summary>
	internal class BulletsPreviewVc : FwBaseVc
	{
		#region Data members
		private const int kdmpFakeHeight = 5000; // height for the "fake text" rectangles
		private ITsTextProps m_propertiesForFirstPreviewParagraph;
		private ITsTextProps m_propertiesForFollowingPreviewParagraph;
		#endregion

		/// <summary>
		/// The main method just displays the text with the appropriate properties.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Make a "context" paragraph before the numbering starts.
			vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			AddPreviewPara(vwenv, null, false);

			// Make the first numbered paragraph.
			// (It's not much use if we don't have properties, but that may happen while we're starting
			// up so we need to cover it.)
			AddPreviewPara(vwenv, m_propertiesForFirstPreviewParagraph, true);

			// Make two more numbered paragraphs.
			AddPreviewPara(vwenv, m_propertiesForFollowingPreviewParagraph, true);
			AddPreviewPara(vwenv, m_propertiesForFollowingPreviewParagraph, true);

			// Make a "context" paragraph after the numbering ends.
			AddPreviewPara(vwenv, null, true);
		}

		/// <summary>
		/// Adds a paragraph (gray line) to the  preview.
		/// </summary>
		private void AddPreviewPara(IVwEnv vwenv, ITsTextProps props, bool fAddSpaceBefore)
		{
			// (width is -1, meaning "use the rest of the line")
			if (props != null)
			{
				vwenv.Props = props;
			}
			else if (fAddSpaceBefore)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint, 6000);
			}

			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, IsRightToLeft ? -1 : 0);
			vwenv.OpenParagraph();
			vwenv.AddSimpleRect(Color.LightGray.ToArgb(), -1, kdmpFakeHeight, 0);
			vwenv.CloseParagraph();
		}

		/// <summary>
		/// Sets the text properties.
		/// </summary>
		internal void SetProps(ITsTextProps propertiesForFirstPreviewParagraph, ITsTextProps propertiesForFollowingPreviewParagraph)
		{
			m_propertiesForFirstPreviewParagraph = propertiesForFirstPreviewParagraph;
			m_propertiesForFollowingPreviewParagraph = propertiesForFollowingPreviewParagraph;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is right to left.
		/// </summary>
		internal bool IsRightToLeft { get; set; }
	}
}