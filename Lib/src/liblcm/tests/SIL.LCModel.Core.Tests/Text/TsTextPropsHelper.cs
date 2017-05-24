// Copyright (c) 2003-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.LCModel.Core.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsTextPropsHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TsTextPropsHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsTextProps
		/// </summary>
		/// <param name="ttp1">expected</param>
		/// <param name="ttp2">actual</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the props are different, or indicating no difference</param>
		/// <returns>True if they contain the same props, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool PropsAreEqual(ITsTextProps ttp1, ITsTextProps ttp2, out string sHowDifferent)
		{
			return PropsAreEqual(ttp1, ttp2, new Dictionary<int, int>(), out sHowDifferent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsTextProps
		/// </summary>
		/// <param name="ttp1">expected</param>
		/// <param name="ttp2">actual</param>
		/// <param name="propsWithWiggleRoom">dictionary of format properties that needn't be exact, along with their acceptable errors</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the props are different, or indicating no difference</param>
		/// <returns>True if they contain the same props, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool PropsAreEqual(ITsTextProps ttp1, ITsTextProps ttp2, IDictionary<int, int> propsWithWiggleRoom, out string sHowDifferent)
		{
			// check how intProps compare
			var cProps1 = ttp1.IntPropCount;
			var cProps2 = ttp2.IntPropCount;
			for (var iprop = 0; iprop < cProps1; iprop++)
			{
				int nVar1, nVar2; // variation info
				int tpt; // prop type
				var tpv1 = ttp1.GetIntProp(iprop, out tpt, out nVar1); // prop values
				var tpv2 = ttp2.GetIntPropValues(tpt, out nVar2); // prop values

				if (!PropsAreEqual(tpv1, tpv2, nVar1, nVar2, tpt, propsWithWiggleRoom))
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
			var s1Count = ttp1.StrPropCount;
			var s2Count = ttp2.StrPropCount;
			for (var iprop = 0; iprop < s1Count; iprop++)
			{
				int strtype;
				var strval1 = ttp1.GetStrProp(iprop, out strtype); // prop values
				var strval2 = ttp2.GetStrPropValue(strtype);

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
			if (s1Count != s2Count)
			{
				sHowDifferent = string.Format("Props differ in count of strProps. "
					+ "Expected <{0}>, but was <{1}>.", s1Count, s2Count);
				return false;
			}

			// if we reach this point, no differences were found
			sHowDifferent = "TextProps objects appear to contain the same properties.";
			return true;
		}

		private static bool PropsAreEqual(int tpv1, int tpv2, int nVar1, int nVar2, int tpt, IDictionary<int, int> propsWithWiggleRoom)
		{
			if (nVar1 != nVar2)
				return false;
			if (tpv1 == tpv2)
				return true;
			if (!propsWithWiggleRoom.ContainsKey(tpt))
				return false;
			var wiggleRoom = propsWithWiggleRoom[tpt];
			Console.WriteLine("Values for TsTextProp {0} are unequal, but are allowed {1} units of wiggle room", tpt, wiggleRoom);
			return tpv1 <= tpv2 + wiggleRoom && tpv1 >= tpv2 - wiggleRoom;
		}
	}
}
