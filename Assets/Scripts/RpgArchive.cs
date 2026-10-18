using System.Collections.Generic;

public static class RpgArchive
{
    public static string PlaceKey(RpgPlace place) => "place:"+(int)place;
    public static List<string> Entries(RpgState state,int category)
    {
        var entries=new List<string>();if(state==null)return entries;
        if(category==0)
        {
            var people=new HashSet<string>(state.archiveNpcs);
            people.UnionWith(state.metNpcs);people.UnionWith(state.defeatedNpcs);
            foreach(string id in people)
            {
                string name=NpcName(id);if(name==null)continue;
                entries.Add(state.HasMetNpc(id) || state.NpcDefeated(id)?name+"\nObyvatel Havranova, kterého jsi potkal.":"NEZNÁMÝ OBYVATEL\nPotkal jsi tohoto obyvatele, ale ještě jste se nepředstavili.");
            }
        }
        else if(category==1)
        {
            if(state.archiveEnemies.Contains("wolf"))entries.Add("VLK\nPozemní protivník. Lze proti němu použít meč nebo luk.");
            if(state.archiveEnemies.Contains("flying"))entries.Add("NETOPÝR\nLétajícího protivníka lze zasáhnout lukem.");
        }
        else if(category==2)
        {
            foreach(var place in new[]{RpgPlace.Town,RpgPlace.Forest,RpgPlace.DarkForest,RpgPlace.Library,RpgPlace.ChurchRuins})
                if(state.archivePlaces.Contains(PlaceKey(place)))entries.Add(RpgLocations.Name(place).ToUpperInvariant()+"\nNavštívená lokace.");
            if(state.archivePlaces.Contains("shop"))entries.Add("OBCHOD\nNavštívený obchod v Havranově.");
            if(state.archivePlaces.Contains("cellar"))entries.Add("SKLEP OBCHODU\nNavštívený sklep pod obchodem.");
        }
        else if(category==3)
        {
            foreach(string id in state.archiveItems)
            {
                string entry=id=="sword"?"ŽELEZNÝ MEČ\nMeč zakoupený u kováře Mikuláše.":
                    id=="bow"?"LUK\nZbraň proti létajícím protivníkům.":
                    id=="armor"?"KOŽENÁ ZBROJ\nVýbava snižující příchozí zranění.":
                    id=="potion"?"LÉČIVÝ LEKTVAR\nObnoví 15 HP, nejvýše do maxima.":
                    id=="gold"?"ZLATO\nPeníze pro nákup zásob a vybavení.":
                    id=="candle"?"SVÍČKA\nZískaná svíčka pro rituál.":
                    id=="supply"?"ZTRACENÉ ZÁSOBY\nNalezené zásoby pro vesnici.":
                    id=="parcel"?"ZÁSILKA\nPřevzatý balíček určený k doručení.":null;
                if(entry!=null)entries.Add(entry);
            }
        }
        return entries;
    }
    private static string NpcName(string id)
    {
        if(id=="guard")return "PETR KŘÍŽ";
        if(id=="elder")return "DĚDA MATĚJ";
        if(id=="healer")return "BYLINKÁŘKA MIRA";
        if(id=="mayor")return "RYCHTÁŘ RADOVAN";
        if(id=="merchant")return "OBCHODNICE ELIŠKA";
        if(id!=null && id.StartsWith("district"))return id.Substring(8).ToUpperInvariant();
        if(id!=null && id.StartsWith("bandit") && int.TryParse(id.Substring(6),out int index))
        {
            string[] names={"PEKAŘ ONDŘEJ","TESAŘ JAKUB","SELKA ANNA","KUPEC VÁCLAV","MLYNÁŘ JAN","ŠVADLENA MARIE"};
            return names[(index%names.Length+names.Length)%names.Length];
        }
        return null;
    }
}
