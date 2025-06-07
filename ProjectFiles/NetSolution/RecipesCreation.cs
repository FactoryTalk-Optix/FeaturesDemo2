#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using System;
using System.Threading;
using UAManagedCore;
using FTOptix.MQTTClient;
using FTOptix.MQTTBroker;
#endregion

public class RecipesCreation : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        recipesCreator = new LongRunningTask(CreateRecipes, LogicObject);
        recipesCreator.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        recipesCreator?.Dispose();
    }

    private void CreateRecipes()
    {
        // Check if we have any recipe in the store
        var myStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase");
        while (myStore.Status != StoreStatus.Online)
        {
            Thread.Sleep(500);
        }
        Object[,] ResultSet;
        String[] Header;
        myStore.Query("SELECT * FROM Recipes", out Header, out ResultSet);
        if (ResultSet.Length > 0)
            return;
        // Add recipes to the store
        var myTable = myStore.Tables.Get<Table>("Recipes");
        string[] columns = { "Name", "/Component1", "/Component2", "/Component3_0", "/Component3_1", "/Component3_2" };
        var values = new object[3, 6];
        values[0, 0] = "Recipe 1";
        values[0, 1] = 100;
        values[0, 2] = 1;
        values[0, 3] = 101;
        values[0, 4] = 102;
        values[0, 5] = 103;
        values[1, 0] = "Recipe 2";
        values[1, 1] = 200;
        values[1, 2] = 0;
        values[1, 3] = 201;
        values[1, 4] = 202;
        values[1, 5] = 203;
        values[2, 0] = "Recipe 3";
        values[2, 1] = 300;
        values[2, 2] = 1;
        values[2, 3] = 301;
        values[2, 4] = 302;
        values[2, 5] = 303;
        myTable.Insert(columns, values);
        Log.Debug("RecipesCreation.CreateRecipes", "Recipes creation completed");
        recipesCreator?.Dispose();
    }

    private LongRunningTask recipesCreator;
}
