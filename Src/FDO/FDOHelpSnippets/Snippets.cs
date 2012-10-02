/*----------------------------------------------------------------------------------------------
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Snippets.cs
Responsibility: John Hatton
Last reviewed: never

	This file contains the following classes:
		Snippets : Object
----------------------------------------------------------------------------------------------*/
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Notebk;
using SIL.FieldWorks.FDO.LangProj;

	namespace FDOHelpSnippets
	{
		/// <summary>
		/// this class is full of short examples which are used in the help documentation.
		/// </summary>
		/// <remarks> Most of the code here is extremely unsafe. All error checking has been omitted
		/// for the sake of brevity.</remarks>
		class Snippets
		{
			/// <summary>
			/// The main entry point for the application.
			/// </summary>
//			[STAThread]
//			static void Main(string[] args)
//			{
//				Snippets t = new Snippets();
//				t.DoAllSnippets();
//
//				Console.WriteLine("Press Enter To Exit");
//				Console.ReadLine();
//			};


			public bool DoAllSnippets(FdoCache fcTLP)
			{
//				const string ksCmPossLangProj = "TestLangProj";
//				using(FdoCache fcTLP = new FdoCache(ksCmPossLangProj))
				{
					LangProject lp = fcTLP.LangProject;
					PrintLexDBName(fcTLP);
					PrintCmPossibility(fcTLP);
					AssignObjects(lp);
					MakeNewObjects (lp);
					DeleteObjects (fcTLP);
					ReadFromVectors (lp);
					ModifyVectors (lp);
					ReadFromVectorsEfficiencyComparison(fcTLP);
					ListLexEntries(fcTLP);
				}
				return true;
			}

			public void PrintLexDBName(FdoCache fcTLP)
			{
				LangProject lp = fcTLP.LangProject;
				LexDb lxdb = lp.LexDbOA;
				Console.WriteLine(lxdb.Name.AnalysisDefaultWritingSystem);
			}

			public void	PrintCmPossibility(FdoCache fcTLP)
			{
				CmPossibility p = new CmPossibility(fcTLP, 190);
				Console.WriteLine(p.Description.AnalysisDefaultWritingSystem);
			}

			public void AssignObjects (LangProject lp)
			{
				//assign an atomic reference attribute
				FdoObjectSet os = (FdoObjectSet)lp.ResearchNotebookOA.RecordsOC.GetEnumerator();
				os.MoveNext();
				RnGenericRec record =(RnGenericRec)os.Current;
				record.ConfidenceRA= (CmPossibility)lp.ConfidenceLevelsOA.PossibilitiesOS[0];
			}

			public void MakeNewObjects(LangProject lp)
			{
				// add a new morphological data to the language project
				// (and replace the existing one, if there is one)
				lp.MorphologicalDataOA = new MoMorphData();

				// add a new set of test words to the morphological data object
				lp.MorphologicalDataOA.TestSetsOC.Add(new WfiWordSet());
			}

			public void DeleteObjects(FdoCache fcTLP)
			{
				//fcTLP.BeginUndoTask("test", "test");
				LangProject lp = fcTLP.LangProject;
				MoMorphData mmd = lp.MorphologicalDataOA;
				if(mmd!= null)
				{
					mmd.DeleteUnderlyingObject();

					//see that this call cleared out members of that object
					Debug.Assert(mmd.hvo < 0);

					mmd = null;  // just in case I try to use it again.
				}

				// or, if we we don't want to bother loading the object,
				// fcTLP.DeleteObject(lp.MorphologicalDataOAHvo);

				//fcTLP.EndUndoTask();
				//fcTLP.Undo ();//don't really want to delete that
			}
			public void ReadFromVectors(LangProject lp)
			{
				//read a single item, the third element in the AnthroList
				CmAnthroItem p= (CmAnthroItem) lp.AnthroListOA.PossibilitiesOS[2];

				//read all of the items in the AntrhoList
				foreach (CmAnthroItem item in lp.AnthroListOA.PossibilitiesOS)
				{
					Console.WriteLine (item.Name.AnalysisDefaultWritingSystem);
				}
			}

			public void ModifyVectors (LangProject lp)
			{
				//add a new item to an owned sequence attribute
				CmAnthroItem a = (CmAnthroItem)lp.AnthroListOA.PossibilitiesOS.Append(new CmAnthroItem ());

				//add a new item to an owned collection attribute
				CmOverlay overlay = (CmOverlay)lp.OverlaysOC.Add (new CmOverlay());

				//add a new item to a reference collection attribute
				CmPossibility position = (CmPossibility)lp.PositionsOA.PossibilitiesOS [0];
				CmPerson person =(CmPerson)lp.PeopleOA.PossibilitiesOS[0];
				person.PositionsRC.Add(position);

				//move the last item in a sequence to the beginning
				FdoOwnSeqVector positions =lp.PositionsOA.PossibilitiesOS;

				position = (CmPossibility)positions[positions.Count-1];
				positions.InsertAt (position,0);

				//do the same, without instantiating the object we're moving
				int hvo = positions.hvoArray[positions.Count-1];
				positions.InsertAt(hvo,0);
			}
			public void ReadFromVectorsEfficiencyComparison(FdoCache fcTLP)
			{
				LangProject lp = fcTLP.LangProject;

				//bad
				for(int i = 1; i< lp.OverlaysOC.Count; i++) // must create an FdoOwnColVector each time!
				{
					CmOverlay o = new CmOverlay(fcTLP,lp.OverlaysOC.hvoArray[i]);	// must create an FdoOwnColVector each time!
					Console.WriteLine(o.Name);
				}

				//better
				int[] hvos  = lp.OverlaysOC.hvoArray;  // get the vector just once
				for(int i = 1; i<hvos.Length; i++)
				{
					CmOverlay o = new CmOverlay(fcTLP,hvos[i]);
					Console.WriteLine(o.Name);
				}

				//best: get vector just once, and all of the overlays will be cached at once
				foreach(CmOverlay o in lp.OverlaysOC)
				{
					Console.WriteLine(o.Name);
				}
			}

			public void ListLexEntries(FdoCache fcTLP)
			{
				LexDb lxdb = fcTLP.LangProject.LexDbOA;
				FdoOwnColVector ocv = lxdb.EntriesOC;
				int[] vecEntryHvos = ocv.hvoArray;
				foreach(LexEntry e in ocv)
				{
					// Will return LexMajorEntry, LexSubentry, or LexMinorEntry,
					// since these are the classes that fit the signature of LexEntry.
					Console.WriteLine(e.CitationForm.VernacularDefaultWritingSystem + " is a " + e.GetType().Name);
				}
			}
		}

	}
