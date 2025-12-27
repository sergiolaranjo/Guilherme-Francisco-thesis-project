// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

Shader "Custom/VolumeRendering"
{
    Properties
    {
        _Data ("Volume Data", 3D) = "" {}
        _TransferFunction ("Transfer Function", 2D) = "white" {}
        _Alpha ("Alpha Multiplier", Range(0, 5)) = 1.0
        _StepSize ("Step Size", Range(0.001, 0.1)) = 0.01
        _Iterations ("Max Iterations", Range(32, 512)) = 256

        // Clipping planes
        _ClipPlaneNormal ("Clip Plane Normal", Vector) = (0, 0, 0, 0)
        _ClipPlanePosition ("Clip Plane Position", Vector) = (0, 0, 0, 0)
        _UseClipPlane ("Use Clip Plane", Float) = 0

        // Window Level (for CT/MRI)
        _WindowCenter ("Window Center", Range(0, 1)) = 0.5
        _WindowWidth ("Window Width", Range(0.001, 1)) = 0.5

        // Lighting
        _LightDir ("Light Direction", Vector) = (0, 1, 0, 0)
        _AmbientLight ("Ambient Light", Range(0, 1)) = 0.3
        _DiffuseLight ("Diffuse Light", Range(0, 1)) = 0.7
        _SpecularLight ("Specular Light", Range(0, 1)) = 0.2
        _Shininess ("Shininess", Range(1, 128)) = 32

        // Threshold for tissue visibility
        _MinThreshold ("Min Threshold", Range(0, 1)) = 0.1
        _MaxThreshold ("Max Threshold", Range(0, 1)) = 1.0

        // Quality settings
        _Density ("Density", Range(0.1, 10)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "VolumeRendering"
            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.5

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler3D _Data;
            sampler2D _TransferFunction;
            float _Alpha;
            float _StepSize;
            int _Iterations;

            float3 _ClipPlaneNormal;
            float3 _ClipPlanePosition;
            float _UseClipPlane;

            float _WindowCenter;
            float _WindowWidth;

            float3 _LightDir;
            float _AmbientLight;
            float _DiffuseLight;
            float _SpecularLight;
            float _Shininess;

            float _MinThreshold;
            float _MaxThreshold;
            float _Density;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz + 0.5; // Transform to 0-1 range
                o.screenPos = ComputeScreenPos(o.vertex);

                return o;
            }

            // Apply window level transformation (for CT Hounsfield units)
            float ApplyWindowLevel(float value)
            {
                float minVal = _WindowCenter - _WindowWidth * 0.5;
                float maxVal = _WindowCenter + _WindowWidth * 0.5;
                return saturate((value - minVal) / (_WindowWidth + 0.0001));
            }

            // Sample the transfer function
            float4 SampleTransferFunction(float density)
            {
                return tex2D(_TransferFunction, float2(density, 0.5));
            }

            // Calculate gradient (normal) at a position using central differences
            float3 CalculateGradient(float3 pos)
            {
                float d = _StepSize;
                float3 gradient;

                gradient.x = tex3D(_Data, pos + float3(d, 0, 0)).r - tex3D(_Data, pos - float3(d, 0, 0)).r;
                gradient.y = tex3D(_Data, pos + float3(0, d, 0)).r - tex3D(_Data, pos - float3(0, d, 0)).r;
                gradient.z = tex3D(_Data, pos + float3(0, 0, d)).r - tex3D(_Data, pos - float3(0, 0, d)).r;

                return normalize(gradient);
            }

            // Apply Phong lighting
            float3 ApplyLighting(float3 color, float3 normal, float3 viewDir)
            {
                float3 lightDir = normalize(_LightDir);

                // Ambient
                float3 ambient = _AmbientLight * color;

                // Diffuse
                float diff = max(dot(normal, lightDir), 0.0);
                float3 diffuse = _DiffuseLight * diff * color;

                // Specular
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfDir), 0.0), _Shininess);
                float3 specular = _SpecularLight * spec * float3(1, 1, 1);

                return ambient + diffuse + specular;
            }

            // Check if point is clipped by the clipping plane
            bool IsClipped(float3 worldPos)
            {
                if (_UseClipPlane < 0.5)
                    return false;

                float3 toPoint = worldPos - _ClipPlanePosition;
                return dot(toPoint, normalize(_ClipPlaneNormal)) < 0;
            }

            // Ray-box intersection
            bool IntersectBox(float3 rayOrigin, float3 rayDir, out float tNear, out float tFar)
            {
                float3 invRay = 1.0 / rayDir;
                float3 t0 = (0 - rayOrigin) * invRay;
                float3 t1 = (1 - rayOrigin) * invRay;

                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                tNear = max(max(tmin.x, tmin.y), tmin.z);
                tFar = min(min(tmax.x, tmax.y), tmax.z);

                return tNear < tFar && tFar > 0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate ray direction from camera to fragment
                float3 rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz + 0.5;
                float3 rayDir = normalize(i.localPos - rayOrigin);

                // Find ray-volume intersection
                float tNear, tFar;
                if (!IntersectBox(rayOrigin, rayDir, tNear, tFar))
                    discard;

                tNear = max(tNear, 0.0);

                // Initialize accumulation
                float4 accumulatedColor = float4(0, 0, 0, 0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Ray marching loop
                float t = tNear;
                int iterations = 0;

                [loop]
                for (int j = 0; j < _Iterations && t < tFar; j++)
                {
                    float3 samplePos = rayOrigin + rayDir * t;

                    // Check bounds
                    if (samplePos.x < 0 || samplePos.x > 1 ||
                        samplePos.y < 0 || samplePos.y > 1 ||
                        samplePos.z < 0 || samplePos.z > 1)
                    {
                        t += _StepSize;
                        continue;
                    }

                    // Check clipping plane
                    float3 worldSamplePos = mul(unity_ObjectToWorld, float4(samplePos - 0.5, 1)).xyz;
                    if (IsClipped(worldSamplePos))
                    {
                        t += _StepSize;
                        continue;
                    }

                    // Sample volume data
                    float density = tex3Dlod(_Data, float4(samplePos, 0)).r;

                    // Apply window level
                    density = ApplyWindowLevel(density);

                    // Apply threshold
                    if (density < _MinThreshold || density > _MaxThreshold)
                    {
                        t += _StepSize;
                        continue;
                    }

                    // Get color from transfer function
                    float4 sampleColor = SampleTransferFunction(density);
                    sampleColor.a *= _Alpha * _Density * _StepSize;

                    // Apply lighting if alpha is significant
                    if (sampleColor.a > 0.01)
                    {
                        float3 gradient = CalculateGradient(samplePos);
                        if (length(gradient) > 0.1)
                        {
                            sampleColor.rgb = ApplyLighting(sampleColor.rgb, gradient, viewDir);
                        }
                    }

                    // Front-to-back compositing
                    sampleColor.rgb *= sampleColor.a;
                    accumulatedColor += (1.0 - accumulatedColor.a) * sampleColor;

                    // Early termination
                    if (accumulatedColor.a > 0.99)
                        break;

                    t += _StepSize;
                    iterations++;
                }

                // Discard fully transparent fragments
                if (accumulatedColor.a < 0.01)
                    discard;

                return accumulatedColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
