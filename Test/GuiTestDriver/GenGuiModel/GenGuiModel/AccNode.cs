// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;

namespace GenGuiModel
{
	/// <summary>
	/// Summary description for AccNode.
	/// </summary>
	public class AccNode
	{
		// data from AccExplorer
		string m_Name = null;
		string m_Action  = null;
		string m_Description = null;
		string m_Help = null;
		string m_HelpTopic = null;
		string m_Shortcut = null;
		int m_LocationX = 0;
		int m_LocationY = 0;
		int m_LocationH = 0;
		int m_LocationW = 0;
		int m_NumChildren = 0;
		string m_Role = null;
		string m_State = null;
		string m_Value = null;

		// generated data
		AccNode m_Parent = null;
		ArrayList m_Children = new ArrayList(5);
		int m_Level = 0;
		int m_Sibling = 0;

		public AccNode(AccNode parent, string name, string role)
		{
			m_Parent = parent;
			m_Name = name;
			m_Role = role;
		}

		public IEnumerator getChildren() {return m_Children.GetEnumerator();}

		public int Level
		{
			get {return m_Level;}
			set {m_Level = value;}
		}

		public int Sibling
		{
			get {return m_Sibling;}
			set {m_Sibling = value;}
		}

		public string Role
		{
			get {return m_Role;}
			set {m_Role = value;}
		}

		public string Name
		{
			get {return m_Name;}
			set {m_Name = value;}
		}

		public string Action
		{
			get {return m_Action;}
			set {m_Action = value;}
		}

		public string Description
		{
			get {return m_Description;}
			set {m_Description = value;}
		}

		public string Help
		{
			get {return m_Help;}
			set {m_Help = value;}
		}

		public string HelpTopic
		{
			get {return m_HelpTopic;}
			set {m_HelpTopic = value;}
		}

		public string Shortcut
		{
			get {return m_Shortcut;}
			set {m_Shortcut = value;}
		}

		public int LocationX
		{
			get {return m_LocationX;}
			set {m_LocationX = value;}
		}

		public int LocationY
		{
			get {return m_LocationY;}
			set {m_LocationY = value;}
		}

		public int LocationH
		{
			get {return m_LocationH;}
			set {m_LocationH = value;}
		}

		public int LocationW
		{
			get {return m_LocationW;}
			set {m_LocationW = value;}
		}

		public int NumChildren
		{
			get {return m_NumChildren;}
			set {m_NumChildren = value;}
		}

		public string State
		{
			get {return m_State;}
			set {m_State = value;}
		}

		public string Value
		{
			get {return m_Value;}
			set {m_Value = value;}
		}

		public void addChild(AccNode child)
		{
			m_Children.Add(child);
		}

		public static AccNode parseNCreate(AccNode parent, int sibNum, ref IEnumerator it)
		{
			string image = (string)it.Current;
			int before   = skipLeveling(image); // skip the | '--- stuff
			int after    = -1;
			// rs.ReadLine breaks lines at \r which can be data
			// So, if the cue isn't found, tack the next line to the image.
			while ((after = image.IndexOf("[")) == -1)
				image = fixLine(image, ref it);
			string name  = image.Substring(before, after-before);
			string role  = getValueFromLine(", Role: ", ", State:", image, ref it);
			AccNode node = new AccNode(parent, name, role);

			node.Level       = (int)Math.Ceiling((before-1.0)/6.0); // not a bad estimate
			node.Sibling     = sibNum;

			node.Action      = getValueFromLine("] {Action: ", ", Description:", image, ref it);
			node.Description = getValueFromLine(", Description: ", ", Help:", image, ref it);
			node.Help        = getValueFromLine(", Help: ", ", Help Topic:", image, ref it);
			node.HelpTopic   = getValueFromLine(", Help Topic: ", ", Shortcut:", image, ref it);
			node.Shortcut    = getValueFromLine(", Shortcut: ", ", Location:", image, ref it);
			string loc       = getValueFromLine(", Location: ", ", Num Children:", image, ref it);
			int x = 0, y = 0, w = 0, h = 0;
			if (loc != null) getLocFromValue(loc, out x, out y, out h, out w);
			node.LocationX   = x; // can't set a property as an out parameter!
			node.LocationY   = y;
			node.LocationH   = h;
			node.LocationW   = w;
			node.NumChildren = getIntFromLine(", Num Children: ", ", Role:", image);
			node.State       = getValueFromLine(", State: ", ", Value:", image, ref it);
			node.Value       = getValueFromLine(", Value: ", null, image, ref it);
			return node;
		}

		/// <summary>
		/// rs.ReadLine breaks lines at \r which can be data.
		/// So, if the cue isn't found, tack the next line to the image
		/// and insert a &nl; (new line) entity.
		/// </summary>
		/// <param name="image">The current line image</param>
		/// <param name="it">The line iterator</param>
		/// <returns>A line image with the next one appended</returns>
		public static string fixLine(string image, ref IEnumerator it)
		{
			string newImage = image;
			if (it.MoveNext()) newImage += "&amp;nl;" + it.Current;
			// else this file ended abruptly!
			return newImage;
		}


		/// <summary>
		/// Skip the leveling representation at the front of the accExplorer text line
		/// </summary>
		/// <param name="image">the text to search</param>
		/// <returns>The position of the node name</returns>
		private static int skipLeveling(string image)
		{
			int len = image.IndexOf("---  ") + 5; // this is the position of the node name
			if (len == 4) len = 1; // the node name starts here
			return len;
		}

		/// <summary>
		/// Return the snip of data from between the before and after delimiters.
		/// If it is "?", then return null.
		/// </summary>
		/// <param name="beforeDelimiter">text before the snip, including spaces</param>
		/// <param name="afterDelimiter">text after the snip including punctuation</param>
		/// <param name="image">the text containing the delimiters and desired snip</param>
		/// <returns>All the text between the delimiters or null if it empty or "?"</returns>
		private static string getValueFromLine(string beforeDelimiter, string afterDelimiter, string image, ref IEnumerator it)
		{
			int before = image.IndexOf(beforeDelimiter) + beforeDelimiter.Length; // skip the | '--- stuff
			int after = image.Length - 1;
			if (afterDelimiter == null) // find the end of the line - it may be cut off!
			{ // "}" is the last char on the line
				afterDelimiter = "}";
				while ((after = image.Substring(before).IndexOf(afterDelimiter)) == -1)
					image = fixLine(image, ref it);
				after = image.Length - 1;
			}
			if (afterDelimiter != null)
			{
				// rs.ReadLine breaks lines at \r which can be data
				// So, if the cue isn't found, tack the next line to the image.
				while ((after = image.IndexOf(afterDelimiter)) == -1)
					image = fixLine(image, ref it);
			}
			string snip = image.Substring(before, after-before);
			if (snip.Equals("?")) snip = null;
			return snip;
		}
		/// <summary>
		/// Return the number from between the before and after delimiters.
		/// If it is "?", then return 0.
		/// </summary>
		/// <param name="beforeDelimiter">text before the number, including spaces</param>
		/// <param name="afterDelimiter">text after the number including punctuation</param>
		/// <param name="image">the text containing the delimiters and desired number</param>
		/// <returns>The number between the delimiters or 0 if it empty or "?"</returns>
		private static int getIntFromLine(string beforeDelimiter, string afterDelimiter, string image)
		{
			int number = 0;
			int before = image.IndexOf(beforeDelimiter) + beforeDelimiter.Length; // skip the | '--- stuff
			int after = image.IndexOf(afterDelimiter);
			string snip = image.Substring(before, after-before);
			if (snip.Equals("?")) snip = null;
			number = Convert.ToInt32(snip);
			return number;
		}

		private static void getLocFromValue(string loc, out int x, out int y, out int h, out int w)
		{ // (173, 492, 191, 517)
			x = 0;
			y = 0;
			h = 0;
			w = 0;
			if (loc.Length > 1)
			{
				int comma1 = 1;
				int comma2 = loc.IndexOf(",");
				x = Convert.ToInt32(loc.Substring(comma1, comma2-1));
				comma1 = comma2+2;
				comma2 = loc.IndexOf(",",comma1);
				y = Convert.ToInt32(loc.Substring(comma1, comma2-comma1));
				comma1 = comma2+2;
				comma2 = loc.IndexOf(",",comma1);
				h = Convert.ToInt32(loc.Substring(comma1, comma2-comma1));
				comma1 = comma2+2;
				comma2 = loc.IndexOf(")",comma1);
				w = Convert.ToInt32(loc.Substring(comma1, comma2-comma1));
			}
		}

	}
}
