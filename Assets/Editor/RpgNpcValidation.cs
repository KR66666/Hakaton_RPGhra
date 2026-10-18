using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RpgNpcValidation
{
    public static int CheckSpacing()
    {
        int checks=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        check(!RpgNpcSpacing.CanStep(new Vector2(-1,0),new Vector2(-.4f,0),Vector2.zero),"Residents stop before overlapping");
        check(!RpgNpcSpacing.CanStep(new Vector2(-3,0),new Vector2(3,0),Vector2.zero),"Long frames cannot pass through a resident");
        check(RpgNpcSpacing.CanStep(new Vector2(-2,-1),new Vector2(2,-1),Vector2.zero),"Residents can pass alongside one another");
        check(RpgNpcSpacing.CanStep(new Vector2(.2f,0),new Vector2(.3f,0),Vector2.zero),"Existing overlaps can separate");
        check(!RpgNpcSpacing.CanStep(new Vector2(.2f,0),new Vector2(.1f,0),Vector2.zero),"Existing overlaps cannot get deeper");
        check(RpgNpcSpacing.CanStep(Vector2.zero,new Vector2(.1f,0),Vector2.zero),"Residents at the same point can start separating");
        check(RpgNpcSpacing.CanStep(new Vector2(2,0),new Vector2(2,0),Vector2.zero),"Waiting away from another actor stays valid");
        check(RpgNpcSpacing.CanStep(new Vector2(-2,1),new Vector2(-1,1),Vector2.zero),"Normal unobstructed walking stays available");
        return checks;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check NPC Rendering And Spacing")]
    public static void Run()
    {
        int count=CheckSpacing();
        foreach(string role in new[]{"mayor","healer","guard","villager","merchant"})
        {
            var first=MedievalNpcAppearance.Get(role);var second=MedievalNpcAppearance.Get(role);
            if(first!=second||first.texture.filterMode!=FilterMode.Point||first.texture.wrapMode!=TextureWrapMode.Clamp||first.texture.mipmapCount!=1)
                throw new Exception("NPC sprite must retain its cached native artwork: "+role);
            count++;
        }
        Debug.Log("PASS: "+count+" NPC sprite and spacing checks.");
    }
#endif
}
