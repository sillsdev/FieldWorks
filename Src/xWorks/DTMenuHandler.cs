// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2003-2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: .cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// DTMenuHandler provides context menus to the data tree.  When the user (or test code)
	/// selects issues commands, this class also invokes the corresponding methods on the data tree.
	/// You may create subclasses to do smart things with menus.
	/// </summary>
	/// <remarks>
	/// Although XWorks doesn't sound Flex-specific, most of the menu commands handled in this
	/// file are specific to Flex.
	/// </remarks>
	public class DTMenuHandler: IxCoreColleague
	{
		/// <summary>
		/// Tree form.
		/// </summary>
		protected DataTree m_dataEntryForm;

		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected XCore.Mediator m_mediator;

		/// <summary>
		/// COnfiguration information.
		/// </summary>
		protected XmlNode m_configuration;


		/// <summary>
		/// factory method which creates the correct subclass based on the XML parameters
		/// </summary>
		/// <param name="dataEntryForm"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static DTMenuHandler Create(DataTree dataEntryForm, XmlNode configuration)
		{
			DTMenuHandler h= null;
			if(configuration !=null)
			{
				XmlNode node = configuration.SelectSingleNode("menuHandler/dynamicloaderinfo");
				if (node != null)
				{
					h = (DTMenuHandler) SIL.Utils.DynamicLoader.CreateObject(node);
				}
			}
			if(h==null)			//no class specified, so just returned a generic DTMenuHandler
				h = new DTMenuHandler();
			h.DtTree = dataEntryForm;
			return h;
		}


		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public StringTable StringTbl
		{
			get
			{
				return m_mediator.StringTbl;
			}
		}

		public DataTree DtTree
		{
			set
			{
				m_dataEntryForm = value;
			}
		}

		protected DTMenuHandler()
		{
		}

		#region IxCoreColleague implementation

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_configuration = configurationParameters;
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			//if the slice implements IxCoreColleague, than it is one of our sub colleagues
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice != null)
					return new IxCoreColleague[] { (IxCoreColleague)slice, this };
			else
				return new IxCoreColleague[] { this };
		}


		/// <summary>
		/// No known case where this is not valid to call.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.High; }
		}

		#endregion

		/// <summary>
		/// Called by reflection based on menu item InsertPicture.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnInsertPicture(object cmd)
		{
			int flid;
			if (!CanInsertPictureOrMediaFile(cmd, out flid))
				return false; // should not happen, but play safe
			var obj = m_dataEntryForm.CurrentSlice.Object;
			int chvo = obj.Cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			using (PicturePropertiesDialog dlg = new PicturePropertiesDialog(obj.Cache, null,
				m_mediator.HelpTopicProvider, app, true))
			{
				if (dlg.Initialize())
				{
					var stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
					dlg.UseMultiStringCaption(obj.Cache, WritingSystemServices.kwsVernAnals, stylesheet);
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoInsertPicture, xWorksStrings.ksRedoInsertPicture, obj, () =>
						{
							string strLocalPictures = CmFolderTags.DefaultPictureFolder;
							int hvoPic = obj.Cache.DomainDataByFlid.MakeNewObject(CmPictureTags.kClassId, obj.Hvo, flid, chvo);
							var picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(hvoPic);
							dlg.GetMultilingualCaptionValues(picture.Caption);
							picture.UpdatePicture(dlg.CurrentFile, null, strLocalPictures, 0);
						});
					}
				}
			}
			return true;
		}

		private bool CanInsertPictureOrMediaFile(object cmd, out int flid)
		{
			Command command = (Command) cmd;
			string field = command.GetParameter("field");
			string className = command.GetParameter("className");
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.IsDisposed)	// LT-3347: there are no slices in this empty data set
			{
				flid = 0;
				return false;
			}
			var obj = current.Object;
			if (obj == null || !obj.IsValidObject)
			{
				// Something has gone horribly wrong, but let's not die.
				flid = 0;
				return false;
			}
			IFwMetaDataCache mdc = obj.Cache.DomainDataByFlid.MetaDataCache;
			int clid = 0;
			try { clid = mdc.GetClassId(className); }
			catch { throw new ConfigurationException("Unknown class for insert command: " + className); }
			try { flid = mdc.GetFieldId2(clid, field, true); }
			catch { throw new ConfigurationException("Unknown field: " + className + "." + field); }
			int clidObj = obj.ClassID;
			return (clidObj == clid); // enhance JohnT: we could allow clidObj to be a subclass of clid.
		}

		/// <summary>
		/// Determine whether we can insert a picture here.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertPicture(object commandObject,
			ref UIItemDisplayProperties display)
		{
			int flid;
			display.Enabled = CanInsertPictureOrMediaFile(commandObject, out flid);
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Called by reflection based on menu item InsertSoundFile.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnInsertMediaFile(object cmd)
		{
			string filter = ResourceHelper.BuildFileFilter(FileFilterType.AllAudio,
				FileFilterType.AllVideo, FileFilterType.AllFiles);
			string keyCaption = "kstidInsertMediaChooseFileCaption";
			string defaultCaption = xWorksStrings.ChooseSoundOrMovieFile;
			return InsertMediaFile(cmd, filter, keyCaption, defaultCaption);
		}

		private bool InsertMediaFile(object cmd, string filter, string keyCaption, string defaultCaption)
		{
			ICmObject obj;
			int flid;
			bool fOnPronunciationSlice = CanInsertPictureOrMediaFile(cmd, out flid);
			if (fOnPronunciationSlice)
			{
				obj = m_dataEntryForm.CurrentSlice.Object;
			}
			else
			{
				// Find the first pronunciation object on the current entry, creating it if
				// necessary.
				var le = m_dataEntryForm.Root as ILexEntry;
				if (le == null)
					return false;
				if (le.PronunciationsOS.Count == 0)
				{
					// Ensure that the pronunciation writing systems have been initialized.
					// Otherwise, the crash reported in FWR-2086 can happen!
					int wsPron = Cache.DefaultPronunciationWs;
					UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo Create Pronuciation",
						"Redo Create Pronunciation", Cache.ActionHandlerAccessor, () =>
						{
							le.PronunciationsOS.Add(Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create());
						});
				}
				obj = le.PronunciationsOS[0];
				flid = LexPronunciationTags.kflidMediaFiles;
			}
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.InitialDirectory = Cache.LangProject.LinkedFilesRootDir;
				dlg.Filter = filter;
				dlg.FilterIndex = 1;
				if (m_mediator != null && m_mediator.HasStringTable)
					dlg.Title = m_mediator.StringTbl.GetString(keyCaption);
				if (string.IsNullOrEmpty(dlg.Title) || dlg.Title == "*" + keyCaption + "*")
					dlg.Title = defaultCaption;
				dlg.RestoreDirectory = true;
				dlg.CheckFileExists = true;
				dlg.CheckPathExists = true;

				DialogResult dialogResult = DialogResult.None;
				while (dialogResult != DialogResult.OK && dialogResult != DialogResult.Cancel)
				{
					dialogResult = dlg.ShowDialog();
					if (dialogResult == DialogResult.OK)
					{
						string file = MoveOrCopyFilesDlg.MoveCopyOrLeaveMediaFile(dlg.FileName,
							Cache.LangProject.LinkedFilesRootDir, m_mediator.HelpTopicProvider, Cache.ProjectId.IsLocal);
						if (String.IsNullOrEmpty(file))
							return true;
						string sFolderName = null;
						if (m_mediator != null && m_mediator.HasStringTable)
							sFolderName = m_mediator.StringTbl.GetString("kstidMediaFolder");
						if (sFolderName == null || sFolderName.Length == 0 || sFolderName == "*kstidMediaFolder*")
							sFolderName = CmFolderTags.LocalMedia;
						if (!obj.IsValidObject)
							return true; // Probably some other client deleted it while we were choosing the file.
						int chvo = obj.Cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
						UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
							xWorksStrings.ksUndoInsertMedia, xWorksStrings.ksRedoInsertMedia,
							Cache.ActionHandlerAccessor, () =>
							{
								int hvo = obj.Cache.DomainDataByFlid.MakeNewObject(CmMediaTags.kClassId, obj.Hvo, flid, chvo);
								var media = Cache.ServiceLocator.GetInstance<ICmMediaRepository>().GetObject(hvo);
								var folder = DomainObjectServices.FindOrCreateFolder(obj.Cache, LangProjectTags.kflidMedia, sFolderName);
								media.MediaFileRA = DomainObjectServices.FindOrCreateFile(folder, file);
							});
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Check whether or not to display the "Insert Sound or Movie" command.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertMediaFile(object commandObject,
			ref UIItemDisplayProperties display)
		{
			// exact same logic as for inserting a picture
			return OnDisplayInsertPicture(commandObject, ref display);
		}

		public bool OnDeleteMediaFile(object cmd)
		{
			var obj = m_dataEntryForm.CurrentSlice.Object;
			var media = obj as ICmMedia;
			if (media != null)
			{
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					xWorksStrings.ksUndoDeleteMediaLink,
					xWorksStrings.ksRedoDeleteMediaLink,
					Cache.ActionHandlerAccessor,
					() =>
					{
						CmObjectUi.ConsiderDeletingRelatedFile(media.MediaFileRA, m_mediator);
						Cache.DomainDataByFlid.DeleteObj(media.Hvo);
					});
			}
			return true;
		}

		/// <summary>
		/// Check whether or not to display the "Delete This Media Link" command.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDeleteMediaFile(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			return true;
		}

		public bool OnDataTreeHelp(object cmd)
		{
			string helpTopicID = null;
			if (m_dataEntryForm.CurrentSlice != null)
				helpTopicID = m_dataEntryForm.CurrentSlice.GetSliceHelpTopicID();
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, helpTopicID);

			return true;
		}

		public bool OnDisplayDataTreeHelp(object cmd, UIItemDisplayProperties display)
		{
			// Only display help if there's a topic linked to the generated ID in the resource file
			string helpTopicID = null;
			if (m_dataEntryForm.CurrentSlice != null)
				helpTopicID = m_dataEntryForm.CurrentSlice.GetSliceHelpTopicID();
			display.Visible = display.Enabled = (m_mediator.HelpTopicProvider.GetHelpString(helpTopicID) != null);

			return true;
		}

		/// <summary>
		/// This method is called when a user selects an Insert operation in on a slice.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnDataTreeInsert(object cmd)
		{
			Command command = (Command) cmd;
			string field = command.GetParameter("field");
			string className = command.GetParameter("className");
			string ownerClassName = command.GetParameter("ownerClass", "");
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current != null)
			{
				current.Validate();
				if (current.Control is ContainerControl)
					((ContainerControl)current.Control).Validate();

			}
			if (current == null && m_dataEntryForm.Slices.Count > 0)
				current = m_dataEntryForm.FieldAt(0);
			string sliceName = command.GetParameter("slice", "");
			if (String.IsNullOrEmpty(ownerClassName) && current != null && current.Object != null)
			{
				var owner = current.Object.Owner;
				if (owner != null && owner.IsValidObject)
				{
					int clid = owner.ClassID;
					if (clid > 0)
						ownerClassName = owner.ClassName;
				}
			}
			if (sliceName != null && sliceName == "owner" && current != null)
			{
				// Find a slice corresponding to the current slice's owner's object.
				var cmo = current.Object;
				foreach (Slice slice in current.ContainingDataTree.Slices)
				{
					if (slice.Object == cmo.Owner)
					{
						current = slice;
						break;
					}
				}
			}
			if (current == null && m_dataEntryForm.Slices.Count > 0)
				current = m_dataEntryForm.Slices[0] as Slice;
			if (current == null || !current.Object.IsValidObject)
				return false;
			// If we're trying to replace a ghost slice, there could be trouble...
			if (current.IsGhostSlice && SliceConfiguredForField(current.ConfigurationNode, field))
			{
				// We can't let the current slice handle the insert because it blows up (see
				// LT-4725).  (This is because the ghost slice is deleted and disposed of in
				// the process of creating and inserting the real slice containing the newly
				// created real object.)  Try using an adjacent slice on the same object,
				// preferably a preceding one.
				Debug.Assert(current.Object != null);
				int hvoObject = current.Object.Hvo;
				int iNew = -1;
				for (int i = current.IndexInContainer - 1; i >= 0; --i)
				{
					if ((current.ContainingDataTree.Slices[i] as Slice).Object != null &&
						(current.ContainingDataTree.Slices[i] as Slice).Object.Hvo == hvoObject)
					{
						iNew = i;
						break;
					}
				}
				if (iNew == -1)
				{
					int cslice = current.ContainingDataTree.Slices.Count;
					for (int i = current.IndexInContainer + 1; i < cslice; ++i)
					{
						if ((current.ContainingDataTree.Slices[i] as Slice).Object != null &&
							(current.ContainingDataTree.Slices[i] as Slice).Object.Hvo == hvoObject)
						{
							iNew = i;
							break;
						}
					}
				}
				if (iNew == -1)
				{
					Logger.WriteEvent(String.Format(
						"Cannot insert class {1} into field {0} of a {2} over a ghost slice.",
						field, className, ownerClassName ?? "nullOwner"));
					return true;
				}
				current = current.ContainingDataTree.Slices[iNew] as Slice;
			}
			Logger.WriteEvent(String.Format("Inserting class {1} into field {0} of a {2}.",
				field, className, ownerClassName ?? "nullOwner"));
			current.HandleInsertCommand(field, className, ownerClassName,
				command.GetParameter("recomputeVirtual", null));

			Logger.WriteEvent("Done Inserting.");
			return true;	//we handled this.
		}

		/// <summary>
		/// This method is called when a user selects a Copy operation in on a slice.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnDataTreeCopy(object cmd)
		{
			Slice originalSlice = m_dataEntryForm.CurrentSlice;
			ICmObject obj = originalSlice.Object;
			object[] key = originalSlice.Key;
			Type type = originalSlice.GetType();

			if (OnDataTreeInsert(cmd))
			{
				string label;
				if (cmd is Command)
					label = (cmd as Command).Label;
				else
					label = "Copy";
				Slice newSlice = m_dataEntryForm.CurrentSlice;
				if (newSlice != null && !originalSlice.IsDisposed)
				{
					//Slice newCopy;
					//Slice newOriginal = m_dataEntryForm.FindMatchingSlices(obj, key, type, out newCopy);
					//Debug.Assert(newOriginal == originalSlice);
					//Debug.Assert(newCopy == newSlice);
					originalSlice.HandleCopyCommand(newSlice, label);
				}
				else
				{
					Slice newCopy;
					Slice newOriginal = m_dataEntryForm.FindMatchingSlices(obj, key, type, out newCopy);
					if (newOriginal != null && newCopy != null)
					{
						newOriginal.HandleCopyCommand(newCopy, label);
						newCopy.FocusSliceOrChild();
					}
				}
			}
			return true;	//we handled this.
		}

		private bool SliceConfiguredForField(XmlNode node, string field)
		{
			if (node != null)
				return XmlUtils.GetOptionalAttributeValue(node, "field") == field;
			else
				return false;
		}

		/// <summary>
		/// Get the class name and field for the given insertion command.
		/// </summary>
		/// <param name="command">insertion command</param>
		/// <param name="fieldName"></param>
		/// <param name="className"></param>
		static protected void ExtractInsertCommandInfo(Command command, out string fieldName, out string className)
		{
			fieldName = null;
			className = null;
			XmlNode node = ExtractInsertCommandParameters(command);
			if (node == null)
				return;
			fieldName = XmlUtils.GetOptionalAttributeValue(node, "field");
			className = XmlUtils.GetOptionalAttributeValue(node, "className");
		}

		private static XmlNode ExtractInsertCommandParameters(Command command)
		{
			return command.ConfigurationNode.SelectSingleNode("parameters");
		}


		/// <summary>
		/// Check to see if the insertion command makes sense for the current slice and its ContainingDataTree.
		/// </summary>
		/// <param name="command">insertion command</param>
		/// <param name="currentSlice">current selected slice on the ContainingDataTree</param>
		/// <param name="index">0-based position for insert, if known, or -1</param>
		/// <returns>true if the insertion command can result in an insertion in this context.</returns>
		protected virtual bool CanInsert(Command command, Slice currentSlice, out int index)
		{
			index = -1;
			if (currentSlice == null)
				return false;
			string fieldName;
			string cmdClassName;
			ExtractInsertCommandInfo(command, out fieldName, out cmdClassName);
			string rootObjClassName = string.Empty;
			var rootObj = currentSlice.ContainingDataTree.Root;
			if (rootObj != null && !rootObj.IsValidObject)
				return false;
			try
			{
				if (rootObj != null)
					rootObjClassName = rootObj.ClassName;
			}
			catch
			{
			}
			// For a SubPossibilities insertion, the class associated with this field
			// in the command node must match the class of the parentObject so that
			// we filter out commands to insert an item into the wrong CmPossibility class.
			// (If we need to do anything else exceptional for CmPossibilities try subclassing
			// DTMenuHandler to handle these things. That requires a menuHandler node in the parameters
			// for each of the tools based on a CmPossibility list.)
			if (fieldName == "SubPossibilities")
			{
				// we can insert only if we are are inserting into the same class.
				if (cmdClassName != rootObjClassName)
					return false;
				// we can insert only if the ContainingDataTree has a SubPossibilities slice
				if(!currentSlice.ContainingDataTree.HasSubPossibilitiesSlice)
					return false;
			}
			// "Insert Subsense" should be enabled only for Sense related slices, not for entry
			// related slices.  This isn't perfect, but behaves the same as "Insert Picture".
			if (command.Id == "CmdInsertSubsense" && fieldName == "Senses")
			{
				if (currentSlice.Object != null && currentSlice.Object.IsValidObject)
					return currentSlice.Object.ClassID == LexSenseTags.kClassId;
			}
			// First, see if the command can insert something that belongs to
			// the current slice containing tree root object.
			bool fCanInsert;
			fCanInsert = CanInsertFieldIntoObj(Cache, fieldName, rootObj, out index);
			// Otherwise, see if the command can insert something that belongs
			// to the current slice object.
			if (!fCanInsert && currentSlice.Object != rootObj)
			{
				fCanInsert = CanInsertFieldIntoObj(Cache, fieldName, currentSlice.Object, out index);
			}
			return fCanInsert;
		}

		/// <summary>
		/// Check if the field can be inserted into the given object.
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="fieldName">name of the field to be inserted</param>
		/// <param name="parentObj">The object where the item would be inserted, if possible.</param>
		/// <param name="index">index (0-based) where it will be inserted. -1 if atomic or returns false</param>
		/// <returns>true if we can insert into the given object</returns>
		protected bool CanInsertFieldIntoObj(FdoCache fdoCache, string fieldName, ICmObject parentObj, out int index)
		{
			index = -1; // atomic or not possible
			if (fdoCache == null || parentObj == null || (!parentObj.IsValidObject) || String.IsNullOrEmpty(fieldName))
				return false;
			var mdc = fdoCache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			// class not specified, depends on the object we're testing.
			int flid = GetFlidIfPossible(parentObj.ClassID, fieldName, mdc);
			if (flid == 0)
				return false; // Some kind of fake field, or wrong type of object, so bail out.
			var type = (CellarPropertyType) mdc.GetFieldType(flid);

			// we can only insert new objects into virtual reference properties
			if (fdoCache.IsReferenceProperty(flid) && !mdc.get_IsVirtual(flid))
				return false;

			if (type == CellarPropertyType.OwningSequence ||
				type == CellarPropertyType.OwningCollection ||
				type == CellarPropertyType.ReferenceCollection ||
				type == CellarPropertyType.ReferenceSequence)
			{
				index = fdoCache.DomainDataByFlid.get_VecSize(parentObj.Hvo, flid);
				return true;
			}

			// if its an atomic field, see if it's already been filled.
			if (type == CellarPropertyType.OwningAtomic || type == CellarPropertyType.ReferenceAtomic)
			{
				return fdoCache.DomainDataByFlid.get_ObjectProp(parentObj.Hvo, flid) == 0;
			}
			return false;
		}

		/// <summary>
		/// This seems a bit clumsy, but the metadata cache now throws an exception if the class
		/// id/field name pair isn't valid for GetFieldId2(). So we check first in cases where
		/// we want a zero if not found.
		/// </summary>
		private int GetFlidIfPossible(int clid, string fieldName, IFwMetaDataCacheManaged mdc)
		{
			if (!mdc.FieldExists(clid, fieldName, true))
				return 0;
			return mdc.GetFieldId2(clid, fieldName, true);
		}

		/// <summary>
		/// decide whether to display this tree insert Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeInsert(object commandObject,
			ref UIItemDisplayProperties display)
		{
			XCore.Command command = commandObject as XCore.Command;
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null && m_dataEntryForm.Slices.Count > 0)
				slice = m_dataEntryForm.FieldAt(0);

			int index = -1;
			if (command != null && slice != null && !slice.IsDisposed &&
				m_dataEntryForm.Visible && this.CanInsert(command, slice, out index))
			{
				display.Enabled = true;
				string toolTipInsert = display.Text.Replace("_", string.Empty);	// strip any menu keyboard accelerator marker;
				command.ToolTipInsert = toolTipInsert.ToLower();
				XmlNode node = ExtractInsertCommandParameters(command);
				if (index >= 0 && node != null && XmlUtils.GetOptionalBooleanAttributeValue(node, "displayNumber", false))
					display.Text += " " + (index + 1);
				return true;
			}
			display.Enabled = false;
			return false;
		}

		/// <summary>
		/// decide whether to display this tree copy Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeCopy(object commandObject,
			ref UIItemDisplayProperties display)
		{
			XCore.Command command = commandObject as XCore.Command;
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null && m_dataEntryForm.Slices.Count > 0)
				slice = m_dataEntryForm.FieldAt(0);

			int index = -1;
			if (command != null && slice != null && !slice.IsDisposed && this.CanInsert(command, slice, out index))
			{
				display.Enabled = true;
				string toolTipInsert = display.Text.Replace("_", string.Empty);	// strip any menu keyboard accelerator marker;
				command.ToolTipInsert = toolTipInsert.ToLower();
				XmlNode node = ExtractInsertCommandParameters(command);
				if (index >= 0 && node != null && XmlUtils.GetOptionalBooleanAttributeValue(node, "displayNumber", false))
					display.Text += " " + (index + 1);
				return true;
			}
			display.Enabled = false;
			return false;
		}
		/// <summary>
		/// This method is called when a user selects a Delete operation for a slice.
		/// The menu item is defined in DataTreeInclude.xml with message="DataTreeDelete"
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public virtual bool OnDataTreeDelete(object cmd)
		{
			Command command = (Command) cmd;
			DeleteObject(command);
			return true;	//we handled this.
		}

		protected virtual bool DeleteObject(Command command)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null && current.Object.IsValidObject)
				return current.HandleDeleteCommand(command);
			return false;
		}

		/// <summary>
		/// decide whether to enable this tree delete Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeDelete(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.IsGhostSlice)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			// NB (JH): this will prove too simplistic when we have an atomic attribute
			// which takes more than 1 base class. in that case, this will prevent the user from
			// deleting the existing object of type X in order to replace it with one of type Y.
			display.Enabled = current != null && current.GetCanDeleteNow();

			//			if(current.GetObjectHvoForMenusToOperateOn() == m_dataEntryForm.Root.Hvo && !current.WrapsAtomic)
			//			{
			//				display.Enabled = false;
			//				display.Text += StringTbl.GetString("(Programming error: would delete this record.)");
			//			}
			//			else
			if(!display.Enabled)
				display.Text += StringTbl.GetString("(cannot delete this)");

			if (display.Text.Contains("{0}"))
			{
				// Insert the class name of the thing we will delete
				var obj = current.GetObjectForMenusToOperateOn();
				if (obj != null)
					display.Text = string.Format(display.Text, m_mediator.StringTbl.GetString(obj.ClassName, "ClassNames"));
			}

			return true;//we handled this, no need to ask anyone else.
		}
		/// <summary>
		/// This method is called when a user selects a Delete Reference operation for a slice.
		/// The menu item is defined in DataTreeInclude.xml with message="DataTreeDeleteReference"
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public virtual bool OnDataTreeDeleteReference(object cmd)
		{
			Command command = (Command)cmd;
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null)
				current.HandleDeleteReferenceCommand(command);
			return true;	//we handled this.
		}

		/// <summary>
		/// decide whether to enable this tree delete reference Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeDeleteReference(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.IsGhostSlice)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			Command command = (Command)commandObject;
			display.Enabled = current != null && current.CanDeleteReferenceNow(command);
			if (!display.Enabled)
				display.Text += StringTbl.GetString("(cannot delete this)");
			return true;//we handled this, no need to ask anyone else.
		}

		public bool OnDataTreeMerge(object cmd)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null)
				current.HandleMergeCommand(true);
			return true;	//we handled this.
		}

		/// <summary>
		/// decide whether to enable this tree delete Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeMerge(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			display.Enabled = current != null && current.GetCanMergeNow();
			if(!display.Enabled)
				display.Text += StringTbl.GetString("(cannot merge this)");

			return true;//we handled this, no need to ask anyone else.
		}

		public bool OnDataTreeSplit(object cmd)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null)
				current.HandleSplitCommand();
			return true;	//we handled this.
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeSplit(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			display.Enabled = current != null && current.GetCanSplitNow();
			return true;	//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// This method is called when a user selects "Edit Reference Set Details" for a Lexical Relation slice.
		/// The menu item is defined in DataTreeInclude.xml with message="DataTreeEdit"
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnDataTreeEdit(object cmd)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null)
				current.HandleEditCommand();
			return true;	//we handled this.
		}

		/// <summary>
		/// This method is called when a user selects Add Reference or Replace Reference
		/// under a lexical relation slice.
		/// The menu item is defined in DataTreeInclude.xml with message="DataTreeAddReference"
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool OnDataTreeAddReference(object cmd)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null)
				current.HandleLaunchChooser();
			return true;	//we handled this.
		}

		/// <summary>
		/// decide whether to enable this Edit Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDataTreeEdit(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			display.Enabled = current != null && current.GetCanEditNow();

			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Launch a control dynamically from the control pointed to by the 'guicontrol' id in the command object.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnLaunchGuiControl(object commandObject)
		{
			Command command = (Command)commandObject;
			using (CmObjectUi fdoUi = CmObjectUi.MakeUi(m_dataEntryForm.CurrentSlice.Object))
			{
				fdoUi.Mediator = m_mediator;
				fdoUi.LaunchGuiControl(command);
			}
			return true;
		}

		/// <summary>
		/// Handle the message to move an object to the previous location in a sequence.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns>true to indicate the message was handled</returns>
		public bool OnMoveUpObjectInSequence(object cmd)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			Debug.Assert(!slice.IsDisposed, "The current slice is already disposed??");
			if (slice != null)
			{
				var cache = m_dataEntryForm.Cache;
				var obj = slice.Object.Owner;
				int flid = slice.Object.OwningFlid;
				int ihvo = cache.DomainDataByFlid.GetObjIndex(obj.Hvo, (int)flid, slice.Object.Hvo);
				if (ihvo > 0)
				{
					// The slice might be invalidated by the MoveOwningSequence, so we get its
					// values first.  See LT-6670.
					XmlNode caller = slice.CallerNode;
					XmlNode config = slice.ConfigurationNode;
					int clid = slice.Object.ClassID;
					Control parent = slice.Parent;
					// We found it in the sequence, and it isn't already the first.
					UndoableUnitOfWorkHelper.Do(xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem, cache.ActionHandlerAccessor,
					()=>cache.DomainDataByFlid.MoveOwnSeq(obj.Hvo, (int)flid, ihvo, ihvo,
						obj.Hvo, (int)flid, ihvo - 1));
				}
			}
			return true;	//we handled this.
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveUpObjectInSequence(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice == null || !slice.IsDisposed, "The current slice is already disposed??");
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			var cache = m_dataEntryForm.Cache;
			// FWR-2742 Handle a slice Object (like LexEntry) being unowned (and OwningFlid = 0)
			var type = CellarPropertyType.ReferenceAtomic;
			if (slice.Object.OwningFlid > 0)
				type = (CellarPropertyType) cache.DomainDataByFlid.MetaDataCache.GetFieldType(slice.Object.OwningFlid);
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			int chvo = cache.DomainDataByFlid.get_VecSize(slice.Object.Owner.Hvo, (int)slice.Object.OwningFlid);
			if (chvo < 2)
			{
				display.Enabled = false;
			}
			else
			{
				int hvo = cache.DomainDataByFlid.get_VecItem(slice.Object.Owner.Hvo, (int)slice.Object.OwningFlid,
					0);
				display.Enabled = slice.Object.Hvo != hvo;
				// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
				// slice displays the second LexEntryRef in the sequence, then we can't move it
				// up, since the first slot is reserved for the complex form.
				if (display.Enabled && slice.Object.OwningFlid == LexEntryTags.kflidEntryRefs)
				{
					if (cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
					{
						int hvo1 = cache.DomainDataByFlid.get_VecItem(slice.Object.Owner.Hvo, (int)slice.Object.OwningFlid, 1);
						display.Enabled = slice.Object.Hvo != hvo1;
					}
				}
			}
			display.Visible = true;
			return true; //we've handled this
		}

		/// <summary>
		/// Handle the message to move an object to the next location in a sequence.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns>true to indicate the message was handled</returns>
		public virtual bool OnMoveDownObjectInSequence(object cmd)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			Debug.Assert(!slice.IsDisposed, "The current slice is already disposed??");
			if (slice != null)
			{
				var cache = m_dataEntryForm.Cache;
				int hvoOwner = slice.Object.Owner.Hvo;
				int flid = slice.Object.OwningFlid;
				int chvo = cache.DomainDataByFlid.get_VecSize(hvoOwner, (int)flid);
				int ihvo = cache.DomainDataByFlid.GetObjIndex(hvoOwner, (int)flid, slice.Object.Hvo);
				if (ihvo >= 0 && ihvo + 1 < chvo)
				{
					// The slice might be invalidated by the MoveOwningSequence, so we get its
					// values first.  See LT-6670.
					XmlNode caller = slice.CallerNode;
					XmlNode config = slice.ConfigurationNode;
					int clid = slice.Object.ClassID;
					Control parent = slice.Parent;
					// We found it in the sequence, and it isn't already the last.
					// Quoting from VwOleDbDa.cpp, "Insert the selected records before the
					// DstStart object".  This means we need + 2 instead of + 1 for the
					// new location.
					UndoableUnitOfWorkHelper.Do(xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem, cache.ActionHandlerAccessor,
					()=>cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, (int)flid, ihvo, ihvo,
						hvoOwner, (int)flid, ihvo + 2));
				}
			}
			return true;	//we handled this.
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveDownObjectInSequence(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice == null || !slice.IsDisposed, "The current slice is already disposed??");
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			FdoCache cache = m_dataEntryForm.Cache;
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			// FWR-2742 Handle a slice Object (like LexEntry) being unowned (and OwningFlid = 0)
			var type = CellarPropertyType.ReferenceAtomic;
			if (slice.Object.OwningFlid > 0)
				type = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(slice.Object.OwningFlid);
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			int chvo = cache.DomainDataByFlid.get_VecSize(slice.Object.Owner.Hvo, (int)slice.Object.OwningFlid);
			if (chvo < 2)
			{
				display.Enabled = false;
			}
			else
			{
				int hvo = cache.DomainDataByFlid.get_VecItem(slice.Object.Owner.Hvo, (int)slice.Object.OwningFlid,
					chvo - 1);
				display.Enabled = slice.Object.Hvo != hvo;
			}
			display.Visible = true;
			return true; //we've handled this
		}

		/// <summary>
		/// Allow any number of variant type LexEntryRef objects per LexEntry.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayConvertEntryIntoVariant(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = DisplayConvertLexEntry(commandObject);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnConvertEntryIntoVariant(object argument)
		{
			return AddNewLexEntryRef(argument, LexEntryRefTags.kflidVariantEntryTypes);
		}

		/// <summary>
		/// Allow only one complex form type LexEntryRef object per LexEntry.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayConvertEntryIntoComplexForm(object commandObject, ref UIItemDisplayProperties display)
		{
			bool fDisplay = DisplayConvertLexEntry(commandObject);
			if (fDisplay)
			{
				if (!m_dataEntryForm.Root.IsValidObject)
					return true;
				foreach (var ler in (m_dataEntryForm.Root as ILexEntry).EntryRefsOS)
				{
					if (ler.ComplexEntryTypesRS.Count == 0)
						continue;
					fDisplay = false;
					break;
				}
			}
			display.Enabled = fDisplay;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnConvertEntryIntoComplexForm(object argument)
		{
			return AddNewLexEntryRef(argument, LexEntryRefTags.kflidComplexEntryTypes);
		}

		private bool DisplayConvertLexEntry(object commandObject)
		{
			// We may not have any data set up yet.  See LT-9712.
			if (Cache == null || m_dataEntryForm == null || m_dataEntryForm.Root == null)
				return false;
			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_dataEntryForm.Root.ClassName)
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;
			return m_dataEntryForm.Root is ILexEntry;
		}

		private bool AddNewLexEntryRef(object argument, int flidTypes)
		{
			Command command = (Command)argument;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_dataEntryForm.Root.ClassName)
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;

			var ent = m_dataEntryForm.Root as ILexEntry;
			if (ent != null)
			{
				UndoableUnitOfWorkHelper.Do(command, ent,
					() =>
						{
							var ler = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
							int insertPos;
							if (flidTypes == LexEntryRefTags.kflidVariantEntryTypes)
							{
								insertPos = ent.EntryRefsOS.Count;
								ent.EntryRefsOS.Add(ler);
								ler.RefType = LexEntryRefTags.krtVariant;
								ler.HideMinorEntry = 0;
							}
							else
							{
								insertPos = 0;
								ent.EntryRefsOS.Insert(insertPos, ler);
								ler.RefType = LexEntryRefTags.krtComplexForm;
								ler.HideMinorEntry = 0; // LT-10928
								ent.ChangeRootToStem();
							}
						});
				return true;
			}
			return false;
		}

		public bool OnDisplayAddComponentToPrimary(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.Flid == 0)
				return true; // already handled - nothing else should be responding to this message
			bool fEnable = false;
			bool fChecked = false;
			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className == current.Object.ClassName)
			{
				string tool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "tool");
				if (tool == null || tool == m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				{
					int hvo = GetSelectedComponentHvo();
					var ler = current.Object as ILexEntryRef;
					if (hvo != 0)
					{
						ICmObject target = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						if (ler != null && ler.RefType == LexEntryRefTags.krtComplexForm &&
							(target is ILexEntry || target is ILexSense))
						{
							fEnable = true;
							fChecked = ler.PrimaryLexemesRS.Contains(target); // LT-11292
						}
					}
				}
			}
			display.Visible = display.Enabled = fEnable;
			display.Checked = fChecked;
			return true;
		}

		public bool OnAddComponentToPrimary(object argument)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.Flid == 0)
				return true; // already handled - nothing else should be responding to this message
			int hvo = GetSelectedComponentHvo();
			if (hvo == 0)
				return true;

			var ler = current.Object as ILexEntryRef;
			var objForHvo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (ler.PrimaryLexemesRS.Contains(objForHvo))
			{   // Remove from visibility array
				using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(
					Cache.ActionHandlerAccessor,
					xWorksStrings.ksUndoShowSubentryForComponent,
					xWorksStrings.ksRedoShowSubentryForComponent))
				{
					ler.PrimaryLexemesRS.Remove(objForHvo);
					helper.RollBack = false;
				}
				return true;
			}
			// Otherwise, continue and add it
			int idx = 0;
			foreach (var obj in ler.ComponentLexemesRS)
			{ // looping preserves the order of the components
				if (obj == objForHvo)
				{
					using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(
						Cache.ActionHandlerAccessor,
						xWorksStrings.ksUndoShowSubentryForComponent,
						xWorksStrings.ksRedoShowSubentryForComponent))
					{
						ler.PrimaryLexemesRS.Insert(idx, objForHvo);
						helper.RollBack = false;
					}
					break;
				}

				if (ler.PrimaryLexemesRS.Contains(obj))
				{
					++idx;
				}
			}
			return true;
		}

		/// <summary>
		/// When a data tree slice is right-clicked, this determines if the VisibleComplexFormEntries
		/// part should be put on its popup menu, like mnuReferenceChoices in Main.xml.
		/// </summary>
		/// <param name="commandObject">The command to build the popup menu.</param>
		/// <param name="display">The display properties for this slice.</param>
		/// <returns>true if the VisibleComplexFormEntries part should be put on the popup menu.</returns>
		public bool OnDisplayVisibleComplexForm(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = display.Enabled = false; // item shows on some wrong slice menus if not false
			Slice complexFormsSlice = m_dataEntryForm.CurrentSlice;
			if (complexFormsSlice == null || complexFormsSlice.Object == null || complexFormsSlice.Flid == 0)
				return true; // already handled - nothing else should be responding to this message
			bool fEnable = false;
			bool fChecked = false;
			// Is this the right slice to handle this command?
			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (("LexEntry" != complexFormsSlice.Object.ClassName &&
				 "LexSense" != complexFormsSlice.Object.ClassName ) || className != "LexEntryOrLexSense")
				return false; // not the right message target
			// The complex form slice is in both entriy and sense layouts.
			if (complexFormsSlice.Flid != Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "ComplexFormEntries", false) &&
				complexFormsSlice.Flid != Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "ComplexFormEntries", false))
				return false; // Not the right slice for this command
			// is a complex form is selected?
			var lexOrSenseComponent = complexFormsSlice.Object;
			int hvo = GetSelectedComplexFormHvo(complexFormsSlice);
			if (hvo == 0)
				return false; // no selection
			// set the checkbox if this component has it set as visible
			var cplxForm = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvo);
			Debug.Assert(cplxForm != null, "A component of a complex form has no reference to its complex form");
			ILexEntryRef cfRef;
			display.Visible = display.Enabled = true;
			display.Checked = ComponentShowsComplexForm(lexOrSenseComponent, cplxForm, out cfRef);
			return true;
		}

		public bool OnVisibleComplexForm(object argument)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.Flid == 0)
				return true; // already handled - nothing else should be responding to this message
			int hvo = GetSelectedComplexFormHvo(current);
			if (hvo == 0)
				return true;

			ICmObject le = current.Object; // can be ILexEntry or ILexSense
			var cplxForm = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvo);
			ILexEntryRef cfRef;
			if (ComponentShowsComplexForm(le, cplxForm, out cfRef))
			{
				// Remove from visibility array
				using (var helper = new UndoableUnitOfWorkHelper(
					Cache.ActionHandlerAccessor,
					xWorksStrings.ksUndoVisibleComplexForm,
					xWorksStrings.ksRedoVisibleComplexForm))
				{
					cfRef.ShowComplexFormsInRS.Remove(le);
					helper.RollBack = false;
				}
				return true;
			}
			// Otherwise, continue and add it
			int idx = 0;
			foreach (var obj in cfRef.ComponentLexemesRS)
			{
				// looping preserves the order of the components
				if (obj == le)
				{
					using (var helper = new UndoableUnitOfWorkHelper(
						Cache.ActionHandlerAccessor,
						xWorksStrings.ksUndoVisibleComplexForm,
						xWorksStrings.ksRedoVisibleComplexForm))
					{
						cfRef.ShowComplexFormsInRS.Insert(idx, le);
						helper.RollBack = false;
					}
					break;
				}
				if (cfRef.ShowComplexFormsInRS.Contains(obj))
				{
					++idx;
				}
			}
			return true;
		}

		/// <summary>
		/// Does this component show this complex form in the dictionary?
		/// The component can be a ILexEntry or a ILexSense.
		/// The reference to the complex form from the component is outted.
		/// </summary>
		/// <param name="component">A component of a complex form, not necessarily cplxForm.</param>
		/// <param name="cplxForm">The complex form in question.</param>
		/// <param name="cfRef">The complex form reference from the component.</param>
		/// <returns>true if cplxForm "contains" component (it has a reference to it).</returns>
		private bool ComponentShowsComplexForm(ICmObject component, ILexEntry cplxForm, out ILexEntryRef cfRef)
		{
			cfRef = (from item in cplxForm.EntryRefsOS where item.RefType == LexEntryRefTags.krtComplexForm select item).FirstOrDefault();
			Debug.Assert(cfRef != null,"A component of a complex form has no reference to its complex form");
			return cfRef.ShowComplexFormsInRS.Contains(component);
		}

		/// <summary>
		/// Intended to get a selected complex form, but it can probably get other
		/// selections from entries or senses.
		/// </summary>
		/// <param name="complexFormsSlice">The non-null Complex Forms slice (from an entry or a sense)</param>
		/// <returns>The HVO of the selected complex form or 0 if there is none.</returns>
		private int GetSelectedComplexFormHvo(Slice complexFormsSlice)
		{
			if (!(complexFormsSlice.Object is ILexEntry) &&
				!(complexFormsSlice.Object is ILexSense)) return 0;
			return GetSelectionHvoFromControls(complexFormsSlice);
		}

		/// <summary>
		/// Gets a selected component's HVO; a component of a complex form.
		/// </summary>
		/// <returns>The HVO of the selected component or 0 if there is none.</returns>
		private int GetSelectedComponentHvo()
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current.Flid != LexEntryRefTags.kflidComponentLexemes ||
				!(current.Object is ILexEntryRef)) return 0;
			return GetSelectionHvoFromControls(current);
		}

		/// <summary>
		/// Gets the selection HVO from a slice control.
		/// </summary>
		/// <param name="slice">The non-null slice to get the selection from.</param>
		/// <returns>The HVO of the selection or 0 if there is none.</returns>
		private int GetSelectionHvoFromControls(Slice slice)
		{
			if (slice.Control == null || slice.Control.Controls.Count == 0) return 0;
			foreach (Control x in slice.Control.Controls)
			{
				if (x is RootSiteControl)
				{
					var site = x as SimpleRootSite;
					if (site != null && site.RootBox != null)
					{
						var tsi = new TextSelInfo(site.RootBox);
						return tsi.Hvo(true);
					}
				}
			}
			return 0; // no selection found
		}

		protected FdoCache Cache
		{
			get
			{
				return m_dataEntryForm.Cache;
			}
		}


		/// <summary>
		/// Invoked by a DataTree (which is in turn invoked by the slice)
		/// when the context menu for a slice is needed.
		/// </summary>
		public ContextMenu ShowSliceContextMenu(object sender, SliceMenuRequestArgs e)
		{
			Slice slice = e.Slice;
			return MakeSliceContextMenu(slice, e.HotLinksOnly);
			// We want something like the following (See LT-2310), but the following code does not work
			// because the menu returned by MakeSliceContextMenu has no items; they are created by an event handler
			// when the menu pops up. There's no way to get at them until then, which is too late. We will have to
			// refactor so we can merge the configuration nodes for the multiple menus.
			//			int index = slice.IndexInContainer;
			//			int indent = slice.Indent;
			//			while (indent > 0)
			//			{
			//				indent--;
			//				index = slice.Container.PrevFieldAtIndent(indent, index);
			//				Slice parentSlice = (Slice)Parent.Controls[index];
			//				ContextMenu parentMenu = MakeSliceContextMenu(parentSlice);
			//				if (parentMenu == null)
			//					continue;
			//				if (menu == null)
			//				{
			//					menu = parentMenu;
			//					continue;
			//				}
			//				menu.MenuItems.Add("---");
			//				Debug.WriteLine("Added --- to menu");
			//				foreach (MenuItem item in parentMenu.MenuItems)
			//					menu.MenuItems.Add(item.CloneMenu());
			//			}
			//we need to stash away this information so that when we receive a command
			//from the menu system, we can associate it with the slice that sent it
			//m_sourceOfMenuCommandSlice = e.Slice;
			//			return menu;
			//nono, this happens immediately m_sourceOfMenuCommandSlice= null;

		}

		protected ContextMenu MakeSliceContextMenu(Slice slice, bool fHotLinkOnly)//, bool retrieveDoNotShow)
		{
			XmlNode configuration =slice.ConfigurationNode;
			XmlNode caller = slice.CallerNode;
			string menuId = null;
			if (caller != null)
				menuId = ShowContextMenu2Id(caller, fHotLinkOnly);
			if (menuId == null || menuId.Length == 0)
				menuId = ShowContextMenu2Id(configuration, fHotLinkOnly);

			XWindow window = (XWindow)m_mediator.PropertyTable.GetValue("window");

			//an empty menu attribute means no menu
			if (menuId != null && menuId.Length== 0)
				return null;

			/*			//a missing menu attribute means "figure out a default"
						if (menuId == null)
						{
							//todo: this is probably too simplistic
							//we are trying to select out just atomic objects
							//of this will currently also select "place keeping" nodes
							if(slice.IsObjectNode)
								//					configuration.HasChildNodes /*<-- that's dumb
								//					&& configuration.SelectSingleNode("seq")== null
								//					&& !(e.Slice.Object.Hvo == slice.Container.RootObjectHvo))
							{
								menuId="mnuDataTree-Object";
							}
							else //we could not figure out a default menu for this item, so fall back on the auto menu
							{	//todo: this must not be used in the final product!
								// return m_dataEntryForm.GetAutoMenu(sender, e);
								return null;
							}
						}
						if (menuId == "")
							return null;	//explicitly stated that there should not be a menu

			*/
			//ChoiceGroup group;
			if(fHotLinkOnly)
			{
				return	window.GetWindowsFormsContextMenu(menuId);
			}
			else
			{
				//string[] menus = new string[2];
				List<string> menus = new List<string>();
				menus.Add(menuId);
				if (slice is MultiStringSlice)
					menus.Add("mnuDataTree-MultiStringSlice");
				else
					menus.Add("mnuDataTree-Object");
				window.ShowContextMenu(menus.ToArray(),
					new Point(Cursor.Position.X, Cursor.Position.Y),
					null, // Don't care about a temporary colleague
					null); // or MessageSequencer
				return null;
			}

			//			group.ConfigurationNode.AppendChild(group.ConfigurationNode.OwnerDocument.ImportNode(addon,true));
			// This causes the menu to be actually populated with the items. It's a rather
			// ugly way to do it...happens all over again when the menu pops up...but we
			// need to know the actual items for various purposes, such as populating a
			// summary slice's command list. Refactoring is complicated because part of
			// the code is in a DotNetBar UiAdapter class that I can't easily modify.
			//			group.OnDisplay(null, new EventArgs());
			//			return menu;
		}

		private string ShowContextMenu2Id(XmlNode caller, bool fHotLinkOnly)
		{
			if (fHotLinkOnly)
			{
				string result = XmlUtils.GetOptionalAttributeValue(caller, "hotlinks");
				if (result != null && result.Length != 0)
					return result;
			}
			return XmlUtils.GetOptionalAttributeValue(caller, "menu");
		}
	}
}
