Shader "CardiacVR/TranslucentAnatomy"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseColor ("Base Color", Color) = (0.9, 0.85, 0.8, 0.7)
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Transparency)]
        _Opacity ("Opacity", Range(0, 1)) = 0.7
        _TransmissionColor ("Transmission Color", Color) = (1, 0.9, 0.85, 1)
        _TransmissionStrength ("Transmission Strength", Range(0, 2)) = 0.8
        _RefractionStrength ("Refraction Strength", Range(0, 0.5)) = 0.1

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        _FresnelColor ("Fresnel Color", Color) = (1, 0.95, 0.9, 1)
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.6
        _EdgeOpacity ("Edge Opacity Boost", Range(0, 1)) = 0.4

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 0.9, 0.85, 1)
        _RimPower ("Rim Power", Range(1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.5

        [Header(Internal Glow)]
        _GlowColor ("Internal Glow Color", Color) = (1, 0.5, 0.4, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.15
        _GlowFalloff ("Glow Falloff", Range(1, 10)) = 3

        [Header(Surface Detail)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5
        _DetailNoise ("Detail Noise", 2D) = "gray" {}
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.1

        [Header(Animation)]
        [Toggle] _AnimateRefraction ("Animate Refraction", Float) = 0
        _AnimSpeed ("Animation Speed", Range(0, 2)) = 0.5

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        LOD 300

        // Depth prepass for proper sorting
        Pass
        {
            Name "DepthPrepass"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                float4 screenPos : TEXCOORD6;
                float fogFactor : TEXCOORD7;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_DetailNoise); SAMPLER(sampler_DetailNoise);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _Metallic;

                float _Opacity;
                float4 _TransmissionColor;
                float _TransmissionStrength;
                float _RefractionStrength;

                float _FresnelPower;
                float4 _FresnelColor;
                float _FresnelIntensity;
                float _EdgeOpacity;

                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;

                float4 _GlowColor;
                float _GlowIntensity;
                float _GlowFalloff;

                float _NormalStrength;
                float _NoiseStrength;

                float _AnimateRefraction;
                float _AnimSpeed;
            CBUFFER_END

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
                OUT.screenPos = ComputeScreenPos(posInputs.positionCS);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Screen UV for effects
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // Sample base texture
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseMap.rgb * _BaseColor.rgb;

                // Normal mapping
                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength
                );

                // Detail noise for organic look
                float noise = SAMPLE_TEXTURE2D(_DetailNoise, sampler_DetailNoise, IN.uv * 3.0).r;
                noise = lerp(1.0, noise, _NoiseStrength);

                // Animated noise offset
                float2 noiseOffset = float2(0, 0);
                if (_AnimateRefraction > 0.5)
                {
                    noiseOffset = float2(
                        sin(_Time.y * _AnimSpeed + IN.uv.y * 10.0) * 0.01,
                        cos(_Time.y * _AnimSpeed + IN.uv.x * 10.0) * 0.01
                    );
                }

                // Transform normal to world space
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                // View direction
                float3 viewDir = normalize(IN.viewDirWS);

                // Fresnel
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                // Rim lighting
                float rim = pow(1.0 - NdotV, _RimPower);
                float3 rimColor = _RimColor.rgb * rim * _RimIntensity;

                // Internal glow (fake subsurface)
                float glow = pow(NdotV, _GlowFalloff);
                float3 glowColor = _GlowColor.rgb * glow * _GlowIntensity;

                // Refraction
                float2 refractionOffset = normalTS.xy * _RefractionStrength + noiseOffset;
                float2 refractedUV = screenUV + refractionOffset;

                // Sample scene behind (simplified - just use offset)
                float3 sceneColor = SampleSceneColor(refractedUV);

                // Transmission (light passing through)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                // Back-face transmission
                float transmission = saturate(dot(-normalWS, lightDir));
                transmission = pow(transmission, 2.0) * _TransmissionStrength;
                float3 transmissionColor = _TransmissionColor.rgb * transmission * mainLight.color;

                // Standard lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                float halfLambert = NdotL * 0.5 + 0.5;

                // Specular
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float roughness = 1.0 - _Smoothness;
                roughness = max(roughness * roughness, 0.001);
                float spec = pow(NdotH, 2.0 / roughness);
                float3 specular = spec * mainLight.color * _Smoothness;

                // Diffuse
                float3 diffuse = albedo * halfLambert * mainLight.color;

                // Ambient
                float3 ambient = SampleSH(normalWS) * albedo * 0.5;

                // Additional lights contribution
                float3 additionalLighting = float3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    additionalLighting += albedo * addNdotL * addLight.color * addLight.distanceAttenuation * 0.5;
                }
                #endif

                // Combine surface color
                float3 surfaceColor = ambient + diffuse + specular + fresnelColor + rimColor + glowColor + transmissionColor + additionalLighting;

                // Blend with scene based on opacity and fresnel
                float finalOpacity = _Opacity * _BaseColor.a * noise;
                finalOpacity = lerp(finalOpacity, saturate(finalOpacity + _EdgeOpacity), fresnel);

                // Mix surface with refracted scene
                float3 finalColor = lerp(sceneColor, surfaceColor, finalOpacity);

                // Add transmission on top
                finalColor += transmissionColor * (1.0 - finalOpacity) * 0.5;

                // Fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, finalOpacity);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
