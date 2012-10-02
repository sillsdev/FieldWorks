// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: PositionAnalyzer.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of PositionAnalyzer and its supporting class: MorphemeWrapper
// </remarks>
//
// --------------------------------------------------------------------------------------------

using System;
using System.Resources;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Main class for analyzing affix positions.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class PositionAnalyzer : GafawsProcessor
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The prefixes that need to be processed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Dictionary<string, MorphemeWrapper> m_prefixes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The suffixes that need to be processed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Dictionary<string, MorphemeWrapper> m_suffixes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Status messages that get added to the output.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Dictionary<string, string> m_messages;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PositionAnalyzer()
		{
			m_prefixes = new Dictionary<string, MorphemeWrapper>();
			m_suffixes = new Dictionary<string, MorphemeWrapper>();
			m_messages = new Dictionary<string, string>();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Implements the IGAFAWS interface.
		/// </summary>
		/// <param name="pathInput">Pathname to the input data.</param>
		/// <returns>The pathname for the processed data.</returns>
		/// -----------------------------------------------------------------------------------
		public string Process(string pathInput)
		{
			// Replaces 8 lines in C++.
			GAFAWSData gData = GAFAWSData.LoadData(pathInput);
			if (gData == null)
				return null;

			Process(gData);
			string outPath = GetOutputPathname(pathInput);
			m_gd.SaveData(outPath);
			return outPath;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Implements the IGAFAWS interface.
		/// </summary>
		/// <param name="gData">GAFAWSData to process.</param>
		/// -----------------------------------------------------------------------------------
		public void Process(GAFAWSData gData)
		{
			LoadMessages();

			// Replaces 8 lines in C++.
			m_gd = gData;
			if (m_gd == null)
				return;

			// Replaces CJGParadigm::GetAffixesFromDom()
			// 52 lines in C++.
			foreach(Morpheme m in m_gd.Morphemes)
			{
				switch (m.type)
				{
					case "pfx":
						m_prefixes.Add(m.MID, new MorphemeWrapper(m));
						break;
					case "sfx":
						m_suffixes.Add(m.MID, new MorphemeWrapper(m));
						break;
					//case "s":	// Skip stems.
				}
			}
			if ((m_prefixes.Count + m_suffixes.Count) == 0)
				return;	// Nothing to do.

			if (!AssignToSets())
				return;	// Nothing to do.

			// Find the classes for prefixes.
			PrefixClassAssigner pca = new PrefixClassAssigner(m_gd, m_messages);
			if (!pca.AssignClasses(m_prefixes))
				return;
			// Find the classes for suffixes.
			SuffixClassAssigner sca = new SuffixClassAssigner(m_gd, m_messages);
			if (!sca.AssignClasses(m_suffixes))
				return;

			foreach (KeyValuePair<string, MorphemeWrapper> kvp in m_prefixes)
				kvp.Value.SetStartAndEnd();
			foreach (KeyValuePair<string, MorphemeWrapper> kvp in m_suffixes)
				kvp.Value.SetStartAndEnd();

			// Set date and time.
			// Replaces 44 lines of C++ code.
			DateTime dt = DateTime.Now;
			m_gd.date = dt.ToLongDateString();
			m_gd.time = dt.ToLongTimeString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Assign affixes to predecessor and successor sets.
		/// </summary>
		/// <returns>True if it assigned affixes to sets, otherwise false.</returns>
		/// -----------------------------------------------------------------------------------
		protected bool AssignToSets()
		{	// C++ took 79 lines.
			if (m_gd.WordRecords.Count == 0)
				return false;	// Nothing to do.

			foreach(WordRecord wr in m_gd.WordRecords)
			{
				List<Affix> ac = null;
				Dictionary<string, MorphemeWrapper> affixes = null;
				for (int i = 0; i < 2; ++i)
				{
					switch (i)
					{
						case 0:	// Prefixes
							ac = wr.Prefixes;
							affixes = m_prefixes;
							break;
						case 1:	// Suffixes
							ac = wr.Suffixes;
							affixes = m_suffixes;
							break;
					}
					// Note: This will not run through the loop for cases where there is
					// only one affix, which is right, since it has nothing on either side.
					for (int iAfx = 1; ac != null && iAfx < ac.Count; ++iAfx)
					{
						string prevID = ac[iAfx -1].MIDREF;
						string curID = ac[iAfx].MIDREF;
						affixes[curID].AddAsSuccessor(prevID);
						affixes[prevID].AddAsPredecessor(curID);
					}
				}
			}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Loads the resource strings.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void LoadMessages()
		{
			string resID = "SIL.WordWorks.GAFAWS.PositionAnalysis.StringsResource";
			ResourceManager LocRM = new ResourceManager(resID, this.GetType().Assembly);

			string stid = "kstidBadPrefixes";
			m_messages.Add(stid, LocRM.GetString(stid));
			stid = "kstidBadSuffixes";
			m_messages.Add(stid, LocRM.GetString(stid));

			LocRM.ReleaseAllResources();
		}
	}	// End class PositionAnalyzer


	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A wrapper class for the data layer morpheme object.
	/// </summary>
	/// <remarks>
	/// This wrapper class allows us to keep track of useful information of a morpheme
	/// that is not supported by the data layer.
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	internal class MorphemeWrapper
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The real morpheme.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Morpheme m_morpheme;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// List of predecessor affixes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private StringCollection m_predecessors;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// List of successor affixes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private StringCollection m_successors;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Starting class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Class m_start;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Ending class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private Class m_end;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cosntructor.
		/// </summary>
		/// <param name="m">The real morpheme that is wrapped.</param>
		/// -----------------------------------------------------------------------------------
		public MorphemeWrapper(Morpheme m)
		{
			m_morpheme = m;
			m_predecessors = new StringCollection();
			m_successors = new StringCollection();
			m_start = null;
			m_end = null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new affix as a successor, if it is not already in the list.
		/// </summary>
		/// <param name="succ">Affix ID.</param>
		/// -----------------------------------------------------------------------------------
		public void AddAsSuccessor(string succ)
		{
			for(int i = 0; i < m_successors.Count; ++i)
				if (m_successors[i] == succ)
					return;
			m_successors.Add(succ);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new affix as a predecessor, if it is not already in the list.
		/// </summary>
		/// <param name="pred">Affix ID.</param>
		/// -----------------------------------------------------------------------------------
		public void AddAsPredecessor(string pred)
		{
			for(int i = 0; i < m_predecessors.Count; ++i)
				if (m_predecessors[i] == pred)
					return;
			m_predecessors.Add(pred);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the starting and ending class IDREFs of the real morpheme.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void SetStartAndEnd()
		{	// Replaces 41 lines in C++.
			if (m_start != null)
				m_morpheme.StartCLIDREF = m_start.CLID;
			if (m_end != null)
				m_morpheme.EndCLIDREF = m_end.CLID;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove an affix from the list of predecessors.
		/// </summary>
		/// <param name="pred">The affix ID to remove.</param>
		/// -----------------------------------------------------------------------------------
		public void RemovePredecessor(string pred)
		{
			m_predecessors.Remove(pred);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove an affix from the list of successors.
		/// </summary>
		/// <param name="succ">The affix ID to remove.</param>
		/// -----------------------------------------------------------------------------------
		public void RemoveSuccessor(string succ)
		{
			m_successors.Remove(succ);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the morpheme can be assigned a class.
		/// </summary>
		/// <param name="lt">The type of list to check.</param>
		/// <returns>True if the class can be assigne, otherwise false.</returns>
		/// -----------------------------------------------------------------------------------
		public bool CanAssignClass(ListType lt)
		{
			bool fRetVal = true;
			switch (lt)
			{
				default:
					fRetVal = false;
					break;
				case ListType.kPred:
					fRetVal = (m_predecessors.Count == 0);
					break;
				case ListType.kSucc:
					fRetVal = (m_successors.Count == 0);
					break;
			}
			return fRetVal;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the starting or ending class.
		/// </summary>
		/// <param name="fIsStartPoint">True if the class is the starting class,
		/// otherwise false.</param>
		/// <param name="cls">The class to set.</param>
		/// -----------------------------------------------------------------------------------
		public void SetAffixClass(bool fIsStartPoint, Class cls)
		{
			if (fIsStartPoint)
			{
				if (m_start == null)
					m_start = cls;
				else Debug.Assert(false);
			}
			else
			{
				if (m_end == null)
					m_end = cls;
				else Debug.Assert(false);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the ID of the wrapped morpheme.
		/// </summary>
		/// <returns>The ID of the wrapped morpheme.</returns>
		/// -----------------------------------------------------------------------------------
		public string GetID()
		{
			return m_morpheme.MID;
		}
	}
}
