using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// This class exposes a static method for ensuring that a control and all its child controls have accessible names.
	/// If no better name is already provided, it supplies one using the Type of the control, and if necessary an
	/// index.
	/// </summary>
	public class AccessibleNameCreator
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
			if (String.IsNullOrEmpty(root.AccessibleName))
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
