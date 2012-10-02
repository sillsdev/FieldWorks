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
		public void SetThen (Context thenCon)
		{
			isNotNull(thenCon,"A then clause is null");
			m_then = thenCon;
		}
		public void SetElse (Context elseCon)
		{
			isNotNull(elseCon,"An else clause is null");
			m_else = elseCon;
		}
		public void AddCondition (Condition cond)
		{
			isNotNull(cond,"An if condition clause is null");
			m_conditions.Add(cond);
		}
		public bool Result
		{
			get {return m_Result;}
			set {m_Result = value;}
		}
		public override void Execute()
		{
			base.Execute();
			Context con = (Context)Ancestor(typeof(Context));
			isNotNull(con, makeNameTag() + " must occur in some context");
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
						if (rest != null) thenCon.Rest = Convert.ToInt32(rest);
						foreach (XmlNode child in xThen.ChildNodes)
						{ // MakeShell adds the ins to thenCon
							XmlInstructionBuilder.MakeShell(child, thenCon);
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
						if (rest != null) elseCon.Rest = Convert.ToInt32(rest);
						foreach (XmlNode child in xElse.ChildNodes)
						{ // MakeShell adds the ins to elseCon
							XmlInstructionBuilder.MakeShell(child, elseCon);
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
