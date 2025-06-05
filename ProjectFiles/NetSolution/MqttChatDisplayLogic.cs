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
#endregion

public class MqttChatDisplayLogic : BaseNetLogic
{
    public override void Start()
    {
        // Get the scroll view to display the messages
        scrollView = Owner.Get<ScrollView>("Chat/MessagesScrollView");

        // Insert code to be executed when the user-defined logic is started
        mqttIncomingMessagePile = Project.Current.GetVariable("Model/Data/MQTT/MessagesHistory");
        mqttIncomingMessagePile.VariableChange += MqttIncomingMessagePile_VariableChanged;

        // Invoke the method to display existing messages
        MqttIncomingMessagePile_VariableChanged(null, null);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        mqttIncomingMessagePile.VariableChange -= MqttIncomingMessagePile_VariableChanged;
    }

    private void MqttIncomingMessagePile_VariableChanged(object sender, VariableChangeEventArgs e)
    {
        try
        {
            // Get my client ID
            string myClientId = Project.Current.GetVariable("MQTT/MQTTClient/ClientId").Value;
            // Create a new column layout to display the incoming messages
            var newColumnLayout = InformationModel.Make<ColumnLayout>("VerticalLayout");
            newColumnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
            newColumnLayout.VerticalAlignment = VerticalAlignment.Top;
            newColumnLayout.Height = -1;
            newColumnLayout.TopMargin = 8;
            newColumnLayout.LeftMargin = 8;
            newColumnLayout.RightMargin = 8;
            newColumnLayout.VerticalGap = 16;
            // Create a new text block for each incoming message
            var messagesPile = (string[])mqttIncomingMessagePile.Value.Value;
            foreach (var message in messagesPile ) 
            {
                if (string.IsNullOrEmpty(message))
                    continue;
                var textBlock = InformationModel.Make<MqttChatMessage>(Guid.NewGuid().ToString());
                // Deserialize the message
                var mqttMessage = MqttChatLogic.FeaturesDemo2MqttMessage.Deserialize(message);
                if (mqttMessage == null)
                    continue;
                textBlock.GetVariable("Sender").Value = mqttMessage.Sender;
                textBlock.GetVariable("Content").Value = mqttMessage.Content;
                textBlock.GetVariable("Timestamp").Value = mqttMessage.Timestamp;
                var isSentMessage = mqttMessage.Sender == myClientId;
                textBlock.GetVariable("Sent").Value = isSentMessage;
                // Add the text block to the column layout
                textBlock.HorizontalAlignment = isSentMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                textBlock.VerticalAlignment = VerticalAlignment.Top;
                newColumnLayout.Add(textBlock);
            }
            // Remove the existing content of the display
            scrollView.Get("VerticalLayout")?.Delete();
            scrollView.Add(newColumnLayout);
        }
        catch (Exception ex)
        {
            Log.Warning("MqttChatDisplayLogic", $"Error processing incoming MQTT messages: {ex.Message}");
        }
    }

    [ExportMethod]
    public void SendMessage(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            Log.Warning("MqttChatDisplayLogic", "Cannot send an empty message.");
            return;
        }

        // Get my client ID
        string myClientId = Project.Current.GetVariable("MQTT/MQTTClient/ClientId").Value;
        // Create a new message
        var dateTime = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        var mqttMessage = new MqttChatLogic.FeaturesDemo2MqttMessage(myClientId, content, dateTime);
        // Load the MQTT configuration
        var mqttPublisher = Project.Current.Get<MQTTPublisher>("MQTT/MQTTClient/MQTTPublisher");
        var mqttConfig = new MqttChatLogic.OptixMqttConfig(mqttPublisher);
        var mqttConfigJson = mqttConfig.Serialize();
        // Publish the message to the MQTT topic
        var mqttClient = Project.Current.Get<MQTTClient>("MQTT/MQTTClient");
        var mqttMessageJson = mqttMessage.Serialize();
        mqttClient.Publish(mqttConfigJson, mqttMessageJson);
    }

    private IUAVariable mqttIncomingMessagePile;
    private ScrollView scrollView;
}

