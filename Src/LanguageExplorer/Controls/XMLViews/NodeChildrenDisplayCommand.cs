// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This one is almost the same but processes the CHILDREN of the stored node.
	/// </summary>
	public class NodeChildrenDisplayCommand : NodeDisplayCommand
	{
		/// <summary />
		public NodeChildrenDisplayCommand(XElement node)
			: base(node)
		{
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vc.ProcessChildren(Node, vwenv, hvo);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			DetermineNeededFieldsForChildren(vc, Node, null, info);
		}

		/// <summary>
		/// Make it work sensibly as a hash key. Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		public override bool Equals(object obj)
		{
			return base.Equals(obj) && obj is NodeChildrenDisplayCommand;
		}

		/// <summary>
		/// Compiler requires override since Equals is overridden.
		/// Make it work sensibly as a hash key. Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}