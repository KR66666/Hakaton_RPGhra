using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private Text themedPotionCount;
    private Image themedWeaponIcon;
    private readonly System.Collections.Generic.List<RectTransform> combatWidgets=new System.Collections.Generic.List<RectTransform>();
    private void BuildThemedHud()
    {
        // Health is already shown in the status panel; avoid a second set of hearts.
        for(int i=0;i<2;i++)
        {
            var slot=Panel(canvas,i==0?"Meč • J":"Lektvar • H",new Vector2(20+i*185,-270),new Vector2(136,64),new Vector2(0,1));
            combatWidgets.Add(slot);
            slot.SetSiblingIndex(modal.GetSiblingIndex());
            var icon=Rect(slot,"Výbava",new Vector2(7,-8),new Vector2(46,46));
            var picture=icon.gameObject.AddComponent<Image>();picture.sprite=RpgVisualTheme.Icon(i==0?"sword":"potion");picture.raycastTarget=false;
            var caption=Label(slot,i==0?"J\nSouboj":"H\nLektvar",new Vector2(56,-9),new Vector2(72,46),17,RpgVisualTheme.Cream);
            caption.alignment=TextAnchor.MiddleCenter;
            if(i==0)themedWeaponIcon=picture;
            if(i==1)themedPotionCount=caption;
        }
    }
    private void UpdateThemedHud()
    {
        if(themedWeaponIcon!=null)themedWeaponIcon.sprite=RpgVisualTheme.Icon(game.State.hasIronSword?"sword":"fist");
        if(themedPotionCount!=null)themedPotionCount.text="H\nLektvar ×"+game.State.potions;
    }
}