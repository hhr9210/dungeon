Shader "Hidden/PixelationGlitch"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _PixelSize("Pixel Size", Float) = 64.0
        _GlitchStrength("Glitch Strength", Range(0, 1)) = 0.1
        _GlitchSpeed("Glitch Speed", Float) = 5.0
        _ScanlineDensity("Scanline Density 1", Range(0, 100)) = 50.0
        _ScanlineStrength("Scanline Strength 1", Range(0, 1)) = 0.1
        _SweepingScanlineSpeed("Sweeping Scanline Speed", Float) = 1.0
        _SweepingScanlineThickness("Sweeping Scanline Thickness", Range(0.001, 0.1)) = 0.01
        _SweepingScanlineIntensity("Sweeping Scanline Intensity", Range(0, 5)) = 1.0
        _CurveAmount("Curve Amount", Range(0, 1)) = 0.5
        _BorderWidth("Border Width", Range(0, 0.5)) = 0.05
        _BorderColor("Border Color", Color) = (0,0,0,1)
        _ChromaticAberrationStrength("Chromatic Aberration Strength", Range(0, 0.01)) = 0.002
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Overlay" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "PixelationGlitchPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _PixelSize;
            float _GlitchStrength;
            float _GlitchSpeed;
            float _ScanlineDensity;
            float _ScanlineStrength;
            float _SweepingScanlineSpeed;
            float _SweepingScanlineThickness;
            float _SweepingScanlineIntensity;
            float _CurveAmount;
            float _BorderWidth;
            float4 _BorderColor;
            float _ChromaticAberrationStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenResolution = _ScreenParams.xy;

                float2 centeredUV = input.uv - 0.5;
                float distSq = dot(centeredUV, centeredUV);
                float distortion = pow(distSq, 2.0) * _CurveAmount * 3.0;
                float2 curvedUV = centeredUV * (1.0 + distortion);
                float2 finalUV = curvedUV + 0.5;

                float2 pixelGridSize = 1.0 / _PixelSize;
                float2 uv_pixelated = floor(finalUV * screenResolution * pixelGridSize) / (screenResolution * pixelGridSize);

                float glitchOffsetX = sin(finalUV.y * 50.0 + _Time.y * _GlitchSpeed) * _GlitchStrength * 0.005;
                float glitchOffsetY = cos(finalUV.x * 30.0 + _Time.y * _GlitchSpeed * 0.7) * _GlitchStrength * 0.003;

                float2 offsetR_glitch = float2(sin(_Time.y * _GlitchSpeed + 10.0) * _GlitchStrength * 0.001, 0);
                float2 offsetG_glitch = float2(sin(_Time.y * _GlitchSpeed + 20.0) * _GlitchStrength * 0.001, 0);
                float2 offsetB_glitch = float2(sin(_Time.y * _GlitchSpeed + 30.0) * _GlitchStrength * 0.001, 0);

                uv_pixelated.x += glitchOffsetX;
                uv_pixelated.y += glitchOffsetY;

                uv_pixelated = saturate(uv_pixelated);

                float aberrationFactor = distSq * _ChromaticAberrationStrength;
                float2 offsetR_ca = float2(aberrationFactor, 0);
                float2 offsetG_ca = float2(0, 0);
                float2 offsetB_ca = float2(-aberrationFactor, 0);

                float4 colR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv_pixelated + offsetR_glitch + offsetR_ca));
                float4 colG = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv_pixelated + offsetG_glitch + offsetG_ca));
                float4 colB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv_pixelated + offsetB_glitch + offsetB_ca));

                float4 finalColor = float4(colR.r, colG.g, colB.b, 1.0);

                float scanline1 = sin(finalUV.y * _ScanlineDensity * screenResolution.y);
                scanline1 = saturate(scanline1 * 0.5 + 0.5);
                scanline1 = lerp(1.0, scanline1, _ScanlineStrength);
                finalColor.rgb *= scanline1;

                float sweepingLinePos = fmod(_Time.y * _SweepingScanlineSpeed, 1.0);
                float distanceFromLine = abs(finalUV.y - sweepingLinePos);
                float sweepingLineMask = 1.0 - smoothstep(0.0, _SweepingScanlineThickness, distanceFromLine);
                sweepingLineMask *= _SweepingScanlineIntensity;

                finalColor.rgb += sweepingLineMask;
                finalColor.rgb = saturate(finalColor.rgb);

                float borderX = smoothstep(0.0, _BorderWidth, finalUV.x) * smoothstep(0.0, _BorderWidth, 1.0 - finalUV.x);
                float borderY = smoothstep(0.0, _BorderWidth, finalUV.y) * smoothstep(0.0, _BorderWidth, 1.0 - finalUV.y);
                float borderMask = borderX * borderY;

                finalColor.rgb = lerp(_BorderColor.rgb, finalColor.rgb, borderMask);

                return finalColor;
            }
            ENDHLSL
        }
    }
}