Shader "Hidden/TFHCPixelateUV"
{
	Properties
	{
		_A ("_UV", 2D) = "white" {}
		_B ("_PixelX", 2D) = "white" {}
		_C ("_PixelY", 2D) = "white" {}
		_D ("_PixelOffset", 2D) = "black" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;
			sampler2D _C;
			sampler2D _D;

			float4 frag(v2f_img i) : SV_Target
			{
				float2 uv = tex2D( _A, i.uv ).rg;
				float pix = tex2D( _B, i.uv ).r;
				float piy = tex2D( _C, i.uv ).r;
				float2 poffset = tex2D( _D, i.uv ).rg;

				float2 steppedPixel = float2( pix, piy );
				float2 pixelatedUV = floor( uv * steppedPixel + poffset ) / steppedPixel;
				return float4(pixelatedUV, 0 , 0);
			}
			ENDCG
		}
	}
}
