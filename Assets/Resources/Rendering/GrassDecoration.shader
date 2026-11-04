Shader "Megdan/GrassDecoration"
{
    Properties
    {
        _MainTex ("Original grass", 2D) = "white" {}
        _Crop ("Source rectangle", Vector) = (0,0,1,1)
        _Ground ("Ground patch", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Crop;
                float _Ground;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; return o;
            }
            half4 frag(Output i) : SV_Target
            {
                float2 sourceUV = _Crop.xy + i.uv * _Crop.zw;
                // The original rectangular texture extends beyond the actual point-top hex.
                // Never lift or patch those out-of-board pixels at edge decorations.
                float hexHalfWidth = .8660254 * min(.5, 2 * min(sourceUV.y, 1-sourceUV.y));
                clip(hexHalfWidth - abs(sourceUV.x-.5));
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sourceUV);
                // Original paintings have a consistent grass field. Retain painted light fills,
                // dark outlines and coloured mushroom caps, removing only the green field.
                half3 grass = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(.55,.45)).rgb;
                half difference = distance(source.rgb, grass);
                if (_Ground < .5)
                {
                    clip(.86 - min(source.r, min(source.g, source.b)));
                    clip(difference - .16);
                    return half4(source.rgb, 1);
                }
                // Sample a decoration-free patch of the same original tile underneath the lifted
                // painting. No source bitmap is changed; top-down disables this complete layer.
                half3 ground = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                    float2(.52,.45) + (i.uv-.5) * .07).rgb;
                return half4(ground, 1);
            }
            ENDHLSL
        }
    }
}
