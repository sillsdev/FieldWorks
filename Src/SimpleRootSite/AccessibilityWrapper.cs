// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Accessibility;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class implements the DotNet Accessibility interface by wrapping the COM IAccessible interface
	/// implemented by the root box.
	/// </summary>
	internal class AccessibilityWrapper : Control.ControlAccessibleObject
	{
		private IAccessible m_comAccessible;
		private SimpleRootSite m_rootSite; // If this is for the root, this is the SimpleRootSite; otherwise, null.
		private string m_tempName = string.Empty;

		/// <summary>
		/// Get the IAccessible that we wrap. If we don't have one yet, see if it's possible to get it
		/// from the rootsite. If so, tell it any accessibleName that has been set on this.
		/// </summary>
		IAccessible AccessibleImpl
		{
			get
			{
				if (m_comAccessible == null)
				{
					m_comAccessible = m_rootSite.AccessibleRootObject as IAccessible;
					if (m_comAccessible != null)
					{
						m_comAccessible.set_accName(null, m_tempName);
					}
				}
				return m_comAccessible;
			}
		}

		/// <summary>
		/// Make one. The root site is used in case comAccessible is null, to keep trying for a valid
		/// one later when the root box has been created.
		/// </summary>
		public AccessibilityWrapper(SimpleRootSite rootSite, IAccessible comAccessible) : base(rootSite)
		{
			Debug.Assert(rootSite != null);
			m_rootSite = rootSite;
			m_comAccessible = comAccessible;
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override int GetChildCount()
		{
			if (AccessibleImpl == null)
			{
				return 0;
			}
			return AccessibleImpl.accChildCount;
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleObject GetChild(int index)
		{
			if (AccessibleImpl == null)
			{
				return null;
			}
			object child = AccessibleImpl.get_accChild(index);
			if (child is IAccessible)
			{
				return MakeRelatedWrapper(child as IAccessible);
			}
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
		private AccessibleObject MakeRelatedWrapper(IAccessible iAccessible)
		{
			if (iAccessible == null)
			{
				return null;
			}
			return new AccessibilityWrapper(m_rootSite, iAccessible);
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override Rectangle Bounds
		{
			get
			{
				if (AccessibleImpl == null)
				{
					return new Rectangle(0, 0, 0, 0);
				}
				int xLeft, yTop, dxWidth, dyHeight;
				AccessibleImpl.accLocation(out xLeft, out yTop, out dxWidth, out dyHeight, null);
				return new Rectangle(xLeft, yTop, dxWidth, dyHeight);
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override string Description => AccessibleImpl == null ? string.Empty : AccessibleImpl.get_accDescription(null);

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleObject HitTest(int x, int y)
		{
			if (AccessibleImpl == null)
			{
				return null;
			}
			object target = AccessibleImpl.accHitTest(x, y);
			if (target is IAccessible)
			{
				return MakeRelatedWrapper(target as IAccessible);
			}
			if (target is int)
			{
				return this; // Views accessibility object only makes int for CHILDID_SELF
			}
			return null;
		}

		// No point in wrapping DefaultAction, DoDefaultAction, GetFocused, GetSelection, KeyboardShortcut, HelpTopic, Select
		// since they do nothing interesting in the Views implementation of IAccessible.


		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override string Name
		{
			get
			{
				if (AccessibleImpl == null)
				{
					return m_tempName;
				}
				string result = AccessibleImpl.get_accName(null);
				if (result == null)
				{
					return string.Empty;
				}
				return result;
			}
			set
			{
				if (AccessibleImpl == null)
				{
					m_tempName = value;
				}
				else
				{
					AccessibleImpl.set_accName(null, value);
				}
			}
		}

		/// <summary>
		/// One of many methods that delegate to the IAccessible, with a suitable default if it is null.
		/// </summary>
		public override AccessibleObject Parent
		{
			get
			{
				if (AccessibleImpl == null)
				{
					return null;
				}
				object parent = AccessibleImpl.accParent;
				if (parent is IAccessible)
				{
					return MakeRelatedWrapper(parent as IAccessible);
				}
				if (m_rootSite == null)
				{
					return null; // Should not happen, child boxes have parent box.
				}
				if (m_rootSite.Parent == null)
				{
					return null;
				}
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
				{
					return AccessibleRole.None;
				}
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
				{
					return AccessibleStates.None;
				}
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
				{
					return string.Empty;
				}
				return AccessibleImpl.get_accValue(null);
			}
			set
			{
				throw new NotSupportedException();
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
			{
				return null;
			}
			object target = AccessibleImpl.accNavigate((int)navdir, null);
			if (target is Accessibility.IAccessible)
			{
				return MakeRelatedWrapper(target as IAccessible);
			}
			return null;
		}
	}
}
