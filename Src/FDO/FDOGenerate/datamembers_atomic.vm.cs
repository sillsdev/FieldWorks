## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
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
