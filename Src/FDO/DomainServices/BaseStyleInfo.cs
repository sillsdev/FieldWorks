// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BaseStyleInfo.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region Generic class InheritableStyleProp
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generic class to encapsulate the notion of a style property whose value can be either
	/// explicit or inherited.
	/// </summary>
	/// <typeparam name="T">Type of property</typeparam>
	/// ----------------------------------------------------------------------------------------
	public class InheritableStyleProp<T> : IStyleProp<T>
	{
		#region Data members
		private T m_value;
		private T m_defaultValue;
		private bool m_inherited;
		private bool m_inheritedValueSet;
		private bool m_defaultValueSet;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InheritableStyleProp&lt;T&gt;"/>
		/// class for an inherited property (value not set).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp()
		{
			m_inherited = true;
			m_inheritedValueSet = false;
			m_defaultValueSet = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp(InheritableStyleProp<T> copyFrom)
		{
			m_value = copyFrom.m_value;
			m_inherited = copyFrom.m_inherited;
			m_inheritedValueSet = copyFrom.m_inheritedValueSet;
			m_defaultValue = copyFrom.m_defaultValue;
			m_defaultValueSet = copyFrom.m_defaultValueSet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InheritableStyleProp&lt;T&gt;"/>
		/// class for a non-inherited (explicit) property.
		/// </summary>
		/// <param name="value">The (explict) value of the property.</param>
		/// ------------------------------------------------------------------------------------
		public InheritableStyleProp(T value)
		{
			m_value = value;
			m_inherited = false;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the style property regardless of whether it is inherited or
		/// explicit.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if this is an inherited
		/// property and the inherited value has not been set.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public T Value
		{
			get
			{
				if (!ValueIsSet)
					throw new InvalidOperationException("Inherited style property value cannot be retrieved until it has been set.");
				return m_value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether it is okay to access the <see cref="Value"/>
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ValueIsSet
		{
			get
			{
				return (!m_inherited || m_inheritedValueSet);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this style property is inherited.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is inherited; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsInherited
		{
			get { return m_inherited; }
			set
			{
				if (!m_inherited && value)
				{
					m_inheritedValueSet = m_defaultValueSet;
					if (m_defaultValueSet)
						m_value = m_defaultValue;
				}
				m_inherited = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is explicit (not inherited).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsExplicit
		{
			get { return !IsInherited; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value for an explicit style property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public T ExplicitValue
		{
			set
			{
				m_inherited = false;
				m_value = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default value for this property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public T DefaultValue
		{
			get
			{
				if (!m_defaultValueSet)
					throw new InvalidOperationException("Default style property value cannot be retrieved until it has been set.");
				return m_defaultValue;
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the new value if it has changed
		/// </summary>
		/// <param name="newInherit">true if the new value is inherited</param>
		/// <param name="newValue">The new value (ignored if newInherit is <c>true</c>)</param>
		/// <returns>true if the value was changed, else false</returns>
		/// ------------------------------------------------------------------------------------
		public bool Save(bool newInherit, T newValue)
		{
			// If this is a property with a default value, we have a special case. We ignore the
			// newInherit flag and just compare the new value with the default. If they're the
			// same, then we regard this property as being inherited (i.e., implicit rather than
			// explicit). This allows us to not save a gazillion explicit property values for
			// the "Normal" style.
			if (m_defaultValueSet && AreEqual(newValue, m_defaultValue))
				newInherit = true;

			// If the new value is inherited...
			if (newInherit)
			{
				// if the old value is not inherited, then switch to inherited
				if (!m_inherited)
				{
					IsInherited = true;
					return true;
				}
			}
			else
			{
				// if the value has changed, then save it
				if (!AreEqual(newValue, m_value))
				{
					ExplicitValue = newValue;
					return true;
				}
				else if (m_inherited != newInherit)
				{
					IsInherited = newInherit;
					return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default value for an inherited style property. If this property is
		/// currently inherited, this is the value that will be returned by the
		/// <see cref="Value"/> property.
		/// </summary>
		/// <param name="value">The default value</param>
		/// ------------------------------------------------------------------------------------
		public void SetDefaultValue(T value)
		{
			if (m_inherited)
			{
				m_inheritedValueSet = true;
				m_value = value;
			}
			m_defaultValue = value;
			m_defaultValueSet = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value for an inherited style property based on the corresponding property
		/// of the style from which it inherits. If this property is not inherited, this method
		/// does nothing
		/// </summary>
		/// <remarks>After calling this method, it is safe to access the <see cref="Value"/>
		/// property of this object if the inherited value of the based on property was set.
		/// Note that this might NOT be the case since it is legitimate to connect a chain of
		/// character styles where none of them (including the top-level one) have an inherited
		/// value set since ultimately character styles inherit from the default paragraph
		/// characters of the containing paragraph style, which can only be determined in the
		/// context a specific string.
		/// </remarks>
		/// <param name="basedOnProp">The corresponding property of the style from which this
		/// property's style inherits.</param>
		/// ------------------------------------------------------------------------------------
		public void InheritValue(InheritableStyleProp<T> basedOnProp)
		{
			if (m_inherited)
			{
				m_inheritedValueSet = !basedOnProp.m_inherited || basedOnProp.m_inheritedValueSet;
				m_value = basedOnProp.m_value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value for an inherited style property based on the corresponding property
		/// of the style from which it inherits. This method forces this property to inherit
		/// the value from the specified base property.
		/// </summary>
		/// <param name="basedOnProp">The corresponding property of the style from which this
		/// property's style inherits.</param>
		/// ------------------------------------------------------------------------------------
		public void ResetToInherited(InheritableStyleProp<T> basedOnProp)
		{
			m_inherited = true;
			InheritValue(basedOnProp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turns an explicit style property into an inherited property having the given
		/// (inherited) value.
		/// </summary>
		/// <param name="value">The (inherited) value</param>
		/// ------------------------------------------------------------------------------------
		public void ResetToInherited(T value)
		{
			m_inherited = true;
			m_inheritedValueSet = true;
			m_value = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the value of this
		/// object.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// <remarks>Unlike the <see cref="Value"/> property, calling ToString will NOT throw
		/// an exception if this is an inherited property whose value has not yet been set.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_value.ToString();
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two T values for equality, safe even if one of them is null
		/// </summary>
		/// <param name="a">One of the values</param>
		/// <param name="b">The other value</param>
		/// <returns><c>true</c> if a == b; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool AreEqual(T a, T b)
		{
			if (a == null)
				return (b == null);
			return a.Equals(b);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the
		/// current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current
		/// <see cref="T:System.Object"></see>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current
		/// <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			InheritableStyleProp<T> other = obj as InheritableStyleProp<T>;
			if (other == null)
				return false;

			if (IsExplicit != other.IsExplicit)
				return false;

			return AreEqual(Value, other.Value);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a hash function for a particular type.
		/// <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing
		/// algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
	#endregion

	#region struct BorderThicknesses
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Structure containing the border thicknesses (in millipoints)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct BorderThicknesses
	{
		/// <summary>Thickness of leading border in millipoints</summary>
		public int Leading;
		/// <summary>Thickness of trailing border in millipoints</summary>
		public int Trailing;
		/// <summary>Thickness of top border in millipoints</summary>
		public int Top;
		/// <summary>Thickness of bottom border in millipoints</summary>
		public int Bottom;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BorderInfo"/> class.
		/// </summary>
		/// <param name="leading">The thickness of leading border in millipoints</param>
		/// <param name="trailing">The thickness of trailing border in millipoints</param>
		/// <param name="top">The thickness of top border in millipoints</param>
		/// <param name="bottom">The thickness of bottom border in millipoints</param>
		/// ------------------------------------------------------------------------------------
		public BorderThicknesses(int leading, int trailing, int top, int bottom)
		{
			Leading = leading;
			Trailing = trailing;
			Top = top;
			Bottom = bottom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares to another instance of a BorderThicknesses item
		/// </summary>
		/// <param name="obj">The other item</param>
		/// <returns>true if the data members are the same</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (!(obj is BorderThicknesses))
				return false;

			BorderThicknesses other = (BorderThicknesses)obj;
			return
				Leading == other.Leading &&
				Trailing == other.Trailing &&
				Top == other.Top &&
				Bottom == other.Bottom;
		}
	}
	#endregion

	#region struct LineHeightInfo
	/// <summary>line height information</summary>
	public struct LineHeightInfo
	{
		/// <summary>
		/// line height in millipoints. For explicit values, it is either a positive
		/// value to indicate at least, or negative to indicate an exact millipoint value.
		/// </summary>
		public int m_lineHeight;
		/// <summary>
		/// true for relative values (single, 1.5, double spacing)
		/// </summary>
		public bool m_relative;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LineHeightInfo"/> class.
		/// </summary>
		/// <param name="lineSpacing">The line spacing.</param>
		/// <param name="fLineSpacingRelative">if set to <c>true</c> line spacing will be
		/// interpreted as a relative "magic" value (10000, 15000, or 20000).</param>
		/// ------------------------------------------------------------------------------------
		public LineHeightInfo(int lineSpacing, bool fLineSpacingRelative)
		{
			m_lineHeight = lineSpacing;
			m_relative = fLineSpacingRelative;
			if (m_relative &&
				m_lineHeight != 10000 && m_lineHeight != 15000 && m_lineHeight != 20000)
			{
				throw new ArgumentException("The acceptable lineSpacing values for relative line spacing are 10000, 15000, and 20000.");
			}
		}
	}
	#endregion

	#region IntPropInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information about an integer property
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class IntPropInfo
	{
		#region data members
		/// <summary></summary>
		public int m_textPropType = 0;
		/// <summary></summary>
		public int m_variant = 0;
		/// <summary></summary>
		public int m_value = 0;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IntPropInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IntPropInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IntPropInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IntPropInfo(int textPropType, int value)
			: this(textPropType, value, (int)FwTextPropVar.ktpvDefault)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IntPropInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IntPropInfo(int textPropType, int value, int variant)
		{
			m_textPropType = textPropType;
			m_value = value;
			m_variant = variant;
		}
	}
	#endregion

	#region StringPropInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information about a string property
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StringPropInfo
	{
		/// <summary></summary>
		public short m_textPropType = 0;
		/// <summary></summary>
		public string m_value = null;
	}
	#endregion

	#region FontOverrideInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FontOverrideInfo
	{
		/// <summary></summary>
		public int m_ws;
		/// <summary></summary>
		public string m_fontFamily;
		/// <summary></summary>
		public List<IntPropInfo> m_intProps = new List<IntPropInfo>();
		/// <summary></summary>
		public List<StringPropInfo> m_stringProps = new List<StringPropInfo>();
	}
	#endregion

	#region Direction enumeration
	/// ----------------------------------------------------------------------------------------
	/// <summary>Special "Bool" that can be undefined</summary>
	/// ----------------------------------------------------------------------------------------
	public enum TriStateBool
	{
		/// <summary>undefined, not specified, unset, inherited, irrelevant</summary>
		triNotSet = 0,
		/// <summary>True</summary>
		triTrue = 1,
		/// <summary>False</summary>
		triFalse = 2,
	}
	#endregion

	#region class BaseStyleInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for objects that store information about a style that is part of a
	/// StyleInfoTable
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BaseStyleInfo : IStyle
	{
		#region Member data
		/// <summary>The name of the style (null if not associated with a (hopefully real) FW style)</summary>
		protected string m_name;
		/// <summary>A unique sequence number for this style</summary>
		internal int m_styleNumber;
		/// <summary>The description of the style (i.e., information about how to use it)</summary>
		protected string m_usage;
		/// <summary>Name of style on which this style is based</summary>
		protected internal string m_basedOnStyleName;
		/// <summary>Name of style to use for following line (always null for character styles)</summary>
		protected internal string m_nextStyleName;
		/// <summary>style on which this style is based</summary>
		protected internal BaseStyleInfo m_basedOnStyle;
		/// <summary>style to use for following line (always null for character styles)</summary>
		protected internal BaseStyleInfo m_nextStyle;
		/// <summary>Paragraph or character</summary>
		protected StyleType m_styleType;
		/// <summary>Context</summary>
		protected ContextValues m_context = ContextValues.General;
		/// <summary>Structure</summary>
		protected StructureValues m_structure = StructureValues.Undefined;
		/// <summary>Function</summary>
		protected FunctionValues m_function = FunctionValues.Prose;
		/// <summary>The Right-to-left setting</summary>
		protected internal InheritableStyleProp<TriStateBool> m_rtl = new InheritableStyleProp<TriStateBool>();
		/// <summary>The Keep with next setting</summary>
		protected internal InheritableStyleProp<bool> m_keepWithNext = new InheritableStyleProp<bool>();
		/// <summary>The Keep lines together setting</summary>
		protected internal InheritableStyleProp<bool> m_keepTogether = new InheritableStyleProp<bool>();
		/// <summary>The Widow/orphan control setting</summary>
		protected internal InheritableStyleProp<bool> m_widowOrphanControl = new InheritableStyleProp<bool>();
		/// <summary>The user level setting</summary>
		protected int m_userLevel = 0;
		/// <summary>The actuall style object associated with the BaseStyleInfo. Can be null</summary>
		protected IStStyle m_style;
		/// <summary>Is the style built-in</summary>
		protected bool m_isBuiltIn;
		/// <summary>Is the style modified?</summary>
		protected bool m_isModified = false;
		/// <summary>All style infos are valid by default.</summary>
		protected bool m_fIsValid = true;

		// Font-related properties
		/// <summary>Default font information for this style</summary>
		protected internal FontInfo m_defaultFontInfo = new FontInfo();
		/// <summary>TODO(TE-4624): Any writing system can have its own font information that overrides the defaults.</summary>
		protected internal Dictionary<int, FontInfo> m_fontInfoOverrides = new Dictionary<int,FontInfo>();

		// Paragraph style properties
		/// <summary>Alignment</summary>
		protected internal InheritableStyleProp<FwTextAlign> m_alignment = new InheritableStyleProp<FwTextAlign>();
		/// <summary>Inter-line spacing in millipoints</summary>
		protected internal InheritableStyleProp<LineHeightInfo> m_lineSpacing = new InheritableStyleProp<LineHeightInfo>();
		/// <summary>Space above paragraph in millipoints</summary>
		protected internal InheritableStyleProp<int> m_spaceBefore = new InheritableStyleProp<int>();
		/// <summary>Space below paragraph in millipoints</summary>
		protected internal InheritableStyleProp<int> m_spaceAfter = new InheritableStyleProp<int>();
		/// <summary>Indentation of first line in millipoints</summary>
		protected internal InheritableStyleProp<int> m_firstLineIndent = new InheritableStyleProp<int>();
		/// <summary>Indentation of paragraph from leading edge in millipoints</summary>
		protected internal InheritableStyleProp<int> m_leadingIndent = new InheritableStyleProp<int>();
		/// <summary>Indentation of paragraph from trailing edge in millipoints</summary>
		protected internal InheritableStyleProp<int> m_trailingIndent = new InheritableStyleProp<int>();

		/// <summary>Thickness of borders</summary>
		protected internal InheritableStyleProp<BorderThicknesses> m_border = new InheritableStyleProp<BorderThicknesses>();
		/// <summary>Color of borders (ARGB)</summary>
		protected internal InheritableStyleProp<Color> m_borderColor = new InheritableStyleProp<Color>();

		/// <summary>Bullet information</summary>
		protected internal InheritableStyleProp<BulletInfo> m_bulletInfo = new InheritableStyleProp<BulletInfo>();

		/// <summary>StyleInfoTable in which this entry is a member</summary>
		internal protected StyleInfoTable m_owningTable;

		/// <summary>Contains the explicit and inherited style properties</summary>
		protected ITsTextProps m_textProps;

		/// <summary>The FDO cache used to retrieve the ws and writing system factory.</summary>
		protected FdoCache m_cache;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BaseStyleInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo()
		{
			m_basedOnStyle = null;
			m_nextStyle = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BaseStyleInfo"/> class from a copy.
		/// This constructor is used to make a new copy of a style with a different name.
		/// </summary>
		/// <param name="copyFrom">The copy from.</param>
		/// <param name="newName">The new name.</param>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo(BaseStyleInfo copyFrom, string newName)
		{
			m_name = newName;
			m_usage = copyFrom.m_usage;
			m_basedOnStyleName = copyFrom.m_basedOnStyleName;
			m_nextStyleName = copyFrom.m_nextStyleName;
			m_basedOnStyle = copyFrom.m_basedOnStyle;
			m_nextStyle = copyFrom.m_nextStyle;
			m_styleType = copyFrom.m_styleType;
			m_context = copyFrom.m_context;
			m_structure = copyFrom.m_structure;
			m_function = FunctionValues.Prose;
			m_userLevel = copyFrom.m_userLevel;

			// User-defined paragraph styles must have a based-on style.  See LT-8315.
			if (m_styleType == StyleType.kstParagraph)
			{
				if (m_basedOnStyle == null)
					m_basedOnStyle = copyFrom;
				if (String.IsNullOrEmpty(m_basedOnStyleName))
					m_basedOnStyleName = copyFrom.Name;
			}

			m_isBuiltIn = false;
			m_isModified = false;
			// Don't copy these (TE-5048)!
			//m_style = copyFrom.m_style;
			//m_isBuiltIn = copyFrom.m_isBuiltIn;

			m_defaultFontInfo = new FontInfo(copyFrom.m_defaultFontInfo);
			m_fontInfoOverrides = new Dictionary<int, FontInfo>();
			if (copyFrom.m_fontInfoOverrides != null)
			{
				foreach (int key in copyFrom.m_fontInfoOverrides.Keys)
					m_fontInfoOverrides.Add(key, new FontInfo(copyFrom.m_fontInfoOverrides[key]));
			}

			m_rtl = new InheritableStyleProp<TriStateBool>(copyFrom.m_rtl);
			m_alignment = new InheritableStyleProp<FwTextAlign>(copyFrom.m_alignment);
			m_lineSpacing = new InheritableStyleProp<LineHeightInfo>(copyFrom.m_lineSpacing);
			m_spaceBefore = new InheritableStyleProp<int>(copyFrom.m_spaceBefore);
			m_spaceAfter = new InheritableStyleProp<int>(copyFrom.m_spaceAfter);
			m_firstLineIndent = new InheritableStyleProp<int>(copyFrom.m_firstLineIndent);
			m_leadingIndent = new InheritableStyleProp<int>(copyFrom.m_leadingIndent);
			m_trailingIndent = new InheritableStyleProp<int>(copyFrom.m_trailingIndent);
			m_border = new InheritableStyleProp<BorderThicknesses>(copyFrom.m_border);
			m_borderColor = new InheritableStyleProp<Color>(copyFrom.m_borderColor);
			m_bulletInfo = new InheritableStyleProp<BulletInfo>(copyFrom.m_bulletInfo);
			m_keepWithNext = new InheritableStyleProp<bool>(copyFrom.m_keepWithNext);
			m_keepTogether = new InheritableStyleProp<bool>(copyFrom.m_keepTogether);
			m_widowOrphanControl = new InheritableStyleProp<bool>(copyFrom.m_widowOrphanControl);

			// this instance will have the same owning table - not a copy
			m_owningTable = copyFrom.m_owningTable;

			m_cache = copyFrom.m_cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BaseStyleInfo"/> class based on a FW
		/// style.
		/// </summary>
		/// <param name="style">An StStyle.</param>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo(IStStyle style) : this()
		{
			Debug.Assert(style != null);
			SetPropertiesBasedOnStyle(style);

			m_cache = style.Cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BaseStyleInfo"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <remarks>This constructor is used to create a new style.</remarks>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo(FdoCache cache)
			: this()
		{
			// A new empty style needs font overrides ready to fill in
			CreateFontInfoOverrides(cache);
			m_cache = cache;
		}
		#endregion

		#region public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties of this entry based on the given FW style.
		/// </summary>
		/// <param name="style">An StStyle.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetPropertiesBasedOnStyle(IStStyle style)
		{
			CreateFontInfoOverrides(style.Cache);

			m_style = style;
			m_name = style.Name;
			ITsString desc = style.Usage.RawUserDefaultWritingSystem;
			if (desc != null && desc.Length > 0) // Only overwrite existing description if not null (needed for USFM export)
				m_usage = desc.Text;
			m_styleType = style.Type;
			m_context = (ContextValues)style.Context;
			m_structure = (StructureValues)style.Structure;
			m_function = (FunctionValues)style.Function;
			m_userLevel = style.UserLevel;
			m_isBuiltIn = style.IsBuiltIn;
			m_isModified = style.IsModified;

			// Get the "based on" style name
			if (style.BasedOnRA != null)
				m_basedOnStyleName = style.BasedOnRA.Name;

			// Get the "next" style name
			if (style.NextRA != null)
				m_nextStyleName = style.NextRA.Name;

			// process the rules if they exist
			ITsTextProps styleProps = style.Rules;
			List<FontOverrideInfo> fontOverrides;
			ProcessStyleRules(styleProps, out fontOverrides);

			// If there were any WS font overrides, then create font info entries
			// for each one.
			if (fontOverrides != null)
				MakeFontWsOverrides(fontOverrides);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates FontInfo entries for all of the available writing systems
		/// </summary>
		/// <param name="cache">FDO cache to use to get writing systems</param>
		/// ------------------------------------------------------------------------------------
		private void CreateFontInfoOverrides(FdoCache cache)
		{
			m_fontInfoOverrides.Clear();

			foreach (IWritingSystem ws in cache.ServiceLocator.WritingSystems.AnalysisWritingSystems
				.Concat(cache.ServiceLocator.WritingSystems.VernacularWritingSystems))
			{
				// Create a FontInfo for each available writing system
				m_fontInfoOverrides[ws.Handle] = new FontInfo();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the style rules.
		/// </summary>
		/// <param name="styleProps">The style props.</param>
		/// <param name="fontOverrides">font overrides</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessStyleRules(ITsTextProps styleProps,
			out List<FontOverrideInfo> fontOverrides)
		{
			fontOverrides = null;
			if (styleProps == null)
				return;
			for (int i = 0; i < styleProps.StrPropCount; i++)
			{
				int tpt;
				string sProp = styleProps.GetStrProp(i, out tpt);

				switch (tpt)
				{
					case (int)FwTextPropType.ktptFontFamily:
						m_defaultFontInfo.m_fontName.ExplicitValue = sProp;
						break;

					case (int)FwTextPropType.ktptBulNumTxtBef:
						{
							m_bulletInfo.IsInherited = false;
							BulletInfo info = m_bulletInfo.Value;
							info.m_textBefore = sProp;
							m_bulletInfo.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptBulNumTxtAft:
						{
							m_bulletInfo.IsInherited = false;
							BulletInfo info = m_bulletInfo.Value;
							info.m_textAfter = sProp;
							m_bulletInfo.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptBulNumFontInfo:
						{
							m_bulletInfo.IsInherited = false;
							BulletInfo info = m_bulletInfo.Value;
							info.EncodedFontInfo = sProp;
							m_bulletInfo.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptWsStyle:
						fontOverrides = ProcessWsSpecificOverrides(sProp);
						break;

					default:
						break;
				}
			}

			for (int i = 0; i < styleProps.IntPropCount; i++)
			{
				int nVar, tpt;
				int iProp = styleProps.GetIntProp(i, out tpt, out nVar);

				SetFontIntProp(tpt, m_defaultFontInfo, iProp);

				switch (tpt)
				{
					case (int)FwTextPropType.ktptRightToLeft:
						m_rtl.ExplicitValue = (iProp != 0) ?
							TriStateBool.triTrue : TriStateBool.triFalse;
						break;

					case (int)FwTextPropType.ktptAlign:
						m_alignment.ExplicitValue = (FwTextAlign)iProp;
						break;

					case (int)FwTextPropType.ktptSpaceBefore:
						m_spaceBefore.ExplicitValue = iProp;
						break;

					case (int)FwTextPropType.ktptSpaceAfter:
						m_spaceAfter.ExplicitValue = iProp;
						break;

					case (int)FwTextPropType.ktptFirstIndent:
						m_firstLineIndent.ExplicitValue = iProp;
						break;

					case (int)FwTextPropType.ktptLeadingIndent:
						m_leadingIndent.ExplicitValue = iProp;
						break;

					case (int)FwTextPropType.ktptTrailingIndent:
						m_trailingIndent.ExplicitValue = iProp;
						break;

					case (int)FwTextPropType.ktptLineHeight:
						{
							LineHeightInfo info = new LineHeightInfo();
							info.m_lineHeight = iProp;
							info.m_relative = (nVar == (int)FwTextPropVar.ktpvRelative);
							m_lineSpacing.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptBorderTop:
						{
							m_border.IsInherited = false;
							BorderThicknesses border = m_border.Value;
							border.Top = iProp;
							m_border.ExplicitValue = border;
						}
						break;

					case (int)FwTextPropType.ktptBorderBottom:
						{
							m_border.IsInherited = false;
							BorderThicknesses border = m_border.Value;
							border.Bottom = iProp;
							m_border.ExplicitValue = border;
						}
						break;

					case (int)FwTextPropType.ktptBorderLeading:
						{
							m_border.IsInherited = false;
							BorderThicknesses border = m_border.Value;
							border.Leading = iProp;
							m_border.ExplicitValue = border;
						}
						break;

					case (int)FwTextPropType.ktptBorderTrailing:
						{
							m_border.IsInherited = false;
							BorderThicknesses border = m_border.Value;
							border.Trailing = iProp;
							m_border.ExplicitValue = border;
						}
						break;

					case (int)FwTextPropType.ktptBorderColor:
						m_borderColor.ExplicitValue =
							MaskTransparent(ColorUtil.ConvertBGRtoColor((uint)iProp));
						break;

					case (int)FwTextPropType.ktptBulNumScheme:
						{
							m_bulletInfo.IsInherited = false;
							BulletInfo info = m_bulletInfo.Value;
							info.m_numberScheme = (VwBulNum)iProp;
							m_bulletInfo.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptBulNumStartAt:
						{
							m_bulletInfo.IsInherited = false;
							BulletInfo info = m_bulletInfo.Value;
							info.m_start = iProp;
							m_bulletInfo.ExplicitValue = info;
						}
						break;

					case (int)FwTextPropType.ktptKeepWithNext:
						m_keepWithNext.ExplicitValue = (iProp != 0);
						break;

					case (int)FwTextPropType.ktptKeepTogether:
						m_keepTogether.ExplicitValue = (iProp != 0);
						break;

					case (int)FwTextPropType.ktptWidowOrphanControl:
						m_widowOrphanControl.ExplicitValue = (iProp != 0);
						break;

					default:
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the int props into a font info
		/// </summary>
		/// <param name="tpt">The text prop type</param>
		/// <param name="fontInfo">The font info.</param>
		/// <param name="iProp">The int value of the property</param>
		/// ------------------------------------------------------------------------------------
		private void SetFontIntProp(int tpt, FontInfo fontInfo, int iProp)
		{
			switch (tpt)
			{
				case (int)FwTextPropType.ktptBold:
					fontInfo.m_bold.ExplicitValue = (iProp != 0);
					break;

				case (int)FwTextPropType.ktptItalic:
					fontInfo.m_italic.ExplicitValue = (iProp != 0);
					break;

				case (int)FwTextPropType.ktptSuperscript:
					fontInfo.m_superSub.ExplicitValue = (FwSuperscriptVal)iProp;
					break;

				case (int)FwTextPropType.ktptFontSize:
					fontInfo.m_fontSize.ExplicitValue = iProp;
					break;

				case (int)FwTextPropType.ktptForeColor:
					fontInfo.m_fontColor.ExplicitValue =
						MaskTransparent(ColorUtil.ConvertBGRtoColor((uint)iProp));
					break;

				case (int)FwTextPropType.ktptBackColor:
					fontInfo.m_backColor.ExplicitValue =
						MaskTransparent(ColorUtil.ConvertBGRtoColor((uint)iProp));
					break;

				case (int)FwTextPropType.ktptOffset:
					fontInfo.m_offset.ExplicitValue = iProp;
					break;

				case (int)FwTextPropType.ktptUnderline:
					fontInfo.m_underline.ExplicitValue = (FwUnderlineType)iProp;
					break;

				case (int)FwTextPropType.ktptUnderColor:
					fontInfo.m_underlineColor.ExplicitValue =
						MaskTransparent(ColorUtil.ConvertBGRtoColor((uint)iProp));
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the string props into a font info
		/// </summary>
		/// <param name="tpt">The text prop type</param>
		/// <param name="fontInfo">The font info.</param>
		/// <param name="sValue">The value of the property</param>
		/// ------------------------------------------------------------------------------------
		private void SetFontStringProp(int tpt, FontInfo fontInfo, string sValue)
		{
			switch (tpt)
			{
				case (int)FwTextPropType.ktptFontVariations:
					fontInfo.m_features.ExplicitValue = sValue;
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the ws-specific font overrides based on the font overrides given
		/// </summary>
		/// <param name="fontOverrides">The font overrides.</param>
		/// ------------------------------------------------------------------------------------
		private void MakeFontWsOverrides(List<FontOverrideInfo> fontOverrides)
		{
			foreach (FontOverrideInfo overrideInfo in fontOverrides)
			{
				if (m_fontInfoOverrides.ContainsKey(overrideInfo.m_ws))
				{
					FontInfo fontInfo = m_fontInfoOverrides[overrideInfo.m_ws];
					if (overrideInfo.m_fontFamily != string.Empty)
						fontInfo.m_fontName.ExplicitValue = overrideInfo.m_fontFamily;
					foreach (IntPropInfo intInfo in overrideInfo.m_intProps)
						SetFontIntProp(intInfo.m_textPropType, fontInfo, intInfo.m_value);
					foreach (StringPropInfo stringInfo in overrideInfo.m_stringProps)
						SetFontStringProp(stringInfo.m_textPropType, fontInfo, stringInfo.m_value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the ws specific overrides.
		/// </summary>
		/// <param name="source">The override string to parse.</param>
		/// <returns>a list of font override info objects</returns>
		/// ------------------------------------------------------------------------------------
		protected List<FontOverrideInfo> ProcessWsSpecificOverrides(string source)
		{
			List<FontOverrideInfo> overrides = new List<FontOverrideInfo>();
			using (BinaryReader reader = new BinaryReader(StringUtils.MakeStreamFromString(source)))
			{
				try
				{
					// read until the end of stream
					while (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						FontOverrideInfo overrideInfo = ReadOneFontOverride(reader);
						overrides.Add(overrideInfo);
					}
				}
				catch (EndOfStreamException)
				{
				}
				finally
				{
					reader.Close();
				}
			}
			return overrides;
		}

		/// <summary>
		/// </summary>
		/// <param name="reader"></param>
		public static FontOverrideInfo ReadOneFontOverride(BinaryReader reader)
		{
			FontOverrideInfo overrideInfo = new FontOverrideInfo();
			overrideInfo.m_ws = reader.ReadInt32();
			short ffLength = reader.ReadInt16();
			overrideInfo.m_fontFamily = StringUtils.ReadString(reader, ffLength);
			short intPropCount = reader.ReadInt16();
			int strPropCount = 0;

			// If the int prop count is negative, then it is really a string
			// prop count. If so, then read the string props.
			if (intPropCount < 0)
			{
				strPropCount = -intPropCount;
				for (int i = 0; i < strPropCount; i++)
				{
					StringPropInfo info = new StringPropInfo();
					info.m_textPropType = reader.ReadInt16();
					int length = reader.ReadInt16();
					info.m_value = StringUtils.ReadString(reader, length);
					overrideInfo.m_stringProps.Add(info);
				}
				// Need to read in the real intPropCount now
				intPropCount = reader.ReadInt16();
			}

			// Read the int props
			for (int i = 0; i < intPropCount; i++)
			{
				IntPropInfo info = new IntPropInfo();
				info.m_textPropType = reader.ReadInt16();
				info.m_variant = reader.ReadInt16();
				info.m_value = reader.ReadInt32();
				overrideInfo.m_intProps.Add(info);
			}
			return overrideInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Masks the transparent color (Makes it Color.Empty).
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>the color</returns>
		/// ------------------------------------------------------------------------------------
		private Color MaskTransparent(Color color)
		{
			if (color == Color.Transparent)
				color = Color.Empty;
			return color;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font info for the given writing system. All public access to font info
		/// shopuld be through this method.
		/// </summary>
		/// <param name="ws">The writing system, or -1 for default font info</param>
		/// <returns>The font information, which may be either the default info for the style
		/// or an override that is specific to the given writing system</returns>
		/// ------------------------------------------------------------------------------------
		public virtual FontInfo FontInfoForWs(int ws)
		{
			if (ws == -1)
				return m_defaultFontInfo;
			FontInfo info;
			if (!m_fontInfoOverrides.TryGetValue(ws, out info))
			{
				info = new FontInfo();
				m_fontInfoOverrides.Add(ws, info);
			}
			return info;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the real font name (family) for the given writing system. This is the real font
		/// face name, not the magic font name.
		/// </summary>
		/// <param name="ws">The writing system, or -1 for default font info</param>
		/// <returns>The font name, which may come from either the default info for the style
		/// or an override that is specific to the given writing system. If this is a character
		/// style which does not (directly or through inheritance) specify a font name, then
		/// this method returns null.</returns>
		/// <exception cref="InvalidOperationException">If this style info entry has not been
		/// added to a StyleInfoTable or if ConnectStyles has not been called</exception>
		/// ------------------------------------------------------------------------------------
		public string RealFontNameForWs(int ws)
		{
			if (m_owningTable == null)
				throw new InvalidOperationException("Cannot retrieve the real font name until this style info entry has been added to a StyleInfoTable.");
			FontInfo fontinfo = FontInfoForWs(ws);
			if (!fontinfo.m_fontName.ValueIsSet)
				return null;
			string fontName = fontinfo.m_fontName.Value;
			if (StyleServices.IsMagicFontName(fontName))
				return m_owningTable.ResolveMagicFontName(fontName, ws != -1 ? ws : m_cache.WritingSystemFactory.UserWs);
			return fontName;
		}
		#endregion

		#region private/internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets any inherited property to the defaults.
		/// </summary>
		/// <remarks>After calling this method, it should be safe to access the
		/// InheritableStyleProp.Value property for any property of this style. These defaults
		/// must be kept in sync with the values set in VwPropertyStore::SetInitialState().
		/// </remarks>
		/// <exception cref="InvalidOperationException">If this style is based on another style.
		/// This method should only be called for the "Normal" style (or other styles that have
		/// no other base than the system defaults).</exception>
		/// ------------------------------------------------------------------------------------
		internal void SetAllDefaults()
		{
			if (m_basedOnStyleName != null)
				throw new InvalidOperationException("This method should only be called for styles that have no other base than the system defaults.");
			m_alignment.SetDefaultValue(FwTextAlign.ktalLeading);
			m_border.SetDefaultValue(new BorderThicknesses());
			m_borderColor.SetDefaultValue(Color.Black);
			m_defaultFontInfo.SetAllDefaults();
			m_firstLineIndent.SetDefaultValue(0);
			m_leadingIndent.SetDefaultValue(0);
			m_lineSpacing.SetDefaultValue(new LineHeightInfo());
			m_rtl.SetDefaultValue(m_owningTable.DefaultRightToLeft);
			m_spaceAfter.SetDefaultValue(0);
			m_spaceBefore.SetDefaultValue(0);
			m_trailingIndent.SetDefaultValue(0);
			m_bulletInfo.SetDefaultValue(new BulletInfo());
			m_keepWithNext.SetDefaultValue(false);
			m_keepTogether.SetDefaultValue(false);
			m_widowOrphanControl.SetDefaultValue(true);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this style is built in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsBuiltIn
		{
			get { return m_isBuiltIn; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this style has been modified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsModified
		{
			get { return m_isModified; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The real style associated with the BaseStyleInfo. This value can be null.
		/// If this property is set, all other properties will take on the properties of the
		/// specified style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStStyle RealStyle
		{
			get { return m_style; }
			set { SetPropertiesBasedOnStyle(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The User level of the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int UserLevel
		{
			get { return m_userLevel; }
			set { m_userLevel = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The description of the usage of the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Usage
		{
			get { return m_usage; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Context
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextValues Context
		{
			get { return m_context; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StructureValues Structure
		{
			get { return m_structure; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Function
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FunctionValues Function
		{
			get { return m_function; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StyleNumber
		{
			get { return m_styleNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style number for the "Next" style (always returns 0 for character styles).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NextStyleNumber
		{
			get { return m_nextStyle == null ? 0 : m_nextStyle.m_styleNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style number for the "Based-On" style (always returns 0 for character
		/// styles).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BasedOnStyleNumber
		{
			get { return m_basedOnStyle == null ? 0 : m_basedOnStyle.m_styleNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether a style is a paragraph style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsParagraphStyle
		{
			get
			{
				return m_styleType == StyleType.kstParagraph;
			}
			set
			{
				m_styleType = value ? StyleType.kstParagraph : StyleType.kstCharacter;
			}
		}

		/// <summary>
		/// IStyle implementation of looking up the default font info.
		/// </summary>
		public ICharacterStyleInfo DefaultCharacterStyleInfo
		{
			get { return FontInfoForWs(-1); }
		}

		/// <summary>
		/// IStyle implementation of looking up the font overrides for a particular WS.
		/// </summary>
		public ICharacterStyleInfo OverrideCharacterStyleInfo(int ws)
		{
			return FontInfoForWs(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a style is a character style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCharacterStyle
		{
			get { return !IsParagraphStyle; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is valid.
		/// </summary>
		/// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsValid
		{
			get { return m_fIsValid; }
			set { m_fIsValid = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates the derived (from inheritance) value of whether this style specifies a
		/// right-to-left direction or not
		/// </summary>
		/// <value>One of three values: <c>triTrue</c> if right-to-left; <c>triFalse</c> if
		/// left-to-right; or <c>triNotSet</c> if direction is not specified (i.e., will be
		/// determined based on the default direction for the view).</value>
		/// ------------------------------------------------------------------------------------
		public TriStateBool DirectionIsRightToLeft
		{
			get { return m_rtl.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates the derived (from inheritance) value of whether the lines of a
		/// paragraph having this style should be kept together on a page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool KeepTogether
		{
			get { return m_keepTogether.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates the derived (from inheritance) value of whether a paragraph having this
		/// style should be kept together on a page with the following paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool KeepWithNext
		{
			get { return m_keepWithNext.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates the derived (from inheritance) value of whether the first and last line of
		/// a paragraph having this style should be prevented from laying out all alone on a
		/// page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool WidowOrphanControl
		{
			get { return m_widowOrphanControl.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the alignment</summary>
		/// ------------------------------------------------------------------------------------
		public FwTextAlign Alignment
		{
			get { return m_alignment.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the inter-line spacing in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public LineHeightInfo LineSpacing
		{
			get { return m_lineSpacing.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the space above paragraph in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int SpaceBefore
		{
			get { return m_spaceBefore.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the space below paragraph in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int SpaceAfter
		{
			get { return m_spaceAfter.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of first line in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int FirstLineIndent
		{
			get { return m_firstLineIndent.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of paragraph from leading edge in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int LeadingIndent
		{
			get { return m_leadingIndent.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the indentation of paragraph from trailing edge in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int TrailingIndent
		{
			get { return m_trailingIndent.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the thickness of leading border in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int BorderLeading
		{
			get { return m_border.Value.Leading; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the thickness of trailing border in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int BorderTrailing
		{
			get { return m_border.Value.Trailing; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the thickness of top border in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int BorderTop
		{
			get { return m_border.Value.Top; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the thickness of bottom border in millipoints</summary>
		/// ------------------------------------------------------------------------------------
		public int BorderBottom
		{
			get { return m_border.Value.Bottom; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the ARGB Color of borders</summary>
		/// ------------------------------------------------------------------------------------
		public Color BorderColor
		{
			get { return m_borderColor.Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the based on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo BasedOnStyle
		{
			get { return m_basedOnStyle; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first based-on style info that represents a real style by going down the
		/// inheritance chain until we find one that has a real style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal BaseStyleInfo RealBasedOnStyleInfo
		{
			get
			{
				return (m_basedOnStyle == null) ? null :
					(m_basedOnStyle.RealStyle != null ? m_basedOnStyle :
					m_basedOnStyle.RealBasedOnStyleInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this style inherits from another style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Inherits
		{
			get { return m_styleType == StyleType.kstCharacter || m_basedOnStyle != null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether other styles can inherit from this style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanInheritFrom
		{
			get
			{
				return (!IsInternalStyle &&
						Function != FunctionValues.Chapter &&
						Function != FunctionValues.Verse);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether user can apply this style directly. If not, this style can't be
		/// set as a following style for user-created styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsInternalStyle
		{
			get { return StyleServices.IsContextInternal(Context); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next style.
		/// </summary>
		/// <value>The next style.</value>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo NextStyle
		{
			get { return m_nextStyle; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the explicit and inherited style properties.
		/// </summary>
		/// <value>The text props.</value>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps TextProps
		{
			get { return m_textProps; }
			internal set { m_textProps = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FDO cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_cache; }
		}
		#endregion
	}
	#endregion
}
