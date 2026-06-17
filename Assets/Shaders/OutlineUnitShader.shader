Shader "Custom/SpriteOutlineSwitch"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.01
        [MaterialToggle] _OutlineEnabled("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _OutlineColor;
            float _OutlineWidth, _OutlineEnabled;

            v2f vert(appdata_full v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // 스위치가 꺼져있거나 투명도 있는 경우 즉시 반환
                if (_OutlineEnabled <= 0.5) return col;

                float2 off = _MainTex_TexelSize.xy * _OutlineWidth * 100;
                float alpha = tex2D(_MainTex, i.uv + float2(off.x, 0)).a +
                              tex2D(_MainTex, i.uv - float2(off.x, 0)).a +
                              tex2D(_MainTex, i.uv + float2(0, off.y)).a +
                              tex2D(_MainTex, i.uv - float2(0, off.y)).a;

                if (col.a < 0.1 && alpha > 0.1) return _OutlineColor;
                return col;
            }
            ENDCG
        }
    }
}