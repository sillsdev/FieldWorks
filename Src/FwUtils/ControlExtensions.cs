// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class ControlExtensions
	{
		/// <summary>
		/// Find a control
		/// </summary>
		/// <typeparam name="T">A Control instacne, or a subclass of Control.</typeparam>
		/// <param name="me">The control that we want to get the parent from.</param>
		/// <returns>The parent of the given class, or null, if there is none.</returns>
		public static T ParentOfType<T>(this Control me) where T : Control
		{
			if (me?.Parent == null)
			{
				// 'me' is null, or Parent of 'me' is null.
				return null;
			}
			var myParent = me.Parent;
			if (myParent is T)
			{
				return (T)myParent;
			}
			return ParentOfType<T>(myParent);
		}
	}
}