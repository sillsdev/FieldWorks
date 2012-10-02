using System;
using System.IO;
using System.Collections;

namespace GenGuiModel
{
	/// <summary>
	/// Summary description for Converter.
	/// </summary>
	public class Converter
	{
		string m_Source = null;
		string m_Target = null;

		public Converter(string source, string target)
		{
			m_Source = source;
			m_Target = target;
		}

		public bool go ()
		{
			ArrayList nodes = readAccExplorerText();
			AccNode ansRoot = parseLines(nodes);
			writeModel(ansRoot);
			return true;
		}

		private ArrayList readAccExplorerText()
		{
			ArrayList nodes = new ArrayList(1000);
			StreamReader sr = File.OpenText(m_Source);
			String nodeLine = null;
			while ((nodeLine = sr.ReadLine()) != null)
			{
				nodes.Add(nodeLine);
			}
			sr.Close();
			return nodes;
		}

		private AccNode parseLines(ArrayList accLines)
		{
			AccNode parent = null;
			IEnumerator it = accLines.GetEnumerator();
			AccNode an = BuildNode(parent, ref it, 0);
			return an;
		}

		private AccNode BuildNode(AccNode parent, ref IEnumerator it, int sib)
		{
			AccNode an = null;
			if (!it.MoveNext()) return an; // end of data lines
			an = AccNode.parseNCreate(parent, sib, ref it);
			if (an.NumChildren > 0)
			{ // this is a child of parent
				AccNode child = null;
				for (int i = 0; i < an.NumChildren; i++)
				{
					child = BuildNode(an, ref it, i);
					an.addChild(child);
				}
			}
			return an;
		}

		private bool writeModel(AccNode root)
		{
			StreamWriter sw = File.CreateText(m_Target);
			sw.WriteLine (@"<?xml version=""1.0"" encoding=""UTF-8""?>");
			sw.WriteLine (@"<?xml-stylesheet type=""text/xsl"" href=""Convert.xsl""?>");

			sw.WriteLine (@"<gui-tree>");
			writeNode(sw, root, 0);
			sw.WriteLine (@"</gui-tree>");

			sw.Close();
			return true;
		}

		private bool writeNode (StreamWriter sw, AccNode node, int level)
		{
			string role = (node.Role).Replace(' ','-');
			sw.Write (@"<{0}",role);
			sw.Write (@" name=""{0}""",node.Name);
			sw.Write (@" lev=""{0}""",level); // not node.Level
			sw.Write (@" sib=""{0}""",node.Sibling);
			if (node.Shortcut != null)		sw.Write (@" sc=""{0}""",node.Shortcut);
			if (node.State != null)			sw.Write (@" st=""{0}""",node.State);
			if (node.Description != null)	sw.Write (@" desc=""{0}""",(node.Description).Replace("<","&lt;"));
			if (node.Action != null)		sw.Write (@" act=""{0}""",node.Action);
			if (node.Help != null)			sw.Write (@" help=""{0}""",node.Help);
			if (node.HelpTopic != null)		sw.Write (@" htop=""{0}""",node.HelpTopic);
			if (node.LocationX != 0)		sw.Write (@" locX=""{0}""",node.LocationX);
			if (node.LocationY != 0)		sw.Write (@" locY=""{0}""",node.LocationY);
			if (node.LocationH != 0)		sw.Write (@" locH=""{0}""",node.LocationH);
			if (node.LocationW != 0)		sw.Write (@" locW=""{0}""",node.LocationW);
			if (node.NumChildren != 0)	    sw.Write (@" child=""{0}""",node.NumChildren);
			if (node.Value != null)			sw.Write (@" value=""{0}""",node.Value);

			IEnumerator it = node.getChildren();
			if (!it.MoveNext()) sw.WriteLine (@"/>");
			else
			{
				it.Reset();
				sw.WriteLine (@">");
				while (it.MoveNext() && it.Current != null) // shouldn't be null!
					writeNode(sw,(AccNode)it.Current,level+1);
				sw.WriteLine (@"</{0}>",role);
			}
			return true;
		}
	}
}
