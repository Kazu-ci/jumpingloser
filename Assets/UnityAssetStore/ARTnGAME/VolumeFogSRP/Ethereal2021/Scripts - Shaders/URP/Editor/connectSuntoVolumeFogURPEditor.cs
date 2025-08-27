using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Artngame.SKYMASTER
{

    [CustomEditor(typeof(connectSuntoVolumeFogURP))]
    //[CanEditMultipleObjects]
    public class connectSuntoVolumeFogURPEditor : Editor
    {
        SerializedProperty enableFog;
        SerializedProperty enableComposite;
        SerializedProperty enableWetnessHaze;
        SerializedProperty FogSky;

        void OnEnable()
        {
            enableFog = serializedObject.FindProperty("enableFog");
            enableComposite = serializedObject.FindProperty("enableComposite");
            enableWetnessHaze = serializedObject.FindProperty("enableWetnessHaze");
            FogSky = serializedObject.FindProperty("FogSky");
        }

        public bool enableEditor = false;
        public bool enableAdvanced = true;
        public bool enableLargeScaleHeightDensity = true;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("ENABLE QUICK SETTINGS");// SETUP EDITOR");
            enableEditor = EditorGUILayout.Toggle(enableEditor);

            EditorGUILayout.LabelField("ENABLE ADVANCED SETTINGS");
            enableAdvanced = EditorGUILayout.Toggle(enableAdvanced);

            if (enableEditor)
            {
                var volumeFog = (target as connectSuntoVolumeFogURP);

                serializedObject.Update();

                Undo.RecordObject(volumeFog, "Changed Fog");

                //EditorGUILayout.LabelField("------------------------------------------------------");
                //EditorGUILayout.LabelField("ENABLE FOG");
                //EditorGUILayout.LabelField("------------------------------------------------------");

                EditorGUILayout.PropertyField(enableFog);


                EditorGUILayout.LabelField("------------------------------------------------------");
                //EditorGUILayout.LabelField("------------------------------------------------------");

                //if (GUILayout.Button("PRESET BRIGHT"))
                //{
                //Debug.Log("It's alive: " + target.name);
                //}




                //ETHEREAL CONTROLS
                EditorGUILayout.LabelField("Blend Fog to Scene");
                volumeFog.blendVolumeLighting = EditorGUILayout.Slider(volumeFog.blendVolumeLighting, 0.001f, 10);
                EditorGUILayout.LabelField("Global Fog Power");
                volumeFog.GlobalFogPower = EditorGUILayout.Slider(volumeFog.GlobalFogPower, 0.001f, 5);

                //STEPS CONTROL
                EditorGUILayout.LabelField("Light Sampling Steps");
                volumeFog.LightRaySamples = EditorGUILayout.Slider(volumeFog.LightRaySamples, 1, 50);
                EditorGUILayout.LabelField("Downsample Factor");
                volumeFog.downSample = EditorGUILayout.Slider(volumeFog.downSample, 0.5f, 20);

                //SKY
                EditorGUILayout.PropertyField(FogSky);
                EditorGUILayout.LabelField("Fog on Sky Power");
                volumeFog.ClearSkyFac = EditorGUILayout.Slider(volumeFog.ClearSkyFac, -10, 10);

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Fog Color and Density", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                EditorGUILayout.LabelField("Fog Color");
                volumeFog._FogColor = EditorGUILayout.ColorField(volumeFog._FogColor);
                EditorGUILayout.LabelField("Fog Distance");
                volumeFog.startDistance = EditorGUILayout.Slider(volumeFog.startDistance, -10000, 10000);
                EditorGUILayout.LabelField("Fog Density");
                volumeFog._fogDensity = EditorGUILayout.Slider(volumeFog._fogDensity, 0, 10);
                EditorGUILayout.LabelField("Fog Height");
                volumeFog._fogHeight = EditorGUILayout.Slider(volumeFog._fogHeight, -1500, 1500);

                EditorGUILayout.LabelField("Enable Large Scale Heigth Fog Density");
                enableLargeScaleHeightDensity = EditorGUILayout.Toggle(enableLargeScaleHeightDensity);
                if (enableLargeScaleHeightDensity)
                {
                    EditorGUILayout.LabelField("Heigth Fog Density");
                    volumeFog.heightDensity = EditorGUILayout.Slider(volumeFog.heightDensity, -100000, 1000000000);
                }
                else
                {
                    EditorGUILayout.LabelField("Heigth Fog Density (Small Range)");
                    volumeFog.heightDensity = EditorGUILayout.Slider(volumeFog.heightDensity, -45, 45);
                }

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Fog Light Scatter", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                //SCATTER
                EditorGUILayout.LabelField("Fog Scatter");
                volumeFog.ScatterFac = EditorGUILayout.Slider(volumeFog.ScatterFac, 0, 500);
                EditorGUILayout.LabelField("Fog Scatter Contrast");
                volumeFog.contrast = EditorGUILayout.Slider(volumeFog.contrast, -30, 30);
                EditorGUILayout.LabelField("Fog Scatter Directional Power");
                volumeFog.mieDirectionalG = EditorGUILayout.Slider(volumeFog.mieDirectionalG, 0, 1);
                EditorGUILayout.LabelField("Fog Scatter Luminance");
                volumeFog.luminance = EditorGUILayout.Slider(volumeFog.luminance, -10, 10);
                EditorGUILayout.LabelField("Fog Scatter Luminance Exponent");
                volumeFog.lumFac = EditorGUILayout.Slider(volumeFog.lumFac, -5, 5);

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Fog Occlusion Control", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                //OCCLUSION
                EditorGUILayout.LabelField("Fog Occlusion Drop");
                volumeFog.occlusionDrop = EditorGUILayout.Slider(volumeFog.occlusionDrop, -15, 15);
                EditorGUILayout.LabelField("Fog Occlusion Exponent");
                volumeFog.occlusionExp = EditorGUILayout.Slider(volumeFog.occlusionExp, 0, 10);

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Fog Scene Composition & Multiscatter", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                //SCENE BLEND CONTROL
                EditorGUILayout.LabelField("Composite to Scene");
                EditorGUILayout.PropertyField(enableComposite);
                //if (volumeFog.enableComposite == false)
                //{
                //    volumeFog.enableWetnessHaze = false;
                //}

                //MULTISCATTER
                EditorGUILayout.LabelField("Enable Multiscatter mode, Requires Composite Mode");
                EditorGUILayout.PropertyField(enableWetnessHaze);
                EditorGUILayout.LabelField("Multiscatter Intensity");
                volumeFog.intensity = EditorGUILayout.Slider(volumeFog.intensity, 0f, 1);
                EditorGUILayout.LabelField("Multiscatter Radius");
                volumeFog.radius = EditorGUILayout.Slider(volumeFog.radius, 0f, 7);
                EditorGUILayout.LabelField("Multiscatter Blur Weight");
                volumeFog.blurWeight = EditorGUILayout.Slider(volumeFog.blurWeight, 0f, 100);

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Fog Noise Controls", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                //FOG NOISE
                EditorGUILayout.LabelField("Global Fog Noise Power");
                volumeFog.GlobalFogNoisePower = EditorGUILayout.Slider(volumeFog.GlobalFogNoisePower, 0.01f, 10);
                EditorGUILayout.LabelField("Volumetric Fog Noise Contrast");
                volumeFog.stepsControl.w = EditorGUILayout.Slider(volumeFog.stepsControl.w, 0.1f, 150);
                EditorGUILayout.LabelField("Volumetric Fog Noise Scaling A");
                volumeFog.lightNoiseControl.z = EditorGUILayout.Slider(volumeFog.lightNoiseControl.z, 0.01f, 50);
                EditorGUILayout.LabelField("Volumetric Fog Noise Speed A");
                volumeFog.lightNoiseControl.w = EditorGUILayout.Slider(volumeFog.lightNoiseControl.w, -1500, 1500);

                EditorGUILayout.LabelField("Volumetric Fog Noise Scaling B");
                volumeFog.noiseScale = EditorGUILayout.Slider(volumeFog.noiseScale, -20, 150);
                EditorGUILayout.LabelField("Volumetric Fog Noise Speed X");
                volumeFog.noiseSpeed.x = EditorGUILayout.Slider(volumeFog.noiseSpeed.x, -1500, 1500);
                EditorGUILayout.LabelField("Volumetric Fog Noise Speed Y");
                volumeFog.noiseSpeed.y = EditorGUILayout.Slider(volumeFog.noiseSpeed.y, -1500, 1500);
                EditorGUILayout.LabelField("Volumetric Fog Noise Speed Z");
                volumeFog.noiseSpeed.z = EditorGUILayout.Slider(volumeFog.noiseSpeed.z, -1500, 1500);

                EditorGUILayout.LabelField("------------------------------------------------------");
                EditorGUILayout.LabelField("Volumetric Lighting Controls", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("------------------------------------------------------");

                //SUN
                EditorGUILayout.LabelField("Sun Light Power");
                volumeFog.lightControlA.x = EditorGUILayout.Slider(volumeFog.lightControlA.x, 0.01f, 20);
                EditorGUILayout.LabelField("Sun Light Volume Steps");
                volumeFog.volumeSamplingControl.w = EditorGUILayout.Slider(volumeFog.volumeSamplingControl.w, -20, 25);

                //LOCAL LIGHTS
                EditorGUILayout.LabelField("Divide Local Lights Power");
                volumeFog.lightControlA.y = EditorGUILayout.Slider(volumeFog.lightControlA.y, 0.1f, 50000);
                EditorGUILayout.LabelField("Local Lights Fade Power");
                volumeFog.localLightAttenuation = EditorGUILayout.Slider(volumeFog.localLightAttenuation, 0.01f, 5);

                //LIGHTS NOISE
                EditorGUILayout.LabelField("Volumetric Lights Noise Power");
                volumeFog.VolumeLightNoisePower = EditorGUILayout.Slider(volumeFog.VolumeLightNoisePower, 0.0f, 10);

                EditorGUILayout.LabelField("Volumetric Lights Noise Mode (0 is no noise mode)");
                volumeFog.volumeSamplingControl.x = EditorGUILayout.Slider(volumeFog.volumeSamplingControl.x, 0f, 5);

                //LOCAL LIGHTS MODE
                if (volumeFog.volumeSamplingControl.x == 0)
                {
                    EditorGUILayout.LabelField("Point-Spot Light Volume Steps");
                    volumeFog.volumeSamplingControl.y = EditorGUILayout.Slider(volumeFog.volumeSamplingControl.y, -10, 150);
                }
                else
                {
                    EditorGUILayout.LabelField("Point-Spot Light Volume Steps no Noise");
                    volumeFog.volumeSamplingControl.y = EditorGUILayout.Slider(volumeFog.volumeSamplingControl.y, -10, 150);

                    EditorGUILayout.LabelField("Point-Spot Light Volume Steps with Noise");
                    volumeFog.volumeSamplingControl.z = EditorGUILayout.Slider(volumeFog.volumeSamplingControl.z, -10, 15);


                }

                //STEPS CONTROL
                EditorGUILayout.LabelField("Volume Lights Step Mode");
                EditorGUILayout.LabelField("0 is depth based step, otherwise is steady step size");
                volumeFog.stepsControl.x = EditorGUILayout.Slider(volumeFog.stepsControl.x, 0f, 150);

                //if (lookAtPoint.vector3Value.y > (target as connectSuntoVolumeFogURP).transform.position.y)
                //{
                //    EditorGUILayout.LabelField("(Above this object)");
                //}
                //if (lookAtPoint.vector3Value.y < (target as connectSuntoVolumeFogURP).transform.position.y)
                //{
                //    EditorGUILayout.LabelField("(Below this object)");
                //}

                serializedObject.ApplyModifiedProperties();

                if (GUI.changed) EditorUtility.SetDirty(target);

                SceneView.RepaintAll();
            }
            if (enableAdvanced)
            {
                base.OnInspectorGUI();
            }
        }

        public void OnSceneGUI()
        {
            var t = (target as connectSuntoVolumeFogURP);

            EditorGUI.BeginChangeCheck();


            //Vector3 pos = Handles.PositionHandle(t.lookAtPoint, Quaternion.identity);
            //if (EditorGUI.EndChangeCheck())
            //{
            //    Undo.RecordObject(target, "Move point");
            //    t.lookAtPoint = pos;
            //    t.Update();
            //}
        }
    }

}