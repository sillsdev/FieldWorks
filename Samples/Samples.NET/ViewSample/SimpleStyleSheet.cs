// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Samples.ViewSample
{
	/// <summary>
	/// SimpleStyleSheet is a very minimal implementation of a stylesheet, just enough so that we can
	/// cache some styles and return them. Currently we only know two styles, normal and "verseNumber"
	/// </summary>
	public class SimpleStyleSheet : IVwStylesheet
	{
		ITsTextProps m_ttpNormal;
		ITsTextProps m_ttpVerseNumber;

		public SimpleStyleSheet()
		{
			ITsPropsBldr tpb = (ITsPropsBldr) new FwKernelLib.TsPropsBldrClass();
			m_ttpNormal = tpb.GetTextProps(); // normal has nothing defined, use system defaults.
			tpb.SetIntPropValues((int)FwKernelLib.FwTextPropType.ktptForeColor, (int)FwKernelLib.FwTextPropVar.ktpvDefault,
				(int)ViewSampleVc.RGB(Color.Red));
			tpb.SetIntPropValues((int)FwKernelLib.FwTextPropType.ktptSuperscript, (int)FwKernelLib.FwTextPropVar.ktpvEnum,
				(int)FwKernelLib.FwSuperscriptVal.kssvSuper);
			m_ttpVerseNumber = tpb.GetTextProps();
		}
		#region IVwStylesheet Members

		public int GetRole(string bstrName)
		{
			// TODO:  Add SimpleStyleSheet.GetRole implementation
			return 0;
		}

		public int get_NthStyle(int ihvo)
		{
			// TODO:  Add SimpleStyleSheet.get_NthStyle implementation
			return 0;
		}

		public int get_CStyles()
		{
			// TODO:  Add SimpleStyleSheet.get_CStyles implementation
			return 0;
		}

		public bool IsPublishedTextStyle(string bstrName)
		{
			// TODO:  Add SimpleStyleSheet.IsPublishedTextStyle implementation
			return false;
		}

		public bool IsModified(string bstrName)
		{
			// TODO:  Add SimpleStyleSheet.IsModified implementation
			return false;
		}

		public string GetBasedOn(string bstrName)
		{
			if (bstrName == "verseNumber")
				return "Normal";
			return null;
		}

		public string get_NthStyleName(int ihvo)
		{
			// TODO:  Add SimpleStyleSheet.get_NthStyleName implementation
			return null;
		}

		public void CacheProps(int cch, string _rgchName, int hvoStyle, ITsTextProps _ttp)
		{
			// TODO:  Add SimpleStyleSheet.CacheProps implementation
		}

		public ISilDataAccess get_DataAccess()
		{
			// TODO:  Add SimpleStyleSheet.get_DataAccess implementation
			return null;
		}

		public void PutStyleRgch(int cch, string _rgchName, int hvoStyle, int hvoBasedOn, int hvoNext, int nType, bool fPublishedTextStyle, bool fBuiltIn, bool fModified, ITsTextProps _ttp)
		{
			// TODO:  Add SimpleStyleSheet.PutStyleRgch implementation
		}

		public ITsTextProps get_NormalFontStyle()
		{
			return m_ttpNormal;
		}

		public void Delete(int hvoStyle)
		{
			// TODO:  Add SimpleStyleSheet.Delete implementation
		}

		public bool IsBuiltIn(string bstrName)
		{
			return true;
		}

		public bool get_IsStyleProtected(string bstrName)
		{
			return true;
		}

		public string GetNextStyle(string bstrName)
		{
			return bstrName;
		}

		public ITsTextProps GetStyleRgch(int cch, string _rgchName)
		{
			if (_rgchName == "verseNumber")
				return m_ttpVerseNumber;
			return m_ttpNormal;
		}

		public int MakeNewStyle()
		{
			// TODO:  Add SimpleStyleSheet.MakeNewStyle implementation
			return 0;
		}

		public int GetType(string bstrName)
		{
			// TODO:  Add SimpleStyleSheet.GetType implementation
			return 0;
		}

		#endregion
	}
}
