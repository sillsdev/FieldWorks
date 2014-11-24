using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class implements the DotNet Acessibility interface by wrapping the COM IAccessible interface
	/// implemented by the root box.
	/// It is uncomfortably similar to COMInterfaces.AccessibleObjectFromIAccessible, but was developed
	/// independently since we (JohnT and Dan) didn't find AccessibleObjectFromIAccessible until later.
	/// The most important difference is that this version stores the root site as well as its
	/// AccessibleRootObject. This handles the problem that DotNet apparently only asks any one object
	/// to CreateAccessibilityInstance() once; then it caches the result. So if we return null,
	/// or make an AccessibleObjectFromIAccessible that has a null IAccessible, it will never work later
	/// when the root box has been made and the IAccessible is available. (In fact, it crashes, because
	/// AccessibleObjectFromIAccessible is not coded to handle a null IAccessible.)
	/// This class, whenever it needs the IAccessble, checks whether it is null. If so, it checks the
	/// root site to see whether it is now possible to get a proper AccessibleRootObject, and if so starts
	/// using it. If not, it answers something safe and neutral.
	///
	/// It's possible that we could refactor to share some code between the two classes, for example,
	/// give AccessibleObjectFromIAccessible a virtual method to get the IAccessible (and always use it),
	/// then this class might only need to override that method to try getting the IAcessible from the
	/// rootsite if it doesn't already have one. However, we'd have to merge into AccessibleObjectFromIAccessible
	/// all the code from here that handles IAccessible being null.
	///
	/// Another possible way to remove the duplication is just to delete AccessibleObjectFromIAccessible,
	/// since it doesn't seem to be used and was possibly just an unsuccessful attempt at what this class
	/// seems to do successfully.
	/// </summary>
	class AccessibilityWrapper : Control.ControlAccessibleObject
	{
		Accessibility.IAccessible m_comAccessible;

		SimpleRootSite m_rootSite; // If this is for the root, this is the SimpleRootSite; otherwise, null.
		string m_tempName = "";

		/// <summary>
		/// Get the IAccessible that we wrap. If we don't have one yet, see if it's possible to get it
		/// from the rootsite. If so, tell it any accesibleName that has been set on this.
		/// </summary>
		Accessibility.IAccessible AccessibleImpl
		{
			get
			{
				if (m_comAccessible == null)
				{
					m_comAccessible = m_rootSite.AccessibleRootObject as Accessibility.IAccessible;
					if (m_comAccessible != null)
						m_comAccessible.set_accName(null, m_tempName);
				}
				return m_comAccessible;
			}
		}

		/// <summary>
		/// Make one. The root site is used in case comAccessible is null, to keep trying for a valid
		/// one later when the root box has been created.
		/// </summary>
		/// <param name="rootSite"></param>
		/// <param name="comAccessible"></param>
		public AccessibilityWrapper(SimpleRootSite rootSite, Accessibility.IAccessible comAccessible) : base(rootSite)
		{
			Debug.Assert(rootSite != null);
			m_rootSite = rootSite;
			m_comAccessible = comAccessible;
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		/// <returns></returns>
		public override int GetChildCount()
		{
			if (AccessibleImpl == null)
				return 0;
			return AccessibleImpl.accChildCount;
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleObject GetChild(int index)
		{
			if (AccessibleImpl == null)
				return null;
			object child = AccessibleImpl.get_accChild(index);
			if (child is Accessibility.IAccessible)
				return MakeRelatedWrapper(child as Accessibility.IAccessible);
			return null; // Enhance: could be an 'element' but I don't think we do this.
		}

		/// <summary>
		/// Used wherever we need a C# AccessibleObject for some child. Note that this is NOT
		/// a wrapper for the whole contents of the rootsite, so we do NOT want to make one
		/// with its IAccessible null, because that would turn around and retrieve the IAccessible
		/// for the whole rootsite. I think we could make one with rootsite null, except that we
		/// have an assertion to prevent that! So just return null if we don't actually have a
		/// related IAccessible to wrap.
		/// </summary>
		/// <param name="iAccessible"></param>
		/// <returns></returns>
		private AccessibleObject MakeRelatedWrapper(Accessibility.IAccessible iAccessible)
		{
			if (iAccessible == null)
				return null;
			return new AccessibilityWrapper(m_rootSite, iAccessible);
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override System.Drawing.Rectangle Bounds
		{
			get
			{
				if (AccessibleImpl == null)
					return new System.Drawing.Rectangle(0, 0, 0, 0);
				int xLeft, yTop, dxWidth, dyHeight;
				AccessibleImpl.accLocation(out xLeft, out yTop, out dxWidth, out dyHeight, null);
				return new System.Drawing.Rectangle(xLeft, yTop, dxWidth, dyHeight);
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override string Description
		{
			get
			{
				if (AccessibleImpl == null)
					return "";
				return AccessibleImpl.get_accDescription(null);
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleObject HitTest(int x, int y)
		{
			if (AccessibleImpl == null)
				return null;
			object target = AccessibleImpl.accHitTest(x, y);
			if (target is Accessibility.IAccessible)
				return MakeRelatedWrapper(target as Accessibility.IAccessible);
			if (target is int)
				return this; // Views accessibility object only makes int for CHILDID_SELF
			return null;
		}

		// No point in wrapping DefaultAction, DoDefaultAction, GetFocused, GetSelection, KeyboardShortcut, HelpTopic, Select
		// since they do noting interesting in the Views implementation of IAccessible.


		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override string Name
		{
			get
			{
				if (AccessibleImpl == null)
					return m_tempName;
				string result = AccessibleImpl.get_accName(null);
				if (result == null)
					return "";
				return result;
			}
			set
			{
				if (AccessibleImpl == null)
					m_tempName = value;
				else
					AccessibleImpl.set_accName(null, value);
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "m_rootSite.Parent returns a reference")]
		public override AccessibleObject Parent
		{
			get
			{
				if (AccessibleImpl == null)
					return null;
				object parent = AccessibleImpl.accParent;
				if (parent is Accessibility.IAccessible)
					return MakeRelatedWrapper(parent as Accessibility.IAccessible);
				if (m_rootSite == null)
					return null; // Should not happen, child boxes have parent box.
				if (m_rootSite.Parent == null)
					return null;
				return m_rootSite.Parent.AccessibilityObject;
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleRole Role
		{
			get
			{
				if (AccessibleImpl == null)
					return AccessibleRole.None;
				return (AccessibleRole)(AccessibleImpl.get_accRole(null));
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleStates State
		{
			get
			{
				if (AccessibleImpl == null)
					return AccessibleStates.None;
				return (AccessibleStates)(AccessibleImpl.get_accState(null));
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override string Value
		{
			get
			{
				if (AccessibleImpl == null)
					return "";
				return AccessibleImpl.get_accValue(null);
			}
			set
			{
				Debug.Fail("AccessibiltyWrapper does not support setting Value");
			}
		}


		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// Not directly tested so far...this will work provided the navdir enum uses the same values as the old C++ defines
		/// for NAVDIR_DOWN etc.
		/// /// </summary>
		public override AccessibleObject Navigate(AccessibleNavigation navdir)
		{
			if (AccessibleImpl == null)
				return null;
			object target = AccessibleImpl.accNavigate((int)navdir, null);
			if (target is Accessibility.IAccessible)
				return MakeRelatedWrapper(target as Accessibility.IAccessible);
			return null;
		}

	}
}
