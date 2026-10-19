using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RpgDialogueValidation
{
    public static int CheckLayout()
    {
        int checks=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        foreach(var kind in new[]{"guideInstruction","cellarGuide","guard","mayor","healer","merchant","shopDialogue","resident","librarian","entrance","sideQuest","cellarElder","cellarMerchant","cellarWalk","cellarRats","cellarEscape","cellarNote","cellarClue"})
            check(RpgDialogueLayout.IsConversation(kind),"Conversation must use the shared layout: "+kind);
        foreach(var kind in new[]{"inventory","character","map","journal","pause","cellarJournal"})
            check(!RpgDialogueLayout.IsConversation(kind),"Utility window must keep its own layout: "+kind);
        foreach(var rect in new[]{RpgDialogueLayout.Portrait,RpgDialogueLayout.Title,RpgDialogueLayout.Body,RpgDialogueLayout.Actions})
            check(rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=RpgDialogueLayout.Size.x&&rect.yMax<=RpgDialogueLayout.Size.y,"Dialogue section must fit inside its frame");
        check(!RpgDialogueLayout.Portrait.Overlaps(RpgDialogueLayout.Body),"Portrait must not overlap dialogue text");
        check(!RpgDialogueLayout.Title.Overlaps(RpgDialogueLayout.Body),"Title must not overlap dialogue text");
        check(!RpgDialogueLayout.Body.Overlaps(RpgDialogueLayout.Actions),"Text must not overlap buttons");
        var buttons=new List<Rect>();
        for(int i=0;i<6;i++)
        {
            var position=RpgDialogueLayout.ButtonPosition(i);
            var button=new Rect(position.x,-position.y,RpgDialogueLayout.ButtonWidth,RpgDialogueLayout.ButtonHeight);
            check(button.xMin>=0&&button.yMin>=0&&button.xMax<=RpgDialogueLayout.Actions.width&&button.yMax<=RpgDialogueLayout.Actions.height,"All six merchant choices must fit inside the action area");
            foreach(var previous in buttons)check(!button.Overlaps(previous),"Conversation buttons must not overlap");
            buttons.Add(button);
        }
        check(RpgDialogueLayout.Size.x<=1600-40&&RpgDialogueLayout.Size.y+22<900,"Conversation fits the CanvasScaler's minimum reference area");
        return checks;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Unified Dialogue Layout")]
    public static void Run() => Debug.Log("PASS: "+CheckLayout()+" shared dialogue layout checks.");
#endif
}
