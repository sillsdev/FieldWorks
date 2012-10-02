using System;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;

// using System.ComponentModel;
// using System.Threading;
// using System.Windows.Forms;

namespace FwUiAutomationProviders
{
	/// <summary>
	/// Exposes methods and properties to support UI Automation client access
	/// to controls that act as containers for a collection of child elements.
	/// The children of this element must implement IGridItemProvider and be
	/// organized in a two-dimensional logical coordinate system that can be
	/// traversed (that is, a UI Automation client can move to adjacent controls)
	/// by using the keyboard.
	/// </summary>
	class RootSiteProvider : IRawElementProviderSimple, IGridProvider
	{
		[DllImport("user32.dll")]
		static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

		[DllImport("user32.dll")]
		static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		[DllImport("user32.dll", SetLastError = true)]
		static extern Rectangle GetWindowRect(IntPtr hWnd, Rectangle lpRect);

		[DllImport("user32.dll")]
		static extern IntPtr GetFocus();

		private IntPtr m_providerHwnd; // hwnd of this RootSite control
		private IVwRootBox m_ivb;      // if this is a RootSite, this is a proxy to it

		/// <summary>
		/// Create a RootSiteProvider based on it's window handle.
		/// </summary>
		/// <param name="hwnd">The RootSite window handle</param>
		public RootSiteProvider(IntPtr hwnd)
		{
			m_providerHwnd = hwnd;
			AccessibleObject ao = new AccessibleObject(hwnd);
			m_ivb = ao.getRootBox();
		}

		/// <summary>
		/// Provides methods and properties that expose basic information
		/// about a UI element.
		/// </summary>
		/// <param name="hwnd">The window for which the provider is created.</param>
		/// <param name="idChild">The child ID of the object.</param>
		/// <param name="idObject">The ID of the object.</param>
		/// <returns></returns>
		internal static IRawElementProviderSimple Create(
			IntPtr hwnd, int idChild, int idObject)
		{
			// Expose children?
			// What is the idObject all about?
			return new RootSiteProvider(hwnd);
		}

		/// <summary>
		/// Exposes methods and properties on user interface (UI) elements that
		/// are part of a structure more than one level deep, such as a list box
		/// or a list item. Implemented by UI Automation providers.
		/// </summary>
		/// <param name="hwnd">The window for which the provider is created.</param>
		/// <param name="idChild">The child ID of the object.</param>
		/// <returns></returns>
		private static IRawElementProviderSimple Create(
			IntPtr hwnd, int idChild)
		{
			// Expose children?
			return new RootSiteProvider(hwnd);
		}

		/// <summary>
		/// Retrieves an object that provides support for a control pattern
		/// on a UI Automation element. (Inherited from IRawElementProviderSimple.)
		/// </summary>
		/// <param name="patternId">Identifier of the pattern.</param>
		/// <returns>Object that implements the pattern interface,
		/// or null if the pattern is not supported.</returns>
		public Object GetPatternProvider(int patternId)
		{   // see method doc for non-null example
			if (patternId == GridPatternIdentifiers.Pattern.Id) return this;
			else return null;
		}

		/// <summary>
		/// Retrieves the value of a property supported by the UI Automation provider.
		/// (Inherited from IRawElementProviderSimple.)
		/// A provider should return NotSupported only if it is explicitly hiding the
		/// property value and the request is not to be passed through to other
		/// providers.
		/// </summary>
		/// <param name="propertyId">The property identifier.</param>
		/// <returns>The property value, or a null if the property is not supported
		/// by this provider, or NotSupported if it is not supported at all.</returns>
		public Object GetPropertyValue(int propertyId)
		{	// see method doc for non-null example
			if (propertyId == AutomationElementIdentifiers.NameProperty.Id
				|| propertyId == AutomationElementIdentifiers.AutomationIdProperty.Id
				|| propertyId == AutomationElementIdentifiers.ClassNameProperty.Id)
				return "RootSite";
			else if (propertyId == AutomationElementIdentifiers.BoundingRectangleProperty.Id)
			{	// use hwnd to get bounding rectangle
				Rectangle tempRect = new Rectangle();
				return GetWindowRect(m_providerHwnd, tempRect);
			}
			else if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
				return System.Windows.Automation.ControlType.DataGrid.Id;
			else if (propertyId == AutomationElementIdentifiers.FrameworkIdProperty.Id)
				return "FDO";
			else if (propertyId == AutomationElementIdentifiers.HasKeyboardFocusProperty.Id)
			{	// use hwnd to determine if this RootSite has focus
				// Can it ever have it, or does it have it if a child has it?
				IntPtr controlHwnd = GetFocus();
				return controlHwnd == m_providerHwnd;
			}
			else if (propertyId == AutomationElementIdentifiers.IsContentElementProperty.Id)
			{	// use hwnd to determine if this RootSite has content (normally true)
				uint GW_CHILD = 5; // from C:\Program Files\PlatformSDK\Include\WinUser.h
				IntPtr childHwnd = GetWindow(m_providerHwnd, GW_CHILD);
				return childHwnd != null;
			}
			else if (propertyId == AutomationElementIdentifiers.IsControlElementProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsEnabledProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsGridPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsKeyboardFocusableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsRangeValuePatternAvailableProperty.Id
					)
				return true;
			else if (propertyId == AutomationElementIdentifiers.IsDockPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsExpandCollapsePatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsGridItemPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsInvokePatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsMultipleViewPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsPasswordProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsRequiredForFormProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsScrollItemPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsScrollPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsSelectionItemPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsSelectionPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsTableItemPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsTablePatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsTextPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsTogglePatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsTransformPatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsValuePatternAvailableProperty.Id
					 || propertyId == AutomationElementIdentifiers.IsWindowPatternAvailableProperty.Id
					)
				return false;
			else if (propertyId == AutomationElementIdentifiers.IsOffscreenProperty.Id)
			{	// use hwnd to determine if this RootSite is off-screen
				/* The value is true if the element is entirely scrolled out of view (for example, an item in
				 * a list box) or collapsed out of view (for example, an item in a tree view or menu, or a
				 * minimized window). If the element has a clickable point that can cause it to be focused,
				 * it is considered to be onscreen.
				 * The value of the property is not affected by occlusion by other windows, or by whether the
				 * element is visible on a specific monitor.
				 * When the value is true for a container, it is also true for the container element's descendants.
				 */
				return IsWindowVisible(m_providerHwnd);
			}
			else if (propertyId == AutomationElementIdentifiers.NativeWindowHandleProperty.Id)
				return m_providerHwnd;
			else if (propertyId == AutomationElementIdentifiers.ProcessIdProperty.Id)
			{	// use hwnd to find the ProcessID likely "flex"
				IntPtr procId;
				IntPtr threadId = GetWindowThreadProcessId(m_providerHwnd, out procId);
				Process proc = Process.GetProcessById((int)procId);
				return proc.ProcessName;
			}
			else if (propertyId == AutomationElementIdentifiers.RuntimeIdProperty.Id)
			{	// use hwnd to find the Runtime Id
				return new int[] { AutomationInteropProvider.AppendRuntimeId, 20 }; // group ID = 20
			}
			else if (propertyId == AutomationElementIdentifiers.StructureChangedEvent.Id)
			{	// use hwnd to listen for events that indicate a row was added/deleted
				return m_ivb.IsDirty(); // (bool) returns true or false
			}
			else if (propertyId == AutomationElementIdentifiers.ClickablePointProperty.Id
					 || propertyId == AutomationElementIdentifiers.CultureProperty.Id
					 || propertyId == AutomationElementIdentifiers.HelpTextProperty.Id
					 || propertyId == AutomationElementIdentifiers.ItemStatusProperty.Id
					 || propertyId == AutomationElementIdentifiers.ItemTypeProperty.Id
					 || propertyId == AutomationElementIdentifiers.LabeledByProperty.Id // Slices have labels
					 || propertyId == AutomationElementIdentifiers.LayoutInvalidatedEvent.Id
					 || propertyId == AutomationElementIdentifiers.LocalizedControlTypeProperty.Id // Yes?
					 || propertyId == AutomationElementIdentifiers.MenuClosedEvent.Id
					 || propertyId == AutomationElementIdentifiers.MenuOpenedEvent.Id
					 || propertyId == AutomationElementIdentifiers.OrientationProperty.Id
					 || propertyId == AutomationElementIdentifiers.ToolTipClosedEvent.Id
					 || propertyId == AutomationElementIdentifiers.ToolTipOpenedEvent.Id
					)
			{	// These do not apply to a RootSite
				return AutomationElementIdentifiers.NotSupported;
			}
			else
				return null;
		}

		/// <summary>
		/// Gets a base provider for this element.
		/// This property is the UI Automation provider for the window of a custom control.
		/// UI Automation uses this provider in combination with your provider
		/// implementation for a control hosted in a window. For example, the run-time
		/// identifier of the element is obtained from the host provider.
		/// A host provider must be returned in any of the following cases:
		/// o This element is the root of a fragment.
		/// o The element is a simple element such as a pushbutton.
		/// o The provider is a repositioning placeholder.
		/// In other cases, the property should return null.
		/// </summary>
		public IRawElementProviderSimple HostRawElementProvider
		{	// this is the root of a fragment? Seems it should be. How to get the Host provider?
			get { return null; }
		}

		/// <summary>
		/// Gets a value that specifies characteristics of the UI Automation provider;
		/// for example, whether it is a client-side or server-side provider.
		/// UI Automation treats different types of providers differently.
		/// For example, events from server-side providers are broadcast to all listening
		/// UI Automation client processes, but events from client-side providers remain
		/// in that client process.
		/// </summary>
		public ProviderOptions ProviderOptions
		{
			get { return ProviderOptions.ClientSideProvider; }
		}

		/// <summary>
		/// Gets the count of rows in the grid.
		/// </summary>
		/// <remarks>
		/// gridItems is a two-dimensional array containing rows in
		/// the first dimension.
		/// </remarks>
		public int RowCount // IGridProvider.
		{	// use hwnd to get the row count
			get
			{
				ISilDataAccess isda = m_ivb.DataAccess;
				// isda.get_IntProp(hvo,tag)
				return 0;
			}
		}

		/// <summary>
		/// Gets the count of columns in the grid.
		/// </summary>
		/// <remarks>
		/// gridItems is a two-dimensional array containing columns
		/// in the second dimension.
		/// </remarks>
		public int ColumnCount // IGridProvider.
		{	// use hwnd to get the column count
			get { return 0; }
		}

		/// <summary>
		/// Returns an object representing the item at the specified location.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public IRawElementProviderSimple GetItem(int row, int column) // IGridProvider.
		{
			// When the requested row coordinate is larger than the RowCount or
			// the column coordinate is larger than the ColumnCount.
			// raise ArgumentOutOfRangeException;
			if (row > RowCount)
				throw new ArgumentOutOfRangeException("row", "row coordinate is larger than the RowCount");
			if (column > ColumnCount)
				throw new ArgumentOutOfRangeException("column", "column coordinate is larger than the ColumnCount");

			// When either of the requested row or column coordinates
			// is less than zero.
			// raise ArgumentOutOfRangeException;
			if (row < 0)
				throw new ArgumentOutOfRangeException("row", "row coordinate is less than zero");
			if (column < 0)
				throw new ArgumentOutOfRangeException("column", "column coordinate is less than zero");

			// return (IRawElementProviderSimple)gridItems[row, column];
			// use hwnd to get the hwnd of the grid item at row, column


			return null;
		}
	}
}
