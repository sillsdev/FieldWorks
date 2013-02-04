using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcGroupNode : ComplexConcPatternNode
	{
		private ObservableCollection<ComplexConcPatternNode> m_nodes;

		public override IList<ComplexConcPatternNode> Children
		{
			get
			{
				if (m_nodes == null)
				{
					m_nodes = new ObservableCollection<ComplexConcPatternNode>();
					m_nodes.CollectionChanged += ChildrenChanged;
				}
				return m_nodes;
			}
		}

		public override bool IsLeaf
		{
			get { return m_nodes == null || m_nodes.Count == 0; }
		}

		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			var group = new Group<ComplexConcParagraphData, ShapeNode>();
			Alternation<ComplexConcParagraphData, ShapeNode> alternation = null;
			bool inAlternation = false;
			foreach (ComplexConcPatternNode child in Children)
			{
				if (child is ComplexConcOrNode)
				{
					if (alternation == null)
					{
						alternation = new Alternation<ComplexConcParagraphData, ShapeNode>();
						alternation.Children.Add(group.Children.Last);
					}
					inAlternation = true;
				}
				else
				{
					if (!inAlternation && alternation != null)
					{
						group.Children.Add(alternation);
						alternation = null;
					}

					PatternNode<ComplexConcParagraphData, ShapeNode> newNode = child.GeneratePattern(featSys);
					if (inAlternation)
					{
						alternation.Children.Add(newNode);
						inAlternation = false;
					}
					else
					{
						group.Children.Add(newNode);
					}
				}
			}

			if (alternation != null)
				group.Children.Add(alternation.Children.Count == 1 ? alternation.Children.First : alternation);

			return AddQuantifier(group);
		}

		private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					AddNodes(e.NewItems.Cast<ComplexConcPatternNode>());
					break;

				case NotifyCollectionChangedAction.Remove:
					RemoveNodes(e.OldItems.Cast<ComplexConcPatternNode>());
					break;

				case NotifyCollectionChangedAction.Replace:
					RemoveNodes(e.OldItems.Cast<ComplexConcPatternNode>());
					AddNodes(e.NewItems.Cast<ComplexConcPatternNode>());
					break;

				case NotifyCollectionChangedAction.Reset:
					break;
			}
		}

		private void AddNodes(IEnumerable<ComplexConcPatternNode> nodes)
		{
			foreach (ComplexConcPatternNode node in nodes)
				node.Parent = this;
		}

		private void RemoveNodes(IEnumerable<ComplexConcPatternNode> nodes)
		{
			foreach (ComplexConcPatternNode node in nodes)
				node.Parent = null;
		}
	}
}
