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
// File: Model.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// This impliments multinode select functionality, so <click> etc. can keep to a single node select.
// The Context instruction is:
// <model select="" ... >
//     ....
// </model>
// It loops over one or more selected GUI Model nodes.
// <model> sets the GUI Model node context and allows exposure of some intrinsic <var>s
// like $guiPath, $appPath, $guiRole and maybe $guiName for the current model node context.
// $ScriptPath is the only intrinsic <var> so far available anywhere in the script.
// It is set up in TestState when the script is run.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Xml;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Model.
	/// </summary>
	public class Model : Context
	{
		// instruction attributes
		string m_when;
		// exposed variables
		string m_role;
		string m_name;
		string m_nodeName;
		// state
		Boolean m_created;

		public Model()
		{
			m_tag = "model";
			m_when = null;
			m_role = null;
			m_name = null;
			m_nodeName = null;
			m_created = false;
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
			m_log.isTrue(Select != null, "Model instruction must have a select attribute.");
			m_log.isTrue(Select != "", "Model instruction must have a non-empty select.");
			return true;
		}

		public string When
		{
			get { return m_when; }
			set { m_when = value; }
		}

		public string Role
		{
			get { return m_role; }
		}

		public string Name
		{
			get { return m_name; }
		}

		/// <summary>
		/// Execute this model node context, specified by @select and
		/// creating and executing child instructions.
		/// </summary>
		public override void Execute()
		{
			base.Execute();
			if (m_created)
			{
				Finished = true; // tell do-once it's done
				return; // all has been done in the base Context.Execute().
			}
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + " must occur in some context");
			AccessibilityHelper ah = con.Accessibility;
			m_log.isNotNull(ah, makeNameTag() + " context is not accessible");

			// If there is a @select, select the nodes
			if (m_select != null && m_select != "")
			{ // each node or attribute selected creates a context
				m_select = Utilities.evalExpr(m_select);
				m_log.paragraph(makeNameTag() + " creating selection targets via " + m_select);
				XmlNodeList pathNodes = Instructionator.selectNodes(con, m_select, makeNameTag());
				m_log.isNotNull(pathNodes, makeNameTag() + " select='" + m_select + "' returned no model nodes");
				// The select text may have selected a string that is itself xPath!
				// If so, select on that xPath
				if (pathNodes.Count == 1 && pathNodes.Item(0).NodeType == XmlNodeType.Text)
				{ // this text node should be an xpath statement
					string xPath = pathNodes.Item(0).Value;
					m_log.paragraph(makeNameTag() + " selected a text node with more XPATH: " + xPath);
					pathNodes = Instructionator.selectNodes(con, xPath, makeNameTag() + " selecting " + xPath);
					m_log.isNotNull(pathNodes, makeNameTag() + " selecting " + xPath + " from select='" + m_select + "' returned no model nodes");
				}
				// Create a list of paths to loop over
				Model lastModel = this; // use as an insert reference node
				foreach (XmlNode node in pathNodes)
				{ // build the path via each model node
					XmlPath xPath = new XmlPath(node);
					// xPath may be invalid - it means it has no guiPath
					//if (!xPath.isValid()) fail(makeNameTag() + " XmlPath not constructable from " + node.OuterXml);
					if (1 == m_logLevel)
					{
						m_log.paragraph(makeNameTag() + " appPath " + xPath.xPath());
						m_log.paragraph(makeNameTag() + " guiPath " + xPath.Path);
					}
					Model model = new Model();
					model.m_created = true;
					model.m_modelNode = xPath;
					model.m_path = xPath.Path;
					model.m_select = xPath.xPath();
					model.m_when = m_when;
					model.m_name = XmlFiler.getAttribute(node, "name");
					model.m_role = XmlFiler.getAttribute(node, "role");
					model.m_nodeName = node.Name;
					model.Number = TestState.getOnly().IncInstructionCount;
					model.Id += (model.Number - Number).ToString();
					model.Parent = con;
					con.Add(lastModel, model);
					lastModel = model; // insert the next one after this one.
					m_log.mark(model); // log the progress of interpretation
					// if there is content, add instructions to the new model context
					if (m_elt.HasChildNodes)
					{
						foreach (XmlNode xnChild in m_elt.ChildNodes)
						{ // a side-effect of MakeShell is to add the instruction to the model
							Instructionator.MakeShell(xnChild, model);
						}
					}
				}
			}
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Overriden in this class to set context variables available to child instructions.
		/// $guiPath, $appPath, $guiRole and $guiName
		/// </summary>
		protected override void SetContextVariables()
		{
			Var guiPath = new Var("guiPath", m_path);
			Var appPath = new Var("appPath", m_select);
			Var guiRole = new Var("guiRole", m_role);
			Var guiName = new Var("guiName", m_name);
			Var nodeName = new Var("nodeName", m_nodeName);
			guiPath.Execute();
			appPath.Execute();
			guiRole.Execute();
			guiName.Execute();
			nodeName.Execute();
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage(string name)
		{
			if (name == null) name = "path";
			switch (name)
			{
			case "name": return m_name;
			case "role": return m_role;
			case "when": return m_when;
			default: return base.GetDataImage(name);
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
			if (m_when != null) image += @" when=""" + m_when + @"""";
			if (m_name != null) image += @" name=""" + Utilities.attrText(m_name) + @"""";
			if (m_role != null) image += @" role=""" + m_role + @"""";
			image += @" created=""" + m_created.ToString() + @"""";
			return image;
		}
	}
}
