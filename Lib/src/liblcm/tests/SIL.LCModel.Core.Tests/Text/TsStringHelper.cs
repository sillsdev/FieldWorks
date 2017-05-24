// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.LCModel.Core.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsStringHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TsStringHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsStrings
		/// </summary>
		/// <param name="tssExpected">expected</param>
		/// <param name="tssActual">actual</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the TsStrings are different, or null if they are the same</param>
		/// <returns>True if TsStrings match, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool TsStringsAreEqual(ITsString tssExpected, ITsString tssActual, out string sHowDifferent)
		{
			return TsStringsAreEqual(tssExpected, tssActual, new Dictionary<int, int>(), out sHowDifferent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsStrings
		/// </summary>
		/// <param name="tssExpected">expected</param>
		/// <param name="tssActual">actual</param>
		/// <param name="propsWithWiggleRoom">dictionary of format properties that needn't be exact, along with their acceptable errors</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the TsStrings are different, or null if they are the same</param>
		/// <returns>True if TsStrings match, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool TsStringsAreEqual(ITsString tssExpected, ITsString tssActual, IDictionary<int, int> propsWithWiggleRoom,
			out string sHowDifferent)
		{
			sHowDifferent = null;

			// Deal with cases where one or both of the passed values is null
			if (tssExpected == null)
			{
				if (tssActual != null)
				{
					sHowDifferent = string.Format("TsStrings differ.{0}\tExpected <null>, but was <{1}>.",
						Environment.NewLine, tssActual.Text);
					return false;
				}
				return true;
			}
			if (tssActual == null)
			{
				sHowDifferent = string.Format("TsStrings differ.{0}\tExpected <{1}>, but was <null>.",
					Environment.NewLine, tssExpected.Text);
				return false;
			}

			// If the lengths differ then the TS Strings are different
			if (tssExpected.Length != tssActual.Length)
			{
				sHowDifferent = string.Format("TsString lengths differ.{0}\tExpected <{1}>, but was <{2}>.",
					Environment.NewLine, tssExpected.Text, tssActual.Text);
				return false;
			}

			if (tssExpected.Text != tssActual.Text)
			{
				sHowDifferent = string.Format("TsString text differs.{0}\tExpected <{1}>, but was <{2}>.",
					Environment.NewLine, tssExpected.Text, tssActual.Text);
				return false;
			}

			var bldr = new StringBuilder();
			int cRuns1 = tssExpected.RunCount;
			int cRuns2 = tssActual.RunCount;
			if (cRuns1 != cRuns2)
			{
				bldr.AppendFormat("TsStrings have different number of runs.{0}\tExpected {1} runs, but was {2} runs.",
					Environment.NewLine, cRuns1, cRuns2);
				for (int iRun = 0; iRun < cRuns1 || iRun < cRuns2; iRun++)
				{
					bldr.AppendFormat("{0}\tExpected run {1}:<", Environment.NewLine, iRun + 1);
					if (iRun < cRuns1)
						bldr.Append(tssExpected.get_RunText(iRun));
					bldr.Append(">, but was:<");
					if (iRun < cRuns2)
						bldr.Append(tssActual.get_RunText(iRun));
					bldr.Append(">");
				}
				sHowDifferent = bldr.ToString();
				return false;
			}

			for (int iRun = 0; iRun < cRuns1; iRun++)
			{
				string sRun1 = tssExpected.get_RunText(iRun);
				string sRun2 = tssActual.get_RunText(iRun);
				if (sRun1 != null && sRun1.Length != sRun2.Length)
				{
					sHowDifferent = string.Format("TsStrings differ in length of run {1}.{0}" +
						"\tExpected length={2}, but was length={3}.{0}" +
						"\texpected run:<{4}>{0}" +
						"\t     but was:<{5}>", Environment.NewLine, iRun + 1, sRun1.Length, sRun2.Length,
						sRun1, sRun2);
					return false;
				}

				string sDetails;
				if (!TsTextPropsHelper.PropsAreEqual(tssExpected.get_Properties(iRun), tssActual.get_Properties(iRun), propsWithWiggleRoom, out sDetails))
				{
					sHowDifferent = string.Format("TsStrings differ in format of run {1}.{0}\t{2}",
						Environment.NewLine, iRun + 1, sDetails);
					return false;
				}
			}

			// if we reach this point, no differences were found
			return true;
		}
	}
}
