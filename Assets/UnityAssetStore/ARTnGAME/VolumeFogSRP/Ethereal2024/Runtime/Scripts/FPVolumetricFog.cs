// Ported from HDRP Volumetric fog
// Limitation: Only works for URP Forward+

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Artngame.SKYMASTER.EtherealVolumetrics
{
    public class FPVolumetricFog : ScriptableRendererFeature
    {
        public VolumetricConfig config;

        public RenderPassEvent passeEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private GenerateMaxZPass m_GenerateMaxZPass;
        private FPVolumetricLightingPass m_VolumetricLightingPass;
        private VBufferParameters m_VBufferParameters;

        public override void Create()
        {
            m_GenerateMaxZPass = new GenerateMaxZPass();
            m_VolumetricLightingPass = new FPVolumetricLightingPass(passeEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (config == null)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            if (!config.volumetricLighting)
                return;

#if UNITY_EDITOR
            // Only activate volumetric lighting in scene view if edit mode.
            // If playing, activate feature only for game view.
            if (renderingData.cameraData.cameraType == CameraType.SceneView)
            {
                if (Application.isPlaying)
                    return;
            }
            else if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                //v0.1
                //if (!Application.isPlaying)
                //    return;
            }
            else // Skip if other camera types
            {
                return;
            }
#endif

            //v0.1
            if (Camera.main != null)
            {
                Ethereal2024 etheral = Camera.main.GetComponent<Ethereal2024>();
                if (etheral != null && etheral.config != null && etheral.enabled && etheral.enableVolumeLights)
                {
                    config = etheral.config;
                }
                if (etheral != null)
                {
                    if (!etheral.enabled || !etheral.enableVolumeLights)
                    {
                        return;
                    }
                }
            }

            m_VBufferParameters = VolumetricUtils.ComputeVolumetricBufferParameters(config, renderingData.cameraData.camera);

            m_GenerateMaxZPass.Setup(config, m_VBufferParameters);
            renderer.EnqueuePass(m_GenerateMaxZPass);
            m_VolumetricLightingPass.Setup(config, m_VBufferParameters);
            renderer.EnqueuePass(m_VolumetricLightingPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_GenerateMaxZPass.Dispose();
            m_VolumetricLightingPass.Dispose();
        }

    }

}

