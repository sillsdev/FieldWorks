## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## It is only used for sequence or collection properties, so it need not deal with interfaces,
## in the return type.
## --------------------------------------------------------------------------------------------
#set( $propTypeInterfaceBase = $fdogenerate.GetClass($prop.Signature) )

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		${prop.CSharpType}<I$propTypeInterfaceBase> $prop.NiuginianPropName
		{
			get;
		}
