// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoTypeSafeCollections.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
// <remarks>
// Implementation of:
//
// FdoCollectionBase : System.Collections.CollectionBase
//		UserViewCollection : FdoCollectionBase
//		LgWritingSystemCollection : FdoCollectionBase
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.FDO
{
	#region Base class for all FDO type-safe collections
	/// <summary>
	/// A base collection class for type-safe CmObject objects.
	/// </summary>
	/// <remarks>
	/// Collections derived from this class do not have the same smarts in them
	/// to handle adding or removing from the database, as have the collections derived from
	/// FdoCollection. This is because they are mostly unowned objects, and can get special treatment,
	/// when new ones are added.
	/// </remarks>
	public abstract class FdoCollectionBase : CollectionBase
	{
		/// <summary></summary>
		protected FdoCache m_fdoCache;

		/// <summary>
		/// Constructor.
		/// </summary>
		public FdoCollectionBase(FdoCache fdoCache)
		{
			m_fdoCache = fdoCache;
		}

		/// <summary>
		/// The hvo ids of our collection.
		/// </summary>
		public int[] HvoArray
		{
			get
			{
				List<int> hvoList = new List<int>();
				foreach (ICmObject co in List)
				{
					hvoList.Add(co.Hvo);
				}
				return hvoList.ToArray();
			}
		}

		/// <summary>
		/// Validate the object, before it goes into the collection,
		/// as it may not be in the DB yet.
		/// </summary>
		/// <param name="obj">Object to validate.</param>
		/// <returns>The same object as was being validated, but with a good ID.</returns>
		protected ICmObject ValidateObject(ICmObject obj)
		{
			Debug.Assert(m_fdoCache != null);
			if (obj.Hvo == (int)CmObject.SpecialHVOValues.kHvoOwnerPending)
				(obj as CmObject).InitNew(m_fdoCache);
			return obj;
		}
	}

	#endregion // Base class for all FDO type-safe collections

	#region UserViewCollection

	/// <summary>
	/// A collection class for UserView objects.
	/// </summary>
	public class UserViewCollection : FdoCollectionBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public UserViewCollection(FdoCache fdoCache)
			: base(fdoCache) {}

		/// <summary>
		/// Add a UserView to the collection.
		/// </summary>
		/// <param name="uv">The UserView to add.</param>
		public IUserView Add(IUserView uv)
		{
			Debug.Assert(uv != null);
			IUserView uvAdd = (IUserView)ValidateObject(uv);
			List.Add(uvAdd);
			return uvAdd;
		}


		/// <summary>
		/// Remove the UserView at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the UserView at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The UserView at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public IUserView Item(int index)
		{
			return (IUserView)List[index];
		}

		/// <summary>
		/// Get a collection of UserView objects with the given application Guid.
		/// </summary>
		/// <param name="app">Guid for an application.</param>
		/// <returns>
		/// UserViewCollection which contains UserView objects that match the
		/// given Guid, if any.
		/// </returns>
		public UserViewCollection GetUserViews(Guid app)
		{
			UserViewCollection uvc = new UserViewCollection(m_fdoCache);
			for(int i = 0; i < List.Count; i++)
			{
				IUserView uv = Item(i);
				if (uv.App == app)
					uvc.Add(uv);
			}
			return uvc;
		}

	}

	#endregion // UserViewCollection

	#region LgWritingSystemCollection

	/// <summary>
	/// A collection class for LgWritingSystem objects.
	/// </summary>
	public class LgWritingSystemCollection : FdoCollectionBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public LgWritingSystemCollection(FdoCache fdoCache)
			: base(fdoCache) {}

		/// <summary>
		/// Add a LgWritingSystem to the collection.
		/// </summary>
		/// <param name="lws">The LgWritingSystem to add.</param>
		public ILgWritingSystem Add(ILgWritingSystem lws)
		{
			Debug.Assert(lws != null);
			ILgWritingSystem leAdd = (ILgWritingSystem)ValidateObject(lws);
			List.Add(leAdd);
			return leAdd;
		}


		/// <summary>
		/// Remove the LgWritingSystem at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the LgWritingSystem at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The LgWritingSystem at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public ILgWritingSystem Item(int index)
		{
			return (ILgWritingSystem)List[index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks up the ICU locale and returns the corresponding writing system code
		/// </summary>
		/// <param name="sICULocale">The ICU locale name</param>
		/// <returns>The Writing System code</returns>
		/// ------------------------------------------------------------------------------------
		public int GetWsFromIcuLocale(string sICULocale)
		{
			foreach (ILgWritingSystem lgws in this)
			{
				if (LanguageDefinition.SameLocale(lgws.ICULocale, sICULocale))
					return lgws.Hvo;
			}
			return 0; // Couldn't find it.
		}
	}

	#endregion // LgWritingSystemCollection

	#region CmPossibilityListCollection

	/// <summary>
	/// A collection class for CmPossibilityList objects.
	/// </summary>
	public class CmPossibilityListCollection : FdoCollectionBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public CmPossibilityListCollection(FdoCache fdoCache)
			: base(fdoCache) {}

		/// <summary>
		/// Add a CmPossibilityList to the collection.
		/// </summary>
		/// <param name="pl">The CmPossibilityList to add.</param>
		public ICmPossibilityList Add(ICmPossibilityList pl)
		{
			Debug.Assert(pl != null);
			ICmPossibilityList plAdd = (ICmPossibilityList)ValidateObject(pl);
			List.Add(plAdd);
			return plAdd;
		}


		/// <summary>
		/// Remove the CmPossibilityList at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the CmPossibilityList at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The CmPossibilityList at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public ICmPossibilityList Item(int index)
		{
			return (ICmPossibilityList)List[index];
		}
	}

	#endregion // CmPossibilityListCollection

	#region MoInflClassCollection

	/// <summary>
	/// A collection class for MoInflClass objects.
	/// </summary>
	public class MoInflClassCollection : FdoCollectionBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MoInflClassCollection(FdoCache fdoCache)
			: base(fdoCache) { }

		/// <summary>
		/// Add a MoInflClass to the collection.
		/// </summary>
		/// <param name="ic">The MoInflClass to add.</param>
		public IMoInflClass Add(IMoInflClass ic)
		{
			Debug.Assert(ic != null);
			IMoInflClass icAdd = (IMoInflClass)ValidateObject(ic);
			List.Add(icAdd);
			return icAdd;
		}


		/// <summary>
		/// Remove the MoInflClass at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the MoInflClass at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The MoInflClass at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public IMoInflClass Item(int index)
		{
			return (IMoInflClass)List[index];
		}
	}
	#endregion // MoInflClassCollection

	#region MoStemNameCollection

	/// <summary>
	/// A collection class for MoStemName objects.
	/// </summary>
	public class MoStemNameCollection : FdoCollectionBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MoStemNameCollection(FdoCache fdoCache)
			: base(fdoCache) { }

		/// <summary>
		/// Add a MoStemName to the collection.
		/// </summary>
		/// <param name="sn">The MoStemName to add.</param>
		public IMoStemName Add(IMoStemName sn)
		{
			Debug.Assert(sn != null);
			IMoStemName snAdd = (IMoStemName)ValidateObject(sn);
			List.Add(snAdd);
			return snAdd;
		}


		/// <summary>
		/// Remove the MoStemName at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}


		/// <summary>
		/// Get the MoStemName at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The MoStemName at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public IMoStemName Item(int index)
		{
			return (IMoStemName)List[index];
		}
	}
	#endregion // MoStemNameCollection

	#region MoMorphTypeCollection

	/// <summary>
	/// A collection class for MoMorphType objects.
	/// </summary>
	public class MoMorphTypeCollection : ReadOnlyCollectionBase
	{
		/// <summary>
		/// Size limit for the MoMorphTypeCollection object.
		/// </summary>
		public const int kmtLimit = 19;
		private FdoCache m_fdoCache;

		/// <summary>
		/// Constructor.
		/// </summary>
		public MoMorphTypeCollection(FdoCache fdoCache)
		{
			Debug.Assert(fdoCache != null);
			m_fdoCache = fdoCache;

			List<IMoMorphType> al = new List<IMoMorphType>();
			foreach (IMoMorphType mt in m_fdoCache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities)
				al.Add(mt);
			if (al.Count < kmtLimit)
				AddMissingMorphTypes(al, fdoCache);
			Debug.Assert(al.Count == kmtLimit);
			InnerList.Capacity = kmtLimit;
			for (int i = 0; i < kmtLimit; ++i)
				InnerList.Add(null);
			foreach (IMoMorphType mmt in al)
				InnerList[MoMorphType.FindMorphTypeIndex(fdoCache, mmt)] = mmt;
		}

		/// <summary>
		/// If Phrase and Discontiguous Phrase somehow missed getting added by data migration,
		/// add them now.  See LT-5567 for an occurrence of this problem.
		/// Actually, LTB-344 demonstrates that another morphtype might be missing for some
		/// reason, so this is a general fix for any number of missing morphtypes.
		/// </summary>
		/// <param name="morphTypes"></param>
		/// <param name="cache"></param>
		private static void AddMissingMorphTypes(List<IMoMorphType> morphTypes, FdoCache cache)
		{
			List<string> neededTypes = new List<string>(19);
			neededTypes.Add(MoMorphType.kguidMorphBoundRoot);
			neededTypes.Add(MoMorphType.kguidMorphBoundStem);
			neededTypes.Add(MoMorphType.kguidMorphCircumfix);
			neededTypes.Add(MoMorphType.kguidMorphClitic);
			neededTypes.Add(MoMorphType.kguidMorphDiscontiguousPhrase);
			neededTypes.Add(MoMorphType.kguidMorphEnclitic);
			neededTypes.Add(MoMorphType.kguidMorphInfix);
			neededTypes.Add(MoMorphType.kguidMorphInfixingInterfix);
			neededTypes.Add(MoMorphType.kguidMorphParticle);
			neededTypes.Add(MoMorphType.kguidMorphPhrase);
			neededTypes.Add(MoMorphType.kguidMorphPrefix);
			neededTypes.Add(MoMorphType.kguidMorphPrefixingInterfix);
			neededTypes.Add(MoMorphType.kguidMorphProclitic);
			neededTypes.Add(MoMorphType.kguidMorphRoot);
			neededTypes.Add(MoMorphType.kguidMorphSimulfix);
			neededTypes.Add(MoMorphType.kguidMorphStem);
			neededTypes.Add(MoMorphType.kguidMorphSuffix);
			neededTypes.Add(MoMorphType.kguidMorphSuffixingInterfix);
			neededTypes.Add(MoMorphType.kguidMorphSuprafix);
			foreach (IMoMorphType mmt in morphTypes)
			{
				foreach (string sGuid in neededTypes)
				{
					if (mmt.Guid.ToString().ToLowerInvariant() == sGuid.ToLowerInvariant())
					{
						neededTypes.Remove(sGuid);
						break;
					}
				}
			}
			int wsEn = cache.LanguageEncodings.GetWsFromIcuLocale("en");
			foreach (string sGuid in neededTypes)
			{
				MoMorphType mmt = new MoMorphType();
				cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Append(mmt);
				mmt.Guid = new Guid(sGuid);
				mmt.ForeColor = -1073741824;
				mmt.BackColor = -1073741824;
				mmt.UnderColor = -1073741824;
				mmt.IsProtected = true;
				switch (sGuid)
				{
					case MoMorphType.kguidMorphBoundRoot:
						mmt.Name.SetAlternative("bound root", wsEn);
						mmt.Abbreviation.SetAlternative("bd root", wsEn);
						mmt.Description.SetAlternative("A bound root is a root which cannot occur as a separate word apart from any other morpheme.", wsEn);
						mmt.Prefix = "*";
						mmt.SecondaryOrder = 10;
						break;
					case MoMorphType.kguidMorphBoundStem:
						mmt.Name.SetAlternative("bound stem", wsEn);
						mmt.Abbreviation.SetAlternative("bd stem", wsEn);
						mmt.Description.SetAlternative("A bound stem is a stem  which cannot occur as a separate word apart from any other morpheme.", wsEn);
						mmt.Prefix = "*";
						mmt.SecondaryOrder = 10;
						break;
					case MoMorphType.kguidMorphCircumfix:
						mmt.Name.SetAlternative("circumfix", wsEn);
						mmt.Abbreviation.SetAlternative("cfx", wsEn);
						mmt.Description.SetAlternative("A circumfix is an affix made up of two separate parts which surround and attach to a root or stem.", wsEn);
						break;
					case MoMorphType.kguidMorphClitic:
						mmt.Name.SetAlternative("clitic", wsEn);
						mmt.Abbreviation.SetAlternative("clit", wsEn);
						mmt.Description.SetAlternative("A clitic is a morpheme that has syntactic characteristics of a word, but shows evidence of being phonologically bound to another word. Orthographically, it stands alone.", wsEn);
						break;
					case MoMorphType.kguidMorphDiscontiguousPhrase:
						mmt.Name.SetAlternative("discontiguous phrase", wsEn);
						mmt.Abbreviation.SetAlternative("dis phr", wsEn);
						mmt.Description.SetAlternative("A discontiguous phrase has discontiguous constituents which (a) are separated from each other by one or more intervening constituents, and (b) are considered either (i) syntactically contiguous and unitary, or (ii) realizing the same, single meaning. An example is French ne...pas.", wsEn);
						break;
					case MoMorphType.kguidMorphEnclitic:
						mmt.Name.SetAlternative("enclitic", wsEn);
						mmt.Abbreviation.SetAlternative("enclit", wsEn);
						mmt.Description.SetAlternative("An enclitic is a clitic that is phonologically joined at the end of a preceding word to form a single unit. Orthographically, it may attach to the preceding word.", wsEn);
						mmt.Prefix = "=";
						mmt.SecondaryOrder = 80;
						break;
					case MoMorphType.kguidMorphInfix:
						mmt.Name.SetAlternative("infix", wsEn);
						mmt.Abbreviation.SetAlternative("ifx", wsEn);
						mmt.Description.SetAlternative("An infix is an affix that is inserted within a root or stem.", wsEn);
						mmt.Postfix = "-";
						mmt.Prefix = "-";
						mmt.SecondaryOrder = 40;
						break;
					case MoMorphType.kguidMorphInfixingInterfix:
						mmt.Name.SetAlternative("infixing interfix", wsEn);
						mmt.Abbreviation.SetAlternative("ifxnfx", wsEn);
						mmt.Description.SetAlternative("An infixing interfix is an infix that can occur between two roots or stems.", wsEn);
						mmt.Postfix = "-";
						mmt.Prefix = "-";
						break;
					case MoMorphType.kguidMorphParticle:
						mmt.Name.SetAlternative("particle", wsEn);
						mmt.Abbreviation.SetAlternative("part", wsEn);
						mmt.Description.SetAlternative("A particle is a word that does not belong to one of the main classes of words, is invariable in form, and typically has grammatical or pragmatic meaning.", wsEn);
						break;
					case MoMorphType.kguidMorphPhrase:
						mmt.Name.SetAlternative("phrase", wsEn);
						mmt.Abbreviation.SetAlternative("phr", wsEn);
						mmt.Description.SetAlternative("A phrase is a syntactic structure that consists of more than one word but lacks the subject-predicate organization of a clause.", wsEn);
						break;
					case MoMorphType.kguidMorphPrefix:
						mmt.Name.SetAlternative("prefix", wsEn);
						mmt.Abbreviation.SetAlternative("pfx", wsEn);
						mmt.Description.SetAlternative("A prefix is an affix that is joined before a root or stem.", wsEn);
						mmt.Postfix = "-";
						mmt.SecondaryOrder = 20;
						break;
					case MoMorphType.kguidMorphPrefixingInterfix:
						mmt.Name.SetAlternative("prefixing interfix", wsEn);
						mmt.Abbreviation.SetAlternative("pfxnfx", wsEn);
						mmt.Description.SetAlternative("A prefixing interfix is a prefix that can occur between two roots or stems.", wsEn);
						mmt.Postfix = "-";
						break;
					case MoMorphType.kguidMorphProclitic:
						mmt.Name.SetAlternative("proclitic", wsEn);
						mmt.Abbreviation.SetAlternative("proclit", wsEn);
						mmt.Description.SetAlternative("A proclitic is a clitic that precedes the word to which it is phonologically joined. Orthographically, it may attach to the following word.", wsEn);
						mmt.Postfix = "=";
						mmt.SecondaryOrder = 30;
						break;
					case MoMorphType.kguidMorphRoot:
						mmt.Name.SetAlternative("root", wsEn);
						mmt.Abbreviation.SetAlternative("ubd root", wsEn);
						mmt.Description.SetAlternative("A root is the portion of a word that (i) is common to a set of derived or inflected forms, if any, when all affixes are removed, (ii) is not further analyzable into meaningful elements, being morphologically simple, and, (iii) carries the principle portion of meaning of the words in which it functions.", wsEn);
						break;
					case MoMorphType.kguidMorphSimulfix:
						mmt.Name.SetAlternative("simulfix", wsEn);
						mmt.Abbreviation.SetAlternative("smfx", wsEn);
						mmt.Description.SetAlternative("A simulfix is a change or replacement of vowels or consonants (usually vowels) which changes the meaning of a word.  (Note: the parser does not currently handle simulfixes.)", wsEn);
						mmt.Postfix = "=";
						mmt.Prefix = "=";
						mmt.SecondaryOrder = 60;
						break;
					case MoMorphType.kguidMorphStem:
						mmt.Name.SetAlternative("stem", wsEn);
						mmt.Abbreviation.SetAlternative("ubd stem", wsEn);
						mmt.Description.SetAlternative("\"A stem is the root or roots of a word, together with any derivational affixes, to which inflectional affixes are added.\" (LinguaLinks Library).  A stem \"may consist solely of a single root morpheme (i.e. a 'simple' stem as in man), or of two root morphemes (e.g. a 'compound' stem, as in blackbird), or of a root morpheme plus a derivational affix (i.e. a 'complex' stem, as in manly, unmanly, manliness).  All have in common the notion that it is to the stem that inflectional affixes are attached.\" (Crystal, 1997:362)", wsEn);
						break;
					case MoMorphType.kguidMorphSuffix:
						mmt.Name.SetAlternative("suffix", wsEn);
						mmt.Abbreviation.SetAlternative("sfx", wsEn);
						mmt.Description.SetAlternative("A suffix is an affix that is attached to the end of a root or stem.", wsEn);
						mmt.Prefix = "-";
						mmt.SecondaryOrder = 70;
						break;
					case MoMorphType.kguidMorphSuffixingInterfix:
						mmt.Name.SetAlternative("suffixing interfix", wsEn);
						mmt.Abbreviation.SetAlternative("sfxnfx", wsEn);
						mmt.Description.SetAlternative("A suffixing interfix is an suffix that can occur between two roots or stems.", wsEn);
						mmt.Prefix = "-";
						break;
					case MoMorphType.kguidMorphSuprafix:
						mmt.Name.SetAlternative("suprafix", wsEn);
						mmt.Abbreviation.SetAlternative("spfx", wsEn);
						mmt.Description.SetAlternative("A suprafix is a kind of affix in which a suprasegmental is superimposed on one or more syllables of the root or stem, signalling a particular  morphosyntactic operation.  (Note: the parser does not currently handle suprafixes.)", wsEn);
						mmt.Postfix = "~";
						mmt.Prefix = "~";
						mmt.SecondaryOrder = 50;
						break;
				}
				morphTypes.Add(mmt);
			}
		}

		/// <summary>
		/// Get the MoMorphType at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The IMoMorphType interface at the specified index.</returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the index is invalid.
		/// </exception>
		public IMoMorphType Item(int index)
		{
			return (IMoMorphType)InnerList[index];
		}

		/// <summary>
		/// If the given form matches exactly one of the morphtype prefixes, return that
		/// MoMorphType object.  If that morphtype also has a postfix, add the postfix to
		/// the adjusted form.  This allows better handling of suprafixes for the default
		/// prefix and postfix marking.  (See LT-6081 and LT-6082.)
		/// </summary>
		/// <param name="sForm"></param>
		/// <param name="sAdjustedForm"></param>
		/// <returns></returns>
		public IMoMorphType GetTypeIfMatchesPrefix(string sForm, out string sAdjustedForm)
		{
			sAdjustedForm = sForm;
			IMoMorphType mmtPossible = null;
			for (int i = 0; i < kmtLimit; ++i)
			{
				if (i == MoMorphType.kmtBoundRoot)
					continue;	// save bound root for last to allow bound stem to have priority.
				IMoMorphType mmt = (IMoMorphType)InnerList[i];
				if (mmt.Prefix != sForm)
					continue;
				// If there's a type with a matching prefix and no postfix, return it.  Don't
				// worry about ambiguity -- that's life.
				if (mmt.Postfix == null)
					return mmt;
				// We have both a prefix and a postfix.  Save it in case it's unique.
				mmtPossible = mmt;
			}
			IMoMorphType mmtBoundRoot = (IMoMorphType)InnerList[MoMorphType.kmtBoundRoot];
			if (mmtBoundRoot.Prefix == sForm && mmtBoundRoot.Postfix == null)
				return mmtBoundRoot;
			if (mmtPossible != null)
			{
				sAdjustedForm = mmtPossible.Prefix + mmtPossible.Postfix;
				return mmtPossible;
			}
			else
			{
				return null;
			}
		}
	}

	#endregion // MoMorphTypeCollection
}
