// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// A concrete rule formula view constructor that draws a single context, so the shared
	/// context-drawing, line-counting, and cell-sizing behavior can be exercised on its own.
	/// </summary>
	internal sealed class TestRuleFormulaVc : RuleFormulaVcBase
	{
		internal TestRuleFormulaVc(LcmCache cache, PropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			MaxNumLines = 1;
		}

		/// <summary>
		/// The line count the drawing code pads single-line piles out to.
		/// </summary>
		internal int MaxNumLines { get; set; }

		/// <summary>
		/// The number of lines the given context occupies.
		/// </summary>
		internal int NumLinesFor(IPhContextOrVar ctxtOrVar)
		{
			return GetNumLines(ctxtOrVar);
		}

		/// <summary>
		/// The cell width the given context is sized to, in the units this environment's
		/// string measurement reports.
		/// </summary>
		internal int WidthOf(IPhContextOrVar ctxtOrVar, IVwEnv vwenv)
		{
			return GetWidth(ctxtOrVar, vwenv);
		}

		/// <summary>
		/// The margin a context's cell carries either side of its drawing.
		/// </summary>
		internal int ContextMargins
		{
			get { return PileMargin * 2; }
		}

		protected override int GetMaxNumLines()
		{
			return MaxNumLines;
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			return -1;
		}
	}

	/// <summary>
	/// A collector environment that captures both the text a view constructor draws and the
	/// display dependencies it registers. String measurement reports one unit per character, so
	/// a drawn string and the width it is measured at are directly comparable.
	/// </summary>
	internal sealed class RecordingCollectorEnv : StringCollectorEnv
	{
		private const char ZeroWidthSpace = '\u200b';

		private readonly List<DependencyCall> m_dependencies = new List<DependencyCall>();

		internal RecordingCollectorEnv(ISilDataAccess sda, int hvoRoot)
			: base(null, sda, hvoRoot)
		{
		}

		/// <summary>
		/// The drawn text with the zero-width boundary markers of each pile removed.
		/// </summary>
		internal string Text
		{
			get { return Result.Replace(ZeroWidthSpace.ToString(), string.Empty); }
		}

		/// <summary>
		/// Every dependency registration, in the order it was made.
		/// </summary>
		internal IReadOnlyList<DependencyCall> Dependencies
		{
			get { return m_dependencies; }
		}

		public override void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
		{
			m_dependencies.Add(new DependencyCall(rghvo, rgtag, chvo));
			base.NoteDependency(rghvo, rgtag, chvo);
		}

		/// <summary>
		/// Reports whether any registration names the given object and property within
		/// the count it declared.
		/// </summary>
		internal bool DependsOn(int hvo, int tag)
		{
			return m_dependencies.Any(call => call.Covers(hvo, tag));
		}

		/// <summary>
		/// One call registering a batch of object and property pairs the display depends on.
		/// </summary>
		internal sealed class DependencyCall
		{
			/// <summary>
			/// Initializes a record of one registration, rejecting a declared count that runs
			/// past either array so that a registration whose count and pairs disagree fails
			/// the test that provoked it.
			/// </summary>
			/// <exception cref="ArgumentException">
			/// The count is negative, or names more pairs than were supplied.
			/// </exception>
			internal DependencyCall(int[] hvos, int[] tags, int count)
			{
				if (count < 0 || count > hvos.Length || count > tags.Length)
				{
					throw new ArgumentException(string.Format(
						"A dependency registration declared {0} pairs but supplied {1} objects"
						+ " and {2} properties.", count, hvos.Length, tags.Length));
				}
				Hvos = hvos;
				Tags = tags;
				Count = count;
			}

			/// <summary>The objects named, paired positionally with the properties.</summary>
			internal int[] Hvos { get; }

			/// <summary>The properties named, paired positionally with the objects.</summary>
			internal int[] Tags { get; }

			/// <summary>How many pairs the caller declared.</summary>
			internal int Count { get; }

			internal bool Covers(int hvo, int tag)
			{
				for (int i = 0; i < Count; i++)
				{
					if (Hvos[i] == hvo && Tags[i] == tag)
						return true;
				}
				return false;
			}
		}
	}
}
