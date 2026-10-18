Shader "RPG/BrightPixelArt"
{
 Properties
 {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _BlockSize ("Pixel grid", Float) = 2
  _Terrain ("Terrain palette", Float) = 0
  _Natural ("Natural fantasy light", Float) = 0
  _StencilComp ("Stencil Comparison", Float) = 8
  _Stencil ("Stencil ID", Float) = 0
  _StencilOp ("Stencil Operation", Float) = 0
  _StencilWriteMask ("Stencil Write Mask", Float) = 255
  _StencilReadMask ("Stencil Read Mask", Float) = 255
  _ColorMask ("Color Mask", Float) = 15
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
   sampler2D _MainTex;float4 _MainTex_TexelSize;fixed4 _Color;float _BlockSize,_Terrain,_Natural;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    if(_Natural>.5)
    {
     fixed4 nativeColor=tex2D(_MainTex,i.uv);
     float3 art=nativeColor.rgb;
     #ifndef UNITY_COLORSPACE_GAMMA
     art=LinearToGammaSpace(art);
     #endif
     float light=dot(art,float3(.299,.587,.114));
     art=saturate(art*float3(.96,.98,1.06));
     art=lerp(light.xxx,art,.94);
     art=lerp(art,art*float3(.94,.98,1.09),saturate((.45-light)*2)*.4);
     #ifndef UNITY_COLORSPACE_GAMMA
     art=GammaToLinearSpace(art);
     #endif
     return fixed4(art,nativeColor.a)*i.color;
    }
    float2 grid=max(_MainTex_TexelSize.zw/max(_BlockSize,1),1);
    fixed4 original=tex2D(_MainTex,(floor(i.uv*grid)+.5)/grid);
    #ifndef UNITY_COLORSPACE_GAMMA
    original.rgb=LinearToGammaSpace(original.rgb);
    #endif
    float3 c=original.rgb;
    float l=dot(c,float3(.299,.587,.114));
    c=saturate(pow(max(c,0),.84));
    c=saturate(lerp(dot(c,float3(.299,.587,.114)).xxx,c,1.14));
    if(_Terrain>.5)
    {
     if(original.g>original.r*1.08 && original.g>original.b*1.05)
      c=lerp(c,float3(.28,.58,.12)+l*float3(.47,.46,.19),.58);
     else if(original.b>original.r*1.10 && original.g>original.r*1.10)
      c=lerp(c,float3(.08,.35,.43)+l*float3(.24,.46,.40),.46);
     else if(max(original.r,max(original.g,original.b))-min(original.r,min(original.g,original.b))<.17)
      c=lerp(c,float3(.24,.18,.10)+pow(l,.65)*float3(.80,.76,.53),.62);
     else c=saturate(c*float3(1.10,1.04,.91));
    }
    c=floor(saturate(c)*31+.5)/31;
    #ifndef UNITY_COLORSPACE_GAMMA
    c=GammaToLinearSpace(c);
    #endif
    return fixed4(c,original.a)*i.color;
   }
   ENDCG
  }
 }
}
