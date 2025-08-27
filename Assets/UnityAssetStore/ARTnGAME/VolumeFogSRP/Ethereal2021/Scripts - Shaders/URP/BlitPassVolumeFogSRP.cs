using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
#if UNITY_2023_3_OR_NEWER
//GRAPH


//using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;
#endif

using Artngame.SKYMASTER;

namespace Artngame.SKYMASTER // UnityEngine.Rendering.Universal// LWRP //v1.1.8n
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    internal class BlitPassVolumeFogSRP : UnityEngine.Rendering.Universal.ScriptableRenderPass
    {
#if UNITY_2023_3_OR_NEWER
        /// <summary>
        /// ///////// GRAPH
        /// </summary>
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            //v0.1
            //internal connectSuntoVolumeFogURP connector;
            //internal bool isForReflections;
            //internal bool isForDualCameras;
            //internal string m_ProfilerTag;
            //internal Material blitMaterial;
            //internal RenderTexture previousFrameTexture;
            //internal RenderTexture previousDepthTexture;
            //internal Matrix4x4 lastFrameViewProjectionMatrix;
            //internal Matrix4x4 viewProjectionMatrix;
            //internal Matrix4x4 lastFrameInverseViewProjectionMatrix;
            //internal int extraCameraID;
            //internal RTHandle _handleA; //UnityEngine.Rendering.Universal.RenderTargetHandle _handle; //v0.1
            //internal RTHandle m_CameraColorTarget;
            //internal TextureHandle tmpBuffer1;
            internal TextureHandle src;
            internal TextureHandle tmpBuffer1;
            // internal TextureHandle copySourceTexture;
            public Material BlitMaterial { get; set; }
           // public TextureHandle SourceTexture { get; set; }
        }
        private Material m_BlitMaterial;

        static void ExecuteBlitterPass(PassData data, RasterGraphContext context, UniversalRenderingData renderingData, ref UniversalCameraData cameraData)
        {   //GRAPH
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
        }
        //
        //static void ExecutePass(PassData data, RasterGraphContext context)
        //{
        //    // Records a rendering command to copy, or blit, the contents of the source texture to the color render target of the render pass.
        //    // The RecordRenderGraph method sets the destination texture as the render target with the UseTextureFragment method.
        //    Blitter.BlitTexture(context.cmd, data.copySourceTexture, new Vector4(1, 1, 0, 0), 0, false);
        //}

        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecuteBlitPass0(PassData data, RasterGraphContext context, UniversalRenderingData renderingData, 
            ref UniversalCameraData cameraData, Material blitMat, int pass, TextureHandle tmpBuffer1aa)
        {   //GRAPH
            //ExecuteA1(context, ref renderingData,ref  cameraData, data);
            //Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
            data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1aa);// data.src);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            //Debug.Log("ExecuteBlitPass");
        }
        static void ExecuteBlitPass6(PassData data, RasterGraphContext context, UniversalRenderingData renderingData, ref UniversalCameraData cameraData, Material blitMat)
        {   //GRAPH
            //ExecuteA1(context, ref renderingData,ref  cameraData, data);
            //Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, 6);
            //Debug.Log("ExecuteBlitPass");
        }
        TextureHandle tmpBuffer1A;
        TextureHandle tmpBuffer2A;
        TextureHandle tmpBuffer3A; //TextureHandle tmpBuffer3AA;

        TextureHandle tmpBuffer4A;
        TextureHandle tmpBuffer5A;
        TextureHandle tmpBuffer6A;
        TextureHandle tmpBuffer7A;

        TextureHandle previousFrameTextureA;
        TextureHandle previousDepthTextureA;
        public void RecordRenderGraphBLIT1(RenderGraph renderGraph, ContextContainer frameData, 
            RenderTextureDescriptor desc, UniversalCameraData cameraData, UniversalRenderingData renderingData, UniversalResourceData resourceData, Material blitMat,
            ref TextureHandle tmpBuffer1, int pass) {
            /////////////////////////////////////////////////////////////////
            string passName = "BLIT1 Keep Source";// "Copy To Debug Texture";
            // This simple pass copies the active color texture to a new texture. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.
            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                //passData.tmpBuffer1 = 
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into                
                // Get the active color texture through the frame data, and set it as the source texture for the blit
                passData.src = resourceData.activeColorTexture;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);              
                //passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);  
               // passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);
                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src,
                AccessFlags.Read);
                // Setup as a render target via UseTextureFragment, which is the equivalent of using the old cmd.SetRenderTarget
                //tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);
                // builder.SetRenderAttachment(tmpBuffer1, 0);//, IBaseRenderGraphBuilder.AccessFlags.None);
                builder.SetRenderAttachment(tmpBuffer1, 0, AccessFlags.Write);
                // We disable culling for this pass for the demonstrative purpose of this sampe, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);
                passData.BlitMaterial = m_BlitMaterial;
                //passData.BlitMaterial.SetTexture("_MainTex", tmpBuffer1A);
                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => 
                    ExecuteBlitPass0(data, context, renderingData, ref cameraData, blitMat, pass, passData.src));
            }
        }
        public void RecordRenderGraphBLIT6(RenderGraph renderGraph, ContextContainer frameData,
            RenderTextureDescriptor desc, UniversalCameraData cameraData, UniversalRenderingData renderingData, UniversalResourceData resourceData, Material blitMat,
            ref TextureHandle tmpBuffer1)
        {
            /////////////////////////////////////////////////////////////////
            string passName = "BLIT1 Keep Source1";// "Copy To Debug Texture";
            // This simple pass copies the active color texture to a new texture. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.
            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                //passData.tmpBuffer1 = 
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into                
                // Get the active color texture through the frame data, and set it as the source texture for the blit
                passData.src = resourceData.activeColorTexture;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);              
                //passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);  
                passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);
                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src,
                AccessFlags.Read);
                // Setup as a render target via UseTextureFragment, which is the equivalent of using the old cmd.SetRenderTarget
                //tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);
                // builder.SetRenderAttachment(tmpBuffer1, 0);//, IBaseRenderGraphBuilder.AccessFlags.None);
                builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                // We disable culling for this pass for the demonstrative purpose of this sampe, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);
                passData.BlitMaterial = m_BlitMaterial;
                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteBlitPass6(data, context, renderingData, ref cameraData, blitMat));
            }
        }



        // This is where the renderGraph handle can be accessed.
        public bool TAAtexturesCreated = false;
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            //grab settings if script on scene camera
            if (connector == null)
            {
                if (connector == null && Camera.main != null)
                {
                    connector = Camera.main.GetComponent<connectSuntoVolumeFogURP>();

                    //v0.2
                    if (connector == null)
                    {
                        try
                        {
                            GameObject effects = GameObject.FindWithTag("SkyMasterEffects");
                            if (effects != null)
                            {
                                connector = effects.GetComponent<connectSuntoVolumeFogURP>();
                            }
                        }
                        catch
                        { }
                    }

                }
            }
            //Debug.Log(Camera.main.GetComponent<connectSuntoVolumeFogURP>().sun.transform.position);
            if (inheritFromController && connector != null)
            {
                this.enableFog = connector.enableFog;
                if (!connector.enabled)
                {
                    this.enableFog = false;
                }

                this.sunTransform = new Vector3(connector.Sun.x, connector.Sun.y, connector.Sun.z);// connector.sun.transform.position;
                this.screenBlendMode = connector.screenBlendMode;
                //public Vector3 sunTransform = new Vector3(0f, 0f, 0f); 
                this.radialBlurIterations = connector.radialBlurIterations;
                this.sunColor = connector.sunColor;
                this.sunThreshold = connector.sunThreshold;
                this.sunShaftBlurRadius = connector.sunShaftBlurRadius;
                this.sunShaftIntensity = connector.sunShaftIntensity;
                this.maxRadius = connector.maxRadius;
                this.useDepthTexture = connector.useDepthTexture;

                ////// VOLUME FOG URP /////////////////
                //FOG URP /////////////
                //FOG URP /////////////
                //FOG URP /////////////
                //this.blend =  0.5f;
                this._FogColor = connector._FogColor;
                //fog params
                this.noiseTexture = connector.noiseTexture;
                this._startDistance = connector._startDistance;
                this._fogHeight = connector._fogHeight;
                this._fogDensity = connector._fogDensity;
                this._cameraRoll = connector._cameraRoll;
                this._cameraDiff = connector._cameraDiff;
                this._cameraTiltSign = connector._cameraTiltSign;
                this.heightDensity = connector.heightDensity;
                this.noiseDensity = connector.noiseDensity;
                this.noiseScale = connector.noiseScale;
                this.noiseThickness = connector.noiseThickness;
                this.noiseSpeed = connector.noiseSpeed;
                this.occlusionDrop = connector.occlusionDrop;
                this.occlusionExp = connector.occlusionExp;
                this.noise3D = connector.noise3D;
                this.startDistance = connector.startDistance;
                this.luminance = connector.luminance;
                this.lumFac = connector.lumFac;
                this.ScatterFac = connector.ScatterFac;
                this.TurbFac = connector.TurbFac;
                this.HorizFac = connector.HorizFac;
                this.turbidity = connector.turbidity;
                this.reileigh = connector.reileigh;
                this.mieCoefficient = connector.mieCoefficient;
                this.mieDirectionalG = connector.mieDirectionalG;
                this.bias = connector.bias;
                this.contrast = connector.contrast;
                this.TintColor = connector.TintColor;
                this.TintColorK = connector.TintColorK;
                this.TintColorL = connector.TintColorL;
                this.Sun = connector.Sun;
                this.FogSky = connector.FogSky;
                this.ClearSkyFac = connector.ClearSkyFac;
                this.PointL = connector.PointL;
                this.PointLParams = connector.PointLParams;
                this._useRadialDistance = connector._useRadialDistance;
                this._fadeToSkybox = connector._fadeToSkybox;

                this.allowHDR = connector.allowHDR;
                //END FOG URP //////////////////
                //END FOG URP //////////////////
                //END FOG URP //////////////////
                ////// END VOLUME FOG URP /////////////////

                //v1.3.0
                GlobalFogPower = connector.GlobalFogPower;
                GlobalFogNoisePower = connector.GlobalFogNoisePower;
                VolumeLightNoisePower = connector.VolumeLightNoisePower;
                useTexture3DNoise = connector.useTexture3DNoise;
                _NoiseTex1 = connector._NoiseTex1;
                localLightAttenuation = connector.localLightAttenuation;//v1.4.3

                this.blendVolumeLighting = connector.blendVolumeLighting;
                this.LightRaySamples = connector.LightRaySamples;
                this.stepsControl = connector.stepsControl;
                this.lightNoiseControl = connector.lightNoiseControl;

                //v1.6
                this.reflectCamera = connector.reflectCamera;

                //v2.0
                this.useOnlyFog = connector.useOnlyFog;
                this.infiniteLocalLigths = connector.infiniteLocalLigths;

                //v1.7
                this.lightCount = connector.lightCount;

                //v1.9.9
                this.lightControlA = connector.lightControlA;
                this.lightControlB = connector.lightControlB;
                this.controlByColor = connector.controlByColor;
                this.lightA = connector.lightA;
                this.lightB = connector.lightB;

                //v2.0
                this.maxImpostorLights = connector.maxImpostorLights;

                //v1.9.9
                this.lightsArray = connector.lightsArray;

                //v1.9.9.2
                this.enableSunShafts = connector.enableSunShafts;

                //v1.9.9.3
                this.shadowsControl = connector.shadowsControl;

                //v1.9.9.4
                this.volumeSamplingControl = connector.volumeSamplingControl;

                //v1.9.9.7 - Ethereal v1.1.8f
                this.extraCameras = connector.extraCameras;

                //v0.6
                this.downSample = connector.downSample;
                this.depthDilation = connector.depthDilation;
                this.enabledTemporalAA = connector.enabledTemporalAA;
                this.TemporalResponse = connector.TemporalResponse;
                this.TemporalGain = connector.TemporalGain;

                //v0.6a
                this.enableBlendMode = connector.enableBlendMode;
                this.controlBackAlphaPower = connector.controlBackAlphaPower;
                this.controlCloudAlphaPower = connector.controlCloudAlphaPower;
                this.controlCloudEdgeA = connector.controlCloudEdgeA;

                //SSMS
                enableComposite = connector.enableComposite;
                downSampleAA = connector.downSampleAA;
                enableWetnessHaze = connector.enableWetnessHaze;
                //SSMS
                thresholdGamma = connector._threshold;
                thresholdLinear = connector._threshold;
                _threshold = connector._threshold;
                softKnee = connector._softKnee;
                _softKnee = connector._softKnee;
                radius = connector._radius;
                _radius = connector._radius;
                blurWeight = connector._blurWeight;
                _blurWeight = connector._blurWeight;
                intensity = connector.intensity;
                _intensity = connector.intensity;
                highQuality = connector._highQuality;
                _highQuality = connector._highQuality;
                _antiFlicker = connector._antiFlicker;
                antiFlicker = connector._antiFlicker;
                _fadeRamp = connector._fadeRamp;
                fadeRamp = connector._fadeRamp;
                _blurTint = connector._blurTint;
                blurTint = connector._blurTint;
            }
            //if still null, disable effect
            bool connectorFound = true;
            if (connector == null)
            {
                connectorFound = false;
            }
            

            if (connectorFound &&  connector.enableFog && enableFog &&  (cameraData.camera == Camera.main || isForReflections || isForDualCameras))
            {                               

                //passData.tmpBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1", false);
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                int rtW = desc.width;
                int rtH = desc.height;
                int xres = (int)(rtW / ((float)downSample));
                int yres = (int)(rtH / ((float)downSample));
                if (_handleA == null || _handleA.rt.width != xres || _handleA.rt.height != yres)
                {
                    _handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }

                xres = rtW;
                yres = rtH;

                if (_handleB == null || _handleB.rt.width != xres || _handleB.rt.height != yres)
                {
                    _handleB = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                if (_handleC == null || _handleC.rt.width != xres || _handleC.rt.height != yres)
                {
                    _handleC = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                tmpBuffer2A = renderGraph.ImportTexture(_handleA);
                previousFrameTextureA = renderGraph.ImportTexture(_handleB);
                previousDepthTextureA = renderGraph.ImportTexture(_handleC);
                TextureDesc descA = new TextureDesc(rtW, rtH);
                // tmpBuffer1A = renderGraph.CreateTexture(descA);
                //tmpBuffer2A = renderGraph.CreateTexture(descA);

                descA.colorFormat = desc.graphicsFormat;// GraphicsFormat.R32G32B32A32_SFloat;// desc.colorFormat;
                descA.dimension = desc.dimension;// TextureDimension.Tex2D;
                descA.slices = 1;
                // tmpBuffer3AA = renderGraph.CreateTexture(descA);
                //tmpBuffer3AA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer3AA", true, FilterMode.Trilinear);

                TextureHandle tmpBuffer3AA = renderGraph.CreateTexture(descA);
                /*
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[0] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[0] = desc.width;
                    _blurBuffer1NSIZEY[0] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[1] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[1] = desc.width;
                    _blurBuffer1NSIZEY[1] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[2] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[2] = desc.width;
                    _blurBuffer1NSIZEY[2] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[3] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[3] = desc.width;
                    _blurBuffer1NSIZEY[3] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[4] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[4] = desc.width;
                    _blurBuffer1NSIZEY[4] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[5] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[5] = desc.width;
                    _blurBuffer1NSIZEY[5] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[6] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[6] = desc.width;
                    _blurBuffer1NSIZEY[6] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[7] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[7] = desc.width;
                    _blurBuffer1NSIZEY[7] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[8] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[8] = desc.width;
                    _blurBuffer1NSIZEY[8] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[9] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[9] = desc.width;
                    _blurBuffer1NSIZEY[9] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[10] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[10] = desc.width;
                    _blurBuffer1NSIZEY[10] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0) {
                    _blurBuffer1N[11] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[11] = desc.width;
                    _blurBuffer1NSIZEY[11] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[12] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[12] = desc.width;
                    _blurBuffer1NSIZEY[12] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[13] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[13] = desc.width;
                    _blurBuffer1NSIZEY[13] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[14] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[14] = desc.width;
                    _blurBuffer1NSIZEY[14] = desc.height;
                }
                descA.width = descA.width / 2;
                descA.height = descA.height / 2;
                if (descA.width > 0 && descA.height > 0)
                {
                    _blurBuffer1N[15] = renderGraph.CreateTexture(descA);
                    _blurBuffer1NSIZEX[15] = desc.width;
                    _blurBuffer1NSIZEY[15] = desc.height;
                }
                // _blurBuffer1NSIZEX[level] = desc.width;
                // _blurBuffer1NSIZEY[level] = desc.height;
                */
                tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true, FilterMode.Point);
                //tmpBuffer2A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer2A", true);

                tmpBuffer3A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer3A", true, FilterMode.Point);
                // if (_handleF == null || _handleF.rt.width != xres || _handleF.rt.height != yres)
                // {
                //     _handleF = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2D);
                //}
                // tmpBuffer3A = renderGraph.ImportTexture(_handleF);


                //if (_handleAA == null || _handleAA.rt.width != rtW || _handleAA.rt.height != rtH)
                //{
                //    _handleAA = RTHandles.Alloc(rtW, rtH, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                //}
                //if (_handleBB == null || _handleBB.rt.width != rtW || _handleBB.rt.height != rtH)
                //{
                //    _handleBB = RTHandles.Alloc(rtW, rtH, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                //}
                // tmpBuffer1A = renderGraph.ImportTexture(_handleAA);
                // tmpBuffer3A = renderGraph.ImportTexture(_handleBB);

                //previousFrameTextureA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "previousFrameTextureA", true);
                //previousDepthTextureA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "previousDepthTextureA", true);
                // }

                //tmpBuffer4A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer4A", true);
                if (_handleD == null || _handleD.rt.width != xres || _handleD.rt.height != yres)
                {
                    _handleD = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                tmpBuffer4A = renderGraph.ImportTexture(_handleD);
                if (_handleE == null || _handleE.rt.width != rtW || _handleE.rt.height != rtH)
                {
                    _handleE = RTHandles.Alloc(rtW, rtH, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                tmpBuffer5A = renderGraph.ImportTexture(_handleE);

                if (_handleF == null || _handleF.rt.width != xres || _handleF.rt.height != yres)
                {
                    _handleF = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                tmpBuffer6A = renderGraph.ImportTexture(_handleF);

                if (_handleG == null || _handleG.rt.width != xres || _handleG.rt.height != yres)
                {
                    _handleG = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, filterMode: FilterMode.Bilinear);
                }
                tmpBuffer7A = renderGraph.ImportTexture(_handleG);

                TextureHandle sourceTexture = resourceData.activeColorTexture;








                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                RenderTextureDescriptor opaqueDesc = cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                //v0.1
                ///////////////////////////////////////////////////////////////// RENDER FOG
                Material _material = m_BlitMaterial;

                //v1.9.9.5 - Ethereal v1.1.8
                _material.SetInt("_visibleLightsCount", renderingData.cullResults.visibleLights.Length);

                _material.SetFloat("_DistanceOffset", _startDistance);
                _material.SetFloat("_Height", _fogHeight); //v0.1                                                                      
                _material.SetFloat("_cameraRoll", _cameraRoll);
                _material.SetVector("_cameraDiff", _cameraDiff);
                _material.SetFloat("_cameraTiltSign", _cameraTiltSign);

                var mode = RenderSettings.fogMode;
                if (mode == FogMode.Linear)
                {
                    var start = RenderSettings.fogStartDistance;//RenderSettings.RenderfogStartDistance;
                    var end = RenderSettings.fogEndDistance;
                    var invDiff = 1.0f / Mathf.Max(end - start, 1.0e-6f);
                    _material.SetFloat("_LinearGrad", -invDiff);
                    _material.SetFloat("_LinearOffs", end * invDiff);
                    _material.DisableKeyword("FOG_EXP");
                    _material.DisableKeyword("FOG_EXP2");
                }
                else if (mode == FogMode.Exponential)
                {
                    const float coeff = 1.4426950408f; // 1/ln(2)
                    var density = RenderSettings.fogDensity;// RenderfogDensity;
                    _material.SetFloat("_Density", coeff * density * _fogDensity);
                    _material.EnableKeyword("FOG_EXP");
                    _material.DisableKeyword("FOG_EXP2");
                }
                else // FogMode.ExponentialSquared
                {
                    const float coeff = 1.2011224087f; // 1/sqrt(ln(2))
                    var density = RenderSettings.fogDensity;//RenderfogDensity;
                    _material.SetFloat("_Density", coeff * density * _fogDensity);
                    _material.DisableKeyword("FOG_EXP");
                    _material.EnableKeyword("FOG_EXP2");
                }
                if (_useRadialDistance)
                    _material.EnableKeyword("RADIAL_DIST");
                else
                    _material.DisableKeyword("RADIAL_DIST");

                if (_fadeToSkybox)
                {
                    _material.DisableKeyword("USE_SKYBOX");
                    _material.SetColor("_FogColor", _FogColor);// RenderfogColor);//v0.1            
                }
                else
                {
                    _material.DisableKeyword("USE_SKYBOX");
                    _material.SetColor("_FogColor", _FogColor);// RenderfogColor);
                }

                //v0.1 - v1.9.9.2
                //if (noiseTexture == null)
                //{
                //    noiseTexture = new Texture2D(1280, 720);
                //}
                if (_material != null && noiseTexture != null)
                {
                    //if (noiseTexture == null)
                    //{
                    //    noiseTexture = new Texture2D(1280, 720);
                    //}
                    _material.SetTexture("_NoiseTex", noiseTexture);
                }

                // Calculate vectors towards frustum corners.
                Camera camera = Camera.main;

                if (isForReflections && reflectCamera != null)
                {
                    camera = reflectCamera;
                }

                if (isForReflections && isForDualCameras) //v1.9.9.7 - Ethereal v1.1.8f
                {
                    //if list has members, choose 0 for 1st etc
                    if (extraCameras.Count > 0 && extraCameraID >= 0 && extraCameraID < extraCameras.Count)
                    {
                        camera = extraCameras[extraCameraID];
                    }
                }

                //v1.7.1 - Solve editor flickering
                if (Camera.current != null)
                {
                    camera = Camera.current;
                }

                var cam = camera;// GetComponent<Camera>();
                var camtr = cam.transform;
                
                ////////// SCATTER
                var camPos = camtr.position;
                float FdotC = camPos.y - _fogHeight;
                float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
                
                _material.SetVector("_CameraWS", camPos);
                _material.SetFloat("blendVolumeLighting", blendVolumeLighting);//v0.2 - SHADOWS
                _material.SetFloat("_RaySamples", LightRaySamples);
                _material.SetVector("_stepsControl", stepsControl);
                _material.SetVector("lightNoiseControl", lightNoiseControl);

                //Debug.Log("_HeightParams="+ new Vector4(_fogHeight, FdotC, paramK, heightDensity * 0.5f));

                _material.SetVector("_HeightParams", new Vector4(_fogHeight, FdotC, paramK, heightDensity * 0.5f));
                _material.SetVector("_DistanceParams", new Vector4(-Mathf.Max(startDistance, 0.0f), 0, 0, 0));
                _material.SetFloat("_NoiseDensity", noiseDensity);
                _material.SetFloat("_NoiseScale", noiseScale);
                _material.SetFloat("_NoiseThickness", noiseThickness);
                _material.SetVector("_NoiseSpeed", noiseSpeed);
                _material.SetFloat("_OcclusionDrop", occlusionDrop);
                _material.SetFloat("_OcclusionExp", occlusionExp);
                _material.SetInt("noise3D", noise3D);
                //SM v1.7
                _material.SetFloat("luminance", luminance);
                _material.SetFloat("lumFac", lumFac);
                _material.SetFloat("Multiplier1", ScatterFac);
                _material.SetFloat("Multiplier2", TurbFac);
                _material.SetFloat("Multiplier3", HorizFac);
                _material.SetFloat("turbidity", turbidity);
                _material.SetFloat("reileigh", reileigh);
                _material.SetFloat("mieCoefficient", mieCoefficient);
                _material.SetFloat("mieDirectionalG", mieDirectionalG);
                _material.SetFloat("bias", bias);
                _material.SetFloat("contrast", contrast);

                //v1.7.1 - Solve editor flickering
                Vector3 sunDir = Sun;// connector.sun.transform.forward;
                if ((Camera.current != null || isForDualCameras) && connector.sun != null) //v1.9.9.2  //v1.9.9.6 - Ethereal v1.1.8e
                {
                    sunDir = connector.sun.transform.forward;
                    sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                    sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                    sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
                }

                _material.SetVector("v3LightDir", sunDir);// Sun);//.forward); //v1.7.1
                _material.SetVector("_TintColor", new Vector4(TintColor.r, TintColor.g, TintColor.b, 1));//68, 155, 345
                _material.SetVector("_TintColorK", new Vector4(TintColorK.x, TintColorK.y, TintColorK.z, 1));
                _material.SetVector("_TintColorL", new Vector4(TintColorL.x, TintColorL.y, TintColorL.z, 1));

                //v1.6 - reflections
                if (isForReflections && !isForDualCameras) //v1.9.9.6 - Ethereal v1.1.8e
                {
                    _material.SetFloat("_invertX", 1);
                }
                else
                {
                    _material.SetFloat("_invertX", 0);
                }

                //v2.0
                updateMaterialKeyword(useOnlyFog, "ONLY_FOG", _material);
                updateMaterialKeyword(infiniteLocalLigths, "INFINITE", _material);

                //v1.7
                _material.SetInt("lightCount", lightCount);

                //v1.3.0
                _material.SetFloat("GlobalFogPower", GlobalFogPower);
                _material.SetFloat("GlobalFogNoisePower", GlobalFogNoisePower);
                _material.SetFloat("VolumeLightNoisePower", VolumeLightNoisePower);
                _material.SetFloat("useTexture3DNoise", useTexture3DNoise ? 1 : 0);
                _material.SetTexture("_NoiseTex1", _NoiseTex1);
                _material.SetFloat("attenExponent", localLightAttenuation);//v1.4.3

                //v1.9.9
                _material.SetVector("lightControlA", lightControlA);
                _material.SetVector("lightControlB", lightControlB);
                if (lightA)
                {
                    _material.SetVector("lightAcolor", new Vector3(lightA.color.r, lightA.color.g, lightA.color.b));
                    _material.SetFloat("lightAIntensity", lightA.intensity);
                }
                if (lightB)
                {
                    _material.SetVector("lightBcolor", new Vector3(lightB.color.r, lightB.color.g, lightB.color.b));
                    _material.SetFloat("lightBIntensity", lightA.intensity);
                }
                if (controlByColor)
                {
                    _material.SetInt("controlByColor", 1);
                }
                else
                {
                    _material.SetInt("controlByColor", 0);
                }

                //v1.9.9.3
                _material.SetVector("shadowsControl", shadowsControl);

                //v1.9.9.4
                _material.SetVector("volumeSamplingControl", volumeSamplingControl);

                //v1.9.9.1
                // Debug.Log(_material.HasProperty("lightsArrayLength"));
                //Debug.Log(_material.HasProperty("controlByColor"));
                if (_material.HasProperty("lightsArrayLength") && lightsArray.Count > 0) //check for other shader versions
                {
                    //pass array
                    _material.SetVectorArray("_LightsArrayPos", new Vector4[maxImpostorLights]);
                    _material.SetVectorArray("_LightsArrayDir", new Vector4[maxImpostorLights]);
                    int countLights = lightsArray.Count;
                    if (countLights > maxImpostorLights)
                    {
                        countLights = maxImpostorLights;
                    }
                    _material.SetInt("lightsArrayLength", countLights);
                    //Debug.Log(countLights);
                    // material.SetFloatArray("_Points", new float[10]);
                    //float[] array = new float[] { 1, 2, 3, 4 };
                    Vector4[] posArray = new Vector4[countLights];
                    Vector4[] dirArray = new Vector4[countLights];
                    Vector4[] colArray = new Vector4[countLights];
                    for (int i = 0; i < countLights; i++)
                    {
                        //posArray[i].x = lightsArray(0).
                        posArray[i].x = lightsArray[i].transform.position.x;
                        posArray[i].y = lightsArray[i].transform.position.y;
                        posArray[i].z = lightsArray[i].transform.position.z;
                        posArray[i].w = lightsArray[i].intensity;
                        //Debug.Log(posArray[i].w);
                        colArray[i].x = lightsArray[i].color.r;
                        colArray[i].y = lightsArray[i].color.g;
                        colArray[i].z = lightsArray[i].color.b;

                        //check if point light
                        if (lightsArray[i].type == LightType.Point)
                        {
                            dirArray[i].x = 0;
                            dirArray[i].y = 0;
                            dirArray[i].z = 0;
                        }
                        else
                        {
                            dirArray[i].x = lightsArray[i].transform.forward.x;
                            dirArray[i].y = lightsArray[i].transform.forward.y;
                            dirArray[i].z = lightsArray[i].transform.forward.z;
                        }
                        dirArray[i].w = lightsArray[i].range;
                    }
                    _material.SetVectorArray("_LightsArrayPos", posArray);
                    _material.SetVectorArray("_LightsArrayDir", dirArray);
                    _material.SetVectorArray("_LightsArrayColor", colArray);
                    //material.SetFloatArray(array);
                }
                else
                {
                    _material.SetInt("lightsArrayLength", 0);
                }
                
                float Foggy = 0;
                if (FogSky) //ClearSkyFac
                {
                    Foggy = 1;
                }
                _material.SetFloat("FogSky", Foggy);
                _material.SetFloat("ClearSkyFac", ClearSkyFac);
                //////// END SCATTER

                //LOCAL LIGHT
                _material.SetVector("localLightPos", new Vector4(PointL.x, PointL.y, PointL.z, PointL.w));//68, 155, 345
                _material.SetVector("localLightColor", new Vector4(PointLParams.x, PointLParams.y, PointLParams.z, PointLParams.w));//68, 155, 345

                //v0.6
                _material.SetFloat("depthDilation", depthDilation);
                _material.SetFloat("_TemporalResponse", TemporalResponse);
                _material.SetFloat("_TemporalGain", TemporalGain);

                //v0.7 GRAPH
                _material.SetFloat("blackBackground", 0);

           

                //RENDER FINAL EFFECT
                //int rtW = opaqueDesc.width;
                //int rtH = opaqueDesc.height;
                //var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
                var format = allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9 //v LWRP //v0.7

                //CONVERT A1
                //RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                //RenderTexture.active = tmpBuffer1;
                //GL.ClearWithSkybox(false, camera);
                //cmd.Blit(source, tmpBuffer1); //v0.1

      //        RecordRenderGraphBLIT1(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material, ref tmpBuffer1A, 24);// 21);
                string passName = "BLIT1 Keep Source";
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                {
                    passData.src = resourceData.activeColorTexture;
                    desc.msaaSamples = 1;  desc.depthBufferBits = 0;
                    builder.UseTexture(passData.src, AccessFlags.Read);
                    builder.SetRenderAttachment(tmpBuffer1A, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    passData.BlitMaterial = _material;
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => 
                        ExecuteBlitPass(data, context, 24, passData.src));
                }

               

                ///// MORE 1
                //if (!enableComposite)
                //{
                //UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                //TextureHandle resHandle = resourceData.activeColorTexture;
                // if (tmpBuffer1A.IsValid())
                //   {
                //  _material.SetTexture("_MainTex", tmpBuffer1A);
                //   }

                //WORLD RECONSTRUCT        
                Matrix4x4 camToWorld = camera.cameraToWorldMatrix;
                _material.SetMatrix("_InverseView", camToWorld);

               

                //v0.6
                //int downSample = 2;
                //RenderTexture tmpBuffer2 = RenderTexture.GetTemporary((int)(rtW / downSample), (int)(rtH / downSample), 0, format);
                /////context.command.BlitFullscreenTriangle(context.source, context.destination, _material, 0);
                //Blit(cmd, m_TemporaryColorTexture.Identifier(), source, _material, (screenBlendMode == BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen) ? 0 : 4);
                //Blit(cmd, tmpBuffer1, source, _material, 6);//v0.6
                //v0.1
                //cmd.Blit(tmpBuffer1, tmpBuffer2, _material, 6);
                if (!enableComposite)
                {
                    //RecordRenderGraphBLIT6(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material, ref tmpBuffer1A);
                    //RecordRenderGraphBLIT1(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material, ref tmpBuffer2A, 6);
                    passName = "BLIT2";
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                    {
                        passData.src = resourceData.activeColorTexture;
                        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                        builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                        builder.AllowPassCulling(false);
                        passData.BlitMaterial = _material;
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPass(data, context, 6, tmpBuffer1A));
                    }
                
                    if (enabledTemporalAA && Time.fixedTime > 0.05f)
                    {
                        var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                        _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                        viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                        _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                        _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                        _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);
                        //_material.SetTexture("_ColorBuffer", tmpBuffer2);
                        //_material.SetTexture("_PreviousColor", previousFrameTexture);
                        //_material.SetTexture("_PreviousDepth", previousDepthTexture);

                        //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                        Matrix4x4 mt = lastFrameViewProjectionMatrix * cameraData.camera.cameraToWorldMatrix;
                        _material.SetMatrix("_FrameMatrix", mt);

                        //v0.2
                        //Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 20);
                        //cmd.Blit(_handleA, previousFrameTexture);
                        //cmd.Blit(_handleA, tmpBuffer2);
                        //Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 21);
                        //cmd.Blit(_handleA, previousDepthTexture);

                        passName = "BLIT_TAA";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                            builder.UseTexture(previousFrameTextureA, AccessFlags.Read);
                            builder.UseTexture(previousDepthTextureA, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer3A, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = _material;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                // ExecuteBlitPassTHREE(data, context, 20, tmpBuffer2A, previousFrameTextureA, previousDepthTextureA));
                                ExecuteBlitPassTEN(data, context, 20, tmpBuffer2A, previousFrameTextureA, previousDepthTextureA,
                                            "_TemporalResponse", TemporalResponse,
                                            "_TemporalGain", TemporalGain,
                                            "_InverseProjectionMatrix", projectionMatrix.inverse,
                                            "_InverseViewProjectionMatrix", viewProjectionMatrix.inverse,
                                            "_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix,
                                            "_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix,
                                            "_FrameMatrix", mt
                                            ));
                        }
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 1", out var passData, m_ProfilingSampler))
                        {
                            builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                           builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                            passData.src = tmpBuffer3A;
                           builder.SetRenderAttachment(previousFrameTextureA, 0, AccessFlags.Write);
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 2", out var passData, m_ProfilingSampler))
                        {
                            builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                           builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                            passData.src = tmpBuffer3A;
                           builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }
                    }



                    //v0.1
                    //cmd.Blit(tmpBuffer2, source);

                    //RenderTexture.ReleaseTemporary(tmpBuffer1); RenderTexture.ReleaseTemporary(tmpBuffer2);
                    //END RENDER FINAL EFFECT

                    ////RELEASE TEMPORARY TEXTURES AND COMMAND BUFFER
                    //cmd.ReleaseTemporaryRT(lrDepthBuffer.id);
                    //cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
                    //context.ExecuteCommandBuffer(cmd);
                    //CommandBufferPool.Release(cmd); //DO NOT release fog here because sun shafts may be active v1.9.9.2

                    //v0.6
                    // RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4);
                    lastFrameViewProjectionMatrix = viewProjectionMatrix;
                    lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
                    //}
                    /////// END MORE 1 

                    //RecordRenderGraphBLIT1(renderGraph, frameData, desc, cameraData, renderingData, resourceData, _material);

                    ///////////////////////////////////////////////////////////////// END RENDER FOG


                    // Now we will add another pass to “resolve” the modified color buffer we have to the pipelinebuffer by doing the reverse blit, from destination to source. Later in this tutorial we will
                    // explore some alternatives that we can do to optimize this second blit away and avoid the round trip.

                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData, m_ProfilingSampler))
                    //{
                    //    passData.BlitMaterial = _material;
                    //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                    //    passData.src = builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    // We use the same BlitTexture API to perform the Blit operation.
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                    //}



                }
                else
                {

                    

                    //COMPOSITE
                    // cmd.Blit(tmpBuffer2a, tmpBuffer2a, _material, 6);//SSMS 6 FOG PASS !
                    //TAA
                    //Shader.SetGlobalTexture("_FogTex", tmpBuffer2a);
                    // _material.SetTexture("_BaseTex", tmpBuffer1);//SSMS
                    //cmd.Blit(tmpBuffer1, tmpBuffer2, _material, 19);//SSMS34

                    passName = "BLIT2";
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                    {
                        passData.src = resourceData.activeColorTexture;
                        desc.msaaSamples = 1; desc.depthBufferBits = 0;
                       // builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        builder.SetRenderAttachment(tmpBuffer2A, 0, AccessFlags.Write);
                        //builder.AllowPassCulling(false);
                       // builder.AllowGlobalStateModification(true);
                        passData.BlitMaterial = _material;
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                           // ExecuteBlitPass(data, context, 6, tmpBuffer1A));
                           ExecuteBlitPassNOMAINTEX(data, context, 6));
                    }


                    //Shader.SetGlobalTexture("_FogTex", tmpBuffer2A);
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("GLOBAL1", out var
                   passData))
                    {
                         builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                        passData.src = tmpBuffer2A;
                        //passData.nameID = nameID;
                        builder.AllowPassCulling(false);
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        sendGLOBAL(data, context));
                    }

                    passName = "BLIT SSMS111a";
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                    {
                        passData.src = resourceData.activeColorTexture;
                        //desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                        builder.UseTexture(tmpBuffer1A, AccessFlags.Read);
                        builder.SetRenderAttachment(tmpBuffer5A, 0, AccessFlags.Write);
                       // builder.AllowPassCulling(false);
                       // builder.AllowGlobalStateModification(true);
                        passData.BlitMaterial = _material;
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            ExecuteBlitPassTWO3(data, context, 34, tmpBuffer2A, tmpBuffer1A));
                        //ExecuteBlitPass(data, context, 35, tmpBuffer1A));
                    }

                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("GLOBAL11", out var
                 passData))
                    {
                       builder.UseTexture(tmpBuffer5A, AccessFlags.Read);
                        passData.src = tmpBuffer5A;
                       //passData.nameID = nameID;
                       builder.AllowPassCulling(false);
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        sendGLOBALNAMED(data, context, "_FogResult"));
                    }


                    //passName = "BLIT SSMS111";
                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                    //{
                    //    passData.src = resourceData.activeColorTexture;
                    //    //desc.msaaSamples = 1; desc.depthBufferBits = 0;
                    //    builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(tmpBuffer4A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    builder.AllowPassCulling(false);
                    //     builder.AllowGlobalStateModification(true);
                    //    passData.BlitMaterial = m_BlitMaterial;
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    //        ExecuteBlitPassTWO3(data, context, 35, tmpBuffer2A, tmpBuffer1A));
                    //       //ExecuteBlitPass(data, context, 35, tmpBuffer1A));
                    //}

                    //return;



                    if (enabledTemporalAA && Time.fixedTime > 0.05f)
                    {
                        var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                        _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                        viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                        _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                        _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                        _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);
                        //_material.SetTexture("_ColorBuffer", tmpBuffer2);
                        //_material.SetTexture("_PreviousColor", previousFrameTexture);
                        //_material.SetTexture("_PreviousDepth", previousDepthTexture);

                        //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                        Matrix4x4 mt = lastFrameViewProjectionMatrix * cameraData.camera.cameraToWorldMatrix;
                        _material.SetMatrix("_FrameMatrix", mt);

                        //v0.2
                        passName = "BLIT_TAA";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(tmpBuffer5A, AccessFlags.Read);
                            builder.UseTexture(previousFrameTextureA, AccessFlags.Read);
                            builder.UseTexture(previousDepthTextureA, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer3A, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            passData.BlitMaterial = _material;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                // ExecuteBlitPassTHREE(data, context, 20, tmpBuffer2A, previousFrameTextureA, previousDepthTextureA));
                                ExecuteBlitPassTEN(data, context, 20, tmpBuffer5A, previousFrameTextureA, previousDepthTextureA,
                                            "_TemporalResponse", TemporalResponse,
                                            "_TemporalGain", TemporalGain,
                                            "_InverseProjectionMatrix", projectionMatrix.inverse,
                                            "_InverseViewProjectionMatrix", viewProjectionMatrix.inverse,
                                            "_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix,
                                            "_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix,
                                            "_FrameMatrix", mt
                                            ));
                        } 
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 1", out var passData, m_ProfilingSampler))
                        {
                            builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                            passData.src = tmpBuffer3A;
                           builder.SetRenderAttachment(previousFrameTextureA, 0, AccessFlags.Write);
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("BLIT_TAA 2", out var passData, m_ProfilingSampler))
                        {
                            builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            builder.UseTexture(tmpBuffer3A, AccessFlags.Read);
                            passData.src = tmpBuffer3A;
                           builder.SetRenderAttachment(tmpBuffer5A, 0, AccessFlags.Write);
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }

                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve1", out var passData, m_ProfilingSampler))
                        //{
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = m_BlitMaterial;
                        //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                        //    passData.src = builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    // We use the same BlitTexture API to perform the Blit operation.
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        //}

                    }
                    //v0.6



                    // RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4);
                    lastFrameViewProjectionMatrix = viewProjectionMatrix;
                    lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;




                    //COMPOSITE
                    //Shader.SetGlobalTexture("_FogTex", tmpBuffer2a);
                    // _material.SetTexture("_BaseTex", tmpBuffer1);//SSMS
                    // cmd.Blit(tmpBuffer1, tmpBuffer2, _material, 19);//SSMS34
                    //tmpBuffer2A = tmpBuffer2a
                    //tmpBuffer4A = tmpBuffer2
                    //ExecuteBlitPassTWO3

                    //passName = "BLIT SSMS111a";
                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                    //{
                    //    passData.src = resourceData.activeColorTexture;
                    //    //desc.msaaSamples = 1; desc.depthBufferBits = 0;
                    //    builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(tmpBuffer4A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    builder.AllowPassCulling(false);
                    //    builder.AllowGlobalStateModification(true);
                    //    passData.BlitMaterial = _material;
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    //        ExecuteBlitPassTWO3(data, context, 35, tmpBuffer2A, tmpBuffer1A));
                    //    //ExecuteBlitPass(data, context, 35, tmpBuffer1A));
                    //}


                    //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve12", out var passData, m_ProfilingSampler))
                    //{
                    //    builder.AllowPassCulling(false);
                    //    builder.AllowGlobalStateModification(true);
                    //    passData.BlitMaterial = _material;
                    //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                    //    passData.src = builder.UseTexture(tmpBuffer5A, IBaseRenderGraphBuilder.AccessFlags.Read);
                    //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    //    // We use the same BlitTexture API to perform the Blit operation.
                    //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                    //}


                    /////////////////// SSMS
                    if (!enableWetnessHaze)
                    {
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve13", out var passData, m_ProfilingSampler))
                        //{
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = _material;
                        //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                        //    passData.src = builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    // We use the same BlitTexture API to perform the Blit operation.
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        //}
                    }
                    else
                    {
                        //            RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                        //            RenderTexture.active = tmpBuffer1;
                        //            GL.ClearWithSkybox(false, camera);
                        //v0.1
                        //            cmd.Blit(source, tmpBuffer1); //v0.1
                        ////TEST 1

                        //Material _material = m_BlitMaterial;
                        //return;
                       

                        var useRGBM = Application.isMobilePlatform;
                        // source texture size
                        var tw = rtW;
                        var th = rtH;
                        // halve the texture size for the low quality mode
                        if (!_highQuality)
                        {
                            tw /= 2;
                            th /= 2;
                        }
                        // blur buffer format
                        var rtFormat = useRGBM ? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;

                        // determine the iteration count
                        var logh = Mathf.Log(th, 2) + _radius - 8;
                        var logh_i = (int)logh;
                        var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

                        // update the shader properties
                        var lthresh = thresholdLinear;
                        _material.SetFloat("_Threshold", lthresh);

                        var knee = lthresh * _softKnee + 1e-5f;
                        var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
                        _material.SetVector("_Curve", curve);

                        var pfo = !_highQuality && _antiFlicker;
                        _material.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

                        _material.SetFloat("_SampleScale", 0.5f + logh - logh_i);
                        _material.SetFloat("_Intensity", _intensity);

                        _material.SetTexture("_FadeTex", _fadeRamp);
                        _material.SetFloat("_BlurWeight", _blurWeight);
                        _material.SetFloat("_Radius", _radius);
                        _material.SetColor("_BlurTint", _blurTint);

                        // prefilter pass
                        //var prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
                        int base_width = desc.width;
                        int base_height = desc.height;
                        desc.width = tw;
                        desc.height = th;
                        //tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true); //prefiltered

                        TextureDesc opaqueDescA = new TextureDesc();
                        opaqueDescA.width = base_width;
                        opaqueDescA.height = base_height;
                        opaqueDescA.colorFormat = desc.graphicsFormat;// GraphicsFormat.R8G8B8_UNorm;// desc.colorFormat;
                        opaqueDescA.dimension = TextureDimension.Tex2D;
                        opaqueDescA.slices = 1;
                        // tmpBuffer4A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer4A", true); //prefilteredrenderGraph.CreateTexture(opaqueDescA);//
                        //tmpBuffer5A

                        int offset = 10 + 15; //GRAPH VERSIONS
                        var pass = _antiFlicker ? 1 + offset : 0 + offset;






                        //prefiltered === tmpBuffer1A
                        //tmpBuffer1  === tmpBuffer2A
                        ////             //cmd.Blit(tmpBuffer1, prefiltered, _material, pass); //v0.5
                        //passName = "prefilter";
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        //{
                        //    passData.src = resourceData.activeColorTexture;
                        //    desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        //    builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(tmpBuffer4A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = _material;
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //        ExecuteBlitPassA1(data, context, pass, tmpBuffer2A, _fadeRamp, tmpBuffer2A));
                        //}

                        passName = "prefilter2";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(tmpBuffer2A, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer4A, 0, AccessFlags.Write);
                           // builder.AllowPassCulling(false);
                           // builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                ExecuteBlitPassA1(data, context, pass, tmpBuffer2A, _fadeRamp, tmpBuffer2A)); //v0.1 was pass= 36
                        }








                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve13", out var passData, m_ProfilingSampler))
                        //{
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = _material;
                        //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                        //    passData.src = builder.UseTexture(tmpBuffer4A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    // We use the same BlitTexture API to perform the Blit operation.
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        //}

                        //return;


                        //opaqueDescA.filterMode = desc.
                        // Debug.Log(" opaqueDescA.width =" + opaqueDescA.width + ",  opaqueDescA.height=" + opaqueDescA.height);
                        // construct a mip pyramid
                        //        var last = tmpBuffer4A;
                        passName = "tmpBuffer6A";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = tmpBuffer4A;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(passData.src, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer7A, 0, AccessFlags.Write);
                           // builder.AllowPassCulling(false);
                            //builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                ExecuteBlitPass(data, context, 24, passData.src));
                        }
                        //last == tmpBuffer3A

                   

                        //return;

                        for (var level = 0; level < iterations; level++)
                        {
                            // _blurBuffer1[level] = RenderTexture.GetTemporary(
                            //    last.width / 2, last.height / 2, 0, rtFormat
                            //);
                            desc.width = desc.width / 2;
                            desc.height = desc.height / 2;
                            opaqueDescA.width = opaqueDescA.width / 2;
                            opaqueDescA.height = opaqueDescA.height / 2;
                            //Debug.Log(" opaqueDescA.width =" + opaqueDescA.width + ",  opaqueDescA.height=" + opaqueDescA.height);
                            //Debug.Log(" level =" + level);
                            _blurBuffer1NSIZEX[level] = desc.width;
                            _blurBuffer1NSIZEY[level] = desc.height;


                            _blurBuffer1N[level] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_blurBuffer1N" + level, true);//////                                        
                                                                                                                                                //_blurBuffer1N[level] = renderGraph.CreateTexture(opaqueDescA);

                            int passC = (level == 0) ? (_antiFlicker ? 3 + offset : 2 + offset) : 4 + offset;
                            //                 Graphics.Blit(last, _blurBuffer1[level], _material, pass);

                            passName = "_blurBuffer1" + level;
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                builder.UseTexture(tmpBuffer7A, AccessFlags.Read);
                                builder.SetRenderAttachment(_blurBuffer1N[level], 0, AccessFlags.Write);
                              //  builder.AllowPassCulling(false);
                              //  builder.AllowGlobalStateModification(true);
                                passData.BlitMaterial = _material;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    ExecuteBlitPassA(data, context, passC, tmpBuffer7A));
                            }

                            //               last = _blurBuffer1N[level];
                            passName = "_blurBuffer1N[level]";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = _blurBuffer1N[level];
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                builder.UseTexture(passData.src, AccessFlags.Read);
                                builder.SetRenderAttachment(tmpBuffer7A, 0, AccessFlags.Write);
                               // builder.AllowPassCulling(false);
                               // builder.AllowGlobalStateModification(true);
                                passData.BlitMaterial = _material;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    ExecuteBlitPass(data, context, 24, passData.src));
                            }
                        }

                     

                        //RESET
                        desc.width = base_width;
                        desc.height = base_height;

                        // Debug.Log("ARRAY1 SIZE=" + _blurBuffer1N.Length);

                        // upsample and combine loop
                        for (var level = iterations - 2; level >= 0; level--)
                        {
                            var basetex = _blurBuffer1N[level];
                            //           _material.SetTexture("_BaseTexA", basetex); //USE another, otherwise BUGS
                            //_blurBuffer2[level] = RenderTexture.GetTemporary(
                            //    basetex.width, basetex.height, 0, rtFormat
                            //);
                            desc.width = _blurBuffer1NSIZEX[level];
                            desc.height = _blurBuffer1NSIZEY[level];
                            _blurBuffer2N[level] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_blurBuffer2N" + level, true);
                            //          _blurBuffer2N[level] = renderGraph.CreateTexture(in opaqueDescA);

                            //tmpBuffer1A = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "tmpBuffer1A", true);
                            int passB = _highQuality ? 6 + offset : 5 + offset;
                            //                   Graphics.Blit(last, _blurBuffer2[level], _material, pass);

                            // Debug.Log("level IN=" + level);

                            passName = "_blurBuffer2" + level;
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = resourceData.activeColorTexture;
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                builder.UseTexture(tmpBuffer7A, AccessFlags.Read);
                                builder.UseTexture(basetex, AccessFlags.Read);
                                builder.SetRenderAttachment(_blurBuffer2N[level], 0, AccessFlags.Write);
                               // builder.AllowPassCulling(false);
                               // builder.AllowGlobalStateModification(true);
                                passData.BlitMaterial = _material;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    ExecuteBlitPassTWO(data, context, passB, tmpBuffer7A, basetex));
                            }

                            //last = _blurBuffer2N[level];
                            passName = "_blurBuffer2N[level]";
                            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                            {
                                passData.src = _blurBuffer2N[level];
                                desc.msaaSamples = 1; desc.depthBufferBits = 0;
                                builder.UseTexture(passData.src, AccessFlags.Read);
                                builder.SetRenderAttachment(tmpBuffer7A, 0, AccessFlags.Write);
                               // builder.AllowPassCulling(false);
                                //builder.AllowGlobalStateModification(true);
                                passData.BlitMaterial = _material;
                                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                    ExecuteBlitPass(data, context, 24, passData.src));
                            }
                        }





                        //                 using (var builder = renderGraph.AddRasterRenderPass<PassData>("GLOBAL111", out var
                        //passData))
                        //                 {
                        //                     passData.src = builder.UseTexture(tmpBuffer7A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //                     //passData.nameID = nameID;
                        //                     builder.AllowPassCulling(false);
                        //                     builder.AllowGlobalStateModification(true);
                        //                     builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //                     sendGLOBALNAMED(data, context, "_FogResult3"));
                        //                 }


                        //FINAL
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4aqa", out var passData, m_ProfilingSampler))
                        //{
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = m_BlitMaterial;
                        //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                        //    passData.src = builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(tmpBuffer1A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    // We use the same BlitTexture API to perform the Blit operation.
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                        //     ExecuteBlitPass(data, rgContext, 37, passData.src));
                        //}

                        //                  passName = "FOG PLUS BACKGROUND";
                        //                  using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        //                  {
                        //                      passData.src = builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //                      desc.msaaSamples = 1; desc.depthBufferBits = 0;
                        //                      builder.SetRenderAttachment(tmpBuffer6A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //                      builder.AllowPassCulling(false);
                        //                      builder.AllowGlobalStateModification(true);
                        //                      passData.BlitMaterial = _material;
                        //                      builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //                          ExecuteBlitPass(data, context, 37, passData.src));
                        //                  }

                        //                  using (var builder = renderGraph.AddRasterRenderPass<PassData>("GLOBAL111", out var
                        //passData))
                        //                  {
                        //                      passData.src = builder.UseTexture(tmpBuffer6A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //                      //passData.nameID = nameID;
                        //                      builder.AllowPassCulling(false);
                        //                      builder.AllowGlobalStateModification(true);
                        //                      builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        //                      sendGLOBALNAMED(data, context, "_FogResult4"));
                        //                  }



                        //return;


                        // finish process
                        //            _material.SetTexture("_BaseTexA", tmpBuffer2A);// ource);//v0.1
                        int passA = _highQuality ? 8 + offset : 7 + offset;
                        //            _material.SetTexture("_MainTexA", last);// ource);//v0.1
                        //v0.1


                        //cmd.Blit(last, source, _material, pass);
                        passName = "LAST";
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                        {
                            passData.src = resourceData.activeColorTexture;
                            desc.msaaSamples = 1; desc.depthBufferBits = 0;
                            builder.UseTexture(tmpBuffer7A, AccessFlags.Read);
                            builder.UseTexture(tmpBuffer5A, AccessFlags.Read);
                            builder.SetRenderAttachment(tmpBuffer6A, 0, AccessFlags.Write);
                            builder.AllowPassCulling(false);
                            builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                                ExecuteBlitPassTWO2(data, context, passA, tmpBuffer7A, tmpBuffer5A));
                        }

                        //FINAL
                        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4aqa", out var passData, m_ProfilingSampler))
                        //{
                        //    builder.AllowPassCulling(false);
                        //    builder.AllowGlobalStateModification(true);
                        //    passData.BlitMaterial = _material;
                        //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                        //    builder.UseTexture(tmpBuffer7A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.UseTexture(tmpBuffer1A, IBaseRenderGraphBuilder.AccessFlags.Read);
                        //    builder.SetRenderAttachment(tmpBuffer6A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                        //    // We use the same BlitTexture API to perform the Blit operation.
                        //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                        //      ExecuteBlitPassTWO22(data, rgContext, 37, tmpBuffer7A, tmpBuffer1A));
                        //}



                        //               context.ExecuteCommandBuffer(cmd);


                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("GLOBAL111", out var
                        passData))
                        {
                           builder.UseTexture(tmpBuffer6A, AccessFlags.Read);
                            passData.src = tmpBuffer6A;
                           //passData.nameID = nameID;
                           builder.AllowPassCulling(false);
                            builder.AllowGlobalStateModification(true);
                            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                            sendGLOBALNAMED(data, context, "_FogResult4"));
                        }
                       // return;


                        //FINAL
                        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4a", out var passData, m_ProfilingSampler))
                        {
                            builder.AllowPassCulling(false);
                            //builder.AllowGlobalStateModification(true);
                            passData.BlitMaterial = _material;
                            // Similar to the previous pass, however now we set destination texture as input and source as output.
                           builder.UseTexture(tmpBuffer6A, AccessFlags.Read);
                            passData.src = tmpBuffer6A;
                           builder.SetRenderAttachment(tmpBuffer1A, 0, AccessFlags.Write);
                            // We use the same BlitTexture API to perform the Blit operation.
                            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                        }
                        //tmpBuffer3A.
                        // return;//

                        // release the temporary buffers
                        for (var i = 0; i < kMaxIterations; i++)
                        {
                            //                    if (_blurBuffer1[i] != null)
                            //                       RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

                            //                    if (_blurBuffer2[i] != null)
                            //                        RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

                            // _blurBuffer1N[i] = null;
                            // _blurBuffer2N[i] = null;
                        }
                        //                RenderTexture.ReleaseTemporary(prefiltered);
                        //                RenderTexture.ReleaseTemporary(tmpBuffer1);
                    }//END SSMS WETNESS


                  

                    ////////////////// END SSMS
                }//END COMPOSITE


                //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4a", out var passData, m_ProfilingSampler))
                //{
                //    builder.AllowPassCulling(false);
                //    builder.AllowGlobalStateModification(true);
                //    passData.BlitMaterial = _material;
                //    // Similar to the previous pass, however now we set destination texture as input and source as output.
                //    passData.src = builder.UseTexture(tmpBuffer6A, IBaseRenderGraphBuilder.AccessFlags.Read);
                //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                //    // We use the same BlitTexture API to perform the Blit operation.
                //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                //}

            }//END A Connector check


            //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4a", out var passData, m_ProfilingSampler))
            //{
            //    builder.AllowPassCulling(false);
            //    builder.AllowGlobalStateModification(true);
            //    passData.BlitMaterial = m_BlitMaterial;
            //    // Similar to the previous pass, however now we set destination texture as input and source as output.
            //    passData.src = builder.UseTexture(tmpBuffer6A, IBaseRenderGraphBuilder.AccessFlags.Read);
            //    builder.SetRenderAttachment(sourceTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
            //    // We use the same BlitTexture API to perform the Blit operation.
            //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            //}

            //FINAL
            //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve4aqa", out var passData, m_ProfilingSampler))
            //{
            //    builder.AllowPassCulling(false);
            //    builder.AllowGlobalStateModification(true);
            //    passData.BlitMaterial = m_BlitMaterial;
            //    // Similar to the previous pass, however now we set destination texture as input and source as output.
            //    passData.src = builder.UseTexture(tmpBuffer2A, IBaseRenderGraphBuilder.AccessFlags.Read);
            //    builder.SetRenderAttachment(tmpBuffer1A, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
            //    // We use the same BlitTexture API to perform the Blit operation.
            //    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            //     ExecuteBlitPass(data, rgContext, 37, passData.src));
            //}

        }
        private void sendGLOBAL(PassData data, RasterGraphContext context)
        {
            context.cmd.SetGlobalTexture("_FogTex", data.src);
        }
        private void sendGLOBALNAMED(PassData data, RasterGraphContext context, string name)
        {
            context.cmd.SetGlobalTexture(name, data.src);
        }
        //temporal
        static void ExecuteBlitPassTEN(PassData data, RasterGraphContext context, int pass,
            TextureHandle tmpBuffer1, TextureHandle tmpBuffer2, TextureHandle tmpBuffer3,
            string varname1, float var1,
            string varname2, float var2,
            string varname3, Matrix4x4 var3,
            string varname4, Matrix4x4 var4,
            string varname5, Matrix4x4 var5,
            string varname6, Matrix4x4 var6,
            string varname7, Matrix4x4 var7
            )
        {
            data.BlitMaterial.SetTexture("_ColorBuffer", tmpBuffer1);
            data.BlitMaterial.SetTexture("_PreviousColor", tmpBuffer2);
            data.BlitMaterial.SetTexture("_PreviousDepth", tmpBuffer3);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
            //lastFrameViewProjectionMatrix = viewProjectionMatrix;
            //lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
        }

        static void ExecuteBlitPassTHREE(PassData data, RasterGraphContext context, int pass,
            TextureHandle tmpBuffer1, TextureHandle tmpBuffer2,  TextureHandle tmpBuffer3)
        {
            data.BlitMaterial.SetTexture("_ColorBuffer",    tmpBuffer1);
            data.BlitMaterial.SetTexture("_PreviousColor",  tmpBuffer2);
            data.BlitMaterial.SetTexture("_PreviousDepth",  tmpBuffer3);          
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassTWO2(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
        {
            data.BlitMaterial.SetTexture("_MainTexA", tmpBuffer1);
            data.BlitMaterial.SetTexture("_BaseTexA", tmpBuffer2);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassTWO22(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
        {
            data.BlitMaterial.SetTexture("_FogResult3", tmpBuffer1);
            data.BlitMaterial.SetTexture("_FogResult", tmpBuffer2);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassTWO(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
        {  
            data.BlitMaterial.SetTexture("_MainTexA", tmpBuffer1);
            data.BlitMaterial.SetTexture("_BaseTexA", tmpBuffer2);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassTWO3(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1, TextureHandle tmpBuffer2)
        {
            data.BlitMaterial.SetTexture("_FogTex", tmpBuffer1);
            data.BlitMaterial.SetTexture("_BaseTex", tmpBuffer2);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassNOMAINTEX(PassData data, RasterGraphContext context, int pass)
        {
            data.BlitMaterial.SetFloat("blackBackground", 1);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPass(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
        {  
            data.BlitMaterial.SetTexture("_MainTex", tmpBuffer1aa);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassA(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa)
        {
           // data.BlitMaterial.SetTexture("_FadeTex", _fadeRamp);
            data.BlitMaterial.SetTexture("_MainTexA", tmpBuffer1aa);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        static void ExecuteBlitPassA1(PassData data, RasterGraphContext context, int pass, TextureHandle tmpBuffer1aa, Texture2D _fadeRamp, TextureHandle fogTex)
        {
            data.BlitMaterial.SetTexture("_FogTex", fogTex);
            data.BlitMaterial.SetTexture("_FadeTex", _fadeRamp);
            data.BlitMaterial.SetTexture("_MainTexA", tmpBuffer1aa);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.BlitMaterial, pass);
        }
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, RasterGraphContext rgContext)
        {
            Blitter.BlitTexture(rgContext.cmd, data.src, new Vector4(1, 1, 0, 0),
            data.BlitMaterial, 23);
        }
        //private Material m_BlitMaterial;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("After Opaques");
        //public BlitPassVolumeFogSRP(Material invertColorMaterial, Material blitMaterial, RenderPassEvent rpEvent)
        //{
        //    //m_InvertColorMaterial = invertColorMaterial;
        //    m_BlitMaterial = blitMaterial;
        //    renderPassEvent = rpEvent;
        //}
        ////// END GRAPH
#endif

        void Dispose()
        { //RAM LEAK FIX
            //_handleA?.Release();
            _handle?.Release();
        }

        //v0.4  - Unity 2020.1
#if UNITY_2020_2_OR_NEWER
        public BlitVolumeFogSRP.BlitSettings settings;

#if UNITY_2022_1_OR_NEWER
        RTHandle _handle; //UnityEngine.Rendering.Universal.RenderTargetHandle _handle; //v0.1

        //v0.5a
#if UNITY_2022_2_OR_NEWER
                RTHandle m_CameraColorTarget;        
                float m_Intensity;
                public void SetTarget(RTHandle colorHandle, float intensity)
                {
                    m_CameraColorTarget = colorHandle;
                    m_Intensity = intensity;
                }
        RTHandle _handleA; RTHandle _handleB; RTHandle _handleC; RTHandle _handleD; RTHandle _handleE; RTHandle _handleF; RTHandle _handleG;
        //RTHandle _handleAA; 
        //RTHandle _handleBB;
#endif

#else
        UnityEngine.Rendering.Universal.RenderTargetHandle _handle; //v0.1
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {

            //RAM LEAK FIX
            if (renderingData.cameraData.camera != Camera.main && ( !isForReflections && !isForDualCameras) )
            {
                return;
            }

            //_handle.Init(settings.textureId);
            //v0.1
            //_handle.Init(settings.textureId);

            //v0.5a
#if UNITY_2022_2_OR_NEWER
            if (m_CameraColorTarget != null)
            {
                ConfigureTarget(m_CameraColorTarget);
            }
            //https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@12.0/manual/rthandle-system-using.html
            //RTHandle rtHandleUsingFunctor = RTHandles.Alloc(ComputeRTHandleSize, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureDimension.Tex2D);
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            int xres = (int)(rtW / ((float)downSample));
            int yres = (int)(rtH / ((float)downSample));
            if (_handleA == null || _handleA.rt.width != xres || _handleA.rt.height != yres)
            {
                _handleA?.Release();
                //_handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D);
                _handleA = RTHandles.Alloc(xres, yres, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2D, depthBufferBits:DepthBits.None);
                //RAM LEAK FIX
                //opaqueDesc.depthBufferBits = 0;
                //RenderingUtils.ReAllocateIfNeeded(ref _handleA, opaqueDesc, FilterMode.Bilinear,
                //                        TextureWrapMode.Clamp, name: "_handleA");

            }
            //R8G8B8A8_UNorm, dimension: TextureDimension.Tex2D);
#endif

            var renderer = renderingData.cameraData.renderer;

#if UNITY_2022_1_OR_NEWER
            // _handle = RTHandles.Alloc(settings.textureId, name: settings.textureId);
            //RAM LEAK FIX
            opaqueDesc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _handle, opaqueDesc, FilterMode.Bilinear,
                                       TextureWrapMode.Clamp, name: "_handle");

            destination = (settings.destination == BlitVolumeFogSRP.Target.Color)
                ? renderer.cameraColorTargetHandle //UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget //v0.1
                : _handle;
            
            //v0.1
            //source = renderer.cameraColorTarget;
            source = renderer.cameraColorTargetHandle;
#else
            _handle.Init(settings.textureId);
            destination = (settings.destination == BlitVolumeFogSRP.Target.Color)
               ? UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget //v0.1
               : _handle;

            //v0.1
            //source = renderer.cameraColorTarget;
            source = renderer.cameraColorTarget;
#endif

        }
#endif

        //SSMS
        #region Public Properties
        public bool Unity2022 = false;
        //v1.3.0
        public float GlobalFogPower = 1;
        public float GlobalFogNoisePower = 1;
        public float VolumeLightNoisePower = 0;
        public bool useTexture3DNoise = false;
        public Texture3D _NoiseTex1;
        public float localLightAttenuation = 1;

        public bool enableComposite = false;
        public float downSampleAA = 1;//SSMS
        public bool enableWetnessHaze = false;

        /// Prefilter threshold (gamma-encoded)
        /// Filters out pixels under this level of brightness.
        public float thresholdGamma;
        //{
        //    get { return Mathf.Max(_threshold, 0); }
        //    set { _threshold = value; }
        //}

        /// Prefilter threshold (linearly-encoded)
        /// Filters out pixels under this level of brightness.
        public float thresholdLinear;
        //{
        //    get { return GammaToLinear(thresholdGamma); }
        //    set { _threshold = LinearToGamma(value); }
        //}

        [HideInInspector]
        [SerializeField]
        [Tooltip("Filters out pixels under this level of brightness.")]
        public float _threshold = 0f;

        /// Soft-knee coefficient
        /// Makes transition between under/over-threshold gradual.
        public float softKnee;
        //{
        //    get { return _softKnee; }
        //    set { _softKnee = value; }
        //}

        [HideInInspector]
        [SerializeField, Range(0, 1)]
        [Tooltip("Makes transition between under/over-threshold gradual.")]
        public float _softKnee = 0.5f;

        /// Bloom radius
        /// Changes extent of veiling effects in a screen
        /// resolution-independent fashion.
        public float radius;
        //{
        //    get { return _radius; }
        //    set { _radius = value; }
        //}

        [Header("Scattering")]
        [SerializeField, Range(1, 7)]
        [Tooltip("Changes extent of veiling effects\n" +
                 "in a screen resolution-independent fashion.")]
        public float _radius = 7f;

        /// Blur Weight
        /// Gives more strength to the blur texture during the combiner loop.
        public float blurWeight;
        //{
        //    get { return _blurWeight; }
        //    set { _blurWeight = value; }
        //}

        [SerializeField, Range(0.1f, 100)]
        [Tooltip("Higher number creates a softer look but artifacts are more pronounced.")] // TODO Better description.
        public float _blurWeight = 1f;

        /// Bloom intensity
        /// Blend factor of the result image.
        public float intensity;
        //{
        //    get { return Mathf.Max(_intensity, 0); }
        //    set { _intensity = value; }
        //}

        [SerializeField]
        [Tooltip("Blend factor of the result image.")]
        [Range(0, 1)]
        public float _intensity = 1f;

        /// High quality mode
        /// Controls filter quality and buffer resolution.
        public bool highQuality;
        //{
        //    get { return _highQuality; }
        //    set { _highQuality = value; }
        //}

        [SerializeField]
        [Tooltip("Controls filter quality and buffer resolution.")]
        public bool _highQuality = true;

        /// Anti-flicker filter
        /// Reduces flashing noise with an additional filter.
        [SerializeField]
        [Tooltip("Reduces flashing noise with an additional filter.")]
        public bool _antiFlicker = true;

        public bool antiFlicker;
        //{
        //    get { return _antiFlicker; }
        //    set { _antiFlicker = value; }
        //}

        /// Distribution texture
        [SerializeField]
        [Tooltip("1D gradient. Determines how the effect fades across distance.")]
        public Texture2D _fadeRamp;

        public Texture2D fadeRamp;
        //{
        //    get { return _fadeRamp; }
        //    set { _fadeRamp = value; }
        //}

        /// Blur tint
        [SerializeField]
        [Tooltip("Tints the resulting blur. ")]
        public Color _blurTint = Color.white;

        public Color blurTint;
        //{
        //    get { return _blurTint; }
        //    set { _blurTint = value; }
        //}
        #endregion

        #region Private Members

        //[SerializeField, HideInInspector]
        //Shader _shader;
        //Material _material;

        const int kMaxIterations = 16;
        RenderTexture[] _blurBuffer1 = new RenderTexture[kMaxIterations];
        RenderTexture[] _blurBuffer2 = new RenderTexture[kMaxIterations];

#if UNITY_2023_3_OR_NEWER
        TextureHandle[] _blurBuffer1N = new TextureHandle[kMaxIterations];
        TextureHandle[] _blurBuffer2N = new TextureHandle[kMaxIterations];
        int[] _blurBuffer1NSIZEX = new int[kMaxIterations];
        int[] _blurBuffer1NSIZEY = new int[kMaxIterations];
#endif

        float LinearToGamma(float x)
        {
#if UNITY_5_3_OR_NEWER
            return Mathf.LinearToGammaSpace(x);
#else
            if (x <= 0.0031308f)
                return 12.92f * x;
            else
                return 1.055f * Mathf.Pow(x, 1 / 2.4f) - 0.055f;
#endif
        }
        float GammaToLinear(float x)
        {
#if UNITY_5_3_OR_NEWER
            return Mathf.GammaToLinearSpace(x);
#else
            if (x <= 0.04045f)
                return x / 12.92f;
            else
                return Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
#endif
        }
        #endregion

        #region MonoBehaviour Functions

        void OnEnable()
        {
            //var shader = _shader ? _shader : Shader.Find("Hidden/SSMS");
            //_material = new Material(shader);
            //_material.hideFlags = HideFlags.DontSave;
            // SMSS
            if (fadeRamp == null)
            {
                //_fadeRamp = Resources.Load("Textures/nonLinear2", typeof(Texture2D)) as Texture2D;
            };
        }
        //void OnDisable()
        //{
        //DestroyImmediate(_material);
        //}
        // [ImageEffectOpaque]
        //void renderSSMS(RenderTexture source, RenderTexture destination, Material _material)
        public void renderSSMS(ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData, CommandBuffer cmd, RenderTextureDescriptor opaqueDesc)
        {
            Material _material = blitMaterial;
            //CAMERA 
            // Calculate vectors towards frustum corners.
            Camera camera = Camera.main;
            if (isForReflections && reflectCamera != null)
            {
                // camera = reflectionc UnityEngine.Rendering.Universal.RenderingData.ca
                // ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData
                camera = reflectCamera;
            }
            if (isForReflections && isForDualCameras) //v1.9.9.7 - Ethereal v1.1.8f
            {
                //if list has members, choose 0 for 1st etc
                if (extraCameras.Count > 0 && extraCameraID >= 0 && extraCameraID < extraCameras.Count)
                {
                    camera = extraCameras[extraCameraID];
                }
            }
            //v1.7.1 - Solve editor flickering
            if (Camera.current != null)
            {
                camera = Camera.current;
            }

            //RENDER FINAL EFFECT
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            //var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
            var format = allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9 //v LWRP //v0.7
            // Debug.Log(renderingData.cameraData.camera.allowHDR);
            //RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(context.width, context.height, 0, format);
            RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
            RenderTexture.active = tmpBuffer1;
            GL.ClearWithSkybox(false, camera);
            ////context.command.BlitFullscreenTriangle(context.source, tmpBuffer1);
            //Blit(cmd, source, m_TemporaryColorTexture.Identifier()); //KEEP BACKGROUND
            
            //v0.1
            cmd.Blit(source, tmpBuffer1); //v0.1

            ////TEST 1
            //Blit(cmd, tmpBuffer1, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //return;

            var useRGBM = Application.isMobilePlatform;

            // source texture size
            var tw = rtW;// source.width;
            var th = rtH;// source.height;

            // halve the texture size for the low quality mode
            if (!_highQuality)
            {
                tw /= 2;
                th /= 2;
            }

            // blur buffer format
            var rtFormat = useRGBM ?
                RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;

            // determine the iteration count
            var logh = Mathf.Log(th, 2) + _radius - 8;
            var logh_i = (int)logh;
            var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

            // update the shader properties
            var lthresh = thresholdLinear;
            _material.SetFloat("_Threshold", lthresh);

            var knee = lthresh * _softKnee + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
            _material.SetVector("_Curve", curve);

            var pfo = !_highQuality && _antiFlicker;
            _material.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

            _material.SetFloat("_SampleScale", 0.5f + logh - logh_i);
            _material.SetFloat("_Intensity", _intensity);

            _material.SetTexture("_FadeTex", _fadeRamp);
            _material.SetFloat("_BlurWeight", _blurWeight);
            _material.SetFloat("_Radius", _radius);
            _material.SetColor("_BlurTint", _blurTint);

            // prefilter pass
            var prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
            int offset = 10;
            var pass = _antiFlicker ? 1 + offset : 0 + offset;

            ////TEST 1
            //Blit(cmd, tmpBuffer1, source, _material, radialBlurIterations);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //return;

            //_material.SetTexture("_FogTex", tmpBuffer1);// ource);//v0.1
            //Graphics.Blit(source, prefiltered, _material, pass);
            //Graphics.Blit(tmpBuffer1, prefiltered, _material, pass); //v0.1
            cmd.Blit(tmpBuffer1, prefiltered, _material, pass); //v0.5

            ////TEST 2
            //Blit(cmd, prefiltered, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //RenderTexture.ReleaseTemporary(prefiltered);
            //RenderTexture.ReleaseTemporary(tmpBuffer1);
            //return;

            //////TEST 3
            // cmd.Blit(prefiltered, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //RenderTexture.ReleaseTemporary(prefiltered);
            //RenderTexture.ReleaseTemporary(tmpBuffer1);
            // return;

            // construct a mip pyramid
            var last = prefiltered;



            for (var level = 0; level < iterations; level++)
            {
                _blurBuffer1[level] = RenderTexture.GetTemporary(
                    last.width / 2, last.height / 2, 0, rtFormat
                );

                pass = (level == 0) ? (_antiFlicker ? 3 + offset : 2 + offset) : 4 + offset;
                //Graphics.Blit(last, _blurBuffer1[level], _material, pass);
                cmd.Blit(last, _blurBuffer1[level], _material, pass);//v2.1

                last = _blurBuffer1[level];
            }

            ////TEST 3
            //Blit(cmd, last, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //RenderTexture.ReleaseTemporary(prefiltered);
            //RenderTexture.ReleaseTemporary(tmpBuffer1);
            //return;

            // upsample and combine loop
            for (var level = iterations - 2; level >= 0; level--)
            {
                var basetex = _blurBuffer1[level];
                _material.SetTexture("_BaseTexA", basetex); //USE another, otherwise BUGS

                _blurBuffer2[level] = RenderTexture.GetTemporary(
                    basetex.width, basetex.height, 0, rtFormat
                );

                pass = _highQuality ? 6 + offset : 5 + offset;
                //Graphics.Blit(last, _blurBuffer2[level], _material, pass);
                cmd.Blit(last, _blurBuffer2[level], _material, pass);//v2.1

                last = _blurBuffer2[level];
            }

            ////TEST 4
            //Blit(cmd, last, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //RenderTexture.ReleaseTemporary(prefiltered);
            //RenderTexture.ReleaseTemporary(tmpBuffer1);
            //return;

            ////TEST 1
            //Blit(cmd, tmpBuffer1, source);
            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //// release the temporary buffers
            //for (var i = 0; i < kMaxIterations; i++)
            //{
            //    if (_blurBuffer1[i] != null)
            //        RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

            //    if (_blurBuffer2[i] != null)
            //        RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

            //    _blurBuffer1[i] = null;
            //    _blurBuffer2[i] = null;
            //}
            //RenderTexture.ReleaseTemporary(prefiltered);
            //RenderTexture.ReleaseTemporary(tmpBuffer1);
            //return;

            // finish process
            _material.SetTexture("_BaseTexA", tmpBuffer1);// ource);//v0.1
            pass = _highQuality ? 8 + offset : 7 + offset;

            //v0.1
            //Graphics.Blit(last, destination, _material, pass);
            // _material.SetTexture("_FogTex", last);// ource);//v0.1
            _material.SetTexture("_MainTexA", last);// ource);//v0.1
            
            //v0.1
            cmd.Blit(last, source, _material, pass);
            
            context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);

            // release the temporary buffers
            for (var i = 0; i < kMaxIterations; i++)
            {
                if (_blurBuffer1[i] != null)
                    RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

                if (_blurBuffer2[i] != null)
                    RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

                _blurBuffer1[i] = null;
                _blurBuffer2[i] = null;
            }
            RenderTexture.ReleaseTemporary(prefiltered);
            RenderTexture.ReleaseTemporary(tmpBuffer1);
        }
        #endregion
        //END SSMS

        //v0.6
        public float downSample = 1;
        public float depthDilation = 1;
        public bool enabledTemporalAA = false;
        public float TemporalResponse = 1;
        public float TemporalGain = 1;

        //v0.6a
        public bool enableBlendMode = false;
        public float controlBackAlphaPower = 1;
        public float controlCloudAlphaPower = 0.001f;
        public Vector4 controlCloudEdgeA = new Vector4(1, 1, 1, 1);

        //v1.9.9.7 - Ethereal v1.1.8f
        public List<Camera> extraCameras = new List<Camera>();
        public int extraCameraID = 0; //assign 0 for reflection camera, 1 to N for choosing from the extra cameras list

        //v1.9.9.6 - Ethereal v1.1.8e
        public bool isForDualCameras = false;

        //v1.9.9.5 - Ethereal v1.1.8
        //Add visible lights count - renderingData.cullResults.visibleLights.Length

        ////v1.9.9.4 - Ethereal 1.1.7 - Control sampling for no noise option
        //[Tooltip("Volume sampling control, noise to no noise ratio, 0 is zero noise (x), no noise sampling step length (y) & noise sampling step lengths (z,w)")]
        public Vector4 volumeSamplingControl = new Vector4(1, 1, 1, 1);

        //v1.9.9.3
        //[Tooltip("Volume Shadow control (x-unity shadow distance, y,z-shadow atten power & offset, w-)")]
        public Vector4 shadowsControl = new Vector4(500, 1, 1, 0);

        //v1.9.9.2
        public bool enableSunShafts = false;//simple screen space sun shafts

        //v2.0
        public int maxImpostorLights = 32;

        //v1.9.9.1
        public List<Light> lightsArray = new List<Light>();

        //v1.9.9
        public Vector4 lightControlA = new Vector4(1, 1, 1, 1);
        public Vector4 lightControlB = new Vector4(1, 1, 1, 1);
        public bool controlByColor = false;
        public Light lightA;
        public Light lightB;//grab colors of the two lights to apply volume to

        //v2.0
        public bool useOnlyFog = false;
        public bool infiniteLocalLigths = false;

        //v1.7
        public int lightCount = 3;

        //v1.6
        public bool isForReflections = false;
        public Camera reflectCamera;

        public float blendVolumeLighting = 0;
        public float LightRaySamples = 8;
        public Vector4 stepsControl = new Vector4(0, 0, 1, 1);
        public Vector4 lightNoiseControl = new Vector4(0.6f, 0.75f, 1, 1);  //v1.5

        //FOG URP /////////////
        //FOG URP /////////////
        //FOG URP /////////////
        //public float blend =  0.5f;
        public Color _FogColor = Color.white / 2;
        //fog params
        public Texture2D noiseTexture;
        public float _startDistance = 30f;
        public float _fogHeight = 0.75f;
        public float _fogDensity = 1f;
        public float _cameraRoll = 0.0f;
        public Vector4 _cameraDiff = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        public float _cameraTiltSign = 1;
        public float heightDensity = 1;
        public float noiseDensity = 1;
        public float noiseScale = 1;
        public float noiseThickness = 1;
        public Vector3 noiseSpeed = new Vector4(1f, 1f, 1f);
        public float occlusionDrop = 1f;
        public float occlusionExp = 1f;
        public int noise3D = 1;
        public float startDistance = 1;
        public float luminance = 1;
        public float lumFac = 1;
        public float ScatterFac = 1;
        public float TurbFac = 1;
        public float HorizFac = 1;
        public float turbidity = 1;
        public float reileigh = 1;
        public float mieCoefficient = 1;
        public float mieDirectionalG = 1;
        public float bias = 1;
        public float contrast = 1;
        public Color TintColor = new Color(1, 1, 1, 1);
        public Vector3 TintColorK = new Vector3(0, 0, 0);
        public Vector3 TintColorL = new Vector3(0, 0, 0);
        public Vector4 Sun = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        public bool FogSky = true;
        public float ClearSkyFac = 1f;
        public Vector4 PointL = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        public Vector4 PointLParams = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

        bool _useRadialDistance = false;
        bool _fadeToSkybox = true;

        bool allowHDR = true;// false; //v0.7
        //END FOG URP //////////////////
        //END FOG URP //////////////////
        //END FOG URP //////////////////


        //SUN SHAFTS         
        public BlitVolumeFogSRP.BlitSettings.SunShaftsResolution resolution = BlitVolumeFogSRP.BlitSettings.SunShaftsResolution.Normal;
        public BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode screenBlendMode = BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen;
        public Vector3 sunTransform = new Vector3(0f, 0f, 0f); // Transform sunTransform;
        public int radialBlurIterations = 2;
        public Color sunColor = Color.white;
        public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);
        public float sunShaftBlurRadius = 2.5f;
        public float sunShaftIntensity = 1.15f;
        public float maxRadius = 0.75f;
        public bool useDepthTexture = true;
        public float blend = 0.5f;
                
        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }
        public bool inheritFromController = true;

        public bool enableFog = true;

        public Material blitMaterial = null;
        //public Material blitMaterialFOG = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }

        //private UnityEngine.Rendering.Universal.RenderTargetHandle destination { get; set; }//v0.1
#if UNITY_2022_1_OR_NEWER
        private RTHandle destination { get; set; }//v0.1
#else
        private UnityEngine.Rendering.Universal.RenderTargetHandle destination { get; set; }//v0.1
#endif

        //RTHandle m_TemporaryColorTexture ;///UnityEngine.Rendering.Universal.RenderTargetHandle m_TemporaryColorTexture; //v0.1
        string m_ProfilerTag;


        //SUN SHAFTS
        RenderTexture lrColorB;
        //RTHandle lrDepthBuffer;// UnityEngine.Rendering.Universal.RenderTargetHandle lrDepthBuffer; //v0.1

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public BlitPassVolumeFogSRP(UnityEngine.Rendering.Universal.RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag, BlitVolumeFogSRP.BlitSettings settings)
        {
#if UNITY_2023_3_OR_NEWER
            //GRAPH
            m_BlitMaterial = blitMaterial;
#endif

            this.enableFog = settings.enableFog;

            this.inheritFromController = settings.inheritFromController;
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            m_ProfilerTag = tag;
            
            //v0.1
            //m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
            //lrDepthBuffer = RTHandles.Alloc("lrDepthBuffer", name: "lrDepthBuffer");
            //m_TemporaryColorTexture.Init("_TemporaryColorTexture");

            //SUN SHAFTS
            this.resolution = settings.resolution;
            this.screenBlendMode = settings.screenBlendMode;
            this.sunTransform = settings.sunTransform;
            this.radialBlurIterations = settings.radialBlurIterations;
            this.sunColor = settings.sunColor;
            this.sunThreshold = settings.sunThreshold;
            this.sunShaftBlurRadius = settings.sunShaftBlurRadius;
            this.sunShaftIntensity = settings.sunShaftIntensity;
            this.maxRadius = settings.maxRadius;
            this.useDepthTexture = settings.useDepthTexture;
            this.blend = settings.blend;

            ////// VOLUME FOG URP /////////////////
            //FOG URP /////////////
            //FOG URP /////////////
            //FOG URP /////////////
            //this.blend =  0.5f;
            this._FogColor = settings._FogColor;
            //fog params
            this.noiseTexture = settings.noiseTexture;
            this._startDistance = settings._startDistance;
            this._fogHeight = settings._fogHeight;
            this._fogDensity = settings._fogDensity;
            this._cameraRoll = settings._cameraRoll;
            this._cameraDiff = settings._cameraDiff;
            this._cameraTiltSign = settings._cameraTiltSign;
            this.heightDensity = settings.heightDensity;
            this.noiseDensity = settings.noiseDensity;
            this.noiseScale = settings.noiseScale;
            this.noiseThickness = settings.noiseThickness;
            this.noiseSpeed = settings.noiseSpeed;
            this.occlusionDrop = settings.occlusionDrop;
            this.occlusionExp = settings.occlusionExp;
            this.noise3D = settings.noise3D;
            this.startDistance = settings.startDistance;
            this.luminance = settings.luminance;
            this.lumFac = settings.lumFac;
            this.ScatterFac = settings.ScatterFac;
            this.TurbFac = settings.TurbFac;
            this.HorizFac = settings.HorizFac;
            this.turbidity = settings.turbidity;
            this.reileigh = settings.reileigh;
            this.mieCoefficient = settings.mieCoefficient;
            this.mieDirectionalG = settings.mieDirectionalG;
            this.bias = settings.bias;
            this.contrast = settings.contrast;
            this.TintColor = settings.TintColor;
            this.TintColorK = settings.TintColorK;
            this.TintColorL = settings.TintColorL;
            this.Sun = settings.Sun;
            this.FogSky = settings.FogSky;
            this.ClearSkyFac = settings.ClearSkyFac;
            this.PointL = settings.PointL;
            this.PointLParams = settings.PointLParams;
            this._useRadialDistance = settings._useRadialDistance;
            this._fadeToSkybox = settings._fadeToSkybox;
            //END FOG URP //////////////////
            //END FOG URP //////////////////
            //END FOG URP //////////////////
            ////// END VOLUME FOG URP /////////////////
            this.blendVolumeLighting = settings.blendVolumeLighting;
            //this.LightRaySamples = settings.LightRaySamples;
            this.isForReflections = settings.isForReflections;

            //v1.9.9.6 - Ethereal v1.1.8e
            this.isForDualCameras = settings.isForDualCameras;

            //v1.9.9.7 - Ethereal v1.1.8f
            this.extraCameraID = settings.extraCameraID;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
#if UNITY_2022_1_OR_NEWER
        public void Setup(RenderTargetIdentifier source, RTHandle destination) //UnityEngine.Rendering.Universal.RenderTargetHandle destination) //v0.1
        {
            this.source = source;
            this.destination = destination;
        }
#else
        public void Setup(RenderTargetIdentifier source, UnityEngine.Rendering.Universal.RenderTargetHandle destination) //UnityEngine.Rendering.Universal.RenderTargetHandle destination) //v0.1
        {
            this.source = source;
            this.destination = destination;
        }
#endif

        connectSuntoVolumeFogURP connector;




        ////GRAPH
        ///// <inheritdoc/>
        //static void ExecuteA1(RasterGraphContext context, ref UniversalRenderingData renderingData, ref UniversalCameraData cameraData, PassData data) //GRAPH
        //{
        //    //grab settings if script on scene camera
        //    if (data.connector == null)
        //    {
        //        if (data.connector == null && Camera.main != null)
        //        {
        //            data.connector = Camera.main.GetComponent<connectSuntoVolumeFogURP>();
        //        }
        //    }

        //    //if still null, disable effect
        //    bool connectorFound = true;
        //    if (data.connector == null)
        //    {
        //        connectorFound = false;
        //    }

        //    if (data.connector.enableFog && connectorFound && (cameraData.camera == Camera.main || data.isForReflections || data.isForDualCameras))
        //    {
        //        CommandBuffer cmd = CommandBufferPool.Get(data.m_ProfilerTag);

        //        RenderTextureDescriptor opaqueDesc = cameraData.cameraTargetDescriptor;
        //        opaqueDesc.depthBufferBits = 0;

        //        //v0.1
        //        //cmd.GetTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode);
        //        //cmd.GetTemporaryRT(m_TemporaryColorTexture.id, opaqueDesc, filterMode);
        //        //RenderShafts(context, renderingData, cmd, opaqueDesc);

        //        RenderFogA(context, ref renderingData, ref cameraData, context.cmd, opaqueDesc, data);

        //    }
        //}



        /// <summary>
        /// NON GRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>        
        public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            ExecuteA(context, ref renderingData);
        }
        public void ExecuteA(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData) //GRAPH
        {

            //grab settings if script on scene camera
            if (connector == null)
            {
                connector = renderingData.cameraData.camera.GetComponent<connectSuntoVolumeFogURP>();
                if (connector == null && Camera.main != null)
                {
                    connector = Camera.main.GetComponent<connectSuntoVolumeFogURP>();

                    //v0.2
                    if (connector == null)
                    {
                        try
                        {
                            GameObject effects = GameObject.FindWithTag("SkyMasterEffects");
                            if (effects != null)
                            {
                                connector = effects.GetComponent<connectSuntoVolumeFogURP>();
                            }
                        }
                        catch
                        { }
                    }
                }
            }
            //Debug.Log(Camera.main.GetComponent<connectSuntoVolumeFogURP>().sun.transform.position);
            if (inheritFromController && connector != null)
            {
                this.enableFog = connector.enableFog;
                if (!connector.enabled)
                {
                    this.enableFog = false;
                }

                this.sunTransform = new Vector3(connector.Sun.x, connector.Sun.y, connector.Sun.z);// connector.sun.transform.position;
                this.screenBlendMode = connector.screenBlendMode;
                //public Vector3 sunTransform = new Vector3(0f, 0f, 0f); 
                this.radialBlurIterations = connector.radialBlurIterations;
                this.sunColor = connector.sunColor;
                this.sunThreshold = connector.sunThreshold;
                this.sunShaftBlurRadius = connector.sunShaftBlurRadius;
                this.sunShaftIntensity = connector.sunShaftIntensity;
                this.maxRadius = connector.maxRadius;
                this.useDepthTexture = connector.useDepthTexture;

                ////// VOLUME FOG URP /////////////////
                //FOG URP /////////////
                //FOG URP /////////////
                //FOG URP /////////////
                //this.blend =  0.5f;
                this._FogColor = connector._FogColor;
                //fog params
                this.noiseTexture = connector.noiseTexture;
                this._startDistance = connector._startDistance;
                this._fogHeight = connector._fogHeight;
                this._fogDensity = connector._fogDensity;
                this._cameraRoll = connector._cameraRoll;
                this._cameraDiff = connector._cameraDiff;
                this._cameraTiltSign = connector._cameraTiltSign;
                this.heightDensity = connector.heightDensity;
                this.noiseDensity = connector.noiseDensity;
                this.noiseScale = connector.noiseScale;
                this.noiseThickness = connector.noiseThickness;
                this.noiseSpeed = connector.noiseSpeed;
                this.occlusionDrop = connector.occlusionDrop;
                this.occlusionExp = connector.occlusionExp;
                this.noise3D = connector.noise3D;
                this.startDistance = connector.startDistance;
                this.luminance = connector.luminance;
                this.lumFac = connector.lumFac;
                this.ScatterFac = connector.ScatterFac;
                this.TurbFac = connector.TurbFac;
                this.HorizFac = connector.HorizFac;
                this.turbidity = connector.turbidity;
                this.reileigh = connector.reileigh;
                this.mieCoefficient = connector.mieCoefficient;
                this.mieDirectionalG = connector.mieDirectionalG;
                this.bias = connector.bias;
                this.contrast = connector.contrast;
                this.TintColor = connector.TintColor;
                this.TintColorK = connector.TintColorK;
                this.TintColorL = connector.TintColorL;
                this.Sun = connector.Sun;
                this.FogSky = connector.FogSky;
                this.ClearSkyFac = connector.ClearSkyFac;
                this.PointL = connector.PointL;
                this.PointLParams = connector.PointLParams;
                this._useRadialDistance = connector._useRadialDistance;
                this._fadeToSkybox = connector._fadeToSkybox;

                this.allowHDR = connector.allowHDR;
                //END FOG URP //////////////////
                //END FOG URP //////////////////
                //END FOG URP //////////////////
                ////// END VOLUME FOG URP /////////////////

                Unity2022 = connector.Unity2022;

                //v1.3.0
                GlobalFogPower = connector.GlobalFogPower;
                GlobalFogNoisePower = connector.GlobalFogNoisePower;
                VolumeLightNoisePower = connector.VolumeLightNoisePower;
                useTexture3DNoise = connector.useTexture3DNoise;
                _NoiseTex1 = connector._NoiseTex1;
                localLightAttenuation = connector.localLightAttenuation;//v1.4.3

                this.blendVolumeLighting = connector.blendVolumeLighting;
                this.LightRaySamples = connector.LightRaySamples;
                this.stepsControl = connector.stepsControl;
                this.lightNoiseControl = connector.lightNoiseControl;

                //v1.6
                this.reflectCamera = connector.reflectCamera;

                //v2.0
                this.useOnlyFog = connector.useOnlyFog;
                this.infiniteLocalLigths = connector.infiniteLocalLigths;

                //v1.7
                this.lightCount = connector.lightCount;

                //v1.9.9
                this.lightControlA = connector.lightControlA;
                this.lightControlB = connector.lightControlB;
                this.controlByColor = connector.controlByColor;
                this.lightA = connector.lightA;
                this.lightB = connector.lightB;

                //v2.0
                this.maxImpostorLights = connector.maxImpostorLights;

                //v1.9.9
                this.lightsArray = connector.lightsArray;

                //v1.9.9.2
                this.enableSunShafts = connector.enableSunShafts;

                //v1.9.9.3
                this.shadowsControl = connector.shadowsControl;

                //v1.9.9.4
                this.volumeSamplingControl = connector.volumeSamplingControl;

                //v1.9.9.7 - Ethereal v1.1.8f
                this.extraCameras = connector.extraCameras;

                //v0.6
                this.downSample = connector.downSample;
                this.depthDilation = connector.depthDilation;
                this.enabledTemporalAA = connector.enabledTemporalAA;
                this.TemporalResponse = connector.TemporalResponse;
                this.TemporalGain = connector.TemporalGain;

                //v0.6a
                this.enableBlendMode = connector.enableBlendMode;
                this.controlBackAlphaPower = connector.controlBackAlphaPower;
                this.controlCloudAlphaPower = connector.controlCloudAlphaPower;
                this.controlCloudEdgeA = connector.controlCloudEdgeA;

                //SSMS
                enableComposite = connector.enableComposite;
                downSampleAA = connector.downSampleAA;
                enableWetnessHaze = connector.enableWetnessHaze;
                //SSMS
                thresholdGamma = connector._threshold;
                thresholdLinear = connector._threshold;
                _threshold = connector._threshold;
                softKnee = connector._softKnee;
                _softKnee = connector._softKnee;
                radius = connector._radius;
                _radius = connector._radius;
                blurWeight = connector._blurWeight;
                _blurWeight = connector._blurWeight;
                intensity = connector.intensity;
                _intensity = connector.intensity;
                highQuality = connector._highQuality;
                _highQuality = connector._highQuality;
                _antiFlicker = connector._antiFlicker;
                antiFlicker = connector._antiFlicker;
                _fadeRamp = connector._fadeRamp;
                fadeRamp = connector._fadeRamp;
                _blurTint = connector._blurTint;
                blurTint = connector._blurTint;
            }

            //if still null, disable effect
            bool connectorFound = true;
            if (connector == null)
            {
                connectorFound = false;
            }

            if (enableFog && connectorFound && (renderingData.cameraData.camera == Camera.main || isForReflections || isForDualCameras))
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                // Can't read and write to same color target, create a temp render target to blit. 
#if UNITY_2022_1_OR_NEWER
                if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//  UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                {
#else
                if (destination == UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                {
#endif
                    //v0.1
                    //cmd.GetTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode);
                    //cmd.GetTemporaryRT(m_TemporaryColorTexture.id, opaqueDesc, filterMode);
                    //RenderShafts(context, renderingData, cmd, opaqueDesc);

                    RenderFog(context, renderingData, cmd, opaqueDesc);

                    //v1.9.9.2
                    //if (enableSunShafts)
                    //{
                    //    if (connector.sun != null)
                    //    {
                    //        this.sunTransform = new Vector3(connector.sun.position.x, connector.sun.position.y, connector.sun.position.z);
                    //        RenderShafts(context, renderingData, cmd, opaqueDesc);
                    //    }
                    //    else
                    //    {
                    //        CommandBufferPool.Release(cmd);//release fog here v1.9.9.2
                    //    }
                    //}
                    //else
                    //{
                    //    CommandBufferPool.Release(cmd);//release fog here v1.9.9.2
                    //}

                    //v1.9.9.2
                    if (enableSunShafts && connector.sun != null)
                    {
                        this.sunTransform = new Vector3(connector.sun.position.x, connector.sun.position.y, connector.sun.position.z);
                        RenderShafts(context, renderingData, cmd, opaqueDesc);
                    }
                    if (enableWetnessHaze)
                    {
                        renderSSMS(context, renderingData, cmd, opaqueDesc);
                    }
                    //if ((enableSunShafts && connector.sun != null) || enableWetnessHaze)//else
                    {
                        CommandBufferPool.Release(cmd);//release fog here v1.9.9.2
                    }

                }
            }
            else
            {
                //v1.9.9.2 - if no fog
                if (enableSunShafts && connectorFound && connector.sun != null)
                {
                    CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

                    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                    opaqueDesc.depthBufferBits = 0;

                    // Can't read and write to same color target, create a temp render target to blit. 
#if UNITY_2022_1_OR_NEWER
                    if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)// UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                    {
#else
                    if (destination == UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
                    {
#endif
                        //cmd.GetTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode);    //          m_TemporaryColorTexture.id, opaqueDesc, filterMode);  //v0.1                          
                        this.sunTransform = new Vector3(connector.sun.position.x, connector.sun.position.y, connector.sun.position.z);
                        RenderShafts(context, renderingData, cmd, opaqueDesc);                         
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            //if (destination == renderingData.cameraData.renderer.cameraColorTargetHandle)//UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget) //v0.1
            //{
             //   cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);            
             //   cmd.ReleaseTemporaryRT(lrDepthBuffer.id);               
            //}
        }    

        //SUN SHAFTS
        public void RenderShafts(ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData, CommandBuffer cmd, RenderTextureDescriptor opaqueDesc)
        {           
            opaqueDesc.depthBufferBits = 0;          
            Material sheetSHAFTS = blitMaterial;
           
            sheetSHAFTS.SetFloat("_Blend", blend);
            
            Camera camera = Camera.main;

            //v1.7.1 - Solve editor flickering
            if (Camera.current != null)
            {
                camera = Camera.current;
            }

            // we actually need to check this every frame
            if (useDepthTexture)
            {               
                camera.depthTextureMode |= DepthTextureMode.Depth;
            }           

            Vector3 v = Vector3.one * 0.5f;
            if (sunTransform != Vector3.zero) 
            {
                v = camera.WorldToViewportPoint(sunTransform);
            }
            else 
            {
                v = new Vector3(0.5f, 0.5f, 0.0f);
            }
            
            //v0.1
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;

            // cmd.GetTemporaryRT(Shader.PropertyToID(lrDepthBuffer.name) , opaqueDesc, filterMode);//   lrDepthBuffer.id, opaqueDesc, filterMode); //v0.1
            var formatA = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
            RenderTexture lrDepthBuffer = RenderTexture.GetTemporary(opaqueDesc.width, opaqueDesc.height, 0, formatA);
            RenderTexture m_TemporaryColorTexture = RenderTexture.GetTemporary(opaqueDesc.width, opaqueDesc.height, 0, formatA);

            sheetSHAFTS.SetVector("_BlurRadius4", new Vector4(1.0f, 1.0f, 0.0f, 0.0f) * sunShaftBlurRadius);
            sheetSHAFTS.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, maxRadius));
            sheetSHAFTS.SetVector("_SunThreshold", sunThreshold);

            if (!useDepthTexture)
            {               
                var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
                RenderTexture tmpBuffer = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                RenderTexture.active = tmpBuffer;
                GL.ClearWithSkybox(false, camera);
             
                sheetSHAFTS.SetTexture("_Skybox", tmpBuffer);     
                          
                //v0.1
		cmd.Blit( source, lrDepthBuffer, sheetSHAFTS, 3);//            cmd.Blit( source, lrDepthBuffer.Identifier(), sheetSHAFTS, 3);

                RenderTexture.ReleaseTemporary(tmpBuffer);
            }
            else
            {               
                //v0.1
            cmd.Blit( source, lrDepthBuffer, sheetSHAFTS, 2);//v0.1
            }            

            //v0.1
            cmd.Blit( source, m_TemporaryColorTexture); //KEEP BACKGROUND //v0.1
           
            radialBlurIterations = Mathf.Clamp(radialBlurIterations, 1, 4);
            float ofs = sunShaftBlurRadius * (1.0f / 768.0f);

            sheetSHAFTS.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            sheetSHAFTS.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, maxRadius));

            for (int it2 = 0; it2 < radialBlurIterations; it2++)
            {
                // each iteration takes 2 * 6 samples
                // we update _BlurRadius each time to cheaply get a very smooth look
                lrColorB = RenderTexture.GetTemporary(rtW, rtH, 0); 
                //v0.1
            cmd.Blit( lrDepthBuffer, lrColorB, sheetSHAFTS, 1);//v0.1
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(lrDepthBuffer.name));// lrDepthBuffer.id);//v0.1
                
                ofs = sunShaftBlurRadius * (((it2 * 2.0f + 1.0f) * 6.0f)) / 768.0f;             
                sheetSHAFTS.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f)); 
                cmd.GetTemporaryRT(Shader.PropertyToID(lrDepthBuffer.name), opaqueDesc, filterMode);    //   lrDepthBuffer.id, opaqueDesc, filterMode); //v0.1               
                //v0.1
            cmd.Blit( lrColorB, lrDepthBuffer, sheetSHAFTS, 1);//v0.1
                RenderTexture.ReleaseTemporary(lrColorB);  
                ofs = sunShaftBlurRadius * (((it2 * 2.0f + 2.0f) * 6.0f)) / 768.0f;              
                sheetSHAFTS.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            }
            
            // put together:
            if (v.z >= 0.0f)
            {              
                sheetSHAFTS.SetVector("_SunColor", new Vector4(sunColor.r, sunColor.g, sunColor.b, sunColor.a) * sunShaftIntensity);
            }
            else
            {
                sheetSHAFTS.SetVector("_SunColor", Vector4.zero); // no backprojection !
            }
          
            cmd.SetGlobalTexture("_ColorBuffer", lrDepthBuffer); //v0.1         
            //v0.1
            cmd.Blit( m_TemporaryColorTexture, source, sheetSHAFTS, (screenBlendMode == BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen) ? 0 : 4);//v0.1

            //cmd.ReleaseTemporaryRT(Shader.PropertyToID(lrDepthBuffer.name));//  lrDepthBuffer.id);//v0.1    
            //cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name));//  m_TemporaryColorTexture.id);     //v0.1  
            

            context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);
            //cmd.ReleaseTemporaryRT(Shader.PropertyToID(lrDepthBuffer.name)); //v0.1
            RenderTexture.ReleaseTemporary(m_TemporaryColorTexture);
            RenderTexture.ReleaseTemporary(lrDepthBuffer);

            RenderTexture.ReleaseTemporary(lrColorB);
        }


        /////////////////////// VOLUME FOG SRP /////////////////////////////////////
        public void RenderFog(ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData, CommandBuffer cmd, RenderTextureDescriptor opaqueDesc)
        //public override void Render(PostProcessRenderContext context)
        {
            //var _material = context.propertySheets.Get(Shader.Find("Hidden/InverseProjectVFogLWRP"));
            Material _material = blitMaterial;

            //v1.9.9.5 - Ethereal v1.1.8
            //Add visible lights count - renderingData.cullResults.visibleLights.Length
            _material.SetInt("_visibleLightsCount",renderingData.cullResults.visibleLights.Length);

            _material.SetFloat("_DistanceOffset", _startDistance);
            _material.SetFloat("_Height", _fogHeight); //v0.1                                                                      
            _material.SetFloat("_cameraRoll", _cameraRoll);
            _material.SetVector("_cameraDiff", _cameraDiff);
            _material.SetFloat("_cameraTiltSign", _cameraTiltSign);

            var mode = RenderSettings.fogMode;
            if (mode == FogMode.Linear)
            {
                var start = RenderSettings.fogStartDistance;//RenderSettings.RenderfogStartDistance;
                var end = RenderSettings.fogEndDistance;
                var invDiff = 1.0f / Mathf.Max(end - start, 1.0e-6f);
                _material.SetFloat("_LinearGrad", -invDiff);
                _material.SetFloat("_LinearOffs", end * invDiff);
                _material.DisableKeyword("FOG_EXP");
                _material.DisableKeyword("FOG_EXP2");
            }
            else if (mode == FogMode.Exponential)
            {
                const float coeff = 1.4426950408f; // 1/ln(2)
                var density = RenderSettings.fogDensity;// RenderfogDensity;
                _material.SetFloat("_Density", coeff * density * _fogDensity);
                _material.EnableKeyword("FOG_EXP");
                _material.DisableKeyword("FOG_EXP2");
            }
            else // FogMode.ExponentialSquared
            {
                const float coeff = 1.2011224087f; // 1/sqrt(ln(2))
                var density = RenderSettings.fogDensity;//RenderfogDensity;
                _material.SetFloat("_Density", coeff * density * _fogDensity);
                _material.DisableKeyword("FOG_EXP");
                _material.EnableKeyword("FOG_EXP2");
            }
            if (_useRadialDistance)
                _material.EnableKeyword("RADIAL_DIST");
            else
                _material.DisableKeyword("RADIAL_DIST");

            if (_fadeToSkybox)
            {
                _material.DisableKeyword("USE_SKYBOX");
                _material.SetColor("_FogColor", _FogColor);// RenderfogColor);//v0.1            
            }
            else
            {
                _material.DisableKeyword("USE_SKYBOX");
                _material.SetColor("_FogColor", _FogColor);// RenderfogColor);
            }

            //v0.1 - v1.9.9.2
            //if (noiseTexture == null)
            //{
            //    noiseTexture = new Texture2D(1280, 720);
            //}
            if (_material != null && noiseTexture != null)
            {
                //if (noiseTexture == null)
                //{
                //    noiseTexture = new Texture2D(1280, 720);
                //}
                _material.SetTexture("_NoiseTex", noiseTexture);
            }

            // Calculate vectors towards frustum corners.
            Camera camera = Camera.main;

            if (isForReflections && reflectCamera != null)
            {
                // camera = reflectionc UnityEngine.Rendering.Universal.RenderingData.ca
                // ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData
                camera = reflectCamera;
            }

            if (isForReflections && isForDualCameras) //v1.9.9.7 - Ethereal v1.1.8f
            {
                //if list has members, choose 0 for 1st etc
                if (extraCameras.Count > 0 && extraCameraID >= 0 && extraCameraID < extraCameras.Count)
                {
                    camera = extraCameras[extraCameraID];
                }
            }

            //v1.7.1 - Solve editor flickering
            if (Camera.current != null)
            {
                camera = Camera.current;
            }

            var cam = camera;// GetComponent<Camera>();
            var camtr = cam.transform;


            ////////// SCATTER
            var camPos = camtr.position;
            float FdotC = camPos.y - _fogHeight;
            float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);


            ///// ffrustrum
            //float camNear = cam.nearClipPlane;
            //float camFar = cam.farClipPlane;
            //float camFov = cam.fieldOfView;
            //float camAspect = cam.aspect;

            //Matrix4x4 frustumCorners = Matrix4x4.identity;

            //float fovWHalf = camFov * 0.5f;

            //Vector3 toRight = camtr.right * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
            //Vector3 toTop = camtr.up * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            //Vector3 topLeft = (camtr.forward * camNear - toRight + toTop);
            //float camScale = topLeft.magnitude * camFar / camNear;

            //topLeft.Normalize();
            //topLeft *= camScale;

            //Vector3 topRight = (camtr.forward * camNear + toRight + toTop);
            //topRight.Normalize();
            //topRight *= camScale;

            //Vector3 bottomRight = (camtr.forward * camNear + toRight - toTop);
            //bottomRight.Normalize();
            //bottomRight *= camScale;

            //Vector3 bottomLeft = (camtr.forward * camNear - toRight - toTop);
            //bottomLeft.Normalize();
            //bottomLeft *= camScale;

            //frustumCorners.SetRow(0, topLeft);
            //frustumCorners.SetRow(1, topRight);
            //frustumCorners.SetRow(2, bottomRight);
            //frustumCorners.SetRow(3, bottomLeft);

            //_material.SetMatrix("_FrustumCornersWS", frustumCorners);

            _material.SetVector("_CameraWS", camPos);
            _material.SetFloat("blendVolumeLighting", blendVolumeLighting);//v0.2 - SHADOWS
            _material.SetFloat("_RaySamples", LightRaySamples);
            _material.SetVector("_stepsControl", stepsControl);
            _material.SetVector("lightNoiseControl", lightNoiseControl);

            //Debug.Log("_HeightParams="+ new Vector4(_fogHeight, FdotC, paramK, heightDensity * 0.5f));

            _material.SetVector("_HeightParams", new Vector4(_fogHeight, FdotC, paramK, heightDensity * 0.5f));
            _material.SetVector("_DistanceParams", new Vector4(-Mathf.Max(startDistance, 0.0f), 0, 0, 0));
            _material.SetFloat("_NoiseDensity", noiseDensity);
            _material.SetFloat("_NoiseScale", noiseScale);
            _material.SetFloat("_NoiseThickness", noiseThickness);
            _material.SetVector("_NoiseSpeed", noiseSpeed);
            _material.SetFloat("_OcclusionDrop", occlusionDrop);
            _material.SetFloat("_OcclusionExp", occlusionExp);
            _material.SetInt("noise3D", noise3D);
            //SM v1.7
            _material.SetFloat("luminance", luminance);
            _material.SetFloat("lumFac", lumFac);
            _material.SetFloat("Multiplier1", ScatterFac);
            _material.SetFloat("Multiplier2", TurbFac);
            _material.SetFloat("Multiplier3", HorizFac);
            _material.SetFloat("turbidity", turbidity);
            _material.SetFloat("reileigh", reileigh);
            _material.SetFloat("mieCoefficient", mieCoefficient);
            _material.SetFloat("mieDirectionalG", mieDirectionalG);
            _material.SetFloat("bias", bias);
            _material.SetFloat("contrast", contrast);

            //v1.7.1 - Solve editor flickering
            Vector3 sunDir = Sun;// connector.sun.transform.forward;
            if ((Camera.current != null || isForDualCameras) && connector.sun != null) //v1.9.9.2  //v1.9.9.6 - Ethereal v1.1.8e
            {
                sunDir = connector.sun.transform.forward;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
            }

            _material.SetVector("v3LightDir", sunDir);// Sun);//.forward); //v1.7.1
            _material.SetVector("_TintColor", new Vector4(TintColor.r, TintColor.g, TintColor.b, 1));//68, 155, 345
            _material.SetVector("_TintColorK", new Vector4(TintColorK.x, TintColorK.y, TintColorK.z, 1));
            _material.SetVector("_TintColorL", new Vector4(TintColorL.x, TintColorL.y, TintColorL.z, 1));

            //v1.6 - reflections
            if (isForReflections && !isForDualCameras) //v1.9.9.6 - Ethereal v1.1.8e
            {
                _material.SetFloat("_invertX", 1);
            }
            else
            {
                _material.SetFloat("_invertX", 0);
            }

            //v2.0
            //#pragma multi_compile __ ONLY_FOG
            //#pragma multi_compile __ INFINITE
            //#if defined(ONLY_FOG)
            //#if defined(INFINITE)
            updateMaterialKeyword(useOnlyFog, "ONLY_FOG", _material);
            updateMaterialKeyword(infiniteLocalLigths, "INFINITE", _material);

            //v1.7
            _material.SetInt("lightCount", lightCount);

            //v1.3.0
            _material.SetFloat("GlobalFogPower", GlobalFogPower);
            _material.SetFloat("GlobalFogNoisePower", GlobalFogNoisePower);
            _material.SetFloat("VolumeLightNoisePower", VolumeLightNoisePower);
            _material.SetFloat("useTexture3DNoise", useTexture3DNoise ? 1 : 0);
            _material.SetTexture("_NoiseTex1", _NoiseTex1);
            _material.SetFloat("attenExponent", localLightAttenuation);//v1.4.3

            //v1.9.9
            _material.SetVector("lightControlA", lightControlA);
            _material.SetVector("lightControlB", lightControlB);
            if (lightA)
            {
                _material.SetVector("lightAcolor", new Vector3(lightA.color.r, lightA.color.g, lightA.color.b));
                _material.SetFloat("lightAIntensity", lightA.intensity); 
            }
            if (lightB)
            {
                _material.SetVector("lightBcolor", new Vector3(lightB.color.r, lightB.color.g, lightB.color.b));
                _material.SetFloat("lightBIntensity", lightA.intensity);
            }
            if (controlByColor)
            {
                _material.SetInt("controlByColor", 1);
            }
            else
            {
                _material.SetInt("controlByColor", 0);
            }

            //v1.9.9.3
            _material.SetVector("shadowsControl", shadowsControl);

            //v1.9.9.4
            _material.SetVector("volumeSamplingControl", volumeSamplingControl);

            //v1.9.9.1
            // Debug.Log(_material.HasProperty("lightsArrayLength"));
            //Debug.Log(_material.HasProperty("controlByColor"));
            if (_material.HasProperty("lightsArrayLength") && lightsArray.Count > 0) //check for other shader versions
            {
                //pass array
                _material.SetVectorArray("_LightsArrayPos", new Vector4[maxImpostorLights]);//32
                _material.SetVectorArray("_LightsArrayDir", new Vector4[maxImpostorLights]);//32
                int countLights = lightsArray.Count;
                if(countLights > maxImpostorLights)//32
                {
                    countLights = maxImpostorLights;//32
                }
                _material.SetInt("lightsArrayLength", countLights);
                //Debug.Log(countLights);
                // material.SetFloatArray("_Points", new float[10]);
                //float[] array = new float[] { 1, 2, 3, 4 };
                Vector4[] posArray = new Vector4[countLights];
                Vector4[] dirArray = new Vector4[countLights];
                Vector4[] colArray = new Vector4[countLights];
                for (int i=0;i< countLights; i++)
                {
                    //posArray[i].x = lightsArray(0).
                    posArray[i].x = lightsArray[i].transform.position.x;
                    posArray[i].y = lightsArray[i].transform.position.y;
                    posArray[i].z = lightsArray[i].transform.position.z;
                    posArray[i].w = lightsArray[i].intensity;
                    //Debug.Log(posArray[i].w);
                    colArray[i].x = lightsArray[i].color.r;
                    colArray[i].y = lightsArray[i].color.g;
                    colArray[i].z = lightsArray[i].color.b;

                    //check if point light
                    if (lightsArray[i].type == LightType.Point)
                    {
                        dirArray[i].x = 0;
                        dirArray[i].y = 0;
                        dirArray[i].z = 0;
                    }
                    else
                    {
                        dirArray[i].x = lightsArray[i].transform.forward.x;
                        dirArray[i].y = lightsArray[i].transform.forward.y;
                        dirArray[i].z = lightsArray[i].transform.forward.z;
                    }
                    dirArray[i].w = lightsArray[i].range;
                }
                _material.SetVectorArray("_LightsArrayPos", posArray);
                _material.SetVectorArray("_LightsArrayDir", dirArray);
                _material.SetVectorArray("_LightsArrayColor", colArray);
                //material.SetFloatArray(array);
            }
            else
            {
                _material.SetInt("lightsArrayLength", 0);
            }


            float Foggy = 0;
            if (FogSky) //ClearSkyFac
            {
                Foggy = 1;
            }
            _material.SetFloat("FogSky", Foggy);
            _material.SetFloat("ClearSkyFac", ClearSkyFac);
            //////// END SCATTER

            //LOCAL LIGHT
            _material.SetVector("localLightPos", new Vector4(PointL.x, PointL.y, PointL.z, PointL.w));//68, 155, 345
            _material.SetVector("localLightColor", new Vector4(PointLParams.x, PointLParams.y, PointLParams.z, PointLParams.w));//68, 155, 345
                                                                                                                                //END LOCAL LIGHT

            //v0.6
            _material.SetFloat("depthDilation", depthDilation);
            _material.SetFloat("_TemporalResponse", TemporalResponse);
            _material.SetFloat("_TemporalGain", TemporalGain);

            //RENDER FINAL EFFECT
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            //var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
            var format = allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9 //v LWRP //v0.7

           // Debug.Log(renderingData.cameraData.camera.allowHDR);

            //RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(context.width, context.height, 0, format);
            RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
            RenderTexture.active = tmpBuffer1;

            GL.ClearWithSkybox(false, camera);
            ////context.command.BlitFullscreenTriangle(context.source, tmpBuffer1);

            //Blit(cmd, source, m_TemporaryColorTexture.Identifier()); //KEEP BACKGROUND
            //v0.1
            cmd.Blit( source, tmpBuffer1); //KEEP BACKGROUND
                                           // cmd.SetGlobalTexture("_ColorBuffer", lrDepthBuffer.Identifier());
                                           // Blit(cmd, m_TemporaryColorTexture.Identifier(), source, _material, (screenBlendMode == BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen) ? 0 : 4);

            if (!enableComposite)
            {
                _material.SetTexture("_MainTex", tmpBuffer1);

                //WORLD RECONSTRUCT        
                Matrix4x4 camToWorld = camera.cameraToWorldMatrix;// context.camera.cameraToWorldMatrix;
                                                                  //Debug.Log(camToWorld);
                _material.SetMatrix("_InverseView", camToWorld);


                //v0.6
                //int downSample = 2;
                RenderTexture tmpBuffer2 = RenderTexture.GetTemporary((int)(rtW / downSample), (int)(rtH / downSample), 0, format);

                /////context.command.BlitFullscreenTriangle(context.source, context.destination, _material, 0);
                //Blit(cmd, m_TemporaryColorTexture.Identifier(), source, _material, (screenBlendMode == BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen) ? 0 : 4);
                //Blit(cmd, tmpBuffer1, source, _material, 6);//v0.6
                //v0.1
            cmd.Blit( tmpBuffer1, tmpBuffer2, _material, 6);

                //v0.6
                // if (previousFrameTexture != null) { previousFrameTexture.Release(); }
                // if (previousDepthTexture != null) { previousDepthTexture.Release(); }
                if (previousFrameTexture == null)
                {
                    previousFrameTexture = new RenderTexture((int)(rtW / ((float)downSample)), (int)(rtH / ((float)downSample)), 0, format);// RenderTextureFormat.DefaultHDR);//v0.7
                    previousFrameTexture.filterMode = FilterMode.Point;
                    previousFrameTexture.Create();
                }
                if (previousDepthTexture == null)
                {
                    previousDepthTexture = new RenderTexture((int)(rtW / ((float)downSample)), (int)(rtH / ((float)downSample)), 0, RenderTextureFormat.RFloat);
                    previousDepthTexture.filterMode = FilterMode.Point;
                    previousDepthTexture.Create();
                }
                RenderTexture tmpBuffer3 = RenderTexture.GetTemporary((int)(rtW / ((float)downSample)), (int)(rtH / ((float)downSample)), 0, format);
                //bool temporalAntiAliasing = true;
                if (enabledTemporalAA && Time.fixedTime > 0.05f) //if (temporalAntiAliasing)
                {
                    //Debug.Log("AA Enabled");
                    var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                    var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                    _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                    viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                    _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                    _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                    _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);
                    _material.SetTexture("_ColorBuffer", tmpBuffer2);//CloudMaterial.SetTexture("_CloudTex", rtClouds);
                    _material.SetTexture("_PreviousColor", previousFrameTexture);
                    _material.SetTexture("_PreviousDepth", previousDepthTexture);

                    //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                    Matrix4x4 mt = lastFrameViewProjectionMatrix * renderingData.cameraData.camera.cameraToWorldMatrix;
                    _material.SetMatrix("_FrameMatrix", mt);

#if UNITY_2022_2_OR_NEWER //v0.5a
                    if(Unity2022){
                        if (mesh == null)
                        {
                            Awake();
                        }
                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 20);
                        //cmd.Blit(m_CameraColorTarget, tmpBuffer3);

                        cmd.Blit(_handleA, previousFrameTexture);
                        cmd.Blit(_handleA, tmpBuffer2);

                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 21);
                        cmd.Blit(_handleA, previousDepthTexture);    
                    }else{
                        cmd.SetRenderTarget(tmpBuffer3);
                        if (mesh == null)
                        {
                            Awake();
                        }
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 7);// (int)RenderPass.TemporalReproj);
                                                                                                         //cmd.blit(context.source, context.destination, _material, 0);
                        cmd.Blit(tmpBuffer3, previousFrameTexture);
                        cmd.Blit(tmpBuffer3, tmpBuffer2);

                        cmd.SetRenderTarget(previousDepthTexture);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 8);//(int)RenderPass.GetDepth);
                    }
#else
                    cmd.SetRenderTarget(tmpBuffer3);
                    if (mesh == null)
                    {
                        Awake();
                    }
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 7);// (int)RenderPass.TemporalReproj);
                                                                                                     //cmd.blit(context.source, context.destination, _material, 0);
                    cmd.Blit(tmpBuffer3, previousFrameTexture);
                    cmd.Blit(tmpBuffer3, tmpBuffer2);

                    cmd.SetRenderTarget(previousDepthTexture);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 8);//(int)RenderPass.GetDepth);
#endif
                    //DEBUG TAA
                    //cmd.Blit(previousDepthTexture, rtClouds);
                    //Blit(cmd, previousDepthTexture, source); //Blit(cmd, rtClouds, source);
                    //context.ExecuteCommandBuffer(cmd);
                    //CommandBufferPool.Release(cmd);
                    //RenderTexture.ReleaseTemporary(rtClouds);
                    //RenderTexture.ReleaseTemporary(tmpBuffer1);
                    //return;
                    //END DEBUG TAA
                }
                //Blit(cmd, tmpBuffer2, source);
                //v0.6a
                RenderTexture tmpBuffer4 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                if (enableBlendMode)
                {
                    //v0.6a
                    _material.SetFloat("controlBackAlphaPower", controlBackAlphaPower);
                    _material.SetFloat("controlCloudAlphaPower", controlCloudAlphaPower);
                    _material.SetVector("controlCloudEdgeA", controlCloudEdgeA);
                    //cmd.SetRenderTarget(source);
                    //RenderTexture.active = tmpBuffer1;

                    _material.SetTexture("_ColorBuffer", tmpBuffer2);
                    _material.SetTexture("_ManTex", tmpBuffer1);
                    //_material.SetTexture("_ColorBuffer", tmpBuffer3);
                    //Blit(cmd, tmpBuffer1, source, _material, 9);

#if UNITY_2022_2_OR_NEWER //v0.5a
                    if(Unity2022){
                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, _material, 22);
                        cmd.Blit(m_CameraColorTarget, tmpBuffer4);
                    }else{
                        cmd.SetRenderTarget(tmpBuffer4);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 9);
                    }
#else
                    cmd.SetRenderTarget(tmpBuffer4);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 9);
#endif

                    //v0.1
                    cmd.Blit( tmpBuffer4, source);
                }
                else
                {
                    //v0.1
                    cmd.Blit( tmpBuffer2, source);
                }

                RenderTexture.ReleaseTemporary(tmpBuffer1); RenderTexture.ReleaseTemporary(tmpBuffer2);
                //END RENDER FINAL EFFECT


                ////RELEASE TEMPORARY TEXTURES AND COMMAND BUFFER
                //cmd.ReleaseTemporaryRT(lrDepthBuffer.id);
                //cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
                context.ExecuteCommandBuffer(cmd);
                //CommandBufferPool.Release(cmd); //DO NOT release fog here because sun shafts may be active v1.9.9.2

                //v0.6
                RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4);
                lastFrameViewProjectionMatrix = viewProjectionMatrix;
                lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
            }
            else
            {
                //SSMS
                //_material.SetTexture("_MainTex", tmpBuffer1); //SSMS

                //WORLD RECONSTRUCT    
                Matrix4x4 camToWorld = camera.cameraToWorldMatrix;// context.camera.cameraToWorldMatrix;
                //Debug.Log(camToWorld);
                _material.SetMatrix("_InverseView", camToWorld);

                //bool enableComposite = true;
                int dimsW = rtW;
                int dimsH = rtH;
                if (enableComposite)
                {
                    //
                }
                else
                {
                    _material.SetTexture("_MainTex", tmpBuffer1);//SSMS
                    dimsW = (int)(rtW / downSample);
                    dimsH = (int)(rtH / downSample);
                }
                //v0.6
                //int downSample = 2;
                RenderTexture tmpBuffer2 = RenderTexture.GetTemporary(dimsW, dimsH, 0, format);//SSMS
                RenderTexture tmpBuffer3 = RenderTexture.GetTemporary((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, format);

                /////context.command.BlitFullscreenTriangle(context.source, context.destination, _material, 0);
                //Blit(cmd, m_TemporaryColorTexture.Identifier(), source, _material, (screenBlendMode == BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen) ? 0 : 4);
                //Blit(cmd, tmpBuffer1, source, _material, 6);//v0.6

                RenderTexture tmpBuffer2a = RenderTexture.GetTemporary((int)(rtW / downSample), (int)(rtH / downSample), 0, format);
                if (enableComposite)
                {
                    //v0.1
            cmd.Blit( tmpBuffer2a, tmpBuffer2a, _material, 6);//SSMS

                    //v0.6 - TEMPORAL
                    // if (previousFrameTexture != null) { previousFrameTexture.Release(); }
                    // if (previousDepthTexture != null) { previousDepthTexture.Release(); }
                    if (previousFrameTexture == null)
                    {
                        previousFrameTexture = new RenderTexture((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, format);// RenderTextureFormat.DefaultHDR);//v0.7
                        previousFrameTexture.filterMode = FilterMode.Point;
                        previousFrameTexture.Create();
                    }
                    if (previousDepthTexture == null)
                    {
                        previousDepthTexture = new RenderTexture((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, RenderTextureFormat.RFloat);
                        previousDepthTexture.filterMode = FilterMode.Point;
                        previousDepthTexture.Create();
                    }
                    //RenderTexture tmpBuffer3 = RenderTexture.GetTemporary((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, format);
                    //bool temporalAntiAliasing = true;
                    if (enabledTemporalAA && Time.fixedTime > 0.05f) //if (temporalAntiAliasing)
                    {
                        //Debug.Log("AA Enabled");
                        var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                        _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                        viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                        _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                        _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                        _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);
                        _material.SetTexture("_ColorBuffer", tmpBuffer2a);//CloudMaterial.SetTexture("_CloudTex", rtClouds);
                        _material.SetTexture("_PreviousColor", previousFrameTexture);
                        _material.SetTexture("_PreviousDepth", previousDepthTexture);

                        //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                        Matrix4x4 mt = lastFrameViewProjectionMatrix * renderingData.cameraData.camera.cameraToWorldMatrix;
                        _material.SetMatrix("_FrameMatrix", mt);

#if UNITY_2022_2_OR_NEWER //v0.5a
                    if(Unity2022){
                        if (mesh == null)
                        {
                            Awake();
                        }
                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 20);
                        //cmd.Blit(m_CameraColorTarget, tmpBuffer3);

                        cmd.Blit(_handleA, previousFrameTexture);
                        cmd.Blit(_handleA, tmpBuffer2a);

                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 21);
                        cmd.Blit(_handleA, previousDepthTexture); 
                    }else{
                        cmd.SetRenderTarget(tmpBuffer3);
                        if (mesh == null)
                        {
                            Awake();
                        }
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 7);// (int)RenderPass.TemporalReproj);
                                                                                                         //cmd.blit(context.source, context.destination, _material, 0);
                        cmd.Blit(tmpBuffer3, previousFrameTexture);
                        cmd.Blit(tmpBuffer3, tmpBuffer2a);

                        cmd.SetRenderTarget(previousDepthTexture);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 8);//(int)RenderPass.GetDepth);
                    }
#else
                        cmd.SetRenderTarget(tmpBuffer3);
                        if (mesh == null)
                        {
                            Awake();
                        }
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 7);// (int)RenderPass.TemporalReproj);
                                                                                                         //cmd.blit(context.source, context.destination, _material, 0);
                        cmd.Blit(tmpBuffer3, previousFrameTexture);
                        cmd.Blit(tmpBuffer3, tmpBuffer2a);

                        cmd.SetRenderTarget(previousDepthTexture);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 8);//(int)RenderPass.GetDepth);
#endif

                    }

                    //SSMS           
                    Shader.SetGlobalTexture("_FogTex", tmpBuffer2a);
                    //_material.SetTexture("_MainTex", tmpBuffer1);//SSMS
                    _material.SetTexture("_BaseTex", tmpBuffer1);//SSMS
                    //v0.1
            		cmd.Blit( tmpBuffer1, tmpBuffer2, _material, 19);//SSMS
                }
                else
                {
                    //v0.1
            		cmd.Blit( tmpBuffer1, source, _material, 6);//v0.6
                }

                //MOVED AA to VOLUME LIGHTING BUFFER ABOVE

                //Blit(cmd, tmpBuffer2, source);
                //v0.6a
                RenderTexture tmpBuffer4 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                if (enableBlendMode)
                {
                    //v0.6a
                    _material.SetFloat("controlBackAlphaPower", controlBackAlphaPower);
                    _material.SetFloat("controlCloudAlphaPower", controlCloudAlphaPower);
                    _material.SetVector("controlCloudEdgeA", controlCloudEdgeA);
                    //cmd.SetRenderTarget(source);
                    //RenderTexture.active = tmpBuffer1;
                    _material.SetTexture("_ColorBuffer", tmpBuffer2);
                    _material.SetTexture("_ManTex", tmpBuffer1);
                    //_material.SetTexture("_ColorBuffer", tmpBuffer3);
                    //Blit(cmd, tmpBuffer1, source, _material, 9);

#if UNITY_2022_2_OR_NEWER //v0.5a
                    if(Unity2022){
                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, _material, 22);
                        cmd.Blit(m_CameraColorTarget, tmpBuffer4);
                    }else{
                        cmd.SetRenderTarget(tmpBuffer4);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 9);
                    }
#else
                    cmd.SetRenderTarget(tmpBuffer4);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, 0, 9);
#endif

                    //v0.1
                    cmd.Blit( tmpBuffer4, source);
                }
                else
                {
                    //v0.1
                    cmd.Blit( tmpBuffer2, source);
                }

                RenderTexture.ReleaseTemporary(tmpBuffer1); RenderTexture.ReleaseTemporary(tmpBuffer2);
                //END RENDER FINAL EFFECT

                ////RELEASE TEMPORARY TEXTURES AND COMMAND BUFFER
                //cmd.ReleaseTemporaryRT(lrDepthBuffer.id);
                //cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
                context.ExecuteCommandBuffer(cmd);
                //CommandBufferPool.Release(cmd); //DO NOT release fog here because sun shafts may be active v1.9.9.2

                //v0.6
                RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4); RenderTexture.ReleaseTemporary(tmpBuffer2a);
                lastFrameViewProjectionMatrix = viewProjectionMatrix;
                lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
            }
        }

        //v0.6
        Matrix4x4 lastFrameViewProjectionMatrix;
        Matrix4x4 viewProjectionMatrix;
        Matrix4x4 lastFrameInverseViewProjectionMatrix;
        void OnDestroy()
        {
            if (previousFrameTexture != null)
            {
                previousFrameTexture.Release();
                previousFrameTexture = null;
            }

            if (previousDepthTexture != null)
            {
                previousDepthTexture.Release();
                previousDepthTexture = null;
            }
        }
        RenderTexture previousFrameTexture;
        RenderTexture previousDepthTexture;
        Mesh mesh;
        void Awake()
        {
            mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, -1, 1)
            };
            mesh.uv = new Vector2[]
            {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1)
            };
            mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
        }
        //v2.0
        private void updateMaterialKeyword(bool b, string keyword, Material CloudMaterial)
        {
            if (b != CloudMaterial.IsKeywordEnabled(keyword))
            {
                if (b)
                {
                    CloudMaterial.EnableKeyword(keyword);
                }
                else
                {
                    CloudMaterial.DisableKeyword(keyword);
                }
            }
        }


        /////////////////////// VOLUME FOG GRAPH /////////////////////////////////////
        private static void updateMaterialKeywordA(bool b, string keyword, Material CloudMaterial)
        {
            if (b != CloudMaterial.IsKeywordEnabled(keyword))
            {
                if (b)
                {
                    CloudMaterial.EnableKeyword(keyword);
                }
                else
                {
                    CloudMaterial.DisableKeyword(keyword);
                }
            }
        }



        /*
        // public void RenderFogA(ScriptableRenderContext context, UnityEngine.Rendering.Universal.RenderingData renderingData, CommandBuffer cmd, RenderTextureDescriptor opaqueDesc)
        void RenderFogA(RasterGraphContext context, ref UniversalRenderingData renderingData, 
            ref UniversalCameraData cameraData, RasterCommandBuffer cmd, RenderTextureDescriptor opaqueDesc, PassData data)
         {
            Material _material = data.blitMaterial;

            //v1.9.9.5 - Ethereal v1.1.8
            _material.SetInt("_visibleLightsCount", renderingData.cullResults.visibleLights.Length);

            _material.SetFloat("_DistanceOffset", data.connector._startDistance);
            _material.SetFloat("_Height", data.connector._fogHeight); //v0.1                                                                      
            _material.SetFloat("_cameraRoll", data.connector._cameraRoll);
            _material.SetVector("_cameraDiff", data.connector._cameraDiff);
            _material.SetFloat("_cameraTiltSign", data.connector._cameraTiltSign);

            var mode = RenderSettings.fogMode;
            if (mode == FogMode.Linear)
            {
                var start = RenderSettings.fogStartDistance;//RenderSettings.RenderfogStartDistance;
                var end = RenderSettings.fogEndDistance;
                var invDiff = 1.0f / Mathf.Max(end - start, 1.0e-6f);
                _material.SetFloat("_LinearGrad", -invDiff);
                _material.SetFloat("_LinearOffs", end * invDiff);
                _material.DisableKeyword("FOG_EXP");
                _material.DisableKeyword("FOG_EXP2");
            }
            else if (mode == FogMode.Exponential)
            {
                const float coeff = 1.4426950408f; // 1/ln(2)
                var density = RenderSettings.fogDensity;// RenderfogDensity;
                _material.SetFloat("_Density", coeff * density * data.connector._fogDensity);
                _material.EnableKeyword("FOG_EXP");
                _material.DisableKeyword("FOG_EXP2");
            }
            else // FogMode.ExponentialSquared
            {
                const float coeff = 1.2011224087f; // 1/sqrt(ln(2))
                var density = RenderSettings.fogDensity;//RenderfogDensity;
                _material.SetFloat("_Density", coeff * density * data.connector._fogDensity);
                _material.DisableKeyword("FOG_EXP");
                _material.EnableKeyword("FOG_EXP2");
            }
            if (data.connector._useRadialDistance)
                _material.EnableKeyword("RADIAL_DIST");
            else
                _material.DisableKeyword("RADIAL_DIST");

            if (data.connector._fadeToSkybox)
            {
                _material.DisableKeyword("USE_SKYBOX");
                _material.SetColor("_FogColor", data.connector._FogColor);// RenderfogColor);//v0.1            
            }
            else
            {
                _material.DisableKeyword("USE_SKYBOX");
                _material.SetColor("_FogColor", data.connector._FogColor);// RenderfogColor);
            }

            if (_material != null && data.connector.noiseTexture != null)
            {
                _material.SetTexture("_NoiseTex", data.connector.noiseTexture);
            }

            // Calculate vectors towards frustum corners.
            Camera camera = Camera.main;

            if (data.isForReflections && data.connector.reflectCamera != null)
            {
                camera = data.connector.reflectCamera;
            }

            if (data.isForReflections && data.isForDualCameras) //v1.9.9.7 - Ethereal v1.1.8f
            {
                if (data.connector.extraCameras.Count > 0 && data.extraCameraID >= 0 && data.extraCameraID < data.connector.extraCameras.Count)
                {
                    camera = data.connector.extraCameras[data.extraCameraID];
                }
            }

            //v1.7.1 - Solve editor flickering
            if (Camera.current != null)
            {
                camera = Camera.current;
            }

            var cam = camera;// GetComponent<Camera>();
            var camtr = cam.transform;

            ////////// SCATTER
            var camPos = camtr.position;
            float FdotC = camPos.y - data.connector._fogHeight;
            float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
           
            _material.SetVector("_CameraWS", camPos);
            _material.SetFloat("blendVolumeLighting", data.connector.blendVolumeLighting);//v0.2 - SHADOWS
            _material.SetFloat("_RaySamples", data.connector.LightRaySamples);
            _material.SetVector("_stepsControl", data.connector.stepsControl);
            _material.SetVector("lightNoiseControl", data.connector.lightNoiseControl);

            //Debug.Log("_HeightParams="+ new Vector4(_fogHeight, FdotC, paramK, heightDensity * 0.5f));

            _material.SetVector("_HeightParams", new Vector4(data.connector._fogHeight, FdotC, paramK, data.connector.heightDensity * 0.5f));
            _material.SetVector("_DistanceParams", new Vector4(-Mathf.Max(data.connector.startDistance, 0.0f), 0, 0, 0));
            _material.SetFloat("_NoiseDensity", data.connector.noiseDensity);
            _material.SetFloat("_NoiseScale", data.connector.noiseScale);
            _material.SetFloat("_NoiseThickness", data.connector.noiseThickness);
            _material.SetVector("_NoiseSpeed", data.connector.noiseSpeed);
            _material.SetFloat("_OcclusionDrop", data.connector.occlusionDrop);
            _material.SetFloat("_OcclusionExp", data.connector.occlusionExp);
            _material.SetInt("noise3D", data.connector.noise3D);
            //SM v1.7
            _material.SetFloat("luminance", data.connector.luminance);
            _material.SetFloat("lumFac", data.connector.lumFac);
            _material.SetFloat("Multiplier1", data.connector.ScatterFac);
            _material.SetFloat("Multiplier2", data.connector.TurbFac);
            _material.SetFloat("Multiplier3", data.connector.HorizFac);
            _material.SetFloat("turbidity", data.connector.turbidity);
            _material.SetFloat("reileigh", data.connector.reileigh);
            _material.SetFloat("mieCoefficient", data.connector.mieCoefficient);
            _material.SetFloat("mieDirectionalG", data.connector.mieDirectionalG);
            _material.SetFloat("bias", data.connector.bias);
            _material.SetFloat("contrast", data.connector.contrast);

            //v1.7.1 - Solve editor flickering
            Vector3 sunDir = data.connector.Sun;// connector.sun.transform.forward;
            if ((Camera.current != null || data.isForDualCameras) && data.connector.sun != null) //v1.9.9.2  //v1.9.9.6 - Ethereal v1.1.8e
            {
                sunDir = data.connector.sun.transform.forward;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
            }

            _material.SetVector("v3LightDir", sunDir);// Sun);//.forward); //v1.7.1
            _material.SetVector("_TintColor", new Vector4(data.connector.TintColor.r, data.connector.TintColor.g, data.connector.TintColor.b, 1));//68, 155, 345
            _material.SetVector("_TintColorK", new Vector4(data.connector.TintColorK.x, data.connector.TintColorK.y, data.connector.TintColorK.z, 1));
            _material.SetVector("_TintColorL", new Vector4(data.connector.TintColorL.x, data.connector.TintColorL.y, data.connector.TintColorL.z, 1));

            //v1.6 - reflections
            if (data.isForReflections && !data.isForDualCameras) //v1.9.9.6 - Ethereal v1.1.8e
            {
                _material.SetFloat("_invertX", 1);
            }
            else
            {
                _material.SetFloat("_invertX", 0);
            }
            updateMaterialKeywordA(data.connector.useOnlyFog, "ONLY_FOG", _material);
            updateMaterialKeywordA(data.connector.infiniteLocalLigths, "INFINITE", _material);

            //v1.7
            _material.SetInt("lightCount", data.connector.lightCount);

            //v1.3.0
            _material.SetFloat("GlobalFogPower", data.connector.GlobalFogPower);
            _material.SetFloat("GlobalFogNoisePower", data.connector.GlobalFogNoisePower);
            _material.SetFloat("VolumeLightNoisePower", data.connector.VolumeLightNoisePower);
            _material.SetFloat("useTexture3DNoise", data.connector.useTexture3DNoise ? 1 : 0);
            _material.SetTexture("_NoiseTex1", data.connector._NoiseTex1);
            _material.SetFloat("attenExponent", data.connector.localLightAttenuation);//v1.4.3

            //v1.9.9
            _material.SetVector("lightControlA", data.connector.lightControlA);
            _material.SetVector("lightControlB", data.connector.lightControlB);
            if (data.connector.lightA)
            {
                _material.SetVector("lightAcolor", new Vector3(data.connector.lightA.color.r, data.connector.lightA.color.g, data.connector.lightA.color.b));
                _material.SetFloat("lightAIntensity", data.connector.lightA.intensity);
            }
            if (data.connector.lightB)
            {
                _material.SetVector("lightBcolor", new Vector3(data.connector.lightB.color.r, data.connector.lightB.color.g, data.connector.lightB.color.b));
                _material.SetFloat("lightBIntensity", data.connector.lightA.intensity);
            }
            if (data.connector.controlByColor)
            {
                _material.SetInt("controlByColor", 1);
            }
            else
            {
                _material.SetInt("controlByColor", 0);
            }

            //v1.9.9.3
            _material.SetVector("shadowsControl", data.connector.shadowsControl);

            //v1.9.9.4
            _material.SetVector("volumeSamplingControl", data.connector.volumeSamplingControl);

            //v1.9.9.1
            if (_material.HasProperty("lightsArrayLength") && data.connector.lightsArray.Count > 0) //check for other shader versions
            {
                _material.SetVectorArray("_LightsArrayPos", new Vector4[data.connector.maxImpostorLights]);
                _material.SetVectorArray("_LightsArrayDir", new Vector4[data.connector.maxImpostorLights]);
                int countLights = data.connector.lightsArray.Count;
                if (countLights > data.connector.maxImpostorLights)
                {
                    countLights = data.connector.maxImpostorLights;
                }
                _material.SetInt("lightsArrayLength", countLights);
                Vector4[] posArray = new Vector4[countLights];
                Vector4[] dirArray = new Vector4[countLights];
                Vector4[] colArray = new Vector4[countLights];
                for (int i = 0; i < countLights; i++)
                {
                    //posArray[i].x = lightsArray(0).
                    posArray[i].x = data.connector.lightsArray[i].transform.position.x;
                    posArray[i].y = data.connector.lightsArray[i].transform.position.y;
                    posArray[i].z = data.connector.lightsArray[i].transform.position.z;
                    posArray[i].w = data.connector.lightsArray[i].intensity;
                    //Debug.Log(posArray[i].w);
                    colArray[i].x = data.connector.lightsArray[i].color.r;
                    colArray[i].y = data.connector.lightsArray[i].color.g;
                    colArray[i].z = data.connector.lightsArray[i].color.b;

                    //check if point light
                    if (data.connector.lightsArray[i].type == LightType.Point)
                    {
                        dirArray[i].x = 0;
                        dirArray[i].y = 0;
                        dirArray[i].z = 0;
                    }
                    else
                    {
                        dirArray[i].x = data.connector.lightsArray[i].transform.forward.x;
                        dirArray[i].y = data.connector.lightsArray[i].transform.forward.y;
                        dirArray[i].z = data.connector.lightsArray[i].transform.forward.z;
                    }
                    dirArray[i].w = data.connector.lightsArray[i].range;
                }
                _material.SetVectorArray("_LightsArrayPos", posArray);
                _material.SetVectorArray("_LightsArrayDir", dirArray);
                _material.SetVectorArray("_LightsArrayColor", colArray);
                //material.SetFloatArray(array);
            }
            else
            {
                _material.SetInt("lightsArrayLength", 0);
            }

            float Foggy = 0;
            if (data.connector.FogSky) //ClearSkyFac
            {
                Foggy = 1;
            }
            _material.SetFloat("FogSky", Foggy);
            _material.SetFloat("ClearSkyFac", data.connector.ClearSkyFac);
            //////// END SCATTER

            //LOCAL LIGHT
            _material.SetVector("localLightPos", new Vector4(data.connector.PointL.x, data.connector.PointL.y, data.connector.PointL.z, data.connector.PointL.w));//68, 155, 345
            _material.SetVector("localLightColor", new Vector4(data.connector.PointLParams.x, data.connector.PointLParams.y, data.connector.PointLParams.z, data.connector.PointLParams.w));

            //v0.6
            _material.SetFloat("depthDilation", data.connector.depthDilation);
            _material.SetFloat("_TemporalResponse", data.connector.TemporalResponse);
            _material.SetFloat("_TemporalGain", data.connector.TemporalGain);




            //RENDER FINAL EFFECT
            int rtW = opaqueDesc.width;
            int rtH = opaqueDesc.height;
            //var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; //v3.4.9
            var format = data.connector.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default; 

            RenderTexture tmpBuffer1 = RenderTexture.GetTemporary(rtW, rtH, 0, format);

            RenderTexture.active = tmpBuffer1;

            GL.ClearWithSkybox(false, camera);
            
            cmd.Blit(source, tmpBuffer1);
            


            if (!data.connector.enableComposite)
            {
                _material.SetTexture("_MainTex", tmpBuffer1);

                //WORLD RECONSTRUCT        
                Matrix4x4 camToWorld = camera.cameraToWorldMatrix;
                _material.SetMatrix("_InverseView", camToWorld);

                RenderTexture tmpBuffer2 = RenderTexture.GetTemporary((int)(rtW / data.connector.downSample), (int)(rtH / data.connector.downSample), 0, format);



                cmd.Blit(tmpBuffer1, tmpBuffer2, _material, 6);



                if (data.previousFrameTexture == null)
                {
                    data.previousFrameTexture = new RenderTexture((int)(rtW / ((float)data.connector.downSample)), (int)(rtH / ((float)data.connector.downSample)), 0, format);// RenderTextureFormat.DefaultHDR);//v0.7
                    data.previousFrameTexture.filterMode = FilterMode.Point;
                    data.previousFrameTexture.Create();
                }
                if (data.previousDepthTexture == null)
                {
                    data.previousDepthTexture = new RenderTexture((int)(rtW / ((float)data.connector.downSample)), (int)(rtH / ((float)data.connector.downSample)), 0, RenderTextureFormat.RFloat);
                    data.previousDepthTexture.filterMode = FilterMode.Point;
                    data.previousDepthTexture.Create();
                }
                RenderTexture tmpBuffer3 = RenderTexture.GetTemporary((int)(rtW / ((float)data.connector.downSample)), (int)(rtH / ((float)data.connector.downSample)), 0, format);
                //bool temporalAntiAliasing = true;
                if (data.connector.enabledTemporalAA && Time.fixedTime > 0.05f) //if (temporalAntiAliasing)
                {
                    //Debug.Log("AA Enabled");
                    var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                    var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                    _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                    data.viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                    _material.SetMatrix("_InverseViewProjectionMatrix", data.viewProjectionMatrix.inverse);
                    _material.SetMatrix("_LastFrameViewProjectionMatrix", data.lastFrameViewProjectionMatrix);
                    _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", data.lastFrameInverseViewProjectionMatrix);
                    _material.SetTexture("_ColorBuffer", tmpBuffer2);//CloudMaterial.SetTexture("_CloudTex", rtClouds);
                    _material.SetTexture("_PreviousColor", data.previousFrameTexture);
                    _material.SetTexture("_PreviousDepth", data.previousDepthTexture);

                    //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                    Matrix4x4 mt = data.lastFrameViewProjectionMatrix * cameraData.camera.cameraToWorldMatrix;
                    _material.SetMatrix("_FrameMatrix", mt);

                    //if (mesh == null)
                    //{
                    //    Awake();
                    //}
                    //v0.2

                    Blitter.BlitTexture(cmd, data.m_CameraColorTarget, new Vector4(1, 1, 0, 0), _material, 20);
                    //                Blitter.BlitCameraTexture(cmd, data.m_CameraColorTarget, data._handleA, _material, 20);
                    //cmd.Blit(m_CameraColorTarget, tmpBuffer3);

                    //Blitter.

                    cmd.Blit(_handleA, previousFrameTexture);
                    cmd.Blit(_handleA, tmpBuffer2);

                    //v0.2
                    Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 21);
                    cmd.Blit(_handleA, previousDepthTexture);

                }
                //Blit(cmd, tmpBuffer2, source);
                //v0.6a
                RenderTexture tmpBuffer4 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                if (enableBlendMode)
                {
                    //v0.6a
                    _material.SetFloat("controlBackAlphaPower", controlBackAlphaPower);
                    _material.SetFloat("controlCloudAlphaPower", controlCloudAlphaPower);
                    _material.SetVector("controlCloudEdgeA", controlCloudEdgeA);                   
                    _material.SetTexture("_ColorBuffer", tmpBuffer2);
                    _material.SetTexture("_ManTex", tmpBuffer1);
                    
                    //v0.2
                    Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, _material, 22);
                    cmd.Blit(m_CameraColorTarget, tmpBuffer4);


                    //v0.1
                    cmd.Blit(tmpBuffer4, source);
                }
                else
                {
                    //v0.1
                    cmd.Blit(tmpBuffer2, source);
                }

                RenderTexture.ReleaseTemporary(tmpBuffer1); RenderTexture.ReleaseTemporary(tmpBuffer2);
                
                context.ExecuteCommandBuffer(cmd);

                //v0.6
                RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4);
                lastFrameViewProjectionMatrix = viewProjectionMatrix;
                lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
            }
            else
            {
                //SSMS
                //_material.SetTexture("_MainTex", tmpBuffer1); //SSMS

                //WORLD RECONSTRUCT    
                Matrix4x4 camToWorld = camera.cameraToWorldMatrix;// context.camera.cameraToWorldMatrix;
                //Debug.Log(camToWorld);
                _material.SetMatrix("_InverseView", camToWorld);

                //bool enableComposite = true;
                int dimsW = rtW;
                int dimsH = rtH;
                if (enableComposite)
                {
                    //
                }
                else
                {
                    _material.SetTexture("_MainTex", tmpBuffer1);//SSMS
                    dimsW = (int)(rtW / downSample);
                    dimsH = (int)(rtH / downSample);
                }
                //v0.6
                //int downSample = 2;
                RenderTexture tmpBuffer2 = RenderTexture.GetTemporary(dimsW, dimsH, 0, format);//SSMS
                RenderTexture tmpBuffer3 = RenderTexture.GetTemporary((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, format);

                RenderTexture tmpBuffer2a = RenderTexture.GetTemporary((int)(rtW / downSample), (int)(rtH / downSample), 0, format);
                if (enableComposite)
                {
                    //v0.1
                    cmd.Blit(tmpBuffer2a, tmpBuffer2a, _material, 6);//SSMS
                    if (previousFrameTexture == null)
                    {
                        previousFrameTexture = new RenderTexture((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, format);// RenderTextureFormat.DefaultHDR);//v0.7
                        previousFrameTexture.filterMode = FilterMode.Point;
                        previousFrameTexture.Create();
                    }
                    if (previousDepthTexture == null)
                    {
                        previousDepthTexture = new RenderTexture((int)(rtW / ((float)downSampleAA)), (int)(rtH / ((float)downSampleAA)), 0, RenderTextureFormat.RFloat);
                        previousDepthTexture.filterMode = FilterMode.Point;
                        previousDepthTexture.Create();
                    }
                    if (enabledTemporalAA && Time.fixedTime > 0.05f) //if (temporalAntiAliasing)
                    {
                        //Debug.Log("AA Enabled");
                        var worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
                        _material.SetMatrix("_InverseProjectionMatrix", projectionMatrix.inverse);
                        viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
                        _material.SetMatrix("_InverseViewProjectionMatrix", viewProjectionMatrix.inverse);
                        _material.SetMatrix("_LastFrameViewProjectionMatrix", lastFrameViewProjectionMatrix);
                        _material.SetMatrix("_LastFrameInverseViewProjectionMatrix", lastFrameInverseViewProjectionMatrix);
                        _material.SetTexture("_ColorBuffer", tmpBuffer2a);//CloudMaterial.SetTexture("_CloudTex", rtClouds);
                        _material.SetTexture("_PreviousColor", previousFrameTexture);
                        _material.SetTexture("_PreviousDepth", previousDepthTexture);

                        //https://github.com/CMDRSpirit/URPTemporalAA/blob/86f4d28bc5ee8115bff87ee61afe398a6b03f61a/TemporalAA/TemporalAAFeature.cs#L134
                        Matrix4x4 mt = lastFrameViewProjectionMatrix * renderingData.cameraData.camera.cameraToWorldMatrix;
                        _material.SetMatrix("_FrameMatrix", mt);

                        if (mesh == null)
                        {
                            Awake();
                        }
                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 20);
                        //cmd.Blit(m_CameraColorTarget, tmpBuffer3);

                        cmd.Blit(_handleA, previousFrameTexture);
                        cmd.Blit(_handleA, tmpBuffer2a);

                        //v0.2
                        Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, _handleA, _material, 21);
                        cmd.Blit(_handleA, previousDepthTexture);
                    }

                    //SSMS           
                    Shader.SetGlobalTexture("_FogTex", tmpBuffer2a);
                    //_material.SetTexture("_MainTex", tmpBuffer1);//SSMS
                    _material.SetTexture("_BaseTex", tmpBuffer1);//SSMS
                                                                 //v0.1
                    cmd.Blit(tmpBuffer1, tmpBuffer2, _material, 19);//SSMS
                }
                else
                {
                    //v0.1
                    cmd.Blit(tmpBuffer1, source, _material, 6);//v0.6
                }

                //v0.6a
                RenderTexture tmpBuffer4 = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                if (enableBlendMode)
                {
                    //v0.6a
                    _material.SetFloat("controlBackAlphaPower", controlBackAlphaPower);
                    _material.SetFloat("controlCloudAlphaPower", controlCloudAlphaPower);
                    _material.SetVector("controlCloudEdgeA", controlCloudEdgeA);
                    _material.SetTexture("_ColorBuffer", tmpBuffer2);
                    _material.SetTexture("_ManTex", tmpBuffer1);
                    Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, _material, 22);
                    cmd.Blit(m_CameraColorTarget, tmpBuffer4);
                    //v0.1
                    cmd.Blit(tmpBuffer4, source);
                }
                else
                {
                    //v0.1
                    cmd.Blit(tmpBuffer2, source);
                }

                RenderTexture.ReleaseTemporary(tmpBuffer1); RenderTexture.ReleaseTemporary(tmpBuffer2);
                //END RENDER FINAL EFFECT

                context.ExecuteCommandBuffer(cmd);
                //v0.6
                RenderTexture.ReleaseTemporary(tmpBuffer3); RenderTexture.ReleaseTemporary(tmpBuffer4); RenderTexture.ReleaseTemporary(tmpBuffer2a);
                lastFrameViewProjectionMatrix = viewProjectionMatrix;
                lastFrameInverseViewProjectionMatrix = viewProjectionMatrix.inverse;
            }
        }
        */
        /////////////////////// END VOLUME FOG SRP /////////////////////////////////

    }
}
