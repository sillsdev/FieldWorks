// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal class EntrySequenceReferenceLauncher : VectorReferenceLauncher
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;

		private System.ComponentModel.IContainer components;

		/// <summary />
		public EntrySequenceReferenceLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary />
		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new EntrySequenceVectorReferenceView();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				// Do this first, before setting m_fDisposing to true.
				components?.Dispose();
			}
			base.Dispose( disposing );
		}

		/// <summary />
		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_sda = m_cache.MainCacheAccessor;
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		protected override void HandleChooser()
		{
			switch (m_flid)
			{
				case LexEntryRefTags.kflidComponentLexemes:
					using (var dlg = new LinkEntryOrSenseDlg())
					{
						dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
						ILexEntry le;
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
						dlg.SetDlgInfo(m_cache, le);
						var str = ShowHelp.RemoveSpaces(this.Slice.Label);
						dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
						if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
						{
							AddItem(dlg.SelectedObject);
						}
					}

					break;
				case LexEntryRefTags.kflidPrimaryLexemes:
					var displayWs = "analysis vernacular";
					var node = m_configurationNode?.Element("deParams");
					if (node != null)
					{
						displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
					}
					var ler = m_obj as ILexEntryRef;
					Debug.Assert(ler != null);
					var labels = ObjectLabel.CreateObjectLabels(m_cache, ler.ComponentLexemesRS, m_displayNameProperty, displayWs);
					using (var chooser = new ReallySimpleListChooser(null, labels, "PrimaryLexemes", m_cache, ler.PrimaryLexemesRS, false, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
					{
						chooser.HideDisplayUsageCheckBox();
						chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
						chooser.Text = LanguageExplorerResources.ksChooseWhereToShowSubentry;
						chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
						chooser.InitializeExtras(null, PropertyTable);
						chooser.AddLink(LanguageExplorerResources.ksAddAComponent, LinkType.kDialogLink, new AddPrimaryLexemeChooserCommand(m_cache, false, null, PropertyTable, Publisher, Subscriber, m_obj, FindForm()));
						var res = chooser.ShowDialog();
						if (DialogResult.Cancel == res)
						{
							return;
						}

						if (chooser.ChosenObjects != null)
						{
							SetItems(chooser.ChosenObjects);
						}
					}

					break;
				default:
					var fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
					Debug.Assert(m_obj is ILexEntry || m_obj is ILexSense);
					switch(fieldName)
					{
						case "ComplexFormEntries":
							using (var dlg = new EntryGoDlg())
							{
								dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
								dlg.StartingEntry = m_obj as ILexEntry ?? (m_obj as ILexSense).Entry;
								dlg.SetDlgInfo(m_cache, null);
								var str = ShowHelp.RemoveSpaces(Slice.Label);
								dlg.SetHelpTopic("khtpChooseComplexFormEntryOrSense-" + str);
								dlg.SetOkButtonText(LanguageExplorerResources.ksMakeComponentOf);
								if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
								{
									try
									{
										UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAddComplexForm, LanguageExplorerResources.ksRedoAddComplexForm,
											m_obj.Cache.ActionHandlerAccessor,
											() => ((ILexEntry)dlg.SelectedObject).AddComponent(m_obj));
									}
									catch (ArgumentException)
									{
										MessageBoxes.ReportLexEntryCircularReference(dlg.SelectedObject, m_obj, false);
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

					break;
			}
		}

		private void HandleChooserForBackRefs(string fieldName, bool fPropContainsEntryRefs)
		{
			var displayWs = "analysis vernacular";
			IEnumerable<ICmObject> options = m_obj is ILexEntry ? ((ILexEntry)m_obj).ComplexFormEntries : ((ILexSense)m_obj).ComplexFormEntries;
			var oldValue = m_cache.GetManagedSilDataAccess().VecProp(m_obj.Hvo, m_flid)
				.Select(hvo => m_cache.ServiceLocator.GetObject(hvo));
			// We want a collection of LexEntries as the current values. If we're displaying lex entry refs we want their owners.
			if (fPropContainsEntryRefs)
			{
				oldValue = from obj in oldValue select obj.Owner;
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, options, m_displayNameProperty, displayWs);
			using (var chooser = new ReallySimpleListChooser(null,
				labels, fieldName, m_cache, oldValue,
				false, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.HideDisplayUsageCheckBox();
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
				chooser.Text = fieldName == "Subentries" ? LanguageExplorerResources.ksChooseSubentries : LanguageExplorerResources.ksChooseVisibleComplexForms;
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID() + "-CFChooser");
				chooser.InitializeExtras(null, PropertyTable);
				// Step 3 of LT-11155:
				chooser.AddLink(LanguageExplorerResources.ksAddAComplexForm, LinkType.kDialogLink, new AddComplexFormChooserCommand(m_cache, false, null, PropertyTable, Publisher, Subscriber, m_obj, FindForm()));
				var res = chooser.ShowDialog();
				if (DialogResult.Cancel == res)
				{
					return;
				}
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
			var fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
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
					{
						((ILexEntryRef)obj).ShowComplexFormsInRS.Add(m_obj);
					}
					break;
				default:
					base.AddNewObjectsToProperty(objectsToAdd);
					break;
			}

			if (m_flid != LexEntryRefTags.kflidComponentLexemes)
			{
				return;
			}
			{
				// Some special rules when adding to component lexemes here.
				// Logic similar to this is in GhostLexRefSlice.AddItem()
				// (when LER does not exist so we have a ghost slice)
				var ler = (ILexEntryRef)m_obj;
				if (ler.PrimaryLexemesRS.Count == 0)
				{
					ler.PrimaryLexemesRS.Add(objectsToAdd.First());
				}
				if (!ler.ComplexEntryTypesRS.Contains(ler.Services.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDerivation)))
				{
					foreach (var item in objectsToAdd)
					{
						// Don't add it twice!  See LT-12285.
						if (!ler.ShowComplexFormsInRS.Contains(item))
							ler.ShowComplexFormsInRS.Add(item);
					}
				}
			}
		}

		/// <summary />
		protected override void RemoveFromPropertyAt(int index, ICmObject oldObj)
		{
			var fieldName = m_obj.Cache.MetaDataCacheAccessor.GetFieldName(m_flid);
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
		private void ChangeItemsInLexEntryRefs(IEnumerable<ICmObject> objectsToAdd, Action<ILexEntryRef> handleItem)
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

		/// <summary />
		public override void AddItem(ICmObject obj)
		{
			var lexemes = new HashSet<ICmObject>();
			var ler = m_obj as ILexEntryRef;
			switch (m_flid)
			{
				case LexEntryRefTags.kflidComponentLexemes:
					lexemes.UnionWith(ler.ComponentLexemesRS);
					break;
				case LexEntryRefTags.kflidPrimaryLexemes:
					lexemes.UnionWith(ler.PrimaryLexemesRS);
					break;
			}
			// don't add a duplicate items.
			if (lexemes.Contains(obj))
			{
				return;
			}
			lexemes.Add(obj);
			try
			{
				SetItems(lexemes);
			}
			catch (ArgumentException)
			{
				MessageBoxes.ReportLexEntryCircularReference(m_obj.Owner, obj, true);
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
}