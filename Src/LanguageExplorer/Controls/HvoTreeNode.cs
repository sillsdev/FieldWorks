// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This class is used for tree nodes in the popup list. It adds an Hvo property to track which selection
	/// is meant by each node.
	/// </summary>
	public class HvoTreeNode : TreeNode, IComparable<HvoTreeNode>
	{
		private ITsString m_text;

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		public int CompareTo(HvoTreeNode other)
		{
			return Text.CompareTo(other.Text);
		}

		/// <summary>
		/// Gets or sets the HVO for the node.
		/// </summary>
		public int Hvo { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public HvoTreeNode(ITsString label, int hvo) : base(label.Text)
		{
			Hvo = hvo;
			m_text = label;
			Name = label.Text; // allows Find() to find nodes.
		}

		/// <summary />
		public HvoTreeNode NodeWithHvo(int hvoToSelect)
		{
			HvoTreeNode revtal = null;
			if (Hvo == hvoToSelect)
			{
				revtal = this;
			}
			else
			{
				foreach (HvoTreeNode htn in Nodes)
				{
					var match = htn.NodeWithHvo(hvoToSelect);
					if (match != null)
					{
						revtal = htn;
						break;
					}
				}
			}
			return revtal;
		}

		/// <summary>
		/// Gets or sets the TSS.
		/// </summary>
		public ITsString Tss
		{
			get
			{
				return m_text;
			}

			set
			{
				m_text = value;
				Text = value.Text;
			}
		}
	}
}
