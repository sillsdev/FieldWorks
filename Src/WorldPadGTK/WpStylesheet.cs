/*
 *    WpStylesheet.cs
 *
 *    A C# port of the WpStylesheet
 *
 *    Tom Hindle - 2008-07-08
 *
 *    $Id$
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.WorldPad
{

	/// <summary>
	/// Holds various constants.
	/// </summary>
	public static class Global
	{

		public const string g_pszwStyleNormal = "Normal";
		public const string g_pszwStyleHeading1 = "Heading 1";
		public const string g_pszwStyleHeading2 = "Heading 2";
		public const string g_pszwStyleHeading3 = "Heading 3";
		public const string g_pszDefaultSerif = "<default serif>";

		// For storing styles from stylesheet as owned objects of the main text;
		// using a number under 10000 is supposed to keep this from conflicting.
		public const int kflidStText_Styles = 100;
	}

	struct HvoClsid
	{
		int hvo;
		int clsid;
	}

	enum WpStyleType {
		khvoText = 1,
		khvoParaMin = 10,
		khvoStyleMin = 100000,
	}

	enum WpDataType {
		kfietUnknown = 1,
		kfietXml,
		kfietTemplate,
		kfietUtf16,
		kfietUtf8,
		kfietAnsi,
		//kfietHtml,

		kfietLim,

		kfietPlain = kfietUtf16,
	}

	enum WpStatusType {
		kflsOkay,     // error-free load
		kflsPartial,  // partial load, with errors
		kflsAborted   // nothing loaded
	}

	/// <summary>
	/// WorldPad Stylesheet. Records styling information for a WorldPad document.
	/// </summary>
	public class WpStylesheet : SIL.FieldWorks.FDO.FwStyleSheet, ISimpleStylesheet
	{
		/// <summary>
		/// The next available HVO number to use when a new HVO number for a style is desired.
		/// </summary>
		protected int m_hvoNextStyle;

		/// <summary>
		/// Initialize the stylesheet for a new document.
		/// </summary>
		/// <param name="sda">data access to use</param>
		public void Init(ISilDataAccess sda)
		{
			base.Init(new FdoMemoryCache(sda), (int)WpStyleType.khvoText, Global.kflidStText_Styles,
				false);
			this.DataAccess = sda;
			this.m_hvoNextStyle = (int)WpStyleType.khvoStyleMin;

			AddNormalParaStyle();
		}

		/// <summary>
		/// Create a paragraph style called "Normal". For now just use a minimal (ie, empty)
		/// set of text properties.
		/// </summary>
		public void AddNormalParaStyle()
		{
			int hvoNormal = this.GetNewStyleHVO();

			ITsPropsBldr propsBuilder = new TsPropsBldr();
			ITsTextProps tps = propsBuilder.GetTextProps();

			this.PutStyle(Global.g_pszwStyleNormal, Global.g_pszwStyleNormal, hvoNormal, 0, 0,
				(int)StyleType.kstParagraph, true, false, tps);

			((VwUndoDa)this.DataAccess).CacheObjProp(hvoNormal,
				(int)CellarModuleDefns.kflidStStyle_Next, hvoNormal);
		}

		/// <summary>
		/// "Set the list of style objects to the (fake) styles attribute of StText.
		/// Called from the XML import routine."
		///
		/// Load specified styles into the stylesheet from the DA.
		/// Replaces existing non-built-in styles. Implements ISimpleStylesheet method.
		/// </summary>
		/// <param name="newStyleHvos">Array of new styles to load.</param>
		/// <param name="newStyleHvos_length">length of array</param>
		/// <param name="nextStyleHvo">next-style-hvo to set in this stylesheet, unless existing
		/// m_hvoNextStyle setting is bigger</param>
		public void AddLoadedStyles(int[] newStyleHvos, int newStyleHvos_length, int nextStyleHvo)
		{
			// Check arguments
			if (null == newStyleHvos)
			{
				string msg = "Error: WpStylesheet.AddLoadedStyles argument newStyleHvos can't be null.";
				Console.WriteLine(msg);
				throw new ArgumentException(msg);
			}
			if (newStyleHvos_length < 0)
			{
				string msg = "Error: WpStylesheet.AddLoadedStyles argument newStyleHvos_length can't be < 0.";
				Console.WriteLine(msg);
				throw new ArgumentOutOfRangeException(msg);
			}
			if (nextStyleHvo < 0)
			{
				string msg = "Error: WpStylesheet.AddLoadedStyles argument nextStyleHvo can't be < 0.";
				Console.WriteLine(msg);
				throw new ArgumentOutOfRangeException(msg);
			}

			// Process
			int cst = m_fdoCache.GetVectorSize((int)WpStyleType.khvoText, Global.kflidStText_Styles);
			((VwUndoDa)this.DataAccess).CacheReplace((int)WpStyleType.khvoText,
				Global.kflidStText_Styles, 0, cst, newStyleHvos, newStyleHvos_length);

			DeleteAllStyles();

			LoadStyles(true);

			m_hvoNextStyle = Math.Max(m_hvoNextStyle, nextStyleHvo);

			// If the Normal style is not one of the styles in newStyleHvosArr, then add it to
			// the stylesheet and DA.
			if (!ContainsNormalStyle(newStyleHvos))
				AddNormalParaStyle();
		}

		/// <summary>
		/// Delete all styles from the stylesheet, excluding any built-in styles.
		/// This does not delete the styles in the DA.
		/// </summary>
		public void DeleteAllStyles()
		{
			try {
				foreach (BaseStyleInfo style in base.Styles)
				{
					if (!style.IsBuiltIn)
						base.Delete(style.RealStyle.Hvo);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(
					"Warning: WpStylesheet.DeleteAllStyles: An exception was caught: {0}", e.Message);
			}
		}

		/// <summary>Implements ISimpleStylesheet method</summary>
		public void FixStyleReferenceAttrs(int flid, System.IntPtr hmhvostu)
		{
			Console.WriteLine("Warning: This WpStylesheet.FixStyleReferenceAttrs is not implemented.");
			throw new NotImplementedException();
		}

		/// <summary>
		/// For each style in stylesToModify, set the attribute flid in the DA to be the hvo in
		/// attributeValuesToSet.
		/// This is called during wpx loading to set Based-On and Next-Style attributes.
		/// This is based on C++ WpStylesheet.cpp.
		/// Implements ISimpleStylesheet method.
		/// </summary>
		/// <param name="flid">attribute to set, such as 'next' ((int)CellarModuleDefns.kflidStStyle_Next)
		/// or 'basedOn' ((int)CellarModuleDefns.kflidStStyle_BasedOn)</param>
		/// <param name="stylesToModify">hvos of styles to modify the flid of</param>
		/// <param name="attributeValuesToSet">hvos of styles, to be writen into the flid
		/// values of stylesToModify</param>
		/// <param name="count">length of each of stylesToModify and attributeValuesToSet</param>
		public void FixStyleReferenceAttrs(int flid, int[] stylesToModify,
			int[] attributeValuesToSet, int count)
		{
			Debug.WriteLine(String.Format("WpStylesheet.FixStyleReferenceAttrs({0}, {1}, {2}, {3})",
				flid, stylesToModify, attributeValuesToSet, count));

			// Check arguments
			if (null == stylesToModify || null == attributeValuesToSet)
			{
				string msg = "Warning: WpStylesheet.FixStyleReferenceAttrs argument stylesToModify or "
					+ "attributeValuesToSet is invalid. It should not be null. Aborting method.";
				Console.WriteLine(msg);
				return;
			}
			if (count == 0)
			{
				string msg = "Warning: WpStylesheet.FixStyleReferenceAttrs argument count is invalid. "
					+ "It should not be 0. Aborting method.";
				Console.WriteLine(msg);
				return;
			}
			if (count < 0)
			{
				string msg = "Error: WpStylesheet.FixStyleReferenceAttrs argument count is invalid. "
					+ "It shouldn't be less than 0.";
				Console.WriteLine(msg);
				throw new ArgumentException(msg);
			}

//			// Marshal input
//			int[] stylesToModify_;
//			int[] attributeValuesToSet_;
//			try
//			{
//				stylesToModify_ = (int[])MarshalEx.NativeToArray(stylesToModify, count, typeof(int));
//				attributeValuesToSet_ =
//					(int[])MarshalEx.NativeToArray(attributeValuesToSet, count, typeof(int));
//			}
//			catch (Exception e)
//			{
//				string msg = "Error: WpStylesheet.FixStyleReferenceAttrs argument stylesToModify or" +
//					" attributeValuesToSet is probably invalid. There was a problem marshalling" +
//					" the data. Exception message is: " + e.Message;
//				Console.WriteLine(msg);
//				throw new ArgumentException(msg);
//			}

			// Process

			// Look through all the styles in the stylesheet
			foreach (BaseStyleInfo style in base.Styles)
			{
				// If this particular style is one we should modify the attribute of..
				int where = Array.IndexOf(stylesToModify, style.RealStyle.Hvo);
				if (where >= 0)
				{
					// What value should we put in the attribute?
					int hvoValueToSet = attributeValuesToSet[where];

					((VwUndoDa)this.DataAccess).CacheObjProp(style.RealStyle.Hvo, flid, hvoValueToSet);
				}
			}
		}

		/// <summary>
		/// Recalculate the text properties with the recomputed derived style properties.
		/// </summary>
		public void FinalizeStyles()
		{
			ComputeDerivedStyles();
		}

		/// <summary>
		/// Initialize a style with an empty text properties object.
		/// Called from the XML import routines.
		/// </summary>
		/// <param name="hvoStyle">style to initialize</param>
		public void AddEmptyTextProps(int hvoStyle)
		{
			// Check arguments
			if (hvoStyle < 0)
			{
				string msg = "Error: WpStylesheet.AddEmptyTextProps argument count is invalid. "
					+ "It shouldn't be less than 0.";
				Console.WriteLine(msg);
				throw new ArgumentException(msg);
			}

			// Process
			ITsPropsBldr propsBuilder = new TsPropsBldr();
			ITsTextProps textProps = propsBuilder.GetTextProps();
			((VwUndoDa)this.DataAccess).CacheUnknown(hvoStyle,
				(int)CellarModuleDefns.kflidStStyle_Rules, textProps);
		}

		/// <summary>
		/// Gets the next style HVO, which is the next available HVO number to use when a new HVO
		/// number for a style is desired.
		/// Implements ISimpleStylesheet method.
		/// </summary>
		public int NextStyleHVO()
		{
			return m_hvoNextStyle;
		}

		/// <summary>
		/// Create a new object for a style, owned in the (fake) styles attribute of StText.
		/// Return the new HVO.
		/// Ported from C++ WpStylesheet::GetNewStyleHVO()
		/// For some reason, just calling base.GetNewStyleHvo() doesn't work.
		/// </summary>
		/// <returns>A new HVO</returns>
		public int GetNewStyleHVO()
		{
			int rv = 0;
			int cst = this.DataAccess.get_VecSize((int)WpStyleType.khvoText, Global.kflidStText_Styles);
			rv =  this.m_hvoNextStyle++;
			int[] array = new int[100];
			array[0] = rv;
			((VwUndoDa)this.DataAccess).CacheReplace((int)WpStyleType.khvoText,
				Global.kflidStText_Styles, cst, cst, array, 1);

			return rv;
		}

		/// <summary>
		/// Get all styles this stylesheet supports.
		/// Implements ISimpleStylesheet method
		/// </summary>
		/// <returns>Array of style HVOs</returns>
		public System.IntPtr GetStyles()
		{
			Console.WriteLine("Error: WpStylesheet.GetStyles is not implemented.");
			//return System.IntPtr.Zero;
			throw new NotImplementedException();
		}

		/// <summary>
		/// Print out all styles in this stylesheet, and all styles in the DA.
		/// Useful for debugging.
		/// </summary>
		public void PrintStyles()
		{
			Console.WriteLine("WpStylesheet.PrintStyles:");
			Console.WriteLine("WpStylesheet.PrintStyles Styles in stylesheet\n\tHVO\tName");
			foreach (BaseStyleInfo style in base.Styles)
			{
				Console.WriteLine("\t{0}\t{1}", style.RealStyle.Hvo, style.RealStyle.Name);
			}

			Console.WriteLine("WpStylesheet.PrintStyles Styles in DA\n\tHVO\tName");

			int startingHvo = (int)WpStyleType.khvoStyleMin;
			int countOfHvos = 1 + this.DataAccess.get_VecSize((int)WpStyleType.khvoText,
				Global.kflidStText_Styles); //'1+' in case there is an extra Normal style

			for (int hvo = startingHvo; hvo < (startingHvo + countOfHvos); hvo++)
			{
				string styleName = this.DataAccess.get_UnicodeProp(hvo,
					(int)CellarModuleDefns.kflidStStyle_Name);

				Console.WriteLine(String.Format("\t{0}\t{1}", hvo, styleName));
			}
		}

		/// <summary>
		/// Get all styles this stylesheet supports.
		/// Implements ISimpleStylesheet method.
		/// </summary>
		/// <param name="maximumNumberOfStyleHvos">Allocated length of styleHvos.
		/// Must be >=0.</param>
		/// <param name="styleHvosArr">An allocated int[] to store the style HVOs</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// if maximumNumberOfStyleHvos is < 0</exception>
		/// <exception cref="ArgumentOutOfRangeException">if maximumNumberOfStyleHvos is too small
		/// to hold all HVOs</exception>
		public void GetArrayOfStyleHvos(int maximumNumberOfStyleHvos, ArrayPtr styleHvosArr)
		{
			// Check arguments
			if (maximumNumberOfStyleHvos < 0)
				throw new ArgumentOutOfRangeException(
					"WpStylesheet.GetArrayOfStyleHvos argument maximumNumberOfStyleHvos must be >= 0.");
			if (maximumNumberOfStyleHvos < base.Styles.Count)
				throw new ArgumentOutOfRangeException(
					"WpStylesheet.GetArrayOfStyleHvos argument maximumNumberOfStyleHvos is not large " +
					"enough to store all style HVOs. Needs to be at least '" + base.Styles.Count + "'");

			// Marshal input
			int[] styleHvos = (int[])MarshalEx.NativeToArray(styleHvosArr, maximumNumberOfStyleHvos,
				typeof(int));

			// Process
			int whichStyle=0;
			foreach (BaseStyleInfo style in base.Styles)
			{
				styleHvos[whichStyle++] = style.RealStyle.Hvo;
				Debug.WriteLine(String.Format("WpStylesheet.GetArrayOfStyleHvos style: {0} {1}",
					style.RealStyle.Name, style.RealStyle.Hvo));
			}

			// Write results to output argument
			Marshal.Copy(styleHvos, 0, styleHvosArr.IntPtr, maximumNumberOfStyleHvos);
		}

		/// <summary>Gets the HVO of a style by name.
		/// Implements ISimpleStylesheet method.</summary>
		/// <param name="styleName">Name of style to look for</param>
		/// <returns>HVO of named style, or -1 if not found</returns>
		public int GetStyleHvoByName(string styleName)
		{
			IStStyle style = FindStyle(styleName);
			if (null == style)
				return -1;
			return style.Hvo;
		}

		/// <summary>
		/// Get style object corresponding to an hvo. Will inspect the style objects in this
		/// stylesheet. Return null if not found.
		/// (Is there not a way to do this in the existing FW code?)
		/// </summary>
		private BaseStyleInfo GetStyleByHvo(int hvo)
		{
			foreach (BaseStyleInfo style in base.Styles)
			{
				if (hvo == style.RealStyle.Hvo)
					return style;
			}
			return null;
		}

		/// <summary>
		/// Does an array of HVOs contain the HVO of a style with name "Normal"?
		/// </summary>
		/// <param name="styleHvos">array of HVOs to inspect</param>
		/// <returns>true if an HVO in styleHvos is the HVO of a style with name "Normal";
		/// otherwise false</returns>
		private bool ContainsNormalStyle(int[] styleHvos)
		{
			foreach (int hvo in styleHvos)
			{
				BaseStyleInfo style = GetStyleByHvo(hvo);
				if (null != style && style.Name == Global.g_pszwStyleNormal)
					return true;
			}
			return false;
		}
	}
}
