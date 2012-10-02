## --------------------------------------------------------------------------------------------
## Copyright (C) 2007-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
#if( $class.StringProperties.Count > 0 )

		/// <summary>
		/// Get an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override ITsString GetITsStringPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetITsStringPropertyInternal(flid);
#foreach( $prop in $class.StringProperties )
				case $prop.Number:
#if( $prop.IsHandGenerated)
					return $prop.NiuginianPropName;
#else
					return m_$prop.NiuginianPropName;
#end
#end
			}
		}

		/// <summary>
		/// Set an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, ITsString newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.StringProperties )
				case $prop.Number:
					if (useAccessor)
						$prop.NiuginianPropName = newValue;
					else
						m_$prop.NiuginianPropName = newValue;
					break;
#end
			}
		}
#end
#if( $class.MultiProperties.Count > 0 )

		/// <summary>
		/// Get an ITsMultiString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override ITsMultiString GetITsMultiStringPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetITsMultiStringPropertyInternal(flid);
#foreach( $prop in $class.MultiProperties )
				case $prop.Number:
					return $prop.NiuginianPropName;
#end
			}
		}
#end
#if( $class.UnicodeProperties.Count > 0 )

		/// <summary>
		/// Get a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override string GetStringPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetStringPropertyInternal(flid);
#foreach( $prop in $class.UnicodeProperties )
				case $prop.Number:
#if( $prop.IsHandGenerated)
					return $prop.NiuginianPropName;
#else
					return m_$prop.NiuginianPropName;
#end
#end
			}
		}

		/// <summary>
		/// Set a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, string newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.UnicodeProperties )
				case $prop.Number:
					if (useAccessor)
						$prop.NiuginianPropName = newValue;
					else
						m_$prop.NiuginianPropName = newValue;
					break;
#end
			}
		}
#end
#if( $class.TextPropBinaryProperties.Count > 0 )

		/// <summary>
		/// Get an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override ITsTextProps GetITsTextPropsPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetITsTextPropsPropertyInternal(flid);
#foreach( $prop in $class.TextPropBinaryProperties )
				case $prop.Number:
#if( $prop.IsHandGenerated)
					return $prop.NiuginianPropName;
#else
					return m_$prop.NiuginianPropName;
#end
#end
			}
		}

		/// <summary>
		/// Set an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, ITsTextProps newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.TextPropBinaryProperties )
				case $prop.Number:
					if (useAccessor)
						$prop.NiuginianPropName = newValue;
					else
						m_$prop.NiuginianPropName = newValue;
					break;
#end
			}
		}
#end