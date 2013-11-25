// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AccessibilityHelper.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using SIL.FieldWorks.AcceptanceTests.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
//using SIL.FieldWorks.Common.Framework;
//using SIL.FieldWorks.Common.Controls;
using System.Runtime.InteropServices;
//using Accessibility;
using System.Text.RegularExpressions;


namespace Simian
{
	/// <summary>
	/// Summary description for AccessibilityHelper.
	/// </summary>
	public class AccessibilityHelper : AccessibilityHelperBase
	{
		//		STDAPI AccessibleObjectFromWindow(
		//			HWND hwnd,
		//			DWORD dwObjectID,
		//			REFIID riid,
		//			void** ppvObject
		//			);

		/// <summary>
		///
		/// </summary>
		[DllImport("oleacc.DLL", EntryPoint="AccessibleObjectFromWindow",  SetLastError=true,
			 CharSet=CharSet.Unicode)]
		public static extern int AccessibleObjectFromWindow(int hWnd, int objId,
			ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object obj);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AccessibilityHelper"/> class
		/// from the top window.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AccessibilityHelper(): base()
		{
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AccessibilityHelper"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AccessibilityHelper(IntPtr hWnd): base(hWnd.ToInt32())
		{
			//
			// TODO: Add constructor logic here
			//
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AccessibilityHelper"/> class
		/// from an AccessibililtyHelperBase object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AccessibilityHelper(AccessibilityHelperBase ah): base(ah)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ptScreen"></param>
		/// -----------------------------------------------------------------------------------
		public AccessibilityHelper(Point ptScreen): base(ptScreen)
		{
		}

		/// <summary>
		/// Construct using a window name. If not found, answers one for the top window.
		/// </summary>
		/// <param name="sWindowName"></param>
		public AccessibilityHelper(string sWindowName) : base(sWindowName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new AccessibilityHelper object from AccessibilityHelperBase object.
		/// </summary>
		/// <param name="ah">AccessibilityHelperBase object</param>
		/// <returns>AccessibilityHelper object</returns>
		/// ------------------------------------------------------------------------------------
		public override AccessibilityHelperBase CreateAccessibilityHelper(
			AccessibilityHelperBase ah)
		{
			return new AccessibilityHelper(ah);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the window handle for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int HWnd
		{
			get
			{
				int hWind = 0;
				try {hWind = base.HWnd;}
				catch (System.NullReferenceException e)
				{
					hWind = 0;
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible","no window handle");
					log.writeAttr("error",e.Message);
					log.endElt();
				}
				return hWind;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible name for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Name
		{
			get
			{
				string name = "";
				try {name = base.Name;}
				catch (System.NullReferenceException e)
				{
					name = "";
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no name");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				if (name == null) name = "";
				return name;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible role for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override AccessibleRole Role
		{
			get
			{
				AccessibleRole role = AccessibleRole.None;
				try {role = base.Role;}
				catch (System.NullReferenceException e)
				{
					role = AccessibleRole.None;
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no role");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return role;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible states for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override AccessibleStates States
		{
			get
			{
				AccessibleStates states = AccessibleStates.None;
				try {states = base.States;}
				catch (System.NullReferenceException e)
				{
					states = AccessibleStates.None;
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no states");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return states;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible keyboard shortcut for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Shortcut
		{
			get
			{
				string shortcut = "";
				try {shortcut = base.Shortcut;}
				catch (System.NullReferenceException e)
				{
					shortcut = "";
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no shortcut");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return shortcut;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible keyboard shortcut for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AccessibleLong
		{
						get { return base.AccessibleLong; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible default action for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string DefaultAction
		{
			get
			{
				string action = "";
				try {action = base.DefaultAction;}
				catch (System.NullReferenceException e)
				{
					action = "";
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no defaultAction");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return action;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Value
		{
			get
			{
				string valu = "";
				try {valu = base.Value;}
				catch (System.NullReferenceException e)
				{
					valu = "";
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no value");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				if (this.Role == AccessibleRole.Text && this.Name == "Paragraph")
				{ // its value is the concatenation of its strings
					valu = "";
					foreach(AccessibilityHelper child in this)
					{
						if (child.Name == "String") valu += child.Value;
					}
				}
				return valu;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of children.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int ChildCount
		{
			get
			{
				int Children = 0;
				try {Children = base.ChildCount;}
				catch (System.NullReferenceException e)
				{
					Children = 0;
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no childcount");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return  Children;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent accessible object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper Parent
		{
			get
			{
				AccessibilityHelper parent = null;
				try {parent = (AccessibilityHelper)base.Parent1;}
				catch (System.NullReferenceException e)
				{
					parent = null;
					Log log = Log.getOnly();
					log.writeElt("fail");
					log.writeAttr("accessible", "no parent");
					log.writeAttr("error", e.Message);
					log.endElt();
				}
				return  parent;
			}
		}

		/// <summary>
		/// Matches legal name and role combinations to their ah counterparts.
		/// They can match on both.
		/// Match NAMELESS to a null ah.name.
		/// Match name to ah.value when role is Alert and ah.role is anything.
		/// Match anything when name is null or #ANY.
		/// Match any ah.role when role is None.
		/// One name match and one role match must be true.
		/// </summary>
		/// <param name="name">The text to match, can be null</param>
		/// <param name="role">The Accessible role - some like Alert are special</param>
		/// <param name="ah">The accessible object</param>
		/// <returns>true if a name and a role matched</returns>
		private bool MatchNameAndRole(string name, AccessibleRole role, AccessibilityHelper ah)
		{
			bool result = false;
			bool NameResult = false;
			// check if the name is a regular expression
			if (name != null && name.StartsWith("rexp#"))
			{
				Regex rx = new Regex(name.Substring(5));
				string MatchItem = "";
				if (ah.Role == AccessibleRole.Alert) MatchItem = ah.Value;
				else                                 MatchItem = ah.Name;
				NameResult = rx.IsMatch(MatchItem);
				Log log = Log.getOnly();
				log.writeElt("match-reg-exp");
				log.writeAttr("pattern", name.Substring(5));
				log.writeAttr("to", MatchItem);
				log.writeAttr("result", result.ToString());
				log.endElt();
			}
			else if (ah.Role == AccessibleRole.Alert)
				NameResult = name == ah.Value;

			result =
				(
					NameResult
					|| name == ah.Name
					|| (name == "NAMELESS" && ah.Name == null) // should never be null, but "" instead
					|| (name == "NAMELESS" && ah.Name == "")
					|| name == null
					|| name == "#ANY"
				)
				&& (role == ah.Role || role == AccessibleRole.None
					|| role == AccessibleRole.Alert);
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the child window with the specified name and/or role. This method recursively
		/// goes down the hierarchy until it finds the window.
		/// </summary>
		/// <param name="name">Accessibility name of the matching window, or <c>null</c></param>
		/// <param name="role">Accessibility role of the matching window, or
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		/// <remarks>If you provide only one parameter, it matches only that parameter.
		/// If you specify both parameters it tries to match both. </remarks>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper FindChild(string name, AccessibleRole role)
		{
			if (name == null && role == AccessibleRole.None)
				return null;

			AccessibilityHelper ah = null;

			foreach(AccessibilityHelper child in this)
			{
				if (child == null)
					continue;
				// does this object match?
				// treat name == "" and child.Name == null as a match
				// Note: a null string shows as "" in the debugger!
				if (MatchNameAndRole(name, role, child))
					return child;
				if (child.m_fRealAccessibleObject)
				{
					// look through the child objects
					ah = child.FindChild(name, role);
					if (ah != null)
						return ah;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the accessible object at the end of the path with optional place and varId from
		/// which it creates a variable object to hold the sibling number of the object found.
		/// An optional path visitor allows the caller to do something with intermediate steps
		/// along the path for debugging or interface control.
		/// The value of a node can be specified via a path step like "value:whatever the value is".
		/// When place is greater than 0, the search is breadth-first.
		/// Generally, clients need not be included in the "path".
		/// When place is 0, a value must be the leaf node of the path
		/// as there is typically some repeated structure
		/// like a table, row and cell with value. The structure is
		/// traversed repeatedly until the leaf node with the value is found.
		/// When place is negative, depth-first search is used. It tends to be
		/// the slowest way to search the Accessibility tree.
		/// </summary>
		/// <param name="path">The path to search. Each step in the path contains an
		/// Accessibility name and Accessibility role of the matching gui object.</param>
		/// <param name="visitor">null or the object providing methods that are called when steps are found.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the object found at the end of the path.</returns>
		public AccessibilityHelper SearchPath(GuiPath path, IPathVisitor visitor)
		{
			if (path == null) return null;

			// three cases based on Nth
			if (path.Nth > 0)
			{ // psuedo breadth-first
				return this.SearchPathByBreadth(path, visitor);
			}
			if (path.Nth == 0)
			{ // loop over FindChildBreadth to find a sibling index to set a var with varId
				return this.SearchPathForIndex(path, visitor);
			}
			else if (path.Nth < 0)
			{ // depth-first
				return this.SearchPathByDepth(path, visitor);
			}
			return null;
		}

		/// <summary>
		/// The search is almost breadth-first:
		/// The first step in the path is searched breath-first.
		/// Subsequent path steps are searched for depending on their content.
		/// The Accessibility helper representing the step is returned it is found and is the last step.
		/// Otherwise, the search continues with the next path step or if not found, null is returned.
		/// If the first step is found, the visitor is applied before continuing to the next step.
		/// </summary>
		/// <param name="path">The path to search. Each step in the path contains an
		/// Accessibility name and Accessibility role of the matching gui object.</param>
		/// <param name="visitor">null or the object providing methods that are called when steps are found.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		private AccessibilityHelper SearchPathByBreadth(GuiPath path, IPathVisitor visitor)
		{
			if (ChildCount <= 0 || path.Nth <= 0)
			{
				if (visitor != null) visitor.notFound(path);
				return null;
			}
			int place = path.Nth;
			Log log = Log.getOnly();
			log.writeElt("SearchPathByBreadth");
			log.writeAttr("from", this.Role + ":" + this.Name);
			log.writeAttr("to", path.toString());
			log.endElt();
			ArrayList lists = new ArrayList(); // list of child lists
			ArrayList nodes = MakeChildList(this);
			lists.Add(nodes);
			// Examine the first node in the list, dropping it after examination
			// and adding its children to the end if prudent.
			log.writeElt("head");
			int lnum = 0;
			while (lists.Count > 0)
			{
				int count = 0; // reset match count
				nodes = (ArrayList)lists[0];
				int nnum = 0; ++lnum;
				while (nodes.Count > 0)
				{
					AccessibilityHelper ah = (AccessibilityHelper)nodes[0];
					log.writeElt("SearchPathByBreadth");
					log.writeAttr("child", lnum + ":" + (++nnum));
					log.writeAttr("path", ah.Role + ":" + ah.Name);
					if (path.Next != null && path.Next.Role == AccessibleRole.Alert)
						log.writeAttr("value", ah.Value);
					log.endElt();

					if (MatchNameAndRole(path.Name, path.Role, ah))
					{ // this is the only way to return
						if (++count >= place)
						{ // Found this step, keep stepping along the path
							if (path.Next != null)
							{
								if (visitor != null) visitor.visitNode(ah); // allow processing of this ah by caller
								ah = ValueOrChild(path.Next, ah, visitor);
							}
							log.endElt(); // end head element
							return ah;
						}
					}
					if (ah.m_fRealAccessibleObject && ah.ChildCount > 0)
						lists.Add(MakeChildList(ah));
					nodes.RemoveAt(0); // when 0 is removed, all indices slide down 1
				}
				lists.RemoveAt(0); // when 0 is removed, all indices slide down 1
			}
			if (visitor != null) visitor.notFound(path);
			log.endElt(); // end head element
			return null;
		}

		/// <summary>
		/// The search is almost breadth-first:
		/// The first step in the path has an unknown sibling index and its role is not 'value'.
		/// If this ah has children of the step type, they are searched until all the
		/// subsequent steps find matching ah's. This search substitutes its own visitor
		/// that records the successful path ah's. Upon success, a sibling index is assigned to
		/// the variable named in the step, the non-terminal subpath ah's are exposed
		/// to the caller's visitor in turn and this method returns the last ah on the path.
		/// Subpath steps are searched for depending on their content.
		/// </summary>
		/// <param name="path">The path to search. Each step in the path contains an
		/// Accessibility name and Accessibility role of the matching gui object.</param>
		/// <param name="visitor">null or the object providing methods that are called when steps are found.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		private AccessibilityHelper SearchPathForIndex(GuiPath path, IPathVisitor visitor)
		{
			Log log = Log.getOnly();
			log.writeElt("head");
			log.writeElt("SearchPathForIndex");
			log.writeAttr("from", this.Role + ":" + this.Name);
			log.writeAttr("to", path.toString());
			log.endElt();
			if (ChildCount <= 0)
			{
				if (visitor != null) visitor.notFound(path);
				return null;
			}
			AccessibilityHelper ah = null;
			int nnum = 0;
			int index = 0;
			string parentName = null;
			if (path.Prev != null) parentName = path.Prev.Name;
			foreach (AccessibilityHelper child in this)
			{
				if (child == null) continue;
				log.writeElt("SearchPathForIndex");
				log.writeAttr("child", (++nnum));
				log.writeAttr("path", ah.Role + ":" + ah.Name);
				// not supposed to be a value!
				if (path.Next != null && path.Next.Role == AccessibleRole.Alert)
					log.writeAttr("bad-value", ah.Value);
				log.endElt();
				if (MatchNameAndRole(path.Name, path.Role, child))
				{ // this is a candidate for matching the path
					++index; // the first one is 1
					if (path.Next != null)
					{  // This method won't allow for caller's visitors on candidate nodes until proved needed
						TrackingVisitor tv = new TrackingVisitor(); // tv may need to open sub menus
						if (tv != null) tv.visitNode(child); // add child to node list to visit later if this is the right path
						ah = ValueOrChild(path.Next, child, tv);
						if (ah != null)
						{   // the subpath was matched to the end
							if (path.VarId != null && path.VarId != "")
							{  // Create and execute a variable to record the index
					//			Var var = new Var();
					//			var.Id = path.VarId;
					//			var.Set = System.Convert.ToString(index); // 1 based count
					//			var.Execute(); // puts the var in the TestState hash
								// don't set path.Nth = index since it might change if the path is used again
							}
							// let the caller's visitor tend to all the path ah's
							if (visitor != null)
							{
								if (tv != null) tv.StepDownPath(visitor);
							}
							log.endElt(); // end head element
							return ah;
						}
					}
				}
				else if (parentName != null && child.Role == AccessibleRole.Client && child.Name == parentName)
				{ // try the client instead
					ah = child.SearchPathForIndex(path, visitor);
					if (ah != null)
					{
						log.endElt(); // end head element
						return ah; // found it
					}
				}
			}
			if (visitor != null) visitor.notFound(path);
			log.endElt(); // end head element
			return null;
		}

		/// <summary>
		/// Searches the path depth-first.
		/// Nth is not used - if it were, it could get a sibling, ancestor or child.
		/// </summary>
		/// <param name="path">The path to search. Each step in the path contains an
		/// Accessibility name and Accessibility role of the matching gui object.</param>
		/// <param name="visitor">null or the object providing methods that are called when steps are found.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		private AccessibilityHelper SearchPathByDepth(GuiPath path, IPathVisitor visitor)
		{
			Log log = Log.getOnly();
			log.writeElt("SearchPathByDepth");
			log.writeAttr("from", this.Role + ":" + this.Name);
			log.writeAttr("to", path.toString());
			log.endElt();

			if (ChildCount <= 0)
			{
				if (visitor != null) visitor.notFound(path);
				return null;
			}
			AccessibilityHelper ah = null;
			// try the client first if there is one, but don't visit it
			AccessibilityHelper client = FindClient();
			if (client != null) ah = client.SearchPathByDepth(path, visitor);
			if (ah != null)
			{
				log.endElt(); // end head element
				return ah;
			}

			// Rats!! It wasn't below the client, the caller wants some system widget or something
			foreach (AccessibilityHelper child in this)
			{
				if (child == null || child.Equals(client))
					continue;
				// does this object match?
				// treat name == "" and child.Name == null as a match
				// Note: a null string shows as "" in the debugger!
				log.writeElt("head");
				log.writeElt("SearchPathByDepth");
				log.writeAttr("path", path.toString());
				log.writeAttr("child", child.Role + ":" + child.Name);
				log.endElt();
				if (MatchNameAndRole(path.Name, path.Role, child))
				{
					if (path.Next != null)
					{
						if (visitor != null) visitor.visitNode(child); // allow processing of this ah by caller
						log.endElt(); // end head element
						return ValueOrChild(path.Next, child, visitor);
					}
				}
				// if not a "real" object, a child takes on it's parent's
				// attributes, so it appears to have the same # of children.
				// The first child is always the first child, so you get
				// infinite recursion on it if you don't check "realness".
				if (child.m_fRealAccessibleObject && child.ChildCount > 0)
				{
					// look through the child objects
					ah = child.SearchPathByDepth(path, visitor);
					if (ah != null)
					{
						if (path.Next != null)
						{
							if (visitor != null) visitor.visitNode(ah); // allow processing of this ah by caller
							log.endElt(); // end head element
							return ValueOrChild(path.Next, ah, visitor);
						}
						log.endElt(); // end head element
						return ah;
					}
				}
			}
			if (visitor != null) visitor.notFound(path);
			log.endElt(); // end head element
			return null;
		}

		/// <summary>
		/// ValueOrChild determines if the ah's value should be macthed or
		/// if a child should be matched.
		/// </summary>
		/// <param name="nextGP">The next path step beyond this ah</param>
		/// <param name="ah">The accessibility object currently considered</param>
		/// <param name="visitor">null or the object providing methods that are called when steps are found.</param>
		/// <returns>An AccessibilityHelper if checking for a value, otherwise null</returns>
		public AccessibilityHelper ValueOrChild(GuiPath nextGP, AccessibilityHelper ah, IPathVisitor visitor)
		{
			bool result = false;
			if (nextGP.Role == AccessibleRole.Alert)
			{ // check if the name is a regular expression
				if (nextGP.Name != null && nextGP.Name.StartsWith("rexp#"))
				{
					Regex rx = new Regex(nextGP.Name.Substring(5));
					result = rx.IsMatch(ah.Value);
					Log log = Log.getOnly();
					log.writeElt("ValueOrChild");
					log.writeAttr("pattern", nextGP.Name.Substring(5));
					log.writeAttr("to", ah.Value);
					log.writeAttr("result", result.ToString());
					log.endElt();
				}
				else result = nextGP.Name == ah.Value; // match the value to the next path's name
				if (!result)
				{ // it didn't match, so the search failed
					if (visitor != null) visitor.notFound(nextGP);
					return null;
				}
			}
			if (result) return ah;
			return ah.SearchPath(nextGP, visitor); // continue on the path
		}

		/// <summary>
		/// Creates a child node list, putting parent's client first.
		/// </summary>
		/// <param name="parent">An ah with children</param>
		/// <returns>An ArrayList of the children</returns>
		private ArrayList MakeChildList(AccessibilityHelper parent)
		{
			ArrayList nodes = new ArrayList(parent.ChildCount);
			//AccessibilityHelper ah = parent.FindClient();
			//if (ah != null) {nodes.Add(ah);} // client first
			foreach(AccessibilityHelper child in parent)
				//if (child != ah)
				if (child != null) nodes.Add(child);
			return nodes;
		}

		/// <summary>
		/// Find the client with the same name as this ah.
		/// </summary>
		/// <returns>The child client with the same name.</returns>
		public AccessibilityHelper FindClient()
		{
			// does this ah have a client with the same name?
			// if so, use it instead - ignore all other children for now (don't waste time searching system widget paths)
			AccessibilityHelper ah = FindDirectChild(this.Name,AccessibleRole.Client);
			if (ah != null)
			{
				Log log = Log.getOnly();
				log.writeElt("FindClient");
				log.writeAttr("found", ah.Role + ":" + ah.Name);
				log.endElt();
			}
			return ah;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the child control with the specified name and/or role, but not below the specified
		/// nLevelsToRecurse. This method recursively searches down the hierarchy until it finds
		/// the control or it has searched nLevelsToRecurse levels.
		/// </summary>
		/// <param name="name">Accessibility name of the matching control, or <c>null</c></param>
		/// <param name="role">Accessibility role of the matching control, or
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <param name="nLevelsToRecurse">The number of levels to search before giving up.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// control.</returns>
		/// </returns>
		/// <remarks>If you provide only one parameter, it matches only that parameter.
		/// If you specify both parameters it tries to match both.</remarks>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper FindChild(string name, AccessibleRole role, int nLevelsToRecurse)
		{
			if (name == null && role == AccessibleRole.None)
				return null;

			AccessibilityHelper ah = null;

			foreach(AccessibilityHelper child in this)
			{
				if (child == null)
					continue;
				// does this object match?
				// treat name == "" and child.Name == null as a match
				// Note: a null string shows as "" in the debugger!
				if (MatchNameAndRole(name, role, child))
					return child;
				if (child.m_fRealAccessibleObject && nLevelsToRecurse > 0)
				{
					// look through the child objects
					ah = child.FindChild(name, role, nLevelsToRecurse-1);
					if (ah != null)
						return ah;
				}
			}
			return null;
		}

		/// <summary>
		/// Like FindChild with nLevelsToRecurse (depth) but it builds a coded path
		/// vector that indicates exactly where the child is in the subtree.
		/// example: If the vector is [2,1,4] the window was found three levels down
		/// as the 2nd child's first child's 4th child. Note, array position 0 is level 1.
		/// If the vector has a 0, the child was not at that level or had no ancestor there.
		/// pathCode must be zeroed before calling.
		/// </summary>
		/// <param name="name">Accessibility name of the matching control, or <c>null</c></param>
		/// <param name="role">Accessibility role of the matching control, or
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <param name="level">This node's level and place in pathCode. First is 1.</param>
		/// <param name="depth">Maximum number of levels to search and
		/// the size of the pathCode array. 1 is the first level (unlike nLevelsToRecurse).</param>
		/// <param name="pathCode">The path coded as child number per level downward.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// control.</returns>
		public AccessibilityHelper LocateChild(string name, AccessibleRole role, int level, int depth, ref int[] pathCode)
		{
			if (name == null && role == AccessibleRole.None || level <= 0 || depth <= 0 || level > depth)
				return null;

			AccessibilityHelper ah = null;
			int  count = 0;
			//bool found = false;

			foreach(AccessibilityHelper child in this)
			{
				count++;
				if (child == null)
					continue;
				// does this object match?
				// treat name == "" and child.Name == null as a match
				// Note: a null string shows as "" in the debugger!
				if (MatchNameAndRole(name, role, child))
				{
					ah = child;
					break; // found
				}
				if (child.m_fRealAccessibleObject && depth > 1)
				{
					// look through the child objects
					ah = child.LocateChild(name, role, level+1, depth, ref pathCode);
					if (ah != null)
						break; // found
				}
			}
			pathCode[level-1] = count;
			return ah;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the child window with the specified name and/or role. This method only looks
		/// in the direct children for the window (no recursion).
		/// </summary>
		/// <param name="name">Accessibility name of the matching window, or <c>null</c></param>
		/// <param name="role">Accessibility role of the matching window, or
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		/// <remarks>If you provide only one parameter, it matches only that parameter.
		/// If you specify both parameters it tries to match both. </remarks>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper FindDirectChild(string name, AccessibleRole role)
		{
			if (name == null && role == AccessibleRole.None)
				return null;
			Log log = Log.getOnly();
			log.writeElt("FindDirectChild");
			log.writeAttr("target", role + ":" + name);
			log.endElt();

			foreach(AccessibilityHelper child in this)
			{
				if (child == null)
					continue;
				log.writeElt("FindDirectChild");
				log.writeAttr("viewed", child.Role + ":" + child.Name);
				log.endElt();

				// does this object match?
				if (MatchNameAndRole(name, role, child))
					return child;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the nth child window with the specified name and/or role.
		/// Beware: you get the nth child in a depth-first traversal.
		/// There is no gaurantee that the name-role duplicates are on the same
		/// tree level; they can be scattered all over the tree.
		/// You will get the same node only if you start from the same place using
		/// the same parameters and if the tree has not dynamically changed - as it
		/// may.
		/// Note: this version differs from the one in the TE project in some ways.
		/// one is that nWhich is not a ref parameter here.
		/// </summary>
		/// <param name="name">Accessibility name of the matching window, <c>null</c>, NAMELESS or #ANY</param>
		/// <param name="role">Accessibility role of the matching window,
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <param name="nWhich">Number of specified child to find. 1 is first.</param>
		/// <param name="nLevelsToRecurse">The last level to search relative to the starting node.
		/// <c>0</c> to search only the immediate children.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		/// <remarks>If you provide only one parameter, it matches only that parameter.
		/// If you specify both parameters it tries to match both. </remarks>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper FindNthChild(string name, AccessibleRole role, int nWhich,
			int nLevelsToRecurse)
		{
			if (name == null && role == AccessibleRole.None)
				return null;

			AccessibilityHelper ah = null;

			foreach(AccessibilityHelper child in this)
			{
				if (child == null)
					continue;
				// does this object match?
				if (MatchNameAndRole(name, role, child))
				{
					nWhich--;
					if (nWhich <= 0)
						return child;
				}
				if (child.m_fRealAccessibleObject)
				{
					if (nLevelsToRecurse > 0)
					{
						// look through the child objects
						ah = child.FindNthChild(name, role, nWhich, nLevelsToRecurse-1);
						if (ah != null)
							return ah;
					}
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigates to the specified UI element and returns that element.
		/// </summary>
		/// <param name="navDir">Direction to navigate.</param>
		/// <returns>An <see cref="AccessibilityHelper"/> object that represents that element,
		/// or <c>null</c> if the element couldn't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper Navigate(AccessibleNavigation navDir)
		{
			return (AccessibilityHelper)base.Navigate1(navDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the focused UI element. Retuns either this one or one of its descendants.
		/// </summary>
		/// <returns>An <see cref="AccessibilityHelper"/> object that represents the focused element,
		/// or <c>null</c> if the element couldn't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper GetFocused
		{
			get { return (AccessibilityHelper)base.FocusedAh; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if this is an accessible object that implements the
		/// <see cref="IAccessible"/> interface.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsRealAccessibleObject
		{
			get { return base.IsRealAccessibleObject; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the default action for the object.
		/// </summary>
		/// <returns><c>true</c> if successful.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool DoDefaultAction()
		{
			return base.DoDefaultAction();
		}

		/// <summary>
		/// Send a Windows message to the window.
		/// </summary>
		/// <param name="wm"></param>
		/// <param name="wparam"></param>
		/// <param name="lparam"></param>
		/// <returns></returns>
		public override int SendWindowMessage(int wm, int wparam, int lparam)
		{
			return base.SendWindowMessage(wm, wparam, lparam);
		}
		/// <summary>
		/// Moves the mouse over this GUI element
		/// </summary>
		public override void MoveMouseOverMe()
		{
			base.MoveMouseOverMe();
		}
		/// <summary>
		/// Moves the mouse over this GUI element
		/// dx and dy are "codes" for where to move to relative to
		/// the left, top accLocation of the accessible object.
		/// If the code is positive, the number represents the number
		/// of pixels from the left, top edge to move to.
		/// If one of these numbers is larger than the object
		/// width or height, the mouse is moved toward the center.
		/// If the code is negative, the number is the percent of
		/// width and height to add to the left top edge to move to.
		/// </summary>
		public override void MoveMouseOverMe(int dx, int dy)
		{
			base.MoveMouseOverMe(dx, dy);
		}

		/// <summary>
		/// Move the mouse relative to this GUI element.
		/// dx and dy are "offsets" for moving to relative to
		/// the left, top accLocation of the accessible object.
		/// The offsets represent the number
		/// of pixels from the left, top edge to move to.
		/// </summary>
		/// <param name="dx">x offset from left of object</param>
		/// <param name="dy">y offset from top of object</param>
		public override void MoveMouseRelative(int dx, int dy)
		{
			base.MoveMouseRelative(dx, dy);
		}

		/// <summary>
		/// Simulates a click on the GUI
		/// </summary>
		public override void SimulateClick()
		{
			base.SimulateClick();
		}

		/// <summary>
		/// Simulates a click on the GUI
		/// See MoveMouseOverMe for notes on dx and dy.
		/// </summary>
		public override void SimulateClick(int dx, int dy)
		{
			base.SimulateClick(dx, dy);
		}

		/// <summary>
		/// Simulates a click on the GUI
		/// See MoveMouseRelative for notes on dx and dy.
		/// </summary>
		public override void SimulateClickRelative(int dx, int dy)
		{
			base.SimulateClickRelative(dx, dy);
		}

		/// <summary>
		/// Simulates a right click on the GUI
		/// </summary>
		public override void SimulateRightClick()
		{
			base.SimulateRightClick();
		}

		/// <summary>
		/// Simulates a right click on the GUI
		/// See MoveMouseOverMe for notes on dx and dy.
		/// </summary>
		public override void SimulateRightClick(int dx, int dy)
		{
			base.SimulateRightClick(dx, dy);
		}

		/// <summary>
		/// Simulates a right click on the GUI
		/// See MoveMouseRelative for notes on dx and dy.
		/// </summary>
		public override void SimulateRightClickRelative(int dx, int dy)
		{
			base.SimulateRightClickRelative(dx, dy);
		}

		/// <summary>
		/// Get a RootBox from the accessible object.
		/// </summary>
		/// <returns></returns>
		unsafe public virtual IVwRootBox RootBox()
		{
			object obj = Marshal.GetObjectForIUnknown(GetIUnknown());
			IOleServiceProvider sp = (IOleServiceProvider) obj;
			if (sp == null)
				return null;
			Guid guidIRoot = Marshal.GenerateGuidForType(typeof(IVwRootBox)); // IID
			Guid guidRoot = Marshal.GenerateGuidForType(typeof(VwRootBox)); // CLSID
			object obj2;
			sp.QueryService(ref guidRoot, ref guidIRoot, out obj2);
			if (obj2 == null)
				return null;
			return (IVwRootBox) obj2;
		}
	}
}
