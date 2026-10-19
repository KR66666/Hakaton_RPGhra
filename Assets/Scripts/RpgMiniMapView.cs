using UnityEngine;

public static class RpgMiniMapView
{
    public const float Zoom=2.5f;
    public static Rect View(Vector2 player)
    {
        float size=1/Zoom;
        return new Rect(Mathf.Clamp(player.x-size/2,0,1-size),Mathf.Clamp(player.y-size/2,0,1-size),size,size);
    }
    public static Vector2 Project(Vector2 point,Rect view) => new Vector2((point.x-view.xMin)/view.width,(point.y-view.yMin)/view.height);
}
