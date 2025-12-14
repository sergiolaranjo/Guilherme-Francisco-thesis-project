// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

Shader "CardiacVR/AnatomicalTissue"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseColor ("Base Color", Color) = (0.8, 0.3, 0.25, 1)
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Subsurface Scattering)]
        _SubsurfaceColor ("Subsurface Color", Color) = (1, 0.4, 0.3, 1)
        _SubsurfaceStrength ("Subsurface Strength", Range(0, 2)) = 0.6
        _SubsurfaceDistortion ("Subsurface Distortion", Range(0, 1)) = 0.5
        _SubsurfacePower ("Subsurface Power", Range(1, 16)) = 4
        _SubsurfaceThickness ("Thickness Map", 2D) = "white" {}
        _ThicknessScale ("Thickness Scale", Range(0, 2)) = 1

        [Header(Fresnel and Rim)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.5
        _RimColor ("Rim Color", Color) = (1, 0.8, 0.7, 1)
        _RimPower ("Rim Power", Range(1, 10)) = 4
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.3

        [Header(Detail and Normal)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _DetailMap ("Detail Normal", 2D) = "bump" {}
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.3
        _DetailTiling ("Detail Tiling", Float) = 5

        [Header(Ambient Occlusion)]
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 0.5
        _AOColor ("AO Tint Color", Color) = (0.7, 0.5, 0.5, 1)

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1
        [Toggle] _EmissionPulse ("Pulse Emission", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0.1, 5)) = 1

        [Header(Transparency)]
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Toggle] _UseTransparency ("Enable Transparency", Float) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Unity 6 compatible multi_compile directives
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma shader_feature_local _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float fogFactor : TEXCOORD6;
                float4 shadowCoord : TEXCOORD7;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_SubsurfaceThickness); SAMPLER(sampler_SubsurfaceThickness);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _Metallic;

                float4 _SubsurfaceColor;
                float _SubsurfaceStrength;
                float _SubsurfaceDistortion;
                float _SubsurfacePower;
                float _ThicknessScale;

                float _FresnelPower;
                float _FresnelIntensity;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;

                float _NormalStrength;
                float _DetailStrength;
                float _DetailTiling;

                float _OcclusionStrength;
                float4 _AOColor;

                float4 _EmissionColor;
                float _EmissionIntensity;
                float _EmissionPulse;
                float _PulseSpeed;

                float _Opacity;
                float _UseTransparency;
            CBUFFER_END

            // Enhanced subsurface scattering calculation
            float3 SubsurfaceScattering(float3 lightDir, float3 viewDir, float3 normal, float thickness)
            {
                // Back-lighting for SSS
                float3 H = normalize(lightDir + normal * _SubsurfaceDistortion);
                float VdotH = pow(saturate(dot(viewDir, -H)), _SubsurfacePower);

                // Forward scattering
                float forwardScatter = VdotH * thickness;

                // Wrap lighting for soft diffuse
                float NdotL = dot(normal, lightDir);
                float wrapDiffuse = saturate((NdotL + 0.5) / 1.5);

                // Combine
                float3 sss = _SubsurfaceColor.rgb * (forwardScatter + wrapDiffuse * 0.3) * _SubsurfaceStrength;

                return sss * thickness;
            }

            // Fresnel effect
            float FresnelEffect(float3 normal, float3 viewDir)
            {
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                return fresnel * _FresnelIntensity;
            }

            // Rim lighting
            float3 RimLighting(float3 normal, float3 viewDir, float3 lightDir)
            {
                float NdotV = saturate(dot(normal, viewDir));
                float rim = pow(1.0 - NdotV, _RimPower);

                // Enhance rim on lit side
                float NdotL = saturate(dot(normal, lightDir) * 0.5 + 0.5);
                rim *= NdotL;

                return _RimColor.rgb * rim * _RimIntensity;
            }

            // Blend normals (UDN style)
            float3 BlendNormals(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                OUT.normalWS = normInputs.normalWS;
                OUT.tangentWS = normInputs.tangentWS;
                OUT.bitangentWS = normInputs.bitangentWS;

                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                OUT.shadowCoord = GetShadowCoord(posInputs);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample textures
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseMap.rgb * _BaseColor.rgb;
                float alpha = baseMap.a * _BaseColor.a * _Opacity;

                // Normal mapping
                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength
                );

                // Detail normal
                float2 detailUV = IN.uv * _DetailTiling;
                float3 detailNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV),
                    _DetailStrength
                );
                normalTS = BlendNormals(normalTS, detailNormal);

                // Transform to world space
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                // Ambient occlusion
                float ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv).r;
                ao = lerp(1.0, ao, _OcclusionStrength);

                // Thickness for SSS
                float thickness = SAMPLE_TEXTURE2D(_SubsurfaceThickness, sampler_SubsurfaceThickness, IN.uv).r;
                thickness *= _ThicknessScale;
                thickness = saturate(thickness);

                // Lighting
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(IN.viewDirWS);
                float3 halfDir = normalize(lightDir + viewDir);

                // Standard lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                float NdotH = saturate(dot(normalWS, halfDir));
                float NdotV = saturate(dot(normalWS, viewDir));

                // Half-Lambert for softer diffuse
                float halfLambert = NdotL * 0.5 + 0.5;
                halfLambert = halfLambert * halfLambert;

                // Shadow attenuation
                float shadow = mainLight.shadowAttenuation;

                // Diffuse
                float3 diffuse = albedo * halfLambert * mainLight.color * shadow;

                // Specular (GGX-style)
                float roughness = 1.0 - _Smoothness;
                roughness = roughness * roughness;
                float specPower = 2.0 / (roughness * roughness + 0.0001) - 2.0;
                float spec = pow(NdotH, specPower) * _Smoothness;
                float3 specular = spec * mainLight.color * shadow * lerp(0.04, albedo, _Metallic);

                // Subsurface scattering
                float3 sss = SubsurfaceScattering(lightDir, viewDir, normalWS, thickness);
                sss *= mainLight.color * shadow;

                // Fresnel
                float fresnel = FresnelEffect(normalWS, viewDir);
                float3 fresnelColor = lerp(float3(0.04, 0.04, 0.04), albedo, _Metallic) * fresnel;

                // Rim lighting
                float3 rim = RimLighting(normalWS, viewDir, lightDir);
                rim *= shadow;

                // Additional lights
                float3 additionalLighting = float3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    additionalLighting += albedo * addNdotL * addLight.color * addLight.distanceAttenuation * addLight.shadowAttenuation;

                    // SSS from additional lights
                    additionalLighting += SubsurfaceScattering(addLight.direction, viewDir, normalWS, thickness)
                                         * addLight.color * addLight.distanceAttenuation * 0.5;
                }
                #endif

                // Ambient
                float3 ambient = SampleSH(normalWS) * albedo * ao;
                ambient = lerp(ambient * _AOColor.rgb, ambient, ao); // Tint AO areas

                // Emission
                float3 emission = float3(0, 0, 0);
                #ifdef _EMISSION
                {
                    float emissionMult = _EmissionIntensity;
                    if (_EmissionPulse > 0.5)
                    {
                        emissionMult *= (sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5);
                    }
                    emission = _EmissionColor.rgb * emissionMult;
                }
                #endif

                // Combine
                float3 finalColor = ambient + diffuse + specular + sss + fresnelColor + rim + additionalLighting + emission;

                // Fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthOnlyFragment(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
