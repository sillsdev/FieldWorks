// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ReferenceLauncher.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	public class ReferenceLauncher : ButtonLauncher
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
				return ((CellarPropertyType)m_cache.DomainDataByFlid.MetaDataCache.GetFieldType((int)m_flid) == CellarPropertyType.ReferenceSequence);
			}
		}

		/// <summary>
		/// flag whether we can modify the slice contents.
		/// </summary>
		protected bool m_fEditable = true;
		/// <summary>
		/// Flag whether we can modify the slice contents.
		/// </summary>
		public bool Editable
		{
			get { return m_fEditable; }
			set { m_fEditable = value; }
		}
		/// <summary>
		/// Allow the launcher button to become visible only if we're editable.
		/// </summary>
		public override bool SliceIsCurrent
		{
			set
			{
				if (m_fEditable)
					base.SliceIsCurrent = value;
			}
		}
		#endregion // Properties

		#region Construction, Initialization, and Disposing
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ReferenceLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			//InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

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
			string displayWs = "analysis vernacular";
			//string displayWs = "best analysis";
			string postDialogMessageTrigger = null;

			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
				{
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
					postDialogMessageTrigger = XmlUtils.GetAttributeValue(node, "postChangeMessageTrigger", null);
				}
			}
			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid),
				m_displayNameProperty, displayWs);

			// I (JH) started down this road to sorting the object labels... it proved bumpy
			// and I bailed out and just turned on the "sorted" property of the chooser,
			// which gives us a dumb English sort.
			// but when it is time to get back to this... what I was doing what is misguided
			// because this sorter wants FdoObjects, but object labels don't have those on the
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

			//SIL.FieldWorks.Filters.RecordSorter sorter =
			//	new SIL.FieldWorks.Filters.PropertyRecordSorter("ShortName");
			//sorter.Sort ((ArrayList) labels);

			using (SimpleListChooser chooser = GetChooser(labels))
			{
				chooser.Cache = m_cache;
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);	// may set TextParamHvo
				if (m_configurationNode != null)
				{
					// Handle the default case ("owner") for text parameters.

					// This (old approach) works only if
					// all of the list items are owned by the same object as the first one in the
					// list.  (Later elements can be owned by elements owned by that first owner,
					// if you know what I mean.)
					//if (candidates.Count != 0)
					//    chooser.TextParamHvo = m_cache.GetOwnerOfObject((int)candidates[0]);
					// JohnT: this approach depends on a new FDO method.
					ICmObject referenceTargetOwner = m_obj.ReferenceTargetOwner(m_flid);
					if (referenceTargetOwner != null)
						chooser.TextParamHvo = referenceTargetOwner.Hvo;
					chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
					chooser.InitializeExtras(m_configurationNode, Mediator);
				}

				var res = chooser.ShowDialog(MainControl.FindForm());
				if (DialogResult.Cancel == res)
					return;

				if (m_configurationNode != null)
					chooser.HandleAnyJump();

				if (chooser.ChosenOne != null)
					AddItem(chooser.ChosenOne.Object);
				else if (chooser.ChosenObjects != null)
					SetItems(chooser.ChosenObjects);
			}

			//if the configuration file says that we should put up a message dialog after a change has been made,
			//do that now.
			if (postDialogMessageTrigger != null)
				XCore.XMessageBoxExManager.Trigger(postDialogMessageTrigger);
			// If the configuration file says to refresh the slice list, do that now.
			if (ChoicesMade != null)
				ChoicesMade(this, new EventArgs());
		}

		/// <summary>
		/// Get the SimpleListChooser/
		/// </summary>
		/// <param name="labels">List of objects to show in the chooser.</param>
		/// <returns>The SimpleListChooser.</returns>
		protected virtual SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			SimpleListChooser x = new SimpleListChooser(m_persistProvider, labels,
				m_fieldName, m_mediator.HelpTopicProvider);
			x.NullLabel.DisplayName  = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}

		/// <summary>
		/// Sets an atomic reference property or appends an object to a reference collection
		/// or sequence.
		/// </summary>
		/// <param name="obj">The obj.</param>
		public virtual void AddItem(ICmObject obj)
		{
			CheckDisposed();

			Debug.Assert(false, "Subclasses must override this to set the new value.");
		}

		/// <summary>
		/// Sets a reference collection or sequence.
		/// </summary>
		/// <param name="chosenObjs">The chosen objs.</param>
		public virtual void SetItems(IEnumerable<ICmObject> chosenObjs)
		{
			CheckDisposed();

			Debug.Assert(false, "Subclasses must override this to set the new value.");
		}

		public virtual void UpdateDisplayFromDatabase()
		{
			CheckDisposed();
		}
	}
}
