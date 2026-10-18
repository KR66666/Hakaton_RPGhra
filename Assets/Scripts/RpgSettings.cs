using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class RpgSettings : MonoBehaviour
{
    private static RpgSettings instance;
    private static int closedFrame=-1;
    public static bool CapturesInput => instance!=null && (instance.panel!=null && instance.panel.activeSelf || closedFrame==Time.frameCount);
    private GameObject panel;
    private AudioSource musicSource,effectsSource;
    private AudioClip click;
    private float master,music,effects;
    private bool mute;
    private int defaultQuality;
    private bool defaultFullscreen;
    private GameObject returnSelection;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        instance=null;closedFrame=-1;
        var obj=new GameObject("Persistent settings and audio");DontDestroyOnLoad(obj);instance=obj.AddComponent<RpgSettings>();
        instance.Load();instance.CreateAudio();instance.Apply();
    }
    private void Load()
    {
        defaultQuality=QualitySettings.GetQualityLevel();defaultFullscreen=Screen.fullScreen;
        master=Mathf.Clamp01(PlayerPrefs.GetFloat("rpg.volume.master",.7f));
        music=Mathf.Clamp01(PlayerPrefs.GetFloat("rpg.volume.music",.3f));
        effects=Mathf.Clamp01(PlayerPrefs.GetFloat("rpg.volume.effects",.7f));
        mute=PlayerPrefs.GetInt("rpg.mute",0)==1;
        QualitySettings.vSyncCount=PlayerPrefs.GetInt("rpg.vsync",1);
        int quality=PlayerPrefs.GetInt("rpg.quality",QualitySettings.GetQualityLevel());
        if(QualitySettings.names.Length>0)QualitySettings.SetQualityLevel(Mathf.Clamp(quality,0,QualitySettings.names.Length-1));
        if(PlayerPrefs.HasKey("rpg.fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("rpg.fullscreen")==1;
    }
    private void Save()
    {
        PlayerPrefs.SetFloat("rpg.volume.master",master);PlayerPrefs.SetFloat("rpg.volume.music",music);PlayerPrefs.SetFloat("rpg.volume.effects",effects);
        PlayerPrefs.SetInt("rpg.mute",mute?1:0);PlayerPrefs.SetInt("rpg.fullscreen",Screen.fullScreen?1:0);
        PlayerPrefs.SetInt("rpg.vsync",QualitySettings.vSyncCount);PlayerPrefs.SetInt("rpg.quality",QualitySettings.GetQualityLevel());PlayerPrefs.Save();Apply();
    }
    private void Apply()
    {
        AudioListener.volume=mute?0:master;
        if(musicSource!=null)musicSource.volume=music;
        if(effectsSource!=null)effectsSource.volume=effects;
    }
    private void CreateAudio()
    {
        musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.spatialBlend=0;
        effectsSource=gameObject.AddComponent<AudioSource>();effectsSource.spatialBlend=0;
        const int rate=22050;var samples=new float[rate*12];
        int[] notes={57,60,64,62,57,55,52,55,57,60,64,67,64,62,60,57};
        for(int i=0;i<samples.Length;i++)
        {
            float t=(float)i/rate;int beat=(int)(t/.75f);float age=t-beat*.75f;
            float hz=440*Mathf.Pow(2,(notes[beat%notes.Length]-69)/12f);
            float envelope=Mathf.Min(age*60,1)*Mathf.Exp(-age*5);
            samples[i]=.12f*envelope*(Mathf.Sin(2*Mathf.PI*hz*age)+.25f*Mathf.Sin(4*Mathf.PI*hz*age));
        }
        var tune=AudioClip.Create("Quiet medieval plucked melody",samples.Length,1,rate,false);tune.SetData(samples,0);musicSource.clip=tune;musicSource.Play();
        var sound=new float[rate/12];for(int i=0;i<sound.Length;i++){float t=(float)i/rate;sound[i]=.16f*Mathf.Sin(2*Mathf.PI*660*t)*Mathf.Exp(-t*55);}
        click=AudioClip.Create("UI click",sound.Length,1,rate,false);click.SetData(sound,0);
    }
    public static void PlayEffect(){if(instance!=null&&instance.effectsSource!=null&&instance.click!=null)instance.effectsSource.PlayOneShot(instance.click);}
    public static void Show()
    {
        if(instance==null)return;
        instance.returnSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        instance.Build();PlayEffect();
    }
    private void Update()
    {
        if(panel!=null&&panel.activeSelf&&UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)Close();
    }
    private void Close()
    {
        Save();panel.SetActive(false);closedFrame=Time.frameCount;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(returnSelection);
    }
    private void Build()
    {
        if(panel!=null){panel.SetActive(false);Destroy(panel);}
        panel=new GameObject("Settings overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));panel.transform.SetParent(transform,false);
        panel.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;panel.GetComponent<Canvas>().sortingOrder=200;
        var scaler=panel.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var shade=new GameObject("Shade",typeof(RectTransform),typeof(Image));shade.transform.SetParent(panel.transform,false);
        var r=shade.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;RpgVisualTheme.Panel(shade.GetComponent<Image>());
        MainMenu.Label("Title",panel.transform,"NASTAVENÍ",40,new Vector2(0,355),new Vector2(900,60));
        Volume("Celková hlasitost",270,master,v=>master=v);
        Volume("Hudba",175,music,v=>music=v);
        Volume("Zvukové efekty",80,effects,v=>effects=v);
        SettingButton("Zvuk: "+(mute?"ztlumený":"zapnutý"),-10,()=>{mute=!mute;Save();Build();});
        SettingButton("Celá obrazovka: "+(Screen.fullScreen?"ano":"ne"),-82,()=>{Screen.fullScreen=!Screen.fullScreen;Save();Build();});
        SettingButton("Kvalita: "+QualitySettings.names[QualitySettings.GetQualityLevel()],-154,()=>{QualitySettings.SetQualityLevel((QualitySettings.GetQualityLevel()+1)%QualitySettings.names.Length);Save();Build();});
        SettingButton("V-Sync: "+(QualitySettings.vSyncCount>0?"zapnutý":"vypnutý"),-226,()=>{QualitySettings.vSyncCount=QualitySettings.vSyncCount>0?0:1;Save();Build();});
        SettingButton("Rozhraní hry • skrýt panely",-287,BuildHudSettings);
        var reset=SettingButton("Obnovit výchozí",-350,()=>{master=.7f;music=.3f;effects=.7f;mute=false;QualitySettings.vSyncCount=1;QualitySettings.SetQualityLevel(defaultQuality);Screen.fullScreen=defaultFullscreen;Save();Build();});
        reset.GetComponent<RectTransform>().anchoredPosition=new Vector2(-240,-350);
        var close=SettingButton("Zpět • uložit",-350,Close);close.GetComponent<RectTransform>().anchoredPosition=new Vector2(240,-350);
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(close.gameObject);
    }
    private void BuildHudSettings()
    {
        Build();
        foreach(Transform child in panel.transform)
            if(child.name!="Shade"){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        MainMenu.Label("HUD title",panel.transform,"ZOBRAZENÍ HRY",36,new Vector2(0,310),new Vector2(1100,70));
        string[] sections={"controls","quest","navigation"};
        string[] names={"Nápověda + Souboj / Lektvar","Panel aktuálního úkolu","Tlačítka Mapa / Postava"};
        for(int i=0;i<sections.Length;i++)
        {
            string section=sections[i];
            SettingButton(names[i]+": "+(RpgHudPreferences.Visible(section)?"zobrazeno":"skryto"),160-i*100,()=>{
                RpgHudPreferences.SetVisible(section,!RpgHudPreferences.Visible(section));BuildHudSettings();
            });
        }
        MainMenu.Label("HUD keys",panel.transform,"F2 rychle skryje / zobrazí všechny tři panely.\nI otevře mobil s inventářem, Q otevře úkoly.\nM a C fungují i se skrytými tlačítky.",22,new Vector2(0,-160),new Vector2(1100,150));
        SettingButton("Zpět do nastavení",-320,Build);
    }
    private Button SettingButton(string title,float y,UnityEngine.Events.UnityAction action)
    {
        float width=y>-300?700:440;
        var button=MainMenu.Button(panel.transform,title,y,action);button.GetComponent<RectTransform>().sizeDelta=new Vector2(width,58);
        var text=button.GetComponentInChildren<Text>();text.fontSize=30;text.rectTransform.sizeDelta=new Vector2(width-20,54);return button;
    }
    private void Volume(string title,float y,float value,System.Action<float> changed)
    {
        var caption=MainMenu.Label(title,panel.transform,title+"  "+Mathf.RoundToInt(value*100)+" %",24,new Vector2(0,y+25),new Vector2(700,32));
        var root=new GameObject(title+" slider",typeof(RectTransform),typeof(Image),typeof(Slider));root.transform.SetParent(panel.transform,false);
        var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.sizeDelta=new Vector2(600,20);rect.anchoredPosition=new Vector2(0,y-14);root.GetComponent<Image>().color=new Color(.2f,.19f,.15f);
        var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(root.transform,false);var hr=handle.GetComponent<RectTransform>();hr.sizeDelta=new Vector2(24,32);handle.GetComponent<Image>().color=new Color(.87f,.7f,.38f);
        var slider=root.GetComponent<Slider>();slider.handleRect=hr;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=0;slider.maxValue=1;slider.value=value;
        slider.onValueChanged.AddListener(v=>{changed(v);caption.text=title+"  "+Mathf.RoundToInt(v*100)+" %";Save();});
    }
    public static Sprite GearSprite()
    {
        var t=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};var pixels=new Color32[256];
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)
        {float d=Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f));bool teeth=(x>=6&&x<=9||y>=6&&y<=9||Mathf.Abs(x-y)<=1||Mathf.Abs(x+y-15)<=1);if(d>=3&&d<=5.7f||d>=5&&d<=7.5f&&teeth)pixels[y*16+x]=new Color32(224,190,119,255);}
        t.SetPixels32(pixels);t.Apply();return Sprite.Create(t,new Rect(0,0,16,16),Vector2.one*.5f,16);
    }
}

