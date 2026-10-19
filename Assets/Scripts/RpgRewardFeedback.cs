using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private RpgState rewardState;
    private int[] rewardAmounts;
    private readonly Queue<RewardNotice> rewardQueue=new Queue<RewardNotice>();
    private Coroutine rewardAnimation;
    private RectTransform rewardCard;
    private static readonly string[] rewardNames={"bod vylepšení","svíčka","léčivý lektvar","","zlata","zásoby"};
    private static readonly string[] rewardIcons={"upgrade","candle","potion","supply","gold","supply","sword","armor","supply","bow"};
    private sealed class RewardNotice
    {
        public string text,icon;
        public int lines;
        public bool points;
    }
    private void UpdateRewardFeedback()
    {
        var state=game.State;
        int[] amounts={state.UpgradePoints,state.candles,state.potions,0,state.gold,state.SuppliesFound,state.hasIronSword?state.weaponLevel+1:0,state.armorLevel,state.deliveryQuest==1&&!state.parcelDelivered?1:0,state.hasBow?1:0};
        // Loading, a fresh story, or a new cycle establishes a baseline rather
        // than announcing the restored inventory as newly earned rewards.
        if(!ReferenceEquals(rewardState,state))
        {
            rewardState=state;rewardAmounts=amounts;rewardQueue.Clear();
            if(rewardAnimation!=null){StopCoroutine(rewardAnimation);rewardAnimation=null;}
            if(rewardCard!=null){rewardCard.gameObject.SetActive(false);Destroy(rewardCard.gameObject);rewardCard=null;}
            return;
        }
        bool anyGain=false;
        for(int i=0;i<amounts.Length;i++)if(amounts[i]>rewardAmounts[i]){anyGain=true;break;}
        if(!anyGain){rewardAmounts=amounts;return;}
        var text=new StringBuilder();int lines=0;string icon=null;bool points=false;
        for(int i=0;i<amounts.Length;i++)
        {
            int gained=amounts[i]-rewardAmounts[i];if(gained<=0)continue;
            if(lines++>0)text.Append('\n');
            if(i<6)text.Append('+').Append(gained).Append(" • ").Append(rewardNames[i]);
            else if(i==9)text.Append("Luk získán");
            else if(i==8)text.Append("+1 • zásilka pro Petra");
            else if(i==6)text.Append(state.weaponLevel==0?"Železný meč získán":"Meč +"+state.weaponLevel);
            else text.Append("Zbroj +").Append(amounts[i]);
            if(icon==null)icon=rewardIcons[i];if(i==0)points=true;
        }
        rewardAmounts=amounts;
        if(lines>0)rewardQueue.Enqueue(new RewardNotice{text=text.ToString(),icon=icon,lines=lines,points=points});
        if(rewardAnimation==null && rewardQueue.Count>0)rewardAnimation=StartCoroutine(AnimateRewards());
    }
    private IEnumerator AnimateRewards()
    {
        while(rewardQueue.Count>0)
        {
            var notice=rewardQueue.Dequeue();float height=76+notice.lines*28;
            rewardCard=Panel(canvas,"Získaná odměna",Vector2.zero,new Vector2(470,height),new Vector2(.5f,1));
            rewardCard.SetAsLastSibling();rewardCard.pivot=new Vector2(.5f,1);
            var group=rewardCard.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            var accent=Rect(rewardCard,"Zlatá linka",new Vector2(0,-8),new Vector2(4,height-16));
            accent.gameObject.AddComponent<Image>().color=notice.points?new Color(.6f,.9f,.8f):Gold;
            var iconRect=Rect(rewardCard,"Předmět",new Vector2(20,-30),new Vector2(56,56));
            var picture=iconRect.gameObject.AddComponent<Image>();picture.sprite=RpgVisualTheme.Icon(notice.icon);picture.preserveAspect=true;
            Label(rewardCard,notice.points?"BOD VYLEPŠENÍ ZÍSKÁN":"ZÍSKÁNO",new Vector2(94,-17),new Vector2(358,32),24,Gold);
            Label(rewardCard,notice.text,new Vector2(94,-55),new Vector2(358,height-64),23,RpgVisualTheme.Cream);
            RpgSettings.PlayReward(notice.points);
            float elapsed=0;
            while(elapsed<2.2f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float enter=Mathf.Clamp01(elapsed/.22f),leave=Mathf.Clamp01((elapsed-1.55f)/.65f);
                float ease=1-Mathf.Pow(1-enter,3);
                rewardCard.anchoredPosition=new Vector2(0,Mathf.Lerp(-30,-58,ease)+leave*24);
                rewardCard.localScale=Vector3.one*Mathf.Lerp(.88f,1,ease);
                group.alpha=enter*(1-leave);yield return null;
            }
            rewardCard.gameObject.SetActive(false);Destroy(rewardCard.gameObject);rewardCard=null;
        }
        rewardAnimation=null;
    }
}
