// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MatchStrings.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//  This class is a test driver instruction that compares UTF-8 or World Pad XML strings.
//  These strings are typically obtained using the select-text instruction.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
//using System.Diagnostics;
using System.Text;
using System.Xml;
using System.IO;
//using System.Windows.Forms;
//using System.Collections;
using Microsoft.XmlDiffPatch; // Does the XML comparison and outputs a diff XML file.
//using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Performs a match of two XML strings from variables stored in two previously executed
	/// instruction elements or literals. The result of the match is stored in a local variable
	/// called "diffs". When the expect attribute is present, the diffs variable is compared
	/// to it. If they are identical, the check passes. If there is no expect attribute,
	/// then the check passes if the diffs variable is empty. The pass/fail decision is stored
	/// in a variable called "result". The value of this variable is returned when this element
	/// is referred to by its id.
	/// </summary>
	public class MatchStrings: CheckBase
	{
		string m_of        = null;
		string m_to        = null;
		string m_expect    = null;
		string m_baseStr   = null;
		string m_targetStr = null;
		string m_diffs     = null;

		public MatchStrings(): base()
		{
			m_diffs = "<notCompared/>";
			m_tag   = "match-strings";
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			m_log.isNotNull(Of, "Match-strings instruction must have an 'of'.");
			m_log.isTrue(Of != "", "Match-strings instruction must have a non-empty 'of'.");
			m_log.isNotNull(To, "Match-strings instruction must have a 'to'.");
			m_log.isTrue(To != "", "Match-strings instruction must have a non-empty 'to'.");
			InterpretMessage(xn.ChildNodes);
			return true;
		}

		public string Of
		{
			get {return m_of;}
			set {m_of = value;}
		}

		public string To
		{
			get {return m_to;}
			set {m_to = value;}
		}

		public string Expect
		{
			get {return m_expect;}
			set {m_expect = value;}
		}

		public override void Execute()
		{
			base.Execute();
			PassFailInContext(m_onPass,m_onFail,out m_onPass,out m_onFail);
			m_baseStr   = Utilities.evalExpr(m_of);
			m_targetStr = Utilities.evalExpr(m_to);
			// m_baseStr and m_targetStr can be null
			XmlStringDiff xsDiff = new XmlStringDiff(m_baseStr, m_targetStr);
			m_diffs = xsDiff.getDiffString();
			m_Result = xsDiff.AreEqual();
			// Does it really want diffs from expected diffs?
			if (m_expect != null)
			{
				XmlStringDiff xsDiffEx = new XmlStringDiff(m_expect, m_diffs);
				m_diffs = xsDiffEx.getDiffString();
				m_Result = xsDiffEx.AreEqual();
			}
			if ((m_onPass == "assert" && m_Result == true)
				||(m_onFail == "assert" && m_Result == false) )
			{
				if (m_message != null)
					m_log.fail(m_message.Read());
				else
					m_log.fail("Match-strings Assert: Result = '" + m_Result + "', on-pass='" + m_onPass + "', on-fail='" + m_onFail + "', diffs='" + m_diffs + "'");
			}
			m_log.result(this);
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
				case "diffs":		return m_diffs;
				case "of":			return m_of;
				case "to":			return m_to;
				case "expect":		return m_expect;
				case "baseStr":		return m_baseStr;
				case "targetStr":	return m_targetStr;
				default:		return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_of != null)   image += @" of="""+Utilities.attrText(m_of)+@"""";
			if (m_to != null) image += @" to="""+Utilities.attrText(m_to)+@"""";
			if (m_expect != null)  image += @" expect="""+Utilities.attrText(m_expect)+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_baseStr != null)   image += @" base="""+Utilities.attrText(m_baseStr)+@"""";
			if (m_targetStr != null) image += @" target=""" + Utilities.attrText(m_targetStr) + @"""";
			if (m_diffs != null) image += @" diffs=""" + Utilities.attrText(m_diffs) + @"""";
			return image;
		}
	}
}
