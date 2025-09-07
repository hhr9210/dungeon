// 这是着色器的名称，可以在 Unity 的 Shader 下拉菜单中找到
Shader "Minimap/FlatColor"
{
    // 定义着色器属性，这些属性可以在材质的 Inspector 中进行编辑
    Properties
    {
        // _BaseColor: 颜色属性，名称和类型与 C# 脚本中的 MaterialPropertyBlock 对应
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

        SubShader
    {
        // 标签：用于指示渲染管线如何处理此着色器
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            // Pass 的名称和标签，用于在 URP 渲染管线中识别此渲染通道
            // 将 LightMode 设置为一个自定义的、不被光照管线处理的模式
            Name "MinimapUnlit"
            Tags { "LightMode" = "MinimapUnlit" }

        // 渲染状态设置
        // SrcAlpha OneMinusSrcAlpha：如果需要透明度，开启混合模式
        Blend SrcAlpha OneMinusSrcAlpha
        // Cull Off：关闭面剔除，确保无论从哪个方向看，模型的两面都渲染
        Cull Off

        HLSLPROGRAM
        // 编译指令，用于指定目标平台和功能
        #pragma vertex vert
        #pragma fragment frag

        // 引入 URP 核心库，包含通用的宏和函数
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // 定义C#脚本中可以通过 MaterialPropertyBlock 修改的属性
        // CBUFFER_START 宏用于定义一个常量缓冲区，以在GPU上存储数据
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; // 与 Properties 中的 _BaseColor 对应
        CBUFFER_END

            // 顶点着色器的输入结构体
            struct Attributes
            {
                float4 positionOS : POSITION; // 模型空间坐标
            };

        // 顶点着色器输出到片段着色器的结构体
        struct Varyings
        {
            float4 positionCS : SV_POSITION; // 裁剪空间坐标，SV_POSITION是着色器语义
        };

        // 顶点着色器函数
        Varyings vert(Attributes input)
        {
            Varyings output;
            // 将模型空间顶点转换为裁剪空间顶点
            output.positionCS = TransformObjectToHClip(input.positionOS);
            return output;
        }

        // 片段着色器函数
        half4 frag(Varyings input) : SV_Target
        {
            // 片段着色器直接返回材质的颜色，不考虑任何光照或纹理
            return half4(_BaseColor);
        }
        ENDHLSL
    }
    }
}
