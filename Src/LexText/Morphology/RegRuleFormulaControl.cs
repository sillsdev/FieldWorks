using System;
using System.Drawing;
using System.Xml;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

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
		}

		/// <summary>
		/// the right hand side
		/// </summary>
		public IPhSegRuleRHS Rhs
		{
			get
			{
				return (IPhSegRuleRHS) m_obj;
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
			Mediator mediator, PropertyTable propertyTable, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, propertyTable, displayNameProperty, displayWs);

			m_view.Init(mediator, propertyTable, obj, this, new RegRuleFormulaVc(mediator, propertyTable), RegRuleFormulaVc.kfragRHS);

			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Phoneme), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.NaturalClass), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Features), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.WordBoundary), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.MorphemeBoundary), DisplayOption);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		protected override string FeatureChooserHelpTopic
		{
			get { return "khtpChoose-Grammar-PhonFeats-RegRuleFormulaControl"; }
		}

		protected override string RuleName
		{
			get { return Rhs.OwningRule.Name.BestAnalysisAlternative.Text; }
		}

		protected override string ContextMenuID
		{
			get { return "mnuPhRegularRule"; }
		}

		private bool DisplayOption(object option)
		{
			var opt = (InsertOption) option;
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId < 0)
				return false;

			switch (cellId)
			{
				case PhSegRuleRHSTags.kflidLeftContext:
					if (Rhs.LeftContextOA == null)
						return true;

					ICmObject[] leftCtxts;
					IPhPhonContext first = null;
					if (Rhs.LeftContextOA.ClassID != PhSequenceContextTags.kClassId)
					{
						leftCtxts = new ICmObject[] { Rhs.LeftContextOA };
						first = Rhs.LeftContextOA;
					}
					else
					{
						var seqCtxt = (IPhSequenceContext) Rhs.LeftContextOA;
						if (seqCtxt.MembersRS.Count > 0)
							first = seqCtxt.MembersRS[0];
						leftCtxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
					}

					if (opt.Type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
							return GetIndicesToRemove(leftCtxts, sel)[0] == 0;
						return GetInsertionIndex(leftCtxts, sel) == 0 && !IsWordBoundary(first);
					}
					// we cannot insert anything to the left of a word boundary in the left context
					return sel.IsRange || GetInsertionIndex(leftCtxts, sel) != 0 || !IsWordBoundary(first);

				case PhSegRuleRHSTags.kflidRightContext:
					if (Rhs.RightContextOA == null || sel.IsRange)
						return true;

					ICmObject[] rightCtxts;
					IPhPhonContext last = null;
					if (Rhs.RightContextOA.ClassID != PhSequenceContextTags.kClassId)
					{
						rightCtxts = new ICmObject[] { Rhs.RightContextOA };
						last = Rhs.RightContextOA;
					}
					else
					{
						var seqCtxt = (IPhSequenceContext) Rhs.RightContextOA;
						if (seqCtxt.MembersRS.Count > 0)
							last = seqCtxt.MembersRS[seqCtxt.MembersRS.Count - 1];
						rightCtxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
					}

					if (opt.Type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							int[] indices = GetIndicesToRemove(rightCtxts, sel);
							return indices[indices.Length - 1] == rightCtxts.Length - 1;
						}
						return GetInsertionIndex(rightCtxts, sel) == rightCtxts.Length && !IsWordBoundary(last);
					}
					// we cannot insert anything to the right of a word boundary in the right context
					return sel.IsRange || GetInsertionIndex(rightCtxts, sel) != rightCtxts.Length || !IsWordBoundary(last);

				default:
					return opt.Type != RuleInsertType.WordBoundary;
			}
		}

		private string DisplayNoOptsMsg()
		{
			return MEStrings.ksRuleWordBdryNoOptsMsg;
		}

		protected override int UpdateEnvironment(IPhEnvironment env)
		{
			string envStr = env.StringRepresentation.Text.Trim().Substring(1).Trim();
			int index = envStr.IndexOf('_');
			string leftEnv = envStr.Substring(0, index).Trim();
			string rightEnv = envStr.Substring(index + 1).Trim();

			if (Rhs.LeftContextOA != null)
			{
				Rhs.LeftContextOA.PreRemovalSideEffects();
				Rhs.LeftContextOA = null;
			}
			InsertContextsFromEnv(leftEnv, PhSegRuleRHSTags.kflidLeftContext, null);

			if (Rhs.RightContextOA != null)
			{
				Rhs.RightContextOA.PreRemovalSideEffects();
				Rhs.RightContextOA = null;
			}
			InsertContextsFromEnv(rightEnv, PhSegRuleRHSTags.kflidRightContext, null);

			return PhSegRuleRHSTags.kflidLeftContext;
		}

		/// <summary>
		/// Parses the string representation of the specified environment and creates contexts
		/// based off of the environment. This is called recursively.
		/// </summary>
		/// <param name="envStr">The environment string.</param>
		/// <param name="flid"></param>
		/// <param name="iterCtxt">The iteration context to insert into.</param>
		private void InsertContextsFromEnv(string envStr, int flid, IPhIterationContext iterCtxt)
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
						int nextIndex = envStr.IndexOfAny(new[] { '[', ' ', '#', '(' }, i + 1);
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
					if (Rhs.LeftContextOA == null)
						Rhs.LeftContextOA = ctxt;
					else
						seqCtxt = CreateSeqCtxt(flid);
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (Rhs.RightContextOA == null)
						Rhs.RightContextOA = ctxt;
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
			int index;
			if (cellId == PhSegmentRuleTags.kflidStrucDesc || cellId == PhSegRuleRHSTags.kflidStrucChange)
			{
				index = obj.IndexInOwner;
			}
			else
			{
				bool leftEnv = cellId == PhSegRuleRHSTags.kflidLeftContext;
				var ctxt = leftEnv ? Rhs.LeftContextOA : Rhs.RightContextOA;

				if (ctxt.ClassID == PhSequenceContextTags.kClassId)
				{
					var seqCtxt = (IPhSequenceContext) ctxt;
					index = seqCtxt.MembersRS.IndexOf((IPhPhonContext) obj);
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
					var ctxt = leftEnv ? Rhs.LeftContextOA : Rhs.RightContextOA;
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
					return Rhs.OwningRule.StrucDescOS.Count;

				case PhSegRuleRHSTags.kflidStrucChange:
					return Rhs.StrucChangeOS.Count;

				case PhSegRuleRHSTags.kflidLeftContext:
				case PhSegRuleRHSTags.kflidRightContext:
					bool leftEnv = cellId == PhSegRuleRHSTags.kflidLeftContext;
					var ctxt = leftEnv ? Rhs.LeftContextOA : Rhs.RightContextOA;
					if (ctxt == null)
						return 0;
					if (ctxt.ClassID != PhSequenceContextTags.kClassId)
						return 1;
					return ((IPhSequenceContext) ctxt).MembersRS.Count;
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

		protected override int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
		{
			ctxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			var cellId = InsertContext(ctxt, sel, out cellIndex);
			ctxt.FeatureStructureRA = nc;
			return cellId;
		}

		int InsertContext(IPhSimpleContext ctxt, SelectionHelper sel, out int cellIndex)
		{
			cellIndex = -1;
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case PhSegmentRuleTags.kflidStrucDesc:
					cellIndex = InsertContextInto(ctxt, sel, Rhs.OwningRule.StrucDescOS);
					break;

				case PhSegRuleRHSTags.kflidStrucChange:
					cellIndex = InsertContextInto(ctxt, sel, Rhs.StrucChangeOS);
					break;

				case PhSegRuleRHSTags.kflidLeftContext:
					if (Rhs.LeftContextOA == null)
						Rhs.LeftContextOA = ctxt;
					else
						cellIndex = InsertContextInto(ctxt, sel, CreateSeqCtxt(cellId));
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (Rhs.RightContextOA == null)
						Rhs.RightContextOA = ctxt;
					else
						cellIndex = InsertContextInto(ctxt, sel, CreateSeqCtxt(cellId));
					break;
			}
			return cellId;
		}

		IPhSequenceContext CreateSeqCtxt(int flid)
		{
			bool leftEnv = flid == PhSegRuleRHSTags.kflidLeftContext;
			var ctxt = leftEnv ? Rhs.LeftContextOA : Rhs.RightContextOA;
			if (ctxt == null)
				return null;

			IPhSequenceContext seqCtxt;
			if (ctxt.ClassID != PhSequenceContextTags.kClassId)
			{
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
				seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
				if (leftEnv)
					Rhs.LeftContextOA = seqCtxt;
				else
					Rhs.RightContextOA = seqCtxt;
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
					if (!RemoveContextsFrom(forward, sel, Rhs.OwningRule.StrucDescOS, true, out cellIndex))
						cellId = -1;
					break;

				case PhSegRuleRHSTags.kflidStrucChange:
					if (Rhs.StrucChangeOS == null)
					{
						cellId = -1;
						break;
					}
					if (!RemoveContextsFrom(forward, sel, Rhs.StrucChangeOS, true, out cellIndex))
						cellId = -1;
					break;

				case PhSegRuleRHSTags.kflidLeftContext:
					if (Rhs.LeftContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (Rhs.LeftContextOA.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = Rhs.LeftContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new ICmObject[] { Rhs.LeftContextOA }, sel, forward);
						if (idx > -1)
						{
							Rhs.LeftContextOA.PreRemovalSideEffects();
							Rhs.LeftContextOA = null;
						}
						else
						{
							cellId = -1;
						}
					}
					break;

				case PhSegRuleRHSTags.kflidRightContext:
					if (Rhs.RightContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (Rhs.RightContextOA.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = Rhs.RightContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new ICmObject[] { Rhs.RightContextOA }, sel, forward);
						if (idx > -1)
						{
							Rhs.RightContextOA.PreRemovalSideEffects();
							Rhs.RightContextOA = null;
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

		protected override void SetupPhonologicalFeatureChoooserDlg(PhonologicalFeatureChooserDlg featChooser)
		{
			featChooser.ShowFeatureConstraintValues = true;
			featChooser.SetDlgInfo(m_cache, m_mediator, m_propertyTable, Rhs.OwningRule);
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
			var ctxt = (IPhPhonContext) obj;
			int index = -1;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksRegRuleUndoSetOccurrence, MEStrings.ksRegRuleRedoSetOccurrence, ctxt, () =>
			{
				if (ctxt.ClassID == PhIterationContextTags.kClassId)
				{
					// if there is an existing iteration context, just update it or remove it if it can occur only once
					var iterCtxt = (IPhIterationContext) ctxt;
					if (min == 1 && max == 1)
					{
						// We want to replace the iteration context with the original (simple?) context which it
						// specifies repeat counts for. That is, we will replace the iterCtxt with its own MemberRA.
						// Then we will delete the iteration context (false argument).
						// We have to do this carefully, however, because when a PhIterationContext is deleted,
						// it also deletes its MemberRA. So if the MemberRA is still linked to the simple context,
						// both get deleted, and the replace unexpectedly fails (LT-13566).
						// So, we must break the link before we do the replacement.
						var temp = iterCtxt.MemberRA;
						iterCtxt.MemberRA = null;
						index = OverwriteContext(temp, iterCtxt, cellId == PhSegRuleRHSTags.kflidLeftContext, false);
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
				var envCtxt = cellId == PhSegRuleRHSTags.kflidLeftContext ? Rhs.LeftContextOA : Rhs.RightContextOA;
				IPhSequenceContext seqCtxt;
				index = GetIndex(ctxt, envCtxt, out seqCtxt);
			}

			ReconstructView(cellId, index, true);
		}

		int OverwriteContext(IPhPhonContext src, IPhPhonContext dest, bool leftEnv, bool preserveDest)
		{
			IPhSequenceContext seqCtxt;
			int index = GetIndex(dest, leftEnv ? Rhs.LeftContextOA : Rhs.RightContextOA, out seqCtxt);
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
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(Rhs.LeftContextOA);
					Rhs.LeftContextOA = src;
				}
				else
				{
					if (preserveDest)
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(Rhs.RightContextOA);
					Rhs.RightContextOA = src;
				}
			}
			return index;
		}

		int GetIndex(IPhPhonContext ctxt, IPhPhonContext envCtxt, out IPhSequenceContext seqCtxt)
		{
			if (envCtxt.ClassID == PhSequenceContextTags.kClassId)
			{
				seqCtxt = (IPhSequenceContext) envCtxt;
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
				var iterCtxt = (IPhIterationContext) obj;
				min = iterCtxt.Minimum;
				max = iterCtxt.Maximum;
			}
			else
			{
				min = 1;
				max = 1;
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

		private readonly ITsTextProps m_ctxtProps;
		private readonly ITsTextProps m_charProps;

		private readonly ITsString m_arrow;
		private readonly ITsString m_slash;
		private readonly ITsString m_underscore;

		private IPhSegRuleRHS m_rhs;

		public RegRuleFormulaVc(Mediator mediator, PropertyTable propertyTable)
			: base(mediator, propertyTable)
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
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
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
