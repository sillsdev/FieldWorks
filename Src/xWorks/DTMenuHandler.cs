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
using System.Xml;
using System.Collections.Generic;

using System.Windows.Forms;
using System.Drawing;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Resources;

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
		protected DataTree m_dataEntryForm = null;

		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected XCore.Mediator m_mediator = null;

		/// <summary>
		/// COnfiguration information.
		/// </summary>
		protected XmlNode m_configuration = null;


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
			ICmObject obj = m_dataEntryForm.CurrentSlice.Object;
			int chvo = obj.Cache.MainCacheAccessor.get_VecSize(obj.Hvo, flid);
			using (PicturePropertiesDialog dlg = new PicturePropertiesDialog(obj.Cache, null,
					   SIL.FieldWorks.Common.Framework.FwApp.App))
			{
				if (dlg.Initialize())
				{
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						string strLocalPictures = EditingHelper.DefaultPictureFolder;
						int hvoPic = obj.Cache.MainCacheAccessor.MakeNewObject(CmPicture.kClassId, obj.Hvo, flid, chvo);
						ICmPicture picture = CmPicture.CreateFromDBObject(obj.Cache, hvoPic);
						picture.InitializeNewPicture(dlg.CurrentFile, dlg.Caption, strLocalPictures, obj.Cache.DefaultAnalWs);
						// Let everything know about the new picture.
						obj.Cache.PropChanged(null, PropChangeType.kpctNotifyAll, obj.Hvo,
							flid, chvo, 1, 0);
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
			if (current == null)	// LT-3347: there are no slices in this empty data set
			{
				flid = 0;
				return false;
			}
			ICmObject obj = current.Object;
			IFwMetaDataCache mdc = obj.Cache.MetaDataCacheAccessor;
			uint clid = mdc.GetClassId(className);
			if (clid == 0)
				throw new ConfigurationException("Unknown class for insert command: " + className);
			uint uflid = mdc.GetFieldId2(clid, field, true);
			if (uflid == 0)
				throw new ConfigurationException("Unknown field: " + className + "." + field);
			flid = (int) uflid;
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
			string filter = ResourceHelper.BuildFileFilter(new FileFilterType[] {
				FileFilterType.AllAudio, FileFilterType.AllVideo, FileFilterType.AllFiles });
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
				LexEntry le = m_dataEntryForm.Root as LexEntry;
				if (le == null)
					return false;
				if (le.PronunciationsOS.Count == 0)
				{
					int hvo = le.Cache.MainCacheAccessor.MakeNewObject(LexPronunciation.kClassId,
						le.Hvo, (int)LexEntry.LexEntryTags.kflidPronunciations, 0);
					le.PronunciationsOS.Append(hvo);
				}
				obj = le.PronunciationsOS[0];
				flid = (int)LexPronunciation.LexPronunciationTags.kflidMediaFiles;
			}
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.InitialDirectory = Cache.LangProject.ExternalLinkRootDir;
				dlg.Filter = filter;
				dlg.FilterIndex = 1;
				if (m_mediator != null && m_mediator.HasStringTable)
					dlg.Title = m_mediator.StringTbl.GetString(keyCaption);
				if (dlg.Title == null || dlg.Title.Length == 0 || dlg.Title == "*" + keyCaption + "*")
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
						string file = MoveOrCopyFilesDlg.MoveCopyOrLeaveFile(dlg.FileName,
							Cache.LangProject.ExternalLinkRootDir, FwApp.App as IHelpTopicProvider);
						if (String.IsNullOrEmpty(file))
							return true;
						string sFolderName = null;
						if (m_mediator != null && m_mediator.HasStringTable)
							sFolderName = m_mediator.StringTbl.GetString("kstidMediaFolder");
						if (sFolderName == null || sFolderName.Length == 0 || sFolderName == "*kstidMediaFolder*")
							sFolderName = StringUtils.LocalMedia;
						int chvo = obj.Cache.MainCacheAccessor.get_VecSize(obj.Hvo, flid);
						int hvo = obj.Cache.MainCacheAccessor.MakeNewObject(FDO.Cellar.CmMedia.kClassId,
							obj.Hvo, flid, chvo);
						ICmMedia media = CmMedia.CreateFromDBObject(obj.Cache, hvo);
						media.InitializeNewMedia(file, null, sFolderName, obj.Cache.DefaultAnalWs);
						// Let everything know about the new picture.
						obj.Cache.PropChanged(null, PropChangeType.kpctNotifyAll, obj.Hvo,
							flid, chvo, 1, 0);
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
			ICmObject obj = m_dataEntryForm.CurrentSlice.Object;
			ICmMedia media = obj as ICmMedia;
			if (media != null)
				media.DeleteUnderlyingObject();
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
			ShowHelp.ShowHelpTopic(FwApp.App, generateSliceHelpTopicID());

			return true;
		}

		public bool OnDisplayDataTreeHelp(object cmd, ref UIItemDisplayProperties display)
		{
			// Only display help if there's a topic linked to the generated ID in the resource file
			string helpTopicID = generateSliceHelpTopicID();
			display.Visible = display.Enabled = (FwApp.App.GetHelpString(helpTopicID, 0) == null ? false : true);

			return true;
		}

		private bool SetValidHelpTopicId(string testId, out string helpTopicID)
		{
			helpTopicID = null;
			if (FwApp.App.GetHelpString(testId, 0) != null)
				helpTopicID = testId;
			return helpTopicID != null;
		}

		private string generateSliceHelpTopicID()
		{
			Slice current = m_dataEntryForm.CurrentSlice;

			string helpTopicID = null;
			if (current != null)
			{
				if (current.HelpTopicID != null)
					helpTopicID = current.HelpTopicID;
				else
				{
					string className = Cache.MetaDataCacheAccessor.GetClassName(
						(uint)current.Object.ClassID);
					string fieldName = XmlUtils.GetOptionalAttributeValue(current.ConfigurationNode, "field");
					if (String.IsNullOrEmpty(fieldName) && !String.IsNullOrEmpty(current.Label))
					{
						// try to use the slice label, without spaces.
						fieldName = current.Label.Replace(" ", "");
					}
					string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);

					// Most times, the tool name isn't needed to distinguish between fields, but if it is, we'll try
					// the specific case first, then the general
					string tempHelpTopicID = "khtpField" + "-" + toolName + "-" + className + "-" + fieldName;
					if (!SetValidHelpTopicId(tempHelpTopicID, out helpTopicID))
					{
						tempHelpTopicID = "khtpField" + "-" + className + "-" + fieldName;
						if (!SetValidHelpTopicId(tempHelpTopicID, out helpTopicID))
						{
							if (!String.IsNullOrEmpty(fieldName))
							{
								// next try some keys with a null fieldname, since we created some of those keys
								// before we added code to substitute the label.
								tempHelpTopicID = "khtpField" + "-" + toolName + "-" + className + "-";
								if (!SetValidHelpTopicId(tempHelpTopicID, out helpTopicID))
								{
									tempHelpTopicID = "khtpField" + "-" + className + "-";
									SetValidHelpTopicId(tempHelpTopicID, out helpTopicID);
								}
							}
						}
					}
					if (helpTopicID == null)
						helpTopicID = "khtpField" + "-" + className + "-" + fieldName;
				}
			}

			return helpTopicID;
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
			if (current == null && m_dataEntryForm.Controls.Count > 0)
				current = m_dataEntryForm.FieldAt(0);
			string sliceName = command.GetParameter("slice", "");
			if (String.IsNullOrEmpty(ownerClassName) && current != null && current.Object != null)
			{
				int hvoOwner = current.Object.OwnerHVO;
				if (Cache.IsValidObject(hvoOwner))
				{
					int clid = Cache.GetClassOfObject(hvoOwner);
					if (clid > 0)
						ownerClassName = Cache.GetClassName((uint)clid);
				}
			}
			if (sliceName != null && sliceName == "owner" && current != null)
			{
				// Find a slice corresponding to the current slice's owner's object.
				ICmObject cmo = current.Object;
				foreach (Slice slice in current.Parent.Controls)
				{
					if (slice.Object.Hvo == cmo.OwnerHVO)
					{
						current = slice;
						break;
					}
				}
			}
			if (current == null && m_dataEntryForm.Controls.Count > 0)
				current = m_dataEntryForm.Controls[0] as Slice;
			if (current == null || !Cache.VerifyValidObject(current.Object))
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
					if ((current.Parent.Controls[i] as Slice).Object != null &&
						(current.Parent.Controls[i] as Slice).Object.Hvo == hvoObject)
					{
						iNew = i;
						break;
					}
				}
				if (iNew == -1)
				{
					int cslice = current.Parent.Controls.Count;
					for (int i = current.IndexInContainer + 1; i < cslice; ++i)
					{
						if ((current.Parent.Controls[i] as Slice).Object != null &&
							(current.Parent.Controls[i] as Slice).Object.Hvo == hvoObject)
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
						field, className, ownerClassName));
					return true;
				}
				current = current.Parent.Controls[iNew] as Slice;
			}
			Logger.WriteEvent(String.Format("Inserting class {1} into field {0} of a {2}.", field, className, ownerClassName));
			current.HandleInsertCommand(field, className, ownerClassName, command.GetParameter("recomputeVirtual", null));

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
			if (OnDataTreeInsert(cmd))
			{
				Slice newSlice = m_dataEntryForm.CurrentSlice;
				originalSlice.HandleCopyCommand(newSlice);
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
			ICmObject rootObj = currentSlice.ContainingDataTree.Root;
			try
			{
				if (rootObj != null)
					rootObjClassName = Cache.MetaDataCacheAccessor.GetClassName((uint)rootObj.ClassID);
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
				if (currentSlice.Object != null)
					return currentSlice.Object.ClassID == LexSense.kclsidLexSense;
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
		static protected bool CanInsertFieldIntoObj(FdoCache fdoCache, string fieldName, ICmObject parentObj, out int index)
		{
			index = -1; // atomic or not possible
			if (fdoCache == null || parentObj == null || fieldName == null || fieldName == string.Empty)
				return false;
			IFwMetaDataCache mdc = fdoCache.MetaDataCacheAccessor;

			// class not specified, depends on the object we're testing.
			int flid = (int)mdc.GetFieldId2((uint)parentObj.ClassID, fieldName, true);
			if (flid == 0)
				return false; // Some kind of fake field, or wrong type of object, so bail out.
			FieldType type = fdoCache.GetFieldType(flid);
			if (type == FieldType.kcptOwningSequence ||
				type == FieldType.kcptOwningCollection ||
				type == FieldType.kfcptReferenceCollection ||
				type == FieldType.kcptReferenceSequence)
			{
				index = fdoCache.MainCacheAccessor.get_VecSize(parentObj.Hvo, flid);
				return true;
			}

			// if its an atomic field, see if it's already been filled.
			if (type == FieldType.kcptOwningAtom ||
				type == FieldType.kfcptReferenceAtom)
			{
				return fdoCache.GetObjProperty(parentObj.Hvo, flid) == 0;
			}
			return false;
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
			if (slice == null && m_dataEntryForm.Controls.Count > 0)
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
			if (slice == null && m_dataEntryForm.Controls.Count > 0)
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
			Slice current = m_dataEntryForm.CurrentSlice;
			Debug.Assert(current != null, "No slice was current");
			if (current != null && Cache.VerifyValidObject(current.Object))
				current.HandleDeleteCommand(command);
			return true;	//we handled this.
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
			Command command = (Command) cmd;
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
			Command command = (Command) cmd;
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
			Command command = (Command) cmd;
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
			Command command = (Command)cmd;
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
		///// Check whether the XML (caller or config) has a "notifyVirtual" attribute,
		///// and if so, try to generate appropriate PropChanged notifications for all similar
		///// objects which may have the same virtual property.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="caller"></param>
		/// <param name="config"></param>
		/// <param name="clid"></param>
		/// <param name="parent"></param>
		public void NotifyVirtualChanged(FdoCache cache, XmlNode caller, XmlNode config,
			int clid, Control parent)
		{
			XmlNode xa = null;
			if (caller != null)
				xa = caller.Attributes["notifyVirtual"];
			if (xa == null && config != null)
				xa = config.Attributes["notifyVirtual"];
			if (xa != null)
				NotifyVirtualChanged(cache, xa.Value, clid, parent);
		}

		/// <summary>
		/// If the virtual property propName is not empty, try to generate appropriate
		/// PropChanged notifications for all similar objects which may have the same virtual
		/// property.  This version is useful when the original slice is Disposed before this
		/// method can be called.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="propName"></param>
		/// <param name="clid"></param>
		/// <param name="parent"></param>
		public void NotifyVirtualChanged(FdoCache cache, string propName, int clid, Control parent)
		{
			if (propName == null || propName == "")
				return;
			string className = cache.MetaDataCacheAccessor.GetClassName((uint)clid);
			IVwVirtualHandler vh = cache.GetVirtualProperty(className, propName);
			if (vh != null)
			{
				int tag = vh.Tag;
				Set<int> hvosSeen = new Set<int>();
				foreach (Slice slice in parent.Controls)
				{
					int hvo = slice.Object.Hvo;
					if (hvosSeen.Contains(hvo))
						continue;
					hvosSeen.Add(hvo);
					if (slice.Object.ClassID == clid)
					{
						cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvo, tag,
							0, 0, 0);
					}
				}
			}
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
			Command command = (Command) cmd;
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			Debug.Assert(!slice.IsDisposed, "The current slice is already disposed??");
			if (slice != null)
			{
				FdoCache cache = m_dataEntryForm.Cache;
				int hvoOwner = slice.Object.OwnerHVO;
				int flid = slice.Object.OwningFlid;
				int ihvo = cache.GetObjIndex(hvoOwner, flid, slice.Object.Hvo);
				if (ihvo > 0)
				{
					// The slice might be invalidated by the MoveOwningSequence, so we get its
					// values first.  See LT-6670.
					XmlNode caller = slice.CallerNode;
					XmlNode config = slice.ConfigurationNode;
					int clid = slice.Object.ClassID;
					Control parent = slice.Parent;
					// We found it in the sequence, and it isn't already the first.
					cache.MoveOwningSequence(hvoOwner, flid, ihvo, ihvo,
						hvoOwner, flid, ihvo - 1);
					// We may need to notify everyone that a virtual property changed.
					NotifyVirtualChanged(cache, caller, config, clid, parent);
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
			FdoCache cache = m_dataEntryForm.Cache;
			FieldType type = cache.GetFieldType(slice.Object.OwningFlid);
			if (type != FieldType.kcptOwningSequence &&
				type != FieldType.kcptReferenceSequence)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			int chvo = cache.GetVectorSize(slice.Object.OwnerHVO, slice.Object.OwningFlid);
			if (chvo < 2)
			{
				display.Enabled = false;
			}
			else
			{
				int hvo = cache.GetVectorItem(slice.Object.OwnerHVO, slice.Object.OwningFlid,
					0);
				display.Enabled = slice.Object.Hvo != hvo;
				// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
				// slice displays the second LexEntryRef in the sequence, then we can't move it
				// up, since the first slot is reserved for the complex form.
				if (display.Enabled && slice.Object.OwningFlid == (int)LexEntry.LexEntryTags.kflidEntryRefs)
				{
					if (cache.GetVectorSize(hvo, (int)LexEntryRef.LexEntryRefTags.kflidComplexEntryTypes) > 0)
					{
						int hvo1 = cache.GetVectorItem(slice.Object.OwnerHVO, slice.Object.OwningFlid, 1);
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
			Command command = (Command) cmd;
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			Debug.Assert(!slice.IsDisposed, "The current slice is already disposed??");
			if (slice != null)
			{
				FdoCache cache = m_dataEntryForm.Cache;
				int hvoOwner = slice.Object.OwnerHVO;
				int flid = slice.Object.OwningFlid;
				int chvo = cache.GetVectorSize(hvoOwner, flid);
				int ihvo = cache.GetObjIndex(hvoOwner, flid, slice.Object.Hvo);
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
					cache.MoveOwningSequence(hvoOwner, flid, ihvo, ihvo,
						hvoOwner, flid, ihvo + 2);
					// We may need to notify everyone that a virtual property changed.
					NotifyVirtualChanged(cache, caller, config, clid, parent);
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
			IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
			FieldType type = cache.GetFieldType(slice.Object.OwningFlid);
			if (type != FieldType.kcptOwningSequence &&
				type != FieldType.kcptReferenceSequence)
			{
				display.Enabled = false;
				display.Visible = false;
				return true;
			}
			int chvo = cache.GetVectorSize(slice.Object.OwnerHVO, slice.Object.OwningFlid);
			if (chvo < 2)
			{
				display.Enabled = false;
			}
			else
			{
				int hvo = cache.GetVectorItem(slice.Object.OwnerHVO, slice.Object.OwningFlid,
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
			return AddNewLexEntryRef(argument, (int)LexEntryRef.LexEntryRefTags.kflidVariantEntryTypes);
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
				foreach (LexEntryRef ler in (m_dataEntryForm.Root as ILexEntry).EntryRefsOS)
				{
					if (ler.ComplexEntryTypesRS.Count > 0)
					{
						fDisplay = false;
						break;
					}
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
			return AddNewLexEntryRef(argument, (int)LexEntryRef.LexEntryRefTags.kflidComplexEntryTypes);
		}

		private bool DisplayConvertLexEntry(object commandObject)
		{
			// We may not have any data set up yet.  See LT-9712.
			if (Cache == null || m_dataEntryForm == null || m_dataEntryForm.Root == null)
				return false;
			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != Cache.GetClassName((uint)m_dataEntryForm.Root.ClassID))
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;
			else
				return m_dataEntryForm.Root is ILexEntry;
		}

		private bool AddNewLexEntryRef(object argument, int flidTypes)
		{
			Command command = (Command)argument;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != Cache.GetClassName((uint)m_dataEntryForm.Root.ClassID))
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;

			ILexEntry ent = m_dataEntryForm.Root as ILexEntry;
			if (ent != null)
			{
				ILexEntryRef ler = new LexEntryRef();
				int insertPos;
				if (flidTypes == (int)LexEntryRef.LexEntryRefTags.kflidVariantEntryTypes)
				{
					insertPos = ent.EntryRefsOS.Count;
					ent.EntryRefsOS.Append(ler);
					ler.RefType = LexEntryRef.krtVariant;
					ler.HideMinorEntry = 0;
				}
				else
				{
					insertPos = 0;
					ent.EntryRefsOS.InsertAt(ler, insertPos);
					ler.RefType = LexEntryRef.krtComplexForm;
					ler.HideMinorEntry = 1;
					ent.ChangeRootToStem();
				}
				Cache.PropChanged(m_dataEntryForm.Root.Hvo, (int)LexEntry.LexEntryTags.kflidEntryRefs, insertPos, 1, 0);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool OnDisplayAddComponentToPrimary(object commandObject, ref UIItemDisplayProperties display)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.Flid == 0)
				return false;
			bool fEnable = false;
			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className == Cache.GetClassName((uint)current.Object.ClassID))
			{
				string tool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "tool");
				if (tool == null || tool == m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				{
					int hvo = GetSelectedComponentHvo();
					ILexEntryRef ler = current.Object as ILexEntryRef;
					if (ler != null && ler.RefType == LexEntryRef.krtComplexForm &&
						hvo != 0 && !ler.PrimaryLexemesRS.Contains(hvo))
					{
						fEnable = true;
					}
				}
			}
			display.Visible = display.Enabled = fEnable;
			return true;
		}

		public bool OnAddComponentToPrimary(object argument)
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current == null || current.Object == null || current.Flid == 0)
				return false;
			int hvo = GetSelectedComponentHvo();
			if (hvo == 0)
				return true;
			ILexEntryRef ler = current.Object as ILexEntryRef;
			if (ler.PrimaryLexemesRS.Contains(hvo))
				return true;
			int idx = 0;
			foreach (ICmObject obj in ler.ComponentLexemesRS)
			{
				if (obj.Hvo == hvo)
				{
					ler.PrimaryLexemesRS.InsertAt(hvo, idx);
					break;
				}
				else if (ler.PrimaryLexemesRS.Contains(obj.Hvo))
				{
					++idx;
				}
			}
			return true;
		}

		private int GetSelectedComponentHvo()
		{
			Slice current = m_dataEntryForm.CurrentSlice;
			if (current.Flid == (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes &&
				current.Object is ILexEntryRef &&
				current.Control != null &&
				current.Control.Controls != null &&
				current.Control.Controls.Count > 0)
			{
				foreach (Control x in current.Control.Controls)
				{
					if (x is RootSiteControl)
					{
						SimpleRootSite site = x as SimpleRootSite;
						if (site != null && site.RootBox != null)
						{
							TextSelInfo tsi = new TextSelInfo(site.RootBox);
							return tsi.Hvo(true);
						}
					}
				}
			}
			return 0;
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
		public ContextMenu ShowSliceContextMenu(object sender, SIL.FieldWorks.Common.Framework.DetailControls.SliceMenuRequestArgs e)
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
