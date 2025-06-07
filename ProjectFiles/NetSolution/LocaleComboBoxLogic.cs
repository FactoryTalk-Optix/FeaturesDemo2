#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class LocaleComboBoxLogic : BaseNetLogic
{
    public override void Start()
    {
        var localeCombo = (ComboBox)Owner;

        string[] projectLocales = Project.Current.Localization.Locales;
        var modelLocales = InformationModel.MakeObject("Locales");
        modelLocales.Children.Clear();

        foreach (string locale in projectLocales)
        {
            var language = InformationModel.MakeVariable(locale, OpcUa.DataTypes.String);
            language.Value = locale;
            modelLocales.Add(language);
        }

        LogicObject.Add(modelLocales);
        localeCombo.Model = modelLocales.NodeId;
    }
}
