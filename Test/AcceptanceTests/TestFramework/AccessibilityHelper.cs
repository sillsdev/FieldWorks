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

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// <summary>
	/// Summary description for AccessibilityHelper.
	/// </summary>
	public class AccessibilityHelper:AccessibilityHelperBase
	{
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
			get {return base.HWnd;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible name for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Name
		{
			get {return base.Name;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible role for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override AccessibleRole Role
		{
			get {return base.Role;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible states for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override AccessibleStates States
		{
			get {return base.States;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible keyboard shortcut for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Shortcut
		{
			get {return base.Shortcut;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessible default action for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string DefaultAction
		{
			get {return base.DefaultAction;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value for this accessible window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Value
		{
			get {return base.Value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of children.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int ChildCount
		{
			get { return  base.ChildCount; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent accessible object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper Parent
		{
			get { return (AccessibilityHelper)base.Parent1; }
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
				if ((name == null || name == child.Name) &&
					(role == AccessibleRole.None || role == child.Role))
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

			foreach(AccessibilityHelper child in this)
			{
				if (child == null)
					continue;
				// does this object match?
				if ((name == null || name == child.Name) &&
					(role == AccessibleRole.None || role == child.Role))
					return child;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the nth child window with the specified name and/or role.
		/// </summary>
		/// <param name="name">Accessibility name of the matching window, or <c>null</c></param>
		/// <param name="role">Accessibility role of the matching window, or
		/// <see cref="AccessibleRole.None"/>.</param>
		/// <param name="nWhich">Number of specified child to find</param>
		/// <param name="nLevelsToRecurse">How much levels to go down. <c>0</c> if you want
		/// to search only in the immediate children.</param>
		/// <returns>A new <see cref="AccessibilityHelper"/> object that wraps the matched
		/// window.</returns>
		/// <remarks>If you provide only one parameter, it matches only that parameter.
		/// If you specify both parameters it tries to match both. </remarks>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper FindNthChild(string name, AccessibleRole role, ref int nWhich,
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
				if ((name == null || name == child.Name) &&
					(role == AccessibleRole.None || role == child.Role))
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
						ah = child.FindNthChild(name, role, ref nWhich, nLevelsToRecurse-1);
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

	}
}
