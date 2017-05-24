## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propTypeInterfaceBase = $lcmgenerate.GetClass($prop.Signature) )
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the vector for use in subsequent calls to the ${prop.Name} property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
#if( $prop.IsOwning )
## Owning col/seq.
#if ( $prop.Cardinality.ToString() == "Collection" )
		private ILcmOwningCollection<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#else
		private ILcmOwningSequence<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		private ILcmReferenceCollection<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#else
##This has to be Reference Sequence
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		private ILcmReferenceSequence<IAnalysis> m_$prop.NiuginianPropName = null;
#else
		private ILcmReferenceSequence<I$propTypeInterfaceBase> m_$prop.NiuginianPropName = null;
#end
#end