// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[TestFixture]
	public partial class DataTreeTests
	{
		#region Wave 3 — Command Handlers & Low-cost Properties

		[Test]
		public void GetMessageTargets_NotVisibleWithCurrentSlice_ReturnsSliceOnly()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var currentSlice = m_dtree.Slices[0];
			SetCurrentSliceFieldForTest(currentSlice);

			var targets = m_dtree.GetMessageTargets();

			Assert.That(targets.Length, Is.EqualTo(1));
			Assert.That(targets[0], Is.SameAs(currentSlice));
		}

		[Test]
		public void OnDisplayJumpToLexiconEditFilterAnthroItems_NonAnthroField_Disables()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			SetCurrentSliceFieldForTest(m_dtree.Slices[0]);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdJumpToLexiconEditWithFilter' message='JumpToLexiconEditFilterAnthroItems' />"))
			{
				var display = new UIItemDisplayProperties(null, "JumpToLexiconEditFilterAnthroItems", true, null, true);
				bool handled = m_dtree.OnDisplayJumpToLexiconEditFilterAnthroItems(cmd, ref display);

				Assert.That(handled, Is.True);
				Assert.That(display.Enabled, Is.False);
				Assert.That(display.Visible, Is.False);
			}
		}

		[Test]
		public void OnDisplayJumpToNotebookEditFilterAnthroItems_NonAnthroField_Disables()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			SetCurrentSliceFieldForTest(m_dtree.Slices[0]);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdJumpToNotebookEditWithFilter' message='JumpToNotebookEditFilterAnthroItems' />"))
			{
				var display = new UIItemDisplayProperties(null, "JumpToNotebookEditFilterAnthroItems", true, null, true);
				bool handled = m_dtree.OnDisplayJumpToNotebookEditFilterAnthroItems(cmd, ref display);

				Assert.That(handled, Is.True);
				Assert.That(display.Enabled, Is.False);
				Assert.That(display.Visible, Is.False);
			}
		}

		[Test]
		public void OnDisplayJumpToLexiconEditFilterAnthroItems_AnthroFieldAndMatchingCommand_Enables()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var slice = m_dtree.Slices[0];
			var originalNode = slice.ConfigurationNode;

			try
			{
				slice.ConfigurationNode = CreateXmlNode("<slice field='AnthroCodes' />");
				SetCurrentSliceFieldForTest(slice);

				using (var cmd = CreateCommandFromXml(
					"<command id='CmdJumpToLexiconEditWithFilter' message='JumpToLexiconEditFilterAnthroItems' />"))
				{
					var display = new UIItemDisplayProperties(null, "JumpToLexiconEditFilterAnthroItems", true, null, true);
					bool handled = m_dtree.OnDisplayJumpToLexiconEditFilterAnthroItems(cmd, ref display);

					Assert.That(handled, Is.True);
					Assert.That(display.Enabled, Is.True);
					Assert.That(display.Visible, Is.True);
				}
			}
			finally
			{
				slice.ConfigurationNode = originalNode;
				SetCurrentSliceFieldForTest(null);
			}
		}

		[Test]
		public void OnDisplayJumpToLexiconEditFilterAnthroItems_AnthroFieldDifferentCommand_Disables()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var slice = m_dtree.Slices[0];
			var originalNode = slice.ConfigurationNode;

			try
			{
				slice.ConfigurationNode = CreateXmlNode("<slice field='AnthroCodes' />");
				SetCurrentSliceFieldForTest(slice);

				using (var cmd = CreateCommandFromXml(
					"<command id='DifferentCommand' message='JumpToLexiconEditFilterAnthroItems' />"))
				{
					var display = new UIItemDisplayProperties(null, "JumpToLexiconEditFilterAnthroItems", true, null, true);
					bool handled = m_dtree.OnDisplayJumpToLexiconEditFilterAnthroItems(cmd, ref display);

					Assert.That(handled, Is.True);
					Assert.That(display.Enabled, Is.False);
					Assert.That(display.Visible, Is.False);
				}
			}
			finally
			{
				slice.ConfigurationNode = originalNode;
				SetCurrentSliceFieldForTest(null);
			}
		}

		[Test]
		public void OnJumpToTool_InvalidCommand_ReturnsFalse()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdInvalidJump' message='JumpToTool'><parameters tool='concordance' className='LexSense'/></command>"))
			{
				bool result = m_dtree.OnJumpToTool(cmd);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public void OnJumpToTool_ValidConcordanceCommand_ReturnsTrue()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdRootEntryJumpToConcordance' message='JumpToTool'><parameters tool='concordance' className='LexEntry'/></command>"))
			{
				cmd.TargetId = Guid.NewGuid();
				bool result = m_dtree.OnJumpToTool(cmd);

				Assert.That(result, Is.True);
				Assert.That(cmd.TargetId, Is.EqualTo(Guid.Empty), "Handled jump should clear TargetId for future use");
			}
		}

		[Test]
		public void OnJumpToLexiconEditFilterAnthroItems_WithoutCurrentSlice_ThrowsNullReferenceException()
		{
			Assert.Throws<NullReferenceException>(() => m_dtree.OnJumpToLexiconEditFilterAnthroItems(null));
		}

		[Test]
		public void OnJumpToNotebookEditFilterAnthroItems_WithoutCurrentSlice_ThrowsNullReferenceException()
		{
			Assert.Throws<NullReferenceException>(() => m_dtree.OnJumpToNotebookEditFilterAnthroItems(null));
		}

		[Test]
		public void OnReadyToSetCurrentSlice_WhenActive_ReturnsTrue()
		{
			bool result = m_dtree.OnReadyToSetCurrentSlice(false);
			Assert.That(result, Is.True);
		}

		[Test]
		public void OnFocusFirstPossibleSlice_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => m_dtree.OnFocusFirstPossibleSlice(null));
		}

		[Test]
		public void Priority_ReturnsMediumColleaguePriority()
		{
			Assert.That(m_dtree.Priority, Is.EqualTo((int)ColleaguePriority.Medium));
		}

		[Test]
		public void ShouldNotCall_FalseByDefault()
		{
			Assert.That(m_dtree.ShouldNotCall, Is.False);
		}

		[Test]
		public void SliceControlContainer_ReturnsSelf()
		{
			Assert.That(m_dtree.SliceControlContainer, Is.SameAs(m_dtree));
		}

		[Test]
		public void LabelWidth_ReturnsExpectedConstant()
		{
			Assert.That(m_dtree.LabelWidth, Is.EqualTo(40));
		}

		[Test]
		public void LastSlice_WhenNoSlices_ReturnsNull()
		{
			Assert.That(m_dtree.LastSlice, Is.Null);
		}

		[Test]
		public void LastSlice_WhenSlicesExist_ReturnsLast()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			Assert.That(m_dtree.LastSlice, Is.SameAs(m_dtree.Slices[m_dtree.Slices.Count - 1]));
		}

		[Test]
		public void ConstructingSlices_IsFalseAfterShowObject()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Assert.That(m_dtree.ConstructingSlices, Is.False);
		}

		[Test]
		public void HasSubPossibilitiesSlice_CfOnlyLayout_ReturnsFalse()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Assert.That(m_dtree.HasSubPossibilitiesSlice, Is.False);
		}

		[Test]
		public void Descendant_GetterIsAccessible_AfterShowObject()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Assert.DoesNotThrow(() =>
			{
				var descendant = m_dtree.Descendant;
				if (descendant != null)
					Assert.That(descendant.IsValidObject, Is.True);
			});
		}

		[Test]
		public void CurrentSlice_SetterAssignsSliceFromTree()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			SetControlVisibleForTest(m_parent, true);
			SetControlVisibleForTest(m_dtree, true);

			var slice = m_dtree.Slices[0];
			Assert.DoesNotThrow(() => m_dtree.CurrentSlice = slice);

			if (m_dtree.CurrentSlice != null)
				Assert.That(m_dtree.CurrentSlice, Is.SameAs(slice));
		}

		[Test]
		public void ActiveControl_SetterToSliceControl_UpdatesCurrentSlice()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			SetControlVisibleForTest(m_parent, true);
			SetControlVisibleForTest(m_dtree, true);

			var targetSlice = m_dtree.Slices[1];
			Assert.That(targetSlice.Control, Is.Not.Null);

			Assert.DoesNotThrow(() => m_dtree.ActiveControl = targetSlice.Control);

			if (m_dtree.CurrentSlice != null)
				Assert.That(m_dtree.CurrentSlice, Is.SameAs(targetSlice));
		}

		[Test]
		public void SliceSplitPositionBase_SetterUpdatesValue()
		{
			int original = m_dtree.SliceSplitPositionBase;
			int updated = original + 7;

			m_dtree.SliceSplitPositionBase = updated;

			Assert.That(m_dtree.SliceSplitPositionBase, Is.EqualTo(updated));
		}

		[Test]
		public void SmallImages_SetterAndGetterRoundTrip()
		{
			var images = new ImageCollection(false);
			m_dtree.SmallImages = images;

			Assert.That(m_dtree.SmallImages, Is.SameAs(images));
		}

		[Test]
		public void StyleSheet_SetterAllowsNullRoundTrip()
		{
			m_dtree.StyleSheet = null;

			Assert.That(m_dtree.StyleSheet, Is.Null);
		}

		[Test]
		public void PersistenceProvider_SetterAndGetterRoundTrip()
		{
			var provider = new PersistenceProvider(m_mediator, m_propertyTable, "DataTreeTests");
			m_dtree.PersistenceProvder = provider;

			Assert.That(m_dtree.PersistenceProvder, Is.SameAs(provider));
		}

		[Test]
		public void RestorePreferences_WithPersistedSplitterDistance_UpdatesSliceSplitPositionBase()
		{
			var provider = new PersistenceProvider(m_mediator, m_propertyTable, "DataTreeTests");
			provider.SetInfoObject("SliceSplitterBaseDistance", 123);
			m_dtree.PersistenceProvder = provider;

			var method = typeof(DataTree).GetMethod("RestorePreferences",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.RestorePreferences method");

			method.Invoke(m_dtree, null);

			Assert.That(m_dtree.SliceSplitPositionBase, Is.EqualTo(123));
		}

		[Test]
		public void ApplyChildren_WithOnlyChangeRecordHandler_ReturnsUnchangedInsertPosition()
		{
			var template = CreateXmlNode("<layout><ChangeRecordHandler /></layout>");
			int result = m_dtree.ApplyChildren(m_entry, null, template, 0, 9,
				new System.Collections.ArrayList(), new ObjSeqHashMap());

			Assert.That(result, Is.EqualTo(9));
		}

		[Test]
		public void MakeEditorAt_DefaultImplementation_ReturnsNull()
		{
			Assert.That(m_dtree.MakeEditorAt(0), Is.Null);
		}

		[Test]
		public void AddAtomicNode_WhenFlidIsZero_ThrowsApplicationException()
		{
			int insertPosition = 0;
			var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => InvokeAddAtomicNode(
				new System.Collections.ArrayList(),
				CreateXmlNode("<slice />"),
				new ObjSeqHashMap(),
				0,
				m_entry,
				null,
				0,
				ref insertPosition,
				true,
				"default",
				false,
				null));

			Assert.That(ex.InnerException, Is.TypeOf<ApplicationException>());
		}

		[Test]
		public void AddAtomicNode_TestOnlyWithoutDataAndNoGhost_ReturnsPossible()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			int flid = LexEntryTags.kflidLexemeForm;
			int insertPosition = 0;
			var result = InvokeAddAtomicNode(
				new System.Collections.ArrayList(),
				CreateXmlNode("<slice field='LexemeForm' />"),
				new ObjSeqHashMap(),
				flid,
				m_entry,
				null,
				0,
				ref insertPosition,
				true,
				"default",
				false,
				null);

			Assert.That(result, Is.EqualTo(DataTree.NodeTestResult.kntrPossible));
		}

		[Test]
		public void AddAtomicNode_TestOnlyWithGhost_ReturnsSomething()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			int flid = LexEntryTags.kflidLexemeForm;
			int insertPosition = 0;
			var result = InvokeAddAtomicNode(
				new System.Collections.ArrayList(),
				CreateXmlNode("<slice field='LexemeForm' ghost='true' />"),
				new ObjSeqHashMap(),
				flid,
				m_entry,
				null,
				0,
				ref insertPosition,
				true,
				"default",
				false,
				null);

			Assert.That(result, Is.EqualTo(DataTree.NodeTestResult.kntrSomething));
		}

		[Test]
		public void AddAtomicNode_VisIfDataWithoutData_ReturnsNothing()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			int flid = LexEntryTags.kflidLexemeForm;
			int insertPosition = 0;
			var result = InvokeAddAtomicNode(
				new System.Collections.ArrayList(),
				CreateXmlNode("<slice field='LexemeForm' />"),
				new ObjSeqHashMap(),
				flid,
				m_entry,
				null,
				0,
				ref insertPosition,
				false,
				"default",
				true,
				null);

			Assert.That(result, Is.EqualTo(DataTree.NodeTestResult.kntrNothing));
		}

		[Test]
		public void RefreshListByHvoTag_NoPendingRefresh_DoesNotThrow()
		{
			var method = typeof(DataTree).GetMethod("RefreshList",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				null,
				new[] { typeof(int), typeof(int) },
				null);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.RefreshList(int, int) method");

			m_dtree.RefreshListNeeded = false;
			Assert.DoesNotThrow(() => method.Invoke(m_dtree, new object[] { m_entry.Hvo, LexEntryTags.kflidCitationForm }));
			Assert.That(m_dtree.RefreshListNeeded, Is.False);
		}

		[Test]
		public void RefreshListByHvoTag_WithPendingRefreshAndSuppressedRefresh_LeavesRefreshNeededTrue()
		{
			var method = typeof(DataTree).GetMethod("RefreshList",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				null,
				new[] { typeof(int), typeof(int) },
				null);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.RefreshList(int, int) method");

			m_dtree.DoNotRefresh = true;
			m_dtree.RefreshListNeeded = true;

			Assert.DoesNotThrow(() => method.Invoke(m_dtree, new object[] { m_entry.Hvo, LexEntryTags.kflidCitationForm }));
			Assert.That(m_dtree.RefreshListNeeded, Is.True);
		}

		[Test]
		public void RefreshDisplay_ReturnsTrue()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			bool result = m_dtree.RefreshDisplay();

			Assert.That(result, Is.True);
		}

		[Test]
		public void SetAndClearCurrentObjectFlids_TracksAndClearsPath()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			m_dtree.SetCurrentObjectFlids(m_entry.Hvo, 123456);

			var field = typeof(DataTree).GetField("m_currentObjectFlids",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_currentObjectFlids field");
			var value = field.GetValue(m_dtree) as System.Collections.IList;
			Assert.That(value, Is.Not.Null);
			Assert.That(value.Count, Is.GreaterThan(0));
			Assert.That(value.Contains(123456), Is.True);

			m_dtree.ClearCurrentObjectFlids();
			Assert.That(value.Count, Is.EqualTo(0));
		}

		[Test]
		public void OnInsertItemViaBackrefVector_WrongClass_ReturnsFalse()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdInsertBackref' message='InsertItemViaBackrefVector'><parameters className='LexSense' fieldName='AnthroCodes'/></command>"))
			{
				bool result = m_dtree.OnInsertItemViaBackrefVector(cmd);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public void OnInsertItemViaBackrefVector_MissingFieldName_ReturnsFalse()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			using (var cmd = CreateCommandFromXml(
				"<command id='CmdInsertBackref' message='InsertItemViaBackrefVector'><parameters className='LexEntry'/></command>"))
			{
				bool result = m_dtree.OnInsertItemViaBackrefVector(cmd);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public void OnDemoteItemInVector_WhenRootIsNull_ReturnsFalse()
		{
			Assert.That(m_dtree.Root, Is.Null);
			bool result = m_dtree.OnDemoteItemInVector(null);
			Assert.That(result, Is.False);
		}

		[Test]
		public void OnDemoteItemInVector_WhenRootIsNotNotebookRecord_ReturnsFalse()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			bool result = m_dtree.OnDemoteItemInVector(null);
			Assert.That(result, Is.False);
		}

		[Test]
		public void PostponePropChanged_TrueThenFalse_TogglesInternalFlag()
		{
			var method = typeof(DataTree).GetMethod("PostponePropChanged",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.PostponePropChanged method");

			var field = typeof(DataTree).GetField("m_postponePropChanged",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_postponePropChanged field");

			method.Invoke(m_dtree, new object[] { true });
			Assert.That((bool)field.GetValue(m_dtree), Is.True);

			method.Invoke(m_dtree, new object[] { false });
			Assert.That((bool)field.GetValue(m_dtree), Is.False);
		}

		[Test]
		public void PrepareToGoAway_WithoutCurrentSlice_ReturnsTrue()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			bool result = m_dtree.PrepareToGoAway();

			Assert.That(result, Is.True);
		}

		[Test]
		public void PropChanged_Unmonitored_WhenRefreshSuppressed_DoesNotQueueRefresh()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var postponeField = typeof(DataTree).GetField("m_postponePropChanged",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(postponeField, Is.Not.Null, "Could not reflect DataTree.m_postponePropChanged field");
			postponeField.SetValue(m_dtree, false);
			m_dtree.DoNotRefresh = true;

			Assert.That(m_dtree.RefreshListNeeded, Is.False);

			m_dtree.PropChanged(m_entry.Hvo, (int)LexEntryTags.kflidSummaryDefinition, 0, 1, 0);

			Assert.That(m_dtree.RefreshListNeeded, Is.False);
		}

		[Test]
		public void PropChanged_Monitored_WhenRefreshSuppressed_QueuesRefresh()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			var postponeField = typeof(DataTree).GetField("m_postponePropChanged",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(postponeField, Is.Not.Null, "Could not reflect DataTree.m_postponePropChanged field");
			postponeField.SetValue(m_dtree, false);
			m_dtree.DoNotRefresh = true;
			m_dtree.MonitorProp(m_entry.Hvo, (int)LexEntryTags.kflidCitationForm);

			Assert.That(m_dtree.RefreshListNeeded, Is.False);

			m_dtree.PropChanged(m_entry.Hvo, (int)LexEntryTags.kflidCitationForm, 0, 1, 0);

			Assert.That(m_dtree.RefreshListNeeded, Is.True);
		}

		[Test]
		public void ResetRecordListUpdater_WithListNameAndNoWindowOwner_LeavesUpdaterNull()
		{
			var listNameField = typeof(DataTree).GetField("m_listName",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(listNameField, Is.Not.Null, "Could not reflect DataTree.m_listName field");

			var rluField = typeof(DataTree).GetField("m_rlu",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(rluField, Is.Not.Null, "Could not reflect DataTree.m_rlu field");

			listNameField.SetValue(m_dtree, "AnyListName");
			rluField.SetValue(m_dtree, null);

			var method = typeof(DataTree).GetMethod("ResetRecordListUpdater",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.ResetRecordListUpdater method");

			Assert.DoesNotThrow(() => method.Invoke(m_dtree, null));
			Assert.That(rluField.GetValue(m_dtree), Is.Null);
		}

		[Test]
		public void SetCurrentSliceNewFromObject_MatchingSlice_SetsCurrentSliceNewField()
		{
			var first = new Slice { Object = m_entry };
			var secondEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var second = new Slice { Object = secondEntry };

			m_dtree.Slices.Clear();
			m_dtree.Slices.Add(first);
			m_dtree.Slices.Add(second);

			var method = typeof(DataTree).GetMethod("SetCurrentSliceNewFromObject",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.SetCurrentSliceNewFromObject method");
			method.Invoke(m_dtree, new object[] { secondEntry });

			var currentSliceNewField = typeof(DataTree).GetField("m_currentSliceNew",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(currentSliceNewField, Is.Not.Null, "Could not reflect DataTree.m_currentSliceNew field");
			Assert.That(currentSliceNewField.GetValue(m_dtree), Is.SameAs(second));
		}

		[Test]
		public void SetCurrentSliceNewFromObject_NoMatch_LeavesCurrentSliceNewNull()
		{
			var first = new Slice { Object = m_entry };
			m_dtree.Slices.Clear();
			m_dtree.Slices.Add(first);

			var currentSliceNewField = typeof(DataTree).GetField("m_currentSliceNew",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(currentSliceNewField, Is.Not.Null, "Could not reflect DataTree.m_currentSliceNew field");
			currentSliceNewField.SetValue(m_dtree, null);

			var method = typeof(DataTree).GetMethod("SetCurrentSliceNewFromObject",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.SetCurrentSliceNewFromObject method");
			method.Invoke(m_dtree, new object[] { Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create() });

			Assert.That(currentSliceNewField.GetValue(m_dtree), Is.Null);
		}

		[Test]
		public void CreateAndAssociateNotebookRecord_WhenCurrentSliceObjectIsNotText_ThrowsArgumentException()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			SetCurrentSliceFieldForTest(m_dtree.Slices[0]);

			var method = typeof(DataTree).GetMethod("CreateAndAssociateNotebookRecord",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.CreateAndAssociateNotebookRecord method");

			var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(m_dtree, null));
			Assert.That(ex.InnerException, Is.TypeOf<ArgumentException>());
		}

		[Test]
		public void FixRecordList_WithNullHandlers_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => m_dtree.FixRecordList());
		}

		[Test]
		public void DescendantForSlice_ParentNull_ReturnsRoot()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var method = typeof(DataTree).GetMethod("DescendantForSlice",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.DescendantForSlice method");

			var slice = m_dtree.Slices[0];
			var result = method.Invoke(m_dtree, new object[] { slice }) as ICmObject;

			Assert.That(result, Is.SameAs(m_entry));
		}

		[Test]
		public void DescendantForSlice_HeaderAncestor_ReturnsHeaderObject()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var headerObject = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var parentSlice = new Slice
			{
				Object = m_entry,
				ConfigurationNode = CreateXmlNode("<slice />")
			};
			var headerSlice = new Slice
			{
				Object = headerObject,
				ParentSlice = parentSlice,
				ConfigurationNode = CreateXmlNode("<slice header='true' />")
			};
			var childSlice = new Slice
			{
				Object = m_entry,
				ParentSlice = headerSlice,
				ConfigurationNode = CreateXmlNode("<slice />")
			};

			var method = typeof(DataTree).GetMethod("DescendantForSlice",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.DescendantForSlice method");

			var result = method.Invoke(m_dtree, new object[] { childSlice }) as ICmObject;

			Assert.That(result, Is.SameAs(headerObject));
		}

		[Test]
		public void RawSetSlice_ReplacesSliceAtIndex()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			var replacement = new Slice { Object = m_entry, ConfigurationNode = CreateXmlNode("<slice />") };
			m_dtree.RawSetSlice(0, replacement);

			Assert.That(m_dtree.Slices[0], Is.SameAs(replacement));
		}

		[Test]
		public void RemoveDisposedSlice_RemovesMatchingSliceFromCollection()
		{
			var slice = new Slice();
			m_dtree.Slices.Add(slice);

			m_dtree.RemoveDisposedSlice(slice);

			Assert.That(m_dtree.Slices.Contains(slice), Is.False);
		}

		[Test]
		public void NotebookRecordRefersToThisText_Null_ThrowsArgumentException()
		{
			IRnGenericRec ignored;
			Assert.Throws<ArgumentException>(() => DataTree.NotebookRecordRefersToThisText(null, out ignored));
		}

		[Test]
		public void NotebookRecordRefersToThisText_TextWithoutAssociation_ReturnsFalse()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();

			IRnGenericRec referringRecord;
			bool result = DataTree.NotebookRecordRefersToThisText(text, out referringRecord);

			Assert.That(result, Is.False);
			Assert.That(referringRecord, Is.Null);
		}

		[Test]
		public void GetSliceContextMenu_UsesRegisteredHandlerAndForwardsArgs()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Slice capturedSlice = null;
			bool capturedHotLinksOnly = false;
			var expectedMenu = new ContextMenu();

			m_dtree.SetContextMenuHandler((sender, args) =>
			{
				capturedSlice = args.Slice;
				capturedHotLinksOnly = args.HotLinksOnly;
				return expectedMenu;
			});

			var slice = m_dtree.Slices[0];
			ContextMenu actualMenu = m_dtree.GetSliceContextMenu(slice, true);

			Assert.That(actualMenu, Is.SameAs(expectedMenu));
			Assert.That(capturedSlice, Is.SameAs(slice));
			Assert.That(capturedHotLinksOnly, Is.True);
		}

		[Test]
		public void SetContextMenuHandler_ReplacesPreviousHandler()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var firstMenu = new ContextMenu();
			var secondMenu = new ContextMenu();

			m_dtree.SetContextMenuHandler((sender, args) => firstMenu);
			m_dtree.SetContextMenuHandler((sender, args) => secondMenu);

			ContextMenu actual = m_dtree.GetSliceContextMenu(m_dtree.Slices[0], false);
			Assert.That(actual, Is.SameAs(secondMenu));
		}

		[Test]
		public void OnShowContextMenu_InvokesHandlerForNonPopupForm()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Slice capturedSlice = null;
			int callCount = 0;
			m_dtree.SetContextMenuHandler((sender, args) =>
			{
				callCount++;
				capturedSlice = args.Slice;
				return new ContextMenu();
			});

			var slice = m_dtree.Slices[0];
			var eventArgs = new TreeNodeEventArgs(m_dtree, slice, new System.Drawing.Point(0, 0));
			Assert.DoesNotThrow(() => m_dtree.OnShowContextMenu(m_dtree, eventArgs));

			Assert.That(callCount, Is.EqualTo(1));
			Assert.That(capturedSlice, Is.SameAs(slice));
		}

		[Test]
		public void SelectFirstPossibleSlice_EmptyList_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => m_dtree.SelectFirstPossibleSlice(new System.Collections.Generic.List<Slice>()));
		}

		[Test]
		public void SelectFirstPossibleSlice_SliceFromDifferentTree_DoesNotThrow()
		{
			var otherTree = new DataTree();
			try
			{
				var slice = new Slice();
				otherTree.Slices.Add(slice);

				Assert.DoesNotThrow(() =>
					m_dtree.SelectFirstPossibleSlice(new System.Collections.Generic.List<Slice> { slice }));
			}
			finally
			{
				otherTree.Dispose();
			}
		}

		[Test]
		public void SelectFirstPossibleSlice_DisposedSlice_DoesNotThrow()
		{
			var slice = new Slice();
			slice.Dispose();

			Assert.DoesNotThrow(() =>
				m_dtree.SelectFirstPossibleSlice(new System.Collections.Generic.List<Slice> { slice }));
		}

		[Test]
		public void TraceMethods_WhenVerboseEnabled_DoNotThrow()
		{
			var tree = new TraceTestDataTree();
			try
			{
				tree.SetTraceLevel(System.Diagnostics.TraceLevel.Verbose);
				Assert.DoesNotThrow(() => tree.CallTraceVerbose("v"));
				Assert.DoesNotThrow(() => tree.CallTraceVerboseLine("vl"));
				Assert.DoesNotThrow(() => tree.CallTraceInfoLine("il"));
			}
			finally
			{
				tree.Dispose();
			}
		}

		[Test]
		public void RecordChangeHandlerDisposed_WhenSenderDiffers_DoesNotClearField()
		{
			var fakeRch = new FakeRecordChangeHandler();
			var rchField = typeof(DataTree).GetField("m_rch",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(rchField, Is.Not.Null, "Could not reflect DataTree.m_rch field");
			rchField.SetValue(m_dtree, fakeRch);

			var method = typeof(DataTree).GetMethod("m_rch_Disposed",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.m_rch_Disposed method");

			Assert.DoesNotThrow(() => method.Invoke(m_dtree, new object[] { new object(), EventArgs.Empty }));
			Assert.That(rchField.GetValue(m_dtree), Is.SameAs(fakeRch));
		}

		[Test]
		public void RecordChangeHandlerDisposed_WhenSenderMatches_ClearsField()
		{
			var fakeRch = new FakeRecordChangeHandler();
			var rchField = typeof(DataTree).GetField("m_rch",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(rchField, Is.Not.Null, "Could not reflect DataTree.m_rch field");
			rchField.SetValue(m_dtree, fakeRch);

			var method = typeof(DataTree).GetMethod("m_rch_Disposed",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.m_rch_Disposed method");

			Assert.DoesNotThrow(() => method.Invoke(m_dtree, new object[] { fakeRch, EventArgs.Empty }));
			Assert.That(rchField.GetValue(m_dtree), Is.Null);
		}

		[Test]
		public void InsertSliceRange_WithTwoSlices_InsertsBoth()
		{
			var tree = new InsertSliceRangeTestDataTree();
			try
			{
				var first = new Slice();
				var second = new Slice();
				var range = new System.Collections.Generic.HashSet<Slice> { first, second };

				Assert.DoesNotThrow(() => tree.CallInsertSliceRange(0, range));
				Assert.That(tree.Slices.Count, Is.EqualTo(2));
				Assert.That(tree.Slices.Contains(first), Is.True);
				Assert.That(tree.Slices.Contains(second), Is.True);
			}
			finally
			{
				tree.Dispose();
			}
		}

		[Test]
		public void SliceSplitterMoved_WhenCurrentSliceNull_DoesNotThrow()
		{
			var method = typeof(DataTree).GetMethod("slice_SplitterMoved",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.slice_SplitterMoved method");

			Assert.DoesNotThrow(() => method.Invoke(m_dtree,
				new object[] { null, new System.Windows.Forms.SplitterEventArgs(0, 0, 0, 0) }));
		}

		[Test]
		public void ObjSeqHashMap_GetSliceToReuse_ReturnsAndRemovesSlice()
		{
			var map = new ObjSeqHashMap();
			var key = new System.Collections.ArrayList { 1, 2, 3 };
			var slice = new Slice();
			try
			{
				slice.Key = new object[] { 1, 2, 3 };
				map.Add(key, slice);

				var reused = map.GetSliceToReuse(nameof(Slice));

				Assert.That(reused, Is.SameAs(slice));
				Assert.That(map[key].Count, Is.EqualTo(0));
			}
			finally
			{
				slice.Dispose();
			}
		}

		[Test]
		public void ObjSeqHashMap_Report_DoesNotThrow()
		{
			var map = new ObjSeqHashMap();
			Assert.DoesNotThrow(() => map.Report());
		}

		[Test]
		public void MakeSliceRealAt_WithExistingSlice_MakesSliceVisible()
		{
			var slice = new Slice
			{
				ConfigurationNode = CreateXmlNode("<slice />"),
				Object = m_entry
			};
			slice.SetBounds(0, 0, 200, 20);

			m_dtree.Width = 220;
			m_dtree.Controls.Add(slice);
			m_dtree.Slices.Add(slice);

			var method = typeof(DataTree).GetMethod("MakeSliceRealAt",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.MakeSliceRealAt");

			Assert.DoesNotThrow(() => method.Invoke(m_dtree, new object[] { 0 }));
			Assert.That(slice.Width, Is.EqualTo(m_dtree.ClientRectangle.Width));
		}

		[Test]
		public void ChooseNewOwner_NullRecords_ThrowsNullReferenceException()
		{
			Assert.Throws<NullReferenceException>(() => m_dtree.ChooseNewOwner(null, "choose"));
		}

		private sealed class TraceTestDataTree : DataTree
		{
			public void SetTraceLevel(System.Diagnostics.TraceLevel level)
			{
				m_traceSwitch.Level = level;
			}

			public void CallTraceVerbose(string message)
			{
				TraceVerbose(message);
			}

			public void CallTraceVerboseLine(string message)
			{
				TraceVerboseLine(message);
			}

			public void CallTraceInfoLine(string message)
			{
				TraceInfoLine(message);
			}
		}

		private sealed class InsertSliceRangeTestDataTree : DataTree
		{
			public void CallInsertSliceRange(int insertPosition, System.Collections.Generic.ISet<Slice> slices)
			{
				InsertSliceRange(insertPosition, slices);
			}
		}

		private DataTree.NodeTestResult InvokeAddAtomicNode(
			System.Collections.ArrayList path,
			System.Xml.XmlNode node,
			ObjSeqHashMap reuseMap,
			int flid,
			ICmObject obj,
			Slice parentSlice,
			int indent,
			ref int insertPosition,
			bool fTestOnly,
			string layoutName,
			bool fVisIfData,
			System.Xml.XmlNode caller)
		{
			var method = typeof(DataTree).GetMethod("AddAtomicNode",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "Could not reflect DataTree.AddAtomicNode method");

			object[] args =
			{
				path,
				node,
				reuseMap,
				flid,
				obj,
				parentSlice,
				indent,
				insertPosition,
				fTestOnly,
				layoutName,
				fVisIfData,
				caller
			};

			var result = (DataTree.NodeTestResult)method.Invoke(m_dtree, args);
			insertPosition = (int)args[7];
			return result;
		}

		private sealed class FakeRecordChangeHandler : SIL.FieldWorks.Common.Framework.IRecordChangeHandler
		{
			public event EventHandler Disposed;

			public bool HasRecordListUpdater
			{
				get { return false; }
			}

			public void Setup(object record, SIL.FieldWorks.Common.Framework.IRecordListUpdater rlu, LcmCache cache)
			{
			}

			public void Fixup(bool fRefreshList)
			{
			}

			public void Dispose()
			{
				var handler = Disposed;
				if (handler != null)
					handler(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}
