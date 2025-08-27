#ifndef UNITY_VOLUMETRIC_LIGHTING_COMMON_INCLUDED
#define UNITY_VOLUMETRIC_LIGHTING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
real LerpWhiteTo(real b, real t) { return (1.0 - t) + b * t; }  // To prevent compile error
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

CBUFFER_START(ShaderVariablesFog)
    uint        _FogEnabled;
    uint        _EnableVolumetricFog;
    uint        _FogColorMode;
    uint        _MaxEnvCubemapMip;
    float4      _FogColor;
    float4      _MipFogParameters;
    float4      _HeightFogParams;
    float4      _HeightFogBaseScattering;
    uint        _CullLightCount;
CBUFFER_END

#define FOGCOLORMODE_SKY_COLOR              1   // 0 = Constant color
#define ENVCONSTANTS_CONVOLUTION_MIP_COUNT  _MaxEnvCubemapMip
#define _MipFogNear                         _MipFogParameters.x
#define _MipFogFar                          _MipFogParameters.y
#define _MipFogMaxMip                       _MipFogParameters.z
#define _HeightFogBaseHeight                _HeightFogParams.x
#define _HeightFogBaseExtinction            _HeightFogParams.y
#define _HeightFogExponents                 _HeightFogParams.zw

CBUFFER_START(ShaderVariablesVolumetricLighting)
    uint        _VolumetricFilteringEnabled;
    uint        _VBufferHistoryIsValid;
    uint        _VBufferSliceCount;
    float       _VBufferAnisotropy;
    float       _CornetteShanksConstant;
    float       _VBufferVoxelSize;
    float       _VBufferRcpSliceCount;
    float       _VBufferUnitDepthTexelSpacing;
    float       _VBufferScatteringIntensity;
    float       _VBufferLocalScatteringIntensity;
    float       _VBufferLastSliceDist;
    float       _vbuffer_pad00_;
    float4      _VBufferViewportSize;
    float4      _VBufferLightingViewportScale;
    float4      _VBufferLightingViewportLimit;
    float4      _VBufferDistanceEncodingParams;
    float4      _VBufferDistanceDecodingParams;
    float4      _VBufferSampleOffset;
    float4      _VLightingRTHandleScale;
    float4x4    _VBufferCoordToViewDirWS;
CBUFFER_END

CBUFFER_START(ShaderVariablesLocalVolume)
    float4      _VolumetricMaterialObbRight;
    float4      _VolumetricMaterialObbUp;
    float4      _VolumetricMaterialObbExtents;
    float4      _VolumetricMaterialObbCenter;
    float4      _VolumetricMaterialAlbedo;
    float4      _VolumetricMaterialRcpPosFaceFade;
    float4      _VolumetricMaterialRcpNegFaceFade;
    float       _VolumetricMaterialInvertFade;
    float       _VolumetricMaterialExtinction;
    float       _VolumetricMaterialRcpDistFadeLen;
    float       _VolumetricMaterialEndTimesRcpDistFadeLen;
    float       _VolumetricMaterialFalloffMode;
    float       _LocalVolume_pad0_;
    float       _LocalVolume_pad1_;
    float       _LocalVolume_pad2_;
CBUFFER_END


struct JitteredRay
{
    float3 originWS;
    float3 centerDirWS;
    float3 jitterDirWS;
    float3 xDirDerivWS;
    float3 yDirDerivWS;
    float  geomDist;

    float maxDist;
};

struct VoxelLighting
{
    float3 radianceComplete;
    float3 radianceNoPhase;
};

// Returns the forward (up) direction of the current view in the world space.
float3 GetViewUpDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return viewMat[1].xyz;
}

float GetInversePreviousExposureMultiplier()
{
    return 1.0f;
}
float GetCurrentExposureMultiplierA()
{
    return 1.0f;
}

// Copied from EntityLighting
real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

bool IsInRange(float x, float2 range)
{
    return clamp(x, range.x, range.y) == x;
}

// To avoid half precision issue on mobile, declare float functions.
float4 LinearizeRGBD_Float(float4 value)
{
    float d = value.a;
    float a = 1 - exp(-d);
    float r = (a >= FLT_EPS) ? (d * rcp(a)) : 1;
    return float4(r * value.rgb, d);
}
float4 DelinearizeRGBA_Float(float4 value)
{
    float d = value.a;
    float a = 1 - exp(-d);
    float i = (a >= FLT_EPS) ? (a * rcp(d)) : 1;
    return float4(i * value.rgb, a);
}
float4 DelinearizeRGBD_Float(real4 value)
{
    float d = value.a;
    float a = 1 - exp(-d);
    float i = (a >= FLT_EPS) ? (a * rcp(d)) : 1; // Prevent numerical explosion
    return float4(i * value.rgb, d);
}
float SafeDiv_Float(float numer, float denom)
{
    return (numer != denom) ? numer / denom : 1;
}
//

// Make new cookie sampling function to avoid 'cannot map expression to cs_5_0 instruction' error
real3 SampleMainLightCookieForVoxelLighting(float3 samplePositionWS)
{
    if(!IsMainLightCookieEnabled())
        return real3(1,1,1);

    float2 uv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, samplePositionWS, float4(1, 1, 0, 0), URP_TEXTURE_WRAP_MODE_NONE);
    real4 color = SAMPLE_TEXTURE2D_LOD(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv, 0);

    return IsMainLightCookieTextureRGBFormat() ? color.rgb
             : IsMainLightCookieTextureAlphaFormat() ? color.aaa
             : color.rrr;
}

VoxelLighting EvaluateVoxelLightingDirectional(float extinction, float anisotropy,
                                               JitteredRay ray, float t0, float t1, float dt, float rndVal)
{
    VoxelLighting lighting;
    ZERO_INITIALIZE(VoxelLighting, lighting);

    const float NdotL = 1;

    float tOffset, weight;
    ImportanceSampleHomogeneousMedium(rndVal, extinction, dt, tOffset, weight);

    float t = t0 + tOffset;
    float3 positionWS = ray.originWS + t * ray.jitterDirWS;

    // Main light
    {
        float  cosTheta = dot(_MainLightPosition.xyz, ray.centerDirWS);
        float  phase = CornetteShanksPhasePartVarying(anisotropy, cosTheta);

        // Evaluate sun shadow
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
        shadowCoord.w = max(shadowCoord.w, 0.001);
        Light mainLight = GetMainLight();
        mainLight.shadowAttenuation = MainLightShadow(shadowCoord, positionWS, 0, 0);
        half3 color = mainLight.color * lerp(_VBufferScatteringIntensity, mainLight.shadowAttenuation, mainLight.shadowAttenuation < 1);

        // Cookie
    #if defined(_LIGHT_COOKIES)
        color *= SampleMainLightCookieForVoxelLighting(positionWS);
    #endif

        lighting.radianceNoPhase += color * weight;
        lighting.radianceComplete += color * weight * phase;
    }

    // Additional light
#if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        float4 lightPositionWS = _AdditionalLightsBuffer[lightIndex].position;
        half3 color = _AdditionalLightsBuffer[lightIndex].color.rgb;
    #else
        float4 lightPositionWS = _AdditionalLightsPosition[lightIndex];
        half3 color = _AdditionalLightsColor[lightIndex].rgb;
    #endif
        
        float  cosTheta = dot(lightPositionWS.xyz, ray.centerDirWS);
        float  phase = CornetteShanksPhasePartVarying(anisotropy, cosTheta);

    #if SUPPORT_ADDITIONAL_SHADOWS
        // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
        // This way the following code will work for both directional and punctual lights.
        float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
        float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

        half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
        half shadowAtten = AdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);
        color *= lerp(_VBufferScatteringIntensity, shadowAtten, shadowAtten < 1);
    #else
        color *= _VBufferScatteringIntensity;
    #endif

        // Cookie
    #if defined(_LIGHT_COOKIES)
        color *= SampleAdditionalLightCookie(lightIndex, positionWS);
    #endif

        lighting.radianceNoPhase += color * weight;
        lighting.radianceComplete += color * weight * phase;
    }
#endif

    return lighting;
}


//v0.1
float UVRandom(float2 uv)
{
    float f = dot(float2(12.9898, 78.233), uv);
    return frac(43758.5453 * sin(f));
}

VoxelLighting EvaluateVoxelLightingLocal(float2 pixelCoord, float extinction, float anisotropy,
                                         JitteredRay ray, float t0, float t1, float dt,
                                         float3 centerWS, float rndVal)
{
    VoxelLighting lighting;
    ZERO_INITIALIZE(VoxelLighting, lighting);

#if USE_FORWARD_PLUS

    uint lightIndex;
    ClusterIterator _urp_internal_clusterIterator = ClusterInit(GetNormalizedScreenSpaceUV(pixelCoord), centerWS, 0);
    [loop]
    while (ClusterNext(_urp_internal_clusterIterator, lightIndex))
    {
        lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT;

    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        float4 lightPositionWS = _AdditionalLightsBuffer[lightIndex].position;
        half3 color = _AdditionalLightsBuffer[lightIndex].color.rgb;
        half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[lightIndex].attenuation;
        half4 spotDirection = _AdditionalLightsBuffer[lightIndex].spotDirection;
        uint lightLayerMask = _AdditionalLightsBuffer[lightIndex].layerMask;
    #else
        float4 lightPositionWS = _AdditionalLightsPosition[lightIndex];
        half3 color = _AdditionalLightsColor[lightIndex].rgb;
        half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[lightIndex];
        half4 spotDirection = _AdditionalLightsSpotDir[lightIndex];
        uint lightLayerMask = asuint(_AdditionalLightsLayerMasks[lightIndex]);
    #endif

        // Jitter
        float lightSqRadius = rcp(distanceAndSpotAttenuation.x);
        float t, distSq, rcpPdf;
        ImportanceSamplePunctualLight(rndVal, lightPositionWS.xyz, lightSqRadius,
                                      ray.originWS, ray.jitterDirWS, t0, t1,
                                      t, distSq, rcpPdf);
        float3 positionWS = ray.originWS + t * ray.jitterDirWS;

        float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
        float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

        half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
        float attenuation = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);
        color *= attenuation;

    #if SUPPORT_ADDITIONAL_SHADOWS
        half shadowAtten = AdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);
        color *= lerp(_VBufferLocalScatteringIntensity, shadowAtten, shadowAtten < 1);
    #else
        color *= _VBufferLocalScatteringIntensity;
    #endif

        // Cookie
    #if defined(_LIGHT_COOKIES)
        color *= SampleAdditionalLightCookie(lightIndex, positionWS);
    #endif

        float3 centerL  = lightPositionWS.wyz - centerWS;
        float  cosTheta = dot(centerL, ray.centerDirWS) * rsqrt(dot(centerL, centerL));
        float  phase = CornetteShanksPhasePartVarying(anisotropy, cosTheta);

        // Compute transmittance from 't0' to 't'.
        float weight = TransmittanceHomogeneousMedium(extinction, t - t0) * rcpPdf;

        lighting.radianceNoPhase += color * weight;
        lighting.radianceComplete += color * weight * phase;
    }
    //////////////////////////////////////////////////////////////////////////////////////////
#else

    //float3 posToCameraA = (ray.originWS - _WorldSpaceCameraPos);
    //float dist = length(posToCameraA);
    //float _RaySamples = 1;
    //float steps = _RaySamples * 1; //v1.5
    //float stepLength = dist / steps;
    //float3 step = ray.jitterDirWS * stepLength;
    //float steps2 = steps;
    //float3 step = ray * stepLength;
    float3 pos = 0;// ray.originWS + step;
    //DIRECTIONAL
    //Light directionalLight = GetMainLight();
    //half shadowdirectionalLight = GetMainLightShadowStrength();
   // ShadowSamplingData shadowSampleData = GetMainLightShadowSamplingData();

   // float3 stepD = step;
    float o = 0.75;
    float2 uv = float2(0.5, 0.5);

    uint _visibleLightsCount = _CullLightCount;// 51u;

    int controlByColor = 0;
    float4 lightAcolor = float4(1, 1, 1, 1);
    float lightAIntensity = 11;

    float4 volumeSamplingControl = float4(1, 1, 1, 1)*0.1;
    float4 shadowsControl = float4(1, 1, 1, 1) * 0.1;
    float4 lightControlA = float4(1, 1, 1, 1) * 0.1;
    float4 _stepsControl = float4(1, 1, 1, 1);
    float attenuate = 0;
    float3 WorldPosition = centerWS;// ray.originWS;    

    float3 sourceImg = lighting.radianceComplete;

    
  
    if (lightControlA.y != 0) {
        //https://forum.unity.com/threads/how-to-make-additional-lights-cast-shadows-toon-shader-setup.757730/		
        half distanceAtten = 0;
#if defined _ADDITIONAL_LIGHTS //|| defined USE_FORWARD_PLUS      
        //https://github.com/Unity-Technologies/Graphics/blob/6de5959b69ae36a22e0745974809353e03665654/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl       
                int m = 0;
                [loop]//[unroll] //[loop]//[unroll]
                for (uint k = 0u; k < _visibleLightsCount; ++k)
                {

                    //v0.2
                    float4 lightPositionWS = _AdditionalLightsPosition[k];
                    half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[k];
                    // Jitter
                    float lightSqRadius = rcp(distanceAndSpotAttenuation.x);
                    float t, distSq, rcpPdf;
                    ImportanceSamplePunctualLight(rndVal, lightPositionWS.xyz, lightSqRadius,
                        ray.originWS, ray.jitterDirWS, t0, t1,
                        t, distSq, rcpPdf);
                    float3 positionWS = ray.originWS + t * ray.jitterDirWS;
                    pos = positionWS;
                    half4 spotDirection = _AdditionalLightsSpotDir[k];
                    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
                    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
                    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
                    float attenuationDIST = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);
                   
                    if (attenuationDIST < 0.00005) {
                        continue;
                    }

                    //v0.2
                    half shadowAttenA = 1;
#if SUPPORT_ADDITIONAL_SHADOWS                   
                    shadowAttenA = AdditionalLightRealtimeShadow(k, pos, lightDirection);// light.direction);                
#else
                    //
#endif                 
                    float weight = saturate(TransmittanceHomogeneousMedium(extinction, t - t0) * rcpPdf); 

                    half3 color = _AdditionalLightsColor[k].rgb * weight;

                    
#if defined(_LIGHT_COOKIES)
                    sourceImg = sourceImg + color * shadowAttenA * (attenuationDIST) * 75 * SampleAdditionalLightCookie(k, positionWS);
#else
                    sourceImg = sourceImg + color * shadowAttenA * (attenuationDIST) * 75;// 100;
#endif
                    
                }
#endif
    }    
    lighting.radianceNoPhase = sourceImg * 1.2 * 1;// sourceImg * 5;// *1 + float4(0.1, 0, 0, 1);
    lighting.radianceComplete = sourceImg * 1.2 * 1;// sourceImg * 5;// *1 + float4(0.1, 0, 0, 1);
#endif

    return lighting;
}

#endif


/*

//float3 posToCameraA = (ray.originWS - _WorldSpaceCameraPos);
    //float dist = length(posToCameraA);
    //float _RaySamples = 1;
    //float steps = _RaySamples * 1; //v1.5
    //float stepLength = dist / steps;
    //float3 step = ray.jitterDirWS * stepLength;
    //float steps2 = steps;
    //float3 step = ray * stepLength;
    float3 pos = 0;// ray.originWS + step;
    //DIRECTIONAL
    //Light directionalLight = GetMainLight();
    //half shadowdirectionalLight = GetMainLightShadowStrength();
   // ShadowSamplingData shadowSampleData = GetMainLightShadowSamplingData();

   // float3 stepD = step;
    float o = 0.75;
    float2 uv = float2(0.5, 0.5);

    uint _visibleLightsCount = 51u;
    int lightCount = 34;

    int controlByColor = 0;
    float4 lightAcolor = float4(1, 1, 1, 1);
    float lightAIntensity = 11;

    float4 volumeSamplingControl = float4(1, 1, 1, 1)*0.1;
    float4 shadowsControl = float4(1, 1, 1, 1) * 0.1;
    float4 lightControlA = float4(1, 1, 1, 1) * 0.1;
    float4 _stepsControl = float4(1, 1, 1, 1);
    float attenuate = 0;
    float3 WorldPosition = centerWS;// ray.originWS;



    float3 sourceImg = lighting.radianceComplete;
    //1.9.9
    
    if (lightControlA.x != 0) {

        [loop]
        for (int i = 0; i < steps; ++i)
        {
            float4 coordinates = TransformWorldToShadowCoord(pos);

            attenuate = SampleShadowmap(coordinates, TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture),
                shadowSampleData, shadowdirectionalLight, false);

            //v1.1.6 ETHEREAL
            float divi = 1;
            if (length(pos - _WorldSpaceCameraPos) > length(_WorldSpaceCameraPos - WorldPosition))
            {
                divi = 0; break;
            }

            //v1.9.9.3
            float attn = attenuate;
            if (shadowsControl.w == 0) {//v1.1.9 - enable legacy if zero
                if (length(pos - _WorldSpaceCameraPos) > shadowsControl.x)
                {
                    attn = 1;
                }
               // sourceImg = sourceImg * 1 + o * 0.1 * sourceImg * directionalLight.color * attn * lightControlA.x * divi
               //     - pow(1 - attn, shadowsControl.y * 65) * 0.001 * shadowsControl.z * 0.01;
            }
            else {
                float diff = length(pos - _WorldSpaceCameraPos) - shadowsControl.x;
                if (length(pos - _WorldSpaceCameraPos) < shadowsControl.x || length(pos - _WorldSpaceCameraPos) > 100 * shadowsControl.y)
                {
                    if (length(pos - _WorldSpaceCameraPos) > 100 * shadowsControl.y)
                    {
                        attn = 1.2 * shadowsControl.w;
                    }
                   // sourceImg = sourceImg + 15 * abs(o * 0.01 * sourceImg * directionalLight.color * attn * lightControlA.x);
                }
            }
            float rand = UVRandom(uv + i + 1);
            //pos += (step + step * rand * 0.8);
            pos += (stepD + stepD * rand * 0.8 * volumeSamplingControl.w); //v1.1.8f
        }
    }
    
    //int lightCountA = 5; //v1.7

    //SPOT - POINT LIGHTS
    //v1.9.9
if (lightControlA.y != 0) {
    //https://forum.unity.com/threads/how-to-make-additional-lights-cast-shadows-toon-shader-setup.757730/		
    half distanceAtten = 0;
#if defined _ADDITIONAL_LIGHTS //|| defined USE_FORWARD_PLUS
    //int pixelLightCount = GetAdditionalLightsCount();
    //https://github.com/Unity-Technologies/Graphics/blob/6de5959b69ae36a22e0745974809353e03665654/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl
    //[unroll]
    //for (uint k = 0u; k < 1; ++k);// for (int k = 0; k < 3; ++k); //for (int k = 0; k < pixelLightCount; ++k)
   // {
        //float3 stepA = ray.centerDirWS * 2; //ray * stepLength;
        //pos = ray.originWS + stepA;//pos = rayStart + stepA;

        //float distToRayStart = length(ray.originWS - pos);//length(rayStart - pos);
        //float steps2 = steps;

        //float powDIVIDE = (pow(distToRayStart, 0.7 * _stepsControl.y) * _stepsControl.z) * lightControlA.y; //v1.9.9

        //[loop]
        //for (int m = 0; m < steps2; ++m)
        //{
    int m = 0;
    // Light lightA = GetAdditionalPerObjectLight(4, pos);
     //sourceImg = sourceImg +0.00001 * 1* abs(stepA) + 1111111 * lightA.distanceAttenuation * lightA.color;

    [unroll] //v1.9.9.8 - Ethereal v1.1.8h
   // for (int k = 0; k < _visibleLightsCount; k++) //v1.9.9.8 - Ethereal v1.1.8h
   // uint pixelLightCount = GetAdditionalLightsCount();
    for (uint k = 0u; k < _visibleLightsCount; ++k)
    {

        //v0.2
        float4 lightPositionWS = _AdditionalLightsPosition[k];
        half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[k];
        // Jitter
        float lightSqRadius = rcp(distanceAndSpotAttenuation.x);
        float t, distSq, rcpPdf;
        ImportanceSamplePunctualLight(rndVal, lightPositionWS.xyz, lightSqRadius,
            ray.originWS, ray.jitterDirWS, t0, t1,
            t, distSq, rcpPdf);
        float3 positionWS = ray.originWS + t * ray.jitterDirWS;
        pos = positionWS;
        half4 spotDirection = _AdditionalLightsSpotDir[k];
        float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
        float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
        half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
        float attenuationDIST = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);
        //color *= attenuation;


        //v1.1.8f
       // float lightPower3 = 1;
       // if (_stepsControl.x != 0 && length(pos - _WorldSpaceCameraPos) > length(WorldPosition - _WorldSpaceCameraPos)) { //if behind obstacle, zero intensity
       //     lightPower3 = 0;
       // }

        //LIGHT 1
       // float distToRayStartA = length(_WorldSpaceCameraPos - pos);//v1.9.9.8 - Ethereal v1.1.8h

//#ifdef URP11//v1.8
                   // Light light = GetAdditionalPerObjectLight(k, pos);
                    //half shadowAtten;// = AdditionalLightRealtimeShadow(k, pos, light.direction);
                   // shadowAtten = distanceAndSpotAttenuation*1;
                    //light.shadowAttenuation = AdditionalLightShadow(k, pos, light.direction, half4(1, 1, 1, 1), half4(0, 0, 0, 0));
//#else
                   // Light light = GetAdditionalPerObjectLight(k, pos);// GetAdditionalLight(k, pos); //v0.4 URP 10 need an extra shadowmask variable
//#endif

                //LIGHT 1
                    
                    float multLightPow = 0.4300346225501;// 0.6805029349687388;// 17088.109200; for 188, for 128  =0.4300346225501
                    if ((lightCount < 0 && distToRayStartA < abs(lightCount * 0.001)) || (lightCount > k && _visibleLightsCount > k + 1)) {//v1.9.9.5 if (_visibleLightsCount > 1) {//v1.9.9.8
                        if (controlByColor == 0 || (controlByColor == 1 &&
                            ((light.color.r == lightAcolor.x * multLightPow * (lightAIntensity)
                                || light.color.g == lightAcolor.y * multLightPow * (lightAIntensity)
                                || light.color.b == lightAcolor.z * multLightPow * (lightAIntensity)
                                )
                                )
                            )) {

                            //sourceImg = sourceImg + light.color * pow(light.distanceAttenuation, 2) * light.shadowAttenuation;

                           // sourceImg = sourceImg +
                           //     (lightPower3 * lightControlA.z * o * 0.04 * sourceImg * light.color * pow(light.distanceAttenuation, 1) * light.shadowAttenuation) / powDIVIDE;
                        }
                    }
                    

                    //v0.2
        half shadowAttenA = 1;
#if SUPPORT_ADDITIONAL_SHADOWS
        //if (UVRandom(uv + 1) < 0.15) {
        shadowAttenA = AdditionalLightRealtimeShadow(k, pos, lightDirection);// light.direction);
        //}
        //sourceImg *= lerp(_VBufferLocalScatteringIntensity, shadowAttenA, shadowAttenA < 1);
        ///shadowAtten = shadowAttenA*shadowAtten;
#else
        //sourceImg *= _VBufferLocalScatteringIntensity;
#endif

                    //sourceImg = sourceImg + 1 * light.distanceAttenuation * light.color * shadowAtten +light.color * shadowAtten * pow(light.distanceAttenuation,2);// *pow(light.distanceAttenuation, 2)* light.shadowAttenuation;
                    //sourceImg = sourceImg + 1 * light.distanceAttenuation * light.color * shadowAtten + light.color * shadowAtten * pow(light.distanceAttenuation, 2);// *pow(light.distanceAttenuation, 2)* light.shadowAttenuation;

        half3 color = _AdditionalLightsColor[k].rgb;

#if defined(_LIGHT_COOKIES)
        sourceImg = sourceImg + color * shadowAttenA * (attenuationDIST) * 75 * SampleAdditionalLightCookie(k, positionWS);
#else
        sourceImg = sourceImg + color * shadowAttenA * (attenuationDIST) * 75;// 100;
#endif

    }//_visibleLightsCount

    //float rand = volumeSamplingControl.y * 0.1 * (1 - volumeSamplingControl.x)
    //    + volumeSamplingControl.z * UVRandom(uv + m + 1) * (volumeSamplingControl.x);

    //pos += stepA + stepA * rand * 0.8;
// }
//}
#endif
}
//sourceImg = saturate(sourceImg);
lighting.radianceNoPhase = sourceImg * 1.2;// sourceImg * 5;// *1 + float4(0.1, 0, 0, 1);
lighting.radianceComplete = sourceImg * 1.2;// sourceImg * 5;// *1 + float4(0.1, 0, 0, 1);
*/