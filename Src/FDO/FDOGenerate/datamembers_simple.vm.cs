## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#if( $prop.CSharpType == "???" )
		// Type not implemented in FDO
#elseif( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode")
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data member for: ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if ( $prop.Signature == "MultiString" )
		private IMultiString m_$prop.NiuginianPropName;
#else
		private IMultiUnicode m_$prop.NiuginianPropName;
#end
#elseif( $prop.Signature == "String")
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data member for: ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ITsString m_$prop.NiuginianPropName;
#elseif( $prop.Signature == "TextPropBinary")
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data member for: ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private $prop.CSharpType m_$prop.NiuginianPropName;
#elseif( $prop.Signature == "Time")
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data member for: ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private $prop.CSharpType m_$prop.NiuginianPropName = DateTime.MinValue;
#else
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data member for: ${prop.Name}
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.OverridenType == "")
		private $prop.CSharpType m_$prop.NiuginianPropName;
#else
		private int m_$prop.NiuginianPropName;
#end
#end
