// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Utilities.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
//using System.Xml.XPath;
using System.Collections;
using System.Windows.Forms;

namespace Simian
{
	/// <summary>
	/// Utilities include methods of general utility.
	/// </summary>
	public class Utilities
	{
		public Utilities()
		{
		}

		/// <summary>
		/// Checks validity of strings - not null nor empty.
		/// </summary>
		/// <param name="str">The string to test</param>
		/// <returns>true if the string is not null nor empty</returns>
		static public bool isGoodStr(string str)
		{
			return str != null && !str.Equals("");
		}

		/// <summary>
		/// Get the number of milliseconds, ticks, using a start and end
		/// tick count.  Routine is used to handle the case (infrequent as
		/// it may be) where the tick count has rolled over.  With out this
		/// a test could fail - and that would be bad...
		/// </summary>
		/// <param name="start">start tick count</param>
		/// <param name="end">end tick count</param>
		/// <returns>number of ticks between the two</returns>
		static public int NumTicks(int start, int end)
		{
			if (end < start)	// case where we're rolled over (~24.9 days)
			{
				return int.MaxValue - start + end;
			}
			else
			{
				return end - start;
			}
		}

		/// <summary>
		/// Returns an array of simple names that compose the path
		/// making a token of text between '/'s.
		/// A '/' in a path token, should be input doubled up as "//".
		/// </summary>
		/// <param name="Path">A '/' separated list of tokens like "fisrt/second/third"</param>
		/// <returns>ArrayList with parse results</returns>
		static public ArrayList ParsePath(string Path)
		{
			ArrayList al = new ArrayList();
			string token = null;
			int last = 0;
			while (Path != null && Path != "")
			{
				if (token != null) Path = Path.Substring(last + 1);
				token = GetFirstStep(Path, out last);
				al.Add(token);
				if (Path.Length <= last) Path = null;
			}
			return al;
		}

		/// <summary>
		/// Removes the first step from the typedPath and returns it.
		/// A '/' in a path token, should be input doubled up as "//".
		/// If the path is a '/' or spaces, the token returned is "NONE:NAMELESS".
		/// </summary>
		/// <param name="Path">A '/' separated list of tokens like "fisrt/second/third"</param>
		/// <param name="last">The last index in Path used to form the returned token</param>
		/// <returns>string with parse results</returns>
		static public string GetFirstStep(string Path, out int last)
		{
			last = 0;
			if (Path == null || Path.Trim() == "")
			{ // there is nothing in the path
				if (Path != null) last = Path.Length;
				return "NONE:NAMELESS";
			}
			string token = null;
			int slashPos = -1;
			slashPos = Path.IndexOf('/'); // Find first slash
			if (slashPos == 0)
			{ // there is nothing before the slash or
			  // it is a double slash meaning its part of the path
				if (Path.Length > 1 && Path.Substring(1, 1).Equals("/"))
				{
					token = "/";
					last = 2;
					Path = Path.Substring(2);
					slashPos = Path.IndexOf('/'); // Find next slash and keep going
				}
				else
				{ // one begining slash means no path step
					last = 0;
					return "NONE:NAMELESS";
				}
			}
			// Continue finding slashes until one ends the token or no more characters
			while (slashPos > -1)
			{ // there is something before the slash
				if (Path.Length > 1+slashPos && Path.Substring(slashPos + 1, 1).Equals("/"))
				{ // a second slash was found //
					last++;
					token = token + Path.Substring(0, ++slashPos); // include a '/'
				}
				else
				{ // this slash ends the token
					token = token + Path.Substring(0, slashPos);
					last += slashPos;
					return token;
				}
				last += slashPos;
				if (Path.Length > 1+slashPos)
					Path = Path.Substring(slashPos + 1);
				else
				{ // Path ends with a double slash
					return token;
				}
				// Path can't be null or empty otherwise Path.Substring() would have complained
				slashPos = Path.IndexOf('/');
			}
			last += Path.Length;
			return token + Path;
		}

		/// <summary>
		/// Splits the type and name from a typed token like "T:N".
		/// For T:N -> name = N, type = T
		/// For :N -> name = N, type = "none"
		/// For N -> name = N, type = "none"
		/// For T:N:N -> name = N:N, type = T
		/// </summary>
		/// <param name="token">A type, ":", then token name like "sidebar:Draft"</param>
		/// <param name="name">Parsed token name</param>
		/// <param name="type">Parsed token type</param>
		static public void SplitTypedToken(string token, out string name, out string type)
		{
			if (token == null)
			{
				Log log = Log.getOnly();
				log.writeElt("fail");
				log.writeAttr("SplitTypedToken", "null");
				log.endElt();
			}
			int pos = token.IndexOf(":");
			if (pos > 0)
			{
				type = token.Substring(0,pos);
				name = token.Substring(pos+1);
			}
			else if (pos == 0)
			{
				type = "none";
				name = token.Substring(pos+1);
			}
			else
			{
				type = "none";
				name = token;
			}
		}
		/// <summary>
		/// Converts a typed token type to its corresponding AccessibleRole.
		/// </summary>
		/// <param name="GuiElementType">Type from a typed token like "Type:Name"</param>
		/// <returns></returns>
		static public AccessibleRole TypeToRole(string GuiElementType)
		{
			AccessibleRole role = AccessibleRole.None;
			switch (GuiElementType)
			{
				case "button": role = AccessibleRole.PushButton; break;
				case "cell": role = AccessibleRole.Cell; break;
				case "chkbox": role = AccessibleRole.CheckButton; break;
				case "colhead": role = AccessibleRole.ColumnHeader; break;
				case "combobox": role = AccessibleRole.ComboBox; break;
				case "editbox": role = AccessibleRole.Text; break;
				case "group": role = AccessibleRole.Grouping; break;
				case "item": role = AccessibleRole.ListItem; break;
				case "line": role = AccessibleRole.Text; break;
				case "list": role = AccessibleRole.List; break;
				case "menu": role = AccessibleRole.MenuItem; break;
				case "menubar": role = AccessibleRole.MenuBar; break;
				case "none": role = AccessibleRole.None; break;
				case "olitem": role = AccessibleRole.OutlineItem; break;
				case "outline": role = AccessibleRole.Outline; break;
				case "para": role = AccessibleRole.Text; break;
				case "pic": role = AccessibleRole.Graphic; break;
				case "popmenu": role = AccessibleRole.MenuPopup; break;
				case "radio": role = AccessibleRole.RadioButton; break;
				case "row": role = AccessibleRole.Row; break;
				case "sbar": role = AccessibleRole.ScrollBar; break;
				case "separator": role = AccessibleRole.Separator; break;
				case "sidebar": role = AccessibleRole.None; break;
				case "split": role = (AccessibleRole)62; break; // splitter button
				case "statusbar": role = AccessibleRole.StatusBar; break;
				case "tabs": role = AccessibleRole.PageTabList; break;
				case "tab": role = AccessibleRole.PageTab; break;
				case "table": role = AccessibleRole.Table; break;
				case "text": role = AccessibleRole.Text; break;
				case "titlebar": role = AccessibleRole.TitleBar; break;
				case "toolbar": role = AccessibleRole.ToolBar; break;
				case "tray": role = AccessibleRole.PushButton; break;
				case "view": role = AccessibleRole.Grouping; break;
				case "value": role = AccessibleRole.Alert; break;
				case "window": role = AccessibleRole.Window; break;
			}
			return role;
		}
		/// <summary>
		/// Converts a GUI Model node name to its corresponding Accil path type.
		/// </summary>
		/// <param name="GuiModelElement">XmlNode from a GUI Model</param>
		/// <returns>Accil path type</returns>
		static public string CalcType(XmlNode GuiModelElement)
		{
			string type = "NONE";
			switch (GuiModelElement.Name)
			{ // default includes:
			  // combobox, menubar, sidebar, titlebar, toolbar, tray, window
			  // Only the exceptions are called out below
				case "button": // what kind?
					type = "button"; // most likely, but not for sure
					XmlAttribute split = GuiModelElement.Attributes["split"];
					if (split != null && split.Value == "yes") type = "split";
					else
					{
						XmlNode ancestorMenu = GuiModelElement.SelectSingleNode("ancestor::menubar");
						if (ancestorMenu != null) type = "menu";
					}
					break;
				case "item": // what kind?
					type = "menu";
					XmlNode ancestorList = GuiModelElement.SelectSingleNode("ancestor::list");
					if (ancestorList != null) type = "item";
					break;
				/*case "view":
					type = "Root";
					break; */
				default:
					type = GuiModelElement.Name;
					break;
			}
			return type;
		}
		/// <summary>
		/// Determines if the string is a literal 'Literal'.
		/// </summary>
		/// <param name="arg">The string to be tested</param>
		/// <returns>true if the string is a literal</returns>
		static public bool IsLiteral(string arg)
		{ // a null '' is not a literal
			const char apos = (char)39; // single quote or apostrophe
			if (arg == null) return false;
			if (arg.Length <= 2) return false;
			if (arg[0] == '"' && arg[arg.Length-1] == '"') return true;
			if (arg[0] == apos && arg[arg.Length-1] == apos )return true;
			return false;
		}
		/// <summary>
		/// Use this method only after IsLiteral. Otherwise the literal may not be correct.
		/// </summary>
		/// <param name="arg">String literal</param>
		/// <returns>The literal without the single quotes</returns>
		static public string GetLiteral (string arg)
		{
			return arg.Substring(1,arg.Length-2);
		}
		/// <summary>
		/// Determines if the string is a number image.
		/// </summary>
		/// <param name="arg">The string to test</param>
		/// <returns>true if the string is a number image</returns>
		static public bool IsNumber(string arg)
		{ // a null '' is not a number
			if (arg == null) return false;
			if (arg.Length == 0) return false;
			try
			{
				double number = double.Parse(arg);
				return true;
			}
			catch (OverflowException)
			{
				Log log = Log.getOnly();
				log.writeElt("fail");
				log.writeAttr("IsNumber", (string)arg);
				log.writeAttr("abs-value", "too large");
				log.endElt();
			}
			catch
			{
				// not in the right format
			}
			return false;
		}
		/// <summary>
		/// Call IsNumber(arg) before GetNumber to make sure it is a number.
		/// </summary>
		/// <param name="arg">The number image string</param>
		/// <returns>The number represented by the string</returns>
		static public double GetNumber(string arg)
		{
			return double.Parse(arg);
		}

		static readonly char [] terminal = new char[] {';',' '};

		/// <summary>
		/// Evaluates an attribute expression.
		/// They may have instruction references of the form:
		///  $ref $ref.data $ref.data; with literal text all around.
		///  If there is no ';', there must be a space.
		///  If the ref can't be found in ts, then assume it's part of the literal.
		/// </summary>
		/// <param name="expr">The expression to evaluate</param>
		/// <returns>The expanded string</returns>
		static public string evalExpr(string expr)
		{
			if (expr == null) return expr;
			string result = null;
			ArrayList parts = new ArrayList(5); // the array of parse product strings
			string deref = null;
			bool found   = false;
			string line  = expr;
			// scan the expression for references
			int loc = line.IndexOf("$");
			while (-1 != loc)
			{ // found a reference - cut it out and expand it
				if (loc > 0) parts.Add(line.Substring(0,loc));
				line = line.Substring(loc); // drop the leading text
				int end = line.IndexOfAny(terminal);
				if (-1 < end)
				{ // other text follows
					string cut = line.Substring(1,end-1);
					deref = evalRef(cut, out found);
					if (line[end] == ';' && found) end++;
					try {line = line.Substring(end);}
					catch (ArgumentOutOfRangeException)
					{line = null;}
					if (line == "") line = null;
				}
				else
				{ // this ref is last in the expression
					deref = evalRef(line.Substring(1), out found);
					line = null; // nothing left to parse
				}
				if (deref != null)
				{
					if (!found) deref = '$' + deref;
					parts.Add(deref);
				}
				if (line != null) loc = line.IndexOf("$");
				else              loc = -1;
			}
			if (line != null) parts.Add(line);
			// line up and return all the parts.
			IEnumerator ie = parts.GetEnumerator();
			while (ie.MoveNext())
			{
				result += (string)ie.Current;
			}
			if (found) return evalExpr(result);
			return result;
		}

		static public string evalRef(string refer, out bool found)
		{
			string value = null;
			found = false;
			Variables vars = Variables.getOnly();
			string    data = null;
			// is there a '.' ?
			int dot = refer.IndexOf('.');
			if (-1 == dot)
			{ // no dot, get the default data or function value
				value = vars.get(refer);
			}
			else
			{ // has a dot, so split it and get the parts.
				string refer2 = refer.Substring(0,dot);
				try {data = refer.Substring(dot+1);} // don't take the dot
				catch(ArgumentOutOfRangeException)
				{data = null;}
				value = vars.getDotted(refer2, data);
			}
			if (value == null) return refer; // not a reference
			found = true;
			return value;
		}

		/// <summary>
		/// Reformats the text so it can be safely used in an XML attribute.
		/// Quote characters and angle brackets are made into entities.
		/// </summary>
		/// <param name="text">The text to be used in an XML attribute.</param>
		/// <returns>The reformatted text.</returns>
		static public string attrText(string text)
		{
			string image = "";
			if (text == null) return image;
			foreach (char ch in text)
			{
				switch (ch)
				{
				case '\'': image += @"&apos;"; break;
				case '"' : image += @"&quot;"; break;
				case '<' : image += @"&lt;";   break;
				case '>' : image += @"&gt;"; break;
				case '&' : image += @"&amp;"; break;
				default  : image += ch; break;
				}
			}
			return image;
		}

		static public string ArrayListToString(ArrayList al)
		{
			string image = null;
			foreach (object ob in al)
			{
				if (image != null) image += ", ";
				image += ob.ToString();
			}
			return image;
		}

		static public Microsoft.Win32.RegistryKey parseRegKey(string regKey, out string key)
		{
			Log log = Log.getOnly();
			// extract the key portions from m_key
			string[] parts = regKey.Split('\\');
			string hkey = parts[0];
			key = parts[parts.Length - 1];
			log.writeElt("parseRegKey");
			log.writeAttr("key",key);
			string rest = regKey.Remove(0, hkey.Length + 1);
			rest = rest.Substring(0, rest.Length - key.Length);
			log.writeAttr("sub-key", key);
			log.endElt();

			Microsoft.Win32.RegistryKey regkey = null;
			switch (hkey)
			{
			case "HKEY_CLASSES_ROOT":
				regkey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(rest, true);
				break;
			case "HKEY_CURRENT_CONFIG":
				regkey = Microsoft.Win32.Registry.CurrentConfig.OpenSubKey(rest, true);
				break;
			case "HKEY_CURRENT_USER":
				regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(rest, true);
				break;
			case "HKEY_DYN_DATA":
				regkey = Microsoft.Win32.Registry.DynData.OpenSubKey(rest, true);
				break;
			case "HKEY_LOCAL_MACHINE":
				regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(rest, true);
				break;
			case "HKEY_USERS":
				regkey = Microsoft.Win32.Registry.Users.OpenSubKey(rest, true);
				break;
			default:
				log.writeElt("parseRegKey");
				log.writeAttr("lacks", "HKEY_");
				log.writeAttr("key", regKey);
				break;
			}
			return regkey;
		}
	}
}
