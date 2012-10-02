using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.Utils;
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
				if (cellId == (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc
					|| cellId == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange)
					return false;

				int hvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Anchor);
				int endHvo = GetItemHvo(sel, SelectionHelper.SelLimitType.End);
				if (hvo != endHvo || hvo == 0 || endHvo == 0)
					return false;

				IPhPhonContext ctxt = PhPhonContext.CreateFromDBObject(m_cache, hvo);
				return ctxt.ClassID != PhSimpleContextBdry.kclsidPhSimpleContextBdry;
			}
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);

			m_view.Init(mediator, obj, this, new RegRuleFormulaVc(cache, mediator), RegRuleFormulaVc.kfragRHS);

			IPhRegularRule rule = new PhRegularRule(m_cache, RHS.OwnerHVO);
			m_insertionControl.Initialize(cache, mediator, persistProvider, rule.Name.BestAnalysisAlternative.Text);
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
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOAHvo == 0)
						return true;

					int[] leftHvos = null;
					IPhPhonContext first = null;
					if (RHS.LeftContextOA.ClassID != PhSequenceContext.kclsidPhSequenceContext)
					{
						leftHvos = new int[] { RHS.LeftContextOAHvo };
						first = RHS.LeftContextOA;
					}
					else
					{
						IPhSequenceContext seqCtxt = RHS.LeftContextOA as IPhSequenceContext;
						if (seqCtxt.MembersRS.Count > 0)
							first = seqCtxt.MembersRS[0];
						leftHvos = seqCtxt.MembersRS.HvoArray;
					}

					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
							return GetIndicesToRemove(leftHvos, sel)[0] == 0;
						else
							return GetInsertionIndex(leftHvos, sel) == 0 && !IsWordBoundary(first);
					}
					else
					{
						// we cannot insert anything to the left of a word boundary in the left context
						return sel.IsRange || GetInsertionIndex(leftHvos, sel) != 0 || !IsWordBoundary(first);
					}

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOAHvo == 0 || sel.IsRange)
						return true;

					int[] rightHvos = null;
					IPhPhonContext last = null;
					if (RHS.RightContextOA.ClassID != PhSequenceContext.kclsidPhSequenceContext)
					{
						rightHvos = new int[] { RHS.RightContextOAHvo };
						last = RHS.RightContextOA;
					}
					else
					{
						IPhSequenceContext seqCtxt = RHS.RightContextOA as IPhSequenceContext;
						if (seqCtxt.MembersRS.Count > 0)
							last = seqCtxt.MembersRS[seqCtxt.MembersRS.Count - 1];
						rightHvos = seqCtxt.MembersRS.HvoArray;
					}

					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							int[] indices = GetIndicesToRemove(rightHvos, sel);
							return indices[indices.Length - 1] == rightHvos.Length - 1;
						}
						else
						{
							return GetInsertionIndex(rightHvos, sel) == rightHvos.Length && !IsWordBoundary(last);
						}
					}
					else
					{
						// we cannot insert anything to the right of a word boundary in the right context
						return sel.IsRange || GetInsertionIndex(rightHvos, sel) != rightHvos.Length || !IsWordBoundary(last);
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

			if (RHS.LeftContextOAHvo != 0)
				RHS.LeftContextOA.DeleteUnderlyingObject();
			InsertContextsFromEnv(leftEnv, (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext, null);

			if (RHS.RightContextOAHvo != 0)
				RHS.RightContextOA.DeleteUnderlyingObject();
			InsertContextsFromEnv(rightEnv, (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext, null);

			return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext;
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
						IPhSimpleContextBdry bdryCtxt = new PhSimpleContextBdry();
						AppendToEnv(bdryCtxt, flid);
						bdryCtxt.FeatureStructureRAHvo = m_cache.GetIdFromGuid(LangProject.kguidPhRuleWordBdry);
						bdryCtxt.NotifyNew();
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
								IPhSimpleContextNC ncCtxt = new PhSimpleContextNC();
								if (iterCtxt != null)
								{
									m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(ncCtxt);
									iterCtxt.MemberRAHvo = ncCtxt.Hvo;
								}
								else
								{
									AppendToEnv(ncCtxt, flid);
								}
								ncCtxt.FeatureStructureRAHvo = nc.Hvo;
								ncCtxt.NotifyNew();
								break;
							}
						}
						i = closeBracket + 1;
						break;

					case '(':
						int closeParen = envStr.IndexOf(')', i + 1);
						string str = envStr.Substring(i + 1, closeParen - (i + 1));
						IPhIterationContext newIterCtxt = new PhIterationContext();
						AppendToEnv(newIterCtxt, flid);
						newIterCtxt.Minimum = 0;
						newIterCtxt.Maximum = 1;
						InsertContextsFromEnv(str, flid, newIterCtxt);
						newIterCtxt.NotifyNew();
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
										IPhSimpleContextSeg segCtxt = new PhSimpleContextSeg();
										if (iterCtxt != null)
										{
											m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(segCtxt);
											iterCtxt.MemberRAHvo = segCtxt.Hvo;
										}
										else
										{
											AppendToEnv(segCtxt, flid);
										}
										segCtxt.FeatureStructureRAHvo = phoneme.Hvo;
										segCtxt.NotifyNew();
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
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOAHvo == 0)
						RHS.LeftContextOA = ctxt;
					else
						seqCtxt = CreateSeqCtxt(flid);
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOAHvo == 0)
						RHS.RightContextOA = ctxt;
					else
						seqCtxt = CreateSeqCtxt(flid);
					break;
			}

			if (seqCtxt != null)
			{
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(ctxt);
				seqCtxt.MembersRS.Append(ctxt);
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
			return flid == (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc
				|| flid == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange
				|| flid == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext
				|| flid == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext;
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					return -1;
			}
			return -1;
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					return -1;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					return (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange;
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					return (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext;
			}
			return -1;
		}

		protected override int GetItemHvo(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return 0;

			int cellId = GetCell(sel);
			if (cellId < 0)
				return 0;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (IsCellFlid(level.tag) || level.tag == (int)PhSequenceContext.PhSequenceContextTags.kflidMembers)
					return level.hvo;
			}

			return 0;
		}

		protected override int GetItemCellIndex(int cellId, int hvo)
		{
			int index = -1;
			switch (cellId)
			{
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					index = m_cache.GetObjIndex(RHS.OwnerHVO, cellId, hvo);
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					index = m_cache.GetObjIndex(RHS.Hvo, cellId, hvo);
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					int ctxtHvo = m_cache.GetObjProperty(RHS.Hvo, cellId);
					if (m_cache.GetClassOfObject(ctxtHvo) == PhSequenceContext.kclsidPhSequenceContext)
					{
						int[] hvos = m_cache.GetVectorProperty(ctxtHvo, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers, false);
						for (int i = 0; i < hvos.Length; i++)
						{
							if (hvos[i] == hvo)
							{
								index = i;
								break;
							}
						}
					}
					else if (ctxtHvo != 0)
					{
						index = 0;
					}
					break;
			}
			return index;
		}

		protected override SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			SelLevInfo[] levels = null;
			switch (cellId)
			{
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					if (cellIndex < 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = m_cache.GetFlid(RHS.Hvo, null, "OwningRule");
					}
					else
					{
						levels = new SelLevInfo[2];
						levels[0].tag = (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc;
						levels[0].ihvo = cellIndex;
						levels[1].tag = m_cache.GetFlid(RHS.Hvo, null, "OwningRule");
					}
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					if (cellIndex >= 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange;
						levels[0].ihvo = cellIndex;
					}
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					int hvo = m_cache.GetObjProperty(RHS.Hvo, cellId);
					if (hvo != 0)
					{
						switch (m_cache.GetClassOfObject(hvo))
						{
							case PhSequenceContext.kclsidPhSequenceContext:
								if (cellIndex < 0)
								{
									levels = new SelLevInfo[1];
									levels[0].tag = cellId;
								}
								else
								{
									levels = new SelLevInfo[2];
									levels[0].tag = (int)PhSequenceContext.PhSequenceContextTags.kflidMembers;
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
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					return m_cache.GetVectorSize(RHS.OwnerHVO, cellId);

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					return m_cache.GetVectorSize(RHS.Hvo, cellId);

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					int hvo = m_cache.GetObjProperty(RHS.Hvo, cellId);
					if (hvo == 0)
						return 0;
					else if (m_cache.GetClassOfObject(hvo) != PhSequenceContext.kclsidPhSequenceContext)
						return 1;
					else
						return m_cache.GetVectorSize(hvo, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers);
			}
			return 0;
		}

		protected override int GetFlid(int cellId)
		{
			return cellId;
		}

		protected override int InsertPhoneme(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextSeg(), (int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		protected override int InsertBdry(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextBdry(), (int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		protected override int InsertNC(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextNC(), (int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		int InsertContext(IPhSimpleContext ctxt, int fsFlid, int fsHvo, SelectionHelper sel, out int cellIndex)
		{
			cellIndex = -1;
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					IPhRegularRule rule = new PhRegularRule(m_cache, RHS.OwnerHVO);
					cellIndex = InsertContextInto(ctxt, fsFlid, fsHvo, sel, rule.StrucDescOS);
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					cellIndex = InsertContextInto(ctxt, fsFlid, fsHvo, sel, RHS.StrucChangeOS);
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOAHvo == 0)
					{
						RHS.LeftContextOA = ctxt;
						m_cache.SetObjProperty(ctxt.Hvo, fsFlid, fsHvo);
						ctxt.NotifyNew();
					}
					else
					{
						cellIndex = InsertContextInto(ctxt, fsFlid, fsHvo, sel, CreateSeqCtxt(cellId));
					}
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOAHvo == 0)
					{
						RHS.RightContextOA = ctxt;
						m_cache.SetObjProperty(ctxt.Hvo, fsFlid, fsHvo);
						ctxt.NotifyNew();
					}
					else
					{
						cellIndex = InsertContextInto(ctxt, fsFlid, fsHvo, sel, CreateSeqCtxt(cellId));
					}
					break;
			}
			return cellId;
		}

		IPhSequenceContext CreateSeqCtxt(int flid)
		{
			bool leftEnv = flid == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext;
			IPhPhonContext ctxt = leftEnv ? RHS.LeftContextOA : RHS.RightContextOA;
			if (ctxt == null)
				return null;

			IPhSequenceContext seqCtxt = null;
			if (ctxt.ClassID != PhSequenceContext.kclsidPhSequenceContext)
			{
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(ctxt);
				seqCtxt = new PhSequenceContext();
				if (leftEnv)
					RHS.LeftContextOA = seqCtxt;
				else
					RHS.RightContextOA = seqCtxt;
				seqCtxt.MembersRS.Append(ctxt);
				seqCtxt.NotifyNew();
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
				case (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc:
					IPhRegularRule rule = new PhRegularRule(m_cache, RHS.OwnerHVO);
					if (!RemoveContextsFrom(forward, sel, rule.StrucDescOS, true, out cellIndex))
						cellId = -1;
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange:
					if (RHS.StrucChangeOS == null)
					{
						cellId = -1;
						break;
					}
					if (!RemoveContextsFrom(forward, sel, RHS.StrucChangeOS, true, out cellIndex))
						cellId = -1;
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext:
					if (RHS.LeftContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (RHS.LeftContextOA.ClassID == PhSequenceContext.kclsidPhSequenceContext)
					{
						IPhSequenceContext seqCtxt = RHS.LeftContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new int[] {RHS.LeftContextOAHvo}, sel, forward);
						if (idx > -1)
							RHS.LeftContextOA.DeleteUnderlyingObject();
						else
							cellId = -1;
					}
					break;

				case (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext:
					if (RHS.RightContextOA == null)
					{
						cellId = -1;
						break;
					}
					if (RHS.RightContextOA.ClassID == PhSequenceContext.kclsidPhSequenceContext)
					{
						IPhSequenceContext seqCtxt = RHS.RightContextOA as IPhSequenceContext;
						if (!RemoveContextsFrom(forward, sel, seqCtxt, true, out cellIndex))
							cellId = -1;
					}
					else
					{
						int idx = GetIndexToRemove(new int[] { RHS.RightContextOAHvo }, sel, forward);
						if (idx > -1)
							RHS.RightContextOA.DeleteUnderlyingObject();
						else
							cellId = -1;
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
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			int hvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Anchor);
			IPhPhonContext ctxt = PhPhonContext.CreateFromDBObject(m_cache, hvo);
			int index = -1;
			using (new UndoRedoTaskHelper(m_cache, MEStrings.ksRegRuleUndoSetOccurrence, MEStrings.ksRegRuleRedoSetOccurrence))
			{
				if (ctxt.ClassID == PhIterationContext.kclsidPhIterationContext)
				{
					// if there is an existing iteration context, just update it or remove it if it can occur only once
					IPhIterationContext iterCtxt = ctxt as IPhIterationContext;
					if (min == 1 && max == 1)
					{
						index = OverwriteContext(iterCtxt.MemberRA, iterCtxt, cellId == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext, false);
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
					IPhIterationContext iterCtxt = new PhIterationContext();
					index = OverwriteContext(iterCtxt, ctxt, cellId == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext, true);
					iterCtxt.MemberRAHvo = ctxt.Hvo;
					iterCtxt.Minimum = min;
					iterCtxt.Maximum = max;
					iterCtxt.NotifyNew();
				}
			}

			if (index == -1)
			{
				IPhPhonContext envCtxt = cellId == (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext ? RHS.LeftContextOA : RHS.RightContextOA;
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
				if (!src.IsRealObject)
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(src);

				seqCtxt.MembersRS.InsertAt(src, index);
				seqCtxt.MembersRS.Remove(dest);
				if (!preserveDest)
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(dest);
			}
			else
			{
				if (leftEnv)
				{
					if (preserveDest)
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(RHS.LeftContextOA);
					RHS.LeftContextOA = src;
				}
				else
				{
					if (preserveDest)
						m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(RHS.RightContextOA);
					RHS.RightContextOA = src;
				}
			}
			return index;
		}

		int GetIndex(IPhPhonContext ctxt, IPhPhonContext envCtxt, out IPhSequenceContext seqCtxt)
		{
			if (envCtxt.ClassID == PhSequenceContext.kclsidPhSequenceContext)
			{
				seqCtxt = envCtxt as IPhSequenceContext;
				int[] hvos = seqCtxt.MembersRS.HvoArray;
				for (int i = 0; i < hvos.Length; i++)
				{
					if (ctxt.Hvo == hvos[i])
						return i;
				}
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
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int hvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Anchor);
			IPhPhonContext ctxt = PhPhonContext.CreateFromDBObject(m_cache, hvo);
			if (ctxt.ClassID == PhIterationContext.kclsidPhIterationContext)
			{
				IPhIterationContext iterCtxt = ctxt as IPhIterationContext;
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
			SelectionHelper sel = SelectionHelper.Create(m_view);
			bool reconstruct = false;
			using (new UndoRedoTaskHelper(m_cache, MEStrings.ksRegRuleUndoSetVariables, MEStrings.ksRegRuleRedoSetVariables))
			{
				using (FeatureConstraintChooserDlg featChooser = new FeatureConstraintChooserDlg())
				{
					IPhSimpleContextNC ctxt = new PhSimpleContextNC(m_cache, CurrentContextHvo);
					featChooser.SetDlgInfo(m_cache, Mediator, new PhRegularRule(m_cache, RHS.OwnerHVO), ctxt);
					DialogResult res = featChooser.ShowDialog();
					if (res != DialogResult.Cancel)
						featChooser.HandleJump();
					reconstruct = res == DialogResult.OK;
				}
			}

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

			m_arrow = m_cache.MakeUserTss("\u2192");
			m_slash = m_cache.MakeUserTss("/");
			m_underscore = m_cache.MakeUserTss("__");
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_rhs = null;

			if (m_ctxtProps != null)
			{
				Marshal.ReleaseComObject(m_ctxtProps);
				m_ctxtProps = null;
			}
			if (m_charProps != null)
			{
				Marshal.ReleaseComObject(m_charProps);
				m_charProps = null;
			}
			if (m_arrow != null)
			{
				Marshal.ReleaseComObject(m_arrow);
				m_arrow = null;
			}
			if (m_slash != null)
			{
				Marshal.ReleaseComObject(m_slash);
				m_slash = null;
			}
			if (m_underscore != null)
			{
				Marshal.ReleaseComObject(m_underscore);
				m_underscore = null;
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		protected override int MaxNumLines
		{
			get
			{
				IPhRegularRule rule = new PhRegularRule(m_cache, m_rhs.OwnerHVO);

				int numLines = GetNumLines(rule.StrucDescOS);
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
			IPhRegularRule rule = new PhRegularRule(m_cache, m_rhs.OwnerHVO);
			List<int> featConstrs = rule.FeatureConstraints;
			for (int i = 0; i < featConstrs.Count; i++)
			{
				if (var.Hvo == featConstrs[i])
					return i;
			}
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();
			switch (frag)
			{
				case kfragRHS:
					m_rhs = new PhSegRuleRHS(m_cache, hvo);
					IPhRegularRule rule = new PhRegularRule(m_cache, m_rhs.OwnerHVO);

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
					vwenv.AddObjProp(m_cache.GetFlid(hvo, null, "OwningRule"), this, kfragRule);
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
					if (m_cache.GetVectorSize(hvo, (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange) > 0)
					{
						vwenv.AddObjVecItems((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange, this, kfragContext);
					}
					else
					{
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidStrucChange, this, kfragEmpty);
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
					if (m_cache.GetObjProperty(hvo, (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext) != 0)
					{
						vwenv.AddObjProp((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext, this, kfragContext);
					}
					else
					{
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidLeftContext, this, kfragEmpty);
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
					if (m_cache.GetObjProperty(hvo, (int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext) != 0)
					{
						vwenv.AddObjProp((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext, this, kfragContext);
					}
					else
					{
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp((int)PhSegRuleRHS.PhSegRuleRHSTags.kflidRightContext, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragRule:
					if (m_cache.GetVectorSize(hvo, (int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc) > 0)
					{
						vwenv.AddObjVecItems((int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc, this, kfragContext);
					}
					else
					{
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp((int)PhRegularRule.PhSegmentRuleTags.kflidStrucDesc, this, kfragEmpty);
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
