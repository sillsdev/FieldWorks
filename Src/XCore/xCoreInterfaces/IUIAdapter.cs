// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Choice.cs
// Authorship History: John Hatton
// Last reviewed:
// --------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;  //for ImageList
using System.Reflection;
using System;
using System.IO;
using SIL.Utils;

namespace XCore
{
	public interface IUIAdapter
	{
		Control Init(System.Windows.Forms.Form window, IImageCollection smallImages,
			IImageCollection largeImages, Mediator mediator);
		void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection);
		void CreateUIForChoiceGroup(ChoiceGroup group);
		void OnIdle();
		void FinishInit();
		void PersistLayout();
	}

	/// <summary>
	/// This could well be added to IUIAdapter, but rather than change all the implementations, for now
	/// it is an optional additional interface that some adapters implement. It should be implemented
	/// by adapters that need to be notified of major changes (such as Refresh), e.g., when a stylesheet
	/// definition changes.
	/// </summary>
	public interface IUIAdapterForceRegenerate
	{
		/// <summary>
		/// Used during Refresh, this indicates that the next idle call to regenerate the toolbar if anything
		/// has changed should behave unconditionally as if something HAS changed, and rebuild the toolbar.
		/// For example, the normal DoesToolStripNeedRegenerating does not notice that unselected items
		/// in the style combo list are different from the ones in the current menu.
		/// </summary>
		void ForceFullRegenerate();
	}

	public class AdapterAssemblyFactory
	{
		public static Assembly GetAdapterAssembly(string preferredLibrary)
		{
			Assembly adaptorAssembly = null;
			// load an adapter library from the same directory as the .dll we're running
			// We strip file:/ because that's not accepted by LoadFrom()
			var codeBasePath = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
			string baseDir = Path.GetDirectoryName(codeBasePath);
			try
			{
				adaptorAssembly = Assembly.LoadFrom(Path.Combine(baseDir, preferredLibrary));
			}
			catch (Exception)
			{
			}
			try
			{
				if (adaptorAssembly == null)
					adaptorAssembly = Assembly.LoadFrom(
						Path.Combine(baseDir, "xCoreOpenSourceAdapter.dll"));
			}
			catch (Exception)
			{
			}
			if (adaptorAssembly == null)
				throw new ApplicationException(String.Format("XCore Could not find the adapter library ( {0} or {1} ) at {2}", preferredLibrary, "FlexUIAdapter.dll", baseDir));
			return adaptorAssembly;
		}
	}

	public interface IUIMenuAdapter
	{
		bool HandleAltKey(System.Windows.Forms.KeyEventArgs e, bool wasDown); //for showing, for example, menus

		// This method supports various scenarios under which the context menu is expected to operate.
		// The first two parameters required, but the last two are optional.
		// The last two are special in that the implementor is expected to do preliminary work with them, before the menu opens,
		// and follow up work when the menu closes.
		void ShowContextMenu(ChoiceGroup group, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer);

		/// <summary>
		/// This is similar except that an action may be supplied to tweak the choice group after populating it.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="location"></param>
		/// <param name="temporaryColleagueParam"></param>
		/// <param name="sequencer"></param>
		/// <param name="adjustAfterPopulate"></param>
		void ShowContextMenu(ChoiceGroup group, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer, Action<ContextMenuStrip> adjustMenu);
	}

	/// <summary>
	/// This class is a 'Parameter Object', as described in Fowler's "Refactoring" book.
	/// It serves here to bundle the Mediator and an XCore colleague together for use
	/// by a client (in this case the implementor of IUIMenuAdapter. The motivation to
	/// introduce this came out of the need for code being done after a popup menu had closed.
	/// The post-closing activity expected here is the close event handler will remove
	/// the temporary colleague from the Mediator.
	/// The expected pre-opening activty is to add the colleague to the Mediator
	/// Both the Mediator and the colleague are required in order to meet both expectations,
	/// so an exception is thrown if either Constructor parameter is null.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "variable is a reference; it is owned by parent")]
	public class TemporaryColleagueParameter
	{
		private Mediator m_mediator;
		private IxCoreColleague m_temporaryColleague;
		private bool m_shouldDispose;

		/// <summary>
		/// Constructor with paramters being required.
		/// </summary>
		/// <param name="mediator">Mediator that will handle the collegue during its temporary liketime.</param>
		/// <param name="temporaryColleague"></param>
		public TemporaryColleagueParameter(Mediator mediator, IxCoreColleague temporaryColleague, bool shouldDispose)
		{
			if (mediator == null)
				throw new ArgumentNullException("'mediator' parameter cannot be null.");
			if (temporaryColleague == null)
				throw new ArgumentNullException("'temporaryColleague' parameter cannot be null.");

			m_mediator = mediator;
			m_temporaryColleague = temporaryColleague;
			m_shouldDispose = shouldDispose;
		}

		public Mediator Mediator
		{
			get { return m_mediator; }
		}

		public bool ShouldDispose
		{
			get { return m_shouldDispose; }
		}

		public IxCoreColleague TemporaryColleague
		{
			get { return m_temporaryColleague; }
		}
	}

	public interface ITestableUIAdapter
	{
		/// <summary>
		/// works for sub-groups too.
		/// </summary>
		/// <param name="groupId"></param>
		/// <returns></returns>
		int GetItemCountOfGroup (string groupId);
		void ClickItem (string groupId, string itemId);
		bool IsItemEnabled(string groupId, string itemId);
		bool HasItem(string groupId, string itemId);
		void ClickOnEverything();
	}

	/// <summary>
	/// A trivial class passed to an api that expects an arbitrary object, in order to allow it to modify a tooltip.
	/// </summary>
	public class ToolTipHolder
	{
		public string ToolTip { get; set; }
	}

	//--------------------------------------------------------------------
	/// <summary>
	/// UIItemDisplayProperties contains the details of how an item should be displayed
	/// e.g its label, whether it is enabled, or whether it has a check mark.
	/// </summary>
	//--------------------------------------------------------------------
	public class UIItemDisplayProperties
	{
		#region Fields
		protected ChoiceGroup m_group;
		protected string m_text;
		protected bool m_enabled;
		protected bool m_checked;
		protected bool m_visible;
		//protected bool m_radio;
		protected string m_imageLabel;

		#endregion
		#region Properties

		/// <summary>
		///
		/// </summary>
		public ChoiceGroup Group
		{
			get { return m_group; }
		}

		public string Text  { get {return m_text;} set{m_text =value;} } // get/set text

		/// <summary>
		/// NOTE: Never assign the Visible property to the Enabled property since Visible.get()
		/// is based on the value of Enabled.
		/// </summary>
		public bool Enabled { get {return m_enabled;} set {m_enabled = value ;} } // enable object

		/// <summary>
		/// Note Never assign the Visible property to the Enabled property because Visible.get()
		/// is based on the value of Enabled.
		/// </summary>
		public bool Visible
		{
			get
			{
				//review: I'm not sure really what to do for this...
				//somehow we want to incorporate the effect of the defaultVisible attribute
				//what I have here is not quite right... it will hide the item just because
//				//it is not currently enabled
				// See the comment on Choice.cs: describes needing set enabled and visible to
				// the same value to keep from showing a bool choice that was deafulted to
				// not visible.
				if (m_enabled || m_visible)
					return true;
				else
					return m_visible;
			}
			set
			{
				m_visible = value ;
			}
		}

		public bool Checked { get {return m_checked;} set{m_checked = value;} } // check it
		public string ImageLabel { get {return m_imageLabel;} }
		//public bool Radio   { get; set; } // use radio button, not checkbox
		#endregion

		public UIItemDisplayProperties(ChoiceGroup group, string text, bool enabled, string imageLabel,bool defaultVisible)
		{
			m_group = group;
			m_text = text;
			m_enabled = enabled;
			m_checked = false;
			m_visible = defaultVisible;
			//TODO: RickM See 'Note' for Visible and Enabled properties.
			//I think to handle the defaultVisible XML attribute better we might want to change
			//the preceeding statement to look like this
			//m_visible = defaultVisible || m_enabled;
			m_imageLabel=imageLabel;
		}
	}

	//--------------------------------------------------------------------
	/// <summary>
	/// UIItemDisplayProperties contains the details of how an item should be displayed
	/// e.g its label, whether it is enabled, or whether it has a check mark.
	/// </summary>
	//--------------------------------------------------------------------
	public class UIListDisplayProperties
	{
		#region Fields
		List m_list;
		string m_propertyName;
		#endregion

		#region Properties
		public List List { get {return m_list;} }

		/// <summary>
		/// this is used for lists which are keyed to change the value of a property.
		/// having this here allows the interested colleague to actually change the property
		/// of the list at run-time, whenever it is redisplayed.
		/// </summary>
		public string PropertyName
		{
			get
			{
				return m_propertyName;
			}
			set
			{
				m_propertyName = value;
			}
		}
		#endregion

		public UIListDisplayProperties(List list)
		{
			m_list= list;
		}
	}

}
