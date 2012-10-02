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
// File: TsStringHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;
using NMock.Constraints;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsStringHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TsStringHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No public constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TsStringHelper()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a TSS string from a text string.
		/// </summary>
		/// <param name="str">contents of string to be created</param>
		/// <param name="ws">integer representing a writing system</param>
		/// <returns>A new TsString</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString MakeTSS(string str, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(str, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two TsStrings
		/// </summary>
		/// <param name="tssExpected">expected</param>
		/// <param name="tssActual">actual</param>
		/// <param name="sHowDifferent">Human(geek)-readable string telling how the TsStrings
		/// are different, or null if they are the same</param>
		/// <returns>True if TsStrings match, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool TsStringsAreEqual(ITsString tssExpected, ITsString tssActual,
			out string sHowDifferent)
		{
			sHowDifferent = null;

			// Deal with cases where one or both of the passed values is null
			if (tssExpected == null)
			{
				if (tssActual != null)
				{
					sHowDifferent = "TsStrings differ.\n\tExpected <null>, but was <" +
						tssActual.Text + ">.";
					return false;
				}
				else
				{
					return true;
				}
			}
			if (tssExpected != null && tssActual == null)
			{
				sHowDifferent = "TsStrings differ.\n\tExpected <" + tssExpected.Text +
					">, but was <null>.";
				return false;
			}

			// If the lengths differ then the TS Strings are different
			if (tssExpected.Length != tssActual.Length)
			{
				sHowDifferent = "TsString lengths differ.\n\tExpected <" + tssExpected.Text +
					">, but was <" + tssActual.Text + ">.";
				return false;
			}

			if (tssExpected.Text != tssActual.Text)
			{
				sHowDifferent = "TsString text differs.\n\tExpected <" + tssExpected.Text +
					">, but was <" + tssActual.Text + ">.";
				return false;
			}

			int cRuns1 = tssExpected.RunCount;
			int cRuns2 = tssActual.RunCount;
			if (cRuns1 != cRuns2)
			{
				sHowDifferent = "TsStrings have different number of runs.\n\tExpected " +
					cRuns1 + " runs, but was " + cRuns2 + " runs.";
				for (int iRun = 0; iRun < cRuns1 || iRun < cRuns2; iRun++)
				{
					sHowDifferent += "\n\tExpected run " + (iRun + 1) + ":<";
					if (iRun < cRuns1)
						sHowDifferent += tssExpected.get_RunText(iRun);
					sHowDifferent += ">, but was:<";
					if (iRun < cRuns2)
						sHowDifferent += tssActual.get_RunText(iRun);
					sHowDifferent += ">";
				}
				return false;
			}

			for (int iRun = 0; iRun < cRuns1; iRun++)
			{
				string sRun1 = tssExpected.get_RunText(iRun);
				string sRun2 = tssActual.get_RunText(iRun);
				if (sRun1 != null && sRun1.Length != sRun2.Length)
				{
					sHowDifferent = "TsStrings differ in length of run " + (iRun + 1) +
						".\n\tExpected length=" + sRun1.Length + ", but was length=" +
						sRun2.Length + ".\n\texpected run:<" + sRun1 + ">\n\t" +
						"but was:<" + sRun2 + ">";
					return false;
				}

				string sDetails;
				if (!TsTextPropsHelper.PropsAreEqual(tssExpected.get_Properties(iRun),
					tssActual.get_Properties(iRun), out sDetails))
				{
					sHowDifferent = "TsStrings differ in format of run " + (iRun + 1) +
						".\n\t" + sDetails;
					return false;
				}
			}

			// if we reach this point, no differences were found
			return true;
		}
	}
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is for tests where TsString parameters are necessary. It compares the value
	/// to an expected value.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExpectedTsString: BaseConstraint
	{
		private ITsString m_tssExpected;
		string m_sHowDifferent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for creatring a constraint that expects a specified TsString
		/// </summary>
		/// <param name="tssExpected">Expected TsString</param>
		/// ------------------------------------------------------------------------------------
		public ExpectedTsString(ITsString tssExpected)
		{
			m_tssExpected = tssExpected;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Evaluate actual parameter to see if it's what was expected
		/// </summary>
		/// <param name="val">Actaul parameter</param>
		/// <returns><c>true</c> if actual param equals expected</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Eval(object val)
		{
			if (!(val is ITsString))
			{
				m_sHowDifferent = "Actual parameter was not a TsString";
				return false;
			}

			return TsStringHelper.TsStringsAreEqual(m_tssExpected, (ITsString)val,
				out m_sHowDifferent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a message describing the problem with this parameter. (I think this is what
		/// this is supposed to do.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Message
		{
			get	{return "<" + m_sHowDifferent + ">";}
		}
	}
}
