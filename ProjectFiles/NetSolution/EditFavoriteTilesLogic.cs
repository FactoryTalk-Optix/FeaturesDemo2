#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Linq;
using UAManagedCore;
#endregion

public class EditFavoriteTilesLogic : BaseNetLogic {
    public override void Start() {
        // Loop per each favorite to check if this tile is marked as favorite
        foreach (var child in Session.User.Children.OfType<IUAObject>()) {
            if (child.GetVariable("Tile_Title").Value == ((LocalizedText)Owner.GetVariable("Tile_Title").Value).TextId) {
                Owner.GetVariable("Tile_Favorite").Value = child.GetVariable("Tile_Favorite").Value;
            }
        }
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void ToggleFavorite() {
        var myUser = Session.User;
        // Check if we have this tab in the favorites list
        foreach (var child in Session.User.Children.OfType<IUAObject>()) {
            if (child.GetVariable("Tile_Title").Value == ((LocalizedText)Owner.GetVariable("Tile_Title").Value).TextId) {
                if (child.GetVariable("Tile_Favorite").Value) {
                    child.Delete();
                    return;
                }
            }
        }
        // Create the new favorite object and add it to the user
        var newFav = InformationModel.Make<FavoriteTab>(NodeId.Random(1).ToString());
        newFav.GetVariable("Tile_Title").Value = ((LocalizedText)Owner.GetVariable("Tile_Title").Value).TextId;
        newFav.GetVariable("Tile_Subtitle").Value = ((LocalizedText)Owner.GetVariable("Tile_Subtitle").Value).TextId;
        newFav.GetVariable("Tile_Icon").Value = Owner.GetVariable("Tile_Icon").Value;
        newFav.GetVariable("Tile_Open_Panel").Value = Owner.GetVariable("Tile_Open_Panel").Value;
        newFav.GetVariable("Tile_Favorite").Value = Owner.GetVariable("Tile_Favorite").Value;
        newFav.GetVariable("Tile_Description").Value = Owner.GetVariable("Tile_Description").Value;
        myUser.Add(newFav);
    }
}
