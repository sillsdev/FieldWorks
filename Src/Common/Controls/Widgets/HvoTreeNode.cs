using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// This class is used for tree nodes in the popup list. It adds an Hvo property to track which selection
	/// is meant by each node.
	/// </summary>
	public class HvoTreeNode : TreeNode, IComparable<HvoTreeNode>
	{
		private int m_hvo;
		private ITsString m_text;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(HvoTreeNode other)
		{
			return Text.CompareTo(other.Text);
		}

		/// <summary>
		/// Gets or sets the HVO for the node.
		/// </summary>
		public int Hvo
		{
			get
			{
				return m_hvo;
			}
			set
			{
				m_hvo = value;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="hvo"></param>
		public HvoTreeNode(ITsString label, int hvo) : base(label.Text)
		{
			m_hvo = hvo;
			m_text = label;
			Name = label.Text; // allows Find() to find nodes.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoToSelect"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public HvoTreeNode NodeWithHvo(int hvoToSelect)
		{
			HvoTreeNode revtal = null;
			if (m_hvo == hvoToSelect)
			{
				revtal = this;
			}
			else
			{
				foreach (HvoTreeNode htn in Nodes)
				{
					HvoTreeNode match = htn.NodeWithHvo(hvoToSelect);
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
		/// <value>The TSS.</value>
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

	/// <summary>
	/// An item in the combo list that groups an object ID with the string to display.
	/// </summary>
	public class HvoTssComboItem : ITssValue
	{
		/// <summary>
		/// the analysis chosen if this combo item is chosen. (May be a WfiGloss or WfiAnalysis.)
		/// </summary>
		protected int m_hvo;
		/// <summary>
		/// the text to display.
		/// </summary>
		protected ITsString m_text;
		/// <summary>
		/// special tag, specifying otherwise ambiguious combo selections. default = 0;
		/// </summary>
		protected int m_tag;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		/// Special combo item tag, to further identify otherwise ambiguious combo selections. default = 0;
		/// </summary>
		public int Tag
		{
			get { return m_tag; }
		}

		/// <summary>
		/// Item for the choose-analysis combo box.
		/// Constructed with an ITsString partly because this is convenient for all current
		/// creators, but also because one day we may do this with a FieldWorks combo that
		/// really takes advantage of them.
		/// </summary>
		/// <param name="hvoAnalysis"></param>
		/// <param name="text"></param>
		public HvoTssComboItem(int hvoAnalysis, ITsString text)
		{
			Init(hvoAnalysis, text, 0);
		}

		/// <summary>
		/// Item for the choose-analysis combo box.
		/// </summary>
		/// <param name="hvoAnalysis"></param>
		/// <param name="text"></param>
		/// <param name="tag">special tag, to identify an otherwise ambiguious combo selections.</param>
		public HvoTssComboItem(int hvoAnalysis, ITsString text, int tag)
		{
			Init(hvoAnalysis, text, tag);
		}

		private void Init(int hvoAnalysis, ITsString text, int tag)
		{
			m_hvo = hvoAnalysis;
			m_text = text;
			m_tag = tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_text.Text;
		}

		#region ITssValue implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString AsTss
		{
			get { return m_text; }
		}

		#endregion ITssValue implementation
	}
}
