using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//GRAPH
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Naiwen.TAA
{
    internal class CameraSettingPass : ScriptableRenderPass
    {

#if UNITY_2023_3_OR_NEWER
        /////GRAPH
        public class PassData
        {
            public RenderingData renderingData;
            public UniversalCameraData cameraData;
            public CullingResults cullResults;
            public TextureHandle colorTargetHandleA;
            public void Init(ContextContainer frameData, IUnsafeRenderGraphBuilder builder = null)
            {
                cameraData = frameData.Get<UniversalCameraData>();
                cullResults = frameData.Get<UniversalRenderingData>().cullResults;
            }
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "CameraSettingPass";
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName,
                out var data))
            {
                builder.AllowPassCulling(false);
                data.Init(frameData, builder);
                builder.AllowGlobalStateModification(true);
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                data.colorTargetHandleA = resourceData.activeColorTexture;
                builder.UseTexture(data.colorTargetHandleA, AccessFlags.ReadWrite);

                builder.SetRenderFunc<PassData>((data, ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    //OnCameraSetupA(cmd, data);
                    ExecutePass(cmd, data, ctx);
                });
            }
        }
        void ExecutePass(CommandBuffer command, PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
        {
            CommandBuffer unsafeCmd = command;


            //unsafeCmd.Clear();
            //CameraData cameraData = data.cameraData;
            //Debug.Log(data.cameraData.camera.worldToCameraMatrix);
            //Debug.Log(m_TaaData.projOverride);
            //Debug.Log(m_TaaData.projOverride);
            unsafeCmd.SetViewProjectionMatrices(data.cameraData.camera.worldToCameraMatrix, m_TaaData.projOverride);

            //CommandBufferPool.Release(unsafeCmd);
            return;

            //if (Camera.main == null)
            //{
            //    return;
            //}           
            //CommandBuffer cmd = unsafeCmd;// CommandBufferPool.Get(m_ProfilerTag);
            RenderTextureDescriptor opaqueDesc = data.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            //v1.6
            if (Camera.main != null && data.cameraData.camera == Camera.main)
            {
                //cmd.Blit(source, source, outlineMaterial, 0);
            }
        }
        public void OnCameraSetupA(CommandBuffer cmd, PassData renderingData)//(CommandBuffer cmd, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            var renderer = renderingData.cameraData.renderer;
            //destination = renderingData.colorTargetHandleA;
            //source = renderingData.colorTargetHandleA;
        }
#endif




        ProfilingSampler m_ProfilingSampler;
        string m_ProfilerTag = "SetCamera";
        TAAData m_TaaData;
        internal CameraSettingPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        internal void Setup(TAAData data)
        {
            m_TaaData = data;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CameraData cameraData = renderingData.cameraData;
                cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, m_TaaData.projOverride);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
