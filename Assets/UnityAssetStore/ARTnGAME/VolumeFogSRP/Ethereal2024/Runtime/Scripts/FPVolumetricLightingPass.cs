using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
//using UnityEngine.Experimental.Rendering.

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Artngame.SKYMASTER.EtherealVolumetrics
{
    public class FPVolumetricLightingPass : ScriptableRenderPass
    {
        private static int s_VolumeVoxelizationCSKernal;
        private static int s_VBufferLightingCSKernal;
        private static int s_VBufferFilteringCSKernal;

        private VolumetricConfig m_Config;
        private ComputeShader m_VolumeVoxelizationCS;
        private ComputeShader m_VolumetricLightingCS;
        private ComputeShader m_VolumetricLightingFilteringCS;
        private Material m_ResolveMat;
        private RTHandle m_VBufferDensityHandle;
        private RTHandle m_VBufferLightingHandle;
        private RTHandle m_VBufferLightingFilteredHandle;
        private RTHandle[] m_VolumetricHistoryBuffers;
        private VBufferParameters m_VBufferParameters;
        private LocalVolumetricFog[] m_LocalVolumes;
        private Matrix4x4[] m_VBufferCoordToViewDirWS;
        private Matrix4x4 m_PixelCoordToViewDirWS;
        private Matrix4x4 m_PrevMatrixVP;

        private Vector2[] m_xySeq;
        private bool m_FilteringNeedsExtraBuffer;
        private bool m_HistoryBufferAllocated;
        private bool m_VBufferHistoryIsValid;
        private int m_FrameIndex;
        private Vector3 m_PrevCamPosRWS;
        private ProfilingSampler m_ProfilingSampler;

        // CBuffers
        private ShaderVariablesFog m_FogCB = new ShaderVariablesFog();
        private ShaderVariablesVolumetricLighting m_VolumetricLightingCB = new ShaderVariablesVolumetricLighting();
        private ShaderVariablesLocalVolume m_LocalVolumeCB = new ShaderVariablesLocalVolume();


        public FPVolumetricLightingPass(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent; //RenderPassEvent.BeforeRenderingPostProcessing;
            m_xySeq = new Vector2[7];
            m_VBufferCoordToViewDirWS = new Matrix4x4[1]; // Currently xr not supported
            m_PrevMatrixVP = Matrix4x4.identity;
        }

        public void Setup(VolumetricConfig config, in VBufferParameters vBufferParameters)
        {
           
            m_VolumeVoxelizationCS = config.volumeVoxelizationCS;
            m_VolumetricLightingCS = config.volumetricLightingCS;
            m_VolumetricLightingFilteringCS = config.volumetricLightingFilteringCS;
            m_ResolveMat = config.resolveMat;
            m_Config = config;
            m_VBufferParameters = vBufferParameters;
            m_ProfilingSampler = new ProfilingSampler("Volumetric Lighting");
            ConfigureInput(ScriptableRenderPassInput.Depth);
           // Debug.Log("m_ResolveMat is " + m_ResolveMat.name);
        }

        public void Dispose()
        {
            m_VBufferDensityHandle?.Release();
            m_VBufferLightingHandle?.Release();
            m_VBufferLightingFilteredHandle?.Release();
            DestroyHistoryBuffers();
        }

#if UNITY_2023_3_OR_NEWER
        //v0.1
        /// <summary>
        /// // RG
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderingData"></param>
        //class PassData
        //{
        //    public CameraData cameraData;
        //    //internal TextureHandle copySourceTexture;
        //    public void Init(ContextContainer frameData)
        //    {
        //        cameraData = new CameraData(frameData);
        //    }

        //}
        //public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        //{
        //    string passName = "Volumetrics";

        //    // Add a raster render pass to the render graph. The PassData type parameter determines
        //    // the type of the passData output variable.
        //    using (var builder = renderGraph.AddUnsafePass<PassData>(passName,
        //        out var data))
        //    {
        //        /*
        //        // UniversalResourceData contains all the texture references used by URP,
        //        // including the active color and depth textures of the camera.
        //        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        //        // Populate passData with the data needed by the rendering function
        //        // of the render pass.
        //        // Use the camera's active color texture
        //        // as the source texture for the copy operation.
        //        passData.copySourceTexture = resourceData.activeColorTexture;

        //        // Create a destination texture for the copy operation based on the settings,
        //        // such as dimensions, of the textures that the camera uses.
        //        // Set msaaSamples to 1 to get a non-multisampled destination texture.
        //        // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
        //        // creates a color texture and not a depth texture.
        //        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        //        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        //        desc.msaaSamples = 1;
        //        desc.depthBufferBits = 0;

        //        // For demonstrative purposes, this sample creates a temporary destination texture.
        //        // UniversalRenderer.CreateRenderGraphTexture is a helper method
        //        // that calls the RenderGraph.CreateTexture method.
        //        // Using a RenderTextureDescriptor instance instead of a TextureDesc instance
        //        // simplifies your code.
        //        TextureHandle destination =
        //            UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
        //                "CopyTexture", false);

        //        // Declare that this render pass uses the source texture as a read-only input.
        //        builder.UseTexture(passData.copySourceTexture);

        //        // Declare that this render pass uses the temporary destination texture
        //        // as its color render target.
        //        // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
        //        builder.SetRenderAttachment(destination, 0);

        //        // RenderGraph automatically determines that it can remove this render pass
        //        // because its results, which are stored in the temporary destination texture,
        //        // are not used by other passes.
        //        // For demonstrative purposes, this sample turns off this behavior to make sure
        //        // that render graph executes the render pass. 
        //        builder.AllowPassCulling(false);

        //        // Set the ExecutePass method as the rendering function that render graph calls
        //        // for the render pass. 
        //        // This sample uses a lambda expression to avoid memory allocations.
        //        // builder.SetRenderFunc((PassData data, RasterGraphContext context)
        //        //     => ExecutePass(data, context));
        //        */

        //        builder.SetRenderFunc<PassData>(( data,  ctx)
        //            => {
        //                data.Init(frameData);
        //                var cmd = ctx.cmd.WrapperCommandBuffer;
        //                var renderContext = ctx.wrappedContext.renderContext;
        //                OnSetup(cmd,data);
        //                ExecutePass(renderContext, cmd, data);
        //            }
        //            );
        //    }
        //}
        //static void ExecutePass(PassData data, RasterGraphContext context)
        //{
        //    // Records a rendering command to copy, or blit, the contents of the source texture
        //    // to the color render target of the render pass.
        //    // The RecordRenderGraph method sets the destination texture as the render target
        //    // with the UseTextureFragment method.
        //    Blitter.BlitTexture(context.cmd, data.copySourceTexture,
        //        new Vector4(1, 1, 0, 0), 0, false);
        //}
       

        public class PassData
        {
            //TEST
            //internal TextureHandle src;
            //internal TextureHandle dest;
            //internal TextureHandle destHalf;
            //internal TextureHandle destQuarter;



            public RenderingData renderingData;
            public UniversalCameraData cameraData;// CameraData cameraData;
            public CullingResults cullResults;
            //public RGUtil.Handle colorTargetHandle; //public RTHandle colorTargetHandle;
            public TextureHandle colorTargetHandleA;
            //internal TextureHandle copySourceTexture;
            public void Init(ContextContainer frameData, IUnsafeRenderGraphBuilder builder = null)
            //public void Init(ContextContainer frameData)
            {
                // cameraData = new CameraData(frameData);
                //frameData.Get(out UniversalResourceData resources);


                cameraData = frameData.Get<UniversalCameraData>();
                //frameData.Get(out cameraData);

                cullResults = frameData.Get<UniversalRenderingData>().cullResults;// renderingData.cullResults;
                // public ref CullingResults cullResults => ref frameData.Get<UniversalRenderingData>().cullResults

                //if (builder == null) // non-RG pass
                //{
                //    //colorTargetHandle = cameraData.renderer.cameraColorTargetHandle;
                //}
                //else
                //{
                //    //Debug.Log("Output SET !!!!!!!!!!!!!!!!!!!!!");
                //  //  colorTargetHandleA = resources.activeColorTexture;
                //   // builder.UseTexture(colorTargetHandleA, AccessFlags.ReadWrite);
                //}
            }
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Volumetrics";

            // Add a raster render pass to the render graph. The PassData type parameter determines
            // the type of the passData output variable.
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName,
                out var data))
            {
                /*
                // UniversalResourceData contains all the texture references used by URP,
                // including the active color and depth textures of the camera.
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Populate passData with the data needed by the rendering function
                // of the render pass.
                // Use the camera's active color texture
                // as the source texture for the copy operation.
                passData.copySourceTexture = resourceData.activeColorTexture;

                // Create a destination texture for the copy operation based on the settings,
                // such as dimensions, of the textures that the camera uses.
                // Set msaaSamples to 1 to get a non-multisampled destination texture.
                // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
                // creates a color texture and not a depth texture.
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                // For demonstrative purposes, this sample creates a temporary destination texture.
                // UniversalRenderer.CreateRenderGraphTexture is a helper method
                // that calls the RenderGraph.CreateTexture method.
                // Using a RenderTextureDescriptor instance instead of a TextureDesc instance
                // simplifies your code.
                TextureHandle destination =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                        "CopyTexture", false);

                // Declare that this render pass uses the source texture as a read-only input.
                builder.UseTexture(passData.copySourceTexture);

                // Declare that this render pass uses the temporary destination texture
                // as its color render target.
                // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
                builder.SetRenderAttachment(destination, 0);

                // RenderGraph automatically determines that it can remove this render pass
                // because its results, which are stored in the temporary destination texture,
                // are not used by other passes.
                // For demonstrative purposes, this sample turns off this behavior to make sure
                // that render graph executes the render pass. 
                builder.AllowPassCulling(false);

                // Set the ExecutePass method as the rendering function that render graph calls
                // for the render pass. 
                // This sample uses a lambda expression to avoid memory allocations.
                // builder.SetRenderFunc((PassData data, RasterGraphContext context)
                //     => ExecutePass(data, context));
                

                builder.SetRenderFunc<PassData>((data, ctx)
                    =>
                {
                    data.Init(frameData);
                    var cmd = ctx.cmd.WrapperCommandBuffer;
                    var renderContext = ctx.wrappedContext.renderContext;
                    OnSetup(cmd, data);
                    ExecutePass(renderContext, cmd, data);
                }
                    );
                */
                builder.AllowPassCulling(false);

                //builder.SetRenderAttachment(destination, 0);

                //frameData.Get(out UniversalResourceData resources);

                data.Init(frameData, builder);
                builder.AllowGlobalStateModification(true);


                //v0.2
                //ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);

                // Get the frame data
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Add the active color buffer to our pass data, and set it as writeable 
                data.colorTargetHandleA = resourceData.activeColorTexture;
                builder.UseTexture(data.colorTargetHandleA, AccessFlags.ReadWrite);






                //TEST
                //UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                //data.src = resourceData.activeColorTexture;
                //UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                //RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                //desc.msaaSamples = 1;
                //desc.depthBufferBits = 0;
                //TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture", false);
                //desc.width /= 2;
                //desc.height /= 2;
                //TextureHandle destinationHalf = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture2", false);
                //desc.width /= 2;
                //desc.height /= 2;
                //TextureHandle destinationQuarter = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture3", false);
                //data.dest = destination;
                //data.destHalf = destinationHalf;
                //data.destQuarter = destinationQuarter;
                //// We declare the src texture as an input dependency to this pass, via UseTexture()
                //builder.UseTexture(data.src);
                //// UnsafePasses don't setup the outputs using UseTextureFragment/UseTextureFragmentDepth, you should specify your writes with UseTexture instead
                //builder.UseTexture(data.dest, AccessFlags.Write);
                //builder.UseTexture(data.destHalf, AccessFlags.Write);
                //builder.UseTexture(data.destQuarter, AccessFlags.Write);

                






                builder.SetRenderFunc<PassData>((data, ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    var renderContext = GetRenderContextB(ctx);



                    OnCameraSetupA(cmd, data);
                    ExecutePass(renderContext, cmd, data, ctx);

                    //Debug.Log("All pass");
                });
            }
        }



        static FieldInfo AR_renderContext = typeof(InternalRenderGraphContext).GetField("renderContext", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo AR_InternalRenderGraphContext = typeof(UnsafeGraphContext).GetField("wrappedContext", BindingFlags.NonPublic | BindingFlags.Instance);
        static InternalRenderGraphContext GetInternalRenderGraphContextB( UnsafeGraphContext unsafeContext)
        {
            return (InternalRenderGraphContext)AR_InternalRenderGraphContext.GetValue(unsafeContext);
        }
        public static ScriptableRenderContext GetRenderContextB( UnsafeGraphContext unsafeContext)
        {
            return (ScriptableRenderContext)AR_renderContext.GetValue(GetInternalRenderGraphContextB(unsafeContext));
        }


        void ExecutePass(ScriptableRenderContext context, CommandBuffer command,  PassData data, UnsafeGraphContext ctx)//, RasterGraphContext context)
        {
            // Records a rendering command to copy, or blit, the contents of the source texture
            // to the color render target of the render pass.
            // The RecordRenderGraph method sets the destination texture as the render target
            // with the UseTextureFragment method.
            //Blitter.BlitTexture(context.cmd, data.copySourceTexture, new Vector4(1, 1, 0, 0), 0, false);


            //TEST
            CommandBuffer unsafeCmd = command;// CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

            //ctx.cmd.SetRenderTarget(data.dest);
            //Blitter.BlitTexture(unsafeCmd, data.src, new Vector4(1, 1, 0, 0), 0, false);

            //// downscale x2

            //ctx.cmd.SetRenderTarget(data.destHalf);
            //Blitter.BlitTexture(unsafeCmd, data.dest, new Vector4(1, 1, 0, 0), 0, false);

            //ctx.cmd.SetRenderTarget(data.destQuarter);
            //Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);

            //// upscale x2

            //ctx.cmd.SetRenderTarget(data.destHalf);
            //Blitter.BlitTexture(unsafeCmd, data.destQuarter, new Vector4(1, 1, 0, 0), 0, false);

            //ctx.cmd.SetRenderTarget(data.dest);
            //Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);

            //ctx.cmd.SetRenderTarget(data.colorTargetHandleA);
           // Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);
           // return;




            m_FrameIndex = (m_FrameIndex + 1) % 14;

            var camera = data.cameraData.camera;

            //Debug.Log("camera name = " + camera.name);

            var cmd = unsafeCmd;// CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var voxelSize = m_VBufferParameters.voxelSize;
                var vBufferViewportSize = m_VBufferParameters.viewportSize;

                // The shader defines GROUP_SIZE_1D = 8.
                int width = ((int)vBufferViewportSize.x + 7) / 8;
                int height = ((int)vBufferViewportSize.y + 7) / 8;

               // Debug.Log("ExecutePass 1 " + width + "," + height);


                // VBuffer Density
                if (m_VolumeVoxelizationCS != null)
                {
                    cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, s_VolumeVoxelizationCSKernal, IDs._VBufferDensity, m_VBufferDensityHandle);
                    cmd.DispatchCompute(m_VolumeVoxelizationCS, s_VolumeVoxelizationCSKernal, width, height, 1);
                }

                // Local Volume material
                if (m_LocalVolumes != null && m_LocalVolumes.Length > 0)
                {
                    for (int i = 0; i < Math.Min(m_LocalVolumes.Length, 4); i++)
                    {
                        var shaderSetting = m_LocalVolumes[i].volumeShaderSetting;
                        bool isShaderValid = shaderSetting.shader != null;
                        var cs = isShaderValid ? shaderSetting.shader : m_Config.defaultLocalVolumeShader;

                        if (cs != null)
                        {
                            // Update RT if implemented in child volume class
                            bool hasRTUpdated = m_LocalVolumes[i].UpdateRenderTextureIfNeeded(context, cmd, ref data.renderingData);

                            // Compute density
                            var kernel = isShaderValid ? cs.FindKernel(shaderSetting.kernelName) : cs.FindKernel("SmokeVolumeMaterial");
                            UpdateVolumeShaderVariables(ref m_LocalVolumeCB, m_LocalVolumes[i], camera);
                            // TODO: Set properties to shader from child local volumetric fog setting
                            m_LocalVolumes[i].SetComputeShaderProperties(cmd, cs, kernel);
                            cmd.SetComputeTextureParam(cs, kernel, IDs._VBufferDensity, m_VBufferDensityHandle);
                            ConstantBuffer.Push(cmd, m_LocalVolumeCB, cs, IDs._ShaderVariablesLocalVolume);
                            cmd.DispatchCompute(cs, kernel, width, height, 1);

                            // Rollback render target
                            if (hasRTUpdated)
                            {
                                CoreUtils.SetRenderTarget(cmd, data.colorTargetHandleA, ClearFlag.None);
                                context.ExecuteCommandBuffer(cmd);
                                cmd.Clear();
                            }
                        }
                    }
                }

                // VBuffer Lighting
                if (m_VolumetricLightingCS != null
                    && m_VolumetricLightingFilteringCS != null
                    && Shader.GetGlobalTexture("_CameraDepthTexture") != null && Shader.GetGlobalTexture("_MaxZMaskTexture") != null)   // To prevent error log
                {
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferDensity, m_VBufferDensityHandle);
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferLighting, m_VBufferLightingHandle);
                    if (m_Config.enableReprojection)
                    {
                        var currIdx = (m_FrameIndex + 0) & 1;
                        var prevIdx = (m_FrameIndex + 1) & 1;
                        cmd.SetComputeVectorParam(m_VolumetricLightingCS, IDs._PrevCamPosRWS, m_PrevCamPosRWS);
                        cmd.SetComputeMatrixParam(m_VolumetricLightingCS, IDs._PrevMatrixVP, m_PrevMatrixVP);
                        cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferFeedback, m_VolumetricHistoryBuffers[currIdx]);
                        cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferHistory, m_VolumetricHistoryBuffers[prevIdx]);
                    }
                    cmd.DispatchCompute(m_VolumetricLightingCS, s_VBufferLightingCSKernal, width, height, 1);

                    if (m_Config.filterVolume)
                    {
                        cmd.SetComputeTextureParam(m_VolumetricLightingFilteringCS, s_VBufferFilteringCSKernal, IDs._VBufferLighting, m_VBufferLightingHandle);
                        if (m_FilteringNeedsExtraBuffer)
                        {
                            cmd.SetComputeTextureParam(m_VolumetricLightingFilteringCS, s_VBufferFilteringCSKernal, IDs._VBufferLightingFiltered, m_VBufferLightingFilteredHandle);
                        }
                        cmd.DispatchCompute(m_VolumetricLightingFilteringCS, s_VBufferLightingCSKernal,
                                            VolumetricUtils.DivRoundUp((int)vBufferViewportSize.x, 8),
                                            VolumetricUtils.DivRoundUp((int)vBufferViewportSize.y, 8),
                                            m_VBufferParameters.viewportSize.z);
                    }
                }

                if (m_ResolveMat != null)// && passData.colorTargetHandleA != null)
                // if (m_ResolveMat != null && passData.colorTargetHandle.RT != null && passData.colorTargetHandle.RT.rt !=null)
                {
                    // CommandBuffer unsafeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    // cmd.SetRenderTarget(data.colorTargetHandleA);
                    //Blitter.BlitTexture(cmd, data.colorTargetHandleA, new Vector4(1, 1, 0, 0), 0, false);
                    //Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), m_ResolveMat, 0);
                    //Blitter.BlitTexture(cmd, data.colorTargetHandleA, new Vector4(1, 1, 0, 0), m_ResolveMat, 0);

                   // Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);
                    Blitter.BlitTexture(unsafeCmd, data.colorTargetHandleA, new Vector4(1, 1, 0, 0), m_ResolveMat, 0);

                  //  Debug.Log("m_ResolveMat = " + m_ResolveMat.name + ", " + new RenderTexture(data.colorTargetHandleA).width);
                    //Debug.Log("m_ResolveMat = " + m_ResolveMat.name + " , RT COLOR = " + passData.colorTargetHandle.RT.rt.width);
                    //Debug.Log("m_ResolveMat = " + m_ResolveMat.name);
                    //CoreUtils.SetRenderTarget(cmd, data.colorTargetHandleA, ClearFlag.None);
                    //CoreUtils.DrawFullScreen(cmd, m_ResolveMat);
                   // CoreUtils.DrawFullScreen(unsafeCmd, m_ResolveMat);
                    //Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), m_ResolveMat,0);//   //(cmd, data.copySourceTexture, new Vector4(1, 1, 0, 0), 0, false);
                    //Blitter.BlitTexture(cmd, data.colorTargetHandle, new Vector4(1, 1, 0, 0), m_ResolveMat, 0);
                }

            }

            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);

            if (m_Config.enableReprojection && !m_VBufferHistoryIsValid)
            {
                m_VBufferHistoryIsValid = true;
            }

            // Set prev cam data
            m_PrevCamPosRWS = camera.transform.position;
            //VolumetricUtils.SetCameraMatricesA(data.cameraData, out var v, out var p, out m_PrevMatrixVP, out var invvp);
            SetCameraMatricesA(data.cameraData, out var v, out var p, out m_PrevMatrixVP, out var invvp);
        }
        static void SetCameraMatricesA(UniversalCameraData cameraData, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out Matrix4x4 viewProjMatrix, out Matrix4x4 invViewProjMatrix)
        {
            var camera = cameraData.camera;
            viewMatrix = camera.worldToCameraMatrix;
            //projMatrix = RGUtil.GetGPUProjectionMatrix(cameraData, true, 0);//cameraData.GetGPUProjectionMatrix();
            projMatrix = GetGPUProjectionMatrixA(cameraData, true, 0);// 
            //projMatrix = GetGPUProjectionMatrixA(cameraData);
            viewProjMatrix = projMatrix * viewMatrix;
            invViewProjMatrix = viewProjMatrix.inverse;
        }
        static MethodInfo GPUProjectionMatrix = typeof(UniversalCameraData).GetMethod("GetGPUProjectionMatrix",
        BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool), typeof(int) }, null);
        static object[] optionsA = new object[2];
        public static Matrix4x4 GetGPUProjectionMatrixA(UniversalCameraData cameraData, bool flipped, int viewIndex = 0)
        {
            optionsA[0] = flipped;
            optionsA[1] = viewIndex;
            return (Matrix4x4)GPUProjectionMatrix.Invoke(cameraData, optionsA);
        }
        //        public static Matrix4x4 GetGPUProjectionMatrixA(UniversalCameraData cameraData, int viewIndex = 0)
        //        {
        //            // Disable obsolete warning for internal usage
        //#pragma warning disable CS0618
        //            // GetGPUProjectionMatrix takes a projection matrix and returns a GfxAPI adjusted version, does not set or get any state.
        //            return cameraData.GetGPUProjectionMatrixNoJitter();
        //#pragma warning restore CS0618
        //        }




        PassData passData = new PassData();
        //static FieldInfo R_RenderingData_frameData = typeof(RenderingData).GetField("frameData", BindingFlags.NonPublic | BindingFlags.Instance);
        //public static ContextContainer GetFrameData(this ref RenderingData renderingData)
        //{
        //    return (ContextContainer)R_RenderingData_frameData.GetValue(renderingData);
        //}
        //public void OnCameraSetupRG(CommandBuffer cmd, ref RenderingData renderingData)
        //{
        //    //var data = new PassData();
        //    passData.renderingData = renderingData;

        //    //renderingData.frameData.Get(out UniversalResourceData resources);
        //    passData.Init(renderingData.GetFrameData());
        //    //data.Init(renderingData.frameData);
        //    OnCameraSetupA(cmd, passData);
        //}
        const string PassName = "pass rg";
        //public void ExecuteRG(ScriptableRenderContext context, ref RenderingData renderingData)
        //{
        //    var data = passData;
        //    data.Init(renderingData.GetFrameData());

        //    var cmd = CommandBufferPool.Get();
        //    cmd.name = PassName;

        //    OnCameraSetupA(cmd, data);
        //    //ExecutePass(context, cmd, data);

        //    context.ExecuteCommandBuffer(cmd);
        //    CommandBufferPool.Release(cmd);
        //}
        public void OnCameraSetupA(CommandBuffer cmd, PassData renderingData)
        {
            if (Camera.main == null)
            {
                return;
            }


            if (m_VolumeVoxelizationCS == null
                || m_VolumetricLightingCS == null
                || m_VolumetricLightingFilteringCS == null)
                return;

            //Debug.Log("OnCameraSetupA");
            var vBufferViewportSize = m_VBufferParameters.viewportSize;
            var camera = renderingData.cameraData.camera;

            // Create render texture
            var desc = new RenderTextureDescriptor(vBufferViewportSize.x, vBufferViewportSize.y, RenderTextureFormat.ARGBHalf, 0);
            desc.dimension = TextureDimension.Tex3D;
            desc.volumeDepth = vBufferViewportSize.z;
            desc.enableRandomWrite = true;
            RenderingUtils.ReAllocateIfNeeded(ref m_VBufferDensityHandle, desc, FilterMode.Point, name: "_VBufferDensity");
            RenderingUtils.ReAllocateIfNeeded(ref m_VBufferLightingHandle, desc, FilterMode.Point, name: "_VBufferLighting");

            m_FilteringNeedsExtraBuffer = !(SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.LoadStore));

            // Filtering
            if (m_Config.filterVolume && m_FilteringNeedsExtraBuffer)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_VBufferLightingFilteredHandle, desc, FilterMode.Point, name: "VBufferLightingFiltered");
                CoreUtils.SetKeyword(m_VolumetricLightingFilteringCS, "NEED_SEPARATE_OUTPUT", m_FilteringNeedsExtraBuffer);
            }

            // History buffer
            if (NeedHistoryBufferAllocation())
            {
                DestroyHistoryBuffers();
                if (m_Config.enableReprojection)
                {
                    CreateHistoryBuffers(camera);
                }
                m_HistoryBufferAllocated = m_Config.enableReprojection;
            }

            // Set shader variables
            SetFogShaderVariablesRG(cmd, camera, renderingData.cullResults);
            SetVolumetricShaderVariablesRG(cmd, renderingData.cameraData);

            s_VolumeVoxelizationCSKernal = m_VolumeVoxelizationCS.FindKernel("VolumeVoxelization");
            s_VBufferLightingCSKernal = m_VolumetricLightingCS.FindKernel("VolumetricLighting");
            s_VBufferFilteringCSKernal = m_VolumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");

            // Local Volumes
            LocalVolumetricFog.RefreshVolumes();
            m_LocalVolumes = LocalVolumetricFog.SortVolumes();
        }
        private void SetFogShaderVariablesRG(CommandBuffer cmd, Camera camera, CullingResults renderingDataCulling)
        {
            float extinction = 1.0f / m_Config.fogAttenuationDistance;
            Vector3 scattering = extinction * (Vector3)(Vector4)m_Config.albedo;
            float layerDepth = Mathf.Max(0.01f, m_Config.maximumHeight - m_Config.baseHeight);
            float H = VolumetricUtils.ScaleHeightFromLayerDepth(layerDepth);
            Vector2 heightFogExponents = new Vector2(1.0f / H, H);

            bool useSkyColor = m_Config.colorMode == FogColorMode.SkyColor;

            m_FogCB._FogEnabled = m_Config.enabled ? 1u : 0u;
            m_FogCB._EnableVolumetricFog = m_Config.volumetricLighting ? 1u : 0u;
            m_FogCB._FogColorMode = useSkyColor ? 1u : 0u;
            m_FogCB._MaxEnvCubemapMip = (uint)VolumetricUtils.CalculateMaxEnvCubemapMip();
            m_FogCB._FogColor = useSkyColor ? m_Config.tint : m_Config.color;
            m_FogCB._MipFogParameters = new Vector4(m_Config.mipFogNear, m_Config.mipFogFar, m_Config.mipFogMaxMip, 0);
            m_FogCB._HeightFogParams = new Vector4(m_Config.baseHeight, extinction, heightFogExponents.x, heightFogExponents.y);
            m_FogCB._HeightFogBaseScattering = m_Config.volumetricLighting ? scattering : Vector4.one * extinction;

            //v0.1            
            m_FogCB._CullLightCount = (uint)renderingDataCulling.visibleLights.Length-1;// (uint)renderingData.cullResults.visibleLights.Length;
           // Debug.Log("visibleLights = " + m_FogCB._CullLightCount);

            ConstantBuffer.PushGlobal(cmd, m_FogCB, IDs._ShaderVariablesFog);
        }
        private void SetVolumetricShaderVariablesRG(CommandBuffer cmd, UniversalCameraData cameraData)
        {
            var camera = cameraData.camera;
            var vBufferViewportSize = m_VBufferParameters.viewportSize;
            var vFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var unitDepthTexelSpacing = VolumetricUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, vBufferViewportSize.y);

            VolumetricUtils.GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = m_FrameIndex % 7;
            var xySeqOffset = new Vector4();
            xySeqOffset.Set(m_xySeq[sampleIndex].x * m_Config.sampleOffsetWeight, m_xySeq[sampleIndex].y * m_Config.sampleOffsetWeight, VolumetricUtils.zSeq[sampleIndex], m_FrameIndex);

            // VolumetricUtils.GetPixelCoordToViewDirWSRG(cameraData, new Vector4(Screen.width, Screen.height, 1f / Screen.width, 1f / Screen.height), ref m_PixelCoordToViewDirWS);
            VolumetricUtils.GetPixelCoordToViewDirWSRG(cameraData, new Vector4(Screen.width, Screen.height, 1f / Screen.width, 1f / Screen.height), ref m_PixelCoordToViewDirWS);
            var viewportSize = new Vector4(vBufferViewportSize.x, vBufferViewportSize.y, 1.0f / vBufferViewportSize.x, 1.0f / vBufferViewportSize.y);
            //VolumetricUtils.GetPixelCoordToViewDirWSRG(cameraData, viewportSize, ref m_VBufferCoordToViewDirWS);
            VolumetricUtils.GetPixelCoordToViewDirWSRG(cameraData, viewportSize, ref m_VBufferCoordToViewDirWS);

            m_VolumetricLightingCB._VolumetricFilteringEnabled = m_Config.filterVolume ? 1u : 0u;
            m_VolumetricLightingCB._VBufferHistoryIsValid = (m_Config.enableReprojection && m_VBufferHistoryIsValid) ? 1u : 0u;
            m_VolumetricLightingCB._VBufferSliceCount = (uint)vBufferViewportSize.z;
            m_VolumetricLightingCB._VBufferAnisotropy = m_Config.anisotropy;
            m_VolumetricLightingCB._CornetteShanksConstant = VolumetricUtils.CornetteShanksPhasePartConstant(m_Config.anisotropy);
            m_VolumetricLightingCB._VBufferVoxelSize = m_VBufferParameters.voxelSize;
            m_VolumetricLightingCB._VBufferRcpSliceCount = 1f / vBufferViewportSize.z;
            m_VolumetricLightingCB._VBufferUnitDepthTexelSpacing = unitDepthTexelSpacing;
            m_VolumetricLightingCB._VBufferScatteringIntensity = m_Config.directionalScatteringIntensity;
            m_VolumetricLightingCB._VBufferLocalScatteringIntensity = m_Config.localScatteringIntensity;
            m_VolumetricLightingCB._VBufferLastSliceDist = m_VBufferParameters.ComputeLastSliceDistance((uint)vBufferViewportSize.z);
            m_VolumetricLightingCB._VBufferViewportSize = viewportSize;
            m_VolumetricLightingCB._VBufferLightingViewportScale = m_VBufferParameters.ComputeViewportScale(vBufferViewportSize);
            m_VolumetricLightingCB._VBufferLightingViewportLimit = m_VBufferParameters.ComputeViewportLimit(vBufferViewportSize);
            m_VolumetricLightingCB._VBufferDistanceEncodingParams = m_VBufferParameters.depthEncodingParams;
            m_VolumetricLightingCB._VBufferDistanceDecodingParams = m_VBufferParameters.depthDecodingParams;
            m_VolumetricLightingCB._VBufferSampleOffset = xySeqOffset;
#if UNITY_EDITOR    // _RTHandleScale is different for scend & game view.
            m_VolumetricLightingCB._VLightingRTHandleScale = Vector4.one;
#else
            m_VolumetricLightingCB._VLightingRTHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
#endif
            m_VolumetricLightingCB._VBufferCoordToViewDirWS = m_VBufferCoordToViewDirWS[0];

            ConstantBuffer.PushGlobal(cmd, m_VolumetricLightingCB, IDs._ShaderVariablesVolumetricLighting);

            cmd.SetGlobalTexture(IDs._VBufferLighting, m_VBufferLightingHandle);
            cmd.SetGlobalMatrix(IDs._PixelCoordToViewDirWS, m_PixelCoordToViewDirWS);

            CoreUtils.SetKeyword(m_VolumetricLightingCS, "ENABLE_REPROJECTION", m_Config.enableReprojection);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "ENABLE_ANISOTROPY", m_Config.anisotropy != 0f);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_DIRECTIONAL_LIGHTS", m_Config.enableDirectionalLight);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", m_Config.enablePointAndSpotLight);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_ADDITIONAL_SHADOWS", m_Config.enableAdditionalShadow);
        }



#endif










        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)//public void OnCameraSetupA(CommandBuffer cmd, PassData renderingData)//public override void OnCameraSetupA(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (Camera.main == null)
            {
                return;
            }


            if (m_VolumeVoxelizationCS == null
                || m_VolumetricLightingCS == null
                || m_VolumetricLightingFilteringCS == null)
                return;


            var vBufferViewportSize = m_VBufferParameters.viewportSize;
            var camera = renderingData.cameraData.camera;

            // Create render texture
            var desc = new RenderTextureDescriptor(vBufferViewportSize.x, vBufferViewportSize.y, RenderTextureFormat.ARGBHalf, 0);
            desc.dimension = TextureDimension.Tex3D;
            desc.volumeDepth = vBufferViewportSize.z;
            desc.enableRandomWrite = true;
            RenderingUtils.ReAllocateIfNeeded(ref m_VBufferDensityHandle, desc, FilterMode.Point, name:"_VBufferDensity");
            RenderingUtils.ReAllocateIfNeeded(ref m_VBufferLightingHandle, desc, FilterMode.Point, name:"_VBufferLighting");

            m_FilteringNeedsExtraBuffer = !(SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.LoadStore));

            // Filtering
            if (m_Config.filterVolume && m_FilteringNeedsExtraBuffer)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_VBufferLightingFilteredHandle, desc, FilterMode.Point, name:"VBufferLightingFiltered");
                CoreUtils.SetKeyword(m_VolumetricLightingFilteringCS, "NEED_SEPARATE_OUTPUT", m_FilteringNeedsExtraBuffer);
            }

            // History buffer
            if (NeedHistoryBufferAllocation())
            {
                DestroyHistoryBuffers();
                if (m_Config.enableReprojection)
                {
                    CreateHistoryBuffers(camera);
                }
                m_HistoryBufferAllocated = m_Config.enableReprojection;
            }

            // Set shader variables
            SetFogShaderVariables(cmd, camera, renderingData);
            SetVolumetricShaderVariables(cmd, renderingData.cameraData);

            s_VolumeVoxelizationCSKernal = m_VolumeVoxelizationCS.FindKernel("VolumeVoxelization");
            s_VBufferLightingCSKernal = m_VolumetricLightingCS.FindKernel("VolumetricLighting");
            s_VBufferFilteringCSKernal = m_VolumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");

            // Local Volumes
            LocalVolumetricFog.RefreshVolumes();
            m_LocalVolumes = LocalVolumetricFog.SortVolumes();
        }

        private void SetFogShaderVariables(CommandBuffer cmd, Camera camera, RenderingData renderingData)
        {
            float extinction = 1.0f / m_Config.fogAttenuationDistance;
            Vector3 scattering = extinction * (Vector3)(Vector4)m_Config.albedo;
            float layerDepth = Mathf.Max(0.01f, m_Config.maximumHeight - m_Config.baseHeight);
            float H = VolumetricUtils.ScaleHeightFromLayerDepth(layerDepth);
            Vector2 heightFogExponents = new Vector2(1.0f / H, H);

            bool useSkyColor = m_Config.colorMode == FogColorMode.SkyColor;

            m_FogCB._FogEnabled = m_Config.enabled ? 1u : 0u;
            m_FogCB._EnableVolumetricFog = m_Config.volumetricLighting ? 1u : 0u;
            m_FogCB._FogColorMode = useSkyColor ? 1u : 0u;
            m_FogCB._MaxEnvCubemapMip = (uint)VolumetricUtils.CalculateMaxEnvCubemapMip();
            m_FogCB._FogColor = useSkyColor ? m_Config.tint : m_Config.color;
            m_FogCB._MipFogParameters = new Vector4(m_Config.mipFogNear, m_Config.mipFogFar, m_Config.mipFogMaxMip, 0);
            m_FogCB._HeightFogParams = new Vector4(m_Config.baseHeight, extinction, heightFogExponents.x, heightFogExponents.y);
            m_FogCB._HeightFogBaseScattering = m_Config.volumetricLighting ? scattering : Vector4.one * extinction;

            //v0.1
            m_FogCB._CullLightCount = (uint)renderingData.cullResults.visibleLights.Length-1;

            ConstantBuffer.PushGlobal(cmd, m_FogCB, IDs._ShaderVariablesFog);
        }

        private void SetVolumetricShaderVariables(CommandBuffer cmd, CameraData cameraData)
        {
            var camera = cameraData.camera;
            var vBufferViewportSize = m_VBufferParameters.viewportSize;
            var vFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var unitDepthTexelSpacing = VolumetricUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, vBufferViewportSize.y);

            VolumetricUtils.GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = m_FrameIndex % 7;
            var xySeqOffset = new Vector4();
            xySeqOffset.Set(m_xySeq[sampleIndex].x * m_Config.sampleOffsetWeight, m_xySeq[sampleIndex].y * m_Config.sampleOffsetWeight, VolumetricUtils.zSeq[sampleIndex], m_FrameIndex);

            VolumetricUtils.GetPixelCoordToViewDirWS(cameraData, new Vector4(Screen.width, Screen.height, 1f / Screen.width, 1f / Screen.height), ref m_PixelCoordToViewDirWS);
            var viewportSize = new Vector4(vBufferViewportSize.x, vBufferViewportSize.y, 1.0f / vBufferViewportSize.x, 1.0f / vBufferViewportSize.y);
            VolumetricUtils.GetPixelCoordToViewDirWS(cameraData, viewportSize, ref m_VBufferCoordToViewDirWS);


            m_VolumetricLightingCB._VolumetricFilteringEnabled = m_Config.filterVolume ? 1u : 0u;
            m_VolumetricLightingCB._VBufferHistoryIsValid = (m_Config.enableReprojection && m_VBufferHistoryIsValid) ? 1u : 0u;
            m_VolumetricLightingCB._VBufferSliceCount = (uint)vBufferViewportSize.z;
            m_VolumetricLightingCB._VBufferAnisotropy = m_Config.anisotropy;
            m_VolumetricLightingCB._CornetteShanksConstant = VolumetricUtils.CornetteShanksPhasePartConstant(m_Config.anisotropy);
            m_VolumetricLightingCB._VBufferVoxelSize = m_VBufferParameters.voxelSize;
            m_VolumetricLightingCB._VBufferRcpSliceCount = 1f / vBufferViewportSize.z;
            m_VolumetricLightingCB._VBufferUnitDepthTexelSpacing = unitDepthTexelSpacing;
            m_VolumetricLightingCB._VBufferScatteringIntensity = m_Config.directionalScatteringIntensity;
            m_VolumetricLightingCB._VBufferLocalScatteringIntensity = m_Config.localScatteringIntensity;
            m_VolumetricLightingCB._VBufferLastSliceDist = m_VBufferParameters.ComputeLastSliceDistance((uint)vBufferViewportSize.z);
            m_VolumetricLightingCB._VBufferViewportSize = viewportSize;
            m_VolumetricLightingCB._VBufferLightingViewportScale = m_VBufferParameters.ComputeViewportScale(vBufferViewportSize);
            m_VolumetricLightingCB._VBufferLightingViewportLimit = m_VBufferParameters.ComputeViewportLimit(vBufferViewportSize);
            m_VolumetricLightingCB._VBufferDistanceEncodingParams = m_VBufferParameters.depthEncodingParams;
            m_VolumetricLightingCB._VBufferDistanceDecodingParams = m_VBufferParameters.depthDecodingParams;
            m_VolumetricLightingCB._VBufferSampleOffset = xySeqOffset;
#if UNITY_EDITOR    // _RTHandleScale is different for scend & game view.
            m_VolumetricLightingCB._VLightingRTHandleScale = Vector4.one;
#else
            m_VolumetricLightingCB._VLightingRTHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
#endif
            m_VolumetricLightingCB._VBufferCoordToViewDirWS = m_VBufferCoordToViewDirWS[0];

            ConstantBuffer.PushGlobal(cmd, m_VolumetricLightingCB, IDs._ShaderVariablesVolumetricLighting);

            cmd.SetGlobalTexture(IDs._VBufferLighting, m_VBufferLightingHandle);
            cmd.SetGlobalMatrix(IDs._PixelCoordToViewDirWS, m_PixelCoordToViewDirWS);

            CoreUtils.SetKeyword(m_VolumetricLightingCS, "ENABLE_REPROJECTION", m_Config.enableReprojection);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "ENABLE_ANISOTROPY", m_Config.anisotropy != 0f);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_DIRECTIONAL_LIGHTS", m_Config.enableDirectionalLight);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", m_Config.enablePointAndSpotLight);
            CoreUtils.SetKeyword(m_VolumetricLightingCS, "SUPPORT_ADDITIONAL_SHADOWS", m_Config.enableAdditionalShadow);
        }

        private void UpdateVolumeShaderVariables(ref ShaderVariablesLocalVolume cb, LocalVolumetricFog volume, Camera camera)
        {
            var obb = volume.GetOBB();
            var engineData = volume.ConvertToEngineData();
            cb._VolumetricMaterialObbRight = obb.right;
            cb._VolumetricMaterialObbUp = obb.up;
            cb._VolumetricMaterialObbExtents = new Vector3(obb.extentX, obb.extentY, obb.extentZ);
            cb._VolumetricMaterialObbCenter = obb.center;
            
            cb._VolumetricMaterialAlbedo = engineData.albedo;
            cb._VolumetricMaterialExtinction = engineData.extinction;
            
            cb._VolumetricMaterialRcpPosFaceFade = engineData.rcpPosFaceFade;
            cb._VolumetricMaterialRcpNegFaceFade = engineData.rcpNegFaceFade;
            cb._VolumetricMaterialInvertFade = engineData.invertFade;

            cb._VolumetricMaterialRcpDistFadeLen = engineData.rcpDistFadeLen;
            cb._VolumetricMaterialEndTimesRcpDistFadeLen = engineData.endTimesRcpDistFadeLen;
            cb._VolumetricMaterialFalloffMode = (int)engineData.falloffMode;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_FrameIndex = (m_FrameIndex + 1) % 14;

            var camera = renderingData.cameraData.camera;
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var voxelSize = m_VBufferParameters.voxelSize;
                var vBufferViewportSize = m_VBufferParameters.viewportSize;

                // The shader defines GROUP_SIZE_1D = 8.
                int width = ((int)vBufferViewportSize.x + 7) / 8;
                int height = ((int)vBufferViewportSize.y + 7) / 8;

                // VBuffer Density
                if (m_VolumeVoxelizationCS != null)
                {
                    cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, s_VolumeVoxelizationCSKernal, IDs._VBufferDensity, m_VBufferDensityHandle);
                    cmd.DispatchCompute(m_VolumeVoxelizationCS, s_VolumeVoxelizationCSKernal, width, height, 1);
                }

                // Local Volume material
                if (m_LocalVolumes != null && m_LocalVolumes.Length > 0)
                {
                    for (int i = 0; i < Math.Min(m_LocalVolumes.Length, 4); i++)
                    {
                        var shaderSetting = m_LocalVolumes[i].volumeShaderSetting;
                        bool isShaderValid = shaderSetting.shader != null;
                        var cs = isShaderValid ? shaderSetting.shader : m_Config.defaultLocalVolumeShader;

                        if (cs != null)
                        {
                            // Update RT if implemented in child volume class
                            bool hasRTUpdated = m_LocalVolumes[i].UpdateRenderTextureIfNeeded(context, cmd, ref renderingData);

                            // Compute density
                            var kernel = isShaderValid ? cs.FindKernel(shaderSetting.kernelName) : cs.FindKernel("SmokeVolumeMaterial");
                            UpdateVolumeShaderVariables(ref m_LocalVolumeCB, m_LocalVolumes[i], camera);
                            // TODO: Set properties to shader from child local volumetric fog setting
                            m_LocalVolumes[i].SetComputeShaderProperties(cmd, cs, kernel);
                            cmd.SetComputeTextureParam(cs, kernel, IDs._VBufferDensity, m_VBufferDensityHandle);
                            ConstantBuffer.Push(cmd, m_LocalVolumeCB, cs, IDs._ShaderVariablesLocalVolume);
                            cmd.DispatchCompute(cs, kernel, width, height, 1);

                            // Rollback render target
                            if (hasRTUpdated)
                            {
                                CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, ClearFlag.None);
                                context.ExecuteCommandBuffer(cmd);
                                cmd.Clear();
                            }
                        }
                    }
                }

                // VBuffer Lighting
                if (m_VolumetricLightingCS != null
                    && m_VolumetricLightingFilteringCS != null
                    && Shader.GetGlobalTexture("_CameraDepthTexture") != null && Shader.GetGlobalTexture("_MaxZMaskTexture") != null)   // To prevent error log
                {
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferDensity, m_VBufferDensityHandle);
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferLighting, m_VBufferLightingHandle);
                    if (m_Config.enableReprojection)
                    {
                        var currIdx = (m_FrameIndex + 0) & 1;
                        var prevIdx = (m_FrameIndex + 1) & 1;
                        cmd.SetComputeVectorParam(m_VolumetricLightingCS, IDs._PrevCamPosRWS, m_PrevCamPosRWS);
                        cmd.SetComputeMatrixParam(m_VolumetricLightingCS, IDs._PrevMatrixVP, m_PrevMatrixVP);
                        cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferFeedback, m_VolumetricHistoryBuffers[currIdx]);
                        cmd.SetComputeTextureParam(m_VolumetricLightingCS, s_VBufferLightingCSKernal, IDs._VBufferHistory, m_VolumetricHistoryBuffers[prevIdx]);
                    }
                    cmd.DispatchCompute(m_VolumetricLightingCS, s_VBufferLightingCSKernal, width, height, 1);

                    if (m_Config.filterVolume)
                    {
                        cmd.SetComputeTextureParam(m_VolumetricLightingFilteringCS, s_VBufferFilteringCSKernal, IDs._VBufferLighting, m_VBufferLightingHandle);
                        if (m_FilteringNeedsExtraBuffer)
                        {
                            cmd.SetComputeTextureParam(m_VolumetricLightingFilteringCS, s_VBufferFilteringCSKernal, IDs._VBufferLightingFiltered, m_VBufferLightingFilteredHandle);
                        }
                        cmd.DispatchCompute(m_VolumetricLightingFilteringCS, s_VBufferLightingCSKernal,
                                            VolumetricUtils.DivRoundUp((int)vBufferViewportSize.x, 8),
                                            VolumetricUtils.DivRoundUp((int)vBufferViewportSize.y, 8),
                                            m_VBufferParameters.viewportSize.z);
                    }
                }

                if (m_ResolveMat != null)
                {                    
                    CoreUtils.DrawFullScreen(cmd, m_ResolveMat);
                }

            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (m_Config.enableReprojection && !m_VBufferHistoryIsValid)
            {
                m_VBufferHistoryIsValid = true;
            }

            // Set prev cam data
            m_PrevCamPosRWS = camera.transform.position;
            VolumetricUtils.SetCameraMatrices(renderingData.cameraData, out var v, out var p, out m_PrevMatrixVP, out var invvp);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        private void CreateHistoryBuffers(Camera camera)
        {
            if (!m_Config.volumetricLighting)
                return;
            
            Debug.Assert(m_VolumetricHistoryBuffers == null);

            m_VolumetricHistoryBuffers = new RTHandle[2];
            var viewportSize = m_VBufferParameters.viewportSize;

            for (int i = 0; i < 2; i++)
            {
                m_VolumetricHistoryBuffers[i] = RTHandles.Alloc(viewportSize.x, viewportSize.y, viewportSize.z, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: string.Format("VBufferHistory{0}", i));
            }

            m_VBufferHistoryIsValid = false;
        }

        private void DestroyHistoryBuffers()
        {
            if (m_VolumetricHistoryBuffers == null)
                return;

            for (int i = 0; i < 2; i++)
            {
                RTHandles.Release(m_VolumetricHistoryBuffers[i]);
            }

            m_VolumetricHistoryBuffers = null;
            m_VBufferHistoryIsValid = false;
        }

        private bool NeedHistoryBufferAllocation()
        {
            if (!m_Config.volumetricLighting || !m_Config.enableReprojection)
                return false;
            
            if (m_VolumetricHistoryBuffers == null)
                return true;

            if (m_HistoryBufferAllocated != m_Config.enableReprojection)
                return true;

            var viewportSize = m_VBufferParameters.viewportSize;
            if (m_VolumetricHistoryBuffers[0].rt.width != viewportSize.x ||
                m_VolumetricHistoryBuffers[0].rt.height != viewportSize.y ||
                m_VolumetricHistoryBuffers[0].rt.volumeDepth != viewportSize.z)
                return true;
            
            return false;
        }
    }
}

