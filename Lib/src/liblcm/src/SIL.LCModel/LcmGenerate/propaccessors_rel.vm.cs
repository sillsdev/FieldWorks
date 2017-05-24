## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $propComment = $prop.Comment )
#set( $propNotes = $prop.Notes )
#set( $propTypeClass = $lcmgenerate.GetClass($prop.Signature) )
#if ( $prop.IsHandGenerated)
#set( $generated = "_Generated" )
#else
#set( $generated = "" )
#end

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
## Interfaces are not needed here (for now anyway),
## since it will return some kind of LCM collection class.
#if( $prop.IsOwning )
#if ( $prop.Cardinality.ToString() == "Collection" )
		[ModelProperty(CellarPropertyType.OwningCollection, $prop.Number, "$propTypeClass")]
		public ILcmOwningCollection<I$propTypeClass> $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.OwningSequence, $prop.Number, "$propTypeClass")]
		public ILcmOwningSequence<I$propTypeClass> $prop.NiuginianPropName$generated
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		[ModelProperty(CellarPropertyType.ReferenceCollection, $prop.Number, "$propTypeClass")]
		public ILcmReferenceCollection<I$propTypeClass> $prop.NiuginianPropName$generated
#else
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		[ModelProperty(CellarPropertyType.ReferenceSequence, $prop.Number, "IAnalyses")]
		public ILcmReferenceSequence<IAnalysis> $prop.NiuginianPropName$generated
#else
		[ModelProperty(CellarPropertyType.ReferenceSequence, $prop.Number, "$propTypeClass")]
		public ILcmReferenceSequence<I$propTypeClass> $prop.NiuginianPropName$generated
#end
#end
		{
			get
			{
				lock (SyncRoot)
				{
#if( $prop.IsOwning )
#if ( $prop.Cardinality.ToString() == "Collection" )
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new LcmOwningCollection<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#else
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new LcmOwningSequence<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
					if (m_$prop.NiuginianPropName == null)
					{
						m_$prop.NiuginianPropName = new LcmReferenceCollection<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
					}
#else
					if (m_$prop.NiuginianPropName == null)
					{
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
						m_$prop.NiuginianPropName = new LcmReferenceSequence<IAnalysis>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<IAnalysisRepository>(),
							this, $prop.Number);
#else
						m_$prop.NiuginianPropName = new LcmReferenceSequence<I$propTypeClass>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<I${propTypeClass}Repository>(),
							this, $prop.Number);
#end
					}
#end
				}
				return m_$prop.NiuginianPropName;
			}
		}