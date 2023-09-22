#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Linq;
using UAManagedCore;
#endregion

public class FavoritesLogic : BaseNetLogic {
    public override void Start() {
        // Check if user already have some favorites
        var userChildren = Session.User.Children.OfType<IUAObject>();
        if (!userChildren.Any(t => t.GetVariable("Tile_Favorite").Value)) {
            //if (!userChildren.Any()) {
            //    // User has no favorite, generate random ones
            //    Log.Info("FavoritesLogic.Start", $"User {Session.User.BrowseName} has no favorites, assigning random tiles");
            //    RandomTiles();
            //} else {
            // User already have some favorites, do not generate more
            //Log.Debug("FavoritesLogic.Start", "Random tiles were already generated for this user");
            //}
            var myLabel = InformationModel.Make<FTOptix.UI.Label>("NoTiles");
            myLabel.HorizontalAlignment = HorizontalAlignment.Center;
            myLabel.VerticalAlignment = VerticalAlignment.Center;
            myLabel.LocalizedText = new LocalizedText(myLabel.NodeId.NamespaceIndex, "NoFavorites");
            myLabel.TextHorizontalAlignment = TextHorizontalAlignment.Center;
            myLabel.TextVerticalAlignment = TextVerticalAlignment.Center;
            Owner.Get("ScrollView").Delete();
            Owner.Add(myLabel);
            return;
        } else {
            // User have some favorite, delete random ones
            Log.Debug("FavoritesLogic.Start", "Removing random tiles from the current user");
            foreach (var defaultFav in userChildren.Where(t => !t.GetVariable("Tile_Favorite").Value)) {
                defaultFav.Delete();
            }
        }
        // Create tiles for this user
        TilesSetup();
    }

    public override void Stop() {
        // Insert code to be executed when the user-defined logic is stopped
    }

    //private void RandomTiles() {
    //    // Loop per each features category we have in the project
    //    Random rnd = new Random();
    //    uint featCounter = 0;
    //    foreach (var featuresFolder in Project.Current.Get("UI/Screens").Children.OfType<Folder>()) {
    //        // Loop per each feature of that category
    //        Log.Verbose1("FavoritesLogic.RandomTiles", "Exploring " + featuresFolder.BrowseName + " folder");
    //        foreach (var featureScreen in featuresFolder.Children.Where(t => !t.BrowseName.Contains("__"))) {
    //            // Randomly pick new features
    //            Log.Verbose1("FavoritesLogic.RandomTiles", "Exploring " + featuresFolder.BrowseName + " feature");
    //            if (rnd.Next(0, 4) == 2) {
    //                Log.Debug("FavoritesLogic.RandomTiles", "Randomly adding " + featureScreen.BrowseName + " to favorites");
    //                ++featCounter;
    //                var newFav = InformationModel.Make<FavoriteTab>("RndFeature" + featCounter.ToString());
    //                newFav.GetVariable("Tile_Favorite").Value = false;
    //                newFav.GetVariable("Tile_Title").Value = featureScreen.BrowseName;
    //                newFav.GetVariable("Tile_Subtitle").Value = featuresFolder.BrowseName;
    //                newFav.GetVariable("Tile_Icon").Value = new ResourceUri("%PROJECTDIR%\\imgs\\Logos\\LogoFTOptixDarkGrey.svg").Uri;
    //                newFav.GetVariable("Tile_Open_Panel").Value = featureScreen.NodeId;
    //                Session.User.Add(newFav);
    //            } else {
    //                Log.Debug("FavoritesLogic.RandomTiles", "Skipping " + featureScreen.BrowseName);
    //            }
    //        }
    //    }
    //}

    private void TilesSetup() {
        Log.Debug("FavoritesLogic.TilesSetup", "Populating favorites page");
        var myUser = Session.User;
        var scrollViewRows = Owner.Get("ScrollView/Rows");
        // Delete all RowLayout in page (if any)
        foreach (var child in scrollViewRows.Children) {
            child.Delete();
        }
        int piecesCounter = 0;
        int rowsCounter = 0;
        // Generate all the tiles
        foreach (var child in myUser.Children.OfType<IUAObject>()) {
            if (piecesCounter % 4 == 0) {
                // Every four pieces we generate a new row
                rowsCounter++;
                var newRow = InformationModel.Make<RowLayout>($"Row{rowsCounter}");
                newRow.HorizontalAlignment = HorizontalAlignment.Stretch;
                newRow.VerticalAlignment = VerticalAlignment.Top;
                newRow.Height = -1;
                newRow.LeftMargin = 16;
                newRow.TopMargin = 16;
                scrollViewRows.Add(newRow);
            }
            // Add the new tile
            piecesCounter++;
            var newTile = InformationModel.Make<FeatureTileObject>($"Tile{piecesCounter}");
            newTile.GetVariable("Tile_Favorite").Value = child.GetVariable("Tile_Favorite").Value;
            newTile.GetVariable("Tile_Title").Value = new LocalizedText(newTile.NodeId.NamespaceIndex, child.GetVariable("Tile_Title").Value);
            newTile.GetVariable("Tile_Subtitle").Value = new LocalizedText(newTile.NodeId.NamespaceIndex, child.GetVariable("Tile_Subtitle").Value);
            newTile.GetVariable("Tile_Icon").Value = child.GetVariable("Tile_Icon").Value;
            newTile.GetVariable("Tile_Open_Panel").Value = child.GetVariable("Tile_Open_Panel").Value;
            newTile.GetVariable("Tile_Description").Value = new LocalizedText(newTile.NodeId.NamespaceIndex, child.GetVariable("Tile_Description").Value);

            scrollViewRows.Get($"Row{rowsCounter}").Add(newTile);
        }
    }
}
