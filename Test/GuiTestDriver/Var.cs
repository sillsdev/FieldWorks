// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Var.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using Microsoft.Win32;
using System.IO;
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Var.
	/// </summary>
	public class Var : Instruction
	{
		string m_val   = null;
		string m_set = null;
		string m_add = null;
		string m_prop = null;
		string m_when = null;
		string m_env = null; // environment string
		string m_reg   = null; // registry key
		string m_file_exists = null; // name of path//file to see if it exists
		bool m_bound = false;

		/// <summary>
		/// Create an empty variable.
		/// </summary>
		public Var()
			: base()
		{
			m_tag = "var";
		}

		/// <summary>
		/// Create a variable instruction with an id and value setting.
		/// </summary>
		/// <param name="id">The name of the variable</param>
		/// <param name="set">Set the variable value with this string</param>
		public Var(string id, string set)
			: base()
		{
			m_tag = "var";
			m_id  = id;
			m_set = set;
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
			m_log.isNotNull(Id, makeNameTag() + " instruction must have a id.");
			m_log.isTrue(Id != "", makeNameTag() + " instruction must have a non-empty id.");
			if (m_select != null && m_select != "")
			{ // the variable can only have one text node or one other node assigned to it.
				m_log.isNotNull(con, "makeNameTag() + select has no context.");
				XmlNodeList pathNodes = Instructionator.selectNodes(con, m_select, "var" + Id);
				m_log.isNotNull(pathNodes, makeNameTag() + "var " + Id + " select='" + m_select + "' returned no result");
				if (pathNodes.Count > 0)
				{ // append first node to set string
					XmlNode modNode = pathNodes.Item(0);
					string prop = null;
					// which property of the node to get?
					if (m_prop == null && modNode is XmlElement) m_prop = "path";
					if (m_prop == null) m_prop = "value";
					if (m_prop != null && m_prop == "value") prop = XmlPath.ResolveModelPath(modNode, modNode.Value);
					if (m_prop != null && m_prop == "name") prop = modNode.Name;
					if (m_prop != null && m_prop == "type") prop = modNode.NodeType.ToString();
					if (m_prop != null && m_prop == "path")
					{
						XmlPath xp = new XmlPath(modNode);
						if (xp.isValid()) prop = xp.Path;
						else prop = null;
					}
					m_set += prop;
				}
				else m_set += "#NoSelection#";
			}
			if (Set == null && m_when == null)
			{ // if there is a select/when then don't complain if the select found nothing
				m_log.isNotNull(Add, makeNameTag() + Id +
					@" set, select or add must result in a string or number value unless when=""exists"" is set.");
				if (m_select != null && m_select != "") Set = @"#not-" + m_when + @"#";
			}
			return true;
		}

		/// <summary>
		/// Sets the value of the variable.
		/// </summary>
		public string Set
		{
			get { return m_set; }
			set { m_set = value; }
		}

		/// <summary>
		/// Adds the value to the variable.
		/// </summary>
		public string Add
		{
			get { return m_add; }
			set { m_add = value; }
		}

		/// <summary>
		/// When is exists, when it exists.
		/// It only sets the variable if the object exists.
		/// </summary>
		public string Prop
		{
			get { return m_prop; }
			set { m_prop = value; }
		}

		/// <summary>
		/// When is exists, when it exists.
		/// It only sets the variable if the object exists.
		/// </summary>
		public string When
		{
			get { return m_when; }
			set { m_when = value; }
		}

		/// <summary>
		/// Get the value from an environment variable.
		/// </summary>
		public string Env
		{
			get { return m_env; }
			set { m_env = value; }
		}

		/// <summary>
		/// Get the value from the registry.
		/// </summary>
		public string Reg
		{
			get { return m_reg; }
			set { m_reg = value; }
		}

		/// <summary>
		/// Tests to see if a file exists.
		/// </summary>
		public string FileExists
		{
			get { return m_file_exists; }
			set { m_file_exists = value; }
		}
		/// <summary>
		/// Execute method.
		/// If not already bound, this variable will be assigned a vaule.
		/// The variable may be bound when it is "built" or "interpreted".
		/// If bound when built, the variable may only reference preceding
		/// instruction settings (not results) and other built-bound vars.
		///
		/// When there is reg, env or set data, it replaces the old data.
		/// When there is add data, it is added if old and add are numbers,
		/// or it is appended to the old data.
		/// When there is set and add data, set replaces the old data and
		/// add is added or appended.
		/// @env and @reg get values from the environment and the registry.
		/// The order of concatenation is @reg, @env, @set then @add.
		/// </summary>
		public override void Execute()
		{ // Bind the variable to a value if not already bound
			if (!m_bound)
			{ // not already bound.
				m_log = Logger.getOnly(); // the log set in Instruction creation may be in another TestState
				TestState ts = TestState.getOnly();
				Instruction ins = ts.Instruction(m_id);
				if (ins != null)
				{ // already one with this name
					m_val = ins.GetDataImage(null);
					m_log.paragraph(makeNameTag() + "Found var " + m_id + " with value=" + m_val);
					// remove the old ins from ts
					ts.RemoveInstruction(m_id);
					if (m_reg != null || m_env != null || m_set != null)
						m_val = null; // don't use previous value
				}
				if (m_reg != null)
				{
					string value = null;
					string key;
					RegistryKey regkey = Utilities.parseRegKey(m_reg, out key);
					if (regkey != null)
					{
						RegistryValueKind rvk = regkey.GetValueKind(key);
						switch (rvk)
						{
						case RegistryValueKind.Binary:
							m_log.paragraph(makeNameTag() + "Reg key is Binary");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.DWord:
							m_log.paragraph(makeNameTag() + "Reg key is DWord");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.ExpandString:
							m_log.paragraph(makeNameTag() + "Reg key is ExpandString");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.MultiString:
							m_log.paragraph(makeNameTag() + "Reg key is MultiString");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.QWord:
							m_log.paragraph(makeNameTag() + "Reg key is QWord");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.String:
							value = (string)regkey.GetValue(key);
							break;
						case RegistryValueKind.Unknown:
							m_log.paragraph(makeNameTag() + "Reg key is Unknown");
							break;
						}
						regkey.Close();
					}
					else
						m_log.paragraph(makeNameTag() + "Invalid Reisitry path: " + m_reg);
					if (value != null) m_val += value;
				}
				if (m_env != null)
				{ // try the process then the user then the machine environment variables.
					string value = Environment.GetEnvironmentVariable(m_env, EnvironmentVariableTarget.Process);
					if (value == null) value = Environment.GetEnvironmentVariable(m_env, EnvironmentVariableTarget.User);
					if (value == null) value = Environment.GetEnvironmentVariable(m_env, EnvironmentVariableTarget.Machine);
					if (value != null) m_val += value;
				}
				if (m_set != null) m_val += Utilities.evalExpr(m_set);
				if (m_add != null)
				{ // add the old and new
					string newVal = Utilities.evalExpr(m_add);
					if (Utilities.IsNumber(newVal) && Utilities.IsNumber(m_val))
					{
						try
						{
							double old = double.Parse(m_val);
							double add = double.Parse(newVal);
							double sum = old + add;
							m_log.paragraph(makeNameTag() + "Variable(" + m_id + ") = " + old + " + " + add + " = " + sum);
							m_val = System.Convert.ToString(sum);
						}
						catch
						{
							m_log.fail(makeNameTag() + "Variable(" + m_id + ") = " + m_val + " + " + m_add + " does not compute!");
						}
					}
					else
					{
						m_log.paragraph(makeNameTag() + "Text add: Variable(" + m_id + ") = " + m_val + " + " + newVal);
						m_val += newVal; // append as strings
					}
				}
				else
					// even if there was a previous var, but m_add was null,
					// then m_val is null if nothing was m_set!
					if (m_set == null) m_val = null;
				if (m_file_exists != null && m_val != null)
				{ // append the file name to the reg and/or env value.
					if (File.Exists(m_val)) m_val = m_file_exists;
					else m_val = "#not-exists#";
				}
				ts.AddNamedInstruction(m_id, this);
			}
			m_bound = true;
			base.Execute();
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Default implementation for getting the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns></returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "value";
			switch (name)
			{
			case "value": return m_val;
			case "set": return m_set;
			case "add": return m_add;
			case "env": return m_env;
			case "reg": return m_reg;
			case "file-exists": return m_file_exists;
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
			if (m_env != null) image += @" env=""" + Utilities.attrText(m_env) + @"""";
			if (m_reg != null) image += @" reg=""" + Utilities.attrText(m_reg) + @"""";
			if (m_set != null) image += @" set=""" + Utilities.attrText(m_set) + @"""";
			if (m_add != null) image += @" add=""" + Utilities.attrText(m_add) + @"""";
			if (m_file_exists != null) image += @" file-exists=""" + Utilities.attrText(m_file_exists) + @"""";
			if (m_val != null && m_val != m_set) image += @" val=""" + Utilities.attrText(m_val) + @"""";
			return image;
		}

	}
}
