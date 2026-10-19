using UnityEditor;
using UnityEngine;

public static class RpgShopPortraitPreview
{
    [MenuItem("Tools/RPG/Preview Henri Shop")]
    public static void Preview()
    {
        var game=RpgGame.Instance;
        if(game==null||!game.IsTestLocation){Debug.Log("Open the test location first; the story save remains untouched.");return;}
        if(Resources.Load<Sprite>("ShopInterior")==null||RpgPortraitFit.HenriShop()==null)
            throw new System.Exception("Shop background or Henri portrait is missing.");
        game.State.heroIsBoy=true;game.State.heroName="Henri";
        game.Interface.EnterRoom("merchant");
        typeof(RpgInterface).GetMethod("ShowShopDialogue",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(game.Interface,new object[]{1});
        Debug.Log("PASS: shop background and supplied Henri portrait loaded in test scene.");
    }
}
