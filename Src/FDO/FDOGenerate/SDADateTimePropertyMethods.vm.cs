## --------------------------------------------------------------------------------------------
## Copyright (c) 2007-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
		/// <summary>
		/// Get a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override DateTime GetTimePropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetTimePropertyInternal(flid);
#foreach( $prop in $class.DateTimeProperties )
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
		/// Set a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, DateTime newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.DateTimeProperties )
				case $prop.Number:
					if (useAccessor)
						$prop.NiuginianPropName = newValue;
					else
						m_$prop.NiuginianPropName = newValue;
					break;
#end
			}
		}