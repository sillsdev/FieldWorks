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
// File: If.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Xml;
namespace GuiTestDriver
{
	/// <summary>
	/// A simple conditional instruction.
	/// </summary>
	public class If : Instruction
	{
		ArrayList m_conditions = null;
		Context m_then = null;
		Context m_else = null;
		bool m_Result = true; // if no conditions, it is trivially true

		public If()
		{
			m_conditions = new ArrayList();
			m_tag = "if";
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
			Logger.getOnly().isNotNull(xn["condition"], "If instruction must have a condition.");
			foreach (XmlNode elt in xn.ChildNodes)
			{
				switch (elt.Name)
				{
				case "condition":
					Condition cond = CreateCondition(elt);
					AddCondition(cond);
					break;
				}
			}
			return true;
		}

		public void SetThen (Context thenCon)
		{
			m_log.isNotNull(thenCon, "A then clause is null");
			m_then = thenCon;
		}
		public void SetElse (Context elseCon)
		{
			m_log.isNotNull(elseCon, "An else clause is null");
			m_else = elseCon;
		}
		public void AddCondition (Condition cond)
		{
			m_log.isNotNull(cond, "An if condition clause is null");
			m_conditions.Add(cond);
		}
		public bool Result
		{
			get {return m_Result;}
			set {m_Result = value;}
		}

		/// <summary>
		/// Creates and parses a condition.
		/// </summary>
		/// <param name="xn">The XML repersentation of the instruction to be checked</param>
		/// <returns>A Condition intsruction</returns>
		static Condition CreateCondition(XmlNode xn)
		{
			Condition cond = new Condition();
			cond.Of = XmlFiler.getAttribute(xn, "of"); // can be an id, 'literal' or number
			Logger.getOnly().isNotNull(cond.Of, "Condition must have an 'of' attribute.");
			Logger.getOnly().isTrue(cond.Of != "", "Condition must have a non-null 'of' attribute.");
			cond.Is = XmlFiler.getAttribute(xn, "is"); // can be equal, true, false, 'literal' or number
			Logger.getOnly().isNotNull(cond.Is, "Condition must have an 'is' attribute.");
			Logger.getOnly().isTrue(cond.Is != "", "Condition must have a non-null 'is' attribute.");
			cond.To = XmlFiler.getAttribute(xn, "to"); // can be an id, 'literal' or number

			foreach (XmlNode condElt in xn.ChildNodes)
			{
				switch (condElt.Name)
				{
				case "condition":
					Condition condChild = CreateCondition(condElt);
					cond.AddCondition(condChild);
					break;
				}
			}
			return cond;
		}

		public override void Execute()
		{
			base.Execute();
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + " must occur in some context");
			m_Result = Condition.EvaluateList(m_conditions);
			if (m_Result == true)
			{
				XmlNode xThen = m_elt.SelectSingleNode("then");
				if (xThen != null)
				{ // then may have been created via do-once before this
					if (m_then == null)
					{ // not created yet
						Context thenCon = new Context();
						thenCon.ModelNode = con.ModelNode;
						thenCon.Parent = this;
						string rest = XmlFiler.getAttribute(xThen, "wait");
						if (rest != null) thenCon.Wait = Convert.ToInt32(rest);
						foreach (XmlNode child in xThen.ChildNodes)
						{ // MakeShell adds the ins to thenCon
							Instructionator.MakeShell(child, thenCon);
						}
						SetThen(thenCon);
					}
					m_then.Execute();
				}
			}
			else
			{
				XmlNode xElse = m_elt.SelectSingleNode("else");
				if (xElse != null)
				{ // else may have been created via do-once before this
					if (m_else == null)
					{ // not created yet
						Context elseCon = new Context();
						elseCon.ModelNode = con.ModelNode;
						elseCon.Parent = this;
						string rest = XmlFiler.getAttribute(xElse, "wait");
						if (rest != null) elseCon.Wait = Convert.ToInt32(rest);
						foreach (XmlNode child in xElse.ChildNodes)
						{ // MakeShell adds the ins to elseCon
							Instructionator.MakeShell(child, elseCon);
						}
						SetElse(elseCon);
					}
					m_else.Execute();
				}
			}
			Logger.getOnly().result(this);
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
				case "result":	return m_Result.ToString();
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
			image += @" conditions=""";
			foreach (Condition cond in m_conditions) image += cond.image();
			image += @"""";
			if (m_then != null) image += @" then=""body present""";
			if (m_else != null) image += @" else=""body present""";
			image += @" else="""+m_Result+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			image += @" result="""+m_Result+@"""";
			return image;
		}
	}
}
