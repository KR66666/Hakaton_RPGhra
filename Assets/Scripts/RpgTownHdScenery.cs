using UnityEngine;

// Independent high-resolution quadrants preserve the original map's collision coordinates.
public sealed class RpgTownHdScenery : MonoBehaviour
{
    private GameObject layer;
    private static Material tileMaterial;
    public void Initialize(SpriteRenderer original)
    {
        if(layer!=null||original==null||original.sprite==null)return;
        string[] names={"NW","NE","SW","SE"};var sprites=new Sprite[4];
        for(int i=0;i<4;i++){sprites[i]=Resources.Load<Sprite>("TownHD/"+names[i]);if(sprites[i]==null)return;}
        layer=new GameObject("Detailní vesnice • čtyři HD části");layer.transform.SetParent(original.transform,false);
        var size=original.sprite.bounds.size;
        var shader=Resources.Load<Shader>("Shaders/TownHdTile");
        if(tileMaterial==null&&shader!=null)tileMaterial=new Material(shader);
        for(int i=0;i<4;i++)
        {
            var part=new GameObject(names[i]);part.transform.SetParent(layer.transform,false);
            part.transform.localPosition=new Vector3((i%2==0?-1:1)*size.x/4,(i<2?1:-1)*size.y/4,-.001f);
            part.transform.localScale=new Vector3(size.x*.5f/sprites[i].bounds.size.x,size.y*.5f/sprites[i].bounds.size.y,1);
            var art=part.AddComponent<SpriteRenderer>();art.sprite=sprites[i];art.sortingLayerID=original.sortingLayerID;art.sortingOrder=original.sortingOrder+1;
            if(tileMaterial!=null)art.sharedMaterial=tileMaterial;
            art.sprite.texture.filterMode=FilterMode.Bilinear;
        }
    }
    public void Show(bool visible){if(layer!=null)layer.SetActive(visible);}
}
