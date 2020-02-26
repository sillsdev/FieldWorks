// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// Test the functionality of the MacroListener class. A few minor methods are not tested, because testing would
	/// require setting up major objects like XWindows or major architectural changes so these objects make more use of interfaces
	/// to support mocking.
	/// </summary>
	[TestFixture]
	public class MacroMenuHandlerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private MacroMenuHandler _macroMenuHandler;

		#region Overrides of LcmTestBase
		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			_macroMenuHandler = new MacroMenuHandler();
			_flexComponentParameters.PropertyTable.SetProperty("cache", Cache);
			_macroMenuHandler.InitializeForTests(Cache);
		}

		public override void TestTearDown()
		{
			try
			{
				_macroMenuHandler?.Dispose();
				TestSetupServices.DisposeTrash(_flexComponentParameters);
				_macroMenuHandler = null;
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}
		#endregion

		/// <summary>
		/// Initialize the listener into a state where there is a MacroF4 mock available to implement that command.
		/// </summary>
		private static MacroF4 SetupF4Implementation(MacroMenuHandler ml)
		{
			var macroF4 = new MacroF4();
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] { macroF4 });
			ml.AssignMacrosToSlots(macroImplementors);
			macroF4.BeEnabled = true;
			return macroF4;
		}

		[Test]
		public void InitMacro_PutsItemAtProperPosition()
		{
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] { new MacroF4() });
			var macros = _macroMenuHandler.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.Null);
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0]));
		}

		[Test]
		public void InitMacro_PutsConflictAtFirstAvailableSpot()
		{
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] { new MacroF4(), new MacroF4(), new MacroF2() });
			var macros = _macroMenuHandler.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.EqualTo(macroImplementors[2])); // the only one that wants to be at F2
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0])); // first one that wants to be a F4
			Assert.That(macros[1], Is.EqualTo(macroImplementors[1])); // can't put where it wants to be, put it in first free slot.
		}

		/// <summary>
		/// Makes sure nothing too drastic happens if someone installs too many macro DLLs.
		/// </summary>
		[Test]
		public void InitMacro_SurvivesTooManyMacros()
		{
			var macroImplementors = new List<IFlexMacro>();
			for (var i = 0; i < 20; i++)
			{
				macroImplementors.Add(new MacroF4());
			}
			var macros = _macroMenuHandler.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.EqualTo(macroImplementors[1])); // first free slot gets the second one
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0])); // first one that wants to be a F4
			Assert.That(macros[1], Is.EqualTo(macroImplementors[2])); // can't put where it wants to be, put it in next free slot.
			Assert.That(macros[3], Is.EqualTo(macroImplementors[3])); // from here on they line up.
		}

		[Test]
		public void DoMacro_NormalPathSucceeds()
		{
			var macro = SetupF4Implementation(_macroMenuHandler);
			var commandName = macro.CommandName;
			ILexEntry entry = null;
			// We normally let undo and redo be localized independently, but we compromise in the interests of making macros
			// easier to create.
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(string.Format(LanguageExplorerResources.Undo_0, commandName), string.Format(LanguageExplorerResources.Redo_0, commandName),
				Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				macro.RunMacro(entry, LexEntryTags.kflidRestrictions, Cache.DefaultAnalWs, 2, 5);
			});

			Assert.That(entry.Restrictions.AnalysisDefaultWritingSystem.Text, Is.EqualTo("test succeeded"));
		}

		[Test]
		public void SafeToDoMacro_WithNoMacro_ReturnsFalse()
		{
			Assert.That(_macroMenuHandler.SafeToDoMacro(null), Is.False);
		}

		[Test]
		public void SafeToDoMacro_WithNoSelection_ReturnsFalse()
		{
			Assert.That(_macroMenuHandler.SafeToDoMacro(new MacroF4()), Is.False);
		}

		[Test]
		public void SafeToDoMacro_WithUnsuitableSelection_ReturnsFalse()
		{
			var macro4 = new MacroF4();
			var sel = new MockSelection
			{
				EndHvo = 317,
				AnchorHvo = 317,
				EndTag = LexEntryTags.kflidRestrictions, // arbitrary in this case
				AnchorTag = LexEntryTags.kflidRestrictions, // arbitrary in this case
				EndIch = 2,
				AnchorIch = 5
			};

			Assert.That(_macroMenuHandler.SafeToDoMacro(macro4, sel, out _, out _, out _, out _, out _), Is.False); // wrong type of selection

			sel.TypeToReturn = VwSelType.kstText;
			sel.EndHvo = 316;
			Assert.That(_macroMenuHandler.SafeToDoMacro(macro4, sel, out _, out _, out _, out _, out _), Is.False); // different objects

			sel.EndHvo = sel.AnchorHvo;
			sel.EndTag = 3;
			Assert.That(_macroMenuHandler.SafeToDoMacro(macro4, sel, out _, out _, out _, out _, out _), Is.False); // different tags
		}

		/// <summary>
		/// Mock macro for F2 key. In this one we only use the PreferredFunctionKey.
		/// </summary>
		private sealed class MacroF2 : IFlexMacro
		{
			public string CommandName => throw new NotSupportedException();

			private bool BeEnabled;
			public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
			{
				return BeEnabled;
			}

			public void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length)
			{
				throw new NotSupportedException();
			}

			public Keys PreferredFunctionKey => Keys.F2;
		}

		/// <summary>
		/// A more complete mock for the F4 key. RunMacro sets a string property. This verifies that we are getting the unit of work
		/// into the required state.
		/// </summary>
		private sealed class MacroF4 : IFlexMacro
		{
			public string CommandName => "F4test";

			public bool BeEnabled;
			public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
			{
				return BeEnabled;
			}

			public void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length)
			{
				((ILexEntry)target).Restrictions.set_String(target.Cache.DefaultAnalWs, "test succeeded");
			}

			public Keys PreferredFunctionKey => Keys.F4;
		}

		/// <summary>
		/// This class mocks the (unfortunately huge) IVwSelection. Only the first couple of methods are actually used by MacroListener,
		/// others are left unimplemented. Enhance JohnT: this would be better done with some sort of dynamic mock, so that changes to
		/// parts of the interface we don't care about won't have to be made here. But we haven't settled on a mock framework for FLEx.
		/// </summary>
		private sealed class MockSelection : IVwSelection
		{
			public VwSelType TypeToReturn;
			public VwSelType SelType => TypeToReturn;

			public int EndHvo;
			public int AnchorHvo;
			public int EndTag;
			public int AnchorTag;
			private int EndWs;
			private int AnchorWs;
			public int EndIch;
			public int AnchorIch;

			public void TextSelInfo(bool fEndPoint, out ITsString ptss, out int ich, out bool fAssocPrev, out int hvoObj, out int tag, out int ws)
			{
				ptss = null;
				fAssocPrev = false;
				if (fEndPoint)
				{
					ich = EndIch;
					hvoObj = EndHvo;
					tag = EndTag;
					ws = EndWs;
				}
				else
				{
					ich = AnchorIch;
					hvoObj = AnchorHvo;
					tag = AnchorTag;
					ws = AnchorWs;
				}
			}

			public void GetSelectionProps(int cttpMax, ArrayPtr rgpttp, ArrayPtr rgpvps, out int cttp)
			{
				throw new NotSupportedException();
			}

			public void GetHardAndSoftCharProps(int cttpMax, ArrayPtr rgpttpSel, ArrayPtr rgpvpsSoft, out int cttp)
			{
				throw new NotSupportedException();
			}

			public void GetParaProps(int cttpMax, ArrayPtr rgpvps, out int cttp)
			{
				throw new NotSupportedException();
			}

			public void GetHardAndSoftParaProps(int cttpMax, ITsTextProps[] rgpttpPara, ArrayPtr rgpttpHard, ArrayPtr rgpvpsSoft, out int cttp)
			{
				throw new NotSupportedException();
			}

			public void SetSelectionProps(int cttp, ITsTextProps[] rgpttp)
			{
				throw new NotSupportedException();
			}

			public int CLevels(bool fEndPoint)
			{
				throw new NotSupportedException();
			}

			public void PropInfo(bool fEndPoint, int ilev, out int hvoObj, out int tag, out int ihvo, out int cpropPrevious, out IVwPropertyStore pvps)
			{
				throw new NotSupportedException();
			}

			public void AllTextSelInfo(out int ihvoRoot, int cvlsi, ArrayPtr rgvsli, out int tagTextProp, out int cpropPrevious, out int ichAnchor, out int ichEnd, out int ws, out bool fAssocPrev, out int ihvoEnd, out ITsTextProps pttp)
			{
				throw new NotSupportedException();
			}

			public void AllSelEndInfo(bool fEndPoint, out int ihvoRoot, int cvlsi, ArrayPtr rgvsli, out int tagTextProp, out int cpropPrevious, out int ich, out int ws, out bool fAssocPrev, out ITsTextProps pttp)
			{
				throw new NotSupportedException();
			}

			public bool CompleteEdits(out VwChangeInfo ci)
			{
				throw new NotSupportedException();
			}

			public void ExtendToStringBoundaries()
			{
				throw new NotSupportedException();
			}

			public void Location(IVwGraphics vg, Rect rcSrc, Rect rcDst, out Rect rdPrimary, out Rect rdSecondary, out bool fSplit, out bool fEndBeforeAnchor)
			{
				throw new NotSupportedException();
			}

			public void GetParaLocation(out Rect rdLoc)
			{
				throw new NotSupportedException();
			}

			public void ReplaceWithTsString(ITsString tss)
			{
				throw new NotSupportedException();
			}

			public void GetSelectionString(out ITsString ptss, string bstrSep)
			{
				throw new NotSupportedException();
			}

			public void GetFirstParaString(out ITsString ptss, string bstrSep, out bool fGotItAll)
			{
				throw new NotSupportedException();
			}

			public void SetIPLocation(bool fTopLine, int xdPos)
			{
				throw new NotSupportedException();
			}

			public void Install()
			{
				throw new NotSupportedException();
			}

			public bool get_Follows(IVwSelection sel)
			{
				throw new NotSupportedException();
			}

			public int get_ParagraphOffset(bool fEndPoint)
			{
				throw new NotSupportedException();
			}

			public IVwSelection GrowToWord()
			{
				throw new NotSupportedException();
			}

			public IVwSelection EndPoint(bool fEndPoint)
			{
				throw new NotSupportedException();
			}

			public void SetTypingProps(ITsTextProps ttp)
			{
				throw new NotSupportedException();
			}

			public int get_BoxDepth(bool fEndPoint)
			{
				throw new NotSupportedException();
			}

			public int get_BoxIndex(bool fEndPoint, int iLevel)
			{
				throw new NotSupportedException();
			}

			public int get_BoxCount(bool fEndPoint, int iLevel)
			{
				throw new NotSupportedException();
			}

			public VwBoxType get_BoxType(bool fEndPoint, int iLevel)
			{
				throw new NotSupportedException();
			}

			public bool IsRange => throw new NotSupportedException();

			public bool EndBeforeAnchor => throw new NotSupportedException();

			public bool CanFormatPara => throw new NotSupportedException();

			public bool CanFormatChar => throw new NotSupportedException();

			public bool CanFormatOverlay => throw new NotSupportedException();

			public bool IsValid => throw new NotImplementedException();

			public bool AssocPrev
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}


			public IVwRootBox RootBox => throw new NotSupportedException();

			public bool IsEditable => throw new NotSupportedException();

			public bool IsEnabled => throw new NotSupportedException();
		}
	}
}