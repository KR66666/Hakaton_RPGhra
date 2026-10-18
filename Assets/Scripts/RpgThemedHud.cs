using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private readonly List<Image> themedHearts=new List<Image>();
    private Text themedPotionCount;
    private void BuildThemedHud()
    {
        var hearts=Panel(canvas,"Srdíčka zdraví",new Vector2(-20,-20),new Vector2(370,58),new Vector2(1,1));
        hearts.SetSiblingIndex(modal.GetSiblingIndex());
        for(int i=0;i<10;i++)
        {
            var empty=Rect(hearts,"Prázdné srdce",new Vector2(12+i*34,-3),new Vector2(36,50));
            var background=empty.gameObject.AddComponent<Image>();background.sprite=RpgVisualTheme.Icon("heart");background.color=new Color(.28f,.30f,.28f);background.raycastTarget=false;
            var full=Rect(empty,"Zdraví",Vector2.zero,new Vector2(36,50));
            var image=full.gameObject.AddComponent<Image>();image.sprite=RpgVisualTheme.Icon("heart");image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.raycastTarget=false;
            themedHearts.Add(image);
        }
        for(int i=0;i<2;i++)
        {
            var slot=Panel(canvas,i==0?"Meč • J":"Lektvar • H",new Vector2(20+i*90,-190),new Vector2(80,90),new Vector2(0,1));
            slot.SetSiblingIndex(modal.GetSiblingIndex());
            var icon=Rect(slot,"Pixelová ikona",new Vector2(9,-4),new Vector2(62,62));
            var picture=icon.gameObject.AddComponent<Image>();picture.sprite=RpgVisualTheme.Icon(i==0?"sword":"potion");picture.raycastTarget=false;
            var caption=Label(slot,i==0?"J • ÚTOK":"H • 0",new Vector2(5,-60),new Vector2(70,25),15,RpgVisualTheme.Cream);caption.alignment=TextAnchor.MiddleCenter;
            if(i==1)themedPotionCount=caption;
        }
    }
    private void UpdateThemedHud()
    {
        float units=Mathf.Clamp01(game.State.health/game.State.MaxHealth)*themedHearts.Count;
        for(int i=0;i<themedHearts.Count;i++)themedHearts[i].fillAmount=Mathf.Clamp01(units-i);
        if(themedPotionCount!=null)themedPotionCount.text="H • "+game.State.potions;
    }
}
