using UnityEngine;

// Expanded-map street junctions keep long patrol legs on the paved roads.
public static class RpgNpcPatrols
{
    public static Vector2[] Points(string role,string name)
    {
        if(role=="guard")return new[]{new Vector2(1060,434),new Vector2(1300,434),new Vector2(1330,460),new Vector2(1350,520),new Vector2(1340,570),new Vector2(1350,520),new Vector2(1330,460),new Vector2(1300,434)};
        if(role=="district")
        {
            if(name.Contains("Bohumil"))return new[]{new Vector2(145,180),new Vector2(190,210),new Vector2(255,215),new Vector2(320,240),new Vector2(350,300),new Vector2(350,350),new Vector2(350,434),new Vector2(350,350),new Vector2(350,300),new Vector2(320,240),new Vector2(255,215),new Vector2(190,210)};
            if(name.Contains("Kovář"))return new[]{new Vector2(140,430),new Vector2(180,480),new Vector2(345,434),new Vector2(350,434),new Vector2(355,520),new Vector2(350,580),new Vector2(355,520),new Vector2(350,434),new Vector2(345,434),new Vector2(180,480)};
            if(name.Contains("Mlynář"))return new[]{new Vector2(1380,185),new Vector2(1325,210),new Vector2(1325,405),new Vector2(1300,434),new Vector2(1325,405),new Vector2(1325,210)};
            if(name.Contains("Rybář"))return new[]{new Vector2(1340,570),new Vector2(1350,520),new Vector2(1330,460),new Vector2(1300,434),new Vector2(1330,460),new Vector2(1350,520),new Vector2(1340,570),new Vector2(1360,645)};
            if(name.Contains("Selka"))return new[]{new Vector2(250,640),new Vector2(250,570),new Vector2(230,520),new Vector2(180,480),new Vector2(230,520),new Vector2(250,570),new Vector2(250,640),new Vector2(215,700),new Vector2(170,760),new Vector2(215,700)};
            return new[]{new Vector2(780,810),new Vector2(790,860),new Vector2(790,980),new Vector2(745,1000),new Vector2(790,980),new Vector2(790,860)};
        }
        return new[]{new Vector2(780,434),new Vector2(1060,434),new Vector2(1060,675),new Vector2(780,675),new Vector2(525,675),new Vector2(525,434)};
    }
}
