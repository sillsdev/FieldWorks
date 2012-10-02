// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TsTextPropsHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsTextPropsHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TsTextPropsHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No public constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TsTextPropsHelper()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsTextProps
		/// </summary>
		/// <param name="ttp1">expected</param>
		/// <param name="ttp2">actual</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the props are
		/// different, or indicating no difference</param>
		/// <returns>True if they contain the same props, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool PropsAreEqual(ITsTextProps ttp1, ITsTextProps ttp2,
			out string sHowDifferent)
		{
			// check how intProps compare
			int cProps1 = ttp1.IntPropCount;
			int cProps2 = ttp2.IntPropCount;
			int tpv1, tpv2; // prop values
			int nVar1, nVar2; // variation info
			int tpt; // prop type
			for (int iprop = 0; iprop < cProps1; iprop++)
			{
				tpv1 = ttp1.GetIntProp(iprop, out tpt, out nVar1);
				tpv2 = ttp2.GetIntPropValues(tpt, out nVar2);

				if (tpv1 != tpv2 || nVar1 != nVar2)
				{
					if (tpt == (int)FwTextPropType.ktptWs)
						sHowDifferent = string.Format("Props differ in ktptWs property. "
							+ "Expected ws <{0}> and var <{1}>, but was ws <{2}> and var <{3}>.", tpv1, nVar1, tpv2, nVar2);
					else
						sHowDifferent = string.Format("Props differ in intProp type {0}. "
							+ "Expected <{1},{2}>, but was <{3},{4}>.", tpt, tpv1, nVar1, tpv2, nVar2);
					return false;
				}
			}
			// if count of intProps differs, it will be difficult to report exact difference
			//  so just issue a simple response for now
			if (cProps1 != cProps2)
			{
				sHowDifferent = string.Format("Props differ in count of intProps. "
					+ "Expected <{0}>, but was <{1}>.", cProps1, cProps2);
				return false;
			}

			// check for string properties differences
			int s1count = ttp1.StrPropCount;
			int s2count = ttp2.StrPropCount;
			int strtype;
			string strval1, strval2; // prop values
			for (int iprop = 0; iprop < s1count; iprop++)
			{
				strval1 = ttp1.GetStrProp(iprop, out strtype);
				strval2 = ttp2.GetStrPropValue(strtype);

				if (strval1 != strval2)
				{
					if (strtype == (int)FwTextPropType.ktptNamedStyle)
						sHowDifferent = string.Format("Props differ in ktptNamedStyle property. "
							+ "Expected <{0}>, but was <{1}>.", strval1, strval2);
					else if (strtype == (int)FwTextPropType.ktptObjData)
						sHowDifferent = string.Format("Props differ in ktptObjData property. "
							+ "Expected <{0}>, but was <{1}>.", strval1, strval2);
							// we could detail the objectDataType and Guid if needed
					else
						sHowDifferent = string.Format("Props differ in strProp type {0}. "
							+ "Expected <{1}>, but was <{2}>.", strtype, strval1, strval2);
					return false;
				}
			}
			// if count of strProps differs, it will be difficult to report exact difference
			//  so just issue a simple response for now
			if (s1count != s2count)
			{
				sHowDifferent = string.Format("Props differ in count of strProps. "
					+ "Expected <{0}>, but was <{1}>.", s1count, s2count);
				return false;
			}

			// if we reach this point, no differences were found
			sHowDifferent = "TextProps objects appear to contain the same properties.";
			return true;
		}
	}
}
