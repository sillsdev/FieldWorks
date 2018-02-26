// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Summary description for FontHeightAdjuster.
	/// </summary>
	public static class FontHeightAdjuster
	{
		/// <summary>
		/// Make sure that all runs of the given ts string will fit within the given height.
		/// </summary>
		/// <param name="tss">(Potentially) unadjusted TsString -- may have some pre-existing
		/// adjustments, but if it does, we (probably) ignore those and recheck every run</param>
		/// <param name="dympMaxHeight">The maximum height (in millipoints) of the Ts String.</param>
		/// <param name="styleSheet"></param>
		/// <param name="writingSystemFactory"></param>
		public static ITsString GetAdjustedTsString(ITsString tss, int dympMaxHeight, IVwStylesheet styleSheet, ILgWritingSystemFactory writingSystemFactory)
		{
			if (dympMaxHeight == 0)
			{
				return tss;
			}

			ITsStrBldr bldr = null;

			var runCount = tss.RunCount;
			for (var irun = 0; irun < runCount; irun++)
			{
				var props = tss.get_Properties(irun);
				int dummy;
				var wsTmp = props.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
				var styleName = props.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);
				int height;
				string name;
				float sizeInPoints;
				using (var fontForStyle = GetFontForStyle(styleName, styleSheet, wsTmp, writingSystemFactory))
				{
					height = GetFontHeight(fontForStyle);
					name = fontForStyle.Name;
					sizeInPoints = fontForStyle.SizeInPoints;
				}
				var curHeight = height;
				// incrementally reduce the size of the font until the text can fit
				while (curHeight > dympMaxHeight)
				{
					using (var font = new Font(name, sizeInPoints - 0.25f))
					{
						curHeight = GetFontHeight(font);
						name = font.Name;
						sizeInPoints = font.SizeInPoints;
					}
				}

				if (curHeight != height)
				{
					// apply formatting to the problem run
					if (bldr == null)
					{
						bldr = tss.GetBldr();
					}

					var iStart = tss.get_MinOfRun(irun);
					var iEnd = tss.get_LimOfRun(irun);
					bldr.SetIntPropValues(iStart, iEnd, (int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, (int)(sizeInPoints * 1000.0f));
				}
			}

			return bldr != null ? bldr.GetString() : tss;
		}

		/// <summary>
		/// Gets the height (ascent + descent) in millipoints of the font.
		/// </summary>
		public static int GetFontHeight(Font f)
		{
			var fontFamily = f.FontFamily;
			var ascent = f.SizeInPoints * fontFamily.GetCellAscent(f.Style) / fontFamily.GetEmHeight(f.Style);
			var descent = f.SizeInPoints * fontFamily.GetCellDescent(f.Style) / fontFamily.GetEmHeight(f.Style);
			return (int)((ascent + descent) * 1000.0f);
		}

		/// <summary>
		/// Make sure that all runs of the given ts string don't specify an explicit point size.
		/// </summary>
		public static ITsString GetUnadjustedTsString(ITsString tss)
		{
			var runCount = tss.RunCount;
			for (var irun = 0; irun < runCount; irun++)
			{
				var props = tss.get_Properties(irun);
				int var;
				props.GetIntPropValues((int)FwTextPropType.ktptFontSize, out var);
				if (var != -1)
				{
					var bldr = tss.GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptFontSize, -1, -1);
					return bldr.GetString();
				}
			}

			return tss; // no modified runs
		}

		/// <summary>
		/// Gets the character render properties for the given style name and writing system.
		/// </summary>
		/// <param name="styleName">The style name.</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="hvoWs">The HVO of the WS.</param>
		/// <param name="writingSystemFactory">The writing system factory.</param>
		/// <returns>The character render properties.</returns>
		public static LgCharRenderProps GetChrpForStyle(string styleName, IVwStylesheet styleSheet, int hvoWs, ILgWritingSystemFactory writingSystemFactory)
		{
			if (string.IsNullOrEmpty(writingSystemFactory.GetStrFromWs(hvoWs)))
			{
				try
				{
					//You may have forgotten to set the WritingSystemFactory in a recently added custom control?
					throw new ArgumentException("This is a hard-to-reproduce scenario (TE-6891) where writing system (" + hvoWs + ") and factory are inconsistent.");
				}
				catch (ArgumentException e)
				{
					Logger.WriteError(e);
					var msg =  $"{e.Message} If we aren't called from a Widget, call an expert (JohnT) while you have this Assert active!";
					Debug.Fail(msg);
					hvoWs = writingSystemFactory.UserWs;
				}
			}

			IVwPropertyStore vwps = VwPropertyStoreClass.Create();
			vwps.Stylesheet = styleSheet;
			vwps.WritingSystemFactory = writingSystemFactory;

			var ttpBldr = TsStringUtils.MakePropsBldr();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleName);
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoWs);
			var ttp = ttpBldr.GetTextProps();
			var chrps = vwps.get_ChrpFor(ttp);
			var ws = writingSystemFactory.get_EngineOrNull(hvoWs);
			ws.InterpretChrp(ref chrps);
			return chrps;
		}

		/// <summary>
		/// Find the height of the font used for the given style name and writing system
		/// </summary>
		/// <param name="styleName">Style name</param>
		/// <param name="styleSheet">The stylesheet where the style is defined</param>
		/// <param name="hvoWs">Id of the writing system</param>
		/// <param name="writingSystemFactory"></param>
		/// <returns>Height of the font (not counting the descender) for the given style in
		/// the given writing system</returns>
		public static int GetFontHeightForStyle(string styleName, IVwStylesheet styleSheet, int hvoWs, ILgWritingSystemFactory writingSystemFactory)
		{
			// For testing, if there is no writing system set, just return 0 for the height.
			if (hvoWs == -1)
			{
				return 0;
			}

			var chrps = GetChrpForStyle(styleName, styleSheet, hvoWs, writingSystemFactory);
			return chrps.dympHeight;
		}

		/// <summary>
		/// Find the font that is used for the given style name and writing system.
		/// </summary>
		/// <param name="styleName">The style name.</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="hvoWs">The HVO of the WS.</param>
		/// <param name="writingSystemFactory">The writing system factory.</param>
		/// <returns>The font.</returns>
		public static Font GetFontForStyle(string styleName, IVwStylesheet styleSheet, int hvoWs, ILgWritingSystemFactory writingSystemFactory)
		{
			var chrps = GetChrpForStyle(styleName, styleSheet, hvoWs, writingSystemFactory);
			var dympHeight = chrps.dympHeight;
			var bldr = new StringBuilder(chrps.szFaceName.Length);
			foreach (var ch in chrps.szFaceName)
			{
				if (ch == 0)
				{
					break; // null termination
				}
				bldr.Append(Convert.ToChar(ch));
			}

			return new Font(bldr.ToString(), dympHeight / 1000.0f);
		}

		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the stylesheet obtained from the mediator.
		/// </summary>
		public static Font GetFontForNormalStyle(int hvoWs, ILgWritingSystemFactory wsf, IPropertyTable propertyTable)
		{
			return GetFontForNormalStyle(hvoWs, propertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"), wsf);
		}

		/// <summary>
		/// Determine the default font to use for the specified writing system, displayed in
		/// the default Normal style of the specified stylesheet using the writing system
		/// factory from the specified cache.
		/// </summary>
		public static Font GetFontForNormalStyle(int hvoWs, IVwStylesheet styleSheet, LcmCache cache)
		{
			return GetFontForNormalStyle(hvoWs, styleSheet, cache.WritingSystemFactory);
		}

		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the specified stylesheet.
		/// This is duplicated in SimpleRootSite/EditingHelper.
		/// </summary>
		public static Font GetFontForNormalStyle(int hvoWs, IVwStylesheet styleSheet, ILgWritingSystemFactory wsf)
		{
			var ttpNormal = styleSheet.NormalFontStyle;
			var styleName = StyleServices.NormalStyleName;
			if (ttpNormal != null)
			{
				styleName = ttpNormal.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			}

			return GetFontForStyle(styleName, styleSheet, hvoWs, wsf);
		}

		/// <summary>
		/// Grow the dialog's height by delta, and adjust any controls that need it.  The grower
		/// Control is adjusted outside this method.
		/// </summary>
		public static void GrowDialogAndAdjustControls(Control parent, int delta, Control grower)
		{
			if (delta == 0)
			{
				return;
			}
			parent.Height += delta;
			foreach (Control c in parent.Controls)
			{
				if (c == grower)
				{
					continue;
				}
				var anchorTop = ((int)c.Anchor & (int)AnchorStyles.Top) != 0;
				var anchorBottom = ((int)c.Anchor & (int)AnchorStyles.Bottom) != 0;
				if (c.Top > grower.Top && anchorTop)
				{
					// Anchored at the top and below our control: move it down
					c.Top += delta;
				}
				if (anchorTop && anchorBottom)
				{
					// Anchored top and bottom, it stretched with the window,
					// but we don't want that.
					c.Height -= delta;
				}
			}
		}
	}
}