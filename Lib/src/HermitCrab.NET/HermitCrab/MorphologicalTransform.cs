using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This abstract class is extended by each type of morphological output record
	/// used on the RHS of morphological rules.
	/// </summary>
	public abstract class MorphologicalOutput
	{
		protected int m_partition = -1;
		protected bool m_redup = false;

		/// <summary>
		/// Gets the partition.
		/// </summary>
		/// <value>The partition.</value>
		public int Partition
		{
			get
			{
				return m_partition;
			}
		}

		public bool IsReduplication
		{
			get
			{
				return m_redup;
			}

			internal set
			{
				m_redup = value;
			}
		}

		/// <summary>
		/// Generates the RHS template of a morphological rule. If the record
		/// modifies the input using a simple context, it should add that context
		/// to varFeats of modify-from contexts. It is used during unapplication.
		/// The reduplication flags are used to indicate that the same input partition
		/// is being copied multiple times.
		/// </summary>
		/// <param name="rhsTemp">The RHS template.</param>
		/// <param name="lhs">The LHS.</param>
		/// <param name="modifyFromCtxts">The modify-from contexts.</param>
		/// <param name="redup">The reduplication flag.</param>
		public abstract void GenerateRHSTemplate(PhoneticPattern rhsTemp, IList<PhoneticPattern> lhs,
			IDictionary<int, SimpleContext> modifyFromCtxts);


		/// <summary>
		/// Applies this output record to the specified word synthesis.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="morpheme">The morpheme info.</param>
		public abstract void Apply(Match match, WordSynthesis input, WordSynthesis output, Allomorph allomorph);
	}

	/// <summary>
	/// This morphological output record is used to copy a partition from the input and
	/// append it to the output. This can be used to perform reduplication.
	/// </summary>
	public class CopyFromInput : MorphologicalOutput
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CopyFromInput"/> class.
		/// </summary>
		/// <param name="partition">The partition to copy.</param>
		public CopyFromInput(int partition)
		{
			m_partition = partition;
		}

		/// <summary>
		/// Generates the RHS template of a morphological rule.
		/// </summary>
		/// <param name="rhsTemp">The RHS template.</param>
		/// <param name="lhs">The LHS.</param>
		/// <param name="modifyFromCtxts">The modify-from contexts.</param>
		/// <param name="redup">The reduplication flag.</param>
		public override void GenerateRHSTemplate(PhoneticPattern rhsTemp, IList<PhoneticPattern> lhs,
			IDictionary<int, SimpleContext> modifyFromCtxts)
		{
			// copy LHS partition over the RHS, only indicate the partition if this is
			// the first time this partition is being copied
			rhsTemp.AddPartition(lhs[m_partition], m_redup ? -1 : m_partition);
		}

		/// <summary>
		/// Copies a partition from the input phonetic shape to the output phonetic shape.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="morpheme">The morpheme info.</param>
		public override void Apply(Match match, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			IList<PhoneticShapeNode> nodes = match.GetPartition(m_partition);
			if (nodes != null && nodes.Count > 0)
			{
				Morph morph = null;
				for (PhoneticShapeNode node = nodes[0]; node != nodes[nodes.Count - 1].Next; node = node.Next)
				{
					PhoneticShapeNode newNode = node.Clone();
					// mark the reduplicated segments with the gloss partition
					if (m_redup)
					{
						if (allomorph != null)
						{
							if (morph == null)
							{
								morph = new Morph(allomorph);
								output.Morphs.Add(morph);
							}
							newNode.Partition = morph.Partition;
							morph.Shape.Add(node.Clone());
						}
						else
						{
							newNode.Partition = -1;
						}
					}
					else if (node.Partition != -1)
					{
						if (morph == null || morph.Partition != node.Partition)
						{
							morph = input.Morphs[node.Partition].Clone();
							morph.Shape.Clear();
							output.Morphs.Add(morph);
						}
						newNode.Partition = morph.Partition;
						morph.Shape.Add(node.Clone());
					}
					output.Shape.Add(newNode);
				}
			}
		}
	}

	/// <summary>
	/// This morphological output record is used to insert a segment to the output
	/// based on a simple context.
	/// </summary>
	public class InsertSimpleContext : MorphologicalOutput
	{
		SimpleContext m_ctxt;

		/// <summary>
		/// Initializes a new instance of the <see cref="InsertSimpleContext"/> class.
		/// </summary>
		/// <param name="ctxt">The simple context.</param>
		public InsertSimpleContext(SimpleContext ctxt)
		{
			m_ctxt = ctxt;
		}

		/// <summary>
		/// Generates the RHS template of a morphological rule.
		/// </summary>
		/// <param name="rhsTemp">The RHS template.</param>
		/// <param name="lhs">The LHS.</param>
		/// <param name="modifyFromCtxts">The modify-from contexts.</param>
		/// <param name="redup">The reduplication flag.</param>
		public override void GenerateRHSTemplate(PhoneticPattern rhsTemp, IList<PhoneticPattern> lhs,
			IDictionary<int, SimpleContext> modifyFromCtxts)
		{
			rhsTemp.Add(m_ctxt.Clone());
		}

		/// <summary>
		/// Inserts a segment based on a simple context to the output.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="morpheme">The morpheme info.</param>
		public override void Apply(Match match, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			Segment newSeg = m_ctxt.ApplyInsertion(match.VariableValues);
			if (allomorph != null)
			{
				Morph morph = new Morph(allomorph);
				output.Morphs.Add(morph);
				morph.Shape.Add(newSeg.Clone());
				newSeg.Partition = morph.Partition;
			}
			output.Shape.Add(newSeg);
		}
	}

	/// <summary>
	/// This morphological output record is used to modify an input partition
	/// using a simple context. This can be used for simulfixes.
	/// </summary>
	public class ModifyFromInput : MorphologicalOutput
	{
		SimpleContext m_ctxt;
		Morpher m_morpher;

		/// <summary>
		/// Initializes a new instance of the <see cref="ModifyFromInput"/> class.
		/// </summary>
		/// <param name="partition">The partition to modify.</param>
		/// <param name="ctxt">The simple context.</param>
		/// <param name="morpher">The morpher.</param>
		public ModifyFromInput(int partition, SimpleContext ctxt, Morpher morpher)
		{
			m_partition = partition;
			m_ctxt = ctxt;
			m_morpher = morpher;
		}

		/// <summary>
		/// Generates the RHS template of a morphological rule.
		/// </summary>
		/// <param name="rhsTemp">The RHS template.</param>
		/// <param name="lhs">The LHS.</param>
		/// <param name="modifyFromCtxts">The modify-from contexts.</param>
		/// <param name="redup">The reduplication flag.</param>
		public override void GenerateRHSTemplate(PhoneticPattern rhsTemp, IList<PhoneticPattern> lhs,
			IDictionary<int, SimpleContext> modifyFromCtxts)
		{
			PhoneticPattern ctxtPattern = new PhoneticPattern();
			ctxtPattern.Add(m_ctxt);
			// combines the simple context with all of the simple contexts on the LHS
			// and adds the results to the RHS template
			PhoneticPattern pattern = GenerateChangePartition(lhs[m_partition], ctxtPattern);
			rhsTemp.AddPartition(pattern, m_redup ? -1 : m_partition);
			// add context to the modify-from contexts
			modifyFromCtxts[m_partition] = m_ctxt;
		}

		PhoneticPattern GenerateChangePartition(PhoneticPattern lhs, PhoneticPattern rhs)
		{
			PhoneticPattern result = new PhoneticPattern();
			foreach (PhoneticPatternNode node in lhs)
			{
				switch (node.Type)
				{
					case PhoneticPatternNode.NodeType.SIMP_CTXT:
						PhoneticPattern temp = new PhoneticPattern();
						temp.Add(node.Clone());
						// generates the RHS template the same way that phonological rules generate their
						// RHS analysis targets
						result.AddMany(rhs.Combine(temp));
						break;

					case PhoneticPatternNode.NodeType.PATTERN:
						NestedPhoneticPattern nestedPattern = node as NestedPhoneticPattern;
						PhoneticPattern pattern = GenerateChangePartition(nestedPattern.Pattern, rhs);
						NestedPhoneticPattern newNestedPattern = new NestedPhoneticPattern(pattern, nestedPattern.MinOccur,
							nestedPattern.MaxOccur);
						result.Add(newNestedPattern);
						break;
				}
			}
			return result;
		}

		/// <summary>
		/// Applies the simple context to the input partition and copies it over to the output
		/// phonetic shape.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="morpheme">The morpheme info.</param>
		public override void Apply(Match match, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			IList<PhoneticShapeNode> nodes = match.GetPartition(m_partition);
			if (nodes != null && nodes.Count > 0)
			{
				Morph morph = null;
				if (allomorph != null)
				{
					morph = new Morph(allomorph);
					output.Morphs.Add(morph);
				}
				for (PhoneticShapeNode node = nodes[0]; node != nodes[nodes.Count - 1].Next; node = node.Next)
				{
					PhoneticShapeNode newNode = node.Clone();
					if (node.Type == PhoneticShapeNode.NodeType.SEGMENT)
					{
						Segment seg = newNode as Segment;
						// sets the context's features on the segment
						m_ctxt.Apply(seg, match.VariableValues);
						seg.IsClean = false;
						seg.Partition = morph == null ? -1 : morph.Partition;
					}
					if (morph != null)
						morph.Shape.Add(newNode.Clone());
					output.Shape.Add(newNode);
				}
			}
		}
	}

	/// <summary>
	/// This morphological output record is used to insert segments and boundaries in to
	/// the output phonetic shape. This can be used for prefixes, suffixes, circumfixes,
	/// and infixes.
	/// </summary>
	public class InsertSegments : MorphologicalOutput
	{
		PhoneticShape m_pshape;

		/// <summary>
		/// Initializes a new instance of the <see cref="InsertSegments"/> class.
		/// </summary>
		/// <param name="pshape">The phonetic shape.</param>
		public InsertSegments(PhoneticShape pshape)
		{
			m_pshape = pshape;
		}

		/// <summary>
		/// Generates the RHS template.
		/// </summary>
		/// <param name="rhsTemp">The RHS template.</param>
		/// <param name="lhs">The LHS.</param>
		/// <param name="modifyFromCtxts">The modify-from contexts.</param>
		/// <param name="redup">The reduplication flag.</param>
		public override void GenerateRHSTemplate(PhoneticPattern rhsTemp, IList<PhoneticPattern> lhs,
			IDictionary<int, SimpleContext> modifyFromCtxts)
		{
			for (PhoneticShapeNode node = m_pshape.Begin; node != m_pshape.Last; node = node.Next)
			{
				// create contexts from the segments and boundaries in the phonetic shape
				// and append them to the RHS template
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.SEGMENT:
						rhsTemp.Add(new SegmentContext(node as Segment));
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						rhsTemp.Add(new BoundaryContext(node as Boundary));
						break;
				}
			}
		}

		/// <summary>
		/// Inserts the segments and boundaries in to the output phonetic shape.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="morpheme">The morpheme info.</param>
		public override void Apply(Match match, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			Morph morph = null;
			if (allomorph != null)
			{
				morph = new Morph(allomorph);
				output.Morphs.Add(morph);
			}
			for (PhoneticShapeNode node = m_pshape.Begin; node != m_pshape.Last; node = node.Next)
			{
				PhoneticShapeNode newNode = node.Clone();
				if (morph != null)
				{
					newNode.Partition = morph.Partition;
					morph.Shape.Add(node.Clone());
				}
				else
				{
					newNode.Partition = -1;
				}
				output.Shape.Add(newNode);
			}
		}
	}

	/// <summary>
	/// This class represents a morphological transform. It is used by morphological rules to apply
	/// affixes and root compounds to a stem. It uses morphological output records to define the
	/// transformation process.
	/// </summary>
	public class MorphologicalTransform
	{
		/// <summary>
		/// The full reduplication morph type
		/// </summary>
		public enum RedupMorphType
		{
			/// <summary>
			/// Prefix
			/// </summary>
			PREFIX,
			/// <summary>
			/// Suffix
			/// </summary>
			SUFFIX,
			/// <summary>
			/// Implicit
			/// </summary>
			IMPLICIT
		}

		List<PhoneticPattern> m_lhs;
		List<MorphologicalOutput> m_rhs;
		Dictionary<int, SimpleContext> m_modifyFromCtxts;
		PhoneticPattern m_rhsTemp;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphologicalTransform"/> class.
		/// </summary>
		/// <param name="lhs">The LHS.</param>
		/// <param name="rhs">The RHS.</param>
		public MorphologicalTransform(IEnumerable<PhoneticPattern> lhs, IEnumerable<MorphologicalOutput> rhs, RedupMorphType redupMorphType)
		{
			m_lhs = new List<PhoneticPattern>(lhs);
			m_rhs = new List<MorphologicalOutput>(rhs);
			m_modifyFromCtxts = new Dictionary<int, SimpleContext>();

			// reduplication flags for each morphological output record
			CheckReduplication(redupMorphType);

			m_rhsTemp = new PhoneticPattern();
			m_modifyFromCtxts = new Dictionary<int, SimpleContext>();
			m_rhsTemp.Add(new MarginContext(Direction.LEFT));
			for (int i = 0; i < m_rhs.Count; i++)
				m_rhs[i].GenerateRHSTemplate(m_rhsTemp, m_lhs, m_modifyFromCtxts);
			m_rhsTemp.Add(new MarginContext(Direction.RIGHT));
		}

		/// <summary>
		/// Gets the RHS template.
		/// </summary>
		/// <value>The RHS template.</value>
		public PhoneticPattern RHSTemplate
		{
			get
			{
				return m_rhsTemp;
			}
		}

		/// <summary>
		/// Gets the partition count.
		/// </summary>
		/// <value>The partition count.</value>
		public int PartitionCount
		{
			get
			{
				return m_lhs.Count;
			}
		}

		/// <summary>
		/// Gets the RHS.
		/// </summary>
		/// <value>The RHS.</value>
		public IEnumerable<MorphologicalOutput> RHS
		{
			get
			{
				return m_rhs;
			}
		}

		/// <summary>
		/// Determines which morphological output records result in a reduplicated affix. It
		/// makes educated guesses.
		/// </summary>
		/// <returns></returns>
		void CheckReduplication(RedupMorphType redupMorphType)
		{
			Dictionary<int, List<int>> partMembers = new Dictionary<int, List<int>>();
			// collect all of the output records for each partition
			for (int i = 0; i < m_rhs.Count; i++)
			{
				if (m_rhs[i].Partition > -1)
				{
					List<int> members;
					if (!partMembers.TryGetValue(m_rhs[i].Partition, out members))
					{
						members = new List<int>();
						partMembers[m_rhs[i].Partition] = members;
					}
					members.Add(i);
				}
			}
			// remove all partitions that have only one output record, so all that is
			// left are the reduplicated partitions
			List<int> toRemove = new List<int>();
			foreach (KeyValuePair<int, List<int>> kvp in partMembers)
			{
				if (kvp.Value.Count == 1)
					toRemove.Add(kvp.Key);
			}
			foreach (int member in toRemove)
				partMembers.Remove(member);

			if (partMembers.Count > 0)
			{
				int start = -1;
				// we have some reduplicated partitions
				switch (redupMorphType)
				{
					case RedupMorphType.PREFIX:
						int prefixPartition = m_lhs.Count - 1;
						for (int i = m_rhs.Count - 1; i >= 0; i--)
						{
							if (m_rhs[i].Partition == prefixPartition || m_rhs[i].Partition == m_lhs.Count - 1)
							{
								if (m_rhs[i].Partition == 0)
								{
									start = i;
									break;
								}
								prefixPartition = m_rhs[i].Partition - 1;
							}
							else
							{
								prefixPartition = m_lhs.Count - 1;
							}
						}
						break;

					case RedupMorphType.SUFFIX:
					case RedupMorphType.IMPLICIT:
						int suffixPartition = 0;
						// look for the full word in the RHS by looking for all of the LHS
						// partitions in order
						for (int i = 0; i < m_rhs.Count; i++)
						{
							if (m_rhs[i].Partition == suffixPartition || m_rhs[i].Partition == 0)
							{
								if (m_rhs[i].Partition == m_lhs.Count - 1)
								{
									start = i - (m_lhs.Count - 1);
									break;
								}
								suffixPartition = m_rhs[i].Partition + 1;
							}
							else
							{
								suffixPartition = 0;
							}
						}
						break;
				}

				// now mark the RHS output records that are not part of the full word
				// as the output records that generate the reduplicated segments
				foreach (List<int> members in partMembers.Values)
				{
					for (int j = 0; j < members.Count; j++)
					{
						int index = members[j];
						if (start == -1)
							m_rhs[index].IsReduplication = j != members.Count - 1;
						else
							m_rhs[index].IsReduplication = index < start || index >= start + m_lhs.Count;
					}
				}
			}
		}

		/// <summary>
		/// Unapplies this transform to the specified partition in the specified match.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="partition">The partition.</param>
		/// <param name="output">The output.</param>
		public void Unapply(Match match, int partition, PhoneticShape output)
		{
			IList<PhoneticShapeNode> nodes = match.GetPartition(partition);
			if (nodes != null && nodes.Count > 0)
			{
				SimpleContext ctxt;
				if (!m_modifyFromCtxts.TryGetValue(partition, out ctxt))
					ctxt = null;

				foreach (PhoneticShapeNode node in nodes)
				{
					switch (node.Type)
					{
						case PhoneticShapeNode.NodeType.SEGMENT:
							Segment newSeg = new Segment(node as Segment);
							// if there is a modify-from context on this partition, unapply it
							if (ctxt != null)
								ctxt.Unapply(newSeg, match.VariableValues);
							output.Add(newSeg);
							break;

						case PhoneticShapeNode.NodeType.BOUNDARY:
							output.Add(node.Clone());
							break;
					}
				}
			}
			else
			{
				// untruncate a partition
				Untruncate(m_lhs[partition], output, false, match.VariableValues);
			}
		}

		void Untruncate(PhoneticPattern lhs, PhoneticShape output, bool optional, VariableValues instantiatedVars)
		{
			// create segments from the LHS partition pattern and append them to the output
			foreach (PhoneticPatternNode node in lhs)
			{
				switch (node.Type)
				{
					case PhoneticPatternNode.NodeType.SIMP_CTXT:
						SimpleContext ctxt = node as SimpleContext;
						Segment newSeg = ctxt.UnapplyDeletion(instantiatedVars);
						newSeg.IsOptional = optional;
						output.Add(newSeg);
						break;

					case PhoneticPatternNode.NodeType.PATTERN:
						NestedPhoneticPattern nestedPattern = node as NestedPhoneticPattern;
						// untruncate nested partitions the maximum number of times it can occur,
						// marking any segments that occur after the minimum number of occurrences
						// as optional
						for (int j = 0; j < nestedPattern.MaxOccur; j++)
							Untruncate(nestedPattern.Pattern, output, j >= nestedPattern.MinOccur, instantiatedVars);
						break;

					case PhoneticPatternNode.NodeType.BDRY_CTXT:
						// skip boundaries
						break;
				}
			}
		}
	}
}
