#region Using directives
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using UAManagedCore;
#endregion

public class AlarmBannerLogic : BaseNetLogic {
    public override void Start() {
        var context = LogicObject.Context;

        affinityId = context.AssignAffinityId();

        RegisterObserverOnLocalizedAlarmsContainer(context);
        RegisterObserverOnSessionActualLanguageChange(context);
        RegisterObserverOnLocalizedAlarmsObject(context);
    }

    public override void Stop() {
        alarmEventRegistration?.Dispose();
        alarmEventRegistration2?.Dispose();
        sessionActualLanguageRegistration?.Dispose();
        alarmBannerSelector?.Dispose();

        alarmEventRegistration = null;
        alarmEventRegistration2 = null;
        sessionActualLanguageRegistration = null;
        alarmBannerSelector = null;

        retainedAlarmsObjectObserver = null;
    }

    [ExportMethod]
    public void NextAlarm() {
        alarmBannerSelector?.OnNextAlarmClicked();
    }

    [ExportMethod]
    public void PreviousAlarm() {
        alarmBannerSelector?.OnPreviousAlarmClicked();
    }

    public void RegisterObserverOnLocalizedAlarmsObject(IContext context) {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        retainedAlarmsObjectObserver = new RetainedAlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer(ctx));

        // observe ReferenceAdded of localized alarm containers
        alarmEventRegistration2 = retainedAlarms.RegisterEventObserver(
            retainedAlarmsObjectObserver, EventType.ForwardReferenceAdded, affinityId);
    }

    public void RegisterObserverOnLocalizedAlarmsContainer(IContext context) {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
        var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
        var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
            localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

        if (alarmEventRegistration != null) {
            alarmEventRegistration.Dispose();
            alarmEventRegistration = null;
        }

        if (alarmBannerSelector != null)
            alarmBannerSelector.Dispose();
        alarmBannerSelector = new AlarmBannerSelector(LogicObject, localizedAlarmsContainer);

        if (localizedAlarmsContainer?.Children.Count > 0)
            alarmBannerSelector.Initialize();

        alarmEventRegistration = localizedAlarmsContainer?.RegisterEventObserver(
            alarmBannerSelector,
            EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved, affinityId);
    }

    public void RegisterObserverOnSessionActualLanguageChange(IContext context) {
        var currentSessionActualLanguage = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLanguage"];

        sessionActualLanguageChangeObserver = new CallbackVariableChangeObserver(
            (IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) => {
                RegisterObserverOnLocalizedAlarmsContainer(context);
            });

        sessionActualLanguageRegistration = currentSessionActualLanguage.RegisterEventObserver(
            sessionActualLanguageChangeObserver, EventType.VariableValueChanged, affinityId);
    }

    private class RetainedAlarmsObjectObserver : IReferenceObserver {
        public RetainedAlarmsObjectObserver(Action<IContext> action) {
            registrationCallback = action;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
            string localeId = targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId;
            if (String.IsNullOrEmpty(localeId))
                localeId = "en-US";

            if (targetNode.BrowseName == localeId)
                registrationCallback(targetNode.Context);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
        }

        private Action<IContext> registrationCallback;
    }

    uint affinityId = 0;
    RetainedAlarmsObjectObserver retainedAlarmsObjectObserver;
    IEventRegistration alarmEventRegistration;
    IEventRegistration alarmEventRegistration2;
    IEventRegistration sessionActualLanguageRegistration;
    IEventObserver sessionActualLanguageChangeObserver;
    AlarmBannerSelector alarmBannerSelector;
}

public class AlarmBannerSelector : IDisposable, IReferenceObserver {

    public AlarmBannerSelector(IUANode logicNode, IUANode localizedAlarmsContainer) {
        this.retaiendAlarms = new List<NodeId>();
        this.retainedAlarmsLock = new Object();
        this.logicNode = logicNode;
        InitializeRetainedAlarmList(localizedAlarmsContainer);

        currentDisplayedAlarm = logicNode.GetVariable("CurrentDisplayedAlarm");
        currentDisplayedAlarmIndex = logicNode.GetVariable("CurrentDisplayedAlarmIndex");
        retainedAlarmsCount = logicNode.GetVariable("AlarmCount");
        rotationTime = logicNode.GetVariable("RotationTime");
        rotationTime.VariableChange += RotationTime_VariableChange;

        UpdateRetainedAlarmsCount();

        rotationTask = new PeriodicTask(() => { lock (retainedAlarmsLock) { DisplayNextAlarm(); } }, rotationTime.Value, logicNode);
    }

    public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
        lock (retainedAlarmsLock) {
            retaiendAlarms.Add(targetNode.NodeId);
            UpdateRetainedAlarmsCount();

            if (!RotationRunning)
                Initialize();
        }
    }

    public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId) {
        lock (retainedAlarmsLock) {
            var alarmIndex = retaiendAlarms.IndexOf(targetNode.NodeId);
            if (alarmIndex == -1) {
                Log.Error($"The alarm {targetNode.NodeId} was not fond in the alarm banner list");
                return;
            }

            if (alarmIndex < currentIndex)
                SetCurrentIndex(currentIndex - 1);

            retaiendAlarms.RemoveAt(alarmIndex);

            if (currentIndex == retaiendAlarms.Count)
                SetCurrentIndex(0);

            UpdateRetainedAlarmsCount();

            if (retaiendAlarms.Count == 0) {
                StopRotation();
            } else if (CurrentDisplayedAlarmNodeId == targetNode.NodeId) {
                UpdateCurrentDisplayedAlarm();
                StopRotation();
                StartRotation();
            }

        }
    }

    private void RotationTime_VariableChange(object sender, VariableChangeEventArgs e) {
        var wasRunning = RotationRunning;
        StopRotation();
        rotationTask = new PeriodicTask(DisplayNextAlarm, e.NewValue, logicNode);
        if (wasRunning)
            StartRotation();
    }

    public void Initialize() {
        SetCurrentIndex(0);
        UpdateCurrentDisplayedAlarm();
        if (RotationRunning)
            StopRotation();
        StartRotation();
    }

    public void Reset() {
        SetCurrentIndex(0);
        UpdateCurrentDisplayedAlarm();
        StopRotation();
    }

    public bool RotationRunning { get; private set; }
    public NodeId CurrentDisplayedAlarmNodeId {
        get { return currentDisplayedAlarm.Value; }
    }

    public void OnNextAlarmClicked() {
        lock (retainedAlarmsLock) {
            RestartRotation();
            DisplayNextAlarm();
        }
    }

    public void OnPreviousAlarmClicked() {
        lock (retainedAlarmsLock) {
            RestartRotation();
            DisplayPreviousAlarm();
        }
    }

    private void StopRotation() {
        if (!RotationRunning)
            return;

        rotationTask.Cancel();
        RotationRunning = false;
        skipFirstCallBack = false;
    }

    private void StartRotation() {
        if (RotationRunning)
            return;

        rotationTask.Start();
        RotationRunning = true;
        skipFirstCallBack = true;
    }

    private void RestartRotation() {
        StopRotation();
        StartRotation();
    }

    private void DisplayPreviousAlarm() {
        var previousIndex = currentIndex - 1 < 0 ? retaiendAlarms.Count - 1 : currentIndex - 1;
        SetCurrentIndex(previousIndex);
        UpdateCurrentDisplayedAlarm();
    }

    private void DisplayNextAlarm() {
        if (skipFirstCallBack) {
            skipFirstCallBack = false;
            return;
        }

        var nextIndex = currentIndex + 1 >= retaiendAlarms.Count ? 0 : currentIndex + 1;
        SetCurrentIndex(nextIndex);
        UpdateCurrentDisplayedAlarm();
    }

    private void SetCurrentIndex(int index) {
        currentIndex = index;
    }

    private void UpdateCurrentDisplayedAlarm() {
        if (retaiendAlarms.Count == 0) {
            currentDisplayedAlarm.Value = NodeId.Empty;
            currentDisplayedAlarmIndex.Value = 0;
            return;
        }

        currentDisplayedAlarmIndex.Value = currentIndex;
        currentDisplayedAlarm.Value = retaiendAlarms[currentIndex];
    }

    private void UpdateRetainedAlarmsCount() {
        retainedAlarmsCount.Value = retaiendAlarms.Count;
    }

    private void InitializeRetainedAlarmList(IUANode localizedAlarmsContainer) {
        foreach (var localizedAlarm in localizedAlarmsContainer.Children)
            retaiendAlarms.Add(localizedAlarm.NodeId);
    }

    private PeriodicTask rotationTask;
    private IUAVariable currentDisplayedAlarm;
    private IUAVariable currentDisplayedAlarmIndex;
    private IUAVariable rotationTime;
    private IUAVariable retainedAlarmsCount;
    private IUANode logicNode;
    private bool skipFirstCallBack = false;
    private int currentIndex = 0;
    private List<NodeId> retaiendAlarms;
    private Object retainedAlarmsLock;

    #region IDisposable Support
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if (disposedValue)
            return;

        if (disposing) {
            Reset();
            rotationTask.Dispose();
        }

        disposedValue = true;
    }

    public void Dispose() {
        Dispose(true);
    }
    #endregion
}
