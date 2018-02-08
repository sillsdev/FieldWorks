// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class implements PerformDisplay by having the VC directly ProcessFrag
	/// on an XElement that it stores.
	/// </summary>
	public class NodeDisplayCommand : DisplayCommand
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeDisplayCommand"/> class.
		/// </summary>
		public NodeDisplayCommand(XElement node)
		{
			Node = node;
		}

		/// <summary>
		/// Gets the node.
		/// </summary>
		public XElement Node { get; }

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			var logStream = vc.LogStream;
			if (logStream != null)
			{

				logStream.WriteLine("Display " + hvo + " using " + Node);
				logStream.IncreaseIndent();
			}

			vc.ProcessFrag(Node, vwenv, hvo, true, null);

			logStream?.DecreaseIndent();
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			vc.DetermineNeededFieldsFor(Node, null, info);
		}

		/// <summary>
		/// Make it work sensibly as a hash key. Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			var other = obj as NodeDisplayCommand;
			if (other == null)
			{
				return false;
			}
			return other.Node == Node;
		}

		/// <summary>
		/// Make it work sensibly as a hash key. Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return Node.GetHashCode();
		}
	}
}