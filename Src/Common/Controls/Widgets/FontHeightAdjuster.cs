// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FontHeightAdjuster.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FontHeightAdjuster.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FontHeightAdjuster
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that all runs of the given ts string will fit within the given height.
		/// </summary>
		/// <param name="tss">(Potentially) unadjusted TsString -- may have some pre-existing
		/// adjustments, but if it does, we (probably) ignore those and recheck every run</param>
		/// <param name="dympMaxHeight">The maximum height (in millipoints) of the Ts String.</param>
		/// <param name="styleSheet"></param>
		/// <param name="writingSystemFactory"></param>
		/// -------------------------------------------------------------------------------------
		public static ITsString GetAdjustedTsString(ITsString tss, int dympMaxHeight,
			IVwStylesheet styleSheet, ILgWritingSystemFactory writingSystemFactory)
		{
			if (dympMaxHeight == 0)
				return tss;

			ITsStrBldr bldr = null;

			int runCount = tss.RunCount;
			for (int irun = 0; irun < runCount; irun++)
			{
				ITsTextProps props = tss.get_Properties(irun);
				int var;
				int wsTmp = props.GetIntPropValues((int)FwTextPropType.ktptWs,
					out var);
				string styleName =
					props.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);

				int height;
				string name;
				float sizeInPoints;
				using (Font f = GetFontForStyle(styleName, styleSheet, wsTmp, writingSystemFactory))
				{
					height = GetFontHeight(f);
					name = f.Name;
					sizeInPoints = f.SizeInPoints;
				}
				int curHeight = height;
				// incrementally reduce the size of the font until the text can fit
				while (curHeight > dympMaxHeight)
				{
					using (var f = new Font(name, sizeInPoints - 0.25f))
					{
						curHeight = GetFontHeight(f);
						name = f.Name;
						sizeInPoints = f.SizeInPoints;
					}
				}

				if (curHeight != height)
				{
					// apply formatting to the problem run
					if (bldr == null)
						bldr = tss.GetBldr();

					int iStart = tss.get_MinOfRun(irun);
					int iEnd = tss.get_LimOfRun(irun);
					bldr.SetIntPropValues(iStart, iEnd,
						(int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, (int)(sizeInPoints * 1000.0f));
				}
			}

			if (bldr != null)
				return bldr.GetString();
			else
				return tss;
		}

		/// <summary>
		/// Gets the height (ascent + descent) in millipoints of the font.
		/// </summary>
		/// <param name="f">The font.</param>
		/// <returns>The height.</returns>
		public static int GetFontHeight(Font f)
		{
			FontFamily ff = f.FontFamily;
			float ascent = f.SizeInPoints * ff.GetCellAscent(f.Style) / ff.GetEmHeight(f.Style);
			float descent = f.SizeInPoints * ff.GetCellDescent(f.Style) / ff.GetEmHeight(f.Style);
			return (int)((ascent + descent) * 1000.0f);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that all runs of the given ts string don't specify an explicit point size.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public static ITsString GetUnadjustedTsString(ITsString tss)
		{
			int runCount = tss.RunCount;
			for (int irun = 0; irun < runCount; irun++)
			{
				ITsTextProps props = tss.get_Properties(irun);
				int var;
				props.GetIntPropValues((int)FwTextPropType.ktptFontSize, out var);
				if (var != -1)
				{
					ITsStrBldr bldr = tss.GetBldr();
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
		public static LgCharRenderProps GetChrpForStyle(string styleName, IVwStylesheet styleSheet,
			int hvoWs, ILgWritingSystemFactory writingSystemFactory)
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
					var msg = e.Message + " If we aren't called from a Widget, "
						+ "call an expert (JohnT) while you have this Assert active!";
					Debug.Fail(msg);
					hvoWs = writingSystemFactory.UserWs;
				}
			}

			VwPropertyStoreManaged vwps = new VwPropertyStoreManaged();
			vwps.Stylesheet = styleSheet;
			vwps.WritingSystemFactory = writingSystemFactory;

			ITsPropsBldr ttpBldr = TsStringUtils.MakePropsBldr();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleName);
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoWs);
			ITsTextProps ttp = ttpBldr.GetTextProps();

			LgCharRenderProps chrps = vwps.get_ChrpFor(ttp);
			ILgWritingSystem ws = writingSystemFactory.get_EngineOrNull(hvoWs);
			ws.InterpretChrp(ref chrps);
			return chrps;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the height of the font used for the given style name and writing system
		/// </summary>
		/// <param name="styleName">Style name</param>
		/// <param name="styleSheet">The stylesheet where the style is defined</param>
		/// <param name="hvoWs">Id of the writing system</param>
		/// <param name="writingSystemFactory"></param>
		/// <returns>Height of the font (not counting the descender) for the given style in
		/// the given writing system</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetFontHeightForStyle(string styleName, IVwStylesheet styleSheet,
			int hvoWs, ILgWritingSystemFactory writingSystemFactory)
		{
			// For testing, if there is no writing system set, just return 0 for the height.
			if (hvoWs == -1)
				return 0;

			LgCharRenderProps chrps = GetChrpForStyle(styleName, styleSheet, hvoWs, writingSystemFactory);
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
		public static Font GetFontForStyle(string styleName, IVwStylesheet styleSheet,
			int hvoWs, ILgWritingSystemFactory writingSystemFactory)
		{
			LgCharRenderProps chrps = GetChrpForStyle(styleName, styleSheet, hvoWs, writingSystemFactory);

			int dympHeight = chrps.dympHeight;
			StringBuilder bldr = new StringBuilder(chrps.szFaceName.Length);
			for (int i = 0; i < chrps.szFaceName.Length; i++)
			{
				ushort ch = chrps.szFaceName[i];
				if (ch == 0)
					break; // null termination
				bldr.Append(Convert.ToChar(ch));
			}

			return new Font(bldr.ToString(), (float)(dympHeight / 1000.0f));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the stylesheet obtained from the mediator.
		/// </summary>
		/// <param name="hvoWs">The hvo of the writing system.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// <param name="propertyTable"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Font GetFontForNormalStyle(int hvoWs, ILgWritingSystemFactory wsf, PropertyTable propertyTable)
		{
			return GetFontForNormalStyle(hvoWs, StyleSheetFromPropertyTable(propertyTable), wsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the default font to use for the specified writing system, displayed in
		/// the default Normal style of the specified stylesheet using the writing system
		/// factory from the specified cache.
		/// </summary>
		/// <param name="hvoWs">The hvo of the writing system.</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="cache">The cache from which the writing system factory is obtained.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public static Font GetFontForNormalStyle(int hvoWs,
			IVwStylesheet styleSheet, LcmCache cache)
		{
			return GetFontForNormalStyle(hvoWs, styleSheet,
				cache.WritingSystemFactory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the specified stylesheet.
		/// This is duplicated in SimpleRootSite/EditingHelper.
		/// </summary>
		/// <param name="hvoWs"></param>
		/// <param name="styleSheet"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Font GetFontForNormalStyle(int hvoWs, IVwStylesheet styleSheet,
			ILgWritingSystemFactory wsf)
		{
			ITsTextProps ttpNormal = styleSheet.NormalFontStyle;
			string styleName = StyleServices.NormalStyleName;
			if (ttpNormal != null)
				styleName = ttpNormal.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);

			return GetFontForStyle(styleName, styleSheet, hvoWs, wsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine a stylesheet from a PropertyTable. Currently this is done by looking for the main window
		/// and seeing whether it has a StyleSheet property that returns one. (We use reflection
		/// because the relevant classes are in DLLs we can't reference.)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static LcmStyleSheet StyleSheetFromPropertyTable(IPropertyRetriever propertyTable)
		{
			Form mainWindow = propertyTable.GetValue<Form>("window");
			PropertyInfo pi = null;
			if (mainWindow != null)
			{
				string areaChoice = propertyTable.GetStringProperty("areaChoice", null);
				if (areaChoice != null && areaChoice.ToLowerInvariant() == "notebook")
					pi = mainWindow.GetType().GetProperty("AnthroStyleSheet");
				if (pi == null)
					pi = mainWindow.GetType().GetProperty("StyleSheet");
			}
			if (pi != null)
				return pi.GetValue(mainWindow, null) as LcmStyleSheet;
			return propertyTable.GetValue<LcmStyleSheet>("LcmStyleSheet");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Grow the dialog's height by delta, and adjust any controls that need it.  The grower
		/// Control is adjusted outside this method.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="delta"></param>
		/// <param name="grower"></param>
		/// ------------------------------------------------------------------------------------
		public static void GrowDialogAndAdjustControls(Control parent, int delta, Control grower)
		{
			if (delta == 0)
				return;
			parent.Height += delta;
			foreach (Control c in parent.Controls)
			{
				if (c == grower)
					continue;
				bool anchorTop = ((((int)c.Anchor) & ((int)AnchorStyles.Top)) != 0);
				bool anchorBottom = ((((int)c.Anchor) & ((int)AnchorStyles.Bottom)) != 0);
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
