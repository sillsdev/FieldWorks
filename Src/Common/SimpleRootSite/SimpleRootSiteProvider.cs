using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	#region SimpleRootSiteDataProvider class

	static class SimpleRootSiteDataProviderServices
	{
		public static void Invoke(this Control control, Action method)
		{
			if (control.InvokeRequired)
				control.Invoke(method);
			else
				method();
		}

		public static TResult Invoke<TResult>(this Control control, Func<TResult> method)
		{
			return control.InvokeRequired ? (TResult) control.Invoke(method) : method();
		}
	}

	/// <summary>
	///
	/// </summary>
	public interface IChildControlNavigation : IRawElementProviderFragment
	{
		/// <summary>
		/// Navigates to the specified sibling given a child control.
		/// </summary>
		/// <param name="child">The child from which to navigate</param>
		/// <param name="direction">The direction to the sibling.</param>
		/// <returns></returns>
		IRawElementProviderFragment Navigate(IRawElementProviderFragment child, NavigateDirection direction);

		/// <summary>
		/// Gets the index of the given child.
		/// </summary>
		/// <param name="child">The child.</param>
		/// <returns></returns>
		int IndexOf(IRawElementProviderFragment child);
	}

	/// <summary>
	/// SimpleRootSiteDataProvider enables Simple Rootsite data to be exposed to
	/// UI Automation clients. SimpleRootSite class provides this to UI Automation
	/// via a WM_GETOBJECT message processed in SimpleRootSite.OriginalWndProc().
	/// </summary>
	public class SimpleRootSiteDataProvider : BaseFragmentProvider<SimpleRootSiteDataProvider>, IRawElementProviderFragmentRoot, ITextProvider
	{
		private SimpleRootSite m_host; // the control hosting this provider

		/// <summary>
		/// Constructor for SimpleRootSiteDataProvider
		/// </summary>
		public SimpleRootSiteDataProvider(SimpleRootSite host)
			: this(host, simpleRootSiteProvider => simpleRootSiteProvider.GetDefaultChildControls)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteDataProvider"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="childControlFactory">The child control factory.</param>
		public SimpleRootSiteDataProvider(SimpleRootSite host,
			Func<IChildControlNavigation, IList<IRawElementProviderFragment>> childControlFactory)
			: this(host, simpleRootSiteProvider => childControlFactory) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteDataProvider"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="childControlFactory">The child control factory.</param>
		protected SimpleRootSiteDataProvider(SimpleRootSite host,
			Func<SimpleRootSiteDataProvider, Func<IChildControlNavigation, IList<IRawElementProviderFragment>>> childControlFactory)
			: base(null, host, childControlFactory)
		{
			Initialize(host);
		}

		private void Initialize(SimpleRootSite host)
		{
			m_host = host;
		}

		/// <summary>
		///
		/// </summary>
		internal SimpleRootSite Site
		{
			get { return m_host; }
			set { Initialize(value); }
		}

		#region Methods of IRawElementProviderSimple

		/// <summary>
		/// Gets a base provider for this element.
		/// </summary>
		public override IRawElementProviderSimple HostRawElementProvider
		{
			get
			{
				IntPtr handle = m_host.Invoke(() => m_host.Handle);
				return AutomationInteropProvider.HostProviderFromHandle(handle);
			}
		}

		/// <summary>
		/// Retrieves an object that provides support for a control pattern on a UI Automation element.
		/// </summary>
		/// <param name="patternId"></param>
		/// <returns>Object that implements the pattern interface, or null if the pattern is not supported</returns>
		public override Object GetPatternProvider(int patternId)
		{
			if (patternId == TextPatternIdentifiers.Pattern.Id)
			{
				return this;
			}
			return null;
		}

		/// <summary>
		/// This (optional property) should be unique across siblings...but since that's non trival we don't return a value.
		///
		/// For a full definition of the use of AutomationElementIdentifiers.AutomationIdProperty
		/// see http://msdn.microsoft.com/en-us/library/system.windows.automation.automationelement.automationidproperty.aspx
		/// Return values of the property are of type String. The default value for the property is an empty string.
		/// When it is available the AutomationIdProperty of an element is expected to always be the same in any instance
		/// of the application regardless of the local language. The value should be unique among sibling elements but not
		/// necessarily unique across the entire desktop. For example, multiple instances of an application, or multiple folder
		/// views in Microsoft Windows Explorer, may contain elements with the same AutomationIdProperty, such as "SystemMenuBar".
		/// While support of an AutomationId is always recommended for better testability, this property is not mandatory.
		/// Where it is supported, an AutomationId is useful for creating test automation scripts that run regardless of UI language.
		/// Clients should make no assumptions regarding the AutomationIds exposed by other applications. An AutomationId is not necessarily
		/// guaranteed to be stable across different releases or builds of an application.
		/// </summary>
		internal string AutomationIdProperty { get { return ""; } }

		/// <summary>
		/// For now, returns the Class name of the SimpleRootSite.
		///
		/// (From http://msdn.microsoft.com/en-us/library/system.windows.automation.automationelement.classnameproperty.aspx)
		/// The class name depends on the implementation of the UI Automation provider and therefore cannot be counted upon to be in a
		/// standard format. However, if you know the class name you can use it to verify that your application is working with the expected
		/// UI Automation element.
		/// </summary>
		internal string ClassName
		{
			get { return m_host.GetType().Name; }
		}

		/// <summary>
		/// Retrieves the value of a property supported by the UI Automation provider.
		/// </summary>
		/// <param name="propertyId"></param>
		/// <returns>The property value, or null if the property is not supported by this provider, or NotSupported if it is not supported at all</returns>
		public override Object GetPropertyValue(int propertyId)
		{
			if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
			{
				return ControlType.Group.Id;
			}
			if (propertyId == AutomationElementIdentifiers.NameProperty.Id)
			{
				return null; // group controls are typically self-labeling.
			}
			if (propertyId == AutomationElementIdentifiers.IsControlElementProperty.Id)
			{
				return true; // group controls always have control view.
			}
			if (propertyId == AutomationElementIdentifiers.IsContentElementProperty.Id)
			{
				return true; // group controls always have content view
			}
			if (propertyId == AutomationElementIdentifiers.AutomationIdProperty.Id)
			{
				return AutomationIdProperty;  // no siblings are expected.
			}
			if (propertyId == AutomationElementIdentifiers.ClassNameProperty.Id)
			{
				return ClassName;
			}
			if (propertyId == AutomationElementIdentifiers.NativeWindowHandleProperty.Id)
			{
				return m_host.Invoke(() => m_host.Handle);
			}
			return base.GetPropertyValue(propertyId);

		}

		#endregion Methods of IRawElementProviderSimple

		#region Methods of IRawElementProviderFragment

		/// <summary>
		/// Gets the bounding rectangle.
		/// </summary>
		/// <remarks>
		/// Fragment roots should return an empty rectangle. UI Automation will get the rectangle
		/// from the host control (the HWND in this case).
		/// </remarks>
		public override System.Windows.Rect BoundingRectangle
		{
			get { return System.Windows.Rect.Empty; }
		}

		/// <summary>
		/// Gets the root of this fragment.
		/// </summary>
		/// <returns>This fragment root</returns>
		public override IRawElementProviderFragmentRoot FragmentRoot
		{ get { return this; } }

		/// <summary>
		/// Gets the runtime identifier of the UI Automation element.
		/// </summary>
		/// <returns>Fragment roots return null.</returns>
		public override int[] GetRuntimeId()
		{ return null; }

		/*
		/// <summary>
		/// Navigates to adjacent elements in the UI Automation tree.
		/// </summary>
		/// <param name="direction">Direction of navigation.</param>
		/// <returns>The element in that direction, or null.</returns>
		/// <remarks>The provider only returns directions that it is responsible for.
		/// UI Automation knows how to navigate between HWNDs, so only the custom item
		/// navigation needs to be provided.
		///</remarks>
		public virtual IRawElementProviderFragment Navigate(NavigateDirection direction)
		{
			if (!ComputedChildElements)
				DetectChildElements();

			switch (direction)
			{
				case NavigateDirection.FirstChild:
					return m_embeddedControls.FirstOrDefault();
				case NavigateDirection.LastChild:
					return m_embeddedControls.LastOrDefault();
				default:
					return null;
			}
		}

		/// <summary>
		/// Gets the first child of this fragment.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetFirstChild()
		{
			return m_embeddedControls.FirstOrDefault();
		}

		/// <summary>
		/// Gets the last child of this fragment.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetLastChild()
		{
			return m_embeddedControls.LastOrDefault();
		}
		*/

		#region IChildControlNavigation Members

		#endregion IChildControlNavigation Members

		private IList<IRawElementProviderFragment> GetDefaultChildControls(IChildControlNavigation rootFragment)
		{
			IList<IRawElementProviderFragment> childControls = new List<IRawElementProviderFragment>();
			var rootb = m_host.RootBox;
			// TODO: when we detect other control types later, we'll need to sort them according to the
			// selection locations.
			// look for a selectable text to provide an edit control.
			IVwSelection selEditable = rootb.MakeSimpleSel(true, true, true, false);
			if (selEditable == null)
				return childControls;
			// we found an editable field, so create an edit control for that selection-range.
			//var editControl = new SimpleRootSiteEditControl(this, selEditable);
			var editControl = new SimpleRootSiteEditControl(rootFragment, m_host, selEditable, GetLabel());
			childControls.Add(editControl);
			return childControls;
		}

		internal string GetLabel()
		{
			string label = string.Empty; // no label.
			var rootb = m_host.RootBox;
			// in general, assume that the label is the first non-editable range
			// if the first editable region comes after the non-editable range.
			// otherwise assume we don't have a label.
			IVwSelection selFirstNonEditable = rootb.MakeSimpleSel(true, false, true, false);
			IVwSelection selFirstEditable = rootb.MakeSimpleSel(true, true, true, false);
			if (selFirstNonEditable == null || selFirstEditable == null)
				return string.Empty; // no label.

			//var sh = SelectionHelper.Create(m_host);
			// make a range selection based upon these two selections, and test to see if the anchor (non-editable)
			// comes before the end (editable).
			var labelRangeSel = rootb.MakeRangeSelection(selFirstNonEditable, selFirstNonEditable, false);
			if (labelRangeSel.EndBeforeAnchor)
				return string.Empty;
			ITsString tsslabel;
			labelRangeSel.GetSelectionString(out tsslabel, "; ");
			if (tsslabel.Length > 0)
				label = tsslabel.Text.Trim();
			return label;
		}



		/// <summary>
		/// Responds to a client request to set the focus to this control.
		/// </summary>
		/// <remarks>Setting focus to the control is handled by the parent window.</remarks>
		public override void SetFocus()
		{
			throw new Exception("The method is not implemented.");
		}

		#endregion Methods of IRawElementProviderFragment

		#region Methods of IRawElementProviderFragmentRoot

		/// <summary>
		/// Gets the child element at the specified point.
		/// </summary>
		/// <param name="x">Distance from the left of the application window.</param>
		/// <param name="y">Distance from the top of the application window.</param>
		/// <returns>The provider for the element at that point.</returns>
		public IRawElementProviderFragment ElementProviderFromPoint(double x, double y)
		{
			if (m_embeddedControls.Count > 0)
			{
				// see if the point is within the BoundingRectangle of any of the sub controls.
				foreach (var control in m_embeddedControls)
				{
					if (control.BoundingRectangle.Contains(x, y))
						return control;
				}
			}
			return null;
			/*
			System.Drawing.Point clientPoint = new System.Drawing.Point((int)x, (int)y);
			int index = -1;
			// Invoke control method on separate thread to avoid clashing with UI.
			// Use anonymous method for simplicity.
			this.OwnerListControl.Invoke(new MethodInvoker(delegate()
			{
				clientPoint = this.OwnerListControl.PointToClient(clientPoint);
			}));

			index = OwnerListControl.ItemIndexFromPoint(clientPoint);
			if (index == -1)
			{
				return null;
			}
			return null; // TBD GetProviderForIndex(index);
			 */
		}

		/// <summary>
		///
		/// </summary>
		/// <returns>The selected item.</returns>
		public IRawElementProviderFragment GetFocus()
		{
			if (m_host.RootBox.SelectionState != VwSelectionState.vssEnabled)
				return null;
			var ipLocation = m_host.IPLocation;
			// TODO: handle range selections
			return ElementProviderFromPoint(ipLocation.X, ipLocation.Y);
			/* int index = OwnerListControl.SelectedIndex;
				return GetProviderForIndex(index);
			*/

		}

		#endregion Methods of IRawElementProviderFragmentRoot

		#region ITextProvider Members

		/// <summary>
		/// Gets a text range that encloses the main text of a document.
		/// </summary>
		/// <value></value>
		public ITextRangeProvider DocumentRange
		{
			get
			{
				// setup additional providers
				return new SimpleRootSiteTextRangeProvider(m_host);
			}
		}

		/// <summary>
		/// Retrieves a collection of disjoint text ranges associated with the current text selection or selections.
		/// </summary>
		/// <returns>A collection of disjoint text ranges.</returns>
		/// <exception cref="T:System.InvalidOperationException">
		/// If the UI Automation provider does not support text selection.
		/// </exception>
		public ITextRangeProvider[] GetSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves an array of disjoint text ranges from a text container where each text range begins with the first partially visible line through to the end of the last partially visible line.
		/// </summary>
		/// <returns>
		/// The collection of visible text ranges within the container or an empty array. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		public ITextRangeProvider[] GetVisibleRanges()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves a text range enclosing a child element such as an image, hyperlink, or other embedded object.
		/// </summary>
		/// <param name="childElement">The enclosed object.</param>
		/// <returns>A range that spans the child element.</returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the child element is a null reference (Nothing in Microsoft Visual Basic .NET).
		/// </exception>
		public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the degenerate (empty) text range nearest to the specified screen coordinates.
		/// </summary>
		/// <param name="screenLocation">The location in screen coordinates.</param>
		/// <returns>
		/// A degenerate range nearest the specified location. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If a given point is outside the UI Automation element associated with the text pattern.
		/// </exception>
		public ITextRangeProvider RangeFromPoint(System.Windows.Point screenLocation)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a value that specifies whether a text provider supports selection and, if so, the type of selection supported.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of None, Single, or Multiple from <see cref="T:System.Windows.Automation.SupportedTextSelection"/>.
		/// </returns>
		public SupportedTextSelection SupportedTextSelection
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}

	/// <summary>
	/// Basic image control, that does not provide content or clicking.
	/// </summary>
	public class ImageControl : VwSelectionBasedControl<ImageControl>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ImageControl"/> class.
		/// </summary>
		/// <param name="parent">The parent control.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		public ImageControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection)
			: base(parent, site, selection)
		{
			// Localized string corresponding to the Image control type.
			AddStaticProperty(AutomationElementIdentifiers.LocalizedControlTypeProperty.Id, Properties.Resources.ksImage);
		}

		#region IRawElementProviderSimple Members

		/// <summary>
		/// (Not supported. IGridItemProvider and ITableItemProvider are not needed for this image control)
		/// Object that implements the pattern interface, or <see langword="null"/> if the pattern is not supported.
		/// </summary>
		/// <param name="patternId">The pattern id.</param>
		/// <returns><see langword="null"/></returns>
		public override object GetPatternProvider(int patternId)
		{
			return null;
		}

		/// <summary>
		/// Retrieves the value of a property supported by the UI Automation provider.
		/// </summary>
		/// <param name="propertyId">The property identifier.</param>
		/// <returns>
		/// The property value, or a null if the property is not supported by this provider, or <see cref="F:System.Windows.Automation.AutomationElementIdentifiers.NotSupported"/> if it is not supported at all.
		/// </returns>
		public override object GetPropertyValue(int propertyId)
		{
			if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
			{
				return null; //ControlType.Image;
			}
			if (propertyId == AutomationElementIdentifiers.NameProperty.Id)
			{
				/*
				 * We're not required to have a name if the image control is purely decorative.
				 *
				 * http://msdn.microsoft.com/en-us/library/ms746603.aspx
				 * "The Name property must be exposed for all image controls that contain information.
				 * Programmatic access to this information requires that a textual equivalent to the graphic be provided.
				 * If the image control is purely decorative, it must only show up in the control view of the UI Automation tree
				 * and is not required to have a name. UI frameworks must support an ALT or alternate text property on images that
				 * can be set from within their framework. This property will then map to the UI Automation Name property."
				 */
				return null;
			}
			if (propertyId == AutomationElementIdentifiers.IsContentElementProperty.Id)
			{
				return false;
			}
			if (propertyId == AutomationElementIdentifiers.IsControlElementProperty.Id)
			{
				return true;
			}
			return base.GetPropertyValue(propertyId);
		}

		#endregion
	}

	/// <summary>
	/// Button control.
	/// </summary>
	public abstract class UiaButton : VwSelectionBasedControl<UiaButton>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UiaButton"/> class.
		/// </summary>
		/// <param name="parent">The parent control.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		/// <param name="buttonName">The name of the button control is the text that is used to label it.
		/// Whenever an image is used to label a button, alternate text must be supplied for the button's Name property.
		/// (e.g. "Drop Down Button")</param>
		protected UiaButton(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection, string buttonName)
			: base(parent, site, selection, button => button.CreateLabelImage)
		{
			// Localized string corresponding to the Button control type.
			AddStaticProperty(AutomationElementIdentifiers.LocalizedControlTypeProperty.Id, Properties.Resources.ksButton);
			// The name of the button control is the text that is used to label it. Whenever an image is used to label a button,
			// alternate text must be supplied for the button's Name property.
			AddStaticProperty(AutomationElementIdentifiers.NameProperty.Id, buttonName);
			AddStaticProperty(AutomationElementIdentifiers.ControlTypeProperty.Id, null); // ControlType.Button
			// The Button control must always be content.
			AddStaticProperty(AutomationElementIdentifiers.IsContentElementProperty.Id, true);
			// The Button control must always be a control.
			AddStaticProperty(AutomationElementIdentifiers.IsControlElementProperty.Id, true);

			// The Button control typically must support an accelerator key to enable an end user to perform the action it represents quickly from the keyboard.
			AddStaticProperty(AutomationElementIdentifiers.AcceleratorKeyProperty.Id, null);
			// The value of this property needs to be unique across all controls in an application.
			AddStaticProperty(AutomationElementIdentifiers.AutomationIdProperty.Id, null);
			// The outermost rectangle that contains the whole control.
			AddStaticProperty(AutomationElementIdentifiers.BoundingRectangleProperty.Id, null);
			// Supported if there is a bounding rectangle. If not every point within the bounding
			// rectangle is clickable, and you perform specialized hit testing, then override and provide a clickable point.
			AddStaticProperty(AutomationElementIdentifiers.ClickablePointProperty.Id, null);
			// The Help Text can indicate what the end result of activating the button will be.
			// This is typically the same type of information presented through a ToolTip.
			AddStaticProperty(AutomationElementIdentifiers.HelpTextProperty.Id, null);
			// If the control can receive keyboard focus, it must support this property.
			AddStaticProperty(AutomationElementIdentifiers.IsKeyboardFocusableProperty.Id, true);
			// Button controls are self-labeled by their contents.
			AddStaticProperty(AutomationElementIdentifiers.LabeledByProperty.Id, null);
		}

		private IList<IRawElementProviderFragment> CreateLabelImage(IChildControlNavigation childProvider)
		{
			if (m_site.Invoke(() => Selection.SelType == VwSelType.kstPicture))
			{
				// create ImageControl.
				var imageControl = new ImageControl(this, m_site, Selection);
				return new List<IRawElementProviderFragment> { imageControl };
			}
			return new List<IRawElementProviderFragment>();
		}

		#region IRawElementProviderSimple Members

		/*
		/// <summary>
		/// All buttons should support the Invoke control pattern or the Toggle control pattern. (or possibly IExpandCollapseProvider)
		/// Object that implements the pattern interface, or <see langword="null"/> if the pattern is not supported.
		/// </summary>
		/// <param name="patternId">The pattern id.</param>
		/// <returns><see langword="null"/></returns>
		public abstract object GetPatternProvider(int patternId); */

		#endregion
	}

	/// <summary>
	/// Button that invokes a given action
	/// </summary>
	public class UiaInvokeButton : UiaButton, IInvokeProvider
	{
		private readonly Action<IVwSelection> m_invokeAction;

		/// <summary>
		/// Initializes a new instance of the <see cref="UiaInvokeButton"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		/// <param name="buttonName">Name of the button.</param>
		/// <param name="invokeAction">The invoke action.</param>
		public UiaInvokeButton(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection, string buttonName,
			Action<IVwSelection> invokeAction)
			: base(parent, site, selection, buttonName)
		{
			m_invokeAction = invokeAction;
		}

		#region Overrides of BaseFragmentProvider

		/// <summary>
		/// Retrieves an object that provides support for a control pattern on a UI Automation element.
		/// </summary>
		/// <param name="patternId">Identifier of the pattern.</param>
		/// <returns>
		/// Object that implements the pattern interface, or null if the pattern is not supported.
		/// </returns>
		public override object GetPatternProvider(int patternId)
		{
			if (patternId == InvokePatternIdentifiers.Pattern.Id)
				return this;
			return null;
		}

		#endregion

		#region Implementation of IInvokeProvider

		/// <summary>
		/// Execute this instance's invoke action.
		/// </summary>
		public void Invoke()
		{
			m_site.Invoke(() => m_invokeAction(Selection));
		}

		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public abstract class BaseFragmentProvider<TFragmentProvider> : IChildControlNavigation
		where TFragmentProvider : BaseFragmentProvider<TFragmentProvider>
	{
		/// <summary>
		///
		/// </summary>
		protected SimpleRootSite m_site;

		/// <summary>
		///
		/// </summary>
		protected IRawElementProviderFragmentRoot m_rootProvider;

		/// <summary>
		///
		/// </summary>
		protected IChildControlNavigation m_parent;

		private readonly Dictionary<int, object> staticProps = new Dictionary<int, object>();
		/// <summary>
		///
		/// </summary>
		protected IList<IRawElementProviderFragment> m_embeddedControls = new List<IRawElementProviderFragment>();
		/// <summary>
		///
		/// </summary>
		protected readonly Func<IChildControlNavigation, IList<IRawElementProviderFragment>> m_childControlFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseFragmentProvider{TFragmentProvider}"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="site">The site.</param>
		protected BaseFragmentProvider(IChildControlNavigation parent, SimpleRootSite site)
		{
			m_parent = parent;
			if (parent != null)
				m_rootProvider = parent.FragmentRoot;
			else
				m_rootProvider = this as IRawElementProviderFragmentRoot;
			m_site = site;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseFragmentProvider{TFragmentProvider}"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="childControlFactory">The child control factory.</param>
		protected BaseFragmentProvider(IChildControlNavigation parent, SimpleRootSite site,
			Func<TFragmentProvider, Func<IChildControlNavigation, IList<IRawElementProviderFragment>>> childControlFactory)
			: this(parent, site)
		{
			// Setup child control creation
			m_childControlFactory = childControlFactory(this as TFragmentProvider);
		}

		/// <summary>
		/// Gets a base provider for this element.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The base provider, or null.
		/// </returns>
		public virtual IRawElementProviderSimple HostRawElementProvider
		{
			get { return null; }
		}

		/// <summary>
		/// Gets a value that specifies characteristics of the UI Automation provider; for example, whether it is a client-side or server-side provider.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// Either <see cref="F:System.Windows.Automation.Provider.ProviderOptions.ClientSideProvider"/> or <see cref="F:System.Windows.Automation.Provider.ProviderOptions.ServerSideProvider"/>.
		/// </returns>
		public ProviderOptions ProviderOptions
		{
			get { return ProviderOptions.ServerSideProvider; }
		}

		/// <summary>
		/// Gets the bounding rectangle of this element.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The bounding rectangle, in screen coordinates.
		/// </returns>
		public virtual System.Windows.Rect BoundingRectangle
		{
			get; protected set;
		}

		/// <summary>
		/// Retrieves the root node of the fragment.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The root node.
		/// </returns>
		public virtual IRawElementProviderFragmentRoot FragmentRoot
		{
			get { return m_rootProvider; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the child controls have been computed.
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if the child controls have been computed; otherwise, <see langword="false"/>.
		/// </value>
		protected bool ComputedChildElements { get; set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="propertyId"></param>
		/// <param name="value"></param>
		protected void AddStaticProperty(int propertyId, object value)
		{
			staticProps.Add(propertyId, value);
		}

		/// <summary>
		/// Retrieves an array of fragment roots that are embedded in the UI Automation element tree rooted at the current element.
		/// </summary>
		/// <returns>An array of root fragments, or null.</returns>
		public virtual IRawElementProviderSimple[] GetEmbeddedFragmentRoots()
		{
			return null;
		}

		/// <summary>
		/// Retrieves the UI Automation element in a specified direction within the tree.
		/// </summary>
		/// <param name="direction">The direction in which to navigate.</param>
		/// <returns>
		/// The element in the specified direction, or null if there is no element in that direction
		/// </returns>
		// Routing function for going to neighboring elements.  We implemented
		// this to delegate to other virtual functions, so don't override it.
		public IRawElementProviderFragment Navigate(NavigateDirection direction)
		{
			if (!ComputedChildElements)
				DetectChildElements();

			switch (direction)
			{
				case NavigateDirection.Parent: return m_parent;
				case NavigateDirection.FirstChild: return GetFirstChild();
				case NavigateDirection.LastChild: return GetLastChild();
				case NavigateDirection.NextSibling: return GetNextSibling();
				case NavigateDirection.PreviousSibling: return GetPreviousSibling();
			}
			return null;
		}

		//public virtual IRawElementProviderFragment Navigate(NavigateDirection direction)
		//{
		//    switch (direction)
		//    {
		//        case NavigateDirection.Parent:
		//            return m_rootProvider;
		//        case NavigateDirection.NextSibling:
		//        case NavigateDirection.PreviousSibling:
		//            var parent = m_rootProvider as IChildControlNavigation;
		//            if (parent != null)
		//                return parent.Navigate(this, direction);
		//            break;
		//    }

		//    return null;
		//}

		#region Override points

		/// <summary>
		/// Gets the first child of this fragment.
		/// </summary>
		/// <returns></returns>
		protected virtual IRawElementProviderFragment GetFirstChild()
		{
			return m_embeddedControls.FirstOrDefault();
		}

		/// <summary>
		/// Gets the last child of this fragment.
		/// </summary>
		/// <returns></returns>
		protected virtual IRawElementProviderFragment GetLastChild()
		{
			return m_embeddedControls.LastOrDefault();
		}

		/// <summary>
		/// Gets the next sibling of this fragment.
		/// </summary>
		/// <returns></returns>
		protected virtual IRawElementProviderFragment GetNextSibling()
		{
			return null;
		}

		/// <summary>
		/// Gets the previous sibling of this fragment.
		/// </summary>
		/// <returns></returns>
		protected virtual IRawElementProviderFragment GetPreviousSibling()
		{
			return null;
		}
		#endregion

		/// <summary>
		/// Sets the focus to this element.
		/// NOTE: also installs the range selection.
		/// </summary>
		public abstract void SetFocus();

		/// <summary>
		/// Retrieves an object that provides support for a control pattern on a UI Automation element.
		/// </summary>
		/// <param name="patternId">Identifier of the pattern.</param>
		/// <returns>
		/// Object that implements the pattern interface, or null if the pattern is not supported.
		/// </returns>
		public abstract object GetPatternProvider(int patternId);

		/// <summary>
		/// Retrieves the value of a property supported by the UI Automation provider.
		/// </summary>
		/// <param name="propertyId">The property identifier.</param>
		/// <returns>
		/// The property value, or a null if the property is not supported by this provider, or <see cref="F:System.Windows.Automation.AutomationElementIdentifiers.NotSupported"/> if it is not supported at all.
		/// </returns>
		public virtual object GetPropertyValue(int propertyId)
		{
			object value;
			if (staticProps.TryGetValue(propertyId, out value))
			{
				return value;
			}

			// Switching construct to go get the right property from a virtual method.
			if (propertyId == AutomationElementIdentifiers.NameProperty.Id)
			{
				return GetName();
			}
			// Add further cases here to support more properties.
			// Do note that it may be more efficient to handle static properties
			// by adding them to the static props list instead of using methods.

			return null;
		}

		/// <summary>
		/// Get the localized name for this control
		/// </summary>
		/// <returns></returns>
		protected virtual string GetName()
		{
			return GetType().Name;
		}

		/// <summary>
		/// Retrieves the runtime identifier of an element.
		/// </summary>
		/// <returns>
		/// The unique run-time identifier of the element.
		/// </returns>
		public virtual int[] GetRuntimeId()
		{
			if (m_parent != null)
			{
				int index = m_parent.IndexOf(this);
				return new[] {AutomationInteropProvider.AppendRuntimeId, index};
			}
			return new int[0];
		}

		/// <summary>
		/// Navigates to the specified sibling given a child control.
		/// </summary>
		/// <param name="child">The child from which to navigate</param>
		/// <param name="direction">The direction to the sibling.</param>
		/// <returns></returns>
		public IRawElementProviderFragment Navigate(IRawElementProviderFragment child, NavigateDirection direction)
		{
			int index = IndexOf(child);

			switch (direction)
			{
				case NavigateDirection.PreviousSibling:
					if (index > 0)
						return m_embeddedControls[index - 1];
					break;
				case NavigateDirection.NextSibling:
					if (index < m_embeddedControls.Count - 1)
						return m_embeddedControls[index + 1];
					break;
			}

			return null;
		}

		/// <summary>
		/// Gets the index of the given child.
		/// </summary>
		/// <param name="child">The child.</param>
		/// <returns></returns>
		public int IndexOf(IRawElementProviderFragment child)
		{
			return m_embeddedControls.IndexOf(child);
		}

		/// <summary>
		/// Detects the child elements.
		/// </summary>
		protected void DetectChildElements()
		{
			// this isn't necessarily the first "control type" in the rootbox, but it's the
			// first we've implemented.
			if (m_childControlFactory != null)
				m_embeddedControls = m_childControlFactory(this);
			ComputedChildElements = true;
			// search through simple root site structure and look for edit boxes and
			// create an edit control for each of those boxes.
			//m_host.RootBox
			//var selectAllRootBox = SimpleRootSiteTextRangeProvider.SelectAll(m_host.RootBox, false, false);
			//var sh = SelectionHelper.Create(selectAllRootBox, m_host);
			//ITsTextProps[] vttp;
			//IVwPropertyStore[] vvps;
			//SelLevInfo[] vsliTop = sh.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			//SelLevInfo[] vsliBottom = sh.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);
			//sh.GetCurrSelectionProps(out vttp, out vvps);
			//for (int i = 0; i < vttp.Length; ++i)
			//{
			//    ITsTextProps ttp = vttp[i];
			//    IVwPropertyStore vps = vvps[i];
			//    for (int itp = 0; itp < ttp.IntPropCount; ++itp)
			//    {
			//        int prop;
			//        int propVal;
			//        if (SelectionHelper.IsEditable(ttp, vps))
			//        {
			//        }
			//        else
			//        {
			//        }
			//    }
			//}

		}
	}

	/// <summary>
	/// Base class for controls that are based upon view selections
	/// </summary>
	public abstract class VwSelectionBasedControl<TSelectionBasedControl> : BaseFragmentProvider<TSelectionBasedControl>
		where TSelectionBasedControl : VwSelectionBasedControl<TSelectionBasedControl>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VwSelectionBasedControl{TSelectionBasedControl}"/> class.
		/// </summary>
		/// <param name="parent">The control parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		protected VwSelectionBasedControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection)
			: this(parent, site, selection, selectionBasedControl => null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="VwSelectionBasedControl{TSelectionBasedControl}"/> class.
		/// </summary>
		/// <param name="parent">The control parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		/// <param name="childControlFactory">The child control factory.</param>
		protected VwSelectionBasedControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection,
			Func<TSelectionBasedControl, Func<IChildControlNavigation, IList<IRawElementProviderFragment>>> childControlFactory)
			: base(parent, site, childControlFactory)
		{
			Selection = selection;
			m_site.Invoke(ComputeScreenBoundingRectangle);
		}

		#region Other protected methods

		#endregion

		/// <summary>
		/// Gets or sets the selection that this control is based upon.
		/// </summary>
		/// <value>The selection.</value>
		protected IVwSelection Selection { get; set; }

		/// <summary>
		/// Computes the screen bounding rectangle.
		/// </summary>
		protected void ComputeScreenBoundingRectangle()
		{
			// probably should extend this to the enclosing (white) edit box;
			using (new HoldGraphics(m_site))
			{
				Rectangle rcPrimary;
				bool fEndBeforeAnchor;
				m_site.SelectionRectangle(Selection, out rcPrimary, out fEndBeforeAnchor);
				var point = m_site.PointToScreen(new Point(rcPrimary.X, rcPrimary.Y));
				var screenPoint = new System.Windows.Point(point.X, point.Y);
				var size = new System.Windows.Size(rcPrimary.Width, rcPrimary.Height);
				BoundingRectangle = new System.Windows.Rect(screenPoint, size);
			}
		}

		#region IRawElementProviderFragment Members

		#endregion

		/// <summary>
		/// Gets the next sibling of this fragment.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetNextSibling()
		{
			if (m_parent != null)
				return m_parent.Navigate(this, NavigateDirection.NextSibling);
			return null;
		}

		/// <summary>
		/// Gets the previous sibling.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetPreviousSibling()
		{
			if (m_parent != null)
				return m_parent.Navigate(this, NavigateDirection.PreviousSibling);
			return null;
		}

		#region IRawElementProviderSimple Members

		#endregion


		private void InstallTextRangeSelection()
		{
			// create a new selection based off our Editable range and install it.
			var sh = SelectionHelper.Create(Selection, m_site);
			sh.SetSelection(m_site, true, true);
		}

		/// <summary>
		/// Sets the focus to this element.
		/// NOTE: also installs the range selection.
		/// </summary>
		public override void SetFocus()
		{
			m_site.Invoke(() =>
				{
					if (m_site.FindForm() == Form.ActiveForm)
						m_site.Focus();
					if (Selection.SelType == VwSelType.kstText)
					{
						InstallTextRangeSelection();
					}
					else if (Selection.SelType == VwSelType.kstPicture)
					{
						var sh = SelectionHelper.Create(Selection, m_site);
						m_site.RootBox.MakeSelInObj(0, sh.LevelInfo.Length, sh.LevelInfo, sh.TextPropId, true);
					}
				});
		}
	}

	/// <summary>
	/// Wraps a given selection so we can provide its information as an edit control
	/// </summary>
	public class SimpleRootSiteEditControl : VwSelectionBasedControl<SimpleRootSiteEditControl>, ITextProvider, IValueProvider
	{
		readonly IVwRootBox m_rootb;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteEditControl"/> class.
		/// </summary>
		/// <param name="parent">The parent control.</param>
		/// <param name="site">The site.</param>
		/// <param name="editableSelection">The editable selection.</param>
		/// <param name="label">The label.</param>
		public SimpleRootSiteEditControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection editableSelection, string label) : base(parent, site, editableSelection)
		{
			m_rootb = editableSelection.RootBox;
			Label = label;
			DocumentRange = new SimpleRootSiteTextRangeProvider(m_site, Selection);

			// Localized string corresponding to the Edit control type.
			AddStaticProperty(AutomationElementIdentifiers.LocalizedControlTypeProperty.Id, Properties.Resources.ksEdit);
		}

		private string Label { get; set; }

		#region IRawElementProviderFragment Members

		bool Focused()
		{
			return m_site.Invoke(() => m_site.Focused);
		}

		#endregion

		#region IRawElementProviderSimple Members

		/// <summary>
		/// Retrieves an object that provides support for a control pattern on a UI Automation element.
		/// </summary>
		/// <param name="patternId">Identifier of the pattern.</param>
		/// <returns>
		/// Object that implements the pattern interface, or null if the pattern is not supported.
		/// </returns>
		public override object GetPatternProvider(int patternId)
		{
			if (patternId == TextPatternIdentifiers.Pattern.Id)
			{
				return this;
			}
			if (patternId == ValuePatternIdentifiers.Pattern.Id)
			{
				return this;
			}
			return null;
		}

		/// <summary>
		/// Retrieves the value of a property supported by the UI Automation provider.
		/// </summary>
		/// <param name="propertyId">The property identifier.</param>
		/// <returns>
		/// The property value, or a null if the property is not supported by this provider, or <see cref="F:System.Windows.Automation.AutomationElementIdentifiers.NotSupported"/> if it is not supported at all.
		/// </returns>
		public override object GetPropertyValue(int propertyId)
		{
			if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
			{
				return null; // for some reason ControlType.Edit crashes.
				//return ControlType.Text;
				//return ControlType.Edit;
			}
			if (propertyId == AutomationElementIdentifiers.NameProperty.Id)
			{
				// The name of the edit control is typically generated from a static text label.
				// If there is not a static text label, a property value for Name must be assigned by the application developer.
				// The Name property should never contain the textual contents of the edit control.
				return Label.Length > 0 ? Label : "";
			}
			if (propertyId == AutomationElementIdentifiers.IsEnabledProperty.Id)
			{
				return m_site.Invoke(() => m_site.Enabled) && m_site.Invoke(() => Selection.IsValid);
			}
			if (propertyId == AutomationElementIdentifiers.IsContentElementProperty.Id)
			{
				return true;
			}
			if (propertyId == AutomationElementIdentifiers.IsControlElementProperty.Id)
			{
				return true;
			}
			return base.GetPropertyValue(propertyId);
		}

		#endregion

		#region ITextProvider Members

		/// <summary>
		/// Gets a text range that encloses the main text of a document.
		/// </summary>
		/// <value></value>
		public ITextRangeProvider DocumentRange
		{
			get;
			private set;
		}

		/// <summary>
		/// Retrieves a collection of disjoint text ranges associated with the current text selection or selections.
		/// </summary>
		/// <returns>A collection of disjoint text ranges.</returns>
		/// <exception cref="T:System.InvalidOperationException">
		/// If the UI Automation provider does not support text selection.
		/// </exception>
		public ITextRangeProvider[] GetSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves an array of disjoint text ranges from a text container where each text range begins with the first partially visible line through to the end of the last partially visible line.
		/// </summary>
		/// <returns>
		/// The collection of visible text ranges within the container or an empty array. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		public ITextRangeProvider[] GetVisibleRanges()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves a text range enclosing a child element such as an image, hyperlink, or other embedded object.
		/// </summary>
		/// <param name="childElement">The enclosed object.</param>
		/// <returns>A range that spans the child element.</returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the child element is a null reference (Nothing in Microsoft Visual Basic .NET).
		/// </exception>
		public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the degenerate (empty) text range nearest to the specified screen coordinates.
		/// </summary>
		/// <param name="screenLocation">The location in screen coordinates.</param>
		/// <returns>
		/// A degenerate range nearest the specified location. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If a given point is outside the UI Automation element associated with the text pattern.
		/// </exception>
		public ITextRangeProvider RangeFromPoint(System.Windows.Point screenLocation)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a value that specifies whether a text provider supports selection and, if so, the type of selection supported.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of None, Single, or Multiple from <see cref="T:System.Windows.Automation.SupportedTextSelection"/>.
		/// </returns>
		public SupportedTextSelection SupportedTextSelection
		{
			get { throw new NotImplementedException(); }
		}

		#endregion


		#region IValueProvider Members

		/// <summary>
		/// Gets a value that specifies whether the value of a control is read-only.
		/// </summary>
		/// <value></value>
		/// <returns>true if the value is read-only; false if it can be modified.
		/// </returns>
		public bool IsReadOnly
		{
			get { return !m_site.EditingHelper.Editable; }
		}

		/// <summary>
		/// Sets the value of a control.
		/// Note: write operations need to occur in the context of a UndoableUnitOfWorkHelper (for Undo/Redo purposes).
		/// This requires fdoCache...so perhaps an edit control needs to be in RootSite.dll rather than SimpleRootSite.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="T:System.InvalidOperationException">
		/// If locale-specific information is passed to a control in an incorrect format such as an incorrectly formatted date.
		/// </exception>
		/// <exception cref="T:System.ArgumentException">
		/// If a new value cannot be converted from a string to a format the control recognizes.
		/// </exception>
		/// <exception cref="T:System.Windows.Automation.ElementNotEnabledException">
		/// When an attempt is made to manipulate a control that is not enabled.
		/// </exception>
		public void SetValue(string value)
		{
			// install the range selection for this text box
			// past a string over the selection
			if (!Focused())
				SetFocus(); //installs range selection.
			// get the writing system at the anchor
			int ws = SelectionHelper.GetFirstWsOfSelection(m_rootb.Selection);
			ITsString tss = TsStringUtils.MakeTss(value, ws);
			m_site.Invoke(() => m_site.EditingHelper.PasteCore(tss));
			// NOTE: PasteCore leaves the rootbox selection in a cursor state
			// at the end of the pasted seleciton. So
			// now we need to readjust the Selection with the new end offset.
			var shEditRange = SelectionHelper.Create(Selection, m_site);
			shEditRange.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
			shEditRange.SetIch(SelectionHelper.SelLimitType.End, tss.Length);
			Selection = shEditRange.SetSelection(m_site, false, false);
			ComputeScreenBoundingRectangle();
		}

		/// <summary>
		/// Gets the value of the control.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The value of the control as a string.
		/// </returns>
		public string Value
		{
			get { return DocumentRange.GetText(-1); }
		}

		#endregion
	}

	#endregion

	#region SimpleRootSiteTextRangeProvider

	/// <summary>
	/// Provides the text for the given selection (or if no selection is specified, the entire rootbox (Select-All))
	/// </summary>
	public class SimpleRootSiteTextRangeProvider : ITextRangeProvider
	{

		private readonly SimpleRootSite m_site; // the control hosting this provider
		private readonly IVwRootBox m_rootb;
		private IVwSelection m_vwTextRange;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteTextRangeProvider"/> class.
		/// </summary>
		/// <param name="site">the root site.(by default provide entire text).</param>
		public SimpleRootSiteTextRangeProvider(SimpleRootSite site)
			: this(site, null)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRootSiteTextRangeProvider"/> class.
		/// </summary>
		/// <param name="textRange">The selection from which to extract the text</param>
		/// <param name="site">The site in which the textRange selection has been made.</param>
		public SimpleRootSiteTextRangeProvider(SimpleRootSite site, IVwSelection textRange)
		{
			m_site = site;
			m_rootb = site.RootBox;
			m_vwTextRange = textRange;
		}

		private IVwSelection TextRange
		{
			get
			{
				if (m_vwTextRange == null)
				{
					// simulate Control-A (Select All) but don't install the selection.
					if (m_vwTextRange == null)
					{
						IVwSelection selDocument = MakeTextSelectionEntireText(false);
						ValidateTextRange(selDocument);
						m_vwTextRange = selDocument;
					}
				}
				return m_vwTextRange;
			}

			//set
			//{
			//    ValidateTextRange(value);
			//    m_vwTextRange = value;
			//}
		}

		private static void ValidateTextRange(IVwSelection value)
		{
			if (value == null || !value.IsRange || value.SelType == VwSelType.kstPicture)
				throw new ArgumentException("TextRange must be set to valid range of a text.");
		}

		#region ITextRangeProvider Members

		/// <summary>
		/// Adds to the collection of highlighted text in a text container that supports multiple, disjoint selections.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// If text provider does not support multiple, disjoint selections (that is, <see cref="P:System.Windows.Automation.Provider.ITextProvider.SupportedTextSelection"/> must have a value of Multiple).
		/// </exception>
		public void AddToSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a new <see cref="T:System.Windows.Automation.Provider.ITextRangeProvider"/> identical to the original <see cref="T:System.Windows.Automation.Provider.ITextRangeProvider"/> and inheriting all properties of the original.
		/// </summary>
		/// <returns>
		/// The new text range. A null reference (Nothing in Microsoft Visual Basic .NET) is never returned.
		/// </returns>
		public ITextRangeProvider Clone()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a value that indicates whether the span (the <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> endpoint to the <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint) of a text range is the same as another text range.
		/// </summary>
		/// <param name="range">A text range to compare</param>
		/// <returns>
		/// true if the span of both text ranges is identical; otherwise false.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the range being compared does not come from the same text provider.
		/// </exception>
		public bool Compare(ITextRangeProvider range)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a value that specifies whether two text ranges have identical endpoints.
		/// </summary>
		/// <param name="endpoint">The <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> or <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint of the caller.</param>
		/// <param name="targetRange">The target range for comparison.</param>
		/// <param name="targetEndpoint">The <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> or <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoint of the target.</param>
		/// <returns>
		/// Returns a negative value if the caller's endpoint occurs earlier in the text than the target endpoint.
		/// Returns zero if the caller's endpoint is at the same location as the target endpoint.
		/// Returns a positive value if the caller's endpoint occurs later in the text than the target endpoint.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If <paramref name="targetRange"/> is from a different text provider.
		/// </exception>
		public int CompareEndpoints(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Expands the text range to the specified text unit.
		/// </summary>
		/// <param name="unit">The textual unit.</param>
		public void ExpandToEnclosingUnit(System.Windows.Automation.Text.TextUnit unit)
		{
			//SelectionHelper helper = SelectionHelper.Create(this);
			//if (helper != null && helper.Selection != null)
			//{
			//    helper.Selection.ExtendToStringBoundaries();
			//    //EditingHelper.SetKeyboardForSelection(helper.Selection);
			//}
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a text range subset that has the specified attribute value.
		/// </summary>
		/// <param name="attribute">The attribute to search for.</param>
		/// <param name="value">The attribute value to search for. This value must match the type specified for the attribute.</param>
		/// <param name="backward">true if the last occurring text range should be returned instead of the first; otherwise false.</param>
		/// <returns>
		/// A text range having a matching attribute and attribute value; otherwise null (Nothing in Microsoft Visual Basic .NET).
		/// </returns>
		public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a text range subset that contains the specified text.
		/// </summary>
		/// <param name="text">The text string to search for.</param>
		/// <param name="backward">true if the last occurring text range should be returned instead of the first; otherwise false.</param>
		/// <param name="ignoreCase">true if case should be ignored; otherwise false.</param>
		/// <returns>
		/// A text range matching the specified text; otherwise null (Nothing in Microsoft Visual Basic .NET).
		/// </returns>
		public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves the value of the specified attribute across the text range.
		/// </summary>
		/// <param name="attribute">The text attribute.</param>
		/// <returns>
		/// Retrieves an object representing the value of the specified attribute. For example, GetAttributeValue(TextPattern.FontNameAttribute) would return a string that represents the font name of the text range while GetAttributeValue(TextPattern.IsItalicAttribute) would return a value of type <see cref="T:System.Boolean"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// If the specified attribute is not valid.
		/// </exception>
		public object GetAttributeValue(int attribute)
		{
			if (attribute == TextPatternIdentifiers.TextFlowDirectionsAttribute.Id)
			{
				// todo: base the text flow off the initial ws of the text?
				//return FlowDirections.Default;
				return null;
			}
			if (attribute == TextPatternIdentifiers.IsReadOnlyAttribute.Id)
			{
				if (TextRange != null)
					return m_site.Invoke(() => TextRange.IsValid && !TextRange.IsEditable);
				return null;
			}
			return null;
		}

		/// <summary>
		/// Get the rectangles for each visible line of text on the screen.
		/// </summary>
		/// <returns></returns>
		public double[] GetBoundingRectangles()
		{
			return new double[0];
		}

		/// <summary>
		/// Retrieves a collection of all embedded objects that fall within the text range.
		/// </summary>
		/// <returns>
		/// A collection of child objects that fall within the range. Children that overlap with the text range but are not entirely enclosed by it will also be included in the collection.
		/// Returns an empty collection if there are no child objects.
		/// </returns>
		public IRawElementProviderSimple[] GetChildren()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the innermost control that encloses the text range.
		/// </summary>
		/// <returns>
		/// The enclosing control, typically the text provider that supplies the text range. However, if the text provider supports child elements such as tables or hyperlinks, then the enclosing element could be a descendant of the text provider.
		/// </returns>
		public IRawElementProviderSimple GetEnclosingElement()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Retrieves the plain text of the range.
		/// </summary>
		/// <param name="maxLength">The maximum length of the string to return. Use -1 if no limit is required.</param>
		/// <returns>
		/// The plain text of the text range, possibly truncated at the specified <paramref name="maxLength"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// If <paramref name="maxLength"/> is less than -1.
		/// </exception>
		public string GetText(int maxLength)
		{
			ITsString tss = null;
			if (!m_site.Invoke(() => TextRange.IsValid))
				return string.Empty;
			try
			{
				m_site.Invoke(() => TextRange.GetSelectionString(out tss, "; "));
			}
			catch (System.Runtime.InteropServices.COMException e)
			{
				// Writing system is required for every run in a TsString (except for newlines)
			}
			if (tss != null && tss.Text != null)
			{
				string text;
				if (maxLength == -1)
					text = tss.Text;
				else
					text = tss.Text.Substring(0, maxLength);
				return text.TrimEnd('\r', '\n');
			}
			return string.Empty;
		}

		/// <summary>
		/// Makes the text selection from the entire text (editable and noneditable).
		/// </summary>
		/// <param name="fInstall">if set to <c>true</c> install the selection.</param>
		/// <returns></returns>
		IVwSelection MakeTextSelectionEntireText(bool fInstall)
		{
			// simulate Control-A (Select All) but don't install the selection.
			return SelectAll(m_rootb, false, false);
		}

		/// <summary>
		/// Selects the contents of the whole rootbox.
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="fEditable">if set to <c>true</c> tries to start and end the selection in an editable field.</param>
		/// <param name="fInstall">if set to <c>true</c> installs the selection.</param>
		/// <returns></returns>
		internal static IVwSelection SelectAll(IVwRootBox rootb, bool fEditable, bool fInstall)
		{
			IVwSelection selDocument = null;
			IVwSelection selStart = rootb.MakeSimpleSel(true, fEditable, false, false);
			IVwSelection selEnd = rootb.MakeSimpleSel(false, fEditable, false, false);
			if (selStart != null && selEnd != null)
				selDocument = rootb.MakeRangeSelection(selStart, selEnd, fInstall);
			return selDocument;
		}

		/// <summary>
		/// Moves the text range the specified number of text units.
		/// </summary>
		/// <param name="unit">The text unit boundary.</param>
		/// <param name="count">The number of text units to move.
		/// A positive value moves the text range forward, a negative value moves the text range backward, and 0 has no effect.</param>
		/// <returns>
		/// The number of units actually moved. This can be less than the number requested if either of the new text range endpoints is greater than or less than the <see cref="P:System.Windows.Automation.Provider.ITextProvider.DocumentRange"/> endpoints.
		/// </returns>
		public int Move(System.Windows.Automation.Text.TextUnit unit, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Moves one endpoint of a text range to the specified endpoint of a second text range.
		/// </summary>
		/// <param name="endpoint">The endpoint to move.</param>
		/// <param name="targetRange">Another range from the same text provider.</param>
		/// <param name="targetEndpoint">An endpoint on the other range.</param>
		public void MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, System.Windows.Automation.Text.TextPatternRangeEndpoint targetEndpoint)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Moves one endpoint of the text range the specified number of text units within the document range.
		/// </summary>
		/// <param name="endpoint">The endpoint to move.</param>
		/// <param name="unit">The textual unit for moving.</param>
		/// <param name="count">The number of units to move. A positive value moves the endpoint forward. A negative value moves backward. A value of 0 has no effect.</param>
		/// <returns>
		/// The number of units actually moved, which can be less than the number requested if moving the endpoint runs into the beginning or end of the document.
		/// </returns>
		public int MoveEndpointByUnit(System.Windows.Automation.Text.TextPatternRangeEndpoint endpoint, System.Windows.Automation.Text.TextUnit unit, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a highlighted section of text, corresponding to the caller's <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> and <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoints, from the collection of highlighted text in a text container that supports multiple, disjoint selections.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// If text provider does not support multiple, disjoint selections (for example, (see <c>System.Windows.Automation.TextPattern.SupportedTextSelection</c>) must have a value of Multiple).
		/// </exception>
		public void RemoveFromSelection()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Causes the text control to scroll vertically until the text range is visible in the viewport.
		/// </summary>
		/// <param name="alignToTop">true if the text control should be scrolled so the text range is flush with the top of the viewport; false if it should be flush with the bottom of the viewport.</param>
		public void ScrollIntoView(bool alignToTop)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Highlights text in the text control corresponding to the text range <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.Start"/> and <see cref="F:System.Windows.Automation.Text.TextPatternRangeEndpoint.End"/> endpoints.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">
		/// Occurs when text selection is not supported by the text control.
		/// </exception>
		public void Select()
		{
			throw new NotImplementedException();
		}

		#endregion

	}
	#endregion
}
