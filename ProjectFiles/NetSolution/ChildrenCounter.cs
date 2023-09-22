#region Using directives
using System;
using System.Linq;
using CoreBase = FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.NetLogic;
#endregion

public class ChildrenCounter : BaseNetLogic
{
    public override void Start()
    {
        var nodePointerVariable = LogicObject.GetVariable("Node");
        if (nodePointerVariable == null)
        {
            Log.Error("ChildrenCounter", "Missing Node variable on ChildrenCounter");
            return;
        }

        var nodePointerValue = (NodeId) nodePointerVariable.Value;
        if (nodePointerValue == null || nodePointerValue  == NodeId.Empty)
        {
            Log.Warning("ChildrenCounter", "Node variable not set");
            return;
        }

        var countVariable = LogicObject.GetVariable("Count");
        if (countVariable == null)
        {
            Log.Error("ChildrenCounter", "Missing variable Count on ChildrenCounter");
            return;
        }

        var resolvedResult = InformationModel.Get(nodePointerValue);
        countVariable.Value = resolvedResult.Children.Count;

        if (referencesEventRegistration != null)
        {
            referencesEventRegistration.Dispose();
            referencesEventRegistration = null;
        }

        referencesObserver = new ReferencesObserver(resolvedResult, countVariable);
        referencesObserver.Initialize();

        referencesEventRegistration = resolvedResult.RegisterEventObserver(
            referencesObserver, EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved);
    }

    public override void Stop()
    {
        if (referencesEventRegistration != null)
            referencesEventRegistration.Dispose();

        referencesEventRegistration = null;
        referencesObserver = null;
    }

    private class ReferencesObserver : IReferenceObserver
    {
        public ReferencesObserver(IUANode nodeToMonitor, IUAVariable countVariable)
        {
            this.nodeToMonitor = nodeToMonitor;
            this.countVariable = countVariable;
        }

        public void Initialize()
        {
            countVariable.Value = nodeToMonitor.Children.Count;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            if (IsReferenceAllowed(referenceTypeId))
            {
                ++countVariable.Value;
            }
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            if (IsReferenceAllowed(referenceTypeId) && countVariable.Value > 0)
            {
                --countVariable.Value;
            }
        }

        public bool IsReferenceAllowed(NodeId referenceTypeId)
        {
            return referenceTypeId == UAManagedCore.OpcUa.ReferenceTypes.HasComponent ||
                   referenceTypeId == UAManagedCore.OpcUa.ReferenceTypes.HasOrderedComponent;
        }

        private IUANode nodeToMonitor;
        private IUAVariable countVariable;
    }

    private ReferencesObserver referencesObserver;
    private IEventRegistration referencesEventRegistration;
}
