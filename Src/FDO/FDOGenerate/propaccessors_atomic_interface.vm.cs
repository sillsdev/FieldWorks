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
#set( $propReturnType = $fdogenerate.GetClass($prop.Signature) )

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		I$propReturnType $prop.NiuginianPropName
		{
			get;
			set;
		}
