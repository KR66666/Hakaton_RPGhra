using System.Collections.Generic;
using UnityEngine;

// A single nearby name stays readable even when residents gather at the square.
public sealed class RpgNpcNameplate : MonoBehaviour
{
    private static readonly List<RpgNpcNameplate> plates=new List<RpgNpcNameplate>();
    private RpgGame game;
    private TextMesh label,shadow;
    private MeshRenderer ink,shadowInk;
    private string title;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => plates.Clear();
    private void OnEnable(){if(!plates.Contains(this))plates.Add(this);}
    private void OnDisable(){plates.Remove(this);if(ink!=null)ink.enabled=false;if(shadowInk!=null)shadowInk.enabled=false;}
    public void Initialize(RpgGame owner,TextMesh text,string name)
    {
        game=owner;label=text;title=name;label.characterSize=.045f;label.color=RpgVisualTheme.Cream;
        ink=label.GetComponent<MeshRenderer>();
        var obj=new GameObject("Stín jména");obj.transform.SetParent(label.transform,false);obj.transform.localPosition=new Vector3(.025f,-.025f,.01f);
        shadow=obj.AddComponent<TextMesh>();shadow.font=label.font;shadow.fontSize=label.fontSize;shadow.characterSize=label.characterSize;
        shadow.anchor=label.anchor;shadow.alignment=label.alignment;shadow.text=title;shadow.color=new Color(0,0,0,.95f);
        shadowInk=shadow.GetComponent<MeshRenderer>();shadowInk.sharedMaterial=ink.sharedMaterial;shadowInk.sortingOrder=ink.sortingOrder-1;
    }
    private void LateUpdate()
    {
        if(game==null||label==null)return;
        float distance=((Vector2)(transform.position-game.Player.position)).sqrMagnitude;
        bool visible=!game.Interface.IsModal&&!game.Interface.InRoom&&distance<3.5f*3.5f;
        if(visible)foreach(var other in plates)
        {
            if(other==this||other.game!=game||other.label==null)continue;
            float otherDistance=((Vector2)(other.transform.position-game.Player.position)).sqrMagnitude;
            bool tied=Mathf.Approximately(otherDistance,distance);
            if((!tied&&otherDistance<distance)||(tied&&plates.IndexOf(other)<plates.IndexOf(this))){visible=false;break;}
        }
        label.text=title;label.transform.rotation=Quaternion.identity;
        label.transform.position=transform.position+Vector3.up*.85f;
        ink.enabled=visible;shadowInk.enabled=visible;
    }
}
