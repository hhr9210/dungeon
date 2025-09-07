using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PixelationGlitchSettings
    {
        public Material effectMaterial = null;
        [Range(0.0f, 1.0f)]
        public float renderPassEvent = 0.9f;
    }

    public PixelationGlitchSettings settings = new PixelationGlitchSettings();

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (settings.effectMaterial == null)
        {
            m_ScriptablePass = null;
            return;
        }

        m_ScriptablePass = new CustomRenderPass(settings.effectMaterial);
        m_ScriptablePass.renderPassEvent = (RenderPassEvent)Mathf.Lerp((float)RenderPassEvent.AfterRenderingOpaques, (float)RenderPassEvent.AfterRenderingPostProcessing, settings.renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (!renderingData.cameraData.postProcessEnabled)
        {
            return;
        }


        //检查摄像机类型
         if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }


        if (m_ScriptablePass == null)
        {
            return;
        }

        renderer.EnqueuePass(m_ScriptablePass);
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;

        public CustomRenderPass(Material material)
        {
            blitMaterial = material;
            tempTexture.Init("_TemporaryColorTexture");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = renderingData.cameraData.renderer as UniversalRenderer;
            if (renderer == null)
            {
                return;
            }
            source = renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blitMaterial == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Pixelation Glitch Pass");

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc, FilterMode.Point);

            Blit(cmd, source, tempTexture.Identifier(), blitMaterial, 0);

            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }
}