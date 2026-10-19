using System;

public sealed partial class RpgInterface
{
    private bool FirstMeeting(string id,string title,string kind,Action continueConversation)
    {
        if(!game.IsTestLocation && !game.State.NpcDefeated(id) && game.State.RecordArchiveNpc(id))game.Save(false);
        if(string.IsNullOrEmpty(id) || game.State.HasMetNpc(id) || game.State.NpcDefeated(id))return false;
        ShowMeeting(id,title,kind,continueConversation,0);return true;
    }
    private void ShowMeeting(string id,string title,string kind,Action continueConversation,int step)
    {
        var state=game.State;
        if(step==0)
        {
            Open(kind,"NEZNÁMÝ OBYVATEL","Ty nejsi zdejší, viď? Myslím, že se vidíme poprvé. Co tě přivádí do Havranova?");
            Action("Představit se",()=>ShowMeeting(id,title,kind,continueConversation,1));
        }
        else if(step==1)
        {
            string arrival=state.cycle>1?
                "Mám pocit, že už jsem tady jednou "+(state.heroIsBoy?"byl":"byla")+", ale všechno začalo znovu. Ty mě nepoznáváš?":
                "Nevím, jak jsem se sem "+(state.heroIsBoy?"dostal":"dostala")+". Tohle místo vůbec neznám. Všechno je tu jiné než doma.";
            Open("heroShopDialogue",state.HeroName.ToUpperInvariant(),"Jmenuji se "+state.HeroName+". "+arrival+" Můžeš mi říct, kdo jsi?");
            Action("Poslechnout si odpověď",()=>ShowMeeting(id,title,kind,continueConversation,2));
        }
        else
        {
            string introduction=id=="guard"?"Jsem Petr Kříž. Pomáhám pocestným najít cestu. Tebe ale vidím poprvé. Jestli se tu cítíš jako ve snu, pověz mi o tom víc.":
                id=="elder"?"Říkají mi děda Matěj. Žiju tady už dlouho, ale tebe neznám. Když budeš něco hledat, možná ti poradím.":
                id=="healer"?"Jsem Mira, zdejší bylinkářka. Neměj strach, cizinče. S lidmi, kteří se ocitnou daleko od domova, mám soucit.":
                id=="mayor"?"Jsem Radovan, rychtář Havranova. Nepamatuji si, že bych tě tu někdy viděl. Dodržuj zdejší pořádek a můžeš se tu porozhlédnout.":
                id=="librarian"?"Jsem Agáta, zdejší knihovnice. Kromě knih uchovávám i staré mapy. Jestli se chystáš do lesa, mohu ti poradit bezpečnou cestu.":
                id=="merchant"?"Jsem Eliška a tohle je můj obchod. Dnes se vidíme poprvé. Jestli hledáš zásoby nebo radu, řekni, co potřebuješ.":
                "Lidé mi říkají "+title+". Žiju tady v Havranově. Tvoje tvář mi není povědomá, ale můžeme se seznámit. Co potřebuješ?";
            Open(kind,title.ToUpperInvariant(),introduction);
            Action("Pokračovat v rozhovoru",()=>{state.MeetNpc(id);game.Save(false);continueConversation();});
        }
        NpcChallenge(id);
        Action("Zatím se rozloučit",Close);
    }
}
