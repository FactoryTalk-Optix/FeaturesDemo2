#region Using directives
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.HMIProject;
using FTOptix.MQTTClient;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using FTOptix.Report;
using FTOptix.Retentivity;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.UI;
using FTOptix.UI;
using FTOptix.WebUI;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class MqttChatLogic : BaseNetLogic
{
    public override void Start()
    {
        // Add observer to the MQTT incoming message variable
        mqttIncomingMessage = Project.Current.GetVariable("Model/Data/MQTT/IncomingMessage");
        mqttIncomingMessage.VariableChange += MqttIncomingMessagePile_VariableChanged;
        // Initialize the MQTT incoming message pile
        mqttIncomingMessagePile = Project.Current.GetVariable("Model/Data/MQTT/MessagesHistory");
    }

    private void MqttIncomingMessagePile_VariableChanged(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue == null)
            return;

        var incomingMessage = (string)e.NewValue;

        if (string.IsNullOrEmpty(incomingMessage))
            return;

        // Process the incoming MQTT message
        Log.Debug("MqttChatLogic", $"Received MQTT message: {incomingMessage}");

        // Add the messages to the circular pile
        var messagesPile = (string[]) mqttIncomingMessagePile.Value.Value;
        for (int i = messagesPile.Length - 1; i > 0; i--)
        {
            messagesPile[i] = messagesPile[i - 1];
        }
        messagesPile[0] = incomingMessage;
        mqttIncomingMessagePile.SetValue(messagesPile);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        mqttIncomingMessage.VariableChange -= MqttIncomingMessagePile_VariableChanged;
    }

    private IUAVariable mqttIncomingMessage;
    private IUAVariable mqttIncomingMessagePile;

    public class FeaturesDemo2MqttMessage
    {
        public string Sender { get; }
        public string Content { get; }
        public string Timestamp { get; }

        [JsonConstructor]
        public FeaturesDemo2MqttMessage(string sender, string content, string timestamp)
        {
            this.Sender = sender;
            this.Content = content;
            this.Timestamp = timestamp ?? new DateTime(0).ToString("yyyy/MM/dd HH:mm:ss");
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static FeaturesDemo2MqttMessage Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<FeaturesDemo2MqttMessage>(json);
            }
            catch (JsonException ex)
            {
                Log.Warning("MqttChatLogic", $"Failed to deserialize MQTT message: {ex.Message}, content: {json}");
                return null;
            }
        }
    }

    public class OptixMqttConfig
    {
        public string Topic { get; }
        public int QoS { get; }
        public bool Retain { get; }

        public OptixMqttConfig(MQTTPublisher mqttPublisher)
        {
            Topic = mqttPublisher.Topic;
            QoS = (int) mqttPublisher.QoS;
            Retain = mqttPublisher.Retain;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
