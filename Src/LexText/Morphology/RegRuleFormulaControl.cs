using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a regular rule formula control. A regular rule formula
	/// is represented by four editable cells: LHS, RHS, left context, and right context.
	/// The LHS cell consists of simple contexts from the <c>StrucDesc</c> field in <c>PhSegmentRule</c>.
	/// The RHS cell consists of simple contexts from the <c>StruChange</c> field in <c>PhSegRuleRHS</c>.
	/// The left context cell consists of a phonological context from the <c>LeftContext</c> field of
	/// <c>PhSegRuleRHS</c>. The right context cell consists of a phonological context from the <c>RightContext</c>
	/// field of <c>PhSegRuleRHS</c>.
	/// </summary>
	public class RegRuleFormulaControl : RuleFormulaControl
	{
		public RegRuleFormulaControl(XmlNode configurationNode)
			: base(configurationNode)
		{
			m_menuId = "mnuPhRegularRule";
		}

		IPhSegRuleRHS RHS
		{
			get
			{
				return m_obj as IPhSegRuleRHS;
			}
		}

		public bool CanModifyContextOccurrence
		{
			get
			{
				CheckDisposed();
				SelectionHelper sel = SelectionHelper.Create(m_view);
				int cellId = GetCell(sel);
				if (cellId == PhSegmentRuleTags.kflidStrucDesc
					|| cellId == PhSegRuleRHSTags.kflidStrucChange)
					return false;

				var obj = GetItem(sel, SelectionHelper.SelLimitType.Anchor);
				var endObj = GetItem(sel, SelectionHelper.SelLimitType.End);
				if (obj != endObj || obj == null || endObj == null)
					return false;

				return obj.ClassID != PhSimpleContextBdryTags.kClassId;
			}
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);

			m_view.Init(mediator, obj, this, new RegRuleFormulaVc(cache, mediator), RegRuleFormulaVc.kfragRHS);

			m_insertionControl.Initialize(cache, mediator, persistProvider, RHS.OwningRule.Name.BestAnalysisAlternative.Text);
			m_insertionControl.AddOption(RuleInsertType.PHONEME, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.NATURAL_CLASS, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.FEATURES, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.WORD_BOUNDARY, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.MORPHEME_BOUNDARY, DisplayOption);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		bool DisplayOption(RuleInsertType type)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId < 0)
				return false;

			switch (cellId)
			{
				case PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOA == null)
						return true;

					IPhPhonContext[] leftCtxts = null;
					IPhPhonContext first = null;
					if (RHS.LeftContextOA.ClassID != PhSequenceContextTags.kClassId)
					{
						leftCtxts = new IPhPhonContext[] { RHS.LeftContextOA };
						first = RHS.LeftContextOA;
					}
					else
					{
						var seqCtxt = RHS.LeftContextOA as IPhSequenceContext;
						if (seqCtxt.MembersRS.Count > 0)
							first = seqCtxt.MembersRS[0];
						leftCtxts = seqCtxt.MembersRS.ToArray();
					}

					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
							return GetIndicesToRemove(leftCtxts, sel)[0] == 0;
						else
							return GetInsertionIndex(leftCtxts, sel) == 0 && !IsWordBoundary(first);
					}
					else
					{
						// we cannot insert anything to the left of a word boundary in the left context
						return sel.IsRange || GetInsertionIndex(leftCtxts, sel) != 0 || !IsWordBoundary(first);
					}

				case PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOA == null || sel.IsRange)
						return true;

					IPhPhonContext[] rightCtxts = null;
					IPhPhonContext last = null;
					if (RHS.RightContextOA.ClassID != PhSequenceContextTags.kClassId)
					{
						rightCtxts = new IPhPhonContext[] { RHS.RightContextOA };
						last = RHS.RightContextOA;
					}
					else
					{
						IPhSequenceContext seqCtxt = RHS.RightContextOA as IPhSequenceContext;
						if (seqCtxt.MembersRS.Count > 0)
							last = seqCtxt.MembersRS[seqCtxt.MembersRS.Count - 1];
						rightCtxts = seqCtxt.MembersRS.ToArray();
					}

					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							int[] indices = GetIndicesToRemove(rightCtxts, sel);
							return indices[indices.Length - 1] == rightCtxts.Length - 1;
						}
						else
						{
							return GetInsertionIndex(rightCtxts, sel) == rightCtxts.Length && !IsWordBoundary(last);
						}
					}
					else
					{
						// we cannot insert anything to the right of a word boundary in the right context
						return sel.IsRange || GetInsertionIndex(rightCtxts, sel) != rightCtxts.Length || !IsWordBoundary(last);
					}

				default:
					return type != RuleInsertType.WORD_BOUNDARY;
			}
		}

		string DisplayNoOptsMsg()
		{
			return MEStrings.ksRuleWordBdryNoOptsMsg;
		}

		protected override int UpdateEnvironment(IPhEnvironment env)
		{
			string envStr = env.StringRepresentation.Text.Trim().Substring(1).Trim();
			int index = envStr.IndexOf('_');
			string leftEnv = envStr.Substring(0, index).Trim();
			string rightEnv = envStr.Substring(index + 1).Trim();

			if (RHS.LeftContextOA != null)
			{
				RHS.LeftContextOA.PreRemovalSideEffects();
				RHS.LeftContextOA = null;
			}
			InsertContextsFromEnv(leftEnv, PhSegRuleRHSTags.kflidLeftContext, null);

			if (RHS.RightContextOA != null)
			{
				RHS.RightContextOA.PreRemovalSideEffects();
				RHS.RightContextOA = null;
			}
			InsertContextsFromEnv(rightEnv, PhSegRuleRHSTags.kflidRightContext, null);

			return PhSegRuleRHSTags.kflidLeftContext;
		}

		/// <summary>
		/// Parses the string representation of the specified environment and creates contexts
		/// based off of the environment. This is called recursively.
		/// </summary>
		/// <param name="envStr">The environment string.</param>
		/// <param name="leftEnv">if <c>true</c> insert in the left context, otherwise the right context.</param>
		/// <param name="iterCtxt">The iteration context to insert into.</param>
		void InsertContextsFromEnv(string envStr, int flid, IPhIterationContext iterCtxt)
		{
			int i = 0;
			while (i < envStr.Length)
			{
				switch (envStr[i])
				{
					case '#':
						var bdryCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
						AppendToEnv(bdryCtxt, flid);
						bdryCtxt.FeatureStructureRA = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleWordBdry);
						i++;
						break;

					case '[':
						int closeBracket = envStr.IndexOf(']', i + 1);
						string ncAbbr = envStr.Substring(i + 1, closeBracket - (i + 1));
						int redupIndex = ncAbbr.IndexOf('^');
						if (redupIndex != -1)
							ncAbbr = ncAbbr.Substring(0, redupIndex);
						foreach (IPhNaturalClass nc in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS)
						{
							if (nc.Abbreviation.BestAnalysisAlternative.Text == ncAbbr)
							{
								var ncCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
								if (iterCtxt != null)
								{
									m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ncCtxt);
									iterCtxt.MemberRA = ncCtxt;
								}
								else
								{
									AppendToEnv(ncCtxt, flid);
								}
								ncCtxt.FeatureStructureRA = nc;
								break;
							}
						}
						i = closeBracket + 1;
						break;

					case '(':
						int closeParen = envStr.IndexOf(')', i + 1);
						string str = envStr.Substring(i + 1, closeParen - (i + 1));
						var newIterCtxt = m_cache.ServiceLocator.GetInstance<IPhIterationContextFactory>().Create();
						AppendToEnv(newIterCtxt, flid);
						newIterCtxt.Minimum = 0;
						newIterCtxt.Maximum = 1;
						InsertContextsFromEnv(str, flid, newIterCtxt);
						i = closeParen + 1;
						break;

					case ' ':
						i++;
						break;

					default:
						int nextIndex = envStr.IndexOfAny(new char[] { '[', ' ', '#', '(' }, i + 1);
						if (nextIndex == -1)
							nextIndex = envStr.Length;
						int len = nextIndex - i;
						while (len > 0)
						{
							string phonemeStr = envStr.Substring(i, len);
							foreach (IPhPhoneme phoneme in m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC)
							{
								foreach (IPhCode code in phoneme.CodesOS)
								{
									if (code.Representation.BestVernacularAlternative.Text == phonemeStr)
									{
										var segCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
										if (iterCtxt != null)
										{
											m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(segCtxt);
											iterCtxt.MemberRA = segCtxt;
										}
										else
										{
											AppendToEnv(segCtxt, flid);
										}
										segCtxt.FeatureStructureRA = phoneme;
										goto Found;
									}
								}
							}
							len--;
						}
				Found:

						if (len == 0)
							i++;
						else
							i += len;
						break;
				}
			}
		}

		void AppendToEnv(IPhPhonContext ctxt, int flid)
		{
			IPhSequenceContext seqCtxt = null;

			switch (flid)
			{
				case PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOA == null)
						RHS.LeftContextOA = ctxt;
					else
						seqCtxt = CreateSeqCtxt(flid);
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOA == null)
						RHS.RightContextOA = ctxt;
					else
						seqCtxt = CreateSeqCtxt(flid);
					break;
			}

			if (seqCtxt != null)
			{
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
				seqCtxt.MembersRS.Add(ctxt);
			}
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return -1;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (IsCellFlid(level.tag))
					return level.tag;
			}

			if (IsCellFlid(sel.GetTextPropId(limit)))
				return sel.GetTextPropId(limit);
			return -1;
		}

		bool IsCellFlid(int flid)
		{
			return flid == PhSegmentRuleTags.kflidStrucDesc
				|| flid == PhSegRuleRHSTags.kflidStrucChange
				|| flid == PhSegRuleRHSTags.kflidLeftContext
				|| flid == PhSegRuleRHSTags.kflidRightContext;
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					return PhSegRuleRHSTags.kflidStrucChange;
				case PhSegRuleRHSTags.kflidStrucChange:
					return PhSegRuleRHSTags.kflidLeftContext;
				case PhSegRuleRHSTags.kflidLeftContext:
					return PhSegRuleRHSTags.kflidRightContext;
				case PhSegRuleRHSTags.kflidRightContext:
					return -1;
			}
			return -1;
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					return -1;
				case PhSegRuleRHSTags.kflidStrucChange:
					return PhSegmentRuleTags.kflidStrucDesc;
				case PhSegRuleRHSTags.kflidLeftContext:
					return PhSegRuleRHSTags.kflidStrucChange;
				case PhSegRuleRHSTags.kflidRightContext:
					return PhSegRuleRHSTags.kflidLeftContext;
			}
			return -1;
		}

		protected override ICmObject GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return null;

			int cellId = GetCell(sel);
			if (cellId < 0)
				return null;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (IsCellFlid(level.tag) || level.tag == PhSequenceContextTags.kflidMembers)
					return m_cache.ServiceLocator.GetObject(level.hvo);
			}

			return null;
		}

		protected override int GetItemCellIndex(int cellId, ICmObject obj)
		{
			int index = -1;
			if (cellId == PhSegmentRuleTags.kflidStrucDesc || cellId == PhSegRuleRHSTags.kflidStrucChange)
			{
				index = obj.IndexInOwner;
			}
			else
			{
				bool leftEnv = cellId == PhSegRuleRHSTags.kflidLeftContext;
				var ctxt = leftEnv ? RHS.LeftContextOA : RHS.RightContextOA;

				if (ctxt.ClassID == PhSequenceContextTags.kClassId)
				{
					var seqCtxt = ctxt as IPhSequenceContext;
					index = seqCtxt.MembersRS.IndexOf(obj as IPhPhonContext);
				}
				else
				{
					index = 0;
				}
			}
			return index;
		}

		protected override SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			SelLevInfo[] levels = null;
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					if (cellIndex < 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = m_cache.MetaDataCacheAccessor.GetFieldId2(PhSegRuleRHSTags.kClassId, "OwningRule", false);
					}
					else
					{
						levels = new SelLevInfo[2];
						levels[0].tag = PhSegmentRuleTags.kflidStrucDesc;
						levels[0].ihvo = cellIndex;
						levels[1].tag = m_cache.MetaDataCacheAccessor.GetFieldId2(PhSegRuleRHSTags.kClassId, "OwningRule", false);
					}
					break;

				case PhSegRuleRHSTags.kflidStrucChange:
					if (cellIndex >= 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = PhSegRuleRHSTags.kflidStrucChange;
						levels[0].ihvo = cellIndex;
					}
					break;

				case PhSegRuleRHSTags.kflidLeftContext:
				case PhSegRuleRHSTags.kflidRightContext:
					bool leftEnv = cellId == PhSegRuleRHSTags.kflidLeftContext;
					var ctxt = leftEnv ? RHS.LeftContextOA : RHS.RightContextOA;
					if (ctxt != null)
					{
						switch (ctxt.ClassID)
						{
							case PhSequenceContextTags.kClassId:
								if (cellIndex < 0)
								{
									levels = new SelLevInfo[1];
									levels[0].tag = cellId;
								}
								else
								{
									levels = new SelLevInfo[2];
									levels[0].tag = PhSequenceContextTags.kflidMembers;
									levels[0].ihvo = cellIndex;
									levels[1].tag = cellId;
								}
								break;

							default:
								if (cellIndex >= 0)
								{
									levels = new SelLevInfo[1];
									levels[0].tag = cellId;
								}
								break;
						}
					}
					break;
			}

			return levels;
		}

		protected override int GetCellCount(int cellId)
		{
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					return RHS.OwningRule.StrucDescOS.Count;

				case PhSegRuleRHSTags.kflidStrucChange:
					return RHS.StrucChangeOS.Count;

				case PhSegRuleRHSTags.kflidLeftContext:
				case PhSegRuleRHSTags.kflidRightContext:
					bool leftEnv = cellId == PhSegRuleRHSTags.kflidLeftContext;
					var ctxt = leftEnv ? RHS.LeftContextOA : RHS.RightContextOA;
					if (ctxt == null)
						return 0;
					else if (ctxt.ClassID != PhSequenceContextTags.kClassId)
						return 1;
					else
						return (ctxt as IPhSequenceContext).MembersRS.Count;
			}
			return 0;
		}

		protected override int GetFlid(int cellId)
		{
			return cellId;
		}

		protected override int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			var segCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			var cellId = InsertContext(segCtxt, sel, out cellIndex);
			segCtxt.FeatureStructureRA = phoneme;
			return cellId;
		}

		protected override int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			var bdryCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
			var cellId = InsertContext(bdryCtxt, sel, out cellIndex);
			bdryCtxt.FeatureStructureRA = bdry;
			return cellId;
		}

		protected override int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex)
		{
			var ncCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			var cellId = InsertContext(ncCtxt, sel, out cellIndex);
			ncCtxt.FeatureStructureRA = nc;
			return cellId;
		}

		int InsertContext(IPhSimpleContext ctxt, SelectionHelper sel, out int cellIndex)
		{
			cellIndex = -1;
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					cellIndex = InsertContextInto(ctxt, sel, RHS.OwningRule.StrucDescOS);
					break;

				case PhSegRuleRHSTags.kflidStrucChange:
					cellIndex = InsertContextInto(ctxt, sel, RHS.StrucChangeOS);
					break;

				case PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOA == null)
						RHS.LeftContextOA = ctxt;
					else
						cellIndex = InsertContextInto(ctxt, sel, CreateSeqCtxt(cellId));
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOA == null)
						RHS.RightContextOA = ctxt;
					else
						cellIndex = InsertContextInto(ctxt, sel, CreateSeqCtxt(cellId));
					break;
			}
			return cellId;
		}

		IPhSequenceContext CreateSeqCtxt(int flid)
		{
			bool leftEnv = flid == PhSegRuleRHSTags.kflidLeftContext;
			var ctxt = leftEnv ? RHS.LeftContextOA : RHS.RightContextOA;
			if (ctxt == null)
				return null;

			IPhSequenceContext seqCtxt = null;
			if (ctxt.ClassID != PhSequenceContextTags.kClassId)
			{
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
				seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
				if (leftEnv)
					RHS.LeftContextOA = seqCtxt;
				else
					RHS.RightContextOA = seqCtxt;
				seqCtxt.MembersRS.Add(ctxt);
			}
			else
			{
				seqCtxt = ctxt as IPhSequenceContext;
			}
			return seqCtxt;
		}

		protected override int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			cellIndex = -1;

			int cellId = GetCell(sel);
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					if (!RemoveContextsFrom(forward, sel, RHS.OwningRule.StrucDescOS, true, out cellIndex))
						cellId = -1;
					break;

				case PhSegRuleRHSTags.kflidStrucChange:
					if (RHS.StrucChangeOS == null)
					{
						cellId = -1;
						break;
					}
					if (!RemoveContextsFrom(forward, sel, RHS.StrucChangeOS, true, out cellIndex))
						cellId = -1;
					break;

				case PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (RHS.LeftContextOA.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = RHS.LeftContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new IPhPhonContext[] { RHS.LeftContextOA }, sel, forward);
						if (idx > -1)
						{
							RHS.LeftContextOA.PreRemovalSideEffects();
							RHS.LeftContextOA = null;
						}
						else
						{
							cellId = -1;
						}
					}
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (RHS.RightContextOA.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = RHS.RightContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new IPhPhonContext[] { RHS.RightContextOA }, sel, forward);
						if (idx > -1)
						{
							RHS.RightContextOA.PreRemovalSideEffects();
							RHS.RightContextOA = null;
						}
						else
						{
							cellId = -1;
						}
					}
					break;
			}

			return cellId;
		}

		/// <summary>
		/// Sets the number of occurrences of a context.
		/// </summary>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		public void SetContextOccurrence(int min, int max)
		{
			CheckDisposed();
			var sel = SelectionHelper.Create(m_view);
			var cellId = GetCell(sel);
			var obj = GetItem(sel, SelectionHelper.SelLimitType.Anchor);
			var ctxt = obj as IPhPhonContext;
			int index = -1;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksRegRuleUndoSetOccurrence, MEStrings.ksRegRuleRedoSetOccurrence, ctxt, () =>
			{
				if (ctxt.ClassID == PhIterationContextTags.kClassId)
				{
					// if there is an existing iteration context, just update it or remove it if it can occur only once
					var iterCtxt = ctxt as IPhIterationContext;
					if (min == 1 && max == 1)
					{
						index = OverwriteContext(iterCtxt.MemberRA, iterCtxt, cellId == PhSegRuleRHSTags.kflidLeftContext, false);
					}
					else
					{
						iterCtxt.Minimum = min;
						iterCtxt.Maximum = max;
					}
				}
				else if (min != 1 || max != 1)
				{
					// create a new iteration context
					var iterCtxt = m_cache.ServiceLocator.GetInstance<IPhIterationContextFactory>().Create();
					index = OverwriteContext(iterCtxt, ctxt, cellId == PhSegRuleRHSTags.kflidLeftContext, true);
					iterCtxt.MemberRA = ctxt;
					iterCtxt.Minimum = min;
					iterCtxt.Maximum = max;
				}
			});

			if (index == -1)
			{
				var envCtxt = cellId == PhSegRuleRHSTags.kflidLeftContext ? RHS.LeftContextOA : RHS.RightContextOA;
				IPhSequenceContext seqCtxt;
				index = GetIndex(ctxt, envCtxt, out seqCtxt);
			}

			ReconstructView(cellId, index, true);
		}

		int OverwriteContext(IPhPhonContext src, IPhPhonContext dest, bool leftEnv, bool preserveDest)
		{
			IPhSequenceContext seqCtxt;
			int index = GetIndex(dest, leftEnv ? RHS.LeftContextOA : RHS.RightContextOA, out seqCtxt);
			if (index != -1)
			{
				if (!src.IsValidObject)
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(src);

				seqCtxt.MembersRS.Insert(index, src);
				seqCtxt.MembersRS.Remove(dest);
				if (!preserveDest)
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(dest);
			}
			else
			{
				if (leftEnv)
				{
					if (preserveDest)
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(RHS.LeftContextOA);
					RHS.LeftContextOA = src;
				}
				else
				{
					if (preserveDest)
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(RHS.RightContextOA);
					RHS.RightContextOA = src;
				}
			}
			return index;
		}

		int GetIndex(IPhPhonContext ctxt, IPhPhonContext envCtxt, out IPhSequenceContext seqCtxt)
		{
			if (envCtxt.ClassID == PhSequenceContextTags.kClassId)
			{
				seqCtxt = envCtxt as IPhSequenceContext;
				return seqCtxt.MembersRS.IndexOf(ctxt);
			}

			seqCtxt = null;
			return -1;
		}

		/// <summary>
		/// Gets the number of occurrences of the currently selected context.
		/// </summary>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		public void GetContextOccurrence(out int min, out int max)
		{
			CheckDisposed();
			var sel = SelectionHelper.Create(m_view);
			var obj = GetItem(sel, SelectionHelper.SelLimitType.Anchor);
			if (obj.ClassID == PhIterationContextTags.kClassId)
			{
				var iterCtxt = obj as IPhIterationContext;
				min = iterCtxt.Minimum;
				max = iterCtxt.Maximum;
			}
			else
			{
				min = 1;
				max = 1;
			}
		}

		/// <summary>
		/// Sets the alpha variables on the currently selected natural class simple context.
		/// </summary>
		public void SetContextVariables()
		{
			var sel = SelectionHelper.Create(m_view);
			bool reconstruct = false;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksRegRuleUndoSetVariables, MEStrings.ksRegRuleRedoSetVariables, RHS, () =>
			{
				using (var featChooser = new FeatureConstraintChooserDlg())
				{
					var ctxt = CurrentContext as IPhSimpleContextNC;
					featChooser.SetDlgInfo(m_cache, Mediator, RHS.OwningRule, ctxt);
					DialogResult res = featChooser.ShowDialog();
					if (res != DialogResult.Cancel)
						featChooser.HandleJump();
					reconstruct = res == DialogResult.OK;
				}
			});

			m_view.Select();
			if (reconstruct)
			{
				m_view.RootBox.Reconstruct();
				sel.RestoreSelectionAndScrollPos();
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_panel != null && m_view != null)
			{
				// make room for the environment button launcher
				int w = Width - m_panel.Width;
				m_view.Width = w > 0 ? w : 0;
			}
		}
	}

	class RegRuleFormulaVc : RuleFormulaVc
	{
		public const int kfragRHS = 100;
		public const int kfragRule = 101;

		ITsTextProps m_ctxtProps;
		ITsTextProps m_charProps;

		ITsString m_arrow;
		ITsString m_slash;
		ITsString m_underscore;

		IPhSegRuleRHS m_rhs = null;

		public RegRuleFormulaVc(FdoCache cache, XCore.Mediator mediator)
			: base(cache, mediator)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_ctxtProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 20000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_charProps = tpb.GetTextProps();

			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			m_arrow = tsf.MakeString("\u2192", userWs);
			m_slash = tsf.MakeString("/", userWs);
			m_underscore = tsf.MakeString("__", userWs);
		}

		protected override int MaxNumLines
		{
			get
			{
				int numLines = GetNumLines(m_rhs.OwningRule.StrucDescOS);
				int maxNumLines = numLines;
				numLines = GetNumLines(m_rhs.StrucChangeOS);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
				numLines = GetNumLines(m_rhs.LeftContextOA);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
				numLines = GetNumLines(m_rhs.RightContextOA);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
				return maxNumLines;
			}
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			int i = 0;
			foreach (var curVar in m_rhs.OwningRule.FeatureConstraints)
			{
				if (var == curVar)
					return i;
				i++;
			}
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRHS:
					m_rhs = m_cache.ServiceLocator.GetInstance<IPhSegRuleRHSRepository>().GetObject(hvo);
					var rule = m_rhs.OwningRule;
					if (rule.Disabled)
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, "Disabled Text");
					}

					int arrowWidth, slashWidth, underscoreWidth, charHeight;
					vwenv.get_StringWidth(m_arrow, m_charProps, out arrowWidth, out charHeight);
					int maxCharHeight = charHeight;
					vwenv.get_StringWidth(m_slash, m_charProps, out slashWidth, out charHeight);
					maxCharHeight = Math.Max(charHeight, maxCharHeight);
					vwenv.get_StringWidth(m_underscore, m_charProps, out underscoreWidth, out charHeight);
					maxCharHeight = Math.Max(charHeight, maxCharHeight);

					int dmpx, spaceHeight;
					vwenv.get_StringWidth(m_zwSpace, m_bracketProps, out dmpx, out spaceHeight);

					int maxNumLines = MaxNumLines;
					int maxCtxtHeight = maxNumLines * spaceHeight;

					int maxHeight = Math.Max(maxCharHeight, maxCtxtHeight);
					int charOffset = maxHeight;
					int ctxtPadding = maxHeight - maxCtxtHeight;

					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(7, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);

					VwLength ctxtLen;
					ctxtLen.nVal = 1;
					ctxtLen.unit = VwUnit.kunRelative;
					VwLength charLen;
					charLen.unit = VwUnit.kunPoint1000;
					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = arrowWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = slashWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = underscoreWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// LHS cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					vwenv.AddObjProp(m_cache.MetaDataCacheAccessor.GetFieldId2(PhSegRuleRHSTags.kClassId, "OwningRule", false), this, kfragRule);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// arrow cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_arrow);
					vwenv.CloseTableCell();

					// RHS cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.StrucChangeOS.Count > 0)
					{
						vwenv.AddObjVecItems(PhSegRuleRHSTags.kflidStrucChange, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] {hvo}, new[] {PhSegRuleRHSTags.kflidStrucChange}, 1);
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidStrucChange, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// slash cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_slash);
					vwenv.CloseTableCell();

					// left context cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.LeftContextOA != null)
					{
						vwenv.AddObjProp(PhSegRuleRHSTags.kflidLeftContext, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] { hvo }, new[] { PhSegRuleRHSTags.kflidLeftContext }, 1);
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidLeftContext, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// underscore cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_underscore);
					vwenv.CloseTableCell();

					// right context cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.RightContextOA != null)
					{
						vwenv.AddObjProp(PhSegRuleRHSTags.kflidRightContext, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] { hvo }, new[] { PhSegRuleRHSTags.kflidRightContext }, 1);
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidRightContext, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragRule:
					if (m_rhs.OwningRule.StrucDescOS.Count > 0)
					{
						vwenv.AddObjVecItems(PhSegmentRuleTags.kflidStrucDesc, this, kfragContext);
					}
					else
					{
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegmentRuleTags.kflidStrucDesc, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					break;

				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}
