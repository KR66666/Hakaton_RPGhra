using UnityEngine;

// Threshold centres traced on the active MedievalTownHD 1536 x 1024 artwork.
// A door may be inside the building footprint; only its approach must be walkable.
public static class RpgWorldDoors
{
    public static readonly Vector2 Shop=new Vector2(659,347),ShopApproach=new Vector2(659,379);
    public static readonly Vector2 Library=new Vector2(641,601),LibraryApproach=new Vector2(641,638);
    public static readonly Vector2 Church=new Vector2(1043,344),ChurchApproach=new Vector2(1043,383);
    public static readonly Vector2 ForestGate=new Vector2(745,12),ForestArrival=new Vector2(749,48);
    public static readonly Vector2 LibraryInside=new Vector2(0,-10.5f),LibraryInsideApproach=new Vector2(0,-9.4f);
    public static readonly Vector2 SecretPassage=new Vector2(11.4f,4.7f),SecretPassageApproach=new Vector2(11.4f,3.7f);
    public static readonly Vector2 SecretInside=new Vector2(0,-10.1f)*RpgLocations.SecretRoomScale;
    public static readonly Vector2 SecretInsideApproach=new Vector2(0,-8.4f)*RpgLocations.SecretRoomScale;
}
