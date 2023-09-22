#region Using directives
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Linq;
using UAManagedCore;
#endregion

public class CheckFavoriteTab : BaseNetLogic {
    public override void Start() {
        // Insert code to be executed when the user-defined logic is started
        var userChildren = Session.User.Children.OfType<IUAObject>();
        if (!userChildren.Any() && Owner.Get<NavigationPanel>("NavigationPanel").GetVariable("CurrentTabIndex").Value == 0) {
            // User has no favorite, generate random ones
            Log.Info("FavoritesLogic.Start", $"User {Session.User.BrowseName} has no favorites, moving to UI tab");
            Owner.Get<NavigationPanel>("NavigationPanel").ChangePanelByTabIndex(1);
        }
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
