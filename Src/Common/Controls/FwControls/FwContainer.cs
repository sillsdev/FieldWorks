// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwContainer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FieldWorks container class. This class helps to expose services.
	/// </summary>
	/// <remarks>To use this class, create a components object of type FwContainer instead of
	/// Container. Add any components and services to the components container. Set the Site
	/// property of your parent class, e.g. to the Site property of the first component:
	/// <code>
	/// 	this.components = new FwContainer();
	/// 	Persistence persistence = new Persistence(this.components);
	/// 	this.Site = components.Components[0].Site;
	/// </code>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class FwContainer : Container
	{
		#region FwSite class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the ISite interface
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public class FwSite : ISite
		{
			private IComponent m_Component;
			private FwContainer m_Container;
			private string m_Name;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:FwSite"/> class.
			/// </summary>
			/// <param name="component">The component.</param>
			/// <param name="container">The container.</param>
			/// <param name="name">The name.</param>
			/// ------------------------------------------------------------------------------------
			public FwSite(IComponent component, FwContainer container, string name)
			{
				m_Component = component;
				m_Container = container;
				m_Name = name;
			}

			#region ISite Members

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the component associated with the <see cref="T:System.ComponentModel.ISite"></see> when implemented by a class.
			/// </summary>
			/// <value></value>
			/// <returns>The <see cref="T:System.ComponentModel.IComponent"></see> instance associated with the <see cref="T:System.ComponentModel.ISite"></see>.</returns>
			/// ------------------------------------------------------------------------------------
			public IComponent Component
			{
				get { return m_Component; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the <see cref="T:System.ComponentModel.IContainer"></see> associated with the <see cref="T:System.ComponentModel.ISite"></see> when implemented by a class.
			/// </summary>
			/// <value></value>
			/// <returns>The <see cref="T:System.ComponentModel.IContainer"></see> instance associated with the <see cref="T:System.ComponentModel.ISite"></see>.</returns>
			/// ------------------------------------------------------------------------------------
			public IContainer Container
			{
				get { return m_Container; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the component is in design mode when implemented by a class.
			/// </summary>
			/// <value></value>
			/// <returns>Always false.</returns>
			/// ------------------------------------------------------------------------------------
			public bool DesignMode
			{
				get { return false; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the name of the component associated with the <see cref="T:System.ComponentModel.ISite"></see> when implemented by a class.
			/// </summary>
			/// <value></value>
			/// <returns>The name of the component associated with the <see cref="T:System.ComponentModel.ISite"></see>; or null, if no name is assigned to the component.</returns>
			/// ------------------------------------------------------------------------------------
			public string Name
			{
				get { return m_Name; }
				set
				{
					if (((value == null) || (m_Name == null)) || !value.Equals(m_Name))
					{
						m_Container.ValidateName(m_Component, value);
						m_Name = value;
					}
				}
			}

			#endregion

			#region IServiceProvider Members

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the service object of the specified type.
			/// </summary>
			/// <param name="serviceType">An object that specifies the type of service object to
			/// get.</param>
			/// <returns>
			/// A service object of type serviceType.-or- null if there is no service object of
			/// type serviceType.
			/// </returns>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
				Justification="See TODO-Linux comment")]
			public object GetService(Type serviceType)
			{
				// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (serviceType == typeof(ISite))
					return this;
				return m_Container.GetService(serviceType);
			}

			#endregion
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwContainer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwContainer()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor - initializes a new instance of the <see cref="T:FwContainer"/>
		/// class based on the existing container.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <remarks>Note: this moves all the components to this new container!</remarks>
		/// ------------------------------------------------------------------------------------
		public FwContainer(IContainer container)
		{
			for (int i = 0; i < container.Components.Count; )
			{
				IComponent component = container.Components[i];
				container.Remove(component);
				Add(component);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a site <see cref="T:System.ComponentModel.ISite"></see> for the given
		/// <see cref="T:System.ComponentModel.IComponent"></see> and assigns the given name
		/// to the site.
		/// </summary>
		/// <param name="component">The <see cref="T:System.ComponentModel.IComponent"></see> to create a site for.</param>
		/// <param name="name">The name to assign to component, or null to skip the name assignment.</param>
		/// <returns>The newly created site.</returns>
		/// ------------------------------------------------------------------------------------
		protected override ISite CreateSite(IComponent component, string name)
		{
			return new FwSite(component, this, name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the service object of the specified type, if it is available.
		/// </summary>
		/// <param name="service">The <see cref="T:System.Type"></see> of the service to
		/// retrieve.</param>
		/// <returns>
		/// An <see cref="T:System.Object"></see> implementing the requested service, or null
		/// if the service cannot be resolved.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		protected override object GetService(Type service)
		{
			object obj = base.GetService(service);

			// if the base class doesn't return the service we look if any of the components
			// is of the requested type
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (obj == null && service != typeof(ContainerFilterService))
			{
				foreach (Component component in Components)
				{
					if (service.IsAssignableFrom(component.GetType()))
						return component;
				}
			}
			return obj;
		}
	}
}
