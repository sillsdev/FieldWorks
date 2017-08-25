// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// This class exposes a static method for ensuring that a control and all its child controls have accessible names.
	/// If no better name is already provided, it supplies one using the Type of the control, and if necessary an
	/// index.
	/// </summary>
	public static class AccessibleNameCreator
	{
		/// <summary>
		/// Add arbitrary accessibility names to every control owned by the root.
		/// </summary>
		/// <param name="root"></param>
		public static void AddNames(Control root)
		{
			AddNames(root, new Dictionary<string, int>());
		}

		/// <summary>
		/// Add names, using a dictionary to keep track of the number of times a given name has been used before.
		/// </summary>
		private static void AddNames(Control root, Dictionary<string, int> previousOccurrences)
		{
			if (string.IsNullOrEmpty(root.AccessibleName))
			{
				var baseName = root.GetType().Name;
				int previous;
				if (!previousOccurrences.TryGetValue(baseName, out previous))
				{
					previous = 0;
				}
				previous++;
				root.AccessibleName = baseName + previous;
				previousOccurrences[baseName] = previous;
			}
			foreach (Control control in root.Controls)
				AddNames(control, previousOccurrences);
		}
	}
}
