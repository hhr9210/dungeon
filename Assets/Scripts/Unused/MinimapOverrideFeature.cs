using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// С��ͼ��Ⱦ������
// ������Խ����ӵ�һ�� URP ��Ⱦ���ϣ���������Ⱦ�����в����Զ������Ⱦ�߼���
public class MinimapOverrideFeature : ScriptableRendererFeature
{
    private MinimapOverridePass minimapOverridePass;

    [Header("��ɫ���Ͳ���")]
    [Tooltip("����С��ͼ�����ƽ�洿ɫ��ɫ��")]
    public Shader flatColorShader;

    [Header("��Ⱦ����")]
    [Tooltip("��û��ƥ��Ĳ㼶���ǩʱʹ�õ�Ĭ����ɫ")]
    public Color defaultColor = Color.gray;
    [Tooltip("���ݲ㼶��Layer������������ɫ")]
    public LayerColor[] layerColors;
    [Tooltip("���ݱ�ǩ��Tag������������ɫ")]
    public TagColor[] tagColors;

    // �����ڱ༭�������õĽṹ��
    [System.Serializable]
    public struct LayerColor { public LayerMask layer; public Color color; }
    [System.Serializable]
    public struct TagColor { public string tag; public Color color; }

    // ����Ⱦ�����Ա�����ʱ����
    public override void Create()
    {
        // ʵ�������ǵ��Զ�����Ⱦ Pass
        minimapOverridePass = new MinimapOverridePass(name, defaultColor, layerColors, tagColors);
        minimapOverridePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        // ���Զ�����ɫ�����ݸ� Pass
        if (flatColorShader != null)
        {
            minimapOverridePass.SetShader(flatColorShader);
        }
    }

    // �� Pass ���ӵ���Ⱦ������
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // ���ݸ��ɵ� URP �汾��ͨ������������ isOrthographic ����
        if (renderingData.cameraData.camera.orthographic)
        {
            renderer.EnqueuePass(minimapOverridePass);
        }
    }

    // ʵ��ִ����Ⱦ�߼����ڲ���
    class MinimapOverridePass : ScriptableRenderPass
    {
        private const string PROFILER_TAG = "MinimapOverridePass";
        private readonly Color defaultColor;
        private readonly LayerColor[] layerColors;
        private readonly TagColor[] tagColors;
        private Material flatColorMaterial;
        private readonly Dictionary<Renderer, Material[]> originalData =
            new Dictionary<Renderer, Material[]>();

        public MinimapOverridePass(string name, Color defaultColor, LayerColor[] layerColors, TagColor[] tagColors)
        {
            this.defaultColor = defaultColor;
            this.layerColors = layerColors;
            this.tagColors = tagColors;
        }

        // ����������Ⱦ����ɫ��
        public void SetShader(Shader shader)
        {
            if (flatColorMaterial != null)
            {
                CoreUtils.Destroy(flatColorMaterial);
            }
            if (shader != null)
            {
                flatColorMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (flatColorMaterial == null) return;
            originalData.Clear();
            Renderer[] sceneRenderers = Object.FindObjectsOfType<Renderer>();

            foreach (var renderer in sceneRenderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                LayerMask currentCullingMask = renderingData.cameraData.camera.cullingMask;
                if (!currentCullingMask.Includes(renderer.gameObject.layer))
                {
                    continue;
                }

                // �洢ԭʼ�����Ա��ָ�
                originalData[renderer] = renderer.sharedMaterials;

                // Ӧ���²��ʣ�ֱ���޸� sharedMaterial
                renderer.sharedMaterial = flatColorMaterial;
            }

            // �ָ�ԭʼ����
            RestoreOriginalData();
        }

        // �ָ�������Ⱦ����ԭʼ����
        private void RestoreOriginalData()
        {
            foreach (var entry in originalData)
            {
                if (entry.Key != null)
                {
                    entry.Key.sharedMaterials = entry.Value;
                }
            }
        }
    }
}

// ��չ���������ڶ�����̬���ж���
public static class LayerMaskExtensions
{
    public static bool Includes(this LayerMask mask, int layer)
    {
        return ((mask.value & (1 << layer)) > 0);
    }
}
