// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace SIL.FieldWorks.Common.RootSites
{
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
}
