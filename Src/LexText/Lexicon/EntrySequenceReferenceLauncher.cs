// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class EntrySequenceReferenceLauncher : VectorReferenceLauncher
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;

		private System.ComponentModel.IContainer components = null;

		public EntrySequenceReferenceLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new EntrySequenceVectorReferenceView();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				// Do this first, before setting m_fDisposing to true.
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, Mediator mediator, PropertyTable propertyTable, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, propertyTable, displayNameProperty, displayWs);
			m_sda = m_cache.MainCacheAccessor;
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			if (m_flid == LexEntryRefTags.kflidComponentLexemes)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					if (m_obj.ClassID == LexEntryTags.kClassId)
					{
						// filter this entry from the list.
						le = m_obj as ILexEntry;
					}
					else
					{
						// assume the owner is the entry (e.g. owner of LexEntryRef)
						le = m_obj.OwnerOfClass<ILexEntry>();
					}
					dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, le);
					String str = ShowHelp.RemoveSpaces(this.Slice.Label);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
					if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
						AddItem(dlg.SelectedObject);
				}
			}
			else if (m_flid == LexEntryRefTags.kflidPrimaryLexemes)
			{
				string displayWs = "analysis vernacular";
				if (m_configurationNode != null)
				{
					XmlNode node = m_configurationNode.SelectSingleNode("deParams");
					if (node != null)
						displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
				}
				ILexEntryRef ler = m_obj as ILexEntryRef;
				Debug.Assert(ler != null);
				var labels = ObjectLabel.CreateObjectLabels(m_cache, ler.ComponentLexemesRS.Cast<ICmObject>(),
					m_displayNameProperty, displayWs);
				using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(null,
					labels, "PrimaryLexemes", m_cache, ler.PrimaryLexemesRS.Cast<ICmObject>(),
					false, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					chooser.HideDisplayUsageCheckBox();
					chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
					chooser.Text = LexEdStrings.ksChooseWhereToShowSubentry;
					chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
					chooser.InitializeExtras(null, Mediator, m_propertyTable);
					chooser.AddLink(LexEdStrings.ksAddAComponent, ReallySimpleListChooser.LinkType.kDialogLink,
						new AddPrimaryLexemeChooserCommand(m_cache, false, null, m_mediator, m_propertyTable, m_obj, FindForm()));
					DialogResult res = chooser.ShowDialog();
					if (DialogResult.Cancel == res)
						return;
					if (chooser.ChosenObjects != null)
						SetItems(chooser.ChosenObjects);
				}
			}
			else
			{
				string fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
				Debug.Assert(m_obj is ILexEntry || m_obj is ILexSense);
				switch(fieldName)
				{
					case "ComplexFormEntries":
						using (var dlg = new EntryGoDlg())
						{
							dlg.StartingEntry = m_obj as ILexEntry ?? (m_obj as ILexSense).Entry;
							dlg.SetDlgInfo(m_cache, null, m_mediator, m_propertyTable);
							String str = ShowHelp.RemoveSpaces(Slice.Label);
							dlg.SetHelpTopic("khtpChooseComplexFormEntryOrSense-" + str);
							dlg.SetOkButtonText(LexEdStrings.ksMakeComponentOf);
							if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
							{
								try
								{
									UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoAddComplexForm, LexEdStrings.ksRedoAddComplexForm,
										m_obj.Cache.ActionHandlerAccessor,
										() => ((ILexEntry)dlg.SelectedObject).AddComponent(m_obj));
								}
								catch (ArgumentException)
								{
									MessageBoxes.ReportLexEntryCircularReference((ILexEntry) dlg.SelectedObject, m_obj, false);
								}
							}
						}
						break;
					case "VisibleComplexFormEntries": // obsolete?
					case "Subentries":
						HandleChooserForBackRefs(fieldName, false);
						break;
					case "VisibleComplexFormBackRefs":
						HandleChooserForBackRefs(fieldName, true);
						break;
					default:
						Debug.Fail("EntrySequenceReferenceLauncher should only be used for variants, components, or complex forms");
						break;
				}
			}
		}

		private void HandleChooserForBackRefs(string fieldName, bool fPropContainsEntryRefs)
		{
			string displayWs = "analysis vernacular";
			IEnumerable<ICmObject> options;
			if (m_obj is ILexEntry)
				options = ((ILexEntry) m_obj).ComplexFormEntries.Cast<ICmObject>();
			else
				options = ((ILexSense)m_obj).ComplexFormEntries.Cast<ICmObject>();
			var oldValue = from hvo in ((ISilDataAccessManaged) m_cache.DomainDataByFlid).VecProp(m_obj.Hvo, m_flid)
				select m_cache.ServiceLocator.GetObject(hvo);
			// We want a collection of LexEntries as the current values. If we're displaying lex entry refs we want their owners.
			if (fPropContainsEntryRefs)
				oldValue = from obj in oldValue select obj.Owner;

			var labels = ObjectLabel.CreateObjectLabels(m_cache, options,
				m_displayNameProperty, displayWs);
			using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(null,
				labels, fieldName, m_cache, oldValue,
				false, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.HideDisplayUsageCheckBox();
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
				chooser.Text = fieldName == "Subentries" ? LexEdStrings.ksChooseSubentries : LexEdStrings.ksChooseVisibleComplexForms;
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID() + "-CFChooser");
				chooser.InitializeExtras(null, Mediator, m_propertyTable);
				// Step 3 of LT-11155:
				chooser.AddLink(LexEdStrings.ksAddAComplexForm, ReallySimpleListChooser.LinkType.kDialogLink,
					new AddComplexFormChooserCommand(m_cache, false, null, m_mediator, m_propertyTable, m_obj, FindForm()));
				DialogResult res = chooser.ShowDialog();
				if (DialogResult.Cancel == res)
					return;
				var chosenObjects = chooser.ChosenObjects;
				if (chosenObjects != null)
				{
					if (fPropContainsEntryRefs)
					{
						chosenObjects = from ILexEntry le in chosenObjects
							from ler in le.EntryRefsOS
							where ler.RefType == LexEntryRefTags.krtComplexForm
							select (ICmObject) ler;
					}
					SetItems(chosenObjects);
				}
			}
		}

		/// <summary>
		/// Special means of adding objects to backref properties.
		/// </summary>
		/// <param name="objectsToAdd"></param>
		protected override void AddNewObjectsToProperty(IEnumerable<ICmObject> objectsToAdd)
		{
			string fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
			// Note that the attempt to add to compoments may fail, due to creating a circular reference.
			// This is caught further out, outside the UOW, so the UOW will be properly rolled back.
			switch (fieldName)
			{
				case "VisibleComplexFormEntries":
					ChangeItemsInLexEntryRefs(objectsToAdd, ler => ler.ShowComplexFormsInRS.Add(m_obj));
					break;
				case "Subentries":
					ChangeItemsInLexEntryRefs(objectsToAdd, ler => ler.PrimaryLexemesRS.Add(m_obj));
					break;
				case "VisibleComplexFormBackRefs":
					foreach (var obj in objectsToAdd)
						((ILexEntryRef)obj).ShowComplexFormsInRS.Add(m_obj);
					break;
				default:
					base.AddNewObjectsToProperty(objectsToAdd);
					break;
			}
			if (m_flid == LexEntryRefTags.kflidComponentLexemes)
			{
				// Some special rules when adding to component lexemes here.
				// Logic similar to this is in GhostLexRefSlice.AddItem()
				// (when LER does not exist so we have a ghost slice)
				var ler = (ILexEntryRef)m_obj;
				if (ler.PrimaryLexemesRS.Count == 0)
					ler.PrimaryLexemesRS.Add(objectsToAdd.First());
				if (!ler.ComplexEntryTypesRS.Contains(ler.Services.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDerivation)))
					foreach (var item in objectsToAdd)
					{
						// Don't add it twice!  See LT-12285.
						if (!ler.ShowComplexFormsInRS.Contains(item))
							ler.ShowComplexFormsInRS.Add(item);
					}
			}
		}

		protected override void RemoveFromPropertyAt(int index, ICmObject oldObj)
		{
			string fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
			switch (fieldName)
			{
				case "VisibleComplexFormEntries":
					ChangeItemsInLexEntryRefs(new [] {oldObj}, ler => ler.ShowComplexFormsInRS.Remove(m_obj));
					break;
				case "Subentries":
					ChangeItemsInLexEntryRefs(new[] { oldObj }, ler => ler.PrimaryLexemesRS.Remove(m_obj));
					break;
				case "VisibleComplexFormBackRefs":
					((ILexEntryRef) oldObj).ShowComplexFormsInRS.Remove(m_obj);
					break;
				default:
					base.RemoveFromPropertyAt(index, oldObj);
					break;
			}
		}

		/// <summary>
		/// Do something (typically add or remove m_obj from a property) with each item in the list,
		/// finding the LexEntryRef on that item which is a complex one and has item as a component.
		/// </summary>
		void ChangeItemsInLexEntryRefs(IEnumerable<ICmObject> objectsToAdd, Action<ILexEntryRef> handleItem)
		{
			foreach (var item in objectsToAdd)
			{
				// We expect that item is a LexEntry which has a complex lex entry ref pointing at our
				// m_obj. Find that LER and add item to the appropriate property using addItem.
				var target = (from ler in ((ILexEntry) item).EntryRefsOS
							  where ler.ComponentLexemesRS.Contains(m_obj) && ler.RefType == LexEntryRefTags.krtComplexForm
							  select ler).First();
					handleItem(target);
			}
		}

		public override void AddItem(ICmObject obj)
		{
			CheckDisposed();

			var lexemes = new HashSet<ICmObject>();
			ILexEntryRef ler = m_obj as ILexEntryRef;
			if (m_flid == LexEntryRefTags.kflidComponentLexemes)
				lexemes.UnionWith(ler.ComponentLexemesRS);
			else if (m_flid == LexEntryRefTags.kflidPrimaryLexemes)
				lexemes.UnionWith(ler.PrimaryLexemesRS);
			// don't add a duplicate items.
			if (!lexemes.Contains(obj))
			{
				lexemes.Add(obj);
				try
				{
					SetItems(lexemes);
				}
				catch (ArgumentException)
				{
					MessageBoxes.ReportLexEntryCircularReference((ILexEntry)m_obj.Owner, obj, true);
				}
			}
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_parentWindow is a reference")]
	internal class AddPrimaryLexemeChooserCommand : ChooserCommand
	{
		private readonly ILexEntryRef m_lexEntryRef;
		private readonly Form m_parentWindow;

		public AddPrimaryLexemeChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, Mediator mediator, PropertyTable propertyTable, ICmObject lexEntryRef, /* Why ICmObject? */
			Form parentWindow)
			: base(cache, fCloseBeforeExecuting, sLabel, mediator, propertyTable)
		{
			m_lexEntryRef = lexEntryRef as ILexEntryRef;
			m_parentWindow = parentWindow;
		}

		public override ObjectLabel Execute()
		{
			ObjectLabel result = null;
			if (m_lexEntryRef != null)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					// assume the owner is the entry (e.g. owner of LexEntryRef)
					le = m_lexEntryRef.OwnerOfClass<ILexEntry>();
					dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, le);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense");
					if (dlg.ShowDialog(m_parentWindow) == DialogResult.OK)
					{
						ICmObject obj = dlg.SelectedObject;
						if (obj != null)
						{
							if (!m_lexEntryRef.PrimaryLexemesRS.Contains(obj))
							{
								try
								{
									UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
										LexEdStrings.ksUndoCreatingEntry,
										LexEdStrings.ksRedoCreatingEntry,
										Cache.ActionHandlerAccessor,
										() =>
											{
												if (!m_lexEntryRef.ComponentLexemesRS.Contains(obj))
													m_lexEntryRef.ComponentLexemesRS.Add(obj);
												m_lexEntryRef.PrimaryLexemesRS.Add(obj);
											});
								}
								catch (ArgumentException)
								{
									MessageBoxes.ReportLexEntryCircularReference((ILexEntry) m_lexEntryRef.Owner, obj, true);
								}
							}
						}
					}
				}
			}
			return result;
		}
	}

	/// <summary>
	/// This is a clone of internal class AddComponentChooserCommand above.
	/// There are 2 key differences:
	/// 1) It expects an ILexEntry instead of an ILexEntryRef;
	/// 2) It displays the EntryGoDlg instead of the LinkEntryOrSenseDlg.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_parentWindow is a reference")]
	internal class AddComplexFormChooserCommand : ChooserCommand
	{
		private readonly ILexEntry m_lexEntry;
		private readonly ILexSense m_lexSense;
		private readonly Form m_parentWindow;

		public AddComplexFormChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, Mediator mediator, PropertyTable propertyTable, ICmObject lexEntry, /* Why ICmObject? */
			Form parentWindow)
			: base(cache, fCloseBeforeExecuting, sLabel, mediator, propertyTable)
		{
			m_lexEntry = lexEntry as ILexEntry;
			if (m_lexEntry == null)
			{
				m_lexSense = lexEntry as ILexSense;
				if (m_lexSense != null)
					m_lexEntry = m_lexSense.Entry;

			}
			m_parentWindow = parentWindow;
		}

		public override ObjectLabel Execute()
		{
			ObjectLabel result = null;
			if (m_lexEntry != null)
			{
				using (var dlg = new EntryGoDlg())
				{
					dlg.SetDlgInfo(m_cache, null, m_mediator, m_propertyTable);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense"); // TODO: When LT-11318 is fixed, use its help topic ID.
					dlg.SetOkButtonText(LexEdStrings.ksMakeComponentOf);
					if (dlg.ShowDialog(m_parentWindow) == DialogResult.OK)
					{
						try
						{
							if (m_lexSense != null)
							{
								UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoAddComplexForm, LexEdStrings.ksRedoAddComplexForm,
								m_lexEntry.Cache.ActionHandlerAccessor,
								() => ((ILexEntry)dlg.SelectedObject).AddComponent((ICmObject)m_lexSense ?? m_lexEntry));
							}
							else
							{
								UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoAddComplexForm, LexEdStrings.ksRedoAddComplexForm,
								m_lexEntry.Cache.ActionHandlerAccessor,
								() => ((ILexEntry)dlg.SelectedObject).AddComponent(m_lexEntry));
							}
						}
						catch (ArgumentException)
						{
							MessageBoxes.ReportLexEntryCircularReference((ILexEntry)dlg.SelectedObject, m_lexEntry, false);
						}
					}
				}
			}
			return result;
		}
	}

	/// <summary>
	/// Subclass VectorReferenceView to support deleting from the (virtual) Complex Forms property and similar.
	/// </summary>
	class EntrySequenceVectorReferenceView: VectorReferenceView
	{
		protected override void RemoveObjectFromList(int[] hvosOld, int ihvo, string undoText, string redoText)
		{
			if (!Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid))
			{
				base.RemoveObjectFromList(hvosOld, ihvo, undoText, redoText);
				return;
			}
			if (Cache.MetaDataCacheAccessor.GetFieldName(m_rootFlid) != "ComplexFormEntries")
				return;
			int startHeight = m_rootb.Height;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, m_rootObj,
				() =>
					{
						var complex = m_rootObj.Services.GetInstance<ILexEntryRepository>().GetObject(hvosOld[ihvo]);
						// the selected object in the list is a complex entry which has this as one of its components.
						// We want to remove this from its components.
						var ler =
							(from item in complex.EntryRefsOS where item.RefType == LexEntryRefTags.krtComplexForm select item).
								First();
						ler.PrimaryLexemesRS.Remove(m_rootObj);
						ler.ShowComplexFormsInRS.Remove(m_rootObj);
						ler.ComponentLexemesRS.Remove(m_rootObj);
					});
			if (m_rootb != null)
			{
				CheckViewSizeChanged(startHeight, m_rootb.Height);
				// Redisplay (?) the vector property.
				m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
			}
		}

		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete.  If the problem is a "complex range", then try to delete one
		/// object from the vector displayed in the entry sequence.
		/// </summary>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			if (dpt == VwDelProbType.kdptComplexRange)
			{
				var helper = SelectionHelper.GetSelectionInfo(sel, this);
				var clev = helper.NumberOfLevels;
				var rginfo = helper.LevelInfo;
				var info = rginfo[clev - 1];
				ICmObject cmo;
				if (info.tag == m_rootFlid &&
					m_fdoCache.ServiceLocator.ObjectRepository.TryGetObject(info.hvo, out cmo))
				{
					var sda = m_fdoCache.DomainDataByFlid as ISilDataAccessManaged;
					Debug.Assert(sda != null);
					var rghvos = sda.VecProp(m_rootObj.Hvo, m_rootFlid);
					var ihvo = -1;
					for (var i = 0; i < rghvos.Length; ++i)
					{
						if (rghvos[i] == cmo.Hvo)
						{
							ihvo = i;
							break;
						}
					}
					if (ihvo >= 0)
					{
						var startHeight = m_rootb.Height;
						if (Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid))
						{
							var obj = m_fdoCache.ServiceLocator.GetObject(rghvos[ihvo]);
							ILexEntryRef ler = null;
							if (obj is ILexEntry)
							{
								var complex = (ILexEntry)obj;
								// the selected object in the list is a complex entry which has this as one of
								// its components.  We want to remove this from its components.
								foreach (var item in complex.EntryRefsOS)
								{
									switch (item.RefType)
									{
										case LexEntryRefTags.krtComplexForm:
										case LexEntryRefTags.krtVariant:
											ler = item;
											break;
										default:
											throw new Exception("Unexpected LexEntryRef type in EntrySequenceVectorReferenceView.OnProblemDeletion");
									}
								}
							}
							else if (obj is ILexEntryRef)
							{
								ler = (ILexEntryRef) obj;
							}
							else
							{
								return VwDelProbResponse.kdprAbort; // we don't know how to delete it.
							}
							var fieldName = m_fdoCache.MetaDataCacheAccessor.GetFieldName(m_rootFlid);
							if (fieldName == "Subentries")
							{
								ler.PrimaryLexemesRS.Remove(m_rootObj);
							}
							else if (fieldName == "VisibleComplexFormEntries" || fieldName == "VisibleComplexFormBackRefs")
							{
								ler.ShowComplexFormsInRS.Remove(m_rootObj);
							}
							else if (fieldName == "VariantFormEntries")
							{
								ler.ComponentLexemesRS.Remove(m_rootObj);
							}
						}
						else
						{
							sda.Replace(m_rootObj.Hvo, m_rootFlid, ihvo, ihvo + 1, new int[0], 0);
						}
						if (m_rootb != null)
						{
							CheckViewSizeChanged(startHeight, m_rootb.Height);
							// Redisplay (?) the vector property.
							m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector,
								m_rootb.Stylesheet);
						}
						return VwDelProbResponse.kdprDone;
					}
				}
			}
			return base.OnProblemDeletion(sel, dpt);
		}
	}
}
