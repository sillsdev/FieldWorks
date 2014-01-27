using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	#region LexRefTypeFactory
	internal partial class LexRefTypeFactory
	{
		public ILexRefType Create(Guid guid, ILexRefType owner)
		{
			ILexRefType lexRefType;
			if(guid == Guid.Empty)
			{
				lexRefType = Create();
			}
			else
			{
				var hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				lexRefType = new LexRefType(m_cache, hvo, guid);
			}
			if(owner != null)
			{
				owner.SubPossibilitiesOS.Add(lexRefType);
			}
			return lexRefType;
		}
	}
	#endregion

	#region LexSenseFactory class
	internal partial class LexSenseFactory
	{
		#region Implementation of ILexSenseFactory

		/// <summary>
		/// Create a new sense and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sandboxMSA"></param>
		/// <param name="gloss">string form of gloss, will be put in DefaultAnalysis ws</param>
		/// <returns></returns>
		public ILexSense Create(ILexEntry entry, SandboxGenericMSA sandboxMSA, string gloss)
		{
			// Handle gloss.
			if (!string.IsNullOrEmpty(gloss))
			{
				var defAnalWs = entry.Cache.DefaultAnalWs;
				var gls = entry.Cache.TsStrFactory.MakeString(gloss, defAnalWs);

				return Create(entry, sandboxMSA, gls);
			}

			return Create(entry, sandboxMSA, (ITsString)null);
		}

		/// <summary>
		/// Create a new sense and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sandboxMSA"></param>
		/// <param name="gloss"></param>
		/// <returns></returns>
		public ILexSense Create(ILexEntry entry, SandboxGenericMSA sandboxMSA, ITsString gloss)
		{
			var sense = new LexSense();
			entry.SensesOS.Add(sense);
			sense.SandboxMSA = sandboxMSA;

			if (gloss != null)
			{
				if (gloss.Length > 256)
				{
					MessageBoxUtils.Show(Strings.ksTruncatingGloss, Strings.ksWarning,
														System.Windows.Forms.MessageBoxButtons.OK,
														System.Windows.Forms.MessageBoxIcon.Warning);
					gloss = gloss.Substring(0, 256);
				}
				sense.Gloss.set_String(gloss.get_WritingSystemAt(0), gloss);
			}
			return sense;
		}

		/// <summary>
		/// Create a new sense with the given guid owned by the given entry.
		/// </summary>
		public ILexSense Create(Guid guid, ILexEntry owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			ILexSense ls;
			if (guid == Guid.Empty)
			{
				ls = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				ls = new LexSense(m_cache, hvo, guid);
			}
			owner.SensesOS.Add(ls);
			return ls;
		}

		/// <summary>
		/// Create a new subsense with the given guid owned by the given sense.
		/// </summary>
		public ILexSense Create(Guid guid, ILexSense owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			ILexSense ls;
			if (guid == Guid.Empty)
			{
				ls = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				ls = new LexSense(m_cache, hvo, guid);
			}
			owner.SensesOS.Add(ls);
			return ls;
		}

		/// <summary>
		/// This is invoked (using reflection) by an XmlRDEBrowseView when the user presses
		/// "Enter" in an RDE view that is displaying lexeme form and definition.
		/// (Maybe also on loss of focus, switch domain, etc?)
		/// It creates a new entry, lexeme form, and sense that are linked to the specified domain.
		/// Typically, later, a call to RDEMergeSense will be made to see whether this
		/// new entry should be merged into some existing sense.
		/// </summary>
		/// <param name="hvoDomain">database id of the semantic domain</param>
		/// <param name="columns"></param>
		/// <param name="rgtss"></param>
		/// <param name="stringTbl"></param>
		public int RDENewSense(int hvoDomain, List<XmlNode> columns, ITsString[] rgtss, StringTable stringTbl)
		{
			Debug.Assert(hvoDomain != 0);
			Debug.Assert(rgtss.Length == columns.Count);

			// Make a new sense in a new entry.
			ILexEntry le = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoForm morph = null;

			// create a LexSense that has the given definition and semantic domain
			// Needs to be LexSense, since later calls use non-interface methods.
			LexSense ls = Create() as LexSense;
			le.SensesOS.Add(ls);

#pragma warning disable 219
			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
#pragma warning restore 219
			// go through each column and store the appropriate information.
			for (int i = 0; i < columns.Count; ++i)
			{
				// Review: Currently we key off the column labels to determine which columns
				// correspond to CitationForm and which correspond to Definition.
				// Ideally we'd like to get at the flids used to build the column display strings.
				// Instead of passing in only ITsStrings, we could pass in a structure containing
				// an index of strings with any corresponding flids.  Here we'd expect strings
				// based upon either LexemeForm.Form or LexSense.Definition. We could probably
				// do this as part of the solution to handling duplicate columns in LT-3763.
				XmlNode column = columns[i] as XmlNode;
				string columnLabel = XmlUtils.GetManditoryAttributeValue(column, "label");
				ITsTextProps ttp = rgtss[i].get_PropertiesAt(0);
				int var;
				int ws = ttp.GetIntPropValues((int) FwTextPropType.ktptWs, out var);
				Debug.Assert(ws != 0);

				ITsString tssStr = rgtss[i];
				string trimmedForm = tssStr.Text;
				if (trimmedForm == null)
					continue; // no point in setting empty field, and MakeMorph may blow up
				trimmedForm = trimmedForm.Trim();
				if (trimmedForm.Length == 0)
					continue;

				// Note: the four column labels we check for should NOT be localized, as we are comparing with
				// the label that appears in the original XML configuration file. Localization is not applied
				// to that file, but using it as a base, we look up the localized string to display in the tool.
				if (columnLabel.StartsWith(@"Word (Lexeme Form)"))
				{
					if (morph == null)
						morph = MakeMorphRde(le, tssStr, ws, trimmedForm);
					else
					{
						// The type of MoForm has been determined by a previous column, but in any case, we don't want
						// morpheme break characters in the lexeme form.
						morph.Form.set_String(ws, MorphServices.EnsureNoMarkers(trimmedForm, m_cache));
					}
					Debug.Assert(le.LexemeFormOA != null);
				}
				else if (columnLabel.StartsWith(@"Word (Citation Form)"))
				{
					if (morph == null)
					{
						morph = MakeMorphRde(le, tssStr, ws, trimmedForm);
						// We'll set the value based on all the nice logic in MakeMorph for trimming morpheme-type indicators
						le.CitationForm.set_String(ws, morph.Form.get_String(ws));
						morph.Form.set_String(ws, ""); // and this isn't really the lexeme form, so leave that empty.
					}
					else
					{
						// The type of MoForm has been determined by a previous column, but in any case, we don't want
						// morpheme break characters in the citation form.
						le.CitationForm.set_String(ws, MorphServices.EnsureNoMarkers(trimmedForm, m_cache));
					}
				}
				else if (columnLabel.StartsWith(@"Meaning (Definition)"))
				{
					if (trimmedForm != "")
						ls.Definition.set_String(ws, trimmedForm);
				}
				else if (columnLabel.StartsWith(@"Meaning (Gloss)"))
				{
					if (trimmedForm != "")
						ls.Gloss.set_String(ws, trimmedForm);
				}
				else if (!HandleTransduceColum(ls, column, ws, tssStr))
				{
					Debug.Fail("column (" + columnLabel + ") not supported.");
				}
			}
			if (morph == null)
				morph = le.LexemeFormOA = new MoStemAllomorph();

			ls.SemanticDomainsRC.Add(m_cache.ServiceLocator.GetObject(hvoDomain) as CmSemanticDomain);

			if (le.MorphoSyntaxAnalysesOC.Count == 0)
			{
				// Commonly, it's a new entry with no MSAs; make sure it has at least one.
				// This way of doing it allows a good bit of code to be shared with the normal
				// creation path, as if the user made a stem but didn't fill in any grammatical
				// information.
				SandboxGenericMSA dummyMsa = new SandboxGenericMSA();
				if (morph != null && morph is IMoAffixForm)
					dummyMsa.MsaType = MsaType.kUnclassified;
				else
					dummyMsa.MsaType = MsaType.kStem;
				ls.SandboxMSA = dummyMsa;
			}

			// We don't want a partial MSA created, so don't bother doing anything
			// about setting ls.MorphoSyntaxAnalysisRA
			return ls.Hvo;
		}

		/// <summary>
		/// Handle a column that contains a "transduce" specification indicating how to find the
		/// field that should be filled in. Currently we support class.field, where class is
		/// LexEntry, LexSense, LexExampleSentence, or CmTranslation, and field is one of the multilingual
		/// or simple string fields of that class.
		/// LexSense means set a field of the sense passed to the method; entry means its owning entry;
		/// example means its first example, which will be created if it doesn't already have one;
		/// and CmTranslation means the first translation of the first example (both of which will
		/// be created if needed). (Since this is used as part of RDENewSense, the first example field
		/// encountered will always create a new example, and the first translation field a new translation.)
		/// enhance: also handle class.field.field, where the first field indicates an atomic object
		/// property?
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="column"></param>
		/// <param name="ws"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private bool HandleTransduceColum(LexSense ls, XmlNode column, int ws, ITsString val)
		{
			var transduce = XmlUtils.GetOptionalAttributeValue(column, "transduce");
			if (string.IsNullOrEmpty(transduce))
				return false;
			var mdc = ls.Cache.MetaDataCacheAccessor;
			var parts = transduce.Split('.');
			if (parts.Length == 2)
			{
				var className = parts[0];
				var fieldName = parts[1];
				int flid = mdc.GetFieldId(className, fieldName, true);
				int hvo;
				switch (className)
				{
					case "LexSense":
						hvo = ls.Hvo;
						break;
					case "LexEntry":
						hvo = ls.OwningEntry.Hvo;
						break;
					case "LexExampleSentence":
						hvo = GetOrMakeFirstExample(ls).Hvo;
						break;
					case "CmTranslation":
						var example = GetOrMakeFirstExample(ls);
						hvo = GetOrMakeFirstTranslation(example).Hvo;
						break;
						// Enhance JohnT: handle other cases as needed.
					default:
						throw new ArgumentException(
							string.Format("transduce attribute of column argument specifies an unhandled class ({0})"), className);
				}
				if (mdc.GetFieldType(flid) == (int)CellarPropertyType.String)
					ls.Cache.DomainDataByFlid.SetString(hvo, flid, val);
				else // asssume multistring
					ls.Cache.DomainDataByFlid.SetMultiStringAlt(hvo, flid, ws, val);
				return true;
			}
			throw new ArgumentException("transduce attr for column spec has wrong number of parts " + transduce + " " + column.OuterXml);
		}

		private ICmTranslation GetOrMakeFirstTranslation(ILexExampleSentence example)
		{
			if (example.TranslationsOC.Count == 0)
			{
				var cmTranslation = example.Services.GetInstance<ICmTranslationFactory>().Create(example, m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation));
				example.TranslationsOC.Add(cmTranslation);
			}
			return example.TranslationsOC.ToArray()[0];
		}

		private static ILexExampleSentence GetOrMakeFirstExample(LexSense ls)
		{
			if (ls.ExamplesOS.Count == 0)
				ls.ExamplesOS.Add(ls.Services.GetInstance<ILexExampleSentenceFactory>().Create());
			return ls.ExamplesOS.ToArray()[0];
		}

		private IMoForm MakeMorphRde(ILexEntry entry, ITsString form, int ws, string trimmedForm)
		{
			var morph = MorphServices.MakeMorph(entry, form);
			if (morph is IMoStemAllomorph)
			{
				// Make sure we have a proper allomorph and MSA for this new entry and sense.
				// (See LT-1318 for details and justification.)
				var morphTypeRep = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				if (trimmedForm.IndexOf(' ') > 0)
					morph.MorphTypeRA = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphPhrase);
				else
					morph.MorphTypeRA = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphStem);
				morph.Form.set_String(ws, trimmedForm);
			}
			return morph;
		}

		#endregion
	}
	#endregion

	#region LexExampleSentenceFactory class
	internal partial class LexExampleSentenceFactory
	{
		#region Implementation of ILexExampleSentenceFactory

		public ILexExampleSentence Create(Guid guid, ILexSense owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			ILexExampleSentence les;
			if (guid == Guid.Empty)
			{
				les = new LexExampleSentence();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				les = new LexExampleSentence(m_cache, hvo, guid);
			}
			owner.ExamplesOS.Add(les);
			return les;
		}
		#endregion
	}
	#endregion

	#region LexEntryFactory class
	internal partial class LexEntryFactory
	{
		#region Implementation of ILexEntryFactory

		/// <summary>
		/// Creates a new LexEntry with the given fields
		/// </summary>
		/// <param name="morphType"></param>
		/// <param name="tssLexemeForm"></param>
		/// <param name="gloss">string for gloss, placed in DefaultAnalysis ws</param>
		/// <param name="sandboxMSA"></param>
		/// <returns></returns>
		public ILexEntry Create(IMoMorphType morphType, ITsString tssLexemeForm, string gloss, SandboxGenericMSA sandboxMSA)
		{
			int writingSystem = m_cache.WritingSystemFactory.GetWsFromStr(m_cache.LangProject.DefaultAnalysisWritingSystem.Id);
			var tssGloss = m_cache.TsStrFactory.MakeString(gloss, writingSystem);
			return Create(morphType, tssLexemeForm, tssGloss, sandboxMSA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new entry.
		/// </summary>
		/// <param name="morphType">Type of the morph.</param>
		/// <param name="tssLexemeForm">The TSS lexeme form.</param>
		/// <param name="gloss">The gloss.</param>
		/// <param name="sandboxMSA">The dummy MSA.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ILexEntry Create(IMoMorphType morphType, ITsString tssLexemeForm, ITsString gloss, SandboxGenericMSA sandboxMSA)
		{
			var entry = Create();
			var sense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(entry, sandboxMSA, gloss);

			if (morphType.Guid == MoMorphTypeTags.kguidMorphCircumfix)
			{
				m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().CreateCircumfix(
					entry, sense, tssLexemeForm, morphType);
			}
			else
			{
#pragma warning disable 168
				var allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA, tssLexemeForm, morphType, true);
#pragma warning restore 168
			}
			// (We don't want a citation form by default.  See LT-7220.)
			return entry;
		}

		/// <summary>
		/// Creates an entry with a form in default vernacular and a sense with a gloss in default analysis.
		/// </summary>
		/// <param name="entryFullForm">entry form including any markers</param>
		/// <param name="senseGloss"></param>
		/// <param name="msa"></param>
		/// <returns></returns>
		public ILexEntry Create(string entryFullForm, string senseGloss, SandboxGenericMSA msa)
		{
			ITsString tssFullForm = TsStringUtils.MakeTss(entryFullForm, m_cache.DefaultVernWs);
			// create a sense with a matching gloss
			var entryComponents = MorphServices.BuildEntryComponents(m_cache, tssFullForm);
			entryComponents.MSA = msa;
			entryComponents.GlossAlternatives.Add(TsStringUtils.MakeTss(senseGloss, m_cache.DefaultAnalWs));
			return m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="entryComponents"></param>
		/// <returns></returns>
		public ILexEntry Create(LexEntryComponents entryComponents)
		{
			if (entryComponents.MorphType == null)
				throw new ArgumentException("Expected entryComponents to already have MorphType");
			var tssGloss =
				entryComponents.GlossAlternatives.DefaultIfEmpty(TsStringUtils.MakeTss("", m_cache.DefaultAnalWs)).
					FirstOrDefault();
			ILexEntry newEntry = Create(entryComponents.MorphType,
				entryComponents.LexemeFormAlternatives[0],
				tssGloss,
				entryComponents.MSA);

			foreach (ITsString tss in entryComponents.LexemeFormAlternatives)
				newEntry.SetLexemeFormAlt(TsStringUtils.GetWsAtOffset(tss, 0), tss);

			foreach (ITsString tss in entryComponents.GlossAlternatives)
				newEntry.SensesOS[0].Gloss.set_String(TsStringUtils.GetWsAtOffset(tss, 0), tss);

			var featsys = m_cache.LanguageProject.MsFeatureSystemOA;
			foreach (XmlNode xn in entryComponents.GlossFeatures)
			{
				featsys.AddFeatureFromXml(xn);
				foreach (var msa in newEntry.MorphoSyntaxAnalysesOC)
				{
					var infl = msa as IMoInflAffMsa;
					if (infl != null)
					{
						if (infl.InflFeatsOA == null)
							infl.InflFeatsOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();

						infl.InflFeatsOA.AddFeatureFromXml(xn, featsys);
						// if there is a POS, add features to topmost pos' inflectable features
						var pos = infl.PartOfSpeechRA;
						if (pos != null)
						{
							var topPos = pos.HighestPartOfSpeech;
							topPos.AddInflectableFeatsFromXml(xn);
						}
					}
				}
			}
			return newEntry;
		}
		/// <summary>
		/// Create a new entry with the given guid owned by the given owner.
		/// </summary>
		public ILexEntry Create(Guid guid, ILexDb owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			ILexEntry le;
			if (guid == Guid.Empty)
			{
				le = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				le = new LexEntry(m_cache, hvo, guid);
			}
			return le;
		}
		#endregion
	}
	#endregion

	#region MoAffixAllomorphFactory class
	internal partial class MoAffixAllomorphFactory
	{
		#region Implementation of IMoAffixAllomorphFactory

		/// <summary>
		/// Create a new circumfix allomorph and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sense"></param>
		/// <param name="lexemeForm"></param>
		/// <param name="morphType"></param>
		/// <returns></returns>
		public IMoAffixAllomorph CreateCircumfix(ILexEntry entry, ILexSense sense, ITsString lexemeForm, IMoMorphType morphType)
		{
			var lexemeAllo = new MoAffixAllomorph();
			entry.LexemeFormOA = lexemeAllo;
			lexemeAllo.Form.set_String(TsStringUtils.GetWsAtOffset(lexemeForm, 0), lexemeForm);
			lexemeAllo.MorphTypeRA = morphType;
			lexemeAllo.IsAbstract = true;

			// split citation form into left and right parts
			var aSpacePeriod = new[] { ' ', '.' };
			var lexemeFormAsString = lexemeForm.Text;
			var wsVern = TsStringUtils.GetWsAtOffset(lexemeForm, 0);
			var iLeftEnd = lexemeFormAsString.IndexOfAny(aSpacePeriod);
			var sLeftMember = iLeftEnd < 0 ? lexemeFormAsString : lexemeFormAsString.Substring(0, iLeftEnd);
			var iRightBegin = lexemeFormAsString.LastIndexOfAny(aSpacePeriod);
			var sRightMember = iRightBegin < 0 ? lexemeFormAsString : lexemeFormAsString.Substring(iRightBegin + 1);
			// Create left and right allomorphs
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			MorphServices.GetMajorAffixMorphTypes(m_cache, out mmtPrefix, out mmtSuffix, out mmtInfix);
			int clsidForm;
			var mmt = MorphServices.FindMorphType(m_cache, ref sLeftMember, out clsidForm);
			if ((mmt.Hvo != mmtPrefix.Hvo) &&
				(mmt.Hvo != mmtInfix.Hvo))
				mmt = mmtPrefix; // force a prefix if it's neither a prefix nor an infix
#pragma warning disable 168
			var allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA,
												   TsStringUtils.MakeTss(sLeftMember, wsVern), mmt, false);
#pragma warning disable 168
			mmt = MorphServices.FindMorphType(m_cache, ref sRightMember, out clsidForm);
			if ((mmt.Hvo != mmtInfix.Hvo) &&
				(mmt.Hvo != mmtSuffix.Hvo))
				mmt = mmtSuffix; // force a suffix if it's neither a suffix nor an infix
			allomorph = MoForm.CreateAllomorph(entry, sense.MorphoSyntaxAnalysisRA,
											   TsStringUtils.MakeTss(sRightMember, wsVern), mmt, false);

			return lexemeAllo;
		}

		#endregion
	}
	#endregion

	#region CmBaseAnnotationFactory class
	internal partial class CmBaseAnnotationFactory
	{
		private ICmBaseAnnotation Create(ICmAnnotationDefn annType, ICmObject instanceOf,
			IStTxtPara beginObject, int beginOffset, int endOffset)
		{
			ICmBaseAnnotation cba = Create();
			// for now we're treating annotations as ownerless, even though that's not what the
			// model says. eventually they'll be owned by things like paragraphs or other annotations.
			if (cba.Cache == null)
				((ICmObjectInternal) cba).InitializeNewOwnerlessCmObject(m_cache);
			else
				Debug.Fail("TODO(EricP): get rid of the code for InitializeNewOwnerlessCmObject");
			cba.AnnotationTypeRA = annType;
			SegmentServices.SetCbaFields(cba, beginObject, beginOffset, endOffset, instanceOf);
			return cba;
		}

		/// <summary>
		/// Create an ownerless object.  This is used in import.
		/// </summary>
		/// <remarks>This can be removed when/if all annotations are owned.</remarks>
		public ICmBaseAnnotation CreateOwnerless()
		{
			ICmBaseAnnotation cba = Create();
			((ICmObjectInternal)cba).InitializeNewOwnerlessCmObject(m_cache);
			return cba;
		}
	}
	#endregion

	#region CmIndirectAnnotationFactory class
	internal partial class CmIndirectAnnotationFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new indirect annotation
		/// </summary>
		/// <param name="annType">The type of indirect annotation</param>
		/// <param name="annotationsToWhichThisApplies">Zero or more annotations to which this
		/// annotation applies (typically a single segment)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmIndirectAnnotation Create(ICmAnnotationDefn annType,
			params ICmAnnotation[] annotationsToWhichThisApplies)
		{
			var ann = Create();
			// REVIEW(FWR-209): Indirect annotations, for now, should be treated ownerless.
			if (ann.Cache == null)
				((ICmObjectInternal)ann).InitializeNewOwnerlessCmObject(m_cache);
			else
				Debug.Fail("TODO(EricP): get rid of the code for InitializeNewOwnerlessCmObject");
			foreach (ICmAnnotation annot in annotationsToWhichThisApplies)
			{
				Debug.Assert(annot != null);
				ann.AppliesToRS.Add(annot);
			}
			// REVIEW(FWR-209): Should we check to ensure that the type is not null?
			ann.AnnotationTypeRA = annType;
			return ann;
		}

		/// <summary>
		/// Create an ownerless object.  This is used in import.
		/// </summary>
		/// <remarks>This can be removed when/if all annotations are owned.</remarks>
		public ICmIndirectAnnotation CreateOwnerless()
		{
			ICmIndirectAnnotation cia = Create();
			((ICmObjectInternal)cia).InitializeNewOwnerlessCmObject(m_cache);
			return cia;
		}
	}
	#endregion

	#region SegmentFactory class
	internal partial class SegmentFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new segment for the given paragraph with the specified begin offset.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="beginOffset">The begin offset.</param>
		/// ------------------------------------------------------------------------------------
		public ISegment Create(IStTxtPara para, int beginOffset)
		{
			Segment seg = (Segment)Create();
			para.SegmentsOS.Add(seg);
			seg.BeginOffset = beginOffset;
			return seg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new segment for the given paragraph with the specified begin offset.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="beginOffset">The begin offset.</param>
		/// <param name="cache">FdoCache to use for hvo creation</param>
		/// <param name="guid">The guid to initialize the segment with.</param>
		/// ------------------------------------------------------------------------------------
		public ISegment Create(IStTxtPara para, int beginOffset, FdoCache cache, Guid guid)
		{
			Segment seg = new Segment(cache, ((IDataReader)cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo(), guid);

			para.SegmentsOS.Add(seg);
			seg.BeginOffset = beginOffset;
			return seg;
		}
	}
	#endregion

	#region ConstChartTagFactory class
	internal partial class ConstChartTagFactory
	{
		/// <summary>
		/// Creates a new user-added Missing Text Marker
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public IConstChartTag CreateMissingMarker(IConstChartRow row, int insertAt, ICmPossibility column)
		{
			if (column == null || row == null)
				throw new ArgumentNullException();
			var ccells = row.CellsOS.Count;
			if (insertAt < 0 || insertAt > ccells) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			var newby = Create();
			row.CellsOS.Insert(insertAt, newby);
			newby.ColumnRA = column;
			newby.TagRA = null;
			return newby;
		}

		/// <summary>
		/// Creates a new Chart Marker from a list of Chart Marker possibilities
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="marker"></param>
		/// <returns></returns>
		public IConstChartTag Create(IConstChartRow row, int insertAt, ICmPossibility column, ICmPossibility marker)
		{
			if (column == null || row == null || marker == null)
				throw new ArgumentNullException();
			var ccells = row.CellsOS.Count;
			if (insertAt < 0 || insertAt > ccells) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			var newby = Create();
			row.CellsOS.Insert(insertAt, newby);
			newby.ColumnRA = column;
			newby.TagRA = marker;
			return newby;
		}
	}
	#endregion

	#region ConstChartClauseMarkerFactory class
	internal partial class ConstChartClauseMarkerFactory
	{
		/// <summary>
		/// Creates a new Chart Clause Marker (reference to dependent/speech/song clauses)
		/// Caller needs to setup the rows with the correct parameters (ClauseType, etc.).
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="depClauses">The chart rows that are dependent/speech/song</param>
		/// <returns></returns>
		public IConstChartClauseMarker Create(IConstChartRow row, int insertAt, ICmPossibility column,
			IEnumerable<IConstChartRow> depClauses)
		{
			if (column == null || row == null)
				throw new ArgumentNullException();
			var ccells = row.CellsOS.Count;
			if (insertAt < 0 || insertAt > ccells) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			var newby = Create();
			row.CellsOS.Insert(insertAt, newby);
			newby.ColumnRA = column;
			newby.DependentClausesRS.Replace(0, 0, depClauses as IEnumerable<ICmObject>);
			return newby;
		}
	}
	#endregion

	#region ConstChartMovedTextMarkerFactory class
	internal partial class ConstChartMovedTextMarkerFactory
	{
		/// <summary>
		/// Creates a new Chart Moved Text Marker (shows where some text was moved from).
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="fPreposed">True if the CCWG was 'moved' earlier than its 'normal' position</param>
		/// <param name="wordGroup">The CCWG that was 'moved'</param>
		/// <returns></returns>
		public IConstChartMovedTextMarker Create(IConstChartRow row, int insertAt, ICmPossibility column,
			bool fPreposed, IConstChartWordGroup wordGroup)
		{
			if (column == null || row == null)
				throw new ArgumentNullException();
			var ccells = row.CellsOS.Count;
			if (insertAt < 0 || insertAt > ccells) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			var newby = Create();
			row.CellsOS.Insert(insertAt, newby);
			newby.ColumnRA = column;
			newby.Preposed = fPreposed;
			newby.WordGroupRA = wordGroup;
			return newby;
		}
	}
	#endregion

	#region ConstChartWordGroupFactory class
	internal partial class ConstChartWordGroupFactory
	{
		/// <summary>
		/// Creates a new Chart Word Group from selected AnalysisOccurrence objects
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public IConstChartWordGroup Create(IConstChartRow row, int insertAt, ICmPossibility column,
			AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			if (column == null || row == null)
				throw new ArgumentNullException();
			var ccells = row.CellsOS.Count;
			if (insertAt < 0 || insertAt > ccells) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			if (begPoint == null || !begPoint.IsValid)
				throw new ArgumentException("Invalid beginPoint");
			if (endPoint == null || !endPoint.IsValid)
				throw new ArgumentException("Invalid endPoint");

			// Make the thing already!
			var newby = Create();
			row.CellsOS.Insert(insertAt, newby);
			newby.ColumnRA = column;
			newby.BeginSegmentRA = begPoint.Segment;
			newby.EndSegmentRA = endPoint.Segment;
			newby.BeginAnalysisIndex = begPoint.Index;
			newby.EndAnalysisIndex = endPoint.Index;
			return newby;
		}
	}
	#endregion

	#region ConstChartRowFactory class
	internal partial class ConstChartRowFactory
	{
		/// <summary>
		/// Creates a new Chart Row with the specified row number/letter label
		/// at the specified location in the specified chart.
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="insertAt"></param>
		/// <param name="rowLabel"></param>
		/// <returns></returns>
		public IConstChartRow Create(IDsConstChart chart, int insertAt, ITsString rowLabel)
		{
			if (chart == null)
				throw new ArgumentNullException("chart");
			if (rowLabel == null)
				throw new ArgumentNullException("rowLabel");
			if (insertAt < 0 || insertAt > chart.RowsOS.Count) // insertAt == Count will append
				throw new ArgumentOutOfRangeException("insertAt");
			var newby = Create();
			chart.RowsOS.Insert(insertAt, newby);
			newby.Label = rowLabel;
			newby.ClauseType = ClauseTypes.Normal;
			return newby;
		}
	}
	#endregion

	#region DsConstChartFactory class
	internal partial class DsConstChartFactory
	{
		/// <summary>
		/// Creates a new Constituent Chart object on a language project with a particular template
		/// and based on a particular text.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="text"></param>
		/// <param name="template"></param>
		/// <returns></returns>
		public IDsConstChart Create(IDsDiscourseData data, IStText text, ICmPossibility template)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (text == null)
				throw new ArgumentNullException("text");
			if (template == null)
				throw new ArgumentNullException("template");
			var newby = Create();
			data.ChartsOC.Add(newby);
			newby.TemplateRA = template;
			newby.BasedOnRA = text;
			return newby;
		}
	}
	#endregion

	#region TextTagFactory class
	internal partial class TextTagFactory
	{
		/// <summary>
		/// Creates a new TextTag object on a text with a possibility item, a beginning point in a text,
		/// and an ending point in a text.
		/// </summary>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		/// <param name="tagPoss"></param>
		/// <returns></returns>
		public ITextTag CreateOnText(AnalysisOccurrence begPoint, AnalysisOccurrence endPoint, ICmPossibility tagPoss)
		{
			if (tagPoss == null)
				throw new ArgumentNullException("tagPoss");
			if (begPoint == null || !begPoint.IsValid)
				throw new ArgumentException("Invalid begPoint.");
			if (endPoint == null || !endPoint.IsValid)
				throw new ArgumentException("Invalid endPoint.");
			var txt = begPoint.Segment.Paragraph.Owner as IStText;
			Debug.Assert(txt != null);
			var newby = Create();
			txt.TagsOC.Add(newby);
			newby.BeginSegmentRA = begPoint.Segment;
			newby.BeginAnalysisIndex = begPoint.Index;
			newby.EndSegmentRA = endPoint.Segment;
			newby.EndAnalysisIndex = endPoint.Index;
			newby.TagRA = tagPoss;
			return newby;
		}
	}
	#endregion

	#region VirtualOrderingFactory class
	internal partial class VirtualOrderingFactory
	{
		public IVirtualOrdering Create(ICmObject parent, string fieldName, IEnumerable<ICmObject> desiredSequence)
		{
			if (!(m_cache.MetaDataCache.GetFieldId2(parent.ClassID, fieldName, true) > 0))
			{
				throw new FDOInvalidFieldException("'flid' is not in the metadata cache.");
			}
			var newby = Create();
			newby.SourceRA = parent;
			newby.Field = fieldName;
			newby.ItemsRS.Replace(0, 0, desiredSequence);
			return newby;
		}
	}
	#endregion

	#region WfiWordformFactory class
	internal partial class WfiWordformFactory
	{
		/// <summary>
		/// Create a WfiWordform with the specified string. It should be a simple
		/// string (use ToWsOnlyString) capable of storing in a MultiUnicode property.
		/// </summary>
		/// <param name="tssForm"></param>
		/// <returns></returns>
		public IWfiWordform Create(ITsString tssForm)
		{
			var wfNew = Create();
			var wsForm = TsStringUtils.GetWsAtOffset(tssForm, 0);
			wfNew.Form.set_String(wsForm, tssForm);
			return wfNew;
		}
	}
	#endregion

	#region WfiAnalysisFactory class
	internal partial class WfiAnalysisFactory
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="glossFactory">For creating a gloss for the first in analysis.Meanings</param>
		/// <returns></returns>
		public IWfiAnalysis Create(IWfiWordform owner, IWfiGlossFactory glossFactory)
		{
			var waNew = Create();
			owner.AnalysesOC.Add(waNew);
			var newGloss = glossFactory.Create();
			waNew.MeaningsOC.Add(newGloss);
			return waNew;
		}
	}
	#endregion

	#region MoMorphTypeFactory class
	/// <summary>
	/// Add IMoMorphTypeFactoryInternal impl.
	/// </summary>
	internal partial class MoMorphTypeFactory : IMoMorphTypeFactoryInternal
	{
		/// <summary>
		/// Create a new IMoMorphType instance with the given parameters.
		/// This will add the new IMoMorphType to the owning list at the given index
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning list. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <param name="name"></param>
		/// <param name="nameWs"></param>
		/// <param name="abbreviation"></param>
		/// <param name="abbreviationWs"></param>
		/// <param name="prefix"></param>
		/// <param name="postfix"></param>
		/// <param name="secondaryOrder"></param>
		/// <returns></returns>
		void IMoMorphTypeFactoryInternal.Create(Guid guid, int hvo, ICmPossibilityList owner, int index, ITsString name, int nameWs, ITsString abbreviation, int abbreviationWs, string prefix, string postfix, int secondaryOrder)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (name == null) throw new ArgumentNullException("name");
			if (abbreviation == null) throw new ArgumentNullException("abbreviation");

			var morphType = new MoMorphType(
				m_cache,
				hvo,
				guid);
			owner.PossibilitiesOS.Add(morphType);
			morphType.Prefix = prefix; // May be null.
			morphType.Postfix = postfix; // May be null.
			morphType.SecondaryOrder = secondaryOrder;
			morphType.Name.set_String(nameWs, name);
			morphType.Abbreviation.set_String(abbreviationWs, abbreviation);
			morphType.IsProtected = true; // They are all protected as fas as this factory method is concerned.
		}

		/// <summary>
		/// Create a new IMoMorphType instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		IMoMorphType IMoMorphTypeFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			IMoMorphType mmt;
			if (guid == Guid.Empty)
			{
				mmt = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				mmt = new MoMorphType(m_cache, hvo, guid);
			}
			owner.PossibilitiesOS.Add(mmt);
			return mmt;
		}

		/// <summary>
		/// Create a new IMoMorphType instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		IMoMorphType IMoMorphTypeFactory.Create(Guid guid, IMoMorphType owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			IMoMorphType mmt;
			if (guid == Guid.Empty)
			{
				mmt = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				mmt = new MoMorphType(m_cache, hvo, guid);
			}
			owner.SubPossibilitiesOS.Add(mmt);
			return mmt;
		}
	}
	#endregion

	#region LexEntryTypeFactory class
	/// <summary>
	/// Add ILexEntryTypeFactoryInternal impl.
	/// </summary>
	internal partial class LexEntryTypeFactory : ILexEntryTypeFactoryInternal
	{
		/// <summary>
		/// Create ILexEntryType instance.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="hvo"></param>
		/// <param name="owner"></param>
		/// <param name="index"></param>
		/// <param name="name"></param>
		/// <param name="nameWs"></param>
		/// <param name="abbreviation"></param>
		/// <param name="abbreviationWs"></param>
		/// <returns></returns>
		void ILexEntryTypeFactoryInternal.Create(Guid guid, int hvo, ICmPossibilityList owner, int index, ITsString name, int nameWs, ITsString abbreviation, int abbreviationWs)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (name == null) throw new ArgumentNullException("name");
			if (abbreviation == null) throw new ArgumentNullException("abbreviation");

			var lexEntryType = new LexEntryType(
				m_cache,
				hvo,
				guid);
			owner.PossibilitiesOS.Add(lexEntryType);
			lexEntryType.Name.set_String(nameWs, name);
			lexEntryType.Abbreviation.set_String(abbreviationWs, abbreviation);
			lexEntryType.IsProtected = true; // They are all protected as fas as this factory method is concerned.
		}
	}
	#endregion

	#region CmPersonFactory class
	internal partial class CmPersonFactory
	{
		/// <summary>
		/// Create a new ICmPossibility instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmPerson ICmPersonFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibilityList", "Possibilities", false);

			var retval = new CmPerson(m_cache, hvo, guid);
			owner.PossibilitiesOS.Add(retval);
			return retval;
		}
	}

	#endregion

	internal partial class FsFeatStrucTypeFactory : IFsFeatStrucTypeFactory
	{
		IFsFeatStrucType IFsFeatStrucTypeFactory.Create(Guid guid, IFsFeatureSystem owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("FsFeatureSystem", "Types", false);

			var retval = new FsFeatStrucType(m_cache, hvo, guid);
			owner.TypesOC.Add(retval);
			return retval;
		}
	}

	internal partial class FsClosedFeatureFactory : IFsClosedFeatureFactory
	{
		IFsClosedFeature IFsClosedFeatureFactory.Create(Guid guid, IFsFeatureSystem owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("FsFeatureSystem", "Features", false);

			var retval = new FsClosedFeature(m_cache, hvo, guid);
			owner.FeaturesOC.Add(retval);
			return retval;
		}
	}

	internal partial class FsComplexFeatureFactory : IFsComplexFeatureFactory
	{
		IFsComplexFeature IFsComplexFeatureFactory.Create(Guid guid, IFsFeatureSystem owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("FsFeatureSystem", "Features", false);

			var retval = new FsComplexFeature(m_cache, hvo, guid);
			owner.FeaturesOC.Add(retval);
			return retval;
		}
	}

	internal partial class FsSymFeatValFactory : IFsSymFeatValFactory
	{
		IFsSymFeatVal IFsSymFeatValFactory.Create(Guid guid, IFsClosedFeature owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("FsClosedFeature", "Values", false);

			var retval = new FsSymFeatVal(m_cache, hvo, guid);
			owner.ValuesOC.Add(retval);
			return retval;
		}
	}

	#region CmPossibilityFactory class
	/// <summary>
	/// Add ICmPossibilityFactoryInternal impl.  Also add methods added to ICmPossibilityFactory.
	/// </summary>
	internal partial class CmPossibilityFactory : ICmPossibilityFactoryInternal
	{
		/// <summary>
		/// Create a new ICmPossibility instance with the given parameters.
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning object. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <returns></returns>
		ICmPossibility ICmPossibilityFactoryInternal.Create(Guid guid, int hvo, ICmPossibilityList owner, int index)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			var retval = new CmPossibility(
				m_cache,
				hvo,
				guid);
			owner.PossibilitiesOS.Insert(index, retval);
			return retval;
		}

		ICmPossibility ICmPossibilityFactoryInternal.Create(Guid guid, int hvo, ICmPossibility owner, int index)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			var retval = new CmPossibility(
				m_cache,
				hvo,
				guid);
			owner.SubPossibilitiesOS.Insert(index, retval);
			return retval;
		}

		ICmPossibility ICmPossibilityFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibilityList", "Possibilities", false);

			var retval = new CmPossibility(m_cache, hvo, guid);
			owner.PossibilitiesOS.Add(retval);
			return retval;
		}

		ICmPossibility ICmPossibilityFactory.Create(Guid guid, ICmPossibility owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibility", "SubPossibilities", false);

			var retval = new CmPossibility(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}
	}
	#endregion

	#region CmAnthroItemFactory class
	/// <summary>
	/// Add methods added to ICmAnthroItemFactory.
	/// </summary>
	internal partial class CmAnthroItemFactory
	{
		ICmAnthroItem ICmAnthroItemFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibilityList", "Possibilities", false);

			var retval = new CmAnthroItem(m_cache, hvo, guid);
			owner.PossibilitiesOS.Add(retval);
			return retval;
		}

		ICmAnthroItem ICmAnthroItemFactory.Create(Guid guid, ICmAnthroItem owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibility", "SubPossibilities", false);

			var retval = new CmAnthroItem(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}
	}
	#endregion

	#region CmLocationFactory class
	/// <summary>
	/// Add methods added to ICmLocationFactory.
	/// </summary>
	internal partial class CmLocationFactory
	{
		ICmLocation ICmLocationFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibilityList", "Possibilities", false);

			var retval = new CmLocation(m_cache, hvo, guid);
			owner.PossibilitiesOS.Add(retval);
			return retval;
		}

		ICmLocation ICmLocationFactory.Create(Guid guid, ICmLocation owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibility", "SubPossibilities", false);

			var retval = new CmLocation(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}
	}
	#endregion

	#region CmSemanticDomainFactory class
	/// <summary>
	/// Add methods added to ICmSemanticDomainFactory.
	/// </summary>
	internal partial class CmSemanticDomainFactory
	{
		ICmSemanticDomain ICmSemanticDomainFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibilityList", "Possibilities", false);

			var retval = new CmSemanticDomain(m_cache, hvo, guid);
			owner.PossibilitiesOS.Add(retval);
			return retval;
		}

		ICmSemanticDomain ICmSemanticDomainFactory.Create(Guid guid, ICmSemanticDomain owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			int flid = m_cache.MetaDataCache.GetFieldId("CmPossibility", "SubPossibilities", false);

			var retval = new CmSemanticDomain(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}
	}
	#endregion

	#region PartOfSpeechFactory class
	internal partial class PartOfSpeechFactory
	{
		IPartOfSpeech IPartOfSpeechFactory.Create(Guid guid, ICmPossibilityList owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			IPartOfSpeech pos;
			if (guid == Guid.Empty)
			{
				pos = Create();
			}
			else
			{
				int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
				pos = new PartOfSpeech(m_cache, hvo, guid);
			}
			owner.PossibilitiesOS.Add(pos);
			return pos;
		}

		IPartOfSpeech IPartOfSpeechFactory.Create(Guid guid, IPartOfSpeech owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();

			var retval = new PartOfSpeech(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}
	}
	#endregion

	#region CmAnnotationDefnFactory class
	/// <summary>
	/// Add ICmPossibilityFactoryInternal impl.
	/// </summary>
	internal partial class CmAnnotationDefnFactory : ICmAnnotationDefnFactoryInternal
	{
		ICmAnnotationDefn ICmAnnotationDefnFactory.Create(Guid guid, ICmAnnotationDefn owner)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();

			var retval = new CmAnnotationDefn(m_cache, hvo, guid);
			owner.SubPossibilitiesOS.Add(retval);
			return retval;
		}

		/// <summary>
		/// Create a new ICmPossibility instance with the given parameters.
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning object. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <returns></returns>
		ICmAnnotationDefn ICmAnnotationDefnFactoryInternal.Create(Guid guid, int hvo, ICmPossibilityList owner, int index)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			var retval = new CmAnnotationDefn(
				m_cache,
				hvo,
				guid);
			owner.PossibilitiesOS.Insert(index, retval);
			return retval;
		}

		ICmAnnotationDefn ICmAnnotationDefnFactoryInternal.Create(Guid guid, int hvo, ICmPossibility owner, int index)
		{
			if (owner == null) throw new ArgumentNullException("owner");

			var retval = new CmAnnotationDefn(
				m_cache,
				hvo,
				guid);
			owner.SubPossibilitiesOS.Insert(index, retval);
			return retval;
		}
	}
	#endregion

	#region CmPossibilityListFactory class
	/// <summary>
	/// Add ICmPossibilityListFactoryInternal impl.
	/// </summary>
	internal partial class CmPossibilityListFactory : ICmPossibilityListFactoryInternal
	{
		/// <summary>
		/// Create a new ICmPossibilityList instance with the given parameters.
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <returns></returns>
		ICmPossibilityList ICmPossibilityListFactoryInternal.Create(Guid guid, int hvo)
		{
			return new CmPossibilityList(
				m_cache,
				hvo,
				guid);
		}

		/// <summary>
		/// Create a new unowned (Custom) ICmPossibilityList instance.
		/// Items in these lists are CmCustomItems.
		/// </summary>
		/// <returns></returns>
		public ICmPossibilityList CreateUnowned(string listName, int ws)
		{
			return CreateUnowned(Guid.NewGuid(), listName, ws);
		}

		/// <summary>
		/// Create a new unowned (Custom) ICmPossibilityList instance.
		/// Items in these lists are CmCustomItems.
		/// </summary>
		/// <returns></returns>
		public ICmPossibilityList CreateUnowned(Guid guid, string listName, int ws)
		{
			var servLoc = m_cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();

			var result = new CmPossibilityList(m_cache, dataReader.GetNextRealHvo(), guid);
			result.ItemClsid = CmCustomItemTags.kClassId;
			result.Name.SetUserWritingSystem(listName);
			result.WsSelector = ws;
			return result;
		}
	}
	#endregion

	#region CmAgentFactory class
	/// <summary>
	/// Add ICmAgentFactoryInternal impl.
	/// </summary>
	internal partial class CmAgentFactory : ICmAgentFactoryInternal
	{
		/// <summary>
		/// Create a new ICmAgent instance with the given parameters.
		/// The owner will the the language project (AnalyzingAgents property)
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="isHuman"></param>
		/// <param name="version">Optional bersion information. (May be null.)</param>
		/// <returns></returns>
		ICmAgent ICmAgentFactoryInternal.Create(Guid guid, int hvo, bool isHuman, string version)
		{
			var newAgent = new CmAgent(
				m_cache,
				hvo,
				guid) {Human = isHuman, Version = version};
			return newAgent;
		}
	}
	#endregion

	#region CmTranslationFactory class
	internal partial class CmTranslationFactory
	{
		/// <summary>
		/// Create a well-formed ICmTranslation which has the owner and Type property set.
		/// </summary>
		public ICmTranslation Create(IStTxtPara owner, ICmPossibility translationType)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (translationType == null) throw new ArgumentNullException("translationType");

			return Create(owner.TranslationsOC, translationType);
		}

		/// <summary>
		/// Create a well-formed ICmTranslation which has the owner and Type property set.
		/// </summary>
		public ICmTranslation Create(ILexExampleSentence owner, ICmPossibility translationType)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (translationType == null) throw new ArgumentNullException("translationType");

			return Create(owner.TranslationsOC, translationType);
		}

		/// <summary>
		/// Do the real work for both smart Create methods.
		/// </summary>
		private ICmTranslation Create(ICollection<ICmTranslation> owningVector, ICmPossibility translationType)
		{
			var newbie = new CmTranslation();
			owningVector.Add(newbie);
			newbie.TypeRA = translationType;
			return newbie;
		}
	}
	#endregion

	#region StStyleFactory class
	internal partial class StStyleFactory
	{
		#region Implementation of IStStyleFactory

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style on the specified style list.
		/// </summary>
		/// <param name="styleList">The style list to add the style to</param>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// <param name="isBuiltin">True for a builtin style, otherwise, false.</param>
		/// <returns>The new created (and properly owned style.</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle Create(IFdoOwningCollection<IStStyle> styleList, string name,
			ContextValues context, StructureValues structure, FunctionValues function,
			bool isCharStyle, int userLevel, bool isBuiltin)
		{
			var retval = new StStyle();
			styleList.Add(retval);
			retval.Name = name;
			retval.Context = context;
			retval.Structure = structure;
			retval.Function = function;
			retval.Type = (isCharStyle ? StyleType.kstCharacter : StyleType.kstParagraph);
			retval.UserLevel = userLevel;
			retval.IsBuiltIn = isBuiltin;

			return retval;
		}

		/// <summary>
		/// Create a new style with a fixed guid.
		/// </summary>
		/// <param name="cache">project cache</param>
		/// <param name="guid">the factory set guid</param>
		/// <returns>A style interface</returns>
		public IStStyle Create(FdoCache cache, Guid guid)
		{
			int hvo = ((IDataReader)cache.ServiceLocator.DataSetup).GetNextRealHvo();
			var retval = new StStyle(cache, hvo, guid);
			return retval;
		}
		#endregion
	}
	#endregion

	#region MoStemMsaFactory class
	internal partial class MoStemMsaFactory
	{
		/// <summary>
		/// Create a new MoStemMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		public IMoStemMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa)
		{
			Debug.Assert(sandboxMsa.MsaType == MsaType.kRoot || sandboxMsa.MsaType == MsaType.kStem);

			var stemMsa = new MoStemMsa();
			entry.MorphoSyntaxAnalysesOC.Add(stemMsa);
			if (sandboxMsa.MainPOS != null)
				stemMsa.PartOfSpeechRA = sandboxMsa.MainPOS;

			return stemMsa;
		}
	}
	#endregion

	#region MoDerivAffMsaFactory class
	internal partial class MoDerivAffMsaFactory
	{
		/// <summary>
		/// Create a new MoDerivAffMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		public IMoDerivAffMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa)
		{
			Debug.Assert(sandboxMsa.MsaType == MsaType.kDeriv);

			var derivMsa = new MoDerivAffMsa();
			entry.MorphoSyntaxAnalysesOC.Add(derivMsa);
			if (sandboxMsa.MainPOS != null)
				derivMsa.FromPartOfSpeechRA = sandboxMsa.MainPOS;
			if (sandboxMsa.SecondaryPOS != null)
				derivMsa.ToPartOfSpeechRA = sandboxMsa.SecondaryPOS;

			return derivMsa;
		}
	}
	#endregion

	#region MoInflAffMsaFactory class
	internal partial class MoInflAffMsaFactory
	{
		/// <summary>
		/// Create a new MoInflAffMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		public IMoInflAffMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa)
		{
			Debug.Assert(sandboxMsa.MsaType == MsaType.kInfl);

			var inflMsa = new MoInflAffMsa();
			entry.MorphoSyntaxAnalysesOC.Add(inflMsa);
			if (sandboxMsa.MainPOS != null)
				inflMsa.PartOfSpeechRA = sandboxMsa.MainPOS;
			if (sandboxMsa.Slot != null)
				inflMsa.SlotsRC.Add(sandboxMsa.Slot);

			return inflMsa;
		}
	}
	#endregion

	#region MoUnclassifiedAffixMsaFactory class
	internal partial class MoUnclassifiedAffixMsaFactory
	{
		/// <summary>
		/// Create a new MoUnclassifiedAffixMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		public IMoUnclassifiedAffixMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa)
		{
			Debug.Assert(sandboxMsa.MsaType == MsaType.kUnclassified);

			var uncMsa = new MoUnclassifiedAffixMsa();
			entry.MorphoSyntaxAnalysesOC.Add(uncMsa);
			if (sandboxMsa.MainPOS != null)
				uncMsa.PartOfSpeechRA = sandboxMsa.MainPOS;

			return uncMsa;
		}
	}
	#endregion

	#region CmPictureFactory class
	internal partial class CmPictureFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ICmPicture Create(string sTextRepOfPicture, string sFolder)
		{
			return Create(sTextRepOfPicture, sFolder, 0, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The picture location parser (can be null).</param>
		/// ------------------------------------------------------------------------------------
		public ICmPicture Create(string sTextRepOfPicture, string sFolder,
			int anchorLoc, IPictureLocationBridge locationParser)
		{
			string[] tokens = sTextRepOfPicture.Split('|');
			if (!CmPictureServices.ValidTextRepOfPicture(tokens))
				throw new ArgumentException("The clipboard format for a Picture was invalid");
			string sDescription = tokens[1];
			string srcFilename = tokens[2];
			string sLayoutPos = tokens[3];
			string sLocationRange = tokens[4];
			string sCopyright = tokens[5];
			string sCaption = tokens[6];
			string sLocationRangeType = tokens[7];
			string sScaleFactor = tokens[8];

			PictureLocationRangeType locRangeType = ParseLocationRangeType(sLocationRangeType);

			return Create(sFolder, anchorLoc, locationParser, sDescription,
				srcFilename, sLayoutPos, sLocationRange, sCopyright, sCaption,
				locRangeType, sScaleFactor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a CmPicture for the given file, having the given caption, and located in
		/// the given folder.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ICmPicture Create(string srcFilename, ITsString captionTss, string sFolder)
		{
			return Create(srcFilename, captionTss, null, PictureLayoutPosition.CenterInColumn,
				100, PictureLocationRangeType.AfterAnchor, 0, 0, null, sFolder, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a Toolbox-style Standard Format import.
		/// </summary>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that the locationParser can use if
		/// necessary (can be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="descriptions">The descriptions in 0 or more writing systems.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="tssCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		public ICmPicture Create(string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, Dictionary<int, string> descriptions,
			string srcFilename, string sLayoutPos, string sLocationRange, string sCopyright,
			ITsString tssCaption, PictureLocationRangeType locRangeType, string sScaleFactor)
		{
			ICmPicture pic = Create(srcFilename, tssCaption, null, sLayoutPos, sScaleFactor,
				locRangeType, anchorLoc, locationParser, sLocationRange, sCopyright, sFolder);
			if (descriptions != null)
			{
				foreach (int ws in descriptions.Keys)
					pic.Description.set_String(ws, descriptions[ws]);
			}
			return pic;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a USFM-style Standard Format import.
		/// </summary>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="sDescription">Illustration description in English.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="sCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		public ICmPicture Create(string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, string sDescription, string srcFilename,
			string sLayoutPos, string sLocationRange, string sCopyright, string sCaption,
			PictureLocationRangeType locRangeType, string sScaleFactor)
		{
			return Create(srcFilename, m_cache.TsStrFactory.MakeString(sCaption, m_cache.DefaultVernWs),
				sDescription, sLayoutPos, sScaleFactor, locRangeType, anchorLoc, locationParser,
				sLocationRange, sCopyright, sFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up. Put the caption in the default vernacular writing system.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption (in the default vernacular Writing System)</param>
		/// <param name="description">Illustration description in English. This is not published.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// <param name="locRangeType">Indicates the type of data contained in LocationMin
		/// and LocationMax.</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="copyright">Publishable information about the copyright that should
		/// appear on the copyright page of the publication.</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		private ICmPicture Create(string srcFilename, ITsString captionTss,
			string description, string sLayoutPos, string sScaleFactor,
			PictureLocationRangeType locRangeType, int anchorLoc, IPictureLocationBridge locationParser,
			string sLocationRange, string copyright, string sFolder)
		{
			int locationMin, locationMax;
			if (locationParser != null)
			{
				locationParser.ParsePictureLoc(sLocationRange, anchorLoc, ref locRangeType,
					out locationMin, out locationMax);
			}
			else
			{
				ParsePictureLoc(sLocationRange, ref locRangeType, out locationMin, out locationMax);
			}

			return Create(srcFilename, captionTss, description, ParseLayoutPosition(sLayoutPos),
				ParseScaleFactor(sScaleFactor), locRangeType, locationMin, locationMax,
				copyright, sFolder, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption (in the given Writing System)</param>
		/// <param name="description">Illustration description in English. This is not
		/// published.</param>
		/// <param name="layoutPos">Indication of where in the column/page the picture is to be
		/// laid out.</param>
		/// <param name="scaleFactor">Integral percentage by which picture is grown or shrunk.</param>
		/// <param name="locationRangeType">Indicates the type of data contained in LocationMin
		/// and LocationMax.</param>
		/// <param name="locationMin">The minimum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="locationMax">The maximum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="copyright">Publishable information about the copyright that should
		/// appear on the copyright page of the publication.</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// <param name="ws">The WS of the caption and copyright</param>
		/// ------------------------------------------------------------------------------------
		private ICmPicture Create(string srcFilename, ITsString captionTss, string description,
			PictureLayoutPosition layoutPos, int scaleFactor,
			PictureLocationRangeType locationRangeType, int locationMin, int locationMax,
			string copyright, string sFolder, int ws)
		{
			ICmPicture pic = Create();
			pic.UpdatePicture(srcFilename, captionTss, sFolder, ws);
			int wsEn = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			if (!String.IsNullOrEmpty(description) && wsEn > 0)
				pic.Description.set_String(wsEn, description);
			pic.LayoutPos = layoutPos;
			pic.ScaleFactor = scaleFactor;
			pic.LocationRangeType = locationRangeType;
			pic.LocationMin = locationMin;
			pic.LocationMax = locationMax;
			if (!string.IsNullOrEmpty(copyright))
				pic.PictureFileRA.Copyright.set_String(ws, copyright);
			return pic;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the string representing the type of the location range.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLocationRangeType.AfterAnchor if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private PictureLocationRangeType ParseLocationRangeType(string token)
		{
			try
			{
				return (PictureLocationRangeType)Enum.Parse(typeof(PictureLocationRangeType), token);
			}
			catch (ArgumentException e)
			{
				Logger.WriteError(e);
				return PictureLocationRangeType.AfterAnchor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to parse the given token as a layout position string.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLayoutPosition.CenterInColumn if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private static PictureLayoutPosition ParseLayoutPosition(string token)
		{
			switch (token)
			{
				case "col":
					return PictureLayoutPosition.CenterInColumn;
				case "span":
					return PictureLayoutPosition.CenterOnPage;
				case "right":
					return PictureLayoutPosition.RightAlignInColumn;
				case "left":
					return PictureLayoutPosition.LeftAlignInColumn;
				case "fillcol":
					return PictureLayoutPosition.FillColumnWidth;
				case "fillspan":
					return PictureLayoutPosition.FillPageWidth;
				case "fullpage":
					return PictureLayoutPosition.FullPage;
				default:
					try
					{
						return (PictureLayoutPosition)Enum.Parse(typeof(PictureLayoutPosition), token);
					}
					catch (ArgumentException e)
					{
						Logger.WriteError(e);
						return PictureLayoutPosition.CenterInColumn;
					}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to parse the given token as a layout position string.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLayoutPosition.CenterInColumn if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private static int ParseScaleFactor(string token)
		{
			if (string.IsNullOrEmpty(token))
				return 100;

			int scaleFactor = 0;
			foreach (char ch in token)
			{
				int value = CharUnicodeInfo.GetDigitValue(ch);
				if (value >= 0 && scaleFactor <= 100)
					scaleFactor = (scaleFactor == 0) ? value : scaleFactor * 10 + value;
				else if (scaleFactor > 0)
					break;
			}
			if (scaleFactor == 0)
			{
				if (!String.IsNullOrEmpty(token))
					Logger.WriteEvent("Unexpected CmPicture Scale value: " + token);
				scaleFactor = 100;
			}
			else
				scaleFactor = Math.Min(scaleFactor, 1000);
			return scaleFactor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the picture location range string.
		/// </summary>
		/// <param name="s">The string representation of the picture location range.</param>
		/// <param name="locType">The type of the location range. The incoming value tells us
		/// the assumed type for parsing. The out value can be set to a different type if we
		/// discover that the actual value is another type.</param>
		/// <param name="locationMin">The location min.</param>
		/// <param name="locationMax">The location max.</param>
		/// ------------------------------------------------------------------------------------
		private static void ParsePictureLoc(string s, ref PictureLocationRangeType locType,
			out int locationMin, out int locationMax)
		{
			locationMin = locationMax = 0;
			switch (locType)
			{
				case PictureLocationRangeType.AfterAnchor:
					return;
				case PictureLocationRangeType.ReferenceRange:
					// Range is generated internally in the form BCCCVVV-BCCCVVV
					int index = s.IndexOf('-');
					if (s.Length <= 15 && index > 0 &&
						Int32.TryParse(s.Substring(0, index), out locationMin) &&
						Int32.TryParse(s.Substring(index + 1), out locationMax))
					{
						// sucessful parse!
						return;
					}
					locationMin = locationMax = 0;
					locType = PictureLocationRangeType.AfterAnchor;
					return;
				case PictureLocationRangeType.ParagraphRange:
					if (String.IsNullOrEmpty(s))
					{
						locType = PictureLocationRangeType.AfterAnchor;
						return;
					}
					string[] pieces = s.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
					if (pieces.Length == 2)
					{
						if (Int32.TryParse(pieces[0], out locationMin))
							if (Int32.TryParse(pieces[1], out locationMax))
								return;
					}
					locType = PictureLocationRangeType.AfterAnchor;
					return;
			}
		}
	}
	#endregion

	#region ScrFootnoteFactory class
	internal partial class ScrFootnoteFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new StFootnote owned by the given book created from the given string
		/// representation (Created from GetTextRepresentation())
		/// </summary>
		/// <param name="book">The book that owns the sequence of footnotes into which the
		/// new footnote is to be inserted</param>
		/// <param name="sTextRepOfFootnote">The given string representation of a footnote
		/// </param>
		/// <param name="footnoteIndex">0-based index where the footnote will be inserted</param>
		/// <param name="footnoteMarkerStyleName">style name for footnote markers</param>
		/// <returns>A ScrFootnote with the properties set to the properties in the
		/// given string representation</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public IScrFootnote CreateFromStringRep(IScrBook book, string sTextRepOfFootnote,
			int footnoteIndex, string footnoteMarkerStyleName)
		{
			IScrFootnote createdFootnote = new ScrFootnote();
			book.FootnotesOS.Insert(footnoteIndex, createdFootnote);

			// create an XML reader to read in the string representation
			using (var reader = new StringReader(sTextRepOfFootnote))
			{
				XmlDocument doc = new XmlDocument();
				try
				{
					doc.Load(reader);
				}
				catch (XmlException)
				{
					throw new ArgumentException("Unrecognized XML format for footnote.");
				}

				XmlNodeList tagList = doc.SelectNodes("FN");

				foreach (XmlNode bla in tagList[0].ChildNodes)
				{
					// Footnote marker
					if (bla.Name == "M")
					{
						createdFootnote.FootnoteMarker = TsStringUtils.MakeTss(bla.InnerText,
							m_cache.DefaultVernWs, footnoteMarkerStyleName);
					}
					// start of a paragraph
					else if (bla.Name == "P")
					{
						IScrTxtPara newPara = m_cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
							createdFootnote, ScrStyleNames.NormalFootnoteParagraph);
						ITsIncStrBldr paraBldr = TsIncStrBldrClass.Create();
						ICmTranslation trans = null;
						ILgWritingSystemFactory wsf = book.Cache.WritingSystemFactory;
						foreach (XmlNode paraTextNode in bla.ChildNodes)
						{
							if (paraTextNode.Name == "PS")
							{
								// paragraph style
								if (!String.IsNullOrEmpty(paraTextNode.InnerText))
									newPara.StyleName = paraTextNode.InnerText;
								else
								{
									Debug.Fail("Attempting to create a footnote paragraph with no paragraph style specified!");
								}
							}
							else if (paraTextNode.Name == "RUN")
							{
								CreateRunFromStringRep(wsf, paraBldr, paraTextNode);
								paraBldr.Append(paraTextNode.InnerText);
							}
							else if (paraTextNode.Name == "TRANS")
							{
								if (trans == null)
									trans = newPara.GetOrCreateBT();

								// Determine which writing system where the string run(s) will be added.
								string iculocale = paraTextNode.Attributes.GetNamedItem("WS").Value;
								if (string.IsNullOrEmpty(iculocale))
									throw new ArgumentException("Unknown ICU locale encountered: " + iculocale);

								int transWS = wsf.GetWsFromStr(iculocale);
								Debug.Assert(transWS != 0, "Unable to find ws from ICU Locale");

								// Build a TsString from the run(s) description.
								ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
								foreach (XmlNode transTextNode in paraTextNode.ChildNodes)
								{
									if (transTextNode.Name != "RUN")
									{
										throw new ArgumentException("Unexpected translation element '" +
											transTextNode.Name + "' encountered for ws '" + iculocale + "'");
									}

									CreateRunFromStringRep(wsf, strBldr, transTextNode);
									strBldr.Append(transTextNode.InnerText);
								}

								trans.Translation.set_String(transWS, strBldr.GetString());
							}
						}
						newPara.Contents = paraBldr.GetString();
					}
				}
				// We shouldn't need to do a PropChanged in the new FDO
				//owner.Cache.DomainDataByFlid.PropChanged(null, (int)PropChangeType.kpctNotifyAll, owner.Hvo,
				//    flid, footnoteIndex, 1, 0);

				return createdFootnote;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the a text run from a string representation.
		/// </summary>
		/// <param name="wsf">The writing system factory.</param>
		/// <param name="strBldr">The structured string builder.</param>
		/// <param name="textNode">The text node which describes runs to be added to the
		/// paragraph or to the translation for a particular writing system</param>
		/// ------------------------------------------------------------------------------------
		private void CreateRunFromStringRep(ILgWritingSystemFactory wsf, ITsIncStrBldr strBldr,
			XmlNode textNode)
		{
			XmlNode charStyle = textNode.Attributes.GetNamedItem("CS");
			if (charStyle != null)
				strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, charStyle.Value);
			else
				strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);

			XmlNode wsICULocale = textNode.Attributes.GetNamedItem("WS");
			if (wsICULocale != null)
			{
				int ws = wsf.GetWsFromStr(wsICULocale.Value);
				if (ws <= 0)
					throw new ArgumentException("Unknown ICU locale encountered: '" + wsICULocale.Value + "'");
				strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, wsf.GetWsFromStr(wsICULocale.Value));
			}
			else
				throw new ArgumentException("Required attribute WS missing from RUN element.");
		}
	}
	#endregion

	#region ScrDraftFactory class
	internal partial class ScrDraftFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an empty saved version (containing no books)
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// ------------------------------------------------------------------------------------
		public IScrDraft Create(string description)
		{
			ScrDraft version = new ScrDraft();
			m_cache.ServiceLocator.GetInstance<IScriptureRepository>().Singleton.ArchivedDraftsOC.Add(version);
			version.Description = description;
			return version;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an empty version (containing no books)
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// <param name="type">The type of version to create (saved or imported)</param>
		/// ------------------------------------------------------------------------------------
		public IScrDraft Create(string description, ScrDraftType type)
		{
			IScrDraft version = Create(description);
			version.Type = type;
			return version;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version, adding copies of the specified books.
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// <param name="books">Books that are copied to the saved version</param>
		/// ------------------------------------------------------------------------------------
		public IScrDraft Create(string description, IEnumerable<IScrBook> books)
		{
			IScrDraft version = Create(description);
			foreach (IScrBook book in books)
				version.AddBookCopy(book);
			return version;
		}
	}
	#endregion

	#region ScrBookFactory class
	internal partial class ScrBookFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the <see cref="IScripture"/>. Also creates a new StText for the ScrBook's Title
		/// property.
		/// </summary>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <param name="title">The title StText created for the new book</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the database</exception>
		/// ------------------------------------------------------------------------------------
		public IScrBook Create(int bookNumber, out IStText title)
		{
			return Create(m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS,
				bookNumber, out title);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the given sequence. Also creates a new StText for the ScrBook's Title
		/// property.
		/// </summary>
		/// <param name="booksOS">Owning sequence of books to add the new book to</param>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <param name="title">The title StText created for the new book</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the given sequence</exception>
		/// ------------------------------------------------------------------------------------
		public IScrBook Create(IFdoOwningSequence<IScrBook> booksOS, int bookNumber,
			out IStText title)
		{
			IScrBook book = Create(booksOS, bookNumber);
			book.TitleOA = title = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the <see cref="IScripture"/>.
		/// </summary>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the database</exception>
		/// ------------------------------------------------------------------------------------
		public IScrBook Create(int bookNumber)
		{
			return Create(m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS, bookNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the given sequence.
		/// </summary>
		/// <param name="booksOS">Owning sequence of books to add the new book to</param>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the given sequence</exception>
		/// ------------------------------------------------------------------------------------
		public IScrBook Create(IFdoOwningSequence<IScrBook> booksOS, int bookNumber)
		{
			int iBook = 0;
			foreach (var existingBook in booksOS)
			{
				if (existingBook.CanonicalNum == bookNumber)
					throw new InvalidOperationException("Attempting to create a Scripture book that already exists.");

				if (existingBook.CanonicalNum > bookNumber)
					break;
				iBook++;
			}

			IScrBook book = new ScrBook();
			booksOS.Insert(iBook, book);
			book.CanonicalNum = bookNumber;
			book.BookIdRA = m_cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton.BooksOS[bookNumber - 1];
			book.Name.CopyAlternatives(book.BookIdRA.BookName);
			book.Abbrev.CopyAlternatives(book.BookIdRA.BookAbbrev);
			return book;
		}
	}
	#endregion

	#region ScrTxtParaFactory class
	internal partial class ScrTxtParaFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new ScrTxtPara with the specified style.
		/// </summary>
		/// <param name="owner">The owner for the created paragraph.</param>
		/// <param name="styleName">Name of the style to apply to the paragraph style rules.</param>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara CreateWithStyle(IStText owner, string styleName)
		{
			return CreateWithStyle(owner, owner.ParagraphsOS.Count, styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new ScrTxtPara with the specified style.
		/// </summary>
		/// <param name="owner">The owner for the created paragraph.</param>
		/// <param name="iPos">The index where the new paragraph should be inserted.</param>
		/// <param name="styleName">Name of the style to apply to the paragraph style rules.</param>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara CreateWithStyle(IStText owner, int iPos, string styleName)
		{
			if (string.IsNullOrEmpty(styleName))
				throw new ArgumentException("styleName can not be null or an empty string", "styleName");

			IScrTxtPara newPara = (IScrTxtPara)CreateInternal();
			owner.ParagraphsOS.Insert(iPos, newPara);
			newPara.GetOrCreateBT(); // Make sure the CmTranslation is created.
			newPara.StyleRules = StyleUtils.ParaStyleTextProps(styleName);
			newPara.Contents = newPara.Cache.TsStrFactory.EmptyString(newPara.Cache.DefaultVernWs);
			return newPara;
		}
	}
	#endregion

	#region ScrRefSystemFactory class
	internal partial class ScrRefSystemFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Basic creation method for a ScrRefSystem.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrRefSystem Create()
		{
			if (m_cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton != null)
				throw new InvalidOperationException("Can not create more than one ScrRefSystem");
			ScrRefSystem newby = new ScrRefSystem();
			((ICmObjectInternal)newby).InitializeNewOwnerlessCmObject(m_cache);
			newby.Initialize();
			return newby;
		}
	}
	#endregion

	#region ScriptureFactory class
	internal partial class ScriptureFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Basic creation method for Scripture.
		/// </summary>
		/// <returns>A new Scripture object, owned by the Language Project.</returns>
		/// <exception cref="InvalidOperationException">If Scripture already exists</exception>
		/// ------------------------------------------------------------------------------------
		public IScripture Create()
		{
			ILangProject lp = m_cache.LanguageProject;
			if (lp.TranslatedScriptureOA != null)
				throw new InvalidOperationException("Scripture already exists!");

			IScripture scr = lp.TranslatedScriptureOA = new Scripture();
			var scrRefSys = m_cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton;
			if (scrRefSys != null && scrRefSys.BooksOS.Count == 0)
			{
				// This partially created object could exist if the project was created in FLEx in FW 6.0
				// and the project was never opened in TE.
				((ScrRefSystem)scrRefSys).Initialize();
			}
			else if (scrRefSys == null)
				m_cache.ServiceLocator.GetInstance<IScrRefSystemFactory>().Create();

			// REVIEW TomB: Do we want the default values for the following four properties to come
			// from a resource file so they can be localized?
			scr.RefSepr = ";";
			scr.ChapterVerseSepr = ":";
			scr.VerseSepr = ",";
			scr.Bridge = "-";
			scr.Versification = ScrVers.English;
			// Initialize footnote sequence options
			scr.RestartFootnoteSequence = true;
			scr.CrossRefsCombinedWithFootnotes = false;
			scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			scr.FootnoteMarkerSymbol = ScriptureTags.kDefaultFootnoteMarkerSymbol;
			scr.DisplayFootnoteReference = false;
			scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			scr.CrossRefMarkerSymbol = ScriptureTags.kDefaultFootnoteMarkerSymbol;
			scr.DisplayCrossRefReference = true;

			// Create a place to hold annotations for every possible Scripture book
			var bookAnnotationsFactory = m_cache.ServiceLocator.GetInstance<IScrBookAnnotationsFactory>();
			for (int iBookNum = 0; iBookNum < BCVRef.LastBook; iBookNum++)
				scr.BookAnnotationsOS.Add(bookAnnotationsFactory.Create());

			// Indicate that certain clean-up actions have been taken. (Since this
			// is a brand-new instance of Scripture, no clean up is needed.)
			((Scripture)scr).FixedOrphanedFootnotes = true;
			((Scripture)scr).ResegmentedParasWithOrcs = true;
			((Scripture)scr).FixedParasWithoutBt = true;
			((Scripture)scr).FixedParasWithoutSegments = true;
			((Scripture)scr).RemovedOldKeyTermsList = true;
			((Scripture)scr).FixedStylesInUse = true;

			return scr;
		}

	}
	#endregion

	#region ScrSectionFactory class
	internal partial class ScrSectionFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>
		/// The contents of the first content paragraph are filled with a single run as
		/// requested. The start and end references for the section are set based on where it's
		/// being inserted in the book.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="contentText">The text to be used as the first para in the new section
		/// content</param>
		/// <param name="contentTextProps">The character properties to be applied to the first
		/// para in the new section content</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>Created section</returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection CreateScrSection(IScrBook book, int iSection, string contentText,
			ITsTextProps contentTextProps, bool isIntro)
		{
			if (book == null)
				throw new ArgumentNullException();

			IScrSection section = CreateSection(book, iSection, isIntro, true, false);

			// Insert the section contents.
			StTxtParaBldr bldr = new StTxtParaBldr(book.Cache);
				bldr.ParaStyleName = isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
				bldr.AppendRun(contentText, contentTextProps);
				bldr.CreateParagraph(section.ContentOA);

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a section with optional heading/content paragraphs.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <param name="createHeadingPara">if true, heading paragraph will be created</param>
		/// <param name="createContentPara">if true, content paragraph will be created</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection CreateSection(IScrBook book, int iSection, bool isIntro,
			bool createHeadingPara, bool createContentPara)
		{
			if (book == null)
				throw new ArgumentNullException();

			// Create an empty section. The end reference needs to be set to indicate to
			// AdjustReferences if the section is an intro section
			IScrSection section = CreateEmptySection(book, iSection);
			section.VerseRefEnd = new ScrReference(book.CanonicalNum, 1, isIntro ? 0 : 1,
				book.Cache.LanguageProject.TranslatedScriptureOA.Versification);

			int defVernWS = m_cache.DefaultVernWs;
			// Add an empty paragraph to the section head.
			if (createHeadingPara)
			{
				string paraStyle = isIntro ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead;
				StTxtParaBldr.CreateEmptyPara(book.Cache, section.HeadingOA, paraStyle, defVernWS);
			}

			// Add an empty paragraph to the section content.
			if (createContentPara)
			{
				string paraStyle = isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
				StTxtParaBldr.CreateEmptyPara(book.Cache, section.ContentOA, paraStyle, defVernWS);
			}

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. Since the StTexts are empty,
		/// this version of the function is generic (i.e. the new section may be made either
		/// an intro section or a scripture text section by the calling code).
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection CreateEmptySection(IScrBook book, int iSection)
		{
			if (book == null)
				throw new ArgumentNullException();
			if (iSection < 0 || iSection > book.SectionsOS.Count)
				throw new ArgumentException();

			// Now insert the section in the book at the specified location.
			IScrSection section = m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Insert(iSection, section);

			// Insert StTexts for section heading and section content.
			IStTextFactory textFact = m_cache.ServiceLocator.GetInstance<IStTextFactory>();
			section.HeadingOA = textFact.Create();
			section.ContentOA = textFact.Create();

			return section;
		}
	}
	#endregion

	#region RnGenericRecFactory class
	internal partial class RnGenericRecFactory
	{
		/// <summary>
		/// Creates a new record with the specified notebook as the owner.
		/// </summary>
		/// <param name="notebook">The notebook.</param>
		/// <param name="title">The title.</param>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IRnGenericRec Create(IRnResearchNbk notebook, ITsString title, ICmPossibility type)
		{
			return Create(notebook.RecordsOC, title, type);
		}

		/// <summary>
		/// Creates a new record with the specified record as the owner.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="title">The title.</param>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IRnGenericRec Create(IRnGenericRec record, ITsString title, ICmPossibility type)
		{
			return Create(record.SubRecordsOS, title, type);
		}

		private IRnGenericRec Create(ICollection<IRnGenericRec> owningCollection, ITsString title, ICmPossibility type)
		{
			var newRecord = new RnGenericRec();
			owningCollection.Add(newRecord);
			newRecord.TypeRA = type;
			newRecord.Title = title;
			newRecord.ParticipantsOC.Add(m_cache.ServiceLocator.GetInstance<IRnRoledParticFactory>().Create());
			var textFactory = m_cache.ServiceLocator.GetInstance<IStTextFactory>();
			newRecord.ConclusionsOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.ConclusionsOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.DescriptionOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.DescriptionOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.DiscussionOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.DiscussionOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.ExternalMaterialsOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.ExternalMaterialsOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.FurtherQuestionsOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.FurtherQuestionsOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.HypothesisOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.HypothesisOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.PersonalNotesOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.PersonalNotesOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.ResearchPlanOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.ResearchPlanOA, "Normal", m_cache.DefaultAnalWs);
			newRecord.VersionHistoryOA = textFactory.Create();
			StTxtParaBldr.CreateEmptyPara(m_cache, newRecord.VersionHistoryOA, "Normal", m_cache.DefaultAnalWs);
			return newRecord;
		}
	}
	#endregion
	#region CmMediaURI
	internal partial class CmMediaURIFactory : ICmMediaURIFactory, IFdoFactoryInternal
	{
		/// <summary>
		/// Basic creation method for an CmMediaURI.
		/// </summary>
		/// <returns>A new, unowned CmMediaURI with the given guid</returns>
		public ICmMediaURI Create(FdoCache cache, Guid guid)
		{
			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			var newby = new CmMediaURI(cache, hvo, guid);
			if (newby.OwnershipStatus != ClassOwnershipStatus.kOwnerRequired)
				((ICmObjectInternal)newby).InitializeNewOwnerlessCmObject(m_cache);
			return newby;
		}
	}
	#endregion
	#region Fdo.Text
	internal partial class TextFactory : ITextFactory, IFdoFactoryInternal
	{
		/// <summary>
		/// Basic creation method for a Text.
		/// </summary>
		/// <returns>A new, unowned Text with the given guid</returns>
		public IText Create(FdoCache cache, Guid guid)
		{
			int hvo = ((IDataReader)m_cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			var newby = new Text(cache, hvo, guid);
			((ICmObjectInternal) newby).InitializeNewOwnerlessCmObjectWithPresetGuid();
			return newby;
		}
	}
	#endregion
}
