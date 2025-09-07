Shader "Custom/URPToonWithOutline"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _Threshold ("Light Threshold", Range(0,1)) = 0.5
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularPower ("Specular Power", Range(1, 128)) = 32
        _SpecularThreshold ("Specular Threshold", Range(0, 1)) = 0.95
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // 描边 Pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // 根据法线方向挤出顶点，生成描边效果
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 offset = normalWS * _OutlineWidth;
                float3 offsetPos = posWS + offset;
                OUT.positionHCS = TransformWorldToHClip(offsetPos);
                return OUT;
            }

            // 返回描边颜色
            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Threshold;
                float4 _SpecularColor;
                float _SpecularPower;
                float _SpecularThreshold;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                // 获取实时阴影
                float shadow = MainLightRealtimeShadow(IN.shadowCoord);

                // 计算漫反射，用阈值实现硬朗的阴影分界
                float NdotL = saturate(dot(N, L));
                float lightIntensity = NdotL > _Threshold ? 1.0 : 0.3;
                lightIntensity *= shadow;

                // 计算高光，使用step函数实现硬高光效果
                float nh = saturate(dot(N, H));
                float spec = step(_SpecularThreshold, nh);
                float3 specular = _SpecularColor.rgb * spec * shadow;

                // 计算边缘光
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float3 rimLight = _RimColor.rgb * rim;

                // 最终颜色 = 基础色 * 光照 + 高光 + 边缘光
                float3 color = _Color.rgb * mainLight.color.rgb * lightIntensity;
                color += specular + rimLight;

                return float4(color, 1.0);
            }
            ENDHLSL
        }

        // 阴影投射 Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}