using UnityEngine;

public sealed partial class RpgGame
{
    private void BuildLibrarian()
    {
        if(State.place==RpgPlace.Library)AddPerson("librarian","Knihovnice Agáta","librarian",new Vector2(0,6.3f),Color.white);
    }
    public void LearnForestRoute()
    {
        if(State.NpcDefeated("librarian"))return;
        State.forestRouteKnown=true;Save(false);Interface.Close();
        Notify("Bezpečná cesta zapamatována. V lese tě ukazatel povede ke svíčce po stezce mimo nepřátele.",8);
    }
}
public sealed partial class RpgInterface
{
    public void ShowLibrarian()
    {
        if(FirstMeeting("librarian","Knihovnice Agáta","librarian",ShowLibrarian))return;
        var s=game.State;
        Open("librarian","KNIHOVNICE AGÁTA",
            "Mám tu starou mapu lesa. Ke svíčce se dá dojít i bez souboje.\n\n"+
            "Od jihozápadního vstupu sleduj spodní stezku až k rozcestníku. Pokračuj doprava, pod velkým balvanem. Obejdi ho zleva, neodbočuj na sever k vlkům. Pak se drž východní cesty a přejdi most. Svíčka leží hned na druhém břehu.\n\n"+
            "Za mostem neodbočuj na jih. Když se budeš držet této cesty, mineš i netopýry. Řeku překračuj jen po mostě."+
            (!s.ForestUnlocked?"\n\nCestu do lesa teď ještě neznáš. Mou radu si můžeš zapamatovat na později.":""));
        Action(s.forestRouteKnown?"Připomenout bezpečnou cestu":"Zapamatovat si bezpečnou cestu",game.LearnForestRoute);
        NpcChallenge("librarian");Action("Odejít",Close);
    }
}
