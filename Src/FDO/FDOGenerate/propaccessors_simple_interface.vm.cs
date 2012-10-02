## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#if( $prop.Cardinality.ToString() == "Atomic")
	#set( $suffix = "Hvo")
#else
	#set( $suffix = "")
#end

#if( $prop.CSharpType == "???" )
		// Type not implemented in FDO
#elseif( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		$prop.CSharpType $prop.NiuginianPropName
		{
			get;
		}
#elseif( $prop.Signature == "String")
##
## No "set" property is needed; one "gets" the accessor and then can use that to set
## individual string alternates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		$prop.CSharpType $prop.NiuginianPropName
		{
			get;
		}
#elseif( $prop.Signature == "TextPropBinary")
##
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name} $suffix
		/// </summary>
		/// <remarks>TsTextProps handled as Unknown props</remarks>
		/// ------------------------------------------------------------------------------------
		$prop.CSharpType $prop.NiuginianPropName$suffix
		{
			get;
			set;
		}
#else
## For CmObjects the return value here will just be an int, not an interface.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name} $suffix
		/// </summary>
		/// ------------------------------------------------------------------------------------
		$prop.CSharpType $prop.NiuginianPropName$suffix
		{
			get;
			set;
		}
#end
