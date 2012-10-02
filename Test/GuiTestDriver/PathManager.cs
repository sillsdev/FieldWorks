using System;
using System.Collections; // for Hashtable and ArrayList
using System.Windows.Forms; // for AccessibleRole
using NUnit.Framework; // for Assertion

namespace GuiTestDriver
{
	/// <summary>
	/// The PathManager controls access to GUI accessible objects for one application.
	/// A PathManager may be created for each application involved in testing.
	/// Accessible objects must be obtained through this class.
	/// A cache of accessible objects via their path is maintained to
	/// speed up access. A path consists of delimited name pairs.
	/// The complexities of the GUI accessible object tree are hidden here.
	///
	/// A path is a series of typed tokens: type:name[#]/type:name[#]/...
	/// Type more or less corresponds to accessible role (see list below).
	/// Name is the accessible name of the GUI item. It is often the same as that
	/// displayed on control items, but need not be.
	/// The number index indicates the nth occurance of the path is meant.
	///
	/// The sequence of typed tokens is more logical than the actual accessibility tree.
	/// For example, the tabs list object and each individual tab are siblings in the
	/// accessibility tree but logically, the individuals belong under tabs list.
	/// A sidebar button is 5 levels below the sidebar window and is a sibling
	/// of its icon area which contains the icons as buttons. Logically, the button
	/// should contain its icons as the button visually pulls out the icons as if
	/// on a tray. Hence: sidebar:sideBarFw/button:Views/icon:Draft.
	///
	/// </summary>
	public class PathManager
	{
		// key is a path, value is an accessibility helper
		// keys are case sensitive
		Hashtable m_Paths = new Hashtable();
		// if needed later a case insensitive one is constructed via:
		//new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());

		AccessibilityHelper m_Root = null; // the root of the accessibility tree

		// Types of controls that the path manager can deal with
		public const string button  = "button";
		public const string combo   = "combobox";
		public const string edit    = "editbox";
		public const string group   = "group";
		public const string icon    = "icon";
		public const string item    = "item";
		public const string line    = "line";
		public const string list    = "list";
		public const string menu    = "menu";
		public const string menubar = "menubar";
		public const string none    = "none";
		public const string olitem  = "olitem";
		public const string para    = "para";
		public const string pop     = "popmenu";
		public const string radio   = "radio";
		public const string root    = "root";
		public const string sep     = "separator";
		public const string sidebar = "sidebar";
		public const string split   = "split";
		public const string statusbar = "statusbar";
		public const string tabs    = "tabs";
		public const string tab     = "tab";
		public const string titlebar = "titlebar";
		public const string toolbar = "toolbar";
		public const string tray    = "tray";
		public const string view    = "view";
		public const string window  = "window";

		public PathManager(AccessibilityHelper root)
		{
			m_Root = root;
			m_Paths.Add("app",m_Root);
		}

		/// <summary>
		/// Gets an accessible object from a path assuming it starts
		/// from the main application window.
		/// In other words, if the path is incomplete, the search starts
		/// with the main application window.
		/// </summary>
		/// <param name="path">Text in the form type:name[#]/type:name[#]/...</param>
		/// <returns>The accessible object found at the end of the path</returns>
		public AccessibilityHelper getAhFromPath(string path)
		{
			AccessibilityHelper ah = null;
			// is the path a key in the m_Paths?
			if (m_Paths.ContainsKey(path))
			{
				ah = (AccessibilityHelper)m_Paths[path];
			}
			else // go find it
			{
				// find the accessible object starting from the tree root
				ah = findFromPath(path);
				if (ah != null) m_Paths.Add(path,ah);
			}
			return ah;
		}

		/// <summary>
		/// Gets an accessible object from a path starting from the ancestor
		/// provided.
		/// </summary>
		/// <param name="ancestor">Begin looking with the children of this object.</param>
		/// <param name="path">Text in the form type:name[#]/type:name[#]/...</param>
		/// <returns>The accessible object found at the end of the path</returns>
		public AccessibilityHelper getAhFromContextPath(AccessibilityHelper ancestor, string path)
		{
			AccessibilityHelper ah = null;
			// is the path a key in the m_Paths?
			string aPath = getPathFromAh(ancestor);
			string fullPath = aPath + '/' +path;
			if (m_Paths.ContainsKey(fullPath))
			{
				ah = (AccessibilityHelper)m_Paths[fullPath];
			}
			else // go find it
			{
				// find the accessible object starting from the ancestor provided
				ah = findFromPath(ancestor, path);
					if (ah != null) m_Paths.Add(fullPath,ah);
			}
			return ah;
		}

		private string getPathFromAh(AccessibilityHelper ancestor)
		{
			string path = null;
			// two ways to do this:
			// 1 : If ancestor is in m_Paths, match the value and return the key.
			bool hasIt = m_Paths.ContainsValue(ancestor);
			if (hasIt)
			{
				ICollection keys = m_Paths.Keys;
				IEnumerator key = keys.GetEnumerator();
				key.MoveNext();
				while (!(m_Paths[key.Current]).Equals(ancestor)) key.MoveNext();
				// since it was contained in m_Paths, it must have been found.
				return (string)(key.Current);
			}

			// 2 : Use the Parent attribute of ancestors to build a path.
			//     (must get rid of "client"s and other fluff)
			if (ancestor.HWnd == m_Root.HWnd) // found the root
				return root + ":" + m_Root.Name;
			string prefix = getPathFromAh(ancestor.Parent);
			string role = RoleToType(ancestor.Role);
			if (role.Equals("none"))
				path = prefix;
			else path = prefix + "/" + role + ":" + ancestor.Name;
			// if (duplicate) path += "[n]";
			return 	path;

		}

		private AccessibilityHelper findFromPath(string path)
		{
			return findFromPath(m_Root,path);
		}

		private AccessibilityHelper findFromPath(AccessibilityHelper ancestor, string path)
		{
			// break the path into typed tokens of the form type:name[#]
			AccessibilityHelper ah = ancestor;
			ArrayList typedTokens = SplitPath(path);
			foreach (string typedToken in typedTokens)
			{
				string name, type;
				int    duplicate;
				SplitTypedToken(typedToken, out name, out type, out duplicate);
				AccessibleRole role = TypeToRole(type);
				if (duplicate == 1) ah = ah.FindChild(name,role);
				else				ah = ah.FindNthChild(name,role,duplicate,10);
			}
			return ah;
		}

		/// <summary>
		/// Returns an array of simple names that compose the path.
		/// </summary>
		/// <param name="Path">A '/' separated list of tokens like "fisrt/second/third"</param>
		/// <returns naem="ArrayList">ArrayList with parse results</returns>
		private ArrayList SplitPath(string Path)
		{
			ArrayList al = new ArrayList();
			int slashPos = -1;
			if (Path != null) slashPos = Path.IndexOf('/');
			while (slashPos > 0)
			{
				al.Add(Path.Substring(0, slashPos));
				Path = Path.Substring(slashPos + 1);
				slashPos = -1;
				if (Path != null) slashPos = Path.IndexOf('/');
			}
			Assert.IsTrue(slashPos != 0,"First or double '/' in path");
			Assert.IsNotNull(Path,"Path terminates in '/' or is null");
			al.Add(Path);
			return al;
		}

		/// <summary>
		/// Splits the type and name from a typed token like "T:N[#]".
		/// </summary>
		/// <param name="token">A type, ":", then token name like "sidebar:Draft[2]"</param>
		/// <param name="name">Parsed token name</param>
		/// <param name="type">Parsed token type</param>
		/// <param name="duplicate">How many siblings of the same type preceed it</param>
		private void SplitTypedToken(string token, out string name, out string type, out int duplicate)
		{
			Assert.IsNotNull(token,"Typed GUI element name expected but got null");
			string pName = null;
			duplicate = 1;
			int pos = token.IndexOf(":");
			if (pos > 0) // "type:name[#]"
			{
				type  = token.Substring(0,pos);
				pName = token.Substring(pos+1);
			}
			else if (pos == 0) // ":name[#]"
			{
				type  = "none";
				pName = token.Substring(pos+1);
			}
			else // "name[#]"
			{
				type  = "none";
				pName = token;
			}
			duplicate = parseIndex(pName, out name);
		}

		/// <summary>
		/// Parses the expression "[n]" for some 'n' in a pair name
		/// </summary>
		/// <param name="pName">A simple path name to be parsed</param>
		/// <param name="name">pName without the index expression</param>
		/// <returns>The index or 1 if there is no index</returns>
		private int parseIndex(string pName, out string name)
		{
			int posN   = pName.IndexOf('[')+1;
			int posEnd = pName.IndexOf(']');
			int Nth    = 1;
			name = "NONE";
			if (posN >= 0 && posEnd > posN)
			{
				Nth  = Convert.ToInt32((pName.Substring(posN,posEnd-posN)));
				name = pName.Substring(0,posN-1);
			}
			return Nth;
		}

		/// <summary>
		/// Converts a path type to its corresponding AccessibleRole.
		/// </summary>
		/// <param name="GuiElementType">Type from a typed token like "Type:Name"</param>
		/// <returns></returns>
		private AccessibleRole TypeToRole(string GuiElementType)
		{
			AccessibleRole role = AccessibleRole.None;
			switch (GuiElementType)
			{
				case button:	role = AccessibleRole.PushButton; break;
					//case "combobox": role = AccessibleRole.ComboBox; break;
				case combo:		role = AccessibleRole.PushButton; break;
				case edit:		role = AccessibleRole.Text; break;
				case group:		role = AccessibleRole.Grouping; break;
				case icon:		role = AccessibleRole.PushButton; break;
				case item:		role = AccessibleRole.ListItem; break;
				case line:		role = AccessibleRole.Text; break;
				case list:		role = AccessibleRole.List; break;
				case menu:		role = AccessibleRole.MenuItem; break;
				case menubar:	role = AccessibleRole.MenuBar; break;
				case none:		role = AccessibleRole.None; break;
				case olitem:	role = AccessibleRole.OutlineItem; break;
				case para:		role = AccessibleRole.Text; break;
				case pop:		role = AccessibleRole.MenuPopup; break;
				case radio:		role = AccessibleRole.RadioButton; break;
				case root:		role = AccessibleRole.Window; break;
				case sep:		role = AccessibleRole.Separator; break;
				case sidebar:	role = AccessibleRole.Window; break;
				case split:		role = AccessibleRole.PushButton; break; // splitter button
					//case "split": role = (AccessibleRole)62; break; // splitter button
				case statusbar: role = AccessibleRole.StatusBar; break;
				case tabs:		role = AccessibleRole.PageTabList; break;
				case tab:		role = AccessibleRole.PageTab; break;
				case titlebar:	role = AccessibleRole.TitleBar; break;
				case toolbar:	role = AccessibleRole.ToolBar; break;
				case tray:		role = AccessibleRole.PushButton; break; // sidebar:sideBarFw/window:NAMELESS/
				case view:		role = AccessibleRole.Grouping; break;
				case window:	role = AccessibleRole.Window; break;
			}
			return role;
		}

		/// <summary>
		/// Most likely pairings of role to type
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		private string RoleToType (AccessibleRole role)
		{
			string type = null;
			switch (role)
			{
				case AccessibleRole.Grouping:	 type = view; break;
				case AccessibleRole.List:		 type = list; break;
				case AccessibleRole.ListItem:	 type = item; break;
				case AccessibleRole.MenuBar:	 type = menubar; break;
				case AccessibleRole.MenuItem:    type = menu; break;
				case AccessibleRole.None:		 type = none; break;
				case AccessibleRole.OutlineItem: type = olitem; break;
				case AccessibleRole.PageTab:	 type = tab; break;
				case AccessibleRole.PageTabList: type = tabs; break;
				case AccessibleRole.MenuPopup:	 type = pop; break;
				case AccessibleRole.PushButton:  type = button; break;
				case AccessibleRole.RadioButton: type = radio; break;
				case AccessibleRole.Separator:	 type = sep; break;
				case AccessibleRole.StatusBar:	 type = statusbar; break;
				case AccessibleRole.Text:		 type = line; break;
				case AccessibleRole.TitleBar:	 type = titlebar; break;
				case AccessibleRole.ToolBar:	 type = toolbar; break;
				case AccessibleRole.Window:		 type = window; break;
				default :						 type = none; break;
			}
			return type;
		}

	}
}
