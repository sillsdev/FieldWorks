<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!--
The element with appropriate xmi.id value
-->
	<xsl:key name="ElementByID" match="*" use="@xmi.id"/>
	<xsl:key name="ModelPackageSubsystemByID" match="Model_Management.Model | Model_Management.Package | Model_Management.Subsystem" use="@xmi.id"/>
	<xsl:key name="UseCaseByID" match="Behavioral_Elements.Use_Cases.UseCase" use="@xmi.id"/>
	<xsl:key name="ExtensionPointByID" match="Behavioral_Elements.Use_Cases.ExtensionPoint" use="@xmi.id"/>
	<xsl:key name="NodeByID" match="Foundation.Core.Node" use="@xmi.id"/>
	<xsl:key name="NodeInstanceByID" match="Behavioral_Elements.Common_Behavior.NodeInstance" use="@xmi.id"/>
	<xsl:key name="ComponentByID" match="Foundation.Core.Component" use="@xmi.id"/>
	<xsl:key name="ComponentInstanceByID" match="Behavioral_Elements.Common_Behavior.ComponentInstance" use="@xmi.id"/>
	<xsl:key name="ClassifierRoleByID" match="Behavioral_Elements.Collaborations.ClassifierRole" use="@xmi.id"/>
	<xsl:key name="StateByID" match="Behavioral_Elements.State_Machines.CompositeState | Behavioral_Elements.State_Machines.SubmachineState | Behavioral_Elements.State_Machines.StubState | Behavioral_Elements.Activity_Graphs.ActionState | Behavioral_Elements.State_Machines.FinalState | Behavioral_Elements.State_Machines.Pseudostate | Behavioral_Elements.State_Machines.SynchState | Behavioral_Elements.State_Machines.State | Behavioral_Elements.Activity_Graphs.ObjectFlowState | Behavioral_Elements.Activity_Graphs.SubactivityState" use="@xmi.id"/>
	<xsl:key name="SubmachinesSubmachineByID" match="Behavioral_Elements.State_Machines.StateMachine | Behavioral_Elements.Activity_Graphs.ActivityGraph" use="@xmi.id"/>
	<xsl:key name="AttributeByID" match="Foundation.Core.Attribute" use="@xmi.id"/>
	<xsl:key name="DataValueByID" match="Behavioral_Elements.Common_Behavior.DataValue" use="@xmi.id"/>
	<xsl:key name="InstanceByID" match="Behavioral_Elements.Common_Behavior.Instance" use="@xmi.id"/>
	<xsl:key name="MessageByID" match="Behavioral_Elements.Collaborations.Message" use="@xmi.id"/>
	<xsl:key name="GeneralizationByID" match="Foundation.Core.Generalization" use="@xmi.id"/>
	<xsl:key name="DependencyByID" match="Foundation.Core.Dependency" use="@xmi.id"/>
	<xsl:key name="AbstractionByID" match="Foundation.Core.Abstraction" use="@xmi.id"/>
	<xsl:key name="AssociationByID" match="Foundation.Core.Association" use="@xmi.id"/>
	<xsl:key name="BindingByID" match="Foundation.Core.Binding" use="@xmi.id"/>
	<xsl:key name="PermissionByID" match="Foundation.Core.Permission" use="@xmi.id"/>
	<xsl:key name="UsageByID" match="Foundation.Core.Usage" use="@xmi.id"/>
	<xsl:key name="TransitionByID" match="Behavioral_Elements.State_Machines.Transition" use="@xmi.id"/>
	<xsl:key name="OperationByID" match="Foundation.Core.Operation" use="@xmi.id"/>
	<xsl:key name="DiagramByID" match="/XMI/XMI.extensions/mdOwnedDiagrams/mdElement" use="@xmi.id"/>
	<xsl:key name="ClassByParentID" match="Foundation.Core.Class[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="InterfaceByParentID" match="Foundation.Core.Interface[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
		 <xsl:key name="ClassByAncestorID" match="Foundation.Core.Class[@xmi.id]" use="ancestor::*/@xmi.id"/>
		 <xsl:key name="InterfaceByAncestorID" match="Foundation.Core.Interface[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="ActorByParentID" match="Behavioral_Elements.Use_Cases.Actor[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="UseCaseByParentID" match="Behavioral_Elements.Use_Cases.UseCase[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="ActorByAncestorID" match="Behavioral_Elements.Use_Cases.Actor[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="UseCaseByAncestorID" match="Behavioral_Elements.Use_Cases.UseCase[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="ComponentByParentID" match="Foundation.Core.Component[@xmi.id] | Behavioral_Elements.Common_Behavior.ComponentInstance[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="NodeByParentID" match="Foundation.Core.Node[@xmi.id] | Behavioral_Elements.Common_Behavior.NodeInstance[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="ComponentByAncestorID" match="Foundation.Core.Component[@xmi.id] | Behavioral_Elements.Common_Behavior.ComponentInstance[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="NodeByAncestorID" match="Foundation.Core.Node[@xmi.id] | Behavioral_Elements.Common_Behavior.NodeInstance[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="AllCollaborations" match="Behavioral_Elements.Collaborations.Collaboration[@xmi.id]" use="true()"/>
	<xsl:key name="CollaborationByParentID" match="Behavioral_Elements.Collaborations.Collaboration[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="CollaborationByAncestorID" match="Behavioral_Elements.Collaborations.Collaboration[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="AllStateMachines" match="Behavioral_Elements.State_Machines.StateMachine[@xmi.id]" use="true()"/>
	<xsl:key name="StateMachineByParentID" match="Behavioral_Elements.State_Machines.StateMachine[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="StateMachineByAncestorID" match="Behavioral_Elements.State_Machines.StateMachine[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<xsl:key name="AllActivityGraphs" match="Behavioral_Elements.Activity_Graphs.ActivityGraph[@xmi.id]" use="true()"/>
	<xsl:key name="ActivityGraphByParentID" match="Behavioral_Elements.Activity_Graphs.ActivityGraph[@xmi.id]" use="parent::Foundation.Core.Namespace.ownedElement/../@xmi.id"/>
	<xsl:key name="ActivityGraphByAncestorID" match="Behavioral_Elements.Activity_Graphs.ActivityGraph[@xmi.id]" use="ancestor::*/@xmi.id"/>
	<!-- Association by end element ID -->
	<xsl:key name="AssociationByEndElementID" match="Foundation.Core.Association" use="./Foundation.Core.Association.connection/Foundation.Core.AssociationEnd/Foundation.Core.AssociationEnd.type/*/@xmi.idref"/>
	<!-- Cellar Association by end element ID -->
	<xsl:key name="CellarAssociationByEndElementID" match="Foundation.Core.Association" use="./Foundation.Core.Association.connection/Foundation.Core.AssociationEnd[position()=1]/Foundation.Core.AssociationEnd.type/Foundation.Core.Class/@xmi.idref"/>
<!-- AssociationRole by end element ID -->
	<xsl:key name="AssociationRoleByEndElementID" match="Behavioral_Elements.Collaborations.AssociationRole" use="./Foundation.Core.Association.connection/Behavioral_Elements.Collaborations.AssociationEndRole/Foundation.Core.AssociationEnd.type/Behavioral_Elements.Collaborations.ClassifierRole/@xmi.idref"/>
	<!-- Extend relationship by end element ID -->
	<xsl:key name="ExtendRelationshipByEndElementID" match="Behavioral_Elements.Use_Cases.Extend" use="./Behavioral_Elements.Use_Cases.Extend.base/Behavioral_Elements.Use_Cases.UseCase/@xmi.idref | ./Behavioral_Elements.Use_Cases.Extend.extension/Behavioral_Elements.Use_Cases.UseCase/@xmi.idref"/>
	<!-- Include relationship by end element ID -->
	<xsl:key name="IncludeRelationshipByEndElementID" match="Behavioral_Elements.Use_Cases.Include" use="./Behavioral_Elements.Use_Cases.Include.base/Behavioral_Elements.Use_Cases.UseCase/@xmi.idref | ./Behavioral_Elements.Use_Cases.Include.addition/Behavioral_Elements.Use_Cases.UseCase/@xmi.idref"/>
	<!-- Link by end element ID -->
	<xsl:key name="LinkByLinkEndID" match="Behavioral_Elements.Common_Behavior.Link" use="./Behavioral_Elements.Common_Behavior.Link.connection/Behavioral_Elements.Common_Behavior.LinkEnd/@xmi.id"/>
	<!-- Stereotype by ID -->
	<xsl:key name="StereotypeByID" match="/XMI/XMI.content/Model_Management.Model/Foundation.Core.Namespace.ownedElement/Foundation.Extension_Mechanisms.Stereotype" use="@xmi.id"/>
	<!-- Tagged Value by ID -->
	<xsl:key name="TaggedValueByID" match="/XMI/XMI.content/Model_Management.Model/Foundation.Core.Namespace.ownedElement/Foundation.Extension_Mechanisms.TaggedValue" use="@xmi.id"/>
	<!-- Constraint by ID -->
	<xsl:key name="ConstraintByID" match="/XMI/XMI.content/Model_Management.Model/Foundation.Core.Namespace.ownedElement/Foundation.Core.Constraint" use="@xmi.id"/>
	<!-- Association Role By id -->
	<xsl:key name="AssociationRoleByID" match="Behavioral_Elements.Collaborations.AssociationRole" use="@xmi.id"/>
	<!-- Instance by Attribute Link Initial value -->
	<xsl:key name="InstanceByAttributeLinkValue" match="Behavioral_Elements.Common_Behavior.Instance" use="Behavioral_Elements.Common_Behavior.Instance.slot/Behavioral_Elements.Common_Behavior.AttributeLink/Behavioral_Elements.Common_Behavior.AttributeLink.value/Behavioral_Elements.Common_Behavior.Instance/@xmi.idref"/>
</xsl:stylesheet>
