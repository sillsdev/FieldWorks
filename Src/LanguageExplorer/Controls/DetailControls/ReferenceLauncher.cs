// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.Cellar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FwUtils.MessageBoxEx;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary></summary>
	internal abstract class ReferenceLauncher : ButtonLauncher
	{
		#region event handler declarations

		public event EventHandler ChoicesMade;

		#endregion event handler declarations

		#region Properties

		/// <summary>
		/// True if property allows duplicates, otherwise false.
		/// </summary>
		public virtual bool AllowsDuplicates
		{
			get
			{
				CheckDisposed();
				return (CellarPropertyType)m_cache.DomainDataByFlid.MetaDataCache.GetFieldType(m_flid) == CellarPropertyType.ReferenceSequence;
			}
		}

		/// <summary>
		/// Flag whether we can modify the slice contents.
		/// </summary>
		public bool Editable { get; set; } = true;

		/// <summary>
		/// Allow the launcher button to become visible only if we're editable.
		/// </summary>
		public override bool SliceIsCurrent
		{
			set
			{
				if (Editable)
				{
					base.SliceIsCurrent = value;
				}
			}
		}
		#endregion // Properties

		#region Construction, Initialization, and Disposing
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		internal ReferenceLauncher()
		{
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		#endregion // Construction, Initialization, and Disposing

		/// <summary>
		/// Handle launching of the standard chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method, if the SimpleListChooser is not suitable.
		/// </remarks>
		protected override void HandleChooser()
		{
			var displayWs = "analysis vernacular";
			string postDialogMessageTrigger = null;

			var node = m_configurationNode?.Element("deParams");
			if (node != null)
			{
				displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
				postDialogMessageTrigger = XmlUtils.GetOptionalAttributeValue(node, "postChangeMessageTrigger", null);
			}
			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid), m_displayNameProperty, displayWs);

			// I (JH) started down this road to sorting the object labels... it proved bumpy
			// and I bailed out and just turned on the "sorted" property of the chooser,
			// which gives us a dumb English sort.
			// but when it is time to get back to this... what I was doing what is misguided
			// because this sorter wants LCM objects, but object labels don't have those on the
			// surface.  instead, they can readily give a string, through ToString().which is
			// what made me realize that until we have a way to sort something based on ICU, I
			// might as well let .net do the sorting.

			// I'm thinking there's a good chance we will eventually use FieldWorks controls for
			// the chooser in fact we will probably just using normal browse view. Then, that
			// chooser can just do the normal sorting that browse view stew, including letting
			// the user sort based on different properties.

			// however, we need a TreeView in many cases... I think there's also a FieldWorks
			// one of those that probably doesn't have sorting built-in yet...in which case we
			// might want to do the sorting here.

			using (var chooser = GetChooser(labels))
			{
				chooser.Cache = m_cache;
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
				if (m_configurationNode != null)
				{
					// Handle the default case ("owner") for text parameters.
					// JohnT: this approach depends on a new LCM method.
					var referenceTargetOwner = m_obj.ReferenceTargetOwner(m_flid);
					if (referenceTargetOwner != null)
					{
						chooser.TextParamHvo = referenceTargetOwner.Hvo;
					}
					chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
					chooser.InitializeExtras(m_configurationNode, PropertyTable);
				}

				var res = chooser.ShowDialog(MainControl.FindForm());
				if (DialogResult.Cancel == res)
				{
					return;
				}

				if (m_configurationNode != null)
				{
					chooser.HandleAnyJump();
				}

				if (chooser.ChosenOne != null)
				{
					AddItem(chooser.ChosenOne.Object);
				}
				else if (chooser.ChosenObjects != null)
				{
					SetItems(chooser.ChosenObjects);
				}
			}

			// If the configuration file says that we should put up a message dialog after a change has been made,
			// do that now.
			if (postDialogMessageTrigger != null)
			{
				MessageBoxExManager.Trigger(postDialogMessageTrigger);
			}
			// If the configuration file says to refresh the slice list, do that now.
			ChoicesMade?.Invoke(this, new EventArgs());
		}

		/// <summary>
		/// Get the SimpleListChooser.
		/// </summary>
		/// <param name="labels">List of objects to show in the chooser.</param>
		/// <returns>The SimpleListChooser.</returns>
		protected virtual SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			var x = new SimpleListChooser(m_persistProvider, labels, m_fieldName, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
			x.NullLabel.DisplayName  = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}

		/// <summary>
		/// Sets an atomic reference property or appends an object to a reference collection
		/// or sequence.
		/// </summary>
		public abstract void AddItem(ICmObject obj);

		/// <summary>
		/// Sets a reference collection or sequence.
		/// </summary>
		public abstract void SetItems(IEnumerable<ICmObject> chosenObjs);

		public virtual void UpdateDisplayFromDatabase()
		{
			CheckDisposed();
		}
	}
}