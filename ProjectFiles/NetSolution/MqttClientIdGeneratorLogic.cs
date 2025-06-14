#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.Core;
using FTOptix.MQTTClient;
using System.Linq;
using FTOptix.MQTTBroker;
#endregion

public class MqttClientIdGeneratorLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var mqttClient = Project.Current.Get<MQTTClient>("MQTT/Mosquitto_MQTTClient");
        if (mqttClient.ClientId == "FTOptix-1")
        {
            mqttClient.ClientId = $"OptixUser-{Guid.NewGuid().ToString().Split("-")[0]}";
            mqttClient.Stop();
            mqttClient.Start();
            Log.Info("ClientIdGeneratorLogic", $"ClientId was set to {mqttClient.ClientId}");
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
