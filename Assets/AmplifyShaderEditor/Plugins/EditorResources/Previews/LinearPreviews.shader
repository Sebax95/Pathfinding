Shader "Hidden/LinearMaterial"
{
	Properties
	{
		_MainTex( "Texture", any ) = "" {}
		_BackGround( "Back", 2D) = "white" {}
	}

	SubShader
	{
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off
		ZTest Always

		CGINCLUDE
		uniform float4x4 unity_GUIClipTextureMatrix;
		sampler2D _GUIClipTexture;

		Texture2D _MainTex;
		SamplerState sampler_MainTex;
		uniform float4 _MainTex_ST;

		SamplerState my_linear_clamp_sampler;

		uniform bool _ManualTex2SRGB;

		ENDCG


		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			float4 _Mask;


			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 clipUV : TEXCOORD1;
			};

			v2f vert( appdata_t v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.texcoord = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
				float3 eyePos = UnityObjectToViewPos( v.vertex );
				o.clipUV = mul( unity_GUIClipTextureMatrix, float4( eyePos.xy, 0, 1.0 ) );
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				float4 c = _MainTex.Sample( my_linear_clamp_sampler, i.texcoord );
				if (_ManualTex2SRGB) c.rgb = LinearToGammaSpace(c.rgb);
				c.rgb *= _Mask.rgb;

				c.a = tex2D( _GUIClipTexture, i.clipUV ).a;
				return saturate( c );
			}
			ENDCG
		}

		Pass { // sphere preview = true, alpha mask = false
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			float _InvertedZoom;
			float4 _Mask;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 clipUV : TEXCOORD1;
			};

			v2f vert( appdata_t v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.texcoord = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
				float3 eyePos = UnityObjectToViewPos( v.vertex );
				o.clipUV = mul( unity_GUIClipTextureMatrix, float4( eyePos.xy, 0, 1.0 ) );
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				float2 p = 2 * i.texcoord - 1;
				float r = sqrt( dot( p,p ) );

				float alpha = saturate( ( 1 - r )*( 45 * _InvertedZoom + 5 ) );

				float4 c = _MainTex.Sample( sampler_MainTex, i.texcoord );
				if (_ManualTex2SRGB) c.rgb = LinearToGammaSpace(c.rgb);

				c.rgb *= _Mask.rgb;
				c.rgb *= alpha;

				c.a = tex2D( _GUIClipTexture, i.clipUV ).a;
				return saturate( c );
			}
			ENDCG
		}

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			sampler2D _BackGround;
			uniform float4 _BackGround_ST;
			float _InvertedZoom;
			float4 _Mask;


			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 clipUV : TEXCOORD1;
			};

			v2f vert( appdata_t v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.texcoord = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
				float3 eyePos = UnityObjectToViewPos( v.vertex );
				o.clipUV = mul( unity_GUIClipTextureMatrix, float4( eyePos.xy, 0, 1.0 ) );
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				float3 back = tex2D( _BackGround, ( i.texcoord * 2 - 1 ) * _InvertedZoom).b;

				float4 c = _MainTex.Sample( sampler_MainTex, i.texcoord );
				if (_ManualTex2SRGB) c.rgb = LinearToGammaSpace(c.rgb);

				c.rgb *= _Mask.rgb;
				c.rgb = lerp( back, c.rgb, c.a );

				c.a = tex2D( _GUIClipTexture, i.clipUV ).a;
				return saturate( c );
			}
			ENDCG
		}

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			sampler2D _BackGround;
			uniform float4 _BackGround_ST;
			float _InvertedZoom;
			float4 _Mask;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 clipUV : TEXCOORD1;
			};

			v2f vert( appdata_t v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.texcoord = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
				float3 eyePos = UnityObjectToViewPos( v.vertex );
				o.clipUV = mul( unity_GUIClipTextureMatrix, float4( eyePos.xy, 0, 1.0 ) );
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				float2 p = 2 * i.texcoord - 1;
				float3 back = tex2D( _BackGround, p * _InvertedZoom).b;
				float r = sqrt( dot( p,p ) );

				float alpha = saturate( ( 1 - r )*( 45 * _InvertedZoom + 5 ) );

				float4 c = 0;
				c = _MainTex.Sample( sampler_MainTex, i.texcoord );
				if (_ManualTex2SRGB) c.rgb = LinearToGammaSpace(c.rgb);
				c.rgb *= _Mask.rgb;
				c.rgb = lerp( back, c.rgb, c.a * alpha);

				c.a = tex2D( _GUIClipTexture, i.clipUV ).a;
				return saturate( c );
			}
			ENDCG
		}
	}
	Fallback Off
}
