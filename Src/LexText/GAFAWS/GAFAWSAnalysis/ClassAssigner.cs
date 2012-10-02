// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: ClassAssigner.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of ClassAssigner and it two sublasses:
// PrefixClassAssigner and SuffixClassAssigner
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// List type enumeration.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal enum ListType
	{
		kPred,
		kSucc
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract superclass for prefix and suffix class assigners.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal abstract class ClassAssigner
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Affixes remaining to be assigned.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected Dictionary<string, MorphemeWrapper> m_toCheck;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set of possible status messages.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected Dictionary<string, string> m_messages;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected bool m_fAssignedOk;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Main GAFAWS data layer object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected GAFAWSData m_gd;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collection of classes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected List<Class> m_classes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The class ID prefix.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected string m_classPrefix;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The class that is the unknown group of classes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected Class m_fogBank;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gd">The main data layer object.</param>
		/// <param name="messages">List of status messages.</param>
		/// -----------------------------------------------------------------------------------
		public ClassAssigner(GAFAWSData gd, Dictionary<string, string> messages)
		{
			m_messages = messages;
			m_fAssignedOk = true;
			m_gd = gd;
			m_fogBank = null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Assign all affixes to the relevant position class.
		/// </summary>
		/// <param name="htToCheck">The set of affixes to check.</param>
		/// <returns>True if the classes were assigned, otherwise false.</returns>
		/// -----------------------------------------------------------------------------------
		public bool AssignClasses(Dictionary<string, MorphemeWrapper> htToCheck)
		{
			m_toCheck = new Dictionary<string, MorphemeWrapper>(htToCheck);
			AssignClassesFromStem();
			m_toCheck = new Dictionary<string, MorphemeWrapper>(htToCheck);
			AssignClassesFromEnd();

			// Number the classes.
			int i = 1;
			foreach(Class c in m_classes)
			{
				switch (c.isFogBank)
				{
					case "0":
						c.CLID = m_classPrefix + i++.ToString();
						break;
					case "1":
						c.CLID = m_classPrefix + "0";
						break;
				}
			}
			return m_fAssignedOk;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Assign the classes from the stem out to the end.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void AssignClassesFromStem()
		{
			int iFog = 0;
			ListType lt = GetListType(true);
			AssignClassesCore(lt, true, iFog, null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Assign classes from the end to the stem.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void AssignClassesFromEnd()
		{
			ListType lt = GetListType(false);
			int iFog = 0;
			List<Class> oldClasses = null;

			if (m_fogBank != null)
			{
				for (iFog = 0; iFog < m_classes.Count; ++iFog)
				{
					if (m_classes[iFog] == m_fogBank)
						break;
				}
			}
			else oldClasses = new List<Class>(m_classes);

			AssignClassesCore(lt, false, iFog, oldClasses);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Abstract method that is to return a ListType.
		/// </summary>
		/// <param name="fFromStem">True, if working out from the stem,
		/// otherwise false.</param>
		/// <returns>The ListType.</returns>
		/// -----------------------------------------------------------------------------------
		abstract protected ListType GetListType(bool fFromStem);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Do the main class assignment.
		/// </summary>
		/// <param name="lt">Tyep of list to process.</param>
		/// <param name="fIsStartPoint">True if the classes are the starting point,
		/// otherwise false.</param>
		/// <param name="iFog">True if the remaining classes are unknowable,
		/// otherwise false.</param>
		/// <param name="oldClasses">Old set of classes, if working towards stem,
		/// otherwise null.</param>
		/// -----------------------------------------------------------------------------------
		protected void AssignClassesCore(ListType lt, bool fIsStartPoint,
			int iFog, List<Class> oldClasses)
		{
			while (m_toCheck.Count > 0)
			{
				bool fInFog;
				Dictionary<string, MorphemeWrapper> toBeAssigned = GetCandidates(lt, out fInFog);
				Class cls = GetClass(lt, fIsStartPoint, fInFog, iFog, oldClasses);
				DoAssignment(toBeAssigned, lt, fIsStartPoint, cls);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gather up a set of affixes to classify.
		/// </summary>
		/// <param name="lt">Type of list to process.</param>
		/// <param name="fInFog">Set to True if the remaining classes are unknowable,
		/// otherwise set to false.</param>
		/// <returns>Set of affixes to work on.</returns>
		/// -----------------------------------------------------------------------------------
		protected Dictionary<string, MorphemeWrapper> GetCandidates(ListType lt, out bool fInFog)
		{
			fInFog = false;
			Dictionary<string, MorphemeWrapper> ht = new Dictionary<string, MorphemeWrapper>();
			foreach (KeyValuePair<string, MorphemeWrapper> kvp in m_toCheck)
			{
				MorphemeWrapper mr = kvp.Value;
				if (mr.CanAssignClass(lt))
					ht.Add(mr.GetID(), mr);
			}
			if (ht.Count == 0)
			{
				// Couldn't find any, so we have bad input data.
				// Assign all remaining affixes to 'fog' class.
				fInFog = true;
				ht = new Dictionary<string, MorphemeWrapper>(m_toCheck);
			}
			return ht;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find (create, if needed) the class to use for the assignment.
		/// </summary>
		/// <param name="lt">Type of list to process.</param>
		/// <param name="fIsStartPoint">True if assigning starting class,
		/// otherwise false.</param>
		/// <param name="fInFog">True if the remaining classes are unknowable,
		/// otherwise false.</param>
		/// <param name="iFog">Index for the unknown class.</param>
		/// <param name="oldClasses">Old set of classes, if working towards stem,
		/// otherwise null.</param>
		/// <returns>The class to use for the assignment.</returns>
		/// -----------------------------------------------------------------------------------
		protected Class GetClass(ListType lt, bool fIsStartPoint, bool fInFog,
			int iFog, List<Class> oldClasses)
		{
			Class cls = null;

			if (fIsStartPoint)
			{
				// Working out from stem.
				cls = new Class();
				m_classes.Add(cls);
				if (fInFog)
				{
					m_fogBank = cls;
					cls.isFogBank = "1";
					Challenge chl = new Challenge();
					m_gd.Challenges.Add(chl);
					chl.message = GetMessage();
				}
			}
			else
			{
				// Working towards stem.
				if (fInFog)
				{
					if (m_fogBank != null)
						cls = m_fogBank;	// Other edge of fog bank.
					else
					{
						// How is it that the fog is unidirectional?
						Debug.Assert(false);
						;
					}
				}
				else
				{
					if (m_fogBank != null)
					{
						cls = new Class();
						// Insert it between fog and other outer classes.
						m_classes.Insert(iFog + 1, cls);
					}
					else
					{
						// Use old classes.
						int idx = oldClasses.Count - 1;
						cls = oldClasses[idx];
						oldClasses.RemoveAt(idx);
					}
				}
			}
			return cls;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Really do the assignment.
		/// </summary>
		/// <param name="htToBeAssigned">Set of affixes to be assigned.</param>
		/// <param name="lt">Type of list to process.</param>
		/// <param name="fIsStartPoint">True if setting the starting class,
		/// otherwise false.</param>
		/// <param name="cls">The class to set to.</param>
		/// -----------------------------------------------------------------------------------
		protected void DoAssignment(Dictionary<string, MorphemeWrapper> htToBeAssigned, ListType lt,
			bool fIsStartPoint, Class cls)
		{
			foreach (KeyValuePair<string, MorphemeWrapper> kvp in htToBeAssigned)
			{
				MorphemeWrapper mr = kvp.Value;
				mr.SetAffixClass(fIsStartPoint, cls);
				string mid = mr.GetID();
				m_toCheck.Remove(mid);
				foreach (KeyValuePair<string, MorphemeWrapper> kvpInner in m_toCheck)
				{
					if (lt == ListType.kSucc)
						kvpInner.Value.RemoveSuccessor(mid);
					else
						kvpInner.Value.RemovePredecessor(mid);
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the message, when there is a problem with the setting the class.
		/// Subclasses msut override this to return a suitable message.
		/// </summary>
		/// <returns>The problem message.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual string GetMessage()
		{
			return "";
		}

	}	// End of class ClassAssigner


	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// The class that assigns prefixes.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class PrefixClassAssigner : ClassAssigner
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gd">Main GAFAWS data layer object.</param>
		/// <param name="messages">Set of status messages.</param>
		/// -----------------------------------------------------------------------------------
		public PrefixClassAssigner(GAFAWSData gd, Dictionary<string, string> messages)
			: base(gd, messages)
		{
			m_classes = gd.Classes.PrefixClasses;
			m_classPrefix = "PP";
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to return proper ListType.
		/// </summary>
		/// <param name="fFromStem">True if working out from stem,
		/// otherwise false.</param>
		/// <returns>The ListType.</returns>
		/// -----------------------------------------------------------------------------------
		protected override ListType GetListType(bool fFromStem)
		{
			return fFromStem ? ListType.kPred : ListType.kSucc;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to return a better error message for this class.
		/// </summary>
		/// <returns>A problem report string.</returns>
		/// -----------------------------------------------------------------------------------
		protected override string GetMessage()
		{
			return m_messages["kstidBadPrefixes"];
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// The class tha assigns suffixes.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class SuffixClassAssigner : ClassAssigner
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gd">Main GAFAWS data layer object.</param>
		/// <param name="messages">Set of status messages.</param>
		/// -----------------------------------------------------------------------------------
		public SuffixClassAssigner(GAFAWSData gd, Dictionary<string, string> messages)
			: base(gd, messages)
		{
			m_classes = gd.Classes.SuffixClasses;
			m_classPrefix = "SP";
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to return proper ListType.
		/// </summary>
		/// <param name="fFromStem">True if working out from stem,
		/// otherwise false.</param>
		/// <returns>The ListType.</returns>
		/// -----------------------------------------------------------------------------------
		protected override ListType GetListType(bool fFromStem)
		{
			return fFromStem ? ListType.kSucc : ListType.kPred;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overrides method to return a better error message for this class.
		/// </summary>
		/// <returns>A problem report string.</returns>
		/// -----------------------------------------------------------------------------------
		protected override string GetMessage()
		{
			return m_messages["kstidBadSuffixes"];
		}
	}
}
