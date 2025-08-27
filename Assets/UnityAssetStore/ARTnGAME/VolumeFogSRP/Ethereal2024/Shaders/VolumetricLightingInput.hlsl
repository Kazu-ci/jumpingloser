#ifndef UNITY_VOLUMETRIC_LIGHTING_INPUT_INCLUDED
#define UNITY_VOLUMETRIC_LIGHTING_INPUT_INCLUDED

float4x4 _PixelCoordToViewDirWS;
TEXTURE3D(_VBufferLighting);
SAMPLER(s_linear_clamp_sampler);

struct Attributes
{
    uint vertexID : SV_VertexID;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    // UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    // UNITY_SETUP_INSTANCE_ID(input);
    // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    return output;
}

//v0.1
uniform float4 _BlitScaleBias;
uniform float4 _BlitScaleBiasRt;
uniform float _BlitMipLevel;
uniform float2 _BlitTextureSize;
uniform uint _BlitPaddingSize;
uniform int _BlitTexArraySlice;
uniform float4 _BlitDecodeInstructions;

#if SHADER_API_GLES
struct AttributesB
{
    float4 positionOS       : POSITION;
    float2 uv               : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#else
struct AttributesB
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

struct VaryingsB
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsB VertA(AttributesB input)
{
    VaryingsB output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv = input.uv;
#else
    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

    output.positionCS = pos;
    output.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

VaryingsB VertQuad(AttributesB input)
{
    VaryingsB output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv = input.uv;
#else
    float4 pos = GetQuadVertexPosition(input.vertexID);
    float2 uv = GetQuadTexCoord(input.vertexID);
#endif

    output.positionCS = pos * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

#endif