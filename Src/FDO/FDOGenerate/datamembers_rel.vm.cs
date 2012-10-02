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
#set( $propTypeInterfaceBase = $fdogenerate.GetClass($prop.Signature) )
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the vector for use in subsequent calls to the ${prop.Name} property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsOwning )
## Owning col/seq.
#if ( $prop.Cardinality.ToString() == "Collection" )
		private IFdoOwningCollection<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#else
		private IFdoOwningSequence<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		private IFdoReferenceCollection<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#else
##This has to be Reference Sequence
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		private IFdoReferenceSequence<IAnalysis> m_$prop.NiuginianPropName = null;
#else
		private IFdoReferenceSequence<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#end
#end