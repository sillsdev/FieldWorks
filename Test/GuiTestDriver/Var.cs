// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Var.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using Microsoft.Win32;
using System.IO;
namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Var.
	/// </summary>
	public class Var : Instruction
	{
		string m_val   = null;
		string m_set   = null;
		string m_add   = null;
		string m_env   = null; // environment string
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
		/// Adds the value to the variable.
		/// </summary>
		public string Env
		{
			get { return m_env; }
			set { m_env = value; }
		}

		/// <summary>
		/// Adds the value to the variable.
		/// </summary>
		public string Reg
		{
			get { return m_reg; }
			set { m_reg = value; }
		}

		/// <summary>
		/// Adds the value to the variable.
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
					m_log.paragraph("Found var " + m_id + " with value=" + m_val);
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
							m_log.paragraph("Reg key is Binary");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.DWord:
							m_log.paragraph("Reg key is DWord");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.ExpandString:
							m_log.paragraph("Reg key is ExpandString");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.MultiString:
							m_log.paragraph("Reg key is MultiString");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.QWord:
							m_log.paragraph("Reg key is QWord");
							value = Convert.ToString(regkey.GetValue(key));
							break;
						case RegistryValueKind.String:
							value = (string)regkey.GetValue(key);
							break;
						case RegistryValueKind.Unknown:
							m_log.paragraph("Reg key is Unknown");
							break;
						}
						regkey.Close();
					}
					else
						m_log.paragraph("Invalid Reisitry path: " + m_reg);
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
							m_log.paragraph("Variable(" + m_id + ") = " + old + " + " + add + " = " + sum);
							m_val = System.Convert.ToString(sum);
						}
						catch
						{
							fail("Variable(" + m_id + ") = " + m_val + " + " + m_add + " does not compute!");
						}
					}
					else
					{
						m_log.paragraph("Text add: Variable(" + m_id + ") = " + m_val + " + " + newVal);
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
