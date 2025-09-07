// ������ɫ�������ƣ������� Unity �� Shader �����˵����ҵ�
Shader "Minimap/FlatColor"
{
    // ������ɫ�����ԣ���Щ���Կ����ڲ��ʵ� Inspector �н��б༭
    Properties
    {
        // _BaseColor: ��ɫ���ԣ����ƺ������� C# �ű��е� MaterialPropertyBlock ��Ӧ
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

        SubShader
    {
        // ��ǩ������ָʾ��Ⱦ������δ������ɫ��
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            // Pass �����ƺͱ�ǩ�������� URP ��Ⱦ������ʶ�����Ⱦͨ��
            // �� LightMode ����Ϊһ���Զ���ġ��������չ��ߴ����ģʽ
            Name "MinimapUnlit"
            Tags { "LightMode" = "MinimapUnlit" }

        // ��Ⱦ״̬����
        // SrcAlpha OneMinusSrcAlpha�������Ҫ͸���ȣ��������ģʽ
        Blend SrcAlpha OneMinusSrcAlpha
        // Cull Off���ر����޳���ȷ�����۴��ĸ����򿴣�ģ�͵����涼��Ⱦ
        Cull Off

        HLSLPROGRAM
        // ����ָ�����ָ��Ŀ��ƽ̨�͹���
        #pragma vertex vert
        #pragma fragment frag

        // ���� URP ���Ŀ⣬����ͨ�õĺ�ͺ���
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // ����C#�ű��п���ͨ�� MaterialPropertyBlock �޸ĵ�����
        // CBUFFER_START �����ڶ���һ������������������GPU�ϴ洢����
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; // �� Properties �е� _BaseColor ��Ӧ
        CBUFFER_END

            // ������ɫ��������ṹ��
            struct Attributes
            {
                float4 positionOS : POSITION; // ģ�Ϳռ�����
            };

        // ������ɫ�������Ƭ����ɫ���Ľṹ��
        struct Varyings
        {
            float4 positionCS : SV_POSITION; // �ü��ռ����꣬SV_POSITION����ɫ������
        };

        // ������ɫ������
        Varyings vert(Attributes input)
        {
            Varyings output;
            // ��ģ�Ϳռ䶥��ת��Ϊ�ü��ռ䶥��
            output.positionCS = TransformObjectToHClip(input.positionOS);
            return output;
        }

        // Ƭ����ɫ������
        half4 frag(Varyings input) : SV_Target
        {
            // Ƭ����ɫ��ֱ�ӷ��ز��ʵ���ɫ���������κι��ջ�����
            return half4(_BaseColor);
        }
        ENDHLSL
    }
    }
}
