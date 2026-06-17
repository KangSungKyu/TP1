Shader "Custom/URP_SpriteOutline"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_OutlineSize ("Outline Size (px)", Float) = 2.0
		_AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.02
		_EdgeScale ("Edge Strength", Float) = 2.0
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200

		Pass
		{
			Name "SobelOutline"
			Tags { "LightMode" = "UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Inspector-friendly texture slots
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			// Sprite texture provided by SpriteRenderer (atlas-safe)
			TEXTURE2D(unity_SpriteTexture);
			SAMPLER(sampler_unity_SpriteTexture);

			CBUFFER_START(UnityPerMaterial)
				float4 _Color;
				float4 _OutlineColor;
				float _OutlineSize;
				float _AlphaCutoff;
				float _EdgeScale;
			CBUFFER_END

			// Ensure texel size symbols exist
			float4 unity_SpriteTexture_TexelSize;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				o.color = v.color * _Color;
				return o;
			}

			// Single-pass: render sprite when opaque, else render outline based on Sobel on alpha
			half4 frag(Varyings IN) : SV_Target
			{
				float2 uv = IN.uv;

				// center sample from unity_SpriteTexture for atlas compatibility
				half4 center = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv);
				float centerA = center.a * IN.color.a;

				// draw sprite when center opaque
				if (centerA > _AlphaCutoff)
				{
					half4 col = center * IN.color;
					return col;
				}

				// texel size
				float2 texel = float2(unity_SpriteTexture_TexelSize.x, unity_SpriteTexture_TexelSize.y);
				if (texel.x <= 0.0) texel = float2(1.0,1.0);

				float2 d = texel * max(1.0, _OutlineSize);

				// sample alpha neighbors (unrolled)
				float a00 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(-d.x, -d.y)).a;
				float a10 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(0.0, -d.y)).a;
				float a20 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(d.x, -d.y)).a;

				float a01 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(-d.x, 0.0)).a;
				float a21 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(d.x, 0.0)).a;

				float a02 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(-d.x, d.y)).a;
				float a12 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(0.0, d.y)).a;
				float a22 = SAMPLE_TEXTURE2D(unity_SpriteTexture, sampler_unity_SpriteTexture, uv + float2(d.x, d.y)).a;

				// sobel on alpha
				float gx = (-a00 + a20) + (-2.0 * a01 + 2.0 * a21) + (-a02 + a22);
				float gy = (-a00 - 2.0 * a10 - a20) + (a02 + 2.0 * a12 + a22);
				float mag = abs(gx) + abs(gy);

				float edge = saturate(mag * _EdgeScale);

				// external-only: require center to be transparent
				float externalMask = saturate(1.0 - centerA);
				float strength = edge * externalMask;

				if (strength <= 0.001) return half4(0,0,0,0);

				half4 oc = _OutlineColor;
				oc.a *= strength * IN.color.a;
				return oc;
			}

			ENDHLSL
		}
	}
}
