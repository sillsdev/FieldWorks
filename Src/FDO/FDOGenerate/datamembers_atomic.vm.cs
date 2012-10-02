## --------------------------------------------------------------------------------------------
## Copyright (C) 2006-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propTypeClass = $fdogenerate.GetClass($prop.Signature) )
		/// ------------------------------------------------------------------------------------
		/// <summary>Data member for: ${prop.Name}. May be null, appropriate subclass of CmObject,
		/// or a base64 string representing a guid.</summary>
		/// ------------------------------------------------------------------------------------
		private object m_$prop.NiuginianPropName;
